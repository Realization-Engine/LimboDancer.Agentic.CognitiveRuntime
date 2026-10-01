using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 23 of the Card Play and Map Studio Redesign Plan (rulings R23.1 to R23.6): a side's view at setup and in play, HIP by SSR, hidden units placed
/// beneath "?", the non-OB "?", and Control as a side may know it. The Guards Counterattack on board 01: the Germans set up first, then the Russians.
/// </summary>
public sealed class BacklogPass23Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a723");
    private static readonly GameScope Scope = new(Tenant, "p23");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly Perspective German = Perspective.Side("german");
    private static readonly Perspective Russian = Perspective.Side("russian");

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p23-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly ScenarioCardLibrary cards;

    public BacklogPass23Tests()
    {
        store = new FileGameStore(Path.Combine(root, "games"));
        cards = new ScenarioCardLibrary(Path.Combine(root, "cards"));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    /// <summary>LOS on board 01, which the fixture does not carry: clear unless blocked (referee, pass 23).</summary>
    private sealed class StubLos : IFireLosReader
    {
        public bool Blocked
        {
            get; set;
        }

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(Blocked ? LosStatus.Blocked : LosStatus.Clear, Blocked, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private StubLos? los;

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog], fireLos: los, cardLibrary: cards);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: new DiceRoller(_ => 5));

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    /// <summary>The Guards Counterattack saved as a user card, with an SSR giving the Russians HIP (ruling R23.5).</summary>
    private string GuardsWithHip(string token = "hip:russian:1")
    {
        var guards = Card("guards-counterattack");
        var card = guards with
        {
            Id = "guards-hip",
            SpecialRules = [.. guards.SpecialRules, new ScenarioCardRule(guards.SpecialRules.Count + 1, "Russian units may set up HIP.", "token", [token], [], null)],
        };
        Assert.Empty(cards.Save(card, Catalog));
        return card.Id;
    }

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task<PlayResult> Place(string card, params Dictionary<string, object>[] placements)
    {
        var node = JsonSerializer.SerializeToNode(new
        {
            gameId = Scope.Game,
            attemptId = $"setup-{Revision}",
            expectedRevision = Revision,
            placements,
        })!.AsObject();
        if (Revision == 0)
        {
            node["start"] = JsonSerializer.SerializeToNode(new
            {
                label = card,
                catalog = "asl-scenario-a1@1.12.0",
                scenario = new
                {
                    id = card,
                    sha256 = cards.Sha256(card),
                    title = card
                },
            });
        }

        return await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(node));
    }

    private async Task<PlayResult> Conceal(string side, params string[] locations) => await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
    {
        gameId = Scope.Game,
        attemptId = $"conceal-{Revision}",
        expectedRevision = Revision,
        placements = Array.Empty<object>(),
        conceal = new
        {
            side,
            locations
        },
    }));

    private async Task<PlayResult> PlaceHidden(params string[] unitIds) => await Commit(Play(), GameActions.PlaceHidden, JsonSerializer.SerializeToElement(new
    {
        gameId = Scope.Game,
        attemptId = $"place-hidden-{Revision}",
        expectedRevision = Revision,
        unitIds,
    }));

    private async Task<PlayResult> Advance() => await Commit(Play(), GameActions.AdvancePhase, JsonSerializer.SerializeToElement(new
    {
        gameId = Scope.Game,
        attemptId = $"advance-{Revision}",
        expectedRevision = Revision,
    }));

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group, bool hidden = false) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["group"] = group,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = hidden },
    };

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A group's whole OB on board 01, squads three to a hex of its buildings, SMC with the first squad, SW held by the area's first squad.</summary>
    private static List<Dictionary<string, object>> Ob(ScenarioCard card, string side, int groupIndex)
    {
        var group = card.Sides.Single(item => item.Side == side).Groups[groupIndex];
        var id = ScenarioCards.GroupId(side, groupIndex);
        var placements = new List<Dictionary<string, object>>();
        var squadsAt = new Dictionary<string, int>(StringComparer.Ordinal);
        var holders = new Dictionary<string, string>(StringComparer.Ordinal);
        var serial = 0;
        foreach (var line in group.Units)
        {
            var areas = line.Area is { } named ? group.Areas.Where(area => area.Id == named).ToArray() : [.. group.Areas.Where(area => area.Kind == "building")];
            var hexes = areas.SelectMany(area => area.Hexes!).ToArray();
            var definition = Catalog.Definition(line.Definition)!;
            for (var count = 0; count < line.Count; count++)
            {
                var unitId = $"{id}-{++serial}";
                if (Vocabulary.IsA(definition.Kind, "asl:equipment"))
                {
                    placements.Add(Weapon(unitId, line.Definition, holders[areas[0].Id], side));
                    continue;
                }

                var hex = definition.Kind == "asl:squad" ? hexes.First(item => squadsAt.GetValueOrDefault(item) < 3) : hexes[0];
                if (definition.Kind == "asl:squad")
                {
                    squadsAt[hex] = squadsAt.GetValueOrDefault(hex) + 1;
                    holders.TryAdd(areas[0].Id, unitId);
                }

                placements.Add(Unit(unitId, line.Definition, $"bd01:{hex}:0", side, id));
            }
        }

        return placements;
    }

    private GameView ViewAs(Perspective perspective)
    {
        var history = History;
        return GameView.Of(history, history.Current!.Revision, perspective, Planner().OutOfSight(history.Current!, perspective));
    }

    /// <summary>The Russian OB, the first squad that holds no SW hidden.</summary>
    private static (List<Dictionary<string, object>> Placements, string Hidden) RussianWithOneHidden(ScenarioCard card)
    {
        var russian = Ob(card, "russian", 0).Concat(Ob(card, "russian", 1)).ToList();
        var holders = russian.Where(item => item.ContainsKey("holding")).Select(item => JsonSerializer.SerializeToElement(item["holding"]).GetProperty("holder").GetString())
            .ToHashSet(StringComparer.Ordinal);
        var index = russian.FindIndex(item => (string)item["kind"] == "asl:squad" && !holders.Contains((string)item["id"]));
        var squad = russian[index];
        var at = JsonSerializer.SerializeToElement(squad["position"]).GetProperty("at").GetString()!;
        russian[index] = Unit((string)squad["id"], (string)squad["definition"], at, "russian", (string)squad["group"], hidden: true);
        return (russian, (string)squad["id"]);
    }

    // R23.3 (A12.12, A2.9): the side setting up is out of the other's sight; once it has finished, the other sees each of its stacks' top counter and
    // counts the rest; from the start of play its Known units are seen whole.
    [Fact]
    public async Task EachSideSetsUpOutOfTheOthersSight()
    {
        var card = Card("guards-counterattack");
        var german = Ob(card, "german", 0);
        Committed(await Place("guards-counterattack", [.. german.Take(3)]));
        Assert.Equal(["german-1"], Planner().OutOfSight(Current, Russian));
        Assert.Empty(Planner().OutOfSight(Current, German));
        Assert.Empty(Planner().OutOfSight(Current, Perspective.Adjudicator));
        Assert.DoesNotContain(ViewAs(Russian).Units, unit => unit.Side == "german");
        Assert.DoesNotContain(ViewAs(Russian).Sealed, presence => presence.Side == "german");

        // Referee, pass 23: the events that created them stay out of the Russian list too.
        Assert.DoesNotContain(ViewAs(Russian).Events, item => item.Payload is InstanceCreated { Instance.Side: "german" });
        Assert.Contains(ViewAs(German).Events, item => item.Payload is InstanceCreated { Instance.Side: "german" });
        Assert.Contains(ViewAs(Perspective.Adjudicator).Units, unit => unit.Side == "german");

        Committed(await Place("guards-counterattack", [.. german.Skip(3)]));
        Assert.Empty(Planner().OutOfSight(Current, Russian));
        Assert.Equal(["russian-1", "russian-2"], Planner().OutOfSight(Current, German).Order(StringComparer.Ordinal));

        // A2.9: each German stack shows its top counter to the Russians; the rest are counted, not identified, and what they hold is withheld.
        var russianView = ViewAs(Russian);
        var stacks = Current.Units.Where(unit => unit.Side == "german").GroupBy(unit => Current.Location(unit.Id)!.Location).ToArray();
        Assert.Equal(stacks.Length, russianView.Units.Count(unit => unit.Side == "german"));
        Assert.Equal(stacks.Sum(stack => stack.Count() - 1), russianView.Sealed.Count(presence => presence.Side == "german" && presence.Uninspected));
        Assert.All(russianView.Equipment.Where(item => item.Side == "german"), item => Assert.Contains(russianView.Units, unit => unit.Id == item.Holding?.Holder));

        Committed(await Place("guards-counterattack", [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
        Assert.Empty(Planner().OutOfSight(Current, German));
        Assert.Contains(ViewAs(German).Sealed, presence => presence.Side == "russian" && presence.Uninspected);

        // From the start of play the enemy's Known units are seen whole (A2.9 limits inspection only "prior to the start of play").
        Committed(await Advance());
        var inPlay = ViewAs(German);
        Assert.Equal(Current.Units.Count(unit => unit.Side == "russian" && unit.Status == InstanceStatus.Active), inPlay.Units.Count(unit => unit.Side == "russian"));
        Assert.Empty(inPlay.Sealed);
    }

    // R23.5 (A12.3): HIP only by an SSR token, up to its squad-equivalents, a SMC only with a hidden MMC; the enemy never sees a hidden unit.
    [Fact]
    public async Task HipNeedsItsSsrAndStaysUnseen()
    {
        var guards = Card("guards-counterattack");
        var (withoutToken, _) = RussianWithOneHidden(guards);
        Committed(await Place("guards-counterattack", [.. Ob(guards, "german", 0)]));
        Refused(await Place("guards-counterattack", [.. withoutToken]), "play.setup-hidden");
    }

    [Fact]
    public async Task AnSsrGivesHipUpToItsSquadEquivalents()
    {
        var id = GuardsWithHip();
        var card = cards.Read(id, Catalog)!.Card!;
        Committed(await Place(id, [.. Ob(card, "german", 0)]));

        // Two squads hidden are more than the SSR's one squad-equivalent.
        var (one, hidden) = RussianWithOneHidden(card);
        var two = one.Select(item => item).ToList();
        var second = two.FindIndex(item => (string)item["kind"] == "asl:squad" && (string)item["id"] != hidden
            && !two.Any(other => other.ContainsKey("holding") && JsonSerializer.SerializeToElement(other["holding"]).GetProperty("holder").GetString() == (string)item["id"]));
        var squad = two[second];
        two[second] = Unit((string)squad["id"], (string)squad["definition"], JsonSerializer.SerializeToElement(squad["position"]).GetProperty("at").GetString()!, "russian",
            (string)squad["group"], hidden: true);
        Refused(await Place(id, [.. two]), "play.setup-hidden");

        // A leader alone hidden is refused: it hides only with a hidden MMC of its Location (the hidden squad is not in the leader's building).
        var leaderAlone = one.Select(item => (string)item["kind"] == "asl:leader"
            ? Unit((string)item["id"], (string)item["definition"], JsonSerializer.SerializeToElement(item["position"]).GetProperty("at").GetString()!, "russian", (string)item["group"], hidden: true)
            : item).ToList();
        Assert.Contains(leaderAlone, item => (string)item["kind"] == "asl:leader");
        Refused(await Place(id, [.. leaderAlone]), "play.setup-hidden");

        Committed(await Place(id, [.. one]));
        Assert.Contains(ViewAs(Perspective.Adjudicator).Units, unit => unit.Id == hidden);
        Assert.DoesNotContain(ViewAs(German).Units, unit => unit.Id == hidden);
        Assert.Equal(Current.Units.Count(unit => unit.Side == "russian" && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True)
            , ViewAs(German).Units.Count(unit => unit.Side == "russian") + ViewAs(German).Sealed.Count(presence => presence.Side == "russian"));

        // A12.32: a hidden unit is placed beneath "?" before it moves, never during setup; the enemy then sees a "?" and nothing more.
        Refused(await PlaceHidden(hidden), "play.place-hidden");
        Committed(await Advance());
        Refused(await PlaceHidden("german-1-1"), "play.place-hidden");
        Committed(await PlaceHidden(hidden));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit(hidden)!, Conditions.Concealed));
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit(hidden)!, Conditions.Hidden));
        Assert.DoesNotContain(ViewAs(German).Units, unit => unit.Id == hidden);
        Assert.Contains(ViewAs(German).Sealed, presence => presence.Location == Current.Location(hidden)!.Location && !presence.Uninspected);
        Assert.DoesNotContain(History.EventsFor(German, Current.Revision), item => item.Payload is ConditionsChanged changed && changed.Id == hidden);

        // Referee, pass 23: the German list holds no event that created the hidden squad.
        Assert.DoesNotContain(ViewAs(German).Events, item => item.Payload is InstanceCreated { Instance.Id: var created } && created == hidden);
    }

    // R23.5 (referee, pass 23): HIP is built for Infantry only; a vehicle or a Gun set up hidden is refused, and a HIP token names a side of the card.
    [Fact]
    public void OnlyInfantrySetsUpHidden()
    {
        var card = cards.Read(GuardsWithHip("hip:russian:9"), Catalog)!.Card!;
        var at = BoardLocation.Parse("bd01:N4:0");
        SetupCounter Counter(string id, string kind, bool equipment = false) => new(id, "russian", "russian-1", null, kind, at, false, true, false, equipment, true);
        Assert.Contains(ScenarioSetup.Check(card, [Counter("v1", "asl:vehicle")], _ => "stone-building", _ => true, _ => null, 10).Reasons,
            reason => reason.Contains("play.setup-hidden", StringComparison.Ordinal) && reason.Contains("Infantry only", StringComparison.Ordinal));
        Assert.Contains(ScenarioSetup.Check(card, [Counter("gun1", "asl:gun", equipment: true)], _ => "stone-building", _ => true, _ => null, 10).Reasons,
            reason => reason.Contains("Infantry only", StringComparison.Ordinal));

        var guards = Card("guards-counterattack");
        var misnamed = guards with
        {
            Id = "guards-misnamed-hip",
            SpecialRules = [.. guards.SpecialRules, new ScenarioCardRule(guards.SpecialRules.Count + 1, "HIP.", "token", ["hip:soviet:1"], [], null)],
        };
        Assert.Contains(cards.Save(misnamed, Catalog), reason => reason.Contains("names no side", StringComparison.Ordinal));
    }

    // R23.5: a malformed HIP token is refused where the card is checked.
    [Fact]
    public void AMalformedHipTokenIsRefused()
    {
        var guards = Card("guards-counterattack");
        var card = guards with
        {
            Id = "guards-bad-hip",
            SpecialRules = [.. guards.SpecialRules, new ScenarioCardRule(guards.SpecialRules.Count + 1, "HIP.", "token", ["hip:russian:some"], [], null)],
        };
        Assert.Contains(cards.Save(card, Catalog), reason => reason.Contains("play.hip-rule", StringComparison.Ordinal));
        Assert.Equal(1m, ScenarioSetup.HipAllowance(["hip:russian:1", "hip:german:3"], "russian"));
        Assert.Equal(1.5m, ScenarioSetup.HipAllowance(["hip:russian:1.5"], "russian"));
        Assert.Null(ScenarioSetup.HipAllowance(["hip:russian:0"], "russian"));
    }

    // R23.6 (A12.12): no non-OB "?" before both sides have set up, none on a stack within 16 hexes of an unbroken enemy ground unit whose LOS to it is
    // clear or cannot be decided (the fixture board has no LOS data), and none once play has started.
    [Fact]
    public async Task NoNonObQuestionMarkInSightOrBeforeBothHaveSetUp()
    {
        var card = Card("guards-counterattack");
        Committed(await Place("guards-counterattack", [.. Ob(card, "german", 0)]));
        var germanAt = Current.Units.Where(unit => unit.Side == "german").Select(unit => Current.Location(unit.Id)!.Location).Distinct().ToArray();
        Refused(await Conceal("german", germanAt[0].ToString()), "play.non-ob-concealment");

        Committed(await Place("guards-counterattack", [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
        var choices = Planner().NonObConcealment(Current, "german");
        var inSight = germanAt.Where(at => !choices.Contains(at)).ToArray();
        Assert.NotEmpty(inSight);
        Refused(await Conceal("german", inSight[0].ToString()), "play.non-ob-concealment");
        Assert.True(Planner().CardSetup(Current, new HashSet<string>())!.Complete);
        Committed(await Advance());
        Refused(await Conceal("russian", Current.Units.First(unit => unit.Side == "russian").Id), "play.setup-closed");
    }

    private static ScenarioCard Minimal(string id) => new(ScenarioCards.Format, id, "A minimal game", "asl-scenario-a1@1.12.0",
        new ScenarioCardSource("A user card (ruling R22.2).", "none", []), string.Empty, new ScenarioCardDate(0, 7, 0), string.Empty,
        [new ScenarioCardBoard("bd01", 0, 0, false)], "top", null, new ScenarioCardTurns(10, false, "german", "german"), null,
        [new ScenarioCardSide("german", 2, null, new ScenarioCardEdge("bottom", "manufactured", null), string.Empty, [], null, 3),
            new ScenarioCardSide("russian", 0, null, new ScenarioCardEdge(string.Empty, "none", null), string.Empty, [], null, 2)],
        [new ScenarioCardRule(1, "The special rules by token.", "token", ["weather:overcast"], [], null)],
        new ScenarioCardVictory("other", "The players judge the result.", []), null);

    private static Dictionary<string, object> Squad(string id, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:squad",
        ["definition"] = side == "german" ? "attacker-squad" : "defender-squad",
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false },
    };

    // R23.6 (A12.12): a stack 17 hexes or more from every unbroken enemy ground unit takes a non-OB "?"; a stack within 16 hexes and in LOS does not. A
    // minimal card has no OB, so its players place them during setup on the honor system; the "?" is a setup event and play still starts after it.
    [Fact]
    public async Task AFarStackTakesANonObQuestionMark()
    {
        Assert.Empty(cards.Save(Minimal("far-apart"), Catalog));
        Committed(await Place("far-apart", Squad("g1", "bd01:A1:0", "german"), Squad("g2", "bd01:Y1:0", "german"), Squad("r1", "bd01:C1:0", "russian")));
        var choices = Planner().NonObConcealment(Current, "german");
        Assert.Contains(BoardLocation.Parse("bd01:Y1:0"), choices);
        Committed(await Conceal("german", "bd01:Y1:0"));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("g2")!, Conditions.Concealed));
        Assert.Contains("g2", Current.NonObConcealed);
        Assert.False(Current.SetupClosed);
        Assert.Contains(ViewAs(Russian).Sealed, presence => presence.Location == BoardLocation.Parse("bd01:Y1:0") && !presence.Uninspected);
        Refused(await Conceal("german", "bd01:Y1:0"), "play.non-ob-concealment");
        Committed(await Advance());
        Assert.True(Current.SetupClosed);
    }

    // R23.6 (A12.12; referee, pass 23): within 16 hexes, a clear LOS from an unbroken enemy ground unit refuses the "?" and a blocked one allows it; a
    // broken enemy unit or a Dummy does not count; a broken unit never gains "?".
    [Fact]
    public async Task TheNonObQuestionMarkReadsTheLos()
    {
        los = new StubLos();
        Assert.Empty(cards.Save(Minimal("near"), Catalog));
        var brokenGerman = Squad("g2", "bd01:A5:0", "german");
        brokenGerman["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true };
        var brokenRussian = Squad("r2", "bd01:H5:0", "russian");
        brokenRussian["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true };
        var dummy = new Dictionary<string, object>
        {
            ["id"] = "d1",
            ["kind"] = UnitKinds.Dummy,
            ["side"] = "russian",
            ["position"] = new
            {
                at = "bd01:A7:0"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = false },
        };
        Committed(await Place("near", Squad("g1", "bd01:A1:0", "german"), brokenGerman, Squad("r1", "bd01:E1:0", "russian"), brokenRussian, dummy));
        Refused(await Conceal("german", "bd01:A1:0"), "play.non-ob-concealment");
        Assert.DoesNotContain(History.Current!.Units, unit => GameState.Condition(unit, Conditions.Concealed) == ConditionState.True && unit.Side == "german");

        // A12.12: a broken unit never gains "?", whatever the LOS.
        los.Blocked = true;
        Refused(await Conceal("german", "bd01:A5:0"), "play.non-ob-concealment");
        Committed(await Conceal("german", "bd01:A1:0"));

        // A Dummy is a "?" to the enemy whatever its condition says (R23.1).
        Assert.Contains(ViewAs(German).Sealed, presence => presence.Location == BoardLocation.Parse("bd01:A7:0"));
        Assert.DoesNotContain(ViewAs(German).Units, unit => unit.Id == "d1");
    }

    // R23.6: only the unbroken russian squad at E1 sees A1, and a clear LOS from it refuses; with r1 gone from sight the broken r2 does not count.
    [Fact]
    public async Task ABrokenEnemyDoesNotStopTheNonObQuestionMark()
    {
        los = new StubLos();
        Assert.Empty(cards.Save(Minimal("broken-near"), Catalog));
        var brokenRussian = Squad("r2", "bd01:E1:0", "russian");
        brokenRussian["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true };
        Committed(await Place("broken-near", Squad("g1", "bd01:A1:0", "german"), brokenRussian));
        Committed(await Conceal("german", "bd01:A1:0"));
    }

    // R23.4 (A26.15): a side reads Control with the enemy's concealed and hidden units neither gaining nor preventing it; the adjudicator reads it all.
    [Fact]
    public async Task ConcealedUnitsNeedNotDeclareControl()
    {
        var card = Card("guards-counterattack");
        Committed(await Place("guards-counterattack", [.. Ob(card, "german", 0)]));
        Committed(await Place("guards-counterattack", [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
        Committed(await Advance());
        var russian = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad").Id;
        bool InM9(UnitInstance unit) => unit.Side == "german" && Current.Location(unit.Id)?.Location.Hex.ToString() == "M9";
        var states = History.States;
        var last = states[^1];
        var taken = last with
        {
            Units = [.. last.Units.Select(unit => InM9(unit) ? unit with { Status = InstanceStatus.Eliminated }
                : unit.Id == russian ? unit with
                {
                    Position = new MapPosition(BoardLocation.Parse("bd01:M9:0")),
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.Concealed] = ConditionState.True },
                }
                : unit)],
        };
        IReadOnlyList<GameState> played = [.. states, taken];
        string? ControlOf(string? knownTo) => ScenarioVictory.Evaluate(card, played, unit => Planner().VictoryPoints(taken, unit), _ => [], false, knownTo)!
            .Control.Single(item => item.Id == "M9").Side;
        Assert.Equal("russian", ControlOf(null));
        Assert.Equal("russian", ControlOf("russian"));
        Assert.Equal("german", ControlOf("german"));
        Assert.True(ScenarioVictory.UnbrokenSquads(taken, "russian", "german") < ScenarioVictory.UnbrokenSquads(taken, "russian"));
    }
}
