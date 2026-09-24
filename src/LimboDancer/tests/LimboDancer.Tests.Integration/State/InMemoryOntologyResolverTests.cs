using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Ontology;
using LimboDancer.Infrastructure.Ontology;

namespace LimboDancer.Tests.Integration.State;

public sealed class InMemoryOntologyResolverTests
{
    [Fact]
    public async Task MappingRoundTripsWithinTenantAndUnknownTermsFailClosed()
    {
        var resolver = new InMemoryOntologyResolver();
        var tenant = new TenantScope(Guid.NewGuid());
        var otherTenant = new TenantScope(Guid.NewGuid());
        var mapping = new OntologyMapping(OntologyTermKind.Property, "ldm:status", "status");
        resolver.Register(tenant, mapping);

        Assert.Equal(mapping, await resolver.ResolveAsync(tenant, OntologyTermKind.Property, "ldm:status"));
        Assert.Equal(mapping, await resolver.ResolveStorageNameAsync(tenant, OntologyTermKind.Property, "status"));
        Assert.Null(await resolver.ResolveAsync(tenant, OntologyTermKind.Property, "ldm:unknown"));
        Assert.Null(await resolver.ResolveAsync(otherTenant, OntologyTermKind.Property, "ldm:status"));
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var resolver = new InMemoryOntologyResolver();
        var cancellation = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => resolver.ResolveAsync(
                new TenantScope(Guid.NewGuid()),
                OntologyTermKind.Property,
                "ldm:status",
                cancellation).AsTask());
    }
}
