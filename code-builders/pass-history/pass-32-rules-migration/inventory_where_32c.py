"""Pass 32.c: add the Rules file and member to the Where column of every inventory row whose code moved in this pass.
A row's Where cell is split on ';' into segments 'File.cs: members'; for each segment of a Play or Units fire file, the segment's member names
and its qualifiers (in parentheses or plain words) choose the Rules functions, grouped by Rules file, unless the row already names them."""
import pathlib
import re

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md")
ELIG = "ScenarioA1FireEligibility.cs"
MAP = "ScenarioA1FireMapRules.cs"
FOLLOW = "ScenarioA1FireFollowUps.cs"

# Old file -> old member (word match) -> (Rules file, new members)
MOVES = {
    "GamePlanner.Fire.cs": {
        "FireBar": (ELIG, ["FireBar"]),
        "VehicleEffectEvents": (FOLLOW, ["VehicleEffect"]),
        "HazardousMovement": (MAP, ["HazardousMovement"]),
        "AddFireFollowUps": (FOLLOW, ["SprayTargetSide", "PlacesEncirclement", "PlacesFireLane", "ThrowerAttack"]),
        "HeatOfBattleFacts": (ELIG, ["RevealedByAttack"]),
    },
    "GamePlanner.FireExtensions.cs": {
        "ConcealmentGains": (FOLLOW, ["ConcealmentGains"]),
        "AddConcealmentGains": (FOLLOW, ["ConcealmentDr"]),
        "PlanOpportunityFire": (ELIG, ["OpportunityFirerBar", "OpportunityFireReveals"]),
        "FireLaneFacts": (FOLLOW, ["FireLaneAttack"]),
        "FireLaneEntries": (MAP, ["FireLaneEntries"]),
        "AddFireLaneAttack": (FOLLOW, ["FireLaneAttacks", "FireLaneMalfunctions"]),
        "EncirclementShare": (MAP, ["EncirclementShare", "PerimeterPosition"]),
        "EncirclementSeal": (MAP, ["EncirclementSeal"]),
    },
    "GamePlanner.Acquisition.cs": {"AcquisitionFollowUp": (FOLLOW, ["AcquisitionFollowUp"])},
    "LiveFire.cs": {
        "CrewExposed": (ELIG, ["CrewExposed"]),
        "Vehicle": (ELIG, ["VehicleTarget"]),
        "OverrunFromState": (ELIG, ["Overrun"]),
        "ResidualFromState": (ELIG, ["Residual"]),
        "DemolitionChargeFromState": (ELIG, ["DemolitionCharge"]),
        "MayBoundingFire": (ELIG, ["MayBoundingFire"]),
        "Fired": (ELIG, ["Fired"]),
        "FireSpent": (ELIG, ["FireSpent"]),
        "CapturedBy": (ELIG, ["CapturedBy"]),
        "WeaponBar": (ELIG, ["WeaponBar"]),
        "Inexperienced": (ELIG, ["InexperiencedForFire"]),
        "GreenInexperienced": (ELIG, ["GreenInexperienced"]),
        "Allies": (ELIG, ["Allies"]),
    },
    "GameProjector.cs": {
        "PlaceResidual": (FOLLOW, ["VerifyResidualFp"]),
        "KeepFireLanes": (FOLLOW, ["FireLaneStands"]),
        "PlaceFireLane": (FOLLOW, ["VerifyFireLane"]),
        "KeepAcquisitions": (FOLLOW, ["AcquisitionHolds", "AcquisitionAfter"]),
        "ChangeAcquisition": (FOLLOW, ["VerifyAcquisitionChange"]),
        "KeepEncirclements": (FOLLOW, ["EncirclementStands"]),
        "Encircle": (FOLLOW, ["VerifyEncirclement"]),
        "DeclareOpportunityFire": (FOLLOW, ["VerifyOpportunityFire"]),
        "SupportWeapons": (FOLLOW, ["SupportWeaponUse", "PanzerfaustShotCounts"]),
    },
}

# Qualifiers (lower-case substrings of the segment) for the long members, each with its Rules file and functions.
QUALIFIED = {
    ("GamePlanner.Fire.cs", "FireMapFacts"): (MAP, [
        ("play.fire-terrain", ["TargetTerrainBar", "HexsideTerrainBar"]),
        ("hexside terrain", ["HexsideTerrainBar"]),
        ("play.fire-target-limit", ["FirerLocation"]),
        ("play.fire-marsh", ["FirerLocation"]),
        ("own location", ["FirerLocation"]),
        ("lane terrain", ["UsesBypassLane", "BypassTerrain"]),
        ("bypass lane", ["UsesBypassLane", "BypassTerrain"]),
        ("fire-bypass", ["BypassTerrain", "BypassLosBar"]),
        ("snap", ["SnapShotBar", "SnapShotLos"]),
        ("mapranges", ["LocationLos"]),
        ("attributed", ["LocationLos"]),
        ("marsh counted", ["LocationLos"]),
        ("june", ["LocationLos"]),
        ("grain", ["GrainTerrain"]),
        ("targetterrain", ["GrainTerrain", "GroupMapFacts"]),
        ("residual fp", ["ResidualMapFacts"]),
        ("pushedgun", ["HazardousMovement"]),
        ("firerlocationsadjacent", ["GroupMapFacts"]),
        ("closest known enemy", ["ReadsSubsequentFirstFireRange", "WithinSubsequentFirstFireRange"]),
    ], ["FirerLocation", "LocationLos", "GroupMapFacts"]),
    ("GamePlanner.Fire.cs", "PlanFire"): (ELIG, [
        ("play.fire-window", ["MphWindow"]),
        ("play.fire-weapons", ["EveryMgBar"]),
        ("play.fire-mf-limit", ["MfLimitBar"]),
        ("play.fire-smc", ["LeaderMgBar"]),
        ("play.fire-lane-mg", ["FireLaneInUseBar"]),
        ("play.fire-group", ["FireGroupBar"]),
        ("firelane", ["FireLaneDeclarationBar"]),
        ("fire lane refused", ["FireLaneDeclarationBar"]),
        ("blockedfirst", ["BlockedLocations", "PartFirers", "PartDirectors"]),
        ("mol", ["MolBar"]),
        ("spray", ["SprayTargetBar", "SprayPhaseBar", "SprayGroupBar", "SprayAdjacentBar"]),
    ], ["GroupFireBar", "FireGroupBar"]),
    ("GamePlanner.Fire.cs", "AddFireEvents"): (FOLLOW, [
        ("residual-fp-placed", ["ResidualFpPlaced"]),
        ("larger counter", ["ResidualFpPlaced"]),
        ("overruneffect", ["OverrunEffectConditions"]),
        ("malfunctioned", ["WeaponEffectConditions"]),
        ("resolved whole", ["NextFireStep"]),
    ], ["NextFireStep", "HidesIdentity"]),
    ("LiveFire.cs", "FromState"): (ELIG, [
        ("possession", ["NamedWeaponsBar"]),
        ("sw limit", ["NamedWeaponsBar", "FirerLimitBar"]),
        ("play.fire-sw-limit", ["NamedWeaponsBar", "FirerLimitBar"]),
        ("dismantled", ["NamedWeaponsBar"]),
        ("play.fire-vehicle-group", ["VehicleFiresAlone"]),
        ("vehicles of the second", ["SelectTargets"]),
        ("targets", ["SelectTargets"]),
        ("movers only", ["SelectTargets"]),
        ("partner", ["PartnerBar"]),
        ("kind by markers", ["MphFireKind"]),
        ("director", ["Director"]),
    ], ["Firer", "Assemble"]),
}

# Plain-word segments (no member name) of the fire files.
WORDS = {
    "GamePlanner.Fire.cs": [
        ("vehicle effect events", FOLLOW, ["VehicleEffect"]),
        ("fire eligibility", ELIG, ["FireBar"]),
        ("blocked firers", ELIG, ["BlockedLocations", "PartFirers"]),
        ("residual fp smoke", MAP, ["ResidualMapFacts"]),
        ("defensive first fire in the defender", ELIG, ["MphWindow"]),
        ("dm on break", FOLLOW, ["AttackedWhileBroken", "Effect"]),
        ("a7.212 limit", MAP, ["FirerLocation"]),
        ("hexside bar", MAP, ["HexsideTerrainBar"]),
        ("final fire bar", ELIG, ["Fired"]),
    ],
    "GamePlanner.FireExtensions.cs": [
        ("opportunity fire", ELIG, ["OpportunityFirerBar"]),
        ("concealment dr", FOLLOW, ["ConcealmentGains", "ConcealmentDr"]),
        ("lane breakdown", FOLLOW, ["FireLaneMalfunctions"]),
    ],
    "LiveFire.cs": [
        ("one ft or dc", ELIG, ["NamedWeaponsBar"]),
        ("fired-this-phase read", ELIG, ["Fired"]),
        ("ti bar", ELIG, ["FirerBar"]),
        ("prep fired units barred", ELIG, ["FirerLimitBar"]),
        ("ft possessed", ELIG, ["Target"]),
    ],
}

lines = PATH.read_text(encoding="utf-8").split("\n")
changed = 0
for index, line in enumerate(lines):
    if not line.startswith("| "):
        continue
    cells = line.split(" | ")
    if len(cells) < 5:
        continue
    where = cells[4]
    additions = {}
    for segment in where.split(";"):
        segment = segment.strip()
        match = re.match(r"([A-Za-z.]+\.cs):\s*(.*)", segment)
        if not match or match.group(1) not in MOVES:
            continue
        file, members = match.group(1), match.group(2)
        lower = members.lower()
        for old, (target, new) in MOVES[file].items():
            if re.search(r"\b" + old + r"\b", members):
                additions.setdefault(target, []).extend(new)
        for (qfile, qmember), (target, keys, default) in QUALIFIED.items():
            if qfile == file and re.search(r"\b" + qmember + r"\b", members):
                found = [new for key, new in keys if key in lower]
                additions.setdefault(target, []).extend(sum(found, []) if found else default)
        for key, target, new in WORDS.get(file, []):
            if key in lower:
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

with open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write("\n".join(lines))
print(f"{changed} rows changed")
