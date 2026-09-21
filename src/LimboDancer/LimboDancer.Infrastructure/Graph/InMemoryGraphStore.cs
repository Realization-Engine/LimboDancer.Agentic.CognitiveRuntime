using System.Collections.Concurrent;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Graph;

namespace LimboDancer.Infrastructure.Graph;

public sealed class InMemoryGraphStore : IGraphQueryReader, IGraphStateWriter
{
    private readonly ConcurrentDictionary<(Guid TenantId, string VertexId), GraphVertex> vertices = new();
    private readonly ConcurrentDictionary<(Guid TenantId, string SourceId, string TargetId, string Label), byte> edges = new();

    public ValueTask UpsertVertexAsync(
        TenantScope tenant,
        GraphVertex vertex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();
        vertices[(tenant.TenantId, vertex.Id)] = vertex;
        return ValueTask.CompletedTask;
    }

    public ValueTask UpsertEdgeAsync(
        TenantScope tenant,
        GraphEdge edge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentException.ThrowIfNullOrWhiteSpace(edge.SourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edge.TargetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(edge.Label);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();

        if (!vertices.ContainsKey((tenant.TenantId, edge.SourceId))
            || !vertices.ContainsKey((tenant.TenantId, edge.TargetId)))
        {
            throw new InvalidOperationException("Both edge vertices must exist in the same tenant scope.");
        }

        edges[(tenant.TenantId, edge.SourceId, edge.TargetId, edge.Label)] = 0;
        return ValueTask.CompletedTask;
    }

    public ValueTask<GraphQueryResult> QueryAsync(
        TenantScope tenant,
        GraphQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<GraphVertex> current = query.SubjectIds.Count == 0
            ? vertices.Where(pair => pair.Key.TenantId == tenant.TenantId).Select(static pair => pair.Value)
            : query.SubjectIds
                .Select(id => vertices.TryGetValue((tenant.TenantId, id), out var vertex) ? vertex : null)
                .OfType<GraphVertex>();

        current = query.Filters.Aggregate(current, ApplyFilter);
        foreach (var step in query.Traversal)
        {
            for (var hop = 0; hop < step.Hops; hop++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentIds = current.Select(static vertex => vertex.Id).ToHashSet(StringComparer.Ordinal);
                var adjacentIds = edges.Keys
                    .Where(key => key.TenantId == tenant.TenantId
                        && string.Equals(key.Label, step.EdgeLabel, StringComparison.Ordinal)
                        && (step.Direction == GraphTraversalDirection.Out
                            ? currentIds.Contains(key.SourceId)
                            : currentIds.Contains(key.TargetId)))
                    .Select(key => step.Direction == GraphTraversalDirection.Out ? key.TargetId : key.SourceId)
                    .Distinct(StringComparer.Ordinal);
                current = adjacentIds
                    .Select(id => vertices.TryGetValue((tenant.TenantId, id), out var vertex) ? vertex : null)
                    .OfType<GraphVertex>();
            }
        }

        var result = current
            .OrderBy(static vertex => vertex.Id, StringComparer.Ordinal)
            .Take(query.Limit)
            .ToArray();
        return ValueTask.FromResult(new GraphQueryResult(Array.AsReadOnly(result)));
    }

    private static IEnumerable<GraphVertex> ApplyFilter(
        IEnumerable<GraphVertex> source,
        GraphPropertyFilter filter)
    {
        return source.Where(vertex => Matches(vertex, filter));
    }

    private static bool Matches(GraphVertex vertex, GraphPropertyFilter filter)
    {
        var exists = vertex.Properties.TryGetValue(filter.PropertyKey, out var value);
        return filter.Operator switch
        {
            GraphComparisonOperator.Equal => exists && Equals(value, filter.Value),
            GraphComparisonOperator.NotEqual => !exists || !Equals(value, filter.Value),
            GraphComparisonOperator.Exists => exists,
            GraphComparisonOperator.NotExists => !exists,
            _ => false,
        };
    }
}
