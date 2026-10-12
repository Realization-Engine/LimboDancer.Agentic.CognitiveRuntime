using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 40, read directly in Rules. Task 40.3: Dash (A4.63, C6.1, pp. 51 to 52, 173). Task 40.2: a woods-road hex, the mover's choice across a road hexside, and the terrain a firer reads for a mover at
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
        Assert.Contains("not on the other side of the road", ScenarioA1Dash.StepBar(DashStep(adjacentToRoad: false)), StringComparison.Ordinal);
        Assert.Contains("not on the other side of the road", ScenarioA1Dash.StepBar(DashStep(isOrigin: true)), StringComparison.Ordinal);
        Assert.Contains("not on the other side of the road", ScenarioA1Dash.StepBar(DashStep(adjacentToOrigin: true)), StringComparison.Ordinal);
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
}
