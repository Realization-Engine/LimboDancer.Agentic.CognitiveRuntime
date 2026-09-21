using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Runtime.Actions;

namespace LimboDancer.Tests.Unit.Actions;

public sealed class ActionRegistryTests
{
    [Fact]
    public void KnownActionIdResolves()
    {
        var descriptor = CreateDescriptor();
        var registry = new ActionRegistry([descriptor]);

        var found = registry.TryGet(descriptor.Id, descriptor.Version, out var resolved);

        Assert.True(found);
        Assert.Same(descriptor, resolved);
    }

    [Fact]
    public void UnknownActionIdFailsClosed()
    {
        var registry = new ActionRegistry([CreateDescriptor()]);

        var found = registry.TryGet(new ActionId("ldm:action/Unknown"), null, out var resolved);

        Assert.False(found);
        Assert.Null(resolved);
    }

    [Fact]
    public void DuplicateIdentityAndVersionIsRejected()
    {
        var descriptor = CreateDescriptor();
        var duplicate = CreateDescriptor();

        var exception = Assert.Throws<InvalidOperationException>(() => new ActionRegistry([descriptor, duplicate]));

        Assert.Contains(descriptor.Id.Value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionlessLookupFailsWhenMultipleVersionsExist()
    {
        var first = CreateDescriptor(version: "1");
        var second = CreateDescriptor(version: "2");
        var registry = new ActionRegistry([first, second]);

        var found = registry.TryGet(first.Id, null, out var resolved);

        Assert.False(found);
        Assert.Null(resolved);
    }

    [Fact]
    public void DescriptorIsImmutableAfterPublication()
    {
        var permissions = new[] { "history:read" };
        var preconditions = new[] { CreatePrecondition() };
        var effects = new[] { CreateEffect() };
        var descriptor = CreateDescriptor(permissions, preconditions, effects);
        var registry = new ActionRegistry([descriptor]);

        permissions[0] = "history:write";
        preconditions[0] = CreatePrecondition("replacement");
        effects[0] = CreateEffect("replacement");
        var resolved = Assert.Single(registry.List());

        Assert.Contains("history:read", resolved.RequiredPermissions);
        Assert.DoesNotContain("history:write", resolved.RequiredPermissions);
        Assert.Equal("precondition", Assert.Single(resolved.Preconditions).Id);
        Assert.Equal("effect", Assert.Single(resolved.ExpectedEffects).Id);
        Assert.Null(typeof(ActionDescriptor).GetProperty(nameof(ActionDescriptor.Version))!.SetMethod);
    }

    internal static ActionDescriptor CreateDescriptor(
        IEnumerable<string>? permissions = null,
        IEnumerable<PreconditionDescriptor>? preconditions = null,
        IEnumerable<EffectDescriptor>? effects = null,
        string version = "1") => new(
            WellKnownActions.HistoryRead,
            new ActionVersion(version),
            "History read",
            "Reads history.",
            ParseJson("""{"type":"object"}"""),
            ParseJson("""{"type":"object"}"""),
            new ActionRiskProfile(
                ActionMutability.ReadOnly,
                ActionIdempotency.Idempotent,
                ActionReversibility.Reversible,
                ActionBoundary.Internal,
                ActionPrivilege.Normal),
            permissions,
            preconditions,
            effects,
            IdempotencyMode.Intrinsic,
            new ExecutorBinding("runtime:executor/HistoryRead"));

    private static PreconditionDescriptor CreatePrecondition(string id = "precondition") => new(
        id,
        PreconditionKind.Semantic,
        "runtime:evaluator/Test",
        ParseJson("{}"),
        true);

    private static EffectDescriptor CreateEffect(string id = "effect") => new(
        id,
        "test",
        ParseJson("{}"),
        true);

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
