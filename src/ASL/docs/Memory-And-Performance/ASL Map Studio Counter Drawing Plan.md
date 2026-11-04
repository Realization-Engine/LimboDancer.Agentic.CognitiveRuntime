# ASL Map Studio Counter Drawing Plan

**Written:** 2026-11-03, at the user's word ("OK, we can pursue this") on the two candidates of the [Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>), section 5.6. This plan is the first: counters drawn more cheaply when a zoom changes many of them at once. The second is the [Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>).
**Status:** approved by the user on 2026-11-03 ("I've reviewed both plans and I approve all proposal in both plans"), with decisions P1 to P4 as section 6 proposes them; then "Execute and work independently until all the code is implemented and I can see a demo and perform tests". Built on branch `claude/speed-plans` (section 8).
**Sources:** the analysis's section 5.6 (the Goodwood suites, the deep-zoom breakdown, the trace); the [Map Lab Plan](<ASL Map Studio Map Lab Plan.md>), finding F9; the [Counter Layer Design](<ASL Map Studio Counter Layer Design.md>), sections 6, 8, and 10; `UnitRenderer.WriteUnit`, `UnitOverlayBuilder.WriteHex`, and `boardViewport.js` on `main`.

## 1. The problem

A counter in the layer is a group (`data-unit-id`, `role="button"`, `tabindex="0"`, a name, a hover title) holding **three references**, one for each detail tier (far, mid, near). Each reference points at the counter's shared face definition, and the face points again at the shared figure art. CSS shows the tier the zoom calls for and hides the other two (`#layer-units [data-tier]` and `data-active-tier`).

Chrome copies the content a reference points at into the page, for each reference. On Goodwood's game map, 548 counters make 1,644 references, and the layer comes to the roughly 14,000 elements that the trace restyled. That costs nothing while a drag hides or shows a few counters at a time. It costs a great deal when one commit changes many of them:

- **A change of tier** (far to near on a deep zoom, back on a return to Fit) rewrites `data-active-tier` on the layer, which restyles every reference of every counter.
- **A big zoom** moves most counters into or out of view at once, and hiding or showing each one restyles and lays out its subtree.

## 2. Baseline, expected gains, method, stop threshold

Stated before the tasks (the user's rule of 2026-10-19).

| Measure | Baseline (analysis, 5.6) | Expected | Stop threshold |
|---|---|---|---|
| Elements restyled when a deep zoom lands (trace, `UpdateLayoutTree` `elementCount`) | 14,336 | Under 3,000 after step 1 | Above 7,000 after step 1: stop and report |
| Deep zoom from Fit, longest frame, counters out of view hidden (Goodwood suite, "in view") | 104 to 164 ms | Within 10 ms of the same run with no counters (96 to 114 ms; the rest is the [Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>)'s) | More than 30 ms above "none" after step 2 |
| Return to Fit after a deep zoom (trace, style recalculation) | 90 ms over 8,373 elements | Under 30 ms | |
| Pane-long drags where labels are readable and at near zoom, "in view" | 59 to 68 fps | Unchanged or better | Worse than the baseline by more than 5 fps: undo the step |
| The Lab, 40,392 hexes, 600 counters, readable-zoom long drag, "in view" and "all" | "all" 6.8 fps (F9); "in view" not yet measured in Chrome | "in view" near the 58.8 fps of no counters | |

**Method.** The user's Chrome, at the window of the earlier runs (912 by 734 on Goodwood), on the display of about 85 Hz:

- `mapProbeSuite()` on Goodwood's game map (behind the side's "show my view" button), three runs, the first dropped;
- the Map Lab's Measure button on the 40,392-hex map with 600 counters;
- one Chrome performance trace of a deep zoom for the element counts, as before (`asl-gate-work/traces/`).

Each step is measured before the next is built.

## 3. The steps

### Step 0: the Lab's baseline

The user runs the Lab's Measure on the 40,392-hex map with 300 German and 300 Russian counters, now that it measures "all", "in view", and "none" (the runs of F9 had only "all"). No code.

### Step 1: one reference a counter (in the browser)

When the viewport receives the counter layer (`setUnits`) or a patch (`applyUnitPatch`), it rewrites each counter's three tier references into one:

- the one reference left points at the active tier's face; the other two faces' addresses are kept on it as data attributes (`data-far`, `data-mid`, `data-near`);
- the layer is marked as rewritten, and a site.css rule there replaces the three-tier rule for it;
- a change of tier points the references of the counters **in the drawn box** at the new tier's face; a counter out of view is pointed at it when it comes back into view (`cullToView` does it as it shows the fragment).

Expected: a third of the references and their copies, and a change of tier that touches only the counters in view (tens to a few hundred) rather than all 548.

What stays as it is: the counter's group with its id, role, tab stop, name, and title; clicks and keyboard (`data-unit-id`, Enter and Space); the gold focus ring, which reaches through the reference by inherited custom properties (Counter Layer Design, section 8); patches, which replace whole hex fragments and are rewritten as they arrive; keyboard focus kept across a patch; the side-by-side comparison, which never clones the counters; the server's markup, fragment cache, goldens, and equivalence oracle (decision P1).

### Step 2: a large change spread over frames (only if step 1 leaves the landing frame long)

When one commit would hide or show more than a set number of fragments (to be set by measure; about 150 to start), the viewport applies the change over several frames, those nearest the middle of the view first. It does not reduce the work; it keeps it out of one frame.

### Step 3: far and middle tiers painted into a canvas (only at the user's ruling, decision P3)

If steps 1 and 2 do not reach the expected figures, counters at the far and middle tiers would be painted into one canvas at each commit, as the labels are: each distinct face drawn once into a bitmap, then placed for the counters in view. At those tiers a counter would no longer be an element in the page: a click would find the counter by its hex from the layer's data, and a counter could not take keyboard focus until the near tier. That is a change in how the map can be used, so it waits on the user (P3), and it would get its own section here before it is built.

## 4. Tests and checks

- **Node viewport tests** for step 1: the rewrite of a counter to one reference with its tier addresses; a change of tier pointing only the counters in the box; a counter coming back into view pointed at the active tier; a patch's new fragment rewritten as it arrives; focus kept by unit id. For step 2: the order and the bound of a spread change.
- **No change on the server**, so no golden or oracle changes and no merge-gate stage (render only; the standing rule of 2026-11-01).
- **My Studio at 6670**: the game map of the Goodwood reference (behind "show my view") and the Lab with 600 counters, at Fit, five steps in, readable, and near zoom; a counter clicked, focused by Tab, and selected at each tier; a step of play that patches the layer; screenshots that the counters look as before at each tier.
- **The user's Chrome** for the figures of section 2.

## 5. Risks

| Risk | Answer |
|---|---|
| A counter out of view is shown with a stale tier | `cullToView` points it at the active tier as it shows it; a Node test checks it |
| A patch's fragment arrives with three references | `applyUnitPatch` rewrites it before it is attached, as `setUnits` does |
| A page that draws counters outside the viewport (the Unit Lab's previews, standalone counters) | Untouched: the rewrite runs only on the map's counter layer, and the three-tier CSS rule stays for everything else |
| Hover titles or screen-reader names change | The counter's group, title, and name are not touched |
| Step 1 gains less than expected because the copies come from the face's own figure references | The trace's element count shows it (section 2's stop threshold); then the face definitions' inner references are the next thing to look at, in a section of their own |

## 6. Decisions for the user

| Id | Question | Proposal |
|---|---|---|
| P1 | Where the three references become one | In the browser, as the layer arrives: no server markup, golden, or oracle changes, and the standalone counters keep their three tiers |
| P2 | Step 2 | Only if step 1 leaves the deep zoom's landing frame more than 30 ms above "none" |
| P3 | Step 3, a canvas for far and middle tiers | Not now; asked again with figures if steps 1 and 2 fall short, since it changes clicks and keyboard focus at those zooms |
| P4 | The order | Step 0, then step 1, measured, then step 2 only as P2 says; one branch, one pull request per step |

## 7. Answers to the guidelines' checklist

- **Work per state, unit, hex, or step:** none added on the server. In the browser, step 1 adds a rewrite per counter when a layer or patch arrives (three attributes read, one written), and removes two references per counter for the life of the layer; a change of tier touches only the counters in view.
- **Ownership, keys, lifetime, bounds, invalidation, concurrent fills:** nothing new is cached. The tier addresses live on the counter's own reference and go with its fragment when a patch replaces it.
- **Disclosure, rout settlement, snapshots, pinned versions, seams:** unchanged; the browser only reshapes what the server sent for the side's view.
- **Unchanged counters reuse fragments:** yes; the server's fragments and patches are unchanged, and a patch rewrites only the fragments it brings.
- **Checks and measurements:** section 4; the probe's suite and the Lab's Measure already measure every counter mode.
- **Tests:** Node tests in the existing viewport test file; no new test project.
- **Benefit against noise:** three runs, the first dropped, the same window and display; the trace for the element counts.
- **Results, backlog, outside outputs:** results in a section of this plan and the analysis; the backlog's rows "Counters drawn cheaply at far and middle zoom" and "The deep zoom of the screen-bounded map" updated; my Studio's build in `asl-gate-work/studio-6670`, cleaned as usual.

## 8. As built (2026-11-03)

Step 1 built on branch `claude/speed-plans`, in `boardViewport.js`, at the user's word to work through to a demo. Step 2 is not built: decision P2 makes it wait on step 1's figures in the user's Chrome. Step 3 waits on P3.

- **Step 1.** As the counter layer arrives (`setUnits`) and as a patch's fragments arrive (`applyUnitPatch`), each counter's three tier references become one, before the node joins the page (`oneReference`): the kept reference loses its `data-tier`, carries the three faces' addresses (`data-far`, `data-mid`, `data-near`), and points at the active tier. A change of tier (`updateUnitTier`) points the counters in view once the commit has culled (`pointTier`); a counter coming back into view is pointed as `cullToView` shows it. No CSS rule changed: the kept reference has no `data-tier`, so the three-tier rule passes it by. `?counters=three` keeps the server's three, for comparison.
- **Checked in my Studio at 6670** on the Goodwood game map (behind "show my view"): all 548 counters one reference each, with their role, tab stop, name, and title; the layer from 4,548 elements to 3,452 and from 1,746 references to 650. At Fit, near zoom, and back, every counter in view pointed at the active tier (530, 9, and 127 in view), and the near faces drawn. A click selected the counter's hex; a counter took keyboard focus with its name. In Replay, three steps back and to the last patched the layer (versions 2 to 4), and every counter stayed one reference, rightly pointed. On the Lab's 40,392-hex map, 600 counters one reference each.
- **Tests:** Node viewport tests for the rewrite, the kept faces, the pointing, and a counter written in place left alone.

## Related documents

- [ASL Map Studio Goodwood Lab Techniques Analysis](<ASL Map Studio Goodwood Lab Techniques Analysis.md>)
- [ASL Map Studio Tile Pacing Plan](<ASL Map Studio Tile Pacing Plan.md>)
- [ASL Map Studio Counter Layer Design](<ASL Map Studio Counter Layer Design.md>)
- [ASL Map Studio Map Lab Plan](<ASL Map Studio Map Lab Plan.md>)
- [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>)
