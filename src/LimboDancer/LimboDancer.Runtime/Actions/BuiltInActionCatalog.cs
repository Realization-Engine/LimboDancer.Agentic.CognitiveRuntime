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
              "additionalProperties": false,
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
              "additionalProperties": false,
              "required": ["sessionId", "messages"],
              "properties": {
                "sessionId": { "type": "string" },
                "messages": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["id", "sender", "text", "timestamp", "metadata"]
                  }
                }
              }
            }
            """),
        ReadOnlyRisk(),
        requiredPermissions: [],
        preconditions: [],
        expectedEffects: [],
        IdempotencyMode.Intrinsic,
        WellKnownActions.HistoryReadExecutor);

    private static ActionDescriptor CreateHistoryAppend() => new(
        WellKnownActions.HistoryAppend,
        WellKnownActions.InitialVersion,
        "Append session history",
        "Appends a message after runtime-owned preconditions authorize the write.",
        ParseSchema(
            """
            {
              "type": "object",
              "additionalProperties": false,
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
              "additionalProperties": false,
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
        WellKnownActions.HistoryAppendExecutor);

    private static ActionDescriptor CreateGraphQuery() => new(
        WellKnownActions.GraphQuery,
        WellKnownActions.InitialVersion,
        "Query the knowledge graph",
        "Filters and traverses vertices using ontology predicates.",
        ParseSchema(
            """
            {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "subjectIds": { "type": "array", "items": { "type": "string" } },
                "keyMode": { "type": "string", "enum": ["ontology", "graph"], "default": "ontology" },
                "filters": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "required": ["property"],
                    "properties": {
                      "property": { "type": "string" },
                      "op": { "type": "string", "enum": ["eq", "neq", "exists", "not_exists"], "default": "eq" },
                      "value": {}
                    }
                  }
                },
                "traverse": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "required": ["relation"],
                    "properties": {
                      "direction": { "type": "string", "enum": ["out", "in"], "default": "out" },
                      "relation": { "type": "string" },
                      "hops": { "type": "integer", "minimum": 1, "maximum": 16, "default": 1 }
                    }
                  }
                },
                "limit": { "type": "integer", "minimum": 1, "maximum": 500, "default": 50 },
                "cursor": { "type": "string" }
              }
            }
            """),
        ParseSchema(
            """
            {
              "type": "object",
              "additionalProperties": false,
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
        WellKnownActions.GraphQueryExecutor);

    private static ActionDescriptor CreateMemorySearch() => new(
        WellKnownActions.MemorySearch,
        WellKnownActions.InitialVersion,
        "Search memory",
        "Performs tenant-scoped hybrid search over the memory index.",
        ParseSchema(
            """
            {
              "type": "object",
              "additionalProperties": false,
              "anyOf": [
                { "required": ["queryText"] },
                { "required": ["vectorBase64"] }
              ],
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
              "additionalProperties": false,
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
        WellKnownActions.MemorySearchExecutor);

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
