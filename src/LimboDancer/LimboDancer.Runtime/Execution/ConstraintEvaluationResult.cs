using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace LimboDancer.Runtime.Execution;

public sealed class ConstraintEvaluationResult
{
    public ConstraintEvaluationResult(
        ConstraintEvaluationOutcome outcome,
        IEnumerable<string>? reasonCodes = null,
        IEnumerable<KeyValuePair<string, string>>? validatedStateVersions = null)
    {
        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        Outcome = outcome;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
        ValidatedStateVersions = (validatedStateVersions ?? [])
            .ToFrozenDictionary(
                static item => item.Key,
                static item => item.Value,
                StringComparer.Ordinal);
    }

    public ConstraintEvaluationOutcome Outcome
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }

    public IReadOnlyDictionary<string, string> ValidatedStateVersions
    {
        get;
    }
}
