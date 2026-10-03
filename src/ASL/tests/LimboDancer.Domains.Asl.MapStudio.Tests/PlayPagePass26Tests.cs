using Bunit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 26 on the Play page (plan task 26.5; the UI and table player reviews): a Passenger placed aboard a vehicle from the setup form, each placement
/// removed by its own key, the Passenger kept out of the movers, unloaded through the vehicle panel, and the cargo notes read for the view. A minimal
/// card on the verified synthetic board.
/// </summary>
public sealed class PlayPagePass26Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-pass26-" + Guid.NewGuid().ToString("N"));
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass26Tests()
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

    /// <summary>Two Open Ground hexes at ground level with no hexside terrain.</summary>
    private (string One, string Two) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool OpenGround(HexName name) => handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground", BaseLevel: 0 } facts
            && facts.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff);
        var names = handle.Geometry.Hexes().Select(index => handle.Geometry.NameOf(index)).Where(OpenGround).ToArray();
        return ($"{Board}:{names[0]}:0", $"{Board}:{names[^1]}:0");
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, string holder = "")
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-holder").Change(holder);
        page.Find("#place-add").Click();
    }

    [Fact]
    public void APassengerIsPlacedAboardAndUnloadsThroughTheVehiclePanel()
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

        // A holder not yet placed is said, not dropped (table player, pass 26).
        Place(page, "gs", "attacker-squad", hexes.One, "ght");
        Assert.Contains("ght is not placed yet", page.Find("#place-note").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#place-list"));

        Place(page, "ght", "attacker-halftrack", hexes.One);
        Place(page, "gs", "attacker-squad", hexes.One, "ght");
        Place(page, "r1", "defender-squad", hexes.Two);
        Place(page, "r1", "defender-squad", hexes.Two);
        // Pass 30: a Passenger's vehicle is changed in its row.
        Assert.Contains("gs: attacker-squad", page.Find("#place-list li:nth-child(2)").TextContent, StringComparison.Ordinal);
        Assert.Equal("ght", page.Find("#place-row-1-holder").GetAttribute("value"));

        // Two placements with the same id are removed one at a time (UI review, pass 26).
        Assert.Equal(2, page.FindAll("#place-list li").Count(item => item.TextContent.StartsWith("r1:", StringComparison.Ordinal)));
        page.FindAll("#place-list li").Last(item => item.TextContent.StartsWith("r1:", StringComparison.Ordinal)).QuerySelector("button")!.Click();
        Assert.Single(page.FindAll("#place-list li"), item => item.TextContent.StartsWith("r1:", StringComparison.Ordinal));

        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        page.ViewAs("german");

        // Ruling R26.2: a Passenger is not offered as a mover; its vehicle's row names it, and only for its own side's view.
        Assert.Empty(page.FindAll(".move-unit[data-unit='gs']"));
        Assert.Contains("Passengers gs", page.Find("#play-units tr[data-unit='ght']").TextContent, StringComparison.Ordinal);
        Assert.Contains("aboard ght", page.Find("#play-units tr[data-unit='gs']").TextContent, StringComparison.Ordinal);
        page.ViewAs("russian");
        Assert.All(page.FindAll("#play-units tr[data-unit='ght']"), row => Assert.DoesNotContain("Passengers", row.TextContent, StringComparison.Ordinal));
        page.ViewAs("german");

        page.Find("#vehicle-unit").Change("ght");
        Assert.Contains("Passengers gs", page.Find("#vehicle-state").TextContent, StringComparison.Ordinal);
        Assert.True(page.Find("#vehicle-unload").HasAttribute("disabled"));
        page.Find(".vehicle-unload[value='gs']").Change(true);
        Commit(page, "#vehicle-unload");
        page.WaitForAssertion(() => Assert.DoesNotContain("aboard ght", page.Find("#play-units tr[data-unit='gs']").TextContent, StringComparison.Ordinal));

    }
}
