"""Pass 40 task 40.2, the Play, Units, and page edits (A4.132, B13.31): exact blocks, each asserted once before anything is written."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:70], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


# The step read takes the mover's choice.
apply("LimboDancer.Domains.Asl.Play/GamePlanner.Terrain.cs", [
    ("    private (InfantryEntry? Entry, string? Reason) InfantryStep(GameState state, BoardLocation from, BoardLocation to)\n    {",
     "    private (InfantryEntry? Entry, string? Reason) InfantryStep(GameState state, BoardLocation from, BoardLocation to, bool terrainRate = false)\n    {"),
    ("            StepLocation(fromRead), StepLocation(toRead), adjacent, crossed is null ? null : CrossedFacts(crossed), state.Night, state.ScenarioMonth);",
     "            StepLocation(fromRead), StepLocation(toRead), adjacent, crossed is null ? null : CrossedFacts(crossed), state.Night, state.ScenarioMonth, terrainRate);"),
    ("        BoardLocation from, BoardLocation to, MovementState? current, List<HexsideDirection>? bypass)\n    {\n        // Pass 32.b: the state and the map are read here, and Rules decides which step the stack takes and reads it.",
     "        BoardLocation from, BoardLocation to, MovementState? current, List<HexsideDirection>? bypass, bool terrainRate = false)\n    {\n        // Pass 32.b: the state and the map are read here, and Rules decides which step the stack takes and reads it."),
    ("            var (exit, exitReason) = InfantryStep(state, from, to);\n            return (exit, exitReason, null, false);",
     "            var (exit, exitReason) = InfantryStep(state, from, to, terrainRate);\n            return (exit, exitReason, null, false);"),
])

apply("LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs", [
    ("        var doubleTime = arguments.TryGetProperty(\"doubleTime\", out var timed) && timed.ValueKind == JsonValueKind.True;\n",
     "        var doubleTime = arguments.TryGetProperty(\"doubleTime\", out var timed) && timed.ValueKind == JsonValueKind.True;\n"
     "\n"
     "        // A4.132 (pass 40): across a road hexside the mover may pay the cost of the hex's other terrain and be in it.\n"
     "        var terrainRate = arguments.TryGetProperty(\"terrainRate\", out var rated) && rated.ValueKind == JsonValueKind.True;\n"),
    ("            : MoveEntry(state, movers, from, to, current, bypass);",
     "            : MoveEntry(state, movers, from, to, current, bypass, terrainRate);"),
    ("        if (entry is null)\n        {\n            return Refused(scope, label, expected, stepReason!);\n        }\n\n        var terrain = entry.Terrain;",
     "        if (entry is null)\n        {\n            return Refused(scope, label, expected, stepReason!);\n        }\n\n"
     "        // A4.132 (pass 40): the choice is a step's across a road hexside; an entry from off board, a Bypass, and an obstacle occupied have none.\n"
     "        if (terrainRate && (entering is not null || bypassing is not null || occupy))\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.move-terrain-rate: the cost of the hex's other terrain is chosen only for a step across a road hexside, not for an entry from off board, a Bypass, or an obstacle occupied (A4.132)\");\n"
     "        }\n\n"
     "        var terrain = entry.Terrain;"),
    ("            [.. hiddenEnemies.Select(unit => new MoveRevealUnitFacts(unit.Id, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Hidden)))], overstack));",
     "            [.. hiddenEnemies.Select(unit => new MoveRevealUnitFacts(unit.Id, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Hidden)))], overstack, entry.OnRoad));"),
    ("            Road = verdict.RoadBonus,\n            MinimumMove = minimumMove,\n            Attempted = forcedBack ? to : null,\n            Bypass = bypassing,",
     "            Road = verdict.RoadBonus,\n            RoadRate = entry.OnRoad && !forcedBack,\n            MinimumMove = minimumMove,\n            Attempted = forcedBack ? to : null,\n            Bypass = bypassing,"),
])

apply("LimboDancer.Domains.Asl.Play/GameActions.cs", [
    ("            \"assault\": { \"type\": \"boolean\" }, \"doubleTime\": { \"type\": \"boolean\" }, \"pushGun\": { \"type\": \"string\" },\n            \"smoke\": { \"type\": \"string\" }, \"smokeBy\": { \"type\": \"string\" }, \"minimumMove\": { \"type\": \"boolean\" },",
     "            \"assault\": { \"type\": \"boolean\" }, \"doubleTime\": { \"type\": \"boolean\" }, \"pushGun\": { \"type\": \"string\" },\n            \"smoke\": { \"type\": \"string\" }, \"smokeBy\": { \"type\": \"string\" }, \"minimumMove\": { \"type\": \"boolean\" }, \"terrainRate\": { \"type\": \"boolean\" },"),
])

# The step's record and the movement state.
apply("LimboDancer.Domains.Asl.Units/State/LiveRecords.cs", [
    ("    public bool Road\n    {\n        get; init;\n    }\n\n    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;",
     "    public bool Road\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n"
     "    /// Whether the movers entered <see cref=\"To\"/> across a road hexside paying the road's cost, into a hex that has other terrain (A4.132; pass 40):\n"
     "    /// they are on the road, not in that terrain, while the DEFENDER First Fires at them there.\n"
     "    /// </summary>\n"
     "    public bool RoadRate\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;"),
    ("    /// <summary>Whether the latest step was a Minimum Move (A4.134; ruling R10.9).</summary>\n    public bool MinimumMove\n    {\n        get; init;\n    }\n",
     "    /// <summary>Whether the latest step was a Minimum Move (A4.134; ruling R10.9).</summary>\n    public bool MinimumMove\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>Whether the latest step entered the Location at the road rate, into a hex that has other terrain (A4.132; pass 40).</summary>\n"
     "    public bool RoadRate\n    {\n        get; init;\n    }\n"),
])

apply("LimboDancer.Domains.Asl.Units/State/GameEventWriter.cs", [
    ("                {\n                    writer.WriteBoolean(\"road\", true);\n                }\n\n                if (moving.MinimumMove)\n                {\n                    writer.WriteBoolean(\"minimumMove\", true);\n                }\n\n                if (moving.Attempted is { } attemptedAt)",
     "                {\n                    writer.WriteBoolean(\"road\", true);\n                }\n\n"
     "                if (moving.RoadRate)\n                {\n                    writer.WriteBoolean(\"roadRate\", true);\n                }\n\n"
     "                if (moving.MinimumMove)\n                {\n                    writer.WriteBoolean(\"minimumMove\", true);\n                }\n\n                if (moving.Attempted is { } attemptedAt)"),
])

apply("LimboDancer.Domains.Asl.Units/State/GameEventReader.cs", [
    ("                        Road = fields.OptionalBoolean(payload, \"road\", path),\n                        MinimumMove = fields.OptionalBoolean(payload, \"minimumMove\", path),\n                        Attempted = attempted,\n                        Bypass = bypass,",
     "                        Road = fields.OptionalBoolean(payload, \"road\", path),\n                        RoadRate = fields.OptionalBoolean(payload, \"roadRate\", path),\n                        MinimumMove = fields.OptionalBoolean(payload, \"minimumMove\", path),\n                        Attempted = attempted,\n                        Bypass = bypass,"),
])

apply("LimboDancer.Domains.Asl.Units/State/GameProjector.cs", [
    ("                    MinimumMove = moving.MinimumMove,\n                    Bypass = moving.Bypass,\n                    From = movers[0]!.Position is MapPosition left && left.Location != moving.To ? left.Location : null,",
     "                    MinimumMove = moving.MinimumMove,\n                    RoadRate = moving.RoadRate,\n                    Bypass = moving.Bypass,\n                    From = movers[0]!.Position is MapPosition left && left.Location != moving.To ? left.Location : null,"),
])

# The move's summary.
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MovementModels.cs", [
    ("    IReadOnlyList<MoveRevealUnitFacts> HiddenEnemies,\n    int OverstackExcess = 0);",
     "    IReadOnlyList<MoveRevealUnitFacts> HiddenEnemies,\n    int OverstackExcess = 0,\n    bool OnRoad = false);"),
])
apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MovementCalculator.cs", [
    (") for {facts.HalfMf / 2m} MF\"\n            + (facts.OverstackExcess > 0",
     ") for {facts.HalfMf / 2m} MF\"\n"
     "            + (facts.OnRoad ? \", at the road rate: on the road, not in the hex's other terrain (A4.132)\" : string.Empty)\n"
     "            + (facts.OverstackExcess > 0"),
])
print("ok")
