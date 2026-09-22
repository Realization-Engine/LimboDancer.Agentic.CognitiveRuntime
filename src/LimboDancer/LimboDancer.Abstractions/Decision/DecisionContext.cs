using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Decision;

public sealed class DecisionContext
{
    public DecisionContext(
        Goal goal,
        StepId stepId,
        RuntimeBudget budget,
        IEnumerable<Observation>? observations = null)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(budget);
        Goal = goal;
        StepId = stepId;
        Budget = budget;
        Observations = ActionResolutionContext.CopyObservations(goal, observations);
    }

    public Goal Goal
    {
        get;
    }

    public StepId StepId
    {
        get;
    }

    public RuntimeBudget Budget
    {
        get;
    }

    public IReadOnlyList<Observation> Observations
    {
        get;
    }
}
