using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;

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

    private static UnitOverlay Build(string sheet, string fixture, bool shared, UnitFragmentCache? cache = null, UnitFaceMode faces = UnitFaceMode.Shared) =>
        UnitOverlayBuilder.Build(Target, "test-set", LayerOracleTests.Fixture(fixture), RenderingTestData.Renderer(sheet), cache, shared, null, faces);

    [Theory]
    [MemberData(nameof(Layers))]
    public void TheSharedLayerDrawsWhatTheWholeCountersDraw(string sheet, string fixture)
    {
        // The faces drawn at the origin and moved into place draw what the whole counters draw, to 1/32 pixel (section 8, "Numbers").
        var whole = LayerOracle.Expand(Build(sheet, fixture, shared: false).Svg);
        var origin = Build(sheet, fixture, shared: true, faces: UnitFaceMode.AtOrigin);
        Assert.Null(LayerOracle.Differs(whole, LayerOracle.Expand(origin.Svg, [XElement.Parse(origin.Figures!)])));

        // And the layer of shared definitions (Q), its references expanded, is exactly that, with the fragment cache too.
        var moved = LayerOracle.Expanded(origin);
        Assert.Equal(moved, LayerOracle.Expanded(Build(sheet, fixture, shared: true)));
        var cache = new UnitFragmentCache();
        Build(sheet, fixture, shared: true, cache);
        Assert.Equal(moved, LayerOracle.Expanded(Build(sheet, fixture, shared: true, cache)));
        Assert.True(cache.Hits > 0);
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void EachCounterRefersToItsThreeFacesAndEachFaceIsDefinedOnce(string sheet, string fixture)
    {
        var overlay = Build(sheet, fixture, shared: true);
        var layer = XElement.Parse(overlay.Svg);
        var definitions = layer.Elements().Where(element => element.Name.LocalName == "defs").SelectMany(defs => defs.Elements()).ToArray();
        var ids = definitions.Select(definition => definition.Attribute("id")!.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.Matches("^face-[0-9a-f]{24}$", id));
        Assert.All(definitions, definition => Assert.Equal("g", definition.Name.LocalName));

        // A definition names no unit and carries nothing the page's style sheet selects on (section 8).
        Assert.All(definitions.SelectMany(definition => definition.DescendantsAndSelf()), element =>
            Assert.DoesNotContain(element.Attributes(), attribute => attribute.Name.LocalName is "data-unit-id" or "data-attached-id" or "data-tier" or "data-face-size"));

        var counters = layer.Descendants().Where(element => element.Attribute("role")?.Value == "button").ToArray();
        Assert.All(counters, counter =>
        {
            var uses = counter.Elements().Where(element => element.Name.LocalName == "use").ToArray();
            Assert.Equal(["far", "mid", "near"], uses.Select(use => use.Attribute("data-tier")!.Value));
            Assert.All(uses, use => Assert.Contains(use.Attribute("href")!.Value[1..], ids));
        });

        // No more definitions than three a counter, and every one of them referred to.
        Assert.True(ids.Length <= 3 * counters.Length, $"{ids.Length} definitions for {counters.Length} counters");
        Assert.Equal(ids.Order(StringComparer.Ordinal), counters.SelectMany(counter => counter.Elements()).Select(use => use.Attribute("href")?.Value[1..]).OfType<string>()
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TwoCountersWithTheSameFaceShareOneDefinition()
    {
        var squad = LayerOracleTests.Fixture("demo").First(unit => unit.Kind == "asl:squad");
        var units = new[] { squad with { Id = "same-1", Location = "bd01:C3:0" }, squad with { Id = "same-2", Location = "bd01:H7:0" } };
        var renderer = RenderingTestData.Renderer(UnitStyles.Digital);
        var layer = XElement.Parse(UnitOverlayBuilder.Build(Target, "test-set", units, renderer).Svg);
        var faces = layer.Elements().Where(element => element.Name.LocalName == "defs").ToArray();

        // An id is written on a counter's face (a sheet may print it), so the two may differ there; here they do not.
        Assert.Equal(3, faces.Length);
        var hrefs = layer.Descendants().Where(element => element.Name.LocalName == "use" && element.Parent!.Attribute("role") is not null)
            .GroupBy(use => use.Parent!.Attribute("data-unit-id")!.Value)
            .Select(group => string.Join(" ", group.Select(use => use.Attribute("href")!.Value))).ToArray();
        Assert.Equal(hrefs[0], hrefs[1]);
    }

    [Fact]
    public void TheFocusEdgeIsACustomPropertyInTheLayerOnly()
    {
        var units = LayerOracleTests.Fixture("demo");
        var renderer = RenderingTestData.Renderer(UnitStyles.Digital);
        var layer = UnitOverlayBuilder.Build(Target, "test-set", units, renderer).Svg;
        Assert.Contains("var(--unit-focus-stroke, ", layer, StringComparison.Ordinal);
        Assert.Contains("var(--unit-focus-width, ", layer, StringComparison.Ordinal);
        var standalone = UnitPreview.Document(renderer, units[0], [DetailTier.Near], null, 1, []);
        Assert.DoesNotContain("--unit-focus", standalone, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void TheLayerCarriesNoOutlineTheSpriteFinds(string sheet, string fixture)
    {
        var overlay = Build(sheet, fixture, shared: true);
        var layer = XElement.Parse(overlay.Svg);
        Assert.DoesNotContain(layer.Descendants(), element => element.Name.LocalName == "defs" && element.Attribute("data-figures") is not null);
        Assert.DoesNotContain(overlay.Fragments, fragment => fragment.Id == UnitOverlayBuilder.FiguresFragmentId);

        // Every reference finds an outline of the sprite.
        var sprite = XElement.Parse(overlay.Figures!);
        var ids = sprite.Descendants().Where(element => element.Name.LocalName == "path").Select(path => path.Attribute("id")!.Value).ToHashSet(StringComparer.Ordinal);
        var hrefs = layer.Descendants().Where(element => element.Name.LocalName == "use").Select(use => use.Attribute("href")!.Value[1..])
            .Where(href => href.StartsWith("fig-", StringComparison.Ordinal)).ToArray();
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
            var units = LayerOracleTests.Fixture("crowded");
            var origin = UnitOverlayBuilder.Build(Target, "test-set", units, renderer, null, shared: true, null, UnitFaceMode.AtOrigin);
            Assert.Null(LayerOracle.Differs(LayerOracle.Expand(UnitOverlayBuilder.Build(Target, "test-set", units, renderer, null, shared: false).Svg),
                LayerOracle.Expand(origin.Svg, [XElement.Parse(origin.Figures!)])));
            Assert.Equal(LayerOracle.Expanded(origin), LayerOracle.Expanded(UnitOverlayBuilder.Build(Target, "test-set", units, renderer)));
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
