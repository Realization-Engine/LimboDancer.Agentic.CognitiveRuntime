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
            evidence.PermittedCandidates[0].Candidate.Descriptor.Risk,
            DecisionWrongChoiceSeverity.Low,
            acceptableCandidateIds: ["candidate-1"]);

        var evaluation = await DecisionProviderEvaluator.EvaluateAsync(provider, [evaluationCase]);

        var result = Assert.Single(evaluation.Results);
        Assert.True(result.Correct);
        Assert.False(result.ProviderFailed);
        Assert.False(result.DisagreedWithOriginal);
        Assert.False(result.WrongChoice);
        Assert.Equal(1, evaluation.CaseCount);
        Assert.Equal(1, evaluation.CorrectSelectionCount);
        Assert.Equal(1d, evaluation.SelectionCorrectnessRate);
        Assert.Equal(12, evaluation.TotalTokens);
        Assert.Equal(0.01m, evaluation.TotalCost);
        Assert.Null(typeof(DecisionProviderEvaluator).GetMethod("ExecuteAsync"));
        Assert.Null(typeof(DecisionProviderEvaluator).GetMethod("AuthorizeAsync"));
    }

    [Fact]
    public async Task LabeledCorpusReportsRequiredOfflineEvaluationMeasures()
    {
        var evidence = CreateEvidence();
        var risk = evidence.PermittedCandidates[0].Candidate.Descriptor.Risk;
        var cases = new[]
        {
            Case("correct-selection", DecisionOutcome.Selected, DecisionWrongChoiceSeverity.Low, true),
            Case("correct-abstention", DecisionOutcome.Abstained, DecisionWrongChoiceSeverity.Medium),
            Case("correct-escalation", DecisionOutcome.Escalated, DecisionWrongChoiceSeverity.High),
            Case("invalid-selection", DecisionOutcome.Selected, DecisionWrongChoiceSeverity.High),
            Case("critical-wrong-choice", DecisionOutcome.Abstained, DecisionWrongChoiceSeverity.Critical),
            Case("unexpected-abstention", DecisionOutcome.Selected, DecisionWrongChoiceSeverity.Medium),
        };
        var provider = new SequenceProvider(
            Decision(DecisionOutcome.Selected, "candidate-1", 0.9, 10),
            Decision(DecisionOutcome.Abstained, null, 0.8, 20),
            Decision(DecisionOutcome.Escalated, null, 0.7, 30),
            Decision(DecisionOutcome.Selected, "unknown", 0.9, 60),
            Decision(DecisionOutcome.Selected, "candidate-1", 0.9, 40),
            Decision(DecisionOutcome.Abstained, null, 0.6, 50));

        var evaluation = await DecisionProviderEvaluator.EvaluateAsync(provider, cases);

        Assert.Equal(6, evaluation.CaseCount);
        Assert.Equal(3, evaluation.CorrectCount);
        Assert.Equal(0.5, evaluation.CorrectnessRate);
        Assert.Equal(3, evaluation.SelectionCaseCount);
        Assert.Equal(1, evaluation.CorrectSelectionCount);
        Assert.Equal(1d / 3d, evaluation.SelectionCorrectnessRate, precision: 10);
        Assert.Equal(2, evaluation.ExpectedAbstentionCount);
        Assert.Equal(1, evaluation.CorrectAbstentionCount);
        Assert.Equal(0.5, evaluation.AbstentionCorrectnessRate);
        Assert.Equal(2, evaluation.AbstentionCount);
        Assert.Equal(1, evaluation.UnexpectedAbstentionCount);
        Assert.Equal(1, evaluation.EscalationCount);
        Assert.Equal(1, evaluation.InvalidResultCount);
        Assert.Equal(1d / 6d, evaluation.InvalidResultRate, precision: 10);
        Assert.Equal(1d / 6d, evaluation.ProviderFailureRate, precision: 10);
        Assert.Equal(3, evaluation.DisagreementCount);
        Assert.Equal(0.5, evaluation.DisagreementRate);
        Assert.Equal(1, evaluation.AmbiguousCaseCount);
        Assert.Equal(1, evaluation.CorrectAmbiguousCount);
        Assert.Equal(1, evaluation.WrongChoiceCount);
        Assert.Equal(1, evaluation.HighOrCriticalWrongChoiceCount);
        var wrongChoice = Assert.Single(evaluation.Results, static result => result.WrongChoice);
        Assert.Equal(DecisionWrongChoiceSeverity.Critical, wrongChoice.WrongChoiceSeverity);
        Assert.Equal(risk, wrongChoice.ActionRisk);
        Assert.Equal(5, evaluation.LatencySampleCount);
        Assert.Equal(TimeSpan.FromMilliseconds(30), evaluation.AverageLatency!.Value);
        Assert.Equal(5, evaluation.ConfidenceSampleCount);
        Assert.Equal(0.42, evaluation.MeanAbsoluteConfidenceError!.Value, precision: 10);
        Assert.Equal(50, evaluation.TotalTokens);
        Assert.Equal(0.05m, evaluation.TotalCost);

        DecisionEvaluationCase Case(
            string caseId,
            DecisionOutcome expected,
            DecisionWrongChoiceSeverity severity,
            bool ambiguous = false) => new(
                caseId,
                evidence,
                expected,
                risk,
                severity,
                ambiguous,
                expected == DecisionOutcome.Selected ? ["candidate-1"] : null);
    }

    private static DecisionResult Decision(
        DecisionOutcome outcome,
        string? candidateId,
        double confidence,
        int latencyMilliseconds) => new(
            outcome,
            candidateId,
            SequenceProvider.Id,
            "decision.corpus",
            confidence,
            providerVersion: "1",
            latency: TimeSpan.FromMilliseconds(latencyMilliseconds),
            cost: 0.01m,
            tokenUsage: new DecisionTokenUsage(8, 2));

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

    private sealed class SequenceProvider(params DecisionResult[] decisions) : IDecisionProvider
    {
        public const string Id = "test:decision/Sequence";
        private readonly Queue<DecisionResult> remaining = new(decisions);

        public string ProviderId => Id;

        public string? ProviderVersion => "1";

        public Task<DecisionResult> DecideAsync(
            DecisionContext context,
            IReadOnlyList<PermittedAction> candidates,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(remaining.Dequeue());
        }
    }
}
