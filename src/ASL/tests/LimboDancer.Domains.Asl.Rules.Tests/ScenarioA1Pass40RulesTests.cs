using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 40, read directly in Rules. Task 40.9: Deployment and Recombining without a leader, the US#, and a butted half-hex (A1.31, A1.32, A1.6, A2.3; p. 45). Task 40.5: the attack owed in the DEFENDER's own Location, and a crew's FP in FPF (A8.311, A8.312; p. 61). Task 40.4: Infantry OVR and the recapture of Unarmed units by entry (A4.15 to A4.152, A20.54; pp. 49, 88). Task 40.7: the portage and movement corrections (A4.4, A4.41, A4.42, A4.44, A4.61; pp. 48 to 51). Task 40.3: Dash (A4.63, C6.1, pp. 51 to 52, 173). Task 40.2: a woods-road hex, the mover's choice across a road hexside, and the terrain a firer reads for a mover at
/// the road rate (A4.132, B3.3, B13.31, pp. 48, 113, 128). Task 40.6: the one count of a stack (A5.1, A5.2, A5.5, pp. 52 to 53), which the MPh, the APh, the IFT, the To Hit
/// DR, Close Combat, and setup all read, and the MF and MP an overstacked Location costs (A5.11). The IFT DRM is tested with the Fire package's
/// helpers in ScenarioA1Pass10Tests.
/// </summary>
public sealed class ScenarioA1Pass40RulesTests
{
    [Theory]
    // The example of A5.11 (p. 52): two squads and a crew; a squad enters (an excess of a half, one MF); a second squad (one and a half, two MF);
    // then a HS or crew (two, two MF).
    [InlineData(2, 1, 0, 0, 0)]
    [InlineData(3, 1, 0, 0, 1)]
    [InlineData(4, 1, 0, 0, 2)]
    [InlineData(4, 2, 0, 0, 2)]
    // A5.5 (p. 53): four SMC are nothing and five a HS; nine are still one HS.
    [InlineData(3, 0, 0, 4, 0)]
    [InlineData(3, 0, 0, 5, 1)]
    [InlineData(2, 1, 0, 5, 0)]
    [InlineData(2, 1, 0, 9, 0)]
    [InlineData(2, 1, 0, 10, 1)]
    // A5.5: a crew or HS manning a Gun is a squad, so three Guns in use fill a Location (A5.4).
    [InlineData(2, 2, 0, 0, 0)]
    [InlineData(2, 0, 2, 0, 1)]
    [InlineData(0, 0, 3, 0, 0)]
    [InlineData(0, 0, 4, 0, 1)]
    public void AStackIsCountedInSquadEquivalentsAndItsExcessRoundedUp(int squads, int halfSquadsAndCrews, int manning, int smc, int excess) =>
        Assert.Equal(excess, new StackCount(squads, halfSquadsAndCrews, manning, smc, 0).PersonnelExcess);

    [Fact]
    public void VehiclesAboveOneAddToTheAttackPenaltyAndNotToTheDefensePenalty()
    {
        // A5.2, A5.12, A5.131 (p. 52): "for each vehicle and squad equivalent" on the attack; Personnel alone on the defense.
        var crowded = new StackCount(4, 0, 0, 0, 3);
        Assert.Equal(1, crowded.PersonnelExcess);
        Assert.Equal(2, crowded.VehicleExcess);
        Assert.Equal(3, crowded.AttackPenalty);
        Assert.Equal(1, crowded.DefensePenalty);
        Assert.Equal(0, new StackCount(3, 0, 0, 4, 1).AttackPenalty);
    }

    [Fact]
    public void TheCountLeavesOutPrisonersAndCountersTheLimitsDoNotCount()
    {
        // A20.51: prisoners never count. A Dummy, a SW, and a Gun have no stacking kind.
        var count = ScenarioA1Stacking.Count([new(ScenarioA1Stacking.KindOf("asl:squad")), new(ScenarioA1Stacking.KindOf("asl:squad"), Captured: true),
            new(ScenarioA1Stacking.KindOf("asl:crew"), Manning: true), new(ScenarioA1Stacking.KindOf("asl:half-squad")), new(ScenarioA1Stacking.KindOf("asl:leader")),
            new(ScenarioA1Stacking.KindOf("asl:hero")), new(ScenarioA1Stacking.KindOf("asl:smc")), new(ScenarioA1Stacking.KindOf("asl:vehicle")),
            new(ScenarioA1Stacking.KindOf("asl:dummy")), new(ScenarioA1Stacking.KindOf("asl:gun")), new(ScenarioA1Stacking.KindOf(null))]);
        Assert.Equal(new StackCount(1, 1, 1, 3, 1), count);
        Assert.Equal(2.5m, count.SquadEquivalents);
        Assert.False(ScenarioA1Stacking.Overstacked(count));
        Assert.True(ScenarioA1Stacking.Overstacked(new StackCount(3, 1, 0, 0, 0)));
    }

    [Fact]
    public void AFireGroupTakesTheLargestAttackPenaltyOfItsLocations()
    {
        // A7.52 (p. 57): a detrimental DRM of any member applies to the whole FG.
        Assert.Equal(2, ScenarioA1Stacking.GroupAttackPenalty([new StackCount(3, 0, 0, 0, 0), new StackCount(5, 0, 0, 0, 0), new StackCount(4, 0, 0, 0, 0)]));
        Assert.Null(ScenarioA1Stacking.GroupAttackPenalty([new StackCount(3, 0, 0, 0, 1)]));
        Assert.Null(ScenarioA1Stacking.GroupAttackPenalty([]));
    }

    [Fact]
    public void AnEntryInTheMphPaysOneMfForEachExcessSquadEquivalent()
    {
        // A5.11 and its example (p. 52): two MF for the building and one for the Location the entry overstacks.
        var building = new InfantryEntry(4, "stone-building", false, false, false, false);
        EntryCostFacts Facts(int excess, bool encircled = false) =>
            new(building, encircled, false, false, false, false, false, 0, false, false, [new EntryMoverFacts(false, 0, false)], excess);
        Assert.Equal((null, 4), ScenarioA1MovementCalculator.EntryCost(Facts(0), (_, _, _) => 4));
        Assert.Equal((null, 6), ScenarioA1MovementCalculator.EntryCost(Facts(1), (_, _, _) => 4));
        Assert.Equal((null, 8), ScenarioA1MovementCalculator.EntryCost(Facts(2), (_, _, _) => 4));

        // A7.7 doubles the entry, not the overstacking MF, as in the advance.
        Assert.Equal((null, 10), ScenarioA1MovementCalculator.EntryCost(Facts(1, encircled: true), (_, _, _) => 4));
    }

    [Fact]
    public void AVehiclePaysOneMpForEachExcessSquadEquivalentOfItsSidesPersonnel()
    {
        // A5.11 (p. 52), in a Bypass of a new hex; the outright entry adds the same half MP beside D2.14's.
        var plain = ScenarioA1VehicleTerrainCosts.BypassHalfMp(1, 0, false, true, () => 0, false, "fully-tracked");
        Assert.Equal(plain + 4, ScenarioA1VehicleTerrainCosts.BypassHalfMp(1, 0, false, true, () => 0, false, "fully-tracked", 0, 4));

        // Not when the vehicle is already in the hex.
        Assert.Equal(ScenarioA1VehicleTerrainCosts.BypassHalfMp(1, 0, false, false, () => 0, false, "fully-tracked"),
            ScenarioA1VehicleTerrainCosts.BypassHalfMp(1, 0, false, false, () => 0, false, "fully-tracked", 0, 4));
    }

    [Fact]
    public void AMoveSaysWhatTheOverstackedLocationCost()
    {
        var verdict = ScenarioA1MovementCalculator.MoveStep(new MoveStepFacts(["g1"], "bd01:F4:0", "bd01:F5:0", false, null, false, "stone-building", null, 6, false, false,
            false, [], false, false, false, false, [], [], 1));
        Assert.Contains("for 3 MF, 1 of them for a Location overstacked by 1 squad-equivalent(s) (A5.11)", verdict.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void AWoodsRoadHexHasARoadAtItsCenterAndWoodsOnAHexsideLocation()
    {
        // B13.31 (p. 128; ruling R40.4), named as VASL names it.
        Assert.True(ScenarioA1Definitions.IsWoodsRoadHex(true, ["Woods", "Dirt Road", "Woods", "Woods", "Woods", "Dirt Road"]));
        Assert.False(ScenarioA1Definitions.IsWoodsRoadHex(true, ["Open Ground", "Dirt Road", null, "Hedge", "Open Ground", "Dirt Road"]));
        Assert.False(ScenarioA1Definitions.IsWoodsRoadHex(false, ["Woods", "Woods", "Woods", "Woods", "Woods", "Woods"]));
    }

    [Fact]
    public void AcrossARoadHexsideTheMoverPaysTheRoadsCostOrTheOtherTerrains()
    {
        // A4.132 (p. 48): "must pay either the road entry cost or the cost of other terrain in the hex".
        var roadHexside = new CrossedHexsideFacts(false, false, null, true, "Dirt Road");
        var plainHexside = new CrossedHexsideFacts(false, false, null, false, "Woods");
        (InfantryEntry? Entry, string? Reason) Step(CrossedHexsideFacts crossed, string terrain, bool terrainRate, int smoke = 0) =>
            ScenarioA1TerrainCosts.GroundStep(crossed, terrain, 0, 7, () => smoke, (_, road, _) => (0, road), terrainRate);

        var onRoad = Step(roadHexside, "woods", false).Entry!;
        Assert.Equal((2, true, true), (onRoad.HalfMf, onRoad.RoadRate, onRoad.OnRoad));
        var inWoods = Step(roadHexside, "woods", true).Entry!;
        Assert.Equal((4, false, false), (inWoods.HalfMf, inWoods.RoadRate, inWoods.OnRoad));

        // SMOKE ends the Road Bonus (B3.4) and not the unit's place on the road: A4.132 negates the FFMO there, not the choice made.
        var smoky = Step(roadHexside, "woods", false, smoke: 2).Entry!;
        Assert.Equal((4, false, true), (smoky.HalfMf, smoky.RoadRate, smoky.OnRoad));

        // A road through Open Ground has no other terrain, so nothing is on the road "instead": the hex is Open Ground either way.
        Assert.False(Step(roadHexside, "open-ground", false).Entry!.OnRoad);
        Assert.StartsWith("play.move-terrain-rate:", Step(roadHexside, "open-ground", true).Reason, StringComparison.Ordinal);
        Assert.StartsWith("play.move-terrain-rate:", Step(plainHexside, "woods", true).Reason, StringComparison.Ordinal);
        Assert.Equal(4, Step(plainHexside, "woods", false).Entry!.HalfMf);
    }

    [Fact]
    public void AMoverAtTheRoadRateIsInOpenGroundOnlyForFirersThatReachATargetPointClear()
    {
        // A4.132's example (p. 48): the center dot along the road ([G2]); the hexside crossed, for a firer beside it ([H4]); neither from behind
        // the woods ([I5]). A7.52: one member's worst case is the group's. When the map cannot say, the Location's terrain stands.
        Assert.Equal("open-ground", ScenarioA1Definitions.RoadRateTargetTerrain("woods", [(false, false)]));
        Assert.Equal("open-ground", ScenarioA1Definitions.RoadRateTargetTerrain("woods", [(true, false)]));
        Assert.Equal("woods", ScenarioA1Definitions.RoadRateTargetTerrain("woods", [(true, true)]));
        Assert.Equal("woods", ScenarioA1Definitions.RoadRateTargetTerrain("woods", [(false, false), (true, true)]));
        Assert.Equal("wooden-building", ScenarioA1Definitions.RoadRateTargetTerrain("wooden-building", [(null, null)]));
        Assert.Equal("woods", ScenarioA1Definitions.RoadRateTargetTerrain("woods", []));
    }

    [Fact]
    public void AMoveSaysItIsAtTheRoadRate()
    {
        var verdict = ScenarioA1MovementCalculator.MoveStep(new MoveStepFacts(["g1"], "bd05:H3:0", "bd05:I4:0", false, null, false, "woods", null, 2, false, false,
            false, [], true, false, false, false, [], [], 0, true));
        Assert.Contains("for 1 MF, at the road rate: on the road, not in the hex's other terrain (A4.132)", verdict.Summary, StringComparison.Ordinal);
    }

    private static DashStepFacts DashStep(string? pending = null, string? declared = "bd04:AA7:0", string to = "bd04:BB7:0", bool firstStep = true, bool spent = false,
        bool assault = false, bool special = false, bool road = true, bool adjacentToRoad = true, bool isOrigin = false, bool adjacentToOrigin = false) =>
        new(pending, declared, to, firstStep, spent, assault, false, false, false, false, false, false, false, special, road, true, adjacentToRoad, isOrigin, adjacentToOrigin);

    [Fact]
    public void ADashIsDeclaredWithTheFirstStepThroughARoadToItsFarSide()
    {
        // A4.63 (p. 51): "declares a Dash move to a particular Location prior to moving"; "may not have expended any MF prior to the Dash".
        Assert.Null(ScenarioA1Dash.StepBar(DashStep()));
        Assert.Null(ScenarioA1Dash.StepBar(DashStep(declared: null)));
        Assert.StartsWith("play.move-dash:", ScenarioA1Dash.StepBar(DashStep(firstStep: false)), StringComparison.Ordinal);
        Assert.StartsWith("play.move-dash:", ScenarioA1Dash.StepBar(DashStep(spent: true)), StringComparison.Ordinal);
        Assert.StartsWith("play.move-dash:", ScenarioA1Dash.StepBar(DashStep(assault: true)), StringComparison.Ordinal);
        Assert.Contains("has no road", ScenarioA1Dash.StepBar(DashStep(road: false)), StringComparison.Ordinal);

        // "Directly into the road and then directly into a ... Location on the other side of the road" (ruling R40.7).
        Assert.Contains("is not across the road", ScenarioA1Dash.StepBar(DashStep(adjacentToRoad: false)), StringComparison.Ordinal);
        Assert.Contains("is not across the road", ScenarioA1Dash.StepBar(DashStep(isOrigin: true)), StringComparison.Ordinal);
        Assert.Contains("is not across the road", ScenarioA1Dash.StepBar(DashStep(adjacentToOrigin: true)), StringComparison.Ordinal);
    }

    [Fact]
    public void OnceADashIsDeclaredItsOnlyStepIsIntoTheLocationDashedTo()
    {
        // A4.63 (p. 52): "Once a Dash is declared, no other movement/MF-expenditure options are available"; it "must end its MPh" there.
        Assert.Null(ScenarioA1Dash.StepBar(DashStep(pending: "bd04:AA7:0", declared: null, to: "bd04:AA7:0", firstStep: false, spent: true)));
        Assert.StartsWith("play.move-dash:", ScenarioA1Dash.StepBar(DashStep(pending: "bd04:AA7:0", declared: null, to: "bd04:BB6:0", firstStep: false, spent: true)), StringComparison.Ordinal);
        Assert.StartsWith("play.move-dash:", ScenarioA1Dash.StepBar(DashStep(pending: "bd04:AA7:0", declared: null, to: "bd04:AA7:0", firstStep: false, spent: true, assault: true)),
            StringComparison.Ordinal);
        Assert.StartsWith("play.move-dash:", ScenarioA1Dash.StepBar(DashStep(pending: "bd04:AA7:0", declared: null, to: "bd04:BB7:0", firstStep: false, spent: true, special: true)),
            StringComparison.Ordinal);
        Assert.StartsWith("play.end-move-dash:", ScenarioA1Dash.EndBar("bd04:AA7:0", true), StringComparison.Ordinal);

        // "Unless it becomes berserk, pinned, broken or wounded in the road Location ... in which case Dash benefits immediately cease."
        Assert.Null(ScenarioA1Dash.EndBar("bd04:AA7:0", false));
        Assert.Null(ScenarioA1Dash.EndBar(null, true));
        Assert.True(ScenarioA1Dash.BenefitsHold([(false, false, false)]));
        Assert.False(ScenarioA1Dash.BenefitsHold([(false, false, false), (false, true, false)]));
        Assert.False(ScenarioA1Dash.BenefitsHold([(true, false, false)]));
        Assert.False(ScenarioA1Dash.BenefitsHold([(false, false, true)]));
    }

    [Fact]
    public void ADashQualifiesAgainstAFirerToWhomNeitherSideOfTheRoadIsOpenGround()
    {
        // The example of A4.63 (p. 51): halved for the 4-4-7 in Y9, behind hedges from both AA7 and CC8; not for the 6-2-8 in CC6, "because AA7 is
        // Open Ground to it". A7.52: one qualifying Location of a Fire Group makes the group's fire Area Fire.
        Assert.True(ScenarioA1Dash.QualifiesAgainst([(false, false)]));
        Assert.False(ScenarioA1Dash.QualifiesAgainst([(false, true)]));
        Assert.False(ScenarioA1Dash.QualifiesAgainst([(true, false)]));
        Assert.True(ScenarioA1Dash.QualifiesAgainst([(false, true), (false, false)]));
        Assert.False(ScenarioA1Dash.QualifiesAgainst([]));

        // Open Ground to a firer: the terrain, a clear LOS with no Hindrance, and no TEM against that firer. "Any Location out of the LOS of a
        // firer is also considered a non-Open Ground Location."
        Assert.True(ScenarioA1Dash.OpenGroundTo(true, true, 0, RoutCoverFacts.None));
        Assert.False(ScenarioA1Dash.OpenGroundTo(false, true, 0, RoutCoverFacts.None));
        Assert.False(ScenarioA1Dash.OpenGroundTo(true, false, 0, RoutCoverFacts.None));
        Assert.False(ScenarioA1Dash.OpenGroundTo(true, true, 1, RoutCoverFacts.None));
        Assert.False(ScenarioA1Dash.OpenGroundTo(true, true, 0, new RoutCoverFacts(null, 0, true, false, false)));
    }

    [Fact]
    public void OrdnanceTakesCaseJAgainstADashingTargetAndNoCaseJ3OrJ4()
    {
        // A4.63 (p. 52), C6.1 (p. 173).
        var dashing = new OrdnanceMovement(null, true, true, 1, 0) { Dashing = true };
        Assert.Equal([("case-j", 2m)], [.. ScenarioA1OrdnanceCalculator.MoverDrm(dashing).Select(item => (item.Name, item.Value))]);
        var walking = new OrdnanceMovement(null, true, true, 1, 0);
        Assert.Equal(["case-j3", "case-j4"], [.. ScenarioA1OrdnanceCalculator.MoverDrm(walking).Select(item => item.Name)]);
    }

    [Fact]
    public void AMoveSaysItIsADash()
    {
        var verdict = ScenarioA1MovementCalculator.MoveStep(new MoveStepFacts(["g1"], "bd04:CC8:0", "bd04:BB7:0", false, null, false, "open-ground", null, 4, false, false,
            false, [], false, false, false, false, [], [], 0, false, "bd04:AA7:0"));
        Assert.Contains("for 2 MF, Dashing to bd04:AA7:0", verdict.Summary, StringComparison.Ordinal);
        Assert.Equal(["g1"], ScenarioA1MovementCalculator.EndingMembers(false, false, ["g1"], null, null, dashArrived: true));
        Assert.Empty(ScenarioA1MovementCalculator.EndingMembers(false, false, ["g1"], null, null));
    }

    private static MovingUnitFacts Mover(string id, bool leader = false) =>
        new(id, "german", false, !leader, leader, false, false, false, "german", 0, false, null);

    [Fact]
    public void TheLeadersIpcGoesToTheLadenMmcTheMoverNames()
    {
        // A4.42 (p. 50; ruling R40.11): "one SMC can add its IPC to that of any one Good Order Infantry unit".
        MovingUnitFacts[] stack = [Mover("s1"), Mover("s2"), Mover("l", leader: true)];
        Assert.Null(ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, _ => true).Recipient);
        Assert.Equal(("s2", "l"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, _ => true, "s2"));
        Assert.Equal(("s1", "l"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0));
        Assert.Null(ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0, "s2").Recipient);

        // The choice holds for the whole move, and names a laden MMC with the leader.
        Assert.Null(ScenarioA1MovementCalculator.IpcChoiceBar(null, "s1", "s1"));
        Assert.Null(ScenarioA1MovementCalculator.IpcChoiceBar("s1", "s1", "s1"));
        Assert.Null(ScenarioA1MovementCalculator.IpcChoiceBar("s1", null, "s1"));
        Assert.StartsWith("play.move-ipc:", ScenarioA1MovementCalculator.IpcChoiceBar("s2", "s1", "s1"), StringComparison.Ordinal);
        Assert.StartsWith("play.move-ipc:", ScenarioA1MovementCalculator.IpcChoiceBar("s2", null, null), StringComparison.Ordinal);
    }

    [Fact]
    public void AnItemIsPortagedOnceAPhaseAndItsCostIsNotRecouped()
    {
        // A4.4 (p. 50).
        var after = ScenarioA1MovementCalculator.Portaged([("g1", "mmg")], [("g1", "mmg"), ("g2", "lmg")]);
        Assert.Equal([("g1", "mmg"), ("g2", "lmg")], after);
        Assert.Equal([("g1", "mmg")], ScenarioA1MovementCalculator.Portaged([("g1", "mmg")], [("g2", "mmg")]));
        Assert.Null(ScenarioA1MovementCalculator.PortagedTwiceBar([("mmg", "g1", "g1"), ("lmg", "g2", null)]));
        Assert.StartsWith("play.move-portage:", ScenarioA1MovementCalculator.PortagedTwiceBar([("mmg", "g2", "g1")]), StringComparison.Ordinal);
    }

    [Fact]
    public void AssaultMovementAllowsMfSpentInsideALocationBesideItsOneChangeOfLocation()
    {
        // A4.61 and its EXC (p. 51): one change of Location; MF spent within a Location, before or after it, do not prevent Assault Movement.
        Assert.Null(ScenarioA1MovementCalculator.MoveTiming(true, true, true, false, null, [], currentChangedLocation: false, stepChangesLocation: true));
        Assert.Null(ScenarioA1MovementCalculator.MoveTiming(true, true, true, false, null, [], currentChangedLocation: true, stepChangesLocation: false));
        Assert.StartsWith("play.move-assault:", ScenarioA1MovementCalculator.MoveTiming(true, true, true, false, null, [], currentChangedLocation: true, stepChangesLocation: true),
            StringComparison.Ordinal);
        Assert.StartsWith("play.move-assault:", ScenarioA1MovementCalculator.MoveTiming(true, true, false, false, null, [], currentChangedLocation: false, stepChangesLocation: true),
            StringComparison.Ordinal);
        Assert.StartsWith("play.move-assault:", ScenarioA1MovementCalculator.MoveTiming(true, false, true, false, null, [], currentChangedLocation: false, stepChangesLocation: true),
            StringComparison.Ordinal);

        // "Which breaks, or becomes berserk ... is no longer considered using Assault Movement."
        Assert.True(ScenarioA1FireEligibility.AssaultHolds(true, [(false, false)]));
        Assert.False(ScenarioA1FireEligibility.AssaultHolds(true, [(true, false)]));
        Assert.False(ScenarioA1FireEligibility.AssaultHolds(true, [(false, false), (false, true)]));
        Assert.False(ScenarioA1FireEligibility.AssaultHolds(false, [(false, false)]));
    }

    [Fact]
    public void AMovedMmgOrHmgDoesNotFireInTheAfph()
    {
        // A4.41 (p. 50): a LMG may; the phase is the AFPh alone.
        Assert.Equal("medium", new ScenarioA1FirePackage().Reference.Definitions["attacker-mmg"].Size);
        Assert.StartsWith("play.fire-weapon-moved:", ScenarioA1FireEligibility.MovedWeaponBar("afph", [("mmg", true, "medium")]), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-weapon-moved:", ScenarioA1FireEligibility.MovedWeaponBar("afph", [("lmg", true, "light"), ("hmg", true, "heavy")]), StringComparison.Ordinal);
        Assert.Null(ScenarioA1FireEligibility.MovedWeaponBar("afph", [("lmg", true, "light"), ("mmg", false, "medium")]));
        Assert.Null(ScenarioA1FireEligibility.MovedWeaponBar("dfph", [("mmg", true, "medium")]));
    }

    [Fact]
    public void ABypassingUnitRecoversNothingAndAUnitThatMountsHasFourMf()
    {
        // A4.44 (p. 50); A4.11 (p. 48) and A4.5 (p. 51).
        Assert.StartsWith("play.recover-bypass:", ScenarioA1SupportWeaponRules.RecoveryBypassBar("g1", true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1SupportWeaponRules.RecoveryBypassBar("g1", false));
        Assert.Equal(4, ScenarioA1PassengerCalculator.ConveyanceAllowance);
        Assert.StartsWith("play.load:", ScenarioA1PassengerCalculator.BoardingDoubleTimeBar("g1", 2), StringComparison.Ordinal);
        Assert.Null(ScenarioA1PassengerCalculator.BoardingDoubleTimeBar("g1", 0));
    }

    private static InfantryOverrunMoverFacts OvrMover(string id, bool mmc = true, bool leader = false, bool goodOrder = true, bool berserk = false) =>
        new(id, mmc, leader, goodOrder, berserk, false, false);

    private static InfantryOverrunDefenderFacts OvrDefender(string id, bool known = true, bool smc = true, bool melee = false, bool unarmed = false, bool vehicle = false) =>
        new(id, known, smc, melee, unarmed, vehicle);

    [Fact]
    public void AnInfantryOvrsNtcIsItsLeadersForTheStackOrItsOneMmcs()
    {
        // A4.15 (p. 49).
        Assert.Equal("s1", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1")]).Taker);
        Assert.Equal("l", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1"), OvrMover("s2"), OvrMover("l", mmc: false, leader: true)]).Taker);
        Assert.Contains("name one MMC at a time", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1"), OvrMover("s2")]).Refusal, StringComparison.Ordinal);
        Assert.Contains("a SMC alone makes none", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("l", mmc: false, leader: true)]).Refusal, StringComparison.Ordinal);
        Assert.Contains("berserk", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1", berserk: true)]).Refusal, StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1", goodOrder: false)]).Refusal);

        // The DRM is the TEM of the enemy's Location, and a hexside's when the LOS crosses it; the check passes at or below the Morale Level.
        Assert.Equal(2, ScenarioA1InfantryOverrun.NtcDrm(2, 1));
        Assert.Equal(2, ScenarioA1InfantryOverrun.NtcDrm(1, 2));
        Assert.Equal(0, ScenarioA1InfantryOverrun.NtcDrm(-1, 0));

        // Each MMC under a passed NTC whose taker enters with it: a leader left behind covers no one, and MMC that each passed their own go together.
        (IReadOnlyList<string> Units, string? Taker, bool Passed)[] byLeader = [(new[] { "s1", "s2", "l" }, "l", true)];
        Assert.True(ScenarioA1InfantryOverrun.NtcCovers(["s1", "s2"], ["s1", "s2", "l"], byLeader));
        Assert.False(ScenarioA1InfantryOverrun.NtcCovers(["s1", "s2"], ["s1", "s2"], byLeader));
        (IReadOnlyList<string> Units, string? Taker, bool Passed)[] each = [(new[] { "s1" }, "s1", true), (new[] { "s2" }, "s2", true), (new[] { "s3" }, "s3", false)];
        Assert.True(ScenarioA1InfantryOverrun.NtcCovers(["s1", "s2"], ["s1", "s2"], each));
        Assert.False(ScenarioA1InfantryOverrun.NtcCovers(["s1", "s3"], ["s1", "s3"], each));
        Assert.False(ScenarioA1InfantryOverrun.NtcCovers([], ["l"], byLeader));
        Assert.Equal(1, ScenarioA1InfantryOverrun.NtcDrm(1, 0));
        Assert.True(ScenarioA1InfantryOverrun.NtcPassed(3, 3, 1, 7));
        Assert.False(ScenarioA1InfantryOverrun.NtcPassed(3, 4, 1, 7));
        Assert.True(ScenarioA1InfantryOverrun.EndsStackMove(true, false));
        Assert.False(ScenarioA1InfantryOverrun.EndsStackMove(false, false));
        Assert.False(ScenarioA1InfantryOverrun.EndsStackMove(true, true));
        Assert.StartsWith("play.overrun-ntc:", ScenarioA1InfantryOverrun.AlreadyTakenBar(true, "x"), StringComparison.Ordinal);
        Assert.Null(ScenarioA1InfantryOverrun.AlreadyTakenBar(false, "x"));
    }

    [Fact]
    public void AnInfantryOvrEntersOnlyALoneKnownSmcsLocationOnAPassedNtcAtDoubleTheMf()
    {
        // A4.15 (p. 49): "a Location containing only one Known enemy SMC (unless that SMC occupies an AFV)".
        Assert.Null(ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l")], "x"));
        Assert.Null(ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l"), OvrDefender("hidden", known: false, smc: false)], "x"));
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("s", smc: false)], "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l"), OvrDefender("l2")], "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l", melee: true)], "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([], "x"), StringComparison.Ordinal);
        Assert.Null(ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l"), OvrDefender("tank", known: false, smc: false, vehicle: true)], "x"));
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l", vehicle: true)], "x"), StringComparison.Ordinal);

        Assert.Null(ScenarioA1InfantryOverrun.EntryBar(true, false, false, false, false, false, "x"));
        Assert.Contains("first passes its NTC", ScenarioA1InfantryOverrun.EntryBar(false, false, false, false, false, false, "x"), StringComparison.Ordinal);
        Assert.Contains("was failed", ScenarioA1InfantryOverrun.EntryBar(false, true, false, false, false, false, "x"), StringComparison.Ordinal);
        Assert.Contains("Minimum Move", ScenarioA1InfantryOverrun.EntryBar(true, false, true, false, false, false, "x"), StringComparison.Ordinal);
        Assert.Contains("berserk", ScenarioA1InfantryOverrun.EntryBar(true, false, false, false, false, true, "x"), StringComparison.Ordinal);
        Assert.Equal(8, ScenarioA1InfantryOverrun.EntryHalfMf(4));

        // A4.14 with the OVR: the Known SMC is no bar and is not drawn for a reveal; a concealed unit there still is (A12.15).
        UnitAtTargetFacts[] there = [new("l", true, "russian", false, true, false), new("q", true, "russian", false, false, false)];
        var barred = ScenarioA1MovementCalculator.EnemiesAtTarget(there, "german", false, false, false, false, false, "g1", "x");
        Assert.StartsWith("play.move-occupied:", barred.Refusal, StringComparison.Ordinal);
        var entered = ScenarioA1MovementCalculator.EnemiesAtTarget(there, "german", false, false, false, false, false, "g1", "x", knownEntered: true);
        Assert.Null(entered.Refusal);
        Assert.Equal(["q"], entered.EnemiesThere);
    }

    [Fact]
    public void UnarmedUnitsAreEnteredAndTheCcOfAnEntryIsOwedAtOnce()
    {
        // A20.54 (p. 88): "Unarmed units are not an obstacle to movement". A4.152, A20.54: the CC at once.
        Assert.True(ScenarioA1InfantryOverrun.OnlyUnarmed([OvrDefender("u", smc: false, unarmed: true)]));
        Assert.False(ScenarioA1InfantryOverrun.OnlyUnarmed([OvrDefender("u", smc: false, unarmed: true), OvrDefender("s", smc: false)]));
        Assert.False(ScenarioA1InfantryOverrun.OnlyUnarmed([]));

        // A20.54: an unarmed unit "may not enter a Known enemy-occupied Location", and Dummies enter none.
        Assert.True(ScenarioA1InfantryOverrun.MayRecapture(false, false));
        Assert.False(ScenarioA1InfantryOverrun.MayRecapture(true, false));
        Assert.False(ScenarioA1InfantryOverrun.MayRecapture(false, true));
        Assert.True(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("l")]));
        Assert.True(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("u", smc: false, unarmed: true), OvrDefender("u2", smc: false, unarmed: true)]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(false, [OvrDefender("l")]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("l", melee: true)]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("l"), OvrDefender("q", known: false)]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(true, []));
        var attacks = ScenarioA1VehicleCloseCombatRules.EntryAttacks(["g1", "g2"], ["l"], ["l"]);
        Assert.Equal(2, attacks.Count);
        Assert.Equal(["g1", "g2"], attacks[0].Attackers);
        Assert.Equal(["l"], attacks[1].Attackers);
    }

    [Fact]
    public void TheSmcMayMoveAwayBeforeAnyFireIntoALocationTheAttackerNames()
    {
        // A4.151 (p. 49).
        Assert.Null(ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, true, false, false, false, 2));
        Assert.Contains("before any Defensive First Fire", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, true, true, true, false, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("broken, pinned", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, true, true, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("broken, pinned", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, false, false, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("no adjacent Accessible Location", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, true, false, false, false, 0), StringComparison.Ordinal);
        Assert.Contains("is not a SMC", ScenarioA1InfantryOverrun.EvadeBar("s", true, true, false, false, true, false, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("is not a SMC", ScenarioA1InfantryOverrun.EvadeBar("l", false, true, false, true, true, false, false, false, 2), StringComparison.Ordinal);

        // "The ATTACKER may not force the SMC into ... [an] Open Ground hex if an alternate choice is available."
        Assert.Equal(["woods"], ScenarioA1InfantryOverrun.EvadeDestinations([("open", true, true, false, true), ("woods", true, true, false, false)]));
        Assert.Equal(["open", "open2"], ScenarioA1InfantryOverrun.EvadeDestinations([("open", true, true, false, true), ("open2", true, true, false, true)]));

        // "Accessible ... not occupied by an enemy unit": a Location it could not enter, one off the playable area, and one an enemy unit holds are left out.
        Assert.Equal(["open"], ScenarioA1InfantryOverrun.EvadeDestinations([("open", true, true, false, true), ("cliff", false, true, false, false),
            ("off", true, false, false, false), ("held", true, true, true, false)]));
        Assert.DoesNotContain("[", ScenarioA1InfantryOverrun.EvadeText("l", ["[C8]"]), StringComparison.Ordinal);
        Assert.Empty(ScenarioA1InfantryOverrun.EvadeDestinations([]));
    }

    private static MandatoryDefenderFacts Owing(string id, bool infantry = true, bool smc = false, bool inherent = true, int weapons = 0, bool unarmed = false, bool broken = false,
        bool melee = false, bool captured = false, bool marked = false, bool attacked = false) =>
        new(id, infantry, smc, inherent, weapons, unarmed, broken, melee, captured, marked, attacked);

    [Fact]
    public void AnArmedUnbrokenInfantryDefenderNotInMeleeOwesItsAttackOnAnMmcThatEntersItsLocation()
    {
        // A8.312 (p. 61).
        MandatoryDefenderFacts[] there = [Owing("s1"), Owing("s2", marked: true), Owing("b", broken: true), Owing("m", melee: true), Owing("u", unarmed: true),
            Owing("p", captured: true), Owing("l", smc: true, inherent: false), Owing("lm", smc: true, inherent: false, weapons: 1),
            Owing("lmf", smc: true, inherent: false, weapons: 1, marked: true), Owing("done", attacked: true), Owing("cannot"), Owing("v", infantry: false), Owing("sm", weapons: 1, marked: true)];
        var asked = new List<string>();
        bool MayAttack(string id)
        {
            asked.Add(id);
            return id != "cannot";
        }

        Assert.Equal(["s1", "s2", "lm", "sm"], ScenarioA1MandatoryFire.Owed(true, true, true, true, there, MayAttack));

        // The fire plan is asked only of a unit that would otherwise owe the attack.
        Assert.Equal(["s1", "s2", "lm", "cannot", "sm"], asked);

        // A unit already marked fires every MG its size operates with its own FP (A8.3, A8.31); one with no FP of its own, one MG; else none.
        Assert.Equal(2, ScenarioA1MandatoryFire.WeaponsFired(true, true, true, 3));
        Assert.Equal(1, ScenarioA1MandatoryFire.WeaponsFired(false, true, true, 2));
        Assert.Equal(0, ScenarioA1MandatoryFire.WeaponsFired(true, true, false, 2));
        Assert.Equal(1, ScenarioA1MandatoryFire.WeaponsFired(false, false, false, 2));
        Assert.Equal(0, ScenarioA1MandatoryFire.WeaponsFired(true, true, true, 0));

        // Only in the MPh, with the DEFENDER's window open, on the step that entered, and for an Infantry or Cavalry MMC.
        Assert.Empty(ScenarioA1MandatoryFire.Owed(false, true, true, true, there, MayAttack));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, false, true, true, there, MayAttack));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, false, true, there, MayAttack));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, true, false, there, MayAttack));
    }

    [Fact]
    public void WhileTheAttackIsOwedThePassIsRefusedAndTheDefendersFireIsTheMandatoryFgs()
    {
        // A8.312, A7.55 (pp. 61, 57).
        string[] owed = ["s1", "s2"];
        Assert.Null(ScenarioA1MandatoryFire.PassBar([], true, "x"));
        Assert.Contains("as one Mandatory FG", ScenarioA1MandatoryFire.PassBar(owed, true, "x"), StringComparison.Ordinal);
        Assert.Contains("ruling R40.17", ScenarioA1MandatoryFire.PassBar(owed, false, "x"), StringComparison.Ordinal);

        Assert.Null(ScenarioA1MandatoryFire.FireBar([], true, ["other"], "y", "x", []));
        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, ["s1", "s2"], "x", "x", []));
        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, ["s1", "s2", "leader"], "x", "x", []));
        Assert.Contains("s2 must join", ScenarioA1MandatoryFire.FireBar(owed, true, ["s1"], "x", "x", []), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-mandatory:", ScenarioA1MandatoryFire.FireBar(owed, true, ["s1", "s2"], "y", "x", []), StringComparison.Ordinal);

        // Where the fire rules form no group of them (a unit with no counter beside one marked First Fire), one of them attacks, and no one else with it.
        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, false, ["s1"], "x", "x", []));
        Assert.StartsWith("play.fire-mandatory:", ScenarioA1MandatoryFire.FireBar(owed, false, ["other"], "x", "x", []), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-mandatory:", ScenarioA1MandatoryFire.FireBar(owed, false, ["s1", "other"], "x", "x", []), StringComparison.Ordinal);

        // Ruling R40.17: a unit that could have fired in one group with those named must join them.
        Assert.Contains("s3 can fire in one group", ScenarioA1MandatoryFire.FireBar(["s1", "s2", "s3"], false, ["s1"], "x", "x", ["s3"]), StringComparison.Ordinal);

        Assert.Contains("as one Mandatory FG", ScenarioA1MandatoryFire.Note(owed, true, "[C9]"), StringComparison.Ordinal);
        Assert.Contains("ruling R40.17", ScenarioA1MandatoryFire.Note(owed, false, "[C9]"), StringComparison.Ordinal);
        Assert.Contains("(A8.312)", ScenarioA1MandatoryFire.HandOverText("[C9]"), StringComparison.Ordinal);

        // A5.13 to A5.132 (p. 52): the target's overstacking penalty is the movers' in the MPh, and Personnel's.
        Assert.True(ScenarioA1Stacking.TargetPenaltyApplies(false, false, true));
        Assert.True(ScenarioA1Stacking.TargetPenaltyApplies(true, true, true));
        Assert.False(ScenarioA1Stacking.TargetPenaltyApplies(true, false, true));
        Assert.False(ScenarioA1Stacking.TargetPenaltyApplies(false, false, false));
    }

    [Fact]
    public void ACrewThatFiredItsGunAddsItsInherentFpToFpfOnly()
    {
        // A8.311 (p. 61) beside A7.352 (p. 56; ruling R8.4).
        Assert.True(ScenarioA1MandatoryFire.CrewAddsInherentFp(false, false));
        Assert.False(ScenarioA1MandatoryFire.CrewAddsInherentFp(true, false));
        Assert.True(ScenarioA1MandatoryFire.CrewAddsInherentFp(true, true));
    }

    [Fact]
    public void GuardsAndUnarmedUnitsNeedNoLeaderAndAGuardSquadDeploysAtWill()
    {
        // A1.31, A1.32 (p. 45); A20.5 (p. 88).
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(true, false));
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, true));
        Assert.False(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, false));
        Assert.True(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, true, false));
        Assert.False(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, true, true));
        Assert.False(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(false, true, false));
        Assert.False(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, false, false));

        // A1.32: "at the start of any RPh".
        Assert.Null(ScenarioA1SupportWeaponRules.RecombineStartBar(false));
        Assert.StartsWith("play.recombine-start:", ScenarioA1SupportWeaponRules.RecombineStartBar(true), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("rph", true, true, false, true, false)]
    [InlineData("rph", false, true, false, true, false)]
    [InlineData("pfph", true, false, false, false, true)]
    [InlineData("pfph", false, false, false, false, false)]
    [InlineData("mph", true, false, true, true, false)]
    [InlineData("mph", false, false, false, false, false)]
    [InlineData("dfph", false, false, false, false, true)]
    [InlineData("dfph", true, false, false, false, false)]
    [InlineData("aph", true, true, true, false, false)]
    [InlineData("aph", false, false, false, false, false)]
    [InlineData("rtph", true, false, false, false, false)]
    [InlineData("ccph", true, false, true, false, false)]
    [InlineData("ccph", false, false, true, false, false)]
    [InlineData(null, true, false, false, false, false)]
    public void ASupportWeaponActsOnlyInThePhasesTheRulesAllow(string? phase, bool phasing, bool transfer, bool drop, bool recover, bool dismantle)
    {
        // A4.431 (transfer), A4.43 (drop), A4.44 (recover) (p. 50); A9.8 (dismantle or assemble, p. 67): the Play streamlining plan, S7 (R-PS.3).
        Assert.Equal(transfer, ScenarioA1SupportWeaponRules.TransferPhase(phase, phasing));
        Assert.Equal(drop, ScenarioA1SupportWeaponRules.DropPhase(phase, phasing));
        Assert.Equal(recover, ScenarioA1SupportWeaponRules.RecoverPhase(phase, phasing));
        Assert.Equal(dismantle, ScenarioA1SupportWeaponRules.DismantlePhase(phase, phasing));
    }

    [Fact]
    public void TheUnitSizeNumberIsOneMemberWithItsFourAndItsFive()
    {
        // A1.6 (p. 45).
        Assert.Equal(1, ScenarioA1Definitions.UnitSize(false, false));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize(false, true));
        Assert.Equal(3, ScenarioA1Definitions.UnitSize(true, false));
        Assert.Equal(4, ScenarioA1Definitions.UnitSize(false, false, fiveEighths: true));
        Assert.Equal(5, ScenarioA1Definitions.UnitSize(false, false, fiveEighths: true, large: true));
        Assert.Equal(3, ScenarioA1Definitions.UnitSize("asl:squad"));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize("asl:half-squad"));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize("asl:crew"));
        Assert.Equal(1, ScenarioA1Definitions.UnitSize("asl:leader"));
        Assert.Equal(3, ScenarioA1FireEligibility.GuardSize("asl:squad"));
    }

    [Fact]
    public void AButtedHalfHexIsInASetupAreaOnlyUnderBothBoardsNames()
    {
        // A2.3 (p. 45): two boards; the half-hex bd02:A1 is butted against bd04:GG10, and the later board, bd02, owns it.
        var card = new SetupCardFacts([], 2, null, false, null, null, null, null, []);
        SetupAreaFacts[] onFirst = [new("a", "hex-numbers", null, "bd04", 8, 10, null, null, null, null, null)];
        SetupAreaFacts[] onSecond = [new("b", "hex-numbers", null, "bd02", 1, 3, null, null, null, null, null)];
        SetupAreaFacts[] onBoth = [.. onFirst, .. onSecond];
        var full = new SetupLocationFacts("bd04:B9:0", "bd04", "B9", 9);
        var seam = new SetupLocationFacts("bd02:A1:0", "bd02", "A1", 1) { OtherHalf = new SetupLocationFacts("bd04:GG10:0", "bd04", "GG10", 10) };
        var edge = new SetupLocationFacts("bd02:A1:0", "bd02", "A1", 1);

        Assert.True(ScenarioA1SetupCalculator.InAreas(card, onFirst, full));
        Assert.False(ScenarioA1SetupCalculator.InAreas(card, onSecond, full));

        // A unit limited to either board alone may not set up in the butted half-hex; one allowed on both may.
        Assert.False(ScenarioA1SetupCalculator.InAreas(card, onFirst, seam));
        Assert.False(ScenarioA1SetupCalculator.InAreas(card, onSecond, seam));
        Assert.True(ScenarioA1SetupCalculator.InAreas(card, onBoth, seam));

        // "... or is not butted against another half-hex".
        Assert.True(ScenarioA1SetupCalculator.InAreas(card, onSecond, edge));
    }
}
