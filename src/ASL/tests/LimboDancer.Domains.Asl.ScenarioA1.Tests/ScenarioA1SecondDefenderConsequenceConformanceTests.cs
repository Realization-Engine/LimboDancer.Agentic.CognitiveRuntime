using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>Versioned events through terrain, exact consequence package, and read-only conclusion.</summary>
public sealed class ScenarioA1SecondDefenderConsequenceConformanceTests
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
    public async Task EachReviewedEventPathReturnsOnlyItsBoundedConsequence(string suffix,
        ScenarioA1SecondDefenderState defender, ScenarioA1OverrunNtc ntc,
        ScenarioA1OverrunMf mf, ConclusionDisposition expected)
    {
        var caseId = "A1-second-defender-consequence-" + suffix;
        var baseState = Snapshot();
        var supplied = baseState with
        {
            Eligibility = baseState.Eligibility with
            {
                SecondDefenderState = defender,
                SecondRevealOrdinal = defender == ScenarioA1SecondDefenderState.KnownSmcNotRevealed
                    ? null : 5,
                Ntc = ntc, NtcOrdinal = ntc == ScenarioA1OverrunNtc.Unresolved ? null : 1,
                MfAtSecondReveal = mf,
            },
        };
        var provider = new ScenarioA1SecondDefenderConsequenceObservationProvider(
            new StubSource(supplied), new Board01TerrainCatalog());
        var observation = Assert.Single((await provider.ObserveAsync(Query(caseId))).Observations);
        var before = observation.Data.GetRawText();
        var descriptor = (await new ScenarioA1SecondDefenderConsequencePackage()
            .ResolveAsync(ScenarioA1SecondDefenderConsequencePackage.Identity)).Package!;
        var result = await new ScenarioA1SecondDefenderConsequenceConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, caseId));

        Assert.Equal(expected, result.Disposition);
        Assert.Equal(before, observation.Data.GetRawText());
        Assert.Contains(result.Evidence, item => item.Kind == EvidenceKind.Observation
            && item.Version == supplied.Eligibility.Version && item.ResourceId == "bd01:E4:0");
        Assert.Contains(result.Evidence, item =>
            item.ResourceId == "asl-scenario-a1.second-defender-consequence-case-matrix.json"
            && item.Version == ScenarioA1SecondDefenderConsequencePackage.MatrixSha256);
        if (expected == ConclusionDisposition.Definitive)
        {
            var value = result.Value!.Value;
            Assert.Equal("bd01:D4:0", value.GetProperty("returnToLocationId").GetString());
            Assert.Equal("bd01:D4:0", value.GetProperty("mfExpenditureLocationId").GetString());
            Assert.Equal(2, value.GetProperty("attemptedEntryMf").GetInt32());
            Assert.False(value.GetProperty("executed").GetBoolean());
            Assert.False(value.GetProperty("additionalOvrMfResolved").GetBoolean());
            Assert.False(value.GetProperty("defensiveAttackResolved").GetBoolean());
            Assert.False(value.GetProperty("responseOrCcResolved").GetBoolean());
            Assert.Equal(4, result.ApplicableRules.Count);
        }
        else
        {
            Assert.Null(result.Value);
            Assert.Empty(result.ApplicableRules);
        }
    }

    [Fact]
    public async Task MissingPriorLocationCannotInheritEarlierEligibilityConclusion()
    {
        const string caseId = "A1-second-defender-consequence-smc-revealed";
        var snapshot = Snapshot() with { IsPreviousLocationLastOccupied = null };
        var provider = new ScenarioA1SecondDefenderConsequenceObservationProvider(
            new StubSource(snapshot), new Board01TerrainCatalog());
        Assert.Empty((await provider.ObserveAsync(Query(caseId))).Observations);
        Assert.Empty((await provider.ObserveAsync(Query(caseId,
            ScenarioA1SecondDefenderPackage.Identity))).Observations);
        Assert.Equal(DomainPackageResolutionOutcome.Resolved,
            (await new ScenarioA1SecondDefenderPackage()
                .ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Outcome);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await new ScenarioA1SecondDefenderConsequencePackage()
                .ResolveAsync(ScenarioA1SecondDefenderPackage.Identity)).Outcome);
    }

    private static ObservationQuery Query(string caseId, DomainPackageRef? package = null) =>
        new("query", Tenant, package ?? ScenarioA1SecondDefenderConsequencePackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1SecondDefenderConsequenceObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0", previousLocationId = "bd01:D4:0",
                observationVersion = "snapshot-1", caseId,
            }), 1);

    private static ScenarioA1SecondDefenderConsequenceSnapshot Snapshot() => new(
        ScenarioA1SecondDefenderConsequencePackage.Identity,
        new ScenarioA1SecondDefenderSnapshot(Tenant, ScenarioA1SecondDefenderPackage.Identity,
            "squad", "bd01:E4:0", "snapshot-1", Now, "test-supplied-events",
            new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion,
                Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null))
        {
            InitialOccupancy = ScenarioA1InitialConcealedOccupancy.Concealed,
            A1215FirstRevealOccurred = true, FirstRevealedUnitId = "first-smc",
            FirstRevealOrdinal = 3, FirstRevealedType = ScenarioA1RevealedOccupant.EnemySmc,
            OverrunElection = ScenarioA1OverrunElection.Elected, ElectionOrdinal = 4,
            SecondDefenderUnitId = "second-unit", SecondDefenderLocationId = "bd01:E4:0",
            SecondDefenderState = ScenarioA1SecondDefenderState.RevealedSmc,
            SecondRevealOrdinal = 5, Ntc = ScenarioA1OverrunNtc.Passed, NtcOrdinal = 1,
            MfAtSecondReveal = ScenarioA1OverrunMf.AtLeastFour,
            IsMovementPhase = true, IsGoodOrderUnconcealedNonDummyInfantryMmc = true,
            IsAdjacentGroundLevelOrdinaryBuilding = true,
            IsOrdinaryObstacleEntryNotBypass = true, HasNoA414Exception = true,
            IsFirstSmcOutsideAfv = true, HasNoSpecialModifier = true,
            HasNoLeaderExemption = true,
        }, "bd01:D4:0", true, 2, 2, false);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation observation, string caseId)
    {
        var question = new DomainQuestion("second-defender-consequence", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1SecondDefenderConsequenceConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0", previousLocationId = "bd01:D4:0",
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
            [Entity("squad"), Entity("bd01:E4:0"), Entity("bd01:D4:0")], [observation]);
    }

    private sealed class StubSource(ScenarioA1SecondDefenderConsequenceSnapshot snapshot)
        : IScenarioA1SecondDefenderConsequenceSnapshotSource
    {
        public ValueTask<ScenarioA1SecondDefenderConsequenceSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1SecondDefenderConsequenceSnapshot?>(snapshot);
    }
}
