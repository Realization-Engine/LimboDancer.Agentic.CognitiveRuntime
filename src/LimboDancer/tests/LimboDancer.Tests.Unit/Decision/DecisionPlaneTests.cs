using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Decision;
using LimboDancer.Runtime.Execution;
using LimboDancer.Tests.Unit.Actions;
using LimboDancer.Tests.Unit.Execution;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Decision;

public sealed class DecisionPlaneTests
{
    [Fact]
    public async Task RuleProviderSelectsOnlyPermittedCandidate()
    {
        var provider = new RuleDecisionProvider();
        var permitted = CreatePermitted("candidate-1");

        var result = await provider.DecideAsync(CreateContext(), [permitted]);

        Assert.Equal(DecisionOutcome.Selected, result.Outcome);
        Assert.Equal("candidate-1", result.SelectedCandidateId);
        Assert.Equal(RuleDecisionProvider.Id, result.ProviderId);
        Assert.Null(result.Confidence);
    }

    [Fact]
    public async Task RuleProviderAbstainsWithoutCandidatesAndEscalatesMultipleCandidates()
    {
        var provider = new RuleDecisionProvider();

        var abstained = await provider.DecideAsync(CreateContext(), []);
        var escalated = await provider.DecideAsync(
            CreateContext(),
            [CreatePermitted("candidate-1"), CreatePermitted("candidate-2")]);

        Assert.Equal(DecisionOutcome.Abstained, abstained.Outcome);
        Assert.Null(abstained.SelectedCandidateId);
        Assert.Equal(DecisionOutcome.Escalated, escalated.Outcome);
        Assert.Null(escalated.SelectedCandidateId);
    }

    [Fact]
    public async Task DecisionPlaneMaterializesSelectionAndWritesEvidenceAudit()
    {
        var audit = new RecordingAuditSink();
        var plane = new DecisionPlane(new RuleDecisionProvider(), audit);
        var context = CreateContext();
        var permitted = CreatePermitted("candidate-1");

        var result = await plane.DecideAsync(context, [permitted]);

        var selected = Assert.IsType<SelectedAction>(result.SelectedAction);
        Assert.Same(permitted.Candidate, selected.Candidate);
        Assert.Equal(SelectionOrigin.DecisionProvider, selected.Origin);
        Assert.Same(result.Decision, selected.Decision);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(AuditEventType.DecisionEvaluated, auditEvent.EventType);
        Assert.Equal(context.InvocationId, auditEvent.InvocationId);
        Assert.Equal(context.Goal.Id, auditEvent.GoalId);
        Assert.Equal(context.StepId, auditEvent.StepId);
        Assert.Equal(DecisionOutcome.Selected, auditEvent.DecisionOutcome);
        Assert.Equal(RuleDecisionProvider.Id, auditEvent.DecisionProviderId);
        Assert.Equal("candidate-1", auditEvent.CandidateId);
        Assert.Null(auditEvent.AuthorizationId);
    }

    [Fact]
    public async Task EmptyPermittedSetBypassesProviderAndAbstains()
    {
        var provider = new StubProvider(static _ => throw new InvalidOperationException("Must not be called."));
        var plane = new DecisionPlane(provider, new RecordingAuditSink());

        var result = await plane.DecideAsync(CreateContext(), []);

        Assert.Equal(0, provider.CallCount);
        Assert.Equal(DecisionOutcome.Abstained, result.Decision.Outcome);
        Assert.Null(result.SelectedAction);
    }

    [Theory]
    [InlineData(DecisionOutcome.Abstained)]
    [InlineData(DecisionOutcome.Escalated)]
    public async Task NonSelectionOutcomeCannotReachExecution(DecisionOutcome outcome)
    {
        var provider = new StubProvider(_ => new DecisionResult(
            outcome,
            null,
            StubProvider.Id,
            $"decision.{outcome.ToString().ToLowerInvariant()}",
            providerVersion: StubProvider.Version));
        var plane = new DecisionPlane(provider, new RecordingAuditSink());

        var result = await plane.DecideAsync(CreateContext(), [CreatePermitted("candidate-1")]);

        Assert.Equal(outcome, result.Decision.Outcome);
        Assert.Null(result.SelectedAction);
    }

    [Fact]
    public async Task UnknownProviderSelectionIsRejectedAndAudited()
    {
        var audit = new RecordingAuditSink();
        var provider = new StubProvider(static _ => new DecisionResult(
            DecisionOutcome.Selected,
            "unknown-candidate",
            StubProvider.Id,
            "decision.selected",
            providerVersion: StubProvider.Version));
        var plane = new DecisionPlane(provider, audit);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => plane.DecideAsync(CreateContext(), [CreatePermitted("candidate-1")]));

        var rejected = Assert.Single(audit.Events);
        Assert.Equal(AuditEventType.DecisionRejected, rejected.EventType);
        Assert.Contains("decision.invalid_result", rejected.ReasonCodes);
        Assert.Null(rejected.AuthorizationId);
    }

    [Fact]
    public async Task ConfidenceNeverCreatesAuthorization()
    {
        var provider = new StubProvider(candidates => new DecisionResult(
            DecisionOutcome.Selected,
            candidates[0].Candidate.CandidateId,
            StubProvider.Id,
            "decision.selected",
            confidence: 1,
            providerVersion: StubProvider.Version));
        var plane = new DecisionPlane(provider, new RecordingAuditSink());

        var result = await plane.DecideAsync(CreateContext(), [CreatePermitted("candidate-1")]);

        Assert.NotNull(result.SelectedAction);
        Assert.DoesNotContain(
            typeof(DecisionPlaneResult).GetProperties(),
            property => property.PropertyType == typeof(AuthorizedAction));
    }

    [Fact]
    public async Task DecisionSelectionIsRevalidatedByExecutionGate()
    {
        var permitted = CreatePermitted("candidate-1");
        var plane = new DecisionPlane(new RuleDecisionProvider(), new RecordingAuditSink());
        var decision = await plane.DecideAsync(CreateContext(), [permitted]);
        var gate = ExecutionGateTests.CreateGate(
            permitted.Candidate.Descriptor,
            new ConstraintEvaluationResult(
                ConstraintEvaluationOutcome.Stale,
                ["state.version_changed"]));

        var gateResult = await gate.AuthorizeAsync(
            decision.SelectedAction!,
            ExecutionGateTests.CreateContext());

        Assert.Equal(ExecutionGateOutcome.Stale, gateResult.Outcome);
        Assert.Null(gateResult.AuthorizedAction);
    }

    private static DecisionContext CreateContext()
    {
        var goal = GoalContractsTests.CreateGoal();
        return new DecisionContext(
            RuntimeInvocationId.New(),
            goal,
            StepId.New(),
            GoalContractsTests.CreateBudget());
    }

    private static PermittedAction CreatePermitted(string candidateId) => new(
        AutonomousActionContractsTests.CreateCandidate(candidateId),
        [AutonomousActionContractsTests.CreateConstraint(candidateId, ConstraintOutcome.Passed)]);

    private sealed class StubProvider(
        Func<IReadOnlyList<PermittedAction>, DecisionResult> decide) : IDecisionProvider
    {
        public const string Id = "test:decision/Stub";
        public const string Version = "1";

        public int CallCount
        {
            get;
            private set;
        }

        public string ProviderId => Id;

        public string ProviderVersion => Version;

        public Task<DecisionResult> DecideAsync(
            DecisionContext context,
            IReadOnlyList<PermittedAction> candidates,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(candidates);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(decide(candidates));
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        private readonly List<RuntimeAuditEvent> events = [];

        public IReadOnlyList<RuntimeAuditEvent> Events => events;

        public ValueTask WriteAsync(
            RuntimeAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);
            cancellationToken.ThrowIfCancellationRequested();
            events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
