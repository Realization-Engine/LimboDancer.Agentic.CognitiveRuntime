using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

public sealed class StyledViewTests
{
    private const string UpdateGoldenVariable = "ASL_MAPS_UPDATE_GOLDEN";
    private static readonly XNamespace Svg = BoardRenderer.SvgNamespace;
    private static readonly Lazy<BoardRenderInput> Input = new(SyntheticBoard.StyledInput);

    public static TheoryData<BoardView> Views => [BoardView.Styled, BoardView.Comparison];

    [Theory]
    [MemberData(nameof(Views))]
    public void DocumentsAreWellFormedDeterministicAndRasterFree(BoardView view)
    {
        var first = BoardRenderer.Document(Input.Value, view);
        Assert.Equal(first, BoardRenderer.Document(SyntheticBoard.StyledInput(), view));
        var document = XDocument.Parse(first);
        Assert.Empty(document.Descendants(Svg + "image"));
        var ids = document.Descendants().Select(element => (string?)element.Attribute("id")).OfType<string>().ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(BoardRenderer.Layers(view).Select(layer => "layer-" + layer), document.Root!.Elements(Svg + "g").Select(group => (string)group.Attribute("id")!));
    }

    [Fact]
    public void EveryFeatureHasItsOwnGroupInItsLayerInPaintOrder()
    {
        var document = XDocument.Parse(BoardRenderer.Document(Input.Value, BoardView.Styled));
        var model = Input.Value.Styled!.Model;
        foreach (var feature in model.Features)
        {
            var group = Assert.Single(document.Descendants(Svg + "g"), element => (string?)element.Attribute("id") == "f-" + feature.Id);
            Assert.Equal("layer-" + BoardRenderer.StyledLayerOf(feature), (string)group.Ancestors(Svg + "g").Last().Attribute("id")!);
        }

        Assert.NotNull(document.Descendants(Svg + "rect").SingleOrDefault(rect => (string?)rect.Attribute("id") == "stair-B1"));
    }

    [Fact]
    public void FeatureFragmentsMatchTheFullRendering()
    {
        var document = BoardRenderer.Document(Input.Value, BoardView.Styled);
        foreach (var feature in Input.Value.Styled!.Model.Features)
        {
            var fragment = Assert.IsType<FeatureFragment>(BoardRenderer.RenderFeature(Input.Value, feature.Id));
            var group = fragment.Svg[(fragment.Svg.IndexOf('>', StringComparison.Ordinal) + 1)..fragment.Svg.LastIndexOf("</svg>", StringComparison.Ordinal)];
            Assert.StartsWith($"<g id=\"f-{feature.Id}\"", group, StringComparison.Ordinal);
            Assert.Contains(group, document, StringComparison.Ordinal);
            Assert.Equal(BoardRenderer.StyledLayerOf(feature), fragment.Layer);
        }

        Assert.Null(BoardRenderer.RenderFeature(Input.Value, "no-such-feature"));
        Assert.Null(BoardRenderer.RenderFeature(SyntheticBoard.Input(), "woods"));
    }

    [Fact]
    public void BeforeIdsFollowPaintOrderWithinALayer()
    {
        var input = SyntheticBoard.StyledInput();
        var model = input.Styled!.Model with
        {
            Features = [.. input.Styled.Model.Features, new AreaTerrainFeature("aaa-first", -1, FeatureShape.Rectangle(FixedVector.FromPixels(0, 0), FixedVector.FromPixels(5, 5)), 60)],
        };
        var withFeature = BoardRenderInput.Create(input.Board, input.Title, input.Grid, input.Catalog, input.Facts, input.Styled with
        {
            Model = model
        });
        Assert.Equal("f-woods", BoardRenderer.RenderFeature(withFeature, "aaa-first")!.BeforeId);
        Assert.Null(BoardRenderer.RenderFeature(withFeature, "woods")!.BeforeId);
    }

    [Fact]
    public void TheDiffLayerOutlinesChangedCellsAndHexes()
    {
        var diff = XDocument.Parse(BoardRenderer.Fragment(Input.Value, BoardView.Comparison, "diff"));
        var summary = diff.Descendants(Svg + "g").Single(group => (string?)group.Attribute("id") == "diff-summary");
        Assert.True(int.Parse((string)summary.Attribute("data-cells")!, System.Globalization.CultureInfo.InvariantCulture) > 0);
        Assert.NotNull(diff.Descendants(Svg + "path").SingleOrDefault(path => (string?)path.Attribute("id") == "diff-cells"));

        // A board compared with its own model has no differences.
        var own = BoardRenderInput.Create(Input.Value.Board, "own", Input.Value.Styled!.Grid, Input.Value.Catalog, Input.Value.Styled.Facts, Input.Value.Styled);
        var ownSummary = XDocument.Parse(BoardRenderer.Fragment(own, BoardView.Comparison, "diff")).Descendants(Svg + "g").Single(group => (string?)group.Attribute("id") == "diff-summary");
        Assert.Equal("0", (string)ownSummary.Attribute("data-cells")!);
        Assert.Equal("0", (string)ownSummary.Attribute("data-hexes")!);
    }

    [Fact]
    public void StyledViewsWithoutAModelRenderEmptyLayers()
    {
        var plain = SyntheticBoard.Input();
        var document = XDocument.Parse(BoardRenderer.Document(plain, BoardView.Styled));
        Assert.DoesNotContain(document.Descendants(Svg + "g"), group => ((string?)group.Attribute("id"))?.StartsWith("f-", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ThemeStylesResolveAndPatternsExist()
    {
        var theme = RenderTheme.Board;
        var patterns = theme.Patterns.Select(pattern => "url(#pattern-" + pattern.Id + ")").ToHashSet();
        foreach (var terrain in SyntheticBoard.Catalog.Types)
        {
            if (theme.StyleFor(terrain) is { } style)
            {
                foreach (var reference in new[] { style.Fill, style.Stroke }.OfType<string>().Where(value => value.StartsWith("url(", StringComparison.Ordinal)))
                {
                    Assert.Contains(reference, patterns);
                }
            }
        }

        Assert.NotNull(theme.StyleFor(SyntheticBoard.Catalog[60]));
        Assert.NotNull(theme.StyleFor(SyntheticBoard.Catalog[42]));
        Assert.Equal("#cbb57f", theme.Tint(1));
        Assert.Equal(theme.Tint(5), theme.Tint(12));
    }

    [Fact]
    public void SmoothingWritesExactNumbers()
    {
        var path = BoardRenderer.Smooth(FeatureShape.Rectangle(FixedVector.FromPixels(0, 0), FixedVector.FromPixels(12, 6))).ToString();
        Assert.StartsWith("M0 0C", path, StringComparison.Ordinal);
        Assert.EndsWith("Z", path, StringComparison.Ordinal);
        Assert.Equal(4, path.Count(character => character == 'C'));
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void SyntheticStyledViewsMatchGolden(BoardView view)
    {
        var actual = BoardRenderer.Document(Input.Value, view);
        var path = Path.Combine(BoardRendererTests.GoldenDirectory(), $"synthetic-{BoardRenderer.ViewName(view)}.svg");
        if (Environment.GetEnvironmentVariable(UpdateGoldenVariable) == "1")
        {
            File.WriteAllText(path, actual);
        }

        Assert.True(File.Exists(path), $"Missing {path}; set {UpdateGoldenVariable}=1 to create it.");
        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), actual.ReplaceLineEndings("\n"));
    }
}
