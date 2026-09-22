using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.State.Graph;

public enum GraphComparisonOperator
{
    Equal,
    NotEqual,
    Exists,
    NotExists,
}

public enum GraphTraversalDirection
{
    Out,
    In,
}

public sealed record GraphPropertyFilter(
    string PropertyKey,
    GraphComparisonOperator Operator,
    object? Value = null);

public sealed record GraphTraversalStep(
    GraphTraversalDirection Direction,
    string EdgeLabel,
    int Hops = 1);

public sealed class GraphQuery
{
    public GraphQuery(
        IEnumerable<string>? subjectIds = null,
        IEnumerable<GraphPropertyFilter>? filters = null,
        IEnumerable<GraphTraversalStep>? traversal = null,
        int limit = 50)
    {
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 500.");
        }

        SubjectIds = Array.AsReadOnly((subjectIds ?? []).ToArray());
        Filters = Array.AsReadOnly((filters ?? []).ToArray());
        Traversal = Array.AsReadOnly((traversal ?? []).ToArray());
        Limit = limit;

        if (SubjectIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Subject identifiers cannot be empty.", nameof(subjectIds));
        }

        if (Filters.Any(static filter => string.IsNullOrWhiteSpace(filter.PropertyKey)))
        {
            throw new ArgumentException("Graph filter keys cannot be empty.", nameof(filters));
        }

        if (Traversal.Any(static step => string.IsNullOrWhiteSpace(step.EdgeLabel) || step.Hops < 1))
        {
            throw new ArgumentException("Graph traversal steps require an edge label and at least one hop.", nameof(traversal));
        }
    }

    public IReadOnlyList<string> SubjectIds
    {
        get;
    }

    public IReadOnlyList<GraphPropertyFilter> Filters
    {
        get;
    }

    public IReadOnlyList<GraphTraversalStep> Traversal
    {
        get;
    }

    public int Limit
    {
        get;
    }
}

public sealed class GraphVertex
{
    public GraphVertex(string id, string label, IReadOnlyDictionary<string, object?>? properties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        Id = id;
        Label = label;
        Properties = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(properties ?? new Dictionary<string, object?>(), StringComparer.Ordinal));
    }

    public string Id
    {
        get;
    }

    public string Label
    {
        get;
    }

    public IReadOnlyDictionary<string, object?> Properties
    {
        get;
    }
}

public sealed record GraphEdge(string SourceId, string TargetId, string Label);

public sealed record GraphQueryResult(IReadOnlyList<GraphVertex> Vertices);
