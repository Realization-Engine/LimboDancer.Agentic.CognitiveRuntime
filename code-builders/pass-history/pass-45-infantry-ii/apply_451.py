"""Pass 45, task 45.1: fire between two levels of one hex as PBF (A7.21, B23.25, B23.26, A8.4), and Encirclement from above and below (A7.7)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""            if (range < (tpbf ? 0 : 1) || (tpbf && range != 0) || inherentOut || weaponOut)
            {
                outside.Add("asl.a1.fire.out-of-range");
            }
""",
         """            // A7.21, B23.26 (pp. 54, 136; pass 45): "Attacks up or down a stairwell hex to other vertical levels of the same building hex are limited
            // to the next higher or lower level." Range 0 from another Location is the next level of the target's hex, whose LOS the planner read.
            var nextLevel = !tpbf && range == 0 && firer.LocationId != attack.TargetLocationId && Math.Abs(LevelAboveOf(attack, firer)) == 1;
            if ((range < (tpbf ? 0 : 1) && !nextLevel) || (tpbf && range != 0) || inherentOut || weaponOut)
            {
                outside.Add("asl.a1.fire.out-of-range");
            }
"""),
        ("""            // A7.21 (rulings R10.4, R10.14): PBF at an adjacent target at most one level above the firer; TPBF in the firer's own Location.
            if (range == 1 && levelAbove <= 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }
            else if (range == 0)
""",
         """            // A7.21 (rulings R10.4, R10.14): PBF at an adjacent target at most one level above the firer; TPBF in the firer's own Location.
            // Pass 45: PBF, not TPBF, at the next level of the firer's own hex ("doubled while ... ADJACENT to its target"; TPBF is for "units in
            // the same Location as the attacker").
            if ((range == 1 && levelAbove <= 1) || (range == 0 && firer.LocationId != attack.TargetLocationId))
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }
            else if (range == 0)
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireMapRules.cs": [
        ("""    /// <summary>B.10: a LOS Hindrance DRM of +6 or more blocks the LOS completely.</summary>
""",
         """    /// <summary>
    /// Fire from one level of a hex at another level of it (A7.21, B23.25, B23.26, pp. 54 and 136; pass 45): "A unit in a building is ADJACENT to an
    /// adjacent building level of the same building only if both are ... vertically connected by a stairwell", "LOS to a unit in a building Location
    /// ... can exist only if drawn to an ADJACENT unit", and such attacks "are limited to the next higher or lower level". The refusal, or null.
    /// </summary>
    public static string? SameHexLevelBar(string from, string target, int levelsApart, Func<bool> adjacent)
    {
        ArgumentNullException.ThrowIfNull(adjacent);
        return levelsApart != 1 || !adjacent()
            ? $"play.fire-level: {from} fires at {target}, another level of its hex, only when it is the next level up or down and a stairwell joins them (B23.25, B23.26)"
            : null;
    }

    /// <summary>The Encirclement position of a LOF from the Location directly above (A7.7); below is <see cref="FromBelow"/>.</summary>
    public const int FromAbove = 12;

    /// <summary>The Encirclement position of a LOF from the Location directly below (A7.7).</summary>
    public const int FromBelow = 13;

    /// <summary>B.10: a LOS Hindrance DRM of +6 or more blocks the LOS completely.</summary>
"""),
        ("""            if (normal is not { } limit || range is not { } distance || distance < 1 || distance > limit || !firers[index].LocationKnown)
            {
                continue;
            }

            units++;
""",
         """            // A7.7 (p. 57; pass 45): "An Encirclement can also be created by a LOF from both the Location directly above and below it in a building hex."
            if (range == 0 && normal is not null && firer.LocationId != facts.TargetLocationId && (firer.TargetLevelAbove ?? facts.TargetLevelAbove) is { } levelAbove
                && Math.Abs(levelAbove) == 1 && firers[index].LocationKnown)
            {
                units++;
                entries.Add(levelAbove > 0 ? FromBelow : FromAbove);
                continue;
            }

            if (normal is not { } limit || range is not { } distance || distance < 1 || distance > limit || !firers[index].LocationKnown)
            {
                continue;
            }

            units++;
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1Geometry.cs": [
        ("""        var set = positions.ToHashSet();
        if (set.Any(position => set.Contains((position + 6) % 12)))
        {
            return true;
        }
""",
         """        var set = positions.ToHashSet();

        // A7.7 (pass 45): a LOF from both the Location directly above (12) and the one directly below (13).
        if (set.Contains(12) && set.Contains(13))
        {
            return true;
        }

        if (set.Any(position => position < 12 && set.Contains((position + 6) % 12)))
        {
            return true;
        }
"""),
        ("""        var sides = set.Where(position => position % 2 == 1).Select(position => (position - 1) / 2).ToHashSet();
""",
         """        var sides = set.Where(position => position < 12 && position % 2 == 1).Select(position => (position - 1) / 2).ToHashSet();
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/FireRange.cs": [
        ("""    /// <summary>Another level of the firer's own hex, which the Fire package does not resolve.</summary>
    SameHexOtherLevel,
""",
         """    /// <summary>Another level of the firer's own hex: PBF at the next level up or down a stairwell (A7.21, B23.26; pass 45).</summary>
    SameHexOtherLevel,
"""),
        ("""        FireRangeBand.PointBlank => 2m,
""",
         """        FireRangeBand.PointBlank or FireRangeBand.SameHexOtherLevel => 2m,
"""),
        ("""            FireRangeBand.SameHexOtherLevel => "the same hex, another level: not built",
""",
         """            FireRangeBand.SameHexOtherLevel => "PBF, FP x2, at the next level of its hex by a stairwell (A7.21, B23.26)",
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""            LosResult los;
            int range;
            if (snapHexside is { } hexside)
""",
         """            // A7.21, B23.25, B23.26 (pass 45): another level of the firer's own hex is fired at as the next level a stairwell joins, at range 0.
            if (direct && from.Board == target.Board && from.Hex == target.Hex)
            {
                if (ScenarioA1FireMapRules.SameHexLevelBar(from.ToString(), target.ToString(), Math.Abs(from.Level - target.Level), () => IsAdjacent(state, from, target)) is { } levelBar)
                {
                    return (null, levelBar);
                }

                perLocation[location] = (0, false, ScenarioA1FireMapRules.OwnLocationLos, height);
                continue;
            }

            LosResult los;
            int range;
            if (snapHexside is { } hexside)
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
