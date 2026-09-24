using System.Xml.Linq;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class DemoUnitOverlayTests
{
    private static readonly BoardRef Board = BoardRef.Parse("ab-synthetic");

    [Fact]
    public void StackRendersInStableOrderAndEscapesFixtureText()
    {
        var fixture = Fixture(
            new("second", "ab-synthetic:B1:1", "red", "B", "front", 2),
            new("first", "ab-synthetic:B1:0", "blue", "A<&", "front", 1),
            new("third", "ab-synthetic:C1:0", "blue", "C", "front", 0));

        var result = Build(fixture);
        var xml = XElement.Parse(result.Svg);
        Assert.Empty(result.Diagnostics);
        Assert.Collection(xml.Elements("g"),
            x => Assert.Equal("first", x.Attribute("data-placement-id")!.Value),
            x => Assert.Equal("second", x.Attribute("data-placement-id")!.Value),
            x => Assert.Equal("third", x.Attribute("data-placement-id")!.Value));
        Assert.Contains("A&lt;&amp;", result.Svg, StringComparison.Ordinal);
        Assert.Equal("ab-synthetic:B1:1", result.Units[1].Location.ToString());
        Assert.Equal(result.Svg, Build(fixture).Svg);
    }

    [Fact]
    public void WrongBoardOrVersionDoesNotRenderAnyUnit()
    {
        var placement = new DemoUnitPlacement("a", "ab-synthetic:B1:0", "blue", "A", "front", 0);
        var wrongBoardFixture = Fixture(placement) with
        {
            BoardRef = "bd01"
        };
        var wrongVersionFixture = Fixture(placement) with
        {
            ExpectedBoardVersion = "different"
        };
        var wrongBoard = Build(wrongBoardFixture);
        var wrongVersion = Build(wrongVersionFixture);
        Assert.Empty(wrongBoard.Units);
        Assert.Empty(wrongVersion.Units);
        Assert.NotEmpty(wrongBoard.Diagnostics);
        Assert.NotEmpty(wrongVersion.Diagnostics);
    }

    [Fact]
    public void BadPlacementsAreDiagnosedWithoutRelocation()
    {
        var fixture = Fixture(
            new("valid", "ab-synthetic:B1:0", "blue", "A", "front", 0),
            new("valid", "ab-synthetic:C1:0", "red", "B", "front", 0),
            new("offboard", "ab-synthetic:Z9:0", "blue", "C", "front", 0),
            new("wrong-board", "bd01:B1:0", "blue", "D", "front", 0),
            new("bad-face", "ab-synthetic:B1:0", "blue", "E", "broken", 0));

        var result = Build(fixture);
        Assert.Single(result.Units);
        Assert.Equal("valid", result.Units[0].Placement.PlacementId);
        Assert.Equal(4, result.Diagnostics.Count);
    }

    [Fact]
    public void ReplacementSnapshotChangesOnlyOverlayContent()
    {
        var first = Build(Fixture(new DemoUnitPlacement("a", "ab-synthetic:B1:0", "blue", "A", "front", 0)));
        var second = Build(Fixture(new DemoUnitPlacement("a", "ab-synthetic:C1:0", "blue", "A", "front", 0)));
        Assert.NotEqual(first.Svg, second.Svg);
        Assert.Equal(first.BoardVersion, second.BoardVersion);
        Assert.Single(second.Units);
        Assert.Equal("C1", second.Units[0].Location.Hex.ToString());
    }

    private static DemoUnitFixture Fixture(params DemoUnitPlacement[] placements) =>
        new(1, Board.Value, "test-demo", placements);

    private static DemoUnitOverlay Build(DemoUnitFixture fixture) =>
        DemoUnitOverlayBuilder.Build(Board, "version-1", SyntheticBoard.Geometry, fixture, "fixture-hash");
}
