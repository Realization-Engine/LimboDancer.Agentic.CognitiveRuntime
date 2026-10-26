using System.Globalization;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31c (design D11; design section 9, "Names"): the names a view reads for a game's units (<see cref="UnitNames"/>), on the game of The Guards
/// Counterattack played to its end through the Play page (613 revisions, the fixture <c>guards-dl-01.game.json</c>). A side's own tags go by entry
/// and kind and are kept through a Replacement, a Reduction, and a Battle Hardening; the other side's units carry no tag and read by counter and
/// Location (the user's ruling of 2026-10-22); a unit the view may not name reads "a concealed unit", or "an uninspected unit" beneath an uninspected
/// stack's top; the adjudicator reads each unit's own-side tag;
/// and no text a view reads of the game holds a unit's id or a Location's identifier.
/// </summary>
public sealed partial class UnitNamesTests : IDisposable
{
    private const string Played = "guards-dl-01";
    private static readonly string[] Sides = ["german", "russian"];
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-names-" + Guid.NewGuid().ToString("N"));
    private readonly GameLibrary games;
    private readonly Lazy<GameHistory> history;

    public UnitNamesTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var maps = new MapService(options, new FakeVaslMapSource());
        var library = new UnitLibrary(options);
        var boards = new SyntheticBoards(maps);
        games = new GameLibrary(library, boards, new LivePlay(library, boards));

        // The played game is read as the live game it is, as ReplayStepsTests reads it.
        var folder = Path.Combine(library.UnitsRoot, "live", LivePlay.Tenant.ToString("N"));
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", Played + ".game.json"), Path.Combine(folder, Played + ".game.json"));
        history = new Lazy<GameHistory>(() => games.Load(GameLibrary.LivePrefix + Played).History!);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private GameHistory History => history.Value;

    private UnitNames Names(string view) => games.NamesOf(History, new Perspective(view));

    /// <summary>Every unit the game ever held, in the order it entered, with the revision that first holds it.</summary>
    private List<(UnitInstance Unit, int Entered)> Entries()
    {
        var entries = new List<(UnitInstance Unit, int Entered)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var revision = 1; revision <= History.States.Count; revision++)
        {
            entries.AddRange(History.States[revision - 1].Units.Where(unit => seen.Add(unit.Id)).Select(unit => (unit, revision)));
        }

        return entries;
    }

    /// <summary>The units a lineage made, each with the unit it came from.</summary>
    private Dictionary<string, (LineageAction Action, string From)> Parents() =>
        History.Events.Select(item => item.Payload).OfType<LineageRecorded>()
            .SelectMany(lineage => lineage.Produced.Select(unit => (unit.Id, lineage.Action, From: lineage.Consumed[0])))
            .ToDictionary(item => item.Id, item => (item.Action, item.From), StringComparer.Ordinal);

    private static string Group(UnitInstance unit) => unit.Kind is "asl:leader" or "asl:hero" ? "smc" : "mmc";

    [Fact]
    public void ASidesOwnUnitsAreTaggedByEntryWithinTheirKind()
    {
        var parents = Parents();
        foreach (var side in Sides)
        {
            // Squads, half-squads, and crews share one count, and leaders and heroes another (design section 13); a unit a lineage made takes
            // no new number.
            var names = Names(side);
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (unit, _) in Entries().Where(entry => entry.Unit.Side == side && !parents.ContainsKey(entry.Unit.Id)))
            {
                var next = counts[Group(unit)] = counts.GetValueOrDefault(Group(unit)) + 1;
                Assert.Equal(string.Create(CultureInfo.InvariantCulture, $"{char.ToUpperInvariant(side[0])}{next}"), names.Tag(unit.Id));
            }

            Assert.True(counts["mmc"] > 1 && counts["smc"] > 1);
        }

        // The name is the printed values, the kind, and the tag; a leader's count is its own, so a squad and a leader may both be "G1".
        var german = Names("german");
        Assert.Equal("4-6-7 squad G1", german.Of("g-squad-1"));
        Assert.Equal("4-6-7 squad G13", german.Of("g-squad-13"));
        Assert.Equal("8-0 leader G2", german.Of("g-leader-8-0-1"));
        Assert.Equal("9-2 leader G3", german.Of("g-leader-9-2-1"));
        var russian = Names("russian");
        Assert.Equal("4-4-7 squad R2", russian.Of("r-squad-2"));
        Assert.Equal("6-2-8 squad R21", russian.Of("r-squad-21"));
        Assert.Equal("9-0 Commissar R1", russian.Of("r-commissar-9-0-1"));

        // An id that is no unit of the game is returned as it is.
        Assert.Equal("no-such-unit", german.Of("no-such-unit"));
        Assert.Null(german.Tag("no-such-unit"));
    }

    [Fact]
    public void ATagIsKeptThroughReplacementReductionAndBattleHardening()
    {
        // Every unit a lineage made of one unit keeps that unit's tag, in its own side's view and in the adjudicator's.
        var parents = Parents();
        Assert.Contains(parents.Values, parent => parent.Action == LineageAction.Replaced);
        Assert.Contains(parents.Values, parent => parent.Action == LineageAction.Reduced);
        var sideOf = Entries().ToDictionary(entry => entry.Unit.Id, entry => entry.Unit.Side, StringComparer.Ordinal);
        foreach (var (id, parent) in parents.Where(pair => pair.Value.Action is LineageAction.Replaced or LineageAction.Reduced))
        {
            Assert.Equal(Names(sideOf[id]).Tag(parent.From), Names(sideOf[id]).Tag(id));
            Assert.Equal(Names(Perspective.AdjudicatorName).Tag(parent.From), Names(Perspective.AdjudicatorName).Tag(id));
        }

        // The values are the unit's own at the time, so the name changes and the tag does not: a squad Replaced by a lower class (A19.13), a
        // squad Reduced to a HS (A7.302), a HS made of a Replaced squad, and a Battle Hardened leader (A15.3).
        var german = Names("german");
        Assert.Equal("4-4-7 squad G1", german.Of("fire-a672887feaab-g-squad-1"));
        Assert.Equal("2-3-7 half-squad G3", german.Of("fire-7a8e607e9d46-g-squad-3"));
        Assert.Equal("9-1 leader G1", german.Of("g-leader-9-1-1"));
        Assert.Equal("9-2 leader G1", german.Of("choose-6a4667fb0056-g-leader-9-1-1"));
        var russian = Names("russian");
        Assert.Equal(russian.Tag("r-squad-3"), russian.Tag("fire-124226653741-fire-151c1e236423-r-squad-3"));
        Assert.StartsWith("2-2-6 half-squad R", russian.Of("fire-124226653741-fire-151c1e236423-r-squad-3"), StringComparison.Ordinal);

        // A leader created in Close Combat (A18.12) takes the next number of its count.
        Assert.Equal("8-1 leader G6", german.Of("cc-473ba669b5ed-leader-0"));
    }

    /// <summary>Where a unit stood at a revision, in the page's words.</summary>
    private string At(string id, int revision)
    {
        var state = History.States[revision - 1];
        return DisplayText.Place(state, state.Location(id)!.Location);
    }

    [Fact]
    public void TheOtherSidesUnitsCarryNoTagAndReadByCounterAndLocation()
    {
        // The user's ruling of 2026-10-22 (option 2): no unit is numbered by the other side's view, so no tag names one counter in one view and
        // another in the other; a unit the view holds reads by its counter and where it stood, and one it never held reads as a concealed unit.
        foreach (var side in Sides)
        {
            var perspective = new Perspective(side);
            var names = Names(side);
            var enemy = Entries().Where(entry => entry.Unit.Side != side).Select(entry => entry.Unit).ToArray();
            Assert.All(enemy, unit => Assert.Null(names.Tag(unit.Id)));

            var everHeld = new HashSet<string>(StringComparer.Ordinal);
            var named = 0;
            for (var revision = 1; revision <= History.States.Count; revision += 7)
            {
                foreach (var unit in games.ViewOf(History, revision, perspective).Units.Where(unit => unit.Side != side))
                {
                    everHeld.Add(unit.Id);
                    if (History.States[revision - 1].Location(unit.Id) is not null && History.States[revision - 1].Unit(unit.Id) is { Kind: not UnitKinds.Dummy })
                    {
                        Assert.EndsWith($" in {At(unit.Id, revision)}", names.Of(unit.Id, revision), StringComparison.Ordinal);
                        named++;
                    }
                }
            }

            Assert.True(named > 50);
            Assert.All(enemy.Where(unit => !everHeld.Contains(unit.Id) && !games.ViewOf(History, History.States.Count, perspective).Units.Any(seen => seen.Id == unit.Id)),
                unit => Assert.True(UnitNames.IsUnnamed(names.Of(unit.Id, 1))));
        }

        // The German view names r-squad-2 by where it stands; the Russian view by its own tag.
        var german = Names("german");
        Assert.Equal($"4-4-7 squad in {At("r-squad-2", 57)}", german.Of("r-squad-2", 57));
        Assert.Equal("4-4-7 squad R2", Names("russian").Of("r-squad-2"));
    }

    [Fact]
    public void AHexAUnitIsNamedByIsSaidOnceInItsClause()
    {
        // The user's ruling of 2026-10-22: a text that says the hex again does not repeat it after each unit's name.
        var german = Names("german");
        var at = History.States[56].Location("r-squad-2")!.Location.ToString();
        var place = At("r-squad-2", 57);
        Assert.Equal($"4-4-7 squad moves from {place} to [K4], level 1", german.InText($"r-squad-2 moves from {at} to bd01:K4:1", 57));
        Assert.Equal($"4-4-7 squad, 4-4-7 squad in {place} fire at [K4]", german.InText($"r-squad-2, r-squad-2 in {at} fire at bd01:K4:0", 57));

        // Another clause, or another Location, keeps it.
        Assert.Equal($"4-4-7 squad in {place} fires; then 4-4-7 squad in {place} is pinned", german.InText("r-squad-2 fires; then r-squad-2 is pinned", 57));
        Assert.Equal($"4-4-7 squad in {place} fires at [K4], level 1", german.InText("r-squad-2 fires at bd01:K4:1", 57));
    }

    [Fact]
    public void AUnitTheViewMayNotNameReadsAConcealedUnit()
    {
        // In a record of an event, a unit of the other side is named only when the view held it by name just before (plan section 15.9).
        var entered = Entries().ToDictionary(entry => entry.Unit.Id, entry => entry.Entered, StringComparer.Ordinal);
        foreach (var side in Sides)
        {
            var perspective = new Perspective(side);
            var names = Names(side);
            var (named, unnamed, uninspected) = (0, 0, 0);
            for (var revision = 1; revision <= History.States.Count; revision += 3)
            {
                var view = games.ViewOf(History, revision, perspective);
                var held = view.Units.Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
                var stacks = view.Sealed.Where(item => item.Uninspected).Select(item => (item.Side, item.Location)).ToHashSet();
                var state = History.States[revision - 1];
                foreach (var unit in state.Units.Where(unit => unit.Side != side && entered[unit.Id] <= revision))
                {
                    var said = names.Of(unit.Id, revision);
                    if (held.Contains(unit.Id))
                    {
                        Assert.False(UnitNames.IsUnnamed(said));
                        Assert.Null(names.Tag(unit.Id));
                        named++;
                    }
                    else if (unit.Status == InstanceStatus.Active && unit.Kind != UnitKinds.Dummy && GameState.Condition(unit, Conditions.Concealed) != ConditionState.True
                        && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True && state.Location(unit.Id) is { } at && stacks.Contains((unit.Side!, at.Location)))
                    {
                        // A2.9, A12.16 (rulings R23.3, R70.18): a counter beneath an uninspected stack's top is under no "?" (the referee's finding at the reviews).
                        Assert.Equal(UnitNames.UninspectedUnit, said);
                        uninspected++;
                    }
                    else
                    {
                        Assert.Equal(UnitNames.Unnamed, said);
                        unnamed++;
                    }
                }
            }

            Assert.True(named > 0 && unnamed > 0 && uninspected > 0);

            // Its own units are named whatever their state.
            Assert.All(History.States[^1].Units.Where(unit => unit.Side == side), unit => Assert.False(UnitNames.IsUnnamed(names.Of(unit.Id, 1))));
        }

        // r-squad-2 sets up beneath the top of a stack under no "?", which may not be inspected before play (A2.9; ruling R23.3): the German side reads an
        // uninspected unit (the referee's finding at the reviews of pass 70, which found "a concealed unit" false) until the revision at which it first holds
        // the squad by name. r-squad-20 sets up under "?" and reads a concealed unit.
        var german = Names("german");
        Assert.Equal(UnitNames.UninspectedUnit, german.Of("r-squad-2", 56));
        Assert.Equal(UnitNames.Unnamed, german.Of("r-squad-20", 56));
        Assert.Equal($"4-4-7 squad in {At("r-squad-2", 57)}", german.Of("r-squad-2", 57));
        Assert.Equal("an uninspected unit moves, and 4-6-7 squad G4 fires", german.InText("r-squad-2 moves, and g-squad-4 fires", 56));
        Assert.Equal($"4-4-7 squad in {At("r-squad-2", 57)} moves to [K4], level 1", german.InText("r-squad-2 moves to bd01:K4:1", 57));

        // A weapon reads beside its holder and alone, and one the view does not hold is not counted.
        Assert.Equal("the MMG", Names("russian").Of("r-mmg-1"));
        Assert.Equal("4-4-7 squad R5 with its MMG", Names("russian").InText("r-squad-5 with its r-mmg-1", 600));
    }

    [Fact]
    public void ARunOfConcealedUnitsReadsAsTheirCount()
    {
        // The user, 2026-10-21: "a concealed unit, a concealed unit, ..." reads as "3 concealed units"; one alone, or two apart, read as before.
        // So does a run of uninspected units (the reviews of pass 70).
        var german = Names("german");
        Assert.Equal("3 concealed units fire at 4-6-7 squad G4", german.InText("r-squad-20, r-squad-20, r-squad-20 fire at g-squad-4", 56));
        Assert.Equal("2 concealed units and 4-6-7 squad G4", german.InText("r-squad-20, r-squad-20 and g-squad-4", 56));
        Assert.Equal("a concealed unit and a concealed unit", german.InText("r-squad-20 and r-squad-20", 56));
        Assert.Equal("2 uninspected units and 4-6-7 squad G4", german.InText("r-squad-2, r-squad-2 and g-squad-4", 56));
        Assert.Equal($"4-4-7 squad, 4-4-7 squad in {At("r-squad-2", 57)}", german.InText("r-squad-2, r-squad-2", 57));
    }

    [Fact]
    public void TheAdjudicatorReadsEachUnitsOwnSideTag()
    {
        var adjudicator = Names(Perspective.AdjudicatorName);
        Assert.All(Entries(), entry =>
        {
            Assert.Equal(Names(entry.Unit.Side).Tag(entry.Unit.Id), adjudicator.Tag(entry.Unit.Id));
            Assert.NotEqual(UnitNames.Unnamed, adjudicator.Of(entry.Unit.Id, 1));
        });
        Assert.Equal("4-4-7 squad R2", adjudicator.Of("r-squad-2"));
        Assert.Equal("4-4-7 squad R3", adjudicator.Of("r-squad-3"));
    }

    [Fact]
    public void NoTextAViewReadsOfThePlayedGameHoldsAnIdOrAnIdentifierLocation()
    {
        // Every id the game ever gave a unit, a Gun, or a weapon, and every definition's id.
        var ids = History.States.SelectMany(state => state.Units.Select(unit => unit.Id).Concat(state.Equipment.Select(item => item.Id))
            .Concat(state.Units.Select(unit => unit.Definition?.Definition).OfType<string>())).Distinct(StringComparer.Ordinal).ToArray();
        Assert.True(ids.Length > 60);

        foreach (var view in Sides.Append(Perspective.AdjudicatorName))
        {
            var perspective = new Perspective(view);
            var names = Names(view);
            var texts = new List<(string Where, string Text)>();
            var records = new PlayRecords(History, perspective, History.States.Count, names);
            texts.AddRange(records.Lines.Select(line => ($"line of revision {line.Revision}", line.Text)));
            texts.AddRange(records.Fire.SelectMany(fire => new[] { ("fire group", fire.Group), ("fire target", fire.Target) }));
            texts.AddRange(records.Ordnance.Concat(records.CloseCombat).Concat(records.Night).Concat(records.Snipers).Select(item => ("record", item.Text)));
            texts.AddRange(records.RallyAndRepair.Select(item => ($"{item.Kind} record", item.Text)));
            texts.AddRange(records.RollRows.Select(row => ("roll", row.Text)));
            Assert.True(texts.Count > 100);

            // The view's own steps: what changed over each, and the latest line as the game stood after it.
            var last = games.ViewOf(History, History.States.Count, perspective);
            foreach (var step in ReplaySteps.Read(History, last, revision => games.ViewOf(History, revision, perspective)).Steps)
            {
                var after = games.ViewOf(History, step.Last, perspective);
                var changes = ReplayDiff.Of(step.First > 1 ? games.ViewOf(History, step.First - 1, perspective) : null, after, [.. after.Events.Where(item => item.Revision >= step.First)], names);
                texts.AddRange(ReplayDiff.Lines(changes).Select(line => ($"change of step {step.Number}", line)));
                texts.Add(($"title of step {step.Number}", step.Title));
                if (new PlayRecords(History, perspective, step.Last, names).Latest is { } latest)
                {
                    texts.Add(($"Latest at revision {step.Last}", latest));
                }
            }

            Assert.All(texts, item =>
            {
                Assert.False(LocationId().IsMatch(item.Text), $"{view}, {item.Where}: an identifier Location in \"{item.Text}\"");
                Assert.False(ids.Any(id => Holds(item.Text, id)), $"{view}, {item.Where}: an id in \"{item.Text}\"");
            });
        }
    }

    /// <summary>Whether a text holds an id as a whole word, so "r-squad-1" is not found inside "r-squad-19".</summary>
    private static bool Holds(string text, string id)
    {
        static bool Part(char letter) => char.IsLetterOrDigit(letter) || letter == '-';
        for (var at = text.IndexOf(id, StringComparison.Ordinal); at >= 0; at = text.IndexOf(id, at + 1, StringComparison.Ordinal))
        {
            var end = at + id.Length;
            if ((at == 0 || !Part(text[at - 1])) && (end == text.Length || !Part(text[end])))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"\b(?:bd\w+|ab-[a-z0-9-]+):[A-Z]{1,2}\d{1,2}\b")]
    private static partial Regex LocationId();
}
