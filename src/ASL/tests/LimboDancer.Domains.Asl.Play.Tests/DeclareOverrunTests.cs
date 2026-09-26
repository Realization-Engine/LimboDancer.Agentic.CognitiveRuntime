using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The attacker's Infantry OVR declaration after a lone SMC reveal (Random Selection and Declined OVR Design, section 6;
/// U10): a decline commits the forced back through the reviewed delegation, and an election is refused with nothing changed.
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

    public DeclareOverrunTests() => store = new FileGameStore(root);

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog]);

    private DiceRoller Fixed(params int[] values) => new(_ => values[draws++ % values.Length] - 1);

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

    /// <summary>A game whose German squad g1 in D4 has entered E4 and revealed a lone SMC, leaving the attempt pending.</summary>
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
                catalog = "asl-scenario-a1@1.0.0",
                boards = Bd01,
                firstSide = "german",
                sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
            },
            placements = new[] { Placement("g1", "attacker-squad", "bd01:D4:0", side: "german") }.Concat(defenders).ToArray(),
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

        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.EnterBuilding,
            Args(new
            {
                gameId = Scope.Game,
                attemptId = "enter-1",
                expectedRevision = Revision,
                unitId = "g1",
                location = "bd01:E4:0"
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
