using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Runtime.Decision;

public sealed class DecisionProviderEvaluator
{
    public async Task<DecisionProviderEvaluation> EvaluateAsync(
        IDecisionProvider provider,
        IReadOnlyList<DecisionEvaluationCase> cases,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(cases);
        var results = new List<DecisionEvaluationResult>(cases.Count);
        foreach (var evaluationCase in cases)
        {
            ArgumentNullException.ThrowIfNull(evaluationCase);
            cancellationToken.ThrowIfCancellationRequested();
            var evidence = evaluationCase.Evidence;
            var context = new DecisionContext(
                evidence.InvocationId,
                evidence.Goal,
                evidence.StepId,
                evidence.Budget,
                evidence.Observations);
            try
            {
                var decision = await provider
                    .DecideAsync(context, evidence.PermittedCandidates, cancellationToken)
                    .ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(decision);
                if (!string.Equals(decision.ProviderId, provider.ProviderId, StringComparison.Ordinal)
                    || !string.Equals(decision.ProviderVersion, provider.ProviderVersion, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Evaluation provider identity does not match its result.");
                }

                DecisionResultValidator.Validate(decision, evidence.PermittedCandidates);
                results.Add(new DecisionEvaluationResult(
                    evaluationCase.CaseId,
                    IsCorrect(evaluationCase, decision),
                    ProviderFailed: false,
                    Disagreed(evidence.Decision, decision),
                    decision));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                results.Add(new DecisionEvaluationResult(
                    evaluationCase.CaseId,
                    Correct: false,
                    ProviderFailed: true,
                    DisagreedWithOriginal: false,
                    Decision: null));
            }
        }

        return new DecisionProviderEvaluation(results);
    }

    private static bool IsCorrect(DecisionEvaluationCase evaluationCase, DecisionResult decision) =>
        decision.Outcome == evaluationCase.ExpectedOutcome
        && (decision.Outcome != DecisionOutcome.Selected
            || evaluationCase.AcceptableCandidateIds.Contains(
                decision.SelectedCandidateId!,
                StringComparer.Ordinal));

    private static bool Disagreed(DecisionResult? original, DecisionResult replay) =>
        original is not null
        && (original.Outcome != replay.Outcome
            || !string.Equals(
                original.SelectedCandidateId,
                replay.SelectedCandidateId,
                StringComparison.Ordinal));
}
