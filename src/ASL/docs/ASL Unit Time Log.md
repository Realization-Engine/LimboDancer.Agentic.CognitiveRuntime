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
