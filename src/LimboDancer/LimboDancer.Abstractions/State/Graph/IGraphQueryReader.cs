namespace LimboDancer.Abstractions.State.Graph;

public interface IGraphQueryReader
{
    ValueTask<GraphQueryResult> QueryAsync(
        TenantScope tenant,
        GraphQuery query,
        CancellationToken cancellationToken = default);
}
