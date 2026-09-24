using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Outlines;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class ShapeRasterizerTests
{
    [Fact]
    public void WholePixelRectanglesCoverExactlyTheirCells()
    {
        var covered = Cells(FeatureTestCatalog.Box(10, 10, 20, 15).Rings, 40, 30);
        Assert.Equal(50, covered.Count);
        Assert.Contains((10, 10), covered);
        Assert.Contains((19, 14), covered);
        Assert.DoesNotContain((20, 14), covered);
        Assert.DoesNotContain((19, 15), covered);
    }

    [Fact]
    public void CenterOnTheLeftEdgeIsInsideAndOnTheRightEdgeIsOutside()
    {
        // Edges at x = 10.5 and x = 20.5 pass through cell centers: the left one covers cell 10, the right one not cell 20.
        var shape = FeatureShape.Rectangle(new FixedVector(10 * 64 + 32, 0), new FixedVector(20 * 64 + 32, 64));
        var covered = Cells(shape.Rings, 40, 30);
        Assert.Equal(Enumerable.Range(10, 10).Select(x => (x, 0)), covered.Order());
    }

    [Fact]
    public void HolesRunningTheOtherWayAreEmptyUnderNonzeroWinding()
    {
        FixedVector[] outer = [FixedVector.FromPixels(0, 0), FixedVector.FromPixels(10, 0), FixedVector.FromPixels(10, 10), FixedVector.FromPixels(0, 10)];
        FixedVector[] hole = [FixedVector.FromPixels(3, 3), FixedVector.FromPixels(3, 7), FixedVector.FromPixels(7, 7), FixedVector.FromPixels(7, 3)];
        FixedVector[] sameWay = [FixedVector.FromPixels(3, 3), FixedVector.FromPixels(7, 3), FixedVector.FromPixels(7, 7), FixedVector.FromPixels(3, 7)];
        Assert.Equal(84, Cells([outer, hole], 20, 20).Count);
        Assert.Equal(100, Cells([outer, sameWay], 20, 20).Count);
    }

    [Fact]
    public void TracedRingsFillExactlyLikeTheEvenOddCoverageRule()
    {
        var random = new Random(9);
        var geometry = BoardGeometry.Standard(3, 2);
        var grid = GridOutlineTracerTests.Build(geometry, (x, y) => random.Next(12) == 0 ? (byte)2 : (byte)(((x / 9) + (y / 7)) % 3));
        foreach (var region in GridOutlineTracer.Trace(grid).Regions.Take(40))
        {
            var rings = region.Rings.Select(ring => ring.Vertices).ToArray();
            var expected = CellCoverage.Fill(rings, geometry.GridWidth, geometry.GridHeight);
            var actual = ShapeRasterizer.Coverage(rings.Select(ring => (IReadOnlyList<FixedVector>)ring.Select(point => FixedVector.FromPixels(point.X, point.Y)).ToArray()),
                geometry.GridWidth, geometry.GridHeight);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ShapesOutsideTheGridAreClipped()
    {
        var covered = Cells(FeatureTestCatalog.Box(-5, -5, 3, 2).Rings, 10, 10);
        Assert.Equal(6, covered.Count);
        Assert.All(covered, cell => Assert.True(cell.X >= 0 && cell.Y >= 0));
    }

    [Fact]
    public void IntegerSquareRootIsExact()
    {
        Assert.Equal(0, FixedGeometry.Sqrt(0));
        Assert.Equal(3, FixedGeometry.Sqrt(15));
        Assert.Equal(4, FixedGeometry.Sqrt(16));
        Assert.Equal(3_037_000_499, FixedGeometry.Sqrt(long.MaxValue));
    }

    [Fact]
    public void CurvesFlattenBetweenTheirEndpointsAndLinesStayExact()
    {
        var path = new CenterlinePath(FixedVector.FromPixels(0, 0),
        [
            new PathSegment(FixedVector.FromPixels(10, 0)),
            new PathSegment(FixedVector.FromPixels(40, 30), FixedVector.FromPixels(25, 0), FixedVector.FromPixels(40, 15)),
        ]);
        var points = FixedGeometry.Flatten(path);
        Assert.Equal(FixedVector.FromPixels(0, 0), points[0]);
        Assert.Equal(FixedVector.FromPixels(10, 0), points[1]);
        Assert.Equal(FixedVector.FromPixels(40, 30), points[^1]);
        Assert.True(points.Count > 5);
        Assert.Equal(points, FixedGeometry.Flatten(path));
    }

    [Fact]
    public void StrokesCoverTheirCenterlineAndNotBeyondTheirWidth()
    {
        var points = new[] { FixedVector.FromPixels(5, 20), FixedVector.FromPixels(35, 20) };
        var covered = new HashSet<(int, int)>();
        foreach (var piece in FixedGeometry.Stroke(points, 8 * 64, roundJoins: true))
        {
            ShapeRasterizer.Fill([piece], 50, 50, (x, y) => covered.Add((x, y)));
        }

        Assert.Contains((20, 19), covered);
        Assert.Contains((20, 16), covered);
        Assert.DoesNotContain((20, 25), covered);
        Assert.Contains((2, 20), covered);
        Assert.DoesNotContain((0, 20), covered);
    }

    [Fact]
    public void FeatureIdsAreUlidShaped()
    {
        var first = FeatureIds.NewUlid();
        var second = FeatureIds.NewUlid();
        Assert.Matches("^[0-9A-HJKMNP-TV-Z]{26}$", first);
        Assert.NotEqual(first, second);
        Assert.Equal(FeatureIds.Sequential(3, 7), FeatureIds.Sequential(3, 7));
        Assert.NotEqual(FeatureIds.Sequential(3, 7), FeatureIds.Sequential(4, 7));
        Assert.True(string.CompareOrdinal(FeatureIds.Sequential(3, 7), FeatureIds.Sequential(3, 8)) < 0);
    }

    private static List<(int X, int Y)> Cells(IEnumerable<IReadOnlyList<FixedVector>> rings, int width, int height)
    {
        var cells = new List<(int X, int Y)>();
        ShapeRasterizer.Fill(rings, width, height, (x, y) => cells.Add((x, y)));
        return cells;
    }
}
