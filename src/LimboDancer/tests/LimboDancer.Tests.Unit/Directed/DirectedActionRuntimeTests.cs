using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Directed;
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

    private static DirectedActionRuntime CreateRuntime(IEnumerable<IRuntimeActionExecutor> executors)
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        return new DirectedActionRuntime(
            new ActionBindingRegistry(
                [new ActionBinding("mcp", "history_get", descriptor.Id, descriptor.Version)]),
            new ActionRegistry([descriptor]),
            new ActionExecutorResolver(executors));
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

    private sealed record TestRuntimeExecutor(ActionId ActionId, ExecutorBinding Binding) : IRuntimeActionExecutor;
}
