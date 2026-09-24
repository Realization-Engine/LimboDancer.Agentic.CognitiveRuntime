using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Graph;
using LimboDancer.Abstractions.State.History;
using LimboDancer.Adapters.Mcp;
using LimboDancer.Infrastructure.Audit;
using LimboDancer.Infrastructure.Graph;
using LimboDancer.Infrastructure.Ontology;
using LimboDancer.Infrastructure.Relational;
using LimboDancer.Infrastructure.Vector;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Actions.Graph;
using LimboDancer.Runtime.Actions.History;
using LimboDancer.Runtime.Actions.Memory;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Directed;
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Tests.Integration.Mcp;

public sealed class McpInteractionAdapterTests
{
    private static readonly string[] ExpectedToolNames =
    [
        "graph_query",
        "history_append",
        "history_get",
        "memory_search",
    ];

    [Fact]
    public void DiscoveryAndLegacyInitializationAdvertiseSupportedLifecycleModels()
    {
        var fixture = CreateFixture();

        var discovery = fixture.Adapter.Discover();
        var initialized = fixture.Adapter.InitializeLegacy(
            new McpLegacyInitializeRequest(McpProtocolVersions.Modern, "client", "1.0"));

        Assert.Contains(McpProtocolVersions.Modern, discovery.ProtocolVersions);
        Assert.Contains(McpProtocolVersions.LatestLegacy, discovery.ProtocolVersions);
        Assert.True(discovery.SupportsTools);
        Assert.Equal(McpProtocolVersions.LatestLegacy, initialized.ProtocolVersion);
        Assert.True(initialized.SupportsTools);
    }

    [Fact]
    public void ToolListingPublishesAllFourBoundSchemas()
    {
        var tools = CreateFixture().Adapter.ListTools();

        Assert.Equal(
            ExpectedToolNames,
            tools.Select(static tool => tool.Name));
        Assert.All(tools, static tool => Assert.Equal(JsonValueKind.Object, tool.InputSchema.ValueKind));
    }

    [Fact]
    public async Task KnownToolExecutesThroughRuntimeGateAndAudit()
    {
        var tenant = new TenantScope(Guid.NewGuid());
        var fixture = CreateFixture();
        await fixture.History.AppendAsync(
            tenant,
            new HistoryAppendRequest("session", "user", "hello", DateTimeOffset.UtcNow));
        var request = CreateRequest("history_get", """{"sessionId":"session"}""");
        var caller = new McpCallerContext(
            tenant.TenantId,
            new RuntimePrincipal("caller", tenant.TenantId, isAuthenticated: true));

        var result = await fixture.Adapter.CallToolAsync(request, caller);

        Assert.False(result.IsError);
        var content = Assert.IsType<JsonElement>(result.Content);
        Assert.Equal("hello", content.GetProperty("messages")[0].GetProperty("text").GetString());
        Assert.Contains(
            fixture.Audit.Snapshot(),
            static auditEvent => auditEvent.EventType == LimboDancer.Abstractions.Audit.AuditEventType.GateAuthorized);
        Assert.Contains(
            fixture.Audit.Snapshot(),
            static auditEvent => auditEvent.EventType == LimboDancer.Abstractions.Audit.AuditEventType.ExecutorCompleted);
    }

    [Fact]
    public async Task AllFourCompatibilityToolsInvokeThroughAdapter()
    {
        var tenant = new TenantScope(Guid.NewGuid());
        var fixture = CreateFixture();
        await fixture.Graph.UpsertVertexAsync(tenant, new GraphVertex("vertex", "entity"));
        await fixture.Memory.UpsertAsync(tenant, new MemoryDocument("memory", "searchable content"));
        var caller = new McpCallerContext(
            tenant.TenantId,
            new RuntimePrincipal("caller", tenant.TenantId, isAuthenticated: true));

        var append = await fixture.Adapter.CallToolAsync(
            CreateRequest(
                "history_append",
                """{"sessionId":"session","sender":"user","text":"hello","subjectVertexId":"subject"}"""),
            caller);
        var read = await fixture.Adapter.CallToolAsync(
            CreateRequest("history_get", """{"sessionId":"session"}"""),
            caller);
        var graph = await fixture.Adapter.CallToolAsync(
            CreateRequest("graph_query", """{"subjectIds":["vertex"],"keyMode":"graph"}"""),
            caller);
        var memory = await fixture.Adapter.CallToolAsync(
            CreateRequest("memory_search", """{"queryText":"searchable"}"""),
            caller);

        Assert.All(new[] { append, read, graph, memory }, static result => Assert.False(result.IsError));
    }

    [Fact]
    public async Task UnknownToolAndInvalidArgumentsMapToProtocolSafeErrors()
    {
        var tenantId = Guid.NewGuid();
        var fixture = CreateFixture();
        var caller = new McpCallerContext(
            tenantId,
            new RuntimePrincipal("caller", tenantId, isAuthenticated: true));

        var unknown = await fixture.Adapter.CallToolAsync(CreateRequest("unknown", "{}"), caller);
        var invalid = await fixture.Adapter.CallToolAsync(CreateRequest("history_get", "{}"), caller);

        Assert.Equal(-32601, Assert.IsType<McpProtocolError>(unknown.Error).Code);
        var invalidError = Assert.IsType<McpProtocolError>(invalid.Error);
        Assert.Equal(-32602, invalidError.Code);
        Assert.Equal("schema.invalid", invalidError.RuntimeCode);
    }

    [Fact]
    public async Task TrustedTenantAndPrincipalFlowUnchangedIntoGate()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var fixture = CreateFixture();
        var caller = new McpCallerContext(
            tenantId,
            new RuntimePrincipal("wrong-tenant-principal", otherTenantId, isAuthenticated: true));

        var result = await fixture.Adapter.CallToolAsync(
            CreateRequest("history_get", """{"sessionId":"session"}"""),
            caller);

        var error = Assert.IsType<McpProtocolError>(result.Error);
        Assert.Equal(-32001, error.Code);
        Assert.Equal("tenant.invalid", error.RuntimeCode);
    }

    [Fact]
    public async Task AuthorizationCannotEnterThroughRequestPayload()
    {
        Assert.DoesNotContain(
            typeof(McpToolCallRequest).GetProperties(),
            static property => property.PropertyType == typeof(AuthorizedAction));
        var tenantId = Guid.NewGuid();
        var fixture = CreateFixture();
        var caller = new McpCallerContext(
            tenantId,
            new RuntimePrincipal("caller", tenantId, isAuthenticated: true));

        var result = await fixture.Adapter.CallToolAsync(
            CreateRequest(
                "history_get",
                """{"sessionId":"session","authorizationId":"forged"}"""),
            caller);

        var error = Assert.IsType<McpProtocolError>(result.Error);
        Assert.Equal(-32602, error.Code);
        Assert.Equal("schema.invalid", error.RuntimeCode);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var tenantId = Guid.NewGuid();
        var fixture = CreateFixture();
        var caller = new McpCallerContext(
            tenantId,
            new RuntimePrincipal("caller", tenantId, isAuthenticated: true));
        var cancellation = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Adapter.CallToolAsync(
                CreateRequest("history_get", """{"sessionId":"session"}"""),
                caller,
                cancellation).AsTask());
    }

    private static AdapterFixture CreateFixture()
    {
        var descriptors = BuiltInActionCatalog.CreateDescriptors();
        var bindings = new ActionBindingRegistry(BuiltInActionCatalog.CreateMcpBindings());
        var registry = new ActionRegistry(descriptors);
        var history = new InMemoryHistoryStore();
        var graph = new InMemoryGraphStore();
        var ontology = new InMemoryOntologyResolver();
        var memory = new InMemoryMemorySearch();
        IActionExecutor[] executors =
        [
            new HistoryReadExecutor(history),
            new HistoryAppendExecutor(history),
            new GraphQueryExecutor(graph, ontology),
            new MemorySearchExecutor(memory),
        ];
        var resolver = new ActionExecutorResolver(executors);
        var audit = new InMemoryAuditSink();
        var gate = new ExecutionGate(
            registry,
            resolver,
            new SatisfiedConstraintEvaluator(),
            new DiagnosticPolicy(),
            new DefaultExecutionRiskPolicy(),
            audit);
        var runtime = new DirectedActionRuntime(bindings, registry, resolver, gate, audit);
        var adapter = new McpInteractionAdapter(
            runtime,
            bindings,
            registry,
            new McpServerIdentity("LimboDancer", "0.1.0"));
        return new AdapterFixture(adapter, history, graph, memory, audit);
    }

    private static McpToolCallRequest CreateRequest(string name, string arguments) => new(
        name,
        Parse(arguments),
        new McpRequestMetadata(McpProtocolVersions.Modern, "integration-client", "1.0"));

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed record AdapterFixture(
        McpInteractionAdapter Adapter,
        InMemoryHistoryStore History,
        InMemoryGraphStore Graph,
        InMemoryMemorySearch Memory,
        InMemoryAuditSink Audit);

    private sealed class SatisfiedConstraintEvaluator : IActionConstraintEvaluator
    {
        public Task<ConstraintEvaluationResult> EvaluateAsync(
            SelectedAction action,
            RuntimeExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Satisfied));
        }
    }
}
