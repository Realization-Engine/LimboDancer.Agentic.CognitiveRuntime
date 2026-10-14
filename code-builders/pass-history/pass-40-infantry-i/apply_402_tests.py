"""Pass 40 task 40.2: the Play test and its LOS stub's read of the target points, and the Rules tests. Exact blocks, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:70], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


PLAY_TEST = '''    [Fact]
    public async Task AMoverAtTheRoadRateIsOnTheRoadForAFirerThatSeesItAndInTheWoodsForOneThatDoesNot()
    {
        // Pass 40, task 40.2 (A4.132 and its example, p. 48; B13.31): D4 and E4 are woods entered across a road hexside. At the road rate the squad
        // pays one MF and is on the road: FFMO and no woods TEM for the firer whose LOS reaches a target point clear, the woods TEM for the one
        // whose LOS crosses the woods. Paying the woods' cost it is in the woods for both, and the Road Bonus is not its.
        board.Terrain["D4"] = "Woods";
        board.Terrain["E4"] = "Woods";
        board.Side("D5", HexsideDirection.North, terrain: "Dirt Road");
        board.Side("E5", HexsideDirection.North, terrain: "Dirt Road");
        los.CrossesTerrain["D2"] = false;
        los.CrossesTerrain["C3"] = true;
        await Setup(Squad("g1", L("D5"), "german"), Squad("g2", L("E5"), "german"), Squad("r1", L("D2"), "russian"), Squad("r2", L("C3"), "russian"));
        var before = Revision;
        Committed(await Move(G1, L("D4")));
        Assert.Equal(2, LastStep(before).HalfMf);
        Assert.True(LastStep(before).RoadRate);
        before = Revision;
        Committed(await Fire(R1, L("D4"), false, 6, 6, 6, 6, 6, 6));
        Assert.Contains(Drm(LastFire(before)), item => item.Name == "ffmo");
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "tem:woods");
        before = Revision;
        Committed(await Fire(R2, L("D4"), false, 6, 6, 6, 6, 6, 6));
        Assert.Contains(("tem:woods", 1m), Drm(LastFire(before)));
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "ffmo");
        await Pass();
        Committed(await EndMove());

        before = Revision;
        Committed(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G2, ["to"] = L("E4"), ["terrainRate"] = true }));
        Assert.Equal(4, LastStep(before).HalfMf);
        Assert.False(LastStep(before).RoadRate);
        Assert.False(LastStep(before).Road);
        before = Revision;
        Committed(await Fire(R1, L("E4"), false, 6, 6, 6, 6, 6, 6));
        Assert.Contains(("tem:woods", 1m), Drm(LastFire(before)));
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "ffmo");
        NoReplayErrors();
    }

    [Fact]
    public async Task AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap()'''

apply("LimboDancer.Domains.Asl.Play.Tests/BacklogPass10Tests.cs", [
    ("    [Fact]\n    public async Task AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap()", PLAY_TEST),
    ("        public HashSet<string> Blocked { get; } = new(StringComparer.Ordinal);\n",
     "        public HashSet<string> Blocked { get; } = new(StringComparer.Ordinal);\n\n"
     "        /// <summary>By the firer's hex: whether its line to a target point of a hex entered at the road rate crosses the hex's other terrain (A4.132).</summary>\n"
     "        public Dictionary<string, bool> CrossesTerrain { get; } = new(StringComparer.Ordinal);\n\n"
     "        public bool? CrossesTerrainInTargetHex(GameState state, BoardLocation from, BoardLocation target, HexsideDirection? enteredAcross) =>\n"
     "            CrossesTerrain.TryGetValue(from.Hex.ToString(), out var crosses) ? crosses : null;\n"),
])

RULES_TESTS = '''
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
}
'''

path = "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
assert text.rstrip().endswith("}")
body = text.rstrip()[:-1].rstrip() + "\n" + RULES_TESTS
old_doc = "/// Pass 40, read directly in Rules. Task 40.6:"
assert body.count(old_doc) == 1
body = body.replace(old_doc, "/// Pass 40, read directly in Rules. Task 40.2: a woods-road hex, the mover's choice across a road hexside, and the terrain a firer reads for a mover at\n/// the road rate (A4.132, B3.3, B13.31, pp. 48, 113, 128). Task 40.6:")
open(ROOT + path, "w", encoding="utf-8", newline="").write(body)
print("ok")
