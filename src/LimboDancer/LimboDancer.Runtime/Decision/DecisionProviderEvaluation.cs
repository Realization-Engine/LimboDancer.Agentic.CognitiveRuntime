using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Runtime.Decision;

public sealed record DecisionEvaluationResult(
    string CaseId,
    bool Correct,
    bool ProviderFailed,
    bool DisagreedWithOriginal,
    bool WrongChoice,
    DecisionOutcome ExpectedOutcome,
    ActionRiskProfile ActionRisk,
    DecisionWrongChoiceSeverity WrongChoiceSeverity,
    bool IsAmbiguous,
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
        CaseCount = values.Length;
        CorrectCount = values.Count(static item => item.Correct);
        ProviderFailureCount = values.Count(static item => item.ProviderFailed);
        InvalidResultCount = ProviderFailureCount;
        SelectionCaseCount = values.Count(static item => item.ExpectedOutcome == DecisionOutcome.Selected);
        CorrectSelectionCount = values.Count(static item =>
            item.ExpectedOutcome == DecisionOutcome.Selected && item.Correct);
        ExpectedAbstentionCount = values.Count(static item => item.ExpectedOutcome == DecisionOutcome.Abstained);
        CorrectAbstentionCount = values.Count(static item =>
            item.ExpectedOutcome == DecisionOutcome.Abstained && item.Correct);
        AbstentionCount = values.Count(static item => item.Decision?.Outcome == DecisionOutcome.Abstained);
        UnexpectedAbstentionCount = values.Count(static item =>
            item.ExpectedOutcome != DecisionOutcome.Abstained
            && item.Decision?.Outcome == DecisionOutcome.Abstained);
        EscalationCount = values.Count(static item => item.Decision?.Outcome == DecisionOutcome.Escalated);
        DisagreementCount = values.Count(static item => item.DisagreedWithOriginal);
        AmbiguousCaseCount = values.Count(static item => item.IsAmbiguous);
        CorrectAmbiguousCount = values.Count(static item => item.IsAmbiguous && item.Correct);
        WrongChoiceCount = values.Count(static item => item.WrongChoice);
        HighOrCriticalWrongChoiceCount = values.Count(static item =>
            item.WrongChoice
            && item.WrongChoiceSeverity is DecisionWrongChoiceSeverity.High
                or DecisionWrongChoiceSeverity.Critical);
        var latencies = values
            .Where(static item => item.Decision?.Latency is not null)
            .Select(static item => item.Decision!.Latency!.Value)
            .ToArray();
        LatencySampleCount = latencies.Length;
        AverageLatency = latencies.Length == 0
            ? null
            : TimeSpan.FromTicks((long)latencies.Average(static latency => latency.Ticks));
        var confidenceErrors = values
            .Where(static item => item.Decision?.Confidence is not null)
            .Select(static item => Math.Abs(item.Decision!.Confidence!.Value - (item.Correct ? 1d : 0d)))
            .ToArray();
        ConfidenceSampleCount = confidenceErrors.Length;
        MeanAbsoluteConfidenceError = confidenceErrors.Length == 0
            ? null
            : confidenceErrors.Average();
        TotalTokens = values.Sum(static item => item.Decision?.TokenUsage?.TotalTokens ?? 0);
        TotalCost = values.Sum(static item => item.Decision?.Cost ?? 0);
    }

    public IReadOnlyList<DecisionEvaluationResult> Results
    {
        get;
    }

    public int CaseCount
    {
        get;
    }

    public int CorrectCount
    {
        get;
    }

    public double CorrectnessRate => Rate(CorrectCount, CaseCount);

    public int ProviderFailureCount
    {
        get;
    }

    public double ProviderFailureRate => Rate(ProviderFailureCount, CaseCount);

    public int InvalidResultCount
    {
        get;
    }

    public double InvalidResultRate => Rate(InvalidResultCount, CaseCount);

    public int SelectionCaseCount
    {
        get;
    }

    public int CorrectSelectionCount
    {
        get;
    }

    public double SelectionCorrectnessRate => Rate(CorrectSelectionCount, SelectionCaseCount);

    public int ExpectedAbstentionCount
    {
        get;
    }

    public int CorrectAbstentionCount
    {
        get;
    }

    public double AbstentionCorrectnessRate => Rate(CorrectAbstentionCount, ExpectedAbstentionCount);

    public int AbstentionCount
    {
        get;
    }

    public int UnexpectedAbstentionCount
    {
        get;
    }

    public int EscalationCount
    {
        get;
    }

    public int DisagreementCount
    {
        get;
    }

    public double DisagreementRate => Rate(DisagreementCount, CaseCount);

    public int AmbiguousCaseCount
    {
        get;
    }

    public int CorrectAmbiguousCount
    {
        get;
    }

    public int WrongChoiceCount
    {
        get;
    }

    public int HighOrCriticalWrongChoiceCount
    {
        get;
    }

    public int LatencySampleCount
    {
        get;
    }

    public TimeSpan? AverageLatency
    {
        get;
    }

    public int ConfidenceSampleCount
    {
        get;
    }

    public double? MeanAbsoluteConfidenceError
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

    private static double Rate(int numerator, int denominator) =>
        denominator == 0 ? 0 : (double)numerator / denominator;
}
