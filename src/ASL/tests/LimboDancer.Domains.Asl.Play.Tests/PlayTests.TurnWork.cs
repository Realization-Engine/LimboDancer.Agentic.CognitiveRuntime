using System.Text;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class PlayTests
{
    private static readonly Abstractions.Execution.RuntimePrincipal WorkAdjudicator =
        GamePlay.Principal("work-adjudicator", Tenant, GameActions.WorkAdjudicationPermission);

    private JsonElement WorkArgs(string attempt, string operation, TurnWorkDeclaration? declaration = null) => Args(new
    {
        gameId = "village",
        attemptId = attempt,
        expectedRevision = Current().Revision,
        workId = "work",
        operation,
        declaration,
        ruleBasis = "Adjudicated fixture: E10.3 / G10.3",
        proposedBy = "adjudicator",
    });

    private static async Task<PlayResult> WorkCommit(GamePlay play, JsonElement args)
    {
        var proposed = await play.ProposeAsync(GameActions.RecordTurnWork, args, WorkAdjudicator);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        return await play.ConfirmAsync(GameActions.RecordTurnWork, args, WorkAdjudicator, proposed.Correlation);
    }

    private async Task<GamePlay> WorkGame()
    {
        var play = Play();
        await Commit(play, GameActions.Setup, SetupArgs("work-setup", 0,
            [Unit("g1", "attacker-squad", "german", "bd01:D4:0"), Unit("g2", "attacker-squad", "german", "bd01:D4:0")]));
        return play;
    }

    private async Task WorkPhases(GamePlay play, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var args = Args(new
            {
                gameId = "village",
                attemptId = "work-phase-" + Current().Revision,
                expectedRevision = Current().Revision
            });
            Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.AdvancePhase, args)).Outcome);
        }
    }

    [Fact]
    public async Task TurnWorkPackingPersistsTiAcrossBothCcPhasesAndReleasesInNextFirstRally()
    {
        var play = await WorkGame();
        var declaration = new TurnWorkDeclaration("work", "german", "g1", ["g1", "g2"], TurnWorkKind.AnimalPacking, null, 1, "G10 externally adjudicated duration");
        var args = WorkArgs("pack-start", "start", declaration);
        var denied = await play.ProposeAsync(GameActions.RecordTurnWork, args, Player);
        Assert.NotEqual(PlayOutcome.NeedsConfirmation, denied.Outcome);
        Assert.Empty(Current().TurnWork);
        Assert.Equal(PlayOutcome.Committed, (await WorkCommit(play, args)).Outcome);
        Assert.Equal(ConditionState.True, GameState.Condition(Current().Unit("g1")!, "asl:ti"));
        var startRevision = Current().Revision;
        await WorkPhases(play, 8);
        Assert.Equal(0, Assert.Single(Current().TurnWork).CompletedGameTurns);
        Assert.Equal(ConditionState.True, GameState.Condition(Current().Unit("g1")!, "asl:ti"));
        await WorkPhases(play, 8);
        Assert.Equal(1, Assert.Single(Current().TurnWork).CompletedGameTurns);
        var guide = await Planner().ReadTurnGuideProfileAsync(Current().Scope, Perspective.Side("german"), "rph", "existing-engine-work-v1");
        Assert.True(Assert.Single(guide.Work).CompletionDue);
        Assert.Equal(TurnSupport.Pending, guide.Work[0].RuleAutomation);
        var assessment = await Planner().AssessTurnPhaseCloseAsync(Current().Stamp, Perspective.Adjudicator, []);
        Assert.Equal(TurnCloseDisposition.Blocked, assessment.Disposition);
        var complete = WorkArgs("pack-complete", "complete");
        Assert.Equal(PlayOutcome.Committed, (await WorkCommit(play, complete)).Outcome);
        Assert.Equal(PlayOutcome.Replay, (await WorkCommit(play, complete)).Outcome);
        Assert.True(Assert.Single(Current().TurnWork).Completed);
        Assert.Equal(ConditionState.False, GameState.Condition(Current().Unit("g1")!, "asl:ti"));
        var record = store.Read(Current().Scope)!;
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Current().Scope, record)));
        Assert.False(read.HasErrors);
        var reloaded = Planner().Replay(read.Record!.Events);
        Assert.True(Assert.Single(reloaded.Current!.TurnWork).Completed);
        var historical = await Planner().ReadTurnGuideProfileAsync(Current().Scope, Perspective.Side("german"), "rph", "existing-engine-work-v1", startRevision);
        Assert.False(Assert.Single(historical.Work).Completed);
        var opponent = await Planner().ReadTurnGuideProfileAsync(Current().Scope, Perspective.Side("russian"), "rph", "existing-engine-work-v1");
        Assert.Empty(opponent.Work);
        await WorkPhases(play, 1);
    }

    [Fact]
    public async Task TurnWorkReplenishmentCompletesOnlyInSecondCcWithSystemRoll()
    {
        var play = await WorkGame();
        var declaration = new TurnWorkDeclaration("work", "german", "g1", ["g1", "g2"], TurnWorkKind.AmmoReplenishment, 9, null, "E10.3 external eligibility");
        await WorkCommit(play, WorkArgs("ammo-start", "start", declaration));
        var early = await Planner().PlanAsync(GameActions.RecordTurnWork, WorkArgs("early", "complete"), Tenant);
        Assert.Equal(GamePlanStatus.Refused, early.Status);
        await WorkPhases(play, 15);
        var plan = await Planner().PlanAsync(GameActions.RecordTurnWork, WorkArgs("ammo-complete", "complete"), Tenant);
        Assert.NotNull(plan.Roll);
        Assert.Empty(plan.Events);
        var before = Current().Revision;
        Assert.Equal(PlayOutcome.Committed, (await WorkCommit(play, WorkArgs("ammo-complete", "complete"))).Outcome);
        Assert.True(Assert.Single(Current().TurnWork).Completed);
        var roll = Assert.Single(store.Read(Current().Scope)!.Events.Where(e => e.Revision > before).Select(e => e.Payload).OfType<DiceRolled>());
        Assert.Equal("work-ammunition", roll.Purpose);
        Assert.Equal(roll.Values.Sum() == 12, Current().TurnWork[0].SupplyDepleted);
        var scope = Current().Scope;
        var record = store.Read(scope)!;
        var restricted = record with
        {
            Events = [.. record.Events.Select(e => e.Payload is DiceRolled { Purpose: "work-ammunition" } ? e with { Visibility = ["adjudicator"] } : e)],
        };
        File.WriteAllText(Path.Combine(root, Tenant.ToString("N"), "village.game.json"), GameEventWriter.Write(scope, restricted));
        var owner = await Planner().ReadTurnGuideProfileAsync(scope, Perspective.Side("german"), "ccph", "existing-engine-work-v1");
        Assert.Empty(owner.Work);
        var arbiter = await Planner().ReadTurnGuideProfileAsync(scope, Perspective.Adjudicator, "ccph", "existing-engine-work-v1");
        Assert.True(Assert.Single(arbiter.Work).Completed);
        await WorkPhases(play, 1);
    }

    [Fact]
    public async Task TurnWorkInterruptResumeAndInvalidDeclarationsDoNotInventProgress()
    {
        var play = await WorkGame();
        var declaration = new TurnWorkDeclaration("work", "german", "g1", ["g1", "g2"], TurnWorkKind.AnimalPacking, null, null, "G10 unresolved");
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.RecordTurnWork, WorkArgs("unknown", "start", declaration), Tenant)).Status);
        await WorkCommit(play, WorkArgs("start", "start", declaration with
        {
            PackingGameTurns = 2
        }));
        await WorkPhases(play, 16);
        await WorkCommit(play, WorkArgs("interrupt", "interrupt"));
        Assert.Equal(1, Current().TurnWork[0].CompletedGameTurns);
        Assert.False(Current().TurnWork[0].Working);
        await WorkPhases(play, 16);
        Assert.Equal(1, Current().TurnWork[0].CompletedGameTurns);
        await WorkCommit(play, WorkArgs("resume", "resume"));
        Assert.True(Current().TurnWork[0].Working);
        await WorkCommit(play, WorkArgs("cancel", "cancel"));
        Assert.True(Current().TurnWork[0].Cancelled);
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.RecordTurnWork, WorkArgs("late", "complete"), Tenant)).Status);
    }

    [Fact]
    public async Task TurnWorkReplayRejectsSkippedBoundariesAndRetainsPriorProgressOnTiLoss()
    {
        var play = await WorkGame();
        var declaration = new TurnWorkDeclaration("work", "german", "g1", ["g1", "g2"], TurnWorkKind.AnimalPacking, null, 2, "G10 adjudication");
        await WorkCommit(play, WorkArgs("start", "start", declaration));
        var events = store.Read(Current().Scope)!.Events;
        GameEvent Add(EventPayload payload, string type) => events[^1] with
        {
            EventId = "test-change",
            Revision = events.Count + 1,
            Type = type,
            Payload = payload,
        };
        var jumped = Planner().Replay([.. events, Add(new PhaseChanged(1, "ccph", "russian"), "phase-changed")]);
        Assert.Contains(jumped.Diagnostics, item => item.Code == "UNIT-WORK-002");
        var duplicate = Planner().Replay([.. events, Add(new TurnWorkRecorded("work", "start", declaration, null, "duplicate"), "turn-work-recorded")]);
        Assert.True(duplicate.HasErrors);
        await WorkPhases(play, 16);
        events = store.Read(Current().Scope)!.Events;
        var revealed = Planner().Replay([.. events, Add(new ConditionsChanged("g1", new Dictionary<string, ConditionState>
        { [Conditions.Concealed] = ConditionState.False }), "conditions-changed")]);
        Assert.False(revealed.HasErrors);
        Assert.True(revealed.Current!.TurnWork[0].Working);
        var interrupted = Planner().Replay([.. events, Add(new ConditionsChanged("g1", new Dictionary<string, ConditionState>
        { ["asl:ti"] = ConditionState.False }), "conditions-changed")]);
        Assert.False(interrupted.HasErrors);
        Assert.False(interrupted.Current!.TurnWork[0].Working);
        Assert.Equal(1, interrupted.Current.TurnWork[0].CompletedGameTurns);
        Assert.Equal(ConditionState.False, GameState.Condition(interrupted.Current.Unit("g2")!, "asl:ti"));
        var interruptionEvent = Add(new ConditionsChanged("g1", new Dictionary<string, ConditionState>
        {
            ["asl:ti"] = ConditionState.False
        }), "conditions-changed");
        var unrelatedTi = interruptionEvent with
        {
            EventId = "other-ti",
            Revision = interruptionEvent.Revision + 1,
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { ["asl:ti"] = ConditionState.True }),
        };
        var cancellation = unrelatedTi with
        {
            EventId = "cancel-work",
            Revision = unrelatedTi.Revision + 1,
            Type = TurnWorkRecorded.AuditedType,
            Payload = new TurnWorkRecorded("work", "cancel", null, null, "Cancel interrupted work"),
        };
        var cancelled = Planner().Replay([.. events, interruptionEvent, unrelatedTi, cancellation]);
        Assert.False(cancelled.HasErrors);
        Assert.True(cancelled.Current!.TurnWork[0].Cancelled);
        Assert.Equal(ConditionState.True, GameState.Condition(cancelled.Current.Unit("g1")!, "asl:ti"));
    }
}
