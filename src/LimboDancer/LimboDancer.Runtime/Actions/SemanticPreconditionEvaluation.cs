using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public sealed class SemanticPreconditionEvaluation
{
    public SemanticPreconditionEvaluation(
        ConstraintOutcome outcome,
        string reasonCode,
        IEnumerable<string>? evidenceRefs = null)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        var evidence = (evidenceRefs ?? []).ToArray();
        if (evidence.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Evidence references cannot be empty.", nameof(evidenceRefs));
        }

        Outcome = outcome;
        ReasonCode = reasonCode;
        EvidenceRefs = new ReadOnlyCollection<string>(evidence);
    }

    public ConstraintOutcome Outcome
    {
        get;
    }

    public string ReasonCode
    {
        get;
    }

    public IReadOnlyList<string> EvidenceRefs
    {
        get;
    }
}
