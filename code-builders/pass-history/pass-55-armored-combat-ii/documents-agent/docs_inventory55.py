"""Pass 55 documents: the rule inventory's rows that waited on pass 55, and the rows of other rules the pass built on. Each row keeps its note
and gains a sentence; its status, its code and test cells, and the pass that takes what is left are set here. The two count tables of section 2
are then rewritten from the rows, by the method of pass 35's docs_inventory_tables.py, with the two lines that script does not reach.

Usage: python docs_inventory55.py [--check]
    --check   says which rows wait on pass 55 and whether every one is named; writes nothing
"""
import collections
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
assert "\r" not in text
lines = text.split("\n")
B = "Pass 55 (2026-10-13): "
R = "ScenarioA1Pass55RulesTests."
Y = "BacklogPass55Tests."
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
STANDS = "Stands by its ruling"
DYO = "Deferred with Chapter H: a DYO chart, purchase, or dr; a card's SSR names the value"


def tests(*names):
    return "; ".join(names)


# rule -> (status or None to keep it, the pass for what is left or "", text added to Where, text added to Test, the sentence added to the note)
SET = {
    "A5.132": (None, "Pass 60", "", "",
               "not built; moved to pass 60 (task 60.6) with the To Hit's target choice (design 3; ruling R55.2)."),
    "B10.3": ("built", "", "ScenarioA1OrdnanceMapRules.cs: HeightAdvantage", R + "HeightAdvantageIsCaseQOnTheInfantryAndVehicleTargetTypes",
              "the note's fault is stale: pass 35 repaired the Open Ground test of the rout and of Interdiction (task 35.4; ruling R35.1). Height Advantage is now read for ordnance and for a vehicle's fire as well (B10.31), so nothing of the rule's two sentences is left. A wall's or hedge's TEM on an ordnance To Hit DR is Case Q's (C6.8), not this rule's."),
    "B10.31": ("partly", "Pass 60", "ScenarioA1OrdnanceMapRules.cs: HeightAdvantage; ScenarioA1OrdnanceCalculator.cs: HeightAdvantageApplies",
               tests(R + "HeightAdvantageIsCaseQOnTheInfantryAndVehicleTargetTypes", R + "HeightAdvantageBarsCaseJ4", R + "AVehiclesMgGivesItsTargetHeightAdvantage", Y + "AGunFiringUphillGivesItsTargetHeightAdvantageAsCaseQ", Y + "ATargetWithAnotherTemTakesNoHeightAdvantage"),
               "Height Advantage is Case Q +1 on an ordnance To Hit DR of the Infantry and Vehicle Target Types when no other Case Q is a positive TEM, bars Case J4, and is read on the IFT for a vehicle's MG fire (design 8.2). A target behind a wall or hedge the LOS crosses claims none (ruling R55.24). A CE crew's DRM is not added to it under a vehicle's MG fire or under Canister. The note's Interdiction is pass 35's (ruling R35.1). Left: a Critical Hit's reversal of it (C3.71; pass 60); the CE DRM beside it under Infantry fire (D5.31; pass 65); the Crest Line exception for a moving vehicle on the Vehicle Target Type, which no review checked."),
    "C2.2401": ("partly", "Pass 110",
                "ScenarioA1GunDuels.cs: Classify, Drm, Verdict, ReturnShotVoid, ShotRecordRefusal, VoidRecordRefusal; GamePlanner.GunDuels.cs: GunDuelDeclaration, PlanGunDuelAnswer, AddGunDuelReturnShot",
                tests(R + "TheGunDuelDrmIsTheFirerBasedAndAcquisitionDrmOfTheShotItself", R + "AGunDuelIsBarredByEachConditionOfTheRule", R + "TheRulesOwnGunDuelIsEqualAndIsNotOfferedInTheSlice", Y + "AShotDeclaredAtAMovingTankWithTheLowerGunDuelDrmWaitsForItsAnswer", Y + "InAGunDuelTheTankFiresFirstAndTheDeclaredShotFollows", Y + "AGunDuelsFirstShotThatBreaksTheCrewVoidsTheDeclaredShot"),
                "built as a slice (task 55.7; rulings R55.19, R55.20, R55.29): a Defensive First Fire ordnance shot declared at a moving vehicle whose Gun Duel DRM is lower waits for the ATTACKER's answer; in a duel the vehicle's MA fires first as Bounding First Fire and the declared shot follows unless the first eliminated, broke, stunned, or shocked its firer. Left: the case of equal DRM, where the Final DRs decide the order and no duel is offered today, though the rule gives one; fire that is not ordnance; the second duel with the ROF as a DRM; a LATW's declared shot; a Gun stacked with other Infantry; a Gyrostabilizer."),
    "C2.6": ("partly", "Pass 105", "ScenarioA1OrdnanceMapRules.cs: ElevationAllowed, ElevationRefusal, OwnHexLevelRefusal, CliffRefusal",
             tests(R + "AGunFiresAtAnotherLevelWhenTheRangeIsAtLeastTheElevationDifference", R + "AVehiclesMgFiresAtAnotherLevelByTheSameLimit", Y + "AShotAtARangeLessThanTheElevationDifferenceIsRefused", Y + "AGunFiresAtNoOtherLevelOfItsOwnHex"),
             "a Gun, a tank's MA, a mortar firing down, and a vehicle's BMG, CMG, and MA AAMG fire at another level when the range is at least the elevation difference; a mortar fires up at any range and a LATW is free of the limit (design 8.2; ruling R55.23). Another level of the firer's own hex stays refused, with the rule in the refusal. Left: the leave of the AA Guns, and of an AAMG (pass 105); a Gun in a building hex firing within its hex (pass 140); a vehicular FT, which has no counter."),
    "C3.22": (None, "Pass 105", "", "",
              "nothing built; moved to pass 105 with the rest of a vehicle's handling of its weapons and arcs (design 3; ruling R55.2)."),
    "C4.3": ("partly", "Pass 110", "ScenarioA1ArmorCalculator.cs: Run (apds)", R + "ApdsLessensTheBasicToHitNumberBeyondTwelveHexes",
             "APDS is an ammunition of its own on the Vehicle Target Type, its Basic TH# lessened as APCR's is (task 55.1). Left: the 28LL and 40LL, which have no counter."),
    "C5.33": (None, "Pass 110", "", "",
              "not built: the Gun Duel clause goes to pass 110 with the rest of the duel (design 3; ruling R55.20). The slice of C2.2401 that pass 55 built does not reach it."),
    "C5.35": ("partly", "Pass 105", "ScenarioA1OrdnanceCalculator.cs: MotionFirer",
              tests(R + "ADefendersVehicleInMotionFiresItsMaWithCaseCAndTheLowerDrDoubled", Y + "ADefendersTankGainsMotionTurnsItsVcaAndFiresAsAMotionFirer"),
              "a vehicle in Motion of the side that is not phasing fires its MA in the MPh and the DFPh with Case C alone and the lower dr doubled, with no Acquisition DRM and no Case L (task 55.3; ruling R55.14, a reading). Left: a Gyrostabilizer (pass 105); Guns en portee (pass 175)."),
    "C6.1": (None, "Pass 105", "", "",
             "nothing built; moved to pass 105 (design 3): the target's half for a vehicle that has paid only its Start MP, and VBM as a cause."),
    "C7.32": ("partly", "Pass 110", "ScenarioA1ArmorReference.cs: BasicTk (apds), CaseD (apds)", R + "ApdsReadsItsOwnListingsOfTheToKillTable",
              "APDS reads the D listings of the APCR/APDS table (57L 18, 76LL 25) and its own Case D row, and the AP table's number against an unarmored vehicle (C7.321). Left: the 28LL and 40LL, which have no counter."),
    "C8.11": ("built", "", "ScenarioA1ArmorCalculator.cs: ParseAmmunition; ScenarioA1Definitions.cs: Ammunitions",
              tests(R + "ApdsFollowsItsDepletionNumber", R + "ANumberHoldsFromItsYearAndItsMonth", Y + "ApdsIsNotThereBeforeJune1944"),
              "APDS (D) by the game's year and month from the counter's own listing (task 55.1; rulings R55.4, R55.7). A game with a year and no month does not have a number that starts in a month of that year (ruling R55.31). A listing's theater mark is kept and not read: every game is taken as the ETO."),
    "C8.12": ("built with a deviation", STANDS, "ScenarioA1ArmorCalculator.cs: ParseAmmunition",
              tests(R + "TheCatalogsCountersAgreeWithTheSupplyChartOrAreNamed", R + "ANumberHoldsFromItsYearAndItsMonth"),
              "a start month is part of the counter's notation (ruling R55.4). The chart is a test's data and not a rule the game reads (ruling R55.5): each catalog counter with A or D agrees with it or is named, and the PzKpfw IIIH and the T-34 M41 are named."),
    "C8.2": ("partly", DYO, "ScenarioA1Definitions.cs: EliteAmmunition; ScenarioA1ArmorCalculator.cs: Depletion",
             tests(R + "EliteForcesHaveOneMore", R + "AnEliteForceHasOneMoreOnEveryDepletionNumber", Y + "AnEliteForcesSevenIsItsLastApdsRound"),
             "the special rule `elite-ammunition:<side>` adds one to every Depletion Number of that side's ordnance, and the shot records it (task 55.1; ruling R55.6, which supersedes R7.6). No card carries it: an SS or Russian Guards formation is Elite only when its card's rule says so. Left: the DYO Majority Squad Type."),
    "C8.4": ("partly", "Pass 110",
             "ScenarioA1CanisterCalculator.cs: Aim, Attacked, Firepower, Multipliers, CaseA, AttackFact; GamePlanner.Canister.cs: CanisterAims, PlanFireCanister",
             tests(R + "ACoveredArcHasFiveCanisterAims", R + "CanisterHasTheFirepowerOfItsGunSizeAndItsHalvings", R + "TheCanisterDrIsTheDepletionMalfunctionAndRofCheck", Y + "CanisterAttacksEveryLocationOfItsAimOnOneDr", Y + "ADrAboveTheDepletionNumberAttacksNothingAndTheGunHasNotFired"),
             "Canister is an ordnance shot at one of the five aims of the CA, with no To Hit DR: one IFT DR for every Location of the cluster that is at the firer's elevation and in its LOS, with the FP of the Gun's size, each halving, each Location's own TEM and Hindrance, and Case A; that DR is the Depletion, malfunction, and ROF check (task 55.2; rulings R55.8, R55.9, R55.26, R55.30). Left: Canister as Defensive First Fire, which is refused in the MPh; the building option; an aim at another elevation; the SMOKE, LV, and NVR clause; Infantry that mans a Gun in the MPh; an AFV with Canister (pass 105), so that the Bounding First Fire and Motion halvings are in no game."),
    "C8.41": ("built", "", "ScenarioA1CanisterCalculator.cs: Attacked, Multipliers",
              tests(R + "TheAdjacentHexIsAttackedOnlyWithALocationOfTheCluster", Y + "AnAimWithNoLocationOfItsClusterToAttackIsRefusedThoughItsAdjacentHexIsThere"),
              "the hex adjacent to the firer on the LOS is attacked at half FP with no PBF, and both adjacent hexes at a quarter when the LOS runs along their hexside; only when a Location of the cluster is attacked (the referee's review). The firer's own hex is in no aim."),
    "C8.42": (None, "Pass 60", "", "",
              "nothing built. The first sentence needs no code as far as the inventory shows: rubble made by fire, a Fire set by fire, and Wire clearance are not built. Left: a Canister KIA's chance of destroying a SW (the referee's review; no KIA destroys a SW in the code; backlog 59.2)."),
    "C8.8": ("built", "", "ScenarioA1ArmorCalculator.cs: ParseAmmunition, Depletion; ScenarioA1OrdnanceCalculator.cs (C2.21, and the Infantry Target Type's step)",
             tests(R + "HeWithADepletionNumberFollowsItOnTheInfantryTargetType", R + "HeWithADepletionNumberFollowsItAtAVehicle", R + "ApWithADepletionNumberFollowsIt", Y + "TheSixPoundersHeIsFiredAtInfantryWhenItIsNamed", Y + "HeAboveItsDepletionNumberAsksForNoIftDrAndIsGone"),
             "HE and AP by a Depletion Number follow C8.9 on the Infantry and Vehicle Target Types (task 55.1; ruling R55.7): the 6-Pounder's HE7 and HE8. AP# has no counter and rests on a Rules test with a made-up Gun. Nothing is built on the Area Target Type, where no mortar of the catalog lists a number. What a Gun with no HE left does at Infantry is C8.31's (pass 60)."),
    "D1.81": (None, "Pass 105", "", "",
              "nothing built; moved to pass 105 (design 3): the hull's pivot for a shot, a hull RMG, a Fixed Mount."),
    "D2.11": ("partly", "Pass 105", "ScenarioA1MotionAttempts.cs: Hexspines, ChangeBar",
              tests(Y + "AVcaChangeInWoodsTakesABogCheckForEachHexspine", Y + "AVehicleThatBogsOnItsVcaChangeGainsNeitherMotionNorTheChange"),
              "a VCA is changed on a passed Motion Attempt, with a Bog Check for each hexspine in woods, a building, or rubble (task 55.3; ruling R55.13). Left: a VCA changed by firing outside the CA, or at the end of a fire phase."),
    "D2.18": (None, "Pass 105", "", "", "not built; moved to pass 105 (design 3)."),
    "D2.35": (None, "Pass 105", "", "", "nothing built; moved to pass 105 (design 3): the changed TCA in Bypass (D2.321)."),
    "D2.401": ("partly", "Pass 105",
               "ScenarioA1MotionAttempts.cs: Bar, Mover, HalfPointsInLos, Number, Succeeds, RecordRefusal; GamePlanner.MotionAttempts.cs: MotionAttemptOptions, PlanMotionAttempt; GameProjector.cs: AttemptMotion",
               tests(R + "AMobileDefendingVehicleAttemptsMotionOnceInTheEnemyMphsWindow", R + "TheNumberIsTheSumOfWhatTheMoverSpentInLos", R + "TheMoverIsOneThatBeganItsMoveOutOfTheVehiclesLos", R + "AHiddenVehicleAttemptsNoMotion", Y + "AFailedMotionAttemptHasNoPenaltyButBarsASecond", Y + "TheMechanicalReliabilityDrMayUndoAPassedAttempt", Y + "AConcealedVehicleLosesItsConcealmentWhenItsMotionAttemptPasses"),
               "the action asl.game.motion-attempt (task 55.3; rulings R55.11 to R55.13, R55.28, R55.32, R55.33): a DEFENDER's Mobile vehicle, once an enemy MPh, by a dr against the MF or MP that a mover which began out of its LOS has spent in it; on a pass the Motion counter and a free VCA and TCA, unless a Bog Check or the Mechanical Reliability DR undoes it. A Hidden vehicle attempts none, and a concealed one loses its \"?\" on a pass. Left: a stall; a facing or a turret for a vehicle in Bypass; the platoon's and the radioless AFV's drm (D14.211, D14.23); an expenditure in a Location whose LOS is undecided, which is not counted."),
    "D3.11": (None, "Pass 105", "", "", "nothing built; moved to pass 105 (design 3): the VCA changed for the shot."),
    "D3.12": ("partly", "Pass 105", "ScenarioA1MotionAttempts.cs: TurnsMainArmament",
              tests(R + "APassedAttemptFreelyChangesTheVcaAndTheTcaUnlessADrUndoesIt", Y + "AnInfantryMoversMfInLosAreSummedAndATcaAloneIsTurned"),
              "the TCA is changed freely on gaining Motion by a Motion Attempt, and only a TCA that changes loses the MA's Acquisition (task 55.3). Left: with a Start MP; in woods, a building, or rubble; at the end of a fire phase; the TCA turning with the hull."),
    "D3.31": ("partly", "Pass 105", "ScenarioA1CanisterCalculator.cs: Multipliers", R + "CanisterHasTheFirepowerOfItsGunSizeAndItsHalvings",
              "Canister's halving in Bounding First Fire and in Motion is in the calculator and its test and in no game: the Canister counter is a Gun (design 2, F2). Left: an AFV with Canister; the Gyrostabilized CMG."),
    "D3.4": ("partly", "Pass 65", "ScenarioA1ArmorLeaders.cs: IsGrade, SetupBar, Leadership, Modifier; GameTypes.cs: ArmorLeader",
             tests(R + "AnArmorLeaderIsOfOneOfFourGrades", Y + "ATankWithAnArmorLeaderFiresItsMaWithHisModifierWhichRevealsHim"),
             "an armed AFV carries an Armor Leader of 8-1, 9-1, 9-2, or 10-2 as a morale and a leadership, with no counter in the catalog (task 55.5; ruling R55.15). Left: the FG of a CE halftrack with its Passengers that an Armor or a Passenger leader directs (D6.64, D6.65)."),
    "D3.41": ("built", "", "ScenarioA1ArmorLeaders.cs: AssignmentRefusal, Shown, Recorded, Pending, ToldToTheEnemy",
              tests(Y + "ATankWithAnArmorLeaderFiresItsMaWithHisModifierWhichRevealsHim", Y + "ACrewChecksAtItsArmorLeadersMoraleAndOnlyThenIsHeRevealed", Y + "APendingChoiceHoldsNoMoraleOfAnUnrevealedArmorLeadersCrew"),
              "he is given to a vehicle at setup and held on it; the enemy's view shows him from the first record that carries his modifier or names his morale in a check (rulings R55.15, R55.27, R55.29). A card's setup plan cannot give one; a hand setup and a game file can (backlog 59.1)."),
    "D3.42": ("built", "", "ScenarioA1ArmorLeaders.cs: CrewMorale, MoraleSource",
              tests(R + "TheCrewChecksAtItsArmorLeadersMoraleLevel", Y + "ACrewChecksAtItsArmorLeadersMoraleAndOnlyThenIsHeRevealed"),
              "the crew checks at his Morale Level, without his modifier, in the two checks the code has: the Shock and Immobilization checks of a hit and the CE crew's MC (ruling R55.17). No separate DR is made for him."),
    "D3.43": ("partly", "Pass 65", "ScenarioA1ArmorLeaders.cs: Lost",
              tests(R + "AnArmorLeaderIsLostAndShownByRule", Y + "AWreckLosesItsArmorLeader"),
              "he affects his own vehicle alone and is lost with a Recall, a wreck or an elimination, and when the vehicle is Abandoned (ruling R55.17). Left: his return when that crew re-crews a vehicle; a captured vehicle."),
    "D3.44": ("partly", "Pass 105", "ScenarioA1ArmorLeaders.cs: Modifier, CloseCombatLeadership, BogRemovalDrm",
              tests(R + "AnArmorLeaderModifiesHisVehiclesMaToHitDr", R + "AnArmorLeaderModifiesTheIftDrOfAMaThatIsAMgAndNoOtherMg", R + "AnArmorLeaderModifiesHisVehiclesOvrDr", R + "AnArmorLeaderModifiesHisVehiclesOwnCcAttack", R + "AnArmorLeaderModifiesTheColoredDrOfABogRemoval"),
              "his modifier is on the MA's To Hit DR, the IFT DR of a MA that is a MG, the OVR DR, the vehicle's own CC attack, and the colored dr of Bog Removal, always applied (task 55.5; ruling R55.16). A BMG and a CMG take none; BU does not void it. Left: the HD Maneuver dr (D4.22); the halftrack's FG (pass 65); the leave to withhold it (A10.72). The last sentence, on Interdiction, was not read against the code."),
    "D3.45": ("built", "", "ScenarioA1ArmorLeaders.cs: Leadership, CrewMorale, SetupBar", Y + "AnInexperiencedCrewAddsOneAndIsPublic",
              "the condition asl:inexperienced-crew, given at setup: +1 on the rolls an Armor Leader modifies, a crew morale of 6, never lost, and not with an Armor Leader (task 55.5; ruling R55.18). Its +1 lowers Residual FP as a firer's positive DRM does (A8.26)."),
    "D3.51": (None, "Pass 105", "", "",
              "nothing built; moved to pass 105 (design 3): the Known-target condition, the TCA turning with a VCA change, the same for bow-mounted weapons."),
    "D3.52": ("partly", "Pass 105", "ScenarioA1CanisterCalculator.cs: CaseA",
              tests(R + "ACanisterAttackTakesItsLocationsTemAndHindranceAndItsCaseA", Y + "ACanisterShotThatTurnsTheGunAddsCaseAToEachAttack"),
              "a Canister shot that turns the Gun adds Case A to the IFT DR of each Location it attacks (task 55.2). Left: the BMG's NT penalty for a pivot."),
    "D7.16": ("partly", "Pass 65", "ScenarioA1ArmorLeaders.cs: Modifier",
              tests(R + "AnArmorLeaderModifiesHisVehiclesOvrDr", Y + "AnArmorLeaderModifiesHisVehiclesOvr"),
              "an Armor Leader's modifier is on his vehicle's OVR DR (task 55.5; ruling R55.16). Left: a Passenger leader."),
}

# Rows of other passes that the pass built on: rule -> (status or None, text added to Where, text added to Test, the sentence added to the note).
ADD = {
    "A7.55": (None, "", tests(R + "AnOrdnanceRecordOfThePhaseFormsNoFireGroup", Y + "ACanisterShotBindsNoFireGroupOfItsLocation"),
              "a Canister shot's fire records are ordnance's and bind no fire group of their Location; the clause is inside the Rules members (the Rules boundary review)."),
    "A8.26": (None, "", "", "an Inexperienced Crew's +1 lowers the Residual FP its vehicle leaves (design 11.1)."),
    "A12.141": (None, "GamePlanner.MotionAttempts.cs: PlanMotionAttempt",
                tests(Y + "AConcealedVehicleLosesItsConcealmentWhenItsMotionAttemptPasses", Y + "AConcealedVehicleKeepsItsConcealmentWhenItsMotionAttemptFails"),
                "a concealed vehicle that gains Motion by a Motion Attempt loses its \"?\" whether or not its CA changes (ruling R55.33); a failed attempt costs none (D2.401)."),
    "A12.32": (None, "ScenarioA1MotionAttempts.cs: Bar", tests(R + "AHiddenVehicleAttemptsNoMotion", Y + "AHiddenVehicleAttemptsNoMotionAndARecordOfOneIsRefused"),
               "a Hidden vehicle may not attempt Motion, and a record of such an attempt is refused (ruling R55.32)."),
    "B11.31": (None, "ScenarioA1OrdnanceMapRules.cs: CliffRefusal", R + "NoOrdnanceFiresThroughACliffHexsideToAnAdjacentLocation",
               "for ordnance, now that fire at another level is decided, the rule's own refusal is in Rules with its exceptions (a LATW other than a PIAT, and a mortar with a minimum range of 1); a Rules test only, since no board in the tests has a cliff. Whether a cliff hexside is stored on both hexes that share it was not checked (backlog 59.2)."),
    "B11.32": (None, "ScenarioA1OrdnanceMapRules.cs: CliffRefusal", R + "NoOrdnanceFiresThroughACliffHexsideToAnAdjacentLocation",
               "for ordnance, the rule's own refusal is in Rules with an ATR, a PIAT, and a mortar with a minimum range of 1 allowed; the AA weapons' leave is not built. A Rules test only."),
    "C2.21": (None, "", R + "AGunWhoseHeHasADepletionNumberNeverFiresItUndeclared",
              "a Gun whose HE has a Depletion Number never fires HE unnamed: at Infantry it must name HE (ruling R55.7)."),
    "C3.3": (None, "ScenarioA1OrdnancePackage.cs: Color (by the Gun and the year)", R + "TheToHitColorIsTheNationalitysAndTheYears",
             "the note's color fault is repaired for a Gun and a vehicle's MA: British black, American red before 1944 and black from 1944 (the chart of p. 695). A light mortar and a LATW keep red for every nationality but the German (ruling R55.22)."),
    "C3.71": (None, "", "", "an ordnance Critical Hit uphill does not reverse the Height Advantage TEM: the hit's IFT attack does not carry the fact (design 8.1)."),
    "C6.5": (None, "", tests(Y + "CanisterCostsTheGunItsAcquisition", Y + "APassedMotionAttemptThatKeepsTheTurretInPlaceKeepsTheAcquisition"),
             "a Canister shot costs the Gun its Acquisition; a passed Motion Attempt costs the MA's only when its CA changes."),
    "C6.8": (None, "ScenarioA1OrdnanceMapRules.cs: HeightAdvantage; ScenarioA1OrdnanceCalculator.cs: HeightAdvantageApplies", R + "HeightAdvantageIsCaseQOnTheInfantryAndVehicleTargetTypes",
             "Height Advantage is Case Q +1 on the Infantry and Vehicle Target Types when no other Case Q is a positive TEM. A wall or hedge the LOS crosses adds no TEM to a To Hit DR: the shot's map read finds it and no ordnance calculator reads it, which is older than the pass and left as found (design 8.1; ruling R55.24). Hull Down is still not built."),
    "C7.321": (None, "", "", "APDS against an unarmored vehicle uses the AP table's number, as APCR does (design 9.2)."),
    "C8.31": (None, "", R + "AnotherAmmunitionNamedAtInfantryIsHeEquivalencyAndNotAnUnnamedHe",
              "a Gun whose HE is by a Depletion Number and has none, and AP or APDS named at Infantry, are refused with this rule as the reason. Still refused."),
    "C8.9": (None, "ScenarioA1OrdnanceCalculator.cs (the Infantry Target Type's step); ScenarioA1CanisterCalculator.cs",
             tests(R + "ApdsFollowsItsDepletionNumber", R + "HeWithADepletionNumberFollowsItOnTheInfantryTargetType", Y + "ApdsAboveItsDepletionNumberWasNeverThereAndTheGunHasNotFired"),
             "the three outcomes hold for every ammunition with a Depletion Number: APDS on the Vehicle Target Type, HE and AP by number on the Infantry and Vehicle Target Types, and Canister on its IFT DR (ruling R55.7). A shot that had no round leaves no fire counter, the Intensive Fire counter included (ruling R55.25). The Depletion Number is carried in the resolution. Not on the Area Target Type."),
    "C8.91": (None, "ScenarioA1ArmorCalculator.cs: ParseAmmunition", tests(R + "ANumberHoldsFromItsYearAndItsMonth", R + "TheDepletionNumbersOfTheEarlierCountersDoNotMove"),
              "the notation also takes a start month, \"from that year on\", and a theater mark (ruling R55.4); a test pins every older counter's number for 1939 to 1945."),
    "C8.92": (None, "", "", "an Intensive Fire shot that finds no round leaves no Intensive Fire counter (ruling R55.25); the number itself is still untouched by Intensive Fire."),
    "D2.4": (None, "", "", "a DEFENDER's vehicle also gains Motion by a Motion Attempt in the enemy MPh (D2.401)."),
    "D2.42": (None, "ScenarioA1OrdnanceCalculator.cs: MotionFirer",
              tests(R + "ADefendersVehicleInMotionFiresItsMaWithCaseCAndTheLowerDrDoubled", R + "ADefendersVehicleInMotionFiresItsMgsHalved"),
              "a Motion firer of the side that is not phasing fires its MA with Case C and the lower dr doubled (ruling R55.14). Left: Passengers' and Riders' Motion Fire."),
    "D2.51": (None, "GamePlanner.MotionAttempts.cs: PlanMotionAttempt", Y + "TheMechanicalReliabilityDrMayUndoAPassedAttempt",
              "the Motion Attempt's trigger is built: a red-MP AFV that passes its attempt rolls, and a 12 immobilizes it with neither Motion nor the change. A stall is not built."),
    "D4.22": ("partly", "ScenarioA1OrdnanceMapRules.cs: HeightAdvantage", R + "HeightAdvantageIsCaseQOnTheInfantryAndVehicleTargetTypes",
              "the Height Advantage TEM for a vehicle target is Case Q +1 on the Vehicle Target Type, and a vehicle in Bypass may take it (design 8.2). Left: everything of the HD Maneuver."),
    "D5.31": (None, "", R + "TheCeDrmIsNotCumulativeWithHeightAdvantageUnderAVehiclesMgOrWithATemUnderCanister",
              "the CE DRM is not added to Height Advantage under a vehicle's MG fire, or to a TEM under Canister (the referee's review). Under Infantry fire the older line still adds both (backlog 59.2)."),
    "D5.341": (None, "ScenarioA1ArmorLeaders.cs: Lost", R + "AnArmorLeaderIsLostAndShownByRule", "an Armor Leader is lost with a Recall (ruling R55.17)."),
    "D8.3": (None, "ScenarioA1ArmorLeaders.cs: BogRemovalDrm",
             tests(R + "AnArmorLeaderModifiesTheColoredDrOfABogRemoval", Y + "AnArmorLeaderModifiesTheColoredDrOfHisVehiclesBogRemoval"),
             "an Armor Leader's modifier, and an Inexperienced Crew's +1, are on the colored dr (ruling R55.16). Left: the -1 for an assisting AFV; the halftrack's doubling."),
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
waiting = sorted(rule for rule, (_, c) in index_of.items() if c[6] == "Pass 55")
if "--check" in sys.argv:
    print(len(waiting), "rows wait on pass 55;", "all named" if waiting == sorted(SET) else "not all named, or already written")
    sys.exit(0)

# Every check before anything is changed.
assert waiting == sorted(SET), (sorted(set(waiting) - set(SET)), sorted(set(SET) - set(waiting)))
assert not set(SET) & set(ADD)
for rule in list(SET) + list(ADD):
    assert rule in index_of, rule
    assert "Pass 55 (2026-10-13)" not in index_of[rule][1][7], rule
    for cell in (SET.get(rule) or ADD.get(rule)):
        assert cell is None or ("|" not in cell and "\u2014" not in cell and "\n" not in cell), (rule, cell)
before_chapter, before_pass = counts()
OLD55 = "| Pass 55: Armored combat II (planned) | 35 |"
assert lines.count(OLD55) == 1
assert sum(1 for line in lines if line.startswith("| " + STANDS + " | ")) == 1
assert sum(1 for line in lines if line.startswith("| " + DYO + " | ")) == 1


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
    c[4] = merge(c[4], where)
    c[5] = merge(c[5], test)
    c[7] = (c[7].rstrip() + " " if c[7] else "") + B + sentence
    lines[index] = "| " + " | ".join(c) + " |"

after_chapter, after_pass = counts()
assert after_pass.get("Pass 55", 0) == 0

# The two count tables of section 2, from the rows.
total = collections.Counter()
for counter in after_chapter.values():
    total.update(counter)
rows_all = sum(total.values())
with_rule = rows_all - total["not applicable"]
need = sum(after_pass.values())
built = total["built"]
end = next(i for i, line in enumerate(lines) if line.startswith("## 3. Reading the tables"))
rewritten = 0
for i in range(end):
    line = lines[i]
    m = re.match(r"^\| ([A-E])\. ([^|]+) \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \|$", line)
    if m:
        counter = after_chapter[m.group(1)]
        lines[i] = f"| {m.group(1)}. {m.group(2)} | {sum(counter.values())} | " + " | ".join(str(counter[s]) for s in STATUSES) + " |"
        rewritten += 1
    elif line.startswith("| **A to E** |"):
        lines[i] = f"| **A to E** | **{rows_all}** | " + " | ".join(f"**{total[s]}**" for s in STATUSES) + " |"
        rewritten += 1
    elif line == OLD55:
        lines[i] = "| Pass 55: Armored combat II (built to the gate, 2026-10-13: 7 of its rows are built, 2 stand by a ruling or wait on Chapter H, and 26 moved to the pass that takes what is left) | 0 |"
        rewritten += 1
    elif line.startswith("| Pass 35: "):
        assert line.endswith("| 0 |") and after_pass.get("Pass 35", 0) == 0
    elif re.match(r"^\| (Pass \d+)(: [^|]+) \| \d+ \|$", line):
        m = re.match(r"^\| (Pass \d+)(: [^|]+) \| \d+ \|$", line)
        lines[i] = f"| {m.group(1)}{m.group(2)} | {after_pass[m.group(1)]} |"
        rewritten += 1
    elif line.startswith("| " + STANDS + " | "):
        lines[i] = f"| {STANDS} | {after_pass[STANDS]} |"
        rewritten += 1
    elif line.startswith("| " + DYO + " | "):
        lines[i] = f"| {DYO} | {after_pass[DYO]} |"
        rewritten += 1
    elif line.startswith("| **All** |"):
        lines[i] = f"| **All** | **{need}** |"
        rewritten += 1
    elif "rows that carry a rule" in line:
        new = re.sub(r"Of the \d+ rows that carry a rule, \d+ are built and \d+ need a pass or a ruling\.",
                     f"Of the {with_rule} rows that carry a rule, {built} are built and {need} need a pass or a ruling.", line)
        assert f"{built} are built and {need} need" in new
        lines[i] = new
        rewritten += 1
    elif re.match(r"^\*\*Where the \d+ rows go\.\*\*$", line):
        lines[i] = f"**Where the {need} rows go.**"
        rewritten += 1

print("rows changed:", len(SET), "of the pass and", len(ADD), "others; table lines rewritten:", rewritten)
for letter in "ABCDE":
    print(letter, [before_chapter[letter][s] for s in STATUSES], "->", [after_chapter[letter][s] for s in STATUSES])
moved = {key: (before_pass.get(key, 0), after_pass.get(key, 0)) for key in set(before_pass) | set(after_pass) if before_pass.get(key, 0) != after_pass.get(key, 0)}
for key, value in sorted(moved.items()):
    print(key[:40], value)
out = "\n".join(lines)
assert "\r" not in out and "\u2014" not in out and out.endswith("\n")
assert len(out.split("\n")) == len(text.split("\n"))
open(P, "w", encoding="utf-8", newline="").write(out)
