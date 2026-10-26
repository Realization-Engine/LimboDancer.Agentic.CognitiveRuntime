using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 71 in live play, Infantry IV: Snipers and wounds. Increment 4a: the Sniper's targets and results (A14.21, A14.22, A14.3, A14.31, A14.33, D5.34,
/// A12.14; rulings R71.3 to R71.8). Board 01's grid, all Open Ground, fixed dice, and a LOS that is clear at the board's range but between the pairs a test
/// blocks. In each game the German 4-6-7 in E3 fires at the Russian 4-4-7 in E4 with an IFT DR of 6, the Russian SAN, and the Russian Sniper in E7 attacks:
/// its Random Location DR (colored 1, white 1) finds E6, one hex north (as in pass 15's model test). Every read of the state replays the game.
/// </summary>
public sealed class BacklogPass71Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f571");
    private static readonly GameScope Scope = new(Tenant, "pass71");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass71-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly HashSet<(string, string)> blocked = [];

    public BacklogPass71Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0.</summary>
    private static BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var type = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 71 test board", new HexFactSet(geometry, "pass71", hexes));
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

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new PairLos(blocked));

    /// <summary>These values first, then the fallback face for every other die.</summary>
    private static DiceRoller Then(int fallback, params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => (queue.Count > 0 ? queue.Dequeue() : fallback) - 1);
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

    private static Dictionary<string, object> Vehicle(string id, string definition, string hex, bool buttonedUp) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = "german",
        ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = "east" },
        ["conditions"] = new Dictionary<string, bool> { [Conditions.ButtonedUp] = buttonedUp },
    };

    /// <summary>A German 4-6-7 aboard a vehicle as a Passenger (D6.1).</summary>
    private static Dictionary<string, object> Passenger(string id, string vehicle)
    {
        var unit = Squad(id, "B10", "german");
        unit["position"] = new Dictionary<string, string> { ["in"] = vehicle, ["role"] = "passenger" };
        return unit;
    }

    private static Dictionary<string, object> Sniper(string id, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:sniper",
        ["side"] = side,
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A German game left in the German PFPh with the Russian SAN of 6 (and a German SAN when given), the 4-6-7 g1 in E3, the 4-4-7 r1 in E4, the Russian Sniper in E7, and the placements given.</summary>
    private Task Setup(int? germanSan, params Dictionary<string, object>[] placements) => Setup(germanSan, 6, [], placements);

    /// <summary>As above, with the Russian SAN and the SSRs given.</summary>
    private async Task Setup(int? germanSan, int russianSan, string[] specialRules, params Dictionary<string, object>[] placements)
    {
        Dictionary<string, object>[] all = [Squad("g1", "E3", "german"), Squad("r1", "E4", "russian"), Sniper("rs", "E7", "russian"), .. placements];
        Committed(await FirstSetup(germanSan, russianSan, specialRules, all));
        await Advance();
    }

    /// <summary>The first setup of a game made without a card: the sides with the SANs given, and the placements given.</summary>
    private async Task<PlayResult> FirstSetup(int? germanSan, int russianSan, string[] specialRules, Dictionary<string, object>[] placements) =>
        await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass71Tests),
                ["catalog"] = $"asl-scenario-a1@{LiveFire.CatalogVersion}",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["specialRules"] = specialRules,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, san = germanSan, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, san = russianSan, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements,
        }));

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

    /// <summary>The German fire that calls the Russian Sniper: IFT DR 3 + 3 (the Russian SAN), the 4-4-7's MC 3 + 3, then the Sniper's dice given.</summary>
    private async Task<SniperAttacked[]> FireCallingTheSniper(params int[] sniperDice)
    {
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, [3, 3, 3, 3, .. sniperDice]), new
        {
            firers = G1,
            target = L("E4"),
        }));
        Assert.False(History.HasErrors);
        return [.. Since(before).Select(item => item.Payload).OfType<SniperAttacked>()];
    }

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    [Fact]
    public async Task ASnipersTwoStunsTheCeCrewOfATank()
    {
        // A14.3, D5.34 (pp. 82, 203; ruling R71.5): the German PzKpfw IIIH in E6 is CE, so its Inherent crew is the one target there; the Sniper's dr 2 Stuns
        // it, and the tank is buttoned up and Stopped as the Fire path's Stun leaves it.
        await Setup(null, Vehicle("de-tk", "attacker-tank", "E6", buttonedUp: false));
        var sniped = Assert.Single(await FireCallingTheSniper(2, 1, 1));
        Assert.Equal(("de-tk", "stunned", L("E6")), (sniped.Unit, sniped.Result, sniped.Target?.ToString()));
        var tank = Current.Unit("de-tk")!;
        Assert.True(Is(tank, Conditions.Stunned));
        Assert.True(Is(tank, Conditions.ButtonedUp));
        Assert.Equal(L("E6"), Current.Location("rs")!.Location.ToString());
    }

    [Fact]
    public async Task AButtonedUpHalftracksPassengersAreNoTargetsSoTheSniperFindsTheNextHex()
    {
        // A14.22 (ruling R71.3): the BU SPW 251/1 in E6 carries a 4-6-7 Passenger; neither it nor the crew is Vulnerable, so E6 holds no target and the
        // Sniper attacks the closest hex that does, the firer's E3 (A14.21): its dr 1 breaks g1. Before the pass the Passenger was attacked in E6.
        await Setup(null, Vehicle("de-ht", "attacker-halftrack", "E6", buttonedUp: true), Passenger("de-p", "de-ht"));
        var sniped = Assert.Single(await FireCallingTheSniper(1, 1, 1));
        Assert.Equal(("g1", "broken", L("E3")), (sniped.Unit, sniped.Result, sniped.Target?.ToString()));
        Assert.False(Is(Current.Unit("de-p")!, Conditions.Broken));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Broken));
    }

    [Fact]
    public async Task ATrucksPassengerCrewAndTheTruckItselfAreThreeCandidatesAndAOneImmobilizesTheTruck()
    {
        // A14.22, A14.33 (rulings R71.3, R71.5): the German truck in E6 with a 4-6-7 Passenger is three candidates for Random Selection, the Passenger, the
        // Inherent crew, and the truck (in id order: de-p, then de-t's crew and de-t); the dice 1, 1, 6 pick the truck, and the dr 1 immobilizes it. The
        // Passenger takes no dr: a vehicle and its PRC are never attacked by the same one.
        await Setup(null, Vehicle("de-t", "attacker-truck", "E6", buttonedUp: false), Passenger("de-p", "de-t"));
        var before = Revision;
        var sniped = Assert.Single(await FireCallingTheSniper(1, 1, 1, 1, 1, 6));
        Assert.Equal(("de-t", "immobilized"), (sniped.Unit, sniped.Result));
        var selection = Since(before).Select(item => item.Payload).OfType<DiceRolled>().Single(item => item.Purpose == "sniper-random-selection");
        Assert.Equal(3, selection.Count);
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Immobilized));
        Assert.False(Is(Current.Unit("de-p")!, Conditions.Broken));
    }

    [Fact]
    public async Task TheEnemySniperIsPinnedUntilTheCloseCombatPhaseEnds()
    {
        // A14.21, A14.31 (ruling R71.6): the German Sniper counter alone in E6 is a target; the Russian Sniper's dr 2 pins it, and its pin is removed with
        // the others at the end of the CCPh.
        await Setup(2, Sniper("gs", "E6", "german"));
        var sniped = Assert.Single(await FireCallingTheSniper(2, 1, 1));
        Assert.Equal(("gs", "sniper-pinned"), (sniped.Unit, sniped.Result));
        Assert.True(Is(Current.Find("gs")!, Conditions.Pinned));
        await Advance(6);
        Assert.Equal("ccph", Current.Phase);
        Assert.True(Is(Current.Find("gs")!, Conditions.Pinned));
        await Advance();
        Assert.False(Is(Current.Find("gs")!, Conditions.Pinned));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ALeaderTheSniperEliminatesStillCausesTheLlmcOfHisLocation()
    {
        // A10.2, A14.3 (pass 70, ruling R70.3; pass 71, the reads through the index): the German 8-0 and 4-6-7 in E6; Random Selection's 6, 1 picks the
        // leader, whose dr 1 eliminates him, and the 4-6-7 takes its LLMC.
        await Setup(null, Unit("de-l", "asl:leader", "attacker-leader-8-0", "E6", "german"), Squad("de-s", "E6", "german"));
        var before = Revision;
        var sniped = Assert.Single(await FireCallingTheSniper(1, 1, 1, 6, 1));
        Assert.Equal(("de-l", "eliminated"), (sniped.Unit, sniped.Result));
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("de-l")!.Status);
        Assert.Single(Since(before).Select(item => item.Payload).OfType<MoraleCheckResolved>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AConcealedSquadPinnedByASniperKeepsItsQuestionMarkOnlyOutOfEveryGoodOrderEnemyLos(bool outOfLos)
    {
        // A12.14 (p. 77; ruling R71.8): the concealed German 4-6-7 in E6 is pinned by the dr 2. Out of the 4-4-7's LOS it keeps its "?", and the record names
        // no unit; in that LOS it loses it.
        if (outOfLos)
        {
            blocked.Add(("E4", "E6"));
        }

        await Setup(null, Squad("de-c", "E6", "german", Conditions.Concealed));
        var sniped = Assert.Single(await FireCallingTheSniper(2, 1, 1));
        Assert.Equal((outOfLos ? null : "de-c", "pinned"), (sniped.Unit, sniped.Result));
        var squad = Current.Unit("de-c")!;
        Assert.True(Is(squad, Conditions.Pinned));
        Assert.Equal(outOfLos, Is(squad, Conditions.Concealed));
    }

    // Increment 4b: the SAN as state and Sniper Check (A14.4, E1.76; rulings R71.9, R71.10).

    /// <summary>The German 8-1 de-l, 4-6-7 de-s, and 2-4-7 de-h in E6, the Sniper's target hex.</summary>
    private static Dictionary<string, object>[] TheGermansInE6() =>
        [Unit("de-l", "asl:leader", "attacker-leader-8-1", "E6", "german"), Squad("de-s", "E6", "german"), Unit("de-h", "asl:half-squad", "attacker-half-squad", "E6", "german")];

    // The Sniper's dr 2 and Random Location DR (north, one hex) find E6; Random Selection's 6, 1, 1 picks de-h (the first by id), which the dr 2 pins.
    private static readonly int[] PinsTheHalfSquad = [2, 1, 1, 6, 1, 1];
    private static readonly string[] TheLeaderAndTheSquad = ["de-l", "de-s"];

    private Task<PlayResult> Choose(string option, DiceRoller roller) => Do(GameActions.Choose, roller, new
    {
        key = Current.Choice!.Key,
        option,
    });

    /// <summary>The German fire with the dice given, which reach the Sniper's, and the events it wrote.</summary>
    private async Task<IReadOnlyList<GameEvent>> Fire(string firer, params int[] dice)
    {
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, dice), new
        {
            firers = new[] { firer },
            target = L("E4"),
        }));
        Assert.False(History.HasErrors);
        return Since(before);
    }

    [Fact]
    public async Task AnEffectiveSniperAttackOffersTheSniperCheckAndDecliningItResolvesNothingMore()
    {
        // A14.4 (p. 82; ruling R71.10): after the dr 2 pins de-h, the 8-1 and the 4-6-7 in E6 may take a Sniper Check: the German side's choice, worded by
        // the hex. Declined, nothing more happens, and nobody is TI.
        await Setup(null, TheGermansInE6());
        var sniped = Assert.Single(await FireCallingTheSniper(PinsTheHalfSquad));
        Assert.Equal(("de-h", ScenarioA1Sniper.Pinned), (sniped.Unit, sniped.Result));
        var choice = Current.Choice!;
        Assert.Equal((ChoicePending.SniperCheck, "german"), (choice.Kind, choice.Side));
        Assert.Contains("[E6]", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        Assert.DoesNotContain("de-", choice.Resume.ToString(), StringComparison.Ordinal);

        var before = Revision;
        Committed(await Choose(ChoicePending.Decline, NoRoll()));
        Assert.Null(Current.Choice);
        Assert.Empty(Since(before).Select(item => item.Payload).OfType<SniperChecked>());
        Assert.False(Is(Current.Unit("de-l")!, "asl:ti"));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task TakingTheSniperCheckEliminatesTheSniperAndReducesItsSan()
    {
        // A14.4 (p. 83): the 8-1 directing the 4-6-7 is -1 and the squad -2; the DR 3 + 2 - 3 = 2 eliminates the Russian Sniper, so the Russian SAN of 6 is 5,
        // and the counter stays. Both participants are TI; the pinned HS took no part.
        await Setup(null, TheGermansInE6());
        await FireCallingTheSniper(PinsTheHalfSquad);
        var before = Revision;
        Committed(await Choose(ChoicePending.Take, Then(3, 3, 2)));
        var check = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperChecked>());
        Assert.Equal(TheLeaderAndTheSquad, check.Units);
        Assert.Equal((-3, ScenarioA1Sniper.SniperEliminated), (check.Drm, check.Result));
        Assert.Equal(new SanReduced("russian", ScenarioA1Sniper.SniperCheckCause), Assert.Single(Since(before).Select(item => item.Payload).OfType<SanReduced>()));
        Assert.Equal(1, Current.Side("russian")!.SanReductions);
        Assert.Equal(InstanceStatus.Active, Current.Find("rs")!.Status);
        Assert.True(Is(Current.Unit("de-l")!, "asl:ti"));
        Assert.True(Is(Current.Unit("de-s")!, "asl:ti"));
        Assert.False(Is(Current.Unit("de-h")!, "asl:ti"));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ASniperCheckOfThreePinsTheSniper()
    {
        // A14.4 (p. 82): "A Sniper Check Final DR of 3 pins the Sniper": 3 + 3 - 3.
        await Setup(null, TheGermansInE6());
        await FireCallingTheSniper(PinsTheHalfSquad);
        Committed(await Choose(ChoicePending.Take, Then(3, 3, 3)));
        Assert.True(Is(Current.Find("rs")!, Conditions.Pinned));
        Assert.Equal(0, Current.Side("russian")!.SanReductions);
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AtSanTwoTheSniperCheckRemovesTheCounterAndALaterDrOfTwoCallsNothing()
    {
        // A14.1, A14.4 (pp. 82, 83; ruling R71.9): the Russian SAN is 2; g1's IFT DR 1 + 1 calls the Sniper (r1's K/ result and its MC, 1 + 2, come first).
        // The Sniper Check eliminates the Sniper: the SAN is 1, and its counter is removed. g2's later IFT DR of 2 calls no Sniper.
        await Setup(null, 2, [], [.. TheGermansInE6(), Squad("g2", "E2", "german")]);
        await Fire("g1", [1, 1, 1, 2, .. PinsTheHalfSquad]);
        Assert.Equal(ChoicePending.SniperCheck, Current.Choice?.Kind);
        Committed(await Choose(ChoicePending.Take, Then(3, 3, 2)));
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("rs")!.Status);
        Assert.True(ScenarioA1Sniper.CounterRemoved(ScenarioA1Sniper.ActualSan(2, false, Current.Side("russian")!.SanReductions)));
        Assert.Empty((await Fire("g2", 1, 1)).Select(item => item.Payload).OfType<SniperAttacked>());
    }

    [Fact]
    public async Task ASnipersOneOnTheEnemySniperReducesItsSanAndAtTwoRemovesItsCounter()
    {
        // A14.3, A14.4 (ruling R71.9): the Russian Sniper's dr 1 on the German Sniper counter alone in E6 eliminates the German Sniper; the German SAN of 2 is
        // 1, so the counter is removed.
        await Setup(2, Sniper("gs", "E6", "german"));
        var sniped = Assert.Single(await FireCallingTheSniper(1, 1, 1));
        Assert.Equal(("gs", ScenarioA1Sniper.SniperEliminated), (sniped.Unit, sniped.Result));
        Assert.Equal(1, Current.Side("german")!.SanReductions);
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("gs")!.Status);
    }

    [Fact]
    public async Task AtNightASniperCheckNeverTakesTheSanBelowThePrintedSoTheCounterStays()
    {
        // E1.76 (p. 225): at night the Russian printed SAN of 2 is 4 (g1's IFT DR 1 + 3 calls it; r1's MC, 1 + 2, comes first). The Sniper Check eliminates the
        // Sniper: the SAN is 3, never below the printed 2, and the counter stays, where by day it would leave.
        await Setup(null, 2, ["night:3"], TheGermansInE6());
        await Fire("g1", [1, 3, 1, 2, .. PinsTheHalfSquad]);
        Assert.Equal(ChoicePending.SniperCheck, Current.Choice?.Kind);
        Committed(await Choose(ChoicePending.Take, Then(3, 3, 2)));
        Assert.Equal(1, Current.Side("russian")!.SanReductions);
        Assert.Equal(3, ScenarioA1Sniper.ActualSan(2, Current.Night, 1));
        Assert.Equal(InstanceStatus.Active, Current.Find("rs")!.Status);
        Assert.False(History.HasErrors);
    }

    // Increment 4c: the Sniper counter at setup (A14.01, A14.2; ruling R71.11). The card games are The Guards Counterattack on board 01 with its real terrain:
    // the Germans set up first, the Russians move first, so the Germans are the DEFENDER; both SANs are 6.
    private const string Guards = "guards-counterattack";

    private GamePlanner CardPlanner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog]);

    private GameState CardState => CardPlanner().Replay(store.Read(Scope)!.Events).Current!;

    private async Task<PlayResult> CardAct(Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] = $"card-{Revision}";
        node["expectedRevision"] = Revision;
        if (Revision == 0)
        {
            node["start"] = JsonSerializer.SerializeToNode(new Dictionary<string, object?>
            {
                ["label"] = Guards,
                ["catalog"] = $"asl-scenario-a1@{LiveFire.CatalogVersion}",
                ["boards"] = Bd01,
                ["firstSide"] = null,
                ["sides"] = Array.Empty<object>(),
                ["scenario"] = new
                {
                    id = Guards,
                    sha256 = ScenarioCards.Sha256(Guards),
                    title = Guards
                },
            });
        }

        return await Commit(new GamePlay(CardPlanner(), store, new NullAudit(), roller: Then(6)), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> PlaceSniper(string side, string at) => CardAct(GameActions.Setup, new
    {
        sniper = new
        {
            side,
            at
        }
    });

    /// <summary>
    /// A group's whole OB on board 01, as a player sets it up (pass 19's model): each line in its area, or the group's building areas, squads three to a hex,
    /// SMC with the first squad, SW held by a squad of the area.
    /// </summary>
    private static Dictionary<string, object>[] Ob(ScenarioCard card, string side, int groupIndex)
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
                    placements.Add(new()
                    {
                        ["id"] = unitId,
                        ["kind"] = definition.Kind,
                        ["definition"] = line.Definition,
                        ["side"] = side,
                        ["holding"] = new
                        {
                            holder = holders[areas[0].Id],
                            role = "possessed"
                        },
                        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
                    });
                    continue;
                }

                var hex = definition.Kind == "asl:squad" ? hexes.First(item => squadsAt.GetValueOrDefault(item) < 3) : hexes[0];
                if (definition.Kind == "asl:squad")
                {
                    squadsAt[hex] = squadsAt.GetValueOrDefault(hex) + 1;
                    holders.TryAdd(areas[0].Id, unitId);
                }

                var unit = Unit(unitId, definition.Kind, line.Definition, hex, side);
                unit["group"] = id;
                placements.Add(unit);
            }
        }

        return [.. placements];
    }

    /// <summary>The Guards Counterattack with both sides set up: the Germans' OB, then the Russians' two groups.</summary>
    private async Task GuardsSetUp()
    {
        var card = ScenarioCards.Read(Guards, Catalog)!.Card!;
        Committed(await CardAct(GameActions.Setup, new
        {
            placements = Ob(card, "german", 0)
        }));
        Committed(await CardAct(GameActions.Setup, new
        {
            placements = (Dictionary<string, object>[])[.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]
        }));
        Assert.True(CardPlanner().CardSetup(CardState, new HashSet<string>())!.Complete);
    }

    [Fact]
    public async Task ACardGameStartsPlayOnlyOnceEachSideHasPlacedItsSniperCounterTheDefenderFirst()
    {
        // A14.01, A14.2 (p. 82; ruling R71.11): every OB with a SAN of 2 or more has a Sniper; "Prior to the start of play, each player (DEFENDER first)
        // places one friendly Sniper Counter". No counter is placed while a group still sets up.
        var card = ScenarioCards.Read(Guards, Catalog)!.Card!;
        Committed(await CardAct(GameActions.Setup, new
        {
            placements = Ob(card, "german", 0)
        }));
        Assert.StartsWith("play.sniper-placement: a Sniper counter is placed once both sides have set up",
            Assert.Single((await PlaceSniper("german", L("A1"))).Reasons), StringComparison.Ordinal);
        Committed(await CardAct(GameActions.Setup, new
        {
            placements = (Dictionary<string, object>[])[.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]
        }));
        Assert.Equal(["german", "russian"], CardPlanner().SniperPlacementsDue(CardState));

        // Play waits for the German counter, and the Russians wait for the Germans; a counter typed into the placements is refused in a card's game.
        Assert.StartsWith("play.setup-incomplete: the german side has a SAN of 2 or more and places its Sniper counter before play starts",
            Assert.Single((await CardAct(GameActions.AdvancePhase, new
            {
            })).Reasons), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: the german side, the DEFENDER, places its Sniper counter first",
            Assert.Single((await PlaceSniper("russian", L("A1"))).Reasons), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: in a game from a card the Sniper counter is placed in the setup's Sniper step",
            Assert.Single((await CardAct(GameActions.Setup, new
            {
                placements = new[] { Sniper("gs", "A1", "german") }
            })).Reasons), StringComparison.Ordinal);

        // A hex a German unit holds is no hex for it ("an unoccupied hex").
        var germanAt = CardState.Location("german-1-1")!.Location;
        Assert.StartsWith($"play.sniper-placement: [{germanAt.Hex}] is no hex for the german Sniper counter",
            Assert.Single((await PlaceSniper("german", germanAt.ToString())).Reasons), StringComparison.Ordinal);

        // The German counter goes in a hex the planner offers, in public; then the Russians place theirs, and play starts.
        var german = CardPlanner().SniperPlacements(CardState, "german")[0];
        var before = Revision;
        Committed(await PlaceSniper("german", german.ToString()));
        var placed = Assert.Single(Since(before));
        Assert.Null(placed.Visibility);
        Assert.Equal(("asl:sniper", "german"), (((InstanceCreated)placed.Payload).Instance.Kind, ((InstanceCreated)placed.Payload).Instance.Side));
        Assert.Equal(german, CardState.Location("sniper-german")!.Location);
        Assert.StartsWith("play.setup-incomplete: the russian side", Assert.Single((await CardAct(GameActions.AdvancePhase, new
        {
        })).Reasons), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: the german side has no Sniper counter to place",
            Assert.Single((await PlaceSniper("german", german.ToString())).Reasons), StringComparison.Ordinal);
        Committed(await PlaceSniper("russian", CardPlanner().SniperPlacements(CardState, "russian")[0].ToString()));
        Assert.Empty(CardPlanner().SniperPlacementsDue(CardState));
        Committed(await CardAct(GameActions.AdvancePhase, new
        {
        }));
        Assert.True(CardState.SetupClosed);
        Assert.False(CardPlanner().Replay(store.Read(Scope)!.Events).HasErrors);
        Assert.Empty(GamePlanner.SniperCounterMissing(CardState));
    }

    [Fact]
    public async Task ACardsSniperHexesAreTheUnoccupiedPlayableHexesNearTheMostEnemyHexes()
    {
        // A14.2 (p. 82; ruling R71.11): the German counter goes in an unoccupied hex within six hexes of six or more Russian-occupied hexes, else five, and so
        // on, inside the card's playable area (hexrows A to P). Each hex offered meets the best count any unoccupied playable hex has, at most six.
        await GuardsSetUp();
        var state = CardState;
        var card = ScenarioCards.Read(Guards, Catalog)!.Card!;
        var board = Board01Fixture.Handle();
        var occupied = state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location.Hex).ToHashSet();
        HexName[] russian = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == "russian" && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location.Hex).Distinct()];
        var counts = board.Geometry.Hexes().Select(board.Geometry.NameOf).Where(hex => !occupied.Contains(hex) && ScenarioCards.Playable(card, new BoardLocation(BoardRef.Parse("bd01"), hex, 0)))
            .ToDictionary(hex => hex, hex => russian.Count(other => board.Distance(hex, other) <= 6));
        var tier = Math.Min(6, counts.Values.Max());
        var offered = CardPlanner().SniperPlacements(state, "german");
        Assert.Equal(counts.Where(item => item.Value >= tier).Select(item => item.Key.ToString()).Order(StringComparer.Ordinal),
            offered.Select(at => at.Hex.ToString()).Order(StringComparer.Ordinal));
        Assert.All(offered, at => Assert.Equal(0, at.Level));
    }

    [Fact]
    public async Task AGameMadeWithoutACardPlacesItsSniperByHandUnderTheSameCheck()
    {
        // A14.2 (ruling R71.11): the German 4-6-7 in E3 is the one enemy unit, so the Russian counter goes within six hexes of it: E4 holds the Russian
        // 4-4-7, and L3 is seven hexes from E3; E7 is the hex every other test of this class uses.
        Dictionary<string, object>[] units = [Squad("g1", "E3", "german"), Squad("r1", "E4", "russian")];
        Assert.StartsWith("play.sniper-placement: [E4] is no hex for the russian Sniper counter",
            Assert.Single((await FirstSetup(null, 6, [], [.. units, Sniper("rs", "E4", "russian")])).Reasons), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: [L3] is no hex for the russian Sniper counter: it goes in an unoccupied hex within six hexes of 1 or more enemy-occupied hexes",
            Assert.Single((await FirstSetup(null, 6, [], [.. units, Sniper("rs", "L3", "russian")])).Reasons), StringComparison.Ordinal);
        Committed(await FirstSetup(null, 6, [], [.. units, Sniper("rs", "E7", "russian")]));
        Assert.Empty(CardPlanner().SniperPlacementsDue(Current));

        // The Sniper step is a card's: in this game the counter is placed by hand. Once play has started no counter is given (A14.01).
        Assert.StartsWith("play.sniper-placement: in a game made without a card",
            Assert.Single((await Do(GameActions.Setup, NoRoll(), new
            {
                sniper = new
                {
                    side = "german",
                    at = L("E7")
                }
            })).Reasons), StringComparison.Ordinal);
        await Advance();
        Assert.StartsWith("play.setup-closed",
            Assert.Single((await Do(GameActions.Setup, NoRoll(), new
            {
                sniper = new
                {
                    side = "german",
                    at = L("E7")
                }
            })).Reasons), StringComparison.Ordinal);
        Assert.Empty(GamePlanner.SniperCounterMissing(Current));
    }

    [Fact]
    public async Task ASnipersHexesCountTheEnemyQuestionMarksItsSideSeesAndNotTheHiddenUnits()
    {
        // A14.2 (p. 82; ruling R71.11): "enemy-occupied (including enemy "?" ...) hexes", counted from the Russian view: the concealed German squad in J3 counts,
        // so the counter goes within six hexes of both E3 and J3; the hidden German squad in G5 is not on the board as the Russians see it, so it neither
        // counts nor occupies G5.
        Committed(await FirstSetup(null, 6, [], [Squad("g1", "E3", "german"), Squad("g2", "J3", "german", Conditions.Concealed), Squad("g3", "G5", "german", Conditions.Hidden),
            Squad("r1", "E4", "russian")]));
        var board = Board();
        HexName[] taken = [HexName.Parse("E3"), HexName.Parse("J3"), HexName.Parse("E4")];
        var expected = board.Geometry.Hexes().Select(board.Geometry.NameOf)
            .Where(hex => !taken.Contains(hex) && board.Distance(hex, HexName.Parse("E3")) <= 6 && board.Distance(hex, HexName.Parse("J3")) <= 6)
            .Select(hex => hex.ToString()).Order(StringComparer.Ordinal).ToArray();
        var offered = Planner().SniperPlacements(Current, "russian").Select(at => at.Hex.ToString()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, offered);
        Assert.Contains("G5", offered);
    }

    [Fact]
    public async Task AGameWhoseSetupClosedBeforeThePassIsGivenNoSniperCounterAndSaysSo()
    {
        // A14.01 (ruling R71.11): The Guards Counterattack as it was played (the fixture guards-dl-01) started with no Sniper counter; it replays as it did, owes
        // none, takes none in a setup step, and the page's line says both sides make no Sniper attacks.
        var tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");
        var file = Path.Combine(root, tenant.ToString("N"), "guards-dl-01.game.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "guards-dl-01.game.json"), file);
        var planner = CardPlanner();
        var scope = new GameScope(tenant, "guards-dl-01");
        var events = store.Read(scope)!.Events;
        var history = planner.Replay(events);
        Assert.False(history.HasErrors);
        Assert.DoesNotContain(history.Current!.Entities, item => item.Kind == "asl:sniper");
        Assert.Empty(planner.SniperPlacementsDue(history.Current!));
        Assert.Equal(["german", "russian"], GamePlanner.SniperCounterMissing(history.Current!));
    }
}
