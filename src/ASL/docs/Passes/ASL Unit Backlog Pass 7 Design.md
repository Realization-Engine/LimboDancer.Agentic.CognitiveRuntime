# ASL Unit Backlog Pass 7 Design

**Status:** Built. Backlog pass 7 (armor), as the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 7.1 to 7.5.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 7 Review](<Scenario A1 Backlog Pass 7 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 6 Design](<ASL Unit Backlog Pass 6 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 1 and 17.

Rulings R7.1 to R7.12 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Tank counters (7.1).** Catalog 1.6.0 adds the German PzKpfw IIIH (`attacker-tank`, German Vehicle Listing, p. 337) and the Russian T-34 M41 (`defender-tank`, Russian Vehicle Listing, p. 355), 43 attributes each, read row by row with the Listings Key (p. 338): AF, turret armor, MA type, caliber and length, ROF, B#, Special Ammunition with its Depletion Numbers by year, BMG and CMG, and CS#. The synthetic catalog gains two matching tanks.
- **Vehicle Target Type (7.2).** A Gun or a tank's MA fires at one named enemy vehicle on the C3 Vehicle row (p. 700), black or red, with the C4 modifications and APCR's (C4.3). Built To Hit Cases: A (a turret turns for +1 per hexspine, T, or +2 then +1, ST and RST), B, D, CX, I (+1 for a BU AFV firing its MA), J (+2 against a moving target), K (+2 against a concealed vehicle), L (not against a Non-Stopped or Motion target), N, P (target size), Q, R, and the "+1" counter after a Stun (D5.34). An improbable hit's subsequent dr picks turret or hull (C3.6).
- **To Kill (7.3).** The hit strikes the turret when the colored dr is below the white dr on a turreted target, else the hull (C3.9). The Target Facing comes from the target hexside the LOS crosses, against the VCA for the hull and the TCA for the turret, the facing less favorable to the firer along a hexspine (D3.2). The AF is the printed front or side AF, a superior or inferior turret one step up or down (D1.63, D1.64). The Basic TK# of AP, APCR, HEAT, or HE (the tables of p. 701, with their colored nationality entries), doubled on a Critical Hit, +1 against the rear, plus the Case D range change, less the AF, gives the Final TK#; against an unarmored vehicle the table's unarmored value. The To Kill DR burns, eliminates, immobilizes, Shocks, or gives a possible Shock (C7.7, C7.41); an Original 12 is a dud. Special Ammunition follows its Depletion Number: below it used, at it used and run out, above it none, nothing fired, and never again (C8.9).
- **MA fire (7.4).** A tank fires its MA as a Gun manned by its own crew, in its side's PFPh, AFPh, or DFPh, at Infantry with HE or at a vehicle. Its turret turns to bring the target in, and the TCA then keeps its direction apart from the hull (D3.12). A closed-topped AFV is BU unless its owner exposes the crew; an RST MA fires only BU (D5.2, D1.321). A Stunned, Shocked, Unconfirmed Kill, Recalled, or Abandoned AFV does not fire, nor one in Motion, nor one that entered a new hex before its AFPh shot (Cases C4 and C are not built).
- **Crew survival and Shock (7.5).** An immobilized vehicle's crew takes a TC, Elite morale for an AFV and 1st Line for an unarmored vehicle (D5.1), and Abandons it on failure, its crew placed beneath it (D5.5). An eliminated AFV that does not burn rolls for Crew Survival against its CS# (D5.6), +1 for a Stunned, Shocked, or Recalled crew or a "+1" counter. A Shocked AFV is BU and stopped; in the next RPh its dr removes the Shock (1, 2) or makes it an Unconfirmed Kill (3 to 6), whose next RPh dr removes it (1 to 3) or wrecks it (4 to 6) (C7.42). An Unconfirmed Kill is still Shocked: like a Stunned AFV it does not move, expose its crew, fire, or repair. A concealed tank loses its "?" when it fires, and a tank keeps its Acquisition as a Gun does.

## 2. The packages

All four Scenario A1 packages are revised for catalog 1.6.0 with their prior manifest digests kept. The Ordnance package pins two new transcriptions, `c3-to-hit-vehicle-row.transcription.json` (p. 700) and `c7-to-kill-tables.transcription.json` (p. 701), reads the tanks as Guns of their caliber (`GunType` `vehicle`, MA type, Special Ammunition), and resolves a shot with `OrdnanceShot.VehicleTarget`, `Vehicle` (the firing AFV's BU, Motion, Stun, Shock, Recall, "+1", and move), `Ammunition`, and `ScenarioYear`. `ScenarioA1ArmorCalculator` is the Vehicle Target Type path; `OrdnanceResolution.Kill` reports the hit location, Target Facing, the TK arithmetic, the To Kill DR and result, and the NTC, TC, and Crew Survival; `AmmunitionUse` reports `used`, `depleted`, or `none`. New roll keys: `toKill`, `shockCheck`, `crewCheck`, `crewSurvival`. The Fire package admits a closed-topped AFV in the target Location, its CE crew alone Vulnerable. New fragments come from `asl-scenario-a1.pass7-pdf-comparison.json` (50 subjects); new cases: `A1-fire-closed-topped-afv`, `A1-ordnance-vehicle-hit`, `A1-ordnance-vehicle-outside`, `A1-ordnance-special-ammunition`, `A1-ordnance-tank-fire`, `A1-ordnance-shock-and-crews`, and `A1-ordnance-unarmored-vehicle`.

The source comparison tool now strips only Markdown tags from a fragment, never an escaped dice sign (`\<`, `\>`), and never touches the page text; `AslScenarioA1FireSourceReview` uses that method for the pass 7 comparison and keeps the earlier one for the batches recorded with it.

## 3. The game model and records

- `game-started` may carry `scenarioYear` (1939 to 1945); setup passes it from `start.scenarioYear`; `GameState.ScenarioYear` holds it.
- `GameState.TurretFacings` (`TurretFacing`: vehicle and facing) holds each TCA that differs from its VCA; an `ordnance-fired` record of a tank sets it from the record's facing.
- `GameState.DepletedAmmunition` (`DepletedAmmunition`: Gun or AFV, ammunition) holds Special Ammunition run out or found missing.
- `ordnance-fired` accepts a tank as Gun and crew. A shot with no Special Ammunition changes nothing but the depletion; the gate reads that back.
- The record **`shock-recovery-rolled`** (`ShockRecoveryRolled`: vehicle, roll, result) with `GameState.ShockRollsThisPhase`; replay recomputes the result from the dr.
- A To Kill result follows the record as ordinary events: `vehicle-wrecked` with a Blaze, `instance-created` for a surviving or Abandoning crew (a crew counter of the vehicle's nationality), and `conditions-changed` for immobilization, Abandonment, Shock, and a lost "?".
- The action **`asl.game.recover-shock`** (vehicle) rolls in the RPh; the RPh does not end while a Shocked AFV or an Unconfirmed Kill owes its dr. `asl.game.fire-ordnance` takes `targetVehicle` and `ammunition`.
- Vocabulary 1.11.0 adds the states `asl:shocked` (SHK) and `asl:unconfirmed-kill` (UK).

## 4. The Play page

- Setup offers the scenario year.
- The Ordnance panel offers the firing side's tanks, labeled with VCA, TCA, BU or CE, fire, Acquisition, run-out ammunition, and what stops them; a target Location with enemy vehicles offers each on the Vehicle Target Type with a choice of AP, APCR, HEAT, or HE.
- The ordnance record reads out the hit location, Target Facing, TK arithmetic, To Kill DR and result, and the crew's checks, and the Depletion Number's outcome.
- The RPh panel offers each Shocked AFV and Unconfirmed Kill its dr.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass7Tests` (21), including every roll sequence of three vehicle shots.
- Play: `BacklogPass7Tests` (11); the action list.
- Studio: `PlayPageVehicleTests.ATankIsOfferedInTheGunPanelWithTheEnemyVehicleAndItsAmmunition`; the catalog choices.
- Authoring: `BuildPass7` verifies the 50 subjects; the matrix tests pin the new digests, the two transcriptions, and sample TK values.
- Units: catalog 1.6.0 and the synthetic tanks; vocabulary 1.11.0. Rendering: goldens for the two new states, in both style sheets. CounterSheets: the 86 new rows.

## 6. Not in this pass

Section 17 of the backlog lists what this pass defers: the BMG and CMG, a tank's MA in CC, OVR, and the MPh, Cases C and C4, HD, Smoke, Intensive Fire, Deliberate Immobilization, Bounding First Fire by ordnance, Elite Depletion Numbers, a red CS#, D5.5's second trigger, an NT AFV's upper superstructure, the Recalled AFV's "+1" fire, and radioless AFVs.
