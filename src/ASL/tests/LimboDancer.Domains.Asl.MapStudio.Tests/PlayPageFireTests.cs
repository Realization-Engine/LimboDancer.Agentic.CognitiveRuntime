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
/// Fire on the Play page (Fire in Live Play, parts 6 and 7; U19, U20): the fire panel, the attack's facts before it
/// commits, each record as each side may see it, and the drawn markers, on the verified synthetic board with its stone
/// building. The LOS is the Studio's own read of that board, and the dice come from a fixed queue.
/// </summary>
public sealed class PlayPageFireTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-fire-" + Guid.NewGuid().ToString("N"));
    private readonly Queue<int> dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageFireTests()
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

    private static readonly GameScope Scope = new(LivePlay.Tenant, "village");

    private long Revision => live.Store.Read(Scope)?.Events.Count ?? 0;

    /// <summary>
    /// The ground-level building, an Open Ground hex beside it at the same level, and another Open Ground hex beside
    /// that one: the fire group's Location, the target, and a second target.
    /// </summary>
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

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, bool concealed = false)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-concealed").Change(concealed);
        page.Find("#place-add").Click();
    }

    /// <summary>
    /// A game set up on the page in the Russian PFPh: r1, r2, r3, and the 8-0 rl in the open hex; the German g1 and gh in
    /// the building; g2 in the second open hex. July, Russian ELR 2, German ELR 3 unless given.
    /// </summary>
    private IRenderedComponent<PlayPage> Game((string From, string Building, string Open) hexes, string germanElr = "3", bool concealed = false)
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "russian", ["second"] = "german", ["first-elr"] = "2", ["second-elr"] = germanElr, ["month"] = "7" });
        Place(page, "r1", "defender-squad", hexes.From);
        Place(page, "r2", "defender-squad", hexes.From);
        Place(page, "r3", "defender-squad", hexes.From);
        Place(page, "rl", "defender-leader", hexes.From);
        Place(page, "g1", "attacker-squad", hexes.Building, concealed);
        Place(page, "gh", "attacker-half-squad", hexes.Building, concealed);
        Place(page, "g2", "attacker-squad", hexes.Open);
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        Assert.Contains("Prep Fire", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        return page;
    }

    private static void ChooseFire(IRenderedComponent<PlayPage> page, string from, string[] firers, string? director, string target)
    {
        page.Find("#fire-from").Change(from);
        foreach (var id in firers)
        {
            page.Find($".fire-firer[data-unit='{id}']").Change(true);
        }

        if (director is not null)
        {
            page.Find("#fire-director").Change(director);
        }

        page.Find("#fire-target").Change(target);
        page.Find("#propose-fire").Click();
    }

    private static string Row(IRenderedComponent<PlayPage> page, string unit) => page.Find($"#play-units tr[data-unit='{unit}']").TextContent;

    /// <summary>The label the map gives a unit's counter, which names its states (such as Prep Fire, pinned, or broken).</summary>
    private static string Counter(IRenderedComponent<PlayPage> page, string unit) =>
        page.Find($"#play-map g[data-unit-id='{unit}']").GetAttribute("aria-label") ?? string.Empty;

    private void Roll(params int[] values)
    {
        foreach (var value in values)
        {
            dice.Enqueue(value);
        }
    }

    private static readonly string[] Perspectives = ["german", "russian", Perspective.AdjudicatorName];

    private static readonly string[] FireGroup = ["r1", "r2", "rl"];

    private static readonly string[] Firers = ["r1", "r2"];

    private static void Confirm(IRenderedComponent<PlayPage> page)
    {
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public async Task U19AFireAttackOnThePlayPageShowsItsFactsArithmeticAndEffectsInEachSidesView()
    {
        var hexes = Hexes();
        var page = Game(hexes);
        var before = Revision;

        // The facts are shown before anything is rolled: the Studio's own LOS read of the board gives range 1, clear.
        ChooseFire(page, hexes.From, ["r1", "r2"], "rl", hexes.Building);
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        string Fact(string name) => page.Find($"#fire-facts tr[data-fact='{name}'] td:last-child").TextContent;
        Assert.Equal(("1", "yes", "clear, Hindrance DRM 0", "stone-building", "7"), (Fact("range"), Fact("level"), Fact("los"), Fact("terrain"), Fact("month")));
        Assert.Equal($"r1, r2 in {hexes.From}, directed by rl", Fact("firers"));
        Assert.Equal(before, Revision);

        // IFT 3+4 = 7, +3 stone building, +0 leadership: Final DR 10 on the 16 column, a NMC. g1 rolls 3+4 = 7, its
        // Morale Level, and passes pinned (A7.8); gh rolls 4+4 = 8 against 7 and breaks.
        Roll(3, 4, 3, 4, 4, 4);
        Confirm(page);
        Assert.Empty(dice);
        foreach (var perspective in Perspectives)
        {
            page.ViewAs(perspective);
            var record = page.Find("#play-fires .fire-record");
            Assert.Equal(("10", "NMC"), (record.QuerySelector(".fire-final-dr")!.TextContent, record.QuerySelector(".fire-result")!.TextContent));
            Assert.Contains("r1: 4 FP × 2 (point-blank-fire, A7.21) = 8", record.QuerySelector(".fire-fp[data-unit='r1']")!.TextContent, StringComparison.Ordinal);
            Assert.Contains("Total 16 FP: the 16 column", record.QuerySelector(".fire-column")!.TextContent, StringComparison.Ordinal);
            Assert.Contains("+ 3 (tem:stone-building, A7.6)", record.QuerySelector(".fire-dr")!.TextContent, StringComparison.Ordinal);
            Assert.Equal("pinned", record.QuerySelector(".fire-effects tr[data-unit='g1'] .fire-effect")!.TextContent);
            Assert.Equal("broken", record.QuerySelector(".fire-effects tr[data-unit='gh'] .fire-effect")!.TextContent);
            Assert.Contains("MC 4, 4 = 8 against 7: failed", record.QuerySelector(".fire-effects tr[data-unit='gh']")!.TextContent, StringComparison.Ordinal);

            // The units table and the drawn counters show the markers and the effects.
            Assert.All(FireGroup, id =>
            {
                Assert.Contains("prep-fire", Row(page, id), StringComparison.Ordinal);
                Assert.Contains("Prep Fire", Counter(page, id), StringComparison.Ordinal);
            });
            Assert.DoesNotContain("prep-fire", Row(page, "r3"), StringComparison.Ordinal);
            Assert.Contains("pinned", Row(page, "g1"), StringComparison.Ordinal);
            Assert.Contains("pinned", Counter(page, "g1"), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("broken", Row(page, "gh"), StringComparison.Ordinal);
            Assert.Contains("broken", Counter(page, "gh"), StringComparison.OrdinalIgnoreCase);
        }

        // Replaying draws no dice and reproduces the state; confirming the same attempt again returns the recorded rolls.
        var committed = live.Store.Read(Scope)!.Events;
        var replayed = live.History("village")!;
        Assert.False(replayed.HasErrors);
        Assert.Equal(committed.Count, replayed.Current!.Revision);
        var ift = committed.Select(item => item.Payload).OfType<DiceRolled>().First(item => item.Purpose == "fire-ift");
        var arguments = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            gameId = "village",
            attemptId = ift.Roll[..^"-roll-1".Length],
            expectedRevision = before,
            firers = Firers,
            director = "rl",
            target = hexes.Building,
        });
        var proposed = await live.Play.ProposeAsync(GameActions.Fire, arguments, live.Principal);
        var again = await live.Play.ConfirmAsync(GameActions.Fire, arguments, live.Principal, proposed.Correlation);
        Assert.Equal(PlayOutcome.Replay, again.Outcome);
        Assert.Equal(committed.Count, Revision);

        // Prep Fire is removed at the end of the AFPh (A3.5), and pins at the end of the CCPh (A3.8).
        for (var phase = 0; phase < 3; phase++)
        {
            Commit(page, "#propose-advance");
        }

        Assert.Contains("prep-fire", Row(page, "r1"), StringComparison.Ordinal);
        Commit(page, "#propose-advance");
        Assert.DoesNotContain("prep-fire", Row(page, "r1"), StringComparison.Ordinal);
        Assert.DoesNotContain("Prep Fire", Counter(page, "r1"), StringComparison.Ordinal);

        // Backlog pass 13 (ruling R13.3): the broken gh, ADJACENT to the Russians, did not rout, so it surrenders as the RtPh ends (A20.21); its
        // captor's side takes it before the phase advances.
        Commit(page, "#propose-advance");
        Assert.Equal("rtph", live.History("village")!.Current!.Phase);
        Commit(page, ".take-prisoner");
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        Assert.Contains("pinned", Row(page, "g1"), StringComparison.Ordinal);
        Commit(page, "#propose-advance");
        Assert.DoesNotContain("pinned", Row(page, "g1"), StringComparison.Ordinal);
        Assert.DoesNotContain("pinned", Counter(page, "g1"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheFireChoicesDoNotSurviveAGameChangeAndOnlyGoodOrderLeadersDirect()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "russian", ["second"] = "german", ["first-elr"] = "2", ["second-elr"] = "3" });
        Place(page, "r1", "defender-squad", hexes.From);
        Place(page, "rl", "defender-leader", hexes.From);
        page.Find("#place-broken").Change(true);
        Place(page, "rb", "defender-leader", hexes.From);
        page.Find("#place-broken").Change(false);
        Place(page, "g1", "attacker-squad", hexes.Building);
        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");

        // A broken leader cannot direct fire (A7.53), so the Director list offers only the Good Order one.
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        string[] offered = [.. page.FindAll("#fire-director option").Select(option => option.GetAttribute("value") ?? string.Empty).Where(value => value.Length > 0)];
        Assert.Equal(["rl"], offered);
        page.Find("#fire-director").Change("rl");
        page.Find("#fire-target").Change(hexes.Building);

        // Another game, then this one again: nothing chosen carries over, so no attack names a unit of another game.
        page.Find("#play-game").Change(string.Empty);
        page.Find("#play-game").Change("village");
        Assert.DoesNotContain(page.FindAll(".fire-firer"), item => item.HasAttribute("checked"));
        Assert.Equal(string.Empty, page.Find("#fire-from").GetAttribute("value") ?? string.Empty);
        Assert.True(page.Find("#propose-fire").HasAttribute("disabled"));
    }

    [Fact]
    public void U20RefusalsOnThePlayPageComeBeforeAnyRollAndChangeNothing()
    {
        var hexes = Hexes();
        var page = Game(hexes);
        Roll(3, 4, 2, 3, 2, 3);
        ChooseFire(page, hexes.From, ["r1", "r2"], "rl", hexes.Building);
        Confirm(page);
        var revision = Revision;

        IReadOnlyList<string> Refused(string[] firers, string target)
        {
            ChooseFire(page, hexes.From, firers, null, target);
            page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
            Assert.Empty(page.FindAll("#play-confirm"));
            Assert.Equal(revision, Revision);
            return [.. page.FindAll("#play-reasons li").Select(item => item.TextContent)];
        }

        // A second attack from the same Location on the same target (A7.55); the dice queue is empty, so any roll would fail the
        // attempt. A unit already marked is not offered at all (A7.1; backlog, section 13).
        Assert.Contains(Refused(["r3"], hexes.Building), reason => reason.Contains("A7.55", StringComparison.Ordinal));
        page.Find("#fire-from").Change(hexes.From);
        Assert.Equal(["r3"], page.FindAll(".fire-firer").Select(item => item.GetAttribute("data-unit")));

        // In the MPh fire is the DEFENDER's (A8.1), offered from its side's Locations; with no stack moving, the planner
        // refuses it (FireTests).
        Commit(page, "#propose-advance");
        Assert.Contains("Movement", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.Contains("The german side may fire now", page.Find("#fire-elsewhere").TextContent, StringComparison.Ordinal);
        Assert.NotNull(page.Find("#propose-move"));

        // Ruling R23.1: the DEFENDER's fire panel is its own.
        page.ViewAs("german");
        Assert.Contains("The german side may fire", page.Find("#fire-side").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void U20AnAttackThatCouldReachAnUndecidedOutcomeIsRefusedOnThePage()
    {
        var hexes = Hexes();
        var page = Game(hexes, germanElr: string.Empty);
        var revision = Revision;
        ChooseFire(page, hexes.From, ["r1", "r2"], "rl", hexes.Building);
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains(page.FindAll("#play-reasons li"), item => item.TextContent.Contains("elr-undeclared", StringComparison.Ordinal));
        Assert.Equal(revision, Revision);
    }

    [Fact]
    public void AConcealedTargetLeftConcealedIsShownToTheFiringSideOnlyAsTheArithmetic()
    {
        var hexes = Hexes();
        var page = Game(hexes, concealed: true);

        // The Russians may target the building, where they know something is (A12.11), though not what.
        Assert.Contains(page.FindAll("#fire-target option"), option => option.GetAttribute("value") == hexes.Building);
        Roll(6, 6);
        ChooseFire(page, hexes.From, ["r1", "r2"], "rl", hexes.Building);
        Confirm(page);

        page.ViewAs("russian");
        var fires = page.Find("#play-fires");
        Assert.Equal("none", fires.QuerySelector(".fire-result")!.TextContent);
        Assert.NotNull(fires.QuerySelector(".fire-withheld"));
        Assert.Null(fires.QuerySelector(".fire-effects"));
        Assert.DoesNotContain("attacker-", fires.TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-units tr[data-unit='g1'], #play-units tr[data-unit='gh']"));
        Assert.Equal(2, page.FindAll("#play-units tr[data-sealed]").Count(row => row.TextContent.Contains(hexes.Building, StringComparison.Ordinal)));

        page.ViewAs("german");
        Assert.Equal("unaffected", page.Find("#play-fires .fire-effects tr[data-unit='g1'] .fire-effect").TextContent);
        Assert.Null(page.Find("#play-fires").QuerySelector(".fire-withheld"));
    }
}
