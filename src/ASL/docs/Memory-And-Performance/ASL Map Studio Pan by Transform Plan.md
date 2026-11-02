# ASL Map Studio Pan by Transform Plan

**Written:** 2026-11-02, at the user's word: "Yes, plan B3", after the measurements of the analysis's section 12 showed that neither counters by view (B6) nor the grid in tiles would make a zoomed-in drag smooth.
**Source:** `ASL Map Studio Large Campaign Map Analysis.md` (the analysis): option B3 (section 6, group B), its first trial and removal (section 10.2), the raster tiles (section 11), and the measurements of section 12.
**Scope:** B3 for every map in the shared viewport: during a drag or a wheel burst the map already painted is moved and scaled by a compositor transform, and the view box is committed, with one repaint, when the gesture rests or nears the edge of what was painted. One branch, one pull request. Not in scope: B6, the grid in tiles, C1 to C4, D1, D2.
**Status:** approved by the user on 2026-11-02 ("Approve as proposed"), with decisions P1 to P4 as section 7 proposes them. Built on branch `claude/pan-by-transform`: tasks 0 to 5, with the deviations of section 9. In the user's Chrome a Goodwood drag runs at 83 to 85 frames per second at Fit and 77 to 80 at playing zoom (analysis, section 13). The merge waits for a green CI run and the user's word.

## 1. Goal and expected gains

Make a drag smooth at the zoom a game is played at, by not repainting the map while it moves. Nothing a page shows at rest changes, and no rule, record, or replay changes.

Stated up front (the user's rule of 2026-10-19 for performance plans). Every figure is the user's Chrome 156 at 2,033 by 803, Goodwood, tiles on (section 12 of the analysis):

| Measure | Baseline at Fit | Baseline at playing zoom | Expected after this plan | Stop threshold |
|---|---|---|---|---|
| Drag: frames per second | 24 to 25 | 9.5 to 13.6 | 50 or more at both zooms | Below 30 at playing zoom after task 3: stop and report |
| Drag: median frame | 35 ms | 71 to 94 ms | under 20 ms | |
| Drag: longest frame | 71 to 82 ms | 235 to 283 ms | under 50 ms within the painted margin | Above 150 ms: stop and report |
| Settling at the end of a drag (pointer up to the repainted frame) | not measured | not measured | under 300 ms: one repaint of a layer four times the pane's area, where one repaint of the pane alone costs 35 ms at Fit and 71 to 94 ms at playing zoom, so it may not scale evenly (risks, and decision P1) | Above 500 ms: stop and report |
| A burst of 20 wheel events, to settled | 58 to 75 ms | not measured | under 200 ms, the commit included | Above 400 ms |

The basis: paint flashing in the user's Chrome shows the whole map area repainted on every drag frame, and the long-frame breakdown puts 45 to 107 ms of each long frame in style, layout, and paint, with 0 to 13 ms of script (analysis, section 12). A compositor transform moves an already painted layer without painting it. The first trial of B3 (Large Campaign Map Plan, task 6; analysis, 10.2) measured worse only in the Claude pane, which has a floor of about 11 frames per second even for an empty map (Raster Tiles Plan, deviation D5); it was never measured in an ordinary browser.

An open point the measurements leave: at playing zoom an empty map still dragged at about 22 frames per second, three times slower than at Fit. A drag frame today is one repaint of the pane, so the repaint's cost at each zoom is known (35 ms at Fit, 71 to 94 ms at playing zoom); what is not known is the cost of repainting the larger layer at a commit, which task 2 measures before task 3 builds on it.

## 2. How it works

### 2.1 Two boxes

The viewport keeps two boxes instead of one:

- **The committed box:** what the SVG's `viewBox` holds and what was last painted. It is wider than the pane by a margin on every side (section 2.2).
- **The view box:** what the user is looking at, moved by every pointer and wheel event, as `state.box` is today.

While a gesture runs, the SVG gets a CSS transform that maps the committed drawing onto the view box: a translation for a drag, a scale about the pointer for a wheel step. The `viewBox` attribute is not touched, so nothing is painted again. When the gesture rests, the committed box becomes the part of the board on screen (`visibleBox`, which counts the letterbox's strips) plus its margin, so the larger SVG's aspect matches its box and nothing is letterboxed inside it; the `viewBox` is written once, the transform is cleared, and the browser repaints once.

The transform reaches the SVG as the first trial set it (Large Campaign Map Plan, task 6): through one custom property, `--gesture`, which the stylesheet's rules for the unturned and the turned map both read, since `site.css` keeps viewport state out of style attributes ("Viewport state is set by boardViewport.js without creating style attributes", line 1838) and a written `transform` would override the turned map's rule.

### 2.2 The painted margin

A moved layer shows only what was painted. The SVG is laid out larger than the pane, by a margin of half the pane on each side (decision P1), inside a host that clips it, so a drag of up to half a pane shows painted map, not a blank strip. The layer is twice the pane each way: at 2,033 by 803 and device pixel ratio 1, 4,066 by 1,606 pixels, about 26 MB of the graphics card's memory (four bytes a pixel); twice that ratio, four times the memory.

### 2.3 When the view is committed

- At the end of a drag (pointer up) and 150 ms after the last wheel event, the same rest the tiles use.
- During any gesture, when the part of the board on screen comes within a tenth of the pane of the painted margin's edge, so no blank strip shows: one repaint, then the gesture goes on by transform. This one rule covers a long drag and a zoom out alike; a zoom about a pointer away from the centre reaches one edge before any fixed scale would.
- During a wheel burst, when the scale since the commit passes 2: zooming in further would blur the painted layer.

### 2.4 What follows the commit, not the frame

Labels in view, tiles in view, and the counter tier are chosen at the commit from the committed box, which includes the margin, so they are already drawn when a drag moves them into view. During a gesture `flushBox` writes only the transform.

### 2.5 Pointer conversion

`toBoard` turns a pointer into board units from the drawing's rectangle and size, the view box, and the letterbox (`boardViewport.js:1261-1280`). Today the rectangle is the SVG's (`takeRect`) and the size is the SVG's, from the size observer (`state.size`, line 51) or `paneSize`. Once the SVG is larger than the pane, all three read the host instead, and `toBoard` keeps using the view box, never the transformed SVG, so a click during or right after a gesture lands on the hex under the pointer. `fitBox`, `visibleBox`, and the tile and label choices take the same host size.

### 2.6 Rotation and side by side

The turned map (pass 29) is itself a CSS transform on the SVG (`site.css`, `.board-host.rotated > .board-svg`), which also sizes the SVG from the host (`100cqh` by `100cqw`). The turned rule reads `--gesture` before its own turn, as in the first trial, and its sizes grow by the margin like the unturned rule's; the pointer test of a turned map (`boardViewport.pointer.test.mjs`) covers it. The side-by-side comparison's second SVG gets the same property and the same commit.

### 2.7 Which pages

Every page through the shared viewport: Play, Replay, the board viewer (every view, the comparison and side by side included), the hand-over map, the card map picker, and the map author page. On the author page a drag with the move tool, and the clicks of the drawing tools, do not pan, so they keep their present path; only its pan and zoom go by transform. `?transform=0` restores the present path on any page, for comparison and measurement, read from the address or the address the page was opened with, as `?tiles=0` is (`tilesOffInAddress`), since Replay rewrites its address.

## 3. What changes for a viewer

At rest, nothing. During a drag, the map moves under the pointer with no repaint; a drag longer than half a pane repaints once on the way. During a wheel zoom in, the map is briefly softer until the burst rests and repaints sharp (about 150 ms); zooming out shows painted map to the margin's edge.

## 4. Tasks

### Task 0: branch

A branch from an up-to-date `main`. The Goodwood working copy `asl-gate-work/ref-run-head` is the measured map; `dl-tractor-works-02` and the Gambit card are the checks of a single board and a joined map.

### Task 1: the probe

The probe gains the settling time after a drag (pointer up to the first frame after it) and a `zoom` option that sets the view to a given number of zoom steps from Fit before measuring, so the user's runs at Fit and at playing zoom repeat exactly. The user runs it before any change, for the settling baseline at both zooms. Tests: the Node test of the new record fields.

### Task 2: drag by transform

The two boxes, the margin, `--gesture` during a drag, the commit at pointer up and near the margin's edge, labels and tiles chosen at the commit, `toBoard` and the sizes from the host, rotation, side by side, and `?transform=0`. With it, the tile group's sharp-cell switch moves from a style attribute to a data attribute and a stylesheet rule: PR 104 wrote `group.style.imageRendering` (`boardViewport.js:419`), against the same convention of `site.css` line 1838. Tests, named by method in the Node file: the transform from the committed box to the view box (`gestureTransform`), the commit rule near the edge (`needsCommit`), a click after a drag landing on the hex under the pointer, letterboxed and turned. Then the user's Chrome measures the settling after a drag at both zooms: if it is above 500 ms with the half-pane margin, the margin drops to a quarter (decision P1) before task 3.

### Task 3: wheel zoom by transform

The scale about the pointer, the commit at rest, near the margin's edge, and past a scale of 2. Tests: the scale transform, a zoom out about an off-centre pointer committing at the near edge, and a zoom in committing past 2. Then the user's Chrome measurement at Fit and at playing zoom: **the stop thresholds of section 1 apply here.**

### Task 4: the Studio check

Every page of section 2.7, turned and unturned, at Fit and at playing zoom, on Goodwood, `dl-tractor-works-02`, and the Gambit card: a drag inside the margin, a drag past it, a wheel burst, a click right after each, the layer switches, and `?transform=0`. The Claude pane checks behaviour only, never speed.

### Task 5: measure and record

The user's Chrome runs as in task 3, three each. Results, limits, and backlog rows go in the analysis as section 13; durations in the time log by `timelog_row.py`.

### Task 6: CI and pull request

No rule, planner, projector, or record changes, so no replay proofs (the user's standing rule of 2026-11-01). The gate's `strict` stage, the Node viewport tests, then the pull request and a green CI run. The merge waits for the user's word. Outside outputs are cleaned at the end.

## 5. Risks

| Risk | Answer |
|---|---|
| Chrome paints the transformed layer again each frame, as the pane did | Measured in the user's Chrome at task 3, against the stop thresholds; `will-change: transform` only for the gesture's length |
| The commit's repaint of a layer four times the pane's area stalls the end of a drag | Measured after task 2, before task 3 builds on it; the settling threshold of section 1; a quarter-pane margin (decision P1) if it stalls |
| A blank strip shows during a fast drag | The commit near the margin's edge; the margin can grow if a tenth of the pane is too late |
| Soft drawing while zooming in | Lasts until the burst rests (150 ms) or the scale passes 2 |
| A click lands on the wrong hex during or after a gesture | `toBoard` reads the view box and the host's rectangle and size; the Node pointer tests, letterboxed and turned |
| The turned map's transform and the pan transform fight | Both rules read `--gesture` before their own transforms, as in the first trial; the turned pointer test and the Studio check |
| The first trial's result (analysis, 10.2) repeats | That trial moved the pane-sized SVG through `--gesture`, accepted blank strips between commits every 250 ms, and was measured only in the pane. This one paints a margin so no strip shows, commits on rest and near the edge, and is judged in the user's Chrome only |

## 6. Out of scope, for the backlog

When the plan is approved, these join the backlog's section 96: B6 and the grid in tiles stay deferred until this plan's measurement shows whether they still matter; the empty map's slowness at playing zoom, if task 1's measurement does not explain it.

## 7. Decisions for the user

| | Question | Proposal |
|---|---|---|
| P1 | The painted margin | Half a pane on each side (a layer twice the pane each way); a quarter if the settling measured after task 2 is above 500 ms |
| P2 | Which pages | Every page through the shared viewport, the author page's pan and zoom included, with `?transform=0` to compare |
| P3 | When to commit | At rest (pointer up, 150 ms after the last wheel event), near the margin's edge (drag or zoom out), and past a scale of 2 (zoom in) |
| P4 | B6 and the grid in tiles | On hold until this plan's measurement |

## 8. Checklist

Answers to the [guidelines'](<ASL Memory and Performance Guidelines.md>) checklist:

- **Work added per state, unit, hex, or step:** none on the server. In the browser: one transform write per gesture frame in place of a `viewBox` write and a repaint; one commit per gesture.
- **Ownership, keys, lifetime, bounds:** the committed box lives with the loaded board and is replaced by the next load; the painted layer is bounded by the margin (four times the pane's area).
- **Disclosure, snapshots, pinned versions, seams:** nothing changes what a viewer sees at rest; seams draw as before.
- **Counters:** the fragments, patches, and tiers are unchanged; the tier is chosen at the commit.
- **Checks and measurements:** the probe before and after, at Fit and at playing zoom, in the user's Chrome; the Node tests; the Studio check of every map page.
- **Tests:** Node tests only; no new test project.
- **Benefit and noise:** section 1's thresholds, three runs each, at the same window size.
- **Records:** results, limits, backlog rows, time log, and outside-output cleanup before the pull request.

## 9. Deviations found while building

| | What changed | Why |
|---|---|---|
| E1 | No Node test of the probe's new record fields (`drag.settleMs`, `zoom`) | The probe dispatches real pointer and wheel events and waits on animation frames; the Node fixture fakes neither. The fields were read in the Studio and in the user's Chrome runs |
| E2 | Side by side, pan by transform rests: the drawing is the pane's and the view box is the view | Side by side lays two drawings in a row in the host; the margin's absolute layout would break it. A Node test covers the switch |
| E3 | The script writes the drawing's `transform` itself during a gesture, not the custom property `--gesture`; `gestureCss` repeats the turned map's own transform before the move | With `--gesture` the user's Chrome still ran 13 to 16 frames per second at playing zoom, with 41 to 130 ms of each long frame in style, layout, and paint: a custom property inherits, so changing it restyled the whole map. Writing `transform` alone: 77 to 80 frames per second |
| E4 | The site.css rules hold only the at-rest transforms; the stylesheet is fingerprinted at build, so a CSS change needs a Studio build to be seen (the script is served from source) | Found when the first Studio check showed the old stylesheet |
| E5 | The rest after a wheel burst is the viewport's existing 200 ms (`gestureRest`), not 150 ms | One rest for every gesture, as the first wave set it |
| E6 | A viewport whose host has width and no height once the drawing is placed absolutely turns pan by transform off for good and pans as before | The card editor's map takes its height from its drawing; with the drawing out of the flow the host collapsed to 1,239 by 0 and the map never drew (found by task 4's check). Play, Replay, the board viewer, the hand-over map, and the author page give the host a height of their own. A Node test covers the switch |

## Related documents

- [ASL Map Studio Large Campaign Map Analysis](<ASL Map Studio Large Campaign Map Analysis.md>) (sections 6, 10, 11, 12)
- [ASL Map Studio Raster Tiles Plan](<ASL Map Studio Raster Tiles Plan.md>) (deviation D5: the pane cannot measure speed)
- [ASL Map Studio Large Campaign Map Plan](<ASL Map Studio Large Campaign Map Plan.md>) (B3's first trial)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
