# ASL Map Studio Play Step Plan

**Written:** 2026-10-19, at the user's word: "P2 to P5 approved, write the plan". Revised the same day at the user's word on the order of work: "Write an implementation and THEN test in Studio", the Studio test before any unit test.
**Source:** `src/ASL/docs/Memory-And-Performance/ASL Map Studio Play Step Analysis.md` (the analysis): its measurement (section 2), the code (section 3), the fixes (section 4), the cost (section 5), and the rulings P1 to P5 (section 6).
**Scope:** the fixes of ruling P2, in one branch and one pull request (P3): C1, B2, B1, H, I, D, and A1 with its measurement first. Not in scope: A2 and lazy hidden tabs (P2), C2 and A3 unless task 8 asks for them (P2, P4), the store's whole-file rewrite (P5, backlog 65), the setup step's browser time and the counter's stalls (backlog 65).
**Status:** built on branch `play-step`, 2026-10-19, at the user's word to carry tasks 0 to 9 without intervention: tasks 0 to 8 committed one a task (504ac764 to 81b051a7), each tested in the Studio before its unit tests; A1 measured and built; C2 and A3 not needed for the goal (the analysis, section 8.5). Task 9: the gate and the pull request. The merge waits for a green CI run and the user's word.

## 1. Goal

Make a Rout Phase step in Play cost about what any other step costs, and stop the reads and copies a step repeats for nothing, without changing what any rule decides or what any page shows.

Expected (the analysis, section 5.3; estimates, not gates): a Rout Phase step from about 567 ms of work to about 150 to 250 ms; any other step from about 122 to about 100 ms; the heaviest traced step's allocation from about 315 MB to under 150 MB.

## 2. The order of work in every task

**Implement, build, test in my Studio, rework until the Studio is right, and only then run the unit tests the change reaches.**

Why, in this work: the goal is a cheaper Play step, and only my Studio running the follower shows whether a change reached it, and that Play still shows the same. A unit test proves the code gives the same answers; it cannot say whether the change paid. If the Studio shows it did not pay, or that a page changed, the implementation is reworked or dropped, and unit tests run before that verdict are spent on code that does not survive. The analysis is itself the warning: the 41, 40, and 31 ms figures, the sampler's charge to the clock call, and the "1.3 GB step" all looked right until the Studio was measured. So the Studio comes first among the tests, the unit tests come once the implementation stands, and they are only the classes the change reaches (the user's rule, 2026-10-19).

### 2.1 The Studio test

The same each time, so the figures compare with the analysis's section 2:

1. **Stop my Studio, build** into `bin/p31b` (`OutDir`; the user's Studio on 5178 holds `bin/Debug`), **start it** on port 6670 (launch config `map-studio-p31b`).
2. **Behaviour:** Play, a follower of cd-guards-5 as the adjudicator, held at the Rout Phase states the task touches (revisions 621 and 729: broken units with obligations, a surrender, cut-off units upstairs). The page reads what it read before the change: the Rout panel's obligations and choices, the unit table's rows and cut-off notes, and, for task 5, the audit panel's last lines. "Before" is a Studio built from `main` on port 6671, the same game and revision.
3. **Measurement, the short form:** the follower run over revisions 715 to 760 (about 25 steps, the German Rout Phase of turn 4 and the steps around it) with the page's `MutationObserver` noting each step: a Rout Phase step's work and an ordinary step's work, against section 2.2's figures (567 and 122 ms). For tasks whose gain is allocation (4, 5, 7), the allocation trace of the step from 621 to 622 (`gc-verbose`, read by `allocread.cs`), against section 2.5's 315 MB.
4. **Verdict:** the task's expected effect is seen and nothing on the page differs; else rework, and test in the Studio again.

The full three runs of the analysis are task 8's, once, at the end.

## 3. Decisions

| | Decision |
|---|---|
| V1 | **One branch, `play-step`, from `main`; one pull request.** A green CI run on it before the merge (the merge gate ruling of 2026-10-13) |
| V2 | **The rules do not change.** Every fix asks Rules the same questions with the same facts, fewer times, or reads a file less. No calculator in the Rules project is touched |
| V3 | **The proof that no decision changed is the gate's replay proofs:** the replay digest and the sweep compared with the baseline (`code-builders/merge-gate/gate.sh`), with `CODE_MOVE=1` in the pass's config (`pass-configs/passplaystep.conf`), so every cut of p31c-tw is swept too |
| V4 | **Each task: implement, the Studio test (section 2.1), then only the unit test classes the change reaches,** found by grep; one commit per task with its tests. The whole projects run once, at the gate |
| V5 | **The per-state memo lives in the planner** (`GamePlanner`), in one `ConditionalWeakTable<GameState, PlayStepMemo>`, as `surrenderedFirst` already does (P4). Its stores are concurrent: the planner is shared by every circuit, and a duplicate fill is harmless since the answers are the same |
| V6 | **The surrender window** (task 2): nothing that reads the enemy list is kept for a state while that state's `SurrenderedBeforeRouts` set is being filled. The set is marked settled when its fill ends; until then the memo neither reads nor writes the rout answers of that state |
| V7 | **Measurement as in the analysis, section 2:** allocation from allocation ticks only (the counter stalls, analysis 2.5); time from the page's step intervals and the trace's outer frames, never from leaf frames (analysis 2.4). The scripts are committed under `code-builders/diagnostics/play-step/` (the ruling of 2026-10-13 on my tools) |
| V8 | **Out-of-scope findings go to the backlog** (section 65 or a new section), never into the branch unasked |
| V9 | **Decided in task 9 under the user's word to go on without intervention, open to a veto:** the gate's "void run" stop (a digest and a sweep that differ in nothing) holds for rule passes; for a code move (`CODE_MOVE=1`) no difference is the proof, and the run is void only when the gate's Play, Rules, and MapStudio DLLs are the baseline's byte for byte. And `gate.sh` refuses to build while MapStudio's `bin` holds any folder but `Debug`, `Release`, and `gate` (the stalled builds of tasks 3, 7, and 9); Claude's Studio builds outside the repository, in `asl-gate-work/studio-6670` (the user agreed, 2026-10-19) |

## 4. Tasks

Each task below lists what it implements, what its Studio test must show, and then the unit tests it reaches.

### Task 0. The branch and the baseline

- Cut `play-step` from `main`. Run the gate's `base` stage once (about 25 minutes, in the background while task 1 is implemented).
- Commit the measurement scripts of the analysis (`split.py`, `trace_split.py`, `trace_children.py`, `trace_nested.py`, `stalls.py`, `allocread.cs`) to `code-builders/diagnostics/play-step/`, with a README for the Studio test (section 2.1) and the three runs.
- Build `main` once into a second output folder for the "before" Studio on port 6671.
- **Done when:** the branch exists, the baseline is made, the scripts are committed, the "before" Studio runs.

### Task 1. C1: encirclement kept per state

- **Implement:** `PlayStepMemo` (V5) with its first store: `UpperLevelEncircled(state, unit)` by unit id. `UpperLevelEncircled` reads it first and fills it after the search. The answer depends on neither the view (the rule counts unconcealed enemies only) nor the surrender window (it reads no enemy list), so it is kept at once.
- **Studio:** at 621 and 729, the cut-off notes in the unit table and the Rout panel as before. The short run: a Rout Phase step's work falls (most of the 216 ms inside the enemy lists is expected to go).
- **Then the tests:** a new planner-level test of `UpperLevelEncircled` (no planner test class covers A7.72 today, by grep): a unit upstairs cut off, one not, and the same state asked twice answered by one search; and the class that tests A7.72 in Rules (`ScenarioA1Pass45RulesTests`).
- **Done when:** the Studio shows the drop and no difference; the tests pass.

### Task 2. B2: the rout answers kept per state

- **Implement:** the memo's rout stores: the enemy list per side (`RoutScan.Enemies` reads it from the memo and builds it once), `MustRout`, `RoutSurrender`, and `RoutAdvice` per unit. The surrender window (V6): `SurrenderedBeforeRouts` marks its state's set settled after `UnionWith`; every rout store is read and written only for a settled state; inside the window every answer is computed as today. A state outside the Rout Phase settles at once (its set stays empty, `Rout.cs:68-71`). The settled mark is set after the set is complete and read with a volatile read.
- **Studio:** at 621 (the German Rout Phase of turn 4) and 729, the Rout panel's obligations, choices, advice, and any surrender text as in the "before" Studio, as the adjudicator and as each side. The short run: a Rout Phase step's work near the estimate (150 to 250 ms).
- **Then the tests:** the surrender case of cd-guards-5 (`BacklogPass35Tests.AUnitBoundToSurrenderHoldsUpNeitherTheRoutOrderNorThePhasesEnd`) unchanged; a new test of the window (a Rout Phase where one side's unit surrenders before the other side routs, A19.12, A20.21, the example on p. 69: the other side's routes read with the memo and from a fresh planner are the same, the surrendered unit a prisoner to them); a new test that a second read of the same state builds the enemy list once per side. The other planner classes with rout cases, by grep at the time (today `BacklogPass13Tests` and `BacklogPass35Tests` hold the surrender and interdiction cases).
- **Done when:** the Studio shows the gain and no difference; the tests pass. The gate's proofs (task 9) are the final word on V2.

### Task 3. B1: the page asks each rout question once

- **Implement:** `ReadRoutObligations` and `ReadRoutChoices` become one pass over the broken units that asks, once per unit, `MayRout`, `RoutSurrenderAdvice`, `MustRout`, and the advice, and builds both lists; the duplicate `MustRout` (`Play.razor:6612`) goes. `routCache` keeps both lists, as now.
- **Studio:** the Rout panel as before at 621 and 729; the short run's Rout Phase step a little lower again (with B2 in, most repeats are already cheap).
- **Then the tests:** the page classes that read the Rout panel, by grep for its ids at the time.
- **Done when:** the Studio shows no difference; the tests pass.

### Task 4. H: a built-in card's text read once

- **Implement:** `ScenarioCards.EmbeddedText` keeps each name's text in a static concurrent dictionary, as `EmbeddedSha256` keeps its hash; the comment of `ScenarioCardLibrary.Current` then says what is true.
- **Studio:** the game opens and plays from its card as before. The allocation trace of the step from 621 to 622: `EmbeddedText` gone from it (87 MB in section 2.5).
- **Then the tests:** one new test that two reads of a built-in card give the same string instance; the class that tests the card library, by grep.
- **Done when:** the trace shows the 87 MB gone; the tests pass.

### Task 5. I: the audit panel's tail without the whole log

- **Implement:** `FileAuditSink.Tail` reads back from the end of the file (blocks of 8 KB, from the end, until it has the lines asked for), under its lock, as now. Reading from the file, not a ring in memory, keeps the other Studio's lines (the two Studios share the folder).
- **Studio:** the adjudicator's audit panel shows the same last lines as the "before" Studio; a line written by the other Studio appears. The allocation trace of the step from 621 to 622: `FileAuditSink.Tail` gone from it (23 MB).
- **Then the tests:** new tests of the tail (a file shorter than a block, longer than many blocks, a last line with and without a newline, no file); the page class that reads the audit panel, by grep.
- **Done when:** the Studio and the trace show it; the tests pass.

### Task 6. D: the unit table's rows once per revision and view

- **Implement:** `UnitRows` cached per (revision, view), as `routCache` is. One view key for the page's three caches (`cutOffCache` keys on `viewAs` today, `routCache` on `Viewer.Name`): the one `Viewer.Name` gives.
- **Studio:** the unit table as before, as the adjudicator and as each side, switching views at one revision (the case a wrong key would show); the short run's ordinary step a little lower (about 13 ms is expected).
- **Then the tests:** the page classes that read `#play-units`, by grep.
- **Done when:** the Studio shows no difference between views and the gain; the tests pass.

### Task 7. A1, measured first

- **Measure:** a counting, timing decorator around `IBoardCatalog`, in a diagnostics harness under `code-builders/diagnostics/play-step/` that builds the planner as the Studio does, replays cd-guards-5 to revisions 621, 729, and 809, and reads the rout answers there in Release: calls of `TryGetBoard` per state and their total time, against the time of the whole read. Time box: 30 minutes; if the harness cannot build the planner within it, the same decorator in my Studio's build only, never committed, read through the follower.
- **Implement, if** the lookup is more than a few per cent of the read after tasks 1 and 2: the allocation-free hit (analysis, section 4, A1): the handle's `BoardReadResult` returned without the throwaway `InMemoryBoardCatalog`, non-capturing lambdas in `TryGetBoard` and `VaslBoardProvider.Load`. If it is less, A1 is recorded as measured and not built.
- **Studio:** the game plays as before; the allocation trace of the step from 621 to 622 with the lookup's share lower (about 40 MB in section 2.5).
- **Then the tests:** `StudioBoardCatalogTests`, `CampaignPlayableFootprintsTests`.
- **Done when:** the count and time are in the analysis; the fix built, tested in the Studio, then by its tests, or recorded as not needed.

### Task 8. The whole Studio measurement

- The analysis's three runs again on the branch, the figures beside section 2's, in a new section of the analysis: a step by phase, the frames by phase, the heavy step's allocation. A last behaviour pass through a whole Rout Phase as each side.
- If the Rout Phase step is still well above the estimate, the trace says whether C2 (the per-state index) or A3 (the map context) is what is left; they are proposed to the user then, not built in this branch unasked (P2).
- **Done when:** the results are written.

### Task 9. The gate, the pull request, the documents

- The gate (V3): `strict`, then `baseline`, `build`, `proofs`; the replay digest and the sweep must show no difference from the baseline. The time log, the analysis's status, this plan's status, and the backlog (any finding of tasks 7 and 8).
- Push the branch, open the pull request, read its CI run (the merge waits for green, and for the user's word).
- **Done when:** CI is green and the user has the results.

## 5. Order and time

| Task | Estimate |
|---|---|
| 0. Branch, baseline (background), scripts, the "before" Studio | 20 min (and 25 min in the background) |
| 1. C1 | 30 min |
| 2. B2 with the surrender window | 1 h 20 |
| 3. B1 | 25 min |
| 4. H | 15 min |
| 5. I | 25 min |
| 6. D | 20 min |
| 7. A1 measured, then built or not | 30 to 50 min |
| 8. The whole Studio measurement | 30 min |
| 9. The gate, the pull request, the documents | 40 min, and CI's 14 min |
| **In all** | **about 5 to 5 h 30** |

Each task's time includes its Studio test (a build, a restart, and a short run: about 5 to 10 minutes), which is why the total is higher than the analysis's 4 to 5 hours: the Studio is tested after every task, not only at the end. Tasks 1 and 2 come first because they hold most of the gain and the one design point; 3 to 6 are small and independent of each other; 7 is measured after 1 and 2, since they cut how often the lookup is called.

## 6. Risks

| Risk | Guard |
|---|---|
| A rout answer kept from inside the surrender window | V6; the Studio's surrender text at 621 against the "before" Studio; the window test of task 2; the gate's proofs |
| A memo shared across circuits read while it is filled | Concurrent stores; answers computed before they are stored; a duplicate fill gives the same answer |
| The page's caches keyed differently, so one view sees another's rows | One view key (task 6); the Studio switching views at one revision |
| The audit tail misses a line the other Studio wrote | Read from the file, not a ring (task 5); the Studio test of it |
| The measurement misleads again | V7: outer frames for time, allocation ticks for allocation, the harness for the lookup |
