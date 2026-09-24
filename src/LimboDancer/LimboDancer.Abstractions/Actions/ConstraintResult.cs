using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Actions;

public enum ConstraintAuthorityClass
{
    Semantic,
    Governance,
}

public enum ConstraintOutcome
{
    Passed,
    Failed,
    Indeterminate,
}

public sealed class ConstraintResult
{
    public ConstraintResult(
        string candidateId,
        string constraintId,
        ConstraintAuthorityClass authorityClass,
        ConstraintOutcome outcome,
        string reasonCode,
        IEnumerable<string>? evidenceRefs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(constraintId);
        if (!Enum.IsDefined(authorityClass))
        {
            throw new ArgumentOutOfRangeException(nameof(authorityClass));
        }

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

        CandidateId = candidateId;
        ConstraintId = constraintId;
        AuthorityClass = authorityClass;
        Outcome = outcome;
        ReasonCode = reasonCode;
        EvidenceRefs = new ReadOnlyCollection<string>(evidence);
    }

    public string CandidateId
    {
        get;
    }

    public string ConstraintId
    {
        get;
    }

    public ConstraintAuthorityClass AuthorityClass
    {
        get;
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
