using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 21 of the Scenario Card Games Plan (rulings R21.1 to R21.5): the Victory Conditions of a card evaluated: Control from the setup areas and through
/// play, VP, CVP, and Exit VP, the outcomes in order with Avoidance or a draw, the result recorded at game end or at once, and Infantry leaving the map.
/// Board 01 with its real terrain; Gambit on open stand-in boards 4 and 2.
/// </summary>
public sealed class BacklogPass21Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a721");
    private static readonly GameScope Scope = new(Tenant, "p21");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bs9 = ["bs9"];
    private static readonly string[] Bs9AndB1 = ["bs9", "b1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p21-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Queue<int> dice = new();

    public BacklogPass21Tests() => store = new FileGameStore(root);

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

    /// <summary>An open stand-in board, verified, for Gambit's boards 4 and 2.</summary>
    private static BoardHandle OpenBoard(string board)
    {
        var open = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var center = new LocationFacts(0, open, null);
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, null, null, false, false, false, false, null))];
            return new HexFacts(geometry.NameOf(index), index, 0, false, center, [center], sides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(BoardRef.Parse(board), "synthetic-1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle(), OpenBoard("bd04"), OpenBoard("bd02")]), Vocabulary, [Catalog]);

    /// <summary>The dice the test queues, then 6s: each die is its value.</summary>
    private DiceRoller Roller() => new(_ => (dice.Count > 0 ? dice.Dequeue() : 6) - 1);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: Roller());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private static Dictionary<string, object?> Start(string card, object? balance = null, string? firstSide = null)
    {
        var start = new Dictionary<string, object?>
        {
            ["label"] = card,
            ["catalog"] = "asl-scenario-a1@1.13.0",
            ["boards"] = new[] { "bd01" },
            ["firstSide"] = firstSide,
            ["sides"] = Array.Empty<object>(),
            ["scenario"] = new
            {
                id = card,
                sha256 = ScenarioCards.Sha256(card),
                title = card
            },
        };
        if (balance is not null)
        {
            start["balance"] = balance;
        }

        return start;
    }

    /// <summary>Setup placements; the first carries the start.</summary>
    private async Task<PlayResult> Place(string card, object? balance, params Dictionary<string, object>[] placements) => await PlaceWith(Start(card, balance), placements);

    private async Task<PlayResult> PlaceWith(Dictionary<string, object?> start, params Dictionary<string, object>[] placements)
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
            node["start"] = JsonSerializer.SerializeToNode(start);
        }

        return await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(node));
    }

    private async Task<PlayResult> Act(Abstractions.Actions.ActionDescriptor action, object? arguments = null)
    {
        var node = arguments is null ? new System.Text.Json.Nodes.JsonObject() : JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Advance() => Act(GameActions.AdvancePhase);

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["group"] = group,
        ["position"] = at == "off" ? new Dictionary<string, object> { ["offMap"] = true } : new Dictionary<string, object> { ["at"] = at },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
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

    private async Task SetUpGuards(object? balance = null, params Dictionary<string, object>[] extra)
    {
        var card = Card("guards-counterattack");
        Committed(await Place("guards-counterattack", balance, [.. Ob(card, "german", 0), .. extra]));
        Committed(await Place("guards-counterattack", balance, [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
    }

    private static readonly string[] Squad = ["g0"];

    private static Dictionary<string, object?> GambitStart() => new()
    {
        ["label"] = "gambit",
        ["catalog"] = "asl-scenario-a1@1.13.0",
        ["boards"] = new[] { "bd04" },
        ["sides"] = Array.Empty<object>(),
        ["scenario"] = new
        {
            id = "gambit",
            sha256 = ScenarioCards.Sha256("gambit"),
            title = "gambit"
        },
    };

    /// <summary>The states of a game with a last state changed by <paramref name="change"/>, as the evaluator reads them.</summary>
    private IReadOnlyList<GameState> With(Func<GameState, GameState> change)
    {
        var states = Planner().Replay(store.Read(Scope)!.Events).States;
        return [.. states, change(states[^1])];
    }

    private static GameState Moved(GameState state, string unit, string at) =>
        state with
        {
            Units = [.. state.Units.Select(item => item.Id == unit ? item with { Position = new MapPosition(BoardLocation.Parse(at)) } : item)]
        };

    private static GameState Gone(GameState state, Func<UnitInstance, bool> which, InstanceStatus status = InstanceStatus.Eliminated) =>
        state with
        {
            Units = [.. state.Units.Select(item => which(item) ? item with { Status = status } : item)]
        };

    private static GameState Marked(GameState state, string unit, string condition) =>
        state with
        {
            Units = [.. state.Units.Select(item => item.Id == unit
                ? item with { Conditions = new Dictionary<string, ConditionState>(item.Conditions, StringComparer.Ordinal) { [condition] = ConditionState.True } }
                : item)]
        };

    private VictoryReport Evaluate(string card, IReadOnlyList<GameState> states, bool ended = false) =>
        ScenarioVictory.Evaluate(Card(card), states, unit => Planner().VictoryPoints(states[^1], unit), _ => [], ended)!;

    private static string? ControlOf(VictoryReport report, string id) => report.Control.Single(item => item.Id == id).Side;

    // R21.3: the cards' Victory Conditions in their structured form validate, and a condition naming no building of the card is refused.
    [Fact]
    public void TheCardsVictoryConditionsAreStructured()
    {
        foreach (var name in ScenarioCards.Names)
        {
            var read = ScenarioCards.Read(name, Catalog)!;
            Assert.True(read.IsValid, string.Join("; ", read.Diagnostics));
            Assert.NotNull(read.Card!.VictoryConditions.Outcomes);
        }

        var guards = Card("guards-counterattack");
        var broken = guards with
        {
            VictoryConditions = guards.VictoryConditions with
            {
                Outcomes = [new ScenarioCardOutcome("russian", false, [new ScenarioCardCondition("control-count", "russian", Building: "Z9", AtLeast: 1)])],
            },
        };
        Assert.Contains(ScenarioCards.Validate(broken, Catalog), diagnostic => diagnostic.StartsWith("card.victory", StringComparison.Ordinal));
    }

    // R21.1 (A26.11, A26.14): each side Controls its setup buildings at the start; a Russian squad alone in M9 gains it, and a German unit there prevents it.
    [Fact]
    public async Task ControlStartsFromTheSetupAreasAndChangesInPlay()
    {
        await SetUpGuards();
        Committed(await Advance());
        var start = Planner().Victory(Planner().Replay(store.Read(Scope)!.Events))!;
        Assert.Equal("german", ControlOf(start, "M9"));
        Assert.Equal("russian", ControlOf(start, "F3"));
        var russian = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad").Id;
        bool InM9(UnitInstance unit) => unit.Side == "german" && Current.Location(unit.Id)?.Location.Hex.ToString() == "M9";

        // The German squad and leader leave M9 (eliminated); a Russian squad enters it and gains the building.
        var taken = Evaluate("guards-counterattack", With(state => Moved(Gone(state, InM9), russian, "bd01:M9:0")));
        Assert.Equal("russian", ControlOf(taken, "M9"));

        // A broken German unit still there is an armed enemy unit, so it prevents the gain (A26.11); a Dummy would not (A26.15).
        var german = Current.Units.First(unit => InM9(unit) && unit.Kind == "asl:squad").Id;
        var held = Evaluate("guards-counterattack", With(state => Moved(Marked(Gone(state, unit => InM9(unit) && unit.Id != german), german, Conditions.Broken), russian, "bd01:M9:0")));
        Assert.Equal("german", ControlOf(held, "M9"));

        // A broken Russian squad gains nothing (A.7).
        var brokenRussian = Evaluate("guards-counterattack", With(state => Marked(Moved(Gone(state, InM9), russian, "bd01:M9:0"), russian, Conditions.Broken)));
        Assert.Equal("german", ControlOf(brokenRussian, "M9"));

        // Control stays once gained until the enemy gains it back (A26.1): the Russian squad leaves M9 again.
        var states = With(state => Moved(Gone(state, InM9), russian, "bd01:M9:0"));
        var left = Evaluate("guards-counterattack", [.. states, Moved(states[^1], russian, "bd01:N4:0")]);
        Assert.Equal("russian", ControlOf(left, "M9"));
    }

    // R21.3, R21.4 (A26.3): The Guards Counterattack played to its end with no Control changed: the Russians meet neither condition, so the Germans win.
    [Fact]
    public async Task TheGuardsCounterattackEndsWithTheGermansWinningByAvoidance()
    {
        await SetUpGuards();
        PlayResult? last = null;
        for (var step = 0; step < 200 && Current.Ended is null; step++)
        {
            last = await Advance();
            Committed(last);
        }

        var result = Current.Ended!.Result!;
        Assert.Equal("german", result.Winner);
        Assert.Contains("A26.3", result.Reason, StringComparison.Ordinal);
        Assert.Contains(result.Facts, fact => fact.StartsWith("building M9: german", StringComparison.Ordinal));
        Assert.Contains(last!.Reasons, reason => reason.StartsWith("play.result: german wins", StringComparison.Ordinal));
    }

    // R21.3 (A26.13, A16): the Russians win The Guards Counterattack with three times the Germans' unbroken squad-equivalents.
    [Fact]
    public async Task ThreeTimesTheUnbrokenSquadsWin()
    {
        await SetUpGuards();
        Committed(await Advance());
        var report = Evaluate("guards-counterattack", With(state => Gone(state, unit => unit.Side == "german" && unit.Kind == "asl:squad" && !unit.Id.EndsWith("-1", StringComparison.Ordinal))), ended: true);
        Assert.Equal(1, report.Sides.Single(side => side.Side == "german").UnbrokenSquads);
        Assert.Equal("russian", report.AtEnd.Winner);
        Assert.Contains("times as many", report.AtEnd.Reason, StringComparison.Ordinal);
        Assert.True(report.Sides.Single(side => side.Side == "russian").Cvp >= 24);
    }

    // R21.1, R21.3 (A26.13): The Tractor Works' X3: the Russians Control its nine hexes from the start; a hex in Melee counts for neither; a side alone in X3.
    [Fact]
    public async Task TheTractorWorksCountsTheHexesOfX3()
    {
        await SetUpGuards();
        Committed(await Advance());
        var start = Evaluate("tractor-works", With(state => state), ended: true);
        Assert.Equal("russian", start.AtEnd.Winner);
        Assert.Contains("9 hexes of building X3", start.AtEnd.Reason, StringComparison.Ordinal);

        // Germans take four hexes: the Russians hold five, too few; with no unit in X3 the game is a draw.
        var germans = Current.Units.Where(unit => unit.Side == "german" && unit.Kind == "asl:squad").Take(4).Select(unit => unit.Id).ToArray();
        string[] hexes = ["X2", "X3", "X4", "W4"];
        var taken = With(state => hexes.Select((hex, index) => (hex, index)).Aggregate(state, (current, item) => Moved(current, germans[item.index], $"bd01:{item.hex}:0")));
        var withdrawn = Evaluate("tractor-works", [.. taken, germans.Aggregate(taken[^1], (current, id) => Moved(current, id, "bd01:F5:0"))], ended: true);
        Assert.Equal(4, withdrawn.Control.Count(item => item.Kind == "hex" && item.Side == "german"));
        Assert.Null(withdrawn.AtEnd.Winner);
        Assert.Equal("no Victory Condition holds", withdrawn.AtEnd.Reason);

        // Staying there, the Germans alone have unbroken units in X3 and win; a Russian in Melee in X2 makes X2 neither's and ends that.
        Assert.Equal("german", Evaluate("tractor-works", taken, ended: true).AtEnd.Winner);
        var russian = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad").Id;
        var melee = Evaluate("tractor-works", [.. taken, Marked(Marked(Moved(taken[^1], russian, "bd01:X2:0"), russian, Conditions.Melee), germans[0], Conditions.Melee)], ended: true);
        Assert.Null(melee.AtEnd.Winner);
    }

    // R21.5 (A2.6): a German squad in bd04 A9 leaves the map across the left edge in its MPh; it may not return, and the British gain its CVP (A26.221).
    [Fact]
    public async Task InfantryLeavesTheMapAcrossAnEdge()
    {
        Committed(await PlaceWith(GambitStart(), Unit("b1", "british-elite-squad", "bd04:E5:0", "british", "british-1"),
            Unit("b2", "british-elite-squad", "bd04:E6:0", "british", "british-1"), Unit("l1", "british-leader-9-1", "bd04:E5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:E6:0", "british", "british-1"), Unit("l3", "british-leader-8-0", "bd04:E7:0", "british", "british-1")));
        List<Dictionary<string, object>> german = [.. Enumerable.Range(0, 8).Select(index => Unit($"g{index}", "attacker-elite-squad-5-4-8", index == 0 ? "bd04:A9:0" : $"bd04:{(char)('C' + (index / 2))}9:0", "german", "german-1")),
            Unit("gl1", "attacker-leader-9-2", "bd04:C9:0", "german", "german-1"), Unit("gl2", "attacker-leader-9-1", "bd04:D9:0", "german", "german-1"),
            Unit("gl3", "attacker-leader-8-1", "bd04:E9:0", "german", "german-1")];
        Committed(await PlaceWith(GambitStart(), [.. german]));
        var offBoard = Enumerable.Range(0, 10).Select(index => Unit($"bs{index}", "british-elite-squad", "off", "british", "british-1")).ToList();
        offBoard.AddRange([Weapon("blmg1", "british-lmg", "bs9", "british"), Weapon("blmg2", "british-lmg", "bs8", "british"),
            Weapon("bmtr1", "british-light-mortar", "bs7", "british"), Weapon("bmtr2", "british-light-mortar", "bs6", "british"), Weapon("batr", "british-atr", "bs5", "british")]);
        Committed(await PlaceWith(GambitStart(), [.. offBoard]));

        Refused(await Act(GameActions.Move, new
        {
            unitIds = Squad,
            exit = "left"
        }), "play.exit-phase");
        Committed(await Advance());
        Committed(await Advance());
        Assert.Equal(("german", "mph"), (Current.PhasingSide, Current.Phase));
        Refused(await Act(GameActions.Move, new
        {
            unitIds = Squad,
            exit = "right"
        }), "play.exit-edge");
        var exited = await Act(GameActions.Move, new
        {
            unitIds = Squad,
            exit = "left"
        });
        Committed(exited);
        Assert.Contains(exited.Reasons, reason => reason.StartsWith("play.exit", StringComparison.Ordinal));
        Assert.Equal(InstanceStatus.Exited, Current.Unit("g0")!.Status);
        Assert.Equal(new UnitExit("g0", BoardLocation.Parse("bd04:A9:0"), "left", 1, false), Current.Exits.Single());
        Assert.Null(Current.Movement);
        var report = Planner().Victory(Planner().Replay(store.Read(Scope)!.Events))!;
        Assert.Equal(2, report.Sides.Single(side => side.Side == "british").Cvp);
        Assert.Equal(0, report.Sides.Single(side => side.Side == "german").ExitVp);
    }

    // R21.2, R21.4 (A26.23): Gambit's British win at once with 20 Exit VP off the south edge near 2I1, 2Q1, or 2Y1; broken Personnel and other hexes give none.
    [Fact]
    public async Task GambitsExitVpWinAtOnce()
    {
        Committed(await PlaceWith(GambitStart(), Unit("b1", "british-elite-squad", "bd04:E5:0", "british", "british-1"),
            Unit("b2", "british-elite-squad", "bd04:E6:0", "british", "british-1"), Unit("l1", "british-leader-9-1", "bd04:E5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:E6:0", "british", "british-1"), Unit("l3", "british-leader-8-0", "bd04:E7:0", "british", "british-1")));
        var states = Planner().Replay(store.Read(Scope)!.Events).States;
        GameState Exited(GameState state, IEnumerable<(string Unit, string From, bool Broken)> exits) => exits.Aggregate(state with
        {
            SetupClosed = true
        }, (current, exit) =>
            (Gone(current, unit => unit.Id == exit.Unit, InstanceStatus.Exited) with
            {
                Exits = [.. current.Exits, new UnitExit(exit.Unit, BoardLocation.Parse(exit.From), "bottom", 3, exit.Broken)],
            }));

        // The two squads (4 VP) and three leaders (9-1 is 2 VP, each 8-0 1 VP): 8 Exit VP, too few.
        var few = Exited(states[^1], [("b1", "bd02:I1:0", false), ("b2", "bd02:Q1:0", false), ("l1", "bd02:I1:0", false), ("l2", "bd02:Y1:0", false), ("l3", "bd02:Y1:0", false)]);
        var fewReport = ScenarioVictory.Evaluate(Card("gambit"), [.. states, few], unit => Planner().VictoryPoints(few, unit), _ => [], false)!;
        Assert.Equal(8, fewReport.Sides.Single(side => side.Side == "british").ExitVp);
        Assert.Null(fewReport.Immediate);

        // Six more squads exited from J1, adjacent to I1, make 20; two exited far off give the Germans CVP (A26.221); one of the six broken makes 18 (A26.23).
        var withUnits = few with
        {
            Units = [.. few.Units, .. Enumerable.Range(0, 8).Select(index => few.Units.Single(unit => unit.Id == "b1") with { Id = $"x{index}" })],
        };
        var many = Exited(withUnits, [.. Enumerable.Range(0, 6).Select(index => ($"x{index}", "bd02:J1:0", false)), ("x6", "bd02:A1:0", false), ("x7", "bd02:A1:0", false)]);
        IEnumerable<BoardLocation> Adjacent(BoardLocation at) => at.Hex.ToString() == "I1" ? [BoardLocation.Parse("bd02:J1:0")] : [];
        var manyReport = ScenarioVictory.Evaluate(Card("gambit"), [.. states, many], unit => Planner().VictoryPoints(many, unit), Adjacent, false)!;
        Assert.Equal(20, manyReport.Sides.Single(side => side.Side == "british").ExitVp);
        Assert.Equal(4, manyReport.Sides.Single(side => side.Side == "german").Cvp);
        Assert.Equal("british", manyReport.Immediate!.Winner);
        var brokenOne = Exited(withUnits, [.. Enumerable.Range(0, 6).Select(index => ($"x{index}", "bd02:J1:0", index == 0))]);
        Assert.Null(ScenarioVictory.Evaluate(Card("gambit"), [.. states, brokenOne], unit => Planner().VictoryPoints(brokenOne, unit), Adjacent, false)!.Immediate);
    }

    // R21.2, R21.4 (referee, pass 21): on the real stand-in boards, an exit from 2J1 is adjacent to 2I1 by the map's own geometry; a game ended at once by
    // a Victory Condition replays with its result, and nothing follows it.
    [Fact]
    public async Task AnImmediateEndReplaysWithItsResultAndAdjacencyIsTheMaps()
    {
        Committed(await PlaceWith(GambitStart(), Unit("b1", "british-elite-squad", "bd04:E5:0", "british", "british-1"),
            Unit("b2", "british-elite-squad", "bd04:E6:0", "british", "british-1"), Unit("l1", "british-leader-9-1", "bd04:E5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:E6:0", "british", "british-1"), Unit("l3", "british-leader-8-0", "bd04:E7:0", "british", "british-1")));
        var history = Planner().Replay(store.Read(Scope)!.Events);
        var last = history.States[^1];
        var exited = (last with
        {
            SetupClosed = true,
            Units = [.. last.Units.Select(unit => unit.Id == "b1" ? unit with { Status = InstanceStatus.Exited } : unit)],
        }) with
        {
            Exits = [new UnitExit("b1", BoardLocation.Parse("bd02:J1:0"), "bottom", 2, false)],
        };
        var report = Planner().Victory(new GameHistory(history.Events, [.. history.States, exited], []))!;
        Assert.Equal(2, report.Sides.Single(side => side.Side == "british").ExitVp);

        var result = new GameResult("british", "british has exited 20 Exit VP", ["british: 0 CVP, 20 Exit VP, 1 unbroken squad-equivalents"]);
        var ended = new GameEvent(Scope, "test-end-1", Revision + 1, DateTimeOffset.UtcNow, LiveGames.Source, "game-ended", new GameEnded(1, "victory") { Result = result },
            null, [], null);
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, "gambit", Revision, [ended], events => Planner().Replay(events)).Status);
        Assert.Equal(result.Winner, Current.Ended!.Result!.Winner);
        Assert.Equal(result.Facts, Current.Ended.Result.Facts);
        Refused(await Advance(), "play.game-over");
    }

    // R21.2 (A26.211): VP by unit.
    [Fact]
    public async Task UnitsAreWorthTheirVp()
    {
        await SetUpGuards();
        var state = Current;
        int Vp(string definition) => Planner().VictoryPoints(state, state.Units.First(unit => unit.Definition?.Definition == definition));
        Assert.Equal(2, Vp("attacker-squad"));
        Assert.Equal(3, Vp("attacker-leader-9-2"));
        Assert.Equal(1, Vp("attacker-leader-8-0"));
        Assert.Equal(1, Vp("defender-commissar-9-0"));
    }
}
