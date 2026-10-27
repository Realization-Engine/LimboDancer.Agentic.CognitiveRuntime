using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnObligationRulesTests
{
    private static TurnRulePosition Position(string phase, TurnCheckpoint checkpoint = TurnCheckpoint.DuringPhase, int turn = 1, string side = "a", int gameTurn = 1) => new(gameTurn, turn, side, phase, checkpoint);
    private static TurnObligationRuleFacts Facts(TurnObligationFamily family, TurnRulePosition now, string originPhase = "mph") =>
        new(family, Position(originPhase), now, "a", TurnRecordedOutcome.None, true, true, false, false, true, false, false, true);

    [Theory]
    [InlineData(TurnCheckpoint.ClearanceResolution, TurnObligationState.Scheduled)]
    [InlineData(TurnCheckpoint.OrdinaryStatusCleanup, TurnObligationState.Due)]
    [InlineData(TurnCheckpoint.ConcealmentComplete, TurnObligationState.Due)]
    [InlineData(TurnCheckpoint.PlayerTurnEnd, TurnObligationState.Due)]
    public void StunWaitsForItsOwnCleanupWindow(TurnCheckpoint checkpoint, TurnObligationState expected)
    {
        Assert.Equal(expected, TurnObligationRules.Evaluate(Facts(TurnObligationFamily.Stun, Position("ccph", checkpoint))).State);
    }

    [Fact]
    public void StunIsOverdueAtNextRallyAndNeverResolvedFromMarkerAbsence()
    {
        var facts = Facts(TurnObligationFamily.Stun, Position("rph", turn: 2, side: "b"));
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            Stunned = false
        }).State);
        Assert.Equal(TurnObligationState.Resolved, TurnObligationRules.Evaluate(facts with
        {
            Stunned = false,
            Outcome = TurnRecordedOutcome.Resolved
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            Active = false
        }).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Recalled = true
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            Recalled = true,
            StunRecovery = true
        }).State);
    }

    [Fact]
    public void CxUsesPermanentOwnerAndKeepsThePreviousCxHistory()
    {
        var opponent = Facts(TurnObligationFamily.Cx, Position("mph", TurnCheckpoint.MovementStart, 2, "b"), "aph");
        Assert.Equal(TurnObligationState.Scheduled, TurnObligationRules.Evaluate(opponent).State);
        var friendly = opponent with
        {
            Now = Position("mph", TurnCheckpoint.MovementStart, 3, "a", 2)
        };
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(friendly).State);
        Assert.Equal(TurnObligationState.Scheduled, TurnObligationRules.Evaluate(friendly with
        {
            Now = Position("pfph", turn: 3, gameTurn: 2)
        }).State);
        foreach (var checkpoint in new[] { TurnCheckpoint.PrepFireComplete, TurnCheckpoint.OpportunityDesignation, TurnCheckpoint.BecomesTi })
        {
            Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(friendly with
            {
                Now = Position("pfph", checkpoint, 3, gameTurn: 2)
            }).State);
        }

        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(friendly with
        {
            Now = Position("dfph", turn: 3, gameTurn: 2)
        }).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(opponent with
        {
            Broken = true
        }).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(opponent with
        {
            Berserk = true
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(opponent with
        {
            Cx = false
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(opponent with
        {
            Active = false
        }).State);
    }

    [Fact]
    public void MovementFlareSurvivesOrdinaryCleanupUntilConcealment()
    {
        var facts = Facts(TurnObligationFamily.TripFlare, Position("ccph", TurnCheckpoint.OrdinaryStatusCleanup));
        Assert.Equal(TurnObligationState.Scheduled, TurnObligationRules.Evaluate(facts).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Now = Position("ccph", TurnCheckpoint.ConcealmentComplete)
        }).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Now = Position("rph", turn: 2, side: "b")
        }).State);
    }

    [Theory]
    [InlineData("rtph")]
    [InlineData("aph")]
    [InlineData("ccph")]
    public void LateFlareSurvivesToNextPlayerTurnsAdvancingFire(string origin)
    {
        var facts = Facts(TurnObligationFamily.TripFlare, Position("ccph", TurnCheckpoint.PlayerTurnEnd), origin);
        Assert.Equal(TurnObligationState.Scheduled, TurnObligationRules.Evaluate(facts).State);
        foreach (var phase in new[] { "rph", "pfph", "mph", "dfph", "afph" })
        {
            Assert.Equal(TurnObligationState.Scheduled, TurnObligationRules.Evaluate(facts with
            {
                Now = Position(phase, turn: 2, side: "b")
            }).State);
        }

        var due = TurnObligationRules.Evaluate(facts with
        {
            Now = Position("afph", TurnCheckpoint.AdvancingFireEnd, 2, "b")
        });
        Assert.Equal(TurnObligationState.Due, due.State);
        Assert.Equal(2, due.Deadline!.EarliestPlayerTurn);
        Assert.Equal(TurnRuleDeadlineKind.NextAdvancingFireEnd, due.Deadline.Kind);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Now = Position("rtph", turn: 2, side: "b")
        }).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Now = Position("rph", turn: 3, gameTurn: 2)
        }).State);
    }

    [Fact]
    public void ClearanceChecksEligibilityBeforeStatusCleanupAndRequiresEvidence()
    {
        var facts = Facts(TurnObligationFamily.NonFlameClearance, Position("ccph"), "dfph");
        Assert.Equal(TurnObligationState.Scheduled, TurnObligationRules.Evaluate(facts).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Now = Position("ccph", TurnCheckpoint.ClearanceResolution)
        }).State);
        Assert.Equal(TurnObligationState.Due, TurnObligationRules.Evaluate(facts with
        {
            Now = Position("ccph", TurnCheckpoint.OrdinaryStatusCleanup)
        }).State);
        Assert.Equal(TurnObligationState.Interrupted, TurnObligationRules.Evaluate(facts with
        {
            ClearanceEligible = false
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            ClearanceEligible = null
        }).State);
        Assert.Equal(TurnObligationState.Resolved, TurnObligationRules.Evaluate(facts with
        {
            Outcome = TurnRecordedOutcome.Resolved
        }).State);
        Assert.Equal(TurnObligationState.Interrupted, TurnObligationRules.Evaluate(facts with
        {
            Outcome = TurnRecordedOutcome.Interrupted
        }).State);
        Assert.Equal(TurnObligationState.Cancelled, TurnObligationRules.Evaluate(facts with
        {
            Outcome = TurnRecordedOutcome.Cancelled
        }).State);
    }

    [Fact]
    public void UnsupportedOrMissingOriginsRemainExplicitGaps()
    {
        var facts = Facts(TurnObligationFamily.Unsupported, Position("ccph"));
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            Origin = null
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            Family = TurnObligationFamily.TripFlare,
            Origin = Position("rph")
        }).State);
        Assert.Equal(TurnObligationState.NotEvaluated, TurnObligationRules.Evaluate(facts with
        {
            Family = TurnObligationFamily.NonFlameClearance,
            Origin = Position("aph")
        }).State);
    }

    [Fact]
    public void InvalidClocksAndEnumsAreRejected()
    {
        var facts = Facts(TurnObligationFamily.Stun, Position("ccph"));
        Assert.Throws<ArgumentNullException>(() => TurnObligationRules.Evaluate(null!));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.Evaluate(facts with { Family = (TurnObligationFamily)999 }));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.Evaluate(facts with { Outcome = (TurnRecordedOutcome)999 }));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.Evaluate(facts with { Origin = Position("mph", turn: 2) }));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.Evaluate(facts with { Origin = Position("mph", gameTurn: 2) }));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.Evaluate(facts with { Now = Position("rph") }));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.Evaluate(facts with { Now = Position("ccph", side: "b") }));
        Assert.Throws<ArgumentNullException>(() => TurnObligationRules.ValidatePosition(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnObligationRules.ValidatePosition(Position("rph", gameTurn: 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnObligationRules.ValidatePosition(Position("rph", turn: 0)));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.ValidatePosition(Position("unknown")));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.ValidatePosition(Position("rph", (TurnCheckpoint)999)));
        Assert.Throws<ArgumentException>(() => TurnObligationRules.ValidatePosition(Position("rph", side: " ")));
    }
}
