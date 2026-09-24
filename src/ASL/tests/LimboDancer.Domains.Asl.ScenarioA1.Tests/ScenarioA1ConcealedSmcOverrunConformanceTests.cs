using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>Supplied state through board 01 validation, exact package, and read-only conclusion.</summary>
public sealed class ScenarioA1ConcealedSmcOverrunConformanceTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private const string Qualified = "A1-concealed-smc-qualified-response-unresolved";

    [Fact]
    public async Task SuppliedRevealAndSoleOccupancyReachOnlyQualifiedAttempt()
    {
        var state = Snapshot();
        var package = new ScenarioA1ConcealedSmcOverrunPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Package!;
        var result = await new ScenarioA1ConcealedSmcOverrunObservationProvider(
            new StubSource(state), new Board01TerrainCatalog()).ObserveAsync(Query(Qualified));
        var observation = Assert.Single(result.Observations);
        var original = observation.Data.GetRawText();
        var conclusion = await new ScenarioA1ConcealedSmcOverrunConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, Qualified));
        Assert.Equal(ConclusionDisposition.Qualified, conclusion.Disposition);
        Assert.Equal(4, conclusion.Value!.Value.GetProperty("entryMfRequired").GetInt32());
        Assert.True(conclusion.Value.Value.GetProperty("attemptOnly").GetBoolean());
        Assert.Equal("defenderResponseOrImmediateCloseCombat",
            conclusion.Value.Value.GetProperty("nextResolution").GetString());
        Assert.Equal(6, conclusion.ApplicableRules.Count);
        Assert.Contains(conclusion.Evidence, item => item.Kind == EvidenceKind.Observation
            && item.Version == "snapshot-1");
        Assert.Contains(conclusion.Evidence, item =>
            item.ResourceId == "asl-scenario-a1.concealed-smc-overrun-case-matrix.json"
            && item.Version == ScenarioA1ConcealedSmcOverrunPackage.MatrixSha256);
        Assert.Equal(original, observation.Data.GetRawText());
        Assert.False(conclusion.Value.Value.TryGetProperty("mfSpent", out _));
        Assert.False(conclusion.Value.Value.TryGetProperty("closeCombatResolved", out _));
    }

    [Fact]
    public async Task AdditionalDefenderCannotInheritSoleSmcAttempt()
    {
        const string caseId = "A1-concealed-smc-another-defender-revealed";
        var state = Snapshot() with
        {
            AdditionalDefenderReveal = ScenarioA1AdditionalDefenderReveal.AnotherNonDummy,
            AdditionalDefenderType = ScenarioA1AdditionalDefenderType.Mmc,
            SoleEnemySmcOccupancyVerified = false,
            DefenderResponseOrImmediateCc = null,
        };
        var descriptor = (await new ScenarioA1ConcealedSmcOverrunPackage()
            .ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Package!;
        var provider = new ScenarioA1ConcealedSmcOverrunObservationProvider(
            new StubSource(state), new Board01TerrainCatalog());
        Assert.Empty((await provider.ObserveAsync(Query(Qualified))).Observations);
        var observation = Assert.Single((await provider.ObserveAsync(Query(caseId))).Observations);
        var conclusion = await new ScenarioA1ConcealedSmcOverrunConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, caseId));
        Assert.Equal(ConclusionDisposition.Indeterminate, conclusion.Disposition);
        Assert.Null(conclusion.Value);
        Assert.Empty(conclusion.ApplicableRules);
    }

    [Fact]
    public async Task ExactPackageCannotFloatOrReplaceEarlierEvidencePaths()
    {
        var package = new ScenarioA1ConcealedSmcOverrunPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Package!;
        Assert.Equal("sha256:" + ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256,
            descriptor.Identity.Version);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1OccupiedPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1OccupiedPackage()
                .ResolveAsync(ScenarioA1OccupiedPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1PostRevealPackage()
                .ResolveAsync(ScenarioA1PostRevealPackage.Identity)).Outcome);
        var provider = new ScenarioA1ConcealedSmcOverrunObservationProvider(
            new StubSource(Snapshot()), new Board01TerrainCatalog());
        Assert.Empty((await provider.ObserveAsync(Query(Qualified,
            ScenarioA1PostRevealPackage.Identity))).Observations);
        var observation = Assert.Single((await provider.ObserveAsync(Query(Qualified))).Observations);
        var context = Context(descriptor, observation, Qualified);
        var wrong = new DomainPackageDescriptor(descriptor.Identity,
            descriptor.CanonicalSources.Take(5).ToArray());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await new ScenarioA1ConcealedSmcOverrunConclusionResolver()
                .ConcludeAsync(new DomainConclusionContext(context.Question, wrong,
                    context.EntityResolutions, context.Observations)));
    }

    private static ObservationQuery Query(string caseId, DomainPackageRef? package = null) =>
        new("query", Tenant, package ?? ScenarioA1ConcealedSmcOverrunPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1ConcealedSmcOverrunObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1", caseId,
            }), 1);

    private static ScenarioA1ConcealedSmcOverrunSnapshot Snapshot() => new(
        Tenant, ScenarioA1ConcealedSmcOverrunPackage.Identity, "squad", "bd01:E4:0",
        "snapshot-1", Now, "test-supplied-state",
        new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion,
            Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null),
        ScenarioA1InitialConcealedOccupancy.Concealed, true,
        ScenarioA1RevealedOccupant.EnemySmc, true,
        true, true, true, true, true, true, true,
        ScenarioA1OverrunElection.Elected, ScenarioA1OverrunNtc.Passed,
        ScenarioA1OverrunMf.AtLeastFour, ScenarioA1AdditionalDefenderReveal.None,
        null, true, ScenarioA1OverrunResponse.Unresolved, null);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation observation, string caseId)
    {
        var question = new DomainQuestion("ovr-conformance", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1ConcealedSmcOverrunConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1", caseId,
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, descriptor.Identity,
                new SemanticIdentifier(descriptor.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(descriptor.Identity.DomainId, id),
                descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant,
                    descriptor.Identity, id, descriptor.Identity.Version, "test"))],
            ["test.resolution"]);
        return new DomainConclusionContext(question, descriptor,
            [Entity("squad"), Entity("bd01:E4:0")], [observation]);
    }

    private sealed class StubSource(ScenarioA1ConcealedSmcOverrunSnapshot snapshot)
        : IScenarioA1ConcealedSmcOverrunSnapshotSource
    {
        public ValueTask<ScenarioA1ConcealedSmcOverrunSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1ConcealedSmcOverrunSnapshot?>(snapshot);
    }
}
