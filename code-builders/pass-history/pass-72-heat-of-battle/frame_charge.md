# Frame A: 72.1 berserk charge and return to normal

The read-only frame agent's report, 2026-10-27, condensed only in layout. Pages read: A15.4 to A15.5 (p. 84), footnote 21 (p. 102), the berserk example (p. 62), E1.533 (p. 224), A4.63 Dash (pp. 51, 52); every page searched for "berserk". Code: `ScenarioA1ChargeCalculator`, `ChargeModels`, `GamePlanner.CloseCombat.cs` (880 to 911, 1303 to 1583), `BerserkStep`/`BerserkFirst` (Movement), `EndMove` (MovementCalculator 705 to 728), the MPh end (GamePlanner.cs 838 to 864), `ScenarioA1SequenceCalculator` Berserk*, `ScenarioA1CloseCombatRules` (ChargeBarred, RequiredUnit), the CC calculator's A15.46 block (911 to 923), `ScenarioA1HeatOfBattle.Resolve`, `ScenarioA1MandatoryFire`, `ScenarioA1Dash`, the DC planner, the fire bars.

## 1. Code map and the "ends in place" cases
- `GamePlanner.SearchCharge` (CloseCombat.cs:1502) reads Known enemy Locations through the memoized `LocationOf` into `ScenarioA1ChargeCalculator.Steps` (116): the nearest Known enemies in LOS by hexes, one Dijkstra per target (`FirstMoves`, 72) with the other enemy Locations blocked. `MustCharge` (1577) lists a unit only when Steps are not empty and affordable. `EndMove` lets an undecided charge end with the R30.5 note (MovementCalculator.cs:720). `BerserkReturns` (GamePlanner.cs:858) ends berserk at the MPh end when no Known enemy is in LOS.

| Case | Where | Decidable now? |
|---|---|---|
| a. distance unreadable (uncomposed board pair) | ChargeCalculator.cs:143 | no: a map gap, keep R30.5 |
| b. no route to the nearest (cliff, water, slope, unreviewed terrain R10.1, marsh from below, boxed in by enemy Locations) | ChargeCalculator.cs:165 | yes: A15.45's next nearest; none reachable, A15.44 Battle Hardening (E1.533, p. 224: an existing berserk status is lost) |
| c. first step into a vehicle-and-Infantry Location | ChargeCalculator.cs:175 via `ScenarioA1CloseCombatRules.ChargeBarred` (297) | yes (section 2) |
| d. a Gun crew's Location (R27.2) | none | already lifted by pass 70 (R70.27), `OrdnanceStepsTests.ABerserkChargeIntoACrewsLocationLeadsToCc`; backlog rows 595 and 940 and inventory A15.43 stale |

Proposals: next distance group when no route reaches the nearest; A15.45's alternate route measured in hexes stepped on the shortest-MF route against the next nearest's range, a tie staying with the nearest; Battle Hardening only when every refused entry on the frontier is a rule bar (cliff, water key), a "not reviewed" refusal keeps R30.5; the conversion writes the rally path's lineage Replaced event (Rally.cs:275 to 292) with `ScenarioA1FireReference.HardenedOf`; a reason-bearing `EntryCost` on `IChargeFactReader`. Blaze to pass 185. Cleanup: `GamePlanner.ChargeBarred(GameState, ...)` (CloseCombat.cs:903) is dead code with a `state.At` scan; GamePlanner.cs:859 evaluates `state.Location(unit.Id)` for every unit eagerly, use `LocationOf`. Estimate 2:00.

## 2. A vehicle-and-Infantry Location
Built by pass 70: R70.30 (Infantry CC beside a vehicle in the vehicle's sequential round); fix Round C `ScenarioA1VehicleCloseCombatRules.OwingAttack` (127, 141); the MPh entry lets an all-berserk stack in (MovementCalculator.cs:273); A8.312 Mandatory Fire is generic. Remains: remove `ChargeBarred`'s refusal and text; one Play test; one Studio game. Not read: whether the CC calculator's A15.46 check (913 to 919) counts a surviving vehicle. Estimate 0:40.

## 3. A15.431 gaps
| Item | Exists? | Proposal |
|---|---|---|
| berserk in mid move | built in effect (`MoveAllowance` 8, `HalfMfLeft`); mid move `MfAllotment` counts only 1PP SW (37) | in: a Play test and the PP read, 0:25 |
| Dash across a road | Dash built; berserk refused (Dash.cs:59) | out: pass 73 (73.3), a row |
| Wire as 1 MF | no Wire | out: pass 120 (120.1); minefields and FFE to 120 and 150 |
| DC Thrown only | built (DemolitionCharges.cs:37, 210) | inventory row fixed |

## 4. A15.432's "not verified" items
All built: the DEFENDER's FPF by A8.312; halved AFPh TPBF (FireCalculator.cs:865, 4128); "not yet held in Melee" (R70.32); berserk fire in the AFPh and DFPh (`FireBar`, FireEligibility.cs:250; Opportunity Fire refused, 1099; `BerserkDirectorBar`). The comment at GamePlanner.Fire.cs:274 is stale. Estimate 0:20 (the comment and one Rules test).

## 5. A15.46 gaps
After AFPh TPBF or a FT clears the Location: a Rules verdict (`BerserkReturnsAfterFire`), Play writes `conditions-changed` (Berserk false) per berserk firer after the fire's effects (Fire.cs around 1100 to 1130), 0:45. After destroying a vehicle in CC: at VehicleCloseCombat.cs:677 when no other Known enemy is left (as `NextAfter` reads at 748), 0:30.

## 6. Recorded games
`dl-tractor-works-01`: a squad went berserk at 664, eliminated at 711, no charge. `dl-tractor-works-02`: `r-squad-10` berserk at 1188; its charge at 1317 (the cut 1316) decided, one step into X3; no AFPh fire, no vehicle CC, never ended in place. No `p71-*` game holds a berserk unit. No recorded plan passes a changed case; every new outcome is a new planner-written event, none derived in the projector.

## 7. Performance
PR 32 memoizes the search under `ChargeKey` in `PlayStepMemo.Charges` (CloseCombat.cs:1438 to 1445; counter `ChargeSearches`). One single-source Dijkstra with every enemy Location terminal gives the first moves to every target; read targets in distance order with each route's hex count. The fall back adds no search. Measurement on `p70-perf-tw` (cut 1316, Russian Turn 5 MPh with `r-squad-10`). A test that `ChargeSearches` stays 1 when it falls back. 0:35.

## Estimates
2:00 + 0:40 + 0:25 + 0:20 + 1:15 + 0:35 = 5:15 (the plan: 1:00 for all of 72.1).

## Not read
A22's FT in its own Location; the CC calculator with a surviving vehicle in A15.46; GameProjector's berserk handling beyond `MassacreEndsBerserk`; cut files.
