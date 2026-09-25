using System.Globalization;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>Overlay placement on a standard board, a b board, and a composed map (ASL-UNIT-071; section 12).</summary>
public sealed class OverlayTests
{
    private static readonly BoardGeometry Small = BoardGeometry.Standard(5, 2);

    [Fact]
    public void UnitsAnchorOnTheCenterDotOfAStandardBoard()
    {
        var board = BoardRef.Parse("bd01");
        var geometry = BoardGeometry.StandardGeomorphic;
        var overlay = Build(UnitMapTarget.ForBoard(board, geometry), Unit("a", "bd01:E4:0"));
        Assert.Empty(overlay.Diagnostics);
        var placed = Assert.Single(overlay.Units);
        Assert.Equal(geometry.IndexOf(HexName.Parse("E4")), placed.Hex);
        var center = geometry.CenterDot(placed.Hex);
        Near(center, FaceCenter(overlay.Svg, "a"));
        Assert.Equal(0.55 * geometry.HexHeight, overlay.FaceSize, 1);
        var layer = XElement.Parse(overlay.Svg);
        Assert.Equal("layer-units", layer.Attribute("id")!.Value);
        Assert.Equal("test-set", layer.Attribute("data-unit-set")!.Value);
    }

    [Fact]
    public void UnitsUseTheBBoardsOwnHexNames()
    {
        var board = BoardRef.Parse("bd1b");
        var geometry = BoardGeometry.Vasl(17, 10, 56.3125, 64.5, 901, 645, columnLetterOffset: 16);
        var overlay = Build(UnitMapTarget.ForBoard(board, geometry), Unit("q1", "bd1b:Q1:0"), Unit("a1", "bd1b:A1:0"));
        var placed = Assert.Single(overlay.Units);
        Assert.Equal(new HexIndex(0, 0), placed.Hex);
        Near(geometry.CenterDot(new HexIndex(0, 0)), FaceCenter(overlay.Svg, "q1"));
        Assert.Contains(overlay.Diagnostics, diagnostic => diagnostic.Contains("a1", StringComparison.Ordinal));
    }

    [Fact]
    public void AComposedMapPlacesEachBoardsLocationsThroughTheMap()
    {
        var first = BoardRef.Parse("bd01");
        var second = BoardRef.Parse("bd02");
        var map = VaslMapBuilder.Build([Place(first, 0), Place(second, 1)], Catalog(), new LosSsRuleSet([])).Map!;
        var target = UnitMapTarget.ForMap(BoardRef.Parse("map-test"), map);
        var overlay = Build(target, Unit("left", "bd01:B1:0"), Unit("right", "bd02:B1:0"), Unit("elsewhere", "bd03:B1:0"));
        Assert.Equal(2, overlay.Units.Count);
        var right = overlay.Units.Single(unit => unit.Document.Id == "right");
        Assert.Equal(map.Locate(second, HexName.Parse("B1")), right.Hex);
        Near(map.Geometry.CenterDot(right.Hex), FaceCenter(overlay.Svg, "right"));
        Assert.True(right.Hex.Column >= 4);
        Assert.Contains(overlay.Diagnostics, diagnostic => diagnostic.Contains("elsewhere", StringComparison.Ordinal));
        Assert.Contains(first, target.Boards);
        Assert.Contains(second, target.Boards);
    }

    [Fact]
    public void AStackOrdersByStackOrderThenIdAndCountsBeyondSix()
    {
        var target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);
        var units = Enumerable.Range(0, 8).Select(index => Unit("u" + index.ToString(CultureInfo.InvariantCulture), "bd01:E4:0", stackOrder: 8 - index)).ToArray();
        var overlay = Build(target, units);
        var ids = XElement.Parse(overlay.Svg).Descendants().Where(element => element.Attribute("role")?.Value == "button")
            .Select(element => element.Attribute("data-unit-id")!.Value).ToArray();
        Assert.Equal(["u7", "u6", "u5", "u4", "u3", "u2"], ids);
        Assert.Equal(8, overlay.Units.Count);
        Assert.Equal(2, overlay.Units.Count(unit => !unit.Shown));
        Assert.Contains("data-stack-count=\"+2\"", overlay.Svg, StringComparison.Ordinal);
        Assert.Equal(overlay.Svg, Build(target, [.. units.Reverse()]).Svg);
    }

    [Fact]
    public void ALevelAboveGroundGetsALevelTabAndItsOwnStack()
    {
        var target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);
        var overlay = Build(target, Unit("ground", "bd01:E4:0"), Unit("upstairs", "bd01:E4:1"), Unit("cellar", "bd01:E4:-1"));
        var layer = XElement.Parse(overlay.Svg);
        Assert.Equal(["-1", "1"], layer.Descendants().Select(element => element.Attribute("data-level")?.Value).OfType<string>());
        Assert.Equal(3, layer.Elements("g").Count(element => element.Attribute("data-stack") is not null));
        Assert.EndsWith(", level 1", overlay.Units.Single(unit => unit.Document.Id == "upstairs").Name, StringComparison.Ordinal);
        Assert.Contains("level 1", layer.Descendants().Single(element => element.Attribute("data-unit-id")?.Value == "upstairs").Attribute("aria-label")!.Value,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyOrForeignSetDrawsAnEmptyLayer()
    {
        var target = UnitMapTarget.ForBoard(BoardRef.Parse("ab-synthetic"), Small);
        var overlay = Build(target, Unit("x", "bd01:E4:0"));
        Assert.Empty(overlay.Units);
        Assert.Empty(XElement.Parse(overlay.Svg).Elements());
        Assert.Single(overlay.Diagnostics);
    }

    [Fact]
    public void TheDemoSetDrawsOnBoard01()
    {
        var vocabulary = RenderingTestData.Vocabulary.Value;
        var set = UnitPlacementSetReader.Read(File.ReadAllBytes(Path.Combine(RenderingTestData.UnitsDirectory(), "examples", "bd01-demo.units.json")), vocabulary).Set!;
        var overlay = UnitOverlayBuilder.Build(UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic), set.SetId, set.Units,
            RenderingTestData.Renderer(UnitStyles.Digital));
        Assert.Empty(overlay.Diagnostics);
        Assert.Equal(["demo-a", "demo-b", "demo-c", "demo-d", "demo-e"], overlay.Units.Select(unit => unit.Document.Id).Order(StringComparer.Ordinal));
        Assert.Equal("German 1st Line squad A, 4-6-7, pinned, with light MG 3-6", overlay.Units.Single(unit => unit.Document.Id == "demo-a").Name);
    }

    private static UnitOverlay Build(UnitMapTarget target, params UnitDocument[] units) =>
        UnitOverlayBuilder.Build(target, "test-set", units, RenderingTestData.Renderer(UnitStyles.Digital));

    private static UnitDocument Unit(string id, string location, int stackOrder = 0) =>
        RenderingTestData.Document($$"""
            { "id": "{{id}}", "kind": "asl:squad", "side": "german", "location": "{{location}}", "stackOrder": {{stackOrder}},
              "faces": { "front": { "firepower": 4, "range": 6, "morale": 7 } } }
            """);

    /// <summary>Positions are written to the nearest 1/64 pixel.</summary>
    private static void Near(PixelPoint expected, PixelPoint actual)
    {
        Assert.Equal(expected.X, actual.X, 0.02);
        Assert.Equal(expected.Y, actual.Y, 0.02);
    }

    /// <summary>The center of a unit's near-tier face, read back from the SVG.</summary>
    private static PixelPoint FaceCenter(string svg, string id)
    {
        var unit = XElement.Parse(svg).Descendants().Single(element => element.Attribute("data-unit-id")?.Value == id);
        var face = unit.Elements("g").Single(tier => tier.Attribute("data-tier")!.Value == "near").Elements()
            .First(element => element.Attribute("data-face") is not null);
        double Read(string name) => double.Parse(face.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        return new PixelPoint(Read("x") + (Read("width") / 2), Read("y") + (Read("height") / 2));
    }

    private static PlacedBoard Place(BoardRef board, int column)
    {
        var codes = new byte[Small.GridWidth * Small.GridHeight];
        var grid = new TerrainGrid(Small, codes, new sbyte[codes.Length], new bool[Small.HexCount]);
        return new PlacedBoard(new BoardPlacement(board, column, 0), grid, HexsideAnnotations.None);
    }

    private static TerrainCatalog Catalog() => new(
    [
        new TerrainType { Code = 0, Name = "Open Ground", Category = LosCategory.Open },
        new TerrainType { Code = 2, Name = "Rooftop", Category = LosCategory.Open },
        new TerrainType { Code = 174, Name = "Cellar", Category = LosCategory.Building, Height = 1 },
    ]);
}
