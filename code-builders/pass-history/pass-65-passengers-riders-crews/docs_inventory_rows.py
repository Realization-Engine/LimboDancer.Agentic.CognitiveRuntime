"""Pass 65 documents: the inventory's rows of the pass, and the other rows its code touched.

Each entry: the rule, the status and pass the row must have now (asserted), and its new status, Where and Test additions, pass, and note sentence.
Every row is found and checked before the file is written. LF, UTF-8, no BOM.
    python docs_inventory_rows.py --check   checks the anchors and prints the changes only
"""
import io
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
R = "ScenarioA1Pass65RulesTests."
B = "BacklogPass65Tests."
G = "PlayPagePass65Tests."
N = "Pass 65 (2026-10-24): "

# rule: (old status, old pass, new status, where add, test add, new pass, note sentence)
ROWS = {
    "A2.52": ("partly", "Pass 65", "partly", "ScenarioA1CarrierRules.cs: SetupBar", R + "ACarrierIsNeverBuAndItsCrewAndPassengersStayCeWhenStunned", "Pass 105",
              "a Carrier sets up CE and may not set up BU (D6.84; task 65.5, increment 2f; ruling R65.18). Left: a CE or BU choice at setup for any other AFV (pass 105)."),
    "A4.11": ("partly", "Pass 65", "partly", "ScenarioA1PassengerCalculator.cs: ConveyedAllowance, ConveyedLeaderBonus",
              R + "AfterUnloadingFourMfNoDoubleTimeAndNoLeaderBonus; " + B + "UnloadedUnitsMoveNextOnFourMfWithoutDoubleTime", "Pass 105",
              "a unit that unloads has four MF for the rest of its MPh, with no leader bonus (task 65.3, increment 2d; ruling R65.15). Left: the Road Bonus can lift an unloader above four MF (backlog 71.1)."),
    "A4.5": ("partly", "Pass 65", "built", "ScenarioA1PassengerCalculator.cs: BoardingDoubleTimeBar, ConveyedDoubleTimeBar",
             R + "AfterUnloadingFourMfNoDoubleTimeAndNoLeaderBonus; " + B + "UnloadedUnitsMoveNextOnFourMfWithoutDoubleTime", "",
             "no Double Time for a unit that unloads, and none for a unit that boards later in the Player Turn (task 65.3, increment 2d; ruling R65.15). Wire is pass 120's, with its own rows."),
    "A5.3": ("partly", "Pass 65", "built", "ScenarioA1RiderRules.cs: Capacity, CapacityBar; ScenarioA1PassengerCalculator.cs: CapacityBar",
             R + "RiderCapacityFollowsTheVehicleClass; " + R + "ACarrierCarriesFourPassengerPpAndEightRiderPp", "",
             "Riders board within their own capacity by vehicle class, counted apart from Passenger PP, and neither counts against the Location's limits (task 65.4, increment 2e; ruling R65.16)."),
    "A7.21": ("partly", "Pass 65", "partly", "ScenarioA1PrcRules.cs: Exposure", R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf", "Pass 105",
              "TPBF against PRC from the firer's own or a higher Location is built (A7.211; task 65.1, increment 2b). Left: IFE, which has no counter (pass 105)."),
    "A7.211": ("partly", "Pass 65", "partly", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1FireCalculator.cs: WithPrc",
               R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf", "Pass 70",
               "the PRC of a vehicle in the firer's Location, or below it, are attacked by TPBF, a BU halftrack's included (D6.61; task 65.1, increment 2b; ruling R65.6). Left: the CC counter and the Melee wording (pass 70)."),
    "A7.307": ("partly", "Pass 65", "partly", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1FireCalculator.cs: WithPrc; GamePlanner.Riders.cs: AddBailOuts",
               R + "AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack; " + R + "TheCeDrmTakesNeitherAPositiveTemNorHeightAdvantageWithTheFact; " + B + "APinnedRiderBailsOutAndChecksItsSw", "Pass 110",
               "Passengers and Riders are Vulnerable PRC of a General Collateral Attack, a pinned or broken Rider Bails Out, and R25.6's refusal of an AFV in positive TEM is lifted where the attack carries `prc` (tasks 65.1, 65.4; increments 2b, 2e; rulings R65.5 to R65.7, R65.17). Left: ATMM (pass 110)."),
    "A7.308": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: VehicleEffects (Random Selection of the vehicles affected, `randomSelection`), WithPrc; GamePlanner.PassengerTargets.cs: SurvivorEvents",
               R + "RandomSelectionPicksTheTrucksA2FpAttackAffects; " + B + "ARussianHalfSquadsFireMeetsThreeTrucksAndRandomSelectionPicksTwo; " + B + "AKilledTrucksPassengersRollForSurvival", "Pass 110",
               "on the IFT, more unarmored vehicles than the column's highest KIA# are picked by Random Selection, and R25.3's one-vehicle refusal is lifted where the attack carries `prc`; their PRC take the Collateral Attack and roll for Survival (tasks 65.1; increments 2b, 2k; rulings R65.8, R65.26). Left: the ordnance Infantry Target Type and a mortar's hit keep `vehicles-beyond-kia` (pass 110); horses (pass 165)."),
    "A7.51": ("partly", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: FireGroupBar, MountedFire", R + "D664GroupsPassengersWithTheirVehicleAndACeHalftracksWithInfantry", "Pass 105",
              "Passengers fire from their vehicle and D6.64's makeup is decided in Rules (task 65.2, increment 2c; ruling R65.12). Left: a vehicle's MG and its Passengers in one FG in Play, where vehicles still fire alone (R25.7), and the Bypass LOS clause (pass 105)."),
    "A7.82": ("partly", "Pass 65", "partly", "", "", "Pass 105",
              "a pinned PRC is BU (A7.821's row; task 65.1, increment 2b). Left: the crew's three items above (pass 105)."),
    "A7.821": ("not built", "Pass 65", "partly", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1PassengerCalculator.cs: InterimMembers, LoadFailures; ScenarioA1RiderRules.cs: CombatBailOut",
               R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf; " + R + "ALoadFailsOnAPinOrABreakAndAnUnloadDoesNot; " + B + "APinnedRiderBailsOutAndChecksItsSw", "Pass 105",
               "a pinned Passenger is BU, a pinned unloader stays in its Dismount Location, and a Rider the IFT pins Bails Out (tasks 65.1, 65.3, 65.4; increments 2b, 2d, 2e; rulings R65.6, R65.14, R65.17). Left: a pin on a Rider by an ordnance, mortar, FT, MOL, or DC hit leaves it aboard (pass 105)."),
    "A11.611": ("partly", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: CloseCombat", R + "PassengersInCcAPassedSwAndAnUnloadAfterFire; " + R + "ARiderFiresAtHalfAQuarterOnceItsAfvMovedAndNoSw", "Pass 70",
                "a Rider's part in CC apart with +1 is decided in Rules (task 65.2, increment 2c; ruling R65.13), and no Play path reads it: a CC action that names a PRC is refused. A vehicle destroyed in CC still gives no Crew Survival, as D5.6 has it. Left: the CC of PRC in Play (pass 70)."),
    "A11.71": ("not built", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: CloseCombat", R + "PassengersInCcAPassedSwAndAnUnloadAfterFire", "Pass 70",
               "a Mobile vehicle's PRC leave Melee after the first round, decided in Rules (task 65.2, increment 2c; ruling R65.13); no Play path reads it. Left: PRC in CC in Play (pass 70)."),
    "A21.2": ("partly", "Pass 65", "built with a deviation", "ScenarioA1CapturedVehicleRules.cs: AutomaticCaptor, AutomaticCaptureDue, CaptureAttemptBar, Captured; GamePlanner.CapturedVehicles.cs; GamePlanner.VehicleCloseCombat.cs (the capture attempt)",
              R + "AnAbandonedAfvIsCapturedAsTheCcphEndsWhenNoneOfItsSidesPersonnelIsThere; " + R + "ACaptureAttemptCapturesTheAbandonedTankWhereAnAttackWouldEliminateIt; " + B + "AnAbandonedPanzerIsCapturedAsTheCcphEndsAndCrewedByARussianCrewUnderTheTemporaryPenalties; " + B + "ACaptureAttemptInCcTakesAnEscortedAbandonedPanzer; " + G + "TheCaptureAttemptNamesTheAfvAfterItsHexSendsCaptureAndShowsItsArithmetic", "Pass 75",
              "an Abandoned AFV is captured as the CCPh ends with none of its side's Personnel there, or by a capture attempt in vehicle CC at -1 with A11.61's DRM (`vehicle-captured`), and a captured vehicle is used by a Temporary Driver or crew (task 65.8, increment 2i; ruling R65.22). Older R11.16 captures replay as recorded. The deviation: the automatic capture is forced and needs Good Order Infantry (backlog 71.2). Left: a captured vehicle's VP (pass 75)."),
    "A21.21": ("not built", "Pass 65", "built with a deviation", "ScenarioA1CapturedVehicleRules.cs: TemporaryDriver; ScenarioA1VehicleProjection.cs: DriverStands",
               B + "ACapturedTrucksTemporaryDriverRunsItAndAnOlderCaptureReplaysAsBefore", "",
               "an MMC captor runs a captured unarmed vehicle as its Temporary Driver (task 65.8, increment 2i; ruling R65.22). The deviation: a SMC captor, whom A21.21 has removed, rides as its Temporary crew (backlog 71.1, the user's reading)."),
    "A21.22": ("not built", "Pass 65", "partly", "ScenarioA1CapturedVehicleRules.cs: Manning, SquadBar, Halved, StartDrm, StartStops, RedMp, WeaponPenalty",
               R + "ATemporaryCrewsPenaltiesFollowWhoMansTheVehicle; " + R + "ATemporaryCrewsOrdnanceFiresOnTheRedNumbersWithCaseHAndALowerBreakdown; " + B + "AnAbandonedPanzerIsCapturedAsTheCcphEndsAndCrewedByARussianCrewUnderTheTemporaryPenalties", "Pass 110",
               "a Temporary crew enters by re-crewing, with halved MP, the start dr (4 or less moves, 5 no move, 6 or more Bogs, +1 for a Russian captor), the captured MG's B# and ROF, red To Hit numbers with Case H, and red MP (task 65.8, increment 2i; ruling R65.22). Left: a capturing squad's free exchange for a HS (ruling R65.32; pass 110); the start dr in a Motion attempt (pass 105)."),
    "A26.12": ("built with a deviation", "Pass 65", "built with a deviation", "ScenarioA1CapturedVehicleRules.cs: Captured; ScenarioA1VictoryCalculator.cs (a captured vehicle's Control)",
               R + "AMannedCapturedVehicleHoldsControlForItsCaptor", "Stands by its ruling",
               "a manned vehicle captured by `vehicle-captured` holds Control for its captor; R24.5 is lifted for it (task 65.8, increment 2i; ruling R65.22). An R11.16 capture recorded before the pass, and PRC, keep R24.5's reading, which stands."),
    "B10.31": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: WithPrc (D5.31 with the `prc` fact)",
               R + "TheCeDrmTakesNeitherAPositiveTemNorHeightAdvantageWithTheFact; " + R + "OpenPrcClaimHeightAdvantageWhereTheInfantryTakeAnAfvsCover", "Pass 100",
               "under Infantry fire the CE DRM is not added to Height Advantage where the attack carries `prc`, and open PRC claim Height Advantage apart from the Infantry's AFV cover (task 65.1, increment 2b; fix Round A; ruling R65.7). Left: the Crest Line exception for a moving vehicle on the Vehicle Target Type, which no review checked (pass 100)."),
    "B13.43": ("not built", "Pass 65", "built", "ScenarioA1RiderRules.cs: TerrainBailOut, LocationBar; GamePlanner.Riders.cs: StepBailOut",
               R + "RidersBailOutInWoodsButNotAcrossARoadInBypassOrInAStream; " + R + "RidersMayNotBoardInWoodsButOnARoadInAStreamOrInBypass; " + B + "ARiderBailsOutAsItsTankEntersWoods; " + B + "RidersMayNotBoardATankInWoods", "",
               "Riders Bail Out before their vehicle enters woods, unless it crosses a road hexside, is in Bypass, or is in a stream, and may not board there (task 65.4, increment 2e; fix Round B; ruling R65.17)."),
    "C1.55": ("partly", "Pass 65", "partly", "ScenarioA1AreaCalculator.cs: AfvAttack (the TC, PRC Survival); ScenarioA1CrewSurvivalRules.cs: TaskCheck",
              B + "AMortarImmobilizesATankAndItsCrewFailsTheTcUnderHazardousMovement; " + B + "AnAirBurstWrecksAShockedHalftrackAndItsCrewSurvivesWithThePlusOne", "Pass 110",
              "a mortar's Immobilization takes the crew's TC, and PRC roll for Survival on its kill (task 65.7, increment 2h; ruling R65.21). Left: more vehicles than the column's highest KIA# stays refused (pass 110); a vehicle in Bypass and a mortar's Defensive First Fire at a hex with a vehicle (passes 100, 110); OBA (pass 150)."),
    "C3.32": ("partly", "Pass 65", "partly", "", "", "Pass 110",
              "not changed: A7.308's Random Selection is built for the IFT only, and the Infantry Target Type keeps `vehicles-beyond-kia` (increment 2k). Left: the note's items (pass 110)."),
    "C5.3": ("partly", "Pass 65", "partly", "", "", "Pass 110",
             "Passengers fire a LMG, a PIAT, or a DC by Mounted Fire (D6.72; increment 2c), and no Passenger fires ordnance. Left: Passenger ordnance (pass 110, with the PIAT)."),
    "D.6": ("partly", "Pass 65", "built", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1FireCalculator.cs: WithPrc, SpecificOnPrc; GamePlanner.PassengerTargets.cs: PrcOf",
            R + "AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack; " + R + "ATrucksPassengersTakeTheTemAndNoCeDrm; " + B + "AnIftAttackOnALoadedHalftrackPinsItsCePassengers", "",
            "a hit on a vehicle attacks its Vulnerable Passengers and Riders as its only target would be, with no FFNAM or FFMO, where the attack carries `prc` (task 65.1, increment 2b; rulings R65.5 to R65.7)."),
    "D.8": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: WithPrc, SpecificOnPrc",
            R + "AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack; " + R + "ASpecificCollateralAttackReachesThePassengersAfterAMissedTkDr; " + R + "AShockOrAnAbandonmentStopsOnlyTheCrewsSpecificCollateralAttack", "Pass 120",
            "Passengers and Riders take both kinds of Collateral Attack, and when crew and Passengers are stunned or shocked only the Riders are attacked (table note 1) (task 65.1, increment 2b; fix Round A; ruling R65.7). Left: mines (pass 120)."),
    "D.8A": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: SpecificOnPrc; ScenarioA1ArmorCalculator.cs: SpecificCollateral (PRC)",
             R + "ASpecificCollateralAttackReachesThePassengersAfterAMissedTkDr; " + R + "AShockOrAnAbandonmentStopsOnlyTheCrewsSpecificCollateralAttack; " + R + "NoCollateralAttackAfterAKillButEachPassengerRollsForSurvival", "Pass 110",
             "the Specific Collateral Attack reaches Passengers, Riders, and an unarmored vehicle's Vulnerable PRC, and an Abandonment, a Shock, or a Stun stops only the crew's (task 65.1, increment 2b; fix Round A; rulings R65.7). Left: a Gun's HEAT with no HE FP to step from (pass 110); an armed unarmored vehicle's crew (pass 175)."),
    "D.8B": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: WithPrc",
             R + "AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack; " + R + "HazardousMovementAndMudsCushionStayOffThePrc; " + B + "AnIftAttackOnALoadedHalftrackPinsItsCePassengers", "Pass 150",
             "the General Collateral Attack takes the attack's column and Original DR against every Vulnerable PRC of each vehicle in the Location, with TEM or the CE DRM, never both, and no Hazardous Movement or Mud cushion (task 65.1, increment 2b; fix Round A; ruling R65.7). Left: OBA (pass 150)."),
    "D2.42": ("partly", "Pass 65", "built", "ScenarioA1PassengerActionRules.cs: Multipliers",
              R + "ATrucksPassengersFireAtHalfAndAtAQuarterWhenItMoves; " + R + "ARiderFiresAtHalfAQuarterOnceItsAfvMovedAndNoSw; " + R + "ARiderFiresAtAQuarterInTheAfphOnceItsAfvMoved", "",
              "Passengers' and Riders' Motion Fire is halved as D6.1 and D6.22 have it (tasks 65.2, 65.4; increments 2c, 2e; fix Round A; ruling R65.10)."),
    "D3.3": ("partly", "Pass 65", "partly", "", "", "Pass 105",
             "not built: a Passenger's Bounding First Fire is refused (increment 2c; backlog 71.1). Left: Passengers and Riders (pass 105)."),
    "D3.4": ("partly", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: FireGroupBar, DirectorOffered",
             R + "D664GroupsPassengersWithTheirVehicleAndACeHalftracksWithInfantry; " + R + "APinnedPassengerLeaderDirectsNoAttack", "Pass 105",
             "a Passenger leader directs his vehicle's Passengers, and an Armor Leader directs Passengers only inside his halftrack's FG, decided in Rules (task 65.2, increment 2c; ruling R65.12). Left: the FG of a halftrack with its Passengers in Play, where vehicles fire alone (R25.7; pass 105)."),
    "D3.43": ("partly", "Pass 65", "partly", "ScenarioA1CrewActsRules.cs: RecrewBar; ScenarioA1VehicleProjection.cs: RecrewStands; GamePlanner.Ordnance.cs: LeavingCrew (armorLeader)",
              B + "ACrewReCrewsItsTankAPlayerTurnLaterAndItsArmorLeaderReturns; " + B + "ACrewThatFailsItsTcTakesTheStunPlusOneWithIt", "Pass 105",
              "a crew that leaves carries its Armor Leader, and he returns to the vehicle when that crew re-crews it; another crew brings none (task 65.6, increment 2g; fix Round B; ruling R65.20). Left: a re-crewing unit that leaves by `instance-moved` (a failed TC, a Recall's Abandonment, Survival) does not carry him (backlog 71.2; pass 105)."),
    "D3.5": ("partly", "Pass 65", "partly", "", "", "Pass 105",
             "a Passenger's Prep Fire marks its vehicle Prep Fired (D.3; ruling R65.27), and the vehicle fires its own weapons before its Passengers in the PFPh. Left: IFE (pass 105)."),
    "D5.1": ("partly", "Pass 65", "built", "ScenarioA1Definitions.cs: IsCrew; ScenarioA1CarrierRules.cs: LeavingCrew; GamePlanner.Ordnance.cs: LeavingCrew",
             R + "AVehicleCrewIsACrewAndASquadIsNot; " + R + "TheCatalogHoldsTheEightVehicleCrewsOfTheChart; " + R + "ACrewLeavesItsVehicleAsItsNationalitysVehicleCrew; " + B + "AKilledGermanTanksSurvivingCrewIsTheGermanVehicleCrew", "",
             "the crew that leaves is the nationality's vehicle crew (`asl:vehicle-crew`, catalog 1.18.0: the chart's eight, p. 695), placed by one member on every path; an older record keeps the infantry crew it placed (task 65.6, increment 2a; rulings R65.3, R65.19). Temporary crews are built (increment 2i), Armor Leaders and Inexperienced Crews by pass 55."),
    "D5.3": ("partly", "Pass 65", "partly", "ScenarioA1CarrierRules.cs: CrewExposed; ScenarioA1PrcRules.cs: Exposure", R + "ACarrierIsNeverBuAndItsCrewAndPassengersStayCeWhenStunned", "Pass 135",
             "a halftrack's Passengers are CE or BU with its crew, and a Carrier's crew and Passengers are always CE (tasks 65.1, 65.5; increments 2b, 2f; rulings R65.6, R65.18). Left: the building entry clause (pass 135); the hero Rider."),
    "D5.31": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: WithPrc; ScenarioA1PrcRules.cs: CeDrm",
              R + "TheCeDrmTakesNeitherAPositiveTemNorHeightAdvantageWithTheFact; " + R + "AnOtAfvsCrewTakesTheReducedCeDrmFromAboveBeyondTheRange", "Pass 105",
              "under Infantry fire the CE DRM is not added to a positive TEM or Height Advantage, R25.6's refusal of an AFV in positive TEM is lifted, and the DRM is reduced from an elevation beyond the range, where the attack carries `prc` (task 65.1, increment 2b; fix Round A; rulings R65.5, R65.7). Left: the CE DRM by Target Facing from Vehicle Notes and the Side facing for Residual FP (pass 105)."),
    "D5.311": ("partly", "Pass 65", "partly", "ScenarioA1FireCalculator.cs: UnprotectedCrewOutcome; ScenarioA1RoutCalculator.cs: RoutBeneath, RoutBeneathCrewText",
               R + "AnUnprotectedCrewThatBreaksIsPlacedBeneathAndOneEliminatedIsGone; " + R + "ABrokenCrewStaysInsideStopsItsAfvAndRoutsBeneathIt; " + R + "AirBurstsReachTheBuPassengersOfAnOtAfv; " + B + "AnAirBurstBreaksAnOtHalftracksCrewWhichRoutsBeneathAndAbandonsIt", "Pass 105",
               "an OT AFV's crew under Air Bursts is attacked as unarmored, and its BU Passengers are reached; a crew that breaks stays inside, Stops the AFV, and routs beneath in the RtPh, Abandoning it (increment 2k; fix Round A; rulings R65.26, R65.31). Left: an unarmored Target Facing and an AFV treated as unarmored (pass 105); ELR and Heat of Battle for the crew (pass 70)."),
    "D5.33": ("partly", "Pass 65", "built", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1PassengerActionRules.cs: MountedFire",
              R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf; " + R + "ACeHalftracksPassengersFireAtFullUnlessItMovedAndABuOnesDoNot", "",
              "Passengers are CE or BU with the crew, and the PRC fire (tasks 65.1, 65.2; increments 2b, 2c; rulings R65.6, R65.10)."),
    "D5.34": ("partly", "Pass 65", "partly", "GamePlanner.Ordnance.cs: LeavingCrew (stunRecovery); ScenarioA1CrewActsRules.cs",
              B + "ACrewThatFailsItsTcTakesTheStunPlusOneWithIt", "Pass 105",
              "a crew that leaves carries the vehicle's Stun +1 on every path (fix Round B). Left: Stun by a Sniper (pass 75) and by Falling Rubble (pass 135); a 1MT AFV's Recall on a Stun (pass 105)."),
    "D5.343": ("not built", "Pass 65", "partly", "ScenarioA1CapturedVehicleRules.cs: SmcFireBar, SmcMoveBar; GameProjector.cs: SmcCrewAfter",
               R + "ASmcCrewIsWoundedByAStunAndEliminatedByARecall; " + R + "AWoundedSmcTemporaryCrewWoundedAgainIsEliminated; " + B + "ASmcTemporaryCrewIsWoundedByAStunAndEliminatedByARecall", "Pass 105",
               "a SMC crew fires the AAMG only while CE, is Wounded by a Stun (eliminated if already Wounded), and is eliminated by a Recall (task 65.8, increment 2i; fix Round B; ruling R65.22). Left: the one-man turret (pass 105)."),
    "D5.41": ("partly", "Pass 65", "partly", "ScenarioA1CrewActsRules.cs: AbandonBar; GamePlanner.CrewActs.cs: PlanAbandon; ScenarioA1VehicleProjection.cs: AbandonStands",
              R + "ACrewAbandonsOnlyAnUnmovedUnfiredArmedVehicleInItsOwnMph; " + B + "AVoluntaryAbandonmentMalfunctionsOrWrecksTheVehicleAsTheCrewLeaves; " + R + "AShockedOrStunnedAfvsCrewMayNotAbandonIt; " + G + "TheCrewActsSectionNamesTheHexSendsItsChoicesAndClearsThem", "Pass 110",
              "Voluntary Abandonment (`abandon-vehicle`, `vehicle-abandoned`) in the crew's own MPh of an unmoved, unfired vehicle, its crew placed with FFNAM, with MG Removal and capture of the Abandoned vehicle (tasks 65.6, 65.8; increments 2g, 2i; fix Round B; ruling R65.20). Abandonment in Motion is allowed (the user's reading, backlog 71.1). Left: unhooking a towed Gun as the crew leaves (pass 110)."),
    "D5.411": ("not built", "Pass 65", "built", "ScenarioA1CrewActsRules.cs: SelfDestructionBar; GamePlanner.CrewActs.cs: PlanAbandon",
               R + "OnlyAnInherentCrewDestroysItsVehicleAndAWreckWaitsForThePassengers; " + B + "AVoluntaryAbandonmentMalfunctionsOrWrecksTheVehicleAsTheCrewLeaves", "",
               "a choice of Voluntary Abandonment: a non-burning wreck with no Crew Survival, or the MA and every MG malfunctioned; a wreck waits until the PRC unload (task 65.6, increment 2g; ruling R65.20)."),
    "D5.42": ("not built", "Pass 65", "partly", "ScenarioA1CrewActsRules.cs: RecrewBar, TemporaryCrew; GamePlanner.CrewActs.cs: PlanRecrew; ScenarioA1VehicleProjection.cs: RecrewStands; GameState.cs: AbandonedThisPlayerTurn",
              R + "AReCrewingIsBarredTheTurnTheCrewLeftAndForARecalledCrew; " + B + "ACrewReCrewsItsTankAPlayerTurnLaterAndItsArmorLeaderReturns; " + B + "TheProjectorRefusesAReCrewingInThePlayerTurnTheCrewLeftAndAnAbandonmentAfterFire", "Pass 105",
              "Infantry in the Location re-crews an Abandoned vehicle in its MPh for all its MF (`recrew-vehicle`, `vehicle-recrewed`, the crew role), keeping its id and its +1, barred in the Player Turn the old crew left and for a Recalled crew into its own AFV (task 65.6, increment 2g; ruling R65.20). Left: D5.43's failure on a pin or break while entering (pass 105); entering at a capture in the CCPh (pass 70)."),
    "D5.43": ("not built", "Pass 65", "partly", "ScenarioA1PassengerCalculator.cs: EmbarkingFfmo, LoadFailures; GamePlanner.Fire.cs (the load's failure)",
              R + "UnloadersTakeFfnamAndFfmoOnlyFromAnUnarmoredConveyance; " + R + "ALoaderFailsWhenTheAttackDestroysTheVehicleItBoards; " + B + "AFailedLoadLeavesThePinnedSquadOnFoot; " + B + "ALoaderSparedByTheAttackThatDestroysItsTruckStaysOnFoot", "Pass 105",
              "loaders and unloaders are Infantry targets of First Fire with FFNAM (`embarking`), and a load fails on a pin, a break, an elimination, or the vehicle's destruction (task 65.3, increment 2d; fix Round A; ruling R65.14). Left: re-crewing and hooking up (pass 105; pass 110 for the hook-up)."),
    "D5.5": ("partly", "Pass 65", "partly", "ScenarioA1CrewSurvivalRules.cs: TaskCheck, SecondCause; ScenarioA1AreaCalculator.cs: AfvAttack; ScenarioA1FireCalculator.cs: VehicleEffects (the Vehicle line's TC)",
             R + "AnImmobileTanksCrewTakesTheTcWhenAnOriginalFiveWouldHaveKilledIt; " + R + "APassedPossibleShockStillCallsForTheSecondCausesTcAndAMgsKillDoesNot; " + B + "AMortarImmobilizesATankAndItsCrewFailsTheTcUnderHazardousMovement", "Pass 120",
             "the TC after a mortar's or the IFT's Immobilization, the second cause on an Original TK DR of 5 at an Immobile target, and Hazardous Movement for a crew that leaves (task 65.7, increment 2h; fix Round B; ruling R65.21). Left: mines (pass 120); an unarmed truck's Inherent Driver still takes the ordnance TC (backlog 71.2)."),
    "D5.6": ("partly", "Pass 65", "partly", "ScenarioA1CrewSurvivalRules.cs: SurvivalDrm, CrewSurvival, Survival",
             R + "CrewSurvivalTakesPlusOneForABrokenStunnedOrShockedCrewAndNoneForAnAbandonedVehicle; " + R + "TheStunPlusOneCountsApartFromTheShockOrStunPlusOneForCrewSurvival; " + B + "AShockedTankKilledByAnAtShotRollsItsCrewSurvivalAtPlusOne; " + B + "ASurvivingRiderIsUnderHazardousMovementAgainstTheNextAttackOfThePhase", "Pass 105",
             "one Crew Survival member on every path, with Survival after an IFT kill of an armed vehicle, the +1 for Shocked or Abandoned, the Stun +1 apart, and survivors under Hazardous Movement with their MF spent (task 65.7, increment 2h; fix Round B; ruling R65.21). An Unconfirmed Kill rightly gives none (C7.42; the second referee). Left: the +1 for a broken crew, never set (backlog 71.2)."),
    "D5.7": ("not built", "Pass 65", "partly", "ScenarioA1CrewSurvivalRules.cs: BurnsRed", R + "ARedCsNumberBurnsOneDrHigherAndChangesNothingElse", "Pass 105",
             "the red CS# (`asl:cs-red`, catalog 1.18.0) gives -1 to the burn test on the ordnance, mortar, FT, MOL, DC, and vehicle CC paths (task 65.7, increment 2h; ruling R65.21); no catalog vehicle prints one. Left: the IFT's Vehicle line (pass 105)."),
    "D5.8": ("not built", "Pass 65", "built", "ScenarioA1CrewActsRules.cs: CrewFireCounter; GamePlanner.Ordnance.cs: LeavingCrew",
             R + "ALeavingCrewIsMarkedPrepOrFinalFireAfterItsVehicleFiredOrMoved", "",
             "a crew that leaves takes a Prep or Final Fire counter from its vehicle's fire counters, Motion, and movement; an older record keeps the conditions its `instance-created` recorded (task 65.6, increment 2g; ruling R65.20)."),
    "D6.1": ("partly", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: MountedFire, Multipliers, Allowed; ScenarioA1RoutCalculator.cs: RoutBeneath, AboardRout; GamePlanner.PassengerActions.cs",
             R + "ATrucksPassengersFireAtHalfAndAtAQuarterWhenItMoves; " + R + "ABrokenPassengerRoutsBeneathAStoppedVehicleAndMustWhenItsCrewIsGone; " + B + "ATrucksPassengerFiresAtHalfFpAndFreezesTheTruck; " + B + "ABrokenPassengerRoutsBeneathItsStoppedTruck; " + G + "TheRoutBeneathButtonNamesTheHexAndItsRecordSaysBeneath", "Pass 70",
             "Mounted Fire with its halvings, the SW a Passenger may fire, and rout beneath a Stopped vehicle, which a Passenger must when its crew is gone (task 65.2, increment 2c; rulings R65.10, R65.11). Left: Passenger CC in Play (pass 70)."),
    "D6.2": ("not built", "Pass 65", "partly", "ScenarioA1RiderRules.cs: Capacity, BoardsAsRiders, DateBar, CapacityBar; GamePlanner.Passengers.cs: PlanLoad (`riders`); GameProjector.cs: StepVehicle (the Rider role)",
             R + "RiderCapacityFollowsTheVehicleClass; " + R + "RidersAreRussianFrom1942AndEveryonesFrom1943; " + B + "RidersBoardAT34In1943AndReplay; " + B + "AGermanRiderIsRefusedIn1942; " + G + "TheRiderCheckboxIsLockedOnATankAndTheLoadSendsRiders", "Pass 175",
             "units board as Riders (`riders`), within a capacity by vehicle class (14 PP, a Carrier 8, another AFV one SMC, a truck none), Russians from 1942 and every nationality from 1943 (task 65.4, increment 2e; ruling R65.16). Left: a Rider's PP against a towed Gun's ammunition (pass 175); Cavalry and motorcycle Riders (passes 165, 175)."),
    "D6.21": ("not built", "Pass 65", "partly", "ScenarioA1RiderRules.cs: TerrainBailOut, TurretBailOut, BailsOutWhereItWas; GamePlanner.Riders.cs: StepBailOut",
              R + "RidersBailOutInWoodsButNotAcrossARoadInBypassOrInAStream; " + R + "ATurretedAfvsTcaChangeMakesItsRidersBailOut; " + R + "RidersBailOutWhereTheAfvWasUnlessItsTurretTurnsAsItEnters; " + B + "ARiderBailsOutAsItsTankEntersWoods; " + B + "ATankShotThatTurnsItsTurretMakesItsRiderBailOut", "Pass 135",
              "Riders Bail Out before woods, an orchard, a building, rubble, a Water Obstacle, or bocage, unless on a road hexside, in Bypass, or in a stream, and on every turret turn (task 65.4, increment 2e; fix Round B; ruling R65.17). Left: a friendly Vehicular-Sized Entrance (pass 135); a stream read from the terrain's name (pass 125); an entry from off board (pass 105); Bypass (pass 100)."),
    "D6.22": ("not built", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: Multipliers; ScenarioA1RiderRules.cs",
              R + "ARiderFiresAtHalfAQuarterOnceItsAfvMovedAndNoSw; " + R + "ARiderFiresAtAQuarterInTheAfphOnceItsAfvMoved", "Pass 70",
              "Riders use no SW and fire at half their inherent FP, a quarter once the AFV moved or is in Motion (task 65.4, increment 2e; fix Round A; ruling R65.17). Left: their +1 in CC in Play (pass 70); their FP in an OVR (pass 105)."),
    "D6.23": ("not built", "Pass 65", "partly", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1RiderRules.cs: CombatBailOut",
              R + "AShockedAfvsPassengersAreSparedAndItsRidersAreNot; " + B + "APinnedRiderBailsOutAndChecksItsSw", "Pass 105",
              "Riders are always Vulnerable, and a pin or break by the IFT makes them Bail Out (task 65.4, increment 2e; ruling R65.17). Left: a pin or break by an ordnance, mortar, FT, MOL, or DC hit (pass 105)."),
    "D6.24": ("not built", "Pass 65", "partly", "ScenarioA1RiderRules.cs: Check, WeaponFate, PrepFireCounter, LeavesBroken; GamePlanner.Riders.cs: AddBailOuts; GameProjector.cs (`rider-bailed-out`)",
              R + "ABailOutsNmcAndItsSwDrs; " + R + "ABrokenRiderIsNeverPinnedAndOneWithNoMoraleLevelIsRefused; " + B + "APinnedRiderBailsOutAndChecksItsSw; " + G + "TheRecordsSayABailOutsDiceAFailedLoadAndATemporaryStart", "Pass 70",
              "the event `rider-bailed-out`: the cause, the NMC, each SW's dr; the unit beneath with its movement ended, a fire counter if unbroken, and FFNAM (task 65.4, increment 2e; ruling R65.17). Left: Heat of Battle, leadership, Wound Severity, and ELR in the NMC, and a CC counter in an enemy Location (pass 70); Bypass (pass 100)."),
    "D6.31": ("partly", "Pass 65", "built", "ScenarioA1PassengerActionRules.cs: TransferBar; GamePlanner.SupportWeapons.cs",
              R + "PassengersInCcAPassedSwAndAnUnloadAfterFire", "",
              "a PRC passes a SW to another PRC of its vehicle or to friendly Infantry in the Location; a unit on foot passes none to a PRC (task 65.2, increment 2c; ruling R65.10)."),
    "D6.4": ("partly", "Pass 65", "partly", "ScenarioA1PassengerCalculator.cs: EndOfMoveLoadBar, BoardingIpcBonus, LeaveAboardBar, KeepsSwAboard, LoadFailures; ScenarioA1VehicleProjection.cs: BoardsAfterMove",
             R + "AStackBoardsAtTheEndOfItsMoveWithItsLeadersIpc; " + R + "ASwStaysAboardOnlyAHalftrackOrCarrierAndTimeAboardCounts; " + B + "ASquadBoardsAtTheEndOfItsMove; " + B + "AHalftrackKeepsTheLmgItsSquadLeavesAboard; " + B + "AFailedLoadLeavesThePinnedSquadOnFoot", "Pass 100",
             "boarding as the last step of a move, a leader's IPC, a SW left aboard a halftrack or Carrier, and FFNAM on the boarders with D5.43's failure (task 65.3, increment 2d; rulings R65.14, R65.15). Left: Bypass (pass 100)."),
    "D6.5": ("partly", "Pass 65", "partly", "ScenarioA1PassengerCalculator.cs: EmbarkingFfmo, InterimMembers, ConveyedAllowance, UnloadAfterFireBar; GamePlanner.Passengers.cs: PlanUnload (`interim`, `crew`)",
             R + "UnloadersTakeFfnamAndFfmoOnlyFromAnUnarmoredConveyance; " + B + "AnUnloadFromATruckInTheOpenTakesFfnamAndFfmo; " + B + "UnloadedUnitsMoveNextOnFourMfWithoutDoubleTime", "Pass 100",
             "FFNAM and FFMO on the unloaders, the interim move, a SW left aboard, no unload after the unit fired, and records of their own (task 65.3, increment 2d; rulings R65.14, R65.15). Left: Bypass (pass 100); a CC counter in an enemy Location (pass 70); the exact interleaving of the interim move (pass 105)."),
    "D6.61": ("partly", "Pass 65", "built", "ScenarioA1PrcRules.cs: Exposure, CeDrm",
              R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf; " + B + "ABuHalftracksPassengerDoesNotFire", "",
              "a BU halftrack's Passengers are attacked only from an elevation advantage beyond the range, with the CE DRM less one per level beyond, or by TPBF, and do not fire (tasks 65.1, 65.2; increments 2b, 2c; ruling R65.6)."),
    "D6.62": ("not built", "Pass 65", "built", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1FireCalculator.cs: WithPrc",
              R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf; " + B + "AnIftAttackOnALoadedHalftrackPinsItsCePassengers", "",
              "a CE halftrack's Passengers are Vulnerable with its crew; a broken, pinned, or shocked one is BU, and a failed MC breaks them, never Stuns (task 65.1, increment 2b; ruling R65.6)."),
    "D6.63": ("not built", "Pass 65", "built", "ScenarioA1PassengerActionRules.cs: MountedFire, Multipliers",
              R + "ACeHalftracksPassengersFireAtFullUnlessItMovedAndABuOnesDoNot; " + B + "ACeHalftracksPassengerFiresNoMmg", "",
              "an armored halftrack's CE Passengers fire, halved when it moved, the movement and the AFPh halving taken as one (task 65.2, increment 2c; fix Round A; ruling R65.10)."),
    "D6.631": ("not built", "Pass 65", "partly", "ScenarioA1CrewActsRules.cs: RemovedMgDefinition, RemovedCondition",
               R + "AnArmoredHalftracksMgOfThreeFpOrLessIsRemovedAsTheNationalitysLmg; " + R + "ACarriersCrewRemovesItsBmgAsAHalftracksDoes; " + B + "AHalftracksMgIsRemovedAsItsCrewAbandonsItAndReturnedWhenItReCrews", "Pass 105",
               "an option of Voluntary Abandonment: the crew of an armored halftrack or Carrier takes a MG of 3 FP or less as the nationality's LMG, the mount Disabled until it returns with a re-crew; a failed TC forbids it (task 65.6, increment 2g; fix Round B; ruling R65.20). Left: a heavier MG or a mortar, and a malfunctioned MG removed (pass 105)."),
    "D6.64": ("not built", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: FireGroupBar",
              R + "D664GroupsPassengersWithTheirVehicleAndACeHalftracksWithInfantry", "Pass 105",
              "D6.64's makeup is decided in Rules (task 65.2, increment 2c; ruling R65.12). Left: a vehicle's MG and its Passengers in one FG in Play, where vehicles fire alone (R25.7; pass 105)."),
    "D6.65": ("not built", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: FireGroupBar, DirectorOffered; Play.razor: FireLeaders",
              R + "D664GroupsPassengersWithTheirVehicleAndACeHalftracksWithInfantry; " + R + "APinnedPassengerLeaderDirectsNoAttack", "Pass 105",
              "a Passenger leader directs his vehicle's Passengers in Play; an Armor Leader's part is decided in Rules (task 65.2, increment 2c; ruling R65.12). Left: the halftrack's own FG with them (pass 105)."),
    "D6.651": ("not built", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: LeaderReaches", R + "APassengerLeadersReachIsHisVehicle", "Pass 70",
               "a Passenger leader's Rally, MC, TC, and LLMC reach is decided in Rules (task 65.2, increment 2c; ruling R65.12), and no Play path reads it. Left: wiring it, with Heat of Battle and LLMC for PRC (pass 70)."),
    "D6.66": ("not built", "Pass 65", "built", "ScenarioA1FireCalculator.cs: WithPrc (DM)", R + "ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf", "",
              "a broken BU Passenger is put under DM by an attack that could inflict a NMC on a CE one (task 65.1, increment 2b; ruling R65.9)."),
    "D6.7": ("partly", "Pass 65", "built", "ScenarioA1PrcRules.cs: Exposure; ScenarioA1FireCalculator.cs: WithPrc",
             R + "ATrucksPassengersTakeTheTemAndNoCeDrm; " + B + "AKilledTrucksPassengersRollForSurvival", "",
             "a truck's PRC are always Vulnerable, with TEM and no CE DRM, and take the Collateral Attack on the truck's DR (task 65.1, increment 2b; rulings R65.6, R65.7)."),
    "D6.71": ("partly", "Pass 65", "built", "ScenarioA1FireCalculator.cs: SpecificOnPrc", R + "ASpecificCollateralAttackReachesThePassengersAfterAMissedTkDr", "",
              "the Specific Collateral Attack on the PRC of an unarmored vehicle the hit leaves standing (task 65.1, increment 2b; ruling R65.7)."),
    "D6.72": ("not built", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: Allowed, OverrunShare",
              R + "APassengerFiresOnlyALmgAPiatOrADc; " + R + "AnOvrAddsItsPassengersShareTripledWithOneLeadershipModifier", "Pass 75",
              "a Passenger fires a LMG, a PIAT, or a DC only, and adds a quarter of its FP to an OVR (task 65.2, increment 2c; ruling R65.10). Left: a DC thrown or Placed by a Passenger (pass 75); Passenger CC (pass 70)."),
    "D6.8": ("not built", "Pass 65", "built", "ScenarioA1CarrierRules.cs: IsCarrier, CrewExposed",
             R + "ACarrierIsNeverBuAndItsCrewAndPassengersStayCeWhenStunned; " + B + "ABrenCarrierIsNeverButtonedUpAndItsStunnedCrewStaysExposed", "",
             "the note was stale: the Goodwood work added `campaign-british-bren-carrier`, class carrier since catalog 1.18.0. Carriers are built (task 65.5, increment 2f; ruling R65.18)."),
    "D6.81": ("not built", "Pass 65", "built", "ScenarioA1CarrierRules.cs: PassengerCapacity; ScenarioA1RiderRules.cs: Capacity",
              R + "ACarrierCarriesFourPassengerPpAndEightRiderPp", "",
              "the note was stale (the Goodwood Bren carrier). 4 Passenger PP (the catalog's 5 corrected to 4 in 1.18.0), none with a mortar, FT, or 6 FP AAMG as MA, and 8 Rider PP (task 65.5, increment 2f; ruling R65.18)."),
    "D6.82": ("not built", "Pass 65", "partly", "ScenarioA1CarrierRules.cs: InherentHalfSquad, LeavingCrew",
              R + "ACarriersCrewLeavesAsAHalfSquadUnlessItHasAFtOr76mmMortarOrTowsAGun; " + B + "AKilledBrenCarriersSurvivingCrewIsABritishHalfSquad", "Pass 70",
              "the note was stale (the Goodwood Bren carrier). Its crew leaves as the nationality's 2-4-7 HS unless it has a FT or a 76mm mortar or tows a Gun (task 65.5, increment 2f; ruling R65.18). Left: the HS recombining without a TC or leader (pass 70); an elite HS and \"specified as towing ordnance\", which wait for a card."),
    "D6.83": ("not built", "Pass 65", "built", "ScenarioA1CarrierRules.cs: CrewUnloadBar; GamePlanner.Passengers.cs: PlanUnload (`crew`)",
              B + "ABrenCarriersInherentHalfSquadUnloadsAndAbandonsIt; " + R + "ACarriersCrewRemovesItsBmgAsAHalftracksDoes; " + G + "TheCarrierCrewCheckboxUnloadsItsHalfSquadAndTheRecordSaysItIsAbandoned", "",
              "the note was stale (the Goodwood Bren carrier). The inherent HS's quicker unload Abandons the Carrier, and its crew may Remove the MG as D6.631 has it (task 65.5, increment 2f; fix Round B; ruling R65.18)."),
    "D6.84": ("not built", "Pass 65", "built", "ScenarioA1CarrierRules.cs: ButtonUpBar, CrewExposed",
              R + "ACarrierIsNeverBuAndItsCrewAndPassengersStayCeWhenStunned; " + B + "AStunnedBrenCarriersCrewTakesTheCollateralAttack", "",
              "the note was stale (the Goodwood Bren carrier). A Carrier is never BU, and its crew, HS, and Passengers are always CE and Vulnerable, stunned or shocked included (task 65.5, increment 2f; ruling R65.18). The page's Starshell list still reads the raw condition (backlog 71.2)."),
    "D6.9": ("built with a deviation", "Pass 65", "built", "ScenarioA1CrewSurvivalRules.cs: Survival; GamePlanner.PassengerTargets.cs: SurvivorEvents; GameProjector.cs: KeepPassengers (the recorded Survival)",
             R + "NoCollateralAttackAfterAKillButEachPassengerRollsForSurvival; " + B + "AKilledTrucksPassengersRollForSurvival", "",
             "each Passenger and Rider rolls its own Survival DR against the CS# or cs#, +1 if broken, survivors beneath the wreck and an unheld SW eliminated; R26.2's elimination stands only for a destruction recorded before the pass (task 65.1, increment 2b; rulings R65.5, R65.9)."),
    "D7.11": ("partly", "Pass 65", "partly", "ScenarioA1PassengerActionRules.cs: OverrunShare, OverrunPassengers, OverrunFirepower",
              R + "AnOvrAddsItsPassengersShareTripledWithOneLeadershipModifier; " + R + "OnlyACeArmoredHalftracksOrCarriersPassengersAddHalfToAnOvr; " + B + "ALoadedHalftracksOvrAddsHalfItsPassengersFpTripledAndTheirLeader", "Pass 105",
              "a CE armored halftrack's or Carrier's Passengers add half their FP and other Passengers a quarter, named at the declaration (`passengers`; task 65.2, increment 2c; fix Round A; ruling R65.13). Left: Rider FP, FT, IFE, the RMG exclusion (pass 105)."),
    "D7.16": ("partly", "Pass 65", "built", "ScenarioA1PassengerActionRules.cs: OverrunLeadership",
              R + "AnOvrAddsItsPassengersShareTripledWithOneLeadershipModifier", "",
              "one leadership modifier from the Armor or the Passenger leader (task 65.2, increment 2c; ruling R65.13)."),
    "D8.1": ("partly", "Pass 65", "partly", "", "", "Pass 120",
             "the crew's TC after an IFT or mortar Immobilization (D5.5's row). Left: mines (pass 120); the flip of an unarmed unarmored immobilized vehicle once unloaded."),
    "D8.11": ("not built", "Pass 65", "built", "ScenarioA1CrewSurvivalRules.cs: SecondCause",
              R + "AnImmobileTanksCrewTakesTheTcWhenAnOriginalFiveWouldHaveKilledIt", "",
              "a Direct Fire ordnance hit on an Immobile target takes the TC where an Original TK DR of 5 would have burned, eliminated, or automatically Shocked it (`secondCause`; task 65.7, increment 2h; ruling R65.21)."),
    "D8.22": ("not built", "Pass 65", "built", "ScenarioA1CrewSurvivalRules.cs: SecondCause",
              R + "AnImmobileTanksCrewTakesTheTcWhenAnOriginalFiveWouldHaveKilledIt", "",
              "D5.5's second clause is built for a bogged target (task 65.7, increment 2h; ruling R65.21)."),
    "D9.1": ("partly", "Pass 65", "built", "ScenarioA1PrcRules.cs: Exposure",
             R + "AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack; " + R + "AirBurstsReachTheBuPassengersOfAnOtAfv", "",
             "an OT AFV's Passengers and Riders are attacked as its crew is (task 65.1, increment 2b; fix Round A; ruling R65.6)."),
    "D9.2": ("partly", "Pass 65", "built", "ScenarioA1CrewSurvivalRules.cs: Survival; GamePlanner.PassengerTargets.cs: SurvivorEvents",
             R + "NoCollateralAttackAfterAKillButEachPassengerRollsForSurvival; " + B + "AnAirBurstWrecksAShockedHalftrackAndItsCrewSurvivesWithThePlusOne", "",
             "Brew Up: a kill that does not burn rolls Survival for the crew and the PRC on every destruction path but CC, the IFT kill included (tasks 65.1, 65.7; increments 2b, 2h; rulings R65.9, R65.21)."),
    "D10.1": ("partly", "Pass 65", "partly", "ScenarioA1WreckRules.cs: AttackedAsVehicle, WreckKill, WreckLineResult, BlazeId, CloseCombatBar",
              R + "AWreckBurnsOnABurnAndNothingElseTouchesIt; " + B + "AT34BurnsAPanzerWreckThatAnEliminatingDrLeftAsItWas; " + B + "TwoSquadsBurnATruckWreckOnTheIftsVehicleLine", "Pass 110",
              "a non-burning wreck is attacked as its vehicle by ordnance on the Vehicle Target Type, the IFT's Vehicle line, and a FT or MOL, where only a Burn counts and places a Blaze; its TCA is its VCA (task 65.9, increment 2j; ruling R65.23). Left: the Infantry Target Type, a mortar, a DC, and the IFT's other limits (pass 110); CC at a wreck (pass 70)."),
    "D10.5": ("not built", "Pass 65", "built", "ScenarioA1WreckRules.cs: ScroungeBar, Scroungeable, Succeeds, Taken; GamePlanner.Scrounging.cs: PlanScrounge; ScenarioA1VehicleProjection.cs: ScroungeStands",
              R + "ScroungingTakesAsManyFunctioningSwAsItsDrFromAWreckOrAnAbandonedVehicle; " + B + "ARussianSquadScroungesAPanzerWrecksBmgAsAGermanLmgAndTheWreckIsScroungedOnce; " + G + "TheScroungingSectionNamesTheHexSendsTheUnitAndClearsItsPick", "",
              "Scrounging in the RPh (`scrounge`, `vehicle-scrounged`): a dr of 1 to 3 takes as many qualifying SW by Random Selection, with no leadership modifier (the triangle, A.10; ruling R65.33 as revised); the unit and its SW TI and the vehicle Scrounged (task 65.9, increment 2j; ruling R65.23)."),
    "D10.51": ("not built", "Pass 65", "built", "ScenarioA1WreckRules.cs: Scroungeable",
               R + "ScroungingTakesAsManyFunctioningSwAsItsDrFromAWreckOrAnAbandonedVehicle", "",
               "only a BMG that is not fixed, an AAMG, or a mortar MA qualifies (task 65.9, increment 2j; ruling R65.23)."),
    "D10.52": ("not built", "Pass 65", "built with a deviation", "ScenarioA1WreckRules.cs: ScroungedNationality, ScroungedDefinition, ScroungedDismantled",
               B + "ARussianSquadScroungesAPanzerWrecksBmgAsAGermanLmgAndTheWreckIsScroungedOnce", "Pass 110",
               "a scrounged MG becomes the nationality's LMG. The deviation: assembled, since a LMG has no dm form (ruling R65.30). Left: the American exemption from captured-use penalties and a mortar MA's dm form (pass 110)."),
}

# Rows of other passes the pass touched: (old status, old pass, note sentence); status and pass kept unless a new status is given.
OTHER = {
    "A7.7": ("partly", "Pass 110", None, "not built: an Immobile vehicle's Vulnerable PRC are not Encircled with its Location; it stays with this row's pass."),
    "A26.213": ("built", "", None, "a Carrier's inherent HS counts its two VP while inherent, and one once it leaves, with no double count (`LeftItsVehicle`; task 65.5, increment 2f). The note's \"No Carriers exist\" was stale since the Goodwood Bren carrier."),
    "C3.332": ("built", "", None, "an OT AFV's crew under Air Bursts is attacked as unarmored (D5.311; increment 2k; rulings R65.26, R65.31)."),
    "C3.72": ("partly", "Pass 150", None, "Air Bursts against an OT AFV's crew are built (D5.311; increment 2k). Left: the FFE Critical Hit (pass 150)."),
    "C7.345": ("built", "", None, "a mortar's Immobilization takes the crew's TC (D5.5; increment 2h; ruling R65.21)."),
    "D3.44": ("partly", "Pass 105", None, "the halftrack's FG with its Passengers under an Armor Leader is decided in Rules (D6.64, D6.65; increment 2c) and not formed in Play, where vehicles fire alone (pass 105)."),
    "A11.1": ("partly", "Pass 70", None, "a crew that mans no Gun is advanced against and fights in CC as ordinary Infantry (A1.123; increment 2k; ruling R65.29); a Gun's crew keeps R24.3's refusal."),
    "A11.52": ("partly", "Pass 70", None, "an Abandoned AFV is captured under A21.2 and a captured vehicle is used by a Temporary Driver or crew (task 65.8, increment 2i). Left: capturing Passengers (pass 75)."),
    "D.3": ("built", "", "built with a deviation", "a Passenger's Prep Fire marks its vehicle Prep Fired, which also bars the vehicle's own fire later in that PFPh, where D.3 bars only its MPh movement and its MPh and AFPh fire (ruling R65.27, the deviation)."),
    "D2.401": ("partly", "Pass 105", None, "a Passenger's time aboard counts toward a later Motion attempt against it (increment 2d)."),
    "A4.42": ("partly", "Pass 75", None, "a leader's IPC counts toward a vehicle's PP when his stack boards (increment 2d; ruling R65.15)."),
    "D5.341": ("partly", "Pass 105", None, "a Recalled crew may not re-crew its own AFV (D5.42; increment 2g; ruling R65.20); an Armor Leader is not carried by a Recall's leaving crew."),
    "D7.12": ("partly", "Pass 105", None, "an OVR attacks an enemy vehicle's Vulnerable PRC in the overrun Location (increment 2c; ruling R65.13)."),
    "D9.3": ("partly", "Pass 100", None, "not built: units Abandoning, Surviving, unloading, or Bailing Out still take the AFV's or wreck's +1 (backlog 71.1)."),
    "A9.74": ("partly", "Pass 105", None, "no dr is made for a SW held by a PRC a KIA eliminates (backlog 71.1)."),
    "C7.42": ("built", "", None, "the second referee confirmed that an Unconfirmed Kill's wreck gives no Crew Survival; its PRC are eliminated with it (the user's reading, backlog 71.1)."),
}


def cells(line):
    return [c.strip() for c in line.split("|")[1:-1]]


def build(c):
    return "| " + " | ".join(c) + " |"


def add(old, new):
    if not new:
        return old
    return new if not old else old + "; " + new


raw = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in raw
lines = raw.split("\n")
index = {}
for i, line in enumerate(lines):
    if line.startswith("| "):
        c = cells(line)
        if len(c) == 9 and (c[0] in ROWS or c[0] in OTHER):
            assert c[0] not in index, ("twice", c[0])
            index[c[0]] = i
missing = [k for k in list(ROWS) + list(OTHER) if k not in index]
assert not missing, missing
changes = []
for key, (old_status, old_pass, status, where, test, new_pass, note) in ROWS.items():
    i = index[key]
    c = cells(lines[i])
    assert c[3] == old_status and c[6] == old_pass, (key, c[3], c[6])
    assert "\u2014" not in note
    c[3] = status
    c[4] = add(c[4], where)
    c[5] = add(c[5] if c[5] != "none found" else "", test)
    c[6] = new_pass
    c[7] = (c[7] + " " if c[7] else "") + N + note
    lines[i] = build(c)
    changes.append((key, old_status, status, old_pass, new_pass))
for key, (old_status, old_pass, status, note) in OTHER.items():
    i = index[key]
    c = cells(lines[i])
    assert c[3] == old_status and c[6] == old_pass, (key, c[3], c[6])
    if status:
        c[3] = status
    c[7] = (c[7] + " " if c[7] else "") + N + note
    lines[i] = build(c)
    changes.append((key, old_status, c[3], old_pass, c[6]))
for change in changes:
    print(*change, sep=" | ")
print("rows", len(changes))
if "--check" not in sys.argv:
    io.open(P, "w", encoding="utf-8", newline="").write("\n".join(lines))
    print("written")
