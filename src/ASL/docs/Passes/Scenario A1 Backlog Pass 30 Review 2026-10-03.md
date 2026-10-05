# Scenario A1 Backlog Pass 30: Setup Plans

**Date:** 2026-10-03

**Branch:** `feature/asl-backlog-pass-30`

**Related documents:** the [pass 30 design](<ASL Unit Backlog Pass 30 Design.md>) (sections 1 to 12), the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>) (pass 30), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 46.

## Status

Done, waiting for the user's word to merge. The pass built setup from the card on the map (tasks 30.1 and 30.2), setup plans as data beside the card (30.3), choosing a plan at setup (30.4), ten plans on the four built-in cards (30.5), and, after Claude Design's review of the first workflow, the setup mode (30.6). Three reviews then read the branch at commit fd69ddc: a referee, a table player, and a UI and Blazor review. None found a blocker. Their findings are fixed below or sent to backlog section 46. The fixes were checked in the Studio (port 6670), then the whole pass (step 5, section 12 of the design).

A setup plan places the card's fixed OB and never changes it. A card's plans are in their own file, so adding or changing plans never changes the card's text, its SHA-256, or a saved game.

## Referee findings

No setup component receives the full game state in place of the view. Plans are offered only in the view of the side that sets up first, while it sets up (ruling R23.3). Seven findings:

| # | Finding | Fix |
|---|---|---|
| 1 | The place note and the one-counter form's fields (Location, id, Bore Sighted Location, group) survived a hand-over, so the next view could read a Location typed by the last. | `ResetForView` and `ChangeGame` clear them; the form's Location starts empty. |
| 2 | A proposal of non-OB "?" kept the review text of the setup proposed before it. | A non-OB proposal writes its own plain review. |
| 3 | The adjudicator was told "this card has no setup plans" for a card that has them. | The line is shown only to a view that may set up. |
| 4 | A malformed setups file (null plans, a null placement) threw. | `ScenarioSetupPlans.Parse` returns a diagnostic; a file locked while it is written gives no plan this time. |
| 5 | New counter ids were checked against the full state, which tells a view an id is taken by a counter it cannot see. | `NewPlacementId` checks the view's units and equipment. |
| 6 | A changed list could be confirmed under a proposal made from the list before the change. | A list change drops a pending setup proposal and says so: "The list changed, so the setup proposed before is dropped: propose it again." |
| 7 | The pass had no tests. | By the pass's rule the tests are written at the merge gate: `BacklogPass30Tests` (Play) and `SetupPlansPageTests` (Studio). |

## Table player findings

The reviewer set up each card by plan and by hand. Eighteen findings:

| # | Finding | Fix |
|---|---|---|
| 1 | The plan card's first sentence was cut at "2-in." | The idea is shown whole, clamped to three lines, with "More". |
| 2 | "Tripwire and reserve" described a setup its placements did not match. | Its idea and "Gives up" reworded to the placements. No placement changed. |
| 3 | West woods (Gambit) and East front and Hidden core (The Tractor Works): "Gives up" left out the leader and the MG. | A sentence added to each. No placement changed. |
| 4 | Gambit by hand: nothing said five counters set up on board and the rest off board. | The Counters tab says the limited area's count ("0 of 5"), and the card's lines start off board to be moved on. |
| 5 | A SW whose holder was removed vanished from the list. | It is listed under "Held by units already set up" or as not placed, with a holder to choose. |
| 6 | A stack's level could be set only for the whole stack. | Each counter has its own level; "stack to" sets all. |
| 7 | "Set up" a group twice doubled its counters. | It adds only what the list lacks ("Add what the group still owes"), and the button is shown only while the list lacks something. |
| 8 | Holder options were ids. | "4-6-7 1st-line squad, F6 on board 01 (g-squad-1)"; "gun", "vehicle" said where it matters. |
| 9 | Counters added for a group stood with no default hex or holder. | A one-hex building is filled in; SW go to squads in turn; a Gun to a crew. |
| 10 | The "?" count was not said. | "? used: 2 of 2 (1 stack under ?, 1 Dummy)". |
| 11 | The setup area was not drawn. | Outlined on the map, with "Zoom to the setup area". |
| 12 | Refusals named Locations as identifiers. | In words, each with a link to its row. |
| 13 | "all 7 off board" read as an error. | "7 counters, all off board to enter"; the off-board section opens by itself when everything waits there. |
| 14 | The bar's problem count counted rows, not reasons. | It counts the reasons. |
| 15 | After Confirm the notice named the wrong next side when the next group only waits off board. | It names the side that owes the next setup, on or off board. |
| 16 to 18 | Wording: "prepared setup", "pool", "line". | "setup plan", "OB group", "counter" throughout. |

## UI and Blazor findings

Twenty findings; the larger ones:

| # | Finding | Fix |
|---|---|---|
| 1 | Confirm could commit a list changed after Propose. | The referee's finding 6. |
| 2 | Focus was lost after "Use this plan", a refusal link, and Confirm. | The Counters tab, the row, and the actions pane take it. |
| 3 | The bar's question and the move banner were not announced. | Both sit in live regions that are always present. |
| 4 | The sticky bar covered the row a link scrolled to. | `scroll-margin-top` on rows, stacks, and plans. |
| 5 | A counter could be moved only with the mouse. | "or type its Location" in the move banner; a wrong Location says the form to type. |
| 6 | `pickingKey` outlived its row. | Cleared when its row leaves the map. |
| 7 | The off-board section closed itself on a render. | Opened once by a latch, never closed by the page. |
| 8 | `MaySetUp`, `PlansOffered`, and `PlanChanges` were recomputed on every render. | Cached by revision, view, and list version. |
| 9 | `[` and `]` fired with Ctrl or Alt held. | Forwarded only bare. |
| 10 | "Zoom to the setup area" zoomed wrongly on a turned map. | `fitRect` keeps the fit when the map is turned or the area is as large as the map. |
| 11 | Buttons overflowed their pane under 400px. | They wrap. |
| 12 | Dead code: the OB table's "Set up" column. | Removed. |

## The Studio check of the whole pass (step 5)

At 1920x1080, 1366x768, 1024x768, 683x384, and 320x640, by keyboard and mouse, in each side's view and the adjudicator's: The Guards Counterattack by plan (shown, used, adjusted, proposed, confirmed), Gambit by hand, Armor Test by hand and its German column entering, The Tractor Works by plan, a non-OB "?" proposal, a user's card with its own setups file (a plan made for an earlier text, a stale plan refused with its reasons), a card with no plans, and a game saved before the pass. Three more faults were found and fixed:

| Fault | Fix |
|---|---|
| At 320 pixels a long holder name pushed the Counters tab sideways. | The select is cut to the pane's width. |
| At 1024x768 the bar, the tabs, and the group's button filled the actions pane, so no row was in view. | The group's button is shown only while the list lacks its counters. |
| A counter with no hex was refused in the gate's words ("$.events[0]... is not a board location"). | "Propose setup" says "1 counter has no hex yet: place it on the map, tick off board, or remove it" and opens the Counters tab. |

## Found at the merge gate

A new game from a card with no OB group opened behind a hand-over although no side sets up there. The hand-over after a new game is now taken only when a side sets up next (`Confirm` in Play); a minimal card's game stays in the view, as before the pass.

## Left out

Backlog section 46 holds what the pass leaves out: a plan comparison table, links from a plan's terrain facts to the map, a draft kept across a reload, an editor for plans and a setups file that follows its card on Rename and Delete, hidden, Bore Sight, and Deployment on a row, and the smaller items of the reviews.

## Tests

`BacklogPass30Tests` (Play): each built-in card's setups file parses with no diagnostic and each plan names its card's current SHA-256; a card's hash is of its own text alone and is the same with and without a setups file; a file of bad form is refused with its reasons and never throws; the gate accepts each of the six plans of board 01 (The Guards Counterattack and The Tractor Works) as the first setup of a game; a side's own Dummies are drawn for it alone. The four plans on board 4 (Gambit, Armor Test) were accepted by the gate in the Studio on the real board; the tests have no board 4 terrain, so they check those plans' form only.

`SetupPlansPageTests` and the component tests (Studio): the setup mode's bar, tabs, and list, "Show on map" leaving the list alone, "Use this plan" filling it, the question before a changed list is replaced, and plans kept from the other side's view and the adjudicator's.
