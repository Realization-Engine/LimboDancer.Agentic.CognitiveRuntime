using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Evidence;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Decision;
using LimboDancer.Tests.Unit.Actions;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Decision;

public sealed class DecisionProviderEvaluatorTests
{
    [Fact]
    public async Task ReplayEvaluationComparesWithoutCreatingAuthority()
    {
        var evidence = CreateEvidence();
        var provider = new EvaluationProvider();
        var evaluationCase = new DecisionEvaluationCase(
            "selected-case",
            evidence,
            DecisionOutcome.Selected,
            ["candidate-1"]);

        var evaluation = await DecisionProviderEvaluator.EvaluateAsync(provider, [evaluationCase]);

        var result = Assert.Single(evaluation.Results);
        Assert.True(result.Correct);
        Assert.False(result.ProviderFailed);
        Assert.False(result.DisagreedWithOriginal);
        Assert.Equal(12, evaluation.TotalTokens);
        Assert.Equal(0.01m, evaluation.TotalCost);
        Assert.Null(typeof(DecisionProviderEvaluator).GetMethod("ExecuteAsync"));
        Assert.Null(typeof(DecisionProviderEvaluator).GetMethod("AuthorizeAsync"));
    }

    private static RuntimeStepEvidence CreateEvidence()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var candidate = new ActionCandidate(
            "candidate-1",
            descriptor,
            GoalContractsTests.ParseJson("{}"));
        var permitted = new PermittedAction(candidate, []);
        var goal = new Goal(
            GoalId.New(),
            new CorrelationId("decision-evaluation-test"),
            Guid.NewGuid(),
            null,
            GoalOrigin.System,
            descriptor.Id.Value,
            GoalContractsTests.ParseJson("{}"),
            DateTimeOffset.UtcNow);
        var original = new DecisionResult(
            DecisionOutcome.Selected,
            candidate.CandidateId,
            EvaluationProvider.Id,
            "decision.expected",
            providerVersion: "1");
        return new RuntimeStepEvidence(
            Guid.NewGuid(),
            RuntimeInvocationId.New(),
            goal,
            StepId.New(),
            new RuntimeBudget(1, DateTimeOffset.UtcNow.AddMinutes(1), 100, 1m, 0, 0),
            DateTimeOffset.UtcNow,
            candidates: [candidate],
            permittedCandidates: [permitted],
            decision: original);
    }

    private sealed class EvaluationProvider : IDecisionProvider
    {
        public const string Id = "test:decision/Evaluation";

        public string ProviderId => Id;

        public string? ProviderVersion => "1";

        public Task<DecisionResult> DecideAsync(
            DecisionContext context,
            IReadOnlyList<PermittedAction> candidates,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new DecisionResult(
                DecisionOutcome.Selected,
                Assert.Single(candidates).Candidate.CandidateId,
                ProviderId,
                "decision.expected",
                providerVersion: ProviderVersion,
                cost: 0.01m,
                tokenUsage: new DecisionTokenUsage(10, 2)));
        }
    }
}
