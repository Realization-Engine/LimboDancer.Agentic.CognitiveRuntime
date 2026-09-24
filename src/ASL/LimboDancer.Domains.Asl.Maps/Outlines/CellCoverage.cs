using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Outlines;

/// <summary>
/// Fills rectilinear integer rings onto a grid with the Model Design's coverage rule: a cell is covered when its
/// center lies inside under the even-odd rule. Used to prove that outlines reproduce the grid exactly.
/// </summary>
public static class CellCoverage
{
    /// <summary>Column-major coverage flags (index = x * height + y) for the given rings.</summary>
    public static bool[] Fill(IEnumerable<IReadOnlyList<GridPoint>> rings, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(rings);
        var covered = new bool[width * height];
        var crossingsByRow = new List<int>[height];
        foreach (var ring in rings)
        {
            for (var index = 0; index < ring.Count; index++)
            {
                var a = ring[index];
                var b = ring[(index + 1) % ring.Count];
                if (a.X != b.X)
                {
                    continue;
                }

                // A vertical edge at x crosses the row centers y + 0.5 for rows between its end points.
                for (var y = Math.Max(0, Math.Min(a.Y, b.Y)); y < Math.Min(height, Math.Max(a.Y, b.Y)); y++)
                {
                    (crossingsByRow[y] ??= []).Add(a.X);
                }
            }
        }

        for (var y = 0; y < height; y++)
        {
            var crossings = crossingsByRow[y];
            if (crossings is null)
            {
                continue;
            }

            crossings.Sort();
            for (var pair = 0; pair + 1 < crossings.Count; pair += 2)
            {
                for (var x = Math.Max(0, crossings[pair]); x < Math.Min(width, crossings[pair + 1]); x++)
                {
                    covered[(x * height) + y] = true;
                }
            }
        }

        return covered;
    }
}
