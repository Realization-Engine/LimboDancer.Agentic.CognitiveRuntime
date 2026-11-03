# ASL Map Studio Screen-Bounded Map Analysis

**Date:** 2026-11-02
**Status:** approved by the user on 2026-11-02 ("Approve as proposed"), with decisions N1 to N6 as section 7 proposes them. Steps 1 to 5 built on branch `claude/screen-bounded-map`, all behind `?present=bounded` (N6); the default presentation is unchanged. Merged at the user's word of 2026-11-03 ("commit and merge what improvement we have") with the results of section 5.6; the switch to the default waits. It answers the user's request of 2026-11-02: apply what the Map Lab learned to a new way of presenting the Goodwood ASL map, rather than explain again why the present one is slow.
**Sources:** the [Map Lab Plan](<ASL Map Studio Map Lab Plan.md>), sections 8 and 8.1 (findings F1 to F7); the [Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>) and its plans (the [first wave](<ASL Map Studio Large Campaign Map Plan.md>), [Raster Tiles](<ASL Map Studio Raster Tiles Plan.md>), [Pan by Transform](<ASL Map Studio Pan by Transform Plan.md>)), for what is already built.

## 1. The principle

The Lab's maps panned at the display's rate from 1,000 to 1,000,000 hexes once two things held (F6, F7):

1. **Nothing in the page grows with the map.** Every map-wide drawing, lines included, was an image tile; the page held 10 to 142 elements at any size.
2. **A gesture moves pixels already painted** (pan by transform, B3), and a repaint draws only the tiles and the few things in view.

Where either failed, the cost followed the map: whole-map vector lines took 300 to 400 ms a frame at 100,000 hexes (F4), and at 1,000,000 Chrome dropped them without a word (F3).

So the rule for the new presentation: **the browser's work is bounded by the screen, never by the map.** Each thing the map shows is sorted by two questions: does it change during play, and does it grow with the map? What is static goes into tiles; what changes is drawn only for the hexes in view.

## 2. What the Goodwood map shows, sorted

| Content | Changes when | Grows with | Today | Screen-bounded |
|---|---|---|---|---|
| Terrain and elevation | A new board version | The map | Tiles (B4) | Tiles, as now |
| Hex grid | Never (the lattice is fixed) | The map | One vector layer for the whole map (B2's repeated columns) | In the tiles at far and middle zoom; vectors for the hexes in view at near zoom (3.2) |
| Formation boundaries | Once per hand-off (the ownership metadata) | The formations | One whole-map path inside the marks group (`Play.razor` 4023 to 4029) | In the tiles (3.1) |
| Hex labels | Never | The map | Drawn for the hexes in view (B1) | As now |
| Setup marks (setup areas, the non-OB "?" hexes) | A setup click, per side's view | The eligible hexes | One whole-map string of 707,155 characters, built and sent whole on a change (C4) | Sent as data, drawn for the hexes in view (3.3) |
| Counters | Each event | The units | One fragment per occupied hex, all of them in the page (4,548 elements) | The same fragments, only those in view in the page (3.4) |
| Selection, LOS, range, highlights | A click | Small | Vectors | Vectors, as now |

Two of the seven are already screen-bounded (terrain, labels). The new work is the other four.

## 3. The parts

### 3.1 One composite tile per place

The Lab's `tileslines` painted terrain, grid, and boundaries into one image per tile (F6). For Goodwood the same: a tile is the terrain, the elevation, the grid, and the Formation boundaries, composited on the server, in the variant the layer switches ask for. Four switches make at most 16 variants, each painted only when asked. The page then holds one `<image>` per tile place, whatever is shown.

- **Lines are one tile pixel wide** at every level (the Lab's edge rule: a pixel whose hex differs from its right or lower neighbour's), and are left out of a level where they would be under about six pixels apart (`GridMinimumPixels`), so far zoom is not a mesh.
- **The boundaries are per game**, not per board: the ownership metadata is the game's sidecar. A boundary variant is keyed by the board version and the metadata's hash; a game without metadata has no such variant.
- **The alternative,** a transparent tile set per line layer stacked over the terrain tiles (the Raster Tiles Plan's task 5), keeps a switch instant but triples the images and decodes per place. A switch is rare and a pan is constant, so the composite is proposed (decision N1).

### 3.2 Near zoom: vectors for the hexes in view

A tile line is crisp while a tile pixel is no larger than a screen pixel. Past level 0 magnified (more than one screen pixel per board unit, about 56 per hex), lines thicken. There the grid is drawn as vectors for the hexes in view only, a few hundred at that zoom: the Lab found any technique smooth at that count (F1). The variant then asks for tiles without the grid; the boundaries can stay in the tiles, since they are drawn thicker.

### 3.3 Setup marks as data

The marks change on a setup click and differ by side, so they cannot be tiles: a tile repaint takes 0.3 to 0.75 s (F5). They are sent as data instead, once per revision and view: for each marked hex its kind (setup area, non-OB "?" hex, and so on). The browser draws the marks of the hexes in view at each commit, as it draws labels today. What a side may see is unchanged: the data is built from the same view the marks string is built from now.

This also retires C4: the server no longer builds and compares a whole-map string on every render.

### 3.4 Counters for the hexes in view (B6)

The counter layer is already one fragment per hex, with an id and a `data-hex` naming its column and row (`UnitOverlay.HexFragmentId`), and patches replace fragments by id. The browser keeps every fragment, but attaches to the page only those whose hex lies in the committed box (the view and its margin), and swaps them at each commit. Patches go to the held fragments whether attached or not. Nothing changes on the server.

### 3.5 Tiles before they are needed

A pan into new ground shows the coarser level, scaled, until its tiles arrive (F5). Two steps take that wait away:

- **Paint ahead.** When a board version is first served, a background task paints its levels from the top down in the default variant (about 215 tiles above level 0 for Goodwood), then, after each commit, the ring of tiles around the committed box and the next finer level.
- **Keep them.** Painted tiles go to a disk cache keyed by board version and variant (the backlog's "disk cache of tiles", section 96), so a Studio restart or a second game on the same board starts warm. The in-memory 64 MB cache stays in front of it.

Estimated: Goodwood has about 790 tiles across all levels (575 at level 0); at 0.3 to 0.4 s each that is four to five minutes of one core, under a minute on eight, and roughly 50 to 100 MB of PNG on disk per variant. These are estimates to be measured (section 5).

### 3.6 What stays

The viewport (`boardViewport.js`): pan and zoom by transform, the commit rules, `toBoard`, rotation, labels by view, the tile pool. The server's per-hex counter fragments and patches. The probe. Every rule, record, and replay: nothing here is read by a rule.

## 4. What it should give

| Measure | Basis | Expected |
|---|---|---|
| Drag, any length, any zoom | F6: display rate up to 1,000,000 hexes with lines in tiles | The display's rate, commits included |
| A commit's repaint | It draws the tiles in view plus the counters, marks, labels, and near grid of the hexes in view | Under 50 ms at any zoom (to measure) |
| Time until a pan into seen map is sharp | Tiles from memory or disk | Under 200 ms |
| Elements in the page | F7 | Bounded by the view: tiles, plus the counters, marks, and labels in view |
| A setup click's map update | The marks' data for one revision, not a 707 KB string | A small message; no whole-layer repaint |
| Larger Situations | Nothing in the page grows with the map | The same figures; the server's tile painting and disk grow, the browser's work does not |

## 5. How to get there

1. **Measure what a user meets.** The probe gains a long drag (two panes, so it commits on the way), a one-way zoom, and the time until the tiles in view are sharp. Without these the new presentation cannot be judged against the old one.
2. **Composite tiles with lines** (3.1), behind a switch (`?present=bounded`), so the user's Chrome compares old and new on the same Goodwood game.
3. **Counters and marks by view** (3.3, 3.4), behind the same switch.
4. **Near-zoom grid** (3.2).
5. **Paint ahead and keep** (3.5).
6. When the user's Chrome shows the new presentation at least as good everywhere, it becomes the default for the Exact view, and the switch keeps the old one for comparison for a while.

Steps 2 to 4 are one change to the viewport and the tile service; 5 is the server alone. The Lab keeps its role: a part whose cost at scale is unknown (the commit with counters and marks by view, for example) is tried there on a synthetic map first, if the user wants it measured before Goodwood.

### 5.1 Baseline: today's presentation (step 1, 2026-11-02)

Measured by the user in Chrome 156 with the new probe options, Goodwood reference, Allied view, one run each, at 912 by 558 (smaller than the 2,034 by 804 of the earlier sections).

| Run | Drag fps | Longest frame | Commits | Sharp after | Wheel: commits, sharp after, longest frame |
|---|---|---|---|---|---|
| Five steps in, drag one pane right, first run | 41.5 | 200 ms (three long frames) | 3 | 47 ms | 1, 253 ms, none long (alternating burst) |
| Five steps in, drag one pane left | 79.2 | 35 ms | 3 | 47 ms | 1, 288 ms, none long |
| Fit, then a one-way wheel zoom in | 83.4 (drag at Fit) | 35 ms | 1 | 58 ms | 2, 653 ms, 116 ms |
| Five steps in, drag one pane right, Formation boundaries on | 69.5 | 82 ms (two long frames) | 3 | 47 ms | 1, 289 ms, none long |

- A pan of one pane commits three times. The first pan over new ground had frames of about 200 ms; the same pan back over painted ground held 79 frames per second.
- Ground already seen is sharp within 47 to 58 ms after a drag.
- A real zoom in is the slowest to sharpen: 653 ms, waiting for the finer level's tiles to be painted.
- With the Formation boundaries on, the longest frame doubled (35 to 82 ms) at the same commits. The element count did not change (4,873), so whether the switch took effect is not certain from the record; the run is kept as reported.

These are the figures step 2 to 5 are compared against, at the same window size.

### 5.2 Step 2: lines in the tiles (2026-11-03)

Measured by the user in Chrome 156 with `?present=bounded`, the same game (now at Turn 1, Allied Prep Fire Phase), at 912 by 734 (taller than 5.1's 558), one run each.

| Run | Drag fps | Longest frame | Long animation frames | Sharp after | Wheel: sharp after, longest frame | Baseline (5.1): fps, longest frame |
|---|---|---|---|---|---|---|
| Five steps in, drag one pane right, first run | 73.8 | 47 ms | 0 | 223 ms | 418 ms, none long | 41.5, 200 ms (three long) |
| Five steps in, drag one pane left | 81.9 | 35 ms | 0 | 47 ms | 287 ms, none long | 79.2, 35 ms |
| Fit, then a one-way wheel zoom in | 84.1 | 23 ms | 0 | 46 ms | 666 ms, 118 ms | 83.4, 35 ms; zoom 653 ms, 116 ms |
| Five steps in, drag one pane right, Formation boundaries on | 75.8 | 71 ms | 0 | 46 ms | 249 ms, none long | 69.5, 82 ms (two long) |

- **The first pan over new ground no longer stalls:** its longest frame fell from 200 to 47 ms and its long animation frames from three to none. With the boundaries on, none either (two before).
- **Panning over painted ground is unchanged** (about 82 frames per second), as expected.
- **A first pan waits longer to sharpen** (47 to 223 ms): the lined tiles were painted for the first time.
- **A zoom in is unchanged** (653 to 666 ms to sharpen, one frame of about 118 ms): it waits on tiles painted on request, which step 5 addresses.

### 5.3 Step 5: tiles ahead and on disk (built 2026-11-03, before steps 3 and 4 at the user's word "Step 5 first")

- **On disk.** `TileCache` keeps a board's tiles under `{CacheRoot}/tiles/{board}/{version}/{ownership or _}/`, reads them back after a restart, and removes the board's other versions in the background. The Map Lab's tiles are not kept.
- **Ahead on the server.** With `?present=bounded`, the page asks `ahead-{variant}.json` once per variant; the Studio paints every level above 0 of that variant to disk in the background, on half the cores, never into the in-memory bound. On Goodwood all 215 tiles above level 0 of `te+g` were on disk within about 20 s of opening the game: 27 MB.
- **Ahead in the browser.** After each commit whose tiles are drawn, the next finer level's tiles for the part of the board on screen are fetched at low priority, at most 48 a time, each once, so a zoom in finds them in the browser's cache. Level 0 is painted on request by this fetch, since it is not painted ahead.

Measured by the user in Chrome 156 (2026-11-03), as in 5.2, at 912 by 734, after the tiles above level 0 were on disk:

| Run | Drag fps | Longest frame | Sharp after drag | Wheel: sharp after, longest frame | Step 2 (5.2) |
|---|---|---|---|---|---|
| Five steps in, drag one pane right, first run | 80.1 | 35 ms | 46 ms | 331 ms, none long | 73.8 fps, 47 ms; sharp 223 ms; wheel 418 ms |
| Five steps in, drag one pane left | 81.5 | 35 ms | 46 ms | 266 ms, none long | 81.9 fps, 35 ms; wheel 287 ms |
| Fit, then a one-way wheel zoom in | 84.8 | 24 ms | 46 ms | 663 ms, 142 ms | 84.1 fps, 23 ms; wheel 666 ms, 118 ms |

- **A first pan now behaves as a pan over seen ground:** sharp in 46 ms (223 before), no long frame.
- **Zooms near playing zoom sharpen faster:** 266 to 331 ms (287 to 418 before). About 220 ms of each is the probe's own wait for the burst's rest (`gestureRest` and 20 ms), after which a zoom commits.
- **The one-way zoom from Fit is unchanged** (663 ms, a 142 ms frame). Twenty steps in at 0.8 each zoom about 87 times, from level 3 straight to near zoom at level 0: the next finer level fetched ahead is level 2, and level 0 is not painted ahead, so its tiles are painted on request; and at near zoom the vector grid is shown again (decision N2), so the commit repaints it.

### 5.4 Level 0 ahead, and step 3 (built 2026-11-03, at the user's word "Yes, both")

- **Level 0 is painted ahead too.** The background painting now covers every level, coarsest first. On Goodwood the variant shown at most zooms (`te+g`) and the one shown at near zoom, where the vector grid stands in for the tiles' (`te`), were each painted whole: 790 tiles each, 198 MB on disk for both.
- **Counters and setup marks only in view** (decisions N3 and N4, built in the browser). After each commit, and whenever the counters or marks change, a counter fragment whose hex (its `data-hex`, widened by a hex each way) lies outside the drawn box, and a mark whose polygon does, gets `data-out="true"`, which a site.css rule hides. They stay in the page, so the counter patches and the marks' replacement work as before. `tiles.json` gains the hex lattice for this (`hexWidth`, `hexHeight`, `a1x`, `a1y`), and the layer token goes to 3 so browsers fetch it again. On Goodwood, five steps beyond playing zoom, all 530 counter fragments were hidden with none in view; at Fit all 530 were shown.
- **Deviation from N3:** the marks are still built and sent by the server as markup, not as data; only their drawing is bounded by the view. The server's whole-string build per render (C4) stays as it was.
- Out-of-view counters cannot take keyboard focus while hidden.

Measured by the user in Chrome 156 (2026-11-03) at 912 by 558, twice in one page: the first run after the layer token's change, when every tile came from the server again, and a second run with the browser's cache warm.

| Run | Step 5 (5.3) | 5.4, first run | 5.4, warm |
|---|---|---|---|
| Five steps in, drag one pane right | 80.1 fps, 35 ms | 65.5 fps, 82 ms | 81.7 fps, 35 ms |
| Five steps in, drag one pane left | 81.5 fps, 35 ms | 80.4 fps, 24 ms | 81.6 fps, 24 ms |
| Fit, then a one-way wheel zoom in: sharp after, longest frame | 663 ms, 142 ms | 1,114 ms, 142 ms | 635 ms, 134 ms |

- Warm, the pans are as in 5.3; the first run's slowness was the emptied cache.
- Level 0 painted ahead barely moved the deep zoom (663 to 635 ms). Of it, about 220 ms is the probe's wait for the burst's rest. Most of the rest: at the switch to near zoom the variant changed (`te+g` to `te`), and the viewport emptied the tile pool, so every tile in view was fetched and decoded again, the coarse tile under them included. The 134 ms frame is the commit at near zoom, where the whole-map vector grid and the labels were shown again.

### 5.5 Tiles kept through a switch, and step 4 (built 2026-11-03, at the user's word "Proceed")

- **A change of variant or lines keeps the old tiles** under the new ones until those have decoded, as a level change already did, so a switch to near zoom (or a layer switch) neither empties the map nor waits on every tile at once.
- **Step 4, the near grid (decision N2).** With `?present=bounded` the whole-map vector grid is always hidden; at near zoom the viewport draws the outlines of the hexes in the drawn box as one path (`#near-grid`, the vector grid's stroke, colour, and opacity), rebuilt only when the box changes. On Goodwood, twelve steps in, it held 284 hexes, aligned with the terrain, the labels, and the counters. It draws no centre dots, which the whole-map grid does.

### 5.6 Where it stands (2026-11-03)

The finished build in the user's Chrome at 912 by 558, one run, against the baseline of 5.1:

| | Baseline (5.1) | Finished build |
|---|---|---|
| First pan over new ground | 41.5 fps, longest 200 ms, three long frames | 82.4 fps, 24 ms, none; sharp in 34 ms |
| A pan with the Formation boundaries on | 69.5 fps, 82 ms, two long frames | 75.8 fps, 71 ms, none (measured after step 2, 5.2) |
| A pan over painted ground | 79.2 fps, 35 ms | 80.7 fps, 35 ms |
| A few wheel steps near playing zoom, sharp after | 253 to 288 ms | 356 to 394 ms |
| A deep wheel zoom from Fit, sharp after, longest frame | 653 ms, 116 ms | 700 ms, two of 143 ms, with 38 ms of forced style and layout |

- **Improved:** a pan over new ground no longer stalls; that came from the lines in the tiles (step 2).
- **Not improved, possibly worse:** zooms. The fix of 5.5 did not change the deep zoom, so its account in 5.4 was wrong. The 38 ms of forced style and layout appeared with steps 3 and 4 and may come from them. Single runs; not yet diagnosed.
- **Not measured:** a warm start from the tiles on disk after a restart.
- Open items are in the backlog's section 96.

### 5.7 Hex labels as one canvas (built 2026-11-03)

The user found on 2026-11-03 that the remaining lag of drag and zoom goes with the hex labels: with them off, both are good. At a readable zoom the drawn box holds about a thousand labels, each an SVG `<text>`, and `fillLabels` handed the texts out again in the new order at every commit, so nearly every one was laid out and painted again in that frame.

At the user's word ("Build it"), with `?present=bounded` the labels of the drawn box are painted at each commit into one canvas the size of that box on screen (`drawLabelCanvas`), inside the label group, so the layer switch and the six-pixel readable rule hold as before and a gesture moves its pixels with the map. The font, size, colour, and opacity are the texts'; on a turned map each label is painted upright, as site.css turned each text. The canvas is painted again only when the box, the scale, or the turn changes, and is at most 4,096 pixels a side. The side view keeps its texts. Checked on Goodwood in the Studio: 382 labels in one 1,442 by 2,138 canvas on a turned map, upright and in place, and in place unturned.

## 6. Risks

| Risk | Answer |
|---|---|
| Tile lines look coarser than vector lines | One tile pixel wide at every level; vectors at near zoom (3.2); judged on screenshots before the default changes |
| A layer switch shows soft tiles until the new variant arrives | The coarse level of the new variant first; paint ahead for the variant in use |
| Marks drawn by view leak what a side may not see | The data is built from the view, as the string is today; checked from each side's view |
| A counter outside the committed box misses a patch | Patches go to the held fragments, attached or not |
| Disk use for tiles | Bounded per board version; the oldest versions dropped first |
| Pages that patch vectors (the map author page, the Comparison view) | Keep the vector layers, as they do today for terrain |

## 7. Decisions for the user

| Id | Question | Proposal |
|---|---|---|
| N1 | One composite tile per place, or a transparent tile set per line layer | Composite (3.1) |
| N2 | The grid at near zoom | Vectors for the hexes in view (3.2) |
| N3 | Setup marks | Data, drawn for the hexes in view (3.3) |
| N4 | Counters | Only those in the committed box in the page (3.4) |
| N5 | Tiles ahead and on disk | Yes (3.5), measured before its bound is set |
| N6 | How it lands | Behind a switch on the Exact view, compared in the user's Chrome, then the default (section 5) |

## Related documents

- [ASL Map Studio Map Lab Plan](<ASL Map Studio Map Lab Plan.md>)
- [ASL Map Studio Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>)
- [ASL Map Studio Pan by Transform Plan](<ASL Map Studio Pan by Transform Plan.md>)
- [ASL Map Studio Counter Layer Design](<ASL Map Studio Counter Layer Design.md>)
- [ASL Independent Map Creation Analysis](<../ASL Independent Map Creation Analysis.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
