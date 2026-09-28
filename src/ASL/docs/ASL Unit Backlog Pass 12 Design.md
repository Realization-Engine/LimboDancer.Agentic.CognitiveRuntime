# ASL Unit Backlog Pass 12 Design

**Status:** Built. Backlog pass 12 (Fire extensions), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 12.1 to 12.8.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 12 Review](<Scenario A1 Backlog Pass 12 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 11 Design](<ASL Unit Backlog Pass 11 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 22.

Rulings R12.1 to R12.12 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Opportunity Fire (12.1).** In its PFPh the phasing side marks Good Order Infantry that have not fired with a Bounding Fire counter (`asl.game.opportunity-fire`); they neither fire in the PFPh nor move in the MPh, and fire in the AFPh without its halving and without Assault Fire, their MGs keeping Multiple ROF (A7.24, A7.25, A7.36). A concealed one in a Good Order enemy's LOS within 16 hexes loses its "?" (Case D). No other AFPh MG keeps Multiple ROF.
- **Blocked LOS (12.2).** Fire whose every firer's LOS is blocked still makes its DR, marks its firers, and affects nothing (A6.11); in a group spanning Locations, the blocked firers do this first and the others attack as a smaller group (A7.52).
- **FPF variants (12.3).** A leader may direct FPF, taking its NMC and modifying the others'; FPF firers may group with Subsequent First Fire firers; a stack of pinned and unpinned movers takes one DR, the pinned ones without FFNAM, FFMO, or Hazardous Movement (A8.31, A7.83).
- **Split fire and SMC fire (12.4).** A squad fires one MG apart from its inherent FP, in either order, and no more than two SW a phase (A7.351); a leader fires a MG alone as Area Fire, or with a stacked SMC as his partner at full FP, and gives no leadership that phase (A9.12).
- **Concealment (12.5).** As a Player Turn ends, the phasing side's Good Order Infantry gain "?" by the Concealment Table's Cases I, J, and K, some on a Final Concealment dr (A12.12, A12.121, A12.122); Concealment Terrain is the Terrain Chart's red terrain.
- **MG techniques (12.6).** Spraying Fire by MGs at two Locations sharing a hexside, Area Fire on one Original DR (A9.5 to A9.52); a Fire Lane along a straight Hex Grain with Defensive First Fire, whose Residual FP attacks Infantry entering its Locations and ends when its MG malfunctions, its manning Infantry breaks or is pinned, or the MPh ends (A9.22 to A9.223).
- **Fire and Melee (12.7).** Fire from outside into a Melee Location attacks every unit there, each side against its own ELR (A11.15); fire at prisoners attacks them with their Guard, Reducing instead of breaking them (A20.54); a Guard whose US# is less than its prisoners' fires only at them (A20.52); a berserk unit fires except in its PFPh (A15.432).
- **Encirclement (12.8).** Consecutive attacks of one side at one Location whose LOS enter by opposite hexspines, opposite hexsides, or three non-adjacent hexsides Encircle it: its units' Morale Level is one lower against attacks, their fire takes +1, and their first Location entered costs double MF, until none is left there (A7.7, A7.71).

## 2. The package

The Fire package is revised with its prior manifest digest kept (`584d82d9...`, prior `14feea97...`; matrix `d9497647...`). `FireAttack` adds `SprayingFire`, `SprayShare`, and `FireLane`; `FireFirer` adds `OpportunityFire`, `Encircled`, and `Partner`; `FireTarget` adds `Encircled`, `Friendly`, and `GuardId`; `FireArithmetic` adds `PinnedFinalDr`, `PinnedResult`, and `PinnedConcealedResult`; `FireResolution` adds `LosBlocked`. The calculator resolves a blocked attack, marks units per firer (`Marked`), gives `Preview` of an attack's arithmetic, and admits a leader's MG. Rulings `opportunityFire`, `blockedLos`, `fpfVariants`, `pinnedMovers`, `smcFire`, `sprayingFire`, `fireLanes`, `meleeAndPrisoners`, and `encirclement`; eight new cases; 16 new fragments from `asl-scenario-a1.pass12-pdf-comparison.json` (309 in all; A9.222 is cited from the PDF, its Markdown text merged with its example).

## 3. The game model and records

- New records: `opportunity-fire-declared`, `encirclement-placed`, and `fire-lane-placed` (with its entries); the gained "?" is a `concealment-gained` conditions change.
- `GameState` adds `Encirclements` (kept while a unit they Encircle is there) and `FireLanes` (cleared at every phase change, and when the MG malfunctions or its manning Infantry breaks or is pinned); `Encircled(unit)` reads a unit's status.
- The Opportunity Firers join the units that may not move this Player Turn.
- The fire readback checks the units the resolution marks.
- The fire record verifier takes Spraying Fire, a Fire Lane present in the state, and an Encirclement completed by the attack as recorded.
- Actions: `asl.game.fire` takes `sprayTarget`, `fireLane` (`weapon`, `to`), and `partners`; new `asl.game.opportunity-fire`.

## 4. The Play page

- An Opportunity Fire panel in the PFPh.
- The fire panel: leaders who possess a MG as firers, a partner SMC, a second Location for Spraying Fire, and a Fire Lane's MG and far Location in the MPh; the proposal names them.
- Encircled units are marked in the units table.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass12Tests` (10); three older package tests follow the new rules.
- Play: `BacklogPass12Tests` (10), on a board 01 grid with a stub LOS a test may block; older Play tests follow the berserk, Guard, blocked LOS, and turn-end concealment rules.
- MapStudio: `TheOpportunityFirePanelMarksTheUnitsAndTheFirePanelOffersSprayingFire`, and the berserk DFPh test.
- Authoring: `ThePass12SubjectsAreVerified` verifies the 16 fragments; the matrix tests pin the new digests and fragment count.

## 6. Not in this pass

Section 22 of the backlog lists what this pass defers.
