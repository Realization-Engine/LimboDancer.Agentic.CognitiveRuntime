"""Pass 32.f: add the Rules file and member to the Where column of every inventory row whose code moved in this pass.
A row's Where cell is split on ';' into segments 'File.cs: members' (or 'File.cs (qualifier)' or the bare file); for each segment of a rout,
Rally, Shock, freed-SMC, or projector segment, the member names and the qualifiers choose the Rules functions, grouped by Rules file, unless the
row already names them. A bare file segment is settled by the row's rule number."""
import pathlib
import re

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md")
ROUT = "ScenarioA1RoutCalculator.cs"
RALLY = "ScenarioA1RallyRules.cs"
PROJ = "ScenarioA1RoutRallyProjection.cs"

# Old file -> old member (word match) -> (Rules file, new members)
MOVES = {
    "GamePlanner.Rout.cs": {
        "MustRout": (ROUT, ["MustRout"]),
        "MayRout": (ROUT, ["MayRout"]),
        "FailureToRout": (ROUT, ["FailureToRoutWhy", "SurrendersInstead", "NoFailureToRout"]),
        "RoutHalfMf": (ROUT, ["RoutHalfMfAsPlanned"]),
        "RoutStepBar": (ROUT, ["RoutStepBar"]),
        "RoutReach": (ROUT, ["RoutReach"]),
        "RoutTargets": (ROUT, ["RoutTargets"]),
        "Interdictor": (ROUT, ["Interdictor"]),
        "OpenGround": (ROUT, ["OpenGround"]),
        "ExposedInOpenGround": (ROUT, ["ExposedInOpenGround"]),
        "NormalRange": (ROUT, ["NormalRange", "OwnRange"]),
        "AdjacentDm": (ROUT, ["DmCandidate", "ArmedEnemyNear"]),
        "RoutPhaseDm": (ROUT, ["DmCandidate", "ExposedInOpenGround", "RoutPhaseDmReason"]),
        "RetainDmBar": (ROUT, ["RetainDmBar"]),
        "BrokenMorale": (ROUT, ["BrokenMorale"]),
        "TrappedByInterdiction": (ROUT, ["TrappedByInterdiction"]),
        "RoutEntry": (ROUT, ["RoutEntry"]),
        "CasualtyReduction": (ROUT, ["CasualtyReduction"]),
    },
    "GamePlanner.Rally.cs": {
        "PlanRally": (RALLY, ["LeaderRallyPhaseActionBar", "LeaderSideBar", "ConcealedAttempt", "SelfRallyRefusal", "Withheld", "RallySummary"]),
        "PlanRepair": (RALLY, ["SwRepairPhaseBar", "SwRepairGoodOrderAsPlanned", "SwRepairRalliedBar", "SwRepairActionBar", "SwRepairWeaponAllowed", "SwRepairNumberBar", "SwRepairResult"]),
        "PlanVehicleRepair": (RALLY, ["VehicleRepairPhaseBar", "VehicleRepairWeaponBar", "VehicleRepairCrewBarAsPlanned", "VehicleMgRepairResult", "VehicleRepairChange"]),
    },
    "GamePlanner.Shock.cs": {"PlanRecoverShock": (RALLY, ["ShockPhaseBar", "OwesShockRoll", "ShockRecoveryWrecks", "ShockRecoveryConditions"])},
    "GamePlanner.FreedSmc.cs": {"WithArmedSmc": (ROUT, ["FreedUnarmedSmc", "ArmsFreedSmc", "SmcArmedReason"])},
    "LiveRally.cs": {},
    "GameProjector.cs": {},
}

# Qualifiers (lower-case substrings of the segment) for the long members, each with its Rules file and functions, and the default.
QUALIFIED = {
    ("GamePlanner.Rout.cs", "PlanRout"): (ROUT, [
        ("lowcrawl", ["RouteShapeBar", "LowCrawlOccupiedBar", "RoutRouteWalk", "RoutDestinationBar"]),
        ("play.night-rout", ["NightRoutBar"]),
        ("no surrender", ["SurrenderCandidate"]),
        ("build", ["InterdictionDue", "InterdictionMorale", "InterdictionEffect", "RoutsOn"]),
    ], ["RoutRouteWalk", "RoutDestinationBar", "RoutSummary"]),
}

# Plain-word segments (no member name) by file.
WORDS = {
    "GamePlanner.Rout.cs": [
        ("open ground test", ROUT, ["OpenGround"]),
        ("low crawl bar", ROUT, ["RoutRouteWalk"]),
        ("play.rout-surrender", ROUT, ["SurrenderCause", "RoutSurrenderText"]),
        ("noquarter", ROUT, ["SurrenderCandidate", "SurrendersInstead"]),
    ],
    "GameProjector.cs": [
        ("rally and repair", PROJ, ["RallyPhase", "VerifyRallyOnce", "SwRepairPhase", "VerifySwRepairOnce"]),
        ("rout events", PROJ, ["RoutPhase", "RoutStepAllowed", "InterdictionAgrees"]),
        ("unit-state-028", PROJ, ["SwRepairRecordAgrees", "VehicleRepairAllowedAsRecorded"]),
        ("surrender-rejected", PROJ, ["RejectSurrenderRefusal", "NoQuarterAfter"]),
        ("prisoners-massacred", PROJ, ["MassacreFirePhase", "MassacreUnitAllowed", "MassacrePrisonerAllowed", "MassacreRaisedElr"]),
    ],
    "LiveRally.cs": [
        ("extremewinterfate", RALLY, ["Attempt"]),
    ],
}

# A bare file segment, settled by the row's rule number.
BARE = {
    "GamePlanner.Rout.cs": {"A3.6": (ROUT, ["RoutPhaseBar"]), "default": (ROUT, ["OpenGround"])},
    "GamePlanner.Rally.cs": {
        "A3.1": (RALLY, ["SwRepairRalliedBar", "SwRepairActionBar", "LeaderRallyPhaseActionBar"]),
        "A15.21": (RALLY, ["HeroConcealed", "RalliedUnitConditions", "ReplacedUnitConditions"]),
        "A15.4": (RALLY, ["RalliedUnitConditions"]),
        "A18.11": (RALLY, ["IsMmcRallier", "SelfRallyRefusal", "CreatedLeaderConditions"]),
        "A25.222": (RALLY, ["ReplacedNotReduced"]),
    },
    "LiveRally.cs": {"A15.41": (RALLY, ["BerserkCompanion", "Attempt"]), "default": (RALLY, ["Attempt", "Commissar"])},
}

lines = PATH.read_bytes().decode("utf-8").split("\n")
changed = 0
for index, line in enumerate(lines):
    if not line.startswith("| "):
        continue
    cells = line.split(" | ")
    if len(cells) < 5:
        continue
    rule = cells[0].lstrip("| ").strip()
    where = cells[4]
    additions = {}
    for segment in where.split(";"):
        segment = segment.strip()
        match = re.match(r"([A-Za-z.]+\.cs)(.*)", segment)
        if not match or match.group(1) not in MOVES:
            continue
        file, rest = match.group(1), match.group(2)
        members = rest.lstrip(": ").strip()
        lower = rest.lower()
        hit = False
        for old, (target, new) in MOVES[file].items():
            if re.search(r"\b" + old + r"\b", members):
                additions.setdefault(target, []).extend(new)
                hit = True
        for (qfile, qmember), (target, keys, default) in QUALIFIED.items():
            if qfile == file and re.search(r"\b" + qmember + r"\b", members):
                found = [new for key, new in keys if key in lower]
                additions.setdefault(target, []).extend(sum(found, []) if found else default)
                hit = True
        for key, target, new in WORDS.get(file, []):
            if key in lower:
                additions.setdefault(target, []).extend(new)
                hit = True
        if not hit and file in BARE:
            target, new = BARE[file].get(rule) or BARE[file].get("default") or (None, [])
            if target:
                additions.setdefault(target, []).extend(new)
    if not additions:
        continue
    parts = []
    for target, names in additions.items():
        kept = []
        for name in names:
            if name not in kept and not re.search(re.escape(target) + r":[^;]*\b" + name + r"\b", where):
                kept.append(name)
        if kept:
            parts.append(f"{target}: {', '.join(kept)}")
    if not parts:
        continue
    cells[4] = where + "; " + "; ".join(parts)
    lines[index] = " | ".join(cells)
    changed += 1
    print(rule, "->", "; ".join(parts))

PATH.write_bytes("\n".join(lines).encode("utf-8"))
print(f"{changed} rows changed")
