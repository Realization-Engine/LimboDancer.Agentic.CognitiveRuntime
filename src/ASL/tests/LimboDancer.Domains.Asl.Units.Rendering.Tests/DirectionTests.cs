using System.Globalization;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>Facing and Covered Arc drawn as a direction (Unit Display Design, phase 2; C3.2, p. 169).</summary>
public sealed class DirectionTests
{
    [Theory]
    [InlineData("east", 1, 0)]
    [InlineData("north-east", 1, -1)]
    [InlineData("north-west", -1, -1)]
    [InlineData("west", -1, 0)]
    [InlineData("south-west", -1, 1)]
    [InlineData("south-east", 1, 1)]
    public void TheArrowPointsAtTheFacedHexspine(string facing, int signX, int signY)
    {
        var svg = Render(UnitStyles.Digital, facing, DetailTier.Near);
        var arrow = svg.Descendants().Single(element => element.Attribute("data-facing") is not null);
        Assert.Equal(facing, arrow.Attribute("data-facing")!.Value);
        var tip = FirstPoint(arrow.Attribute("d")!.Value);
        Assert.Equal(signX, Math.Sign(Math.Round(tip.X - 50, 3)));
        Assert.Equal(signY, Math.Sign(Math.Round(tip.Y - 50, 3)));
    }

    [Fact]
    public void TheCoveredArcIsASixtyDegreeWedgeAtTheNearTierOnly()
    {
        var near = Render(UnitStyles.Digital, "east", DetailTier.Near);
        var wedge = near.Descendants().Single(element => element.Attribute("data-covered-arc") is not null);
        var points = Points(wedge.Attribute("d")!.Value);
        Assert.Equal(3, points.Length);
        var angles = points.Skip(1).Select(point => Math.Atan2(point.Y - 50, point.X - 50) * 180 / Math.PI).Order().ToArray();
        Assert.Equal(-30, angles[0], 1);
        Assert.Equal(30, angles[1], 1);
        Assert.DoesNotContain(Render(UnitStyles.Digital, "east", DetailTier.Mid).Descendants(), element => element.Attribute("data-covered-arc") is not null);
        Assert.Contains(Render(UnitStyles.Digital, "east", DetailTier.Far).Descendants(), element => element.Attribute("data-facing") is not null);
    }

    [Theory]
    [InlineData("east", null)]
    [InlineData("north-east", "rotate(-60 ")]
    [InlineData("west", "rotate(180 ")]
    public void TheClassicSheetTurnsTheSilhouetteToItsFacing(string facing, string? transform)
    {
        var svg = Render(UnitStyles.Classic, facing, DetailTier.Near);
        var glyph = svg.Descendants().Single(element => element.Attribute("data-slot")?.Value == "glyph");
        var rotation = glyph.Elements().FirstOrDefault(element => element.Attribute("transform") is not null)?.Attribute("transform")!.Value;
        if (transform is null)
        {
            Assert.Null(rotation);
        }
        else
        {
            Assert.StartsWith(transform, rotation, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(svg.Descendants(), element => element.Attribute("data-facing") is not null);
    }

    [Fact]
    public void AMalfunctionedGunIsCrossedUnderTheClassicSheet()
    {
        var gun = RenderingTestData.Catalog.Value["example-at-gun"] with
        {
            States = ["asl:malfunctioned"]
        };
        var svg = RenderingTestData.Render(RenderingTestData.Renderer(UnitStyles.Classic), gun);
        Assert.Contains(svg.Descendants(), element => element.Attribute("data-pattern")?.Value == "cross");
        var slots = CascadeTests.Slots(svg);
        Assert.Equal("R1", slots["repair"]);
        Assert.Equal("X6", slots["removal"]);
        Assert.Equal("AT", slots["type"]);
    }

    [Fact]
    public void AnOptionalAttributeLeavesTheRestOfTheContent()
    {
        var gun = RenderingTestData.Catalog.Value["example-mortar-gun"];
        var slots = CascadeTests.Slots(RenderingTestData.Render(RenderingTestData.Renderer(UnitStyles.Digital), gun));
        Assert.Equal("82", slots["cal"]);
        Assert.Equal("[3-60]", slots["range"]);
        Assert.Equal("S8 WP7", slots["ammo"]);
    }

    [Fact]
    public void TheDemoSetShowsAGunWithItsDirection()
    {
        var set = UnitExamples.PlacementSets(RenderingTestData.Vocabulary.Value).Single().Set!;
        var overlay = UnitOverlayBuilder.Build(UnitMapTarget.ForBoard(Maps.Coordinates.BoardRef.Parse("bd01"), Maps.Geometry.BoardGeometry.StandardGeomorphic),
            set.SetId, set.Units, RenderingTestData.Renderer(UnitStyles.Digital));
        var gun = overlay.Units.Single(unit => unit.Document.Id == "demo-e");
        Assert.EndsWith("facing south-east", gun.Name, StringComparison.Ordinal);
        Assert.Contains("data-covered-arc=\"south-east\"", overlay.Svg, StringComparison.Ordinal);
    }

    private static XElement Render(string sheet, string facing, DetailTier tier)
    {
        var gun = RenderingTestData.Document($$"""
            { "id": "g", "kind": "asl:gun", "side": "german", "facing": "{{facing}}",
              "faces": { "front": { "gun-type": "at", "caliber": 75, "rate-of-fire": 2, "manhandling": 8 } } }
            """);
        return RenderingTestData.Render(RenderingTestData.Renderer(sheet), gun, tier);
    }

    private static (double X, double Y) FirstPoint(string path) => Points(path)[0];

    private static (double X, double Y)[] Points(string path) =>
        [.. path.Split(['M', 'L', 'Z'], StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(' '))
            .Select(pair => (double.Parse(pair[0], CultureInfo.InvariantCulture), double.Parse(pair[1], CultureInfo.InvariantCulture)))];
}
