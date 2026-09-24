using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>A terrain catalog with VASL's codes, names, and categories for the kinds the compiler treats specially.</summary>
internal static class FeatureTestCatalog
{
    public static readonly TerrainCatalog Catalog = new(
    [
        Type(0, "Open Ground", LosCategory.Open),
        Type(2, "Rooftop", LosCategory.Open),
        Type(32, "Shallow Stream", LosCategory.Depression),
        Type(40, "Stone Building", LosCategory.Building, height: 1),
        Type(42, "Stone Building, 2 Level", LosCategory.Building, height: 2),
        Type(45, "Stone Factory Wall, 1.5 Level", LosCategory.Factory, height: 1),
        Type(47, "Stone Factory, 1.5 Level", LosCategory.Factory, height: 1),
        Type(60, "Woods", LosCategory.Woods),
        Type(66, "Paved Road", LosCategory.Road),
        Type(67, "Elevated Road", LosCategory.Road),
        Type(68, "Sunken Road", LosCategory.Depression),
        Type(72, "Wall", LosCategory.Hexside),
        Type(73, "Hedge", LosCategory.Hexside),
        Type(75, "Cliff", LosCategory.Hexside),
        Type(84, "Stone Bridge", LosCategory.Bridge),
        Type(90, "Grain", LosCategory.Other),
        Type(157, "Rrembankment", LosCategory.Hexside),
        Type(170, "Roofless Stone Factory, 1.5 Level", LosCategory.Factory),
        Type(174, "Cellar", LosCategory.Building, height: 1),
        Type(200, "PartialOrchard", LosCategory.Hexside),
    ]);

    public static byte Code(string name) => Catalog[name].Code;

    public static HexFactSet Derive(TerrainGrid grid) => VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None);

    public static FeatureModel Model(BoardGeometry geometry, params Feature[] features) =>
        FeatureModel.New(geometry, "test-catalog") with
        {
            Features = features
        };

    /// <summary>A rectangle in whole pixels, [x0, x1) by [y0, y1).</summary>
    public static FeatureShape Box(int x0, int y0, int x1, int y1) =>
        FeatureShape.Rectangle(FixedVector.FromPixels(x0, y0), FixedVector.FromPixels(x1, y1));

    private static TerrainType Type(byte code, string name, LosCategory category, int height = 0) =>
        new()
        {
            Code = code,
            Name = name,
            Category = category,
            Height = height,
        };
}
