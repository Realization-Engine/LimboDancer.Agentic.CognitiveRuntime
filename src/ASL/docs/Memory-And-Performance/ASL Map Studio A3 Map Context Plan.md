# ASL Map Studio A3 Map Context Plan

**Written:** 2026-10-19, at the user's word ("Write it"), after the Play Step Plan was merged (PR 23, 97666623).
**Source:** the Play Step Analysis (`ASL Map Studio Play Step Analysis.md`): fix A3 (section 4, A), what is left of a Rout Phase step (section 8.5), the board lookup counted (section 8.4); backlog section 65; a read of the code on `main` at 97666623 (section 2 below).
**Scope:** A3, the map context per state: each board's handle and a placed map's composed read, obtained once per game state and kept with it, and `MapInPlay.Board` without its per-call allocation. Not in scope unless the user rules otherwise (section 9): a cache of resolved Locations per state (A3b), and C2 (the index of a state's objects).
**Status:** written; not started. It starts at the user's word.

## 1. Expected gains, stated first

The baselines are this branch's starting point, `main` at 97666623, as measured on 2026-10-19 (the Play Step Analysis, section 8). Each figure is judged at the end by the method named, against the threshold: a figure that does not reach it does not justify the change it stands for.

| Figure | Baseline (source) | Expected after A3 | How measured | Below this, that part is dropped |
|---|---|---|---|---|
| A Rout Phase step's work, median, cd-guards-5 (one board) | 258 ms (section 8.1, run 1, 33 steps) | about 210 to 240 ms | Run 1 again: the whole game, each Studio's tab in front | 15 ms saved |
| Board lookups (`TryGetBoard`) in a Rout Phase step, cd-guards-5 | 77,000 to 106,000 a step, about 1.1 µs each before A1 (section 8.4) | one per board per state, a few a step | The counting decorator, my Studio's build only (task 2) | |
| Their time after A1 | not measured (A1 removed their allocation; its time gain was below the short runs' noise) | the whole of it saved | The same decorator, timed (task 2) | if task 2 finds it under 15 ms a step, the single-board part of A3 is not built |
| The heaviest step's allocation, 621 to 622, cd-guards-5 | 101 MB (section 8.3) | about 92 to 95 MB (`MapInPlay.Board`'s capturing lambda, about 7 MB, and the footprint reads) | `gc-verbose` allocation ticks, `allocread.cs` | 5 MB saved; `BoardHandle.Resolve`'s 23 MB stays unless A3b is ruled in (section 9) |
| A Rout Phase step on a placed map (bd04 with bd02 reversed, the Gambit card) | not measured: task 2 measures it on the test game of task 1 | well below the baseline: today every `ReadLocation`, `Step`, and `Los` on a placed map rebuilds the composed map (N lookups, a dictionary, `MapLayout.Create`); after A3 it is built once a state | Task 1's game followed before and after, with the decorator counting `Composed` builds | 25% of that step's work |
| Any other step's work, cd-guards-5 | 104 ms (section 8.1) | the same, or a few ms lower | Run 1 | (must not rise) |

What A3 cannot buy, stated so the measurement is not read as a failure: the route searches themselves (`RoutAdvice`, about 117 ms of a Rout Phase step) and the two `Resolve` calls in each step stay; A3 removes the lookups around them.

## 2. The code A3 changes (read on `main` at 97666623)

- **Every lookup goes through the planner's `boards` field** (`GamePlanner.cs:129`). Its funnels: `ReadLocation` (`GamePlanner.Map.cs:25`, 64 callers), `Step` (`Map.cs:80`, 5 callers), `Los` (`Map.cs:113`, 24 callers, one-board branch), `HexDistance` (`GamePlanner.CloseCombat.cs:1214`), `Neighbors` (`CloseCombat.cs:1192`), `Across` (`GamePlanner.Terrain.cs:82`), and smaller ones (`Snipers`, `Ordnance.Bearing`, `Recall.EdgeSides`, `LosToHexside`, `CrossesTerrainInTargetHex`). One-off reads during a proposal and at setup stay as they are.
- **`Composed`** (`GamePlanner.cs:2054-2073`) builds a `ComposedMapRead` on every call: `Placements()`, a lookup and a `Map.Board` scan per board, then `ComposedMapRead.Create` with its dictionary and `MapLayout.Create`. 13 call sites, among them `ReadLocation` and `Step`. Nothing is kept.
- **Per `InfantryStep`:** on one board, one lookup and two `Resolve` (two lookups in the same hex); on a placed map of N boards, N lookups, two `Resolve`, and one `Composed` build (2N and two in the same hex).
- **`MapInPlay.Board`** (`Units/State/GameTypes.cs:124`) is `Boards.FirstOrDefault(placed => placed.Board == board)`, a capturing lambda per call; Play calls it 15 times.
- **The caches keyed by a handle** (`BoardLosMaps`, `LosReads`, `Map.cs:123-125`) keep working: the catalog returns the same handle object for a loaded board (`StudioBoardCatalog`, asserted by `StudioBoardCatalogTests`). A placed map's LOS map is cached by a key string (`LosMap.ForPlacedMap`), not by handle.
- **A map is fixed for a game:** `GameState.Map` comes from `GameStarted` alone, each board's version pinned. "Placed" is `IsPlaced`: every board has a slot, which a single board may have too.
- **The one way a kept handle can go stale is a footprint:** `StudioBoardCatalog` reads `CampaignPlayableFootprints` on every call; a footprint published while a state lives would give later calls the restricted handle (`StudioBoardCatalogTests.NewlyPublishedFootprintReplacesUnrestrictedCachedHandleAndSurvivesRestart`). A footprint once accepted is never replaced.

## 3. The order of work in every task

The Play Step Plan's rule, unchanged (the user, 2026-10-19): **implement, build, test in my Studio, rework until the Studio is right, and only then run the unit test classes the change reaches.** The Studio test is the Play Step Plan's section 2.1 (`code-builders/diagnostics/play-step/README.md`): my Studio on 6670 against a Studio built from `main` on 6671, the same game and revision, behaviour first and then the figures of section 1. Each Studio's tab in front while it is measured. Whole test projects run only at the gate.

## 4. Decisions

| | Decision |
|---|---|
| W1 | **One branch, `map-context`, from `main`; one pull request,** green CI before the merge |
| W2 | **The rules do not change.** A3 asks the same map questions and gets the same answers, with fewer lookups. The proof is the gate's (`CODE_MOVE=1`): the text list, the replay digest, and the sweep of every cut of p31c-tw equal to the baseline, the test game of task 1 included |
| W3 | **The map context lives in the planner's per-state memo** (`PlayStepMemo`, ruling P4 of the analysis): per board, the handle and its read result; for a placed map, the composed read, built at its first use. Filled concurrently, as the memo's other stores |
| W4 | **A footprint published while a state lives is read from the next state on** (the analysis, section 4, A3: a state is a snapshot). A Campaign Studio that publishes a footprint sees it from its next step. Open to the user's veto; the alternative is to keep the footprint read on every call, which keeps most of the single-board lookup's cost |
| W5 | **`MapInPlay.Board` becomes a plain loop in Units,** not a planner-side copy: the same answer for every caller, no change to any record's shape |
| W6 | **The "before" Studio is built outside the repository,** in a worktree of `main` at `E:/Archive/GitHub/dlandi/before-main` (launch config `map-studio-before-6671`), never under MapStudio's `bin` (the standing rules, 2026-10-19) |
| W7 | **Out-of-scope findings go to the backlog,** never into the branch unasked |
| W8 | **Every build output outside the repository is registered as it is made and removed at the earliest opportunity** (the standing rules, section 0, point 14): section 5 lists them; the gate's `ci` stage removes them once CI passes; task 5 confirms nothing is left |

## 5. Build outputs outside the repository

| Output | Where | Registered by | Removed |
|---|---|---|---|
| My Studio's build | `E:/Archive/GitHub/dlandi/asl-gate-work/studio-6670` | task 0, at its first build (`outside_outputs.sh register`) | the gate's `ci` stage, once CI passes |
| The "before" Studio: a worktree of `main` and its build | `E:/Archive/GitHub/dlandi/before-main` | task 0, as the worktree is added | the same; the worktree is moved out and pruned |
| The baseline's copy of `main` and the proof tools | `asl-gate-work/main-src`, `main.tar`, `main-bin`, `proof`, `proof-gate`, `proof-main` | `gate.sh`, as it builds them | the same |
| The measurement decorator of task 2 | my Studio's build only (above) | (inside my Studio's build) | with it |

Nothing is built under MapStudio's `bin` but the gate's `bin/gate` and the user's `bin/Debug`.

## 6. Tasks

### Task 0. The branch, the baseline, the "before" Studio

- Cut `map-context` from `main`. Write `code-builders/merge-gate/pass-configs/passa3.conf` (`CODE_MOVE=1`, `ABSENT="MapContext"`). Run the gate's `base` stage in the background.
- Build `main` in its worktree outside the repository (W6) and start it on 6671. Register the worktree and my Studio's build as each is made
  (`bash code-builders/build-and-test/outside_outputs.sh register <path>`; section 5, W8).
- **Done when:** the branch exists, the baseline is made, the "before" Studio runs.

### Task 1. The placed-map test game (the testing rules: a game for a change that alters how a placed map is read)

- `GameFile` (`code-builders/test-games/game_file.py`) gains a placed map: the Gambit card's two boards, bd04 at (0,0) and bd02 at (0,1) reversed, with the versions the Studio computes. A script under `code-builders/pass-history/a3-map-context/` writes `a3-gambit-rout`: squads and a leader of both sides on both boards, several broken units whose nearest woods or building lies across the seam between the boards, at the German Advancing Fire Phase of turn 1.
- In the "before" Studio, played into and through the Rout Phase with scripted dice (each rout, the phase's end), so the record holds about 8 to 12 steps the follower can run. The game file is the gate's own game (`OWN`), swept on the baseline's side too.
- **Studio:** the game opens on both boards; the Rout panel offers routes across the seam; the record replays.
- **Done when:** the record exists and replays in both Studios.

### Task 2. Measure first

- The counting, timing decorator of the Play Step Plan's task 7, in my Studio's build only, never committed, extended to count `Composed` builds, `Resolve` calls, and `MapInPlay.Board` calls per step. Read through the follower: cd-guards-5, 713 to 760 (and 621, 809), and `a3-gambit-rout` whole.
- **Decide by section 1's thresholds:** the single-board part (W3's handles) is built if the lookups' time is 15 ms a Rout Phase step or more; the placed-map part (W3's composed read) is built if its builds are 25% or more of a Rout Phase step on the Gambit game. Count also how often the same Location is resolved twice in a state: the number section 9 needs for A3b.
- **Done when:** the counts and times are written in the plan's results (section 10), with the decision.

### Task 3. The map context

- **Implement** (as task 2 decided): `MapContext` in `PlayStepMemo`, the funnels of section 2 reading their handle from it, `Composed` returning the kept read; `MapInPlay.Board` as a loop (W5).
- **Studio:** at cd-guards-5's 621, 622, and 729 to 744, and at each step of `a3-gambit-rout`, the Rout panel, the unit table, and the map's routes read the same as the "before" Studio, as the adjudicator and as each side. The short run on cd-guards-5 and the Gambit record against section 1; the allocation trace of 621 to 622.
- **Then the tests:** a new planner test with `CountingBoardCatalog` (one lookup per board per state, asked twice; a placed map composed once per state); a new test of W4 (a footprint published while a state lives: the state keeps its handle, the next state reads the footprint); the classes the change reaches, by grep at the time: today `SeamEntryTests`, `BacklogPass26Tests`, `BacklogPass20Tests`, `BacklogPass21Tests` and their table player classes, `BacklogPass25Tests`, `StudioBoardCatalogTests`, `TablePlayer22Tests`, and in Units `MapPlacementTests`.
- **Done when:** the Studio shows the gain and no difference; the tests pass.

### Task 4. The whole Studio measurement

- Run 1 on cd-guards-5 (the whole game), the Gambit record followed whole, the allocation trace of 621 to 622; written as the plan's section 10 beside section 1's table, figure by figure, each marked met or not.
- **Done when:** the results are written.

### Task 5. The gate, the documents, the pull request

- `gate.sh a3 <base> strict`, then `baseline`, `build`, `proofs` (W2). The plan's status, the analysis's section 8.5 and backlog 65 brought up to date, the time log.
- Push the branch, open the pull request, read its CI run (`gate.sh a3 <base> ci`, which removes the registered outside build outputs once
  CI has passed); then `outside_outputs.sh list` must report nothing, or what holds an output is stopped and `clean` run again (W8). The
  merge waits for green and the user's word.

## 7. Order and time

| Task | Estimate |
|---|---|
| 0. Branch, baseline (background), the "before" Studio | 20 min |
| 1. The placed-map test game, recorded | 45 min |
| 2. Measure first | 30 min |
| 3. The map context | 1 h 15 |
| 4. The whole Studio measurement | 30 min |
| 5. The gate, the documents, the pull request | 40 min, and CI's 14 min |
| **In all** | **about 4 hours** |

Task 2 may end the work early: if neither part reaches its threshold, the plan's results say so and nothing is built.

## 8. Risks

| Risk | Guard |
|---|---|
| A kept handle misses a footprint published mid-state | W4 stated and tested; the next state reads it |
| A placed map's composed read kept from a state it does not belong to | Kept in the per-state memo only, keyed by the state object; the gate's sweep over the Gambit record |
| The LOS caches keyed by a handle stop hitting | The same handle object is kept (section 2); the Studio's figures would show a rise |
| The test game does not stage a rout across the seam | Task 1's Studio check reads the Rout panel's routes before the game is recorded |
| The measurement misleads | The Play Step Plan's V7: time from step intervals and outer frames, allocation from allocation ticks, counts from the decorator |

## 9. For the user to rule on

| # | Question | Proposal |
|---|---|---|
| Q1 | W4: a footprint published while a state lives is read from the next state on | **Yes:** a state is a snapshot; the alternative keeps most of the lookup's cost |
| Q2 | A3b: keep each resolved Location (`BoardHandle.Resolve`'s `LocationRead`) in the map context too, which A3 alone does not remove (about 23 MB of the heaviest step, and the time of two `Resolve` per step) | **Decide after task 2:** it reports how often a state resolves the same Location again; propose A3b then if the repeats are high, as its own task, not built unasked |

## 10. Results

To be written by tasks 2 and 4.
