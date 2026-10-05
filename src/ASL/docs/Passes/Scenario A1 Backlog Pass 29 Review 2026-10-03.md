# Scenario A1 Backlog Pass 29: The Shared Board Workspace and the UI Batch

**Date:** 2026-10-03

**Branch:** `ui-improvements`

**Related documents:** the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>) (sections 1 to 16), the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>) (pass 29, section 15.11), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 44 and 45.

## Status

Done. Three reviews read the batch at commit 5978e4e: a table player, a referee (disclosure, rulings R23.1 to R23.4), and a UI and Blazor review. Their findings are fixed below or sent to backlog section 45. The fixes were checked in the Studio (port 6670), then the whole batch: a turn of game `p28c-walk` in each side's view and the adjudicator's, the board viewer, the Library, Fidelity, Maps, Game states, the Card editor, and the Unit Lab, at 1920x1080, 1366x768, 1024x768, 683x384, and 320x640, with the keyboard as well as the mouse. The design's section 15 has the detail of each fix.

## Table player findings

The reviewer read the code and walked a turn for each side and the adjudicator. Wide windows (1440px and up) played well. The trouble was from 1024px to 1439px, under 1024px, and one gap: a map click never filled an action's Location.

| # | Finding | Fix |
|---|---|---|
| 1 | Under 1024px every action bounced between tabs: after Confirm the player stayed in Map and had to open Actions again. | After a commit the actions take the focus; under 1024px their tab opens (`ShowActions` in Play). |
| 2 | From 1024px to 1439px Confirm could be scrolled out of sight under the reasons, in a pane capped at 40vh. | Confirm and Cancel come right after the outcome line (`ProposalReviewPanel`). |
| 3 | "Show it on the map" and the units table's Locate only set the picked hex; under 1024px nothing visible happened. | One `LocateHex`: it picks the hex, opens the Map tab when narrow, opens Selection unless a proposal waits, and brings the map into view with the focus. |
| 4 | Under 1024px what a click picked showed below the fold. | The map pane is 55vh (was 70vh), so the inspector starts in view. |
| 5 | A map click never filled an action's Location. | `LocationField` takes the picked hex from a cascading value and offers "Use E4 on board 01". It serves Move, the building entry, setup, Bore Sighting, and placing a DC. The fire panels' selects and their free-text targets are in backlog section 45. |
| 6 | Some Locations and sides read as identifiers. | `DisplayText.Location` and `DisplayText.Side` in the DEFENDER's note, the moving stack's status, Residual FP, the Fire Lane and encirclement lines, the card's OB groups, and the Location options of the fire, ordnance, Bounding Fire, and Close Combat panels. |
| 7 | While Confirm waited, a counter click kept the Proposal tab but hid the unit's details. | The Proposal tab says "Picked: F5 on board 01, 2 counters; the Selection tab has its details.", counted from the view's overlay. |
| 8 | `SelectedUnitInspector` kept its own conditions text. | It uses `DisplayText.ConditionsOf` and `DisplayText.Kind`. |

## Referee findings

No component of the batch receives the full game state in place of the view: the workspace's units, view, and marks all come from `GameMaps.Layers`, which reads the same projection as Play's own view. Five findings:

| # | Finding | New? | Fix |
|---|---|---|---|
| 1 | The hex from "Back to Play" survived a change of view before the hand-over was confirmed, and `hex=` stayed in the address, so a reload or another game picked it again. It leaked one hex, no unit. | New | A change of view drops the waiting hex; the address loses `hex=` once it is read and on a game change. |
| 2 | The hand-over on load could be dropped without confirming: pick another view, then the first again. | As on main | The view shown again drops a hand-over only once it has been confirmed since the load or the game change (`unconfirmed`). |
| 3 | "Prisoner of", "Produced from", and "Inside" in the inspector, and "guarded by" and "guards" in the units table, wrote the id of a unit the view does not hold. | The viewer and the table did so on main; Play's inspector is new | A side's view reads "a unit not in view"; the table lists only the prisoners the view holds. The adjudicator reads the ids. |
| 4 | The board viewer shows any view of a live game with no hand-over. | As on main | None here: the decision is the user's (backlog section 45). The batch does not widen it. |
| 5 | A copied Location stays on the clipboard across the hand-over. | New on Play | None: terrain text only. |

Checked and found correct: the Selection tab's stack list and the "?" counters, the LOS and Evidence tabs (terrain only), a picked "?" dropped when the view is read again, the Proposal tab's reasons and the DEFENDER's note, the hand-over's clearing (the workspace is unmounted and keyed by game and view), late results dropped by `AskedBy`, storage (only the map's turn and scroll positions), and the game line.

## UI and Blazor findings

| # | Finding | Fix |
|---|---|---|
| 1 | A map load or a layer that failed was tried again on every render, without end. | `BoardWorkspace` keeps the failed key; only "Try again" clears it. |
| 2 | "Rotate map" with the side-by-side comparison drew the two maps over each other. | "Rotate map" is disabled in that mode, and a turned map is turned back when the mode opens. |
| 3 | The Maps slot diagram had no upper bound: a typed column of 99999 drew about 100,000 slots. | Above 12 columns or rows the diagram is replaced by a note. A slot with three boards says so. |
| 4 | The test project referred to the retired `PlaySelectedHex` and the old ids. | Fixed at the merge gate (the design's section 16). |
| 5 | The focus was lost after Cancel and after a committed Confirm. | The actions take the focus (with the table player's finding 1); in the wide layouts without scrolling. |
| 6 | The Fidelity legend promised focus the chips could not take. | A chip that failed or differs, with a detail, takes the keyboard focus; the legend says so. |
| 7 | Play's inspector pane had lost its accessible name. | `aria-label="Review"` again. |
| 8 | `hex=` was never taken out of the address. | With the referee's finding 1. |
| 9 | The board viewer had no narrow layout: at 320px the map's column was 0px wide. | Under 1024px the viewer stacks: the map at 60vh, the inspector below, the page scrolling. |
| 10 | The slot diagram scaled its text with its width. | The diagram keeps its own size and scrolls sideways inside its figure. |
| 11 | Two view changes on the board viewer could finish out of order. | A request counter drops the earlier one. |
| 12 | Clicks on a turned map may be wrong in Firefox (suspected, not reproduced). | Backlog section 45. |
| 13 | More sides and Good Order values read as identifiers. | `DisplayText` in the move help's summary, the fire help, the read case's Good Order, the setup pools, the victory standing, and "would win" on the card. |
| 14 | Duplicate and dead CSS. | The repeated Unit Lab rule and `.play-context-line` are removed. |
| 15 | Copies of the `asl:` prefix removal. | `DisplayText.Kind` in the inspector and three places in Play. `GameText.Plain` stays: the Game states page's own helper. |
| 16 | Good Order could read "Unknown: not recorded whether " with nothing after it. | Plain "Unknown" when no condition is named. |
| 17 | The viewer's "Counter style" had another accessible name; its map had none. | The `aria-label` is removed; the map is "The board's map". |
| 18 | Each map click drew Play's actions pane twice. | The workspace leaves the second draw to a host that binds the hex. |
| 19 | A turned map in a tall pane cannot be panned by keyboard. | Backlog section 45. |

## Found in the Studio check

- After a commit from 1024px to 1439px, bringing the actions into view scrolled the review under the sticky context. The focus now moves without scrolling in the wide layouts (`reveal(element, scroll)` in `playWorkspace.js`).
- With every board listed, the Fidelity page had 940 focusable chips. Only chips that failed or differ take the focus.
- The moving stack's status read "r3 in bd01:G4:0"; it now reads "r3 in G4 on board 01".
- The revision control on Game states wraps at 320px with no overflow (committed in the last session, checked here).
- Once, after a commit that opened the German Movement Phase at 1366x768, the focus was on "Rotate map" and not on the actions. It did not happen again in the moves, passes, and phase ends that followed in three views, and no cause was found; it is a row in backlog section 45.

## Left out

Backlog section 45 holds what the pass leaves: the fire panels' Locations from the map, arrow-key panning, the Firefox check of a turned map, the units table's Locations in words, the focus seen once on "Rotate map", and the two rows it already had.

## Verification

The design's section 16 has the suite's numbers, the Docker check, and the chart supplement. The Docker check found one thing the local suite did not: 9 Studio tests raced in the Linux container, which `ImmediateProposals` answers for the Play page tests.
