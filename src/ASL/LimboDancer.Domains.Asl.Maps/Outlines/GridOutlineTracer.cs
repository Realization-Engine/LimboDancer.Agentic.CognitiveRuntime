using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Outlines;

/// <summary>
/// Traces a <see cref="TerrainGrid"/> into <see cref="GridOutlines"/>: one region per 4-connected component of cells
/// with the same terrain code and elevation, bounded by rings along pixel edges. Filling every region's rings under
/// the even-odd rule with pixel-center coverage reproduces the grid exactly (<see cref="CellCoverage"/>).
/// </summary>
public static class GridOutlineTracer
{
    // Boundary edges run with the region on their right on screen (y down), which makes outer rings clockwise.
    private enum Direction
    {
        Right,
        Down,
        Left,
        Up,
    }

    public static GridOutlines Trace(TerrainGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var geometry = grid.Geometry;
        var width = geometry.GridWidth;
        var height = geometry.GridHeight;
        var codes = grid.Codes.ToArray();
        var elevations = grid.Elevations.ToArray();
        var labels = new int[width * height];
        var regions = new List<OutlineRegion>();
        var queue = new Queue<int>();

        // Column-major scan, so each component's anchor is its first cell in LOSData order.
        for (var start = 0; start < labels.Length; start++)
        {
            if (labels[start] != 0)
            {
                continue;
            }

            var label = regions.Count + 1;
            var code = codes[start];
            var elevation = elevations[start];
            var cells = new List<int>();
            labels[start] = label;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                cells.Add(cell);
                var x = cell / height;
                var y = cell % height;
                Visit(x - 1, y);
                Visit(x + 1, y);
                Visit(x, y - 1);
                Visit(x, y + 1);
            }

            regions.Add(new OutlineRegion(code, elevation, new GridPoint(start / height, start % height), cells.Count,
                TraceRings(cells, labels, label, width, height)));

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return;
                }

                var index = (x * height) + y;
                if (labels[index] == 0 && codes[index] == code && elevations[index] == elevation)
                {
                    labels[index] = label;
                    queue.Enqueue(index);
                }
            }
        }

        var ordered = regions
            .OrderBy(region => region.Code)
            .ThenBy(region => region.Elevation)
            .ThenBy(region => region.AnchorCell.X)
            .ThenBy(region => region.AnchorCell.Y)
            .ToArray();
        return new GridOutlines(geometry, ordered);
    }

    private static List<OutlineRing> TraceRings(List<int> cells, int[] labels, int label, int width, int height)
    {
        // Unit boundary edges keyed by start vertex. A vertex has at most two outgoing edges of one component.
        var outgoing = new Dictionary<(int X, int Y), List<(int X, int Y, Direction Direction)>>();
        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && labels[(x * height) + y] == label;
        void Add(int x0, int y0, int x1, int y1, Direction direction)
        {
            if (!outgoing.TryGetValue((x0, y0), out var list))
            {
                list = [];
                outgoing[(x0, y0)] = list;
            }

            list.Add((x1, y1, direction));
        }

        foreach (var cell in cells)
        {
            var x = cell / height;
            var y = cell % height;
            if (!Inside(x, y - 1))
            {
                Add(x, y, x + 1, y, Direction.Right);
            }

            if (!Inside(x + 1, y))
            {
                Add(x + 1, y, x + 1, y + 1, Direction.Down);
            }

            if (!Inside(x, y + 1))
            {
                Add(x + 1, y + 1, x, y + 1, Direction.Left);
            }

            if (!Inside(x - 1, y))
            {
                Add(x, y + 1, x, y, Direction.Up);
            }
        }

        var rings = new List<OutlineRing>();
        while (outgoing.Count > 0)
        {
            var start = outgoing.Keys.First();
            var path = new List<(int X, int Y)>();
            var directions = new List<Direction>();
            var current = start;
            Direction? previous = null;
            do
            {
                var edges = outgoing[current];

                // At a pinch vertex the region touches itself diagonally; turning right keeps the rings of a
                // 4-connected region separate.
                var index = edges.Count == 1 || previous is null ? 0 : PreferRightTurn(edges, previous.Value);
                var edge = edges[index];
                edges.RemoveAt(index);
                if (edges.Count == 0)
                {
                    outgoing.Remove(current);
                }

                path.Add(current);
                directions.Add(edge.Direction);
                previous = edge.Direction;
                current = (edge.X, edge.Y);
            }
            while (current != start);

            rings.Add(Simplify(path, directions));
        }

        // Canonical order: outer rings before holes, then by top-left vertex.
        return rings
            .OrderBy(ring => ring.IsHole)
            .ThenBy(ring => ring.Vertices[0].Y)
            .ThenBy(ring => ring.Vertices[0].X)
            .ToList();
    }

    private static int PreferRightTurn(List<(int X, int Y, Direction Direction)> edges, Direction previous)
    {
        var right = (Direction)(((int)previous + 1) % 4);
        var index = edges.FindIndex(edge => edge.Direction == right);
        return index >= 0 ? index : 0;
    }

    // Keeps only turning vertices, starts at the top-left vertex, and classifies the ring by its winding.
    private static OutlineRing Simplify(List<(int X, int Y)> path, List<Direction> directions)
    {
        var vertices = new List<GridPoint>();
        for (var index = 0; index < path.Count; index++)
        {
            var incoming = directions[(index + path.Count - 1) % path.Count];
            if (incoming != directions[index])
            {
                vertices.Add(new GridPoint(path[index].X, path[index].Y));
            }
        }

        var first = 0;
        for (var index = 1; index < vertices.Count; index++)
        {
            if (vertices[index].Y < vertices[first].Y || (vertices[index].Y == vertices[first].Y && vertices[index].X < vertices[first].X))
            {
                first = index;
            }
        }

        var rotated = vertices.Skip(first).Concat(vertices.Take(first)).ToArray();
        return new OutlineRing(rotated, SignedArea(rotated) < 0);
    }

    // Shoelace sum; positive for clockwise rings on screen (y down).
    private static long SignedArea(GridPoint[] vertices)
    {
        long sum = 0;
        for (var index = 0; index < vertices.Length; index++)
        {
            var a = vertices[index];
            var b = vertices[(index + 1) % vertices.Length];
            sum += ((long)a.X * b.Y) - ((long)b.X * a.Y);
        }

        return sum;
    }
}
