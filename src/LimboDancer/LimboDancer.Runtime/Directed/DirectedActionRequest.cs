using System.Text.Json;
using LimboDancer.Abstractions.Runtime;

namespace LimboDancer.Runtime.Directed;

public sealed class DirectedActionRequest
{
    public DirectedActionRequest(
        RuntimeInvocationId invocationId,
        CorrelationId correlationId,
        Guid tenantId,
        string protocol,
        string externalActionName,
        JsonElement arguments)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invocationId.Value, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId.Value);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalActionName);

        InvocationId = invocationId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        Protocol = protocol;
        ExternalActionName = externalActionName;
        Arguments = arguments.Clone();
    }

    public RuntimeInvocationId InvocationId { get; }

    public CorrelationId CorrelationId { get; }

    public Guid TenantId { get; }

    public string Protocol { get; }

    public string ExternalActionName { get; }

    public JsonElement Arguments { get; }
}
