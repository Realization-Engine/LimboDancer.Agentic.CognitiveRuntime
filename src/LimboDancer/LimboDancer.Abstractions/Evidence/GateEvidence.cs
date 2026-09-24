using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Abstractions.Evidence;

public sealed class GateEvidence
{
    public GateEvidence(
        ExecutionGateOutcome outcome,
        IEnumerable<string>? reasonCodes = null,
        DiagnosticDisposition? diagnosticDisposition = null,
        string? authorizationId = null)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if ((outcome == ExecutionGateOutcome.Authorized) != (authorizationId is not null))
        {
            throw new ArgumentException(
                "Only an authorized gate result may reference an authorization.",
                nameof(authorizationId));
        }

        if (authorizationId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authorizationId);
        }

        if (diagnosticDisposition is not null && outcome != ExecutionGateOutcome.DiagnosticBlocked)
        {
            throw new ArgumentException(
                "A diagnostic disposition is valid only for a diagnostic-blocked gate result.",
                nameof(diagnosticDisposition));
        }

        var reasons = (reasonCodes ?? []).ToArray();
        if (reasons.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Gate reason codes cannot be empty.", nameof(reasonCodes));
        }

        Outcome = outcome;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
        DiagnosticDisposition = diagnosticDisposition;
        AuthorizationId = authorizationId;
    }

    public ExecutionGateOutcome Outcome
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }

    public DiagnosticDisposition? DiagnosticDisposition
    {
        get;
    }

    public string? AuthorizationId
    {
        get;
    }
}
