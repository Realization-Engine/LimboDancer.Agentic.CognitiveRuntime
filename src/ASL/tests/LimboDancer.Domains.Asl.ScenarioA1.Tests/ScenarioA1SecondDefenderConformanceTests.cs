using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>Supplied ordered events through pinned terrain, package, and eligibility conclusion.</summary>
public sealed class ScenarioA1SecondDefenderConformanceTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("smc-revealed", ScenarioA1SecondDefenderState.RevealedSmc,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, ConclusionDisposition.Definitive)]
    [InlineData("mmc-revealed", ScenarioA1SecondDefenderState.RevealedMmc,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, ConclusionDisposition.Definitive)]
    [InlineData("other-type", ScenarioA1SecondDefenderState.RevealedOtherNonDummy,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, ConclusionDisposition.Indeterminate)]
    [InlineData("unknown-type", ScenarioA1SecondDefenderState.RevealedUnknownType,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, ConclusionDisposition.Indeterminate)]
    [InlineData("unrevealed-smc", ScenarioA1SecondDefenderState.KnownSmcNotRevealed,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, ConclusionDisposition.Indeterminate)]
    [InlineData("capability-unresolved", ScenarioA1SecondDefenderState.RevealedSmc,
        ScenarioA1OverrunNtc.Unresolved, ScenarioA1OverrunMf.Unknown, ConclusionDisposition.Indeterminate)]
    [InlineData("mf-insufficient", ScenarioA1SecondDefenderState.RevealedSmc,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.Insufficient, ConclusionDisposition.Abstained)]
    public async Task EachReviewedEventPathConcludesOnlyItsEligibilityCase(string suffix,
        ScenarioA1SecondDefenderState secondState, ScenarioA1OverrunNtc ntc,
        ScenarioA1OverrunMf mf, ConclusionDisposition expected)
    {
        var caseId = "A1-second-defender-" + suffix;
        var snapshot = Snapshot() with
        {
            SecondDefenderState = secondState,
            SecondRevealOrdinal = secondState == ScenarioA1SecondDefenderState.KnownSmcNotRevealed
                ? null : 4,
            Ntc = ntc, NtcOrdinal = ntc == ScenarioA1OverrunNtc.Unresolved ? null : 1,
            MfAtSecondReveal = mf,
        };
        var provider = new ScenarioA1SecondDefenderObservationProvider(
            new StubSource(snapshot), new Board01TerrainCatalog());
        var observation = Assert.Single((await provider.ObserveAsync(Query(caseId))).Observations);
        var original = observation.Data.GetRawText();
        var descriptor = (await new ScenarioA1SecondDefenderPackage()
            .ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Package!;
        var conclusion = await new ScenarioA1SecondDefenderConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, caseId));

        Assert.Equal(expected, conclusion.Disposition);
        Assert.Equal(original, observation.Data.GetRawText());
        Assert.Contains(conclusion.Evidence, item => item.Kind == EvidenceKind.Observation
            && item.Version == snapshot.Version && item.ResourceId == snapshot.LocationId);
        Assert.Contains(conclusion.Evidence, item =>
            item.ResourceId == "asl-scenario-a1.second-defender-reveal-case-matrix.json"
            && item.Version == ScenarioA1SecondDefenderPackage.MatrixSha256);
        if (expected == ConclusionDisposition.Definitive)
        {
            var value = conclusion.Value!.Value;
            Assert.Equal(caseId, value.GetProperty("caseId").GetString());
            Assert.False(value.GetProperty("singleSmcOverrunEligible").GetBoolean());
            Assert.False(value.GetProperty("forcedBackResolved").GetBoolean());
            Assert.False(value.GetProperty("mfSpendResolved").GetBoolean());
            Assert.False(value.GetProperty("responseOrCcResolved").GetBoolean());
            Assert.Equal(3, conclusion.ApplicableRules.Count);
        }
        else
        {
            Assert.Null(conclusion.Value);
            Assert.Empty(conclusion.ApplicableRules);
        }
    }

    [Fact]
    public async Task ExactPackageDoesNotReplaceEarlierMilestones()
    {
        var package = new ScenarioA1SecondDefenderPackage();
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1ConcealedSmcOverrunPackage()
                .ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1PostRevealPackage()
                .ResolveAsync(ScenarioA1PostRevealPackage.Identity)).Outcome);
        var provider = new ScenarioA1SecondDefenderObservationProvider(
            new StubSource(Snapshot()), new Board01TerrainCatalog());
        Assert.Empty((await provider.ObserveAsync(Query("A1-second-defender-smc-revealed",
            ScenarioA1ConcealedSmcOverrunPackage.Identity))).Observations);
    }

    private static ObservationQuery Query(string caseId, DomainPackageRef? package = null) =>
        new("query", Tenant, package ?? ScenarioA1SecondDefenderPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1SecondDefenderObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1", caseId,
            }), 1);

    private static ScenarioA1SecondDefenderSnapshot Snapshot() => new(
        Tenant, ScenarioA1SecondDefenderPackage.Identity, "squad", "bd01:E4:0",
        "snapshot-1", Now, "test-supplied-events",
        new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion,
            Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null))
    {
        InitialOccupancy = ScenarioA1InitialConcealedOccupancy.Concealed,
        A1215FirstRevealOccurred = true, FirstRevealedUnitId = "first-smc",
        FirstRevealOrdinal = 2, FirstRevealedType = ScenarioA1RevealedOccupant.EnemySmc,
        OverrunElection = ScenarioA1OverrunElection.Elected, ElectionOrdinal = 3,
        SecondDefenderUnitId = "second-unit", SecondDefenderLocationId = "bd01:E4:0",
        SecondDefenderState = ScenarioA1SecondDefenderState.RevealedSmc,
        SecondRevealOrdinal = 4, Ntc = ScenarioA1OverrunNtc.Passed, NtcOrdinal = 1,
        MfAtSecondReveal = ScenarioA1OverrunMf.AtLeastFour,
        IsMovementPhase = true, IsGoodOrderUnconcealedNonDummyInfantryMmc = true,
        IsAdjacentGroundLevelOrdinaryBuilding = true,
        IsOrdinaryObstacleEntryNotBypass = true, HasNoA414Exception = true,
        IsFirstSmcOutsideAfv = true, HasNoSpecialModifier = true, HasNoLeaderExemption = true,
    };

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation observation, string caseId)
    {
        var question = new DomainQuestion("second-defender-conformance", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1SecondDefenderConclusionResolver.QuestionKind),
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

    private sealed class StubSource(ScenarioA1SecondDefenderSnapshot snapshot)
        : IScenarioA1SecondDefenderSnapshotSource
    {
        public ValueTask<ScenarioA1SecondDefenderSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1SecondDefenderSnapshot?>(snapshot);
    }
}
