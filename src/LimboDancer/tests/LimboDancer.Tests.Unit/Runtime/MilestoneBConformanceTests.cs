using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Decision;
using LimboDancer.Tests.Unit.Actions;

namespace LimboDancer.Tests.Unit.Runtime;

public sealed class MilestoneBConformanceTests
{
    [Fact]
    public async Task GoalProgressesThroughObservationResolutionConstraintsAndDecisionToSelection()
    {
        var tenantId = Guid.NewGuid();
        var descriptor = CreateDescriptor();
        var goal = new Goal(
            GoalId.New(),
            new CorrelationId("milestone-b"),
            tenantId,
            null,
            GoalOrigin.System,
            descriptor.Id.Value,
            GoalContractsTests.ParseJson("{}"),
            DateTimeOffset.UtcNow);
        var observation = new Observation(
            "observation-1",
            new ObservationSource("milestone-b-fixture", "1"),
            tenantId,
            DateTimeOffset.UtcNow,
            GoalContractsTests.ParseJson("""{"eligible":true}"""),
            "subject-1",
            "state-42",
            "milestone-b-fixture");
        var stepId = StepId.New();
        var invocationId = RuntimeInvocationId.New();
        var budget = GoalContractsTests.CreateBudget();
        var actionResolver = new RegisteredActionResolver(new ActionRegistry([descriptor]));
        var candidates = await actionResolver.ResolveAsync(
            new ActionResolutionContext(goal, stepId, [observation]));
        var constraintPipeline = new SemanticActionConstraintPipeline([new EligibilityEvaluator()]);
        var constraintResult = await constraintPipeline.EvaluateAsync(
            candidates,
            new ConstraintContext(
                goal,
                stepId,
                new RuntimePrincipal("principal-1", tenantId, isAuthenticated: true),
                budget,
                [observation]));
        var auditSink = new RecordingAuditSink();
        var decisionPlane = new DecisionPlane(new RuleDecisionProvider(), auditSink);

        var decision = await decisionPlane.DecideAsync(
            new DecisionContext(invocationId, goal, stepId, budget, [observation]),
            constraintResult.Permitted);

        var candidate = Assert.Single(candidates);
        Assert.Contains(observation.ObservationId, candidate.EvidenceRefs);
        Assert.Equal("state-42", candidate.StateVersions[observation.ObservationId]);
        Assert.Empty(constraintResult.Rejected);
        Assert.Single(constraintResult.Permitted);
        var selected = Assert.IsType<SelectedAction>(decision.SelectedAction);
        Assert.Same(candidate, selected.Candidate);
        Assert.Equal(SelectionOrigin.DecisionProvider, selected.Origin);
        Assert.Equal(DecisionOutcome.Selected, decision.Decision.Outcome);
        Assert.Same(decision.Decision, selected.Decision);
        Assert.DoesNotContain(
            typeof(DecisionPlaneResult).GetProperties(),
            property => property.PropertyType == typeof(AuthorizedAction));
        var decisionAudit = Assert.Single(auditSink.Events);
        Assert.Equal(AuditEventType.DecisionEvaluated, decisionAudit.EventType);
        Assert.Equal(invocationId, decisionAudit.InvocationId);
        Assert.Equal(goal.Id, decisionAudit.GoalId);
        Assert.Equal(stepId, decisionAudit.StepId);
        Assert.Null(decisionAudit.AuthorizationId);
    }

    private static ActionDescriptor CreateDescriptor()
    {
        var precondition = new PreconditionDescriptor(
            "milestone-b.eligible",
            PreconditionKind.Semantic,
            EligibilityEvaluator.Id,
            GoalContractsTests.ParseJson("{}"),
            required: true);
        return ActionRegistryTests.CreateDescriptor(preconditions: [precondition]);
    }

    private sealed class EligibilityEvaluator : ISemanticPreconditionEvaluator
    {
        public const string Id = "milestone-b:evaluator/Eligibility";

        public string EvaluatorId => Id;

        public ValueTask<SemanticPreconditionEvaluation> EvaluateAsync(
            ActionCandidate candidate,
            PreconditionDescriptor precondition,
            ConstraintContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var observation = Assert.Single(context.Observations);
            return ValueTask.FromResult(new SemanticPreconditionEvaluation(
                ConstraintOutcome.Passed,
                "milestone-b.eligible",
                [observation.ObservationId]));
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
