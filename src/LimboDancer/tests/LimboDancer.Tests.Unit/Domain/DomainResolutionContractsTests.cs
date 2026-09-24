using LimboDancer.Abstractions.Domain;
using LimboDancer.Runtime.Domain;

namespace LimboDancer.Tests.Unit.Domain;

public sealed class DomainResolutionContractsTests
{
    [Fact]
    public async Task PackageResolutionRequiresExactRegisteredVersion()
    {
        var registered = CreatePackage("1.0");
        var registry = new DomainPackageRegistry(
            [new DomainPackageDescriptor(registered, [CreateSource(registered)])]);

        var exact = await registry.ResolveAsync(CreatePackage("1.0"));
        var newer = await registry.ResolveAsync(CreatePackage("1.1"));

        Assert.Equal(DomainPackageResolutionOutcome.Resolved, exact.Outcome);
        Assert.Equal("1.0", exact.Package!.Identity.Version);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable, newer.Outcome);
        Assert.Null(newer.Package);
    }

    [Fact]
    public void EntityResolutionOutcomeControlsCandidateCardinality()
    {
        var tenantId = Guid.NewGuid();
        var package = CreatePackage("1.0");
        var query = CreateEntityQuery(tenantId, package);

        Assert.Throws<ArgumentException>(() => new DomainEntityResolution(
            query,
            DomainEntityResolutionOutcome.Resolved,
            [],
            ["entity.resolved"]));
        Assert.Throws<ArgumentException>(() => new DomainEntityResolution(
            query,
            DomainEntityResolutionOutcome.Ambiguous,
            [CreateEntity(tenantId, package, "entity-1")],
            ["entity.ambiguous"]));

        var unresolved = new DomainEntityResolution(
            query,
            DomainEntityResolutionOutcome.Unresolved,
            [],
            ["entity.unresolved"]);
        Assert.Empty(unresolved.Candidates);
    }

    [Fact]
    public void EntityResolutionRejectsCrossTenantEvidence()
    {
        var tenantId = Guid.NewGuid();
        var package = CreatePackage("1.0");
        var query = CreateEntityQuery(tenantId, package);

        Assert.Throws<ArgumentException>(() => new DomainEntityResolution(
            query,
            DomainEntityResolutionOutcome.Resolved,
            [CreateEntity(Guid.NewGuid(), package, "entity-1")],
            ["entity.resolved"]));
    }

    internal static DomainPackageRef CreatePackage(string version) => new(
        new DomainId("fake-domain"),
        "fake-package",
        version);

    internal static CanonicalReference CreateSource(DomainPackageRef package) => new(
        package,
        "fake-source",
        "root",
        package.Version);

    internal static DomainEntityQuery CreateEntityQuery(Guid tenantId, DomainPackageRef package) => new(
        "entity-query-1",
        tenantId,
        package,
        new SemanticIdentifier(package.DomainId, "fake-entity-kind"),
        "subject-1");

    internal static DomainEntityCandidate CreateEntity(
        Guid tenantId,
        DomainPackageRef package,
        string identity) => new(
            new SemanticIdentifier(package.DomainId, identity),
            new CanonicalReference(package, "fake-entities", identity, package.Version),
            new EvidenceReference(
                $"evidence-{identity}",
                EvidenceKind.CanonicalSource,
                tenantId,
                package,
                identity,
                package.Version,
                "fake-domain"));
}
