using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Decision;

public sealed class DecisionPlaneResult
{
    public DecisionPlaneResult(DecisionResult decision, SelectedAction? selectedAction)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if ((decision.Outcome == DecisionOutcome.Selected) != (selectedAction is not null))
        {
            throw new ArgumentException(
                "Only a selected Decision may materialize a selected action.",
                nameof(selectedAction));
        }

        if (selectedAction is not null && !ReferenceEquals(selectedAction.Decision, decision))
        {
            throw new ArgumentException("Selected action must retain the Decision evidence.", nameof(selectedAction));
        }

        Decision = decision;
        SelectedAction = selectedAction;
    }

    public DecisionResult Decision
    {
        get;
    }

    public SelectedAction? SelectedAction
    {
        get;
    }
}
