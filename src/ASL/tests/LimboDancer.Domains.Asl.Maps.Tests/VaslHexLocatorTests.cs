using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class VaslHexLocatorTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;
    private static readonly VaslHexLocator Locator = new(Geometry);

    [Fact]
    public void HexCentersBelongToTheirHex()
    {
        foreach (var hex in Geometry.Hexes())
        {
            var center = Geometry.CenterPoint(hex);
            if (Geometry.ContainsCell(center.X, center.Y))
            {
                Assert.Equal(hex, Locator.GridToHex(center.X, center.Y));
            }
        }
    }

    [Fact]
    public void PixelsOffTheGridHaveNoHex()
    {
        Assert.Null(Locator.GridToHex(-1, 10));
        Assert.Null(Locator.GridToHex(10, Geometry.GridHeight));
    }

    [Fact]
    public void TheEditorLookupAgreesWithTheNearestCenterAlmostEverywhere()
    {
        // gridToHex decides by column bands and polygon tests, not distance, so a thin seam near hex borders differs.
        var compared = 0;
        var agreed = 0;
        for (var x = 0; x < Geometry.GridWidth; x += 3)
        {
            for (var y = 0; y < Geometry.GridHeight; y += 3)
            {
                if (Locator.GridToHex(x, y) is { } hex)
                {
                    compared++;
                    agreed += Geometry.HexAt(x + 0.5, y + 0.5) == hex ? 1 : 0;
                }
            }
        }

        Assert.True(compared > 120_000);
        Assert.True(agreed > compared * 0.97, $"{agreed} of {compared} agree.");
    }
}
