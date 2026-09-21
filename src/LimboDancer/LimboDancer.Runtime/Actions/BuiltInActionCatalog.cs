using System.Text.Json;
using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public static class BuiltInActionCatalog
{
    public static IReadOnlyList<ActionDescriptor> CreateDescriptors() =>
    [
        CreateHistoryRead(),
        CreateHistoryAppend(),
        CreateGraphQuery(),
        CreateMemorySearch(),
    ];

    public static IReadOnlyList<ActionBinding> CreateMcpBindings() =>
    [
        new(WellKnownActions.McpProtocol, "history_get", WellKnownActions.HistoryRead, WellKnownActions.InitialVersion),
        new(WellKnownActions.McpProtocol, "history_append", WellKnownActions.HistoryAppend, WellKnownActions.InitialVersion),
        new(WellKnownActions.McpProtocol, "graph_query", WellKnownActions.GraphQuery, WellKnownActions.InitialVersion),
        new(WellKnownActions.McpProtocol, "memory_search", WellKnownActions.MemorySearch, WellKnownActions.InitialVersion),
    ];

    private static ActionDescriptor CreateHistoryRead() => new(
        WellKnownActions.HistoryRead,
        WellKnownActions.InitialVersion,
        "Read session history",
        "Returns recent messages for a session.",
        ParseSchema(
            """
            {
              "type": "object",
              "required": ["sessionId"],
              "properties": {
                "sessionId": { "type": "string" },
                "limit": { "type": "integer", "minimum": 1, "maximum": 200, "default": 50 },
                "before": { "type": "string", "format": "date-time" }
              }
            }
            """),
        ParseSchema(
            """
            {
              "type": "object",
              "required": ["sessionId", "messages"],
              "properties": {
                "sessionId": { "type": "string" },
                "messages": { "type": "array" }
              }
            }
            """),
        ReadOnlyRisk(),
        requiredPermissions: [],
        preconditions: [],
        expectedEffects: [],
        IdempotencyMode.Intrinsic,
        new ExecutorBinding("runtime:executor/HistoryRead"));

    private static ActionDescriptor CreateHistoryAppend() => new(
        WellKnownActions.HistoryAppend,
        WellKnownActions.InitialVersion,
        "Append session history",
        "Appends a message after runtime-owned preconditions authorize the write.",
        ParseSchema(
            """
            {
              "type": "object",
              "required": ["sessionId", "sender", "text", "subjectVertexId"],
              "properties": {
                "sessionId": { "type": "string" },
                "sender": { "type": "string" },
                "text": { "type": "string" },
                "timestamp": { "type": "string", "format": "date-time" },
                "metadata": { "type": "object", "additionalProperties": true },
                "subjectVertexId": { "type": "string" }
              }
            }
            """),
        ParseSchema(
            """
            {
              "type": "object",
              "required": ["messageId", "sessionId", "timestamp"],
              "properties": {
                "messageId": { "type": "string" },
                "sessionId": { "type": "string" },
                "timestamp": { "type": "string", "format": "date-time" }
              }
            }
            """),
        new ActionRiskProfile(
            ActionMutability.Write,
            ActionIdempotency.NonIdempotent,
            ActionReversibility.Compensatable,
            ActionBoundary.Internal,
            ActionPrivilege.Normal),
        requiredPermissions: [],
        preconditions: [],
        expectedEffects: [],
        IdempotencyMode.KeyRequired,
        new ExecutorBinding("runtime:executor/HistoryAppend"));

    private static ActionDescriptor CreateGraphQuery() => new(
        WellKnownActions.GraphQuery,
        WellKnownActions.InitialVersion,
        "Query the knowledge graph",
        "Filters and traverses vertices using ontology predicates.",
        ParseSchema(
            """
            {
              "type": "object",
              "properties": {
                "subjectIds": { "type": "array", "items": { "type": "string" } },
                "keyMode": { "type": "string", "enum": ["ontology", "graph"], "default": "ontology" },
                "filters": { "type": "array" },
                "traverse": { "type": "array" },
                "limit": { "type": "integer", "minimum": 1, "maximum": 500, "default": 50 },
                "cursor": { "type": "string" }
              }
            }
            """),
        ParseSchema(
            """
            {
              "type": "object",
              "required": ["vertices"],
              "properties": {
                "vertices": { "type": "array" },
                "nextCursor": { "type": ["string", "null"] }
              }
            }
            """),
        ReadOnlyRisk(),
        requiredPermissions: [],
        preconditions: [],
        expectedEffects: [],
        IdempotencyMode.Intrinsic,
        new ExecutorBinding("runtime:executor/GraphQuery"));

    private static ActionDescriptor CreateMemorySearch() => new(
        WellKnownActions.MemorySearch,
        WellKnownActions.InitialVersion,
        "Search memory",
        "Performs tenant-scoped hybrid search over the memory index.",
        ParseSchema(
            """
            {
              "type": "object",
              "properties": {
                "queryText": { "type": "string" },
                "vectorBase64": { "type": "string" },
                "k": { "type": "integer", "minimum": 1, "maximum": 100, "default": 8 },
                "ontologyClass": { "type": "string" },
                "uriEquals": { "type": "string" },
                "tagsAny": { "type": "array", "items": { "type": "string" } }
              }
            }
            """),
        ParseSchema(
            """
            {
              "type": "object",
              "required": ["tenantId", "count", "items"],
              "properties": {
                "tenantId": { "type": "string" },
                "count": { "type": "integer" },
                "items": { "type": "array" }
              }
            }
            """),
        ReadOnlyRisk(),
        requiredPermissions: [],
        preconditions: [],
        expectedEffects: [],
        IdempotencyMode.Intrinsic,
        new ExecutorBinding("runtime:executor/MemorySearch"));

    private static ActionRiskProfile ReadOnlyRisk() => new(
        ActionMutability.ReadOnly,
        ActionIdempotency.Idempotent,
        ActionReversibility.Reversible,
        ActionBoundary.Internal,
        ActionPrivilege.Normal);

    private static JsonElement ParseSchema(string schema)
    {
        using var document = JsonDocument.Parse(schema);
        return document.RootElement.Clone();
    }
}
