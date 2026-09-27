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
/// Unit steps 29 and 30 in live play (U33, U34): Advance into a Known enemy, Close Combat with and without Ambush, Melee and the
/// end of a broken unit held in it, a Heat of Battle Berserk result and the charge it forces, and a Surrender and the capture
/// that follows. Board 01's hex facts, fixed dice, and a stub LOS reader that sees clear from everywhere unless told otherwise.
/// </summary>
public sealed class CloseCombatStepsTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f129");
    private static readonly GameScope Scope = new(Tenant, "village");
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

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-cc-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private readonly StubLos los = new();

    public CloseCombatStepsTests() => store = new FileGameStore(root);

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

    private async Task Setup(string firstSide, params Dictionary<string, object>[] placements)
    {
        var result = await Commit(Play(), GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Close Combat",
                catalog = "asl-scenario-a1@1.4.0",
                boards = Bd01,
                firstSide,
                scenarioMonth = 7,
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

    [Fact]
    public async Task U33ASquadAdvancesIntoAKnownEnemyTheyCloseCombatAndTheSurvivorsAreInMelee()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian"));
        await Advance(6);
        Assert.Equal("aph", Current.Phase);

        // A3.7, A4.7: g1 advances into B1, Open Ground, where the Known Russian squad is.
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B1:0"
        }));
        Assert.Equal(BoardLocation.Parse("bd01:B1:0"), Current.Location("g1")!.Location);
        await Advance();
        Assert.Equal("ccph", Current.Phase);

        // A11.11: 4 FP against 4 at 1-1, Kill Number 5; both sides' DR are 12: no effect. The record shows the odds and DR.
        var before = Revision;
        Committed(await Do(GameActions.CloseCombat, Once(6, 6, 6, 6), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G1, ["r1"]), Attack(["r1"], G1) },
        }));
        var record = Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>().Single();
        var resolution = record.Resolution.Deserialize<CloseCombatResolution>(LiveFire.Json)!;
        Assert.Equal((CloseCombatResolved.Simultaneous, "1-1", 5, 12), (record.Round, resolution.Attacks[0].Odds, resolution.Attacks[0].KillNumber,
            resolution.Attacks[0].Defending[0].FinalDr));

        // A11.12: the Location's CC is resolved for this CCPh.
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.CloseCombat, Once(1, 1), new
        {
            location = "bd01:B1:0",
            attacks = Array.Empty<object>(),
        })).Outcome);

        // A11.15: at the end of the CCPh both are held in Melee, and a unit in Melee does not move or fire.
        await Advance();
        Assert.True(Is(Current.Unit("g1")!, Conditions.Melee) && Is(Current.Unit("r1")!, Conditions.Melee));
        Assert.Equal(ConditionState.False, GameState.GoodOrder(Current.Unit("r1")!, Vocabulary));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnAdvanceIntoWoodsCallsForAmbushAndTheAmbusherResolvesFirst()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:C2:0", "russian"));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:C2:0"
        }));
        await Advance();

        // A11.4: Infantry advanced into CC in woods, so the Ambush drs come first.
        var early = await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:C2:0",
            attacks = new[] { Attack(G1, ["r1"]) },
        });
        Assert.Contains(early.Reasons, reason => reason.StartsWith("play.cc-ambush-first", StringComparison.Ordinal));
        var woods = BoardLocation.Parse("bd01:C2:0");
        Assert.Empty(Planner().DeclaringSides(Current, woods, ambusherAgain: false));

        // The German dr 1 is at least three below the Russian 5: the Germans ambush.
        Committed(await Do(GameActions.Ambush, Once(1, 5), new
        {
            location = "bd01:C2:0"
        }));
        Assert.Equal("german", Current.CloseCombats.Single().Ambusher);
        Assert.Equal(["german"], Planner().DeclaringSides(Current, woods, ambusherAgain: false));

        // The Russians may not attack first (A11.32); the German round has -1: 3+3-1 = 5 at 1-1 is a Partial Kill, reducing r1.
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:C2:0",
            attacks = new[] { Attack(["r1"], G1) },
        })).Outcome);
        Committed(await Do(GameActions.CloseCombat, Once(3, 3), new
        {
            location = "bd01:C2:0",
            attacks = new[] { Attack(G1, ["r1"]) },
        }));
        var half = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Side == "russian");

        // The page offers only the sides the planner reads: the ambushed side next, or the ambusher again when that is chosen.
        Assert.Equal(["russian"], Planner().DeclaringSides(Current, woods, ambusherAgain: false));
        Assert.Equal(["german"], Planner().DeclaringSides(Current, woods, ambusherAgain: true));
        Assert.Equal("defender-half-squad", half.Definition!.Definition);

        // The phase waits for the ambushed side's round; its HS attacks back with +1 against the ambusher.
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        })).Outcome);
        var before = Revision;
        Committed(await Do(GameActions.CloseCombat, Once(6, 5), new
        {
            location = "bd01:C2:0",
            attacks = new[] { Attack([half.Id], G1) },
        }));
        var second = Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>().Single().Resolution.Deserialize<CloseCombatResolution>(LiveFire.Json)!;
        Assert.Contains(second.Attacks[0].Drm, item => item.Name == "vs-ambush" && item.Value == 1m);
        await Advance();
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ABrokenUnitHeldInMeleeMustWithdrawAndADisruptedOneIsEliminated()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"),
            Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian", "asl:broken"),
            Unit("r2", "asl:squad", "defender-squad", "bd01:B1:0", "russian", "asl:broken", "asl:disrupted"));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B1:0"
        }));
        await Advance();

        // A11.16: broken units do not attack; the German attack at 1-2 (4-8) with -2 against each: 6+6-2 = 10, no effect.
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G1, ["r1", "r2"]) },
        }));
        await Advance();
        Assert.True(Is(Current.Unit("r1")!, Conditions.Melee));

        // Through the Russian Player Turn to its CCPh: the broken r1 must attempt to withdraw (A11.16) before the phase ends.
        await Advance(7);
        Assert.Equal(("ccph", "russian"), (Current.Phase, Current.PhasingSide));
        Assert.Contains((await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        })).Reasons, reason => reason.StartsWith("play.cc-withdraw-required", StringComparison.Ordinal));

        // A11.2: r1 withdraws to A1; the German attack on it takes -2 broken, -2 withdrawing, and +1 for r2, which stays: 6+6-3 = 9 at
        // 1-1 (4-4) is no effect, so r1 withdraws. The Disrupted r2 may not withdraw (A11.2) and is eliminated at the end (A19.12).
        var before = Revision;
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G1, ["r1"]) },
            withdrawals = new Dictionary<string, string> { ["r1"] = "bd01:A1:0" },
        }));
        var record = Since(before).Select(item => item.Payload).OfType<CloseCombatResolved>().Single().Resolution.Deserialize<CloseCombatResolution>(LiveFire.Json)!;
        Assert.Equal([("vs-broken", -2m), ("vs-withdrawing", -2m), ("covering", 1m)], record.Attacks[0].Defending[0].Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal(BoardLocation.Parse("bd01:A1:0"), Current.Location("r1")!.Location);
        Assert.False(Is(Current.Unit("r1")!, Conditions.Melee));
        before = Revision;
        await Advance();
        Assert.Contains(Since(before), item => item.Payload is InstanceEliminated { Id: "r2" });
        Assert.False(Is(Current.Unit("g1")!, Conditions.Melee));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U34ABerserkResultMakesTheSquadChargeTheNearestKnownEnemyInItsNextMph()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A3:0", "german"), Unit("g3", "asl:squad", "attacker-squad", "bd01:B3:0", "german"));
        await Advance();

        // 16 FP, 4+6 = 10: a NMC; g2 rolls an Original 2, and its Heat of Battle DR 5+5 = 10 makes it berserk (A15.4).
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 5, 5), new
        {
            firers = R4R5,
            target = "bd01:A3:0"
        }));
        Assert.True(Is(Current.Unit("g2")!, Conditions.Berserk));
        Assert.Equal("is berserk, and berserk fire is not reviewed (A15.432)", GamePlanner.FireBar(Current, Current.Unit("g2")!));
        await Advance(9);
        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));

        // The page marks the charge from the planner's reading: g2 charges A1, and its only next step is A2 (A15.43, A15.431).
        var charge = Assert.Single(Planner().Charges(Current));
        Assert.Equal(("g2", BoardLocation.Parse("bd01:A1:0")), (charge.Unit.Id, charge.Target));
        Assert.Equal([BoardLocation.Parse("bd01:A2:0")], charge.Next);
        Assert.Null(charge.Undecided);

        // A15.43: g2 charges before any other unit moves, by the shortest route to A1: A2, not B2.
        Assert.Contains((await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G3,
            to = "bd01:B2:0"
        })).Reasons, reason => reason.StartsWith("play.berserk-first", StringComparison.Ordinal));
        Assert.Contains((await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:B2:0"
        })).Reasons, reason => reason.StartsWith("play.berserk-charge", StringComparison.Ordinal));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:A2:0"
        }));
        Assert.Equal(BoardLocation.Parse("bd01:A1:0"), Current.Movement!.Charge);
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));

        // It may not stop short while it has the MF, and it enters the Russians' Location (A15.432).
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.EndMove, NoRoll(), new
        {
        })).Outcome);
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:A1:0"
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.Equal(BoardLocation.Parse("bd01:A1:0"), Current.Location("g2")!.Location);

        // Now g3 may move.
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G3,
            to = "bd01:B2:0"
        }));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U34ASurrenderNextToAGoodOrderEnemyMakesTheUnitItsPrisoner()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        await Advance();

        // A NMC; g2's Original 2 passes, and its Heat of Battle DR 6+6 = 12 is a Surrender: broken and Disrupted, next to the
        // Good Order Russian squads (A15.5).
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 6, 6), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        var pending = Since(before).Select(item => item.Payload).OfType<SurrenderPending>().Single();
        Assert.Equal(["r4", "r5"], pending.Captors);
        Assert.True(Is(Current.Unit("g2")!, Conditions.Broken) && Is(Current.Unit("g2")!, Conditions.Disrupted));

        // Nothing else happens until the Russians choose the Guard.
        Assert.Contains((await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        })).Reasons, reason => reason.StartsWith("play.surrender-pending", StringComparison.Ordinal));
        Committed(await Do(GameActions.TakePrisoner, NoRoll(), new
        {
            unitId = "g2",
            captorId = "r4"
        }));
        var prisoner = Current.Unit("g2")!;
        Assert.Equal(("r4", true, BoardLocation.Parse("bd01:A1:0")), (prisoner.Custodian, Is(prisoner, Conditions.Captured), Current.Location("g2")!.Location));
        Assert.Equal("is a prisoner and does not fire (A20.5)", GamePlanner.FireBar(Current, prisoner));
        Assert.Equal("guards prisoners, and a Guard's fire is not reviewed (A20.52)", GamePlanner.FireBar(Current, Current.Unit("r4")!));
        Assert.Null(GamePlanner.FireBar(Current, Current.Unit("r5")!));

        // A20.53: the prisoner advances with its Guard.
        await Advance(5);
        Assert.Equal("aph", Current.Phase);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = R4,
            to = "bd01:B1:0"
        }));
        Assert.Equal(BoardLocation.Parse("bd01:B1:0"), Current.Location("g2")!.Location);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ABerserkResultWithNoKnownEnemyInLosIsBattleHardening()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A3:0", "german"));
        await Advance();

        // The stub reads every LOS blocked: the fire still resolves (the package takes the LOS as the planner read it), but the
        // Berserk result finds no Known enemy in g2's LOS, so g2 is Battle Hardened instead (A15.44).
        los.Blocked = true;
        var refused = await Do(GameActions.Fire, NoRoll(), new
        {
            firers = R4R5,
            target = "bd01:A3:0"
        });
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        los.Blocked = false;
        var planner = Planner();
        Assert.True(planner.KnownEnemyInLos(Current, "german", BoardLocation.Parse("bd01:A3:0")));
        los.Blocked = true;
        Assert.False(Planner().KnownEnemyInLos(Current, "german", BoardLocation.Parse("bd01:A3:0")));
        var heat = ScenarioA1HeatOfBattle.Resolve(new ScenarioA1FirePackage().Reference.Definitions["attacker-squad"], false, null, false, [5, 5],
            new ScenarioA1FirePackage().Reference.Definitions, false, []);
        Assert.Equal((HeatOfBattleOutcome.BattleHardening, true), (heat.Outcome!.Result, heat.Outcome.NoKnownEnemyInLos));
    }

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side) => new()
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

    [Fact]
    public async Task ABerserkUnitAbandonsItsMmgBeforeChargingAndDoesNotChargeIntoPrisoners()
    {
        // A15.431: a berserk squad abandons its 4PP MMG before charging; A20.4: the charge may not end among prisoners (not reviewed).
        await Setup("german", Unit("g2", "asl:squad", "attacker-squad", "bd01:A3:0", "german", "asl:berserk"), Weapon("gmmg", "attacker-mmg", "g2", "german"),
            Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("g9", "asl:half-squad", "attacker-half-squad", "bd01:A1:0", "german", "asl:captured"));
        await Advance(2);
        var before = Revision;
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:A2:0"
        }));
        Assert.Contains(Since(before), item => item.Payload is EquipmentTransferred { Id: "gmmg", Holding: null });
        Assert.Equal(BoardLocation.Parse("bd01:A3:0"), Current.Location("gmmg")!.Location);
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Assert.Contains((await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:A1:0"
        })).Reasons, reason => reason.StartsWith("play.berserk-concealed", StringComparison.Ordinal));

        // The page marks the charge as undecided, with the planner's reason (backlog, section 13).
        var undecided = Assert.Single(Planner().Charges(Current));
        Assert.Equal("g2", undecided.Unit.Id);
        Assert.Empty(undecided.Next);
        Assert.StartsWith("play.berserk-concealed", undecided.Undecided, StringComparison.Ordinal);

        // Table-player review, item 2: the step the model cannot take leaves the charge undecided, so it ends in place (ruling R30.5)
        // and the MPh can end; before, the move could neither go on nor end.
        var ended = await Do(GameActions.EndMove, NoRoll(), new
        {
        });
        Committed(ended);
        Assert.Contains(ended.Reasons, reason => reason.Contains("ends in place", StringComparison.Ordinal));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
        Assert.Equal("dfph", Current.Phase);
    }

    [Fact]
    public async Task AGuardMayNotAdvanceIntoCcWithItsPrisoners()
    {
        // Table-player review, item 3: A20.53, A20.55: the prisoners would enter CC with their Guard, which is not reviewed; a Guard
        // may still advance where no enemy unit is.
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"), Unit("g3", "asl:squad", "attacker-squad", "bd01:B1:0", "german"));
        await Advance();
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 6, 6), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Committed(await Do(GameActions.TakePrisoner, NoRoll(), new
        {
            unitId = "g2",
            captorId = "r4"
        }));
        await Advance(5);
        Assert.Equal("aph", Current.Phase);
        Assert.Contains((await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = R4,
            to = "bd01:B1:0"
        })).Reasons, reason => reason.StartsWith("play.advance-guard", StringComparison.Ordinal));
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = R5,
            to = "bd01:B1:0"
        }));
    }

    [Fact]
    public async Task AWithdrawalMayEnterALocationHeldByAFriendlyGuard()
    {
        // Table-player review, items 5 and 11: A11.21 allows any ADJACENT Location the unit could advance into with no enemy unit; a
        // Location holding a friendly Guard and its prisoner qualifies.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"),
            Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian", "asl:broken", "asl:melee"),
            Unit("rl", "asl:leader", "defender-leader", "bd01:A1:0", "russian"), Unit("g9", "asl:half-squad", "attacker-half-squad", "bd01:A1:0", "german", "asl:captured"));
        var state = Current;
        var destinations = Planner().WithdrawalDestinations(state, state.Unit("r1")!, BoardLocation.Parse("bd01:B1:0"));
        Assert.Contains(BoardLocation.Parse("bd01:A1:0"), destinations);
        Assert.DoesNotContain(BoardLocation.Parse("bd01:B1:0"), destinations);
    }

    [Fact]
    public async Task TheAmbushersAttacksAreSequentialAndTheAmbushedSideCloses()
    {
        // Table-player review, item 6: A11.3: the ambusher's attacks need not be predesignated; each is resolved before the next.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B2:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B2:0", "german"),
            Unit("r1", "asl:squad", "defender-squad", "bd01:C2:0", "russian"), Unit("r2", "asl:squad", "defender-squad", "bd01:C2:0", "russian"));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1G2,
            to = "bd01:C2:0"
        }));
        await Advance();
        Committed(await Do(GameActions.Ambush, Once(1, 5), new
        {
            location = "bd01:C2:0"
        }));

        // g1 attacks r1 (6+6-1 = 11 at 1-1: no effect), then g2 attacks r2 in a second record of the same round.
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:C2:0",
            attacks = new[] { Attack(G1, ["r1"]) },
        }));
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:C2:0",
            round = CloseCombatResolved.AmbusherRound,
            attacks = new[] { Attack(G2, ["r2"]) },
        }));

        // g1 may not attack twice (A11.12).
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:C2:0",
            round = CloseCombatResolved.AmbusherRound,
            attacks = new[] { Attack(G1, ["r2"]) },
        })).Outcome);
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:C2:0",
            attacks = new[] { Attack(["r1", "r2"], G1) },
        }));
        Assert.Equal([CloseCombatResolved.AmbusherRound, CloseCombatResolved.AmbusherRound, CloseCombatResolved.AmbushedRound], Current.CloseCombats.Single().Rounds);
        Assert.True(Current.CloseCombats.Single().Closed);
        await Advance();
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ABerserkUnitWithAKnownEnemyMustHaveItsCcBeforeTheCcphEnds()
    {
        // Table-player review, item 7: A15.43: the berserk unit attacks in CC; the CCPh may not end with no round in its Location.
        await Setup("german", Unit("g2", "asl:squad", "attacker-squad", "bd01:B1:0", "german", "asl:berserk"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian"));
        await Advance(7);
        Assert.Equal("ccph", Current.Phase);
        Assert.Contains((await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        })).Reasons, reason => reason.StartsWith("play.cc-required", StringComparison.Ordinal));
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:B1:0",
            attacks = Array.Empty<object>(),
        })).Outcome);
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G2, ["r1"]) },
        }));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    [Fact]
    public async Task ATiUnitRefusesCcAndPrisonersBarARallyThatCouldGoBerserk()
    {
        // A4.8: CC with a TI unit is not reviewed (ruling R29.14).
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:B1:0", "russian", "asl:ti"),
            Unit("r2", "asl:squad", "defender-squad", "bd01:D4:0", "russian", "asl:broken"), Unit("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian"),
            Unit("g9", "asl:half-squad", "attacker-half-squad", "bd01:D4:0", "german", "asl:captured"));

        // A20.4: a leader's rally can reach a Berserk result, and a berserk unit massacres the prisoners in its Location (not reviewed).
        Assert.Contains((await Do(GameActions.Rally, NoRoll(), new
        {
            unitId = "r2",
            leader = "rl"
        })).Reasons, reason => reason.StartsWith("play.rally-massacre", StringComparison.Ordinal));
        await Advance(7);
        Assert.Equal("ccph", Current.Phase);
        Assert.Contains((await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:B1:0",
            attacks = new[] { Attack(G1, ["r1"]) },
        })).Reasons, reason => reason.StartsWith("play.cc-ti", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ASurrenderToAGuardWithNoCapacityLeftIsUndecided()
    {
        // A20.51: a leader (US# 1) guards at most five US#; with a squad already his prisoner, another squad (3) would exceed it, and the
        // excess would be freed as Unarmed (A20.21), which is not built: the captors are unread and a Heat of Battle Surrender refused.
        await Setup("russian", Unit("rl", "asl:leader", "defender-leader", "bd01:A1:0", "russian"), Unit("g9", "asl:squad", "attacker-squad", "bd01:A1:0", "german"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        var state = Current;
        var held = state with
        {
            Units = [.. state.Units.Select(unit => unit.Id == "g9" ? unit with { Custodian = "rl", Conditions = new Dictionary<string, ConditionState>(unit.Conditions) { [Conditions.Captured] = ConditionState.True } } : unit)],
        };
        Assert.Null(Planner().Captors(held, held.Unit("g2")!));
        Assert.Equal(["rl"], Planner().Captors(state, state.Unit("g2")!));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
