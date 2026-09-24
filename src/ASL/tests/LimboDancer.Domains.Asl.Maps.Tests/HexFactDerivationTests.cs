using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// Synthetic checks of each derivation step, runnable without VASL. The full proof is F2 against VASL's own classes
/// (Maps.Vasl.Tests, local only); these tests pin the behavior in CI.
/// </summary>
public sealed class HexFactDerivationTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    // E4's center point is (225, 225); painting this square covers every center and probe sample.
    private static readonly (int X0, int Y0, int X1, int Y1) E4Center = (219, 219, 231, 231);

    [Fact]
    public void OpenBoardDerivesOpenGroundEverywhere()
    {
        var facts = Derive(Paint());
        Assert.Equal(346, facts.Hexes.Count);
        Assert.All(facts.Hexes, hex =>
        {
            Assert.Equal("Open Ground", hex.Center.Terrain?.Name);
            Assert.Equal(0, hex.BaseLevel);
            Assert.All(hex.Hexsides, side => Assert.True(side.OnMap));
            Assert.All(hex.Hexsides, side => Assert.Null(side.HexsideTerrain));
        });
    }

    [Fact]
    public void TwoLevelStoneBuildingHasCellarUpperLevelsAndRooftop()
    {
        var e4 = Derive(Paint((E4Center, "Stone Building, 2 Level")))[HexName.Parse("E4")];
        Assert.Equal(
            [(-1, "Cellar"), (0, "Stone Building, 2 Level"), (1, "Stone Building, 2 Level"), (2, "Stone Building, 2 Level"), (3, "Rooftop")],
            Levels(e4));
        Assert.False(e4.Stairway);
        Assert.Equal(CenterTerrainSource.CenterSample, e4.CenterSource);
    }

    [Fact]
    public void OneLevelBuildingsGetAnInherentStairway()
    {
        var e4 = Derive(Paint((E4Center, "Stone Building, 1 Level")))[HexName.Parse("E4")];
        Assert.Equal([(-1, "Cellar"), (0, "Stone Building, 1 Level"), (1, "Stone Building, 1 Level"), (2, "Rooftop")], Levels(e4));
        Assert.True(e4.Stairway);
    }

    [Fact]
    public void GenericBuildingTypesGetNoOtherLocations()
    {
        var e4 = Derive(Paint((E4Center, "Stone Building")))[HexName.Parse("E4")];
        Assert.Equal([(0, "Stone Building")], Levels(e4));
    }

    [Fact]
    public void FactoryWithoutStairwayHasNoUpperLevelOrCellar()
    {
        // The rooftop sits at the factory height plus one. With no upper levels to rebuild, the first pass's rooftop
        // stays linked and the second pass adds another above it; VASL does the same on 57 BFP factory hexes.
        var e4 = Derive(Paint((E4Center, "Stone Factory, 1.5 Level")))[HexName.Parse("E4")];
        Assert.Equal([(0, "Stone Factory, 1.5 Level"), (2, "Rooftop"), (2, "Rooftop")], Levels(e4));
    }

    [Fact]
    public void MarketplaceGroundLevelIsOpenGround()
    {
        var e4 = Derive(Paint((E4Center, "Stone Market Place")))[HexName.Parse("E4")];
        Assert.Equal([(-1, "Cellar"), (0, "Open Ground"), (1, "Stone Market Place"), (2, "Rooftop")], Levels(e4));
    }

    [Fact]
    public void WallAtAnEdgeSampleIsSeenFromBothHexes()
    {
        // E4's north-east sample is (251, 210); F3's south-west sample is (254, 208).
        var facts = Derive(Paint(((250, 207, 256, 212), "Wall")));
        var e4 = facts[HexName.Parse("E4")];
        var f3 = facts[HexName.Parse("F3")];
        Assert.Equal("Wall", e4.Hexsides[1].HexsideTerrain?.Name);
        Assert.Equal("Wall", e4.Hexsides[1].Terrain?.Name);
        Assert.Equal("Wall", f3.Hexsides[4].HexsideTerrain?.Name);
    }

    [Fact]
    public void HedgeFoundOnlyByTheGapCheckReachesTheNeighborOnTheSecondPass()
    {
        // E4's north sample (225, 194) is open, but the gap point toward vertex 0 is (215, 194).
        var facts = Derive(Paint(((215, 194, 216, 195), "Hedge")));
        var e4 = facts[HexName.Parse("E4")];
        Assert.Equal("Hedge", e4.Hexsides[0].HexsideTerrain?.Name);
        Assert.Equal("Open Ground", e4.Hexsides[0].Terrain?.Name);

        // E3 is processed before E4, so it only takes E4's hedge from the second pass.
        Assert.Equal("Hedge", facts[HexName.Parse("E3")].Hexsides[3].HexsideTerrain?.Name);
    }

    [Fact]
    public void DepressionCenterRecordsItsDepression()
    {
        var e4 = Derive(Paint((E4Center, "Shallow Stream")))[HexName.Parse("E4")];
        Assert.Equal("Shallow Stream", e4.Center.Terrain?.Name);
        Assert.Equal("Shallow Stream", e4.Center.DepressionTerrain?.Name);
    }

    [Fact]
    public void InherentTerrainNearestTheCenterBecomesTheCenterTerrain()
    {
        var e4 = Derive(Paint(((224, 225, 225, 226), "Crags")))[HexName.Parse("E4")];
        Assert.Equal("Crags", e4.Center.Terrain?.Name);
    }

    [Fact]
    public void OutOfBoundsTerrainTakesAHexsideOffTheMap()
    {
        var e4 = Derive(Paint(((225, 257, 226, 258), "OutOfBounds")))[HexName.Parse("E4")];
        Assert.False(e4.Hexsides[3].OnMap);
        Assert.True(e4.Hexsides[0].OnMap);
    }

    [Fact]
    public void BridgeOverADepressionAddsABridgeLocation()
    {
        // With no road hexside, the bridge sits one level above the hex's base level.
        var e4 = Derive(Paint((E4Center, "Shallow Stream"), ((240, 230, 241, 231), "Stone Bridge")))[HexName.Parse("E4")];
        Assert.Equal([(0, "Shallow Stream"), (1, "Stone Bridge")], Levels(e4));
        Assert.Equal(new BridgeFacts(Catalog["Stone Bridge"], 1), e4.Bridge);
    }

    [Fact]
    public void BaseLevelComesFromTheNearCenterElevation()
    {
        var e4 = Derive(Paint(), elevation: (x, y) => (x, y) == (226, 224) ? (sbyte)2 : (sbyte)0)[HexName.Parse("E4")];
        Assert.Equal(2, e4.BaseLevel);
    }

    [Fact]
    public void HexsideAnnotationsAreReported()
    {
        var e4Name = HexName.Parse("E4");
        var annotations = new HexsideAnnotations(
            new Dictionary<HexName, IReadOnlySet<HexsideDirection>> { [e4Name] = new HashSet<HexsideDirection> { HexsideDirection.South } },
            new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(),
            new Dictionary<HexName, IReadOnlySet<HexsideDirection>>());
        var e4 = VaslCompatibleHexFactDerivation.Derive(Paint(), Catalog, annotations)[e4Name];
        Assert.True(e4.Hexsides[3].Slope);
        Assert.False(e4.Hexsides[0].Slope);
    }

    private static readonly TerrainCatalog Catalog = new(
    [
        Type(0, "Open Ground", LosCategory.Open),
        Type(2, "Rooftop", LosCategory.Open),
        Type(26, "Crags", LosCategory.Other, inherent: true),
        Type(32, "Shallow Stream", LosCategory.Depression),
        Type(40, "Stone Building", LosCategory.Building, height: 1),
        Type(41, "Stone Building, 1 Level", LosCategory.Building, height: 1),
        Type(42, "Stone Building, 2 Level", LosCategory.Building, height: 2),
        Type(47, "Stone Factory, 1.5 Level", LosCategory.Factory, height: 1),
        Type(49, "Stone Market Place", LosCategory.Marketplace, height: 1),
        Type(60, "Woods", LosCategory.Woods),
        Type(66, "Paved Road", LosCategory.Road),
        Type(72, "Wall", LosCategory.Hexside),
        Type(73, "Hedge", LosCategory.Hexside),
        Type(84, "Stone Bridge", LosCategory.Bridge),
        Type(157, "Rrembankment", LosCategory.Hexside),
        Type(174, "Cellar", LosCategory.Building, height: 1),
        Type(200, "PartialOrchard", LosCategory.Hexside),
        Type(213, "OutOfBounds", LosCategory.Other),
    ]);

    private static TerrainType Type(byte code, string name, LosCategory category, int height = 0, bool inherent = false) =>
        new()
        {
            Code = code,
            Name = name,
            Category = category,
            Height = height,
            IsInherent = inherent
        };

    private static HexFactSet Derive(TerrainGrid grid) => VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None);

    private static HexFactSet Derive(TerrainGrid grid, Func<int, int, sbyte> elevation)
    {
        var elevations = new sbyte[grid.CellCount];
        for (var x = 0; x < Geometry.GridWidth; x++)
        {
            for (var y = 0; y < Geometry.GridHeight; y++)
            {
                elevations[(x * Geometry.GridHeight) + y] = elevation(x, y);
            }
        }

        return Derive(new TerrainGrid(Geometry, grid.Codes, elevations, grid.Stairways));
    }

    private static (int Level, string? Terrain)[] Levels(HexFacts hex) =>
        hex.Locations.Select(location => (location.Level, location.Terrain?.Name)).ToArray();

    // Paints half-open rectangles [X0, X1) by [Y0, Y1) of named terrain on an Open Ground board; later ones win.
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
