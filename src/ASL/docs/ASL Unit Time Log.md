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
| Merge gate (full local suite, Docker Linux check) | 13:28 | 13:42 | 0:14 | All projects pass locally and in Docker; the branch push was refused by the auto-mode classifier and awaits the user |

Pass 3 total: 12:04 to 13:42, 1:38 against the 5 to 8 h estimate (with 0:14 of reading before the kickoff, alongside the pass 2 merge gate). The two second-pass reviews ran alongside other work; their fixes took 0:14 together.
