# ASL Map Studio Raster Tiles Plan

**Written:** 2026-11-02, at the user's word: "Write the B4 plan".
**Source:** `ASL Map Studio Large Campaign Map Analysis.md` (the analysis): option B4 (section 6, group B), decision P3 (section 8), the first wave's results (section 10), and M1, the user's own browser (section 10.6).
**Scope:** B4 for the Exact view: its terrain and elevation layers drawn as raster tiles at several zoom levels, in one branch and one pull request. The grid joins the tiles only if task 1 shows it is worth it (decision T3). Not in scope: the Styled, Hex facts, and Comparison views; the map author page; B5, B6, C1 to C4, D1, and D2.
**Status:** approved by the user on 2026-11-02 ("Approve as proposed"), with decisions T1 to T5 as section 7 proposes them. Built on branch `claude/raster-tiles`: tasks 0 to 4, 6, and 7; task 5 (the grid in tiles) not built, by decision T3 on task 1's measurement. Results in the analysis, section 11: a Goodwood drag from 5.3 to about 23 frames per second in the user's Chrome. The merge waits for a green CI run and the user's word.

## 1. Goal and expected gains

Make dragging and zooming a Goodwood-sized map smooth in an ordinary browser, and make the browser's cost of a map depend on the screen, not on the map's size. Nothing any page shows changes beyond the softening of section 3.2, and no rule, record, or replay changes.

Stated up front (the user's rule of 2026-10-19 for performance plans):

| Measure | Baseline: user's Chrome, 2,033 by 803 (10.6) | Baseline: Claude pane (10.1) | Expected after this plan | Stop threshold |
|---|---|---|---|---|
| Drag: frames per second | 5.3 | 8.4 to 9.8 | 30 or more | Below 15 after task 4, in the pane: stop and report |
| Drag: median frame | 12 ms | 129 to 141 ms | stays under 20 ms | |
| Drag: longest frame | 777 ms | 165 to 329 ms | under 100 ms | Above 300 ms after task 4: stop and report |
| A burst of 20 wheel events, to settled | 116 ms | 61 to 179 ms | under 150 ms (a zoom may load new tiles) | Above 300 ms |
| Label layer switch | 129 ms | 174 ms | under 100 ms (less to repaint under the labels) | Recorded, not a gate |
| Characters in the map | 4,904,874 (the same page as the pane's) | 4,904,874 | about 0.5 million: 4.4 million of terrain and elevation paths leave the page | Recorded |
| Browser memory for the map | not measured since the first wave (the 2 GB of the analysis, 3.4, was the pane before it) | | decoded tiles: about 12 MB at Fit, at most about 80 MB at any zoom (section 2.5) | Measured before and after with Task Manager, not a gate |
| Server memory added | | | a tile cache bounded at 64 MB | Bound held under the probe |

These are estimates. The basis: in the user's Chrome most drag frames take 12 ms, because the browser reuses what it painted, and a few take most of a second, when the moved view needs the paths painted again (analysis, 10.6). Those repaints are of 4.4 million characters of terrain and elevation paths; with tiles a repaint draws at most a few dozen images and the grid. Task 1 measures how much of a frame the grid takes before anything is built, and the estimates are revised there if it is large.

## 2. How it works

### 2.1 Tiles are painted from the grid, not from the SVG

The Exact view draws the terrain grid cell by cell: `WriteExactTerrain` fills each cell's region with its catalog colour, and `WriteElevation` lays black (for levels above 0) or `#1f4e79` (below 0) over it at opacity `min(0.5, 0.12 x |level|)`, both with `shape-rendering: crispEdges` (`BoardRenderer.cs:250-300`). The regions are traced along cell edges, and filling them reproduces the grid exactly (`GridOutlineTracer.cs:6-10`), so the drawing at one screen pixel per cell is exactly one colour per cell:

```
colour(cell) = blend(catalogColour(code), overlay(elevation), opacity(elevation))
```

A code the catalog does not know is `#ff00ff`, as in the SVG; a cell at level 0 has no overlay.

So a tile can be painted straight from `TerrainGrid.Codes` and `Elevations`, with no SVG rasteriser and no image library. This avoids the native-library risk of SkiaSharp on the Linux CI runners (the lesson of `embedded-resource-culture-linux`), and the lock files do not change.

### 2.2 The pyramid

- **Tile size:** 512 by 512 tile pixels.
- **Level 0:** one tile pixel per grid cell. Goodwood (11,700 by 12,449 cells) is 23 by 25 tiles.
- **Level n:** one tile pixel per 2^n by 2^n cells, the average of their colours. Levels go up until the board fits one tile (Goodwood: level 5, one tile of 366 by 390, so six levels).
- **Painted on request**, never ahead: a level-n tile reads its 512·2^n square of cells directly from the grid, so no level depends on another. A level-2 tile reads 4.2 million cells, an estimated 10 to 30 ms with the PNG encoding.
- **Edge tiles** are cut to the grid; the margin and the legend stay as they are.

### 2.3 Format and encoder

PNG, 8-bit RGB (RGBA for the elevation-only variant, section 2.6), each row filtered by "Sub", compressed by .NET's own `ZLibStream`, with a 256-entry CRC-32 table: about 120 lines in `LimboDancer.Domains.Asl.Maps.Rendering`, beside `SvgWriter`. PNG is lossless, so level 0 is exact; terrain's flat areas compress well. JPEG is not used: it blurs cell edges and makes level 0 inexact.

### 2.4 Serving and caching (server)

- **URL:** the existing route `/render/{board}/{version}/exact/{file}`, with files `tile-{variant}-{level}-{column}-{row}.png` and `tiles.json` (the grid's width and height, the tile size, and the levels). Today the handler accepts only `.svg` layer names and `labels.json` (`RenderEndpoints.cs:99-115`); it is widened to these two names, with the content types `image/png` and `application/json`, and only for the Exact view. The version in the URL makes every response immutable; the ETag is the PNG's content hash and `Cache-Control: immutable`, as for the SVG layers; PNG is not gzipped again.
- **`TileCache`**, a new Studio service beside `RenderCache`: PNG bytes keyed by board, version, variant, level, column, and row; a least-recently-used bound of 64 MB by bytes; the same rule as `RenderCache` that a board keeps only the version last asked for; one fill per key under concurrency (a `Lazy` per key, so two requests for one tile paint it once).
- **Buffers:** a row buffer and a pixel buffer rented from the shared pool per tile and returned; no per-cell allocation.

### 2.5 Drawing (browser)

- **In place of two layers:** `Load` gets a tiles flag. With it, the viewport does not fetch `exact-terrain.svg` and `exact-elevation.svg`; one group `layer-exact-tiles` stands where they stood, holding `<image>` elements placed in board coordinates (cell units) with `preserveAspectRatio="none"`. The C# layer lists (`BoardRenderer.Layers`) do not change, so the layer switches, the author page, and the tests that name those layers stay as they are.
- **Which level:** the drawing's scale is the one `toBoard` already uses, `min(width / box.width, height / box.height)` (`boardViewport.js:1042`), since a fitted map is letterboxed: at Fit in a 2,033 by 803 pane, Goodwood is held by its height, about 0.064 screen pixels per cell. The level is the coarsest whose tile pixel is no larger than a device pixel (that scale times `devicePixelRatio`): level 3 at that Fit, 12 tiles for the whole map.
- **Which tiles:** on each frame's flush (`flushBox`, where the labels in view are already chosen), the tiles that meet the box widened by one tile. The `<image>` elements are pooled and reused, as label texts are.
- **Sharp only when magnified:** a tile is drawn on screen at between half and one device pixel per tile pixel, except at level 0 when zoomed past one pixel per cell; only then does it get `image-rendering: pixelated`, so the cells stay sharp as the crisp-edged vectors do. Shrunk tiles keep the browser's smoothing, so they do not shimmer while dragging.
- **No stalls on decode:** a tile is fetched and decoded off-screen (`new Image()`, `img.decode()`) before its `<image>` gets the URL, so a frame never waits for a decode. Until it arrives, the tile beneath is the coarsest level's single tile, which stays loaded for the whole visit: no blank squares, only a short softness.
- **Bound:** a tile covers 256 to 512 device pixels on screen, so a 2,033 by 803 pane at `devicePixelRatio` 1 shows at most about 45 tiles, and about 80 with the margin; at 1 MB decoded each, that is the "at most about 80 MB" of section 1. The pool keeps only those; others are dropped and left to the browser's HTTP cache.
- **The probe** (`mapProbe`) gains a field for the level and the number of tiles in view, and an option to hide named layers for task 1.

### 2.6 Layer switches

The layer switches keep their names: "exact-terrain" and "exact-elevation". `setVisible` maps the two to the tile group, which asks for one of three variants: terrain with elevation (the usual one), terrain alone, or elevation alone (RGBA: the overlay colour at the overlay's opacity, transparent where the level is 0, as the SVG elevation layer draws it). Hiding both hides the tile group.

### 2.7 Which pages draw tiles

Every page that shows the Exact view through the shared viewport: Play and Replay (`BoardWorkspace`), the board viewer's Exact view (`BoardViewer.razor`), the hand-over map (`HandOverMap.razor`), and the card map picker (`CardMapPicker.razor`). The map author page (`Author.razor`) keeps the vector layers, since its edits patch them (`RefreshedLayers`, `Author.razor:88`), and so does the Comparison view, which draws the Exact terrain under the Styled layers. `?tiles=0` on any page restores the vector layers, for comparison and measurement.

## 3. What changes for a viewer

### 3.1 Unchanged

Counters, their tiers and patches; the grid, labels, legend, LOS and range drawings; rotation; the Comparison view and its side-by-side mode, which keep the vectors; hex picking and the hex inspector, which read board coordinates through `toBoard`, not the terrain paths (no script reads those paths).

### 3.2 Changed

At far zoom, terrain is the average of its cells instead of the browser's aliased sampling of crisp-edged paths: thin features (a one-cell-wide road at Fit) become lighter tints instead of broken lines. At level 0 and closer the drawing is the same cell for cell. Task 6 compares the two side by side at five zooms, and the user judges the far-zoom look (decision T4).

## 4. Tasks

### Task 0: branch

A branch `claude/raster-tiles` from an up-to-date `main`. The Goodwood working copy `asl-gate-work/ref-run-head` is the measured map (decision P4); `dl-tractor-works-02` is the check of a stock multi-board map with seams. The Studio build folder `asl-gate-work/studio-6670` is registered as an outside output.

### Task 1: where a frame's time goes

Extend the probe with a `hide` option and measure Goodwood at Fit in the pane, three runs each: every layer shown; terrain and elevation hidden; the grid hidden; both hidden. This tells how much of a drag frame the tiles can remove and how much the grid keeps. If the grid takes a large share (decision T3), task 5 is done; otherwise task 5 goes to the backlog. The section 1 estimates are revised here. Tests: the Node probe test for the `hide` option.

### Task 2: the tile painter

In `Maps.Rendering`: `TilePainter` (a tile's pixels from a grid, a catalog, a variant, a level, and a tile position) and `PngEncoder`. Tests, named by method, on the synthetic board CI already uses:
- `Level0MatchesTheExactFormulaPerCell`
- `LevelNAveragesItsCells`
- `EdgeTileIsCutToTheGrid`
- `ElevationVariantIsTransparentAtLevelZero`
- `PngRoundTripReadsBackTheSamePixels` (decoded in the test by `ZLibStream` and the filter's inverse)

### Task 3: the endpoint and the cache

`TileCache`, the handler widened to the tile files and `tiles.json`. Tests: `HoldsOneVersionPerBoard`, `EvictsLeastRecentlyUsedByBytes`, `PaintsATileOnceUnderConcurrentRequests`, and in `RenderEndpointTests` the tile's content type, ETag, and 304, and a 404 for an unknown tile or a view other than Exact.

### Task 4: the tile layer in the viewport

`layer-exact-tiles` in `boardViewport.js`, the tiles flag on `Load` from the pages of section 2.7, the variant switch, and `?tiles=0`. Studio check on Goodwood, then the probe in the pane. **The stop thresholds of section 1 apply here:** if a drag in the pane is below 15 frames per second or its longest frame is above 300 ms, stop and report before going on. Tests: Node tests for the level choice and the tiles in view (`tilesInView`, beside `labelsInView`), and for the pool's bound.

### Task 5: the grid in tiles (only if task 1 calls for it)

A separate, transparent grid tile set (outlines at opacity 0.45 and centre dots at 0.6, as `WriteGrid` draws them, `BoardRenderer.cs:416-431`), anti-aliased by drawing at four times the tile's scale and averaging. It is its own group under the "grid" switch, not baked into the terrain variants, so the switches of section 2.6 keep working. At level 0 and closer the vector grid is shown instead, since a raster line blurs when magnified.

### Task 6: the Studio check

Every page of section 2.7, turned and unturned, at Fit and four other zooms, on Goodwood and `dl-tractor-works-02`: tiles against `?tiles=0`, the layer switches, rotation, and side-by-side. Screenshots of the far-zoom comparison for the user (decision T4).

### Task 7: measure and record

The probe in the pane, three runs; then the user runs it in Chrome at the same size as 10.6 (the M1 steps). Results, limits, and backlog rows go in the analysis as section 11. Durations go in the time log by `timelog_row.py`.

### Task 8: CI and pull request

No rule, planner, projector, or record changes, so no replay proofs or sweeps (the user's standing rule of 2026-11-01). The gate's `strict` stage, the Node viewport tests by hand, then the pull request and a green CI run. The merge waits for the user's word. Outside outputs are cleaned at the end.

## 5. Risks

| Risk | Answer |
|---|---|
| Tiles and the vector drawing disagree by a pixel's rounding of the blend | Test tolerance of 1 in each channel at level 0; the Studio comparison at 400% |
| A tile request storm on the first view (12 tiles at Fit, up to about 80 after a zoom) | Tiles are painted in parallel by the requests themselves; the coarse tile shows at once; the stop threshold covers the result |
| Decoding still stalls a frame | Decode off-screen before attaching; measured by the probe's longest frame |
| A board with no grid, or a composed map | Tiles need only `StudioBoard.InputFor(BoardView.Exact)`, which composed maps already give; a board without it keeps the vector layers |
| A new board version leaves stale tiles | The version is in the URL and the cache key; the cache keeps one version per board |
| `image-rendering: pixelated` on an SVG `<image>`, used only for magnified level-0 tiles, differs across browsers | Checked in the pane (Chromium) and the user's Chrome; Edge is Chromium too; others are out of scope |
| Server memory during a tile storm | Pooled buffers per tile; the 64 MB cache bound; measured with the probe |

## 6. Out of scope, for the backlog

When the plan is approved, these join the backlog's section 96:

- Tiles for the Styled and Hex facts views (they need the theme or the facts painted, not cell colours).
- Tiles on the map author page.
- The grid in tiles, if task 1 does not call for it.
- A disk cache of tiles across Studio restarts.
- B5 and B6, C1 to C4 and M3, D1 and D2, as before.

## 7. Decisions for the user

| | Question | Proposal |
|---|---|---|
| T1 | Which pages draw tiles | Every Exact view in the shared viewport except the map author page (section 2.7), with `?tiles=0` to compare |
| T2 | Painting tiles from the grid instead of rasterising the SVG | Yes: exact at level 0, no native library, no lock file change (section 2.1) |
| T3 | The grid in tiles | Only if task 1 shows the grid takes a large share of a drag frame; otherwise backlog |
| T4 | The far-zoom look (section 3.2) | Accept averaged cells; judged on task 6's screenshots before the pull request |
| T5 | Server tile cache size | 64 MB, in memory, rebuilt after a restart |

## 8. Checklist

Answers to the [guidelines'](<ASL Memory and Performance Guidelines.md>) checklist:

- **Work added per state, unit, hex, or step:** none per state or step. Per board version: tiles painted when a view first asks for them, from the grid the board already holds. In the browser: a tile choice per frame, bounded by the tiles in view.
- **Ownership, keys, lifetime, bounds, concurrent fills:** `TileCache` owns tiles, keyed by board, version, variant, level, column, and row; one version per board; 64 MB by least-recently-used; one fill per key. The browser's pool is bounded by the view; its HTTP cache by the immutable URLs.
- **Disclosure, snapshots, pinned versions, seams:** tiles show terrain only, which every viewer sees; the pinned version names the tiles; composed maps tile their composed grid, so seams draw as today.
- **Counters:** unchanged; the PR 22 fragments, patches, and tiers are not touched.
- **Checks and measurements:** the probe before and after, in the pane and in the user's Chrome; the named unit and Node tests; the Studio check of every map page.
- **Tests:** each test builds its synthetic grid once per class; no new test project.
- **Benefit and noise:** section 1's thresholds, three runs each, at the same window size as 10.6.
- **Records:** results, limits, backlog rows, time log, and outside-output cleanup before the pull request.

## 9. Deviations found while building

| | What changed | Why |
|---|---|---|
| D1 | Above level 3 a tile pixel averages an even 8 by 8 sample of its cells, not all of them (`TilePainter.SamplesPerSide`) | A top tile reads every cell of the map: 8.3 s for Goodwood with the first painter, 2.4 s with the colour table, 0.27 s with sampling. At those levels one screen pixel spans 16 cells or more |
| D2 | The painter looks each (code, level) colour up in a table made once per tile, and adds integers | The first painter's floating-point blend per cell made a level-4 tile take 1 to 4 s |
| D3 | While a zoom is moving, the viewport keeps the level it draws (150 ms of rest before a change), and keeps the old tiles under the new until those have decoded | A zoom asked for every level it passed through: tiles nobody saw, painted ahead of the ones wanted |
| D4 | Labels and tiles are chosen from the part of the board on screen (`visibleBox`), not the view box | The user's screenshot: in a pane wider than the box, the strips beside it had no labels. The labels' fault came with the first wave's labels on demand (PR 102); the tiles had it too, hidden by their margin |
| D5 | The pane cannot judge the thresholds: with every layer hidden, an empty map, it drags at about 11 frames per second with frames of 100 to 280 ms, none in script and little in style and layout | Task 1's split and task 4's stop thresholds are judged in the user's Chrome instead |

## Related documents

- [ASL Map Studio Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>) (sections 6, 8, 10)
- [ASL Map Studio Large Campaign Map Plan](<ASL Map Studio Large Campaign Map Plan.md>) (the first wave)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
- [ASL Independent Map Creation Analysis](<../ASL Independent Map Creation Analysis.md>) (the larger aim: maps far beyond Goodwood)
