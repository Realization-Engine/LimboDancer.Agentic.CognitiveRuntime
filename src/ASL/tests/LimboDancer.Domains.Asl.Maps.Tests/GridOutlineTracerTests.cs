using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Outlines;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class GridOutlineTracerTests
{
    private static readonly BoardGeometry Small = BoardGeometry.Standard(3, 2);

    [Fact]
    public void UniformGridIsOneRectangle()
    {
        var outlines = GridOutlineTracer.Trace(Build(Small, (_, _) => 0));
        var region = Assert.Single(outlines.Regions);
        var ring = Assert.Single(region.Rings);
        Assert.False(ring.IsHole);
        Assert.Equal(Small.GridWidth * Small.GridHeight, region.CellCount);
        Assert.Equal(
            new[] { new GridPoint(0, 0), new GridPoint(Small.GridWidth, 0), new GridPoint(Small.GridWidth, Small.GridHeight), new GridPoint(0, Small.GridHeight) }.ToHashSet(),
            ring.Vertices.ToHashSet());
        AssertLossless(Build(Small, (_, _) => 0), outlines);
    }

    [Fact]
    public void EnclosedBlockMakesAHole()
    {
        var grid = Build(Small, (x, y) => x is >= 10 and < 20 && y is >= 5 and < 8 ? (byte)3 : (byte)0);
        var outlines = GridOutlineTracer.Trace(grid);
        Assert.Equal(2, outlines.Regions.Count);
        var outer = outlines.Regions.Single(region => region.Code == 0);
        var block = outlines.Regions.Single(region => region.Code == 3);
        Assert.Equal(2, outer.Rings.Count);
        Assert.Single(outer.Rings, ring => ring.IsHole);
        Assert.Equal(30, block.CellCount);
        Assert.Equal(new GridPoint(10, 5), block.AnchorCell);
        Assert.Equal("r3e0x10y5", block.Id);
        Assert.Equal(4, Assert.Single(block.Rings).Vertices.Count);
        AssertLossless(grid, outlines);
    }

    [Fact]
    public void DiagonalCellsAreSeparateRegionsAndPinchesResolve()
    {
        // Two cells touching only at a corner: 4-connectivity keeps them apart, and the background ring pinches there.
        var grid = Build(Small, (x, y) => (x, y) is (5, 5) or (6, 6) ? (byte)2 : (byte)0);
        var outlines = GridOutlineTracer.Trace(grid);
        Assert.Equal(2, outlines.Regions.Count(region => region.Code == 2));
        Assert.All(outlines.Regions.Where(region => region.Code == 2), region => Assert.Equal(1, region.CellCount));
        AssertLossless(grid, outlines);
    }

    [Fact]
    public void ElevationSplitsRegionsOfOneCode()
    {
        var grid = Build(Small, (_, _) => 0, (x, _) => x < 50 ? (sbyte)0 : (sbyte)1);
        var outlines = GridOutlineTracer.Trace(grid);
        Assert.Equal(new[] { (sbyte)0, (sbyte)1 }, outlines.Regions.Select(region => region.Elevation).Order());
        AssertLossless(grid, outlines);
    }

    [Fact]
    public void RandomGridsAreTracedLosslessly()
    {
        var random = new Random(4);
        for (var trial = 0; trial < 5; trial++)
        {
            // Coarse blobs plus single-cell noise exercise holes, pinches, and nested regions.
            var grid = Build(Small, (x, y) => random.Next(10) == 0 ? (byte)random.Next(4) : (byte)((x / 7) + (y / 5) & 3),
                (x, y) => (sbyte)((x / 13) % 2));
            AssertLossless(grid, GridOutlineTracer.Trace(grid));
        }
    }

    [Fact]
    public void OuterRingsAreClockwiseOnScreenAndHolesCounterclockwise()
    {
        var grid = Build(Small, (x, y) => x is >= 10 and < 20 && y is >= 5 and < 8 ? (byte)3 : (byte)0);
        foreach (var region in GridOutlineTracer.Trace(grid).Regions)
        {
            foreach (var ring in region.Rings)
            {
                Assert.Equal(ring.IsHole, SignedArea(ring.Vertices) < 0);
            }

            Assert.Equal(region.CellCount, region.Rings.Sum(ring => SignedArea(ring.Vertices)));
        }
    }

    [Fact]
    public void VerifierAcceptsTracedOutlinesAndLocatesDamage()
    {
        var grid = Build(Small, (x, y) => x is >= 10 and < 20 && y is >= 5 and < 8 ? (byte)3 : (byte)0, (x, _) => x < 50 ? (sbyte)0 : (sbyte)1);
        var outlines = GridOutlineTracer.Trace(grid);
        Assert.True(GridOutlineVerifier.Verify(grid, outlines).Lossless);

        // Dropping the building leaves its 30 cells uncovered, because the open-ground ring keeps its hole.
        var withoutBlock = new GridOutlines(Small, outlines.Regions.Where(region => region.Code != 3).ToArray());
        var damaged = GridOutlineVerifier.Verify(grid, withoutBlock);
        Assert.Equal(30, damaged.MismatchedCells);
        Assert.Equal(new GridPoint(10, 5), damaged.FirstMismatch);

        // Relabeling a region paints its cells with the wrong key.
        var relabeled = new GridOutlines(Small, outlines.Regions.Select(region => region.Code == 3 ? region with { Code = 4 } : region).ToArray());
        Assert.Equal(30, GridOutlineVerifier.Verify(grid, relabeled).MismatchedCells);
    }

    [Fact]
    public void CoverageFillsEvenOdd()
    {
        GridPoint[] outer = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        GridPoint[] hole = [new(1, 1), new(1, 3), new(3, 3), new(3, 1)];
        var cells = CellCoverage.Fill([outer, hole], 5, 5);
        Assert.Equal(12, cells.Count(covered => covered));
        Assert.True(cells[0]);
        Assert.False(cells[(1 * 5) + 1]);
        Assert.False(cells[(4 * 5) + 0]);
    }

    internal static TerrainGrid Build(BoardGeometry geometry, Func<int, int, byte> codeAt, Func<int, int, sbyte>? elevationAt = null)
    {
        var codes = new byte[geometry.GridWidth * geometry.GridHeight];
        var elevations = new sbyte[codes.Length];
        for (var x = 0; x < geometry.GridWidth; x++)
        {
            for (var y = 0; y < geometry.GridHeight; y++)
            {
                codes[(x * geometry.GridHeight) + y] = codeAt(x, y);
                elevations[(x * geometry.GridHeight) + y] = elevationAt?.Invoke(x, y) ?? 0;
            }
        }

        return new TerrainGrid(geometry, codes, elevations, new bool[geometry.HexCount]);
    }

    private static void AssertLossless(TerrainGrid grid, GridOutlines outlines)
    {
        var owner = new int[grid.CellCount];
        for (var index = 0; index < outlines.Regions.Count; index++)
        {
            var region = outlines.Regions[index];
            var coverage = CellCoverage.Fill(region.Rings.Select(ring => ring.Vertices), grid.Geometry.GridWidth, grid.Geometry.GridHeight);
            Assert.Equal(region.CellCount, coverage.Count(covered => covered));
            for (var cell = 0; cell < coverage.Length; cell++)
            {
                if (coverage[cell])
                {
                    Assert.Equal(0, owner[cell]);
                    owner[cell] = index + 1;
                    Assert.Equal(region.Code, grid.Codes[cell]);
                    Assert.Equal(region.Elevation, grid.Elevations[cell]);
                }
            }
        }

        Assert.DoesNotContain(0, owner);
    }

    private static int SignedArea(IReadOnlyList<GridPoint> ring)
    {
        long twice = 0;
        for (var index = 0; index < ring.Count; index++)
        {
            var current = ring[index];
            var next = ring[(index + 1) % ring.Count];
            twice += ((long)current.X * next.Y) - ((long)next.X * current.Y);
        }

        return (int)(twice / 2);
    }
}
