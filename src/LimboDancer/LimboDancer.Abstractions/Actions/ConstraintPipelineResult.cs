using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Actions;

public sealed class ConstraintPipelineResult
{
    public ConstraintPipelineResult(
        IEnumerable<PermittedAction> permitted,
        IEnumerable<RejectedCandidate> rejected)
    {
        ArgumentNullException.ThrowIfNull(permitted);
        ArgumentNullException.ThrowIfNull(rejected);
        var permittedValues = permitted.ToArray();
        var rejectedValues = rejected.ToArray();
        if (permittedValues.Any(static item => item is null)
            || rejectedValues.Any(static item => item is null))
        {
            throw new ArgumentException("Constraint pipeline results cannot contain null values.");
        }

        var candidateIds = permittedValues
            .Select(static item => item.Candidate.CandidateId)
            .Concat(rejectedValues.Select(static item => item.Candidate.CandidateId))
            .ToArray();
        if (candidateIds.Distinct(StringComparer.Ordinal).Count() != candidateIds.Length)
        {
            throw new ArgumentException("A candidate cannot appear more than once in a constraint result.");
        }

        Permitted = new ReadOnlyCollection<PermittedAction>(permittedValues);
        Rejected = new ReadOnlyCollection<RejectedCandidate>(rejectedValues);
    }

    public IReadOnlyList<PermittedAction> Permitted
    {
        get;
    }

    public IReadOnlyList<RejectedCandidate> Rejected
    {
        get;
    }
}
