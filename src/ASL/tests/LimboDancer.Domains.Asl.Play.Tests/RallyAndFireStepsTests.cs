using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Unit steps 19 to 23 in live play (U21 to U27): Rally and Repair in the RPh, Advancing Fire and fire groups across
/// Locations, Dummies and fire at an empty Location, hex-by-hex movement with Defensive First Fire, Subsequent First Fire,
/// and Residual FP, and MGs with Multiple ROF and malfunction. Board 01's hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class RallyAndFireStepsTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f119");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R4R5 = ["r4", "r5"];
    private static readonly string[] R4 = ["r4"];
    private static readonly string[] R6 = ["r6"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G3 = ["g3"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] G1G3 = ["g1", "g3"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-steps-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private readonly StubLos los = new();

    public RallyAndFireStepsTests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) => new(LosStatus.Clear, false, 1, 0, null, string.Empty);
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

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side, bool malfunctioned = false) => new()
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
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = malfunctioned },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game on board 01 in the first side's RPh, July, Russian ELR 2 and German ELR 3.</summary>
    private async Task Setup(string firstSide, params Dictionary<string, object>[] placements)
    {
        var result = await Commit(Play(), GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Steps",
                catalog = "asl-scenario-a1@1.8.0",
                boards = Bd01,
                firstSide,
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 2 } },
            },
            placements,
        }));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        Assert.Equal("rph", Current.Phase);
    }

    private async Task Advance(int times = 1)
    {
        for (var index = 0; index < times; index++)
        {
            var result = await Commit(Play(), GameActions.AdvancePhase, Args(new
            {
                gameId = Scope.Game,
                attemptId = $"advance-{Revision}",
                expectedRevision = Revision
            }));
            Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
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

    [Fact]
    public async Task U32HeatOfBattleInFireCreatesAHeroAndBattleHardens()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        await Advance();

        // 16 FP, 4+6 = 10: a NMC; g2 rolls an Original 2 and passes, then its Heat of Battle DR, 2+2 = 4 with no DRM for a German
        // 1st Line squad, creates a German hero in A2 (A15.1, A15.21).
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 2, 2), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "fire-heat-of-battle");
        var hero = Current.Units.Single(unit => unit.Kind == "asl:hero");
        Assert.Equal(("attacker-hero", "german", "bd01:A2:0"), (hero.Definition!.Definition, hero.Side, Current.Location(hero.Id)!.Location.ToString()));
        Assert.Equal(InstanceStatus.Active, Current.Unit("g2")!.Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U32AHeatOfBattleSixBothCreatesAHeroAndBattleHardensTheSquad()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        await Advance();

        // 3+3 = 6: a hero, and g2 may be exchanged for the squared-E 4-6-8 (A15.3): the attack waits for the German side's answer (ruling R5.8).
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 3, 3), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Assert.Equal(("battleHardening:g2", "german"), (Current.Choice!.Key, Current.Choice.Side));
        Assert.Equal(InstanceStatus.Active, Current.Unit("g2")!.Status);
        Committed(await Do(GameActions.Choose, NoRoll(), new
        {
            key = "battleHardening:g2",
            option = "take"
        }));
        Assert.Equal(InstanceStatus.Consumed, Current.Unit("g2")!.Status);
        var elite = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Definition?.Definition == "attacker-elite-squad");
        Assert.False(Is(elite, Conditions.Broken));
        Assert.Single(Current.Units, unit => unit.Kind == "asl:hero");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U32ALeadersRallyOnAnOriginalTwoTakesHeatOfBattle()
    {
        await Setup("russian", Unit("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian", "asl:broken", "asl:dm"),
            Unit("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian"));

        // 1+1 rallies r1; its Heat of Battle DR, 1+2 = 3, +2 Russian, +1 broken: 6 creates a Russian hero and Battle Hardens
        // r1 into the squared-E 4-5-8 (A15.1, A15.21, A15.3).
        Committed(await Do(GameActions.Rally, Once(1, 1, 1, 2), new
        {
            unitId = "r1",
            leader = "rl"
        }));
        Committed(await Do(GameActions.Choose, NoRoll(), new
        {
            key = "battleHardening:r1",
            option = "take"
        }));
        var elite = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Definition?.Definition == "defender-elite-squad");
        Assert.Equal((false, false), (Is(elite, Conditions.Broken), Is(elite, Conditions.DesperationMorale)));
        var hero = Current.Units.Single(unit => unit.Kind == "asl:hero");
        Assert.Equal(("defender-hero", "bd01:D4:0"), (hero.Definition!.Definition, Current.Location(hero.Id)!.Location.ToString()));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U21ALeaderRalliesABrokenSquadUnderDm()
    {
        await Setup("russian", Unit("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian", "asl:broken", "asl:dm"),
            Unit("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian"));
        var before = Revision;

        // 1+2 = 3, +4 DM, +0 leadership, -1 wooden building: Final DR 6 against broken morale 7 rallies r1.
        var result = await Do(GameActions.Rally, Once(1, 2), new
        {
            attemptId = "rally-1",
            unitId = "r1",
            leader = "rl"
        });
        Committed(result);
        var added = Since(before);
        Assert.Equal(["dice-rolled", "rally-attempted", "conditions-changed"], added.Select(item => item.Type));
        var record = Assert.IsType<RallyAttempted>(added[1].Payload);
        Assert.Equal(6, record.Resolution.GetProperty("arithmetic").GetProperty("finalDr").GetInt32());
        Assert.False(Is(Current.Unit("r1")!, Conditions.Broken));

        // Replaying draws nothing; confirming the same attempt again returns the recorded roll.
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
        var again = await Commit(Play(), GameActions.Rally, Args(new
        {
            gameId = Scope.Game,
            attemptId = "rally-1",
            expectedRevision = before,
            unitId = "r1",
            leader = "rl"
        }));
        Assert.Equal(PlayOutcome.Replay, again.Outcome);

        // DM is removed at the end of the RPh (A10.62).
        Assert.True(Is(Current.Unit("r1")!, Conditions.DesperationMorale));
        await Advance();
        Assert.False(Is(Current.Unit("r1")!, Conditions.DesperationMorale));
    }

    [Fact]
    public async Task FateReducesTheSquadAndAFirstMmcSelfRallyCreatesALeader()
    {
        await Setup("russian", Unit("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian", "asl:broken"),
            Unit("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian"),
            Unit("r2", "asl:squad", "defender-squad", "bd01:A1:0", "russian", "asl:broken"));

        // A18.11: the first MMC Rally attempt of the side's own RPh, Self-Rally, rolls an Original 2: rallied, and a Leader
        // Creation dr of 2, +1 Russian, +1 broken (its broken Morale Level of 7 adds nothing): 4 creates a 7-0 (A18.2).
        Committed(await Do(GameActions.Rally, Once(1, 1), new
        {
            unitId = "r2"
        }));
        Assert.Equal(("leaderCreation:r2", "russian"), (Current.Choice!.Key, Current.Choice.Side));
        Committed(await Do(GameActions.Choose, Once(2), new
        {
            key = "leaderCreation:r2",
            option = "take"
        }));
        Assert.False(Is(Current.Unit("r2")!, Conditions.Broken));
        var record = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<RallyAttempted>().Single();
        Assert.Equal(4, record.Resolution.GetProperty("arithmetic").GetProperty("leaderCreation").GetProperty("finalDr").GetInt32());
        var created = Current.Units.Single(unit => unit.Definition?.Definition == "defender-leader-7-0");
        Assert.Equal(("russian", "bd01:A1:0", false), (created.Side, Current.Location(created.Id)!.Location.ToString(), Is(created, Conditions.Broken)));

        // Fate: an Original 12 Reduces r1 to a broken HS and does not rally it (A10.64).
        Committed(await Do(GameActions.Rally, Once(6, 6, 2, 2), new
        {
            unitId = "r1",
            leader = "rl"
        }));
        var half = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Definition?.Definition == "defender-half-squad");
        Assert.True(Is(half, Conditions.Broken));
    }

    [Fact]
    public async Task U22RallyRefusalsComeBeforeAnyRoll()
    {
        await Setup("russian", Unit("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian", "asl:broken"),
            Unit("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian"),
            Unit("r2", "asl:squad", "defender-squad", "bd01:A1:0", "russian", "asl:broken"),
            Unit("r3", "asl:squad", "defender-squad", "bd01:A2:0", "russian", "asl:broken", "asl:disrupted"));
        Committed(await Do(GameActions.Rally, Once(6, 5), new
        {
            unitId = "r1",
            leader = "rl"
        }));
        var revision = Revision;

        async Task Refused(object arguments, string reason)
        {
            var result = await Do(GameActions.Rally, NoRoll(), arguments);
            Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
            Assert.Contains(result.Reasons, item => item.Contains(reason, StringComparison.Ordinal));
            Assert.Equal(revision, Revision);
        }

        await Refused(new
        {
            unitId = "r1",
            leader = "rl"
        }, "already-attempted");
        await Refused(new
        {
            unitId = "r3"
        }, "disrupted-self-rally");
        await Refused(new
        {
            unitId = "r2",
            leader = "rl"
        }, "leader-outside");

        // r1's attempt spent the first MMC attempt of the RPh, so r2 would Self-Rally on a capability the catalog does not record.
        await Refused(new
        {
            unitId = "r2"
        }, "self-rally-capability-unrecorded");
        await Advance();
        revision = Revision;
        await Refused(new
        {
            unitId = "r2",
            leader = "rl"
        }, "phase-outside");
    }

    [Fact]
    public async Task RepairInTheRphRepairsOrEliminatesAMalfunctionedMg()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Weapon("mg1", "defender-mmg", "r4", "russian", malfunctioned: true),
            Weapon("mg2", "defender-lmg", "r4", "russian", malfunctioned: true));

        // R2: a dr of 2 repairs mg1; a 6 eliminates mg2 (A9.72).
        Committed(await Do(GameActions.Repair, Once(2), new
        {
            unitId = "r4",
            equipmentId = "mg1"
        }));
        Assert.False(Is(Current.Find("mg1")!, Conditions.Malfunctioned));
        Committed(await Do(GameActions.Repair, Once(6), new
        {
            unitId = "r4",
            equipmentId = "mg2"
        }));
        Assert.Equal(InstanceStatus.Eliminated, Current.Equipment.Single(item => item.Id == "mg2").Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U23AdvancingFireIsHalvedAndAGroupSpansAdjacentLocations()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("r5", "asl:squad", "defender-squad", "bd01:B1:0", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        await Advance(4);
        Assert.Equal("afph", Current.Phase);

        // A1 and B1 are ADJACENT; each squad is at range 1 by the stub LOS: 4 x 2 / 2 = 4 each, the 8 column.
        var result = await Do(GameActions.Fire, Once(6, 5), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        });
        Committed(result);
        var record = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(8m, record.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.True(record.Facts.GetProperty("firerLocationsAdjacent").GetBoolean());
        Assert.True(Is(Current.Unit("r4")!, Conditions.PrepFire));
    }

    [Fact]
    public async Task U23ASquadMarkedPrepFireMayNotFireInTheAfph()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"), Unit("g4", "asl:squad", "attacker-squad", "bd01:B1:0", "german"));
        await Advance();
        Committed(await Do(GameActions.Fire, Once(6, 5), new
        {
            firers = R4,
            target = "bd01:A2:0"
        }));
        await Advance(3);
        Assert.Equal("afph", Current.Phase);
        var revision = Revision;
        var refused = await Do(GameActions.Fire, NoRoll(), new
        {
            firers = R4,
            target = "bd01:B1:0"
        });
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Equal(revision, Revision);
    }

    [Fact]
    public async Task U24ADummyIsRemovedAndFireAtAnEmptyLocationCommits()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r6", "asl:squad", "defender-squad", "bd01:B1:0", "russian"),
            Dummy("gd", "bd01:A2:0", "german"));
        await Advance();

        // 1+2 on the 8 column (16 halved against the concealed Dummy): an effect removes it (A12.14).
        Committed(await Do(GameActions.Fire, Once(1, 2), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("gd")!.Status);

        // Fire at a Location with nothing in it commits on the concealed column; on no effect its record is not the firing
        // side's, and a public report carries the arithmetic (R21.1).
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 5), new
        {
            firers = R6,
            target = "bd01:B2:0"
        }));
        var added = Since(before);
        var fired = added.Single(item => item.Payload is FireResolved);
        Assert.Equal(["german"], fired.Visibility);
        Assert.Contains(added, item => item.Type == "fire-reported" && item.Visibility is null);
        Assert.True(Is(Current.Unit("r6")!, Conditions.PrepFire));
    }

    [Fact]
    public async Task U25DefensiveFirstFireAttacksTheMoverAndLeavesResidualFp()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("g3", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"));
        await Advance(2);
        Assert.Equal("mph", Current.Phase);

        // g1 enters B1, Open Ground, for 1 MF; the DEFENDER's window opens.
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B1:0"
        }));
        Assert.True(Current.Movement is { WindowOpen: true, Step: 1 });

        // The ATTACKER cannot move on while the window is open.
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B2:0"
        })).Outcome);

        // First Fire: 16 FP with -1 FFNAM and -1 FFMO, and 6+5 (doubles would Cower, A7.9); any check g1 takes it passes on 2+2.
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 5, 2, 2), new
        {
            firers = R4R5,
            target = "bd01:B1:0"
        }));
        var record = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(1, record.MovementStep);
        Assert.Equal("first-fire", record.Facts.GetProperty("fireKind").GetString());
        Assert.Contains(record.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "ffmo");
        Assert.Equal(8, Assert.Single(Current.ResidualFire).Fp);
        Assert.True(Is(Current.Unit("r4")!, Conditions.FirstFire));

        // A second group attack from A1 at this MF expenditure is refused (A7.55).
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.Fire, NoRoll(), new
        {
            firers = R4R5,
            target = "bd01:B1:0"
        })).Outcome);

        // The DEFENDER passes, and the ATTACKER ends g1's move.
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.True(Current.Unit("g1")!.MovementEnded);

        // U26: g3 entering B1 is attacked by the 8 Residual FP first, alone (A8.22).
        before = Revision;
        Committed(await Do(GameActions.Move, Once(6, 6, 2, 2), new
        {
            unitIds = G3,
            to = "bd01:B1:0"
        }));
        var residual = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Empty(residual.Firers);
        Assert.Equal(8, residual.Facts.GetProperty("residualFp").GetInt32());
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

        // Residual FP is gone after the MPh; First Fire after the DFPh (A8.2, A3.4).
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        await Advance();
        Assert.Empty(Current.ResidualFire);
        await Advance();
        Assert.False(Is(Current.Unit("r4")!, Conditions.FirstFire));
    }

    [Fact]
    public async Task ALeaderMovesWithSixMfAndAWoundedLeaderWithThree()
    {
        await Setup("russian", Unit("rl", "asl:leader", "defender-leader", "bd01:A1:0", "russian"),
            Unit("rw", "asl:leader", "defender-leader", "bd01:A1:0", "russian", "asl:wounded"));
        await Advance(2);
        Assert.Equal("mph", Current.Phase);
        string[] leader = ["rl"];
        string[] wounded = ["rw"];

        async Task<PlayResult> Step(string[] movers, string to)
        {
            var moved = await Do(GameActions.Move, NoRoll(), new
            {
                unitIds = movers,
                to
            });
            if (moved.Outcome == PlayOutcome.Committed)
            {
                Committed(await Do(GameActions.PassFire, NoRoll(), new
                {
                }));
            }

            return moved;
        }

        // A4.11: six MF: B1 Open Ground 1, C1 and C2 woods 2 each, B2 Open Ground 1; a seventh is refused.
        foreach (var to in new[] { "bd01:B1:0", "bd01:C1:0", "bd01:C2:0", "bd01:B2:0" })
        {
            Committed(await Step(leader, to));
        }

        Assert.Equal(6, Current.Unit("rl")!.MfSpent);
        var refused = await Step(leader, "bd01:A2:0");
        Assert.Contains(refused.Reasons, reason => reason.Contains("play.move-mf", StringComparison.Ordinal));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));

        // A17.2: a wounded leader has three.
        Committed(await Step(wounded, "bd01:B1:0"));
        Committed(await Step(wounded, "bd01:C1:0"));
        refused = await Step(wounded, "bd01:C2:0");
        Assert.Contains(refused.Reasons, reason => reason.Contains("play.move-mf", StringComparison.Ordinal));
    }

    [Fact]
    public async Task U30AStackSplitsWhenAMemberBreaksAndTheOtherMovesOn()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"), Unit("g3", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"));
        await Advance(2);
        string[] stack = ["g1", "g2"];
        string[] g2 = ["g2"];
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = stack,
            to = "bd01:B1:0"
        }));

        // Defensive First Fire on the 16 column, 3+4 - 2 = 5, a 3MC: g1 fails by 2, within its ELR, and breaks; g2 passes on 6.
        Committed(await Do(GameActions.Fire, Once(3, 4, 2, 4, 1, 2), new
        {
            firers = R4R5,
            target = "bd01:B1:0"
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Broken));
        Assert.False(Is(Current.Unit("g2")!, Conditions.Broken));
        Assert.Equal(g2, Current.Movement!.Members);
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));

        // A4.2: g2 moves on alone; g1 may not, and g3, not of the stack, waits until the stack's move ends.
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B2:0"
        })).Outcome);
        Assert.NotEqual(PlayOutcome.Committed, (await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G3,
            to = "bd01:B2:0"
        })).Outcome);
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = g2,
            to = "bd01:B2:0"
        }));
        Assert.Equal(g2, Current.Movement!.Movers);
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.Null(Current.Movement);
        Assert.True(Current.Unit("g2")!.MovementEnded);

        // The stack's move is over, so g3 moves.
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G3,
            to = "bd01:B2:0"
        }));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AUnitThatPrepFiredMayNotMoveInTheMph()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("g3", "asl:squad", "attacker-squad", "bd01:A2:0", "german"), Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.Fire, Once(6, 5), new
        {
            firers = G1,
            target = "bd01:A1:0"
        }));
        await Advance();
        Assert.Equal("mph", Current.Phase);

        // A3.3 (p. 47): g1 fired in the PFPh, so it may not move; g3 did not fire and moves, alone or with nothing that fired.
        var refused = await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1G3,
            to = "bd01:B1:0"
        });
        Assert.Contains(refused.Reasons, reason => reason.StartsWith("play.move-prep-fire: g1", StringComparison.Ordinal));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G3,
            to = "bd01:B1:0"
        }));

        // Replay refuses a movement step of a unit marked Prep Fire, whoever wrote it (UNIT-STATE-029).
        var events = store.Read(Scope)!.Events;
        var step = events[^1] with
        {
            Payload = new MovementStepped(G1, BoardLocation.Parse("bd01:B1:0"), 2, false, 1)
        };
        var history = Planner().Replay([.. events.Take(events.Count - 1), step]);
        Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-STATE-029" && item.Message.Contains("A3.3", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ASplitStackEndsOneMemberAndTheOtherMovesOn()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        await Advance(2);
        string[] stack = ["g1", "g2"];
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = stack,
            to = "bd01:B1:0"
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));

        // The ATTACKER ends g1's move and moves g2 on (A4.2).
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
            unitIds = G1
        }));
        Assert.True(Current.Unit("g1")!.MovementEnded);
        Assert.NotNull(Current.Movement);
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:B2:0"
        }));
        Assert.Equal(BoardLocation.Parse("bd01:B2:0"), Current.Location("g2")!.Location);
    }

    [Fact]
    public async Task SubsequentFirstFireMarksTheFirersWithFinalFire()
    {
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"));
        await Advance(2);
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B1:0"
        }));
        Committed(await Do(GameActions.Fire, Once(6, 5, 2, 2), new
        {
            firers = R4R5,
            target = "bd01:B1:0"
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));

        // g1 moves on to B2; r4 and r5, First Fire already, fire again as Subsequent First Fire (A8.3) and take Final Fire.
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:B2:0"
        }));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 5, 2, 2), new
        {
            firers = R4R5,
            target = "bd01:B2:0"
        }));
        var record = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(2, record.MovementStep);
        Assert.Equal("subsequent-first-fire", record.Facts.GetProperty("fireKind").GetString());
        Assert.True(Is(Current.Unit("r4")!, Conditions.FinalFire));
        Assert.False(Is(Current.Unit("r4")!, Conditions.FirstFire));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task U27AMgAddsItsFirepowerKeepsItsRofAndFiresAgainAlone()
    {
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Weapon("mg1", "defender-lmg", "r4", "russian"),
            Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"), Unit("g4", "asl:squad", "attacker-squad", "bd01:B1:0", "german"));
        await Advance();

        // 4 x 2 inherent + the LMG's 2 x 2 = 12 FP; a colored 1 keeps its ROF 1, so the LMG is not marked (A9.2).
        var weapons = new Dictionary<string, string[]> { ["r4"] = ["mg1"] };
        Committed(await Do(GameActions.Fire, Once(1, 6, 2, 2), new
        {
            firers = R4,
            weapons,
            target = "bd01:A2:0"
        }));
        var record = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal(12m, record.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal());
        Assert.True(Is(Current.Unit("r4")!, Conditions.PrepFire));
        Assert.False(Is(Current.Find("mg1")!, Conditions.PrepFire));

        // The LMG fires again alone at another target; a 5+6 reaches its B11, and it malfunctions (A9.7).
        Committed(await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = R4,
            weapons,
            withoutInherent = R4,
            target = "bd01:B1:0"
        }));
        Assert.True(Is(Current.Find("mg1")!, Conditions.Malfunctioned));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
