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
