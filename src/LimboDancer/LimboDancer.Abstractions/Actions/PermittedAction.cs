using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.Actions;

public sealed class PermittedAction
{
    internal PermittedAction(ActionCandidate candidate, IEnumerable<ConstraintResult> constraintResults)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(constraintResults);
        var results = constraintResults.ToArray();
        if (results.Any(result =>
                result is null
                || !string.Equals(result.CandidateId, candidate.CandidateId, StringComparison.Ordinal)
                || result.Outcome != ConstraintOutcome.Passed))
        {
            throw new ArgumentException(
                "Permitted actions require only passing constraint results for the same candidate.",
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
