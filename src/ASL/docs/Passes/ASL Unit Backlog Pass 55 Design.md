# ASL Unit Backlog Pass 55 Design

**Status:** The frame, written on 2026-10-13 under the user's word of 2026-10-12 for this pass ("I want you to run without my intervention up to the Gate"). The frame's questions were put to three read-only agents, as that word asks; their answers were judged against the pages and the code, and each decision below is a ruling decided under that word and open to a veto. Nothing is built yet.

**Date:** 2026-10-13

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 22.1, "Pass 55: Armored combat II"; the [rule inventory](<../ASL Rule Inventory A to E.md>); the [backlog](<../ASL Unit Backlog.md>), sections 55 to 58; the [pass 50 design](<ASL Unit Backlog Pass 50 Design.md>), sections 6 and 10 to 12; the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>); the session's prompt, `Prompts/Pass 55 Prompt I.md`.

**The rulebook.** Read for this frame on 2026-10-13, from the page texts of the tools folder, with their examples: page 125 (B10.3, B10.31), page 167 and page 168 (C2.21, C2.24, C2.2401 with its example, C2.6), page 171 (C4.3), page 176 (C7.32, C7.321 and the two examples above them), pages 177 to 179 (C8 to C8.92, with the Supply Chart, the Canister table, and the example's caption), page 198 (D2.35, D2.4 to D2.51), page 200 (D3.31 to D3.5 and the long example), and page 207 (D7.16). The whole book was scanned for Canister, APDS, Armor Leader, Motion attempt, Gun Duel, Inexperienced Crew, and Height Advantage (`pass55/term_scan.py`, all 716 pages); every mention of Canister and of an Armor Leader in chapters A to D was read in its place. The agents read on their pages, and I did not: C5.3 to C5.35 (p. 172), C6.8 (p. 175), C7.22 (p. 175), D4.22 (p. 202), D5.34 and D5.341 (pp. 203, 204), D8.3 (p. 209), the To Hit chart (p. 189), the To Kill chart (p. 190), the British and American Ordnance Listings (pp. 392, 426) with their notes, and the National Capabilities Chart (p. 695). Each task reads its own pages again before it builds.

**What the frame rests on.** One read-only agent mapped the code on this branch (cut from main at 846eb5a3); its map is `pass55/codemap.md` in the tools folder. Three more answered the frame's questions, each with the rule and the code behind its answer: the ammunition and the counters; Canister and fire at another level; Motion attempts, Armor Leaders, and Gun Duels. Where two of them disagreed (the Canister counter; how an Armor Leader is held) the decision and its reason are in section 6.

## 1. Outline of this design

1. This outline.
2. What the pass is, what departs from the plan's entry, and its increments.
3. The 35 rows, by task.
4. What is certain, and what needs a read.
5. What the frame found outside the rows.
6. The frame's questions, each with its analysis and its ruling.

Each increment then gets a section of its own when it is built, and the pass ends with the reviews, the Studio check, and "as built".

## 2. What the pass is

Pass 55 is the second pass of block 2, armored combat: the ammunition a Gun may run out of, Canister, a DEFENDER's vehicle that goes into Motion, ordnance and vehicle fire between levels, Armor Leaders, and, carried in from pass 50, Gun Duels.

**What departs from the plan's entry, first** (each decided under the user's word of 2026-10-12):

- **The pass is about twice the plan's size.** The plan gives it six tasks, 20 rows, 3:40 of building, and 4:55 in all. The inventory names 35 rows, the prompt adds Gun Duels as a seventh task, and the estimates made from the code are 9:50 of building (section 6, F9). The pass stays one pass and is built in the order of section 2's table, Gun Duels last, so that whatever a session's end leaves out is the task that was carried in, not one of the plan's own (ruling R55.1).
- **Gun Duels (C2.2401) is built as a slice:** ordnance on both sides, the case where the vehicle's Gun Duel DRM is lower, the first duel only. The case of equal DRM, in which the Final DRs decide the order, is left out, and so the ATTACKER is denied a duel the rule allows there (F8).
- **No Armor Leader counter is added to the catalog.** The plan names "Armor Leaders 8-1 to 10-2" among the pass's counters; D3.41 keeps the counter out of play and notes the leader on a side record, so he is held as a morale and a leadership on his vehicle (F7).
- **The Canister counter is a Gun, not an AFV,** so the halvings for Bounding First Fire and Motion (C8.4, D3.31, D2.42) are in the calculator and its tests but in no game (F2).
- **The counters need two crews as well,** a British and an American one: a Gun is fired by a crew of its own nationality, and the catalog has only the German and the Russian crew.
- **The catalog goes to version 1.14.0** (F2).
- **Fifteen rows that earlier passes moved here are moved on,** each with its backlog row: section 3 says where.

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 2a | 55.1 (first part), 55.6 | The Depletion Number's notation with a start month; the four definitions, the catalog at 1.14.0, the To Hit color of a British and an American Gun. | 1:25 |
| 2b | 55.4 | Ordnance and a vehicle's MGs fire at another level; Height Advantage on the To Hit DR. | 1:10 |
| 2c | 55.1 (the rest) | APDS on the tables; an HE Depletion Number on the Infantry and Area Target Types; Elite; the page's ammunition choice. | 1:05 |
| 2d | 55.2 | Canister. | 2:15 |
| 2e | 55.5 | Armor Leaders and Inexperienced Crews. | 1:15 |
| 2f | 55.3 | Motion attempts, and the DEFENDER's Motion firer. | 1:10 |
| 2g | 55.7 | Gun Duels, the slice. | 1:30 |
| | Overhead (the plan's) | The reviews, the Studio check, the documents. | 1:15 |
| | **Total** | build 9:50 | **11:05** |

## 3. The 35 rows, by task

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 55.1 | C4.3 (171) | The Basic TH# of APDS lessened as APCR's is; the 28LL and 40LL | 55.1 for APDS. The 28LL and 40LL have no counter: pass 110. |
| 55.1 | C7.32 (176) | The APDS listings of the To Kill Table and its Case D row | 55.1 |
| 55.1 | C8.11 (177) | APDS (D) by the scenario's date | 55.1 |
| 55.1 | C8.12 (177) | The Supply Chart; a parenthesized start month | 55.1: the month in the counter's notation; the chart as a test's data, not a rule the game reads (F3). |
| 55.1 | C8.2 (178) | Elite: one more on the Depletion Number | 55.1, by a special rule of the game. The DYO Majority Squad Type: the DYO passes. |
| 55.1 | C8.8 (179) | AP# and HE# | 55.1: HE# on the 6-Pounder; AP# by the same code and a Rules test, no counter. AP fired at Infantry with its HE Equivalency (C8.31): pass 60. |
| 55.2 | C8.4, C8.41, C8.42 (178) | Canister | 55.2. The building option, an aim at another elevation, the SMOKE and LV clause, pillboxes, Infantry that mans a Gun in the MPh: backlog. An AFV with Canister: pass 105. |
| 55.2 | D3.31 (200) | Canister halved in Bounding First Fire | In the calculator; no game (the counter is a Gun). The Gyrostabilized CMG: pass 105. |
| 55.3 | D2.401 (198) | The Motion attempt | 55.3. A stall: pass 105 (the code has none at the Start MP either). |
| 55.3 | D2.11 (195) | A VCA changed after a Motion Attempt | 55.3. By firing outside the CA, or at the end of a fire phase: pass 105. |
| 55.3 | D3.12 (199) | The TCA changed freely on gaining Motion | 55.3. With a Start MP, doubled in woods, at the end of a fire phase, turning with the hull: pass 105. |
| 55.3 | C5.35 (172) | A Motion firer of the side that is not phasing | 55.3 (F5). A Gyrostabilizer: pass 105. Guns en portee: pass 175. |
| 55.4 | C2.6 (168) | Fire at another level by the range against the elevation difference | 55.4. The AA exemption: pass 105. A Gun in a building hex firing within its hex: stays refused, pass 140. |
| 55.4 | B10.3, B10.31 (125) | Height Advantage for ordnance and vehicle fire | 55.4. The rows' note of a fault in the rout's Open Ground test is stale: pass 35 repaired it (task 35.4); the rows are corrected. |
| 55.5 | D3.4 to D3.44 (200) | Armor Leaders | 55.5. The halftrack's FG with its Passengers (D6.64, D6.65): pass 65. The HD Maneuver dr: pass 105. |
| 55.5 | D3.45 (200) | Inexperienced Crews | 55.5 |
| 55.5 | D7.16 (207) | Leadership in an OVR | 55.5 for the Armor Leader. A Passenger leader: pass 65. |
| 55.7 | C2.2401 (167) | Gun Duels | 55.7, the slice (F8). The equal case, fire that is not ordnance, the second duel, C5.33's duel declared by the DEFENDER: pass 110. |
| 55.7 | C5.33 (172) | The Gun Duel clause | Pass 110, with the rest of the duel. |
| moved | A5.132 (52) | A hit on another vehicle of an overstacked hex | Pass 60 (task 60.6). |
| moved | C3.22 (169) | A tank's turret or VCA changed without fire | Pass 105. |
| moved | C6.1 (173) | Case J against a vehicle that has paid only its Start MP; VBM as a cause | Pass 105. |
| moved | D1.81 (195) | A BMG fired by a pivot of the hull; a hull RMG; a Fixed Mount | Pass 105. |
| moved | D2.18 (196) | A declared higher MP expenditure | Pass 105. |
| moved | D2.35 (198) | The TCA in Bypass (D2.321) | Pass 105. |
| moved | D3.11 (199) | The VCA changed for a shot | Pass 105. |
| moved | D3.51, D3.52 (201) | The Known-target condition; the BMG's NT penalty | Pass 105. |

The table groups the 35 rows by rule; the documents step sets each row's own note and pass.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **A Depletion Number is read for APCR and HEAT only, and only on the Vehicle Target Type** (`ScenarioA1ArmorCalculator.Depletion`). Its notation is a letter, a number, and a year digit (`A4^1`); a month would make the parser throw. The Infantry and Area Target Types have no step at which a shot can turn out to have had no such round.
- **The To Kill tables' transcription already holds the APDS listings** (57L 18, 76LL 25) and the APDS Case D row; C4.3's To Hit modification is one row for both kinds.
- **Every ordnance shot between levels is refused** as "not reviewed", though the range against the elevation difference is computed; so is a vehicle's MG fire. Height Advantage is built for Infantry fire on the IFT and is computed for the ordnance shot's probe, then dropped.
- **A vehicle goes into Motion only at the end of its own move.** A DEFENDER's vehicle in Motion does not fire its MA at all.
- **No leader touches a vehicle.** A vehicle's crew has the morale of its nationality's best elite MMC, read in two places (the Shock and Immobilization checks; the CE crew's MC).
- **A resolution can already wait for the other side.** A `choice-pending` holds the game until `asl.game.choose` answers it, and an ordnance shot already stops on one and resumes. The planner replays the events it has just planned and plans on against the new state, in 22 places. Pass 50's frame weighed neither.
- **A Gun is fired by a crew of its own nationality,** and the To Hit numbers' color is German black, Russian red, and red for everyone else, which is wrong for a British Gun (black) and for an American one from 1944 (black).

**Needs a read at its task.**

- 55.6: whether a replay compares the package digest an event names, since the four digests change with the catalog; whether the gate's replay digest prints the catalog's identity.
- 55.4: whether a Gun's shot across a wall gets the wall's TEM today (an agent saw the probe drop the hexside TEM as it drops Height Advantage; not verified); the Area calculator's Hindrance line.
- 55.2: whether the ordnance record's check accepts a record with no To Hit; how a pending choice in one Location of a Spraying Fire attack resumes the rest.
- 55.5: whether a card's order of battle can give a vehicle a condition; how the Victory calculator counts a leader.
- 55.3: whether the projector's handling of a Bog Check and a Mechanical Reliability DR assumes the phasing mover.
- 55.7: how `PlanFireOrdnance` splits so that the vehicle's possible shot can be planned inside the DEFENDER's proposal.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and for the absent-safety of each): on an ordnance shot, Elite, Height Advantage, the Canister block, the firer's Armor Leader modifier, and the target crew's morale; on a fire attack, the Canister fact and the Armor Leader modifier; on a vehicle Close Combat attack, the modifier; on a unit, the Armor Leader; in the state, the Motion attempts of the phase. New event types: `motion-attempted`, and the Armor Leader's assignment if it is an event of its own. A new choice kind: `gun-duel`.

## 5. What the frame found outside the rows

1. **The To Hit color of a nationality** (C3.3; the National Capabilities Chart, p. 695) is wrong for the two new Guns as the code stands. It is repaired with the counters: no British or American ordnance has fired before, so no record changes.
2. **A mortar firing down is not held to C2.6,** which exempts SW but not "MTR/INF/RCL ordnance SW": the SW's map facts say the elevation is always allowed. Repaired in 55.4; a mortar's shot between levels was refused before, so no record changes.
3. **A vehicle's MG at Point Blank Range doubles with no test of the level** (it could not matter while levels were refused). Repaired in 55.4.
4. **Case J4 reads Open Ground without Height Advantage,** which B10.31 says bars FFMO. Repaired in 55.4.
5. **Ruling R7.6** ("the Elite forces' higher Depletion Numbers are not built") is superseded by 55.1.
6. **A stall** (the Russian red-MP AFV's Original 11 at a Start MP; Chapter H, Russian Vehicle Note M) is not in the code at all. Not this pass's: a backlog row, pass 105.
7. **The inventory's notes on B10.3 and B10.31** call the rout's Open Ground test a fault; pass 35 repaired it. A correction of the two rows.

## 6. The frame's questions

Each was put to a read-only agent with a proposal, and is answered here by a ruling decided under the user's word of 2026-10-12. Rulings made at a task are numbered on from these.

**F1. The rows.** The inventory lists 35 rows for the pass and the plan 20. **R55.2:** section 3's placement. What is moved on goes to the pass whose ground it is: A5.132 to pass 60, the rest of a vehicle's handling of its weapons and arcs to pass 105, the duel's remainder to pass 110 beside OVR Prevention.

**F2. The counters.** The agents disagreed on Canister: one proposed the American M3A1 light tank, which the book's own figure uses and which would play the Bounding First Fire and Motion halvings; the other the M3A1 37mm AT Gun. The tank's listing row was not found by either, and a vehicle is about 45 transcription rows against a Gun's 26. **R55.3:** four definitions, every value printed in the book, none manufactured:

- `british-at-gun`, the OQF 6-Pounder (British Ordnance Listing, p. 426, and its note 6): AT, 57L, ROF 3, B12, range 225, M10, a Small Target, NT, QSU, no HE but by its Depletion Number, "HE7" from February 1943 and "HE8" from 1944, "D6" from June 1944 in the ETO and "D7" in 1945. One Gun shows APDS, a start month, and an HE Depletion Number.
- `american-at-gun`, the M3A1 37mm AT Gun (U.S. Ordnance Listing, p. 392; Ordnance Note C, p. 395): AT, 37LL, ROF 2, B12, range 292, M12, a Small Target, NT, QSU, Canister "C7" from August 1942, 12 FP.
- `british-crew` (2-2-8) and `american-crew` (2-2-7), from the National Capabilities Chart (p. 695).

The catalog becomes `asl-scenario-a1@1.14.0`. The tests, the cards, the fixtures, and the proof store that name 1.13.0 are left as they are: a game reads the loaded catalog of the name it records, and editing a card would change its digest and mark every recorded game's provenance as changed. No counter prints AP# short of the 155 mm Guns; a Rules test with a made-up definition covers it.

**F3. Special ammunition.** **R55.4:** the notation is `<type><number>[^<year digit>[m<month>][+][E]]`, with the types A, D, H, HE, AP, and C; the book's month letters are ambiguous (J, A), so the transcription gives the month as a number and keeps the printed form in its note. `+` is "from that year on". `E` (the ETO only) is kept and not read: the game has no theater, every game is taken as the ETO, and the PTO's three more on a Canister or HE number is never added. **R55.5:** the Supply Chart of C8.12 is not a rule the game reads. C8.11 has the availability "listed on the back of each applicable counter" and C8.12 lets a counter's own number differ; no counter prints a bare letter for the chart to fill. The chart is a test's data: each catalog counter with A or D agrees with it or the test names the difference. **R55.6:** Elite (C8.2) is a special rule of the game, `elite-ammunition:<side>`, which a card's rule can carry; the shot records it, and a record without it is not Elite. No card is changed: Guards Counterattack has no ordnance, and The Gambit already says its forces are not Elite for C8.2. It supersedes R7.6. **R55.7:** C8.9's three outcomes apply to every ammunition with a Depletion Number, on all three Target Types. An Original To Hit DR above the number is "not to have fired yet": the ROF is kept, no fire counter is placed, the Acquisition stands, and nothing of concealment is lost, as APCR has it today; a malfunction on that DR still counts. A Gun whose HE has a Depletion Number never fires HE undeclared (C2.21): at Infantry it must name `he`, and when its HE is gone, or was never there, the shot is refused with the reason that AP at Infantry (C8.31's HE Equivalency) is pass 60's.

**F4. Canister.** My reading of the aim point ("walk two hexsides from a vertex of the firing hex") was wrong: a script of the hex grid gives two such vertices in a CA, each with an adjacent hex in its cluster. **R55.8:** the five aim points of a CA are the vertices whose three hexes all lie in the CA at range 2 or 3, which the script counts as five for every facing and both column parities, and which are the five circles of the figure on p. 178 (firer in Z6; Z3/Z4/AA4, Z4/AA4/AA5, AA4/AA5/BB4, AA5/BB4/BB5, BB4/BB5/CC5). With L and R the two hexes of the CA adjacent to the firer:

| Aim | The cluster | Ranges | The adjacent hex on the LOS (C8.41) |
|---|---|---|---|
| 1 | 2L, 3L, 2L+R | 2, 3, 3 | L |
| 2 | 2L, L+R, 2L+R | 2, 2, 3 | L |
| 3 | L+R, 2L+R, L+2R | 2, 3, 3 | L and R, quartered: the LOS runs along their hexside |
| 4 | 2R, L+R, L+2R | 2, 2, 3 | R |
| 5 | 2R, 3R, L+2R | 2, 3, 3 | R |

**R55.9:** Canister is the ordnance shot with the ammunition `canister` and an aim of 1 to 5, with a facing when the CA must turn. It rolls no To Hit DR: one IFT DR, recorded on the shot, attacks every Location of the cluster and of the adjacent hex that is at the firer's elevation and in its LOS, each as a fire record of its own on that same DR with its own TEM and Hindrance, as Spraying Fire's second Location is chained. The same DR is the Depletion, the malfunction, and the ROF check. The FP is 12, 20, or 24 by caliber; each of these halves it and they add up as C8.4's own "halved again" has it: the adjacent hex (no PBF), Bounding First Fire, Motion or not stopped, Intensive Fire, a pinned crew, a concealed target. Case A is an IFT DRM (D3.52). No leader, no FG, no Cowering, no Snap Shot; the Gun's Acquisition is lost (C6.5). Friendly units are not attacked (A7.4); a Location in Melee refuses the shot. An AFV is unharmed and its CE crew takes the Collateral Attack the IFT already gives. **R55.10:** FFMO and FFNAM apply to Canister in Defensive First Fire as to any IFT attack that is not an ordnance hit; an AFPh shot by a Gun that did not move is not halved, since C8.4 halves it only for Infantry that moved to man the Gun.

**F5. Motion attempts.** **R55.11:** a new action, `asl.game.motion-attempt`, for the side that is not phasing while the window on an enemy ground unit's expenditure is open: the vehicle, and a VCA and a TCA to take on success. The dr must be at most the MF or MP the moving unit has spent in the vehicle's LOS in this MPh, counted as a sum over its steps and not as an unbroken LOS (Cases C1 and J1 count the other way). **R55.12:** "had not been in the vehicle's LOS during that Player Turn" is decided exactly: nothing moves in the PFPh and a unit moves once, so the LOS to the Location the unit began its move in decides it. The rule's "automatically fails" follows a LOS Check made after the declaration; the game makes that check first and refuses, which costs no attempt. **R55.13:** the vehicle is Mobile, not stunned, shocked, or Abandoned, carries no First, Final, or Intensive Fire counter, and has made no attempt in this MPh; a vehicle under the "+1" counter may, and so may one already in Motion, for the free change of CA. A VCA change in woods, a building, or rubble takes its Bog Check, and a bogged vehicle gains neither the Motion nor the change. A red-MP AFV makes its Mechanical Reliability DR. A new event, `motion-attempted`, and a list of the phase's attempts in the state. **R55.14:** a DEFENDER's vehicle in Motion fires its MA with Case C alone and the lower dr doubled (C5.35: "must use the Case C, or C1, or C2 DRM as applicable"; C5.31 and C5.32 define C1 and C2 only for "a vehicle First Firing during its own MPh"). No sentence says so outright; it is a reading. Its MGs are halved already.

**F7. Armor Leaders.** The agents agreed on a field of the vehicle and not a unit held in it (a contained unit meets every scan of the units and every path that walks Passengers), and disagreed on catalog definitions. **R55.15:** no definition. A vehicle carries an Armor Leader as a morale and a leadership, of the grades 8-1, 9-1, 9-2, and 10-2 (the H1.43 table lists exactly these), and whether he has been revealed. The enemy's view does not show him until the first record that carries his modifier or his morale (D3.41). **R55.16:** his modifier is always applied where D3.44 lets it be: the MA's To Hit DR, the IFT DR of a MA that is a MG, the OVR DR (D7.16), the vehicle's own CC attack, and the colored dr of Bog Removal (D8.3). Every grade's modifier is negative and none of those rolls is harmed by a lower DR; the player's leave to withhold it (A10.72) costs only the reveal, and gets a backlog row. **R55.17:** the crew's Morale Level is his (D3.42) in the two checks the code has, the Shock and Immobilization checks and the CE crew's MC; his modifier is not added to them, since he takes the check himself. He is lost with a Recall (D5.341) or a wreck, and when the crew takes counter form (D3.43); his return when that crew re-crews is left out. **R55.18:** an Inexperienced Crew (D3.45) is a condition of the vehicle, `asl:inexperienced-crew`, given at setup: plus 1 on the same rolls, a crew morale of 6, never lost.

**F8. Gun Duels.** **R55.19:** when the side that is not phasing proposes a Defensive First Fire ordnance shot at the moving vehicle, and the firer is not concealed, the vehicle is not conducting an OVR, its MA could Bounding First Fire at the firer without a change of CA, and its Gun Duel DRM is lower than the firer's, the proposal's commit is the declaration: a pending choice of the kind `gun-duel` for the ATTACKER, holding the declared shot's arguments and no rolls. Both totals are open to both sides, as the example has them compared aloud. On `decline` the declared shot resolves. On `duel` one commit holds the vehicle's shot, then the declared shot planned again against the state the first left; a firer that the first shot eliminated, broke, stunned, or shocked is refused by the checks that exist, which is the rule's void shot. The Gun Duel DRM is the firer-based To Hit DRM (Cases A to I, with the stun's, an Armor Leader's, and the others the chart lists as the firer's) and the Acquisition, without Case C4's doubled dr. **R55.20:** left out, each to pass 110: the equal case, fire that is not ordnance on either side, the second duel with the ROF as a DRM, a duel declared by the DEFENDER (C5.33), a Gyrostabilizer.

**F9. The order and the estimate.** **R55.21:** section 2's table. The counters first, with the notation they are written in: the version's change is made once. Fire at another level next: it changes the map facts every later shot reads. Then the ammunition, Canister, Armor Leaders, Motion attempts, and the duel last, which sums the firer's DRM of all the others.

## 7. The notation and the counters: 55.1 (first part), 55.6 (increment 2a)

Built on 2026-10-13 in one commit by a building agent to a brief (`pass55/brief_build.md` and the task's own), its diff read and its work checked in the Studio by the session.

### 7.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **The British light mortar keeps red To Hit numbers,** though the chart of p. 695 gives the British black. Its shots are recorded with red, and a recorded shot must not change; the chart's color is read for a Gun and a vehicle's MA only (ruling R55.22).
- **No second reading of the 84 transcription rows was made.** The source record names the frame's agent as the reviewer of the values, which the building agent matched one by one against its own renders of pp. 392, 426, and 695. The reviews of the pass read them again.
- **APDS, Canister, and the 6-Pounder's HE are listed and not yet fired:** the page offers them for the new Guns and the gate refuses them until increments 2c and 2d.

### 7.2 What is built

**The notation (ruling R55.4).** `ParseAmmunition` reads one item of a counter's special ammunition, `<type><number>[^<year digit>[m<month>][+][E]]`, with the two-letter types (HE, AP) tried before the one-letter ones (A, D, H, C); anything else a counter lists, such as `s7`, is not an item and is passed over. `Depletion` answers for all six types: a number holds from its year, and from its month where the counter gives one, until the next listed one; a number with no year holds at any date (C8.8); undated HEAT keeps C8.3's dates; Elite adds one (C8.2), which increment 2c sets. A test pins the Depletion Number of every item of every older counter for each year from 1939 to 1945, run against the old code before the rewrite and against the new after it: nothing moved.

**The counters (ruling R55.3).** `british-at-gun`, `american-at-gun`, `british-crew`, and `american-crew`, every value as the frame gives it and none manufactured; the malfunctioned side's repair and removal numbers are "not in source", as the older Guns have them. 84 rows in each of the two counter sheets, two new sheets in the source record (OLB, OLA), the catalog rebuilt at 1.14.0, the four packages' digests pinned again. A game recorded under 1.13.0 loads and replays against 1.14.0 (`OlderGamesTests` has the row); nothing compares the package digest an event names at replay; the cards, the fixtures, and the tests that name 1.13.0 are untouched. A new game is recorded under 1.14.0.

**The To Hit color (C3.3, p. 169; the chart of p. 695).** British black; American red before 1944 and black from 1944. An American shot always records the scenario's year, which a shot at Infantry with no named ammunition did not; no American ordnance fired before the pass.

### 7.3 Ruling, the new facts, the games, and the tests

- **R55.22.** The chart's To Hit color is read for a Gun and a vehicle's MA. A light mortar and a LATW keep the color they had before the pass, red for every nationality but the German.

**New in a record:** `scenarioYear` on an American shot at Infantry. No record made before the pass can lack it.

| Game | What it shows |
|---|---|
| `p55-6pdr` | June 1944. A 6-Pounder in [D10] is offered AP, APDS, and HE against a PzKpfw IIIH in [F10]; APDS, and HE at the squad in [G10], are refused for now. AP: Basic TH# 10 (black), a hull hit, TK# 15 + 1 less AF 6 = 10, a wreck. Recorded. |
| `p55-37mm-1943` | An American 37mm AT Gun fires HE at [H10]: "Basic TH# 8 (red)". Recorded. |
| `p55-37mm-1944` | The same shot a year later: "Basic TH# 8 (black)", a hit, 4 FP. Recorded. |

Rules tests: `ScenarioA1Pass55RulesTests`, nine tests. Run by the agent: Rules 262 (the pass's class, pass 7's, the packages', and the classes the catalog reaches), Authoring's matrix tests 21, Units 409 and its counter sheets 19 and rendering 352 whole, Play 104, MapStudio 41: none failed.

## 8. Fire at another level: 55.4 (increment 2b)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 8.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **The halftrack's AAMG is held to C2.6's limit like any vehicle MG.** The rule names "non-AA vehicular MG", so an AAMG may be free to fire at a higher target nearer than the elevation difference; what an AA weapon may do is pass 105's, with the AA Guns.
- **A Critical Hit does not reverse the Height Advantage TEM (C3.71).** The hit's IFT attack does not carry the fact; an ordnance Critical Hit uphill gets no -1.
- **A wall or hedge the LOS crosses gives an ordnance shot no TEM on its To Hit DR.** This is older than the pass: the shot's map read finds the hexside's TEM and drops it, and no ordnance calculator reads it. It is not repaired here, since the repair would change what a shot recorded before the pass resolves to; it has a backlog row. What the pass does with it: such a target claims no Height Advantage, since it has another positive TEM to claim (ruling R55.24).
- **No Play test and no game for a vehicle's MG at another level of its own hex:** the setup refuses a tank in a building. A Rules test holds the bar.
- **A cliff** (B11.31, B11.32) refuses an ordnance shot at an adjacent Location of another level, with the LATW, ATR, PIAT, and mortar exceptions; a Rules test only, since no board in the tests has a cliff.

### 8.2 What is built

**The limit (C2.6).** A Gun, a tank's MA, and a mortar firing down fire at a Location of another level when the range is at least the elevation difference, on the Infantry, Vehicle, and Area Target Types; a mortar fires up at any range and a LATW is free of the limit ("but not to SW [EXC: they do apply to MTR/INF/RCL ordnance SW]"). A vehicle's BMG, CMG, and MA AAMG fire on the IFT under the same limit. The two refusals "at different levels are not reviewed" are gone. Another level of the firer's own hex stays refused, for a Gun and for every vehicle weapon, with the rule in the refusal.

**Height Advantage (B10.31, C6.8, D4.22).** The shot's map read already found it, with the Crest Line exception, and dropped it. It is now a fact of the shot, and Case Q +1 on the To Hit DR of the Infantry and the Vehicle Target Type when no other Case Q of the shot is a positive TEM; a vehicle in Bypass, which takes no terrain TEM, may take it. With it the mover's Case J4 is not applied ("not subject to Interdiction/FFMO by an attack to which that +1 TEM applies"). Not on the Area Target Type: a mortar's fire is Indirect Fire (C.1). A vehicle's MG fire reads Height Advantage on the IFT as Infantry fire does.

**The old lines beside it,** each read against a firer and a target at different levels: the To Hit cases A to P and R, and all of the To Kill run, are right as they stand (Case L has no level clause; no To Kill case reads the firer's elevation but the Aerial, DC, and MOL one); a vehicle's MG no longer doubles at Point Blank Range against a target more than one level above (A7.21; ruling R10.4's bound).

### 8.3 Rulings, the new facts, the games, and the tests

- **R55.23.** C2.6's limit holds for a vehicle's MA AAMG as for its BMG and CMG.
- **R55.24.** A target behind a wall or hedge the LOS crosses claims no Height Advantage on a To Hit DR.

**New in a record:** `heightAdvantage` on an ordnance shot; on a vehicle's fire attack, `heightAdvantage` and `targetLevelAbove`, which Infantry fire had. A record made before the pass is at one level and has none: no Case Q line, Case J4 as it was, the PBF as it was.

In my Studio on port 6671, board 3, scripted dice; each pair's LOS was read on the board viewer first.

| Game | What it shows |
|---|---|
| `p55-ha-inf` | A German INF Gun in [E6] fires up at a squad on the hill [E5]: Case L -2, "Case Q, height-advantage, B10.31" +1, a hit. Recorded. |
| `p55-ha-veh` | A PzKpfw IIIH in [E6] fires AP up at a T-34 in [E5]: Case I, Case L, and Case Q +1 (D4.22). Recorded. |
| `p55-ha-down` | CONTROL: a Russian AT Gun in [E5] fires down at [E6]: Case L alone. Recorded. |
| `p55-cmg-up` | A PzKpfw IIIH in [N4] fires its CMG, then its MA with HE, at a squad on level 1 of the stone building [N2] at range 2: the building's +3 as Case Q, no Height Advantage. Recorded. |
| `p55-own-hex` | A Gun at ground level of [M2] is refused level 1 of its own hex: "does not fire at another level of its own hex ... (C2.6)". A refusal, nothing recorded. |

Rules tests: six more in `ScenarioA1Pass55RulesTests`. Play tests: `BacklogPass55Tests`, new. Run by the agent: Rules 259 in fourteen classes, Play 149 in eleven: none failed. One older test changed: `RefusalReasonsTests` named the refusal that is gone.

**The replay digest after increment 2a** (473 games, 93,266 lines; the base built from main's sources at 846eb5a3; the catalog's identity, which is hashed into every state and went to 1.14.0, left out on both sides): no line differs. The first trial, without that, differed in every state's line and was a void run; a dump of one state named the two properties in a minute.
