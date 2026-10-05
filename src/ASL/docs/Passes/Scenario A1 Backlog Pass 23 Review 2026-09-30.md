# Scenario A1 Backlog Pass 23: Per-Side Views and Hidden Setup

**Status:** Reviewed. Pass 23 of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the first game pass: a side's view at setup and in play, the hand-over screen, HIP by SSR, the non-OB "?", Control as a side knows it, and the Play components of task 23.5. No package changes.

**Date:** 2026-09-30

**Plan:** the Redesign Plan, pass 23 (tasks 23.1 to 23.5) and sections 13.4, 15.4, 16.17, and 17.2; rulings R23.1 to R23.6 in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. A referee, a table player, and a UI and Blazor reviewer read the change, in parallel; all were read-only.

**Design:** [ASL Unit Backlog Pass 23 Design](<ASL Unit Backlog Pass 23 Design.md>).

## The user's ruling

Before the build, the plan's task 23.2 (the second side sees nothing of the first side's placements until both have set up) was put against the rulebook (A12.12: the first side sets up "out of vision of his opponent"; A2.9: "No enemy stack ... may be inspected prior to the start of play"). The user chose the rulebook: the second side sees the first side's finished setup as on the board, each stack's top counter until play starts.

## Candidates

Extracted: all 12 P1 (P01, P02, P07, N02, R01, R02, R03, R12, K05, K06, K07, K08) and the P2 R13, with `HandOverScreen` added. Before the extraction, `PlayComponentTests` pinned the Victory standing (`#play-victory`, `data-control`, `data-side`) in a side's view and the adjudicator's. The page tests run on a synthetic board, where no card has structured Victory Conditions, so the pin is at the component; the page wiring was checked in the Studio.

## Visual checks

Before the reviews: a Gambit game in the Studio (the British set up; the German view showing the British E6 stack's top counter and two counted beneath; the hand-over screen, with the map, panels, and board link out of the page; the British view while the Germans set up, its note, and no German counter on the map or in the table; the non-OB "?" fieldset offering Q10 and Y10 by board 04's LOS, one placed on Y10 and seen by the British as "?"; play started, the standing as the British know it and the adjudicator's; the board viewer from the British link); a minimal game with a hidden squad and its place-hidden button; the hand-over at 375px. Two wordings fixed. After the review fixes: the British view of the German PFPh (the Fire panel's note, no German "?" among the pickers) and the Games page's British event list during the German setup. Clean.

## Referee findings

| Finding | Disposition |
|---|---|
| 1. Setup events named what a side's view hid: the out-of-sight side's placements, and a SW held by a HIP squad | Fixed: a side's events leave out the creation of instances it may not see; tests for the out-of-sight side and the hidden squad. |
| 2. A Gun or a vehicle could be set up hidden, a vehicle costing no squad-equivalent | Fixed: HIP for Infantry only; hidden Guns, fortifications, and vehicles are backlog (section 37); a test. |
| 3. A HIP unit could not give up its status to stop an enemy non-OB "?" (A12.32) | Backlog (section 37); the ruling says so. |
| 4. A broken unit could gain a non-OB "?" (A12.12: never while broken or berserk) | Fixed; a test. |
| 5. A Dummy placed without its concealed condition showed to the enemy | Fixed: a Dummy is always "?" to the enemy; a test. |
| 6. The tests did not prove the LOS reading of A12.12 (board 01's fixture has no LOS data); the lone leader test could pass vacuously | Fixed: a LOS stub proves clear and blocked LOS within 16 hexes, and a broken enemy and a Dummy not counting; the leader test asserts its case. Board LOS in the fixture is backlog. |
| 7. Out of sight hid a whole side, even a group already seen | Fixed: out of sight by OB group. |
| 8. The setup table showed the progress of a side setting up out of sight | Fixed: such a group shows only its status. |
| 9. Stale backlog rows, no rows for what the pass leaves out, a misplaced doc comment, the advance refusal without the "?" step, a HIP token naming no card side | Fixed: rows removed and section 37 added; the comment moved; the refusal names A12.32; the card check refuses the token. |

Checked and found correct: R23.6's range and LOS reading and its timing, R23.3's out-of-sight and top-counter rules, R23.4's Control and squad-equivalents for a side, and R23.5's allowance, kinds, and terrain.

## Table player findings

| Finding | Disposition |
|---|---|
| 1. The Move, Fire, and SW panels showed the other side's movers, firers, and weapons, hidden and concealed ones too | Fixed: the Move panel is the moving side's and the Fire panel the firing side's (the adjudicator sees both; the other side reads who acts); pickers read the view; page tests now hand the screen to the firing side before firing. |
| 2. The CC panel listed concealed enemy units with their values | Fixed: CC Locations and units come from the view. An attack on a "?" from a side's view is backlog; the adjudicator declares it. |
| 3. A hand-over kept the last side's drafts | Fixed: every draft is dropped but the CC declarations, which both sides make openly into one proposal; a page test. |
| 4. The non-OB "?" refusal named the enemy Location that sees the stack | Fixed: it names none. That the offered list reads every enemy unit is the opponent's check at the table; backlog row records it. |
| 5. The Games page showed a side setting up in full; an open board viewer tab keeps its view | The Games page fixed (one view helper); the tab is backlog. |
| 6. Opening or reloading the page shows the phasing side at once | Kept (ruling R23.2); a hand-over on load is backlog with the Play workspace (pass 28c). |
| 7. The hand-over is slow in the MPh and Defensive First Fire loop | Backlog: a "hand over" button beside an outcome (pass 28c). |
| 8. Uninspected counters are drawn as "?" placeholders | Backlog: a stack-depth marker; the table and the accessible names say "not inspectable before play". |
| 9. The out-of-sight note left the setup form usable; the entry picker's default could be a hidden enemy | The note says only the side setting up places units (honor system, backlog row); the default comes from the view. |

## UI and Blazor findings

| Finding | Disposition |
|---|---|
| 1. The last view's drafts survived the hand-over | Fixed (as table player 3). |
| 2. A side saw every unit when the replay failed partway | Fixed: with no view, a side sees nothing. |
| 3. The entry picker's default could be a unit the view hides | Fixed. |
| 4. K05 received the whole game's setup report | Fixed: groups out of sight show only their status. |
| 5. Two call sites of `GameView.Of` without the out-of-sight groups | Fixed: `GameLibrary.ViewOf` for the Games page; the fire targets pass them. |
| 6. Every render repeated the card setup, the out-of-sight read, and the Victory fold | Fixed: read once per history, revision, and viewer. The map's per-render drawing is backlog. |
| 7. The view cache rebuilt on every refresh | Kept: the history is a new object after each read, which keeps the cache from going stale. |
| 8. Moving focus to the hand-over button broke arrowing through the picker; focus after confirming | Fixed: no focus move; Tab reaches the button. Focus after confirming is backlog. |
| 9. K08 received the result and hid it; K06 received the sides' state | Fixed: K08 takes the "would win" line only for the adjudicator; K06 takes group labels. |
| 10. Same-named non-OB buttons, locate buttons named by a coordinate, an empty label | Fixed. |
| 11. The hand-over's corner radius | Fixed: the theme token. |
| 12. A doubled doc comment | Fixed. |
| 13. Opening another game skips the hand-over | Kept, as table player 6. |

## Tests

Play: `BacklogPass23Tests` 10, with `PlayTests` updated for the new action. Units: `StateTests` updated for A2.9. MapStudio: `PlayComponentTests` 4, two page tests, five page tests handing over to the firing side, and every view change through `PlayViews.ViewAs`. The full suite and ScenarioA1 ran before the commit; the Docker check after it.
