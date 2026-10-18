# ASL Map Studio SVG Memory Plan

**Written:** 2026-10-18, at the user's word: "Write the short implementation plan for the first group (N2, M, G1, S1, J, K, L, G2, G3) in src/ASL/docs/Plans on svg-memory-analysis … Do not write code yet."
**Source:** `src/ASL/docs/Designs/ASL Map Studio SVG Memory Analysis.md` (the analysis): its options, its measurements (sections 2 and 5.1), and its rulings (section 8).
**Scope:** the first group of section 7 only. The counter-layer group (C, D, E, Q, F) needs a design before a plan and is not covered here.
**Status:** approved by the user, 2026-10-18 ("All approved": the word to start, and V1, V2, V5); in progress.

## 1. Goal

Lower the Studio's memory and allocation without changing a byte of what it draws:

- the SVG writer stops making a string per character and per number (G1, G2, G3);
- the style cascade stops allocating on every rule it walks (S1);
- the two caches without a limit get one (J, K), and board SVG is served compressed (L);
- the garbage collector conserves memory (N2), at a level chosen by measurement;
- the Blazor connection's compression is known (M).

## 2. Decisions

| | Decision |
|---|---|
| V1 | **One branch, one pull request.** The work continues on `svg-memory-analysis` after `main` is merged into it (`unit-figure-art` is in `main` since b50fa2d8, so this is ruling 5's "branch from main"). The analysis, this plan, the benchmark, and the code reach `main` together. The branch adds files, so it needs a pull request and a green CI run in any case (`how_to_reach_main.sh`). |
| V2 | **No `gate.sh` proofs.** Nothing here changes Rules or Play behavior, so the replay digest and planner sweep have nothing to prove; the gate is the strict build, the tests below, and CI. The rule passes' gate stays as it is. |
| V3 | **Output identity is the main check.** Every task except N2, J, K, and L must leave every golden SVG unchanged by a byte, and the benchmark's replay must report `identical: True` everywhere. A golden that changes is a defect in the task, never a golden to update. |
| V4 | **Measured, not assumed.** Each task's gain is read from the benchmark (`code-builders/diagnostics/svg-memory`) or the replay of section 2.2, and recorded in the analysis's new section 10. |
| V5 | **Studio check scope** (per the case-by-case rule): bd01 in the board viewer and the cd-guards-5 replay, stepped through, at one width, after the last code task; the card editor once after K. No five-width check, since nothing visual changes. |

## 3. Tasks

Each task is one commit (G2 may be two), with its tests in the same commit. Tests run on the blast radius during a task; the four projects in full, and the strict build with warnings as errors, at the end (task 10).

Test projects: `Maps.Tests` (FixedPoint), `Maps.Rendering.Tests` (SvgWriter, 4 board goldens), `Units.Rendering.Tests` (138 counter goldens, 69 per sheet), `MapStudio.Tests` (render endpoint, map service, pages).

### Task 1. Merge main; take the baseline

- Merge `main` into `svg-memory-analysis`.
- Run the benchmark and record its report as the baseline in section 10 (it should match section 5.1).
- **Done when:** the branch builds; the baseline is recorded.

### Task 2. M: read the connection's compression

- Find whether the circuit's WebSocket negotiates `permessage-deflate`: the response headers of the `_blazor` connection in the browser, and the .NET 10 default of `DisableWebSocketCompression` for `AddInteractiveServerRenderMode`.
- No product change in this task.
- **Done when:** the answer is in section 10. If compression is off, turning it on is a question for the user (the analysis notes the side-channel trade); it is not part of this plan.

### Task 3. N2: choose the conservation level

- Run the cd-guards-5 replay of section 2.2 (memory sampled every second, first step to last) with `DOTNET_GCConserveMemory` at 0, 5, and 7, set in the environment of my Studio, so no rebuild is needed between runs.
- Choose the lowest level that gives most of the saving, without step time rising by more than 10% over level 0.
- Set it in the Studio's project file (`RuntimeHostConfigurationOption` `System.GC.ConserveMemory`), so it reaches `runtimeconfig.json`.
- **Done when:** the three runs and the choice are in section 10; the Studio runs with the chosen level; `MapStudio.Tests` pass.
- **Outcome (2026-10-18):** no level gave a measurable saving (two default runs differed by more than the levels did). The user ruled to leave it off; the project file is not changed. Section 10 of the analysis has the runs, and a correction: the step times first reported were distorted by a hidden page's throttled timers.

### Task 4. G1: escape by span

- Tests first: `TextAndAttributesAreEscaped` becomes a theory: an empty value, no special characters, a special character first, last, and twice in a row, every special in both text and attributes, `>` left as it is in an attribute and `"` left as it is in text (today's behavior), and non-ASCII text (a name such as "Kübelwagen").
- `SvgWriter.Text` and `SvgWriter.WriteTag` append the spans between the characters that need escaping (`SearchValues<char>`, `IndexOfAny`, `Append(ReadOnlySpan<char>)`). No API change.
- **Done when:** `Maps.Rendering.Tests` and `Units.Rendering.Tests` pass with every golden unchanged; the benchmark reports `identical: True`, and its whole-render allocation for bd01 exact falls from about 9.5 MB to about 3.8 MB (section 5.1, G1).

### Task 5. S1: the cascade without boxing or closures

- `StyleCascade.Compute`, `Rank`, and `Matches`: index loops over the `IReadOnlyList` members instead of `foreach`, and plain loops instead of `Any` with a lambda.
- No change in which rules match or in their rank.
- **Done when:** `Units.Rendering.Tests` pass with every golden unchanged; the benchmark's allocation by type for 80 counters no longer lists the `Selector` enumerator, delegates, or closures among its top items, and the whole render falls by about 25 MB of its 56 MB.

### Task 6. J: a bounded render cache that keeps bytes

- `RenderCache` keeps the latest version of each board only: a request for a new version of a board drops that board's older entries.
- It stores UTF-8 bytes, the ETag (the same SHA-256 of the same bytes as now, so browsers' cached copies stay valid), and a gzip copy for task 8.
- The endpoint writes the bytes as they are, instead of re-encoding a string on every request.
- New tests (none exist today): a second version evicts the first; two boards keep their own entries; the served bytes and the ETag are the ones computed today.
- **Done when:** `MapStudio.Tests` pass, the new tests among them.

### Task 7. K: a bounded map cache

- `MapService.cache` drops the maps built from placements when `placed` is cleared at 32 (saved maps, which are few and reloaded often, stay).
- New tests: building more than 32 placement maps leaves at most 32 of them cached; a saved map stays cached throughout.
- **Done when:** `MapStudio.Tests` pass, the new tests among them; the card editor checked once in the Studio (V5).

### Task 8. L: compressed board SVG

- `/render` sends the gzip copy from task 6 with `Content-Encoding: gzip` and `Vary: Accept-Encoding` when the request accepts gzip, and the plain bytes otherwise; a `304` answers as now.
- Precompressed rather than the response compression middleware, so nothing is compressed twice.
- New tests: gzip asked, gzip given, and its content unzips to the plain bytes; gzip not asked, plain bytes; the ETag is the same either way.
- **Done when:** `MapStudio.Tests` pass; bd01's layers arrive compressed in the browser (about 77 KB for the styled document instead of 334 KB).

### Task 9. G2 and G3: the span writer

G2, numbers and paths written as numbers:

- `FixedPoint` gains `TryFormat` (`ISpanFormattable`), giving exactly `ToString`'s text. Tests: it agrees with `ToString` on the edge values and on every value from -100,000 to 100,000 raw.
- `PathData` and attribute values accept numbers, so a number is formatted straight into the output. Every `N(…)`, `SvgWriter.Number(…)`, and `SvgWriter.Fixed(…)` at a call site (about 100) passes the number instead of its text.
- Figure outlines, already proven to be path data by `FigureArt.CheckedPath`, are copied without escaping.

G3, a pooled buffer:

- `SvgWriter` writes into a `char` buffer rented from `ArrayPool<char>.Shared` instead of a `StringBuilder`, and returns it when the document is turned into its string.

- **Done when:** `Maps.Tests`, `Maps.Rendering.Tests`, and `Units.Rendering.Tests` pass with every golden unchanged; the benchmark reports `identical: True`, and bd01 exact's whole render allocates about 0.6 MB (from 9.5 MB).

### Task 10. Measure, check, and gate

- Repeat section 2.2 (the cd-guards-5 replay, with samples and heap dumps) and the benchmark; record the after-figures in section 10 beside the before-figures.
- The Studio check of V5.
- The strict build of the solution, and the four test projects in full.
- **Done when:** all of it passes and section 10 is complete.

## 4. Route to main

1. Task 10 passes on this machine.
2. At the user's word: push `svg-memory-analysis` and open its pull request (the standing permission covers the pull request once the push is approved).
3. CI (`asl-authoring-ci.yml`) green on the branch's head.
4. The merge is the user's to start; after it, I read the CI run of the merge.

## 5. Record

- Each task's start, end, and duration in `src/ASL/docs/ASL Unit Time Log.md`, under the SVG memory section.
- Each task's measured result in the analysis's section 10.
- The analysis's section 7 marks each option done as its task lands.

## 6. What needs the user

- The word to start (this plan builds nothing until then).
- V1, V2, and V5: approved 2026-10-18.
- After task 2, if the connection is uncompressed: whether to turn compression on.
- After task 10, the word to push.
