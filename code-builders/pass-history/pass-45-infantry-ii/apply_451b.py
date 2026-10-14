"""Pass 45, task 45.1b: the readers of Encircled take the planner's read, which adds A7.72."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""        return NightAndWeatherFacts(state, facts, target, targetRead.Hex.BaseLevel + (targetRead.Hex.Center.Terrain?.Height ?? 0), perLocation);
    }
""",
         """        return UpperLevelEncircled(state, NightAndWeatherFacts(state, facts, target, targetRead.Hex.BaseLevel + (targetRead.Hex.Center.Terrain?.Height ?? 0), perLocation));
    }

    /// <summary>
    /// A7.72 (p. 58; pass 45): the firers and targets in an upper building level with no path to ground level are Encircled, as a read of the map
    /// recorded with the attack; a unit Encircled by a counter is so already.
    /// </summary>
    private (FireAttack? Facts, string? Reason) UpperLevelEncircled(GameState state, (FireAttack? Facts, string? Reason) read)
    {
        if (read.Facts is not { } facts)
        {
            return read;
        }

        bool CutOff(string? id) => id is not null && state.Unit(id) is { } unit && UpperLevelEncircled(state, unit);
        return (facts with
        {
            Firers = facts.Firers is null ? null : [.. facts.Firers.Select(firer => firer.Encircled != true && CutOff(firer.UnitId) ? firer with { Encircled = true } : firer)],
            Targets = facts.Targets is null ? null : [.. facts.Targets.Select(target => target.Encircled != true && target.Dummy != true && CutOff(target.UnitId)
                ? target with { Encircled = true } : target)],
        }, read.Reason);
    }
""", 1),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""                    // Pass 31d (ruling R31d.2): who sees a concealed firer is the planner's read of the map, recorded with the attack.
                    SeenByGoodOrderEnemy = record.SeenByGoodOrderEnemy,
""",
         """                    // Pass 31d (ruling R31d.2): who sees a concealed firer is the planner's read of the map, recorded with the attack.
                    SeenByGoodOrderEnemy = record.SeenByGoodOrderEnemy,

                    // A7.72 (pass 45): a firer cut off in an upper building level is Encircled by a read of the map, recorded with the attack.
                    Encircled = fact.Encircled ?? record.Encircled,
""", 1),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.CloseCombat.cs": [
        ("units.Any(unit => state.Encircled(unit!))", "units.Any(unit => Encircled(state, unit!))", 2),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs": [
        ("movers.Any(state.Encircled)", "movers.Any(mover => Encircled(state, mover))", 1),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Rout.cs": [
        ("Is(item.Unit, Conditions.Pinned), state.Encircled(item.Unit),", "Is(item.Unit, Conditions.Pinned), planner.Encircled(state, item.Unit),", 1),
        ("SurrenderCause(Is(unit, Conditions.Disrupted), state.Encircled(unit),", "SurrenderCause(Is(unit, Conditions.Disrupted), Encircled(state, unit),", 1),
        ("limit, state.Encircled(unit),", "limit, Encircled(state, unit),", 1),
        ("        var encircled = state.Encircled(unit);\n", "        var encircled = Encircled(state, unit);\n", 1),
    ],
    "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor": [
        (""".Concat(state.Encircled(unit) ? ["Encircled (A7.7)"] : [])""",
         """.Concat(state.Encircled(unit) ? ["Encircled (A7.7)"] : Live.Planner.UpperLevelEncircled(state, unit) ? ["Encircled: no path to ground level (A7.72)"] : [])""", 1),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new, count in edits:
        if text.count(old) != count:
            sys.exit(f"{name}: anchor found {text.count(old)} times, not {count}: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
