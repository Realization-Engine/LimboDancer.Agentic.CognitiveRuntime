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
    private async Task Setup(int? germanSan, params Dictionary<string, object>[] placements)
    {
        Dictionary<string, object>[] all = [Squad("g1", "E3", "german"), Squad("r1", "E4", "russian"), Sniper("rs", "E7", "russian"), .. placements];
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
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
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, san = germanSan, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, san = 6, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements = all,
        })));
        await Advance();
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
}
