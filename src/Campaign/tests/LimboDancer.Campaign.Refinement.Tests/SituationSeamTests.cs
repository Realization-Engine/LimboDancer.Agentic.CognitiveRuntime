using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationSeamTests
{
    [Fact]
    public void AllAdjacentPackagesHaveIdenticalInteriorTerrainAndElevation()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")))
            .ForVersion(PilotInputs.SituationGeneratorVersion);
        var tiles = SituationCoverage.Plan(inputs);
        var previousColumn = new Dictionary<int, (Crop Crop, TerrainGrid Grid)>();
        var compared = 0;
        foreach (var column in tiles.GroupBy(t => t.Crop.Column))
        {
            var currentColumn = new Dictionary<int, (Crop Crop, TerrainGrid Grid)>();
            (Crop Crop, TerrainGrid Grid)? above = null;
            foreach (var tile in column.OrderBy(t => t.Crop.Row))
            {
                var grid = new PilotGenerator(inputs).Generate("00112233445566778899aabbccddeeff", tile.Crop).Grid;
                if (previousColumn.TryGetValue(tile.Crop.Row, out var left) && tile.Crop.Column - left.Crop.Column == SituationCoverage.Columns)
                    compared += Compare(left.Crop, left.Grid, tile.Crop, grid);
                if (above is { } prior && tile.Crop.Row - prior.Crop.Row == SituationCoverage.Rows)
                    compared += Compare(prior.Crop, prior.Grid, tile.Crop, grid);
                currentColumn.Add(tile.Crop.Row, (tile.Crop, grid));
                above = (tile.Crop, grid);
            }
            previousColumn = currentColumn;
        }
        Assert.True(compared > 1_000_000);
    }

    private static int Compare(Crop aCrop, TerrainGrid a, Crop bCrop, TerrainGrid b)
    {
        var dx = checked((int)((bCrop.Column - aCrop.Column) * 56.25));
        var dy = checked((int)((bCrop.Row - aCrop.Row) * 64.5));
        // Exclude one hex of compiler border pixels, but include the actual ownership seam.
        const int border = 65;
        var x0 = Math.Max(border, dx + border); var x1 = Math.Min(a.Geometry.GridWidth - border, dx + b.Geometry.GridWidth - border);
        var y0 = Math.Max(border, dy + border); var y1 = Math.Min(a.Geometry.GridHeight - border, dy + b.Geometry.GridHeight - border);
        Assert.True(x1 > x0 && y1 > y0);
        for (var x = x0; x < x1; x++)
        {
            var ai = x * a.Geometry.GridHeight + y0;
            var bi = (x - dx) * b.Geometry.GridHeight + y0 - dy;
            Assert.True(a.Codes.Slice(ai, y1 - y0).SequenceEqual(b.Codes.Slice(bi, y1 - y0)),
                $"Terrain seam differs between {aCrop} and {bCrop} at pixel column {x}.");
            Assert.True(a.Elevations.Slice(ai, y1 - y0).SequenceEqual(b.Elevations.Slice(bi, y1 - y0)),
                $"Elevation seam differs between {aCrop} and {bCrop} at pixel column {x}.");
        }
        return (x1 - x0) * (y1 - y0);
    }
}
