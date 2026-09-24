using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SecondDefenderConsequenceObservationTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private const string SmcCase = "A1-second-defender-consequence-smc-revealed";

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
    public async Task SuppliedAttemptAndSecondRevealProjectOnlyExactCase(
        ScenarioA1SecondDefenderState state, ScenarioA1OverrunNtc ntc,
        ScenarioA1OverrunMf mf, string suffix)
    {
        var snapshot = Snapshot() with
        {
            Eligibility = Snapshot().Eligibility with
            {
                SecondDefenderState = state,
                SecondRevealOrdinal = state == ScenarioA1SecondDefenderState.KnownSmcNotRevealed
                    ? null : 5,
                Ntc = ntc, NtcOrdinal = ntc == ScenarioA1OverrunNtc.Unresolved ? null : 1,
                MfAtSecondReveal = mf,
            },
        };
        var caseId = "A1-second-defender-consequence-" + suffix;
        var result = await Provider(snapshot).ObserveAsync(Query(caseId));
        var observation = Assert.Single(result.Observations);
        Assert.Equal(ScenarioA1SecondDefenderConsequencePackage.Identity,
            observation.DomainPackage);
        Assert.Equal("snapshot-1", observation.Version);
        Assert.Equal("bd01:D4:0", observation.Data.GetProperty("previousLocationId").GetString());
        Assert.Equal("ordinaryBuildingTwoMf",
            observation.Data.GetProperty("attemptedEntryMf").GetString());
        Assert.Equal(15, observation.Data.EnumerateObject().Count());
        Assert.Equal("asl.a1.second-defender-consequence.exact-case:" + caseId,
            Assert.Single(result.ReasonCodes));
        if (caseId != SmcCase)
            Assert.Empty((await Provider(snapshot).ObserveAsync(Query(SmcCase))).Observations);
    }

    [Fact]
    public async Task StaleWrongOrderWrongLocationOrResolvedOvrCannotProject()
    {
        var state = Snapshot();
        Assert.Single((await Provider(state).ObserveAsync(Query(SmcCase))).Observations);
        foreach (var candidate in new[]
        {
            state with { PreviousLocationId = "bd01:E4:0" },
            state with { PreviousLocationId = "bd01::0" },
            state with { IsPreviousLocationLastOccupied = null },
            state with { AttemptedEntryMf = 4 },
            state with { AttemptOrdinal = 3 },
            state with { OvrEntryResolved = true },
            state with { Package = ScenarioA1SecondDefenderPackage.Identity },
            state with { Eligibility = state.Eligibility with { Package = state.Package } },
            state with { Eligibility = state.Eligibility with { Version = "snapshot-2" } },
            state with { Eligibility = state.Eligibility with { SecondRevealOrdinal = 4 } },
            state with { Eligibility = state.Eligibility with { NtcOrdinal = 6 } },
            state with { Eligibility = state.Eligibility with
                { Terrain = state.Eligibility.Terrain with { BoardVersion = "wrong" } } },
        })
            Assert.Empty((await Provider(candidate).ObserveAsync(Query(SmcCase))).Observations);
        Assert.Empty((await Provider(state).ObserveAsync(Query(SmcCase,
            ScenarioA1SecondDefenderPackage.Identity))).Observations);
        Assert.Empty((await Provider(state).ObserveAsync(Query(SmcCase, previous: "bd01:D5:0")))
            .Observations);
        Assert.Empty((await Provider(state).ObserveAsync(Query("invalid"))).Observations);
    }

    private static ScenarioA1SecondDefenderConsequenceObservationProvider Provider(
        ScenarioA1SecondDefenderConsequenceSnapshot state) =>
        new(new StubSource(state), new Board01TerrainCatalog());

    private static ObservationQuery Query(string caseId, DomainPackageRef? package = null,
        string previous = "bd01:D4:0") => new("query", Tenant,
        package ?? ScenarioA1SecondDefenderConsequencePackage.Identity,
        new SemanticIdentifier(new DomainId("asl"),
            ScenarioA1SecondDefenderConsequenceObservationProvider.QueryKind),
        JsonSerializer.SerializeToElement(new
        {
            unitId = "squad", locationId = "bd01:E4:0", previousLocationId = previous,
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

    private sealed class StubSource(ScenarioA1SecondDefenderConsequenceSnapshot snapshot)
        : IScenarioA1SecondDefenderConsequenceSnapshotSource
    {
        public ValueTask<ScenarioA1SecondDefenderConsequenceSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1SecondDefenderConsequenceSnapshot?>(snapshot);
    }
}
