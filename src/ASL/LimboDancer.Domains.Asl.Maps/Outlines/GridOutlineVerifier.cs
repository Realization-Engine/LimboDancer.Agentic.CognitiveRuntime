using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Outlines;

/// <summary>The result of refilling outlines onto the grid: mismatched cells and the first one in column-major order.</summary>
public sealed record OutlineVerification(int MismatchedCells, GridPoint? FirstMismatch)
{
    public bool Lossless => MismatchedCells == 0;
}

/// <summary>
/// Proves that outlines reproduce their grid (Model Design section 4.4). All rings of one (code, elevation) are
/// filled together under the even-odd rule, as the Exact view draws them. A cell mismatches when no key covers it,
/// when more than one key covers it, or when the covering key differs from the grid.
/// </summary>
public static class GridOutlineVerifier
{
    public static OutlineVerification Verify(TerrainGrid grid, GridOutlines outlines)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(outlines);
        var width = grid.Geometry.GridWidth;
        var height = grid.Geometry.GridHeight;
        var coverCount = new byte[grid.CellCount];
        var mismatched = new bool[grid.CellCount];
        foreach (var key in outlines.Regions.GroupBy(region => (region.Code, region.Elevation)))
        {
            var covered = CellCoverage.Fill(key.SelectMany(region => region.Rings).Select(ring => ring.Vertices), width, height);
            for (var cell = 0; cell < covered.Length; cell++)
            {
                if (!covered[cell])
                {
                    continue;
                }

                if (coverCount[cell] < byte.MaxValue)
                {
                    coverCount[cell]++;
                }

                if (grid.Codes[cell] != key.Key.Code || grid.Elevations[cell] != key.Key.Elevation)
                {
                    mismatched[cell] = true;
                }
            }
        }

        var count = 0;
        GridPoint? first = null;
        for (var cell = 0; cell < mismatched.Length; cell++)
        {
            if (mismatched[cell] || coverCount[cell] != 1)
            {
                count++;
                first ??= new GridPoint(cell / height, cell % height);
            }
        }

        return new OutlineVerification(count, first);
    }
}
