"""Pass 32.g: add the Rules file and member to the Where column of every inventory row whose code moved in this pass (S6: Close Combat, the advance,
the charge, prisoners, Mopping Up, CC with vehicles, LiveCloseCombat, the projector's fourteen). A row's Where cell is split on ';' into segments
'File.cs: members' (or 'File.cs (qualifier)' or the bare file); the member names choose the Rules functions, grouped by Rules file, unless the row
already names them. A bare file segment is settled by the row's rule number or the file's default. Run after the code; prints each row changed."""
import pathlib
import re

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md")
ADV = "ScenarioA1AdvanceCalculator.cs"
CCR = "ScenarioA1CloseCombatRules.cs"
PRI = "ScenarioA1PrisonerCalculator.cs"
CHG = "ScenarioA1ChargeCalculator.cs"
VCC = "ScenarioA1VehicleCloseCombatRules.cs"
MOP = "ScenarioA1MoppingUp.cs"
FAC = "ScenarioA1CloseCombatFactBuilders.cs"
PROJ = "ScenarioA1CloseCombatProjection.cs"

MOVES = {
    "GamePlanner.CloseCombat.cs": {
        "PlanAdvanceUnits": (ADV, ["AdvancePhaseBar", "AdvanceUnitAllowed", "EntryStackBar", "AdvanceBarringCondition", "MovementEndedBar", "PaatcAfv", "AdvanceStepReason", "MarshBar",
                                   "AdvanceHalfMf", "DifficultAdvance", "CxDifficultBar", "UnarmedBar", "CrewBar", "Surrenders", "Taker", "AdvanceSummary", "PaatcText", "LosesConcealment"]),
        "Opposing": (ADV, ["Opposing"]),
        "DifficultAdvance": (ADV, ["DifficultAdvance"]),
        "OverstackExcess": (ADV, ["OverstackExcess"]),
        "AdvanceAid": (ADV, ["AdvanceAid"]),
        "PlanAmbush": (CCR, ["CcPhaseBar", "AmbushOrderBar", "DarkNight", "AmbushRollSide", "AmbushedLosesConcealment", "AmbushedConditions", "AmbushSummary"]),
        "PlanCloseCombat": (CCR, ["OverrunCc", "RoundPhaseBar", "VehiclePresentBar", "OverrunAttacksBar", "WithdrawalPortageBar", "WithdrawalBar", "WithdrawRequiredText", "InfiltrationBar",
                                  "HandToHandSsrBar", "HandToHandTimingBar", "NextStep", "SelectionKey", "OverrunHoldsInMelee", "DescribedAttacks", "RoundSummary"]),
        "WithdrawalDestinations": (CCR, ["WithdrawalDestination"]),
        "WithdrawalTires": (CCR, ["WithdrawalTires"]),
        "MustWithdraw": (CCR, ["MustWithdraw"]),
        "CloseCombatRequired": (CCR, ["CcRequiredVehicleText", "RequiredUnit", "CcRequiredText"]),
        "MandatoryAttackDecidable": (CCR, ["TrialUnits"]),
        "DeclaringSides": (CCR, ["DeclaringSides"]),
        "AmbushDue": (CCR, ["AmbushDueCandidate", "AmbushRollSide", "AmbushUndecidedText"]),
        "CloseCombatDue": (CCR, ["NothingLeft", "DueOpenLine", "DueAmbushLine", "DueWithdrawLine"]),
        "ChargeBarred": (CCR, ["ChargeBarred"]),
        "FirstRoundStacking": (CCR, ["KeepsFirstRoundStacking"]),
        "CloseCombatEffects": (PRI, ["CreatedLeaderId", "CreatedCcLeaderConditions", "CreatedLeaderPlacedAt", "RoundConditions", "RearmedConditions", "AfterRoundConditions", "Reduced", "MovesAfterRound"]),
        "CaptureEffects": (PRI, ["CaptiveDropsWeapons", "CapturedHalfIds", "CapturedConditions"]),
        "PlanTakePrisoner": (PRI, ["NoSurrenderText", "CanGuard", "FreesOrRedirects", "CaptorCapacityText", "FreedUnarmedText", "NoQuarterText", "CaptorText", "TakenPrisonerConditions", "CaptureSummary"]),
        "KnownEnemyInLos": (PRI, ["KnownEnemyInLos"]),
        "KnownEnemy": (PRI, ["KnownEnemy"]),
        "Captors": (PRI, ["Captor", "Captors", "CanGuard"]),
        "GuardLoad": (PRI, ["CanGuard"]),
        "UnitSize": (PRI, ["UnitSize"]),
        "ChargeSteps": (CHG, ["ChargeTarget", "Steps"]),
        "ChargeEdges": (CHG, ["Edges"]),
        "ChargeFirstMoves": (CHG, ["FirstMoves"]),
        "Charges": (CHG, ["Steps"]),
        "MustCharge": (CHG, ["Steps", "Affordable"]),
        "ChargeLanes": (CHG, ["Steps"]),
    },
    "GamePlanner.Prisoners.cs": {
        "PlanAmbushWithdrawal": (PRI, ["AmbushWithdrawalPhaseBar", "AmbushWithdrawer", "AmbushWithdrawerText", "AmbushWithdrawalToBar", "AmbushWithdrawalSummary"]),
        "PlanGuardPrisoners": (PRI, ["GuardNoneText", "GuardPhaseBar", "PrisonersAbandonedText", "Heir", "HeirText", "GuardCapacityBar", "PrisonersTransferredText"]),
    },
    "GamePlanner.VehicleCloseCombat.cs": {
        "VehicleCcArmament": (VCC, ["VehicleCcArmament"]),
        "CcAttacker": (VCC, ["CcAttacker"]),
        "SidesWithAttacks": (VCC, ["SidesWithAttacks"]),
        "FirstCcSide": (VCC, ["FirstCcSide"]),
        "NextCcSide": (VCC, ["NextCcSide"]),
        "BerserkOwingVehicleAttack": (VCC, ["OwingPossible", "BerserkOwingVehicleAttack"]),
        "BerserkOverrunPending": (VCC, ["OverrunPendingPossible", "OverrunSmc"]),
        "OverrunAttacks": (VCC, ["OverrunAttacks"]),
        "VehicleCloseCombatTurn": (VCC, ["TurnOpen", "Turn"]),
        "PlanVehicleCloseCombat": (VCC, ["VehicleCcPhaseBar"]),
        "PlanReactionFire": (VCC, ["ReactionWindowBar", "ReactionOverrunBar", "ReactionAttacker", "ReactionAttackersBar", "ReactionFinalFireBar"]),
        "CapturedVehicles": (VCC, ["CapturedVehicle"]),
        "PlanSequentialCloseCombat": (VCC, ["NoVehicleCcText", "OtherOpenBar", "BerserkNoPassText", "PassSummary", "VehicleNamedBar", "VehicleAttackOrderBar", "SequentialAttacker", "InfantryAttackersBar"]),
        "WithInexperience": (VCC, ["WithInexperience"]),
        "PlanVehicleAttack": (VCC, ["VehicleRollMissing", "VehicleAttackDice", "WithVehicleAttackRoll", "Destroyed", "ImmobilizedConditions", "Survives", "ReactionFireConditions", "PaatcText", "VehicleAttackSummary"]),
        "NextAfter": (VCC, ["NextCcSide"]),
        "PlanVehicleAttacksInfantry": (VCC, ["VehicleRollMissing", "RandomSelected", "VehicleInfantryDice", "WithVehicleInfantryRoll", "VehicleAttacksInfantrySummary"]),
    },
    "GamePlanner.MoppingUp.cs": {
        "PlanMopUp": (MOP, ["CardBar", "BuildingBar", "PhaseBar", "InBuilding", "SingleHexBar", "OnceBar", "NoQuarterBar", "MopUpUnit", "UnitsBar", "UnitWhy", "EnemyInside", "UnconcealedUnbroken",
                            "RangeBar", "Guard", "HiddenPlacedConditions", "Phrase", "Concealed", "BypassedVehicle", "SecuredSummary", "Surrenders", "SurrenderConditions", "Defender", "CasualtyDrm",
                            "CasualtyReduces", "Struck", "CasualtyText"]),
        "MopUpBuildings": (MOP, ["MopUpBuilding"]),
    },
    "LiveCloseCombat.cs": {
        "Units": (FAC, ["CatalogBar", "IsCcUnit", "OutsideCatalogText", "Unit"]),
        "AmbushFromState": (FAC, ["Ambush"]),
        "FromState": (FAC, ["Overrun", "AmbushFirstBar", "NextRound", "HandToHand", "Round"]),
        "VehicleFromState": (FAC, ["VehicleNotActiveText", "VehicleMoving", "Vehicle", "FireMarked", "VehicleAttack"]),
    },
    "GameProjector.cs": {
        "StartCloseCombat": (PROJ, ["StartUnit", "Contested", "StartEliminates", "StartPlaces"]),
        "InMelee": (PROJ, ["MeleeCandidate", "InMelee"]),
        "HoldsInMelee": (PROJ, ["HoldsInMelee"]),
        "HoldInMelee": (PROJ, ["MeleeChange"]),
        "KeepMelee": (PROJ, ["LeavesMelee"]),
        "Advance": (PROJ, ["AdvanceRefusal", "AdvanceBarred", "AdvanceBarredRefusal", "ExitRefusal"]),
        "MovePrisoners": (PROJ, ["FollowsGuard"]),
        "Ambush": (PROJ, ["AmbushVerifierRefusal", "AmbushRollRefusal", "AmbushOrderRefusal", "VerifierRefusal"]),
        "VehicleCloseCombat": (PROJ, ["CcVerifierRefusal", "CcRollRefusal", "ReactionRecordRefusal", "SequentialRecordRefusal", "VerifierRefusal", "ReactionLeavesState"]),
        "PassVehicleCloseCombat": (PROJ, ["PassRecordRefusal"]),
        "CloseCombat": (PROJ, ["CcVerifierRefusal", "CcRollRefusal", "OverrunRecord", "CcRecordRefusal", "AllowedRounds", "RoundRecordRefusal", "VerifierRefusal", "Closes", "RecordHandToHand"]),
        "RecordLineage": (PROJ, ["ReducedAttackerHasAttacked"]),
        "Capture": (PROJ, ["CaptureNotActiveRefusal", "CaptureBerserkRefusal", "CaptorRefusal"]),
        "FreePrisoner": (PROJ, ["AlreadyFreed", "NotGuardedRefusal"]),
        "KeepGuards": (PROJ, ["GuardLeft", "GuardHeir"]),
    },
}

# Plain-word segments (lower-case substrings, no member name) by file.
WORDS = {
    "GamePlanner.CloseCombat.cs": [
        ("charge", CHG, ["Steps"]),
        ("ovr cc at once", CCR, ["OverrunCc", "RoundPhaseBar"]),
    ],
    "GamePlanner.VehicleCloseCombat.cs": [
        ("berserk ovr", VCC, ["OverrunPendingPossible", "OverrunSmc", "OverrunAttacks"]),
    ],
    "GameProjector.cs": [
        ("advanced", PROJ, ["AdvanceRefusal", "AdvanceBarred"]),
        ("melee", PROJ, ["InMelee", "HoldsInMelee", "LeavesMelee"]),
        ("hiddenplaced", PROJ, ["StartPlaces"]),
        ("startclosecombat", PROJ, ["StartPlaces", "StartEliminates"]),
        ("keepguards", PROJ, ["GuardLeft", "GuardHeir"]),
        ("capture", PROJ, ["CaptorRefusal", "CaptureBerserkRefusal"]),
    ],
}

# A bare file segment, settled by the row's rule number or the file's default.
BARE = {
    "GamePlanner.CloseCombat.cs": {"A3.8": (CCR, ["CcPhaseBar", "RoundPhaseBar"]), "default": (CCR, ["RoundPhaseBar"])},
    "GamePlanner.VehicleCloseCombat.cs": {"default": (VCC, ["VehicleCcPhaseBar"])},
    "GamePlanner.MoppingUp.cs": {"default": (MOP, ["PhaseBar"])},
    "GamePlanner.Prisoners.cs": {"default": (PRI, ["GuardPhaseBar", "AmbushWithdrawalPhaseBar"])},
    "LiveCloseCombat.cs": {"default": (FAC, ["Unit", "Round"])},
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
