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
        new TerrainType { Code = 2, Name = "Rooftop", Category = LosCategory.Open },
        new TerrainType
        {
            Code = 41,
            Name = "Stone Building, 1 Level",
            Category = LosCategory.Building,
            IsLosObstacle = true,
            IsLowerLosObstacle = true,
            IsHalfLevelHeight = true,
            Height = 1,
        },
        new TerrainType
        {
            Code = 72,
            Name = "Wall",
            Category = LosCategory.Hexside,
            IsLosObstacle = true,
            IsLowerLosObstacle = true,
            IsHalfLevelHeight = true,
        },
        new TerrainType
        {
            Code = 174,
            Name = "Cellar",
            Category = LosCategory.Building,
            IsLosObstacle = true,
            IsLowerLosObstacle = true,
            Height = 1,
        },
        new TerrainType { Code = 30, Name = "Gully", Category = LosCategory.Depression },
        new TerrainType { Code = 75, Name = "Cliff", Category = LosCategory.Hexside },
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
    public void ACellarCountsOneLevelHigherAndCannotSeeOverHexsideTerrain()
    {
        // E2 is a one-level building, so it has a cellar at level -1. Map.checkTerrainHeightRule counts the cellar one
        // level higher, so the open ground between is level with both ends and does not block.
        var cellar = new BoardLocation(Board, HexName.Parse("E2"), -1);
        var building = (Around("E2"), "Stone Building, 1 Level");
        var clear = LosCalculator.Check(Map(Paint(building)), cellar, Location("E6"));
        Assert.Equal((LosStatus.Clear, false, 4), (clear.Status, clear.IsBlocked, clear.Range));

        // A wall in E4, two hexes from each end: Map.checkHexsideTerrainRule (O6.3).
        var blocked = LosCalculator.Check(Map(Paint(building, ((215, 240, 235, 244), "Wall"))), cellar, Location("E6"));
        Assert.Equal((LosStatus.Blocked, true), (blocked.Status, blocked.IsBlocked));
        Assert.Equal("Unit in cellar cannot see over hexside terrain to non-adjacent target (O6.3)", blocked.Reason);
        Assert.Equal((Board, HexName.Parse("E4")), (blocked.BlockedAt!.Board, blocked.BlockedAt.Hex));

        var seen = LosCalculator.Check(Map(Paint(building, ((215, 240, 235, 244), "Wall"))), Location("E6"), cellar);
        Assert.Equal("Unit in cellar cannot be seen over hexside terrain by non-adjacent target (O6.3)", seen.Reason);
    }

    [Fact]
    public void ARooftopCountsHalfALevelLower()
    {
        // E2's rooftop is level 2 of a one-level building, so the rules count it at 1.5. A one-level building in E4 is
        // then exactly as high as the rooftop (1 plus its half level), which blocks by Map.checkTerrainHeightRule; at 2
        // the rooftop would see over it.
        var rooftop = new BoardLocation(Board, HexName.Parse("E2"), 2);
        var map = Map(Paint((Around("E2"), "Stone Building, 1 Level"), (Around("E4"), "Stone Building, 1 Level")));
        Assert.Equal("Rooftop", map.FactsOf(Geometry.IndexOf(HexName.Parse("E2"))).Locations[^1].Terrain?.Name);
        var result = LosCalculator.Check(map, rooftop, Location("E6"));
        Assert.Equal((LosStatus.Blocked, true, 4), (result.Status, result.IsBlocked, result.Range));
        Assert.Equal("Must have a height advantage to see over this terrain (A6.2)", result.Reason);
        Assert.Equal((Board, HexName.Parse("E4")), (result.BlockedAt!.Board, result.BlockedAt.Hex));
    }

    [Fact]
    public void LosMustLeaveAGullyOnlyWhenTheRangeRestrictionIsMet()
    {
        // E2 is a gully hex at level -1; the rest of the column is open ground at level 0. A target one level higher at
        // range 4 is not higher by the range, so Map.checkDepressionRule blocks the LOS where it leaves the depression,
        // in E3 (A6.3).
        var map = Map(PaintLevels((Whole("E2"), "Gully", -1)));
        var e2 = map.FactsOf(Geometry.IndexOf(HexName.Parse("E2")));
        Assert.Equal((-1, "Gully"), (e2.BaseLevel, e2.Center.DepressionTerrain?.Name));
        var exits = LosCalculator.Check(map, Location("E2"), Location("E6"));
        Assert.Equal((LosStatus.Blocked, true, 4), (exits.Status, exits.IsBlocked, exits.Range));
        Assert.Equal("Exits depression before range/elevation restrictions are satisfied (A6.3)", exits.Reason);
        Assert.Equal((Board, HexName.Parse("E3")), (exits.BlockedAt!.Board, exits.BlockedAt.Hex));

        // A target on a level 3 hill is higher by the range, so the LOS may leave the depression.
        var hill = LosCalculator.Check(Map(PaintLevels((Whole("E2"), "Gully", -1), (Whole("E6"), "Open Ground", 3))), Location("E2"), Location("E6"));
        Assert.Equal((LosStatus.Clear, false, 4), (hill.Status, hill.IsBlocked, hill.Range));
    }

    [Fact]
    public void ACliffHexsideMakesTheHexBelowItBlind()
    {
        // E2 is a level 2 hill, E3 and E4 level 1, and a cliff on E4's south hexside drops to E5 at level 0. The level 1
        // crest alone does not hide E5 from E2, but a cliff hexside crossed next to the lower end does:
        // Map.checkBlindHexRule tests every cliff pixel and Map.isBlindHex leaves at least one blind hex (B10.23).
        var e4 = Geometry.CenterPoint(Geometry.IndexOf(HexName.Parse("E4")));
        var hills = new[] { (Whole("E2"), "Open Ground", 2), (Whole("E3"), "Open Ground", 1), (Whole("E4"), "Open Ground", 1) };
        var cliff = ((e4.X - 20, e4.Y + 26, e4.X + 20, e4.Y + 37), "Cliff", 1);

        var crest = LosCalculator.Check(Map(PaintLevels(hills)), Location("E2"), Location("E5"));
        Assert.Equal((LosStatus.Clear, false, 3), (crest.Status, crest.IsBlocked, crest.Range));

        var map = Map(PaintLevels([.. hills, cliff]));
        Assert.True(map.FactsOf(Geometry.IndexOf(HexName.Parse("E4"))).Hexsides[(int)HexsideDirection.South].Cliff);
        var blind = LosCalculator.Check(map, Location("E2"), Location("E5"));
        Assert.Equal((LosStatus.Blocked, true, 3), (blind.Status, blind.IsBlocked, blind.Range));
        Assert.Equal("Source or Target location is in a blind hex (B10.23)", blind.Reason);
        Assert.Equal((Board, HexName.Parse("E4")), (blind.BlockedAt!.Board, blind.BlockedAt.Hex));
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

    // A box well inside the hex, around its center point.
    private static (int X0, int Y0, int X1, int Y1) Around(string hex)
    {
        var center = Geometry.CenterPoint(Geometry.IndexOf(HexName.Parse(hex)));
        return (center.X - 25, center.Y - 25, center.X + 25, center.Y + 25);
    }

    // A box a little larger than the hex's extended border, so the whole hex takes the terrain and level.
    private static (int X0, int Y0, int X1, int Y1) Whole(string hex)
    {
        var center = Geometry.CenterPoint(Geometry.IndexOf(HexName.Parse(hex)));
        return (center.X - 40, center.Y - 34, center.X + 40, center.Y + 36);
    }

    private static LosMap Map(TerrainGrid grid) =>
        LosMap.ForGrid(Board, grid, VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None), Catalog);

    private static TerrainGrid Paint(params ((int X0, int Y0, int X1, int Y1) Area, string Terrain)[] areas) =>
        PaintLevels([.. areas.Select(area => (area.Area, area.Terrain, 0))]);

    // Areas painted in order, each with its terrain and ground level; the rest is open ground at level 0.
    private static TerrainGrid PaintLevels(params ((int X0, int Y0, int X1, int Y1) Area, string Terrain, int Level)[] areas)
    {
        var codes = new byte[Geometry.GridWidth * Geometry.GridHeight];
        var levels = new sbyte[codes.Length];
        foreach (var ((x0, y0, x1, y1), terrain, level) in areas)
        {
            var code = Catalog[terrain].Code;
            for (var x = x0; x < x1; x++)
            {
                for (var y = y0; y < y1; y++)
                {
                    codes[(x * Geometry.GridHeight) + y] = code;
                    levels[(x * Geometry.GridHeight) + y] = (sbyte)level;
                }
            }
        }

        return new TerrainGrid(Geometry, codes, levels, new bool[Geometry.HexCount]);
    }
}
