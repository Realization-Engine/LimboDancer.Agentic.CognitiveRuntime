using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Reasoning;

public sealed class ReasoningContext
{
    public ReasoningContext(
        RuntimeInvocationId invocationId,
        Goal goal,
        StepId stepId,
        RuntimeBudget budget,
        IEnumerable<Observation>? observations = null,
        IEnumerable<ReasoningStepRecord>? history = null,
        IEnumerable<ReasoningActionOutcome>? actionOutcomes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(budget);
        var observationValues = (observations ?? []).ToArray();
        if (observationValues.Any(item => item is null || item.TenantId != goal.TenantId))
        {
            throw new ArgumentException("Observations must be non-null and match the Goal tenant.", nameof(observations));
        }

        var historyValues = (history ?? []).ToArray();
        if (historyValues.Any(static item => item is null))
        {
            throw new ArgumentException("Reasoning history cannot contain null values.", nameof(history));
        }

        var outcomeValues = (actionOutcomes ?? []).ToArray();
        if (outcomeValues.Any(static item => item is null))
        {
            throw new ArgumentException("Reasoning action outcomes cannot contain null values.", nameof(actionOutcomes));
        }

        InvocationId = invocationId;
        Goal = goal;
        StepId = stepId;
        Budget = budget;
        Observations = new ReadOnlyCollection<Observation>(observationValues);
        History = new ReadOnlyCollection<ReasoningStepRecord>(historyValues);
        ActionOutcomes = new ReadOnlyCollection<ReasoningActionOutcome>(outcomeValues);
    }

    public RuntimeInvocationId InvocationId
    {
        get;
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

    public IReadOnlyList<ReasoningStepRecord> History
    {
        get;
    }

    public IReadOnlyList<ReasoningActionOutcome> ActionOutcomes
    {
        get;
    }
}
