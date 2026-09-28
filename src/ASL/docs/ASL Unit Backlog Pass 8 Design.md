# ASL Unit Backlog Pass 8 Design

**Status:** Built. Backlog pass 8 (Guns, part 2), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 8.1 to 8.10.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 8 Review](<Scenario A1 Backlog Pass 8 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 7 Design](<ASL Unit Backlog Pass 7 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1 and 18.

Rulings R8.1 to R8.12 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Guns in the movement windows (8.1).** In the DEFENDER's window after a moving unit's or vehicle's MF or MP expenditure, a Gun manned by its crew, or a tank's MA, Defensive First Fires at the moving units in their Location (C6.1, A8.1). Case J2 (+4) applies when the vehicle has spent at most one MP in the firer's continuous LOS, Case J1 (+3) at most three, else Case J (+2); the MP are counted back through the vehicle's steps this MPh to the last Location the firer could not see, and MP an earlier shot there claimed are spent (C6.11 to C6.17). Against Infantry, Cases J3 and J4 (-1 each) replace FFNAM and FFMO, which only a Critical Hit keeps (C3.71). A Gun fires no more often at a stack in a Location than the MF or MP it spent there. A Gun whose colored dr exceeds its ROF is marked First Fire and fires once more that Player Turn only as Intensive Fire (C2.241); one not so marked keeps its Multiple ROF in the DFPh.
- **Intensive Fire (8.2).** A Gun that has used its normal ROF, marked Prep or First Fire, fires once more in the PFPh, MPh, or DFPh, not after a Final Fire counter, never in the AFPh, and not with a pinned crew: Case F (+2), its B# two lower, and an Intensive Fire counter (C5.6 to C5.63).
- **Guns as targets (8.3).** A Gun's crew alone in its Location takes the gunshield's +2 against fire from within the Gun's CA, or the +2 Emplacement TEM of a Gun that set up manned by a crew and has not moved or been hooked up, instead of a lower positive TEM (C11.2, C11.5). An HE hit adds the Gun's Target Size and its Emplacement to the To Hit DR; its IFT DR before the gunshield destroys the Gun and its crew on a KIA, malfunctions it on a K, and a Critical Hit destroys both; on a Near Miss the gunshield adds +2 (C11.4, C11.6).
- **Crews' own fire (8.4).** A crew fires its inherent FP like other Infantry, unless it fired its Gun this Player Turn (A7.352); a squad keeps its FP.
- **Concealed Guns (8.5).** A Gun sets up concealed or hidden with its crew; firing or a CA change in the view of a Good Order enemy ground unit within 16 hexes loses both "?" (A12.14, A12.141).
- **Gun movement (8.6).** A Good Order, unpinned crew pushes its QSU Gun into Open Ground or grain at double MF with a Manhandling DR against the M# (+the MF expended, -2 along a road): below it they enter, at it they enter and stop, above it they stay; the Gun and crew become TI (C10.3). A Stopped truck or halftrack whose T# is at most the Gun's M# hooks it up for half its MP with the crew on foot; the Gun is towed at +1 MP per hex, cannot fire, and is destroyed with its vehicle; unhooking costs half the MP again and returns the Gun to its crew (C10.1 to C10.12). A crew that walks away abandons its Gun.
- **Levels and boards (8.7).** A shot the C2.6 elevation limit forbids is refused; the CA and Target Facing bearings are read on the composed map, across boards and on reversed boards.
- **Special shots (8.8).** Case E (+2, +4 in woods or a building) in the Gun's own Location; Case H (+2) for a squad or HS manning a Gun of its nationality; Case M (-2) at the Location a Scenario Defender's Gun Bore Sighted at setup, while its original crew fires it from its setup Location, the firer taking Case M or N; a shot may turn to the facing the fewest hexspines give.
- **Covered Arc tools (8.9).** A Gun whose crew could still fire it changes its CA without firing in its side's fire phase and fires no more that phase; in the PFPh it and its crew do not move that Player Turn. The page lists each enemy Location with its range and whether it is in the CA or how many hexspines the Gun must turn, and draws the CA's two rays on the map.
- **Overstacking and catalog (8.10).** A Gun or tank firing from an overstacked Location adds +1 per squad equivalent over, and Personnel attacked in an overstacked Location subtract 1 per squad equivalent over (A5.12, A5.131). Catalog 1.7.0 adds each Gun's BPV, dates, and Animal-Pack capability (note O).

## 2. The packages

All four Scenario A1 packages are revised for catalog 1.7.0 with their prior manifest digests kept. The Ordnance package adds `OrdnanceShot.FireKind` (`first-fire`), `IntensiveFire`, `Movement` (MP in LOS, MP claimed, non-Assault Movement, Open Ground, MF spent and shots in the Location), `SameHex`, `NonQualified`, `BoreSighted`, `CrewSeen`, and the overstacking counts; `OrdnanceGun.FirstFire`, `FinalFire`, and `IntensiveFired`; and `OrdnanceResolution.GunTargetFate`. The Fire package adds `FireAttack.GunTarget` (the Gun whose crew is the target, with its Emplacement and gunshield), `FireFirer.GunFired`, admits ordnance hits in the MPh and a crew as a firer, and adds FFNAM and FFMO to a Critical Hit of Defensive First Fire. New fragments come from `asl-scenario-a1.pass8-pdf-comparison.json` (54 subjects); new cases: `A1-fire-gun-crew-target`, `A1-fire-crew-inherent-fp`, `A1-ordnance-defensive-first-fire`, `A1-ordnance-intensive-fire`, `A1-ordnance-gun-target`, `A1-ordnance-special-shots`, and `A1-ordnance-overstacking`.

## 3. The game model and records

- `game-started` may carry `scenarioDefender`; `GameState.ScenarioDefender`.
- **`bore-sighted`** (`BoreSighted`: Gun, Location, crew, setup Location), made at setup from a Gun placement's `boreSighted`; `GameState.BoreSights`.
- **`gun-turned`** (`GunTurned`), **`manhandling-rolled`** (`ManhandlingRolled`, recomputed on replay), and **`gun-hooked`** (`GunHooked`); `MovementStepped.PushedGun`.
- `GameState.GunCrewsFired` (Player Turn), `OrdnanceShotsHere` (per Location of the moving stack, with the MP claimed), `UnemplacedGuns`, and `NoMoveThisPlayerTurn`.
- `ordnance-fired` is admitted in the MPh and for an Intensive Fire shot after the ROF is spent; a pushed Gun moves with its crew, a Gun of a crew that walks away is released, a towed Gun follows its vehicle and is destroyed with it; TI leaves at the end of the Player Turn.
- Actions **`asl.game.turn-gun`** and **`asl.game.hook-gun`**; `asl.game.fire-ordnance` takes `intensive`, `asl.game.move` takes `pushGun`.
- Vocabulary 1.12.0: the state `asl:intensive-fire` (IF), the attribute `asl:dates`, the trait `asl:animal-pack`, and `asl:bpv` on Guns.

## 4. The Play page

- Setup names the Scenario Defender, and a Gun placement may name its Bore Sighted Location; a Gun is concealed or hidden like its crew.
- The Ordnance panel appears in the DEFENDER's window in the MPh, offers Intensive Fire and a turn without firing, and lists each enemy Location against the chosen Gun's CA; the map draws the CA.
- The move panel offers to push the Gun a single checked crew mans; the vehicle panel offers to hook up or unhook a Gun in the vehicle's hex.
- The RPh records list Manhandling DRs, CA changes, and hook-ups.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass8Tests` (12).
- Play: `BacklogPass8Tests` (14); the action list, and tests updated for crews that may now move.
- Studio: `PlayPageVehicleTests.TheGunPanelListsTargetsWithTheirCoveredArcAndOffersTurningIntensiveFireAndPushing`.
- Authoring: `BuildPass8` verifies the 54 subjects; the matrix tests pin the new digests and fragment counts.
- Units: catalog 1.7.0, vocabulary 1.12.0; rendering goldens for the Intensive Fire state. CounterSheets: the six new rows.

## 6. Not in this pass

Section 18 of the backlog lists what this pass defers: Gun Duels, OVR Prevention, a vehicle's Intensive Fire, C5.51's Case E in the MPh, the Area Target Type against a Gun, AP and HEAT at Guns, a crew with other units as a target, an unmanned Gun as a target, Random SW Destruction of Guns, Hazardous Movement for a pushing crew, the Labor counter, more than one crew pushing, Passengers, Recovery and captured Guns, Multiple Hits, levels in fire, a Target Facing change restarting the C6.17 count, the TCA of tanks set without fire, and vehicle overstacking.
