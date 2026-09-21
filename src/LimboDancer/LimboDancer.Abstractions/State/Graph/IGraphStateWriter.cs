namespace LimboDancer.Abstractions.State.Graph;

public interface IGraphStateWriter
{
    ValueTask UpsertVertexAsync(
        TenantScope tenant,
        GraphVertex vertex,
        CancellationToken cancellationToken = default);

    ValueTask UpsertEdgeAsync(
        TenantScope tenant,
        GraphEdge edge,
        CancellationToken cancellationToken = default);
}
