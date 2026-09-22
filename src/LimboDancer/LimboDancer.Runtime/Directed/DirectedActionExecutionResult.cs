using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Directed;

public sealed class DirectedActionExecutionResult
{
    public DirectedActionExecutionResult(
        DirectedActionResolution resolution,
        ExecutionGateOutcome? gateOutcome = null,
        ActionExecutionResult? execution = null,
        IEnumerable<string>? reasonCodes = null)
    {
        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        Resolution = resolution;
        GateOutcome = gateOutcome;
        Execution = execution;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
    }

    public DirectedActionResolution Resolution
    {
        get;
    }

    public ExecutionGateOutcome? GateOutcome
    {
        get;
    }

    public ActionExecutionResult? Execution
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }

    public bool Succeeded => Resolution == DirectedActionResolution.Resolved
        && GateOutcome == ExecutionGateOutcome.Authorized
        && Execution?.Succeeded == true;
}
