using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

/// <summary>A small hand-painted 3 by 2 board with open ground, woods, a building, a stream, and a hill.</summary>
internal static class SyntheticBoard
{
    public static readonly TerrainCatalog Catalog = new(
    [
        Type(0, "Open Ground", LosCategory.Open, new TerrainColor(0xa8, 0xb8, 0x60)),
        Type(2, "Rooftop", LosCategory.Open, new TerrainColor(0x80, 0x80, 0x80)),
        Type(32, "Shallow Stream", LosCategory.Depression, new TerrainColor(0x6c, 0xa6, 0xcd)),
        Type(42, "Stone Building, 2 Level", LosCategory.Building, new TerrainColor(0x70, 0x70, 0x70), height: 2),
        Type(60, "Woods", LosCategory.Woods, new TerrainColor(0x2e, 0x7d, 0x32)),
        Type(72, "Wall", LosCategory.Hexside, new TerrainColor(0x5a, 0x5a, 0x5a)),
    ]);

    public static readonly BoardGeometry Geometry = BoardGeometry.Standard(3, 2);

    public static TerrainGrid Grid()
    {
        var width = Geometry.GridWidth;
        var height = Geometry.GridHeight;
        var codes = new byte[width * height];
        var elevations = new sbyte[codes.Length];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var cell = (x * height) + y;
                codes[cell] = (x, y) switch
                {
                    _ when x is >= 20 and < 45 && y is >= 15 and < 50 => 60,
                    _ when x is >= 60 and < 80 && y is >= 20 and < 40 => 42,
                    _ when y is >= 90 and < 96 => 32,
                    _ when x is 100 && y < 60 => 72,
                    _ => 0,
                };
                elevations[cell] = (x - 110) * (x - 110) + (y - 70) * (y - 70) < 400 ? (sbyte)1 : y >= 90 && y < 96 ? (sbyte)-1 : (sbyte)0;
            }
        }

        var stairways = new bool[Geometry.HexCount];
        stairways[Geometry.HexOrdinal(new HexIndex(1, 0))] = true;
        return new TerrainGrid(Geometry, codes, elevations, stairways);
    }

    public static BoardRenderInput Input()
    {
        var grid = Grid();
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None);
        return BoardRenderInput.Create(BoardRef.Parse("ab-synthetic"), "Synthetic 3 by 2 board", grid, Catalog, facts);
    }

    private static TerrainType Type(byte code, string name, LosCategory category, TerrainColor color, int height = 0) =>
        new()
        {
            Code = code,
            Name = name,
            Category = category,
            MapColor = color,
            Height = height,
        };
}
