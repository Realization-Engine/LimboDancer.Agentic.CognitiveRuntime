# ASL Unit Time Log

Wall-clock durations per task and sub-task, taken with `date "+%Y-%m-%d %H:%M"` at each boundary. Estimates come from the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>) kickoff notes.

Past actuals for reference: steps 19 to 23 about 8 h; the six Play page fixes about 1 h; pass 1 (steps 26 to 28) about 3 h; the Studio scripted dice about 0.5 h.

## Pass 2: steps 29 and 30, fixes A and B

Estimate: 4 to 6 h (fixes A and B 0.5 h each). Kickoff 2026-09-27 09:31.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Kickoff to session start | 09:31 | 09:39 | 0:08 | Handoff; session started |
| Plan reading (plan, rule pages, code) | 09:39 | 09:47 | 0:08 | Plan, A11, A15.4 to A15.5, A20, A3, A4.7 pages; projector, planners, packages |
| Fix A: Prep Fire units may not move | 09:47 | 09:51 | 0:04 | Planner, projector (UNIT-STATE-029), Play page; test |
| Fix B: fire selection and Director list | 09:51 | 09:53 | 0:02 | Reset on game change, prune to the From Location, Good Order directors; test |
| Review stage, first pass | 09:53 | 10:31 | 0:38 | CCT transcription, two PDF comparisons (42 subjects), Close Combat package, Heat of Battle Berserk and Surrender in Fire and Rally, walks |
| Referee review (background agent) | 10:31 | 10:52 | 0:21 | Ran alongside the live stage; 13 defects, 11 disputed readings |
| Live stage | 10:31 | 10:52 | 0:21 | Records, projector, verifier, planners (Advance, Ambush, CC, charge, capture), Play page, U33 and U34 |
| Referee fixes | 10:52 | 11:15 | 0:23 | Withdrawal from Melee built for D3; D1 to D13 and seven disputed readings fixed with tests |
| Table-player review (background agent) | 11:15 | 11:27 | 0:12 | 4 freezes, 8 other differences, page findings |
| Documents (review, design, plan rulings, backlog, requirements) | 11:15 | 11:22 | 0:07 | Alongside the table-player review |
| Table-player fixes | 11:27 | 11:46 | 0:19 | Freezes, sequential Ambush, mandatory CC, withdrawal control and page usability; tests in Units, Play, MapStudio |
| Merge gate (full local suite, Docker Linux check) | 11:47 | 12:04 | 0:17 | All projects pass locally and in Docker; pass 3 reading alongside |

Pass 2 total: 09:31 to 12:04, 2:33 against the 4 to 6 h estimate. Fixes A and B took 0:06 together against 1 h. The review stage (0:38) and the live stage (0:21, alongside the referee) were the largest items; the two second-pass reviews and their fixes took 1:15 with the documents.

## Pass 3: step 24, ordnance

Estimate: 5 to 8 h. Kickoff 2026-09-27 12:04, at the pass 2 merge.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (Chapter C, Chapter H Gun listings, NCC, TH chart render) | 11:50 | 12:08 | 0:18 | Mostly alongside the pass 2 merge gate |
| Catalog 1.4.0 (two crews, two Guns, 78 rows) | 12:08 | 12:20 | 0:12 | Referee confirmed all 78 rows (12:14 to 12:18, alongside) |
| Review stage (C3 transcription, 49-subject comparison, Ordnance package, Fire ordnance-hit mode, walks) | 12:20 | 12:38 | 0:18 | Fire, Rally, and CC packages revised for the catalog digest |
| Referee review (background agent) | 12:38 | 12:57 | 0:19 | Ran alongside the live stage; 11 defects, 4 disputed readings |
| Live stage (records, projector, verifier, planner, Covered Arc, Play page, U28) | 12:38 | 13:00 | 0:22 | Found and fixed a pass 2 defect: berserk companions' NTC rolls in live Fire and Rally |
| Referee fixes | 12:57 | 13:03 | 0:06 | D1 to D10 fixed or recorded; readings A and C adopted |
| Documents (review, design, backlog, catalog design, requirements) | 13:03 | 13:15 | 0:12 | Alongside the table-player review and the full suite |
| Table-player review (background agent) | 13:04 | 13:19 | 0:15 | 3 stuck games, 6 other differences, page findings |
| Table-player fixes | 13:19 | 13:27 | 0:08 | Crews in CC, advances, and charges; Melee crews; concealed crews; facing check; stacking; page |
| Merge gate (full local suite, Docker Linux check) | 13:28 | 13:42 | 0:14 | All projects pass locally and in Docker; the branch push was refused by the auto-mode classifier; the user then approved pushing, and main and both branches were pushed |

Pass 3 total: 12:04 to 13:42, 1:38 against the 5 to 8 h estimate (with 0:14 of reading before the kickoff, alongside the pass 2 merge gate). The two second-pass reviews ran alongside other work; their fixes took 0:14 together.

## Studio demo of passes 2 and 3

Asked by the user after the pass 3 merge; not in the plan's estimates.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Demo: a Gun's shot, Advance, and Close Combat (`pass23-demo`) | 15:55 | 16:09 | 0:14 | Studio with scripted dice; one finding (a crew manning a Gun offered in the Advance panel) |
| Demo: Ambush, Melee, Berserk, and Surrender (`pass2-ambush`, `pass2-hob`) | 16:09 | 16:56 | 0:47 | Eight more findings, recorded in the backlog, section 13 |

## Play page fixes from the Studio demo

Approved by the user at 16:57 (outside the plan); estimate 1 to 1.5 h. Branch `feature/asl-play-page-fixes`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (page, planner refusals, package codes) and branch | 16:57 | 17:00 | 0:03 | |
| Fixes and tests (nine findings) | 17:00 | 17:09 | 0:09 | RefusalReasons, FireBar, FireSpent, DeclaringSides, Charges; page panels; Play and Studio tests |
| Visual check on the Studio, with its fixes | 17:09 | 17:26 | 0:17 | Games `fix-hob`, `fix-ambush`, `fix-charge`; found crews offered in the Fire and Movement panels and an undecided charge left unmarked, all fixed |
| Merge gate (full local suite, Docker Linux check) | 17:26 | 17:56 | 0:30 | All projects pass locally and in Docker; pass 4 reading alongside |

Studio demo total: 15:55 to 16:56, 1:01. Play page fixes total: 16:57 to 17:56, 0:59 against the 1 to 1.5 h estimate; the visual check (0:17) found three more gaps, fixed before the merge.

## Pass 4: step 25, vehicles

Estimate: 6 to 9 h in the plan; 3 to 4 h from the recent actuals (given to the user at 17:36). Kickoff 2026-09-27 17:56, at the fixes' merge.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (Chapter D 1 to 5, alongside the fixes' merge gate) | 17:36 | 17:56 | 0:20 | D.1 to D.8, D1, D2.1 to D2.5, D3.1 to D3.7, D5.1 to D5.8 |
| Review stage (catalog 1.5.0 and vocabulary 1.8.0, Vehicle line and vehicle MP transcriptions, 43-subject comparison, Fire package revision, tests) | 17:56 | 18:29 | 0:33 | Rally, Close Combat, and Ordnance packages revised for the catalog digest |
| Live stage (vehicle planner, projector, gate, refusals, Play tests, Play page) with the referee's ten defects fixed alongside | 18:30 | 19:29 | 0:59 | Referee report at about 18:50; catalog rebuilt for `asl:cs-passengers-only`, A8.2 and A10.31 added to the comparison, packages re-pinned |
| Visual check on the Studio, with its five page fixes; the table player's thirteen findings (review in the background) fixed with tests | 19:29 | 19:51 | 0:22 | Games `pass4-vehicles-2` and `pass4-vehicles-3`; one blocker (a berserk charge at a vehicle) and nine other fixes, three recorded deviations |
| Documents (plan rulings, review, design, requirements, backlog, catalog design) | 19:51 | 19:55 | 0:04 | |
| Merge gate, first run (full local suite and Docker on c4a0623), with the user's look at the vehicle panels | 19:57 | 20:22 | 0:25 | Failures in projects not run during the pass: the Authoring matrix pins and source review, the Units synthetic catalog list, the Rendering goldens for the new states (badges added), and the page's "left@movingNow" text; all fixed |
| Merge gate fixes and reruns (the Authoring source review's two-part subjects, D1.2 in the vehicle-outside case, re-pinned digests; second local run; Studio check of the state line; Docker on 9d4b286) | 20:22 | 21:10 | 0:48 | All projects pass locally and in Docker |
| Merge and push | 21:10 | 21:12 | 0:02 | |

Pass 4 total: 17:36 to 21:12, 3:36 against the 3 to 4 h estimate (6 to 9 h in the plan). The build itself (reading to documents) took 2:18; the merge gate took 1:15, of which 0:48 went to failures in projects not run during the pass (Authoring, Units, Rendering), so later passes should run the full local suite before their first commit.

## Backlog housekeeping and the backlog passes plan

Asked by the user after the pass 4 merge. Branch `docs/asl-backlog-housekeeping`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Housekeeping: fourteen simplified resolutions gathered in section 1 (from sections 10, 12, and 14), finished rows of sections 6 and 7 removed, section 14 after section 13 | 21:20 | 21:21 | 0:01 | 138 open rows |
| Draft of the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>): every open row in twelve passes, 75 tasks, each estimated | 21:21 | 21:25 | 0:04 | 63:25 of working time estimated, 44:24 to 82:26 |
| The plan rewritten as the plan to follow: each task in play terms with its rules, the way a pass is run, and the Docker check script (appendix A) | 21:44 | 21:46 | 0:02 | Approved by the user as the plan to follow |

## Pass 5: deviations and small items

Estimate: 4:55 (build 3:40) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-27 21:57. Branch `feature/asl-backlog-pass-5`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (plan, backlog, code, rules) and rulings R5.1 to R5.20 | 21:57 | 22:12 | 0:15 | A4.4 to A4.72, A7.302 to A7.309, A8.14, A11, A15, A18, A20, C6.5, D2.1, D2.4, D5.33 to D5.41, A2.6, B15.6 |
| Build of tasks 5.1 to 5.10: state, projector, packages (fire, rally, CC, ordnance), pass 5 source comparison, planner, gate, tests | 22:12 | 23:01 | 0:49 | One block: the split by task was not recorded. 31 source subjects verified; 9 package tests, 14 play tests |
| Referee review and fixes | 23:01 | 23:18 | 0:17 | 6 defects fixed with tests (berserk clears CX, withdrawal IPC, second HoB with no captor, ordnance CX To Hit with a new case, leader bonus refused, R5.13 phases); 5 disputed readings adopted (Massacre as SW use, berserk massacre AFPh/DFPh only, R5.14, R5.16, Rally BH) |
| Live stage: Play page (edges, Double Time, choice panel, reject surrender, Massacre, vehicle exit and intended hex, Recall note, Stun +1, used BU hidden, printed values, per-side CC declarations, SW left unpossessed) and 4 page tests | 23:18 | 23:25 | 0:07 | 34 page tests pass |
| Documents: design, requirements note, backlog (20 rows built, 2 rows trimmed, section 15 with 4 deferrals) | 23:25 | 23:27 | 0:02 | Review document written after the table player |
| Visual check in the Studio | 23:27 | 23:29 | 0:02 | New game with edges, Double Time step into woods: CX shown, box cleared. The pane did not draw, so the check read the page text rather than screenshots |
| Table-player review (agent ran 23:25 to 23:31, beside the documents and visual check) and fixes | 23:29 | 23:35 | 0:06 | 5 defects fixed with tests, 3 notes done, 1 gap already handled, 1 gap to the backlog, 1 refusal added |
| Full local suite (solution and ScenarioA1) | 23:35 | 23:45 | 0:10 | 1,960 passed, 30 skipped, 0 failed |
| Commit f6c727d and Docker Linux check (appendix A) | 23:45 | 23:54 | 0:09 | restore, build (warnings as errors), solution tests, and ScenarioA1 tests all exit 0 |
| Merge into main and push | 23:54 | 23:56 | 0:02 | |

Pass 5 actual: 1:59 (21:57 to 23:56) against the estimate of 4:55.

| Plan task | Estimate | Actual |
|---|---|---|
| 5.1 to 5.10 build (one block; split not recorded) | 3:40 | 0:49 |
| Overhead: reading and rulings | | 0:15 |
| Overhead: referee review and fixes | | 0:17 |
| Overhead: live stage (Play page) | | 0:07 |
| Overhead: table player and fixes | | 0:06 |
| Overhead: documents and visual check | | 0:04 |
| Overhead: full suite, Docker check, merge | | 0:21 |
| Overhead total | 1:15 | 1:10 |
| **Total** | **4:55** | **1:59** |

## Pass 6: vehicles, part 2

Estimate: 4:35 (build 3:20) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-28 00:09. Branch `feature/asl-backlog-pass-6`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (plan, backlog, code, rules) and rulings R6.1 to R6.10 | 00:09 | 00:15 | 0:06 | D9.3, D9.4, D10, B25.14, B25.2, A24.2, A24.8, A8.2, A8.222, D.8B, A12.2, A12.12, A12.4, D2.14, D2.41, D3.3, D3.7, A4.6, A6.1 |
| Review stage: rulings text, Fire and Ordnance package revision (AFV/wreck cover, Residual FP vs vehicles, concealed vehicles, vehicle First and Bounding First Fire), vocabulary 1.10.0, 14-subject comparison, package tests | 00:15 | 00:26 | 0:11 | Fire matrix 197 fragments, 26 cases; Ordnance 53, 13 |
| Referee review (agent ran 00:26 to 00:31) and fixes: rulings R6.1 to R6.10 rewritten, D3.31 halving, Residual FP smoke, Case J by hex entry or Motion, Case H after each step, package texts re-digested | 00:31 | 00:34 | 0:03 | 4 defects and 2 disputed readings fixed with tests; notes to the rulings and backlog |
| Live stage: LOS crossed hexes, wrecks (status, event, Blaze), AFV and wreck cover and Hindrance, smoke, entry costs, Residual FP vs vehicles, vehicle concealment and entry reveals, vehicle First and Bounding First Fire, vehicle MG repair, gate readback, Play page, 11 play tests and 1 page test | 00:34 | 00:49 | 0:15 | 176 play tests and 35 page tests pass |
| Documents: rulings text, design, requirements note, backlog (6 rows built or trimmed, section 16 with 9 deferrals, 1 deviation) | 00:49 | 00:51 | 0:02 | Review document after the table player |
| Visual check in the Studio | 00:51 | 00:53 | 0:02 | New game, Russians' fire with scripted dice 2 4 wrecks the truck: the Units table shows it wrecked at B8, and the map draws its wreck face. Page text read, since the pane does not draw |
| Table-player review (agent ran 00:49 to 00:56) and fixes, rendering goldens for the two new states, 3 play tests | 00:56 | 01:03 | 0:07 | 6 defects fixed with tests, 2 gaps fixed, 1 gap and 2 notes recorded |
| Full local suite (solution and ScenarioA1) | 01:03 | 01:10 | 0:07 | 1,990 passed, 30 skipped, 0 failed |
