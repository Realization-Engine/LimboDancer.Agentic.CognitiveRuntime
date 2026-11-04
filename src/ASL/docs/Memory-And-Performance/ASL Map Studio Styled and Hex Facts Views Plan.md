# ASL Map Studio Styled and Hex Facts Views Plan

**Written:** 2026-11-04, at the user's word: "After that we need to bring the same refactors to the Styled and Hex facts views for Large Maps." And, while it was written: "The actual Game Maps don't have a way to choose the Map Style. This should be a chose in the Play and Replay pages that launches the games" (step 1b).
**Revised:** the same day, at the user's two corrections. On Hex facts: "the Hex Facts hexes are rendered as a single solid color. Think about that." On Styled: "The 'Styled' view will definitely need a raster like the 'Exact' view needed. Why do you think you need to do measurement to figure that out? All our work from yesterday should be informing your choices."
**Status:** approved by the user on 2026-11-04 ("Approve as proposed"), with decisions V1 to V6 as section 7 proposes them.
**Sources:** `BoardRenderer` (`Layers`, `WriteHexFacts`, `WriteHexMarks`) and `StyledLayers.cs` in `Maps.Rendering`; `TilePainter` and `TileLinePainter`; `boardViewport.js`, `RenderEndpoints.cs`, and `TileCache.cs` in the Studio; `LabMapRenderer.TilePng`; on `main` with PR 117 (the centre dots). The [Screen-Bounded Map Analysis](<ASL Map Studio Screen-Bounded Map Analysis.md>), the [Map Lab Plan](<ASL Map Studio Map Lab Plan.md>) (findings F2 to F9), the [Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>), the [Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>), the [Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>), and the [Counter Drawing Plan](<ASL Map Studio Counter Drawing Plan.md>).

## 1. What decides it

The rule of the Screen-Bounded Map Analysis (section 1): **the browser's work is bounded by the screen, never by the map.** Whatever a view draws as vectors over the whole map grows with the map, and the Map Lab showed where that ends: at 100,000 hexes the whole-map paths were the whole cost of a drag (F4), and at 1,000,000 Chrome stopped drawing them without a word (F3). With the lines in raster tiles, a drag ran at the display's rate at every size (F6). So both views go to raster tiles, through the pipeline the Exact view has: painted ahead, kept on disk under the painter's version, paced a few a frame, fetched ahead during a zoom, with the grid and its centre dots in them. Measures come afterwards, to show the gain, not to decide.

A layer is judged by what grows with the map (the area it paints and its path data), not by its element count.

## 2. Where the two views stand

The board viewer and the map author page offer four views of a board: Exact, Styled, Hex facts, and Comparison. Play and Replay draw only the Exact view. Measured on Goodwood's board in my Studio on 2026-11-04 (the layer files as the server sends them):

| View | What it draws | As sent today |
|---|---|---|
| Exact | Terrain and elevation by grid cell | Raster tiles |
| Hex facts | Each hex in **one flat colour**, its centre terrain's (white where it has none), with a hatch for a depression, its hexside terrain as thick strokes on its edges (an off-map side dashed red), a stairway square, and a text mark such as "+1 L2 B" | `hexfacts.svg`, 7.2 MB, about 89,000 elements: one group a hex |
| Styled | The map from its features: the base elevation's tint, elevation regions, areas, linear features (an outline, and centreline strokes with round caps and joins, some dashed), bridges, buildings (with a shadow at 0.25), hexside strokes, stairway squares; smoothed outlines (cubic Bézier rings), filled with a colour or a theme pattern | Seven `styled-*` layers, 1.2 MB of whole-map path data |

Both views also lack what the Exact view gained last week: their labels come as `labels.svg` (1.7 MB, 40,442 texts on Goodwood), though the server already serves `labels.json` for every view; and they have no tile index (`tiles.json` answers 404), so counters and marks out of view are not hidden and there is no near grid.

**The Hex facts view is the cheapest to tile.** A hex in one flat colour is exactly what the Map Lab's tiles paint (`LabMapRenderer.TilePng`: each pixel takes the colour of the hex its centre falls in), which the Lab ran at the display's rate up to 1,000,000 hexes. The Exact view's line painter already finds each pixel's hex (`TileLinePainter.OrdinalAt`) and paints the grid and the centre dots. So a Hex facts tile is a colour a hex handed to code that exists, plus the few things drawn on top.

**The Styled view needs a rasterizer.** Its tiles are not cell or hex colours: they are smoothed filled shapes, strokes, and patterns. The Studio has nothing that paints those, and the Raster Tiles Plan's decision T2 rules out a native library. This is the plan's one open choice (V3).

## 3. Baseline, expected gains, method, stop thresholds

| Measure | Baseline (step 0) | Expected | Stop threshold |
|---|---|---|---|
| Hex facts, Goodwood board: drag fps at Fit, five steps in, labels readable, near zoom | Step 0 | 80 fps or more at each, as the Exact view | Below 50 at any after step 2: stop and report |
| Styled, Goodwood board: the same | Step 0 | 80 fps or more at each, as the Exact view | Below 50 at any after step 3: stop and report |
| Elements in the page at Fit, each view | About 89,000 (Hex facts), about 2,800 (Styled), plus 40,442 labels | Under 300 | |
| Deep wheel zoom from Fit, each view, sharp after | Step 0 | As the Exact view's (279 to 362 ms on Goodwood) | |
| How Hex facts looks | The vector layer | Pixel for pixel at level 0 on bd01, as the Exact view's tiles were checked, except the antialiased edges of hexside strokes | A difference a user can see at playing zoom |
| How Styled looks | The vector layers | Within a set tolerance a pixel at level 0 against Chrome's own drawing of the same layers (section 5), on bd01 and on Goodwood | A difference a user can see at playing zoom |

**Method.** The user's Chrome, on Goodwood's board in the board viewer (`/boards/{board}?mapMetrics=1`) with the view picker on each view, and on a Goodwood game in Play once step 1b offers the views there (behind "show my view"); `mapProbeSuite()` in the console; three runs, the first dropped; the window and display of the earlier runs (912 by 734, about 85 Hz). My browser pane checks that each step works, not its speed.

## 4. The steps

### Step 0: the baseline

The user runs `mapProbeSuite()` on Goodwood's board in the board viewer, once in each view. It gives the before figures; it decides nothing.

### Step 1: what both views can take from the Exact view at once (browser and endpoint)

- **Labels as data in every view:** `labelsAsData` takes any view's base address, so the Styled and Hex facts views fetch `labels.json` and paint one canvas, as the Exact view does. The side-by-side comparison keeps its texts.
- **The tile index for every view:** the server answers `tiles.json` for the Styled and Hex facts views with the board's lattice, so the viewport hides counters and marks out of view. Until a view has tiles, its index says so, and the viewport draws no tile group.

### Step 1b: the map style in Play and Replay (decision V5)

Play and Replay gain the view picker the board viewer has (`BoardViewPicker`), offering Exact, Styled, and Hex facts:

- **What it changes:** only how the board is drawn. The counters, LOS and range lines, marks, selection, and every rule, record, and replay are the same in every view; the counter layer is drawn over the chosen view, as the board viewer draws it.
- **Remembered** per viewer in the browser, as the map's turn is (Exact when nothing is kept), and carried in the page's address (`&style=styled`), so a link opens the same view.
- **The Comparison view** is not offered in a game (V4). **The hand-over screen's small map** keeps the Exact view.

Steps 1 and 1b go together: with step 1, a large map is playable in the other views from the start.

### Step 2: Hex facts as raster tiles, by the Map Lab's per-hex painting

A painter beside `TilePainter`, in `Maps.Rendering`:

- **Each pixel's hex** by the lattice (`TileLinePainter.OrdinalAt`), in that hex's colour from a table built once per board version: the centre terrain's colour, or white; in the trace mode, the legend's colour for where the centre's terrain came from. At levels above 0 the colour is the mean of four samples, as the Lab's painter does.
- **On top**, from the same table: a depression's hatch (the pattern's stripes in the hex's pixels); hexside terrain as a band along the edge, four cells wide (five for a cliff), in the pixels within half the width of the edge; an off-map side dashed red, 1.5 cells wide; the stairway's square, six cells, white with a black edge.
- **The grid and its centre dots** as the Exact view's tiles carry them (`TileLinePainter`, the `+g` lines).
- **The text marks** ("+1", "L2", "B") are not painted, since the server has no font: they go to the browser as data (`hexmarks.json`, a hex's centre and its mark), and the viewport paints those in view into the label canvas (decision V2).
- **Variants:** `f` and, traced, `ft`, through the same `TileCache`.

The vector `hexfacts` layer stays for the map author page, whose edits patch it, and for any page that turns tiles off.

### Step 3: Styled as raster tiles, painted by a rasterizer of our own (decision V3)

**3a. The rasterizer**, in `Maps.Rendering`, beside `PngEncoder`, managed code with no dependency:

- **Fill:** closed paths of straight and cubic Bézier segments (the curves flattened within a fraction of a pixel), the nonzero rule, antialiased by each pixel's exact coverage of the shape, as a scanline with coverage accumulation.
- **Stroke:** a centreline widened to a polygon, with round or butt caps and round joins, and dashes; filled by the same fill.
- **Paint:** a solid colour with an opacity, or a theme pattern: its elements painted once into a small bitmap at the tile's scale and repeated in board units (`patternUnits="userSpaceOnUse"`), as the SVG repeats it.
- **Compositing:** each shape over the tile in paint order, alpha blended.

**3b. The Styled painter:** walks the features in `PaintOrder`, as `WriteFeature` does, and gives each to the rasterizer from the same geometry (`Smooth`, `Sharp`, `Centerline`, the hexside endpoints), in the same colours, widths, caps, dashes, and patterns from the theme. Not by reading the SVG text: from the feature model, so nothing is parsed and the shapes are the same ones.

**3c. Into the pipeline:** variant `s`, through `TileCache`, painted ahead and on disk under the painter's version, with the grid and its dots over it; the Comparison view and the map author page keep the vector layers.

Expected cost on the server: a level-0 tile walks only the features that meet it (a bounding box a feature, kept with the model), so it costs about as much as the shapes it holds.

## 5. Tests and checks

- **Step 1:** Node viewport tests that any view's labels come as data, and that a tile index with no levels draws no tiles but places counters; a render-endpoint test that `tiles.json` answers for each view.
- **Step 1b:** component tests that Play and Replay offer the three views, load the chosen one with the counter layer over it, keep the choice across a reload and in the address, and draw the hand-over map in the Exact view; the icon coverage file for the picker; in my Studio, a step of play in each view on bd01 and on Goodwood (behind "show my view").
- **Step 2:** painter tests: each hex's colour at its centre, a hexside band in its colour and width, an off-map side's dashes, the stairway's square, the hatch, the grid and dots where wide enough; every pixel against the vector layer at level 0 on bd01, as the Exact view's tiles were checked; `TileCacheTests` for the variants; a test of `hexmarks.json`.
- **Step 3:** rasterizer tests on shapes whose coverage is known (a square on pixel bounds, a half-pixel offset, a circle's area within a tolerance, a stroke's width and caps, a dash pattern, a pattern's repeat); painter tests on bd01's features. **The look against Chrome's own drawing:** reference images of the Styled layers drawn by Chrome at level-0 scale, made once by a tool under `code-builders` with the installed Chrome's own headless screenshot (`--headless --screenshot`, no package), kept as test fixtures, as the VASL oracle's fixtures are made by a tool and not by product code (decision V6); each tile compared within a tolerance a pixel.
- **My Studio at 6670:** bd01 and Goodwood's board in each view, Fit to the closest zoom; screenshots of tiles beside vectors; the map author page unchanged.
- **The user's Chrome:** the figures of section 3. Render only, no rule, record, or replay: no merge-gate stage.

## 6. Risks

| Risk | Answer |
|---|---|
| Styled tiles look unlike Chrome's vectors (antialiasing, curve flattening, pattern phase) | Compared with Chrome's drawing at level 0 within a tolerance (section 5); screenshots judged by the user before the tiles are shown by default |
| The rasterizer is more work than its parts suggest | Built first on its own, with its tests (3a), before the painter (3b); the parts are the ones the Styled writer uses, no more |
| A theme pattern uses an element the rasterizer does not paint | The patterns' elements are listed from the shipped themes before 3a; any other is a test failure, not a silent gap |
| A hex's facts or a feature change (an edit on the author page) | The author page keeps the vector layers, as for the Exact view; elsewhere a new board version makes new tiles |
| The disk holds more tiles | Up to three more variants a board version (`f`, `ft` when traced, `s`), each about the Exact view's size, one version a board kept |
| Something reads the Hex facts groups (`h-{hex}`, `data-hex`) | Searched 2026-11-04: nothing in the Studio reads them (the other `data-hex` attributes are on Play's panels and the card editor's picked hexes) |

## 7. Decisions for the user

| Id | Question | Proposal |
|---|---|---|
| V1 | The order | Step 0, then steps 1 and 1b together, then step 2, then step 3 (3a, 3b, 3c), one pull request each |
| V2 | The Hex facts view's text marks | Data drawn in the label canvas, since the server paints no text |
| V3 | How the Styled tiles are painted | A rasterizer of our own in `Maps.Rendering` (step 3a): no dependency, the rule of T2 kept, deterministic and testable, and its tiles go through the server's cache. The others: a managed graphics library (less code, a new dependency and its licence to rule on), or tiles painted in the browser from the SVG (exact look, but the cost on the player's machine at each level's first visit, outside the server's cache) |
| V4 | The Comparison view | Unchanged: a tool to compare the Styled layers with the Exact terrain, not a view to play on; not offered in Play or Replay |
| V5 | The map style in a game | A view picker in Play and Replay (Exact, Styled, Hex facts), remembered per viewer and carried in the address; Exact when nothing is kept |
| V6 | How the Styled tiles' look is checked | Against Chrome's own drawing of the same layers, made once by a `code-builders` tool with the installed Chrome's headless screenshot and kept as fixtures; no browser in product code or in CI |

## 8. Answers to the guidelines' checklist

- **Work per state, unit, hex, or step:** none in play. Steps 2 and 3 add tile painters run once per tile and variant, ahead and on disk; the Hex facts colour table and the features' bounding boxes are built once per board version.
- **Ownership, keys, lifetime, bounds, invalidation, concurrent fills:** the same `TileCache`: one version a board in memory and on disk, the painter's version in the key, one painting per tile at a time. The marks' data and the colour table belong to the board version.
- **Disclosure, rout settlement, snapshots, pinned versions, seams:** unaffected; the views draw the board, not a game's state. The view picker changes only the board's drawing.
- **Unchanged counters reuse fragments:** unaffected.
- **Checks and measurements:** section 5; the probe and its suite work in any view.
- **Tests:** in the existing test projects; the Styled references made by a tool under `code-builders` and kept as fixtures; no new test project.
- **Benefit against noise:** three runs, the first dropped, the same window and display.
- **Results, backlog, outside outputs:** results in a section of this plan; the backlog's row "Raster tiles for the Styled and Hex facts views" updated; my Studio's build in `asl-gate-work/studio-6670`, and the reference tool's headless browser profile in `asl-gate-work`, cleaned as usual.

## 9. As built

### Steps 1 and 1b (2026-11-04, PR 119)

- **Step 1.** `labelsAsData` takes the Styled and Hex facts views' base addresses, so both fetch `labels.json` and paint one canvas; the Comparison view keeps its texts. The server answers `tiles.json` for the Styled and Hex facts views (and still no tiles for them), and the viewport takes it as the board's lattice (`latticeWanted`, `state.lattice`), from which `cullToView` hides counters and marks out of view.
- **Step 1b.** `MapStylePicker` (beside the map's rotate button in Play and Replay) offers Exact, Styled when the board has a Styled input, and Hex facts; it makes a VASL board's Styled input before showing it, keeps the choice in the browser (`studio.map.style`), and writes it into the address (`?style=`), which wins over the kept one when a page opens. The hand-over map keeps the Exact view. Play draws the Formation boundaries as a path outside the Exact view, since only the Exact view's tiles carry them.
- **Checked in my Studio at 6670** on the Goodwood game (behind "show my view"): in Hex facts and in Styled, all 548 counters, labels as data (no texts; one canvas once readable), the lattice fetched, 447 of 530 counter fragments hidden zoomed in, the boundaries as a path; the style kept across a reload (no `style` in the address) and in Replay; the hand-over map in the Exact view. The Hex facts view still holds about 92,700 elements, its vector layer, until step 2.
- **Tests:** Node viewport tests for labels as data and the lattice by view; `MapStylePickerTests` (the views offered, a choice raised, kept, and written into the address, the address over the kept choice); `RenderEndpointTests` for the index of the Styled and Hex facts views; the Play and Replay page tests.

## Related documents

- [ASL Map Studio Screen-Bounded Map Analysis](<ASL Map Studio Screen-Bounded Map Analysis.md>)
- [ASL Map Studio Map Lab Plan](<ASL Map Studio Map Lab Plan.md>)
- [ASL Map Studio Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>)
- [ASL Map Studio Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>)
- [ASL Map Studio Counter Drawing Plan](<ASL Map Studio Counter Drawing Plan.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
