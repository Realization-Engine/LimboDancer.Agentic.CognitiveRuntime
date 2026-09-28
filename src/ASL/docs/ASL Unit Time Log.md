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
