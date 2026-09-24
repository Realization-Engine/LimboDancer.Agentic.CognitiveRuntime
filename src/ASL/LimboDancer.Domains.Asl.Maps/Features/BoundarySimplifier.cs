using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Outlines;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>
/// A labeled image traced once and simplified on demand with Douglas-Peucker (Model Design section 7.3). Boundaries are
/// split into chains at junctions (pixel corners where three or more labels meet, or two meet diagonally). Each chain
/// is simplified once and shared by the two regions on either side, so simplification never opens gaps or overlaps
/// between neighbors. Simplified vertices are a subset of the pixel-corner vertices, so they stay exact in fixed point.
/// </summary>
public sealed class BoundarySimplifier
{
    private readonly int[] labels;
    private readonly int width;
    private readonly int height;
    private readonly List<TracedRegion> regions = [];
    private readonly Dictionary<ChainKey, Chain> chains = [];

    /// <summary>Traces every region of <paramref name="labels"/> (column-major, values 0 to 255).</summary>
    public BoundarySimplifier(BoardGeometry geometry, int[] labels)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(labels);
        this.labels = labels;
        width = geometry.GridWidth;
        height = geometry.GridHeight;
        var codes = new byte[labels.Length];
        for (var cell = 0; cell < labels.Length; cell++)
        {
            codes[cell] = checked((byte)labels[cell]);
        }

        var outlines = GridOutlineTracer.Trace(new TerrainGrid(geometry, codes, new sbyte[codes.Length], new bool[geometry.HexCount]));
        foreach (var region in outlines.Regions)
        {
            var rings = region.Rings.Select(ring => (SplitRing(ring.Vertices), ring.IsHole)).ToArray();
            regions.Add(new TracedRegion(region.Code, region.AnchorCell, region.CellCount, rings));
        }
    }

    public IReadOnlyList<TracedRegion> Regions => regions;

    /// <summary>
    /// The region's rings simplified with the tolerance each chain's bounding box receives from
    /// <paramref name="tolerance"/>, in pixels. Rings that collapse below three vertices are dropped.
    /// </summary>
    public FeatureShape? Simplify(TracedRegion region, Func<PixelBox, double> tolerance)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(tolerance);
        var rings = new List<IReadOnlyList<FixedVector>>();
        foreach (var (ring, isHole) in region.Rings)
        {
            var points = new List<GridPoint>();
            foreach (var (key, reversed) in ring)
            {
                var chain = chains[key];
                var simplified = chain.Simplified(tolerance(chain.Box));
                var sequence = reversed ? simplified.Reverse() : simplified;
                foreach (var point in sequence)
                {
                    if (points.Count == 0 || points[^1] != point)
                    {
                        points.Add(point);
                    }
                }
            }

            if (points.Count > 1 && points[^1] == points[0])
            {
                points.RemoveAt(points.Count - 1);
            }

            if (points.Count >= 3)
            {
                rings.Add(points.Select(point => FixedVector.FromPixels(point.X, point.Y)).ToArray());
            }
            else if (!isHole)
            {
                // The outer ring collapsed, so the region is dropped with its holes.
                return null;
            }
        }

        return rings.Count == 0 ? null : new FeatureShape(rings);
    }

    // Expands a turn-vertex ring to every pixel corner, finds junctions, and splits it into chains.
    private List<(ChainKey Key, bool Reversed)> SplitRing(IReadOnlyList<GridPoint> vertices)
    {
        var lattice = new List<GridPoint>();
        for (var index = 0; index < vertices.Count; index++)
        {
            var from = vertices[index];
            var to = vertices[(index + 1) % vertices.Count];
            var dx = Math.Sign(to.X - from.X);
            var dy = Math.Sign(to.Y - from.Y);
            var point = from;
            while (point != to)
            {
                lattice.Add(point);
                point = new GridPoint(point.X + dx, point.Y + dy);
            }
        }

        var junctions = new List<int>();
        for (var index = 0; index < lattice.Count; index++)
        {
            if (IsJunction(lattice[index]))
            {
                junctions.Add(index);
            }
        }

        if (junctions.Count == 0)
        {
            // A closed loop between two labels: anchor it at its smallest corner, which both sides agree on.
            var anchor = 0;
            for (var index = 1; index < lattice.Count; index++)
            {
                if (Compare(lattice[index], lattice[anchor]) < 0)
                {
                    anchor = index;
                }
            }

            junctions.Add(anchor);
        }

        var result = new List<(ChainKey, bool)>();
        for (var index = 0; index < junctions.Count; index++)
        {
            var start = junctions[index];
            var end = index + 1 < junctions.Count ? junctions[index + 1] : junctions[0] + lattice.Count;
            var points = new List<GridPoint>(end - start + 1);
            for (var position = start; position <= end; position++)
            {
                points.Add(lattice[position % lattice.Count]);
            }

            result.Add(Register(points));
        }

        return result;
    }

    private (ChainKey Key, bool Reversed) Register(List<GridPoint> points)
    {
        var reversed = Compare(points[^1], points[0]) < 0 || (points[^1] == points[0] && Compare(points[^2], points[1]) < 0);
        if (reversed)
        {
            points.Reverse();
        }

        var key = new ChainKey(points[0], points[1], points[^1]);
        if (!chains.ContainsKey(key))
        {
            chains[key] = new Chain(points);
        }

        return (key, reversed);
    }

    private bool IsJunction(GridPoint corner)
    {
        var a = LabelAt(corner.X - 1, corner.Y - 1);
        var b = LabelAt(corner.X, corner.Y - 1);
        var c = LabelAt(corner.X - 1, corner.Y);
        var d = LabelAt(corner.X, corner.Y);
        var distinct = 1 + (b != a ? 1 : 0) + (c != a && c != b ? 1 : 0) + (d != a && d != b && d != c ? 1 : 0);
        return distinct >= 3 || (distinct == 2 && a == d && b == c && a != b);
    }

    private int LabelAt(int x, int y) => x < 0 || y < 0 || x >= width || y >= height ? -1 : labels[(x * height) + y];

    private static int Compare(GridPoint left, GridPoint right) => left.X != right.X ? left.X.CompareTo(right.X) : left.Y.CompareTo(right.Y);

    private sealed class Chain
    {
        private readonly GridPoint[] turns;
        private readonly Dictionary<double, GridPoint[]> cache = [];

        public Chain(List<GridPoint> points)
        {
            var turnPoints = new List<GridPoint> { points[0] };
            for (var index = 1; index < points.Count - 1; index++)
            {
                var previous = points[index - 1];
                var next = points[index + 1];
                if ((points[index].X - previous.X) != (next.X - points[index].X) || (points[index].Y - previous.Y) != (next.Y - points[index].Y))
                {
                    turnPoints.Add(points[index]);
                }
            }

            turnPoints.Add(points[^1]);
            turns = [.. turnPoints];
            Box = new PixelBox(turns.Min(point => point.X), turns.Min(point => point.Y), turns.Max(point => point.X), turns.Max(point => point.Y));
        }

        public PixelBox Box
        {
            get;
        }

        public GridPoint[] Simplified(double tolerance)
        {
            if (tolerance <= 0)
            {
                return turns;
            }

            if (cache.TryGetValue(tolerance, out var cached))
            {
                return cached;
            }

            var keep = new bool[turns.Length];
            keep[0] = true;
            keep[^1] = true;
            if (turns[0] == turns[^1])
            {
                // A closed loop: split at the vertex farthest from the anchor and simplify both halves.
                var far = 1;
                for (var index = 2; index < turns.Length - 1; index++)
                {
                    if (DistanceSquared(turns[index], turns[0]) > DistanceSquared(turns[far], turns[0]))
                    {
                        far = index;
                    }
                }

                keep[far] = true;
                DouglasPeucker(turns, 0, far, tolerance * tolerance, keep);
                DouglasPeucker(turns, far, turns.Length - 1, tolerance * tolerance, keep);
            }
            else
            {
                DouglasPeucker(turns, 0, turns.Length - 1, tolerance * tolerance, keep);
            }

            var result = turns.Where((_, index) => keep[index]).ToArray();
            cache[tolerance] = result;
            return result;
        }

        private static void DouglasPeucker(GridPoint[] points, int first, int last, double toleranceSquared, bool[] keep)
        {
            var stack = new Stack<(int First, int Last)>();
            stack.Push((first, last));
            while (stack.Count > 0)
            {
                var (from, to) = stack.Pop();
                if (to - from < 2)
                {
                    continue;
                }

                var farthest = -1;
                var farthestDistance = -1.0;
                for (var index = from + 1; index < to; index++)
                {
                    var distance = SegmentDistanceSquared(points[index], points[from], points[to]);
                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        farthest = index;
                    }
                }

                if (farthestDistance > toleranceSquared)
                {
                    keep[farthest] = true;
                    stack.Push((from, farthest));
                    stack.Push((farthest, to));
                }
            }
        }

        private static long DistanceSquared(GridPoint a, GridPoint b) => ((long)(a.X - b.X) * (a.X - b.X)) + ((long)(a.Y - b.Y) * (a.Y - b.Y));

        private static double SegmentDistanceSquared(GridPoint point, GridPoint a, GridPoint b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            var lengthSquared = (dx * dx) + (dy * dy);
            if (lengthSquared == 0)
            {
                return DistanceSquared(point, a);
            }

            var t = Math.Clamp((((point.X - a.X) * dx) + ((point.Y - a.Y) * dy)) / lengthSquared, 0, 1);
            var px = a.X + (t * dx) - point.X;
            var py = a.Y + (t * dy) - point.Y;
            return (px * px) + (py * py);
        }
    }
}

/// <summary>Identifies a boundary chain by its first two and last pixel corners in canonical direction.</summary>
internal readonly record struct ChainKey(GridPoint First, GridPoint Second, GridPoint Last);

/// <summary>An inclusive pixel-corner bounding box.</summary>
public readonly record struct PixelBox(int MinX, int MinY, int MaxX, int MaxY)
{
    public bool Intersects(PixelBox other) => MinX <= other.MaxX && other.MinX <= MaxX && MinY <= other.MaxY && other.MinY <= MaxY;
}

/// <summary>One traced region: its label, first cell, size, and rings as references to shared chains.</summary>
public sealed class TracedRegion
{
    internal TracedRegion(byte label, GridPoint anchorCell, int cellCount, IReadOnlyList<(List<(ChainKey Key, bool Reversed)> Chains, bool IsHole)> rings)
    {
        Label = label;
        AnchorCell = anchorCell;
        CellCount = cellCount;
        Rings = rings;
    }

    public byte Label
    {
        get;
    }

    public GridPoint AnchorCell
    {
        get;
    }

    public int CellCount
    {
        get;
    }

    internal IReadOnlyList<(List<(ChainKey Key, bool Reversed)> Chains, bool IsHole)> Rings
    {
        get;
    }
}
