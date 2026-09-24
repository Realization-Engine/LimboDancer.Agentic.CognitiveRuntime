using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Outlines;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>One feature's markup for a render patch: its layer, element id, fragment, and the element it precedes.</summary>
public sealed record FeatureFragment(string Layer, string Id, string Svg, string? BeforeId);

/// <summary>
/// The Styled and Comparison layers (Architecture and Rendering Design, section 3.7). Features are drawn in compile
/// order, each in its own group <c>f-{FeatureId}</c>, so an edit can replace one feature's markup. Area and elevation
/// outlines are smoothed with Catmull-Rom splines computed in fixed point; smoothing is display-only.
/// </summary>
public static partial class BoardRenderer
{
    private const string DiffColor = "#d0006f";

    public static IReadOnlyList<string> StyledLayers
    {
        get;
    } =
        ["styled-elevation", "styled-area", "styled-linear", "styled-bridges", "styled-buildings", "styled-hexside", "styled-marks"];

    /// <summary>The styled layer that draws a feature.</summary>
    public static string StyledLayerOf(Feature feature) => feature switch
    {
        ElevationRegion => "styled-elevation",
        AreaTerrainFeature => "styled-area",
        LinearTerrainFeature => "styled-linear",
        BridgeFeature => "styled-bridges",
        BuildingFeature => "styled-buildings",
        HexsideTerrainFeature => "styled-hexside",
        _ => "styled-marks",
    };

    /// <summary>Features in compile order: stage, then level for elevation, then layer, then id.</summary>
    public static IReadOnlyList<Feature> PaintOrder(FeatureModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Features
            .OrderBy(feature => feature.Stage)
            .ThenBy(feature => feature is ElevationRegion region ? region.Level : 0)
            .ThenBy(feature => feature.Layer)
            .ThenBy(feature => feature.Id, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// A single feature as a patch fragment, with the id of the next feature in its layer so the browser can insert it
    /// in paint order; null when the feature is not in the model.
    /// </summary>
    public static FeatureFragment? RenderFeature(BoardRenderInput input, string featureId)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Styled is not { } styled || styled.Model.Features.FirstOrDefault(feature => feature.Id == featureId) is not { } feature)
        {
            return null;
        }

        var layer = StyledLayerOf(feature);
        var order = PaintOrder(styled.Model).Where(candidate => StyledLayerOf(candidate) == layer).ToArray();
        var index = Array.IndexOf(order, feature);
        var before = index + 1 < order.Length ? "f-" + order[index + 1].Id : null;
        var writer = new SvgWriter();
        writer.Start("svg", ("xmlns", SvgNamespace), ("viewBox", ViewBox(input, BoardView.Styled)));
        WriteFeature(writer, input, feature);
        writer.End();
        return new FeatureFragment(layer, "f-" + feature.Id, writer.ToString(), before);
    }

    private static void WriteThemeDefs(SvgWriter writer, RenderTheme theme)
    {
        writer.Start("defs");
        foreach (var pattern in theme.Patterns)
        {
            writer.Start("pattern", ("id", "pattern-" + pattern.Id), ("width", SvgWriter.Number(pattern.Width)), ("height", SvgWriter.Number(pattern.Height)),
                ("patternUnits", "userSpaceOnUse"));
            writer.Empty("rect", ("width", SvgWriter.Number(pattern.Width)), ("height", SvgWriter.Number(pattern.Height)), ("fill", pattern.Background));
            foreach (var (name, attributes) in pattern.Elements)
            {
                writer.Empty(name, [.. attributes]);
            }

            writer.End();
        }

        writer.Start("pattern", ("id", "diff-hatch"), ("width", "6"), ("height", "6"), ("patternUnits", "userSpaceOnUse"), ("patternTransform", "rotate(45)"));
        writer.Empty("line", ("x1", "0"), ("y1", "0"), ("x2", "0"), ("y2", "6"), ("stroke", DiffColor), ("stroke-width", "2"));
        writer.End();
        writer.End();
    }

    private static void WriteStyledLayer(SvgWriter writer, BoardRenderInput input, string layer)
    {
        if (input.Styled is not { } styled)
        {
            return;
        }

        var geometry = styled.Model.Geometry;
        if (layer == "styled-elevation")
        {
            writer.Empty("rect", ("x", "0"), ("y", "0"), ("width", SvgWriter.Number(geometry.GridWidth)), ("height", SvgWriter.Number(geometry.GridHeight)),
                ("fill", input.Theme.Tint(styled.Model.BaseElevation)));
        }

        foreach (var feature in PaintOrder(styled.Model).Where(feature => StyledLayerOf(feature) == layer))
        {
            WriteFeature(writer, input, feature);
        }

        if (layer == "styled-marks")
        {
            foreach (var hex in styled.Model.Annotations.Stairways.Where(geometry.Contains).OrderBy(hex => hex.Column).ThenBy(hex => hex.Row))
            {
                var center = geometry.CenterDot(hex);
                writer.Empty("rect", ("id", "stair-" + geometry.NameOf(hex)), ("x", SvgWriter.Number(center.X - 3)), ("y", SvgWriter.Number(center.Y - 3)),
                    ("width", "6"), ("height", "6"), ("fill", "#ffffff"), ("stroke", "#000000"), ("stroke-width", "0.75"));
            }
        }
    }

    private static void WriteFeature(SvgWriter writer, BoardRenderInput input, Feature feature)
    {
        var geometry = input.Styled!.Model.Geometry;
        var theme = input.Theme;
        var code = TerrainKinds.CodeOf(feature);
        var terrain = code is { } value && input.Catalog.TryGet(value, out var type) ? type : null;
        var style = terrain is null ? null : theme.StyleFor(terrain);
        var fallback = terrain?.MapColor.ToHex() ?? "#ff00ff";
        writer.Start("g", ("id", "f-" + feature.Id), ("data-kind", KindName(feature)), ("data-code", code is { } dataCode ? SvgWriter.Number(dataCode) : string.Empty));
        switch (feature)
        {
            case ElevationRegion region:
                writer.Empty("path", ("fill", theme.Tint(region.Level)), ("stroke", theme.Crest.Stroke ?? "none"), ("stroke-width", theme.Crest.StrokeWidth ?? "1"),
                    ("fill-rule", "nonzero"), ("d", Smooth(region.Shape).ToString()));
                break;
            case AreaTerrainFeature area:
                Fill(writer, Smooth(area.Shape), style, fallback);
                break;
            case BridgeFeature bridge:
                Fill(writer, Sharp(bridge.Shape), style, fallback);
                break;
            case BuildingFeature building:
                foreach (var footprint in building.Footprints)
                {
                    if (style is { Shadow: true } && terrain is { Height: >= 2 } or { IsFactory: true })
                    {
                        writer.Empty("path", ("fill", "#000000"), ("fill-opacity", "0.25"), ("d", Sharp(footprint, 2 * FixedPoint.UnitsPerPixel).ToString()));
                    }

                    Fill(writer, Sharp(footprint), style, fallback);
                }

                break;
            case LinearTerrainFeature linear:
                if (linear.Outline is { } outline)
                {
                    writer.Empty("path", ("fill", style?.Fill ?? fallback), ("d", Smooth(outline).ToString()));
                }

                if (linear.Centerline is { } centerline)
                {
                    var strokes = style is { Strokes.Count: > 0 } ? style.Strokes : [new ThemeStroke(fallback, 0, null)];
                    foreach (var stroke in strokes)
                    {
                        var width = linear.Width.Raw + (int)Math.Round(stroke.Extra * FixedPoint.UnitsPerPixel);
                        var attributes = new List<(string, string)>
                        {
                            ("fill", "none"), ("stroke", stroke.Color), ("stroke-width", SvgWriter.Fixed(width)),
                            ("stroke-linecap", "round"), ("stroke-linejoin", "round"),
                        };
                        if (stroke.Dash is { } dash)
                        {
                            attributes.Add(("stroke-dasharray", dash));
                        }

                        attributes.Add(("d", Centerline(centerline).ToString()));
                        writer.Empty("path", [.. attributes]);
                    }
                }

                break;
            case HexsideTerrainFeature hexside:
                foreach (var span in hexside.Spans)
                {
                    var (from, to) = FeatureCompiler.HexsideEndpoints(geometry, span.Side);
                    var start = Lerp(from, to, span.From);
                    var end = Lerp(from, to, span.To);
                    var attributes = new List<(string, string)>
                    {
                        ("fill", "none"),
                        ("stroke", style?.Stroke ?? fallback),
                        ("stroke-width", span.Width is { } spanWidth ? SvgWriter.Number(spanWidth) : style?.StrokeWidth ?? SvgWriter.Number(theme.HexsideWidth)),
                        ("stroke-linecap", "butt"),
                    };
                    if (style?.Dash is { } dash)
                    {
                        attributes.Add(("stroke-dasharray", dash));
                    }

                    attributes.Add(("d", new PathData().MoveTo(SvgWriter.Fixed(start.X), SvgWriter.Fixed(start.Y)).LineTo(SvgWriter.Fixed(end.X), SvgWriter.Fixed(end.Y)).ToString()));
                    writer.Empty("path", [.. attributes]);
                }

                break;
            case FidelityPin pin:
                writer.Empty("path", ("class", "pin"), ("fill", "#ff00ff"), ("fill-opacity", "0.45"), ("stroke", "#ff00ff"), ("stroke-width", "1"),
                    ("d", Sharp(pin.Shape).ToString()));
                break;
        }

        writer.End();
    }

    private static void Fill(SvgWriter writer, PathData path, TerrainStyle? style, string fallback)
    {
        var attributes = new List<(string, string)> { ("fill", style?.Fill ?? fallback) };
        if (style?.Stroke is { } stroke)
        {
            attributes.Add(("stroke", stroke));
            attributes.Add(("stroke-width", style.StrokeWidth ?? "1"));
        }

        attributes.Add(("fill-rule", "nonzero"));
        attributes.Add(("d", path.ToString()));
        writer.Empty("path", [.. attributes]);
    }

    /// <summary>Closed Catmull-Rom splines through each ring's vertices, as cubic Bézier segments in fixed point.</summary>
    public static PathData Smooth(FeatureShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        var path = new PathData();
        foreach (var ring in shape.Rings)
        {
            var count = ring.Count;
            path.MoveTo(SvgWriter.Fixed(ring[0].X), SvgWriter.Fixed(ring[0].Y));
            for (var index = 0; index < count; index++)
            {
                var p0 = ring[(index - 1 + count) % count];
                var p1 = ring[index];
                var p2 = ring[(index + 1) % count];
                var p3 = ring[(index + 2) % count];
                path.CubicTo(
                    SvgWriter.Fixed(p1.X + FixedGeometry.RoundDiv(p2.X - p0.X, 6)), SvgWriter.Fixed(p1.Y + FixedGeometry.RoundDiv(p2.Y - p0.Y, 6)),
                    SvgWriter.Fixed(p2.X - FixedGeometry.RoundDiv(p3.X - p1.X, 6)), SvgWriter.Fixed(p2.Y - FixedGeometry.RoundDiv(p3.Y - p1.Y, 6)),
                    SvgWriter.Fixed(p2.X), SvgWriter.Fixed(p2.Y));
            }

            path.Close();
        }

        return path;
    }

    private static PathData Sharp(FeatureShape shape, int offset = 0)
    {
        var path = new PathData();
        foreach (var ring in shape.Rings)
        {
            path.MoveTo(SvgWriter.Fixed(ring[0].X + offset), SvgWriter.Fixed(ring[0].Y + offset));
            for (var index = 1; index < ring.Count; index++)
            {
                path.LineTo(SvgWriter.Fixed(ring[index].X + offset), SvgWriter.Fixed(ring[index].Y + offset));
            }

            path.Close();
        }

        return path;
    }

    private static PathData Centerline(CenterlinePath centerline)
    {
        var path = new PathData().MoveTo(SvgWriter.Fixed(centerline.Start.X), SvgWriter.Fixed(centerline.Start.Y));
        foreach (var segment in centerline.Segments)
        {
            if (segment.Control1 is { } c1 && segment.Control2 is { } c2)
            {
                path.CubicTo(SvgWriter.Fixed(c1.X), SvgWriter.Fixed(c1.Y), SvgWriter.Fixed(c2.X), SvgWriter.Fixed(c2.Y), SvgWriter.Fixed(segment.End.X), SvgWriter.Fixed(segment.End.Y));
            }
            else
            {
                path.LineTo(SvgWriter.Fixed(segment.End.X), SvgWriter.Fixed(segment.End.Y));
            }
        }

        return path;
    }

    private static FixedVector Lerp(FixedVector from, FixedVector to, int sixtyFourths) => new(
        from.X + FixedGeometry.RoundDiv((long)(to.X - from.X) * Math.Clamp(sixtyFourths, 0, 64), 64),
        from.Y + FixedGeometry.RoundDiv((long)(to.Y - from.Y) * Math.Clamp(sixtyFourths, 0, 64), 64));

    private static string KindName(Feature feature) => feature switch
    {
        ElevationRegion => "elevation",
        AreaTerrainFeature => "area",
        LinearTerrainFeature => "linear",
        BridgeFeature => "bridge",
        BuildingFeature => "building",
        HexsideTerrainFeature => "hexside",
        _ => "pin",
    };

    // The diff layer: cells where the Styled model's compiled grid differs from the board's grid, hatched, and hexes
    // whose facts differ, outlined (section 3.7).
    private static void WriteDiff(SvgWriter writer, BoardRenderInput input)
    {
        if (input.Styled is not { } styled)
        {
            return;
        }

        var grid = input.Grid;
        var labels = new byte[grid.CellCount];
        var cells = 0;
        for (var cell = 0; cell < labels.Length; cell++)
        {
            if (grid.Codes[cell] != styled.Grid.Codes[cell] || grid.Elevations[cell] != styled.Grid.Elevations[cell])
            {
                labels[cell] = 1;
                cells++;
            }
        }

        var hexes = HexFactComparer.DifferingHexes(input.Facts, styled.Facts);
        writer.Start("g", ("id", "diff-summary"), ("data-cells", SvgWriter.Number(cells)), ("data-hexes", SvgWriter.Number(hexes.Count)), ("pointer-events", "none"));
        if (cells > 0)
        {
            var outlines = GridOutlineTracer.Trace(new TerrainGrid(grid.Geometry, labels, new sbyte[labels.Length], new bool[grid.Geometry.HexCount]));
            var path = new PathData();
            foreach (var ring in outlines.Regions.Where(region => region.Code == 1).SelectMany(region => region.Rings))
            {
                path.Ring(ring.Vertices);
            }

            writer.Empty("path", ("id", "diff-cells"), ("fill", "url(#diff-hatch)"), ("stroke", DiffColor), ("stroke-width", "0.5"), ("fill-rule", "evenodd"),
                ("shape-rendering", "crispEdges"), ("d", path.ToString()));
        }

        foreach (var hex in hexes)
        {
            var name = grid.Geometry.NameOf(hex).ToString();
            writer.Empty("path", ("id", "d-" + name), ("fill", "none"), ("stroke", DiffColor), ("stroke-width", "3"),
                ("d", new PathData().Polygon(grid.Geometry.Vertices(hex)).ToString()));
        }

        writer.End();
    }

    private static List<(string Color, string Label)> StyledLegendEntries(BoardRenderInput input)
    {
        if (input.Styled is not { } styled)
        {
            return [];
        }

        var codes = styled.Model.Features.Select(TerrainKinds.CodeOf).OfType<byte>().Append(styled.Model.BaseCode).Distinct().Order();
        return codes.Select(code =>
        {
            if (!input.Catalog.TryGet(code, out var terrain))
            {
                return ("#ff00ff", "Unknown code " + SvgWriter.Number(code));
            }

            var style = input.Theme.StyleFor(terrain);
            var color = style?.Fill ?? style?.Stroke ?? (style is { Strokes.Count: > 0 } ? style.Strokes[^1].Color : null);
            return code == styled.Model.BaseCode && style is null
                ? (input.Theme.Tint(styled.Model.BaseElevation), terrain.Name)
                : (color ?? terrain.MapColor.ToHex(), style is null ? terrain.Name + " (unstyled)" : terrain.Name);
        }).ToList();
    }

    internal static string Describe(int count) => count.ToString(CultureInfo.InvariantCulture);
}
