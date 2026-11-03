# ASL Map Studio Screen-Bounded Map Analysis

**Date:** 2026-11-02
**Status:** approved by the user on 2026-11-02 ("Approve as proposed"), with decisions N1 to N6 as section 7 proposes them. No code is changed yet. It answers the user's request of 2026-11-02: apply what the Map Lab learned to a new way of presenting the Goodwood ASL map, rather than explain again why the present one is slow.
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
