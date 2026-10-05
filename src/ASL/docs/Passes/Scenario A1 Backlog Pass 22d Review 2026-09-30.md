# Scenario A1 Backlog Pass 22d: The Workspaces

**Status:** Reviewed. Pass 22d of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>): the board viewport, the board viewer, the board editor, and the Unit Lab as components. No package changes.

**Date:** 2026-09-30

**Plan:** the Redesign Plan, pass 22d (tasks 22d.1 to 22d.4) and sections 16.17 and 17.2. A UI and Blazor reviewer read the change; a table player who inspects boards, checks LOS, and designs counters read it and the rendered pages. Both were read-only.

**Design:** [ASL Unit Backlog Pass 22d Design](<ASL Unit Backlog Pass 22d Design.md>).

## Candidates

Extracted: all 20 P1 (B01 to B07, B09, B10, B12 to B15, U01 to U03, U06, U08, U09, E01) and 8 of the P2 and P3 (U04, U05, U07, S14, E03, E04, E06, E08). Left inline, as section 16.17 allows: B08 `UnitGameFacts` and B11 `LosResult` (each a short block inside its extracted parent), U10 `PlacedUnitList` (a short list in U09), E02 `UnitIdentityFields` (grouped as a fieldset in the unit editor instead), E05 `UnitStateChecks` (one list), and E07 `HexFeatureList` (used only by the inspector).

## Visual checks

Before the reviews: the viewer on bd01 (the Layers panel, the replay strip, a unit clicked, Selection, LOS from the selected hex, Evidence, the Russian view clearing a hidden German unit, Comparison in swipe, and a route to bd02 reloading the one viewport), the editor (the header's states, a rename, leaving declined then accepted, the tool options), and the Unit Lab (groups, previews beside the fields, findings, the style sheet in Advanced); no overflow at 1100 and 1300px. The browser pane stopped drawing partway, so later checks read the page state, computed styles, and measurements. Once, while the pane was not drawing, a navigation from the editor stalled after a declined question; it did not recur in five later tries. After the review fixes: the Lab's moved editors styled again, the LOS badges, a route dropping the old LOS line, and a name typed back reading saved. Clean.

## UI and Blazor findings

| Finding | Disposition |
|---|---|
| 1. The Lab's rules no longer reached the style sheet and placement moved below it | Fixed. |
| 2. The leave question could time out after a minute and end the circuit | Fixed: it runs with the navigation's cancellation; a failure stays on the page. |
| 3. Findings lost each diagnostic's path | Fixed. |
| 4. Disposal while the module loaded left the module and .NET reference | Fixed. |
| 5. A command sent during disposal could reach a disposed reference | Fixed. |
| 6. Home and End also scroll the inspector | Backlog: Blazor cannot prevent the default for some keys only. |
| 7. A route to another board kept the old LOS line and the editor's selection | Fixed. |
| 8. The Advanced section stayed closed over the sheet's errors | Fixed. |
| Clean | The viewport's ordering and single instance, the callbacks, the keys and draft resets, disclosure in the viewer, the tabs' ARIA, markup and ids, and old behavior. |

## Table player findings

| Finding | Disposition |
|---|---|
| 1. On a board that is not verified, the LOS badge said clear or blocked while the text said not definitive | Fixed: "clear, not definitive" or "blocked, not definitive", in the warning tone. |
| 2. A mistyped Location read "not answered", like a rule not built | Fixed: "not read". |
| 3. Clicking a unit on the LOS tab left the tab | Fixed. |
| 4. Nothing said Escape releases a kept hex | Fixed: the hint says so. |
| 5. "Pixel under the pointer" was the pixel clicked | Fixed. |
| 6. The Evidence tab counted findings shown elsewhere | Fixed: no count. |
| 7. The replay strip did not say what a perspective sees | Fixed. |
| 8. A board click left the Validation list | Fixed. |
| 9. A name typed back still read as unsaved | Fixed. |
| 10. The face gallery went blank without saying why when the sheet had errors | Fixed. |
| 11. The placeholder and the states were unclear | Fixed: '"?" placeholder only', and a state that turns a face says which; grouping the states by kind is backlog. |
| Earlier than 22d | The unit sources with the same label and the layer ids: backlog. |
