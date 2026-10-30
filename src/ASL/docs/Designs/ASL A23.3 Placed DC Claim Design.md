# ASL A23.3 Placed DC Claim Design

**Status:** Approved by the user and built, 2026-10-29 (section 10), after a sub-agent's review of the design (section 9). The A23.3 claim (Claude; the inventory's section 6, claimed on main as 74a29884) takes A23.3 Placement and A23.4 Detonation, the Placed Demolition Charge, on the branch `claim/a23.3-placed-dc`. Rules only: no caller changes, so no recorded game replays differently. Rulings are numbered RA23.n.

## 1. The rule (A23.3, A23.4, pp. 90 and 91; A23.2; A8.1; A11.6; A20.24; A4.5, A4.51; A7.21; C7.346; ASOP 5.22A)

- **A23.3:** a DC is Placed on an ADJACENT target Location during the MPh by a carrying Infantry unit, which expends extra MF in the Placement Hex equal to what it would need to enter that Location directly during that MPh without Bypass. The Placement is movement in the Placement Hex (the placer's Location). A unit may not Place a DC if it fired during the PFPh or was pinned before the Placement during that MPh. If an enemy AFV occupies the target Location, most Placing units must pass a PAATC (A11.6, which exempts an SMC, a Fanatic, or a Berserk unit). The DC is operably Placed if the Placing unit survives, unbroken and unpinned (even if newly berserk), all Defensive First Fire, Subsequent First Fire, and FPF in the Placement Hex; a moving unit stays open to First Fire until it leaves that Location, another unit begins movement, or the MPh ends (A8.1). If it is broken, pinned, or eliminated before or during the Placement, the DC is not operably Placed, stays with the unit (or in its Location if the unit is eliminated), and may not attack. No Placement to an adjacent Location out of LOS (A7.21) [EXC: Cave, G11.8331].
- **A23.4:** for a Placed or Thrown DC, a CX Placing or Throwing unit's +1 DRM applies to the attack's DR (or the Position DR, C7.346). An operably Placed DC detonates in the AFPh on any Original DR under its removal number and is removed from play; an Original DR at or above it removes the DC without detonation (a Thrown DC's second DR is A23.6's exception). The removal number is A23.2's: 12; 10 for a non-elite user (the captured-SW penalty, A21.11), a penalty doubled with a captured DC (8 for a non-elite user, 10 for an elite one), one lower again for Inexperienced Personnel. ASOP 5.22A makes the Placed DC's attack a step of that AFPh.
- **What each side knows:** a Placement is movement the opponent watches, and the DC's attack is public; whether a unit carries a DC, whether it is Infantry, and A11.6's exemption (an SMC, a Fanatic, a Berserk unit) are the placer's owner's while it is concealed.

## 2. What decides it today

Inside Rules:
- `ScenarioA1DemolitionChargeRules.PlacementShort` (`:17`) **decides** the Placement's MF against the MF left (Assault Movement included); the MF cost itself is not in Rules. Its other members are other rules' (`DoubleTimeMf`, A4.5, cited as A4.61; `AllConcealed` A23.1; `ThrowPhase` and `FiredRefusal` A23.6 and A7.351).
- `ScenarioA1FireCalculator`: `DemolitionChargeOutside` (`:986`) admits a Placed DC only in the phasing side's AFPh at range 1; `AssaultWeaponRemoval` (`:1616`) gives A23.2's removal number; `:3857` and `:1994` **decide** A23.4's malfunction on the IFT DR and on the Position DR, for a Placed and a Thrown DC; the DC branch of `:3690` and `:2171` **decide** A23.4's CX +1, for both (`:3690` also holds the ordinary firers' A4.51 CX, not this claim's).
- `ScenarioA1FireEligibility.DemolitionCharge` (`:671`) admits only an operable placement and takes its CX; `DcPlacementVehicleRefusal` (`:528`) is A23.5's.
- **Three window decisions:** `ScenarioA1SequenceCalculator.PlacedChargeDue` (`:257`) keeps the AFPh open while an operable DC can detonate; `TurnObligationRules.Evaluate`, its Placed DC case (`:127` to `:133`), the detonation deadline; `TurnPlacedDcWindowRules` (`Matches`, `Assess`), the window with the AFPh opening's evidence (Drift, WP, Gusts, Gliders: the ASOP's steps).
- `ScenarioA1MovementCalculator.SpecialAction` (`:466`) bars a berserk placer (A15.431's) and a placer in Bypass, which A23.3's text does not say (it names Bypass only for the cost): a simplification, named in the wiring row.

Outside Rules (track 3's, the wiring row):
- `Play/GamePlanner.DemolitionCharges.cs`: `:37` the PFPh-fired bar (with berserk, Unarmed, A22.3's once per Player Turn at `:42`, other rules'), no explicit pinned bar; `:52` the ADJACENT and LOS bar through `IsAdjacent`, and `target == from` (A23.61's); `:64` and `:115` to `:131` the PAATC through the overrun calculator's `NeedsPaatc`; `:71` the MF cost through `EntryCost`; `:99` the CX at Placement; `:154` the AFPh check again.
- `Play/GamePlanner.cs:803` (the AFPh hold), `GamePlanner.PlacedDcRoute.cs:134`, `GamePlanner.OpeningReview.cs:32`, `Units/State/GameProjector.DecisionWindows.cs:14`: the windows' callers. `Play/LiveFire.cs:424` reads operable and CX. `Play/GamePlanner.Fire.cs:927` removes the DC after its attack.
- `Units/State/GameProjector.cs:4966` to `:5025`, `KeepPlacedCharges`, **decides** operability: settled at the placer's own `MovementEnded` (`:5011`), earlier than A8.1's moment, unless broken, pinned, captured, or eliminated.

## 3. Rulings

- **RA23.1 One calculator.** `ScenarioA1DemolitionChargeRules`, held whole, gains A23.3's and A23.4's members; `ScenarioA1DemolitionChargeContract.cs` holds two contracts. The file's other members stay as they are; `DoubleTimeMf`'s citation is corrected to A4.5.
- **RA23.2 The MF cost.** `PlacementCost(directEntryMf)`: the MF the unit would need to enter the target Location directly during this MPh without Bypass, gathered as a fact. `PlacementShort` keeps comparing it with the MF left.
- **RA23.3 Who may Place.** `PlacementRefusal(PlacementFacts)`: a carrying Infantry unit; during the MPh; a target Location ADJACENT and in its LOS (a Cave, G11.8331, is not built: refused); not fired during the PFPh (Bounding First Fire is vehicle fire, not this bar); not pinned before the Placement this MPh. Other rules' bars, which the caller asks too: berserk (A15.431), Unarmed, Opportunity Fire marked or a labor task attempted (A3.3, A7.25: a Placement is movement), once per Player Turn (A22.3), Wire (B26.45), Panji (G9.211), the own Location (A23.61).
- **RA23.4 The PAATC.** `PlacementNeedsPaatc(enemyAfvInTarget, placerNeedsPaatc)`: an enemy AFV in the target Location and a placer A11.6 does not exempt (A11.6's answer, gathered as a fact).
- **RA23.5 Operable.** `PlacementOperable(OperabilityFacts)`: the placer survived, unbroken and unpinned (newly berserk is fine), every Defensive First Fire, Subsequent First Fire, and FPF in the Placement Hex, settled once it leaves the Placement Location, another unit begins movement, or the MPh ends (A8.1); not operable if broken, pinned, or eliminated before then. Capture cannot come before that moment (A20.2: the RtPh, the CCPh, Mopping Up), and capture after it has no effect. `UnplacedDcStays(eliminatedOrCaptured)`: with the unit, or in its Location; a captured unit first abandons its SW (A20.24). The projector's earlier moment (its own `MovementEnded`) is a behavior change of the wiring.
- **RA23.6 The CX DRM.** `DcCxDrm(cxAtUse)`: +1 to the attack DR or the Position DR, for a Placed or a Thrown DC. CX at Placement is the placer's CX status at that moment, from any cause: a Double Time declared before it (A4.5), a deep stream (B20.43); not one declared after it (the A23.3 example), and not one from an earlier turn, which leaves at the start of the MPh (A4.51). Only the DC branch of the fire calculator's `:3690`, and `:2171`, forward to it (shared hold, edges only).
- **RA23.7 Detonation.** `DetonationWindow(phase, ownerPhasing, operable, placedThisPlayerTurn)`: the owner's AFPh of the Player Turn it was Placed, operable. `DcMalfunction(originalDr, removalNumber)`: at or above the removal number, removed without detonation; under it, it detonates. The removal number stays `AssaultWeaponRemoval`'s (A23.2). Detonation is mandatory in that AFPh (A23.4, ASOP 5.22A): there is no carry-over, and the AFPh does not end while it is due (`PlacedChargeDue`). Either way the DC is removed from play (`RemovedAfterAttack`). The fire calculator's `:3857` and `:1994` forward to `DcMalfunction` for a Placed and a Thrown DC alike; A23.6's second-DR exception stays A23.6's.
- **RA23.8 The windows.** `TurnPlacedDcWindowRules.Assess` keeps its AFPh-opening evidence, the ASOP's, and asks `DetonationWindow` for the window itself; its answers do not change. `TurnObligationRules.Evaluate`'s Placed DC case keeps its answer: it is an obligation's deadline (due from the AFPh on, overdue after), not the window, so it cannot forward with the same answer; its comment names `DetonationWindow`, and the wiring row lists it. `PlacedChargeDue` forwards to `DetonationWindow` with the AFPh, the owner phasing, and "placed this Player Turn" as constants, since the projector clears every placement at the CCPh: its `operable` means operable this Player Turn.
- **RA23.9 Old members.** Forwarding, answers equal: the fire calculator's DC CX and malfunction sites, `TurnPlacedDcWindowRules.Assess`'s window, `PlacedChargeDue`. Keeping its answer, named: `TurnObligationRules`' Placed DC deadline. Unchanged: `PlacementShort`, `DemolitionChargeOutside`, `DemolitionCharge`. Outside Rules, every site of section 2 moves in the wiring.
- **RA23.10 Contracts.** `a23.3-dc-placement` (may this unit Place its DC there now?): `placerId` (Unit, All); `carryingDc`, `infantry`, `paatcExempt` (Owner, concerning `placerId`); ADJACENT, LOS, fired in the PFPh, pinned, the phase, a manned unconcealed enemy AFV there (All). The MF cost is a fact the wiring gathers for `PlacementCost` and `PlacementShort`; the contract asks only who may Place. Outcomes `may-place`, `may-place-after-paatc`, `refused`, `not-evaluated`. And `a23.4-dc-detonation` (what does this Original DR do?): operable, the phase, owner phasing, placed this Player Turn, the removal number, the DR (All). Outcomes `detonates`, `removed`, `not-due`, `not-evaluated`.

## 4. Out of the claim

Nothing to the backlog. A23.1, A23.2, A23.41, A23.5, A23.6, A23.61, A23.7, A15.431, A22.3, B26.45, G9.211, and G11.8331 are their own rows.

## 5. The wiring row W-A23.3

- `Play/GamePlanner.DemolitionCharges.cs` `:37`, `:52`, `:64`, `:71`, `:99`, `:115` to `:131`, `:154`: ask `PlacementRefusal`, `PlacementNeedsPaatc`, `PlacementCost`, the CX at Placement, and `DetonationWindow`; check adjacency before the vehicle bar.
- `Units/State/GameProjector.cs` `KeepPlacedCharges` (`:4966` to `:5025`): ask `PlacementOperable` at A8.1's moment, and `UnplacedDcStays`.
- `Play/GamePlanner.cs:803`, `GamePlanner.PlacedDcRoute.cs:134`, `GamePlanner.OpeningReview.cs:32`, `GameProjector.DecisionWindows.cs:14`, `Play/LiveFire.cs:424`, `Play/GamePlanner.Fire.cs:927`: the windows, the operable and CX facts, `RemovedAfterAttack`.
- `ScenarioA1MovementCalculator.SpecialAction`'s Bypass bar: a simplification to settle with the movement claim.
- Old members to remove once callers move: none in Rules.
- Behavior changes: operability settles when another unit begins movement or the MPh ends, not at the placer's own move's end; an explicit pinned bar; a Cave refused until G11.8331.

## 6. Tests and minigame

- `ScenarioA1PlacedDcCalculatorTests`: each refusal; the PAATC need; operability with First Fire after the stack stops but before another unit moves, a newly berserk placer, elimination; where an unplaced DC stays (eliminated, captured); the CX DRM (Double Time before and after, a CX from an earlier turn); the window; detonation and removal at the removal numbers 12, 10, 8 (and 11 Inexperienced); each forwarding site equal to its old answer, the Thrown DC's included.
- The contracts: outcomes, not-evaluated cases, and the placer's Owner facts hidden from the opponent.
- A new bundle `a23-placed-dc` on an imported board: A23.3's example (Y6 to X5, two MF, operable, detonates in the AFPh), a placer that fired in the PFPh, a pinned placer, an enemy AFV's PAATC, a captured DC's removal number.

## 7. Memory and performance

Pure static checks over a few facts; no state, cache, or event.

## 8. Build outputs outside the repository

The filtered test builds in `asl-gate-work`, registered as made, in the worktree `asl-gate-work/wt-a23`; a Studio check of the bundle on the Rule Workbench in Claude's Studio (6670), stopped when done.

## 9. Review of the design

A sub-agent read the first draft against pp. 90 and 91 (A4.5, A4.51, A8.1, A20.2, A20.24, A21.11, A23.2, A3.3, A7.25, C7.346, ASOP 5.22A) and the code, read-only (2026-10-29): twelve findings, all taken. Blocking: operability settles at A8.1's moment, and the projector's earlier one is a behavior change (RA23.5); capture is A20.24's and cannot come first (RA23.5); `TurnObligationRules` decides the window a third time, now held shared and forwarded (RA23.8); the CX and malfunction sites serve a Thrown DC too, so the members cover both (RA23.6, RA23.7); the placer's DC, Infantry, and A11.6 facts are its owner's (RA23.10). Also taken: detonation is mandatory with no carry-over (RA23.7); `PlacedChargeDue`'s forward decided (RA23.8); the full removal numbers (section 1); A4.5 and A4.51, CX from any cause (RA23.6); the other rules' bars, and the Bypass bar named (RA23.3, the wiring row); the missing sites; the missing tests and the contract's fact.

## 10. As built (2026-10-29)

- `ScenarioA1DemolitionChargeRules`: `PlacementCost`, `PlacementRefusal`, `PlacementNeedsPaatc`, `PlacementOperable`, `UnplacedDcStays`, `DcCxDrm`, `DetonationWindow`, `DcMalfunction`, `RemovedAfterAttack`; `DoubleTimeMf`'s citation corrected to A4.5. `ScenarioA1DemolitionChargeContract.cs`: the contracts `a23.3-dc-placement` and `a23.4-dc-detonation`, the record `DcPlacementFacts`, the enum `DcHeldBy`.
- Forwarding with the same answer: the fire calculator's DC malfunction (`:1994`, `:3857`) and DC CX (`:2171`, the DC branch of `:3690`) sites; `ScenarioA1SequenceCalculator.PlacedChargeDue`; `TurnPlacedDcWindowRules.Assess`'s window. Keeping its answer, named: `TurnObligationRules`' Placed DC case, a deadline (due from the AFPh on, overdue after) rather than the window. The members read the phase lower-case, as the old ones do; the contracts lower-case it.
- Tests: `ScenarioA1PlacedDcCalculatorTests`; the bundle `a23-placed-dc` (nine checks); the old DC tests in Rules and Play pass unchanged.
- The wiring row W-A23.3 is in the inventory's section 7.

## 11. Review of the build

A sub-agent read the build read-only (2026-10-29): every forward gives the old answer on every input. Five minor findings, taken: RA23.8 and RA23.9 say that `TurnObligationRules`' deadline keeps its answer; RA23.10 drops the MF facts from the contract; the Placement contract refuses when the known facts already make the refusal certain; the AFV is a manned, unconcealed one (A11.6); W-A23.3's capability is movement.
