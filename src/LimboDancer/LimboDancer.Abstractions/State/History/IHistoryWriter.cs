namespace LimboDancer.Abstractions.State.History;

public interface IHistoryWriter
{
    ValueTask<HistoryEntry> AppendAsync(
        TenantScope tenant,
        HistoryAppendRequest request,
        CancellationToken cancellationToken = default);
}
