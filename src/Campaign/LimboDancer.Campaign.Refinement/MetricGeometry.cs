namespace LimboDancer.Campaign.Refinement;

public static class MetricGeometry
{
    public static bool Contains(IReadOnlyList<MetricPoint> polygon, MetricPoint p)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i]; var b = polygon[j];
            if (DistanceSquared(p, a, b) == 0) return true;
            // Cross multiplication avoids division and preserves exact integer boundary decisions.
            if ((a.Y > p.Y) != (b.Y > p.Y))
            {
                var left = (Int128)(p.X - a.X) * (b.Y - a.Y);
                var right = (Int128)(b.X - a.X) * (p.Y - a.Y);
                if (b.Y > a.Y ? left < right : left > right) inside = !inside;
            }
        }
        return inside;
    }
    public static decimal DistanceSquared(MetricPoint p, MetricPoint a, MetricPoint b)
    {
        decimal dx = b.X - a.X, dy = b.Y - a.Y;
        var length = dx * dx + dy * dy;
        var t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length, 0, 1);
        var x = p.X - a.X - t * dx; var y = p.Y - a.Y - t * dy;
        return x * x + y * y;
    }
    public static MetricPoint[] Box(MetricPoint center, int size) =>
        [new(center.X-size/2,center.Y-size/2), new(center.X+size/2,center.Y-size/2),
         new(center.X+size/2,center.Y+size/2), new(center.X-size/2,center.Y+size/2)];
    public static bool Near(IReadOnlyList<MetricPoint> points, MetricPoint p, long distance, bool closed)
    {
        ArgumentNullException.ThrowIfNull(points);
        var count = closed ? points.Count : points.Count - 1;
        for (var i = 0; i < count; i++)
            if (DistanceSquared(p, points[i], points[(i + 1) % points.Count]) <= (decimal)distance * distance) return true;
        return false;
    }
    public static MetricPoint[] Hex(MetricPoint center) =>
        [new(center.X+23094,center.Y), new(center.X+11547,center.Y+20000),
         new(center.X-11547,center.Y+20000), new(center.X-23094,center.Y),
         new(center.X-11547,center.Y-20000), new(center.X+11547,center.Y-20000)];

    public static bool Overlaps(IReadOnlyList<MetricPoint> a, IReadOnlyList<MetricPoint> b)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if (a.Max(p => p.X) < b.Min(p => p.X) || b.Max(p => p.X) < a.Min(p => p.X) ||
            a.Max(p => p.Y) < b.Min(p => p.Y) || b.Max(p => p.Y) < a.Min(p => p.Y)) return false;
        if (a.Any(p => Contains(b, p)) || b.Any(p => Contains(a, p))) return true;
        static Int128 Cross(MetricPoint p, MetricPoint q, MetricPoint r) =>
            (Int128)(q.X - p.X) * (r.Y - p.Y) - (Int128)(q.Y - p.Y) * (r.X - p.X);
        for (var i = 0; i < a.Count; i++) for (var j = 0; j < b.Count; j++)
        {
            var p = a[i]; var q = a[(i + 1) % a.Count]; var r = b[j]; var t = b[(j + 1) % b.Count];
            var x = Cross(p, q, r); var y = Cross(p, q, t); var u = Cross(r, t, p); var v = Cross(r, t, q);
            if (((x < 0 && y > 0) || (x > 0 && y < 0)) && ((u < 0 && v > 0) || (u > 0 && v < 0))) return true;
        }
        return false;
    }

    public static bool Intersects(IReadOnlyList<MetricPoint> points, Crop crop, long margin = 0)
    {
        ArgumentNullException.ThrowIfNull(points); ArgumentNullException.ThrowIfNull(crop);
        var left = SpatialFrame.ColumnMm * crop.Column; var right = SpatialFrame.ColumnMm * (crop.Column + crop.Width - 1);
        var top = crop.Row * 40000L - 20000; var bottom = (crop.Row + crop.Height) * 40000L - 20000;
        return points.Max(p => p.X) >= left - margin && points.Min(p => p.X) <= right + margin
            && points.Max(p => p.Y) >= top - margin && points.Min(p => p.Y) <= bottom + margin;
    }
}
