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
