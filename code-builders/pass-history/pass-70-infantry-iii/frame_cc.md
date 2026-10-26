# Frame agent C: Close Combat (70.6, 70.7, and the CC rows moved in)

Read-only agent, launched 11:54, reported 12:04 (2026-10-25). Its report, kept as it came; the design's rulings judge it.

## Q1. 70.6: Withdrawal and CC with a Gun's crew

Built: `GamePlanner.CloseCombat.cs:637-640` `WithdrawalDestinations` (the six ground-level neighbours, `Neighbors` :1195-1203, Level 0); refuses a Laden unit; bars any Location with an uncaptured enemy unit, concealed or hidden included. `WithdrawalTires` (:656) CX. `ScenarioA1CloseCombatRules.WithdrawalDestination/WithdrawalTires` (:167-171). Bars `WithdrawalPortageBar` (:481), `WithdrawalBar` (:486), `MustWithdraw` (:493).

Missing (inventory A11.21): dropping SW above the IPC (refused today); withdrawal into a concealed enemy's Location (A11.21, p. 73: the withdrawer is eliminated automatically, Random Selection of the enemy loses "?"; p. 74 example places HIP units); other building levels. The bar leaks: refusal and page list (Play.razor:4371, :4548, :4603) count enemy units the side cannot see.

Proposal: bar only by a Known enemy from the side's view; into a concealed enemy: eliminated, Random Selection loses "?", HIP placed; Dummy-only: Dummies removed; a `drops` declaration leaves excess SW unpossessed. Other levels to pass 135.

A Gun's crew is refused everywhere: advance `ScenarioA1AdvanceCalculator.CrewBar` (called GamePlanner.CloseCombat.cs:195); charge `ChargeBarred` (ScenarioA1CloseCombatRules.cs:285); required CC `RequiredUnit` (:203); berserk vehicle attack `BerserkOwingVehicleAttack` (ScenarioA1VehicleCloseCombatRules.cs:76); package `UnitsOutside` (ScenarioA1CloseCombatCalculator.cs:270-273, `asl.a1.cc.unit-outside` for `Manning == true`). No rule in A11 or C (pp. 160-199) bars CC with a Gun's crew: inherent FP (A1.123), counts as a squad for stacking (A5.5), the Gun never used in CC (A11.13, p. 72); each Gun of a unit eliminated in CC is eliminated on a colored dr of 1 then a dr at or below the black Kill Number, otherwise stays unpossessed. Proposal: retire the five gates; Gun loss through `CloseCombatUnit.Guns`, gated for old records.

## Q2. 70.7: Infantry against Infantry beside a vehicle, Ambush, Street Fighting

Today: `PlanCloseCombat` refuses Infantry CC in a vehicle's Location (`VehiclePresentBar`, GamePlanner.CloseCombat.cs:425); only `PlanSequentialCloseCombat` (GamePlanner.VehicleCloseCombat.cs:257). `SidesWithAttacks` (ScenarioA1VehicleCloseCombatRules.cs:30-47) counts Infantry only against a vehicle. `AmbushDueCandidate` (ScenarioA1CloseCombatRules.cs:255) gives no Ambush where a vehicle stands. `LiveCloseCombat.Units` (:28) takes every active unit, vehicles included.

Missing: Infantry-against-Infantry attacks in the alternating sequence (A11.31; H3 example p. 74); the Ambush drm for a vehicle (+2 Vehicle, +1 per BU or Stunned AFV, A11.4 table p. 73); A11.34 precedence (ambusher resolves all its attacks first); the Ambush -1/+1 in vehicle CC both ways (A11.51, I5 example).

Proposal: a "Sequential" round kind (one attack by the side named next, no simultaneous reply); `LiveCloseCombat.Units` drops vehicles; `SidesWithAttacks` counts Infantry facing enemy Infantry; one attack per unit per CCPh; attacked by Infantry and by a vehicle counted apart (A11.12 EXC).

Street Fighting (A11.8, p. 76): a vehicle in a road hex ADJACENT to building hexes on both sides of the road; Infantry advancing from the ground level of those buildings get -1 on CC vs the vehicle, the vehicle +1 on them; not with an actual Ambush; afterwards they return or stay (not if broken, pinned, or on Wire). Nothing exists. Smallest build: a Maps query (road hex flanked by buildings), `AdvanceRecord.From` (state only), the -1/+1 in vehicle facts, a return step at the Location's close. Reaction Fire from the buildings: D7.211, pass 105. Bypass: pass 100.

## Q3. A11.52 Passengers, A18.12 vs a vehicle, PRC in CC

A11.52 (p. 74): capturing an unarmored stationary vehicle with Passengers needs every enemy Personnel there eliminated or captured and every Passenger captured; ordinary Infantry CC, -2 on attacks at those Passengers with the capture's +1, +2 on the Passengers' attacks at captors; all captured, the vehicle is captured (`vehicle-captured`, `VehicleCaptured.CloseCombat`).

A18.12 (p. 85): `ScenarioA1VehicleCloseCombat` gains a `leaderCreation` dr after a MMC's Original 2 (the Infantry calculator's table drm); the created leader adds his leadership to the Original 2, +1 CCV only if the attack had no SMC (A11.5 limits two units); the Unlikely Kill still follows; Play writes the leader's `instance-created`; `VehicleRolls` accepts `leaderCreation`.

PRC: `ScenarioA1PassengerActionRules.CloseCombat` (:318) gives a role: Apart (a Rider, Infantry-vs-Infantry, +1 attacking, -1 defending, A11.611 p. 75); Attacks (CE halftrack Passenger joins the vehicle's attack, A11.62); Idle. New finding: `GameProjector.InMelee` (:794-805) groups `state.Units` by `state.Location`, which follows containment, so a vehicle's PRC and inside crew are probably marked `asl:melee` at the CCPh's end; A11.71 (p. 76) holds only the PRC of an Immobile vehicle. Proposal: `HeldInMelee` into `MeleeCandidate`, gated by a fact on the CCPh-ending `phase-changed`.

## Q4. The CC counter

`asl:melee` (GameTypes.cs:230), placed by the projector at the CCPh's end (:577, `InMelee`), by the OVR CC plan (GamePlanner.CloseCombat.cs:600-611), cleared by `KeepMelee` (:823). `asl:cc-reaction` (D7.21) is the only CC-type counter. D6.5 (p. 205) and A7.211 (p. 55): the CC counter shows the units are not held in Melee; today's default gives that result. Proposal: no stored condition; a record line and a view-derived map badge (0:15).

## Q5. Wreck CC, Carrier HS, entry at a capture

| Item | Rule | Proposal |
|---|---|---|
| D10.1 (p. 210) | A non-burning wreck attacked as its vehicle; only a burning result matters (Blaze) | Move to pass 110 with the rest of D10.1 (inventory 2423 already Pass 110) |
| D6.82 (p. 206) with A1.32 (p. 45) | A1.32 names "Carrier HS" among units needing no leader to Recombine | An absent-safe `CrewOf` on the leaving crew's `instance-created` (GamePlanner.Ordnance.cs:1484); `NeedsNoLeader` gains an input. 0:20 |
| D5.42 (p. 204) | EXC CCPh/A21.2: the captor may enter the captured vehicle as crew in the CCPh | `PlanRecrew` in the CCPh for a vehicle captured this CCPh, without the same-Player-Turn bar. 0:20 |

## Q6. The page

`VehicleCloseCombatPanel.razor`: an Infantry-vs-Infantry attack builder (attackers, defenders, capture-Passengers option); Riders as targets hex-first; Street Fighting "return to [F3]"; drop the help text "CC between Infantry there is not built" (:7). Withdrawal and infiltration selects from the side's view with a drop-SW choice. `VehicleCcRows` (Play.razor:4005-4016) reads `state.At` filtered by `SeenOnly`. A Gun's crew appears in `CloseCombatAttackBuilder`.

## Q7. Facts

Gated: `CloseCombatUnit.Guns`; `VehicleCloseCombatFacts.LeaderCreation` (from the record, as `Capture` and `Unmanned`, LiveCloseCombat.cs:381-382); `AmbushFacts` per-side vehicle drm, null where no vehicle; a `PrcMelee` fact on the CCPh-ending `phase-changed`; `NewInstance.CrewOf`. Not gated: Ambush and Street Fighting in vehicle CC (none before); lifted bars (refusals before); `SidesWithAttacks` (the projector keeps recorded `Next` and `Closed`, GameProjector.cs:1084-1085, 1104-1105).

## Q8. Blast radius

Gun's crew: BacklogPass65Tests.AnAdvanceOntoAGunsCrewIsStillRefused; OrdnanceStepsTests.TableFindingsACrewNeitherStrandsTheGameNorFiresFromMelee; OrdnanceStepsTests.ABerserkChargeIntoACrewsLocationEndsInPlace; ScenarioA1CloseCombatRulesTests.AStepIntoACrewsLocationLeavesTheChargeUndecided; ScenarioA1CloseCombatRulesTests.TheRequiredCcUnitIsTheBerserkOneThenTheReinforcingOne; ScenarioA1Pass65RulesTests.ACrewThatMansNoGunIsAnOrdinaryTargetAndAGunsCrewKeepsRulingR83. Infantry beside a vehicle: ScenarioA1CloseCombatRulesTests.TheDueLinesAndTheBarredCharge; ScenarioA1CloseCombatRulesTests.TheSidesWithVehicleCcAttacksLeftAndTheirOrder; BacklogPass11Tests.InfantryAdvanceOnAnAfvAfterAPaatcAndFightItInSequentialCc. Withdrawal: BacklogPass5Tests.AUnitCarryingMoreThanItsIpcMayNotWithdrawAndABerserkUnitLosesItsCx; CloseCombatStepsTests.AWithdrawalMayEnterALocationHeldByAFriendlyGuard; PlayPageCloseCombatTests.AUnitHeldInMeleeWithdrawsFromThePage. Carrier HS: ScenarioA1Pass40RulesTests.GuardsAndUnarmedUnitsNeedNoLeaderAndAGuardSquadDeploysAtWill. Page: CloseCombatComponentTests.VehicleCloseCombatOffersEachLocationItsOwnUnits; PlayPageVehicleTests.AVehicleCloseCombatAttackNamesOnlyItsOwnLocationsUnits. Wreck CC: ScenarioA1Pass65RulesTests.AWreckBurnsOnABurnAndNothingElseTouchesIt.

## Q9. Estimates

70.6 (plan 0:40): 1:15 (Gun's crew CC with A11.13 0:35, drop SW 0:10, concealed destination 0:25, page 0:05); D6.82 0:20 more.
70.7 with the moved rows (plan 1:20): 5:00 (Inf vs Inf 1:00, Ambush with a vehicle 0:35, Street Fighting 0:40, A11.52 0:35, A18.12 0:25, PRC in CC and the Melee fix 0:40, CC counter 0:15, D5.42 0:20, page 0:30).
Move out: D10.1 wreck CC to pass 110; withdrawal to other levels to pass 135; Street Fighting by Reaction Fire and in Bypass (passes 105, 100). Next to move if the pass must shrink: A11.52's capture of Passengers, to pass 105.

Not read: `CloseCombatPanel.razor` internals, `ScenarioA1VehicleCloseCombat.cs` beyond a grep, `CloseCombatEffects`, the pass 65 design and code map, the guidelines, Prompt I beyond a grep, the rulebook illustrations. The PRC-in-Melee finding is from the code, not a test.
