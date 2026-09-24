using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1ConcealedSmcOverrunObservationTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private const string Qualified = "A1-concealed-smc-qualified-response-unresolved";

    [Fact]
    public async Task PinnedBoardTerrainAndSuppliedDynamicStateYieldOneExactObservation()
    {
        var snapshot = Snapshot();
        var result = await Provider(snapshot).ObserveAsync(Query());
        var observation = Assert.Single(result.Observations);
        Assert.Equal("snapshot-1", observation.Version);
        Assert.Equal(ScenarioA1ConcealedSmcOverrunPackage.Identity, observation.DomainPackage);
        Assert.Equal("A12.15-immediate-defender-reveal",
            observation.Data.GetProperty("revealProvenance").GetString());
        Assert.Equal("verifiedBySuppliedState",
            observation.Data.GetProperty("soleEnemySmcOccupancy").GetString());
        Assert.Equal("passed", observation.Data.GetProperty("ntc").GetString());
        Assert.Equal("atLeastFour", observation.Data.GetProperty("remainingMf").GetString());
        Assert.Equal(17, observation.Data.EnumerateObject().Count());
        Assert.Equal("asl.a1.ovr.exact-case:" + Qualified, Assert.Single(result.ReasonCodes));
    }

    [Fact]
    public async Task MissingConflictingStaleOrWrongPackageStateCannotInventQualifiedFacts()
    {
        var snapshot = Snapshot();
        foreach (var candidate in new[]
        {
            snapshot with { InitialOccupancy = ScenarioA1InitialConcealedOccupancy.Hidden },
            snapshot with { A1215ImmediateDefenderReveal = null },
            snapshot with { RevealedOccupant = ScenarioA1RevealedOccupant.OtherNonDummy },
            snapshot with { OverrunElection = null },
            snapshot with { Ntc = null },
            snapshot with { RemainingMf = ScenarioA1OverrunMf.Insufficient },
            snapshot with { AdditionalDefenderReveal = ScenarioA1AdditionalDefenderReveal.Unresolved },
            snapshot with { AdditionalDefenderReveal = ScenarioA1AdditionalDefenderReveal.AnotherNonDummy,
                AdditionalDefenderType = ScenarioA1AdditionalDefenderType.Unknown,
                SoleEnemySmcOccupancyVerified = false },
            snapshot with { SoleEnemySmcOccupancyVerified = null },
            snapshot with { DefenderResponseOrImmediateCc = ScenarioA1OverrunResponse.ResolvedWithOutcome,
                ResponseOrCcOutcome = null },
            snapshot with { HasNoOtherModifier = false },
            snapshot with { Version = "snapshot-2" },
            snapshot with { Package = ScenarioA1PostRevealPackage.Identity },
            snapshot with { Terrain = snapshot.Terrain with { BoardVersion = "wrong" } },
            snapshot with { Terrain = snapshot.Terrain with { Variant = "NoRoads" } },
            snapshot with { Terrain = snapshot.Terrain with { Hex = "E5" } },
        })
            Assert.Empty((await Provider(candidate).ObserveAsync(Query())).Observations);
        Assert.Empty((await Provider(snapshot).ObserveAsync(Query(
            package: ScenarioA1PostRevealPackage.Identity))).Observations);
        Assert.Empty((await Provider(snapshot).ObserveAsync(Query(extra: true))).Observations);
    }

    [Theory]
    [InlineData("A1-concealed-smc-election-unknown")]
    [InlineData("A1-concealed-smc-declined")]
    [InlineData("A1-concealed-smc-ntc-failed")]
    [InlineData("A1-concealed-smc-mf-insufficient")]
    [InlineData("A1-concealed-smc-another-defender-revealed")]
    [InlineData("A1-concealed-smc-response-resolved")]
    public async Task DistinctSuppliedBranchesProjectOnlyTheirOwnExactCase(string caseId)
    {
        var snapshot = caseId switch
        {
            "A1-concealed-smc-election-unknown" => BeforeNtc(Snapshot()) with
                { OverrunElection = ScenarioA1OverrunElection.Unknown, Ntc = null },
            "A1-concealed-smc-declined" => BeforeNtc(Snapshot()) with
                { OverrunElection = ScenarioA1OverrunElection.Declined, Ntc = null },
            "A1-concealed-smc-ntc-failed" => BeforeNtc(Snapshot()) with
                { Ntc = ScenarioA1OverrunNtc.Failed },
            "A1-concealed-smc-mf-insufficient" => BeforeMf(Snapshot()) with
                { RemainingMf = ScenarioA1OverrunMf.Insufficient },
            "A1-concealed-smc-another-defender-revealed" => Snapshot() with
                { AdditionalDefenderReveal = ScenarioA1AdditionalDefenderReveal.AnotherNonDummy,
                    AdditionalDefenderType = ScenarioA1AdditionalDefenderType.Mmc,
                    SoleEnemySmcOccupancyVerified = false, DefenderResponseOrImmediateCc = null },
            "A1-concealed-smc-response-resolved" => Snapshot() with
                { DefenderResponseOrImmediateCc = ScenarioA1OverrunResponse.ResolvedWithOutcome,
                    ResponseOrCcOutcome = "state-source-recorded-outcome" },
            _ => throw new InvalidOperationException("Unexpected case."),
        };
        Assert.Single((await Provider(snapshot).ObserveAsync(Query(caseId))).Observations);
        Assert.Empty((await Provider(snapshot).ObserveAsync(Query())).Observations);
    }

    private static ScenarioA1ConcealedSmcOverrunSnapshot BeforeNtc(
        ScenarioA1ConcealedSmcOverrunSnapshot snapshot) => snapshot with
        {
            RemainingMf = null, AdditionalDefenderReveal = null, AdditionalDefenderType = null,
            SoleEnemySmcOccupancyVerified = null, DefenderResponseOrImmediateCc = null,
            ResponseOrCcOutcome = null,
        };

    private static ScenarioA1ConcealedSmcOverrunSnapshot BeforeMf(
        ScenarioA1ConcealedSmcOverrunSnapshot snapshot) => snapshot with
        {
            AdditionalDefenderReveal = null, AdditionalDefenderType = null,
            SoleEnemySmcOccupancyVerified = null, DefenderResponseOrImmediateCc = null,
            ResponseOrCcOutcome = null,
        };

    private static ScenarioA1ConcealedSmcOverrunObservationProvider Provider(
        ScenarioA1ConcealedSmcOverrunSnapshot state) =>
        new(new StubSource(state), new Board01TerrainCatalog());

    private static ObservationQuery Query(string caseId = Qualified,
        DomainPackageRef? package = null, bool extra = false) => new("query", Tenant,
        package ?? ScenarioA1ConcealedSmcOverrunPackage.Identity,
        new SemanticIdentifier(new DomainId("asl"),
            ScenarioA1ConcealedSmcOverrunObservationProvider.QueryKind),
        extra ? JsonSerializer.SerializeToElement(new
        {
            unitId = "squad", locationId = "bd01:E4:0", observationVersion = "snapshot-1",
            caseId, unreviewed = "yes",
        }) : JsonSerializer.SerializeToElement(new
        {
            unitId = "squad", locationId = "bd01:E4:0", observationVersion = "snapshot-1", caseId,
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

    private sealed class StubSource(ScenarioA1ConcealedSmcOverrunSnapshot snapshot)
        : IScenarioA1ConcealedSmcOverrunSnapshotSource
    {
        public ValueTask<ScenarioA1ConcealedSmcOverrunSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1ConcealedSmcOverrunSnapshot?>(snapshot);
    }
}
