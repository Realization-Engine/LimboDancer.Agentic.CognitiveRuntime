using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>
/// Builds a Map Lab map from its spec (Map Lab Plan, section 3.2): a rectangle of standard hexes about 1.6 times as wide as it is tall, terrain and
/// levels from seeded value noise (decision L3), and formations of about the spec's group size whose edges wander, as on the Goodwood ASL map.
/// The same spec always gives the same map. No rule reads it.
/// </summary>
public static class LabMapGenerator
{
    private const double Aspect = 1.6;

    public static LabMap Generate(LabMapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var unit = BoardGeometry.StandardGeomorphic;
        var columns = Math.Max(2, (int)Math.Round(Math.Sqrt(spec.Hexes * Aspect * unit.HexHeight / unit.HexWidth)));
        var rows = Math.Max(1, (spec.Hexes - (columns / 2)) / columns);
        var geometry = BoardGeometry.Standard(columns, rows);
        var count = geometry.HexCount;
        var terrain = new LabTerrain[count];
        var levels = new byte[count];
        var parents = new int[count];

        // Formation seeds: one in each square cell of the area of `group` hexes, moved at random within it.
        var side = Math.Sqrt(spec.Group * geometry.HexWidth * geometry.HexHeight);
        var cellColumns = (int)Math.Ceiling(geometry.GridWidth / side) + 1;
        var cellRows = (int)Math.Ceiling(geometry.GridHeight / side) + 1;
        var seed = spec.Seed;

        var ordinal = 0;
        for (var column = 0; column < columns; column++)
        {
            for (var row = 0; row < geometry.RowCount(column); row++)
            {
                // Centres in hex units: a column apart is about 0.87 of a hex height.
                var x = column * geometry.HexWidth;
                var y = (geometry.HexHeight / 2) + (geometry.HexHeight * row) - (geometry.HexHeight / 2 * (column % 2));
                var hx = x / geometry.HexHeight;
                var hy = y / geometry.HexHeight;

                var ground = Fractal(hx / 40, hy / 40, seed);
                var kind = LabTerrain.Open;
                if (ground < 0.2)
                {
                    kind = LabTerrain.Water;
                }
                else if (ground < 0.25)
                {
                    kind = LabTerrain.Marsh;
                }
                else if (Fractal(hx / 9, hy / 9, seed + 101) > 0.6)
                {
                    kind = LabTerrain.Woods;
                }
                else if (Fractal(hx / 5, hy / 5, seed + 202) > 0.74)
                {
                    kind = LabTerrain.Town;
                }

                terrain[ordinal] = kind;
                levels[ordinal] = kind is LabTerrain.Water or LabTerrain.Marsh ? (byte)0 : ground > 0.72 ? (byte)2 : ground > 0.55 ? (byte)1 : (byte)0;
                parents[ordinal] = spec.Group == 1 ? ordinal : Formation(x, y, side, cellColumns, cellRows, seed);
                ordinal++;
            }
        }

        var formations = spec.Group == 1 ? count : CountDistinct(parents, cellColumns * cellRows);
        return new LabMap(spec, geometry, terrain, levels, parents, formations);
    }

    // The formation whose seed is nearest the point, the point first moved by noise so the borders between formations wander.
    private static int Formation(double x, double y, double side, int cellColumns, int cellRows, int seed)
    {
        var px = x + ((Fractal(x / side * 1.5, y / side * 1.5, seed + 303) - 0.5) * side * 0.7);
        var py = y + ((Fractal(x / side * 1.5, y / side * 1.5, seed + 404) - 0.5) * side * 0.7);
        var cx = (int)Math.Floor(px / side);
        var cy = (int)Math.Floor(py / side);
        var best = -1;
        var bestDistance = double.MaxValue;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var ix = cx + dx;
                var iy = cy + dy;
                if (ix < 0 || iy < 0 || ix >= cellColumns || iy >= cellRows)
                {
                    continue;
                }

                var sx = (ix + 0.15 + (0.7 * Hash(ix, iy, seed + 505))) * side;
                var sy = (iy + 0.15 + (0.7 * Hash(ix, iy, seed + 606))) * side;
                var distance = ((sx - px) * (sx - px)) + ((sy - py) * (sy - py));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (iy * cellColumns) + ix;
                }
            }
        }

        return Math.Max(0, best);
    }

    private static int CountDistinct(int[] parents, int bound)
    {
        var seen = new bool[bound];
        var count = 0;
        foreach (var parent in parents)
        {
            if (!seen[parent])
            {
                seen[parent] = true;
                count++;
            }
        }

        return count;
    }

    // Three octaves of value noise, in 0 to 1.
    private static double Fractal(double x, double y, int seed) =>
        ((Noise(x, y, seed) * 4) + (Noise(x * 2, y * 2, seed + 17) * 2) + Noise(x * 4, y * 4, seed + 31)) / 7;

    private static double Noise(double x, double y, int seed)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var fx = Smooth(x - x0);
        var fy = Smooth(y - y0);
        var top = Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), fx);
        var bottom = Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), fx);
        return Lerp(top, bottom, fy);
    }

    private static double Smooth(double t) => t * t * (3 - (2 * t));

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    /// <summary>A repeatable value in 0 to 1 for a lattice point and a seed.</summary>
    private static double Hash(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)((x * 374761393) + (y * 668265263) + (seed * 1274126177));
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (double)0x1000000;
        }
    }
}
