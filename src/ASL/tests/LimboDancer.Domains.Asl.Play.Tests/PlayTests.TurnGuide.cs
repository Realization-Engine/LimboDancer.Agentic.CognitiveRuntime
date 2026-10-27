using System.Text.Json;
using System.Text.Json.Nodes;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class PlayTests
{
    private static readonly string[] GuideRetainDm = ["g1"];
    [Fact]
    public async Task TurnGuideUsesFullCommandChecksWithoutWritingOrRolling()
    {
        await GameInMph();
        var planner = Planner();
        var scope = new GameScope(Tenant, "village");
        var before = JsonSerializer.Serialize(store.Read(scope));
        var state = Current();
        var guide = await planner.ReadTurnGuideAsync(scope, Perspective.Adjudicator, "mph");
        Assert.Equal(114, guide.Guide.Options.Length);
        Assert.Equal(TurnCloseDisposition.Ready, guide.PhaseClose.Disposition);
        Assert.Equal(before, JsonSerializer.Serialize(store.Read(scope)));
        var args = Args(new
        {
            gameId = "village",
            attemptId = "parity",
            expectedRevision = state.Revision,
            proposedBy = "adjudicator"
        });
        var plan = await planner.PlanAsync(GameActions.AdvancePhase, args, Tenant);
        Assert.Equal(GamePlanStatus.Ready, plan.Status);
        Assert.Equal(plan.Reasons, guide.PhaseClose.Reasons);
        Assert.Equal(plan.Events.Select(item => item.Type), guide.PhaseClose.AutomaticEffects);
        Assert.Null(planner.PhaseEndBar(state, store.Read(scope)!.Events, "german"));
        var refused = await planner.AssessTurnPhaseCloseAsync(state.Stamp, Perspective.Adjudicator, ["g1"]);
        var refusedPlan = await planner.PlanAsync(GameActions.AdvancePhase,
            Args(new
            {
                gameId = "village",
                attemptId = "bad-retain",
                expectedRevision = state.Revision,
                retainDm = GuideRetainDm
            }), Tenant);
        Assert.Equal(GamePlanStatus.Refused, refusedPlan.Status);
        Assert.Equal(TurnCloseDisposition.Blocked, refused.Disposition);
        Assert.Equal(refusedPlan.Reasons, refused.Reasons);
        Assert.Equal(before, JsonSerializer.Serialize(store.Read(scope)));
    }

    [Fact]
    public async Task TurnGuideSideReadIsInvariantUnderHiddenUnitAndRevisionChanges()
    {
        var play = Play();
        await Commit(play, GameActions.Setup, SetupArgs("setup-1", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0")]));
        var scope = new GameScope(Tenant, "village");
        var first = await Planner().ReadTurnGuideProfileAsync(scope, Perspective.Side("german"), "rph", "existing-engine-fire-v1");
        var hidden = JsonSerializer.SerializeToNode(Unit("secret-unit", "defender-squad", "russian", "bd01:G4:0", hidden: true))!;
        hidden["conditions"]!["asl:cx"] = true;
        await Commit(play, GameActions.Setup, SetupArgs("setup-hidden", Current().Revision, [hidden], start: false));
        var second = await Planner().ReadTurnGuideProfileAsync(scope, Perspective.Side("german"), "rph", "existing-engine-fire-v1");
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, second.PhaseClose.Disposition);
        Assert.DoesNotContain("secret-unit", JsonSerializer.Serialize(second), StringComparison.Ordinal);
        var adjudicator = await Planner().ReadTurnGuideAsync(scope, Perspective.Adjudicator, "rph");
        Assert.Equal(TurnObligationState.NotEvaluated, Assert.Single(adjudicator.Obligations, item => item.SubjectId == "secret-unit").State);
    }

    [Fact]
    public async Task TurnGuideCxUsesRealHistoryAndExistingCleanupExactlyOnce()
    {
        var play = await GameInMph();
        await Commit(play, GameActions.Move, Args(new
        {
            gameId = "village",
            attemptId = "double-time",
            expectedRevision = Current().Revision,
            unitIds = GuideRetainDm,
            to = "bd01:E4:0",
            doubleTime = true
        }));
        await Commit(play, GameActions.PassFire, Args(new
        {
            gameId = "village",
            attemptId = "pass",
            expectedRevision = Current().Revision
        }));
        await Commit(play, GameActions.EndMove, Args(new
        {
            gameId = "village",
            attemptId = "end-move",
            expectedRevision = Current().Revision
        }));
        var scope = new GameScope(Tenant, "village");
        var originalRevision = Current().Revision;
        var first = await Planner().ReadTurnGuideProfileAsync(scope, Perspective.Side("german"), "rph", "existing-engine-fire-v1");
        Assert.Equal(TurnObligationState.Scheduled, Assert.Single(first.Obligations).State);
        for (var i = 0; i < 16; i++)
        {
            var state = Current();
            var planner = Planner();
            var assessment = await planner.AssessTurnPhaseCloseAsync(state.Stamp, Perspective.Adjudicator, []);
            var plan = await planner.PlanAsync(GameActions.AdvancePhase,
                Args(new
                {
                    gameId = "village",
                    attemptId = "parity-" + i,
                    expectedRevision = state.Revision
                }), Tenant);
            Assert.Equal(GamePlanStatus.Ready, plan.Status);
            Assert.Equal(TurnCloseDisposition.Ready, assessment.Disposition);
            Assert.Equal(plan.Reasons, assessment.Reasons);
            Assert.Equal(plan.Events.Select(item => item.Type), assessment.AutomaticEffects);
            await Commit(play, GameActions.AdvancePhase, Args(new
            {
                gameId = "village",
                attemptId = "phase-" + i,
                expectedRevision = Current().Revision
            }));
        }
        Assert.Equal(ConditionState.False, GameState.Condition(Current().Unit("g1")!, Conditions.Cx));
        var count = store.Read(scope)!.Events.Count;
        var guide = await Planner().ReadTurnGuideAsync(scope, Perspective.Side("german"), "mph");
        Assert.Equal(TurnObligationState.Resolved, Assert.Single(guide.Obligations).State);
        var reloaded = await Planner().ReadTurnGuideAsync(scope, Perspective.Side("german"), "mph");
        Assert.Equal(JsonSerializer.Serialize(guide), JsonSerializer.Serialize(reloaded));
        Assert.Equal(count, store.Read(scope)!.Events.Count);
        var old = await Planner().ReadTurnGuideAsync(scope, Perspective.Side("german"), "rph", originalRevision);
        Assert.Equal(TurnCloseDisposition.ReadOnly, old.PhaseClose.Disposition);
        Assert.Equal(TurnObligationState.Scheduled, Assert.Single(old.Obligations).State);
    }

    [Fact]
    public async Task TurnGuideRejectsWrongPerspectiveStaleMapAndStaleRevision()
    {
        await GameInMph();
        var planner = Planner();
        var state = Current();
        await Assert.ThrowsAsync<ArgumentException>(() => planner.ReadTurnGuideAsync(state.Scope, Perspective.Side("intruder"), "mph"));
        await Assert.ThrowsAsync<ArgumentException>(() => planner.AssessTurnPhaseCloseAsync(state.Stamp, Perspective.Side("intruder"), []));
        await Assert.ThrowsAsync<ArgumentException>(() => planner.ReadTurnGuideAsync(state.Scope with { Game = "missing" }, Perspective.Adjudicator, "mph"));
        Assert.Equal(TurnCloseDisposition.Stale, (await planner.AssessTurnPhaseCloseAsync(state.Stamp with
        {
            Revision = state.Revision - 1
        }, Perspective.Adjudicator, [])).Disposition);
        Assert.Equal(TurnCloseDisposition.Stale, (await planner.AssessTurnPhaseCloseAsync(state.Stamp with
        {
            MapVersion = "old-map"
        }, Perspective.Adjudicator, [])).Disposition);
        Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, (await planner.AssessTurnPhaseCloseAsync(state.Stamp with
        {
            Revision = 1
        }, Perspective.Side("german"), [])).Disposition);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => planner.ReadTurnGuideAsync(state.Scope, Perspective.Adjudicator, "mph", state.Revision + 1));
    }
}
