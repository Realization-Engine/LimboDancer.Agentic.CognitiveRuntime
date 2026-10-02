# ASL Unit Backlog Pass 29 Design

**Status:** In progress. Tasks 29.1 to 29.4 are built and checked in the Studio; 29.5 is not started. The unit tests, the reviews, and the merge gate wait until the batch of UI work on branch `ui-improvements` is done (the user's direction, 2026-10-02). Pass 29 (The shared board workspace) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, a Studio pass.

**Date:** 2026-10-02

**Related documents:** the plan's sections 13.1, 13.4, 15.5, 15.10, and 15.11; the [pass 28c design](<ASL Unit Backlog Pass 28c Design.md>); and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 44.

A Studio pass adds no rulings. Its design decisions are recorded here and in the plan's section 15.11.

## 1. Outcome

The user compared the board viewer's inspector with Play's review pane. The board viewer shows a picked unit's details, the units in a hex, the hex's terrain facts, LOS as a tab, and Evidence (grid samples and provenance). Play showed a list of unit ids in the picked hex, and its LOS form sat collapsed under the map. The two were separate code and would keep drifting apart.

The pass extracts one map-and-inspector component from the board viewer, and both pages now host it. On Play, the proposal review is the inspector's first tab.

The user's answers on 2026-10-02:

- **Proposal as a tab.** The review is a Proposal tab, chosen and marked when a proposal arrives, rather than a pane pinned above the tabs.
- **Order.** The four tasks were built in one go, on branch `ui-improvements`, each checked in the Studio.
- **Numbering.** This is pass 29. Every planned pass from the old 29 moved up by one: DYO is now passes 30 and 31 (still deferred), and the rule packages are 32 to 41 with 33b.

## 2. The components

| Component | Folder | What it is |
|---|---|---|
| `BoardInspector` | Components/Board | The tabbed pane alone, presentational: Selection, LOS, and Evidence, with any tabs the host adds placed first. It takes the board, the hex kept and the hex hovered, the units already read for the view, the picked unit, the view, the pixel clicked, the LOS draft and result, and the open tab. It shows only what it is given. |
| `BoardWorkspace` | Components/Board | B06 `BoardViewport` and `BoardInspector`, with the logic lifted out of the board viewer page: click, hover, Escape, a counter click, the highlight, the pixel sample, and the LOS check. It loads the board when the board, its view, or its trace changes, and sends each layer (units, LOS, marks, highlight, visible layers, comparison) only when it changes. A failed load shows its reason and a Try again button. |

`BoardWorkspace`'s contract:

- **The host gives:** the board; the view, trace, visible layers, and comparison (the board viewer only); the unit overlay already read for the view, and the view; a marks layer (Play only); a toolbar and a footer.
- **The host binds:** the hex kept (`Selected`) and the open tab (`Panel`).
- **The workspace keeps:** the hex hovered, the picked unit, the pixel sample, and the LOS draft and check. A unit the overlay no longer draws, or one outside the hex kept, is dropped.
- **Layout.** The stage (toolbar, map, footer) and the inspector are sibling elements, not one wrapper, so each host places them in its own grid. The host names their classes and attributes, may put the inspector first, and may put markup between the two (`Between`).
- **Methods.** `Zoom`, `Fit`, and `Inspector` (the inspector's element, for a host that brings it into view).

## 3. The board viewer (29.1, 29.2)

The page keeps its route and query parameters, the board's load, the unit sources (placement sets and games, perspective, revision, counter sheet), and its study controls: the view picker, comparison, layers, and trace, given to the workspace as its toolbar. The Styled view's vectorization runs before the view that needs it is drawn. Nothing it shows or does changed; the tab, panel, and LOS ids are the same (`viewer-tab-*`, `los-*`).

## 4. The game's overlay (29.3)

`GameMaps.Layers` returns, beside the SVG layers, the `UnitOverlay` the units were drawn from and the `GameView` they were read for (`GameMapLayers.Overlay` and `View`). The inspector's Selection tab reads only these, so it shows what the view may see:

- a concealed unit is "<side> concealed unit", with its identity and conditions withheld from the other side;
- a unit's facts in the game come from the view, never the full state.

Two helpers convert between the page's picked Location and the map's hex: `GameMaps.LocationOf(board, hex)` (the board and hex that own a hex of a composed map) and `GameMaps.FactsAt(board, location)`.

## 5. Play on the workspace (29.4)

| Before | Now |
|---|---|
| The review pane: the proposal, then the picked hex's list (`PlaySelectedHex`) | The inspector: Proposal, Selection, LOS, and Evidence tabs |
| `PlayMapPanel` on B06 | `BoardWorkspace`'s stage, with Play's zoom buttons and the Residual FP line as its toolbar |
| The LOS form under the map, with a "From unit" select | The LOS tab, with "selected hex" for either end; a counter click picks its hex |
| The page drew the picked hex and the LOS line through `GameMaps.Layers` | The workspace draws both; the page's map cache keys only on the game, revision, view, and Gun |

- **The Proposal tab.** It holds the existing review: the "Nothing to review" note, or the proposal with Confirm and Cancel, busy and Stale as in pass 28c. While a proposal waits, the tab reads "Proposal (1)". A new proposal chooses the tab and takes the focus there; the header's "Review: <action>" button does the same from any tab.
- **Selection.** A click on the map, a click on a counter, "Locate" in the units table, and the DEFENDER's "Show it on the map" all pick a hex. The tab shows the picked unit's details, the units in the hex, and the hex's terrain facts.
- **The hand-over** still unmounts the workspace, so the next view starts with no hex kept, no LOS line, and the Selection tab; the proposal is cleared with the view, as before.
- **The layout** keeps pass 28c's breakpoints and ids (`play-panel-review`, `play-panel-map`, `play-panel-actions`, `play-panel-activity`, `play-map-zoom-in`, `play-map-fit`). The inspector comes first in the page, then the narrow tabs and the actions (as the workspace's `Between`), then the map. Under 1024px the inspector sits above the Map, Actions, and Activity tabs and scrolls within 55% of the window's height; it always shows, since its tabs are always useful.
- **Retired:** `PlayMapPanel`, `PlaySelectedHex`, the `play-los` form, and their styles.

## 6. Disclosure

- The workspace receives the overlay already read for the view; it never sees the game's state.
- Hex facts, Evidence, and LOS are terrain only, so every view may read them (as on the board viewer).
- The hand-over clears everything of the last view: the workspace is unmounted, and the page clears the picked hex, the proposal, and the open tab.

## 7. Studio checks

Checked in the Studio on 2026-10-02.

- **29.1**, in the user's Studio on port 5178, on `/boards/bd01` with the synthetic game: a hex click, a counter click, the tabs by keyboard (Right and End), LOS from the selected hex with its line drawn and cleared, Evidence, Escape, and hover.
- **29.2 to 29.4**, in a Studio on port 6670:
  - the board viewer: a counter click with the unit's details, LOS kept across a change to the Hex Facts view, a layer toggled, and the Russian perspective;
  - Play on `p28c-walk2` in the German view: the Russian "?" shown only as a concealed unit; a proposal choosing and focusing the Proposal tab; Review from the LOS tab; Cancel; Locate; LOS; the hand-over to the Russian view clearing the hex, the line, and the tab;
  - the widths 1920x1080, 1366x768, 1024x768, 683x384, and 320x640: no page scrolls sideways, and at 320px the map keeps 258 by 346 pixels.

One issue was found and fixed: a counter click kept the hex but lost the unit's details, because the unit was set after the hex's change had rendered.

## 8. Open

- **29.5, the selection in the board link** (`Games.BoardLink` with `hex=`), is not started.
- **Narrow windows.** With the Map tab open, a click on the map fills the inspector above the tabs, out of sight until the page scrolls up.
- **Tests, at the end of the batch.** The component tests of `PlayMapPanel` and `PlaySelectedHex` go, and the Play page tests that read the old map panel, `play-selected`, or `play-los-*` move to the workspace. New component tests for `BoardInspector` and `BoardWorkspace`.
