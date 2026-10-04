using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 28c: the Play workspace in use. A page opened on a game waits behind the hand-over (backlog section 37); the context offers the hand-over a
/// waiting answer needs, and the DEFENDER passes from its own view; a Confirm at another revision is stale (plan section 13.4); the records are
/// grouped by turn and phase (backlog section 43); and under 1024px the panes are labelled tabs (plan section 11.1).
/// </summary>
public sealed class PlayPagePass28cTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-28c-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass28cTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
        live = new LivePlay(library, boards, dice.Roller);
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
        context.Services.AddSingleton(dice);
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Board => FakeBoardProvider.Board.Ref.Value;

    /// <summary>An Open Ground hex and two Open Ground hexes ADJACENT to it at its level, with no hexside terrain between.</summary>
    private (string One, string Two, string Three) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool Open(HexName? hex, int level) => hex is { } at && handle.HexFacts(at) is { Center.Terrain.Name: "Open Ground" } facts && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let one = handle.Geometry.NameOf(index)
                let level = handle.HexFacts(one)!.BaseLevel
                where Open(one, level) && handle.HexFacts(one)!.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff)
                let near = Enum.GetValues<HexsideDirection>().Select(side => handle.Neighbor(one, side)).Where(hex => Open(hex, level)).ToArray()
                where near.Length >= 2
                select ($"{Board}:{one}:0", $"{Board}:{near[0]}:0", $"{Board}:{near[1]}:0")).First();
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal),
            page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-add").Click();
    }

    private IRenderedComponent<PlayPage> NewGame(bool narrow = false)
    {
        var page = context.Render<PlayPage>(parameters => parameters.AddCascadingValue("StudioNarrow", narrow));
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        return page;
    }

    /// <summary>The German squad g1 and the Russian squad r1 in ADJACENT hexes, set up; play starts in the German RPh.</summary>
    private IRenderedComponent<PlayPage> Started(bool narrow = false)
    {
        var hexes = Hexes();
        var page = NewGame(narrow);
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        return page;
    }

    private GameState Current => live.History("village")!.Current!;

    // Backlog section 37, built in pass 28c: a page opened on a game shows nothing of either side until its viewer says who they are.
    [Fact]
    public void APageOpenedOnAGameWaitsBehindTheHandOver()
    {
        Started();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("games/play?game=village");
        var opened = context.Render<PlayPage>();
        Assert.Contains("German side", opened.Find("#play-handover-title").TextContent, StringComparison.Ordinal);
        Assert.Empty(opened.FindAll("#play-workspace"));
        Assert.Empty(opened.FindAll("#play-panel-map"));

        // The context is known to both sides, so it shows during the hand-over.
        Assert.Contains("Rally Phase", opened.Find("#play-summary").TextContent, StringComparison.Ordinal);
        opened.Find("#play-handover-confirm").Click();
        Assert.Single(opened.FindAll("#play-workspace"));

        // The actions take the focus once the viewer is at the screen.
        opened.WaitForAssertion(() => Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "reveal"));
    }

    // Plan section 13.4: a proposal made before another commit is stale; it is refused and never confirmed.
    [Fact]
    public void AConfirmAfterAnotherCommitIsStale()
    {
        var page = Started();
        page.Find("#propose-advance").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("Advance the phase", page.Find("#play-review-jump").TextContent, StringComparison.Ordinal);
        var revision = live.History("village")!.Events.Count;

        // Another page commits first.
        var other = context.Render<PlayPage>();
        other.OpenGame("village");
        other.EndPhase(Commit);
        Assert.Equal("pfph", Current.Phase);

        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.StartsWith("Stale", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));

        // Pass 31c (design D15): a side is shown no revision number in a game still played, so it reads only that the game moved on.
        Assert.Equal("The game has moved on since this proposal was made. Propose again.", page.Find("#play-reasons").TextContent.Trim());
        Assert.Empty(page.FindAll("#play-revision"));
        Assert.Empty(page.FindAll("#play-confirm"));
        Assert.Equal("pfph", Current.Phase);
        Assert.True(live.History("village")!.Events.Count > revision);
    }

    // Backlog section 37: after a step the DEFENDER may fire or pass; the context hands the screen over, and the DEFENDER passes from its own view.
    [Fact]
    public void AStepHandsTheScreenToTheDefenderWhoMayPass()
    {
        var hexes = Hexes();
        var page = Started();
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        Assert.Equal("mph", Current.Phase);

        page.Find(".move-unit[data-unit='g1']").Change(true);
        page.Find("#move-to").Change(hexes.Three);
        Commit(page, "#propose-move");
        Assert.Contains("DEFENDER may fire", page.Find("#play-awaiting").TextContent, StringComparison.Ordinal);
        Assert.Contains("Russian side", page.Find("#play-hand-over").TextContent, StringComparison.Ordinal);

        // A8.1, A8.11 (referee, pass 28c): only the DEFENDER passes; the moving side's view has no pass.
        Assert.Empty(page.FindAll("#propose-pass"));
        Assert.NotEmpty(page.Find("#play-status").TextContent);

        page.Find("#play-hand-over").Click();
        Assert.Contains("Russian side", page.Find("#play-handover-title").TextContent, StringComparison.Ordinal);

        // Ruling R23.2: the last view's announcement does not stay for the next viewer.
        Assert.Empty(page.Find("#play-status").TextContent);
        page.Find("#play-handover-confirm").Click();
        Assert.Empty(page.FindAll("#play-hand-over"));
        Assert.Contains($"The moving stack is at {DisplayText.Place(1, hexes.Three)}:", page.Find("#dff-note").TextContent, StringComparison.Ordinal);

        // Ruling R23.1: the DEFENDER's review of its pass names no mover, by its id or by its name.
        page.Find("#propose-pass").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.DoesNotContain("g1", page.Find("#play-reasons").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("squad G1", page.Find("#play-reasons").TextContent, StringComparison.Ordinal);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.True(Current.Movement is { WindowOpen: false }));
    }

    // Backlog section 43: the records sit under their turn and phase, and the strip shows the latest one.
    [Fact]
    public void RecordsAreGroupedByTurnAndPhase()
    {
        var hexes = Hexes();
        var page = Started();
        page.EndPhase(Commit);
        page.Find("#fire-from").Change(hexes.One);
        page.Find(".fire-firer[data-unit='g1']").Change(true);
        page.Find("#fire-target").Change(hexes.Two);
        dice.Enqueue([6, 5]);
        Commit(page, "#propose-fire");

        // Pass 31c (design D17): the heading names the side whose phase it is, and the latest line says the firer by name, with its hex.
        Assert.Equal("Turn 1, German Prep Fire Phase", page.Find("#play-fires .record-when").TextContent);
        var latest = page.Find("#play-latest").TextContent;
        Assert.Equal($"Latest: Turn 1, German Prep Fire Phase: 4-6-7 squad G1 in {DisplayText.Place(1, hexes.One)} fires at {DisplayText.Place(1, hexes.Two)}: no effect.", latest);
    }

    // Plan section 11.1: under 1024px the map, the actions, and the activity are tabs; a pane not chosen is hidden, so it keeps its state.
    [Fact]
    public void UnderTheNarrowWidthThePanesAreLabelledTabs()
    {
        var page = Started(narrow: true);
        Assert.Equal(["Map", "Actions", "Activity"], page.FindAll("#play-workspace [aria-label=Workspace] [role=tab]").Select(tab => tab.TextContent.Trim()));
        Assert.Equal("play-panel-map", page.Find("#play-tab-map").GetAttribute("aria-controls"));

        // Table player, pass 29: after a commit the Actions tab is open, so the next action needs no tab change.
        Assert.False(page.Find("#play-panel-actions").HasAttribute("hidden"));
        page.Find("#play-tab-map").Click();
        Assert.False(page.Find("#play-panel-map").HasAttribute("hidden"));
        Assert.True(page.Find("#play-panel-actions").HasAttribute("hidden"));
        Assert.Equal("tabpanel", page.Find("#play-panel-actions").GetAttribute("role"));

        page.Find("#play-tab-actions").Click();
        Assert.False(page.Find("#play-panel-actions").HasAttribute("hidden"));
        Assert.True(page.Find("#play-panel-map").HasAttribute("hidden"));
        Assert.Single(page.FindAll("#play-panel-map"));

        // UI review, pass 28c: a hand-over opens the Actions tab, so the focus has a pane to go to.
        page.Find("#play-tab-map").Click();
        page.ViewAs("russian");
        Assert.False(page.Find("#play-panel-actions").HasAttribute("hidden"));
    }

    // Wide, the panes are named regions with no tabs.
    [Fact]
    public void WideThePanesAreNamedRegions()
    {
        var page = Started();
        Assert.Empty(page.FindAll("#play-workspace [role=tablist][aria-label=Workspace]"));
        Assert.Equal("Actions", page.Find("#play-panel-actions").GetAttribute("aria-label"));
        Assert.False(page.Find("#play-panel-actions").HasAttribute("hidden"));
        Assert.Contains("Live", page.Find("#play-live").TextContent, StringComparison.Ordinal);
    }
}
