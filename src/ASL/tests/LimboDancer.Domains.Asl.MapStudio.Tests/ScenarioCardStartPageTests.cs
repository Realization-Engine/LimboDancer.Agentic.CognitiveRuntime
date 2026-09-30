using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Play page's scenario card picker (backlog pass 18, rulings R18.1 to R18.3): a card fills and locks the start fields.</summary>
public sealed class ScenarioCardStartPageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-card-start-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();

    public ScenarioCardStartPageTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        var boards = new BuildingBoards(maps);
        var dice = new ScriptedDice();
        var live = new LivePlay(library, boards, dice.Roller);
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
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

    [Fact]
    public void ACardFillsAndLocksTheStartFields()
    {
        var page = context.Render<PlayPage>();
        Assert.Equal(ScenarioCards.Names.Count + 1, page.FindAll("#new-card option").Count);
        Assert.False(page.Find("#new-board").HasAttribute("disabled"));

        page.Find("#new-card").Change("guards-counterattack");
        Assert.Equal("bd01", page.Find("#new-board").GetAttribute("value"));
        Assert.True(page.Find("#new-board").HasAttribute("disabled"));
        Assert.True(page.Find("#new-first").HasAttribute("disabled"));
        Assert.Equal("russian", page.Find("#new-first").GetAttribute("value"));
        Assert.Equal("6", page.Find("#new-first-san").GetAttribute("value"));
        Assert.Equal("3", page.Find("#new-first-elr").GetAttribute("value"));
        Assert.Equal("top", page.Find("#new-first-edge").GetAttribute("value"));
        Assert.Equal("10", page.Find("#new-month").GetAttribute("value"));
        Assert.Equal("The Guards Counterattack", page.Find("#new-label").GetAttribute("value"));

        // R18.3: the placement offers the card's OB groups.
        var groups = page.FindAll("#place-group option").Select(option => option.TextContent).ToArray();
        Assert.Contains("russian: Elements of 2nd Battalion, 37th Guards Division, ELR 3", groups);
        Assert.Contains("german: Company H, 389th Infantry Regiment, ELR 4", groups);

        // Pass 19 (ruling R19.2): the setup shows the card's OB group by group, the Germans first.
        Assert.Contains("setting up now", page.Find("#setup-pools tr[data-group='german-1']").TextContent, StringComparison.Ordinal);
        Assert.Contains("waits", page.Find("#setup-pools tr[data-group='russian-2']").TextContent, StringComparison.Ordinal);
        Assert.Contains("12 6-2-8 elite squad in F3", page.Find("#setup-pools tr[data-group='russian-2']").TextContent, StringComparison.Ordinal);

        page.Find("#new-card").Change(string.Empty);
        Assert.Empty(page.FindAll("#setup-pools"));
        Assert.False(page.Find("#new-board").HasAttribute("disabled"));
        Assert.Empty(page.FindAll("#place-group"));
    }

    // A3.9: a card that leaves the first move to a die roll keeps the first side open, and the other side follows it.
    [Fact]
    public void TheTractorWorksRollsForTheFirstMoveAndOffersTheBalance()
    {
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("tractor-works");

        // Pass 20 (ruling R20.2): the game rolls for the first move as it starts; the fields list the side that sets up first, and name no winner.
        Assert.True(page.Find("#new-first").HasAttribute("disabled"));
        Assert.Contains("rolls a dr for each side", page.Find("#new-first-roll").TextContent, StringComparison.Ordinal);
        Assert.Equal("russian", page.Find("#new-first").GetAttribute("value"));
        Assert.Equal("german", page.Find("#new-second").GetAttribute("value"));
        Assert.Equal("bottom", page.Find("#new-second-edge").GetAttribute("value"));

        // Ruling R20.3 (A26.4): no Balance, a side's by agreement, or a dr when both players wish the same side, who then name themselves.
        Assert.Equal(5, page.FindAll("#new-balance option").Count);
        Assert.Empty(page.FindAll("#new-player-one"));
        page.Find("#new-balance").Change("wish:german");
        Assert.NotNull(page.Find("#new-player-one"));
        Assert.Empty(page.FindAll("#propose-needs-winner"));
    }
}
