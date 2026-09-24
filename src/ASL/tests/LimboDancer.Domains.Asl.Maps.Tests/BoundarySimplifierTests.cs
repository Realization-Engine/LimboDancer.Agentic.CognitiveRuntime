using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class BoundarySimplifierTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Standard(6, 4);

    [Fact]
    public void ToleranceZeroReproducesEveryRegionExactly()
    {
        var labels = Blobs();
        var simplifier = new BoundarySimplifier(Geometry, labels);
        var owner = new int[labels.Length];
        foreach (var region in simplifier.Regions)
        {
            var shape = Assert.IsType<FeatureShape>(simplifier.Simplify(region, _ => 0));
            ShapeRasterizer.Fill(shape.Rings, Geometry.GridWidth, Geometry.GridHeight, (x, y) =>
            {
                var cell = (x * Geometry.GridHeight) + y;
                Assert.Equal(labels[cell], region.Label);
                owner[cell]++;
            });
        }

        Assert.All(owner, count => Assert.Equal(1, count));
    }

    [Fact]
    public void SharedBoundariesStayConsistentWhenSimplified()
    {
        // Neighbors use the same simplified chains, so the regions still tile the grid: no gaps and no overlaps.
        var labels = Blobs();
        var simplifier = new BoundarySimplifier(Geometry, labels);
        var owner = new int[labels.Length];
        var mismatched = 0;
        var vertices = 0;
        foreach (var region in simplifier.Regions)
        {
            if (simplifier.Simplify(region, _ => 1.5) is not { } shape)
            {
                continue;
            }

            vertices += shape.VertexCount;
            ShapeRasterizer.Fill(shape.Rings, Geometry.GridWidth, Geometry.GridHeight, (x, y) =>
            {
                var cell = (x * Geometry.GridHeight) + y;
                owner[cell]++;
                mismatched += labels[cell] == region.Label ? 0 : 1;
            });
        }

        Assert.All(owner, count => Assert.Equal(1, count));
        Assert.True(mismatched < labels.Length / 50, $"{mismatched} cells changed label.");
        Assert.True(vertices < simplifier.Regions.Sum(region => 1) * 200);
    }

    [Fact]
    public void TinyRegionsCollapseWithTheirHoleInTheNeighbor()
    {
        var labels = new int[Geometry.GridWidth * Geometry.GridHeight];
        labels[(50 * Geometry.GridHeight) + 50] = 1;
        var simplifier = new BoundarySimplifier(Geometry, labels);
        var speck = simplifier.Regions.Single(region => region.Label == 1);
        var background = simplifier.Regions.Single(region => region.Label == 0);
        Assert.Null(simplifier.Simplify(speck, _ => 1.5));
        Assert.Single(simplifier.Simplify(background, _ => 1.5)!.Rings);
        Assert.Equal(2, simplifier.Simplify(background, _ => 0)!.Rings.Count);
    }

    // Three labels in overlapping discs over a background, so boundaries are curved and meet at junctions.
    private static int[] Blobs()
    {
        var labels = new int[Geometry.GridWidth * Geometry.GridHeight];
        (int X, int Y, int R, int Label)[] discs = [(80, 90, 60, 1), (150, 120, 55, 2), (200, 60, 40, 3), (110, 200, 30, 1)];
        for (var x = 0; x < Geometry.GridWidth; x++)
        {
            for (var y = 0; y < Geometry.GridHeight; y++)
            {
                foreach (var (cx, cy, r, label) in discs)
                {
                    if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) < r * r)
                    {
                        labels[(x * Geometry.GridHeight) + y] = label;
                    }
                }
            }
        }

        return labels;
    }
}
