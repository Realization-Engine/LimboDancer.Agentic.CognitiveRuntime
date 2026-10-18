using Bunit;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The SVG memory analysis, section 13: a development aid that makes a new game follow a recorded one, a replay step at a time, so the Play page
/// does on each step the work it does after a confirm.
/// </summary>
public sealed class FollowRecordTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-follow-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public FollowRecordTests()
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
        context.Services.AddSingleton(new GameMaps(boards, maps, library, games));
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

    private IReadOnlyList<GameEvent> Events(string game) => live.Store.Read(new GameScope(LivePlay.Tenant, game))!.Events;

    /// <summary>A recorded game "village": a setup and three phase ends, each a step of its replay.</summary>
    private void Record()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        var hexes = handle.Geometry.Hexes().Select(index => handle.Geometry.NameOf(index)).Take(2).ToArray();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        foreach (var (id, definition, hex) in new[] { ("g1", "attacker-squad", hexes[0]), ("r1", "defender-squad", hexes[1]) })
        {
            page.Find("#place-definition").Change(definition);
            page.Find("#place-id").Change(id);
            page.Find("#place-location").Change($"{Board}:{hex}:0");
            page.Find("#place-add").Click();
        }

        Commit(page, "#propose-setup");
        for (var phase = 0; phase < 3; phase++)
        {
            page.EndPhase(Commit);
        }
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void AFollowerRepeatsTheRecordAStepAtATime()
    {
        Record();
        var record = Events("village");
        Assert.Equal(AppendStatus.Committed, live.StartFollowing("village", "village-follow-1").Status);
        var sizes = new List<int> { Events("village-follow-1").Count };
        while (Events("village-follow-1").Count < record.Count)
        {
            Assert.Equal(AppendStatus.Committed, live.FollowStep("village", "village-follow-1").Status);
            sizes.Add(Events("village-follow-1").Count);
        }

        // Each step is one attempt of the record, as Replay counts them, and the follower ends with the record's events in its order.
        var attempts = record.Select(item => ReplaySteps.AttemptOf(item.EventId)).Distinct(StringComparer.Ordinal).Count();
        Assert.Equal(attempts, sizes.Count);
        Assert.Equal(record.Select(item => item.EventId), Events("village-follow-1").Select(item => item.EventId));
        Assert.Equal(record.Select(item => item.Type), Events("village-follow-1").Select(item => item.Type));
        Assert.Equal(live.History("village")!.Current!.Revision, live.History("village-follow-1")!.Current!.Revision);

        var done = live.FollowStep("village", "village-follow-1");
        Assert.Equal(AppendStatus.Invalid, done.Status);
        Assert.Equal("FOLLOW-003", Assert.Single(done.Diagnostics).Code);
    }

    [Fact]
    public void AFollowerThatLeftTheRecordIsRefusedAndAMissingRecordToo()
    {
        Record();
        Assert.Equal(AppendStatus.Committed, live.StartFollowing("village", "village-follow-1").Status);

        // An event the record does not hold, as a step played on by hand would add: the follower has left the record, and is refused from then on.
        var copy = Events("village-follow-1");
        var diverged = live.Store.Append(new GameScope(LivePlay.Tenant, "village-follow-1"), "diverged", copy.Count,
            [new GameEvent(new GameScope(LivePlay.Tenant, "village-follow-1"), "hand-0000000000-1", copy.Count + 1, DateTimeOffset.UtcNow, "test",
                Events("village")[copy.Count].Type, Events("village")[copy.Count].Payload, Events("village")[copy.Count].RulePackage, [],
                Events("village")[copy.Count].Visibility)], live.Planner.Replay);
        Assert.Equal(AppendStatus.Committed, diverged.Status);
        var refused = live.FollowStep("village", "village-follow-1");
        Assert.Equal("FOLLOW-002", Assert.Single(refused.Diagnostics).Code);

        Assert.Equal("FOLLOW-001", Assert.Single(live.StartFollowing("nowhere", "nowhere-follow-1").Diagnostics).Code);
        Assert.Equal("FOLLOW-001", Assert.Single(live.FollowStep("village", "nowhere").Diagnostics).Code);
    }

    [Fact]
    public void ThePlayPageFollowsARecordWhereScriptedDiceAre()
    {
        Record();

        // The lobby: no game open.
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("games/play");
        var page = context.Render<PlayPage>();
        page.Find("#follow-record-source").Change("village");
        page.Find("#follow-record-start").Click();

        // The new game opens behind the hand-over screen, as a game opened from Replay's play on does.
        page.WaitForAssertion(() => Assert.True(page.FindAll("#play-handover-confirm").Count > 0 || page.FindAll("#follow-record-progress").Count > 0));
        if (page.FindAll("#play-handover-confirm").Count > 0)
        {
            page.Find("#play-handover-confirm").Click();
        }

        page.WaitForAssertion(() => Assert.Contains("Following village", page.Find("#follow-record-progress").TextContent, StringComparison.Ordinal));

        var before = live.History("village-follow-1")!.Current!.Revision;
        page.Find("#follow-record-next").Click();
        page.WaitForAssertion(() => Assert.True(live.History("village-follow-1")!.Current!.Revision > before));
        Assert.Contains($"revision {live.History("village-follow-1")!.Current!.Revision} of {Events("village").Count}",
            page.Find("#follow-record-progress").TextContent, StringComparison.Ordinal);
    }
}
