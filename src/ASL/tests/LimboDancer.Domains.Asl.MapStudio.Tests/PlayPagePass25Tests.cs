using Bunit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 25 on the Play page (plan task 25.6): leaving the map through the move panel (#move-exit, #propose-exit), which no page test covered, and the
/// movement toolbar after its extraction into components. A minimal card on the verified synthetic board.
/// </summary>
public sealed class PlayPagePass25Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-pass25-" + Guid.NewGuid().ToString("N"));
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass25Tests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
        live = new LivePlay(library, boards, new DiceRoller(_ => throw new InvalidOperationException("The test drew a roll it did not expect.")));
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
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

    /// <summary>An Open Ground hex at ground level on the top edge (no hex north of it), and another Open Ground hex of the small board for the other side.</summary>
    private (string Edge, string Inside) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool OpenGround(HexName name) => handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground", BaseLevel: 0 } facts
            && facts.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff);
        var names = handle.Geometry.Hexes().Select(index => handle.Geometry.NameOf(index)).Where(OpenGround).ToArray();
        var edge = names.First(name => handle.Neighbor(name, Maps.Geometry.HexsideDirection.North) is null);
        var inside = names.Last(name => name != edge);
        return ($"{Board}:{edge}:0", $"{Board}:{inside}:0");
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
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

    // A2.6 (rulings R21.5, R25.5): a German squad in a top-edge hex leaves the map from the move panel; the button waits for a mover and an edge, the
    // proposal says it leaves, and the squad is gone from the unit table and the movers.
    [Fact]
    public void AStackLeavesTheMapFromTheMovePanel()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string>
        {
            ["board"] = Board,
            ["first"] = "german",
            ["second"] = "russian",
            ["first-elr"] = "3",
            ["second-elr"] = "2",
            ["month"] = "7",
        });
        Place(page, "g1", "attacker-squad", hexes.Edge);
        Place(page, "r1", "defender-squad", hexes.Inside);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);

        Assert.True(page.Find("#propose-exit").HasAttribute("disabled"));
        page.Find(".move-unit[data-unit='g1']").Change(true);
        Assert.True(page.Find("#propose-exit").HasAttribute("disabled"));
        page.Find("#move-exit").Change("top");
        Assert.False(page.Find("#propose-exit").HasAttribute("disabled"));
        page.Find("#propose-exit").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("leave the map from", page.Markup, StringComparison.Ordinal);
        page.WaitForElement("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        // An Exited unit leaves the unit table and the movers.
        page.WaitForAssertion(() => Assert.Empty(page.FindAll("#play-units tr[data-unit='g1']")));
        Assert.NotEmpty(page.FindAll("#play-units tr[data-unit='r1']"));
        Assert.Empty(page.FindAll(".move-unit[data-unit='g1']"));
    }
}
