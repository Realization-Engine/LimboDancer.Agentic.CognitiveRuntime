using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>bUnit tests for the Studio's board components (Architecture and Rendering Design, section 8).</summary>
public sealed class ComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    [Fact]
    public void ThePaletteMarksTheActiveToolAndReportsChanges()
    {
        string? chosen = null;
        var palette = context.Render<ToolPalette>(parameters => parameters
            .Add(component => component.Tool, EditorTool.Area)
            .Add(component => component.ToolChanged, tool => chosen = tool));

        var buttons = palette.FindAll("button.tool");
        Assert.Equal(EditorTool.All.Count, buttons.Count);
        Assert.Equal("Area", palette.Find("button.tool.active").TextContent);
        buttons.Single(button => button.TextContent == "Hexside").Click();
        Assert.Equal(EditorTool.Hexside, chosen);
    }

    [Fact]
    public void TheValidationPanelSummarizesAndSelectsFindings()
    {
        ValidationFinding? selected = null;
        var finding = new ValidationFinding("MAP-VAL-005", MapDiagnosticSeverity.Warning, "Stairway in B1.", new HexIndex(1, 1));
        var panel = context.Render<ValidationPanel>(parameters => parameters
            .Add(component => component.Report, new ValidationReport([finding]))
            .Add(component => component.OnSelect, item => selected = item));

        Assert.Contains("0 errors", panel.Markup, StringComparison.Ordinal);
        Assert.Contains("1 warnings", panel.Markup, StringComparison.Ordinal);
        panel.Find("button.link").Click();
        Assert.Equal(finding, selected);

        var valid = context.Render<ValidationPanel>(parameters => parameters.Add(component => component.Report, new ValidationReport([])));
        Assert.Contains("The board is valid.", valid.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PropertiesOfferOnlyCodesOfTheFeaturesKind()
    {
        Feature? changed = null;
        var feature = new AreaTerrainFeature("woods", 0, FeatureShape.Rectangle(FixedVector.FromPixels(0, 0), FixedVector.FromPixels(10, 10)), 60);
        var properties = context.Render<FeatureProperties>(parameters => parameters
            .Add(component => component.Feature, feature)
            .Add(component => component.Catalog, SyntheticBoard.Catalog)
            .Add(component => component.BaseCode, (byte)0)
            .Add(component => component.OnChange, value => changed = value));

        var options = properties.FindAll("#feature-code option").Select(option => option.TextContent).ToArray();
        Assert.Contains("Woods", options);
        Assert.Contains("Open Ground", options);
        Assert.DoesNotContain("Wall", options);
        Assert.DoesNotContain("Stone Building, 2 Level", options);

        properties.Find("#feature-layer").Change("4");
        Assert.Equal(4, Assert.IsType<AreaTerrainFeature>(changed).Layer);
        properties.Find("#feature-code").Change("0");
        Assert.Equal(0, Assert.IsType<AreaTerrainFeature>(changed).Code);
    }

    [Fact]
    public void TheLayerListGroupsFeaturesByKind()
    {
        string? selected = null;
        var model = SyntheticBoard.Model();
        var list = context.Render<LayerList>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.Catalog, SyntheticBoard.Catalog)
            .Add(component => component.SelectedId, "house")
            .Add(component => component.OnSelect, id => selected = id));

        Assert.Equal(model.Features.Count, list.FindAll("li").Count);
        Assert.Contains("building", list.Markup, StringComparison.Ordinal);
        Assert.Single(list.FindAll("li.selected"));
        list.FindAll("button.link")[0].Click();
        Assert.NotNull(selected);
    }

    [Fact]
    public void TheInspectorShowsFactsSamplesFeaturesAndTheLocation()
    {
        var input = SyntheticBoard.StyledInput();
        var styled = input.Styled!;
        var hex = new HexIndex(1, 1);
        string? copied = null;
        var inspector = context.Render<HexInspector>(parameters => parameters
            .Add(component => component.Board, BoardRef.Parse("ab-synthetic"))
            .Add(component => component.Facts, styled.Facts[hex])
            .Add(component => component.Grid, styled.Grid)
            .Add(component => component.Catalog, SyntheticBoard.Catalog)
            .Add(component => component.Model, styled.Model)
            .Add(component => component.OnCopy, location => copied = location));

        Assert.Contains("ab-synthetic:B1:0", inspector.Markup, StringComparison.Ordinal);
        Assert.Equal(1 + 15, inspector.FindAll("table.samples tr").Count);
        Assert.Contains("Stone Building, 2 Level", inspector.Markup, StringComparison.Ordinal);
        inspector.Find("h2 button.link").Click();
        Assert.Equal("ab-synthetic:B1:0", copied);
    }

    public void Dispose() => context.Dispose();
}
