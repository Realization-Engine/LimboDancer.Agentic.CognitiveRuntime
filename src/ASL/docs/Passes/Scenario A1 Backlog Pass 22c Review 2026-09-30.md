# Scenario A1 Backlog Pass 22c: The Collection Pages

**Status:** Reviewed. Pass 22c of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>): the Board library, Maps and New board, Game states, Fidelity, and Settings as components, and a searchable library. No package changes.

**Date:** 2026-09-30

**Plan:** the Redesign Plan, pass 22c (tasks 22c.1 to 22c.4) and sections 16.17 and 17.2. A UI and Blazor reviewer read the change; a table player who builds scenario maps read it as such a user. Both were read-only.

**Design:** [ASL Unit Backlog Pass 22c Design](<ASL Unit Backlog Pass 22c Design.md>).

## Candidates

The 19 P1 candidates are extracted, and M05 `MapBuildActions` (P2), since it holds the out-of-date logic. Left inline, as section 16.17 allows: H04 `LibraryReportContext` (one sentence and a link, now beside the filter bar that took its toggle), H06 `BoardVerificationRow` (the row is simple once resolved upstream), M02 `MapPlacementRow`, M07 `BoardDimensionsFields`, G02 `GameReadFindings`, G05 `GameEquipmentTable`, G08 `PerspectiveEventList`, F03 `LosFidelityResultsTable`, F07 `FidelityDifferences`, F08 `FidelityCheckBadges` (each a short block inside an extracted parent, rendered nowhere else), and F09 `StudioConfigurationReport` (a small read-only page; its cache measure moved out of render instead).

## Visual checks

Before the reviews: the library (search, status, out of scope, a viewer opened and the back button, with filter and scroll kept), Maps (placements, rules, the compact form, a check then an edit), New board (a VASL draft notice, a taken reference refused), Game states (the German view at revision 8, a case read), Fidelity, and Settings. Fixed: a green "VASL builds this map" shown while out of date; the report picker and the library note without space. After the review fixes: the VASL-only filters disabled when Show excludes VASL boards, the single no-match message, rule Up and Down, board titles, the slider. Fixed: the rule buttons without space, and disabled link buttons that looked active. Clean.

## UI and Blazor findings

| Finding | Disposition |
|---|---|
| 1. A rule named twice (text, query, or a saved map) gave two list items with one key, which ends the circuit | Fixed: a rule is kept once; tested. |
| 2. Opening another saved map after a check showed that check as this map's, out of date | Fixed: loading a map or text clears the result; tested. |
| 3. The revision field's width rule shrank the Game states slider | Fixed: it applies to the number field only. |
| 4. A failed save was later called "the last check" | Fixed: "the last result". |
| 5. The Advanced section closed itself when a problem cleared | Fixed. |
| 6. A page closed while the scroll module loaded kept its listener | Fixed. |
| 7. A board in the folder and in drafts shared a row key | Fixed. |
| 8. Fidelity's status regions appeared already filled and announced every board | Fixed: one region per job, always present, at start and end. |
| 9. A comment said the filter was kept in the address | Fixed. |
| 10. Missing tests | Added: duplicate rules, another map after a check, the new-board form. |
| Clean | The filter and typing, disclosure on Game states, the draft resets, keys elsewhere, table markup, element ids, and old behavior. |

## Table player findings

| Finding | Disposition |
|---|---|
| 1. Old findings stayed under an out-of-date result, looking current | Fixed: hidden while out of date. |
| 2. Another map's result carried over (as UI finding 2) | Fixed. |
| 3. A failed save called a check (as UI finding 4) | Fixed. |
| 4. The VASL status left authored boards and maps listed; the VASL controls stayed live for other types | Fixed: a status narrows to VASL boards; the controls are disabled when Show excludes them. |
| 5. The count included hidden out-of-scope boards | Fixed. |
| 6. Four empty messages for one no-match | Fixed: one. |
| 7. The scroll was restored on every visit | Fixed: only on back or forward. |
| 8. Rules could not be reordered | Fixed: Up and Down. |
| 9. The board picker showed bare references | Fixed: titles shown. |
| 10. Typed text replaced by edits above without warning | Fixed: the help says so. |
| 11. The filter comment (as UI finding 9) | Fixed. |

## Full suite

The first full run failed one MapStudio test that had passed alone: a check still running when another map was opened reported on the new map. The page now discards a check result whose map was replaced while it ran. The new page tests also read two states straight after an asynchronous step (the library's first render, a map check), which made them depend on timing; they now wait for those states. MapStudio then passed three runs in a row.
