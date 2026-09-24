using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Abstractions.Execution;

public sealed class SelectedAction
{
    public SelectedAction(ActionCandidate candidate, SelectionOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (origin == SelectionOrigin.DecisionProvider)
        {
            throw new ArgumentException(
                "Decision-provider selection must be materialized from a validated Decision.",
                nameof(origin));
        }

        Candidate = candidate;
        Origin = origin;
        Decision = null;
    }

    internal SelectedAction(ActionCandidate candidate, DecisionResult decision)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.Outcome != DecisionOutcome.Selected
            || !string.Equals(decision.SelectedCandidateId, candidate.CandidateId, StringComparison.Ordinal))
        {
            throw new ArgumentException("The Decision must select the supplied candidate.", nameof(decision));
        }

        Candidate = candidate;
        Origin = SelectionOrigin.DecisionProvider;
        Decision = decision;
    }

    public ActionCandidate Candidate
    {
        get;
    }

    public SelectionOrigin Origin
    {
        get;
    }

    public DecisionResult? Decision
    {
        get;
    }
}
