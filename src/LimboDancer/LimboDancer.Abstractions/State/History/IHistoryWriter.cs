namespace LimboDancer.Abstractions.State.History;

public interface IHistoryWriter
{
    public ValueTask<HistoryEntry> AppendAsync(
        TenantScope tenant,
        HistoryAppendRequest request,
        CancellationToken cancellationToken = default);
}
