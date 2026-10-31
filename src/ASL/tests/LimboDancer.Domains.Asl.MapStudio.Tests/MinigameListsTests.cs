using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using CardEditorPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.CardEditor;
using MinigameListPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.MinigameList;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;
using ReplayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Replay;
using ScenariosPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Scenarios;
using WorkbenchPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Workbench;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Minigames page design (2026-10-31): the user's games and the minigames' games are never listed together. Play, Replay, and Scenarios list
/// the other games and cards, and a minigame's alone while one of its games or its card is open; the Rule Workbench lists the minigames' games
/// unless a link opened another game; the Minigames page lists each bundle's games. A game is a minigame's by its record's "minigame" field.
/// </summary>
public sealed class MinigameListsTests : IDisposable
{
    private const string Real = "real-game";
    private const string Mini = "mini-game";
    private const string Bundle = "a12-advance-concealment";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-minigame-lists-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public MinigameListsTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        var boards = new WideBoards(maps);
        live = new LivePlay(library, boards, dice.Roller);
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, library, games));
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

    private static string Card => Minigames.Find(Bundle)!.Card;

    /// <summary>A game set up on the wide board, and a copy of its record carrying the A12.14 minigame's id.</summary>
    private async Task Start()
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        var node = JsonSerializer.SerializeToNode(new
        {
            start = new
            {
                label = nameof(MinigameListsTests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = "german",
                scenarioMonth = 7,
                scenarioYear = 1942,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3, san = 6 }, new { id = "russian", nationality = "russian", elr = 3, san = 6 } },
            },
            placements = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = "de-1", ["kind"] = "asl:squad", ["definition"] = "attacker-squad", ["side"] = "german",
                    ["position"] = new Dictionary<string, string> { ["at"] = WideBoards.At(3) },
                },
            },
        })!.AsObject();
        node["gameId"] = Real;
        node["attemptId"] = "setup-0";
        node["expectedRevision"] = 0;
        var element = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(GameActions.Setup, element, live.Principal);
        var result = proposed.Outcome == PlayOutcome.NeedsConfirmation ? await live.Play.ConfirmAsync(GameActions.Setup, element, live.Principal, proposed.Correlation) : proposed;
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

        var record = live.Store.Read(new GameScope(LivePlay.Tenant, Real))!;
        var path = Path.Combine(live.Root, LivePlay.Tenant.ToString("N"), Mini + ".game.json");
        File.WriteAllText(path, GameEventWriter.Write(new GameScope(LivePlay.Tenant, Mini), new GameRecord("Minigame copy", false, record.Events) { Minigame = Bundle }));
    }

    private IRenderedComponent<T> Open<T>(string address) where T : IComponent
    {
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(address);
        return context.Render<T>();
    }

    private static string[] Options<T>(IRenderedComponent<T> page, string select) where T : IComponent =>
        [.. page.FindAll($"{select} option").Select(option => option.GetAttribute("value") ?? string.Empty).Where(value => value.Length > 0)];

    [Fact]
    public async Task TheLiveListsHoldOneKindOfGameEach()
    {
        await Start();
        Assert.Equal([Real], live.GameNames());
        Assert.Equal([Mini], live.MinigameGameNames());
        Assert.Equal([Mini], live.MinigameGameNames(Bundle));
        Assert.Empty(live.MinigameGameNames("a10-rally"));
        Assert.Equal([Mini], live.GamesLike(Mini));
        Assert.Equal([Real], live.GamesLike(Real));
        Assert.Equal([Real], live.GamesLike(string.Empty));
    }

    [Fact]
    public async Task TheMinigamesPageListsEachBundlesGamesAndStartsANewOneOnPlay()
    {
        await Start();
        var page = Open<MinigameListPage>("games/minigames");
        Assert.Equal(Minigames.All.Count, page.FindAll("section.minigame").Count);
        var section = page.Find($"#minigame-{Bundle}");
        Assert.Single(section.QuerySelectorAll($"li[data-game='{Mini}']"));
        Assert.Equal($"games/play?card={Uri.EscapeDataString(Card)}", section.QuerySelector("a.minigame-start")!.GetAttribute("href"));
        Assert.Empty(page.FindAll($"li[data-game='{Real}']"));
    }

    [Fact]
    public async Task PlayListsTheOtherGamesAndCardsUntilAMinigamesCardIsOpen()
    {
        await Start();
        var page = Open<PlayPage>("games/play");
        Assert.Equal([Real], Options(page, "#play-game"));
        Assert.DoesNotContain(Card, Options(page, "#new-card"));
        Assert.Empty(page.FindAll("#play-minigame"));

        page = Open<PlayPage>($"games/play?card={Uri.EscapeDataString(Card)}");
        Assert.Equal([Mini], Options(page, "#play-game"));
        Assert.Equal([Card], Options(page, "#new-card"));
        Assert.Single(page.FindAll("#play-minigame"));
    }

    [Fact]
    public async Task ReplayListsTheMinigamesGamesOnlyWhileOneIsOpen()
    {
        await Start();
        var page = Open<ReplayPage>("games/replay");
        Assert.Equal([Real], Options(page, "#replay-game"));

        page = Open<ReplayPage>($"games/replay?game={Mini}");
        Assert.Equal([Mini], Options(page, "#replay-game"));
        Assert.Single(page.FindAll("#replay-minigame"));
    }

    [Fact]
    public async Task TheWorkbenchListsTheMinigamesGamesUnlessALinkOpenedAnotherGame()
    {
        await Start();
        var page = Open<WorkbenchPage>("games/workbench");
        Assert.Equal([Mini], Options(page, "#workbench-game"));

        page = Open<WorkbenchPage>($"games/workbench?game={Real}&viewer=adjudicator");
        Assert.Equal([Real], Options(page, "#workbench-game"));
        Assert.Single(page.FindAll("#workbench-real-game"));
    }

    [Fact]
    public void ScenariosNeverShowsAMinigamesCardAndPointsToTheMinigamesPage()
    {
        var page = Open<ScenariosPage>("units/scenarios");
        Assert.DoesNotContain(Card, Options(page, "#card-choice"));
        Assert.Empty(page.FindAll("#card-minigame"));

        // The minigame cards design: the address names the minigame's card; the list and the card shown are the scenario cards', and a line points
        // to the card on the Minigames page.
        page = Open<ScenariosPage>($"units/scenarios?card={Uri.EscapeDataString(Card)}");
        Assert.DoesNotContain(Card, Options(page, "#card-choice"));
        Assert.NotEqual(Card, page.Find("#card-choice").GetAttribute("value"));
        Assert.Equal($"games/minigames?open={Bundle}", page.Find("#card-minigame a").GetAttribute("href"));
    }

    [Fact]
    public void TheMinigamesPageShowsAMinigamesCardOnlyWhenOpened()
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var page = Open<MinigameListPage>("games/minigames");
        Assert.Empty(page.FindAll(".minigame-card"));
        page.Find($"#minigame-{Bundle}-card-toggle").Click();
        Assert.Single(page.FindAll(".minigame-card"));
        Assert.Contains(Minigames.Find(Bundle)!.Title, page.Find($"#minigame-{Bundle}-card").TextContent, StringComparison.Ordinal);
        page.Find($"#minigame-{Bundle}-card-toggle").Click();
        Assert.Empty(page.FindAll(".minigame-card"));

        // Play's "The whole card" link names the bundle: its card opens with the page.
        page = Open<MinigameListPage>($"games/minigames?open={Bundle}");
        Assert.Single(page.FindAll($"#minigame-{Bundle}-card"));
    }

    [Fact]
    public void TheCardEditorTakesNoMinigameCardAsItsSource()
    {
        var page = Open<CardEditorPage>($"units/cards/edit?card={Uri.EscapeDataString(Card)}");
        Assert.Single(page.FindAll("#edit-minigame-refused"));
        Assert.Equal(string.Empty, page.Find("#edit-source").GetAttribute("value") ?? string.Empty);
        Assert.DoesNotContain(Card, Options(page, "#edit-source"));
    }

    [Fact]
    public void PlaysWholeCardLinkOpensAMinigamesCardOnTheMinigamesPage()
    {
        var catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, context.Services.GetRequiredService<UnitLibrary>().Vocabulary)!.Catalog!;
        var minigame = context.Render<PlayCardPanel>(parameters => parameters
            .Add(panel => panel.Scenario, new ScenarioCardReference(Card, "sha", "Minigame")).Add(panel => panel.Card, ScenarioCards.Read(Card, catalog)!.Card));
        var scenario = context.Render<PlayCardPanel>(parameters => parameters
            .Add(panel => panel.Scenario, new ScenarioCardReference("gambit", "sha", "Gambit")).Add(panel => panel.Card, ScenarioCards.Read("gambit", catalog)!.Card));
        Assert.Equal($"games/minigames?open={Bundle}", minigame.Find("#play-card-whole").GetAttribute("href"));
        Assert.Equal("units/scenarios?card=gambit", scenario.Find("#play-card-whole").GetAttribute("href"));
    }
}
