using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Graph;
using LimboDancer.Abstractions.State.Ontology;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Actions.Graph;

public sealed class GraphQueryExecutor : IActionExecutor
{
    private readonly IGraphQueryReader graphReader;
    private readonly IOntologyResolver ontologyResolver;

    public GraphQueryExecutor(IGraphQueryReader graphReader, IOntologyResolver ontologyResolver)
    {
        this.graphReader = graphReader ?? throw new ArgumentNullException(nameof(graphReader));
        this.ontologyResolver = ontologyResolver ?? throw new ArgumentNullException(nameof(ontologyResolver));
    }

    public ActionId ActionId => WellKnownActions.GraphQuery;

    public ExecutorBinding Binding => WellKnownActions.GraphQueryExecutor;

    public async Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ActionExecutorJson.IsExpectedAction(action, ActionId))
        {
            return Failed("graph.query.action_mismatch");
        }

        var arguments = action.Selected.Candidate.Arguments;
        if (arguments.ValueKind != JsonValueKind.Object
            || !ActionExecutorJson.TryGetStringArray(arguments, "subjectIds", out var subjectIds)
            || !ActionExecutorJson.TryGetOptionalString(arguments, "keyMode", out var keyMode)
            || !ActionExecutorJson.TryGetOptionalInt32(arguments, "limit", 50, 1, 500, out var limit)
            || !ActionExecutorJson.TryGetOptionalString(arguments, "cursor", out var cursor)
            || !string.IsNullOrWhiteSpace(cursor))
        {
            return Failed("graph.query.invalid_arguments");
        }

        var useOntology = keyMode is null || string.Equals(keyMode, "ontology", StringComparison.Ordinal);
        if (!useOntology && !string.Equals(keyMode, "graph", StringComparison.Ordinal))
        {
            return Failed("graph.query.invalid_arguments");
        }

        try
        {
            var tenant = new TenantScope(action.TenantId);
            var filters = new List<GraphPropertyFilter>();
            var filterOutcome = await ParseFiltersAsync(
                    arguments,
                    tenant,
                    useOntology,
                    filters,
                    cancellationToken)
                .ConfigureAwait(false);
            if (filterOutcome != ParseOutcome.Valid)
            {
                return Failed(CodeFor(filterOutcome));
            }

            var traversal = new List<GraphTraversalStep>();
            var traversalOutcome = await ParseTraversalAsync(
                    arguments,
                    tenant,
                    useOntology,
                    traversal,
                    cancellationToken)
                .ConfigureAwait(false);
            if (traversalOutcome != ParseOutcome.Valid)
            {
                return Failed(CodeFor(traversalOutcome));
            }

            var result = await graphReader
                .QueryAsync(tenant, new GraphQuery(subjectIds, filters, traversal, limit), cancellationToken)
                .ConfigureAwait(false);
            var output = ActionExecutorJson.Serialize(new
            {
                vertices = result.Vertices.Select(static vertex => new
                {
                    id = vertex.Id,
                    label = vertex.Label,
                    properties = vertex.Properties,
                }),
                nextCursor = (string?)null,
            });
            return new ActionExecutionResult(succeeded: true, "graph.query.succeeded", output);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed("graph.query.failed");
        }
    }

    private async ValueTask<ParseOutcome> ParseFiltersAsync(
        JsonElement arguments,
        TenantScope tenant,
        bool useOntology,
        ICollection<GraphPropertyFilter> destination,
        CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("filters", out var filters)
            || filters.ValueKind == JsonValueKind.Null)
        {
            return ParseOutcome.Valid;
        }

        if (filters.ValueKind != JsonValueKind.Array)
        {
            return ParseOutcome.Invalid;
        }

        foreach (var filter in filters.EnumerateArray())
        {
            if (filter.ValueKind != JsonValueKind.Object
                || !ActionExecutorJson.TryGetRequiredString(filter, "property", out var property)
                || !ActionExecutorJson.TryGetOptionalString(filter, "op", out var operation)
                || !TryParseOperator(operation, out var parsedOperator))
            {
                return ParseOutcome.Invalid;
            }

            var storageName = property;
            if (useOntology)
            {
                var mapping = await ontologyResolver
                    .ResolveAsync(tenant, OntologyTermKind.Property, property, cancellationToken)
                    .ConfigureAwait(false);
                if (mapping is null)
                {
                    return ParseOutcome.MappingUnresolved;
                }

                storageName = mapping.StorageName;
            }

            object? value = null;
            if (filter.TryGetProperty("value", out var valueElement))
            {
                value = ActionExecutorJson.ToScalar(valueElement);
            }

            if (parsedOperator is GraphComparisonOperator.Equal or GraphComparisonOperator.NotEqual
                && !filter.TryGetProperty("value", out _))
            {
                return ParseOutcome.Invalid;
            }

            destination.Add(new GraphPropertyFilter(storageName, parsedOperator, value));
        }

        return ParseOutcome.Valid;
    }

    private async ValueTask<ParseOutcome> ParseTraversalAsync(
        JsonElement arguments,
        TenantScope tenant,
        bool useOntology,
        ICollection<GraphTraversalStep> destination,
        CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("traverse", out var traversal)
            || traversal.ValueKind == JsonValueKind.Null)
        {
            return ParseOutcome.Valid;
        }

        if (traversal.ValueKind != JsonValueKind.Array)
        {
            return ParseOutcome.Invalid;
        }

        foreach (var step in traversal.EnumerateArray())
        {
            if (step.ValueKind != JsonValueKind.Object
                || !ActionExecutorJson.TryGetRequiredString(step, "relation", out var relation)
                || !ActionExecutorJson.TryGetOptionalString(step, "direction", out var direction)
                || !TryParseDirection(direction, out var parsedDirection)
                || !ActionExecutorJson.TryGetOptionalInt32(step, "hops", 1, 1, 16, out var hops))
            {
                return ParseOutcome.Invalid;
            }

            var storageName = relation;
            if (useOntology)
            {
                var mapping = await ontologyResolver
                    .ResolveAsync(tenant, OntologyTermKind.Relation, relation, cancellationToken)
                    .ConfigureAwait(false);
                if (mapping is null)
                {
                    return ParseOutcome.MappingUnresolved;
                }

                storageName = mapping.StorageName;
            }

            destination.Add(new GraphTraversalStep(parsedDirection, storageName, hops));
        }

        return ParseOutcome.Valid;
    }

    private static bool TryParseOperator(string? value, out GraphComparisonOperator result)
    {
        switch (value)
        {
            case null:
            case "eq":
                result = GraphComparisonOperator.Equal;
                return true;
            case "neq":
                result = GraphComparisonOperator.NotEqual;
                return true;
            case "exists":
                result = GraphComparisonOperator.Exists;
                return true;
            case "not_exists":
                result = GraphComparisonOperator.NotExists;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static bool TryParseDirection(string? value, out GraphTraversalDirection result)
    {
        switch (value)
        {
            case null:
            case "out":
                result = GraphTraversalDirection.Out;
                return true;
            case "in":
                result = GraphTraversalDirection.In;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static string CodeFor(ParseOutcome outcome) => outcome == ParseOutcome.MappingUnresolved
        ? "graph.query.semantic_mapping_unresolved"
        : "graph.query.invalid_arguments";

    private static ActionExecutionResult Failed(string code) => new(succeeded: false, code);

    private enum ParseOutcome
    {
        Valid,
        Invalid,
        MappingUnresolved,
    }
}
