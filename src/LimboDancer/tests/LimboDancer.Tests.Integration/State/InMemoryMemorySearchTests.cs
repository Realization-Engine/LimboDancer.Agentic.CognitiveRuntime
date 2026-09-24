using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Memory;
using LimboDancer.Infrastructure.Vector;

namespace LimboDancer.Tests.Integration.State;

public sealed class InMemoryMemorySearchTests
{
    [Fact]
    public async Task SearchAlwaysUsesTenantScope()
    {
        var search = new InMemoryMemorySearch();
        var firstTenant = new TenantScope(Guid.NewGuid());
        var secondTenant = new TenantScope(Guid.NewGuid());
        await search.UpsertAsync(firstTenant, new MemoryDocument("first", "shared searchable text"));
        await search.UpsertAsync(secondTenant, new MemoryDocument("second", "shared searchable text"));

        var result = Assert.Single(await search.SearchAsync(firstTenant, new MemorySearchQuery("searchable")));

        Assert.Equal("first", result.Id);
    }

    [Fact]
    public void AzureSearchFilterContainsMandatoryTenantClause()
    {
        var tenant = new TenantScope(Guid.Parse("2ee987f4-7e92-4f4d-94fb-63c7ab0755ab"));

        var filter = AzureSearchTenantFilter.Build(tenant);

        Assert.Equal("tenantId eq '2ee987f4-7e92-4f4d-94fb-63c7ab0755ab'", filter);
    }

    [Fact]
    public async Task VectorOnlySearchIsSupported()
    {
        var search = new InMemoryMemorySearch();
        var tenant = new TenantScope(Guid.NewGuid());
        await search.UpsertAsync(tenant, new MemoryDocument("matching", "content", vector: [1, 0]));
        await search.UpsertAsync(tenant, new MemoryDocument("opposite", "content", vector: [0, 1]));

        var result = Assert.Single(await search.SearchAsync(
            tenant,
            new MemorySearchQuery(null, queryVector: [1, 0])));

        Assert.Equal("matching", result.Id);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var search = new InMemoryMemorySearch();
        var cancellation = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => search.SearchAsync(
                new TenantScope(Guid.NewGuid()),
                new MemorySearchQuery("query"),
                cancellation).AsTask());
    }
}
