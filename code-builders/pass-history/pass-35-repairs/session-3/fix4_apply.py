"""Pass 35 review fix 4: a Recall exit by an unpaved road in Mud pays the Open Ground cost, as an entry does (E3.6, p. 229 to 230; D2.16).
The road rate is one Rules member for the entry and the exit. Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1VehicleTerrainCosts.cs": [
        ("""        if (road && mud && !paved)
        {
            road = false;
            terrain = "open-ground";
        }
""",
         """        if (MudTurnsRoadOff(road, mud, pavedRoad))
        {
            road = false;
            terrain = "open-ground";
        }
"""),
        ("""            cost = buttonedAfv || groundSnow || deepSnow ? 2 : 1;
""",
         """            cost = RoadHalfMp(buttonedAfv, groundSnow || deepSnow);
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1RecallCalculator.cs": [
        ("""    /// A2.6, D2.16: the half MP of an exit: its hex's own terrain cost, or the road rate when a road crosses the hexside: a full MP for a BU AFV, and
    /// in Ground or Deep Snow (E3.724, E3.7331), as an entry by road pays (pass 35, task 35.13 a; the pass 32 design's section 12, item 15).
    /// </summary>
    public static int ExitHalfMp(int terrainHalfMp, bool road, bool buttonedUpAfv, bool snow = false, int weatherHalfMp = 0) =>
        (road ? (buttonedUpAfv || snow ? 2 : 1) : terrainHalfMp) + weatherHalfMp;""",
         """    /// A2.6, D2.16: the half MP of an exit: its hex's own terrain cost, or the road rate when a road crosses the hexside, the same rate an entry by
    /// road pays (<see cref="ScenarioA1VehicleTerrainCosts.RoadHalfMp"/>; pass 35, task 35.13 a; the pass 32 design's section 12, item 15). The
    /// caller hands over <paramref name="road"/> as false, and the Open Ground cost, for an unpaved road in Mud (<see cref="ExitTerrain"/>).
    /// </summary>
    public static int ExitHalfMp(int terrainHalfMp, bool road, bool buttonedUpAfv, bool snow = false, int weatherHalfMp = 0) =>
        (road ? ScenarioA1VehicleTerrainCosts.RoadHalfMp(buttonedUpAfv, snow) : terrainHalfMp) + weatherHalfMp;"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Recall.cs": [
        ("""        if (ScenarioA1RecallCalculator.ExitTerrain(terrain, state.ScenarioMonth) is not { } paid || !VehicleTerrainHalfMp.TryGetValue((type, paid), out var halfMp))
        {
            return null;
        }

        // E1.52, E3.9 (pass 35, task 35.14; the pass 32 design's section 12, item 15): the night's and the weather's MP of the hexside crossed.
        var crossed = read.Hex.Hexsides.FirstOrDefault(item => item.Side == side)?.Terrain;
        var road = crossed?.IsRoad == true;
        return ScenarioA1RecallCalculator.ExitHalfMp(halfMp, road, ButtonedUpAfv(vehicle), state.Weather("ground-snow") || state.Weather("deep-snow"),
            VehicleWeatherHalfMp(state, type, paid, road, road && crossed?.Name == "Paved Road", road && state.SpecialRules.Contains("plowed-roads", StringComparer.Ordinal), 0));""",
         """        // E3.6 (the Rules boundary review): an unpaved road in Mud gives no road rate, and the exit pays the Open Ground cost, as an entry does.
        var crossed = read.Hex.Hexsides.FirstOrDefault(item => item.Side == side)?.Terrain;
        var paved = crossed?.Name == "Paved Road";
        var roadOff = ScenarioA1VehicleTerrainCosts.MudTurnsRoadOff(crossed?.IsRoad == true, state.Weather("mud"), paved);
        if (ScenarioA1RecallCalculator.ExitTerrain(terrain, state.ScenarioMonth, roadOff) is not { } paid || !VehicleTerrainHalfMp.TryGetValue((type, paid), out var halfMp))
        {
            return null;
        }

        // E1.52, E3.9 (pass 35, task 35.14; the pass 32 design's section 12, item 15): the night's and the weather's MP of the hexside crossed.
        var road = crossed?.IsRoad == true && !roadOff;
        return ScenarioA1RecallCalculator.ExitHalfMp(halfMp, road, ButtonedUpAfv(vehicle), state.Weather("ground-snow") || state.Weather("deep-snow"),
            VehicleWeatherHalfMp(state, type, paid, road, road && paved, road && state.SpecialRules.Contains("plowed-roads", StringComparer.Ordinal), 0));"""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1VehicleMovementRulesTests.cs": [
        ("""        Assert.Equal(10, ScenarioA1RecallCalculator.ExitHalfMp(8, false, false, weatherHalfMp: 2));
""",
         """        Assert.Equal(10, ScenarioA1RecallCalculator.ExitHalfMp(8, false, false, weatherHalfMp: 2));

        // E3.6 (p. 229 to 230; the Rules boundary review): on an unpaved road in Mud "the road bonus ... is NA and Open Ground movement COT applies",
        // for the exit as for an entry; a paved road is not affected. The road rate itself is one member for both.
        Assert.True(ScenarioA1VehicleTerrainCosts.MudTurnsRoadOff(road: true, mud: true, pavedRoad: false));
        Assert.False(ScenarioA1VehicleTerrainCosts.MudTurnsRoadOff(road: true, mud: true, pavedRoad: true));
        Assert.False(ScenarioA1VehicleTerrainCosts.MudTurnsRoadOff(road: true, mud: false, pavedRoad: false));
        Assert.False(ScenarioA1VehicleTerrainCosts.MudTurnsRoadOff(road: false, mud: true, pavedRoad: false));
        Assert.Equal("open-ground", ScenarioA1RecallCalculator.ExitTerrain("woods", null, roadInMud: true));
        Assert.Equal("woods", ScenarioA1RecallCalculator.ExitTerrain("woods", null, roadInMud: false));
        Assert.Equal(1, ScenarioA1VehicleTerrainCosts.RoadHalfMp(false, false));
        Assert.Equal(2, ScenarioA1VehicleTerrainCosts.RoadHalfMp(true, false));
        Assert.Equal(2, ScenarioA1VehicleTerrainCosts.RoadHalfMp(false, true));
"""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
