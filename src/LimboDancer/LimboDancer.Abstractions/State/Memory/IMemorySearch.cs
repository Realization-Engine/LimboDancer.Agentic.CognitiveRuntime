namespace LimboDancer.Abstractions.State.Memory;

public interface IMemorySearch
{
    ValueTask<IReadOnlyList<MemorySearchItem>> SearchAsync(
        TenantScope tenant,
        MemorySearchQuery query,
        CancellationToken cancellationToken = default);
}
