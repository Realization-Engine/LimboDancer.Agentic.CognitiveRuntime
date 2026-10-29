using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Fidelity;
using LimboDancer.Domains.Asl.MapStudio.Components.Maps;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Vasl;
using Microsoft.Extensions.DependencyInjection;
using HomePage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Home;
using MapsPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Maps;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Pass 22c (plan sections 12.1, 12.2, and 16.4 to 16.9): the Board library's search, filters, and states; the map composer's editors.</summary>
public sealed class CollectionPageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-collection-pages-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly FidelityJobRunner runner;
    private readonly StudioOptions options;

    public CollectionPageTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var store = new FidelityReportStore(options);
        store.Save(FidelityTestData.Report(new DateTimeOffset(2026, 9, 24, 8, 0, 0, TimeSpan.Zero)));
        runner = new FidelityJobRunner(new FakeFidelityBatch(), store);
        var authored = new AuthoredBoardService(options, new FakeCatalogSource());
        var maps = new MapService(options, new FakeVaslMapSource());
        context.Services.AddSingleton(options);
        context.Services.AddSingleton(authored);
        context.Services.AddSingleton(maps);
        context.Services.AddSingleton<IVaslMapSource>(new FakeVaslMapSource());
        context.Services.AddSingleton<IBoardProvider>(new FakeBoardProvider(authored, maps));
        context.Services.AddSingleton(runner);
        context.Services.AddSingleton(new LibraryViewState());
        context.Services.AddSingleton(new MapHexIndexStore(options));
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        this.options = options;
    }

    public void Dispose()
    {
        context.Dispose();
        runner.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TheLibrarySearchesFiltersAndKeepsItsFiltersOnReturn()
    {
        var page = context.Render<HomePage>();
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("#library-vasl tr[data-board]").Count));
        Assert.Contains("2 verified", page.Find("#library-totals").TextContent, StringComparison.Ordinal);

        // Out-of-scope boards show only when asked.
        // The page finishes its first render (the scroll module) before the first change is taken.
        page.WaitForAssertion(() => Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "restore"));
        page.Find("#library-out-of-scope").Change(true);
        Assert.Equal(3, page.FindAll("#library-vasl tr[data-board]").Count);

        // Search by reference or name, with the count.
        page.Find("#library-search").Input("bd02");
        Assert.Single(page.FindAll("#library-vasl tr[data-board]"));
        Assert.StartsWith("1 of", page.Find("#library-count").TextContent.Trim(), StringComparison.Ordinal);
        Assert.Contains("No map matches", page.Find("#library-maps-empty").TextContent, StringComparison.Ordinal);

        // A status narrows the list to VASL boards; a status nothing has leaves one message, not one per section; then Clear.
        page.Find("#library-search").Input(string.Empty);
        page.Find("#library-status").Change("verified");
        Assert.Equal(2, page.FindAll("#library-vasl tr[data-board]").Count);
        Assert.Empty(page.FindAll("#library-maps-empty"));
        page.Find("#library-status").Change("notchecked");
        Assert.NotNull(page.Find("#library-no-match"));
        Assert.Empty(page.FindAll("#library-vasl-no-match"));
        page.Find("#library-clear").Click();
        Assert.Equal(2, page.FindAll("#library-vasl tr[data-board]").Count);
        Assert.Empty(page.FindAll("#library-clear"));

        // The filters are as the user left them when the page opens again, as on return from a viewer.
        page.Find("#library-type").Change("vasl");
        page.Find("#library-search").Input("synthetic");
        var again = context.Render<HomePage>();
        again.WaitForAssertion(() => Assert.Single(again.FindAll("#library-vasl tr[data-board]")));
        Assert.Equal("synthetic", again.Find("#library-search").GetAttribute("value"));
        Assert.Empty(again.FindAll("#library-authored-empty"));
    }

    [Fact]
    public void TheHasTerrainFilterKeepsTheBoardsHoldingEveryFeatureAndLinksTheViewerToIt()
    {
        // No index yet: the filter says so and points to the batch (ruling RMH.5).
        var page = context.Render<HomePage>();
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("#library-vasl tr[data-board]").Count), TimeSpan.FromSeconds(10));
        Assert.Contains("no MapHexIndex yet", page.Find("#library-terrain-none").TextContent, StringComparison.Ordinal);

        var rows = page.FindAll("#library-vasl tr[data-board]").Select(row => row.GetAttribute("data-board")!).ToArray();
        var folder = new MapHexIndexStore(options).Directory;
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "maphexindex-20261028T080000Z.json"), new Maps.Derivation.MapHexIndex("1.0.0", "test", null, null, "fidelity-20261028T080000Z",
        [
            new(rows[0], "verified", null, [new("flag:cliff", "flag", "cliff", ["G7"]), new("terrain:Woods", "terrain", "Woods", ["C3", "D4"])]),
            new(rows[1], "verified", null, [new("terrain:Woods", "terrain", "Woods", ["A1"])]),
        ]).ToJson());
        page = context.Render<HomePage>();
        // The dropdown of checkboxes offers every feature of the index, one box each, and its closed line says how many are offered.
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".library-terrains")), TimeSpan.FromSeconds(10));
        Assert.Equal(["flag:cliff", "terrain:Woods"], page.FindAll("input.library-terrain").Select(box => box.GetAttribute("data-unit")));
        Assert.Contains("choose (2 offered)", page.Find(".library-terrains summary").TextContent, StringComparison.Ordinal);

        // A feature only one board holds keeps that board, hides authored boards and maps, and its link carries the feature.
        page.Find("input.library-terrain[data-unit='flag:cliff']").Change(true);
        Assert.Equal([rows[0]], page.FindAll("#library-vasl tr[data-board]").Select(row => row.GetAttribute("data-board")));
        Assert.Empty(page.FindAll("#library-maps"));
        Assert.EndsWith("?terrain=flag%3Acliff", page.Find("#library-vasl tr[data-board] a").GetAttribute("href"), StringComparison.Ordinal);

        // A second feature both boards hold keeps the same board, and the link carries both, in the order chosen, for the viewer to outline each.
        page.Find("input.library-terrain[data-unit='terrain:Woods']").Change(true);
        Assert.Equal([rows[0]], page.FindAll("#library-vasl tr[data-board]").Select(row => row.GetAttribute("data-board")));
        Assert.EndsWith("?terrain=flag%3Acliff&terrain=terrain%3AWoods", page.Find("#library-vasl tr[data-board] a").GetAttribute("href"), StringComparison.Ordinal);
        Assert.True(page.Find("input.library-terrain[data-unit='terrain:Woods']").HasAttribute("checked"));

        // Each chosen feature has a chip; a click on one removes it and unticks its box; unticking the other gives the whole list back.
        Assert.Equal(["flag:cliff", "terrain:Woods"], page.FindAll("button[data-terrain]").Select(chip => chip.GetAttribute("data-terrain")));
        page.Find("button[data-terrain='terrain:Woods']").Click();
        Assert.False(page.Find("input.library-terrain[data-unit='terrain:Woods']").HasAttribute("checked"));
        page.Find("input.library-terrain[data-unit='flag:cliff']").Change(false);
        Assert.Empty(page.FindAll("button[data-terrain]"));
        Assert.Equal(2, page.FindAll("#library-vasl tr[data-board]").Count);
        Assert.False(page.Find("input.library-terrain[data-unit='flag:cliff']").HasAttribute("checked"));
    }

    [Fact]
    public void TheLibrarySaysWhenItCannotBeRead()
    {
        context.Services.AddSingleton<IBoardProvider>(new UnreadableBoardProvider());
        var page = context.Render<HomePage>();
        page.WaitForAssertion(() => Assert.Contains("could not be read", page.Find("#library-failure").TextContent, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("#library-vasl"));
    }

    [Fact]
    public void TheComposerKeepsPlacementIdentityAndRuleOrderAndMarksAnOldCheck()
    {
        var page = context.Render<MapsPage>();
        page.Find("#map-add-board").Click();
        page.Find("#map-add-board").Click();
        var ids = page.FindAll("#map-placements tr[data-placement]").Select(row => row.GetAttribute("data-placement")).ToArray();
        Assert.Equal(3, ids.Length);

        // Removing the middle placement keeps the others as they were.
        page.FindAll("#map-placements tr[data-placement]")[1].QuerySelector("button")!.Click();
        Assert.Equal([ids[0], ids[2]], page.FindAll("#map-placements tr[data-placement]").Select(row => row.GetAttribute("data-placement")));

        // Rules keep the order they were added in, in the list and in the compact form.
        page.Find("#map-next-rule").Change("WoodsToOpenGround");
        page.Find("#map-add-rule").Click();
        page.Find("#map-add-rule").Click();
        Assert.Equal(["WoodsToOpenGround", "NoWhiteHexIDs"], page.FindAll("#map-rules li").Select(item => item.GetAttribute("data-rule")));
        Assert.Contains("[WoodsToOpenGround,NoWhiteHexIDs]", page.Find("#map-text").GetAttribute("value"), StringComparison.Ordinal);

        // A check's result stays after an edit, marked out of date.
        page.Find("#map-check").Click();
        page.WaitForAssertion(() => Assert.Contains("VASL", page.Find("#map-result").TextContent, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("#map-result-stale"));
        page.FindAll("#map-placements tr[data-placement]")[1].QuerySelector("input[type=number]")!.Change("2");
        page.WaitForAssertion(() => Assert.Contains("VASL", page.Find("#map-result-stale").TextContent, StringComparison.Ordinal));
        Assert.DoesNotContain("VASL", page.Find("#map-result").TextContent, StringComparison.Ordinal);

        // Text that is not placements says so, and changes nothing.
        page.Find("#map-text").Change("not placements");
        page.Find("#map-use-text").Click();
        Assert.NotNull(page.Find("#map-text-problem"));
        Assert.Equal(2, page.FindAll("#map-placements tr[data-placement]").Count);
    }

    [Fact]
    public void TheComposerMovesRulesKeepsOneOfEachAndStartsAFreshMapWithoutAnOldResult()
    {
        context.Services.GetRequiredService<MapService>().Save("Saved map", [new BoardPlacement(BoardRef.Parse("bd02"))]);
        var page = context.Render<MapsPage>();
        page.Find("#map-add-rule").Click();
        page.Find("#map-add-rule").Click();
        page.Find("#map-rules li[data-rule='WoodsToOpenGround'] button[aria-label='Move WoodsToOpenGround up']").Click();
        Assert.Equal(["WoodsToOpenGround", "NoWhiteHexIDs"], page.FindAll("#map-rules li").Select(item => item.GetAttribute("data-rule")));

        // A rule named twice in the text is kept once.
        page.Find("#map-text").Change("02@0,0[NoWhiteHexIDs,NoWhiteHexIDs]");
        page.Find("#map-use-text").Click();
        Assert.Single(page.FindAll("#map-rules li"));

        // Opening a saved map after a check shows no result from the map before it.
        page.Find("#map-check").Click();
        page.WaitForAssertion(() => Assert.Contains("VASL", page.Find("#map-result").TextContent, StringComparison.Ordinal));
        page.Find("#map-saved button.link").Click();
        page.WaitForAssertion(() => Assert.DoesNotContain("VASL", page.Find("#map-result").TextContent, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("#map-result-stale"));
    }

    [Fact]
    public void TheNewBoardFormFillsTheReferenceAndWarnsOfAVaslDraft()
    {
        var drafts = new List<NewBoardForm.NewBoardDraft>();
        var form = context.Render<NewBoardForm>(parameters => parameters
            .Add(component => component.Draft, new NewBoardForm.NewBoardDraft("x", "x", "blank", 33, 10, null))
            .Add(component => component.DraftChanged, draft => drafts.Add(draft)));
        Assert.Empty(form.FindAll("#board-draft-notice"));
        form.Find("#board-name").Input("Hill 621, north");
        Assert.Equal("hill-621-north", drafts[^1].Slug);

        form.Render(parameters => parameters.Add(component => component.Draft, drafts[^1] with { Start = "vasl" }));
        Assert.Contains("draft", form.Find("#board-draft-notice").TextContent, StringComparison.Ordinal);
        Assert.Contains("warning", form.Find("#board-draft-notice").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRunControlsShowProgressWhileRunning()
    {
        var idle = context.Render<FidelityRunControls>(parameters => parameters.Add(component => component.State, FidelityJobState.Failed).Add(component => component.Error, "disk full"));
        Assert.Empty(idle.FindAll("#fidelity-cancel"));
        Assert.Contains("disk full", idle.Find("#fidelity-status").TextContent, StringComparison.Ordinal);

        var running = context.Render<FidelityRunControls>(parameters => parameters
            .Add(component => component.State, FidelityJobState.Running).Add(component => component.Progress, new BatchProgress(1, 4, "bd02")));
        Assert.True(running.Find("#fidelity-start").HasAttribute("disabled"));
        Assert.NotNull(running.Find("#fidelity-cancel"));
        Assert.Contains("1 of 4, last bd02", running.Markup, StringComparison.Ordinal);

        Assert.NotNull(context.Render<FidelityReportPicker>(parameters => parameters.Add(component => component.Reports, [])).Find("#fidelity-no-reports"));
    }

    private sealed class UnreadableBoardProvider : IBoardProvider
    {
        public string? SourceDescription => "An unreadable source";

        public string? CatalogBlob => null;

        public IReadOnlyList<BoardListing> List() => throw new IOException("The checkout is not readable.");

        public BoardLoadResult Load(BoardRef board) => throw new IOException("The checkout is not readable.");

        public BoardLoadResult? Cached(BoardRef board) => null;
    }
}
