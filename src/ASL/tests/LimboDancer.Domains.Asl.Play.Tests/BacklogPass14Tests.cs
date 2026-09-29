using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 14 in live play (rulings R14.1 to R14.14): advances into concealed units, Dummies and hidden units at the CCPh, Hand-to-Hand,
/// capture attempts and prisoners handed over or abandoned, Infiltration, overstacked advances, Ambush Withdrawal, and mandatory CC that lapses.
/// Board 01's hex facts, fixed dice, and a stub LOS reader that sees clear from everywhere.
/// </summary>
public sealed class BacklogPass14Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f14a");
    private static readonly GameScope Scope = new(Tenant, "pass14");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] G3 = ["g3"];
    private static readonly string[] R4 = ["r4"];
    private static readonly string[] R4R5 = ["r4", "r5"];
    private static readonly string[] R5 = ["r5"];
    private static readonly string[] G1G2 = ["g1", "g2"];
    private static readonly string[] G4 = ["g4"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R9 = ["r9"];
    private static readonly string[] HandToHandRules = ["hand-to-hand"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass14-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private readonly StubLos los = new();

    public BacklogPass14Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubLos : IFireLosReader
    {
        public bool Blocked
        {
            get; set;
        }

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            Blocked ? new(LosStatus.Blocked, true, 1, 0, null, string.Empty) : new(LosStatus.Clear, false, 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog], fireLos: los);

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

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side, params string[] states)
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
                at
            },
            ["conditions"] = conditions
        };
    }

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private Task Setup(string firstSide, params Dictionary<string, object>[] placements) => Setup(firstSide, [], placements);

    private async Task Setup(string firstSide, string[] specialRules, params Dictionary<string, object>[] placements)
    {
        var result = await Commit(Play(), GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Close Combat",
                catalog = "asl-scenario-a1@1.12.0",
                boards = Bd01,
                firstSide,
                scenarioMonth = 7,
                specialRules,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 2 } },
            },
            placements,
        }));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
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

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static object Attack(string[] attackers, string[] defenders) => new
    {
        attackers,
        defenders
    };

    private static Dictionary<string, object> Dummy(string id, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:dummy",
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    private static CloseCombatResolution LastRound(IEnumerable<GameEvent> events) =>
        events.Select(item => item.Payload).OfType<CloseCombatResolved>().Last().Resolution.Deserialize<CloseCombatResolution>(LiveFire.Json)!;

    [Fact]
    public async Task AnAdvanceIntoConcealedUnitsIsAllowedAndTheCcphRemovesDummiesAndPlacesHiddenUnits()
    {
        // A12.14, A11.19 (ruling R14.2): the advance keeps its way into "?" units; as the CCPh begins the Dummy goes and the hidden unit is placed.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian", "asl:concealed"),
            Unit("r2", "asl:half-squad", "defender-half-squad", "bd01:B1:0", "russian", "asl:hidden"), Dummy("d1", "bd01:B1:0", "russian"));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B1:0"
        }));
        await Advance();
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("d1")!.Status);
        Assert.True(Is(Current.Unit("r2")!, Conditions.Concealed));
        Assert.Contains("r2", Current.HiddenPlaced);

        // A11.4: an Ambush can occur in Open Ground against concealed units; with none, the German attack on the concealed squad is halved.
        Assert.True(Planner().AmbushDue(Current, BoardLocation.Parse("bd01:B1:0")));
        Committed(await Do(GameActions.Ambush, Once(3, 3), new
        {
            location = "bd01:B1:0"
        }));
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G1, ["r1"]) },
        }));
        Assert.Contains(LastRound(store.Read(Scope)!.Events).Attacks[0].FirepowerModifiers, item => item.Name == "vs-concealed");

        // A11.15 EXC: the Russians kept their "?", so no one is held in Melee.
        await Advance();
        Assert.False(Is(Current.Unit("g1")!, Conditions.Melee));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Concealed));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task HandToHandNeedsItsSsrAndUsesTheRedKillNumber()
    {
        // J2.31 (ruling R14.1): with the SSR, 4 against 4 at 1-1 kills on a DR of 6 (red 7); without it the declaration is refused.
        await Setup("german", HandToHandRules, Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian"));
        await Advance(7);
        Assert.Equal("ccph", Current.Phase);
        Committed(await Do(GameActions.CloseCombat, Once(3, 3), new
        {
            location = "bd01:B1:0",
            handToHand = true,
            attacks = new[] { Attack(G1, ["r1"]) },
        }));
        Assert.Equal(7, LastRound(store.Read(Scope)!.Events).Attacks[0].KillNumber);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("r1")!.Status);
        Assert.True(Current.CloseCombats.Single().HandToHand);
    }

    [Fact]
    public async Task HandToHandIsRefusedWithoutItsSsr()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian"));
        await Advance(7);
        Assert.Contains((await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:B1:0",
            handToHand = true,
            attacks = new[] { Attack(G1, ["r1"]) },
        })).Reasons, reason => reason.StartsWith("play.cc-hand-to-hand", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ACaptureAttemptTakesAPrisonerWhomItsGuardCanHandOverOrAbandon()
    {
        // A20.22 (ruling R14.4): 4 against a HS's 2 is 2-1, Kill 7; +1 for the attempt: an Original 5 captures the HS, placed with g1 and Unarmed.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B1:0", "german"),
            Unit("r1", "asl:half-squad", "defender-half-squad", "bd01:B1:0", "russian"));
        await Advance(7);
        Committed(await Do(GameActions.CloseCombat, Once(2, 3), new
        {
            location = "bd01:B1:0",
            attacks = new object[] { new { attackers = G1, defenders = R1, capture = true } },
        }));
        var prisoner = Current.Unit("r1")!;
        Assert.Equal("g1", prisoner.Custodian);
        Assert.True(Is(prisoner, Conditions.Captured) && Is(prisoner, Conditions.Unarmed));

        // A20.5 (ruling R14.7): in the German RPh g1 hands the prisoner to g2, then g2 abandons it.
        await Advance(9);
        Assert.Equal(("rph", "german"), (Current.Phase, Current.PhasingSide));
        Committed(await Do(GameActions.GuardPrisoners, NoRoll(), new
        {
            guardId = "g1",
            to = "g2"
        }));
        Assert.Equal("g2", Current.Unit("r1")!.Custodian);
        Committed(await Do(GameActions.GuardPrisoners, NoRoll(), new
        {
            guardId = "g2",
            abandon = true
        }));
        Assert.Null(Current.Unit("r1")!.Custodian);
        Assert.True(Is(Current.Unit("r1")!, Conditions.Unarmed));
        Assert.False(Is(Current.Unit("r1")!, Conditions.Captured));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnInfiltratingSquadLeavesBeforeTheDefenderStrikes()
    {
        // A11.22 (ruling R14.8): the German Original 2 creates a 7-0 (dr 6, -1 German), eliminates the Russian at 1-1, and g1 withdraws to B2 with him,
        // so the Russian attack on it is forfeited.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian"));
        await Advance(7);
        Committed(await Do(GameActions.CloseCombat, Once(1, 1, 6), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G1, ["r1"]), Attack(["r1"], G1) },
            infiltrations = new Dictionary<string, string> { ["g1"] = "bd01:B2:0" },
        }));
        Assert.Equal(BoardLocation.Parse("bd01:B2:0"), Current.Location("g1")!.Location);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("r1")!.Status);
        var leader = Current.Units.Single(unit => unit.Kind == "asl:leader");
        Assert.Equal(BoardLocation.Parse("bd01:B2:0"), Current.Location(leader.Id)!.Location);
    }

    [Fact]
    public async Task AnAdvanceMayOverstackTheLocation()
    {
        // A5.11 (ruling R14.10): a fourth squad may advance into a Location with three, paying one more MF for the excess.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B1:0", "german"),
            Unit("g3", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("g4", "asl:squad", "attacker-squad", "bd01:B2:0", "german"));
        await Advance(6);
        var result = await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G4,
            to = "bd01:B1:0"
        });
        Committed(result);
        Assert.Equal(BoardLocation.Parse("bd01:B1:0"), Current.Location("g4")!.Location);
    }

    [Fact]
    public async Task TheAmbushersMayWithdrawBeforeTheFirstRound()
    {
        // A11.41 (ruling R14.9): after the Germans ambush in the woods, g2 declines CC by withdrawing to B2.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B2:0", "german"),
            Unit("r1", "asl:squad", "defender-squad", "bd01:C2:0", "russian"));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1G2,
            to = "bd01:C2:0"
        }));
        await Advance();
        Committed(await Do(GameActions.Ambush, Once(1, 6), new
        {
            location = "bd01:C2:0"
        }));
        Assert.Equal("german", Current.CloseCombats.Single().Ambusher);
        Committed(await Do(GameActions.AmbushWithdraw, NoRoll(), new
        {
            location = "bd01:C2:0",
            unitIds = G2,
            to = "bd01:B2:0"
        }));
        Assert.Equal(BoardLocation.Parse("bd01:B2:0"), Current.Location("g2")!.Location);
    }

    [Fact]
    public async Task AnEscapeThatEliminatesTheGuardIsRecordedAndRearmsThePrisoner()
    {
        // Table player, pass 14, finding 1 (A20.55, A20.551): the HS captured in the German CCPh; Russian fire breaks and reduces the Guard; in the Russian
        // CCPh the prisoner passes its NTC (2) and eliminates the broken Guard (1-4, Kill 3, -2: an Original 4), and is rearmed as a Conscript HS.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:C2:0", "german"), Unit("r1", "asl:half-squad", "defender-half-squad", "bd01:C2:0", "russian"),
            Unit("r9", "asl:squad", "defender-squad", "bd01:E2:0", "russian"));
        await Advance(7);
        Committed(await Do(GameActions.CloseCombat, Once(1, 2), new
        {
            location = "bd01:C2:0",
            attacks = new object[] { new { attackers = G1, defenders = R1, capture = true } },
        }));
        await Advance(2);
        await Do(GameActions.Fire, Once(1, 2, 4, 5, 1, 1, 1, 1, 1, 1), new Dictionary<string, object> { ["firers"] = R9, ["target"] = "bd01:C2:0" });
        var guard = Current.Unit("r1")!.Custodian!;
        while (Current.Phase != "ccph")
        {
            await Advance();
        }

        Committed(await Do(GameActions.CloseCombat, Once(1, 1, 1, 3, 1, 1, 1, 1), new
        {
            location = "bd01:C2:0",
            round = "prisoners",
            attacks = new[] { Attack(R1, [guard]) },
        }));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit(guard)!.Status);
        var rearmed = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Definition?.Definition == "defender-conscript-half-squad");
        Assert.False(Is(rearmed, Conditions.Captured) || Is(rearmed, Conditions.Unarmed));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task WhenEveryAmbusherWithdrawsTheAmbushedSideCloses()
    {
        // Referee, pass 14, item 1 (A11.41): with both ambushing squads gone, the ambushed side's round (no attacks) closes the Location.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B2:0", "german"),
            Unit("r1", "asl:squad", "defender-squad", "bd01:C2:0", "russian"));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1G2,
            to = "bd01:C2:0"
        }));
        await Advance();
        Committed(await Do(GameActions.Ambush, Once(1, 6), new
        {
            location = "bd01:C2:0"
        }));
        Committed(await Do(GameActions.AmbushWithdraw, NoRoll(), new
        {
            location = "bd01:C2:0",
            unitIds = G1G2,
            to = "bd01:B2:0"
        }));
        Committed(await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:C2:0",
            attacks = Array.Empty<object>(),
        }));
        Assert.True(Current.CloseCombats.Single().Closed);
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    [Fact]
    public async Task MandatoryCcLapsesWhenThePackageRefusesEveryAttack()
    {
        // A15.43 (ruling R14.14): a berserk crew's attack is refused (a crew in CC is not reviewed), so the CCPh may end. (Backlog pass 15 decides the NKVD
        // Field Promotion this test used before.)
        await Setup("russian", Unit("rn", "asl:crew", "defender-crew", "bd01:B1:0", "russian", "asl:berserk"), Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"));
        await Advance(7);
        Assert.Equal("ccph", Current.Phase);
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
