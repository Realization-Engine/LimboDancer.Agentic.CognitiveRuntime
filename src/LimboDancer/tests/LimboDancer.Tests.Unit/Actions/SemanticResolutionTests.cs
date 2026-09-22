using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Actions;

public sealed class SemanticResolutionTests
{
    [Fact]
    public async Task RegisteredSemanticIntentProducesFiniteCandidate()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var resolver = new RegisteredActionResolver(new ActionRegistry([descriptor]));
        var goal = CreateGoal(descriptor.Id.Value);

        var candidates = await resolver.ResolveAsync(new ActionResolutionContext(goal, StepId.New()));

        var candidate = Assert.Single(candidates);
        Assert.Same(descriptor, candidate.Descriptor);
        Assert.Equal(goal.Inputs, candidate.Arguments);
    }

    [Fact]
    public async Task UnknownSemanticIntentProducesNoExecutableCandidate()
    {
        var resolver = new RegisteredActionResolver(
            new ActionRegistry([ActionRegistryTests.CreateDescriptor()]));

        var candidates = await resolver.ResolveAsync(new ActionResolutionContext(
            CreateGoal("ldm:action/Unknown"),
            StepId.New()));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task PassingSemanticPreconditionProducesPermittedAction()
    {
        var candidate = CreateCandidate();
        var pipeline = new SemanticActionConstraintPipeline(
            [new StubSemanticEvaluator(ConstraintOutcome.Passed)]);

        var result = await pipeline.EvaluateAsync([candidate], CreateConstraintContext());

        Assert.Same(candidate, Assert.Single(result.Permitted).Candidate);
        Assert.Empty(result.Rejected);
    }

    [Theory]
    [InlineData(ConstraintOutcome.Failed)]
    [InlineData(ConstraintOutcome.Indeterminate)]
    public async Task BlockingSemanticPreconditionRemovesCandidate(ConstraintOutcome outcome)
    {
        var candidate = CreateCandidate();
        var pipeline = new SemanticActionConstraintPipeline([new StubSemanticEvaluator(outcome)]);

        var result = await pipeline.EvaluateAsync([candidate], CreateConstraintContext());

        Assert.Empty(result.Permitted);
        var rejected = Assert.Single(result.Rejected);
        Assert.Equal(outcome, Assert.Single(rejected.ConstraintResults).Outcome);
    }

    [Fact]
    public async Task MissingSemanticEvaluatorFailsClosed()
    {
        var candidate = CreateCandidate();
        var pipeline = new SemanticActionConstraintPipeline([]);

        var result = await pipeline.EvaluateAsync([candidate], CreateConstraintContext());

        Assert.Empty(result.Permitted);
        Assert.Equal(
            "semantic.evaluator_unavailable",
            Assert.Single(Assert.Single(result.Rejected).ConstraintResults).ReasonCode);
    }

    [Fact]
    public async Task DuplicateCandidateIdentityIsRejected()
    {
        var candidate = CreateCandidate();
        var pipeline = new SemanticActionConstraintPipeline(
            [new StubSemanticEvaluator(ConstraintOutcome.Passed)]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => pipeline.EvaluateAsync([candidate, candidate], CreateConstraintContext()));
    }

    private static Goal CreateGoal(string intent) => new(
        GoalId.New(),
        new CorrelationId("semantic-resolution-test"),
        Guid.NewGuid(),
        null,
        GoalOrigin.System,
        intent,
        GoalContractsTests.ParseJson("{}"),
        DateTimeOffset.UtcNow);

    private static ActionCandidate CreateCandidate()
    {
        var precondition = new PreconditionDescriptor(
            "fake.semantic.required",
            PreconditionKind.Semantic,
            StubSemanticEvaluator.Id,
            GoalContractsTests.ParseJson("{}"),
            required: true);
        return new ActionCandidate(
            "candidate-1",
            ActionRegistryTests.CreateDescriptor(preconditions: [precondition]),
            GoalContractsTests.ParseJson("{}"));
    }

    private static ConstraintContext CreateConstraintContext()
    {
        var goal = GoalContractsTests.CreateGoal();
        return new ConstraintContext(
            goal,
            StepId.New(),
            new RuntimePrincipal("principal-1", goal.TenantId, isAuthenticated: true),
            GoalContractsTests.CreateBudget());
    }

    private sealed class StubSemanticEvaluator : ISemanticPreconditionEvaluator
    {
        public const string Id = "fake:evaluator/Semantic";
        private readonly ConstraintOutcome outcome;

        public StubSemanticEvaluator(ConstraintOutcome outcome)
        {
            this.outcome = outcome;
        }

        public string EvaluatorId => Id;

        public ValueTask<SemanticPreconditionEvaluation> EvaluateAsync(
            ActionCandidate candidate,
            PreconditionDescriptor precondition,
            ConstraintContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new SemanticPreconditionEvaluation(
                outcome,
                $"fake.semantic.{outcome.ToString().ToLowerInvariant()}",
                ["observation-1"]));
        }
    }
}
