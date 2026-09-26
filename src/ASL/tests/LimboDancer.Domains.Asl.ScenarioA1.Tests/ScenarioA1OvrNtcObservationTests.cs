using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The OVR NTC observation provider (Infantry OVR Design, section 5): each supplied branch projects its own exact case,
/// and a snapshot outside the package's scope or reviewed state projects nothing.
/// </summary>
public sealed class ScenarioA1OvrNtcObservationTests
{
    private static readonly Guid Tenant = Guid.Parse("0c5a4f1e-7d2b-4c69-9f3e-5a1b2c3d4e5f");
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("failed")]
    [InlineData("mf-insufficient")]
    [InlineData("passed-second-defender-revealed")]
    [InlineData("passed-against-lone-smc")]
    [InlineData("passed-other-occupants-unknown")]
    public async Task EachSuppliedBranchProjectsItsOwnExactCase(string suffix)
    {
        var failed = Snapshot();
        var snapshot = suffix switch
        {
            "failed" => failed,
            "mf-insufficient" => failed with { Election = ScenarioA1OvrElection.Requested, RemainingMf = ScenarioA1OvrRemainingMf.BelowFour, Ntc = null },
            "passed-second-defender-revealed" => failed with
            {
                Ntc = ScenarioA1OvrNtcResult.Passed,
                OtherConcealedNonDummy = ScenarioA1OtherConcealedNonDummy.Present,
                SecondRevealRevealedNonDummy = true,
            },
            "passed-against-lone-smc" => failed with { Ntc = ScenarioA1OvrNtcResult.Passed, OtherConcealedNonDummy = ScenarioA1OtherConcealedNonDummy.None },
            _ => failed with { Ntc = ScenarioA1OvrNtcResult.Passed, OtherConcealedNonDummy = ScenarioA1OtherConcealedNonDummy.Unknown },
        };
        var result = await Provider(snapshot).ObserveAsync(Query());
        var observation = Assert.Single(result.Observations);
        Assert.Equal("asl.a1.ovr-ntc.exact-case:A1-ovr-ntc-" + suffix, Assert.Single(result.ReasonCodes));
        Assert.Equal(("snapshot-1", ScenarioA1OvrNtcPackage.Identity), (observation.Version, observation.DomainPackage));
        Assert.Equal("bd01:D4:0", observation.Data.GetProperty("previousLocationId").GetString());
        Assert.Equal("oneEnemySmc-under-A12.15", observation.Data.GetProperty("firstReveal").GetString());
    }

    [Fact]
    public async Task StateOutsideTheScopeOrTheReviewedCasesProjectsNothing()
    {
        var snapshot = Snapshot();
        foreach (var candidate in new[]
        {
            snapshot with { Version = "snapshot-2" },
            snapshot with { Package = ScenarioA1PostRevealPackage.Identity },
            snapshot with { PreviousLocationId = "bd01:E4:0" },
            snapshot with { Terrain = snapshot.Terrain with { Hex = "E5" } },
            snapshot with { Terrain = snapshot.Terrain with { BoardVersion = "wrong" } },
            snapshot with { IsMovementPhase = null },
            snapshot with { FirstRevealLoneSmcUnderA1215 = false },
            snapshot with { HasNoA414Exception = false },
            snapshot with { Election = ScenarioA1OvrElection.Unknown },
            snapshot with { RemainingMf = ScenarioA1OvrRemainingMf.BelowFour },
            snapshot with { Ntc = null },
            snapshot with { OtherConcealedNonDummy = ScenarioA1OtherConcealedNonDummy.Present },
            snapshot with { Ntc = ScenarioA1OvrNtcResult.Passed, OtherConcealedNonDummy = ScenarioA1OtherConcealedNonDummy.Present },
        })
        {
            Assert.Empty((await Provider(candidate).ObserveAsync(Query())).Observations);
        }

        Assert.Empty((await Provider(snapshot).ObserveAsync(Query(extra: true))).Observations);
    }

    private static ScenarioA1OvrNtcObservationProvider Provider(ScenarioA1OvrNtcSnapshot state) => new(new StubSource(state), new Board01TerrainCatalog());

    private static ObservationQuery Query(bool extra = false) => new("query", Tenant, ScenarioA1OvrNtcPackage.Identity,
        new SemanticIdentifier(new DomainId("asl"), ScenarioA1OvrNtcObservationProvider.QueryKind),
        extra
            ? JsonSerializer.SerializeToElement(new { unitId = "squad", locationId = "bd01:E4:0", observationVersion = "snapshot-1", unreviewed = "yes" })
            : JsonSerializer.SerializeToElement(new { unitId = "squad", locationId = "bd01:E4:0", observationVersion = "snapshot-1" }), 1);

    /// <summary>The failed branch: an election with four MF left and a failed NTC.</summary>
    private static ScenarioA1OvrNtcSnapshot Snapshot() => new(
        Tenant, ScenarioA1OvrNtcPackage.Identity, "squad", "bd01:E4:0", "bd01:D4:0", "snapshot-1", Now, "test-supplied-state",
        new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion, Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null),
        true, true, true, true, true, true, true, true,
        ScenarioA1OvrElection.Elected, ScenarioA1OvrRemainingMf.AtLeastFour, ScenarioA1OvrNtcResult.Failed, null, null);

    private sealed class StubSource(ScenarioA1OvrNtcSnapshot snapshot) : IScenarioA1OvrNtcSnapshotSource
    {
        public ValueTask<ScenarioA1OvrNtcSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1OvrNtcSnapshot?>(snapshot);
    }
}
