using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Abstractions.Execution;

public sealed class ExecutionGateResult
{
    public ExecutionGateResult(
        ExecutionGateOutcome outcome,
        AuthorizedAction? authorizedAction,
        IEnumerable<string>? reasonCodes = null,
        DiagnosticDisposition? diagnosticDisposition = null)
    {
        if ((outcome == ExecutionGateOutcome.Authorized) != (authorizedAction is not null))
        {
            throw new ArgumentException(
                "Only an authorized outcome may contain an authorized action.",
                nameof(authorizedAction));
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
            throw new ArgumentException("Reason codes cannot be empty.", nameof(reasonCodes));
        }

        Outcome = outcome;
        AuthorizedAction = authorizedAction;
        ReasonCodes = new ReadOnlyCollection<string>(reasons);
        DiagnosticDisposition = diagnosticDisposition;
    }

    public ExecutionGateOutcome Outcome
    {
        get;
    }

    public AuthorizedAction? AuthorizedAction
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
}
