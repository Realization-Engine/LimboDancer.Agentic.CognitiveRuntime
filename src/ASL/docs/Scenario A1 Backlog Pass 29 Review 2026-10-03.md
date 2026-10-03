# Scenario A1 Backlog Pass 29: The Shared Board Workspace and the UI Batch

**Date:** 2026-10-03 (draft; the reviews are not finished)

**Branch:** `ui-improvements`

**Related documents:** the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>) (sections 1 to 14), the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>) (pass 29, section 15.11), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 44 and 45.

## Status

The batch's code is built to commit eb1e523 and checked in the Studio as it was written (the design's sections 7 to 14 record those checks). Three reviews were started on 2026-10-03; only the table player's finished before the user moved the work to a new session. The referee and the UI and Blazor reviews are to be run again there, their findings fixed, the Studio check repeated, and then the merge gate.

## Table player findings (not yet fixed)

The reviewer read the code at eb1e523 and walked a turn for each side and the adjudicator. Wide windows (1440px and up) play well: the workspace, the Proposal tab and its hold on map clicks, the hand-overs, and the turned map. The trouble is in the 1024px to 1439px band, under 1024px, and one gap: a map click never fills an action's Location.

1. **Under 1024px every action bounces between tabs.** A proposal opens Map with the Proposal tab; after Confirm the player stays there and must open Actions again. Fix: after a commit, when narrow, set `paneTab = "actions"` and `focusActions = true` (Play.razor, in Confirm after a committed result). Quick.
2. **From 1024px to 1439px Confirm can be scrolled out of sight.** The review pane is capped at 40vh (site.css, the 1439px media rule) and Confirm and Cancel come after the reasons and the fire details (ProposalReviewPanel.razor). Fix: put the buttons right after the outcome line, or keep them sticky at the pane's foot. Quick.
3. **"Show it on the map" and Locate only set the picked hex.** Under 1024px "Show it on the map" is in the Actions tab, so nothing visible happens; Locate sits below the workspace, so the hex lights up on a map off screen; the inspector stays on its tab. Fix: one `LocateHex(at)` that picks the hex, opens the Map tab when narrow, opens Selection unless a proposal waits, and scrolls the map into view (`playWorkspace.js` reveal). Quick.
4. **Under 1024px what a click picks shows below the fold.** The map pane is 70vh and the inspector sits under it. Fix: about 55vh for the map, or scroll the inspector into view (block "nearest") after a click in the narrow layout. Quick.
5. **A map click never fills an action's Location.** Move "To" and the setup Location are typed; fire From and Target are selects of raw ids. Fix: `LocationField` takes an optional picked value and shows "Use E4 on board 01"; Play passes its picked hex. About an hour. (Setup picking is pass 30's task 30.2; the other actions are new.)
6. **Some Locations and sides still read as identifiers:** the DEFENDER's note ("The moving stack is at bd01:E4:0"), Residual FP, the encirclement lines (lowercase side), the card's OB groups ("german: ..."), and the fire select options (SmallArmsFirePanel). Fix: `DisplayText.Location` and `DisplayText.Side`. Quick.
7. **While Confirm waits, a counter click keeps the Proposal tab but hides the unit's details.** Fix: a one-line note in the Proposal tab, "Picked: E4 on board 01, 3 units; see Selection". Quick.
8. **SelectedUnitInspector keeps its own conditions text** although `DisplayText.ConditionsOf` replaced the copies elsewhere. Fix: use the shared helper. Quick.

## Referee findings

Not yet run (stopped when the session moved).

## UI and Blazor findings

Not yet run (stopped when the session moved).
