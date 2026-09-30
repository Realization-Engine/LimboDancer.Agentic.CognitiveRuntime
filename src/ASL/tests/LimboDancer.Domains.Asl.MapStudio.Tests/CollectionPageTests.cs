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
        context.JSInterop.Mode = JSRuntimeMode.Loose;
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
