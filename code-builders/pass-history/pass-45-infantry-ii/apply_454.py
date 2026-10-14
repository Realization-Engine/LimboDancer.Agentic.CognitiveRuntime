"""Pass 45, task 45.4: Squad Spraying Fire off the catalog trait, the capability test for a group, two levels of one hex, and Encirclement (A7.34, A9.5, A9.52, A7.7)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireReference.cs": [
        ("""    /// <summary>The Assault Fire bonus: an underlined FP (A1.21, A7.36).</summary>
    public bool? AssaultFire
    {
        get; init;
    }
""",
         """    /// <summary>The Assault Fire bonus: an underlined FP (A1.21, A7.36).</summary>
    public bool? AssaultFire
    {
        get; init;
    }

    /// <summary>The Spraying Fire option: an underscored Range (A7.34; pass 45).</summary>
    public bool? SprayingFire
    {
        get; init;
    }
"""),
        ("""            AssaultFire = Trait("front", "asl:assault-fire"),
""",
         """            AssaultFire = Trait("front", "asl:assault-fire"),
            SprayingFire = Trait("front", "asl:spraying-fire"),
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""        if (attack.SprayingFire == true && (phase is not ("PFPh" or "AFPh" or "DFPh") || tpbf || firers.Any(item => item.Weapons is not { Count: > 0 } weapons
            || weapons.Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is not { IsMg: true }))))
        {
            outside.Add("asl.a1.fire.spraying-fire-outside");
        }
""",
         """        // Pass 45 (A7.34, A9.52, pp. 56 and 64; ruling R45.5): "Spraying Fire cannot be used in a FG attack unless all members of the FG are capable
        // of Spraying Fire". A MG is; a squad's own FP is when its Range is underscored, to "a maximum range of three hexes"; no other unit's FP is.
        // An attack recorded before the pass keeps ruling R12.6's test: every firer fires a MG, and its holder's FP goes with it.
        bool Sprays(FireFirer item, FireDefinition? definition) => item.Weapons is { } used
            && used.All(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is { IsMg: true })
            && (item.UsesInherentFp == false ? used.Count > 0
                : definition is { Kind: "asl:squad", SprayingFire: true } && RangeOf(attack, item) <= 3);
        var capable = attack.CoweringMarksAndWeaponResidual == true
            ? firers.Zip(firerDefinitions).All(pair => Sprays(pair.First with { Weapons = pair.First.Weapons ?? [] }, pair.Second))
            : firers.All(item => item.Weapons is { Count: > 0 } weapons
                && weapons.All(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is { IsMg: true }));
        if (attack.SprayingFire == true && (phase is not ("PFPh" or "AFPh" or "DFPh") || tpbf || !capable))
        {
            outside.Add("asl.a1.fire.spraying-fire-outside");
        }
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs": [
        ("""    /// <summary>A9.5 (ruling R12.6): Spraying Fire attacks two Locations that share a hexside, at the same level.</summary>
    public static string? SprayTargetBar(bool parsed, bool sameAsTarget, bool sameLevel, bool sharesHexside) =>
        !parsed || sameAsTarget || !sameLevel || !sharesHexside ? "play.fire-spray: Spraying Fire attacks two Locations that share a hexside (A9.5)" : null;
""",
         """    /// <summary>
    /// A9.5 (p. 64; ruling R12.6): Spraying Fire attacks two Locations that share a hexside, at the same level. Pass 45: "The target Locations can be
    /// 'adjacent' vertically in the case of building upper levels, but each must be within one level of the other and in the same hex."
    /// </summary>
    public static string? SprayTargetBar(bool parsed, bool sameAsTarget, bool sameLevel, bool sharesHexside, bool nextLevelOfTheHex = false) =>
        !parsed || sameAsTarget || !((sameLevel && sharesHexside) || nextLevelOfTheHex)
            ? "play.fire-spray: Spraying Fire attacks two Locations that share a hexside, or two levels of one building hex that are one level apart (A9.5)" : null;
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireMapRules.cs": [
        ("""        if (phase is not ("pfph" or "dfph" or "afph") || facts.SprayingFire == true || !targetKnown)
        {
            return null;
        }
""",
         """        // Pass 45 (A7.7): Spraying Fire is fire by inherent FP or a SW as any other, and counts when its FP could inflict a NMC.
        if (phase is not ("pfph" or "dfph" or "afph") || !targetKnown)
        {
            return null;
        }
"""),
        ("""            if (attack.Read() is not { } record || record.Blocked || record.Recorded.SprayingFire == true)
""",
         """            if (attack.Read() is not { } record || record.Blocked)
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""            if (ScenarioA1FireEligibility.SprayTargetBar(parsed, parsed && second! == target, parsed && second!.Level == target.Level,
                parsed && SideToward(state, target, second!) is not null) is { } sprayTarget)
""",
         """            if (ScenarioA1FireEligibility.SprayTargetBar(parsed, parsed && second! == target, parsed && second!.Level == target.Level,
                parsed && SideToward(state, target, second!) is not null,
                parsed && second!.Board == target.Board && second.Hex == target.Hex && Math.Abs(second.Level - target.Level) == 1) is { } sprayTarget)
"""),
    ],
    "LimboDancer.Domains.Asl.Play/RefusalReasons.cs": [],
}

texts = {}
for name, edits in EDITS.items():
    if not edits:
        continue
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
