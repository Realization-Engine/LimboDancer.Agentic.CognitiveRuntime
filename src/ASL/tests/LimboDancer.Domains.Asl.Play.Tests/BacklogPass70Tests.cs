using System.Text.Json;
using System.Text.Json.Nodes;
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
using FireResolution = LimboDancer.Domains.Asl.Rules.FireResolution;
using HeatOfBattleOutcome = LimboDancer.Domains.Asl.Rules.HeatOfBattleOutcome;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 70 in live play, Infantry III. Increment 3a: who sees (A12.14, A12.34; ruling R70.16), an action that costs a "?" (A12.141; ruling R70.17), and
/// the Right of Inspection (A12.16; rulings R70.18, R70.19). Board 01's grid, all Open Ground, fixed dice, and a LOS that is clear at the board's range
/// but between the pairs a test blocks; every read of the state replays the game, so each record is checked by the projector.
/// </summary>
public sealed class BacklogPass70Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f570");
    private static readonly GameScope Scope = new(Tenant, "pass70");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] BothGermans = ["de-1", "de-2"];
    private static readonly string[] TopGerman = ["de-1"];
    private static readonly string[] Transferers = ["de-s", "de-h"];
    private static readonly string[] HiddenState = ["asl:hidden"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass70-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly HashSet<(string, string)> blocked = [];

    public BacklogPass70Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static TerrainType Open() => new()
    {
        Code = 1,
        Name = "Open Ground",
        Category = LosCategory.Open,
    };

    private static TerrainType Woods() => new()
    {
        Code = 2,
        Name = "Woods",
        Category = LosCategory.Woods,
    };

    // Increment 3b: the hexes a test reads as woods.
    private readonly HashSet<string> woods = new(StringComparer.Ordinal);

    // Increment 3b: the scenario year of the game (D6.2: Riders of every nationality from 1943).
    private int year = 1942;

    // Increment 3d: the hexes a test reads as one stone building, and the SSRs of the game.
    private readonly HashSet<string> stone = new(StringComparer.Ordinal);
    private string[] ssrs = [];

    private static TerrainType Stone() => new()
    {
        Code = 3,
        Name = "Stone Building",
        Category = LosCategory.Building,
    };

    /// <summary>Board 01's grid, all Open Ground at level 0 but the hexes a test reads as woods.</summary>
    private BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var type = woods.Contains(text) ? Woods() : stone.Contains(text) ? Stone() : Open();
            var center = new LocationFacts(0, type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 70 test board", new HexFactSet(geometry, "pass70", hexes));
    }

    // Clear LOS at the board's true range, blocked between the pairs of hexes a test names (either way).
    private sealed class PairLos(HashSet<(string, string)> blocked) : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            var shut = blocked.Contains((from.Hex.ToString(), target.Hex.ToString())) || blocked.Contains((target.Hex.ToString(), from.Hex.ToString()));
            return shut ? new(LosStatus.Blocked, true, range, 0, null, string.Empty) : new(LosStatus.Clear, false, range, 0, null, string.Empty);
        }
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new PairLos(blocked));

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History => Planner().Replay(store.Read(Scope)!.Events);

    private GameState Current => History.Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static string L(string hex) => $"bd01:{hex}:0";

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string hex, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

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

    private static Dictionary<string, object> Squad(string id, string hex, string side, params string[] states) =>
        Unit(id, "asl:squad", side == "german" ? "attacker-squad" : "defender-squad", hex, side, states);

    private static Dictionary<string, object> Lmg(string id, string holder) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = "attacker-lmg",
        ["side"] = "german",
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A German infantry Gun manned by its crew, both hidden (A12.34).</summary>
    private static Dictionary<string, object>[] HiddenGun(string hex) =>
    [
        Unit("de-gc", "asl:crew", "attacker-crew", hex, "german", "asl:hidden"),
        new()
        {
            ["id"] = "de-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "attacker-inf-gun",
            ["side"] = "german",
            ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = "east" },
            ["holding"] = new
            {
                holder = "de-gc",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false, ["asl:hidden"] = true },
        },
    ];

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game of the first side against the other, left in the first side's RPh and then advanced the times given (1: its PFPh, 2: its MPh).</summary>
    private async Task SetupAt(int advances, string firstSide, params Dictionary<string, object>[] placements)
    {
        var start = new Dictionary<string, object>
        {
            ["label"] = nameof(BacklogPass70Tests),
            ["catalog"] = $"asl-scenario-a1@{LiveFire.CatalogVersion}",
            ["boards"] = Bd01,
            ["firstSide"] = firstSide,
            ["sides"] = new object[]
            {
                new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
            },
            ["scenarioMonth"] = 7,
            ["scenarioYear"] = year,
        };
        if (ssrs.Length > 0)
        {
            start["specialRules"] = ssrs;
        }

        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start,
            placements,
        })));
        for (var index = 0; index < advances; index++)
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

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(History.HasErrors);

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private GameView ViewAs(string side)
    {
        var history = History;
        return Planner().ViewOf(history, history.Current!.Revision, history.Current.Perspectives.Single(item => item.Name == side));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    [InlineData("asl:broken", true, false)]
    [InlineData("asl:hidden", true, true)]
    public async Task AnUnconcealedGermanStackOutOfEveryRussianGoodOrderUnitsLosShowsItsTopOnly(string? viewer, bool inLos, bool inspected)
    {
        // A12.16 (p. 79; ruling R70.18): after play begins the Russian inspects the German stack in E5 only from the LOS of a Good Order ground unit of
        // his, a hidden one included; a broken one is not Good Order. Out of every such LOS he sees its top counter, the 4-6-7 de-1, and a count for
        // de-2, whose LMG is withheld.
        if (!inLos)
        {
            blocked.Add(("E2", "E5"));
        }

        await SetupAt(1, "german", Squad("de-1", "E5", "german"), Squad("de-2", "E5", "german"), Lmg("de-lmg", "de-2"),
            Squad("ru-1", "E2", "russian", viewer is null ? Array.Empty<string>() : new[] { viewer }));
        Assert.True(Current.SetupClosed);
        var russian = ViewAs("russian");
        string[] germans = [.. russian.Units.Where(unit => unit.Side == "german").Select(unit => unit.Id)];
        if (inspected)
        {
            Assert.Equal(BothGermans, germans);
            Assert.Contains(russian.Equipment, item => item.Id == "de-lmg");
            Assert.DoesNotContain(russian.Sealed, item => item.Side == "german");
        }
        else
        {
            Assert.Equal(TopGerman, germans);
            Assert.DoesNotContain(russian.Equipment, item => item.Id == "de-lmg");
            var sealedOne = Assert.Single(russian.Sealed, item => item.Side == "german");
            Assert.Equal((L("E5"), true, true), (sealedOne.Location.ToString(), sealedOne.Uninspected, sealedOne.OutOfLos));
        }

        // The German sees his own stack in full; a view built without the set (Units, no map) shows it in full as before the pass (ruling R70.19).
        Assert.Equal(BothGermans, ViewAs("german").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        var history = History;
        var plain = GameView.Of(history, history.Current!.Revision, history.Current.Perspectives.Single(item => item.Name == "russian"));
        Assert.Equal(BothGermans, plain.Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        NoReplayErrors();
    }

    [Fact]
    public async Task TheStackIsInspectedInFullOnceARussianUnitSeesIt()
    {
        // A12.16: the same stack, out of ru-1's LOS, is shown in full to the Russian once ru-2 in G2 has a LOS to it. The view is derived, never
        // recorded, so the record is the same either way (ruling R70.19).
        blocked.Add(("E2", "E5"));
        blocked.Add(("G2", "E5"));
        await SetupAt(1, "german", Squad("de-1", "E5", "german"), Squad("de-2", "E5", "german"), Squad("ru-1", "E2", "russian"), Squad("ru-2", "G2", "russian"));
        Assert.Equal(TopGerman, ViewAs("russian").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        blocked.Remove(("G2", "E5"));
        Assert.Equal(BothGermans, ViewAs("russian").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task AHiddenGunThatFiresIsNotRevealedByAHiddenEnemy(bool viewerHidden, bool hiddenAfter)
    {
        // A12.34 (p. 80; rulings R26.5, R70.16): the hidden German Gun in E5 fires at the broken Russian squad in G5 with a colored dr of 5. The nearest
        // Good Order enemy ground unit with a LOS is ru-1 in E2, three hexes away, which reveals it; a hidden ru-1 forces nothing (R35.5), so the Gun keeps
        // HIP. Before the pass a hidden unit was counted here.
        await SetupAt(1, "german", [.. HiddenGun("E5"), Squad("ru-t", "G5", "russian", "asl:broken"), Squad("ru-1", "E2", "russian", viewerHidden ? HiddenState : Array.Empty<string>())]);
        Committed(await Do(GameActions.FireOrdnance, Once(5, 6, 3, 3, 3, 3, 3, 3), new
        {
            gunId = "de-gun",
            target = L("G5"),
            ammunition = "he"
        }));
        Assert.Equal(hiddenAfter, Is(Current.Find("de-gun")!, Conditions.Hidden));
        Assert.Equal(hiddenAfter, Is(Current.Unit("de-gc")!, Conditions.Hidden));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AConcealedMoverIsNotRevealedByAHiddenUnit(bool viewerHidden)
    {
        // A12.14 (p. 77; ruling R70.16, the Loss seers): r1 moves in Open Ground without Assault Movement in the LOS of g1, three hexes away. A Good Order
        // g1 takes its "?"; a hidden one does not.
        await SetupAt(2, "russian", Squad("r1", "E5", "russian", "asl:concealed"), Squad("g1", "E2", "german", viewerHidden ? HiddenState : Array.Empty<string>()));
        Assert.Equal("mph", Current.Phase);
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = R1,
            to = L("E6")
        }));
        Assert.Equal(viewerHidden, Is(Current.Unit("r1")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ATransferCostsBothUnitsTheirQuestionMarkInTheLosOfAGoodOrderEnemy(bool inLos)
    {
        // A12.141 (p. 78; ruling R70.17): a concealed squad passes its LMG to a concealed HS in its RPh; both engage in the transfer, so both lose their
        // "?" in the LOS of the Good Order Russian squad in E2 three hexes away, and keep it out of that LOS.
        if (!inLos)
        {
            blocked.Add(("E2", "E5"));
        }

        await SetupAt(0, "german", Squad("de-s", "E5", "german", "asl:concealed"), Unit("de-h", "asl:half-squad", "attacker-half-squad", "E5", "german", "asl:concealed"),
            Lmg("de-lmg", "de-s"), Squad("ru-1", "E2", "russian"));
        Assert.Equal("rph", Current.Phase);
        var before = Revision;
        var result = await Do(GameActions.Transfer, NoRoll(), new
        {
            unitId = "de-s",
            equipmentId = "de-lmg",
            toUnitId = "de-h"
        });
        Committed(result);
        string[] lost = [.. Since(before).Where(item => item.Type == "concealment-lost").Select(item => ((ConditionsChanged)item.Payload).Id)];
        Assert.Equal(inLos ? Transferers : Array.Empty<string>(), lost);
        Assert.Equal(!inLos, Is(Current.Unit("de-s")!, Conditions.Concealed));
        Assert.Equal(!inLos, Is(Current.Unit("de-h")!, Conditions.Concealed));
        Assert.Equal(inLos, result.Reasons.Any(reason => reason.StartsWith("play.concealment:", StringComparison.Ordinal) && reason.Contains("(A12.141)", StringComparison.Ordinal)));
        NoReplayErrors();
    }

    // Increment 3b: the morale checks made outside an attack (task 70.1, part I; rulings R70.3 to R70.10).

    private static Dictionary<string, object> Vehicle(string id, string definition, string hex, string side, string facing) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = facing },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    /// <summary>A unit aboard a vehicle as a Rider (D6.2).</summary>
    private static Dictionary<string, object> Rider(string id, string kind, string definition, string vehicle)
    {
        var unit = Unit(id, kind, definition, "B10", "german");
        unit["position"] = new Dictionary<string, string> { ["in"] = vehicle, ["role"] = "rider" };
        return unit;
    }

    private IEnumerable<MoraleCheckResolved> ChecksSince(long revision) => Since(revision).Select(item => item.Payload).OfType<MoraleCheckResolved>();

    private static FireResolution ResolutionOf(MoraleCheckResolved check) => check.Resolution.Deserialize<FireResolution>(LiveFire.Json)!;

    [Fact]
    public async Task AnInterdictionNmcsOriginalTwoDrawsHeatOfBattleAndABerserkUnitRoutsNoFurther()
    {
        // A10.53, A15.1 (pp. 67, 83; rulings R70.4, R70.5): the broken 4-4-7 routs from E5 for the woods of E7 and is Interdicted in E6 by the 4-6-7 in E2.
        // Its NMC is an Original 2, which passes and calls for Heat of Battle: 3 + 3, +2 Russian, +1 broken = 9, Berserk, a Known enemy unit in its LOS.
        // It rallies, so its rout ends in E6.
        woods.Add("E7");
        await SetupAt(5, "german", Squad("g1", "E2", "german"), Squad("r1", "E5", "russian", "asl:broken"));
        var before = Revision;
        Committed(await Do(GameActions.Rout, Once(1, 1, 3, 3), new
        {
            unitId = "r1",
            route = new[] { L("E6"), L("E7") },
        }));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        var check = Assert.Single(ChecksSince(before));
        Assert.Equal(("interdiction", L("E6")), (check.Cause, check.Location));
        Assert.Equal(HeatOfBattleOutcome.Berserk, Assert.Single(ResolutionOf(check).Effects).HeatOfBattle!.Result);
        var unit = Current.Unit("r1")!;
        Assert.Equal((true, false, L("E6")), (Is(unit, Conditions.Berserk), Is(unit, Conditions.Broken), Current.Location("r1")!.Location.ToString()));
        NoReplayErrors();
    }

    [Fact]
    public async Task ALeaderLostToInterdictionCausesALlmcOfTheUnitsInHisLocation()
    {
        // A10.2 (p. 65; ruling R70.3): the broken 8-1 routs into E6, where the 4-4-7 r2 stands, and fails his Interdiction NMC (6 + 5 = 11 against 8): a
        // wound, mortal on a 6 (A17.11). His loss calls on r2 for a LLMC with his -1 reversed: 2 + 3 + 1 = 6 against 7, passed.
        woods.Add("E7");
        await SetupAt(5, "german", Squad("g1", "E2", "german"), Unit("rl", "asl:leader", "defender-leader-8-1", "E5", "russian", "asl:broken"), Squad("r2", "E6", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Rout, Once(6, 5, 6, 2, 3), new
        {
            unitId = "rl",
            route = new[] { L("E6"), L("E7") },
        }));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rl")!.Status);
        var check = Assert.Single(ChecksSince(before));
        var effect = Assert.Single(ResolutionOf(check).Effects);
        Assert.Equal(("r2", "LLMC", 6, true), (effect.UnitId, effect.Checks[0].Kind, effect.Checks[0].FinalDr, effect.Checks[0].Passed));
        NoReplayErrors();
    }

    [Fact]
    public async Task ALeaderEliminatedForFailureToRoutCausesALlmcBeforeThePhaseEnds()
    {
        // A10.5, A10.2 (ruling R70.3): the broken 8-1 in E5, in the Open Ground in the LOS and Normal Range of the 4-6-7 in E2, does not rout and is
        // eliminated as the RtPh ends; before the phase changes the 4-4-7 with him takes its LLMC: 4 + 3 + 1 = 8 against 7, broken, within its ELR of 2.
        await SetupAt(5, "german", Squad("g1", "E2", "german"), Unit("rl", "asl:leader", "defender-leader-8-1", "E5", "russian", "asl:broken"), Squad("r2", "E5", "russian"));
        var before = Revision;
        var result = await Do(GameActions.AdvancePhase, Once(4, 3), new
        {
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.leader-loss:", StringComparison.Ordinal));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rl")!.Status);
        var check = Assert.Single(ChecksSince(before));
        Assert.Equal("failure-to-rout", check.Cause);
        Assert.True(Is(Current.Unit("r2")!, Conditions.Broken));
        NoReplayErrors();
    }

    /// <summary>A German PzKpfw IIIH at B10 with its Riders, moved in its MPh into B9, read as woods, so they Bail Out in B10 (D6.21).</summary>
    private async Task<long> BailOutIntoWoods(DiceRoller dice, params Dictionary<string, object>[] riders)
    {
        year = 1943;
        woods.Add("B9");
        await SetupAt(2, "german", [Vehicle("de-tank", "attacker-tank", "B10", "german", "north-east"), .. riders, Squad("ru-s", "H8", "russian")]);
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-tank",
            kind = "start",
        }));
        if (Current.Movement is { WindowOpen: true })
        {
            Committed(await Do(GameActions.PassFire, NoRoll(), new
            {
            }));
        }

        var before = Revision;
        Committed(await Do(GameActions.MoveVehicle, dice, new
        {
            vehicleId = "de-tank",
            kind = "enter",
            to = L("B9"),
        }));
        return before;
    }

    [Fact]
    public async Task ASmcRidersBailOutTakesItsWoundSeverityDrAndAnOlderRecordReplaysAsItWas()
    {
        // D6.24, A10.31, A17.11, A19.13 (ruling R70.9; the crash of brief 3b): the 8-1 Rider's NMC is an Original 12, a Casualty MC 4 beyond his 8 and his
        // side's ELR of 3: he is Replaced by the 8-0, then wounded, his Wound Severity dr of 2 minor, broken beneath the tank in B10. Before the pass the
        // plan threw on the missing dr.
        var before = await BailOutIntoWoods(Once(6, 6, 2, 1, 1), Rider("de-l", "asl:leader", "attacker-leader-8-1", "de-tank"));
        var bailed = Assert.Single(Since(before).Select(item => item.Payload).OfType<RiderBailedOut>());
        Assert.Equal(("reduced", 0, 3), (bailed.Result, bailed.Drm, bailed.Elr));
        var leader = Assert.Single(Current.Units, unit => unit.Kind == "asl:leader" && unit.Status == InstanceStatus.Active);
        Assert.Equal(("attacker-leader-8-0", true, true, L("B10")), (leader.Definition!.Definition, Is(leader, Conditions.Wounded), Is(leader, Conditions.Broken),
            Current.Location(leader.Id)!.Location.ToString()));
        NoReplayErrors();

        // The record without the pass's DRM and ELR, as a Bail Out recorded before it, is checked as before and replays to the same state.
        GameEvent[] older = [.. store.Read(Scope)!.Events.Select(item => item.Payload is RiderBailedOut old ? item with { Payload = old with { Drm = null, Elr = null } } : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.Equal(JsonSerializer.Serialize(Current.Units), JsonSerializer.Serialize(replayed.Current!.Units));
    }

    [Fact]
    public async Task ARiderLeadersDrmReachesTheBailOutNmcOfHisVehiclesRiders()
    {
        // A10.7 EXC, D6.24 (ruling R70.9): the 8-1 Rider passes his NMC (2, 2); the 4-6-7's 4 + 4 - 1 = 7 against 7 pins it rather than breaking it. Its
        // record carries the DRM, which the projector checks; without it the record would say broken.
        var before = await BailOutIntoWoods(Once(2, 2, 4, 4, 1, 1), Rider("de-l", "asl:leader", "attacker-leader-8-1", "de-tank"),
            Rider("de-s", "asl:squad", "attacker-squad", "de-tank"));
        var squad = Since(before).Select(item => item.Payload).OfType<RiderBailedOut>().Single(item => item.Unit == "de-s");
        Assert.Equal(("pinned", -1), (squad.Result, squad.Drm));
        Assert.True(Is(Current.Unit("de-s")!, Conditions.Pinned));
        NoReplayErrors();
        GameEvent[] stripped = [.. store.Read(Scope)!.Events.Select(item => item.Payload is RiderBailedOut old ? item with { Payload = old with { Drm = null, Elr = null } } : item)];
        Assert.True(Planner().Replay(stripped).HasErrors);
    }

    // Increment 3c: Voluntary Break and Voluntary Rout (task 70.1, part II; rulings R70.11, R70.12).

    private static readonly string[] PinnedHalfSquad = ["de-h"];
    private static readonly string[] OtherSquad = ["de-s"];
    private static readonly string[] GermanLeader = ["de-l"];

    [Fact]
    public async Task APinnedHalfSquadBreaksVoluntarilyAndRoutsAsInTheExampleOnPage69()
    {
        // A10.41, A10.62, A7.83 (pp. 66, 68, 58; ruling R70.11; the pinned HS in P8 of the example on p. 69): at the start of the German RtPh the pinned 2-3-7
        // in E5, two hexes from the Russian 4-4-7 in E7 (in its LOS and Normal Range of 4), breaks voluntarily: broken, under DM, its pin gone. It must then
        // rout, and does, to the woods of E4. Once a unit has routed, the 4-6-7 in E6 (ADJACENT to E7) may no longer break voluntarily.
        woods.Add("E4");
        await SetupAt(5, "german", Unit("de-h", "asl:half-squad", "attacker-half-squad", "E5", "german", Conditions.Pinned), Squad("de-s", "E6", "german"), Squad("ru-1", "E7", "russian"));
        Assert.Equal("rtph", Current.Phase);
        var before = Revision;
        var result = await Do(GameActions.VoluntaryBreak, NoRoll(), new
        {
            unitIds = PinnedHalfSquad,
        });
        Committed(result);
        Assert.Equal(PinnedHalfSquad, Assert.Single(Since(before).Select(item => item.Payload).OfType<VoluntaryBreak>()).Units);
        Assert.Contains(result.Reasons, reason => reason.Contains("loses its pin by breaking (A7.83)", StringComparison.Ordinal));
        var half = Current.Unit("de-h")!;
        Assert.Equal((true, true, false), (Is(half, Conditions.Broken), Is(half, Conditions.DesperationMorale), Is(half, Conditions.Pinned)));
        Assert.NotNull(Planner().MustRout(Current, half));
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "de-h",
            route = new[] { L("E4") },
        }));
        Assert.Equal(L("E4"), Current.Location("de-h")!.Location.ToString());
        var late = await Do(GameActions.VoluntaryBreak, NoRoll(), new
        {
            unitIds = OtherSquad,
        });
        Assert.Contains(late.Reasons, reason => reason.Contains("a unit has routed this RtPh (A10.41)", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task AUnitOutOfTheLosOfEveryKnownEnemyMayNotBreakVoluntarily()
    {
        // A10.41: with the LOS from E7 blocked, the HS in E5 is neither in a Known enemy unit's LOS nor ADJACENT to one.
        blocked.Add(("E5", "E7"));
        await SetupAt(5, "german", Unit("de-h", "asl:half-squad", "attacker-half-squad", "E5", "german"), Squad("ru-1", "E7", "russian"));
        var refused = await Do(GameActions.VoluntaryBreak, NoRoll(), new
        {
            unitIds = PinnedHalfSquad,
        });
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.voluntary-break: de-h is neither in the LOS", StringComparison.Ordinal));
        Assert.False(Is(Current.Unit("de-h")!, Conditions.Broken));
    }

    [Fact]
    public async Task ALeadersVoluntaryBreakCallsForALltcOfTheSquadWithHim()
    {
        // A10.2 (p. 65: "Should an unbroken leader break"; ruling R70.11): the 9-1 in E5 breaks voluntarily; the 4-6-7 with him, of lower Morale Level, takes
        // a LLTC with his -1 reversed: 5 + 4 + 1 = 10 against 7, failed, so it is pinned rather than broken.
        await SetupAt(5, "german", Unit("de-l", "asl:leader", "attacker-leader-9-1", "E5", "german"), Squad("de-s", "E5", "german"), Squad("ru-1", "E7", "russian"));
        var before = Revision;
        var result = await Do(GameActions.VoluntaryBreak, Once(5, 4), new
        {
            unitIds = GermanLeader,
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.leader-loss: the break of de-l", StringComparison.Ordinal));
        var check = Assert.Single(ChecksSince(before));
        Assert.Equal("voluntary-break", check.Cause);
        var effect = Assert.Single(ResolutionOf(check).Effects);
        Assert.Equal(("de-s", "LLTC", 10, false), (effect.UnitId, effect.Checks[0].Kind, effect.Checks[0].FinalDr, effect.Checks[0].Passed));
        Assert.True(Is(Current.Unit("de-l")!, Conditions.Broken));
        Assert.Equal((true, false), (Is(Current.Unit("de-s")!, Conditions.Pinned), Is(Current.Unit("de-s")!, Conditions.Broken)));
        NoReplayErrors();
    }

    /// <summary>A Russian RtPh: the broken 4-4-7 r1 and the 8-1 rl in E5, in the LOS of the German 4-6-7 in E2; E7 is woods, E6 Open Ground in g1's range.</summary>
    private async Task VoluntaryRoutGame()
    {
        woods.Add("E7");
        await SetupAt(5, "russian", Squad("g1", "E2", "german"), Squad("r1", "E5", "russian", Conditions.Broken), Unit("rl", "asl:leader", "defender-leader-8-1", "E5", "russian"));
    }

    [Fact]
    public async Task ALeaderRoutsWithABrokenSquadAndHisDrmPassesItsInterdiction()
    {
        // A10.711 (p. 69; ruling R70.12): the 8-1 routs with the broken 4-4-7 by E6 to the woods of E7, taking no NMC himself. Its Interdiction NMC in E6
        // is 3 + 4 - 1 (his leadership) = 6 against 7: passed, where it would have pinned it at 7 without him. Both steps of both units are recorded, his
        // naming the unit he routs with; without that name the projector refuses his steps.
        await VoluntaryRoutGame();
        var before = Revision;
        var result = await Do(GameActions.Rout, Once(3, 4), new
        {
            unitId = "r1",
            route = new[] { L("E6"), L("E7") },
            withLeader = "rl",
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.voluntary-rout: rl routs with r1", StringComparison.Ordinal));
        var interdicted = Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        Assert.Equal(("r1", -1, "rl", RoutInterdicted.Passed), (interdicted.Unit, interdicted.Drm, interdicted.Leader, interdicted.Result));
        RoutStepped[] led = [.. Since(before).Select(item => item.Payload).OfType<RoutStepped>().Where(step => step.Unit == "rl")];
        Assert.Equal(2, led.Length);
        Assert.All(led, step => Assert.Equal("r1", step.With));
        Assert.Equal((L("E7"), L("E7")), (Current.Location("r1")!.Location.ToString(), Current.Location("rl")!.Location.ToString()));
        Assert.False(Is(Current.Unit("rl")!, Conditions.Broken));
        NoReplayErrors();
        GameEvent[] stripped = [.. store.Read(Scope)!.Events.Select(item => item.Payload is RoutStepped { With: not null } step ? item with { Payload = step with { With = null } } : item)];
        Assert.True(Planner().Replay(stripped).HasErrors);
    }

    [Fact]
    public async Task ALeaderRoutingWithASquadThatFailsItsInterdictionIsEliminatedAndTheHalfSquadTakesTheLlmc()
    {
        // A10.711, A10.2 (rulings R70.12, R70.3): the NMC is 5 + 4 - 1 = 8 against 7: the 4-4-7 is Reduced to a broken HS and the 8-1 eliminated. His loss
        // calls on the HS (broken Morale Level 6, lower than his 8) for a LLMC with his -1 reversed: 2 + 2 + 1 = 5, passed; the HS routs on to E7.
        await VoluntaryRoutGame();
        var before = Revision;
        Committed(await Do(GameActions.Rout, Once(5, 4, 2, 2), new
        {
            unitId = "r1",
            route = new[] { L("E6"), L("E7") },
            withLeader = "rl",
        }));
        Assert.Equal(RoutInterdicted.Reduced, Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>()).Result);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rl")!.Status);
        var check = Assert.Single(ChecksSince(before));
        Assert.Equal("interdiction", check.Cause);
        var effect = Assert.Single(ResolutionOf(check).Effects);
        Assert.Equal(("LLMC", 5, true), (effect.Checks[0].Kind, effect.Checks[0].FinalDr, effect.Checks[0].Passed));
        var half = Current.Unit(effect.UnitId)!;
        Assert.Equal(("asl:half-squad", InstanceStatus.Active, L("E7")), (half.Kind, half.Status, Current.Location(half.Id)!.Location.ToString()));
        NoReplayErrors();
    }

    [Fact]
    public async Task ARoutWithNoLeaderIsRecordedAndReplaysAsBeforeThePass()
    {
        // A10.53 (ruling R70.6): with no Voluntary Rout leader the 8-1 in the hex gives no DRM: 3 + 4 = 7 against 7 pins the 4-4-7 in E6. Its records
        // carry neither the leader nor the unit routed with, as every record before the pass.
        await VoluntaryRoutGame();
        var before = Revision;
        Committed(await Do(GameActions.Rout, Once(3, 4), new
        {
            unitId = "r1",
            route = new[] { L("E6"), L("E7") },
        }));
        var interdicted = Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        Assert.Equal((0, (string?)null, RoutInterdicted.Pinned), (interdicted.Drm, interdicted.Leader, interdicted.Result));
        Assert.Equal((L("E6"), L("E5")), (Current.Location("r1")!.Location.ToString(), Current.Location("rl")!.Location.ToString()));
        var written = JsonNode.Parse(GameEventWriter.Write(Scope, store.Read(Scope)!))!["events"]!.AsArray();
        Assert.All(written.Where(item => item!["type"]!.GetValue<string>() is "rout-stepped" or "rout-interdicted"),
            item => Assert.False(item!["payload"]!.AsObject().ContainsKey("with") || item["payload"]!.AsObject().ContainsKey("leader")));
        NoReplayErrors();
    }

    // Increment 3d: rout from a building, Interdiction by vehicles and Guns, Fanaticism by SSR (task 70.2; rulings R70.13 to R70.15).

    /// <summary>
    /// A German RtPh: the broken 4-6-7 de-1 in the Open Ground of E5, the Russian T-34 M41 ru-t in E2 with its hull facing the given hexspine; E7 is woods.
    /// Facing south-east, E5 and E6 lie in its VCA, on the arc's edge (C3.2); facing north-east, they do not.
    /// </summary>
    private async Task TankGame(string facing)
    {
        woods.Add("E7");
        await SetupAt(5, "german", Squad("de-1", "E5", "german", Conditions.Broken), Vehicle("ru-t", "defender-tank", "E2", "russian", facing));
        Assert.Equal("rtph", Current.Phase);
    }

    [Fact]
    public async Task ABrokenSquadInOpenGroundInRangeOfAnEnemyAfvMustRoutAndIsInterdictedByIt()
    {
        // A10.5 "unit/its-SW/Gun", A10.532 (pp. 66, 67; ruling R70.14): the T-34's Normal Range is its weapons' (the BMG's eight, the CMG's twelve, the MA's
        // 16), so the broken 4-6-7 three hexes off in Open Ground must rout. Routing by E6 to the woods of E7, it is Interdicted in E6 by the T-34's BMG,
        // four hexes off through its VCA: 3 + 3 = 6 against 7, passed.
        await TankGame("south-east");
        Assert.Contains("ru-t", Planner().MustRout(Current, Current.Unit("de-1")!), StringComparison.Ordinal);
        var before = Revision;
        var result = await Do(GameActions.Rout, Once(3, 3), new
        {
            unitId = "de-1",
            route = new[] { L("E6"), L("E7") },
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.Contains($"{L("E6")} by ru-t", StringComparison.Ordinal));
        var interdicted = Assert.Single(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        Assert.Equal(("ru-t", RoutInterdicted.Passed), (interdicted.Interdictor, interdicted.Result));
        Assert.Equal(L("E7"), Current.Location("de-1")!.Location.ToString());
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOlderRecordedRoutPastAnEnemyAfvReplaysAsItDid()
    {
        // Ruling R70.14: Interdiction is decided at plan time and recorded; the projector does not read it again. A rout past the T-34 faced away, with no
        // Interdiction, replays the same when the record's T-34 faces the route, as a game recorded before the pass, whose vehicles did not Interdict.
        await TankGame("north-east");
        var before = Revision;
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "de-1",
            route = new[] { L("E6"), L("E7") },
        }));
        Assert.Empty(Since(before).Select(item => item.Payload).OfType<RoutInterdicted>());
        GameEvent[] older = [.. store.Read(Scope)!.Events.Select(item => item.Payload is InstanceCreated { Instance: { Id: "ru-t", Position: MapPosition at } tank }
            ? item with { Payload = new InstanceCreated(tank with { Position = at with { Facing = Units.Documents.UnitFacing.SouthEast } }) } : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.Equal(L("E7"), replayed.Current!.Location("de-1")!.Location.ToString());
        Assert.Equal(Units.Documents.UnitFacing.SouthEast, (replayed.Current.Unit("ru-t")!.Position as MapPosition)!.Facing);
    }

    [Fact]
    public async Task ABrokenUnitRoutsOutOfItsOwnBuildingPastItsOtherHex()
    {
        // A10.51's EXC (p. 67; ruling R70.13): the broken 4-6-7 in E5, a hex of the stone building E5-E6, ADJACENT to the Russian 4-4-7 in E4, must rout. It
        // need not stop in E6, its own building's hex, and routs by it and the street of E7 to the woods of E8; the 4-4-7 has no LOS to E7.
        stone.Add("E5");
        stone.Add("E6");
        woods.Add("E8");
        blocked.Add(("E4", "E7"));
        await SetupAt(5, "german", Squad("de-1", "E5", "german", Conditions.Broken), Squad("ru-1", "E4", "russian"));
        Committed(await Do(GameActions.Rout, NoRoll(), new
        {
            unitId = "de-1",
            route = new[] { L("E6"), L("E7"), L("E8") },
        }));
        Assert.Equal(L("E8"), Current.Location("de-1")!.Location.ToString());
        NoReplayErrors();
    }

    [Fact]
    public async Task AFanaticSsrMakesTheSidesPersonnelFanaticAsTheySetUp()
    {
        // A10.8 (p. 69; ruling R70.15): the SSR fanatic:russian makes the Russian 4-4-7 Fanatic at setup, and the German 4-6-7 not; the condition is written
        // in the unit's setup record, so the replay reads it from there.
        ssrs = ["fanatic:russian"];
        await SetupAt(0, "german", Squad("de-1", "E5", "german"), Squad("ru-1", "E2", "russian"));
        Assert.Equal((true, false), (Is(Current.Unit("ru-1")!, Conditions.Fanatic), Is(Current.Unit("de-1")!, Conditions.Fanatic)));
        Assert.Contains("fanatic:russian", Current.SpecialRules);
        NoReplayErrors();
    }

    // Increment 3e: the owner's and the viewer's choices on a "?", and concealment for a vehicle and an Emplaced Gun (tasks 70.4, 70.5; rulings R70.20 to
    // R70.22).

    private static readonly string[] RemovedUnits = ["de-c", "de-gc", "de-d"];
    private static readonly string[] RevealedByRemoval = ["de-c", "de-gc", "de-gun"];
    private static readonly string[] ConcealedRussian = ["r1"];
    private static readonly string[] OpenGerman = ["de-o"];
    private static readonly string[] RussianSquad = ["ru-1"];
    private static readonly string[] TankAndRider = ["de-tank", "de-r"];
    private static readonly string[] GunAndCrew = ["de-gun", "de-gc"];
    private static readonly string[] GermanOnly = ["german"];

    private static Dictionary<string, object> Dummy(string id, string hex) => new()
    {
        ["id"] = id,
        ["kind"] = UnitKinds.Dummy,
        ["side"] = "german",
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    private IReadOnlyList<GameEvent> EventsOf(string side)
    {
        var history = History;
        return history.EventsFor(history.Current!.Perspectives.Single(item => item.Name == side), history.Current.Revision);
    }

    [Fact]
    public async Task AVoluntaryRemovalShowsAUnitRevealsAHiddenGunWithItsCrewAndRemovesADummy()
    {
        // A12.14 (p. 78; ruling R70.20): "The owning player can voluntarily remove any concealment at any time during his or his opponent's Player Turn".
        // In the German PFPh his concealed 4-6-7 in E5, his hidden crew in E7 with its Gun (A12.2: the Gun with it), and his Dummy in E6 give it up: the
        // squad and the crew are revealed in full (A12.31), the Dummy is removed (A12.11); the events are public. A unit under no "?" is refused.
        blocked.Add(("E2", "E7"));
        await SetupAt(1, "german", [Squad("de-c", "E5", "german", "asl:concealed"), Dummy("de-d", "E6"), .. HiddenGun("E7"), Squad("de-o", "E4", "german"),
            Squad("ru-1", "E2", "russian")]);
        var refused = await Do(GameActions.RemoveConcealment, NoRoll(), new
        {
            unitIds = OpenGerman
        });
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.remove-concealment: the units are units of one side", StringComparison.Ordinal));

        var before = Revision;
        Committed(await Do(GameActions.RemoveConcealment, NoRoll(), new
        {
            unitIds = RemovedUnits
        }));
        Assert.Equal(RevealedByRemoval, Since(before).Where(item => item.Type == "concealment-lost").Select(item => ((ConditionsChanged)item.Payload).Id));
        Assert.Equal("de-d", ((InstanceEliminated)Assert.Single(Since(before), item => item.Type == "instance-eliminated").Payload).Id);
        Assert.All(Since(before), item => Assert.Null(item.Visibility));
        Assert.False(Is(Current.Unit("de-c")!, Conditions.Concealed));
        Assert.False(Is(Current.Unit("de-gc")!, Conditions.Hidden));
        Assert.False(Is(Current.Find("de-gun")!, Conditions.Hidden));
        Assert.Contains(ViewAs("russian").Units, unit => unit.Id == "de-c");
        NoReplayErrors();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AConcealedViewerMayDeclineToForceAMoversLossAndTheMoverLearnsNothing(bool force)
    {
        // A12.14 (p. 78; ruling R70.21): the concealed r1 moves in Open Ground without Assault Movement in the LOS of g1, three hexes away, the only Good
        // Order German ground unit in its LOS, itself concealed. The German may force the loss, g1 forfeiting its "?" for a moment, shown to the Russian
        // only; or decline it, r1 keeping its "?". The choice and the answer are the German's records alone.
        await SetupAt(2, "russian", Squad("r1", "E5", "russian", "asl:concealed"), Squad("g1", "E2", "german", "asl:concealed"));
        var before = Revision;
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = ConcealedRussian,
            to = L("E6")
        }));
        Assert.Equal((ChoicePending.ConcealmentLoss, "german"), (Current.Choice!.Kind, Current.Choice.Side));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.DoesNotContain(EventsOf("russian"), item => item.Payload is ChoicePending);
        Assert.Contains(EventsOf("german"), item => item.Payload is ChoicePending);

        Committed(await Do(GameActions.Choose, NoRoll(), new
        {
            key = Current.Choice.Key,
            option = force ? ChoicePending.Take : ChoicePending.Decline
        }));
        Assert.Null(Current.Choice);
        Assert.Equal(!force, Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Concealed));
        Assert.DoesNotContain(EventsOf("russian"), item => item.Payload is ChoiceMade);
        Assert.Equal(force, EventsOf("russian").Any(item => item.Payload is ConcealmentVerified { Units: ["g1"] }));
        Assert.DoesNotContain(EventsOf("german"), item => item.Payload is ConcealmentVerified);
        Assert.Equal(force, Since(before).Any(item => item.Type == "concealment-lost"));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnUnconcealedViewerForcesTheLossWithNoChoice()
    {
        // A12.14: with an unconcealed Good Order German in LOS within 16, the loss is forced as before (the option is the concealed viewer's only).
        await SetupAt(2, "russian", Squad("r1", "E5", "russian", "asl:concealed"), Squad("g1", "E2", "german", "asl:concealed"), Squad("g2", "E3", "german"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = ConcealedRussian,
            to = L("E6")
        }));
        Assert.Null(Current.Choice);
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleAndItsRiderGainQuestionMarkAsItsPlayerTurnEnds()
    {
        // The Concealment Table (A12.121; ruling R70.22), Case K: the German tank in the Open Ground of E5, out of the LOS of the Russian squad in E2 but
        // within 16 hexes of it, makes a Concealment dr: 1 + 4 (US# of a 5/8" counter, A1.6) = 5, and gains "?" with its Rider (A12.2), which no longer
        // checks on its own.
        year = 1943;
        blocked.Add(("E2", "E5"));
        await SetupAt(7, "german", Vehicle("de-tank", "attacker-tank", "E5", "german", "east"), Rider("de-r", "asl:squad", "attacker-squad", "de-tank"),
            Squad("ru-1", "E2", "russian"));
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.AdvancePhase, Once(1), new
        {
        }));
        string[] gained = [.. Since(before).Where(item => item.Type == "concealment-gained").Select(item => ((ConditionsChanged)item.Payload).Id)];
        Assert.Equal(TankAndRider, gained);
        Assert.Single(Since(before), item => item.Payload is DiceRolled { Purpose: "concealment" });
        Assert.True(Is(Current.Unit("de-tank")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Fact]
    public async Task AHiddenGunOutOfConcealmentTerrainIsPlacedBeneathQuestionMarkOnceAnEnemySeesIt()
    {
        // A12.34 (p. 80; ruling R70.22): the hidden German Gun and crew in the Open Ground of E5 are out of the LOS of the Russian squad in H8 as the phases
        // pass; when the squad steps to H7, which sees E5, both are placed beneath "?", a record the German alone reads: the Russian sees a "?" appear.
        blocked.Add(("H8", "E5"));
        await SetupAt(2, "russian", [.. HiddenGun("E5"), Squad("ru-1", "H8", "russian")]);
        Assert.True(Is(Current.Find("de-gun")!, Conditions.Hidden));
        var before = Revision;
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = RussianSquad,
            to = L("H7")
        }));
        var placed = Since(before).Where(item => item.Type == "hidden-placed").ToArray();
        Assert.Equal(GunAndCrew, placed.Select(item => ((ConditionsChanged)item.Payload).Id));
        Assert.All(placed, item => Assert.Equal(GermanOnly, item.Visibility!));
        Assert.True(Is(Current.Find("de-gun")!, Conditions.Concealed));
        Assert.False(Is(Current.Unit("de-gc")!, Conditions.Hidden));
        Assert.Contains(ViewAs("russian").Sealed, item => item.Location.ToString() == L("E5"));
        NoReplayErrors();
    }

    [Fact]
    public void AnOffBoardReinforcementWaitsConcealedWhateverItsOb()
    {
        // A12.12 (p. 77; ruling R70.35, lifting R20.5's refusal): Gambit's British, whose OB gives no "?", may wait off board under a non-OB "?", the two
        // squads of the entry area one stack under one "?"; a Dummy off board is still refused.
        var gambit = ScenarioCards.Read("gambit", Catalog)!.Card!;
        Assert.Contains(gambit.Sides.Single(side => side.Side == "british").Groups, group => (group.Dummies ?? 0) == 0);
        SetupCounter OffBoard(string id, string kind = "asl:squad", string? definition = "british-elite-squad") =>
            new(id, "british", "british-1", definition, kind, null, true, false, kind == UnitKinds.Dummy, false, true) { OffBoard = true };
        IEnumerable<string> Reasons(params SetupCounter[] counters) => ScenarioSetup.Check(gambit, counters, _ => "open-ground", _ => true, Rules.ScenarioA1FireReference.HalfSquadOf,
            gambit.Date.Month).Reasons.Where(reason => reason.StartsWith("play.setup-offboard", StringComparison.Ordinal) || reason.StartsWith("play.setup-concealment", StringComparison.Ordinal));
        Assert.Empty(Reasons(OffBoard("bs1"), OffBoard("bs2")));
        Assert.Contains(Reasons(OffBoard("bd1", UnitKinds.Dummy, null)), reason => reason.StartsWith("play.setup-offboard: bd1", StringComparison.Ordinal));
    }
}
