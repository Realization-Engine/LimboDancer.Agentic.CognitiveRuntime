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
        var whole = LayerOracle.Expanded(Build(sheet, fixture, shared: false));
        Assert.Equal(whole, LayerOracle.Expanded(Build(sheet, fixture, shared: true)));

        // With the fragment cache too, a hex taken from it brings its outlines.
        var cache = new UnitFragmentCache();
        Build(sheet, fixture, shared: true, cache);
        Assert.Equal(whole, LayerOracle.Expanded(Build(sheet, fixture, shared: true, cache)));
        Assert.True(cache.Hits > 0);
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void TheLayerCarriesNoOutlineTheSpriteFinds(string sheet, string fixture)
    {
        var overlay = Build(sheet, fixture, shared: true);
        var layer = XElement.Parse(overlay.Svg);
        Assert.DoesNotContain(layer.Descendants(), element => element.Name.LocalName == "defs");
        Assert.DoesNotContain(overlay.Fragments, fragment => fragment.Id == UnitOverlayBuilder.FiguresFragmentId);

        // Every reference finds an outline of the sprite.
        var sprite = XElement.Parse(overlay.Figures!);
        var ids = sprite.Descendants().Where(element => element.Name.LocalName == "path").Select(path => path.Attribute("id")!.Value).ToHashSet(StringComparer.Ordinal);
        var hrefs = layer.Descendants().Where(element => element.Name.LocalName == "use").Select(use => use.Attribute("href")!.Value[1..]).ToArray();
        Assert.NotEmpty(hrefs);
        Assert.All(hrefs, href => Assert.Contains(href, ids));
    }

    [Fact]
    public void TheSpriteHoldsEveryOutlineOnce()
    {
        var art = FigureArt.BuiltIn;
        var sprite = XElement.Parse(UnitFigureSprite.Markup(art));
        Assert.Equal(UnitFigureSprite.Id, sprite.Attribute("id")!.Value);
        var paths = sprite.Descendants().Where(element => element.Name.LocalName == "path").ToArray();
        Assert.Equal(art.Outlines.Count(), paths.Length);
        Assert.Equal(paths.Length, paths.Select(path => path.Attribute("id")!.Value).Distinct(StringComparer.Ordinal).Count());
        Assert.All(paths, path => Assert.Matches("^fig-[0-9a-f]{24}$", path.Attribute("id")!.Value));
        Assert.Equal(art.Outlines.Order(StringComparer.Ordinal), paths.Select(path => path.Attribute("d")!.Value).Order(StringComparer.Ordinal));

        // Made once for the art.
        Assert.Same(UnitFigureSprite.Markup(art), UnitFigureSprite.Markup(art));
    }

    [Fact]
    public void EachFigureArtHasItsOwnSprite()
    {
        // The sprite is the renderer's own figure art: with none, the counters draw the fallback figures and refer to nothing; with the stand-ins
        // (the Unit Lab's Art tab), the sprite holds them too. Either way the layer, expanded with its sprite, draws what the whole counters draw.
        var sheet = UnitStyles.Load(UnitStyles.Digital);
        foreach (var art in new[] { FigureArt.None, FigureArt.BuiltIn.WithStandIns() })
        {
            var renderer = new UnitRenderer(RenderingTestData.Vocabulary.Value, sheet, Palettes.PaletteSetReader.Embedded(sheet.Palette!)) { Figures = art };
            var overlay = UnitOverlayBuilder.Build(Target, "test-set", LayerOracleTests.Fixture("crowded"), renderer);
            Assert.Equal(LayerOracle.Expanded(UnitOverlayBuilder.Build(Target, "test-set", LayerOracleTests.Fixture("crowded"), renderer, null, shared: false)),
                LayerOracle.Expanded(overlay));
        }
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
