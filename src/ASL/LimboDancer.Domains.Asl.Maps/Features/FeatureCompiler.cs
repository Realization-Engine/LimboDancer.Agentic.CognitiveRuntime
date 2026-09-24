using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>A compiled grid, with the grid as it stood before the post-passes for diagnostics and the vectorizer.</summary>
public sealed record CompileResult(TerrainGrid Grid, TerrainGrid PrePassGrid);

/// <summary>
/// Compiles a Feature Model into a Terrain Grid (Model Design section 5.3), deterministically:
/// <list type="number">
/// <item>base; elevation regions by level then layer; area terrain; linear terrain; bridges; buildings; hexside
/// terrain, each by (layer, id);</item>
/// <item>the <c>LOSDataEditor.createLOSData</c> post-passes in its order: cliff elevations, exterior factory walls,
/// depression elevation minus one, and sunken roads;</item>
/// <item>fidelity pins, painted over the post-pass result because they reproduce final VASL values;</item>
/// <item>stairways from the hex annotations.</item>
/// </list>
/// </summary>
public static class FeatureCompiler
{
    public const string Version = "1.0.0";

    /// <summary>The standard hexside stroke width in pixels, and the smallest allowed.</summary>
    public const int StandardHexsideWidth = 6;
    public const int MinimumHexsideWidth = 3;

    public static CompileResult Compile(FeatureModel model, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(catalog);
        var geometry = model.Geometry;
        var width = geometry.GridWidth;
        var height = geometry.GridHeight;
        var codes = new byte[width * height];
        var elevations = new sbyte[codes.Length];
        Array.Fill(codes, model.BaseCode);
        Array.Fill(elevations, checked((sbyte)model.BaseElevation));

        var ordered = model.Features
            .OrderBy(feature => feature.Stage)
            .ThenBy(feature => feature is ElevationRegion region ? region.Level : 0)
            .ThenBy(feature => feature.Layer)
            .ThenBy(feature => feature.Id, StringComparer.Ordinal)
            .ToArray();

        foreach (var feature in ordered)
        {
            switch (feature)
            {
                case ElevationRegion region:
                    var level = checked((sbyte)region.Level);
                    ShapeRasterizer.Fill(region.Shape.Rings, width, height, (x, y) => elevations[(x * height) + y] = level);
                    break;
                case AreaTerrainFeature area:
                    PaintCode(area.Shape.Rings, area.Code, codes, width, height);
                    break;
                case LinearTerrainFeature linear:
                    PaintLinear(linear, codes, width, height);
                    break;
                case BridgeFeature bridge:
                    PaintCode(bridge.Shape.Rings, bridge.Code, codes, width, height);
                    break;
                case BuildingFeature building:
                    foreach (var footprint in building.Footprints)
                    {
                        PaintCode(footprint.Rings, building.Code, codes, width, height);
                    }

                    break;
                case HexsideTerrainFeature hexside:
                    foreach (var span in hexside.Spans)
                    {
                        PaintCode(HexsideStroke(geometry, span), hexside.Code, codes, width, height);
                    }

                    break;
                case FidelityPin:
                    break;
                default:
                    throw new NotSupportedException($"Unknown feature kind {feature.GetType().Name}.");
            }
        }

        var prePass = new TerrainGrid(geometry, codes, elevations, new bool[geometry.HexCount]);
        ApplyPostPasses(geometry, catalog, codes, elevations);

        foreach (var pin in ordered.OfType<FidelityPin>())
        {
            var code = pin.Code;
            var elevation = pin.Elevation is { } value ? checked((sbyte)value) : (sbyte?)null;
            ShapeRasterizer.Fill(pin.Shape.Rings, width, height, (x, y) =>
            {
                codes[(x * height) + y] = code;
                if (elevation is { } level)
                {
                    elevations[(x * height) + y] = level;
                }
            });
        }

        var stairways = new bool[geometry.HexCount];
        foreach (var hex in model.Annotations.Stairways)
        {
            if (geometry.Contains(hex))
            {
                stairways[geometry.HexOrdinal(hex)] = true;
            }
        }

        return new CompileResult(new TerrainGrid(geometry, codes, elevations, stairways), prePass);
    }

    /// <summary>
    /// The stroke polygon for one hexside span: centered on the hexside, butt ends, clipped to the extent. The width is
    /// the span's own, or the standard 6 pixels, and never less than 3.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<FixedVector>> HexsideStroke(BoardGeometry geometry, HexsideSpan span)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(span);
        var (from, to) = HexsideEndpoints(geometry, span.Side);
        var start = Lerp(from, to, Math.Clamp(span.From, 0, 64));
        var end = Lerp(from, to, Math.Clamp(span.To, 0, 64));
        var widthPixels = Math.Max(MinimumHexsideWidth, span.Width ?? StandardHexsideWidth);
        return FixedGeometry.Quad(start, end, widthPixels * FixedPoint.UnitsPerPixel / 2) is { } quad ? [quad] : [];
    }

    /// <summary>The two vertices bounding a hexside, in fixed point, in clockwise order around the hex.</summary>
    public static (FixedVector From, FixedVector To) HexsideEndpoints(BoardGeometry geometry, HexsideRef side)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var vertices = geometry.Vertices(side.Hex);
        var index = (int)side.Side;
        var a = vertices[index];
        var b = vertices[(index + 1) % 6];
        return (FixedVector.FromExactPixels(a.X, a.Y), FixedVector.FromExactPixels(b.X, b.Y));
    }

    private static FixedVector Lerp(FixedVector from, FixedVector to, int sixtyFourths) => new(
        from.X + FixedGeometry.RoundDiv((long)(to.X - from.X) * sixtyFourths, 64),
        from.Y + FixedGeometry.RoundDiv((long)(to.Y - from.Y) * sixtyFourths, 64));

    private static void PaintCode(IEnumerable<IReadOnlyList<FixedVector>> rings, byte code, byte[] codes, int width, int height) =>
        ShapeRasterizer.Fill(rings, width, height, (x, y) => codes[(x * height) + y] = code);

    private static void PaintLinear(LinearTerrainFeature linear, byte[] codes, int width, int height)
    {
        if (linear.Outline is { } outline)
        {
            PaintCode(outline.Rings, linear.Code, codes, width, height);
        }

        if (linear.Centerline is { } centerline)
        {
            // Each stroke piece is filled separately, so the painted cells are the union of the pieces.
            foreach (var piece in FixedGeometry.Stroke(FixedGeometry.Flatten(centerline), linear.Width.Raw, roundJoins: true))
            {
                PaintCode([piece], linear.Code, codes, width, height);
            }
        }
    }

    private static void ApplyPostPasses(BoardGeometry geometry, TerrainCatalog catalog, byte[] codes, sbyte[] elevations)
    {
        var width = geometry.GridWidth;
        var height = geometry.GridHeight;
        var cliff = catalog.TryGet("Cliff", out var cliffType) ? cliffType.Code : (byte?)null;
        var sunkenRoad = catalog.TryGet("Sunken Road", out var sunkenType) ? sunkenType.Code : (byte?)null;
        var elevatedRoad = catalog.TryGet("Elevated Road", out var elevatedType) ? elevatedType.Code : (byte?)null;
        VaslHexLocator? locator = null;

        // 1. Cliff pixels take the base level of the hex the editor assigns them to. The editor's map uses the "Normal"
        //    grid configuration, under which getAdjacentHex returns the hex itself, so "the lower of the two hexes" is
        //    always that hex's own base level.
        if (cliff is { } cliffCode && Array.IndexOf(codes, cliffCode) >= 0)
        {
            locator ??= new VaslHexLocator(geometry);
            var facts = IntermediateFacts(geometry, catalog, codes, elevations);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var cell = (x * height) + y;
                    if (codes[cell] == cliffCode && locator.GridToHex(x, y) is { } hex)
                    {
                        elevations[cell] = checked((sbyte)facts[hex].BaseLevel);
                    }
                }
            }
        }

        // 2. Exterior factory walls: factory pixels with a non-factory 4-neighbor (edges clamp to the pixel itself).
        //    Walls are factory terrain too, so converting in place does not cascade.
        var wallFor = FactoryWalls(catalog);
        var isFactory = new bool[256];
        foreach (var type in catalog.Types)
        {
            isFactory[type.Code] = type.IsFactory;
        }

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var cell = (x * height) + y;
                if (!isFactory[codes[cell]])
                {
                    continue;
                }

                if (!isFactory[codes[(Math.Max(x - 1, 0) * height) + y]] || !isFactory[codes[(Math.Min(x + 1, width - 1) * height) + y]]
                    || !isFactory[codes[(x * height) + Math.Max(y - 1, 0)]] || !isFactory[codes[(x * height) + Math.Min(y + 1, height - 1)]])
                {
                    codes[cell] = wallFor[codes[cell]];
                }
            }
        }

        // 3. Depression pixels one level down.
        var isDepression = new bool[256];
        foreach (var type in catalog.Types)
        {
            isDepression[type.Code] = type.IsDepression;
        }

        for (var cell = 0; cell < codes.Length; cell++)
        {
            if (isDepression[codes[cell]])
            {
                elevations[cell] = checked((sbyte)(elevations[cell] - 1));
            }
        }

        // 4. Sunken roads: Elevated Road at level 1 in hexes whose base level is 1, otherwise elevation -1.
        if (sunkenRoad is { } sunkenCode && elevatedRoad is { } elevatedCode && Array.IndexOf(codes, sunkenCode) >= 0)
        {
            locator ??= new VaslHexLocator(geometry);
            var facts = IntermediateFacts(geometry, catalog, codes, elevations);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var cell = (x * height) + y;
                    if (codes[cell] != sunkenCode)
                    {
                        continue;
                    }

                    // gridToHex can return null here; VASL would then fail, so such pixels are left as they are.
                    if (locator.GridToHex(x, y) is not { } hex)
                    {
                        continue;
                    }

                    if (facts[hex].BaseLevel == 1)
                    {
                        elevations[cell] = 1;
                        codes[cell] = elevatedCode;
                    }
                    else
                    {
                        elevations[cell] = -1;
                    }
                }
            }
        }
    }

    // Map.resetHexTerrain between the editor's passes: derive facts from the grid as it stands.
    private static HexFactSet IntermediateFacts(BoardGeometry geometry, TerrainCatalog catalog, byte[] codes, sbyte[] elevations) =>
        VaslCompatibleHexFactDerivation.Derive(new TerrainGrid(geometry, codes, elevations, new bool[geometry.HexCount]), catalog, HexsideAnnotations.None);

    // LOSDataEditor.setExteriorFactoryWalls: the wall code for each factory interior code; other factory codes keep their own.
    private static byte[] FactoryWalls(TerrainCatalog catalog)
    {
        var map = new byte[256];
        for (var code = 0; code < 256; code++)
        {
            map[code] = (byte)code;
        }

        (string Factory, string Wall)[] pairs =
        [
            ("Wooden Factory, 1.5 Level", "Wooden Factory Wall, 1.5 Level"),
            ("Wooden Factory, 2.5 Level", "Wooden Factory Wall, 2.5 Level"),
            ("Stone Factory, 1.5 Level", "Stone Factory Wall, 1.5 Level"),
            ("Stone Factory, 2.5 Level", "Stone Factory Wall, 2.5 Level"),
            ("Roofless Stone Factory, 1.5 Level", "Stone Factory Wall, 1.5 Level"),
            ("Roofless Stone Factory, 2.5 Level", "Stone Factory Wall, 2.5 Level"),
            ("Wooden Factory, 1 Level", "Wooden Factory Wall, 1 Level"),
        ];
        foreach (var (factory, wall) in pairs)
        {
            if (catalog.TryGet(factory, out var factoryType) && catalog.TryGet(wall, out var wallType))
            {
                map[factoryType.Code] = wallType.Code;
            }
        }

        return map;
    }

    /// <summary>The factory interior codes whose exterior wall is <paramref name="wallCode"/>, for the vectorizer.</summary>
    public static IReadOnlyList<byte> FactoriesWithWall(TerrainCatalog catalog, byte wallCode)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var walls = FactoryWalls(catalog);
        return Enumerable.Range(0, 256).Where(code => code != wallCode && walls[code] == wallCode).Select(code => (byte)code).ToArray();
    }
}
