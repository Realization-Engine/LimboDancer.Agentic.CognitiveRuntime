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

    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;
    private const string OrdnanceAt = "bd03:G5:0";

    private static FireAttack OrdnanceHit(string phase, string side, bool infantry, string terrain = "open-ground") =>
        new(phase, side, null, null, OrdnanceAt, [], null, null, true, new FireLos(false, 0, true, false), 7, terrain,
            infantry ? [new FireTarget("ru-s", "defender-squad", OrdnanceAt, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }] : [], 2, null);

    /// <summary>The German infantry gun at Infantry, or a LATW at a tank; an Opportunity Firer when asked, with the To Hit DR a miss so no IFT roll is owed.</summary>
    private static OrdnanceResolution Fire(string phase, bool opportunity, string? latw = null, int range = 5, bool pinned = false, bool backblast = false,
        OrdnanceMovement? movement = null, int shots = 0, bool kept = false, string terrain = "open-ground", bool bypass = false, bool nonStopped = false)
    {
        var side = phase is "DFPh" or "MPh" ? "non-phasing" : "phasing";
        var russian = latw == "defender-atr";
        var shot = new OrdnanceShot(phase, side, russian ? "russian" : "german", new OrdnanceGun("gun", latw ?? "attacker-inf-gun", false, shots, kept, shots > 0),
            new OrdnanceCrew("firer", latw is null ? "attacker-crew" : russian ? "defender-squad" : "attacker-squad", false, pinned, false, false, false)
            {
                OpportunityFire = opportunity ? true : null,
            }, OrdnanceAt, range, 0, false, true, 0, OrdnanceHit(phase, side, latw is null, terrain), new OrdnanceRolls([5, 6], null, null, null, null) { ToKill = [6, 6] })
        {
            VehicleTarget = latw is null ? null : new OrdnanceVehicleTarget("target", russian ? "attacker-tank" : "defender-tank", "side", "side", false, nonStopped, false, false, true)
            {
                Bypass = bypass ? true : null,
            },
            Ammunition = latw is null ? null : russian ? "ap" : "heat",
            ScenarioYear = 1944,
            Panzerfaust = backblast ? new OrdnancePanzerfaust(null, null, true, null) : null,
            FireKind = movement is null ? null : "first-fire",
            Movement = movement,
        };
        return ScenarioA1OrdnanceCalculator.Resolve(shot, Ordnance);
    }

    private static decimal? CaseOf(OrdnanceResolution result, string name) =>
        result.ToHit!.Drm.Where(item => item.Name.StartsWith(name, StringComparison.Ordinal)).Select(item => item.Value).ToArray() is { Length: > 0 } values ? values.Sum() : null;

    [Fact]
    public void RubbleIsAFirersTerrainAsWoodsAndBuildingsAre()
    {
        // C5.11, C5.2, C5.5 (p. 172; task 35.10): one list feeds Case A's and Case E's doubling, Case B's +3, and the fixed CA.
        Assert.Contains("stone-rubble", ScenarioA1Definitions.WoodsOrBuilding);
        Assert.Contains("wooden-rubble", ScenarioA1Definitions.WoodsOrBuilding);

        // C5.34, C13.8 (pp. 172, 185): rubble is a Backblast Location as a building is.
        Assert.True(ScenarioA1OrdnanceMapRules.IsBackblastLocation("stone-rubble"));
        Assert.True(ScenarioA1OrdnanceMapRules.IsBackblastLocation("wooden-building"));
        Assert.False(ScenarioA1OrdnanceMapRules.IsBackblastLocation("woods"));
    }

    [Fact]
    public void AWreckSmokeOrAnAfvInTheGunsHexHindersAShotWithinIt()
    {
        // C5.5 (p. 172; task 35.10) with A24.2, A24.8 and D9.4: one SMOKE source is +2 and +1 for fire within it, two are held to +3 and +1; a
        // wreck or AFV adds +1. Until pass 35 the shot's Hindrance was 0.
        Assert.Equal(0, ScenarioA1VehicleSightRules.OwnHexHindrance(0, false));
        Assert.Equal(1, ScenarioA1VehicleSightRules.OwnHexHindrance(0, true));
        Assert.Equal(3, ScenarioA1VehicleSightRules.OwnHexHindrance(1, false));
        Assert.Equal(5, ScenarioA1VehicleSightRules.OwnHexHindrance(2, true));
    }

    [Fact]
    public void AnAtrTakesCaseLAndAPanzerschreckDoesNot()
    {
        // C6.3 (p. 174; task 35.10): only a LATW that reads its own To Hit Table is denied Point Blank Range.
        Assert.Equal(-1m, CaseOf(Fire("PFPh", false, "defender-atr", 2), "case-l"));
        Assert.Equal(-2m, CaseOf(Fire("PFPh", false, "defender-atr", 1), "case-l"));
        Assert.Null(CaseOf(Fire("PFPh", false, "attacker-psk", 2), "case-l"));
    }

    [Fact]
    public void HazardousMovementIsCaseOInPlaceOfFfnamAndFfmo()
    {
        // C6.6 (p. 175; task 35.10): -2, never with Case J's subcases. Until pass 35 a crew pushing its Gun gave the firer Cases J3 and J4.
        var pushing = Fire("MPh", false, movement: new OrdnanceMovement(null, true, true, 1, 0) { Hazardous = true });
        Assert.Equal((-2m, (decimal?)null), (CaseOf(pushing, "case-o"), CaseOf(pushing, "case-j")));
        var walking = Fire("MPh", false, movement: new OrdnanceMovement(null, true, true, 1, 0));
        Assert.Equal(((decimal?)null, -2m), (CaseOf(walking, "case-o"), CaseOf(walking, "case-j")));
    }

    [Fact]
    public void AnOpportunityFirersOrdnanceTakesNoAfphPenalty()
    {
        // C5.2 (p. 172; task 35.10): Case B, one shot, and no Multiple ROF are for AFPh fire not using Opportunity Fire (A7.25).
        var plain = Fire("AFPh", false);
        var held = Fire("AFPh", true);
        Assert.Equal((2m, 0), (CaseOf(plain, "case-b"), plain.Gun!.RateOfFire));
        Assert.Null(CaseOf(held, "case-b"));
        Assert.True(held.Gun!.RateOfFire > 0);
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Fire("AFPh", false, shots: 1, kept: true).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Fire("AFPh", true, shots: 1, kept: true).Disposition);
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Fire("AFPh", true, shots: 1).Reasons);

        // C5.34, C13.1, C13.8 (pp. 172, 183, 185): Case C3 for the AFPh and for the Backblast, neither for an Opportunity Firer; a pinned firer
        // in a building or rubble fires only by Desperation, which is not built.
        Assert.Equal(4m, CaseOf(Fire("AFPh", false, "attacker-psk", 2, backblast: true), "case-c3"));
        Assert.Null(CaseOf(Fire("AFPh", true, "attacker-psk", 2, backblast: true), "case-c3"));
        Assert.Equal(2m, CaseOf(Fire("PFPh", false, "attacker-psk", 2, backblast: true), "case-c3"));
        Assert.Contains("asl.a1.ordnance.panzerfaust-backblast", Fire("AFPh", true, "attacker-psk", 2, pinned: true, backblast: true).Reasons);
    }

    [Fact]
    public void AnAcquisitionIsLostWhenItsFirerLeavesOrTurnsWithoutFiringAndFollowsAVehicle()
    {
        // C6.5 (p. 174; task 35.11): lost when the Gun or its manning Infantry leaves its Location, or the Gun changes its CA without having fired
        // on its acquired target in the current phase.
        Assert.False(ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn("bd01:A8:0", "bd01:A8:0", false, 0));
        Assert.True(ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn("bd01:A8:0", "bd01:B8:0", false, 1));
        Assert.True(ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn("bd01:A8:0", null, false, 0));
        Assert.True(ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn("bd01:A8:0", "bd01:A8:0", true, 0));
        Assert.False(ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn("bd01:A8:0", "bd01:A8:0", true, 1));
        Assert.False(ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn(null, "bd01:A8:0", false, 0));

        // C6.51: a Known vehicle that the shot does not wreck carries the counter; a concealed one only once a hit has revealed it.
        Assert.Equal("tank", ScenarioA1ResultTables.AcquiredVehicle("tank", false, false, false));
        Assert.Equal("tank", ScenarioA1ResultTables.AcquiredVehicle("tank", true, true, false));
        Assert.Null(ScenarioA1ResultTables.AcquiredVehicle("tank", true, false, false));
        Assert.Null(ScenarioA1ResultTables.AcquiredVehicle("tank", false, true, true));
        Assert.Null(ScenarioA1ResultTables.AcquiredVehicle(null, false, true, false));
    }

    [Fact]
    public void ABypassingVehicleTakesNoTemOfItsObstacleAndHindersOnlyAcrossItsHexside()
    {
        // D2.38 (p. 198; task 35.13 b): a vehicle in Bypass is in the Open Ground of its hex. Until pass 35 a shot at it took the woods' Case Q.
        Assert.Equal(1m, CaseOf(Fire("PFPh", false, "defender-atr", 3, terrain: "woods"), "case-q"));
        Assert.Null(CaseOf(Fire("PFPh", false, "defender-atr", 3, terrain: "woods", bypass: true), "case-q"));

        // D9.4 (p. 210; task 35.13 h): a Bypassing AFV or wreck hinders only a LOS that touches the hexside it Bypasses.
        Assert.True(ScenarioA1VehicleSightRules.BypassHinders(false, () => throw new InvalidOperationException("not read")));
        Assert.True(ScenarioA1VehicleSightRules.BypassHinders(true, () => true));
        Assert.False(ScenarioA1VehicleSightRules.BypassHinders(true, () => false));

        // C6.3, D2.13 (pp. 174, 196; task 35.13): no Case L against a vehicle that started in its MPh and has not stopped, Motion counter or none.
        Assert.Equal(-1m, CaseOf(Fire("DFPh", false, "defender-atr", 2), "case-l"));
        Assert.Null(CaseOf(Fire("DFPh", false, "defender-atr", 2, nonStopped: true), "case-l"));
        OrdnanceVehicleTargetStateFacts vehicle = new("tank", "attacker-tank", false, true, false, false, false, false, false, false, false, false, false);
        Assert.False(ScenarioA1OrdnanceEligibility.VehicleTarget(vehicle).NonStopped);
        var moving = ScenarioA1OrdnanceEligibility.VehicleTarget(vehicle with
        {
            Bypass = true,
            MovingUnstopped = true
        });
        Assert.Equal((true, true), (moving.NonStopped, moving.Bypass));
    }

    [Fact]
    public void ABoggedVehicleLoadsAndUnloadsAndARecalledOneIsAbandonedMakesNoEsbAndStopsOnlyToUnload()
    {
        // D8.3, D8.5 (p. 209; task 35.13 f): Bog Removal as its first expenditure, and the non-movement expenditures: Loading and Unloading.
        Assert.True(ScenarioA1VehicleProjection.BoggedMaySpend("start", true));
        Assert.False(ScenarioA1VehicleProjection.BoggedMaySpend("start", false));
        Assert.True(ScenarioA1VehicleProjection.BoggedMaySpend("unload", false));
        Assert.True(ScenarioA1VehicleProjection.BoggedMaySpend("load", true));
        Assert.False(ScenarioA1VehicleProjection.BoggedMaySpend("enter", true));
        Assert.False(ScenarioA1VehicleProjection.BoggedMaySpend("turn", true));
        static string? Move(string kind, bool started = false) => ScenarioA1VehicleMovementCalculator.MoveBar("tank", kind, false, () => false, name => name == "asl:bogged", false, null, false,
            false, null, () => null, started);
        Assert.Null(Move("unload"));
        Assert.Null(Move("unload", started: true));
        Assert.Null(Move("start"));
        Assert.Contains("play.move-vehicle-bog", Move("enter"));
        Assert.Contains("play.move-vehicle-bog", Move("start", started: true));

        // D5.341 (p. 203; task 35.13 g): a bogged Recalled AFV is Abandoned as an immobilized one is; ESB is NA; it Stops only to unload.
        Assert.True(ScenarioA1SequenceCalculator.RecallAbandoned(true, true, true, true, false, false, bogged: true));
        Assert.True(ScenarioA1SequenceCalculator.RecallAbandoned(true, true, true, true, true, false));
        Assert.False(ScenarioA1SequenceCalculator.RecallAbandoned(true, true, true, true, false, false));
        Assert.False(ScenarioA1SequenceCalculator.RecallAbandoned(true, true, true, false, false, false, bogged: true));
        Assert.Contains("bogged", ScenarioA1SequenceCalculator.RecallAbandonedText("tank", true));
        static string? Esb(bool recalled) => ScenarioA1VehicleMovementCalculator.EsbBar("tank", true, true, () => false, false, () => null, 2, 4, recalled);
        Assert.Null(Esb(false));
        Assert.Contains("D5.341", Esb(true));
        static string? Stop(bool carrying) => ScenarioA1VehicleMovementCalculator.StopBar("tank", true, () => null, () => true, false, () => false, carrying);
        Assert.Contains("play.recall-motion", Stop(false));
        Assert.Null(Stop(true));
        Assert.True(ScenarioA1RecallCalculator.StopsToUnload("stop", true));
        Assert.False(ScenarioA1RecallCalculator.StopsToUnload("stop", false));
        Assert.False(ScenarioA1RecallCalculator.StopsToUnload("turn", true));
    }
}
