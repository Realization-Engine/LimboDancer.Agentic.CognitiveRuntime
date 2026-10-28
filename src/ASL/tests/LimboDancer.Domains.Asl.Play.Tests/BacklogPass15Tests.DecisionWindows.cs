using System.Text;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass15Tests
{
    [Fact]
    public async Task DurableWindowReviewBindsOccurrenceOriginAuthorityAndOpeningEvidence()
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
        var origin = Current.PlacedCharges.Single().OriginEventId!;
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        var mph = Current.TurnPhaseOccurrence!;
        await Advance(2);
        var entry = Current.TurnPhaseOccurrence!;
        Assert.NotEqual(mph.EventId, entry.EventId);
        Assert.Equal("afph", entry.Phase);
        var initial = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", "existing-engine-dc-v1");
        Assert.Equal(TurnDecisionTiming.NotEvaluated, Assert.Single(initial.DecisionWindows).Timing);
        Assert.Equal("afph:phase", initial.Guide.WindowId);
        Assert.All(initial.Guide.Options.SelectMany(option => option.Decisions), decision => Assert.Null(decision.ActionRoute));
        Assert.Empty((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "afph", "existing-engine-dc-v1")).DecisionWindows);
        Assert.Empty((await Planner().ReadTurnGuideAsync(Scope, Perspective.Adjudicator, "afph")).DecisionWindows);
        var review = new TurnPlacedDcReview("dc", origin, entry.EventId, "german", "A23.4; ASOP opening externally reviewed",
            new(TurnOpeningEvidence.Resolved, TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.Resolved, TurnOpeningEvidence.NotApplicable));
        JsonElement Args(TurnPlacedDcReview value) => JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "review-" + Revision,
            expectedRevision = Revision,
            review = value
        });
        var play = Play();
        var args = Args(review);
        var denied = await play.ProposeAsync(GameActions.ReviewPlacedDcWindow, args, Player);
        Assert.NotEqual(PlayOutcome.NeedsConfirmation, denied.Outcome);
        Assert.Null(Current.PlacedDcWindowReview);
        foreach (var invalid in new[] { review with { PhaseEventId = mph.EventId }, review with { Owner = "russian" },
            review with { OriginEventId = "missing" }, review with { Charge = "missing" }, review with { RuleBasis = "" } })
        {
            Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.ReviewPlacedDcWindow, Args(invalid), Tenant)).Status);
        }
        var principal = GamePlay.Principal("window-adjudicator", Tenant, GameActions.WindowAdjudicationPermission);
        var proposed = await play.ProposeAsync(GameActions.ReviewPlacedDcWindow, args, principal);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Committed(await play.ConfirmAsync(GameActions.ReviewPlacedDcWindow, args, principal, proposed.Correlation));
        var reviewedAt = Revision;
        Assert.Equal(GamePlanStatus.Replay, (await Planner().PlanAsync(GameActions.ReviewPlacedDcWindow, args, Tenant)).Status);
        var changedAttempt = JsonSerializer.SerializeToNode(args)!.AsObject();
        changedAttempt["review"] = JsonSerializer.SerializeToNode(review with
        {
            RuleBasis = "changed"
        });
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.ReviewPlacedDcWindow,
            JsonSerializer.SerializeToElement(changedAttempt), Tenant)).Status);

        Assert.Equal(entry, Current.TurnPhaseOccurrence);
        var ready = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", "existing-engine-dc-v1");
        Assert.Equal(TurnDecisionTiming.ReadyForCommandAssessment, Assert.Single(ready.DecisionWindows).Timing);
        Assert.DoesNotContain(origin, JsonSerializer.Serialize(ready.DecisionWindows));
        Assert.DoesNotContain(entry.EventId, JsonSerializer.Serialize(ready.DecisionWindows));
        Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, ready.PhaseClose.Disposition);
        var serialized = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(serialized)).Record!;
        Assert.Equal(serialized, GameEventWriter.Write(Scope, loaded));
        Assert.Equal(TurnDecisionTiming.ReadyForCommandAssessment, Assert.Single((await ReadCopiedDcHistory(loaded.Events, Perspective.Side("german"))).DecisionWindows).Timing);
        var withheld = loaded.Events.Select(item => item.Revision == reviewedAt ? item with { Visibility = ["russian"] } : item).ToArray();
        Assert.Equal(TurnDecisionTiming.NotEvaluated, Assert.Single((await ReadCopiedDcHistory(withheld, Perspective.Side("german"))).DecisionWindows).Timing);
        var expiredEvent = loaded.Events[^1] with
        {
            EventId = "window-state-changed",
            Revision = reviewedAt + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { ["asl:pinned"] = ConditionState.True }),
        };
        Assert.Equal(TurnDecisionTiming.NotEvaluated, Assert.Single((await ReadCopiedDcHistory([.. loaded.Events, expiredEvent], Perspective.Adjudicator)).DecisionWindows).Timing);
        var legacy = loaded.Events.Take(loaded.Events.Count - 1).Select(item => item.Payload is MovementStepped { DcPlacement: { } placement } step ? item with
        {
            Payload = step with
            {
                DcPlacement = placement with
                {
                    OwnerSide = null
                }
            }
        } : item).ToArray();
        Assert.Equal(TurnDecisionTiming.NotEvaluated, Assert.Single((await ReadCopiedDcHistory(legacy, Perspective.Adjudicator)).DecisionWindows).Timing);
        Assert.Empty((await ReadCopiedDcHistory(legacy, Perspective.Adjudicator)).OpeningReviews);
        Assert.True(Planner().Replay([.. legacy, loaded.Events[^1]]).HasErrors);
        var hiddenEntry = loaded.Events.Select(item => item.EventId == entry.EventId ? item with
        {
            Visibility = ["russian"]
        } : item).ToArray();
        Assert.Equal(TurnDecisionTiming.NotEvaluated, Assert.Single((await ReadCopiedDcHistory(hiddenEntry, Perspective.Side("german"))).DecisionWindows).Timing);
        var hiddenOrigin = loaded.Events.Select(item => item.EventId == origin ? item with
        {
            Visibility = ["russian"]
        } : item).ToArray();
        Assert.Empty((await ReadCopiedDcHistory(hiddenOrigin, Perspective.Side("german"))).DecisionWindows);
        Assert.Equal(reviewedAt, Revision);
        Committed(await Do(GameActions.DetonateDc, Then(3), new
        {
            equipmentId = "dc"
        }));
        Assert.Null(Current.PlacedDcWindowReview);
        Assert.Empty((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", "existing-engine-dc-v1")).DecisionWindows);
        var past = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", "existing-engine-dc-v1", reviewedAt);
        Assert.True(Assert.Single(past.DecisionWindows).Historical);
        Assert.Equal(TurnDecisionTiming.ReadyForCommandAssessment, past.DecisionWindows[0].Timing);
        var before = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", "existing-engine-dc-v1", reviewedAt - 1);
        Assert.Equal(TurnDecisionTiming.NotEvaluated, Assert.Single(before.DecisionWindows).Timing);
        await Advance();
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.ReviewPlacedDcWindow, Args(review), Tenant)).Status);
    }
}
