using LimboDancer.Domains.Asl.Maps.Read;
using System.Text;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass15Tests
{
    private async Task PlacedDcRouteGame()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        await Advance(2);
    }

    private async Task ReviewDcRoute(TurnOpeningEvidence wp = TurnOpeningEvidence.NotApplicable)
    {
        var charge = Current.PlacedCharges.Single();
        var review = new TurnPlacedDcReview(charge.Charge, charge.OriginEventId!, Current.TurnPhaseOccurrence!.EventId,
            "german", "ASOP opening reviewed", new(TurnOpeningEvidence.NotApplicable, wp, TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.NotApplicable));
        var args = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "route-review-" + Revision,
            expectedRevision = Revision,
            review
        });
        var principal = GamePlay.Principal("referee", Tenant, GameActions.WindowAdjudicationPermission);
        var play = Play();
        var proposed = await play.ProposeAsync(GameActions.ReviewPlacedDcWindow, args, principal);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Committed(await play.ConfirmAsync(GameActions.ReviewPlacedDcWindow, args, principal, proposed.Correlation));
    }

    private Task<TurnHostGuide> DcGuide(Perspective? perspective = null, long? revision = null, string phase = "afph", string profile = "existing-engine-dc-v1") =>
        Planner().ReadTurnGuideProfileAsync(Scope, perspective ?? Perspective.Side("german"), phase, profile, revision);

    [Fact]
    public async Task PlacedDcRouteExecutesThroughNormalConfirmationAndDurableRetry()
    {
        await PlacedDcRouteGame();
        var missing = await DcGuide();
        Assert.Empty(missing.Actions);
        Assert.Equal(TurnApplicability.NotEvaluated, Assert.Single(missing.Guide.Options.Single(item => item.Topic.Id == "afph:placed").Decisions).Applicability);
        await ReviewDcRoute();
        var guide = await DcGuide();
        var route = Assert.Single(guide.Actions);
        var decision = Assert.Single(guide.Guide.Options.Single(item => item.Topic.Id == "afph:placed").Decisions);
        Assert.Equal(TurnApplicability.Required, decision.Applicability);
        Assert.Equal(route.Token, decision.ActionRoute);
        Assert.Equal("afph:phase", guide.Guide.WindowId);
        Assert.All(guide.Guide.Options.Where(item => item.Topic.Id != "afph:placed").SelectMany(item => item.Decisions), item => Assert.Null(item.ActionRoute));
        Assert.Equal(JsonSerializer.Serialize(guide), JsonSerializer.Serialize(await DcGuide()));
        Assert.Empty((await DcGuide(Perspective.Side("russian"))).Actions);
        Assert.Empty((await DcGuide(Perspective.Adjudicator)).Actions);
        Assert.Empty((await DcGuide(revision: Revision)).Actions);
        Assert.Empty((await DcGuide(phase: "rph")).Actions);
        Assert.Empty((await DcGuide(profile: "existing-engine")).Actions);
        foreach (var invalid in new[] { route with { Token = "forged" }, route with { SubjectId = "other" }, route with { Profile = "unknown" }, route with { ActionId = "other" } })
        {
            Assert.Null(await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), invalid, "bad"));
        }
        Assert.Null(await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("russian"), route, "enemy"));
        Assert.Null(await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Adjudicator, route, "referee"));
        var args = (await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), route, "guided-detonation"))!.Value;
        var before = Revision;
        var denied = await Play().ProposeAsync(GameActions.DetonateDc, args, GamePlay.Principal("reader", Tenant));
        Assert.Equal(PlayOutcome.Denied, denied.Outcome);
        Assert.Equal(before, Revision);
        var play = Play(Then(3));
        var proposed = await play.ProposeAsync(GameActions.DetonateDc, args, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Assert.Equal(before, Revision);
        Assert.Equal(TurnCloseDisposition.Blocked, (await DcGuide(Perspective.Adjudicator)).PhaseClose.Disposition);
        Committed(await play.ConfirmAsync(GameActions.DetonateDc, args, Player, proposed.Correlation));
        Assert.Empty(Current.PlacedCharges);
        Assert.Equal(TurnObligationState.Resolved, Assert.Single((await DcGuide()).Obligations).State);
        Assert.Empty((await DcGuide()).Actions);
        Assert.Null(await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), route, "again"));
        Assert.Equal(GamePlanStatus.Replay, (await Planner().PlanAsync(GameActions.DetonateDc, args, Tenant)).Status);
        var after = Revision;
        Assert.Equal(PlayOutcome.Replay, (await Commit(Play(), GameActions.DetonateDc, args)).Outcome);
        Assert.Equal(after, Revision);
        var changed = JsonSerializer.SerializeToNode(args)!.AsObject();
        changed["equipmentId"] = "another-dc";
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.DetonateDc, JsonSerializer.SerializeToElement(changed), Tenant)).Status);
        var journal = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var restored = GameEventReader.Read(Encoding.UTF8.GetBytes(journal)).Record!;
        Assert.Equal(journal, GameEventWriter.Write(Scope, restored));
        Assert.NotNull(restored.Events.Single(item => item.Revision == before + 1).GuidedDecisionHash);
        Assert.False(Planner().Replay(restored.Events).HasErrors);
        var copied = new FileGameStore(Path.Combine(root, "route-reload"));
        var reloadedPlanner = new GamePlanner(copied, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());
        Assert.Equal(AppendStatus.Committed, copied.Append(Scope, "route reload", 0, restored.Events, reloadedPlanner.Replay).Status);
        Assert.Equal(GamePlanStatus.Replay, (await reloadedPlanner.PlanAsync(GameActions.DetonateDc, args, Tenant)).Status);
        await Advance();
    }

    [Fact]
    public async Task PlacedDcRouteRejectsStaleConfirmationAndCannotTreatUnknownReviewAsPermission()
    {
        await PlacedDcRouteGame();
        await ReviewDcRoute();
        var route = Assert.Single((await DcGuide()).Actions);
        var args = (await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), route, "stale-guided"))!.Value;
        var play = Play();
        var proposed = await play.ProposeAsync(GameActions.DetonateDc, args, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        await ReviewDcRoute(TurnOpeningEvidence.Unknown);
        var revision = Revision;
        Assert.Empty((await DcGuide()).Actions);
        Assert.Null(await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), route, "stale"));
        Assert.Equal(PlayOutcome.Stale, (await play.ConfirmAsync(GameActions.DetonateDc, args, Player, proposed.Correlation)).Outcome);
        Assert.Equal(revision, Revision);
        var forged = JsonSerializer.SerializeToNode(args)!.AsObject();
        forged["expectedRevision"] = Revision;
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.DetonateDc, JsonSerializer.SerializeToElement(forged), Tenant)).Status);
        Assert.Single(Current.PlacedCharges);
        Assert.Equal(TurnCloseDisposition.Blocked, (await DcGuide(Perspective.Adjudicator)).PhaseClose.Disposition);
    }

    [Fact]
    public async Task PlacedDcRouteKeepsExistingBattleHardeningChoiceAndRemovalFollowUp()
    {
        await PlacedDcRouteGame();
        await ReviewDcRoute();
        var route = Assert.Single((await DcGuide()).Actions);
        var args = (await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), route, "guided-choice"))!.Value;
        var play = Play(Then(6, 4, 4, 1, 1, 3, 3));
        var before = Revision;
        var proposed = await play.ProposeAsync(GameActions.DetonateDc, args, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Committed(await play.ConfirmAsync(GameActions.DetonateDc, args, Player, proposed.Correlation));
        Assert.Equal(ChoicePending.BattleHardening, Current.Choice!.Kind);
        Assert.Empty((await DcGuide()).Actions);
        Assert.Equal(InstanceStatus.Active, Current.Find("dc")!.Status);
        // Imported timing evidence cannot bypass a pending choice in the full command assessment.
        var events = store.Read(Scope)!.Events;
        var reviewed = events.Single(item => item.Payload is PlacedDcWindowReviewed) with
        {
            EventId = "imported-review-with-choice",
            Revision = Revision + 1
        };
        var blocked = await ReadCopiedDcHistory([.. events, reviewed], Perspective.Side("german"));
        Assert.Empty(blocked.Actions);
        Assert.Equal(TurnApplicability.Unavailable, Assert.Single(blocked.Guide.Options.Single(item => item.Topic.Id == "afph:placed").Decisions).Applicability);
        Committed(await Do(GameActions.Choose, Then(6), new
        {
            key = Current.Choice!.Key,
            option = "decline"
        }));
        Assert.Single(Fires(before));
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("dc")!.Status);
        Assert.Equal(TurnObligationState.Resolved, Assert.Single((await DcGuide()).Obligations).State);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }
}
