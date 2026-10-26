using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 71, Infantry IV: Snipers and wounds, on the Play page. Increment 4a: the Sniper's record line names the hex attacked in brackets and the target
/// after it, "[hex]: ...", as the page's other lines do, with the new results worded and cited (design section 5, item 3).
/// </summary>
public sealed class PlayPagePass71Tests : IDisposable
{
    // Its own id: UnitNames keeps names by the game's id and first event id.
    private const string Game = "playpage-pass71";

    // CA1861: the German squad that fires in each test.
    private static readonly string[] GermanFirer = ["de-1"];
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-71-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass71Tests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        var boards = new WideBoards(maps);
        live = new LivePlay(library, boards, dice.Roller);
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
        context.Services.AddSingleton(dice);
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Dictionary<string, object> Placed(string id, string kind, string? definition, string at, string side, Dictionary<string, bool> conditions)
    {
        var placed = new Dictionary<string, object>
        {
            ["id"] = id,
            ["kind"] = kind,
            ["side"] = side,
            ["position"] = kind == "asl:vehicle" ? new Dictionary<string, string> { ["at"] = at, ["facing"] = "east" } : new Dictionary<string, string> { ["at"] = at },
            ["conditions"] = conditions,
        };
        if (definition is not null)
        {
            placed["definition"] = definition;
        }

        return placed;
    }

    private static Dictionary<string, object> Squad(string id, string definition, string at, string side) =>
        Placed(id, "asl:squad", definition, at, side, new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false });

    private IReadOnlyList<GameEvent> Events => live.Store.Read(new GameScope(LivePlay.Tenant, Game))?.Events ?? [];

    private async Task Do(LimboDancer.Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        var revision = Events.Count;
        node["gameId"] = Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{revision}";
        node["expectedRevision"] = revision;
        var element = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(action, element, live.Principal);
        var result = proposed.Outcome == PlayOutcome.NeedsConfirmation ? await live.Play.ConfirmAsync(action, element, live.Principal, proposed.Correlation) : proposed;
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
    }

    /// <summary>Increment 4d (A14.2; ruling R71.12): an effective Sniper dr waits for the Sniper player's choice, here answered "attack"; the dice come from the queue.</summary>
    private async Task Attack()
    {
        var choice = live.History(Game)!.Current!.Choice!;
        Assert.Equal(ChoicePending.Sniper, choice.Kind);
        await Do(GameActions.Choose, new
        {
            key = choice.Key,
            option = Rules.ScenarioA1Sniper.Attack,
        });
    }

    /// <summary>A game named after this class on the wide board, the Russian SAN 6, gone on to the German PFPh, where setup has closed.</summary>
    private Task Start(params Dictionary<string, object>[] placements) => Start(null, placements);

    /// <summary>As above, with a German SAN when given.</summary>
    private async Task Start(int? germanSan, params Dictionary<string, object>[] placements)
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = nameof(PlayPagePass71Tests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = "german",
                scenarioMonth = 7,
                scenarioYear = 1942,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3, san = germanSan }, new { id = "russian", nationality = "russian", elr = 3, san = 6 } },
            },
            placements,
        });
        await Do(GameActions.AdvancePhase, new
        {
        });
    }

    [Fact]
    public async Task TheSniperLineNamesTheHexAndTheStunnedCrewsTank()
    {
        // A14.3, D5.34 (ruling R71.5): the German squad's IFT DR of 3 + 3 equals the Russian SAN; the Russian 4-4-7 passes its MC (3 + 3); the Russian Sniper
        // in the fifth row rolls dr 2 and a Random Location DR of 1, 1 (one hex north), onto the CE German tank in the fourth row, whose crew is Stunned.
        // Every view reads the line with the hex in brackets first and the tank after it.
        var tankAt = WideBoards.At(9);
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Placed("de-tk", "asl:vehicle", "attacker-tank", tankAt, "german", new Dictionary<string, bool> { [Conditions.ButtonedUp] = false }),
            Placed("rs", "asl:sniper", null, sniperAt, "russian", new Dictionary<string, bool>()));
        dice.Enqueue([3, 3, 3, 3, 2, 1, 1]);
        await Do(GameActions.Fire, new
        {
            firers = GermanFirer,
            target = WideBoards.At(5),
        });
        await Attack();
        var sniped = Assert.Single(Events.Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(("de-tk", "stunned", tankAt), (sniped.Unit, sniped.Result, sniped.Target?.ToString()));
        foreach (var view in new[] { Perspective.Adjudicator, new Perspective("german"), new Perspective("russian") })
        {
            var history = live.History(Game)!;
            var line = Assert.Single(new PlayRecords(history, view, long.MaxValue, UnitNames.For(history, view, [.. live.Catalogs])).Snipers).Text;
            Assert.StartsWith("The Russian Sniper (", line, StringComparison.Ordinal);
            Assert.Matches(@": dr 2; \[[A-Z]{1,2}\d{1,2}\]: the crew of .+ is Stunned \(A14\.3, D5\.34\)$", line);
            Assert.DoesNotContain("ab-wide:", line, StringComparison.Ordinal);
        }
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    private static Dictionary<string, object> Plain(string id, string kind, string definition, string at, string side) =>
        Placed(id, kind, definition, at, side, new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false });

    [Fact]
    public async Task TheSniperCheckIsTheGermanSidesChoiceAndItsLinesNameTheHex()
    {
        // Increment 4b (A14.4; rulings R71.9, R71.10): as above, the Russian Sniper's dr 2 lands one hex north, now on a German 8-1, 4-6-7, and 2-4-7;
        // Random Selection's 6, 1, 1 picks the HS, which is pinned. The German view offers the Sniper Check with its two answers, the Russian view none. Taken,
        // the DR 3 + 2 - 3 eliminates the Russian Sniper, and the lines say the hex in brackets and the Russian SAN of 5.
        var stackAt = WideBoards.At(9);
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Plain("de-l", "asl:leader", "attacker-leader-8-1", stackAt, "german"), Squad("de-s", "attacker-squad", stackAt, "german"),
            Plain("de-h", "asl:half-squad", "attacker-half-squad", stackAt, "german"), Placed("rs", "asl:sniper", null, sniperAt, "russian", new Dictionary<string, bool>()));
        dice.Enqueue([3, 3, 3, 3, 2, 1, 1, 6, 1, 1]);
        await Do(GameActions.Fire, new
        {
            firers = GermanFirer,
            target = WideBoards.At(5),
        });
        await Attack();
        var choice = live.History(Game)!.Current!.Choice!;
        Assert.Equal((ChoicePending.SniperCheck, "german"), (choice.Kind, choice.Side));

        var german = Open("german");
        Assert.Equal(2, german.FindAll("#play-choice button.choose").Count);
        Assert.Contains("take the Sniper Check", german.Find("#play-choice").TextContent, StringComparison.Ordinal);
        Assert.Empty(Open("russian").FindAll("#play-choice button.choose"));

        dice.Enqueue([3, 2]);
        await Do(GameActions.Choose, new
        {
            key = choice.Key,
            option = ChoicePending.Take,
        });
        var history = live.History(Game)!;
        var view = new Perspective("german");
        var lines = new PlayRecords(history, view, long.MaxValue, UnitNames.For(history, view, [.. live.Catalogs])).Snipers.Select(item => item.Text).ToArray();
        Assert.Contains(lines, line => line.Contains("Sniper Check against the Russian Sniper", StringComparison.Ordinal) && line.Contains("= 2, the Sniper is eliminated", StringComparison.Ordinal)
            && line.Contains(" in [", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.EndsWith("the Russian SAN is now 5 (A14.4)", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("ab-wide:", StringComparison.Ordinal));
    }

    // Increment 4c: the Sniper counter at setup (A14.01, A14.2; ruling R71.11).
    private const string CardId = "sniped";

    // CA1861: the two sides' views.
    private static readonly string[] SideViews = ["german", "russian"];

    private static string HexOf(int column) => BoardLocationOf(column).Hex.ToString();

    private static LimboDancer.Domains.Asl.Maps.Coordinates.BoardLocation BoardLocationOf(int column) => LimboDancer.Domains.Asl.Maps.Coordinates.BoardLocation.Parse(WideBoards.At(column));

    /// <summary>
    /// A user's card on the wide board, both SANs 2: the Germans set up first and move first, two squads in the fourth row's second and third hexes; the
    /// Russians, the DEFENDER, one squad in its tenth hex.
    /// </summary>
    private string SaveCard()
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        // A building area is named by its first hex (ruling R17.8).
        static ScenarioCardGroup Group(string name, int order, string[] hexes, string definition, int count) =>
            new(name, 3, [new ScenarioCardSetup(hexes[0], "building", hexes, null, null, null, null, null, null)], [new ScenarioCardUnit(definition, count, hexes[0])], order);
        static ScenarioCardSide Side(string side, ScenarioCardGroup group) =>
            new(side, 2, null, new ScenarioCardEdge(side == "german" ? "bottom" : "top", "manufactured", null), "None.", [group]);
        var card = new ScenarioCard(ScenarioCards.Format, CardId, "Sniped", $"{catalog.Identity.Catalog}@{catalog.Identity.Version}", new ScenarioCardSource("A test.", "none", []),
            string.Empty, new ScenarioCardDate(1, 7, 1942), string.Empty, [new ScenarioCardBoard(WideBoards.Board.Ref.Value, 0, 0, false)], "top", null,
            new ScenarioCardTurns(5, false, "german", "german"), null,
            [Side("german", Group("The line", 1, [HexOf(2), HexOf(3)], "attacker-squad", 2)), Side("russian", Group("The guard", 2, [HexOf(10)], "defender-squad", 1))], [],
            new ScenarioCardVictory("other", "The players judge the result.", ["A26.1"]), null);
        Assert.Empty(live.Cards.Save(card, catalog));
        return live.Cards.Sha256(CardId)!;
    }

    private static Dictionary<string, object> Grouped(string id, string definition, string at, string side, string group)
    {
        var squad = Squad(id, definition, at, side);
        squad["group"] = group;
        return squad;
    }

    [Fact]
    public async Task TheSetupsSniperBlockOffersTheDefendersHexesFirstAndTheOtherSideSeesOnlyThatItPlaces()
    {
        // A14.2 (ruling R71.11): once both sides have set up, the Russians, the DEFENDER, place their Sniper counter first. Their view offers the hexes, each
        // written "[hex]"; the German view is told only that the Russians place theirs now, with no hex. Proposed from the page, the counter is placed, and the
        // German view then offers its own.
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        var sha256 = SaveCard();
        await Do(GameActions.Setup, new
        {
            start = new Dictionary<string, object?>
            {
                ["label"] = CardId,
                ["catalog"] = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                ["boards"] = new[] { WideBoards.Board.Ref.Value },
                ["firstSide"] = null,
                ["sides"] = Array.Empty<object>(),
                ["scenario"] = new { id = CardId, sha256, title = "Sniped" },
            },
            placements = new[] { Grouped("de-1", "attacker-squad", WideBoards.At(2), "german", "german-1"), Grouped("de-2", "attacker-squad", WideBoards.At(3), "german", "german-1") },
        });
        await Do(GameActions.Setup, new
        {
            placements = new[] { Grouped("ru-1", "defender-squad", WideBoards.At(10), "russian", "russian-1") }
        });

        var russian = Open("russian");
        Assert.Contains("Sniper counter for Russian", russian.Find("#setup-sniper legend").TextContent, StringComparison.Ordinal);
        var options = russian.FindAll("#sniper-hex-russian option").Skip(1).ToArray();
        Assert.NotEmpty(options);
        Assert.All(options, option => Assert.Matches(@"^\[[A-Z]{1,2}\d{1,2}\]$", option.TextContent.Trim()));
        Assert.True(russian.Find("#propose-sniper-russian").HasAttribute("disabled"));
        Assert.Contains("The German side places its own next", russian.Find("#sniper-next").TextContent, StringComparison.Ordinal);

        var german = Open("german");
        Assert.Empty(german.FindAll("#sniper-hex-russian"));
        Assert.Contains("The Russian side places its Sniper counter now (A14.2).", german.Find("#sniper-waiting").TextContent, StringComparison.Ordinal);

        var at = options[0].GetAttribute("value")!;
        russian.Find("#sniper-hex-russian").Change(at);
        russian.Find("#propose-sniper-russian").Click();
        russian.WaitForAssertion(() => Assert.Contains("Confirm to commit", russian.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        russian.Find("#play-confirm").Click();
        russian.WaitForAssertion(() => Assert.Equal(at, live.History(Game)!.Current!.Location("sniper-russian")?.Location.ToString()));
        Assert.NotEmpty(Open("german").FindAll("#sniper-hex-german option"));
    }

    [Fact]
    public async Task AGameStartedWithoutTheRussianSniperCounterSaysOnceThatItMakesNoSniperAttacks()
    {
        // A14.01 (ruling R71.11): a game whose play started without a side's counter is never given one; both views read the one line, which names no unit.
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"));
        foreach (var view in SideViews)
        {
            Assert.Equal("The Russian side has a SAN but no Sniper counter in this game, so it makes no Sniper attacks (A14.01).",
                Assert.Single(Open(view).FindAll("#play-sniper-missing")).TextContent.Trim());
        }
    }

    // Increment 4d: the Sniper player's choices (A14.2; ruling R71.12).

    // CA1861: the two answers to the choice to forfeit, as the Sniper side's view words them.
    private static readonly string[] TheForfeitAnswers = ["Propose: attack: make the Random Location DR (A14.2)", "Propose: forfeit the attack and reposition the Sniper counter (A14.2)"];

    [Fact]
    public async Task TheSnipersChoiceIsTheRussianViewsToAnswerTheGermanViewReadsItAndTheHexesToRepositionToAreOneList()
    {
        // A14.2 (p. 82): the Russian Sniper's dr 2 waits on the Russian side's choice to attack or to forfeit and reposition. The Russian view offers both in
        // words; the German view reads the same choice, with the dr, and has nothing to answer. Forfeited, the hexes offered are many, so they are one list,
        // each "[hex]", and the hex proposed from it takes the counter.
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Placed("rs", "asl:sniper", null, sniperAt, "russian", new Dictionary<string, bool>()));
        dice.Enqueue([3, 3, 3, 3, 2]);
        await Do(GameActions.Fire, new
        {
            firers = GermanFirer,
            target = WideBoards.At(5),
        });
        var choice = live.History(Game)!.Current!.Choice!;
        Assert.Equal((ChoicePending.Sniper, "russian"), (choice.Kind, choice.Side));

        var russian = Open("russian");
        string[] answers = [.. russian.FindAll("#play-choice button.choose").Select(button => button.TextContent.Trim())];
        Assert.Equal(TheForfeitAnswers, answers);
        var german = Open("german");
        Assert.Contains("dr of 2 is effective", german.Find("#play-choice").TextContent, StringComparison.Ordinal);
        Assert.Empty(german.FindAll("#play-choice button.choose"));

        await Do(GameActions.Choose, new
        {
            key = choice.Key,
            option = Rules.ScenarioA1Sniper.Reposition,
        });
        russian = Open("russian");
        var hexes = russian.FindAll("#play-choice-select option").Skip(1).ToArray();
        Assert.True(hexes.Length > 6);
        Assert.All(hexes, option => Assert.Matches(@"^\[[A-Z]{1,2}\d{1,2}\]$", option.TextContent.Trim()));
        Assert.Empty(Open("german").FindAll("#play-choice-select"));

        var to = hexes[0].GetAttribute("value")!;
        russian.Find("#play-choice-select").Change(to);
        russian.Find("#play-choice-propose").Click();
        russian.WaitForAssertion(() => Assert.Contains("Confirm to commit", russian.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        russian.Find("#play-confirm").Click();
        russian.WaitForAssertion(() => Assert.Equal(to, live.History(Game)!.Current!.Location("rs")?.Location.ToString()));
        Assert.Equal(Rules.ScenarioA1Sniper.Forfeited, Assert.Single(Events.Select(item => item.Payload).OfType<SniperAttacked>()).Result);
    }

    // CA1861: the moving German squad.
    private static readonly string[] Mover = ["de-1"];
    private static readonly string[] RussianFirer = ["ru-1"];

    [Fact]
    public async Task AMovementPlanWaitsForTheSnipersChoiceAsTheChoosingSidesTurn()
    {
        // Ruling R5.8 (pass 71, ruling R71.12): the German squad's plan steps beside the Russian 4-4-7, which fires at it: its IFT DR of 3 + 3 equals the German
        // SAN, and the German Sniper's dr 2 waits on the German side's choice. The plan, reviewed, is the German side's turn, and says whose answer it awaits.
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(6, Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Placed("gs", "asl:sniper", null, sniperAt, "german", new Dictionary<string, bool>()));
        await Do(GameActions.AdvancePhase, new
        {
        });
        Assert.Equal("mph", live.History(Game)!.Current!.Phase);
        await live.Plans.Start(new MovementPlan(Game, Mover, [LimboDancer.Domains.Asl.Maps.Coordinates.BoardLocation.Parse(WideBoards.At(4))], false, false, "german", "russian",
            "plan-71"));
        dice.Enqueue([3, 3, 3, 3, 2]);
        await Do(GameActions.Fire, new
        {
            firers = RussianFirer,
            target = WideBoards.At(4),
        });
        Assert.Equal((ChoicePending.Sniper, "german"), (live.History(Game)!.Current!.Choice?.Kind, live.History(Game)!.Current!.Choice?.Side));

        var run = await live.Plans.Review(Game);
        Assert.Equal(MovementPlanTurn.Mover, run.Turn);
        Assert.Equal("The move waits for the German side's answer to the choice before play goes on.", run.Note);
    }

    // Increment 4f: the SMC crew's Stun wound (D5.343, A17.11; ruling R71.15).

    [Fact]
    public async Task TheWoundLineOfASmcCrewsStunNamesHim()
    {
        // D5.343, A17.11 (ruling R71.15): the German 8-1 crews the Abandoned CE tank in the fourth row in the German MPh; de-1 moves to the second hex, and the
        // Russian 4-4-7's First Fire at it (4 + 4) gives it a check, whose 3 + 3 equals the Russian SAN. The Russian Sniper's dr 2 and Random Location DR of
        // 1, 1 find the tank, whose crew is Stunned: his Wound Severity dr of 1 is rolled before the Stun, and its line names him, in every view.
        var tankAt = WideBoards.At(9);
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Placed("de-tk", "asl:vehicle", "attacker-tank", tankAt, "german", new Dictionary<string, bool> { [Conditions.ButtonedUp] = false, [Conditions.Abandoned] = true }),
            Plain("de-l", "asl:leader", "attacker-leader-8-1", tankAt, "german"), Placed("rs", "asl:sniper", null, sniperAt, "russian", new Dictionary<string, bool>()));
        await Do(GameActions.AdvancePhase, new
        {
        });
        await Do(GameActions.RecrewVehicle, new
        {
            unitId = "de-l",
            vehicleId = "de-tk",
        });
        if (live.History(Game)!.Current!.Movement is { WindowOpen: true })
        {
            await Do(GameActions.PassFire, new
            {
            });
        }

        if (live.History(Game)!.Current!.Movement is not null)
        {
            await Do(GameActions.EndMove, new
            {
            });
        }

        await Do(GameActions.Move, new
        {
            unitIds = Mover,
            to = WideBoards.At(2),
        });
        dice.Enqueue([4, 4, 3, 3, 2, 1, 1, 1]);
        await Do(GameActions.Fire, new
        {
            firers = RussianFirer,
            target = WideBoards.At(2),
        });
        if (live.History(Game)!.Current!.Choice is { Kind: ChoicePending.Sniper })
        {
            await Attack();
        }

        Assert.Equal("stunned", Assert.Single(Events.Select(item => item.Payload).OfType<SniperAttacked>()).Result);
        foreach (var view in new[] { Perspective.Adjudicator, new Perspective("german"), new Perspective("russian") })
        {
            var wound = Assert.Single(new PlayRecords(live.History(Game)!, view, long.MaxValue).RallyAndRepair, record => record.Kind == "wound");
            Assert.StartsWith("Wound Severity dr 1 for de-l: a wound, not mortal", wound.Text, StringComparison.Ordinal);
        }
    }
}
