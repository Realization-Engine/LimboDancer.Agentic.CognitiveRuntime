"""Pass 60 documents: the rule inventory's rows that waited on pass 60, and the rows of other rules the pass's code touched. Each row keeps its note
and gains a sentence; its status, its code and test cells, and the pass that takes what is left are set here. Pass 60's line of the second count
table is renamed; the counts themselves are left to code-builders/documents/inventory_count_tables.py.

Usage: python docs_inventory60.py [--check]
    --check   says which rows wait on pass 60 and whether every one is named; writes nothing
"""
import collections
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("\ufeff")
lines = text.split("\n")
B = "Pass 60 (2026-10-15): "
R = "ScenarioA1Pass60RulesTests."
Y = "BacklogPass60Tests."
G = "PlayPagePass60Tests."
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]


def tests(*names):
    return "; ".join(names)


# rule -> (status or None to keep it, the pass for what is left or "", text added to Where, text added to Test, the sentence added to the note)
SET = {
    "A5.132": (None, "Pass 110", "", "",
               "not built; moved to pass 110 beside OVR Prevention: it needs a Random Selection by size among vehicles and a target choice the To Hit does not have (design 2, F9; ruling R60.21)."),
    "A7.307": ("partly", "Pass 65", "ScenarioA1FireCalculator.cs: NamedVehicleAttack, FlameKill, ChargeKill, Position; ScenarioA1FlameKill.cs: BasicTk, FlameResult, ChargeResult",
               tests(R + "TheFtMolColumnBurnsBelowEliminatesOnEqualAndLeavesTheCrewItsSpecificCollateralAttack", R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks", Y + "AFtAtACeTankMakesItsToKillDrAndAttacksItsCrew", Y + "AMolAtATankBesideASquadBurnsItAndAttacksTheSquadWithoutTheFourFp"),
               "a FT, a MOL, and a DC that name an AFV make their To Kill attack on it (task 60.3; rulings R60.15, R60.16), and R25.6's refusal of an AFV in positive TEM is lifted for that attack alone. Left: ATMM (pass 110); Passengers and Riders as Vulnerable PRC, Bailing Out, and an AFV in positive TEM under any other IFT attack (pass 65)."),
    "A9.223": ("built", "", "ScenarioA1FireEligibility.cs: FireLaneCancelledByCcReaction; GameProjector.cs: the CC reaction's Fire Lane cancellation (cancelsFireLanes)",
               R + "CcReactionFireCancelsItsMgsFireLaneAndMarksItsWeapons",
               "CC Reaction Fire by a lane's MG or its manning Infantry cancels the Fire Lane (task 50.5; ruling R60.20, the narrow reading), gated by `cancelsFireLanes` on the reaction's record so that an older reaction replays as it did. Nothing of the note's Missing is left."),
    "A9.6": ("partly", "Pass 110", "ScenarioA1MachineGunKill.cs: Gun, Outside, Bar; ScenarioA1ArmorCalculator.cs: Run (a MG as ordnance)",
             tests(R + "AMgsToKillNumberIsFourWithCaseDsRowOfTwentyFiveMmLessTheAf", R + "AMgBeyondTwelveHexesTakesTheSmallCaliberModifications", R + "ASwMgInItsTargetsHexTakesNoCaseE", Y + "ARussianHmgStunsTheGermanHalftrack"),
             "a MG fires at an AFV's armored Target Facing on the To Hit and AP To Kill Tables, as an ordnance shot with a named AFV (task 50.5; ruling R60.19); C4.2's modifications beyond 12 hexes, and no Case E for a SW MG in its target's hex (the second referee). Left: the AFPh and a MG's Residual FP after its To Kill attack (pass 110); an unarmored Target Facing, which no catalog AFV has."),
    "A9.61": ("partly", "Pass 110", "ScenarioA1MachineGunKill.cs: Gun, MainArmamentFirepower, Outside, Bar, Result; ScenarioA1OrdnanceEligibility.cs: MachineGunRefusal",
              tests(R + "AMgsToKillNumberIsFourWithCaseDsRowOfTwentyFiveMmLessTheAf", R + "AMgsToKillDrBurnsEliminatesStunsOrDoesNothingAndATwelveIsADud", R + "AMgScoresNoCriticalHitAndATurretHitMeetsTheTurretsAf", R + "AMgsToKillAttackIsRefusedInTheAfphAtAnUnarmoredVehicleAndBeyondNormalRange", R + "AMgsToKillAttackAtAConcealedAfvIsRefused", Y + "ARussianHmgStunsTheGermanHalftrack", G + "AMgIsOfferedAsOrdnanceWhenItsSideSeesAnEnemyAfvAndWaitsForItsName"),
              "built for a SW MG fired alone, and a vehicle's MA MG, at a named AFV within Normal Range in the PFPh or DFPh (task 50.5; rulings R60.19, R60.27): a To Hit DR with black numbers, a To Kill DR with Basic TK# 4, Case A, and Case D's 25mm row, less the AF; at most half burns, below eliminates, equal Stuns; no Critical Hit, Shock, or Immobilization. Left (pass 110): the AFPh, Bounding First Fire, a pinned possessor, a lone leader or hero, Intensive Fire, a directing leader, a concealed AFV, the MG's Residual FP; no catalog MG is 12.7mm or 15mm."),
    "A9.611": ("built", "", "ScenarioA1ArmorCalculator.cs: SpecificCollateral (a MG's attack at its IFT FP)",
               tests(R + "ACeCrewTakesTheMgsFpOnTheSameDrUnlessTheAttackStuns", Y + "AGermanLmgAtACeT34LeavesItStandingAndAttacksItsCrew"),
               "the Collateral Attack of a MG's To Kill attack is built: the CE crew takes the MG's FP on the same DR, read on the column at or below it, unless the attack Stuns (task 50.5; rulings R60.7, R60.25)."),
    "A9.74": ("partly", "Pass 105", "ScenarioA1GunDestruction.cs: Checks, Random, IsKia, IsK; ScenarioA1FireCalculator.cs: WeaponDestruction; LiveFire.cs: WithRandomDestruction; GamePlanner.Fire.cs: WeaponDestructionEvents",
              tests(R + "AKiaMakesTheVictimsWeaponsRollOnTheSameColumn", R + "NoDrOnAKOrCasualtyReductionNorWithoutThePassesFact", R + "ARecordNamesNoWeaponItsTargetsHoldAndWritesNoNullMembers", Y + "AKiaOnASquadHoldingALmgDestroysTheLmgOnItsDr", Y + "AnOlderRecordOfAKiaOnASquadHoldingALmgReplaysAsItWas", G + "AKiaOnASquadHoldingALmgShowsTheLmgsFateInItsRecord"),
              "every Final KIA of an IFT attack makes each SW and Gun its victims held roll a dr on the same column: a KIA destroys it, a K malfunctions it, or eliminates it if it was malfunctioned or has an X# (task 60.5; rulings R60.3, R60.17). Applied only where the record carries `randomDestruction`; the weapons held are read from the state, never recorded. Left: an OVR's KIA, which makes no dr (pass 105); unpossessed weapons under Indirect Fire and an OVR, and the OVR's end-of-MPh check."),
    "A9.8": ("partly", "Pass 110", "ScenarioA1GunDestruction.cs: Random (a dm SW is a SW)", R + "AKiaMakesTheVictimsWeaponsRollOnTheSameColumn",
             "a dm SW rolls for Random SW Destruction as a SW (task 60.5). Left as the note says: starting dm; capture (pass 110, task 110.6); Scrounged weapons."),
    "A22.34": ("built", "", "ScenarioA1FireCalculator.cs: NamedVehicleAttack, FlameKill; ScenarioA1FlameKill.cs: BasicTk, FlameResult; GamePlanner.Fire.cs: WithNamedVehicle",
               tests(R + "AFlamethrowersTkNumberIsEightHalvedAtLongRangeFirstThenCeOtAndRear", R + "TheFtMolColumnBurnsBelowEliminatesOnEqualAndLeavesTheCrewItsSpecificCollateralAttack", R + "OnlyAFtMolOrDcNamesAnEnemyArmoredVehicleInTheTargetLocation", Y + "AFtAtACeTankMakesItsToKillDrAndAttacksItsCrew", G + "AFtNamesTheAfvInTheViewsWordsAndItsRecordShowsTheAttackOnIt"),
               "a fire request names an AFV in the target Location as the FT's target; Basic TK# 8, halved at Long Range before +1 CE and +2 OT, +1 through the rear, no AF, on the FT/MOL column, and the Specific Collateral Attack on its CE crew if it survives (task 60.3; ruling R60.15). Naming an AFV beside concealed or hidden units or a Dummy is refused. A vehicular FT has its own rows (pass 105)."),
    "A22.612": ("partly", "Pass 75", "ScenarioA1FireCalculator.cs: NamedVehicleAttack, FlameKill; ScenarioA1FlameKill.cs: BasicTk, FlameResult",
                tests(R + "AMolsOneDrIsItsToKillDrTheCrewsAttackWithFourMoreFpAndTheOthersAttack", Y + "AMolAtATankBesideASquadBurnsItAndAttacksTheSquadWithoutTheFourFp"),
                "a MOL names an AFV: Basic TK# 6, +2 OT or +1 CE, -2 against a moving vehicle, +1 or +2 for an elevation advantage, +1 through the rear, on the FT/MOL column; its one DR also attacks the CE crew at the Small Arms FP plus 4 and the other unarmored units at the Small Arms FP (task 60.3; ruling R60.15). Left: a MOL at an unarmored vehicle (refused), a leader's MOL, and a throw down a level (pass 75); its Flame (pass 185); a vehicle as the thrower (pass 105)."),
    "A23.5": ("built", "", "ScenarioA1FireCalculator.cs: Position, ChargeKill; ScenarioA1FlameKill.cs: BasicTk, ChargeResult, PositionResult, AerialAf; ScenarioA1FireEligibility.cs: DcPlacementVehicleRefusal; GamePlanner.DemolitionCharges.cs: PlanPlaceDc (PAATC), PlanDetonateDc, PlanThrowDc",
              tests(R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks", R + "ADcPoorlyPositionedAttacksTheCrewOnTheSecondDrAndOneOfTwelveOrMoreTheOthersAsAreaFire", Y + "APlacedDcAtATankAfterItsPaatcBurnsIt", Y + "APlacedDcReadsTheTargetFacingItWasPlacedThrough", G + "AThrownDcOffersTheAfvInTheViewsWords", G + "APlacedDcOffersItsDetonationAtTheAfvInTheViewsWords"),
              "a DC is Placed where an enemy AFV is once its placer passes a PAATC, or Thrown at one, and names it at detonation: the Position DR, then TK# 16 less the AF on the DC column (task 60.3; ruling R60.16). A Placed DC keeps the Target Facings it was Placed through (the second referee). A DC Placed where an unarmored or a friendly vehicle is stays refused, by a Rules member."),
    "B10.31": ("partly", "Pass 65", "ScenarioA1FireCalculator.cs: the critical-hit-tem:height-advantage reversal",
               R + "ACriticalHitUphillReversesHeightAdvantageOnlyWithThePassesFact",
               "a Direct Fire Critical Hit reverses Height Advantage to -1 where the shot carries `reversesHeightAdvantage` (task 60.2; rulings R60.3, R60.14). Left: the CE DRM beside it under Infantry fire (D5.31; pass 65); the Crest Line exception for a moving vehicle on the Vehicle Target Type, which no review checked."),
    "B13.3": ("partly", "Pass 150", "ScenarioA1FireCalculator.cs: AirBurstUnarmored; ScenarioA1AreaCalculator.cs: Run",
              tests(R + "AnOtHalftrackInWoodsIsHitAsUnarmoredAndACeTanksCrewTakesTheAirBurst", R + "AnEmplacedGunInWoodsTakesItsEmplacementWithTheAirBurst"),
              "Air Bursts in full against an unarmored, CE, or OT target in woods, with the Emplacement TEM, and kept on a Critical Hit; an OT AFV so hit is attacked as unarmored (task 60.2; ruling R60.12). Left: OBA (pass 150); an entrenchment, which does not exist (pass 115)."),
    "B23.32": ("partly", "Pass 140", "ScenarioA1AreaCalculator.cs: Run, LocationAttack (tem:levels-above), PrecheckAttacks; GamePlanner.Ordnance.cs: AreaLocations",
               tests(R + "ATwoLevelStoneBuildingIsHitAtEachLevelWithPlusOneBelow", Y + "AMortarsHitOnATwoLevelBuildingAttacksBothLevels"),
               "a mortar fires at a hex whose units are on several levels of a building: each Location is judged on its own To Hit, and one Effects DR attacks each Location hit with its building TEM and +1 for each level above it (task 60.2; rulings R60.11, R60.24). Left: Interior Building Hexes and rooftops, which the board data does not mark (pass 140); OBA."),
    "C1.55": ("partly", "Pass 65", "ScenarioA1AreaCalculator.cs: AfvAttack, Run; ScenarioA1OrdnanceEligibility.cs: AreaVehicle, AreaVehicleRefusal; GamePlanner.Ordnance.cs: AreaVehicleEvents",
              tests(R + "AMortarsHitOnATruckIsOnTheVehicleLine", R + "AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp", R + "AMortarsHitOnAnOtHalftrackTakesTheAfAndOtDrm", R + "ACriticalHitOnAnAfvIsAtDoubledFullFpWithNoTem", Y + "AMortarsHitOnATankBesideASquadShocksItOnTheIft"),
              "a mortar's hit on a vehicle is built (task 60.2; rulings R60.10, R60.23): an AFV on the IFT at half the HE FP with the AF and OT DRM, its hit Location from the Original IFT DR; an unarmored vehicle on the Vehicle line; the CE crew Collaterally. Left: more vehicles than the column's highest KIA# refuses the shot, and no Immobilization Task Check (pass 65); a vehicle in Bypass and a mortar's Defensive First Fire at a hex with a vehicle stay refused; OBA."),
    "C3.32": ("partly", "Pass 65", "ScenarioA1OrdnanceCalculator.cs: Run (targetHits), HitVehicles, OwnVehicleDrm, HeEquivalency; ScenarioA1OrdnanceEligibility.cs: LocationVehicle, LocationVehicleRefusal",
              tests(R + "AnHeHitAtASquadAndATruckAttacksBothOnOneDr", R + "ACeAfvBesideASquadIsImmuneAndItsCrewIsAttacked", R + "ApAtInfantryAttacksByItsHeEquivalency", Y + "AnHeHitAtASquadAndATruckAttacksBothOnOneDr", Y + "ACeAfvBesideASquadIsImmuneAndItsCrewIsAttacked"),
              "a Location with an unarmored vehicle is a target: each unit and vehicle is hit on its own Final TH#, and one Effects DR attacks them all, the vehicles on the Vehicle line; a CE AFV there is immune and its crew is attacked (task 60.1; ruling R60.8). AP and HEAT fire by HE Equivalency (task 60.4). Left: more vehicles than the column's highest KIA# (pass 65); a PF's or ATR's shot there with no vehicle named, and Defensive First Fire where a vehicle moves with Infantry, stay refused; a vehicle's own Bypass and shellhole TEM; an empty Location."),
    "C3.332": ("built", "", "ScenarioA1AreaCalculator.cs: Run, AfvAttack; ScenarioA1OrdnanceEligibility.cs: AreaVehicle",
               tests(R + "AMortarsHitOnATruckIsOnTheVehicleLine", R + "AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp", Y + "AMortarsHitOnATankBesideASquadShocksItOnTheIft"),
               "an Area Target Type hit on a vehicle is resolved as C1.55 has it, and its Vulnerable crew is attacked Collaterally (task 60.2; ruling R60.10). An OT AFV's crew under Air Bursts (D5.311) is pass 65's; no motorcycle exists."),
    "C3.4": ("partly", "Pass 110", "ScenarioA1OrdnanceCalculator.cs: Run (targetHits); GamePlanner.Ordnance.cs: OrdnanceSummary",
             tests(R + "ADrThatHitsOnlyTheKnownSquadLeavesTheConcealedOneAlone", Y + "ADrThatHitsOnlyTheKnownSquadLeavesTheConcealedOneConcealed", Y + "AHiddenUnitBesideAKnownSquadIsNeverNamedNorCountedInTheProposal"),
             "a Location of Known and concealed units is one To Hit DR judged unit by unit; a concealed unit hit loses its \"?\" and one missed keeps it, and hidden units are named nowhere (task 60.1; ruling R60.9; the reviews). `concealment-mixed` is gone. Left: units out of LOS or too close on the Infantry Target Type (pass 110)."),
    "C3.41": ("partly", "Pass 140", "ScenarioA1OrdnanceEligibility.cs: UnattendedGun, LocationVehicle; ScenarioA1OrdnanceCalculator.cs: Unattended; GamePlanner.Ordnance.cs: UnattendedGunAt",
              tests(R + "AnUnattendedGunIsATargetItsKiaDestroysItAndItsKMalfunctionsIt", Y + "AShotAtAnUnattendedGunDestroysItOnAKia"),
              "the Infantry Target Type attacks an unarmored vehicle (task 60.1) and an unattended Gun (task 60.5; ruling R60.18); a Location holding only AFVs stays the Vehicle Target Type's. Left: a building, a bridge, and an empty hex (pass 140); an unattended Gun on the Area Target Type (pass 110)."),
    "C3.7": ("partly", "Pass 100", "ScenarioA1OrdnanceCalculator.cs: Run (a LATW's Original 2)",
             tests(R + "AnOriginalTwoOnAPsksOwnTableIsACriticalHit", R + "AMgScoresNoCriticalHitAndATurretHitMeetsTheTurretsAf"),
             "a PF's or PSK's Original 2 on its own To Hit table is a Critical Hit outright (the first referee); a MG's To Kill attack scores no Critical Hit (task 50.5). Left: the HD clauses (pass 100), Deliberate Immobilization (pass 105), the FFE Critical Hit (pass 150)."),
    "C3.71": ("partly", "Pass 100", "ScenarioA1FireCalculator.cs: the critical-hit-tem:height-advantage reversal, VehicleEffects (an unarmored vehicle's own TEM reversed)",
              tests(R + "ACriticalHitUphillReversesHeightAdvantageOnlyWithThePassesFact", R + "ACriticalHitReversesAnUnarmoredVehiclesOwnTem"),
              "a Direct Fire Critical Hit reverses Height Advantage, where the shot carries `reversesHeightAdvantage` (task 60.2; ruling R60.14), and an unarmored vehicle's own positive TEM (the first referee); HE Equivalency is built (task 60.4). Left: hexside TEM reversal (pass 100); the C.7 column rule."),
    "C3.72": ("partly", "Pass 150", "ScenarioA1AreaCalculator.cs: AfvAttack (critical hit)", R + "ACriticalHitOnAnAfvIsAtDoubledFullFpWithNoTem",
              "an Area Target Type HE Critical Hit on an AFV is resolved as C1.55 has it with doubled full FP and no TEM, the AF and OT DRM kept (task 60.2; ruling R60.10). Left: the FFE Critical Hit (pass 150); Air Bursts against an OT AFV's crew (D5.311; pass 65)."),
    "C3.74": ("partly", "Pass 150", "ScenarioA1AreaCalculator.cs: Run (a Location, then a unit); ScenarioA1OrdnanceCalculator.cs: Run (units and unarmored vehicles in the pool)",
              tests(R + "ACriticalHitOnTheTruckBesideAGunLeavesTheGunToItsCrewsHit", R + "ATwoLevelStoneBuildingIsHitAtEachLevelWithPlusOneBelow"),
              "Random Selection takes units and unarmored vehicles into its pool, never an AFV (task 60.1), and on the Area Target Type picks a Location, then a unit (task 60.2); a Critical Hit on a truck no longer destroys the Gun beside it (the first referee). Left: OBA (pass 150); a tie between Locations goes to the lowest (backlog 60.2)."),
    "C3.9": (None, "Pass 105", "ScenarioA1AreaCalculator.cs: AfvAttack (the hit Location from the Original IFT DR)", R + "AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp",
             "Indirect Fire takes the hit Location from the Original IFT DR's dice (task 60.2; ruling R60.23). The upper superstructure of a non-turreted AFV is not built and moves to pass 105 (design 12.1; ruling R60.22)."),
    "C6.52": (None, "Pass 110", "", "",
              "nothing built: a transfer between the Area Target Type and the others moves to pass 110 (design 3)."),
    "C6.521": ("partly", "Pass 110", "ScenarioA1OrdnanceEligibility.cs: AreaAcquisition", tests(R + "AreaAcquisitionCoversAnotherLocationOfTheHex", R + "TheAreaAcquisitionIsHeldToTheStateForARecordThatCarriesIt"),
               "a mortar's Area Acquisition covers every Location of the hex in its LOS, decided in Rules and held to the state at replay (task 60.2; ruling R60.13; the reviews). Left: an empty hex as a target (C3.41's row), the loss when the mortar is carried away (pass 110)."),
    "C7.21": ("partly", "Pass 160", "ScenarioA1FireCalculator.cs: FlameKill, ChargeKill (case-a); ScenarioA1ArmorCalculator.cs: Run (a MG's Case A)",
              tests(R + "AFlamethrowersTkNumberIsEightHalvedAtLongRangeFirstThenCeOtAndRear", R + "AMgsToKillNumberIsFourWithCaseDsRowOfTwentyFiveMmLessTheAf"),
              "FT, MOL, DC, and a MG's To Kill attacks take +1 through the rear Target Facing (tasks 60.3, 50.5). Left: aircraft (pass 160)."),
    "C7.344": ("built", "", "ScenarioA1FlameKill.cs: BasicTk, FlameResult; ScenarioA1FireCalculator.cs: FlameKill",
               tests(R + "AFlamethrowersTkNumberIsEightHalvedAtLongRangeFirstThenCeOtAndRear", R + "TheFtMolColumnBurnsBelowEliminatesOnEqualAndLeavesTheCrewItsSpecificCollateralAttack", R + "AMolsOneDrIsItsToKillDrTheCrewsAttackWithFourMoreFpAndTheOthersAttack"),
               "a FT's and a MOL's To Kill resolution at a named AFV (task 60.3; ruling R60.15). A MOL at an unarmored vehicle is A22.612's (pass 75)."),
    "C7.345": ("built", "", "ScenarioA1AreaCalculator.cs: AfvAttack",
               tests(R + "AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp", Y + "AMortarsHitOnATankBesideASquadShocksItOnTheIft"),
               "a mortar's hit on an AFV is resolved on the IFT, never on a To Kill Table (task 60.2; rulings R60.10, R60.23). Its Immobilization Task Check is D5.5's (pass 65)."),
    "C7.346": ("partly", "Pass 105", "ScenarioA1FireCalculator.cs: Position, ChargeKill; ScenarioA1FlameKill.cs: PositionResult, AerialAf",
               tests(R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks", R + "ADcPoorlyPositionedAttacksTheCrewOnTheSecondDrAndOneOfTwelveOrMoreTheOthersAsAreaFire", Y + "APlacedDcAtATankAfterItsPaatcBurnsIt"),
               "the Position DR with its DRM whose facts exist: 5 or less the Aerial AF, 6 to 8 the AF, 9 to 11 the CE crew alone on a new DR, 12 or more the other unarmored units as Area Fire (task 60.3; ruling R60.16). Left: the IFT attack through an unarmored Target Facing (pass 105, D1.22); the Bypass -2, never reached (pass 100)."),
    "C7.7": ("partly", "Pass 120", "ScenarioA1FlameKill.cs: FlameResult, ChargeResult; ScenarioA1MachineGunKill.cs: Result; ScenarioA1AreaCalculator.cs: AfvAttack",
             tests(R + "TheFtMolColumnBurnsBelowEliminatesOnEqualAndLeavesTheCrewItsSpecificCollateralAttack", R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks", R + "AMgsToKillDrBurnsEliminatesStunsOrDoesNothingAndATwelveIsADud", R + "AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp"),
             "the DC, FT/MOL, MG, and Indirect Fire columns are built as Rules tables (tasks 60.2, 60.3, 50.5; ruling R60.2). Left: note A, since the catalog's CS# carries no color (backlog 60.1); the mine column (pass 120)."),
    "C8.31": ("built", "", "ScenarioA1HeEquivalency.cs: Firepower, AtrHasEquivalency, Weapon; ScenarioA1OrdnanceCalculator.cs: HeEquivalency, EquivalencyOutside; ScenarioA1OrdnanceMapRules.cs: HeatWallTem",
              tests(R + "ApBelow37mmIsOneFp", R + "Ap37mmAndUpIsTwoFp", R + "EveryApcrApdsAndAtrIsOneFp", R + "APfIsSixteenFp", R + "OtherOrdnanceHeatIsOneColumnLeftOfItsHeFp", R + "ApAtInfantryAttacksByItsHeEquivalency", R + "HeatAtInfantryNeedsAWallBuildingOrRubbleTem", R + "AGunWithNoHeHasNoHeatToFireAtInfantry", Y + "ASixPoundersApAtASquadAttacksByItsHeEquivalency", Y + "APskAtASquadInTheOpenIsRefused"),
              "HE Equivalency is built as a Rules table (task 60.4; rulings R60.2, R60.4, R60.5): AP, APCR, APDS, HEAT, and a 20L ATR fire at an unarmored target at their equivalency FP by the HE path, with no Residual FP; HEAT only at a target in a building, rubble, or behind a wall, and not from a Gun with no HE. A non-20mm ATR gives no Specific Collateral Attack. No catalog Gun lists H#[9], no catalog ATR is 20L, and no pillbox exists (backlog 60.1)."),
    "C8.42": ("built", "", "ScenarioA1GunDestruction.cs: Checks, Random; ScenarioA1FireCalculator.cs: WeaponDestruction", R + "ACanisterKiaDestroysASwAsAnHeKiaDoes",
              "a Canister KIA makes the SW its victims held roll for Random SW Destruction as an HE KIA does (task 60.5; ruling R60.17). The first sentence's rubble, Fire, and Wire are not made by any fire in the code, as the note says."),
    "C9.1": ("partly", "Pass 110", "ScenarioA1AreaCalculator.cs: Run, AfvAttack, LocationAttack; GamePlanner.Ordnance.cs: AreaLocations",
             tests(R + "AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp", R + "ATwoLevelStoneBuildingIsHitAtEachLevelWithPlusOneBelow", Y + "AMortarsHitOnATwoLevelBuildingAttacksBothLevels"),
             "a mortar fires at a hex holding a vehicle or units on several levels (task 60.2; rulings R60.10, R60.11, R60.24). Left: friendly units in the target hex, a vehicle in Bypass there, and Defensive First Fire with a vehicle in the hex stay refused (pass 110)."),
    "C11.2": ("partly", "Pass 110", "ScenarioA1FireCalculator.cs: the crew's emplacement and gunshield DRM (C11.5), the Air Burst with the Emplacement; ScenarioA1AreaCalculator.cs: Run (case-p, the Emplacement on an Area hit)",
              tests(R + "AnEmplacedGunInWoodsTakesItsEmplacementWithTheAirBurst", R + "AGunsGunshieldProtectsItsCrewAloneNotAVehicleBesideIt", Y + "AGunASquadMansInPlayKeepsItsEmplacementOnceACrewMansItAgain"),
              "the Emplacement on a mortar's Area hit and with Air Bursts; the +2 withheld while a squad mans the Gun; the Emplacement is the crew's alone, not a vehicle's beside it (task 60.5; rulings R60.12, R60.14, R60.26; the first referee). Left: no Emplacement on a paved road or a bridge, which needs a recorded map fact; the Area Target Type option; the owner's choice; OBA."),
    "C11.3": ("partly", "Pass 100", "ScenarioA1OrdnanceProjection.cs: LosesEmplacement; GameProjector.cs: KeepNonCrewManning; LiveOrdnance.cs: Emplaced (nonCrewManning)",
              tests(R + "TheBoundaryPredicatesAreRules", Y + "AGunSetUpMannedByASquadLosesItsEmplacementForTheGame", Y + "AGunASquadMansInPlayKeepsItsEmplacementOnceACrewMansItAgain"),
              "a Gun that starts the scenario manned by a non-crew unit loses its Emplacement for the game; a later manning does not (task 60.5; rulings R60.18, R60.26), decided in Rules and gated in the verifier by `randomDestruction` (backlog 60.2). Left: forfeiting Wall Advantage (pass 100), a voluntary non-Emplaced setup, RCL."),
    "C11.4": ("partly", "Pass 150", "ScenarioA1GunDestruction.cs: Fate; ScenarioA1OrdnanceCalculator.cs: Run (an unattended Gun, a Critical Hit's selection); ScenarioA1FireCalculator.cs: gunshield +1 against Indirect Fire, a DC's Gun",
              tests(R + "AnUnattendedGunIsATargetItsKiaDestroysItAndItsKMalfunctionsIt", R + "AGunshieldGivesPlusOneAgainstAMortarAndAKMalfunctionsTheGun", R + "ADcDestroysTheGunItsTargetsManOnAKiaAndMalfunctionsItOnAK", R + "ACriticalHitOnTheTruckBesideAGunLeavesTheGunToItsCrewsHit", Y + "AShotAtAnUnattendedGunDestroysItOnAKia"),
              "an unattended Gun is a target, the gunshield gives +1 against Indirect Fire, a DC destroys or malfunctions the Gun its target units man, and a Critical Hit destroys the Gun only when Random Selection picks its crew (task 60.5; rulings R60.14, R60.18; the first referee). Left: a DC at a manned Gun reads the Gun's fate from its crew's result, with no Random Selection among the targets (pass 110); OBA (pass 150)."),
    "C11.5": ("partly", "Pass 110", "ScenarioA1FireCalculator.cs: gunshield DRM (+1 indirect), the crew's own DRM", tests(R + "AGunshieldGivesPlusOneAgainstAMortarAndAKMalfunctionsTheGun", R + "AGunsGunshieldProtectsItsCrewAloneNotAVehicleBesideIt"),
              "+1 against Indirect Fire (task 60.2; ruling R60.14); the gunshield protects the Good Order manning crew alone, never a vehicle of its Location (the first referee). Left: the target's own choice between gunshield and TEM."),
    "C11.51": ("partly", "Pass 75", "ScenarioA1GunDestruction.cs: Fate, Random; ScenarioA1OrdnanceCalculator.cs: Run (HEAT at a Gun)",
               tests(R + "HeatsKAtAGunMalfunctionsIt", R + "TheGunDestructionTableReadsEachColumn"),
               "HEAT at a Gun uses the HE hit's mechanics, and a FT, a MOL, a MG, or small arms destroy a Gun only through A9.74 (tasks 60.4, 60.5; ruling R60.18). Left: a MOL in a FG (pass 75)."),
    "C11.52": ("built", "", "ScenarioA1HeEquivalency.cs: Firepower; ScenarioA1GunDestruction.cs: Fate (armorPiercing); ScenarioA1OrdnanceCalculator.cs: AtrAtGun",
               tests(R + "AnApKAtAGunEliminatesTheGun", R + "AnAtrAtAGunAttacksOnlyTheGunsCrewAtOneFp", Y + "AnAtrAtAGunAttacksOnlyItsCrewAtOneFp"),
               "AP, APCR, APDS, and an ATR fire at a Gun at their HE Equivalency FP, and a K eliminates the Gun (C11.6 note 5); a MG's or ATR's small-arms fire destroys a Gun through A9.74 (tasks 60.4, 60.5; rulings R60.6, R60.17)."),
    "C11.6": ("partly", "Pass 150", "ScenarioA1GunDestruction.cs: Fate, Checks, Random",
              tests(R + "TheGunDestructionTableReadsEachColumn", R + "ADcDestroysTheGunItsTargetsManOnAKiaAndMalfunctionsItOnAK"),
              "the MG/IFE/Small Arms/FT/MOL and DC columns and note 5 are built as a Rules table (task 60.5; rulings R60.2, R60.17). Left: the OVR's dr (pass 105, backlog 60.2); Bomb (pass 160) and OBA (pass 150); note 3."),
    "C13.23": ("built", "", "ScenarioA1OrdnanceCalculator.cs: AtrAtGun; ScenarioA1HeEquivalency.cs: Firepower",
               tests(R + "AnAtrAtAGunAttacksOnlyTheGunsCrewAtOneFp", Y + "AnAtrAtAGunAttacksOnlyItsCrewAtOneFp"),
               "an ATR fires at a Gun on the Infantry Target Type at 1 FP, attacking only the Gun and its manning crew (task 60.4; ruling R60.6)."),
    "C13.31": ("partly", "Pass 110", "ScenarioA1OrdnanceCalculator.cs: PanzerfaustCheck (not-afv, aug-sept-1943), Run (one unit by Random Selection)",
               tests(R + "APfAtASquadInABuildingTakesThePlusOneAndHitsOneUnitByRandomSelection", Y + "APfAtASquadInABuildingTakesThePlusOneAndSixteenFp"),
               "a PF fires at Personnel at 16 FP by HE Equivalency: +1 at a target that is not an AFV, one unit affected by Random Selection, an Original 12 Casualty-Reduces its firer (task 60.4; ruling R60.5). Left: the August and September 1943 +1, in the calculator and unreachable before October 1943; the firer's choice of a Known unit manning a Gun or SW (pass 110)."),
    "C13.9": ("built", "", "ScenarioA1HeEquivalency.cs: Firepower; ScenarioA1OrdnanceCalculator.cs: Run (a LATW on the Infantry Target Type)",
              tests(R + "APskAtInfantryInTheOpenIsRefusedAndInABuildingHitsEveryUnit", Y + "APskAtASquadInABuildingAttacksEveryUnitAtTwelveFp", Y + "APskAtASquadInTheOpenIsRefused"),
              "a PF's and a PSK's HEAT at Personnel by HE Equivalency (16 and 12 FP) at a target in a building, rubble, or behind a wall (task 60.4; rulings R60.4, R60.5)."),
    "D.8A": ("partly", "Pass 65", "ScenarioA1ArmorCalculator.cs: SpecificCollateral; ScenarioA1FireCalculator.cs: CrewOutcome, CrewChecks",
             tests(R + "ASpecificCollateralAttackFollowsAMissedTkDrAtACeTank", R + "ASpecificCollateralAttackByApTakesItsHeEquivalency", R + "NoSpecificCollateralAttackAtABuTankOrInAnOlderRecord", R + "NoSpecificCollateralAttackOnADudOrAKill", R + "NoSpecificCollateralAttackAfterAShock", Y + "AHitThatLeavesACeTankStandingAttacksItsCrew", Y + "AnOlderRecordOfAHitOnACeTankReplaysAsItWas"),
             "the Specific Collateral Attack is built after an ordnance hit, a FT, a MOL, a DC, and a MG's To Kill attack that leave an AFV standing: its CE crew takes the attack's IFT FP on the Original TK DR with the CE DRM alone; none after a Dud, a Shock, or a Stun the hit causes (task 60.1; rulings R60.3, R60.7, R60.25). Left: Passengers and Riders, an unarmored vehicle's Vulnerable PRC, and an Abandonment on a failed Immobilization Task Check (pass 65)."),
    "D2.41": ("built", "", "ScenarioA1FireCalculator.cs: Position (+2 for a Non-Stopped or Motion target), FlameKill (-2 for a moving vehicle)",
              tests(R + "AMolsOneDrIsItsToKillDrTheCrewsAttackWithFourMoreFpAndTheOthersAttack", R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks"),
              "the DC and MOL attacks on a moving vehicle take their DRM (task 60.3; rulings R60.15, R60.16)."),
    "D3.2": ("partly", "Pass 105", "GamePlanner.Fire.cs: TargetFacingsFrom; ScenarioA1ArmorCalculator.cs: Run (a SW MG in its target's hex)",
             tests(R + "ASwMgInItsTargetsHexTakesNoCaseE", Y + "APlacedDcReadsTheTargetFacingItWasPlacedThrough"),
             "a FT, MOL, or DC from the AFV's own hex attacks through the rear, otherwise through the hexside crossed, and a Placed DC keeps the facings it was Placed through; a SW MG in its target's hex reads the colored dr facing (tasks 60.3, 50.5). Left: Defensive First Fire at a vehicle entering the hex (C5.51; pass 105)."),
    "D3.54": ("built", "", "ScenarioA1MachineGunKill.cs: VehicleWeaponRefusal, MainArmamentFirepower; GamePlanner.Vehicles.cs: HasMainArmamentMg",
              tests(R + "AVehiclesMaMgMakesTheAttackAndItsBmgMayNot", Y + "AHalftracksAamgFiresAtTheT34AsOrdnance"),
              "a vehicle's MA that is a MG makes a MG's To Kill attack, and its other MGs may not (task 50.5; ruling R60.19). The Bren Carrier's plain `mg` MA is read; it prints no FP and fires at the British LMG's 2 FP (backlog 60.1)."),
    "D7.21": ("partly", "Pass 105", "ScenarioA1VehicleCloseCombatRules.cs: ReactionWeaponConditions; ScenarioA1OrdnanceEligibility.cs: CcCounterRefusal",
              R + "CcReactionFireCancelsItsMgsFireLaneAndMarksItsWeapons",
              "the CC counter of CC Reaction Fire marks the reacting unit's SW and Guns (`asl:cc-reaction`), and they make no Non-CC Reaction Fire at the vehicle in its Location (task 50.5; ruling R60.20). Left: the Unarmed bar (pass 105, with task 105.4); Cavalry."),
}

# Rows of other passes that pass 60's code touched: (status or None, text added to Where, text added to Test, the sentence added to the note)
ADD = {
    "A7.308": (None, "ScenarioA1OrdnanceCalculator.cs: HitVehicles; ScenarioA1FireReference.cs: HighestKia", R + "AnHeHitAtASquadAndATruckAttacksBothOnOneDr",
               "an Infantry or Area Target Type hit attacks the unarmored vehicles it hit on the Vehicle line on the units' Effects DR (tasks 60.1, 60.2; ruling R60.8). More vehicles in the Location than the column's highest KIA# is refused, since their Random Selection is not built (`vehicles-beyond-kia`)."),
    "A19.32": (None, "ScenarioA1ArmorCalculator.cs: Run (a MG fired as ordnance)", "",
               "a MG fired as ordnance at an AFV in Inexperienced hands has its B# lowered by one (design 12.1); its IFT fire keeps the printed B#, as the note says."),
    "A23.3": (None, "ScenarioA1FireEligibility.cs: DcPlacementVehicleRefusal; GamePlanner.DemolitionCharges.cs: PlanPlaceDc (PAATC)", Y + "APlacedDcAtATankAfterItsPaatcBurnsIt",
              "a DC is Placed where an enemy AFV is once its placer passes a PAATC; a failed one pins the placer (task 60.3; ruling R60.16). An unarmored or a friendly vehicle there still refuses the Placement, by a Rules member."),
    "A23.6": (None, "GamePlanner.DemolitionCharges.cs: PlanThrowDc (vehicle)", G + "AThrownDcOffersTheAfvInTheViewsWords",
              "a DC is Thrown at a named AFV in the target Location, with +2 on its Position DR (task 60.3; ruling R60.16)."),
    "C3.33": (None, "ScenarioA1AreaCalculator.cs: Run (the Locations out of the firer's LOS)", R + "ATwoLevelStoneBuildingIsHitAtEachLevelWithPlusOneBelow",
              "a mortar fires at a hex holding a vehicle, a manned Gun, or units in several Locations, and its hit attacks the units out of its LOS when it hit the hardest target in its LOS (task 60.2; rulings R60.10, R60.11, R60.24). Friendly units in the hex stay refused. The note's other Guns, SMOKE, and WP stay with pass 85."),
    "C4.2": (None, "ScenarioA1ArmorCalculator.cs: Run (a MG's modifications)", R + "AMgBeyondTwelveHexesTakesTheSmallCaliberModifications",
             "the \"including MG\" clause is reached: a MG's To Hit DR beyond 12 hexes takes the row's modifications (task 50.5; the second referee)."),
    "C5.5": (None, "ScenarioA1ArmorCalculator.cs: Run (no Case E for a SW MG)", R + "ASwMgInItsTargetsHexTakesNoCaseE",
             "a SW MG at an AFV in its own hex takes no Case E, which is a Gun's (task 50.5; the second referee)."),
    "C7.12": ("partly", "ScenarioA1FlameKill.cs: AerialAf", R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks",
              "a DC's Position DR of 5 or less meets the Aerial AF, read from C7.11's row of p. 190 over the worst AF of hull and turret (task 60.3; ruling R60.16); where the row prints none the lowest printed hull AF stands (backlog 60.1). Left: aircraft and Underbelly hits."),
    "C7.5": (None, "ScenarioA1AreaCalculator.cs: AfvAttack; ScenarioA1FlameKill.cs: ChargeResult", R + "ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks",
             "a mortar's hull hit at the K/# DR or one above, and a DC's result, immobilize (tasks 60.2, 60.3); the FT/MOL column has no Immobilization. No Immobilization Task Check follows a mortar's (backlog 60.1). Left: the HD exception."),
    "C7.6": (None, "ScenarioA1FlameKill.cs: FlameResult", R + "TheFtMolColumnBurnsBelowEliminatesOnEqualAndLeavesTheCrewItsSpecificCollateralAttack",
             "the FT and MOL sentence applies: a FT or MOL To Kill DR below the Final TK# burns the AFV (task 60.3)."),
    "D.8": (None, "ScenarioA1ArmorCalculator.cs: SpecificCollateral", R + "ASpecificCollateralAttackFollowsAMissedTkDrAtACeTank",
            "the Specific Collateral Attack is built after an ordnance hit, a MG's To Kill attack, a FT, a MOL, and a DC (D.8A's row). Left: mines, Passengers and Riders, the table's other rows."),
    "D.8B": (None, "ScenarioA1OrdnanceCalculator.cs: Run (a CE AFV's crew in a mixed Location); ScenarioA1AreaCalculator.cs: Run", R + "ACeAfvBesideASquadIsImmuneAndItsCrewIsAttacked",
             "Infantry and Area Target Type hits on a Location holding a vehicle attack a CE AFV's crew Collaterally on the same DR (tasks 60.1, 60.2; rulings R60.8, R60.10). Left: Passengers and Riders, OBA."),
    "D3.53": (None, "", "",
              "a vehicle's MA MG makes no To Kill attack in the AFPh, unhalved or not: it is refused (task 50.5; backlog 60.2)."),
    "D5.311": ("partly", "ScenarioA1FireCalculator.cs: AirBurstUnarmored", R + "AnOtHalftrackInWoodsIsHitAsUnarmoredAndACeTanksCrewTakesTheAirBurst",
               "an OT AFV hit by Air Bursts in woods is attacked as unarmored, on the Vehicle line (task 60.2; ruling R60.12). Left: its crew, which breaks and routs like Infantry and is not attacked today; fire through an unarmored Target Facing; elevation; Vulnerable Passengers."),
    "D5.34": (None, "ScenarioA1MachineGunKill.cs: Result (stun)", R + "AMgsToKillDrBurnsEliminatesStunsOrDoesNothingAndATwelveIsADud",
              "a MG's To Kill DR equal to the Final TK# Stuns the AFV, even if BU, and buttons it up at once, so no Collateral Attack follows (task 50.5; rulings R60.19, R60.25). Left: Stun by a Sniper and by Falling Rubble; a 1MT AFV's Recall on a Stun."),
    "D9.3": (None, "GamePlanner.Ordnance.cs: the AFV a mortar's target Infantry shelter beside (afvCover)", Y + "AMortarsHitOnATankBesideASquadShocksItOnTheIft",
             "a mortar's target Infantry keep the +1 of a friendly AFV or wreck beside them (task 60.2; ruling R60.10), from a map fact absent in older records."),
    "C13.24": (None, "ScenarioA1HeEquivalency.cs: AtrHasEquivalency", R + "ANonTwentyMmAtrMakesNoCollateralAttack",
               "a 20L ATR fires at Personnel on the Infantry Target Type by HE Equivalency, in the calculator (no catalog ATR is 20L); a non-20mm ATR gives no Specific Collateral Attack (task 60.4; ruling R60.4; the first referee)."),
}


def cells(line):
    return [cell.strip() for cell in line.split("|")[1:-1]]


def rows():
    chapter = None
    for index, line in enumerate(lines):
        if line.startswith("### Chapter "):
            chapter = line[len("### Chapter "):][0]
        if chapter and line.startswith("| ") and not line.startswith("| §") and not line.startswith("|---"):
            c = cells(line)
            if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
                yield index, chapter, c


def counts():
    by_chapter = collections.OrderedDict((letter, collections.Counter()) for letter in "ABCDE")
    by_pass = collections.Counter()
    for _, chapter, c in rows():
        by_chapter[chapter][c[3]] += 1
        if c[6]:
            by_pass[c[6]] += 1
    return by_chapter, by_pass


index_of = {c[0]: (index, c) for index, _, c in rows()}
waiting = sorted(rule for rule, (_, c) in index_of.items() if c[6] == "Pass 60")
if "--check" in sys.argv:
    print(len(waiting), "rows wait on pass 60;", "all named" if waiting == sorted(SET) else "not all named, or already written")
    print("missing:", sorted(set(waiting) - set(SET)), "extra:", sorted(set(SET) - set(waiting)))
    sys.exit(0)

assert waiting == sorted(SET), (sorted(set(waiting) - set(SET)), sorted(set(SET) - set(waiting)))
assert not set(SET) & set(ADD)
for rule in list(SET) + list(ADD):
    assert rule in index_of, rule
    assert "Pass 60 (2026-10-15)" not in index_of[rule][1][7], rule
    for cell in (SET.get(rule) or ADD.get(rule)):
        assert cell is None or ("|" not in cell and "\u2014" not in cell and "\n" not in cell), (rule, cell)
before_chapter, before_pass = counts()
before_status = {rule: index_of[rule][1][3] for rule in list(SET) + list(ADD)}
OLD60 = "| Pass 60: Armored combat III: other attacks on vehicles and Guns (proposed) | 49 |"
assert lines.count(OLD60) == 1


def merge(cell, more):
    if not more or more in cell:
        return cell
    return (cell + "; " if cell and cell != "none found" else "") + more


for rule, (status, target, where, test, sentence) in SET.items():
    index, c = index_of[rule]
    if status is not None:
        assert status in STATUSES
        c[3] = status
    c[6] = target
    assert (c[3] in ("built", "not applicable")) == (target == ""), rule
    c[4] = merge(c[4], where)
    c[5] = merge(c[5], test)
    c[7] = (c[7].rstrip() + " " if c[7] else "") + B + sentence
    lines[index] = "| " + " | ".join(c) + " |"
for rule, (status, where, test, sentence) in ADD.items():
    index, c = index_of[rule]
    if status is not None:
        assert status in STATUSES
        c[3] = status
    assert (c[3] in ("built", "not applicable")) == (c[6] == ""), rule
    c[4] = merge(c[4], where)
    c[5] = merge(c[5], test)
    c[7] = (c[7].rstrip() + " " if c[7] else "") + B + sentence
    lines[index] = "| " + " | ".join(c) + " |"

after_chapter, after_pass = counts()
assert after_pass.get("Pass 60", 0) == 0
built = sum(1 for rule in SET if index_of[rule][1][3] == "built")
moved = len(SET) - built
lines[lines.index(OLD60)] = f"| Pass 60: Armored combat III: other attacks on vehicles and Guns (built to the gate, 2026-10-15: {built} of its rows are built and {moved} moved to the pass that takes what is left) | 0 |"

print("rows changed:", len(SET), "of the pass and", len(ADD), "others")
transitions = collections.Counter((before_status[rule], index_of[rule][1][3]) for rule in SET)
print("the pass's rows, status before -> after:", dict(transitions))
transitions = collections.Counter((before_status[rule], index_of[rule][1][3]) for rule in ADD)
print("the other rows, status before -> after:", dict(transitions))
for letter in "ABCDE":
    print(letter, [before_chapter[letter][s] for s in STATUSES], "->", [after_chapter[letter][s] for s in STATUSES])
for key in sorted(set(before_pass) | set(after_pass)):
    if before_pass.get(key, 0) != after_pass.get(key, 0):
        print(key[:40], before_pass.get(key, 0), "->", after_pass.get(key, 0))
out = "\n".join(lines)
assert "\r" not in out and "\u2014" not in out and out.endswith("\n")
assert len(out.split("\n")) == len(text.split("\n"))
open(P, "w", encoding="utf-8", newline="").write(out)
