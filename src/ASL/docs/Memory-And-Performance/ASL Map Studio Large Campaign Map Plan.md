# ASL Map Studio Large Campaign Map Plan

**Written:** 2026-11-01, at the user's word: "All proposals accepted", then "If so, write it" (a design for the work the analysis orders).
**Source:** `src/ASL/docs/Memory-And-Performance/ASL Map Studio Large Campaign Map Analysis.md` (the analysis): its measurements (section 3), the code (section 4), the options (section 6), the order (section 7), and the decisions P1 to P4 (section 8), all accepted on 2026-11-01.
**Scope:** the first wave of the analysis's order: M2 and M1 (measure), A1 to A4 (the event path), B1 (labels on demand), B2 (the grid drawn once and repeated), and B3 (pan by transform), in one branch and one pull request. Not in scope: M3 and C1 to C4 (the setup panel and page renders), B4 (raster tiles), B5, B6, D1, and D2. Each of those follows its own measurement, after this wave, as the analysis's order says.
**Status:** built on branch `claude/large-map-pan-zoom` (2026-11-01) at the user's word to carry it without intervention: tasks 0 to 5 and 7; task 6 (B3) built, measured worse, and removed; task 2's ordinary-browser measurement waits for the user's browser. The results are in the analysis, section 10. The merge waits for a green CI run and the user's word.

## 1. Goal and expected gains

Make panning and zooming a large map smooth, without changing what any page shows or what any rule decides.

Stated up front (the user's rule of 2026-10-19 for performance plans): the baseline, the expected result, how it is measured, and when to stop.

| Measure | Baseline (analysis, section 3, browser pane) | Expected after this plan | Stop threshold |
|---|---|---|---|
| Forced layout per pan or zoom event | 628 to 1,185 ms | 0 on the event path; at most one layout per frame | No synchronous layout read in any event handler |
| One pan step (shift, two frames) with every layer shown | 951 to 1,203 ms | 100 to 300 ms after A1 to A4, B1, and B2 | Under 300 ms in an ordinary browser |
| Frames during a drag | about 1 per second | 50 per second or more, by B3 | 50 per second or more in an ordinary browser, longest frame under 50 ms |
| Settling at the end of a gesture | (not measured) | one layout and paint, about the pan step above | Under 300 ms |
| Turning the hex labels on or off | about a minute, measured as the label group's detach and reattach | under 100 ms, by B1 | Under 100 ms |
| Board SVG characters | 12,184,195 | about 5.5 million, by B1 (-1.7 M) and B2 (-4.9 M) | Recorded, not a gate |
| Board SVG elements | 45,079 | about 4,700 plus the labels in view | Recorded, not a gate |

These are estimates from the analysis's measurements, not promises. If A1 to B3 do not reach the thresholds on this map in an ordinary browser, the result is recorded and B4 (raster tiles) is proposed as the next step, by decision P3.

## 2. How the work is done

- **One branch from `main`, one pull request,** with a commit per task.
- **Studio first.** Each task is built, then checked in my Studio (port 6670, `build_studio.sh`, launch entry `map-studio-campaign-6670`), on the Goodwood reference (section 2.1), before its unit tests are written or run (the user's rule of 2026-10-19). Then only the tests that assert the changed behavior run (the user's rule; the full suite waits for the gate).
- **Every page that draws a map** is checked, since all use one viewport: Play (`BoardWorkspace`), Replay, the board viewer (`BoardViewer.razor`, with its layer switches, comparison, and side-by-side view), the map author page (`Author.razor`), the card map picker (`CardMapPicker.razor`), and the hand-over map (`HandOverMap.razor`). Turned and unturned; at the widths the Studio check names.
- **The merge gate** (`code-builders/merge-gate/gate.sh`): its `base` stage runs when the branch is cut (task 0), under a pass configuration `code-builders/merge-gate/pass-configs/pass<id>.conf` written in task 0; its `strict` stage runs before the documents commit; the rest before the pull request (task 8). No recorded game may replay or sweep differently, since nothing here changes play. The Node viewport test (`boardViewport.pointer.test.mjs`) is run by hand at the gate, as CI runs it and the local suite does not.
- **Tests named by method** (the user's rule): each task names the test methods that assert its change, and only those run before the gate. Tests that CI runs use the synthetic board and a composed synthetic map; bd01 needs the VASL checkout (`[VaslFact]`) and the Goodwood reference lives outside the repository, so those two are measurements and Studio checks, not CI tests.
- **Outside build outputs** are registered as made and cleaned at the end (the user's rule of 2026-10-19). Task durations go in the time log by `timelog_row.py`.

### 2.1 The reference workload (decision P4)

The Goodwood hand-off of 2026-11-01 (`campaign-21f5abd45868a5100c5b8475`: three joined boards, 624 counters, Turn 1 setup) is the reference for map size. Task 0 saves its game record, board package, and Campaign context outside the repository, beside the other proof games, so every measurement in this plan opens the same map. `dl-tractor-works-02` stays the reference for play steps and is used here as the check that a stock multi-board map (placed boards, seams) still draws right.

## 3. Tasks

### Task 0: branch and reference

Cut the branch from an up-to-date `main`. Write the pass configuration and run the gate's `base` stage. Save the reference workload (2.1). Register the outside outputs. Record the start in the time log.

### Task 1: the pan probe (M2)

A probe in `boardViewport.js`, enabled by `?mapMetrics=1` (which already turns on the load timer, lines 251 and 673 to 684), and run from the console or by a button the metrics mode shows:

- **Scripted gestures:** a drag of a fixed screen distance over one second, and a burst of 20 wheel events at the center, both through the real event handlers (dispatched events, not direct `setBox` calls), so the probe measures what a user's gesture costs.
- **Readings:** frames painted, the longest frame, and, where the browser provides it, the Long Animation Frames entries (as the SVG Memory Analysis did, sections 12 and 13), with their forced-style-and-layout time; plus the time of the real label layer switch (the analysis measured a detach and reattach as a stand-in).
- **Output:** a JSON record logged to the console and written to a `data-map-probe` attribute on the host, with the board, the counter count, the viewport size, and the browser's user agent.
- **Restores** the view and the layer choices it found.

The probe ships with the product (it is off unless the URL asks for it), so the same measurement serves every later task and every later map. A Node test checks its arithmetic on a faked frame clock.

### Task 2: the baseline (M1)

Measure the reference with the probe in two browsers: the Claude browser pane, to compare with the analysis, and an ordinary browser. The ordinary browser is the user's Chrome, through Claude in Chrome with the user's leave, or the user runs the probe URL and pastes its record. Record both in the analysis as an addendum to section 3. If the ordinary browser shows a different split (the forced layout no longer dominating), the order of tasks 3 to 6 is reviewed with the user before going on.

### Task 3: the event path (A1 to A4)

All in `boardViewport.js`. Task 3 is done and measured in two steps: A3 first, alone (it may remove much of the label cost by itself, since today's rewrite of an attribute on the 40,442-node group can invalidate its style), then A1, A2, and A4.

- **A3, write only on change.** `updateLabelDetail` compares with the attribute it holds before writing, as `updateUnitTier` already does. Measured with the probe before going on.
- **A1, one DOM update per frame.** `state.box` (and `fitted`) still change at once, in every caller (`zoomAt`, `zoomCenter`, `revealPoint`, `fitRect`, `reset`, the keyboard zoom, the drag), so a burst of wheel events compounds its zoom steps as today. Only the DOM writes move to one `requestAnimationFrame` callback (`flushBox`): the `viewBox` on the board and the side view, the unit tier, the label detail. Where no frame scheduler exists (Node), `flushBox` runs at once, so the module's behavior without a browser is unchanged.
- **A2, no layout read on the event path.** The SVG's size comes from a `ResizeObserver` on the SVG element itself (not the host: side by side, each SVG is half the host, site.css 936), by extending the existing `shapeWatcher` rather than adding a second observer; in a turned map its box is 100cqh by 100cqw, which equals today's `clientWidth`. Its screen position comes from one `getBoundingClientRect()`, taken again at `pointerenter`, at `pointerdown`, and at the start of each frame callback before any write, when layout is clean, since the page can move without a scroll or resize (the stacked layout at 60vh, content added above). `toBoard` converts a pointer position from that rectangle and the current box, applying the default `preserveAspectRatio` (`xMidYMid meet`) letterboxing and, for a turned map, the quarter turn, as `getScreenCTM()` does today; `getScreenCTM()` stays as the fallback when no rectangle is known yet. `unitTier`, the label rule, `fitRect`, and `fitBox` read the observed size, not `clientWidth`.
- **A4, save the view at rest.** A programmatic change (`zoom`, `reset`, `fitRect`, `reveal`, `focusPoint`) saves the view at once, as today, so the saved-view test (lines 110 to 140) holds without graceful cleanup. A gesture saves when it ends: pointer up, or 200 ms after the last wheel event (one idle delay, shared with task 6), and on `pagehide`. The `pagehide` listener is removed in `dispose` and is not added where no `window` exists.

Hover keeps its 100 ms limit to .NET and uses the new `toBoard`, so a mouse move no longer forces a layout.

What A1 and A2 do not do: they move the layout out of the event handlers into the frame and stop paying it per event, but one frame's layout still costs what the page's DOM costs; that cost falls only when B1 lands.

**Test fakes** (`boardViewport.pointer.test.mjs`): today they have no `ResizeObserver`, `getBoundingClientRect`, frame queue (only one test installs one, line 163), `host.clientWidth` or `clientHeight`, `svg.style`, or window listeners; their handler map keeps one handler per event name; and `getScreenCTM` returns a constant. Task 3 extends them with a fake observer the tests fire, a fake frame queue with a flush helper, a rectangle, and a real affine screen matrix covering `meet` letterboxing and the quarter turn. The tier test (lines 102 to 104, `svg.clientWidth = 2000; reset()`) fires the fake observer instead.

**Tests:** a burst of wheel events writes the DOM once per frame and compounds its zoom as today; `toBoard` gives the same board point as the screen-matrix path for an unturned, a turned, and a letterboxed map; the label attribute is not rewritten when unchanged; a gesture's view is saved at its end and a programmatic change's at once; every existing pointer test stays green.

### Task 4: labels on demand (B1, decision P2)

For the Exact view only, the view Play uses; the other views keep their SVG labels.

- **Server.** `BoardRenderer` writes a new layer, `labels.json`: a compact list of hex names and centers. `RenderEndpoints.Render` today serves only names ending in `.svg`, with the content type fixed to `image/svg+xml`, and caches by (board, version, view, layer, trace); it gains a per-layer content type (`application/json` for this one), the distinct layer key `labels.json` (so it never collides with the SVG `labels` layer), and a rename of its `RenderedSvg` record to fit both kinds. Its gzip copy and ETag work as they are. The SVG `labels` layer stays for the other views and for the `document` endpoint, `BoardRenderChecks`, `RenderEndpointTests`, and `RenderCacheTests`, its only other readers (the Studio has no print or image export).
- **Cache busting without unverifying boards.** The browser must not keep the old layer list, but raising `RendererVersion` would make every board unverified until the VASL fidelity batch runs again (`Fidelity.cs`: `renderer` is among the tools `StaleReason` checks; `FreshResult` and `MissingReason` then return no result, and the Fidelity page and Home show "stale"). So the layer request in `BoardViewport.razor`'s `RenderQuery` gains its own token (`layers=2`), and `RendererVersion` stays.
- **Browser.** `fetchGroups` adds `.svg` to every layer name today; it gains a JSON path for `labels.json`, as does the author page's `reloadLayers` (`Author.razor:312`), which reloads `labels` after each edit. `load` reads the list into typed arrays and a coarse spatial index (a grid of cells over the board). The label group keeps its font attributes (`updateLabelDetail` reads the font size from it) and holds a pool of `<text>` elements; at each frame where labels are readable (the existing six-pixel rule), `flushBox` fills the pool with the labels of the hexes in view plus a margin. The pool is sized to what the view needs and never drops a label in view: at the readable limit a 3,840 by 2,160 screen shows about 5,000 hexes. Below the readable size the group is empty. The side view, which clones `layer-labels` today (boardViewport.js 445 to 447), fills its own pool the same way.
- **Meaning kept.** The label layer switch keeps its meaning (the user's hidden-layer choice, `data-layer-hidden`, stays separate from the zoom rule, `data-detail-visible`). A turned map turns its labels upright as today (site.css 4692 to 4696), now for the labels in view only.

**Tests:** C#: the label list for the synthetic board and a composed synthetic map has one entry per hex, with the names and centers the SVG layer has; the endpoint serves it as JSON with gzip and an ETag and keeps the SVG `labels` layer apart. Node: only labels in view are drawn; a label in view is never dropped; toggling the layer adds or removes no label elements beyond the view's; the side view fills its own.

### Task 5: the grid drawn once and repeated (B2)

A composed map is one hex lattice: `MapLayout` builds one `BoardGeometry.Vasl` from the first board's hex size (`MapLayout.cs:115`), and `WriteGrid` draws `input.Grid.Geometry`; reversing a board changes its terrain, not the grid, and seams share a column. So one grid serves the whole map, on a single board or many.

- **Repeated columns, not a tiled pattern.** `BoardRenderer.WriteGrid` writes the outlines and center dots of one pair of columns (an even and an odd column, which differ by the half-hex offset) as one path in `layer-defs`, and places it about `WidthInHexes / 2` times with `<use x="...">`, a few thousand characters in all. This stays exact in vector form, with no tile seams: an SVG `<pattern>` tiled by the lattice's fractional periods risks snapping and drift in the browser, and a tile boundary along a hex edge would draw faint double lines from two tiles' anti-aliasing.
- **The edges.** The map's edge hexes are not regular: `CenterDot` clamps x at the right edge, and `FixMapEdgePoint` moves vertices at -1, `GridWidth`, and `GridHeight` by one unit. The repeated columns cover the interior hexes; the edge hexes are written as a small path of their own, exactly as today.
- **Hit-testing and paint:** the placed copies keep `pointer-events="none"` and today's stroke, opacity, and width.
- **All four views** draw the grid, so the change shows in each, and the golden files change: `Golden/synthetic-*.svg` (`SyntheticBoardMatchesGolden`, `SyntheticStyledViewsMatchGolden`) and the `BoardRenderChecks` SHA-256 values. They are regenerated and their differences reviewed (the grid only). Before changing the grid, task 5 checks whether the VASL fidelity batch compares the grid layer; if it does, task 5 runs the batch again and records the new report.

**Tests:** C#: for the synthetic board and a composed synthetic map, the repeated grid's hex outlines and center dots equal the per-hex path's at every hex (interior and edge); the golden files' differences are the grid only. Studio: the grid looks the same at every zoom on the reference, on `dl-tractor-works-02` (seams and a reversed board), and on bd01, turned and unturned; the `document` endpoint still draws it.

### Task 6: pan by transform (B3)

- **The transform through a custom property.** Setting `svg.style.transform` would override the turned map's rule (`.board-host.rotated > .board-svg { transform: translateX(100cqw) rotate(90deg); transform-origin: 0 0 }`, site.css 690 to 698) and break the convention of site.css 1838 ("without creating style attributes"). So the CSS rules read a custom property: `transform: var(--gesture, none)` for an unturned map and `transform: var(--gesture, ) translateX(100cqw) rotate(90deg)` for a turned one, both with `transform-origin: 0 0`. The script sets only `--gesture` on the board SVG and the side view. This is a recorded exception to the convention: one custom property, cleared when the gesture ends.
- **During a gesture**, `--gesture` moves and scales the SVG on a compositor layer (`will-change: transform` only while the gesture lasts), from the box the gesture started at; the `viewBox` is not changed. `toBoard` inverts the gesture's transform, since the rectangle taken at the gesture's start no longer matches the screen.
- **The exposed edges.** The composited image holds only what the SVG drew in its own box: a drag exposes a blank strip, and zooming out shows empty margins, until the `viewBox` is set again. So during a long gesture the real `viewBox` is committed every 250 ms, or as soon as the exposed strip passes a tenth of the pane, and the gesture's transform restarts from it. Between commits the blank strip is accepted.
- **When the gesture ends** (pointer up, or 200 ms after the last wheel event, task 3's one idle delay), one frame sets the real `viewBox`, clears `--gesture`, and updates the unit tier and the labels.
- Large zoom changes look soft until the next commit, then sharpen. A click after a gesture lands on the hex shown.

**Tests:** Node: a gesture changes `--gesture`, not the `viewBox`, until a commit or its end; at its end the `viewBox` equals the box the old code would have set; `toBoard` during a gesture inverts the transform; a turned map keeps its turn. Studio: drag and wheel on every map page, turned and unturned, side by side; the exposed strip refills at the commit; a click right after a gesture selects the hex under the pointer.

### Task 7: measure and record

Run the probe (task 1) on the reference in both browsers after each of tasks 3 to 6, and on `dl-tractor-works-02` and bd01 at the end to show the small maps are no worse. Record the results against section 1's thresholds in the analysis (a new results section), with the limits found and any backlog rows. If a threshold is missed, say so and propose B4 by decision P3.

### Task 8: gate and pull request

The gate's `strict` stage before the documents commit, the Node viewport test by hand, the pull request with a green CI run, the outside outputs cleaned. The gate's `baseline`, `build`, and `proofs` stages are not run: they check what games decide, which this plan does not touch (the user's standing rule of 2026-11-01: never run tests the change cannot affect). The merge waits for the user's word.

## 4. Risks

| Risk | Answer |
|---|---|
| Pointer conversion without the screen matrix drifts on a turned, letterboxed, or moved map | Task 3 applies `meet` letterboxing and the turn, retakes the rectangle at pointer entry, pointer down, and each frame, and tests against a real affine screen matrix |
| The repeated grid differs from the path | One lattice for the whole map; interior columns repeated, edge hexes as today's path; task 5 compares every hex |
| Changing the renderer version unverifies every board | The layer request gets its own cache token; `RendererVersion` stays; task 5 reruns the fidelity batch only if it compares the grid |
| A reader of the SVG label layer breaks | The SVG `labels` layer stays for the other views and its listed readers |
| Batching by frame loses zoom steps | `state.box` changes at once; only DOM writes wait for the frame |
| Blank strips and soft drawing during a gesture | The `viewBox` is committed every 250 ms or when the strip passes a tenth of the pane; softness lasts until then |
| The pane's numbers mislead | Task 2 measures in an ordinary browser before the work, task 7 after |

## 5. Out of scope, for the backlog

When the plan is approved, these join the backlog as one new section:

- M3 and C1 to C4: the setup panel's renders and the setup-closing step, after their own measurement.
- B4: raster tiles, if this plan's thresholds are missed (decision P3).
- B5 and B6: terrain level of detail; counters by view.
- D1 and D2: PilotHost's memory after a hand-off, and the Studio's import peak.

## 6. Checklist

Answers to the [guidelines'](<ASL Memory and Performance Guidelines.md>) checklist:

- **Work added per state, unit, hex, or step:** none on the server per step. Per board version: one label list (task 4) and a repeated-column grid in place of the path (task 5), both cached with the layers. In the browser: one frame callback per gesture frame; a pooled label fill bounded by the hexes in view.
- **Ownership, keys, lifetime, bounds:** the label list and the grid are keyed by board, version, view, and layer name (with the `layers=2` token in the browser's URL), in `RenderCache` and the browser's HTTP cache, like today's layers; the browser's label index lives with the loaded board and is dropped on the next load.
- **Disclosure, snapshots, pinned versions, seams:** nothing changes what a viewer sees; seams and reversed boards are tested (task 5).
- **Counters:** the PR 22 fragments, patches, and three tiers are unchanged; the unit tier is computed from the cached size.
- **Checks:** the probe (task 1) on three workloads; the Node tests; the C# rendering tests; the Studio check of every map page; the merge gate.
- **Tests:** the C# tests build each board's geometry once per class; no new test project.
- **Benefit and noise:** section 1's thresholds, measured in an ordinary browser.
- **Records:** results, limits, backlog rows, time log, and outside-output cleanup with each task.

## Related documents

- [ASL Map Studio Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
- [ASL Map Studio SVG Memory Analysis](<ASL Map Studio SVG Memory Analysis.md>)
- [ASL Map Studio Counter Layer Design](<ASL Map Studio Counter Layer Design.md>)
