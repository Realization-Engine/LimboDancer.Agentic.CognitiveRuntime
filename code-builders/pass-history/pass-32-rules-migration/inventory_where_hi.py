"""Passes 32.h and 32.i: add the Rules file and member to the Where column of every inventory row whose code moved in the block (S9: night, weather,
Starshells, Snipers; S2: the sequence of play, the game's start, the choices and Massacre, the A1 entry, the projector's sequence members). A row's Where
cell is split on ';' into segments 'File.cs: members' (or 'File.cs (qualifier)' or the bare file); the member names and plain words choose the Rules
functions, grouped by Rules file, unless the row already names them. A bare file segment is settled by the row's rule number or the file's default.
Run after the code; prints each row changed. Modelled on inventory_where_32g.py."""
import pathlib
import re

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md")
NAW = "ScenarioA1NightAndWeather.cs"
STAR = "ScenarioA1Starshells.cs"
SNI = "ScenarioA1Sniper.cs"
SEQ = "ScenarioA1SequenceCalculator.cs"
START = "ScenarioA1GameStart.cs"
ENT = "ScenarioA1EntryRules.cs"
PROJ = "ScenarioA1SequenceProjection.cs"

MOVES = {
    "GamePlanner.Night.cs": {
        "NightAndWeatherRulesBar": (NAW, ["RulesBar", "NightRule"]),
        "Rain": (NAW, ["Rain"]),
        "InfantryWeatherHalfMf": (NAW, ["InfantryWeatherHalfMf"]),
        "VehicleWeatherHalfMp": (NAW, ["VehicleWeatherHalfMp"]),
        "NvrLimits": (NAW, ["NvrLimits"]),
        "NvrOf": (NAW, ["NvrOf"]),
        "Illuminated": (NAW, ["Illuminated"]),
        "Gunflash": (NAW, ["Gunflash", "GunflashMarkers"]),
        "NightSight": (NAW, ["NightSight", "MotionVehicleSightRange"]),
        "ExtremeWinterReduction": (NAW, ["ExtremeWinterReduction"]),
        "NightAndWeatherFacts": (NAW, ["NightAndWeatherFacts"]),
        "WindChangeDue": (NAW, ["WindChangeDue"]),
        "AddWindChange": (NAW, ["WindChange", "WindChangeText"]),
    },
    "GamePlanner.Starshells.cs": {
        "PlanStarshell": (STAR, ["DayBar", "FirerBar", "PhaseBar", "OnceBar", "FirstBar", "TimingBar", "ThreeHexesBar", "AtTargetBar", "UsageNeed", "UsagePassed", "PlacementExtent",
                                 "HiddenFirerConditions", "Summary"]),
        "Seen": (NAW, ["NightSight"]),
    },
    "GamePlanner.Snipers.cs": {
        "AddSniperAttacks": (SNI, ["TriggerPhase", "RollKey", "RollMaker", "SniperActivationNumber", "Triggers"]),
        "FiringSideOf": (SNI, ["FiringSide"]),
        "Snipe": (SNI, ["Attacks", "Effective", "RandomLocation", "Eligible", "TargetTem", "TargetHex", "TargetLocation", "Candidates", "Tied", "Picked", "ComesUnderDm"]),
        "SnipeUnit": (SNI, ["UnitEffect", "Mortal", "ReductionResult", "WoundedConditions", "BrokenConditions", "PinnedConditions"]),
    },
    "GamePlanner.Choices.cs": {
        "PlanMassacre": (SEQ, ["MassacreTarget", "MassacreTargetText", "MassacrePhaseBar", "MassacreUnitBar", "MassacreOnceBar", "MassacreFiredBar", "MassacreMarker", "MassacreSummary"]),
        "BerserkMassacres": (SEQ, ["BerserkMassacringSide", "BerserkMassacres", "MassacredPrisoner", "BerserkMassacreText"]),
        "PlanChoose": (SEQ, ["NoChoiceText", "OptionBar", "ChoiceSummary", "ResumeBar"]),
        "Pending": (SEQ, ["OptionKey", "PendingChoice"]),
        "DescribeChoice": (SEQ, ["DescribeChoice"]),
    },
    "GamePlanner.Proposer.cs": {
        "PhaseEndedBy": (SEQ, ["PhaseEndedBy"]),
        "ProposerBar": (SEQ, ["ProposerChecked", "Owner", "NotYourActionBar", "RoutPhaseEndBar", "ActorArguments", "NotYourUnitBar"]),
    },
    "GamePlanner.cs": {
        "PlanDeclareOverrun": (ENT, ["NoPendingDeclarationText", "DeclarationExists", "InitialOccupancy", "DeclinedText", "Election", "ElectionMfBar", "NoOtherConcealedBar", "NtcTem",
                                     "NtcPassed", "ElectedText"]),
        "Balance": (START, ["Balance", "PlayerName", "Higher", "BalancePlayers"]),
    },
    "GameProjector.cs": {
        "ChangePhase": (PROJ, ["TurnRefusal", "OpenAttemptRefusal", "ChoiceRefusal", "SurrenderRefusal", "NothingLeft", "OpenCloseCombatRefusal", "MeleeEliminationOwed",
                               "MeleeEliminationRefusal", "ClearedMarkers", "CxRests", "StunsEnd", "NoMoveListGrows", "RemovesSmokeGrenades", "RemovesStarshells"]),
        "End": (PROJ, ["EndRefusal"]),
        "EndStuns": (PROJ, ["StunsEnd", "StunEnds"]),
        "NoMoveThisPlayerTurn": (PROJ, ["NoMoveListGrows", "JoinsNoMoveList"]),
        "RemoveSmokeGrenades": (PROJ, ["RemovesSmokeGrenades", "SmokeGrenade"]),
        "ChangeWind": (NAW, ["VerifyWindChange", "RainedAfter"]),
        "MovedVehicles": (PROJ, ["MovedThisPlayerTurn"]),
        "BoreSight": (PROJ, ["BoreSightRefusal"]),
        "NoQuarter": (PROJ, ["HungariansVersusRomanians"]),
        "Nation": (PROJ, ["NationMisnamed", "NationRefusal"]),
    },
    "GameTypes.cs": {
        "HungariansVersusRomanians": (PROJ, ["HungariansVersusRomanians"]),
    },
}

# Plain-word segments (lower-case substrings, no member name) by file.
WORDS = {
    "GamePlanner.Starshells.cs": [
        ("starshell", STAR, ["FirerBar", "PhaseBar"]),
    ],
    "GamePlanner.Snipers.cs": [
        ("san", SNI, ["SniperActivationNumber", "Triggers"]),
    ],
    "GamePlanner.cs": [
        ("random selection", ENT, ["RandomSelectionText", "LoneSmcRevealed"]),
        ("game end", SEQ, ["GameEnds", "LastPhase", "GameEndReason"]),
        ("a4.14 case", ENT, ["Route", "ProhibitedText", "A414Exception"]),
        ("ovr ntc", ENT, ["Election", "ElectionMfBar", "NtcTem", "NtcPassed", "ElectedText"]),
        ("ccph end", SEQ, ["MeleeEliminated", "MeleeEliminatedText"]),
        ("melee-eliminated", SEQ, ["MeleeEliminated", "MeleeEliminatedText"]),
        ("concealed entry", ENT, ["Route", "SampleBranch", "ForcedBackText", "DeclarationPendingText", "LoneSmcRevealed"]),
        ("declaration", ENT, ["DeclarationExists", "DeclinedText"]),
        ("mph end", SEQ, ["BerserkChargeBar", "BerserkReturns", "BerserkEndsText"]),
        ("berserk-ends", SEQ, ["BerserkReturns", "BerserkEndsConditions", "BerserkEndsText"]),
        ("play.group", START, ["NeedsGroup", "GroupText"]),
        ("phase change to ccph", SEQ, ["VehicleCaptureDue", "CapturedVehicleConditions", "CcVehicleCaptureText"]),
        ("afph hold", SEQ, ["PlacedChargeDue", "DcDetonatePendingText"]),
        ("commissar", SEQ, ["CommissarCandidate", "CommissarOwes", "CommissarRallyText"]),
        ("ntc modifier", ENT, ["NtcTem", "NtcTemRule"]),
        ("bore sight", START, ["BoreSightBar"]),
        ("abandonment", SEQ, ["RecallAbandoned", "RecallAbandonedText"]),
        ("night dm", SEQ, ["NightDmShed", "NightDmKept", "NightDmText"]),
    ],
    "GameProjector.cs": [
        ("random-selection", PROJ, ["SelectionAttemptRefusal", "SelectionOrderRefusal", "SelectionRollRefusal", "SelectionSubjectsRefusal", "SelectionSubjectRefusal", "Revealing",
                                    "RevealOutsideSelectionRefusal"]),
        ("markers cleared", PROJ, ["ClearedMarkers"]),
        ("pins and ti", PROJ, ["ClearedMarkers"]),
        ("pins removed", PROJ, ["ClearedMarkers"]),
        ("first and final fire counters", PROJ, ["ClearedMarkers"]),
        ("cx removal", PROJ, ["CxRests", "CxRestedConditions", "LosesCx"]),
        ("game start", PROJ, ["SidesRefusal", "GroupsRefusal", "NationalityRefusal", "NationMisnamed", "NationRefusal", "HungariansVersusRomanians", "RainedAtStart"]),
        ("movement-step smoke", PROJ, ["SmokePendingLapses", "SmokeCreationRefusal"]),
        ("starshells removed", PROJ, ["RemovesStarshells", "Starshell"]),
    ],
}

# A bare file segment, settled by the row's rule number or the file's default.
BARE = {
    "GamePlanner.Snipers.cs": {"A10.62": (SNI, ["ComesUnderDm"]), "A12.14": (SNI, ["WoundedConditions", "BrokenConditions", "PinnedConditions"]), "default": (SNI, ["Triggers"])},
    "GamePlanner.Choices.cs": {"default": (SEQ, ["PendingChoice", "DescribeChoice"])},
    "GameProjector.cs": {"A11.16": (PROJ, ["MeleeEliminationOwed", "MeleeEliminationRefusal"])},
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
