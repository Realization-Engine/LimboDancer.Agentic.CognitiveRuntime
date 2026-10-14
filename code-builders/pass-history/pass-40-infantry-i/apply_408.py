"""Pass 40 task 40.8, Hazardous Movement in every fire phase (A4.62): exact blocks with asserted counts, all checked before any file is written."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PENDING = []


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    PENDING.append((path, text))


R = "LimboDancer.Domains.Asl.Rules/"
P = "LimboDancer.Domains.Asl.Play/"
U = "LimboDancer.Domains.Asl.Units/State/"

apply(R + "ScenarioA1FireMapRules.cs", [
    ("    /// <summary>A4.62 (ruling R10.8; referee, pass 10): only the crew pushing its Gun into the target Location this MPh is under Hazardous Movement, when it is every target.</summary>\n"
     "    public static bool? HazardousMovement(string? phase, bool pushedGunAtTarget, string? gunHolder, FireAttack attack)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(attack);\n"
     "        return phase == \"mph\" && pushedGunAtTarget && gunHolder is not null && attack.Targets!.Count > 0 && attack.Targets.All(item => item.UnitId == gunHolder) ? true : null;\n"
     "    }\n",
     "    /// <summary>\n"
     "    /// A4.62 (p. 51; rulings R10.8 and R40.10; referee, pass 10): an attack is against Hazardous Movement \"regardless of fire phase\" when every\n"
     "    /// target is a unit that pushed a Gun this Player Turn and none of them is broken. The pinned among them are left out by the package, which\n"
     "    /// reads each target's own state. Null when it is not.\n"
     "    /// </summary>\n"
     "    public static bool? HazardousMovement(IReadOnlyCollection<string> hazardousMovers, FireAttack attack)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(hazardousMovers);\n"
     "        ArgumentNullException.ThrowIfNull(attack);\n"
     "        return attack.Targets is { Count: > 0 } targets && targets.All(item => item.UnitId is { } id && hazardousMovers.Contains(id) && item.Broken != true) ? true : null;\n"
     "    }\n"),
])
apply(R + "ScenarioA1FireCalculator.cs", [
    ("attack.HexsideTem is not null))\n            || (attack.HazardousMovement == true && phase != \"MPh\"))\n",
     "attack.HexsideTem is not null)))\n"),
])
apply(R + "ScenarioA1MovementCalculator.cs", [
    ("    /// <summary>A4.41 (referee, pass 9): a light mortar carried into a new Location does not fire in the AFPh;",
     "    /// <summary>\n"
     "    /// A4.62 (p. 51; ruling R40.10): the units under Hazardous Movement this Player Turn, after a step: those already there and the movers of a\n"
     "    /// step that pushes a Gun. The list is emptied as a new Player Turn begins.\n"
     "    /// </summary>\n"
     "    public static IReadOnlyList<string> HazardousMovers(IReadOnlyList<string> already, bool pushesGun, IReadOnlyList<string> movers)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(already);\n"
     "        ArgumentNullException.ThrowIfNull(movers);\n"
     "        return pushesGun ? [.. already.Union(movers, StringComparer.Ordinal)] : already;\n"
     "    }\n\n"
     "    /// <summary>A4.41 (referee, pass 9): a light mortar carried into a new Location does not fire in the AFPh;"),
])
apply(R + "ScenarioA1OrdnanceModels.cs", [
    ("    /// <summary>\n    /// Whether a Good Order enemy ground unit within 16 hexes sees the firing Gun's Location,",
     "    /// <summary>\n"
     "    /// Whether every target of a shot outside the MPh is under Hazardous Movement (C6.6, A4.62; pass 40, ruling R40.10): Case O. In the MPh the\n"
     "    /// shot's movement facts carry it. Null is false.\n"
     "    /// </summary>\n"
     "    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]\n"
     "    public bool? HazardousTarget\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n    /// Whether a Good Order enemy ground unit within 16 hexes sees the firing Gun's Location,"),
])
apply(R + "ScenarioA1OrdnanceCalculator.cs", [
    ("        else if (shot.FireKind is not null && shot.Movement is { } movement)\n        {\n            drm.AddRange(MoverDrm(movement));\n        }\n",
     "        else if (shot.FireKind is not null && shot.Movement is { } movement)\n        {\n            drm.AddRange(MoverDrm(movement));\n        }\n"
     "        else if (shot.HazardousTarget == true)\n        {\n"
     "            // C6.6, A4.62 (pp. 175, 51; pass 40): Case O in any fire phase.\n"
     "            drm.Add(new FireModifier(\"case-o\", -2, \"C6.6\"));\n        }\n"),
])
apply(R + "ScenarioA1OrdnanceEligibility.cs", [
    ("            CrewSeen = recorded.CrewSeen,\n", "            CrewSeen = recorded.CrewSeen,\n            HazardousTarget = recorded.HazardousTarget,\n"),
])

apply(U + "GameState.cs", [
    ("    public IReadOnlyList<string> MovedWeapons { get; init; } = [];\n",
     "    public IReadOnlyList<string> MovedWeapons { get; init; } = [];\n\n"
     "    /// <summary>The units that pushed a Gun in this Player Turn's MPh, under Hazardous Movement in every fire phase of it (A4.62; pass 40, ruling R40.10).</summary>\n"
     "    public IReadOnlyList<string> HazardousMovers { get; init; } = [];\n"),
])
apply(U + "GameProjector.cs", [
    ("                    MovedWeapons = newPlayerTurn ? [] : state.MovedWeapons,\n",
     "                    MovedWeapons = newPlayerTurn ? [] : state.MovedWeapons,\n                    HazardousMovers = newPlayerTurn ? [] : state.HazardousMovers,\n"),
    ("                SmokeAttempts = moving.Smoke is { } attempt ? [.. next.SmokeAttempts, attempt.Unit] : next.SmokeAttempts,\n",
     "                SmokeAttempts = moving.Smoke is { } attempt ? [.. next.SmokeAttempts, attempt.Unit] : next.SmokeAttempts,\n"
     "                HazardousMovers = Rules.ScenarioA1MovementCalculator.HazardousMovers(next.HazardousMovers, moving.PushedGun is not null, moving.Movers),\n"),
])

apply(P + "GamePlanner.Fire.cs", [
    ("            // B10.31 (ruling R10.4): Height Advantage over every firer; A4.62 (ruling R10.8): a crew pushing its Gun is under Hazardous Movement.\n"
     "            var pushedGun = state.Phase == \"mph\" && state.Movement is { PushedGun: { } pushed } pushing && pushing.Location == target ? pushed : null;\n",
     "            // B10.31 (ruling R10.4): Height Advantage over every firer; A4.62 (rulings R10.8, R40.10): a crew that pushed its Gun this Player Turn\n"
     "            // is under Hazardous Movement in every fire phase of it.\n"),
    ("                HazardousMovement = ScenarioA1FireMapRules.HazardousMovement(state.Phase, pushedGun is not null,\n"
     "                    pushedGun is not null && state.Find(pushedGun) is EquipmentInstance { Holding: { } manning } ? manning.Holder : null, attack),\n",
     "                HazardousMovement = ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, attack),\n"),
])
apply(P + "GamePlanner.Ordnance.cs", [
    ("                        // C6.6, A4.62 (ruling R10.8): a crew pushing its Gun into the target Location is under Hazardous Movement.\n"
     "                        Hazardous = state.Movement is { PushedGun: { } pushed } pushing && pushing.Location == target\n"
     "                            ? ScenarioA1FireMapRules.HazardousMovement(state.Phase, true, state.Find(pushed) is EquipmentInstance { Holding: { } manning } ? manning.Holder : null, shot.Hit)\n"
     "                            : null,\n",
     "                        // C6.6, A4.62 (rulings R10.8, R40.10): a crew that pushed its Gun this Player Turn is under Hazardous Movement.\n"
     "                        Hazardous = ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, shot.Hit),\n"),
    ("                    },\n            };\n        }\n",
     "                    },\n            };\n        }\n"
     "        else if (shot.VehicleTarget is null && shot.Hit is { } aimed && ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, aimed) == true)\n"
     "        {\n"
     "            // C6.6, A4.62 (pass 40, ruling R40.10): Case O outside the MPh too, \"regardless of fire phase\".\n"
     "            shot = shot with\n            {\n                HazardousTarget = true,\n            };\n"
     "        }\n"),
])

for path, text in PENDING:
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)
print("ok", len(PENDING))
