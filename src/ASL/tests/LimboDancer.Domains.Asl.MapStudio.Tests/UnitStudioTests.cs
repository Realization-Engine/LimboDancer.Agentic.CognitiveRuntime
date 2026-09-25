using System.Xml.Linq;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Pages;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Serves only the synthetic board, so the Lab can place units without a VASL checkout.</summary>
internal sealed class SyntheticBoards(MapService maps) : IBoardProvider
{
    public string? SourceDescription => "Synthetic test source";

    public string? CatalogBlob => null;

    public IReadOnlyList<BoardListing> List() => [new BoardListing(FakeBoardProvider.Board.Ref, FakeBoardProvider.Board.Title)];

    public BoardLoadResult Load(BoardRef board) =>
        board == FakeBoardProvider.Board.Ref ? new BoardLoadResult(FakeBoardProvider.Board, [])
        : board.Kind == BoardRefKind.ComposedMap ? maps.Load(board)
        : new BoardLoadResult(null, []);

    public BoardLoadResult? Cached(BoardRef board) => Load(board);
}

/// <summary>The Units layer's inputs and the Unit Lab (Unit Display Design, sections 10 and 12).</summary>
public sealed class UnitStudioTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-units-" + Guid.NewGuid().ToString("N"));
    private readonly StudioOptions options;
    private readonly UnitLibrary library;
    private readonly MapService maps;
    private readonly BunitContext context = new();

    public UnitStudioTests()
    {
        options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        library = new UnitLibrary(options);
        maps = new MapService(options, new FakeVaslMapSource());
        context.Services.AddSingleton(options);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(maps);
        context.Services.AddSingleton<IBoardProvider>(new SyntheticBoards(maps));
    }

    [Fact]
    public void TheBuiltInSetIsSyntheticAndShowsOnlyWhereItsBoardIs()
    {
        var entry = Assert.Single(library.Sets());
        Assert.True(entry.BuiltIn);
        Assert.True(entry.Set.Synthetic);
        Assert.Equal("bd01-demo", entry.Set.SetId);
        Assert.Empty(library.SetsFor(FakeBoardProvider.Board));
        Assert.Equal(12, library.Examples.Count);
    }

    [Fact]
    public void ASavedSetShowsOnItsBoardWithBoardGeometryAnchors()
    {
        var unit = Squad("s1", "ab-synthetic:B1:0");
        Assert.Null(library.SaveSet(new UnitPlacementSet("lab-units", "Lab units", true, ["asl@1.0.0"], [unit], string.Empty)));
        var entry = Assert.Single(library.SetsFor(FakeBoardProvider.Board));
        Assert.False(entry.BuiltIn);
        var overlay = library.Overlay(FakeBoardProvider.Board, entry.Set, library.Renderer(UnitLibrary.DefaultSheet)!);
        var placed = Assert.Single(overlay.Units);
        Assert.Equal(FakeBoardProvider.Board.Render.Grid.Geometry.IndexOf(HexName.Parse("B1")), placed.Hex);
        Assert.Equal("layer-units", XElement.Parse(overlay.Svg).Attribute("id")!.Value);

        // Built-in ids stay read-only.
        Assert.NotNull(library.SaveSet(entry.Set with
        {
            SetId = "bd01-demo"
        }));
    }

    [Fact]
    public void AComposedMapShowsSetsForItsPlacedBoards()
    {
        var map = maps.Save("Unit map", [new BoardPlacement(BoardRef.Parse("bd02")), new BoardPlacement(BoardRef.Parse("bd03"), 1, 0)]).Ref!;
        var board = maps.Load(map).Board!;
        Assert.Null(library.SaveSet(new UnitPlacementSet("map-units", "Map units", true, ["asl@1.0.0"],
            [Squad("left", "bd02:A1:0"), Squad("right", "bd03:B1:0"), Squad("missing", "bd09:A1:0")], string.Empty)));
        var entry = Assert.Single(library.SetsFor(board));
        var overlay = library.Overlay(board, entry.Set, library.Renderer(UnitStyles.Classic)!);
        Assert.Equal(2, overlay.Units.Count);
        Assert.Equal(board.Composition!.Map.Locate(BoardRef.Parse("bd03"), HexName.Parse("B1")), overlay.Units.Single(u => u.Document.Id == "right").Hex);
        Assert.Contains(overlay.Diagnostics, diagnostic => diagnostic.Contains("missing", StringComparison.Ordinal));
    }

    [Fact]
    public void SheetsSaveUnderNewNamesOnly()
    {
        Assert.NotNull(library.SaveSheet(UnitStyles.Digital, "unit { fill: #fff; }"));
        Assert.NotNull(library.SaveSheet("Not A Slug", "unit { fill: #fff; }"));
        Assert.Null(library.SaveSheet("my-sheet", "unit { fill: #ffffff; }"));
        Assert.Equal([UnitStyles.Classic, UnitStyles.Digital, "my-sheet"], library.SheetNames());
        Assert.NotNull(library.Renderer("my-sheet"));
    }

    [Fact]
    public void TheLabPreviewsEachSheetAndTier()
    {
        var lab = context.Render<UnitLab>();
        Assert.Equal(6, lab.FindAll(".previews span.preview[data-preview]").Count(span => span.GetAttribute("data-preview") is UnitStyles.Classic or UnitStyles.Digital));
        Assert.Contains("German 1st Line squad A, 4-6-7, with light MG 3-6", lab.Find("#lab-name").TextContent, StringComparison.Ordinal);
        Assert.Contains("No findings.", lab.Find("#lab-findings").TextContent, StringComparison.Ordinal);
        Assert.Contains("broken face", lab.Markup, StringComparison.Ordinal);
        Assert.Contains("opponent's view", lab.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EditingAFormFieldRedrawsThePreview()
    {
        var lab = context.Render<UnitLab>();
        lab.Find("#unit-front-firepower").Change("5");
        Assert.Contains("5-6-7", lab.Find("#lab-name").TextContent, StringComparison.Ordinal);
        var marks = lab.FindAll("[data-mark='underline']").Count;
        lab.Find("#unit-front-spraying-fire").Change(true);
        Assert.True(lab.FindAll("[data-mark='underline']").Count > marks);
        lab.Find("#unit-state-pinned").Change(true);
        Assert.Contains("data-badge=\"PIN\"", lab.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AWrongTypeIsAFindingAndStopsThePreview()
    {
        var lab = context.Render<UnitLab>();
        lab.Find("#unit-front-firepower").Change("four");
        Assert.Contains("UNIT-DOC-004", lab.Find("#lab-findings").TextContent, StringComparison.Ordinal);
        Assert.Contains("The document is refused", lab.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ASheetSyntaxErrorIsShownWithItsPosition()
    {
        var lab = context.Render<UnitLab>();
        lab.Find("#lab-sheet-text").Input("asl|squad { fill: #fff");
        var diagnostics = lab.Find("#lab-sheet-diagnostics").TextContent;
        Assert.Contains("line 1, column", diagnostics, StringComparison.Ordinal);
        Assert.True(lab.Find("#lab-save-sheet").HasAttribute("disabled"));
    }

    [Fact]
    public void ThePlausibilityCheckWarnsInTheLab()
    {
        var lab = context.Render<UnitLab>();
        lab.Find("#lab-example").Change("example-leader");
        lab.Find("#unit-front-range").Change("4");
        Assert.Contains("A1.22, p. 44", lab.Find("#lab-findings").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("The document is refused", lab.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EquipmentCanBeAttachedAndRemoved()
    {
        var lab = context.Render<UnitLab>();
        lab.Find("#lab-example").Change("example-half-squad");
        lab.Find("#lab-attach").Click();
        lab.Find("#att0-front-firepower").Change("2");
        Assert.Contains("with MG", lab.Find("#lab-name").TextContent, StringComparison.Ordinal);
        lab.Find("#att0-front-size").Change("light");
        Assert.Contains("with light MG", lab.Find("#lab-name").TextContent, StringComparison.Ordinal);
        lab.Find("#att0-remove").Click();
        Assert.DoesNotContain("with", lab.Find("#lab-name").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void UnitsArePlacedOnABoardAndSavedAsASet()
    {
        var lab = context.Render<UnitLab>();
        lab.Find("#lab-board").Change("ab-synthetic");
        lab.Find("#lab-hex").Change("Z9");
        lab.Find("#lab-place").Click();
        Assert.Contains("is not a hex on ab-synthetic", lab.Markup, StringComparison.Ordinal);
        lab.Find("#lab-hex").Change("B1");
        lab.Find("#lab-place").Click();
        lab.Find("#lab-place").Click();
        Assert.Equal(2, lab.FindAll("#lab-placed li").Count);
        lab.Find("#lab-set").Change("lab-test");
        lab.Find("#lab-save-set").Click();
        Assert.Equal("boards/ab-synthetic?units=lab-test", lab.Find("#lab-view").GetAttribute("href"));
        var saved = library.Set("lab-test")!;
        Assert.True(saved.Set.Synthetic);
        Assert.Equal(["example-squad", "example-squad-2"], saved.Set.Units.Select(unit => unit.Id));
        Assert.All(saved.Set.Units, unit => Assert.Equal("ab-synthetic:B1:0", unit.Location));
    }

    private static UnitDocument Squad(string id, string location) =>
        Assert.Single(UnitDocumentReader.Read($$"""
            { "vocabulary": ["asl@1.0.0"], "id": "{{id}}", "kind": "asl:squad", "side": "german", "location": "{{location}}",
              "faces": { "front": { "firepower": 4, "range": 6, "morale": 7 } } }
            """, Units.Vocabulary.UnitVocabulary.Asl()).Documents);

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

public sealed class UnitViewerTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    [Fact]
    public async Task TheViewerOffersAUnitsLayerForSavedSets()
    {
        var library = factory.Services.GetRequiredService<UnitLibrary>();
        var unit = Assert.Single(UnitDocumentReader.Read("""
            { "vocabulary": ["asl@1.0.0"], "id": "v1", "kind": "asl:squad", "side": "russian", "location": "ab-synthetic:C1:0" }
            """, library.Vocabulary).Documents);
        Assert.Null(library.SaveSet(new UnitPlacementSet("viewer-units", "Viewer units", true, ["asl@1.0.0"], [unit], string.Empty)));

        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/boards/ab-synthetic?units=viewer-units", UriKind.Relative));
        Assert.Contains("> Units</label>", html, StringComparison.Ordinal);
        Assert.Contains("Viewer units (synthetic)", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Demo units", html, StringComparison.Ordinal);

        var lab = await client.GetStringAsync(new Uri("/units/lab?board=ab-synthetic", UriKind.Relative));
        Assert.Contains("Unit Lab", lab, StringComparison.Ordinal);
        Assert.Contains("value=\"ab-synthetic\"", lab, StringComparison.Ordinal);
    }
}
