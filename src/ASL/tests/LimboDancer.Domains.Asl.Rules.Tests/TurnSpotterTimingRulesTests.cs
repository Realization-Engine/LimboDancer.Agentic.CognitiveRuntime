using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnSpotterTimingRulesTests
{
    private static TurnRulePosition At(int playerTurn = 1, string side = "a", string phase = "pfph", TurnCheckpoint checkpoint = TurnCheckpoint.DuringPhase) =>
        new((playerTurn + 1) / 2, playerTurn, side, phase, checkpoint);
    private static TurnSpotterTimingFacts Initial => new("a", At(), false, TurnSpotterChange.None);

    [Theory]
    [InlineData("a", "pfph", true)]
    [InlineData("b", "pfph", false)]
    [InlineData("a", "dfph", false)]
    [InlineData("b", "dfph", true)]
    [InlineData("a", "mph", false)]
    [InlineData("a", "afph", false)]
    public void OriginalDesignationUsesTheOwningSidesFireWindow(string phasing, string phase, bool allowed)
    {
        var verdict = TurnSpotterTimingRules.Evaluate(Initial with
        {
            Now = At(side: phasing, phase: phase)
        });
        Assert.Equal(allowed ? TurnSpotterTimingState.InitialDesignationWindow : TurnSpotterTimingState.OutsideDesignationWindow, verdict.State);
        Assert.Contains("C9.3", verdict.Reference, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TurnSpotterChange.Eliminated)]
    [InlineData(TurnSpotterChange.Broken)]
    [InlineData(TurnSpotterChange.Captured)]
    public void ReplacementNeedsLossAndTheFollowingOwnersMovementOpening(TurnSpotterChange change)
    {
        var facts = new TurnSpotterTimingFacts("a", At(phase: "mph", checkpoint: TurnCheckpoint.MovementStart), true, change, At());
        Assert.Equal(TurnSpotterTimingState.ReplacementWindow, TurnSpotterTimingRules.Evaluate(facts).State);
        Assert.Equal(TurnSpotterTimingState.OutsideDesignationWindow, TurnSpotterTimingRules.Evaluate(facts with
        {
            Now = At(phase: "mph")
        }).State);
        Assert.Equal(TurnSpotterTimingState.OutsideDesignationWindow, TurnSpotterTimingRules.Evaluate(facts with
        {
            Now = At(2, "b", "mph", TurnCheckpoint.MovementStart)
        }).State);
        Assert.Equal(TurnSpotterTimingState.OutsideDesignationWindow, TurnSpotterTimingRules.Evaluate(facts with
        {
            Now = At(3, phase: "pfph")
        }).State);
        Assert.Equal(TurnSpotterTimingState.ReplacementWindow, TurnSpotterTimingRules.Evaluate(facts with
        {
            Now = At(3, phase: "mph", checkpoint: TurnCheckpoint.MovementStart)
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(facts with
        {
            LossAt = null
        }).State);
    }

    [Fact]
    public void LossDuringMovementCannotUseThatSameMovementOpening()
    {
        var opening = At(phase: "mph", checkpoint: TurnCheckpoint.MovementStart);
        var facts = new TurnSpotterTimingFacts("a", opening, true, TurnSpotterChange.Broken, opening);
        Assert.Equal(TurnSpotterTimingState.OutsideDesignationWindow, TurnSpotterTimingRules.Evaluate(facts).State);
        Assert.Equal(TurnSpotterTimingState.ReplacementWindow, TurnSpotterTimingRules.Evaluate(facts with
        {
            Now = At(3, phase: "mph", checkpoint: TurnCheckpoint.MovementStart)
        }).State);
        Assert.Equal(TurnSpotterTimingState.ReplacementWindow, TurnSpotterTimingRules.Evaluate(facts with
        {
            LossAt = At(2, "b", "ccph"),
            Now = At(3, phase: "mph", checkpoint: TurnCheckpoint.MovementStart)
        }).State);
    }

    [Fact]
    public void ReductionRetainsOnlyTheProvenGoodOrderSuccessorAndDoesNotInventALoss()
    {
        var facts = Initial with
        {
            HasDesignation = true,
            Change = TurnSpotterChange.SquadReduced,
            GoodOrderReductionSurvivor = true
        };
        Assert.Equal(TurnSpotterTimingState.Retained, TurnSpotterTimingRules.Evaluate(facts).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(facts with
        {
            GoodOrderReductionSurvivor = null
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(facts with
        {
            GoodOrderReductionSurvivor = false
        }).State);
        Assert.Equal(TurnSpotterTimingState.Retained, TurnSpotterTimingRules.Evaluate(Initial with
        {
            HasDesignation = true
        }).State);
    }

    [Fact]
    public void UnknownAndInconsistentOriginsNeverOpenAWindow()
    {
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(Initial with
        {
            HasDesignation = null
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(Initial with
        {
            Change = TurnSpotterChange.Unknown
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(Initial with
        {
            Change = TurnSpotterChange.Broken
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(Initial with
        {
            LossAt = At()
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(Initial with
        {
            GoodOrderReductionSurvivor = true
        }).State);
        Assert.Equal(TurnSpotterTimingState.NotEvaluated, TurnSpotterTimingRules.Evaluate(Initial with
        {
            HasDesignation = true,
            LossAt = At()
        }).State);
    }

    [Theory]
    [InlineData(false, true, true, true, TurnSpotterChange.None)]
    [InlineData(true, true, true, true, TurnSpotterChange.Eliminated)]
    [InlineData(true, false, true, true, TurnSpotterChange.Captured)]
    [InlineData(true, false, true, false, TurnSpotterChange.Broken)]
    [InlineData(true, false, false, false, TurnSpotterChange.None)]
    public void ObservedAssignmentLossDoesNotMistakeLineageConsumptionForElimination(bool active, bool eliminated,
        bool broken, bool captured, TurnSpotterChange expected) =>
        Assert.Equal(expected, TurnSpotterLossRules.ObservedLoss(active, eliminated, broken, captured));

    [Theory]
    [InlineData(true, true, true, true, false, true)]
    [InlineData(false, true, true, true, false, false)]
    [InlineData(true, false, true, true, false, false)]
    [InlineData(true, true, false, true, false, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(true, true, true, true, true, false)]
    public void ReductionRequiresAGoodOrderHalfSquadAndNoPriorLoss(bool reduced, bool halfSquad, bool sameSide,
        bool goodOrder, bool lost, bool retained) =>
        Assert.Equal(retained, TurnSpotterLossRules.RetainsReducedSquad(reduced, halfSquad, sameSide, goodOrder, lost));

    [Fact]
    public void InvalidAndFuturePositionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TurnSpotterTimingRules.Evaluate(null!));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { OwnerSide = " " }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { Change = (TurnSpotterChange)999 }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { LossAt = At(3) }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { LossAt = At() with { GameTurn = 2 } }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { Now = At(3), LossAt = At(3) with { GameTurn = 1 } }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { LossAt = At(side: "b") }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { LossAt = At(phase: "dfph") }));
        Assert.Throws<ArgumentException>(() => TurnSpotterTimingRules.Evaluate(Initial with { Now = At(phase: "missing") }));
    }
}
