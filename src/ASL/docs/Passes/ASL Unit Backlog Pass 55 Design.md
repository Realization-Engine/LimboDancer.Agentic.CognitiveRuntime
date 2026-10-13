# ASL Unit Backlog Pass 55 Design

**Status:** Merged into main as 50987ff3 and pushed on 2026-10-13, after the gate passed (section 16); the user approved the departures and the rulings that day. Built to the merge gate on 2026-10-13 under the user's word of 2026-10-12 for this pass ("I want you to run without my intervention up to the Gate"): the frame, seven tasks in seven commits (the plan's 55.1 to 55.6, and 55.7, Gun Duels, carried in from pass 50 and built as a slice), then the five reviews and their fixes. Sections 14 and 15 say how it ended; section 14.1 lists what departs from the plan's entry and the frame. The frame's questions were put to three read-only agents, as that word asks; their answers were judged against the pages and the code, and each decision below is a ruling decided under that word and open to a veto. The gate was run later that day (section 16).

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

Then: 7, the notation and the counters (55.1 in part, 55.6); 8, fire at another level (55.4); 9, special ammunition (55.1, the rest); 10, Canister (55.2); 11, Armor Leaders and Inexperienced Crews (55.5); 12, Motion attempts and the DEFENDER's Motion firer (55.3); 13, Gun Duels (55.7); 14, the five reviews and the Studio check; 15, as built; 16, the gate.

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

## 9. Special ammunition: 55.1, the rest (increment 2c)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 9.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **Nothing is built on the Area Target Type.** Only mortars fire on it, and no mortar of the catalog lists an HE number. Ruling R55.7 named all three Target Types.
- **AP against Infantry (C8.31) stays out,** as the frame said: a 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry.
- **An Intensive Fire shot that finds no round leaves no Intensive Fire counter,** read from "not to have fired yet for any purpose" (ruling R55.25).
- **A malfunction on a DR above the number records a miss for the new kinds only** (APDS, HE, AP by number). An APCR or HEAT record keeps the To Hit result it showed before the pass, which may read "hit" beside a malfunction and no To Kill DR; it is left so that no record changes.

### 9.2 What is built

**APDS (C4.3, C7.32, C8.11).** The ammunition `apds` for a Gun that lists a D item, on the Vehicle Target Type: the Basic TH# lessened as APCR's is, the Basic TK# of the APCR/APDS table's D listings (57L 18, 76LL 25; read on a render of p. 190), its own Case D row, and the AP table's number against an unarmored vehicle (C7.321).

**C8.9 for every ammunition with a Depletion Number.** Below the number the round is used; at it, it is the last; above it the Gun had none, has not fired (the ROF kept, no fire counter, the Acquisition as it was), and has none for the rest of the game, unless the same DR is a malfunction. This was APCR's and HEAT's on the Vehicle Target Type; it now holds for APDS there and for HE by its number on the Infantry Target Type, where the step stands after the malfunction check and before the hit, so that no IFT roll is asked for a round that was not there. AP by its number takes the same road (a Rules test with a made-up Gun).

**C2.21.** A Gun whose HE has a Depletion Number does not fire HE unnamed. At Infantry the page names `he` for it; unnamed it is refused (`he-not-named`), and with no HE at the date or none left it is refused with "This Gun's HE is Special Ammunition (C8.8) and it has none now ... its AP against Infantry is not built (C8.31, HE Equivalency)".

**Elite (C8.2; ruling R55.6).** The special rule `elite-ammunition:<side>` adds one to every Depletion Number of that side's ordnance; the shot records `elite`. No card carries it.

**The page.** The ammunition list shows what the Gun may fire at the chosen target, each special kind with its number at the game's date ("APDS (Depletion Number 6)"; "7: 6, and 1 for Elite, C8.2"), and a note for a kind left out ("APDS: none at the scenario's date (C8.9)", "APDS: it has run out (C8.9)"). The proposal says before the roll what a DR above the number means; the record says which of the three it was.

**The Supply Chart (C8.12; ruling R55.5)** is a Rules test's data. Two counters differ from it and are pinned by name, as C8.12 allows a counter to: the PzKpfw IIIH keeps A5 from 1943 on where the chart falls to A4, A3, and none, and the T-34 M41 has A6 in 1945 where the chart has A7. The 6-Pounder and the Russian 45L agree with it.

### 9.3 Ruling, the new facts, the games, and the tests

- **R55.25.** A shot whose special ammunition was not there leaves no fire counter of any kind, the Intensive Fire counter included (C8.9; C8.92 only says the number is not changed by Intensive Fire).

**New in a record:** `elite` on an ordnance shot (absent: not Elite; the check recomputes it from the game's own special rules); `ammunitionUse` on an Infantry Target Type resolution; `apds` and `he` among a Gun's depleted kinds. The step runs only for a shot that names an ammunition whose counter lists a number, which before the pass was APCR and HEAT alone.

In my Studio on port 6671, board 3, scripted dice, a 6-Pounder in [D10], a PzKpfw IIIH in [F10], a squad in [G10]; each game's file was read back.

| Game | What it shows |
|---|---|
| `p55-apds-usedc` | June 1944. APDS on an Original 3: "below the Depletion Number 6: APDS fired", Basic TK# 18. APDS on a 6: its last; the list then offers AP and HE with "APDS: it has run out (C8.9)". Recorded. |
| `p55-apds-nonec` | APDS on an Original 7: no hit, no fire counter, the ROF kept; AP then fires. Recorded. |
| `p55-apds-malfc` | APDS on a 12: none, and the Gun malfunctions and takes its Prep Fire counter. Recorded. |
| `p55-apds-mayc` | CONTROL, May 1944: the list has no APDS, "none at the scenario's date". Nothing proposed. |
| `p55-apds-elitec` | With `elite-ammunition:british`: "Depletion Number 7"; an Original 7 is the last round. Recorded. |
| `p55-he-numberc` | HE (8) at the squad in [G10]: named by the page, a hit, 6 FP, "HE fired (C8.9)". Recorded. |
| `p55-he-1942c` | CONTROL, July 1942: no HE yet; refused with the reason above. |
| the same ids without a letter, and with `b` | SUPERSEDED or spoiled by the session's own script (a tank wrecked by the first shot; a page not yet loaded when the dice were queued). |

Rules tests: 24 in `ScenarioA1Pass55RulesTests` in all. Play tests: 17 in `BacklogPass55Tests` in all. Run by the agent: the Rules classes of the ordnance passes 149, Play 121, MapStudio's ordnance, vehicle, and record classes 14: none failed.

**The replay digest after increment 2b** (93,266 lines, from the copy of 2b's own build): no line differs from the base.

## 10. Canister: 55.2 (increment 2d)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 10.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **Canister is not fired as Defensive First Fire.** In the MPh it is refused with its reason. Ruling R55.10's first half (FFMO and FFNAM for Canister) is therefore not built. It would need the mover's own Location among several attacked, FFMO and FFNAM for one Location of several, the shots a Gun has at one expenditure (C6.17), and Residual FP, which A8.2 gives Canister (C8.31 denies it to the AP kinds only).
- **A Location that the IFT refuses refuses the whole shot:** an AFV in positive TEM (ruling R25.6), a Melee, prisoners. For a hidden AFV the refusal could tell the firer something; not tested.
- **One filter stands outside Rules:** a fire record of a Canister shot is marked as ordnance's, and A7.55's bar on a second attack at the same Location passes over such records at three places, two in Play and one in the projector. Without it Infantry beside the Gun could not fire at a Location the Canister attacked. It is a rule's clause decided outside Rules, and has a backlog row for the boundary.
- **A pending choice in one attacked Location** resumes the others through the follow-up the fire planner already has; no test reaches it.
- **Left out as the frame said:** the building option, an aim at another elevation, the SMOKE and LV clause, pillboxes, Infantry that mans a Gun in the MPh, the Bypass TCA, an AFV with Canister (so the Bounding First Fire and Motion halvings are in the calculator and its tests and in no game).

### 10.2 What is built

**The shot (rulings R55.8, R55.9).** `asl.game.fire-ordnance` with the ammunition `canister`, an `aim` of 1 to 5, and an `aimFacing` when the CA turns first; it names no target. Rules holds the aim table in steps of the CA's two adjacent hexes; Play turns an aim into hexes on the map. It rolls no To Hit DR: one IFT DR, recorded on the shot, and a fire record on that same DR for every Location of the cluster and of C8.41's adjacent hex or hexes that is at the firer's elevation and in its LOS, an empty one too, so that the records do not tell the firer which hexes hold hidden units. Each Location takes its own TEM and Hindrance on the IFT DR; a CA change adds Case A to each (D3.52; the Gun's type, doubled in woods, a building, or rubble) and lowers the ROF as for any shot (C2.5).

**The FP** is 12, 20, or 24 by caliber, and each of these halves it: the adjacent hex (a quarter in each of the two when the LOS runs along their hexside), Bounding First Fire, Motion, Intensive Fire, a pinned crew; a concealed target takes half again by the IFT's own Area Fire. No PBF, no leader, no FG, no Cowering, no fire counter from the IFT's side.

**The one DR** is also the Depletion check with all three of C8.9's outcomes (above the number nothing is attacked and the Gun has not fired), the malfunction check, and by its colored dr the ROF check. The Gun's Acquisition is lost (C6.5). An AFV in an attacked Location is unharmed. Friendly units are not attacked (A7.4).

**The page.** A Gun that lists Canister shows its ammunition choice before any target; with Canister chosen, a facing (`#ordnance-canister-facing`, the Gun's own first, the others marked "turn first: Case A on the IFT DR") and the aims of that facing (`#ordnance-canister-aim`), each with its hexes and what the side's view knows there: "aim 4: [D9] [D10] [C10], and [E10] at half FP: 3 units". An aim with no hex on the map is not offered. The proposal lists each Location with its FP and terrain and says what the one DR also checks.

### 10.3 The new facts, the games, and the tests

**"Barring obstacles in the first hex of the cluster"** is read as the LOS to each Location, which the per-Location read gives (ruling R55.26).

- **R55.26.** A Location of the aim is attacked when the firer has a LOS to it; no further test is made of the cluster's first hex.

**New in a record:** on an ordnance shot, `rolls.canister`, `facts.canister` (the aim, the facing, the Locations), `resolution.canister`, and a null `toHit`; on each fire record of the shot, `facts.canister` and no firers; in the state, `ordnance` on a fire of the phase. Every one is absent from a record made before the pass, when the ammunition was refused.

In my Studio on port 6671, board 3, August 1943, scripted dice: a 37mm AT Gun in [F10] facing west; German squads in [D10], [E10], and, concealed, [D9]; a PzKpfw IIIH in [C10].

| Game | What it shows |
|---|---|
| `p55-can-used` | Aim 4, a DR of 2 and 3: "below the Depletion Number 7", four fire records: [D9] at 12 FP and half again against the concealed squad, [D10] and [C10] at 12, [E10] at 6; the ROF kept. Then aim 3 on a DR of 7: the last round, [E10] at 3 FP ("a quarter in each of the two adjacent hexes"), no more fire; the list then reads "it has run out". The tank is unharmed in both. Recorded. |
| `p55-can-none` | A DR of 8: "it had no Canister, attacks nothing, has not fired"; no fire record. Recorded. |
| `p55-can-malf` | A DR of 12: no Canister, and the Gun malfunctions. Recorded. |
| `p55-can-turn` | The CA turned one hexspine to the north-west: "Case A +3 on the IFT DR (D3.52)" on each of three Locations, [C9] with its woods +1; a colored 1 keeps the lowered ROF. An aim to the south-west, off the board, is not offered. Recorded. |
| `p55-can-1942` | CONTROL, July 1942: the list has HE alone, "none at the scenario's date". |

Rules tests: 30 in `ScenarioA1Pass55RulesTests` in all. Play tests: 26 in `BacklogPass55Tests` in all. A page test in `PlayPageOrdnanceTests`. Run by the agent: Rules 196 and 136 in two runs, Units 409 whole, Play 131, MapStudio 35: none failed.

**The replay digest after increment 2c** (93,266 lines, from the copy of 2c's own build): no line differs from the base.

## 11. Armor Leaders and Inexperienced Crews: 55.5 (increment 2e)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 11.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **A possible leak is not closed:** a pending choice that interrupts a shot at his tank keeps the shot's facts whole in its resume, the crew's morale among them, before he is revealed.
- **A card's setup plan cannot give a vehicle an Armor Leader;** a hand setup on the page and a game file can.
- **The map counter's tooltip and the placement list's row do not name him;** the units table and the firer's label do, for his owner.
- **Whether a vehicle is an armed AFV is checked by the planner's setup bar,** not by the projector, which refuses only a leader on something that is no vehicle, a bad grade, or a leader beside the Inexperienced Crew. A hand-written file with a leader on a truck replays.
- **Not built, as the frame said:** the HD Maneuver dr, the halftrack's FG with its Passengers, the assisting AFV of D8.3, the leave to withhold his modifier (A10.72), his return when the crew re-crews, his VP.
- **Added beyond the brief:** an Inexperienced Crew's +1 lowers Residual FP as a firer's positive DRM does (A8.26).
- **No Studio game of an OVR or of the vehicle's Close Combat attack with him;** their Play tests hold those two uses.

### 11.2 What is built

**The leader (ruling R55.15).** A vehicle instance carries `armorLeader`, a morale and a leadership of the grades 8-1, 9-1, 9-2, and 10-2, and whether he is revealed. The page's placement form offers him for an armed AFV (`#place-armor-leader`). The enemy's view shows neither him on the unit nor his part of the record that created the vehicle, until the first record that carries his modifier or names his morale in a check; from then on he is public. He is lost with a Recall, a wreck or an elimination, and when the vehicle is Abandoned (D5.341, D3.43).

**His modifier (ruling R55.16; D3.44)** is a DRM line of its own, "Armor Leader -1 (D3.44)", on the MA's To Hit DR on the Infantry and Vehicle Target Types, on the IFT DR of a MA that is a MG, on the OVR DR (D7.16), on the vehicle's own Close Combat attack, and on the colored dr of Bog Removal (D8.3). BU does not void it. A tank's BMG and CMG take none.

**His morale (ruling R55.17; D3.42)** replaces the nationality's in the two checks the code has, the Shock and Immobilization checks of a hit and the CE crew's MC under fire, without his modifier. A record names the crew's morale only when that crew checked, so that a miss tells the enemy nothing; the check's record says whose morale it was ("crew morale 10, its Armor Leader's (D3.42)").

**Inexperienced Crews (ruling R55.18; D3.45).** The condition `asl:inexperienced-crew`, given at setup (`#place-inexperienced-crew`), public: +1 on each of those rolls, a crew morale of 6, never lost; not with an Armor Leader.

### 11.3 Ruling, the new facts, the games, and the tests

- **R55.27.** A record carries the morale of a target's crew only when that crew took a check in it.

**New in a record:** `armorLeader` on an instance; `armorLeadership` on a vehicle firer's facts (ordnance, fire, OVR, Close Combat) and on a Bog Removal's check; `crewMorale` on a vehicle target and `moraleSource` on a check. Each is absent without a leader or the condition, which no older game has.

In my Studio on port 6671, board 3, scripted dice: a PzKpfw IIIH in [D10] and a T-34 in [F10].

| Game | What it shows |
|---|---|
| `p55-al-fire` | The German tank with a 9-1 fires AP: "- 1 (Armor Leader, D3.44)". The Russian view's row for the tank does not name him before the shot and reads "with Armor Leader 9-1" after it. Recorded. |
| `p55-al-ctl` | CONTROL: written, not played; `p55-al-shock`'s firer has no leader and shows the same shot without the line. |
| `p55-al-shock` | The T-34 hits the tank with a 10-2 and the TK DR is a dud: Final TK# 9 read; no check, so no morale in the record. Recorded. |
| `p55-al-shock2` | The same hit with a TK DR of 10: "possible Shock; NTC DR 10 against 10 (crew morale 10, its Armor Leader's (D3.42)): passed"; the Russian view then names him. Recorded. |
| `p55-al-inexp` | A T-34 with an Inexperienced Crew fires: "+ 1 (Inexperienced Crew, D3.45)"; its row reads "Inexperienced Crew (D3.45)". Recorded. |
| `p55-al-bog` | A bogged tank with a 9-1 starts: "freed on a colored dr of 1 to 4 (Armor Leader -1 (D3.44))"; a colored 5 frees it. Recorded. |

Rules tests in `ScenarioA1Pass55RulesTests` and Play tests in `BacklogPass55Tests` for each use, the view, and the loss; a Units test of the loss by Recall, Abandonment, and elimination. Run by the agent: Rules 354 in fourteen classes, Units 416 whole, Play 226 in fifteen classes, MapStudio 49 in eight: none failed.

**The replay digest after increment 2d** (93,266 lines, from the copy of 2d's own build, the pass's new properties left out on both sides): no line differs from the base.

## 12. Motion attempts and the DEFENDER's Motion firer: 55.3 (increment 2f)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session.

### 12.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **An attempt that no dr could pass is refused and costs nothing:** with less than one MF or MP spent in the vehicle's LOS the attempt is not rolled (ruling R55.28).
- **A facing or a turret named for a vehicle in Bypass is refused;** the attempt alone is allowed there. Bypass's CA is not built.
- **The projector admits a Mechanical Reliability or Bog check of a vehicle that is not phasing** while the window is open and the vehicle has made no attempt; it does not ask that the attempt's own record follows.
- **Not built:** a stall; the platoon's and the radioless AFV's drm (D14.211, D14.23); the Convoy's bar.
- **A vehicle's own Bog and Mechanical Reliability checks have no record line of their own** on the page, as before the pass; the attempt's line says what they did to it.

### 12.2 What is built

**The attempt (rulings R55.11 to R55.13).** `asl.game.motion-attempt`: a vehicle of the side that is not phasing, while the window on an enemy ground unit's expenditure is open, with a VCA and a TCA to take on success. Rules bars a vehicle that is immobilized, bogged, mired, stunned, shocked, Abandoned, a wreck, or Recalled in this Player Turn; one under a First, Final, or Intensive Fire counter; and one that has attempted in this MPh. The moving unit must have begun its move out of the vehicle's LOS, read at the Location it began in, or the attempt is refused with the reason and none is spent. The number is the sum of the MF or MP of the mover's steps that ended in the vehicle's LOS, its Start, Stop, and Delay MP among them; a half adds nothing to a dr. Of a moving stack the unit that began out of LOS and has spent the most is answered.

**On a pass:** a red-MP AFV rolls for Mechanical Reliability, and a 12 immobilizes it with neither Motion nor the change (D2.51). A VCA change in woods, a building, or rubble takes a Bog Check for each hexspine, the shorter way round; bogged, the vehicle gains neither. Otherwise it takes the Motion counter, the VCA, and the TCA, and loses its Acquisition if either changed. A failure leaves only the mark of the attempt. A new event, `motion-attempted`, and the state's list of the phase's attempts.

**The DEFENDER's Motion firer (ruling R55.14).** A vehicle in Motion of the side that is not phasing fires its MA in the MPh and the DFPh with Case C alone, the lower dr of its To Hit DR doubled, no Acquisition DRM, and no Case L; a turret turned for the shot pays Case A, which C5.13 bars only in the firer's own MPh. Its MGs fired halved before the pass and still do.

**The page.** The DEFENDER's part of the page gains "Motion attempt (D2.401)": each of the side's vehicles with what the mover has spent in its LOS and the dr that passes, or the reason it may not attempt; a VCA and a TCA to take.

### 12.3 Ruling, the new facts, the games, and the tests

- **R55.28.** A Motion attempt against a mover that has spent less than one MF or MP in the vehicle's LOS is refused and is no attempt.

**New in a record:** the event `motion-attempted` (the vehicle, the roll, the half points, the result, the mover, the facing, the turret); in the state, `motionAttempts`. No fact is added for the Motion firer: its shot was refused before, so no record holds one.

In my Studio on port 6671, board 3, scripted dice, the adjudicator's view: a PzKpfw IIIH starts in [D9], out of LOS of a T-34 in the woods of [H10], and enters [D10].

| Game | What it shows |
|---|---|
| `p55-mot-pass` | After the Start MP: "has spent 0 MP in vehicle R1's LOS", no attempt offered. After the entry: "1 MP in its LOS: succeeds on a dr of 1 or less". A dr of 1 with the VCA to the west: the Mechanical Reliability DR, three Bog DR for three hexspines in woods, then "it gains Motion and turns its VCA to west". The T-34 then fires its MA: Case C +4, Case I, Case J2 +4, "Case C4, the lower dr doubled" +2. Recorded. |
| `p55-mot-fail` | A dr of 4: "it fails, with no penalty but that it gains no Motion this Player Turn"; a second attempt is not offered. Recorded. |
| `p55-mot-immob` | A dr of 1 and a Mechanical Reliability DR of 12: immobilized, "neither Motion nor a change of CA (D2.51)". Recorded. |
| `p55-mot-seen` | CONTROL: the German tank begins in [D10]: "began its move in vehicle R1's LOS ... no attempt is spent". |

Rules and Play tests in the pass's two classes; a page test with an Infantry mover. One older test changed: pass 50's Rules test of a Motion firer asserted the refusal that is gone. Run by the agent: Rules 106 in four classes, Play 136 in six, Units 416 whole, MapStudio 38: none failed.

**The replay digest after increment 2e** (93,266 lines, from the copy of 2e's own build): no line differs from the base.

## 13. Gun Duels: 55.7 (increment 2g)

Built on 2026-10-13 in one commit by a building agent to a brief, its diff read and its games played in the Studio by the session. The task was carried in from pass 50 (its design, section 6, question F5).

### 13.1 Departures from the plan's entry and the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **The slice, as the frame said (rulings R55.19, R55.20):** ordnance on both sides, the vehicle's Gun Duel DRM strictly lower, the first duel of an expenditure. **When the two totals are equal no duel is offered, though the rule gives one** with the Final DRs deciding the order. Fire that is not ordnance, the second duel with the ROF as a DRM, a duel declared by the DEFENDER (C5.33), and a Gyrostabilizer are left with it.
- **A LATW's declared shot is not dueled** (a PF, a PSK, an ATR): a PF's shot waits on its own check, and the three are kept alike. The frame left this to the build.
- **A Gun stacked with other Infantry is offered no duel:** the tank's shot at such a Location is refused by a check older than the pass, so its potential shot cannot be planned.
- **An Armor Leader who is not yet revealed is not counted in the vehicle's Gun Duel DRM at the declaration** (ruling R55.29). The declaration is the DEFENDER's act, and a proposal read and cancelled would otherwise show the enemy his line. This can deny the ATTACKER a duel that his modifier would earn.
- **A declined duel does not bar another** at the same expenditure; a fought one does.
- **The projector trusts a declaration:** it holds a duel shot to a `duel` answer for that vehicle at that expenditure, and does not ask again whether the choice was rightly offered.
- **Special ammunition is not offered for the duel's shot:** the vehicle fires AP at a vehicle and HE otherwise.

### 13.2 What is built

**The declaration.** When the side that is not phasing proposes a Defensive First Fire shot by a Gun or a vehicle's MA at the moving vehicle, Rules is asked, before any roll, whether that vehicle may duel: the firer is not concealed; the vehicle is not conducting an OVR; its MA could Bounding First Fire at the firer now but for the open window, by the same facts and checks as any such shot; that shot turns no turret and no hull; and its Gun Duel DRM is lower than the firer's. The Gun Duel DRM is read from each shot's own To Hit DRM list: the firer-based cases and the Acquisition, by one classification of every DRM name the three calculators can add (a test scans their sources, so a name added later fails until it is classified), without Case C4's doubled dr. If the vehicle may duel, the commit holds no shot: it is the declaration, a pending choice of the kind `gun-duel` for the ATTACKER with the declared shot's arguments and both totals with their lines, which both sides read. The proposal says so before the commit: "Declaring this shot lets vehicle G1 answer with a Gun Duel (C2.2401): its Gun Duel DRM +7 (Case C2 +6, Case I +1) is lower than gun R1's +8 (Case A +8); no dice are rolled until it answers".

**The answer.** `decline`: the declared shot is planned and resolved. `duel`: one commit holds the vehicle's MA shot at the firer as Bounding First Fire, marked as the duel's, then the declared shot planned again against the state the first left; when the firer can no longer fire, the event `gun-duel-voided` with its cause ("the return shot of gun R1 is void: its crew is broken (C2.2401)"). If the vehicle's shot stops at a choice of its own, the owed shot is carried in that choice's resume and runs after it. The duel shot is a Bounding First Fire shot for the ROF, the counter, and the MP between shots.

### 13.3 Ruling, the new facts, the games, and the tests

- **R55.29.** An Armor Leader not yet revealed is left out of his vehicle's Gun Duel DRM when a duel is offered and in what is shown of it; the shot the vehicle then fires uses him and reveals him.

**New in a record:** the choice kind `gun-duel`; `gunDuel` on an ordnance shot; the event `gun-duel-voided`; `gunDuelOwed` in a later choice's resume; in the state, the duel in progress. A Defensive First Fire shot recorded before the pass replays where a duel would now be offered: the projector asks for no declaration.

In my Studio on port 6671, board 3, scripted dice, the adjudicator's view: a PzKpfw IIIH starts in [D9], enters [D10] with its turret turned east, and is still moving; a Russian AT Gun in the woods of [H10].

| Game | What it shows |
|---|---|
| `p55-duel-decline` | The Gun must turn two hexspines: its proposal names the duel, +7 against +8, and its commit draws no dice: "declares a shot at vehicle G1, which may fire its MA first". The German side declines: the Gun's shot resolves, Case A +8, Case J2 +4. Recorded. |
| `p55-duel-miss` | The German side fires first: "as Bounding First Fire (D3.3), first in a Gun Duel (C2.2401)", Case C2 +6 and the lower dr doubled, a miss; then the Gun's shot. One commit. Recorded. |
| `p55-duel-void` | The tank's shot hits on an Original 2 and breaks the crew: "The return shot of gun R1 is void: its crew is broken (C2.2401)". Recorded. |
| `p55-duel-none` | CONTROL: the Gun already faces the tank, its DRM 0 against +7: no duel named, the shot resolves at once. Recorded. |

Rules tests (the classification, the rule's own example at +6 and +6, each bar, ruling R55.29) and Play tests (declaration, decline, duel and miss, duel and void, a concealed Gun, equal totals, a second declared shot, the vehicle's shot stopping at a choice) in the pass's two classes; a page test of the choice. Run by the agent after the last change: `ScenarioA1Pass55RulesTests` 51, `BacklogPass55Tests` 58, `PlayPageVehicleTests` 9; before it, Rules 149, Play 306, Units 416 whole, MapStudio 62: none failed.

**The replay digest after increment 2f** (93,266 lines, from the copy of 2f's own build): no line differs from the base.

## 14. The five reviews and the Studio check

Run on 2026-10-13 under the user's word of 2026-10-12 for this pass: five read-only agents in one message (the referee in two halves, the table player, the Rules boundary, the UI and Blazor review), their briefs in `Prompts/Pass 55 Review Briefs.md`. They found one BLOCKER. Each finding was judged against the code and its page; those with a contained fix were fixed by a fixing agent to the session's fix lists (`pass55/fixes_referee1.md`, `fixes_referee2.md`, `fixes_boundary.md`, `fixes_table.md`, `fixes_ui.md`; what was done is `pass55/fixes_done.md`), and the rest have a row in the backlog's section 59. Nothing was put to the user first; every fix below is open to the user's veto.

**What the reviews confirmed.** The referee's first half read every catalog value of the two Guns and the two crews against renders of pp. 392, 426, and 695 and Notes 6 and C, which is the second reading increment 2a lacked (section 7.1), and found them right; so the To Hit colors, the APDS Case D row of p. 190, C8.9's "not to have fired" on each Target Type and for Canister, the Canister aim table for the figure's Z6 and for [F10] facing west, and that the halvings multiply. The second half found that the classification of the To Hit DRM agrees with the chart of p. 189, that the duel's paths neither lose nor double the declared shot, that the Armor Leader is given on no BMG or CMG attack and on none against his vehicle, and that the new facts are absent-safe. The table player checked the arithmetic of the recorded games against the charts. The UI review found that the Armor Leader in the units table comes from the view's own units, that the Motion block lists only the viewing side's vehicles, and that the Gun Duel's options go only to a view that may answer.

### 14.1 What departs from the plan's entry and the frame, first

Each was decided under the user's word of 2026-10-12, and each has a backlog row (section 59) unless the line says it is closed. The most important first.

- **The pass is about twice the plan's size** (section 2; ruling R55.1). The plan gives six tasks, 20 rows, and 3:40 of building; the inventory named 35 rows, Gun Duels came in as a seventh task, and the frame's estimate was 9:50 of building.
- **Gun Duels (C2.2401, p. 167) is a slice** (sections 2 and 13.1; rulings R55.19, R55.20): ordnance on both sides, the vehicle's Gun Duel DRM strictly lower, the first duel of an expenditure. **When the two totals are equal no duel is offered, though the rule gives one** with the Final DRs deciding the order; the rule's own example is that case. Fire that is not ordnance, the second duel with the ROF as a DRM, a duel declared by the DEFENDER (C5.33, p. 172), and a Gyrostabilizer are left with it.
- **No Armor Leader counter is in the catalog** (section 2; F7; ruling R55.15). The plan names "Armor Leaders 8-1 to 10-2" among the pass's counters. He is a morale and a leadership on his vehicle, as D3.41 (p. 200) keeps the counter out of play.
- **The Canister counter is a Gun, not an AFV** (section 2; F2), so the halvings for Bounding First Fire and Motion (C8.4, p. 178; D3.31, p. 200) are in the calculator and its tests and in no game. **Canister is not fired as Defensive First Fire** (section 10.1): in the MPh it is refused with its reason, and the first half of ruling R55.10 (FFMO and FFNAM for Canister) is not built.
- **A wall or hedge the LOS crosses gives an ordnance shot no TEM on its To Hit DR** (section 8.1). This is older than the pass and is left as found, since the repair would change what a recorded shot resolves to. Such a target claims no Height Advantage (ruling R55.24).
- **Nothing of C8.9 is built on the Area Target Type** (section 9.1), though ruling R55.7 named all three Target Types: only mortars fire on it, and no mortar of the catalog lists an HE number.
- **AP against Infantry (C8.31, p. 178) stays out** (section 9.1): a 6-Pounder with no HE at the date, or with its HE gone, is refused at Infantry.
- **A Critical Hit does not reverse the Height Advantage TEM** (C3.71, p. 170; section 8.1).
- **The halftrack's AAMG is held to C2.6's limit** (p. 168; section 8.1; ruling R55.23), though the rule names "non-AA vehicular MG". The AA Guns' leave is not built.
- **The British light mortar and every LATW keep red To Hit numbers** (section 7.1; ruling R55.22), though the chart of p. 695 gives the British black.
- **A Gun Duel is not offered** for a LATW's declared shot, or to a Gun stacked with other Infantry; **an Armor Leader who is not yet revealed is not counted** in either Gun Duel DRM at the declaration, his own vehicle's or the declaring vehicle's (section 13.1; ruling R55.29, which the reviews extended to the declaring vehicle), and this can deny the ATTACKER a duel his modifier would earn. A declined duel does not bar another at the same expenditure. The projector trusts a declaration. Special ammunition is not offered for the duel's shot (section 13.1).
- **A Hidden vehicle may not attempt Motion** (A12.32, p. 80; ruling R55.32), and **gaining Motion costs a concealed vehicle its "?" whether or not its CA changes** (A12.141, p. 78; ruling R55.33). Both came from the reviews; the frame had neither.
- **A Motion attempt that no dr could pass is refused and costs nothing** (section 12.1; ruling R55.28). A facing or a turret named for a vehicle in Bypass is refused. A stall, the platoon's and the radioless AFV's drm (D14.211, D14.23), and the Convoy's bar are not built. A vehicle's own Bog and Mechanical Reliability checks have no record line of their own on the page (section 12.1).
- **A Location that the IFT refuses refuses the whole Canister shot** (section 10.1): an AFV in positive TEM, a Melee, prisoners. Left out as the frame said: the building option, an aim at another elevation, the SMOKE and LV clause, pillboxes, Infantry that mans a Gun in the MPh, the Bypass TCA, an AFV with Canister. **A Canister shot calls no Sniper,** as ordnance does not (ruling R55.30).
- **An Intensive Fire shot that finds no round leaves no Intensive Fire counter** (section 9.1; ruling R55.25). **A number that starts in a month of the game's year is not available when the game gives no month** (ruling R55.31). A malfunction on a DR above the number keeps, for an APCR or HEAT record, the words it showed before the pass (section 9.1).
- **Armor Leaders** (section 11.1): a card's setup plan cannot give a vehicle one; the map counter's tooltip and the placement list's row do not name him; whether the vehicle is an armed AFV is checked by the planner's setup bar and not by the projector. Not built, as the frame said: the HD Maneuver dr, the halftrack's FG with its Passengers, the assisting AFV of D8.3, the leave to withhold his modifier (A10.72), his return when the crew re-crews, his VP. Added beyond the brief: an Inexperienced Crew's +1 lowers Residual FP (A8.26).
- **Tests and games that are lacking:** no Play test and no game for a vehicle's MG at another level of its own hex; a cliff (B11.31, B11.32, p. 126) by a Rules test only (section 8.1); no Studio game of an OVR or of the vehicle's Close Combat attack with an Armor Leader (section 11.1); no page test for the UI review's fixes 1, 2, 3, 5, and 6.
- **The counters need two crews as well,** and **the catalog goes to version 1.14.0** (section 2); the cards, the fixtures, and the tests that name 1.13.0 are left as they are.
- **Rows that earlier passes moved here are moved on** (section 3; ruling R55.2). Section 2 says fifteen. Section 3's table names nine rows moved whole (A5.132 to pass 60; C3.22, C6.1, D1.81, D2.18, D2.35, D3.11, D3.51, and D3.52 to pass 105) and C5.33 to pass 110; the others it counts are rows built in part whose remainder moves. The inventory has each row's own pass.

**Departures of sections 7.1 to 13.1 that the reviews closed,** and so have no row: the second reading of the 84 transcription rows (section 7.1), made by the referee's first half; the possible leak of a crew's Morale Level in a pending choice's resume (section 11.1); the filter for A7.55 that stood outside Rules (section 10.1); the projector's check of a recorded Motion attempt, which now applies the planner's bars (section 12.1, in part: the count and order of its checks are still Play's, section 14.3). Section 10.1 said that no test reaches a pending choice in one attacked Location of a Canister shot; `ACanisterShotResumedAfterAnOwnersChoiceCallsNoSniper` now does.

### 14.2 Fixed

| Review | What was wrong | What was done |
|---|---|---|
| Referee, second half, 1 (BLOCKER) | A hidden vehicle's Motion attempt was written where the enemy reads it: the bar and the facts never read the Hidden condition, and the events had no visibility. | A Hidden vehicle may not attempt Motion (A12.32, p. 80: "Hidden units may not move (even within their hex or to change a CA)"; ruling R55.32). The same bar holds a recorded attempt at replay. |
| Referee, second half, 2 | A concealed vehicle kept its "?" after a passed Motion attempt. | It loses it as a vehicle's own step does (A12.141, p. 78), with or without a change of CA (ruling R55.33). A failed attempt costs none (D2.401, p. 198). |
| Referee, second half, 3 | A Gun Duel declaration showed the DEFENDER vehicle's unrevealed Armor Leader: ruling R55.29 left out only the moving vehicle's. | He is left out of the declared shot's Gun Duel DRM as of the potential shot's. When each shot is fired his modifier applies and that record reveals him. |
| Referee, second half, 4 | A CE crew fired on from below by a vehicle's MG got Height Advantage on top of its CE DRM, which D5.31 (p. 203) and B10.31 (p. 125) do not allow. | Dropped for a vehicle's fire and for Canister, both new to the pass. The same line for Infantry fire is older than the pass and is left, with a backlog row. |
| Referee, second half, 5 | Pending-choice resumes held a crew's Morale Level before the Armor Leader is revealed (the leak section 11.1 named). | The resumes are written without it and read it again from the state. |
| Referee, second half, 6; Rules boundary 1 | The projector did not hold a recorded Motion attempt to the bars the planner applies. | One Rules member serves the planner and the record's check. |
| Rules boundary 3 | Whether a duel's return shot is void was decided in Play, by any refusal of the shot. | A Rules member decides it from the firer's facts (eliminated, broken, stunned, shocked, the Gun gone or malfunctioned). The projector refuses a void recorded for a firer still able to fire. A refusal for another cause is recorded as `other` with its reason. |
| Rules boundary 4 | A passed Motion attempt that named the TCA it already had lost the Acquisition. | Only a TCA that changes loses it (C6.5, p. 174). |
| Rules boundary 7 | Three filters outside Rules passed over a Canister fire record for A7.55. | One clause inside the Rules members; the callers hand over every record of the phase. |
| Rules boundary 11 | An undecided LOS at the mover's starting Location was read as no LOS, which allows the attempt. | The attempt is refused when it is undecided. |
| Referee, first half, 1 | Sniper checks on a Canister shot depended on the path: none in the plain shot, some after an owner's choice. | A Canister shot follows ordnance: none (ruling R55.30). |
| Referee, first half, 3 | The adjacent hex was attacked, and the round spent, when no Location of the cluster was. | A cluster Location must be attacked first (C8.41, p. 178), and an aim with no cluster hex on the map is not offered. Decided in Rules. |
| Referee, first half, 5 | AP or APDS named at Infantry by the 6-Pounder was told to name HE. | It is told that AP against Infantry (C8.31) is not built. |
| Referee, first half, 2 | A game with a year and no month gave no reason for an ammunition that starts in a month of that year. | Such a number is not available (ruling R55.31); the note and the refusal say why. |
| Table player 1; UI 9 | A shot that had no round was recorded as a "miss" that "may fire again (C2.24)"; the Depletion line was worded from today's catalog. | "no shot: it had no APDS" and "it has not fired and may fire other ammunition (C8.9)". The Depletion Number is carried in the resolution; an APCR or HEAT record made before the pass keeps its words exactly. |
| Table player 2, 4, 5, 6, 7, 8, 10, 11; UI 8 | "Case Q, height-advantage"; the Motion attempt list named the vehicle twice and put a whole refusal in an option; the Motion record did not say who spent the MP, and gave a hidden vehicle's hex; an empty Location's Canister record read as a concealed target; the Bog Removal proposal and the morale words had nested parentheses; the ammunition note shouted "CANISTER" and cited C8.9 for a date; one refusal covered three faults; capitals and three sentences. | Each reworded: "Case Q, Height Advantage"; short reasons, with the sentence in the option's title; the record names the mover and whose MP they were; "no Known unit"; the kind's own name with C8.11, C8.3, or C8.8; the elevation case has its own refusal with the numbers ("3 levels apart at range 2"). |
| Table player 3 | Three games' labels promised what their files do not hold. | Relabelled by the session; the spoiled copies marked SUPERSEDED. |
| UI 1, 2, 3 | The placement form's Armor Leader and Inexperienced Crew, the Motion attempt's three choices, and the Motion options' cache outlived a hand-over or a change of game. | Cleared. |
| UI 4, 5, 6, 7, 10 | The ammunition list followed "the Location holds a vehicle" while the shot followed "a vehicle is chosen"; a Canister aim was sent without checking it is among the aims shown; Canister lookups ran uncached on every render; an aim's "N units" counted an AFV and left out the "?" stacks; the form let an Armor Leader and an Inexperienced Crew be set together. | Both follow the chosen vehicle; the aim is checked; the lookups are read once; the count leaves out vehicles and names the concealed stacks; each of the two disables the other with its reason. |
| UI 11; table player 9 | The replay title of a fought Gun Duel named only the first shot. | It names the return shot too. |

**Two findings came after the five reviews, and both are fixed.** The fixing agent found that the answer to a duel's choice showed the DEFENDER's unrevealed Armor Leader in the declared shot's summary: the leak was real, and both answers now word the declared shot without his line (`ScenarioA1ArmorLeaders.ToldToTheEnemy`); the shot is still fired with his modifier, and its record reveals him. And the documents' agent found that C13.8's bar (p. 185) was missing: with levels opened, a PF or PSK, which C2.6 frees of its limit, could fire at a target two or more levels higher in an adjacent hex. It is refused there and at a higher Location of its own hex, with the rule in the refusal (`BackblastElevationRefusal`); an ATR is not barred. Before the pass every ordnance shot between levels was refused, so no record holds such a shot. A LATW fired down within its own hex stays refused, which is stricter than the rules; it has a backlog row.

**Answered, not a row.** The referee's first half, finding 6: a Canister shot that malfunctions on a DR above the Depletion Number removes the Gun's Acquisition. It is right as built: C6.5 (p. 174) takes the Acquisition when "the Gun malfunctions".

**The four checks the referee's second half could not make,** answered by the fixing agent from the code: a normal Bounding First Fire shot at the same MP expenditure after a fought duel is refused, since the duel's shot is that step's shot; no duel is offered between an OVR's resolution and the next expenditure, since the OVR records the MA as fired at that step; a Passenger is never among the movers of a Motion attempt, the vehicle alone is; `asl.game.motion-attempt` is admitted only from the side whose vehicle it is and only for a vehicle that is not phasing, and the adjudicator's view is unchecked, as elsewhere.

**Records of the pass made stale by a fix.** Three kinds, none of them in a game the session recorded: a Motion attempt by a hidden vehicle; a vehicle MG's or Canister's fire record against a CE AFV with Height Advantage or behind a wall; a later MA shot after a Motion attempt that named its present TCA. All 48 `p55-*` games of the live folder replay to their end with no diagnostic on the build with the fixes (`pass55/own_replay.sh`).

### 14.3 Not fixed: a row each in the backlog's section 59

| Review | What |
|---|---|
| Rules boundary 2; referee, first half, 4 | A Canister fire record is not held to its shot at replay: its halvings, Case A, and roll are taken from the record |
| Rules boundary 5 | The count and order of a Motion attempt's checks are decided in Play, and replay does not ask that a passed attempt by a red-MP AFV carries its DR (D2.401, D2.51, p. 198) |
| Rules boundary 6 | Which Locations a Canister shot attacks is partly decided in Play: the LOS and NVR skip, the Melee refusal, two facts set to pass Rules' own tests |
| Rules boundary 8 | Two Canister decisions in `LiveOrdnance`: a second MPh refusal, and the Acquisition zeroed before Rules sees it |
| Rules boundary 9 | When an Armor Leader is revealed is decided in the projector, by the fact's presence and not by the modifier's use |
| Rules boundary 10 | Smaller copies outside Rules: the PFPh bar on an Opportunity Firer in the Canister planner, the turret types twice more, what Reaction Fire is, the four grades in the placement editor |
| Referee, first half, 7 | C8.42's second sentence (p. 178): a Canister KIA and a SW. No SW is destroyed by any KIA in the code |
| Referee, second half, 4 (the other half) | Height Advantage on top of a CE crew's DRM under Infantry fire (D5.31, p. 203); older than the pass |
| The fixing agent | A Motion attempt does not count an expenditure made in a Location whose LOS is undecided |
| The fixing agent | A Passenger's time aboard is not read when it later moves on foot and a Motion attempt is made against it |
| The fixing agent | A `gun-duel-voided` of cause `other` is admitted at replay without the planner's precheck |
| Referee, second half (not checked by any review) | Whether a cliff hexside is stored on both hexes that share it, for a vehicle's MG firing down through one (B11.31, p. 126); whether B10.31's Crest Line exception reaches a moving vehicle on the Vehicle Target Type |
| UI 1, 2, 3, 5, 6 | No page test for these five fixes |
| (the session) | A LATW fired at a lower Location of its own hex is refused as out of range, though neither C13.8 nor C2.6 bars it. Stricter than the rules; found with the C13.8 fix, and not tried in a game |

### 14.4 The Studio check

In my Studio on port 6671, built into `bin/p31b` after the reviews' fixes, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320), in each side's view and the adjudicator's, each control measured for anything past either edge and the page for being wider than its window:
  - `p55-w-can`: the 37mm AT Gun's ammunition list, the Canister facing and aim lists, and the propose button, in the American view and the adjudicator's; the German view shows none of them.
  - `p55-w-apds`: the 6-Pounder's ammunition list with its Depletion Numbers, the target and the vehicle lists, in the British view.
  - `p55-w-mot`: the "Motion attempt (D2.401)" block with its vehicle, VCA, and TCA lists and its button, in the Russian view and the adjudicator's; the German view shows none of it.
  - `p55-w-duel`: the Gun Duel's pending choice with its two options, in the German view and the adjudicator's; the Russian view shows the choice's text and no option.
  - the placement form's Armor Leader list and Inexperienced Crew box, at 1920 and at 320.
  Nothing past an edge at any width, and the page never wider than its window. Under 1024 pixels the controls were read in the Actions tab.
- **A Player Turn at 320** (`p55-w-mot`, the adjudicator's view): the T-34's Motion attempt with its VCA turned west, its MA fired from Motion (Case C +4, Case J2 +4, the lower dr doubled), the DEFENDER's pass, the German tank's Stop MP, the end of its move, and every phase to the Russian Rally Phase. The page stayed 320 wide at every step, and the T-34 still carried its Motion counter in the Russian RPh.
- **By the keyboard:** the pane was on screen in this session. In `p55-w-can` at 1920 the propose button and then the confirm button were given the focus and pressed with a real Enter key each; the Canister shot at aim 4 was proposed and committed that way. A real Tab key moved the focus on the new-game page. The lists were set by script, not by arrow keys.
- **The reviews' fixes, seen:** a shot with no round reads "no shot: it had no APDS ... it has not fired and may fire other ammunition (C8.9)" (`p55-w-apds`); a Canister aim reads "2 units in view, 1 concealed stack" and no longer counts the tank; the Motion record reads "attempts Motion as vehicle G1 moves: vehicle G1 has spent 1 MP in its LOS"; the Gun Duel's choice is one sentence with both totals; the placement form shows the Armor Leader for a tank only, and a chosen leader disables the Inexperienced Crew box with "an Inexperienced Crew is its own 6+1 Armor Leader (D3.45)".
- **All 48 `p55-*` games of the live folder** were replayed on the build with the fixes: each to its end, none with a diagnostic.
- **Not done:** `p55-fix-hidden` does not stage its case: the engine reveals a hidden vehicle when an enemy moves into its LOS, so the T-34 was no longer hidden when the attempt was offered; ruling R55.32's bar rests on its Rules and Play tests. No Studio game holds an OVR or a vehicle's Close Combat attack with an Armor Leader, a vehicle's MG at another level of its own hex, a cliff, or a PF or PSK fired at another level. The Replay page was not opened, though its new steps are in its code and its tests. The 683 and 320 widths were measured for overflow and not looked at one by one beyond one screenshot at 320.

## 15. As built

Seven tasks, in seven commits after the frame's, then the reviews' fixes. The plan's six are built; the seventh, Gun Duels, was carried in from pass 50 and is a slice.

| Task | Rules members | Play and Units | What a player meets |
|---|---|---|---|
| 55.1 | `ScenarioA1ArmorCalculator.ParseAmmunition`, `Depletion`; `ScenarioA1Definitions.Ammunitions`, `EliteAmmunition`, `DepletionWords`; the `apds` listings and Case D row in `ScenarioA1ArmorReference` | the ammunition `apds`; the special rule `elite-ammunition:<side>`; `AmmunitionUse` and `DepletionNumber` on a resolution | APDS for a Gun that lists it at the game's date; HE and AP by a Depletion Number; the three outcomes of C8.9 on the Infantry and Vehicle Target Types; one more for an Elite force; the ammunition list with each number and the reason for a kind left out. |
| 55.2 | `ScenarioA1CanisterCalculator`: `Aim`, `ArcSides`, `Attacked`, `Firepower`, `Multipliers`, `CaseA`, `AttackFact` | `GamePlanner.CanisterAims`, `PlanFireCanister`; the shot's `aim` and `aimFacing`; a fire record for each Location on the shot's one DR | A Gun that lists Canister fires it at one of five aims of its CA, with no To Hit DR: one IFT DR for every Location of the cluster and the adjacent hex, which is also the Depletion, malfunction, and ROF check. In the MPh it is refused. |
| 55.3 | `ScenarioA1MotionAttempts`: `Bar`, `Mover`, `HalfPointsInLos`, `Number`, `Succeeds`, `Hexspines`, `TurnsMainArmament`, `ChangeBar`, `RecordRefusal`; `ScenarioA1OrdnanceCalculator.MotionFirer` | `asl.game.motion-attempt`; `GamePlanner.MotionAttemptOptions`, `PlanMotionAttempt`; the event `motion-attempted`; `GameState.MotionAttempts`; the projector's `AttemptMotion` | A DEFENDER's vehicle attempts Motion against a mover that began out of its LOS, by a dr against what the mover has spent in its LOS, and on a pass takes a new VCA and TCA. A vehicle in Motion of the side that is not phasing fires its MA with Case C and the lower dr doubled. |
| 55.4 | `ScenarioA1OrdnanceMapRules.ElevationAllowed`, `ElevationRefusal`, `OwnHexLevelRefusal`, `CliffRefusal`, `HeightAdvantage`; `ScenarioA1OrdnanceCalculator.HeightAdvantageApplies` | `heightAdvantage` on an ordnance shot; `heightAdvantage` and `targetLevelAbove` on a vehicle's fire attack | A Gun, a tank's MA, a mortar, and a vehicle's MGs fire at a Location of another level when the range is at least the elevation difference. A target above the firer takes Height Advantage as Case Q +1, and is then free of Case J4. |
| 55.5 | `ScenarioA1ArmorLeaders`: `IsGrade`, `SetupBar`, `Leadership`, `CrewMorale`, `Modifier`, `BogRemovalDrm`, `Lost`, `Shown`, `Recorded`, `Pending`, `ToldToTheEnemy` | `ArmorLeader` on a vehicle instance; the condition `asl:inexperienced-crew` | An armed AFV is given an Armor Leader of 8-1, 9-1, 9-2, or 10-2, or an Inexperienced Crew, at setup. His modifier is on the MA's To Hit DR, the IFT DR of a MA that is a MG, the OVR DR, the vehicle's own CC attack, and Bog Removal; his morale is the crew's. The enemy learns of him at his first use. |
| 55.6 | `ScenarioA1OrdnancePackage.Color` | the catalog at 1.14.0: `british-at-gun`, `american-at-gun`, `british-crew`, `american-crew` | The OQF 6-Pounder and the M3A1 37mm AT Gun with their crews. British To Hit numbers are black; American ones red before 1944 and black from 1944. |
| 55.7 | `ScenarioA1GunDuels`: `Classify`, `Drm`, `Verdict`, `ReturnShotVoid`, `ShotRecordRefusal`, `VoidRecordRefusal` | `GamePlanner.GunDuelDeclaration`, `PlanGunDuelAnswer`, `AddGunDuelReturnShot`; the choice kind `gun-duel`; `GameState.GunDuel`; the event `gun-duel-voided` | A Defensive First Fire shot declared at a moving vehicle whose Gun Duel DRM is lower waits for the ATTACKER's answer: `decline`, or `duel`, in which the vehicle's MA fires first as Bounding First Fire and the declared shot follows unless the first voided it. |

**New in a game's record.** Each is absent from a record made before the pass, which is checked as it was.

- **On an ordnance shot:** `scenarioYear` on an American shot at Infantry; `heightAdvantage`; `elite`; `rolls.canister`, `facts.canister` (the aim, the facing, the Locations), `resolution.canister`, and a null `toHit`; `gunDuel`; `armorLeadership` on a vehicle firer's facts; `crewMorale` on a vehicle target, only when that crew checked.
- **On an ordnance resolution:** `ammunitionUse` on the Infantry Target Type; `depletionNumber`, added by the reviews' fixes (a record without it replays, and an APCR or HEAT record made before the pass keeps its words); `apds` and `he` among a Gun's depleted kinds.
- **On a fire record:** a vehicle's `heightAdvantage` and `targetLevelAbove`, which Infantry fire had; `facts.canister` and no firers on a Canister shot's record; `armorLeadership` on a vehicle firer; `crewMorale` on a vehicle, and `moraleSource` on a check.
- **On an OVR, a vehicle's Close Combat attack, and a Bog Removal's check:** `armorLeadership`.
- **On a unit:** `armorLeader` (a morale, a leadership, and whether he is revealed); the condition `asl:inexperienced-crew`.
- **Event types:** `motion-attempted` (the vehicle, the roll, the half points, the result, the mover, the facing, the turret); `gun-duel-voided` (the firer, the vehicle, the cause, and a `reason` when the cause is `other`, added by the reviews' fixes).
- **A choice kind:** `gun-duel`, with the declared shot's arguments and both totals; `gunDuelOwed` in a later choice's resume. A resume no longer holds a crew's Morale Level.
- **In the state:** `ordnance` on a fire of the phase; `motionAttempts`; the Gun Duel in progress.
- **An action and its arguments:** `asl.game.motion-attempt`; on `asl.game.fire-ordnance`, the ammunition `apds` and `canister`, with `aim` and `aimFacing`. **A special rule of a game:** `elite-ammunition:<side>`.

**The replay digest over the proof store** (473 games, 93,266 lines) was run after each of the seven increments. The base was built from main's sources at 846eb5a3. The catalog's identity, which is hashed into every state and went to 1.14.0, was left out on both sides, and the pass's new properties were left out. No line differed in any of the seven runs. The first trial, without the catalog's identity left out, differed in every state's line and was a void run (section 8.3). The store holds none of the pass's own games, so this shows that nothing recorded before the pass replays differently, and nothing more. The run after the reviews' fixes (99456de6), with the pass's 144 new properties left out: no line differs. All 53 `p55-*` games of the live folder replay to their end on that build with no diagnostic.

**Rulings:** R55.1 to R55.33, every one decided under the user's word of 2026-10-12 and open to a veto. All are in the Passes Plan, section 5.

- R55.1. The pass stays one pass at about twice the plan's size, built in section 2's order with Gun Duels last.
- R55.2. The placement of the 35 rows (section 3): A5.132 to pass 60, the rest of a vehicle's handling of its weapons and arcs to pass 105, the duel's remainder to pass 110.
- R55.3. Four definitions, every value printed in the book: the OQF 6-Pounder, the M3A1 37mm AT Gun, a British and an American crew. The catalog becomes 1.14.0; what names 1.13.0 is left.
- R55.4. The notation `<type><number>[^<year digit>[m<month>][+][E]]`, the month as a number; `E` is kept and not read.
- R55.5. The Supply Chart of C8.12 is a test's data, not a rule the game reads.
- R55.6. Elite (C8.2) is a special rule of the game, `elite-ammunition:<side>`. It supersedes R7.6.
- R55.7. C8.9's three outcomes apply to every ammunition with a Depletion Number; a Gun whose HE has one never fires HE undeclared (C2.21).
- R55.8. The five aim points of a CA are the vertices whose three hexes all lie in the CA at range 2 or 3.
- R55.9. Canister is the ordnance shot with the ammunition `canister` and an aim: no To Hit DR, one IFT DR for every Location, each halving of C8.4 applied.
- R55.10. FFMO and FFNAM apply to Canister in Defensive First Fire (not built, section 10.1); an AFPh shot by a Gun that did not move is not halved.
- R55.11. `asl.game.motion-attempt`; the number is the sum of the MF or MP the mover's steps spent in the vehicle's LOS.
- R55.12. The LOS to the Location the mover began its move in decides "had not been in the vehicle's LOS"; the check is made first, and a refusal costs no attempt.
- R55.13. Who may attempt; the Bog Check of a VCA change and the Mechanical Reliability DR; the event `motion-attempted`.
- R55.14. A DEFENDER's vehicle in Motion fires its MA with Case C alone and the lower dr doubled (C5.35); a reading.
- R55.15. No Armor Leader definition: he is a morale and a leadership on his vehicle, of four grades, unseen by the enemy until the first record that uses him.
- R55.16. His modifier is always applied where D3.44 lets it be.
- R55.17. The crew's Morale Level is his in the two checks the code has, without his modifier; he is lost with a Recall, a wreck, and when the crew takes counter form.
- R55.18. An Inexperienced Crew is the condition `asl:inexperienced-crew`: plus 1 on the same rolls, a crew morale of 6, never lost.
- R55.19. The commit of a Defensive First Fire ordnance shot at a moving vehicle with the lower Gun Duel DRM is the declaration; the ATTACKER answers `decline` or `duel`.
- R55.20. Left to pass 110: the equal case, fire that is not ordnance, the second duel, a duel declared by the DEFENDER, a Gyrostabilizer.
- R55.21. The order of the increments and their estimates (section 2).
- R55.22. The chart's To Hit color is read for a Gun and a vehicle's MA; a light mortar and a LATW keep the color they had.
- R55.23. C2.6's limit holds for a vehicle's MA AAMG as for its BMG and CMG.
- R55.24. A target behind a wall or hedge the LOS crosses claims no Height Advantage on a To Hit DR.
- R55.25. A shot whose special ammunition was not there leaves no fire counter of any kind, the Intensive Fire counter included.
- R55.26. A Location of a Canister aim is attacked when the firer has a LOS to it; no further test is made of the cluster's first hex.
- R55.27. A record carries the morale of a target's crew only when that crew took a check in it.
- R55.28. A Motion attempt against a mover that has spent less than one MF or MP in the vehicle's LOS is refused and is no attempt.
- R55.29. An Armor Leader not yet revealed is left out of a Gun Duel DRM when a duel is offered and in what is shown of it; extended by the reviews to the declaring vehicle's leader.
- R55.30. A Canister shot calls no Sniper, as ordnance does not.
- R55.31. A number that starts in a month of the game's year is not available when the game gives no month.
- R55.32. A Hidden vehicle may not attempt Motion (A12.32).
- R55.33. Gaining Motion costs a concealed vehicle its "?" whether or not its CA changes (A12.141, "any other activity").

**Tests after the reviews' fixes** (the fixing agent's runs): Rules 220 in nine classes; Play 184 in eleven; Units 416 whole; MapStudio 75. The four test projects build with warnings as errors, 0 warnings; the MapStudio build ends with exit 0. The pass's own classes are `ScenarioA1Pass55RulesTests` (60 test methods as this is written) and `BacklogPass55Tests` (69). The Rules and Play projects were not run whole: that is the gate's.

**The gate** is section 16.

## 16. The gate

Run on 2026-10-13 at the user's word ("start the gate and stop before the merge"), by `gate.sh` in its first full run. The baseline is main at 846eb5a3, built from its own sources; the proof store went from 473 to 504 games with the pass's 31 recorded games. **The gate passed.** `origin/main` moved during the gate to 681e8bdb by one commit, 14 files, all under `prototypes/europe-hex-map`: nothing of the ASL solution and no file of the branch.

In the same message the user answered the pass's report: "All open proposals are approved." The departures of section 14.1, rulings R55.1 to R55.33, and pass 50's rulings R50.1 to R50.14 with its design's section 10.1 are no longer open to a veto.

| Step | Result |
|---|---|
| The solution build with warnings as errors | **Failed at the first try: 450 errors in four of the pass's test files** (440 of IDE0055, six of CA1861, one each of CA1822, CA1875, xUnit2013, xUnit2029). Fixed in 8862d209, a change to test files only; 0 warnings and 0 errors at the second try. See below. |
| The trial | Passed: five older games, 230 replay lines and 1,441 sweep lines equal on both sides. |
| The whole local suite, from `bin/gate` | Dice 24, Rules 897, Units 416, Maps 244, Maps.Vasl 121 (30 skipped), Maps.Rendering 34 (2 skipped), Units.Rendering 352, CounterSheets 19, MapStudio 395, Play 819, Authoring 167: none failed, each at its first run. Rules and Play ran whole for the first time on this branch. |
| CI's Node viewport test | 5 of 5. |
| The chart supplement | Equal. |
| The text list | 1,336 texts on main, 1,376 on the branch; 70 lines differ. Each is a text the pass added (Canister, the Motion attempt, the Gun Duel, fire at another level, C13.8, the Armor Leader's and the Inexperienced Crew's setup bars) or a raised count of a code it uses again. One literal is gone, `asl.a1.fire.levels-differ`, which task 55.4 removed. |
| The replay digest | 93,401 lines on main, 93,732 on the branch. No line of a game made before the pass differs. 387 lines differ, all in 28 of the pass's 31 games: main cannot read five of them (a `motion-attempted` or `gun-duel-voided` event) and stops the others with a diagnostic at their first record of the pass. `p55-al-bog`, `p55-al-shock`, and `p55-duel-none` replay alike on both sides. All 31 replay to their end on the branch with no diagnostic. `p35-acquire` carries its known diagnostic on both sides. |
| The planner sweep, 17 games, 515,357 lines on main and 516,111 on the branch | 758 lines are new, all in the four games of the pass in the sweep, of which main brings none to a state. 2,511 lines of games made before the pass have the same text and another hash; they are read below, and with their one cause left out on both sides no line of the 515,357 differs. |
| Docker, Linux | Passed on 8862d209 at the first run: eleven projects, 3,488 tests, the two CI checks. |

**The 2,511 differing lines of the sweep** are plans of `p31c-tw` (2,500), `p25-visual` (7), `p35-mud` (2), and `p35-smoke-hill` (2): moves, drops, fire, advances, dismantlings, rallies, repairs, routs, and one Ambush, each with the same status and the same words on both sides. A full dump of one game's planned events from each side (`pass55/dump_events.sh`, `PROOF_DUMP_EVENTS`) showed the one difference: the event's rule package reference. Increment 2a pinned the four packages again with the catalog (fire, ordnance, rally, close combat), so their digests changed, and every planned event carries its package's digest. The sweep was then run again on both sides with `GameEvent.RulePackage` left out as well (`pass55/sweep_nopkg.sh`): first eleven games, 33,392 lines, where 693 lines had differed and none did; then all 17, where no line of a game made before the pass differs and the same 758 are new. This is the second thing of the kind `BOTH_SKIP` was written for, and `pass55.conf` named only the first.

**What the sweep does not show.** A Canister aim, a named ammunition, a Motion attempt, and a duel's answer are asked for only with arguments the sweep does not send, and main has no answer to compare with. For those the proof is the pass's tests and Studio games. What the digest and the sweep show is that nothing outside the pass's ground moved.

**A false sentence of section 15 and of the review, corrected here.** Both said "the four test projects build with warnings as errors, 0 warnings". No log backs it. The pass's test script, `pass55/test.sh`, builds the test projects with `TreatWarningsAsErrors=false`, and the one strict build of the session, the Studio's, leaves the test projects out; the sentence came from an agent's report and was written down unchecked. The lesson that the strict build belongs before the documents commit was written after pass 45 and was a line of prose with no script behind it.

**What went wrong in the gate itself.**

- **The baseline's sweep ended in a null reference after 53 minutes and wrote nothing.** `SWEEP_EXTRA` named four of the pass's games; main cannot read `p55-mot-pass`, the replay mode passes over such a game and the sweep mode did not, and the tool writes its file only at its end. The trial did not meet it: it sweeps older games only. The check that would have, the pass's four games on main's side, took six seconds afterwards. The tool now writes `UNREADABLE` and goes on; the second run took 2,065 s.
- **`gate.sh` wrote the baseline's DONE marker after the crash,** so the proofs stage started against a baseline with no sweep. The second sweep ended before the gate's own, and the stage's comparison found it.
- **The stages ran side by side** (the baseline, the suite, Docker, then the proofs), and each ran two to three times as long: the baseline's replay took 1,855 s against 682 s alone, Rules 32 minutes against about ten. The last comparison ran both sides alone and together in 1,066 s each.
- **The proof tool was changed in the gate:** the sweep's guard above; `PROOF_DUMP_EVENTS`; and `GameEvent.RulePackage` as a name `PROOF_SKIP` may hold, since the events' digest wrote the reference outside the property walk. The replay digest and the stage's own sweep were made before the last two changes, which alter nothing unless asked for.
- **The gate by the clock:** 14:47 to 17:32, 2:45. About 13 minutes went to the failed build, 53 minutes of machine time to the lost sweep, and 25 minutes to the last comparison.

**After the gate.** Merged into main as 50987ff3 with `--no-ff` in a temporary worktree and pushed on 2026-10-13 at the user's word ("merge and push"); the merge's tree under `src/ASL` is the gated branch's. **The CI run of the merge failed, for a reason outside the pass:** it stopped at the locked restore, before any build or test, with NU1004. The workflow asks for .NET `10.0.x`; the October servicing release came out between two pushes, the run installed SDK 10.0.402, whose `Microsoft.AspNetCore.App.Internal.Assets` is 10.0.13, and MapStudio's lock file held 10.0.12 from SDK 10.0.401, which the green run of 20 hours before had found on the runner. The user chose to move to 10.0.402 rather than pin the workflow: the SDK was installed on this machine, the one lock entry regenerated, a locked restore of the solution run with it, and the file pushed as 53a22fcf. That run's restore passed; the rest of it was left for the user to read. The Docker check's image, `mcr.microsoft.com/dotnet/sdk:10.0`, was still at 10.0.401 that evening and fails its locked restore until its next image is out (backlog 59.2).

**The repairs, made after the merge** at the user's word, in the tools folder (`pass32-tools/gate`, `proof/Program.cs`):

- The proof tool writes its output a game at a time and turns a game's exception into an ERROR line; the replay passes over cut files; a name with a leading `=` matches one game; `PROOF_EXCLUDE` leaves games out by name.
- `gate.sh` has a `base` stage, which makes the baseline of the games made before the pass once and keeps it under a key (the base commit, the older games, `BOTH_SKIP`, the sweep list, the tool), and a `baseline` stage that reuses it and adds only the pass's own games. A stage stops unless the tool exited 0, wrote its file, and no game ended in an ERROR line.
- The trial holds the pass's own games and its extra sweep games on both sides, and two cuts of `p31c-tw`.
- A rule pass sweeps `p31c-tw` at each of its eight phases at four points of the game, 32 cuts of 127 (`pick_cuts.py`); `CODE_MOVE=1` in a pass's `.conf` sweeps them all.
- A `strict` stage formats the pass's changed test files and builds the solution with warnings as errors, and `strict-check` says whether that passed for the tree as it stands.
- The order of the stages: the baseline, the build, the trial, the proofs and Docker side by side, then the suite. It is to be timed at one gate before it is called settled.

**The repaired gate, run on this pass's own data** the same evening as its first test, with `GameEvent.RulePackage` added to the pass's `BOTH_SKIP`:

| Stage | At the gate | After the repairs |
|---|---|---|
| The baseline's replay digest | 682 s alone during the build, 1,855 s beside the suite; 93,401 lines | 76 s; 20,226 lines, the cuts passed over |
| The baseline's sweep | 2,065 s beside the suite, 1,066 s alone; 515,357 lines | 344 s; 135,940 lines, 32 cuts of `p31c-tw` |
| The baseline, whole | about 90 minutes, with the lost sweep | 11 minutes with main's sources built again; reused after that |
| The trial | passed, and met neither the unreadable game nor the packages' digests | about 90 seconds, with the pass's 31 games and its four sweep games on both sides |
| The proofs | 49 minutes, and 25 more for the packages' digests | 7 minutes: replay 66 s, sweep 323 s |
| The strict stage | (none) | 4 minutes: 19 changed test files formatted with no change, 0 warnings, 0 errors |

The result is the gate's own: 70 lines of the text list, the same 28 of the pass's games in the digest, no line of a game made before the pass in the sweep, and 758 new lines. The suite and Docker were not run again, and no whole gate has been timed in the new order. The CI run of 53a22fcf passed in 13:55, the first CI run of the pass's code. Main moved once more that evening, to d5c81e8d, by four files under `prototypes/europe-hex-map`.

**What the user approved during the gate, as it was put, to be made after the merge and the push** (the user's word: "Make these changes after we have merged and pushed Pass-55"): the trial covers every game the long runs touch; the tool writes as it goes and turns a game's exception into a line; no DONE marker without the files; the baseline is made once and reused; proofs and Docker together, then the suite; the replay passes over cut files, which are 78% of its lines and repeat the games they are cut from; a rule pass sweeps `p31c-tw` at each of its eight phases at four points of the game, about 32 cuts of 127; a strict build script beside `test.sh`, which the documents' checklist requires; and a line in the standing rules that an agent's report is not evidence of a build or a test.
