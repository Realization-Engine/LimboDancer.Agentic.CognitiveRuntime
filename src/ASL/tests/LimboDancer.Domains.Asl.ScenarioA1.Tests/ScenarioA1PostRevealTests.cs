using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1PostRevealTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ScenarioA1DefenderReveal.NonDummy,
        "A1-post-reveal-nondummy-forced-back", ConclusionDisposition.Definitive,
        true, false)]
    [InlineData(ScenarioA1DefenderReveal.DummiesOnly,
        "A1-post-reveal-dummies-only-continue", ConclusionDisposition.Qualified,
        false, true)]
    public async Task AffirmativeReviewResolvesOnlyTwoPostRevealConsequences(
        ScenarioA1DefenderReveal reveal, string caseId, ConclusionDisposition expected,
        bool forcedBack, bool mayContinue)
    {
        var package = new ScenarioA1PostRevealPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1PostRevealPackage.Identity)).Package!;
        var observation = Assert.Single((await Provider(Snapshot() with
        {
            DefenderReveal = reveal,
        }).ObserveAsync(Query())).Observations);
        var before = observation.Data.GetRawText();
        var result = await new ScenarioA1PostRevealConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation));
        Assert.Equal(expected, result.Disposition);
        Assert.Equal(caseId, result.Value!.Value.GetProperty("caseId").GetString());
        Assert.Equal(forcedBack, result.Value.Value.GetProperty("forcedBack").GetBoolean());
        Assert.Equal(mayContinue,
            result.Value.Value.GetProperty("mayContinueFromAttemptedLocation").GetBoolean());
        Assert.Equal(forcedBack, result.Value.Value.GetProperty("movementPhaseEnds").GetBoolean());
        Assert.Equal(forcedBack,
            result.Value.Value.GetProperty("attemptedMfSpentInPreviousLocation").GetBoolean());
        Assert.Equal(mayContinue, result.Value.Value.GetProperty("dummiesRemoved").GetBoolean());
        Assert.False(result.Value.Value.GetProperty("followOnFireResolved").GetBoolean());
        Assert.Equal(forcedBack ? 2 : 1, result.ApplicableRules.Count);
        Assert.Contains(result.ApplicableRules, item => item.ElementId == "A12.15");
        Assert.Contains(result.Evidence, item =>
            item.Kind == EvidenceKind.Observation && item.Version == "snapshot-1");
        Assert.Equal(before, observation.Data.GetRawText());
        Assert.Equal("indeterminate", new ScenarioA1SemanticCandidate()
            .Evaluate("A1-concealed-occupancy-attempt", new Dictionary<string, string>())
            .Disposition);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1OccupiedPackage.Identity)).Outcome);
    }

    [Fact]
    public async Task UnknownRevealAndExcludedModesNeverProduceAnObservation()
    {
        var baseline = Snapshot();
        foreach (var state in new[]
        {
            baseline,
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                IsObstacleEntryNotBypass = false },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                IsAttackerUnconcealedNonDummy = null },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                HasNoA414EntryException = false },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                HasNoOverrunElection = false },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                IsMovementPhase = false },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                Terrain = baseline.Terrain with { Variant = "NoRoads" } },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                Version = "snapshot-2" },
            baseline with { DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
                TenantId = Guid.NewGuid() },
        })
            Assert.Empty((await Provider(state).ObserveAsync(Query())).Observations);
        Assert.Empty((await Provider(baseline with
        {
            DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
        }).ObserveAsync(new ObservationQuery("extra", Tenant, ScenarioA1PostRevealPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1PostRevealObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1", caseId = "forced-back",
            }), 1))).Observations);
    }

    [Fact]
    public async Task MissingStaleOrCrossScopeEvidenceCannotYieldAResult()
    {
        var descriptor = (await new ScenarioA1PostRevealPackage()
            .ResolveAsync(ScenarioA1PostRevealPackage.Identity)).Package!;
        var observation = Assert.Single((await Provider(Snapshot() with
        {
            DefenderReveal = ScenarioA1DefenderReveal.NonDummy,
        }).ObserveAsync(Query())).Observations);
        var context = Context(descriptor, observation);
        var resolver = new ScenarioA1PostRevealConclusionResolver();
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(new DomainConclusionContext(context.Question,
                descriptor, context.EntityResolutions, []))).Disposition);
        var changed = Copy(observation, version: "snapshot-2");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, changed))).Disposition);
        Assert.Throws<ArgumentException>(() => Context(descriptor,
            Copy(observation, tenantId: Guid.NewGuid())));
        Assert.Throws<ArgumentException>(() => Context(descriptor,
            Copy(observation, package: ScenarioA1OccupiedPackage.Identity)));
        var extra = new Observation("extra", observation.Source, Tenant, Now,
            JsonSerializer.SerializeToElement(new Dictionary<string, string>
            {
                ["defenderReveal"] = "nonDummy", ["previousLocationId"] = "bd01:D4:0",
                ["a414Exception"] = "none", ["attacker"] = "unconcealedNonDummyOrdinaryInfantry",
                ["entryMode"] = "obstacleEntryNotBypass", ["overrunElection"] = "none",
                ["phase"] = "mph", ["specialModifier"] = "none",
                ["unreviewed"] = "yes",
            }), observation.ResourceId, observation.Version, observation.Provenance,
            observation.DomainPackage);
        Assert.Equal(ConclusionDisposition.Abstained,
            (await resolver.ConcludeAsync(Context(descriptor, extra))).Disposition);
    }

    private static Observation Copy(Observation source, string? version = null,
        Guid? tenantId = null, DomainPackageRef? package = null) =>
        new(source.ObservationId, source.Source, tenantId ?? source.TenantId,
            source.ObservedAt, source.Data, source.ResourceId, version ?? source.Version,
            source.Provenance, package ?? source.DomainPackage);

    private static ScenarioA1PostRevealObservationProvider Provider(
        ScenarioA1PostRevealSnapshot state) =>
        new(new StubSource(state), new Board01TerrainCatalog());

    private static ObservationQuery Query() =>
        new("query", Tenant, ScenarioA1PostRevealPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1PostRevealObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1",
            }), 1);

    private static ScenarioA1PostRevealSnapshot Snapshot() =>
        new(Tenant, ScenarioA1PostRevealPackage.Identity, "squad", "bd01:E4:0",
            "bd01:D4:0", "snapshot-1", Now, "test-post-reveal-state",
            new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion,
                Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null),
            ScenarioA1DefenderReveal.Unknown,
            true, true, true, true, true, true, true);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation observation)
    {
        var question = new DomainQuestion("post-reveal-entry", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1PostRevealConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1",
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, descriptor.Identity,
                new SemanticIdentifier(descriptor.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(
                new SemanticIdentifier(descriptor.Identity.DomainId, id),
                descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant,
                    descriptor.Identity, id, descriptor.Identity.Version, "test"))],
            ["test.resolution"]);
        return new DomainConclusionContext(question, descriptor,
            [Entity("squad"), Entity("bd01:E4:0")], [observation]);
    }

    private sealed class StubSource(ScenarioA1PostRevealSnapshot snapshot)
        : IScenarioA1PostRevealSnapshotSource
    {
        public ValueTask<ScenarioA1PostRevealSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1PostRevealSnapshot?>(snapshot);
    }
}
