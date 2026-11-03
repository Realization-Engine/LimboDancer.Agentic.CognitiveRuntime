using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

/// <summary>Screen-Bounded Map Analysis, section 3.1: the grid and the Formation boundaries painted into tiles.</summary>
public sealed class TileLinesTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Standard(6, 5);

    [Fact]
    public void FindsTheHexWhoseCentreIsNearest()
    {
        var random = new Random(5);
        for (var i = 0; i < 2_000; i++)
        {
            var x = random.NextDouble() * (Geometry.GridWidth - 2);
            var y = random.NextDouble() * (Geometry.GridHeight - 1);
            var expected = Geometry.HexAt(x, y)!.Value;
            var found = Geometry.Hexes().ElementAt(TileLinePainter.OrdinalAt(Geometry, x, y));
            if (found != expected)
            {
                // On an edge two centres are equally near: either answer is right.
                Assert.Equal(Distance(Geometry.CenterDot(expected), x, y), Distance(Geometry.CenterDot(found), x, y), 6);
            }
        }
    }

    [Fact]
    public void ShowsEachLineOnlyWhereItIsWideEnough()
    {
        // A standard hex is 56.25 cells wide: six pixels or more up to level 3 (7.03), under six at level 4 (3.5).
        Assert.Equal(TileLines.Grid, TileLinePainter.Shown(Geometry, TileLines.Grid, 3, 1));
        Assert.Equal(TileLines.None, TileLinePainter.Shown(Geometry, TileLines.Grid, 4, 1));
        // A formation of 36 hexes is about six hexes across: 21 pixels at level 4, 5.3 at level 6.
        Assert.Equal(TileLines.Boundaries, TileLinePainter.Shown(Geometry, TileLines.Grid | TileLines.Boundaries, 4, 36));
        Assert.Equal(TileLines.None, TileLinePainter.Shown(Geometry, TileLines.Boundaries, 6, 36));
    }

    [Fact]
    public void PaintsBoundariesBetweenParentsAndTheGridWithinOne()
    {
        // Two columns of parents: the first three columns are parent 0, the rest parent 1.
        var parents = Geometry.Hexes().Select(hex => hex.Column < 3 ? 0 : 1).ToArray();
        var width = Geometry.GridWidth;
        var height = Geometry.GridHeight;
        var plain = Enumerable.Repeat((byte)200, width * height * 3).ToArray();
        var grid = (byte[])plain.Clone();
        TileLinePainter.Paint(Geometry, TileLines.Grid, [], 0, 0, 1, grid, width, height);
        var both = (byte[])plain.Clone();
        TileLinePainter.Paint(Geometry, TileLines.Grid | TileLines.Boundaries, parents, 0, 0, 1, both, width, height);

        var gridPixels = Count(grid, pixel => pixel == (110, 110, 110));
        var boundaryPixels = Count(both, pixel => pixel == (0x9b, 0x5f, 0x17));
        Assert.True(gridPixels > 0);
        Assert.True(boundaryPixels > 0);
        // A boundary pixel is a grid pixel that the parents turn: none appears off the grid's lines.
        Assert.Equal(gridPixels, Count(both, pixel => pixel == (110, 110, 110)) + boundaryPixels);
        // The boundary runs between columns 2 and 3 only: no boundary pixel lies left of column 2's centre or right of column 3's.
        for (var i = 0; i < width * height; i++)
        {
            if ((both[i * 3], both[(i * 3) + 1], both[(i * 3) + 2]) == (0x9b, 0x5f, 0x17))
            {
                Assert.InRange(i % width, (int)(2 * Geometry.HexWidth), (int)Math.Ceiling(3 * Geometry.HexWidth));
            }
        }
    }

    private static int Count(byte[] pixels, Func<(byte, byte, byte), bool> match)
    {
        var count = 0;
        for (var i = 0; i < pixels.Length; i += 3)
        {
            if (match((pixels[i], pixels[i + 1], pixels[i + 2])))
            {
                count++;
            }
        }

        return count;
    }

    private static double Distance(PixelPoint centre, double x, double y) => Math.Sqrt(((centre.X - x) * (centre.X - x)) + ((centre.Y - y) * (centre.Y - y)));
}
