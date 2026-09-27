using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Play page fixes from the Studio demo of passes 2 and 3 (backlog, section 13): each panel offers only what the gate would
/// accept, choices do not outlive their phase, a berserk charge is marked, custody is shown, and scripted dice are labeled. The
/// verified synthetic board and the Studio's scripted dice.
/// </summary>
public sealed class PlayPageDemoFixesTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-fixes-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageDemoFixesTests()
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

    private static void Advance(IRenderedComponent<PlayPage> page, int phases)
    {
        for (var phase = 0; phase < phases; phase++)
        {
            Commit(page, "#propose-advance");
        }
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

    private IRenderedComponent<PlayPage> Start(string first, string second, params (string Id, string Definition, string At)[] units)
    {
        var page = context.Render<PlayPage>();
        page.Find("#new-board").Change(Board);
        page.Find("#new-first").Change(first);
        page.Find("#new-second").Change(second);
        page.Find("#new-first-elr").Change("3");
        page.Find("#new-second-elr").Change("3");
        foreach (var (id, definition, at) in units)
        {
            Place(page, id, definition, at);
        }

        Commit(page, "#propose-setup");
        return page;
    }

    private static string[] Checked(IRenderedComponent<PlayPage> page, string selector) =>
        [.. page.FindAll(selector).Select(item => item.GetAttribute("data-unit")!)];

    [Fact]
    public void TheFireMovementAndAdvancePanelsLeaveOffACrewThatMansAGun()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        page.Find("#new-board").Change(Board);
        page.Find("#new-first").Change("german");
        page.Find("#new-second").Change("russian");
        Place(page, "de-crew", "attacker-crew", hexes.One);
        page.Find("#place-definition").Change("attacker-inf-gun");
        page.Find("#place-facing").Change("east");
        Place(page, "de-gun", "attacker-inf-gun", hexes.One, holder: "de-crew");
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");

        // Nor does the Fire panel offer the crew: its inherent fire is not reviewed (ruling R24.8).
        Advance(page, 1);
        page.Find("#fire-from").Change(hexes.One);
        Assert.Equal(["g1"], Checked(page, ".fire-firer"));

        // Nor the Movement panel (ruling R24.4).
        Advance(page, 1);
        Assert.Equal(["g1"], Checked(page, ".move-unit"));
        Advance(page, 4);

        Assert.Equal(["g1"], Checked(page, ".advance-unit"));
    }

    [Fact]
    public void TheFirePanelLeavesOffUnitsThatFiredAndDropsItsChoicesAtTheNextPhase()
    {
        var hexes = Hexes();
        var page = Start("german", "russian", ("g1", "attacker-squad", hexes.One), ("g2", "attacker-squad", hexes.One), ("r1", "defender-squad", hexes.Two));
        Advance(page, 1);
        page.Find("#fire-from").Change(hexes.One);
        Assert.Equal(["g1", "g2"], Checked(page, ".fire-firer"));
        page.Find(".fire-firer[data-unit='g1']").Change(true);
        page.Find("#fire-target").Change(hexes.Two);

        // 6, 5 on the 8 column: no effect.
        Assert.True(dice.Enqueue([6, 5]));
        Commit(page, "#propose-fire");
        page.Find("#fire-from").Change(hexes.One);
        Assert.Equal(["g2"], Checked(page, ".fire-firer"));

        // A choice made in the PFPh does not survive into the MPh, where the other side fires (backlog, section 13).
        page.Find(".fire-firer[data-unit='g2']").Change(true);
        page.Find("#fire-target").Change(hexes.Two);
        Advance(page, 1);
        Assert.Equal(string.Empty, page.Find("#fire-from").GetAttribute("value"));
        Assert.Equal(string.Empty, page.Find("#fire-target").GetAttribute("value"));
        Assert.Empty(page.FindAll(".fire-firer"));
    }

    [Fact]
    public void ABerserkUnitIsNotOfferedAsAFirerAndIsMarkedAndPreselectedToCharge()
    {
        var hexes = Hexes();
        var page = Start("russian", "german", ("r4", "defender-squad", hexes.One), ("r5", "defender-squad", hexes.One),
            ("g2", "attacker-squad", hexes.Two));
        Advance(page, 1);

        // 16 FP, 4+6 = 10: a NMC; g2's Original 2 passes, and its Heat of Battle DR 5+5 = 10 makes it berserk (A15.4).
        page.Find("#fire-from").Change(hexes.One);
        page.Find(".fire-firer[data-unit='r4']").Change(true);
        page.Find(".fire-firer[data-unit='r5']").Change(true);
        page.Find("#fire-target").Change(hexes.Two);
        Assert.True(dice.Enqueue([4, 6, 1, 1, 5, 5]));
        Commit(page, "#propose-fire");
        Assert.Contains("berserk", page.Find("#play-units tr[data-unit='g2']").TextContent, StringComparison.Ordinal);
        Assert.NotNull(page.Find("#rolls-scripted"));

        // In the DFPh the berserk g2, the Germans' only unit, is not offered as a firer, so its Location is not offered either (A15.432).
        Advance(page, 2);
        Assert.Equal("dfph", live.History("village")!.Current!.Phase);
        Assert.DoesNotContain(page.FindAll("#fire-from option"), option => option.GetAttribute("value") == hexes.Two);

        // The German MPh: g2 charges first, marked with its target and next step, and it is the unit checked.
        Advance(page, 7);
        Assert.Equal("mph", live.History("village")!.Current!.Phase);
        var charge = page.Find("[data-charge='g2']").TextContent;
        Assert.Contains($"g2 is berserk and charges {hexes.One}", charge, StringComparison.Ordinal);
        Assert.Equal(["g2"], page.FindAll(".move-unit").Where(item => item.HasAttribute("checked")).Select(item => item.GetAttribute("data-unit")));
        Assert.Equal(hexes.One, page.Find("#move-to").GetAttribute("value"));
    }

    [Fact]
    public void APrisonerShowsItsGuardAndTheGuardItsPrisoner()
    {
        var hexes = Hexes();
        var page = Start("russian", "german", ("r4", "defender-squad", hexes.One), ("r5", "defender-squad", hexes.One),
            ("g2", "attacker-squad", hexes.Two));
        Advance(page, 1);

        // A NMC; g2's Original 2 passes, and its Heat of Battle DR 6+6 = 12 is a Surrender to the ADJACENT Russians (A15.5).
        page.Find("#fire-from").Change(hexes.One);
        page.Find(".fire-firer[data-unit='r4']").Change(true);
        page.Find(".fire-firer[data-unit='r5']").Change(true);
        page.Find("#fire-target").Change(hexes.Two);
        Assert.True(dice.Enqueue([4, 6, 1, 1, 6, 6]));
        Commit(page, "#propose-fire");
        Commit(page, ".take-prisoner[data-captor='r4']");

        Assert.Contains("guarded by r4", page.Find("#play-units tr[data-unit='g2']").TextContent, StringComparison.Ordinal);
        Assert.Contains("guards g2", page.Find("#play-units tr[data-unit='r4']").TextContent, StringComparison.Ordinal);
    }
}
