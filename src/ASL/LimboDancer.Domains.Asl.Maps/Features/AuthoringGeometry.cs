using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>
/// The building footprint kit (Model Design section 5.2). Span and flush boxes extend one pixel past the hexsides they
/// touch, so a neighbor's footprint that also reaches the hexside covers both edge samples and joins the building.
/// </summary>
public static class BuildingKit
{
    public const int DefaultSize = 28;

    /// <summary>A square of <paramref name="size"/> pixels centered on the hex.</summary>
    public static FeatureShape Centered(BoardGeometry geometry, HexIndex hex, int size = DefaultSize)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var center = Center(geometry, hex);
        var half = size * FixedPoint.UnitsPerPixel / 2;
        return FeatureShape.Rectangle(new FixedVector(center.X - half, center.Y - half), new FixedVector(center.X + half, center.Y + half));
    }

    /// <summary>
    /// A box from one hexside across the hex to the opposite one, along axis 0 (north to south), 1 (northeast to
    /// southwest), or 2 (southeast to northwest).
    /// </summary>
    public static FeatureShape Span(BoardGeometry geometry, HexIndex hex, int axis, int width = DefaultSize)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentOutOfRangeException.ThrowIfNegative(axis);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(axis, 2);
        var from = HexsideMidpoint(geometry, new HexsideRef(hex, (HexsideDirection)axis));
        var to = HexsideMidpoint(geometry, new HexsideRef(hex, ((HexsideDirection)axis).Opposite()));
        return Box(from, to, width);
    }

    /// <summary>A box from the hex center to one hexside.</summary>
    public static FeatureShape Flush(BoardGeometry geometry, HexIndex hex, HexsideDirection side, int width = DefaultSize)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        return Box(Center(geometry, hex), HexsideMidpoint(geometry, new HexsideRef(hex, side)), width, extendStart: false);
    }

    public static FixedVector Center(BoardGeometry geometry, HexIndex hex)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var dot = geometry.CenterDot(hex);
        return FixedVector.FromPixels(dot.X, dot.Y);
    }

    public static FixedVector HexsideMidpoint(BoardGeometry geometry, HexsideRef side)
    {
        var (from, to) = FeatureCompiler.HexsideEndpoints(geometry, side);
        return new FixedVector((from.X + to.X) / 2, (from.Y + to.Y) / 2);
    }

    // A rectangle of the given width along from -> to, extended one pixel past the far end (and the near end).
    private static FeatureShape Box(FixedVector from, FixedVector to, int width, bool extendStart = true)
    {
        long dx = to.X - from.X;
        long dy = to.Y - from.Y;
        var length = FixedGeometry.Sqrt((dx * dx) + (dy * dy));
        var ex = FixedGeometry.RoundDiv(dx * FixedPoint.UnitsPerPixel, length);
        var ey = FixedGeometry.RoundDiv(dy * FixedPoint.UnitsPerPixel, length);
        var start = extendStart ? new FixedVector(from.X - ex, from.Y - ey) : from;
        var end = new FixedVector(to.X + ex, to.Y + ey);
        return new FeatureShape([FixedGeometry.Quad(start, end, width * FixedPoint.UnitsPerPixel / 2)!]);
    }
}

/// <summary>Pointer geometry for the editor: hexside hit tests, feature hit tests, and snapping (Architecture section 6.2).</summary>
public static class AuthoringGeometry
{
    /// <summary>The kinds of snap target, in priority order.</summary>
    public enum SnapKind
    {
        HexCenter,
        Vertex,
        HexsideMidpoint,
        FeatureVertex,
        Pixel,
    }

    /// <summary>
    /// The canonical hexside nearest a point, within <paramref name="radiusPixels"/> of its segment; null when none is
    /// that close.
    /// </summary>
    public static HexsideRef? NearestHexside(BoardGeometry geometry, FixedVector point, int radiusPixels = 6)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometry.HexAt(point.X / 64.0, point.Y / 64.0) is not { } hex)
        {
            return null;
        }

        HexsideRef? best = null;
        var bestDistance = (double)radiusPixels * FixedPoint.UnitsPerPixel;
        foreach (var side in HexsideDirections.All)
        {
            var reference = new HexsideRef(hex, side);
            var (from, to) = FeatureCompiler.HexsideEndpoints(geometry, reference);
            var distance = SegmentDistance(point, from, to);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = reference;
            }
        }

        return best is { } found ? geometry.Canonicalize(found) : null;
    }

    /// <summary>
    /// Snaps a point to hex centers, vertices, hexside midpoints, and feature vertices, in that priority, within
    /// <paramref name="radiusPixels"/>; otherwise to the nearest whole pixel.
    /// </summary>
    public static (FixedVector Point, SnapKind Kind) Snap(BoardGeometry geometry, FeatureModel? model, FixedVector point, int radiusPixels = 6)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var radius = (long)radiusPixels * FixedPoint.UnitsPerPixel;
        var nearby = geometry.HexAt(point.X / 64.0, point.Y / 64.0) is { } hex
            ? new[] { hex }.Concat(HexsideDirections.All.Select(side => geometry.Neighbor(hex, side)).OfType<HexIndex>()).ToArray()
            : [];

        var centers = nearby.Select(candidate => BuildingKit.Center(geometry, candidate));
        var vertices = nearby.SelectMany(candidate => geometry.Vertices(candidate)).Select(vertex => FixedVector.FromPixels(vertex.X, vertex.Y));
        var midpoints = nearby.SelectMany(candidate => HexsideDirections.All.Select(side => BuildingKit.HexsideMidpoint(geometry, new HexsideRef(candidate, side))));
        var featureVertices = model?.Features.SelectMany(feature => FeatureVertices(geometry, feature)) ?? [];
        foreach (var (kind, candidates) in new[] { (SnapKind.HexCenter, centers), (SnapKind.Vertex, vertices), (SnapKind.HexsideMidpoint, midpoints), (SnapKind.FeatureVertex, featureVertices) })
        {
            FixedVector? best = null;
            var bestDistance = radius * radius;
            foreach (var candidate in candidates)
            {
                long dx = candidate.X - point.X;
                long dy = candidate.Y - point.Y;
                var distance = (dx * dx) + (dy * dy);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            if (best is { } snapped)
            {
                return (snapped, kind);
            }
        }

        return (new FixedVector(FixedGeometry.RoundDiv(point.X, 64) * 64, FixedGeometry.RoundDiv(point.Y, 64) * 64), SnapKind.Pixel);
    }

    /// <summary>
    /// The topmost feature whose painted area covers the point, by compile order (hexside terrain within its stroke),
    /// or null.
    /// </summary>
    public static Feature? FeatureAt(FeatureModel model, FixedVector point)
    {
        ArgumentNullException.ThrowIfNull(model);
        var geometry = model.Geometry;
        var x = (int)Math.Floor(point.X / 64.0);
        var y = (int)Math.Floor(point.Y / 64.0);
        var ordered = model.Features
            .OrderBy(feature => feature.Stage)
            .ThenBy(feature => feature is ElevationRegion region ? region.Level : 0)
            .ThenBy(feature => feature.Layer)
            .ThenBy(feature => feature.Id, StringComparer.Ordinal)
            .Reverse();
        foreach (var feature in ordered)
        {
            if (Rings(geometry, feature).Any(rings => Covers(rings, x, y)))
            {
                return feature;
            }
        }

        return null;
    }

    /// <summary>The ring sets a feature paints: one set per shape, footprint, stroke piece, or hexside span.</summary>
    public static IEnumerable<IReadOnlyList<IReadOnlyList<FixedVector>>> Rings(BoardGeometry geometry, Feature feature)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(feature);
        switch (feature)
        {
            case ElevationRegion region:
                yield return region.Shape.Rings;
                break;
            case AreaTerrainFeature area:
                yield return area.Shape.Rings;
                break;
            case BridgeFeature bridge:
                yield return bridge.Shape.Rings;
                break;
            case FidelityPin pin:
                yield return pin.Shape.Rings;
                break;
            case BuildingFeature building:
                foreach (var footprint in building.Footprints)
                {
                    yield return footprint.Rings;
                }

                break;
            case LinearTerrainFeature linear:
                if (linear.Outline is { } outline)
                {
                    yield return outline.Rings;
                }

                if (linear.Centerline is { } centerline)
                {
                    foreach (var piece in FixedGeometry.Stroke(FixedGeometry.Flatten(centerline), linear.Width.Raw, roundJoins: true))
                    {
                        yield return [piece];
                    }
                }

                break;
            case HexsideTerrainFeature hexside:
                foreach (var span in hexside.Spans)
                {
                    yield return FeatureCompiler.HexsideStroke(geometry, span);
                }

                break;
        }
    }

    /// <summary>The feature's pixel bounding box, inclusive, from its painted rings.</summary>
    public static PixelBox? Bounds(BoardGeometry geometry, Feature feature)
    {
        var points = Rings(geometry, feature).SelectMany(rings => rings).SelectMany(ring => ring).ToArray();
        return points.Length == 0
            ? null
            : new PixelBox(points.Min(point => point.X) / 64, points.Min(point => point.Y) / 64, (points.Max(point => point.X) / 64) + 1, (points.Max(point => point.Y) / 64) + 1);
    }

    private static IEnumerable<FixedVector> FeatureVertices(BoardGeometry geometry, Feature feature) => feature switch
    {
        LinearTerrainFeature { Centerline: { } path } => new[] { path.Start }.Concat(path.Segments.Select(segment => segment.End)),
        HexsideTerrainFeature => [],
        _ => Rings(geometry, feature).SelectMany(rings => rings).SelectMany(ring => ring),
    };

    private static bool Covers(IReadOnlyList<IReadOnlyList<FixedVector>> rings, int x, int y)
    {
        var covered = false;
        ShapeRasterizer.Fill(rings.Select(ring => (IReadOnlyList<FixedVector>)ring.Select(point => new FixedVector(point.X - (x * 64), point.Y - (y * 64))).ToArray()),
            1, 1, (_, _) => covered = true);
        return covered;
    }

    private static double SegmentDistance(FixedVector point, FixedVector a, FixedVector b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        var lengthSquared = (dx * dx) + (dy * dy);
        var t = lengthSquared == 0 ? 0 : Math.Clamp((((point.X - a.X) * dx) + ((point.Y - a.Y) * dy)) / lengthSquared, 0, 1);
        var px = a.X + (t * dx) - point.X;
        var py = a.Y + (t * dy) - point.Y;
        return Math.Sqrt((px * px) + (py * py));
    }
}
