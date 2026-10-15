using System.Globalization;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class CampaignMapBoundariesTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Standard(6, 6);

    [Theory]
    [InlineData("A", 10)]
    [InlineData("B", 11)]
    public void SharedEdgesAreDrawnOnceOnlyWhenParentsDiffer(string secondParent, int segments)
    {
        var ownership = new Dictionary<string, string> { ["B2"] = "A", ["C2"] = secondParent };
        var root = XElement.Parse(CampaignMapBoundaries.Draw(Geometry, ownership));
        var path = Assert.Single(root.Elements());
        Assert.Equal(segments, path.Attribute("d")!.Value.Count(character => character == 'M'));
        Assert.Equal("none", root.Attribute("pointer-events")!.Value);
        Assert.Equal("non-scaling-stroke", path.Attribute("vector-effect")!.Value);
        Assert.Equal("none", path.Attribute("fill")!.Value);
        var first = Geometry.IndexOf(HexName.Parse("B2"));
        Assert.Equal(Geometry.IndexOf(HexName.Parse("C2")), Geometry.Neighbor(first, HexsideDirection.NorthEast));
        var edge = Geometry.Vertices(first);
        var shared = FormattableString.Invariant($"M{edge[1].X},{edge[1].Y}L{edge[2].X},{edge[2].Y}");
        Assert.Equal(secondParent == "B", path.Attribute("d")!.Value.Contains(shared, StringComparison.Ordinal));
    }

    [Fact]
    public void StableAcrossOrderingAndCultureAndNoUnownedInteriorEdges()
    {
        var ownership = new Dictionary<string, string> { ["B2"] = "A", ["C2"] = "B" };
        var expected = CampaignMapBoundaries.Draw(Geometry, ownership);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(expected, CampaignMapBoundaries.Draw(Geometry, ownership.Reverse().ToDictionary()));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
        Assert.Equal(string.Empty, CampaignMapBoundaries.Draw(Geometry, new Dictionary<string, string>()));
        var single = XElement.Parse(CampaignMapBoundaries.Draw(Geometry, new Dictionary<string, string> { ["B2"] = "A" }));
        Assert.Equal(6, single.Element("path")!.Attribute("d")!.Value.Count(character => character == 'M'));
    }
}
