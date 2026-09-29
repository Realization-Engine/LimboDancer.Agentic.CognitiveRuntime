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

        page.Find("#new-card").Change(string.Empty);
        Assert.False(page.Find("#new-board").HasAttribute("disabled"));
        Assert.Empty(page.FindAll("#place-group"));
    }

    // A3.9: a card that leaves the first move to a die roll keeps the first side open, and the other side follows it.
    [Fact]
    public void TheTractorWorksLeavesTheFirstMoveOpen()
    {
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("tractor-works");
        Assert.False(page.Find("#new-first").HasAttribute("disabled"));
        // Referee and table player, pass 18: only the card's sides may move first, and the winner is not preset.
        Assert.Equal(3, page.FindAll("#new-first option").Count);
        Assert.Equal(string.Empty, page.Find("#new-first").GetAttribute("value"));
        Assert.True(page.Find("#new-second").HasAttribute("disabled"));
        page.Find("#new-first").Change("german");
        Assert.Equal("russian", page.Find("#new-second").GetAttribute("value"));
        Assert.Equal("4", page.Find("#new-first-elr").GetAttribute("value"));
        Assert.Equal("bottom", page.Find("#new-first-edge").GetAttribute("value"));
    }
}
