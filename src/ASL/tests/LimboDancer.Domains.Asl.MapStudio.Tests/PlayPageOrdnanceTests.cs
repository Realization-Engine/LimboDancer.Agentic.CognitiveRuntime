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
/// Unit step 24 on the Play page (U28): a Gun and its crew are placed with the Gun's facing, and in the PFPh the Ordnance panel fires
/// the Gun at an ADJACENT Russian squad; the record shows the To Hit arithmetic with the colored die. The verified synthetic board and
/// dice from a fixed queue.
/// </summary>
public sealed class PlayPageOrdnanceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-ordnance-" + Guid.NewGuid().ToString("N"));
    private readonly Queue<int> dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageOrdnanceTests()
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

    /// <summary>Two ADJACENT Open Ground hexes at one level.</summary>
    private (string One, string Two) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool OpenGround(HexName? hex, int level) => hex is { } name && handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground" } facts && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let one = handle.Geometry.NameOf(index)
                let level = handle.HexFacts(one)!.BaseLevel
                where OpenGround(one, level)
                from side in Enum.GetValues<HexsideDirection>()
                let two = handle.Neighbor(one, side)
                where OpenGround(two, level) && handle.HexFacts(one)!.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff)
                select ($"{Board}:{one}:0", $"{Board}:{two}:0")).First();
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

    [Fact]
    public void U28OnThePageAGunManByItsCrewFiresAndItsRecordShowsTheToHitArithmetic()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "2" });
        Place(page, "de-crew", "attacker-crew", hexes.One);
        page.Find("#place-definition").Change("attacker-inf-gun");
        page.Find("#place-facing").Change("east");
        page.Find("#place-holder").Change("de-crew");
        Place(page, "de-gun", "attacker-inf-gun", hexes.One);
        Assert.Equal(string.Empty, page.Find("#place-holder").GetAttribute("value"));
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);

        // C3.3: black Basic TH# 8 at one hex with Case L -2 (and Case A if the Gun turns): 6 and 5 miss whatever the turn.
        Assert.Contains(page.FindAll("#ordnance-gun option"), option => option.GetAttribute("value") == "de-gun");
        page.Find("#ordnance-gun").Change("de-gun");
        page.Find("#ordnance-target").Change(hexes.Two);
        dice.Enqueue(6);
        dice.Enqueue(5);
        Commit(page, "#propose-ordnance");
        Assert.Empty(dice);
        var record = page.Find("#play-ordnance .ordnance-record").TextContent;
        Assert.StartsWith($"Gun G1 in {DisplayText.Place(1, hexes.One)} fires HE at {DisplayText.Place(1, hexes.Two)}: ", record, StringComparison.Ordinal);
        Assert.DoesNotContain("de-gun", record, StringComparison.Ordinal);
        Assert.Contains("Basic TH# 8 (black) = 8; DR 6 (colored), 5 = 11", record, StringComparison.Ordinal);
        Assert.Contains("miss", record, StringComparison.Ordinal);
        Assert.False(live.History("village")!.HasErrors);
    }

    [Fact]
    public void AGunWhoseHeHasADepletionNumberNamesItAtInfantryAndShowsTheNumber()
    {
        // Pass 55 (C2.21, C8.8, C8.9): the British 6-Pounder's HE is Special Ammunition, HE8 in June 1944. At Infantry the panel offers it with
        // its number and the shot names it; an Original 9 finds none, asks for no IFT DR, and the panel then says it has run out.
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string>
        {
            ["board"] = Board, ["first"] = "british", ["second"] = "german", ["first-elr"] = "3", ["second-elr"] = "3", ["year"] = "1944", ["month"] = "6",
        });
        Place(page, "uk-crew", "british-crew", hexes.One);
        page.Find("#place-definition").Change("british-at-gun");
        page.Find("#place-facing").Change("east");
        page.Find("#place-holder").Change("uk-crew");
        Place(page, "uk-gun", "british-at-gun", hexes.One);
        Place(page, "g1", "attacker-squad", hexes.Two);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);

        page.Find("#ordnance-gun").Change("uk-gun");
        page.Find("#ordnance-target").Change(hexes.Two);
        Assert.Empty(page.FindAll("#ordnance-vehicle"));
        var option = Assert.Single(page.FindAll("#ordnance-ammunition option"));
        Assert.Equal(("he", "HE (Depletion Number 8)"), (option.GetAttribute("value"), option.TextContent));
        Assert.Empty(page.FindAll("#ordnance-ammunition-note"));
        dice.Enqueue(5);
        dice.Enqueue(4);
        Commit(page, "#propose-ordnance");
        Assert.Empty(dice);

        // What the page sent, read from the committed game.
        var history = live.History("village")!;
        Assert.False(history.HasErrors);
        var fired = history.Events.Select(item => item.Payload).OfType<OrdnanceFired>().Single();
        Assert.Equal("he", fired.Facts.GetProperty("ammunition").GetString());
        Assert.Equal(1944, fired.Facts.GetProperty("scenarioYear").GetInt32());
        Assert.Equal("none", fired.Resolution.GetProperty("ammunitionUse").GetString());
        Assert.False(fired.Resolution.TryGetProperty("hit", out _));
        Assert.Contains("the Original DR is above the Depletion Number 8: it had no HE, has not fired", page.Find("#play-ordnance .ordnance-record").TextContent, StringComparison.Ordinal);

        // The Gun has not fired and is still offered, with no HE left to name.
        page.Find("#ordnance-gun").Change("uk-gun");
        page.Find("#ordnance-target").Change(hexes.Two);
        Assert.Empty(page.FindAll("#ordnance-ammunition"));
        Assert.Equal("HE: it has run out (C8.9)", page.Find("#ordnance-ammunition-note").TextContent);

        // The shot still names HE, so the refusal is that the Gun has none, not that it was left unnamed.
        page.Find("#propose-ordnance").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.True(page.Find("#play-proposal").TextContent.Contains("it has none now", StringComparison.Ordinal), page.Find("#play-proposal").TextContent);
        Assert.DoesNotContain("name HE for the shot", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
    }
}
