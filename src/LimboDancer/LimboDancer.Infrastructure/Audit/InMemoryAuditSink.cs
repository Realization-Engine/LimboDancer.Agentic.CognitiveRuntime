using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Audit;

namespace LimboDancer.Infrastructure.Audit;

public sealed class InMemoryAuditSink : IAuditSink
{
    private readonly ConcurrentQueue<RuntimeAuditEvent> events = new();

    public ValueTask WriteAsync(
        RuntimeAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        cancellationToken.ThrowIfCancellationRequested();
        events.Enqueue(auditEvent);
        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<RuntimeAuditEvent> Snapshot() =>
        new ReadOnlyCollection<RuntimeAuditEvent>(events.ToArray());
}
