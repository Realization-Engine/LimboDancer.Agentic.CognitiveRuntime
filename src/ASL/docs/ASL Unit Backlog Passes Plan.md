# ASL Unit Backlog Passes Plan

**Status:** Draft for the user's review. Nothing here is authorized yet.

**Date:** 2026-09-27

**Scope:** every open row of the [ASL Unit Backlog](<ASL Unit Backlog.md>) (138 rows after the 2026-09-27 housekeeping; the heroes, Berserk, and Surrender row of section 6 was removed as built by steps 28 and 30), grouped into twelve passes numbered after the four of the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>). Section 13 of the backlog (the Studio demo findings) is all fixed and not planned.

## 1. How the estimates are made

Each estimate is the build time of one task at the pace of the deviations passes, measured in the [ASL Unit Time Log](<ASL Unit Time Log.md>):

| Pass | Work | Plan estimate | Actual |
|---|---|---|---|
| 2 | Close Combat, Berserk, Surrender (27 rulings) | 4 to 6 h | 2:33 |
| 3 | Ordnance: a Gun firing HE (a new package and chart) | 5 to 8 h | 1:38 |
| 4 | Vehicles (catalog, package revision, movement, page) | 3 to 4 h | 3:36 (build 2:18, merge gate 1:15) |
| Fixes | Nine Studio findings | 1 to 1.5 h | 0:59 |

The earlier plan's estimates ran two to four times the actuals, so these estimates are set from the actuals, not from the page counts. Each pass also carries a fixed overhead, the second-pass reviews running alongside the build:

| Overhead per pass | Minutes |
|---|---|
| Reading and rulings for the referee | 15 |
| Documents (rulings, review, design, requirements, backlog) | 10 |
| Full local suite before the first commit | 15 |
| Merge gate (local suite and Docker in parallel) and merge | 35 |
| **Total** | **75** |

The full local suite runs before the first commit of every pass, the lesson of pass 4, whose first merge gate failed in projects the pass had not run.

The estimates are point values. Pass 4, the one pass estimated from actuals, came in inside its 3 to 4 hour estimate; the report in section 3 allows 30 percent either side.

## 2. The passes

### Pass 5: Deviations and small items

From: Backlog section 1, and the small data and page rows of sections 10, 11, and 14.

| Task | Estimate | Notes |
|---|---|---|
| 5.1 CX status: Double Time (A4.5), the CX DRM on fire and CC, and the CX a Melee withdrawal or an advance brings (A11.21, A4.72) | 0:30 | Removes a section 1 deviation and rows of sections 4 and 11 |
| 5.2 The captor's choice at a surrender: No Quarter and Massacre (A20.3, A20.4) | 0:25 | Section 1 |
| 5.3 Owner options in mid-resolution: decline the Leader Creation dr, refuse Battle Hardening, and the firer's Unlikely Kill dr (A18.11, A15.3, A7.309) | 0:30 | One pending-choice mechanism for all three; section 1 |
| 5.4 A second Heat of Battle DR in one attack: a roll key per check (A15.1) | 0:10 | Section 1 |
| 5.5 A hero created in the MPh moving on with his creator (A15.21) | 0:15 | Section 1 |
| 5.6 A Casualty Reduced squad's HS keeping its SW, with portage (A7.302) | 0:15 | Section 1 |
| 5.7 Acquisition that follows its target unit (C6.5, C6.51) | 0:20 | Section 1 |
| 5.8 A vehicle's declared next hex for Motion, and MP left unspent counted in the final hex with the DEFENDER's further fire (D2.4, D2.1, A8.14) | 0:25 | Section 1 |
| 5.9 Recall as its own status: exiting a friendly board edge, and Abandonment of an immobilized Recalled AFV (D5.341, D5.5) | 0:20 | Section 1; needs board edges from the map |
| 5.10 Small data and page rows: Grain season for Infantry MF, the passenger-only cs# in the wreck name, the Stun +1 label and the used BU toggle, counter names on the Play page, and the three CC page rows | 0:30 | Sections 10, 11, 14 |
| Overhead | 1:15 | |
| **Pass 5 total** | **4:55** | Build 3:40 |

### Pass 6: Vehicles, part 2

From: Backlog section 14, apart from armor and vehicle movement.

| Task | Estimate | Notes |
|---|---|---|
| 6.1 An AFV's +1 TEM for friendly Infantry, its +1 LOS Hindrance, and FFMO cancelling, with the LOS read exposing the hexes it crosses (D9.3, D9.4, A4.6, D2.41) | 0:50 | Removes the refusal that makes Infantry with their AFV untouchable |
| 6.2 Residual FP against trucks and Vulnerable crews (A8.2, A8.222) | 0:30 |  |
| 6.3 Wrecks and burning wrecks: counters, LOS Hindrance, and movement (D10) | 0:40 | Removes a section 1 deviation |
| 6.4 Vehicle concealment and HIP, and a vehicle entering a Location with unseen enemy units (A12.2, A12.15) | 0:35 |  |
| 6.5 Vehicle MG fire in the MPh (Defensive and Bounding First Fire), BMG and CMG, and vehicular MG repair (D3.3, D3.7) | 0:45 |  |
| Overhead | 1:15 | |
| **Pass 6 total** | **4:35** | Build 3:20 |

### Pass 7: Armor

From: Closed-topped AFVs and their main armament; the Vehicle Target Type.

| Task | Estimate | Notes |
|---|---|---|
| 7.1 Catalog: a German and a Russian closed-topped tank from Chapter H, reviewed row by row | 0:40 | For example a PzKpfw III and a T-34 |
| 7.2 The Vehicle Target Type To Hit, with Target Facing, Case J and the other vehicle Cases (C3.33, C5, C6) | 0:40 | Also lets Guns fire at vehicles |
| 7.3 AP, HEAT, and APCR To Kill: the To Kill tables transcribed, armor factors, hit locations, and results (C7, C8, D1.6) | 1:30 | The largest chart work of the plan |
| 7.4 Main armament fire by a vehicle: turret and hull Covered Arcs, turret turning, and BU and CE for closed-topped crews (D1.3, D3.1, D5) | 0:50 |  |
| 7.5 Crew survival and the AFV's crew after a kill (D5.6, D5.7) | 0:20 | Uses the cs# already in the catalog |
| Overhead | 1:15 | |
| **Pass 7 total** | **5:15** | Build 4:00 |

### Pass 8: Guns, part 2

From: Backlog section 12, apart from armor and mortars.

| Task | Estimate | Notes |
|---|---|---|
| 8.1 Defensive First Fire by Guns, Cases J1 to J4, Gun Duels, and a kept ROF in Final Fire (C2.2401, C2.241, C6.1 to C6.17) | 0:40 |  |
| 8.2 Intensive Fire and OVR Prevention (C5.6 to C5.641, C2.5) | 0:15 |  |
| 8.3 Guns and crews as targets, and Gun destruction (C11, C11.6 chart) | 0:40 | Also CC with a Gun's crew |
| 8.4 Crews' inherent fire, and its loss after firing the Gun (A7.352) | 0:20 |  |
| 8.5 Concealed crews and Guns (A12.14, C6.57) | 0:20 |  |
| 8.6 Gun movement: manhandling, limbering, towing, abandoning (C10, C2.8) | 0:50 | Towing needs the trucks of pass 4 |
| 8.7 Targets at another level and C2.6 limits; Covered Arcs across boards and on reversed boards | 0:30 |  |
| 8.8 Multiple Hits, Case E, Case H, Bore Sighting, and choosing a facing (C3.8, C5.5, C5.8, C6.4, C3.21) | 0:30 |  |
| 8.9 Turning a Gun without firing, and the Covered Arc overlay on the Play page (C3.22) | 0:30 |  |
| 8.10 Overstacked Locations for ordnance (A5.12, A5.131), and the catalog's Gun BPV, dates, and Animal-Pack | 0:20 | Gun repair stays blocked: no registered source gives the malfunctioned sides |
| Overhead | 1:15 | |
| **Pass 8 total** | **6:10** | Build 4:55 |

### Pass 9: Mortars, SMOKE, and LATW

From: Backlog section 7 and the Area Target Type.

| Task | Estimate | Notes |
|---|---|---|
| 9.1 Catalog: a light mortar of each side, and the Panzerschreck, Panzerfaust, and Russian ATR | 0:40 |  |
| 9.2 The Area Target Type and mortar spotting (C3.31, C9) | 0:50 |  |
| 9.3 SMOKE and WP: placement, Hindrance, dispersal, and WP morale checks (A24) | 1:00 | Smoke also from section 6 |
| 9.4 LATW To Hit, Backblast, and PF checks (C13) | 1:00 |  |
| Overhead | 1:15 | |
| **Pass 9 total** | **4:45** | Build 3:30 |

### Pass 10: Movement and terrain

From: Backlog sections 4 and 9, and the berserk-route deviation.

| Task | Estimate | Notes |
|---|---|---|
| 10.1 Terrain transcriptions beyond the reviewed set: walls, hedges, rubble, marsh, and the rest of board 01's terrain (B chapter, Terrain Chart) | 1:00 |  |
| 10.2 Level changes: stairs, upper levels, hills, and fire across levels with its Hindrance (A4.13, B10, A6) | 0:50 | Also section 3's fire across levels |
| 10.3 Hexside terrain on entry and at a target (B9) | 0:40 |  |
| 10.4 Bypass (A4.3) | 0:40 |  |
| 10.5 Hazardous Movement, the Road Bonus, a leader's MF bonus, and Minimum Move (A4.62, A4.132, A4.12, A4.134) | 0:30 |  |
| 10.6 Concealed movement and the loss of "?" (A12.13, A12.14) | 0:35 |  |
| 10.7 Entry into enemy-occupied non-building Locations, TPBF, and Snap Shots (A4.14, A7.212, A8.15) | 0:45 |  |
| 10.8 Building entry of steps 7 to 11 folded into the move action | 0:30 |  |
| 10.9 Every berserk charge route decided, removing its deviation, and a charge onto a lone SMC as an Infantry OVR (A15.431, A15.432) | 0:25 | After the terrain work |
| Overhead | 1:15 | |
| **Pass 10 total** | **7:10** | Build 5:55 |

### Pass 11: Vehicle movement and OVR

From: The rest of backlog section 14.

| Task | Estimate | Notes |
|---|---|---|
| 11.1 Reverse movement, VBM, ESB, Minimum Move, and vehicle stacking (D2.2, D2.3, D2.5, D2.14, D2.15) | 0:50 |  |
| 11.2 Vehicle terrain beyond Open Ground, Grain, and roads, levels, and hexside terrain; bog (D8) | 0:50 | After pass 10 |
| 11.3 Vehicular OVR and CC against vehicles, sequential CC, and Infantry entering an enemy vehicle's Location (D7, A11.5, A11.6) | 1:30 |  |
| Overhead | 1:15 | |
| **Pass 11 total** | **4:25** | Build 3:10 |

### Pass 12: Fire extensions

From: Backlog sections 3 and 8, and the fire rows of section 11.

| Task | Estimate | Notes |
|---|---|---|
| 12.1 Opportunity Fire and the Bounding Fire marker (A7.25) | 0:30 |  |
| 12.2 Declared fire with a blocked LOS (A6.11) | 0:10 |  |
| 12.3 FPF directed by a leader, mixed FPF groups, and one attack on a stack mixing pinned and unpinned units | 0:30 |  |
| 12.4 A squad firing its inherent FP apart from its MG, SMC fire, and a leader firing a MG | 0:30 |  |
| 12.5 The Concealment Table (A12.12) | 0:25 |  |
| 12.6 Spraying Fire and Fire Lanes (A9.5, A9.22) | 1:00 |  |
| 12.7 Fire into a Melee Location and by units in Melee, fire at prisoners and by a Guard, and berserk fire (A11.15, A20.52, A15.432) | 0:40 |  |
| 12.8 Encirclement: fire, morale, and rally (A7.7) | 0:30 |  |
| Overhead | 1:15 | |
| **Pass 12 total** | **5:30** | Build 4:15 |

### Pass 13: Rally, Rout, and support weapons

From: Backlog sections 2 and 5, and section 8's Self-Rally row.

| Task | Estimate | Notes |
|---|---|---|
| 13.1 DM: ADJACENT armed enemies, the start of the RtPh, and retention (A10.62, A10.63) | 0:30 |  |
| 13.2 Rally terrain beyond woods and buildings | 0:20 |  |
| 13.3 Routing and the RtPh (A10.5) | 1:00 | Also surrender in the RtPh from section 11 |
| 13.4 Deploy and recombine (A1.31, A1.32) | 0:25 |  |
| 13.5 SW transfer, possession, recovery, dismantling and assembly, and captured SW (A4.43, A9.8, A21) | 1:00 |  |
| 13.6 Self-Rally capability of the catalog counters | 0:10 |  |
| Overhead | 1:15 | |
| **Pass 13 total** | **4:40** | Build 3:25 |

### Pass 14: Close Combat and capture, part 2

From: The rest of backlog section 11.

| Task | Estimate | Notes |
|---|---|---|
| 14.1 Hand-to-Hand CC and its red Kill Numbers (J2.31) | 0:30 |  |
| 14.2 Concealed and hidden units and Dummies in CC, and TI units (A11.19, A12) | 0:40 |  |
| 14.3 Capture attempts, prisoner escape and recapture, and moving or transferring prisoners (A20) | 0:55 |  |
| 14.4 Infiltration and Ambush Withdrawal (A11.22, A11.4) | 0:25 |  |
| 14.5 Overstacked CC and advances; Unarmed units; surrender of a Disrupted unit (A5, A20.5, A19.12) | 0:40 |  |
| 14.6 Field Promotion edge cases, odds just above 10 to 1, the berserk Ambush drm, a Guard advancing, and mandatory CC the package refuses | 0:40 |  |
| Overhead | 1:15 | |
| **Pass 14 total** | **5:05** | Build 3:50 |

### Pass 15: Special units and nationalities

From: Backlog sections 6 and 10, and the nationality rows of section 11.

| Task | Estimate | Notes |
|---|---|---|
| 15.1 Flamethrowers, Demolition Charges, and Molotov cocktails (A22, A23, A25.23) | 1:40 |  |
| 15.2 Snipers (A14) | 1:00 |  |
| 15.3 Commissars and NKVD Field Promotion; Allied Troops (A25.22, A25.25, A25.5) | 0:40 |  |
| 15.4 Underscored morale (A19.13), Green MMC (A19.3), and hero SW use and concealment (A15.23, A15.21) | 0:45 |  |
| 15.5 Other nationalities: catalog counters and their Heat of Battle and Leader Creation exceptions (A25) | 1:00 |  |
| 15.6 A berserk unit's choice of which 1PP SW to abandon | 0:10 |  |
| Overhead | 1:15 | |
| **Pass 15 total** | **6:30** | Build 5:15 |

### Pass 16: Night, weather, and scenario cards

From: Backlog section 6 and the night rows of sections 2 and 11.

| Task | Estimate | Notes |
|---|---|---|
| 16.1 Night: visibility, NVR, illumination, and Night Ambush and DM rules (E1) | 1:30 |  |
| 16.2 Weather and Extreme Winter Fate (E3) | 1:00 |  |
| 16.3 Scenario cards as a registered source: ELR and setup from the card (A19.1) | 0:40 |  |
| Overhead | 1:15 | |
| **Pass 16 total** | **4:25** | Build 3:10 |

## 3. Duration report

| Pass | Title | Tasks | Build | With overhead | Range (-30 % to +30 %) |
|---|---|---|---|---|---|
| 5 | Deviations and small items | 10 | 3:40 | 4:55 | 3:26 to 6:24 |
| 6 | Vehicles, part 2 | 5 | 3:20 | 4:35 | 3:12 to 5:58 |
| 7 | Armor | 5 | 4:00 | 5:15 | 3:40 to 6:50 |
| 8 | Guns, part 2 | 10 | 4:55 | 6:10 | 4:19 to 8:01 |
| 9 | Mortars, SMOKE, and LATW | 4 | 3:30 | 4:45 | 3:20 to 6:10 |
| 10 | Movement and terrain | 9 | 5:55 | 7:10 | 5:01 to 9:19 |
| 11 | Vehicle movement and OVR | 3 | 3:10 | 4:25 | 3:06 to 5:44 |
| 12 | Fire extensions | 8 | 4:15 | 5:30 | 3:51 to 7:09 |
| 13 | Rally, Rout, and support weapons | 6 | 3:25 | 4:40 | 3:16 to 6:04 |
| 14 | Close Combat and capture, part 2 | 6 | 3:50 | 5:05 | 3:34 to 6:36 |
| 15 | Special units and nationalities | 6 | 5:15 | 6:30 | 4:33 to 8:27 |
| 16 | Night, weather, and scenario cards | 3 | 3:10 | 4:25 | 3:06 to 5:44 |
| | **All passes** | **75** | **48:25** | **63:25** | **44:24 to 82:26** |

At the pace of 2026-09-27 (passes 2 to 4, the Studio demo, and the fixes in about 10 hours), the whole plan is about 63:25 of working time, or 8.5 working days of 7.5 hours.

## 4. Order and dependencies

- **Pass 5 first:** it removes the recorded deviations, which each earlier plan named as the first work of a later pass, and fixes the small data and page rows.
- **Passes 6 and 7** build on pass 4's vehicles; pass 7's Vehicle Target Type also serves pass 8's Guns.
- **Pass 9** (mortars, SMOKE) comes after pass 8, which settles the ordnance model it extends.
- **Pass 10** (movement and terrain) must precede pass 11 (vehicle movement over the same terrain) and settles the berserk routes.
- **Passes 12 to 16** are independent of each other and may be taken in any order; pass 16 (night and weather) touches every other area, so it is last.
- **Blocked:** Gun repair and removal (backlog section 12) needs a Gun's malfunctioned side, which no registered source prints; it stays in the backlog until one is registered.

## 5. How each pass is run

As in the earlier plan, section 12: a branch per pass; a review stage with a referee agent, a live stage with a table-player agent; rulings proposed to the plan, subject to the referee; documents; the full local suite and the Docker check; then the merge. Each task's start and end go in the time log.

## 6. Decisions for the user

1. Approve the plan, or reorder or drop passes.
2. Authorize the passes to run without stopping, merging each on the local suite and the Docker check, or authorize them one at a time.
