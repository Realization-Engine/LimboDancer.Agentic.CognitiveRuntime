# ASL Unit Backlog Pass 60 Design

**Status:** Built to the merge gate on 2026-10-15 under the user's word of that day ("this is my word to start pass 60, run without my intervention up to the gate"), given on 2026-10-13 as a mode and on 2026-10-15 as the start: the frame, six tasks in six commits (the plan's 60.1 to 60.6, with 50.5 carried in from pass 50 and built with 60.6), the five reviews and their fixes, a narrower Studio check at the user's word, and the documents. Sections 13 to 15 say how it ended; section 13.1 lists what departs from the plan's entry and the frame. **The gate is not run:** it is the user's to start (section 15). The frame's questions were put to five read-only agents, as that word asks; their answers were judged against the pages and the code, and each decision below is a ruling decided under that word and open to a veto. The word ends at the gate.

**Date:** 2026-10-15

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 22.1, "Pass 60: Armored combat III"; the [rule inventory](<../ASL Rule Inventory A to E.md>); the [backlog](<../ASL Unit Backlog.md>), sections 58 and 59; the [pass 55 design](<ASL Unit Backlog Pass 55 Design.md>), sections 3, 9.1, and 14; the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>); the session's prompt, `Prompts/Pass 60 Prompt I.md`.

**The rulebook.** Read for this frame by three read-only agents from the page texts of the working folder, with their examples: pages 52, 55, 56, 60, 63 to 65, 77, 89 to 92, 125, 128, 136, 162, 165, 169 to 171, 174 to 179, 181 to 185, 189, 190, 192, 193, 198, 199, 201, 203, 207, 209, and 221. The C7.34 and C8.31 tables (pp. 190, 178) were read on renders of the PDF, since the text layer scrambles their columns. I read the agents' answers against the inventory's rows and the code; I did not read every page myself. Each task reads its own pages again before it builds.

**What the frame rests on.** Two read-only agents mapped the code on this branch (cut from main at 380b4a7b, after the Goodwood merge); the map is `code-builders/pass-history/pass-60-armored-combat-iii/codemap.md`. Three more read the rules of 60.1 and 60.2, of 60.3 and 60.4, and of 60.5, 60.6, and task 50.5, and answered the frame's questions with a proposal each.

## 1. Outline of this design

1. This outline.
2. What the pass is, what departs from the plan's entry, and its increments.
3. The 49 rows, by task.
4. What is certain, and what needs a read.
5. What the frame found outside the rows.
6. The frame's questions, each with its analysis and its ruling.

Then: 7, HE Equivalency (60.4); 8, the Specific Collateral Attack and mixed Locations (60.1); 9, Indirect Fire (60.2); 10, FT, MOL, and DC at an AFV (60.3); 11, Guns as targets (60.5); 12, a MG's To Kill attack (60.6, with 50.5); 13, the reviews, their fixes, and the Studio check; 14, as built; 15, the gate.

## 2. What the pass is

Pass 60 is the third pass of block 2, armored combat: the attacks on vehicles and Guns other than an AP shot on the Vehicle Target Type. HE and AP at a Location that holds a vehicle; a mortar's hit on a vehicle and on a building of several levels; FT, MOL, and DC at an AFV; AP and HEAT at Infantry by their HE Equivalency; a Gun and its crew as a target; a KIA's destruction of the weapons its victims held; and, carried in from pass 50, a MG's To Kill attack on an AFV.

**What departs from the plan's entry, first** (each decided under the user's word of 2026-10-15):

- **The pass is larger than the plan's entry.** The plan names 37 rows and 4:40 of building; the inventory names 49 rows for pass 60, and the prompt carries in task 50.5 (MG fire against armor), which has no row in the plan's table. The estimates made from the code are 6:40 of building (section 6, F10). The pass stays one pass and is built in the order of the table below, with the carried-in task last, so that whatever a session's end leaves out is the task carried in, not one of the plan's (ruling R60.1).
- **The tables are Rules code, not transcriptions** (F2). C8.31's HE Equivalency, C7.34's FT, MOL, and DC numbers, the C7.7 columns for those attacks, and the C11.6 Gun Destruction Table go into the Rules project as small tables with their page, so that no transcription, case matrix, or package is changed and the four packages are not pinned again.
- **The To Kill table's note A** (a red CS# lowers the DR for the burning test) is left out: the catalog's Crew Survival number carries no color, and adding one changes the catalog. Backlog.
- **A5.132** (a hit on another vehicle of a vehicle-overstacked hex) is moved on to pass 110, beside OVR Prevention: it needs a Random Selection by size among vehicles and a target choice the To Hit does not have (F9).
- **The upper superstructure of a non-turreted AFV** (C3.9, the end of ruling R7.4) is built only if 2f has time; otherwise moved to pass 105 (F9).

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 2a | 60.4, and C11.52 of 60.5 | HE Equivalency (C8.31): AP, APCR, APDS, HEAT, and the 20L ATR on the Infantry Target Type at unarmored targets; HEAT's target bar; no Residual FP; a PF and a PSK at Personnel; AP, HEAT, and an ATR at a Gun. | 0:50 |
| 2b | 60.1 | The Specific Collateral Attack (D.8A) after a Vehicle Target Type hit; the Infantry Target Type at a Location that holds a vehicle; Known and concealed units together. | 1:00 |
| 2c | 60.2 | A mortar's hit on a vehicle (C1.55, C3.332, C3.72, C7.345), on a building of several levels (B23.32), Air Bursts in full (B13.3, D5.311), Area Acquisition by hex (C6.521), the gunshield's +1 against Indirect Fire, a Critical Hit's reversal of Height Advantage (C3.71). | 1:10 |
| 2d | 60.3 | FT, MOL, and DC at an AFV (A22.34, A22.612, A23.5, C7.344, C7.346, C7.21, the C7.7 columns, D2.41). | 1:20 |
| 2e | 60.5 | Random SW and Gun Destruction (A9.74, C8.42); an unattended Gun as a target; the Gun Destruction Table's other columns; Emplacement's limits. | 1:00 |
| 2f | 60.6, 50.5 | A MG's To Kill attack on an AFV (A9.6, A9.61, A9.611, D3.54) with Case A and the MG column; a MG's Fire Lane and CC Reaction Fire (A9.223, D7.21); the upper superstructure if time allows. | 1:20 |
| | Overhead (the plan's) | The reviews, the Studio check, the documents. | 1:15 |
| | **Total** | build 6:40 | **7:55** |

## 3. The 49 rows, by task

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 60.1 | D.8A (192) | The Specific Collateral Attack | 2b |
| 60.1 | C3.32 (169) | The Infantry Target Type at a Location with a vehicle | 2b |
| 60.1 | C3.4 (170) | Known and concealed units together | 2b |
| 60.1 | C3.41 (170) | What the Infantry and Area Target Types may attack | 2b for an unarmored vehicle; 2e for an unattended Gun. A building, a bridge, an empty hex: pass 140. |
| 60.1 | C3.74 (171) | Random Selection with vehicles in the pool | 2b; among Locations, 2c |
| 60.2 | C1.55 (165), C3.332, C3.72 (170, 171), C7.345 (177), C9.1 (179) | A mortar's hit on a vehicle | 2c |
| 60.2 | B23.32 (136) | Indirect Fire by building level | 2c, outside an Interior Building Hex; rooftops and Interior hexes: pass 140 |
| 60.2 | B13.3 (128) | Air Bursts in full | 2c |
| 60.2 | C6.52, C6.521 (174) | Bracketing; Area Acquisition by hex | 2c for a mortar; a transfer between Target Types: pass 110 |
| 60.2 | C3.7, C3.71 (170) | A Critical Hit's reversal of Height Advantage | 2c |
| 60.3 | A7.307 (55), A22.34, A22.612 (89), A23.5 (91), C7.344, C7.346, C7.7 (177), C7.21 (176), D2.41 (198) | FT, MOL, DC at an AFV | 2d. A MOL at an unarmored vehicle, ATMM, a vehicle as the thrower, a throw down a level, the Flame of a MOL: backlog. Note A of C7.7: backlog. |
| 60.4 | C8.31 (178), C13.9 (185), C13.31 (183) | HE Equivalency; a SCW at Personnel | 2a |
| 60.5 | C11.52 (182), C13.23 (183) | AP and an ATR at a Gun | 2a |
| 60.5 | C11.2, C11.3, C11.4, C11.5, C11.51, C11.6 (181, 182) | Guns as targets | 2c for the gunshield's +1 against Indirect Fire; the rest 2e |
| 60.5 | A9.74 (65), C8.42 (178), A9.8 (65) | Random SW and Gun Destruction; a Canister KIA; a dm SW | 2e |
| 60.6 | C3.9 (171), D3.2 (199) | The upper superstructure; the colored dr facing for a MG from its own hex | 2f |
| 60.6 | A5.132 (52) | A hit on another vehicle of an overstacked hex | Pass 110 (F9) |
| 50.5 | A9.6, A9.61, A9.611 (64, 65), D3.54 (201), C7.21 | MG fire against armor | 2f |
| 50.5 | A9.223 (63), D7.21 (207) | A Fire Lane's cancellation; CC Reaction Fire's counter on SW and Guns | 2f |
| other | B10.31 (125) | Height Advantage | 2c (the Critical Hit's reversal); its other parts stay with their passes |

The table groups the 49 rows by rule; the documents step sets each row's own note and pass.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **No attack on a vehicle exists but the ordnance shot on the Vehicle Target Type and the IFT's own handling** (`ScenarioA1FireCalculator.VehicleEffects`: an unarmored vehicle on the Vehicle line, an AFV unharmed and its CE crew attacked). An ordnance hit that does not destroy the vehicle does nothing to its crew.
- **Each newly admitted attack is refused today, by a code the frame can name:** `play.ordnance-vehicle` (R25.10), `play.ordnance-area-vehicle` and `play.ordnance-area-levels` (R9.3), `concealment-mixed`, `target-outside` for an unmanned Gun, `he-equivalency-not-built`, `gun-outside` and `target-type-outside` for AP, HEAT, a PF, a PSK, and an ATR at anything but a vehicle, `fire.mol-outside`, `play.dc-vehicle`, and `fire.target-outside` for a DC. A recorded game holds none of these attacks, so lifting a refusal changes no record.
- **Two rules change the result of attacks that are made today:** A9.74 (every Final KIA of an IFT attack on a unit that holds a SW or Gun) and C3.71's reversal of Height Advantage on a Critical Hit. D.8A also changes what an existing Vehicle Target Type hit does to a CE crew. Each is gated by a fact on the record (F3).
- **The rule packages need not be pinned again** if the tables are Rules code (F2). The catalog is not changed.
- **D9.3's +1 for Infantry beside a friendly AFV is built** on the IFT and on the ordnance To Hit DR (Case Q).
- **The game-default Guns and vehicles** (`campaign-`, sheet MFG) fall under every rule here; their values are manufactured.

**Needs a read at its task.**

- 2a: how the existing HE path on the Infantry Target Type reads the HE FP, so that the equivalency FP enters at one place.
- 2b: how the Infantry Target Type's hit list is built (`ScenarioA1OrdnanceEligibility.AreaTarget` leaves vehicles out) and how a vehicle's own To Hit DR is judged beside the units'.
- 2c: whether the board data tells an Interior Building Hex and a rooftop apart; how the Area calculator's one Location becomes several.
- 2d: how a fire request names a vehicle (none does today); the Aerial AF a Position DR of 5 or less reads.
- 2e: every place a KIA eliminates a unit (IFT, ordnance hit, Canister, Residual FP, CC is not an IFT attack), and where the weapons it held are dropped (`GameProjector.DropHeldBy`).
- 2f: how far a MG can ride the ordnance shot (the To Hit, the ROF, the malfunction), against a new path.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and the absent-safety of each): on an ordnance shot, the HE Equivalency FP, the Specific Collateral Attack and its rule's gate, the vehicles of an Infantry Target Type hit, the Locations of a mortar's hit; on a fire attack, the named AFV of a FT, MOL, or DC with its To Kill and Position DR, the A9.74 dr of each weapon, and the MG's To Kill. New choice kinds: none planned.

## 5. What the frame found outside the rows

1. **The test-game library's default catalog is 1.14.0** (`code-builders/test-games/game_file.py`) while the code's is 1.17.0, and fire facts refuse a game of another version (`play.fire-catalog`). Repaired in 2a: the default follows the code.
2. **`area-friendly-units` has no sentence** in `RefusalReasons`. Repaired in 2c, where the mortar's refusals are read.
3. **The Bren Carrier's MA is a plain `mg`** (`campaign-british-bren-carrier`), which the vehicle fire path does not read (it takes `aamg` only). Repaired in 2f, where a vehicle's MA MG is read for D3.54.
4. **`gate.sh`'s `base` stage could not unpack the baseline's archive** on this machine: `tar` read `E:` as a remote host. Repaired at the branch's cut (`--force-local`), committed with the pass.

## 6. The frame's questions

Each was put to a read-only agent with a proposal and is answered by a ruling decided under the user's word of 2026-10-15. Rulings made at a task are numbered on from these.

**F1. The rows and the order.** **R60.1:** section 3's placement and section 2's order. The HE Equivalency first, since D.8A's attack takes its FP; the Specific Collateral Attack second, since every later attack on an AFV ends in it; the mortar next; then FT, MOL, and DC, which use both; the Guns; and the carried-in MG last.

**F2. The tables.** The To Kill transcription's boundary leaves out FT, MOL, DC, MG, Indirect Fire, and the Gun Destruction Table, and changing it or adding a transcription changes a case matrix, so the four packages are pinned again and every planned event's digest changes. **R60.2:** the pass's tables are Rules code with their page: C8.31 (p. 178: AP below 37mm 1; AP 37mm and up 2; every APCR, APDS, and ATR 1; BAZ and PIAT 8; PSK, PFk, and H#[9] 12; PF 16; other HEAT one IFT column left of the Gun's HE FP), C7.34 (p. 190: DC 16, FT 8, MOL 6), the DC and FT/MOL columns of C7.7 (p. 190), and C11.6 (p. 182). `BOTH_SKIP` keeps `GameEvent.RulePackage` in case a later task must pin.

**F3. Rules that change an existing attack.** A9.74, C3.71's reversal, and D.8A after a Vehicle Target Type hit would change what a recorded attack resolves to. **R60.3:** each is applied only where the record carries the pass's fact for it (a recorded fact, taken from the record at replay, never recomputed from the state), so that an attack recorded before the pass resolves as it did; the planner sets the fact on every new attack. The replay digest is where a miss shows (the standing rules, the lesson of R45.4).

**F4. HE Equivalency (C8.31, C13.9, C13.31, C11.52, C13.23).** **R60.4:** AP, APCR, APDS, and HEAT fire on the Infantry Target Type at an unarmored target at their equivalency FP, by the HE path that exists (TEM as Case Q on the To Hit DR, one IFT DR for every unit hit, a Critical Hit doubling it and reversing positive TEM). HEAT is fired at Infantry only where the target Location gives a wall, building, rubble, or pillbox TEM; an ordnance HEAT round needs an H# and a printed HE FP to step left from, and a Gun with no HE is refused it. None of these leaves Residual FP. An ATR fires at Personnel on the Infantry Target Type only if it is 20L; the catalog's two ATRs are not, so they keep firing at Personnel as Small Arms. **R60.5:** a PF and a PSK fire at Personnel under C8.31's bar (16 and 12 FP). The PF Check takes +1 at a target that is not an AFV, and +1 in August and September 1943; a PF's hit affects one unit by Random Selection. The firer's choice of a Known unit manning a Gun or SW after the hit is left out (backlog). **R60.6:** AP, APCR, APDS, HEAT, and any ATR fire at a Gun on the Infantry Target Type at their equivalency FP, and the Gun's fate is C11.4's with C11.6's note 5 (a K eliminates the Gun when AP is fired); a non-20L ATR attacks only the Gun and its manning crew.

**F5. The Specific Collateral Attack and the Infantry Target Type at a mixed Location (D.8A, C3.32, C3.4, C3.41, C3.74).** **R60.7:** after a Vehicle Target Type hit on an AFV that does not destroy it (a TK DR missed, an Immobilization, an Unconfirmed Kill), its CE crew takes an IFT attack at the round's IFT FP (HE, or its HE Equivalency) on the Original TK DR, with the CE DRM alone. None after a Dud, a Shock this hit causes (C7.42), a crew already stunned or shocked, a buttoned-up crew, or a vehicle destroyed. One attack for a Multiple Hit, and no Residual FP. Passengers and Riders: pass 65. **R60.8:** an Infantry Target Type shot may name a Location that holds an unarmored vehicle. Each vehicle and each unit is hit on its own Final TH# (the vehicle by its size; a concealed unit with Case K); every unit and unarmored vehicle hit shares one Effects DR on the HE FP column, the vehicle on the Vehicle line (A7.308, A7.309). An AFV is immune and its CE crew takes a General Collateral Attack on the same DR when the AFV's own TH# is reached. A Location holding only AFVs stays the Vehicle Target Type's (C3.41, D.6). **R60.9:** a Location of Known and concealed units is one To Hit DR judged per unit; a concealed unit hit loses its "?" as A12.14 has it, and one missed keeps it. Units out of LOS or too close: backlog.

**F6. Indirect Fire (C1.55, C3.332, C3.72, C9.1, B23.32, B13.3, C6.521).** **R60.10:** a mortar's hit on a vehicle is an IFT attack at half the HE FP, never To Kill: -1 if every AF is 4 or less, -1 if OT, +1 if every AF is 8 or more, with the TEM that gives no HD; a KIA destroys it, a DR at most half the K/# DR burns it, and the K/# DR and one above Shock a turret hit or Immobilize a hull hit, the hit's Location read from the To Hit DR's dice (C3.9). An unarmored vehicle is on the Vehicle line. The CE crew takes a General Collateral Attack on the same DR. A Critical Hit is at double the full FP with no TEM, the AF and OT modifiers kept. **R60.11:** a mortar may fire at a hex whose units are in several Locations of a building that is not an Interior Building Hex: each Location is hit on its own To Hit DR modifiers, and one Effects DR attacks every Location hit, each with its building TEM and +1 for each level above it; a Critical Hit goes to one Location by Random Selection. A Location out of the firer's LOS is not attacked. Friendly units in the target hex stay refused (backlog). **R60.12:** Air Bursts are -1 against an unarmored, CE, or OT target in woods, combined with an entrenchment or Emplacement, and kept on a Critical Hit; an OT AFV so hit is attacked as unarmored (D5.311). None against a unit in Bypass. **R60.13:** a mortar's Area Acquisition covers every Location of the hex in its LOS. **R60.14:** a Critical Hit on a Direct Fire target that claimed Height Advantage reverses it to -1 (C3.71). The gunshield gives +1, not +2, against Indirect Fire (C11.4, C11.5).

**F7. FT, MOL, and DC at an AFV.** **R60.15:** a fire request names an AFV in the target Location as the FT's, MOL's, or DC's target (predesignation). A FT's Basic TK# is 8, halved at Long Range before +1 CE and +2 OT; a MOL's is 6 with +2 OT or +1 CE (closed-topped), -2 against a moving vehicle (C.8), and +1 or +2 for an elevation advantage; both take +1 through the rear, no AF, and the FT/MOL column (below the TK# burns; equal eliminates with Crew Survival). A MOL's DR also attacks the CE crew at the Small Arms FP plus 4 and every other unarmored unit at the Small Arms FP; a failed MOL Check is an IFT attack on them all without the 4. A FT attacks only the AFV and, if it survives, its CE crew. **R60.16:** a DC at an AFV rolls the Position DR (C7.346) with its modifiers whose facts exist; 5 or less uses the Aerial AF, 6 to 8 the AF, 9 to 11 attacks the CE crew only on a new DR, 12 or more attacks the other unarmored units of the Location. Its TK# is 16 less the AF, +1 through the rear, on the DC column. A Placed DC needs the PAATC that exists. A same-hex attack is through the rear; otherwise the hexside crossed. A MOL at an unarmored vehicle, a MOL's Flame, ATMM, a vehicle as the thrower, and a throw down a level: backlog.

**F8. Guns as targets and A9.74.** **R60.17:** on every Final KIA of an IFT attack (a Canister KIA included, C8.42), each SW and Gun held by a unit it eliminated rolls a dr on the same column: a KIA destroys it, a K malfunctions it, or eliminates it if it was malfunctioned or has an X#. Ordnance, DC, and OBA at the Gun itself follow C11.4 instead. Unpossessed weapons checked by Indirect Fire and by an OVR, the OVR's end-of-MPh check, and an OVR of an empty Location: backlog. **R60.18:** an unattended Gun may be the target of the Infantry and Area Target Types: a KIA destroys it and a K malfunctions it. A Gun manned by a non-crew loses Emplacement for the game. An attack by a MG, small arms, a FT, or a MOL destroys a Gun only through A9.74 (C11.51, C11.6).

**F9. 60.6 and the carried-in MG.** **R60.19:** a SW MG fired alone at a named AFV within Normal Range in the PFPh or DFPh is a To Hit DR on the Vehicle Target Type with black numbers, then a To Kill DR with Basic TK# 4 (12.7mm and 15mm by their rows), Case A, the 25mm row of Case D, less the AF; below the number eliminates (at most half burns), equal Stuns, and there is no Shock, Immobilization, Critical Hit, or Multiple Hit; the ROF is kept as ordnance; R60.7's attack follows. A vehicle's MA that is a MG may make it (D3.54); its other MGs may not. Its Residual FP and the AFPh: backlog. **R60.20:** A9.223's narrow reading: a Fire Lane is cancelled when its MG fires CC Reaction Fire; D7.21's CC counter bars its SW and Guns from Non-CC Reaction Fire. **R60.21:** A5.132 moves to pass 110. **R60.22:** the upper superstructure of a non-turreted AFV (C3.9) is built with a fact on the record if 2f has time, else moved to pass 105.

**F10. The estimate.** Section 2's table: 6:40 of building against the plan's 4:40, from the code the agents mapped and pass 55's rate (7 increments in 4:50 against 9:50 estimated).

## 7. HE Equivalency: 60.4, and C11.52 and C13.23 of 60.5 (increment 2a)

Built on 2026-10-15 in one commit (afdcb2b5) by a building agent to a brief (`brief_build.md` and `brief_2a.md` in the pass's folder of `code-builders/pass-history`), its diff read and its games played in the Studio by the session.

### 7.1 Departures from the frame, first

Each decided under the user's word of 2026-10-15; each gets a backlog row.

- **The PF Check's +1 in August and September 1943 cannot be reached:** the PF's date bar refuses it before October 1943, and the PFk is not built. The modifier is in the calculator with no test.
- **HEAT's bar at Infantry reads a building, rubble, or a wall; no pillbox** (none is built). The wall's TEM is still not applied to the ordnance To Hit DR (an older gap, pass 100's, backlog section 59.1), so HEAT behind a wall takes no Case Q from the wall.
- **The page offers HEAT at Infantry whatever the target's terrain;** its note gives the bar, and Rules refuses the shot at a target in the open. The refusal's advice ("fire HE or AP at Infantry in the open") reads oddly for a PSK, which has neither.
- **The H#[9] HEAT of the German 37mm Guns** is in the table and no catalog Gun maps to it.
- **A non-20L ATR attacks only the Gun's crew** where `GunAt` finds a crewed Gun; at any other target it stays Small Arms and is refused as ordnance.

### 7.2 What is built

`ScenarioA1HeEquivalency` holds C8.31's table (R60.2). AP, APCR, APDS, and HEAT named at a target with no vehicle pass the old refusals (`gun-outside`, `he-equivalency-not-built`) where the Gun lists them at the date, run pass 55's Depletion Number steps, and resolve by the HE path at the equivalency FP; new refusals `heat-no-he` and `heat-target-outside`. A LATW on the Infantry Target Type: the PF's To Hit of 10 less 2 a hex, the PSK's table, the ATR's black row; no ROF or Acquisition; a PF's Original 12 Casualty-Reduces its firer; the PF Check's `not-afv` and `aug-sept-1943` modifiers; one unit by Random Selection, on the roll C3.74 uses. A Gun's fate under AP, APCR, APDS, or an ATR: a K destroys it; under HEAT a K malfunctions it. The page's Infantry ammunition list names each kind with its equivalency ("AP: 2 FP by HE Equivalency, C8.31"). Tests that asserted the old refusals were brought to the rule (`ScenarioA1Pass55RulesTests`, `BacklogPass55Tests`, `PlayPageOrdnanceTests`).

**New in a record:** `wallTem` on an ordnance shot (taken from the record at replay; absent on every older record, none of which was HEAT at Infantry); `heEquivalency` on its resolution (null for every shot the old rules allowed). Every new path was refused before the pass, so no recorded attack changes.

### 7.3 The games and the tests

In my Studio on port 6671, board 3, scripted dice; each game's file was read back.

| Game | What it shows |
|---|---|
| `p60-ap` | July 1942: a 6-Pounder with no HE yet fires AP at a squad in [D10]; the list says "AP: 2 FP by HE Equivalency"; a hit on 4,3; the 2 FP column on 2,1 is a 1MC; the squad breaks. Recorded. |
| `p60-psk` | July 1944: a PSK at a squad in the stone building [O5]: Case Q +3 on the To Hit DR, a hit on 2,2, the 12 FP column on 4,4, a 1MC, the squad pinned. Recorded. |
| `p60-psk-open` | CONTROL: the same PSK at a squad in the open: refused with C8.31's bar. |
| `p60-pf` | A PF at two squads in [O5]: Check dr 2, +1 not an AFV, a shot; a hit on 1,2 with +3; Random Selection takes one squad; the 16 FP column on 4,3, a 2MC; it breaks. Recorded. |
| `p60-atr-gun` | 1943: a Russian ATR at a German INF Gun in [D10]: AP at 1 FP; Case P and the Emplacement on the To Hit DR; on the IFT only the crew, with the gunshield's +2; no effect. Recorded. |

Run by the agent: Rules, the pass 60 class and the ordnance passes' classes, 202 passed; Play, 134 passed; MapStudio's ordnance page and record classes, 9 passed (logs in the working folder's `test-classes`). `ScenarioA1Pass60RulesTests` has 16 tests and `BacklogPass60Tests` 5.

## 8. The Specific Collateral Attack and mixed Locations: 60.1 (increment 2b)

Built on 2026-10-15 in one commit (4a628394) by a building agent to a brief (`brief_2b.md`), its diff read and its games played in the Studio by the session.

### 8.1 Departures from the frame, first

Each decided under the user's word of 2026-10-15; each gets a backlog row.

- **More unarmored vehicles in the Location than the column's highest KIA# refuses the shot** (`vehicles-beyond-kia`). A7.308's Random Selection of the vehicles affected is not built.
- **A PF's or an ATR's shot at a Location with a vehicle stays refused** unless the vehicle is named; the 20L ATR too.
- **In the MPh, Defensive First Fire at a Location where a vehicle moves with the Infantry stays refused,** and vehicles that are not moving are left out of the hit, as C3.4 has it for every non-moving unit. That includes a phasing vehicle's Bounding First Fire.
- **No Specific Collateral Attack after the crew Abandons on a failed Immobilization Task Check,** and none for a Gun's HEAT with no HE FP to step from. The attack applies to AFVs only: an unarmored vehicle's Vulnerable PRC on the Vehicle Target Type is pass 65's (with Passengers).
- **A CE AFV in terrain with a positive TEM is admitted for an ordnance hit only:** its crew takes +2 for CE alone (D5.31). The Fire package's refusal of it for other IFT attacks (ruling R25.6) stands.
- **A vehicle's own To Hit DRM** are Case K or N, Case P for its size, and the Location's terrain TEM as on the Vehicle Target Type: no Bypass distinction, and the C3.32 example's point that a shellhole's TEM is the Infantry's alone is not modelled.
- **A Critical Hit's Random Selection** takes units and unarmored vehicles into its pool, never an AFV.

### 8.2 What is built

`ScenarioA1ArmorCalculator.SpecificCollateral` makes D.8A's attack on the Original TK DR with +2 for CE; its result is `specificCollateral` in the resolution (FP, result, effect) and a record line on the page. The Infantry Target Type at a mixed Location (`ScenarioA1OrdnanceCalculator.Run`) judges target by target (`targetHits`), puts the unarmored vehicles hit on the Vehicle line with the units' one Effects DR, and attacks a CE AFV's crew when the AFV's own TH# is reached. `concealment-mixed` is gone. The IFT's crew outcome was moved into `CrewChecks` and `CrewOutcome`, which the IFT and the ordnance hit share; the moved body is the old one (read in the diff).

**New in a record:** `specificCollateral` on an ordnance shot (taken from the record; absent: no attack, and `vehicleTarget.crewExposed` is dropped from the state-built facts); `hit.vehicles` (built only outside the MPh for an Infantry Target Type shot, which was refused with any vehicle in the Location before); the roll `collateralCheck`; `targetHits` and `specificCollateral` in the resolution.

### 8.3 The games and the tests

Board 3, firer in [F10], target in [D10], scripted dice.

| Game | What it shows |
|---|---|
| `p60-coll` | A PzKpfw IIIH's HE at a CE T-34: a hull hit on 4,2; the TK DR 1,2 against a Final TK# of -5 does nothing; the crew takes 6 FP on that DR 3, +2 for CE, a 1MC, passed on 2,2. Recorded. |
| `p60-mixed` | A 45L's HE at a squad and a truck in [D10], no vehicle named: both hit on 3,3; one Effects DR 7 on the 4 FP column, a PTC for the squad (passed) and nothing for the truck on the Vehicle line (Kill number 5). Recorded. |
| `p60-mixed-ce` | The same at a squad beside a CE PzKpfw IIIH: the squad's PTC; the tank's crew takes the same DR with +2, Final 9, nothing. Recorded. |
| `p60-conc` | The same at a Known squad beside a concealed one, To Hit 4,4: the Known squad hit (Final 7), the concealed one missed with Case K (Final 9) and keeps its "?". Recorded. |

Run by the agent: Rules classes of the pass and the ordnance passes, 189 passed; Play, 139 passed, among them `AnOlderRecordOfAHitOnACeTankReplaysAsItWas`, which strips the pass's facts from a recorded hit and replays it; MapStudio, 3 passed.

## 9. Indirect Fire: 60.2 (increment 2c)

Built on 2026-10-15 in one commit (3ac44bea) by a building agent to a brief (`brief_2c.md`), with one change sent back, its diff read and its games played in the Studio by the session.

### 9.1 Departures from the frame, first

Each decided under the user's word of 2026-10-15; each gets a backlog row.

- **R60.23, correcting R60.10:** a mortar's hit on an AFV takes its hit Location from the Original IFT DR, not from the To Hit DR. My frame said the To Hit DR; the C9.5 example and the C7.7 chart's note E ("Indirect Fire uses the Original IFT DR for hit location") say the IFT DR. The agent built the frame's reading and was sent back.
- **No Immobilization Task Check (D5.5)** after a mortar immobilizes an AFV, as on the IFT's Vehicle line today.
- **An OT AFV hit by Air Bursts (D5.311)** goes on the Vehicle line with no Air Burst TEM, but its crew is not attacked: D5.311's Infantry-like crew (break, rout, no Stun or Recall) is not built.
- **Crew Survival's +1** reads Stunned, Recalled, or Stun +1 only; the vehicle's facts carry no Shocked or Abandoned.
- **A Critical Hit across Locations:** a tied Random Selection goes to the lowest Location (the agent's choice).
- **Still refused:** a vehicle in Bypass in the target hex, and a mortar's Defensive First Fire with a vehicle in the hex.
- **No entrenchment exists,** so Air Bursts with an entrenchment rest on an Emplaced Gun in woods (+2 -1). **The board data has no Interior Building Hex and no rooftop:** every building hex is taken as not Interior, and no rooftop is attacked.

### 9.2 What is built

`ScenarioA1AreaCalculator` was rewritten around Locations: each Location of the target hex with a target in the firer's LOS is judged on its own To Hit (Case K, N, P, R), and one Effects DR attacks each Location hit with its building TEM and `tem:levels-above` (B23.32); vehicles are judged with the units. An AFV is attacked on the IFT (`AfvAttack`): the AF and OT modifiers, the TEM but on a Critical Hit, a burning wreck, elimination with Crew Survival, Shock or Immobilization at and one above the K/# DR. An unarmored vehicle is on the Vehicle line, an OT AFV in woods under Air Bursts too. A Critical Hit picks a Location, then a unit. The Area Acquisition covers the hex. A mortar may fire at a crewed Gun: Emplacement on the Area hit, the gunshield's +1 against Indirect Fire where no other positive TEM applies, the Gun's fate. `critical-hit-tem:height-advantage` reverses Height Advantage on a Direct Fire Critical Hit. `area-friendly-units` has its sentence.

**Found outside the task and fixed:** the ordnance record check recomputed a target's `encircled` fact from the state; any recorded shot at an upper-level unit the planner had read as Encircled failed replay. It is now taken from the record, as fire records take it.

**New in a record:** on the shot `reversesHeightAdvantage`, `areaLocations` (each with its terrain, LOS, and levels above), `areaAcquisition`, and `afvCover` on a mortar's hit; on the resolution `levelHits` and `areaVehicles`; the roll `areaSurvival`. Each is taken from the record or absent from every older one: units on two levels and vehicles in a mortar's hex were refused before, a Critical Hit's reversal is gated, and the Area Acquisition's hex is gated since a later shot at another level of an acquired hex could be made before.

### 9.3 The games and the tests

Board 3, scripted dice; each game's file read back. The LOS from [K3] to both levels of [M2] was read on the board viewer first.

| Game | What it shows |
|---|---|
| `p60-mtr-afv` | A German 50mm mortar in [H10] at [D10], a Russian squad beside a BU T-34: both hit on 4,2; the squad on the 2 FP column with D9.3's +1 beside the AFV, a 1MC passed; the T-34 on the same IFT DR 3, one above the K/# DR, a turret hit by the IFT dice (colored 1 below white 2): Shocked. Recorded. |
| `p60-mtr-truck` | The same at a truck: Case P +1 for a small target; the Vehicle line, DR 3 below the Kill number 4: eliminated. Recorded. |
| `p60-mtr-levels` | The mortar in [K3] at [M2], a squad on each level: the ground level with the grain's Hindrance (Case R +1), both hit; one Effects DR 7, the ground +3 stone +2 levels above (12), level 1 +3 +1 (11): no effect. Recorded. |
| `p60-mtr-gun` | A Russian mortar at an Emplaced German INF Gun: Case P on the To Hit; the Emplacement's +2 taken over the gunshield's +1 (not cumulative, C11.2). Recorded. |

Run by the agent: Rules, ten classes, 246 passed; Play, 147 passed; MapStudio 13; Units 50. After the change sent back: the pass 60 and pass 9 classes, Rules 49 and Play 29 passed.

## 10. FT, MOL, and DC at an AFV: 60.3 (increment 2d)

Built on 2026-10-15 in one commit (5913d7bb) by a building agent to a brief (`brief_2d.md`), its diff read and its games played in the Studio by the session.

### 10.1 Departures from the frame, first

Each decided under the user's word of 2026-10-15; each gets a backlog row.

- **The Aerial AF is read from C7.11's row on p. 190** (a render): AF 0 and 1 give 0, 2 gives 1, 3 gives 2, 4 and 6 give 3, 8 and 11 give 4, over the worst AF of hull and turret, front and side (C7.12). Where the row leaves an AF blank (14, 18, 26), the lowest printed hull AF stands, as the brief said.
- **A DC's malfunction on its Position DR** is the existing A23.4 number (12 for an elite owner, lower for a non-elite or captured DC, A23.2), not a flat 12.
- **The DC's second DR** is drawn before the Position DR: it is the To Kill DR on a Final Position DR of 8 or less, the crew's new DR on 9 to 11, and the attack on the other units on 12 or more.
- **Naming an AFV in a Location that also holds concealed or hidden units or a Dummy is refused** (`target-vehicle-concealed`): the attack's record would name them to the firing side.
- **A Placed DC names its AFV when it is detonated,** and the facing is read then from the placement hex: a turret turned between the placement and the AFPh is not tracked. **A failed PAATC** pins the placer and writes no step.
- **Not reached or not built:** the Bypass -2 (a DC is never Placed or Thrown into its own Location); Case B only when the attack carries the target's level above; C7.346's IFT attack through an unarmored Target Facing (no AFV of the catalog has one); a DC Placed where an unarmored or friendly vehicle is stays refused.
- **R25.6's refusal of an AFV in positive TEM** is lifted only when the attack names the AFV; its crew then takes the CE DRM alone.
- **Found in the Studio, for the reviews:** the thrown DC's vehicle list names the tank by its id and definition ("rt (defender-tank)") where the other panels use the side's label; and the DC's record keeps an IFT arithmetic line (15 FP, "2MC") that attacks no one, since the Area Fire on the other units applies only on a Position DR of 12 or more.

### 10.2 What is built

`ScenarioA1FlameKill` holds C7.34's Basic TK# (DC 16, FT 8, MOL 6), the FT/MOL and DC columns of C7.7, the Position bands, and the Aerial AF. `ScenarioA1FireCalculator` gains the named vehicle's attack: `NamedVehicleAttack`, `Position`, `ChargeKill`, `FlameKill`, and the Specific Collateral Attack after each through 2b's member. The fire request, the throw, and the detonation take an optional `vehicle`; the page's fire panel (`#fire-vehicle`), the throw panel (`#throw-dc-vehicle`), and the detonation (`button.detonate-dc-vehicle`) offer an AFV the side sees in the target Location; the proposal states the TK# and its modifiers, and the record lists the Position DR and the To Kill DR (`ul.fire-named-vehicle`).

**New in a record:** `targetVehicle` (with `immobile`) on the fire attack's facts, absent from every older record, and every new branch is gated on it; the rolls `position`, `crewSurvival`, `taskCheck`, `collateralCheck`; `position`, `vehicleKill`, and `specificCollateral` in the resolution.

### 10.3 The games and the tests

Board 3, the attacker in [F9], the AFV in [F10] facing south-east, scripted dice; each game's file read back.

| Game | What it shows |
|---|---|
| `p60-ft-afv` | A Russian FT names a CE PzKpfw IIIH: TK# 8, +1 CE, +1 through the rear, no AF, Final 10; DR 5 burns it. Recorded. |
| `p60-mol-afv` | A Russian MOL at a CE PzKpfw IIIH beside a German squad: the Check passes on 1; DR 7 under Final TK# 8 (6, +1 CE, +1 rear) burns it; the squad takes the 8 FP of Point Blank Fire alone, D9.3's +1, an NMC passed. Recorded. |
| `p60-dc-afv` | A German DC thrown at a BU T-34: Position 4, +2 thrown, -1 hull rear, Final 5, the Aerial AF 3; TK# 16 +1 rear -3 = 14; DR 5 burns it; the thrower's own Location at +3 on its own DR, a 1MC and a pin. Recorded. |

Run by the agent: Rules, the pass 60 class 40 passed and the fire and ordnance classes 202; Play, 190; MapStudio, 45; after the last edit, Play 101 and Rules 55, all passed.

## 11. Guns as targets, and Random SW and Gun Destruction: 60.5 (increment 2e)

Built on 2026-10-15 in one commit (24620722) by a building agent to a brief (`brief_2e.md`), its diff read and its games played in the Studio by the session. AP, HEAT, and an ATR at a Gun (C11.52, C13.23) were built in 2a, and a mortar's hit on a Gun with the gunshield's +1 in 2c.

### 11.1 Departures from the frame, first

Each decided under the user's word of 2026-10-15; each gets a backlog row.

- **No Gun is Emplaced on a paved road or a bridge: not built.** The map marks both, but the ordnance record's check recomputes Emplacement from the state, which has no map; the bar needs a recorded map fact.
- **An unattended Gun on the Area Target Type: not built.** The Area calculator judges unit by unit and has no place for a Gun with no crew; a mortar's shot at one stays `target-outside`. The Infantry Target Type is built.
- **An OVR's KIA makes no A9.74 dr at all.** The frame put only the OVR's -1 and its end-of-MPh check in the backlog; A9.74 without the -1 would be wrong for a fully-tracked AFV, so the fact is left off OVR attacks.
- **A DC at a manned Gun** reads the Gun's fate from the result its manning unit's group took; C11.4's Random Selection with the Gun counted among the targets is not modelled.
- **A Critical Hit on an unattended Gun destroys it with no IFT DR.**
- **The test of a non-crew manning ending Emplacement is a Play test,** since the rule lives in the projector's state and in `LiveOrdnance.Emplaced`.
- **For the reviews (hidden information):** the weapons a target holds ride in the fire facts. They are withheld with the record where it hides identity, but a pending choice's resume, which both sides read, and an ordnance shot's `hit.targets` can name a concealed unit's SW; those records already named the unit.

**What a DC did to a Gun before the pass:** it attacked the crew on the IFT and never the Gun; a crew eliminated left the Gun intact.

### 11.2 What is built

`ScenarioA1GunDestruction` holds C11.6 by column (ordnance, small arms, DC) and A9.74's dr. The fire calculator keeps each target's result and column and, for each unit `eliminated-kia`, rolls each held SW's and Gun's dr (`weaponDestruction:<id>`); the Gun an ordnance hit targets is left to C11.4. The planner writes the weapon's `instance-eliminated` or malfunction before the unit's own events, so it is gone before the projector drops what the unit held. `GamePlanner.UnattendedGunAt` gives the Infantry Target Type an unmanned Gun; `GameState.NonCrewMannedGuns` ends a Gun's Emplacement once a non-crew unit mans it. The record lists each dr (`ul.fire-weapon-destruction`) and a Gun's fate ("a Direct Hit, destroyed (C11.4)"), which older games now show too.

**New in a record:** `randomDestruction` on a fire attack and an ordnance shot, taken from the record (absent: no dr, as before); `heldWeapons` on a target and the rolls `weaponDestruction:<id>`, read only with the fact; `weaponDestruction` in the resolution. `AnOlderRecordOfAKiaOnASquadHoldingALmgReplaysAsItWas` strips the fact from a recorded KIA and replays it with the LMG dropped intact.

### 11.3 The games and the tests

| Game | What it shows |
|---|---|
| `p60-random-sw` | Two Russian squads in [D9] at a German squad holding a LMG in [D10]: 16 FP, DR 3, a 1KIA; the LMG's dr 3 on the 16 column is a KIA, and the LMG is destroyed before the squad is eliminated. Recorded. |
| `p60-unattended` | A German INF Gun at an unattended Russian 45L in [D10]: the target list offers the Location; Case P on the To Hit DR; the 12 FP column on 1,2 is a K/3: the Gun malfunctions. Recorded. |

Run by the agent: Rules, the pass 60 class 46 passed and the fire, ordnance, and older classes 334 of 335 (the one its own new test, corrected and run again); Play, 307; Units whole, 416; MapStudio, 33.

## 12. A MG's To Kill attack on an AFV: 60.6, with 50.5 (increment 2f)

Built on 2026-10-15 in one commit (6bfead84) by a building agent to a brief (`brief_2f.md`), its diff read and its games played in the Studio by the session.

### 12.1 Departures from the frame, first

Each decided under the user's word of 2026-10-15; each gets a backlog row.

- **The upper superstructure of a non-turreted AFV (C3.9, ruling R60.22) is not built:** it moves to pass 105, as the ruling allowed.
- **No Collateral Attack after a MG's Stun:** D5.34 buttons the crew up at once, which leaves no Vulnerable crew, as R60.7 has it for a Shock; the D1.22 example reads the same way ("fails to destroy or Stun").
- **The Bren Carrier's MA prints no FP:** it fires at the British LMG's 2 FP with a Normal Range of 8, like the other vehicle MGs; the catalog is unchanged. A manufactured value under R0.3 would change the catalog; backlog.
- **A MG fired as ordnance is no Gun of a size:** it takes no C4 To Hit modification, fires on the plain black Vehicle row, and is read as 25mm or less for Case D. It acquires nothing. Inexperienced Personnel lower its B# by one (A19.32).
- **Refused:** the AFPh (backlog), Bounding First Fire, a pinned possessor, a lone leader or hero (A9.12), a vehicle in Motion or BU firing its MA MG, Intensive Fire, a directing leader, and an unarmored vehicle (it stays on the IFT's Vehicle line).
- **Not built:** a 1MT AFV's Recall on a Stun; the AAMG's all-round arc against a bow MG's VCA (no Case A charged, no arc enforced); the MG's Residual FP. No catalog MG is 12.7mm or 15mm, so the Basic TK# is always 4.
- **The CE crew's Collateral Attack at a FP between columns** is read on the column at or below it (A7.3): a 3 FP LMG on the 2 column.
- **A9.223 is the narrow reading (R60.20):** a reaction CC attack cancels the Fire Lanes its attackers man; D7.21's CC counter marks the attacker's SW and Guns (`asl:cc-reaction`) and the fire path bars a marked SW. The cancellation is not gated: none of the three live games with a Fire Lane holds a vehicle Close Combat.
- **Found at the end of the build, for the fixes:** `PlayPageVehicleTests.ATankIsOfferedInTheGunPanelWithTheEnemyVehicleAndItsAmmunition` fails: it expects no ammunition list at Infantry, which 2a changed (a tank's AP by HE Equivalency), and 2a's own page run did not include the class. **A rule slip:** the agent ran one multi-line `python -c`, which the hook let through.

### 12.2 What is built

`ScenarioA1MachineGunKill` holds the MG's attack: who may make it (`Outside`, `VehicleWeaponRefusal` for D3.54), Basic TK# 4, and its results. A possessed `asl:mg` and a vehicle's MA MG ride `asl.game.fire-ordnance` with a named AFV and the ammunition `ap`; the To Kill DR adds Case A and Case D's 25mm row, less the AF; at most half burns, below eliminates, equal Stuns (`stun`: Stunned, BU, out of Motion). The vehicle fire path reads the MA `mg`. The page's `#ordnance-gun` offers a possessed MG and a MG-armed vehicle while an enemy vehicle is on the map ("at an AFV only (A9.61)"), with a fixed ammunition text; the record reads "a MG To Kill attack (A9.61)" and "Stunned, BU (D5.34)".

**New in a record:** none; a MG's shot is an ordinary `ordnance-fired` with the result `stun`, and a weapon may carry `asl:cc-reaction`. Every new path was refused before the pass.

### 12.3 The games and the tests

| Game | What it shows |
|---|---|
| `p60-mg-stun` | A Russian HMG in [F10] at a CE SPW 251/1 in [D10] facing it: a hull hit on 4,2; TK# 4, +1 Case D at range 2, less AF 1, Final 4; DR 4 Stuns it and buttons it up. Recorded. |
| `p60-mg-coll` | A German LMG at a CE T-34: Final TK# -6, DR 7, nothing; the crew takes the LMG's 3 FP on the same DR, +2 CE, Final 9, nothing. Recorded. |

Run by the agent: Rules, the pass 60 class 53 passed and the related classes 203; Play, the pass 60 class 22 and related 215; MapStudio 33 passed and the one failure above.

## 13. The five reviews, their fixes, and the Studio check

Five read-only reviews ran in parallel on 6bfead84 (briefs in `brief_reviews.md`): two referees (60.1, 60.2, 60.4; and 60.3, 60.5, 60.6 with 50.5), a table player, a UI and Blazor review, and a Rules boundary review. Each finding was judged against its page and the code; the fix lists are `fixes_boundary.md`, `fixes_referee1.md`, `fixes_referee2.md`, `fixes_table.md`, and `fixes_ui.md` in the pass's folder, and one agent applied them in one commit (3bf7ec8e).

### 13.1 What departs from the plan's entry and the frame, first

- **Three hidden-information leaks, graded MAJOR, all fixed.** The Gun panel offered a side's MGs once any enemy vehicle existed, seen or not, so their appearance told the side of an unseen vehicle; the weapons a target holds (A9.74's facts) were serialized into records and resumes both sides read, a concealed unit's included; and the proposal, the record, and a refusal of 2b named hidden (HIP) units and counted the units under a "?".
- **Two rulings of the frame were wrong on the page and are amended:** R60.24 amends R60.11 (C3.33, p. 169: a mortar's hit also attacks the hex's units out of its LOS when it hit the hardest-to-hit target in its LOS); R60.26 amends R60.18 (C11.3, p. 182: only a Gun that starts the scenario manned by a non-crew unit loses its Emplacement; a later non-crew manning only withholds the +2, C11.2). Two more rulings were made from the findings: R60.25 (a Stun the hit causes buttons the crew up, D5.34, so no Collateral Attack follows) and R60.27 (a MG's To Kill attack at a concealed AFV is refused).
- **The Studio check of the whole pass was narrower than the standing rules' step 4,** at the user's word of 2026-10-15 ("Run the narrower check for this Pass. From now on, let's reassess the need ... on a case-by-case basis"); section 13.4.

### 13.2 Fixed

- **Rules:** a MG's To Hit takes C4.2's modifications beyond 12 hexes and no Case E in its own hex (C5.5 is a Gun's); a Critical Hit that Random Selection puts on a truck no longer destroys the Gun beside it (C3.74); a Gun crew's gunshield and Emplacement DRM no longer reach the vehicles of its Location (C11.5); a Critical Hit on an unarmored vehicle reverses its positive TEM (C3.71); a PF's or PSK's Original 2 is a Critical Hit outright on its own table (C3.7); a non-20mm ATR gives no Specific Collateral Attack (C8.31 note 1); a Placed DC keeps the facings it was Placed through (A23.5); R60.24, R60.26, R60.27.
- **Records (R60.3):** `heldWeapons` is never recorded (re-read from the state at replay and on a resume); the Fire Lane's cancellation by CC Reaction Fire is gated by `cancelsFireLanes` on the reaction's record; `areaAcquisition` is held to the state, not copied from the record; new nested members are left out when null.
- **Into Rules:** C11.3's predicate, a target's needed Original DR, Area Acquisition's hex, the unattended Gun, the DC Placement bar, A9.74's scope.
- **The page and the texts:** the Gun panel reads the side's own view; the DC lists use the side's labels; stale AFV choices are cleared; the MG waits for a named AFV ("Name the AFV (A9.61)") and carries Rules' reason when it cannot fire; refusal advice by weapon; sentences for the MG's refusals; an IFT line that attacked no one is hidden; the PF's Random Selection, the unattended Gun, the MG's kill and malfunction, the Stun's missing Collateral Attack, and the DC's second DR are worded; `PlayPagePass60Tests` (six page tests, two text tests); `PlayPageVehicleTests` brought to 2a's ammunition list.
- **The fixing agent's own departures:** the Fire Lane gate is a flag on the reaction's record, not the weapon's CC counter, which a reaction that destroys the vehicle never places; the gunshield fix is gated by `randomDestruction`, since an IFT attack on a crew beside a vehicle could be made before the pass; a PF's or PSK's Critical Hit on a Final DR below half the Modified TH# still stands; a concealed unit's record line keeps its Case K (pass 9's form), only a concealed vehicle's DRM and Final DR are hidden; the MG stays in the Gun list with its reason rather than leaving it.

The agent's run: the build exit 0; Rules, fifteen classes, 375 passed; Play 340 passed; MapStudio 48; Units whole 416. The replay digest after the commit (41 s): no line of 20,557 differs; the pass's 22 own games replay to their end (`p60-random-sw`, which recorded `heldWeapons`, was labelled SUPERSEDED and played again as `p60-random-sw2`).

### 13.3 Not fixed: a row each in the backlog

The pass 55 page test of HE refused after it ran out (the Gun now offers AP instead); three rule conditions decided in the page (an AAMG MA fires only CE, a plain MA MG's 8-hex Normal Range, when HE is left unnamed), extensions of passes 50 and 55; `#propose-throw-dc` disabled with no title; C11.3 gated in the verifier by `randomDestruction`'s presence; `vehicles-beyond-kia` refusing a mortar's shot that C1.55 only limits; a tie between Locations in a Critical Hit's selection; an OVR's KIA with no A9.74 dr, where a vehicle not fully-tracked rolls a plain one; D3.53's unhalved AFPh To Kill attack by a vehicle's MA MG.

### 13.4 The Studio check

The narrower check the user approved, in my Studio on port 6671, on the build of 3bf7ec8e:

- **The unseen vehicle** (`p60-unseen-afv`, a German LMG and a hidden T-34 far off): the German view offers no Gun panel and names no vehicle.
- **Hidden and concealed units** (`p60-hidden`, a Russian 45L at a Known squad beside a hidden squad and a concealed one holding a LMG): the Russian proposal names the Known squad and "its concealed units" with no count; the committed record names the Known squad and "a concealed unit", neither the hidden squad nor any weapon; the game's file holds no `heldWeapons`.
- **The new controls** (`p60-check-controls`, a German squad with a DC and a LMG beside a CE T-34): at 1920 the throw panel's list reads "vehicle R1" and the MG's propose button waits with "Name the AFV (A9.61)."; at 1024 and at 320 the throw list, the vehicle list, and the propose button sit inside the width in the Actions tab, with no horizontal overflow; at 320 the MG's To Kill attack was proposed and committed by real Enter keys on the focused buttons, and the focus moved to the Actions panel after the commit, as under 1024 it should.

## 14. As built

**The pass in commits,** on `feature/asl-backlog-pass-60`, cut from 380b4a7b: the frame (38bd4133); 60.4 (afdcb2b5); 60.1 (4a628394); 60.2 (3ac44bea); 60.3 (5913d7bb); 60.5 (24620722); 60.6 with 50.5 (6bfead84); the reviews' fixes (3bf7ec8e); the strict build's two style fixes and the documents (the branch's head). Each task was built by an agent to a brief, its diff read, its games played in the Studio by the session, and the replay digest of the older games run after its commit with the gate's own skip list: no line of 20,557 differed after any of the six, nor after the fixes, and the pass's own games replayed to their end each time.

**What the plan's entry asked, and where it went.** Of the 49 inventory rows: 60.1 to 60.5 are built as sections 7 to 11 say, each with its departures; 60.6 is built for the MG's To Kill attack (with 50.5) and Case A and the MG column of C7.7; the upper superstructure (C3.9) moved to pass 105 and A5.132 to pass 110; C7.7's note A waits for a catalog that colors the Crew Survival number. The backlog's section 60 has a row for every departure and every finding not fixed.

**The rulings:** R60.1 to R60.27, decided under the user's word of 2026-10-15 and open to a veto, with three of the frame's corrected on the page: R60.10 by R60.23, R60.11 by R60.24, R60.18 by R60.26. They are in the Passes Plan's section 5.

**The tables** (ruling R60.2) are Rules code, each with its page: `ScenarioA1HeEquivalency` (C8.31), `ScenarioA1FlameKill` (C7.34, the C7.7 DC and FT/MOL columns, C7.11's Aerial AF, C7.346's bands), `ScenarioA1GunDestruction` (C11.6, A9.74), `ScenarioA1MachineGunKill` (A9.61, D3.54). No transcription, case matrix, package, or catalog changed; the packages were not pinned again.

**What a record gained** (each absent from every older record or taken from the record at replay, ruling R60.3): on an ordnance shot `wallTem`, `specificCollateral`, `reversesHeightAdvantage`, `areaLocations`, `areaAcquisition` (held to the state), `afvCover`, `randomDestruction`, `hit.vehicles`; on a fire attack `targetVehicle`, `randomDestruction`; on a DC's placement `dcFacings`; on a vehicle Close Combat `cancelsFireLanes`; in resolutions `heEquivalency`, `specificCollateral`, `targetHits`, `levelHits`, `areaVehicles`, `position`, `vehicleKill`, `weaponDestruction`; new rolls `collateralCheck`, `areaSurvival`, `weaponDestruction:<id>`, `position`, `taskCheck`; a new result `stun`; a weapon's `asl:cc-reaction`. No new event type, action, or choice kind.

**The time:** the frame 0:13, the build 3:50 (10:33 to 14:23) against 6:40 estimated and the plan's 4:40, the reviews and their fixes 0:51, the Studio check and the documents after them; the time log has each row.

**The strict build** (`gate.sh 60 380b4a7b strict`): the first run failed on six CA1861 errors in two of the pass's test files (constant arrays passed inline), fixed by static fields (`fix_ca1861.py`); the second run's result is in the working folder's `strict-60b.log`, and `strict-check` says whether it holds for the tree as committed.

## 15. The gate

Not run: it is the user's to start by name (`baseline`, `build`, `proofs`, then the push and the pull request, and CI's run of it). `pass60.conf` names the pass's 23 recorded games (`p60-random-sw` left out, SUPERSEDED). The baseline's cache key holds the list of the pass's own games, which was empty at the cut, so the `baseline` stage makes the baseline of the older games again once (about eight minutes) before its own part.

What the gate should show, from the build: the text list differs in the pass's new texts and the changed refusal sentences; the replay digest of the older games differs in nothing (as after each task), and the pass's own games are new lines; the sweep may show refusals turning into plans in older games where a pass 60 refusal stood (a Location with a vehicle on the Infantry Target Type, a mortar's hex with a vehicle or several levels, AP or HEAT at Infantry, an unattended Gun): each such line is read as 60.1, 60.2, 60.4, or 60.5's intended change.
