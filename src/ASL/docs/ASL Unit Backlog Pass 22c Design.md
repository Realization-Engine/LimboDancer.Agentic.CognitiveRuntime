# ASL Unit Backlog Pass 22c Design

**Status:** Built. Pass 22c (the collection pages) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>).

**Date:** 2026-09-30

**Related documents:** the [Scenario A1 Backlog Pass 22c Review](<Scenario A1 Backlog Pass 22c Review 2026-09-30.md>), the plan's sections 12.1, 12.2, 14, 15, and 16.4 to 16.9, and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 35.

The pass adds no rule, ruling, counter, or package, and changes no service's behavior: the pages compose the same services as before.

## 1. Outcome

- **The Board library (22c.1).** `Components/Library`: H01 `AuthoredBoardList`, H02 `MapSummaryList` (also on Maps, with Edit and Delete there only), H03 `BoardScopeSummary` (also the Fidelity report's totals, without "not checked"), H05 `VaslBoardTable`, and `LibraryFilterBar`. The page reads the listing once when it opens and resolves each VASL board's row then (a board loaded in this session, else a fresh batch result, else not checked, or out of scope), not on each render. Search matches a name or reference, ignoring case; Show picks everything, authored boards, maps, or VASL boards; a VASL status narrows the list to VASL boards; out-of-scope boards show on request. The count leaves out hidden out-of-scope boards, and Clear filters appears when a filter is set. Loading, a source that is not configured, a library that cannot be read, no authored boards or maps, a checkout with no boards, and nothing matching are each said in words, and nothing matching is said once. The filter is kept for the browser session (`LibraryViewState`, scoped), and the scroll position for the tab (`scrollMemory.js`, restored only on back or forward, so a list opened from the navigation starts at the top).
- **Maps and New board (22c.2).** `Components/Maps`: M01 `MapPlacementEditor` (rows keyed by a draft id, so removing one keeps the others; each board listed with its title), M03 `ScenarioRuleEditor` (the rules in the order they apply, moved up or down, a rule kept once), M04 `PlacementTextEditor` (the compact form in an Advanced section that stays open while it reports a problem), M05 `MapBuildActions` (Check and Save through S04; after an edit the last result is kept, labeled out of date, with its findings hidden; opening another map or applying text clears it, and a check still running when another map is opened is discarded), and M06 `NewBoardForm` with M08 `SourceDraftNotice` (a warning, also on the board editor). There is no drawn preview of an unsaved map; the last result stands for it (plan section 12.2 now says so).
- **Game states (22c.3).** `Components/Games`: G01 `GameReplayToolbar` composing S09 `PerspectivePicker` and S10 `RevisionNavigator` (now with a slider, caller-given button ids, and a refused or clamped value drawn again), G03 `GameContextSummary` (for Play's context header in pass 28c), G04 `ProjectedGameUnitTable`, G06 `ReadCaseForm`, G07 `ReadCaseResult`, and `GameText` for the wording they share. Every component receives only the projected view or the perspective-bound case read; the page prepares the rows when the game, revision, or perspective changes.
- **Fidelity and Settings (22c.4).** `Components/Fidelity`: F01 `FidelityRunControls`, F02 `LosFidelityPanel`, F04 `FidelityReportPicker`, F05 `FidelityReportMetadata`, and F06 `FidelityResultsTable`. Each job has one status region, always present, that says when it starts and ends rather than announcing every board. Settings measures the cache once when it opens.
- **Styles.** A disabled link button reads as plain text; the rule actions are spaced; the filter bar has its own block.

## 2. Tests

`CollectionPageTests` (MapStudio): the library's search, type, status, and out-of-scope filters, the count, the single no-match message, Clear, and the filter kept on return; a library that cannot be read; the composer's placement identity on removal, rule order and moves, a duplicate rule kept once, the out-of-date result and its hidden findings, text that is not placements, and no old result after opening another map; the new-board form's reference and draft warning; the run controls. `GameStatesTests` pass unchanged through the extracted components; the library page test reads the new toggle label.

## 3. Left out

Section 35 of the backlog: a drawn preview of an unsaved map, searchable board and SSR pickers, and the filters in the address.
