namespace LimboDancer.Abstractions.State.Graph;

public interface IGraphStateWriter
{
    public ValueTask UpsertVertexAsync(
        TenantScope tenant,
        GraphVertex vertex,
        CancellationToken cancellationToken = default);

    public ValueTask UpsertEdgeAsync(
        TenantScope tenant,
        GraphEdge edge,
        CancellationToken cancellationToken = default);
}
