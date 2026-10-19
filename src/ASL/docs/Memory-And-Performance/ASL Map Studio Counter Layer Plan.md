# ASL Map Studio Counter Layer Plan

**Written:** 2026-10-18, with its design: `src/ASL/docs/Memory-And-Performance/ASL Map Studio Counter Layer Design.md` (the design). Revised the same day for the user's rulings U2, U5, and the order.
**Source:** the design; the SVG memory analysis's rulings 2, 3, and 4 (C1 first, then C2 when D is built; all three tiers until D and Q are built; Q with G instead of P); the user's rulings of 2026-10-18 (U2: the hex, all its levels; U5: 4 million characters, least recently used out; the order: E first, alone).
**Scope:** the second group of the analysis, in two parts. Part 1 is E alone (tasks E1 to E4), its own branch and pull request. Part 2 is the rest: C1, the leads in the builder, D with C2, Q, and the measurement that decides F (tasks 1 to 7), started only when the user says so.
**Status:** part 1 approved 2026-10-18 and built on branch `counter-cache-e` (PR 21; results in the design, section 7.3). U1, U3, U4, and U6 approved 2026-10-19; part 2 built on `counter-layer-2`, stacked on part 1, at the user's word ("Start part 2 now on a branch stacked on counter-cache-e"); results and F in the design, section 16. **Complete:** PR 21 (part 1) merged into `counter-layer-2`, and both parts merged into `main` together as PR 22 (68447a05) on 2026-10-19; F ruled by the user the same day: F3, all three tiers stay.

## 1. Decisions

| | Decision |
|---|---|
| V1 | **One branch and one pull request per part.** Part 1 is cut from `main` once `play-follow-profile` has merged, or from `play-follow-profile` if it has not merged when part 1 starts (E needs its follower to measure Play, and its set-id fix to measure Replay as the analysis's section 14 did). The design and this plan come with part 1. A green CI run on each pull request before its merge. |
| V2 | **No `gate.sh` proofs.** Nothing here changes Rules or Play behavior; the gate is the strict build, the tests below, the Node viewport test by hand, and CI. |
| V3 | **The 138 counter goldens and 4 board goldens never change.** A change in one is a defect in the task. In part 1, the layer itself never changes either: the layer hash manifest (task E1) is the proof. In part 2, the manifest and the layer goldens (if U6 is approved) change on purpose, each change explained in its commit, and the equivalence oracle proves the drawing unchanged. |
| V4 | **Measured, not assumed,** in milliseconds and bytes allocated per step (not private memory). Each part records its numbers in a "Results" section of the design, against the expectations stated before building (design, section 7.1, for part 1). |
| V5 | **Measuring the Studio.** Play through the follower (`LivePlay.StartFollowing`, a recorded game's steps into a live copy); Replay through the page's own server-paced Run forward (a key or click stops it; resume it). Never browser-timer stepping: a hidden or unfocused pane throttles it. |
| V6 | **Studio check scope,** proposed under the case-by-case rule. Part 1: nothing the browser receives changes, so one width: the cd-guards-5 replay run forward and stepped back, step 13 compared with a screenshot taken in task E1 at the far, mid, and near tiers; the Play follower over a few steps; one counter's hover title read. Part 2: as in the design, section 11, after task 5 (D in the browser) and task 6 (Q). |
| V7 | **Times** are logged per task in `src/ASL/docs/ASL Unit Time Log.md`. |

## 2. Part 1: E alone

Each task is one commit with its tests. Filtered tests during a task; the test projects in full, the strict build, and the Node test at task E4.

Test projects: `Units.Rendering.Tests` (goldens, overlay), `MapStudio.Tests` (library, workspace, game fixtures, `boardViewport.pointer.test.mjs`), `Maps.Rendering.Tests` (board goldens, unchanged), and the benchmark in `code-builders/diagnostics/svg-memory`.

### Task E1. The baseline and the layer hash manifest

- The benchmark's layer workload: one map, a sequence of overlays with two counters moved per step; layer characters, bytes allocated, and time per step, for `Build` and for `Led` separately.
- The manifest: the SHA-256 of the layer at every revision of the game fixtures `MapStudio.Tests` holds, and of the nine demo counters on bd01 and a crowded hex (levels, a cellar, more than six counters, attachments, facings, a draft), both sheets; recorded as test data by today's builder, and a test that asserts it.
- In my Studio: the Replay run forward over cd-guards-5 and the Play follower over the same game, with the counter build (`Build`) and `Led` timed per step, allocation per step, and step 13's screenshots at the three tiers.
- **Done when:** the manifest test passes on today's builder; the baseline is in the design's Results section.

### Task E2. The builder writes hex fragments

- `UnitOverlayBuilder.Build` writes each hex (every level, badges, tabs) into a fragment of its own and joins them in paint order; `data-face-size` and the other root attributes stay outside the fragments; no wrapper element (design, section 7.1).
- **Done when:** the manifest is unchanged; counter goldens unchanged; the existing overlay tests pass unchanged.

### Task E3. UnitFragmentCache

- The hex key of the design, section 7, without the lead (section 7.1); the renderer's fingerprint (`StyleSheet.Hash`, the palette's name, `UnitRenderer.Version`, and the vocabulary's and figure art's content hashes, added where missing).
- The value: the fragment's markup, its content hash, and its counters' warnings, replayed with their unit ids on a hit.
- The bound: 4 million characters, least recently used out (U5); one lock, never held while a hex renders.
- Owned by `UnitLibrary` (one in the Studio) and passed to `Build` by `UnitLibrary.Overlay`; `Build` without a cache renders everything, as today.
- **Done when:** the manifest is unchanged with the cache on; cache-on equals cache-off at every revision of the game fixtures, forward, back, and jumping; the key changes with each document state a counter can show and each fingerprint input; the bound holds; warnings return on a hit; the concurrent test passes; the benchmark shows rendering only for changed hexes.

### Task E4. Measured in the Studio, the checks, and the pull request

- The Replay run and the Play follower of task E1 repeated: the counter build and `Led` per step, allocation per step, the cache's hit rate and size; compared with the expectations of the design, section 7.1, in its Results section.
- The Studio check (V6).
- The three test projects in full, the strict build, the Node viewport test by hand; the pull request; CI read after every push.
- If `Led` is a large share of what remains, L6 put to the user as the next slice, with the number.
- **Done when:** the results are in the design; the pull request's CI is green; the merge waits for the user's word.

## 3. Part 2: the rest of the group

Started only on the user's word, from `main` after part 1 has merged. Its tasks are those of the first version of this plan, less E (its task 7), with C2 joined to D in the browser.

### Task 1. The oracle

- The test helper that expands a layer's `<use>` references and compares it with standalone counters (design, section 11). Against today's layer it must pass as it is (each counter's own `<defs>`).
- **Done when:** the oracle passes on the layer fixtures and the game fixtures.

### Task 2. C1: one `<defs>` of outlines per layer

- `UnitLayerContext`; `Glyphs.ArtFigures` asks it for outline ids (`fig-{hash}`, one per outline); `UnitOverlayBuilder` writes the collected `<defs data-figures>` after the last counter. Standalone drawing unchanged.
- With E built, the outlines are collected per fragment and the layer's `<defs>` is the union of its fragments' outlines, so a hit still contributes its outlines. `UnitRenderer.Version` is raised, which empties the cache by key.
- The four layer goldens added, if U6 is approved; the manifest changes on purpose.
- **Done when:** counter goldens unchanged; the oracle passes; every `href` resolves; step 13's layer is about 376,000 characters with 6 outline paths.

### Task 3. The leads in the builder

- `Build` and `UnitLibrary.Overlay` take a lead; `GameMaps.Layers`, the Play setup map, and the hand-over map pass theirs; `Led` and its regular expression go when unused. The lead joins the hex key.
- **Done when:** parsed names and titles equal today's `Led` output for every fixture; the key changes with the lead; the Replay and Play pages' counters show the same names (checked with task 5's Studio check).

### Task 4. D on the server: fragments with ids

- The fragment wrapper `units-{c}-{r}`; `UnitOverlay.Fragments`, `Attributes`, and a lazy `Svg`.
- `UnitLayerSync` (version, set, face size, fragment ids and hashes), the patch model, and the L5 fallbacks; tested as a C# model.
- **Done when:** the fragments joined equal `Svg`; patch folding equals the next overlay at every fixture revision; an unchanged overlay gives no patch.

### Task 5. D in the browser and in the hosts, with C2

- C2 (needs U1): the sprite built once per figure art version; `setUnitFigures` in `boardViewport.js`; the layer in sprite mode writes no outlines.
- `applyUnitPatch` (version check, removals, upserts in place or before, attributes, focus, highlight, tier); `BoardViewport.ShowUnits`; `BoardWorkspace`, the board viewer, and the hand-over map use it; `sent.Units` becomes the sync record.
- Node tests for the patch, the version, focus, highlight, and tier. Studio check (V6).
- **Done when:** the sprite holds every built-in outline once and every layer `href` resolves to it; the Node test passes; in the Studio, the cd-guards-5 replay steps forward and back with patches only after the first step, and step 13 matches the baseline screenshot at each tier.

### Task 6. Q: face definitions (needs U3 and U4)

- `WriteFace` at the origin in layer mode; `unit-faces` definitions (`<g data-face-def>`); three `<use>` per counter; the face cache beside the fragment cache; faces in the patch (`facesAdd`, `facesRemove`); the focus custom properties and the `site.css` rule.
- Studio check (V6).
- **Done when:** counter goldens unchanged; the oracle passes within 1/32 pixel; the Q tests of the design (section 11) pass; in the Studio, step 13 matches the baseline screenshot at each tier and the gold focus edge shows.

### Task 7. The checks, the replay, and F

- The test projects in full, the strict build, the Node test.
- The Replay run and the Play follower of task E1 repeated, and the replay of the analysis's task 11, three runs: allocation per step, collections, characters sent per step, live heap.
- F put to the user with the numbers (design, section 13).
- **Done when:** the results are in the design; the pull request's CI is green; F is the user's to rule.

## 4. Not in this plan

- F itself (task 7 only measures for it).
- P, R, A, B, H (analysis, ruling 4 and section 7).
- The table previews (`UnitPreview` in Replay and Games): they stay standalone.
- Play's panel rules (rout obligations, about 41 ms a step; upper-level encirclement, about 40 ms), and the rest of a step (projection, rules, render tree): in the ASL Unit Backlog, section 62.
