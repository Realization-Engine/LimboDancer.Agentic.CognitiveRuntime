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
        Assert.Equal(new[] { "first", "second", "third" },
            xml.Elements("g").Select(x => x.Attribute("data-placement-id")!.Value).ToArray());
        Assert.Contains("A&lt;&amp;", result.Svg, StringComparison.Ordinal);
        Assert.Equal("ab-synthetic:B1:1", result.Units[1].Location.ToString());
        Assert.Equal(result.Svg, Build(fixture).Svg);
    }

    [Fact]
    public void WrongBoardOrVersionDoesNotRenderAnyUnit()
    {
        var wrongBoard = Build(Fixture(new("a", "ab-synthetic:B1:0", "blue", "A", "front", 0)) with { BoardRef = "bd01" });
        var wrongVersion = Build(Fixture(new("a", "ab-synthetic:B1:0", "blue", "A", "front", 0)) with { ExpectedBoardVersion = "different" });
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
        var first = Build(Fixture(new("a", "ab-synthetic:B1:0", "blue", "A", "front", 0)));
        var second = Build(Fixture(new("a", "ab-synthetic:C1:0", "blue", "A", "front", 0)));
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
