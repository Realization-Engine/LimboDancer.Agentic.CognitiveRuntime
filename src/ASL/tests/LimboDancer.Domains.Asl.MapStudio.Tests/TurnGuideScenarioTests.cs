using System.Text.Json;
using ASL.Turns;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Vasl;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class TurnGuideScenarioFactAttribute : FactAttribute
{
    public TurnGuideScenarioFactAttribute()
    {
        if (VaslSource.FromEnvironment() is null)
        {
            Skip = $"Set {VaslSource.EnvironmentVariable} to a local VASL checkout for the real-board scenario test.";
        }
    }
}

/// <summary>The recorded Tractor Works setup continued through the real Play page and governed commands.</summary>
public sealed class TurnGuideScenarioTests : IDisposable
{
    private const string Game = "turn-guide-tractor";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-turn-scenario-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private static GameScope Scope => new(LivePlay.Tenant, Game);
    private GameState Current => live.History(Game)!.Current!;

    public TurnGuideScenarioTests()
    {
        var options = new StudioOptions
        {
            CacheRoot = Path.Combine(root, "cache"),
            BoardsRoot = Path.Combine(root, "boards"),
            VaslRoot = Environment.GetEnvironmentVariable(VaslSource.EnvironmentVariable)
        };
        var library = new UnitLibrary(options);
        var boards = new VaslBoardProvider(options);
        var maps = new MapService(options, boards);
        live = new LivePlay(library, boards, dice.Roller, openingReviewer: GamePlay.Principal("scenario-referee", LivePlay.Tenant, GameActions.WindowAdjudicationPermission));
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, library, games));
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
        context.Services.AddSingleton(dice);
        context.UseViewport();
        var directory = Path.Combine(live.Root, LivePlay.Tenant.ToString("N"));
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "p31c-tw.game.json"), Path.Combine(directory, "p31c-tw.game.json"));
        Assert.Equal(AppendStatus.Committed, live.PlayOn("p31c-tw", 133, Game, "isolated guide acceptance").Status);
        Assert.False(live.Planner.Replay(live.Store.Read(Scope)!.Events).HasErrors);
        Assert.Equal("rph", Current.Phase);
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private async Task Do(LimboDancer.Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Game;
        node["attemptId"] = $"scenario-{action.Id.Value}-{Current.Revision}";
        node["expectedRevision"] = Current.Revision;
        var args = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(action, args, live.Principal);
        Assert.True(proposed.Outcome == PlayOutcome.NeedsConfirmation, string.Join("; ", proposed.Reasons));
        var result = await live.Play.ConfirmAsync(action, args, live.Principal, proposed.Correlation);
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
    }

    private void Commit(IRenderedComponent<PlayPage> page, string selector)
    {
        var before = Current.Revision;
        var queued = dice.Queued.Count;
        page.Find(selector).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(before, Current.Revision);
        Assert.Equal(queued, dice.Queued.Count);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.True(Current.Revision > before);
    }

    private async Task<IRenderedComponent<PlayPage>> PlaceAndReachAfph()
    {
        // The recorded setup predates the mandatory Sniper setup step. Complete it through today's governed setup.
        foreach (var side in live.Planner.SniperPlacementsDue(Current))
        {
            await Do(GameActions.Setup, new
            {
                sniper = new
                {
                    side,
                    at = live.Planner.SniperPlacements(Current, side)[0].ToString()
                }
            });
        }
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs("german");
        page.Find("#play-one-click").Change(false);
        page.Find("#play-turn-guide-toggle").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".turn-guide [data-topic]")));
        var before = Current.Revision;
        var topics = 0;
        foreach (var phase in TurnGuideCatalog.Load().Phases)
        {
            page.Find($".turn-guide [data-phase='{phase.Id}']").Click();
            page.WaitForAssertion(() => Assert.Equal(phase.Topics.Length, page.FindAll(".turn-guide [data-topic]").Count));
            topics += page.FindAll(".turn-guide [data-topic]").Count;
            Assert.Contains("Pending rules:", page.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        }
        Assert.Equal(538, topics);
        Assert.Equal(before, Current.Revision);
        page.Find(".turn-guide [data-phase=rph]").Click();
        Commit(page, ".turn-close");
        Commit(page, ".turn-close");
        Assert.Equal("mph", Current.Phase);
        page.Find(".move-unit[data-unit='g-engineer-3']").Change(true);
        page.Find("#place-dc").Change("g-dc-3");
        page.Find("#place-dc-at").Change("bd01:BB3:0");
        Commit(page, "#propose-place-dc");
        page.PassAs("russian", Commit);
        Commit(page, "#propose-end-move");
        Commit(page, ".turn-close");
        Assert.Equal("dfph", Current.Phase);
        page.ViewAs("russian");
        Commit(page, ".turn-close");
        page.ViewAs("german");
        Assert.Equal("afph", Current.Phase);
        Assert.Single(Current.PlacedCharges);
        page.Find("[data-topic='afph:placed']").Click();
        Assert.Contains("Pending rules:", page.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".turn-action"));
        return page;
    }

    private async Task Review(TurnOpeningEvidence wp = TurnOpeningEvidence.NotApplicable)
    {
        var charge = Assert.Single(Current.PlacedCharges);
        var review = new TurnPlacedDcReview(charge.Charge, charge.OriginEventId!, Current.TurnPhaseOccurrence!.EventId,
            "german", "Tractor Works: no drift, WP, Gusts or glider activity before this DC", new(TurnOpeningEvidence.NotApplicable, wp,
                TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.NotApplicable));
        var args = JsonSerializer.SerializeToElement(new
        {
            gameId = Game,
            attemptId = "opening-" + Current.Revision,
            expectedRevision = Current.Revision,
            review
        });
        var before = Current.Revision;
        Assert.Equal(PlayOutcome.Denied, (await live.Play.ProposeAsync(GameActions.ReviewPlacedDcWindow, args, live.Principal)).Outcome);
        Assert.Equal(before, Current.Revision);
        var referee = GamePlay.Principal("scenario-referee", LivePlay.Tenant, GameActions.WindowAdjudicationPermission);
        var proposal = await live.Play.ProposeAsync(GameActions.ReviewPlacedDcWindow, args, referee);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposal.Outcome);
        Assert.Equal(PlayOutcome.Committed, (await live.Play.ConfirmAsync(GameActions.ReviewPlacedDcWindow, args, referee, proposal.Correlation)).Outcome);
    }

    private void ReviewInUi(IRenderedComponent<PlayPage> page, string wp = "NotApplicable")
    {
        Assert.Empty(page.FindAll(".turn-opening-review"));
        page.ViewAs(Perspective.AdjudicatorName);
        page.Find("[data-topic='afph:placed']").Click();
        page.WaitForAssertion(() => Assert.Single(page.FindAll(".turn-opening-review")));
        Assert.True(page.Find(".turn-review-propose").HasAttribute("disabled"));
        page.Find(".turn-review-drift").Change("NotApplicable");
        page.Find(".turn-review-wp").Change(wp);
        page.Find(".turn-review-gusts").Change("NotApplicable");
        page.Find(".turn-review-gliders").Change("NotApplicable");
        page.Find(".turn-review-basis").Input("ASOP 5.11B-5.21A: checked this Tractor Works opening.");
        var before = Current.Revision;
        page.Find(".turn-review-propose").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(before, Current.Revision);
        Assert.Contains("Opening Dispersed WP (5.12A): " + (wp == "NotApplicable" ? "Not applicable" : wp), page.Find(".turn-review-summary").TextContent, StringComparison.Ordinal);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(before + 1, Current.Revision);
        Assert.Single(Current.PlacedCharges);
        Assert.Empty(page.FindAll(".turn-action"));
        page.ViewAs("german");
        page.Find("[data-topic='afph:placed']").Click();
        Assert.Empty(page.FindAll(".turn-opening-review"));
    }

    [TurnGuideScenarioFact]
    public async Task OpeningReviewUnknownCancelAndStaleAreSafeInTheUi()
    {
        var page = await PlaceAndReachAfph();
        ReviewInUi(page, "Unknown");
        Assert.Empty(page.FindAll(".turn-action"));
        page.ViewAs(Perspective.AdjudicatorName);
        page.Find("[data-topic='afph:placed']").Click();
        page.Find(".turn-review-basis").Input("Partial review remains Unknown.");
        var before = Current.Revision;
        page.Find(".turn-review-propose").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-cancel").Click();
        Assert.Equal(before, Current.Revision);
        page.Find(".turn-review-propose").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        await Review(); // A different authorized host commits while this proposal is waiting.
        before = Current.Revision;
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Stale", page.Find("#play-outcome").TextContent, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before, Current.Revision);
        page.ViewAs("german");
        Assert.Empty(page.FindAll(".turn-review-summary, .turn-opening-review, #play-confirm"));
    }

    // Optional isolated browser rehearsal, generated only by explicit request. Never overwrite an existing game.
    private void SaveBrowserSeed()
    {
        if (Environment.GetEnvironmentVariable("ASL_TURN_GUIDE_BROWSER_ROOT") is not { Length: > 0 } destination)
        {
            return;
        }
        var directory = Path.Combine(destination, "units", "live", LivePlay.Tenant.ToString("N"));
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(live.Root, LivePlay.Tenant.ToString("N"), Game + ".game.json"), Path.Combine(directory, Game + ".game.json"));
    }

    [TurnGuideScenarioFact]
    public async Task TractorWorksGuidedDcResolvesThenPhaseCloseCommits()
    {
        var page = await PlaceAndReachAfph();
        var before = Current.Revision;
        page.Find(".turn-close").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(before, Current.Revision);
        Assert.Equal(TurnCloseDisposition.Blocked, (await live.Planner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-work-v1")).PhaseClose.Disposition);
        ReviewInUi(page);
        before = Current.Revision;
        page.Find(".turn-close").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(before, Current.Revision); // Complete evidence alone never discharges the attack obligation.
        SaveBrowserSeed();
        page.Find(".turn-refresh").Click();
        page.WaitForAssertion(() => Assert.Single(page.FindAll(".turn-action")));
        dice.Enqueue(Enumerable.Repeat(5, 20));
        Commit(page, ".turn-action");
        Assert.Empty(Current.PlacedCharges);
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".turn-action")));
        Assert.Contains("Resolved", page.Find(".turn-evidence").TextContent, StringComparison.Ordinal);
        Assert.Contains("Pending rules:", page.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        Assert.Contains(live.Store.Read(Scope)!.Events, item => item.GuidedDecisionHash is not null);
        Commit(page, ".turn-close");
        Assert.Equal("rtph", Current.Phase);
        page.WaitForAssertion(() => Assert.Equal("true", page.Find(".turn-guide [data-phase=rtph]").GetAttribute("aria-pressed")));
        Assert.False(live.Planner.Replay(live.Store.Read(Scope)!.Events).HasErrors);
        before = Current.Revision;
        page.Dispose();
        var reopened = context.Render<PlayPage>();
        reopened.OpenGame(Game);
        reopened.ViewAs("german");
        reopened.Find("#play-turn-guide-toggle").Click();
        reopened.WaitForAssertion(() => Assert.Contains("Resolved", reopened.Find(".turn-evidence").TextContent, StringComparison.Ordinal));
        Assert.Equal("true", reopened.Find(".turn-guide [data-phase=rtph]").GetAttribute("aria-pressed"));
        Assert.Contains("Pending rules:", reopened.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        Assert.Empty(reopened.FindAll(".turn-action, .turn-opening-review, #play-confirm"));
        Assert.Equal(before, Current.Revision); // Reopening and reading guidance must not append events.
    }

    [TurnGuideScenarioFact]
    public async Task TractorWorksStaleConfirmationCannotDetonateOrClose()
    {
        var page = await PlaceAndReachAfph();
        await Review();
        page.Find(".turn-refresh").Click();
        page.WaitForAssertion(() => Assert.Single(page.FindAll(".turn-action")));
        page.Find(".turn-action").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        await Review(TurnOpeningEvidence.Unknown);
        var before = Current.Revision;
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Stale", page.Find("#play-outcome").TextContent, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before, Current.Revision);
        Assert.Single(Current.PlacedCharges);
        page.Find(".turn-refresh").Click();
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".turn-action")));
        Assert.Contains("Unknown", page.Find(".turn-evidence").TextContent, StringComparison.Ordinal);
        Assert.Contains("Pending rules:", page.Find(".turn-guide").TextContent, StringComparison.Ordinal);
        Assert.Equal(TurnCloseDisposition.Blocked, (await live.Planner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-work-v1")).PhaseClose.Disposition);
    }
}
