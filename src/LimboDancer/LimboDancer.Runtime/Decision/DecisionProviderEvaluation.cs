using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Runtime.Decision;

public sealed record DecisionEvaluationResult(
    string CaseId,
    bool Correct,
    bool ProviderFailed,
    bool DisagreedWithOriginal,
    DecisionResult? Decision);

public sealed class DecisionProviderEvaluation
{
    public DecisionProviderEvaluation(IEnumerable<DecisionEvaluationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var values = results.ToArray();
        if (values.Any(static item => item is null))
        {
            throw new ArgumentException("Evaluation results cannot contain null values.", nameof(results));
        }

        Results = new ReadOnlyCollection<DecisionEvaluationResult>(values);
        CorrectCount = values.Count(static item => item.Correct);
        ProviderFailureCount = values.Count(static item => item.ProviderFailed);
        AbstentionCount = values.Count(static item => item.Decision?.Outcome == DecisionOutcome.Abstained);
        DisagreementCount = values.Count(static item => item.DisagreedWithOriginal);
        TotalTokens = values.Sum(static item => item.Decision?.TokenUsage?.TotalTokens ?? 0);
        TotalCost = values.Sum(static item => item.Decision?.Cost ?? 0);
    }

    public IReadOnlyList<DecisionEvaluationResult> Results
    {
        get;
    }

    public int CorrectCount
    {
        get;
    }

    public int ProviderFailureCount
    {
        get;
    }

    public int AbstentionCount
    {
        get;
    }

    public int DisagreementCount
    {
        get;
    }

    public long TotalTokens
    {
        get;
    }

    public decimal TotalCost
    {
        get;
    }
}
