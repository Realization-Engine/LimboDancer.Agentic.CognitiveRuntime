# ASL Map Studio Map Lab Plan

**Written:** 2026-11-02, at the user's word: "Draft the plan", after the user asked to use the empty Map Lab page (`/lab/maps`) to construct maps for experiments, keeping the Formation boundaries outline of the Goodwood ASL map. Revised the same day at the user's word: the Map Lab is a place to experiment with large hexagonal maps; it has no target outside itself, and no finding is applied to an application map until there is reason to.
**Source:** the [Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>) (sections 10 to 13, the techniques already proved on the Studio's map) and the [Independent Map Creation Analysis](<../ASL Independent Map Creation Analysis.md>) (section 7, where the two Labs fit).
**Scope:** the Map Lab as a test bench for large hexagonal maps: it builds maps of any size, draws them by interchangeable techniques with a Formation boundaries outline, and measures pan and zoom. Not in scope: any change to Play, the Campaign Atlas, or another application map; map painting, hex designs, the Hex Lab; saving maps; any rule, record, or replay.
**Status:** approved by the user on 2026-11-02 ("Approve as proposed"), with decisions L1 to L3 as section 6 proposes them. Built on branch `claude/map-lab`.

## 1. Goal

Learn what it costs to pan and zoom a hexagonal map as it grows, and which drawing technique holds up at which size, on maps the Lab builds itself. The findings are recorded in this plan's results section. They change no application map; applying one is a later decision of the user's, with its own plan.

## 2. Expected findings

Stated up front (the user's rule of 2026-10-19). Every figure is measured in the user's Chrome at about 2,033 by 803, as in the analysis, three runs each with the first dropped as warm-up. The Lab's own simplest technique (`elements`, section 3.3) is the baseline, measured in task 1.

| Measure | Baseline | Expected, best technique | Stop threshold |
|---|---|---|---|
| Drag: frames per second, at Fit and closest zoom, 10,000 hexes | Task 1 | 60 or more at both | Below 30 at either after task 3: stop and report |
| Drag: longest frame, 10,000 hexes | Task 1 | under 50 ms | Above 150 ms: stop and report |
| A burst of 20 wheel events, to settled | Task 1 | under 150 ms | Above 400 ms |
| Elements in the map | Task 1 | under 2,000 at any size | |
| The same at 100,000 and 1,000,000 hexes | none | reported, with the size where each technique falls below 30 frames per second | |

The basis: the Studio's ASL map of 40,442 hexes drags at 78 to 85 frames per second in the user's Chrome with raster tiles and pan by transform (analysis, section 13). The Lab tests whether that holds at ten and a hundred times the size, and where a canvas or another method is needed. The 1,000,000 case answers the Independent Map Creation Analysis's question 3 (size target) with figures.

## 3. How it works

### 3.1 The page

The Map Lab page (`Components/Pages/MapLab.razor`) gains a short form above the map:

- **Size:** columns and rows, or a hex count (1,038, 10,000, 100,000, 1,000,000, or a typed number), and a seed.
- **Grouping:** single hexes, or formations of hexes (about 36 hexes each, as on the Goodwood ASL map), so the Formation boundaries outline has groups to outline.
- **Technique:** the choices of section 3.3.
- **Layers:** terrain, grid, Formation boundaries, labels, each on or off.
- **Probe:** a Measure button, and the figures of the last runs beside it.

Nothing is saved. A map is built in memory from the form, the address carries the form (`/lab/maps?n=100000&group=36&tech=tiles&seed=7`), and a reload repeats a run.

### 3.2 Generated maps

A rectangle of hexes on the Studio's hex geometry, with terrain from seeded noise (open, woods, town, water, and two levels), so every run with the same seed is the same map. Formations are made by grouping hexes into compact parents of about the chosen size, with irregular edges so the outline looks like the Goodwood one rather than a regular grid of large hexes. The generator lives under `Services/MapLab/` in the Studio, not in Maps or Rules: no rule reads it.

### 3.3 Techniques

Each technique draws the same map record. The Lab switches between them without a reload.

| Key | Technique | What it tests |
|---|---|---|
| `elements` | An SVG element per hex (fill and outline), zoom by `viewBox`, no frame coalescing | The plain approach, as the baseline |
| `paths` | One path per terrain kind and one for the grid; the Studio's viewport (`boardViewport.js`) for pan and zoom | What the element count alone costs |
| `tiles` | Terrain painted into raster tiles (the B4 tile cache), grid and boundaries as vectors, the Studio's viewport with pan by transform (B3) | The Studio's current answer at larger sizes |
| `canvas` | Everything on one canvas, drawn from the map record for the tiles in view, pan by transform during a gesture | Whether a canvas beats tiles at the largest sizes |

Further techniques (the grid in tiles, a simplified far-zoom drawing, labels by view) are added as rows when a measurement points to them.

### 3.4 The Formation boundaries outline

`CampaignMapBoundaries.Draw` (`Services/CampaignMapBoundaries.cs`) already builds the outline as one path: every transition between parents and the owned perimeter, each shared edge once, with a non-scaling stroke. The Lab calls it with the generated grouping. It stays a vector layer in every technique, and the probe measures it on and off. Its build time at 1,000,000 hexes is measured too, since it walks every hex.

### 3.5 The probe

The Studio's `mapProbe()` drives a drag and a wheel burst and reports frames, the longest frame, and the long-animation-frame breakdown. The Lab exposes it behind the Measure button, with an equivalent driver for `elements` and `canvas`, which do not use the Studio's viewport. It also counts the map's elements and characters, times the map's build on the server, and records the technique, size, grouping, zoom, and window size with each run.

Speed is judged in the user's Chrome; the Claude browser pane has a floor of about 11 frames per second even on an empty map (Raster Tiles Plan, deviation D5). The pane only checks that the page works.

## 4. Tasks

| Task | Work | Done when |
|---|---|---|
| 1 | The page, the generator, `elements`, and the probe | Baseline measured at 1,038 and 10,000 |
| 2 | `paths` and the Formation boundaries outline with grouping | Measured at 1,038, 10,000, and 100,000 |
| 3 | `tiles` | Measured at every size; the stop thresholds of section 2 checked |
| 4 | `canvas`, only if `tiles` falls below 60 frames per second at 100,000 | Measured as task 3 |
| 5 | Results in a section of this plan, the backlog rows, the time log | The findings are recorded; nothing is applied elsewhere |

Tests: unit tests for the generator (count, seed repeatability, grouping covers every hex once) and for any change to `CampaignMapBoundaries`, by method name; a component test for the form. No merge-gate stage applies: the Lab touches no rule, record, or replay (standing rule of 2026-11-01). One branch, one pull request, CI green, the user's word to merge.

## 5. Risks

- **Server memory at 1,000,000 hexes.** A map record of small structs is tens of megabytes; tiles are bounded by the existing 64 MB cache. The probe records the Studio's working set at each size, and the Lab refuses a size whose build passes a set limit (decision L2).
- **Generated terrain is not real terrain.** Noise gives fewer long features (roads, rivers) than a real map, and long features are what make paths heavy. Task 2 adds roads and streams to the generator if the paths look too light against the Goodwood map's.

## 6. Decisions for the user

- **L1. Maps are not saved.** Proposed: the Lab holds maps in memory, from address values, until a plan for map painting (Independent Map Creation Analysis, section 7) decides a store.
- **L2. Largest size.** Proposed: 1,000,000 hexes, refused above it, so a mistyped size cannot exhaust the Studio.
- **L3. Generated terrain.** Proposed: seeded noise with five terrain kinds and two levels, plus roads and streams if task 2 needs them.

## 7. As built (2026-11-02)

Tasks 1 to 3 are built on `claude/map-lab` as one change; task 4 waits on task 3's figures, and task 5's findings wait on the user's Chrome.

- **The bench reuses the Studio's viewport whole.** A Lab map is served at `lab-render/{key}/exact/{file}`, in the shape of a board's Exact view, so `BoardViewport` and `boardViewport.js` load, tile, label, pan, and probe it unchanged. The key (`n10000-g36-s7-tiles`) is the whole map (L1), so every file is immutable.
- **Deviation D1: `elements` is the Studio's viewport with pan by transform off** (`?transform=0`, the switch B3 already has), not a viewport of its own; it keeps frame coalescing. Section 3.5's second probe driver was therefore not needed.
- **Deviation D2: each map loads the page again.** Switching technique in place left the old map in the new viewport; a fresh page per map is also the cleaner measure. The page's measures are kept by the Studio (`LabMapCache.Runs`, the last 60), so they survive the reload.
- **Deviation D3: tiles are painted from the hex table**, not from a grid: each pixel takes the colour of the hex nearest it, the mean of four samples above level 0. `TileCache` gained an overload for any named source, so the Lab's tiles share the one bound with the boards' and one Lab map is held at a time.
- **Deviation D4: `CampaignMapBoundaries` gained `AppendEdges`**, which takes the parents as an array by hex ordinal. `Draw` calls it, with the same output for Play; the Lab draws both the grid (every hex its own parent) and the Formation boundaries with it.
- **No component test of the form** (backlog section 97); the page was checked in my Studio at 6670: every technique, 1,038 to 1,000,000 hexes, the measure, and its log across a reload.

**Server figures at 1,000,000 hexes** (999,929: 1,354 columns by 738 rows, 27,889 formations, tiles), from the page's server table: the map 1.9 s; the grid 89.5 MB in 6.6 s; the Formation boundaries 20.5 MB in 3.6 s; the labels 24.5 MB in 1.6 s. My Studio's working set afterwards: 1.1 GB. At this size the vector grid, not the terrain, is what the browser must take whole.

## 8. Findings (2026-11-02)

Measured by the user in Chrome with the Measure button, at 1,323 by 735, formations of 36, seed 7, every layer shown; one run each. Fit, then five steps in. A frame of 12 ms is the display's own rate (about 85 frames per second).

| Map | Technique | Elements | Drag fps, Fit / in | Longest frame, Fit / in | 20 wheel events, Fit / in |
|---|---|---|---|---|---|
| 1,034 | elements | 2,074 | 85.7 / 85.1 | 12 / 12 ms | 19 / 16 ms |
| 1,034 | paths | 14 | 86 / 85.1 | 12 / 12 ms | 15 / 13 ms |
| 1,034 | tiles | 23 | 85.3 / 85.3 | 12 / 12 ms | 22 / 15 ms |
| 9,922 | elements | 19,850 | 29.7 / 58.6 | 71 / 36 ms | 59 / 23 ms |
| 9,922 | paths | 17 | 85.4 / 85 | 12 / 12 ms | 19 / 20 ms |
| 99,938 | paths | 18 | 33.6 / 7.4 | 365 / 459 ms | 50 / 282 ms |
| 99,938 | tiles | 14 | 56.8 / 15.4 | 200 / 365 ms | 13 / 69 ms |
| 999,929 | tiles | 23 | 85.7 / 85.5 | 12 / 12 ms | 23 / 18 ms (void: F3) |

`tiles` at 10,000 hexes was not measured; `paths` at 10,000 already runs at the display's rate.

- **F1. Up to about 1,000 hexes, any technique is smooth,** one element per hex included. Goodwood's formation count is not by itself a cost.
- **F2. One element per hex stops being smooth by 10,000 hexes** (30 frames per second at Fit). One path per colour holds the display's rate there.
- **F3. Chrome does not paint a path of tens of millions of characters, and says nothing.** At 1,000,000 hexes the grid (93.8 million characters of path data) and the boundaries (21.5 million) are in the page but nothing of them is drawn, even zoomed in; at 100,000 the grid (8.9 million) is drawn. The 1,000,000 run therefore measured the terrain tiles alone and is void (section 0, point 3 of the Standing Rules). The limit lies between 9 and 94 million characters.
- **F4. At 100,000 hexes the grid and the boundaries are the whole cost.** Both techniques miss the stop threshold zoomed in (section 2: below 30 frames per second), so the plan stops here and reports. A check in my browser pane on `tiles` at 100,000, five steps in, two runs each (a pointer only, section 3.5): every layer, 25.7 and 41.6 frames per second, longest frame 306 and 388 ms, long animation frames up to 413 ms; the grid and boundaries hidden, 67.7 and 62.9, longest frame 24 ms, no long animation frame.

**What follows** (proposed, for the user's word): a fourth technique, `tiles+lines`, that paints the grid and the Formation boundaries into the tiles with the terrain, so no vector path grows with the map. It removes F3 and F4 together and makes 1,000,000 hexes measurable. `canvas` (task 4) stays in the backlog: F4 points at the vector lines, not at tiles.

### 8.1 Tiles and lines (built 2026-11-02, at the user's word "Build tiles+lines")

`tileslines` paints the terrain, the grid, and the Formation boundaries into one set of tiles. A tile pixel whose hex differs from the hex of the pixel to its right or below is a line pixel: boundary colour where the two formations differ, grid colour otherwise, so lines are one tile pixel wide at every level. The grid is left out of a level where a hex is under 6 pixels across, and the boundaries where a formation is (`GridMinimumPixels`), so far levels are not a mesh. The map has two layers, the tiles and the labels; nothing sent to the browser grows with the map but the labels' data.

- Checked in my Studio at 6670: at 100,000 and 1,000,000 hexes both kinds of line are drawn, thin, once the level's tiles arrive; F3 does not occur.
- **F5. A tile with lines takes about 0.75 s to paint** at 1,000,000 hexes (the Studio's log, level 1), against about 0.4 s for a board's tile (backlog section 96). A drag moves painted tiles, so it should not feel this; a zoom shows the coarser level, scaled, until the finer tiles arrive.
- The Lab's tiles are now held under the whole key, so `tiles` and `tileslines` of one map never share a tile.
- A check in my pane was void: frames came once every two seconds with no long animation frame, a throttled window, not a slow page.

Measured by the user in Chrome, as in section 8 (1,323 by 735, formations of 36, seed 7, one run each):

| Map | Elements, Fit / in | Drag fps, Fit / in | Longest frame, Fit / in | 20 wheel events, Fit / in | `tiles` on the same map, fps Fit / in |
|---|---|---|---|---|---|
| 9,922 | 16 / 142 | 85.3 / 82.7 | 12 / 47 ms | 17 / 15 ms | not measured |
| 99,938 | 10 / 106 | 85.9 / 85 | 12 / 12 ms | 13 / 23 ms | 56.8 / 15.4 |
| 999,929 | 19 / 79 | 85.7 / 85.5 | 12 / 12 ms | 23 / 14 ms | void (F3) |

- **F6. With the lines in the tiles, a drag runs at the display's rate at every size measured, up to 1,000,000 hexes.** Every expectation of section 2 is met: 60 frames per second or more at Fit and zoomed in, the longest frame under 50 ms, the wheel burst under 150 ms, and under 2,000 elements. At 100,000 hexes a zoomed drag went from 15 to 85 frames per second and its longest frame from 365 to 12 ms.
- **F7. The map's size no longer reaches the browser.** The elements in the map follow the tiles in view (10 to 142), not the hexes. What still grows with the map is the server's work: the labels' data (24.5 MB at 1,000,000 hexes) and the time to paint a tile (F5).
- `canvas` (task 4) is not needed: its condition (`tiles` under 60 frames per second at 100,000) is answered by `tileslines`, and it stays in the backlog.

These findings stay in the Lab (decision of 2026-11-02): no other map is changed by them.

### 8.2 Dragging with labels (2026-11-03)

The user found a 10,000-hex Lab map zoomed well with labels on, but dragged with many pauses. Two causes, both met on Goodwood first (the Screen-Bounded Map Analysis, sections 3.5 and 5.7), and the Lab had neither answer:

- **Labels as texts.** The Lab's viewport drew one SVG text per label; the label canvas of PR 110 was only on with `?present=bounded`. `BoardViewport` gains `CanvasLabels`, which the Lab turns on, so its labels are one canvas painted off the page. At the readable zoom the Lab's map holds 60 elements.
- **Tiles painted on request.** A drag over new ground waited 0.1 to 0.4 s for each level 0 tile. When the Lab page builds a tiled map it now paints the map's tiles in the background, coarsest first, on half the cores, up to 320 tiles (`LabMapEndpoints.PaintAhead`): all 207 of a 10,000-hex map. Checked: level 0 tiles the view never touched came back in 3 to 14 ms, against 87 to 436 ms before.
- The Measure button adds a run that drags a whole pane at the zoom where the labels become readable (`mapProbe({ zoom: "labels", dragPanes: 1 })`).

Measured by the user in Chrome (2026-11-03) on the 10,000-hex `tileslines` map, at 1,290 by 534, on a display of 60 frames per second (a 17 ms frame is its rate):

| Run | Elements | Drag fps | Median, longest frame | Settle after drag | 20 wheel events |
|---|---|---|---|---|---|
| Fit | 44 | 60.1 | 17, 17 ms | 32 ms | 28 ms |
| Five steps in | 139 | 50.8 | 17, 183 ms | 33 ms | 29 ms |
| Labels readable, a pane-long drag | 97 | 59.6 | 17, 33 ms | 33 ms | 6 ms |

The user's own drags: "good now. No pauses." The one long frame, five steps in, is open.

### 8.3 Counters (built 2026-11-03)

The user asked on 2026-11-03 to test counters on the Lab's map (the [Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>), section 5.2, found that a Lab map has none, so the Lab cannot say what Goodwood's counters cost). The page gains a Units row per side, German and Russian, whose buttons add 10, 50, 100, 200, 300, 400, 500, or 1,000 units, and a Clear button.

- **Nothing new draws them** (the user: reuse the existing parts). The units are the shipped catalog's own German and Russian squads, half-squads, leaders, and machine guns (`UnitLibrary.CatalogChoices`). The layer is built by `UnitOverlayBuilder` with the Studio's renderer and shared fragment cache, and shown by the Lab's `BoardViewport.ShowUnits`, which sends a patch of the hexes that changed. `LabUnits` only places them.
- **Placement:** German units on the west half of the map, Russian on the east, never on water, at most 10,000 a side. Each unit's hex comes from its own hashed seed, so the same map and clicks give the same hexes. The first build seeded `System.Random` with neighbouring numbers and drew its hexes in bands; a test now checks the spread.
- **The runs table gains a Counters column**, the counters in the page when the run was made.
- Checked in my Studio at 6670 on the 10,000-hex `tileslines` map: 1,000 counters in 952 hexes, the layer built in 732 ms and shown in 253 ms; counters drawn as in Play, stacks included. The page then held about 10,200 elements (60 to 139 without counters). A Measure in my pane dragged at 4 to 8 frames per second, a pointer only (section 3.5); speed waits on the user's Chrome.
- Left out (backlog section 97): the units are not in the address, so a reload or a new map drops them; the Lab draws every counter, since it runs without `?present=bounded`; a click on a counter does nothing.

## 9. Answers to the guidelines' checklist

- **Work per hex or step:** none in play; the Lab builds a map record once per form change and paints tiles on request.
- **Ownership and lifetime:** the map record belongs to the page's circuit and is dropped with it; tiles use the existing `TileCache` keyed by the form values as the map's version.
- **Pinned versions and seams:** none; no game reads a Lab map.
- **Counters:** none drawn.
- **Measurements:** section 2, in the user's Chrome.
- **Tests:** built once per class; no new test project.
- **Benefit against noise:** each technique is compared with `elements` on the same machine, window, and map.
- **Outside outputs:** my Studio's build in `asl-gate-work/studio-6670`, registered and cleaned by the gate's ci stage as usual.

## Related documents

- [ASL Map Studio Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>)
- [ASL Map Studio Pan by Transform Plan](<ASL Map Studio Pan by Transform Plan.md>)
- [ASL Independent Map Creation Analysis](<../ASL Independent Map Creation Analysis.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
