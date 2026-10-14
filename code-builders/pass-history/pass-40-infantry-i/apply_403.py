"""Pass 40 task 40.3, Dash (A4.63, C6.1): the edits outside ScenarioA1Dash.cs. Exact blocks with asserted counts, all checked before any file is written."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PENDING = []


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for edit in edits:
        old, new = edit[0], edit[1]
        count = edit[2] if len(edit) > 2 else 1
        assert text.count(old) == count, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    PENDING.append((path, text))


R = "LimboDancer.Domains.Asl.Rules/"
P = "LimboDancer.Domains.Asl.Play/"
U = "LimboDancer.Domains.Asl.Units/State/"
M = "LimboDancer.Domains.Asl.MapStudio/"

apply(R + "ScenarioA1MovementModels.cs", [
    ("    int OverstackExcess = 0,\n    bool OnRoad = false);", "    int OverstackExcess = 0,\n    bool OnRoad = false,\n    string? DashText = null);"),
])
apply(R + "ScenarioA1MovementCalculator.cs", [
    ("            + (facts.OnRoad ? \", at the road rate: on the road, not in the hex's other terrain (A4.132)\" : string.Empty)\n",
     "            + (facts.OnRoad ? \", at the road rate: on the road, not in the hex's other terrain (A4.132)\" : string.Empty)\n"
     "            + (facts.DashText is { } dashed && !forcedBack ? ScenarioA1Dash.DeclaredText(dashed) : string.Empty)\n"),
    ("    public static IReadOnlyList<string> EndingMembers(bool minimumMove, bool forcedBack, IReadOnlyList<string> movers, string? smokeUnit, int? smokeDr)\n"
     "    {\n        ArgumentNullException.ThrowIfNull(movers);\n        return minimumMove || forcedBack ? movers",
     "    public static IReadOnlyList<string> EndingMembers(bool minimumMove, bool forcedBack, IReadOnlyList<string> movers, string? smokeUnit, int? smokeDr, bool dashArrived = false)\n"
     "    {\n        ArgumentNullException.ThrowIfNull(movers);\n\n"
     "        // A4.63 (pass 40): a Dashing stack ends its MPh in the Location it Dashed to.\n"
     "        return minimumMove || forcedBack || dashArrived ? movers"),
])
apply(R + "ScenarioA1FireModels.cs", [
    ("    /// <summary>\n    /// The attack penalty of the firers' side for overstacking (A5.12, p. 52; pass 40):",
     "    /// <summary>\n"
     "    /// Whether the attack is at a Dashing stack in its road Location, from a Location to which neither side of the road is Open Ground (A4.63,\n"
     "    /// pp. 51 to 52; pass 40): Area Fire for the DEFENDER's non-ordnance Defensive First Fire. Null is false.\n"
     "    /// </summary>\n"
     "    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]\n"
     "    public bool? Dash\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n    /// The attack penalty of the firers' side for overstacking (A5.12, p. 52; pass 40):"),
])
apply(R + "ScenarioA1FireCalculator.cs", [
    ("            // A7.24, A7.25 (ruling R12.1): AFPh fire is halved, unless it is Opportunity Fire.\n",
     "            // A4.63 (pp. 51 to 52; pass 40): Defensive First Fire at a Dashing unit in the road Location is Area Fire, but for a Fire Lane.\n"
     "            if (attack.Dash == true && attack.FireLane != true)\n"
     "            {\n"
     "                multipliers.Add(new FireModifier(\"dash\", 0.5m, \"A4.63\"));\n"
     "            }\n\n"
     "            // A7.24, A7.25 (ruling R12.1): AFPh fire is halved, unless it is Opportunity Fire.\n"),
])
apply(R + "ScenarioA1OrdnanceModels.cs", [
    ("    public bool? Hazardous\n    {\n        get; init;\n    }\n",
     "    public bool? Hazardous\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n"
     "    /// Whether the moving target is a Dashing stack in its road Location, to a firer for whom the Dash qualifies (A4.63, C6.1; pass 40): Case J in\n"
     "    /// place of Cases J3 and J4, and no shot that must change its CA. Null is false.\n"
     "    /// </summary>\n"
     "    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]\n"
     "    public bool? Dashing\n    {\n        get; init;\n    }\n"),
])
apply(R + "ScenarioA1OrdnanceCalculator.cs", [
    ("            yield return new FireModifier(\"case-o\", -2, \"C6.6\");\n            yield break;\n        }\n",
     "            yield return new FireModifier(\"case-o\", -2, \"C6.6\");\n            yield break;\n        }\n\n"
     "        // A4.63, C6.1 (pp. 52, 173; pass 40): ordnance \"must use the Case J To Hit DRM (Cases J3 and J4 do not apply in the road Location)\".\n"
     "        if (movement.Dashing == true)\n"
     "        {\n"
     "            yield return new FireModifier(\"case-j\", 2, \"C6.1\");\n"
     "            yield break;\n"
     "        }\n"),
    ("    private static List<string> Outside(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)\n    {\n        var outside = new List<string>();\n        var phase = shot.Phase!;\n",
     "    private static List<string> Outside(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)\n    {\n        var outside = new List<string>();\n        var phase = shot.Phase!;\n\n"
     "        // A4.63 (p. 52; pass 40): \"a weapon with a specified CA may not fire on a Dashing unit in the road Location if it must change its CA to do so\".\n"
     "        if (shot.Movement?.Dashing == true && shot.HexspinesToTurn is > 0)\n"
     "        {\n"
     "            outside.Add(Prefix + \"dash-covered-arc\");\n"
     "        }\n\n"),
])
apply(R + "ScenarioA1OrdnanceEligibility.cs", [
    ("                Hazardous = recorded.Movement?.Hazardous,\n", "                Hazardous = recorded.Movement?.Hazardous,\n                Dashing = recorded.Movement?.Dashing,\n"),
])

apply(U + "LiveRecords.cs", [
    ("    public bool RoadRate\n    {\n        get; init;\n    }\n\n    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;",
     "    public bool RoadRate\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n"
     "    /// The Location a Dash declared with this step leads to (A4.63; pass 40): the step is into the road Location, and the stack's only further step\n"
     "    /// is into that Location, where its MPh ends. Null for other steps.\n"
     "    /// </summary>\n"
     "    public BoardLocation? Dash\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;"),
    ("    /// <summary>Whether the latest step entered the Location at the road rate, into a hex that has other terrain (A4.132; pass 40).</summary>\n    public bool RoadRate\n    {\n        get; init;\n    }\n",
     "    /// <summary>Whether the latest step entered the Location at the road rate, into a hex that has other terrain (A4.132; pass 40).</summary>\n    public bool RoadRate\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>The Location the stack Dashes to from the road Location it is in (A4.63; pass 40); null when it is not Dashing.</summary>\n"
     "    public BoardLocation? Dash\n    {\n        get; init;\n    }\n"),
])
apply(U + "GameEventWriter.cs", [
    ("                if (moving.RoadRate)\n                {\n                    writer.WriteBoolean(\"roadRate\", true);\n                }\n",
     "                if (moving.RoadRate)\n                {\n                    writer.WriteBoolean(\"roadRate\", true);\n                }\n\n"
     "                if (moving.Dash is { } dashTo)\n                {\n                    writer.WriteString(\"dash\", dashTo.ToString());\n                }\n"),
])
apply(U + "GameEventReader.cs", [
    ("                var charge = payload.TryGetProperty(\"charge\", out _) ? ReadLocation(payload, \"charge\", path, fields, diagnostics) : null;\n",
     "                var charge = payload.TryGetProperty(\"charge\", out _) ? ReadLocation(payload, \"charge\", path, fields, diagnostics) : null;\n"
     "                var dash = payload.TryGetProperty(\"dash\", out _) ? ReadLocation(payload, \"dash\", path, fields, diagnostics) : null;\n"),
    ("                        RoadRate = fields.OptionalBoolean(payload, \"roadRate\", path),\n",
     "                        RoadRate = fields.OptionalBoolean(payload, \"roadRate\", path),\n                        Dash = dash,\n"),
])
apply(U + "GameProjector.cs", [
    ("                    EndingMembers = Rules.ScenarioA1MovementCalculator.EndingMembers(moving.MinimumMove, moving.Attempted is not null, moving.Movers, moving.Smoke?.Unit, moving.Smoke?.Dr),\n",
     "                    EndingMembers = Rules.ScenarioA1MovementCalculator.EndingMembers(moving.MinimumMove, moving.Attempted is not null, moving.Movers, moving.Smoke?.Unit, moving.Smoke?.Dr,\n"
     "                        current?.Dash is { } dashedTo && dashedTo == moving.To && moving.Attempted is null),\n"
     "                    Dash = moving.Dash,\n"),
])

apply(P + "GameActions.cs", [
    ("\"terrainRate\": { \"type\": \"boolean\" },", "\"terrainRate\": { \"type\": \"boolean\" }, \"dash\": { \"type\": \"string\" },"),
])
apply(P + "LiveFire.cs", [
    ("            FirerOverstack = recorded.FirerOverstack,\n            TargetOverstack = recorded.TargetOverstack,\n",
     "            FirerOverstack = recorded.FirerOverstack,\n            TargetOverstack = recorded.TargetOverstack,\n\n"
     "            // Pass 40 (A4.63): whether the fire is at a Dash is the planner's read of the two sides of the road, recorded with the attack.\n"
     "            Dash = recorded.Dash,\n"),
])
apply(P + "GamePlanner.Fire.cs", [
    ("            TargetOverstack = targetSide is not null && LiveStacking.Of(state, target, targetSide).DefensePenalty is var crowded and > 0 ? crowded : null,\n        };\n",
     "            TargetOverstack = targetSide is not null && LiveStacking.Of(state, target, targetSide).DefensePenalty is var crowded and > 0 ? crowded : null,\n\n"
     "            // A4.63 (pass 40): non-ordnance Defensive First Fire at a Dashing stack in its road Location; a Fire Lane and Residual FP are excepted.\n"
     "            Dash = attack.OrdnanceHit is null && attack.FireLane != true\n"
     "                && attack.FireKind is ScenarioA1FireCalculator.FirstFire or ScenarioA1FireCalculator.SubsequentFirstFire or ScenarioA1FireCalculator.FinalProtectiveFire\n"
     "                ? DashAgainst(state, [.. perLocation.Keys.Select(BoardLocation.Parse)], target) : null,\n        };\n"),
    ("    /// <summary>A table of Locations by index, with the LOS between two of them as Rules asks for it (the pass 32 design, D4; pass 32.c).</summary>\n",
     "    /// <summary>\n"
     "    /// A4.63 (pass 40): whether fire from these Locations at a Location is at a Dashing stack in its road Location, for firers to whom neither side\n"
     "    /// of the road is Open Ground; null when it is not. Rules decides from each firing Location's two reads.\n"
     "    /// </summary>\n"
     "    private bool? DashAgainst(GameState state, IReadOnlyList<BoardLocation> firingLocations, BoardLocation target)\n"
     "    {\n"
     "        if (state.Phase != \"mph\" || state.Movement is not { Dash: { } dashTo, From: { } dashFrom } dashing || dashing.Location != target)\n"
     "        {\n"
     "            return null;\n"
     "        }\n\n"
     "        UnitInstance[] movers = [.. dashing.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>()];\n"
     "        if (movers.Length == 0 || !ScenarioA1Dash.BenefitsHold(movers.Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk)))))\n"
     "        {\n"
     "            return null;\n"
     "        }\n\n"
     "        var side = movers[0].Side;\n"
     "        return ScenarioA1Dash.QualifiesAgainst(firingLocations.Select(firer => (OpenGroundTo(state, firer, dashFrom, side), OpenGroundTo(state, firer, dashTo, side)))) ? true : null;\n"
     "    }\n\n"
     "    /// <summary>Whether a Location is Open Ground to a firing Location, as Interdiction reads it (A10.531; ruling R35.1): the reads handed to Rules.</summary>\n"
     "    private bool OpenGroundTo(GameState state, BoardLocation firer, BoardLocation at, string side)\n"
     "    {\n"
     "        var open = ReadLocation(state, at) is { } read && TerrainKey(read) == \"open-ground\";\n"
     "        var los = firer == at ? null : Los(state, firer, at);\n"
     "        var seen = firer == at || los is { IsBlocked: false };\n"
     "        return ScenarioA1Dash.OpenGroundTo(open, seen, los?.Hindrance ?? 0, open && seen && firer != at ? InterdictionCover(state, firer, at, side) : RoutCoverFacts.None);\n"
     "    }\n\n"
     "    /// <summary>A table of Locations by index, with the LOS between two of them as Rules asks for it (the pass 32 design, D4; pass 32.c).</summary>\n"),
])
apply(P + "GamePlanner.Ordnance.cs", [
    ("                            : null,\n                    },\n            };\n        }",
     "                            : null,\n\n"
     "                        // A4.63, C6.1 (pass 40): a Dashing stack in its road Location, to this firer.\n"
     "                        Dashing = DashAgainst(state, [from], target),\n"
     "                    },\n            };\n        }"),
])
apply(P + "GamePlanner.Movement.cs", [
    ("        var terrainRate = arguments.TryGetProperty(\"terrainRate\", out var rated) && rated.ValueKind == JsonValueKind.True;\n",
     "        var terrainRate = arguments.TryGetProperty(\"terrainRate\", out var rated) && rated.ValueKind == JsonValueKind.True;\n\n"
     "        // A4.63 (pass 40): the Location a Dash is declared to, with the stack's first step.\n"
     "        BoardLocation? dashTo = null;\n"
     "        if (Text(arguments, \"dash\", out var dashText) && !BoardLocation.TryParse(dashText, out dashTo))\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.invalid-arguments: a Dash names the Location Dashed to, such as bd01:E4:0\");\n"
     "        }\n"),
    ("        var action = ScenarioA1MovementCalculator.SpecialAction(Text(arguments, \"placeDc\", out var chargeId),",
     "        // A4.63 (pass 40): once a Dash is declared no SMOKE grenade or DC is placed with the MF of the road Location.\n"
     "        if (current?.Dash is { } pendingDash && (Text(arguments, \"placeDc\", out _) || Text(arguments, \"smoke\", out _)))\n"
     "        {\n"
     "            return Refused(scope, label, expected, ScenarioA1Dash.StepBar(new DashStepFacts(pendingDash.ToString(), null, to.ToString(), false, true, false, false, false, false, false, false,\n"
     "                false, false, true, false, false, false, false, false))!);\n"
     "        }\n\n"
     "        var action = ScenarioA1MovementCalculator.SpecialAction(Text(arguments, \"placeDc\", out var chargeId),"),
    ("        var terrain = entry.Terrain;\n\n        // A5.11: the stack the entry makes there;",
     "        // A4.63 (pass 40): a Dash declared with this step, or pending from the last; Rules decides from the step's facts and the map's.\n"
     "        if (ScenarioA1Dash.StepBar(new DashStepFacts(current?.Dash?.ToString(), dashTo?.ToString(), to.ToString(), current is null,\n"
     "            movers.Any(unit => unit.MfSpent != 0 || unit.HalfMfSpent), assault, minimumMove, bypassing is not null, occupy, pushed is not null, entering is not null, berserk > 0,\n"
     "            terrainRate, false, ReadLocation(state, to)?.Hex.Center.Terrain?.IsRoad == true, dashTo is { Level: 0, Side: null },\n"
     "            dashTo is not null && SideToward(state, to, dashTo) is not null, dashTo == from, dashTo is not null && SideToward(state, from, dashTo) is not null)) is { } dashBar)\n"
     "        {\n"
     "            return Refused(scope, label, expected, dashBar);\n"
     "        }\n\n"
     "        // \"Provided the normal MF expenditure for this two-hex move is <= the unit's available MF\": the step on, at its cost of entry.\n"
     "        var dashHalfMf = 0;\n"
     "        if (dashTo is not null)\n"
     "        {\n"
     "            var (onward, onwardReason) = InfantryStep(state, to, dashTo);\n"
     "            if (onward is null)\n"
     "            {\n"
     "                return Refused(scope, label, expected, onwardReason!);\n"
     "            }\n\n"
     "            dashHalfMf = onward.HalfMf;\n"
     "        }\n\n"
     "        var terrain = entry.Terrain;\n\n        // A5.11: the stack the entry makes there;"),
    ("state.Nvr, assault, halfMf, leaderIpcTo,", "state.Nvr, assault, halfMf + dashHalfMf, leaderIpcTo,"),
    ("overstack, entry.OnRoad));", "overstack, entry.OnRoad, dashTo?.ToString()));"),
    ("            RoadRate = entry.OnRoad && !forcedBack,\n", "            RoadRate = entry.OnRoad && !forcedBack,\n            Dash = forcedBack ? null : dashTo,\n"),
    ("        var verdict = ScenarioA1MovementCalculator.EndMove(movement is null ? null\n",
     "        // A4.63 (pass 40): a Dashing stack ends its MPh in the Location it Dashed to, unless its Dash has ceased.\n"
     "        if (ScenarioA1Dash.EndBar(movement?.Dash?.ToString(), movement is not null && ScenarioA1Dash.BenefitsHold(movement.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>()\n"
     "            .Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk))))) is { } dashEnd)\n"
     "        {\n"
     "            return Refused(scope, label, expected, dashEnd);\n"
     "        }\n\n"
     "        var verdict = ScenarioA1MovementCalculator.EndMove(movement is null ? null\n"),
])

apply(M + "Services/DisplayText.cs", [
    ("        [\"spraying-fire\"] = \"Spraying Fire\",\n", "        [\"spraying-fire\"] = \"Spraying Fire\",\n        [\"dash\"] = \"Area Fire at a Dash\",\n"),
])
apply(M + "Components/Play/InfantryMovementAction.razor", [
    ('<label><input id="move-assault" type="checkbox"',
     '<LocationField Id="move-dash" Label="Dash to" Value="@Draft.Dash" Title="A4.63: declared before the unit moves, with its first step into a road Location; the Location named is on the other side of the road, and the unit\'s only further step is into it, where its MPh ends. In the road Location the DEFENDER\'s small arms fire at it is Area Fire for firers to whom both sides of the road are not Open Ground." OnChange="@(value => Changed(Draft with { Dash = value }))" />\n'
     '<label><input id="move-assault" type="checkbox"'),
    ("    public sealed record MoveDraft(string To, bool Minimum, string Bypass, bool Assault, bool DoubleTime, bool Push, bool TerrainRate = false);",
     "    public sealed record MoveDraft(string To, bool Minimum, string Bypass, bool Assault, bool DoubleTime, bool Push, bool TerrainRate = false, string Dash = \"\");"),
    ("the Gun pushed, and whether the\n    /// other terrain's cost is paid across a road hexside (A4.132).",
     "the Gun pushed, whether the\n    /// other terrain's cost is paid across a road hexside (A4.132), and the Location a Dash is declared to (A4.63)."),
])
apply(M + "Components/Pages/Play.razor", [
    ("    private bool moveTerrainRate;\n", "    private bool moveTerrainRate;\n    private string moveDash = string.Empty;\n"),
    ("new(moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate);", "new(moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate, moveDash);"),
    ("        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate);",
     "        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate, moveDash) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate, draft.Dash);"),
    ("        if (moveTerrainRate)\n        {\n            arguments[\"terrainRate\"] = true;\n        }\n",
     "        if (moveTerrainRate)\n        {\n            arguments[\"terrainRate\"] = true;\n        }\n\n"
     "        // A4.63 (pass 40): the Location a Dash is declared to.\n"
     "        if (moveDash.Trim().Length > 0)\n        {\n            arguments[\"dash\"] = moveDash.Trim();\n        }\n"),
    ("\n        moveTerrainRate = false;\n", "\n        moveTerrainRate = false;\n        moveDash = string.Empty;\n", 2),
    ("\n                moveTerrainRate = false;\n", "\n                moveTerrainRate = false;\n                moveDash = string.Empty;\n", 2),
])

for path, text in PENDING:
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)
print("ok", len(PENDING))
