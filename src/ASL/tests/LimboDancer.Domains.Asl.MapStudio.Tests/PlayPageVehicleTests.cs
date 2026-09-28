using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Play page for unit step 25 (U29): a vehicle is placed with its VCA, the Infantry move list leaves vehicles out, the vehicle
/// movement panel spends one MP expenditure at a time, the crew exposure panel offers the AFV's BU counter, and the units table shows
/// a vehicle's VCA, MP, and CE state. The verified synthetic board and the Studio's scripted dice.
/// </summary>
public sealed class PlayPageVehicleTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-vehicles-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageVehicleTests()
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

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, string facing)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-facing").Change(facing);
        page.Find("#place-add").Click();
    }

    [Fact]
    public void AVehicleIsPlacedWithItsVcaAndMovesByItsOwnPanel()
    {
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        page.Find("#new-board").Change(Board);
        page.Find("#new-first").Change("german");
        page.Find("#new-second").Change("russian");
        page.Find("#new-first-elr").Change("3");
        page.Find("#new-second-elr").Change("3");

        // D2.11: the setup offers a VCA for a vehicle.
        page.Find("#place-definition").Change("attacker-truck");
        Assert.Contains("VCA", page.Find("#place-facing").ParentElement!.TextContent, StringComparison.Ordinal);
        Place(page, "de-t", "attacker-truck", one, "east");
        Place(page, "de-ht", "attacker-halftrack", two, "west");
        Assert.Contains("VCA west", page.Find("#place-list").TextContent, StringComparison.Ordinal);
        Commit(page, "#propose-setup");

        // The units table shows each vehicle's VCA, and the OT halftrack's crew as CE (D5.3).
        Assert.Contains("VCA east", page.Find("tr[data-unit='de-t']").TextContent, StringComparison.Ordinal);
        Assert.Contains("CE", page.Find("tr[data-unit='de-ht']").TextContent, StringComparison.Ordinal);

        // In the PFPh the halftrack's AAMG may fire; the unarmed truck may not (D5.1).
        Commit(page, "#propose-advance");
        var firing = page.FindAll("#fire-from option").Select(item => item.GetAttribute("value")).ToArray();
        Assert.Contains(two, firing);
        Assert.DoesNotContain(one, firing);
        Commit(page, "#propose-advance");
        Assert.Empty(page.FindAll(".move-unit"));
        Assert.Equal(["", "de-ht", "de-t"], page.FindAll("#vehicle-unit option").Select(item => item.GetAttribute("value")).Order(StringComparer.Ordinal));
        Assert.Single(page.FindAll(".bu-toggle[data-vehicle='de-ht']"));

        // D2.12: Start costs 1 MP; the DEFENDER's window opens.
        page.Find("#vehicle-unit").Change("de-t");
        Assert.Contains("0 of 28 MP spent, 28 left", page.Find("#vehicle-state").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#vehicle-stop"));
        Commit(page, "#vehicle-start");
        Assert.Empty(page.FindAll("#vehicle-start"));
        Assert.Single(page.FindAll("#vehicle-stop"));
        Assert.Equal("open", page.Find("#move-state").GetAttribute("data-window"));
        Assert.Contains("1 of 28 MP spent, 27 left, moving", page.Find("#vehicle-state").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll(".vehicle-enter"));
    }

    [Fact]
    public void TheMovingHalftrackOffersBoundingFirstFireOnceTheDefenderPasses()
    {
        // D3.3 (ruling R6.9): after the DEFENDER passes on its Start, the halftrack's panel offers Bounding First Fire at a Location the
        // Germans can see.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        page.Find("#new-board").Change(Board);
        page.Find("#new-first").Change("german");
        page.Find("#new-second").Change("russian");
        page.Find("#new-first-elr").Change("3");
        page.Find("#new-second-elr").Change("3");
        Place(page, "de-ht", "attacker-halftrack", one, "east");
        page.Find("#place-definition").Change("defender-squad");
        page.Find("#place-id").Change("r1");
        page.Find("#place-location").Change(two);
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        page.Find("#vehicle-unit").Change("de-ht");
        Commit(page, "#vehicle-start");
        Assert.Empty(page.FindAll("#vehicle-bff"));
        Commit(page, "#propose-pass");
        Assert.Contains(two, page.FindAll("#vehicle-bff-target option").Select(item => item.GetAttribute("value")));
    }

    [Fact]
    public void ATankIsOfferedInTheGunPanelWithTheEnemyVehicleAndItsAmmunition()
    {
        // D1.3, C3.31 (rulings R7.2, R7.10): in the PFPh the German tank is offered as a Gun, labeled with its VCA, TCA, and BU crew; a target
        // Location holding the T-34 offers it on the Vehicle Target Type with a choice of ammunition. The year setting is offered at setup.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        page.Find("#new-board").Change(Board);
        page.Find("#new-first").Change("german");
        page.Find("#new-second").Change("russian");
        page.Find("#new-first-elr").Change("3");
        page.Find("#new-second-elr").Change("3");
        page.Find("#new-year").Change("1942");
        Place(page, "de-tank", "attacker-tank", one, "east");
        Place(page, "ru-tank", "defender-tank", two, "west");
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        var gun = page.FindAll("#ordnance-gun option").Single(item => item.GetAttribute("value") == "de-tank");
        Assert.Contains("VCA east, TCA east, BU", gun.TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#ordnance-vehicle"));
        page.Find("#ordnance-gun").Change("de-tank");
        page.Find("#ordnance-target").Change(two);
        Assert.Contains("ru-tank", page.FindAll("#ordnance-vehicle option").Select(item => item.GetAttribute("value")));

        // C8.1: the PzKpfw IIIH lists APCR and no HEAT.
        Assert.Equal(["ap", "apcr", "he"], page.FindAll("#ordnance-ammunition option").Select(item => item.GetAttribute("value")));
    }

    [Fact]
    public void TheGunPanelListsTargetsWithTheirCoveredArcAndOffersTurningIntensiveFireAndPushing()
    {
        // R8.2, R8.6, R8.9: in the PFPh the leIG's panel reads each enemy Location against its CA, offers Intensive Fire and a turn without
        // firing, and draws its CA; in the MPh its crew may push it.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        page.Find("#new-board").Change(Board);
        page.Find("#new-first").Change("german");
        page.Find("#new-second").Change("russian");
        page.Find("#new-first-elr").Change("3");
        page.Find("#new-second-elr").Change("3");
        page.Find("#new-defender").Change("russian");
        page.Find("#place-definition").Change("attacker-crew");
        page.Find("#place-id").Change("de-crew");
        page.Find("#place-location").Change(one);
        page.Find("#place-add").Click();
        page.Find("#place-definition").Change("attacker-inf-gun");
        Assert.Single(page.FindAll("#place-bore"));
        page.Find("#place-id").Change("de-gun");
        page.Find("#place-location").Change(one);
        page.Find("#place-holder").Change("de-crew");
        page.Find("#place-facing").Change("east");
        page.Find("#place-add").Click();
        page.Find("#place-definition").Change("defender-squad");
        page.Find("#place-id").Change("r1");
        page.Find("#place-location").Change(two);
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        page.Find("#ordnance-gun").Change("de-gun");
        Assert.Contains("range 1", page.Find($"#ordnance-arc li[data-at='{two}']").TextContent, StringComparison.Ordinal);
        Assert.Single(page.FindAll("#ordnance-intensive"));
        Assert.Single(page.FindAll("#propose-turn-gun"));
        Assert.Contains("layer-covered-arc", page.Markup, StringComparison.Ordinal);
        Commit(page, "#propose-advance");
        page.Find(".move-unit[data-unit='de-crew']").Change(true);
        Assert.Single(page.FindAll("#move-push"));
    }
}
