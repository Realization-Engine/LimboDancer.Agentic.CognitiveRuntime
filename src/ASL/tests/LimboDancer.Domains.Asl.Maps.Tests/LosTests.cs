using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// Synthetic checks of the LOS geometry and read (LOS Design, section 5), runnable without VASL. The full proof is U14
/// against VASL's own <c>Map.LOS</c> (Maps.Vasl.Tests, local only); these tests pin the behavior in CI.
/// </summary>
public sealed class LosTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    private static readonly BoardRef Board = BoardRef.Parse("bd01");

    private static readonly TerrainCatalog Catalog = new(
    [
        new TerrainType { Code = 0, Name = "Open Ground", Category = LosCategory.Open },
        new TerrainType
        {
            Code = 24,
            Name = "Grain",
            Category = LosCategory.Other,
            IsLosHindrance = true,
            IsLowerLosHindrance = true,
            IsHalfLevelHeight = true,
        },
        new TerrainType
        {
            Code = 60,
            Name = "Woods",
            Category = LosCategory.Woods,
            IsLosObstacle = true,
            IsLowerLosObstacle = true,
            Height = 1,
        },
    ]);

    // E4's center point is (225, 225); this box covers the whole hex.
    private static readonly (int X0, int Y0, int X1, int Y1) E4Box = (188, 194, 263, 258);

    [Fact]
    public void TheExtendedBorderMovesEachTruncatedVertexOutward()
    {
        var locator = new VaslHexLocator(Geometry, runtime: true);
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        Assert.Equal(
            [new GridPoint(205, 192), new GridPoint(244, 192), new GridPoint(263, 225), new GridPoint(244, 259), new GridPoint(205, 259), new GridPoint(186, 225)],
            locator.ExtendedBorder(e4));
        Assert.True(locator.ExtendedBorderContains(e4, 225, 225));
        Assert.True(locator.ExtendedBorderContains(e4, 225, 193));
        Assert.False(locator.BorderContains(e4, 225, 193));
        Assert.False(locator.ExtendedBorderContains(e4, 225, 300));
    }

    [Fact]
    public void SixtyDegreeLinesFollowVaslsRangeTolerance()
    {
        var center = new GridPoint(225, 225);
        Assert.True(new LosLine(center, new GridPoint(281, 225)).IsHorizontal);
        Assert.False(new LosLine(center, new GridPoint(281, 226)).IsHorizontal);

        // tan 60 is 1.7320...: 97 / 56 = 1.7321 is on a hexspine at any range; 93 / 56 = 1.6607 at none.
        Assert.True(new LosLine(center, new GridPoint(281, 322)).Is60Degree(2));
        Assert.True(new LosLine(center, new GridPoint(169, 128)).Is60Degree(20));
        Assert.False(new LosLine(center, new GridPoint(281, 318)).Is60Degree(2));

        // 1.77 is within 0.05 at short range, but not within 0.03 from range 5.
        Assert.True(new LosLine(new GridPoint(0, 0), new GridPoint(100, 177)).Is60Degree(4));
        Assert.False(new LosLine(new GridPoint(0, 0), new GridPoint(100, 177)).Is60Degree(5));
        Assert.Equal((0.05, 0.03, 0.03, 0.015), (LosLine.Tolerance(4), LosLine.Tolerance(5), LosLine.Tolerance(15), LosLine.Tolerance(16)));
    }

    [Fact]
    public void SegmentsIntersectAsJavaLine2DDoes()
    {
        Assert.True(LosLine.SegmentsIntersect(0, 0, 10, 10, 0, 10, 10, 0));
        Assert.True(LosLine.SegmentsIntersect(0, 0, 10, 0, 10, 0, 20, 5));
        Assert.True(LosLine.SegmentsIntersect(0, 0, 10, 0, 5, 0, 15, 0));
        Assert.False(LosLine.SegmentsIntersect(0, 0, 10, 0, 11, 0, 15, 0));
        Assert.False(LosLine.SegmentsIntersect(0, 0, 10, 0, 0, 1, 10, 1));
    }

    [Fact]
    public void TheNearestLocationAndTheHexsidesALineCrossesComeFromTheHexsidePoints()
    {
        var map = Map(Paint());
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        var southEast = Assert.IsType<HexIndex>(Geometry.Neighbor(e4, HexsideDirection.SouthEast));
        Assert.Null(map.NearestLocation(e4, 225, 225));
        var edge = map.EdgePoint(e4, HexsideDirection.SouthEast);
        Assert.Equal(HexsideDirection.SouthEast, map.NearestLocation(e4, edge.X, edge.Y));
        Assert.Equal(HexsideDirection.North, map.NearestHexside(e4, 225, 200, HexsideDirection.North, HexsideDirection.South));
        Assert.Equal([HexsideDirection.SouthEast], map.HexsidesCrossed(e4, map.LosPoint(e4), map.LosPoint(southEast)));
    }

    [Fact]
    public void OpenGroundIsClearAndWoodsBetweenBlock()
    {
        var e2 = Location("E2");
        var e6 = Location("E6");
        var clear = LosCalculator.Check(Map(Paint()), e2, e6);
        Assert.Equal((LosStatus.Clear, false, 4, 0), (clear.Status, clear.IsBlocked, clear.Range, clear.Hindrance));

        var blocked = LosCalculator.Check(Map(Paint((E4Box, "Woods"))), e2, e6);
        Assert.Equal((LosStatus.Blocked, true, 4), (blocked.Status, blocked.IsBlocked, blocked.Range));
        Assert.Equal("Terrain is higher than both the source and target (A6.2)", blocked.Reason);
        Assert.Equal((Board, HexName.Parse("E4")), (blocked.BlockedAt!.Board, blocked.BlockedAt.Hex));
    }

    [Fact]
    public void HalfLevelHindrancesAreCountedOncePerRange()
    {
        var result = LosCalculator.Check(Map(Paint((E4Box, "Grain"))), Location("E2"), Location("E6"));
        Assert.Equal((LosStatus.Clear, 1), (result.Status, result.Hindrance));
    }

    [Fact]
    public void ABoardWithoutLosDataOrVerifiedTerrainIsNotDefinitive()
    {
        var grid = Paint((E4Box, "Woods"));
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None);
        var bare = new BoardHandle(Board, "v1", BoardReadStatus.Verified, "synthetic", facts);
        Assert.Equal("MAP-LOS-001", Assert.Single(LosMap.ForBoard(bare).Diagnostics).Code);

        var authored = new BoardHandle(Board, "v1", BoardReadStatus.Authored, "synthetic", facts) { Los = new LosData(grid, Catalog) };
        var map = Assert.IsType<LosMap>(LosMap.ForBoard(authored).Map);
        var result = LosCalculator.Check(map, Location("E2"), Location("E6"));
        Assert.Equal((LosStatus.Nondefinitive, true), (result.Status, result.IsBlocked));
    }

    [Fact]
    public void LocationsOffTheMapOrOutsideTheChainAreRefused()
    {
        var map = Map(Paint());
        Assert.Throws<ArgumentException>(() => LosCalculator.Check(map, Location("E2"), BoardLocation.Parse("bd01:E4:1")));
        Assert.Throws<ArgumentException>(() => LosCalculator.Check(map, Location("E2"), BoardLocation.Parse("bd02:E4:0")));
        Assert.Throws<ArgumentException>(() => LosCalculator.Check(map, Location("E2"), BoardLocation.Parse("bd01:E4:0/1")));
    }

    private static BoardLocation Location(string hex) => new(Board, HexName.Parse(hex), 0);

    private static LosMap Map(TerrainGrid grid) =>
        LosMap.ForGrid(Board, grid, VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None), Catalog);

    private static TerrainGrid Paint(params ((int X0, int Y0, int X1, int Y1) Area, string Terrain)[] areas)
    {
        var codes = new byte[Geometry.GridWidth * Geometry.GridHeight];
        foreach (var ((x0, y0, x1, y1), terrain) in areas)
        {
            var code = Catalog[terrain].Code;
            for (var x = x0; x < x1; x++)
            {
                for (var y = y0; y < y1; y++)
                {
                    codes[(x * Geometry.GridHeight) + y] = code;
                }
            }
        }

        return new TerrainGrid(Geometry, codes, new sbyte[codes.Length], new bool[Geometry.HexCount]);
    }
}
