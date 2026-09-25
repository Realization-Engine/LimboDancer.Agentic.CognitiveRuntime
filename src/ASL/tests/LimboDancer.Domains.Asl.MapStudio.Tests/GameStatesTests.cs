using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Pages;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Game states page and the game library (State Model Design, section 10).</summary>
public sealed class GameStatesTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-games-" + Guid.NewGuid().ToString("N"));
    private readonly UnitLibrary library;
    private readonly GameLibrary games;
    private readonly BunitContext context = new();

    public GameStatesTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var maps = new MapService(options, new FakeVaslMapSource());
        library = new UnitLibrary(options);
        games = new GameLibrary(library, new SyntheticBoards(maps));
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(games);
    }

    [Fact]
    public void TheFixtureReplaysAndSaysWhenBoardsCannotBeChecked()
    {
        var entry = games.Load("a1-village.synthetic");
        Assert.False(entry.History!.HasErrors);
        Assert.Equal(18, entry.History.States.Count);

        // The synthetic board provider has no bd01, so positions cannot be checked, and the entry says so.
        Assert.False(entry.PositionsChecked);
        Assert.Contains(entry.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-020");
    }

    [Fact]
    public void TheAdjudicatorSeesEverything()
    {
        var page = context.Render<Games>();
        Assert.Contains("Revision 18 of 18", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#game-units tr[data-unit='r2']"));
        Assert.Contains("prisoner of g1", page.Find("#game-units tr[data-unit='r1']").TextContent, StringComparison.Ordinal);
        Assert.Contains("from g2", page.Find("#game-units tr[data-unit='g2-hs']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ASideSeesOnlyWhatItMayKnow()
    {
        var page = context.Render<Games>();
        page.Find("#game-perspective").Change("german");
        page.Find("#game-revision").Change("8");
        Assert.Contains("Revision 8 of 18", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#game-units tr[data-unit='r1']"));
        Assert.Empty(page.FindAll("#game-units tr[data-unit='r2']"));
        Assert.Contains("withheld", page.Find("#game-units tr[data-unit='sealed-1']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("r1", page.Find("#game-events").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("r2", page.Markup, StringComparison.Ordinal);

        page.Find("#game-next").Click();
        page.Find("#game-next").Click();
        Assert.Contains("Revision 10 of 18", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#game-units tr[data-unit='r1']"));
    }

    [Fact]
    public void AViewIsShownOnTheBoardAsAGeneratedSet()
    {
        var entry = games.Load("a1-village.synthetic");
        var setId = games.ShowOnBoard(entry, Perspective.Side("german"), 8);
        var set = library.Set(setId)!;
        Assert.True(set.BuiltIn);
        Assert.True(set.Set.Synthetic);
        Assert.Equal(["f1", "g1", "g2", "gh1", "sealed-1"], set.Set.Units.Select(unit => unit.Id).Order(StringComparer.Ordinal));
        Assert.Contains(library.Sets(), item => item.Set.SetId == setId);
        Assert.Equal($"{setId} ships with the Studio; save under another id.", library.SaveSet(set.Set with
        {
            Units = []
        }));
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
