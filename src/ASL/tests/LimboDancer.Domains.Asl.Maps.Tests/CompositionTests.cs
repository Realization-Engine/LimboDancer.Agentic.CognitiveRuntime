using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// Maps built from placed boards (VASL Board Ingestion Design, section 11) on synthetic 5 by 2 boards. The VASL-backed
/// oracle scenarios in the Vasl tests check the same code against VASL itself.
/// </summary>
public sealed class CompositionTests
{
    private static readonly BoardGeometry Small = BoardGeometry.Standard(5, 2);

    private static readonly TerrainCatalog Catalog = new(
    [
        Type(0, "Open Ground", LosCategory.Open),
        Type(2, "Rooftop", LosCategory.Open),
        Type(35, "Brush", LosCategory.Other),
        Type(36, "Bamboo", LosCategory.Other),
        Type(40, "Stone Building", LosCategory.Building, obstacle: true, height: 1),
        Type(60, "Woods", LosCategory.Woods, obstacle: true),
        Type(61, "Light Jungle", LosCategory.Woods, obstacle: true),
        Type(90, "Grain", LosCategory.Other),
        Type(174, "Cellar", LosCategory.Building, height: 1),
    ]);

    private static readonly LosSsRuleSet Rules = new(
    [
        new LosSsRule("BrushToOpenGround", LosSsRuleKind.TerrainMap, "Brush", "Open Ground"),
        new LosSsRule("Level2ToLevel1", LosSsRuleKind.ElevationMap, "2", "1"),
        new LosSsRule("WoodsToLevel1", LosSsRuleKind.TerrainToElevationMap, "Woods", "1"),
        new LosSsRule("GrainToLevel1", LosSsRuleKind.TerrainToElevationMap, "Grain", "1"),
        new LosSsRule("Level1ToGrain", LosSsRuleKind.ElevationToTerrainMap, "1", "Grain"),
        new LosSsRule("Woods1ToLevel1", LosSsRuleKind.TerrainToSelectElevationMap, "Woods", "1"),
        new LosSsRule("NoWhiteHexIDs", LosSsRuleKind.Ignore, string.Empty, string.Empty),
        new LosSsRule("NoStairwells", LosSsRuleKind.CustomCode, string.Empty, string.Empty),
        new LosSsRule("Bamboo", LosSsRuleKind.CustomCode, string.Empty, string.Empty),
        new LosSsRule("Teleport", LosSsRuleKind.CustomCode, string.Empty, string.Empty),
        new LosSsRule("ScrubToMud", LosSsRuleKind.TerrainMap, "Scrub", "Mud"),
        new LosSsRule("GrainToMud", LosSsRuleKind.TerrainMap, "Grain", "Mud"),
    ]);

    private static readonly BoardRef First = BoardRef.Parse("bd01");
    private static readonly BoardRef Second = BoardRef.Parse("bd02");

    [Fact]
    public void ASingleBoardWithoutRulesDerivesAsTheBoardDoes()
    {
        var grid = Grid((x, y) => x > 100 && y < 50 ? (byte)60 : (byte)0, stairways: hex => hex.Column == 1);
        var map = Build(Place(First, grid));
        Assert.Equal(grid.Codes.ToArray(), map.Grid.Codes.ToArray());
        var expected = VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None);
        Assert.Equal(expected.Hexes.Select(Describe), map.Facts.Hexes.Select(Describe));
    }

    [Fact]
    public void BoardsSideBySideShareAnEdgeColumnAndTheSeamPrefersNonOpenTerrain()
    {
        // The first board's last column is woods above y = 60; the second board's first column is grain below y = 90.
        var left = Grid((x, y) => x == Small.GridWidth - 1 && y < 60 ? (byte)60 : (byte)0);
        var right = Grid((x, y) => x == 0 && y >= 90 ? (byte)90 : (byte)0);
        var map = Build(Place(First, left), Place(Second, right, column: 1));

        Assert.Equal((9, 2, 450, 129), (map.Geometry.WidthInHexes, map.Geometry.HeightInHexes, map.Geometry.GridWidth, map.Geometry.GridHeight));
        Assert.Equal(60, map.Grid.CodeAt(225, 10));
        Assert.Equal(0, map.Grid.CodeAt(225, 70));
        Assert.Equal(90, map.Grid.CodeAt(225, 100));
        Assert.Equal(0, map.Grid.CodeAt(226, 10));

        // The shared column: the second board placed later names it and owns it.
        var shared = Assert.IsType<HexIndex>(map.Locate(First, HexName.Parse("E1")));
        Assert.Equal(shared, map.Locate(Second, HexName.Parse("A1")));
        Assert.Equal((Second, HexName.Parse("A1")), map.OwnerOf(shared));
        Assert.Equal(HexName.Parse("A1"), map.Facts[shared].Hex);
        Assert.Equal(new HexIndex(8, 1), map.Locate(Second, HexName.Parse("E2")));
    }

    [Fact]
    public void BoardsStackedVerticallyMergeTheirSeamRowBothWays()
    {
        var top = Grid((x, y) => y == Small.GridHeight - 1 && x < 20 ? (byte)60 : (byte)0);
        var bottom = Grid((x, y) => y == 0 && x >= 20 && x < 40 ? (byte)90 : (byte)0);
        var map = Build(Place(First, top), Place(Second, bottom, row: 1));
        Assert.Equal((5, 4, 225, 258), (map.Geometry.WidthInHexes, map.Geometry.HeightInHexes, map.Geometry.GridWidth, map.Geometry.GridHeight));
        Assert.Equal(60, map.Grid.CodeAt(10, 129));
        Assert.Equal(90, map.Grid.CodeAt(30, 129));
        Assert.Equal(0, map.Grid.CodeAt(50, 129));
    }

    [Fact]
    public void AReversedBoardIsRotatedWithItsNamesAndStairways()
    {
        var grid = Grid((x, y) => x < 10 && y < 10 ? (byte)60 : (byte)0, stairways: hex => hex == new HexIndex(0, 0));
        var map = Build(Place(First, grid, reversed: true));

        Assert.Equal(60, map.Grid.CodeAt(Small.GridWidth - 1, Small.GridHeight - 1));
        Assert.Equal(0, map.Grid.CodeAt(0, 0));

        // A1 (column 0, row 0) lands in the last column's last hex; E2 lands top left.
        var a1 = Assert.IsType<HexIndex>(map.Locate(First, HexName.Parse("A1")));
        Assert.Equal(new HexIndex(4, 1), a1);
        Assert.Equal(HexName.Parse("A1"), map.Facts[a1].Hex);
        Assert.True(map.Facts[a1].Stairway);
        Assert.Equal(HexName.Parse("E2"), map.Facts[new HexIndex(0, 0)].Hex);
        Assert.Equal(new HexIndex(0, 0), map.Locate(First, HexName.Parse("E2")));
    }

    [Theory]
    [InlineData("four across")]
    [InlineData("gap")]
    [InlineData("same slot")]
    [InlineData("hex size")]
    [InlineData("empty")]
    public void LayoutsVaslCannotBuildAreRefused(string layout)
    {
        var grid = Grid((_, _) => 0);
        PlacedBoard[] boards = layout switch
        {
            "four across" => [.. Enumerable.Range(0, 4).Select(column => Place(BoardRef.Parse($"bd0{column + 1}"), grid, column: column))],
            "gap" => [Place(First, grid), Place(Second, grid, column: 2)],
            "same slot" => [Place(First, grid), Place(Second, grid)],
            "hex size" => [Place(First, grid), Place(Second, new TerrainGrid(BoardGeometry.Vasl(5, 2, 168.7857142857, 194.2, 676, 971),
                new byte[676 * 971], new sbyte[676 * 971], new bool[12]), column: 1)],
            _ => [],
        };
        var result = VaslMapBuilder.Build(boards, Catalog, Rules);
        Assert.Null(result.Map);
        var expected = layout switch
        {
            "four across" or "gap" => "VASL-MAP-003",
            "hex size" => "VASL-MAP-002",
            _ => "VASL-MAP-001",
        };
        Assert.Equal(expected, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void TerrainAndElevationRulesRewriteTheWholeGrid()
    {
        var grid = Grid((x, _) => x switch { < 20 => 35, < 40 => 60, < 60 => 90, _ => 0 }, (x, _) => x is >= 60 and < 80 ? (sbyte)2 : (sbyte)0);
        var map = Build(Place(First, grid, rules: ["BrushToOpenGround", "Level2ToLevel1", "NoWhiteHexIDs", "NoSuchRule", "ScrubToMud"]));
        Assert.Equal((0, 60, 90), (map.Grid.CodeAt(10, 5), map.Grid.CodeAt(30, 5), map.Grid.CodeAt(50, 5)));
        Assert.Equal(1, map.Grid.ElevationAt(70, 5));
    }

    [Fact]
    public void TerrainToElevationTurnsTheTerrainIntoRaisedOpenGround()
    {
        // Open cells amid grain stay at level 0: VASL's repair for them never fires (VASLBoard's "=+1").
        var grid = Grid((x, y) => x is >= 20 and < 60 && !(x == 40 && y == 40) ? (byte)90 : (byte)0);
        var map = Build(Place(First, grid, rules: ["GrainToLevel1"]));
        Assert.Equal((0, 1), (map.Grid.CodeAt(30, 30), map.Grid.ElevationAt(30, 30)));
        Assert.Equal(0, map.Grid.ElevationAt(40, 40));
    }

    [Fact]
    public void ElevationToTerrainSparesLosObstacles()
    {
        var grid = Grid((x, _) => x < 20 ? (byte)60 : (byte)0, (x, _) => x < 40 ? (sbyte)1 : (sbyte)0);
        var map = Build(Place(First, grid, rules: ["Level1ToGrain"]));
        Assert.Equal((60, 0), (map.Grid.CodeAt(10, 5), map.Grid.ElevationAt(10, 5)));
        Assert.Equal((90, 0), (map.Grid.CodeAt(30, 5), map.Grid.ElevationAt(30, 5)));
    }

    [Fact]
    public void SelectElevationRulesClearOnlyThatLevel()
    {
        var grid = Grid((x, _) => x < 40 ? (byte)60 : (byte)0, (x, _) => x < 20 ? (sbyte)1 : (sbyte)0);
        var map = Build(Place(First, grid, rules: ["Woods1ToLevel1"]));
        Assert.Equal(0, map.Grid.CodeAt(10, 5));
        Assert.Equal(60, map.Grid.CodeAt(30, 5));
    }

    [Fact]
    public void CustomRulesClearStairwaysAndImplyLightJungle()
    {
        var grid = Grid((x, _) => x switch { < 20 => 35, < 40 => 60, _ => 0 }, stairways: _ => true);
        var map = Build(Place(First, grid, rules: ["NoStairwells", "Bamboo"]));
        Assert.All(map.Facts.Hexes, hex => Assert.False(hex.Stairway));
        Assert.Equal((36, 61), (map.Grid.CodeAt(10, 5), map.Grid.CodeAt(30, 5)));
    }

    [Theory]
    [InlineData("Teleport", "VASL-SSR-002")]
    [InlineData("GrainToMud", "VASL-SSR-001")]
    public void RulesVaslCannotApplyStopTheBuild(string rule, string code)
    {
        var grid = Grid((x, _) => x < 20 ? (byte)90 : (byte)0);
        var result = VaslMapBuilder.Build([Place(First, grid, rules: [rule])], Catalog, Rules);
        Assert.Null(result.Map);
        Assert.Equal(code, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void RuleSetsKeepTheFirstPositionAndTheLastDefinition()
    {
        var rules = new LosSsRuleSet(
        [
            new LosSsRule("MarshToWater", LosSsRuleKind.TerrainMap, "Marsh", "Water"),
            new LosSsRule("Other", LosSsRuleKind.Ignore, string.Empty, string.Empty),
            new LosSsRule("MarshToWater", LosSsRuleKind.TerrainToElevationMap, "Marsh", "-1"),
        ]);
        Assert.Equal(["MarshToWater", "Other"], rules.Rules.Select(rule => rule.Name));
        Assert.Equal(LosSsRuleKind.TerrainToElevationMap, rules["MarshToWater"].Kind);
    }

    [Fact]
    public void AnnotationsGoToTheFirstHexWithTheNameAndReplaceItsFlags()
    {
        var geometry = BoardGeometry.Vasl(9, 2, 56.25, 64.5, 450, 129);
        var map = new VaslMapDerivation(geometry, Catalog);
        map.Rename(new HexIndex(6, 0), HexName.Parse("A1"));
        Assert.Equal(new HexIndex(0, 0), map.Find(HexName.Parse("A1")));

        var slopes = new Dictionary<HexName, IReadOnlySet<HexsideDirection>> { [HexName.Parse("A1")] = new HashSet<HexsideDirection> { HexsideDirection.North } };
        map.ApplyAnnotations(new HexsideAnnotations(slopes, new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(), new Dictionary<HexName, IReadOnlySet<HexsideDirection>>()));
        slopes[HexName.Parse("A1")] = new HashSet<HexsideDirection> { HexsideDirection.South };
        map.ApplyAnnotations(new HexsideAnnotations(slopes, new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(), new Dictionary<HexName, IReadOnlySet<HexsideDirection>>()));
        var grid = new VaslMapGrid(geometry).Snapshot();
        map.Pass(grid);
        var facts = map.Facts();
        Assert.Equal([false, false, false, true, false, false], facts[new HexIndex(0, 0)].Hexsides.Select(side => side.Slope));
        Assert.All(facts[new HexIndex(6, 0)].Hexsides, side => Assert.False(side.Slope));
    }

    [Fact]
    public void PlacementsHaveACompactForm()
    {
        Assert.Equal("01@0,0", new BoardPlacement(First).ToString());
        Assert.Equal("1b@1,0/r[NoStairwells,Level_1ToLevel0]",
            new BoardPlacement(BoardRef.Parse("bd1b"), 1, 0, true, ["NoStairwells", "Level_1ToLevel0"]).ToString());
    }

    private static VaslMap Build(params PlacedBoard[] boards)
    {
        var result = VaslMapBuilder.Build(boards, Catalog, Rules);
        Assert.Empty(result.Diagnostics);
        return Assert.IsType<VaslMap>(result.Map);
    }

    private static PlacedBoard Place(BoardRef board, TerrainGrid grid, int column = 0, int row = 0, bool reversed = false, string[]? rules = null) =>
        new(new BoardPlacement(board, column, row, reversed, rules ?? []), grid, HexsideAnnotations.None);

    private static TerrainGrid Grid(Func<int, int, byte> code, Func<int, int, sbyte>? elevation = null, Func<HexIndex, bool>? stairways = null)
    {
        var codes = new byte[Small.GridWidth * Small.GridHeight];
        var elevations = new sbyte[codes.Length];
        for (var x = 0; x < Small.GridWidth; x++)
        {
            for (var y = 0; y < Small.GridHeight; y++)
            {
                codes[(x * Small.GridHeight) + y] = code(x, y);
                elevations[(x * Small.GridHeight) + y] = elevation?.Invoke(x, y) ?? 0;
            }
        }

        var flags = new bool[Small.HexCount];
        foreach (var hex in Small.Hexes())
        {
            flags[Small.HexOrdinal(hex)] = stairways?.Invoke(hex) ?? false;
        }

        return new TerrainGrid(Small, codes, elevations, flags);
    }

    private static string Describe(HexFacts facts) =>
        $"{facts.Hex} {facts.Index} {facts.BaseLevel} {facts.Stairway} {facts.Center.Terrain?.Name} "
        + string.Join(",", facts.Locations.Select(location => $"{location.Level}:{location.Terrain?.Name}"))
        + " " + string.Join(",", facts.Hexsides.Select(side => $"{side.Terrain?.Name}/{side.HexsideTerrain?.Name}"));

    private static TerrainType Type(byte code, string name, LosCategory category, bool obstacle = false, int height = 0) =>
        new()
        {
            Code = code,
            Name = name,
            Category = category,
            IsLosObstacle = obstacle,
            Height = height,
        };
}
