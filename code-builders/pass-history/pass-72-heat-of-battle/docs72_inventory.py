"""Pass 72 documents: the rule inventory's rows the pass built or touched. Every row is found and its old status checked before the file is written.

    python docs72_inventory.py --check   prints what would change, writes nothing
    python docs72_inventory.py           writes the rows
"""
import io
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
K = "Pass 72 (2026-10-27): "
R72 = "ScenarioA1Pass72RulesTests."
B72 = "BacklogPass72Tests."

# section: (old status, new status or None, code appended, tests appended, pass or None to keep, note appended, [(old, new) replacements in the code cell])
ROWS = {
    "A15.23": ("partly", "partly",
               "ScenarioA1OrdnanceCalculator.cs: HeroDrm (hero:<id> -1 on the To Hit DR, hero-two-man:<id> +1 for a PSK); ScenarioA1OrdnanceModels.cs: OrdnanceShot.HeroWeapons; ScenarioA1AreaCalculator.cs, ScenarioA1ArmorCalculator.cs (the hero DRM on the To Hit DR only); LiveOrdnance.cs (heroWeapons for a hero crew)",
               f"{R72}AHerosLightMortarTakesItsMinusOneOnTheToHitDrOnly; {R72}AHerosPfAndAtrAtAVehicleTakeTheMinusOneOnTheToHitDrAndNeverOnTheToKill; {R72}ALoneHeroFiresAPskWithTheTwoManPlusOneNegatedAndALoneLeaderStaysRefused; {R72}AHerosAtrAsSmallArmsTakesTheHeroicDrmAndNoHeroMgDrm; {R72}TheHerosDrmReadsInTheProposalAsTheRecordSaysIt; {B72}ALoneHeroFiresAPskAtATankWithTheTwoManDrmNegated; {B72}AHeroFiresAnAtrAtASquadAsSmallArmsWithHisHeroicDrm",
               "Pass 110",
               K + "a hero firing a non-MG SW takes -1 on its To Hit DR (a light mortar, a PF, an ATR at a vehicle), never on the To Kill DR or a hit's IFT (C13.25); a lone hero fires a PSK, its +1 for one man negated by his -1; an ATR fired as Small Arms is allowed and takes A15.24's heroic -1 and no second one; behind the shot's `heroWeapons` fact, so a shot recorded before the pass replays as resolved (increment 5e; fix Round B; ruling R72.8). The note's \"Refused (backlog): a LATW, a light mortar\" was stale: a hero already fired a light mortar, a PF, and an ATR at a vehicle, with no -1 (design section 4). Left: a Gun, a bazooka, a MOL-Projector, and a RCL (pass 110, with 110.6 to 110.9); the AAMG as a Rider (pass 105, with 105.1); a MG fired as ordnance by a hero, and A7.353's same-phase forfeit (backlog 75.2).",
               []),
    "A15.43": ("built with a deviation", "built with a deviation",
               "ScenarioA1ChargeCalculator.cs: RuleBar, NoneReachable (one search, the targets by range group, a relaxed cost beside the real one)",
               f"{R72}TheSingleSearchChargesTheNearestAndEveryEquidistantTargetAsBefore; {R72}AtATieTheAttackerChoosesAmongBothGroupsFromOneSearch; {R72}AnOrdinaryChargePastAMultiLevelBuildingKeepsTheNearest; {R72}ATargetCutOffByAnotherEnemyLocationKeepsRulingR305; {B72}ABerserkChargeEntersALocationHoldingAnEnemyTankAndEnemyInfantryAndTheFpfIsOwed",
               "",
               K + "one search reads every Known enemy target by range group; equidistant targets are the ATTACKER's choice, a tie between groups included; the charge enters a Location holding an enemy vehicle and enemy Infantry, with A8.312's FPF and the owed attack following (increment 5a; fix Round A; rulings R72.3, R72.5, R72.17). The note's \"a Gun crew's Location\" was stale since ruling R70.27 (design section 5, item 1). Deviation (R30.5): a charge the model cannot decide still ends in place, where a gap of the model (unreviewed terrain, a bridged hex, a pond hexside; backlog 75.2) or only other enemy Locations keep the target out of reach.",
               [("GamePlanner.CloseCombat.cs: ChargeSteps, MustCharge, ChargeBarred;", "GamePlanner.CloseCombat.cs: ChargeSteps, MustCharge, ChargeEntry;"),
                ("ScenarioA1CloseCombatRules.cs: ChargeBarred; ", "")]),
    "A15.431": ("partly", "partly",
                "ScenarioA1MovementCalculator.cs: MfAllotment (a berserk unit's 1 PP SW up to its IPC); ScenarioA1MovementModels.cs: BerserkAbandonFacts",
                f"{R72}AUnitMadeBerserkInMidMoveHasEightMfLessThoseSpentAndAbandonsBeyondItsIpc; {R72}WithNoKnownEnemyInLosTheChargeIsNeverHardened; {B72}ASquadMadeBerserkInMidMoveHasEightMfLessThoseSpentAndAbandonsItsFourthLightSw",
                "Pass 73",
                K + "a unit made berserk in mid move charges with eight MF less those spent and abandons at once every SW beyond A15.431's PP limits, not only 1 PP ones, the abandonment read at every step (increment 5a; ruling R72.6); a unit with no Known enemy in LOS returns to normal and is never Battle Hardened (fix Round A; ruling R72.17). The note's \"a unit made berserk in mid move ... was not found\" and \"DC Thrown only ... absent\" were stale: both were built (design section 5, item 1). Left: a berserk Dash across a road (pass 73, with 73.3); Wire as 1 MF in a charge's route (pass 120) (ruling R72.2).",
                []),
    "A15.432": ("partly", "built",
                "ScenarioA1FireEligibility.cs: SelectTargets (firerInTarget: the halved AFPh TPBF in the charged Location)",
                f"{B72}ABerserkChargeEntersALocationHoldingAnEnemyTankAndEnemyInfantryAndTheFpfIsOwed; {B72}ABerserkSquadsHalvedTpbfInTheAfphEliminatesTheLastKnownEnemyAndItReturnsToNormal; {R72}ATpbfIsSelectedOnlyWhenAFirerStandsInTheTargetLocation",
                "",
                K + "the note's \"not verified\" items are built: the DEFENDER's FPF is forced (A8.312, generic since pass 70) and berserk fire is reviewed; the stale comment in GamePlanner.Fire.cs was rewritten (increment 5a; design section 5, items 1 and 4). The halved AFPh TPBF in the charged Location was refused in Play (\"unit listed twice\") and now reaches it: `SelectTargets` attacks only the other side's units in the firer's own Location, decided in Rules from a firer-in-target fact (increment 5b; fix Round B; rulings R72.7, R72.21).",
                []),
    "A15.45": ("partly", "partly",
               "ScenarioA1ChargeCalculator.cs: RuleBar (a cliff or an unbridged Water Obstacle), NoneReachable, HardeningDue; ScenarioA1ChargeModels.cs: ChargeEntryFacts (Bridged); GamePlanner.CloseCombat.cs: ChargeEntry",
               f"{R72}PastACliffOrAWaterObstacleTheChargeTakesTheNextNearest; {R72}PastABarAnAlternateRouteStaysOnTheNearestWhileStillCloserThanTheNextNearest; {R72}WithTheNextNearestUnreachableTheChargeKeepsTheNearest; {R72}WithOnlyRuleBarsBetweenItAndEveryTargetTheChargeIsUnreachableAndHardeningIsDue; {R72}AGapOfTheModelOnTheFrontierKeepsRulingR305; {R72}TheRuleBarsAreACliffAndAnUnbridgedWaterObstacle; {R72}ASingleSearchFallsBackWithoutSearchingAgain; {B72}ABerserkSquadWhoseNearestEnemyIsAcrossWaterChargesTheNextNearestFromOneSearch; {B72}ABerserkSquadThatCanReachNoEnemyIsOfferedBattleHardeningAsTheMphEnds",
               "Pass 120",
               K + "where a cliff or an unbridged Water Obstacle lies across the nearest target's route, the charge takes the next nearest group that has a reachable target, unless its route stays strictly closer than that group's range; when a Known enemy unit is in LOS, none can be reached, and only such bars keep them out of reach, the unit stays in place and at the MPh's end loses berserk status and is Battle Hardened, its owner able to refuse (choice kind `battle-hardening`, key `berserkHardening:<unit>`) (A15.44; increment 5a; fix Round A; rulings R72.3, R72.4, R72.17). The note's \"test: none found\" is closed. Left: a bridged hex and a pond hexside are read as gaps of the model, so the charge ends in place (R30.5; backlog 75.2); Wire and minefields (pass 120), an FFE (pass 150), a Blaze (pass 185) (ruling R72.2).",
               []),
    "A15.46": ("partly", "built",
               "ScenarioA1SequenceCalculator.cs: BerserkReturnsAfterAttack, BerserkClearedText; ScenarioA1SequenceModels.cs: BerserkAttackFacts, KnownEnemyCount; GamePlanner.Fire.cs: BerserkReturnsAfterFire; GamePlanner.VehicleCloseCombat.cs: BerserkReturnsAfterVehicleKill; ScenarioA1CloseCombatModels.cs: CloseCombatFacts.VehicleSides, KnownEnemiesOnly; LiveCloseCombat.cs (vehicleSides, Known vehicles only)",
               f"{R72}ABerserkUnitStaysBerserkWhileAKnownEnemyVehicleSurvivesInItsLocation; {R72}ABerserkGroupReturnsToNormalWhenItsAttackClearsItsLocation; {R72}TheLocationIsReadOnlyForAnAttackThatCanReturnABerserkUnit; {R72}AFirerReplacedUnderAmmunitionShortageDoesNotReturnToNormal; {R72}AConcealedEnemyLeftInCcKeepsNoBerserkUnitBerserkFromThePassOn; {B72}ABerserkSquadsHalvedTpbfInTheAfphEliminatesTheLastKnownEnemyAndItReturnsToNormal; {B72}ABerserkSquadsFtInItsOwnLocationInTheAfphEliminatesTheLastKnownEnemyAndItReturnsToNormal; {B72}ABerserkSquadSetUpWithALoneTankInItsLocationDestroysItInTheCcphAndReturnsToNormal; {B72}ABerserkSquadThatDestroysTheTankInCcReturnsToNormalOnlyWhenNoKnownEnemyIsLeft; {B72}ABerserkSquadThatDestroysATankCarryingARiderInCcReturnsToNormal; {B72}ACcRoundRecordsThePassStampAndAnOlderRoundReplaysWithoutIt",
               "",
               K + "a berserk unit returns to Good Order when it, or the group it attacks with, eliminates every Known enemy unit in its Location (at least one) by TPBF or a FT in the AFPh, or by CC that destroys an enemy vehicle there and leaves no other Known enemy; a surviving Known enemy vehicle keeps it berserk (`vehicleSides`), a destroyed vehicle's PRC are not Known enemies left (A11.5), CC counts Known enemies only (`knownEnemiesOnly`), and a firer Replaced under Ammunition Shortage does not return; decided in Rules, written by Play as `conditions-changed`, the record line saying \"by its TPBF\" or \"by its FT\" (increment 5b; fix Rounds A and B; rulings R72.5, R72.7, R72.20). The backlog row that cites A15.45 for the vehicle case is marked built.",
               []),
    "A19.131": ("not built", "partly",
                "ScenarioA1UnitSubstitution.cs: AmmunitionShortageToken, AmmunitionShortage, RulesBar; ScenarioA1FireCalculator.cs: ShortageReplacements, Shortage, WeaponEffects, BreakdownWords; ScenarioA1FireModels.cs: FireAttack.AmmunitionShortage, FireRolls.ShortageSelection, FireResolution.ShortageEffects; ScenarioA1FireFollowUps.cs: ShortageReplaced; ScenarioA1FireEligibility.cs: FireLaneOffered; ScenarioA1OrdnanceModels.cs: OrdnanceShot.AmmunitionShortage; ScenarioA1Definitions.cs: DepletionNumber; ScenarioA1ArmorCalculator.cs: Depletion, DepletionShortage; GamePlanner.Fire.cs (the shortage effects); LiveFire.cs; LiveOrdnance.cs",
                f"{R72}AnOriginal12WithInherentFpReplacesTheMmcTheRandomSelectionPicks; {R72}ASquadFiringOnlyItsLmgIsNotReplacedAndItsLoweredBreakdownNumberIsItsX; {R72}ACapturedMgKeepsItsBreakdownNumberUnderAmmunitionShortage; {R72}AnEliteUnderscoredSquadIsReplacedByItsTwoHalfSquads; {R72}AConscriptIncapableOfQualityLossIsBrokenUnlessBerserk; {R72}ADcKeepsItsBreakdownNumberUnderAmmunitionShortage; {R72}DepletionNumbersAreOneLowerUnderAmmunitionShortage; {R72}AFireLaneIsRefusedUnderAmmunitionShortage; {R72}AnUnderscoredSquadUnderBothSsrsIsStillReplacedByItsTwoHalfSquadsOnAShortage; {R72}ACapturedGunsDepletionNumberIsNotLoweredUnderAmmunitionShortage; {R72}TheFireLaneIsOfferedUnderAmmunitionShortageOnlyWithACapturedWeapon; {B72}UnderAmmunitionShortageARussianFireGroupsOriginal12ReplacesOneSquadInPublic; {B72}AFireRecordMadeBeforeTheShortageFactVerifiesAsItWasRecorded; {B72}AFireLaneIsRefusedUnderAmmunitionShortage",
                "Pass 105",
                K + "the SSR token `ammo-shortage:<side>`; an attack with an Original 12 IFT DR in which an MMC of that side used its Inherent FP Replaces one such MMC after the attack, chosen by Random Selection (every tied MMC Replaced, A.9), keeping its broken or unbroken status (an MMC incapable of further quality loss breaks unless berserk), an underscored squad by its two HS (A19.13's EXC); every SW's B# is one lower (not a DC or a captured SW, A21.11) and its Original B# becomes its X# (A.11), only a printed X# itself one lower; Depletion Numbers are one lower (not a captured Gun); no Fire Lane, the fields hidden; the facts recorded on the attack or shot and checked against the card (increments 5c, 5d; fix Round B; rulings R72.10, R72.11, R72.12, R72.16, R72.18, R72.22). Not on CC, a Sniper, Residual FP, vehicle fire, a DC, the HE hit's IFT, or a To Hit 12. Left: the circled B# of vehicles and Guns and the Low Ammo counter at start (pass 105, with 105.7); OBA's Scarce Ammo (pass 150) (ruling R72.2); ordnance has no X# removal under the Shortage (backlog 75.2).",
                []),
    "A19.132": ("not built", "built with a deviation",
                "ScenarioA1UnitSubstitution.cs: UnderscoredElrToken, UnderscoredElr, RulesBar; ScenarioA1FireCalculator.cs: Round.ElrOf (the SSR ELR), Replace; ScenarioA1FireReference.cs: UnderscoredReplacementOf; FireTarget.UnderscoredElr, FireFirer.UnderscoredElr (FPF firers); ScenarioA1MoraleCheck.cs (underscoredElr and the stamp); LiveFire.cs: UnderscoredElr; ScenarioA1NightAndWeather.cs: RulesBar; ScenarioCards.cs (card.ssr)",
                f"{R72}TheUnitSubstitutionTokensAreParsedAndAMalformedOneIsRefused; {R72}AnSsrElrReplacesTheUnderscoredEngineerSquadByTheConscriptWhereR159OnlyBreaksIt; {R72}EveryUnderscoredMmcUnderAnSsrElrIsReplacedNormally; {B72}UnderAnSsrElrOfThreeAFailedMcReplacesTheGermanEngineerSquadByTheConscript; BacklogPass17Tests.TheSsrTokensAreCheckedAsANewGameChecksThem; BacklogPass70Tests.ALeaderLostToInterdictionCausesALlmcOfTheUnitsInHisLocation",
                "",
                K + "the SSR token `underscored-elr:<side>[:<group>]:<n>` (n 0 to 4) gives the side's or a group's underscored MMC that ELR, a group's token before its side's; a target and an FPF firer carry `underscoredElr`, read by `ElrOf` in place of R15.9's 5, and a failure beyond it Replaces the unit by the normal table: the German 8-3-8 by the 4-3-6 Conscript, the German 3-3-8 by its HS, the 2-3-7, only one with neither Disrupted (A19.12); a morale check record carries the stamp, and with it its `underscoredElr` is checked (increment 5c; fix Round B; rulings R72.10, R72.13, R72.15, R72.19, R72.22). R15.9 still holds where no token is given. A25.72's Finnish chart (p. 97) holds no Sissi unit, so the Finnish 8-3-8 and 3-3-8 stay as R15.13 has them (fix Round B). Deviation: a PRC keeps R15.9's 5 (`FirePrc` has no `underscoredElr`; backlog 75.2).",
                []),
    "A19.31": ("partly", "partly", "", "", "Pass 180",
               K + "the conveyance exception is built (passes 40 and 65; ruling R65.15) and was verified by the frame (design section 5, item 1; ruling R72.14): the note's \"Not verified\" is closed. Left: the eight MF of Inexperienced Infantry in a Human Wave (pass 180, with Human Wave).",
               []),
    "A19.32": ("built with a deviation", "built with a deviation",
               "ScenarioA1FireCalculator.cs: Inexperience (every SW but the FT under the stamp; the FT through AssaultWeaponRemoval), BreakdownWords; FireAttack.UnitSubstitution (the pass's stamp); LiveFire.cs (the stamp and the Inexperienced fact)",
               f"{R72}AConscriptsMgBreaksDownOneLowerWithThePassStampAndAsRecordedWithout; {R72}AGreenSquadStackedWithALeaderKeepsItsMgsBreakdownNumber; {R72}ALoweredBreakdownNumberIsSaidWithItsPrintedNumberAndItsCause; {B72}AConscriptSquadsLmgMalfunctionsAtItsLoweredBreakdownNumber",
               "",
               K + "R9.10 is lifted for attacks recorded with the pass's stamp (`unitSubstitution`): every SW used by Inexperienced Personnel, a MG included, has its B# one lower, Inexperienced read from the firer's fact (A19.3: a Green MMC with an unbroken leader is not) or by Conscript class where an older record lacks it; the FT takes its -1 through `AssaultWeaponRemoval` (R15.1), so no second one; the record line says \"less 1 in Inexperienced hands, A19.32\"; a record without the stamp replays as recorded (`dl-tractor-works-01` revision 1267, B11, DR 10, a hit) (increment 5c; fix Round B; rulings R72.9, R72.14). Deviation: the ordnance package reads Inexperience by class only, missing A19.3's Green-with-leader exemption (backlog 75.1).",
               []),
    "A15.21": ("built", "partly", "", "", "Pass 73",
               K + "found by the frame: a heroic leader keeps no heroic quality in fire (`FireFirer` has no heroic fact), so he fires a MG as a leader and takes no heroic -1 (design section 5, item 5); moved to pass 73 (ruling R72.2).",
               []),
    "A15.24": ("built", None, "ScenarioA1FireCalculator.cs (a hero's ATR as Small Arms)",
               f"{R72}AHerosAtrAsSmallArmsTakesTheHeroicDrmAndNoHeroMgDrm; {B72}AHeroFiresAnAtrAtASquadAsSmallArmsWithHisHeroicDrm", None,
               K + "a hero's ATR fired as Small Arms takes the heroic -1 within its range and no second -1 for the SW (increment 5e; ruling R72.8).",
               []),
    "A15.3": ("built", None, "ScenarioA1ChargeCalculator.cs: Hardening, HardeningText; GamePlanner.Choices.cs: BerserkHardeningPending, PlanBerserkHardeningAnswer",
              f"{R72}BattleHardeningMakesTheNextHigherQualityOrAnEliteUnitFanatic; {B72}ABerserkSquadThatCanReachNoEnemyIsOfferedBattleHardeningAsTheMphEnds", None,
              K + "A15.44's Battle Hardening of a berserk unit that can reach no Known enemy, at the MPh's end, takes the same table and the Fanatic path; its owner may refuse it by a pending choice (kind `battle-hardening`, key `berserkHardening:<unit>`), and the record says what the unit became (increment 5a; fix Round A; rulings R72.4, R72.17).",
              []),
    "A15.44": ("built", None, "ScenarioA1ChargeCalculator.cs: NoneReachable, HardeningDue, HardeningDueText, HardeningChoiceText, HardeningText; GamePlanner.cs (the MPh's end); GamePlanner.Choices.cs: BerserkHardeningPending, PlanBerserkHardeningAnswer",
               f"{R72}WithOnlyRuleBarsBetweenItAndEveryTargetTheChargeIsUnreachableAndHardeningIsDue; {R72}WithNoKnownEnemyInLosTheChargeIsNeverHardened; {B72}ABerserkSquadThatCanReachNoEnemyIsOfferedBattleHardeningAsTheMphEnds", None,
               K + "A15.45's \"follow A15.44\" is built for a unit already berserk: when a Known enemy unit is in LOS, none can be reached, and only rule bars keep them out of reach, the unit loses berserk status at the MPh's end and is Battle Hardened unless its owner refuses (E1.533's \"it is not berserk\"; increment 5a; fix Round A; rulings R72.4, R72.17).",
               []),
    "A7.21": ("partly", None, "ScenarioA1FireEligibility.cs: SelectTargets (firerInTarget)",
              f"{R72}ATpbfIsSelectedOnlyWhenAFirerStandsInTheTargetLocation; {B72}ABerserkSquadsHalvedTpbfInTheAfphEliminatesTheLastKnownEnemyAndItReturnsToNormal", None,
              K + "TPBF in the firer's own Location, outside the MPh's moving stack, attacks only the other side's units there (`ScenarioA1FireEligibility.SelectTargets`, from a firer-in-target fact); before, Play listed the firer among the targets and the attack was refused (increment 5b; fix Round B). Left: as before (pass 105).",
              []),
    "A7.211": ("built with a deviation", None, "", "", None,
               K + "a vehicle firing at range 0 into its own Location outside the MPh is TPBF and stays allowed (ruling R72.21; the comment corrected at fix Round B).",
               []),
    "A7.212": ("built", None, "ScenarioA1FireEligibility.cs: SelectTargets", f"{R72}ATpbfIsSelectedOnlyWhenAFirerStandsInTheTargetLocation", None,
               K + "a firer standing in the target Location attacks only the other side's units there, not its own (`SelectTargets`; increment 5b; fix Round B).",
               []),
    "A11.5": ("partly", None, "", "", None,
              K + "for A15.46 a destroyed vehicle's PRC are eliminated with it and are not Known enemies left (fix Round B; ruling R72.20). Left: those PRC stay in the state after their vehicle is destroyed (backlog 75.2); as before (pass 80).",
              []),
    "A.9": ("built with a deviation", None, "ScenarioA1FireCalculator.cs: ShortageReplacements (Random Selection, the roll shortageSelection)",
            f"{R72}AnOriginal12WithInherentFpReplacesTheMmcTheRandomSelectionPicks", None,
            K + "Ammunition Shortage's Unit Replacement is chosen by Random Selection among the MMC that used Inherent FP, every tied MMC Replaced (increment 5d; ruling R72.16); the record line lists every candidate's dr (fix Round B).",
            []),
    "A.11": ("built", None, "ScenarioA1FireCalculator.cs: Shortage, BreakdownWords",
             f"{R72}ASquadFiringOnlyItsLmgIsNotReplacedAndItsLoweredBreakdownNumberIsItsX; {R72}ALoweredBreakdownNumberIsSaidWithItsPrintedNumberAndItsCause", None,
             K + "under Ammunition Shortage a SW's B# is one lower and its Original B# becomes its X# (a B11 LMG is B10 and X11), only a printed X# itself one lower; the record line names the number and its cause, \"B10 (B11, less 1 for Ammunition Shortage, A19.131)\" (increment 5d; fix Round B; ruling R72.18). The note's \"No logic was found that turns the Original B# into an X#\" has been stale since pass 45 (ruling R45.4).",
             []),
    "A19.13": ("built", None, "ScenarioA1FireReference.cs: UnderscoredReplacementOf; ScenarioA1FireCalculator.cs: ShortageReplacements",
               f"{R72}AnSsrElrReplacesTheUnderscoredEngineerSquadByTheConscriptWhereR159OnlyBreaksIt; {R72}EveryUnderscoredMmcUnderAnSsrElrIsReplacedNormally; {R72}AnEliteUnderscoredSquadIsReplacedByItsTwoHalfSquads; {R72}AnUnderscoredSquadUnderBothSsrsIsStillReplacedByItsTwoHalfSquadsOnAShortage", None,
               K + "under an SSR ELR (A19.132) an underscored MMC is Replaced by the table: the German 8-3-8 by the 4-3-6 Conscript (a new row), the German 3-3-8 by its HS, the 2-3-7 (rulings R72.13, R72.15); Ammunition Shortage's Replacement keeps this rule's EXC, an underscored squad becoming its two HS (ruling R72.22) (increments 5c, 5d; fix Round B).",
               []),
    "A19.12": ("partly", None, "", "", None,
               K + "under an SSR ELR an underscored unit with no table entry is Replaced by its HS where it has one; only one with neither is Disrupted (increment 5c; ruling R72.15). Left: as before (pass 73).",
               []),
    "A19.3": ("built", None, "ScenarioA1FireCalculator.cs: Inexperience (the firer's Inexperienced fact)", f"{R72}AGreenSquadStackedWithALeaderKeepsItsMgsBreakdownNumber", None,
              K + "A19.32's lowered B# reads the firer's `Inexperienced` fact, so a Green MMC with an unbroken leader keeps its SW's B# under the pass's stamp, the ATR's -1 included (increment 5c; ruling R72.9); the ordnance package still reads class only (backlog 75.1).",
              []),
    "A21.11": ("partly", None, "", f"{R72}ACapturedMgKeepsItsBreakdownNumberUnderAmmunitionShortage; {R72}ACapturedGunsDepletionNumberIsNotLoweredUnderAmmunitionShortage; {R72}TheFireLaneIsOfferedUnderAmmunitionShortageOnlyWithACapturedWeapon", None,
               K + "Ammunition Shortage is built (A19.131), and its exclusion with it: a captured SW keeps its B#, a captured MG may still be given a Fire Lane, and a captured Gun's Depletion Number is not lowered (increment 5d; fix Round B; ruling R72.18). The note's \"Ammunition Shortage is not built\" is closed. Left: as before (pass 110).",
               []),
    "A9.22": ("partly", None, "ScenarioA1FireEligibility.cs: FireLaneOffered",
              f"{R72}AFireLaneIsRefusedUnderAmmunitionShortage; {R72}TheFireLaneIsOfferedUnderAmmunitionShortageOnlyWithACapturedWeapon; {B72}AFireLaneIsRefusedUnderAmmunitionShortage; CloseCombatComponentTests.TheFirePanelFollowsTheSelectionAndThePhase", None,
              K + "a side under Ammunition Shortage declares no Fire Lane but with a captured MG; the refusal cites A19.131, and the fire panel hides the Fire Lane fields (increment 5d; fix Round B; ruling R72.12). Left: a chosen Fire Lane weapon is still sent when the field hides (backlog 75.2); as before (pass 130).",
              []),
    "A9.7": ("built", None, "", f"{B72}AConscriptSquadsLmgMalfunctionsAtItsLoweredBreakdownNumber", None,
             K + "under the pass's stamp an Inexperienced MG malfunctions one lower, and under Ammunition Shortage every SW but a DC or a captured one; the record says the number reached and its causes (\"malfunctioned: the DR reached its B10 (B11, less 1 in Inexperienced hands, A19.32)\") (increments 5c, 5d; fix Round B; rulings R72.9, R72.12, R72.18).",
             []),
    "A22.3": ("built", None, "", "", None,
              K + "a FT's removal number is one lower under Ammunition Shortage (A19.131 excepts only the DC); its Inexperienced -1 stays the one `AssaultWeaponRemoval` takes (R15.1), so A19.32 adds none (increment 5d; the Unit Substitution referee's finding 2).",
              []),
    "A9.61": ("partly", None, "", "", None,
              K + "a MG fired as ordnance at an AFV by a hero takes no hero DRM (increment 5e; backlog 75.2); under Ammunition Shortage its ordnance B# is one lower (increment 5d). Left: as before (pass 110).",
              []),
    "A7.353": ("built", None, "", "", None,
               K + "a hero firing a light mortar forfeits his inherent FP in that attack (the projector's `GunCrewsFired`), but this rule's crew exception still lets him add halved inherent FP later in the same phase, where A15.23 forfeits it for the phase (increment 5e; backlog 75.2).",
               []),
    "C13.45": ("not built", "partly", "ScenarioA1OrdnanceCalculator.cs: HeroDrm (hero-two-man:<id>), the PSK's crew check",
               f"{R72}ALoneHeroFiresAPskWithTheTwoManPlusOneNegatedAndALoneLeaderStaysRefused; {B72}ALoneHeroFiresAPskAtATankWithTheTwoManDrmNegated", "Pass 110",
               K + "a lone hero fires a PSK, its +1 for one man firing a two-man SW negated by his -1 (cited to A15.23 since fix Round B), behind the shot's `heroWeapons` fact; a lone leader stays refused and the refusal says so (increment 5e; fix Round B; ruling R72.8). Left: two SMC firing a PSK, and the BAZ (pass 110).",
               []),
    "C13.48": ("partly", None, "", f"{R72}ALoneHeroFiresAPskWithTheTwoManPlusOneNegatedAndALoneLeaderStaysRefused", None,
               K + "a lone hero fires a PSK (C13.45, A15.23; increment 5e; ruling R72.8), and the card names it \"PSK\", not \"PSK -\" (fix Round B). Left: as before but the lone hero (pass 110).",
               []),
    "C13.25": ("built", None, "", f"{R72}AHerosPfAndAtrAtAVehicleTakeTheMinusOneOnTheToHitDrAndNeverOnTheToKill", None,
               K + "a hero's -1 with a non-MG SW is on the To Hit DR only, never the To Kill DR (A15.23; increment 5e; ruling R72.8).",
               []),
    "E1.533": ("not built", None, "", "", None,
               K + "by day, A15.44's end of berserk status for a unit that can reach no Known enemy is built (A15.45; increment 5a; rulings R72.4, R72.17). Lax, no Straying, and Known at night stay with pass 90.",
               []),
    "D3.71": ("not built", None, "", "", None,
              K + "Ammunition Shortage's circled B# for vehicles and Guns and the Low Ammo counter at start stay with this pass, with 105.7 (A19.131; ruling R72.2).",
              []),
}


def join(old, add, sep="; "):
    if not add:
        return old
    if not old or old == "none found" or old == "none found by name":
        return add
    return old + sep + add


def main():
    check = "--check" in sys.argv
    raw = io.open(P, encoding="utf-8", newline="").read()
    assert "\r" not in raw
    lines = raw.split("\n")
    found = {}
    for i, line in enumerate(lines):
        if line.startswith("| "):
            cells = [c.strip() for c in line.split("|")[1:-1]]
            if len(cells) == 9 and cells[0] in ROWS:
                assert cells[0] not in found, cells[0]
                found[cells[0]] = i
    missing = [s for s in ROWS if s not in found]
    assert not missing, missing
    before, after = {}, {}
    for s, (old_status, new_status, code, tests, pass_, note, repl) in ROWS.items():
        i = found[s]
        cells = [c.strip() for c in lines[i].split("|")[1:-1]]
        assert cells[3] == old_status, (s, cells[3])
        assert K not in cells[7], (s, "already has a pass 72 note")
        for old, new in repl:
            assert cells[4].count(old) == 1, (s, old)
            cells[4] = cells[4].replace(old, new)
        before[s] = (cells[3], cells[6])
        if new_status:
            cells[3] = new_status
        cells[4] = join(cells[4], code)
        cells[5] = join(cells[5], tests)
        if pass_ is not None:
            cells[6] = pass_
        cells[7] = join(cells[7], note, " ")
        assert "\u2014" not in "".join(cells), s
        after[s] = (cells[3], cells[6])
        lines[i] = "| " + " | ".join(cells) + " |"
    for s in ROWS:
        print(f"{s}: {before[s][0]} [{before[s][1]}] -> {after[s][0]} [{after[s][1]}]")
    if check:
        print("check only:", len(ROWS), "rows")
        return
    io.open(P, "w", encoding="utf-8", newline="").write("\n".join(lines))
    print("written:", len(ROWS), "rows")


main()
