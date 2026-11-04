# ASL Map Studio Tile Pacing Plan

**Written:** 2026-11-03, at the user's word ("OK, we can pursue this") on the two candidates of the [Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>), section 5.6. This plan is the second: new tiles shown a few a frame, and the tiles a zoom heads for fetched while it is still under way. The first is the [Counter Drawing Plan](<ASL Map Studio Counter Drawing Plan.md>).
**Status:** approved by the user on 2026-11-03 ("I've reviewed both plans and I approve all proposal in both plans"), with decisions T1 to T3 as section 6 proposes them; then "Execute and work independently until all the code is implemented and I can see a demo and perform tests". Built on branch `claude/speed-plans` (section 8).
**Sources:** the analysis's section 5.6 (the Goodwood suites, the deep-zoom breakdown, and the trace); `updateTiles`, `tileImage`, `prefetchFiner`, and `zoomAt` in `boardViewport.js` on `main`.

## 1. The problem

Two costs remain when a zoom lands, both independent of the counters (the "none" runs have them too):

- **A long commit when new tiles are shown.** `tileImage` fetches a tile, decodes it off-screen, and points the tile's image at it as soon as it is decoded. After a big zoom some 16 to 25 new level-0 tiles of 512 by 512 pixels become ready within a few frames and are all shown together. The trace has a commit of 295 ms (traced; about half that untraced) in the task where they were shown, with no other work in it and no other thread busy: the main thread waited, most likely on the images reaching the GPU together.
- **A wait for sharp tiles.** After the zoom rests, the destination level's tiles are fetched only then: the browser fetches ahead only the next finer level of the one in view (`prefetchFiner`), and a deep zoom from Fit skips four levels. The map takes 370 to 941 ms to sharpen, about 220 ms of it the probe's own wait.

## 2. Baseline, expected gains, method, stop threshold

| Measure | Baseline (analysis, 5.6) | Expected | Stop threshold |
|---|---|---|---|
| Deep zoom from Fit, longest frame, no counters (Goodwood suite, "none") | 96 to 114 ms | Under 50 ms after step 1 | Above 80 ms after step 1: stop and report |
| The commit when the new tiles are shown (trace) | 295 ms (traced) | Under 60 ms (traced), spread over several frames | |
| Deep zoom from Fit, sharp after | 370 to 941 ms ("none"); 666 to 699 ms in the single runs | Under 450 ms after step 2, of which about 220 ms is the probe's wait | Above 600 ms after step 2 |
| A one-pane drag over painted ground, sharp after | 34 to 47 ms (Screen-Bounded Map Analysis, 5.6) | Unchanged | More than 30 ms later: lower the pacing's hold for small batches |
| Pane-long drags, fps | 59 to 85 by run (analysis, 5.6) | Unchanged or better | Worse than the baseline by more than 5 fps: undo the step |

**Method.** As the Counter Drawing Plan's: `mapProbeSuite()` on Goodwood's game map in the user's Chrome at 912 by 734, three runs, the first dropped; one trace of a deep zoom; the Map Lab's Measure on the 40,392-hex map, which has the same tile pipeline without Goodwood's other layers. Each step measured before the next.

## 3. The steps

### Step 1: show new tiles a few a frame

`tileImage` puts a decoded tile in a queue instead of showing it at once. Each animation frame shows at most a set number from the queue (to be set by measure; four to start), those nearest the middle of the part of the board on screen first, so the tiles a user looks at sharpen first and the margin last. The coarse tile and the old level stay under the new ones until they are shown, as now, so the map never shows a gap. A batch of no more than the set number is shown at once, so a drag over painted ground (two or three tiles at a time) is not slowed.

The probe records, per run, the most tiles shown in one frame and the frames the batch took.

### Step 2: fetch the destination while the zoom is under way

`zoomAt` already knows, at each wheel event, the box the gesture will end at. During a gesture whose box has moved past the next finer level, the viewport asks for that box's tiles at the level the box calls for: fetched and decoded at low priority, at most 48 a gesture, each once, and kept decoded until they are shown or the gesture leaves them behind. When the gesture rests, `updateTiles` finds them ready, and step 1 shows them a few a frame.

This adds to `prefetchFiner`, which stays as it is for zooms of one level.

## 4. Tests and checks

- **Node viewport tests**: the queue's order (nearest the middle first), its bound per frame, a batch under the bound shown at once, a tile dropped from the pool leaving the queue; the destination box and level computed from a wheel burst; at most 48 fetches a gesture, each once.
- **My Studio at 6670**: the Goodwood game map (behind "show my view") and the Lab, a deep zoom from Fit and back, drags at each zoom, a layer switch (terrain, elevation, Formation boundaries) that changes the tile variant; the map never shows a gap while tiles come in.
- **The user's Chrome** for the figures of section 2; render only, so no merge-gate stage.

## 5. Risks

| Risk | Answer |
|---|---|
| A paced batch makes a drag sharpen later | Batches no larger than the bound are shown at once; the drag's sharp-after figure is a stop threshold |
| The fetches during a zoom take bandwidth from the tiles in view | Low priority, at most 48 a gesture, and none until the box has passed the next finer level |
| Decoded tiles held for a zoom that turns back use memory | At most 48, released when the gesture rests or leaves them behind; a decoded 512-pixel tile is about 1 MB |
| The long commit has another cause than the images together | Step 1's trace shows it; the stop threshold then applies, and the cause is reported |

## 6. Decisions for the user

| Id | Question | Proposal |
|---|---|---|
| T1 | How many tiles a frame | Set by measure, four to start; the bound is one constant |
| T2 | Step 2 | After step 1 is measured, since it changes the wait, not the long frame |
| T3 | The order against the Counter Drawing Plan | This plan's step 1 first: it is smaller, it acts on every map (counters or none), and the "none" runs measure it cleanly; then the Counter Drawing Plan's step 1 |

## 7. Answers to the guidelines' checklist

- **Work per state, unit, hex, or step:** none on the server. In the browser, a queue of decoded tiles and, during a zoom, up to 48 low-priority fetches.
- **Ownership, keys, lifetime, bounds, invalidation, concurrent fills:** the queue belongs to the tile pool and is cleared with it; the prefetched images are held by the gesture and released at its rest; the server's tile cache and its disk are unchanged.
- **Disclosure, rout settlement, snapshots, pinned versions, seams:** unaffected; tiles carry terrain, the grid, and the game's Formation boundaries, as now.
- **Unchanged counters reuse fragments:** not touched.
- **Checks and measurements:** section 4; the probe gains the per-frame tile count.
- **Tests:** Node tests in the existing viewport test file; no new test project.
- **Benefit against noise:** three runs, the first dropped, the same window and display; a trace for the commit.
- **Results, backlog, outside outputs:** results in a section of this plan and the analysis; the backlog's rows "New tiles shown a few a frame" and "The deep zoom of the screen-bounded map" updated; my Studio's build in `asl-gate-work/studio-6670`, cleaned as usual.

## 8. As built (2026-11-03)

Both steps built on branch `claude/speed-plans`, in `boardViewport.js`, at the user's word to work through to a demo; the step-by-step measures in the user's Chrome wait for that demo.

- **Step 1.** A decoded tile is queued (`queueTile`); each animation frame shows at most `tilesPerFrame` (4) of the queue, those nearest the middle of the board on screen first (`pacedBatch`), and all of it when no larger. A tile dropped from the pool while it waited is not shown. `?pace=0` shows each tile as it decodes, as before, for comparison. The probe records per drag and per wheel burst the frames that showed new tiles and the most in one (`tilesShown`); `mapProbeSuite()` prints them.
- **Step 2.** During a wheel gesture, once the box has passed the next finer level than the one drawn, the destination level's tiles for the part of the board on screen are fetched and decoded at low priority, at most 48, each once (`destinationTiles`, `prefetchDestination`), in the variant the landing will draw (at near zoom the near grid's, unless `?near=tiles`). `tileImage` takes a tile so fetched instead of fetching it again; what the rest leaves is let go. Only on the screen-bounded map, as `prefetchFiner`.
- **Checked in my Studio at 6670** on the Goodwood game map (behind "show my view"), one deep wheel zoom from Fit, my browser pane (a pointer only, Map Lab Plan, section 3.5): the 20 level-0 tiles were each fetched once, in the near variant, and shown over six frames, at most four in one. Sharp after 342 ms with a longest frame of 58 ms, against 411 to 446 ms and 133 to 152 ms in the same pane with `?pace=0&counters=three`.
- **Tests:** Node viewport tests for the batch's order and bound, a small batch at once, and the destination's level, tiles, and bound.

## 9. Results in the user's Chrome (2026-11-03)

One `mapProbeSuite()` on Goodwood's game map (behind "show my view") with this plan and the Counter Drawing Plan's step 1 on, and one with both off (`?pace=0&counters=three`), at about 85 Hz; then the Map Lab's Measure on the 40,392-hex map with 600 counters, each way, at 1,295 by 735. One run each, so the figures are pointers to be repeated; the two plans' changes were on and off together.

**The deep zoom from Fit** (the probe's longest frame counts frames over 50 ms; 0 is none):

| Map, counters | On: sharp after, longest frame | Off: sharp after, longest frame |
|---|---|---|
| Goodwood, out of view hidden (the default) | 279 to 313 ms, none | 447 to 581 ms, 96 to 99 ms |
| Goodwood, none drawn | 360 to 362 ms, none | 512 to 560 ms, 91 to 103 ms |
| Goodwood, all drawn | 621 to 631 ms, 130 to 161 ms | 590 to 595 ms, 98 to 104 ms |
| Lab, out of view hidden | 497 ms, 59 ms | 815 ms, 94 ms |
| Lab, none drawn | 537 ms, 59 ms | 695 ms, 47 ms |
| Lab, all drawn | 663 ms, 71 ms | 834 ms, 82 ms |

- **Against section 2's thresholds:** on Goodwood, with no counters, the landing frame went from 96 to 114 ms (and 91 to 103 here) to none, under the 50 ms expected; the map sharpens in 360 ms, under the 450 ms expected. With counters out of view hidden, the same. The Lab, with its larger, detailed counters, sharpens 150 to 300 ms sooner.
- **With every counter drawn on Goodwood** the landing frame was longer with the changes on (130 to 161 ms against 98 to 104), while on the Lab it was shorter (71 against 82). That mode is drawn only with `?present=classic`; it is left to a repeated run.
- **Drags** at readable and near zoom are unchanged within the runs' spread on Goodwood (59 to 72 frames per second each way), and slightly better on the Lab (67.8 to 70.8 against 60.9 to 69.9, counters in view or none): section 2's drag threshold holds.
- The Fit runs, the first of each Measure, swung from 1.8 to 80 frames per second both ways on the Lab, and are not read.

## Related documents

- [ASL Map Studio Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>)
- [ASL Map Studio Counter Drawing Plan](<ASL Map Studio Counter Drawing Plan.md>)
- [ASL Map Studio Screen-Bounded Map Analysis](<ASL Map Studio Screen-Bounded Map Analysis.md>)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
