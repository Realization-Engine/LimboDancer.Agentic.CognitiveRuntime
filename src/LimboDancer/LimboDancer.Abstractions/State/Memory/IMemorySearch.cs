namespace LimboDancer.Abstractions.State.Memory;

public interface IMemorySearch
{
    public ValueTask<IReadOnlyList<MemorySearchItem>> SearchAsync(
        TenantScope tenant,
        MemorySearchQuery query,
        CancellationToken cancellationToken = default);
}
