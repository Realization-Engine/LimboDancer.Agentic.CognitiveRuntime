"""Pass 40 task 40.3, Dash: the Rules tests and the Play test. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"

PLAY_TEST = '''    [Fact]
    public async Task ADashIsAreaFireInTheRoadLocationAndEndsWhereItWasDeclaredTo()
    {
        // Pass 40, task 40.3 (A4.63 and its example, pp. 51 to 52): from the woods of D5 through the road of D4 to the woods of D3. Neither side of
        // the road is Open Ground to the firer, so its First Fire at the squad in D4 is Area Fire. Once declared, the only step is into D3, and the
        // move ends there. In the control hex, with no Dash declared, the same fire is not halved.
        board.Terrain["D5"] = "Woods";
        board.Terrain["D4"] = "Dirt Road";
        board.Terrain["D3"] = "Woods";
        board.Terrain["E4"] = "Dirt Road";
        await Setup(Squad("g1", L("D5"), "german"), Squad("g2", L("E5"), "german"), Squad("r1", L("B3"), "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("D4"), ["dash"] = L("D3") }));
        Assert.Equal(At("D3"), LastStep(before).Dash);
        Assert.Equal(At("D3"), Current.Movement!.Dash);
        before = Revision;
        Committed(await Fire(R1, L("D4"), false, 6, 6, 6, 6, 6, 6));
        Assert.Contains("dash", Multipliers(LastFire(before)));
        await Pass();
        Refused(await EndMove(), "play.end-move-dash");
        Refused(await Move(G1, L("C4")), "play.move-dash");
        Committed(await Move(G1, L("D3")));
        Assert.Null(Current.Movement!.Dash);
        await Pass();
        Assert.True(Current.Unit("g1")!.MovementEnded);

        // A Dash is declared with the first step, through a road Location, to the far side of it.
        Refused(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G2, ["to"] = L("E4"), ["dash"] = L("E5") }), "play.move-dash");
        Refused(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G2, ["to"] = L("F4"), ["dash"] = L("F3") }), "play.move-dash");
        Committed(await Move(G2, L("E4")));
        before = Revision;
        Committed(await Fire(R1, L("E4"), false, 6, 6, 6, 6, 6, 6));
        Assert.DoesNotContain("dash", Multipliers(LastFire(before)));
        NoReplayErrors();
    }

    [Fact]
    public async Task AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap()'''

path = "LimboDancer.Domains.Asl.Play.Tests/BacklogPass10Tests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public async Task AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap()"
assert text.count(anchor) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(anchor, PLAY_TEST))

RULES_TESTS = '''
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
'''

path = "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
assert text.rstrip().endswith("}")
body = text.rstrip()[:-1].rstrip() + "\n" + RULES_TESTS
old_doc = "/// Pass 40, read directly in Rules. Task 40.2:"
assert body.count(old_doc) == 1
body = body.replace(old_doc, "/// Pass 40, read directly in Rules. Task 40.3: Dash (A4.63, C6.1, pp. 51 to 52, 173). Task 40.2:")
open(ROOT + path, "w", encoding="utf-8", newline="").write(body)

FIRE_TEST = '''    [Fact]
    public void FireAtADashIsAreaFireButNotAlongAFireLane()
    {
        // A4.63 (p. 52; pass 40, task 40.3): kept here for this class's attack helpers.
        var dash = Complete(Moving(Attack([5, 5])) with
        {
            Dash = true,
        });
        Assert.Contains("dash", Multipliers(dash));
        Assert.DoesNotContain("dash", Multipliers(Complete(Moving(Attack([5, 5])))));
    }

    [Fact]
    public void MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem()'''
path = "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass10Tests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public void MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem()"
assert text.count(anchor) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(anchor, FIRE_TEST))
print("ok")
