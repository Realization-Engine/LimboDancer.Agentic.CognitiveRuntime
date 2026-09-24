using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Verification;

public sealed class VerificationContext
{
    public VerificationContext(
        RuntimeInvocationId invocationId,
        Goal goal,
        StepId stepId,
        IEnumerable<Observation>? beforeObservations = null,
        IEnumerable<Observation>? afterObservations = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentOutOfRangeException.ThrowIfEqual(stepId.Value, Guid.Empty);
        InvocationId = invocationId;
        Goal = goal;
        StepId = stepId;
        BeforeObservations = CopyObservations(goal, beforeObservations, nameof(beforeObservations));
        AfterObservations = CopyObservations(goal, afterObservations, nameof(afterObservations));
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

    public IReadOnlyList<Observation> BeforeObservations
    {
        get;
    }

    public IReadOnlyList<Observation> AfterObservations
    {
        get;
    }

    private static ReadOnlyCollection<Observation> CopyObservations(
        Goal goal,
        IEnumerable<Observation>? observations,
        string parameterName)
    {
        var values = (observations ?? []).ToArray();
        if (values.Any(item => item is null || item.TenantId != goal.TenantId))
        {
            throw new ArgumentException(
                "Verification observations must be non-null and match the Goal tenant.",
                parameterName);
        }

        return new ReadOnlyCollection<Observation>(values);
    }
}
