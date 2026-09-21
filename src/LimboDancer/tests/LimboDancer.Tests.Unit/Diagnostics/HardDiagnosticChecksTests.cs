using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Diagnostics;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Tests.Unit.Actions;

namespace LimboDancer.Tests.Unit.Diagnostics;

public sealed class HardDiagnosticChecksTests
{
    [Fact]
    public async Task MissingTenantFails()
    {
        var check = new TenantContextPresentCheck();

        var finding = await check.EvaluateAsync(CreateContext(tenantId: Guid.Empty));

        Assert.Equal(DiagnosticOutcome.Fail, finding.Outcome);
        Assert.Equal(DiagnosticSeverity.Critical, finding.Severity);
        Assert.Equal("tenant.missing", finding.Code);
    }

    [Fact]
    public async Task RegisteredAuthoritativeDescriptorPasses()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var check = new ActionDescriptorCurrentCheck(new ActionRegistry([descriptor]));

        var finding = await check.EvaluateAsync(CreateContext(descriptor: descriptor));

        Assert.Equal(DiagnosticOutcome.Pass, finding.Outcome);
        Assert.Equal("descriptor.current", finding.Code);
    }

    [Fact]
    public async Task UnregisteredDescriptorInstanceFails()
    {
        var registered = ActionRegistryTests.CreateDescriptor();
        var untrustedCopy = ActionRegistryTests.CreateDescriptor();
        var check = new ActionDescriptorCurrentCheck(new ActionRegistry([registered]));

        var finding = await check.EvaluateAsync(CreateContext(descriptor: untrustedCopy));

        Assert.Equal(DiagnosticOutcome.Fail, finding.Outcome);
        Assert.Equal("descriptor.not_current", finding.Code);
    }

    [Fact]
    public async Task MatchingExecutorBindingPasses()
    {
        var descriptor = ActionRegistryTests.CreateDescriptor();
        var executor = new TestRuntimeExecutor(descriptor.Id, descriptor.Executor);
        var check = new ExecutorBindingResolvableCheck(new ActionExecutorResolver([executor]));

        var finding = await check.EvaluateAsync(CreateContext(descriptor: descriptor));

        Assert.Equal(DiagnosticOutcome.Pass, finding.Outcome);
        Assert.Equal("executor.resolved", finding.Code);
    }

    [Fact]
    public async Task UnknownRequiredSemanticMappingFailsWithEvidence()
    {
        var resolver = new TestSemanticMappingResolver(["ldm:property/known"]);
        var check = new RequiredSemanticMappingsResolvableCheck(resolver);
        var context = CreateContext(requiredSemanticMappingIds:
        [
            "ldm:property/known",
            "ldm:property/unknown",
        ]);

        var finding = await check.EvaluateAsync(context);

        Assert.Equal(DiagnosticOutcome.Fail, finding.Outcome);
        Assert.Equal("semantic_mappings.unresolved", finding.Code);
        Assert.True(finding.Evidence.ContainsKey("unresolvedMappingIds"));
    }

    private static DiagnosticContext CreateContext(
        Guid? tenantId = null,
        ActionDescriptor? descriptor = null,
        IEnumerable<string>? requiredSemanticMappingIds = null) => new(
            RuntimeInvocationId.New(),
            new CorrelationId("hard-check-test"),
            tenantId ?? Guid.NewGuid(),
            GoalLifecycleState.Gating,
            DiagnosticPosition.PreFlight,
            descriptor,
            requiredSemanticMappingIds);

    private sealed record TestRuntimeExecutor(
        ActionId ActionId,
        ExecutorBinding Binding) : IRuntimeActionExecutor;

    private sealed class TestSemanticMappingResolver(IEnumerable<string> knownMappings)
        : IRequiredSemanticMappingResolver
    {
        private readonly HashSet<string> knownMappings = knownMappings.ToHashSet(StringComparer.Ordinal);

        public bool CanResolve(string mappingId) => knownMappings.Contains(mappingId);
    }
}
