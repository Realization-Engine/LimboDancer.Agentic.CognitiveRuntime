# ASL Map Studio Counter Layer Plan

**Written:** 2026-10-18, with its design: `src/ASL/docs/Designs/ASL Map Studio Counter Layer Design.md` (the design).
**Source:** the design; the SVG memory analysis's rulings 2, 3, and 4 (C1 first, then C2 when D is built; all three tiers until D and Q are built; Q with G instead of P).
**Scope:** the second group of the analysis: C1, the leads in the builder, D with C2, E, Q, and the measurement that decides F.
**Status:** proposed; waiting for the user's approval, and for the design's rulings U1 to U7. No code until both.

## 1. Decisions

| | Decision |
|---|---|
| V1 | **One branch, one pull request**, cut from `main` after PR 19 (`svg-memory-analysis`) has merged (U7). The design and this plan come with it. A green CI run on the pull request before the merge. |
| V2 | **No `gate.sh` proofs.** Nothing here changes Rules or Play behavior; the gate is the strict build, the tests below, the Node viewport test by hand, and CI. |
| V3 | **The 138 counter goldens and 4 board goldens never change.** A change in one is a defect in the task. The layer goldens (added in task 2) change on purpose, each change explained in its commit, and the equivalence oracle (task 1) proves the drawing unchanged. |
| V4 | **Measured, not assumed.** Each task records its characters and allocation from the benchmark's layer workload in a "Results" section of the design; task 9 repeats the replay of the analysis's task 11. |
| V5 | **Studio check scope** as in the design, section 11: after task 6 (D in the browser) and after task 8 (Q), at one width. |
| V6 | **Times** are logged per task in `src/ASL/docs/ASL Unit Time Log.md`. |

## 2. Tasks

Each task is one commit with its tests. Filtered tests during a task; the four projects in full, the strict build, and the Node test at task 9.

Test projects: `Units.Rendering.Tests` (goldens, overlay), `MapStudio.Tests` (library, workspace, game fixtures, `boardViewport.pointer.test.mjs`), `Maps.Rendering.Tests` (board goldens, unchanged), and the benchmark in `code-builders/diagnostics/svg-memory`.

### Task 1. The oracle, the benchmark workload, and the baseline

- The test helper that expands a layer's `<use>` references and compares it with standalone counters (design, section 11). Against today's layer it must pass as it is (each counter's own `<defs>`).
- The benchmark's layer workload: one map, a sequence of overlays with two counters moved per step; characters of the layer, characters that would be sent, bytes allocated per step.
- Record the baseline, and the layer at step 13 of cd-guards-5 in my Studio (characters, outline paths, elements).
- **Done when:** the oracle passes on today's layer for both fixtures and the game fixtures; the baseline is recorded.

### Task 2. C1: one `<defs>` of outlines per layer

- `UnitLayerContext`; `Glyphs.ArtFigures` asks it for outline ids (`fig-{hash}`, one per outline); `UnitOverlayBuilder` writes the collected `<defs data-figures>` after the last counter. Standalone drawing unchanged.
- The four layer goldens added (as of this task).
- **Done when:** counter goldens unchanged; the oracle passes; every `href` resolves; step 13's layer is about 376,000 characters with 6 outline paths.

### Task 3. The leads in the builder

- `Build` and `UnitLibrary.Overlay` take a lead; `GameMaps.Layers`, the Play setup map, and the hand-over map pass theirs; `Led` and its regular expression go when unused.
- **Done when:** parsed names and titles equal today's `Led` output for every fixture; the Replay and Play pages' counters show the same names (checked with task 6's Studio check).

### Task 4. D on the server: fragments by hex

- Hex fragments `units-{c}-{r}`; `UnitOverlay.Fragments`, `Attributes`, and a lazy `Svg`.
- `UnitLayerSync` (version, set, face size, fragment ids and hashes), the patch model, and the L5 fallbacks; tested as a C# model.
- **Done when:** the fragments joined equal `Svg`; patch folding equals the next overlay at every fixture revision; an unchanged overlay gives no patch.

### Task 5. C2: the figure sprite

- The sprite built once per figure art version; `setUnitFigures` in `boardViewport.js`; the layer in sprite mode writes no outlines.
- **Done when:** the sprite holds every built-in outline once; every layer `href` resolves to it; the oracle passes with the sprite.

### Task 6. D in the browser and in the hosts

- `applyUnitPatch` (version check, removals, upserts in place or before, attributes, focus, highlight, tier); `BoardViewport.ShowUnits`; `BoardWorkspace`, the board viewer, and the hand-over map use it; `sent.Units` becomes the sync record.
- Node tests for the patch, the version, focus, highlight, and tier.
- Studio check (V5).
- **Done when:** the Node test passes; in the Studio, the cd-guards-5 replay steps forward and back with patches only after the first step, and step 13 matches the baseline screenshot at each tier.

### Task 7. E: the fragment cache

- `UnitFragmentCache` with the hex key of the design (section 7), bounded at 4 million characters (U5), owned by `UnitLibrary`.
- **Done when:** cache-on equals cache-off at every revision of the game fixtures, forward, back, and jumping; each key input changes the key; the bound holds; the benchmark shows rendering only for changed hexes.

### Task 8. Q: face definitions

- `WriteFace` at the origin in layer mode; `unit-faces` definitions (`<g data-face-def>`); three `<use>` per counter; the face cache; faces in the patch (`facesAdd`, `facesRemove`); the focus custom properties and the `site.css` rule.
- Studio check (V5).
- **Done when:** counter goldens unchanged; the oracle passes within 1/32 pixel; the Q tests of the design (section 11) pass; in the Studio, step 13 matches the baseline screenshot at each tier and the gold focus edge shows.

### Task 9. The checks, the replay, and F

- The four test projects in full, the strict build, the Node test.
- The replay of the analysis's task 11, three runs, compared with its after-runs: allocation per step, collections, characters sent per step, live heap.
- F put to the user with the numbers (design, section 13).
- **Done when:** the results are in the design; the pull request's CI is green; F is the user's to rule.

## 3. Not in this plan

- F itself (task 9 only measures for it).
- P, R, A, B, H (analysis, ruling 4 and section 7).
- The table previews (`UnitPreview` in Replay and Games): they stay standalone.
- The rest of a step (projection, rules, render tree), which becomes the larger share after this work: in the ASL Unit Backlog, section 62.
