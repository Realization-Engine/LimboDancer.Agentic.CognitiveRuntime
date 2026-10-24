# ASL Map Studio Berserk Charge Plan

**Written:** 2026-10-23, at the user's word: "Yes, plan the fix", after the replay of `dl-tractor-works-02` (memory note `berserk-charge-slowness`). The user's rule for this work, the same day: "test everything in the Studio. Do not run ALL unit tests until the gate. You are permitted to run specific unit tests that impact code you've changed. Standing Rule: Do Not run unnecessary tests."
**Scope:** the berserk charge search's cost in Play, in one branch and one pull request: T1 (the charge answer kept per state) and T2 (C2, the index of a state's objects, used by the charge search). No rule changes. Not in scope: any other use of the index, the Rout Phase's remaining cost, and the Turn 6 pauses the replay did not reproduce (section 8).
**Status:** approved 2026-10-23 at the user's word: "Proposals C1, C2 and C3 accepted. Plan approved. Proceed. Start on the branch berserk-charge-memo. Attempt to execute without my intervention." Built on `berserk-charge-memo`: task 1 (8841f234), task 2 (c69dd542), task 3 (07a439ba, results in section 9). Task 4: the gate passed 2026-10-23 (strict, then baseline, build, proofs with `CODE_MOVE=1`: the text list, replay digest, and sweep equal the baseline's in every line; the Node viewport test 13 of 13). The merge waits for a green CI run and the user's word.

## 1. Expected gains

Measured 2026-10-23 on `main` at 5c178fc8, my Studio on port 6670 (Debug, built outside the repository), `dl-tractor-works-02` followed a step a second, the tab in front, nothing else building.

| Figure | Baseline | Expected after T1 | Expected after T2 | How measured | Stop threshold |
|---|---|---|---|---|---|
| Work of the step to revision 1316 (Russian MPh, Turn 5; `r-squad-10` berserk) | 10.9 s (adjudicator), 11.1 s (repeat), 12.0 s (Russian view) | about 2.3 s | under 1 s | Follower cut to 1305, one step to 1315 to warm the caches, then the 1316 step timed by the page (`steplog.js` interval less the one-second pause) | T1 is dropped if the step is not under 4 s. T2 is dropped if one search does not fall by at least half (step not under 1.5 s) |
| `ChargeSteps` calls for one state | 5 in the 1316 step; 3 per later render (2.2 s each) | 1 per state; 0 on later renders | 1 per state | `dotnet-sampled-thread-time` trace, outermost entries of `GamePlanner.ChargeSteps` (`trace_calls.py`) | T1 is not done until a later render of the same state shows none |
| One charge search (the 1316 state) | 2.2 s | 2.2 s | 0.5 to 1.0 s | The same trace: the duration of each outermost `ChargeSteps` entry | As above |
| A render of the 1316 state after a hand-over to another view | about 7 s (3 searches) | under 0.3 s | under 0.3 s | Page stopwatch: perspective change to hand-over screen, then the confirm click to the view drawn | |
| Allocation in the 1316 step | about 1.4 GB (150 MB/s for 9 s, counter estimate) | about 300 MB | under 150 MB | `gc-verbose` trace of that one step, read with `allocread.cs` (allocation ticks only; the counter stalls) | |
| Ordinary steps, whole game | about 0.13 to 0.2 s of work; Rout Phase 1 to 2 s | unchanged | unchanged | Whole-game follower run, split by phase (`split.py`) | Any phase median more than 15% worse than the baseline run, unexplained, stops the merge |

The figures after T1 and T2 are estimates from the trace: `At`, `Location`, and `Find` are about 67% of the search's time (7.3 of 10.9 s), and the rest is the bypass rules and map reads. They are what each task is judged against, not limits that hold on every machine.

## 2. The cause (the replay's findings)

- `GamePlanner.ChargeSteps` (`GamePlanner.CloseCombat.cs:1365`) asks Rules' `ScenarioA1ChargeCalculator.Steps`. Its `FirstMoves` and `Edges` ask `ChargeScan.BypassHalfMf` for many Locations, which calls `BypassStep` (`GamePlanner.Terrain.cs:305`).
- `BypassStep` reads the units in the target Location with `state.At(to)` (line 310). `ChargeScan.Occupants` does the same (line 1355).
- `GameState.At` (`GameState.cs:445`) calls `Location(id)` for every object, and `Location` calls `Find`, a linear scan of every object. One `At` costs objects squared. The object list keeps eliminated units and the units made during play, so the cost grows every turn.
- The page and the planner run the whole search several times for one state:
  - `Charges` runs `MustCharge` and then `ChargeSteps` again for each unit.
  - `ChargeLanes` runs it again for the markup (`Play.razor:644`).
  - `Refresh` and the sequence bar run `MustCharge` again (`GamePlanner.cs:747`, `Play.razor:6624`, `6634`).
- While a berserk unit still has to charge, every render of the MPh pays this again. A charge answer reads only the state: `KnownEnemy` reads the unit's own conditions, not a viewer.

## 3. Decisions

| | Decision |
|---|---|
| D1 | **One branch, `berserk-charge-memo`, from `main`; one pull request.** A green CI run on it before the merge, and the user's word |
| D2 | **The rules do not change.** No calculator in the Rules project is touched; Play asks Rules the same questions with the same facts, fewer times and faster |
| D3 | **The proof that no decision changed is the gate's replay proofs** with `CODE_MOVE=1` (a pass config like `passplaystep.conf`): the replay digest and the sweep must equal the baseline in every line |
| D4 | **Each task: implement, build, the Studio test (section 4), rework until right, then only the named test methods (section 5).** The whole projects run once, at the gate. No page test that a new unit test already covers |
| D5 | **Both stores live in the planner's per-state memo** (`PlayStepMemo`, `GamePlanner.PlayStepMemo.cs`), as the guidelines and ruling P4 require: never in `GameState`, never process-wide |
| R1 | **Made in task 1 under the user's word to go on without intervention, open to a veto:** the tests see "searched once" through an internal count of the planner's charge searches (`GamePlanner.ChargeSearches`), with `InternalsVisibleTo` for the Play test project only; no timing in a test, and nothing public added |
| R2 | **Made in task 2, the same way:** no recorded game holds an ordinary Bypass move, so `BypassStep`'s use of the index outside a charge is proved by the three Bypass test methods and the gate's sweep, not in the Studio |
| D6 | **Fixtures:** copies only. `dl-tractor-works-02` is followed (the follower is a new game); the user's file is never written. `demo-min-berserk` and `p35-berserk-deploy` for the behaviour check |

## 4. The Studio test

The same each time, so the figures compare with section 1:

1. **Stop my Studio, build** outside the repository (`-p:OutDir=E:/Archive/GitHub/dlandi/asl-gate-work/studio-6670/`, registered with `outside_outputs.sh`), **start it** on 6670 (`map-studio-p31b`).
2. **Behaviour, against a "before" Studio** (main built outside the repository in a detached worktree, port 6671, registered):
   - At revision 1316 of a `dl-tractor-works-02` follower, as the adjudicator and as the Russian side: the charge panel's unit, target, next Locations, and Bypass lanes; the sequence bar's berserk line; the move form's preselected charge.
   - Then propose the recorded charge by hand (`r-squad-10` to [X3]) on a cut copy: accepted in both Studios.
   - `demo-min-berserk` through its charge, the same panel and proposal in both.
3. **Measurement:** the 1316 step and the hand-over render (section 1), with a trace for the call counts. Task 3 adds the whole-game run and the allocation trace.
4. **Verdict:** the expected effect is seen and nothing on the page differs; else rework and test again.

## 5. Tasks

### Task 0. The branch, the baseline, the "before" Studio

- Cut `berserk-charge-memo` from `main`. Run the gate's `base` stage in the background.
- Build `main` for the "before" Studio (section 4, step 2). Commit the scripts this work wrote (`audit_spans.py`, `steps_by_phase.py`, `trace_inclusive.py`, `trace_calls.py`, `counters_series.py`) under `code-builders/diagnostics/play-step/`, with lines in its README.
- **Done when:** the branch exists, the baseline runs, both Studios start.

### Task 1. T1: the charge answer kept per state

- **Implement:** a memo store `Charge`, keyed by (the movers' ids in order, the start Location, whether the movers are in the state's current move). `ChargeSteps` reads it first and fills it after the search.
  - It is kept only when every mover is the state's own instance (reference check against the state's unit) and `current` is null or `state.Movement` itself. Any other call is computed as today (the guidelines: a changed unit object must not reuse the state's answer).
  - The kept value is the returned tuple; its dictionary is never handed out for writing.
  - `Charges` keeps its order and output. Its second search for a unit `MustCharge` already asked becomes a memo hit.
- **Studio:** section 4. Expected: the 1316 step about 2.3 s; one `ChargeSteps` entry in the step's trace; none on the hand-over render.
- **Then the tests:**
  - New, in `LimboDancer.Domains.Asl.Play.Tests`:
    - the same state asked `Charges` twice is searched once (a counting seam, or the memo's count, decided at the task);
    - a new state is searched again;
    - a unit object that is not the state's own is not served from the store.
  - Existing, by name:
    - `BacklogPass27Tests`: `ABerserkChargeTakesTheShorterRouteInBypass`, `ABerserkChargeClimbsAStairwellToAnEnemyUpstairs`, `ABerserkChargeIntoConcealedUnitsDrawsDummiesOnlyUntilARealUnit`, `ABerserkOverrunOfALoneSmcHasItsCcAtOnceInTheMph`, `ALoneBerserkLeaderEntersAnSmcsLocationWithoutAnOverrunCc`;
    - `CloseCombatStepsTests`: `U34ABerserkResultMakesTheSquadChargeTheNearestKnownEnemyInItsNextMph`, `ABerserkUnitAbandonsItsMmgBeforeChargingAndChargesIntoPrisoners`, `ABerserkUnitWithAKnownEnemyMustHaveItsCcBeforeTheCcphEnds`;
    - `BacklogPass10Tests`: `ABerserkChargeOverrunsALoneSmcAndRevealsConcealedUnitsOnItsRoute`;
    - `OrdnanceStepsTests`: `ABerserkChargeIntoACrewsLocationEndsInPlace`;
    - `VehicleStepsTests`: `ABerserkChargeAtAnEnemyVehicleEntersItsLocationAndAttacksIt`, `AVehicleWaitsForABerserkUnitsCharge`.
    - That is 12 methods, filtered by method.
- **Done when:** the Studio shows the drop and no difference; the tests pass. One commit with its tests.

### Task 2. T2: C2, the index of a state's objects, in the charge search

- **Implement:** a memo store built once per state at first use:
  - objects by id;
  - the active objects by `BoardLocation`, built with the state's own `Location` and `Aboard`, so it holds exactly what `GameState.At` returns, in the same order.
  - Planner helpers read it. Their use, at ruling C1 (section 6): `BypassStep`'s `there`, `ChargeScan.Occupants`, and the `state.Location` calls of `ChargeSteps`' enemy list. `GameState` itself is not changed.
- **Studio:** section 4. Expected: one search 0.5 to 1.0 s, the 1316 step under 1 s. If a search does not fall by half, T2 is reverted and recorded as measured and not built.
- **Then the tests:**
  - New: the index's answer equals `GameState.At` for every Location of a state holding a vehicle with Passengers, a held SW, a manned gun, an eliminated unit, and units at two levels of one hex; and equals `Find` for every id.
  - Existing: the 12 methods of task 1, plus the Bypass movement methods that reach `BypassStep`, named by grep at the task (kept under 25 in all; narrowed first if more).
- **Done when:** as task 1.

### Task 3. The whole Studio measurement

- The whole-game follower run of `dl-tractor-works-02` (all 494 steps) on the branch, split by phase, against the 2026-10-23 run. The `gc-verbose` trace of the 1316 step. The hand-over render at 1316 in each view.
- The results go in this plan's section 9, against section 1.
- **Done when:** the results are written, and any phase that regressed is explained or fixed.

### Task 4. The gate, the pull request, the documents

- The gate: `strict`, then `baseline`, `build`, `proofs` with `CODE_MOVE=1`, and the Node viewport test by hand. The digest and the sweep must equal the baseline.
- Update the time log, this plan's status, the guidelines (C2 is built, for the charge search), and the backlog (section 8).
- Push, open the pull request, and read its CI. The merge waits for green and for the user's word. The outside outputs are cleaned by the gate's `ci` stage; `outside_outputs.sh list` must be empty.
- **Done when:** CI is green and the user has the results.

## 6. Rulings

All three proposals accepted by the user, 2026-10-23.

| | Question | Ruling |
|---|---|---|
| C1 | Where the index of task 2 is used | **The charge search only** (`BypassStep`, `ChargeScan.Occupants`, `ChargeSteps`' enemy list). `BypassStep` also serves ordinary Bypass moves, which then use the index too, with the same answers. Every other `state.At` and `Find` stays as it is; the guidelines say an index is extended only when a measurement shows its benefit. Wider use goes to the backlog with the figures of task 3 |
| C2 | Whether T2 is built at all if T1 alone brings the 1316 step under 4 s | **Yes, measured first.** T1 removes the repeats, but one search is still 2.2 s now and grows with the game. T2 is kept only at section 1's threshold |
| C3 | Whether the page's repeated reads of `Charges` (the markup, `Refresh`, the sequence bar) are also cut in `Play.razor` | **No.** With T1 they are memo hits, cheap by comparison. Leaving the page alone keeps the change in the planner and needs no page tests |

## 7. Design checklist (the guidelines, section "Design and review checklist")

- **Added work:** per state, at most one charge search per berserk stack and start, and one index built at first use (one pass over the objects, each object's Location resolved once). Read through the planner's existing memo, by the existing charge and Bypass paths.
- **Ownership, key, lifetime:** the planner's `ConditionalWeakTable` keyed by the state object; collected with the state. Key inputs are complete: movers by id with a reference check, start, and the move (from the state). No invalidation, since a state never changes. Concurrent fills compute the same answer; either is kept.
- **Disclosure, rout settlement, snapshots, versions, seams:** the charge answer reads no viewer, so one answer serves every view, as the page already shows it today. The memo's rout stores and their surrender window are untouched. Map reads still go through `ReadLocation` and the state's handles.
- **Counters:** not touched.
- **Regression checks:** the Studio test (section 4), the 12 named methods and the new ones, the gate's digest and sweep, and the whole-game run by phase.
- **Benefit against noise:** the expected change is from 11 s to under 1 s, far beyond the ±40 ms run noise.
- **Memory:** a few charge answers per MPh state with a berserk unit; one index (two dictionaries of a few hundred entries) per state the charge or Bypass path asks. Both are small beside a state and are collected with it.
- **Recorded before completion:** results (section 9), backlog (section 8), outside outputs (task 4).

## 8. Out of scope, for the backlog (section 70, added by this plan)

- **Turn 6 pauses.** The replay did not reproduce the Turn 6 pauses the user saw:
  - every Turn 6 step was normal in all three views;
  - a fire proposal took 0.38 s and its confirm 0.39 s;
  - the audit log shows the session's checks under 1.6 s.
  - Next: the user's own Studio (Debug under Visual Studio, a long session) is the next place to look, or a recording of the moment it happens.
- **Other `state.At` and `Find` uses.** The index is not used in the rest of the planner and the page (ruling C1). Extend it only with a measurement.
- **The Rout Phase.** Rout Phase steps still cost 1 to 2 s of work in this game, against about 0.2 s for other steps (Turn 1, revisions 422 to 444; Turn 2, 659 to 662).

## 9. Results

Measured 2026-10-23 on the branch (c69dd542) against `main` (5c178fc8), two Studios built the same way, each run with its tab in front and nothing else building. A first "after" run was discarded: a polling timer, throttled while the app was not in front, rounded its intervals to whole seconds from revision 859 on. Both runs below log with the page's observer, as `steplog.js` does.

| Figure | Baseline (main) | After T1 | After T2 (the branch) | Verdict |
|---|---|---|---|---|
| Work of the step to 1316 | 12.8 s (whole run); 10.9 to 12.0 s (single steps, section 1) | 2.1 s (single step, under the gate's load) | 0.37 s (whole run); 0.56 and 0.68 s (single steps) | Met (under 1 s) |
| `ChargeSteps` searches for the 1316 state | 5 in the step, 3 per later render | | 1 in the step (from `MustCharge`), 0 in the hand-over render | Met |
| One charge search | 2.2 s | 2.2 s | 0.29 s | Met (more than halved) |
| Allocation in the 1316 step | about 1.4 GB (counter estimate) | | about 102 MB (allocation ticks, `allocread.cs`) | Met (under 150 MB) |
| A render after a hand-over to the Russian view at 1316 | about 7 s (trace, section 1); 6.1 s (stopwatch, under load) | 1.5 s | 1.6 to 2.3 s | **Not met (target 0.3 s).** No charge search is left in it; the rest is the victory check and the unit names catching up for the new viewer (backlog 70) |
| Ordinary steps, whole game (430) | median 196 ms, p90 274 | | median 151 ms, p90 222 | No regression |
| Rout Phase steps, whole game (63) | median 379 ms, p90 878 | | median 309 ms, p90 675 | No regression |

Both runs hold one or two lone stalls at steps the other run took normally: main at 1305 (3.9 s) and 1362 (1.5 s), the branch at 1393 (3.8 s, a German move's window closing, with no berserk unit or Bypass in it). They are the store's intermittent append stall (backlog 65), not this change.

Behaviour (section 4): at 1316, as the adjudicator and as the Russian side, the charge line reads the same in both Studios ("4-4-7 squad R10 in [X4] is berserk and charges [X3] ... next steps [W4] or [X3]"); the charge proposed by hand (R10 to [X3]) was accepted in both, with the event the user's game recorded at 1317. `demo-min-berserk` has no berserk unit despite its name; both Studios show no charge line in it. No recorded game holds an ordinary Bypass move (ruling R2).

Tests run, by method only: 15 for T1 (3 new and the 12 named in task 1), then 19 for T2 (the index test, the 3 Bypass methods, and the 15). All passed.

## 10. Order and time

| Task | Estimate |
|---|---|
| 0. Branch, baseline (background), "before" Studio, scripts | 25 min (and 25 min in the background) |
| 1. T1 | 50 min |
| 2. T2 | 1 h |
| 3. The whole Studio measurement | 30 min |
| 4. The gate, the pull request, the documents | 40 min, and CI's 14 min |
| **In all** | **about 3 h 30** |

Each task's time includes its Studio test (a build, a restart, the 1316 step, and the behaviour check: about 10 minutes).
