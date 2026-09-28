using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Unit step 24 in live play (U28): a German 7.5cm leIG 18 manned by its crew fires HE at a Russian squad on the Infantry Target
/// Type, the record showing its To Hit arithmetic and IFT effect; its ROF allows another shot on a colored dr of 2 or less; its
/// Acquisition lowers the next To Hit DR; a shot that turns the Gun changes its Covered Arc. Also the berserk leader's companions
/// in live fire (A15.41), whose NTC rolls the planner now draws. Board 01's hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class OrdnanceStepsTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f124");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] R4R5 = ["r4", "r5"];
    private static readonly string[] Crew = ["de-crew"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R2 = ["r2"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-ordnance-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private readonly StubLos los = new();

    public OrdnanceStepsTests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS everywhere, at the board's true range, which the To Hit Table reads.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
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

    private static Dictionary<string, object> Gun(string id, string definition, string at, string facing, string crew, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:gun",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new
        {
            at,
            facing
        },
        ["holding"] = new
        {
            holder = crew,
            role = "manned"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task Setup(string firstSide, params Dictionary<string, object>[] placements)
    {
        var result = await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Ordnance",
                catalog = "asl-scenario-a1@1.5.0",
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

    private OrdnanceResolution LastShot(long before) =>
        Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single().Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json)!;

    [Fact]
    public async Task U28AGunFiresHeAtASquadKeepsItsRofAndItsAcquisitionLowersTheNextToHitDr()
    {
        // The leIG 18 in A2 faces south-east; the Russian squad in A5 is three hexes due south, on the boundary of its Covered Arc.
        await Setup("german", Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german"),
            Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "south-east", "de-crew", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:A5:0", "russian"));
        await Advance();
        Assert.Equal("pfph", Current.Phase);

        // C3.3: black Basic TH# 8 at three hexes, no DRM: 2 and 3 hit; the colored 2 keeps the ROF of 2 (C2.24); the IFT's 12 FP column (C.6)
        // with no TEM on the Effects DR: 6 and 6 has no effect.
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(2, 3, 6, 6), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        }));
        var first = LastShot(before);
        Assert.Equal((8, 5, true, true), (first.ToHit!.ModifiedToHit, first.ToHit.FinalDr, first.ToHit.Hit, first.Gun!.RateOfFireKept));
        Assert.Equal(12, first.Hit!.Arithmetic!.ColumnFp);
        Assert.Equal(new GunAcquisition("de-gun", BoardLocation.Parse("bd01:A5:0"), -1) { Units = ["r1"] }, Current.Acquisitions.Single());
        Assert.True(Is(Current.Find("de-gun")!, Conditions.PrepFire) && Is(Current.Unit("de-crew")!, Conditions.PrepFire));

        // The second shot has -1 Acquisition (C6.5): 3 and 4 = 6; the colored 3 loses the ROF.
        before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(3, 4, 6, 6), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        }));
        var second = LastShot(before);
        Assert.Contains(second.ToHit!.Drm, item => item.Name == "case-n" && item.Value == -1);
        Assert.Equal((6, false, -2), (second.ToHit.FinalDr, second.Gun!.RateOfFireKept, Current.Acquisitions.Single().Level));

        // No third shot, and none in the AFPh after firing in the PFPh (C2.24, C5.2).
        Assert.Contains((await Do(GameActions.FireOrdnance, NoRoll(), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        })).Reasons, reason => reason.Contains("gun-already-fired", StringComparison.Ordinal));
        await Advance(3);
        Assert.Equal("afph", Current.Phase);
        Assert.Contains((await Do(GameActions.FireOrdnance, NoRoll(), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        })).Reasons, reason => reason.Contains("gun-already-fired", StringComparison.Ordinal));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AShotOutsideTheCoveredArcTurnsTheGunWithCaseAAndALowerRof()
    {
        // Facing north-east, the Gun turns two hexspines to south-east for A5: Case A +3 +1 (C5.1), and its ROF is 1 for the shot (C2.5).
        await Setup("german", Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german"),
            Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "north-east", "de-crew", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:A5:0", "russian"));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(2, 1, 6, 6), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        }));
        var shot = LastShot(before);
        Assert.Contains(shot.ToHit!.Drm, item => item.Name == "case-a:2" && item.Value == 4);
        Assert.Equal((false, 1), (shot.Gun!.RateOfFireKept, shot.Gun.RateOfFire));
        Assert.Equal(UnitFacing.SouthEast, ((MapPosition)Current.Find("de-gun")!.Position).Facing);
        Assert.Equal(UnitFacing.SouthEast, Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single().Facing);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AHitThatBreaksTheSquadRecordsItsEffectAndTheCrewStaysWithItsGun()
    {
        await Setup("german", Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german"),
            Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "south-east", "de-crew", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:A5:0", "russian"));
        await Advance();

        // A hit (4 and 4 = 8) and 3 and 4 on the 12 FP column is a 1MC; the squad rolls 4 and 3 + 1 = 8 against 7 and breaks, within its ELR.
        Committed(await Do(GameActions.FireOrdnance, Once(4, 4, 3, 4, 4, 3), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        }));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Broken));

        // C10: a crew manning a Gun does not move away from it (ruling R24.4).
        await Advance();
        Assert.Equal("mph", Current.Phase);
        Assert.Contains((await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = Crew,
            to = "bd01:A3:0"
        })).Reasons, reason => reason.StartsWith("play.move-crew-mans-gun", StringComparison.Ordinal));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task TableFindingsACrewNeitherStrandsTheGameNorFiresFromMelee()
    {
        // A concealed crew fires its Gun and loses its "?" (A12.14); a crew in Melee does not fire (A11.15).
        await Setup("german", Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german", "asl:concealed"),
            Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "south-east", "de-crew", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:A5:0", "russian"),
            Unit("r2", "asl:squad", "defender-squad", "bd01:B2:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.FireOrdnance, Once(3, 5, 6, 6), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        }));
        Assert.False(Is(Current.Unit("de-crew")!, Conditions.Concealed));

        // The record turns the Gun exactly as many hexspines as its facts say; a record naming another facing does not replay.
        var events = store.Read(Scope)!.Events.ToList();
        var index = events.FindLastIndex(item => item.Payload is OrdnanceFired);
        events[index] = events[index] with
        {
            Payload = ((OrdnanceFired)events[index].Payload) with
            {
                Facing = UnitFacing.West
            }
        };
        Assert.True(Planner().Replay(events).HasErrors);

        // The Russians may not advance into the crew's Location, whose CC is not reviewed (R24.3).
        await Advance(13);
        Assert.Equal(("aph", "russian"), (Current.Phase, Current.PhasingSide));
        Assert.Contains((await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = R2,
            to = "bd01:A2:0"
        })).Reasons, reason => reason.StartsWith("play.advance-crew", StringComparison.Ordinal));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ACrewHeldInMeleeDoesNotFireItsGun()
    {
        // A11.15: a unit held in Melee fires only in CC, so its Gun does not fire.
        await Setup("german", Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german"),
            Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "south-east", "de-crew", "german"), Unit("r1", "asl:squad", "defender-squad", "bd01:A5:0", "russian"));
        await Advance();
        var state = Current;
        var held = state with
        {
            Units = [.. state.Units.Select(unit => unit.Id == "de-crew"
                ? unit with { Conditions = new Dictionary<string, ConditionState>(unit.Conditions) { [Conditions.Melee] = ConditionState.True } }
                : unit)],
        };
        Assert.StartsWith("play.ordnance-crew", LiveOrdnance.FromState(held, "de-gun", BoardLocation.Parse("bd01:A5:0")).Reason, StringComparison.Ordinal);
        Assert.NotNull(LiveOrdnance.FromState(state, "de-gun", BoardLocation.Parse("bd01:A5:0")).Shot);
    }

    [Fact]
    public async Task ABerserkChargeIntoACrewsLocationEndsInPlace()
    {
        // A berserk Russian squad next to the German crew and Gun may not charge in (CC with a crew is not reviewed, R24.3), so its charge
        // ends in place (R30.5) and no CC is required; the game moves on.
        await Setup("russian", Unit("r1", "asl:squad", "defender-squad", "bd01:A3:0", "russian", "asl:berserk"),
            Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "south-east", "de-crew", "german"));
        await Advance(2);
        Assert.Equal("mph", Current.Phase);
        Assert.Contains((await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = R1,
            to = "bd01:A2:0"
        })).Reasons, reason => reason.StartsWith("play.berserk", StringComparison.Ordinal));
        await Advance(5);
        Assert.Equal("ccph", Current.Phase);
        await Advance();
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ABerserkLeaderTakesHisCompanionsWithHimInLiveFire()
    {
        // Pass 2 defect found in pass 3: the Fire planner did not draw the NTC of a berserk leader's companions (A15.41), and asked forever.
        // The Russians' 16 FP at one hex, 4 and 6: a NMC. The 8-1 rolls 1 and 1 and a Heat of Battle DR of 5 and 5: berserk; the squad passes
        // its NMC (3 and 4) and its NTC (3 and 3 - 1 = 5) and goes berserk with him.
        await Setup("russian", Unit("r4", "asl:squad", "defender-squad", "bd01:A1:0", "russian"), Unit("r5", "asl:squad", "defender-squad", "bd01:A1:0", "russian"),
            Unit("gl", "asl:leader", "attacker-leader-8-1", "bd01:A2:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 5, 5, 3, 4, 3, 3), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "fire-berserk-check");
        Assert.True(Is(Current.Unit("gl")!, Conditions.Berserk) && Is(Current.Unit("g2")!, Conditions.Berserk));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public void ARallyRecordReadsTheBerserkChecksOfALeadersCompanions()
    {
        // The same pass 2 defect in the Rally planner and its record reader: the NTC of a berserk leader's companions (A15.41) is kept
        // under "berserkCheck:<unit>".
        var facts = JsonSerializer.SerializeToElement(new
        {
        });
        var rally = new RallyAttempted("gl", "gl2", new Dictionary<string, string> { ["rally"] = "d1", ["berserkCheck:g1"] = "d2" }, facts, facts);
        var dice = new Dictionary<string, DiceRolled>
        {
            ["d1"] = new("d1", "rally", 2, 6, [1, 1], DiceRolled.SystemSource, "player"),
            ["d2"] = new("d2", "rally-berserk-check", 2, 6, [3, 4], DiceRolled.SystemSource, "player"),
        };
        var rolls = LiveRally.Rolls(rally, dice)!;
        Assert.Equal([3, 4], rolls.BerserkChecks!["g1"]);
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
