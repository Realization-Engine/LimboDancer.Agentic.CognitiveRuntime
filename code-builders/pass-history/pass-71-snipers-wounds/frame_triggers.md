# Frame agent A: which DRs call a Sniper attack (A14.1, p. 82)

(The agent's closing report, kept as it came, 2026-10-26 about 08:3x.)

## 1. How the trigger works today
- `AddSniperAttacks` (GamePlanner.Snipers.cs:20-65) is called at Fire.cs:593 (the blocked-LOS split attack), Fire.cs:608 (an ordinary attack), Fire.cs:922 (a DC's follow-ups, after the charge is removed), and Choices.cs:226 (an attack resumed after an owner's choice; skipped for a DC and Canister, R55.30).
- Guards (line 23): nothing when the commit holds a `ChoicePending`; nothing unless `ScenarioA1Sniper.TriggerPhase(before.Phase)` (Sniper.cs:47: pfph, mph, dfph, afph). `before` is the state ahead of the commit.
- Records read: only `FireResolved` in this commit's `events` (line 32); each `fire.Rolls` key with a 2-dice roll (35-39). `RollKey` (Sniper.cs:50) splits kind and unit, drops `#n`; `RollMaker` (Sniper.cs:67-76): `attack` (IFT) is the firing side's (`FiringSideOf`, Snipers.cs:68: firers, a DC's user, the DEFENDER for Residual FP); `checks`, `leaderLoss`, `berserkCheck`, `crewCheck` the unit's side's, never a prisoner's. `Triggers` (Sniper.cs:82) compares with the enemy SAN through `SniperActivationNumber` (Sniper.cs:79: +2 at night, at most 7; E1.76, p. 225). Needs an active enemy `asl:sniper` entity (Snipers.cs:54).
- Order: by DR order of the commit's `dice-rolled` events, earlier commits' dice included for a resumed attack (29-30, 61). `Snipe` runs in the same commit after every fire event, follow-up, and bail-out; never part of the proposal; drawn inside the PlannedRoll's Build.
- Skipped kinds inside fire records that ARE A14.1 DRs: `collateralCheck` (a CE crew's MC, FireCalculator.cs:2057) and `taskCheck` (an immobilized crew's TC, FireCalculator.cs:2295). Rightly skipped: `heatOfBattle`, `crewSurvival`, `prcSurvival`, `position`, selections.

## 2. Qualifying DRs the hook does not read
| DR | Where | Key | Recorded | Phase | Hook could read |
|---|---|---|---|---|---|
| Residual FP on an Infantry move | Movement.cs:593 | fire `attack` | FireResolved | MPh | yes: no call today |
| Fire Lane attack | FireExtensions.cs:185 from Movement.cs:598 | fire | yes | MPh | yes |
| Residual FP on a vehicle entering | Vehicles.cs:260 | fire | yes | MPh | yes |
| Vehicle OVR IFT | Overrun.cs:111 | fire | yes | MPh | yes; only the resumed OVR calls it now (Choices.cs:226) |
| Crew Collateral MC, immobilization TC | FireCalculator.cs:2057, 2295 | `collateralCheck`, `taskCheck` | yes | fire phases | yes, with a maker "the target vehicle's side" |
| Ordnance To Hit (Guns, MA incl. BFF, LATW, PF, PSK, ATR, mortars) | Ordnance.cs:893-921 | `toHit` | `OrdnanceFired.Rolls` | all four | yes, but the ordnance Build, its resume, and Gun Duels each need the call; nested keys |
| HE hit IFT and MCs; crew TCs and MCs | Ordnance.cs | `hit:attack`, `hit:checks:id`, `critical-hit:...`, `crewCheck`, `collateralCheck`, `areaCrewCheck:id`, `canister` | `OrdnanceFired` | all four | same; reverses R55.30 |
| LLMC after a PF Check | Ordnance.cs:962 | morale-check-resolved | yes | fire phases, MPh | only if the ordnance path calls the hook |
| LLMC after Searching | Searching.cs:220 | morale-check-resolved | yes | MPh | yes |
| LLMC after Mopping Up | MoppingUp.cs:218 | morale-check-resolved | yes | PFPh | yes |
| LLMC after a Sniper hit | Snipers.cs:214 | morale-check-resolved, cause sniper | yes | all four | not today (the trigger list is built before `Snipe`) |
| Bail Out NMC | Riders.cs:134, 239 | `RiderBailedOut.Roll` | yes | MPh, fire phases | yes |
| PAATC | Overrun.cs:293 (MPh); Overrun.cs:323 `AddPaatc` (CC Reaction Fire in MPh; advance in APh) | `PaatcTaken.Roll` | yes | MPh only | yes |
| OVR NTC (SMC vs vehicle OVR) | GamePlanner.cs:1569 | `TaskCheck.Roll` | yes | MPh | yes |
| Infantry OVR NTC | InfantryOverrun.cs:104 | `InfantryOverrunNtc.Roll` | yes | MPh | yes |
| Entrenching | not built | | | | |
| Minefield | not built | | | | |

Outside the four phases (no change): the Interdiction NMC and its Heat of Battle, Failure to Rout, Voluntary Break and Rout (RtPh); Rally and its LLMC, Repair, Shock recovery, Deployment (RPh); CC, Ambush, Withdrawal, vehicle CC rounds (not IFT, CCPh). Not A14.1 kinds: To Kill, Shock, Crew Survival, Bog, ESB, Mechanical, Manhandling, Heat of Battle DR, Wound Severity, every dr. Already read: DC, FT, Spray, the blocked-LOS part, Residual FP left by an attack.

## 3. "A DR that can yield no result other than a SAN is not made"
Partly; no general guard. The blocked-LOS part's DR is made (Fire.cs:387-426, A6.11). The Morale Check member draws dice only on demand (`AnyCheck`, MoraleChecks.cs:102). Fire Lane skipped when `Precheck` fails (FireExtensions.cs:178). Not verified: whether Fire refuses an attack on a Location where nothing can be affected (`SelectTargets`, FireEligibility.cs:349, does not refuse an empty list). Proposal: no work in pass 71; a backlog row if a case is found.

## 4. Order, and one hook for every package
- The hook runs once after every DR and effect of the step, attacks by DR order. A small existing deviation: a trigger by an early DR is resolved after later DRs of the same step (a DC thrower's attack, Spray's second Location).
- `dice-rolled` alone cannot replace the records: it carries a purpose, no unit or side; purposes such as `fire-check` are shared.
- One reader can serve every package: `FireResolved.Rolls`, `OrdnanceFired.Rolls`, `MoraleCheckResolved.Rolls` share the key grammar; `PaatcTaken.Roll`, `TaskCheck.Roll`, `InfantryOverrunNtc.Roll`, `RiderBailedOut.Roll` are single rolls.
- Proposal: generalize `AddSniperAttacks` into one reader with a payload-type switch in Play; extend `RollMaker` in Rules with `toHit`, `hit:`, `paatc`, `ntc`, and the vehicle-crew maker. Call it once per planned step through a central plan wrapper (GamePlanner.cs near line 410, like `WithImmediateVictory`), keeping Fire's calls and skipping a commit that already ran the hook. Loop the reader over the events each `Snipe` adds, so a Sniper's LLMC can call the next attack (A14.1 "not before resolving all Sniper attacks made prior to it", A14.3 "resolved only after ... all LLMC/LLTC": needs a ruling); it ends since a pinned Sniper makes no more attacks and each round needs a new DR.

## 5. Proposal and estimates
In the pass (about 0:55): the missing fire-record calls and the OVR paths agreeing 0:10; `collateralCheck`/`taskCheck` makers 0:10; the generic reader through the wrapper 0:25; chaining after a Sniper's LLMC 0:10.

Moved out, each with a backlog row: ordnance triggers (To Hit, HE IFT and MCs, crew TCs, mortars, LATW, BFF, Canister) to pass 110 (0:35 to 0:45 more here; reverses R55.30); Entrenching to pass 115; the captured MG's automatic Sniper dr at night (E1.76, backlog 26) to pass 90; the early-DR order deviation to the backlog.

Proof store: none of the 43 whole games holds an `asl:sniper` entity or a `sniper-attacked` event; no `ordnance-fired`, `task-check`, `infantry-overrun-ntc`, or `rider-bailed-out`; no `morale-check-resolved` or `paatc-taken` in a trigger phase (scan `scratchpad/frame/scan.py`). Older records are safe: a Sniper attack is a recorded event; the projector (GameProjector.cs:4874) checks only that the counter is active and the attack's and trigger's rolls are recorded. A new trigger changes only new plans with an active enemy Sniper counter.

Not read: the ordnance resume path, Gun Duels, Massacre, CrewActs, MandatoryFire, Recall; A14.4; backlog 25 and 26 text; the pass 70 design.
