using Bunit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The backlog pass 5 on the Play page: Friendly Board Edges named at a new game (R5.16), Double Time (A4.5, R5.1), the owner's answer to a
/// pending choice offered only to the side that makes it (R5.8), and the captor's side rejecting a surrender or massacring a prisoner (A20.3,
/// A20.4, R5.6, R5.7).
/// </summary>
public sealed class PlayPagePass5Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-pass5-" + Guid.NewGuid().ToString("N"));
    private readonly Queue<int> dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass5Tests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
        live = new LivePlay(library, boards, new DiceRoller(_ => dice.TryDequeue(out var value) ? value - 1
            : throw new InvalidOperationException("The test drew a roll it did not expect.")));
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

    /// <summary>An Open Ground hex beside the ground-level building, the building, and another Open Ground hex beside the first.</summary>
    private (string From, string Building, string Open) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool OpenGround(HexName? hex, int level) => hex is { } name && handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground" } facts && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let building = handle.Geometry.NameOf(index)
                where handle.HexFacts(building)!.Locations.Any(location => location.Level == 0 && location.Terrain?.Name.Contains("Building", StringComparison.Ordinal) == true)
                let level = handle.HexFacts(building)!.BaseLevel
                from side in Enum.GetValues<HexsideDirection>()
                let open = handle.Neighbor(building, side)
                where OpenGround(open, level)
                from other in Enum.GetValues<HexsideDirection>()
                let second = handle.Neighbor(open!.Value, other)
                where second != building && OpenGround(second, level)
                select ($"{Board}:{open}:0", $"{Board}:{building}:0", $"{Board}:{second}:0")).First();
    }

    private void Roll(params int[] values)
    {
        foreach (var value in values)
        {
            dice.Enqueue(value);
        }
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, string holder = "", bool broken = false)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-holder").Change(holder);
        page.Find("#place-broken").Change(broken);
        page.Find("#place-add").Click();
    }

    private IRenderedComponent<PlayPage> NewGame(string firstSide, string secondSide, string firstEdge = "", string secondEdge = "")
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string>
        {
            ["board"] = Board,
            ["first"] = firstSide,
            ["second"] = secondSide,
            ["first-elr"] = firstSide == "russian" ? "2" : "3",
            ["second-elr"] = secondSide == "russian" ? "2" : "3",
            ["month"] = "7",
            ["first-edge"] = firstEdge,
            ["second-edge"] = secondEdge,
        });
        return page;
    }

    private static string Row(IRenderedComponent<PlayPage> page, string unit) => page.Find($"#play-units tr[data-unit='{unit}']").TextContent;

    private IRenderedComponent<PlayPage> Surrender(string id)
    {
        // The Russians' 4+6 at the ADJACENT g2 is a NMC; its Original 2 passes, and its Heat of Battle DR 6+6 = 12 is a Surrender (A15.5).
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r4", "defender-squad", hexes.From);
        Place(page, "r5", "defender-squad", hexes.From);
        Place(page, "r6", "defender-squad", hexes.From);
        Place(page, id, "attacker-squad", hexes.Open);
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r4']").Change(true);
        page.Find(".fire-firer[data-unit='r5']").Change(true);
        page.Find("#fire-target").Change(hexes.Open);
        Roll(4, 6, 1, 1, 6, 6);
        Commit(page, "#propose-fire");
        Assert.Empty(dice);
        return page;
    }

    [Fact]
    public void ANewGameNamesEachSidesFriendlyBoardEdgeAndAUnitMayDoubleTime()
    {
        var hexes = Hexes();
        // Ruling R22.4: the edges come from the game's (minimal) card.
        var page = NewGame("german", "russian", "left", "right");
        Place(page, "g1", "attacker-squad", hexes.Open);
        Place(page, "r1", "defender-squad", hexes.Building);
        Commit(page, "#propose-setup");
        var file = File.ReadAllText(Directory.GetFiles(live.Root, "village.game.json", SearchOption.AllDirectories).Single());
        Assert.Matches(@"""friendlyEdge"":\s*""left""", file);
        Assert.Matches(@"""friendlyEdge"":\s*""right""", file);

        // A4.5: g1 Double Times into the Open Ground beside it and is CX.
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        page.Find(".move-unit[data-unit='g1']").Change(true);
        page.Find("#move-to").Change(hexes.From);
        page.Find("#move-double-time").Change(true);
        Commit(page, "#propose-move");
        Assert.Contains("cx", Row(page, "g1"), StringComparison.Ordinal);
        Assert.False(page.Find("#move-double-time").HasAttribute("checked"));
    }

    [Fact]
    public void OnlyTheAnsweringSideIsOfferedAPendingChoice()
    {
        // 3+3 = 6 after the Original 2: a hero and Battle Hardening, which waits for the German side (A15.3, R5.8).
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r4", "defender-squad", hexes.From);
        Place(page, "r5", "defender-squad", hexes.From);
        Place(page, "g2", "attacker-squad", hexes.Open);
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r4']").Change(true);
        page.Find(".fire-firer[data-unit='r5']").Change(true);
        page.Find("#fire-target").Change(hexes.Open);
        Roll(4, 6, 1, 1, 3, 3);
        Commit(page, "#propose-fire");
        Assert.Empty(dice);
        Assert.Equal("battleHardening:g2", page.Find("#play-choice").GetAttribute("data-key"));
        Assert.Empty(page.FindAll("#play-choice .choose"));
        Assert.Contains("Waiting for the german side", page.Find("#play-choice").TextContent, StringComparison.Ordinal);

        page.ViewAs("german");
        Commit(page, "#play-choice .choose[data-option='decline']");
        Assert.Empty(page.FindAll("#play-choice"));
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Contains(page.FindAll("#play-units tr"), row => row.TextContent.Contains("hero", StringComparison.Ordinal));
        Assert.DoesNotContain("eliminated", Row(page, "g2"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheCaptorsSideMayRejectASurrender()
    {
        var page = Surrender("g2");
        Assert.NotEmpty(page.FindAll(".reject-surrender"));
        Commit(page, ".reject-surrender[data-unit='g2']");
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Contains("eliminated", Row(page, "g2"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".reject-surrender"));
    }

    [Fact]
    public void ARussianUnitMayMassacreItsPrisonerInItsFirePhase()
    {
        var page = Surrender("g2");
        Commit(page, ".take-prisoner[data-captor='r4']");
        Assert.Contains("captured", Row(page, "g2"), StringComparison.Ordinal);
        page.ViewAs("german");
        Assert.Empty(page.FindAll(".massacre"));
        page.ViewAs("russian");
        Commit(page, ".massacre[data-unit='r6'][data-prisoner='g2']");
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Contains("eliminated", Row(page, "g2"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".massacre"));
    }
}
