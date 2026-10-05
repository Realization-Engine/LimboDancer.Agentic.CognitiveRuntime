# Scenario A1 Backlog Pass 5: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire`, `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are revised and republished with their prior manifest digests kept. None has execution authority. Backlog pass 5 (deviations and small items).

**Date:** 2026-09-27

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 5), and 5 (rulings R5.1 to R5.20). The user approved the plan and autonomous passes on 2026-09-27. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 5 Design](<ASL Unit Backlog Pass 5 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

CX and Double Time (A4.5, A4.51, A4.52, A4.72, A11.21), portage (A4.42), the captor's choice at a surrender and Massacre (A20.3, A20.4), owner options during a resolution (A18.11, A15.3, A7.309), a second Heat of Battle DR (A15.1, A15.5), a hero created in the MPh (A15.21), a HS keeping its squad's SW (A7.302), Acquisition on units (C6.5, C6.51), vehicle Motion and MP left (D2.4, D2.1, A8.14), Recall and Abandonment (D5.341, D5.41, A2.6, A20.53), and grain by season (B15.6).

## Sources

`asl-scenario-a1.pass5-pdf-comparison.json` (`10c5936c...`) compares 31 subjects, made as the earlier comparisons were, from 26 rules: A2.6, A4.42, A4.43, A4.431, A4.5, A4.51, A4.52, A4.72, A7.302, A7.309, A8.14, A11.21, A15.1, A15.21, A15.3, A15.5, A18.11, A20.3, A20.4, B15.6, C6.5, C6.51, D2.1, D2.4, D5.341, and D5.41. `AslScenarioA1FireSourceReview.BuildPass5` verifies each against the PDF.

| Artifact | Digest | Prior manifest kept |
|---|---|---|
| Fire case matrix (188 fragments, 22 cases) | `439fb9ad...` | |
| Fire package manifest | `f36022ca...` | `ef97d62a...` |
| Rally case matrix (47 fragments, 13 cases) | `23a49556...` | |
| Rally package manifest | `f8e87429...` | `9a1f5173...` |
| Close Combat case matrix (37 fragments, 15 cases) | `06530005...` | |
| Close Combat package manifest | `d976896c...` | `9f5d613b...` |
| Ordnance case matrix (51 fragments, 12 cases) | `6924b0fe...` | |
| Ordnance package manifest | `2e3621f2...` | `e02e8e4b...` |
| Vocabulary 1.9.0 | `2e855551...` | |

## Rulings

R5.1 to R5.20 are in the plan, section 5, as revised after both reviews.

## Referee findings

The referee re-read every cited rule and checked the packages, the planner, and the projector against R5.1 to R5.20.

| Finding | Rule | Resolution |
|---|---|---|
| D1. A CX unit that went berserk kept its CX counter. | A15.42, p. 84 | Fixed, with a test; R5.3 names it. |
| D2. A unit could withdraw from Melee carrying more than its IPC. | A11.21, p. 73; A4.43, p. 50 | Refused, with a test; dropping the SW is deferred (backlog section 15); R5.5 says so. |
| D3. A Surrender result with no captor, which only Disrupts, stopped the second Heat of Battle DR. | A15.5, p. 84; A15.1, p. 83 | Fixed: only a surrender to a captor stops it, with a test; R5.10 names it. |
| D4. A CX Gun crew did not add +1 to the To Hit DR. | A4.51, p. 51 | Fixed in the Ordnance package with the case `A1-ordnance-cx` and its fragment, with a test; R5.2 names it. |
| D5. A Good Order leader advancing with a laden MMC lends it two MF and one IPC, which the A4.72 test did not count. | A4.72 EX, p. 52; A4.12, p. 49 | Refused until pass 10 (task 10.5), with a test; backlog section 15; R5.5 says so. |
| D6. R5.13 named only the MPh and APh for an Acquisition's split units. | C6.5, p. 174 | R5.13 names the RtPh and a CCPh withdrawal too. |

It disputed these readings, all adopted:

| Reading | Rule | Resolution |
|---|---|---|
| A Massacre is made "as if using a SW": a MMC keeps its inherent FP, and a SMC forfeits its own. The first build marked every unit as having fired. | A20.4, p. 87; A7.351, A7.352 | Fixed: once per phase; only a SMC is marked, and a SMC that fired may not massacre. Tests updated; R5.7 says so. |
| A berserk unit never fires in the PFPh, so it massacres at the start of its AFPh or DFPh only. | A15.432, p. 84 | Fixed, with the test; R5.7 says so. |
| R5.14's hexspine cost for the intended hex is a reading; D2.4 says only "insufficient MP". | D2.4, p. 198 | R5.14 marks it as a reading. |
| R5.16 should cite A20.53's definition and say the game simplifies it. | A20.53, p. 87 | R5.16 rewritten. |
| Battle Hardening in a Rally changes a Disrupted unit only when the Rally leaves it Disrupted. | A15.3, p. 83 | Fixed in the Rally package. |

## Table player findings

The table player read the planner and the Play page as a player would. Each defect is fixed with a test.

| Finding | Rule | Resolution |
|---|---|---|
| 1. A Recalled AFV that must leave could not be chosen in the vehicle panel, and a Recall; +1 crew was never offered its BU toggle. | D5.341, D5.33, D5.34 | Fixed: `GamePlanner.MayStartVehicleMove` and `MayChangeExposure` decide both lists, with a test. |
| 2. A broken unit in Melee carrying more than its IPC had to withdraw but could not, so its CC could never be resolved. | A11.16, A11.21, A4.43 | Fixed: such a unit is offered no withdrawal, so it need not try, with a test. |
| 3. Any advance by a leader with a laden MMC was refused, even into Open Ground. | A4.72 EX, A4.12 | Fixed: refused only where the advance is Difficult for the MMC alone, with a test. |
| 4. A Russian MMC could massacre in the PFPh and then move. | A20.4, A7.351, A3.3 | Fixed: its Massacre counts as Prep Fire for the MPh, with a test; R5.7 says so. |
| 5. Massacre buttons showed in the MPh and to the other side, and read the side's nationality rather than the counter's. | A20.4; R5.20 | Fixed: only in the unit's own PFPh, AFPh, or DFPh, only to that side or the adjudicator, by the counter's nationality, with a page test. |
| 6. Both exit buttons at a corner hex sent no edge. | A2.6 | Fixed: each button names its edge (`edge` in `asl.game.move-vehicle`), with a test of `ExitEdge`. |
| 7. The Double Time tooltip named the wrong bar. | A4.51 | Reworded. |
| 8. The Friendly Board Edge selects said "first side" and "second side". | | They name the sides chosen above. |
| 9. Nothing makes a Recall; +1 AFV move, and it may not Stop to unload Passengers. | D5.341 | The first is already refused: the phase does not advance until it has moved (`play.recall-move`). The Stop for Passengers is in backlog section 15. |
| 10. Assault Movement with Double Time was accepted. | A4.61 | Refused, with a test. |

It confirmed CX removal and the Double Time bar, the Double Time MF and caps, the portage arithmetic of the A4.52 example, the A4.72 tests, grain by season, No Quarter, the once-only ELR rise, the hero's movement status, and who sees the choice, Guard, and reject controls.

## Visual check

In the Studio (`map-studio-scripted`), a new game named each side's Friendly Board Edge, and a German squad Double Timed into woods: the units table showed it CX, the move's reason named A4.5, and the Double Time box cleared. The pane did not draw during the check, so it read the page's text rather than screenshots. The bUnit page tests cover the choice panel, the reject and Massacre buttons, and the per-side CC declarations.
