using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 35 in live play, the Rout Phase group: a unit that surrenders instead of routing holds up neither the rout order nor the phase's end (task
/// 35.17; A10.5, A20.21), a Disrupted unit stays put (task 35.2; A19.12), and a rout into a concealed unit's Location is repulsed (task 35.4;
/// A10.533). A board 01 grid whose terrain each test draws, fixed dice, and a clear LOS everywhere, as the pass 13 tests have it.
/// </summary>
public sealed class BacklogPass35Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f535");
    private static readonly GameScope Scope = new(Tenant, "pass35");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass35-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Dictionary<string, string> terrain = new(StringComparer.Ordinal);

    public BacklogPass35Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static TerrainType Type(string name) => new()
    {
        Code = (byte)(Math.Abs(name.GetHashCode(StringComparison.Ordinal)) % 250 + 1),
        Name = name,
        Category = name switch
        {
            "Woods" => LosCategory.Woods,
            _ when name.Contains("Building", StringComparison.Ordinal) => LosCategory.Building,
            _ => LosCategory.Open,
        },
    };

    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain a test draws on it.</summary>
    private BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var type = Type(terrain.GetValueOrDefault(text) ?? "Open Ground");
            var center = new LocationFacts(0, type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 35 test board", new HexFactSet(geometry, "pass35", hexes));
    }

    // Clear LOS at the board's true range, with no Hindrance.
    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static string L(string hex) => $"bd01:{hex}:0";

    private static Dictionary<string, object> Unit(string id, string definition, string hex, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        var kind = definition.Contains("half-squad", StringComparison.Ordinal) ? "asl:half-squad" : definition.Contains("leader", StringComparison.Ordinal) || definition.Contains("commissar", StringComparison.Ordinal) ? "asl:leader" : "asl:squad";
        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at = L(hex)
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Mg(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task SetupAt(int advances, string firstSide, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 35",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements,
        })));
        await Advance(advances);
    }

    private async Task Advance(int times = 1)
    {
        for (var index = 0; index < times; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Rout(string unit, string[] route, DiceRoller? roller = null, bool lowCrawl = false) =>
        Do(GameActions.Rout, roller ?? NoRoll(), new
        {
            unitId = unit,
            route = route.Select(L).ToArray(),
            lowCrawl
        });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static Dictionary<string, object> Dummy(string id, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:dummy",
        ["side"] = side,
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    private Task<PlayResult> EndPhaseAs(string side) => Do(GameActions.AdvancePhase, NoRoll(), new
    {
        proposedBy = side
    });

    [Fact]
    public async Task AUnitBoundToSurrenderHoldsUpNeitherTheRoutOrderNorThePhasesEnd()
    {
        // Task 35.17 (A10.5, A20.21), as in the game cd-guards-5. The German side is the ATTACKER. Its broken gb in E3 is ADJACENT to the Russian r1 and can
        // get away only through Open Ground r1 Interdicts, so it surrenders instead of routing. The Russian broken r2 in H5 must rout from g1's sight.
        terrain["H7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "H2", "german"), Unit("gb", "attacker-squad", "E3", "german", "asl:broken"),
            Unit("r1", "defender-squad", "E2", "russian"), Unit("r2", "defender-squad", "H5", "russian", "asl:broken"));
        Assert.Equal("rtph", Current.Phase);
        Refused(await Rout("gb", ["E4", "E5"]), "play.rout-surrender");

        // The German side may not end the phase while the Russian r2 still owes its rout; the Russian side may, since gb owes none.
        Refused(await EndPhaseAs("german"), "play.not-your-action");

        // The DEFENDER's r2 does not wait for gb: it routs, passing its Interdiction in H6.
        Committed(await Rout("r2", ["H6", "H7"], Once(3, 3)));
        Assert.Equal(L("H7"), Current.Location("r2")!.Location.ToString());

        // Either side now ends the phase, and gb surrenders as it ends.
        var result = await EndPhaseAs("russian");
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.failure-to-rout-surrender", StringComparison.Ordinal));
        Assert.Equal("gb", Assert.Single(Current.PendingSurrenders).Unit);
    }

    [Fact]
    public async Task ADisruptedUnitStaysPutUnlessItMustRoutAndNeverLowCrawls()
    {
        // Task 35.2 (A19.12). r2 is Disrupted in Open Ground in g1's sight, with no captor ADJACENT: it must rout, and may not use Low Crawl. (That a
        // Disrupted unit's DM alone gives it no rout is tested in Rules: DM is not kept in cover through the RPh, so this board cannot show it.)
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("r2", "defender-squad", "E5", "russian", "asl:broken", "asl:disrupted"));
        Assert.Equal("rtph", Current.Phase);
        Refused(await Rout("r2", ["E6"], lowCrawl: true), "play.rout-low-crawl");
        Committed(await Rout("r2", ["E6", "E7"], Once(3, 3)));
    }

    [Fact]
    public async Task ARoutIntoAConcealedUnitsLocationIsRepulsedAndTheUnitLosesItsConcealment()
    {
        // Task 35.4 (A10.533). The woods of E7 are r1's nearest cover and hold the concealed g2, which the routing side does not know: the plan says
        // nothing of it. r1 passes its Interdiction in E6, is repulsed from E7, and ends its rout in E6; g2 loses its "?".
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("g2", "attacker-squad", "E7", "german", "asl:concealed"),
            Unit("r1", "defender-squad", "E5", "russian", "asl:broken"));
        var before = Revision;
        Committed(await Rout("r1", ["E6", "E7"], Once(3, 3)));
        var steps = Since(before).Select(item => item.Payload).OfType<RoutStepped>().ToArray();
        Assert.Equal(2, steps.Length);
        Assert.Equal((L("E6"), null), (steps[0].To.ToString(), steps[0].Attempted?.ToString()));
        Assert.Equal((L("E6"), L("E7")), (steps[1].To.ToString(), steps[1].Attempted?.ToString()));
        Assert.Equal(L("E6"), Current.Location("r1")!.Location.ToString());
        Assert.False(Is(Current.Unit("g2")!, Conditions.Concealed));
        Assert.DoesNotContain(Since(before), item => item.Payload is DiceRolled { Purpose: "random-selection" });

        // Its rout is over, and it ends the RtPh ADJACENT to g2, now Known: it surrenders, as a unit that fails to rout beside its captors does.
        Refused(await Rout("r1", ["E5"]), "play.rout-unit");
        var result = await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        });
        Committed(result);
        Assert.Equal("r1", Assert.Single(Current.PendingSurrenders).Unit);

        // The record reads back from the store with its attempted Location.
        Assert.Contains(store.Read(Scope)!.Events, item => item.Payload is RoutStepped { Attempted: not null });
    }

    [Fact]
    public async Task TwoConcealedUnitsDrawByRandomSelectionForTheOneThatRepulsesTheRout()
    {
        // A10.533, A.9: g2 and g3 are concealed in E7. After the Interdiction DR in E6, one dr each: g3's 5 is the higher, so g3 loses its "?" and g2 keeps it.
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("g2", "attacker-squad", "E7", "german", "asl:concealed"),
            Unit("g3", "attacker-squad", "E7", "german", "asl:concealed"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken"));
        var before = Revision;
        Committed(await Rout("r1", ["E6", "E7"], Once(3, 3, 2, 5)));
        var selection = Assert.Single(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "random-selection");
        Assert.Equal([2, 5], selection.Values);
        Assert.True(Is(Current.Unit("g2")!, Conditions.Concealed));
        Assert.False(Is(Current.Unit("g3")!, Conditions.Concealed));
        Assert.Equal(L("E6"), Current.Location("r1")!.Location.ToString());
    }

    [Fact]
    public async Task DummiesAloneAreRemovedAndTheRoutGoesIn()
    {
        // A10.533 names a concealed non-Dummy unit: Dummies alone repulse nothing. They are removed and r1 enters the woods.
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Dummy("gd", "E7", "german"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken"));
        var before = Revision;
        Committed(await Rout("r1", ["E6", "E7"], Once(3, 3)));
        Assert.Contains(Since(before), item => item.Payload is InstanceEliminated { Id: "gd" });
        Assert.DoesNotContain(Since(before), item => item.Payload is RoutStepped { Attempted: not null });
        Assert.Equal(L("E7"), Current.Location("r1")!.Location.ToString());
    }

    [Fact]
    public async Task ACommissarRoutsThroughInterdictionRatherThanSurrender()
    {
        // Task 35.4 (A20.21, A25.22). The broken Commissar rc in E3 is ADJACENT to g1 and can get away only through Open Ground g1 Interdicts. Another
        // unit would surrender instead; a Commissar never does, and risks the Interdiction: two NMC, each passed.
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("rc", "defender-commissar-9-0", "E3", "russian", "asl:broken"));
        Assert.Equal("rtph", Current.Phase);
        var before = Revision;
        Committed(await Rout("rc", ["E4", "E5"], Once(1, 1, 1, 1)));
        Assert.Equal(2, Since(before).Select(item => item.Payload).OfType<RoutInterdicted>().Count());
        Assert.Equal(L("E5"), Current.Location("rc")!.Location.ToString());
    }

    [Fact]
    public async Task ACommissarThatFailsToRoutIsEliminatedAndDoesNotSurrender()
    {
        // A20.21: "If unable to do any of these, they are eliminated rather than surrender."
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("rc", "defender-commissar-9-0", "E3", "russian", "asl:broken"));
        var before = Revision;
        await Advance();
        Assert.Contains(Since(before), item => item.Payload is InstanceEliminated { Id: "rc" });
        Assert.Empty(Current.PendingSurrenders);
    }

    [Fact]
    public async Task AFanaticUnitTakesItsInterdictionCheckAgainstOneMore()
    {
        // Task 35.4 (A10.8): a Fanatic unit's broken Morale Level is one higher. r1's is 7, so its Interdiction NMC is against 8: a DR of 7 passes, where against 7 it would pin (A7.8).
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken", "asl:fanatic"));
        var before = Revision;
        Committed(await Rout("r1", ["E6", "E7"], Once(4, 3)));
        var interdicted = Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        Assert.Equal((8, RoutInterdicted.Passed), (interdicted.Morale, interdicted.Result));
        Assert.Equal(L("E7"), Current.Location("r1")!.Location.ToString());
    }
}
