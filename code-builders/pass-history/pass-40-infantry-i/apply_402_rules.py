"""Pass 40 task 40.2, the Rules edits of the mover's choice (A4.132): exact blocks, each asserted once before anything is written."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:70], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MovementModels.cs", [
    ("public sealed record InfantryEntry(int HalfMf, string Terrain, bool RoadRate, bool AllMf, bool MinimumMoveOnly, bool LevelChange);",
     "public sealed record InfantryEntry(int HalfMf, string Terrain, bool RoadRate, bool AllMf, bool MinimumMoveOnly, bool LevelChange)\n"
     "{\n"
     "    /// <summary>\n"
     "    /// A4.132 (p. 48; pass 40): whether the step crossed a road hexside paying the road's cost into a hex that has other terrain, so the mover is\n"
     "    /// on the road and not in that terrain while the DEFENDER First Fires. SMOKE in the hex does not change it; <see cref=\"RoadRate\"/> is the\n"
     "    /// Road Bonus's read, which SMOKE ends (B3.4).\n"
     "    /// </summary>\n"
     "    public bool OnRoad\n"
     "    {\n"
     "        get; init;\n"
     "    }\n"
     "}"),
    ("    bool Night,\n    int? Month);\n\n/// <summary>One hexside a Bypass runs along",
     "    bool Night,\n    int? Month,\n    bool TerrainRate = false);\n\n/// <summary>One hexside a Bypass runs along"),
])

apply("LimboDancer.Domains.Asl.Rules/ScenarioA1TerrainCosts.cs", [
    ("        return GroundStep(crossed, terrain, toRead.BaseLevel - fromRead.BaseLevel, facts.Month, smokeHalfMf, weather);\n    }",
     "        return GroundStep(crossed, terrain, toRead.BaseLevel - fromRead.BaseLevel, facts.Month, smokeHalfMf, weather, facts.TerrainRate);\n    }"),
    ("    public static (InfantryEntry? Entry, string? Reason) GroundStep(CrossedHexsideFacts crossed, string terrain, int rise, int? month, Func<int> smokeHalfMf, InfantryWeatherRead weather)\n    {",
     "    public static (InfantryEntry? Entry, string? Reason) GroundStep(CrossedHexsideFacts crossed, string terrain, int rise, int? month, Func<int> smokeHalfMf, InfantryWeatherRead weather,\n"
     "        bool terrainRate = false)\n    {"),
    ("        var road = crossed.Road && !ScenarioA1Definitions.IsRubbleTerrain(terrain);\n",
     "        // A4.132 (p. 48; pass 40, ruling R40.5): across a road hexside the mover pays the road's cost or the cost of the hex's other terrain, and\n"
     "        // is then in that terrain. The choice exists only where there is other terrain to pay for.\n"
     "        var roadHexside = crossed.Road && !ScenarioA1Definitions.IsRubbleTerrain(terrain);\n"
     "        if (terrainRate && (!roadHexside || terrain is \"open-ground\" or \"marsh\"))\n"
     "        {\n"
     "            return (null, \"play.move-terrain-rate: the cost of the hex's other terrain is chosen only across a road hexside into a hex that has other terrain (A4.132)\");\n"
     "        }\n"
     "\n"
     "        var road = roadHexside && !terrainRate;\n"),
    ("        return (new InfantryEntry(cost + extra, terrain, roadRate && smoke == 0, allMf, minimumOnly, rise != 0), null);",
     "        return (new InfantryEntry(cost + extra, terrain, roadRate && smoke == 0, allMf, minimumOnly, rise != 0)\n"
     "        {\n"
     "            OnRoad = road && roadRate && !allMf && terrain != \"open-ground\",\n"
     "        }, null);"),
])
print("ok")
