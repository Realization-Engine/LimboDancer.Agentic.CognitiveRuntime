# ASL Unit Backlog Pass 40 Design

**Status:** The frame (increment 1), written 2026-10-11. **Answered by the user the same day: yes to F1 to F12 as proposed, and F13 done (section 5.5).** Increment 2a (stacking, 40.6) is built and committed (section 7). Increment 2b (roads, Dash, Hazardous Movement) is next. The pass runs as pass 35 did, at the user's word: each increment is designed, built on the branch `feature/asl-backlog-pass-40` (cut from main at 3c660afc), given a small test game in the Studio and its tests, and committed at the user's word.

**Date:** 2026-10-11

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 19 (decisions 5 to 7) and section 22.1 (c), "Pass 40: Infantry I"; the [rule inventory](<../ASL Rule Inventory A to E.md>), the 48 rows whose pass is 40; the [backlog](<../ASL Unit Backlog.md>), section 55.2; the [pass 35 design](<ASL Unit Backlog Pass 35 Design.md>) and its [review](<Scenario A1 Backlog Pass 35 Review 2026-10-09.md>); the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>).

**The rulebook.** Every rule named here was read on its page of the registered PDF on 2026-10-11, from the page texts of the tools folder: pages 43, 45 to 53, 61, 87 to 88, 113 to 114, 128, 130, and 173. The examples on pages 48, 49, 51, 52, and 61 were read with their rules. Two things were not read and are named where they matter: the figures of the examples (the text layer gives their captions, not the drawing), and the rules a row only points at (A7.25, A9.8, A12.15, C5.64, C10.3, D2.14, D6.5), which their increment reads.

**What the frame rests on.** The inventory's statuses and notes are of 2026-10-05. For this frame three read-only agents read the code as it stands on the branch, one for movement, one for entry into enemy Locations and fire at an entering unit, and one for stacking and the small rows. Their findings are cited below as "the code read"; the files and lines are theirs, and no line was opened by me. Nothing was built and nothing was run. Each increment opens the lines it changes before it designs the change.

## 1. Outline of this design

| Section | What | Increment |
|---|---|---|
| 1 to 6 | The frame: the outline, what the pass is, the rows by task, what is certain and what needs a run, what the frame found outside the rows, the questions | 1 |
| 7 | Stacking: 40.6 | 2a |
| 8 | Roads and movement in the open: 40.2, 40.3, 40.8 | 2b |
| 9 | Portage and movement corrections: 40.7 | 2c |
| 10 | Enemy Locations: 40.4, 40.5 | 2d |
| 11 | The small rows and the items taken from the backlog: 40.9 | 2e |
| 12 | Recorded games, the proofs, and how a difference is read | 3 |
| 13 | The questions together, and the user's answers | with each increment |
| 14 | As built | after the build |

Each task's subsection in sections 7 to 11 has the parts pass 35 used: the rule as read on its page; what the code does today, with file and line; the change as a Rules verdict and the facts it takes; the refusal texts; the games, with which are recorded; the tests; the questions, each with a proposal.

## 2. What the pass is

Block 1 of the run order, "Repairs and the ground under everything", after pass 35 and before pass 45. The plan gives it nine tasks and 49 rows. Task 40.1 (ADJACENT, A.8) was taken and built by pass 35, so eight tasks and 48 rows are left. No counter is new.

Where pass 35 repaired rules already built, this pass mostly builds what is missing beside them: a choice the mover has and the game makes for him (the road), a move that does not exist (Dash), an entry that is refused as unreviewed (Infantry OVR onto a lone SMC), an attack the DEFENDER may skip though the rule forces it (TPBF), and penalties charged in one phase and not in the others (overstacking, Hazardous Movement). As in pass 35, every decision is a calculator in Rules over facts, and the pass changes results on purpose, so each differing line of the gate's three proofs is named as one task's change or is a fault.

## 3. The 48 rows, by task

Statuses are the inventory's. "Today" is the code read of 2026-10-11 where it differs from or sharpens the inventory's note.

### 3.1 Stacking: 40.6 (increment 2a)

| Row | Page | Status | What is missing or wrong today |
|---|---:|---|---|
| A5.11 Movement | 52 | partly | The extra MF a squad-equivalent of excess is charged in the APh only. A MPh entry pays nothing, and a vehicle pays no MP for overstacked Personnel. |
| A5.12 Attack Penalty | 52 | partly | The +1 is on the CC DR and the ordnance To Hit DR. It is not on the IFT DR, and no count includes a vehicle, though the rule counts "each vehicle and squad equivalent". |
| A5.13 Defense Penalties | 52 | not built | In the MPh the penalties are for the moving units alone. No task cites this row (section 5.1). |
| A5.131 Personnel | 52 | partly | The -1 is on CC and To Hit, not on the IFT DR. |
| A5.132 Vehicular | 52 | not built | No hit on another vehicle of a vehicle-overstacked hex (rulings R8.10 and R11.6 leave it out). |
| A5.2 Vehicular | 52 | built with a deviation | Ruling R11.6: any number of vehicles share a Location at an MP cost. There is no limit of one a side, so no vehicle is ever "excess". |
| A5.3 PRC | 52 | partly | Passengers are left out of the Location's count, as the rule has it. Riders do not exist. No task cites this row. |
| A5.4 Combined Arms | 53 | partly | Holds by construction but for the limit of three Guns in use, which follows from A5.5. |
| A5.5 Equivalents | 53 | partly | A crew or HS manning a Gun counts as a squad at setup only (ruling R26.3); in play it counts half. |

**The copies.** The backlog says overstacking is counted four ways. The code read found five counts of a stack against the limit, and they disagree:

| Point | Advance | Close Combat | Ordnance | Setup | An entering stack |
|---|---|---|---|---|---|
| Five SMC | half a squad | half a squad | not counted | a tenth of a squad each past four | a fifth SMC is refused |
| Who is a SMC | the kind `asl:smc` | a leader or a hero | | the SMC kinds | the kind `asl:smc` |
| A crew manning a Gun | half | half | half | one | half |
| Units already in the hex | counted | counted | counted | counted | not counted |
| Dummies | by kind | left out | by kind | left out | not filtered |

The page (A5.1 and A5.5, pp. 52 to 53) settles each line: three squads or their equivalents and four SMC without penalty; five SMC equal a HS and four or fewer count nothing; two HS or crews equal a squad; a crew or HS manning a Gun is a squad; the excess is rounded up (the example on p. 52, where an excess of one and a half costs two MF). Two further copies are not stack counts and stay: the Passenger capacity (in PP) and the hard-wired check of one reviewed case (`IsReviewedThreeMfEntry`), which the increment reads to see whether the played path still reaches it.

### 3.2 Roads and movement in the open: 40.2, 40.3, 40.8 (increment 2b)

| Task | Row | Page | Status | What is missing or wrong today |
|---|---|---:|---|---|
| 40.2 | A4.132 Road | 48 | built with a deviation | Ruling R10.1: a road hexside is always crossed at the road rate. The mover cannot pay the other terrain's cost and take its cover. |
| 40.2 | B3.3 | 113 | built with a deviation | Fire never reads how the hex was entered. A mover that entered a woods or building road hex at the road rate keeps the TEM and takes no FFMO; the rule gives FFMO and no TEM. |
| 40.2 | B3.4 | 114 | partly | The Road Bonus is built and is lost to SMOKE, a burning wreck, rubble, mud, snow, a pushed Gun, a step off the road, a forced return, and NVR 0. Mines, Wire, roadblocks, debris, and Panji have no counters to read. |
| 40.2 | B13.31 | 128 | not built | No notion of a woods-road hex in Rules. Fire reads the terrain at the hex center. |
| 40.2 | B13.32 | 128 | partly | A vehicular OVR takes the Location's TEM; the exception for a vehicle on the road is absent. |
| 40.2 | B3.41 | 114 | partly | Built but for Graveyard roads and Convoy. No task cites this row. |
| 40.2 | B13.41 Vehicles | 128 | partly | Built but for a move off the road into the woods of a woods-road hex, and motorcycles. No task cites this row. |
| 40.3 | A4.63 Dash | 51 | not built | Nothing. |
| 40.3 | C6.1 Case J | 173 | partly | Case J, J1, and J2 are charged against a vehicle by the MP seen. Dashing Infantry is not a cause, nor is VBM. |
| 40.8 | A4.62 Hazardous Movement | 51 | partly | The -2 with no FFMO or FFNAM, and Case O for ordnance, are built for a crew pushing its Gun and in the MPh only. The rule says "regardless of fire phase until they are pinned". |

### 3.3 Portage and movement corrections: 40.7 (increment 2c)

| Row | Page | Status | What is missing or wrong today |
|---|---:|---|---|
| A4.11 MF | 48 | partly | A SMC that mounts, rides, or dismounts has four MF that Player Turn, and a wounded SMC or Inexperienced Infantry four in place of three. Neither is applied in loading or unloading. |
| A4.4 Portage | 50 | partly | The MF a SW cost come back when it is dropped in the middle of a move, and an item can be carried twice in a phase. |
| A4.41 AFPh SW Fire Limits | 50 | partly | Only a light mortar that moved is barred. A MMG, HMG, or INF or RCL SW that moved may fire in the AFPh. The note's "pushed Gun" looks wrong: the code read found that a push leaves Gun and crew TI, which bars the shot. |
| A4.42 IPC | 50 | built with a deviation | Ruling R10.8: the leader's IPC goes by itself to the one laden MMC of his stack. With two laden units it goes to neither, and no SMC but a leader lends it. |
| A4.43 Possession | 50 | partly | Unpossessed SW are not removed in marsh, water, a Blaze, or a rubbled building. No task cites this row. |
| A4.44 Recovery | 50 | partly | Missing: the SMC's Recovery at once when a unit is eliminated, surrenders, or routs away; the bar on a Bypassing unit; Recovery aboard vehicles. |
| A4.5 Double Time | 51 | partly | Not barred to a unit that mounts, rides, or dismounts in that Player Turn. No task cites this row. |
| A4.51 CX | 51 | built with a deviation | The counter leaves at the start of the unit's next MPh or when it breaks. The rule lists earlier moments; section 4 says why they may change nothing. The +1 on SW Availability and Search drs is missing. |
| A4.61 Assault Movement | 51 | partly | Any second step of an Assault move is refused, so MF spent inside a Location (a Recovery, a SMOKE attempt) and then the one change of Location is refused. Assault status is not lost when the unit breaks or goes berserk. |
| A4.72 vs Difficult Terrain | 52 | partly | The cost tested includes SMOKE's MF, which the rule leaves out. |

### 3.4 Enemy Locations: 40.4, 40.5 (increment 2d)

| Task | Row | Page | Status | What is missing or wrong today |
|---|---|---:|---|---|
| 40.4 | A4.14 Enemy Units | 49 | partly | The berserk exception is built. The Disrupted and Unarmed exceptions are not: a Known Disrupted or Unarmed unit bars the entry like any other. Human Wave is pass 180's; PRC and Cavalry dismounting are passes 65 and 165. |
| 40.4 | A4.15 Infantry OVR | 49 | partly | Entry onto a lone Known SMC is refused. One case rolls the NTC (a concealed SMC revealed by the attempt, with another concealed unit there) and always ends with the mover forced back. A passed NTC onto a truly lone SMC is refused as unreviewed. The leader's NTC for his stack is missing; the mover moves alone. |
| 40.4 | A4.151 SMC Options | 49 | partly | The SMC may fire through the ordinary window. Its move away to a Location of the ATTACKER's choice does not exist. |
| 40.4 | A4.152 CC | 49 | partly | The immediate CC in the MPh and the Melee after it are built for a berserk OVR only. Moving on with the MF left, other units then passing through, and a second OVR against a SMC held in Melee are missing. |
| 40.4 | A20.54 Attack Effects | 87 to 88 | partly | Unarmed units are "not an obstacle to movement" and are recaptured by entry and an immediate CC in the MPh; neither is built. Prisoners eliminated by their own side's fire count double; the fire's warning says so and the CVP count does not do it. |
| 40.5 | A8.311 Restrictions | 61 | partly | Infantry manning ordnance "must add their inherent FP to the FPF attack". Today a crew that fired its Gun makes the whole attack refused (ruling R8.4). The OVR Prevention exception (C5.64) is missing. |
| 40.5 | A8.312 TPBF | 61 | not built | The DEFENDER may always pass. The rule forces the attack on any Infantry or Cavalry MMC that enters the firer's Location, after Residual FP, as one Mandatory FG. |

### 3.5 The small rows: 40.9 (increment 2e)

| Row | Page | Status | What is missing or wrong today |
|---|---:|---|---|
| A1.31 Deployment | 45 | built with a deviation | No unit Deploys without a leader. The branch for a Guard's squad is dead code. |
| A1.32 Recombine | 45 | built with a deviation | Allowed at any point of the RPh, where the rule says its start; no "same terrain" test; no side check, where Deployment has one. |
| A1.6 US# | 45 | partly | Three copies, each answering 3, 2, or 1; a vehicle or a Gun reads as 1. The 4 and the 5 are missing. |
| A2.2 Grid Coordinates | 45 | built with a deviation | A seam hex is named by the board placed later, as VASL does, not by the northeasternmost board. No ruling records it. |
| A2.3 Half-Hexes | 45 | partly | A seam hex belongs to the later board alone. A unit limited to the earlier board cannot set up in its own half-hex, and one limited to the later board may, with no test of the other half. |
| A2.52 Offboard Actions | 46 | partly | Built but for SW starting dismantled, ordnance limbered, and an AFV's CE or BU as a choice at setup, which the audit did not check. |
| A2.9 Setup Limitations | 47 | partly | Built but for setup in Crest status (B20.9) and HD status (D4.221). |
| A3.9 Turn Record Chart | 47 | partly | Built but for provisional (gray) turns and a chart that runs past END. `LastPlayerTurn`, which the row names, does not exist. |
| A2.4 Cumulative Terrain Effects | 45 | partly | A hex with two in-hex features is read as one terrain. No task cites this row. |
| A3.2 PFPh | 47 | partly | OBA SMOKE and the labor tasks are missing. No task cites this row. |
| A4.13 Terrain Effects | 48 | partly | Every terrain outside the chart rows built is refused (ruling R10.1). No task cites this row. |
| A4.131 Hexside Costs | 48 | partly | Any hexside terrain but a wall or hedge is refused. No task cites this row. |

### 3.6 The count

| Task | Rows cited by the plan | Rows no task cites, placed here | The plan's estimate |
|---|---:|---:|---|
| 40.1 (built by pass 35) | 1 (A.8, no longer in the 48) | | 0:40 |
| 40.2 | 5 | 2 (B3.41, B13.41) | 0:40 |
| 40.3 | 2 | | 0:40 |
| 40.4 | 5 | | 1:00 |
| 40.5 | 2 | | 0:20 |
| 40.6 | 7 | 2 (A5.13, A5.3) | 0:40 |
| 40.7 | 8 | 2 (A4.43, A4.5) | 0:40 |
| 40.8 | 1 | | 0:20 |
| 40.9 | 8 | 4 (A2.4, A3.2, A4.13, A4.131) | 0:20 |
| **Eight tasks left** | **38** | **10** | **4:40** |

The 48 rows were listed from the inventory by a search for its pass column and each placed by hand against the plan's citations: 38 are cited, ten are not, and none is cited twice.

## 4. What is certain, and what needs a run

**A. The rule read on its page, and the code read finding the gap plainly.** The change is as stated unless the increment's own reading of the lines says otherwise.

- 40.6: the five counts and where they differ; no MPh MF; no IFT DRM; no vehicle in any count.
- 40.3: Dash does not exist.
- 40.5: the DEFENDER may always pass; A8.312 is cited nowhere in the code.
- 40.7: the MF come back after a drop; an item may be carried twice; the leader's IPC has no player input; no four MF in loading; the SMOKE MF inside the Difficult Terrain cost.
- 40.8: Hazardous Movement is refused as a fact outside the MPh.
- 40.9: the dead Guard branch; the three US# copies; the seam hex's one owner.

**B. Certain as a gap, with the shape of the change open until a member is read or a page beyond the row is read.**

| Task | Item | What is open | What settles it |
|---|---|---|---|
| 40.4 | Two entry paths | A "reviewed" path in `GamePlanner.cs` (one squad, a first step, a catalog of cases) and the general path (`EnemiesAtTarget`). The OVR's election lives on the first, the berserk OVR's CC on the second. | Both read whole in increment 2d; section 5.3 proposes which carries the OVR. |
| 40.4 | The berserk wiring | The CC at once is hard-wired to a charge: `OverrunSmc` asks for a berserk MMC, `OverrunAttacks` gathers berserk units, and capture and withdrawal are barred for "every berserk". | The members read; each test that is "berserk" restated as the rule has it (A4.152 against A15.432). |
| 40.5 | A crew's FP in FPF | Ruling R8.4 gives a crew that fired its Gun no inherent FP; A8.311 has it add that FP to FPF. | The rule ruling R8.4 cites, read on its page beside A8.311, and the ruling amended. |
| 40.7 | CX removal | The rule removes the counter after Prep Fire, on designation as an Opportunity Firer, on moving, on TI, or as the next MPh ends, and also says it is removed at the start of a MPh. The game removes it at the start of the MPh. | A walk through each moment for an effect before the MPh. My reading: there is none in the PFPh but the unit's own Prep Fire, which keeps its +1 either way. If so the row is built and only its two missing drs are left. |
| 40.7 | A pushed Gun in the AFPh | The inventory says it may fire; the code read says TI bars it. | One planner test. |
| 40.8 | How long Hazardous Movement lasts | "Regardless of fire phase until they are pinned" names no end. | C10.3 read; a ruling proposed in question F9. |

**C. Needs a run or a board before the change can be designed.**

| Task | Item | Why it is not certain | What settles it |
|---|---|---|---|
| 40.2 | The two target points of A4.132 | FFMO at the road rate holds "if the LOS can be traced to either of the two target points ... without crossing an obstruction in that hex": the hex center, or the point where the road crosses the hexside entered. Fire reads a hex-center LOS (ruling R11.2 took the same for Bypass). Whether Maps can trace to a point on a hexside, and whether it can say that a LOS crosses the target hex's own woods or building depiction, is not known. | Maps' LOS read, then a run on the rule's own example: board 5, [I4] and [I9], with firers in [G2], [H4], [I3], [I5], and [J4]. |
| 40.2 | A woods-road hex on a real board | **Settled by a run, section 5.5.** The woods-road hex [I4] of board 5 reads as Open Ground throughout: its center is Dirt Road. A squad enters it for one MF, and a shot from the woods of [I5] takes FFMO, which the example on p. 48 says it never does. | Done. |
| 40.2, 40.3 | A board for the games | **Settled by a run, section 5.5.** Boards 4 and 5 open and play in the Studio. | Done. |
| 40.3 | "Non-Open Ground" by the firer | A Dash's halving holds against a firer only if the Locations on each side of the road are not Open Ground to that firer (the example on p. 51: halved for the 4-4-7, not for the 6-2-8). A Location out of the firer's LOS counts as not Open Ground. | The Open Ground read of pass 35's Interdiction (ruling R35.1), which is already by firer; a run on the example, board 4 [BB7]. |
| 40.4 | The SMC's move away | A decision by the DEFENDER before any First Fire, then a choice by the ATTACKER among Accessible Locations, in the middle of the ATTACKER's move. The game has a declaration by the ATTACKER (decline or elect) and a window for the DEFENDER; it has no choice by one side that waits on the other's. | The window's states read; a small game. |
| 40.9 | A seam hex in a card's setup area | Whether any card of the library draws a setup area along a seam. | A search of the cards. |

**Rulings the changes touch.** Each needs the user's word on the ruling as well as on the code, and each increment quotes the ruling and proposes its new text: R10.1 (the road rate without the mover's choice), R10.8 (the leader's IPC assigned by the game; Hazardous Movement in the MPh only), R11.6 and R8.10 (vehicles share a Location without a limit; no hit on another vehicle), R26.3 (a manning crew is a squad at setup only), R8.4 (a crew that fired its Gun has no inherent FP), R27.3 (the berserk OVR, which becomes one case of the OVR), R13.4 (Deployment and Recombining), R35.9 (ADJACENT, for the marsh question), and R11.2 if the road's target points bring a LOS to a point.

**Tests that assert today's result.** Known from the reading: the two `DeclareOverrunTests` in which a passed or a declined NTC forces the mover back with nothing else there to force it, and `ConcealedEntryTests.AnEntryNoReviewedCaseCoversIsRefusedWithoutNamingWhatIsHidden` where its case is a lone SMC. Each increment lists what it finds.

## 5. What the frame found outside the 48 rows

### 5.1 The ten rows no task cites

| Row | What is left of it | Proposal |
|---|---|---|
| A5.13 | The MPh limits the penalties to the movers | 40.6: it is one fact of the member the task writes. |
| A5.3 | Riders | Nothing to build here. The row's remainder goes to pass 65. |
| B3.41 | Graveyard roads, Convoy | Nothing here. To pass 145 (the village terrain) with a note for Convoy (pass 175). |
| B13.41 | A vehicle leaving the road inside a woods-road hex; motorcycles | 40.2 if the increment gives a vehicle a place "on the road" in such a hex, which B13.31's exception needs anyway; otherwise pass 135. Motorcycles are pass 175's. |
| A4.43 | Unpossessed SW removed in marsh, water, a Blaze, a rubbled building | 40.7 for the Blaze and the marsh, as one Rules verdict the projector reads; water and rubble to passes 125 and 140. |
| A4.5 | No Double Time with a conveyance | 40.7, with A4.11: the same fact (the unit mounts, rides, or dismounts this Player Turn). |
| A2.4 | A hex with two in-hex features | Nothing here. To pass 135. |
| A3.2 | OBA SMOKE; the labor tasks | Nothing here. To pass 150, with a note for entrenching (pass 115). |
| A4.13, A4.131 | The refused terrain and hexsides | Nothing here. To pass 135. |

That places five in this pass and moves five on. If the user agrees, the inventory's count for pass 40 becomes 43 before anything is built.

### 5.2 Backlog rows of section 55.2 in this pass's ground

| Item | Proposal |
|---|---|
| ADJACENT between a marsh hex and dry ground (A.8, p. 43; B16.4, p. 130) | **A ruling, no code: they are ADJACENT.** A.8 asks that a hypothetical Infantry unit could move "from that Location into the adjacent Location" in the APh, and names Locations as ADJACENT as a pair. An advance out of the marsh onto dry ground is possible; none goes in. R35.9 read "either direction" for a level and a stairwell, and the pair of two marsh hexes is already not ADJACENT. What rides on it: a unit beside a marsh may use FPF against, accept the surrender of, and deny a rout to a unit in the marsh it cannot itself advance on. I know no official answer either way; the ruling says so. |
| Overstacking counted four ways | 40.6, its subject. Five counts, as section 3.1 has them. |
| Case J against a vehicle that has paid only its Start MP (C.8, C6.1) | **Take the target's half in 40.3.** C6.1 is this pass's row, and Dash makes Case J a fact of the target ("is Dashing; entered a new hex or used VBM this Player Turn; is or was in Motion") in place of the MP seen. The firer's half (Case B for a tank that only Started and Stopped) stays a row for pass 50. |
| The Gun's target list offers a Location with no LOS | **Leave, for the pass that reworks the fire panels (pass 45).** The list is one the small arms panel shares, built on the page, and it checks no range and no Covered Arc either; a LOS filter alone would be a third of the repair. The planner refuses the shot. The row stays. |
| The DFPh's disabled button, the "Latest" line, the hand-over prompt at 320 pixels | **Take the first and the third in increment 2e; read the second and take it if its cause is one line.** The pass's Studio check walks these views at 320 pixels, and 40.5 changes what the DEFENDER's view must say when a pass is refused. |
| `EnemyUnitFacts.Broken` | Remove it in increment 2d, which rewrites the facts beside it. |

### 5.3 Found by the code read

| # | What | Proposal |
|---|---|---|
| 1 | **Two entry paths.** The "reviewed" entry of `GamePlanner.cs` decides in Play who is revealed, who has the highest dr, the MF left, and the NTC's sum, and it charges a literal two MF. | 40.4 carries the OVR on the general path, where the berserk OVR already is, and moves the NTC and the election into Rules. The reviewed path's cases that the general path already answers (a concealed unit forces the mover back) are folded into it only where the increment shows the two give the same record; whatever is left is a backlog row, not this pass. |
| 2 | **Play decides Deployment, Recombining, Drop, and Recovery.** Rules gives three one-line members; the phase, the side, the leader, and the one-action rule are in `GamePlanner.SupportWeapons.cs`. | Move what 40.7 and 40.9 change (Recovery's bars, Deployment's and Recombining's eligibility) into Rules as they are changed. No move for its own sake. |
| 3 | **Three copies of the US#,** one with exact kind strings in the projector. | One Rules member in 40.9, with the 4 and the 5. |
| 4 | **Dead or stale:** the refusal reason `cc.overstacked-unreviewed` has no reader; the documentation of the ordnance models says vehicles are counted; the inventory's A3.9 row names a member that does not exist. | Removed or corrected in the task that touches each. |
| 5 | **A8.312 and prisoners.** Fire from inside a Location that holds prisoners is refused whole, so a Guard could not fire at a unit that enters. | Read in 40.5 against A20.54: only a berserk unit can enter a Guard's Location in the MPh. Taken there if the forced attack reaches it; otherwise a backlog row for pass 75. |
| 6 | **The estimate.** Section 3.6's 4:40 is the plan's. From the reading, 40.4 is not an hour (a new decision by the DEFENDER, a choice by the ATTACKER, the stack's NTC, the CC's un-wiring from berserk, movement after it), and 40.2 is not forty minutes if Maps needs a LOS to a point. | A first guess of the build: 40.6 1:15, 40.2 1:15, 40.3 0:50, 40.8 0:20, 40.7 1:00, 40.4 2:00, 40.5 0:40, 40.9 0:40, in all 8:00 against 4:40. Each increment's design sizes its own before it is built, and the plan's figure is changed only at the user's word. |

### 5.4 The games

Each task gets a game of a few counters and a control beside it under its own id, written by a script kept in `pass32-tools/pass40/`. The frame's proposal for where they stand, to be confirmed with the board viewer before any is written:

| Task | Game | Board |
|---|---|---|
| 40.6 | The example of A5.11: two squads and a crew in a building, a squad moving in for three MF, a second advancing in and becoming CX; a fire attack from and into the overstacked Location | Board 3, [O5] or [O10], or board 1 [F5] as the page has it |
| 40.2 | The example of A4.132: a squad into [I4] at one MF and at two, fired on from five hexes; the building [I9] entered from the road | Board 5 |
| 40.3 | The example of A4.63: a Dash through [BB7] to [AA7], fired on by one firer for whom it is a Dash and one for whom it is not | Board 4 |
| 40.8 | A crew pushing its Gun, fired on in the MPh and again in the DFPh | Board 3 |
| 40.7 | One game a correction where the page shows it: a MMG carried and fired in the AFPh; a SW dropped mid-move; two laden squads and a leader; an Assault move after a Recovery; an advance through SMOKE | Board 3 |
| 40.4 | A squad's OVR of a lone leader with each outcome (the NTC failed; the SMC moves away; the SMC stays, is eliminated, and the squad moves on; the SMC survives into Melee); a leader's NTC for two squads; an Unarmed unit recaptured by entry | Board 3 |
| 40.5 | A berserk charge into a squad's Location with the forced attack; a crew at its Gun adding its FP to FPF | Board 3 |
| 40.9 | Recombining at the start of the RPh; a Guard's squad Deploying; a setup along a seam, if a two-board card can be written by script | Board 3; two boards for the seam |

Which of them are recorded (played and confirmed, so the file holds the result) is said in each increment and in section 12.

### 5.5 Boards 4 and 5 in the Studio (the user's word of 2026-10-11, after the frame)

The user asked that boards 5 and 4 be brought in so the rule's own examples can be staged. What was found and done:

- **No import step exists or is needed.** The Studio loads any VASL board on demand from the VASL checkout (`VaslBoardProvider`), and the oracle fixtures hold hex facts for 235 boards, boards 4 and 5 among them. Earlier "imports" were the ingestion design (ASL-MAP-02) and the fidelity batch (ASL-MAP-05), which cover every board at once. Of the 228 live games, 125 stand on board 1, 19 on board 2, 71 on board 3, and 32 on board 4 (Gambit and the Armor Test). No game had ever stood on board 5.
- **Both boards are Verified.** Board 5 (VASL version 7.1) opens in the board viewer as Verified, with Open Ground, Gully, Wooden Building, Woods, Dirt Road, and a Single Hex Stone Bridge in its legend. Board 4 was already in play.
- **A game names its board with a version,** the Git blob id of the board archive's LOSData entry. `pass32-tools/pass40/board_version.py` computes it, and gives the recorded values for boards 3 and 4 (ba2920ce and 4fd9b95d); board 5 is 6be1f4b1.
- **`pass32-tools/pass40/make_board_game.py`** writes a small game on any single board at the German MPh, a squad a Location; `hexfacts.py` prints the oracle's facts of the hexes named.
- **The hexes of the examples, from the oracle's facts.** Board 5: [I4] has a Dirt Road center with woods hexsides, [H3] and [I10] are Dirt Road, [I9] is a Wooden Building, and [G2], [H4], [I3], [I5], [I8], and [J4] are Woods. Board 4: [BB7] is Dirt Road between hedges, [AA7] and [CC8] are Open Ground behind hedge hexsides, and [Y9] and [CC6] are Wooden Buildings. Both match the examples of pp. 48 and 51.
- **Two games, opened and played in my Studio on 6671** (the build of pass 35's last code commit, which is the branch's code):
  - `p40-bd05-road`, **recorded**: the German squad stepped from [H3] into [I4] ("enters [I4] (Open Ground) for 1 MF", confirmed). A shot from the Russian squad in the woods of [I5] was then proposed and cancelled: "DRM: - 1 (FFNAM, A4.6) - 1 (FFMO, A4.6)", "Target terrain: open ground". The example says fire from [I5] or [J4] never receives FFMO there. The file holds the step and not the shot.
  - `p40-bd04-dash`, **setup only**: the German squad in [CC8], Russian squads in [Y9] and [CC6]. A step into [BB7] was proposed ("enters [BB7] (Open Ground) for 2 MF", the hedge's MF with the road's) and cancelled.

**What this changes for 40.2.** The fault is wider than the inventory's row says. It is not only that a mover at the road rate keeps the woods' TEM; in a hex whose center dot is on the road the game sees no woods at all, for movement (no two MF entry to choose), for fire (no TEM, FFMO from every side), and for anything else that reads the Location's terrain (rout cover, concealment terrain, Interdiction). Before the mover's choice can be built, a road hex must carry its other terrain as a fact. The hex facts give each hexside's terrain, so a hex with a Dirt Road center and Woods hexsides is recognisable; whether that is enough to say which LOS crosses the woods symbol is the open part of question F4.

**Not seen:** the Russian view's hand-over on board 4, any vehicle on either board, the gully and the bridge of board 5 (refused terrain today, by ruling R10.1), and the LOS tab on either board. Board 4 has no LOS oracle fixture, as board 3 has none; board 5 has one.

## 6. The frame's questions

Each carries its proposal; the answer asked for is a veto or a change. **The user's answer of 2026-10-11: yes to F1 through F12.** The five rows F2 moves to other passes are moved in the inventory with the pass's documents.

| # | Question | Proposal |
|---|---|---|
| F1 | Is the order of the increments right: stacking, then roads and movement in the open, then the portage corrections, then enemy Locations, then the small rows? | Yes. An entry's cost is read by everything after it: the OVR pays "double the total MF cost of entry", which includes the overstack MF and the road's choice, and the forced TPBF reads the overstack DRM. The largest and least certain task (40.4) comes fourth, on ground already laid, and before the small rows so that it is not cut short. |
| F2 | Do the ten uncited rows go as section 5.1 proposes: five into tasks 40.2, 40.6, and 40.7, five moved to passes 65, 135, 145, and 150? | Yes. |
| F3 | Are a marsh hex and dry ground ADJACENT? | Yes, by a ruling R40.1 that keeps what is built and says no official answer was found. |
| F4 | The road's two target points (A4.132). If Maps can trace a LOS to a point and tell the target hex's own obstruction, build the rule whole. If it cannot, which way? | First give a road hex its other terrain as a fact (section 5.5: a woods-road hex reads as Open Ground for everything today). Then build the rule whole, and extend Maps with a LOS to a point on a hexside if that is what is missing, since Bypass (A4.34, ruling R11.2) and Snap Shots wait on the same thing. The increment first shows the run on board 5 and the size of the Maps change, and stops there if it is more than an hour. The fallback, should the user prefer it: the mover's choice and its cost now, with FFMO at the road rate judged by the hex-center LOS alone under a ruling, and the second target point a backlog row. |
| F5 | Infantry OVR "in full": does the pass build all of A4.15 to A4.152, with the leader's NTC for his stack, the SMC's move away to a Location the ATTACKER chooses, the CC at once, movement after it, and a second OVR against a SMC in Melee? | Yes, all of it, on the general entry path, with the berserk OVR of R27.3 becoming one case of it. It is the task most over its estimate (two hours against one). Left out with a row each: a SMC in an AFV (pass 65), Human Wave (pass 180), a minefield, FFE, or Wire among the Locations the SMC may be sent to (their passes; Open Ground is read). |
| F6 | How is the forced TPBF of A8.312 put to the DEFENDER? | The DEFENDER's pass is refused while the attack is owed, with the reason and the rule; the fire panel offers the one Mandatory FG that Rules composed (every armed, unbroken Infantry unit there not in Melee, by First Fire, Subsequent First Fire, or FPF as its markers stand, a SMC with a First-Fired MG left out), and the DEFENDER confirms it. The DEFENDER keeps the one free choice the rule leaves, a leader's direction. The game does not fire on its own: every attack so far is a confirmed proposal. |
| F7 | Overstacking: one Rules member by A5.5 as the page has it, read by the MPh, the APh, the IFT, the To Hit DR, CC, and setup; a manning crew or HS a squad in play too (R26.3 amended); vehicles limited to one a side for the attack penalty (R11.6 amended), with the MP cost kept. And A5.132, the hit on another vehicle? | Yes to the member and both amendments. A5.132: build it, last in the increment. It is a dr and a Random Selection after a To Hit DR that just missed, the plan's task names it, and block 2 (armor) reads this pass. If the increment has passed its time by then, it moves to pass 50 with a row. |
| F8 | Of the portage corrections, two reach into pass 65's ground (a SMC's four MF and no Double Time when it mounts or dismounts) and one into pass 45's (the German dismantled MMG or HMG firing as a LMG). | Take the conveyance MF and the Double Time bar: each is a fact to a member this pass changes. Leave the dismantled MG to pass 45, which manufactures the dm values. A4.44's Recovery "regardless of phase" by a SMC when a unit is eliminated or routs away is taken; Recovery aboard a vehicle is pass 65's. |
| F9 | How long does Hazardous Movement last, and which acts? | Only pushing a Gun exists; every other act arrives with its own pass and reads the same fact. A ruling R40.n: the -2 holds from the push until the unit is pinned or broken, or the Player Turn ends, in every fire phase between. To be confirmed against C10.3 in the increment. |
| F10 | The small rows (40.9): what is built, what is ruled, what moves on? | **Built:** a Guard's squad and an Unarmed unit Deploy and Recombine without a leader; Recombining at the start of the RPh only; one US# member with the 4 and the 5; a seam half-hex belongs to both boards for setup, entry, and occupation; a CE or BU choice at setup if the read finds none. **Ruled, no code:** a seam hex keeps the name VASL gives it (A2.2), because the boards' fixtures come from VASL. **Moved on with a row:** Finns, U.S.M.C. 7-6-8s, and Carrier HS (passes 80 and 180); "the same terrain" for Recombining, and HD status (pass 100); Crest status (pass 125); a SW dismantled (pass 45) and ordnance limbered (pass 110); the US# of horses (pass 165). **Provisional turns and a chart past END:** built with the first card that prints one, under a ruling, since each hangs on its card's own SSR and no card of the library has one; the search of the cards in the increment confirms that or the task is built. |
| F11 | The backlog's rows of section 5.2: Case J's target half taken in 40.3; the DFPh sentence and the 320 pixel prompt taken in 2e; the Gun's target list left to pass 45; `EnemyUnitFacts.Broken` removed in 2d. | Yes. |
| F12 | The estimate: the frame's first guess is eight hours of building against the plan's 4:40 for the eight tasks. Is the plan's figure changed now? | No. Each increment sizes itself before it is built; the plan is changed once, with the pass's documents, to what the clock showed, as pass 35's was. |
| F13 | The games on boards 5 and 4, where the rule's own examples stand, in place of board 3? | **Answered by the user on 2026-10-11: yes, and done** (section 5.5). Both boards play in the Studio, and the games `p40-bd05-road` and `p40-bd04-dash` stand on the examples' hexes. |

## 7. Stacking: 40.6 (increment 2a)

Built on 2026-10-11 and committed at the user's word of the same day, with yes to the two rulings, to the reading of A5.13, and to moving A5.132 to pass 50.

### 7.1 The rules, as read on their pages

- **A5.1 (p. 52):** each side may stack three squads or their equivalents and four SMC in a Location without penalty, each level of a hex its own Location; never exceeded at setup or offboard setup.
- **A5.11 (p. 52), with its example:** entering a Location overstacked by Personnel, "even if overstacked only due to the moving unit's entry", costs one more MF for each squad-equivalent (FRU) above the limit. A vehicle pays one MP for each, beside D2.14's. Personnel entering a Location overstacked by vehicles pay nothing. The example: two squads and a crew in a building; a squad enters for three MF; a second squad advances in for four and becomes CX; a HS or crew after that pays two for the overstacking.
- **A5.12 (p. 52):** all units attacking from or within a Location their side overstacks add one to their IFT or CC DR, or to their To Hit DR, "for each vehicle and squad equivalent (FRU)" above the limits.
- **A5.13 (p. 52):** in the MPh the penalties apply to the moving units alone, though the units that did not move decide whether the mover is overstacked at the instant of attack.
- **A5.131 (p. 52):** Personnel attacked in a Location their side overstacks take -1 on the To Hit DR, or on the IFT or CC DR, for each squad-equivalent above the Personnel limit. PRC are excepted.
- **A5.132 (p. 52), with its example:** a Vehicle Target Type shot that misses a vehicle in a vehicle-overstacked hex by less than the number of vehicles there may hit another of them on a dr.
- **A5.2, A5.3, A5.4 (pp. 52 to 53):** one vehicle a side; wrecks do not count; PRC are not in the Location's count; vehicles do not change the Personnel limit; no limit for SW or ordnance.
- **A5.5 (p. 53):** five SMC equal a HS and four or fewer count nothing; two HS or crews equal a squad; a crew or HS manning a Gun is a squad.
- **A7.52 (p. 57),** read for a Fire Group: a detrimental DRM of any member applies to the whole FG.

### 7.2 What the code did

Five counts, as section 3.1 has them, each read by me on its lines before the change: `ScenarioA1AdvanceCalculator.OverstackExcess` (the advance), `ScenarioA1CloseCombatCalculator.Overstack` (CC), `ScenarioA1OrdnanceEligibility.OverstackExcessForOrdnance` with `LiveOrdnance.Excess` (the To Hit DR, and the ordnance record's verifier), `ScenarioA1SetupCalculator.SetupStackSquadEquivalents` (setup), and `EntryStackOverstacked` (an entering stack). A MPh entry paid nothing, a vehicle paid nothing for Personnel, the IFT DR took nothing, and no count held a vehicle.

### 7.3 The change

**One count in Rules, `ScenarioA1Stacking`** (a new file): `StackUnitFacts` (a counter's kind, whether it mans a Gun, whether it is a prisoner), `StackCount` (squads, other HS and crews, manning HS and crews, SMC, vehicles), and from it the squad-equivalents, the Personnel excess rounded up, the vehicles above one, the attack penalty (both), and the defense penalty (Personnel alone). `GroupAttackPenalty` takes the largest of a Fire Group's Locations (A7.52). **Play hands over the facts in one place,** `LiveStacking.Of`: a side's active counters in a Location, with any units about to arrive.

Its readers:

| Reader | What it reads | What changed for it |
|---|---|---|
| The MPh entry (`EntryCost`, a new fact `OverstackExcess`) | The Personnel excess of the stack the entry makes | New: one MF for each. Not for a stack occupying the obstacle it Bypasses (it is in the Location already), and not in marsh (all MF). The move's summary says how many MF are for the overstacking. |
| A vehicle's entry and its Bypass of a new hex | The Personnel excess of its side there | New: one MP for each, beside D2.14's and outside the woods doubling. |
| The advance | The same member | The count is the shared one; a manning crew is now a squad. |
| The IFT DR (two new facts of the attack, `FirerOverstack` and `TargetOverstack`) | The attack penalty of the firers' side, the defense penalty of the targets' | New: "overstacked" +n (A5.12) and "vs-overstacked" -n (A5.131), for every attack on the IFT but the resolution of an ordnance hit, which took them on its To Hit DR. The record's verifier takes both as recorded, like the other map reads. |
| The To Hit DR | The same two penalties | Five SMC now count as a HS, a manning crew as a squad, and the firer's side's vehicles above one count (ruling R11.6 said they do; the code did not). |
| Close Combat | The shared count over the CC's units | A manning crew is a squad (a new optional fact `Manning` of a CC unit). |
| Setup | The shared count, vehicles left out | Five SMC are a HS and four nothing; the old count took a tenth of a squad for each SMC once there were five. |
| An entering stack | The shared formula | Five SMC are a HS; the old test refused a fifth SMC outright. The refusal's text says so. |

**A5.13** needs no code of its own: in the MPh a First Fire attack reaches only the movers, and the count at their Location holds the units that did not move, which is what the rule says decides it.

**Not changed:** the Passenger capacity (in PP, A5.3) and `IsReviewedThreeMfEntry`, the fixed check of one reviewed case that the semantic candidate reads.

### 7.4 Rulings

- **R40.2 (approved 2026-10-11; amends R26.3 and R8.10):** a stack is counted one way in every phase, by A5.5 as the page has it. A crew or HS manning a Gun is a squad in play as at setup. Five SMC are a HS for the To Hit DR too.
- **R40.3 (approved 2026-10-11; carries out R11.6):** a side's vehicles above one in a Location add to its attack penalty on the IFT, To Hit, and CC DR. The MP cost of D2.14 is unchanged, and no entry is refused for vehicles.
- **A5.13, as read (approved 2026-10-11):** the title is "Defense Penalties" and its subsections are the two defense penalties, so it is read as limiting those to the movers. It is not read as freeing a DEFENDER's own overstacked Location from the +1 of A5.12 when it fires in the MPh. Said here because the sentence alone could be read the wider way.

### 7.5 A5.132: moved to pass 50 (the user, 2026-10-11)

Question F7 was answered "build it, last in the increment". Reading it against the code changes that, so it is put back to the user and not built:

- The diverted hit lands on **another vehicle**, so the shot needs the To Kill facts of every other vehicle in the hex (its armor, its facing to the firer, its size for the Random Selection's drm) and a second resolution in the same record. Today an ordnance shot's record holds one vehicle target.
- That is the armor pipeline's record, which pass 50 (Armored combat I) reworks.

**Proposal:** A5.132's row moves to pass 50, with a backlog row. Rulings R8.10 and R11.6 keep their sentence that it is not built.

### 7.6 The games

Written by `pass32-tools/pass40/make_stack.py` on board 1, the example's own building [F5] (the page's crew is a HS here; they count the same). Both are **recorded**: the lines below are from the game files.

| Game | What it holds |
|---|---|
| `p40-stack` | Two German squads and a HS in [F5]. A third squad enters from [F4]: `movement-step`, 6 half MF (three MF). The Russian squad in [E6] First Fires at it: DRM stone building +3, FFNAM -1, vs-overstacked -1. In the AFPh a squad in [F5] fires at [E6]: DRM overstacked +1. In the APh a fourth squad advances in from [G5]: "becomes CX advancing into Difficult Terrain (A4.72); the Location is overstacked by 2 squad-equivalent(s), which costs 2 more MF (A5.11)", as the example has it. |
| `p40-stack-control` | A squad and a HS in [F5]. The squad enters for 4 half MF (two MF), and the same shot takes +3 and -1 and no overstacking DRM. |

**A fault the game's first run hid and the Play test found.** In the first run both overstacked shots were proposed with the right DRM, and my script took the click on Confirm for a commit. The gate had refused both: the fire record's verifier did not carry the two new facts, so the record did not match. The Play test failed on exactly that, the game file showed no `fire-resolved` event, the verifier was repaired (`LiveFire.cs`), and both games were written again and played with each commit read back.

**Not staged:** a vehicle paying MP for overstacked Personnel, two vehicles of one side firing, a manning crew in a stack, five SMC, a Fire Group from two Locations, Close Combat in an overstacked Location, and setup. Each rests on its Rules test or on a test that was already there.

### 7.7 Tests

- **Rules, new:** `ScenarioA1Pass40RulesTests` (seven tests: the count by the example and by A5.5, vehicles in the attack penalty only, prisoners and uncounted kinds, the Fire Group's largest penalty, the MPh entry's MF, a vehicle's MP in Bypass, the move's summary) and `ScenarioA1Pass10Tests.OverstackingIsOnTheIftDrFromTheFirersSideAndAgainstTheTargets` (kept with that class's attack helpers).
- **Rules, changed with the rule:** `TheSetupStackingCountsFiveSmcAsAHalfSquad` (was `...CountsSmcInTenthsBeyondFour`: a stack of two squad-equivalents and five SMC is no longer refused entry); three assertions of `ScenarioA1Pass14Tests` and one of `ScenarioA1CloseCombatRulesTests` read the new member.
- **Play, new:** `BacklogPass10Tests.AnEntryInTheMphThatOverstacksCostsOneMoreMfAndTheStackTakesTheFireDrm`, with a control step in the same game.
- **Run:** 149 Rules tests in seven classes, passed. 109 Play tests in six classes (movement, the advance and CC, entry, ordnance, setup, vehicles): 108 passed and the new one failed as told above; after the repair its class of 21 passed. The other five Play classes were not run again after the repair, which touched only the fire record's verifier.

### 7.8 Recorded games

- A record made before this pass keeps replaying where the planner's read is recorded with it: a fire attack (the two penalties are taken as recorded), a move and an advance (their MF are in the event).
- **An ordnance shot's overstacking is recomputed at replay,** as before the pass. A recorded shot fired from or into a Location with five or more SMC, a manning crew or HS beside two and a half squad-equivalents or more, or two vehicles of the firing side, will no longer match its record. None is known; the gate's replay of the live games will say.
- Not run in this increment: the replay digest, the sweep, and the text list. Three texts changed or were added on purpose (the entry stack's refusal, the move's overstacking clause, four display names of the DRM).

## 8. Roads and movement in the open: 40.2, 40.3, 40.8 (increment 2b)

### 8.1 Task 40.2: the road

Built on 2026-10-11 and committed at the user's word of the same day, with yes to rulings R40.4, R40.5, and R40.6.

**The rules, as read on their pages.**

- **A4.132 (p. 48), with both examples:** Infantry entering a hex across a road hexside pays the road's cost or the cost of the hex's other terrain. Paying the other terrain's cost it is in that terrain. Paying the road's, it is subject to FFMO if the LOS can be traced to either of two target points without crossing an obstruction in that hex: the hex center dot, or the point where the hexside crossed meets the road. A Hindrance or Artificial Terrain there negates the FFMO.
- **B3.3 (pp. 113 to 114):** the other terrain of a road hex gives its TEM; a unit that moved in at the road rate is subject to Interdiction and FFMO in its place, unless the LOS is traced through other terrain between the firer and the target points.
- **B3.4, B3.43 (p. 114):** the Road Bonus, for a unit that crosses only road hexsides in its MPh; not for one that pays extra MF for the cover of woods.
- **B13.31 (p. 128):** a mover in a woods-road hex has no woods TEM in Defensive First Fire, and takes FFMO, if the LOS does not cross a woods symbol and it entered at the road rate; "otherwise, the normal woods TEM is in effect", and a vehicle there is always on the road.
- **B13.32, B13.41 (p. 128):** a vehicular OVR in a woods-road hex takes the woods' +1 but against a vehicle on the road; a vehicle may leave the road into the woods of its hex at the woods' cost.

**What the code did** (read on its lines, and seen in the Studio on board 5, section 5.5): a hex whose center dot is on a road read as Open Ground for everything; a road hexside was always crossed at the road's cost; fire never read how a hex was entered.

**The change.**

1. **A woods-road hex is woods** (`ScenarioA1Definitions.IsWoodsRoadHex`, read by Play's `TerrainKey`): a road at the center dot and Woods on one of the hex's hexside Locations. That is VASL's own test (`VASLBoard.fillWoodsRoadHexes`, which the map builder already ports for the NoWoodsRoads SSR). By it boards 1 to 4 have no such hex and board 5 has eleven ([I4], [J3], [K3], [L2], [M3], [N3], [O4], [P4], [P5], [Q6], [R6]; `pass32-tools/pass40/woods_road.py`), so no game played before the pass changes. Such a hex now costs two MF across a hexside without a road, gives its TEM to a unit that is not a mover at the road rate, and is woods for a rout's cover and for concealment.
2. **The mover's choice** (`GroundStep`, a new argument; the move's new argument `terrainRate`; a checkbox on the Movement panel): across a road hexside into a hex that has other terrain the unit pays the road's cost unless it chooses the other terrain's. The choice is refused where there is nothing to choose: no road hexside, a hex of Open Ground, an entry from off board, a Bypass, an obstacle occupied. Paying the other terrain's cost gives up the Road Bonus (B3.43).
3. **The step records that it was at the road rate** (`MovementStepped.RoadRate`, the event's `roadRate`, carried in the movement state): the unit is on the road and not in the other terrain. SMOKE in the hex ends the Road Bonus and not this.
4. **The two target points, in Maps** (`LosTargetPoints.CrossesTerrainInTargetHex`, a new file of 45 lines beside the LOS calculator): whether the straight line from a firing hex's LOS point to the target hex's center dot, or to the edge point of the hexside crossed, passes inside the target hex over a cell that is neither open nor hexside terrain. VASL's walk is not touched: its LOS to a center Location never asks about the target hex's own terrain. The LOS up to the hex stays the calculator's.
5. **Fire reads it** (`FireMapFacts`, with `ScenarioA1Definitions.RoadRateTargetTerrain`): for direct fire in the MPh at a stack that entered its Location at the road rate, the target terrain is Open Ground when every firing Location reaches one of the two points clear (A7.52: the worst case of a group's Locations is the group's), and the Location's terrain otherwise. Open Ground then brings FFMO by the rule already built, with its own bars (a Hindrance, an AFV or wreck, Height Advantage, a wall or hedge).
6. **A vehicle in a woods-road hex is on the road** (B13.31 EXC): fire at vehicles alone there reads Open Ground.

**Rulings (approved 2026-10-11).**

- **R40.4:** a woods-road hex is named as VASL names it. Orchard, brush, and grain beside a road at the center dot are not read this way (none is on boards 1 to 5); a backlog row.
- **R40.5 (amends R10.1):** the road's cost is the default across a road hexside and the other terrain's cost the mover's choice, declared with the step.
- **R40.6:** the point "where the hexside crossed intersects the road" is the edge point of that hexside, which VASL keeps for every hexside. On the example it gives the page's answer for all twelve cases.

**What the run settled (question F4).** No LOS to a point had to be added to the VASL walk, and the Maps change is far under the hour set as its limit. The test `LosTargetPointTests` runs the page's example on the real board 5: [G2], [J3], [K3], and [L2] reach [I4]'s center dot clear; [I5] and [J4] reach neither point clear; [H4] and [I3] reach only the H3-I4 hexside; [I8] reaches neither point of [I9], and [I10] reaches its road hexside.

**The games,** on board 5, written by `make_board_game.py`. All three are **recorded**; the lines are from the game files.

| Game | What it holds |
|---|---|
| `p40-road` | The squad enters [I4] from [H3]: "(woods) for 1 MF, at the road rate: on the road, not in the hex's other terrain (A4.132)". First Fire from [I5]: woods +1, FFNAM -1, no FFMO. From [H4]: FFNAM -1, FFMO -1. From [G2], at range 2: FFNAM -1, FFMO -1. |
| `p40-road-control` | The same squad enters [I4] with the other terrain's cost chosen: "(woods) for 2 MF". First Fire from [H4]: woods +1, FFNAM -1. |
| `p40-road-building` | The squad enters the building [I9] by the road of [I10] for one MF, at the road rate. First Fire from [I8], behind the building: wooden building +2, FFNAM -1. From [H9] and from [J9]: FFNAM -1, FFMO -1. |

The frame's game `p40-bd05-road` holds one step recorded by the build before this task; it is left as the record of the fault and is not a fixture.

**Tests.**

- **Maps, new:** `LosTargetPointTests` (two tests on board 5; they run where a VASL checkout is configured, as the other board tests do, so not on CI).
- **Rules, new,** in `ScenarioA1Pass40RulesTests`: a woods-road hex by its facts; the road's cost against the other terrain's, with SMOKE, Open Ground, and a hexside without a road; the terrain a firer reads from its two points, for one Location and for a group; the move's summary.
- **Play, new:** `BacklogPass10Tests.AMoverAtTheRoadRateIsOnTheRoadForAFirerThatSeesItAndInTheWoodsForOneThatDoesNot`, with the terrain cost chosen as its control. Its LOS stub answers the target points by the firer's hex.
- **Run:** 57 Rules tests in three classes and 99 Play tests in five classes (movement, entry, vehicles, fire, the rout), all passed; the two Maps tests passed.

**Left out, each for a backlog row.**

- On a map of several boards the two target points are not read, so a mover at the road rate keeps the Location's TEM and takes no FFMO there, as before the pass. Gambit and the Armor Test stand on such maps; they have buildings beside roads and no woods-road hex.
- Interdiction of a rout at the road rate (B3.3) and Residual FP against a mover at the road rate still read the Location's terrain.
- A vehicle that leaves the road inside a woods-road hex (B13.41) is not tracked: it pays the woods' cost across a hexside without a road and is then read as on the road.
- Orchard, brush, and grain beside a road at the center dot (ruling R40.4).
- A step that spends MF inside the Location after a road-rate entry (a SMOKE attempt) writes a new step, and the unit is then read as in the terrain.

**Not seen or not run.** A vehicle in a woods-road hex, a Fire Group from two Locations, a Bypass of a woods-road hex, a rout into one, and the Movement panel at narrow widths were not staged. The page's component tests (`MovementComponentTests`), the replay digest, the sweep, and the text list were not run. Texts added on purpose: the refusal `play.move-terrain-rate` (two wordings), the move's road-rate clause, and the checkbox's label and title.

### 8.2 Task 40.3: Dash

Built on 2026-10-11 and committed at the user's word of the same day, with yes to rulings R40.7, R40.8, and R40.9.

**The rules, as read on their pages.**

- **A4.63 (pp. 51 to 52), with both examples:** Infantry declares a Dash to a particular Location before it moves, then moves from a non-Open Ground Location on one side of a road directly into the road and directly into a non-Open Ground Location on the other side, if the normal MF for the two-hex move is within its MF. A Location out of a firer's LOS counts as not Open Ground. The unit may have spent no MF before (but for a SMOKE grenade attempt), spends none in the road or the Location Dashed to beyond the cost of entry, and ends its MPh in the Location Dashed to, unless it becomes berserk, pinned, broken, or wounded in the road Location or cannot enter, when the Dash's benefits cease. All non-ordnance Defensive First Fire at it in the road Location is Area Fire, but for Fire Lanes and Residual FP already there; FFMO and FFNAM apply as usual. A weapon with a CA may not fire at it there if it must change its CA. Ordnance uses Case J, without Cases J3 and J4. Blocked from the Location by a concealed unit, it is back in the road Location with no Dash. "A declared Dash might not qualify as such vs one or more DEFENDING units." No other movement or MF expenditure is open to a Dashing unit, and Armored Assault is NA.
- **C6.1 (p. 173):** Case J, +2, against a Dashing target.
- **B3.43 (p. 114):** no Dash in a road hex covered by rubble or debris.

**What the code did:** nothing; the word was in no rule file.

**The change.** The rule is one Rules file, `ScenarioA1Dash`; Play hands it the facts.

1. **The declaration** is the move's new argument `dash`, the Location Dashed to, given with the stack's first step, which is the step into the road Location ("Dash to" on the Movement panel). `StepBar` refuses it unless: no mover has spent MF and it is the first step; it is not from off board, by a berserk unit, or with Assault Movement, a Minimum Move, Bypass, a pushed Gun, or the other terrain's cost; the Location entered has a road at its center; and the Location Dashed to is at ground level, beside the road Location, and neither the Location left nor beside it. The MF of both steps together must be within the movers' MF, by the check the step already makes.
2. **The step records it** (`MovementStepped.Dash`, the event's `dash`), and the movement state carries it while the stack is in the road Location.
3. **While it is pending** the only step is into the Location Dashed to, at its cost of entry: any other step, any option with it, and a SMOKE grenade or DC placed with MF there are refused, and so is ending the move in the road Location. A stack pinned, broken, or berserk there has no Dash any more, and may end its move.
4. **On arrival** the stack's MPh ends when the DEFENDER's window on that step closes (`EndingMembers`, a new fact). A stack forced back by a concealed unit stays in the road Location, its move ended as before, with no Dash.
5. **Fire.** The attack's new fact `Dash` is set for the DEFENDER's First Fire, Subsequent First Fire, and FPF at the stack in the road Location (not an ordnance hit, a Fire Lane, or Residual FP) when `QualifiesAgainst` says so: a firing Location to which neither the Location left nor the Location Dashed to is Open Ground. Open Ground to a firer is read as Interdiction reads it since pass 35 (the terrain, a clear LOS with no Hindrance, and no TEM against that firer). The package then halves the FP as "dash" (A4.63). The fire record's verifier takes the fact as recorded.
6. **Ordnance.** The shot's movement gains `Dashing`, set by the same read from the Gun's Location: Case J (+2) in place of Cases J3 and J4, and the shot is outside the package ("dash-covered-arc") when the Gun must turn.

**Rulings (approved 2026-10-11).**

- **R40.7:** "on the other side of the road" is a ground-level Location beside the road Location that is neither the Location left nor beside the Location left. The page gives no geometry beyond its example ([CC8], [BB7], [AA7] of board 4, which this admits).
- **R40.8:** a declared Dash is not refused for its terrain. Whether it qualifies is decided against each firer, as the page says; a Dash between two Locations that are Open Ground to every firer is legal and changes nothing but the stack's freedom to do anything else.
- **R40.9:** a Fire Group whose Locations differ takes the Dash's halving if it holds against any one of them (A7.52).

**The games,** on board 4, the example's own hexes, written by `make_board_game.py`. Both are **recorded**; the lines are from the game files and the proposals' reviews.

| Game | What it holds |
|---|---|
| `p40-dash` | The squad in [CC8] steps into [BB7] for 2 MF (a hedge), "Dashing to [AA7]". First Fire from [Y9]: "4 FP x 0.5 (Area Fire at a Dash, A4.63) = 2", FFNAM -1, FFMO -1, as the example has the 4-4-7 halved. First Fire from [CC6], to which [AA7] is Open Ground: 4 FP, not halved, hedge +1, FFNAM -1. After the DEFENDER's pass, ending the move is refused ("ends its MPh there, not in the road Location") and so is a step to [BB6] (both read and cancelled, so not in the file). The step into [AA7] for 2 MF; a shot at it there is Subsequent First Fire's Area Fire, not the Dash's; after the pass no unit is offered to move. |
| `p40-dash-control` | The same squad enters [BB7] with no Dash: the fire from [Y9] is 4 FP, not halved. |

The frame's game `p40-bd04-dash` holds its setup alone and is not a fixture.

**Tests.**

- **Rules, new,** in `ScenarioA1Pass40RulesTests`: the declaration's bars; the pending Dash's bars, the end bar, and when the benefits cease; against which firers it qualifies and what Open Ground to a firer is; Case J for ordnance without J3 and J4; the move's summary and the arrival's end. In `ScenarioA1Pass10Tests`: the halving on the IFT.
- **Play, new:** `BacklogPass10Tests.ADashIsAreaFireInTheRoadLocationAndEndsWhereItWasDeclaredTo`, with a move through a road and no Dash as its control. Its first run failed on the control alone: the same squad fired twice, and A8.3 rightly refused its Subsequent First Fire beyond the closest enemy; the control has its own firer now.
- **Run:** 71 Rules tests in four classes passed. 90 Play tests in five classes (movement, ordnance, fire, the rout): 89 passed and the new one failed as told; its class of 23 passed after the test's repair.

**Left out, each for a backlog row.**

- The SMOKE grenade attempt before a Dash (the rule's one exception to "no MF spent").
- "Wounded in the road Location": the game cannot tell a wound taken there from one carried in, so a wound alone does not end the Dash.
- A Dashing stack that has not the MF left to enter (the example's leader who breaks) may not end its move in the road Location unless it is pinned, broken, or berserk; Double Time, as the example has it, is open to it.
- Recovery, a Drop, and other actions with their own commands are not barred to a Dashing stack.
- The CA limit for a vehicle's MG and for a MG with a Covered Arc; only a Gun's shot is barred.
- Armored Assault (D9.31) is not built, so its bar has nothing to bar.
- B3.43: rubble and debris on a road. A rubble hex's center is not a road in the map's terms, so no Dash is offered there; debris has no counter.

**Not seen or not run.** No ordnance shot at a Dashing unit was staged, and the CA bar has no test: Case J rests on its Rules test, and the bar on reading. A Fire Group from two Locations, a Dash forced back by a concealed unit, a Dash by a stack with a leader, and the panel at narrow widths were not staged. The page's component tests, the replay digest, the sweep, and the text list were not run. Texts added on purpose: three refusals `play.move-dash`, `play.end-move-dash`, the move's Dash clause, the fire's "Area Fire at a Dash", the reason `dash-covered-arc`, and the panel's field.

### 8.3 Task 40.8: Hazardous Movement in every fire phase

Built on 2026-10-11 and committed at the user's word of the same day. It ends increment 2b.

**The rule, as read on its page.** A4.62 (p. 51): certain activities "automatically incur a -2 IFT DRM to any attacks against units so engaged regardless of fire phase until they are pinned"; neither FFMO nor FFNAM applies to such a shot; it is cumulative with all other terrain DRM. The acts are pushing a Gun; Flame, roadblock, or rubble Clearance; descending paratroops; Fording; preparing a Set DC; Climbing; and sewer movement; never a vehicle, but PRC Survival. C6.6 (p. 175) gives ordnance Case O for it. C10.3 (p. 181) was read for the push: it says nothing of how long the state lasts.

**What the code did:** the fact was read only in the MPh, from the stack then moving with a pushed Gun, and the Fire package put an attack that carried it in any other phase outside itself.

**The change.**

1. **The state keeps who pushed** (`GameState.HazardousMovers`, set by `ScenarioA1MovementCalculator.HazardousMovers` from a step that pushes a Gun, and emptied as a new Player Turn begins).
2. **`ScenarioA1FireMapRules.HazardousMovement` reads that list in any phase:** every target is a unit on it and none is broken. The pinned are left out by the package, as before, target by target.
3. **The Fire package no longer refuses the fact outside the MPh.** The -2 stands in place of FFNAM and FFMO where those would apply, and beside every TEM.
4. **Ordnance:** in the MPh Case O comes from the same read as before; outside it a new fact of the shot, `HazardousTarget`, gives Case O.

**Ruling R40.10 (question F9, answered yes on 2026-10-11).** Hazardous Movement holds from the push until the unit is pinned or broken, or the Player Turn ends, in every fire phase between. The page gives the pin alone; the break and the turn's end are the ruling's, since a broken crew is no longer "so engaged" and nothing on the page carries the act into the next Player Turn. Only pushing a Gun exists in the game; each other act arrives with its own pass and joins the same list.

**The games,** on board 3, written by `make_hazard.py`. Both are **recorded**; the lines are from the game files.

| Game | What it holds |
|---|---|
| `p40-hazard` | A Russian crew pushes its AT Gun from [D10] to [D9] in its MPh ("for 2 MF, pushing gun R1: a Manhandling DR +2 against M11"). The German squad in [F10] passes; in the DFPh its fire at [D9] has the DRM gunshield +2 and Hazardous Movement -2. |
| `p40-hazard-control` | The Gun is not pushed. The same squad's DFPh fire at [D10] has the Emplacement's +2 and no Hazardous Movement. |

Seen in passing and not part of this task: the control's record names the Gun "r gun" where the other names it "gun R1" (the Emplacement's DRM label against the gunshield's).

**Tests.**

- **Rules, new,** in `ScenarioA1Pass10Tests`: `HazardousMovementIsReadInEveryFirePhaseForTheUnitsThatPushed` (the read by the list, a broken target, the -2 outside the MPh with no refusal, the list after a step).
- **Play, new:** `BacklogPass8Tests.ACrewThatPushedItsGunIsUnderHazardousMovementInTheDfphToo`, with a squad that pushed nothing as its control and the list empty in the next Player Turn. Its first run failed on the control alone: a lone crew with no Gun is a target the Fire package does not review (ruling R24.3); the control is a squad now.
- **Run:** 99 Rules tests in five classes passed. 59 Play tests in three classes (pushing and ordnance, movement, the mortar): 58 passed and the new one failed as told; its class of 19 passed after the test's repair.

**Left out, each for a backlog row.**

- The -2 for a stack in which only some targets pushed: the fact is read for the attack as a whole, as ruling R10.8 had it, so a crew fired on together with a unit that did not push takes none.
- Case O outside the MPh for the Area Target Type.
- The other Hazardous acts (Clearance, Fording, a Set DC, Climbing, sewer movement, descending paratroops, PRC Survival): passes 115 to 175.

**Not seen or not run.** No ordnance shot at the crew outside the MPh was staged and `HazardousTarget` has no test; it rests on reading. A pinned crew in the DFPh was not staged. The replay digest, the sweep, and the text list were not run. No text was added or changed.
