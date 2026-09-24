using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SecondDefenderObservationTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private const string SmcCase = "A1-second-defender-smc-revealed";

    [Theory]
    [InlineData(ScenarioA1SecondDefenderState.RevealedSmc,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, "smc-revealed")]
    [InlineData(ScenarioA1SecondDefenderState.RevealedMmc,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, "mmc-revealed")]
    [InlineData(ScenarioA1SecondDefenderState.RevealedOtherNonDummy,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, "other-type")]
    [InlineData(ScenarioA1SecondDefenderState.RevealedUnknownType,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, "unknown-type")]
    [InlineData(ScenarioA1SecondDefenderState.KnownSmcNotRevealed,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, "unrevealed-smc")]
    [InlineData(ScenarioA1SecondDefenderState.RevealedSmc,
        ScenarioA1OverrunNtc.Unresolved, ScenarioA1OverrunMf.Unknown, "capability-unresolved")]
    [InlineData(ScenarioA1SecondDefenderState.RevealedSmc,
        ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.Insufficient, "mf-insufficient")]
    public async Task SuppliedEventsProjectOnlyTheirExactCase(
        ScenarioA1SecondDefenderState state, ScenarioA1OverrunNtc ntc,
        ScenarioA1OverrunMf mf, string suffix)
    {
        var snapshot = Snapshot() with
        {
            SecondDefenderState = state,
            SecondRevealOrdinal = state == ScenarioA1SecondDefenderState.KnownSmcNotRevealed
                ? null : 3,
            Ntc = ntc, NtcOrdinal = ntc == ScenarioA1OverrunNtc.Unresolved ? null : 1,
            MfAtSecondReveal = mf,
        };
        var caseId = "A1-second-defender-" + suffix;
        var provider = Provider(snapshot);
        var result = await provider.ObserveAsync(Query(caseId));
        var observation = Assert.Single(result.Observations);
        Assert.Equal("snapshot-1", observation.Version);
        Assert.Equal(ScenarioA1SecondDefenderPackage.Identity, observation.DomainPackage);
        Assert.Equal("enemySmc-under-A12.15",
            observation.Data.GetProperty("firstReveal").GetString());
        Assert.Equal("elected-after-first-reveal",
            observation.Data.GetProperty("overrunElection").GetString());
        Assert.Equal(13, observation.Data.EnumerateObject().Count());
        Assert.Equal("asl.a1.second-defender.exact-case:" + caseId,
            Assert.Single(result.ReasonCodes));
        if (caseId != SmcCase)
            Assert.Empty((await provider.ObserveAsync(Query(SmcCase))).Observations);
    }

    [Fact]
    public async Task LaterNtcCannotProveCapabilityAtSecondReveal()
    {
        var before = Snapshot();
        Assert.Single((await Provider(before).ObserveAsync(Query(SmcCase))).Observations);
        Assert.Empty((await Provider(before with { NtcOrdinal = 5 })
            .ObserveAsync(Query(SmcCase))).Observations);
    }

    [Fact]
    public async Task MissingConflictingStaleOrForeignEventsCannotProjectEligibility()
    {
        var state = Snapshot();
        foreach (var candidate in new[]
        {
            state with { FirstRevealOrdinal = null },
            state with { ElectionOrdinal = null },
            state with { ElectionOrdinal = 1 },
            state with { SecondRevealOrdinal = null },
            state with { SecondRevealOrdinal = 2 },
            state with { SecondDefenderUnitId = "first-smc" },
            state with { SecondDefenderLocationId = "bd01:D4:0" },
            state with { NtcOrdinal = null },
            state with { NtcOrdinal = 4 },
            state with { Ntc = ScenarioA1OverrunNtc.Failed },
            state with { HasNoSpecialModifier = false },
            state with { A1215FirstRevealOccurred = null },
            state with { InitialOccupancy = ScenarioA1InitialConcealedOccupancy.Hidden },
            state with { Version = "snapshot-2" },
            state with { Package = ScenarioA1ConcealedSmcOverrunPackage.Identity },
            state with { Terrain = state.Terrain with { BoardVersion = "wrong" } },
            state with { Terrain = state.Terrain with { Variant = "NoRoads" } },
        })
            Assert.Empty((await Provider(candidate).ObserveAsync(Query(SmcCase))).Observations);
        Assert.Empty((await Provider(state).ObserveAsync(Query(SmcCase,
            ScenarioA1ConcealedSmcOverrunPackage.Identity))).Observations);
        Assert.Empty((await Provider(state).ObserveAsync(Query(SmcCase, extra: true))).Observations);
    }

    private static ScenarioA1SecondDefenderObservationProvider Provider(
        ScenarioA1SecondDefenderSnapshot state) =>
        new(new StubSource(state), new Board01TerrainCatalog());

    private static ObservationQuery Query(string caseId, DomainPackageRef? package = null,
        bool extra = false) => new("query", Tenant,
        package ?? ScenarioA1SecondDefenderPackage.Identity,
        new SemanticIdentifier(new DomainId("asl"),
            ScenarioA1SecondDefenderObservationProvider.QueryKind),
        extra ? JsonSerializer.SerializeToElement(new
        {
            unitId = "squad", locationId = "bd01:E4:0", observationVersion = "snapshot-1",
            caseId, unreviewed = "yes",
        }) : JsonSerializer.SerializeToElement(new
        {
            unitId = "squad", locationId = "bd01:E4:0", observationVersion = "snapshot-1", caseId,
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

    private sealed class StubSource(ScenarioA1SecondDefenderSnapshot snapshot)
        : IScenarioA1SecondDefenderSnapshotSource
    {
        public ValueTask<ScenarioA1SecondDefenderSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1SecondDefenderSnapshot?>(snapshot);
    }
}
