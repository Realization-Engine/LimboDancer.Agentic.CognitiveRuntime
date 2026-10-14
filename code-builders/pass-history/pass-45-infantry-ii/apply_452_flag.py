"""Pass 45, tasks 45.2 and 45.3: the new Cowering marks and Residual FP figures apply to attacks planned from the pass on.
An attack recorded before it carries no such fact and resolves as it was recorded (the pattern of pass 31's PinnedMgAreaFire)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireModels.cs": [
        ("""    /// <summary>
    /// Pass 31 (ruling R31.3; A7.81): pinned Infantry fires its MG as Area Fire and keeps no Multiple ROF. The planner records it with every attack
""",
         """    /// <summary>
    /// Pass 45 (A7.9, A8.2, A8.221, A8.23, A8.26; ruling R45.4): the attack marks the units that cower, shifts two columns for Inexperienced Personnel
    /// by their fact, and leaves the Residual FP its weapons' fates give, held to 12 after its reduction. The planner records it with every attack
    /// from the pass on; an attack recorded before it has none, and replays as it was resolved.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CoweringMarksAndWeaponResidual
    {
        get; init;
    }

    /// <summary>
    /// Pass 31 (ruling R31.3; A7.81): pinned Infantry fires its MG as Area Fire and keeps no Multiple ROF. The planner records it with every attack
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1NightAndWeather.cs": [
        ("""            PinnedMgAreaFire = true,
""",
         """            PinnedMgAreaFire = true,

            // Pass 45 (ruling R45.4): every attack planned from the pass on marks its cowering units and leaves its weapons' own Residual FP.
            CoweringMarksAndWeaponResidual = true,
"""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""            PinnedMgAreaFire = recorded.PinnedMgAreaFire,
""",
         """            PinnedMgAreaFire = recorded.PinnedMgAreaFire,

            // Pass 45 (ruling R45.4): so are the Cowering marks and the Residual FP by weapon.
            CoweringMarksAndWeaponResidual = recorded.CoweringMarksAndWeaponResidual,
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class == "conscript"
                || (item.Inexperienced ?? reference.Definitions[item.DefinitionId!].Class == "green"));
""",
         """            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class == "conscript"
                || (attack.CoweringMarksAndWeaponResidual == true ? item.Inexperienced ?? reference.Definitions[item.DefinitionId!].Class == "green"
                    : reference.Definitions[item.DefinitionId!].Class == "green"));
"""),
        ("""            cowering.Clear();
            if (!arithmetic.Cowered || attack.SprayShare == true)
""",
         """            cowering.Clear();
            if (!arithmetic.Cowered || attack.SprayShare == true || attack.CoweringMarksAndWeaponResidual != true)
"""),
        ("""            if (weapons is null || arithmetic.ResidualFp is null && !weapons.Any(item => item.Malfunctioned || item.RateOfFireRetained))
""",
         """            if (weapons is null || attack.CoweringMarksAndWeaponResidual != true
                || (arithmetic.ResidualFp is null && !weapons.Any(item => item.Malfunctioned || item.RateOfFireRetained)))
"""),
        ("""            var index = Array.FindLastIndex(columns, fp => fp <= highest / 2m);
""",
         """            if (attack.CoweringMarksAndWeaponResidual != true)
            {
                // An attack recorded before pass 45: the counter at most half the column, lowered by Hindrance, leadership, and hexside TEM.
                var before = Array.FindLastIndex(ResidualCounters, fp => fp <= highest / 2m) - (hindrance + Math.Max(leadership, 0) + (attack.HexsideTem?.Tem ?? 0)
                    + (attack.OrdnanceHit is not null && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground" ? 1 : 0));
                return before < 0 ? null : ResidualCounters[before];
            }

            var index = Array.FindLastIndex(columns, fp => fp <= highest / 2m);
"""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass45RulesTests.cs": [
        ("""        new(phase, side, true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], director, 1, true, new FireLos(false, 0, true, false), null,
            "open-ground", targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null));
""",
         """        new(phase, side, true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], director, 1, true, new FireLos(false, 0, true, false), null,
            "open-ground", targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null))
        {
            // As the planner records with every attack from pass 45 on (ruling R45.4).
            CoweringMarksAndWeaponResidual = true,
        };
"""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1FirePackageTests.cs": [
        ("""        // Pass 45 (A7.9): a fire group that cowers picks the unit it marks by Random Selection, a roll of its own.
        var cowered = ScenarioA1FireCalculator.Resolve(U18(Rolls([3, 3], checks: checks) with
        {
            CowerSelection = new Dictionary<string, int> { ["ru-1"] = 1, ["ru-2"] = 2 },
        }, useDirector: false), Reference).Arithmetic!;
""",
         """        var cowered = ScenarioA1FireCalculator.Resolve(U18(Rolls([3, 3], checks: checks), useDirector: false), Reference).Arithmetic!;
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
