using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The calculators that took over rule decisions the Turn Guide branch had made in Play, the Units projector and ASL.Turns. Each pins the
/// truth table of the expression it replaced, so the move changed no verdict.
/// </summary>
public sealed class TurnRuleBoundaryTests
{
    [Theory]
    [InlineData("rph", true, TurnWorkWindow.FirstRally)]
    [InlineData("rph", false, TurnWorkWindow.Unknown)]
    [InlineData("ccph", false, TurnWorkWindow.SecondCloseCombat)]
    [InlineData("ccph", true, TurnWorkWindow.Unknown)]
    [InlineData("mph", true, TurnWorkWindow.Unknown)]
    [InlineData("dfph", false, TurnWorkWindow.Unknown)]
    public void WorkWindowIsTheFirstPlayersRallyOrTheSecondPlayersCloseCombat(string phase, bool phasingIsFirst, TurnWorkWindow expected) =>
        Assert.Equal(expected, TurnWorkRules.Window(phase, phasingIsFirst));

    [Fact]
    public void AnyRecordedActivityInterruptsWorkAndNoneKeepsIt()
    {
        var none = new TurnWorkActivity(false, false, false, false, false, false, false, false, false, false);
        Assert.False(TurnWorkRules.Interrupted(none));
        foreach (var one in new[]
        {
            none with { AuditedEligibilityLost = true }, none with { ParticipantGoneOrMoved = true }, none with { TiCleared = true },
            none with { SubjectButtonedUp = true }, none with { Moved = true }, none with { Fired = true }, none with { OrdnanceFired = true },
            none with { CloseCombat = true }, none with { VehicleStepped = true }, none with { PackingCrewLostPackTi = true },
        })
        {
            Assert.True(TurnWorkRules.Interrupted(one));
        }
        Assert.Throws<ArgumentNullException>(() => TurnWorkRules.Interrupted(null!));
    }

    [Theory]
    [InlineData(true, true, true, TurnWorkIntervalOutcome.Interrupted)]
    [InlineData(true, false, false, TurnWorkIntervalOutcome.Interrupted)]
    [InlineData(false, true, true, TurnWorkIntervalOutcome.Complete)]
    [InlineData(false, true, false, TurnWorkIntervalOutcome.Incomplete)]
    [InlineData(false, false, true, TurnWorkIntervalOutcome.Incomplete)]
    [InlineData(false, false, false, TurnWorkIntervalOutcome.Incomplete)]
    public void AGameTurnIntervalCountsOnlyUninterruptedWithBothBoundaries(bool interrupted, bool firstRally, bool secondCloseCombat,
        TurnWorkIntervalOutcome expected) =>
        Assert.Equal(expected, TurnWorkRules.Interval(interrupted, firstRally, secondCloseCombat));

    [Theory]
    [InlineData(TurnWorkKind.AmmoReplenishment, true)]
    [InlineData(TurnWorkKind.AmmoTransfer, false)]
    [InlineData(TurnWorkKind.AnimalPacking, false)]
    public void OnlyReplenishmentTakesAnAmmunitionDr(TurnWorkKind kind, bool expected) =>
        Assert.Equal(expected, TurnWorkLifecycle.RequiresAmmunitionDr(kind));

    private const string InOrder = "Active work requires every phase boundary in order.";
    private const string Boundary = "Resolve due work before closing its release window; work cannot skip Game Turn boundaries.";

    [Fact]
    public void PhaseChangeWithWorkKeepsOrderResolvesDueWorkAndCrossesOnlyIntoTheNextFirstRally()
    {
        // No open work: nothing to refuse, whatever the change.
        Assert.Null(TurnWorkLifecycle.PhaseChangeRefusal(false, false, false, TurnWorkWindow.Unknown, 3, 7, TurnWorkWindow.Unknown));
        // Working, out of order: the order refusal comes first, even when work is also due.
        Assert.Equal(InOrder, TurnWorkLifecycle.PhaseChangeRefusal(true, true, false, TurnWorkWindow.SecondCloseCombat, 3, 4, TurnWorkWindow.FirstRally));
        // Due work blocks closing its window, working or not.
        Assert.Equal(Boundary, TurnWorkLifecycle.PhaseChangeRefusal(false, true, true, TurnWorkWindow.SecondCloseCombat, 3, 3, TurnWorkWindow.Unknown));
        // Working, in order, within the Game Turn.
        Assert.Null(TurnWorkLifecycle.PhaseChangeRefusal(true, false, true, TurnWorkWindow.Unknown, 3, 3, TurnWorkWindow.Unknown));
        // Working, crossing the Game Turn: only from the second CCPh into the next Game Turn's first RPh.
        Assert.Null(TurnWorkLifecycle.PhaseChangeRefusal(true, false, true, TurnWorkWindow.SecondCloseCombat, 3, 4, TurnWorkWindow.FirstRally));
        Assert.Equal(Boundary, TurnWorkLifecycle.PhaseChangeRefusal(true, false, true, TurnWorkWindow.Unknown, 3, 4, TurnWorkWindow.FirstRally));
        Assert.Equal(Boundary, TurnWorkLifecycle.PhaseChangeRefusal(true, false, true, TurnWorkWindow.SecondCloseCombat, 3, 5, TurnWorkWindow.FirstRally));
        Assert.Equal(Boundary, TurnWorkLifecycle.PhaseChangeRefusal(true, false, true, TurnWorkWindow.SecondCloseCombat, 3, 4, TurnWorkWindow.Unknown));
        // Not working: a Game Turn change is not this check's concern.
        Assert.Null(TurnWorkLifecycle.PhaseChangeRefusal(false, false, true, TurnWorkWindow.Unknown, 3, 5, TurnWorkWindow.Unknown));
    }

    [Fact]
    public void ReplenishmentNeedsEveryConditionAndNoPassengers()
    {
        Assert.True(TurnWorkEffects.ReplenishmentReady(true, true, true, true, false));
        Assert.False(TurnWorkEffects.ReplenishmentReady(false, true, true, true, false));
        Assert.False(TurnWorkEffects.ReplenishmentReady(true, false, true, true, false));
        Assert.False(TurnWorkEffects.ReplenishmentReady(true, true, false, true, false));
        Assert.False(TurnWorkEffects.ReplenishmentReady(true, true, true, false, false));
        Assert.False(TurnWorkEffects.ReplenishmentReady(true, true, true, true, true));
    }

    [Fact]
    public void PackingNeedsAnOwnGoodOrderCrewWithTheGunInTheExpectedState()
    {
        // Pack: manned by this crew, not yet packed.
        Assert.True(TurnWorkEffects.PackingReady(TurnWorkEffectKind.PackGun, true, true, true, false, true, true));
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.PackGun, true, true, true, true, true, true));
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.PackGun, true, true, true, false, false, true));
        // Unpack: packed and held by nobody.
        Assert.True(TurnWorkEffects.PackingReady(TurnWorkEffectKind.UnpackGun, true, true, true, true, false, false));
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.UnpackGun, true, true, true, false, false, false));
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.UnpackGun, true, true, true, true, false, true));
        // Every kind needs an own Good Order crew in the Gun's Location.
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.PackGun, false, true, true, false, true, true));
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.PackGun, true, false, true, false, true, true));
        Assert.False(TurnWorkEffects.PackingReady(TurnWorkEffectKind.UnpackGun, true, true, false, true, false, false));
    }

    [Fact]
    public void SupplyRelocationNeedsABoundSupplyOnAnEmptyImmobileSourceAndAFreeOwnDestination()
    {
        Assert.True(TurnWorkEffects.RelocationSourceReady(true, true, false, false, false));
        Assert.True(TurnWorkEffects.RelocationSourceReady(true, false, true, false, false));
        Assert.False(TurnWorkEffects.RelocationSourceReady(true, false, false, false, false));
        Assert.False(TurnWorkEffects.RelocationSourceReady(false, true, false, false, false));
        Assert.False(TurnWorkEffects.RelocationSourceReady(true, true, false, true, false));
        Assert.False(TurnWorkEffects.RelocationSourceReady(true, true, false, false, true));

        Assert.True(TurnWorkEffects.TransferDestinationReady(true, true, false, false));
        Assert.False(TurnWorkEffects.TransferDestinationReady(false, true, false, false));
        Assert.False(TurnWorkEffects.TransferDestinationReady(true, false, false, false));
        Assert.False(TurnWorkEffects.TransferDestinationReady(true, true, true, false));
        Assert.False(TurnWorkEffects.TransferDestinationReady(true, true, false, true));
    }

    [Fact]
    public void DeploymentSuccessorIsTheChosenHalfOfAGoodOrderSquadStillHoldingTheAssignment()
    {
        string[] produced = ["a-1", "a-2"];
        Assert.True(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, true, "first", "a-1", produced, false));
        Assert.True(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, true, "second", "a-2", produced, false));
        Assert.False(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, true, "second", "a-1", produced, true));
        Assert.False(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(false, true, "first", "a-1", produced, true));
        Assert.False(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, false, "first", "a-1", produced, true));
        // A record from before the choice was recorded: the squad's custody decides.
        Assert.True(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, true, null, "a-1", produced, true));
        Assert.False(ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, true, null, "a-1", produced, false));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1SpotterCalculator.DeploymentSuccessorValid(true, true, "first", "a-1", null!, false));
    }

    [Theory]
    [InlineData("pfph", true)]
    [InlineData("dfph", false)]
    [InlineData("mph", false)]
    public void APrepFireDesignationBarsMovement(string phase, bool expected) =>
        Assert.Equal(expected, ScenarioA1SpotterCalculator.BarsMovement(phase));
}
