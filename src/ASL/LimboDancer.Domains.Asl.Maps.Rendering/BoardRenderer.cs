using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Outlines;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

public enum BoardView
{
    /// <summary>The terrain grid traced losslessly and drawn in catalog colors (ASL-MAP-060, view 1).</summary>
    Exact,

    /// <summary>Every hex drawn from its derived Hex Facts (ASL-MAP-060, view 3).</summary>
    HexFacts,
}

/// <summary>Everything a board rendering needs. Outlines are traced once and reused across views and layers.</summary>
public sealed record BoardRenderInput(
    BoardRef Board,
    string Title,
    TerrainGrid Grid,
    TerrainCatalog Catalog,
    HexFactSet Facts,
    GridOutlines Outlines,
    GridOutlines ElevationOutlines)
{
    public static BoardRenderInput Create(BoardRef board, string title, TerrainGrid grid, TerrainCatalog catalog, HexFactSet facts)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var elevationOnly = new TerrainGrid(grid.Geometry, new byte[grid.CellCount], grid.Elevations, grid.Stairways);
        return new BoardRenderInput(board, title, grid, catalog, facts, GridOutlineTracer.Trace(grid), GridOutlineTracer.Trace(elevationOnly));
    }
}

/// <summary>
/// Renders boards as SVG documents and per-layer fragments (Architecture and Rendering Design, section 3). Output is a
/// pure function of the input, view, and trace flag, and never contains raster images (ASL-MAP-064).
/// </summary>
public static class BoardRenderer
{
    public const string RendererVersion = "1.0.0";
    public const string SvgNamespace = "http://www.w3.org/2000/svg";

    private const int Margin = 16;
    private const int LegendColumnWidth = 225;
    private const int LegendRowHeight = 20;

    public static IReadOnlyList<string> Layers(BoardView view) => view switch
    {
        BoardView.Exact => ["defs", "exact-terrain", "exact-elevation", "grid", "labels", "legend"],
        BoardView.HexFacts => ["defs", "hexfacts", "grid", "labels", "legend"],
        _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unknown view."),
    };

    /// <summary>The view box shared by every layer of a board: the grid, a margin, and the legend below.</summary>
    public static string ViewBox(BoardRenderInput input, BoardView view)
    {
        ArgumentNullException.ThrowIfNull(input);
        var geometry = input.Grid.Geometry;
        var height = geometry.GridHeight + (2 * Margin) + LegendHeight(input, view);
        return $"{-Margin} {-Margin} {geometry.GridWidth + (2 * Margin)} {height}".ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A standalone SVG document with every layer of the view.</summary>
    public static string Document(BoardRenderInput input, BoardView view, bool traceMode = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        var writer = new SvgWriter();
        writer.Start("svg", ("xmlns", SvgNamespace), ("viewBox", ViewBox(input, view)), ("data-board", input.Board.Value),
            ("data-view", ViewName(view)), ("data-renderer", RendererVersion));
        writer.Start("title");
        writer.Text(input.Title);
        writer.End();
        writer.Empty("rect", ("x", SvgWriter.Number(-Margin)), ("y", SvgWriter.Number(-Margin)), ("width", "100%"), ("height", "100%"), ("fill", "#ffffff"));
        foreach (var layer in Layers(view))
        {
            WriteLayer(writer, input, view, layer, traceMode);
        }

        writer.End();
        return writer.ToString();
    }

    /// <summary>One layer as a standalone SVG document whose root wraps the layer's group, for the Studio to import.</summary>
    public static string Fragment(BoardRenderInput input, BoardView view, string layer, bool traceMode = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Layers(view).Contains(layer))
        {
            throw new ArgumentOutOfRangeException(nameof(layer), layer, $"The {ViewName(view)} view has no such layer.");
        }

        var writer = new SvgWriter();
        writer.Start("svg", ("xmlns", SvgNamespace), ("viewBox", ViewBox(input, view)));
        WriteLayer(writer, input, view, layer, traceMode);
        writer.End();
        return writer.ToString();
    }

    public static string ViewName(BoardView view) => view == BoardView.Exact ? "exact" : "hexfacts";

    public static bool TryParseView(string? text, out BoardView view)
    {
        view = text switch
        {
            "exact" => BoardView.Exact,
            "hexfacts" => BoardView.HexFacts,
            _ => (BoardView)(-1),
        };
        return Enum.IsDefined(view);
    }

    private static void WriteLayer(SvgWriter writer, BoardRenderInput input, BoardView view, string layer, bool traceMode)
    {
        writer.Start("g", ("id", "layer-" + layer));
        switch (layer)
        {
            case "defs":
                WriteDefs(writer);
                break;
            case "exact-terrain":
                WriteExactTerrain(writer, input);
                break;
            case "exact-elevation":
                WriteElevation(writer, input);
                break;
            case "hexfacts":
                WriteHexFacts(writer, input, traceMode);
                break;
            case "grid":
                WriteGrid(writer, input.Grid.Geometry);
                break;
            case "labels":
                WriteLabels(writer, input.Grid.Geometry);
                break;
            case "legend":
                WriteLegend(writer, input, view, traceMode);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown layer.");
        }

        writer.End();
    }

    private static void WriteDefs(SvgWriter writer)
    {
        writer.Start("defs");
        writer.Start("pattern", ("id", "depression-hatch"), ("width", "6"), ("height", "6"), ("patternUnits", "userSpaceOnUse"),
            ("patternTransform", "rotate(45)"));
        writer.Empty("line", ("x1", "0"), ("y1", "0"), ("x2", "0"), ("y2", "6"), ("stroke", "#1f4e79"), ("stroke-width", "1.5"), ("stroke-opacity", "0.6"));
        writer.End();
        writer.End();
    }

    private static void WriteExactTerrain(SvgWriter writer, BoardRenderInput input)
    {
        writer.Start("g", ("shape-rendering", "crispEdges"));
        foreach (var group in input.Outlines.Regions.GroupBy(region => (region.Code, region.Elevation)))
        {
            var path = new PathData();
            foreach (var ring in group.SelectMany(region => region.Rings))
            {
                path.Ring(ring.Vertices);
            }

            var color = input.Catalog.TryGet(group.Key.Code, out var terrain) ? terrain.MapColor.ToHex() : "#ff00ff";
            writer.Empty("path",
                ("id", $"t{group.Key.Code}e{group.Key.Elevation}".ToString(CultureInfo.InvariantCulture)),
                ("data-code", SvgWriter.Number(group.Key.Code)),
                ("data-elev", SvgWriter.Number(group.Key.Elevation)),
                ("fill", color),
                ("fill-rule", "evenodd"),
                ("d", path.ToString()));
        }

        writer.End();
    }

    // Elevation bands: higher ground darkens, depressions tint blue; ground level is not drawn.
    private static void WriteElevation(SvgWriter writer, BoardRenderInput input)
    {
        writer.Start("g", ("shape-rendering", "crispEdges"), ("pointer-events", "none"));
        foreach (var group in input.ElevationOutlines.Regions.Where(region => region.Elevation != 0).GroupBy(region => region.Elevation))
        {
            var path = new PathData();
            foreach (var ring in group.SelectMany(region => region.Rings))
            {
                path.Ring(ring.Vertices);
            }

            var level = group.Key;
            var opacity = Math.Min(0.5, 0.12 * Math.Abs(level)).ToString("0.00", CultureInfo.InvariantCulture);
            writer.Empty("path",
                ("id", "elev" + SvgWriter.Number(level)),
                ("data-elev", SvgWriter.Number(level)),
                ("fill", level > 0 ? "#000000" : "#1f4e79"),
                ("fill-opacity", opacity),
                ("fill-rule", "evenodd"),
                ("d", path.ToString()));
        }

        writer.End();
    }

    private static void WriteHexFacts(SvgWriter writer, BoardRenderInput input, bool traceMode)
    {
        var geometry = input.Grid.Geometry;
        foreach (var facts in input.Facts.Hexes)
        {
            var name = facts.Hex.ToString();
            var vertices = geometry.Vertices(facts.Index);
            var center = geometry.CenterDot(facts.Index);
            writer.Start("g", ("id", "h-" + name), ("data-hex", name));
            var fill = traceMode ? TraceColor(facts.CenterSource) : facts.Center.Terrain?.MapColor.ToHex() ?? "#ffffff";
            writer.Empty("path", ("fill", fill), ("d", new PathData().Polygon(vertices).ToString()));
            if (facts.Center.DepressionTerrain is not null)
            {
                writer.Empty("path", ("fill", "url(#depression-hatch)"), ("d", new PathData().Polygon(vertices).ToString()));
            }

            for (var side = 0; side < 6; side++)
            {
                var hexside = facts.Hexsides[side];
                var from = vertices[side];
                var to = vertices[(side + 1) % 6];
                var segment = new PathData().MoveTo(SvgWriter.Number(from.X), SvgWriter.Number(from.Y))
                    .LineTo(SvgWriter.Number(to.X), SvgWriter.Number(to.Y)).ToString();
                if (!hexside.OnMap)
                {
                    writer.Empty("path", ("data-side", SvgWriter.Number(side)), ("stroke", "#c00000"), ("stroke-width", "1.5"),
                        ("stroke-dasharray", "3 3"), ("fill", "none"), ("d", segment));
                }
                else if (hexside.HexsideTerrain is { } terrain)
                {
                    writer.Empty("path", ("data-side", SvgWriter.Number(side)), ("data-terrain", terrain.Name),
                        ("stroke", HexsideColor(terrain)), ("stroke-width", hexside.Cliff ? "5" : "4"), ("stroke-linecap", "round"),
                        ("fill", "none"), ("d", segment));
                }
            }

            WriteHexMarks(writer, facts, center);
            writer.End();
        }
    }

    private static void WriteHexMarks(SvgWriter writer, HexFacts facts, PixelPoint center)
    {
        if (facts.Stairway)
        {
            writer.Empty("rect", ("x", SvgWriter.Number(center.X - 3)), ("y", SvgWriter.Number(center.Y - 3)), ("width", "6"), ("height", "6"),
                ("fill", "#ffffff"), ("stroke", "#000000"), ("stroke-width", "0.75"));
        }

        var upper = facts.Locations.Where(location => location.Level > 0 && location.Terrain is { IsBuilding: true }).Select(location => location.Level).DefaultIfEmpty(0).Max();
        var marks = new List<string>();
        if (facts.BaseLevel != 0)
        {
            marks.Add((facts.BaseLevel > 0 ? "+" : string.Empty) + SvgWriter.Number(facts.BaseLevel));
        }

        if (upper > 0)
        {
            marks.Add("L" + SvgWriter.Number(upper));
        }

        if (facts.Bridge is not null)
        {
            marks.Add("B");
        }

        if (marks.Count > 0)
        {
            writer.Start("text", ("x", SvgWriter.Number(center.X)), ("y", SvgWriter.Number(center.Y + 16)), ("font-family", "Arial, sans-serif"),
                ("font-size", "9"), ("text-anchor", "middle"), ("fill", "#000000"));
            writer.Text(string.Join(' ', marks));
            writer.End();
        }
    }

    private static void WriteGrid(SvgWriter writer, BoardGeometry geometry)
    {
        var outlines = new PathData();
        var dots = new PathData();
        foreach (var hex in geometry.Hexes())
        {
            outlines.Polygon(geometry.Vertices(hex));
            var center = geometry.CenterDot(hex);
            dots.MoveTo(SvgWriter.Number(center.X - 1), SvgWriter.Number(center.Y - 1))
                .Horizontal(SvgWriter.Number(center.X + 1)).Vertical(SvgWriter.Number(center.Y + 1))
                .Horizontal(SvgWriter.Number(center.X - 1)).Close();
        }

        writer.Empty("path", ("fill", "none"), ("stroke", "#000000"), ("stroke-opacity", "0.45"), ("stroke-width", "0.75"),
            ("pointer-events", "none"), ("d", outlines.ToString()));
        writer.Empty("path", ("fill", "#000000"), ("fill-opacity", "0.6"), ("pointer-events", "none"), ("d", dots.ToString()));
    }

    private static void WriteLabels(SvgWriter writer, BoardGeometry geometry)
    {
        writer.Start("g", ("font-family", "Arial, sans-serif"), ("font-size", "9"), ("text-anchor", "middle"), ("fill", "#000000"),
            ("fill-opacity", "0.75"), ("pointer-events", "none"));
        foreach (var hex in geometry.Hexes())
        {
            var center = geometry.CenterDot(hex);
            writer.Start("text", ("x", SvgWriter.Number(center.X)), ("y", SvgWriter.Number(center.Y - 22)));
            writer.Text(geometry.NameOf(hex).ToString());
            writer.End();
        }

        writer.End();
    }

    private static void WriteLegend(SvgWriter writer, BoardRenderInput input, BoardView view, bool traceMode)
    {
        var entries = LegendEntries(input, view, traceMode);
        var geometry = input.Grid.Geometry;
        var perRow = Math.Max(1, geometry.GridWidth / LegendColumnWidth);
        writer.Start("g", ("font-family", "Arial, sans-serif"), ("font-size", "12"), ("fill", "#000000"));
        for (var index = 0; index < entries.Count; index++)
        {
            var x = (index % perRow) * LegendColumnWidth;
            var y = geometry.GridHeight + Margin + ((index / perRow) * LegendRowHeight);
            writer.Empty("rect", ("x", SvgWriter.Number(x)), ("y", SvgWriter.Number(y)), ("width", "14"), ("height", "14"),
                ("fill", entries[index].Color), ("stroke", "#000000"), ("stroke-width", "0.5"));
            writer.Start("text", ("x", SvgWriter.Number(x + 20)), ("y", SvgWriter.Number(y + 11)));
            writer.Text(entries[index].Label);
            writer.End();
        }

        writer.End();
    }

    private static List<(string Color, string Label)> LegendEntries(BoardRenderInput input, BoardView view, bool traceMode)
    {
        if (view == BoardView.HexFacts && traceMode)
        {
            return Enum.GetValues<CenterTerrainSource>().Select(source => (TraceColor(source), TraceLabel(source))).ToList();
        }

        IEnumerable<byte> codes = view == BoardView.Exact
            ? input.Grid.DistinctCodes()
            : input.Facts.Hexes.Select(hex => hex.Center.Terrain).Concat(input.Facts.Hexes.SelectMany(hex => hex.Hexsides.Select(side => side.HexsideTerrain)))
                .Where(terrain => terrain is not null).Select(terrain => terrain!.Code).Distinct().Order();
        return codes.Select(code => input.Catalog.TryGet(code, out var terrain)
            ? (view == BoardView.HexFacts && terrain.IsHexsideTerrain ? HexsideColor(terrain) : terrain.MapColor.ToHex(), terrain.Name)
            : ("#ff00ff", "Unknown code " + SvgWriter.Number(code))).ToList();
    }

    private static int LegendHeight(BoardRenderInput input, BoardView view)
    {
        var perRow = Math.Max(1, input.Grid.Geometry.GridWidth / LegendColumnWidth);
        var entries = Math.Max(LegendEntries(input, view, traceMode: false).Count, Enum.GetValues<CenterTerrainSource>().Length);
        return ((entries + perRow - 1) / perRow) * LegendRowHeight;
    }

    // Catalog colors for walls and hedges are close to open ground, so hexside features use their own palette.
    private static string HexsideColor(TerrainType terrain) => terrain.Name switch
    {
        "Wall" => "#6e6e6e",
        "Hedge" => "#2e7d32",
        "Bocage" => "#1b5e20",
        "Cliff" => "#5d4037",
        _ when terrain.IsRowhouseFactoryWallOrBreach => "#000000",
        _ => terrain.MapColor.ToHex(),
    };

    private static string TraceColor(CenterTerrainSource source) => source switch
    {
        CenterTerrainSource.CenterSample => "#e8eef7",
        CenterTerrainSource.HexsideBuildingFallback => "#f4b183",
        CenterTerrainSource.ProbeBuildingFallback => "#c55a11",
        _ => "#ff0000",
    };

    private static string TraceLabel(CenterTerrainSource source) => source switch
    {
        CenterTerrainSource.CenterSample => "Center sample",
        CenterTerrainSource.HexsideBuildingFallback => "Hexside building fallback",
        CenterTerrainSource.ProbeBuildingFallback => "Probe building fallback",
        _ => "Default (Open Ground)",
    };
}
