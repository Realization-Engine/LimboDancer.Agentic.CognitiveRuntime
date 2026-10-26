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
    private static readonly string[] TheCrewTheTruckOrRandomSelection = ["crew:de-t", "vehicle:de-t", ScenarioA1Sniper.RandomSelection];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass71-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly HashSet<(string, string)> blocked = [];
    private readonly HashSet<string> upstairs = new(StringComparer.Ordinal);

    // The hidden-information fixes: the hexes a test reads as Grain, in season in July, where a vehicle may set up under "?" (A12.2, B15.6; ruling R6.7).
    private readonly HashSet<string> grain = new(StringComparer.Ordinal);
    private static readonly TerrainType Grain = new() { Code = 3, Name = "Grain", Category = LosCategory.Open };

    // Fix round B: the hexes whose ground level a test reads as Woods, TEM +1 (B13.3), for A14.21's TEM comparison.
    private readonly HashSet<string> woods = new(StringComparer.Ordinal);
    private static readonly TerrainType Woods = new() { Code = 4, Name = "Woods", Category = LosCategory.Woods };

    // The scenario's year (increment 4e: Riders need 1943, D6.2).
    private int year = 1942;

    public BacklogPass71Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with a level 1 in the hexes a test names (increment 4d: a hex of two Locations).</summary>
    private BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var type = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, grain.Contains(text) ? Grain : woods.Contains(text) ? Woods : type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            LocationFacts[] locations = upstairs.Contains(text) ? [center, new LocationFacts(1, type, null)] : [center];
            hexes.Add(new HexFacts(name, index, 0, false, center, locations, hexsides, null, CenterTerrainSource.CenterSample));
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

    private static Dictionary<string, object> Vehicle(string id, string definition, string hex, bool buttonedUp, string facing = "east", string side = "german") => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = facing },
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
                ["scenarioYear"] = year,
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

    /// <summary>
    /// The German fire that calls the Russian Sniper: IFT DR 3 + 3 (the Russian SAN), the 4-4-7's MC 3 + 3, then the Sniper's dr; an effective one stops at the
    /// Sniper player's choice (increment 4d, ruling R71.12), answered "attack" with the rest of the Sniper's dice given.
    /// </summary>
    private async Task<SniperAttacked[]> FireCallingTheSniper(params int[] sniperDice)
    {
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, [3, 3, 3, 3, sniperDice[0]]), new
        {
            firers = G1,
            target = L("E4"),
        }));
        await Attack(sniperDice[1..]);
        Assert.False(History.HasErrors);
        return [.. Since(before).Select(item => item.Payload).OfType<SniperAttacked>()];
    }

    /// <summary>The Sniper player's answer to the choice to forfeit, when it waits: "attack", the attack's next dice given (A14.2; ruling R71.12).</summary>
    private async Task Attack(params int[] dice)
    {
        if (Current.Choice is { Kind: ChoicePending.Sniper } choice && choice.Options.Contains(ScenarioA1Sniper.Attack))
        {
            Committed(await Choose(ScenarioA1Sniper.Attack, Then(3, dice)));
        }
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
        // Passenger takes no dr: a vehicle and its PRC are never attacked by the same one. Increment 4d (A14.2 EXC; ruling R71.12): the Russian side might
        // have chosen the crew or the truck before Random Selection, and asks for Random Selection instead.
        await Setup(null, Vehicle("de-t", "attacker-truck", "E6", buttonedUp: false), Passenger("de-p", "de-t"));
        var before = Revision;
        Assert.Empty(await FireCallingTheSniper(1, 1, 1));
        Assert.Equal(TheCrewTheTruckOrRandomSelection, Current.Choice!.Options);
        Committed(await Choose(ScenarioA1Sniper.RandomSelection, Then(3, 1, 1, 6)));
        var sniped = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
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
        // leader, whose dr 1 eliminates him, and the 4-6-7 takes its LLMC. Its 3 + 3 calls the Sniper again, resolved at once (ruling R71.25): its dr 3 has no
        // effect.
        await Setup(null, Unit("de-l", "asl:leader", "attacker-leader-8-0", "E6", "german"), Squad("de-s", "E6", "german"));
        var before = Revision;
        var sniped = (await FireCallingTheSniper(1, 1, 1, 6, 1))[0];
        Assert.Equal(("de-l", "eliminated"), (sniped.Unit, sniped.Result));
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("de-l")!.Status);
        Assert.Single(Since(before).Select(item => item.Payload).OfType<MoraleCheckResolved>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AConcealedSquadPinnedByASniperKeepsItsQuestionMarkOnlyOutOfEveryGoodOrderEnemyLos(bool outOfLos)
    {
        // A12.14 (p. 77; ruling R71.8): the concealed German 4-6-7 in E6 is pinned by the dr 2. Out of the 4-4-7's LOS it keeps its "?", and the public record
        // names no unit (ruling R71.22: its result is the neutral one, and its owner's twin names it); in that LOS it loses it.
        if (outOfLos)
        {
            blocked.Add(("E4", "E6"));
        }

        await Setup(null, Squad("de-c", "E6", "german", Conditions.Concealed));
        var sniped = await FireCallingTheSniper(2, 1, 1);
        Assert.Equal(outOfLos ? KeptPin : LostPin, sniped.Select(item => (item.Unit, item.Result)));
        var squad = Current.Unit("de-c")!;
        Assert.True(Is(squad, Conditions.Pinned));
        Assert.Equal(outOfLos, Is(squad, Conditions.Concealed));
    }

    // The hidden-information fixes (rulings R71.18 to R71.22): each test reads the records as the side that must not see them.

    private static readonly (string?, string)[] KeptPin = [(null, ScenarioA1Sniper.ConcealedKept), ("de-c", ScenarioA1Sniper.Pinned)];
    private static readonly (string?, string)[] LostPin = [("de-c", ScenarioA1Sniper.Pinned)];
    private static readonly (string?, string)[] KeptStun = [(null, ScenarioA1Sniper.ConcealedKept), ("de-tk", ScenarioA1Sniper.Stunned)];
    private static readonly (string?, string)[] KeptNoTarget = [(null, ScenarioA1Sniper.ConcealedKept), ("de-ht", ScenarioA1Sniper.NoEffect)];
    private static readonly string[] German = ["german"];
    private static readonly string[] Russian = ["russian"];

    /// <summary>Every event since <paramref name="before"/> the side given may read names none of <paramref name="ids"/> (its payload as JSON).</summary>
    private void NothingNames(string side, long before, params string[] ids)
    {
        var perspective = Current.Perspectives.Single(item => item.Name == side);
        foreach (var item in Since(before).Where(item => item.IsVisibleTo(perspective)))
        {
            var text = JsonSerializer.Serialize(item.Payload, item.Payload.GetType());
            foreach (var id in ids)
            {
                Assert.False(text.Contains(id, StringComparison.Ordinal), $"The {side} side reads {id} in {item.Type}: {text}");
            }
        }
    }

    /// <summary>A German vehicle placed under "?" (A12.2), in a hex the test reads as Grain.</summary>
    private static Dictionary<string, object> Concealed(Dictionary<string, object> vehicle, bool buttonedUp)
    {
        vehicle["conditions"] = new Dictionary<string, bool> { [Conditions.ButtonedUp] = buttonedUp, [Conditions.Concealed] = true };
        return vehicle;
    }

    [Fact]
    public async Task AConcealedTargetThatKeepsItsQuestionMarkIsNamedWithItsResultToItsOwnerAlone()
    {
        // A12.14, A14.23 (ruling R71.22): out of every enemy LOS the concealed 4-6-7 in E6 is pinned by the dr 2 and keeps its "?": the public record carries
        // the neutral result; its twin, which names the unit and the pin, and the pin itself are the German side's. The Russian side reads nothing of de-c.
        blocked.Add(("E4", "E6"));
        await Setup(null, Squad("de-c", "E6", "german", Conditions.Concealed));
        var before = Revision;
        await FireCallingTheSniper(2, 1, 1);
        var records = Since(before).Where(item => item.Payload is SniperAttacked).ToArray();
        Assert.Equal(KeptPin, records.Select(item => (((SniperAttacked)item.Payload).Unit, ((SniperAttacked)item.Payload).Result)));
        Assert.Null(records[0].Visibility);
        Assert.Equal(German, records[1].Visibility);
        Assert.Equal(German, Since(before).Single(item => item.Payload is ConditionsChanged { Id: "de-c" }).Visibility);
        NothingNames("russian", before, "de-c");
        Assert.True(Is(Current.Unit("de-c")!, Conditions.Pinned));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AConcealedTanksStunKeepsItsQuestionMarkAndIsItsOwnersAlone()
    {
        // A12.14, A14.3, D5.34 (rulings R71.8, R71.22; R2's finding 11, 4a's known item): out of every enemy LOS the concealed CE PzKpfw IIIH in E6 is Stunned
        // by the dr 2 and keeps its "?": its Stun is the German side's, and the Russian side reads only the neutral result.
        blocked.Add(("E4", "E6"));
        grain.Add("E6");
        await Setup(null, Concealed(Vehicle("de-tk", "attacker-tank", "E6", buttonedUp: false), buttonedUp: false));
        var before = Revision;
        var sniped = await FireCallingTheSniper(2, 1, 1);
        Assert.Equal(KeptStun, sniped.Select(item => (item.Unit, item.Result)));
        Assert.Equal(German, Since(before).Single(item => item.Payload is ConditionsChanged { Id: "de-tk" }).Visibility);
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Stunned));
        Assert.True(Is(Current.Unit("de-tk")!, Conditions.Concealed));
        NothingNames("russian", before, "de-tk");
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task TheSniperCheckIsOfferedWhileAQuestionMarkStaysThoughNoUnitThereMayTakeItAndTakingItRollsNothing()
    {
        // A14.4, A14.23, A12.141 (ruling R71.18): the pinned de-c may not take the Sniper Check, and E6 still holds its "?": the check is offered in public all
        // the same, so the offer tells the Russian side nothing. Taken, nothing is rolled or changed, and only the German proposal says why.
        blocked.Add(("E4", "E6"));
        await Setup(null, Squad("de-c", "E6", "german", Conditions.Concealed));
        await FireCallingTheSniper(2, 1, 1);
        var choice = Current.Choice!;
        Assert.Equal((ChoicePending.SniperCheck, "german"), (choice.Kind, choice.Side));
        Assert.Null(store.Read(Scope)!.Events.Single(item => item.EventId == choice.Event).Visibility);
        var before = Revision;
        var taken = await Choose(ChoicePending.Take, NoRoll());
        Committed(taken);
        Assert.Contains(taken.Reasons, reason => reason.StartsWith("play.sniper-check: no unit in [E6] may take", StringComparison.Ordinal));
        Assert.Equal("choice-made", Assert.Single(Since(before)).Type);
        Assert.Null(Current.Choice);
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task OnlyAnUnconcealedEnemySeerCostsTheQuestionMarkInASniperAttack()
    {
        // A12.14 (p. 78; ruling R71.21): the 4-4-7 in E4 has no LOS to E6, and the concealed 4-4-7 r2 in E8 has one. Only an unconcealed Good Order enemy
        // ground unit's LOS costs the "?", so the pinned de-c keeps it (A12.14's momentary forfeiture is not offered in a Sniper attack: a deviation).
        blocked.Add(("E4", "E6"));
        await Setup(null, Squad("de-c", "E6", "german", Conditions.Concealed), Squad("r2", "E8", "russian", Conditions.Concealed));
        var sniped = await FireCallingTheSniper(2, 1, 1);
        Assert.Equal(KeptPin, sniped.Select(item => (item.Unit, item.Result)));
        Assert.True(Is(Current.Unit("de-c")!, Conditions.Concealed));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ASniperCheckSeenOnlyByAConcealedEnemyKeepsItsQuestionMarkAndItsDiceAreItsOwners()
    {
        // A12.141 (p. 78; rulings R71.21, R71.22): Random Selection's 6, 1 gives the Sniper's dr 2 to the 2-4-7 de-h in E6 rather than the "?" stack, and pins
        // it; the concealed de-c takes the Sniper Check. Only the concealed r2 in E8 sees E6, so de-c keeps its "?": the check's record, its DR, and its TI are
        // the German side's, and the Russian side reads nothing of de-c.
        blocked.Add(("E4", "E6"));
        await Setup(null, Squad("de-c", "E6", "german", Conditions.Concealed), Unit("de-h", "asl:half-squad", "attacker-half-squad", "E6", "german"),
            Squad("r2", "E8", "russian", Conditions.Concealed));
        var sniped = Assert.Single(await FireCallingTheSniper(2, 1, 1, 6, 1));
        Assert.Equal(("de-h", ScenarioA1Sniper.Pinned), (sniped.Unit, sniped.Result));
        Assert.Equal(ChoicePending.SniperCheck, Current.Choice?.Kind);
        var before = Revision;
        Committed(await Choose(ChoicePending.Take, Then(3, 3, 3)));
        Assert.Equal(German, Since(before).Single(item => item.Payload is DiceRolled { Purpose: "sniper-check" }).Visibility);
        Assert.Equal(German, Since(before).Single(item => item.Payload is SniperChecked).Visibility);
        var squad = Current.Unit("de-c")!;
        Assert.True(Is(squad, Conditions.Concealed));
        Assert.True(Is(squad, "asl:ti"));
        NothingNames("russian", before, "de-c");
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AConcealedTrucksPassengerCrewAndTruckAreOneQuestionMarkAndWhatOneLosesTheyAllLose()
    {
        // A12.2, A14.23 (pp. 79, 82; ruling R71.20): the concealed German truck in E6 and its 4-6-7 Passenger, not under a "?" of its own, are one "?"
        // candidate: no choice before Random Selection, and the stack declares its three targets. Random Selection's 6, 1, 1 within it picks the Passenger,
        // whose dr 1 breaks it; it loses its "?", and so does the truck.
        grain.Add("E6");
        await Setup(null, Concealed(Vehicle("de-t", "attacker-truck", "E6", buttonedUp: false), buttonedUp: false), Passenger("de-p", "de-t"));
        var before = Revision;
        var sniped = Assert.Single(await FireCallingTheSniper(1, 1, 1, 6, 1, 1));
        Assert.Equal(("de-p", ScenarioA1Sniper.Broken), (sniped.Unit, sniped.Result));
        Assert.Equal(3, Since(before).Select(item => item.Payload).OfType<DiceRolled>().Single(item => item.Purpose == "sniper-random-selection").Count);
        Assert.True(Is(Current.Unit("de-p")!, Conditions.Broken));
        Assert.False(Is(Current.Unit("de-t")!, Conditions.Concealed));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AConcealedButtonedUpHalftrackIsStillOneQuestionMarkCandidateAndIsAttackedToNoEffect()
    {
        // A14.21, A14.23 (ruling R71.20): the BU SPW 251/1 in E6 holds no eligible target, which its "?" hides from the Russian side: E6 is the target hex, the
        // stack declares none, and the attack has no effect; it keeps its "?", and g1 in E3 is not attacked. The public record is the neutral one, its twin
        // the German side's, and the Russian side reads nothing of the halftrack or its Passenger.
        grain.Add("E6");
        await Setup(null, Concealed(Vehicle("de-ht", "attacker-halftrack", "E6", buttonedUp: true), buttonedUp: true), Passenger("de-p", "de-ht"));
        var before = Revision;
        var sniped = await FireCallingTheSniper(1, 1, 1);
        Assert.Equal(KeptNoTarget, sniped.Select(item => (item.Unit, item.Result)));
        Assert.Equal(L("E6"), Current.Location("rs")!.Location.ToString());
        Assert.False(Is(Current.Unit("g1")!, Conditions.Broken));
        Assert.True(Is(Current.Unit("de-ht")!, Conditions.Concealed));
        NothingNames("russian", before, "de-ht", "de-p");
        Assert.False(History.HasErrors);
    }

    // Increment 4b: the SAN as state and Sniper Check (A14.4, E1.76; rulings R71.9, R71.10).

    /// <summary>The German 8-1 de-l, 4-6-7 de-s, and 2-4-7 de-h in E6, the Sniper's target hex.</summary>
    private static Dictionary<string, object>[] TheGermansInE6() =>
        [Unit("de-l", "asl:leader", "attacker-leader-8-1", "E6", "german"), Squad("de-s", "E6", "german"), Unit("de-h", "asl:half-squad", "attacker-half-squad", "E6", "german")];

    // The Sniper's dr 2 and Random Location DR (north, one hex) find E6; Random Selection's 6, 1, 1 picks de-h (the first by id), which the dr 2 pins.
    private static readonly int[] PinsTheHalfSquad = [2, 1, 1, 6, 1, 1];
    private static readonly string[] TheLeaderAndTheSquad = ["de-l", "de-s"];

    // CA1861: the causes of the 8-1's and the 4-6-7's Sniper Check DRM of -3 (A14.4).
    private static readonly string[] TheCheckCauses = ["leadership -1", "2 HS equivalents -2"];

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

        // R4's finding 2: the answering side is offered the check with its takers and its DRM's causes, as the record then carries them (R4's finding 3).
        var offer = Planner().SniperCheckOffer(Current, Current.Choice!);
        Assert.Equal(TheLeaderAndTheSquad, offer.Units);
        Assert.Equal(-3, offer.Drm);
        Assert.Equal(TheCheckCauses, offer.Causes);
        var before = Revision;
        Committed(await Choose(ChoicePending.Take, Then(3, 3, 2)));
        var check = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperChecked>());
        Assert.Equal(TheLeaderAndTheSquad, check.Units);
        Assert.Equal((-3, ScenarioA1Sniper.SniperEliminated), (check.Drm, check.Result));
        Assert.Equal(TheCheckCauses, check.DrmCauses);
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
        await Fire("g1", [1, 1, 1, 2, PinsTheHalfSquad[0]]);
        await Attack(PinsTheHalfSquad[1..]);
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
        await Fire("g1", [1, 3, 1, 2, PinsTheHalfSquad[0]]);
        await Attack(PinsTheHalfSquad[1..]);
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
        var byHand = Assert.Single((await FirstSetup(null, 6, [], [.. units, Sniper("rs", "E4", "russian")])).Reasons);
        Assert.StartsWith("play.sniper-placement: [E4] is no hex for the russian Sniper counter", byHand, StringComparison.Ordinal);

        // The table player's note on 4c: placed by hand, the counter is moved to such a hex; no setup step offers one.
        Assert.EndsWith("; place it in such a hex (A14.2; ruling R71.11)", byHand, StringComparison.Ordinal);
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

    // Increment 4d: the Sniper player's choices (A14.2, A14.21, A12.14; ruling R71.12). In each game the German fire's IFT DR of 6 calls the Russian Sniper in E7.

    private static readonly string[] DeC = ["de-c"];

    /// <summary>A hex beside E6, across one of its hexsides, by the board's geometry.</summary>
    private static string Beside(HexsideDirection side) => Board01Fixture.Handle().Neighbor(HexName.Parse("E6"), side)!.Value.ToString();

    /// <summary>The German fire with the Sniper's dr given; the game then waits on the Russian side's choice to attack or forfeit.</summary>
    private async Task<long> FireToTheForfeit(int dr)
    {
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 3, 3, 3, 3, dr), new
        {
            firers = G1,
            target = L("E4"),
        }));
        Assert.Equal((ChoicePending.Sniper, "russian", ScenarioA1Sniper.ForfeitStep), (Current.Choice?.Kind, Current.Choice?.Side, Current.Choice?.Resume.GetProperty("step").GetString()));
        return before;
    }

    private static readonly string[] AttackOrReposition = [ScenarioA1Sniper.Attack, ScenarioA1Sniper.Reposition];

    [Fact]
    public async Task AnEffectiveDrAsksTheSniperPlayerToAttackOrToForfeitAndRepositionHisCounterInPublic()
    {
        // A14.2 (p. 82; ruling R71.12): "it may be repositioned by the Sniper player (within the same placement requirements) if he forfeits an effective
        // Sniper attack dr ... prior to selecting the hex": the dr 2 stops before the Random Location DR, at the Russian side's public choice, which names no
        // German unit. Forfeited, the hexes offered are A14.2's placements of the moment, E7 aside; the counter moves to the one chosen, and no die is rolled.
        await Setup(null);
        var before = await FireToTheForfeit(2);
        var choice = Current.Choice!;
        Assert.Equal(AttackOrReposition, choice.Options);
        Assert.Null(store.Read(Scope)!.Events.Single(item => item.EventId == choice.Event).Visibility);
        Assert.Contains("dr of 2", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        Assert.DoesNotContain("g1", choice.Resume.ToString(), StringComparison.Ordinal);
        Assert.Empty(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());

        Committed(await Choose(ScenarioA1Sniper.Reposition, NoRoll()));
        var hexes = Current.Choice!;
        Assert.Equal(ScenarioA1Sniper.RepositionStep, hexes.Resume.GetProperty("step").GetString());

        // Ruling R71.19: the hexes are the Russian side's alone, its own hidden units shaping them (A12.3); the German side reads that it forfeits and
        // repositions (its public answer), and then the counter's new hex.
        Assert.Equal(Russian, store.Read(Scope)!.Events.Single(item => item.EventId == hexes.Event).Visibility);
        Assert.Equal(Planner().SniperPlacements(Current, "russian").Where(at => at.Hex.ToString() != "E7").Select(at => at.ToString()), hexes.Options);
        var to = hexes.Options[^1];
        Committed(await Choose(to, NoRoll()));
        var forfeited = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal((ScenarioA1Sniper.Forfeited, to, (string?)null), (forfeited.Result, forfeited.Target?.ToString(), forfeited.Unit));
        Assert.Equal(to, Current.Location("rs")!.Location.ToString());
        Assert.DoesNotContain(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), item => item.Purpose == "sniper-location");
        Assert.Null(Current.Choice);
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task APinnedSniperNeitherAttacksNorRepositions()
    {
        // A14.31 (p. 82): a pinned Sniper "forfeits all further attack and repositioning (14.2) opportunities during that Player Turn": no dr, no choice.
        var pinned = Sniper("rs", "E7", "russian");
        pinned["conditions"] = new Dictionary<string, bool> { [Conditions.Pinned] = true };
        Committed(await FirstSetup(null, 6, [], [Squad("g1", "E3", "german"), Squad("r1", "E4", "russian"), pinned]));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 3, 3, 3, 3, 1), new
        {
            firers = G1,
            target = L("E4"),
        }));
        Assert.Null(Current.Choice);
        Assert.Empty(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(L("E7"), Current.Location("rs")!.Location.ToString());
    }

    [Fact]
    public async Task EquidistantHexesOfEqualTemAreTheSniperPlayersChoiceAndNoQuestionMarkIsRemovedWhileItWaits()
    {
        // A14.21 (p. 82): the Random Location DR finds E6, empty; the German squads beside it to the north-east and north-west are equidistant, both Open
        // Ground, so "the Sniper player chooses which of those equidistant hexes to attack". A12.14 (p. 78): the owner may remove a "?" at any time "[EXC:
        // between when a Sniper target hex Random Location DR (14.2) is made and the Sniper counter is relocated]": the German "?" in E2 stays while the choice
        // waits. The north-west squad chosen, the dr 1 breaks it.
        var (east, west) = (Beside(HexsideDirection.NorthEast), Beside(HexsideDirection.NorthWest));
        await Setup(null, Squad("de-a", east, "german"), Squad("de-b", west, "german"), Squad("de-c", "E2", "german", Conditions.Concealed));
        await FireToTheForfeit(1);
        await Attack(1, 1);
        var choice = Current.Choice!;
        Assert.Equal((ChoicePending.Sniper, ScenarioA1Sniper.HexStep), (choice.Kind, choice.Resume.GetProperty("step").GetString()));
        Assert.Equal(new[] { L(east), L(west) }.Order(StringComparer.Ordinal), choice.Options);
        Assert.Contains(Since(0).Select(item => item.Payload).OfType<DiceRolled>(), item => item.Purpose == "sniper-location");

        // R4's finding 5: the choice says where the Random Location DR landed.
        Assert.Equal(L("E6"), choice.Resume.GetProperty("place").GetString());
        Assert.Contains("Random Location DR lands in [E6], which holds no eligible target", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        Assert.Equal(L("E7"), Current.Location("rs")!.Location.ToString());

        var refused = await Do(GameActions.RemoveConcealment, NoRoll(), new
        {
            unitIds = DeC,
        });
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.choice-pending: the russian side answers first", StringComparison.Ordinal));
        Assert.True(Is(Current.Unit("de-c")!, Conditions.Concealed));

        var before = Revision;
        Committed(await Choose(L(west), Then(3)));
        var sniped = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(("de-b", ScenarioA1Sniper.Broken, L(west)), (sniped.Unit, sniped.Result, sniped.Target?.ToString()));
        Assert.False(Is(Current.Unit("de-a")!, Conditions.Broken));
        Assert.Equal(L(west), Current.Location("rs")!.Location.ToString());
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task TheSniperPlayerChoosesTheLocationOfAHexOfTwo()
    {
        // A14.2 (p. 82): "If that new target hex Contains > one Location occupied by an eligible (14.22) enemy unit(s), the Sniper player selects which of
        // those Locations to attack": German squads at both levels of E6; the level 1 chosen, the dr 1 breaks the squad there, and not the one below.
        upstairs.Add("E6");
        var above = Squad("de-b", "E6", "german");
        above["position"] = new
        {
            at = "bd01:E6:1"
        };
        await Setup(null, Squad("de-a", "E6", "german"), above);
        await FireToTheForfeit(1);
        await Attack(1, 1);
        var choice = Current.Choice!;
        Assert.Equal(ScenarioA1Sniper.LocationStep, choice.Resume.GetProperty("step").GetString());
        Assert.Equal([L("E6"), "bd01:E6:1"], choice.Options);
        Assert.Contains("attacks [E6]", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);

        var before = Revision;
        Committed(await Choose("bd01:E6:1", NoRoll()));
        var sniped = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(("de-b", ScenarioA1Sniper.Broken, "bd01:E6:1"), (sniped.Unit, sniped.Result, sniped.Target?.ToString()));
        Assert.False(Is(Current.Unit("de-a")!, Conditions.Broken));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task BeforeRandomSelectionTheSniperPlayerMayChooseAnUnarmoredVehicle()
    {
        // A14.2 EXC (p. 82): "prior to Random Selection, the Sniper may choose ... a Vulnerable Inherent Crew; or an unarmored vehicle": the manned German truck
        // in E6 is two candidates, its crew and itself. The truck chosen, no Random Selection is rolled, and the dr 1 immobilizes it.
        await Setup(null, Vehicle("de-t", "attacker-truck", "E6", buttonedUp: false));
        var before = await FireToTheForfeit(1);
        await Attack(1, 1);
        Assert.Equal(TheCrewTheTruckOrRandomSelection, Current.Choice!.Options);
        Committed(await Choose("vehicle:de-t", NoRoll()));
        var sniped = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(("de-t", ScenarioA1Sniper.Immobilized), (sniped.Unit, sniped.Result));
        Assert.DoesNotContain(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), item => item.Purpose == "sniper-random-selection");
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Immobilized));
    }

    private static readonly string[] TheHalfSquadOrTheLeader = ["unit:de-h", "unit:de-l"];

    [Fact]
    public async Task TheSniperPlayerChoosesWhichTiedTargetTakesTheDrAndTheOtherTakesANewOne()
    {
        // A14.2 (p. 82): "If the Random Selection results in multiple targets, only one (Sniper's choice) is attacked by the initial Sniper attack dr; the others
        // are each subject to a new Sniper dr": Random Selection's 6, 6, 1 ties the 2-4-7 and the 8-1. The leader chosen takes the dr 2 and is wounded (Wound
        // Severity dr 1); the HS's own dr 3 has no effect.
        await Setup(null, TheGermansInE6());
        var before = await FireToTheForfeit(2);
        var sniperRoll = Since(before).Select(item => item.Payload).OfType<DiceRolled>().Single(item => item.Purpose == "sniper").Roll;
        await Attack(1, 1, 6, 6, 1);
        var choice = Current.Choice!;
        Assert.Equal(ScenarioA1Sniper.FirstStep, choice.Resume.GetProperty("step").GetString());
        Assert.Equal(TheHalfSquadOrTheLeader, choice.Options);

        Committed(await Choose("unit:de-l", Then(3, 3, 1)));
        var sniped = Since(before).Select(item => item.Payload).OfType<SniperAttacked>().ToArray();
        Assert.Equal(2, sniped.Length);
        Assert.Equal(("de-l", ScenarioA1Sniper.Wounded, sniperRoll, 2), (sniped[0].Unit, sniped[0].Result, sniped[0].Roll, sniped[0].Dr));
        Assert.Equal((3, ScenarioA1Sniper.NoEffect), (sniped[1].Dr, sniped[1].Result));
        Assert.NotEqual(sniperRoll, sniped[1].Roll);
        Assert.True(Is(Current.Unit("de-l")!, Conditions.Wounded));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task OfTwoSniperAttacksInOneStepTheSecondResolvesOnlyAfterTheFirstsChoice()
    {
        // A14.1 (p. 82): "The Sniper attack occurs ... not before resolving (14.3) all (if any) Sniper attacks made prior to it": g1's IFT DR of 6 calls the
        // Russian Sniper and r1's MC DR of 6 the German Sniper in I4 (both SANs are 6). The Russian dr 2 waits on its side's choice, and the German attack with
        // it; answered, the Russian Sniper pins g1 in E3, the closest German unit to E6, and then the German Sniper's dr 3 has no effect.
        await Setup(6, Sniper("gs", "I4", "german"));
        var before = await FireToTheForfeit(2);
        Assert.Empty(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal("gs", Current.Choice!.Resume.GetProperty("rest")[0].GetProperty("sniper").GetString());

        Committed(await Choose(ScenarioA1Sniper.Attack, Then(3, 1, 1, 3)));
        var sniped = Since(before).Select(item => item.Payload).OfType<SniperAttacked>().ToArray();
        Assert.Equal(["rs", "gs"], sniped.Select(item => item.Sniper));
        Assert.Equal(("g1", ScenarioA1Sniper.Pinned), (sniped[0].Unit, sniped[0].Result));
        Assert.Equal((3, ScenarioA1Sniper.NoEffect), (sniped[1].Dr, sniped[1].Result));
        Assert.Null(Current.Choice);
        Assert.False(History.HasErrors);
    }

    // Increment 4e: every A14.1 trigger, read by one reader once per planned step (ruling R71.13). Unless a test says otherwise, every die after those given is a
    // 4: a Sniper dr of 4 has no effect, and no check of 4 + 4 equals a SAN.

    private static readonly string[] R1 = ["r1"];
    private static readonly string[] G2 = ["g2"];

    /// <summary>The hex across one hexside of another, by board 01's geometry.</summary>
    private static string N(string hex, HexsideDirection side) => Board01Fixture.Handle().Neighbor(HexName.Parse(hex), side)!.Value.ToString();

    private SniperAttacked[] Sniped(long before) => [.. Since(before).Select(item => item.Payload).OfType<SniperAttacked>()];

    private async Task PassFire() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private Task<PlayResult> Drive(string kind, string? to = null, Dictionary<string, object>? options = null)
    {
        var arguments = new Dictionary<string, object> { ["vehicleId"] = "de-tk", ["kind"] = kind };
        if (to is not null)
        {
            arguments["to"] = to;
        }

        foreach (var (name, value) in options ?? [])
        {
            arguments[name] = value;
        }

        return Do(GameActions.MoveVehicle, NoRoll(), arguments);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task ResidualFpOnAMoveCallsTheSniperOnlyWhenItsDrEqualsTheEnemySan(int white, bool calls)
    {
        // A8.2, A14.1 (pp. 59, 82; ruling R71.13): in the German MPh g1 enters E2, and r1's First Fire at it (6 + 5) leaves Residual FP there; g2 entering E2
        // is attacked by it. That DR is the DEFENDER's (A8.2): the Russian 1 + 2 equals the German SAN of 3 and calls the German Sniper in I4; a 1 + 3 calls
        // none. Before the increment no move called a Sniper.
        await Setup(3, Squad("g2", "E3", "german"), Sniper("gs", "I4", "german"));
        await Advance();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2"),
        }));
        Committed(await Do(GameActions.Fire, Then(4, 6, 5), new
        {
            firers = R1,
            target = L("E2"),
        }));
        await PassFire();
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        var before = Revision;
        Committed(await Do(GameActions.Move, Then(4, 1, white), new
        {
            unitIds = G2,
            to = L("E2"),
        }));
        var residual = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(ScenarioA1FireCalculator.ResidualFire, residual.Facts.GetProperty("fireKind").GetString());
        var sniped = Sniped(before);
        Assert.Equal(calls ? 1 : 0, sniped.Length);
        if (calls)
        {
            Assert.Equal(("gs", residual.Rolls["attack"], ScenarioA1Sniper.NoEffect), (sniped[0].Sniper, sniped[0].Trigger, sniped[0].Result));
        }

        Assert.False(History.HasErrors);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public async Task AnOrdnanceToHitDrCallsTheSniperOnlyWhenItEqualsTheEnemySan(int white, bool calls)
    {
        // A14.1 (p. 82; ruling R71.13, which supersedes R55.30): "a TH ... Original DR": the German PzKpfw IIIH in E6, facing north, fires its MA at r1 in E4
        // in the PFPh; its To Hit DR 3 + 3 equals the Russian SAN of 6 and calls the Russian Sniper, a 3 + 4 none.
        await Setup(null, Vehicle("de-tk", "attacker-tank", "E6", buttonedUp: true, facing: "north-east"));
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Then(4, 3, white), new
        {
            gunId = "de-tk",
            target = L("E4"),
        }));
        var shot = Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();
        var sniped = Sniped(before);
        Assert.Equal(calls ? 1 : 0, sniped.Length);
        if (calls)
        {
            Assert.Equal(("rs", shot.Rolls["toHit"]), (sniped[0].Sniper, sniped[0].Trigger));
        }

        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task TheHeHitsCheckOfItsTargetCallsTheEnemySniper()
    {
        // A14.1 (ruling R71.13): the German MA's To Hit DR 3 + 4 hits r1 in E4; the hit's IFT DR 4 + 4 gives it a check, and r1's 2 + 3 equals the German SAN
        // of 5 and calls the German Sniper in I4. The check is keyed inside the hit's own rolls.
        await Setup(5, Vehicle("de-tk", "attacker-tank", "E6", buttonedUp: true, facing: "north-east"), Sniper("gs", "I4", "german"));
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Then(4, 3, 4, 4, 4, 2, 3), new
        {
            gunId = "de-tk",
            target = L("E4"),
        }));
        var shot = Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();
        Assert.True(shot.Rolls.ContainsKey("hit:checks:r1"), string.Join(", ", shot.Rolls.Keys));
        Assert.Equal(("gs", shot.Rolls["hit:checks:r1"]), (Assert.Single(Sniped(before)).Sniper, Sniped(before)[0].Trigger));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task TwoTriggersOfOneMoveAreResolvedInTheOrderOfTheirDrs()
    {
        // A14.1 (p. 82): g2 entering E2 is attacked by r1's Residual FP; that Russian DR, 3 + 3, calls the German Sniper (both SANs are 6), and g2's check
        // after it, 3 + 3, the Russian Sniper: the German attack resolves first, then the Russian.
        await Setup(6, Squad("g2", "E3", "german"), Sniper("gs", "I4", "german"));
        await Advance();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2"),
        }));
        Committed(await Do(GameActions.Fire, Then(4, 6, 5), new
        {
            firers = R1,
            target = L("E2"),
        }));
        await PassFire();
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        var before = Revision;
        Committed(await Do(GameActions.Move, Then(4, 3, 3, 3, 3), new
        {
            unitIds = G2,
            to = L("E2"),
        }));
        var residual = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.True(residual.Rolls.ContainsKey("checks:g2"), string.Join(", ", residual.Rolls.Keys));
        var sniped = Sniped(before);
        Assert.Equal(["gs", "rs"], sniped.Select(item => item.Sniper));
        Assert.Equal([residual.Rolls["attack"], residual.Rolls["checks:g2"]], sniped.Select(item => item.Trigger));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AFireLaneAttackOnALaterMoverCallsTheSniper()
    {
        // A9.22, A14.1 (ruling R71.13): r1's LMG Defensive First Fires at g1 entering the hex north-east of E4 and lays a Fire Lane one hex beyond; g2 then
        // enters that far hex and is attacked by the lane. Its Russian DR, 3 + 3, equals the German SAN of 6 and calls the German Sniper in I4.
        var near = N("E4", HexsideDirection.NorthEast);
        var far = N(near, HexsideDirection.NorthEast);
        Dictionary<string, object> lmg = new()
        {
            ["id"] = "rmg",
            ["kind"] = "asl:mg",
            ["definition"] = "defender-lmg",
            ["side"] = "russian",
            ["holding"] = new
            {
                holder = "r1",
                role = "possessed",
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        };
        await Setup(6, lmg, Squad("g2", N(far, HexsideDirection.North), "german"), Sniper("gs", "I4", "german"));
        await Advance();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L(near),
        }));
        Committed(await Do(GameActions.Fire, Then(4, 5, 4), new Dictionary<string, object>
        {
            ["firers"] = R1,
            ["target"] = L(near),
            ["weapons"] = new Dictionary<string, string[]> { ["r1"] = ["rmg"] },
            ["fireLane"] = new Dictionary<string, string> { ["weapon"] = "rmg", ["to"] = L(far) },
        }));
        Assert.Single(Current.FireLanes);
        await PassFire();

        // g1's move ended with its failed check, or ends now.
        var ended = await Do(GameActions.EndMove, NoRoll(), new
        {
        });
        Assert.True(ended.Outcome == PlayOutcome.Committed || ended.Reasons.Any(reason => reason.StartsWith("play.end-move: no stack is moving", StringComparison.Ordinal)),
            string.Join("; ", ended.Reasons));
        var before = Revision;
        Committed(await Do(GameActions.Move, Then(4, 3, 3), new
        {
            unitIds = G2,
            to = L(far),
        }));
        var lane = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.True(lane.Facts.GetProperty("fireLane").GetBoolean());
        Assert.Equal(("gs", lane.Rolls["attack"]), (Assert.Single(Sniped(before)).Sniper, Sniped(before)[0].Trigger));
        Assert.False(History.HasErrors);
    }

    private static readonly string[] SquadAndLeader = ["de-l", "g1"];

    [Fact]
    public async Task ASearchsLlmcInTheMphCallsTheSniper()
    {
        // A12.154, A10.2, A14.1 (ruling R71.13): g1 and the 8-1 move beside r1 and Search E4; the DEFENDER's Casualty dr of 1 strikes, Random Selection's 6, 1
        // picks the leader, and his Wound Severity dr of 6 eliminates him. g1's LLMC, 3 + 3, equals the Russian SAN of 6 and calls the Russian Sniper.
        var beside = N("E4", HexsideDirection.NorthWest);
        await Setup(null, Unit("de-l", "asl:leader", "attacker-leader-8-1", "E3", "german"));
        await Advance();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = SquadAndLeader,
            to = L(beside),
        }));
        await PassFire();
        var before = Revision;
        Committed(await Do(GameActions.Search, Then(4, 6, 1, 6, 1, 6, 3, 3), new
        {
            unitIds = SquadAndLeader,
            hexes = new[] { L("E4") },
        }));
        var llmc = Since(before).Select(item => item.Payload).OfType<MoraleCheckResolved>().Single();
        Assert.Equal(ScenarioA1MoraleCheck.Searching, llmc.Cause);
        Assert.Equal(("rs", llmc.Rolls.Single().Value), (Assert.Single(Sniped(before)).Sniper, Sniped(before)[0].Trigger));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ARidersBailOutNmcCallsTheSniper()
    {
        // D6.21, D6.24, A14.1 (ruling R71.13): the German tank in E6 faces east, so its MA's shot at r1 in E4 turns the turret and its 4-6-7 Rider Bails Out;
        // the To Hit DR 6 + 5 misses, and the Rider's NMC, 3 + 3, equals the Russian SAN of 6 and calls the Russian Sniper. The year is 1943 (D6.2).
        year = 1943;
        var rider = Squad("de-r", "B10", "german");
        rider["position"] = new Dictionary<string, string> { ["in"] = "de-tk", ["role"] = "rider" };
        await Setup(null, Vehicle("de-tk", "attacker-tank", "E6", buttonedUp: true), rider);
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Then(4, 6, 5, 3, 3), new
        {
            gunId = "de-tk",
            target = L("E4"),
        }));
        var bailed = Since(before).Select(item => item.Payload).OfType<RiderBailedOut>().Single();
        var sniped = Assert.Single(Sniped(before));
        Assert.Equal(("rs", bailed.Roll), (sniped.Sniper, sniped.Trigger));
        Assert.False(History.HasErrors);
    }

    /// <summary>The German tank in the hex south-west of E4 starts in the German MPh and enters r1's E4 with an OVR, the Russian passing each time.</summary>
    private async Task OverrunR1(int? germanSan, params Dictionary<string, object>[] placements)
    {
        await Setup(germanSan, [Vehicle("de-tk", "attacker-tank", N("E4", HexsideDirection.SouthWest), buttonedUp: true), .. placements]);
        await Advance();
        Committed(await Drive("start"));
        await PassFire();
        Committed(await Drive("enter", L("E4"), new()
        {
            ["overrun"] = true
        }));
        await PassFire();
    }

    [Fact]
    public async Task AVehicleOvrsIftDrCallsTheSniper()
    {
        // D7.1, A14.1 (ruling R71.13): the German tank overruns r1 in E4 in the German MPh; the OVR's IFT DR 3 + 3 equals the Russian SAN of 6 and calls the
        // Russian Sniper. Before the increment only an OVR resumed after a choice called it.
        await OverrunR1(null);
        var before = Revision;
        Committed(await Do(GameActions.Overrun, Then(4, 3, 3), new
        {
        }));
        var overrun = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(("rs", overrun.Rolls["attack"]), (Assert.Single(Sniped(before)).Sniper, Sniped(before)[0].Trigger));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task APaatcInTheMphCallsTheSniper()
    {
        // A11.6, D7.21, A14.1 (ruling R71.13): the OVR's Original 12 gives r1 a PTC, passed on 2 + 2, and malfunctions the MA (6, 1, 1; as pass 11's test). r1
        // then takes its PAATC for CC Reaction Fire: 3 + 4 equals the German SAN of 7 and calls the German Sniper in I4.
        await OverrunR1(7, Sniper("gs", "I4", "german"));
        Committed(await Do(GameActions.Overrun, Then(4, 6, 6, 2, 2, 6, 1, 1), new
        {
        }));
        var before = Revision;
        Committed(await Do(GameActions.VehicleCloseCombat, Then(4, 3, 4), new Dictionary<string, object>
        {
            ["location"] = L("E4"),
            ["attackers"] = R1,
        }));
        var paatc = Since(before).Select(item => item.Payload).OfType<PaatcTaken>().Single();
        Assert.Equal(("gs", paatc.Roll), (Assert.Single(Sniped(before)).Sniper, Sniped(before)[0].Trigger));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task APaatcInTheAphCallsNoSniper()
    {
        // A14.1 (p. 82): a Sniper is called "during any PFPh, MPh, DFPh or AFPh": g1's PAATC to advance onto the Russian tank in E2, 3 + 3, equals the Russian
        // SAN of 6 in the German APh, and calls nothing.
        await Setup(null, Vehicle("ru-tk", "defender-tank", "E2", buttonedUp: true, side: "russian"));
        await Advance(5);
        Assert.Equal("aph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.Advance, Then(4, 3, 3), new
        {
            unitIds = G1,
            to = L("E2"),
        }));
        Assert.Single(Since(before).Select(item => item.Payload).OfType<PaatcTaken>());
        Assert.Empty(Sniped(before));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AnInfantryOvrNtcCallsTheSniper()
    {
        // A4.15, A14.1 (p. 49, 82; ruling R71.13): g1 takes the NTC of an Infantry OVR of the lone Russian 7-0 beside it; its 3 + 3 equals the Russian SAN of 6
        // and calls the Russian Sniper.
        var lone = N("E3", HexsideDirection.NorthWest);
        await Setup(null, Unit("rl", "asl:leader", "defender-leader-7-0", lone, "russian"));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.InfantryOverrunNtc, Then(4, 3, 3), new Dictionary<string, object>
        {
            ["unitIds"] = G1,
            ["to"] = L(lone),
        }));
        var ntc = Since(before).Select(item => item.Payload).OfType<InfantryOverrunNtc>().Single();
        Assert.Equal(("rs", ntc.Roll), (Assert.Single(Sniped(before)).Sniper, Sniped(before)[0].Trigger));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ASnipersOwnLlmcCallsTheNextAttackBeforeItsSniperCheck()
    {
        // A14.1, A14.3 (p. 82; rulings R71.13, R71.25): the Russian Sniper's dr 1 eliminates the German 8-0 in E6, and the 4-6-7's LLMC, 3 + 3, equals the
        // Russian SAN of 6: it calls the Sniper again. An attack "is considered resolved only after the full resolution of all further Sniper attacks generated
        // by ... all LLMC", so the second attack (its dr 4 has no effect) comes before the first attack's Sniper Check (A14.4: "After resolving (14.3)"), which
        // keeps nothing more to resolve. Before the fix round the check came first.
        await Setup(null, Unit("de-l", "asl:leader", "attacker-leader-8-0", "E6", "german"), Squad("de-s", "E6", "german"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(4, [3, 3, 3, 3, 1]), new
        {
            firers = G1,
            target = L("E4"),
        }));
        var first = Current.Choice!.Resume.GetProperty("trigger").GetString()!;
        Committed(await Choose(ScenarioA1Sniper.Attack, Then(4, 1, 1, 6, 1, 3, 3)));
        var llmc = Since(before).Select(item => item.Payload).OfType<MoraleCheckResolved>().Single();
        var trigger = llmc.Rolls.Single().Value;
        var sniped = Sniped(before);
        Assert.Equal(2, sniped.Length);
        Assert.Equal(("de-l", ScenarioA1Sniper.Eliminated), (sniped[0].Unit, sniped[0].Result));
        Assert.Equal(("rs", trigger, 4), (sniped[1].Sniper, sniped[1].Trigger, sniped[1].Dr));
        var check = Current.Choice!;
        Assert.Equal((ChoicePending.SniperCheck, $"{ChoicePending.SniperCheck}:rs:{first}"), (check.Kind, check.Key));
        Assert.Equal(0, check.Resume.GetProperty("rest").GetArrayLength());

        Committed(await Choose(ChoicePending.Decline, NoRoll()));
        Assert.Equal(2, Sniped(before).Length);
        Assert.Null(Current.Choice);
        Assert.False(History.HasErrors);
    }

    // Increment 4f: wounds (A17.2, A17.11, D5.343); rulings R71.14, R71.15.

    private static readonly string[] Leader = ["de-l"];

    /// <summary>The Open Ground hexes from four hexes south-east of E6 to E6, by board 01's geometry: the German 8-1's path, 1 MF a hex.</summary>
    private static string[] PathToE6()
    {
        var path = new List<string> { "E6" };
        for (var step = 0; step < 4; step++)
        {
            path.Insert(0, N(path[0], HexsideDirection.SouthEast));
        }

        return [.. path];
    }

    /// <summary>
    /// The German 8-1 de-l, set up at the start of <see cref="PathToE6"/> with the placements given, moves alone in the German MPh along it for the steps
    /// given, the Russian passing his fire after each step but the last; the hex he ends in.
    /// </summary>
    private async Task<string> LeaderMoves(int steps, params Dictionary<string, object>[] placements)
    {
        var path = PathToE6();
        await Setup(null, [Unit("de-l", "asl:leader", "attacker-leader-8-1", path[0], "german"), .. placements]);
        await Advance();
        for (var step = 1; step <= steps; step++)
        {
            Committed(await Do(GameActions.Move, NoRoll(), new
            {
                unitIds = Leader,
                to = L(path[step]),
            }));
            if (step < steps)
            {
                await PassFire();
            }
        }

        Assert.Equal(steps, Current.Unit("de-l")!.MfSpent);
        return path[steps];
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public async Task ALeaderWoundedByDefensiveFirstFireAfterMoreThanThreeMfIsPinned(int steps, bool pinned)
    {
        // A17.2 (p. 85; ruling R71.14): "A SMC who becomes wounded (e.g., by Defensive First Fire) after already expending > 3 MF is pinned". r1 First Fires
        // at the moving 8-1 in the Open: 4 FP, DR 2 + 2 with -1 FFMO and -1 FFNAM is a Final 2, K/2; the Random Selection die and the Wound Severity dr of
        // 2 leave him wounded, and his 2MC, 2 + 2 + 2 against his wounded 7, passes. After 4 MF he is pinned; after 3 he is not.
        var at = await LeaderMoves(steps);
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(2), new
        {
            firers = R1,
            target = L(at),
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        var target = fire.Facts.GetProperty("targets")[0];
        Assert.Equal(pinned, target.TryGetProperty("woundPins", out var pins) && pins.GetBoolean());
        var leader = Current.Unit("de-l")!;
        Assert.True(Is(leader, Conditions.Wounded), fire.Resolution.ToString());
        Assert.Equal(pinned, Is(leader, Conditions.Pinned));
        Assert.False(Is(leader, Conditions.Broken));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ASniperWoundInTheMphAfterMoreThanThreeMfPins()
    {
        // A14.3, A17.2 (ruling R71.14): the 8-1 ends his 4 MF in E6; r1's First Fire at him, 4 + 4 - 2, is a NMC, and his DR of 3 + 3 passes it and equals the
        // Russian SAN of 6. The Russian Sniper in E7 rolls dr 2 and a Random Location DR of 1, 1 (one hex north, E6): the leader, its one target, is
        // wounded (Wound Severity dr 1) and pinned.
        await LeaderMoves(4);
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(1, 4, 4, 3, 3, 2), new
        {
            firers = R1,
            target = L("E6"),
        }));
        await Attack(1, 1, 1);
        var sniped = Assert.Single(Sniped(before));
        Assert.Equal(("de-l", ScenarioA1Sniper.Wounded), (sniped.Unit, sniped.Result));
        var leader = Current.Unit("de-l")!;
        Assert.True(Is(leader, Conditions.Wounded));
        Assert.True(Is(leader, Conditions.Pinned));
        Assert.False(History.HasErrors);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, false)]
    public async Task ASniperStunOfATankCrewedByASmcRollsHisWoundSeverityDr(int severity, bool survives)
    {
        // D5.343 (p. 204), A17.11 (p. 85; ruling R71.15): the German 8-1 crews the Abandoned PzKpfw IIIH in E6 in the German MPh, CE, a SMC Temporary crew.
        // g1 moves to E2; r1's First Fire at it, 4 + 4 - 2, is a NMC, and g1's 3 + 3 equals the Russian SAN of 6. The Russian Sniper's dr 2 and Random
        // Location DR of 1, 1 find E6, where the CE crew is the one target: the tank is Stunned, and the leader's Wound Severity dr is rolled before it: 1
        // wounds him, 5 eliminates him and leaves the tank Abandoned.
        var tank = Vehicle("de-tk", "attacker-tank", "E6", buttonedUp: false);
        tank["conditions"] = new Dictionary<string, bool> { [Conditions.ButtonedUp] = false, [Conditions.Abandoned] = true };
        await Setup(null, tank, Unit("de-l", "asl:leader", "attacker-leader-8-1", "E6", "german"));
        await Advance();
        Committed(await Do(GameActions.RecrewVehicle, NoRoll(), new
        {
            unitId = "de-l",
            vehicleId = "de-tk",
        }));
        Assert.Equal(ScenarioA1CapturedVehicleRules.Smc, Current.Unit("de-tk")!.TemporaryCrew?.Manning);
        if (Current.Movement is { WindowOpen: true })
        {
            await PassFire();
        }

        if (Current.Movement is not null)
        {
            Committed(await Do(GameActions.EndMove, NoRoll(), new
            {
            }));
        }

        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2"),
        }));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 4, 4, 3, 3, 2, 1, 1, severity), new
        {
            firers = R1,
            target = L("E2"),
        }));
        await Attack(1, 1, severity);
        Assert.Equal(("de-tk", ScenarioA1Sniper.Stunned), (Assert.Single(Sniped(before)).Unit, Sniped(before)[0].Result));
        var events = Since(before).ToList();
        var roll = events.FindIndex(item => item.Payload is DiceRolled { Purpose: ScenarioA1Wounds.SeverityPurpose } rolled && rolled.Roll.EndsWith("-wound-de-l", StringComparison.Ordinal));
        var stun = events.FindIndex(item => item.Payload is ConditionsChanged { Id: "de-tk" } changed && changed.Conditions.ContainsKey(Conditions.Stunned));
        Assert.True(roll >= 0 && roll < stun, $"{roll} {stun}");
        Assert.Equal([severity], ((DiceRolled)events[roll].Payload).Values);
        var leader = Current.Unit("de-l")!;
        Assert.Equal(survives, leader.Status == InstanceStatus.Active);
        Assert.Equal(survives, Is(leader, Conditions.Wounded));
        Assert.Equal(!survives, Is(Current.Unit("de-tk")!, Conditions.Abandoned));
        Assert.False(History.HasErrors);
    }

    // Increment 4g: carrying a wounded man (A17.2, A4.42); ruling R71.16.

    private static readonly string[] TheSquadAndTheLeader = ["de-l", "de-s"];
    private static readonly string[] TheCarrier = ["de-s"];

    /// <summary>The Open Ground hexes from five hexes south-east of E6 to E6: the carrying stack's path, 1 MF a hex.</summary>
    private static string[] CarryPath()
    {
        var path = new List<string> { "E6" };
        for (var step = 0; step < 5; step++)
        {
            path.Insert(0, N(path[0], HexsideDirection.SouthEast));
        }

        return [.. path];
    }

    /// <summary>The German 4-6-7 de-s and the wounded 8-1 de-l at the start of <see cref="CarryPath"/>, with the placements given, in the German MPh.</summary>
    private async Task<string[]> CarryGame(params Dictionary<string, object>[] placements)
    {
        var path = CarryPath();
        await Setup(null, [Squad("de-s", path[0], "german"), Unit("de-l", "asl:leader", "attacker-leader-8-1", path[0], "german", Conditions.Wounded), .. placements]);
        await Advance();
        return path;
    }

    private Task<PlayResult> CarryStep(string to, bool name = false) => name
        ? Do(GameActions.Move, NoRoll(), new
        {
            unitIds = TheSquadAndTheLeader,
            to = L(to),
            carried = new[] { new { man = "de-l", carrier = "de-s" } },
        })
        : Do(GameActions.Move, NoRoll(), new
        {
            unitIds = TheSquadAndTheLeader,
            to = L(to),
        });

    [Fact]
    public async Task ASquadCarriesAWoundedLeaderWhoSpendsNoMfAndItsOwnMfFallByFivePp()
    {
        // A17.2 (p. 85; ruling R71.16): the 4-6-7 carries the wounded 8-1 at five PP, named on the first step and kept for the move. Each step is 1 MF of Open
        // Ground: the leader spends none of his 3 MF. The squad's 4 MF and the wounded leader's bonus of 2 (which still applies) less the 2 PP beyond its IPC
        // of 3 (A4.42) leave it 4 MF, not 6: its fifth step is refused.
        var path = await CarryGame();
        var before = Revision;
        Committed(await CarryStep(path[1], name: true));
        await PassFire();
        Committed(await CarryStep(path[2]));
        var steps = Since(before).Select(item => item.Payload).OfType<MovementStepped>().ToArray();
        Assert.All(steps, step => Assert.Equal(new CarriedMan("de-l", "de-s"), Assert.Single(step.Carried!)));
        var (leader, squad) = (Current.Unit("de-l")!, Current.Unit("de-s")!);
        Assert.Equal((0, false), (leader.MfSpent, leader.HalfMfSpent));
        Assert.Equal(2, squad.MfSpent);
        Assert.Equal(L(path[2]), Current.Location("de-l")!.Location.ToString());

        for (var step = 3; step <= 4; step++)
        {
            await PassFire();
            Committed(await CarryStep(path[step]));
        }

        await PassFire();
        var refused = await CarryStep(path[5]);
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.Contains("de-s has 0 MF left", StringComparison.Ordinal));
        Assert.Equal(0, Current.Unit("de-l")!.MfSpent);
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task AWoundedLeaderWhoPortagesASwIsNotCarried()
    {
        // A17.2: "A Good Order wounded man who is not portaging a SW": the 8-1 holds the German LMG, so the carry is refused and says why.
        Dictionary<string, object> lmg = new()
        {
            ["id"] = "dmg",
            ["kind"] = "asl:mg",
            ["definition"] = "attacker-lmg",
            ["side"] = "german",
            ["holding"] = new
            {
                holder = "de-l",
                role = "possessed",
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        };
        var path = await CarryGame(lmg);
        var refused = await CarryStep(path[1], name: true);
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.Contains("play.move-carry: de-l portages a SW", StringComparison.Ordinal));
        Assert.Null(Current.Movement);
    }

    [Fact]
    public async Task ACarriedManWhoseCarrierStopsIsSetDownAndMovesNoFarther()
    {
        // A17.2 (ruling R71.16): the squad carries the 8-1 one hex; the ATTACKER ends the squad's move, and the leader, set down, may not step on alone.
        var path = await CarryGame();
        Committed(await CarryStep(path[1], name: true));
        await PassFire();
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
            unitIds = TheCarrier,
        }));
        var refused = await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = Leader,
            to = L(path[2]),
        });
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Reasons, reason => reason.Contains("set down", StringComparison.Ordinal));
        Assert.False(History.HasErrors);
    }

    // Fix round B: the Sniper's and the wound's rules (rulings R71.25 to R71.28; the reviews' findings R1's 1, 2, 4, 5, 7, R3's 1 to 3, 5, R5's 1).

    /// <summary>The Sniper player's answer to a choice to forfeit: reposition, then the last hex offered when he is asked for one; the dice given drawn last.</summary>
    private async Task Reposition(params int[] dice)
    {
        Committed(await Choose(ScenarioA1Sniper.Reposition, Then(3, dice)));
        if (Current.Choice is { Kind: ChoicePending.Sniper } hexes && hexes.Resume.GetProperty("step").GetString() == ScenarioA1Sniper.RepositionStep)
        {
            Committed(await Choose(hexes.Options[^1], Then(3, dice)));
        }
    }

    private static string ForfeitKey(string sniper, string trigger) => $"{ChoicePending.Sniper}:{sniper}:{trigger}:{ScenarioA1Sniper.ForfeitStep}";

    [Fact]
    public async Task TwoTriggersOfOneSniperInOneRecordReachTheForfeitEachUnderItsOwnKey()
    {
        // R5's finding 1 (A14.1, A14.2): g1's IFT DR of 3 + 3 at r1 and r2 in E4 gives each a MC, 3 + 3, equal to the German SAN of 6 (the Russian SAN is 7):
        // two calls of the German Sniper in I4. Each dr 2 waits on the German side's choice to attack or forfeit, under a key that names its trigger; the
        // second, after the first is answered with no fire record between, is not refused as a choice already answered.
        await Setup(6, 7, [], Squad("r2", "E4", "russian"), Sniper("gs", "I4", "german"));
        Committed(await Do(GameActions.Fire, Then(3, 3, 3, 3, 3, 3, 3, 2), new
        {
            firers = G1,
            target = L("E4"),
        }));
        var fire = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<FireResolved>().Last();
        Assert.Equal(ForfeitKey("gs", fire.Rolls["checks:r1"]), Current.Choice?.Key);

        await Reposition(2);
        Assert.Equal(ForfeitKey("gs", fire.Rolls["checks:r2"]), Current.Choice?.Key);
        Assert.Equal(ScenarioA1Sniper.Forfeited, Assert.Single(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<SniperAttacked>()).Result);
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ASnipersOwnLlmcAttackWaitsOnItsOwnForfeitThenTheFirstAttacksSniperCheckFollows()
    {
        // Ruling R71.25, R5's finding 1 (A14.3, A14.4): as above, the Russian Sniper's dr 1 eliminates the 8-0 and the 4-6-7's LLMC, 3 + 3, calls the Sniper
        // again in the same commit; its dr 2 waits on the Russian side's choice under its own key (an answered Sniper choice and then a non-fire trigger), and
        // keeps the first attack's Sniper Check, owed in E6, to follow. Forfeited and repositioned, the check is offered, keyed by the first attack's trigger.
        await Setup(null, Unit("de-l", "asl:leader", "attacker-leader-8-0", "E6", "german"), Squad("de-s", "E6", "german"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(4, [3, 3, 3, 3, 1]), new
        {
            firers = G1,
            target = L("E4"),
        }));
        var first = Current.Choice!.Resume.GetProperty("trigger").GetString()!;
        Committed(await Choose(ScenarioA1Sniper.Attack, Then(4, 1, 1, 6, 1, 3, 3, 2)));
        var trigger = Since(before).Select(item => item.Payload).OfType<MoraleCheckResolved>().Single().Rolls.Single().Value;
        var waiting = Current.Choice!;
        Assert.Equal(ForfeitKey("rs", trigger), waiting.Key);
        var owed = Assert.Single(waiting.Resume.GetProperty("rest").EnumerateArray());
        Assert.Equal((first, L("E6")), (owed.GetProperty("roll").GetString(), owed.GetProperty("check").GetString()));
        Assert.Equal(("de-l", ScenarioA1Sniper.Eliminated), (Assert.Single(Sniped(before)).Unit, Sniped(before)[0].Result));

        await Reposition();
        var check = Current.Choice!;
        Assert.Equal((ChoicePending.SniperCheck, $"{ChoicePending.SniperCheck}:rs:{first}"), (check.Kind, check.Key));
        Committed(await Choose(ChoicePending.Decline, NoRoll()));
        Assert.Equal(ScenarioA1Sniper.Forfeited, Sniped(before)[^1].Result);
        Assert.Null(Current.Choice);
        Assert.False(History.HasErrors);
    }

    [Theory]
    [InlineData(1, ScenarioA1Sniper.Eliminated)]
    [InlineData(2, ScenarioA1Sniper.Wounded)]
    public async Task ASmcTemporaryCrewOfATruckIsEliminatedOrWoundedAsTheSmcHeIs(int dr, string result)
    {
        // A14.3 (p. 82; ruling R71.26): the German 8-1 crews the Abandoned truck in E6 in the German MPh, a SMC Temporary crew, no Inherent crew. g1 moves to
        // E2; r1's First Fire at it, 4 + 4 - 2, is a NMC, and g1's 3 + 3 equals the Russian SAN of 6. The Russian Sniper's Random Location DR of 1, 1 finds E6,
        // and its side chooses the crew before Random Selection: the dr 1 eliminates him and leaves the truck Abandoned; the dr 2 wounds him (Wound Severity
        // dr 1). Before the fix round he was broken or pinned as an Inherent crew.
        var truck = Vehicle("de-t", "attacker-truck", "E6", buttonedUp: false);
        truck["conditions"] = new Dictionary<string, bool> { [Conditions.Abandoned] = true };
        await Setup(null, truck, Unit("de-l", "asl:leader", "attacker-leader-8-1", "E6", "german"));
        await Advance();
        Committed(await Do(GameActions.RecrewVehicle, NoRoll(), new
        {
            unitId = "de-l",
            vehicleId = "de-t",
        }));
        Assert.Equal(ScenarioA1CapturedVehicleRules.Smc, Current.Unit("de-t")!.TemporaryCrew?.Manning);
        if (Current.Movement is { WindowOpen: true })
        {
            await PassFire();
        }

        if (Current.Movement is not null)
        {
            Committed(await Do(GameActions.EndMove, NoRoll(), new
            {
            }));
        }

        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2"),
        }));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 4, 4, 3, 3, dr), new
        {
            firers = R1,
            target = L("E2"),
        }));
        await Attack(1, 1);
        Assert.Equal(ScenarioA1Sniper.TargetStep, Current.Choice!.Resume.GetProperty("step").GetString());
        Committed(await Choose("crew:de-t", Then(3, 1)));
        Assert.Equal(("de-l", result), (Assert.Single(Sniped(before)).Unit, Sniped(before)[0].Result));
        var leader = Current.Unit("de-l")!;
        Assert.Equal(dr == 2, leader.Status == InstanceStatus.Active && Is(leader, Conditions.Wounded));
        Assert.Equal(dr == 1, leader.Status == InstanceStatus.Eliminated);
        Assert.Equal(dr == 1, Is(Current.Unit("de-t")!, Conditions.Abandoned));
        Assert.False(Is(Current.Unit("de-t")!, Conditions.Broken));
        Assert.False(History.HasErrors);
    }

    private static readonly string[] TheFirstTwoBeneathTheQuestionMark = ["concealed:1", "concealed:2"];
    private static readonly string[] TheSecondRussian = ["r2"];
    private static readonly string[] TheSmoker = ["de-s"];

    [Fact]
    public async Task RandomSelectionInAConcealedStackGivesItsTieToTheSniperPlayerAndTheOtherANewDr()
    {
        // A14.23, A14.2 (p. 82; R1's finding 4): the three German 4-6-7s under "?" in E6 are one candidate; the stack holds three targets, and their Random
        // Selection's 6, 6, 1 ties de-a and de-b. The Russian side chooses, unseen, the second place: de-b takes the dr 2 and is pinned, and de-a takes a new dr,
        // 3, to no effect. The choice is public and names no unit. Before the fix round de-a took the dr.
        await Setup(null, Squad("de-a", "E6", "german", Conditions.Concealed), Squad("de-b", "E6", "german", Conditions.Concealed),
            Squad("de-c", "E6", "german", Conditions.Concealed));
        var before = await FireToTheForfeit(2);
        await Attack(1, 1, 6, 6, 1);
        var choice = Current.Choice!;
        Assert.Equal(ScenarioA1Sniper.StackFirstStep, choice.Resume.GetProperty("step").GetString());
        Assert.Equal(TheFirstTwoBeneathTheQuestionMark, choice.Options);
        Assert.Null(store.Read(Scope)!.Events.Single(item => item.EventId == choice.Event).Visibility);
        Assert.DoesNotContain("de-", choice.Resume.ToString(), StringComparison.Ordinal);

        Committed(await Choose("concealed:2", Then(3, 3)));
        var sniped = Sniped(before);
        Assert.Equal(2, sniped.Length);
        Assert.Equal(("de-b", ScenarioA1Sniper.Pinned, 2), (sniped[0].Unit, sniped[0].Result, sniped[0].Dr));
        Assert.Equal((3, ScenarioA1Sniper.NoEffect), (sniped[1].Dr, sniped[1].Result));
        Assert.True(Is(Current.Unit("de-b")!, Conditions.Pinned));
        Assert.False(Is(Current.Unit("de-a")!, Conditions.Pinned));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task WhereTheTemComparedEquidistantHexesTheLowestTemLocationIsTheTargetUnasked()
    {
        // A14.21 (p. 82; R1's finding 5): the Random Location DR finds E6, empty; the hexes beside it to the north-east and north-west are equidistant. The
        // north-east hex is Woods at ground level, with de-a there and de-b on its level 1 (TEM 0); the north-west one Woods, with de-c. "The Location with the
        // lowest TEM is the target": de-b's, so the dr 1 breaks de-b and no choice of Location is asked. Before the fix round the hex's two Locations were offered.
        var (east, west) = (Beside(HexsideDirection.NorthEast), Beside(HexsideDirection.NorthWest));
        woods.UnionWith([east, west]);
        upstairs.Add(east);
        var above = Squad("de-b", east, "german");
        above["position"] = new
        {
            at = $"bd01:{east}:1"
        };
        await Setup(null, Squad("de-a", east, "german"), above, Squad("de-c", west, "german"));
        var before = await FireToTheForfeit(1);
        await Attack(1, 1);
        Assert.Null(Current.Choice);
        Assert.Equal(("de-b", ScenarioA1Sniper.Broken, $"bd01:{east}:1"), (Assert.Single(Sniped(before)).Unit, Sniped(before)[0].Result, Sniped(before)[0].Target?.ToString()));
        Assert.False(Is(Current.Unit("de-a")!, Conditions.Broken));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task SmokeCountsInItsOwnLocationAloneInTheTemComparison()
    {
        // A14.21 (p. 82; R1's finding 5): "the lowest ... in-hex TEM/SMOKE DRM applicable to any eligible target". In the German MPh de-s enters the
        // north-east hex beside E6 and places SMOKE in its ground level (A24.1, dr 1); de-b is on that hex's level 1, de-c in the north-west hex, all Open
        // Ground. g1 then enters E2, and its check, 3 + 3, after r1's First Fire calls the Russian Sniper, whose Random Location DR finds E6, empty. The SMOKE
        // does not raise de-b's TEM, so both hexes are of TEM 0 and the Russian side chooses. Before the fix round the SMOKE counted for the whole hex, and the
        // north-west hex was the target unasked.
        var (east, west) = (Beside(HexsideDirection.NorthEast), Beside(HexsideDirection.NorthWest));
        upstairs.Add(east);
        var above = Squad("de-b", east, "german");
        above["position"] = new
        {
            at = $"bd01:{east}:1"
        };
        await Setup(null, Squad("de-s", N(east, HexsideDirection.NorthEast), "german"), above, Squad("de-c", west, "german"));
        await Advance();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = TheSmoker,
            to = L(east),
        }));
        await PassFire();
        Committed(await Do(GameActions.Move, Then(3, 1), new
        {
            unitIds = TheSmoker,
            to = L(east),
            smoke = L(east),
            smokeBy = "de-s",
        }));
        Assert.Single(Current.Entities, item => item.Kind == "asl:smoke" && item.Status == InstanceStatus.Active);
        if (Current.Movement is { WindowOpen: true })
        {
            await PassFire();
        }

        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2"),
        }));
        Committed(await Do(GameActions.Fire, Then(3, 4, 4, 3, 3, 1), new
        {
            firers = R1,
            target = L("E2"),
        }));
        await Attack(1, 1);
        var choice = Current.Choice!;
        Assert.Equal(ScenarioA1Sniper.HexStep, choice.Resume.GetProperty("step").GetString());
        Assert.Equal(new[] { L(east), L(west) }.Order(StringComparer.Ordinal), choice.Options);
    }

    [Fact]
    public async Task ABrokenLeaderWoundedAfterMoreThanThreeMfIsNotPinned()
    {
        // A7.8 (p. 58; ruling R71.27, R3's finding 1): the 8-1 ends his 4 MF; r1's First Fire, 5 + 3 - 2, gives him a check, which his 5 + 4 fails: he breaks. r2's
        // First Fire, 2 + 2 - 2, is a K/2: his Wound Severity dr of 2 wounds him, and his 2MC, 2 + 2 + 2, passes. "There is no need to place Pin counters on
        // broken units": the record carries no wound pin, and he is broken, wounded, and not pinned, so his rout is not barred.
        var at = await LeaderMoves(4, Squad("r2", N("E4", HexsideDirection.SouthWest), "russian"));
        Committed(await Do(GameActions.Fire, Then(4, 5, 3, 5, 4), new
        {
            firers = R1,
            target = L(at),
        }));
        Assert.True(Is(Current.Unit("de-l")!, Conditions.Broken), store.Read(Scope)!.Events.Select(item => item.Payload).OfType<FireResolved>().Last().Resolution.ToString());
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(2), new
        {
            firers = TheSecondRussian,
            target = L(at),
        }));
        var fire = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.False(fire.Facts.GetProperty("targets")[0].TryGetProperty("woundPins", out _));
        var leader = Current.Unit("de-l")!;
        Assert.True(Is(leader, Conditions.Wounded), fire.Resolution.ToString());
        Assert.True(Is(leader, Conditions.Broken));
        Assert.False(Is(leader, Conditions.Pinned));
        Assert.False(History.HasErrors);
    }

    [Fact]
    public async Task ARecordedWoundPinTheStateDoesNotGiveIsRefused()
    {
        // R3's finding 2 (A17.2; ruling R71.14): after 3 MF the 8-1's wound by r1's K/2 does not pin him (as in the 4f test); a record that says it does is
        // refused at replay.
        var at = await LeaderMoves(3);
        Committed(await Do(GameActions.Fire, Then(2), new
        {
            firers = R1,
            target = L(at),
        }));
        var events = store.Read(Scope)!.Events;
        Assert.False(Planner().Replay(events).HasErrors);
        GameEvent[] forged = [.. events.Select(item =>
        {
            if (item.Payload is not FireResolved fired)
            {
                return item;
            }

            var facts = System.Text.Json.Nodes.JsonNode.Parse(fired.Facts.GetRawText())!.AsObject();
            facts["targets"]![0]!["woundPins"] = true;
            return item with { Payload = fired with { Facts = JsonSerializer.SerializeToElement(facts) } };
        })];
        Assert.True(Planner().Replay(forged).HasErrors);
    }

    [Fact]
    public async Task AStepRecordedForACarriedManWithoutHisCarrierIsRefused()
    {
        // R3's finding 3 (A17.2; ruling R71.16): the 4-6-7 carries the wounded 8-1 two steps. A record of the second step that moves the man without his carrier
        // is refused at replay as the planner refuses it: he was set down and moves no farther.
        var path = await CarryGame();
        Committed(await CarryStep(path[1], name: true));
        await PassFire();
        Committed(await CarryStep(path[2]));
        var events = store.Read(Scope)!.Events;
        Assert.False(Planner().Replay(events).HasErrors);
        var second = events.Last(item => item.Payload is MovementStepped).EventId;
        GameEvent[] forged = [.. events.Select(item => item.EventId == second && item.Payload is MovementStepped step
            ? item with { Payload = step with { Movers = Leader, Carried = null } } : item)];
        var replayed = Planner().Replay(forged);
        Assert.True(replayed.HasErrors);
        Assert.Contains(replayed.Diagnostics, diagnostic => diagnostic.Message.Contains("moves no farther this MPh", StringComparison.Ordinal));
    }
}
