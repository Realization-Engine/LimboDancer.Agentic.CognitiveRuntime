using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Directed;
using LimboDancer.Runtime.Execution;
using LimboDancer.Tests.Unit.Actions;

namespace LimboDancer.Tests.Unit.Directed;

public sealed class DirectedActionRuntimeTests
{
    [Fact]
    public async Task MissingExecutorPreventsExecutableResolution()
    {
        var runtime = CreateRuntime([]);

        var result = await runtime.ResolveAsync(CreateRequest());

        Assert.Equal(DirectedActionResolution.MissingExecutor, result.Resolution);
        Assert.False(result.IsExecutable);
        Assert.NotNull(result.Descriptor);
    }

    [Fact]
    public async Task MatchingRuntimeExecutorMakesDescriptorExecutable()
    {
        var executor = new TestRuntimeExecutor(
            WellKnownActions.HistoryRead,
            new ExecutorBinding("runtime:executor/HistoryRead"));
        var runtime = CreateRuntime([executor]);

        var result = await runtime.ResolveAsync(CreateRequest());

        Assert.Equal(DirectedActionResolution.Resolved, result.Resolution);
        Assert.True(result.IsExecutable);
        Assert.Equal(WellKnownActions.HistoryRead, result.Descriptor!.Id);
    }

    [Fact]
    public async Task ResolutionWritesAdmissionAndActionIdentityEvents()
    {
        var executor = new TestRuntimeExecutor(
            WellKnownActions.HistoryRead,
            new ExecutorBinding("runtime:executor/HistoryRead"));
        var auditSink = new RecordingAuditSink();
        var runtime = CreateRuntime([executor], auditSink);

        var result = await runtime.ResolveAsync(CreateRequest());

        Assert.Equal(DirectedActionResolution.Resolved, result.Resolution);
        Assert.Collection(
            auditSink.Events,
            static admitted => Assert.Equal(AuditEventType.InvocationAdmitted, admitted.EventType),
            resolved =>
            {
                Assert.Equal(AuditEventType.ActionResolved, resolved.EventType);
                Assert.Equal(result.Descriptor!.Id, resolved.ActionId);
                Assert.Equal(result.Descriptor.Version, resolved.ActionVersion);
            });
    }

    [Fact]
    public async Task ExecutorRegisteredForDifferentActionDoesNotMakeDescriptorExecutable()
    {
        var executor = new TestRuntimeExecutor(
            WellKnownActions.GraphQuery,
            new ExecutorBinding("runtime:executor/HistoryRead"));
        var runtime = CreateRuntime([executor]);

        var result = await runtime.ResolveAsync(CreateRequest());

        Assert.Equal(DirectedActionResolution.MissingExecutor, result.Resolution);
        Assert.False(result.IsExecutable);
    }

    [Fact]
    public void DirectedBoundaryContainsNoProtocolSdkTypes()
    {
        var contractTypes = new[]
        {
            typeof(IDirectedActionRuntime),
            typeof(DirectedActionRequest),
            typeof(DirectedActionResult),
            typeof(DirectedActionExecutionResult),
        };

        var exposedTypes = contractTypes
            .SelectMany(static type =>
                type.GetProperties().Select(static property => property.PropertyType)
                    .Concat(type.GetMethods().Select(static method => method.ReturnType))
                    .Concat(type.GetMethods().SelectMany(static method =>
                        method.GetParameters().Select(static parameter => parameter.ParameterType))))
            .SelectMany(Flatten)
            .Distinct()
            .ToArray();

        Assert.DoesNotContain(
            exposedTypes,
            static type => type.Namespace?.Contains("ModelContextProtocol", StringComparison.Ordinal) == true);
    }

    private static DirectedActionRuntime CreateRuntime(
        IEnumerable<IActionExecutor> executors,
        IAuditSink? auditSink = null)
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        return new DirectedActionRuntime(
            new ActionBindingRegistry(
                [new ActionBinding("mcp", "history_get", descriptor.Id, descriptor.Version)]),
            new ActionRegistry([descriptor]),
            new ActionExecutorResolver(executors),
            new StubExecutionGate(),
            auditSink ?? new RecordingAuditSink());
    }

    private static DirectedActionRequest CreateRequest() => new(
        RuntimeInvocationId.New(),
        new CorrelationId("test-correlation"),
        Guid.NewGuid(),
        "mcp",
        "history_get",
        ParseJson("{}"));

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Flatten(argument))
            {
                yield return nested;
            }
        }
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed record TestRuntimeExecutor(ActionId ActionId, ExecutorBinding Binding) : IActionExecutor
    {
        public Task<ActionExecutionResult> ExecuteAsync(
            AuthorizedAction action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ActionExecutionResult(succeeded: true, "test.success"));
        }
    }

    private sealed class StubExecutionGate : IExecutionGate
    {
        public Task<ExecutionGateResult> AuthorizeAsync(
            SelectedAction action,
            LimboDancer.Abstractions.Execution.ExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                new ExecutionGateResult(
                    ExecutionGateOutcome.Denied,
                    authorizedAction: null,
                    ["test.denied"]));
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        private readonly List<RuntimeAuditEvent> events = [];

        public IReadOnlyList<RuntimeAuditEvent> Events => events;

        public ValueTask WriteAsync(
            RuntimeAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);
            cancellationToken.ThrowIfCancellationRequested();
            events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
