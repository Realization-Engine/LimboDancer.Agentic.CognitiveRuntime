using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Graph;
using LimboDancer.Abstractions.State.History;
using LimboDancer.Abstractions.State.Ontology;
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
using LimboDancer.Runtime.Execution;
using RuntimeExecutionContext = LimboDancer.Abstractions.Execution.ExecutionContext;

namespace LimboDancer.Tests.Integration.Actions;

public sealed class DirectedActionExecutorsTests
{
    [Fact]
    public async Task HistoryReadExecutesThroughGateAndAudit()
    {
        var tenantId = Guid.NewGuid();
        var store = new InMemoryHistoryStore();
        await store.AppendAsync(
            new TenantScope(tenantId),
            new HistoryAppendRequest("session", "user", "hello", DateTimeOffset.UtcNow));
        var executor = new HistoryReadExecutor(store);
        var audit = new InMemoryAuditSink();
        var authorized = await AuthorizeAsync(executor, tenantId, Parse("""{"sessionId":"session"}"""), audit);

        var result = await new AuditedActionExecutor(executor, audit).ExecuteAsync(authorized);

        Assert.True(result.Succeeded);
        Assert.Equal("history.read.succeeded", result.Code);
        var output = Assert.IsType<JsonElement>(result.Output);
        Assert.Equal("hello", output.GetProperty("messages")[0].GetProperty("text").GetString());
        AssertExecutionAudited(audit, "history.read.succeeded");
    }

    [Fact]
    public async Task HistoryAppendUsesAuthorizedTenantAndRejectsCallerAuthority()
    {
        var tenantId = Guid.NewGuid();
        var store = new InMemoryHistoryStore();
        var executor = new HistoryAppendExecutor(store);
        var audit = new InMemoryAuditSink();
        var arguments = Parse(
            """
            {
              "sessionId": "session",
              "sender": "user",
              "text": "hello",
              "subjectVertexId": "subject"
            }
            """);
        var authorized = await AuthorizeAsync(executor, tenantId, arguments, audit);

        var result = await new AuditedActionExecutor(executor, audit).ExecuteAsync(authorized);

        Assert.True(result.Succeeded);
        Assert.Equal("history.append.succeeded", result.Code);
        var entry = Assert.Single(await store.ListAsync(new TenantScope(tenantId), "session", 10));
        Assert.Equal("hello", entry.Text);
        AssertExecutionAudited(audit, "history.append.succeeded");

        var rejected = await AuthorizeAsync(
            executor,
            tenantId,
            Parse(
                """
                {
                  "sessionId": "session",
                  "sender": "user",
                  "text": "unsafe",
                  "subjectVertexId": "subject",
                  "effects": []
                }
                """),
            new InMemoryAuditSink());
        var rejectedResult = await executor.ExecuteAsync(rejected);
        Assert.False(rejectedResult.Succeeded);
        Assert.Equal("history.append.caller_authority_rejected", rejectedResult.Code);
    }

    [Fact]
    public async Task GraphQueryResolvesOntologyBeforeTenantScopedRead()
    {
        var tenant = new TenantScope(Guid.NewGuid());
        var graph = new InMemoryGraphStore();
        await graph.UpsertVertexAsync(
            tenant,
            new GraphVertex("vertex", "entity", new Dictionary<string, object?> { ["status"] = "ready" }));
        var ontology = new InMemoryOntologyResolver();
        ontology.Register(
            tenant,
            new OntologyMapping(OntologyTermKind.Property, "ldm:status", "status"));
        var executor = new GraphQueryExecutor(graph, ontology);
        var audit = new InMemoryAuditSink();
        var arguments = Parse(
            """
            {
              "filters": [{"property":"ldm:status","op":"eq","value":"ready"}]
            }
            """);
        var authorized = await AuthorizeAsync(executor, tenant.TenantId, arguments, audit);

        var result = await new AuditedActionExecutor(executor, audit).ExecuteAsync(authorized);

        Assert.True(result.Succeeded);
        var output = Assert.IsType<JsonElement>(result.Output);
        Assert.Equal("vertex", output.GetProperty("vertices")[0].GetProperty("id").GetString());
        AssertExecutionAudited(audit, "graph.query.succeeded");

        var unresolved = await AuthorizeAsync(
            executor,
            tenant.TenantId,
            Parse("""{"filters":[{"property":"ldm:unknown","op":"exists"}]}"""),
            new InMemoryAuditSink());
        var unresolvedResult = await executor.ExecuteAsync(unresolved);
        Assert.False(unresolvedResult.Succeeded);
        Assert.Equal("graph.query.semantic_mapping_unresolved", unresolvedResult.Code);
    }

    [Fact]
    public async Task MemorySearchExecutesWithMandatoryTenantScope()
    {
        var tenant = new TenantScope(Guid.NewGuid());
        var otherTenant = new TenantScope(Guid.NewGuid());
        var search = new InMemoryMemorySearch();
        await search.UpsertAsync(tenant, new MemoryDocument("expected", "searchable memory", vector: [1, 0]));
        await search.UpsertAsync(otherTenant, new MemoryDocument("forbidden", "searchable memory", vector: [1, 0]));
        var executor = new MemorySearchExecutor(search);
        var audit = new InMemoryAuditSink();
        var vectorBytes = new byte[sizeof(float) * 2];
        Buffer.BlockCopy(new float[] { 1, 0 }, 0, vectorBytes, 0, vectorBytes.Length);
        var arguments = Parse(
            JsonSerializer.Serialize(new
            {
                queryText = "searchable",
                vectorBase64 = Convert.ToBase64String(vectorBytes),
            }));
        var authorized = await AuthorizeAsync(executor, tenant.TenantId, arguments, audit);

        var result = await new AuditedActionExecutor(executor, audit).ExecuteAsync(authorized);

        Assert.True(result.Succeeded);
        var output = Assert.IsType<JsonElement>(result.Output);
        Assert.Equal(tenant.ToString(), output.GetProperty("tenantId").GetString());
        Assert.Equal("expected", output.GetProperty("items")[0].GetProperty("id").GetString());
        AssertExecutionAudited(audit, "memory.search.succeeded");
    }

    [Fact]
    public void CatalogBindingsMatchConcreteExecutors()
    {
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
        var descriptors = BuiltInActionCatalog.CreateDescriptors().ToDictionary(static descriptor => descriptor.Id);

        Assert.All(executors, executor => Assert.Equal(descriptors[executor.ActionId].Executor, executor.Binding));
        Assert.All(
            descriptors.Values,
            static descriptor => Assert.False(descriptor.InputSchema.GetProperty("additionalProperties").GetBoolean()));
        var historyAppendProperties = descriptors[WellKnownActions.HistoryAppend]
            .InputSchema
            .GetProperty("properties");
        Assert.False(historyAppendProperties.TryGetProperty("preconditions", out _));
        Assert.False(historyAppendProperties.TryGetProperty("effects", out _));
        Assert.True(descriptors[WellKnownActions.MemorySearch].InputSchema.TryGetProperty("anyOf", out _));
    }

    [Fact]
    public async Task CancellationPropagatesFromEveryExecutor()
    {
        var tenantId = Guid.NewGuid();
        var history = new InMemoryHistoryStore();
        var graph = new InMemoryGraphStore();
        var ontology = new InMemoryOntologyResolver();
        var memory = new InMemoryMemorySearch();
        var cases = new (IActionExecutor Executor, JsonElement Arguments)[]
        {
            (new HistoryReadExecutor(history), Parse("""{"sessionId":"session"}""")),
            (new HistoryAppendExecutor(history), Parse("""{"sessionId":"session","sender":"user","text":"text","subjectVertexId":"subject"}""")),
            (new GraphQueryExecutor(graph, ontology), Parse("{}")),
            (new MemorySearchExecutor(memory), Parse("""{"queryText":"query"}""")),
        };

        foreach (var (executor, arguments) in cases)
        {
            var authorized = await AuthorizeAsync(executor, tenantId, arguments, new InMemoryAuditSink());
            var cancellation = new CancellationToken(canceled: true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => executor.ExecuteAsync(authorized, cancellation));
        }
    }

    [Fact]
    public async Task InvalidPayloadsReturnStableFailureCodes()
    {
        var tenantId = Guid.NewGuid();
        var history = new InMemoryHistoryStore();
        var cases = new (IActionExecutor Executor, JsonElement Arguments, string Code)[]
        {
            (new HistoryReadExecutor(history), Parse("{}"), "history.read.invalid_arguments"),
            (new HistoryAppendExecutor(history), Parse("{}"), "history.append.invalid_arguments"),
            (new GraphQueryExecutor(new InMemoryGraphStore(), new InMemoryOntologyResolver()), Parse("""{"keyMode":"bad"}"""), "graph.query.invalid_arguments"),
            (new MemorySearchExecutor(new InMemoryMemorySearch()), Parse("{}"), "memory.search.invalid_arguments"),
        };

        foreach (var (executor, arguments, code) in cases)
        {
            var authorized = await AuthorizeAsync(executor, tenantId, arguments, new InMemoryAuditSink());
            var result = await executor.ExecuteAsync(authorized);
            Assert.False(result.Succeeded);
            Assert.Equal(code, result.Code);
        }
    }

    private static async Task<AuthorizedAction> AuthorizeAsync(
        IActionExecutor executor,
        Guid tenantId,
        JsonElement arguments,
        InMemoryAuditSink auditSink)
    {
        var descriptor = BuiltInActionCatalog.CreateDescriptors().Single(candidate => candidate.Id == executor.ActionId);
        var registry = new ActionRegistry([descriptor]);
        var gate = new ExecutionGate(
            registry,
            new ActionExecutorResolver([executor]),
            new SatisfiedConstraintEvaluator(),
            new DiagnosticPolicy(),
            new DefaultExecutionRiskPolicy(),
            auditSink);
        var selected = new SelectedAction(
            new ActionCandidate(Guid.NewGuid().ToString("N"), descriptor, arguments),
            SelectionOrigin.DirectedCaller);
        var context = new RuntimeExecutionContext(
            RuntimeInvocationId.New(),
            new CorrelationId(Guid.NewGuid().ToString("N")),
            tenantId,
            new RuntimePrincipal("integration-test", tenantId, isAuthenticated: true),
            new RuntimeBudget(
                maxSteps: 10,
                deadline: DateTimeOffset.UtcNow.AddMinutes(1),
                maxTokens: null,
                maxCost: null,
                maxExternalCalls: 10,
                maxRetries: 1));

        var gateResult = await gate.AuthorizeAsync(selected, context);
        Assert.Equal(ExecutionGateOutcome.Authorized, gateResult.Outcome);
        return Assert.IsType<AuthorizedAction>(gateResult.AuthorizedAction);
    }

    private static void AssertExecutionAudited(InMemoryAuditSink auditSink, string executionCode)
    {
        var completed = Assert.Single(
            auditSink.Snapshot(),
            auditEvent => auditEvent.EventType == AuditEventType.ExecutorCompleted);
        Assert.Equal(executionCode, completed.ExecutionCode);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

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
