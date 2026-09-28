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
/// The backlog pass 13 in live play: the RtPh (ruling R13.3), DM (R13.1), Deployment and Recombining (R13.4), SW transfer, drop, and Recovery
/// (R13.5), dismantling (R13.6), and a captured MG (R13.7). A board 01 grid whose terrain each test draws, fixed dice, and a clear LOS everywhere.
/// </summary>
public sealed class BacklogPass13Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f513");
    private static readonly GameScope Scope = new(Tenant, "pass13");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] Gm = ["gm"];
    private static readonly string[] H1H2 = ["h1", "h2"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass13-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Dictionary<string, string> terrain = new(StringComparer.Ordinal);

    public BacklogPass13Tests() => store = new FileGameStore(root);

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

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 13 test board", new HexFactSet(geometry, "pass13", hexes));
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

        var kind = definition.Contains("half-squad", StringComparison.Ordinal) ? "asl:half-squad" : definition.Contains("leader", StringComparison.Ordinal) ? "asl:leader" : "asl:squad";
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
                ["label"] = "Pass 13",
                ["catalog"] = "asl-scenario-a1@1.9.0",
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

    [Fact]
    public async Task ABrokenUnitInOpenGroundRoutsToTheNearestWoodsAndIsInterdictedOnTheWay()
    {
        // r1 is broken in Open Ground three hexes from g1, which sees it: it must rout (A10.5), and gains DM as the RtPh begins (A10.62).
        terrain["E7"] = "Woods";
        terrain["H8"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken"),
            Unit("r2", "defender-squad", "H8", "russian", "asl:broken"));
        Assert.Equal("rtph", Current.Phase);
        Assert.True(Is(Current.Unit("r1")!, Conditions.DesperationMorale));

        // r2 is in woods out of danger and not under DM, so it may not rout; r1 may not step closer to g1, nor stop short of the woods.
        Refused(await Rout("r2", ["H9"]), "play.rout-not-allowed");
        Refused(await Rout("r1", ["E4"]), "play.rout-step");
        Refused(await Rout("r1", ["E6"]), "play.rout-destination");

        // E6 is Open Ground in g1's LOS and Normal Range: Interdiction, a NMC against broken morale 7; 3+3 passes. The woods in E7 are not Interdicted.
        var before = Revision;
        Committed(await Rout("r1", ["E6", "E7"], Once(3, 3)));
        var interdicted = Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        Assert.Equal((L("E6"), 7, RoutInterdicted.Passed), (interdicted.At.ToString(), interdicted.Morale, interdicted.Result));
        Assert.Equal(L("E7"), Current.Location("r1")!.Location.ToString());
        Refused(await Rout("r1", ["E8"]), "play.rout-unit");

        // In woods out of g1's reach r1 has routed well: the RtPh ends with no elimination.
        before = Revision;
        await Advance();
        Assert.DoesNotContain(Since(before), item => item.Payload is InstanceEliminated);
    }

    [Fact]
    public async Task AFailedInterdictionReducesTheSquadAndItsHalfSquadRoutsOn()
    {
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken"));

        // 5+4 = 9 exceeds broken morale 7: Casualty Reduction (A10.53); the HS goes on to E7.
        Committed(await Do(GameActions.Rout, Once(5, 4), new
        {
            unitId = "r1",
            route = new[] { L("E6"), L("E7") },
            attemptId = "rout"
        }));
        Assert.Equal(InstanceStatus.Consumed, Current.Unit("r1")!.Status);
        var half = Current.Unit("rout-r1")!;
        Assert.Equal(("asl:half-squad", L("E7")), (half.Kind, Current.Location(half.Id)!.Location.ToString()));
    }

    [Fact]
    public async Task LowCrawlEscapesInterdictionAndAUnitThatStaysInTheOpenFailsToRout()
    {
        terrain["E7"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken"),
            Unit("r2", "defender-squad", "G4", "russian", "asl:broken"));

        // Low Crawl is one Location toward the woods, using all six MF, and is never Interdicted (A10.52).
        Refused(await Rout("r1", ["E6", "E7"], lowCrawl: true), "play.rout-route");
        Committed(await Rout("r1", ["E6"], lowCrawl: true));
        Assert.Equal((6, false), (Current.Unit("r1")!.MfSpent, Current.Unit("r1")!.HalfMfSpent));

        // r2 stays in Open Ground in g1's LOS and Normal Range: eliminated for Failure to Rout; r1 Low Crawled, so it stays.
        var before = Revision;
        await Advance();
        Assert.Contains(Since(before), item => item.Payload is InstanceEliminated { Id: "r2" });
        Assert.Equal(InstanceStatus.Active, Current.Unit("r1")!.Status);
    }

    [Fact]
    public async Task AUnitThatRoutedIntoTheOpenWithNoCoverInReachIsNotEliminated()
    {
        // No woods or building within six MF: any legal route (A10.51). r1 passes its Interdiction by g1 in E6 and stays there; having routed, it does
        // not fail to rout (referee, pass 13). The concealed g2 does not Interdict (A10.533).
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("g2", "attacker-squad", "C6", "german", "asl:concealed"),
            Unit("r1", "defender-squad", "E5", "russian", "asl:broken"));
        var before = Revision;
        Committed(await Rout("r1", ["E6"], Once(2, 3)));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        before = Revision;
        await Advance();
        Assert.DoesNotContain(Since(before), item => item.Payload is InstanceEliminated);
    }

    [Fact]
    public async Task ABrokenUnitAdjacentToItsEnemyAtTheEndOfTheRtPhSurrenders()
    {
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E2", "german"), Unit("r1", "defender-squad", "E3", "russian", "asl:broken"));

        // r1 can get away from g1 only through Open Ground that g1 Interdicts, so it surrenders rather than routs (A20.21; referee, pass 13).
        Refused(await Rout("r1", ["E4", "E5"]), "play.rout-surrender");

        // As the RtPh ends it surrenders instead of Failure to Rout, and the phase waits for its captor.
        var result = await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.failure-to-rout-surrender", StringComparison.Ordinal));
        Assert.Equal("rtph", Current.Phase);
        Assert.Equal("r1", Assert.Single(Current.PendingSurrenders).Unit);
    }

    [Fact]
    public async Task AKnownArmedEnemyMovingAdjacentPutsABrokenUnitUnderDm()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E5", "russian", "asl:broken"));
        Assert.False(Is(Current.Unit("r1")!, Conditions.DesperationMorale));

        // g1 moves to E4, ADJACENT to r1 (A10.62; ruling R13.1).
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4")
        }));
        Assert.True(Is(Current.Unit("r1")!, Conditions.DesperationMorale));
    }

    [Fact]
    public async Task DmIsRetainedOutsideWoodsAndBuildingsOnly()
    {
        terrain["E7"] = "Woods";
        await SetupAt(0, "german", Unit("r1", "defender-squad", "E5", "russian", "asl:broken", "asl:dm"), Unit("r2", "defender-squad", "E7", "russian", "asl:broken", "asl:dm"));
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
            retainDm = R2
        }), "play.retain-dm");
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
            retainDm = R1
        }));
        Assert.True(Is(Current.Unit("r1")!, Conditions.DesperationMorale));
        Assert.False(Is(Current.Unit("r2")!, Conditions.DesperationMorale));
    }

    [Fact]
    public async Task ASquadDeploysWithItsLeaderAndGuardsDeployAlone()
    {
        await SetupAt(0, "german", Unit("g1", "attacker-squad", "C3", "german"), Unit("gl", "attacker-leader-8-1", "C3", "german"), Mg("gm", "attacker-lmg", "g1", "german"),
            Unit("g2", "attacker-squad", "C3", "german"), Unit("gl2", "attacker-leader-8-1", "C3", "german"),
            Unit("r1", "defender-guards-squad", "H3", "russian"));

        // A NTC: 3+3 = 6, -1 for the 8-1, is at most morale 7 (A1.31); the LMG goes with the second HS as named.
        Committed(await Do(GameActions.Deploy, Once(3, 3), new
        {
            squadId = "g1",
            leader = "gl",
            secondWeapons = Gm,
            attemptId = "dep"
        }));
        Assert.Equal(InstanceStatus.Consumed, Current.Unit("g1")!.Status);
        Assert.Equal("dep-g1-2", ((EquipmentInstance)Current.Find("gm")!).Holding!.Holder);
        Assert.Equal("asl:half-squad", Current.Unit("dep-g1-1")!.Kind);

        // The leader has directed his Deployment this RPh; a failed NTC leaves the squad whole.
        Refused(await Do(GameActions.Deploy, Once(3, 3), new
        {
            squadId = "g2",
            leader = "gl"
        }), "play.rph-action");
        Committed(await Do(GameActions.Deploy, Once(6, 6), new
        {
            squadId = "g2",
            leader = "gl2"
        }));
        Assert.Equal(InstanceStatus.Active, Current.Unit("g2")!.Status);
        Refused(await Do(GameActions.Deploy, Once(1, 1), new
        {
            squadId = "g2",
            leader = "gl2"
        }), "play.rph-action");

        // Guards need no leader, and take the NTC unmodified: 4+4 = 8 against their morale 8 (A1.31).
        Committed(await Do(GameActions.Deploy, Once(4, 4), new
        {
            squadId = "r1"
        }));
        Assert.Equal(2, Current.Units.Count(unit => unit.Status == InstanceStatus.Active && unit.Side == "russian" && unit.Kind == "asl:half-squad"));
    }

    [Fact]
    public async Task TwoHalfSquadsRecombineWithTheirLeader()
    {
        await SetupAt(0, "german", Unit("h1", "attacker-half-squad", "C3", "german"), Unit("h2", "attacker-half-squad", "C3", "german"),
            Unit("gl", "attacker-leader-8-1", "C3", "german"), Mg("gm", "attacker-lmg", "h2", "german"));
        Refused(await Do(GameActions.Recombine, NoRoll(), new
        {
            halfSquads = H1H2
        }), "play.deploy-leader");
        Committed(await Do(GameActions.Recombine, NoRoll(), new
        {
            halfSquads = H1H2,
            leader = "gl",
            attemptId = "rec"
        }));
        var squad = Current.Unit("rec-h1")!;
        Assert.Equal(("asl:squad", "attacker-squad"), (squad.Kind, squad.Definition!.Definition));
        Assert.Equal("rec-h1", ((EquipmentInstance)Current.Find("gm")!).Holding!.Holder);

        // It was the leader's RPh action, and the new squad's: neither rallies, repairs, or Deploys this RPh (referee, pass 13).
        Assert.Contains("gl", Current.RallyPhaseActions);
        Assert.Contains("rec-h1", Current.RallyPhaseActions);
    }

    [Fact]
    public async Task ASwIsTransferredDroppedAndRecovered()
    {
        await SetupAt(0, "german", Unit("g1", "attacker-squad", "C3", "german"), Unit("g2", "attacker-squad", "C3", "german"), Mg("gm", "attacker-lmg", "g1", "german"));

        // In the RPh stacks are freely rearranged: a transfer is no unit's sole RPh action (A4.431; referee, pass 13).
        Committed(await Do(GameActions.Transfer, NoRoll(), new
        {
            unitId = "g1",
            equipmentId = "gm",
            toUnitId = "g2"
        }));
        Assert.Equal("g2", ((EquipmentInstance)Current.Find("gm")!).Holding!.Holder);
        Assert.Empty(Current.RallyPhaseActions);

        // In the MPh g2 drops it before moving, and g1 Recovers it for one MF on a dr below 6 (A4.43, A4.44).
        await Advance(2);
        Committed(await Do(GameActions.Drop, NoRoll(), new
        {
            unitId = "g2",
            equipmentId = "gm"
        }));
        Assert.Null(((EquipmentInstance)Current.Find("gm")!).Holding);
        Committed(await Do(GameActions.Recover, Once(5), new
        {
            unitId = "g1",
            equipmentId = "gm"
        }));
        Assert.Equal("g1", ((EquipmentInstance)Current.Find("gm")!).Holding!.Holder);
        Assert.Equal(1, Current.Unit("g1")!.MfSpent);
    }

    [Fact]
    public async Task AGermanMmgIsDismantledInAFirePhaseAndThenIsNotFired()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "C3", "german"), Mg("gm", "attacker-mmg", "g1", "german"), Mg("gl", "attacker-lmg", "g1", "german"),
            Unit("r1", "defender-squad", "C5", "russian"));
        Refused(await Do(GameActions.Dismantle, NoRoll(), new
        {
            unitId = "g1",
            equipmentId = "gl"
        }), "play.dismantle-weapon");
        Committed(await Do(GameActions.Dismantle, NoRoll(), new
        {
            unitId = "g1",
            equipmentId = "gm"
        }));
        var mmg = (EquipmentInstance)Current.Find("gm")!;
        Assert.True(Is(mmg, Conditions.Dismantled) && Is(mmg, Conditions.PrepFire));

        // Dismantling was the MG's use this phase, and a dismantled MG is not fired (A9.8).
        Refused(await Do(GameActions.Dismantle, NoRoll(), new
        {
            unitId = "g1",
            equipmentId = "gm"
        }), "play.dismantle-weapon");
        Refused(await Do(GameActions.Fire, NoRoll(), new
        {
            firers = G1,
            weapons = new Dictionary<string, string[]> { ["g1"] = ["gm"] },
            target = L("C5")
        }),
            "play.fire-weapon");
    }

    [Fact]
    public async Task ARussianSquadFiresACapturedGermanLmgWithItsPenalties()
    {
        await SetupAt(1, "russian", Unit("r1", "defender-squad", "E5", "russian"), Mg("gm", "attacker-lmg", "r1", "russian"), Unit("g1", "attacker-squad", "E6", "german"));

        // The LMG's B11 is B9 captured (A21.11): an Original 9 malfunctions it.
        Committed(await Do(GameActions.Fire, Once(4, 5, 3, 3, 3, 3, 3, 3), new
        {
            firers = R1,
            weapons = new Dictionary<string, string[]> { ["r1"] = ["gm"] },
            target = L("E6")
        }));
        Assert.True(Is(Current.Find("gm")!, Conditions.Malfunctioned));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }
}
