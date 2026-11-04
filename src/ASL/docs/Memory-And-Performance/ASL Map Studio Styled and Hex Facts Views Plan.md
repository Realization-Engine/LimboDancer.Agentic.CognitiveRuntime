# ASL Map Studio Styled and Hex Facts Views Plan

**Written:** 2026-11-04, at the user's word: "After that we need to bring the same refactors to the Styled and Hex facts views for Large Maps." And, while it was written: "The actual Game Maps don't have a way to choose the Map Style. This should be a chose in the Play and Replay pages that launches the games" (step 1b).
**Status:** for the user's approval; nothing is built.
**Sources:** `BoardRenderer` (`Layers`, `WriteHexFacts`, `WriteHexMarks`) and `StyledLayers.cs` in `Maps.Rendering`; `boardViewport.js`, `RenderEndpoints.cs`, and `TileCache.cs` in the Studio, on `main` with PR 117 (the centre dots); the [Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>), the [Screen-Bounded Map Analysis](<ASL Map Studio Screen-Bounded Map Analysis.md>), the [Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>), the [Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>), and the [Counter Drawing Plan](<ASL Map Studio Counter Drawing Plan.md>).

## 1. Where the two views stand

The board viewer and the map author page offer four views of a board: Exact, Styled, Hex facts, and Comparison. Play and Replay draw only the Exact view, and every refactor of the last weeks was built for it. Measured on Goodwood's board in my Studio on 2026-11-04 (the layer files as the server sends them):

| View | What it draws | Its layers | Elements |
|---|---|---|---|
| Exact | Terrain and elevation by grid cell | Raster tiles (4.5 MB of vectors no longer sent) | Tiles in view |
| Hex facts | Each hex's derived facts: its centre terrain's colour, a hatch for a depression, its hexside terrain as thick strokes on its edges (an off-map side dashed red), a stairway square, and a mark such as "+1 L2 B" | `hexfacts.svg`, 7.2 MB (1.1 MB gzipped), one group a hex | About 89,000 |
| Styled | The map as drawn from its features: elevation, area, linear, bridges, buildings, hexsides, marks, with smoothed outlines and the theme's patterns | Seven `styled-*` layers, 1.2 MB together | About 2,800 |

What each view has of the refactors:

| Refactor | Exact | Styled and Hex facts |
|---|---|---|
| Pan and zoom by transform | Yes | Yes |
| Hex labels as data, one canvas | Yes | **No**: the viewport treats labels as data only for the Exact view, so both fetch `labels.svg`, 1.7 MB and 40,442 texts on Goodwood. The server already serves `labels.json` for every view |
| Tile index with the hex lattice (`tiles.json`) | Yes | **No** (404): so no counters or marks hidden out of view, and no near grid |
| Raster tiles, painted ahead and on disk, paced, fetched ahead | Yes | **No**: vector layers |
| Grid in the tiles, with the centre dots | Yes | **No**: the whole-map vector grid (75,000 characters), with its dots |
| One reference a counter | Yes | Yes (the counter layer is the same in every view) |

The Hex facts view is where the old Exact view was before its tiles, at about twice the size. The Styled view is light by comparison, and its cost is not measured.

## 2. Baseline, expected gains, method, stop threshold

| Measure | Baseline | Expected | Stop threshold |
|---|---|---|---|
| Hex facts, Goodwood board: drag fps at Fit, five steps in, labels readable | Step 0 | 80 fps or more at each, as the Exact view after its tiles | Below 50 at any after step 2: stop and report |
| Hex facts: elements in the page at Fit | About 89,000 plus 40,442 labels | Under 300 | |
| Styled, Goodwood board: the same drags | Step 0 | After step 1, decided by its figures (V3) | |
| Both views: labels, data and the readable-zoom drag | 40,442 texts; step 0 | One canvas; the drag where labels are readable within 5 fps of the Exact view's | |
| Hex facts: how it looks | The vector layer | Pixel for pixel at level 0, as the Exact view's tiles were checked (Large Campaign Map Analysis, section 10.6), except the antialiased edges of the hexside strokes | A difference a user can see at playing zoom: stop and report |

**Method.** The user's Chrome, on Goodwood's board in the board viewer (`/boards/{board}?mapMetrics=1`), with the view picker on each view, `mapProbeSuite()` in the console; three runs, the first dropped; the window and display of the earlier runs (912 by 734, about 85 Hz). My browser pane checks that each step works, not its speed. Each step is measured before the next is built.

## 3. The steps

### Step 0: the baseline

The user runs `mapProbeSuite()` on Goodwood's board in the board viewer, once in the Hex facts view and once in the Styled view. No code.

### Step 1: what both views can take from the Exact view at once (browser and endpoint only)

- **Labels as data in every view** of a board: `labelsAsData` takes any view's base address, so the Styled and Hex facts views fetch `labels.json` and paint one canvas, as the Exact view does. The side-by-side comparison keeps its texts, as now.
- **The tile index for every view:** the server answers `tiles.json` for the Styled and Hex facts views with the board's lattice (it is the same board), so the viewport can place counters by hex and hide those out of view. With no tiles yet, the index says the view has none, and the viewport draws no tile group.

Expected: the 40,442 texts leave both views, and counters out of view are hidden in them. No new tile painting.

### Step 1b: the map style in Play and Replay (decision V5)

Play and Replay draw a game's map only in the Exact view. Both gain the view picker the board viewer has (`BoardViewPicker`), offering Exact, Styled, and Hex facts, beside the map's other controls:

- **What it changes:** only how the board is drawn. The counters, LOS and range lines, marks, selection, and every rule, record, and replay are the same in every view; the counter layer is drawn over whichever view is chosen, as the board viewer draws it.
- **Remembered** per viewer in the browser, as the map's turn is (the Exact view when nothing is kept), so a player who prefers the Styled map keeps it from game to game; the page's address carries it too (`&style=styled`), so a link opens the same view.
- **The Comparison view** is not offered in a game (V4).
- **The hand-over screen's small map** keeps the Exact view.

With it, a Campaign game such as Goodwood can be played in the Styled or Hex facts view, which is what makes steps 1 and 2 matter for play, not only for the board viewer.

### Step 2: raster tiles for the Hex facts view

A tile painter for the facts, beside `TilePainter`, from the same `BoardRenderInput`:

- **Each pixel's hex** by the lattice (`TileLinePainter.OrdinalAt`), coloured by that hex's centre terrain (the colour `WriteHexFacts` fills it with), or white where it has none.
- **A depression's hatch**: the `depression-hatch` pattern's stripes painted into the hex's pixels.
- **Hexside terrain**: a band along the edge, four cells wide (five for a cliff), in the hexside's colour, painted in the pixels within half the width of the edge; an off-map side dashed red, 1.5 cells wide.
- **The stairway's square**: six cells, white with a black edge, at the hex's centre.
- **The grid and its centre dots** as the Exact view's tiles carry them (`TileLinePainter`), under the same `+g` variant.
- **The text marks** ("+1", "L2", "B") are not painted: the server has no font. They go to the browser as data (`hexmarks.json`: a hex's centre and its mark), and the viewport paints them in view into the label canvas, as the labels (decision V2).
- **The trace mode** (the legend's colours for where a centre's terrain came from) is a variant of its own (`f`, and `ft` traced).

The tiles go through the same `TileCache`: in memory, on disk under the painter's version, painted ahead, paced, and fetched ahead during a zoom. The vector `hexfacts` layer stays for the map author page and for any page that turns tiles off, as the Exact view's vectors did.

### Step 3: the Styled view, decided by its figures (decision V3)

After step 1, the Styled view sends about 2,800 elements and no labels. If its drags then hold the Exact view's figures, it stays vector, and this plan ends. If not, the next step is its own section here, with the choice the figures point to:

- **Server tiles** need a rasterizer for smoothed paths, strokes, and the theme's patterns. The Studio has none, and the Raster Tiles Plan's decision T2 rules out a native library; a managed one would be new code of its own, or a new dependency for the user to rule on.
- **Tiles painted in the browser**: the Styled layers drawn once a level into bitmaps the viewport keeps, so a pan moves pictures. The browser draws the vectors exactly; the cost is on the user's machine at each level's first visit.

## 4. Tests and checks

- **Step 1:** Node viewport tests that any view's labels come as data, and that a tile index with no levels draws no tiles but places counters; a render-endpoint test that `tiles.json` answers for the Styled and Hex facts views.
- **Step 1b:** component tests that Play and Replay offer the three views, load the chosen one with the counter layer over it, keep the choice across a reload and in the address, and draw the hand-over map in the Exact view; the icon coverage file for the picker; in my Studio, a step of play in each view on bd01 and on Goodwood (behind "show my view").
- **Step 2:** painter tests in `Maps.Rendering.Tests`: each hex's colour at its centre; a hexside band on the right edge, in the right colour and width; an off-map side's dashes; the stairway's square; the grid where it is wide enough; the comparison with the vector `hexfacts` layer at level 0 on bd01, every pixel, as the Exact view's tiles were checked. `TileCacheTests` for the new variants' names and keys. A test of `hexmarks.json`.
- **My Studio at 6670:** bd01 and Goodwood's board in the board viewer, each view, Fit to the closest zoom; the map author page unchanged; screenshots of the Hex facts view's tiles beside its vectors.
- **The user's Chrome:** the figures of section 2.
- Render only; no rule, record, or replay: no merge-gate stage.

## 5. Risks

| Risk | Answer |
|---|---|
| The facts' tiles look unlike the vectors | Compared pixel for pixel at level 0; hexside strokes are the one place an edge's antialiasing differs, judged on screenshots |
| A hex's facts change (an edit on the author page) | The author page keeps the vector layer, as it does for the Exact view; elsewhere a new board version makes new tiles |
| Text marks out of place | They are drawn at the centres the vector layer uses, in view, in the label canvas; a Node test places them |
| The disk holds more tiles | Two more variants a board version (`f`, and `ft` only when traced), about the Exact view's size, kept one version a board |
| Something reads the Hex facts groups (`h-{hex}`, `data-hex`) in the page | Searched 2026-11-04: nothing in the Studio reads them (the other `data-hex` attributes are on Play's panels and the card editor's picked hexes). A page that came to need them would keep the vector layer |

## 6. Decisions for the user

| Id | Question | Proposal |
|---|---|---|
| V1 | The order | Step 0, then steps 1 and 1b together (1b is small and needs step 1's labels and culling to be playable on a large map), then step 2, each measured; step 3 only as V3 says |
| V2 | The Hex facts view's text marks | Data drawn in the label canvas, since the server paints no text |
| V3 | The Styled view | Vector after step 1 unless its figures fall short; then a section of its own, choosing between server tiles (a rasterizer to write or a dependency to rule on) and tiles painted in the browser |
| V4 | The Comparison view | Unchanged: it overlays the Styled layers on the Exact terrain to compare them, and is a tool, not a view to play on; not offered in Play or Replay |
| V5 | The map style in a game | A view picker in Play and Replay (Exact, Styled, Hex facts), remembered per viewer in the browser and carried in the address; Exact when nothing is kept |

## 7. Answers to the guidelines' checklist

- **Work per state, unit, hex, or step:** none in play. Step 2 adds a tile painter run once per tile and variant, ahead and on disk, as the Exact view's.
- **Ownership, keys, lifetime, bounds, invalidation, concurrent fills:** the same `TileCache`: one version a board in memory and on disk, the painter's version in the key, one painting per tile at a time. The marks' data is a file of the board version, cached as the labels are.
- **Disclosure, rout settlement, snapshots, pinned versions, seams:** unaffected; the views show the board, not a game's state.
- **Unchanged counters reuse fragments:** unaffected.
- **Checks and measurements:** section 4; the probe and its suite, which work in any view.
- **Tests:** in the existing test projects; no new project.
- **Benefit against noise:** three runs, the first dropped, the same window and display.
- **Results, backlog, outside outputs:** results in a section of this plan; the backlog's row "Raster tiles for the Styled and Hex facts views" updated; my Studio's build in `asl-gate-work/studio-6670`, cleaned as usual.

## Related documents

- [ASL Map Studio Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>)
- [ASL Map Studio Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>)
- [ASL Map Studio Counter Drawing Plan](<ASL Map Studio Counter Drawing Plan.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
