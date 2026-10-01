# Scenario A1 Backlog Pass 25: Entry and Exit

**Status:** Reviewed. Pass 25 of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), the third game pass: entry by advance, blocked and delayed entry, entry through the full movement step, offboard actions, exits, thirteen Play components, and the messages of task 25.7. No package changes.

**Date:** 2026-10-01

**Plan:** the Redesign Plan, pass 25 (tasks 25.1 to 25.7) and sections 15.3, 16.17, 17.1, and 17.2; rulings R25.1 to R25.7 in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. A referee, a table player, and a UI and Blazor reviewer read the change, in parallel; all were read-only.

**Design:** [ASL Unit Backlog Pass 25 Design](<ASL Unit Backlog Pass 25 Design.md>).

## The user's rulings

Before the build the user answered three questions, each with the recommended reading: an entry area may name its entry hexes, and when every one is held the entry is a Game Turn later within four hexes, four more per further turn, never past a river or canal; an entering stack that attempts a hex of concealed or hidden enemy units is forced back off board (A12.15 with A2.51), its MPh over, free to enter by advance in the APh; and the APh, not the MPh, holds units that must enter. No split of the pass was proposed: task 25.6 had thirteen candidates.

## The K08 carry-over

From the pass 24 demo, the user asked that K08 `VictoryStandingTable` show `#play-victory-locations` only when a Location row is listed. It now reads `ScenarioVictory.DifferingLocations(Report.Control).Any()`; `MovementComponentTests.TheLocationNoteShowsOnlyWithALocationRow` renders a building and an upper level of the same side (no row) and finds no note, and the pass 24 test still finds it with a differing Location.

## The components

All thirteen candidates of task 25.6 were extracted; none was left inline. S12 `UnitSelectionList` and S11 `LocationField` (P2) are reused by four and three parents; N04 `BerserkRetainedWeaponsField` and A15 `MovementWindowActions` (P2) were extracted to keep the movement toolbar a list of components. K09 `MapExitAction` serves the move (`#move-exit`) and the advance (`#advance-exit`). Every element id the page tests use is kept; the page keeps the drafts and their reset rules, so no component holds state. The page test that section 17.1 asked for first, leaving the map from the move panel, is `PlayPagePass25Tests.AStackLeavesTheMapFromTheMovePanel`.

## Visual checks

Before the reviews, on copies of the Gambit demo game (`p25-visual`, from `p23-visual`) and the Guards demo game (`p25-smoke`, from `p24-visual`): the British MPh with ten units waiting off board and the note naming them; three entering bd04:E1 ("play.enter: ... for 1 MF from off board"); the MPh ended with seven waiting; the APh refused while they wait; the advance panel listing them with the edge hexes as destinations; three entering C1 by advance (grain); b10 leaving by advance off the top edge, and the standing giving the Germans its 2 CVP (A26.221); the German MPh in the Guards game, the SMOKE choices (only the squad with an exponent, its own Location for 1 MF and the six ADJACENT ones for 2 MF) and a SMOKE proposal; the building entry panel. Found and fixed: "its first MF expenditure" for a stack, the edge hexes sorted as text (A1, AA1, B0), and a TI unit (after Mopping Up) offered as a mover. After the review fixes, on `p25-check` and `p25-check2`: the movers' fieldset (labels run together until it was styled as Mopping Up's), the ready note, the edges with their compass directions, the `entry-due` refusal naming seven units and hexes open to them, and an exit by advance saying it meets no exit condition (its sentence reordered). Clean.

## Referee findings

| Finding | Disposition |
|---|---|
| 1. Entering stacks were not held to the stacking limits (A5.1: never exceeded in offboard setup) | Fixed: `EntryCheck` refuses more than three squad-equivalents or four SMC; a test. The MPh's missing A5.11 overstack MF is backlog (section 39). |
| 2. The APh hold could deadlock on a unit unable to advance (pinned, no MF after portage, CX into Difficult Terrain) | Fixed: `AdvanceEntries` tests each unit; one that cannot advance does not hold the APh (A2.5: "if capable of movement in the APh"). |
| 3. A PAATC on entry by advance reads the leader of the AFV's hex | Backlog (section 39): no card fields an AFV until plan pass 26. |
| 4. An exit by advance ignored the leader's MF and IPC | Fixed: `AdvanceAid` serves both the advance and the exit by advance. |
| 5. A20.53 does not bar a Guard from other edges, and a side may have several Friendly Board Edges | Fixed: a Guard leaves by any edge and is spared from CVP only off the card's Friendly Board Edge or its exit condition's edge; a test. Several edges per side are backlog. |
| 6. The river test also catches ponds | Kept: VASL names rivers and ponds both Water; backlog (section 39). |
| 7. Named entry hexes not on their edge failed silently | Fixed: a refusal names them; checking them at edit time is backlog (plan pass 28). |
| Checked and correct | A12.15 forced back off board with no fire (A2.51), Dummies, Residual FP and Fire Lanes first, no SMOKE, DC, or Gun push off board (A2.52), Bypass at entry from a vertex of the edge hexside, Minimum Move, the leader bonus, berserk first, the road rate (2Y1), the Bypass exit (X0), Prep Fire units exiting by advance, no RtPh exit, `ExitUnits`, the CVP and Exit VP of exited prisoners, the pass that ends a move, and offboard Deployment. |

## Table player findings

| Finding | Disposition |
|---|---|
| 1. The APh hold and the advance disagreed (marsh, cliff or unreviewed hexsides) | Fixed with the referee's finding 2; a test with a marsh entry hex. |
| 2. The MPh page did not say which hexes a named entry may use | Fixed: the note names them, or says the entry is a Game Turn later. |
| 3. The `entry-due` refusal named one unit | Fixed: every unit still waiting and hexes open to them. |
| 4. A stack forced back off board vanished from the page | Fixed: a note names it and says it enters by advance in the APh. |
| 5. SMOKE and DC controls for units off board | Fixed with the UI finding 5. |
| 6. Exits did not say whether they score; edges were top and bottom, the card says south | Fixed: each exit says what it counts for; the edges read "bottom (south)"; tests. |
| 7. Exited prisoners in Gambit's immediate win | Kept as A26.23 says (normal VP during play, double at the end); whether a card's VP list includes prisoners is backlog. |
| 8. An exit while another stack moves blamed the wrong stack | Fixed: `play.move-order` names the moving stack; a test. |
| 9. The advance destination could go stale | Fixed with the UI finding 4. |
| 10. Ponds as rivers; the off-board leader's entry turn; the Deploy help | The leader's entry turn is now checked; the help text names the off-board case; off-board squads are marked; ponds are backlog. |
| 11. Test gaps | Added: the marsh hold, the radius on Turn 3 (eight hexes, not past C1's water), the exit order message, the scoring texts, and the Guard off a non-friendly edge. Page tests of entry by advance are backlog. |

## UI and Blazor findings

| Finding | Disposition |
|---|---|
| 1. CC Reaction Fire lists read the full state and showed in every view (as on main) | Fixed: `SeenOnly`, computed once, and the panel only for the DEFENDER's view and the adjudicator. |
| 2. `LeadersWith` on board read the full state; the Deploy squad list offered enemy squads | Fixed: `SeenOnly`, and a side's own squads only. |
| 3. SMOKE and DC drafts never reset, now hidden behind selects | Fixed: reset with the game and phase, pruned to the choices offered (`PruneMoveDrafts`). |
| 4. The advance destination and exit edge not pruned or reset | Fixed: pruned on each toggle and refresh; `advanceExit` cleared after an advance. |
| 5. SMOKE and DC offered for units off board | Fixed: only movers on the map. |
| 6. Accessibility below the Mopping Up style | Fixed: fieldsets with legends for the movers and the advancing units, `aria-describedby` on the advance and exit selects and the Location field, ready notes for the disabled propose buttons. The rule help stays in `title` as well. |
| 7. Parameter typing; `ReactionUnits` called three times | Fixed: `BuildingEntryAction.Choice`; one computation. |
| 8. The Deploy help out of date | Fixed. |
| 9. Test gaps | A component test of the five small actions, and the fieldset and ready note in the component tests; page tests of entry by advance and the off-board lists are backlog (section 39). |

## Tests

`BacklogPass25Tests` (Play, 11): entry by advance and the APh hold, with an advance into a Known German's hex; a concealed German forcing a stack back off board; Residual FP attacking an entering stack; Bypass, Minimum Move, no SMOKE from off board, and the stacking limit at entry; a marsh entry hex not holding the APh; a named entry hex blocked, the radius on Turns 2 and 3, and the water of C1; offboard Deployment; a named entry area and another side's OB group refused; exits by advance, from Bypass, and at the road rate, with the exit order message; a Guard leaving by a non-friendly, a friendly, and an exit condition's edge, with CVP and Exit VP during play and at the end; and the pass that ends a move whose movers are gone. `BacklogPass20Tests` no longer expects the MPh to hold, and `BacklogPass20TablePlayerTests` now expects an off-board squad's Deployment attempt with a leader waiting along its edge (R25.4) where it expected a refusal; the full suite found it. `PlayPagePass25Tests` (MapStudio, 1) and `MovementComponentTests` (MapStudio, 8). The full local suite and ScenarioA1, the Docker Linux check, and the chart supplement regeneration are in the time log.
