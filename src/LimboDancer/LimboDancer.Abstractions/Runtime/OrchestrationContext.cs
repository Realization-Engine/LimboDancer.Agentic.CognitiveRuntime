using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Abstractions.Runtime;

public sealed class OrchestrationContext
{
    public OrchestrationContext(
        Goal goal,
        RuntimePrincipal principal,
        RuntimeBudget budget,
        GoalLifecycleState state,
        StepId? stepId = null,
        IEnumerable<Observation>? observations = null,
        DecisionResult? decision = null)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(budget);
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (principal.TenantId != goal.TenantId)
        {
            throw new ArgumentException("Principal and Goal tenant must match.", nameof(principal));
        }

        if (stepId is { Value: var stepValue } && stepValue == Guid.Empty)
        {
            throw new ArgumentException("A Step identifier cannot be empty.", nameof(stepId));
        }

        var observationValues = (observations ?? []).ToArray();
        if (observationValues.Any(item => item is null || item.TenantId != goal.TenantId))
        {
            throw new ArgumentException("Observations must be non-null and match the Goal tenant.", nameof(observations));
        }

        Goal = goal;
        Principal = principal;
        Budget = budget;
        State = state;
        StepId = stepId;
        Observations = new ReadOnlyCollection<Observation>(observationValues);
        Decision = decision;
    }

    public Goal Goal
    {
        get;
    }

    public RuntimePrincipal Principal
    {
        get;
    }

    public RuntimeBudget Budget
    {
        get;
    }

    public GoalLifecycleState State
    {
        get;
    }

    public StepId? StepId
    {
        get;
    }

    public IReadOnlyList<Observation> Observations
    {
        get;
    }

    public DecisionResult? Decision
    {
        get;
    }
}
