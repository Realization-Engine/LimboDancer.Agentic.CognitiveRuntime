namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>
/// Fills fixed-point rings onto the pixel grid with the coverage rule of Model Design section 5.3: a cell is covered
/// when its center lies inside under the nonzero winding rule. Integer arithmetic only, so results are identical on
/// every platform.
/// </summary>
public static class ShapeRasterizer
{
    private const int Unit = 64;
    private const int Half = 32;

    /// <summary>Calls <paramref name="cover"/> once for each covered cell (x, y) inside the grid bounds.</summary>
    public static void Fill(IEnumerable<IReadOnlyList<FixedVector>> rings, int width, int height, Action<int, int> cover)
    {
        ArgumentNullException.ThrowIfNull(rings);
        ArgumentNullException.ThrowIfNull(cover);
        var crossings = new List<(int Row, int Cell, int Direction)>();
        foreach (var ring in rings)
        {
            for (var index = 0; index < ring.Count; index++)
            {
                AddEdge(ring[index], ring[(index + 1) % ring.Count], height, crossings);
            }
        }

        crossings.Sort();
        var start = 0;
        while (start < crossings.Count)
        {
            var row = crossings[start].Row;
            var end = start;
            while (end < crossings.Count && crossings[end].Row == row)
            {
                end++;
            }

            var winding = 0;
            for (var index = start; index < end - 1; index++)
            {
                winding += crossings[index].Direction;
                if (winding == 0)
                {
                    continue;
                }

                var from = Math.Max(0, crossings[index].Cell);
                var to = Math.Min(width, crossings[index + 1].Cell);
                for (var x = from; x < to; x++)
                {
                    cover(x, row);
                }
            }

            start = end;
        }
    }

    /// <summary>Coverage as column-major flags (index = x * height + y), for tests and diagnostics.</summary>
    public static bool[] Coverage(IEnumerable<IReadOnlyList<FixedVector>> rings, int width, int height)
    {
        var covered = new bool[width * height];
        Fill(rings, width, height, (x, y) => covered[(x * height) + y] = true);
        return covered;
    }

    private static void AddEdge(FixedVector a, FixedVector b, int height, List<(int Row, int Cell, int Direction)> crossings)
    {
        if (a.Y == b.Y)
        {
            return;
        }

        var direction = a.Y < b.Y ? 1 : -1;
        var (low, high) = a.Y < b.Y ? (a, b) : (b, a);

        // Rows whose center y * 64 + 32 lies in [low.Y, high.Y).
        var firstRow = Math.Max(0, CeilDiv(low.Y - Half, Unit));
        var lastRow = Math.Min(height - 1, CeilDiv(high.Y - Half, Unit) - 1);
        long dy = high.Y - low.Y;
        long dx = high.X - low.X;
        for (var row = firstRow; row <= lastRow; row++)
        {
            long center = ((long)row * Unit) + Half;

            // The crossing is at x = low.X + (center - low.Y) * dx / dy; the first covered cell k has k * 64 + 32 >= x.
            var numerator = ((long)low.X * dy) + ((center - low.Y) * dx) - (Half * dy);
            var cell = CeilDiv(numerator, Unit * dy);
            crossings.Add((row, (int)Math.Clamp(cell, int.MinValue / 2, int.MaxValue / 2), direction));
        }
    }

    internal static int CeilDiv(int numerator, int denominator) => (int)CeilDiv((long)numerator, denominator);

    internal static long CeilDiv(long numerator, long denominator) =>
        numerator >= 0 ? (numerator + denominator - 1) / denominator : -(-numerator / denominator);
}

/// <summary>Deterministic integer geometry for flattening curves and building stroke polygons.</summary>
public static class FixedGeometry
{
    // cos(k * 22.5 degrees) * 65536, k = 0..15; sin is the same table shifted by four.
    private static readonly int[] Cosines =
    [
        65536, 60547, 46341, 25080, 0, -25080, -46341, -60547, -65536, -60547, -46341, -25080, 0, 25080, 46341, 60547,
    ];

    /// <summary>The integer square root, floor(sqrt(value)).</summary>
    public static long Sqrt(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        // Corrected in integers without squaring, so values near long.MaxValue cannot overflow.
        var root = (long)Math.Sqrt(value);
        while (root > 0 && root > value / root)
        {
            root--;
        }

        while (root + 1 <= value / (root + 1))
        {
            root++;
        }

        return root;
    }

    public static long Length(FixedVector from, FixedVector to)
    {
        long dx = to.X - from.X;
        long dy = to.Y - from.Y;
        return Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// Flattens a centerline into points. A cubic segment is subdivided into a fixed count derived from its control
    /// polygon length (one step per 4 pixels, 1 to 128 steps), evaluated with integer Bernstein weights.
    /// </summary>
    public static IReadOnlyList<FixedVector> Flatten(CenterlinePath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var points = new List<FixedVector> { path.Start };
        var current = path.Start;
        foreach (var segment in path.Segments)
        {
            if (!segment.IsCurve)
            {
                points.Add(segment.End);
                current = segment.End;
                continue;
            }

            var p1 = segment.Control1!.Value;
            var p2 = segment.Control2!.Value;
            var p3 = segment.End;
            var controlLength = Length(current, p1) + Length(p1, p2) + Length(p2, p3);
            var steps = (int)Math.Clamp(ShapeRasterizer.CeilDiv(controlLength, 4 * 64), 1, 128);
            long n = steps;
            var cube = n * n * n;
            for (long i = 1; i <= n; i++)
            {
                var j = n - i;
                long w0 = j * j * j;
                long w1 = 3 * j * j * i;
                long w2 = 3 * j * i * i;
                long w3 = i * i * i;
                points.Add(new FixedVector(
                    RoundDiv((w0 * current.X) + (w1 * p1.X) + (w2 * p2.X) + (w3 * p3.X), cube),
                    RoundDiv((w0 * current.Y) + (w1 * p1.Y) + (w2 * p2.Y) + (w3 * p3.Y), cube)));
            }

            current = p3;
        }

        return points;
    }

    /// <summary>
    /// Polygons whose union is the stroke of a polyline: one quad per segment (butt ends) and, when
    /// <paramref name="roundJoins"/> is set, a 16-sided disk at every vertex for round joins and caps.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<FixedVector>> Stroke(IReadOnlyList<FixedVector> points, int width, bool roundJoins)
    {
        ArgumentNullException.ThrowIfNull(points);
        var pieces = new List<IReadOnlyList<FixedVector>>();
        var radius = width / 2;
        for (var index = 0; index + 1 < points.Count; index++)
        {
            if (Quad(points[index], points[index + 1], radius) is { } quad)
            {
                pieces.Add(quad);
            }
        }

        if (roundJoins && radius > 0)
        {
            foreach (var point in points)
            {
                pieces.Add(Disk(point, radius));
            }
        }

        return pieces;
    }

    /// <summary>A rectangle of half-width <paramref name="radius"/> centered on the segment, with butt ends.</summary>
    public static IReadOnlyList<FixedVector>? Quad(FixedVector from, FixedVector to, int radius)
    {
        long dx = to.X - from.X;
        long dy = to.Y - from.Y;
        var length = Sqrt((dx * dx) + (dy * dy));
        if (length == 0 || radius <= 0)
        {
            return null;
        }

        var nx = (int)RoundDiv(-dy * radius, length);
        var ny = (int)RoundDiv(dx * radius, length);
        return
        [
            new FixedVector(from.X + nx, from.Y + ny),
            new FixedVector(to.X + nx, to.Y + ny),
            new FixedVector(to.X - nx, to.Y - ny),
            new FixedVector(from.X - nx, from.Y - ny),
        ];
    }

    public static IReadOnlyList<FixedVector> Disk(FixedVector center, int radius)
    {
        var points = new FixedVector[16];
        for (var k = 0; k < 16; k++)
        {
            points[k] = new FixedVector(
                center.X + (int)RoundDiv((long)radius * Cosines[k], 65536),
                center.Y + (int)RoundDiv((long)radius * Cosines[(k + 12) % 16], 65536));
        }

        return points;
    }

    /// <summary>Division rounded half away from zero.</summary>
    public static int RoundDiv(long numerator, long denominator)
    {
        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        var result = numerator >= 0 ? (numerator + (denominator / 2)) / denominator : -((-numerator + (denominator / 2)) / denominator);
        return (int)result;
    }
}
