# Scenario A1 Backlog Pass 27: Heat of Battle, Leader Creation, and Berserk Gaps

**Date:** 2026-10-01

**Pass:** 27 of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), the fifth game pass. Design: [ASL Unit Backlog Pass 27 Design](<ASL Unit Backlog Pass 27 Design.md>). Rulings R27.1 to R27.5 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5; what is left out is in section 41 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

Three reviews ran in parallel on the built pass: a skeptical ASL referee, an experienced table player, and a UI and Blazor reviewer, since task 27.5 extracts Play components. Their findings and what was done follow.

## The user's rulings

Asked before building: one Axis Minor counter set shared by the five nations, with the nation named per side; the counter values now, and 1PAATC, no escape, red TH#, and PF in the backlog; catalog 1.13.0 with its re-pins; the berserk leader's companions' TCs shown beside the CC record, with no Heat of Battle in CC; a level-aware graph for the charge only; and one pass, C01 included.

## Scope settled while building

- A Gun crew's Location stays barred to a charge: CC with a crew is backlog row 120 (section 12), which needs crews in the CC package and Gun capture. R27.2 says so.
- No Wire is on any map, so A15.431's Wire at one MF has nothing to apply to.
- A card's side id is its nationality, so a card cannot hold two Axis Minor sides; Hungarians against Romanians play only from a setup that names both sides (section 41).

## Visual checks

The Studio ran from a local clone on port 5179 (the user's own Studio, under Visual Studio's debugger, held port 5178 and the main checkout's build output). The first check (`p27-visual` on board 01) played a berserk squad's charge notice, its move into a lone 7-0's Location, the DEFENDER's pass, the OVR's CC panel and its CC in the MPh (both missed and were held in Melee), the DFPh's Fire panel and an attack through it, and the CCPh's Close Combat panel with its stacking, withdrawal, attack builder, and Infiltration parts; `p27-axis` loaded Hungarians against Romanians and listed the Axis Minor counters. No issues. After the fixes the check ran again: the OVR notice appears to the Russian view without a button and to the German view with one, and the CC proposed from the planner's attacks commits. Screenshots timed out (the window was behind others), so both checks read the page's DOM.

## Referee findings

| # | Finding | Done |
|---|---|---|
| 1 | The 2-2-7 HS Battle Hardened into a 2-4-7; A25.84 makes it Fanatic like its 5-3-7 | Fixed; R27.1 and the test corrected. |
| 2 | A lone berserk SMC triggered the OVR's CC in the MPh; A15.432 and A4.15 give the OVR to a berserk MMC | `BerserkOverrunPending` needs a berserk MMC; a test. |
| 3 | Replay accepts an MPh CC record flagged as an OVR without re-checking the OVR's conditions | Section 41. |
| 4 | Ordinary A12.15 detection leaves Dummies out of the A.9 draw (before this pass) | Section 41. |
| 5 | Tests missing: the attack on the vehicle and the CCPh ending, an OVR that kills the SMC, the spray, Encirclement, and Fire Lane resumes, Hungarians against Romanians in play | The lone berserk SMC test was added; the rest are in section 41. |
| 6 | A Bypass lane must be named in the planner's hexside order; a second OVR onto a held SMC is not built; the OVR gate replays the log on every action | The charge notice now names each step's lanes in that order (the move itself needs that order, A4.31); the second OVR is in section 41; the replay cost is noted. |

## Table player findings

| # | Finding | Done |
|---|---|---|
| 1 | The ATTACKER decided whether the DEFENDER's SMC attacks back, and the DEFENDER's view saw no OVR | The SMC attacks back whenever it can (it never costs it anything), from the planner's `OverrunAttacks`; every view sees the notice, and only the phasing side's view proposes. |
| 2 | The OVR gate could lock the game if the CC were refused | `BerserkOverrunPending` is null when the Close Combat package refuses the round (the units wait for the CCPh); `take-prisoner` passes the gate. |
| 3 | A berserk SMC triggered the OVR | As referee 2. |
| 4 | A charge into a vehicle's Location that also holds enemy Infantry froze the stack (CC between Infantry beside a vehicle is not built) | Barred again for that case: the charge ends in place (R30.5). Setup cannot place a vehicle with enemy Infantry, so no test sets it up. |
| 5 | A DC's Sniper check was lost when its attack paused for an option (and an ordinary attack's always was) | `AddSniperAttacks` reads the earlier commits' dice, and the fire resume makes the checks. |
| 6 | The charge notice never named a Bypass | It names each step's lanes (`ChargeLanes`). |
| 7 | Berserk units charged Abandoned vehicles | Not targets now. |
| 8 | Destroying a vehicle in CC does not end berserk (A15.45) | Section 41. |
| 9 | The companions' TCs could be pinned on the wrong unit and repeated | Read from each Berserk TC's `berserk-leader:` DRM, on attacked units and companions alike, and shown with the first CC record after the leader went berserk. |
| 10 | Hungarians against Romanians cannot come from a card | Section 41. |
| 11 | A12.15's forced reveal of another unit after an SMC is drawn in a charge | Section 41. |

## UI and Blazor findings

| # | Finding | Done |
|---|---|---|
| 1 | The companions' TC text took the wrong leader and reported attacked units that went berserk as staying | As table player 9. |
| 2 | No tests for the OVR panel and the companions' text | A component test for `BerserkOverrunCloseCombat`; the page's companion text has no page test (section 41). |
| 3 | The SMC's attack back decided by the wrong side; a stale draft | As table player 1; the draft is gone. |
| 4 | `PrisonerCustodyActions` showed to every view and read the full state (R23.1) | Shown only to a view that may act for the phasing side; the heirs pass through `SeenOnly`. |
| 5 | The vehicle CC panel read the full state and offered every view the next side's attacks | Its units pass through `SeenOnly`; a Location is offered only to a view that may act for the side attacking next. |
| 6 | `CloseCombatRecords` was computed twice a render | Once. |
| 7 | 37 and 35 parameters on `CloseCombatPanel` and `SmallArmsFirePanel` | Section 41 (draft records). |
| 8 | P2 candidates left inline | Agreed: C03, C07, N05 inside `CloseCombatPanel`; C14, C15, N10 inside `SmallArmsFirePanel`; A12, C10, C11, N07 in the page, all short. |
| 9 | Accessibility: the OVR notice, the attacks' remove buttons | `role="status"` and `aria-live`; each remove button names its attack. The vehicle CC selects' names are in section 41. |
| 10 | Small component test gaps | Section 41. |

## Components

The 14 P1 roots are extracted (R27.5); the reviewer compared every block with the page before the pass and found every id, class, data attribute, label, option, disabled condition, and conditional block unchanged. `BerserkOverrunCloseCombat` is new.

## Tests

Play: `BacklogPass27Tests` (6), the DC choice test in `BacklogPass15Tests`, the vehicle charge in `VehicleStepsTests`, and the updated pass 10 OVR test. ScenarioA1: `ScenarioA1Pass27Tests` (4). MapStudio: `CloseCombatComponentTests` (13) and the catalog page's list.
