# ASL Map Studio Goodwood Lab Techniques Analysis

**Date:** 2026-11-03
**Status:** analysis for the user's review; nothing is built. It answers the user's word of 2026-11-03, "In our next session, let's try to apply these improvements to the Goodwood ASL Map", and the request of the same day to read the new code, understand the final solution, and write an analysis of applying its techniques to the Goodwood ASL map. Decisions G1 to G4 (section 9) wait on the user.
**Sources:** the code on `main` at d88564ef: `boardViewport.js`, `BoardViewport.razor`, `MapLab.razor`, `Services/MapLab/LabMapEndpoints.cs` and `LabMapRenderer.cs`, `Services/TileCache.cs`, `BoundaryParents.cs`, `RenderEndpoints.cs`, `Maps.Rendering/RasterTiles.cs` and `TileLines.cs`, `Play.razor`; the [Map Lab Plan](<ASL Map Studio Map Lab Plan.md>), section 8; the [Screen-Bounded Map Analysis](<ASL Map Studio Screen-Bounded Map Analysis.md>), sections 3 to 7; the backlog's sections 96 and 97.

## 1. Summary

- **Most of the Lab's approach is already on Goodwood** under `?present=bounded`: lines painted into the tiles, every tile painted before it is needed, and the labels painted as one canvas off the page. Both maps run the same functions for these (section 2).
- **The Lab's map differs from Goodwood's in three ways:**
  - It uses one set of tiles at every zoom. Goodwood changes to other tiles and a vector grid at near zoom (decision N2).
  - Its page holds two layers, the tiles and the labels. Goodwood's page also holds every counter fragment (hidden out of view, not removed), the whole-map vector grid (hidden), the setup marks, and the figure sprite.
  - Its tiles ahead are kept in memory, Goodwood's on disk. This difference does not matter (section 5.5).
- **The Lab never measured a zoom.** Its Measure button times a wheel burst that alternates in and out, until two frames after it; it records no one-way zoom and no time until sharp. Zoom is Goodwood's open problem (backlog 96), and the Lab gives no evidence on it.
- **The remaining long frames of a Goodwood drag do not come from the near grid.** Ten steps in from Fit is level 0 but not near zoom (section 4.1), and with labels hidden that run still had a frame of 58 ms. The commit at level 0 carries the cost. The Lab's page makes the same commit with no frame over 33 ms. So the cause is most likely what Goodwood adds to that commit, but this is not yet measured.
- **Proposed** (section 6):
  1. Measure first. Run a "Lab parity" run on Goodwood: the same two-layer page the Lab draws, made by taking the counters and marks off the page for the run. It tells Goodwood's extra layers apart from the tile pipeline at Goodwood's size.
  2. Use one set of tiles at every zoom (G1), the Lab's way.
  3. Attach only the counters in view, as decision N4 first proposed, if the parity run points at them.
  4. Diagnose zoom: attribute its long frames in the probe, and run the same zoom on a Lab map of Goodwood's size (G3).
  5. Make the bounded map the default (N6) when the figures allow.

## 2. The final solution in the code

### 2.1 On the server

- **Painting a tile.** Boards are painted by `TilePainter.Png` (`Maps.Rendering/RasterTiles.cs`), which paints the terrain and then calls `TileLinePainter.Paint` (`TileLines.cs`). The Lab has its own copy, `LabMapRenderer.TilePng`, which reads its hex table. The line rule is the same in both:
  - A pixel whose hex (the hex nearest the pixel's centre) differs from the hex of the pixel to its right or below is a line pixel.
  - The line pixel takes the boundary colour where the two hexes' parents differ. Otherwise it is grid: the board's painter darkens the terrain under it to 55 percent, as the vector grid's black at opacity 0.45 does.
  - Each kind of line is left out of a level where a hex, or a formation, is under 6 tile pixels across (`MinimumPixels`, `GridMinimumPixels`). The elevation variant takes no lines.
- **Naming a tile.** A board tile's name carries its lines (`tile-te+gb-0-3-4.png`), and a tile with boundaries is keyed by the game's ownership token (`BoundaryParents`). The Lab's tile names never carry lines: the map's technique decides them.
- **Keeping tiles.** `TileCache` holds tiles in memory, least recently used first out, up to 64 MB, and one version per board. A board's tiles are also written to disk under `{CacheRoot}/tiles/{board}/{version}/{ownership or _}/`; the Lab's are not.
- **Painting ahead.**
  - The Lab (`LabMapEndpoints.PaintAhead`) starts when the page builds a tiled map. It paints coarsest level first, up to 320 tiles (all 207 of a 10,000-hex map), on half the cores, into the memory cache.
  - Goodwood starts when the viewport asks `ahead-{variant}.json` (`askAhead`), once per variant and visit. `TileCache.PaintAhead` paints every level, coarsest first, on half the cores, to disk only, so the painting never pushes the tiles in view out of memory. One painting runs per board version, variant, lines, and ownership.
- **Serving a tile.** Both answer immutable, with an ETag. A board tile not in memory is read from disk and hashed, then held in memory.

### 2.2 In the browser: what the Lab page runs

The Lab loads its maps in the Studio's own viewport (`BoardViewport` and `boardViewport.js`), at `lab-render/{key}/exact/`, **without `?present=bounded`**. So `state.bounded` is false, and of the bounded presentation the Lab runs only the label canvas, which it turns on with `CanvasLabels` (`data-labels="canvas"`).

| Mechanism | Code | Lab page | Goodwood, `?present=bounded` |
|---|---|---|---|
| Pan and zoom by a transform, a commit near the painted edge or past twice the painted scale | `flushBox`, `needsCommit` | Yes | Yes |
| Tile pool; old tiles kept under new ones until those have decoded | `updateTiles`, `tileImage` | Yes; one variant (`t`) at every zoom | Yes; `te+g` (or `te+gb`), and at near zoom `te` (or `te+b`) |
| Lines named in tile addresses | `tileLines`, `refreshTileLines` | No: lines come from the technique | Yes |
| Vector grid of the hexes in view at near zoom | `updateNearGrid`, `nearGridPath` | No | Yes |
| Whole-map vector grid | `#layer-grid` | Not sent (`tileslines` has two layers) | Fetched (about 75,000 characters) and hidden by `data-tiled` |
| Labels as one canvas painted off the page | `drawLabelCanvas` | Yes | Yes |
| Tiles painted ahead on the server | `PaintAhead` | Started by the page | Started by `askAhead` |
| Next finer level fetched by the browser | `prefetchFiner` | No | Yes, up to 48 a time |
| Counters and marks out of view | `cullToView` | None on the map | Hidden by `data-out` and a site.css rule; all stay in the page |
| Formation boundaries | | In the tiles, by technique | In the tiles (`+b`), once the page names its game (`setBoundaries`) |

So the Lab's final solution, as it runs:

- pan by transform;
- one set of tiles carrying terrain, grid, and boundaries, at every level and zoom, pixelated when level 0 is magnified (`data-sharp`);
- the labels painted off the page into one canvas;
- every tile painted before it is asked for.

The page holds the tile images in view and one canvas: 44 to 139 elements in the runs of section 8.2.

## 3. What the Lab proved, and what it did not

**Proved** (Map Lab Plan, F6, F7, and section 8.2):

- With lines in the tiles, a drag runs at the display's rate from 1,000 to 1,000,000 hexes.
- Nothing in the page grows with the map but the labels' data.
- With labels as one canvas and the tiles painted ahead, a pane-long drag on 10,000 hexes at readable zoom ran at 59.6 frames per second on a 60 Hz display, longest frame 33 ms. The user's own drags had no pauses.

**Not proved:**

- **Zoom.** `MapLab.razor`'s Measure runs `mapProbe` at Fit, five steps in, and readable zoom with a pane-long drag. Its wheel part is the probe's default, a burst that alternates in and out, and the Lab's table keeps only its time to settle. No run zoomed one way or timed until sharp (`wheel: "in"`, `sharpMs`).
- **The look of magnified tile lines.** The user's comments on the Lab were on speed.
- **Counters, marks, and LOS.** A Lab map has none.
- **An 85 Hz display.** The runs of section 8.2 were at 60 Hz, a budget of 16.7 ms a frame against Goodwood's 11.8 ms. "No long frame" is a stricter bar on Goodwood's display.
- **The frame of 183 ms five steps in** on the Lab is unexplained.

## 4. Where Goodwood's time goes now

### 4.1 Near zoom is a narrow band

Goodwood's grid is 11,700 by 12,449 cells, and a hex is 56.25 cells wide.

- The view box keeps the board's shape at every zoom, and `zoomAt` stops at a fortieth of the board's width, 292.5 cells. On a landscape pane, the height decides the scale. At the closest zoom that gives about 1.8 CSS pixels per cell at 912 by 558, 2.4 at 912 by 734, and 2.6 at 2,034 by 804.
- Near zoom (`state.near`) starts where a cell is larger than one device pixel. A wheel step zooms by 1.25, so at a device pixel ratio of 1, near zoom is only the last three or four steps.

Applied to the runs of the analysis's section 5.7, at 912 by 734:

| Run | View box, cells | CSS pixels per cell | Tile level | Near zoom |
|---|---|---|---|---|
| Ten steps in | about 1,256 by 1,336 | 0.55 | 0 | No |
| Thirteen steps in | about 644 by 685 | 1.07 | 0 | Yes, just |

At ten steps in, with labels hidden, the drag still had one long frame of 58 ms and a longest frame of 71 ms. Neither the near grid nor a change of variant was involved. The frame belongs to an ordinary commit at level 0.

### 4.2 What a commit at level 0 draws

At ten steps in, the committed box is twice the pane each way, about 3,300 by 2,700 cells. `tilesAtLevel` widens it by a tile on each side, so the pool asks for about 9 by 8 level-0 tiles: some 75 images of 512 by 512, about 1 MB each decoded. A pane-long drag commits about three times, and each commit brings two or three new columns of tiles.

The Lab at readable zoom stands at level 0 too, and its page held 97 elements, nearly all of them tile images. It had no frame over 33 ms. In the same commit, Goodwood also:

- draws the counters in view at their tier, each face a reference into the figure sprite;
- walks all 530 counter fragments and the marks to set `data-out` (`cullToView`);
- keeps the hidden whole-map grid, the legend, and the marks group in the drawing.

Which of these makes the 58 ms frame is not known. The probe cannot take the counters off the page: `setVisible` passes over the units layer, and the probe's `hide` option goes through it.

### 4.3 The deep zoom

The probe's deep zoom sends 20 wheel events in one task from Fit (level 4). The code then runs as follows:

1. One flush commits at the final box: past twice the painted scale, `needsCommit` is true. The zoom has stopped at the fortieth.
2. `updateTiles` keeps level 4 drawn for 150 ms (`tileSettleMs`). Those tiles now stand about 40 times magnified.
3. The same commit crosses into near zoom. `refreshTileLines` changes the variant from `te+g` to `te` and asks for that variant's painting ahead. The label canvas is painted, and the near grid is built.
4. About 160 ms after the burst, `updateTiles` asks for the level-0 `te` tiles of the committed box, some 16 to 25 of them. None is in the browser's cache: `prefetchFiner` fetched only level 3, the level after Fit's. They come from the server's disk.
5. At 200 ms the gesture rests (`gestureRest`) and commits again.

The Lab's approach would change only step 3: no change of variant and no near grid. Level 0 would still be fetched and decoded. So **one set of tiles at every zoom is not expected to cure the deep zoom on its own.**

The figure for the deep zoom is also out of date. The 700 ms and the two frames of 143 ms (section 5.6) were measured before the labels moved to `OffscreenCanvas` (section 5.7), which removed the forced style and layout that the first canvas caused. No deep zoom has been measured since.

## 5. The differences, one by one

### 5.1 One set of tiles at every zoom (in place of N2)

**The change.**

- `tileLines` keeps the grid at near zoom: its `grid` term loses `&& !near`.
- `updateNearGrid` draws nothing under the switch.
- `state.near` still sets `data-sharp`, as now.
- No change on the server. The `te` and `te+b` pyramids are no longer asked for, so painting ahead does one pyramid per view instead of two: 790 tiles and about 99 MB on disk, against 198 MB.

**What it should give.**

- A zoom across the near boundary asks for no second set of tiles.
- A commit at near zoom builds no grid path.
- For drags the gain is small, since near zoom is only the last few steps (4.1). It may show in a zoom that ends at near zoom.

**The look.** A level-0 tile line is one cell wide.

- At the closest zoom (1.8 to 2.6 CSS pixels per cell) a line is 2 to 3 pixels wide. The vector grid's 0.75-cell stroke is 1.3 to 1.9 pixels there.
- Pixelated, the four slanted sides of each hex step by a cell, 2 to 3 pixels. The straight top and bottom sides stay clean.
- On a display with a device pixel ratio of 2, near zoom starts about three steps earlier, and the steps are the same size in CSS pixels.

**Centre dots.** The tile grid has none; the vector grid draws one per hex at opacity 0.6. Under `?present=bounded` the whole-map grid is hidden at every zoom, so the dots are already missing at every zoom, not only at near zoom as the backlog's row says. They could be painted into the tiles at the levels where a hex is wide enough to show one (24 tile pixels or more: levels 0 and 1). That costs the painter a few pixels per hex and the browser nothing (G2).

**If the look is refused**, two fallbacks:

- Keep N2's vector grid at near zoom, but over the `te+g` tiles. There is then no change of variant and still one pyramid. The tile line lies within a cell of the vector line, so the lines read slightly darker.
- Keep N2 as it is.

### 5.2 Layers the Lab's page does not hold

- **Counters.**
  - What is built: Goodwood has 530 fragments (4,548 elements in the baseline). All of them stay in the page, and those out of view are hidden by `data-out`. Decision N4 proposed attaching only those in the committed box. The build hides them instead (analysis, 5.4), and that change was not recorded as a deviation.
  - What it costs: hidden elements are neither laid out nor painted. But every commit walks all 530, and a change of tier (`data-active-tier` on `#layer-units`) restyles every fragment. Patches would go to the held fragments, attached or not, as section 3.4 of the analysis set out.
  - Proposed: measure first (section 6, step 1). Build attachment by view only if the measure points at the counters.
  - **Since measured on the Lab (2026-11-03; Map Lab Plan, section 8.3, F8):** the Lab now places catalog counters, and the user found its drags good with 1,000 counters and slightly laggy at 2,000, every counter drawn. Goodwood holds about 530 fragments and hides those out of view, so counters alone are unlikely to explain its 58 ms frames at level 0. This points at what else differs: the map's size (the 10,000-hex Lab map is about a quarter of Goodwood's 40,442 hexes, so a level-0 commit there meets fewer tiles) and the 85 Hz display. Two caveats: the finding is the user's feel, not the probe's figures, and the Lab's counters are infantry only, where Goodwood's may include vehicles with more figure art. The next run (section 6, steps 1 and 4, and G3 in one): a Lab map of Goodwood's size, `tileslines`, labels shown, about 530 counters, measured with the Measure button.
- **The whole-map vector grid.** Under `?present=bounded` the Exact view still fetches it and keeps it hidden. It costs little per frame, but the page holds work that grows with the map.
  - Proposed: under the switch, the Exact view does not fetch it. The Comparison view, whose side view clones `#layer-grid`, keeps it.
- **Setup marks.** These are markup, 707,155 characters during setup, hidden out of view. They matter only while a side sets up, and the Lab says nothing about them. They stay in the backlog (N3), waiting on a measured cost.
- **Formation boundaries.** These are already in the tiles, as in the Lab. With G1 the near variant `te+b` goes as well.
- **The figure sprite, LOS, and highlight.** These are small and drawn on demand. No change is proposed.

### 5.3 The default (N6)

The gate set in the analysis's section 5.6: the bounded map becomes the default when the user's Chrome shows it at least as good everywhere, zooms included. The Lab gives no evidence on zooms, so N6 waits on section 5.4 here.

### 5.4 Zoom

The Lab's approach does not reach zoom (4.3), so zoom needs its own diagnosis. The order:

1. **Measure the current build again:** the deep zoom and the few steps near playing zoom, three runs each, with the first dropped.
2. **Attribute the long frames.** The probe records each Long Animation Frame's scripts (`invoker`, `sourceFunctionName`, `duration`), not only their count and forced layout. A frame with no script time points at painting or decoding, which only a trace in the user's Chrome shows.
3. **Run the same zoom on a Lab map of Goodwood's size** (40,442 hexes, `tileslines`, labels shown; G3).
   - If the Lab map stalls too, the cause is in the tile pipeline the two share.
   - If it does not, the cause is in Goodwood's other layers.

Candidates once the cause is known. None is proposed yet.

- Fetch the destination level's tiles during the wheel gesture: the box the zoom is heading to is known from its first events.
- Shorten the 150 ms hold for a zoom that goes one way.
- Ask first for the tiles of the visible box, before those of the margin.

### 5.5 What is not a difference

- **Tiles ahead in memory or on disk.** One Goodwood variant (about 99 MB) is larger than the 64 MB memory bound, so disk is the right place for it. Reading a tile from disk is a file read and a hash, small beside a paint of 0.3 to 0.4 s. A warm start after a restart is still not measured (backlog 96).
- **The browser's next-level fetch.** It is Goodwood's only, it does no harm, and it stays.
- **The label canvas.** Both maps run the same function.

## 6. Proposed order

| Step | Work | Measured by | Done when |
|---|---|---|---|
| 0 | The probe gains options to take the counters and the marks off the page for a run and put them back, and records each long frame's scripts | Node tests for the options; one probe run in my Studio as a check, not a measure | The options work and restore the page |
| 1 | **Measure.** The current build again: a one-pane drag at ten and thirteen steps in, labels shown and hidden; the deep zoom; a few wheel steps near playing zoom. Then the Lab parity run: the same runs with the counters and marks off the page. Three runs each, first dropped | The user's Chrome at 912 by 734 | It is known whether Goodwood's extra layers make its long frames |
| 2 | One set of tiles at every zoom (G1), behind `?near=tiles` within `?present=bounded`; the centre dots in the tiles if G2 is approved | The step 1 runs at thirteen steps and the deep zoom; screenshots at the closest zoom at 912 pixels and at the user's width | The user judges the look; the figures are recorded |
| 3 | Only the counters in the committed box attached (N4 as proposed), if step 1 points at them | The step 1 runs | Long frames at level 0 are under the stop threshold, or the step is reported as not enough |
| 4 | Zoom, from the attribution of step 1 and the Lab map of Goodwood's size (G3) | The deep zoom, three runs | Under the threshold of section 7 |
| 5 | The Exact view does not fetch the whole-map grid under the switch | Elements and bytes at load | Done with step 2 or 3 |
| 6 | The bounded map as the default (N6), the old presentation kept behind a switch for a while | Every row of section 7 | The user's word |

Steps 0 and 1 change only the probe, and they come first: they decide whether steps 3 and 4 are needed and what step 4 is.

## 7. Baseline, expectations, method, and stop thresholds (for the plan)

**Baseline:** the current build (`main` at d88564ef, `?present=bounded`), measured again in step 1. The figures of the analysis's sections 5.6 and 5.7 stand as reference until then.

| Measure | Now (analysis, 5.6 and 5.7) | Expected | Stop threshold |
|---|---|---|---|
| One-pane drag, ten and thirteen steps in, labels shown | 66 to 67 fps; longest frame 71 ms; two or three long frames | 80 fps or more; no frame over 35 ms (three display frames at 85 Hz) | The parity run (step 1) still has frames over 50 ms: stop and report, since the cause then lies outside the layers the Lab lacks |
| The same, labels hidden | 72 to 74 fps; longest frame 71 to 82 ms; one long frame of 58 ms | As above | As above |
| Deep wheel zoom from Fit, sharp after | about 700 ms; two frames of 143 ms (before `OffscreenCanvas`) | Under 450 ms, of which about 220 ms is the probe's own wait; no frame over 50 ms | Step 4's first change leaves it above the old presentation's 653 ms: stop and report |
| A few wheel steps near playing zoom, sharp after | 356 to 394 ms | At or under the old presentation's 253 to 288 ms | Above 394 ms after step 4: stop and report |
| Elements in the page at playing zoom | To be measured in step 1 | Tiles in view, the label canvas, and the counters in view | |
| Tiles on disk per board version | 198 MB (two variants) | About 99 MB with G1 | |

**Method.**

- Each figure is measured by the user in Chrome, with the probe, on the Goodwood reference, in the Allied view, at 912 by 734, on the same display.
- Three runs each, with the first dropped (the Map Lab Plan's rule; the backlog's N6 row asks for three).
- Each map is compared with its own earlier runs, never with the other map: the Lab's runs were on a 60 Hz display at another size.
- The Lab map of Goodwood's size (G3) is measured on the Lab's display, against itself.

## 8. Risks

| Risk | Answer |
|---|---|
| Tile lines at the closest zoom look worse than the vector grid | Behind its own switch first; screenshots at 912 pixels and at the user's width; the fallbacks of 5.1 |
| The tiles of the abandoned near variant stay on disk until the board's version changes | They are removed with the version folder, as now; or step 2 removes them once, if the user prefers |
| Counters attached by view miss a patch or lose keyboard focus | Patches go to the held fragments, attached or not (analysis, 3.4); a counter out of view cannot take focus today either (analysis, 5.4) |
| The parity run is not the Lab's page in every way (its terrain, its 85 Hz display) | It is compared with Goodwood's own runs, which is the question it answers; the Lab map of Goodwood's size answers the other |
| The Lab is used for a question about Goodwood | The Map Lab Plan allows a part whose cost at scale is unknown to be tried there first, at the user's word (G3) |

## 9. Decisions for the user

| Id | Question | Proposal |
|---|---|---|
| G1 | The grid at near zoom | One set of tiles at every zoom, the Lab's way, behind `?near=tiles` until the user has judged the look at the closest zoom; N2's vector grid over the `te+g` tiles as the fallback (5.1) |
| G2 | Centre dots | Painted into the tiles at levels 0 and 1, so the bounded map shows them where the vector grid did (5.1) |
| G3 | A Lab map of Goodwood's size as a bench for the zoom question | Yes: 40,442 hexes, `tileslines`, labels shown, a one-way wheel zoom timed until sharp (5.4) |
| G4 | The order | Measure first (steps 0 and 1), then build only what the measures point at (section 6) |

## 10. Answers to the guidelines' checklist

- **Work added per state, unit, hex, or step:** none in play.
  - Per commit, G1 removes the near grid's build.
  - Counters by view (step 3) would make the commit's walk follow the counters near the view, not all 530.
  - The painter's dots (G2) add a few pixels per hex to a tile's paint, at levels 0 and 1 only.
- **Ownership, keys, lifetime, bounds, invalidation, concurrent fills:** `TileCache` as now: one version per board in memory and on disk, a tile painted once while several ask, tiles with boundaries keyed by the ownership token. G1 retires a variant; it adds no key.
- **Disclosure, rout settlement, snapshots, pinned versions, seams:** unchanged.
  - Tiles carry terrain, the grid, and the game's Formation boundaries, which the page shows either side today.
  - Counters attached by view change only which held fragments are in the page; the fragments are still built from the side's view on the server.
  - No rule, record, or replay reads any of this.
- **Unchanged counters reuse fragments:** yes. No change on the server, and patches still apply to the held fragments.
- **Checks and measurements:**
  - The Node viewport tests in `boardViewport.pointer.test.mjs` change where behaviour changes: "bounded tiles ask for the grid and the boundaries the view shows" for G1, and "the near grid outlines only the hexes about the box" retired with N2 under the switch.
  - New tests cover the probe's options.
  - The probe's runs of section 7.
- **Tests:** no new test project. A test for the dots paints one small tile.
- **Benefit against noise:** three runs each, first dropped, the same pane and display.
- **Results, backlog, outside outputs:** results go into a section of the plan, and into the analysis's section 5. Rows to correct in the backlog's section 96: the centre dots (missing at every zoom under the switch, not only near zoom), and N4 (built as hiding, not as attachment). My Studio's build in `asl-gate-work/studio-6670` is registered and cleaned as usual.

## Related documents

- [ASL Map Studio Screen-Bounded Map Analysis](<ASL Map Studio Screen-Bounded Map Analysis.md>)
- [ASL Map Studio Map Lab Plan](<ASL Map Studio Map Lab Plan.md>)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>)
- [ASL Map Studio Pan by Transform Plan](<ASL Map Studio Pan by Transform Plan.md>)
- [ASL Map Studio Counter Layer Design](<ASL Map Studio Counter Layer Design.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
