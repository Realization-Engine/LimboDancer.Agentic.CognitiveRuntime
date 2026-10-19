using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>
/// The counter layer's shared definitions (the Counter Layer Design, sections 4 and 11): the layer drawn with them is, once its references are
/// expanded, the layer drawn with every counter whole; each figure outline is written once; the layer goldens (ruling U6) hold the layer as sent.
/// </summary>
public sealed class LayerSharingTests
{
    private const string UpdateGoldenVariable = "ASL_UNITS_UPDATE_GOLDEN";
    private static readonly UnitMapTarget Target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);

    public static TheoryData<string, string> Layers => LayerOracleTests.Layers;

    private static UnitOverlay Build(string sheet, string fixture, bool shared, UnitFragmentCache? cache = null) =>
        UnitOverlayBuilder.Build(Target, "test-set", LayerOracleTests.Fixture(fixture), RenderingTestData.Renderer(sheet), cache, shared);

    [Theory]
    [MemberData(nameof(Layers))]
    public void TheSharedLayerDrawsWhatTheWholeCountersDraw(string sheet, string fixture)
    {
        var whole = LayerOracle.Expanded(Build(sheet, fixture, shared: false).Svg);
        Assert.Equal(whole, LayerOracle.Expanded(Build(sheet, fixture, shared: true).Svg));

        // With the fragment cache too, a hex taken from it brings its outlines.
        var cache = new UnitFragmentCache();
        Build(sheet, fixture, shared: true, cache);
        Assert.Equal(whole, LayerOracle.Expanded(Build(sheet, fixture, shared: true, cache).Svg));
        Assert.True(cache.Hits > 0);
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void EachOutlineIsWrittenOnceAtTheLayersEnd(string sheet, string fixture)
    {
        var layer = XElement.Parse(Build(sheet, fixture, shared: true).Svg);
        var definitions = Assert.Single(layer.Descendants(), element => element.Name.LocalName == "defs");
        Assert.Same(layer.Elements().Last(), definitions);
        Assert.Equal("true", definitions.Attribute("data-figures")?.Value);
        var ids = definitions.Elements().Select(path => path.Attribute("id")!.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.Matches("^fig-[0-9a-f]{24}$", id));
        Assert.Equal(ids.Length, definitions.Elements().Select(path => path.Attribute("d")!.Value).Distinct(StringComparer.Ordinal).Count());

        // Every reference finds one of them, and every one of them is referred to.
        var hrefs = layer.Descendants().Where(element => element.Name.LocalName == "use").Select(use => use.Attribute("href")!.Value[1..]).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(ids.Order(StringComparer.Ordinal), hrefs.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void TheLayerMatchesItsGolden(string sheet, string fixture)
    {
        var actual = Build(sheet, fixture, shared: true).Svg;
        var directory = Path.Combine(RenderingTestData.GoldenDirectory(), "layers", sheet);
        var path = Path.Combine(directory, fixture + ".svg");
        if (Environment.GetEnvironmentVariable(UpdateGoldenVariable) == "1")
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, actual + "\n");
        }

        Assert.True(File.Exists(path), $"Missing {path}; set {UpdateGoldenVariable}=1 to create it.");
        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n").TrimEnd('\n'), actual);
    }
}
