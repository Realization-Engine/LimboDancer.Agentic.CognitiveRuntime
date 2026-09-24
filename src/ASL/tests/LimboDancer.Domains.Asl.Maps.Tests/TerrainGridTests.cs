using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class TerrainGridTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void HexOrdinalFollowsVaslOrder()
    {
        var ordinal = 0;
        foreach (var hex in Geometry.Hexes())
        {
            Assert.Equal(ordinal++, Geometry.HexOrdinal(hex));
        }

        Assert.Equal(346, ordinal);
    }

    [Fact]
    public void GridStoresCellsColumnMajorAndStairwaysByHex()
    {
        var grid = Build(codeAt: (x, y) => (byte)((x + y) % 7), elevationAt: (x, _) => (sbyte)(x % 3 - 1),
            stairwayAt: hex => hex == new HexIndex(4, 3));
        Assert.Equal(1800 * 645, grid.CellCount);
        Assert.Equal((byte)((10 + 20) % 7), grid.CodeAt(10, 20));
        Assert.Equal(grid.Codes[(10 * 645) + 20], grid.CodeAt(10, 20));
        Assert.Equal((sbyte)(10 % 3 - 1), grid.ElevationAt(10, 20));
        Assert.True(grid.HasStairway(new HexIndex(4, 3)));
        Assert.False(grid.HasStairway(new HexIndex(4, 4)));
        Assert.Equal(Enumerable.Range(0, 7).Select(code => (byte)code), grid.DistinctCodes());
    }

    [Fact]
    public void CellsOffTheGridHaveNoCode()
    {
        var grid = Build();
        Assert.False(grid.TryGetCode(-26, 17, out _));
        Assert.False(grid.TryGetCode(1800, 0, out _));
        Assert.True(grid.TryGetCode(1799, 644, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.CodeAt(0, 645));
    }

    [Fact]
    public void WrongSizedInputsAreRejected()
    {
        var cells = 1800 * 645;
        Assert.Throws<ArgumentException>(() => new TerrainGrid(Geometry, new byte[cells - 1], new sbyte[cells], new bool[346]));
        Assert.Throws<ArgumentException>(() => new TerrainGrid(Geometry, new byte[cells], new sbyte[cells], new bool[345]));
    }

    [Fact]
    public void CenterSampleTakesTheFirstOnGridNearCenterPoint()
    {
        // E4's center point is (225, 225); the first probe is (226, 224).
        var grid = Build(codeAt: (x, y) => (x, y) == (226, 224) ? Woods : Open);
        var sample = CenterTerrainSampler.Sample(grid, Catalog, new HexIndex(4, 3));
        Assert.Equal("Woods", sample.Terrain.Name);
        Assert.Equal(CenterTerrainSource.CenterSample, sample.Source);
    }

    [Fact]
    public void BuildingAtAHexsideReplacesANonBuildingCenter()
    {
        // E4's north-east edge sample point is (251, 210).
        var grid = Build(codeAt: (x, y) => (x, y) == (251, 210) ? Stone2 : Open);
        var sample = CenterTerrainSampler.Sample(grid, Catalog, new HexIndex(4, 3));
        Assert.Equal("Stone Building, 2 Level", sample.Terrain.Name);
        Assert.Equal(CenterTerrainSource.HexsideBuildingFallback, sample.Source);
    }

    [Fact]
    public void LastBuildingProbeWins()
    {
        // Probes run right (230, 225), left (220, 225), below (225, 230), above (225, 220); the last building found wins.
        var grid = Build(codeAt: (x, y) => (x, y) switch
        {
            (230, 225) => Stone2,
            (225, 220) => Wooden1,
            _ => Open,
        });
        var sample = CenterTerrainSampler.Sample(grid, Catalog, new HexIndex(4, 3));
        Assert.Equal("Wooden Building, 1 Level", sample.Terrain.Name);
        Assert.Equal(CenterTerrainSource.ProbeBuildingFallback, sample.Source);
    }

    [Fact]
    public void CenterOutsideTheGridUsesTheNextOnGridProbe()
    {
        // GG1's center dot clamps to x = 1799, so (1800, y) probes are off the grid and (1798, y + 1) is used.
        var gg1 = new HexIndex(32, 0);
        var center = Geometry.CenterPoint(gg1);
        var grid = Build(codeAt: (x, y) => (x, y) == (center.X - 1, center.Y + 1) ? Woods : Open);
        Assert.Equal("Woods", CenterTerrainSampler.Sample(grid, Catalog, gg1).Terrain.Name);
    }

    private const byte Open = 0;
    private const byte Stone2 = 42;
    private const byte Wooden1 = 51;
    private const byte Woods = 60;

    private static readonly TerrainCatalog Catalog = new(
    [
        new TerrainType { Code = Open, Name = "Open Ground", Category = LosCategory.Open },
        new TerrainType { Code = Stone2, Name = "Stone Building, 2 Level", Category = LosCategory.Building, Height = 2 },
        new TerrainType { Code = Wooden1, Name = "Wooden Building, 1 Level", Category = LosCategory.Building, Height = 1 },
        new TerrainType { Code = Woods, Name = "Woods", Category = LosCategory.Woods },
    ]);

    private static TerrainGrid Build(Func<int, int, byte>? codeAt = null, Func<int, int, sbyte>? elevationAt = null,
        Func<HexIndex, bool>? stairwayAt = null)
    {
        var codes = new byte[Geometry.GridWidth * Geometry.GridHeight];
        var elevations = new sbyte[codes.Length];
        for (var x = 0; x < Geometry.GridWidth; x++)
        {
            for (var y = 0; y < Geometry.GridHeight; y++)
            {
                codes[(x * Geometry.GridHeight) + y] = codeAt?.Invoke(x, y) ?? Open;
                elevations[(x * Geometry.GridHeight) + y] = elevationAt?.Invoke(x, y) ?? 0;
            }
        }

        var stairways = Geometry.Hexes().Select(hex => stairwayAt?.Invoke(hex) ?? false).ToArray();
        return new TerrainGrid(Geometry, codes, elevations, stairways);
    }
}
