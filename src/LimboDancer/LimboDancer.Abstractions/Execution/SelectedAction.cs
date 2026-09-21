using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Abstractions.Execution;

public sealed class SelectedAction
{
    public SelectedAction(ActionCandidate candidate, SelectionOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (origin == SelectionOrigin.DecisionProvider)
        {
            throw new ArgumentException(
                "Decision-provider selection requires the deferred Decision contract.",
                nameof(origin));
        }

        Candidate = candidate;
        Origin = origin;
    }

    public ActionCandidate Candidate
    {
        get;
    }

    public SelectionOrigin Origin
    {
        get;
    }
}
