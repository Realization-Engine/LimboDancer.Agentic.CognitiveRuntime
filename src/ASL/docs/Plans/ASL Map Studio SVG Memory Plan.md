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

### Task 11. The replay measured by what the plan changes

Added 2026-10-18 at the user's word ("Yes, do that. Let's come up with a plan so we know what our expectations should be"), after the replay of task 10 showed private memory no lower (section 10 of the analysis: not definitive, and private memory is the wrong measure).

**What is compared.** The Studio as it was before the plan (`c3033048`: `main` with documents only) and as it is after it (this branch's head), built alike, each run on a freshly started process. The "before" build comes from a temporary git worktree in my scratchpad and runs on port 6671; the "after" build is my Studio on port 6670.

**The run.** The cd-guards-5 replay, all 258 steps, driven by the Replay page's own "run forward" at one step a second. That control is paced on the server (`Task.Delay` in `Replay.razor`), so a hidden page cannot slow it, and every run takes the same time. A visibility log goes with each run. Three runs of each build, alternating (before, after, before, after, before, after), so a drift in the machine's load falls on both.

**What is measured, per run:**

| Measure | How | Why |
|---|---|---|
| Bytes allocated per step | The runtime's counter of total allocated bytes (`dotnet-counters`, every second), from the first step to the last, divided by 258 | What the plan changed |
| Collections | The runtime's collection counts by generation, over the same span | Fewer allocations should mean fewer collections |
| CPU time | The process's processor time over the same span | Less work per render |
| Live heap after the run | A heap dump (`dotnet-gcdump`, which collects first), total and by type | What is held |
| Private memory: start, peak, settled | The sampler, as before | Recorded for continuity; not judged |

**Expectations, before running.** From the benchmark (section 10 of the analysis): a counter drawn at its three detail levels allocated about 0.70 MB before the plan and 0.14 MB after it. cd-guards-5 has 33 to 42 counters on the map, so one build of the counter layer should fall from about 23 to 29 MB to about 5 to 6 MB. Each step builds the layer at least once. If the Replay page also draws its unit table's previews each step (one detail level each), they add about 8 MB of saving.

| Measure | Expected change, after against before |
|---|---|
| Bytes allocated per step | 18 to 32 MB less per step; 4.6 to 8.3 GB less over the run |
| Gen 0 collections | Fewer, roughly in proportion to the allocation saved |
| CPU time | 4 to 10 s less over the run (the benchmark's render times fell by 15 to 40 ms per build) |
| Live heap after the run | The same within about 10 MB: the plan changes what is allocated, not what is kept |
| Private memory | No expectation: set by the collector's budgets, and 47 MB apart between identical runs |

**What the results would mean:**

- Allocation per step falls by the expected 18 to 32 MB: the plan works in the Studio as in the benchmark, and private memory is the collector's choice, not the Studio's need.
- It falls by much less than 18 MB: the counter layer is built less often than assumed, or other work dominates a step; the next step is an allocation trace of a few steps, by method.
- The live heap after the run is more than 10 MB higher after the plan: something now holds memory it did not; the dump's types say what (the pooled writer buffers are the first suspect).

**Needs.** `dotnet-counters` is not installed. It is Microsoft's tool, from nuget.org, installed into my scratchpad (`dotnet tool install dotnet-counters --tool-path <scratchpad>`), which is a download and needs the user's word. `dotnet-gcdump` is installed; `dotnet-trace` exists in an earlier session's scratchpad. A launch entry for port 6671 pointing at the worktree's build is added to `.claude/launch.json` (a local file the repository ignores) for the duration and removed after.

**Time.** About 45 minutes: the worktree and its build, six runs of about six minutes, the dumps.

- **Done when:** the six runs are in section 10 of the analysis, each measure against its expectation, with the visibility log of each run.
- **Outcome (2026-10-18):** done. Allocation per step fell 55% (20.95 to 9.40 MB), the same in all three pairs; gen 1 collections fell from 125 to 1, the collector's pauses by 14%; the live heap is unchanged. The expected saving (18 to 32 MB a step) was too high: the counter layer was about 14 MB of a 21 MB step. CPU did not measurably change. Committed and private memory rose by 68 to 92 MB in all three pairs: the collector keeps more, the Studio holds no more. One change from the procedure: the Replay page stops a run on a key or a click, so the script restarted a stopped run (none needed it after the first attempt at A1, which a click stopped at step 214 and which was discarded).

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
- For task 11, the word to install `dotnet-counters` into my scratchpad.
