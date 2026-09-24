using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class HexHitTestTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void CenterDotsHitTheirOwnHex()
    {
        // Half hexes along the bottom edge have their center dot on the grid boundary, outside the hit-test range.
        var tested = 0;
        foreach (var hex in Geometry.Hexes())
        {
            var center = Geometry.CenterDot(hex);
            if (center.Y < Geometry.GridHeight)
            {
                Assert.Equal(hex, Geometry.HexAt(center.X, center.Y));
                tested++;
            }
        }

        Assert.Equal(Geometry.HexCount - (Geometry.WidthInHexes / 2), tested);
    }

    [Fact]
    public void PointsOffTheGridHitNothing()
    {
        Assert.Null(Geometry.HexAt(-0.5, 10));
        Assert.Null(Geometry.HexAt(10, -0.5));
        Assert.Null(Geometry.HexAt(Geometry.GridWidth, 10));
        Assert.Null(Geometry.HexAt(10, Geometry.GridHeight));
    }

    [Fact]
    public void HitTestMatchesTheNearestCenterOverTheWholeBoard()
    {
        var centers = Geometry.Hexes().Select(hex => (Hex: hex, Center: Geometry.CenterDot(hex))).ToArray();
        for (var x = 0.5; x < Geometry.GridWidth; x += 7)
        {
            for (var y = 0.5; y < Geometry.GridHeight; y += 7)
            {
                var nearest = centers.MinBy(entry => Square(entry.Center.X - x) + Square(entry.Center.Y - y));
                var hit = Geometry.HexAt(x, y);
                Assert.NotNull(hit);
                var hitCenter = Geometry.CenterDot(hit.Value);
                Assert.Equal(
                    Square(nearest.Center.X - x) + Square(nearest.Center.Y - y),
                    Square(hitCenter.X - x) + Square(hitCenter.Y - y),
                    9);
            }
        }
    }

    private static double Square(double value) => value * value;
}
