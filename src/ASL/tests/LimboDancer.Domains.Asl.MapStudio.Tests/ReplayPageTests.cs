using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Components.Replay;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;
using ReplayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Replay;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31b: the Replay page. A game still played opens behind the hand-over screen and shows a side no revision (ruling R31b.1); the transport,
/// the keys, and the timeline move through the view's own steps, and the address follows; "Play on from here" makes a new game the Play page opens
/// and leaves the first as it was; an ended game shows any view at once, from its first step; and Play's records lead to their steps.
/// </summary>
public sealed class ReplayPageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-replay-page-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public ReplayPageTests()
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

    private NavigationManager Navigation => context.Services.GetRequiredService<NavigationManager>();

    private GameHistory Game(string name = "village") => live.History(name)!;

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

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-add").Click();
    }

    /// <summary>
    /// A game of a squad a side on a minimal card, played through the Play page to the German Movement Phase of Turn 1: the start and the setup
    /// (one step), then the ends of the Rally and Prep Fire Phases.
    /// </summary>
    private void PlayAGame()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        Assert.Equal("mph", Game().Current!.Phase);
    }

    /// <summary>The Replay page opened at an address; a game still played waits behind the hand-over, which its first viewer confirms here.</summary>
    private IRenderedComponent<ReplayPage> Open(string address, bool confirm = true)
    {
        Navigation.NavigateTo(address);
        var page = context.Render<ReplayPage>();
        if (confirm && page.FindAll("#play-handover-confirm") is [var button])
        {
            button.Click();
        }

        return page;
    }

    private static int Step(IRenderedComponent<ReplayPage> page) => int.Parse(page.Find("#replay-step").GetAttribute("value")!, System.Globalization.CultureInfo.InvariantCulture);

    private static int Steps(IRenderedComponent<ReplayPage> page) => page.FindAll("#replay-timeline .replay-step").Count;

    // Ruling R31b.1: a game still played keeps Play's views and hand-over; a side is shown no revision, no link that carries one, and no way to
    // play on from a moment of a game it may not know whole.
    [Fact]
    public void AGameStillPlayedOpensBehindTheHandOverAndShowsASideNoRevision()
    {
        PlayAGame();
        var page = Open("games/replay?game=village&view=russian", confirm: false);
        Assert.Contains("Hand the screen to the Russian side", page.Find("#play-handover-title").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#replay-timeline"));
        Assert.Empty(page.FindAll("#replay-panel-map"));
        Assert.Empty(page.FindAll("#replay-transport"));

        page.Find("#play-handover-confirm").Click();
        Assert.Contains("as the Russian side may read them", page.Find("#replay-count").TextContent, StringComparison.Ordinal);
        Assert.Equal(Steps(page), Step(page));
        Assert.Empty(page.FindAll("#play-revision"));
        Assert.Empty(page.FindAll("#replay-view"));
        Assert.Empty(page.FindAll("#replay-play-on"));
        Assert.Empty(page.FindAll("#play-live"));
        Assert.NotNull(context.MapQuery("g[data-unit-id='r1']"));

        // The adjudicator's view goes through the hand-over too; it reads the revision and may play on from the step.
        page.Find("#replay-perspective").Change(Perspective.AdjudicatorName);
        Assert.Contains("Hand the screen to the adjudicator", page.Find("#play-handover-title").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#replay-timeline"));
        page.Find("#play-handover-confirm").Click();
        Assert.Equal($"revision {Game().States.Count}", page.Find("#play-revision").TextContent.Trim());
        Assert.NotEmpty(page.FindAll("#replay-view"));
        Assert.NotEmpty(page.FindAll("#replay-play-on"));
        Assert.EndsWith($"games/replay?game=village&view=adjudicator&step={Steps(page)}", Navigation.Uri, StringComparison.Ordinal);
    }

    // Design D5: the transport, a click on a step, and the keys move through the steps; a button with nothing to do keeps the focus and does nothing.
    [Fact]
    public async Task TheTransportTheTimelineAndTheKeysMoveThroughTheSteps()
    {
        PlayAGame();
        var page = Open("games/replay?game=village&view=adjudicator");
        var last = Steps(page);
        Assert.True(last >= 3);
        Assert.Equal(last, Step(page));
        Assert.Equal("true", page.Find("#replay-forward").GetAttribute("aria-disabled"));
        page.Find("#replay-forward").Click();
        Assert.Equal(last, Step(page));

        page.Find("#replay-first").Click();
        Assert.Equal(1, Step(page));
        Assert.StartsWith("The game starts", page.Find("#replay-step-title").TextContent, StringComparison.Ordinal);
        Assert.Equal("Setup, before play.", page.Find("#play-summary").TextContent.Trim());
        Assert.Equal("true", page.Find("#replay-back").GetAttribute("aria-disabled"));
        Assert.EndsWith("&step=1", Navigation.Uri, StringComparison.Ordinal);

        page.Find("#replay-forward").Click();
        Assert.Equal(2, Step(page));
        page.Find("#replay-last").Click();
        Assert.Equal(last, Step(page));
        page.Find("#replay-back").Click();
        Assert.Equal(last - 1, Step(page));
        page.Find("#replay-phase-back").Click();
        Assert.True(Step(page) < last - 1 || last - 1 == 1);

        // The step shown is the timeline's one tab stop, and a click on another step goes to it.
        page.Find($"#replay-step-{last}").Click();
        Assert.Equal(last, Step(page));
        Assert.Equal("step", page.Find($"#replay-step-{last}").GetAttribute("aria-current"));
        Assert.Equal($"replay-step-{last}", Assert.Single(page.FindAll("#replay-timeline .replay-step[tabindex='0']")).Id);
        Assert.Contains("ends", page.Find("#replay-step-title").TextContent, StringComparison.Ordinal);

        // The keys, as the page's script sends them from the map or the timeline.
        await page.Instance.OnReplayKey("Home");
        Assert.Equal(1, Step(page));
        await page.Instance.OnReplayKey("ArrowRight");
        Assert.Equal(2, Step(page));
        await page.Instance.OnReplayKey("End");
        Assert.Equal(last, Step(page));
        await page.Instance.OnReplayKey("ArrowLeft");
        Assert.Equal(last - 1, Step(page));
        await page.Instance.OnReplayKey("PageDown");
        Assert.Equal(last, Step(page));

        // A typed step past the end is the last step.
        page.Find("#replay-step").Change("500");
        Assert.Equal(last, Step(page));
        Assert.EndsWith($"&step={last}", Navigation.Uri, StringComparison.Ordinal);
    }

    // Design D8: a new game from the events up to the step, opened on the Play page; the first game is not changed.
    [Fact]
    public void PlayOnFromHereMakesANewGameAndLeavesTheFirstAsItWas()
    {
        PlayAGame();
        var events = Game().Events.Count;
        var page = Open("games/replay?game=village&view=adjudicator");
        page.Find("#replay-back").Click();
        var step = Step(page);
        var revision = long.Parse(page.Find("#play-revision").TextContent.Trim()["revision ".Length..], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(revision < events);
        Assert.Equal($"village-r{revision}", page.Find("#replay-play-on-name").GetAttribute("value"));

        // A name that is taken is refused with a reason, and nothing is written.
        page.Find("#replay-play-on-name").Change("village");
        page.Find("#replay-play-on-make").Click();
        Assert.Contains("exists already", page.Find("#replay-play-on-problem").TextContent, StringComparison.Ordinal);
        Assert.Equal(["village"], live.Games().Select(scope => scope.Game));

        page.Find("#replay-play-on-name").Change("village-on");
        page.Find("#replay-play-on-make").Click();
        Assert.EndsWith("games/play?game=village-on", Navigation.Uri, StringComparison.Ordinal);
        Assert.Equal(["village", "village-on"], live.Games().Select(scope => scope.Game));
        Assert.Equal(revision, Game("village-on").States.Count);
        Assert.False(Game("village-on").HasErrors);
        Assert.Equal(Game().Events.Take((int)revision).Select(item => item.EventId), Game("village-on").Events.Select(item => item.EventId));
        Assert.EndsWith($"(from village at step {step}, revision {revision})", live.Store.Read(new GameScope(LivePlay.Tenant, "village-on"))!.Label, StringComparison.Ordinal);
        Assert.Equal(events, Game().Events.Count);

        // The Play page opens the new game, at the phase the step left it in.
        var play = context.Render<PlayPage>();
        play.OpenGame("village-on");
        Assert.Contains("Prep Fire Phase", play.Find("#play-summary").TextContent, StringComparison.Ordinal);
    }

    // Ruling R31b.1: an ended game opens in the adjudicator's view at its first step, and any view is chosen at once.
    [Fact]
    public void AnEndedGameOpensAtItsFirstStepAndShowsAnyViewAtOnce()
    {
        PlayAGame();
        var game = Game();
        var scope = new GameScope(LivePlay.Tenant, "village");
        var ended = live.Store.Append(scope, "village", game.Events.Count,
            [new GameEvent(scope, "over-000000000000-1", game.Events.Count + 1, game.Events[^1].Time, "test", "game-ended", new GameEnded(1, "the test ends the game"), null, [], null)],
            live.Planner.Replay);
        Assert.Equal(AppendStatus.Committed, ended.Status);
        Assert.NotNull(Game().Current!.Ended);

        var page = Open("games/replay?game=village", confirm: false);
        Assert.Empty(page.FindAll("#play-handover"));
        Assert.Equal(Perspective.AdjudicatorName, page.Find("#replay-perspective").GetAttribute("value"));
        Assert.Equal(1, Step(page));
        Assert.Contains("Ended", page.Find("#play-live").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-result-line"));

        page.Find("#replay-perspective").Change("german");
        Assert.Empty(page.FindAll("#play-handover"));
        Assert.Contains("as the German side may read them", page.Find("#replay-count").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#replay-view"));
        Assert.NotEmpty(page.FindAll("#replay-play-on"));

        // The result is stated at the last step, from which nothing is played on.
        page.Find("#replay-last").Click();
        Assert.Contains("The game ends", page.Find("#replay-step-title").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#play-result-line"));
        Assert.Empty(page.FindAll("#replay-play-on"));
        Assert.Equal("true", page.Find("#replay-play").GetAttribute("aria-disabled"));
    }

    // Task 31b.8: a record on Play leads to its step, named by its event; a record list given no address shows no link, as on the Replay page itself.
    [Fact]
    public void ARecordLeadsToItsStepAndThePageFindsIt()
    {
        var records = new[] { new ActionRecordList.Entry("rally-0123456789ab-2", "rally", "rl rallies r1") };
        var linked = context.Render<ActionRecordList>(parameters => parameters.Add(list => list.Heading, "Rally").Add(list => list.ListId, "records").Add(list => list.Records, records)
            .Add(list => list.Replay, id => $"games/replay?game=village&view=german&at={id}"));
        var link = linked.Find("#records li a.record-replay");
        Assert.Equal("games/replay?game=village&view=german&at=rally-0123456789ab-2", link.GetAttribute("href"));
        Assert.Equal("Replay from here: rl rallies r1", link.GetAttribute("aria-label"));
        Assert.Equal("Rl rallies r1", linked.Find("#records li").RecordText());
        Assert.Empty(context.Render<ActionRecordList>(parameters => parameters.Add(list => list.Heading, "Rally").Add(list => list.ListId, "plain").Add(list => list.Records, records))
            .FindAll("a.record-replay"));

        // The page opens at the step that holds the event's attempt, and says so when the view has no such step.
        PlayAGame();
        var second = Game().Events.First(item => item.Payload is PhaseChanged);
        var page = Open($"games/replay?game=village&view=adjudicator&at={second.EventId}");
        Assert.True(Step(page) < Steps(page));
        Assert.Equal("The German Rally Phase ends", page.Find("#replay-step-title").TextContent);
        Assert.Empty(page.FindAll("#replay-at-missing"));
    }

    // The table player's review: a record that is no step of the view shown is said to be so, and the note goes with the next step chosen.
    [Fact]
    public void ARecordThatIsNoStepOfTheViewIsSaidToBeSo()
    {
        PlayAGame();
        var page = Open("games/replay?game=village&view=adjudicator&at=nothing-000000000000-1");
        Assert.Contains("is not a step of this view", page.Find("#replay-at-missing").TextContent, StringComparison.Ordinal);
        Assert.Equal(Steps(page), Step(page));
        page.Find("#replay-back").Click();
        Assert.Empty(page.FindAll("#replay-at-missing"));
    }

    // The transport on its own: the jumps come from the timeline it is given, and a refused step draws the field again.
    [Fact]
    public void TheTransportGoesWhereTheTimelineSays()
    {
        ReplayStep[] steps = [.. Enumerable.Range(1, 6).Select(number => new ReplayStep(number, number, number, $"a-{number}", "phase", number <= 3 ? 1 : 2, "rph", "german", null, $"step {number}", ReplayRead.Full))];
        var timeline = new ReplayTimeline(steps, [new ReplayPhase(1, "german", "rph", 1, 3), new ReplayPhase(2, "german", "rph", 4, 3)]);
        var went = new List<int>();
        var transport = context.Render<ReplayTransport>(parameters => parameters.Add(item => item.Timeline, timeline).Add(item => item.Number, 2).Add(item => item.Go, (int number) => went.Add(number)));
        foreach (var id in new[] { "replay-back", "replay-forward", "replay-phase-back", "replay-phase-forward", "replay-turn-back", "replay-turn-forward", "replay-first", "replay-last" })
        {
            transport.Find($"#{id}").Click();
        }

        Assert.Equal([1, 3, 1, 4, 1, 4, 1, 6], went);
        Assert.Contains("of 6", transport.Find(".replay-step-field").TextContent, StringComparison.Ordinal);
        transport.Find("#replay-step").Change("not a number");
        Assert.Equal(8, went.Count);
        transport.Find("#replay-step").Change("0");
        Assert.Equal(1, went[^1]);
    }
}
