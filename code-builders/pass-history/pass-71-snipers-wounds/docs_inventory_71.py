"""Pass 71 documents: the inventory rows the pass built or touched. Every row is found exactly once and round-trips before any write. LF, UTF-8, no BOM."""
import io
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
D = "Pass 71 (2026-10-26): "
R = "ScenarioA1Pass71RulesTests."
B = "BacklogPass71Tests."
G = "PlayPagePass71Tests."


def tests(prefix, names):
    return "; ".join(prefix + n for n in names)


# rule: (status or None, ("replace"|"append", code) or None, tests to append or None, pass or None to keep, note to append)
ROWS = {
    "A14.01": ("built", ("append", "ScenarioA1Sniper.cs: PlacementsDue, PlacementDefender, PlacementDueBar, PlacementTurnBar, CardPlacementBar, NoCounter, NoCounterText; GamePlanner.SniperPlacement.cs: SniperPlacementsDue, SniperCounterMissing, PlanSniperPlacement; GamePlanner.CardSetup.cs: CardSetupIncomplete"),
               tests(R, ["EverySideWithASanOfTwoOrMorePlacesItsSniperCounterBeforePlayTheDefenderFirst", "AGameStartedWithoutASidesSniperCounterSaysItMakesNoSniperAttacks"]) + "; "
               + tests(B, ["ACardGameStartsPlayOnlyOnceEachSideHasPlacedItsSniperCounterTheDefenderFirst", "AGameWhoseSetupClosedBeforeThePassIsGivenNoSniperCounterAndSaysSo"]) + "; "
               + tests(G, ["TheSetupsSniperBlockOffersTheDefendersHexesFirstAndTheOtherSideSeesOnlyThatItPlaces", "AGameStartedWithoutTheRussianSniperCounterSaysOnceThatItMakesNoSniperAttacks"]),
               "", D + "a card game whose side has a SAN of 2 or more starts play only once that side's Sniper counter is placed, by the setup's `sniper` step, the DEFENDER first; a game whose setup closed before the pass is given no counter, and the page says once that the side makes no Sniper attacks (increment 4c; fix Rounds C and D; ruling R71.11). The note's \"Missing\" is closed."),
    "A14.1": ("partly", ("append", "ScenarioA1Sniper.cs: ActualSan, CounterRemoved, VerifySanReduced, FiringSide; GamePlanner.Snipers.cs: SniperTriggerWalk, ReadTriggers, KeepTriggers, SnipeInOrder; GameProjector.cs: ReduceSan; PlayStepMemo.cs: Snipers"),
              tests(R, ["TheActualSanIsThePrintedLessItsReductionsAndAtNightNeverBelowThePrinted", "ASanBelowTwoRemovesTheCounter", "AToHitACanisterAndAnHeHitsIftDrAreTheFiringSidesAndTheirNestedChecksTheUnits", "AVehicleCrewsCheckIsItsVehiclesSideAndEveryOtherCheckItsUnitsButAPrisoners", "OnlyARollOfTwoDiceCallsASniper"]) + "; "
              + tests(B, ["ResidualFpOnAMoveCallsTheSniperOnlyWhenItsDrEqualsTheEnemySan", "AnOrdnanceToHitDrCallsTheSniperOnlyWhenItEqualsTheEnemySan", "TwoTriggersOfOneMoveAreResolvedInTheOrderOfTheirDrs", "APaatcInTheAphCallsNoSniper", "ASnipersOwnLlmcCallsTheNextAttackBeforeItsSniperCheck", "AtSanTwoTheSniperCheckRemovesTheCounterAndALaterDrOfTwoCallsNothing"]),
              "Pass 115", D + "every Original DR A14.1 names calls the Sniper through one reader of a planned step's records: fire (with `collateralCheck` and `taskCheck`), ordnance (To Hit, the HE hit's IFT and checks, crew checks, Canister), the morale checks outside fire, the Bail Out, PAATC, and both OVR NTCs, each roll's maker named in Rules; a Sniper's own LLMC DRs chain, resolved before its Sniper Check (increment 4e; fix Rounds B and C; rulings R71.13, R71.25; R55.30 superseded). The SAN changes: `san-reduced`, the actual SAN decided in Rules, the counter removed below 2 (4b; ruling R71.9). The note's \"Missing\" is closed but for Entrenching. Deviation: a trigger by an early DR of one step resolves after the step's later DRs (backlog 74.1). Left: Entrenching (pass 115)."),
    "A14.2": ("built", ("replace", "GamePlanner.Snipers.cs: Snipe, SniperResume, SnipeInOrder, PlanSniperAnswer; ScenarioA1Sniper.cs: Attacks, Effective, Eligible, Placements, PlacementBar, ChoiceOffered, ChoiceText, TargetOption, StackMemberOption, PriorTargets, Candidates, Tied, Picked, ComesUnderDm; GamePlanner.SniperPlacement.cs: SniperPlacements, HandSniperBar"),
              tests(R, ["ASniperGoesWithinSixHexesOfSixEnemyHexesWhenItCanAndOfFiveWhenNoHexHasSix", "ASnipersPlacementCountsTheEnemyUnitsItsSideSeesAndNotTheHiddenOnes", "BeforeRandomSelectionTheSniperMayChooseTheEnemySniperACrewOrAnUnarmoredVehicleButNoConcealedStack", "TheSnipersChoicesAreWordedByTheirStepAndCiteTheRule"]) + "; "
              + tests(B, ["AnEffectiveDrAsksTheSniperPlayerToAttackOrToForfeitAndRepositionHisCounterInPublic", "TheSniperPlayerChoosesTheLocationOfAHexOfTwo", "TheSniperPlayerChoosesWhichTiedTargetTakesTheDrAndTheOtherTakesANewOne", "AGameMadeWithoutACardPlacesItsSniperByHandUnderTheSameCheck", "TwoTriggersOfOneSniperInOneRecordReachTheForfeitEachUnderItsOwnKey"]) + "; "
              + tests(G, ["TheSnipersChoiceIsTheRussianViewsToAnswerTheGermanViewReadsItAndTheHexesToRepositionToAreOneList"]),
              "", D + "the counter placed at setup by A14.2's tiers, from the placing side's view (increment 4c; ruling R71.11); the Sniper player's choices by a pending choice of kind `sniper`: attack or forfeit and reposition, the Location, the prior target, the first of tied targets, each asked only where more than one option exists, and nothing else may happen while it waits (4d; ruling R71.12, replacing R15.5's picks for new plans); the reposition hexes the Sniper side's alone (fix Round A; ruling R71.19); a pinned Sniper neither attacks nor repositions. The note's \"Missing\" is closed; an older record replays its recorded picks."),
    "A14.21": ("partly", ("replace", "GamePlanner.Snipers.cs: Snipe, SniperTargets; ScenarioA1Sniper.cs: TargetTem, TargetHexes, TemDecidesLocation, TargetLocations, Eligible, VehicleTargets"),
               tests(R, ["SmokeInATargetsHexCountsInTheTemComparison", "TheEnemySniperIsEliminatedOnAOnePinnedOnATwoAndCountsAsACandidateOfItsOwn", "EquidistantHexesOfTheLowestTemAreTheSnipersChoiceAndAnyOtherTieIsNot", "WhereTheTemComparedEquidistantHexesOnlyTheLowestTemLocationIsTheTarget"]) + "; "
               + tests(B, ["EquidistantHexesOfEqualTemAreTheSniperPlayersChoiceAndNoQuestionMarkIsRemovedWhileItWaits", "WhereTheTemComparedEquidistantHexesTheLowestTemLocationIsTheTargetUnasked", "SmokeCountsInItsOwnLocationAloneInTheTemComparison"]),
               "Pass 115", D + "the in-hex SMOKE DRM counts in each Location's TEM (increment 4a; fix Round B; ruling R71.7); the enemy Sniper counter counts as a target (4a; ruling R71.6); the equidistant hexes left after the lowest TEM are the Sniper player's choice, and where the TEM decided, its lowest Location is the target unasked (4d; fix Round B; ruling R71.12). R15.5's deviation is gone for new plans; an older record replays its recorded picks. The HA and Factory TEM are not in the table, so their NA holds. Left: a Fortification's TEM and the reveal of hidden Fortifications (pass 115)."),
    "A14.22": ("partly", ("replace", "GamePlanner.Snipers.cs: SniperTarget, SniperTargets; ScenarioA1Sniper.cs: Eligible, VehicleTargets, Manned, CrewImpaired, InConcealedStack, CountsAsConcealedVehicle, StackTargets; FireDefinition.PartiallyArmored"),
               tests(R, ["AnAfvWithNoVulnerablePrcIsNoTargetAndACeOneOffersItsCrewAndItsExposedPassengers", "APartiallyArmoredAfvsPrcAndCrewAreVulnerableEvenWhenBu", "AnUnarmoredVehicleIsATargetApartFromItsPrcButNotWhenEmpty", "WhoMayBeATargetAtAll", "TheCatalogsVehiclesReadTheirPartiallyArmoredTrait", "AVehicleIsMannedUnlessAbandonedOrItsCrewBrokenAndAStunnedRecalledOrShockedCrewSparesItsPassengers"]) + "; "
               + tests(B, ["AButtonedUpHalftracksPassengersAreNoTargetsSoTheSniperFindsTheNextHex"]),
               "Pass 135", D + "a unit inside a vehicle is a target only when Vulnerable (a PRC of a CE, Partially Armored, or unarmored vehicle; any Rider), the Inherent crew when the vehicle is CE, Partially Armored, or unarmored, and an unarmored vehicle with PRC or crew itself; an AFV with nothing Vulnerable is no target; a concealed vehicle's PRC and crew are part of its \"?\" (increment 4a; fix Rounds A and C; rulings R71.3, R71.20). Deviation: an empty unarmored vehicle is never eligible (ruling R71.4; backlog 74.1). Left: Interior Building Locations (pass 135); subterranean Locations (pass 145 and Chapter G)."),
    "A14.23": (None, ("replace", "GamePlanner.Snipers.cs: Snipe (a concealed stack, its real count); ScenarioA1Sniper.cs: Candidates, Tied, Picked, InConcealedStack, CountsAsConcealedVehicle, StackTargets, StackMemberOption, StackMemberOf, PublicResult"),
               tests(R, ["AConcealedVehiclesPrcCrewAndItselfAreItsStackAndOneWithNoEligibleTargetIsStillOneCandidate", "ATiedTargetBeneathAQuestionMarkIsAnsweredByItsPlaceAlone", "ATargetThatKeepsItsQuestionMarkHasOneNeutralPublicResult"]) + "; "
               + tests(B, ["RandomSelectionInAConcealedStackGivesItsTieToTheSniperPlayerAndTheOtherANewDr", "AConcealedButtonedUpHalftrackIsStillOneQuestionMarkCandidateAndIsAttackedToNoEffect"]),
               None, D + "a concealed stack is never offered before Random Selection (increment 4d); a concealed vehicle with its PRC and crew is one candidate whatever its state (fix Round A; ruling R71.20); Random Selection inside a concealed stack honors ties with the Sniper player's choice and new drs (fix Round B); a target that keeps its \"?\" has one neutral public result, `concealed-kept`, with an owner-only twin (fix Round A; ruling R71.22)."),
    "A14.3": ("built", ("replace", "GamePlanner.Snipers.cs: SnipeUnit, SnipeVehicle, SnipeSniper, Snipe (DM); ScenarioA1Sniper.cs: UnitEffect, Mortal, ReductionResult, CrewResult, CrewAttackedAsSmc, VehicleResult, SniperResult, VehicleConditions, WoundedConditions, BrokenConditions, PinnedConditions, KeepsConcealment, PublicResult, VerifySniperAttack, ComesUnderDm; ScenarioA1MoraleCheck.cs (the LLMC after a Sniper)"),
              tests(R, ["ACeCrewOfAFullyArmoredAfvIsRecalledOnAOneAndStunnedOnATwo", "AnUnprotectedInherentCrewBreaksOnAOneAndIsPinnedOnATwo", "ASmcTemporaryCrewOfAVehicleNotFullyArmoredIsAttackedAsTheSmcHeIs", "APinnedSnipersRecordIsRefused"]) + "; "
              + tests(B, ["ASnipersTwoStunsTheCeCrewOfATank", "ALeaderTheSniperEliminatesStillCausesTheLlmcOfHisLocation", "ASnipersOwnLlmcCallsTheNextAttackBeforeItsSniperCheck", "ASmcTemporaryCrewOfATruckIsEliminatedOrWoundedAsTheSmcHeIs"]) + "; "
              + tests(G, ["TheSniperLineNamesTheHexAndTheStunnedCrewsTank"]),
              "", D + "the results against the CE crew of a fully armored AFV (Recalled on 1, Stunned on 2, Recalled under Stun +1), the Inherent crew of an unarmored vehicle or Partially Armored AFV (broken, pinned), a SMC Temporary crew there as the SMC he is, an unarmored vehicle (immobilized on 1), and the enemy Sniper (eliminated, pinned) (increment 4a; fix Round B; rulings R71.5, R71.6, R71.26); a wound in the MPh after more than 3 MF pins (4f; ruling R71.14). The note's \"no LLMC or LLTC follows a leader's loss\" was stale since pass 70 (design section 5, item 5): the LLMC is made through the Morale Check member, and a Sniper's own LLMC DRs call further attacks, resolved before its Sniper Check (4e; fix Round B; ruling R71.25)."),
    "A14.31": ("built", ("replace", "GamePlanner.Snipers.cs: SnipeSniper, Snipe (a pinned Sniper neither attacks nor repositions); ScenarioA1Sniper.cs: SniperResult, VerifySniperAttack, ActualSan, CounterRemoved; GameProjector.cs: UnpinSnipers"),
               tests(R, ["TheEnemySniperIsEliminatedOnAOnePinnedOnATwoAndCountsAsACandidateOfItsOwn", "APinnedSnipersRecordIsRefused"]) + "; "
               + tests(B, ["TheEnemySniperIsPinnedUntilTheCloseCombatPhaseEnds", "ASnipersOneOnTheEnemySniperReducesItsSanAndAtTwoRemovesItsCounter", "APinnedSniperNeitherAttacksNorRepositions"]),
               "", D + "the enemy Sniper counter is a target: a dr 1 eliminates it and reduces its side's SAN, removing the counter below 2 (A14.4); a dr 2 pins it, and the projector clears its pin at the CCPh's end; a pinned Sniper neither attacks nor repositions, and a recorded attack by one is refused (increments 4a, 4b; fix Round B; rulings R71.6, R71.9). The note's \"nothing ... ever pins or attacks a Sniper\" is closed."),
    "A14.32": (None, ("replace", "GamePlanner.Snipers.cs: SniperTargets (the attacked side only); ScenarioA1Sniper.cs: Eligible"), None, None,
               D + "unchanged by the targets' rewrite (ruling R71.3): a Sniper still picks units of the attacked side only; still no test reaches a Melee Location."),
    "A14.33": ("built", ("replace", "GamePlanner.Snipers.cs: SnipeVehicle; ScenarioA1Sniper.cs: VehicleTargets, VehicleResult, VehicleConditions"),
               tests(R, ["AnUnarmoredVehicleIsATargetApartFromItsPrcButNotWhenEmpty", "AnUnarmoredVehicleIsImmobilizedOnlyOnAOneAndOnlyOnce"]) + "; "
               + tests(B, ["ATrucksPassengerCrewAndTheTruckItselfAreThreeCandidatesAndAOneImmobilizesTheTruck", "BeforeRandomSelectionTheSniperPlayerMayChooseAnUnarmoredVehicle"]) + "; "
               + tests(G, ["TheSnipersOptionsBeforeRandomSelectionNameTheHexFirstAndTellTheTruckFromItsCrew"]),
               "", D + "an unarmored vehicle with PRC or crew is a target apart from them, never by the same dr; a dr 1 immobilizes it, no effect if already immobilized (increment 4a; rulings R71.3, R71.5); the Sniper player may choose it before Random Selection (4d; ruling R71.12). An empty one is ineligible (ruling R71.4, the deviation recorded at A14.22)."),
    "A14.4": ("built with a deviation", ("replace", "GamePlanner.Snipers.cs: SniperCheckers, SniperCheckOffer, PlanSniperCheckAnswer, ReduceSan; ScenarioA1Sniper.cs: MayTakeSniperCheck, FiredOrMoved, SniperCheckDrm, SniperCheckResult, SniperCheckConditions, VerifySniperCheck, SniperCheckChoiceText, SniperCheckOffered, NoSniperCheckerText, ActualSan, CounterRemoved; GameProjector.cs: CheckSniper, ReduceSan; GameState.FiredOrMovedThisPlayerTurn"),
              tests(R, ["TheSniperCheckDrmFollowsTheExamplesThreeCases", "ASniperCheckCountsEachHeroAndEachCrewAndAHeroicLeaderGivesOneOrTheOther", "ASniperCheckFinalDrOfTwoEliminatesTheSniperAndAThreePinsIt", "OnlyUnpinnedNonTiArmedGoodOrderInfantryThatHasNeitherFiredNorMovedMayTakeTheSniperCheck", "TheSniperCheckIsOfferedWheneverAUnitMayTakeItOrAQuestionMarkStaysInTheLocation", "AUnitHasFiredOrMovedByAnyFireMarkerAnyMfAFireThisPhaseAGunShotOrAnEarlierPhase"]) + "; "
              + tests(B, ["AnEffectiveSniperAttackOffersTheSniperCheckAndDecliningItResolvesNothingMore", "TakingTheSniperCheckEliminatesTheSniperAndReducesItsSan", "ASniperCheckOfThreePinsTheSniper", "AtNightASniperCheckNeverTakesTheSanBelowThePrintedSoTheCounterStays", "TheSniperCheckIsOfferedWhileAQuestionMarkStaysThoughNoUnitThereMayTakeItAndTakingItRollsNothing"]) + "; "
              + tests(G, ["TheSniperCheckIsTheGermanSidesChoiceAndItsLinesNameTheHex"]),
              "", D + "after an effective attack the attacked side is offered a Sniper Check (kind `sniper-check`) whenever a unit there may take it or a \"?\" of that side stays; taking it commits every eligible unit to one DR, its DRM decided in Rules (the EX's three cases, heroes, crews and HS, a wounded leader one worse); 2 or less eliminates the Sniper and reduces the SAN (`san-reduced`), 3 pins it; the participants become TI and lose a \"?\" only to an unconcealed Good Order enemy seer; the DR calls no Sniper (increment 4b; fix Rounds A, B, C, and D; rulings R71.9, R71.10, R71.18, R71.21). The counter leaves when the SAN falls below 2 (\"reduced to 1\"). Deviation: a check by a subset of the eligible units is not offered (backlog 74.1)."),
    "A17.2": ("partly", ("append", "ScenarioA1Wounds.cs: PinnedByWound, CarriedManPp, Portage, MoverHalfMf, CarryBar, CarriedMoveBar, CarriedText, CarryFacts; FireTarget.WoundPins; GamePlanner.PassengerTargets.cs: WithWoundPins; GamePlanner.Movement.cs: PlanMove (carried); GameProjector.cs (movement-step carried, UNIT-STATE-029)"),
              tests(R, ["ASmcWoundedInTheMphAfterMoreThanThreeMfIsPinned", "ABrokenSmcIsNeverPinnedByAWound", "ASearchCasualtyCountsTheSearchsMfAmongHisForTheWoundPin", "AWoundedManIsCarriedOnlyInGoodOrderWithNoSwByAGoodOrderMmcOfHisStack", "ACarriersFivePpReduceItsMfByThePortageRulesAndTheManSpendsNone", "ACarriedWoundedLeaderStillGivesHisCarrierTheTwoMfBonus"]) + "; "
              + tests(B, ["ALeaderWoundedByDefensiveFirstFireAfterMoreThanThreeMfIsPinned", "ASniperWoundInTheMphAfterMoreThanThreeMfPins", "ABrokenLeaderWoundedAfterMoreThanThreeMfIsNotPinned", "ARecordedWoundPinTheStateDoesNotGiveIsRefused", "ASquadCarriesAWoundedLeaderWhoSpendsNoMfAndItsOwnMfFallByFivePp", "AWoundedLeaderWhoPortagesASwIsNotCarried", "ACarriedManWhoseCarrierStopsIsSetDownAndMovesNoFarther", "AStepRecordedForACarriedManWithoutHisCarrierIsRefused"]) + "; "
              + tests(G, ["TheMovementPanelOffersToCarryAWoundedManOfTheStackByHexAndTheMoveRecordsIt", "AFireAttacksWoundThatPinsIsSaidOnceWithItsRule"]),
              "Pass 165", D + "a SMC wounded in the MPh after spending more than 3 MF (3 1/2 counts) is pinned, unless broken (A7.8), in Fire through `woundPins` and with a Sniper's or a Search's wound (increment 4f; fix Rounds B and C; rulings R71.14, R71.27); a Good Order wounded man with no SW is carried by a Good Order MMC of his stack at 5 PP (`carried` on the move), spending no MF, set down when his carrier is not among a later step's movers (4g; fix Round B; rulings R71.16, R71.28). The note's \"4 MF on a conveyance\" was stale: built for vehicles (pass 40; ruling R65.15; design section 5, item 5). Left: the 4 MF on cavalry (pass 165), boats (pass 170), motorcycles and wagons (pass 175); the RtPh's wound pin, a PRC SMC's wound pin, and the carry in the APh (backlog 74)."),
    "A18.2": ("built", ("append", "ScenarioA1VehicleCloseCombat.cs: AfvDrmDeclared; VehicleCloseCombatFacts.AfvDrm; GamePlanner.VehicleCloseCombat.cs: PlanVehicleAttack (afvDrm)"),
              tests(R, ["LeaderCreationInCcAgainstAnAfvTakesMinusOne", "TheAfvsMinusOneIsDeclaredOnlyWithALeaderCreationAgainstAVehicleNotPrintedUnarmored"]) + "; BacklogPass70Tests.ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack; BacklogPass70Tests.ASquadsOriginalTwoAgainstATruckTakesNoAfvDrmForItsLeader",
              "", D + "the -1 for CC against an AFV is added to Leader Creation in vehicle CC when the vehicle is armored, through `afvDrm` on the vehicle CC facts (absent, as before); the per-column drm in Infantry CC was built (increment 4f; fix Round C; ruling R71.17). G.M.D. leaders stay refused under R27.1 until a card brings Chinese counters (backlog 74.1)."),
    # Rows of other rules the pass's code touched.
    "A12.14": (None, ("append", "GamePlanner.Snipers.cs: NearestUnconcealedLossSeer; ScenarioA1Sniper.cs: KeepsConcealment, PublicResult"),
               tests(B, ["AConcealedSquadPinnedByASniperKeepsItsQuestionMarkOnlyOutOfEveryGoodOrderEnemyLos", "OnlyAnUnconcealedEnemySeerCostsTheQuestionMarkInASniperAttack", "EquidistantHexesOfEqualTemAreTheSniperPlayersChoiceAndNoQuestionMarkIsRemovedWhileItWaits"]),
               None, D + "an effective Sniper attack costs a \"?\" only in the LOS of an unconcealed Good Order enemy ground unit, whatever the result, and a break, wound, Reduction, or elimination always (rulings R71.8, R71.21); where only concealed enemy units see it the \"?\" stays, a deviation in the target's favor, since the concealed viewer's option to force the loss is not built inside a Sniper attack (backlog 74.2); a kept \"?\" has one neutral public result (ruling R71.22). The Sniper exception is built: while a Sniper's choice waits no voluntary removal comes between the Random Location DR and the relocation (increment 4d; ruling R71.12), so \"the Sniper exception (backlog 72.1)\" is closed."),
    "A12.141": (None, ("append", "GamePlanner.Snipers.cs: PlanSniperCheckAnswer"), tests(B, ["ASniperCheckSeenOnlyByAConcealedEnemyKeepsItsQuestionMarkAndItsDiceAreItsOwners"]), None,
                D + "a Sniper Check costs its participants' \"?\" in the LOS of an unconcealed Good Order enemy ground unit within 16; seen only by concealed enemy units they keep it, the deviation recorded at A12.14 (increment 4b; fix Round A; rulings R71.10, R71.21)."),
    "A12.16": (None, None, None, None,
               D + "a Sniper attack's Random Selection inspects its Location: the Sniper side learns the unconcealed targets it chooses among, whatever A12.16 would withhold at a distance; a concealed stack stays one \"?\" (fix Round A; ruling R71.23)."),
    "A12.2": (None, None, tests(B, ["AConcealedTrucksPassengerCrewAndTruckAreOneQuestionMarkAndWhatOneLosesTheyAllLose"]), None,
              D + "a concealed vehicle's PRC and crew are part of its concealed stack for a Sniper attack: one \"?\" candidate, and a \"?\" lost by one is lost by all (fix Round A; ruling R71.20)."),
    "A12.3": (None, None, None, None,
              D + "the Sniper's reposition hexes, which its own hidden units shape, are shown to the Sniper side alone (fix Round A; ruling R71.19); a Sniper's placement counts no hidden enemy unit (ruling R71.11)."),
    "A15.21": (None, None, tests(R, ["ASniperCheckCountsEachHeroAndEachCrewAndAHeroicLeaderGivesOneOrTheOther"]), None,
               D + "in a Sniper Check a heroic leader gives his leadership or the hero's -1, never both (A14.4; increment 4b)."),
    "A17.1": (None, ("append", "GamePlanner.Fire.cs: AddSmcCrewSeverity"), None, None,
              D + "a SMC Temporary crew's Stun wound takes its Wound Severity dr (D5.343; ruling R71.15), and a SMC Temporary crew of a vehicle not fully armored is wounded by a Sniper's dr 2 as a SMC (ruling R71.26)."),
    "A17.11": (None, ("append", "ScenarioA1CapturedVehicleRules.cs: SmcCrewOutcome (severityDr), SmcCrewSeverityDue; GamePlanner.Fire.cs: AddSmcCrewSeverity; GameProjector.cs: SmcCrewAfter"),
               tests(R, ["ASmcCrewsStunWoundTakesItsSeverityDrAndARecallStillEliminatesHim"]) + "; " + tests(B, ["ASniperStunOfATankCrewedByASmcRollsHisWoundSeverityDr"]) + "; " + tests(G, ["TheWoundLineOfASmcCrewsStunNamesHim", "ASniperWoundThatPinsIsSaidWoundedAndPinned"]),
               None, D + "a SMC Temporary crew Stunned takes a Wound Severity dr (`{attempt}-wound-{crew}`, +1 if already wounded, 5 or 6 eliminates), rolled before the vehicle's change in Fire, ordnance, and a Sniper attack and read by the projector; a record with none keeps the older result (increment 4f; ruling R71.15); a Sniper wound's Severity dr is said in its record line (fix Round D)."),
    "A17.3": (None, ("append", "ScenarioA1Sniper.cs: SniperCheckDrm (wounded)"), tests(R, ["AWoundedLeadersLeadershipIsOneWorseInTheSniperCheck"]), None,
              D + "a wounded leader's leadership is one worse in a Sniper Check's DRM (`SniperCheckParticipant.Wounded`; increment 4b; fix Round C); a carried wounded leader still gives his carrier the two MF bonus (A17.2; ruling R71.28)."),
    "A18.12": (None, None, tests(R, ["LeaderCreationInCcAgainstAnAfvTakesMinusOne"]) + "; BacklogPass70Tests.ASquadsOriginalTwoAgainstATruckTakesNoAfvDrmForItsLeader", None,
               D + "the created leader's Leader Creation dr takes A18.2's -1 against an AFV when the vehicle is armored (`afvDrm`; increment 4f; ruling R71.17)."),
    "A4.11": (None, None, None, None,
              D + "a carried wounded man spends no MF of his own while his carrier moves him (A17.2; `ScenarioA1Wounds.MoverHalfMf`; increment 4g; ruling R71.16). Left: as before (pass 105)."),
    "A4.4": (None, ("append", "ScenarioA1Wounds.cs: Portage, CarriedManPp"), tests(R, ["ACarriersFivePpReduceItsMfByThePortageRulesAndTheManSpendsNone"]), None,
             D + "a carried wounded man counts 5 PP against his carrier's portage, kept for the move once taken (A17.2; increment 4g; ruling R71.16)."),
    "A4.72": (None, None, None, None,
              D + "A4.7, A4.72, and its example allow a wounded man to be carried in the APh; not built (backlog 74.1)."),
    "A7.8": (None, ("append", "ScenarioA1Wounds.cs: PinnedByWound (broken)"), tests(R, ["ABrokenSmcIsNeverPinnedByAWound"]), None,
             D + "a broken SMC is never pinned by a wound (\"Pinning affects broken units only during Interdiction\"; fix Round B; ruling R71.27)."),
    "D5.311": (None, None, None, None,
               D + "its \"[EXC: Sniper attack]\" holds: a Sniper never makes a crew Vulnerable through an unarmored facing; A14.3's broken Inherent crew of an unarmored vehicle or Partially Armored AFV is placed broken inside and routs beneath through the same routine (increment 4a; ruling R71.5)."),
    "D5.34": (None, ("append", "ScenarioA1Sniper.cs: CrewResult, VehicleConditions; GamePlanner.Snipers.cs: SnipeVehicle"),
              tests(R, ["ACeCrewOfAFullyArmoredAfvIsRecalledOnAOneAndStunnedOnATwo"]) + "; " + tests(B, ["ASnipersTwoStunsTheCeCrewOfATank"]), None,
              D + "Stun by a Sniper is built: a dr 2 Stuns the CE crew of a fully armored AFV, written as the vehicle's `conditions-changed` (increment 4a; ruling R71.5). Left: Stun by Falling Rubble (pass 135); a 1MT AFV's Recall on a Stun (pass 105; backlog 74.1)."),
    "D5.341": (None, None, tests(R, ["ACeCrewOfAFullyArmoredAfvIsRecalledOnAOneAndStunnedOnATwo"]), None,
               D + "Recall by a Sniper's dr 1 on the CE crew of a fully armored AFV is built (increment 4a; ruling R71.5). Left: the Recall of a 1MT AFV on a Stun (pass 105); the LLMC an Armor Leader's loss by a Sniper's Recall would cause (backlog 74.1); \"as soon as possible\" for the unloading."),
    "D5.342": (None, None, tests(R, ["ACeCrewOfAFullyArmoredAfvIsRecalledOnAOneAndStunnedOnATwo"]), None,
               D + "a Sniper's Stun of a crew already under Stun +1 Recalls it (increment 4a; ruling R71.5)."),
    "D5.343": (None, ("append", "ScenarioA1CapturedVehicleRules.cs: SmcCrewOutcome (severityDr), SmcCrewSeverityDue; GamePlanner.Fire.cs: AddSmcCrewSeverity; ScenarioA1Sniper.cs: CrewAttackedAsSmc"),
               tests(R, ["ASmcCrewsStunWoundTakesItsSeverityDrAndARecallStillEliminatesHim", "ASmcTemporaryCrewOfAVehicleNotFullyArmoredIsAttackedAsTheSmcHeIs"]) + "; " + tests(B, ["ASniperStunOfATankCrewedByASmcRollsHisWoundSeverityDr", "ASmcTemporaryCrewOfATruckIsEliminatedOrWoundedAsTheSmcHeIs"]),
               None, D + "the Stun wound takes its Wound Severity dr (with A17.11), rolled before the vehicle's change and read by the projector; a record with none keeps the older result (increment 4f; ruling R71.15); a Recall still eliminates him; a SMC crew of a vehicle not fully armored is attacked by a Sniper as the SMC he is (fix Round B; ruling R71.26). Left: the one-man turret (pass 105)."),
    "E1.76": (None, ("append", "ScenarioA1Sniper.cs: ActualSan"),
              tests(R, ["TheActualSanIsThePrintedLessItsReductionsAndAtNightNeverBelowThePrinted"]) + "; " + tests(B, ["AtNightASniperCheckNeverTakesTheSanBelowThePrintedSoTheCounterStays"]), None,
              D + "the Sniper Check floor is built: at night the SAN is the printed plus two, at most 7, less its reductions, never below the printed, and the record line says when it stays (increment 4b; fix Round D; ruling R71.9). Left: the captured MG's automatic Sniper dr (pass 90, task 90.6)."),
}

text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text
lines = text.split("\n")
found = {}
for i, line in enumerate(lines):
    if not line.startswith("| "):
        continue
    c = [cell.strip() for cell in line.split("|")[1:-1]]
    if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES and c[0] in ROWS:
        assert "| " + " | ".join(c) + " |" == line.replace("|  |", "|  |"), ("round trip", c[0])
        found.setdefault(c[0], []).append(i)
bad = [k for k in ROWS if len(found.get(k, [])) != 1]
if bad:
    print("ANCHOR", bad)
    sys.exit(1)

changes = []
for rule, (status, code, more_tests, pass_cell, note) in ROWS.items():
    i = found[rule][0]
    c = [cell.strip() for cell in lines[i].split("|")[1:-1]]
    before = (c[3], c[6])
    if status:
        c[3] = status
    if code:
        mode, value = code
        c[4] = value if mode == "replace" or not c[4] else c[4] + "; " + value
    if more_tests:
        c[5] = more_tests if not c[5] or c[5] == "none found" else c[5] + "; " + more_tests
    if pass_cell is not None:
        c[6] = pass_cell
    c[7] = (c[7] + " " if c[7] else "") + note
    assert "\u2014" not in lines[i] + note
    new = "| " + " | ".join(c) + " |"
    if c[3] in ("built", "not applicable"):
        assert not c[6], rule
    lines[i] = new
    changes.append((rule, before, (c[3], c[6])))

if "--check" in sys.argv:
    for item in changes:
        print(item)
    sys.exit(0)
io.open(P, "w", encoding="utf-8", newline="").write("\n".join(lines))
for item in changes:
    print(item)
print("rows changed:", len(changes))
