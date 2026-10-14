"""Pass 45, task 45.9: a crew's Casualty Reduction (A7.302), and the A7.212 limit on ordnance."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""            if (unit.Definition.Kind == "asl:half-squad")
            {
                unit.Eliminate(reason);
                return true;
            }
""",
         """            // A7.302 (p. 55; pass 45): "Casualty Reduction eliminates any HS or crew". The Recall exception is a vehicle's Inherent crew's (D5.341).
            if (unit.Definition.Kind is "asl:half-squad" or "asl:crew")
            {
                unit.Eliminate(reason);
                return true;
            }
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireRuleModels.cs": [
        ("""    Func<bool>? CcMarkedFirerAtVehicle = null);
""",
         """    Func<bool>? CcMarkedFirerAtVehicle = null, bool Ordnance = false);
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireMapRules.cs": [
        ("""            // A7.212 (ruling R10.14): a unit whose Location holds a Known enemy unit fires at nothing else.
            if (!facts.IsTarget && facts.KnownArmedEnemyThere())
            {
                return new FirerLocationVerdict($"play.fire-target-limit: {facts.Location} holds a Known enemy unit, so its units fire only at their own Location (A7.212)", false);
            }
        }
""",
         """        }

        // A7.212 (p. 55; ruling R10.14): "A unit does not have the freedom to attack units in other Locations while its own Location is occupied by a
        // Known enemy unit", and "Spotters (C9.3) are similarly limited". Pass 45: a Gun, a mortar, and a LATW with the rest, read at the Location
        // the shot is seen from.
        if ((facts.Direct || facts.Ordnance) && !facts.IsTarget && facts.KnownArmedEnemyThere())
        {
            return new FirerLocationVerdict($"play.fire-target-limit: {facts.Location} holds a Known enemy unit, so its units fire only at their own Location (A7.212)", false);
        }
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""                    && (attack.Firers ?? []).Any(item => item.LocationId == location && state.Unit(item.UnitId!) is { } firer && Is(firer, Conditions.CcReaction))));
""",
         """                    && (attack.Firers ?? []).Any(item => item.LocationId == location && state.Unit(item.UnitId!) is { } firer && Is(firer, Conditions.CcReaction)),
                attack.OrdnanceHit is not null));
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
