using System.Text;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass15Tests
{
    private readonly Dictionary<GamePlanner, FileGameStore> acceptanceStores = [];
    private GamePlanner AcceptanceCopy(IReadOnlyList<GameEvent> events)
    {
        var copied = new FileGameStore(Path.Combine(root, Guid.NewGuid().ToString("N")));
        var reader = new GamePlanner(copied, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());
        Assert.Equal(AppendStatus.Committed, copied.Append(Scope, "acceptance copy", 0, events, reader.Replay).Status);
        Assert.Equal(AppendStatus.Replay, copied.Append(Scope, "acceptance copy", 0, events, reader.Replay).Status);
        acceptanceStores.Add(reader, copied);
        return reader;
    }

    [Theory]
    [MemberData(nameof(TurnGuideAcceptanceCases.Profiles), MemberType = typeof(TurnGuideAcceptanceCases))]
    public async Task AcceptanceProfilesReplayDcLifecycleWithoutPromotingPendingRules(string profile)
    {
        await PlacedDcRouteGame();
        var missingAt = Revision;
        var missing = await DcGuide(profile: profile);
        TurnGuideAcceptanceCases.AssertProfile(missing, profile);
        Assert.Empty(missing.Actions);
        var enemyBefore = await DcGuide(Perspective.Side("russian"), profile: profile);
        await ReviewDcRoute();
        var readyAt = Revision;
        var live = await DcGuide(profile: profile);
        TurnGuideAcceptanceCases.AssertProfile(live, profile);
        Assert.Equal(profile == "existing-engine" ? 0 : 1, live.Actions.Length);
        Assert.Equal(TurnGuideAcceptanceCases.Json(enemyBefore), TurnGuideAcceptanceCases.Json(await DcGuide(Perspective.Side("russian"), profile: profile)));
        var snapshots = new Dictionary<string, string>();
        foreach (var viewer in new[] { Perspective.Side("german"), Perspective.Side("russian"), Perspective.Adjudicator })
        {
            var view = await DcGuide(viewer, profile: profile);
            TurnGuideAcceptanceCases.AssertProfile(view, profile);
            snapshots[viewer.Name] = TurnGuideAcceptanceCases.Json(view);
            Assert.Equal(snapshots[viewer.Name], TurnGuideAcceptanceCases.Json(await DcGuide(viewer, profile: profile)));
            if (viewer.Name != "german")
            {
                Assert.Empty(view.Actions);
            }
            var historical = await DcGuide(viewer, readyAt, profile: profile);
            TurnGuideAcceptanceCases.AssertProfile(historical, profile);
        }
        var beforeJournal = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var restored = GameEventReader.Read(Encoding.UTF8.GetBytes(beforeJournal)).Record!;
        Assert.Equal(beforeJournal, GameEventWriter.Write(Scope, restored));
        var copy = AcceptanceCopy(restored.Events);
        foreach (var viewer in new[] { Perspective.Side("german"), Perspective.Side("russian"), Perspective.Adjudicator })
        {
            Assert.Equal(snapshots[viewer.Name], TurnGuideAcceptanceCases.Json(await copy.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile)));
        }
        Assert.Equal(readyAt, Revision);
        JsonElement? prepared = null;
        if (profile != "existing-engine")
        {
            prepared = await Planner().PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), Assert.Single(live.Actions), "matrix-dc");
            Assert.NotNull(prepared);
            Committed(await Commit(Play(Then(3)), GameActions.DetonateDc, prepared.Value));
            var after = Revision;
            Assert.Equal(PlayOutcome.Replay, (await Commit(Play(), GameActions.DetonateDc, prepared.Value)).Outcome);
            Assert.Equal(after, Revision);
        }
        else
        {
            Committed(await Do(GameActions.DetonateDc, Then(3), new
            {
                equipmentId = "dc"
            }));
        }
        var final = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, store.Read(Scope)!))).Record!;
        var finished = AcceptanceCopy(final.Events);
        var prefix = AcceptanceCopy(final.Events.Take((int)readyAt).ToArray());
        foreach (var viewer in new[] { Perspective.Side("german"), Perspective.Side("russian"), Perspective.Adjudicator })
        {
            var now = await DcGuide(viewer, profile: profile);
            TurnGuideAcceptanceCases.AssertProfile(now, profile);
            Assert.Empty(now.Actions);
            Assert.Equal(TurnGuideAcceptanceCases.Json(now), TurnGuideAcceptanceCases.Json(await finished.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile)));
            Assert.Equal(TurnGuideAcceptanceCases.Json(await prefix.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile, readyAt)),
                TurnGuideAcceptanceCases.Json(await DcGuide(viewer, readyAt, profile: profile)));
            var old = await DcGuide(viewer, missingAt, profile: profile);
            TurnGuideAcceptanceCases.AssertProfile(old, profile);
            if (viewer.Name != "russian")
            {
                Assert.Equal(TurnObligationState.Resolved, Assert.Single(now.Obligations).State);
                Assert.Equal(profile == "existing-engine" ? TurnSupport.Pending : TurnSupport.IntegratedSubset, now.Obligations[0].CommandSupport);
                Assert.Equal(TurnObligationState.Due, Assert.Single(old.Obligations).State);
            }
            else
            {
                Assert.Empty(now.Obligations);
            }
        }
        if (prepared is { } arguments)
        {
            Assert.Equal(GamePlanStatus.Replay, (await finished.PlanAsync(GameActions.DetonateDc, arguments, Tenant)).Status);
        }
        Assert.Equal(final.Events.Count, Revision);
    }

    [Theory]
    [MemberData(nameof(TurnGuideAcceptanceCases.Profiles), MemberType = typeof(TurnGuideAcceptanceCases))]
    public async Task AcceptanceProfilesRetainLegacyGapsAndWithholdPrivateEvidence(string profile)
    {
        await PlacedDcRouteGame();
        var events = store.Read(Scope)!.Events;
        var legacy = events.Select(item => item.Payload is MovementStepped { DcPlacement: { } dc } step ? item with
        {
            Payload = step with
            {
                DcPlacement = dc with
                {
                    OwnerSide = null
                }
            }
        } : item).ToArray();
        var reader = AcceptanceCopy(legacy);
        foreach (var viewer in new[] { Perspective.Side("german"), Perspective.Side("russian"), Perspective.Adjudicator })
        {
            var guide = await reader.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile);
            TurnGuideAcceptanceCases.AssertProfile(guide, profile);
            Assert.Empty(guide.Actions);
            if (viewer.Name != "russian")
            {
                Assert.Equal(TurnObligationState.NotEvaluated, Assert.Single(guide.Obligations).State);
                Assert.Equal(TurnSupport.Pending, guide.Obligations[0].CommandSupport);
            }
        }
        await ReviewDcRoute();
        var reviewed = store.Read(Scope)!.Events;
        foreach (var hide in new[] { "origin", "review", "boundary" })
        {
            var hidden = reviewed.Select(item => (hide == "origin" && item.Payload is MovementStepped { DcPlacement: not null }
                || hide == "review" && item.Payload is PlacedDcWindowReviewed
                || hide == "boundary" && item.EventId == Current.TurnPhaseOccurrence!.EventId)
                ? item with
                {
                    Visibility = ["russian"]
                } : item).ToArray();
            var withheld = AcceptanceCopy(hidden);
            var own = await withheld.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", profile);
            TurnGuideAcceptanceCases.AssertProfile(own, profile);
            Assert.Empty(own.Actions);
            Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, own.PhaseClose.Disposition);
            Assert.Empty(own.PhaseClose.AutomaticEffects);
            Assert.False(own.PhaseClose.RequiresRoll);
            var referee = await withheld.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", profile);
            if (profile != "existing-engine")
            {
                Assert.Equal(TurnDecisionTiming.ReadyForCommandAssessment, Assert.Single(referee.DecisionWindows).Timing);
            }
        }
        // Two different private adjudications produce exactly the same opposing-side response.
        var changedPrivate = reviewed.Select(item => item.Payload is PlacedDcWindowReviewed record ? item with
        {
            Payload = record with
            {
                Review = record.Review with
                {
                    RuleBasis = "private ruling",
                    Opening = record.Review.Opening with
                    {
                        Wp = TurnOpeningEvidence.Unknown
                    }
                }
            }
        } : item).ToArray();
        var other = AcceptanceCopy(changedPrivate);
        Assert.Equal(TurnGuideAcceptanceCases.Json(await DcGuide(Perspective.Side("russian"), profile: profile)),
            TurnGuideAcceptanceCases.Json(await other.ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "afph", profile)));
        var publicError = await Assert.ThrowsAsync<ArgumentException>(() => Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "bad-phase", profile));
        var privateError = await Assert.ThrowsAsync<ArgumentException>(() => other.ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "bad-phase", profile));
        Assert.Equal(publicError.Message, privateError.Message);
    }
    [Theory]
    [MemberData(nameof(TurnGuideAcceptanceCases.Profiles), MemberType = typeof(TurnGuideAcceptanceCases))]
    public async Task AcceptancePrivateChoiceCannotChangePassiveOwnerRouteDisclosure(string profile)
    {
        await PlacedDcRouteGame();
        await ReviewDcRoute();
        var events = store.Read(Scope)!.Events;
        var review = events[^1];
        var prefix = events.Take(events.Count - 1).ToArray();
        var privateSlot = review with
        {
            EventId = "private-event",
            Type = "conditions-changed",
            Payload = new ConditionsChanged("r1", new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.False }),
            Visibility = ["russian"]
        };
        var privateChoice = privateSlot with
        {
            Type = "choice-pending",
            Payload = new ChoicePending("private-choice", ChoicePending.BattleHardening, "russian", ["decline"], JsonSerializer.SerializeToElement(new
            {
            }))
        };
        var sameReview = review with
        {
            Revision = review.Revision + 1
        };
        var clear = AcceptanceCopy([.. prefix, privateSlot, sameReview]);
        var waiting = AcceptanceCopy([.. prefix, privateChoice, sameReview]);
        var ownClear = await clear.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", profile);
        var ownWaiting = await waiting.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", profile);
        Assert.Equal(TurnGuideAcceptanceCases.Json(ownClear), TurnGuideAcceptanceCases.Json(ownWaiting));
        if (profile != "existing-engine")
        {
            var route = Assert.Single(ownWaiting.Actions);
            var args = (await waiting.PreparePlacedDcDecisionAsync(Scope, Perspective.Side("german"), route, "explicit-proposal"))!.Value;
            Assert.Equal(GamePlanStatus.Refused, (await waiting.PlanAsync(GameActions.DetonateDc, args, Tenant)).Status);
            Assert.Equal(GamePlanStatus.Ready, (await clear.PlanAsync(GameActions.DetonateDc, args, Tenant)).Status);
            var before = acceptanceStores[waiting].Read(Scope)!.Events.Count;
            var blockedPlay = new GamePlay(waiting, acceptanceStores[waiting], new NullAudit(), roller: NoRoll());
            Assert.Equal(PlayOutcome.Denied, (await blockedPlay.ProposeAsync(GameActions.DetonateDc, args, Player)).Outcome);
            Assert.Equal(before, acceptanceStores[waiting].Read(Scope)!.Events.Count);
        }
        Assert.Equal(TurnCloseDisposition.Blocked, (await waiting.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", profile)).PhaseClose.Disposition);
    }

}
