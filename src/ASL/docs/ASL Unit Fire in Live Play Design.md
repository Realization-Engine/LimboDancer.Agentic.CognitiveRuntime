# ASL Unit Fire in Live Play Design

**Status:** Unit step 18 is built: parts 1 to 4 (the package revision, hindrance terrain, the game model, and the fire action), part 5 (visibility), part 6 (the Play page), and the acceptance run of part 7.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 18, and acceptance scenarios U19 and U20 (section 14).

**Related documents:** the [Scenario A1 Fire Review](<Scenario A1 Fire Review 2026-09-26.md>) and its package `scenario-a1-fire` (revised at part 1), the [ASL Unit Infantry OVR Design](<ASL Unit Infantry OVR Design.md>) (rolls on demand and the task check), and the [ASL Unit LOS Result Design](<ASL Unit LOS Result Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

A side may fire in its PFPh (the phasing side) or DFPh (the other side) through `asl.game.fire`: a fire group of Good Order MMC in one Location, optionally directed by a leader there, at another Location. The Fire package resolves the attack; the game records its rolls, its arithmetic, and each target unit's effect, and replay recomputes them.

## 2. Principles

- **Every outcome decided before any roll.** `ScenarioA1FireCalculator.Precheck` refuses an attack from which the dice could reach an undecided outcome. After part 1, every reason the package gives other than a missing roll depends on the facts alone, so the check is exact rather than a search: the facts must stop only at the missing IFT roll, the target side's ELR must be declared, a concealed firer or director needs a Good Order target within 16 hexes, and every unit a Reduction or Replacement can produce must have its Morale Levels. `ScenarioA1FireReachabilityTests` walks every roll outcome of three accepted attacks and finds each path Resolved.
- **Rolls on demand, one at a time.** The build function asks the package, draws the one roll it names, and asks again, so the log holds only rolls that decided something (DICE-07 to DICE-12). The walk found that a K/# or KIA result wounding a leader asked for two rolls together; the calculator now stops at the first.
- **The package is the arithmetic.** The game does not re-implement Fire: replay hands each record to a verifier that runs the package.

## 3. Part 2: hindrance terrain

`LosHindrance` gains `Terrains`: the terrain names of every hindrance hex met at that range, the read's own record beside VASL's values. Two entries are equal only when their terrains agree. `LosFidelity.Compare` checks on every answered fixture pair that each counted range names its terrain and that brush and grain are valued 1.

The planner attributes the Hindrance DRM (A6.7, p. 54) only when every counted range's terrains are brush or grain: each range with brush adds 1, and each with grain adds 1 in a declared month from June to September (B15.2, p. 129). A range with any other terrain leaves the DRM unattributed, and the package refuses the attack.

## 4. Part 3: game model

- **Vocabulary asl@1.5.0.** `asl:prep-fire` and `asl:final-fire`, drawn as a "Prep" or "Final" badge at the bottom right on both sheets, with goldens and parity rows. `Conditions` gains `Pinned`, `Wounded`, `Disrupted`, `PrepFire`, and `FinalFire`.
- **The scenario month.** `game-started` may carry `scenarioMonth` (1 to 12); setup passes it from `start.scenarioMonth`; `GameState.ScenarioMonth` holds it.
- **`fire-resolved`.** It records the firers, the director, the two Locations, the declared facts, the roll ids by purpose, and the package's resolution. Replay refuses it without a verifier (UNIT-STATE-023), when a roll it names is not recorded before it (023), when its Location has already fired at that target this phase (A7.55, p. 57; 024), and when the verifier disagrees (024). `GameState.FiresThisPhase` keeps the phase's attacks.
- **Effects.** They are ordinary events after the record: `conditions-changed` (broken, pinned, wounded, Disrupted, concealment lost), `instance-eliminated`, and `lineage` (Reduced for a squad's Casualty Reduction, Replaced for ELR Replacement). The firers and the director receive the fire marker, and lose "?" where A12.14 says.
- **Marker removal.** A phase change removes Final Fire after the DFPh (A3.4), Prep Fire after the AFPh (A3.5), and pins after the CCPh (A3.8), all on p. 47.
- **The verifier.** `FireRecordVerifier` (Play) rebuilds the state's facts with `LiveFire.FromState` and requires them to equal the recorded ones: units, conditions, phase, side, ELR, and month. The map facts (range, levels, LOS, terrain) are the planner's, taken as recorded. It then rebuilds the rolls from the recorded dice and requires the package's resolution to equal the recorded one. The planner's replay and the Studio's game library both pass it.

## 5. Part 4: the fire action

`asl.game.fire` takes `firers`, an optional `director`, and a `target` Location. The planner:

1. reads the state's facts (`LiveFire.FromState`): the game must use catalog `asl-scenario-a1@1.1.0`, the firers must share one Location, the director must be there, and the targets are every active unit in the target Location, in ordinal id order;
2. refuses a second attack from the same Location on the same target in the phase (A7.55);
3. reads the map facts: the LOS (Clear or Blocked only; an unsupported or nondefinitive read is refused), its range and attributed Hindrance, whether firer and target are at the same level (base level plus location level), and the target's terrain from the VASL name (Open Ground, Brush, Woods, Orchard, Grain, and the ordinary wooden and stone buildings); hexside terrain at the target is refused;
4. runs the pre-check, and refuses with its reasons;
5. plans a roll whose build function draws each roll the package asks for (`fire-ift`, `fire-random-selection` with one die per target, `fire-check`, `fire-leader-loss`, `fire-wound-severity`), then adds `fire-resolved` and the effect and marker events.

A `fire-resolved` whose result leaves a concealed target concealed is visible only to the target side, so the firing side learns the result from the rolls but not the target's identity.

The planner takes an optional `IFireLosReader`. Without one it builds the LOS map from the boards' LOS data; the Play tests pass a stub, since the board 01 fixture has hex facts but no LOS data.

**The store.** The Play tests found that a batch whose event type was misnamed replayed in memory and was written, but could not be read back. `FileGameStore` now writes a log only when it reads back.

## 6. Part 5: visibility

Rolls are public. The rest of an attack is shown to each side as it may know it:

- **A concealed target left concealed.** On a result of no effect, `fire-resolved` is visible only to the target side, since its facts and resolution name the target units. A public `fire-reported` event follows it: the record's event id, its two Locations, and its arithmetic (each firer's FP and multipliers, the column and any shift, the DRM, the dice, the Final DR, and the result), which name no target unit. The firing side learns what it would know at the table. Replay refuses a report that is not public, names no earlier record, names a record that every side may see, repeats one already reported, or differs from the record's Locations or arithmetic (UNIT-STATE-025).
- **Any other result** reveals every concealed target (A12.14, p. 76): the record is public, and the effect events clear "?".
- **A concealed firer or director** loses "?" by firing, in the public marker event.
- **Refusals.** When the target Location holds a unit the firing side cannot see, a refusal by the pre-check tells that side only `play.fire-refused` without the package's reasons, which can name a unit's definition (`definition-incomplete`) or its printed values (`underscored-morale`, `leaders-interact`). The target side and the adjudicator see the package's reasons. The plan carries them as a `FireProposal` with the firing side, the target side, the facts, and the firing side's reasons. That a refusal tells the firing side something is there, including a hidden unit, is the known limitation that requirement step 18 accepts.

## 7. Part 6: the Play page

- **New game.** The form records each side's ELR (A19.1) and the scenario month, which the pre-check needs.
- **The fire panel.** It is offered in the PFPh to the phasing side and in the DFPh to the other side. It lists the Locations that hold the firing side's units, the squads and half-squads there as firers, the leaders there as directors, and as targets the Locations where the firing side sees an enemy unit or knows of a concealed one (A12.11). A hidden unit is not offered.
- **Before confirmation** the proposal shows the attack's facts from the game and the map: phase and side, the fire group, the target Location, range, level, LOS and its Hindrance DRM, the target's terrain, and the month. The targets themselves are not listed.
- **After it** the page shows each attack the viewer may see, latest first: each firer's FP and multipliers, the total and its column, the IFT DR with each DRM, the Final DR and result, and, when the viewer may see the record, each target unit's effect with its MC, NTC, LLMC, or LLTC arithmetic. A side shown only the report is told that the target Location's units are not disclosed.
- **Markers.** The Prep Fire, Final Fire, and pin states are drawn by the vocabulary (part 3) in each side's view of the map, and listed in the units table.
- **LOS.** The Studio's board handles carry LOS data (`StudioBoardCatalog`), so the Studio's planner reads fire LOS itself, for a single board and a placed map alike; no separate reader was needed.
- **Dice.** `LivePlay` takes an optional `DiceRoller`, so page tests can fix the rolls; the Studio uses the default.

## 8. Tests

- Maps: the breakdown tests name their terrains; the fixture comparison checks terrains (with the VASL root).
- Units, `FireRecordTests`: a verified record kept for the phase; refusals without a verifier, with a missing roll, and on disagreement; A7.55; the record and the month round-trip, and a month of 13 is refused; each marker removed by the phase that ends it.
- Scenario A1, `ScenarioA1FireReachabilityTests`: the exhaustive walk and the pre-check's refusals.
- Play, `FireTests`: U19 (the committed attack, its events and arithmetic, the markers, a replay that draws nothing, a repeated attempt returned as a replay, and Prep Fire removed after the AFPh); U20 (the fire group rule, a marked firer, fire in the MPh, each before any roll with the revision unchanged); an undeclared ELR and an unanswered LOS refused; Casualty Reduction through lineage; the store's read-back guard.
- Units, `FireRecordTests`: a fire report accepted, round-tripped, and refused in each way section 6 lists.
- Play, `FireVisibilityTests` (part 5): each side's view and events for a known target broken, a concealed target left concealed (no event the firing side may see names it; the report gives the arithmetic), a concealed target revealed by any other result, a concealed firer and director revealed, and a hidden target refused with the firing side's reasons withheld.
- Studio, `PlayPageFireTests` (part 7): U19 on the Play page, on the synthetic board with its stone building and the Studio's own LOS read (the facts before confirmation; the arithmetic, the effects, the units table, and the drawn Prep Fire, pin, and broken states in each side's view; a replay that draws nothing and a repeated attempt; Prep Fire gone after the AFPh and pins after the CCPh); U20 on the page (the fire group rule and a marked firer refused with the revision unchanged, no fire offered in the MPh, an undeclared ELR refused); and a concealed target left concealed, shown to the firing side only as the arithmetic.
- A live run in the Studio on board bd01 committed a directed attack from D4 on the stone building E4 with the real LOS read and real dice.

## 9. Not built

- Everything step 17 excludes: Defensive First Fire, Final Fire in the AFPh, Subsequent First Fire, Residual FP, multi-Location fire groups, support weapons and MGs, Rally, and the rest of its list.
- Fire at a Location that holds a hidden unit or a Dummy stays refused (`concealment-unreviewed`).
