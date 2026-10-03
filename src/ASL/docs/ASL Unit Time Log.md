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
| Commit 8afdcd5 and Docker Linux check (appendix A) | 01:10 | 01:16 | 0:06 | restore, build (warnings as errors), solution tests, and ScenarioA1 tests all exit 0 |
| Merge into main and push | 01:16 | 01:17 | 0:01 | |

Pass 6 actual: 1:08 (00:09 to 01:17) against the estimate of 4:35.

| Plan task | Estimate | Actual |
|---|---|---|
| 6.1 to 6.5 build: review stage, referee fixes, live stage, table-player fixes (one block; split not recorded) | 3:20 | 0:36 |
| Overhead: reading and rulings | | 0:06 |
| Overhead: documents and visual check | | 0:04 |
| Overhead: full suite, Docker check, merge | | 0:14 |
| Overhead total (the two reviews ran beside the build) | 1:15 | 0:24 |
| **Total** | **4:35** | **1:08** |

## Pass 7: armor

Estimate: 5:15 (build 4:00) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-28 01:26. Branch `feature/asl-backlog-pass-7`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (plan, Chapter H listings and Key, C3.3 to C3.9, C4 to C8, D1.3 to D1.7, D3.1 to D3.2, D5.2 to D5.7, the C3 and C7 charts) | 01:26 | 01:31 | 0:05 | Listings, To Hit and To Kill charts rendered; the end time is an estimate between two clock reads |
| Catalog 1.6.0: PzKpfw IIIH and T-34 M41 (86 rows), source record, manifest, synthetic tanks, version references | 01:31 | 01:37 | 0:06 | |
| Review stage: C3 Vehicle row and C7 To Kill transcriptions, rulings R7.1 to R7.12, armor reference and calculator, vocabulary 1.11.0 (Shock, UK) | 01:37 | 01:47 | 0:10 | The end time is an estimate between two clock reads |
| Source comparison (50 fragments; the tool's markup stripping fixed for escaped dice signs), package revision of all four packages for catalog 1.6.0, package tests | 01:47 | 02:21 | 0:34 | |
| Referee agent (13 findings) and fixes: improbable hit location, "+1" counter, Motion and moved firers, depletion above the number, unarmored crew TC, crew morale refusal, rulings and matrix | 02:22 | 02:35 | 0:13 | The agent ran while the live stage began |
| Live stage: scenario year, turret facings, depleted ammunition, Shock recovery action and record, tank MA and vehicle-target shots, To Kill effects, gate readback, Play page, live tests | 02:35 | 02:43 | 0:08 | |
| Full local suite (catalog and action lists, rendering goldens for SHK and UK), Studio page test, visual check in the Studio | 02:43 | 02:49 | 0:06 | The pane did not draw; the check read the page text and the map's accessible names |
| Documents: design, requirements (review and backlog wait for the table player) | 02:49 | 02:52 | 0:03 | |
| Table player agent (11 findings) and fixes: Shocked and UK stop a vehicle everywhere, Abandoned tanks, a tank's "?" and Acquisition, malfunction on missing ammunition, crew check exemptions, ammunition list, Shock records | 02:52 | 02:56 | 0:04 | The agent ran from 02:44 during the suite and the visual check |
| Documents: review, backlog section 17 and four deviations, rulings R7.8 to R7.10, catalog design 9.7 | 02:56 | 02:58 | 0:02 | |
| Full local suite before the commit (1,782 tests pass, 30 skipped) | 02:58 | 03:05 | 0:07 | |
| Commit, Docker Linux check (every project exits 0), merge, push | 03:05 | 03:12 | 0:07 | |

Pass 7 total: 01:26 to 03:12, 1:46 against the estimate of 5:15 (build 4:00).

| Task (plan estimate) | Actual | Where it was logged |
|---|---|---|
| 7.1 Tank counters (0:40) | 0:06 | Catalog 1.6.0 |
| 7.2 Vehicle Target Type and 7.3 To Kill (2:10) | 0:44 | Review stage code (0:10) and source comparison, package revision, package tests (0:34) |
| 7.4 Main armament fire and 7.5 Crew survival (1:10) | 0:08 | Live stage |
| Overhead (1:15) | 0:48 | Reading, referee and table player with their fixes, suite, visual check, documents, merge gate |

## Pass 8: Guns, part 2

Estimate: 6:10 (build 4:55) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-28 03:12. Branch `feature/asl-backlog-pass-8`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (plan, backlog section 12, C2.24 to C2.8, C3.21, C3.22, C5.5 to C5.9, C6.1 to C6.57, C10, C11, A4.41, A5.12, A7.352, A12.14) and rulings R8.1 to R8.12 | 03:12 | 03:38 | 0:26 | |
| Catalog 1.7.0: BPV, dates, and Animal-Pack of the two Guns (6 rows), vocabulary 1.12.0, version references | 03:38 | 03:43 | 0:05 | |
| Package stage: Fire and Ordnance package code (First Fire, Intensive Fire, Cases E, F, H, J1 to J4, M, overstacking, Guns and crews as targets, crews firing), package revision for catalog 1.7.0, package tests | 03:43 | 03:56 | 0:13 | |
| Source comparison (54 fragments), package revision (fragments, rulings, cases), referee agent (9 findings) and its fixes, first live-stage code (Defensive First Fire, Intensive Fire, Gun targets, crews' fire, markers) | 03:56 | 04:13 | 0:17 | The referee ran in parallel |
| Live stage: live tests for Defensive First Fire, Intensive Fire, Gun targets, crews' fire; CA change action, pushing and abandoning, hooking and towing, Bore Sighting, Case E and H, C2.6, bearings across boards, Play page and Studio test | 04:13 | 04:34 | 0:21 | |
| Full suite (test updates for the new rules, goldens for IF), table player agent (14 findings) and fixes, verifier recomputing state facts, rulings and design | 04:34 | 04:51 | 0:17 | The table player ran from 04:36 |
| Documents (review, backlog section 18, requirements, catalog design 9.8) and visual check in the Studio | 04:51 | 04:53 | 0:02 | The pane did not draw; the check read the page text and markup |
| Full local suite before the commit (1,807 tests pass, 30 skipped) | 04:53 | 05:01 | 0:08 | |
| Commit, Docker Linux check (every project exits 0), merge, push | 05:01 | 05:08 | 0:07 | |

Pass 8 total: 03:12 to 05:08, 1:56 against the estimate of 6:10 (build 4:55).

| Task (plan estimate) | Actual | Where it was logged |
|---|---|---|
| 8.10 Catalog details (part of 0:20) | 0:05 | Catalog 1.7.0 |
| 8.1 to 8.9 and 8.10 overstacking, package side (about 2:30) | 0:13 | Package stage |
| 8.1 to 8.9, live side (about 2:05) | 0:38 | Live stage (0:21) and the live code begun with the source comparison (about 0:17) |
| Overhead (1:15) | 1:00 | Reading and rulings, source comparison, referee and table player with their fixes, suites, documents, visual check, merge gate |

## Pass 9: Mortars, SMOKE, and anti-tank weapons

Estimate: 4:45 (build 3:30) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-28 05:10. Branch `feature/asl-backlog-pass-9`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (plan, backlog sections 6, 7, 12, A24, C1.52 to C1.55, C3.33 to C3.74, C4, C9, C13, the SW Chart, the two listing rows, the To Hit Table) and rulings R9.1 to R9.9 | 05:10 | 05:46 | 0:36 | |
| Catalog 1.8.0: two light mortars (36 rows), vocabulary 1.13.0, version references, synthetic catalog | 05:46 | 05:51 | 0:05 | |
| Package stage: Area Target Type, spotting, leadership, and Panzerfaust code, the Area row, source comparison (23 subjects), package revision, package tests | 05:51 | 06:11 | 0:20 | |
| Live stage: state and records, projector, SMOKE grenades and Hindrance, mortar and PF live facts and planner, referee agent (15 findings) and its fixes | 06:11 | 06:30 | 0:19 | The referee ran from 06:11 to 06:24 |
| Live tests, Play page, table player agent (14 findings) started | 06:30 | 06:40 | 0:10 | |
| Full suite (one Studio test updated), documents drafted, visual check in the Studio | 06:40 | 06:51 | 0:11 | The pane did not draw; the check read the page text |
| Table player fixes with tests, documents (review, design, backlog sections 1 and 19, requirements, catalog design 9.9) | 06:51 | 06:59 | 0:08 | The table player ran from 06:40 to 06:52 |
| Full local suite before the commit (2,088 tests pass, 30 skipped) | 06:59 | 07:08 | 0:09 | |
| Commit, Docker Linux check (every project exits 0), merge, push | 07:08 | 07:15 | 0:07 | |

Pass 9 total: 05:10 to 07:15, 2:05 against the estimate of 4:45 (build 3:30).

| Task (plan estimate) | Actual | Where it was logged |
|---|---|---|
| 9.1 Counters (0:40) | 0:05 | Catalog 1.8.0; the PSK and ATR are blocked by their source |
| 9.2 to 9.4, package side (about 1:25) | 0:20 | Package stage |
| 9.2 to 9.4, live side (about 1:25) | 0:37 | Live stage (0:19, with the referee's fixes), live tests and page (0:10), table player fixes (0:08) |
| Overhead (1:15) | 1:03 | Reading and rulings, suites, documents, visual check, merge gate |

## Pass 9b: the Panzerschreck and the ATR

No plan estimate: the user asked for it after pass 9 (2026-09-28), with manufactured counter values under ruling R0.3, and to await instructions after it. Kickoff 2026-09-28 07:38. Branch `feature/asl-backlog-pass-9b`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Memory, catalog 1.9.0 (32 manufactured rows), vocabulary 1.14.0, version references | 07:38 | 07:48 | 0:10 | Two shell permission checks gave no verdict at the start |
| Package stage: ATR and PSK in the Ordnance and Fire packages, source comparison (16 subjects), rulings R9.10 and R9.11, package revision, package tests | 07:48 | 07:55 | 0:07 | |
| Live stage: live facts, planner, X# removal, projector, Play page, live tests; referee and table player agents started | 07:55 | 07:58 | 0:03 | |
| Documents; referee (10 findings) and table player (8 findings) fixes with tests; visual check in the Studio | 07:58 | 08:10 | 0:12 | The agents ran from 07:55 and 07:58 |
| Full local suite before the commit (2,100 tests pass, 30 skipped) | 08:10 | 08:22 | 0:12 | Answered the user's estimate and scenario card questions meanwhile |
| Commit, Docker Linux check (every project exits 0), merge, push | 08:22 | 08:34 | 0:12 | |

Pass 9b total: 07:38 to 08:34, 0:56, with no plan estimate.

## Pass 10: Movement and terrain

Estimate: 7:10 (build 5:55) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>); the user's latest estimate was about 2:45. Kickoff 2026-09-28 08:36. Branch `feature/asl-backlog-pass-10`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (plan, backlog sections 1, 3, 4, 9, 11, A4, A6, A7.21, A8.15, A12.1, A15.43, B9, B10, B16, B23, B24, the Terrain Chart) and rulings R10.1 to R10.15 | 08:36 | 08:52 | 0:16 | |
| Package stage: Fire package code (levels, walls, Height Advantage, Snap Shots, TPBF, Hazardous Movement), the pass 10 Terrain Chart transcription, source comparison (40 subjects), package revision; Units records and the planner's terrain, movement, and fire code | 08:52 | 09:17 | 0:25 | The comparison ran as an agent from 08:48 to 09:00 |
| Referee agent (18 findings), Authoring and package tests, re-pinning | 09:17 | 09:30 | 0:13 | The referee ran from 09:17 to 09:43 |
| Referee fixes and rulings revised | 09:30 | 09:39 | 0:09 | |
| Live tests (15), older tests updated, Play page, table player agent started | 09:39 | 09:48 | 0:09 | |
| Documents (design, review, backlog sections 1 and 20, requirements) while the full suite ran | 09:48 | 09:55 | 0:07 | |
| Table player fixes (18 findings: 14 fixed, 4 recorded) with tests, page status line, documents | 09:55 | 10:03 | 0:08 | The table player ran from 09:45 to 09:59; the first full suite ran from 09:48 to 10:03 |
| Three older tests updated for the new rules, visual check in the Studio | 10:03 | 10:15 | 0:12 | The pane was driven through DOM events and read as text |
| Full local suite before the commit (2,126 tests pass, 30 skipped) | 10:15 | 10:25 | 0:10 | |
| Commit, Docker Linux check (every project exits 0), merge, push | 10:26 | 10:36 | 0:10 | |

Pass 10 total: 08:36 to 10:36, 2:00 against the plan estimate of 7:10 (build 5:55) and the user's estimate of 2:45.

| Task (plan estimate) | Actual | Where it was logged |
|---|---|---|
| 10.1 to 10.9, package side (about 2:00) | 0:25 | Package stage, with the first live code |
| 10.1 to 10.9, live side (about 3:55) | 0:38 | Referee fixes (0:09), live tests and page (0:09), table player fixes (0:08), older tests and visual check (0:12) |
| Overhead (1:15) | 0:57 | Reading and rulings, referee and table player, documents, suites, merge gate |

## Pass 11: Vehicle movement and OVR

Estimate: 4:25 (build 3:10) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-28 11:10. Branch `feature/asl-backlog-pass-11`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| First reading of D2.1 to D2.7, D7, and D8.1 to D8.5 (earlier session) | 11:10 | 11:35 | 0:25 | The session ended before rulings |
| State check, MapStudio tests (125 pass), commit of the pass 10 Play page fixes (9f82c7d) | 11:37 | 11:39 | 0:02 | |
| Reading (D2.8 to D2.38, D2.5, D2.51, D7, D8, A5.2, A11.3 to A11.7, A12.41, B9.4, B13.41, B23.41, B24.4, the Terrain Chart's vehicle columns) and rulings R11.1 to R11.18 | 11:39 | 11:53 | 0:14 | |
| Units model (Bypass, ESB, checks, PAATC, OVR, vehicle CC records), the Fire package's OVR, the Close Combat package's CC with vehicles | 11:53 | 12:07 | 0:14 | |
| Planner: vehicle terrain, Reverse, VBM, ESB, Minimum Move, Bog, D2.6, OVR, A12.41 choice, PAATC, CC Reaction Fire, sequential CC | 12:07 | 12:28 | 0:21 | |
| Play tests: 21 new, 4 older updated; Bypass Target Facing in ordnance fire; referee agent started | 12:28 | 12:40 | 0:12 | |
| Referee's 16 findings (13 fixed with tests, 3 recorded), ScenarioA1 tests, Play page panels, table-player agent started, the pass 11 PDF comparison (59 subjects) | 12:40 | 13:12 | 0:32 | |
| Source review registration, Fire and Close Combat package revisions; Authoring (161) and ScenarioA1 (414) tests pass | 13:12 | 13:22 | 0:10 | Test run in the background to 13:30 |
| Table player's 13 findings: 11 fixed with 6 new tests and one extended, 2 recorded; Play tests (268) pass | 13:22 | 13:36 | 0:14 | |
| Visual check in the Studio (three games), and the fire summary's OVR firer fixed | 13:36 | 13:41 | 0:05 | |
| Review, design, backlog section 21, requirements, and ruling wording | 13:41 | 13:47 | 0:06 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (2,138 passed, 30 skipped) | 13:47 | 13:57 | 0:10 | |
| Commit c282c46; merge gate: the Docker Linux check (restore, build, test, a1: every step exit 0) | 13:57 | 14:07 | 0:10 | |
| Merge and push | 14:07 | 14:09 | 0:02 | |

Total 2:59 of working time (11:10 to 14:09, less the 11:35 to 11:37 gap) against the 4:25 estimate; the build, 11:53 to 13:36 with its fixes, took 1:43 against 3:10.

## Pass 12: Fire extensions

Estimate: 2:30, the user's revision of 2026-09-28 (the plan's 5:30, build 4:15, scaled by the passes 5 to 11 actuals). Kickoff 2026-09-28 14:12. Branch `feature/asl-backlog-pass-12`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (A6.11, A7.24, A7.25, A7.35 to A7.353, A7.52, A7.53, A7.7, A7.83, A8.31, A9.12, A9.22 to A9.223, A9.5 to A9.52, A11.15, A12.12 to A12.122, the Concealment Table, A15.432, A20.52, A20.54; the Terrain Chart's red Concealment Terrain) and rulings R12.1 to R12.12 | 14:12 | 14:18 | 0:06 | |
| Fire package (Opportunity Fire, blocked LOS, FPF variants, pinned movers, a leader's MG, Spraying Fire, Fire Lane Residual FP, Melee and prisoner targets, Encirclement), Units records, and the planner | 14:18 | 14:40 | 0:22 | |
| Regressions from the new rules fixed (berserk and Guard fire, blocked LOS, turn-end concealment); Play (268) and MapStudio (126) tests pass; referee agent started | 14:40 | 14:48 | 0:08 | |
| Tests: ScenarioA1 (10) and Play (7) pass 12 tests; the PDF comparison (16 fragments; A9.222 cited from the PDF) and its registration; the Fire package revision | 14:48 | 14:53 | 0:05 | |
| Referee's 12 findings: 11 fixed (tests where the catalog allows), 1 recorded; three older package tests follow the new rules; Play (275), ScenarioA1 (422) pass | 14:53 | 15:04 | 0:11 | |
| Play page: Opportunity Fire panel, Spraying Fire, Fire Lane, and partner controls, Encircled units; Studio test; MapStudio (127) pass; table-player agent started | 15:04 | 15:08 | 0:04 | |
| Visual check in the Studio (Opportunity Fire, Spraying Fire, the AFPh), the proposal text fixed; review, design, backlog section 22, and requirements | 15:08 | 15:11 | 0:03 | |
| Table player's 11 findings: 8 fixed with 3 new tests, 3 recorded (and the follow-ons after an owner's choice); Play (278), MapStudio (127) pass | 15:11 | 15:22 | 0:11 | Authoring (162) passed at 15:17 |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (2,188 passed, 30 skipped) | 15:22 | 15:33 | 0:11 | |
| Commit 5ea5923; merge gate: the Docker Linux check (restore, build, test, a1: every step exit 0) | 15:33 | 15:43 | 0:10 | |
| Merge and push | 15:43 | 15:45 | 0:02 | |

Total 1:33 of working time (14:12 to 15:45) against the user's 2:30 estimate (the plan's 5:30); the build with both reviews' fixes, 14:18 to 15:22, took 1:04.

## Pass 13: Rally, Rout, and support weapons

Estimate: 4:40 (build 3:25) in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>). Kickoff 2026-09-28 17:53. Branch `feature/asl-backlog-pass-13` (its first commit, the Fire Lane label fix, merged as 53985f2).

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (A1.31, A1.32, A4.43, A4.431, A4.44, A9.8, A10.5 to A10.533, A10.61 to A10.63, A20.21, A21.1 to A21.12) and rulings R13.1 to R13.8 | 17:53 | 17:58 | 0:05 | |
| Units records and projector (rout steps, Interdiction, Deployment, RPh actions, Recovery), the Rally package's terrain and Self-Rally, the Fire package's captured MG | 17:58 | 18:05 | 0:07 | |
| PDF comparison (21 fragments: A1.31, A1.32, A4.43 to A4.44, A9.8, A10.5 to A10.533, A21.1 to A21.12) and its registration; the Fire and Rally package revisions and pins; Authoring matrices and source review (42) pass | 18:05 | 18:33 | 0:28 | |
| Planner: the rout, Interdiction, DM (ADJACENT, RtPh start, retention), Failure to Rout and surrender; Deploy, Recombine, SW transfer, drop, Recovery, dismantling; captured MG facts; three older tests follow the new rules; Play (278) pass; referee agent started | 18:33 | 18:47 | 0:14 | |
| Tests: ScenarioA1 (5) and Play (12) pass 13 tests, the gate's readback of the new records; referee's 25 findings: 22 fixed (tests where they show), 3 recorded or reworded; Play (290) pass | 18:47 | 19:07 | 0:20 | |
| Play page: the Rout panel, the SW panel (transfer, drop, recover, dismantle), Deploy, Recombine, DM retention, and their records; a Studio test; an older Studio test's broken HS now surrenders; MapStudio (128) pass; table-player agent started | 19:07 | 19:17 | 0:10 | |
| Visual check in the Studio (pass13-demo: a transfer, a Deployment, the RtPh panel and a rout; pass13-demo-2 timed): a rout took 15 s to plan, so the route search and the board LOS map now keep their reads (under 4 s); Play (290) pass | 19:17 | 19:35 | 0:18 | |
| Table player's 16 findings (27 situations, played in a separate worktree): 11 fixed with 5 new tests, 5 recorded; rulings R13.3 to R13.6 reworded; Play (295), MapStudio (128), ScenarioA1 (427) pass | 19:35 | 19:45 | 0:10 | |
| Review, design, backlog section 23 (13 rows removed from sections 2, 3, 5, 8, 11, and 22), requirements | 19:45 | 19:52 | 0:07 | |
| Full local suite: build with warnings as errors, every ASL test project (2,211 passed, 30 skipped, 1 failed: the architecture test's scan hit `.git/worktrees`, held in a pending delete after a stray agent worktree was removed; the Docker clean clone runs it) | 19:50 | 20:06 | 0:16 | |
| Commit d87daf8; merge gate: the Docker Linux check (restore, build, test, a1: every step exit 0; Authoring 163 of 163) and the three CI regeneration checks (identical) | 20:06 | 20:21 | 0:15 | |
| Merge and push | 20:21 | 20:24 | 0:03 | |

Total 2:31 of working time (17:53 to 20:24) against the 4:40 estimate (build 3:25); the build with both reviews' fixes, 17:58 to 19:45, took 1:47.

## Pass 14: Close Combat and capture, part 2

Estimate: 2:45, the user's figure of 2026-09-28 (the plan's 5:05, build 3:50, scaled by the passes 11 to 13 actuals). Kickoff 2026-09-28 20:41. Branch `feature/asl-backlog-pass-14`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (A4.8, A5.1 to A5.131, A11.11 to A11.41, A12.13 to A12.15, A18.12, A18.2, A19.12, A19.35, A20 to A20.552; the CCT's red Kill Numbers and modifiers; G1.64, A25.43, and W.6B on Hand-to-Hand; the A./G. National Capabilities Chart's BPV) and rulings R14.1 to R14.14 | 20:41 | 20:51 | 0:10 | J2.31's text is not in the registered PDF |
| Close Combat package: models, the ordered round (Infiltration, capture, escape, rearming, concealment), Ambush, BPV, red Kill Numbers; six older tests follow the new rules; 17 package tests (every outcome walked) pass | 20:51 | 21:00 | 0:09 | |
| PDF comparison (9 fragments: A4.8, A5.11, A11.33, A11.34, A12.15, A19.35, A20.22, A20.221, A20.551) and its registration; the Close Combat package revision (80 fragments, prior 2dde0d2b); ScenarioA1 (173) and Authoring pass | 21:00 | 21:05 | 0:05 | Authoring run in the background to 21:10 |
| Units records and projector (Unarmed, `prisoner-freed`, the CCPh start, Guard succession, the prisoners' round), planner (advance, round declarations, effects, Ambush reveal, free as Unarmed, mandatory CC), two new actions; four older Play tests follow the new rules; 8 new Play tests; Play (303) pass; referee agent started | 21:05 | 21:23 | 0:18 | |
| Play page: Hand-to-Hand, capture attempt with Guard and yield order, Infiltration, prisoners' round, Ambush Withdrawal, free as Unarmed, Guard transfer and abandonment, and their records | 21:23 | 21:27 | 0:04 | A Visual Studio Studio session locked the Debug build; built in Release |
| Referee's 13 findings: 11 fixed with 7 new tests, 2 recorded (the prisoners' round declared at once; the 10-to-1 reading); ScenarioA1 (175), Play (304) pass | 21:28 | 21:35 | 0:07 | |
| Table-player clone (`git worktree add` failed on the held `.git/worktrees`; a local clone in the scratchpad instead); an unintended whole-project whitespace format of the ScenarioA1 project reverted; table-player agent started | 21:35 | 21:38 | 0:03 | |
| MapStudio page test (a capture attempt; MapStudio 129 pass); visual check in the Studio (pass14-demo: an Ambush against a concealed squad in Open Ground, a capture attempt and its record, the Units table, the Guard panel and a transfer); the Ambush hint text fixed | 21:38 | 21:44 | 0:06 | |
| Waiting on the table player: documents drafted, the Docker script given two CI regeneration checks | 21:44 | 21:47 | 0:03 | The table player ran 21:38 to 21:55 |
| Table player's 8 findings (25 situations, 39 cases, in the scratchpad clone): 5 fixed (a freeze: an escape that eliminates its Guard; Recovery by Unarmed units; concealed advances; two refusal texts) with one new test and the reproductions rerun, and a sixth bug the rerun showed (a rearmed prisoner still captured); 3 recorded; Play (305) pass | 21:55 | 21:58 | 0:03 | |
| Review, design, backlog section 24 (17 rows removed from sections 11 and 20), requirements, ruling wording | 21:58 | 22:00 | 0:02 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (2,269 passed, 30 skipped, 3 failed: the architecture test's scan hit the held `.git/worktrees`; a Units fixture whose Russian squad is concealed, so no longer held in Melee (R14.2), made Known; a Guard in another Location, which Guard succession had repaired instead of refusing, is an invariant error again, and a routing Guard now takes its prisoners); Units (405), Play (305) pass | 22:00 | 22:20 | 0:20 | |
| Commit 6cf7ea1; merge gate: the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 164 of 164) and the chart supplement regeneration run locally (identical); the user's confirmation of the two readings recorded | 22:20 | 22:31 | 0:11 | |
| Merge and push | 22:31 | 22:33 | 0:02 | |

Total 1:52 of working time (20:41 to 22:33) against the 2:45 estimate (the plan's 5:05, build 3:50); the build with both reviews' fixes, 20:51 to 21:58, took 1:07.

## Pass 15: Special units and nationalities

Estimate: 2:40, the user's figure of 2026-09-28 (the plan's 6:30, build 5:15, scaled by the passes 12 to 14 actuals). Kickoff 2026-09-28 22:34. Branch `feature/asl-backlog-pass-15`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (A14, A22, A23, A25.22 to A25.25, A10.7, A19.13, A19.2, A19.3, A15.21 to A15.24, A15.431, A15.1, A18.2; the A./G. National Capabilities Chart rendered from physical page 695) and rulings R15.1 to R15.14 | 22:34 | 22:44 | 0:10 | |
| Catalog 1.10.0: 88 counters from the chart and Chapter A (sheet MFG for the FT and DC portage and the Commissars' broken morale), the catalog rebuilt, the id lists and the catalog version in the tests | 22:44 | 22:54 | 0:10 | |
| Fire, Rally, Close Combat, and Heat of Battle packages: FT, DC, MOL, Commissars, Allied Troops, underscored Morale Factors, a hero's MG, the NKVD table, the nationalities' tables | 22:54 | 23:07 | 0:13 | |
| Units records and projector (DC Placement, Sniper attacks), planner (FT, MOL, Throw, Place, detonate, Snipers, Commissar duty, berserk keep), refusal texts | 23:07 | 23:24 | 0:17 | |
| PDF comparison (36 fragments) and its registration; the four package revisions with their prior digests | 23:24 | 23:30 | 0:06 | |
| Tests: 12 package tests, 15 Play tests, the older tests the new rules change; ScenarioA1 and Play pass | 23:30 | 23:53 | 0:23 | |
| Play page: FT, MOL, DC Throw and detonate, DC Placement and berserk keep in the move panel, Snipers list, SAN inputs, Sniper counter placement; page test | 23:53 | 00:13 | 0:20 | The referee ran 23:53 to 00:17 |
| Visual check in the Studio (pass15-demo: a FT attack and a Thrown DC's two records); Studio stopped; table-player clone and agent started | 00:13 | 00:17 | 0:04 | The table player ran 00:17 to about 00:48 |
| Referee's 16 findings: the underscored ELR of 5, Finnish ranks and classes, the Italian line class, Italian and Finnish progressions (catalog regenerated, 92 counters), the DC freeze, Cowering immunity, Finnish 1st Line and Inexperienced FT and DC users, the thrower's DR; ten more fragments compared (46); packages re-pinned; four new package tests; 9 recorded | 00:17 | 00:50 | 0:33 | |
| Table player's 8 findings (34 situations): 6 fixed (a crash on a DFF Throw, the vehicle Placement freeze, one FT or DC per Player Turn, Prep Fire by a FT, a DC thrower's inherent FP, a hero's FT; the Finnish Self-Rally fixed by the catalog), 2 recorded; 33 situations kept as `BacklogPass15TablePlayerTests`; Play (353) pass | 00:50 | 01:10 | 0:20 | |
| Review, design, backlog section 25 (14 rows removed from sections 2, 5, 6, 10, 11, and 22), requirements, ruling wording | 01:10 | 01:25 | 0:15 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (464); 4 failed: the architecture test's scan hit the held `.git/worktrees`, and three Units catalog tests whose synthetic American green and Italian conscript squads now share key facts with the new nationalities' counters (they now read the synthetic catalog without those); Units (405) pass; the source verification, pending comparison, and chart supplement regenerations run locally (identical) | 01:25 | 01:36 | 0:11 | |
| Commit 0af4dde; merge gate: the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 165 of 165, ScenarioA1 464) | 01:36 | 01:51 | 0:15 | |
| Merge and push | 01:51 | 01:53 | 0:02 | |

Total 3:19 of working time (22:34 to 01:53) against the 2:40 estimate (the plan's 6:30, build 5:15); the referee's and table player's fixes, 00:17 to 01:10, took 0:53.

## Pass 16: Night and weather

Estimate: 1:50, scaled from the plan's 3:45 (build 2:30) by the passes 12 to 15 actuals (about 0.43 of plan, 0.5 for the larger passes). Kickoff 2026-09-29 09:13. Branch `feature/asl-backlog-pass-16`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading (E1 Night, E3 Weather, B25.65 the Wind Change DR, B.8) and rulings R16.1 to R16.14 drafted; a code survey agent mapped the hooks | 09:13 | 09:24 | 0:11 | |
| PDF comparison (72 fragments of E1, E3, B25.65, B.8) and its registration; the Fire, Rally, Close Combat, and Ordnance package revisions (the Low Visibility DRM, a Gunflash beyond NVR, the weather cushion, Extreme Winter B# and Fate, the night Ambush) | 09:24 | 09:31 | 0:07 | |
| Units records and projector (`wind-changed`, `starshell-fired`, the NVR and precipitation, night fire counters, Starshell removal), vocabulary 1.15.0 (the Starshell), planner (SSR checks, NVR, Illumination, Gunflashes, the fire facts, the Wind Change DR, Starshells, night rout, DM, concealment, Ambush, SAN, Recovery, MF and MP, Bog, ordnance), the record verifiers | 09:31 | 09:47 | 0:16 | |
| Tests: 15 Play tests, 5 package tests, the older tests the action list and vocabulary change; Play (368), Units (406), ScenarioA1 (464) pass; the referee agent started | 09:47 | 09:55 | 0:08 | |
| Play page: special rules at a new game, the night and weather line, the Starshell panel, the night records; a page test; MapStudio (131) pass | 09:55 | 10:00 | 0:05 | |
| Table-player clone and agent started; visual check in the Studio (pass16-demo: night:1 with overcast, the status line, fire beyond NVR refused; pass16-demo2: a Starshell three hexes out landing in B3, fire at the Illuminated squad in B4 with the +1 Low Visibility DRM); Studio stopped | 10:00 | 10:05 | 0:05 | The first demo showed the first-Starshell condition refusing correctly where board 01 blocks the LOS |
| Referee's 20 findings: 14 fixed (the ordnance Low Visibility DRM as its own Case R term, vehicle roads in Mud and snow, rain that has fallen, NVR in the Starshell checks, SMOKE in rain, Mud, and Deep Snow, NVR 0 vehicles, a BU AFV's NVR for its MA and its movement, the stairwell MF, the uncapped LV DRM blocking at 6, the Bog notes, the snow road minimum, the Starshell timing, the Japanese and Extreme Winter with snow), 6 recorded; the packages re-pinned; 7 new tests; Play (374) pass | 10:05 | 10:18 | 0:13 | The referee ran 09:55 to about 10:05 |
| Table player's 9 findings (58 situations, 86 cases): 5 already fixed by the referee round (the snow road, the Starshell NVR checks and timing, rain that has fallen), 3 fixed (a hidden Starshell firer's readback, moving vehicles beyond NVR, the ATTACKER-only night Ambush margin; the Close Combat package re-pinned), 1 recorded (Known at night for routing); 57 situations kept as `BacklogPass16TablePlayerTests`; Play pass 16 tests (103) pass | 10:18 | 10:26 | 0:08 | The table player ran 10:00 to about 10:21 |
| Review, design, backlog section 26 (5 rows removed from sections 2, 6, and 11, and 4 rows narrowed in sections 19, 21, 22, and 23), requirements, ruling wording | 10:26 | 10:33 | 0:07 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (470); 1 failed, the architecture test's scan of the held `.git/worktrees`; Play (459), Units (406), MapStudio (131) pass; the source verification, pending comparison, and chart supplement regenerations run locally (identical) | 10:33 | 10:47 | 0:14 | |
| Commit eec0739; merge gate: the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 166 of 166, ScenarioA1 470) | 10:47 | 11:01 | 0:14 | |
| Merge and push | 11:01 | 11:03 | 0:02 | |

Total 1:50 of working time (09:13 to 11:03) against the 1:50 estimate (the plan's 3:45, build 2:30); the build with both reviews' fixes, 09:24 to 10:26, took 1:02.

## Pass 17: Scenario cards

Estimate: about 1:00, scaled from the plan's 1:40 (build 0:25; the plan's 1:55 before the 2026-09-29 ruling cut the pass to the card presentation) by the pass 16 actual (1:50 against a plan of 3:45). Kickoff 2026-09-29 11:57. Branch `feature/asl-backlog-pass-17`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| 17.0 Legacy card analysis: The General, Vol 22 to Vol 32 scanned (about 60 card pages), screened against the boards and the catalog; The Guards Counterattack (Vol 22.6, p. 51) and Gambit (Vol 28.6, p. 64) chosen, The Tractor Works the alternate | 11:57 | 12:06 | 0:09 | The legacy card paths were asked for at kickoff |
| Plan pass 17 revised to the presentation-only scope, the legacy source cards and their adaptation to the registered rulebook added, the duration report updated | 12:12 | 12:16 | 0:04 | |
| Reading (A2.1, A3.9, A16, A19.1, A20.53, A26, the Index's Scenario Defender, H1.28, H1.29, H1.8) and rulings R17.1 to R17.11 drafted; a code survey agent mapped the hooks | 12:16 | 12:30 | 0:14 | |
| PDF comparison (17 fragments) and its registration in the Fire source review | 12:30 | 12:35 | 0:05 | |
| Catalog 1.11.0: six counters (NCC 5-4-8 and HS, OLG 2-in. mortar, MFG HMG, LMG, ATR), the manifest, source record, synthetic catalog, the version pins, and the matrix and package re-pins; Units (406) and ScenarioA1 (470) pass | 12:35 | 13:06 | 0:31 | The ScenarioA1 run took 7:41; repo writes had no classifier verdict for a while |
| The card format, reader, and validation (`ScenarioCards`), the two cards, and 19 Play tests | 13:06 | 13:14 | 0:08 | |
| The Studio's Scenario cards page and 2 page tests; rulings into the plan; the referee agent started | 13:14 | 13:18 | 0:04 | The referee ran 13:18 to about 13:27 |
| Visual check in the Studio (map-studio-scripted: Gambit, then The Guards Counterattack by a DOM change event); Studio stopped; table-player clone (core.longpaths) and agent started; Play (478) and MapStudio (133) pass | 13:18 | 13:24 | 0:06 | The table player ran 13:23 to about 13:31 |
| Referee's 15 and table player's 11 findings: the second 8-0, whole-building setup areas, EC kept apart from weather, the HS Exit VP, the 9-0 Commissar (A25.22), ANZAC Stealth noted, the Defender's own setup, a missing BPV, the SSR 2 wording, five more fragments compared (22), the page's wording; 2 recorded; rulings revised | 13:24 | 13:34 | 0:10 | |
| Review, design, backlog section 27 (2 rows removed from sections 6 and 15, 5 rows re-pointed), requirements, registry README; clone removed | 13:34 | 13:37 | 0:03 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (470); the four Authoring matrix tests pinned the old matrix digests in test code (re-pinned, 21 pass) and the architecture test hit the held `.git/worktrees`; Play (478), MapStudio (133), Units (406), CounterSheets (19) pass | 13:37 | 14:04 | 0:27 | The ScenarioA1 and Authoring runs took 6:55 and 9:47 |
| Commit 55d522a; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 167 of 167, ScenarioA1 470) | 14:04 | 14:20 | 0:16 | |
| Merge and push | 14:20 | 14:22 | 0:02 | |

Total 2:25 of working time (11:57 to 14:22) against the 1:00 estimate (the plan's 1:40, build 0:25); the catalog additions, 12:35 to 13:06, took 0:31, and the referee's and table player's fixes, 13:24 to 13:34, took 0:10.

## Pass 17b: The Tractor Works

The user asked on 2026-09-29 for The Tractor Works as a third card, to run without intervention. No estimate was set. Kickoff 15:02. Branch `feature/asl-card-tractor-works`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| The legacy card read at 300 dpi, the NCC rows rendered, the board 1 building extents; catalog 1.12.0 (NCC plain-E 8-3-8 and 3-3-8 HS, a manufactured Russian HMG), the version pins, and the re-pins | 15:02 | 15:09 | 0:07 | |
| Four fragments compared (26 in all), the format extensions (setup order, "?", a first move by die roll), the card, rulings R17.12 and R17.13, and the tests; the referee agent started | 15:09 | 15:14 | 0:05 | The referee ran 15:14 to about 15:23 |
| Visual check in the Studio (map-studio-scripted, the card chosen by a DOM change event); the first-move sentence fixed; Studio stopped; design and backlog | 15:14 | 15:17 | 0:03 | |
| Referee's 9 findings: the edges derived column by column, the edge basis phrase, the 295th label noted, start Control (A26.11), SSR 2 and the sewers in both Stalingrad cards, the HMG portage note, A12.12; 1 recorded; catalog rebuilt and re-pinned; the review updated | 15:23 | 15:26 | 0:03 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (470); the architecture test hit the held `.git/worktrees`; Play (482), MapStudio (134), Authoring (166 of 167) pass | 15:26 | 15:45 | 0:19 | ScenarioA1 and Authoring ran side by side, 13:56 and 13:10 |
| Commit 1347115; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 167 of 167, ScenarioA1 470) | 15:45 | 16:01 | 0:16 | |
| Merge and push | 16:01 | 16:03 | 0:02 | |

Total 1:01 of working time (15:02 to 16:03); the build with the referee's fixes, 15:02 to 15:26, took 0:24, and the test runs and merge gate 0:35.

## Pass 18: Start from a card

Estimate: 2:00 (build 0:45), the Scenario Card Games Plan, approved 2026-09-29. Kickoff 17:11. Branch `feature/asl-backlog-pass-18`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading the new-game form, the setup action, the planner's start, the event model, and where fire reads the ELR | 17:11 | 17:14 | 0:03 | |
| The event model (OB groups on a side, the card on `game-started`, a unit's group kept through lineage, a Massacre raising every group), a target's and FPF firer's own ELR in the Fire package, the card's start in the planner, the Play page's card picker, group choice, and card panel | 17:14 | 17:20 | 0:06 | |
| 7 Play tests and 2 page tests; rulings R18.1 to R18.3; the referee agent started | 17:20 | 17:26 | 0:06 | The referee ran 17:26 to about 17:33 |
| Visual check in the Studio (map-studio-scripted: The Guards Counterattack from the picker, two grouped units, the setup committed, the card panel and its link); Studio stopped; table-player clone and agent started; design drafted | 17:26 | 17:31 | 0:05 | The table player ran 17:31 to about 17:41 |
| Referee's 13 and table player's 9 findings: units created in play join a group, A19.11 units need no ELR, the FPF firers only, group validation, the page's side lookup, hash, cache, stale groups, no preset winner, the card panel's setup order and groups, the card notes; 4 more Play tests and a page check; 3 recorded; rulings, review, design, backlog section 28, requirements | 17:33 | 17:44 | 0:11 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (470); the architecture test hit the held `.git/worktrees`; Play (493), MapStudio (136), Units (406), Authoring (166 of 167) pass | 17:44 | 18:03 | 0:19 | ScenarioA1 and Authoring ran side by side, 14:49 and 13:05 |
| Commit 9fce110; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 167 of 167, ScenarioA1 470) | 18:03 | 18:23 | 0:20 | Read-only reading for pass 19 while it ran |
| Merge and push | 18:23 | 18:25 | 0:02 | |

Total 1:14 of working time (17:11 to 18:25) against the 2:00 estimate (build 0:45); the build with both reviews' fixes, 17:11 to 17:44, took 0:33, and the test runs and merge gate 0:41.

## Pass 19: Setup from the OB

Estimate: 3:30 (build 2:15), the Scenario Card Games Plan. The user said on 2026-09-29 to move straight to pass 19 after pass 18 merged. Kickoff 18:24 (reading done during pass 18's merge gate). Branch `feature/asl-backlog-pass-19`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| The setup checker (`ScenarioSetup`: OB lines by group and area, order, areas, terrain, stacking, OB "?", SSR limits, Deployment), its planner reading, the setup and start-of-play checks, Dummies keeping their group, the card's area limits (Gambit), 7 Play tests (board 01's real terrain), the Play page's setup table, a page check, and rulings R19.1 to R19.6 | 18:24 | 18:37 | 0:13 | |
| The referee agent started; visual check in the Studio (map-studio-scripted: The Guards Counterattack's setup table, an out-of-order Russian squad refused, a German squad committed, the start of play refused); Studio stopped; table-player clone and agent started | 18:37 | 18:42 | 0:05 | The referee ran 18:37 to about 18:43; the table player 18:42 to about 18:51 |
| Referee's 15 and table player's 9 findings: Deployment FRU and its base, no action before the setup is done, off-map counters, a changed card at the start of play, marsh, several SSR areas, unheld equipment, A5.5 SMC, no "?" after a later group begins, the messages, what an SSR area still owes; 5 more tests; 7 recorded; rulings, review, design, backlog section 29, requirements | 18:43 | 18:53 | 0:10 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (470); the architecture test hit the held `.git/worktrees`; Play (502), MapStudio (136), Units (406), Authoring (166 of 167) pass | 18:53 | 19:08 | 0:15 | ScenarioA1 and Authoring ran side by side, 11:12 and 10:12 |
| Commit 7132323; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 167 of 167, ScenarioA1 470) | 19:08 | 19:28 | 0:20 | |
| Merge and push | 19:28 | 19:30 | 0:02 | |

Total 1:06 of working time (18:24 to 19:30) against the 3:30 estimate (build 2:15); the build with both reviews' fixes, 18:24 to 18:53, took 0:29, and the test runs and merge gate 0:37.

## Pass 19b: the Fire tables for the card counters

No estimate of its own: backlog section 29's first row, found by pass 19, run as a small pass by the user's kickoff of 2026-09-29. Kickoff 19:48. Branch `feature/asl-backlog-pass-19b`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading the plan, rulings R17 to R19, the pass 18 and 19 documents, backlog sections 27 to 29, and the Fire reference tables | 19:48 | 19:51 | 0:03 | |
| A19.13, A15.3, and the NCC German rows (rendered); the HS, Replacement, and highest-quality entries; 8 ScenarioA1 tests and 2 Play tests; ruling R19.7; the referee agent started | 19:51 | 20:02 | 0:11 | The referee ran 20:02 to about 20:10; pass 20 reading meanwhile |
| Referee's 10 findings: the CC BPV table (matrix and package re-pinned), R19.7 reworded around A25.13, a separate Casualty HS table, the ELR 5 test, the package test reading the cards, a CC test; review, design, backlog, requirements | 20:10 | 20:18 | 0:08 | |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (480); the architecture test hit the held `.git/worktrees`; Play (504), MapStudio (136), Units (406), Authoring (166 of 167) pass | 20:18 | 20:33 | 0:15 | ScenarioA1 and the solution ran side by side, 14:00 and 12:35; pass 20 code drafted meanwhile |
| Commit 628cd3f; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 167 of 167, ScenarioA1 480) | 20:33 | 20:58 | 0:25 | Pass 20 built and its reviews started meanwhile |
| Merge and push | 20:58 | 21:00 | 0:02 | |

Total 1:12 of working time (19:48 to 21:00), with no estimate of its own; the build with the referee's fixes, 19:48 to 20:18, took 0:30, and the test runs and merge gate 0:40, most of it overlapped with pass 20.

## Pass 20: Turns, reinforcements, and the start options

Estimate: 3:00 (build 1:45), the Scenario Card Games Plan. Run in the same session as pass 19b, by the user's kickoff of 2026-09-29; its reading and much of its build overlapped with pass 19b's referee, test runs, and merge gate. Branch `feature/asl-backlog-pass-20`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading A2.1, A2.5 to A2.9, A3.9, A4.1, and A26.4; the planner's phase advance, movement step, setup, and start; the event model; the Play page; the design, the rulings drafted, and the edits written as scripts | 20:02 | 20:33 | 0:31 | Alongside pass 19b's referee and full local suite |
| The edits applied: `game-ended`, the first-move and Balance drs, the Balance counters, off-board setup and entry, the entry due at the MPh's end, the playable area, the page; 9 Play tests and a page test; pass 18 and 19 tests roll for The Tractor Works; the solution built with warnings as errors; Play (513), Units (406), MapStudio (136) pass; rulings R20.1 to R20.6; the referee and the table player (a clone) started | 20:34 | 20:56 | 0:22 | The pass 19b Docker check ran meanwhile |
| Visual check in the Studio (map-studio-scripted: The Tractor Works' die roll and Balance choices, the first setup committing its drs, the card panel, the Scenarios page's enforced area); Studio stopped; pass 19b merged meanwhile | 20:56 | 21:04 | 0:08 | The referee ran 20:56 to about 21:05; the table player 20:56 to about 21:26 |
| Referee's 16 findings: the rout and charge searches within the area, the multi-board area, the off-board Balance counter, the shared entry-hex test, vehicles at entry, the agreed Balance with players, the card read once; 2 tests; rulings reworded | 21:05 | 21:12 | 0:07 | |
| Table player's 19 situations brought in as `BacklogPass20TablePlayerTests`: the Balance counters owed, no broken setup (R20.7), the rout test rewritten, the constant arrays; Play (534) pass; rulings, review, design, backlog section 30, requirements | 21:26 | 21:46 | 0:20 | The Play run took 8 minutes |
| Full local suite: build with warnings as errors, every solution test project, and the ScenarioA1 tests (480); the architecture test hit the held `.git/worktrees`; Play (534), MapStudio (136), Units (406), Authoring (166 of 167) pass | 21:46 | 22:32 | 0:46 | ScenarioA1 26:05 and Play 18:39, slowed by the Docker check running alongside from 22:07 |
| Commit 13495a9; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Authoring 167 of 167, Play 534, ScenarioA1 480) | 22:07 | 22:48 | 0:41 | Overlapped with the local suite |
| Merge and push | 22:48 | 22:50 | 0:02 | |

Total 2:17 of working time (20:02 to 22:50, less the 0:31 of pass 20 reading that overlapped pass 19b's gate) against the 3:00 estimate (build 1:45); the build with both reviews' fixes, 20:02 to 21:46, took 1:13 of its own, and the test runs and merge gate 1:04, most of it waiting on runs that the Docker check slowed.

## Pass 21: Victory Conditions

Estimate: 3:45 (build 2:30), the Scenario Card Games Plan. The user said on 2026-09-29 to go ahead with pass 21, and later to continue with the remaining passes after it merges without their intervention. Kickoff 23:05. Branch `feature/asl-backlog-pass-21`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading A26.1 to A26.4, A.7, A2.6; the cards' Victory Conditions; the event model and the move step | 23:05 | 23:20 | 0:15 | |
| The Infantry exit and exit records, the result on `game-ended`, the structured Victory Conditions and the three cards, `ScenarioVictory` (Control, VP, CVP, Exit VP, outcomes), the planner's result at the end and at once, the Play page; 8 Play tests; rulings R21.1 to R21.5 | 23:20 | 23:34 | 0:14 | |
| The referee and the table player (a clone) started; visual check in the Studio (The Guards Counterattack set up through the page, the Victory Conditions table, played to its end by Avoidance); Studio stopped | 23:34 | 23:44 | 0:10 | The referee ran 23:34 to about 23:41; the table player 23:34 to about 00:13 |
| Referee's findings: normal VP in the immediate check, no end while something is open, the card's hash and validity, no exit with a manned Gun or prisoners, Guns out of the VP, the CR ruling; 1 test; rulings | 23:41 | 23:48 | 0:07 | |
| Table player's findings: SW leave with their carriers, Melee hexes shown as neither's; its tests brought in (one Gambit walk-off kept); 18 pass 21 tests pass; review, design, backlog section 31, requirements | 00:13 | 00:26 | 0:13 | The pass 21 tests took 5:18 |
| Full local suite and the Docker check, side by side: build with warnings as errors, every solution test project, and the ScenarioA1 tests (480); the architecture test hit the held `.git/worktrees`. Docker Desktop was not running and was started (00:30). Both runs found 2 pass 20 tests comparing the whole `game-ended` record, which now carries the result; fixed to compare turn and reason (commit f03e6b5) | 00:26 | 01:02 | 0:36 | Pass 22 read and drafted meanwhile |
| Commit 4bd323d and f03e6b5; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check on f03e6b5 (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Play 552, Authoring 167 of 167, ScenarioA1 480) | 01:02 | 01:25 | 0:23 | |
| Merge and push | 01:25 | 01:27 | 0:02 | |

Total 2:22 of working time (23:05 to 01:27) against the 3:45 estimate (build 2:30); the build with both reviews' fixes, 23:05 to 00:26, took 1:21, and the test runs and merge gate 1:01, with one Docker run lost to Docker Desktop being down and one repeated for the pass 20 tests.

## Pass 22: The card editor

Estimate: 3:30 (build 2:15), the Scenario Card Games Plan. Run without the user's intervention after pass 21 merged, by their instruction of 2026-09-29. Branch `feature/asl-backlog-pass-22`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading the Play page's new-game form, the Scenarios page, the page tests' game starts, and the planner's card reads; the design; the card library, minimal cards, the editor page, and the test migration written as scripts | 00:40 | 01:25 | 0:45 | During pass 21's merge gate |
| The scripts applied: the card library, minimal cards, the planner reading the library, the editor page, the Scenarios page, the Play page without the form (dead members removed); the page tests moved to minimal cards; 4 Play tests and 3 editor page tests; MapStudio (139) and the pass 17 and 22 Play tests pass; rulings R22.1 to R22.4 | 01:27 | 01:40 | 0:13 | |
| Live check in the Studio preview (copy, picker, save, the Play page's list and summary, delete); the referee and the table player started | 01:40 | 01:43 | 0:03 | |
| The referee's fixes (a changed or gone card read by no rule, copies keeping the Integrity totals and the hexrows' board, save refusing a taken id, text and hash read together, the picker, atomic writes, the name pattern, minimal outcomes, the Play page's card reads) and the table player's (Extreme Winter's date, the gone-card message, side ELR with an OB, the id as typed, side changes, the stray comma, the Scenarios victory line, the picker hint); the table player's 16 tests kept; MapStudio 156 pass | 01:43 | 02:01 | 0:18 | The reviews ran 01:43 to 01:54 |
| The design, the review, backlog section 32, and the ruling amendments; the full local suite in the background | 02:01 | 02:05 | 0:04 | |
| Full local suite: every solution test project and the ScenarioA1 tests (480); Play 556, MapStudio 156; the architecture test hit the held `.git/worktrees` (known) | 02:05 | 02:36 | 0:31 | |
| Commit fa365b7; merge gate: the chart supplement regeneration run locally (identical with sorted keys) and the Docker Linux check (restore, build, test, a1, and the source verification and pending comparison regenerations: every step exit 0; Play 556, MapStudio 156, Authoring 167 of 167, ScenarioA1 480) | 02:36 | 02:58 | 0:22 | |
| Merge and push | 02:58 | 03:00 | 0:02 | |

Total 2:18 of working time (00:40 to 01:25 during pass 21's merge gate, then 01:27 to 03:00) against the 3:30 estimate (build 2:15); the build with both reviews' fixes took 1:21, and the docs, test runs, and merge gate 0:57.

### The card editor demonstration and its fixes

At the user's request, 2026-09-30, branch `feature/asl-card-editor-polish`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| The Studio started and the editor demonstrated: its layout fixed first, then a minimal card, a copy of the Guards with the picker, the Play page, and the Scenarios page; four defects found | 07:03 | 07:10 | 0:07 | |
| The four defects fixed (Save never disabled, the side ELR and the Victory line on the Scenarios page, no OB table for a minimal card); the tests changed; MapStudio 156 pass; retested in the Studio | 07:10 | 07:17 | 0:07 | |
| Full local suite (MapStudio 156, Play 556, ScenarioA1 480; the architecture test hit the held `.git/worktrees`, known); the plan for passes 23 to 30 drafted meanwhile | 07:18 | 07:42 | 0:24 | |
| Commit db812c9; the Docker Linux check (every step exit 0; MapStudio 156, Play 556, Authoring 167 of 167, ScenarioA1 480); no Authoring change, so no chart supplement run; merge and push | 07:43 | 08:02 | 0:19 | |

Total 0:57 (07:03 to 08:02, with the plan drafted during the suite).

## Pass 22b: The theme and the shared foundation

Estimate: 5:30 (build 4:15), the ASL Card Play and Map Studio Redesign Plan, approved 2026-09-30. Kickoff 11:20. Branch `feature/asl-backlog-pass-22b`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| 22b.1: branch `UI-Redesign-01` merged into the pass branch (clean; site.css, local fonts, viewport data attributes) | 11:20 | 11:21 | 0:01 | |
| 22b.2: S01 `StudioNavigation` (grouped sidebar, collapsible, a drawer under 1024px, Escape returns focus), `studioShell.js`, S02 `PageHeader` on ten pages, the draft restriction as a warning banner; checked in the Studio (wide, collapsed, nested route, narrow drawer) | 11:21 | 11:28 | 0:07 | |
| 22b.3 and 22b.4: S03 to S07, S09, S10; K02 `CardPicker` on Play, Scenarios, and the card editor; K01 `NewGameFromCard` with K03 and K04; S03, S04, S06 adopted; 22b.5: `CardProvenance` in the Play library with the pass 17 registry embedded, K21 `CardProvenancePanel` on Scenarios and Play; 12 component tests, 6 provenance tests, page assertions; MapStudio 175 pass | 11:28 | 11:46 | 0:18 | Session paused here for a new chat |
| Visual check in the Studio (map-studio-scripted): the ten page headers, Play new-game fields and start summary, the card picker on Play, Scenarios, and the card editor, the editor findings and save feedback, the provenance panel on Scenarios and in a card game, Play at phone width; fixed the Unit Lab picker overflow and preview overflow, the Scenarios picker spacing, the scripted-dice note, the start summary date (the card date wording), and the provenance table alignment; rebuilt and checked again, clean | 11:50 | 12:00 | 0:10 | Resumed in a new chat |
| Two reviews (UI and Blazor, 12 findings; table player, 11 findings) and fixes: the shell's Escape and focus, the status region, the game's provenance cached, the gone card's recorded start, the user card's file, blank provenance fields, the editor's outcomes; counters per side with Balance counters apart, the start's Game Turns and setup order, the enforced SSRs, FBE, the Balance texts and dr wording, plain value names and comparison words; shell and page tests | 12:00 | 12:07 | 0:07 | The reviewers ran from 12:00 to 12:04 |
| Visual check again (Play start and Balance, Scenarios provenance for all three cards and a user card, the drawer by keyboard at 800px), two wording fixes and a CSS specificity fix, rebuilt and checked, clean | 12:07 | 12:11 | 0:04 | |
| Documents (design, review, the plan's status, backlog section 34) while the full suite ran | 12:11 | 12:15 | 0:04 | |
| Full local suite and ScenarioA1 (480): one MapStudio assertion on the old date wording, fixed and rerun (176 pass); Authoring fails only the known architecture test (`.git/worktrees` held); formatting verified | 12:11 | 12:59 | 0:48 | Mostly waiting on the runs |
| Merge gate: commit af3c959, push, the Docker Linux check (every step exit 0; Authoring 167, Play 563, MapStudio 176, ScenarioA1 480), the chart supplement regeneration (same), merge into main, push | 12:59 | 13:22 | 0:23 | |
| **Pass 22b total** | 11:20 | 13:22 | **1:58** | Estimate 5:30; the session break (11:46 to 11:50) is not counted |

## Pass 22c: The collection pages

Estimate: 5:00 (build 3:45), the ASL Card Play and Map Studio Redesign Plan. Kickoff 13:30 on the user's go-ahead. Branch `feature/asl-backlog-pass-22c`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan sections 12, 14 to 17 and the H, M, G, and F candidates; the six pages and their tests | 13:30 | 13:33 | 0:03 | |
| 22c.1 Board library: H01 `AuthoredBoardList`, H02 `MapSummaryList`, H03 `BoardScopeSummary`, H05 `VaslBoardTable` (rows resolved once on load), the filter bar (search, type, VASL status, out of scope, count, Clear), loading, failure, empty, and no-match states, filters kept for the session (`LibraryViewState`) and scroll kept for the tab (`scrollMemory.js`) | 13:33 | 13:36 | 0:03 | |
| 22c.2 Maps and New board: M01 `MapPlacementEditor` keyed by draft id, M03 `ScenarioRuleEditor` keeping order, M04 `PlacementTextEditor` in an Advanced section, M05 `MapBuildActions` with the last check labeled out of date, M06 `NewBoardForm`, M08 `SourceDraftNotice` (New board and the board editor) | 13:36 | 13:38 | 0:02 | |
| 22c.3 Game states: G01 `GameReplayToolbar` composing S09 and S10 (slider, the test ids kept, a refused revision redrawn), G03 `GameContextSummary`, G04 `ProjectedGameUnitTable`, G06 `ReadCaseForm`, G07 `ReadCaseResult`, `GameText` | 13:38 | 13:40 | 0:02 | |
| 22c.4 Fidelity and Settings: F01 `FidelityRunControls`, F02 `LosFidelityPanel`, F04 `FidelityReportPicker`, F05 `FidelityReportMetadata`, F06 `FidelityResultsTable`, H03 reused for the report totals; Settings measures the cache once | 13:40 | 13:42 | 0:02 | |
| Build, `CollectionPageTests` (library, composer, run controls), one library assertion on the new toggle label; MapStudio 180 pass | 13:42 | 13:47 | 0:05 | |
| Visual check in the Studio: Board library (search, status filter, out of scope, return from a viewer with filter and scroll kept), Maps (placements, rules, the compact form, a check then an edit), New board (a VASL draft notice, a taken reference refused), Game states (German view, revision 8, a case read), Fidelity, Settings; fixed a green result shown while out of date, the report picker and library link spacing | 13:47 | 13:52 | 0:05 | |
| Two reviews (table player, 11 findings; UI and Blazor, 12 findings incl. missing tests) and fixes: VASL status narrows to VASL boards, the count without hidden boards, one no-match message, scroll restored only on back or forward, rules moved up and down, board titles in the picker, stale findings hidden, no old result on another map, duplicate rules kept once (a keyed-list crash), the slider width, the Advanced section kept open, disposal during import, the authored key, one status region per Fidelity job; four tests; MapStudio 182 pass | 13:52 | 14:00 | 0:08 | The reviewers ran from 13:53 |
| Visual check again (library status and type filters with the VASL-only controls disabled, the single no-match message, Maps rule Up and Down, board titles, Game states slider): fixed the rule buttons' spacing and disabled link buttons that looked active; rebuilt and checked, clean | 14:00 | 14:03 | 0:03 | |
| Documents (design, review, the plan's status and a note in section 12.2, backlog section 35) and formatting, while the full suite ran | 14:03 | 14:06 | 0:03 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; one MapStudio test failed under load, which found a real race (a check reporting on a map opened after it started, fixed) and two timing-dependent reads in the new tests (now waited for); MapStudio 182 pass three times | 14:03 | 14:49 | 0:46 | Mostly waiting on the runs |
| Merge gate: commit 5931150, push, the Docker Linux check (every step exit 0; Authoring 167, Play 563, MapStudio 182, ScenarioA1 480), the chart supplement regeneration (same), merge into main, push | 14:49 | 15:14 | 0:25 | |
| **Pass 22c total** | 13:30 | 15:14 | **1:44** | Estimate 5:00 |

## Pass 22d: The workspaces

Estimate: 5:45 (build 4:30), the ASL Card Play and Map Studio Redesign Plan. Kickoff 16:59 on the user's go-ahead. Branch `feature/asl-backlog-pass-22d`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan sections 12.3, 12.4, 15.5, 16.6, 16.7, 16.14; the viewer, the editor, the Unit Lab, the unit editor, the inspector, and the viewport script; the tests on them | 16:59 | 17:04 | 0:05 | |
| 22d.1 B06 `BoardViewport`: owns the host, the module, the viewport, the .NET reference, the callbacks (plain delegates, so a hover does not redraw the page), the commands, and disposal (a viewport created after disposal is disposed at once); `boardViewport.js` now removes its host key listener on dispose | 17:04 | 17:06 | 0:02 | |
| 22d.2 Viewer: B01 toolbar, B02 view picker, B03 comparison, B04 Layers panel, B05 game and replay strip composing S09 and S10 (perspective and revision kept in words), B07 unit inspector with B08 inline and U05 details, B09 stack list, B10 LOS panel with B11 inline (blocked, clear, not answered as badges), B12 provenance, B15 `InspectorTabs` (Selection, LOS, Evidence; arrow keys, Home, End), E06 grid samples in Evidence | 17:06 | 17:08 | 0:02 | |
| 22d.3 Editor: B13 header (unsaved or saved, Undo, Redo, Save; a rename alone can be saved), B14 tool options, E08 `TerrainPicker` (tool options and feature properties), B15 tabs, a confirmation before leaving unsaved changes (`NavigationLock`); build clean | 17:08 | 17:10 | 0:02 | |
| 22d.4 Unit Lab: U01 template picker, U02 attached equipment keyed by draft, U03 tier grid, U04 face gallery, U05 details (also the viewer's), U06 findings through S06, U07 document output with S14 `JsonDisclosure`, U08 style sheet in an Advanced section, U09 placement with U10 inline; E01 `VocabularyAttributeInput`, E03 `UnitFaceEditor`, E04 `UnitAttributeFields`, identity fields grouped; the preview column kept beside the fields; MapStudio 182 pass | 17:10 | 17:13 | 0:03 | |
| `WorkspaceComponentTests`: the viewport's mounting (once), callbacks, commands, and disposal; the tabs' keyboard and tab order; the editor header's saved and unsaved states | 17:13 | 17:16 | 0:03 | |
| Visual check in the Studio (the pane stopped drawing partway, so later checks read the page state): viewer on bd01 (Layers panel, replay strip, a unit clicked, Selection, LOS from the selected hex blocked, Evidence samples and provenance, the Russian view clearing a hidden German unit, Comparison in swipe, a route change to bd02 reloading one viewport), editor (header saved and unsaved, a rename, leaving asked, declined, then accepted; tool options), Unit Lab (identity group, faces, tiers, findings, style in Advanced, the preview beside the fields); no overflow at 1100 and 1300px. One stuck navigation while the pane was hidden could not be reproduced in three tries | 17:16 | 17:22 | 0:06 | |
| Two reviews (table player, 11 findings; UI and Blazor, 8) and fixes: LOS badges not definitive on an unverified board and "not read" for a bad Location, a unit clicked on the LOS tab keeps it, Escape named in the hint, "Pixel clicked", no Evidence count, what a perspective sees, the Validation tab kept, a name typed back not a change, a broken sheet opens Advanced and says so, the placeholder and state labels; the Lab rules reach the moved editors, the leave question with the navigation's cancellation and no circuit loss, diagnostic paths kept, disposal while loading, commands during disposal, a new board without the old LOS line or selection; one test; MapStudio 186 pass | 17:22 | 17:33 | 0:11 | The reviewers ran from 17:23 |
| Visual check again (the pane drew for the Unit Lab, then stopped; the rest read the page state and computed styles): the Lab's moved editors styled again, the LOS badges ("not read", "clear"), a route to bd02 dropping the old LOS line, the editor's name typed back reading saved, leaving declined then accepted; clean | 17:33 | 17:34 | 0:01 | |
| Documents (design, review, the plan's status, backlog section 36) and formatting, while the full suite ran | 17:34 | 17:37 | 0:03 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; MapStudio 186, Play 563 | 17:34 | 18:00 | 0:26 | Waiting on the runs |
| Merge gate: commit 7532322, push, the Docker Linux check (every step exit 0; Authoring 167, Play 563, MapStudio 186, ScenarioA1 480), the chart supplement regeneration (same), merge into main, push | 18:00 | 18:19 | 0:19 | |
| **Pass 22d total** | 16:59 | 18:19 | **1:20** | Estimate 5:45 |

## Fix: three Studio styles found in the pass 22d demonstration

On the user's go-ahead after the demonstration, branch `fix/studio-22d-styles`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| The LOS panel's From and To on their own rows (its actions no longer the toolbar band), the editor's tool rail wrapping its help and fitting its fields, a disabled primary button greyed; tried in the Studio, written, rebuilt, and checked again | 18:34 | 18:36 | 0:02 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; MapStudio 186, Play 563. No Docker check: a UI fix seen in the Studio (the user's rule) | 18:46 | 19:18 | 0:32 | Waiting on the runs |
| Merge into main (--no-ff), push, GitHub Actions checked | 19:19 | 19:22 | 0:03 | |
| **Fix total** | 18:34 | 19:22 | **0:38** | Working time, the break from 18:36 to 18:46 left out |

## Pass 23: Per-side views and hidden setup

Estimate: 5:25 (build 4:10), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-23`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading, while the style fix's suite ran: plan sections 5 (pass 23), 13, 15.4, 16.10, 16.13, 16.17; the Backlog Passes Plan section 1 and rulings R10.10, R18 to R22; backlog sections 20, 27, 29, 31; A2.9, A12.11 to A12.15, A12.3 to A12.34, A26.15 in the PDF; `GameView`, the card setup, the Victory fold, the Play page's perspective | 18:47 | 19:18 | 0:31 | Overlapped the style fix's test runs |
| Rulings R23.1 to R23.6 proposed; the user chose the rulebook for sequential setup (A12.12, A2.9) | 19:19 | 19:32 | 0:13 | Includes the fix's merge |
| 23.1 to 23.4 rules: `GameView` with sides out of sight during setup and the top counter of an enemy stack before play (A12.12, A2.9); the `setup-concealed` event (a setup event); HIP by the SSR token `hip:<side>:<n>` in the setup checks and the card's SSR check; the non-OB "?" as a setup `conceal` argument, range before LOS; `asl.game.place-hidden` (A12.32); Control and unbroken squad-equivalents as a side knows them (A26.15) | 19:32 | 19:44 | 0:12 | |
| Play page first cut: the hand-over screen and P02 `LiveGameToolbar` (the board link follows the view), K08 `VictoryStandingTable`, a cached view, unit and equipment pickers reading the view, the out-of-sight note, the non-OB "?" fieldset, the place-hidden buttons; styles | 19:44 | 19:48 | 0:04 | |
| `BacklogPass23Tests` (7): setup out of sight and the top counter, HIP needs its SSR, the SSR's allowance and the leader, place-hidden, a malformed token, the non-OB "?" near and far, Control as known; the gate's effect checks for the two new events | 19:48 | 19:56 | 0:08 | |
| Page tests through the hand-over (a `ViewAs` helper for the 20 view changes); the before-play rule refined (equipment beneath a stack is withheld, a fortification is a top counter); a state test updated to A2.9 | 19:56 | 20:00 | 0:04 | |
| 23.5 components: P01 `ScriptedDicePanel`, P07 `GameReplayFailure`, N02 `NightWeatherSummary`, R01 `ProposalReviewPanel` with R02 `EntryReviewFacts` and R03 `FireReviewFacts`, R12 `PlayUnitTable` (rows, never the state), R13 `AdjudicatorAuditPanel` (lines only for the adjudicator), K05 `SetupPoolsTable`, K06 `PlayCardPanel` with K07 `GameEndNotice` and K08; `PlayComponentTests` (4, the Victory standing pinned in both views) and a page test of the hand-over and the board link; MapStudio 191 pass | 20:00 | 20:10 | 0:10 | |
| Visual check in the Studio: a Gambit game (`p23-visual`): the British set up, the German view showing the E6 stack's top counter and two counted beneath; the hand-over screen (focus on its button, the map and panels absent, the board link gone); the British view while the Germans set up (the note, no German counter on the map or in the table); the non-OB "?" fieldset (Q10 and Y10 offered by the board's LOS), one placed on Y10 and seen as "?" by the British; play started, the standing as the British know it (7 Known squad-equivalents of 8), the adjudicator's with its "would win"; the board viewer from the British link; a minimal game (`p23-hidden`) with a hidden squad, its place-hidden button, the Russian view after it; the hand-over at 375px. Two wordings fixed (the standing's "Known to you", the HIP note) | 20:10 | 20:17 | 0:07 | |
| Rulings R23.1 to R23.6 written; three reviews (referee, 9 findings; table player, 9; UI and Blazor, 13), run in parallel | 20:17 | 20:25 | 0:08 | The reviewers ran from 20:18 |
| Review fixes: a hand-over drops every draft but the open CC declarations; the Move and Fire panels only for the side acting (and the adjudicator); CC, SW, and entry pickers from the view; a side with no view sees nothing; setup events of instances a side may not see left out of its event list; a Dummy always "?"; out of sight by OB group; HIP for Infantry only, a HIP token naming a card side; no non-OB "?" on a broken or berserk unit, and no enemy Location named in its refusal; a masked setup table; one view helper for the Games page; the setup, out-of-sight, and Victory reads cached per view; K06 and K08 contracts narrowed; no focus move on the hand-over; labels; 5 page tests handed over to the firing side; 5 tests added; Play 48 of the pass's set, MapStudio 192, Units 406 pass | 20:25 | 20:34 | 0:09 | |
| Visual check again: the British view in the German PFPh (the Fire panel's note, no German concealed unit among the pickers), the Games page's British event list during the German setup (British creations only); clean | 20:34 | 20:36 | 0:02 | |
| Documents (design, review, rulings updated for the review fixes, the plan's status and decisions, backlog section 37 with six rows removed) and formatting, while the full suite ran | 20:36 | 20:41 | 0:05 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; Play 573, MapStudio 192, Units 406 | 20:37 | 21:12 | 0:35 | Waiting on the runs |
| Merge gate: commit c90cada, push, the Docker Linux check (every step exit 0; Authoring 167, Play 573, MapStudio 192, ScenarioA1 480), the chart supplement regeneration (same), merge into main, push | 21:12 | 21:35 | 0:23 | |
| **Pass 23 total** | 19:19 | 21:35 | **2:16** | Estimate 5:25; the reading (0:31) ran earlier, during the style fix's test runs |

## Fix: pass 23 demo, the pickers the reviews missed

Found in the Studio demo of pass 23 on the user's request, branch `fix/pass23-demo-pickers`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Demo in the Studio: a fresh Gambit game (`p23-demo`) through setup, the hand-over, the British view while the Germans set up, the non-OB "?", and play; found the British view offering the German Opportunity Fire list, with the two units under the Y10 "?" | 21:37 | 21:42 | 0:05 | |
| Fix: every action's unit list passes through one `SeenOnly` filter (ten lists); the Opportunity Fire and Advance panels only for the phasing side and the adjudicator; checked again in the Studio (the British view in the PFPh and the APh); a page test; MapStudio 192 pass | 21:42 | 21:50 | 0:08 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; Play 573, MapStudio 192. No Docker check: a UI fix seen in the Studio (the user's rule) | 21:51 | 22:22 | 0:31 | Waiting on the runs |
| Merge into main (--no-ff), push, GitHub Actions checked | 22:22 | 22:24 | 0:02 | |
| **Fix total** | 21:37 | 22:24 | **0:47** | |

## Pass 24: Control of Locations and more VP

Estimate: 3:25 (build 2:10), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-24`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 24, backlog sections 31 and 37, rulings R21.1 to R21.5 and R23.4, the pass 21 and 23 designs; A26.1 to A26.23, A12.153, A12.154 in the PDF; `ScenarioVictory`, the Victory planner, a survey of vehicles, Guns, capture, and actions (an agent) | 22:29 | 22:35 | 0:06 | The survey agent ran to about 22:42 |
| Rulings proposed; the user's three answers (unentered Locations keep their own Control, a building needs no Location, Mopping Up with its Search casualties) | 22:35 | 22:45 | 0:10 | |
| 24.1 to 24.4: Location items and the fold with `VictoryReading`, `VictoryCache`, start Control per hex, vehicles' temporary Control, Gun and vehicle VP and CVP; `building-mopped-up` (record, reader, writer, projector, gate) and `asl.game.mop-up`; 24.5: K08 rows and `MoppingUpAction` | 22:45 | 22:59 | 0:14 | |
| `BacklogPass24Tests` (10) and two component tests; vehicle MA read from its type as well as the catalog's MA weapon | 22:59 | 23:12 | 0:13 | |
| Visual check in the Studio: a Guards game (`p24-visual`) set up and played to the Russian PFPh, a Mop Up of F3, a squad walked into F5's H5 and its Location row, the German PFPh refusal; four issues fixed (none checked by default, places on labels, one-hex one-level buildings left out, singular wording) | 23:12 | 23:20 | 0:08 | |
| Rulings R24.1 to R24.7 written; three reviews (referee, 12 findings; table player, 10; UI and Blazor, 12) run in parallel | 23:20 | 23:22 | 0:02 | The reviewers ran to about 23:32 |
| UI review fixes (stale drafts, hints, places with the board, the fieldset's row) and the visual check of the earlier fixes | 23:22 | 23:26 | 0:04 | |
| Referee and table player fixes: a vehicle in Bypass outside the building, armed vehicles by inherent crew, the guard per A20.5, cellars left out, a MG MA disabled, refusal reasons, what the Mop Up found, grouped Location rows; three tests; 12 and 7 pass | 23:26 | 23:33 | 0:07 | |
| Visual check again: the German view's panel (F5, K5, M7; places; none checked; the hint), a Mop Up of K5 and the list after it; clean | 23:33 | 23:36 | 0:03 | |
| Documents (design, review, rulings updated, the plan's status, backlog sections 31 and 38) while the full suite ran | 23:36 | 23:40 | 0:04 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; the action registry test needed the new action (fixed, rerun); Play 585, MapStudio 195, Units 406 | 23:36 | 00:16 | 0:40 | Waiting on the runs |
| Merge gate: commit 52dd9c8, the Docker Linux check (every step exit 0; Authoring 167, Play 585, MapStudio 195, ScenarioA1 480), the chart supplement regeneration (same), merge into main, push | 00:16 | 00:41 | 0:25 | |
| **Pass 24 total** | 22:29 | 00:41 | **2:12** | Estimate 3:25 (build 2:10); the build to the documents took 1:11, the full suite 0:40, the merge gate 0:25 |

## Pass 25: Entry and exit

Estimate: 6:05 (build 4:50), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-25`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 25, sections 15.3 and 16.17, backlog sections 19, 25, 30, 31, and 38, rulings R20.5, R20.6, R21.5, and R24.3, the pass 20, 21, and 24 designs; A2.5 to A2.6, A4.134, A4.14, A4.3, A4.7, A8.22, A12.15, A20.53, A24.1, A26.22, and A26.23 in the PDF; `PlanEnter`, `PlanMove`, `PlanExit`, `PlanAdvanceUnits` | 07:20 | 07:27 | 0:07 | |
| Rulings proposed; the user's three answers (named entry hexes, forced back off board, the APh holds) | 07:27 | 07:40 | 0:13 | No split proposed |
| 25.1 to 25.5 and 25.7 in Units and Play: `OffMapPosition.Entry`, `AdvanceMoved.Exit`, `UnitExit.CapturedBy` and `Escort`, the forced back off board, `ExitUnits`; `EntryCheck`, `EntryHexesFor`, `EntryStep`, `GroundStep`, `BypassStep`, entry by advance, the new `PlanExit`, offboard Deployment, the setup entry area and group checks, the pass and end-move fixes, Exit VP and CVP | 07:40 | 08:03 | 0:23 | |
| `BacklogPass25Tests` (10), the gate's readbacks, the Bypass entry fix (`BypassEntered`); Play 31 of pass 20, 21, 25 | 08:03 | 08:20 | 0:17 | Test runs about 5 to 9 minutes each |
| 25.6: thirteen components, the page's lists and notes, K08; `PlayPagePass25Tests` and `MovementComponentTests`; MapStudio 203 | 08:20 | 08:32 | 0:12 | |
| Visual check in the Studio (`p25-visual`, `p25-smoke`): entry in the MPh, the APh hold, entry and exit by advance, SMOKE choices; three fixes (wording, hex order, TI movers) | 08:32 | 08:40 | 0:08 | |
| Rulings R25.1 to R25.7 written; three reviews (UI and Blazor, 9 findings; table player, 11; referee, 7) run in parallel; the design drafted meanwhile | 08:40 | 08:50 | 0:10 | The reviewers ran to about 09:00 |
| Review fixes: disclosure of the reaction and Deploy lists, draft resets and pruning, accessibility; the APh hold by each unit's own advance, the `entry-due` text, open named hexes and forced-back notes, exit scoring and compass edges, the exit order message; stacking at entry, the leader's aid in an exit by advance, a Guard by any edge, the named-hex refusal; tests; Play 31, MapStudio 204 | 08:50 | 09:11 | 0:21 | |
| Visual check again (`p25-check`, `p25-check2`): the fieldset (styled), notes, compass edges, the `entry-due` text, an exit by advance's scoring (reordered); clean | 09:11 | 09:17 | 0:06 | |
| Documents (design, review, rulings, the plan's status, backlog section 39 and rows removed, this log) while the full suite ran | 09:17 | 09:22 | 0:05 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; Play found the pass 20 table player test expecting an off-board Deployment refused (R25.4 allows it): updated, rerun, passes; Play 596, MapStudio 204, Units 406 | 09:17 | 09:45 | 0:28 | Pass 26's reading and survey ran meanwhile, at the user's request |
| Merge gate: commit 4ab541d, the Docker Linux check (every step exit 0 but MapStudio: one timing failure in `CollectionPageTests`, the library page, untouched by the pass; MapStudio rerun twice in the same container, 204 and 204; Authoring 167, Play 596, ScenarioA1 480), the chart supplement regeneration (same), merge into main (16eb160), push | 09:45 | 10:08 | 0:23 | |
| **Pass 25 total** | 07:20 | 10:08 | **2:48** | Estimate 6:05 (build 4:50); the build to the documents took 2:02, the full suite 0:28, the merge gate 0:23 |


## Pass 26: Vehicles and Guns on a card

Estimate: 6:00 (build 4:45), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-26`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 26, sections 15.3 and 16.17, backlog sections 12, 18, 21, 22, 29 to 31, 37 to 39, rulings R20.5, R21.5, R23.5, R24.3, R25.1 to R25.5; A2.52, A5.5, A12.34, C10, C11.2, D2.4, D6.4, D6.5 in the PDF (the survey of pass-26-prep reused) | 12:10 | 12:14 | 0:04 | |
| Rulings proposed; the user agreed with all five recommendations (loading and unloading; limbering and en portee to the backlog; A12.34 in Concealment Terrain only; the MPh holds vehicles) and one pass | 12:14 | 12:15 | 0:01 | No split |
| 26.1 to 26.4, 26.6, 26.7 in Units and Play: `VehicleStepped` entry, exit edge, load and unload; `GameState.Aboard` and `Passengers`, Passengers left out of `At`; the projector's entry from off board, load, unload, exits recorded with Passengers and towed Gun, `KeepPassengers`; `GamePlanner.Passengers` (entry, load, unload, capacity, the MPh hold, the aboard refusal); setup counters (Passengers, towed and manned Guns, a manning crew as a squad, A12.34 HIP); the crew's push off the map; Gun Exit VP and CVP; hook-up with the crew boarding, the QSU refusal; A12.34's reveal by fire; the Armor Test card | 12:15 | 12:40 | 0:25 | |
| `BacklogPass26Tests` (9): the card's checks (legacy note, Balance, citable rules), the test helpers, a towed Gun's position, the push readback | 12:40 | 12:53 | 0:13 | |
| 26.5: ten components (P08, P09, A17 to A22, C18, S13), the page's setup drafts (Passengers, towed Guns, vehicles off board, the entry area), the vehicle panel (off-board entries, load and unload, the hex wished to enter next as VCA buttons, the crew boarding), cargo notes in the unit table; `VehicleComponentTests` (7); the full Play project run (602 of 605; three earlier tests updated for R26.5 and the fourth card, then 12 pass) | 12:53 | 13:12 | 0:19 | Rulings R26.1 to R26.8 drafted while Play ran |
| Visual check in the Studio (`p26-visual`, Armor Test): setup with a hidden Emplaced Gun, Passengers, and a towed Gun; the MPh hold; entry; stop and unload; the truck's entry and unhooking for its crew; the tank's entry, the hex wished to enter next, its exit and the standing (6 CVP to the Russians off a hex no condition names); boarding on Turn 2; the Gun's arc list (no LOS anywhere, so the A12.34 reveal stays with the planner tests). Fixes: the entry as a hex select, a VCA picker, and one button (66 buttons before); "manned by" in the list; `data-bypass` values; the vehicle exit's scoring note; Passengers left out of every action list | 13:14 | 13:29 | 0:15 | Screenshots timed out; checked through the page's DOM |
| Three reviews in parallel (UI and Blazor, 16 findings; table player, 16; referee, 13), the design drafted meanwhile | 13:29 | 13:40 | 0:11 | |
| Review fixes: the vehicle panel's moving and Stopped states, towing as the planner allows it, cargo for the own side's view, the entry area and Passenger drafts reset and filtered, placement keys, one crew-boards box, the entry button's reasons, entry options read once per revision; the push exit from the page and its refusals, Passengers out of fire lists, Control, and A12.34's spotting, placement order; unloading from a Prep-Fired, immobilized, or Abandoned vehicle, broken Passengers free of rout, Recall exits unscored, boarding MF kept, pushing on and off the map, a captor's Gun exit; `PlayPagePass26Tests` (1), a planner test (Play 10, MapStudio 8) | 13:40 | 13:48 | 0:08 | |
| Visual check again (`p26-check`): the placement note, the entry button's reason, the moving and stopped panels, one crew-boards box; clean | 13:48 | 13:51 | 0:03 | |
| Documents (design, review, rulings updated, the plan's status, backlog section 40 and the rows built, this log) while the full suite ran | 13:51 | 13:58 | 0:07 | |
| Full local suite and ScenarioA1 (480): Authoring fails only the known architecture test; Play 606; MapStudio found two tests changed by the pass (the cards page now shows Armor Test first; a vehicle page test that had always stopped early on `data-bypass` now reaches the CC Reaction Fire panel, the DEFENDER's since pass 25): fixed, MapStudio rerun 212 | 13:51 | 14:24 | 0:33 | Documents written meanwhile |
| Merge gate: commit b0c5d4f, the Docker Linux check (every step exit 0; Authoring 167, Play 606, MapStudio 212, ScenarioA1 480), the chart supplement regeneration (identical), merge into main (bc0e8f4; a first attempt left the branch's new files untracked, checked identical and removed), push | 14:24 | 14:46 | 0:22 | |
| **Pass 26 total** | 12:10 | 14:46 | **2:36** | Estimate 6:00 (build 4:45); the build to the documents took 1:41, the full suite 0:33, the merge gate 0:22 |


## Pass 27: Heat of Battle, Leader Creation, and berserk gaps

Estimate: 5:15 (build 4:00), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-27`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 27, the backlog rows, rulings R10.7, R10.11, R10.15, R11.17, R15.2, R15.13; A25.8 to A25.85 and the National Capabilities Chart in the PDF (the survey of pass-27-prep reused) | 15:30 | 15:36 | 0:06 | |
| Rulings R27.1 to R27.5 proposed with the six questions; the user agreed with every recommendation (one shared Axis Minor set with the nation per side, counter values now and the other A25.8 rules to the backlog, catalog 1.13.0, the companions' TCs beside the CC record, level-aware neighbours for the charge only, one pass) | 15:36 | 16:05 | 0:29 | Mostly waiting for the answers |
| 27.5 handed to a background agent in a local clone (`.git/worktrees` is held, so no worktree): 14 P1 components and `CloseCombatComponentTests` | 16:05 | | | Runs alongside 27.1 to 27.4 |
| 27.1: vocabulary 1.16.0 (`axis-minor`), palettes 1.1.0, 17 Axis Minor counters transcribed from the chart (NCC, LGR, CEX), catalog 1.13.0 rebuilt from its sources, the synthetic catalog, the re-pins (66 version strings, four packages' digest chains); Replacement and Battle Hardening chains (A25.84), Heat of Battle +3 and surrender on 10, Leader Creation drm 0 and unknown nationalities refused, a side's nation, No Quarter for Hungarians against Romanians, a card side's nation | 16:05 | 16:19 | 0:14 | |
| 27.2: the charge's route by a forward search over Locations, stairwell levels, and Bypass lanes; the first moves of every shortest route; a charge into an enemy vehicle's Location and the berserk unit's vehicle CC required in the CCPh | 16:19 | 16:33 | 0:14 | A Gun crew's Location stays barred (CC with a crew is backlog row 120) |
| 27.3: A.9 Random Selection among Dummies and real units in a charge; the berserk OVR's CC at once in the MPh (calculator, projector, planner gate, Melee after it) | 16:33 | 16:43 | 0:10 | The page parts wait for 27.5 |
| 27.4: `FireFollowUps` (Spraying Fire's second Location, Encirclement, Fire Lane, a DC's thrower attack and removal) carried in the resume; a DC asks its owners' options | 16:43 | 16:48 | 0:05 | |
| Tests: `ScenarioA1Pass27Tests` (4), `BacklogPass27Tests` (5), the DC choice test, two earlier tests updated (the pass 10 OVR, the vehicle charge); 30 pass | 16:48 | 16:56 | 0:08 | |
| The app quit while the rules were building; the component agent stopped with its 14 components and tests uncommitted in the clone | 16:56 | 17:12 | 0:16 | Lost time; the user's Studio, under Visual Studio's debugger, then held port 5178 and the main checkout's MapStudio output |
| The agent's components checked (MapStudio 224 in the clone) and committed; the rules committed on the branch (b0fded2) and merged into the clone; `BerserkOverrunCloseCombat`; the catalog page test's list | 17:12 | 17:20 | 0:08 | Work moved to the clone, whose Studio runs on port 5179 |
| Visual check in the Studio (`p27-visual`, `p27-axis`, hand-written games on board 01): the charge notice, the OVR's CC in the MPh and Melee, the Fire panel and an attack, the CC panel and its parts, the Axis Minor counters; clean | 17:20 | 17:30 | 0:10 | Screenshots timed out; checked through the DOM |
| Three reviews in parallel (UI and Blazor, 10 findings; table player, 11; referee, 6); the rulings and design drafted meanwhile | 17:30 | 17:40 | 0:10 | |
| Review fixes: the 2-2-7 Fanatic, the OVR for a berserk MMC only, the SMC attacking back from the planner and every view told, a gate that cannot stall, a vehicle's Location with enemy Infantry barred, Abandoned vehicles no target, the Sniper checks after a resume, the Bypass lanes in the notice, the companions' TCs by leader, the custody and vehicle CC panels reading the view; tests (Play 88, ScenarioA1 4, MapStudio 13 targeted) | 17:40 | 17:46 | 0:06 | |
| Visual check again (`p27-visual`): the notice in the Russian view without a button, in the German view with one, and the CC from the planner's attacks; clean | 17:46 | 17:49 | 0:03 | |
| Documents (rulings updated, design, review, the plan's status, backlog section 41 and the rows built, this log) | 17:49 | 17:50 | 0:01 | |
| Full local suite in the clone: Dice 24, Authoring 167 (the architecture test passes in a clone), MapStudio 225, Maps 244, Maps.Rendering 34, Maps.Vasl 121, Play 613, ScenarioA1 484, Units.CounterSheets 19, Units.Rendering 352; Units found the synthetic catalog test's list missing the Axis Minor counters: fixed (0548516), Units 407 | 17:50 | 18:42 | 0:52 | Two runs were stopped at the background time limit and the rest rerun; the chart supplement regenerated identically meanwhile |
| Merge gate: the Docker Linux check (Docker Desktop started; a first run stopped for the Units fix; the rerun every step exit 0: Authoring 167, Play 613, MapStudio 225, ScenarioA1 484), merge into main, push | 18:21 | 18:50 | 0:29 | |
| **Pass 27 total** | 15:30 | 18:50 | **3:20** | Estimate 5:15 (build 4:00); 0:29 waiting for the answers and 0:16 lost to the app quitting; the build to the documents took 1:45, the full suite 0:52, the merge gate 0:29 |


## Pass 28: The card editor's forms and map picking

Estimate: 5:00 (build 3:45), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-28`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 28, backlog sections 29, 32, and 41, rulings R17.1 to R17.13, R19.3, R20.5, R21.5, R22.2, plan section 16.16 (K10 to K20); A19.1 and A26 in the PDF; survey of the card editor, `ScenarioCards`, `ScenarioCardLibrary`, the map service, B06, and the live games' card records | 19:42 | 20:05 | 0:23 | Found the editor dropping an Axis Minor side's nation |
| Rulings R28.1 to R28.5 proposed with four questions; the user took every recommendation (every field a form with the JSON read only; compose or take a saved map; warn, and keep a built-in card's earlier hashes; side id to the backlog) | 20:05 | 21:15 | 1:10 | Mostly waiting for the answers |
| 28.1 to 28.5 built: `CardDraft`, `CardMaps`, the earlier-hash check, the Guards SSR 3 revision, the game-to-card index, 17 card components, the editor and Scenarios pages | 21:15 | 21:33 | 0:18 | |
| The user's Studio under Visual Studio's debugger held the main MapStudio output: work committed and moved to a local clone; the clone's CRLF checkout broke the digest-pinned packages and a `checkout -- .` reverted uncommitted edits, redone by script | 21:33 | 21:42 | 0:09 | Lost time |
| Tests: `CardEditorDriver`, the editor tests through the forms, `CardEditorPass28Tests`; 38 pass | 21:42 | 21:50 | 0:08 | |
| Visual check in the clone's Studio (port 5179): Guards copy on board 01, Gambit on two boards, a saved map, picking a building and exit hexes, the Scenarios page; one finding (counter lines kept an area's old id) fixed | 21:50 | 21:59 | 0:09 | |
| Three reviews in parallel (referee, 10 findings; card author, 16; UI and Blazor, 12); Play 613 meanwhile; rulings and design drafted | 21:59 | 22:08 | 0:09 | |
| Review fixes (edge rule as the game's, Rowhouse and Factory walls, seam hexes, paired earlier hashes, values kept as read, picking ending with its target, textareas, keys, focus, names, marks layer, cached choices) and tests (MapStudio 104 targeted, Play 6) | 22:08 | 22:24 | 0:16 | |
| Visual check again: marks layer, picking ended by switching the Victory Conditions off, side lists, all four built-in cards valid against their boards; clean | 22:24 | 22:27 | 0:03 | |
| Full local suite in the clone: Dice 24, Authoring 167, Maps 244, Maps.Rendering 34, Maps.Vasl 121, Units 407, Units.CounterSheets 19, Units.Rendering 352, MapStudio 241, ScenarioA1 484, Play 615; documents written meanwhile (design, review, rulings, backlog section 42 and the rows built, the plan's status) | 22:27 | 22:53 | 0:26 | ScenarioA1 is not in the solution: it was built and run on its own |
| Merge gate: commit 99720b7, the Docker Linux check (every step exit 0; Authoring 167, Play 615, MapStudio 241, ScenarioA1 484), the chart supplement regeneration (identical), the time log, merge into main, push | 22:43 | 23:10 | 0:27 | Ran beside the end of the local suite |
| **Pass 28 total** | 19:42 | 23:10 | **3:28** | Estimate 5:00 (build 3:45); 1:10 waiting for the answers and 0:09 lost to the clone's line endings; the build to the documents took 1:12, the full suite 0:26, the merge gate 0:27 (overlapping) |


## Pass 28b: The rest of Play

Estimate: 4:25 (build 3:10), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead, after the user committed the uncommitted catalog work on the main checkout to main (2aefeed). Branch `feature/asl-backlog-pass-28b`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 28b, sections 13, 15.1 to 15.8, 16.10 to 16.13, 16.15, 16.17 (A02 to A10, C16, C17, N13, R04 to R10, S08), backlog section 23; survey of Play.razor and the Play components | 23:43 | 23:46 | 0:03 | |
| Four questions (section 23 additions, P2 candidates, RuleHelp's reach and shape); the user took every recommendation | 23:46 | 23:47 | 0:01 | |
| 28b.1 to 28b.4 built: 19 components, `PlayChoice`, `FireText`, the page edits, the Deploy split, the three records, the resets, the stylesheet; the Studio on 5178 stopped (user's rule); built | 23:47 | 00:00 | 0:13 | Writes into the repository got no verdict: files written in the scratchpad and copied in |
| Visual check in the Studio (port 6670 after 6666 proved browser-blocked; `p28b-visual` on a minimal user card): Deploy, transfer, DM, small arms fire, the mortar fields, the Rout panel; clean | 00:00 | 00:10 | 0:10 | Two restarts: the Guards card's hash, then its OB |
| Tests: `PlayPagePass28bTests` (4), `RallyAndRecordComponentTests` (6); the transfer record fixed for the DM the gate adds; commit d33c5d5; MapStudio 252 | 00:10 | 00:17 | 0:07 | |
| Three reviews in parallel (table player, 14 findings; UI and Blazor, 13; referee, 13); design and review drafted meanwhile | 00:17 | 00:30 | 0:13 | |
| Review fixes (kept DM, five summaries, record order, the Deploy record and legend, the SW note, Starshell limits, hidden ordnance values, one reset method, `SeenOnly`, readiness, RuleHelp's label) and tests; commit d9ff080; MapStudio 252 | 00:30 | 00:37 | 0:07 | |
| Visual check again (`p28b-visual2`): legend, note, Recover, DM fieldset, kept DM, record order and the Deploy record; clean | 00:37 | 00:38 | 0:01 | Through the DOM; screenshots timed out |
| Documents (design, review, the plan's status and section 15.9, backlog section 43 and the rows built, this log) | 00:38 | 00:45 | 0:07 | Ran beside the full local suite |
| Full local suite: Dice 24, Authoring 166 of 167 (the architecture test, held `.git/worktrees`; Docker passes it), MapStudio 252, Maps 244, Maps.Rendering 34, Maps.Vasl 121, Units 408, Units.CounterSheets 19, Units.Rendering 352, Play 615, ScenarioA1 484 | 00:38 | 01:09 | 0:31 | Play took 27 minutes |
| Merge gate: commit 2967b01, the Docker Linux check (every step exit 0; Authoring 167, Play 615, MapStudio 252, ScenarioA1 484), the chart supplement regeneration (identical), the time log, merge into main, push | 00:45 | 01:20 | 0:35 | Ran beside the local suite |
| **Pass 28b total** | 23:43 | 01:20 | **1:37** | Estimate 4:25 (build 3:10); the build to the documents took 0:55, the full suite 0:31, the merge gate 0:35 (overlapping) |


## Pass 28c: The Play workspace and hardening

Estimate: 5:15 (build 4:00), the ASL Card Play and Map Studio Redesign Plan. Kickoff on the user's go-ahead. Branch `feature/asl-backlog-pass-28c`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading: plan pass 28c, sections 11.1, 13, 14, 15.1 to 15.9, 16.13, 16.16, 17.3, backlog sections 37 and 43; survey of Play.razor, the Play components, the shell, B06, and the map service | 05:39 | 05:45 | 0:06 | |
| Four questions; the user chose the interactive BoardViewport for R11 (beyond the recommended markup move), all four backlog rows, the recommended activity strip, and Play fully hardened with the other pages checked | 05:45 | 05:45 | 0:00 | Kept as one pass: about 7:00 at the plan's rates, and recent passes ran at half their estimates |
| 28c.1 to 28c.3 built: R11 `PlayMapPanel` on B06, `GameMaps.Layers`, the context header, the picked hex, the workspace grid and tabs, hand-over on load and the outcome hand-over, busy and stale handling, focus, records by turn and phase, RuleHelp on ten older panels, ready notes, RPh headings, "Weapon" | 05:45 | 06:05 | 0:20 | Writes into the repository got no verdict: scratchpad and copy |
| Visual check in the Studio (port 6670; `p28c-walk`, `p28c-walk2` on the minimal user card) at 1920x1080, 1366x768, 1024x768, 800x900, 683x384 (200% zoom), and 320x640, and every page at 320px: eight issues fixed and checked again (narrow context bar, collapsed navigation 1024 to 1439px, workspace height, Review scroll, DEFENDER pass, control contrast, 320px reflow on nine pages, Covered Arc cache) | 06:05 | 06:21 | 0:16 | The user asked for rigorous Studio testing mid-pass |
| Tests: `PlayMaps` (viewport set up in the Play contexts, layers read), `PlayPagePass28cTests` (6), `PlayWorkspaceComponentTests` (6), the map assertions rewritten; commit 34d9684; MapStudio 264 | 06:21 | 06:30 | 0:09 | |
| Three reviews in parallel (referee, 7 findings and 7 summaries; table player, 14; UI and Blazor, 14 and test notes) | 06:25 | 06:33 | 0:08 | Beside the tests |
| Review fixes (the DEFENDER's own pass and its reasons, the status line and another game behind the hand-over, narrow focus, busy, stale with no revision, both-sides phases, blocking items first, context wording, measured workspace height, pixel breakpoints, load failure, map landmark, units table region, summaries) and tests; commit 6c5b28c; MapStudio 264 | 06:33 | 06:45 | 0:12 | A FiringSide change was reverted: it broke behavior three tests pin; backlog section 44 |
| Visual check again: a whole turn of `p28c-walk2` (RPh to CCPh, a surrender in the RtPh) at 1920x1080, 1366x768, 683x384, and 320x640; two issues fixed (button wrap, the both-sides note); commit aadc49d; clean | 06:45 | 06:52 | 0:07 | The user asked about turning the map at 320px: backlog section 44 |
| Documents (design, review, the plan's status and section 15.10, sections 16.13 and 16.17, backlog section 44 and the rows built) | 06:52 | 06:57 | 0:05 | Beside the solution build |
| Full local suite: Dice 24, Authoring 166 of 167 (the architecture test, held `.git/worktrees`; Docker passes it), MapStudio 264, Maps 244, Maps.Rendering 34, Maps.Vasl 121, Units 408, Units.CounterSheets 19, Units.Rendering 352, Play 615, ScenarioA1 484; the chart supplement regeneration (identical) | 06:52 | 07:30 | 0:38 | The solution build took 5:38; Play took 30 minutes; the user's question about ScenarioA1 led to a backlog row for renaming the project (4ab1662) |
| Merge gate: commit aad550c, the Docker Linux check (every step exit 0; Authoring 167, Play 615, MapStudio 264, ScenarioA1 484), the time log, merge into main, push | 06:57 | 07:37 | 0:40 | Ran beside the local suite |
| **Pass 28c total** | 05:39 | 07:37 | **1:58** | Estimate 5:15 (build 4:00); the build to the documents took 1:13 with two visual checks and three reviews, the full suite 0:38, the merge gate 0:40 (overlapping) |


## The ScenarioA1 project renamed Rules

Backlog section 44, on the user's go-ahead after pass 28c. No estimate. Branch `feature/rename-scenarioa1-to-rules`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Survey: references to the project and namespace (202), the explicit resource names, lock files, the CI workflow and Docker script, a Maps test naming the folders, three design documents; saved games record only package ids | 07:42 | 07:46 | 0:04 | Neither project is in the solution |
| Rename: two folders and project files, namespace in 161 files, partial qualifiers `ScenarioA1.` to `Rules.` in 3 files, lock files, CI, Docker script, documents noting the old name; solution, rules tests, and a locked restore build clean | 07:46 | 07:55 | 0:09 | No Studio check: nothing the Studio shows changes |
| Full local suite: Dice 24, Authoring 166 of 167 (the architecture test, held `.git/worktrees`; Docker passes it), MapStudio 264, Maps 244, Maps.Rendering 34, Maps.Vasl 121, Units 408, Units.CounterSheets 19, Units.Rendering 352, Play 615, Rules 484; the chart supplement regeneration (identical) | 07:56 | 08:25 | 0:29 | |
| Merge gate: commit 07084d2, the Docker Linux check (every step exit 0; Authoring 167, Play 615, MapStudio 264, Rules 484), the time log, merge into main, push | 07:56 | 08:31 | 0:35 | Ran beside the local suite |
| **Rename total** | 07:42 | 08:31 | **0:49** | The edit took 0:13; the rest was the test runs |


## The rules projects in the solution

On the user's go-ahead after the rename, with every new `--warnaserror` error fixed. No estimate. Branch `feature/rules-in-solution`.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Both projects added to `LimboDancer.Domains.Asl.sln` (`LimboDancer.Abstractions`, which `dotnet sln add` pulled in, taken out again); their lock-file and code-style opt-outs removed; lock files generated; a locked restore passes | 08:57 | 09:00 | 0:03 | |
| The `--warnaserror` build's 1,160 new errors fixed with `dotnet format` (style IDE0011 and IDE0040, then whitespace IDE0055), limited to the files named: the library 462 in 25 files, the tests 698 in 24 files; a token comparison shows no change but added braces and access modifiers; the separate rules-test steps of CI and the Docker check removed; the architecture design's note updated | 09:00 | 09:08 | 0:08 | Release and Debug build with no error |
| Full local suite: Dice 24, Authoring 166 of 167 (the architecture test, held `.git/worktrees`; Docker passes it), MapStudio 264, Maps 244, Maps.Rendering 34, Maps.Vasl 121, Units 408, Units.CounterSheets 19, Units.Rendering 352, Play 615, Rules 484; the chart supplement regeneration (identical) | 09:09 | 09:32 | 0:23 | |
| The merge gate's documents updated (the rules tests run with the solution; test file paths), commit 45d6712 | 09:15 | 09:20 | 0:05 | At the user's request, beside the suite |
| Merge gate: commit 3b6744f, the Docker Linux check (the solution's test step ran the rules tests: Rules 484, Authoring 167, Play 615, MapStudio 264; every step exit 0), the time log, merge into main, push | 09:09 | 09:34 | 0:25 | |
| **Total** | 08:57 | 09:34 | **0:37** | |


## Pass 29: The shared board workspace

Added by the user on 2026-10-02 as the first task of the UI and playability work. Estimate 4:30 (build 3:15). Branch `ui-improvements`. Each task is checked in the Studio only; the unit tests, reviews, and merge gate run once the batch of UI work is done.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Preparation: the plan's sections 13, 14, and 15.10, the pass 28c design and review, backlog sections 37, 43, and 44, a survey of Play and its components; four candidate groups | 18:05 | 18:20 | 0:15 | Approximate start |
| The planned passes from the old 29 renumbered up by one, pass 29 added to the plan's section 5 and schedule, the totals recomputed | 18:27 | 18:29 | 0:02 | Plus the backlog, three rulings, and a code comment |
| 29.1 `BoardInspector` moved out of the board viewer; checked in the user's Studio (5178): hex and counter clicks, tabs by keyboard, LOS, Evidence, Escape, hover | 18:29 | 18:38 | 0:09 | |
| 29.2 to 29.4: `BoardWorkspace`, the board viewer as its host, `GameMaps.Layers` with the overlay and view, Play on the workspace with a Proposal tab; `PlayMapPanel` and `PlaySelectedHex` retired; checked in my Studio (6670) at 1920x1080, 1366x768, 1024x768, 683x384, and 320x640 | 18:41 | 19:01 | 0:20 | One fix found in the Studio: a counter click lost the unit's details |
| The review's fixes and the turned map: "Rotate map" (CSS transform, pointer and drag kept right, remembered), the inspector under the map below 1024px, the Proposal tab held and counted only while Confirm waits, pickers disabled while the gate works, results for another view dropped, the workspace keyed by game and view, hover drawing the inspector alone, ids in the unit list, LOS From, and the small fixes; checked in my Studio (6670) at 1920x1080, 683x384, and 320x640, and on the board viewer | 19:47 | 19:56 | 0:09 | The review read the branch on GitHub; the backlog's rotation row leaves section 44 |
| The second review's fixes: Fit on a turned map fills the pane's width, the turn remembered per pane shape (a hidden pane has none), the pickers drawn disabled before the gate is asked, Cancel clearing the status, the reveal waiting for the context's height, `?game=` in the address, Good Order's unknown explained, sides spelled as on the counters; checked in my Studio (6670) at 320x640 and 1480x900, and on the board viewer | 20:17 | 20:23 | 0:06 | One fix found in the Studio: a hidden Map tab counted as a wide pane |
| The third check's fixes: a fitted view fitted again when its pane resizes, a turned map in a wide pane shown whole, the old storage key removed; the resize across 1024px checked in my Studio | 21:35 | 21:39 | 0:04 | |


## UI improvements from the UX analysis

Claude Design's analysis of the Studio (2026-10-02, the user's PDF). Small tasks on branch `ui-improvements`, each checked in the Studio only, in the order the user approved: the display formatter, the scenario card collapsed on Play, the board viewer's game banner with task 29.5, and the verification counts.

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Reading the analysis (8 pages) and sorting its findings | 22:00 | 22:10 | 0:10 | Approximate start |
| 1. `DisplayText`: sides as on the counters, Yes and No, kinds without `asl:`, board statuses in words, Locations as "F6 on bd01"; applied to Play's context, notes, hand-over, view picker, and non-OB labels, the Game states page, the board headers, and the inspector; checked in my Studio (6670) | 22:12 | 22:18 | 0:06 | |
| 2. The scenario card on Play starts closed; the context's "Card and Victory Conditions" link opens it below the context and focuses its title; the duplicate `play-card-link` id renamed in the card (`play-card-whole`); checked in my Studio: the workspace starts at 168px instead of about 830 | 22:18 | 22:22 | 0:04 | |
| 3. Task 29.5 (`hex=` in the board link, the viewer opening on that hex) and the board viewer's game banner (game, view, revision, Historical below the latest, Back to Play); checked in my Studio | 22:22 | 22:28 | 0:06 | |
| 4. Verification counts: the cause traced (the 24 Sep report predates importer 1.1.0), `StaleReason` shared by the library and the Fidelity page with a notice on each, the batch run again (235 verified, 1 failed, both pages agree in scope); the 5 boards with an underscore traced | 22:28 | 22:37 | 0:09 | The batch took 1.9 minutes |
| 5. Claude Design's review, items 1 to 8: sides and views everywhere, the "Card changed" badge, one game statement in the viewer, one status vocabulary with shared conditions text, plain names for F1, F2, F3 and the checks, Locations as "E4 on board 01", Fidelity opening on its failures with coloured chips and a legend, the 5 unlisted boards counted; checked in my Studio | 23:09 | 23:20 | 0:11 | |
| 6. The rest of the list: hints beside disabled Setup buttons, the viewer's Layers tab, card editor labels, the Unit Lab's sticky preview, the authored boards' names and "Kept in", compact Game states rows with "23 / 23", two-layer intros on Play and Game states, the card's Close button, the hex on "Back to Play", the library's plain out-of-date note; checked in my Studio | 23:21 | 23:31 | 0:10 | One fix found in the Studio: the hand-over on load cleared the hex from "Back to Play" |
