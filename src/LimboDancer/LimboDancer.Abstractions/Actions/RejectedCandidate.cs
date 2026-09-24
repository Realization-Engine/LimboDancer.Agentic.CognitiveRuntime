using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Actions;

public sealed class RejectedCandidate
{
    public RejectedCandidate(ActionCandidate candidate, IEnumerable<ConstraintResult> constraintResults)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(constraintResults);
        var results = constraintResults.ToArray();
        if (results.Length == 0
            || results.Any(result =>
                result is null
                || !string.Equals(result.CandidateId, candidate.CandidateId, StringComparison.Ordinal))
            || results.All(static result => result.Outcome == ConstraintOutcome.Passed))
        {
            throw new ArgumentException(
                "Rejected candidates require a failed or indeterminate result for the same candidate.",
                nameof(constraintResults));
        }

        Candidate = candidate;
        ConstraintResults = new ReadOnlyCollection<ConstraintResult>(results);
    }

    public ActionCandidate Candidate
    {
        get;
    }

    public IReadOnlyList<ConstraintResult> ConstraintResults
    {
        get;
    }
}
