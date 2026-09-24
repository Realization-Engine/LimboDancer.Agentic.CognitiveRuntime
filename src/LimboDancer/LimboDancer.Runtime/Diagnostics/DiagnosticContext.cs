using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Runtime.Diagnostics;

public sealed class DiagnosticContext
{
    public DiagnosticContext(
        RuntimeInvocationId invocationId,
        CorrelationId correlationId,
        Guid tenantId,
        GoalLifecycleState phase,
        DiagnosticPosition position,
        ActionDescriptor? descriptor = null,
        IEnumerable<string>? requiredSemanticMappingIds = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId.Value);

        var mappingIds = (requiredSemanticMappingIds ?? []).ToArray();
        if (mappingIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Required semantic mapping identifiers cannot be empty.",
                nameof(requiredSemanticMappingIds));
        }

        InvocationId = invocationId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        Phase = phase;
        Position = position;
        Descriptor = descriptor;
        RequiredSemanticMappingIds = new ReadOnlyCollection<string>(mappingIds);
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

    public GoalLifecycleState Phase
    {
        get;
    }

    public DiagnosticPosition Position
    {
        get;
    }

    public ActionDescriptor? Descriptor
    {
        get;
    }

    public IReadOnlyList<string> RequiredSemanticMappingIds
    {
        get;
    }
}
