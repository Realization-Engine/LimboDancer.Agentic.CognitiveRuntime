using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.History;
using LimboDancer.Infrastructure.Relational;

namespace LimboDancer.Tests.Integration.State;

public sealed class InMemoryHistoryStoreTests
{
    [Fact]
    public async Task ReadsCannotCrossTenantBoundary()
    {
        var store = new InMemoryHistoryStore();
        var firstTenant = new TenantScope(Guid.NewGuid());
        var secondTenant = new TenantScope(Guid.NewGuid());
        var timestamp = DateTimeOffset.UtcNow;

        await store.AppendAsync(firstTenant, new HistoryAppendRequest("session", "user", "first", timestamp));
        await store.AppendAsync(secondTenant, new HistoryAppendRequest("session", "user", "second", timestamp));

        var firstResult = Assert.Single(await store.ListAsync(firstTenant, "session", 10));
        var secondResult = Assert.Single(await store.ListAsync(secondTenant, "session", 10));
        Assert.Equal("first", firstResult.Text);
        Assert.Equal("second", secondResult.Text);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var store = new InMemoryHistoryStore();
        var cancellation = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.ListAsync(new TenantScope(Guid.NewGuid()), "session", 10, cancellationToken: cancellation).AsTask());
    }

    [Fact]
    public async Task DefaultTenantScopeFailsClosed()
    {
        var store = new InMemoryHistoryStore();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.ListAsync(default, "session", 10).AsTask());
    }
}
