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
/// Unit steps 19 to 23 on the Play page: a Dummy and a MG placed at setup, a rally in the RPh panel, a MG joining a fire
/// group and a Dummy removed by it, and a stack moving in the MPh panel under Defensive First Fire that leaves Residual FP
/// drawn on the map. The verified synthetic board, the Studio's own LOS read, and dice from a fixed queue.
/// </summary>
public sealed class PlayPageStepsTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-steps-" + Guid.NewGuid().ToString("N"));
    private readonly Queue<int> dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageStepsTests()
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

    private IRenderedComponent<PlayPage> NewGame(string firstSide, string secondSide)
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = firstSide, ["second"] = secondSide, ["first-elr"] = firstSide == "russian" ? "2" : "3", ["second-elr"] = secondSide == "russian" ? "2" : "3", ["month"] = "7" });
        return page;
    }

    private static string Row(IRenderedComponent<PlayPage> page, string unit) => page.Find($"#play-units tr[data-unit='{unit}']").TextContent;

    [Fact]
    public void ALeaderRalliesInTheRphAndAMgJoinsTheFireThatRemovesADummy()
    {
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", hexes.From, broken: true);
        Place(page, "r2", "defender-squad", hexes.From);
        Place(page, "rl", "defender-leader", hexes.From);
        Place(page, "mg1", "defender-lmg", hexes.From, holder: "r2");
        Place(page, "gd", "dummy:german", hexes.Open);
        Assert.Contains("a Dummy", page.Find("#place-list").TextContent, StringComparison.Ordinal);
        // Pass 30: a row's holder is changed in the row, so it is a select holding the choice.
        Assert.Equal("r2", page.Find("#place-row-3-holder").GetAttribute("value"));
        Commit(page, "#propose-setup");
        Assert.Contains("broken", Row(page, "r1"), StringComparison.Ordinal);

        // U21 on the page: rl rallies r1 in Open Ground, 1+2 = 3 against its broken morale.
        page.Find("#rally-unit").Change("r1");
        page.Find("#rally-leader").Change("rl");
        Roll(1, 2);
        Commit(page, "#propose-rally");
        Assert.Empty(dice);
        Assert.DoesNotContain("broken", Row(page, "r1"), StringComparison.Ordinal);
        Assert.Contains(page.FindAll("#play-rolls li"), item => item.TextContent.StartsWith("Rally DR: 1, 2", StringComparison.Ordinal));
        var rallied = page.Find("#play-rallies .rally-record").RecordText();
        Assert.StartsWith($"8-0 leader R1 rallies 4-4-7 squad R1 in {DisplayText.Place(1, hexes.From)}: DR 1, 2 = 3", rallied, StringComparison.Ordinal);
        Assert.Contains("against", rallied, StringComparison.Ordinal);
        Assert.EndsWith(": rallied", rallied, StringComparison.Ordinal);

        // U24 and U27 on the page: r2 fires with its LMG at the Dummy; the colored 1 keeps the LMG's ROF (A9.2), and an
        // effect removes the Dummy (A12.14).
        page.EndPhase(Commit);
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find(".fire-weapon[data-weapon='mg1']").Change(true);
        page.Find("#fire-target").Change(hexes.Open);
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal($"4-4-7 squad R2 with its LMG in {DisplayText.Place(1, hexes.From)}", page.Find("#fire-facts tr[data-fact='firers'] td:last-child").TextContent);
        Roll(1, 2);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Empty(dice);
        var group = page.Find("#play-fires .fire-group").TextContent.Trim();
        Assert.Equal($"4-4-7 squad R2 with its LMG in {DisplayText.Place(1, hexes.From)} fires at {DisplayText.Place(1, hexes.Open)}", group);
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Contains("eliminated", Row(page, "gd"), StringComparison.Ordinal);
        Assert.Contains("prep-fire", Row(page, "r2"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".fire-firer:checked"));
    }

    [Fact]
    public void AStackMovesUnderDefensiveFirstFireAndLeavesResidualFpOnTheMap()
    {
        var hexes = Hexes();
        var page = NewGame("german", "russian");
        Place(page, "g1", "attacker-squad", hexes.From);
        Place(page, "g2", "attacker-squad", hexes.From);
        Place(page, "r1", "defender-squad", hexes.Open);
        Place(page, "r2", "defender-squad", hexes.Open);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        Assert.Contains("Movement", page.Find("#play-summary").TextContent, StringComparison.Ordinal);

        // U25 on the page: g1 enters the building; the DEFENDER's window opens and the ATTACKER waits.
        page.Find(".move-unit[data-unit='g1']").Change(true);
        page.Find("#move-to").Change(hexes.Building);
        Commit(page, "#propose-move");
        Assert.Equal("open", page.Find("#move-state").GetAttribute("data-window"));
        Assert.True(page.Find("#propose-end-move").HasAttribute("disabled"));

        // The Russians fire at the building, typed in: 6+5 with -1 FFNAM and +3 for the stone building is no effect, and the
        // attack leaves Residual FP in the building, drawn on the map for every viewer. The screen is handed to them first (ruling R23.1).
        page.ViewAs("russian");
        page.Find("#fire-from").Change(hexes.Open);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-free-target").Change(hexes.Building);
        Roll(6, 5);
        Commit(page, "#propose-fire");
        Assert.Empty(dice);
        Assert.Contains($"in {DisplayText.Place(1, hexes.Building)}", page.Find("#play-residual").TextContent, StringComparison.Ordinal);
        foreach (var perspective in new[] { "german", "russian", Perspective.AdjudicatorName })
        {
            page.ViewAs(perspective);
            page.WaitForAssertion(() => Assert.Equal(hexes.Building, context.MapQuery(".play-residual")?.GetAttribute("data-location")));
        }

        Assert.Contains("first-fire", Row(page, "r1"), StringComparison.Ordinal);

        // The DEFENDER passes and the ATTACKER ends the move.
        Commit(page, "#propose-pass");
        Assert.Equal("closed", page.Find("#move-state").GetAttribute("data-window"));
        Commit(page, "#propose-end-move");
        Assert.Empty(page.FindAll("#move-state"));
        Assert.Contains("movement ended", Row(page, "g1"), StringComparison.Ordinal);

        // U26 on the page: g2 entering the building is attacked by the Residual FP first, alone (A8.22).
        page.Find(".move-unit[data-unit='g2']").Change(true);
        page.Find("#move-to").Change(hexes.Building);
        Roll(6, 5);
        Commit(page, "#propose-move");
        Assert.Empty(dice);
        Assert.Equal($"8 Residual FP fires at {DisplayText.Place(1, hexes.Building)}", page.Find("#play-fires .fire-group").TextContent.Trim());
        Commit(page, "#propose-pass");
        Commit(page, "#propose-end-move");

        // Residual FP is gone when the MPh ends (A8.2).
        page.EndPhase(Commit);
        Assert.Empty(page.FindAll("#play-residual"));
        page.WaitForAssertion(() => Assert.Null(context.MapQuery(".play-residual")));
    }

    [Fact]
    public void ALocationHoldingOnlyADummyOrALeaderIsNotOfferedAsAFireGroup()
    {
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", hexes.From);
        Place(page, "gd", "dummy:german", hexes.Building);
        Place(page, "g1", "attacker-squad", hexes.Open);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);

        // In the Russian MPh the Germans are the DEFENDER: g1's Location is offered, the Dummy's is not (A12.1).
        page.ViewAs("german");
        Assert.Equal(["", hexes.Open], page.FindAll("#fire-from option").Select(option => option.GetAttribute("value") ?? string.Empty));
    }

    [Fact]
    public void AGameUsingTheArchivedCatalogReplaysWithoutRewritingItsRecord()
    {
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", hexes.From);
        Commit(page, "#propose-setup");

        var file = Directory.GetFiles(live.Root, "village.game.json", SearchOption.AllDirectories).Single();
        File.WriteAllText(file, File.ReadAllText(file).Replace("asl-scenario-a1@1.13.0", "asl-scenario-a1@1.12.0", StringComparison.Ordinal));
        var original = File.ReadAllBytes(file);
        var reopened = context.Render<PlayPage>();
        reopened.OpenGame("village");

        Assert.Empty(reopened.FindAll("#play-replay-failed"));
        Assert.False(live.History("village")!.HasErrors);
        Assert.False(context.Services.GetRequiredService<GameLibrary>().Load("live:village").History!.HasErrors);
        Assert.Equal(original, File.ReadAllBytes(file));
    }

    [Fact]
    public void AGameThatDoesNotReplaySaysWhy()
    {
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", hexes.From);
        Commit(page, "#propose-setup");

        // The game is rewritten to name a catalog the Studio no longer carries, as a game set up before catalog 1.2.0 does.
        var file = Directory.GetFiles(live.Root, "village.game.json", SearchOption.AllDirectories).Single();
        File.WriteAllText(file, File.ReadAllText(file).Replace("asl-scenario-a1@1.13.0", "asl-scenario-a1@1.1.0", StringComparison.Ordinal));
        var reopened = context.Render<PlayPage>();
        reopened.OpenGame("village");
        Assert.Contains("does not replay", reopened.Find("#play-replay-failed").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(reopened.FindAll("#play-replay-failed li"));
        Assert.Contains("asl-scenario-a1@1.1.0", reopened.Find("#play-replay-catalog").TextContent, StringComparison.Ordinal);
    }
}
