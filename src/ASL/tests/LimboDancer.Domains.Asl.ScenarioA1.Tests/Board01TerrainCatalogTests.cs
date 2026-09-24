using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class Board01TerrainCatalogTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Time = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PinnedMetadataDistinguishesExplicitBuildingOverridesFromUnknownHexes()
    {
        var catalog = new Board01TerrainCatalog();
        Assert.Equal(63, catalog.OverrideCount);
        Assert.True(catalog.TryGetBuilding("E4", out var stone));
        Assert.Equal(new Board01Building("stone", 2), stone);
        Assert.True(catalog.TryGetBuilding("P7", out var oneLevel));
        Assert.Equal(new Board01Building("stone", 1), oneLevel);
        Assert.True(catalog.TryGetBuilding("F1", out var wooden));
        Assert.Equal(new Board01Building("wooden", 1), wooden);
        Assert.False(catalog.TryGetBuilding("A1", out _));
        Assert.False(catalog.TryGetBuilding("e4", out _));
    }

    [Fact]
    public async Task PinnedTerrainAndSuppliedVariablesProduceOnlyReviewedCases()
    {
        var snapshot = Snapshot();
        var source = new Board01ValidatedSnapshotSource(new StubSource(snapshot),
            new Board01TerrainCatalog());
        var result = await new ScenarioA1BoardObservationProvider(source).ObserveAsync(Query());
        Assert.Equal("asl.a1.board.exact-case:A1-empty-ordinary-mph",
            Assert.Single(result.ReasonCodes));
        Assert.Single(result.Observations);

        var enemy = snapshot with
        {
            Occupancy = ScenarioA1BoardOccupancy.KnownUnconcealedEnemyMmc,
            IsAdjacentGroundLevelOrdinaryBuilding = null,
        };
        var enemyResult = await new ScenarioA1BoardObservationProvider(
            new Board01ValidatedSnapshotSource(new StubSource(enemy),
                new Board01TerrainCatalog())).ObserveAsync(Query());
        Assert.Equal("asl.a1.board.exact-case:A1-known-enemy-mmc-mph",
            Assert.Single(enemyResult.ReasonCodes));

        var otherPhase = snapshot with { IsMovementPhase = false };
        var otherResult = await new ScenarioA1BoardObservationProvider(
            new Board01ValidatedSnapshotSource(new StubSource(otherPhase),
                new Board01TerrainCatalog())).ObserveAsync(Query());
        Assert.Empty(otherResult.Observations);
    }

    [Fact]
    public async Task ChangedBoardIdentityUnknownHexOverlayOrUnsupportedLevelCannotSupplyTerrain()
    {
        var initial = Snapshot();
        var binding = initial.Terrain!;
        foreach (var state in new[]
        {
            initial with { Terrain = binding with { BoardVersion = "6.8" } },
            initial with { Terrain = binding with { MetadataGitBlobSha = "different" } },
            initial with { Terrain = binding with { Hex = "A1" }, LocationId = "bd01:A1:0" },
            initial with { Terrain = binding with { Hex = "F1" } },
            initial with { Terrain = binding with { Level = 1 }, LocationId = "bd01:E4:1" },
            initial with { Terrain = binding with { Variant = "NoRoads" } },
            initial with { Terrain = null },
            initial with { IsKnownBuildingLocation = null },
            initial with { IsAdjacentGroundLevelOrdinaryBuilding = null },
        })
        {
            var source = new Board01ValidatedSnapshotSource(new StubSource(state),
                new Board01TerrainCatalog());
            Assert.Null(await source.ReadAsync(Tenant, ScenarioA1OccupiedPackage.Identity,
                "squad", state.LocationId));
        }
    }

    private static ScenarioA1BoardSnapshot Snapshot() =>
        new(Tenant, ScenarioA1OccupiedPackage.Identity, "squad", "bd01:E4:0",
            "state-1", Time, "test-snapshot", ScenarioA1BoardOccupancy.KnownEmpty,
            true, true, true, true, true, true, true, true, true, true,
            new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion,
                Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null));

    private static ObservationQuery Query() =>
        new("query", Tenant, ScenarioA1OccupiedPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1BoardObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0", observationVersion = "state-1",
            }), 1);

    private sealed class StubSource(ScenarioA1BoardSnapshot snapshot)
        : IScenarioA1BoardSnapshotSource
    {
        public ValueTask<ScenarioA1BoardSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1BoardSnapshot?>(snapshot);
    }
}
