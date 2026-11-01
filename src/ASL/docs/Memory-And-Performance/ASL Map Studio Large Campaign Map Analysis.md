# ASL Map Studio Large Campaign Map Analysis

Date: 1 November 2026

Status: analysis; no code is changed. The user accepted every proposal on 2026-11-01: the decisions of section 8 (P1 to P4) and the order of section 7. Work follows that order, starting with M1 and M2. The user asked on 2026-11-01 for ways to optimize memory and performance for the squad-level map of a Campaign Situation, which is very sluggish to pan and zoom, and to await instructions after this analysis.

## 1. The question and the short answer

The Goodwood Situation, handed off from the Campaign at squad level, opens in Play as one map of three joined boards (B, A, C at 40 m a hex) carrying 624 ASL counters. Panning and zooming it is very slow. Why, and what would make it fast?

Short answer, from measurements in the running Studio (section 3) and a reading of the code (section 4):

- **The time is spent in the browser, not on the server.** Pan and zoom never call the server: each wheel or drag event changes the `viewBox` of one inline SVG.
- **Each event forces a synchronous layout of the whole map, costing 0.6 to 1.2 seconds.** `setBox` in `boardViewport.js` changes the `viewBox` and then reads `svg.clientWidth` (twice), which makes the browser recompute style and layout for the whole SVG at once. Every plain mouse move over the map also calls `getScreenCTM()`, which forces the same layout, so even hovering stutters.
- **The 40,442 hex labels are about two-thirds of that cost, though they are hidden.** The label layer is one `<text>` per hex, kept in the page with `display: none` at this zoom. With it detached, a pan step falls from about 1,000 ms to 120 to 430 ms. `updateLabelDetail` also rewrites an attribute on that 40,442-node group on every event, even when its value is unchanged.
- **Painting is the smaller part:** 20 to 220 ms a frame, for terrain and grid paths with millions of coordinates (the grid alone is 4.9 million characters). Hiding the terrain, elevation, grid, marks, and counter layers one at a time changed little while the labels stayed.
- **The counter layer, optimized in PR 19 and PR 22, is not the problem:** 389,165 characters for 548 drawn stacks, under 4% of the map.
- **Nothing coalesces events per frame.** A trackpad burst of wheel events pays a full layout for each event.

The remedies that follow from this are cheap first (section 6, group A): stop forcing layout on every event, coalesce updates to one per animation frame, stop rewriting unchanged attributes, and take the hidden labels out of the page. Then the larger ones (group B): pan by a compositor transform during a gesture, and draw the static layers (terrain, elevation, grid) as raster tiles rather than one vast vector image.

## 2. Workload and method

- **Game:** `campaign-21f5abd45868a5100c5b8475`, the Goodwood complete ASL setup handed off from the Campaign Atlas on 2026-11-01 (114 ground formations, 624 ASL counters, 8 aircraft off-map), at Turn 1, Allied Rally Phase, setup open, viewed as Allied.
- **Builds:** my Studio on port 6670 built from `main` at 44e90e43 by `code-builders/build-and-test/build_studio.sh` into `asl-gate-work/studio-6670`; PilotHost on 8093 built from the same commit into `asl-gate-work/campaign-host-8093` (later commits on the branch are documents only).
- **Browser:** the Claude desktop app's browser pane (Chromium), at a 2,380 by 1,006 pixel viewport, with the map at Fit zoom. The pane runs with automation attached, which may make DOM changes and layout dearer than in an ordinary browser. Every figure here should be measured again in an ordinary Chrome or Edge window before a fix is judged (option M1).
- **Pan probe:** a script that shifts the `viewBox` 40 board units left or right, then waits two animation frames, four times per case, with chosen layers hidden by `display: none` and restored afterwards. The first shift of each case was usually an outlier (1.3 to 7.4 seconds) and is left out of the ranges below. A second probe split one shift into the attribute write, a forced layout (`svg.clientWidth`), and the frames that follow.
- **Process memory:** Windows working set and private bytes of the two `dotnet` processes and of the browser's processes.

## 3. Measurements

### 3.1 The page

| Item | Value |
|---|---|
| DOM elements in the page | 51,437 |
| Elements in the board SVG | 45,079 |
| Characters in the board SVG | 12,184,195 |
| Characters in the whole page | 12,707,541 |
| `<text>` elements in the board SVG | 40,568, of which 40,442 are hex labels |
| Drawn counter stacks (`g[aria-label]` in the units layer) | 548 |
| `<use>` elements (counter faces, three tiers each) | 1,746 |

### 3.2 The board SVG by layer

| Layer | Characters | Elements | Notes |
|---|---|---|---|
| `layer-grid` | 4,944,347 | 2 | Two paths: every hex outline and every center dot |
| `layer-exact-terrain` | 3,419,069 | 15 | One path per terrain and elevation group; the largest path's `d` is 1,210,369 characters |
| `layer-labels` | 1,703,067 | 40,442 | One `<text>` per hex; hidden (`display: none`) at this zoom |
| `layer-exact-elevation` | 989,432 | 4 | The elevation rings |
| `layer-marks` | 707,155 | 2 | The setup overlays (setup areas, non-OB "?" hexes), sent as one string |
| `layer-units` | 389,165 | 4,548 | The counters, in fragments by hex, with shared faces |
| `unit-figures` | 30,114 | 36 | The figure sprite |

### 3.3 Pan steps

Times are one `viewBox` shift plus two animation frames, in milliseconds, second to fourth shift of each case.

| Case | Times |
|---|---|
| Two idle frames, no change (control) | 23 to 24 |
| Every layer shown | 951 to 1,203 |
| Grid hidden | 965 to 1,286 |
| Terrain hidden | 849 to 1,073 |
| Elevation hidden | 909 to 1,490 |
| Marks hidden | 851 to 1,153 |
| Counters hidden | 695 to 873 |
| Terrain, elevation, grid, marks hidden | 1,243 to 1,443 |
| All five hidden | 931 to 1,440 |
| **Labels detached from the page** | **121 to 429** |
| Labels detached, terrain, elevation, grid, marks hidden | 106 to 284 |
| Labels detached, counters hidden | 189 to 298 |

One shift split into its parts (every layer shown):

| Part | Milliseconds |
|---|---|
| Writing the `viewBox` attribute | 0 |
| The forced layout that follows (reading `svg.clientWidth`) | 628 to 1,185 |
| From then to the next frame (paint) | 19 to 221 |
| The second frame | 6 (once 4,136, the first shift) |

Detaching the label group itself froze the page for more than 45 seconds, and putting it back took about a minute. Any operation that changes the label group as a whole (for example the label layer switch, or zooming across the threshold that shows the labels) pays a cost of that order today.

### 3.4 Memory

| Process | Working set | Private | Note |
|---|---|---|---|
| Studio (6670) | 905 MB | 928 MB | Peak working set 4,069 MB, during the hand-off's import |
| PilotHost (8093) | 2,175 MB | 2,174 MB | Holds the Situation's generated terrain, packages, and sessions after the hand-off |
| Largest browser process | 2,000 MB | 2,231 MB | Most likely the pane's renderer holding the map; the attribution is not proven |

## 4. Where the cost is in the code

From a reading of the code on 2026-11-01 (paths under `src/ASL/LimboDancer.Domains.Asl.MapStudio`, MS):

### 4.1 Pan and zoom (`MS/wwwroot/js/boardViewport.js`)

- **Wheel** (lines 62 to 69): every event calls `zoomAt` with a fixed factor of 0.8 or 1.25, whatever the size of `deltaY`; trackpads send many events per gesture.
- **Drag** (87 to 117): every `pointermove` calls `toBoard`, which calls `getScreenCTM()` (741 to 747), and reads `svg.clientWidth`; then `setBox`.
- **Hover** (90 to 95): a `pointermove` without a drag also calls `toBoard`, so every mouse move over the map forces a layout, and calls .NET `OnBoardHover` at most every 100 ms.
- **`setBox`** (730 to 739), on every event: writes the `viewBox` on the board SVG and on the side view; `rememberView` writes the view to `localStorage` with `JSON.stringify` (13 to 18); `updateUnitTier` and `updateLabelDetail` each read `svg.clientWidth`, which forces the layout measured in section 3.3. `updateLabelDetail` writes `data-detail-visible` on the label group every time, even when unchanged (`updateUnitTier` checks first).
- **No coalescing:** no `requestAnimationFrame`, no throttle, no CSS transform or `will-change` during a gesture. The SVG is not on a compositor layer of its own, so each change repaints the whole map.

### 4.2 The board (`LimboDancer.Domains.Asl.Maps.Rendering/BoardRenderer.cs`, `MS/Services/RenderEndpoints.cs`)

- Play uses the Exact view (`BoardWorkspace.razor:85`). Its layers are fetched once per board version as immutable, gzip-cached SVG files and parsed into the one SVG.
- A composed map of several boards becomes one terrain raster traced into paths (`MS/Services/Maps.cs:196-209`), so the paths' size grows with the whole map's area and detail.
- The grid layer is two paths over every hex; the label layer is one `<text>` per hex, hidden below 6 screen pixels by CSS (`site.css:4688`).

### 4.3 The counters (`Units.Rendering/UnitOverlay.cs`, `MS/Services/UnitLayerSync.cs`)

- One fragment per occupied hex, faces shared through `<use>`, three tiers kept (ruling F3), at most six counters drawn per level. Patches carry only changed fragments. This is the design of PR 22 and it holds at this size.
- `UnitLayerSync.Diff` has an O(n²) search for new fragments (100 to 119); harmless at 548 stacks, worth noting.

### 4.4 Server and Blazor, per render (not measured here)

The pan and zoom slowness is all in the browser, but a click (a checkbox, a counter) re-renders the whole `Play.razor` page; no Play or board component overrides `ShouldRender`. In setup, with 624 counters, each render also:

- rebuilds the setup panel's `CounterModel` (`Play.razor:3021-3120`), which is O(placements²), with a `<select>` per support weapon or Gun listing every eligible unit of its side (`SetupCounters.razor:316-328`), possibly tens of thousands of `<option>` elements for Blazor to diff;
- builds the non-OB "?" fieldset, one checkbox per eligible hex with string keys built three times per row (`Play.razor:111-149`), and the marks string again (4012 to 4030), which `SendLayers` compares and resends whole when it changed;
- recomputes a key over every draft row for the map's units (2475 to 2536).

The step that closes setup was measured earlier at 10 to 13 seconds with only about 0.35 s on the server (Play Step Analysis, section 2.2); at this size it will be worse. Both are open (backlog sections 62 and 65).

### 4.5 Memory

The Studio's 4 GB peak during the import and PilotHost's 2.2 GB held afterwards are a separate subject: the hand-off's terrain generation and packages (84 tiles, three boards) and what is kept after it. Neither affects pan and zoom.

## 5. What the measurements rule out

- **Not the counters.** The PR 22 design scales: 389 KB and 4,548 elements for 548 stacks, and hiding them saves little.
- **Not the server on pan or zoom.** No call is made.
- **Not memory leaking in the browser per pan.** The cost is per event, flat, and returns to idle frame times.
- **Not mostly painting.** Painting is 20 to 220 ms; the forced layout is 600 to 1,200 ms.

## 6. Options

Each option states its expected gain against the baseline of section 3, how it is measured, and when to stop (the user's rule of 2026-10-19 for performance plans). All are proposals.

### Group A: the event path (small, in `boardViewport.js`)

| Id | Change | Expected | Measure | Stop when |
|---|---|---|---|---|
| A1 | Coalesce: an event only records the wanted box; one `requestAnimationFrame` callback applies the last box per frame | At most one layout per frame instead of one per event: a burst of wheel events costs one step, not several | Frames per second and step time during a scripted 1-second wheel burst | A burst costs no more than a single step |
| A2 | No forced layout in the path: keep the SVG's width from a `ResizeObserver`, and convert pointer positions from that width and the box instead of `getScreenCTM()` on every move (keep `getScreenCTM` for rotation, computed once per gesture) | Removes the 0.6 to 1.2 s synchronous layout from each event; the browser lays out once, at the frame | The split probe of section 3.3: the forced-layout part reads 0 | The forced-layout column is gone |
| A3 | Write only on change: `updateLabelDetail` compares before writing, as `updateUnitTier` does | Stops invalidating the 40,442-node label group's style on every event | Pan step with labels attached | The labels-attached step equals A1 and A2 alone |
| A4 | Save the view to `localStorage` on gesture end or after 250 ms idle, not per event | Removes a synchronous storage write and a JSON string per event (small, but on the hot path) | Event handler time | Not measurable alone; folded into A1's measurement |

Expected together: a pan step from about 1,000 ms to the paint cost, about 120 to 430 ms, and fewer steps per gesture. The `boardViewport.pointer.test.mjs` test (run in CI and by hand at every gate) covers the pointer mathematics and must stay green.

### Group B: what is drawn

| Id | Change | Expected | Measure | Stop when |
|---|---|---|---|---|
| B1 | Labels on demand: no label elements in the page; at readable zoom, draw labels only for the hexes in view (and a margin), from a compact list of hex names and centers sent once | Removes 40,442 elements and 1.7 MB from the page: about two-thirds of today's step (section 3.3) and the minute-long freeze on toggling labels | Pan step and label toggle at Fit and at label zoom | Toggling labels takes under 100 ms; the step equals the labels-detached figure |
| B2 | Grid as a pattern: draw the hex grid with an SVG `<pattern>` per board (hexes are regular within a board), instead of 4.9 million characters of path | Removes the largest layer and its paint; page size falls by about 40% | Paint part of the split probe; page characters | Grid paint under 10 ms; seams and reversed boards still drawn right |
| B3 | Pan by transform during a gesture: while dragging or zooming, move the SVG with a CSS `transform` on its own compositor layer (`will-change: transform`) and set the real `viewBox` once at the end | Frames during a gesture cost compositing only, typically under 16 ms; one full layout and paint at the end | Frames per second during a scripted drag | 50 frames per second or more during a drag |
| B4 | Raster tiles for static layers: draw terrain, elevation, and grid as image tiles per zoom level (server-rendered and cached like the layer files, or drawn by the browser into canvas bitmaps once per zoom level), keeping counters, marks, and highlights as SVG above them | Paint becomes proportional to the screen, not the board; the static layers leave the DOM | Paint part at Fit and at close zoom; memory of the browser process | Paint under 16 ms at any zoom; terrain colors and edges match the vector rendering |
| B5 | Level of detail for terrain: a simplified set of terrain paths for far zoom, the exact paths for near zoom | Cheaper paint at Fit without tiles | Paint part at Fit | Only if B4 is not chosen |
| B6 | Counters by view: keep only the counter fragments near the view in the page | Small at 548 stacks; matters only for larger games | Elements in the page | Only after A and B1 to B4, if counters then dominate |

B3 alone gives smooth gestures quickly; B4 is the lasting answer for maps of this size and the larger work. B1 and B2 are independent of both and pay at once.

### Group C: the server and Blazor

| Id | Change | Expected | Measure |
|---|---|---|---|
| C1 | Stop re-rendering the whole Play page for map-local events: split the map, the setup panel, and the side panels into components with `ShouldRender`, so a hex click renders only what changed | Click latency in setup from the page's whole render to the panel's | Server render time and circuit payload per click (the Play Step Analysis's follower method) |
| C2 | Cache `CounterModel` per revision and draft; replace the per-row `<select>` of every eligible unit with a search picker that loads its options on opening | Removes O(placements²) work and tens of thousands of `<option>` elements per render in setup | As C1 |
| C3 | The non-OB "?" fieldset: build its row keys once per revision; group by setup area, or show it with `<Virtualize>` | Fewer elements and strings per render | As C1 |
| C4 | Memoize the marks string per revision and draft, so `SendLayers` compares references, not 700 KB strings | Removes a whole-string build and compare per render | Allocation per render |

### Group D: memory

| Id | Change | Expected |
|---|---|---|
| D1 | PilotHost: release the hand-off's terrain-generation intermediates once the packages are written, keeping only what a later hand-off or a reopened Situation reads | Most of the 2.2 GB held after the hand-off |
| D2 | Studio: measure the import's 4 GB peak (which stage holds what) before proposing a change | A finding, not yet a gain |

### Group M: measurement first

| Id | Change |
|---|---|
| M1 | Measure section 3 again in an ordinary Chrome or Edge window, without automation, with the Long Animation Frames API (as the SVG Memory Analysis did in its sections 12 and 13), to confirm the split before any fix |
| M2 | Add a pan and zoom benchmark to `?mapMetrics=1`: a scripted drag and wheel burst, reporting frames per second, the longest frame, and the forced-layout time, so each option's gain is read the same way |
| M3 | Measure a setup click and the setup-closing step on this game with the Play Step Analysis's method, for group C's baseline |

## 7. Proposed order

1. **M1 and M2:** confirm the baseline in an ordinary browser and make the probe a tool.
2. **A1 to A4:** one small change to `boardViewport.js`; the largest gain for the least work.
3. **B1 and B2:** take the hidden labels and the grid path out of the page.
4. **B3:** smooth gestures by transform.
5. **M3, then C1 to C4:** the setup panel and page renders.
6. **B4:** raster tiles, if A to B3 do not reach the stop thresholds at this map size.
7. **D1, D2:** the Campaign host's and the import's memory.

Each step ends with the measurement of its row and the Studio check of the changed pages (the user's rule: the Studio measurement comes first in every performance task).

## 8. Decisions

Approved: the user accepted every proposal of this section on 2026-11-01. Each holds as its proposal is written.

| Id | Question | Proposal |
|---|---|---|
| P1 | Scope: pan and zoom first, then the setup panel, then memory | Yes, in the order of section 7 |
| P2 | May labels leave the page, drawn only for the hexes in view at readable zoom (B1) | Yes; the labels look the same, and the layer switch keeps its meaning |
| P3 | Raster tiles for the static layers (B4), which changes how the map is drawn | Decide after A to B3 are measured; if needed, server-rendered tiles cached like today's layer files |
| P4 | A reference workload for large maps | Keep this Goodwood hand-off as the reference for map size, beside `dl-tractor-works-02` for play steps, and save its game record outside the repository with the other proof games |

## 9. Checklist

Answers to the [guidelines'](<ASL Memory and Performance Guidelines.md>) checklist, for the options as proposed:

- **Work added per state, unit, hex, or step:** none on the server for group A and B3; B1 adds a compact hex list sent once per board version; B4 adds a tile render per board version and zoom level, cached like the layer files.
- **Ownership, keys, lifetime:** the viewport state stays in `boardViewport.js`, per map; tiles and the label list are keyed by board, version, and view, like the layer cache.
- **Disclosure, snapshots, seams:** unchanged; nothing here changes what a viewer is shown. Seams and reversed boards must draw the same under B2 and B4.
- **Counter fragments:** unchanged; PR 22's incremental rendering is kept.
- **Checks:** the pan probe as a tool (M2); `boardViewport.pointer.test.mjs`; the Studio check at the widths the pass's scope names.
- **Tests:** any new test reads its inputs once; no new test project is proposed.
- **Benefit and noise:** each option has a stop threshold in section 6; figures from the automated pane are confirmed in an ordinary browser first (M1).
- **Records:** results, limits, and backlog rows go with each task.

## Related documents

- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
- [ASL Map Studio SVG Memory Analysis](<ASL Map Studio SVG Memory Analysis.md>) (sections 12 and 13, browser measurements)
- [ASL Map Studio Counter Layer Design](<ASL Map Studio Counter Layer Design.md>) (the tiers, ruling F3)
- [ASL Map Studio Play Step Analysis](<ASL Map Studio Play Step Analysis.md>) (the setup-closing step)
- [Goodwood Transition Release Report](<../../../Campaign/docs/Goodwood Transition Release Report.md>)
