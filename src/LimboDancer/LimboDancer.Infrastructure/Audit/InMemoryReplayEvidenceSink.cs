using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Evidence;

namespace LimboDancer.Infrastructure.Audit;

public sealed class InMemoryReplayEvidenceSink : IReplayEvidenceSink
{
    private readonly ConcurrentQueue<RuntimeStepEvidence> records = new();

    public ValueTask WriteAsync(
        RuntimeStepEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        cancellationToken.ThrowIfCancellationRequested();
        records.Enqueue(evidence);
        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<RuntimeStepEvidence> Snapshot(Guid tenantId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        return new ReadOnlyCollection<RuntimeStepEvidence>(records
            .Where(item => item.Goal.TenantId == tenantId)
            .ToArray());
    }
}
