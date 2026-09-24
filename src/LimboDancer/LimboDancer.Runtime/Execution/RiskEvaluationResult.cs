using System.Collections.ObjectModel;

namespace LimboDancer.Runtime.Execution;

public sealed class RiskEvaluationResult
{
    public RiskEvaluationResult(
        RiskEvaluationOutcome outcome,
        IEnumerable<string>? reasonCodes = null)
    {
        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        Outcome = outcome;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
    }

    public RiskEvaluationOutcome Outcome
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }
}
