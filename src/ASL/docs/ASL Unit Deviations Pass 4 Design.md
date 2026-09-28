# ASL Unit Deviations Pass 4 Design

**Status:** Built. Unit step 25 (vehicles): trucks and an open-topped halftrack that move, fire, and are fired on, as the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), section 10, set out.

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 25, and acceptance scenario U29 (section 14).

**Related documents:** the [Scenario A1 Vehicle Review](<Scenario A1 Vehicle Review 2026-09-27.md>) (the review stage, with both second-pass reports), the [ASL Unit Deviations Pass 3 Design](<ASL Unit Deviations Pass 3 Design.md>), the [Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>), section 9.6 (catalog 1.5.0), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 14.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Vehicles.** Catalog 1.5.0 adds the German Opel 6700 (Blitz) and Russian GAZ-MM trucks and the German SPW 251/1 halftrack (Chapter H, pp. 339 and 356). A vehicle is placed at a Location with its VCA pointing at a hexspine (D2.11).
- **Movement.** `asl.game.move-vehicle` spends one MP expenditure: `start`, `turn` (one hexspine), `enter` (either hex of the VCA), or `stop`, each opening the DEFENDER's window (D2.1, A8.1). A vehicle ends its move in Motion only when it cannot Stop or reach another hex (D2.4); one in Motion needs no Start and must spend an MP.
- **Crew exposure.** `asl.game.button-up` places or removes the halftrack's BU counter in its owner's MPh or APh (D5.33).
- **Fire at vehicles.** An Infantry attack at a Location with a vehicle resolves it on the IFT Vehicle line of the attack's column (A7.308, A7.309), or, for the halftrack, attacks its CE crew Collaterally with +2 (D.8B, D5.31): Stun, Stun +1, Recall, or a pin.
- **Fire by vehicles.** The halftrack's AAMG fires alone on the IFT with its Multiple ROF and breakdown (D1.83, D3.5, D3.7).

## 2. Catalog 1.5.0 and vocabulary 1.8.0

Three definitions of kind `asl:vehicle`, 129 rows, and the trait `asl:cs-passengers-only` (section 9.6 of the catalog design). The Fire reference reads a vehicle's movement type, MP, armor, open top, and MA from the catalog; the AFV crew's Morale Level is its nationality's best elite MMC (D5.1). Vocabulary 1.8.0 adds the vehicle states. Games that name catalog 1.4.0, including the Studio's demo games of earlier passes, no longer replay.

## 3. The packages

The Fire package gains the IFT Vehicle line (its own transcription and digest), `FireAttack.Vehicles` and `FireAttack.VehicleFire`, two roll kinds (`crewCheck`, two dice; `unlikelyKill`, one die), and `FireResolution.VehicleEffects`. Four cases join the matrix, and the prior manifest digest is kept. The Rally, Close Combat, and Ordnance packages are republished for the catalog digest.

## 4. The live records

- **`vehicle-step`** (`VehicleStepped`): the vehicle, the kind, the Location after the step, the new VCA for a turn, the half MP, and the step number. The projector (UNIT-STATE-034) checks the phase, the vehicle's state, the kind against whether it is moving, and the MP allotment, and opens the window. `MovementState` gains `Vehicle`, `Started`, and `Stopped`.
- **`crew-exposure-changed`**: a `ConditionsChanged` for `asl:bu`; the planner allows one per vehicle per phase.
- **Fire records.** A vehicle's AAMG record names the vehicle as its firer; a kept Multiple ROF is tracked with the Guns' shots. A7.55 binds the AAMG and its Location's Infantry, apart from its own ROF. Vehicle effects are `instance-eliminated` (eliminated or burning) or `conditions-changed` (immobilized; Stunned, BU, not in Motion; Recalled; pinned).
- **Turn end.** At a new Player Turn a Stun becomes Stun +1 and a Recalled vehicle leaves play as eliminated.
- **Gate readback.** A vehicle step's window and Location, and the BU condition, are checked after commit.

## 5. The planner's refusals

Setup refuses vehicles without a VCA, concealed or hidden, above ground level, outside Open Ground, Grain, and roads, stacked with another vehicle, or with an enemy unit. Infantry may not move or advance into an enemy vehicle's Location; CC in a Location with a vehicle, a Gun's shot at one, and fire at Infantry with an AFV or past one are refused (R25.9, R25.10). A berserk charge at a vehicle ends in place.

## 6. The Play page

- Setup offers a VCA for a vehicle and lists it.
- In the MPh, a vehicle movement panel shows the chosen vehicle's VCA, MP spent and left, and whether a Start is due, and offers only the expenditures it may make; each disabled entry names its reason. Vehicles are left off the Infantry move list.
- In the MPh and APh, a crew exposure panel offers the halftrack's BU counter.
- The Fire panel offers the halftrack's Location in the fire phases while its crew is CE; the fire record shows each vehicle's Vehicle line result and its crew's Collateral Attack with its DRM and check.
- The units table shows a vehicle's VCA, MP of its allotment, and CE.
- On the map, a vehicle's counter carries a badge for each vehicle state, in both style sheets: Imm (IMM), Stun (STUN), Stun+1 (+1), and Recall (RCL), beside the existing BU, CE, and Motion badges. A Recalled crew is marked Recalled alone; every check reads Stunned or Recalled.

## 7. Tests

- ScenarioA1: `ScenarioA1VehicleFireTests` (16): the Vehicle line, Unlikely Kill, Collateral Attacks with Stun, Recall, and the Casualty MC, the AAMG's modifications and ROF, and the outside cases.
- Play: `VehicleStepsTests` (11): U29's truck and halftrack, Motion, the Stun lifecycle, the refusals, setup, and the table player's fixes; the action list.
- Studio: `PlayPageVehicleTests`: placement with a VCA, the panels, and the Start and Stop buttons.
- CounterSheets, Units, and Studio catalog lists updated for catalog 1.5.0 and vocabulary 1.8.0.
- Authoring: `BuildVehicles` verifies the 45 comparison subjects, and the four matrix tests pin the new digests (the Fire matrix's 186 fragments).
- Rendering: goldens for the four new states on the example halftrack, in both style sheets.

## 8. Not in this pass

Everything the rulings leave out is in the backlog, section 14: Reverse movement and the other Chapter D movement, Residual FP against trucks and CE crews, wrecks, closed-topped AFVs and To Kill, an AFV's TEM and Hindrance for Infantry, vehicle fire in the MPh, vehicles in CC, Rally, and Rout, and vehicle concealment.
