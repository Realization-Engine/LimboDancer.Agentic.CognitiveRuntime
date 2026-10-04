using Bunit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Unit step 29 on the Play page (U33): a squad advances in the APh panel into a Location holding a Known enemy squad, the
/// attacks are declared in the CCPh panel with the SMC stacking, the round's record shows its odds, DR, and DRM, and the units
/// left are held in Melee. The verified synthetic board and dice from a fixed queue.
/// </summary>
public sealed class PlayPageCloseCombatTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-cc-" + Guid.NewGuid().ToString("N"));
    private readonly Queue<int> dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageCloseCombatTests()
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

    private static readonly string[] Held = ["g1", "gl", "r1"];

    private static string Row(IRenderedComponent<PlayPage> page, string unit) => page.Find($"#play-units tr[data-unit='{unit}']").TextContent;

    [Fact]
    public void U33OnThePageASquadAdvancesAndItsCloseCombatLeavesTheSurvivorsInMelee()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "2" });
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "gl", "attacker-leader-8-1", hexes.One);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        for (var phase = 0; phase < 6; phase++)
        {
            page.EndPhase(Commit);
        }

        Assert.Contains("Advance", page.Find("#play-summary").TextContent, StringComparison.Ordinal);

        // The German squad and its leader advance into the Russian squad's Location (A4.7).
        page.Find(".advance-unit[data-unit='g1']").Change(true);
        page.Find(".advance-unit[data-unit='gl']").Change(true);
        page.Find("#advance-to").Change(hexes.Two);
        Commit(page, "#propose-advance-units");
        page.EndPhase(Commit);
        Assert.Contains("Close Combat", page.Find("#play-summary").TextContent, StringComparison.Ordinal);

        // The CCPh panel: the leader is stacked with g1 (A11.14); the Germans attack at 1-1 (5-4) with the 8-1's -1 and the Russians
        // at 1-2 (4-5); 6+6 and 6+5 have no effect.
        page.Find("#cc-location").Change(hexes.Two);
        page.Find(".cc-stack[data-unit='gl']").Change("g1");
        page.Find(".cc-attacker[data-unit='g1']").Change(true);
        page.Find(".cc-attacker[data-unit='gl']").Change(true);
        page.Find(".cc-defender[data-unit='r1']").Change(true);
        page.Find("#cc-director").Change("gl");
        page.Find("#cc-add-attack").Click();

        // Each side declares only its own attacks (ruling R5.20): the Russian player declares r1's.
        Assert.Empty(page.FindAll(".cc-attacker[data-unit='r1']"));
        page.ViewAs("russian");
        Assert.Empty(page.FindAll(".cc-stack[data-unit='gl']"));
        page.Find(".cc-attacker[data-unit='r1']").Change(true);
        page.Find(".cc-defender[data-unit='g1']").Change(true);
        page.Find(".cc-defender[data-unit='gl']").Change(true);
        page.Find("#cc-add-attack").Click();
        Assert.Equal(2, page.FindAll("#cc-attacks li").Count);
        foreach (var value in new[] { 6, 6, 6, 5 })
        {
            dice.Enqueue(value);
        }

        Commit(page, "#propose-cc");
        Assert.Empty(dice);
        var record = page.Find("#play-close-combats .cc-record").TextContent;
        Assert.Contains("at 1-1, Kill Number 5: DR 6 (colored), 6 = 12 - 1 (leadership:gl, A11.141); r1: 11 = Final DR 11: no effect", record, StringComparison.Ordinal);
        Assert.Contains("at 1-2, Kill Number 4", record, StringComparison.Ordinal);

        // A11.15: at the end of the CCPh the units are held in Melee.
        page.EndPhase(Commit);
        Assert.All(Held, id => Assert.Contains("melee", Row(page, id), StringComparison.Ordinal));
        Assert.False(live.History("village")!.HasErrors);
    }

    [Fact]
    public void ACaptureAttemptIsDeclaredOnThePageAndItsRecordNamesTheGuard()
    {
        // Backlog pass 14 (ruling R14.4): the attack is marked as a capture attempt; 4 FP against a HS's 2 is 2-1, Kill Number 7, and +1 for the
        // attempt: 2 and 3 capture the HS, which g1 guards (A20.22, A20.5).
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "2" });
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "r1", "defender-half-squad", hexes.Two);
        Commit(page, "#propose-setup");
        for (var phase = 0; phase < 6; phase++)
        {
            page.EndPhase(Commit);
        }

        page.Find(".advance-unit[data-unit='g1']").Change(true);
        page.Find("#advance-to").Change(hexes.Two);
        Commit(page, "#propose-advance-units");
        page.EndPhase(Commit);
        page.Find("#cc-location").Change(hexes.Two);
        page.Find(".cc-attacker[data-unit='g1']").Change(true);
        page.Find(".cc-defender[data-unit='r1']").Change(true);
        page.Find("#cc-capture").Change(true);
        page.Find("#cc-add-attack").Click();
        Assert.Contains("attempt to capture", page.Find("#cc-attacks").TextContent, StringComparison.Ordinal);
        dice.Enqueue(2);
        dice.Enqueue(3);
        Commit(page, "#propose-cc");
        var record = page.Find("#play-close-combats .cc-record").TextContent;
        Assert.Contains("capture, A20.22", record, StringComparison.Ordinal);
        Assert.Contains("r1 is captured and guarded by g1 (A20.22)", record, StringComparison.Ordinal);
        Assert.False(live.History("village")!.HasErrors);
    }

    [Fact]
    public void AUnitHeldInMeleeWithdrawsFromThePage()
    {
        // Table-player review, item 1 and the page findings: the advance destinations are a list of ADJACENT Locations, no Ambush is
        // offered in Open Ground, and a unit held in Melee chooses its withdrawal on the CC panel (A11.2).
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "2" });
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        for (var phase = 0; phase < 6; phase++)
        {
            page.EndPhase(Commit);
        }

        page.Find(".advance-unit[data-unit='g1']").Change(true);
        Assert.Contains(page.FindAll("#advance-to option"), option => option.GetAttribute("value") == hexes.Two);
        page.Find("#advance-to").Change(hexes.Two);
        Commit(page, "#propose-advance-units");
        page.EndPhase(Commit);

        page.Find("#cc-location").Change(hexes.Two);
        Assert.True(page.Find("#propose-ambush").HasAttribute("disabled"));
        Assert.True(page.FindAll(".cc-withdraw").Count == 0);
        page.Find(".cc-attacker[data-unit='g1']").Change(true);
        page.Find(".cc-defender[data-unit='r1']").Change(true);
        page.Find("#cc-add-attack").Click();
        dice.Enqueue(6);
        dice.Enqueue(6);
        Commit(page, "#propose-cc");

        // Through the Russian Player Turn to its CCPh: both are held in Melee, and each may withdraw.
        for (var phase = 0; phase < 8; phase++)
        {
            page.EndPhase(Commit);
        }

        Assert.Contains("Close Combat", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        page.Find("#cc-location").Change(hexes.Two);
        Assert.Equal(2, page.FindAll(".cc-withdraw").Count);
        page.Find(".cc-withdraw[data-unit='r1']").Change(hexes.One);
        page.Find(".cc-attacker[data-unit='g1']").Change(true);
        page.Find(".cc-defender[data-unit='r1']").Change(true);
        page.Find("#cc-add-attack").Click();
        dice.Enqueue(6);
        dice.Enqueue(6);
        Commit(page, "#propose-cc");
        Assert.Contains($"r1 withdraws to {hexes.One} (A11.2)", page.Find("#play-close-combats .cc-record").TextContent, StringComparison.Ordinal);
        Assert.Contains("vs-withdrawing", page.Find("#play-close-combats .cc-record").TextContent, StringComparison.Ordinal);
        Assert.False(live.History("village")!.HasErrors);
    }
}
