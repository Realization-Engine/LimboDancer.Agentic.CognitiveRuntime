# ASL Unit Deviations Pass 3 Design

**Status:** Built. Unit step 24 (ordnance): a Gun firing HE at Infantry, as the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), section 9, set out.

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 24, and acceptance scenario U28 (section 14).

**Related documents:** the [Scenario A1 Ordnance Review](<Scenario A1 Ordnance Review 2026-09-27.md>) (the review stage, with both second-pass reports), the [ASL Unit Deviations Pass 2 Design](<ASL Unit Deviations Pass 2 Design.md>), the [Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>), section 9.5 (catalog 1.4.0), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 12.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Guns and crews.** Catalog 1.4.0 adds the German 7.5cm leIG 18 and the Russian 45mm PTP obr. 32 (Chapter H, pp. 351 and 363) and a 2-2-8 Infantry crew of each side (A1.123, p. 695). A Gun is placed at a Location, facing a hexspine (C2.23), and manned by its crew there (A21.13).
- **A Gun's shot.** In the PFPh, AFPh, or DFPh, `asl.game.fire-ordnance` fires a manned Gun's HE at the enemy units of a Location on the Infantry Target Type (C3.3, C3.32). The shot turns the Gun when the target is outside its Covered Arc (C3.21), with Case A.
- **Its effects.** A hit attacks every enemy unit there on the IFT column of the Gun's HE FP (C.6), with one Effects DR and no TEM or Hindrance (C.3); a Critical Hit doubles the FP and reverses the TEM for the unit Random Selection picks (C3.71, C3.74).
- **Its state.** An Original colored dr at most the ROF lets the Gun fire again that phase (C2.24); an Original DR at or above its B# of 12 malfunctions it (C2.28); each shot acquires the target Location for the next (C6.5).
- **A pass 2 defect.** The live Fire and Rally planners never drew the NTC rolls of a berserk leader's companions (A15.41), and would have asked for them forever. Both planners and both record readers now handle them.

## 2. Catalog 1.4.0

- **Rows.** 78 rows under sheets OLG, OLR (the Chapter H listings), and NCC, confirmed value by value by the referee (the review's "Counters" section). The Guns' malfunctioned sides are not in any source and stay not-in-source; Gun repair is not built.
- **Vocabulary.** No change: `asl:gun` (facing, faces front, malfunctioned, limbered) and `asl:crew` were declared already.
- **Version.** Added definitions make a minor version: `asl-scenario-a1@1.4.0`. The synthetic catalog gains matching synthetic definitions. Games that name 1.3.0 (the pass 1 demos in `boards/`) no longer replay; the user's `steps-demo` games name 1.2.0 and stopped replaying with pass 1.

## 3. The packages

- **Ordnance (new).** `scenario-a1-ordnance`: 49 fragments, ten cases, the C3 To Hit Table transcription, and rulings R24.1 to R24.8. `ScenarioA1OrdnanceCalculator` is pure: missing facts, then Outside, then Undecided, then the shot, asking for its rolls one at a time: `toHit` (colored die first), `subsequent`, `criticalSelection:<units>`, and the IFT rolls as `critical-hit:<key>` and `hit:<key>`. `Precheck` proves both a normal hit and a Critical Hit on every target are decided before any roll.
- **The To Hit DR.** The Basic TH# by range and color (C3.3), the C4 modifications beyond 12 hexes, and the DRM of Cases A (+3 and +1 per hexspine for a non-turreted Gun, doubled in woods or a building), B, D, K, L, N, Q, and R. A Final DR below half the Modified TH# is a Critical Hit, and so is an Original 2 that hits on a subsequent dr of 1 or at most half the Modified TH# (C3.7; R24.7); when no Final DR can hit, an Original 2 hits on a subsequent dr of 1 to 3 (C3.6).
- **Fire (revised).** `FireAttack.OrdnanceHit` resolves a hit with no firers: the Gun's HE FP column, doubled on a Critical Hit, no Cowering, no halving, no TEM or Hindrance, or the reversed positive TEM on a Critical Hit. The Fire package refuses a crew as a target (R24.3). Its manifest keeps the prior digest.
- **Rally and Close Combat (revised).** Only the catalog digest changes; each manifest keeps its prior digest.

## 4. The live records

- **Event.** `ordnance-fired` (OrdnanceFired): the Gun, its crew, the target, the Gun's new facing when the shot turned it, the ROF kept, the Acquisition after the shot and where, the rolls by key, the facts, and the resolution. Its effects follow as ordinary events caused by it.
- **State.** `GameState.OrdnanceShots` (each Gun's shots this phase and whether its ROF is kept; cleared at every phase change) and `GameState.Acquisitions` (each Gun's acquired Location and level). The Gun and its crew carry the phase's Prep or Final Fire marker from the first shot.
- **Replay.** UNIT-STATE-033: a Gun fires in a fire phase, manned by its crew, and again only on a kept ROF. `IOrdnanceRecordVerifier` rebuilds the state's facts, takes the map reads as recorded (range, Covered Arc, terrain, LOS, the targets' Heat of Battle reads), and requires the package to reproduce the resolution, ROF, and Acquisition.
- **The Covered Arc.** The planner computes the bearing from the Gun's hex center to the target's on flat-topped hexes (odd columns half a hex higher) and turns the Gun the fewest hexspines that bring the target within 30 degrees of its barrel, the boundary rows included (C3.2). It is read on one unreversed board only.
- **Crews.** A crew manning a Gun may not move or advance (R24.4); crews and Guns are not targets (R24.3).

## 5. The Play page

- **Setup.** A Gun is placed with a "Gun facing" and its crew named in "Or held by"; it is placed manned, facing that hexspine.
- **Ordnance panel.** In the fire phases, the firing side's manned Guns (with their facing, shots, ROF, and Acquisition) and the target Locations; "Propose: fire the Gun".
- **Records.** Each shot in words: the Basic TH#, its modifications, the Modified TH#, the dice with the colored die, the DRM, the Final DR, hit or miss or Critical Hit, each IFT attack's column, dice, and result, the ROF, malfunction, and Acquisition.

## 6. Tests

- **ScenarioA1:** the table and colors, Case A to R, Critical and Improbable Hits, ROF, breakdown, Acquisition, a Critical Hit among several targets sharing the Effects DR, the refusals, and reachability walks over five shots.
- **Authoring:** the Ordnance matrix and its 49 verified subjects; the revised matrices.
- **Units:** the record's round trip, ROF counting, facing, Acquisition, and UNIT-STATE-033.
- **Play:** U28 (a shot, a second shot on the kept ROF with Acquisition, no third, none in the AFPh), a shot that turns the Gun, a hit that breaks a squad, the crew's refusal to move, and the berserk companions in live fire and rally.
- **MapStudio:** placing a Gun and its crew, and firing it from the panel.

## 7. Not in this pass

In the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 12: the Area and Vehicle Target Types, mortars, Special Ammunition and To Kill, Defensive First Fire by Guns, Intensive Fire, Bore Sighting, fire within the Gun's hex, Guns and crews as targets, Gun movement and repair, crews' inherent fire, and Covered Arcs across boards.
