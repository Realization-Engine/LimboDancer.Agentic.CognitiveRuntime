"""Pass 45, task 45.8: the group as one chain (A7.5), no berserk unit in a multi-Location group (A7.54), a crew's FP after its Gun fired (A7.353)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireMapRules.cs": [
        ("""            // A7.5: every Location of the group ADJACENT to another of them.
            var locations = perLocation.Keys.ToArray();
""",
         """            // A7.5 (p. 57; pass 45): the group is one "string of ADJACENT Locations", so every Location is reached from the first through
            // Locations of the group; two pairs apart are two groups.
            var locations = perLocation.Keys.ToArray();
            var chain = new HashSet<string>(StringComparer.Ordinal) { locations[0] };
            for (var grew = true; grew;)
            {
                grew = false;
                foreach (var next in locations.Where(one => !chain.Contains(one) && chain.Any(linked => adjacent(linked, one))).ToArray())
                {
                    grew |= chain.Add(next);
                }
            }

"""),
        ("""                FirerLocationsAdjacent = locations.All(one => locations.Any(two => two != one && adjacent(one, two))),
""",
         """                FirerLocationsAdjacent = chain.Count == locations.Length,
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireRuleModels.cs": [
        ("""    string? Partner,
    bool Mol,
    bool? Inexperienced);
""",
         """    string? Partner,
    bool Mol,
    bool? Inexperienced,
    bool Berserk = false);
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs": [
        ("""            Mol = facts.Mol ? true : null,
            Inexperienced = facts.Inexperienced,
        };
""",
         """            Mol = facts.Mol ? true : null,
            Inexperienced = facts.Inexperienced,
            Berserk = facts.Berserk ? true : null,
        };
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireModels.cs": [
        ("""    /// <summary>Whether its Green user is Inexperienced (A19.32; referee, pass 15): its malfunction number is one lower; null is false.</summary>
""",
         """    /// <summary>Whether the firer is berserk (A7.54, A7.9; pass 45): it joins no multi-Location group and never cowers; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }

    /// <summary>Whether its Green user is Inexperienced (A19.32; referee, pass 15): its malfunction number is one lower; null is false.</summary>
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""            // A7.352 (ruling R8.4): a crew that fired its Gun this Player Turn has no inherent FP, but in FPF, where it "must add" it (A8.311; pass 40,
            // ruling R40.18).
            || firers.Any(item => item.UsesInherentFp != false
                && !ScenarioA1MandatoryFire.CrewAddsInherentFp(item.GunFired == true, attack.FireKind == FinalProtectiveFire)))
        {
            outside.Add("asl.a1.fire.firer-outside");
        }
""",
         """            // A7.352, A7.353 (p. 56; ruling R8.4): a crew that fired its Gun this Player Turn has no inherent FP, but "Subsequent First Fire (8.3),
            // FPF (8.31), and Final Fire (8.4) vs adjacent units retain halved inherent FP": its own fire of those kinds, which is Area Fire.
            || firers.Any(item => item.UsesInherentFp != false && !ScenarioA1MandatoryFire.CrewAddsInherentFp(item.GunFired == true,
                attack.FireKind == FinalProtectiveFire || OwnKind(attack, item) == SubsequentFirstFire || IsFinalFireAgain(attack, item))))
        {
            outside.Add("asl.a1.fire.firer-outside");
        }

        // A7.54 (p. 57; pass 45): "A berserk unit may never be part of a multi-Location FG."
        if (multi && firers.Any(item => item.Berserk == true))
        {
            outside.Add("asl.a1.fire.berserk-group-outside");
        }
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1MandatoryFire.cs": [
        ("""    /// Whether a crew that fired its Gun this Player Turn still adds its inherent FP (A8.311, p. 61): "Infantry manning ordnance cannot fire it during
    /// FPF ... but must add their inherent FP to the FPF attack". Outside FPF it has none (A7.352; ruling R8.4).
    /// </summary>
    public static bool CrewAddsInherentFp(bool gunFired, bool finalProtectiveFire) => !gunFired || finalProtectiveFire;
""",
         """    /// Whether a crew that fired its Gun this Player Turn still adds its inherent FP (A8.311, p. 61): "Infantry manning ordnance cannot fire it during
    /// FPF ... but must add their inherent FP to the FPF attack"; and A7.353 (p. 56) keeps it in Subsequent First Fire and in Final Fire against an
    /// adjacent unit (<paramref name="areaFireAgain"/>). In any other fire it has none (A7.352; ruling R8.4).
    /// </summary>
    public static bool CrewAddsInherentFp(bool gunFired, bool areaFireAgain) => !gunFired || areaFireAgain;
"""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""                molUser == unit.Id, GreenInexperienced(state, unit))));
""",
         """                molUser == unit.Id, GreenInexperienced(state, unit), Is(unit, Conditions.Berserk))));
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
