# ASL Unit Rally and Fire Extensions in Live Play Design

**Status:** Unit steps 19 to 23 are built in one pass, as the [ASL Unit Rally and Fire Extensions Plan](<../Plans/ASL Unit Rally and Fire Extensions Plan.md>), section 11, set out: the review sitting (the Rally package, the revised Fire package, and catalog 1.2.0), then the game model, the actions, and the Play page. What the rulings leave out, and the deviations this pass records, are in the [ASL Unit Backlog](<../ASL Unit Backlog.md>).

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, steps 19 to 23, and acceptance scenarios U21 to U27 (section 14).

**Related documents:** the [Scenario A1 Rally Review](<../Reviews/Scenario A1 Rally Review 2026-09-26.md>), the [Scenario A1 Fire Review](<../Reviews/Scenario A1 Fire Review 2026-09-26.md>), section "Revision at unit steps 19 to 23", and the [ASL Unit Fire in Live Play Design](<ASL Unit Fire in Live Play Design.md>), whose principles this pass keeps.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Rally and Repair (steps 19 and 23).** In the RPh a broken unit attempts to rally through `asl.game.rally`, resolved by the reviewed package `scenario-a1-rally`, and a Good Order unit repairs a malfunctioned MG it possesses through `asl.game.repair` (A9.72, p. 65).
- **Advancing Fire and fire groups (step 20).** The phasing side fires in its AFPh at half FP, and a fire group may span Locations each ADJACENT to another (A7.5, p. 57).
- **Hidden units, Dummies, and empty Locations (step 21).** Fire at a Location holding a hidden unit or a Dummy commits, and so does fire at a Location holding nothing.
- **Movement (step 22).** A stack moves one Location per step through `asl.game.move`; after each step the DEFENDER fires or passes (`asl.game.pass-fire`), and the ATTACKER moves on or ends the move (`asl.game.end-move`). Defensive First Fire, Subsequent First Fire, FPF, and Residual FP are resolved by the revised Fire package.
- **MGs (step 23).** A firer's MGs add their FP to its attack, keep or lose their Multiple ROF on the colored die, and malfunction on their B#.

## 2. Principles carried forward

- **Every outcome decided before any roll.** Each action runs its package's pre-check and refuses before drawing anything. The Rally and Fire reachability walks cover every accepted case kind.
- **Rolls on demand.** Each build function asks its package for the one roll it is missing (`rally`, `rally-wound-severity`, `repair`, and the Fire purposes, now with `weaponSelection` and `firerSelection` Random Selection keys) and draws only that.
- **The package is the arithmetic.** Replay hands each Rally record to `RallyRecordVerifier` and each fire record to `FireRecordVerifier`; both rebuild the state's facts and require the package to reproduce the recorded resolution.

## 3. Game model

- **Vocabulary asl@1.6.0.** The state `asl:first-fire`, drawn as a "First" badge on both sheets, and the kind `asl:dummy`, drawn as a Dummy face. `Conditions` gains `FirstFire`, `DesperationMorale` (`asl:dm`), and `Malfunctioned`.
- **Equipment definitions.** An equipment instance may name its catalog definition, which gives a MG its FP, range, B#, ROF, and Repair Number. The four Scenario A1 MGs are in catalog 1.2.0 with values manufactured under ruling R0.3.
- **New records** (`LiveRecords.cs`):
  - `rally-attempted`: the unit, the rallying leader, the roll ids, the facts, and the resolution. Replay refuses it outside the RPh, for a unit that already attempted this Player Turn or repaired this phase, or on the verifier's disagreement (UNIT-STATE-027). It records the side's first MMC attempt of its own RPh (A18.11).
  - `repair-attempted`: the unit, the SW, the roll, the Repair Number, and the result. Replay refuses it unless the SW is malfunctioned and possessed by the unit, the unit did not attempt to rally, and the Repair Number and dr agree with the result (UNIT-STATE-028).
  - `movement-step`, `movement-window-closed`, and `movement-ended`: the moving stack, the Location entered, its cost in half MF (grain costs 1½), whether it is Assault Movement, and the step number. Replay refuses a step outside the MPh, by a stack that is not the phasing side's, while the DEFENDER's window is open, by another stack before this one ends, or out of order (UNIT-STATE-029).
  - `residual-fp-placed`: the fire record and the value it leaves in its target Location. Replay refuses a value the record does not give, and a counter no larger than one already there (A8.21; UNIT-STATE-026).
- **`fire-resolved`** carries the movement step it answers in the MPh; replay refuses one outside the open window, and applies A7.55 per step.
- **Phase changes** clear First Fire and Final Fire after the DFPh, Prep Fire after the AFPh, pins after the CCPh, and DM after the RPh (A10.62, p. 68); Residual FP and the moving stack are cleared, and the Rally attempts reset on a new Player Turn.

## 4. The actions

- **`asl.game.rally`** takes `unitId` and an optional `leader`. `LiveRally.FromState` reads the unit, the leader, the other leaders in its Location, DM, and whether this is the side's first MMC attempt; the planner adds the Location's terrain and, for a concealed unit or leader, whether a Good Order enemy within 16 hexes has LOS to it (A12.141). An attempt by a concealed unit that stays concealed is visible only to its side (ruling R19.8).
- **`asl.game.repair`** takes `unitId` and `equipmentId`, draws one die, and records the result. It is attributed to the Fire package, whose review admitted A9.72.
- **`asl.game.move`** takes `unitIds`, `to`, and `assault`. The stack moves from one Location to an adjacent one at the same level across no hexside terrain, into Open Ground, orchard, brush, woods, grain, or an ordinary building, at the MF A4.13, B12.4, B13.4, B14.4, B15.4, and B23.4 give, or 1 MF across a road hexside (A4.132). An enemy-occupied Location is refused (ruling R22.6), and so is a move beyond a mover's MF: four for a MMC, three if Inexperienced, and six for a SMC, three if wounded (A4.11, p. 48; A17.2, p. 85). The building entry of steps 7 to 11 keeps the MMC allotment its reviewed cases assume. When the Location holds Residual FP, the planner replays the step and plans the Residual FP attack on the entering stack in the same batch, before the DEFENDER may fire (A8.22).
- **`asl.game.pass-fire`** closes the DEFENDER's window on the latest step; **`asl.game.end-move`** marks the stack's units as done moving.
- **`asl.game.fire`** now takes `firers` in one or more Locations, `director` or `directors`, `weapons` (a map from firer to its MGs), `withoutInherent` (firers whose MGs fire without their inherent FP), and `target`, which may be any Location. In the MPh the fire kind comes from the firers' markers (First Fire, Subsequent First Fire, or FPF) and the targets are the moving stack; the attack must answer the open window (`play.fire-window`), and a firer attacks the stack in a Location no more often than the MF the stack spent there (A8.3, A9.2; `play.fire-mf-limit`). The events add the fire markers, the change from First Fire to Final Fire, DM for a broken unit attacked by FP that could cause a NMC, the FPF firers' NMC, MG malfunction and ROF markers, and the Residual FP counter.
- **The gate's read-back** now recognises each new kind of commit: a Rally or Repair attempt kept in the state, a movement step with its window open, a closed window, an ended move, and a fire record whose firers carry Prep, First, or Final Fire.

## 5. Readings of this pass

- **A road hex is Open Ground.** B1.11 (p. 113) says a road hex devoid of other terrain is Open Ground apart from movement through a road hexside, so the planner reads "Paved Road" and "Dirt Road" as Open Ground for TEM, FFMO, and Rally terrain.
- **Fire at an empty Location** is resolved on the concealed column, as fire at a Location the firing side cannot see into. On no effect its record is withheld from the firing side, exactly as for unseen targets left unaffected, and a public `fire-reported` gives the arithmetic, so the firing side cannot tell an empty Location from a concealed stack left unaffected (ruling R21.1).
- **A moving stack stays whole.** When a mover breaks or pins, it may not move again (A8.1), and neither may the rest of its stack, since a move names the whole stack. The ATTACKER ends the move. Splitting the stack is in the backlog.

## 6. The Play page

- **Setup** places a Dummy for either side (always concealed), a unit broken, and a SW in a unit's possession ("Or held by").
- **The RPh panel** offers each broken unit, the leaders in its Location, and Self-Rally, and each malfunctioned SW for repair by its holder.
- **The MPh panel** shows the moving stack, its step, and whether the DEFENDER's window is open, and offers the phasing side's units that may still move, the Location to enter, Assault Movement, the DEFENDER's pass, and the end of the move.
- **The fire panel** is offered to the phasing side in the PFPh and AFPh and to the other side in the MPh and DFPh. Firers may be chosen from several Locations, each firer's MGs may join the attack or fire without its inherent FP, a leader in a firer's Location may direct, and the target may be typed as any Location. A committed action clears its choices.
- **The map** draws Residual FP as a counter with its value in each viewer's map, and the First Fire badge through the vocabulary. The units table shows a unit that is no longer active by its status rather than its last Location.
- **The fire group** is named the same way before confirmation and in each record: each Location's firers with the MGs they use, an MG firing without its holder's inherent FP named alone, and every directing leader. The range is each firer's when the group spans Locations. Only a Location holding a MMC is offered as a firing Location, so a Dummy's or a lone leader's is not.
- **Rally and Repair records** are listed with their arithmetic: the rally DR, each DRM with its rule, the Final DR against the Morale Level, and the result, with Fate, a wound, and Heat of Battle or Leader Creation not taken; a repair's dr against the Repair Number and its result.
- **A game that does not replay** says so, with its first diagnostics, and names the catalog when the game was set up with one the Studio no longer carries (a game set up before catalog 1.2.0).

## 7. Tests

- Scenario A1: `ScenarioA1RallyPackageTests`, `AslScenarioA1RallyMatrixTests`, `ScenarioA1FireExtensionTests`, and the reachability walks over twelve accepted attacks.
- Units, `LiveRecordTests`: each record accepted, refused in the ways section 3 lists, and round-tripped.
- Play, `RallyAndFireStepsTests`: U21 (a rally under DM in a wooden building, replay, the repeated attempt, DM removed after the RPh); Fate and the first MMC Self-Rally; U22's refusals before any roll; Repair; U23 (AFPh halving, a group in two ADJACENT Locations, a Prep Fire-marked squad refused in the AFPh); U24 (a Dummy removed, fire at an empty Location withheld with its public report); U25 and U26 (Defensive First Fire with FFMO, Residual FP, A7.55 per step, the Residual FP attack on the next stack, markers cleared); Subsequent First Fire; U27 (a LMG in the group keeping its ROF, then firing alone and malfunctioning).
- Studio, `PlayPageStepsTests`: setup with a Dummy, a broken squad, and a held LMG; a rally in the RPh panel; the LMG joining a fire that removes the Dummy; a stack moving under Defensive First Fire with Residual FP drawn for every viewer, the pass, the end of the move, and Residual FP gone after the MPh. `PlayPageFireTests` now expects the DEFENDER's fire panel in the MPh.
- A live run in the Studio on 2026-09-26, on board bd01 with the real LOS read and real dice, followed the pass: a rally, an LMG in a Prep Fire attack on a Dummy, Defensive First Fire with FFNAM and FFMO leaving Residual FP, the Residual FP attack on the next stack to enter, a leader moving six MF, an Advancing Fire group across two Locations, and the markers cleared by their phases. It found six problems, fixed on `fix/asl-play-page-gaps`: leaders could not move, Rally records showed only their dice, the facts table and records named one Location and no MGs, a Dummy's Location was offered as a firing Location, and a game on the retired catalog opened blank. Page tests now cover each, and `ALeaderMovesWithSixMfAndAWoundedLeaderWithThree` covers the allotments.

## 8. Not built

Everything in the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 1 to 8, and the items this pass added there (section 9), among them the road bonus, the leader's MF bonus, minimum movement, concealed movement, merging building entry into the move, and splitting a moving stack.
