using LimboDancer.Abstractions.Actions;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Tests.Unit.Actions;

public sealed class ActionBindingRegistryTests
{
    [Fact]
    public void ProtocolBindingResolves()
    {
        var binding = new ActionBinding("mcp", "history_get", WellKnownActions.HistoryRead);
        var registry = new ActionBindingRegistry([binding]);

        var found = registry.TryResolve("mcp", "history_get", out var resolved);

        Assert.True(found);
        Assert.Same(binding, resolved);
    }

    [Fact]
    public void UnknownProtocolBindingFailsClosed()
    {
        var registry = new ActionBindingRegistry(
            [new ActionBinding("mcp", "history_get", WellKnownActions.HistoryRead)]);

        var found = registry.TryResolve("mcp", "unknown", out var resolved);

        Assert.False(found);
        Assert.Null(resolved);
    }

    [Fact]
    public void BindingCannotCarryAuthoritativeDescriptorMetadata()
    {
        var propertyNames = typeof(ActionBinding)
            .GetProperties()
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(propertyNames.SetEquals(
        [
            nameof(ActionBinding.Protocol),
            nameof(ActionBinding.ExternalName),
            nameof(ActionBinding.ActionId),
            nameof(ActionBinding.Version),
        ]));
    }

    [Fact]
    public void BuiltInMcpBindingsResolveToBuiltInDescriptors()
    {
        var actionRegistry = new ActionRegistry(BuiltInActionCatalog.CreateDescriptors());
        var bindingRegistry = new ActionBindingRegistry(BuiltInActionCatalog.CreateMcpBindings());

        foreach (var externalName in new[] { "history_get", "history_append", "graph_query", "memory_search" })
        {
            Assert.True(bindingRegistry.TryResolve(WellKnownActions.McpProtocol, externalName, out var binding));
            Assert.NotNull(binding);
            Assert.True(actionRegistry.TryGet(binding.ActionId, binding.Version, out var descriptor));
            Assert.NotNull(descriptor);
        }
    }

    [Fact]
    public void HistoryAppendSchemaDoesNotGrantCallerAuthoredEffectsOrPreconditions()
    {
        var descriptor = Assert.Single(
            BuiltInActionCatalog.CreateDescriptors(),
            static candidate => candidate.Id == WellKnownActions.HistoryAppend);

        var properties = descriptor.InputSchema.GetProperty("properties");

        Assert.False(properties.TryGetProperty("preconditions", out _));
        Assert.False(properties.TryGetProperty("effects", out _));
    }
}
