using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31 (the play-test UI, part I): a side's view lists its own units and ends only the phases its side ends (ruling R31.6); the header gives the
/// turn of the card's turns; the hand-over screen says when, why, and what the arriving side will do (ruling R31.7); and a phase end that eliminates
/// a unit is refused to the other side and shown to its own side as a consequence, with a Confirm that says so (play test P-06, P-07, P-08).
/// </summary>
public sealed class PlayPagePass31Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-31-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass31Tests()
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

    /// <summary>Two ADJACENT Open Ground hexes at one level, with no hexside terrain.</summary>
    private (string One, string Two) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool Terrain(HexName? hex, string name, int level) => hex is { } at && handle.HexFacts(at) is { Center.Terrain.Name: { } terrain } facts && terrain == name && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let one = handle.Geometry.NameOf(index)
                let level = handle.HexFacts(one)!.BaseLevel
                where Terrain(one, "Open Ground", level)
                from side in Enum.GetValues<HexsideDirection>()
                let two = handle.Neighbor(one, side)
                where Terrain(two, "Open Ground", level) && handle.HexFacts(one)!.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff)
                select ($"{Board}:{one}:0", $"{Board}:{two}:0")).First();
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, string? holder = null)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        if (holder is not null)
        {
            page.Find("#place-holder").Change(holder);
        }

        page.Find("#place-add").Click();
    }

    private IRenderedComponent<PlayPage> NewGame()
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        return page;
    }

    private GameState Current => live.History("village")!.Current!;

    // Ruling R31.6 (play test P-07): the pickers list the viewing side's units, and the phase end is offered to the side that ends the phase.
    [Fact]
    public void AViewListsItsOwnUnitsAndEndsItsOwnPhases()
    {
        var hexes = Hexes();
        var page = NewGame();
        page.Find("#place-broken").Change(true);
        Place(page, "g1", "attacker-squad", hexes.One);
        page.Find("#place-broken").Change(true);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        Assert.Equal("rph", Current.Phase);

        string[] Rallying() => [.. page.FindAll("#rally-unit option").Select(option => option.GetAttribute("value")!).Where(value => value.Length > 0)];
        Assert.Equal(["g1"], Rallying());
        page.ViewAs("russian");
        Assert.Equal(["r1"], Rallying());

        // Either side ends the RPh, where both act; the ATTACKER ends its PFPh, and the other view reads who does.
        page.EndPhase(Commit);
        Assert.Equal("pfph", Current.Phase);
        Assert.Empty(page.FindAll("#propose-advance"));
        Assert.Contains("The German side ends the Prep Fire Phase", page.Find("#advance-other").TextContent, StringComparison.Ordinal);
        page.ViewAs("german");
        Assert.NotEmpty(page.FindAll("#propose-advance"));
        Assert.Empty(page.FindAll("#advance-other"));
    }

    // Play test P-08 and ruling R31.7: the header gives the turn of the card's turns, and the hand-over screen says when, why, and what comes next.
    [Fact]
    public void TheHeaderCountsTheTurnsAndTheHandOverScreenSaysWhatIsGoingOn()
    {
        var hexes = Hexes();
        var page = NewGame();
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        Assert.Matches("^Turn 1 of [0-9]+", page.Find("#play-summary").TextContent.Trim());

        // The first phase end closes setup; the German side then acts in its Prep Fire Phase, and the game does not wait on the Russian side.
        page.EndPhase(Commit);
        Assert.Equal("pfph", Current.Phase);
        page.Find("#play-perspective").Change("russian");
        var screen = page.Find("#play-handover");
        Assert.Contains("Hand the screen to", screen.QuerySelector("#play-handover-title")!.TextContent, StringComparison.Ordinal);
        Assert.Matches("^Turn 1 of [0-9]+.*German Prep Fire Phase", screen.QuerySelector("#play-handover-when")!.TextContent.Trim());
        Assert.Contains("The game does not wait on this side now", screen.QuerySelector("#play-handover-why")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("the game waits on the other side", screen.QuerySelector("#play-handover-tasks")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("German Prep Fire Phase, Turn 1", screen.QuerySelector("#play-handover-happened")!.TextContent, StringComparison.Ordinal);

        // The screen names no unit of either side.
        Assert.DoesNotContain("g1", screen.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("r1", screen.TextContent, StringComparison.Ordinal);
        page.Find("#play-handover-confirm").Click();
        Assert.Empty(page.FindAll("#play-handover"));
    }

    // Play test P-06 and the table player's review: a side may not end the RtPh while the other side must still rout; that side's own end of the phase
    // says what it loses before Confirm, and Confirm says so.
    [Fact]
    public void TheRoutPhasesEndIsRefusedToTheOtherSideAndWarnsItsOwn()
    {
        var hexes = Hexes();
        var page = NewGame();
        page.Find("#place-definition").Change("attacker-tank");
        page.Find("#place-facing").Change("east");
        Place(page, "gt", "attacker-tank", hexes.One);
        page.Find("#place-broken").Change(true);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        for (var phase = 0; phase < 5; phase++)
        {
            page.EndPhase(Commit);
        }

        Assert.Equal("rtph", Current.Phase);
        page.ViewAs("german");
        page.Find("#propose-advance").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("still has a unit that must rout", page.Find("#play-reasons").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-consequences"));

        page.ViewAs("russian");
        page.Find("#propose-advance").Click();
        page.WaitForAssertion(() => Assert.Contains("with consequences", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("r1 is eliminated for Failure to Rout", page.Find("#play-consequences").TextContent, StringComparison.Ordinal);
        Assert.Equal("Confirm: 1 unit is lost", page.Find("#play-confirm").TextContent.Trim());
        Assert.DoesNotContain("Failure to Rout", page.Find("#play-reasons").TextContent, StringComparison.Ordinal);
        Assert.Equal(InstanceStatus.Active, Current.Unit("r1")!.Status);
    }
}
