# Frame agent A: morale outside fire, and rout (70.1, 70.2)

Read-only agent, launched 11:54, reported about 12:10 (2026-10-25). Read pp. 55, 65 to 69, 83, 85 to 87, 200, 203 to 206. Kept as it came, condensed only in layout; the design's rulings judge it.

## 1. Every MC/TC outside Fire's attack resolution and the Rally calculator

HoB = Heat of Battle, Ldr = leadership DRM, ELR = A19.13 Replacement, Wd = A17.11 Wound Severity dr, LLMC = after a leader's loss.

| Path | Where | HoB | Ldr | ELR | Wd | LLMC | PRC / Unarmed |
|---|---|---|---|---|---|---|---|
| Rout Interdiction NMC | GamePlanner.Rout.cs:685-737; RoutCalculator.cs:783-793; `RoutInterdicted(..., Drm 0)` | none (owed on an Original 2, +1 broken) | always 0 | n/a (broken immune, A19.11) | yes (pass 35) | none | neither |
| Bail Out NMC | GamePlanner.Riders.cs:104-171; `ScenarioA1RiderRules.Check` (RiderRules.cs:181); GameProjector.cs:2644 | none, none owed (PRC exempt, A15.1) | none | none | missing, and it crashes: Riders.cs:164 passes a null severity, a SMC Rider's "reduced" throws at RoutCalculator.cs:312 | none, none owed (A10.2 EXC Riders) | PRC always |
| D5.311 crew | FireCalculator.cs:1309 `UnprotectedCrewOutcome`, called 2476 | none owed (crews, A15.1) | none, rightly (D3.44, p. 200) | none owed (crews, A19.11) | n/a | n/a | crew; a broken crew checked as if unbroken |
| PAATC | GamePlanner.Overrun.cs:229-335 | n/a | yes | n/a | n/a | n/a | neither |
| Mopping Up casualty (dr) | GamePlanner.MoppingUp.cs:185-219 | n/a | in the dr (A12.154) | n/a | yes | missing | Unarmed excluded |
| Sniper | ScenarioA1Sniper.cs:166; GamePlanner.Snipers.cs | n/a | n/a | n/a | yes | missing (backlog 75.1) | Unarmed eligible |
| Rally Fate | RallyCalculator.cs:303-360 | in package | in package | n/a | yes | missing after a mortal wound | no |
| PF/ATR firer's CR | GamePlanner.Ordnance.cs:937-955; OrdnanceEventRules.cs:104 | n/a | n/a | n/a | yes | missing; no LLTC when a leader's PF Check breaks him | no |
| Infantry OVR NTC | GamePlanner.InfantryOverrun.cs:29-105 | n/a | leader for the stack (A4.15) | n/a | n/a | n/a | Unarmed recapture: none |
| Concealed SMC OVR NTC | GamePlanner.cs:1490-1520 | n/a | TEM only | n/a | n/a | n/a | no |
| Deployment NTC | GamePlanner.SupportWeapons.cs:315-340 | n/a | yes | n/a | n/a | n/a | an Unarmed squad needs no leader |
| Failure to Rout elimination | GamePlanner.Rout.cs:923 | n/a | n/a | n/a | n/a | missing when a leader is eliminated | |
| SMC crew wounded by a Stun | GameProjector.cs:4148-4163 | n/a | n/a | n/a | missing; a second wound eliminates | n/a | |

Proposal: one Rules member `ScenarioA1MoraleCheck.Resolve(MoraleCheckFacts, ScenarioA1FireReference) -> MoraleCheckVerdict`, built on the Fire calculator: an internal `ScenarioA1FireCalculator.ResolveCheck` running the existing `Resolution.Apply(kind, targets)` then `LeaderLoss()`, no IFT roll; reuses MoraleOutcome, HoB, ELR, wounds, the LLMC cascade, the prisoner, berserk, hero, PRC branches. Facts: the check (NMC, nMC, PTC, or none for a lost leader alone); subjects as `FireTarget` with optional `Unarmed`, `PrcRole`, `Bystander`; the lost leaders (id, ML at loss, leadership, eliminated or broke); `PinsBroken` (A7.8); rolls (`FireRolls`), Choices, TargetSideElr, NoQuarter, InMelee. Verdict `FireUnitEffect[]`; Play writes through Fire's `EffectEvents` (GamePlanner.Fire.cs:1465); one new `morale-check-resolved` record keeps facts and verdict. `Unarmed` also in Fire, read by `ScenarioA1HeatOfBattle.Subject` and `ElrImmune` (A15.1, A19.11).

Gated (an old result would change): Bail Out (optional `Drm` and `Elr` on `RiderBailedOut`, `CheckAgrees` extended only when present); D5.311 (a `FireVehicle` fact for a crew already broken); Unarmed and PRC leadership in Fire (optional facts on the attack). New: everything appended after an existing record (HoB after an Interdiction NMC's Original 2; every LLMC). The projector checks Interdiction with dice, Drm, Morale, Result (GameProjector.cs:3210); `Drm` exists.

Rulings needed: a HoB that rallies or makes berserk a routing unit ends its rout where it stands; a leader already in the Open Ground hex a unit routs into gives no DRM, only the Voluntary Rout leader (A10.711).

## 2. Casualty Reduction and wounds

Built with the Wound Severity dr: Fire, CC, vehicle CC (VehicleCloseCombat.cs:596-613), Rally Fate, Sniper, Interdiction, Mopping Up, the PF firer. Missing: the Bail Out (throws); the SMC crew Stun wound in the projector (D5.34 with A17.11; no dr, a second wound eliminates against "no additional penalty"): to the backlog (75.3 or 105).

## 3. ELR and Unarmed

`ElrImmune` FireCalculator.cs:69 (heroes, crews, Commissars); `ElrOf` 4536, `Replace` 4556, `ReduceBeyondElr` 4606; `GameState.ElrOf` GameState.cs:48. Unarmed is `asl:unarmed` (GameTypes.cs:308), set by capture (GameProjector.cs:4454, 4489, 4698), on freed prisoners, on Unarmed squads that Deploy; cleared by `GamePlanner.FreedSmc`. Fire handles prisoners through `GuardId`; `FireTarget` has no Unarmed fact, so a freed Unarmed unit takes HoB and Replacement. A19.11 (p. 86): "crew-types, Commissars, Heroes, unarmed and already broken units are never subject to Replacement". To verify: Fire's `Replace` Disrupts a PRC with no lesser unit; A19.12 says PRC are never Disrupted.

## 4. LLMC outside fire

A10.2 (p. 65): "a mortal wound, or any other cause". Lost outside Fire by: Interdiction CR or elimination (and the Voluntary Rout leader), Mopping Up, a PF Check, Rally Fate, Failure to Rout, a Sniper. An LLMC needs the friendly Personnel of the Location with a lower current ML and the dead leader's reversed negative DRM; none in Melee (A11.141); Riders exempt. An LLMC is a MC with HoB, ELR, wounds: the member's lost-leader entry.

## 5. Voluntary Break and Voluntary Rout

A10.41 (p. 66): at the start of the RtPh, a unit (even pinned) in the LOS and Normal Range (A10.532) of an armed, unbroken Known enemy ground unit, or ADJACENT to any unbroken enemy ground unit; not if breaking would Reduce or eliminate it at once; heroes, berserk, Unarmed never break (A10.3). It is under DM (A10.62) and must rout, or surrender (A20.21). Proposal: action `asl.game.voluntary-break {unitIds}` in the RtPh before any `rout-stepped`; a record `voluntary-break(Units)` then `conditions-changed` (Broken, DM, pin cleared), the projector checking the timing; a list in `RoutActionPanel.razor` by hex. Ruling needed: a leader's Voluntary Break causes a LLTC (A10.2 makes no exception).

A10.711 (p. 69): a non-berserk, non-pinned, unbroken leader already stacked with the broken unit routs with it, exempt from one-at-a-time; no NMC himself; adds his DRM to its NMC; eliminated if it fails (then an LLMC, the surviving HS takes it); portages his own SW, cannot raise its IPC. Proposal: `withLeader` on `asl.game.rout`; `RoutStepped` for both; `RoutInterdicted.Drm` carries his DRM plus an optional `Leader`; a leader select in `RoutActionPanel.razor`.

## 6. 70.2

- By level: largely built since 2026-10-08 (`RoutScan.Neighbors`, GamePlanner.Map.cs:177); the inventory's A10.5 note is stale. Missing: A10.532's EXC (p. 67), a unit ignoring the hexes of the building it starts in; `RoutTargets` (RoutCalculator.cs:471) excludes only the starting Location.
- Vehicles and Guns: `Interdictor` skips vehicles (RoutCalculator.cs:199); `NormalRange` gives a vehicle none (GamePlanner.Rout.cs:303-311). Needs a vehicle's weapon ranges, a bar on any positive DRM (D3.44 implies one; A10.532's Gun CA change), and feeds must-rout and RtPh DM (A10.5 "unit/its-SW/Gun"). Decided at plan time and recorded: no record fact.
- Fanaticism by SSR: SSRs are tokens in `GameState.SpecialRules` from `game-started` (GameProjector.cs:486), card tokens checked by `ScenarioA1SetupCalculator.RuleDiagnostics`. Proposal: `fanatic:<side>[:<group>]`, setting `asl:fanatic` at setup; absent-safe.

## 7. HoB, leadership, LLMC for PRC

HoB rightly none (Fire skips PRC, FireCalculator.cs:4477; A15.1). Leadership: Fire's `Check` skips PRC (4677). LLMC: `LeaderLoss` iterates `state`, not `prc`. To wire: `FirePrc` gets its vehicle's motion and the leader's CE-in-an-unmoved-halftrack (GamePlanner.PassengerTargets.cs); `LeaderReaches` in `Check` both ways; a Passenger leader's loss checks fellow Passengers only; Riders neither take nor cause it (A10.2, D6.651). Rider leaders: A10.7's EXC lets a Rider leader aid his own vehicle's units in the Bail Out.

## 8. Bail Out and D5.311

Bail Out: leadership (a Rider leader), Wound Severity, ELR (Replacement, never Disruption, A19.12) fall out of the member; HoB not owed (PRC). D5.311: HoB, ELR, leadership not owed; left: a broken crew checked again (CR eliminates) and a Failure to Rout that Abandons the AFV.

## 9. Tests asserting today's behavior

Member, Unarmed, HoB: ScenarioA1FireExtensionTests.AnOriginalTwoOnAnMcCallsForHeatOfBattle; ScenarioA1FirePackageTests.AnEliminatedLeaderCausesALlmc, .ALeaderWhoBreaksCausesALltcAndNoLongerAidsMcs, .AMortalWoundEliminatesTheLeaderAndCausesALlmc; ScenarioA1Pass5Tests.ASecondOriginalTwoInOneAttackCallsForASecondHeatOfBattleDr; ScenarioA1FireReachabilityTests.ThePrecheckRefusesWhatTheDiceCouldLeaveUndecided.
Interdiction and Voluntary Rout: BacklogPass13Tests.ABrokenUnitInOpenGroundRoutsToTheNearestWoodsAndIsInterdictedOnTheWay, .LowCrawlEscapesInterdictionAndAUnitThatStaysInTheOpenFailsToRout, .AFailedInterdictionReducesTheSquadAndItsHalfSquadRoutsOn; BacklogPass35Tests.AFanaticUnitTakesItsInterdictionCheckAgainstOneMore, .ALeadersCasualtyReductionInARoutIsAWoundWithItsSeverityDr, .ACommissarRoutsThroughInterdictionRatherThanSurrender; ScenarioA1RoutRallyRulesTests.TheProjectorsRoutAndMassacreArithmetic, .TheRoutSummaryWordsTheStepsTheInterdictionAndWhatIsLeft.
Bail Out: ScenarioA1Pass65RulesTests.ABailOutsNmcAndItsSwDrs, .ABrokenRiderIsNeverPinnedAndOneWithNoMoraleLevelIsRefused; BacklogPass65Tests.APinnedRiderBailsOutAndChecksItsSw; PlayPagePass65Tests.TheRecordsSayABailOutsDiceAFailedLoadAndATemporaryStart.
D5.311: ScenarioA1Pass65RulesTests.AnUnprotectedCrewThatBreaksIsPlacedBeneathAndOneEliminatedIsGone, .ABrokenCrewStaysInsideStopsItsAfvAndRoutsBeneathIt; BacklogPass65Tests.AnAirBurstBreaksAnOtHalftracksCrewWhichRoutsBeneathAndAbandonsIt.
PRC leadership: ScenarioA1Pass65RulesTests.APassengerLeadersReachIsHisVehicle, .ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf, .AirBurstsReachTheBuPassengersOfAnOtAfv.
PF: ScenarioA1Pass9Tests.APanzerfaustCheckMayGiveNoShotOrPinOrBreakItsFirer, .APanzerfaustTakesCaseC3AndAnOriginal12ReducesItsFirer; BacklogPass9Tests.APanzerfaustCheckOf6PinsTheUnit.
Mopping Up: ScenarioA1CloseCombatRulesTests.MoppingUpsCasualtyDrmAndItsNotes.
70.2: ScenarioA1RoutRallyRulesTests.TheInterdictorIsUnbrokenInfantryNotCxPinnedEncircledOrInMelee, .TheNormalRangeIsTheLongestOfOwnAndWeaponsAtMostSixteen, .ALoneSmcDoesNotInterdictWithASupportWeapon, .TheBarsReadTheirLazyFactsOnlyWhenTheyMatter; BacklogPass35Tests.ARoutUpAHillIsInterdictedOnlyAcrossTheCrestLineTheLosCrosses; ScenarioA1Pass55RulesTests.HeightAdvantageBarsCaseJ4; BacklogPass10Tests.ABrokenUnitUpstairsRoutsDownItsStairwell; PlayPagePass31dTests.TheRoutPanelSaysWhatALadenUnitLeavesOffersAWayAndTheReviewSaysWhatIsLeft; ScenarioA1Pass19bTests.BattleHardeningMakesTheCardSquadsFanatic.
None exist for Voluntary Break or Rout, or for `ElrImmune`.

## 10. Estimates

70.1: member and the check-only branch 1:30; Unarmed in Fire 0:20; Interdiction wired (HoB, the rout ending, LLMC) 1:00; Bail Out 0:50; LLMC for Mopping Up, PF (with LLTC), Rally Fate, Failure to Rout, Sniper 0:50; PRC leadership and LLMC in Fire 0:45; D5.311 broken crew and Abandon 0:30; Voluntary Break 1:00; Voluntary Rout 1:00; record texts 0:20. Total 8:05 (plan 0:40).
70.2: the starting-building exception 0:30; vehicle and Gun Interdictors 1:15; Fanaticism by SSR 0:40. Total 2:25 (plan 0:40).

Move out to the backlog: the SMC crew Stun wound dr (75.3 or 105); re-figuring a rout's destination when an enemy becomes newly Known; the minefield, FFE, shellhole, entrenchment, Blaze rout options and Spotted Fire Interdiction (terrain passes 115 to 140); Fire's possible Disruption of a PRC (verify first). If 70.1 builds the Sniper's LLMC, remove "LLMC after a hit" from 75.1.

Not read: Failure to Rout's code (Rout.cs:923), the D5.311 rout beneath, GamePlanner.Snipers.cs, Play.razor's wiring, pass 65's design and code map, the standing rules, vehicle weapon ranges.
