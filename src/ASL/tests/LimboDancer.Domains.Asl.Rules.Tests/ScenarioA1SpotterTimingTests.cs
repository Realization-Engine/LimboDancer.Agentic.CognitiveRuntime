using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>C9.3's designation timing, retention, and succession, as <see cref="ScenarioA1SpotterCalculator"/> decides them (the C9.3 claim, ruling RC93.1).</summary>
public sealed class ScenarioA1SpotterTimingTests
{
    private static TurnRulePosition At(int playerTurn = 1, string side = "a", string phase = "pfph", TurnCheckpoint checkpoint = TurnCheckpoint.DuringPhase) =>
        new((playerTurn + 1) / 2, playerTurn, side, phase, checkpoint);
    private static SpotterTimingFacts Initial => new("a", At(), false, SpotterChange.None);

    [Theory]
    [InlineData("a", "pfph", true)]
    [InlineData("b", "pfph", false)]
    [InlineData("a", "dfph", false)]
    [InlineData("b", "dfph", true)]
    [InlineData("a", "mph", false)]
    [InlineData("a", "afph", false)]
    public void OriginalDesignationUsesTheOwningSidesFireWindow(string phasing, string phase, bool allowed)
    {
        var verdict = ScenarioA1SpotterCalculator.Timing(Initial with
        {
            Now = At(side: phasing, phase: phase)
        });
        Assert.Equal(allowed ? SpotterTimingState.InitialDesignationWindow : SpotterTimingState.OutsideDesignationWindow, verdict.State);
        Assert.Contains("C9.3", verdict.Reference, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SpotterChange.Eliminated)]
    [InlineData(SpotterChange.Broken)]
    [InlineData(SpotterChange.Captured)]
    public void ReplacementNeedsLossAndTheFollowingOwnersMovementOpening(SpotterChange change)
    {
        var facts = new SpotterTimingFacts("a", At(phase: "mph", checkpoint: TurnCheckpoint.MovementStart), true, change, At());
        Assert.Equal(SpotterTimingState.ReplacementWindow, ScenarioA1SpotterCalculator.Timing(facts).State);
        Assert.Equal(SpotterTimingState.OutsideDesignationWindow, ScenarioA1SpotterCalculator.Timing(facts with
        {
            Now = At(phase: "mph")
        }).State);
        Assert.Equal(SpotterTimingState.OutsideDesignationWindow, ScenarioA1SpotterCalculator.Timing(facts with
        {
            Now = At(2, "b", "mph", TurnCheckpoint.MovementStart)
        }).State);
        Assert.Equal(SpotterTimingState.OutsideDesignationWindow, ScenarioA1SpotterCalculator.Timing(facts with
        {
            Now = At(3, phase: "pfph")
        }).State);
        Assert.Equal(SpotterTimingState.ReplacementWindow, ScenarioA1SpotterCalculator.Timing(facts with
        {
            Now = At(3, phase: "mph", checkpoint: TurnCheckpoint.MovementStart)
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(facts with
        {
            LossAt = null
        }).State);
    }

    [Fact]
    public void LossDuringMovementCannotUseThatSameMovementOpening()
    {
        var opening = At(phase: "mph", checkpoint: TurnCheckpoint.MovementStart);
        var facts = new SpotterTimingFacts("a", opening, true, SpotterChange.Broken, opening);
        Assert.Equal(SpotterTimingState.OutsideDesignationWindow, ScenarioA1SpotterCalculator.Timing(facts).State);
        Assert.Equal(SpotterTimingState.ReplacementWindow, ScenarioA1SpotterCalculator.Timing(facts with
        {
            Now = At(3, phase: "mph", checkpoint: TurnCheckpoint.MovementStart)
        }).State);
        Assert.Equal(SpotterTimingState.ReplacementWindow, ScenarioA1SpotterCalculator.Timing(facts with
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
            Change = SpotterChange.SquadReduced,
            GoodOrderReductionSurvivor = true
        };
        Assert.Equal(SpotterTimingState.Retained, ScenarioA1SpotterCalculator.Timing(facts).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(facts with
        {
            GoodOrderReductionSurvivor = null
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(facts with
        {
            GoodOrderReductionSurvivor = false
        }).State);
        Assert.Equal(SpotterTimingState.Retained, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            HasDesignation = true
        }).State);
    }

    [Fact]
    public void RecordedLossRequiresReplacementBeforeFurtherSpottedFire()
    {
        Assert.Null(ScenarioA1SpotterCalculator.FireBar(false));
        Assert.Contains("replacement", ScenarioA1SpotterCalculator.FireBar(true)!, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownAndInconsistentOriginsNeverOpenAWindow()
    {
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            HasDesignation = null
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            Change = SpotterChange.Unknown
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            Change = SpotterChange.Broken
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            LossAt = At()
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            GoodOrderReductionSurvivor = true
        }).State);
        Assert.Equal(SpotterTimingState.NotEvaluated, ScenarioA1SpotterCalculator.Timing(Initial with
        {
            HasDesignation = true,
            LossAt = At()
        }).State);
    }

    [Theory]
    [InlineData(false, true, true, true, SpotterChange.None)]
    [InlineData(true, true, true, true, SpotterChange.Eliminated)]
    [InlineData(true, false, true, true, SpotterChange.Captured)]
    [InlineData(true, false, true, false, SpotterChange.Broken)]
    [InlineData(true, false, false, false, SpotterChange.None)]
    public void ObservedAssignmentLossDoesNotMistakeLineageConsumptionForElimination(bool active, bool eliminated,
        bool broken, bool captured, SpotterChange expected) =>
        Assert.Equal(expected, ScenarioA1SpotterCalculator.ObservedLoss(active, eliminated, broken, captured));

    [Theory]
    [InlineData(true, true, true, true, false, true)]
    [InlineData(false, true, true, true, false, false)]
    [InlineData(true, false, true, true, false, false)]
    [InlineData(true, true, false, true, false, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(true, true, true, true, true, false)]
    public void ReductionRequiresAGoodOrderHalfSquadAndNoPriorLoss(bool reduced, bool halfSquad, bool sameSide,
        bool goodOrder, bool lost, bool retained) =>
        Assert.Equal(retained, ScenarioA1SpotterCalculator.RetainsReducedSquad(reduced, halfSquad, sameSide, goodOrder, lost));

    [Theory]
    [InlineData(true, null, true)]
    [InlineData(true, "first", false)]
    [InlineData(true, "second", false)]
    [InlineData(true, "third", true)]
    [InlineData(false, null, false)]
    [InlineData(false, "first", true)]
    public void DeploymentRequiresAChoiceOnlyForAnExistingAssignment(bool assigned, string? choice, bool refused) =>
        Assert.Equal(refused, ScenarioA1SpotterCalculator.DeploymentChoiceRefusal(assigned, choice) is not null);

    [Theory]
    [InlineData(true, true, true, true, false, true)]
    [InlineData(false, true, true, true, false, false)]
    [InlineData(true, false, true, true, false, false)]
    [InlineData(true, true, false, true, false, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(true, true, true, true, true, false)]
    public void DeploymentRetentionRequiresAChosenGoodOrderHalfSquadWithoutPriorLoss(bool deployed, bool chosen, bool sameSide,
        bool goodOrder, bool lost, bool retained) =>
        Assert.Equal(retained, ScenarioA1SpotterCalculator.RetainsDeployedSquad(deployed, chosen, sameSide, goodOrder, lost));

    [Fact]
    public void InvalidAndFuturePositionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => ScenarioA1SpotterCalculator.Timing(null!));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { OwnerSide = " " }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { Change = (SpotterChange)999 }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { LossAt = At(3) }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { LossAt = At() with { GameTurn = 2 } }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { Now = At(3), LossAt = At(3) with { GameTurn = 1 } }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { LossAt = At(side: "b") }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { LossAt = At(phase: "dfph") }));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.Timing(Initial with { Now = At(phase: "missing") }));
    }
}
