# ASL Map Studio Play Step Plan

**Written:** 2026-10-19, at the user's word: "P2 to P5 approved, write the plan".
**Source:** `src/ASL/docs/Memory-And-Performance/ASL Map Studio Play Step Analysis.md` (the analysis): its measurement (section 2), the code (section 3), the fixes (section 4), the cost (section 5), and the rulings P1 to P5 (section 6).
**Scope:** the fixes of ruling P2, in one branch and one pull request (P3): C1, B2, B1, H, I, D, and A1 with its measurement first. Not in scope: A2 and lazy hidden tabs (P2), C2 and A3 unless task 9 asks for them (P2, P4), the store's whole-file rewrite (P5, backlog 65), the setup step's browser time and the counter's stalls (backlog 65).
**Status:** written; not started. It starts at the user's word.

## 1. Goal

Make a Rout Phase step in Play cost about what any other step costs, and stop the reads and copies a step repeats for nothing, without changing what any rule decides or what any page shows.

Expected (the analysis, section 5.3; estimates, not gates): a Rout Phase step from about 567 ms of work to about 150 to 250 ms; any other step from about 122 to about 100 ms; the heaviest traced step's allocation from about 315 MB to under 150 MB.

## 2. Decisions

| | Decision |
|---|---|
| V1 | **One branch, `play-step`, from `main`; one pull request.** A green CI run on it before the merge (the merge gate ruling of 2026-10-13) |
| V2 | **The rules do not change.** Every fix asks Rules the same questions with the same facts, fewer times, or reads a file less. No calculator in the Rules project is touched |
| V3 | **The proof that no decision changed is the gate's replay proofs:** the replay digest and the sweep compared with the baseline (`code-builders/merge-gate/gate.sh`), with `CODE_MOVE=1` in the pass's config (`pass-configs/passplaystep.conf`), so every cut of p31c-tw is swept too |
| V4 | **Each task is one commit with its tests;** a task runs only the tests its change reaches (the user's rule, 2026-10-19); the whole projects run once, at the gate |
| V5 | **The per-state memo lives in the planner** (`GamePlanner`), in one `ConditionalWeakTable<GameState, PlayStepMemo>`, as `surrenderedFirst` already does (P4). Its stores are concurrent: the planner is shared by every circuit, and a duplicate fill is harmless since the answers are the same |
| V6 | **The surrender window** (task 2): nothing that reads the enemy list is kept for a state while that state's `SurrenderedBeforeRouts` set is being filled. The set is marked settled when its fill ends; until then the memo neither reads nor writes the rout answers of that state |
| V7 | **Measurements as in the analysis, section 2:** my Studio on port 6670 (Debug, built with `OutDir` `bin/p31b`), the follower of cd-guards-5 as the adjudicator, the three runs; allocation from allocation ticks only (the counter stalls, analysis 2.5). The scripts are committed under `code-builders/diagnostics/play-step/` (the ruling of 2026-10-13 on my tools) |
| V8 | **Out-of-scope findings go to the backlog** (section 65 or a new section), never into the branch unasked |

## 3. Tasks

### Task 0. The branch and the baseline

- Cut `play-step` from `main`. Run the gate's `base` stage once (about 25 minutes, in the background while task 1 is built).
- Commit the measurement scripts of the analysis (`split.py`, `trace_split.py`, `trace_children.py`, `trace_nested.py`, `stalls.py`, `allocread.cs`) to `code-builders/diagnostics/play-step/`, with a README that says how the three runs are made.
- The "before" figures are the analysis's section 2 (`main` at 645f18c3; nothing since has changed product code).
- **Done when:** the branch exists, the baseline is made, the scripts are committed.

### Task 1. C1: encirclement kept per state

- `PlayStepMemo` (V5) with its first store: `UpperLevelEncircled(state, unit)` by unit id. `UpperLevelEncircled` reads it first and fills it after the search.
- The answer does not depend on the view (the rule counts unconcealed enemies only) nor on the surrender window (it reads no enemy list), so it is kept at once.
- **Tests:** a planner-level test of `UpperLevelEncircled` (none exists): a unit upstairs cut off, one not, and the same state asked twice gives the same answer from one search (a counting fact reader, or the memo's own count). Run: the A7.72 rules tests (`ScenarioA1Pass45RulesTests`), the planner's encirclement tests (`BacklogPass12Tests`, `BacklogPass26Tests`, `BacklogPass35Tests`).
- **Done when:** those pass; expected gain most of the 216 ms of a Rout Phase step inside the enemy lists (checked in task 9).

### Task 2. B2: the rout answers kept per state

- The memo's rout stores: the enemy list per side (`RoutScan.Enemies` reads it from the memo and builds it once), `MustRout` per unit, `RoutSurrender` per unit, and `RoutAdvice` per unit.
- The surrender window (V6): `SurrenderedBeforeRouts` marks its state's set settled after `UnionWith`. Every rout store is read and written only for a settled state; inside the window (the routes read while the set is being filled, through `RoutSurrender` and `TrappedByInterdiction`) every answer is computed as today. A state outside the Rout Phase settles at once (its set stays empty, `Rout.cs:68-71`).
- Thread safety: the settled mark is set after the set is complete and read with a volatile read; the rout stores are concurrent dictionaries.
- **Tests:**
  - the surrender case of cd-guards-5 (`BacklogPass35Tests.AUnitBoundToSurrenderHoldsUpNeitherTheRoutOrderNorThePhasesEnd`) passes unchanged;
  - a new test of the window: a Rout Phase where one side's unit surrenders before the other side routs (A19.12, A20.21, the example on p. 69); the other side's routes are read once with the memo and once from a fresh planner, and must be the same, with the surrendered unit a prisoner to them;
  - a test that a second read of the same state reuses the enemy list (one build per side).
- Run: the planner's rout tests (`BacklogPass10Tests`, `BacklogPass13Tests`, `BacklogPass20TablePlayerTests`, `BacklogPass31dTests`, `BacklogPass35Tests`) and the rout rules tests (`ScenarioA1RoutRallyRulesTests`).
- **Done when:** those pass; the gate's proofs (task 10) are the final word on V2.

### Task 3. B1: the page asks each rout question once

- `ReadRoutObligations` and `ReadRoutChoices` become one pass over the broken units that asks, once per unit, `MayRout`, `RoutSurrenderAdvice`, `MustRout`, and the advice, and builds both lists; the duplicate `MustRout` (`Play.razor:6612`) goes. `routCache` keeps both lists, as now.
- **Tests:** the Play page's rout tests (`PlayPageDemoFixesTests`, `PlayPagePass28bTests`, `PlayPagePass31Tests`, `PlayPagePass31dTests`) unchanged.
- **Done when:** they pass.

### Task 4. H: a built-in card's text read once

- `ScenarioCards.EmbeddedText` keeps each name's text in a static concurrent dictionary, as `EmbeddedSha256` keeps its hash; the comment of `ScenarioCardLibrary.Current` then says what is true.
- **Tests:** one test that two reads of a built-in card give the same string instance; the scenario card tests in `LimboDancer.Domains.Asl.Play.Tests` that read cards.
- **Done when:** they pass.

### Task 5. I: the audit panel's tail without the whole log

- `FileAuditSink.Tail` reads back from the end of the file (blocks of 8 KB, from the end, until it has the lines asked for), under its lock, as now. Reading from the file, not a ring in memory, keeps the other Studio's lines (the two Studios share the folder).
- **Tests:** the sink's tail on a file shorter than a block, longer than many blocks, with a last line with and without a newline, and with no file. The adjudicator panel's page tests.
- **Done when:** they pass.

### Task 6. D: the unit table's rows once per revision and view

- `UnitRows` cached per (revision, view), as `routCache` is. One view key for the page's three caches (`cutOffCache` keys on `viewAs` today, `routCache` on `Viewer.Name`): the one `Viewer.Name` gives.
- **Tests:** the Play page's unit table tests (`PlayPageTests` and the pass tests that read `#play-units tr`) unchanged.
- **Done when:** they pass.

### Task 7. A1, measured first

- **The measurement:** a counting, timing decorator around `IBoardCatalog`, in a diagnostics harness under `code-builders/diagnostics/play-step/` that builds the planner as the Studio does, replays cd-guards-5 to revisions 621, 729, and 809 (Rout Phase states), and reads the rout answers there in Release: calls of `TryGetBoard` per state and their total time, against the time of the whole read. Time box: 30 minutes; if the harness cannot build the planner within it, the same decorator in my Studio's build only (never committed), read through the follower.
- **The fix**, if the lookup is more than a few per cent of the read after tasks 1 and 2: the allocation-free hit of the analysis (section 4, A1): the handle's `BoardReadResult` returned without the throwaway `InMemoryBoardCatalog`, non-capturing lambdas in `TryGetBoard` and `VaslBoardProvider.Load`. If it is less, A1 is recorded as measured and not built.
- **Tests:** `StudioBoardCatalogTests` (the same handle back, a pinned version not re-read, a published footprint seen), `CampaignPlayableFootprintsTests`.
- **Done when:** the count and time are in the analysis; the fix built and tested, or recorded as not needed.

### Task 8. The Studio check

- My Studio, Play, cd-guards-5 followed as the adjudicator and as each side, stopped in a Rout Phase with broken units: the Rout panel's obligations and choices as before (compared with a Studio built from `main` on port 6671, the same game and revision); the unit table's rows and the cut-off note; the adjudicator's audit panel shows the last lines. The scope of the five-width check is proposed to the user per the standing rule (one width, since no layout changes).
- **Done when:** both Studios show the same, and nothing else differs.

### Task 9. Measure after

- The analysis's three runs again on the branch (V7), the figures beside section 2's, in a new section of the analysis: a step by phase, the frames by phase, the heavy step's allocation.
- If the Rout Phase step is still well above the estimate, the trace says whether C2 (the per-state index) or A3 (the map context) is what is left; they are proposed to the user then, not built in this branch unasked (P2).
- **Done when:** the results are written.

### Task 10. The gate, the pull request, the documents

- The gate (V3): `strict`, then `baseline`, `build`, `proofs`; the replay digest and the sweep must show no difference from the baseline. The time log, the analysis's status, this plan's status, and the backlog (any finding of tasks 7 to 9).
- Push the branch, open the pull request, read its CI run (the merge waits for green, and for the user's word).
- **Done when:** CI is green and the user has the results.

## 4. Order and time

| Task | Estimate |
|---|---|
| 0. Branch, baseline (background), scripts committed | 15 min (and 25 min in the background) |
| 1. C1 | 20 min |
| 2. B2 with the surrender window | 1 h 10 |
| 3. B1 | 20 min |
| 4. H | 10 min |
| 5. I | 20 min |
| 6. D | 15 min |
| 7. A1 measured, then built or not | 30 to 50 min |
| 8. The Studio check | 20 min |
| 9. Measure after | 30 min |
| 10. The gate, the pull request, the documents | 40 min, and CI's 14 min |
| **In all** | **about 4 h 30 to 5 h** |

Tasks 1 and 2 come first because they hold most of the gain and the one design point; 3 to 6 are small and independent of each other; 7 is measured after 1 and 2, since they cut how often the lookup is called.

## 5. Risks

| Risk | Guard |
|---|---|
| A rout answer kept from inside the surrender window | V6; the window test of task 2; the gate's proofs |
| A memo shared across circuits read while it is filled | Concurrent stores; answers computed before they are stored; a duplicate fill gives the same answer |
| The page's caches keyed differently, so one view sees another's rows | One view key (task 6); the Studio check as each side (task 8) |
| The audit tail misses a line the other Studio wrote | Read from the file, not a ring (task 5) |
| The measurement misleads again | The sampler's limits (analysis 2.4) and the counter's stalls (2.5) kept in mind: outer frames for time, allocation ticks for allocation, the harness for the lookup |
