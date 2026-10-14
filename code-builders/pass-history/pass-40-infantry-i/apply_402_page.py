"""Pass 40 task 40.2, the page: the mover's choice of the other terrain's cost (A4.132). Exact blocks with asserted counts."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new, count in edits:
        assert text.count(old) == count, (path, old[:70], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("Play/InfantryMovementAction.razor", [
    ('<label><input id="move-assault" type="checkbox"',
     '<label title="A4.132: across a road hexside into a hex that has other terrain (a woods-road hex, a building beside its road) the unit pays the road\'s cost and is on the road, with no TEM of that terrain and FFMO where the firer sees the road; check this to pay the other terrain\'s cost and be in it."><input id="move-terrain-rate" type="checkbox" checked="@Draft.TerrainRate" @onchange="@(args => Changed(Draft with { TerrainRate = args.Value is true }))" /> pay the other terrain\'s cost, not the road\'s</label>\n'
     '<label><input id="move-assault" type="checkbox"', 1),
    ("    /// <summary>The step drafted: the Location typed, Minimum Move, the Bypass hexsides typed, Assault Movement, Double Time, and the Gun pushed.</summary>\n"
     "    public sealed record MoveDraft(string To, bool Minimum, string Bypass, bool Assault, bool DoubleTime, bool Push);",
     "    /// <summary>\n"
     "    /// The step drafted: the Location typed, Minimum Move, the Bypass hexsides typed, Assault Movement, Double Time, the Gun pushed, and whether the\n"
     "    /// other terrain's cost is paid across a road hexside (A4.132).\n"
     "    /// </summary>\n"
     "    public sealed record MoveDraft(string To, bool Minimum, string Bypass, bool Assault, bool DoubleTime, bool Push, bool TerrainRate = false);", 1),
])

apply("Pages/Play.razor", [
    ("    private bool moveDoubleTime;\n", "    private bool moveDoubleTime;\n    private bool moveTerrainRate;\n", 1),
    ("    private InfantryMovementAction.MoveDraft MoveDraft => new(moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush);",
     "    private InfantryMovementAction.MoveDraft MoveDraft => new(moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate);", 1),
    ("        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush) = (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push);",
     "        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate);", 1),
    ("        // A4.134, A4.3 (rulings R10.7, R10.9): a Minimum Move, or the hexsides of a Bypass.\n        if (moveMinimum)",
     "        // A4.132 (pass 40): the other terrain's cost across a road hexside, in place of the road's.\n"
     "        if (moveTerrainRate)\n        {\n            arguments[\"terrainRate\"] = true;\n        }\n\n"
     "        // A4.134, A4.3 (rulings R10.7, R10.9): a Minimum Move, or the hexsides of a Bypass.\n        if (moveMinimum)", 1),
    ("        moveMinimum = false;\n        moveBypass = string.Empty;\n", "        moveMinimum = false;\n        moveTerrainRate = false;\n        moveBypass = string.Empty;\n", 2),
    ("                moveMinimum = false;\n                moveBypass = string.Empty;\n", "                moveMinimum = false;\n                moveTerrainRate = false;\n                moveBypass = string.Empty;\n", 2),
])
print("ok")
