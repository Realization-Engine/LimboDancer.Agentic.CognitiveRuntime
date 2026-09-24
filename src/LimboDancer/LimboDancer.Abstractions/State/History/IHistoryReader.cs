namespace LimboDancer.Abstractions.State.History;

public interface IHistoryReader
{
    public ValueTask<IReadOnlyList<HistoryEntry>> ListAsync(
        TenantScope tenant,
        string sessionId,
        int limit,
        DateTimeOffset? before = null,
        CancellationToken cancellationToken = default);
}
