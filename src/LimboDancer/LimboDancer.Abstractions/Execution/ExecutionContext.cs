using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Abstractions.Execution;

public sealed class ExecutionContext
{
    public ExecutionContext(
        RuntimeInvocationId invocationId,
        CorrelationId correlationId,
        Guid tenantId,
        RuntimePrincipal principal,
        RuntimeBudget budget,
        IEnumerable<DiagnosticFinding>? diagnosticFindings = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId.Value);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(budget);

        var findings = (diagnosticFindings ?? []).ToArray();
        if (findings.Any(static finding => finding is null))
        {
            throw new ArgumentException("Diagnostic findings cannot be null.", nameof(diagnosticFindings));
        }

        InvocationId = invocationId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        Principal = principal;
        Budget = budget;
        DiagnosticFindings = new ReadOnlyCollection<DiagnosticFinding>(findings);
    }

    public RuntimeInvocationId InvocationId
    {
        get;
    }

    public CorrelationId CorrelationId
    {
        get;
    }

    public Guid TenantId
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

    public IReadOnlyList<DiagnosticFinding> DiagnosticFindings
    {
        get;
    }
}
