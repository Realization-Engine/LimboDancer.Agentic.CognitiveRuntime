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

    // Increment 3f: the hexsides of a woods hex a test reads as Open Ground, for Bypass (A4.3).
    private readonly HashSet<(string Hex, HexsideDirection Side)> openSides = [];

    // Increment 3d: the hexes a test reads as one stone building, and the SSRs of the game.
    private readonly HashSet<string> stone = new(StringComparer.Ordinal);
    private string[] ssrs = [];

    private static TerrainType Stone() => new()
    {
        Code = 3,
        Name = "Stone Building",
        Category = LosCategory.Building,
    };

    // Increment 3g: the hexsides a test reads as a Rowhouse wall (B23.71), which divide one building from the next.
    private readonly HashSet<(string Hex, HexsideDirection Side)> rowhouseWalls = [];

    private static TerrainType RowhouseWall() => new()
    {
        Code = 4,
        Name = "Rowhouse Wall",
        Category = LosCategory.Building,
    };

    // Increment 3j: the hexsides a test reads as a road crossing them (A11.8).
    private readonly HashSet<(string Hex, HexsideDirection Side)> roadSides = [];

    private static TerrainType Road() => new()
    {
        Code = 5,
        Name = "Dirt Road",
        Category = LosCategory.Road,
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
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true,
                roadSides.Contains((text, side)) ? Road() : openSides.Contains((text, side)) ? Open() : type,
                rowhouseWalls.Contains((text, side)) ? RowhouseWall() : null, false, false, false, false, null))];
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

    [Fact]
    public async Task ACaseReadShowsAStackItMayNotInspectAsTheViewDoes()
    {
        // A12.16 (ruling R70.18; the referee's finding at the reviews): a Russian case read at the German stack in E5, out of ru-1's LOS, built through the
        // planner's entry, holds its top counter and a count, as the Russian view does; without the entry it holds both, as before the pass.
        blocked.Add(("E2", "E5"));
        await SetupAt(1, "german", Squad("de-1", "E5", "german"), Squad("de-2", "E5", "german"), Squad("ru-1", "E2", "russian"));
        var history = History;
        var request = new CaseRequest(Scope, "scenario-a1", "ru-1", BoardLocation.Parse(L("E5")), history.Current!.Revision, new Perspective("russian"));
        var planner = Planner();
        var read = new CaseReader(new HistoryGameSource([history]), new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], planner.ViewOf).Read(request);
        Assert.Equal(TopGerman, read.Snapshot!.Occupancy.Units.Select(unit => unit.Id));
        Assert.Contains(read.Snapshot.Occupancy.Sealed, item => item.Side == "german");
        var plain = new CaseReader(new HistoryGameSource([history]), new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog]).Read(request);
        Assert.Equal(BothGermans, plain.Snapshot!.Occupancy.Units.Select(unit => unit.Id));
    }

    [Fact]
    public async Task ARussianDummyInTheStacksLocationInspectsNothing()
    {
        // Ruling R70.40 (the referee's finding at the reviews): a Dummy is no unit (A12.11) and takes no part in CC, so a Russian Dummy in E5 with the German
        // stack, out of ru-1's LOS, does not open it to the Russian: he sees its top only.
        blocked.Add(("E2", "E5"));
        await SetupAt(1, "german", Squad("de-1", "E5", "german"), Squad("de-2", "E5", "german"), Dummy("ru-d", "E5", "russian"), Squad("ru-1", "E2", "russian"));
        Assert.Equal(TopGerman, ViewAs("russian").Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
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

    private static readonly string[] SquadChecked = ["r2"];

    [Fact]
    public async Task AConcealedUnitThatTakesNoLlmcIsNotNamedInThePublicRecord()
    {
        // A12.14, A12.3 (the referee's findings at the reviews): beside the 4-4-7 r2, the concealed 6-2-8 rc (Morale Level 8, not lower than the lost 8-1's)
        // takes no LLMC, so the record both sides read names r2 alone, and its dice are as public as the record.
        await SetupAt(5, "german", Squad("g1", "E2", "german"), Unit("rl", "asl:leader", "defender-leader-8-1", "E5", "russian", "asl:broken"), Squad("r2", "E5", "russian"),
            Unit("rc", "asl:squad", "defender-elite-squad", "E5", "russian", "asl:concealed"));
        var before = Revision;
        Committed(await Do(GameActions.AdvancePhase, Once(4, 3), new
        {
        }));
        var check = Assert.Single(ChecksSince(before));
        Assert.Equal(SquadChecked, check.Units);
        Assert.Null(Since(before).Single(item => item.Payload is MoraleCheckResolved).Visibility);
        Assert.All(Since(before).Where(item => item.Payload is DiceRolled), item => Assert.Null(item.Visibility));
        Assert.True(Is(Current.Unit("rc")!, Conditions.Concealed));
        NoReplayErrors();
    }

    [Fact]
    public async Task ALeaderLostWithOnlyAConcealedUnitBesideHimStillRollsAndNamesNoVerdict()
    {
        // A12.14 (the referee's finding at the reviews): either side may end the RtPh, so the plan rolls for the LLMC whenever a leader is lost, and its line
        // says only that one may be due; beside the lost 8-1 the concealed 6-2-8 takes none, and nothing is recorded of it.
        await SetupAt(5, "german", Squad("g1", "E2", "german"), Unit("rl", "asl:leader", "defender-leader-8-1", "E5", "russian", "asl:broken"),
            Unit("rc", "asl:squad", "defender-elite-squad", "E5", "russian", "asl:concealed"));
        var before = Revision;
        var result = await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.leader-loss: the loss of rl may call for a LLMC", StringComparison.Ordinal));
        Assert.Empty(ChecksSince(before));
        Assert.True(Is(Current.Unit("rc")!, Conditions.Concealed));
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
        // calls on the HS (broken Morale Level 6, lower than his 8) for a LLMC with his -1 reversed: 2 + 2 + 1 = 5, passed; the HS routs on to E7. The record's
        // cause is A10.711's, the failed NMC of the unit he routed with (the table player's finding at the reviews).
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
        Assert.Equal("voluntary-rout", check.Cause);
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
    public async Task ABrokenUnitWhoseOnlyCoverInReachIsItsOwnBuildingIsToldItMayIgnoreIt()
    {
        // A10.51 EXC (p. 67) and the example on p. 69, S3 (the referee's finding at the reviews): the broken 4-6-7 in E5, of the stone building E5-E6, ADJACENT
        // to the Russian 4-4-7 in E4, must rout; E6 is the only cover within its reach, and of its own building, so nothing binds it and E6 is named as
        // ignorable: any legal route will do, one ending in E6 included.
        stone.Add("E5");
        stone.Add("E6");
        await SetupAt(5, "german", Squad("de-1", "E5", "german", Conditions.Broken), Squad("ru-1", "E4", "russian"));
        var unit = Current.Unit("de-1")!;
        var (targets, canRout, _) = Planner().RoutAdvice(Current, unit);
        Assert.Equal((0, true), (targets.Count, canRout));
        Assert.Equal(L("E6"), Assert.Single(Planner().RoutIgnoredCovers(Current, unit)).ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnUnarmedEnemyAllowsAVoluntaryBreakOnlyWhenAdjacent(bool adjacent)
    {
        // A10.41 (p. 66; ruling R70.43, at the fixes): "ADJACENT to any unbroken enemy ground unit" asks for no arms; the LOS and Normal Range clause asks
        // for an armed one. The Russian Unarmed 4-4-7 in E6 lets the HS in E5 break; in E7, in its LOS two hexes off, it does not.
        await SetupAt(5, "german", Unit("de-h", "asl:half-squad", "attacker-half-squad", "E5", "german"), Squad("ru-1", adjacent ? "E6" : "E7", "russian", Conditions.Unarmed));
        var result = await Do(GameActions.VoluntaryBreak, NoRoll(), new
        {
            unitIds = PinnedHalfSquad,
        });
        Assert.Equal(adjacent, result.Outcome == PlayOutcome.Committed);
        Assert.Equal(adjacent, Is(Current.Unit("de-h")!, Conditions.Broken));
        if (!adjacent)
        {
            Assert.Contains(result.Reasons, reason => reason.EndsWith("does not count (ruling R70.43)", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task AnUnarmedSquadThatFailsItsLlmcIsCasualtyReducedNotBroken()
    {
        // A20.54 (p. 88; ruling R70.47, at the fixes): the broken 8-1 in E5 is eliminated for Failure to Rout as the RtPh ends; the Unarmed 4-4-7 with him
        // takes its LLMC, 4 + 3 + 1 = 8 against 7, and fails: it is Casualty Reduced to a HS, which is not broken.
        await SetupAt(5, "german", Squad("g1", "E2", "german"), Unit("rl", "asl:leader", "defender-leader-8-1", "E5", "russian", "asl:broken"),
            Squad("r2", "E5", "russian", Conditions.Unarmed));
        var before = Revision;
        Committed(await Do(GameActions.AdvancePhase, Once(4, 3), new
        {
        }));
        var effect = Assert.Single(ResolutionOf(Assert.Single(ChecksSince(before))).Effects);
        Assert.Equal(("r2", "casualty-reduced", false), (effect.UnitId, effect.Checks[0].Consequence, effect.Broken));
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("r2")!.Status);
        var half = Current.Units.Single(unit => unit.Kind == "asl:half-squad" && unit.Status == InstanceStatus.Active && unit.Side == "russian");
        Assert.False(Is(half, Conditions.Broken));
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

    private static Dictionary<string, object> Dummy(string id, string hex, string side = "german") => new()
    {
        ["id"] = id,
        ["kind"] = UnitKinds.Dummy,
        ["side"] = side,
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

        // The referee's finding at the reviews: the mover's next action meets the open window's refusal, code and words, so it tells him nothing of the
        // choice; the viewing side's own reads R5.8's.
        var waiting = await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = ConcealedRussian,
            to = L("E7"),
            proposedBy = "russian",
        });
        Assert.Contains("play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)", waiting.Reasons);
        Assert.DoesNotContain(waiting.Reasons, reason => reason.Contains("choice", StringComparison.Ordinal) || reason.Contains("answer", StringComparison.Ordinal));
        var viewing = await Do(GameActions.PassFire, NoRoll(), new
        {
            proposedBy = "german",
        });
        Assert.Contains(viewing.Reasons, reason => reason.StartsWith("play.choice-pending: the german side answers first", StringComparison.Ordinal));

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
            new(id, "british", "british-1", definition, kind, null, true, false, kind == UnitKinds.Dummy, false, true)
            {
                OffBoard = true
            };
        IEnumerable<string> Reasons(params SetupCounter[] counters) => ScenarioSetup.Check(gambit, counters, _ => "open-ground", _ => true, Rules.ScenarioA1FireReference.HalfSquadOf,
            gambit.Date.Month).Reasons.Where(reason => reason.StartsWith("play.setup-offboard", StringComparison.Ordinal) || reason.StartsWith("play.setup-concealment", StringComparison.Ordinal));
        Assert.Empty(Reasons(OffBoard("bs1"), OffBoard("bs2")));
        Assert.Contains(Reasons(OffBoard("bd1", UnitKinds.Dummy, null)), reason => reason.StartsWith("play.setup-offboard: bd1", StringComparison.Ordinal));
    }

    // Increment 3f: Bypass and detection (task 70.8; ruling R70.23).

    private static readonly string[] GermanSquad = ["g1"];
    private static readonly string[] BypassLane = ["southeast", "northeast"];
    private static readonly string[] BothRussians = ["r1", "r2"];
    private static readonly string[] ThirdRussian = ["r3"];

    private static string N(string hex, HexsideDirection side) => Board01Fixture.Handle().Neighbor(HexName.Parse(hex), side)!.ToString()!;

    /// <summary>D4 is woods but along its southeast and northeast hexsides (A4.3): a stack from D5 Bypasses it along them and leaves north, to D3 (A4.31).</summary>
    private void BypassedWoods()
    {
        woods.Add("D4");
        openSides.Add(("D4", HexsideDirection.SouthEast));
        openSides.Add(("D4", HexsideDirection.NorthEast));
    }

    private Task<PlayResult> MoveG1(string hex, bool bypass = false, bool minimumMove = false)
    {
        var arguments = new Dictionary<string, object> { ["unitIds"] = GermanSquad, ["to"] = L(hex) };
        if (bypass)
        {
            arguments["bypass"] = BypassLane;
        }

        if (minimumMove)
        {
            arguments["minimumMove"] = true;
        }

        return Do(GameActions.Move, NoRoll(), arguments);
    }

    private async Task PassFire() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private MovementStepped LastStep(long before) => Since(before).Select(item => item.Payload).OfType<MovementStepped>().Last();

    private static IEnumerable<string> DrmNames(FireResolved record) =>
        record.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray().Select(item => item.GetProperty("name").GetString()!);

    private IEnumerable<string> RevealedSince(long before) => Since(before).Where(item => item.Type == "concealment-lost").Select(item => ((ConditionsChanged)item.Payload).Id);

    [Fact]
    public async Task AMinimumMoveIntoAConcealedSquadIsForcedBackAndEndsItsMphUnpinned()
    {
        // A12.15 (p. 78; ruling R70.23): the German 4-6-7's Minimum Move into the woods of E4 meets the concealed Russian squad: it is revealed and the
        // squad stays in E5, its MF spent and its MPh over. A4.134 (p. 48): it entered no hex, so it is neither pinned nor CX.
        woods.Add("E4");
        await SetupAt(2, "german", Squad("g1", "E5", "german"), Squad("r1", "E4", "russian", Conditions.Concealed));
        var before = Revision;
        Committed(await MoveG1("E4", minimumMove: true));
        var step = LastStep(before);
        Assert.Equal((L("E5"), L("E4"), true), (step.To.ToString(), step.Attempted?.ToString(), step.MinimumMove));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        await PassFire();
        var squad = Current.Unit("g1")!;
        Assert.Equal((true, false, false, L("E5")), (squad.MovementEnded, Is(squad, Conditions.Pinned), Is(squad, Conditions.Cx), Current.Location("g1")!.Location.ToString()));
        NoReplayErrors();
    }

    [Fact]
    public async Task AStackForcedBackFromItsBypassExitStaysInItsLaneAndEveryQuestionMarkThereIsLost()
    {
        // A12.151 (p. 79; ruling R70.23): the German squad Bypasses D4 along its southeast and northeast hexsides, past the concealed Russian squad in the
        // woods, and tries to leave for D3, where a second concealed squad stands: r2 is revealed, and g1 is forced back into its lane in D4, where it ends
        // its MPh with a concealed enemy unit, so r1 loses its "?" too. r1 then fires TPBF at it: the lane's Open Ground, no woods TEM, and FFMO.
        BypassedWoods();
        await SetupAt(2, "german", Squad("g1", "D5", "german"), Squad("r1", "D4", "russian", Conditions.Concealed), Squad("r2", "D3", "russian", Conditions.Concealed));
        Committed(await MoveG1("D4", bypass: true));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        await PassFire();

        var before = Revision;
        var result = await MoveG1("D3");
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.Contains("it stays in Bypass", StringComparison.Ordinal) && reason.Contains("(A12.151)", StringComparison.Ordinal));
        var step = LastStep(before);
        Assert.Equal((L("D4"), L("D3"), 2), (step.To.ToString(), step.Attempted?.ToString(), step.Bypass?.Count));
        Assert.Equal(2, Current.Movement!.Bypass!.Count);
        Assert.Equal(BothRussians, RevealedSince(before).Order(StringComparer.Ordinal));

        var fired = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 6), new Dictionary<string, object> { ["firers"] = R1, ["target"] = L("D4") }));
        var drm = DrmNames(Since(fired).Select(item => item.Payload).OfType<FireResolved>().Single()).ToArray();
        Assert.DoesNotContain("tem:woods", drm);
        Assert.Contains("ffmo", drm);
        await PassFire();
        Assert.True(Current.Unit("g1")!.MovementEnded);
        NoReplayErrors();

        // A forced back recorded before the pass carries no lane: the projector reads it as before, the stack in the obstacle.
        var events = store.Read(Scope)!.Events;
        var at = events.Select((item, index) => (item, index)).Single(pair => pair.item.Payload is MovementStepped { Attempted: not null }).index;
        GameEvent[] older = [.. events.Take(at + 1).Select(item => item.Payload is MovementStepped { Attempted: not null } old ? item with { Payload = old with { Bypass = null } } : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.Null(replayed.Current!.Movement!.Bypass);
    }

    [Fact]
    public async Task APinnedBypassMemberRevealsTheQuestionMarksOfItsLocationAsTheWindowCloses()
    {
        // A12.151, A4.33 (pp. 79, 50; ruling R70.23): the Russian squad two hexes off fires at the German squad Bypassing D4: 5 + 4, FFNAM and FFMO, a PTC
        // the squad fails with 6 + 6, pinned. As the DEFENDER passes, g1's MPh ends in the Bypass Location with the concealed r1, which loses its "?".
        BypassedWoods();
        var firing = N(N("D4", HexsideDirection.SouthEast), HexsideDirection.SouthEast);
        await SetupAt(2, "german", Squad("g1", "D5", "german"), Squad("r1", "D4", "russian", Conditions.Concealed), Squad("r3", firing, "russian"));
        Committed(await MoveG1("D4", bypass: true));
        Committed(await Do(GameActions.Fire, Once(5, 4, 6, 6), new Dictionary<string, object> { ["firers"] = ThirdRussian, ["target"] = L("D4") }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Pinned));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        var before = Revision;
        var result = await Do(GameActions.PassFire, NoRoll(), new
        {
        });
        Committed(result);
        Assert.Equal(R1, RevealedSince(before));
        Assert.Contains(result.Reasons, reason => reason.Contains("(A12.151)", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleEndingItsMphInBypassRevealsTheConcealedSquadThereButNotTheHiddenOne()
    {
        // A12.42 (p. 80; ruling R70.23): the German halftrack from E5 Bypasses the woods of the hex north-east of it along the hexside it shares with the
        // hex south-east of E5 (D2.3), stops, and ends its MPh there: the concealed Russian squad in the woods loses its "?"; the hidden one stays hidden.
        var wood = N("E5", HexsideDirection.NorthEast);
        woods.Add(wood);
        openSides.Add((wood, HexsideDirection.South));
        await SetupAt(2, "german", Vehicle("de-ht", "attacker-halftrack", "E5", "german", "east"), Squad("r1", wood, "russian", Conditions.Concealed),
            Squad("r2", wood, "russian", Conditions.Hidden));
        async Task Step(Dictionary<string, object> arguments)
        {
            Committed(await Do(GameActions.MoveVehicle, NoRoll(), arguments));
            if (Current.Movement is { WindowOpen: true })
            {
                await PassFire();
            }
        }

        await Step(new Dictionary<string, object> { ["vehicleId"] = "de-ht", ["kind"] = "start" });
        await Step(new Dictionary<string, object> { ["vehicleId"] = "de-ht", ["kind"] = "enter", ["to"] = L(wood), ["bypass"] = true });
        Assert.NotNull(Current.Unit("de-ht")!.Straddling);
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        await Step(new Dictionary<string, object> { ["vehicleId"] = "de-ht", ["kind"] = "stop" });
        var before = Revision;
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        if (Current.Movement is { WindowOpen: true })
        {
            await PassFire();
        }

        Assert.Equal(R1, RevealedSince(before));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.True(Is(Current.Unit("r2")!, Conditions.Hidden));
        NoReplayErrors();
    }

    // Increment 3g (task 70.9): Searching (A12.152, A12.154; ruling R70.24) and Mopping Up in any building (A12.153; ruling R70.25).
    private static readonly string[] SquadAndLeader = ["g1", "l1"];
    private static readonly string[] Searched = [L("E6"), L("E7"), L(N("E6", HexsideDirection.NorthEast))];
    private static readonly string[] SearchedOwnAndE7 = [L("E6"), L("E7")];
    private static readonly string[] RussianOwner = ["russian"];
    private static readonly string[] BuildingE5 = ["bd01:E5"];
    private static readonly string[] RowOfThree = ["E5", "E6", "E7"];
    private static readonly string[] E5AndE6 = ["E5", "E6"];
    private static readonly string[] E7Alone = ["E7"];

    /// <summary>The stone hexes E5, E6, and E7, with a Rowhouse wall between E6 and E7 (B23.71).</summary>
    private void RowhouseAtE7()
    {
        stone.UnionWith(RowOfThree);
        rowhouseWalls.Add(("E6", HexsideDirection.South));
        rowhouseWalls.Add(("E7", HexsideDirection.North));
    }

    private async Task MoveAndClose(string[] units, string hex)
    {
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = units,
            to = L(hex)
        }));
        await PassFire();
    }

    [Fact]
    public async Task ASquadAndItsLeaderEndingTheirMoveSearchAndRevealAConcealedAndAHiddenSquad()
    {
        // A12.152 (p. 79; rulings R70.24, R70.42): the German 4-6-7 and 8-1 move from E5 to E6 and, the Russian having passed, Search it and the two hexes
        // named, E7 and the hex north-east of E6. The dr is 6 with -1 for two HS-equivalents and -1 leadership: a Final dr of 4 leaves four of the six
        // Accessible hexes unsearched, so the two named, first in his order, are the only others searched; the unnamed hex north-west keeps its concealed
        // squad. r1 loses its "?", the hidden r2 is placed beneath "?" (the record the Russian's alone), and r3 stays concealed. The DEFENDER's Casualty dr
        // (A12.154), -3 for four concealed HS-equivalents, is a 6: no casualty. The searchers are TI, spend one more MF, and end their move when the Russian
        // passes. (Before the fixes the dr was a 1, a Final dr of -1, and only the named hexes were searched: ruling R70.42 searches all six on that.)
        var east = N("E6", HexsideDirection.NorthEast);
        await SetupAt(2, "german", Squad("g1", "E5", "german"), Unit("l1", "asl:leader", "attacker-leader-8-1", "E5", "german"),
            Squad("r1", "E7", "russian", Conditions.Concealed), Squad("r2", east, "russian", Conditions.Hidden), Squad("r3", N("E6", HexsideDirection.NorthWest), "russian", Conditions.Concealed));
        await MoveAndClose(SquadAndLeader, "E6");
        var spent = Current.Unit("g1")!.MfSpent;
        var before = Revision;
        var result = await Do(GameActions.Search, Once(6, 6), new
        {
            unitIds = SquadAndLeader,
            hexes = new[] { L("E7"), L(east) },
        });
        Committed(result);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.search: g1, l1 Search from bd01:E6:0", StringComparison.Ordinal));
        var search = Assert.Single(Since(before).Select(item => item.Payload).OfType<UnitsSearched>());
        Assert.Equal((-2, 6), (search.Drm, search.Accessible));
        Assert.Equal(Searched, search.Searched.Select(hex => hex.ToString()));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.True(Is(Current.Unit("r2")!, Conditions.Concealed));
        Assert.False(Is(Current.Unit("r2")!, Conditions.Hidden));
        Assert.Equal(RussianOwner, Since(before).Single(item => item.Type == "hidden-placed").Visibility);
        Assert.True(Is(Current.Unit("r3")!, Conditions.Concealed));
        Assert.True(Is(Current.Unit("g1")!, "asl:ti"));
        Assert.True(Is(Current.Unit("l1")!, "asl:ti"));
        Assert.Equal(spent + 1, Current.Unit("g1")!.MfSpent);
        Assert.True(Current.Movement is { WindowOpen: true });
        await PassFire();
        Assert.True(Current.Unit("g1")!.MovementEnded);
        Assert.True(Current.Unit("l1")!.MovementEnded);
        NoReplayErrors();
    }

    [Fact]
    public async Task ASearchCasualtyDrReducesTheSearchingSquad()
    {
        // A12.154 (p. 79): the German 4-6-7 alone Searches from E6 with a dr of 1 (-1 for two HS-equivalents): a Final dr of 0 leaves none of its six
        // Accessible hexes unsearched, so E7, named, is searched first and then the five others in map order (ruling R70.42, at the fixes; before them only
        // E7 was). In E7 the concealed Russian squad loses its "?". As an armed Good Order DEFENDER unit is there, his Casualty dr is made at once (ruling
        // R70.45): 2 - 1 for its two HS-equivalents is 1, and the searcher is Reduced to a HS, which stays TI.
        await SetupAt(2, "german", Squad("g1", "E5", "german"), Squad("r1", "E7", "russian", Conditions.Concealed));
        await MoveAndClose(GermanSquad, "E6");
        var before = Revision;
        Committed(await Do(GameActions.Search, Once(1, 2), new
        {
            unitIds = GermanSquad,
            hexes = new[] { L("E7") },
        }));
        var searched = Assert.Single(Since(before).Select(item => item.Payload).OfType<UnitsSearched>()).Searched.Select(hex => hex.ToString()).ToArray();
        Assert.Equal(SearchedOwnAndE7, searched.Take(2));
        Assert.Equal(7, searched.Distinct().Count());
        Assert.False(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("g1")!.Status);
        var half = Current.Units.Single(unit => unit.Kind == "asl:half-squad" && unit.Status == InstanceStatus.Active && unit.Side == "german");
        Assert.True(Is(half, "asl:ti"));
        NoReplayErrors();
    }

    [Fact]
    public async Task ASearchWaitsForTheDefendersWindowOnTheLastMfToClose()
    {
        // A12.152, A8.1 (ruling R70.24): the stack Searches as it ends its move, once the DEFENDER's window on its last MF is closed.
        await SetupAt(2, "german", Squad("g1", "E5", "german"), Squad("r1", "E7", "russian", Conditions.Concealed));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = GermanSquad,
            to = L("E6")
        }));
        var open = await Do(GameActions.Search, NoRoll(), new
        {
            unitIds = GermanSquad,
        });
        Assert.Contains(open.Reasons, reason => reason.StartsWith("play.search-move: the DEFENDER's window", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public void TheMapsFloodFillSplitsARowhouseAtItsWall()
    {
        // B23.71 (ruling R17.8; pass 70, ruling R70.25): the stone hexes E5, E6, and E7, with a Rowhouse wall between E6 and E7, are two buildings: E5 and
        // E6, and E7 alone. The game's map and the Studio's card editor read this one fill.
        RowhouseAtE7();
        var board = Board();
        var geometry = board.Geometry;
        string[] Fill(string hex) => [.. MapBuildings.Building(board.Facts, geometry.IndexOf(HexName.Parse(hex)), _ => true).Select(index => geometry.NameOf(index).ToString()).Order(StringComparer.Ordinal)];
        Assert.Equal(E5AndE6, Fill("E6"));
        Assert.Equal(E7Alone, Fill("E7"));
        Assert.Empty(Fill("E8"));
    }

    [Fact]
    public async Task AStoneBuildingNoCardNamesIsMoppedUpAndItsBrokenEnemySurrenders()
    {
        // A12.153 (p. 79; ruling R70.25): the game has no card; the stone building E5-E6 is the map's, named "bd01:E5" after its board and lowest hex, and
        // E7, a Rowhouse of one hex and one level beyond its wall, is no building to Mop Up. The German 4-6-7 in E5 Mops it Up in his PFPh: no concealed
        // enemy unit is left, so the German side Controls it, and the broken Russian squad in E6 surrenders to it.
        RowhouseAtE7();
        await SetupAt(1, "german", Squad("g1", "E5", "german"), Squad("g2", "E7", "german"), Squad("r1", "E6", "russian", Conditions.Broken));
        Assert.Equal(BuildingE5, Planner().MopUpBuildings(Current, [BoardLocation.Parse(L("E5")), BoardLocation.Parse(L("E7"))]).Select(item => item.Id));
        var refused = await Do(GameActions.MopUp, NoRoll(), new
        {
            building = "bd01:E6",
            unitIds = GermanSquad,
        });
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.mop-up-building: 'bd01:E6' is not a building of the map", StringComparison.Ordinal));
        var mopped = await Do(GameActions.MopUp, NoRoll(), new
        {
            building = "bd01:E5",
            unitIds = GermanSquad,
        });
        Committed(mopped);

        // The referee's finding at the reviews: the proposal says only what the declarer knows; the records say what it found.
        Assert.Contains(mopped.Reasons, reason => reason.EndsWith("What it finds is said when it is made", StringComparison.Ordinal));
        Assert.DoesNotContain(mopped.Reasons, reason => reason.Contains("surrenders to", StringComparison.Ordinal));
        Assert.Contains(Current.Secured, item => item.Building == "bd01:E5" && item.Side == "german" && item.Locations.Count == 2);
        Assert.Contains("bd01:E5", Current.MoppedUpThisPlayerTurn);
        Assert.Equal("g1", Current.Unit("r1")!.Custodian);
        NoReplayErrors();
    }

    [Fact]
    public void AnOlderRecordedMoppingUpOfACardsBuildingReplaysAsItDid()
    {
        // Ruling R70.25: a building a card names keeps the card's id; the Guards Counterattack as it was played (the fixture guards-dl-01) holds a Mopping
        // Up of a card building, which the projector replays as before: the building is Mopped Up for its Player Turn under the card's id.
        var tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");
        var file = Path.Combine(root, tenant.ToString("N"), "guards-dl-01.game.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "guards-dl-01.game.json"), file);
        var planner = new GamePlanner(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog], fireLos: new PairLos(blocked));
        var history = planner.Replay(store.Read(new GameScope(tenant, "guards-dl-01"))!.Events);
        Assert.False(history.HasErrors);
        var mopped = history.Events.Single(item => item.Payload is BuildingMoppedUp);
        var building = ((BuildingMoppedUp)mopped.Payload).Building;
        Assert.DoesNotContain(':', building);
        Assert.Contains(building, history.At(mopped.Revision)!.MoppedUpThisPlayerTurn);
    }

    // Increment 3h (task 70.6): Withdrawal completed (A11.21; ruling R70.26), a Gun's crew in CC (A11.13, A1.123; ruling R70.27), and a Carrier HS
    // (A1.32, D6.82; ruling R70.28).
    private static readonly string[] AdvancingGermans = ["de-1", "de-2"];
    private static readonly string[] RussianCrew = ["ru-gc"];
    private static readonly string[] CarrierHalfSquads = ["h1", "h2"];
    private static readonly string[] OrdinaryHalfSquads = ["h3", "h4"];
    private static readonly string[] DroppedMmg = ["de-mmg"];

    private static Dictionary<string, object> Sw(string id, string definition, string holder) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = definition,
        ["side"] = "german",
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A Russian 45mm AT Gun manned by its crew, both in plain sight.</summary>
    private static Dictionary<string, object>[] RussianGun(string hex) =>
    [
        Unit("ru-gc", "asl:crew", "defender-crew", hex, "russian"),
        new()
        {
            ["id"] = "ru-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "defender-at-gun",
            ["side"] = "russian",
            ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = "west" },
            ["holding"] = new
            {
                holder = "ru-gc",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        },
    ];

    private async Task<PlayResult> Withdraw(DiceRoller roller, string unit, string to, string[]? drops = null) => await Do(GameActions.CloseCombat, roller, new
    {
        location = L("E5"),
        attacks = Array.Empty<object>(),
        withdrawals = new Dictionary<string, string> { [unit] = L(to) },
        drops = drops ?? [],
    });

    [Fact]
    public async Task AWithdrawalIntoAConcealedEnemysLocationEliminatesTheWithdrawerAndARandomSelectionLosesItsQuestionMark()
    {
        // A11.21 (p. 73; ruling R70.26): "The Location withdrawn to cannot be currently occupied by a Known enemy unit": E6, under two Russian "?", is
        // offered, E4, with a Known Russian squad, is not. "If a unit withdraws into a concealed enemy's (not Dummy) Location it is eliminated
        // automatically but an enemy unit(s) in that Location (determined by Random Selection) must forfeit its concealment": drs 3 and 5, ru-3 loses it.
        // The projector frees a unit placed in Melee alone (KeepMelee), so the enemy is placed first and the withdrawer beside it.
        await SetupAt(7, "german", Squad("ru-1", "E5", "russian", Conditions.Melee), Squad("de-1", "E5", "german", Conditions.Melee),
            Squad("ru-2", "E6", "russian", Conditions.Concealed), Squad("ru-3", "E6", "russian", Conditions.Concealed), Squad("ru-4", "E4", "russian"));
        Assert.Equal("ccph", Current.Phase);
        var destinations = Planner().WithdrawalDestinations(Current, Current.Unit("de-1")!, BoardLocation.Parse(L("E5")));
        Assert.Contains(BoardLocation.Parse(L("E6")), destinations);
        Assert.DoesNotContain(BoardLocation.Parse(L("E4")), destinations);
        var before = Revision;
        Committed(await Withdraw(Once(3, 5), "de-1", "E6"));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("de-1")!.Status);
        Assert.True(Is(Current.Unit("ru-2")!, Conditions.Concealed));
        Assert.False(Is(Current.Unit("ru-3")!, Conditions.Concealed));
        var roll = Assert.Single(Since(before).Select(item => item.Payload).OfType<DiceRolled>());
        Assert.Equal(("random-selection", 2), (roll.Purpose, roll.Count));
        Assert.Contains(Since(before), item => item.Type == "concealment-lost" && item.Visibility is null);
        NoReplayErrors();
    }

    [Fact]
    public async Task AWithdrawalIntoAHiddenGunsLocationPlacesTheGunAndItsCrewUnconcealed()
    {
        // The p. 74 example: "there is a HIP Russian AT Gun and crew in G4 ... The Gun and crew are placed on board unconcealed (11.21) and the 2-4-7 is
        // eliminated." Here the Russian squad withdraws from E5 into E6, where a German Gun and crew are hidden (out of its LOS, so A12.34 leaves them
        // hidden); the Russian's list offers E6, which tells him nothing of them.
        blocked.Add(("E5", "E6"));
        await SetupAt(7, "german", [Squad("de-1", "E5", "german", Conditions.Melee), Squad("ru-1", "E5", "russian", Conditions.Melee), .. HiddenGun("E6")]);
        Assert.True(Is(Current.Unit("de-gc")!, Conditions.Hidden));
        Assert.Contains(BoardLocation.Parse(L("E6")), Planner().WithdrawalDestinations(Current, Current.Unit("ru-1")!, BoardLocation.Parse(L("E5"))));
        Committed(await Withdraw(NoRoll(), "ru-1", "E6"));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("ru-1")!.Status);
        var crew = Current.Unit("de-gc")!;
        var gun = Current.Find("de-gun")!;
        Assert.Equal((false, false, false, false), (Is(crew, Conditions.Hidden), Is(crew, Conditions.Concealed), Is(gun, Conditions.Hidden), Is(gun, Conditions.Concealed)));
        NoReplayErrors();
    }

    [Fact]
    public async Task AUnitAboveItsIpcWithdrawsByDroppingItsMmgAndAWithdrawalRemovesTheDummiesItEnters()
    {
        // A11.21 (ruling R70.26): de-1 carries an MMG (4PP) and an LMG (1PP), above its IPC of three: it withdraws only by dropping SW, the MMG here, which
        // stays unpossessed in E5. E6 holds a Russian Dummy alone, which is removed ("If one had been a dummy stack, it would be removed now", p. 74).
        await SetupAt(7, "german", Squad("ru-1", "E5", "russian", Conditions.Melee), Squad("de-1", "E5", "german", Conditions.Melee), Sw("de-mmg", "attacker-mmg", "de-1"),
            Sw("de-lmg", "attacker-lmg", "de-1"), Dummy("ru-d", "E6", "russian"));
        Assert.Empty(Planner().WithdrawalDestinations(Current, Current.Unit("de-1")!, BoardLocation.Parse(L("E5")), []));
        var refused = await Withdraw(NoRoll(), "de-1", "E6");
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.cc-withdrawal-portage: de-1 carries more than its IPC, so it withdraws only by dropping SW", StringComparison.Ordinal));
        var foreign = await Withdraw(NoRoll(), "de-1", "E6", ["ru-none"]);
        Assert.Contains(foreign.Reasons, reason => reason.StartsWith("play.cc-withdrawal-drop: ru-none is not a SW", StringComparison.Ordinal));
        var dropped = await Withdraw(NoRoll(), "de-1", "E6", DroppedMmg);
        Committed(dropped);
        Assert.Contains(dropped.Reasons, reason => reason.Contains("de-mmg is dropped, unpossessed in the Location, if its unit withdraws", StringComparison.Ordinal));
        Assert.Equal(L("E6"), Current.Location("de-1")!.Location.ToString());
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("ru-d")!.Status);
        var mmg = (EquipmentInstance)Current.Find("de-mmg")!;
        Assert.Equal(((Holding?)null, L("E5")), (mmg.Holding, Current.Location(mmg.Id)!.Location.ToString()));
        Assert.Equal("de-1", ((EquipmentInstance)Current.Find("de-lmg")!).Holding!.Holder);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnMmgDroppedToWithdrawStaysBehindWhenItsUnitDiesOnEntry()
    {
        // A11.21, A4.43 (ruling R70.26; fix round C): de-1 drops its MMG to withdraw into E6, where a concealed Russian squad is, and is eliminated on entry;
        // the MMG was dropped before it went, so it lies unpossessed in E5.
        await SetupAt(7, "german", Squad("ru-1", "E5", "russian", Conditions.Melee), Squad("de-1", "E5", "german", Conditions.Melee), Sw("de-mmg", "attacker-mmg", "de-1"),
            Sw("de-lmg", "attacker-lmg", "de-1"), Squad("ru-2", "E6", "russian", Conditions.Concealed));
        var before = Revision;
        Committed(await Withdraw(NoRoll(), "de-1", "E6", DroppedMmg));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("de-1")!.Status);
        var events = Since(before).ToArray();
        var drop = Array.FindIndex(events, item => item.Payload is EquipmentTransferred { Id: "de-mmg" });
        var death = Array.FindIndex(events, item => item.Payload is InstanceEliminated { Id: "de-1" });
        Assert.True(drop >= 0 && drop < death);
        var mmg = (EquipmentInstance)Current.Find("de-mmg")!;
        Assert.Equal(((Holding?)null, L("E5")), (mmg.Holding, Current.Location(mmg.Id)!.Location.ToString()));
        NoReplayErrors();
    }

    /// <summary>The German HS in E4 advance onto the Russian Gun's crew in E5 in their APh, and the CCPh begins.</summary>
    private async Task AdvanceOntoTheGun()
    {
        await SetupAt(6, "german", [Squad("de-1", "E4", "german"), Squad("de-2", "E4", "german"), .. RussianGun("E5")]);
        Assert.Equal("aph", Current.Phase);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = AdvancingGermans,
            to = L("E5"),
        }));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Assert.Equal("ccph", Current.Phase);
    }

    private async Task<PlayResult> AttackTheCrew(DiceRoller roller) => await Do(GameActions.CloseCombat, roller, new
    {
        location = L("E5"),
        attacks = new[] { new { attackers = AdvancingGermans, defenders = RussianCrew } },
    });

    [Fact]
    public async Task AnAdvanceOntoAGunsCrewLeadsToCcAndItsGunIsLostOnAColoredOneAndTheKillNumber()
    {
        // Ruling R70.27: the advance is no longer refused; the crew fights with its inherent FP (A1.123), 8 FP against its 2 at 4-1. A colored 1 and a
        // white 2 eliminate it; A11.13: "The attacker makes a subsequent dr for each Gun/SW possessed by the eliminated unit and if this dr is <= the black
        // Kill Number of that CC attack, the Gun/SW of the unit is eliminated also": a dr of 3, so the Gun goes too. The record lists the Gun the crew mans.
        await AdvanceOntoTheGun();
        var before = Revision;
        Committed(await AttackTheCrew(Once(1, 2, 3)));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("ru-gc")!.Status);
        Assert.Equal(InstanceStatus.Eliminated, ((EquipmentInstance)Current.Find("ru-gun")!).Status);
        var round = Assert.Single(Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>());
        var crewFacts = JsonNode.Parse(round.Facts.GetRawText())!["units"]!.AsArray().Single(unit => unit!["unitId"]!.GetValue<string>() == "ru-gc")!;
        Assert.Equal("ru-gun", Assert.Single(crewFacts["guns"]!.AsArray())!.GetValue<string>());
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "cc-weapon-loss");
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOlderCcRecordThatListsNoGunsReplaysAsItDid()
    {
        // Ruling R70.27: a CC record made before the pass lists no Guns, so the projector asks none of the state and rolls no Gun loss. The same round on a
        // colored 2 (no Gun loss), its facts stripped of the Gun, replays with the Gun left unpossessed in E5 (A11.13).
        await AdvanceOntoTheGun();
        Committed(await AttackTheCrew(Once(2, 1)));
        var events = store.Read(Scope)!.Events.ToList();
        var index = events.FindLastIndex(item => item.Payload is CloseCombatResolved);
        var combat = (CloseCombatResolved)events[index].Payload;
        var facts = JsonNode.Parse(combat.Facts.GetRawText())!.AsObject();
        foreach (var unit in facts["units"]!.AsArray())
        {
            unit!.AsObject().Remove("guns");
        }

        Assert.DoesNotContain("\"guns\"", facts.ToJsonString(), StringComparison.Ordinal);
        events[index] = events[index] with
        {
            Payload = combat with
            {
                Facts = JsonSerializer.SerializeToElement(facts)
            }
        };
        var older = Planner().Replay(events);
        Assert.False(older.HasErrors);
        var gun = (EquipmentInstance)older.Current!.Find("ru-gun")!;
        Assert.Equal((InstanceStatus.Active, (Holding?)null), (gun.Status, gun.Holding));
        Assert.Equal(InstanceStatus.Eliminated, older.Current.Unit("ru-gc")!.Status);
    }

    [Fact]
    public async Task CarrierHalfSquadsRecombineWithNoLeaderAndOrdinaryOnesStillNeedOne()
    {
        // A1.32 (p. 45): "Finns, Guards, Carrier HS and unarmed HS do not need leader presence to Recombine"; D6.82 (p. 206). Ruling R70.28: a Carrier
        // HS is one whose creation recorded the Carrier it left ("crewOf"); two ordinary HS beside them still need their leader.
        static Dictionary<string, object> Carried(Dictionary<string, object> unit)
        {
            unit["crewOf"] = "de-carrier";
            return unit;
        }

        await SetupAt(0, "german", Carried(Unit("h1", "asl:half-squad", "attacker-half-squad", "C3", "german")), Carried(Unit("h2", "asl:half-squad", "attacker-half-squad", "C3", "german")),
            Unit("h3", "asl:half-squad", "attacker-half-squad", "D3", "german"), Unit("h4", "asl:half-squad", "attacker-half-squad", "D3", "german"));
        Assert.Equal("de-carrier", Current.Unit("h1")!.CrewOf);
        var refused = await Do(GameActions.Recombine, NoRoll(), new
        {
            halfSquads = OrdinaryHalfSquads
        });
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.deploy-leader", StringComparison.Ordinal));
        Committed(await Do(GameActions.Recombine, NoRoll(), new
        {
            halfSquads = CarrierHalfSquads,
            attemptId = "rec"
        }));
        Assert.Equal("asl:squad", Current.Unit("rec-h1")!.Kind);
        NoReplayErrors();
    }

    // Increment 3i (task 70.7, part I): Infantry against Infantry in a Location with a vehicle (A11.31; ruling R70.30), its Ambush (A11.4, A11.34, A11.51),
    // and PRC in CC (A11.611, A11.71; ruling R70.31).
    private static readonly string[] RussianOne = ["ru-1"];
    private static readonly string[] RussianTwo = ["ru-2"];
    private static readonly string[] BothRussianSquads = ["ru-1", "ru-2"];
    private static readonly string[] GermanOne = ["de-1"];
    private static readonly string[] GermanRider = ["de-r"];
    private static readonly int[] PaatcPassed = [3, 4];

    private async Task<PlayResult> VehicleCc(DiceRoller roller, string hex = "E5", string[]? attackers = null, string[]? defenders = null, string? vehicle = null, bool pass = false)
    {
        var arguments = new Dictionary<string, object> { ["location"] = L(hex) };
        if (attackers is not null)
        {
            arguments["attackers"] = attackers;
        }

        if (defenders is not null)
        {
            arguments["defenders"] = defenders;
        }

        if (vehicle is not null)
        {
            arguments["vehicleId"] = vehicle;
        }

        if (pass)
        {
            arguments["pass"] = true;
        }

        return await Do(GameActions.VehicleCloseCombat, roller, arguments);
    }

    /// <summary>
    /// The Russian units in the first hex advance into the German vehicle's Location in their APh, each passing its PAATC (3 + 4 against 7; A11.6), and the
    /// CCPh begins. A vehicle sharing a Location with an enemy unit at setup is not reviewed (ruling R25.3), so the CC is reached by the advance.
    /// </summary>
    private async Task AdvanceOntoTheTank(string to, string[] advancing, params Dictionary<string, object>[] placements)
    {
        await SetupAt(6, "russian", placements);
        Committed(await Do(GameActions.Advance, Once([.. advancing.SelectMany(_ => PaatcPassed)]), new
        {
            unitIds = advancing,
            to = L(to),
        }));
        Assert.Equal(L(to), Current.Location(advancing[0])!.Location.ToString());
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Assert.Equal("ccph", Current.Phase);
    }

    [Fact]
    public async Task TheH3ExamplesShapeAlternatesInfantryAndATankAgainstInfantryWithNoReply()
    {
        // A11.31 (p. 73) and the H3 example (p. 74): in E5 with the German PzKpfw IIIH and 4-6-7, the non-vehicular Russians attack first, "the 6-2-8 attacks
        // the German Infantry at 1:1 ... Subsequent attack opportunities alternate between sides". Each attack is one record with no reply (ruling R70.30);
        // A11.12's EXC: the tank may still attack a squad Infantry attacked, and Infantry no unit Infantry attacked already.
        await AdvanceOntoTheTank("E5", BothRussianSquads, Squad("ru-1", "E4", "russian"), Squad("ru-2", "E4", "russian"), Vehicle("de-tank", "attacker-tank", "E5", "german", "east"),
            Squad("de-1", "E5", "german"));
        var e5 = BoardLocation.Parse(L("E5"));
        Assert.Equal(("russian", true), Planner().VehicleCloseCombatTurn(Current, e5));
        var simultaneous = await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = L("E5"),
            attacks = new[] { new { attackers = RussianOne, defenders = GermanOne } },
        });
        Assert.Contains(simultaneous.Reasons, reason => reason.Contains("one attack at a time, Infantry against Infantry too", StringComparison.Ordinal));

        var before = Revision;
        Committed(await VehicleCc(Once(6, 6), attackers: RussianOne, defenders: GermanOne));
        var first = Assert.Single(Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>());
        Assert.Equal((CloseCombatResolved.SequentialRound, "german", false), (first.Round, first.Next, first.Closed));
        Assert.Equal(GermanOne, Current.CloseCombats.Single().InfantryAttacked);

        Committed(await VehicleCc(Once(6, 6), attackers: GermanOne, defenders: RussianOne));
        Assert.Equal("russian", Current.CloseCombats.Single().Next);
        Assert.Contains((await VehicleCc(NoRoll(), attackers: RussianTwo, defenders: GermanOne)).Reasons, reason => reason.StartsWith("play.cc-vehicle-defenders", StringComparison.Ordinal));
        Assert.Contains((await VehicleCc(NoRoll(), attackers: RussianOne, vehicle: "de-tank")).Reasons, reason => reason.StartsWith("play.cc-vehicle-order", StringComparison.Ordinal));

        // ru-2 attacks the tank (CCV 5, +2 for the escorting squad): 8 + 2 has no effect; the tank then attacks both squads, ru-1 among them, and the CC is over.
        Committed(await VehicleCc(Once(4, 4), attackers: RussianTwo, vehicle: "de-tank"));
        Assert.Equal("german", Current.CloseCombats.Single().Next);
        before = Revision;
        Committed(await VehicleCc(Once(5, 6), defenders: BothRussianSquads, vehicle: "de-tank"));
        Assert.True(Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>()).Closed);
        NoReplayErrors();
    }

    [Fact]
    public async Task ABerserkSquadBesideItsTankOwesItsAttackOnTheEnemyInfantryThere()
    {
        // A15.43 (p. 84; fix round C): the berserk German 4-6-7 beside its tank in E5 must attack the Russian squad that advanced in (A11.31, A11.12; ruling
        // R70.30): after the Russian attack on the tank (4 + 4 + 2, no effect), the German side may not pass, nor the CCPh end, until it has.
        await AdvanceOntoTheTank("E5", RussianOne, Squad("ru-1", "E4", "russian"), Vehicle("de-tank", "attacker-tank", "E5", "german", "east"),
            Squad("de-1", "E5", "german", Conditions.Berserk));
        Committed(await VehicleCc(Once(4, 4), attackers: RussianOne, vehicle: "de-tank"));
        Assert.Equal("german", Current.CloseCombats.Single().Next);
        var pass = await VehicleCc(NoRoll(), pass: true);
        Assert.Contains(pass.Reasons, reason => reason.StartsWith("play.cc-vehicle-berserk: de-1 is berserk and attacks enemy Infantry", StringComparison.Ordinal));
        Assert.Contains((await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        })).Reasons, reason => reason.StartsWith("play.cc-required: de-1 is berserk with enemy Infantry", StringComparison.Ordinal));
        Committed(await VehicleCc(Once(6, 6), attackers: GermanOne, defenders: RussianOne));
        Assert.Null(Planner().CloseCombatDue(Current).FirstOrDefault(line => line.Contains("de-1 is berserk", StringComparison.Ordinal)));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAmbushInAVehiclesLocationTakesTheVehicleDrmAndTheAmbusherAttacksFirstWithItsDrmBothWays()
    {
        // A11.4 (p. 73): the Russian squad advances into the woods of E5 (after its PAATC, 3 + 4 against 7), so an Ambush can occur though the German tank is
        // there; the German dr takes +2 Vehicle and +1 for the BU AFV. A Russian 1 against the German 4 + 3: the Russians ambush. A11.34: they attack first;
        // A11.4, A11.51 (ruling R70.30): -1 to their attacks, +1 to the tank's attack on them.
        woods.Add("E5");
        await SetupAt(6, "russian", Squad("ru-1", "E4", "russian"), Vehicle("de-tank", "attacker-tank", "E5", "german", "east"), Squad("de-1", "E5", "german"));
        Committed(await Do(GameActions.Advance, Once(3, 4), new
        {
            unitIds = RussianOne,
            to = L("E5"),
        }));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        var e5 = BoardLocation.Parse(L("E5"));
        Assert.True(Planner().AmbushDue(Current, e5));
        var before = Revision;
        Committed(await Do(GameActions.Ambush, Once(1, 4), new
        {
            location = L("E5"),
        }));
        var ambush = Assert.Single(Since(before).Select(item => item.Payload).OfType<AmbushRolled>());
        Assert.Equal("russian", ambush.Ambusher);
        var german = ambush.Resolution.GetProperty("sides").EnumerateArray().Single(side => side.GetProperty("side").GetString() == "german");
        Assert.Equal(["vehicle", "bu-or-stunned-afv"], german.GetProperty("drm").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(("russian", true), Planner().VehicleCloseCombatTurn(Current, e5));

        before = Revision;
        Committed(await VehicleCc(Once(6, 6), attackers: RussianOne, defenders: GermanOne));
        var round = Assert.Single(Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>());
        Assert.Contains(round.Resolution.GetProperty("attacks")[0].GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "ambush");
        Assert.Equal("german", round.Next);
        before = Revision;
        Committed(await VehicleCc(Once(5, 6), defenders: RussianOne, vehicle: "de-tank"));
        var byTank = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>());
        Assert.Equal("russian", byTank.Facts.GetProperty("ambusher").GetString());
        Assert.Contains(byTank.Resolution.GetProperty("defending")[0].GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "vs-ambush");
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOlderRecordedVehicleCcSequenceKeepsItsRecordedNextAndClose()
    {
        // The gating of increment 3i: a vehicle CC record keeps the next side and the close it recorded (GameProjector), so a game made before the pass,
        // whose last attack closed the Location with no CC between Infantry there, replays closed, though the German squad would now have an attack.
        await AdvanceOntoTheTank("E5", RussianOne, Squad("ru-1", "E4", "russian"), Vehicle("de-tank", "attacker-tank", "E5", "german", "east"), Squad("de-1", "E5", "german"));
        Committed(await VehicleCc(Once(4, 4), attackers: RussianOne, vehicle: "de-tank"));
        Committed(await VehicleCc(Once(5, 6), defenders: RussianOne, vehicle: "de-tank"));
        var events = store.Read(Scope)!.Events.ToList();
        var index = events.FindLastIndex(item => item.Payload is VehicleCloseCombatResolved);
        var last = (VehicleCloseCombatResolved)events[index].Payload;
        Assert.Equal(("german", false), (last.Next, last.Closed));
        events[index] = events[index] with
        {
            Payload = last with
            {
                Next = null,
                Closed = true
            }
        };
        var older = Planner().Replay(events);
        Assert.False(older.HasErrors);
        Assert.True(older.Current!.CloseCombats.Single().Closed);
        Assert.False(Planner().VehicleCloseCombatTurn(older.Current, BoardLocation.Parse(L("E5"))).Open);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ARiderIsAttackedApartAndOnlyAnImmobileVehiclesPrcAreHeldInMelee(bool immobile)
    {
        // A11.611 (p. 75): the Russian squad attacks the German Rider apart, with -1 (ruling R70.31). A11.71 (p. 76): "Passengers/Riders of Immobile vehicles
        // are held in Melee, but ... Passengers/Riders of Mobile vehicles who survive their initial round of CC are not required to remain in Melee"; the
        // CCPh's record names the PRC held. The tank, stopped, holds the Russian squad either way (A11.7).
        year = 1943;
        var tank = Vehicle("de-tank", "attacker-tank", "B10", "german", "east");
        if (immobile)
        {
            tank["conditions"] = new Dictionary<string, bool> { [Conditions.Immobilized] = true };
        }

        await AdvanceOntoTheTank("B10", RussianOne, Squad("ru-1", "B9", "russian"), tank, Rider("de-r", "asl:squad", "attacker-squad", "de-tank"));
        var before = Revision;
        Committed(await VehicleCc(Once(6, 6), "B10", attackers: RussianOne, defenders: GermanRider));
        var round = Assert.Single(Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>());
        Assert.Contains(round.Resolution.GetProperty("attacks")[0].GetProperty("defending")[0].GetProperty("drm").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "vs-rider");
        Assert.Equal("german", round.Next);
        Committed(await VehicleCc(NoRoll(), "B10", pass: true));
        before = Revision;
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        // The public record names no unit aboard (A12.2, A12.3; the referee's finding at the reviews): the projector reads the vehicle's own state.
        var ended = Assert.Single(Since(before).Select(item => item.Payload).OfType<PhaseChanged>());
        Assert.True(ended.PrcMelee);
        Assert.Null(ended.PrcHeldInMelee);
        Assert.True(Is(Current.Unit("ru-1")!, Conditions.Melee));
        Assert.Equal(immobile, Is(Current.Unit("de-r")!, Conditions.Melee));
        NoReplayErrors();

        // A record of the pass's builds names the PRC held, and replays as it did.
        GameEvent[] listed = [.. store.Read(Scope)!.Events.Select(item => item.Payload is PhaseChanged { PrcMelee: true } change
            ? item with { Payload = change with { PrcMelee = null, PrcHeldInMelee = immobile ? GermanRider : [] } } : item)];
        var fromList = Planner().Replay(listed);
        Assert.False(fromList.HasErrors);
        Assert.Equal(immobile, Is(fromList.Current!.Unit("de-r")!, Conditions.Melee));

        // A record made before the pass names no PRC held, and the projector reads the Rider for Melee as it did: held, beside the Russian squad.
        GameEvent[] older = [.. store.Read(Scope)!.Events.Select(item => item.Payload is PhaseChanged { PrcMelee: true } change ? item with { Payload = change with { PrcMelee = null } } : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.True(Is(replayed.Current!.Unit("de-r")!, Conditions.Melee));
    }

    // Increment 3j (task 70.7, part II): Street Fighting (A11.8; ruling R70.33), A11.52's Passengers, A18.12 against a vehicle, and D5.42's entry at a capture
    // (ruling R70.34).
    private static readonly string[] GermanPassenger = ["de-p"];
    private static readonly string[] RussianHalfSquad = ["ru-h"];

    /// <summary>A unit aboard a vehicle as a Passenger (D6.1).</summary>
    private static Dictionary<string, object> Passenger(string id, string kind, string definition, string vehicle)
    {
        var unit = Unit(id, kind, definition, "B10", "german");
        unit["position"] = new Dictionary<string, string> { ["in"] = vehicle, ["role"] = "passenger" };
        return unit;
    }

    /// <summary>The neighbor of a hex of board 01 across a hexside, by the board's geometry.</summary>
    private static string Beside(string hex, HexsideDirection side)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        Assert.True(geometry.TryGetIndex(HexName.Parse(hex), out var index));
        return geometry.NameOf(geometry.Neighbor(index, side)!.Value).ToString();
    }

    [Fact]
    public async Task AVehicleInARoadHexBetweenBuildingsIsFoughtByStreetFightingAndTheSquadReturnsToItsBuilding()
    {
        // A11.8 (p. 76): the road runs through F5 across its north and south hexsides; the stone buildings NW and NE of it flank it on both sides. The Russian
        // 4-4-7 advances from the building NW of F5 onto the German tank there (PAATC 3 + 4), and fights it with -1 (5 + 5 - 1 = 9 against CCV 5: no effect);
        // the tank's attack on it takes +1. Once the CC is over the squad returns to its building (ruling R70.33).
        var west = Beside("F5", HexsideDirection.NorthWest);
        var east = Beside("F5", HexsideDirection.NorthEast);
        stone.Add(west);
        stone.Add(east);
        roadSides.Add(("F5", HexsideDirection.North));
        roadSides.Add(("F5", HexsideDirection.South));
        var facts = Board().Facts;
        Assert.True(BoardGeometry.StandardGeomorphic.TryGetIndex(HexName.Parse("F5"), out var f5));
        var sides = RoadFlanks.Of(facts, f5);
        Assert.Equal(2, sides.Count);
        Assert.All(sides, side => Assert.Single(side));

        await AdvanceOntoTheTank("F5", RussianOne, Squad("ru-1", west, "russian"), Vehicle("de-tank", "attacker-tank", "F5", "german", "east"));
        Assert.Equal(L(west), Current.Advances.Single().AdvancedFrom!.ToString());
        var before = Revision;
        Committed(await VehicleCc(Once(5, 5), "F5", attackers: RussianOne, vehicle: "de-tank"));
        var attack = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>());
        Assert.Equal(RussianOne, attack.Facts.GetProperty("streetFighters").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains(attack.Resolution.GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "street-fighting");
        Assert.Empty(Planner().StreetFightingReturns(Current, "russian"));
        before = Revision;
        Committed(await VehicleCc(Once(5, 6), "F5", defenders: RussianOne, vehicle: "de-tank"));
        var byTank = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>());
        Assert.Contains(byTank.Resolution.GetProperty("defending")[0].GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "vs-street-fighting");
        Assert.True(byTank.Closed);

        var returning = Assert.Single(Planner().StreetFightingReturns(Current, "russian"));
        Assert.Equal(("ru-1", L(west)), (returning.Unit.Id, returning.From.ToString()));
        Committed(await Do(GameActions.StreetFightingReturn, NoRoll(), new
        {
            unitId = "ru-1",
        }));
        Assert.Equal(L(west), Current.Location("ru-1")!.Location.ToString());
        Assert.Empty(Planner().StreetFightingReturns(Current, "russian"));
        NoReplayErrors();
    }

    [Fact]
    public async Task ATrucksPassengerIsCapturedInCcAndTheTruckWithIt()
    {
        // A11.52 (p. 74): the German truck in E5 carries a 4-6-7 Passenger; the Russian 4-4-7 advances into its Location (no PAATC against an unarmored
        // vehicle). Its Passenger is attacked only to capture it, at -2 with the capture's +1: 2 + 2 - 1 = 3, below the Kill Number, captures it. Every
        // Passenger captured and no German Personnel left, the truck is captured too (ruling R70.34); the prisoner leaves the truck for its Guard's Location.
        await SetupAt(6, "russian", Squad("ru-1", "E4", "russian"), Vehicle("de-t", "attacker-truck", "E5", "german", "east"),
            Passenger("de-p", "asl:squad", "attacker-squad", "de-t"));
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = RussianOne,
            to = L("E5"),
        }));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Assert.Equal("german", Current.Unit("de-t")!.Side);
        Assert.Contains((await VehicleCc(NoRoll(), attackers: RussianOne, defenders: GermanPassenger)).Reasons,
            reason => reason.StartsWith("play.cc-vehicle-defenders", StringComparison.Ordinal));
        var before = Revision;
        Committed(await Do(GameActions.VehicleCloseCombat, Once(2, 2), new
        {
            location = L("E5"),
            attackers = RussianOne,
            defenders = GermanPassenger,
            capture = true,
        }));
        var round = Assert.Single(Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>());
        Assert.Equal(RussianOne, round.Captors);
        Assert.Contains(round.Resolution.GetProperty("attacks")[0].GetProperty("defending")[0].GetProperty("drm").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "vs-passenger-capture");
        var captured = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCaptured>());
        Assert.Equal(("de-t", "russian"), (captured.Vehicle, captured.Side));
        Assert.Equal("russian", Current.Unit("de-t")!.Side);
        Assert.True(Is(Current.Unit("de-p")!, Conditions.Captured));
        Assert.IsType<MapPosition>(Current.Unit("de-p")!.Position);
        Assert.Equal(RussianOne, Current.CloseCombats.Single().Captors);
        NoReplayErrors();
    }

    [Fact]
    public async Task ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack()
    {
        // A18.12 (p. 85): the Russian 4-4-7 rolls an Original 2 against the PzKpfw IIIH in E5; the Leader Creation dr 5, +1 Russian, is 6: a 6+1, who adds
        // his +1 and one CCV (ruling R70.34). The record names him, and he attacks no more this CCPh.
        await AdvanceOntoTheTank("E5", RussianOne, Squad("ru-1", "E4", "russian"), Vehicle("de-tank", "attacker-tank", "E5", "german", "east"));
        var before = Revision;
        Committed(await VehicleCc(Once(1, 1, 5), attackers: RussianOne, vehicle: "de-tank"));
        var attack = Assert.Single(Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>());
        Assert.True(attack.Facts.GetProperty("leaderCreation").GetBoolean());
        Assert.Equal("defender-leader-6-plus-1", attack.Resolution.GetProperty("leaderCreation").GetProperty("leaderDefinitionId").GetString());
        var leader = attack.CreatedLeader!;
        Assert.Equal(("defender-leader-6-plus-1", "russian", L("E5")), (Current.Unit(leader)!.Definition!.Definition, Current.Unit(leader)!.Side, Current.Location(leader)!.Location.ToString()));
        Assert.Contains(leader, Current.CloseCombats.Single().Attacking);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOlderRecordedVehicleCcWithAnOriginalTwoReplaysWithNoLeader()
    {
        // The gating of A18.12 (ruling R70.34): a record made before the pass declares no Leader Creation, rolls none, and creates no leader; it replays as it did.
        await AdvanceOntoTheTank("E5", RussianOne, Squad("ru-1", "E4", "russian"), Vehicle("de-tank", "attacker-tank", "E5", "german", "east"));
        Committed(await VehicleCc(Once(1, 1, 5), attackers: RussianOne, vehicle: "de-tank"));
        var events = store.Read(Scope)!.Events.ToList();
        var index = events.FindIndex(item => item.Payload is VehicleCloseCombatResolved);
        var record = (VehicleCloseCombatResolved)events[index].Payload;
        var leader = record.CreatedLeader!;
        var facts = record.Facts.Deserialize<Rules.VehicleCloseCombatFacts>(LiveFire.Json)! with
        {
            LeaderCreation = null
        };
        var resolution = Rules.ScenarioA1VehicleCloseCombat.Resolve(facts with
        {
            Rolls = new Rules.VehicleCloseCombatRolls(SnakeEyesRoll)
        }, new Rules.ScenarioA1CloseCombatPackage().Reference);
        Assert.Null(resolution.LeaderCreation);
        events[index] = events[index] with
        {
            Payload = record with
            {
                Facts = JsonSerializer.SerializeToElement(facts, LiveFire.Json),
                Rolls = record.Rolls.Where(item => item.Key != "leaderCreation").ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal),
                Resolution = JsonSerializer.SerializeToElement(resolution, LiveFire.Json),
                CreatedLeader = null,
            }
        };
        // The record now reads as one made before the pass (its leader's own event left in place, as the verifier reads the record alone).
        var older = Planner().Replay(events);
        Assert.False(older.HasErrors, string.Join(" ", older.Diagnostics));
        Assert.DoesNotContain(leader, older.Current!.CloseCombats.Single().Attacking);
    }

    private static readonly int[] SnakeEyesRoll = [1, 1];

    [Fact]
    public async Task AHalfSquadThatCapturedAnAbandonedTankEntersItAsItsCrewInThatCcph()
    {
        // D5.42 (p. 204): "[EXC: CCPh; A21.2]": the Russian HS captures the Abandoned PzKpfw IIIH in E5 by a capture attempt (1 + 2 - 3 against CCV 3, A21.2),
        // and enters it as its Temporary crew in the same CCPh, its MF spent by the advance (ruling R70.34).
        var tank = Vehicle("de-tank", "attacker-tank", "E5", "german", "east");
        tank["conditions"] = new Dictionary<string, bool> { [Conditions.Abandoned] = true };
        await AdvanceOntoTheTank("E5", RussianHalfSquad, Unit("ru-h", "asl:half-squad", "defender-half-squad", "E4", "russian"), tank);
        Committed(await Do(GameActions.VehicleCloseCombat, Once(1, 2), new
        {
            location = L("E5"),
            attackers = RussianHalfSquad,
            vehicleId = "de-tank",
            capture = true,
        }));
        Assert.Equal("russian", Current.Unit("de-tank")!.Side);
        Assert.Contains("de-tank", Current.CapturedThisPhase);
        Assert.Equal(RussianHalfSquad, Planner().RecrewCandidates(Current, Current.Unit("de-tank")!).Select(unit => unit.Id));
        Committed(await Do(GameActions.RecrewVehicle, NoRoll(), new
        {
            unitId = "ru-h",
            vehicleId = "de-tank",
        }));
        Assert.IsType<ContainedPosition>(Current.Unit("ru-h")!.Position);
        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Abandoned));
        NoReplayErrors();
    }
}
