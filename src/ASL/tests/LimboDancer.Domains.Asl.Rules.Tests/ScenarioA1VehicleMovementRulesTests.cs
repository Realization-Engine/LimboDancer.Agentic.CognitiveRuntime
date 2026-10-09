using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 32.e: the vehicle movement, terrain cost, Recall, and Passenger decisions moved into Rules, read directly, with throwing callbacks where a
/// read must not happen.
/// </summary>
public sealed class ScenarioA1VehicleMovementRulesTests
{
    private static Func<T> Never<T>(string what) => () => throw new InvalidOperationException(what + " was read.");

    // ScenarioA1VehicleMovementCalculator

    [Fact]
    public void AnAfvIsAVehicleThatIsNotUnarmored()
    {
        Assert.True(ScenarioA1VehicleMovementCalculator.IsAfv(true, false));
        Assert.False(ScenarioA1VehicleMovementCalculator.IsAfv(true, true));
        Assert.False(ScenarioA1VehicleMovementCalculator.IsAfv(true, null));
        Assert.False(ScenarioA1VehicleMovementCalculator.IsAfv(false, false));
        Assert.True(ScenarioA1VehicleMovementCalculator.IsClosedTopped(true, null));
        Assert.False(ScenarioA1VehicleMovementCalculator.IsClosedTopped(true, true));
    }

    [Fact]
    public void TheAllotmentIncludesEsbMp() =>
        Assert.Equal((5, 28), ScenarioA1VehicleMovementCalculator.HalfMp(2, true, 12, 2));

    [Theory]
    [InlineData(30, 0, true)]
    [InlineData(330, 0, true)]
    [InlineData(90, 0, false)]
    [InlineData(0, 0, false)]
    public void AHexIsInTheVcaThirtyDegreesEitherSide(double bearing, double facing, bool inVca) =>
        Assert.Equal(inVca, ScenarioA1VehicleMovementCalculator.InVca(bearing, facing));

    [Fact]
    public void ARecallRouteTakesOnlyPlainForwardEntries()
    {
        Assert.Equal(4, ScenarioA1VehicleMovementCalculator.RecallEntryHalfMp(false, null, 4));
        Assert.Null(ScenarioA1VehicleMovementCalculator.RecallEntryHalfMp(true, null, 4));
        Assert.Null(ScenarioA1VehicleMovementCalculator.RecallEntryHalfMp(false, 1, 4));
    }

    [Fact]
    public void FirstEntryAndAfterAllReadTheSteps()
    {
        Assert.True(ScenarioA1VehicleMovementCalculator.FirstEntry([(true, false)]));
        Assert.False(ScenarioA1VehicleMovementCalculator.FirstEntry([(true, true)]));
        Assert.True(ScenarioA1VehicleMovementCalculator.AfterAllEntry([(false, false), (true, true)]));
        Assert.False(ScenarioA1VehicleMovementCalculator.AfterAllEntry([(true, false)]));
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, false)]
    public void AVehicleIsHaltedWhenInactiveImmobilizedOrRecalledWithoutStunRecovery(bool active, bool recalled, bool stunRecovery, bool halted) =>
        Assert.Equal(halted, ScenarioA1VehicleMovementCalculator.Halted(active, false, false, false, false, false, false, recalled, stunRecovery));

    [Fact]
    public void AVehicleMayMoveOnWithANonAllEntryOrATurnAndOneMp()
    {
        Assert.True(ScenarioA1VehicleMovementCalculator.MayMoveOn(0, 4, () => [(4, false)], false, Never<bool>("cafp"), Never<int>("turn")));
        Assert.False(ScenarioA1VehicleMovementCalculator.MayMoveOn(0, 4, () => [(4, true), (null, false)], true, () => true, Never<int>("turn")));
        Assert.True(ScenarioA1VehicleMovementCalculator.MayMoveOn(0, 4, () => [(6, false)], false, Never<bool>("cafp"), () => 2));
        Assert.False(ScenarioA1VehicleMovementCalculator.MayMoveOn(0, 4, () => [(6, false)], false, Never<bool>("cafp"), () => 4));
    }

    [Fact]
    public void AnIntendedEntryAddsItsTurns()
    {
        Assert.Equal(10, ScenarioA1VehicleMovementCalculator.IntendedEntryHalfMp(false, 2, 24, 4, 2));
        Assert.Equal(24, ScenarioA1VehicleMovementCalculator.IntendedEntryHalfMp(true, 2, 24, 4, 0));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "\u00BD")]
    [InlineData(4, "2")]
    [InlineData(5, "2\u00BD")]
    public void MpAreWrittenWithHalves(int halfMp, string text) =>
        Assert.Equal(text, ScenarioA1VehicleMovementCalculator.Mp(halfMp));

    [Fact]
    public void AMovingStepNeedsAMovingVehicleAndOnlyStopsAfterAll()
    {
        Assert.Contains("must start first", ScenarioA1VehicleMovementCalculator.MovingBar("v", "enter", false, false));
        Assert.Contains("ALL entry", ScenarioA1VehicleMovementCalculator.MovingBar("v", "enter", true, true));
        Assert.Null(ScenarioA1VehicleMovementCalculator.MovingBar("v", "stop", true, true));
    }

    [Fact]
    public void ATurnIsOneHexspineAndNotAgainAtTheCafp()
    {
        Assert.Contains("one hexspine", ScenarioA1VehicleMovementCalculator.TurnBar("v", 2, 0, false, Never<bool>("cafp")));
        Assert.Contains("one hexspine", ScenarioA1VehicleMovementCalculator.TurnBar("v", null, 0, false, Never<bool>("cafp")));
        Assert.Null(ScenarioA1VehicleMovementCalculator.TurnBar("v", 1, 0, false, Never<bool>("cafp")));
        Assert.Contains("D2.33", ScenarioA1VehicleMovementCalculator.TurnBar("v", 5, 0, true, () => true));
    }

    [Fact]
    public void AnEntrysCostBarsItUnlessAllOrAMinimumMove()
    {
        Assert.Contains("first expenditure", ScenarioA1VehicleMovementCalculator.EntryCostBar("v", "G5", true, 24, 24, false, false, false, false));
        Assert.Contains("needs no Minimum Move", ScenarioA1VehicleMovementCalculator.EntryCostBar("v", "G5", false, 4, 24, true, true, false, false));
        Assert.Contains("only entry", ScenarioA1VehicleMovementCalculator.EntryCostBar("v", "G5", false, 30, 24, true, true, true, false));
        Assert.Null(ScenarioA1VehicleMovementCalculator.EntryCostBar("v", "G5", false, 30, 24, true, true, false, false));
        Assert.Contains("only a Minimum Move", ScenarioA1VehicleMovementCalculator.EntryCostBar("v", "G5", false, 30, 24, true, false, false, false));
        Assert.Null(ScenarioA1VehicleMovementCalculator.EntryCostBar("v", "G5", false, 4, 24, false, false, false, false));
    }

    [Fact]
    public void ALeavingVehicleDoesNotStopAndReadsNothingElse()
    {
        Assert.Contains("D5.341", ScenarioA1VehicleMovementCalculator.StopBar("v", true, Never<string?>("enemy"), Never<bool>("move on"), true, Never<bool>("cafp")));
        Assert.Contains("enemy-afv", ScenarioA1VehicleMovementCalculator.StopBar("v", false, () => "blocked", () => true, false, Never<bool>("cafp")));
        Assert.Null(ScenarioA1VehicleMovementCalculator.StopBar("v", false, () => null, Never<bool>("move on"), false, Never<bool>("cafp")));
    }

    [Fact]
    public void AnExpenditureIsPaidFromTheMpLeft()
    {
        Assert.Contains("an ESB DR", ScenarioA1VehicleMovementCalculator.ExpenditureBar("v", "enter", false, false, false, false, 20, 24, 6, true));
        Assert.Null(ScenarioA1VehicleMovementCalculator.ExpenditureBar("v", "enter", false, false, true, false, 20, 24, 6, true));
        Assert.Contains("keeps one MP", ScenarioA1VehicleMovementCalculator.ExpenditureBar("v", "turn", false, false, null, true, 20, 24, 4, false));
        Assert.Null(ScenarioA1VehicleMovementCalculator.ExpenditureBar("v", "turn", false, false, null, false, 20, 24, 4, false));
    }

    [Fact]
    public void AStartIsBarredWhileMovingInReverseWhenTowingOrWithoutOneMp()
    {
        Assert.Contains("needs no Start MP", ScenarioA1VehicleMovementCalculator.StartBar("v", true, true, false, false, Never<bool>("tow"), false, 0, 24));
        Assert.Contains("tows a Gun", ScenarioA1VehicleMovementCalculator.StartBar("v", false, false, true, false, () => true, false, 0, 24));
        Assert.Contains("starting costs 1", ScenarioA1VehicleMovementCalculator.StartBar("v", false, false, false, false, Never<bool>("tow"), false, 23, 24));
        Assert.Null(ScenarioA1VehicleMovementCalculator.StartBar("v", false, false, false, false, Never<bool>("tow"), true, 23, 24));
    }

    [Fact]
    public void ABogRemovalCostsTheProductOfTheDiceDoubledForATruck()
    {
        Assert.Equal(12, ScenarioA1VehicleMovementCalculator.BogRemovalHalfMp(2, 3, false));
        Assert.Equal(24, ScenarioA1VehicleMovementCalculator.BogRemovalHalfMp(2, 3, true));
    }

    // ScenarioA1VehicleTerrainCosts

    [Fact]
    public void ReverseAndTrackedDependOnTheMovementType()
    {
        Assert.Equal(3, ScenarioA1VehicleTerrainCosts.ReverseMultiplier("truck"));
        Assert.Equal(4, ScenarioA1VehicleTerrainCosts.ReverseMultiplier("fully-tracked"));
        Assert.True(ScenarioA1VehicleTerrainCosts.Tracked("half-tracked"));
        Assert.False(ScenarioA1VehicleTerrainCosts.Tracked("truck"));
    }

    [Fact]
    public void AVehiclesOwnBogDrmFollowItsGroundPressureTowAndType()
    {
        Assert.Equal([("normal-ground-pressure", 1)], ScenarioA1VehicleTerrainCosts.VehicleBogDrm(null, false, "fully-tracked"));
        Assert.Equal([("high-ground-pressure", 2), ("towing", 1), ("not-fully-tracked", 1), ("truck-type-mp", 1)],
            ScenarioA1VehicleTerrainCosts.VehicleBogDrm("high", true, "truck"));
        Assert.Empty(ScenarioA1VehicleTerrainCosts.VehicleBogDrm("low", false, "fully-tracked"));
    }

    private static (VehicleTerrainEntry? Entry, string? Reason) Entry(string? type = "fully-tracked", int level = 0, bool cliff = false,
        string? terrain = "open-ground", int? month = 7, int rise = 0, bool reverse = false) =>
        ScenarioA1VehicleTerrainCosts.EntryCost(type, "G5", level, cliff, terrain, "that terrain", null, null, false, rise, month, false, false, false,
            24, false, false, false, _ => 0, () => false, false, (_, _, _, _) => 0, reverse, false, []);

    [Fact]
    public void AnEntryIsRefusedOutsideTheReviewedCases()
    {
        Assert.Contains("not an adjacent Location", Entry(type: null).Reason);
        Assert.Contains("ground level", Entry(level: 1).Reason);
        Assert.Contains("Continuous Slopes", Entry(cliff: true).Reason);
        Assert.Contains("not a reviewed entry", Entry(terrain: null).Reason);
        Assert.Contains("scenario month", Entry(terrain: "grain", month: null).Reason);
        Assert.Contains("Double-Crest", Entry(rise: 2).Reason);
    }

    [Fact]
    public void AnOpenGroundEntryCostsOneMpForATrackedVehicleAndReverseMultipliesIt()
    {
        var entry = Entry().Entry!;
        Assert.Equal(2, entry.HalfMp);
        Assert.False(entry.All);
        Assert.Null(entry.BogDrm);
        Assert.Equal(8, Entry(reverse: true).Entry!.HalfMp);
        Assert.Equal("open-ground", Entry(terrain: "grain", month: 11).Entry!.Terrain);
    }

    [Fact]
    public void TheEsbDrmFollowsTheManufacturersNationality()
    {
        // D2.5 (p. 198; pass 35, task 35.13): the ESB DRM Table. Until pass 35 every nationality but German and Russian took +3.
        Assert.Equal(0, ScenarioA1VehicleMovementCalculator.EsbNationalDrm("american"));
        Assert.Equal(0, ScenarioA1VehicleMovementCalculator.EsbNationalDrm("czech"));
        Assert.Equal(1, ScenarioA1VehicleMovementCalculator.EsbNationalDrm("russian"));
        Assert.Equal(1, ScenarioA1VehicleMovementCalculator.EsbNationalDrm("chinese"));
        Assert.Equal(2, ScenarioA1VehicleMovementCalculator.EsbNationalDrm("british"));
        Assert.Equal(2, ScenarioA1VehicleMovementCalculator.EsbNationalDrm("german"));
        foreach (var other in new[] { "french", "italian", "finnish", "axis-minor", null })
        {
            Assert.Equal(3, ScenarioA1VehicleMovementCalculator.EsbNationalDrm(other));
        }
    }

    [Fact]
    public void BypassReadsTheWreckPenaltyOnlyForANewHex()
    {
        Assert.Equal(4, ScenarioA1VehicleTerrainCosts.BypassHalfMp(2, 0, false, false, Never<int>("wrecks"), false, "fully-tracked"));
        Assert.Equal(6, ScenarioA1VehicleTerrainCosts.BypassHalfMp(2, 0, false, true, () => 2, false, "fully-tracked"));
        Assert.Equal(12, ScenarioA1VehicleTerrainCosts.BypassHalfMp(2, 0, false, false, Never<int>("wrecks"), true, "truck"));
    }

    [Fact]
    public void ATurnOutsideAnObstacleCostsOneMpAndReadsNoBogDrm()
    {
        Assert.Equal((2, (int?)null), Take(ScenarioA1VehicleTerrainCosts.TurnCost("open-ground", Never<List<(string, int)>>("bog"))));
        var woods = ScenarioA1VehicleTerrainCosts.TurnCost("woods", () => [("normal-ground-pressure", 1)]);
        Assert.Equal((4, (int?)1), Take(woods));
        Assert.Equal(["woods", "normal-ground-pressure"], woods.Causes);
    }

    private static (int, int?) Take((int HalfMp, int? BogDrm, IReadOnlyList<string> Causes) cost) => (cost.HalfMp, cost.BogDrm);

    [Theory]
    [InlineData(60, "front")]
    [InlineData(61, "side")]
    [InlineData(120, "side")]
    [InlineData(150, "rear")]
    public void TheTargetFacingFollowsTheAngle(double off, string facing) =>
        Assert.Equal(facing, ScenarioA1VehicleTerrainCosts.Facing(off));

    [Fact]
    public void ATkDrOfFiveKillsAboveTheHullOrAtTheTurret()
    {
        Assert.True(ScenarioA1VehicleTerrainCosts.KillsWithFive(11, 0, 5, "front", null, "front"));
        Assert.False(ScenarioA1VehicleTerrainCosts.KillsWithFive(10, 0, 5, "front", null, "front"));
        Assert.True(ScenarioA1VehicleTerrainCosts.KillsWithFive(10, 0, 5, "rear", null, "front"));
        Assert.True(ScenarioA1VehicleTerrainCosts.KillsWithFive(10, 0, null, "front", 5, "front"));
    }

    [Fact]
    public void WrecksAndSmokeAddToAnEntry()
    {
        Assert.Equal(4, ScenarioA1VehicleTerrainCosts.WreckEntryHalfMp(1, 0, false, true));
        Assert.Equal(8, ScenarioA1VehicleTerrainCosts.WreckEntryHalfMp(1, 1, true, false));
        Assert.Equal(2, ScenarioA1VehicleTerrainCosts.BlazeEntryHalfMf(true));
        Assert.Equal(0, ScenarioA1VehicleTerrainCosts.BlazeEntryHalfMf(false));
    }

    // ScenarioA1RecallCalculator

    [Fact]
    public void ARecalledVehicleMustLeaveUnlessImmobilizedOrAbandoned()
    {
        Assert.True(ScenarioA1RecallCalculator.MustLeave(true, true, true, false, false));
        Assert.False(ScenarioA1RecallCalculator.MustLeave(true, true, false, false, false));
        Assert.False(ScenarioA1RecallCalculator.MustLeave(true, true, true, true, false));
        Assert.False(ScenarioA1RecallCalculator.MustLeave(true, true, true, false, true));
    }

    [Fact]
    public void AnExitPaysItsTerrainOrTheRoadRate()
    {
        Assert.True(ScenarioA1RecallCalculator.ExitWithinVca(30, 0));
        Assert.False(ScenarioA1RecallCalculator.ExitWithinVca(180, 0));
        Assert.Equal("grain", ScenarioA1RecallCalculator.ExitTerrain("grain", 4));
        Assert.Equal("open-ground", ScenarioA1RecallCalculator.ExitTerrain("grain", 10));
        Assert.Null(ScenarioA1RecallCalculator.ExitTerrain("grain", null));
        Assert.Equal("woods", ScenarioA1RecallCalculator.ExitTerrain("woods", null));
        Assert.Equal(1, ScenarioA1RecallCalculator.ExitHalfMp(8, true, false));
        Assert.Equal(2, ScenarioA1RecallCalculator.ExitHalfMp(8, true, true));
        Assert.Equal(8, ScenarioA1RecallCalculator.ExitHalfMp(8, false, true));

        // E3.724, E3.7331 (pass 35, task 35.13 a): in Ground or Deep Snow an exit by road costs a full MP, as an entry by road does.
        Assert.Equal(2, ScenarioA1RecallCalculator.ExitHalfMp(8, true, false, snow: true));
        Assert.Equal(8, ScenarioA1RecallCalculator.ExitHalfMp(8, false, false, snow: true));

        // E1.52, E3.9 (pass 35, task 35.14): the night's and the weather's MP are added to an exit as to an entry.
        Assert.Equal(3, ScenarioA1RecallCalculator.ExitHalfMp(8, true, false, weatherHalfMp: 2));
        Assert.Equal(10, ScenarioA1RecallCalculator.ExitHalfMp(8, false, false, weatherHalfMp: 2));
    }

    private static IEnumerable<(string Move, int HalfMp, int To)> Moves(int node, bool lowerBound) => node switch
    {
        0 => [("a", 2, 1), ("b", 4, 2), .. lowerBound ? new[] { ("c", 1, 3) } : []],
        _ => [],
    };

    private static int? Exit(int node) => node switch { 1 => 2, 2 => 0, 3 => 0, _ => null };

    [Fact]
    public void DistanceIsTheLeastHalfMpOffTheMap()
    {
        Assert.Equal(4, ScenarioA1RecallCalculator.Distance<int, string>(0, false, Moves, Exit));
        Assert.Equal(1, ScenarioA1RecallCalculator.Distance<int, string>(0, true, Moves, Exit));
        Assert.Null(ScenarioA1RecallCalculator.Distance<int, string>(0, false, (_, _) => [], _ => null));
    }

    [Fact]
    public void ARouteGivesEveryFirstMoveOfAShortestRouteOrIsUndecided()
    {
        var exact = ScenarioA1RecallCalculator.Route<int, string>(0, (node, _) => Moves(node, false), Exit, _ => ["off"]);
        Assert.False(exact.Undecided);
        Assert.Equal(4, exact.HalfMp);
        Assert.Equal(["a", "b"], exact.Moves);

        var undecided = ScenarioA1RecallCalculator.Route<int, string>(0, Moves, Exit, _ => ["off"]);
        Assert.True(undecided.Undecided);
        Assert.Empty(undecided.Moves);
    }

    // ScenarioA1PassengerCalculator

    [Fact]
    public void PassengerCapacityLosesFourOrEightPpPerTowedGun()
    {
        Assert.Equal(4, ScenarioA1PassengerCalculator.PassengerCapacity(12, [75, 95]));
        Assert.Equal(0, ScenarioA1PassengerCalculator.PassengerCapacity(12, [75, 100]));
        Assert.Null(ScenarioA1PassengerCalculator.PassengerCapacity(null, []));
        Assert.Equal(15, ScenarioA1PassengerCalculator.PassengerPp(true, false, [5]));
        Assert.Equal(5, ScenarioA1PassengerCalculator.PassengerPp(false, true, []));
        Assert.Null(ScenarioA1PassengerCalculator.PassengerPp(true, false, null));
    }

    [Fact]
    public void TheCapacityBarReadsEachUnitOnlyUntilOneIsNotRecorded()
    {
        Assert.Contains("carries no Passengers", ScenarioA1PassengerCalculator.CapacityBar("v", null, null, 0, [("u", Never<int?>("u"))]));
        Assert.Contains("more than four SMC", ScenarioA1PassengerCalculator.CapacityBar("v", 10, 10, 5, [("u", Never<int?>("u"))]));
        Assert.Contains("u1 carries are not recorded", ScenarioA1PassengerCalculator.CapacityBar("v", 10, 10, 0, [("u1", () => null), ("u2", Never<int?>("u2"))]));
        Assert.Contains("towed Gun", ScenarioA1PassengerCalculator.CapacityBar("v", 6, 10, 0, [("u1", () => 10)]));
        Assert.Null(ScenarioA1PassengerCalculator.CapacityBar("v", 10, 10, 0, [("u1", () => 5), ("u2", () => 5)]));
    }

    [Fact]
    public void BoardingAndUnloadingCosts()
    {
        Assert.Contains("more than v's allotment", ScenarioA1PassengerCalculator.EntryAllotmentBar("v", "G5", 26, false, 24));
        Assert.Null(ScenarioA1PassengerCalculator.EntryAllotmentBar("v", "G5", 26, true, 24));
        Assert.Equal(3, ScenarioA1PassengerCalculator.BoardingHalfMfLeft(4, 1, true));
        Assert.Equal(12, ScenarioA1PassengerCalculator.LoadKeptHalfMp(24, 2));
        Assert.Equal(24, ScenarioA1PassengerCalculator.LoadKeptHalfMp(24, 6));
        Assert.True(ScenarioA1PassengerCalculator.UnloadTooLate(19, 24));
        Assert.False(ScenarioA1PassengerCalculator.UnloadTooLate(18, 24));
        Assert.Equal(6, ScenarioA1PassengerCalculator.UnloadHalfMp(24));
        Assert.Equal(1, ScenarioA1PassengerCalculator.UnloadMf(0, 24));
        Assert.Equal(3, ScenarioA1PassengerCalculator.UnloadMf(7, 24));
        Assert.Equal(1, ScenarioA1PassengerCalculator.UnloadMf(5, 0));
    }
}
