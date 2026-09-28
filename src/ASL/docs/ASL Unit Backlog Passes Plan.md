# ASL Unit Backlog Passes Plan

**Status:** Approved by the user on 2026-09-27 as the plan to follow. Each session starts from the kickoff prompt the user gives it, which says how far it is authorized to go.

**Date:** 2026-09-27

**Scope:** every open row of the [ASL Unit Backlog](<ASL Unit Backlog.md>) (138 rows after the 2026-09-27 housekeeping), in twelve passes, 5 to 16, numbered after the four of the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>). Backlog section 13 (the Studio demo findings) is all fixed and not planned.

## 1. How a pass is run

1. **Branch.** One branch per pass, `feature/asl-backlog-pass-<n>`, from main.
2. **Reading and rulings.** Read the rules of the pass in the registered PDF (the whole rulebook is in scope; page citations are provenance only). Propose rulings as R<n>.<k> in section 5 of this plan.
3. **Review stage.** Transcribe any chart and catalog rows, compare the rule text with the PDF, and revise the packages with their prior manifest digests kept. A separate agent, briefed as a skeptical ASL rules referee, reviews the stage. Fix every defect it finds with a test, or record it as a deviation.
4. **Live stage.** Build the planner, projector, records, and Play page changes, with Units, Play, and Studio tests. A separate agent, briefed as an experienced table player, reviews the stage. Fix its findings the same way.
5. **Visual check.** Run the Studio (`map-studio-scripted`) and play the new behavior. Stop the Studio before building or running tests, because it locks the DLLs.
6. **Documents.** A review document and a design document for the pass; the rulings here; the requirements' done notes; the backlog: every row the pass builds leaves it with the pass named, and everything left out gets a row.
7. **Full local suite before the first commit.** Run the whole solution and the ScenarioA1 tests, not only the projects touched. The Authoring, Units, and Rendering tests pin catalog and matrix digests, source-review subjects, and state goldens.
8. **Merge gate.** Commit, then run the full local suite and the Docker Linux check (the script in appendix A, run as `bash docker_check.sh <branch>` from the session's scratchpad directory; it clones the committed branch, so commit first). Merge with `git merge --no-ff` into main and push. Report the time breakdown.

**Standing rules:**
- Stage explicit paths only. Never `git add src/ASL`, `git add -A`, or anything under `src/ASL/boards/` (untracked live Studio data) or `.claude/`, and never commit images.
- Do not modify the user's live games `steps-demo` and `steps-demo-2`.
- Time every sub-task in the [ASL Unit Time Log](<ASL Unit Time Log.md>) with `date "+%Y-%m-%d %H:%M"`.
- Build with `DOTNET_CLI_USE_MSBUILD_SERVER=0`, `MSBUILDDISABLENODEREUSE=1`, `--disable-build-servers -m:1`; IDE0055 is an error, fixed with `dotnet format whitespace <csproj> --include <files>`.
- Writing: no em-dashes, no emoticons, plain language.
- Stop and ask the user only for a rule question that changes a pass's scope, a failure that needs a design change, or anything beyond this plan.

## 2. How the estimates are made

The estimates come from the actual times of 2026-09-27 in the time log, not from page counts. The earlier plan's estimates ran two to four times the actuals:

| Work | Plan estimate | Actual |
|---|---|---|
| Pass 2: Close Combat, Berserk, Surrender (27 rulings) | 4 to 6 h | 2:33 |
| Pass 3: Ordnance, a Gun firing HE (a new package and chart) | 5 to 8 h | 1:38 |
| Pass 4: Vehicles (catalog, package revision, movement, page) | 3 to 4 h | 3:36 (build 2:18, merge gate 1:15) |
| Nine Studio page fixes | 1 to 1.5 h | 0:59 |

Every pass carries the same overhead; the two second-pass reviews run in the background alongside the build:

| Overhead per pass | Minutes |
|---|---|
| Reading, and rulings proposed for the referee | 15 |
| Documents: rulings, review, design, requirements, backlog | 10 |
| Full local suite before the first commit | 15 |
| Merge gate: full local suite and Docker in parallel, merge, push | 35 |
| **Total** | **75** |

The estimates are point values. Pass 4, the one pass estimated from actuals, came in inside its estimate; the report in section 4 allows 30 percent either side.

## 3. The passes

### Pass 5: Deviations and small items

**Purpose:** Remove the simplified resolutions the game records (backlog section 1) and fix the small data and page rows. **After:** Nothing; first. **From:** Backlog section 1; the small rows of sections 10, 11, and 14.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 5.1 CX status | Infantry may use Double Time for extra MF and become CX, with its penalties on fire and CC. A Melee withdrawal or an advance that should make a unit CX places the counter. | A4.5, A4.72, A11.21 | 0:30 |
| 5.2 The captor's choice at a surrender | The capturing side may refuse a surrender (No Quarter) or massacre the prisoners; today every surrender is accepted. | A20.3, A20.4 | 0:25 |
| 5.3 Owner options during resolution | One pending-choice mechanism offers three choices the game makes for the player today: declining the Leader Creation dr, refusing Battle Hardening, and the firer's Unlikely Kill dr after a result that already harmed the vehicle. | A18.11, A15.3, A7.309 | 0:30 |
| 5.4 A second Heat of Battle DR in one attack | When one attack gives a unit two Heat of Battle chances (its MC, then a LLMC), the second is taken, with a roll key per check. | A15.1 | 0:10 |
| 5.5 A hero created in the MPh moves on | A hero created mid-move may continue with his creator; today he moves no further that phase. | A15.21 | 0:15 |
| 5.6 A half-squad keeps its squad's SW | A squad Casualty Reduced to a HS keeps its SW when the HS can carry it. | A7.302, A4.42 | 0:15 |
| 5.7 Acquisition follows the target unit | A Gun's Acquisition stays on the unit it fired at, following it within LOS; today it stays on the Location. | C6.5, C6.51 | 0:20 |
| 5.8 Vehicle Motion and unspent MP | The player names the hex the vehicle wished to enter next, so it may end in Motion as D2.4 allows. MP left at the end of its move count as spent in its final hex, with the DEFENDER's further fire. | D2.4, D2.1, A8.14 | 0:25 |
| 5.9 Recall as its own status | A Recalled vehicle leaves by its friendly board edge and is recorded as exited, not eliminated; an immobilized Recalled AFV is Abandoned by its crew. | D5.341, D5.5 | 0:20 |
| 5.10 Small data and page fixes | Grain costs Infantry the Open Ground MF outside its season. A truck's wreck name shows its passenger-only cs#. The page shows "Stun +1", hides a BU toggle already used that phase, and shows counters by printed values ("7-0", "4-6-7"). In CC each side declares only its own SMC stacking and attacks, and only the captor's side chooses the Guard; the CC record lists SW left unpossessed and a berserk leader's companions' checks. | B15.6, D5.6, D5.33, A11.14, A20.5 | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 5 total** (build 3:40) | | **4:55** |

### Pass 6: Vehicles, part 2

**Purpose:** Finish the vehicle rules pass 4 deferred, apart from tanks and vehicle movement. **After:** Pass 5. **From:** Backlog section 14; the wreck deviation of section 1.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 6.1 AFV cover for Infantry | Infantry with their AFV get its +1 TEM, and an AFV's hex is a +1 LOS Hindrance, with FFMO cancelled; the LOS read reports the hexes it crosses. Replaces the refusal that makes such Infantry impossible to fire on. | D9.3, D9.4, A4.6, D2.41 | 0:50 |
| 6.2 Residual FP against vehicles | Trucks and Vulnerable crews are attacked by Residual FP instead of being barred from it. | A8.2, A8.222, A7.308, D.8B | 0:30 |
| 6.3 Wrecks | A destroyed vehicle leaves a wreck or burning wreck that hinders LOS and movement. | D10, A7.308 | 0:40 |
| 6.4 Vehicle concealment | Vehicles may set up concealed or hidden; a vehicle may enter a Location holding enemy units it cannot see. | A12.2, A12.15 | 0:35 |
| 6.5 Vehicle MG fire during movement | The halftrack's MG fires as Defensive or Bounding First Fire; BMG and CMG; vehicle MG repair. | D3.3, D3.7 | 0:45 |
| | Overhead | | 1:15 |
| | **Pass 6 total** (build 3:20) | | **4:35** |

### Pass 7: Armor

**Purpose:** Closed-topped tanks, their main armament, and the Vehicle Target Type. **After:** Pass 6. **From:** Backlog sections 12 and 14.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 7.1 Tank counters | A German and a Russian closed-topped tank (for example a PzKpfw III and a T-34) in the catalog, reviewed row by row. | Chapter H | 0:40 |
| 7.2 Vehicle Target Type | Guns and tanks fire at vehicles, with Target Facing and the moving-target Cases. | C3.33, C5, C6 | 0:40 |
| 7.3 To Kill | AP, HEAT, and APCR against armor: hit location, armor factor, the To Kill tables transcribed, and the results. | C7, C8, D1.6 | 1:30 |
| 7.4 Main armament fire | A tank fires its MA with turret and hull Covered Arcs and turret turning; closed-topped crews button up. | D1.3, D3.1, D5 | 0:50 |
| 7.5 Crew survival | A knocked-out AFV's crew may survive and bail out. | D5.6, D5.7 | 0:20 |
| | Overhead | | 1:15 |
| | **Pass 7 total** (build 4:00) | | **5:15** |

### Pass 8: Guns, part 2

**Purpose:** The rest of the Gun rules pass 3 deferred. **After:** Pass 7. **From:** Backlog section 12.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 8.1 Guns in the movement windows | Defensive First Fire by Guns, Cases J1 to J4, Gun Duels, and a kept ROF in Final Fire. | C2.2401, C2.241, C6.1 to C6.17 | 0:40 |
| 8.2 Intensive Fire | A Gun may fire once more at a breakdown risk; OVR Prevention. | C5.6 to C5.641, C2.5 | 0:15 |
| 8.3 Guns as targets | Guns and crews can be attacked and destroyed; CC against a Gun crew. | C11, C11.6 chart, A11 | 0:40 |
| 8.4 Crews' own fire | A crew fires its inherent FP and loses it after firing its Gun. | A7.352 | 0:20 |
| 8.5 Concealed Guns | Guns and crews may be concealed and lose it when they fire. | A12.14, C6.57 | 0:20 |
| 8.6 Gun movement | Manhandling, limbering, towing behind the trucks, and abandoning a Gun; a crew leaving its Gun. | C10, C2.8, A4.41 | 0:50 |
| 8.7 Levels and boards | Targets at another level within C2.6 limits; Covered Arcs across boards and on reversed boards. | C2.6, C3.2 | 0:30 |
| 8.8 Special shots | Multiple Hits, fire within the Gun's hex, captured and non-qualified use, Bore Sighting, and choosing the turn direction. | C3.8, C5.5, C5.8, C6.4, C3.21 | 0:30 |
| 8.9 Covered Arc tools | Turning a Gun without firing; the page draws the Covered Arc and marks which targets are in it, in range, or refused. | C3.22, C3.2 | 0:30 |
| 8.10 Stacking and catalog details | Overstacked Locations for ordnance; a Gun's BPV, dates, and Animal-Pack in the catalog. Gun repair stays blocked: no registered source prints a Gun's malfunctioned side. | A5.12, A5.131, Chapter H key | 0:20 |
| | Overhead | | 1:15 |
| | **Pass 8 total** (build 4:55) | | **6:10** |

### Pass 9: Mortars, SMOKE, and anti-tank weapons

**Purpose:** Light mortars, the Area Target Type, SMOKE and WP, and LATW. **After:** Pass 8. **From:** Backlog sections 6, 7, and 12.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 9.1 Counters | A light mortar of each side, the Panzerschreck, the Panzerfaust, and the Russian ATR. | Chapter H | 0:40 |
| 9.2 Area fire | Mortars fire on the Area Target Type, with spotting. | C3.31, C9 | 0:50 |
| 9.3 SMOKE and WP | Placing SMOKE, its Hindrance and dispersal; WP's morale checks. | A24 | 1:00 |
| 9.4 Anti-tank weapons | LATW To Hit, Backblast, and PF checks. | C13 | 1:00 |
| | Overhead | | 1:15 |
| | **Pass 9 total** (build 3:30) | | **4:45** |

### Pass 10: Movement and terrain

**Purpose:** Infantry movement over the whole board, levels, and hexside terrain. **After:** Pass 5. **From:** Backlog sections 3, 4, and 9; the berserk-route deviation of section 1.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 10.1 More terrain | Walls, hedges, rubble, marsh, and the rest of board 01's terrain, transcribed and reviewed for movement and fire. | B chapter, Terrain Chart | 1:00 |
| 10.2 Levels | Stairs, upper building levels, hills, and fire across levels with its Hindrance. | A4.13, B10, A6 | 0:50 |
| 10.3 Hexside terrain | Walls and hedges on entry and as cover at the target. | B9 | 0:40 |
| 10.4 Bypass | Moving along a building's or woods' hexsides. | A4.3 | 0:40 |
| 10.5 Movement details | Hazardous Movement, the Road Bonus, a leader's MF bonus (with portage in the A4.72 reading), and Minimum Move. | A4.62, A4.132, A4.12, A4.134, A4.72 | 0:30 |
| 10.6 Concealed movement | Concealed units move and may keep or lose their "?". | A12.13, A12.14 | 0:35 |
| 10.7 Entering enemy Locations | Moving into an enemy-held non-building Location, TPBF, and Snap Shots. | A4.14, A7.212, A8.15 | 0:45 |
| 10.8 One move action | The building entry of steps 7 to 11 folds into the move action. |  | 0:30 |
| 10.9 Berserk routes | Every berserk charge route is decided, removing its deviation; a charge onto a lone SMC is an Infantry OVR. | A15.431, A15.432 | 0:25 |
| | Overhead | | 1:15 |
| | **Pass 10 total** (build 5:55) | | **7:10** |

### Pass 11: Vehicle movement and OVR

**Purpose:** Vehicle movement over all terrain, and overruns and Close Combat against vehicles. **After:** Passes 6 and 10. **From:** Backlog sections 11 and 14.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 11.1 Vehicle movement options | Reverse movement, VBM, ESB, Minimum Move, and vehicle stacking. | D2.2, D2.3, D2.5, D2.14, D2.15 | 0:50 |
| 11.2 Vehicle terrain | Vehicles cross the terrain of pass 10, change levels, and can bog. | D2, D8 | 0:50 |
| 11.3 OVR and CC against vehicles | A vehicle overruns Infantry; Infantry attack vehicles in CC and may enter or advance into an enemy vehicle's Location; sequential CC. | D7, A11.5, A11.6 | 1:30 |
| | Overhead | | 1:15 |
| | **Pass 11 total** (build 3:10) | | **4:25** |

### Pass 12: Fire extensions

**Purpose:** The fire rules left out of steps 17 to 23 and passes 1 and 2. **After:** Pass 5. **From:** Backlog sections 3, 8, and 11.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 12.1 Opportunity Fire | Holding fire from the PFPh to the AFPh, with the Bounding Fire marker. | A7.25 | 0:30 |
| 12.2 Blocked LOS | Fire declared at a Location out of LOS still counts as the firer having fired. | A6.11 | 0:10 |
| 12.3 FPF variants | FPF directed by a leader, mixed FPF groups, and one attack on a stack mixing pinned and unpinned units. | A8.31 | 0:30 |
| 12.4 Split fire | A squad fires its inherent FP apart from its MG; SMC fire; a leader firing a MG. | A7.35, A7.53 | 0:30 |
| 12.5 Concealment Table | Its full use against concealed units. | A12.12 | 0:25 |
| 12.6 MG techniques | Spraying Fire and Fire Lanes. | A9.5, A9.22 | 1:00 |
| 12.7 Fire and Melee | Fire into a Melee Location and by units in Melee, fire at prisoners and by a Guard, and fire by berserk units. | A11.15, A20.52, A15.432 | 0:40 |
| 12.8 Encirclement | Its effects on fire, morale, and rally. | A7.7 | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 12 total** (build 4:15) | | **5:30** |

### Pass 13: Rally, Rout, and support weapons

**Purpose:** Desperation Morale, the RtPh, deploying, and SW handling. **After:** Pass 5. **From:** Backlog sections 2, 5, and 8.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 13.1 Desperation Morale | DM from an ADJACENT armed enemy, at the start of the RtPh, and its retention. | A10.62, A10.63 | 0:30 |
| 13.2 Rally terrain | Rallying outside woods and buildings. | A10.61 | 0:20 |
| 13.3 The Rout Phase | Broken units rout, and may surrender while routing. | A10.5, A20.21 | 1:00 |
| 13.4 Deploy and recombine | Squads split into half-squads and recombine. | A1.31, A1.32 | 0:25 |
| 13.5 SW handling | Transfer, possession changes, recovery, dismantling and assembly, and captured SW. | A4.43, A9.8, A21 | 1:00 |
| 13.6 Self-Rally | Self-Rally capability for the catalog counters. | A10.63 | 0:10 |
| | Overhead | | 1:15 |
| | **Pass 13 total** (build 3:25) | | **4:40** |

### Pass 14: Close Combat and capture, part 2

**Purpose:** The rest of the CC and capture rules pass 2 deferred. **After:** Pass 5. **From:** Backlog section 11.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 14.1 Hand-to-Hand | Hand-to-Hand CC with its red Kill Numbers. | J2.31 | 0:30 |
| 14.2 Hidden units in CC | Concealed and hidden units and Dummies in CC, advances into concealed units, and TI units. | A11.19, A12 | 0:40 |
| 14.3 Prisoners | Capture attempts, prisoner escape and recapture, and moving, transferring, or freeing prisoners. | A20 | 0:55 |
| 14.4 Slipping away | Infiltration on an Original 2 or 12, and Ambush Withdrawal. | A11.22, A11.4 | 0:25 |
| 14.5 Edge cases | Overstacked CC and advances, Unarmed units and a Guard without capacity, and a Disrupted unit's surrender. | A5, A20.5, A19.12 | 0:40 |
| 14.6 Refusals removed | Field Promotion edge cases, odds just above 10 to 1, the berserk Ambush drm, a Guard advancing into CC, and mandatory CC the package refuses today. | A18.2, A11.11, A11.4, A20.55, A11.15 | 0:40 |
| | Overhead | | 1:15 |
| | **Pass 14 total** (build 3:50) | | **5:05** |

### Pass 15: Special units and nationalities

**Purpose:** Assault weapons, Snipers, Commissars, morale specials, and other nationalities. **After:** Pass 5. **From:** Backlog sections 5, 6, 10, and 11.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 15.1 Assault weapons | Flamethrowers, Demolition Charges, and Molotov cocktails. | A22, A23, A25.23 | 1:40 |
| 15.2 Snipers | Each side's Sniper activates on its SAN. | A14 | 1:00 |
| 15.3 Leadership specials | Commissars, NKVD Field Promotion, and Allied Troops. | A25.22, A25.25, A25.5 | 0:40 |
| 15.4 Morale specials | Underscored morale, Green MMC, and heroes using SW or created concealed. | A19.13, A19.3, A15.23, A15.21 | 0:45 |
| 15.5 Other nationalities | Counters of other nationalities and their Heat of Battle and Leader Creation exceptions. | A25, A15.1, A18.2 | 1:00 |
| 15.6 Berserk SW choice | A berserk unit chooses which 1PP SW to abandon beyond its IPC. | A15.431 | 0:10 |
| | Overhead | | 1:15 |
| | **Pass 15 total** (build 5:15) | | **6:30** |

### Pass 16: Night, weather, and scenario cards

**Purpose:** Night, weather, and scenario cards as a registered source. **After:** All other passes; it touches every area. **From:** Backlog sections 2, 6, and 11.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 16.1 Night | Visibility (NVR), illumination, Night Ambush, and night DM and rally. | E1 | 1:30 |
| 16.2 Weather | Weather effects and Extreme Winter Fate. | E3 | 1:00 |
| 16.3 Scenario cards | Scenario cards as a registered source, so ELR and setup come from the card. | A19.1 | 0:40 |
| | Overhead | | 1:15 |
| | **Pass 16 total** (build 3:10) | | **4:25** |

## 4. Duration report

| Pass | Title | Tasks | Build | Total | Range (-30 % to +30 %) |
|---|---|---|---|---|---|
| 5 | Deviations and small items | 10 | 3:40 | 4:55 | 3:26 to 6:24 |
| 6 | Vehicles, part 2 | 5 | 3:20 | 4:35 | 3:12 to 5:58 |
| 7 | Armor | 5 | 4:00 | 5:15 | 3:40 to 6:50 |
| 8 | Guns, part 2 | 10 | 4:55 | 6:10 | 4:19 to 8:01 |
| 9 | Mortars, SMOKE, and anti-tank weapons | 4 | 3:30 | 4:45 | 3:20 to 6:10 |
| 10 | Movement and terrain | 9 | 5:55 | 7:10 | 5:01 to 9:19 |
| 11 | Vehicle movement and OVR | 3 | 3:10 | 4:25 | 3:06 to 5:44 |
| 12 | Fire extensions | 8 | 4:15 | 5:30 | 3:51 to 7:09 |
| 13 | Rally, Rout, and support weapons | 6 | 3:25 | 4:40 | 3:16 to 6:04 |
| 14 | Close Combat and capture, part 2 | 6 | 3:50 | 5:05 | 3:34 to 6:36 |
| 15 | Special units and nationalities | 6 | 5:15 | 6:30 | 4:33 to 8:27 |
| 16 | Night, weather, and scenario cards | 3 | 3:10 | 4:25 | 3:06 to 5:44 |
| | **All passes** | **75** | **48:25** | **63:25** | **44:24 to 82:26** |

The whole plan is about 63:25 of working time, 8.5 working days of 7.5 hours.

**Order.** Pass 5 first. Passes 6 and 7 build on pass 4's vehicles; pass 8 follows 7 (the Vehicle Target Type) and pass 9 follows 8. Pass 10 precedes pass 11. Passes 12 to 15 may be taken in any order after pass 5; pass 16 is last. The default order is 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16.

**Blocked.** Gun repair and removal (backlog section 12) needs a Gun's malfunctioned side, which no registered source prints. It stays in the backlog until one is registered.

## 5. Rulings

Each pass adds its rulings here, R5.1 onward for pass 5, subject to the referee's review.

| Ruling | Question | Ruling |
|---|---|---|
| R5.1 | How is Double Time declared, and what does it give? | The move action declares it for its movers (A4.5): on a unit's first step it adds two MF, after MF were spent one; a CX counter is placed with the step. A broken, wounded, berserk, or already CX unit, or one whose CX counter was removed at the start of this MPh, may not Double Time. The allotment is at most eight MF (seven for Conscripts). |
| R5.2 | Which CX effects are built? | A4.51: +1 to the IFT DR of an attack a CX unit makes or directs (once per attack, however many CX units take part), +1 to the To Hit DR of a Gun its CX crew fires, +1 to a CC attack a CX unit makes (once per attack), -1 to a CC attack against a CX unit (for that defender, as the broken DRM is), +1 to its side's Ambush dr (once, as broken and pinned are), and no advance into Difficult Terrain (A4.72). Search, Recovery, and availability drs are not built. |
| R5.3 | When does a CX counter leave? | A4.51: when the unit breaks or goes berserk (A15.42), and at the start of its side's next MPh; a unit whose counter left then may not Double Time in that MPh. Its Prep Fire keeps the +1, since the counter leaves only once its Prep Fire is complete. Opportunity Fire, TI, and Minimum Move are not built. |
| R5.4 | Does portage change the MF allotment? | Yes, for every Infantry unit (A4.42, A4.52): one MF less for each PP carried beyond its IPC (three for a MMC, one for a SMC; one less while CX). It was not built before this pass. A leader's IPC added to another unit is pass 10. |
| R5.5 | When do an advance and a withdrawal make a unit CX? | A4.72: an advance into a Location whose MF cost is at least four MF or all of the unit's non-Double Time allotment after portage, whichever is less, makes it CX, and an already CX unit may not make it; a Good Order leader advancing with a unit that carries more than its IPC would add two MF and one IPC (A4.72 EX, A4.12), which is pass 10, so that advance is refused. A11.21: a withdrawal from Melee follows the same test, so an already CX unit may not withdraw where the advance would be barred, and a withdrawing unit carries no more than its IPC: it drops the SW beyond it, as its owner declares (A4.43). |
| R5.6 | May the captor refuse a surrender? | A20.3: the captor's side chooses a Guard or rejects the surrender (No Quarter), which eliminates the unit. From then on the rejected side is faced with No Quarter: its units treat a Heat of Battle Surrender as Berserk and are not Disrupted by it (A15.5 EXC; the Heat of Battle table note). The RtPh surrender it also prevents is pass 13. |
| R5.7 | How is Massacre played? | A20.4: in its own fire phase, a Russian or berserk Infantry unit not in Melee may declare a prisoner in its Location as its target, which eliminates it, once per phase, "as if using a SW": a MMC keeps its inherent FP (A7.351), and a SMC forfeits its own (A7.352) and is marked as having fired; a MMC that massacres in the PFPh has Prep Fired and does not move in the MPh (A3.3). A berserk unit in a Location with enemy prisoners eliminates them at the start of its side's AFPh or DFPh (it never fires in the PFPh, A15.432) and returns to normal. A Massacre raises the victim side's ELR by one, once, to at most 6, and faces that side with No Quarter. SS, Japanese, and Partisan units are not in the catalog. |
| R5.8 | How are the owner's options offered? | One pending choice: a resolution stops before an optional step, keeps the rolls drawn so far, and names the side that chooses; nothing else may happen until it chooses, and the resolution then continues with the choice as a declared fact, which replay checks. The choices are the Leader Creation dr after a Self-Rally Original 2 (A18.11, the rallying side), refusing Battle Hardening (A15.3, the unit's owner), and the Unlikely Kill dr (A7.309, the firer) whenever an Original 2 on the Vehicle line allows it, whatever the result it improves on. |
| R5.9 | What does a refused Battle Hardening leave? | A15.3: the unit keeps its counter and is not made Fanatic; the rest of the Heat of Battle result stands (a hero created, a leader made heroic), and a broken unit stays broken unless the Original 2 of a Rally rallied it. |
| R5.10 | When does one attack give a unit a second Heat of Battle DR? | A15.1: after each Original MC DR of 2 (its MC, then its LLMC), with the roll keys `<unit>` and `<unit>:2`, while the unit is still subject: not eliminated, not berserk, not heroic, and not surrendered to a captor (a Surrender result with no captor only Disrupts it, A15.5). The second applies to the unit as the first left it. |
| R5.11 | What movement status does a hero created in the MPh have? | A15.21: his creator's. A hero created by a member of the moving stack joins its members with the creator's MF spent and may move on with it; one whose creator has ended its move has ended his. |
| R5.12 | Does a HS keep the SW of the squad Reduced to it? | Yes (A7.302, A4.43, A4.431): the HS replaces the squad in its Location and possesses its SW, as a sub-unit created from a MMC may; possession has no limit, and IPC limits only movement (R5.4). |
| R5.13 | What does an Acquisition follow? | C6.5, C6.51: the Known target units of the shot. The counter moves with them while they stay in the Gun's LOS; when they enter a Location out of its LOS it stays in the last Location in LOS with no unit. When the acquired units are in more than one Location as one of them finishes its MPh, APh, RtPh, or CCPh Withdrawal, the Gun's side chooses which Location keeps it (a pending choice); until then a shot at any of their Locations uses it. It is lost when the crew is no longer Good Order or no longer mans the Gun. |
| R5.14 | How does a vehicle end its MPh in Motion? | D2.4: the ATTACKER names the ADJACENT hex it wished to enter next; it ends in Motion when its MP left are fewer than that hex's entry cost plus one MP for each hexspine its VCA must turn (a reading: D2.4 says only "insufficient MP remaining to enter the next hex"), or when it has fewer than one MP left. |
| R5.15 | What happens to MP left at the end of a vehicle's move? | D2.1: they are spent in its final hex as one expenditure, which opens the DEFENDER's window (A8.1, A8.14); the move ends when the DEFENDER passes. |
| R5.16 | Where is a side's Friendly Board Edge? | A20.53: the edge the side entered from, or, for a side set up on board with no reinforcements, any edge it set up in front of with no enemy between. The game cannot read either without the scenario (pass 16), so the players name one edge at the start: the map's top, bottom, left, or right. |
| R5.17 | How does a Recalled AFV leave? | D5.341: at the end of the Player Turn of the Recall the counter shows Recall; +1, and in each of its owner's MPh the AFV must move by a shortest route in MP to the Friendly Board Edge without stopping, and exits by entering the mirror image of its edge hex (A2.6). It is recorded as exited, not eliminated. With no edge named, or a route the terrain review cannot decide, it may end its move in place, as the move's reason records. |
| R5.18 | When is an immobilized Recalled AFV Abandoned? | D5.341, D5.41: at the end of the Player Turn of its Recall, or when it is immobilized while Recalled: it is marked Abandoned and its crew is placed beneath it as a crew counter of its nationality, carrying the Stun +1 (D5.34). The crew need not leave and may not re-crew it. |
| R5.19 | Grain out of season? | B15.6: for Infantry, grain costs 1½ MF from April to September and is Open Ground otherwise; for fire it is Open Ground outside June to September, so FFMO applies there. A game with no scenario month is refused for grain, as for vehicles. |
| R5.20 | How does the hot-seat Play page keep each side to its own CC declarations? | The page names the side it is played as; the CC panel offers SMC stacking and attacks only for that side's units, and the Guard choice only to the captor's side. The adjudicator sees and sets everything. |
| R6.1 | When does an AFV or a wreck give Infantry +1 TEM? | D9.3, D10.3: Infantry in a Location with a non-burning wreck of either side, a friendly AFV, or an abandoned enemy AFV take +1 TEM when their terrain gives no positive TEM, cumulative with any Hindrance, and no FFMO (A4.6). Not against an attack from within that Location; not from an AFV in Motion (D2.41); and, in the MPh, DFPh, and AFPh, not from an AFV or wreck that entered a new hex or was in Motion in this Player Turn's MPh (the Case J clause, C6.1). An unarmored vehicle gives none until it is a wreck. It applies to IFT attacks, Residual FP, and a Gun's To Hit DR on the Infantry Target Type, and a Critical Hit reverses it with the terrain's TEM (C3.71). The "no other positive TEM" test reads the terrain's TEM, since no hexside TEM is admitted. Armored Assault (D9.31), entrenched and Dug-In AFVs, Depressions, and the Case J exception for units Abandoning, Bailing Out, or unloading are not built. |
| R6.2 | When does an AFV or a wreck hinder LOS? | D9.4, D10.3, A6.7: +1 for a same-level LOS traced through (not into or out of) a hex holding an AFV or a non-burning wreck of either side, when the firer and the target each have LOS to that hex, and not from an AFV in Motion or under the Case J clause of R6.1. The LOS read reports the hexes it crosses, both hexes where it runs along their shared hexside, with the range of each; at each range only the highest Hindrance counts, so a map Hindrance in another hex at that range is the higher, but an AFV or wreck in a brush or in-season grain hex adds its +1 to that hex's (A6.7 and its example). "Same level" compares the absolute level of the vehicle's hex with the firer's. A concealed AFV's Hindrance is real, since a "?" vehicle is never a Dummy here; Bypass is not built. |
| R6.3 | What does a burning wreck do? | B25.14, B25.2, B25.141, A24.2, A24.8: a Blaze is placed on the wreck when the Vehicle line gives a Final DR at most half its Kill Number or an Unlikely Kill dr of 1 after an Original 2 (the other B25.14 causes are not built). It gives no TEM or wreck Hindrance; its smoke is a +2 Hindrance, in addition to any other Hindrance, for fire traced into, through, within, or out of its Location, one more for fire traced out of or within it, at any level up to four above, counted once per Location; Residual FP in its Location takes the +2 (A8.2). Entering its Location costs one more MF or MP. Spreading Fire and terrain Blazes (B25.1 to B25.6) are not built. |
| R6.4 | What does a wreck do to movement? | D2.14, D10.2: a vehicle pays one more MP per wreck or vehicle to enter a hex, two when it enters by a road hexside at the road rate; Infantry pay nothing more except for a burning wreck (R6.3). A wreck does not count for vehicle stacking. Pushing (D10.42) and Scrounging (D10.5) are not built. |
| R6.5 | How is a wreck recorded? | D10.1: an eliminated or burning vehicle becomes a wreck in its Location: its instance takes the status `wrecked` and the state `asl:wrecked` (the counter's wreck face), keeps its VCA, is seen by both sides (a concealed vehicle's "?" goes with it), and no longer acts as a unit. Every catalog vehicle has a wreck depiction on its reverse, so every one leaves a wreck. A burning wreck carries a Blaze. A Recalled AFV that is Abandoned is not a wreck; it stays an abandoned AFV (R6.1). |
| R6.6 | How does Residual FP attack a vehicle? | A8.2, A8.222, A7.308, D.8B: a vehicle that enters or spends MP in a Residual FP Location is attacked by it as Infantry are: an unarmored vehicle on the Vehicle line of the Residual FP column, and an AFV's Vulnerable crew Collaterally, with the CE DRM; an AFV itself is unaffected. Residual FP has no LOS Hindrance and no Cowering (A8.2, A8.224), but the SMOKE of its Location applies, and cancels FFMO (A24.2). |
| R6.7 | May a vehicle set up concealed or hidden, and when does it lose "?"? | A12.2, A12.12, A12.3: a vehicle may set up concealed or hidden only in Concealment Terrain, which for the reviewed vehicle terrain is grain in season (B15.6); the A12.2 road clause (a grain-road hex is Open Ground to an LOS along the road) is not built. It loses "?" when it enters a hex or changes its VCA within 16 hexes and in the LOS of a Good Order enemy ground unit, or moves there under a Motion counter; when it is in the LOS of one while not in Concealment Terrain (Case H: checked after each MF or MP expenditure of either side; other changes that open an LOS, such as a Rally, are not checked); when it fires; and, attacked by fire in the LOS of one, when its crew takes a check or its Vehicle line gives a result. Fire at a concealed vehicle is Area Fire (A12.13). |
| R6.8 | What happens when a vehicle enters a Location holding enemy units it cannot see? | A12.4, A12.41: the entry stands. The concealed or hidden enemy Personnel there, other than those exempt from a PAATC, must either be revealed or take a combined PAATC; the PAATC is pass 11's, so they are revealed, a deviation that takes the owner's PAATC option away (backlog section 1), and a Dummy is removed. Every catalog vehicle is unbroken and no Bypass or woods-road entry is built, so A12.41 always applies. OVR stays refused. |
| R6.9 | May a vehicle fire in the MPh? | D3.3, D3.31, D2.42, A8.1: an AFV's AAMG may fire as Defensive First Fire at a moving unit in its LOS, with the usual MG rules (R25.7), and a phasing vehicle may fire it as Bounding First Fire during its own MPh, at its outset before any MP or after any MP expenditure the DEFENDER has passed on, at half FP (D3.31), halved again while Non-Stopped (D2.42); Bounding Fire marks it unless it kept a Multiple ROF, and a vehicle with Prep Fire or Bounding Fire may not fire in the AFPh. A vehicle fires once per Player Turn unless it keeps a Multiple ROF: Final Fire after First Fire (A8.4) is not built. Opportunity Fire is not allowed for vehicles. Passengers' and Riders' fire (D6.64) waits for Passengers. |
| R6.10 | How is a vehicle MG repaired? | D3.7: once per RPh, in either side's RPh, a malfunctioned AAMG may be repaired by its CE crew that is not Stunned or Recalled (a Recall is read as a Stun, D5.341; Shock is not built; a Hero Rider waits for Riders): dr 1 repairs it, 6 disables it for good, 2 to 5 do nothing. A vehicle whose MA and Secondary Armament are all disabled is Recalled unless it can carry Passengers or tow; the SPW 251/1 can. No catalog vehicle has a BMG or CMG, so they wait for pass 7's armor. |
| R7.1 | Which tanks? | The German PzKpfw IIIH (German Vehicle Listing, p. 337) and the Russian T-34 M41 (Russian Vehicle Listing, p. 355), in catalog 1.6.0, read row by row with the Listings Key (p. 338). This edition prints the counters' red values in bold (the Key's red FT values are bold), so the T-34's bold MP 17 is red: mechanically unreliable (D2.51). Their APCR Depletion Numbers by year come from the listing and the C8.12 Ammunition Supply Chart (p. 178). The T-34 is radioless (®); D14 is not built. |
| R7.2 | When is the Vehicle Target Type used? | C3.31, C.9: a Gun or a tank's MA firing at one named vehicle uses it, and must at an AFV. A hit affects only that vehicle. The Basic TH# is the C3 To Hit Table's Vehicle row (p. 700), black or red as for the Infantry Target Type, with the C4 modifications of the Gun and of APCR. |
| R7.3 | Which To Hit DRM are built for a vehicle target? | C5, C6: the Infantry Target Type's built Cases (A, B, D, CX, L, N, Q, R), Case I (+1 for a BU AFV firing its MA; an RST or 1MT MA fires only BU, D1.321, D1.322), Case J (+2 against a moving target: one that entered a new hex or began its MPh in Motion this Player Turn, or is in Motion, C.8), Case K (+2 against a concealed vehicle), and Case P (the target's size: +1 Small, 0 Average). Case L does not apply against a Non-Stopped or Motion target. Cases C, E, G, J1, J2, and O, and the Bounding Fire of ordnance, are not built. |
| R7.4 | Where does a hit strike, and which armor does it meet? | C3.9, D3.2, D1.6: a hit strikes the turret when the colored dr of the Original TH DR is less than the white dr and the target is turreted; otherwise the hull. Target Facing is read from the bearing of the firer from the target against its VCA (hull) or TCA (turret): within 60 degrees of it front, beyond 120 degrees rear, otherwise side; along a hexspine the facing less favorable to the firer counts (front before side, side before rear). An improbable hit (C3.6) strikes the turret on a subsequent dr of 2 and the hull on 3 (referee finding). The AF is the printed front or side/rear AF of the hull, or of the turret raised or lowered one AF step where it is superior or inferior (D1.63, D1.64). |
| R7.5 | How is the To Kill number found? | C7.1 to C7.34 (the tables on p. 701): the Basic TK# of the ammunition's table for the Gun's caliber and length, taking the colored entries for the listed nationalities (the Russian 76L is 13 on the AP table, 14 on the APCR table); plus one against the rear Target Facing (Case A), doubled first on a Critical Hit (Case C), and the Case D range change of AP and APCR (where Case D prints NA the shot is refused as out of range); less the AF hit. Against an unarmored vehicle the Final TK# is the table's unarmored value (AP by caliber, APCR as AP, HEAT 11, HE by caliber), doubled on a Critical Hit. |
| R7.6 | Which ammunition may be fired? | C8.1, C8.9, C8.3: AP unless the Gun has no AP; HE unless it has no HE; APCR and HEAT only where the counter lists them for the scenario year, declared before the To Hit DR: an Original TH DR less than the Depletion Number uses it, equal to it uses it and runs out, and greater than it had none, so nothing was fired, the Gun may fire again, and it may not use that ammunition again (C8.9). The Elite forces' higher Depletion Numbers (C8.2) are not built. A game with no scenario month has no year, so APCR and HEAT are refused. |
| R7.7 | What does the To Kill DR do? | C7.35, C7.7, C7.4 to C7.6, D5.7: an Original 12 is a dud. Against an AFV by Direct Fire: a Final TK DR at most half the Final TK# burns it (a red CS#'s -1, D5.7, is not built: no catalog vehicle has one), less than the TK# eliminates it, equal to it immobilizes it on a hull hit and Shocks it on a turret hit, and one greater immobilizes (hull) or Shocks (turret) an HE hit and gives any other ammunition a possible Shock: the crew takes a NTC and is Shocked if it fails. Against an unarmored vehicle: at most half burns, less eliminates, equal immobilizes. |
| R7.8 | What does Shock do? | C7.42: the AFV may not move, fire, or change its CA, its CE crew buttons up, and at the end of the next RPh a dr of 1 or 2 removes the Shock and 3 to 6 turns it into an Unconfirmed Kill; at the end of the RPh after that a dr of 1 to 3 removes the UK and 4 to 6 wrecks the AFV with no Crew Survival. A second Shock returns a UK to Shock. An AFV under a UK counter is still Shocked: it does not move, fire, change its CA, expose its crew, or repair (table player finding). |
| R7.9 | What happens to the crew? | D5.5, D5.6: a vehicle immobilized by a non-CC attack has its crew take an immediate TC, an AFV's crew on Elite morale and an unarmored vehicle's on 1st Line morale (D5.1); failure Abandons it and places the crew beneath it. D5.5's second trigger (an Original 5 against an already immobilized vehicle) is not built. A Stunned or Shocked crew takes no Immobilization TC, nor does an Abandoned or already immobilized vehicle, and an Abandoned AFV has no crew to survive it (table player finding). An AFV eliminated but not burned rolls for Crew Survival: a Final DR at most its CS#, +1 if its crew was Stunned, Shocked, or Recalled or is under a "+1" counter (D5.34), places a crew beneath the wreck; the "+1" counter adds one to the NTC and TC as well. The crews are the catalog's crew counters of that side (the vehicle-crew counter is not in the catalog). |
| R7.10 | How does a tank fire its MA? | D1.3, D3.1, C5.1: in its side's PFPh, AFPh, or DFPh, like a Gun manned by its crew, at a vehicle (R7.2) or at Infantry on the Infantry Target Type with HE. The target must lie in its TCA, or the turret turns: Case A of +1 for each hexspine (T), or +2 for the first and +1 for each other (ST, RST); the TCA then points at the target and is kept on the vehicle. The TCA changes only when the MA fires outside it; setting it with MP or at the end of a fire phase (D3.12, C3.22) is not built, and a turret starts pointing along the VCA. Its MA malfunctions on an Original TH DR of 12 (B# 12). A Stunned, Shocked, Unconfirmed Kill, Recalled, or Abandoned AFV does not fire (the +1 fire of a Recalled AFV after its Stun, D5.341, is not built); an AFV in Motion (Case C4, D2.42, C5.35) or one that entered a new hex in its MPh and fires in the AFPh (Case C, C5.3) is refused, those Cases not built; an AFV under a "+1" counter adds one to its To Hit DR (D5.34); a tank's MA fire in the MPh is not built. |
| R7.11 | Are closed-topped crews exposed? | D5.2, D5.3: a CT AFV is BU unless it has a CE counter; its owner may place or remove it as for an OT AFV (D5.33). A CE CT crew is Vulnerable to Collateral Attacks as an OT one is (R25.6), but an RST MA does not fire while CE. Infantry small arms never harm a CT AFV itself (A7.307). |
| R7.12 | What else is out? | The BMG and CMG of the tanks (backlog: fire extensions), a tank's MA in CC and OVR, HD, Smoke, Intensive Fire, Deliberate Immobilization, Bounding First Fire of ordnance, radioless rules, and aerial and indirect fire are not built. |
| R8.1 | How does a Gun or a tank fire in the MPh? | C6.1 to C6.17, C2.24, A8.1: in the DEFENDER's window after a moving unit's or vehicle's MF or MP expenditure, a Gun manned by its non-phasing crew, or a non-phasing tank's MA, may Defensive First Fire at it, on the Infantry Target Type (every moving unit of the stack in the Location, HE) or the Vehicle Target Type. Case J2 (+4) applies when the vehicle has expended at most one MP in the firer's continuous LOS, Case J1 (+3) at most three, else Case J (+2); the MP are counted from the vehicle's steps this MPh since the last Location the firer could not see, each read with the game's LOS. Case J3 (-1) against Infantry using non-Assault Movement and Case J4 (-1) against moving Infantry in Open Ground replace the IFT's FFNAM and FFMO, which the hit's IFT attack does not add again, except on a Critical Hit (C3.71). No more shots at the same target in the same Location than the MF or MP it spent there, a minimum of one (C6.17); against a vehicle the MP an earlier shot there claimed count neither toward the next shot's Case J1 or J2 nor toward its limit (the C6.17 EX; referee finding). A change of the AFV's Target Facing does not restart the count (backlog). A Gun whose colored dr exceeds its ROF is marked First Fire. A vehicle that began its MPh out of the firer's LOS has spent all its MP since in it (C6.15; table player finding). Gun Duels need the ATTACKER's Bounding First Fire by ordnance, which is not built. A TI Gun, crew, or vehicle does not fire or move (A4.8). |
| R8.2 | What may a Gun do after First Fire, and in Final Fire? | C2.241, C5.6 to C5.63: a Gun marked First Fire may fire once more that Player Turn, as Intensive Fire, in the MPh or the DFPh. A Multiple ROF Gun not marked First Fire keeps its Multiple ROF in the DFPh. Intensive Fire: a non-vehicular Gun that has used its normal ROF (it carries a Prep or First Fire marker, never a Final Fire one, and its crew is not pinned; referee finding) fires once more in the PFPh, MPh, or DFPh, never the AFPh (C5.6 bars a crew marked Final Fire, so a normal DFPh shot is not followed by Intensive Fire; the table player read it otherwise, and the rule's text decides), with Case F (+2) and its B# lowered by two; it is then marked Intensive Fire and fires no more that Player Turn. No catalog Gun has "No IF"; C2.5's Intensive Fire counter needs a Gun without Multiple ROF, and OVR Prevention needs OVR (pass 11); a vehicle's Intensive Fire is not built. |
| R8.3 | How are Guns and their crews attacked? | C11.1 to C11.6: fire at a Location holding a manned AT or INF Gun attacks its crew, which takes the Gun's gunshield +2 IFT DRM against fire from within the Gun's CA instead of a lower positive TEM, never both, never against fire from its own hex, and never for a squad manning it or a crew moving or pushing under Defensive First Fire (referee finding). A Gun that set up manned and has not moved (nor been hooked up) is Emplaced: +2 TEM, not cumulative with other positive TEM; an ordnance shot on the Infantry Target Type adds its Target Size and the Emplacement TEM as the combined Case P and Q DRM (the Area Target Type option waits for pass 9). An HE hit whose IFT Final DR, before the gunshield, reaches a KIA destroys the Gun and eliminates its crew; a K result malfunctions the Gun and gives its crew Casualty Reduction; a Critical Hit destroys both. Infantry fire destroys a Gun only through Random SW Destruction, not built for Guns; AP and HEAT at a Gun (HE Equivalency, C8.31) and an unmanned Gun as a target are not built. A fire group with any firer within the CA faces the gunshield (C11.51; table player finding). |
| R8.4 | May a crew fire its own FP? | A7.352, A7.35: a crew fires its inherent FP like other Infantry, unless it fired its Gun this Player Turn (until CC or the Player Turn's end); a squad that fires a Gun keeps its inherent FP (A7.351). As a project ruling, a crew that fired its inherent FP does not fire its Gun that Player Turn. A7.353's halved FP against adjacent units is not built. |
| R8.5 | How do concealed Guns work? | A12.14, A12.141, C6.57: a Gun and its crew are concealed or hidden together, set up in Concealment Terrain. Firing the Gun, or changing its CA, loses both "?" if a Good Order enemy ground unit within 16 hexes has LOS to it, which the game reads; a concealed crew is a Case K target, acquired only if the shot costs it its "?". |
| R8.6 | How do Guns move? | C10.1 to C10.31, A4.41: a Good Order, unpinned crew or HS pushes its QSU Gun, never by Assault Movement, into an adjacent Open Ground or grain hex (a road hex by its terrain) in its MPh at double the hex's MF cost, with a Manhandling DR against the Gun's M#: +the hex's TEM and the MF cost, -2 across a road hexside; above the M# they stay, equal enters and stops, below enters. After each push the Gun and crew are TI, though the crew still pushing goes on; the Gun loses Emplacement and any Bore Sighting. A Stopped truck or halftrack whose T# is at most the Gun's M# hooks it up by spending half its MP (FRU) in its hex with the Gun's crew on foot there, and unhooks it the same way; a vehicle towing a Gun pays +1 MP per hex, the Gun cannot fire, and it is destroyed with the vehicle. The crew does not ride (Passengers are pass 11). A crew that moves away leaves its Gun unmanned (the page pushes by default); manning it again waits for Recovery (pass 13), but a crew or HS on foot may hook it up to a vehicle. The Labor counter after a failed push is not built. |
| R8.7 | Can a Gun fire at another level or across boards? | C2.6, C3.2: the game reads the elevation difference and the range and refuses a shot the C2.6 limit forbids; fire at another level is otherwise decided when levels come to fire (pass 10.2). The Covered Arc and Target Facing bearings are read across the composed map, reversed boards included. |
| R8.8 | Which special shots are built? | C5.5 (Case E): a Gun fires at enemy Infantry in its own Location with +2, doubled in woods or a building, without Cases J3, J4, L, and M, and without changing its CA; C5.51's Defensive First Fire in its own hex, turning with Case A, is not built. C5.8 (Case H): a Gun manned by a squad or HS of its nationality (non-qualified) adds +2. C6.4 (Case M): the Scenario Defender, named at setup, may Bore Sight one Location per Gun at setup, outside its hex, in its LOS, and within 16 hexes: -2 while its original crew fires it from its setup Location, the firer choosing Case M or N. C3.21, C5.1: a shot may name the facing to turn to. Multiple Hits (C3.8) need a Gun of 40mm or less, which the catalog has not; captured use needs Recovery. |
| R8.9 | How is a Covered Arc changed and shown? | C3.22: a Gun whose crew could still fire it without Intensive Fire may change its CA without firing in a friendly fire phase; it then fires no more that phase, and a change in the PFPh ends its and its crew's movement for the Player Turn. As a project ruling, the rule's "at the end of the phase" is read as "and then no more fire that phase". The Play page lists, for each Gun, every Location with an enemy unit in its LOS: in its CA, the hexspines to turn, out of range, or refused and why, and draws the CA's two bounding rays on the map. |
| R8.10 | What do overstacked Locations do to ordnance? | A5.12, A5.131: a Gun or tank firing from a Location its side overstacks adds +1 To Hit per squad equivalent (FRU) over the limits (vehicles over the limit are not counted, backlog); Personnel attacked by ordnance in a Location their side overstacks give -1 To Hit per squad equivalent over. A crew counts as a HS. A5.132's accidental hits on another vehicle are not built. |
| R8.11 | Which catalog details are added? | Chapter H Ordnance Listings (pp. 351, 363) and their Key: each Gun's BPV, its dates of use, and its Animal-Pack capability (note O), as printed, in catalog 1.7.0. They change no play; Gun repair stays blocked, since no registered source prints a Gun's malfunctioned side. |
| R8.12 | What else is out? | Gun Duels, OVR Prevention, a vehicle's Intensive Fire, Area Target Type fire at a Gun, AP and HEAT at Guns, Random SW Destruction of Guns, the Labor counter, Passengers, Recovery and captured Guns, Multiple Hits, and levels in fire stay in the backlog. |
| R9.1 | Which counters? | Chapter H Ordnance Listings (pp. 351, 363), in catalog 1.8.0: the German 5cm leGrW 36 and the Russian 50mm RM obr. 40, light mortars (C9.2) and so SW: MTR 50* overscored (HE only, C2.21), ROF 3, no B# printed so B# 12 (C2.28), range 2-13 and 3-20, 5 and 4 PP, dates 36-45 and 39-45, no BPV printed. The listings do not print a SW mortar's malfunctioned side, so mortar repair is not built, as for Guns (R8.11). The PF is not a counter: it is inherent in German Infantry (C13.3). The Panzerschreck's To Hit Table is printed only on its counter (C13.48), and no registered source prints the ATR counter's values (C13.2), so neither is added; both are in the backlog as a question for the user. Vocabulary 1.13.0 lets a light mortar carry its caliber suffix and dates. |
| R9.2 | How does a light mortar fire? | C9.1, C9.2, C3.33: possessed by a Good Order squad, HS, crew, or SMC of its nationality (no crew or Case H needed), in its side's PFPh, DFPh, or AFPh (Case B), or as Defensive First Fire in the MPh at the moving units; always HE on the Area Target Type, at the C3 To Hit Table's red Area row (p. 700) with the C4 modifications for * and for 57mm or less; never nearer than its minimum range nor beyond its maximum (C9.4). It has no CA (no Case A) and no Intensive Fire (Case F is NA to SW). Firing it is using a SW (A7.35): a squad keeps its inherent FP, a HS or crew loses it (A7.351); a lone SMC fires it without Multiple ROF (C9.2) and loses his own FP (A7.352); a squad may fire it after its inherent FP, but not after it fired its inherent FP with another SW, nor after it fired in an earlier phase (a Prep Fire counter in the AFPh, a Final Fire counter in the DFPh), and after its mortar it fires its inherent FP with no other SW (referee and table player findings). One Good Order leader in the firer's Location who fires nothing else adds his leadership modifier to the mortar's To Hit DR, as his direction for the phase, and goes on directing its further ROF shots (A7.531, A7.53, C9.2). It does not fire from a building Location (B23.423), nor in the AFPh after it was carried to a new Location in the MPh (A4.41) (referee findings). |
| R9.3 | What does the Area Target Type hit? | C3.33, C3.331, C9.5, C3.71, B13.3: every unit in the target hex, friendly ones included; since a friendly unit hit would take its MC against its own side's ELR, which the shot does not carry, a shot at a hex holding friendly units is refused (a deviation, backlog section 1). The To Hit DR is judged for each unit with the shot's DRM plus its own: Case K for a concealed enemy unit, Case N only for Known units, and the -1 per squad equivalent of its side's overstacking; Cases C1 to C4, E, G, L, and Q do not apply. A CH needs an Original 2 (with C3.7's exception when only the lowest Final DR hits, and C3.6). Each unit hit is attacked with one IFT DR at half the HE FP (C.6; 6 FP for 50mm, so the 2 column), with its TEM on the IFT, woods giving -1 instead of +1 (Air Bursts); the unit Random Selection picks for a CH is attacked at the full FP doubled, with positive TEM reversed but Air Bursts kept at -1. The Area Target Acquisition applies to every unit of the hex, concealed or not, and each shot that does not malfunction acquires the hex, one step more if already acquired (C6.521, C6.57). The CH and C3.6 are judged for each unit with one subsequent dr: an Original 2 is a CH on a unit more than the lowest Final DR could hit; a unit only the lowest Final DR hits takes a CH on a subsequent dr of 1; a unit no Final DR could hit is hit on 1 to 3, a CH on 1 (referee findings). A mortar's Defensive First Fire leaves no Residual FP, although A8.2 gives it half its IFT column: a deviation (backlog section 1), as for Guns. A hex with units in more than one Location, a vehicle, or a manned Gun is refused (backlog). |
| R9.4 | How does spotting work? | C9.3, C9.31: in the PFPh or DFPh, when the Spotter is designated (the AFPh would need Opportunity Fire, not built; referee finding), a mortar may fire at a hex in the LOS of a Spotter: a Good Order Personnel unit of its side in the mortar's hex or an adjacent one, named with the shot. Spotted fire adds +2 To Hit and lowers the Multiple ROF by one; the Hindrance is read along the Spotter's LOS, and a pinned Spotter adds Case D. Spotting is use of a SW for the Spotter, marked with a fire counter; a unit spots for one mortar a phase, and a HS, crew, or SMC that has fired does not spot (C9.31 EX, A7.352; table player finding). Once a mortar has a Spotter, only that unit spots for it while it stays Good Order and on the map; when it breaks or is eliminated or captured, another may be named (the rule's wait until the next MPh is not built). A leader with the Spotter does not modify the shot (the C9.31 EX). In the MPh a mortar fires only at what its firer sees. |
| R9.5 | How are SMOKE grenades placed? | A24.1, A24.11: in its MPh, a Good Order squad with a Smoke Placement Exponent (the German 4-6-7 1, the 4-6-8 2; no Russian squad has one), in the moving stack and still able to move, may once per MPh name its own Location (1 MF) or an ADJACENT Location at its level (2 MF), not in water or marsh; the game has no wind, so adjacent placement is allowed. The MF are an expenditure of the moving stack, which opens the DEFENDER's window. A dr at most the exponent, +1 if the squad is CX or Double Times with the attempt (A4.51; table player finding), places a 1/2" SMOKE counter (+2); a 6 ends the stack's MPh; the attempt is not use of a SW. The counters are removed at the end of that MPh. A dr of 6 ends the placing squad's MPh only, not its stack's. Placement at another level, over a Crest Line on a subsequent dr, down a stairwell, or to the ground level of a non-Interior building hex is refused: deviations from A24.1 while levels are not built (backlog section 1; referee finding). |
| R9.6 | What does SMOKE do? | A24.2, A24.7, A24.8: +2 Hindrance for fire traced into, through, within, or out of its Location, and +1 more for fire traced out of or within it; a Location's SMOKE, with a Blaze's, is at most +3 before the outgoing +1; it costs one more MF or MP to enter a SMOKE Location; FFMO (and Case J4) does not apply to a target in a SMOKE Location, FFNAM does. SMOKE affects Direct Fire and mortar fire; the +2 of the target Location applies to Residual FP too, the outgoing +1 does not (A8.2, A24.8; referee finding). |
| R9.7 | How is a PF used? | C13.3, C13.31, the SW Chart (p. 694): in a game dated after September 1943, a Good Order or berserk German Infantry unit that may still fire in its fire phase, or in the DEFENDER's window of an enemy vehicle's MP expenditure, makes a PF Check dr: -1 in 1945, +1 for a HS or crew, +2 for a SMC, +1 if CX. A final 1 to 3 gives a shot, fired at once; 4 or more none; an Original 6 pins the unit (breaks it if already pinned; Casualty Reduction if berserk or heroic). The check is use of a SW: a squad makes one check unless it fired in an earlier phase, fired its inherent FP with another SW this phase, or, in the MPh, is marked First Fire (no check in Subsequent First Fire), and a second only while its first check is its only fire (so forfeiting its inherent FP); a HS, crew, or SMC checks only while it has not fired, and loses its FP (A7.351; referee finding). The shots of a scenario may not exceed the German squad equivalents set up (a HS or crew counting half; the game has no reinforcements, so its OB is what set up) before 1944, one and a half times (FRD) in 1944, and twice in 1945. PFk, the optional usage of C13.311, and a check in Subsequent First Fire are not built. |
| R9.8 | How does a PF shot resolve? | C13.32 to C13.36, C13.8, C3.7, C7.33: only at an AFV on the Vehicle Target Type (vs other targets it uses HE Equivalency, C8.31: backlog), at 1 hex before June 1944, 2 from June to December 1944, and 3 in 1945, never within its own hex. The Modified TH# is 10 minus 2 per hex. Its DRM are the LATW ones (the green L): Cases C3 (+2 in the AFPh, and +2 from a ground-level building), D, J, J1, J2, K, P, Q, and R, CX, overstacking, and one leader's modifier (C13.35); not Cases A, B, E, F, L, M, or N. A CH needs an Original 2, with C3.7's exception; an Original 12 misses and gives the firer Casualty Reduction, an 11 or 12 for Inexperienced Infantry (A19.32). A hit is resolved on the HEAT To Kill Table at TK# 31, with the Vehicle Target Type's To Kill rules. From above ground level, from a pillbox, or by a pinned unit from a ground-level building it is refused (Desperation is not built; referee finding). A PF at range 0 (TH# 10) is refused, a deviation no game in the review reaches, since Infantry and an enemy vehicle never share a Location (R25.3). |
| R9.9 | What else is out? | The Panzerschreck and ATR (R9.1), ordnance SMOKE and WP (no catalog Gun has an s# or WP#), WP grenades (no nationality in the game has them), Dispersed SMOKE, drift, Gusts, and weather, mortars against vehicles and Guns (C1.55, C3.332) and at hexes of several Locations, units out of the firer's LOS in the target hex, mortar repair and dismantling, the Spotter's waiting period and the acquisition a spotting squad loses by firing, Area Target Type fire by Guns (C3.33), Bore Sighting by light mortars (C6.41), Opportunity Fire, PF against non-AFV targets, PFk, and Desperation fire stay in the backlog. |

## Appendix A. The Docker Linux check

Save as `docker_check.sh` in the session's scratchpad directory. It builds with warnings as errors and runs every test project of the solution and the ScenarioA1 tests, which are not in the solution. Every step must report `== exit 0`.

```bash
#!/bin/bash
# Usage: docker_check.sh <branch>
BRANCH="$1"
MSYS_NO_PATHCONV=1 docker run --rm -v "E:/Archive/GitHub/dlandi/LimboDancer.MCP:/repo:ro" mcr.microsoft.com/dotnet/sdk:10.0 bash -c '
  git config --global --add safe.directory "*"
  git clone -q --branch '"$BRANCH"' /repo /work || exit 1
  cd /work
  step() { echo "== $1"; shift; "$@"; echo "== exit $?"; }
  step restore dotnet restore src/ASL/LimboDancer.Domains.Asl.sln --locked-mode -v:minimal
  step build dotnet build src/ASL/LimboDancer.Domains.Asl.sln --configuration Release --no-restore --warnaserror -v:minimal
  step test dotnet test src/ASL/LimboDancer.Domains.Asl.sln --configuration Release --no-build --no-restore
  step a1 dotnet test src/ASL/tests/LimboDancer.Domains.Asl.ScenarioA1.Tests/LimboDancer.Domains.Asl.ScenarioA1.Tests.csproj --configuration Release --warnaserror -p:RestoreLockedMode=false
  echo "== done"'
```
