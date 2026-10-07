using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 32.e: the vehicle decisions moved into Rules, read directly: the projector's (ScenarioA1VehicleProjection) and the sight, cover, and
/// Hindrance rules (ScenarioA1VehicleSightRules), with their lazy reads.
/// </summary>
public sealed class ScenarioA1VehicleRulesTests
{
    private static Func<bool> Never(string what) => () => throw new InvalidOperationException(what + " was read.");

    [Fact]
    public void AVehicleStepIsMadeInTheMph()
    {
        Assert.True(ScenarioA1VehicleProjection.StepPhase("mph"));
        Assert.False(ScenarioA1VehicleProjection.StepPhase("dfph"));
        Assert.False(ScenarioA1VehicleProjection.StepPhase(null));
    }

    [Theory]
    [InlineData(true, true, false, 2, false, true)]
    [InlineData(false, true, false, 2, false, false)]
    [InlineData(true, false, false, 2, false, false)]
    [InlineData(true, true, true, 2, false, false)]
    [InlineData(true, true, false, -1, false, false)]
    [InlineData(true, true, false, 0, false, false)]
    [InlineData(true, true, false, 0, true, true)]
    public void AVehicleStepMovesAPhasingVehicleThatHasNotEndedItsMove(bool vehicle, bool phasing, bool ended, int halfMp, bool load, bool allowed) =>
        Assert.Equal(allowed, ScenarioA1VehicleProjection.StepAllowed(vehicle, phasing, ended, halfMp, load));

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void ARecallStopsTheAfvUntilItsCounterShowsRecallPlusOne(bool recalled, bool stunRecovery, bool recalling) =>
        Assert.Equal(recalling, ScenarioA1VehicleProjection.Recalling(recalled, stunRecovery));

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(0, 5, true)]
    [InlineData(5, 0, true)]
    [InlineData(0, 2, false)]
    [InlineData(0, 3, false)]
    [InlineData(3, 3, false)]
    public void ATurnIsOneHexspine(int turned, int facing, bool one) =>
        Assert.Equal(one, ScenarioA1VehicleProjection.OneHexspine(turned, facing));

    [Theory]
    [InlineData(0, false, 2, 1, false)]
    [InlineData(1, true, 1, 2, false)]
    [InlineData(1, false, 3, 2, true)]
    [InlineData(2, true, 0, 2, true)]
    public void MpAreCountedInHalves(int mf, bool half, int halfMp, int expectedMf, bool expectedHalf) =>
        Assert.Equal((expectedMf, expectedHalf), ScenarioA1VehicleProjection.Spend(mf, half, halfMp));

    [Fact]
    public void ABogRemovalReadsTheColoredDieAndOtherChecksTheDr()
    {
        Assert.Equal(5, ScenarioA1VehicleProjection.CheckFinal(true, 3, 6, 2));
        Assert.Equal(11, ScenarioA1VehicleProjection.CheckFinal(false, 3, 6, 2));
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, false)]
    public void ABogAnImmobilizationOrAFailedBogRemovalEndsTheMove(bool boggedOrImmobilized, bool bogRemoval, bool freed, bool stops) =>
        Assert.Equal(stops, ScenarioA1VehicleProjection.CheckStops(boggedOrImmobilized, bogRemoval, freed));

    [Fact]
    public void AnOverrunReadsItsMoverAndFireOnlyWhenTheWindowHasClosedHere()
    {
        Assert.False(ScenarioA1VehicleProjection.OverrunResolvable(false, true, Never("mover"), Never("fire")));
        Assert.False(ScenarioA1VehicleProjection.OverrunResolvable(true, false, Never("mover"), Never("fire")));
        Assert.False(ScenarioA1VehicleProjection.OverrunResolvable(true, true, () => false, Never("fire")));
        Assert.True(ScenarioA1VehicleProjection.OverrunResolvable(true, true, () => true, () => true));
    }

    [Theory]
    [InlineData(3, 4, 0, 7, true)]
    [InlineData(3, 4, 1, 7, false)]
    [InlineData(1, 1, -1, 2, true)]
    public void APaatcPassesWhenItsFinalDrDoesNotExceedTheMorale(int first, int second, int drm, int morale, bool passed) =>
        Assert.Equal(passed, ScenarioA1VehicleProjection.PaatcPassed(first, second, drm, morale));

    [Fact]
    public void PassengersExitOnlyWithAnExitedVehicle()
    {
        Assert.True(ScenarioA1VehicleProjection.PassengerExits(true));
        Assert.False(ScenarioA1VehicleProjection.PassengerExits(false));
    }

    [Fact]
    public void OnlyAVehicleOnTheMapBecomesAWreck()
    {
        Assert.False(ScenarioA1VehicleProjection.WreckAllowed(false, Never("location")));
        Assert.False(ScenarioA1VehicleProjection.WreckAllowed(true, () => false));
        Assert.True(ScenarioA1VehicleProjection.WreckAllowed(true, () => true));
    }

    [Theory]
    [InlineData(5, "grain", false)]
    [InlineData(6, "grain", true)]
    [InlineData(9, "grain", true)]
    [InlineData(10, "grain", false)]
    [InlineData(7, "brush", false)]
    public void GrainInSeasonIsConcealmentTerrain(int month, string terrain, bool concealment) =>
        Assert.Equal(concealment, ScenarioA1VehicleSightRules.ConcealmentTerrain(month, () => terrain));

    [Fact]
    public void ConcealmentTerrainReadsNoTerrainOutOfSeason() =>
        Assert.False(ScenarioA1VehicleSightRules.ConcealmentTerrain(null, () => throw new InvalidOperationException("terrain was read.")));

    [Fact]
    public void AWatcherIsGoodOrderPersonnelOrAnUnstunnedVehicle()
    {
        Assert.True(ScenarioA1VehicleSightRules.Watching(true, true, false, false, false, false, false, Never("personnel"), true, true, true, true));
        Assert.False(ScenarioA1VehicleSightRules.Watching(true, true, false, false, false, true, false, Never("personnel"), false, false, false, false));
        Assert.True(ScenarioA1VehicleSightRules.Watching(true, false, false, false, false, false, false, () => true, false, false, false, false));
        Assert.False(ScenarioA1VehicleSightRules.Watching(true, false, false, false, false, false, false, () => true, true, false, false, false));
        Assert.False(ScenarioA1VehicleSightRules.Watching(true, false, false, false, false, false, false, () => false, false, false, false, false));
        Assert.False(ScenarioA1VehicleSightRules.Watching(false, true, false, false, false, false, false, Never("personnel"), false, false, false, false));
    }

    [Fact]
    public void TheRangeIsReadOnlyWhenALimitIsGiven()
    {
        Assert.True(ScenarioA1VehicleSightRules.WithinRange(null, () => throw new InvalidOperationException("range was read.")));
        Assert.True(ScenarioA1VehicleSightRules.WithinRange(16, () => 16));
        Assert.False(ScenarioA1VehicleSightRules.WithinRange(16, () => 17));
        Assert.False(ScenarioA1VehicleSightRules.WithinRange(16, () => null));
    }

    [Fact]
    public void AVehicleLosesItsConcealmentMovingWithin16OrSeenOutOfConcealmentTerrain()
    {
        Assert.True(ScenarioA1VehicleSightRules.ConcealmentLost(true, within => within == 16, Never("terrain")));
        Assert.True(ScenarioA1VehicleSightRules.ConcealmentLost(false, within => within is null, () => false));
        Assert.False(ScenarioA1VehicleSightRules.ConcealmentLost(false, _ => true, () => true));
        Assert.False(ScenarioA1VehicleSightRules.ConcealmentLost(true, _ => false, () => false));
    }

    [Theory]
    [InlineData(false, "mph", false, true)]
    [InlineData(true, "rph", false, false)]
    [InlineData(false, "mph", true, false)]
    [InlineData(false, "dfph", true, false)]
    [InlineData(false, "afph", true, false)]
    [InlineData(false, "pfph", true, true)]
    public void AnAfvOrWreckStandsOutOfMotionAndNotAfterMovingThisTurn(bool motion, string phase, bool moved, bool standing) =>
        Assert.Equal(standing, ScenarioA1VehicleSightRules.Standing(motion, phase, moved));

    [Fact]
    public void CoverIsTheFirstStandingUnburningWreckElseTheFirstStandingFriendlyAfv()
    {
        Assert.Equal("w2", ScenarioA1VehicleSightRules.Cover(
            [("w1", () => true, Never("w1 standing")), ("w2", () => false, () => true), ("w3", Never("w3 burning"), Never("w3 standing"))],
            [("a1", Never("a1"), Never("a1"), Never("a1"))]));
        Assert.Equal("a2", ScenarioA1VehicleSightRules.Cover(
            [("w1", () => false, () => false)],
            [("a1", () => true, () => false, Never("a1 standing")), ("a2", () => true, () => true, () => true)]));
        Assert.Null(ScenarioA1VehicleSightRules.Cover([], [("a1", () => false, Never("a1"), Never("a1"))]));
    }

    [Theory]
    [InlineData(false, "open", 1, true)]
    [InlineData(true, "open", 7, false)]
    [InlineData(true, "brush", 1, true)]
    [InlineData(true, "grain", 7, true)]
    [InlineData(true, "grain", 4, false)]
    public void AVehicleHindersAtARangeUnlessTheMapHindersThereAndItDoesNotAdd(bool mapHindrance, string terrain, int month, bool hinders) =>
        Assert.Equal(hinders, ScenarioA1VehicleSightRules.HindersAtRange(mapHindrance, terrain, month));

    [Fact]
    public void SmokeIsTwoPerSourceAtMostThreeWithOneMoreForTheFirersLocation()
    {
        Assert.Equal(3, ScenarioA1VehicleSightRules.SmokeDrm(1, true, false, Never("crossed")));
        Assert.Equal(4, ScenarioA1VehicleSightRules.SmokeDrm(2, true, false, Never("crossed")));
        Assert.Equal(3, ScenarioA1VehicleSightRules.SmokeDrm(2, false, true, Never("crossed")));
        Assert.Equal(2, ScenarioA1VehicleSightRules.SmokeDrm(1, false, false, () => true));
        Assert.Equal(0, ScenarioA1VehicleSightRules.SmokeDrm(1, false, false, () => false));
    }
}
