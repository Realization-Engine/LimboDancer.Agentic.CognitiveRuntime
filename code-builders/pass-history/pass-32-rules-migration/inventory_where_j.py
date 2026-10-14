"""Pass 32.j: add the Rules file and member to the Where column of every inventory row whose code moved in S10 (setup, entry, exit, the Victory
Conditions, concealment at setup). A row's Where cell is split on ';' into segments 'File.cs: members' (or 'File.cs (qualifier)' or the bare file); the
member names and plain words choose the Rules functions, grouped by Rules file, unless the row already names them. Modelled on inventory_where_hi.py."""
import pathlib
import re

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md")
SETUP = "ScenarioA1SetupCalculator.cs"
ENTRY = "ScenarioA1EntryCalculator.cs"
VIC = "ScenarioA1VictoryCalculator.cs"
CONC = "ScenarioA1Concealment.cs"

MOVES = {
    "ScenarioCards.cs": {
        "Validate": (SETUP, ["BoardDiagnostics", "PlayableAreaDiagnostic", "TurnDiagnostics", "SideDiagnostics", "IntegrityDiagnostics", "DefenderDiagnostics", "RuleDiagnostics", "VictoryDiagnostics"]),
        "Side": (SETUP, ["SideDiagnostics"]),
        "Area": (SETUP, ["AreaValid"]),
        "ConditionValid": (SETUP, ["ConditionValid"]),
        "Cited": (SETUP, ["CitedDiagnostics"]),
        "Starting": (SETUP, ["StartsOnTurnOne"]),
        "SquadEquivalents": (SETUP, ["SquadEquivalent"]),
        "IntegrityBpv": (SETUP, ["IntegrityBpv", "Mmc"]),
        "Playable": (SETUP, ["Playable", "HexrowIndex"]),
        "HexrowIndex": (SETUP, ["HexrowIndex"]),
        "ScenarioCardTurns": (SETUP, ["TurnDiagnostics"]),
    },
    "ScenarioSetup.cs": {
        "Within": (SETUP, ["Within"]),
        "Check": (SETUP, ["Check", "SetupStackSquadEquivalents"]),
        "Fill": (SETUP, ["Check", "EntryOf"]),
        "HipAllowance": (SETUP, ["Check"]),
    },
    "SetupPlanMatch.cs": {
        "Look": (CONC, ["PlanLook"]),
    },
    "GamePlanner.CardSetup.cs": {
        "CardSetup": (SETUP, ["SetupCounterOffBoard", "EquipmentHiddenOfItsOwn", "EquipmentSetupSide", "InfantryCouldEnter"]),
        "PlayableBar": (SETUP, ["PlayableBar"]),
        "EntryFor": (ENTRY, ["EntryFor"]),
        "EntryHexesFor": (ENTRY, ["EntryHexesFor", "IsRiver"]),
        "IsRiver": (ENTRY, ["IsRiver"]),
        "EntryHexes": (ENTRY, ["EntryHex"]),
        "EntryHexBar": (ENTRY, ["EntryObstructor", "EntryHexBar"]),
        "EntryDue": (ENTRY, ["EntryDue"]),
        "AdvanceEntries": (ENTRY, ["AdvanceEntries"]),
        "OpenEntryHexes": (ENTRY, ["OpenEntryHexes"]),
        "EntryCheck": (ENTRY, ["EntryCheck"]),
        "CardSetupIncomplete": (SETUP, ["SetupIncompleteBar"]),
    },
    "GamePlanner.Victory.cs": {
        "HexLevels": (VIC, ["HexLevels"]),
        "VictoryPoints": (VIC, ["VictoryPoints"]),
        "VehicleVictoryPoints": (VIC, ["VehicleVictoryPoints"]),
        "HasInherentCrew": (VIC, ["VehicleArmed", "HasInherentCrew"]),
        "WithImmediateVictory": (VIC, ["ImmediateVictoryRead", "NothingLeftOpen"]),
        "ExitScoring": (ENTRY, ["ExitMeetsCondition", "ExitScoring"]),
        "PlanExit": (ENTRY, ["ExitPhaseBar", "ExitStackBar", "ExitFromBar", "ExitUnableBar", "ExitGunBar", "ExitMovementBar", "ExitEdgeBar", "BypassExit", "ExitCost", "PushedExit", "AdvanceExit", "ExitMfBar"]),
    },
    "ScenarioVictory.cs": {
        "Evaluate": (VIC, ["Evaluate"]),
        "LocationControl": (VIC, ["LocationControl"]),
        "Control": (VIC, ["Evaluate"]),
        "StartSide": (VIC, ["StartSide"]),
        "Armed": (VIC, ["Armed"]),
        "Gainer": (VIC, ["Evaluate"]),
        "Undeclared": (VIC, ["UnbrokenSquads"]),
        "UnbrokenSquads": (VIC, ["UnbrokenSquads"]),
        "Qualifies": (ENTRY, ["ExitMeetsCondition"]),
        "EscortEdge": (VIC, ["EscortEdge"]),
        "ExitVp": (VIC, ["Evaluate"]),
        "Cvp": (VIC, ["Evaluate"]),
        "Holds": (VIC, ["Evaluate"]),
        "GunVp": (VIC, ["Evaluate"]),
    },
    "GamePlanner.Disclosure.cs": {
        "OutOfSight": (SETUP, ["OutOfSight"]),
        "NonObConcealment": (CONC, ["NonObConcealmentBar", "NonObConcealment", "NonObLocationBar"]),
        "PlanNonObConcealment": (CONC, ["NonObGameBar", "NonObLocationBar", "NonObConcealmentText"]),
        "PlanPlaceHidden": (CONC, ["PlaceHiddenSetupBar", "PlaceHiddenUnitsBar", "PlaceHiddenConditions"]),
    },
    "GameProjector.cs": {
        "SetupConceal": (CONC, ["SetupConceal"]),
    },
}

WORDS = {
    "ScenarioCards.cs": [
        ("card.san", SETUP, ["SideDiagnostics"]),
        ("card.elr", SETUP, ["SideDiagnostics"]),
        ("card.", SETUP, ["SideDiagnostics"]),
    ],
    "ScenarioSetup.cs": [
        ("stacking", SETUP, ["Check", "SetupStackSquadEquivalents"]),
        ("hidden", SETUP, ["Check"]),
        ("emplaced", SETUP, ["Check"]),
        ("deploy", SETUP, ["Check"]),
    ],
    "ScenarioVictory.cs": [
        ("armed good order", VIC, ["Armed", "Evaluate"]),
        ("control", VIC, ["Evaluate"]),
    ],
    "GamePlanner.Victory.cs": [
        ("exit push", ENTRY, ["ExitGunBar", "PushedExit", "PushOffDrm"]),
        ("exit with prisoners", ENTRY, ["ExitScoring", "ExitWith"]),
        ("exits", ENTRY, ["ExitScoring"]),
    ],
    "GamePlanner.Disclosure.cs": [
        ("non-ob", CONC, ["NonObLocationBar"]),
    ],
}

BARE = {
    "ScenarioSetup.cs": {"default": (SETUP, ["Check"])},
    "ScenarioCards.cs": {"default": (SETUP, ["SideDiagnostics"])},
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
        if rest and not rest.startswith((":", " ", "(")):
            continue
        members = rest.lstrip(": ").strip()
        lower = rest.lower()
        hit = False
        for old, (target, new) in MOVES[file].items():
            if re.search(r"\b" + old + r"\b", members):
                additions.setdefault(target, []).extend(new)
                hit = True
        if not hit:
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
