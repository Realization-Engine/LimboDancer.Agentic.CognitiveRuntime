using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Tests.Unit.Domain;
using LimboDancer.Tests.Unit.Runtime;

namespace LimboDancer.Tests.Unit.Observations;

public sealed class ObservationAcquisitionContractsTests
{
    [Fact]
    public void AcquisitionPreservesTenantPackageVersionAndProvenance()
    {
        var tenantId = Guid.NewGuid();
        var package = DomainResolutionContractsTests.CreatePackage("1.0");
        var query = CreateQuery(tenantId, package, maxResults: 1);
        var observation = CreateObservation(tenantId, package, "state-42");

        var result = new ObservationAcquisitionResult(query, [observation]);

        var acquired = Assert.Single(result.Observations);
        Assert.Equal(tenantId, acquired.TenantId);
        Assert.Equal(package, acquired.DomainPackage);
        Assert.Equal("state-42", acquired.Version);
        Assert.Equal("fake-observation-provider", acquired.Provenance);
    }

    [Fact]
    public void AcquisitionRejectsUnboundedDuplicateOrCrossScopeResults()
    {
        var tenantId = Guid.NewGuid();
        var package = DomainResolutionContractsTests.CreatePackage("1.0");
        var query = CreateQuery(tenantId, package, maxResults: 1);
        var duplicateQuery = CreateQuery(tenantId, package, maxResults: 2);
        var observation = CreateObservation(tenantId, package, "state-42");

        Assert.Throws<ArgumentException>(() => new ObservationAcquisitionResult(
            duplicateQuery,
            [observation, observation]));
        Assert.Throws<ArgumentException>(() => new ObservationAcquisitionResult(
            query,
            [observation, observation]));
        Assert.Throws<ArgumentException>(() => new ObservationAcquisitionResult(
            query,
            [CreateObservation(Guid.NewGuid(), package, "state-42")]));
        Assert.Throws<ArgumentException>(() => new ObservationAcquisitionResult(
            query,
            [CreateObservation(tenantId, DomainResolutionContractsTests.CreatePackage("1.1"), "state-42")]));
    }

    internal static ObservationQuery CreateQuery(
        Guid tenantId,
        DomainPackageRef package,
        int maxResults = 10) => new(
            "observation-query-1",
            tenantId,
            package,
            new SemanticIdentifier(package.DomainId, "fake-observation-kind"),
            GoalContractsTests.ParseJson("{}"),
            maxResults);

    internal static Observation CreateObservation(
        Guid tenantId,
        DomainPackageRef package,
        string? version,
        string data = "{}") => new(
            "observation-1",
            new ObservationSource("fake-provider", "1"),
            tenantId,
            DateTimeOffset.UtcNow,
            GoalContractsTests.ParseJson(data),
            "subject-1",
            version,
            "fake-observation-provider",
            package);
}
