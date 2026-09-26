using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The attacker's Infantry OVR declaration after a lone SMC reveal (Random Selection and Declined OVR Design, section 6;
/// U10): a decline commits the forced back through the reviewed delegation. An election (Infantry OVR Design, section 5;
/// U11, U12) rolls the NTC when another concealed unit is present, and is refused against a lone SMC with nothing changed.
/// </summary>
public sealed class DeclareOverrunTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000c311");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-ovr-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);
    private int draws;
    private string from = "bd01:D4:0";
    private int spentBeforeEntry;

    public DeclareOverrunTests() => store = new FileGameStore(root);

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog]);

    private DiceRoller Fixed(params int[] values) => new(_ => values[draws++ % values.Length] - 1);

    /// <summary>A roller that returns the given values once each, in order, and fails on any further draw.</summary>
    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? new(_ => throw new InvalidOperationException("No roll.")));

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static object Placement(string id, string definition, string at, bool concealed = false, string side = "russian") => new
    {
        id,
        kind = definition.Contains("leader", StringComparison.Ordinal) ? "asl:leader" : "asl:squad",
        definition,
        side,
        position = new
        {
            at
        },
        conditions = new Dictionary<string, bool>
        {
            ["asl:broken"] = false,
            ["asl:berserk"] = false,
            ["asl:captured"] = false,
            ["asl:melee"] = false,
            ["asl:ti"] = false,
            ["asl:disrupted"] = false,
            ["asl:concealed"] = concealed,
            ["asl:hidden"] = false,
        },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        return await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>
    /// A game whose German squad g1 (in D4 unless <see cref="from"/> says otherwise) has entered the first defender's
    /// location and revealed a lone SMC, leaving the attempt pending. <see cref="spentBeforeEntry"/> MF are spent in place first.
    /// </summary>
    private async Task Pending(DiceRoller? roller, params object[] defenders)
    {
        var play = Play(roller);
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Village test",
                catalog = "asl-scenario-a1@1.1.0",
                boards = Bd01,
                firstSide = "german",
                sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
            },
            placements = new[] { Placement("g1", "attacker-squad", from, side: "german") }.Concat(defenders).ToArray(),
        }))).Outcome);
        for (var step = 0; step < 2; step++)
        {
            Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.AdvancePhase,
                Args(new
                {
                    gameId = Scope.Game,
                    attemptId = $"advance-{step}",
                    expectedRevision = Revision
                }))).Outcome);
        }

        if (spentBeforeEntry > 0)
        {
            var existing = store.Read(Scope)!.Events;
            var spend = existing[^1] with
            {
                EventId = "spend-1",
                Revision = existing.Count + 1,
                Type = "instance-moved",
                Payload = new InstanceMoved("g1", new MapPosition(BoardLocation.Parse(from)), spentBeforeEntry),
                Causes = [],
                Visibility = null,
            };
            Assert.Equal(AppendStatus.Committed, store.Append(Scope, "Village test", existing.Count, [spend], Planner().Replay).Status);
        }

        var target = JsonSerializer.SerializeToElement(defenders[0]).GetProperty("position").GetProperty("at").GetString();
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.EnterBuilding,
            Args(new
            {
                gameId = Scope.Game,
                attemptId = "enter-1",
                expectedRevision = Revision,
                unitId = "g1",
                location = target
            }))).Outcome);
        Assert.Single(Current.OpenAttempts);
    }

    private JsonElement Declare(string choice, string attempt = "declare-1", long? expected = null) =>
        Args(new
        {
            gameId = Scope.Game,
            attemptId = attempt,
            expectedRevision = expected ?? Revision,
            unitId = "g1",
            choice
        });

    [Fact]
    public async Task AnElectionIsRefusedAndADeclineForcesTheMoverBack()
    {
        // U10 with a lone concealed leader, revealed without a roll.
        await Pending(null, Placement("l1", "defender-leader", "bd01:E4:0", concealed: true));
        var pending = Revision;

        var elect = await Play().ProposeAsync(GameActions.DeclareOverrun, Declare("elect"), Player);
        Assert.Equal(PlayOutcome.Denied, elect.Outcome);
        Assert.StartsWith(EntryDisclosure.CannotResolve, Assert.Single(elect.Plan!.Reasons), StringComparison.Ordinal);
        Assert.Equal(pending, Revision);
        Assert.Single(Current.OpenAttempts);

        var decline = await Commit(Play(), GameActions.DeclareOverrun, Declare("decline", expected: pending));
        Assert.Equal(PlayOutcome.Committed, decline.Outcome);
        var events = store.Read(Scope)!.Events.Skip((int)pending).ToArray();
        Assert.Equal(["overrun-declared", "entry-forced-back"], events.Select(item => item.Type));
        Assert.Equal(ScenarioA1.ScenarioA1ConcealedSmcOverrunPackage.Identity.ToString(), events[0].RulePackage);
        Assert.Equal(ScenarioA1.ScenarioA1PostRevealPackage.Identity.ToString(), events[1].RulePackage);
        Assert.Contains(decline.Plan!.Reasons, reason => reason.StartsWith("play.declined", StringComparison.Ordinal));
        Assert.Equal(("bd01:D4:0", 2, true), (Current.Location("g1")!.Location.ToString(), Current.Unit("g1")!.MfSpent, Current.Unit("g1")!.MovementEnded));
        Assert.Empty(Current.OpenAttempts);

        // The phase can advance again, a repeat is a replay, and the same attempt with the other choice is refused.
        Assert.Equal(PlayOutcome.Replay, (await Play().ConfirmAsync(GameActions.DeclareOverrun, Declare("decline", expected: pending), Player, decline.Correlation)).Outcome);
        var reused = await Planner().PlanAsync(GameActions.DeclareOverrun, Declare("elect", expected: pending), Tenant);
        Assert.StartsWith("play.attempt-reused", Assert.Single(reused.Reasons), StringComparison.Ordinal);
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(), GameActions.AdvancePhase,
            Args(new
            {
                gameId = Scope.Game,
                attemptId = "advance-late",
                expectedRevision = Revision
            }))).Outcome);
    }

    [Fact]
    public async Task ADeclineAfterARandomSelectionRevealsALoneSmcForcesTheMoverBack()
    {
        // l1 rolls 6 against r1's 1, so only the leader is revealed and the squad stays concealed.
        await Pending(Fixed(6, 1), Placement("l1", "defender-leader", "bd01:E4:0", concealed: true), Placement("r1", "defender-squad", "bd01:E4:0", concealed: true));
        Assert.Equal(["l1"], Current.OpenAttempts[0].Revealing);
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(), GameActions.DeclareOverrun, Declare("decline"))).Outcome);
        Assert.True(Current.Unit("g1")!.MovementEnded);
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("r1")!, Conditions.Concealed));
    }

    [Fact]
    public async Task ADeclarationNeedsAPendingAttempt()
    {
        await Pending(null, Placement("l1", "defender-leader", "bd01:E4:0", concealed: true));
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(), GameActions.DeclareOverrun, Declare("decline"))).Outcome);
        var again = await Play().ProposeAsync(GameActions.DeclareOverrun, Declare("decline", attempt: "declare-2"), Player);
        Assert.Equal(PlayOutcome.Denied, again.Outcome);
        Assert.StartsWith("play.no-pending-declaration", Assert.Single(again.Plan!.Reasons), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFailedNtcRevealsNothingFurtherAndForcesTheMoverBack()
    {
        // U11: l1 is revealed by Random Selection and r1 stays concealed; the NTC of 4, 4 + 3 (stone E4) = 11 fails morale 7.
        await Pending(Fixed(6, 1), Placement("l1", "defender-leader", "bd01:E4:0", concealed: true), Placement("r1", "defender-squad", "bd01:E4:0", concealed: true));
        var pending = Revision;
        var elect = await Commit(Play(Once(4, 4)), GameActions.DeclareOverrun, Declare("elect"));
        Assert.Equal(PlayOutcome.Committed, elect.Outcome);
        Assert.Contains(elect.Plan!.Reasons, reason => reason.StartsWith("play.elected", StringComparison.Ordinal));

        var events = store.Read(Scope)!.Events.Skip((int)pending).ToArray();
        Assert.Equal(["overrun-declared", "dice-rolled", "task-check", "entry-forced-back"], events.Select(item => item.Type));
        Assert.All(events, item => Assert.Equal(ScenarioA1.ScenarioA1OvrNtcPackage.Identity.ToString(), item.RulePackage));
        Assert.Equal(OverrunDeclared.Elected, Assert.IsType<OverrunDeclared>(events[0].Payload).Choice);
        var roll = Assert.IsType<DiceRolled>(events[1].Payload);
        Assert.Equal(("declare-1-roll-1", TaskCheck.OvrNtc), (roll.Roll, roll.Purpose));
        Assert.Equal([4, 4], roll.Values);
        var check = Assert.IsType<TaskCheck>(events[2].Payload);
        Assert.Equal((7, 11, false), (check.MoraleLevel, check.FinalDr, check.Passed));
        Assert.Equal([new TaskCheckModifier("B23.3", 3)], check.Modifiers);
        Assert.Equal(("bd01:D4:0", 2, true), (Current.Location("g1")!.Location.ToString(), Current.Unit("g1")!.MfSpent, Current.Unit("g1")!.MovementEnded));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("r1")!, Conditions.Concealed));
        Assert.Empty(Current.OpenAttempts);

        // A repeated confirmation returns the recorded rolls without drawing.
        Assert.Equal(PlayOutcome.Replay, (await Play().ConfirmAsync(GameActions.DeclareOverrun, Declare("elect", expected: pending), Player, elect.Correlation)).Outcome);
        Assert.Equal(pending + 4, Revision);
    }

    [Fact]
    public async Task APassedNtcRevealsTheOtherUnitByRandomSelectionAndForcesTheMoverBack()
    {
        // U12: 1, 2 + 3 = 6 passes; the Random Selection among the remaining concealed units then reveals r1.
        await Pending(Fixed(6, 1), Placement("l1", "defender-leader", "bd01:E4:0", concealed: true), Placement("r1", "defender-squad", "bd01:E4:0", concealed: true));
        var pending = Revision;
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(Once(1, 2, 3)), GameActions.DeclareOverrun, Declare("elect"))).Outcome);

        var events = store.Read(Scope)!.Events.Skip((int)pending).ToArray();
        Assert.Equal(["overrun-declared", "dice-rolled", "task-check", "dice-rolled", "random-selection", "conditions-changed", "entry-forced-back"],
            events.Select(item => item.Type));
        Assert.True(Assert.IsType<TaskCheck>(events[2].Payload).Passed);
        var selection = Assert.IsType<DiceRolled>(events[3].Payload);
        Assert.Equal(("declare-1-roll-2", "random-selection", 1), (selection.Roll, selection.Purpose, selection.Count));
        Assert.Equal(["r1"], Assert.IsType<RandomSelection>(events[4].Payload).Subjects);
        Assert.Equal(["russian"], events[4].Visibility);
        Assert.Equal(ConditionState.False, GameState.Condition(Current.Unit("r1")!, Conditions.Concealed));
        Assert.Equal(("bd01:D4:0", true), (Current.Location("g1")!.Location.ToString(), Current.Unit("g1")!.MovementEnded));
        Assert.Empty(Current.OpenAttempts);
    }

    [Fact]
    public async Task AWoodenBuildingGivesATemOfTwo()
    {
        // g1 in the stone L7 enters the wooden K7: 3, 3 + 2 = 8 fails morale 7.
        from = "bd01:L7:0";
        await Pending(Fixed(6, 1), Placement("l1", "defender-leader", "bd01:K7:0", concealed: true), Placement("r1", "defender-squad", "bd01:K7:0", concealed: true));
        var pending = Revision;
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(Once(3, 3)), GameActions.DeclareOverrun, Declare("elect"))).Outcome);
        var check = Assert.IsType<TaskCheck>(store.Read(Scope)!.Events[(int)pending + 2].Payload);
        Assert.Equal([new TaskCheckModifier("B23.3", 2)], check.Modifiers);
        Assert.Equal((8, false), (check.FinalDr, check.Passed));
    }

    [Fact]
    public async Task AnElectionWithFewerThanFourMfIsRefused()
    {
        spentBeforeEntry = 1;
        await Pending(Fixed(6, 1), Placement("l1", "defender-leader", "bd01:E4:0", concealed: true), Placement("r1", "defender-squad", "bd01:E4:0", concealed: true));
        var pending = Revision;
        var elect = await Play().ProposeAsync(GameActions.DeclareOverrun, Declare("elect"), Player);
        Assert.Equal(PlayOutcome.Denied, elect.Outcome);
        Assert.StartsWith("play.election-unavailable: g1 has 3 MF left", Assert.Single(elect.Plan!.Reasons), StringComparison.Ordinal);
        Assert.Equal(pending, Revision);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
