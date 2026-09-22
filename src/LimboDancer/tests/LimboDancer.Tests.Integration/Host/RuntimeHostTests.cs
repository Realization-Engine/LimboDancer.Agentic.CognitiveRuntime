using System.Security.Claims;
using LimboDancer.Adapters.Mcp;
using LimboDancer.Host;
using LimboDancer.Infrastructure.Audit;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Directed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LimboDancer.Tests.Integration.Host;

public sealed class RuntimeHostTests
{
    [Fact]
    public async Task CompositionResolvesAndStartupStructuralValidationPasses()
    {
        await using var provider = CreateServices().BuildServiceProvider();

        var runtime = provider.GetRequiredService<IDirectedActionRuntime>();
        var adapter = provider.GetRequiredService<IMcpInteractionAdapter>();
        var validator = provider.GetRequiredService<RuntimeStructureValidator>();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();

        Assert.NotNull(runtime);
        Assert.Equal(4, adapter.ListTools().Count);
        Assert.True(validator.Validate().IsValid);
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void StructuralValidationFailsWhenARequiredExecutorIsMissing()
    {
        var registry = new ActionRegistry(BuiltInActionCatalog.CreateDescriptors());
        var bindings = new ActionBindingRegistry(BuiltInActionCatalog.CreateMcpBindings());
        var resolver = new ActionExecutorResolver([]);
        var diagnostics = new DiagnosticRunner([]);
        var validator = new RuntimeStructureValidator(registry, bindings, resolver, diagnostics);

        var result = validator.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, static failure => failure.Contains("no matching executor", StringComparison.Ordinal));
    }

    [Fact]
    public void StructuralValidationFailsWhenARequiredDescriptorIsMissing()
    {
        var registry = new ActionRegistry([]);
        var bindings = new ActionBindingRegistry(BuiltInActionCatalog.CreateMcpBindings());
        var resolver = new ActionExecutorResolver([]);
        var diagnostics = new DiagnosticRunner([]);
        var validator = new RuntimeStructureValidator(registry, bindings, resolver, diagnostics);

        var result = validator.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, static failure => failure.StartsWith("Required action", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadinessValidationIsObservational()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var audit = provider.GetRequiredService<InMemoryAuditSink>();
        var validator = provider.GetRequiredService<RuntimeStructureValidator>();
        var before = audit.Snapshot();

        var first = validator.Validate();
        var second = validator.Validate();

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        Assert.Equal(before, audit.Snapshot());
    }

    [Fact]
    public void AuthenticatedIdentityDeterminesTenantAndPrincipalContext()
    {
        var tenantId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "operator"),
                new Claim(LimboDancerClaimTypes.TenantId, tenantId.ToString("D")),
                new Claim(LimboDancerClaimTypes.Permission, "history.read"),
            ],
            "test");

        var caller = new McpCallerContextFactory().Create(new ClaimsPrincipal(identity));

        Assert.Equal(tenantId, caller.TenantId);
        Assert.Equal(tenantId, caller.Principal.TenantId);
        Assert.Equal("operator", caller.Principal.PrincipalId);
        Assert.Contains("history.read", caller.Principal.Permissions);
    }

    [Fact]
    public void HostCompositionLoadsOnlyNewRuntimeAssemblies()
    {
        using var provider = CreateServices().BuildServiceProvider();
        _ = provider.GetRequiredService<IMcpInteractionAdapter>();

        var legacyAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(static assembly => assembly.GetName().Name)
            .Where(static name => name?.StartsWith("LimboDancer.MCP.", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Empty(legacyAssemblies);
    }

    private static ServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LimboDancer:ServerName"] = "LimboDancer.Tests",
                ["LimboDancer:ServerVersion"] = "1.0.0",
                ["LimboDancer:InvocationTimeoutSeconds"] = "10",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLimboDancerHost(configuration);
        return services;
    }
}
