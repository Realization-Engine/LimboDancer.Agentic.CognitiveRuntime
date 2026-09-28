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
/// The backlog pass 5 in live play: Double Time, CX, and portage (R5.1 to R5.5); No Quarter and Massacre (R5.6, R5.7); the owners' options
/// as pending choices (R5.8, R5.9); a hero created in the MPh moving on (R5.11); a HS keeping its squad's SW (R5.12); Acquisition following
/// its units (R5.13); a vehicle's Motion and MP left (R5.14, R5.15); the Recall and Abandonment (R5.16 to R5.18); and grain out of season
/// (R5.19). Board 01's hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass5Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f505");
    private static readonly GameScope Scope = new(Tenant, "pass5");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] G3 = ["g3"];
    private static readonly string[] GH = ["gh"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] R4R5 = ["r4", "r5"];
    private static readonly string[] G4Gl = ["g4", "gld"];
    private static readonly string[] G7Gl = ["g7", "gld2"];
    private static readonly string[] R6 = ["r6"];
    private static readonly string[] G5 = ["g5"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass5-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public BacklogPass5Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS at the board's true range, except to the Locations named blocked.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public HashSet<BoardLocation> Blocked { get; } = [];

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) => Blocked.Contains(target) || Blocked.Contains(from)
            ? new(LosStatus.Blocked, true, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty)
            : new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
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

    private static Dictionary<string, object> Squad(string id, string at, string side, params string[] states) =>
        Unit(id, "asl:squad", side == "german" ? "attacker-squad" : "defender-squad", at, side, states);

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

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string facing = "east", params string[] states)
    {
        var conditions = new Dictionary<string, bool>();
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = "asl:vehicle",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Gun(string id, string definition, string at, string facing, string crew, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:gun",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
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

    private async Task Setup(string firstSide, int? month, params Dictionary<string, object>[] placements)
    {
        var start = new Dictionary<string, object>
        {
            ["label"] = "Pass 5",
            ["catalog"] = "asl-scenario-a1@1.5.0",
            ["boards"] = Bd01,
            ["firstSide"] = firstSide,
            ["sides"] = new object[]
            {
                new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
            },
        };
        if (month is { } value)
        {
            start["scenarioMonth"] = value;
        }

        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start,
            placements,
        })));
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

    private Task<PlayResult> Move(string[] units, string to, bool doubleTime = false, DiceRoller? roller = null) =>
        Do(GameActions.Move, roller ?? NoRoll(), new
        {
            unitIds = units,
            to,
            doubleTime
        });

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private async Task EndMove() => Committed(await Do(GameActions.EndMove, NoRoll(), new
    {
    }));

    private Task<PlayResult> Step(string vehicle, string kind, string? to = null, string? facing = null)
    {
        var arguments = new Dictionary<string, string> { ["vehicleId"] = vehicle, ["kind"] = kind };
        if (to is not null)
        {
            arguments["to"] = to;
        }

        if (facing is not null)
        {
            arguments["facing"] = facing;
        }

        return Do(GameActions.MoveVehicle, NoRoll(), arguments);
    }

    private Task<PlayResult> Choose(string key, string option, DiceRoller? roller = null) => Do(GameActions.Choose, roller ?? NoRoll(), new
    {
        key,
        option
    });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static BoardLocation At(string location) => BoardLocation.Parse(location);

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task DoubleTimeAddsMfMakesTheUnitCxAndPortageCountsAgainstIts()
    {
        // A German squad carrying the 4PP MMG has 3 MF (A4.42); Double Time on its first step gives it 4: 4 + 2 - (4 - 2) with the CX IPC
        // of two (A4.5, A4.52 EX).
        await Setup("german", 7, Squad("g1", "bd01:A1:0", "german"), Weapon("gm", "attacker-mmg", "g1", "german"), Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Assert.Equal("mph", Current.Phase);
        Refused(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:A2:0",
            doubleTime = true,
            assault = true
        }), "play.move-assault");
        Committed(await Move(G1, "bd01:A2:0", doubleTime: true));
        var g1 = Current.Unit("g1")!;
        Assert.Equal((true, 2), (Is(g1, Conditions.Cx), g1.DoubleTimeMf));
        await Pass();

        // Open Ground (1), then the woods of A4 (2): four MF spent in all; one more MF is more than it has.
        Committed(await Move(G1, "bd01:A3:0"));
        await Pass();
        Committed(await Move(G1, "bd01:A4:0"));
        await Pass();
        Refused(await Move(G1, "bd01:A5:0"), "play.move-mf");
        await EndMove();

        // A4.51: the CX squad's AFPh fire at the Russian squad adds one to the IFT DR.
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 5), new
        {
            firers = G1,
            target = "bd01:J9:0"
        }));
        var record = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Contains(record.Resolution.GetProperty("arithmetic").GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "cx:g1");

        // The counter stays through the Russian Player Turn and leaves at the start of the German's next MPh, where g1 may not Double Time.
        await Advance(11);
        Assert.True(Is(Current.Unit("g1")!, Conditions.Cx));
        await Advance(3);
        Assert.Equal(("mph", "german"), (Current.Phase, Current.PhasingSide));
        Assert.False(Is(Current.Unit("g1")!, Conditions.Cx));
        Refused(await Move(G1, "bd01:A5:0", doubleTime: true), "play.move-double-time");
        Committed(await Move(G1, "bd01:A5:0"));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnAdvanceIntoDifficultTerrainMakesTheUnitCxAndAnAlreadyCxUnitMayNotMakeIt()
    {
        // g1 carries the MMG and the LMG, 5PP: 4 - 2 = 2 MF, so the woods' 2 MF are all of its allotment (A4.72); g2, as laden, Double Times
        // into A5 and is CX. g3 carries two MMG, 8PP, and has no MF left after portage.
        await Setup("german", 7, Squad("g1", "bd01:A3:0", "german"), Weapon("gm", "attacker-mmg", "g1", "german"), Weapon("gl", "attacker-lmg", "g1", "german"),
            Squad("g2", "bd01:A6:0", "german"), Weapon("gm2", "attacker-mmg", "g2", "german"), Weapon("gl2", "attacker-lmg", "g2", "german"),
            Squad("g3", "bd01:B3:0", "german"), Weapon("gm3", "attacker-mmg", "g3", "german"), Weapon("gm4", "attacker-mmg", "g3", "german"),
            Squad("g4", "bd01:A3:0", "german"), Weapon("gm5", "attacker-mmg", "g4", "german"), Weapon("gl5", "attacker-lmg", "g4", "german"),
            Unit("gld", "asl:leader", "attacker-leader-6-plus-1", "bd01:A3:0", "german"),
            Squad("g7", "bd01:B2:0", "german"), Weapon("gm7", "attacker-mmg", "g7", "german"), Unit("gld2", "asl:leader", "attacker-leader-7-0", "bd01:B2:0", "german"),
            Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Move(G2, "bd01:A5:0", doubleTime: true));
        await Pass();
        await EndMove();
        await Advance(4);
        Assert.Equal("aph", Current.Phase);
        Refused(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G2,
            to = "bd01:A4:0"
        }), "play.advance-difficult-terrain");
        Refused(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G3,
            to = "bd01:B4:0"
        }), "play.advance-mf");

        // g4 carries the MMG and the LMG, 5PP, with the leader gld advancing alongside: the woods are all of g4's 2 MF, where the leader's
        // MF and IPC bonus, not built, would matter (A4.72 EX, A4.12; R5.5). g7 carries the MMG, 3 MF, and advances with gld2 into Open
        // Ground, which is not Difficult either way.
        Refused(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G4Gl,
            to = "bd01:A4:0"
        }), "play.advance-leader-bonus");
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G7Gl,
            to = "bd01:B1:0"
        }));
        Assert.False(Is(Current.Unit("g7")!, Conditions.Cx));
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G1,
            to = "bd01:A4:0"
        }));
        Assert.True(Is(Current.Unit("g1")!, Conditions.Cx));
    }

    [Fact]
    public async Task TheCaptorMayRejectASurrenderAndARussianUnitMayMassacreAPrisoner()
    {
        await Setup("russian", 7, Squad("r4", "bd01:A1:0", "russian"), Squad("r5", "bd01:A1:0", "russian"), Squad("r6", "bd01:A1:0", "russian"),
            Squad("g2", "bd01:A2:0", "german"), Squad("g3", "bd01:B1:0", "german"));
        await Advance();

        // A NMC; g2's Original 2 passes, and its Heat of Battle DR 6+6 = 12 is a Surrender to the ADJACENT Russians (A15.5). The Russians
        // reject it: g2 is eliminated and the German side is faced with No Quarter (A20.3).
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 6, 6), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Assert.Single(Current.PendingSurrenders);
        Committed(await Do(GameActions.TakePrisoner, NoRoll(), new
        {
            unitId = "g2",
            reject = true
        }));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("g2")!.Status);
        Assert.Equal(["german"], Current.NoQuarter);
        Assert.Empty(Current.PendingSurrenders);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ARussianUnitMassacresAPrisonerInItsFirePhaseAndABerserkUnitAtItsStart()
    {
        // The captured German HS g9 is held in A1 with the Russians; the German berserk squad g1 in B1 holds the Russian prisoner r9.
        await Setup("russian", 7, Squad("r6", "bd01:A1:0", "russian"), Unit("g9", "asl:half-squad", "attacker-half-squad", "bd01:A1:0", "german", Conditions.Captured),
            Unit("g8", "asl:half-squad", "attacker-half-squad", "bd01:A1:0", "german", Conditions.Captured),
            Squad("g1", "bd01:C8:0", "german", Conditions.Berserk), Unit("r9", "asl:half-squad", "defender-half-squad", "bd01:C8:0", "russian", Conditions.Captured));

        // A20.4 (R5.7): in the Russian PFPh, r6 may massacre g9 as if using a SW, once per phase, and keeps its inherent FP; the German ELR
        // rises from 3 to 4, and the Germans are faced with No Quarter. The berserk g1 does not massacre in its enemy's PFPh.
        await Advance();
        Assert.Equal("pfph", Current.Phase);
        Refused(await Do(GameActions.Massacre, NoRoll(), new
        {
            unitId = "g1",
            prisonerId = "r9"
        }), "play.massacre-phase");
        Committed(await Do(GameActions.Massacre, NoRoll(), new
        {
            unitId = "r6",
            prisonerId = "g9"
        }));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("g9")!.Status);
        Assert.False(Is(Current.Unit("r6")!, Conditions.PrepFire));
        Refused(await Do(GameActions.Massacre, NoRoll(), new
        {
            unitId = "r6",
            prisonerId = "g8"
        }), "play.massacre-once");
        Assert.Equal(4, Current.Side("german")!.Elr!.Value);
        Assert.Equal(["german"], Current.NoQuarter);

        // Having massacred in the PFPh as if using a SW, r6 has Prep Fired and does not move (A20.4, A3.3).
        await Advance();
        Assert.Equal("mph", Current.Phase);
        Refused(await Move(R6, "bd01:A2:0"), "play.move-prep-fire");

        // The berserk g1 massacres r9 at the start of the Russian DFPh, its side's fire phase, and returns to normal; the Russian ELR rises
        // from 2 to 3 (A20.4).
        await Advance();
        Assert.Equal("dfph", Current.Phase);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("r9")!.Status);
        Assert.Equal(InstanceStatus.Active, Current.Unit("g8")!.Status);
        Assert.False(Is(Current.Unit("g1")!, Conditions.Berserk));
        Assert.Equal(3, Current.Side("russian")!.Elr!.Value);
        Assert.Equal(["german", "russian"], Current.NoQuarter.Order(StringComparer.Ordinal));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task BattleHardeningMayBeRefusedAndNothingElseHappensUntilTheOwnerAnswers()
    {
        await Setup("russian", 7, Squad("r4", "bd01:A1:0", "russian"), Squad("r5", "bd01:A1:0", "russian"), Squad("g2", "bd01:A2:0", "german"));
        await Advance();

        // 3+3 = 6: a hero and Battle Hardening (A15.21, A15.3); the attack waits for the German side (R5.8), and the phase may not change.
        Committed(await Do(GameActions.Fire, Once(4, 6, 1, 1, 3, 3), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        Assert.Equal((ChoicePending.BattleHardening, "german"), (Current.Choice!.Kind, Current.Choice.Side));
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }), "play.choice-pending");
        Refused(await Choose("battleHardening:g2", "maybe"), "play.choice-option");
        Committed(await Choose("battleHardening:g2", "decline"));
        Assert.Null(Current.Choice);
        Assert.Equal(("attacker-squad", InstanceStatus.Active), (Current.Unit("g2")!.Definition!.Definition, Current.Unit("g2")!.Status));
        Assert.Single(Current.Units, unit => unit.Kind == "asl:hero");
        var record = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<FireResolved>().Single();
        Assert.Equal("decline", record.Facts.GetProperty("choices").GetProperty("battleHardening:g2").GetString());
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task TheSelfRallyingSideMayDeclineTheLeaderCreationDr()
    {
        await Setup("russian", 7, Unit("r2", "asl:squad", "defender-squad", "bd01:A1:0", "russian", Conditions.Broken));

        // A18.11: the first MMC Self-Rally's Original 2 rallies r2; its side declines the dr, so none is rolled and no leader is created.
        Committed(await Do(GameActions.Rally, Once(1, 1), new
        {
            unitId = "r2"
        }));
        Assert.Equal(ChoicePending.LeaderCreation, Current.Choice!.Kind);
        Committed(await Choose("leaderCreation:r2", "decline"));
        Assert.False(Is(Current.Unit("r2")!, Conditions.Broken));
        Assert.DoesNotContain(Current.Units, unit => unit.Kind == "asl:leader");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task TheFirerMayMakeTheUnlikelyKillDrAfterAnImmobilization()
    {
        // The German HS fires its 2 FP at five hexes, halved to 1 (A7.22), directed by the 6+1: the Original 2 plus 1 is the 1 column's Kill Number
        // 3: immobilized (A7.308). The firer makes the Unlikely Kill dr: a 1 makes a burning wreck (A7.309).
        await Setup("german", 7, Unit("gh", "asl:half-squad", "attacker-half-squad", "bd01:A1:0", "german"),
            Unit("gl", "asl:leader", "attacker-leader-6-plus-1", "bd01:A1:0", "german"), Vehicle("rt", "defender-truck", "bd01:A6:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.Fire, Once(1, 1), new
        {
            firers = GH,
            director = "gl",
            target = "bd01:A6:0"
        }));
        Assert.Equal((ChoicePending.UnlikelyKill, "german"), (Current.Choice!.Kind, Current.Choice.Side));
        Committed(await Choose("unlikelyKill:rt", "take", Once(1)));
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("rt")!.Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AHeroCreatedInTheMphMovesOnWithHisCreator()
    {
        // g1 enters A2; the Russian 4-4-7 in A3 fires 8 FP with FFNAM and FFMO: 4+5-2 = 7, a 1MC. g1's Original 2 passes, and its Heat of
        // Battle DR 1+2 = 3 creates a hero, who shares g1's movement status (A15.21, R5.11) and moves on with it.
        await Setup("german", 7, Squad("g1", "bd01:A1:0", "german"), Squad("r1", "bd01:A3:0", "russian"));
        await Advance(2);
        Committed(await Move(G1, "bd01:A2:0"));
        Committed(await Do(GameActions.Fire, Once(4, 5, 1, 1, 1, 2), new
        {
            firers = R1,
            target = "bd01:A2:0"
        }));
        var hero = Current.Units.Single(unit => unit.Kind == "asl:hero");
        Assert.Contains(hero.Id, Current.Movement!.Members);
        Assert.Equal((1, false), (hero.MfSpent, hero.MovementEnded));
        await Pass();
        Committed(await Move(["g1", hero.Id], "bd01:B1:0"));
        Assert.Equal(At("bd01:B1:0"), Current.Location(hero.Id)!.Location);
    }

    [Fact]
    public async Task AHalfSquadKeepsTheSwOfTheSquadReducedToIt()
    {
        // 16 FP, 1+3 = 4: K/3 (A7.302). g2 is Reduced to its HS, which keeps the LMG (R5.12), and passes its 3MC with 1+2 = 3 + 3 = 6.
        await Setup("russian", 7, Squad("r4", "bd01:A1:0", "russian"), Squad("r5", "bd01:A1:0", "russian"), Squad("g2", "bd01:A2:0", "german"),
            Weapon("glmg", "attacker-lmg", "g2", "german"));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(1, 3, 4, 1, 2), new
        {
            firers = R4R5,
            target = "bd01:A2:0"
        }));
        var half = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Kind == "asl:half-squad" && unit.Side == "german");
        Assert.Equal(new Holding(half.Id, HoldingRole.Possessed), Current.Equipment.Single(item => item.Id == "glmg").Holding);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnAcquisitionFollowsItsUnitsAndStaysWhereTheyLeftTheGunsLos()
    {
        // The leIG 18 in A2 hits the Russian squads in A5: 2+3 hits, 6+6 has no effect; the Acquisition is on r1 and r2 (C6.5).
        await Setup("german", 7, Unit("de-crew", "asl:crew", "attacker-crew", "bd01:A2:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A2:0", "south-east", "de-crew", "german"),
            Squad("r1", "bd01:A5:0", "russian"), Squad("r2", "bd01:A5:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.FireOrdnance, Once(2, 3, 6, 6), new
        {
            gunId = "de-gun",
            target = "bd01:A5:0"
        }));
        Assert.Equal(["r1", "r2"], Current.Acquisitions.Single().Units);

        // In the Russian MPh r1 moves alone to A6, in the Gun's LOS; when it ends its move apart from r2, the Germans choose which Location keeps
        // the Acquisition (C6.51, R5.13).
        await Advance(9);
        Assert.Equal(("mph", "russian"), (Current.Phase, Current.PhasingSide));
        Committed(await Move(R1, "bd01:A6:0"));
        await Pass();
        await EndMove();
        Assert.Equal((ChoicePending.Acquisition, "german"), (Current.Choice!.Kind, Current.Choice.Side));
        Committed(await Choose("acquisition:de-gun", "bd01:A6:0"));
        Assert.Equal(At("bd01:A6:0"), Current.Acquisitions.Single().Location);
        Assert.Equal(["r1"], Current.Acquisitions.Single().Units);

        // r2 no longer carries it; r1 moves into A7, out of the Gun's LOS: the counter stays in A6, on no unit.
        los.Blocked.Add(At("bd01:A7:0"));
        Committed(await Move(R2, "bd01:B5:0"));
        await Pass();
        await EndMove();
        Assert.Null(Current.Choice);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AVehicleNamesTheHexItWishedToEnterToEndInMotionAndSpendsItsMpLeftInItsHex()
    {
        // The truck spends its MP entering Open Ground hexes of its VCA (4 MP each) until it has at most 4 left; a hex it could still enter may
        // not be named to end in Motion, and one costing more than its MP left, with any VCA change, may (D2.4, R5.14). Its MP left are spent
        // in its hex (D2.1, R5.15).
        await Setup("german", 7, Vehicle("de-t", "attacker-truck", "bd01:A8:0", "german"), Squad("r1", "bd01:J1:0", "russian"));
        await Advance(2);
        Committed(await Step("de-t", "start"));
        await Pass();
        var handle = Board01Fixture.Handle();
        while (GamePlanner.VehicleHalfMp(Current.Unit("de-t")!) is var (spent, allotment) && allotment - spent > 8)
        {
            var entry = Planner().VehicleEntries(Current, Current.Unit("de-t")!).First(item => item.HalfMp == 8);
            Committed(await Step("de-t", "enter", to: entry.To.ToString()));
            await Pass();
        }

        var truck = Current.Unit("de-t")!;
        var left = GamePlanner.VehicleHalfMp(truck).Allotment - GamePlanner.VehicleHalfMp(truck).Spent;
        Assert.InRange(left, 1, 8);
        var here = Current.Location("de-t")!.Location;
        var neighbors = Enum.GetValues<Maps.Geometry.HexsideDirection>().Select(side => handle.Neighbor(here.Hex, side)).OfType<HexName>()
            .Select(hex => new BoardLocation(here.Board, hex, 0)).Where(hex => handle.Resolve(hex).Read?.Level.Terrain?.Name == "Open Ground").ToArray();
        var entries = Planner().VehicleEntries(Current, truck);
        if (entries.FirstOrDefault(item => item.HalfMp <= left) is { To: { } ahead })
        {
            Refused(await Do(GameActions.EndMove, NoRoll(), new
            {
                intended = ahead.ToString()
            }), "play.vehicle-motion");
        }

        var ended = false;
        foreach (var aside in entries.Where(item => item.HalfMp > left).Select(item => item.To).Concat(neighbors.Where(hex => entries.All(item => item.To != hex))))
        {
            if ((await Do(GameActions.EndMove, NoRoll(), new
            {
                intended = aside.ToString()
            })).Outcome == PlayOutcome.Committed)
            {
                ended = true;
                break;
            }
        }

        Assert.True(ended);
        Assert.True(Current.Movement is { Ending: true, WindowOpen: true });
        await Pass();
        Assert.True(Is(Current.Unit("de-t")!, Conditions.Motion));
        Assert.Equal(0, GamePlanner.VehicleHalfMp(Current.Unit("de-t")!).Allotment - GamePlanner.VehicleHalfMp(Current.Unit("de-t")!).Spent);
    }

    [Fact]
    public async Task ARecalledAfvLeavesByItsFriendlyBoardEdgeAndIsRecordedAsExited()
    {
        // The halftrack in A8 faces west, its VCA off the left edge, the German Friendly Board Edge (R5.16); its Recall shows Recall; +1.
        await Setup("german", 7, Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german", "west", Conditions.Recalled, Conditions.StunRecovery),
            Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }), "play.recall-move");
        var halftrack = Current.Unit("de-ht")!;
        Assert.True(GamePlanner.MayStartVehicleMove(halftrack));
        Assert.True(GamePlanner.MayChangeExposure(halftrack));
        Assert.False(GamePlanner.MayStartVehicleMove(halftrack with
        {
            Conditions = new Dictionary<string, ConditionState>(halftrack.Conditions) { [Conditions.StunRecovery] = ConditionState.False }
        }));
        Assert.Equal(["left"], Planner().VehicleExits(Current, halftrack).Select(exit => Planner().ExitEdge(Current, halftrack, exit)).Distinct());
        Committed(await Step("de-ht", "start"));
        await Pass();
        Refused(await Step("de-ht", "stop"), "play.recall-motion");
        Refused(await Step("de-ht", "turn", facing: "north-west"), "play.recall-route");
        Committed(await Step("de-ht", "exit"));
        var gone = Current.Unit("de-ht")!;
        Assert.Equal((InstanceStatus.Exited, true), (gone.Status, gone.Position is OffMapPosition));
        Assert.Null(Current.Movement);
        await Advance();
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnImmobilizedRecalledAfvIsAbandonedAtTheEndOfThePlayerTurn()
    {
        await Setup("german", 7, Vehicle("de-ht", "attacker-halftrack", "bd01:A8:0", "german", "east", Conditions.Recalled, Conditions.Immobilized),
            Squad("r1", "bd01:J9:0", "russian"));

        // D5.341, D5.41 (R5.18): at the end of the German Player Turn the Recall flips, and the immobilized AFV's crew Abandons it.
        await Advance(8);
        Assert.Equal("russian", Current.PhasingSide);
        var halftrack = Current.Unit("de-ht")!;
        Assert.True(Is(halftrack, Conditions.Abandoned));
        var crew = Current.Units.Single(unit => unit.Kind == "asl:crew");
        Assert.Equal(("attacker-crew", At("bd01:A8:0"), true), (crew.Definition!.Definition, Current.Location(crew.Id)!.Location, Is(crew, Conditions.StunRecovery)));
    }

    [Fact]
    public async Task GrainCostsInfantryOneAndAHalfMfOnlyInItsSeason()
    {
        // B15.6 (R5.19): with A2 as grain, the squad pays 1 MF in October, 1½ in July, and a game with no month is refused.
        boards = new InMemoryBoardCatalog([Board01Fixture.Handle(changed: ("A2", "Grain"))]);
        await Setup("german", 10, Squad("g1", "bd01:A1:0", "german"), Squad("r1", "bd01:J9:0", "russian"));
        await Advance(2);
        Committed(await Move(G1, "bd01:A2:0"));
        Assert.Equal((1, false), (Current.Unit("g1")!.MfSpent, Current.Unit("g1")!.HalfMfSpent));
    }

    [Fact]
    public async Task AUnitCarryingMoreThanItsIpcMayNotWithdrawAndABerserkUnitLosesItsCx()
    {
        // g5 carries the MMG and the LMG, 5PP, in Melee in A2; a withdrawal carries no more than its IPC, and dropping a SW is not built (A11.21,
        // A4.43; R5.5). The CX g6 goes berserk and loses its CX counter (A15.42; R5.3).
        await Setup("german", 7, Squad("g5", "bd01:A3:0", "german"), Weapon("gm", "attacker-mmg", "g5", "german"),
            Weapon("gl", "attacker-lmg", "g5", "german"), Squad("r2", "bd01:A2:0", "russian"), Squad("g6", "bd01:J9:0", "german", Conditions.Cx));
        await Advance(6);
        Committed(await Do(GameActions.Advance, NoRoll(), new
        {
            unitIds = G5,
            to = "bd01:A2:0"
        }));
        await Advance();

        // 6+6 is no effect: the Melee holds through the Russian Player Turn to its CCPh.
        Committed(await Do(GameActions.CloseCombat, Once(6, 6), new
        {
            location = "bd01:A2:0",
            attacks = new[] { new { attackers = G5, defenders = R2 } },
        }));
        await Advance(8);
        Assert.Equal(("ccph", "russian"), (Current.Phase, Current.PhasingSide));
        Assert.True(Is(Current.Unit("g5")!, Conditions.Melee));
        Refused(await Do(GameActions.CloseCombat, NoRoll(), new
        {
            location = "bd01:A2:0",
            attacks = new[] { new { attackers = R2, defenders = G5 } },
            withdrawals = new Dictionary<string, string> { ["g5"] = "bd01:A3:0" },
        }), "play.cc-withdrawal-portage");
        Assert.Empty(Planner().WithdrawalDestinations(Current, Current.Unit("g5")!, At("bd01:A2:0")));
        Assert.False(Planner().MustWithdraw(Current, Current.Unit("g5")!));

        var events = store.Read(Scope)!.Events;
        var berserk = Planner().Replay([.. events, events[^1] with
        {
            EventId = "berserk-1",
            Revision = events[^1].Revision + 1,
            Causes = [],
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g6", new Dictionary<string, ConditionState> { [Conditions.Berserk] = ConditionState.True }),
        }]);
        Assert.False(Is(berserk.Current!.Unit("g6")!, Conditions.Cx));
    }
}
