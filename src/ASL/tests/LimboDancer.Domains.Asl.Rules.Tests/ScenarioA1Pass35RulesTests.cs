using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 35, the Infantry increment, read directly in Rules: Good Order as A.7 has it and the scans that read it (task 35.3), the Morale Level
/// ceiling of A.18 (task 35.3), and the Russian Conscript's Battle Hardening (task 35.8). The wound procedure of task 35.1 is tested with the rout.
/// </summary>
public sealed class ScenarioA1Pass35RulesTests
{
    private sealed class ClearLos : ILosFactReader
    {
        public LosFacts? Los(int fromLocation, int toLocation) => new(true, Math.Abs(fromLocation - toLocation));
    }

    private static EnemyUnitFacts Enemy(int at, bool goodOrder = true, bool hidden = false, bool aboard = false, bool? broken = false, bool dummy = false) =>
        new(true, "german", dummy, aboard, broken, hidden, goodOrder, at);

    [Fact]
    public void GoodOrderIsNotBrokenBerserkCapturedStunnedShockedOrInMelee()
    {
        // A.7 (p. 43): a pinned, CX, TI, or unarmed unit is still in Good Order, so none of those is a fact of the verdict.
        Assert.True(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(false, false, false, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, true, false, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, true, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, true, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, true, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, false, true, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, false, false, true));

        // The planner's SW and Deployment actions keep their bar on a TI unit, apart from Good Order.
        Assert.True(ScenarioA1Definitions.FreeToActAsPlanned(true, false));
        Assert.False(ScenarioA1Definitions.FreeToActAsPlanned(true, true));
        Assert.False(ScenarioA1Definitions.FreeToActAsPlanned(false, false));
    }

    [Fact]
    public void TheScansCountAGoodOrderEnemyNotMerelyAnUnbrokenOne()
    {
        var los = new ClearLos();

        // A12.14 (backlog section 51): the move's read. An unbroken enemy that is not in Good Order (in Melee, say), a hidden one, and a Passenger do
        // not force the loss of a mover's "?"; a Good Order unit in the open does.
        Assert.True(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, goodOrder: false)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, hidden: true)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, aboard: true)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, dummy: true)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(17)], "russian", 0, los));

        // A12.34: the nearest Good Order enemy with a LOS.
        Assert.Equal(5, ScenarioA1MovementCalculator.NearestGoodOrderEnemyInLos([Enemy(2, goodOrder: false), Enemy(5)], "russian", 0, los));
        Assert.Null(ScenarioA1MovementCalculator.NearestGoodOrderEnemyInLos([Enemy(2, goodOrder: false)], "russian", 0, los));
    }

    [Fact]
    public void AMoraleLevelNeverExceedsTen()
    {
        // A.18 (p. 44): "even if the unit is Fanatic, heroic, with a Commissar, and/or part of a Human Wave".
        Assert.Equal(10, ScenarioA1Definitions.MoraleCeiling(11));
        Assert.Equal(10, ScenarioA1Definitions.MoraleCeiling(10));
        Assert.Equal(7, ScenarioA1Definitions.MoraleCeiling(7));
        Assert.Equal(10, ScenarioA1OverrunCalculator.PaatcMorale([new PaatcUnit(false, 10, true, false)]));
        Assert.Equal(9, ScenarioA1OverrunCalculator.PaatcMorale([new PaatcUnit(false, 8, true, false)]));
        Assert.Equal(10, ScenarioA1RoutCalculator.BrokenMorale(true, 10, 10, false, fanatic: true));
    }

    [Fact]
    public void ARussianConscriptBattleHardensToTheFiveTwoSeven()
    {
        // A25.2 (p. 93): "A 4-2-6 squad Battle Hardens to a 5-2-7", and its 2-2-6 HS to the 5-2-7's own 2-2-7; the 2-2-7 to a 3-2-8.
        Assert.Equal("defender-line-squad", ScenarioA1FireReference.HardenedOf("defender-conscript-squad"));
        Assert.Equal("defender-line-half-squad", ScenarioA1FireReference.HardenedOf("defender-conscript-half-squad"));
        Assert.Equal("defender-guards-half-squad", ScenarioA1FireReference.HardenedOf("defender-line-half-squad"));
    }

    private static InfantryEntry Step(int rise, int smokeHalfMf, string terrain = "open-ground") =>
        ScenarioA1TerrainCosts.GroundStep(new CrossedHexsideFacts(false, false, null, false, null), terrain, rise, 7, () => smokeHalfMf, (_, _, _) => (0, false)).Entry!;

    [Fact]
    public void SmokesMfIsDoubledWithTheRestOneLevelUp()
    {
        // B.2 (p. 112; task 35.5): "2 x 2 = 4 MF, not 2 x 1 = 2 + 1 = 3 MF", except across an Abrupt Elevation Change.
        Assert.Equal(8, Step(1, 2).HalfMf);
        Assert.Equal(4, Step(1, 0).HalfMf);
        Assert.Equal(4, Step(0, 2).HalfMf);
        Assert.Equal(12, Step(1, 2, "woods").HalfMf);
        Assert.Equal(10, Step(2, 2).HalfMf);
    }

    [Fact]
    public void AHindranceTotalOfSixBlocksWhateverItsSources()
    {
        // B.10 (p. 113; task 35.5): three brush hexes on the LOS and +3 from vehicles and SMOKE make six.
        Assert.False(ScenarioA1FireMapRules.HindranceBlocks(5));
        Assert.True(ScenarioA1FireMapRules.HindranceBlocks(6));
        LosReadFacts los = new(true, "Clear", string.Empty, false, 5, [new(1, 1, ["Brush"]), new(2, 1, ["Brush"]), new(3, 1, ["Brush"])]);
        Assert.True(ScenarioA1FireMapRules.LocationLos(los, 7, true, _ => (3, null)).Los!.Blocked);
        var five = ScenarioA1FireMapRules.LocationLos(los, 7, true, _ => (2, null)).Los!;
        Assert.Equal((false, 5), (five.Blocked, five.HindranceDrm));
    }

    private static (VehicleTerrainEntry? Entry, string? Reason) Vehicle(string type, string terrain, bool road = false, bool besideMarsh = false,
        string? wall = null, bool towing = false) =>
        ScenarioA1VehicleTerrainCosts.EntryCost(type, "G5", 0, false, terrain, "that terrain", wall, wall, road, 0, 7, false, false, false,
            24, false, false, false, _ => 0, () => false, towing, (_, _, _, _) => 0, false, false, [], besideMarsh);

    [Fact]
    public void AVehicleTowingAGunCrossesNoWallOrHedgeEntersNoRubbleAndUsesNoBypass()
    {
        // C10.1 (p. 180; task 35.12). Until pass 35 a halftrack towed a Gun over a hedge, a tank into rubble, and either along a Bypass hexside at +1 MP.
        Assert.Contains("may not cross a hedge (C10.1)", Vehicle("half-tracked", "open-ground", wall: "hedge", towing: true).Reason);
        Assert.Contains("may not cross a wall (C10.1)", Vehicle("fully-tracked", "open-ground", wall: "wall", towing: true).Reason);
        Assert.Contains("may not enter rubble (C10.1)", Vehicle("fully-tracked", "stone-rubble", towing: true).Reason);
        Assert.Equal("a vehicle towing a Gun may not use Bypass Movement (C10.1)", ScenarioA1VehicleTerrainCosts.TowingBypassBar(true));
        Assert.Null(ScenarioA1VehicleTerrainCosts.TowingBypassBar(false));

        // Not towing, each is entered as before; towing, a road's gap in the hedge is crossed at the road rate plus the one MP of the tow.
        Assert.Equal(6, Vehicle("half-tracked", "open-ground", wall: "hedge").Entry!.HalfMp);
        Assert.NotNull(Vehicle("fully-tracked", "stone-rubble").Entry);
        Assert.Equal(3, Vehicle("half-tracked", "open-ground", road: true, wall: "hedge", towing: true).Entry!.HalfMp);
    }

    [Fact]
    public void AnOrchardCostsAVehicleWhatOpenGroundDoes()
    {
        // B14.4 (p. 129; task 35.6). Until pass 35 a vehicle was refused an orchard.
        foreach (var type in new[] { "fully-tracked", "half-tracked", "truck" })
        {
            Assert.Equal(Vehicle(type, "open-ground").Entry!.HalfMp, Vehicle(type, "orchard").Entry!.HalfMp);
            Assert.Null(Vehicle(type, "orchard").Entry!.BogDrm);
        }
    }

    [Fact]
    public void AHexBesideAMarshIsABogHexOffTheRoad()
    {
        // B16.43 (p. 130; task 35.7).
        var bog = Vehicle("fully-tracked", "open-ground", besideMarsh: true).Entry!;
        Assert.NotNull(bog.BogDrm);
        Assert.Contains("beside-marsh", bog.BogCauses);
        Assert.Null(Vehicle("fully-tracked", "open-ground", road: true, besideMarsh: true).Entry!.BogDrm);
        Assert.Null(Vehicle("fully-tracked", "open-ground").Entry!.BogDrm);
    }
}
