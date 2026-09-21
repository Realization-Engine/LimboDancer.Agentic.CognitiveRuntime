using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Graph;
using LimboDancer.Infrastructure.Graph;

namespace LimboDancer.Tests.Integration.State;

public sealed class InMemoryGraphStoreTests
{
    [Fact]
    public async Task ReadsAndMutationsRemainInsideTenantBoundary()
    {
        var store = new InMemoryGraphStore();
        var firstTenant = new TenantScope(Guid.NewGuid());
        var secondTenant = new TenantScope(Guid.NewGuid());
        await store.UpsertVertexAsync(firstTenant, new GraphVertex("source", "node", new Dictionary<string, object?> { ["name"] = "first" }));
        await store.UpsertVertexAsync(secondTenant, new GraphVertex("source", "node", new Dictionary<string, object?> { ["name"] = "second" }));
        await store.UpsertVertexAsync(secondTenant, new GraphVertex("target", "node"));

        var firstResult = Assert.Single((await store.QueryAsync(firstTenant, new GraphQuery(["source"]))).Vertices);
        var secondResult = Assert.Single((await store.QueryAsync(secondTenant, new GraphQuery(["source"]))).Vertices);
        Assert.Equal("first", firstResult.Properties["name"]);
        Assert.Equal("second", secondResult.Properties["name"]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.UpsertEdgeAsync(firstTenant, new GraphEdge("source", "target", "RELATED_TO")).AsTask());

        await store.UpsertVertexAsync(firstTenant, new GraphVertex("target", "node"));
        await store.UpsertEdgeAsync(firstTenant, new GraphEdge("source", "target", "RELATED_TO"));
        var traversed = await store.QueryAsync(
            firstTenant,
            new GraphQuery(["source"], traversal: [new GraphTraversalStep(GraphTraversalDirection.Out, "RELATED_TO")]));
        Assert.Equal("target", Assert.Single(traversed.Vertices).Id);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var store = new InMemoryGraphStore();
        var cancellation = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.QueryAsync(new TenantScope(Guid.NewGuid()), new GraphQuery(), cancellation).AsTask());
    }
}
