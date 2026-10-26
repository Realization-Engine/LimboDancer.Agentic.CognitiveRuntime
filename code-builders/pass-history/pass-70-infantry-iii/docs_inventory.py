"""Pass 70 documents: the inventory rows of pass 70 and the other rows its code touched. Each row is found by its rule, its status checked, and its
status, code, tests, pass, and note changed; every row is checked before the file is written. LF, UTF-8, no BOM."""
import io
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
R = "ScenarioA1Pass70RulesTests."
B = "BacklogPass70Tests."
G = "PlayPagePass70Tests."
KEEP = None  # keep the cell as it is
N = "Pass 70 (2026-10-25): "

# rule: (status before, status after or KEEP, pass after or KEEP, code to add, tests to add, note to add)
ROWS = {
    # The 48 rows of pass 70.
    "A7.211": ("partly", "built with a deviation", "", "ScenarioA1PassengerActionRules.cs: MakesEnemyLocation",
               R + "AKnownEnemyUnitOnFootMakesTheLocationOfAnUnloadOrABailOutAnEnemyLocation",
               "the CC counter is no stored condition, since units are held in Melee only at the CCPh's end; the fire proposal says the PRC that "
               "survive are in an enemy Location and not held in Melee (A7.211, A11.71; increment 3i; ruling R70.32, the deviation). Left: the fire "
               "record line (backlog 72.1)."),
    "A10.2": ("partly", "built", "", "ScenarioA1MoraleCheck.cs: Resolve, LeaderLossSubjects, LeaderLossDueText, Unnamed, Withheld, EndsRoutAfterLeaderLoss; "
              "ScenarioA1FireCalculator.cs: ResolveCheck, RunCheck, LeaderLossReaches; GamePlanner.MoraleChecks.cs: AddMoraleCheck, AddLeaderLossChecks, LeaderLossDue",
              R + "ALlmcTakesTheReversedDrmAndRidersAndOtherVehiclesPassengersTakeNone; " + R + "AnUnbrokenLeaderWhoBreaksCausesALltc; " + R +
              "ALeadersLossOutsideFireCallsOnTheOtherUnitsOfHisSideThereAndTheVoluntaryRoutLeadersLossSaysA10711; " + B +
              "ALeaderLostToInterdictionCausesALlmcOfTheUnitsInHisLocation; " + B + "ALeaderEliminatedForFailureToRoutCausesALlmcBeforeThePhaseEnds; " +
              B + "ALeadersVoluntaryBreakCallsForALltcOfTheSquadWithHim; " + G + "TheRecordsSayALlmcAfterALeaderLostToFailureToRout",
              "a leader lost outside the Fire package (an Interdiction, Mopping Up, a Sniper, Rally Fate, a PF firer's Casualty Reduction, Failure to "
              "Rout, a Search) causes a LLMC, and a leader's Voluntary Break a LLTC, through one Morale Check member, recorded as "
              "`morale-check-resolved` (increments 3b, 3c; rulings R70.3, R70.4, R70.11); a leader lost in a rout causes a LLMC of bystanders too "
              "(ruling R70.44); a \"?\" unit that takes no check is not named in the public record (fix Round A). Left: a side with no declared ELR "
              "takes none through the member (backlog 72.1)."),
    "A10.41": ("not built", "built with a deviation", "", "ScenarioA1VoluntaryRout.cs: Threat, BreakPhaseBar, BreakUnitBar, BreakEffects, BreakSummary, "
               "LeaderBreakText, Refusal; GamePlanner.VoluntaryRout.cs: PlanVoluntaryBreak, VoluntaryBreakBar; GameProjector.cs (`voluntary-break`)",
               R + "AUnitMayBreakInTheLosAndNormalRangeOfAKnownArmedUnbrokenEnemyOrAdjacentToOne; " + R +
               "AnUnarmedEnemyAllowsAVoluntaryBreakOnlyAdjacentAndTheBreakIsDecidedInRules; " + R +
               "AVoluntaryBreakIsRefusedWhereBreakingWouldReduceTheUnitOrItNeverBreaksAndAfterARoutStep; " + B +
               "APinnedHalfSquadBreaksVoluntarilyAndRoutsAsInTheExampleOnPage69; " + B + "AUnitOutOfTheLosOfEveryKnownEnemyMayNotBreakVoluntarily; " +
               B + "AnUnarmedEnemyAllowsAVoluntaryBreakOnlyWhenAdjacent; " + G + "TheRoutPanelOffersAVoluntaryBreakByHexAndALeaderToRoutWith",
               "the action `asl.game.voluntary-break` at the RtPh's start, before any rout step: the units broken under DM (A10.62), a pin cleared "
               "(A7.83), a \"?\" lost (A12.14), a leader's LLTC (increment 3c; rulings R70.11, R70.43). The deviation: a concealed or hidden enemy "
               "unit does not allow the break, so a refusal tells nothing of a \"?\" (ruling R70.43; backlog 72.2). Left: a Passenger's or Rider's "
               "break (backlog 72.1)."),
    "A10.5": ("partly", "partly", "Pass 115", "ScenarioA1RoutCalculator.cs: InterdictionReach, VehicleNormalRange, GunNormalRange, RoutIgnoredCovers, RoutEndAdvice",
              B + "ABrokenSquadInOpenGroundInRangeOfAnEnemyAfvMustRoutAndIsInterdictedByIt; " + B + "AnOlderRecordedRoutPastAnEnemyAfvReplaysAsItDid",
              "the note's \"routs to or from upper building levels (steps are ground level only)\" was stale: rout by level is built since "
              "2026-10-08 (design section 5, item 2). Enemy vehicles and manned Guns now cause must-rout and RtPh DM within their Normal Range "
              "(increment 3d; ruling R70.14); with no legal step the advice says the unit surrenders if it can (A20.21) or is eliminated for Failure "
              "to Rout (fix Round B). Left: the Entrenched and Emplaced Gun exemptions and the fortification rout options (pass 115), a Blaze (pass 185)."),
    "A10.51": ("partly", "partly", "Pass 115", "ScenarioA1RoutCalculator.cs: BuildingGoesOn, RoutIgnoredCovers; GamePlanner.Buildings.cs: BuildingHexes, BoardBuildings",
               R + "AUnitInABuildingMayIgnoreItsOwnBuildingsHexesWhenChoosingWhereToRout; " + R +
               "AUnitThatMayIgnoreEveryCoverWithinReachIsToldSoAndWhereItMayEnd; " + B + "ABrokenUnitRoutsOutOfItsOwnBuildingPastItsOtherHex; " + B +
               "ABrokenUnitWhoseOnlyCoverInReachIsItsOwnBuildingIsToldItMayIgnoreIt",
               "the EXC is built: a routing unit may ignore the hexes of the building it starts in (A10.51's EXC, pp. 66 to 67; increment 3d; "
               "ruling R70.13, its citation corrected at 3d), with one building read of the map (3g); the advice names what it may ignore, \"so any "
               "legal route will do, one ending there included\" (the example on p. 69, S3; fix Round B). Left: minefield and FFE routes and the "
               "shellhole or entrenchment option (pass 115 and the terrain passes); a destination refigured when an enemy becomes Known (backlog 72.1)."),
    "A10.53": ("partly", "built", "", "ScenarioA1MoraleCheck.cs: InterdictionHeatOfBattleDue, EndsRout; ScenarioA1VoluntaryRout.cs: LeaderDrm, LeaderEliminated, "
               "InterdictionRevealsLeader; ScenarioA1RoutCalculator.cs: VehicleInterdiction",
               R + "AnInterdictionNmcsOriginalTwoCallsForHeatOfBattle; " + B + "AnInterdictionNmcsOriginalTwoDrawsHeatOfBattleAndABerserkUnitRoutsNoFurther",
               "the Interdiction NMC goes through the Morale Check member: an Original 2 draws Heat of Battle, and a unit it rallies or makes berserk "
               "ends its rout there (increment 3b; rulings R70.3, R70.5); a leader's Casualty Reduction takes its Wound Severity dr (pass 35); the "
               "Voluntary Rout leader's DRM (3c; ruling R70.12); vehicles and Guns Interdict (3d; ruling R70.14)."),
    "A10.532": ("partly", "built", "", "ScenarioA1RoutCalculator.cs: InterdictionReach, VehicleNormalRange, VehicleInterdiction, GunNormalRange, GunArcWeapon, "
                "VehicleWeapons, GunWorks; GamePlanner.PlayStepMemo.cs: MannedGuns",
                R + "AVehicleInterdictsWithinItsMgsRangeAndCoveredArcAndNotWithAPositiveDrm; " + R + "AGunThatMustChangeItsCaDoesNotInterdict",
                "vehicles and manned Guns Interdict within their Normal Range when no positive DRM would apply (not Abandoned, Shocked, in Motion, or "
                "Stunned; a MA only with a CE crew; a Gun that must change its CA does not) (increment 3d; ruling R70.14). Left: a mortar's minimum "
                "range, a restricted AAMG, a VCA-only CMG, Rear MGs, and Spotted Fire (backlog 72.1)."),
    "A10.533": ("partly", "built", "", "ScenarioA1ConcealmentChoices.cs: RemovalBar, RemovalEliminates; GamePlanner.ConcealmentChoices.cs: PlanRemoveConcealment",
                R + "AQuestionMarkIsRemovedVoluntarilyOnceInPlayADummyEliminatedAndAHiddenUnitRevealed",
                "a concealed unit may give up its \"?\" by choice at any time of either side's phase, and so Interdict as a Known unit (increment 3e; "
                "ruling R70.20)."),
    "A10.711": ("not built", "built", "", "ScenarioA1VoluntaryRout.cs: LeaderBar, LeaderDrm, LeaderEliminated, LeaderStepAllowed, RoutLimit, "
                "InterdictionLeaderAgrees, InterdictionRevealsLeader; GamePlanner.VoluntaryRout.cs: VoluntaryRoutLeaders; GamePlanner.Rout.cs: PlanRout (withLeader)",
                R + "AVoluntaryRoutLeaderAddsHisDrmToTheNmcAndIsEliminatedWhenItFails; " + B + "ALeaderRoutsWithABrokenSquadAndHisDrmPassesItsInterdiction; " +
                B + "ALeaderRoutingWithASquadThatFailsItsInterdictionIsEliminatedAndTheHalfSquadTakesTheLlmc; " + B +
                "ARoutWithNoLeaderIsRecordedAndReplaysAsBeforeThePass",
                "`withLeader` on the rout: the leader moves with the broken unit, takes no NMC, adds his DRM to its Interdiction NMC (`rout-interdicted`'s "
                "`leader` and `drm`), and is eliminated if it fails, which causes a LLMC (increment 3c; ruling R70.12). Left: his own portage (backlog 72.1)."),
    "A10.8": ("partly", "built", "", "ScenarioA1GameStart.cs: FanaticBySsr; ScenarioSetup.cs (`fanatic:<side>`)",
              R + "TheSsrsTokenSetsFanaticism; " + B + "AFanaticSsrMakesTheSidesPersonnelFanaticAsTheySetUp",
              "an SSR token `fanatic:<side>` or `fanatic:<side>:<group>` on `game-started` makes Personnel Fanatic at setup (increment 3d; ruling "
              "R70.15). Left: units created after setup (backlog 72.1)."),
    "A11.1": ("partly", "partly", "Pass 115", "ScenarioA1CloseCombatCalculator.cs: Guns; ScenarioA1CloseCombatFactBuilders.cs: Unit (guns)",
              R + "AGunsCrewFightsInCcWithItsInherentFpAndItsGunIsLostOnAColoredOneAndTheKillNumber; " + B +
              "AnAdvanceOntoAGunsCrewLeadsToCcAndItsGunIsLostOnAColoredOneAndTheKillNumber",
              "a Gun's crew is advanced against and fights in CC with its inherent FP; R24.3's refusal is lifted (increment 3h; ruling R70.27). "
              "Left: Pillbox CC (B30, pass 115)."),
    "A11.21": ("partly", "partly", "Pass 135", "ScenarioA1Withdrawal.cs: Entry, Revealed, SelectionRolled, DropsText, ForeignDrop; GamePlanner.Withdrawal.cs: "
               "WithdrawalEntries, AddWithdrawalReveals; ScenarioA1CloseCombatRules.cs: MayWithdrawByDropping",
               R + "AWithdrawalIsBarredOnlyByAKnownEnemyAndMeetsWhatIsThere; " + R + "AUnitAboveItsIpcWithdrawsByDroppingItsOwnSw; " + B +
               "AWithdrawalIntoAConcealedEnemysLocationEliminatesTheWithdrawerAndARandomSelectionLosesItsQuestionMark; " + B +
               "AWithdrawalIntoAHiddenGunsLocationPlacesTheGunAndItsCrewUnconcealed; " + B +
               "AUnitAboveItsIpcWithdrawsByDroppingItsMmgAndAWithdrawalRemovesTheDummiesItEnters; " + B +
               "AnMmgDroppedToWithdrawStaysBehindWhenItsUnitDiesOnEntry; " + G + "TheCcPanelOffersADropAboveTheIpcWithdrawalsTheSideMayKnowOfAndAGunsCrewHexFirst",
               "a withdrawal is barred only by a Known enemy unit; into a concealed enemy's Location the withdrawer is eliminated, a Random Selection "
               "loses its \"?\", hidden units are placed; a Dummy-only Location's Dummies are removed; a unit above its IPC drops SW, which stay "
               "unpossessed (increment 3h; fix Round C; ruling R70.26). Left: other building levels (pass 135); a concealed Gun in the Random "
               "Selection, and an Ambush Withdrawal's drops on the page (backlog 72.1)."),
    "A11.31": ("partly", "built", "", "ScenarioA1VehicleCloseCombatRules.cs: SidesWithAttacks, FirstCcSide, NextCcSide, OwingAttack; GamePlanner.VehicleCloseCombat.cs: "
               "PlanInfantrySequential; ScenarioA1CloseCombatCalculator.cs (sequential)",
               R + "ASequentialRoundIsOneAttackWithNoReplyAndKeepsTheAmbushDrm; " + R + "TheSidesWithAttacksCountInfantryAgainstInfantryAndTheAmbusherAttacksFirst; " +
               B + "TheH3ExamplesShapeAlternatesInfantryAndATankAgainstInfantryWithNoReply; " + B + "AnOlderRecordedVehicleCcSequenceKeepsItsRecordedNextAndClose",
               "Infantry against Infantry in a Location holding a vehicle joins the vehicle sequence (the H3 example): one attack by the side named "
               "next, no simultaneous reply, R11.16's refusal lifted (increment 3i; ruling R70.30); a berserk or reinforcing unit owes its attack on "
               "enemy Infantry there (fix Round C). Left: withdrawal and Infiltration in that sequence (backlog 72.1)."),
    "A11.34": ("partly", "built", "", "ScenarioA1VehicleCloseCombatRules.cs: FirstCcSide, NextCcSide (ambusher); ScenarioA1CloseCombatFactBuilders.cs: AmbushVehicles",
               B + "AnAmbushInAVehiclesLocationTakesTheVehicleDrmAndTheAmbusherAttacksFirstWithItsDrmBothWays",
               "an Ambush in a Location holding a vehicle: the ambusher resolves all its attacks first (increment 3i; ruling R70.30)."),
    "A11.4": ("partly", "partly", "Pass 115", "ScenarioA1CloseCombatCalculator.cs: AmbushDrm (vehicles); ScenarioA1CloseCombatFactBuilders.cs: AmbushVehicles",
              R + "TheAmbushDrTakesTheVehicleDrmOfEachSide; " + R + "TheAmbushDrmAppliesBothWaysInCcWithAVehicle",
              "an Ambush with a vehicle present takes +2 Vehicle and +1 for each BU or Stunned AFV, counted in the open (increment 3i; rulings "
              "R70.30, R70.41). Left: the pillbox drm (pass 115); a side whose force is a vehicle alone (backlog 72.1)."),
    "A11.5": ("partly", "partly", "Pass 80", "ScenarioA1VehicleCloseCombat.cs: AgainstVehicle (streetFighters, leaderCreation, captors)",
              R + "StreetFightingGivesMinusOneAgainstTheVehicleAndPlusOneToItsAttackButNotWithAnActualAmbush; " + R +
              "AMmcsOriginalTwoAgainstAVehicleCreatesALeaderWhoAddsHisLeadershipAndACcvOnlyWithoutASmc",
              "Street Fighting's -1, a created leader in place of a combining one, and the captors' +2 (increments 3j; fix Round C; rulings R70.33, "
              "R70.34, R70.46). Left: +1 for an Assault Engineer (pass 80, the catalog's trait), other halving penalties, Cavalry (pass 165)."),
    "A11.51": ("partly", "partly", "Pass 105", "ScenarioA1VehicleCloseCombat.cs: AgainstVehicle (ambush, streetFighters)",
               R + "TheAmbushDrmAppliesBothWaysInCcWithAVehicle; " + R + "StreetFightingGivesMinusOneAgainstTheVehicleAndPlusOneToItsAttackButNotWithAnActualAmbush",
               "the Ambush's -1 and +1 in vehicle CC both ways (increment 3i; ruling R70.30) and Street Fighting's (3j; ruling R70.33). Left: a "
               "turreted MA of 15mm or less as MG armament, the \"armed\" test on escorts (pass 105)."),
    "A11.52": ("partly", "built with a deviation", "", "ScenarioA1CapturedVehicleRules.cs: Passengers, PassengersCapturable, CapturedWithPassengers; "
               "ScenarioA1CloseCombatCalculator.cs: PassengerOf (vs-passenger-capture); ScenarioA1VehicleProjection.cs: CaptureStands",
               R + "PassengersAreAttackedOnlyToCaptureThemAtMinusTwoAndTheirCaptorsDefendAtPlusTwo; " + B + "ATrucksPassengerIsCapturedInCcAndTheTruckWithIt; " +
               G + "TheVehicleCcPanelOffersTheCaptureOfATrucksPassengerNamedHexFirst",
               "the capture of Passengers: -2 on attacks at them, +2 on their attacks at the captors, the vehicle captured with them (cause "
               "`passengers` for an armed unarmored vehicle, marked Abandoned until a captor enters) (increment 3j; fix Round C; ruling R70.34). The "
               "deviation, a reading for the user: the +2 applies to the vehicle's whole attack, its MG FP included, and the Passengers never attack "
               "apart (backlog 72.2)."),
    "A11.611": ("partly", "built", "", "ScenarioA1CloseCombatCalculator.cs: Rider; ScenarioA1PassengerActionRules.cs: CloseCombat",
                R + "ARiderAttacksWithPlusOneAndIsAttackedWithMinusOne; " + B + "ARiderIsAttackedApartAndOnlyAnImmobileVehiclesPrcAreHeldInMelee; " +
                G + "TheVehicleCcPanelOffersAnInfantryAttackOnARiderNamedHexFirst",
                "PRC in CC in Play: a Rider attacks apart with +1 and is attacked with -1; a BU Passenger is offered nothing (increment 3i; ruling "
                "R70.31). Left: a Rider's CC against a vehicle, a reading (backlog 72.1)."),
    "A11.71": ("partly", "partly", "Pass 165", "ScenarioA1PassengerActionRules.cs: HeldInMelee; GameProjector.cs (`prcMelee`)",
               R + "ACeHalftracksPassengersJoinItsAttackAndOnlyAnImmobileVehiclesPrcAreHeldInMelee; " + R +
               "AStacksTopIsNeverAUnitAboardAndOnlyAnImmobileVehiclesPrcAreHeld; " + B + "ARiderIsAttackedApartAndOnlyAnImmobileVehiclesPrcAreHeldInMelee",
               "only the PRC of an Immobile vehicle are held in Melee at the CCPh's end, never a crew inside; the CCPh-ending record carries the flag "
               "`prcMelee` and names no unit aboard (increment 3i; fix Round A; ruling R70.31). Left: Cavalry, cyclists, and skiers (pass 165)."),
    "A11.8": ("not built", "partly", "Pass 105", "RoadFlanks.cs: Of; ScenarioA1StreetFighting.cs: Flanked, Qualifies, Applies, ReturnBar; "
              "GamePlanner.StreetFighting.cs: StreetFighters, RoadSides, PlanStreetFightingReturn",
              R + "ARoadHexFlankedByBuildingsOnBothSidesQualifiesTheInfantryThatAdvancedFromTheirGroundLevel; " + R +
              "StreetFightingGivesMinusOneAgainstTheVehicleAndPlusOneToItsAttackButNotWithAnActualAmbush; " + R +
              "AStreetFighterReturnsOnceTheCcIsOverUnlessBrokenOrPinned; " + B +
              "AVehicleInARoadHexBetweenBuildingsIsFoughtByStreetFightingAndTheSquadReturnsToItsBuilding",
              "Street Fighting: a vehicle in a road hex flanked by buildings attacked from their ground level with -1, its attack on them +1, not "
              "with an actual Ambush; the return or staying, unless broken or pinned (`asl.game.street-fighting-return`) (increment 3j; ruling "
              "R70.33). Left: Reaction Fire from the buildings (pass 105), a vehicle in Bypass (pass 100), mines and Wire on the return (backlog 72.1)."),
    "A12.1": ("partly", "built", "", "ScenarioA1Sight.cs: Sees, SeerLocations, NearestSeer, EmptyVehicle, ActionLosesConcealment; GamePlanner.Sight.cs: "
              "SightTable, NearestSeer, WithSeen, SeerFactsOf",
              R + "EachPurposeCountsItsOwnSeers; " + R + "TheNearestSeerIsReadWithinAndBeyondSixteen; " + B + "AConcealedMoverIsNotRevealedByAHiddenUnit",
              "the note's \"movement, advance, Rally and CA change read any unbroken enemy\" was stale: they read a Good Order enemy since R35.5 "
              "(design section 5, item 2). One read of who sees for every caller, by purpose: Loss (Good Order), GainDenial (unbroken, Dummies "
              "included), Inspection; an empty vehicle (Abandoned, no crew inside, no PRC) is \"broken\" for each (increment 3a; fix Round D; ruling R70.16)."),
    "A12.11": ("partly", "partly", "Pass 120", "ScenarioA1ConcealmentChoices.cs: RemovalEliminates",
               R + "AQuestionMarkIsRemovedVoluntarilyOnceInPlayADummyEliminatedAndAHiddenUnitRevealed; " + B + "ARussianDummyInTheStacksLocationInspectsNothing",
               "a Dummy removed by its owner is eliminated (ruling R70.20); a Dummy is no unit for A12.151's loss and inspects nothing (fix Round "
               "A). Left: the mine-attack proof (pass 120); 5/8\" Dummies, Dummies among reinforcements (backlog 72.1); an advancing Dummy in Open "
               "Ground (backlog 51)."),
    "A12.12": ("partly", "built", "", "ScenarioA1ConcealmentChoices.cs: RoadConcealmentHex, SetupConcealmentTerrain; ScenarioA1FireFollowUps.cs: ConcealmentGains "
               "(vehicles, Emplaced Gun crews); ScenarioA1Concealment.cs: NonObConcealmentBar (off board)",
               R + "AnyReinforcementWaitsOffBoardUnderANonObQuestionMark; " + R + "AStackOfTwoWaitingOffBoardTakesOneQuestionMark; " + B +
               "AnOffBoardReinforcementWaitsConcealedWhateverItsOb; " + B + "AVehicleAndItsRiderGainQuestionMarkAsItsPlayerTurnEnds",
               "any non-concealed Good Order reinforcement stack may wait off board under one non-OB \"?\", never on top of another, lifting R20.5's "
               "refusal (increment 3e; rulings R70.22, R70.35); vehicles and an Emplaced Gun's crew gain \"?\" (ruling R70.22)."),
    "A12.121": ("partly", "partly", "Pass 115", "ScenarioA1FireFollowUps.cs: ConcealmentGains (Cases J and K for vehicles and Emplaced Gun crews)",
                R + "AnEmplacedGunsCrewAndAVehicleGainByCasesJAndKOnly",
                "gain for an Emplaced Gun's crew and for vehicles under Cases J and K, read from a render of the table (p. 107): only out of every "
                "LOS (increment 3e; ruling R70.22). Left: the Fortification row (pass 115), most of Case C."),
    "A12.122": ("partly", "built", "", "ScenarioA1FireFollowUps.cs: ConcealmentDr (Lax, Stealthy, a possessed Gun's US#)",
                R + "TheConcealmentDrTakesLaxAndStealthy",
                "+1 Lax, -1 Stealthy, and a possessed Gun's US#, decided in Rules from the raw facts (increment 3e; fix Round D; ruling R70.22)."),
    "A12.13": ("partly", "built", "", "", "",
               "the note was stale: ordnance at a Location holding both concealed and Known targets is built since pass 60 (ruling R60.9) (design "
               "section 5, item 2)."),
    "A12.14": ("partly", "built with a deviation", "", "ScenarioA1ConcealmentChoices.cs: RemovalBar, RemovalText, PendingLossBar, LossChoiceText; "
               "ScenarioA1Sight.cs: ViewerMayDecline, ConcealedLossSeers; GamePlanner.ConcealmentChoices.cs: PlanRemoveConcealment, PlanConcealmentLossAnswer",
               R + "TheViewerMayDeclineOnlyWhenEveryLossSeerWithinSixteenIsConcealed; " + R + "TheViewersPendingOptionIsRefusedToTheMoverAsTheOpenWindowIs; " +
               B + "AConcealedViewerMayDeclineToForceAMoversLossAndTheMoverLearnsNothing; " + B + "AnUnconcealedViewerForcesTheLossWithNoChoice; " +
               B + "AVoluntaryRemovalShowsAUnitRevealsAHiddenGunWithItsCrewAndRemovesADummy; " + G + "TheOwnerGivesUpAQuestionMarkFromThePlayPage; " +
               G + "TheViewersOptionIsShownToItsSideAloneAndTheMoverSeesTheWindow",
               "the note's fault (\"outside fire the viewer need only be unbroken\") was stale since R35.5, and the read is one since R70.16 (design "
               "section 5, item 2). Voluntary removal at any time (ruling R70.20); the viewer's option not to force a loss where every Loss seer "
               "within 16 is concealed, on a move step by day, hidden from the mover (increment 3e; fix Round A; ruling R70.21). The deviation that "
               "stays: the loss is forced in the advance, in fire, at night, and on a rolling step (R31d.2; backlog 72.1). Left: the overstacked "
               "case, the Dummy stack's Morale Level 7 out of LOS, the Sniper exception (backlog 72.1)."),
    "A12.141": ("partly", "built", "", "ScenarioA1Sight.cs: ActionLosesConcealment; GamePlanner.Sight.cs: AddActionConcealmentLoss, ConcealmentLossNote",
                R + "AnActionCostsAConcealedUnitsQuestionMarkInTheLosOfASeerWithinSixteen; " + B + "ATransferCostsBothUnitsTheirQuestionMarkInTheLosOfAGoodOrderEnemy",
                "every built action A12.141 names costs a \"?\" in the LOS of a Good Order enemy within 16: Deploy, Recombine, SMOKE grenades, a "
                "LLTC or LLMC, a PF or MOL check (by firing, R31d.2), a leadership modifier on a MC, Rally, or Search, Recover, Transfer, Dismantle, "
                "Scrounge, Hook Gun, the Mopping Up declaration; its exceptions stay free (increments 3a, 3b; ruling R70.17, corrected on the page "
                "before any build). Left: entrenching, a Fire, and Clearance, with the passes that build them (backlog 72.1)."),
    "A12.15": ("partly", "partly", "Pass 75", "ScenarioA1MovementCalculator.cs: EnemiesAtTarget (Minimum Move)",
               R + "AMinimumMoveIntoAConcealedUnitIsForcedBackAndPinsNone; " + B + "AMinimumMoveIntoAConcealedSquadIsForcedBackAndEndsItsMphUnpinned",
               "the note's \"a routing unit's entry (A10.533)\" was stale: it is built since pass 35 (task 35.4) (design section 5, item 2). A "
               "Minimum Move entry is forced back, MF spent, neither pinned nor CX (increment 3f; rulings R70.23, R70.38). Left: the Infantry OVR "
               "offer on a lone SMC outside the reviewed building case (pass 75; ruling R70.2)."),
    "A12.151": ("partly", "built", "", "ScenarioA1BypassDetection.cs: KeepsLane, EndsInBypass, AllConcealmentLost, Occupy; GamePlanner.Bypass.cs: AddBypassReveal, "
                "EndedInBypassReveal; ScenarioA1MandatoryFire.cs: Owed (Bypass)",
                R + "AStackForcedBackFromItsBypassExitKeepsItsLaneAndEveryQuestionMarkThereIsLost; " + R +
                "ABypassMemberBrokenOrPinnedEndsItsMphWhereTheQuestionMarksAreLost; " + R +
                "AnAttemptToOccupyAnObstacleWithConcealedUnitsRevealsThemAndForcesTheStackBack; " + R +
                "AFirerInTheBypassedObstacleMayFireTpbfAtTheBypassingStack; " + B + "AStackForcedBackFromItsBypassExitStaysInItsLaneAndEveryQuestionMarkThereIsLost; " +
                B + "APinnedBypassMemberRevealsTheQuestionMarksOfItsLocationAsTheWindowCloses",
                "a stack forced back from its Bypass exit keeps its lane and reveals every concealed unit there; a broken or pinned member does the "
                "same; an attempt to occupy the obstacle reveals and forces back; TPBF inside the hex is allowed and owed by no Mandatory Fire "
                "(increment 3f; rulings R70.23, R70.37); the loss needs a real concealed enemy unit (fix Round A); R10.7's refusal lifted. Left: "
                "\"any other Infantry unit which legally ends its MPh\" there (backlog 72.1)."),
    "A12.152": ("not built", "partly", "Pass 120", "ScenarioA1Searching.cs: UnitBar, MoveBar, MmcBar, HexesBar, Searched<T>, Reveal, LeadershipApplied, "
                "HalfMfCost, SearcherConditions; GamePlanner.Searching.cs: PlanSearch, SearchHexes; GameProjector.cs: Search (`units-searched`)",
                R + "TheSearchDrTakesStealthHalfSquadsTheBestLeaderLaxAndCx; " + R + "TheFinalDrLeavesThatManyAccessibleHexesUnsearched; " + R +
                "OnlyGoodOrderInfantryOfTheMovingStackWithAMmcNotPinnedNorUsingAssaultMovementSearches; " + R +
                "ASearchRevealsConcealedPlacesHiddenRemovesDummiesAndAnArmedGoodOrderDefenderDrawsTheCasualtyDr; " + B +
                "ASquadAndItsLeaderEndingTheirMoveSearchAndRevealAConcealedAndAHiddenSquad; " + B + "ASearchWaitsForTheDefendersWindowOnTheLastMfToClose; " +
                G + "TheMovementPanelOffersASearchOfTheHexesTickedAndBothSidesReadIt; " + G + "TheSearchButtonWaitsWhileAProposalIsMade",
                "the action `asl.game.search` at a moving stack's end: one more MF, TI, the Search dr with its drm in the open, the named hexes "
                "searched first then the others up to the Accessible count less the Final dr, reveals, hidden units placed, Dummies removed "
                "(increment 3g; fix Round B; rulings R70.24, R70.39, R70.42). Accessible is R70.24's reading. Left: minefields and Booby Traps "
                "(pass 120), a Fortified Building's reveal (pass 115)."),
    "A12.153": ("partly", "partly", "Pass 135", "GamePlanner.Buildings.cs: BoardBuildings, Building, MopUpHexes, SameMapBuilding; GamePlanner.PlayStepMemo.cs: "
                "Occupied, OccupiedIn",
                R + "MoppingUpIsBarredOnlyForANameThatIsNoBuildingAndABlazeIsNoPartOfOne; " + R + "TheMoppingUpProposalSaysOnlyWhatTheDeclarerKnows; " +
                B + "TheMapsFloodFillSplitsARowhouseAtItsWall; " + B + "AStoneBuildingNoCardNamesIsMoppedUpAndItsBrokenEnemySurrenders; " + B +
                "AnOlderRecordedMoppingUpOfACardsBuildingReplaysAsItDid",
                "Mopping Up in any multi-hex or multi-level building of the map, found by one flood fill per board (a Rowhouse split at its wall, "
                "a Blaze no part of one), a card's building keeping its id; its proposal says only what the declarer knows, everything found said at "
                "commit (increment 3g; fix Round A; ruling R70.25). Left: rubbled Locations (pass 135), a Fortified Building's reveal (pass 115)."),
    "A12.154": ("partly", "partly", "Pass 120", "ScenarioA1Searching.cs: CasualtyDue, CasualtyTrigger, RecordText (casualtyDrm)",
                R + "ASearchRevealsConcealedPlacesHiddenRemovesDummiesAndAnArmedGoodOrderDefenderDrawsTheCasualtyDr; " + R +
                "TheCasualtyDrsRecordLineGivesItsDrmOnlyToTheViewThatMayKnowIt; " + B + "ASearchCasualtyDrReducesTheSearchingSquad",
                "the Search half is built: one Casualty dr per Search dr where a Searched Location holds an armed Good Order DEFENDER unit or "
                "Residual FP, with Mopping Up's members (increment 3g; ruling R70.24); always made, the DEFENDER's \"may\" not offered (ruling "
                "R70.45, a deviation; backlog 72.2); its drm recorded as `casualtyDrm`, said to the DEFENDER's and the adjudicator's views only "
                "(fix Round E; ruling R70.48). Left: the minefield and Booby Trap triggers (pass 120), FFE (pass 150)."),
    "A12.16": ("partly", "built", "", "ScenarioA1Sight.cs: Inspectable, InspectsByPresence, OwnSeerLocations; GamePlanner.Sight.cs: Uninspected, UninspectedOf, "
               "ViewOf, HistoryViewOf; GameView.cs: Of (uninspected), SealedPresence.OutOfLos; CaseRead.cs (the planner's view)",
               R + "AStackIsInspectedOnlyAfterSetupAndInTheLosOfASeer; " + R + "ABrokenUnitAndAUnitInMeleeInTheEnemysLocationInspectIt; " + R +
               "AStackOutOfInspectionShowsItsTopAndCountsTheRest; " + B + "AnUnconcealedGermanStackOutOfEveryRussianGoodOrderUnitsLosShowsItsTopOnly; " +
               B + "TheStackIsInspectedInFullOnceARussianUnitSeesIt; " + B + "ACaseReadShowsAStackItMayNotInspectAsTheViewDoes; " + B +
               "AHistoryViewShowsWhatThePlannersViewShowsWithOrWithoutAMemo; " + G + "AStackBeyondTheHillShowsTheRussianItsTopAndACountNamedForTheRule",
               "after setup an enemy stack not concealed and out of the LOS of every Good Order ground unit of the viewing side is shown as a stack "
               "before play, its top counter and a sealed count, in every view, older games' included (increment 3a; rulings R70.18, R70.19); a "
               "side's units in the stack's own Location inspect it whatever their state, Dummies and prisoners excepted (3h; fix Round A; ruling "
               "R70.40); a history's views through a transient memo (fix Round D). Left: a view built with no live planner (backlog 72.2)."),
    "A12.2": ("partly", "partly", "Pass 105", "ScenarioA1ConcealmentChoices.cs: RoadConcealmentHex, SetupConcealmentTerrain, SharesConcealment; "
              "ScenarioA1FireFollowUps.cs: ConcealmentGains (vehicles)",
              R + "AVehicleOnAWoodsOrchardBrushOrGrainRoadSetsUpUnderQuestionMarkOrHidden; " + R + "AnEmplacedGunsCrewAndAVehicleGainByCasesJAndKOnly; " +
              B + "AVehicleAndItsRiderGainQuestionMarkAsItsPlayerTurnEnds; " + B + "ATankGivingUpItsQuestionMarkRevealsItsRiderAndTheRiderMayNotGiveItsUpAlone",
              "woods-, orchard-, brush-, and grain-road hexes at setup, and vehicle gain of \"?\" (increment 3e; ruling R70.22); a vehicle's \"?\" "
              "shared with its PRC and crew both ways (fix Round D). Left: Target Size kept secret and the Smoke Dispenser (pass 105); Cavalry, "
              "horses, bicycles (pass 165); 5/8\" Dummies (backlog 72.1)."),
    "A12.3": ("partly", "built", "", "ScenarioA1SetupCalculator.cs: Check (hidden vehicles); ScenarioA1Definitions.cs: HiddenSetupKinds",
              R + "AVehicleSetsUpHiddenByItsSsr",
              "HIP for vehicles by SSR, a hidden vehicle taking one squad-equivalent of the allowance (increment 3e; rulings R70.22, R70.36). "
              "Fortifications stay refused (A12.33)."),
    "A12.34": ("partly", "built with a deviation", "", "ScenarioA1ConcealmentChoices.cs: EmplacedHipLost; GamePlanner.ConcealmentChoices.cs: AddEmplacedHipLoss; "
               "ScenarioA1Sight.cs: NearestSeer",
               R + "AnEmplacedGunSetsUpHiddenOutOfConcealmentTerrainAndIsPlacedBeneathQuestionMarkOnceSeen; " + B +
               "AHiddenGunOutOfConcealmentTerrainIsPlacedBeneathQuestionMarkOnceAnEnemySeesIt; " + B + "AHiddenGunThatFiresIsNotRevealedByAHiddenEnemy",
               "an Emplaced Gun may set up hidden out of Concealment Terrain, placed beneath \"?\" at the next step or phase that finds a Good Order "
               "enemy in LOS (increment 3e; ruling R70.22); its reveal no longer counts hidden viewers (3a; design section 5, item 3). The deviation: "
               "the setup does not check that no enemy has a LOS to it (backlog 72.1)."),
    "A12.42": ("partly", "built", "", "ScenarioA1BypassDetection.cs: VehicleEndRevealsInBypass, VehicleReveals; GamePlanner.Bypass.cs: VehicleBypassReveal; "
               "GamePlanner.Vehicles.cs: PlanEndVehicle",
               R + "AVehicleEndingItsMphInBypassRevealsTheConcealedUnitsThereUnlessBroken; " + B +
               "AVehicleEndingItsMphInBypassRevealsTheConcealedSquadThereButNotTheHiddenOne",
               "a vehicle ending its MPh in Bypass of a Location with concealed enemy units reveals them unless it is \"broken\"; hidden units stay "
               "hidden (increment 3f; ruling R70.23)."),
    "A15.1": ("partly", "partly", "Pass 180", "ScenarioA1HeatOfBattle.cs: Subject (unarmed, PRC); ScenarioA1MoraleCheck.cs: InterdictionHeatOfBattleDue",
              R + "AnUnarmedUnitTakesNoHeatOfBattleAndNoReplacement; " + R + "AnInterdictionNmcsOriginalTwoCallsForHeatOfBattle",
              "Unarmed units and PRC are exempted (the `unarmed` fact); a MC outside Fire and Rally (the Interdiction NMC) calls Heat of Battle "
              "through the Morale Check member; the Bail Out NMC and D5.311's crew owe none (increment 3b; rulings R70.3, R70.7, R70.9, R70.10). "
              "Left: the nationalities still refused (pass 180)."),
    "A17.1": ("partly", "built", "", "ScenarioA1MoraleCheck.cs: BailOutResult; ScenarioA1FireCalculator.cs: ResolveCheck (Wound)",
              R + "ASmcsCasualtyReductionTakesItsWoundSeverityDr; " + B + "ASmcRidersBailOutTakesItsWoundSeverityDrAndAnOlderRecordReplaysAsItWas",
              "every Casualty Reduction outside fire takes the wound procedure through the Morale Check member, a SMC Rider's Bail Out included "
              "(the crash of a null severity repaired) (increment 3b; rulings R70.3, R70.9)."),
    "A18.12": ("partly", "built", "", "ScenarioA1VehicleCloseCombat.cs: AgainstVehicle (leaderCreation, createdLeader)",
               R + "AMmcsOriginalTwoAgainstAVehicleCreatesALeaderWhoAddsHisLeadershipAndACcvOnlyWithoutASmc; " + B +
               "ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack; " + B + "AnOlderRecordedVehicleCcWithAnOriginalTwoReplaysWithNoLeader",
               "a MMC's Original 2 against a vehicle draws a Leader Creation dr (`leaderCreation`, `createdLeader`); the created leader replaces a "
               "combining leader's DRM, adds none beside a hero, and with no SMC adds his leadership and +1 CCV (increment 3j; fix Round C; rulings "
               "R70.34, R70.46). Left: a created leader in a sequential Infantry round not counted as having attacked (backlog 72.2), and one whose "
               "infiltrating MMC dies on entry (backlog 72.1)."),
    "A19.11": ("partly", "built", "", "ScenarioA1FireCalculator.cs: ElrImmune (unarmed)",
               R + "AnUnarmedUnitTakesNoHeatOfBattleAndNoReplacement",
               "an Unarmed unit is never Replaced, read from the `unarmed` fact (increment 3b; ruling R70.7)."),
    "D6.1": ("partly", "built", "", "ScenarioA1PassengerActionRules.cs: CloseCombat, HeldInMelee; GamePlanner.VehicleCloseCombat.cs: PlanInfantrySequential",
             B + "ARiderIsAttackedApartAndOnlyAnImmobileVehiclesPrcAreHeldInMelee",
             "Passenger CC in Play (increment 3i; ruling R70.31)."),
    "D6.22": ("partly", "partly", "Pass 105", "ScenarioA1CloseCombatCalculator.cs: Rider",
              R + "ARiderAttacksWithPlusOneAndIsAttackedWithMinusOne",
              "a Rider's +1 in CC in Play (increment 3i; ruling R70.31). Left: their FP in an OVR (pass 105)."),
    "D6.24": ("partly", "partly", "Pass 100", "ScenarioA1MoraleCheck.cs: BailOutResult; GamePlanner.Riders.cs: AddBailOuts (drm, elr); ScenarioA1PassengerActionRules.cs: MakesEnemyLocation",
              R + "ABailOutTakesARiderLeadersDrmAndElrReplacementNeverDisruption; " + B + "ARiderLeadersDrmReachesTheBailOutNmcOfHisVehiclesRiders",
              "the NMC takes a Rider leader's DRM, Wound Severity, and ELR Replacement, never Disruption (`drm`, `elr`); Heat of Battle is not owed "
              "(A15.1); the record says the units are in an enemy Location and not held in Melee (increments 3b, 3i; rulings R70.9, R70.32). Left: "
              "Bypass (pass 100)."),
    "D6.651": ("partly", "built with a deviation", "", "ScenarioA1FireCalculator.cs: LeadershipReaches, LeaderLossReaches; GamePlanner.MoraleChecks.cs: AboardFacts",
               R + "APassengerLeaderAidsAndChecksOnlyHisVehiclesPassengers; " + R + "AFirePassengerLeaderAidsHisFellowPassengersWhenTheRecordCarriesThePassFacts",
               "wired: a Passenger leader's reach is read both ways in the check, his loss checks his vehicle's Passengers only (increment 3b; ruling "
               "R70.8). The deviation: in Fire a CE leader in an unmoved halftrack leads only his fellow Passengers, not the Location's units on foot "
               "(backlog 72.1)."),
    "D6.82": ("partly", "built", "", "ScenarioA1SupportWeaponRules.cs: NeedsNoLeader (carrierHalfSquad); GameProjector.cs (`crewOf`)",
              R + "ACarrierHalfSquadRecombinesWithNoLeader; " + B + "CarrierHalfSquadsRecombineWithNoLeaderAndOrdinaryOnesStillNeedOne",
              "a Carrier HS recombines with no leader and no TC, the leaving crew's `instance-created` recording its vehicle (`crewOf`) (increment "
              "3h; ruling R70.28). An elite HS and \"specified as towing ordnance\" still wait for a card; Carrier squads Deploying to mount are "
              "backlog 72.1."),
    # Other rows the pass's code touched: a note, and the cells as they stand unless named.
    "A.7": ("built with a deviation", KEEP, KEEP, "", "",
            "for the Right of Inspection, units held in Melee or broken inspect an enemy stack in their own Location though not Good Order (A.7; "
            "ruling R70.40); the seers of A12.14, A12.122, and A12.16 are read by purpose (ruling R70.16)."),
    "A1.32": ("partly", KEEP, KEEP, "ScenarioA1SupportWeaponRules.cs: NeedsNoLeader (carrierHalfSquad)", B + "CarrierHalfSquadsRecombineWithNoLeaderAndOrdinaryOnesStillNeedOne",
              "a Carrier HS recombines with no leader (ruling R70.28; D6.82). The CC referee read the rule's \"Guards\" as the Russian Guards unit "
              "type, where R31.4 reads prisoners' Guards: for the user, older than the pass. Left: the same terrain; Finns (pass 80)."),
    "A2.9": ("partly", KEEP, KEEP, "", "",
             "a unit aboard is never a sealed stack's top counter, before play as after (fix Round A); the same sealed form shows an uninspected "
             "stack in play (A12.16; ruling R70.18)."),
    "A4.134": ("built with a deviation", KEEP, KEEP, "", R + "AMinimumMoveIntoAConcealedUnitIsForcedBackAndPinsNone",
               "a Minimum Move into a Location of concealed enemy units is no longer refused: it is forced back, all MF spent, and the stack forced "
               "back is neither pinned nor CX, since it entered no hex (increment 3f; rulings R70.23, R70.38). Bypass stays refused (R10.9)."),
    "A7.83": ("built", KEEP, KEEP, "", "",
              "a Voluntary Break clears the unit's pin (the p. 69 example's pinned HS routs; increment 3c)."),
    "A8.312": ("built", KEEP, KEEP, "ScenarioA1MandatoryFire.cs: Owed (Bypass)", "",
               "TPBF inside a Bypassed hex is allowed and never owed against a stack in Bypass (A12.151's \"may\"; ruling R70.37)."),
    "A10.3": ("built", KEEP, KEEP, "", B + "AnUnarmedSquadThatFailsItsLlmcIsCasualtyReducedNotBroken",
              "the EXC for Unarmed units: one that fails a MC is Casualty Reduced, not broken, where the record carries the `unarmed` fact (A20.54; "
              "fix Round B; ruling R70.47)."),
    "A10.62": ("partly", KEEP, KEEP, "", "",
               "a Voluntary Break puts the unit under DM (increment 3c; ruling R70.11); vehicles and manned Guns give RtPh DM within their Normal "
               "Range (3d; ruling R70.14)."),
    "A10.7": ("built", KEEP, KEEP, "", "",
              "the Passenger or Rider leader EXC is now read: a Rider leader's DRM reaches the Bail Out NMC of his vehicle's Riders (ruling R70.9), "
              "and a Passenger leader's reach is his vehicle (D6.651; ruling R70.8)."),
    "A10.72": ("built", KEEP, KEEP, "", "",
               "the Voluntary Rout leader's DRM is mandatory on the Interdiction NMC (increment 3c), and a Search's leadership drm is always applied "
               "(3g)."),
    "A11.12": ("built", KEEP, KEEP, "", "",
               "in a Location holding a vehicle, a unit attacked by Infantry and by a vehicle is counted apart (A11.12's EXC; increment 3i; ruling "
               "R70.30)."),
    "A11.13": ("built", KEEP, KEEP, "ScenarioA1CloseCombatCalculator.cs: Guns", R + "AGunsCrewFightsInCcWithItsInherentFpAndItsGunIsLostOnAColoredOneAndTheKillNumber",
               "the note's \"Guns do not arise\" no longer holds: a Gun's crew fights in CC, and each Gun of a unit eliminated is lost on a colored "
               "dr of 1 and a dr at or below the black Kill Number, read where the record lists the unit's `guns` (increment 3h; ruling R70.27). "
               "Left: the loss after a vehicle's CC attack (backlog 72.2)."),
    "A11.15": ("built", KEEP, KEEP, "", R + "ABerserkOrReinforcingUnitOwesItsAttackOnEnemyInfantryWhereAVehicleStands",
               "a unit reinforcing a Melee where a vehicle stands owes its attack on Known enemy Infantry no Infantry has attacked (fix Round C)."),
    "A11.19": ("built", KEEP, KEEP, "", "",
               "the Ambush record counts each side's vehicles and BU or Stunned AFVs in the open, since a concealed unit in CC reveals its strength "
               "before declarations (ruling R70.41)."),
    "A11.22": ("built with a deviation", KEEP, KEEP, "", "",
               "an infiltrating unit above its IPC drops SW, which stay in the Location (`.cc-infiltration-drop`; increment 3h; ruling R70.26)."),
    "A11.6": ("built", KEEP, KEEP, "", "",
              "after the PAATC, an advance into a Location holding an enemy vehicle and enemy Infantry leads to CC with both, in the vehicle sequence "
              "(increment 3i; ruling R70.30)."),
    "A11.62": ("partly", KEEP, KEEP, "ScenarioA1VehicleCloseCombatRules.cs: PassengersJoin", R + "ACeHalftracksPassengersJoinItsAttackAndOnlyAnImmobileVehiclesPrcAreHeldInMelee",
               "a CE halftrack's Passengers join the vehicle's attack (a MMC its printed FP, a SMC 1, pinned half, broken none, then the vehicle's "
               "halvings) (increment 3i; ruling R70.31). Left: the attack offered with or without them (backlog 72.2), RMG, IFE, separate attacks "
               "by each weapon, a CMG limited to the VCA (pass 105)."),
    "A11.7": ("built", KEEP, KEEP, "", "",
              "a crew inside a vehicle is never held in Melee; only an Immobile vehicle's PRC are (A11.71; increment 3i; ruling R70.31)."),
    "A12.31": ("built", KEEP, KEEP, "", "",
               "a hidden unit whose owner gives up its \"?\" is placed and revealed in full (ruling R70.20); a record line says it (fix Round E)."),
    "A12.32": ("built", KEEP, KEEP, "", "",
               "a \"?\" placed over hidden units has a record line for the other side (fix Round E)."),
    "A12.33": ("refused", KEEP, KEEP, "", "",
               "vehicles may now set up hidden by SSR (A12.3; ruling R70.36); Fortifications stay refused."),
    "A12.41": ("built", KEEP, KEEP, "", "",
               "the PAATC's subjects and the vehicle's sight are read through R70.16's Loss purpose: hidden units and PRC are no seers (increment 3a)."),
    "A15.3": ("built", KEEP, KEEP, "", "",
              "in a morale check outside fire (the Morale Check member) Battle Hardening is always taken: the owner's refusal there is backlog 72.1."),
    "A15.43": ("built with a deviation", KEEP, KEEP, "", B + "ABerserkSquadBesideItsTankOwesItsAttackOnTheEnemyInfantryThere",
               "a berserk unit in a Location holding its vehicle owes its attack on Known enemy Infantry there (fix Round C); a charge into such a "
               "Location still ends in place (R30.5; backlog 72.1)."),
    "A17.11": ("built", KEEP, KEEP, "", "",
               "the note's \"No LLMC follows a mortal wound outside fire\" no longer holds: the LLMC is made through the Morale Check member "
               "(increment 3b; ruling R70.3)."),
    "A19.12": ("partly", KEEP, KEEP, "", R + "ABailOutTakesARiderLeadersDrmAndElrReplacementNeverDisruption",
               "PRC are never Disrupted, behind the PRC facts (`prc-not-disrupted`), and the Bail Out NMC never Disrupts (increment 3b; ruling R70.9). "
               "Left: the SS against Russians exception."),
    "A20.21": ("built with a deviation", KEEP, KEEP, "", "",
               "a unit that must rout with no legal step is told it surrenders if it can or is eliminated for Failure to Rout (fix Round B)."),
    "A20.54": ("partly", KEEP, KEEP, "", B + "AnUnarmedSquadThatFailsItsLlmcIsCasualtyReducedNotBroken",
               "an Unarmed unit that fails a MC suffers Casualty Reduction instead of breaking, where the record carries `unarmed` (fix Round B; "
               "ruling R70.47). The rout still reads Unarmed Personnel as armed (backlog 72.2)."),
    "A21.22": ("partly", KEEP, KEEP, "", "",
               "an armed unarmored vehicle captured with all its Passengers takes the cause `passengers` and is marked Abandoned until a captor "
               "enters it (A11.52; ruling R70.34, a reading the CC referee found right)."),
    "D5.311": ("partly", KEEP, KEEP, "", R + "ABrokenUnprotectedCrewThatFailsAMcIsEliminated",
               "a crew already broken that the attack reaches is eliminated by Casualty Reduction (`crewBroken`); its ELR, Heat of Battle, and "
               "leadership are not owed (A15.1, A19.11, D3.44), so the note's \"ELR and Heat of Battle for the crew (pass 70)\" is closed "
               "(increment 3b; ruling R70.10)."),
    "D5.42": ("partly", KEEP, KEEP, "ScenarioA1CrewActsRules.cs: RecrewCandidate, CapturedThisCcph", R + "ACaptorEntersTheVehicleItCapturedInThatCcphWhateverMfItSpent; " +
              B + "AHalfSquadThatCapturedAnAbandonedTankEntersItAsItsCrewInThatCcph",
              "a captor may enter the vehicle it captured as its crew in the CCPh of its capture, whatever MF it spent (increment 3j; ruling "
              "R70.34). Left: D5.43's failure (pass 105)."),
    "D6.5": ("partly", KEEP, KEEP, "ScenarioA1PassengerActionRules.cs: MakesEnemyLocation", "",
             "the CC counter in an enemy Location is no stored condition: the unload record says the units are in an enemy Location and not held "
             "in Melee (ruling R70.32). Left: Bypass (pass 100); the interleaving of the interim move (pass 105)."),
}


def add(cell, more):
    if not more:
        return cell
    return more if not cell or cell == "none found" else cell + "; " + more


raw = io.open(P, encoding="utf-8", newline="").read()
assert "\r\n" not in raw
lines = raw.split("\n")
found = {}
before = {}
after = {}
for i, line in enumerate(lines):
    if not line.startswith("| "):
        continue
    c = [cell.strip() for cell in line.split("|")[1:-1]]
    if not (len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES):
        continue
    if c[0] not in ROWS:
        continue
    if c[0] in found:
        print("DUPLICATE", c[0])
        sys.exit(1)
    found[c[0]] = i
    was, status, pass_, code, tests, note = ROWS[c[0]]
    if c[3] != was:
        print("STATUS", c[0], "is", c[3], "expected", was)
        sys.exit(1)
    before[c[0]] = c[3]
    if status is not KEEP:
        c[3] = status
    if pass_ is not KEEP:
        c[6] = pass_
    c[4] = add(c[4], code)
    c[5] = add(c[5], tests)
    c[7] = (c[7] + " " if c[7] else "") + N + note
    for cell in c:
        assert "|" not in cell and "\n" not in cell and "\u2014" not in cell, c[0]
    if status is not KEEP and c[3] in ("built", "built with a deviation") and c[6]:
        print("BUILT WITH A PASS", c[0], c[6])
        sys.exit(1)
    after[c[0]] = c[3]
    lines[i] = "| " + " | ".join(c) + " |"
missing = sorted(set(ROWS) - set(found))
if missing:
    print("MISSING", missing)
    sys.exit(1)
io.open(P, "w", encoding="utf-8", newline="").write("\n".join(lines))
print("rows changed:", len(found))
for label, table in (("before", before), ("after", after)):
    counts = {}
    for rule, status in table.items():
        counts[status] = counts.get(status, 0) + 1
    print(label, sorted(counts.items()))
print("status changed:", sum(1 for r in found if before[r] != after[r]))
