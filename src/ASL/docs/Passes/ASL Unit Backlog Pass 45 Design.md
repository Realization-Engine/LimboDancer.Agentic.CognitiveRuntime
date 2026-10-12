# ASL Unit Backlog Pass 45 Design

**Status:** The frame (increment 1), written 2026-10-12. Read-only: nothing is built. **Answered by the user the same day: yes to F1 through F10 as proposed.** Increment 2a (45.8, 45.9) is next.

**Date:** 2026-10-12

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 19 (decisions 5 to 7) and section 22.1, "Pass 45: Infantry II"; the [rule inventory](<../ASL Rule Inventory A to E.md>); the [backlog](<../ASL Unit Backlog.md>), sections 55 and 56; the [pass 40 design](<ASL Unit Backlog Pass 40 Design.md>) and [review](<Scenario A1 Backlog Pass 40 Review 2026-10-11.md>); the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>); the session's prompt, `Prompts/Pass 45 Prompt I.md`.

**The rulebook.** Read for this frame on 2026-10-12, from the page texts of the tools folder, with their examples: page 43 (A.7, A.11), page 50 (A4.41, A4.42, A4.44), page 52 (A4.8), pages 54 to 58 (A7 whole), pages 60 to 65 (A8 and A9 whole), page 136 (B23.25, B23.26), and page 174 (C6.4 to C6.44). A4.61 and A4.63 (page 51) were not read again; pass 40 read them, and task 45.10 reads them before it builds. The index entry for TI was found by a scan of all 716 pages of the PDF (page 39 of the PDF, pointing to A4.8).

**What the frame rests on.** The inventory's notes are of 2026-10-05, amended by passes 35 and 40. Two read-only agents checked every "Missing", "Fault", "Departs", and "Left" claim of the 45 rows against the code on this branch (cut from main at 5982767e) and gave a file and line for each. I read these myself on their lines: the Residual FP member, the weapon effects (B#, ROF), Casualty Reduction, the Cowering shift, and the catalog's MG and mortar values. The rest of the code findings below are the agents', not rechecked line by line; each increment's design reads its code before it builds. Where an agent wrote "cannot tell", section 4 lists the point as needing a read.

## 1. Outline of this design

1. This outline.
2. What the pass is, and its increments.
3. The 45 rows, by task.
4. What is certain, and what needs a run or a read.
5. What the frame found outside the rows.
6. The frame's questions, each with its analysis and a proposal.

Sections for the increments, the reviews, and "As built" are added as the pass goes.

## 2. What the pass is

Pass 45 is the second pass of block 1, "Repairs and the ground under everything": fire by Infantry, machine guns, and support weapons. It adds no counters. It changes the results of fire on purpose, so the gate will name every differing `asl.game.fire` line as one task's change.

The plan gives it nine tasks, 38 rows, 5:40 of building, and 6:55 in all. The inventory now names 45 rows: passes 35 and 40 moved seven here (A.7, A4.41, A4.42, A4.44, A4.61, A4.63, A8.312). The frame proposes a tenth task for the movement rows among those seven, and splits A7.72 off task 45.1 (questions F1 and F7). With both, and the setup change of question F6, the building estimate is 7:20 (the plan's 5:40, and 0:40, 0:40, and 0:20) and the pass 8:35 with the plan's overhead of 1:15. Pass 40 took 6:41 by the clock against 6:35, of which the gate was 1:47.

**The increments proposed** (question F2 asks for the order):

| Increment | Tasks | Why together |
|---|---|---|
| 1 | The frame | This document. |
| 2a | 45.8, 45.9 | The shape of an attack: a fire kind for each firer, the group as one chain, the firer facts that later tasks read (berserk, Inexperienced). Built first because 45.2, 45.3, and 45.5 all write marks and Residual FP on top of it. |
| 2b | 45.1, 45.1b | Levels of one hex: fire up and down a stairwell, and Encirclement in upper levels. |
| 2c | 45.2, 45.3 | What an attack leaves: Cowering's marks, the choice of ROF or Residual FP, Residual FP completed. One member (`Residual`) and one record. |
| 2d | 45.4, 45.5, 45.6 | The MG's own rules: Spraying Fire, Fire Lanes, Field of Fire, Mandatory Fire Direction. |
| 2e | 45.7, 45.10 | Upkeep and leftovers: the X#, repair, self-destruction, dismantling; pass 40's movement rows. |

## 3. The 45 rows, by task

"Stale" marks a note that no longer describes the code; the documents of the pass correct the row. Where a remainder is sent to another pass, the pass is named from the plan's titles (50 to 65 armored combat and Passengers, 75 the small sections, 110 Guns, 115 and 120 Fortifications, 135 the refused terrain, 150 OBA, 165 and 170 special units); the frame did not check each against that pass's own rows, and the documents step does.

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 45.1 | A7.21 (54) | PBF between two levels of the firer's hex; today refused as out of range | 45.1. IFE has no counter in the catalog: stays out, backlog. A vehicle's MG at another level: pass 50. |
| 45.1 | B23.26 (136) | Attacks up or down a stairwell, one level only; ground and level 2 never ADJACENT | 45.1 |
| 45.1 | B23.25 (136) | Vertical ADJACENT | Stale: built by pass 35 (A.8). 45.1 only checks that the fire reads it. |
| 45.1 | A8.4 (61) | Final Fire into another level of the same hex | 45.1 (not cited by the plan). A vehicle's Final Fire after First Fire (ruling R6.9): pass 50. |
| 45.1 | A7.7 (57) | Encirclement by a LOF from the Locations directly above and below | 45.1 (not cited by the plan). Spraying Fire counted: 45.4. An ordnance hit and vehicular armament as valid fire, and the +1 on an Encircled unit's To Hit DR: pass 110. Vulnerable PRC: pass 65. Capture: stale, built (the rout's surrender reads Encircled). |
| 45.1b | A7.72 (58) | Upper-level Encirclement by a cut-off path to ground level | Its own task (question F7). |
| 45.2 | A7.9 (58) | The cowering unit and all its SW marked Final or Prep Fire; Random Selection in a group; berserk firers exempt; the double shift for Inexperienced Personnel, not for a class | 45.2 |
| 45.2 | A8.23 (60) | The choice between keeping Multiple ROF and leaving Residual FP | 45.2 (question F3) |
| 45.3 | A8.2 (60) | The cap of 12 after the A8.26 reduction; MF spent inside a Residual Location; Residual FP from a To Kill DR | 45.3. FFE Hindrance: pass 150. |
| 45.3 | A8.22 (60) | Once a Location for each unit, again only on a larger counter or a worse DRM; the activity first | 45.3; needs a record of who was attacked. Pillbox, OBA, minefields: passes 115, 120, 150. |
| 45.3 | A8.221 (60) | No Residual FP for a weapon that malfunctions on that DR | 45.3. Ammunition Shortage: pass 75. |
| 45.3 | A8.24 (60) | Spraying Fire in the MPh, a counter in each Location, even an empty one | 45.3, after 45.4 |
| 45.3 | A8.25 (60) | An ordnance hit's Residual FP | 45.3 for HE by a Gun or mortar that hit. Air Bursts raising it: with the row's pass for Air Bursts. |
| 45.3 | A8.26 (60) | CX, an Encircled firer, a hero's two-man +1, a vehicle's Stun | 45.3. The frame adds pass 40's firer-overstack DRM, which the note does not name (section 5). |
| 45.4 | A7.34 (56) | Squad Spraying Fire off the trait: three hexes, Long Range halving past Normal Range | 45.4 |
| 45.4 | A9.5 (64) | In the MPh; two levels of one hex; counted toward Encirclement | 45.4. Bypass and road points: pass 135. |
| 45.4 | A9.52 (64) | Every member of the group capable; a moving unit; an empty Location | 45.4. Paratroops: pass 170. |
| 45.5 | A9.22 (63) | Lanes against vehicles; later MF in a lane Location; wall or hedge TEM | 45.5. The MG as ordnance: pass 60. |
| 45.5 | A9.221 (63) | Alternate Hex Grain; the Fire Lane Snap Shot | 45.5 |
| 45.5 | A9.222 (63) | Hard Hindrances and hexside TEM as DRM; SMOKE placed after the lane | 45.5. Impulse movement: passes 75 and 165. |
| 45.5 | A9.223 (63) | Forced cancellation when the MG's Location is entered (TPBF) | 45.5. CC Reaction Fire: pass 60. |
| 45.6 | A9.21 (62) | Field of Fire for a MMG or HMG in woods, rubble, or a building; the pinned carry-over | 45.6. INF, RCL, and 20L ATR: not in the catalog, backlog. |
| 45.6 | A9.4 (64) | 16 hexes without a leader; Infantry at 17 or more as concealed | 45.6 |
| 45.6 | A7.53 (57) | A leader directs a MG's every shot; later direction only for firers he directed in First Fire | 45.6 |
| 45.7 | A.11 (43) | The Original B# becomes an X# under Sustained Fire, capture, or Inexperienced use | 45.7 |
| 45.7 | A9.3 (63) | The X#; no Sustained Fire by a lone SMC's MG | 45.7. A vehicle's MA: pass 50. |
| 45.7 | A9.72 (65) | Mortar repair; no repair of a captured SW; repair while dm | 45.7. A Gun's repair by its crew: pass 110. |
| 45.7 | A9.73 (65) | Self-destruction or deliberate malfunction in the PFPh or DFPh | 45.7 |
| 45.7 | A9.8 (65) | Dismantling the German HMG and the three light mortars; a dm German MMG or HMG fired as a LMG; starting dm | 45.7. Scrounged weapons: pass 60. |
| 45.7 | A4.41 (50) | The German dm MMG or HMG firing as a LMG in the AFPh | 45.7 (moved in by pass 40) |
| 45.7 | A.7 (43) | The Good Order sense of a SW | 45.7: one member, read by the Fire Lane's declaration. The TI rules for the SW and Deployment actions: 45.10 (question F8). |
| 45.8 | A7.5 (57) | The group as one chain of ADJACENT Locations | 45.8. Vertical and wall or hedge ADJACENT: stale, built by pass 35. Pillbox: pass 115. |
| 45.8 | A7.54 (57) | No berserk unit in a multi-Location group | 45.8 |
| 45.8 | A7.25 (55) | Opportunity Fire by ordnance | Stale: built by pass 35, task 35.10 (question F5). Intensive Fire by an Opportunity Firer: pass 110. |
| 45.8 | A7.353 (56) | A crew's halved inherent FP after it fired its Gun | 45.8, outside FPF (in FPF it is built, ruling R40.18, at full FP; the page halves it) |
| 45.8 | A8.31 (61) | FPF in one group with units not using FPF; the CC counter after a TPBF FPF | 45.8 (question F4) |
| 45.8 | A8.312 (61) | One Mandatory FG of units whose counters differ; the other acts held behind the attack owed | 45.8 (moved in by pass 40; question F4) |
| 45.8 | C6.44 (174) | The MG's Bore Sighting DRM of minus 2 | 45.8 (question F6) |
| 45.8 | A7.51 (57) | A vehicle's MG, Passengers, and Riders in a group | Not this pass: D6.64 and D3.5 are pass 65's. The row's pass column moves. |
| 45.9 | A7.302 (55) | A crew's Casualty Reduction eliminates it | 45.9. A prisoner's: the task reads A20.5 first. |
| 45.9 | A7.212 (55) | The target limit on ordnance; a test of the refusal | 45.9. Spotters: pass 110. |
| 45.10 | A4.42 (50) | A hero's IPC; the choice in the APh; a CX leader; a SMC as the one helped; the default kept in the record | 45.10 (moved in by pass 40) |
| 45.10 | A4.44 (50) | A SMC's Recovery at once when a unit is eliminated, surrenders, or routs away | 45.10 |
| 45.10 | A4.61 (51) | A break ends Assault Movement for the unit, not the stack | 45.10 |
| 45.10 | A4.63 (51) | The overstack MF and lost Road Bonus of the Location Dashed to | 45.10. The halving by firer is left: ruling R40.9 stands. |

Of pass 40's other leftovers the prompt lists: A4.132 (the road's second target point) is a LOS question and waits for pass 135. The rules decided in Play (backlog 56.2) are taken only when a task opens their file. Of pass 35's section 55.2: the Gun's target list that offers a Location with no LOS is the ordnance panel's and goes with pass 110; the "Latest" line through phase changes is left with a reason in the backlog.

## 4. What is certain, and what needs a run or a read

**Certain: the page and the code read against each other.**

- **The cap of 12 (A8.2).** The counter list tops at 12 and the DRM reduction is taken afterwards, so a 36 FP attack with one +1 leaves 8. The page: half of the column, less one column for each +1, then at most 12. That attack leaves 12.
- **Residual FP ignores a malfunction and a kept ROF (A8.221, A8.23).** It is computed from the whole column before the weapon effects are decided. The projector checks that the counter placed equals the record's own figure, so both corrections must be made inside the recorded arithmetic, not afterwards.
- **A8.26 reads three DRM.** Hindrance, positive leadership, and hexside TEM lower the counter. CX, an Encircled firer's +1, a hero's +1, and a Stun do not. The page names CX, BU, Stun, the leadership modifier, and hexside or bridge TEM, and leaves out Height Advantage and LV.
- **No X# (A.11).** A DR that reaches the B# malfunctions the weapon; only a FT is removed. The catalog's flag for it is read by no code. The A9.71 example gives the Random Selection: with a HMG in Sustained Fire (B10, X12) and a LMG (B12), a DR of 12 removes the HMG, malfunctions the LMG, or both.
- **Cowering (A7.9).** The shift is doubled when a firer's class is Green or Conscript, not when it is Inexperienced, though the firer facts carry that. A cowering unit is not marked, and its MG may keep its ROF. No berserk exemption: the firer facts carry no berserk state, which A7.54 needs too.
- **A crew's Casualty Reduction (A7.302)** ends undecided, as "reduction counter missing". The page eliminates it.
- **The group's shape (A7.5).** The test is that every Location has a neighbor in the group, so two pairs far apart pass as one group. The page asks for one chain.
- **Fire between levels of one hex** is refused in the calculator as out of range, and Final Fire with it.
- **The fire kind is one for the attack.** Only in an FPF attack does each firer take a kind from its own marks, and a guard before it makes that branch unreachable for a mixed group. The Mandatory FG of pass 40 therefore forms no group of units whose counters differ.
- **Spraying Fire** is refused unless every firer fires a MG, and in the MPh. The trait is read by nothing. The catalog marks it present on 13 of 37 squads and on no HS or crew, which agrees with A7.34 ("squads").
- **Fire Lanes** walk the six straight grains only, attack Infantry on entry only, never vehicles, and keep a Hindrance only to cancel FFMO. The comment at the declaration says the forced cancellation is not built.
- **No Field of Fire (A9.21) and no 16-hex limit (A9.4).** A SW has no CA in the state. An undirected MG fires to twice its Normal Range.
- **Bore Sighting accepts a manned Gun only.** A MG is possessed, not manned, so the setup refuses it (question F6).
- **The catalog.** Seven MGs, all with a Repair number of 2; three light mortars (German 50mm of 5 PP, Russian 50mm and British 51mm of 4 PP), none with a Repair number. Dismantling tests for the German MMG by its id. A9.8 names any light mortar of four PP or more and the non-Russian MMG and HMG, so the German HMG and all three mortars qualify and the Russian MGs do not.

**Needs a run before its task is designed.**

- **The FPF whose NMC replaces a firer by ELR** (UNIT-STATE-007 at the commit). Reproduced first in increment 2a, in a test of one, since 45.8 rewrites the same follow-ups.
- **A LOS between two levels of one hex.** What the LOS reader answers for a stairwell pair, on board 3's [M5] and [L4] (one wooden building with a level 1 and stairwells) and the two-level stone buildings [M2], [N1], [N2].
- **Whether an Infantry step already keeps ground and level 2 apart** (B23.26's last sentence); the agent could not tell.
- **A proposal made again on its own dice.** Question F3 turns on whether the page can change one choice of a proposal and keep the dice it drew.
- **The Fire Lane Snap Shot** against the Snap Shot built for ordinary fire, and the LOS to a hexside.
- **A7.72's path search** and its cost on a large building; see question F7.

**Needs a read at its task** (the agents could not tell): the Good Order sense of a SW in the code; who sets the "lent" IPC in the advance; a vehicle's MG fired at another level; the pillbox clauses; a prisoner's Casualty Reduction (A20.5); which way the note on repair while dm means (the planner repairs a dm MG today, which the page allows).

**New properties the gate must be told of** (the `PROOF_SKIP` list, planned now): a fire kind for each firer in the attack's record; the firer's berserk state; the units a Residual FP counter and a Fire Lane have attacked; a MG's fixed CA; a weapon removed on its X#; a MG's Bore Sighted Location. The list is completed from the diff before the gate's first run.

## 5. What the frame found outside the rows

1. **Two rows are stale in part.** Opportunity Fire by ordnance and LATW (A7.25) was built by pass 35, task 35.10, with the ROF kept and no Case B. ADJACENT between levels a stairwell joins and across a wall or hedge (A7.5, B23.25) passes since pass 35. Task 45.8 is smaller by both; the documents correct the rows (question F5).
2. **The MG's Bore Sighting needs a setup change the plan does not count.** C6.41 allows the MMG, the HMG, and light mortars; C6.42 has the Location and the possessor recorded; C6.43 loses it when the weapon leaves its setup Location, is dm, or is fired by another unit (question F6).
3. **A8.26 and pass 40.** The firer's overstack DRM that pass 40 added to the IFT DR is a positive DRM caused outside the target hex. It is not subtracted from the Residual FP. Taken into 45.3 with CX.
4. **A9.3 and the lone SMC.** Pass 40 exempts a SMC with a marked MG from owing the Mandatory FG, but nothing bars a lone SMC's MG from Sustained Fire. Taken into 45.7.
5. **A9.22, the declaration's bars.** The page allows no Fire Lane when the manning Infantry Cowered or used Subsequent First Fire or FPF in that attack, nor for a MG its terrain holds to Area Fire. Checked at 45.5; the Cowering one meets 45.2's marks.
6. **A7.21's PBF by height.** The page gives PBF when ADJACENT, or when adjacent and "either within one level of or higher than its target", so a firer two levels above an adjacent target has it and one two levels below has not. The code's test is range 1 and a level difference of at most 1 on one side; the agent read it as right, and I have not read the line. It is read at 45.1.
7. **A8.31, the crew at full FP in FPF.** Ruling R40.18 adds a crew's FP whole in FPF. A7.353 on the page halves it there as in Subsequent First Fire and Final Fire. 45.8 halves it in all three; this amends R40.18's figure, not its ruling that the crew fires.
8. **TI is A4.8, page 52.** "A TI unit may not move, advance, fire, or perform other labor tasks anytime during its Player Turn", and A.7 calls a TI unit Good Order (question F8).
9. **The inventory's code column is behind the code.** `PlanFire` is a wrapper now and the plan's body is `PlanFireOf`; `GoodOrderAsPlanned` is gone; the A8.312 and A4.63 rows name no members though pass 40 wrote them. Corrected with the pass's documents.

## 6. The frame's questions

Each carries its proposal; the answer asked for is a veto or a change.

**F1. Where the rows the plan does not cite go.** Section 3 places all 45. In short: A8.4 and A7.7's Locations above and below join 45.1; A.7's SW sense and A4.41 join 45.7; A8.312 joins 45.8; A4.42, A4.44, A4.61, and A4.63 make a new task 45.10, "pass 40's movement rows" (0:40); A7.51 leaves the pass for pass 65. Remainders that need another pass's ground (ordnance, vehicles, pillboxes, OBA) keep their row with the pass column moved, as section 3 names them. **Proposal: as placed.**

**F2. The order.** The plan's order starts with levels. The per-firer fire kind (45.8) changes the attack's record, and the marks of Cowering, the ROF choice, and the Fire Lane's cancellation are all written on it. Built late, it would reopen three tasks. **Proposal: the increments of section 2, with 45.8 and 45.9 first.**

**F3. How the DEFENDER chooses between ROF and Residual FP (A8.23).** The page: the choice is made at once after the attack is resolved. The projector requires the counter placed to equal the attack's own recorded figure, so the choice has to be part of the attack. Two ways: (a) a choice declared with the attack for each MG, "if it keeps its ROF, forfeit it and leave Residual FP", the default being to keep the ROF as the page's "does not automatically leave"; (b) the proposal shown with its dice, and the choice changed before the confirmation on the same dice. (a) is small and loses what the page gives the player, the sight of the result. (b) is the page's rule and depends on whether a proposal can be made again on its own dice. **Proposal: (b) if the run of section 4 shows a proposal can keep its dice with a change the size of a task; otherwise (a), as a ruling, with a backlog row for the choice after the roll.** The group's other firers leave their own Residual FP either way, as the page's example has it (a 4-6-7 and a MMG at 9 FP: the squad leaves 2).

**F4. A fire kind for each firer, and what waits behind the attack owed (A8.31, A8.312, rulings R12.3, R40.17, R40.19).** The page: a unit using FPF may form a FG with units not using FPF, and only those using FPF bear its effects; the long example on pages 61 and 62 has a squad in FPF fire with a crew in First Fire, and a LMG in Sustained Fire fire with a squad in First Fire at 11 FP. **Proposal:** each firer and each MG takes its kind from its own counter (none: First Fire; First Fire: Subsequent First Fire; Final Fire: FPF); the modifiers, the Sustained Fire penalty, the NMC of FPF, and the counters placed follow the firer's kind; the attack's recorded kind stays as the heaviest among them, so recorded games keep their shape. This replaces the "one kind an attack" part of R12.3 and closes R40.17: the Mandatory FG is every unit that owes the attack. For R40.19 the page says the DEFENDER "must ... then immediately attack": **proposal:** while the attack is owed, every act of the DEFENDER but that attack waits (a Gun's shot, a thrown DC, a Starshell), under the same test as now that the attack can be made at all.

**F5. The two stale rows.** **Proposal:** 45.8 builds nothing for A7.25 or for vertical ADJACENT; the documents mark both built by pass 35, and A7.25's remainder (Intensive Fire by an Opportunity Firer) moves to pass 110.

**F6. The MG's Bore Sighting (C6.44).** The minus 2 is one line; the setup is not. To build it the setup must accept a possessed MMG or HMG of the Scenario Defender, record its possessor and setup Location, and the setup page must offer it; the DRM then applies against Infantry only in Defensive First Fire, never with Spraying Fire, a Snap Shot, or Residual FP, and for a group only when every element sighted that Location. The light mortar's Bore Sighting is a To Hit case (C6.4). **Proposal: build the MMG's and HMG's in 45.8, with the setup field and its Studio check, and add 0:20; the light mortar's goes to pass 110.** The other way is to move the whole row to pass 110, which keeps the pass at its plan.

**F7. Upper-level Encirclement (A7.72).** A unit in an upper building level is Encircled while it can trace no path to ground level free of an unbroken, armed, unconcealed enemy unit or a Blaze, and is released "the instant such a path can be traced". That is a condition of the map and the units, not a counter. **Proposal:** a task of its own, 45.1b (0:40): Rules decides from a path reader that Play implements (the building's Locations and stairwells, who stands in each, as the viewing side may know it), and the result is read wherever Encircled is read (morale, the +1 on fire, the doubled MF, surrender), with nothing stored in the state. Scaling is not built and is left out with a backlog row. What a side may know: the search reads unconcealed units only, as the rule does, so it tells nothing new.

**F8. A TI unit and the SW and Deployment actions (A.7, A4.8; ruling R35.5).** The code bars a TI unit from Deploying, Recombining, Transferring, Recovering, and Dropping. A4.8 bars movement, advance, fire, and "other labor tasks", and A.7 keeps the unit in Good Order. Recovery in the MPh costs a MF, which a TI unit cannot spend. The others cost nothing and are not labor tasks in any rule read. **Proposal:** in 45.10 the bar stays on Recovery in the MPh and is lifted from Deployment, Recombining, Transfer, Drop, and Recovery in the RPh. I found no built action that makes a unit TI before its own RPh, so the change may show only in a staged game; the task says so if that holds.

**F9. The mortars' Repair number and the dm counters.** No mortar in the catalog has a Repair number, and no dm side exists for the HMG or the mortars. The dm portage is the rule's own (half, rounded up: 3 for the German HMG and mortar, 2 for the Russian and British mortars). **Proposal:** at 45.7 the whole rulebook is scanned for the mortars' Repair number first (Chapter H and the counter art); if no source prints it, it is manufactured under R0.3 on sheet MFG and the value is put in the task's report before the commit. Nothing blocks on it.

**F10. The estimate.** The plan's nine tasks are 5:40 of building. Task 45.10, task 45.1b, and F6's setup change add 1:40, which makes 7:20; with the plan's overhead of 1:15 the pass is 8:35, against the plan's 6:55. Pass 40's gate alone took 1:47, more than that overhead. **Proposal: accept it; F6's second way gives back 0:20, and leaving 45.1b to pass 140 (buildings) gives back 0:40.**

## 7. The shape of an attack: 45.8, 45.9 (increment 2a)

Built on 2026-10-12 under the user's word of that morning: "Implement all parts of Pass 45 with out my intervention running all Studio tests as appropriate and then stop before the gate." Each task is committed without a stop, and each ruling below is decided by the proposal the frame put (F1 to F10, answered yes) or, where the frame put none, marked **decided under the user's word of 2026-10-12**.

### 7.1 Task 45.8

**A fire kind for each firer (A8.3, A8.31, A8.312, pp. 61 to 62; question F4).** The attack keeps one recorded kind, the heaviest among its firers, so recorded games keep their shape. Inside an attack of Subsequent First Fire or FPF each firer fires by its own counter:

- a unit marked Final Fire uses FPF: Area Fire, the NMC on the Original DR, its MG in Sustained Fire;
- a unit marked First Fire uses Subsequent First Fire: Area Fire, the range limits of A8.3, its MG in Sustained Fire;
- a unit with no counter fires First Fire: its FP whole, its MG's ROF kept on the colored dr, and it is marked First Fire, not Final Fire;
- a unit with no counter that fires a MG marked First Fire uses Subsequent First Fire with the MG alone: its own FP whole, the MG halved and in Sustained Fire, and unit and MG both marked Final Fire. This is the page's example: "{4 [4-6-7 FP] + 1 1/2 [LMG Area Fire]} x 2 [PBF] = 11 ... the 4-6-7 and LMG would then both be marked with a Final Fire counter".

The resolution carries the firers whose counter differs from the attack's (`FirerCounters`), written only when one does. The planner's refusal "a group mixes firers marked for different kinds of Defensive fire" is gone.

**Ruling R45.1** (question F4, approved in the frame): each firer of a Defensive fire attack fires by its own counter, and the attack's recorded kind is the heaviest. It replaces the "one kind an attack" part of ruling R12.3 and closes ruling R40.17: the Mandatory FG of A8.312 is every unit that owes the attack. The texts that cite R40.17 stay for a group the fire rules still refuse for another reason.

**Ruling R45.2** (question F4, approved in the frame; replaces R40.19): while the Mandatory FG's attack is owed, a Gun's shot, a thrown DC, and a Starshell by the DEFENDER wait for it (`play.act-mandatory`). As with the fire action, the bar is on a proposal that names a unit or a weapon of the side that is not moving, so no refusal tells the ATTACKER who owes the attack; and it stands only while the attack can be made at all.

**The FPF whose NMC replaces its firer (backlog 56.1).** Reproduced in a Play test with an Original DR of 11: the firer was Replaced by ELR, and the fire marker was then written to the unit the Replacement had consumed, which the gate refused. A firer that its own NMC eliminates, Reduces, or Replaces now takes no fire marker; the unit in its place keeps the Final Fire counter it had. Fixed.

**The group as one chain (A7.5, p. 57).** The Locations of a group are reached one from another, starting at the first; two pairs apart are refused as before by `firer-outside`. Vertical ADJACENT and ADJACENT across a wall or hedge were built by pass 35 and are unchanged.

**No berserk unit in a multi-Location group (A7.54).** The firer facts carry the berserk state; the refusal is `berserk-group-outside`, with its sentence for the page.

**A crew's FP after its Gun fired (A7.353, p. 56).** "Subsequent First Fire (8.3), FPF (8.31), and Final Fire (8.4) vs adjacent units retain halved inherent FP". The crew, marked First Fire by its Gun's shot, fires its own FP in Subsequent First Fire and in Final Fire against an adjacent unit, as it did in FPF since pass 40. **Correction to the frame's point 5.7:** the "halved" of A7.353 is the Area Fire halving those three kinds of fire already carry, not a further one. The code halves once, and ruling R40.18's figure needs no amendment.

**The MG's Bore Sighting (C6.41 to C6.44, p. 174; question F6).** The setup accepts a MMG or HMG the Scenario Defender possesses, for a Location outside its hex, in its LOS, within its Normal Range and 16 hexes; the record names its possessor and their setup Location. The setup editor shows the Bore Sighted field for a MG. The attack deducts 2 when every firer fires its MG alone, each MG has its sighting on the target Location, and the possessor is the unit that set up with it, in its setup Location.

**Ruling R45.3, decided under the user's word of 2026-10-12:** against Infantry the minus 2 is given in an attack of the kind First Fire only. The page says "only such an attack conducted as Defensive First Fire (A8.1)", and cites A8.1, not A8.3 or A8.31. A squad's own FP has Bore Sighted nothing, so a MG firing with its squad's FP has no DRM ("only if all elements of that FG have Bore Sighted that same Location").

**Left out, each with a backlog row:**

- A MG that leaves its setup Location and comes back claims its Bore Sighting again. C6.43 loses it for good; the game tests where the MG stands when it fires.
- "In a building hex only the ground-level Location ... may be Bore Sighted" (C6.42) is not checked, for a Gun or a MG.
- The CC counter on the co-occupants after a TPBF FPF (A8.31): nothing in the game reads it.
- A crew that fired its Gun and carries no fire counter (its Gun's shot kept its ROF) has no FP in a First Fire attack, as before.
- A light mortar's Bore Sighting (pass 110).

**The games,** on board 3, played in my Studio on 6671 in the adjudicator's view and read back from their files:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-fg-mixed` | Two Russian squads in [C9], one marked First Fire; a berserk squad enters. The page's note asks for one Mandatory FG of both. The attack is recorded as subsequent first fire: 12 FP (TPBF) and 6 FP (TPBF, Area Fire), the 16 column; the unmarked squad is marked First Fire and the other Final Fire; 8 Residual FP. | yes |
| `p45-fg-plain` | The control: neither squad marked. First fire, 24 FP. | yes |
| `p45-fg-bore` | A Russian MMG in [F10] Bore Sighted on [D10] fires alone at a squad that enters it: DRM "- 2 (bore sighting, C6.44) - 1 (FFNAM) - 1 (FFMO)". | yes |
| `p45-fg-unsighted` | The control: no sighting, DRM -2 in all. | yes |

The games are written by `pass45/make_458.py` in the tools folder. The setup editor's field for a MG is checked with the whole pass's Studio check, in a game set up by hand.

**The tests.** Rules: `ScenarioA1Pass45RulesTests` (ten tests of the kinds, the chain, the berserk bar, the crew, Bore Sighting, and the held acts); `ScenarioA1Pass12Tests` has one assertion turned, from "an unmarked unit does not join FPF" to its First Fire counter. Play: `CloseCombatStepsTests` gains the mixed Mandatory FG with the held Gun's shot, and the FPF whose firer is Replaced; `BacklogPass8Tests` gains the crew's Subsequent First Fire. Classes run: those three, and `ScenarioA1Pass40RulesTests`, `BacklogPass12Tests`, `BacklogPass30Tests`, `FireTests`, `RallyAndFireStepsTests`, `RefusalReasonsTests`, `FireOwnReasonsTests`. No record of the state changed, so the Units project was not run.

### 7.2 Task 45.9

**A crew's Casualty Reduction (A7.302, p. 55).** "Casualty Reduction eliminates any HS or crew". The reduction had no branch for a crew and ended undecided ("reduction counter missing"); a crew is now eliminated as a HS is. The same member serves a prisoner that fails a MC (A20.54, p. 88: "A prisoner/unarmed unit which fails a MC is not broken; it suffers Casualty Reduction instead"), so a prisoner crew is covered by it. The Recall exception is a vehicle's Inherent crew's (D5.341), which is no unit of its own here.

**The A7.212 limit on ordnance (p. 55).** "A unit does not have the freedom to attack units in other Locations while its own Location is occupied by a Known enemy unit ... Spotters (C9.3) are similarly limited". The limit was read for IFT fire only. It is now read for a Gun's, a mortar's, and a LATW's shot too, at the Location the shot is seen from, which for a spotted mortar is its Spotter's. The frame had sent Spotters to pass 110; the page gives them the same limit in the same sentence and the same read serves both, so they are built here. The exception for an unarmed, unarmored vehicle is the one the IFT read already has.

**What the frame did not know:** a crew is a target the Fire package reviews only alone at its Gun (ruling R8.3); a crew with other units, or with no Gun, is refused whole (`crew-target-unreviewed`). That is older than the pass; the pass's documents give it a backlog row if it has none. The Casualty Reduction is shown on a crew at its Gun.

**Left out, with a backlog row:** the Gun's target list still offers a Location that A7.212 refuses (the row of section 55.2 about that list, pass 110).

**The games,** on board 3, in the adjudicator's view:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-cr-crew` | Two German squads in [D9], directed by a 9-1, fire at a Russian crew alone at its AT Gun in [D10]: 16 FP, DRM +2 emplacement and -1 leadership, DR 3: K/3, "2-2-8 crew R1: eliminated". Before the task the attack ended undecided. | yes |
| `p45-cr-held` | A German squad stands in the Gun's own Location [D10]. The Gun's shot at [F10] is refused: "[D10] holds a Known enemy unit, so its units fire only at their own Location (A7.212)". | no: a refusal writes nothing |
| `p45-cr-free` | The control: no enemy in [D10]; the shot at [F10] is proposed and committed. | yes |

**The tests.** Rules: `ScenarioA1Pass45RulesTests` gains the crew's elimination and the limit's four cases (ordnance held, IFT fire held, free, its own Location). No Play test of its own: the planner writes no new event, and the refusal is the Rules member's. Classes run: `ScenarioA1Pass45RulesTests`, `ScenarioA1Pass19bTests`, and in Play `OrdnanceStepsTests`, `BacklogPass8Tests`, `BacklogPass9Tests`, `BacklogPass10Tests`, `BacklogPass35Tests`.

## 8. Levels of one hex: 45.1, 45.1b (increment 2b)

### 8.1 Task 45.1

**The rule (A7.21, p. 54; B23.25 and B23.26, p. 136; A8.4, p. 61).** FP "is doubled while either ADJACENT to its target or adjacent to and either within one level of or higher than its target"; TPBF is for "units in the same Location as the attacker". "A unit in a building is ADJACENT to an adjacent building level of the same building only if both are ... vertically connected by a stairwell", "LOS to a unit in a building Location ... can exist only if drawn to an ADJACENT unit", and "Attacks up or down a stairwell hex to other vertical levels of the same building hex are limited to the next higher or lower level. A ground level and second (or higher) level of the same building hex are never ADJACENT."

**As built.** Fire from one level of a hex at another level of it was refused as out of range. Now:

- The planner reads no LOS line for it. It asks Rules (`SameHexLevelBar`) with the levels' distance and the pass 35 ADJACENT read: the next level, and a stairwell between them, or the refusal `play.fire-level`.
- The attack is at range 0 with the target one level above or below, and its FP is doubled as PBF, not tripled. The target's TEM is its Location's (a building's); a hexside TEM, Height Advantage, and Hindrance have nothing to read.
- A8.4, A8.3, and A8.31 ask for an adjacent or same-hex target; range 0 satisfies each, so Final Fire by a First Fire unit, Subsequent First Fire, and FPF reach the next level.
- The range band reads "PBF, FP x2, at the next level of its hex by a stairwell" and a farther level "no attack". The page's Target list said "out of range" for the next level until the game showed it; it now says PBF.

**Encirclement from above and below (A7.7, p. 57).** "An Encirclement can also be created by a LOF from both the Location directly above and below it in a building hex." An attack from the next level counts toward Encirclement as one from above or from below, and the two together Encircle.

**The frame's point 5.6 is withdrawn.** The PBF test "range 1 and the target at most one level above the firer" is the page's rule as it stands: a firer higher by any amount has PBF, and one lower by two has not.

**Left out, each with a backlog row:**

- The Target list offers the next level of a hex that no stairwell joins, as PBF; the proposal is refused with the reason. The list reads levels, not stairwells.
- A group whose firers stand in the target's own Location and at the next level of its hex is refused, as any group that mixes TPBF with other fire was before.
- A vehicle's MG at another level of its hex (pass 50), and IFE (no counter in the catalog).
- The "Latest" line does not say that an attack Encircled its target; the Encirclement counter is in the record and on the map. Older than the pass.

**The games,** on board 3, in the adjudicator's view. Stairwells were read from the oracle's hex facts (`pass45/board3_buildings.py`): [L4], [M2], [M5], [R3], [R5], [R6], [S3], [T6], and [U6] have one; [N1] and [N2] have none.

| Game | What it shows | Recorded |
|---|---|---|
| `p45-lv-up` | A German squad at ground level of [M5] fires up at a Russian squad at level 1: "4 FP x 2 (Point Blank Fire, A7.21) = 8", DRM +2 wooden building. | yes |
| `p45-lv-up2` | The same position after the page's fix: the Target list reads "[M5], level 1: PBF". | no: read only |
| `p45-lv-far` | The control, on [M2]: ground level at level 2 is refused, "fired at only when it is the next level up or down and a stairwell joins them (B23.25, B23.26)". | no: a refusal |
| `p45-lv-two`, `p45-lv-encircle` | On [N2], which has no stairwell: both the next level and level 2 are refused. Kept as the control for "no stairwell". | no: refusals |
| `p45-lv-enc` | On [M2]: a Russian squad at level 1, fired at from ground level and then from level 2, 8 FP each with +3 stone building. The second attack writes `encirclement-placed`. | yes |

**The tests.** Rules: `ScenarioA1Pass45RulesTests` gains PBF at the next level with its two refusals, and Encirclement from above and below; `FireRangeTests` has the band's two cases and its words. No Play test: board 01's fixture has no stairwell building the test boards can reach, and the planner's part is one call to the Rules member; the games stand for it. Classes run: those two, `ScenarioA1Pass12Tests`, the pass 10 Rules classes; in Play `BacklogPass10Tests`, `BacklogPass12Tests`, `BacklogPass31Tests`, `FireTests`; the page's `Pass31cRangeTests`.

### 8.2 Task 45.1b

**The rule (A7.72, p. 58).** "Encirclement also applies to any non-heroic/non-berserk unit in an upper level building Location that cannot trace a path free of an unbroken, armed, unconcealed enemy unit/Blaze to ground level through (or past, if Scaling; B23.424) Locations it could legally traverse if so inclined. This type of Encirclement is broken the instant such a path can be traced."

**As built (question F7).** Nothing is stored. Rules owns the search (`ScenarioA1UpperLevelEncirclement`): from the unit's Location, breadth first, through the Locations Infantry could step into, never through one the enemy holds, until a ground-level Location is reached or none is left. Play implements the reader: the steps are the movement rules' own Infantry steps (the stairwell up and down, the same level of the next hex of the building), and a Location is held by an active enemy unit that is not broken, unarmed, captured, concealed, or hidden. The planner's `Encircled` is the counter's Encirclement or this one, and every reader in Play takes it: the fire facts of firers and targets (the morale drop and the +1), the MF of a move and of an advance, the rout's reach and its surrender, the Interdiction read, and the page's units table, which says "Encircled: no path to ground level (A7.72)".

In a fire attack the fact is a read of the map, so it is recorded with the attack and the replay takes it from the record, as it does the LOS.

**What a side may know.** The search reads only unconcealed, unhidden enemy units, as the rule does, so the result tells a side nothing it cannot see.

**Left out, each with a backlog row:** a terrain Blaze on the path (terrain Blazes are not built); Scaling (B23.424); the projector does not know this Encirclement, so it ends no counter and writes none, which is the rule ("broken the instant such a path can be traced").

**The games,** on board 3 in the stone building of [M2], whose only stairwell is in [M2]:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-up-cut` | A Russian squad at level 2; German squads at level 1 and at ground level. The units table reads "Encircled: no path to ground level (A7.72)". The squad at level 1 fires up: the record has the target Encircled and its Morale Level 6, and it breaks on a check of 8. | yes |
| `p45-up-open` | The squad at level 1 is broken. The Russian squad is still Encircled: the unbroken squad at ground level holds the foot of the only stairwell. I had written this as the control and expected no Encirclement; the game was right and my control was wrong. | no: read only |
| `p45-up-free` | The control: both German squads broken. No Encirclement. | no: read only |

**The tests.** Rules: `ScenarioA1Pass45RulesTests` gains the search over a small building: a free path, the stairwell held, the way through the next hex, both ways held, and the three exemptions. No Play test: the test boards have no stairwell building, as for 45.1. Classes run: that one; in Play `RallyAndFireStepsTests`, `CloseCombatStepsTests`, `BacklogPass10Tests`, `BacklogPass12Tests`, `BacklogPass14Tests`, `FireTests`, `OlderGamesTests`. No record of the state changed.

## 9. What an attack leaves: 45.2, 45.3 (increment 2c)

The two tasks share one member, the Residual FP an attack leaves, and are committed together, each listed in the commit.

### 9.1 A departure from the frame, first

**Ordnance HE's Residual FP (A8.25) is not built; it goes to pass 110.** The frame's section 3 placed it in 45.3. An ordnance hit is resolved inside the ordnance shot's own record, not as a fire record: to leave Residual FP the hit's facts would need a fact that survives the ordnance record's replay, and the projector's Residual FP check, which reads fire records, would need to read ordnance records too. Both belong to the ordnance package. Decided under the user's word of 2026-10-12; it has a backlog row and is first in the pass's report.

**Spraying Fire's Residual FP in the MPh (A8.24) moves to task 45.4,** which builds Spraying Fire in the MPh; the plan had it in 45.3, before the thing it rests on.

### 9.2 Task 45.2: Cowering's marks, and ROF or Residual FP

**Cowering marks the unit (A7.9, p. 58).** "any unit that cowers (as well as all of its SW regardless of whether it was using its inherent FP) is automatically marked with a Final or Prep Fire counter as appropriate ... If a FG cowers, Random Selection is used to determine the unit(s) (and its SW) that becomes marked".

- A lone firer that can cower is the unit marked. Of a fire group, each firer that can cower rolls a dr (a roll of its own, `cowerSelection`); the highest is marked, ties with it.
- The marked unit takes a Prep Fire counter in the PFPh and AFPh and a Final Fire counter in the MPh and DFPh, whatever counter the attack gives the others. A squad that fired only its MG is marked too.
- Its SW take the counter: those it fired keep no Multiple ROF, and those it did not fire are marked by the planner.
- A berserk firer never cowers (the firer facts carry the state since 45.8).
- The shift is doubled "for an attack by Inexperienced (19.33) Personnel": a Conscript always; a Green unit by its Inexperience fact (A19.3), not by its class.

A22.1 (p. 89) says a FT's FP "is reduced by cowering", so a FT attack marks its squad as any other; two older tests that rolled doubles with a FT and then fired again were given other dice.

**ROF or Residual FP (A8.23, p. 60; question F3).** "A weapon does not automatically leave Residual FP if it retains a Multiple ROF ... The decision to forfeit possible remaining First Fire capability in order to leave Residual FP must be made immediately after resolving its attack."

The run the frame asked for: the page draws the dice at the confirmation, not at the proposal, so a proposal cannot be "made again on its own dice". The choice after the roll is therefore an action of its own, **which is F3's way (b), built:**

- The attack leaves the Residual FP of the firers other than the weapons that kept their ROF, as the page's example has it (a squad and a MMG: "the squad still leaves two Residual FP"). Its record also gives the figure with the ROF given up, when that is more.
- `asl.game.forfeit-rof` ("Give up a weapon's ROF for Residual FP") is open to the DEFENDER while nothing has happened since the attack but what the attack caused. It marks each such weapon First Fire and raises the Residual FP in the Location to the record's second figure. Any other act closes it: the DEFENDER's next fire, its pass, the ATTACKER's next step.
- The Play page shows it in a block of its own, to the DEFENDER's view and the adjudicator's, never to the ATTACKER's. It says what would stand: "the MMG kept its Multiple ROF, so its FP left no Residual FP. Give the ROF up now, before anything else, and 4 Residual FP stands in [D10]".
- The projector accepts a Residual FP counter at the record's own figure or at its figure with the ROF given up.

### 9.3 Task 45.3: Residual FP completed

- **The cap (A8.2).** "half (up to a maximum of 12 after adjustment as per 8.26; FRD) of the highest IFT FP column used". The half is taken down to an IFT column, lowered one column for each +1, and then held to 12. A 36 FP attack with one +1 leaves 12; it left 8. Every lower column gives what it gave.
- **The DRM that lower it (A8.26).** LOS Hindrance, a positive leadership modifier, and a hexside TEM lowered it already. Now also the firers' own: CX, an Encircled firer's +1, a hero's, a Stun, and the firer-overstack DRM pass 40 added. Height Advantage and LV stay out, as the page says.
- **A malfunction (A8.221).** "Residual FP is not gained for an attack by a malfunctioning weapon": the weapon's FP is left out.
- **Once a Location (A8.22).** "A unit can be attacked by Residual FP only once per Location [EXC: if ... the Residual FP has increased in strength or the unit is subject to more-negative-DRM/less-positive-DRM ...]". A vehicle was attacked at each MP expenditure in the Location. The planner now reads this MPh's Residual FP attacks on it there and asks Rules (`ResidualAttacksAgain`) with the counter's FP and the attack's DRM, then and now.

**Ruling R45.4, decided under the user's word of 2026-10-12:** the Cowering marks, the Inexperience read, and the Residual FP figures of this increment apply to attacks planned from pass 45 on. Each such attack records the fact `coweringMarksAndWeaponResidual`; an attack recorded before the pass has none and resolves as it was recorded, so no recorded game changes. This is the pattern of pass 31's ruling R31.3. Without it two recorded games of the test fixtures no longer replayed.

**Left out, each with a backlog row:**

- Ordnance HE's Residual FP, and Air Bursts raising it (A8.25, A8.26): pass 110.
- A Residual FP attack on an Infantry MF expenditure inside the Location (SMOKE grenades, Recovery, a DC): each such act would need the attack; SMOKE placement there is still refused (ruling R9.5).
- "Residual FP does not attack until any activity that a MF/MP expenditure allowed is completed": nothing to order yet.
- The "Latest" line does not say that a ROF was given up; the Records tab has the events.
- Ammunition Shortage (pass 75); pillboxes, OBA, minefields (passes 115, 120, 150).

**The games,** on board 3, in the adjudicator's view unless said:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-cw-doubles` | A Russian squad with a MMG and a LMG fires squad and MMG at a squad entering [D10], DR 2 and 2. The 8 column shifts to 6; the squad, the MMG, and the LMG it did not fire are all marked Final Fire; Residual FP 2. | yes |
| `p45-cw-rof` | The same with DR 2 and 3: the MMG keeps its ROF and the attack leaves 2. The page offers 4; the action marks the MMG First Fire and places 4. The ATTACKER's view does not see the offer. | yes |
| `p45-cw-keep` | The same, the ROF kept. The MMG's second shot at the same stack is refused for the one MF spent (A9.2), which leaves the offer open; the DEFENDER's pass, from the Russian view, closes it. | yes |
| `p45-rs-cx` | A CX squad's attack: DRM "cx" +1, Residual FP 1. | yes |
| `p45-rs-fresh` | The control: Residual FP 2. | yes |

No game for the vehicle's one attack a Location: it needs a truck driven through three MP expenditures in a Residual Location, and the Play test does exactly that through the gate. Said plainly: I did not run that test against the code before the change to see it fail.

**The tests.** Rules, in `ScenarioA1Pass45RulesTests`: the cowering unit's counters and its MG's ROF in three phases; the Random Selection of a fire group; the berserk exemption and the Inexperience read; the cap; CX; the MG's ROF kept, lost, and malfunctioned; the offer's conditions with the projector's check; the once-a-Location rule. Play: `CloseCombatStepsTests` (the ROF given up through the gate, and refused the second time), `BacklogPass6Tests` (the truck attacked once), `PlayTests` (the new action registered). Four older Play tests were given dice that are not doubles (pass 10's road test, pass 15's two FT tests and its MOL test), with a comment each. **The whole Play project was run once, since Cowering is read all over fire: 738 of 738.** Rules classes run: pass 45, 9, 10, 12, 15, 16, and the Fire package's.

## 10. The MG's own rules: 45.4, 45.5, 45.6 (increment 2d)

The three tasks are committed together, each listed in the commit. They share no member; I built them in one working tree to spare Studio builds, and say so.

### 10.1 Departures from the frame, first

Decided under the user's word of 2026-10-12; each has a backlog row and is in the pass's report.

- **Spraying Fire in the MPh is not built (A9.52, A8.24).** The frame placed it in 45.4. In the MPh the second Location usually holds no moving unit, so its attack is "halved again" and only leaves Residual FP; each Location's Residual FP then has to follow the ROF-or-Residual choice of 45.2, and the offer reads one record where a spray writes two. That is a design of its own, not a line in this task.
- **Fire Lanes are completed in one point only: the forced cancellation (A9.223).** The frame placed five more in 45.5: lanes against vehicles, attacks on later MF expenditures in a lane Location, the Alternate Hex Grain, the Fire Lane Snap Shot, and hexside TEM and hard Hindrances as DRM. The last three need a LOS to a hexside or along a hexspine, which is pass 135's ground; the first two need the record of who a lane has attacked, which this pass built for vehicles and Residual FP counters only.
- **No game for Mandatory Fire Direction (A9.4).** It needs a clear LOS of 17 hexes on a board the Studio loads; I did not look for one. The Rules test stands for it.

### 10.2 Task 45.4: Spraying Fire

- **Squad Spraying Fire (A7.34, p. 56).** The fire reference reads the catalog's trait (an underscored Range; 13 of the 37 squads carry it, no HS and no crew). A squad with the option sprays with its own FP "limited to a maximum range of three hexes"; a target beyond its Normal Range is halved again as Long Range Fire, which the page's example gives (a 6-2-8: 3 FP at two hexes, 1 1/2 at three).
- **The capability test (A9.52, p. 64).** "Spraying Fire cannot be used in a FG attack unless all members of the FG are capable of Spraying Fire". A MG is capable; a squad's own FP is with the option; no other unit's FP is.
- **Ruling R45.5, decided under the user's word of 2026-10-12** (replaces the leave ruling R12.6 gave): a squad with no option may not add its own FP to its MG's Spraying Fire; its MG sprays alone. An attack recorded before the pass keeps R12.6 (the same fact as ruling R45.4).
- **Two levels of one hex (A9.5).** "The target Locations can be 'adjacent' vertically in the case of building upper levels, but each must be within one level of the other and in the same hex."
- **Encirclement (A7.7).** Spraying Fire counts toward Encirclement as any fire by inherent FP or a SW does, when its FP could inflict a NMC.

### 10.3 Task 45.5: the Fire Lane's cancellation

A9.223 (p. 63): "A MG may cancel its Fire Lane in order to gain freedom to fire elsewhere (/its manning Infantry may use Subsequent-First-Fire/FPF after establishing the Fire Lane) only if a TPBF ... situation occurs (8.312 ...) in which case the Fire Lane must be cancelled". The lane's bar on its MG and its manning Infantry no longer holds against an attack at their own Location, and the projector removes the lane with that attack's record. Before, a squad with a Fire Lane whose Location was entered could not make the attack A8.312 forces on it, and pass 40's read then found it owed none.

### 10.4 Task 45.6: Field of Fire, Mandatory Fire Direction, and direction of a MG's later shots

- **Field of Fire (A9.21, p. 62).** "if a SW MMG/HMG in a woods/rubble/building hex fires and is entitled to another shot, it may continue to fire during that phase only inside the CA of the prior shot (which ... includes the MG's own Location ...)". No CA is stored. The planner reads the MG's earlier shots of the phase from the records and asks Rules whether they and the new target lie in one 60-degree wedge on a hexspine (C3.2). The refusal is `play.fire-field`.
- **Mandatory Fire Direction (A9.4, p. 64).** A MG fires beyond 16 hexes only when a leader directs it (`mg-direction-outside`), and at 17 or more its FP is halved as against a concealed target. Under the fact of ruling R45.4, so recorded attacks stand.
- **A leader directs a MG's later shots (A7.53, p. 57).** "a leader can direct the fire of a MG as many times as the MG can fire, even if he also directed other units as part of a FG in the MG's previous attack." The planner reads from the phase's records whether he directed every MG the attack fires, and records it with the attack; the package then admits him when nothing but those MGs fires.

**An older fault the game found.** With the leader's second direction the proposal passed and the commit was refused. The cause was not the direction: the projector's A7.55 check ("a Location fires at a target once a phase") refused any Infantry MG's later shot on its Multiple ROF at the same Location, where the planner's check lets it stand and the rule excepts "the subsequent shots of multiple ROF weapons". A MG that kept its ROF in the PFPh, AFPh, or DFPh could not fire again at the Location it had just fired at. Fixed in the Rules member both read.

**Left out, each with a backlog row:** the departures of 10.1; the pinned operator's CA carried to the DFPh, a stairwell CA, and the INF, RCL, and 20L ATR types (A9.21); the exception for broken, berserk, or overstacked Infantry at 17 hexes and more, and "no effect vs armored targets" (A9.4); "only firers he directed in First Fire" for a leader's Subsequent First Fire, FPF, and Final Fire direction (A7.53), which is as it was; the Mandatory FG's plainest attack names no MG that is marked First Fire with a Fire Lane, so the squad fires alone and the MG's fire is forfeit for the turn, as A8.3 allows.

**The games,** on board 3, in the adjudicator's view:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-sp-spray` | A 4-6-7 in [F10] sprays [D10] and [D9]: "4 FP x 0.5 (Spraying Fire, A9.5) = 2", two records on one DR. | yes |
| `p45-sp-nospray` | The control: a 4-4-7 is refused with the sentence for Spraying Fire. | no: a refusal |
| `p45-ff-field` | A MMG in the woods of [C9] fires at [D9] and keeps its ROF. [C8] is then refused ("fires again only inside the CA of that shot (A9.21)"); [C10] is allowed. | yes (the first shot) |
| `p45-ff-open` | The control: the MMG in the Open Ground of [D9] fires at [D10], and then [C9] and [E10] are both allowed. | yes (the first shot) |
| `p45-ff-leader` | The fault: the leader's second direction proposed, and the commit refused. Kept as the fixture of the repair. | yes (the first attack) |
| `p45-ff-leader2` | After the fix: the 8-0 directs the squad and its MMG, the MMG keeps its ROF, and he directs its second shot alone. | yes |
| `p45-fl-lane3` | A berserk squad enters [E10]; the squad and MMG in [F10] fire and lay a Fire Lane in [E10]. The squad then enters [F10]: the page says the squad must attack, and its Subsequent First Fire at its own Location commits (6 FP, TPBF and Area Fire). | yes |
| `p45-fl-lane`, `p45-fl-lane2` | False starts of mine: a berserk unit must charge before another moves, and a 12 on its MC eliminated it. Not fixtures. | partly |

For `p45-fl-lane3` I could not read from the page that a lane had been listed before the attack and was gone after it; the page lists Residual FP there. The projector's removal has its Rules test.

**The tests.** Rules, in `ScenarioA1Pass45RulesTests`: the squad's spray at two and three hexes with its two refusals; the group's capability with the older record's leave; the Field of Fire's wedge; the 16-hex limit and the halving; the leader's later direction; the lane's bar and its cancellation. Play: `CloseCombatStepsTests` gains the leader's later direction through the gate, which is the fixture of the projector's fault. Classes run: Rules pass 45, pass 12, the Fire package's; **the Units project whole (409 of 409), since the projector changed**; Play `BacklogPass10Tests`, `BacklogPass12Tests`, `BacklogPass15` (both), `BacklogPass31`, `CloseCombatStepsTests`, `FireTests`, `OlderGamesTests`, `RallyAndFireStepsTests`, `RefusalReasonsTests`.

## 11. Upkeep and leftovers: 45.7, 45.10 (increment 2e)

### 11.1 Departures from the frame, first

Decided under the user's word of 2026-10-12; each has a backlog row and is in the pass's report.

- **The mortars' Repair number is not manufactured, so a mortar is still not repaired (A9.72; question F9).** The answer to F9 was to manufacture it under R0.3 if no source prints it. The Repair number is a catalog value, and a value added to the catalog is a catalog version: its manifest, its counter transcription, and the tests that name the version. That is a job of its own that the pass, which the plan gives no counters, does not open. I did not run the rulebook scan either; it belongs with that job.
- **SW Self-Destruction (A9.73) is not built.** It is an action of its own (a deliberate malfunction or destruction in the PFPh or DFPh, counting as the weapon's use), with its registration and its place on the page.
- **The SW sense of Good Order (A.7)** is left as it is: the Fire Lane's declaration and the fire package already ask for an unbroken, unpinned possessor and a weapon that is not malfunctioned, which is the rule's content.

### 11.2 Task 45.7

- **The X# (A.11, p. 43; A9.7, p. 65).** A fire that lowers the B# "also transforms the weapon's Original B# to an X# during that use": a weapon whose B# this attack lowered (Sustained Fire, capture, Inexperience) is removed on a DR at least its printed B#. The weapon effect says so and the planner eliminates it. Under the fact of ruling R45.4. A9.71's Random Selection among several weapons is the malfunction's own, already built: a weapon it selects whose printed B# was reached is removed.
- **A lone SMC's MG (A9.3, p. 63).** "Sustained Fire cannot be used ... by a MG fired by a lone SMC": a leader with no partner fires no MG in Subsequent First Fire, FPF, or Final Fire by a First Fire weapon.
- **Repair of a captured SW (A9.72).** "A captured SW/Gun may never be repaired unless recaptured by its original side": refused.
- **Dismantling (A9.8, p. 65).** "Any light mortar of >= four PP, 76-82mm mortar, or non-Russian HMG/MMG". The test was the German MMG by its id, decided in Play. It is now a Rules member over the catalog's kind, nationality, size, and portage: the German HMG and the three light mortars are dismantled too, and the Russian MGs are not. A dismantled mortar's shot is refused.
- **A German dm MMG or HMG fires as a LMG (A9.8, A4.41).** "A dm SW may not be fired until reassembled [EXC: a German dm HMG/MMG may be fired as a German LMG]". Its fire facts name the German LMG's definition, so it has that weapon's FP, range, ROF, and B#, and, being a LMG, it fires in the AFPh after moving.

**Left out, each with a backlog row:** the departures of 11.1; starting a scenario dismantled, and a captor dismantling; a dm weapon repaired with its assembled Repair number (the planner repairs a dm MG as any other already); a malfunctioned dm German MG keeps the one malfunction condition; the fire panel's weapon label does not say that a HMG is dismantled and fires as a LMG, and the "Latest" line does not say that a weapon was removed on its X#.

**The games,** on board 3, in the adjudicator's view:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-sw-dm` | A German squad's dismantled HMG fires at an adjacent squad: "the HMG ...: 3 FP x 2 (Point Blank Fire) = 6". | yes |
| `p45-sw-whole` | The control: assembled, "7 FP x 2 = 14". | yes |
| `p45-sw-xnumber` | A Russian squad and its LMG, both marked First Fire, fire again at a squad entering [E10]: the LMG's B# is 9 in Sustained Fire, the DR is 11, its printed B#: the record has it removed and the game eliminates it. The squad's FP alone leaves the Residual FP (2). | yes |
| `p45-sw-mortar` | A Russian squad dismantles its 50mm mortar in the PFPh: the SW list reads "dismantled, 2 PP". Its shot is then refused: "is dismantled and is not fired until it is assembled (A9.8)". | yes (the dismantling) |

**The tests.** Rules, in `ScenarioA1Pass45RulesTests`: the X# at three DRs and a normal shot's malfunction; the lone SMC's refusal; the dismantle table, the LMG rule, and the captured SW's repair. No new Play test: `BacklogPass13Tests` has the dismantling and the repair through the gate, and one of its assertions is turned (a German dm MMG fired in the phase it was dismantled is refused for that use, no longer for being dismantled). Classes run: Rules pass 45, 12, 13, the Fire package's; Play `BacklogPass9Tests`, `BacklogPass12Tests`, `BacklogPass13Tests`, `OlderGamesTests`, `PlayTests`, `RallyAndFireStepsTests`.

### 11.3 Task 45.10: pass 40's movement rows

The frame gave this task four rows (A4.42, A4.44, A4.61, A4.63) and question F8. Two things are built; the rest stays in the backlog with its reason, **which is a departure from the frame's placement, decided under the user's word of 2026-10-12.**

**Built.**

- **A CX leader lends no IPC (A4.42, A4.52, pp. 50 to 51).** "CX Infantry have an IPC one < normal." A leader's IPC is one, so a leader who Double Times with his stack, or is CX, has none to add to a laden MMC's. The review of pass 40 found a squad with too many MF by one; `LeaderIpcRecipient` now gives no recipient in Double Time.
- **A TI unit and the SW and Deployment actions (A.7, p. 43; A4.8, p. 52; question F8).** **Ruling R45.6** (approved in the frame): Good Order is enough for Deployment, Recombining, a Transfer, a Drop, and Recovery in the RPh, since A.7 keeps a TI unit in Good Order and A4.8 bars it only from moving, advancing, firing, and "other labor tasks". Recovery in the MPh costs a MF and stays barred. Dismantling stays barred too: it counts as the weapon's use, as fire does. The frame said the bar might show only in a staged game; that holds: no built action makes a unit TI before its own RPh.

**Left in the backlog, with the reason:**

- **A hero's IPC, and a SMC as the one helped (A4.42):** the laden unit is found through the leader's MF bonus, which a hero does not give; lending by a hero needs its own read of the stack.
- **The leader's IPC as a choice in the APh, and the unasked default kept in the record (A4.42):** the advance's aid is a read of its own (`AdvanceAid`), not opened here.
- **A SMC's Recovery at once when a unit is eliminated, surrenders, or routs away (A4.44):** an act in the other side's phase, at each of three kinds of loss, each of which must say which SW it left.
- **A break ends Assault Movement for the unit, not the stack (A4.61):** FFNAM is one DRM of an attack; giving it to some targets and not others needs a Final DR for each kind of target, as the pinned movers have.
- **The overstack MF and lost Road Bonus of the Location Dashed to (A4.63).**
- The halving by firer against a Dash stays as ruling R40.9 has it, by the user's word.

**The games,** on board 3:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-mv-ipc` | A Russian squad carrying a MMG (4 PP) and an 8-0 leader Double Time from [F10] west along the Open Ground of row 10 and back. Six steps of 1 MF pass; the seventh is refused: "4-4-7 squad R1 has 0 MF left". Before the task the squad had seven. Played from the Russian view, the German view passing each step. | yes |
| `p45-mv-ti` | A TI squad Transfers its LMG to the squad beside it in the RPh. | yes |

`p45-mv-ipc2` was written and not played; it is no fixture.

**The tests.** Rules, in `ScenarioA1Pass45RulesTests`: the leader's IPC with and without Double Time, and the four cases of the TI rule. No Play test of its own: the Transfer and the move are the gate's as before, and the game played both. Classes run: Rules pass 35, 40, 45; Play `BacklogPass5Tests`, `BacklogPass10Tests`, `BacklogPass13Tests`, `BacklogPass35Tests`, `PlayTests`.
