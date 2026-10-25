# Frame agent B: who sees, concealment, HIP, Right of Inspection (70.3, 70.4, 70.5, 70.10)

Read-only agent, launched 11:54, reported about 12:08 (2026-10-25). Read A12.1 to A12.42 on pp. 76 to 80 with the examples; the Concealment Loss/Gain Table not rendered (scrambled), worked from the body rules. Kept as it came, condensed only in layout; the design's rulings judge it.

## 1. Who sees (70.3)

| Read | Where | Condition | Departs from A12.1/A12.14/A12.141 |
|---|---|---|---|
| `EnemyGoodOrderInLosWithin16` | Rules `ScenarioA1MovementCalculator.cs:1148`; Play `GamePlanner.Map.cs:262` | active, enemy, not a Dummy, `GoodOrder` (A.7, `GoodOrderOf`), not hidden, not aboard; `Status==Clear`, range 16 or less | An empty or Abandoned vehicle is not "broken" (A12.1); Nondefinitive LOS counts as unseen; same-Location viewer relies on the map's self-LOS |
| Its callers | Movement `Unmask` (Movement.cs:464), advance (CloseCombat.cs:263), Rally (Rally.cs:80), Gun CA (Guns.cs:50), ordnance `CrewSeen` (Ordnance.cs:709) | same | same |
| `WithSeen` | Rules :182; Play Map.cs:244; Fire.cs:175, 411, 469, 583 | same filter plus a same-Location viewer; nothing at night | same |
| `NearestGoodOrderEnemyInLos` | Rules :164; Map.cs:231; Ordnance.cs:1085 (A12.34 reveal) | as above but counts hidden viewers, no range limit | inconsistent with R35.5 (a hidden unit forces nothing) |
| `Watching` / `SeenByEnemy` | `GamePlanner.VehicleConcealment.cs:21, 26`; Rules `ScenarioA1VehicleSightRules.cs:14` | a vehicle not Stunned, Shocked, UK, Recalled, Abandoned; Personnel not broken, berserk, captured, in Melee; `IsBlocked:false` | counts hidden units and Passengers; Nondefinitive "clear" counts; scans per watcher. Used by Case H and moving (Vehicles.cs:732, MotionAttempts.cs:227), Opportunity Fire reveals (FireExtensions.cs:56), OVR PAATC subjects (Overrun.cs:201) |
| Gain watchers | `FireExtensions.cs:219`; Rules `ScenarioA1FireFollowUps.cs:51` | active, enemy, not broken, not captured (Dummies count, right) | hidden units deny gain without being placed (A12.32 fault); empty vehicles deny; `IsBlocked:false`; Concealment Terrain is a duplicate list (:78) beside `IsConcealmentTerrain`; per-candidate `state.At` lambdas (:212, :215) break the memory guideline |
| Non-OB "?" reader | `GamePlanner.Disclosure.cs:59` | unbroken, not hidden, not captured | setup only; may stay |

The inventory notes for A12.1 and A12.14 are stale (pass 35's R35.5 fixed movement, advance, Rally); 70.3 corrects them.

Proposal, Rules: one member `ScenarioA1Sight`: `SeerFacts(Active, Side, Dummy, Hidden, Aboard, GoodOrder, Unbroken, EmptyVehicle, Location)`; `Sees(facts, purpose)` with three purposes: Loss (Good Order, not a Dummy, not hidden, not a Passenger, not an empty vehicle), GainDenial (unbroken, Dummies included, not hidden, not an empty vehicle), Inspection (own Good Order ground units; hidden count, Dummies not); `NearestSeer(seerLocations, at, los)`, the nearest range with a clear LOS or null. "Within 16 and in LOS" is `nearest <= 16`.

Proposal, Play: one read `int? NearestSeer(state, watchedSide, at, purpose)` in `PlayStepMemo`: the distinct seer Locations per (side, purpose), and the answer per (side, purpose, at). Today `EnemyScan` rebuilds every unit's facts with a `state.Location` read each call. Replay safe: reveals are recorded events; only new proposals change, as R35.5 did without a marker.

Other actions that cost a "?" (A12.141): built: Rally and a leader's Rally DRM; Gun CA; vehicle VCA/TCA and Motion; fire and directing fire, MOL and PF; CC. Built as actions but costing no "?": Deploy, Recombine, SMOKE grenades; under "any other activity": Recover, Transfer, Dismantle, Scrounge, Hook Gun, the Mopping Up declaration, a passed LLTC/LLMC, a concealed leader's DRM to another unit's MC. Not built: entrench, set a Fire, Clearance, ATMM, Sniper Check (pass 75). Exempt, correctly free: Drop, Repair.

## 2. Owner's choices on a "?" (70.4)

Exists: `asl:concealed`, `asl:hidden`, Dummies `UnitKinds.Dummy`; events `concealment-lost`, `concealment-gained`, `hidden-placed`; `PlanPlaceHidden` (Disclosure.cs:106); `ChoicePending` with Take/Decline (Choices.cs), used by A12.41's reveal-or-PAATC. No voluntary removal; the viewer's option is the deviation of R10.10 and R31d.2 (backlog row 786).

Proposal: action `remove-concealment(unitIds)` shaped as `PlanPlaceHidden`; any time, by the owner; writes `concealment-lost` with both conditions false; a Dummy is eliminated; a hidden unit fully revealed (A12.31); the Sniper exception noted, not built. The viewer's option: a `ChoicePending` to the viewing side only when every Loss seer within 16 is itself concealed; its momentary reveal a disclosure to the mover only; hooks at the one `NearestSeer` read, but the move, advance, and fire paths each need a resume.

## 3. 70.5

- Gain: the Rules gain search excludes vehicles and Gun-holders (`FireFollowUps.cs:52, 55`). Missing: an Emplaced Gun crew's and a vehicle's J/K gain; A12.122's +1 Lax, -1 Stealthy, a possessed Gun's US#.
- Vehicle Concealment Terrain is grain in season only (R6.7, `ScenarioA1VehicleSightRules.cs:11`). Missing: A12.2's woods-, orchard-, brush-, grain-road at setup; secret Target Size; Smoke Dispenser; Cavalry, horses, bicycles.
- Setup (`ScenarioA1SetupCalculator.cs:676-731`): HIP needs SSR `hip:side:n` (`HipAllowance`, Definitions.cs:187), kinds in `HiddenSetupKinds` (Infantry; :13), in `IsConcealmentTerrain` (:160). An Emplaced Gun and crew may hide in Concealment Terrain with no SSR (R26.5). Hidden vehicles refused (:691). An Emplaced Gun out of Concealment Terrain and out of enemy LOS refused (backlog 570): needs a reveal check after each step and each phase (the memoized read again).
- Entering concealed: refused (R20.5, backlog 495); no Dummies among OB "?" reinforcements.

## 4. Right of Inspection (70.10)

A12.16 (p. 79): after play begins, the opponent inspects an unconcealed stack unless it is out of the LOS of all his Good Order ground units; then only verification of actions. Dummies are not Good Order.

The view: `GameView.Of` (Units, `GameView.cs:56-174`) applies `ScenarioA1Visibility`. Before play (A2.9, R23.3) it shows a stack's top counter and counts the rest as `SealedUninspected`. No LOS.

Who builds a view: `Play.razor:4911` (cached per history, revision, viewer; a hand-over builds one); `Play.razor:6543` `SideView` (one slot: fire and ordnance targets, `EnemyAfvSeen`); `GameLibrary.ViewOf` (:129, uncached, each map-layer projection Play.razor:9276, Replay :418, 702, 748, Games :157); `UnitNames.cs:306` (once per new revision per viewer, over the whole history); `CaseRead.cs:162` (Units); the proof tool's sweep (`Program.cs:242`). No planner proposal builds a view.

References: Units references Maps and Rules (`Units.csproj:3, 5`); the LOS map (`Composed`, `Handle`, `LosReads`) lives in `GamePlanner`.

Proposal: Rules `Inspectable(setupClosed, anySeerWithLos)` (Inspection purpose). Play `IReadOnlySet<BoardLocation> Uninspected(state, perspective)` memoized per (state, viewer) in `PlayStepMemo`: enemy unconcealed stack Locations with no Inspection seer in LOS. `GameView.Of` takes the set as it takes `outOfSight`, treating those stacks as before play (top shown, rest `SealedUninspected`, equipment beneath withheld). One Play entry `planner.ViewOf` for Play.razor, `GameLibrary`, `UnitNames`, the sweep. Null or Unsupported LOS counts as in LOS. Events unchanged (A12.16 allows verification). Two wordings: these counters (today "not inspectable before play") and `UnitNames`' "a concealed unit".

Cost: each LOS read cached for good per map and (from, to) (`Map.cs:121`, static `ConcurrentDictionary`); the cold `LosCalculator.Check` is unmeasured. `dl-tractor-works-02` (1,441 events; agent's estimate): rev 1316 German 26 units in 9 Locations, Russian 18 in 8; at the end 25 in 9 and 7 in 5. A view is at most about 9 x 9 pair lookups with an early exit; the real cost is `UnitNames`' read of every revision, bounded by the memo per state.

## 5. Older games

The replay digest hashes states only (`Program.cs:177`). The text list is a scan of source literals (`text_list.py`). Only the sweep's `GameView` line, one per perspective per game, changes, and only if the sweep passes the set. Proposal: apply 70.10 to every game with no marker (the view is derived, never recorded; A12.16 is a gap, plan :2148; a marker would keep the fault in `dl-tractor-works-02`); the sweep calls `planner.ViewOf`.

## 6. Pass 60's AFV-naming row

`target-vehicle-concealed` (`ScenarioA1FireCalculator.cs:586`) exists because the FT/MOL/DC record would name concealed or hidden units or Dummies to the firer (a MOL's DR on the other units, a DC's Position DR of 12+; pass 60 design :247). 70.3 and 70.10 do not answer it. Proposal: move to pass 75 task 75.6.

## 7. Blast radius

View: StateTests.ASideDoesNotSeeHiddenUnitsAndSeesConcealedOnesAsSealedPresence, .TheRussianSideDoesNotSeeHiddenGermansAndSeesConcealedOnesAsSealedPresence, .ARevealShowsTheUnitToTheOtherSide; GameStatesTests.ASideSeesOnlyWhatItMayKnow, .TheRussianSideSeesOnlyWhatItMayKnow; CaseReadTests.AConcealedOccupantIsOnlyASealedPresence, .TheRussianSideCannotSeeTheHiddenOrConcealedGermans; FireVisibilityTests.EachSideSeesAKnownTargetBrokenAndTheFirersMarked; UnitNamesTests.TheOtherSidesUnitsCarryNoTagAndReadByCounterAndLocation, .AUnitTheViewMayNotNameReadsAConcealedUnit; ReplayStepsTests.WhatChangedIsSaidFromTheViewAlone. Page: PlayComponentTests.TheUnitTableNamesWhatItCannotShow; PlayPagePass31cTests.TheInspectorsRangeTabReadsTheHexesAndTheUnitsTheViewHolds, .TheHandOverScreensMapShowsNoCounterOfAConcealedOrHiddenUnit; PlayPagePass60Tests.AMgIsOfferedAsOrdnanceOnlyWhenItsSideSeesAnEnemyAfv, .AMgIsOfferedAsOrdnanceWhenItsSideSeesAnEnemyAfvAndWaitsForItsName; BacklogPass30bTests.ThePlanUsedMatchesWhatTheOtherSideSeesAndTheOthersDoNot.

Who sees: ScenarioA1Pass35RulesTests.TheScansCountAGoodOrderEnemyNotMerelyAnUnbrokenOne; ScenarioA1VehicleRulesTests.AWatcherIsGoodOrderPersonnelOrAnUnstunnedVehicle, .AVehicleLosesItsConcealmentMovingWithin16OrSeenOutOfConcealmentTerrain; BacklogPass35Tests.AConcealedMoverLosesItsConcealmentToAGoodOrderEnemyAndNotToAHiddenOne; FireConcealedFirerTests (five); BacklogPass10Tests.ConcealedUnitsMoveAndLoseTheirConcealmentInTheOpen; BacklogPass12Tests.InfantryGainConcealmentAsTheirPlayerTurnEnds; BacklogPass16TablePlayerTests.NightConcealmentGainAndNvr; ScenarioA1RallyPackageTests (concealed cases); BacklogPass26Tests.AHiddenEmplacedGunThatFiresIsRevealedOrConcealedByItsColoredDr; BacklogPass6Tests.AConcealedVehicleOutOfConcealmentTerrainLosesItsQuestionMarkWhenAnEnemyComesIntoView; BacklogPass55Tests.AConcealedVehicleLosesItsConcealmentWhenItsMotionAttemptPasses, .AConcealedVehicleKeepsItsConcealmentWhenItsMotionAttemptFails.

HIP: BacklogPass23Tests.HipNeedsItsSsrAndStaysUnseen, .OnlyInfantrySetsUpHidden. Units tests have no map, so a null set keeps them green.

## 8. Where a view shows on the page

`Play.razor`: `View` (:4898-4915), `SealedPresences` (:5216), target areas (:2372), `FireTargets` and `OrdnanceTargets` (:6522-6560), `EnemyAfvSeen` (:5262), map layers (:9276 via `GameMaps.Layers`, `GameLibrary.ViewOf`). `GameMaps.cs:112-114`; `BoardWorkspace.razor:230`, `SelectedUnitInspector.razor:35`, `PlayUnitTable.razor:26`; `ReplayDiff.cs:73`, `Replay.razor:873`, `Games.razor:162`, `GameText.cs:59`.

## 9. Estimates

| Task | Plan | Agent | Contents |
|---|---|---|---|
| 70.3 | 0:40 | 2:30 | the sight member and memo read, six call sites, the A12.141 actions |
| 70.4 | 0:40 | 2:00 | removal 0:40, the viewer's option 1:20 |
| 70.5 | 0:40 | 3:30 | crew and vehicle gain 1:00, entering concealed 0:50, HIP for vehicles 0:40, an Emplaced Gun's HIP out of Concealment Terrain with its reveal 1:00 |
| 70.10 | 0:40 | 2:00 | |

Move out: 5/8" Dummies, Cavalry, horses, bicycles, secret Target Size, Smoke Dispenser to the backlog (no counters); entrench, set a Fire, Clearance as "?" costs to pass 115 and the terrain passes; pass 60's AFV-naming row to pass 75 (75.6); if needed, the viewer's option of 70.4, R31d.2 kept.
