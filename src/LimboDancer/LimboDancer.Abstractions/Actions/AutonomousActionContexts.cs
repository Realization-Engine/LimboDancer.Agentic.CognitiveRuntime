using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Actions;

public sealed class ActionResolutionContext
{
    public ActionResolutionContext(Goal goal, StepId stepId, IEnumerable<Observation>? observations = null)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        Goal = goal;
        StepId = stepId;
        Observations = CopyObservations(goal, observations);
    }

    public Goal Goal
    {
        get;
    }

    public StepId StepId
    {
        get;
    }

    public IReadOnlyList<Observation> Observations
    {
        get;
    }

    internal static ReadOnlyCollection<Observation> CopyObservations(
        Goal goal,
        IEnumerable<Observation>? observations)
    {
        var values = (observations ?? []).ToArray();
        if (values.Any(observation => observation is null || observation.TenantId != goal.TenantId))
        {
            throw new ArgumentException("Observations must be non-null and match the Goal tenant.", nameof(observations));
        }

        return new ReadOnlyCollection<Observation>(values);
    }
}

public sealed class ConstraintContext
{
    public ConstraintContext(
        Goal goal,
        StepId stepId,
        RuntimePrincipal principal,
        RuntimeBudget budget,
        IEnumerable<Observation>? observations = null)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(budget);
        if (principal.TenantId != goal.TenantId)
        {
            throw new ArgumentException("Principal and Goal tenant must match.", nameof(principal));
        }

        Goal = goal;
        StepId = stepId;
        Principal = principal;
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

    public RuntimePrincipal Principal
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
