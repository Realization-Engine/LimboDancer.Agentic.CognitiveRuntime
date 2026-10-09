# ASL Unit Backlog Pass 35 Design

**Status:** In preparation and being built at once, at the user's word of 2026-10-09: each increment is designed, built on the branch feature/asl-backlog-pass-35, given a small test game in the Studio and Play tests, and committed at the user's word. Increment 1 (the frame) answered: yes to F1 to F6. Increment 2a (the Rout Phase: 35.17, 35.2, 35.4, with ADJACENT taken from pass 40) built and committed (section 7). Increment 2b (Infantry: 35.1, 35.3, 35.8) built and committed (section 8). **Increment 2c (terrain: 35.5, 35.6, 35.7, 35.9) built, awaiting the user's word to commit (section 9).** Still to come: ordnance, vehicles, night and weather, the two that are not rules, and the cross-cutting sections.

**Date:** 2026-10-09

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 19 (decisions 5 to 7), section 22.1 (c), and section 23; the [rule inventory](<../ASL Rule Inventory A to E.md>), the 45 rows whose pass is 35; the [rule coverage](<../ASL Rule Coverage.md>), section 8.3; the [pass 32 design](<ASL Unit Backlog Pass 32 Design.md>), sections 12 and 24; the [backlog](<../ASL Unit Backlog.md>), sections 51 to 54; the [week review](<../ASL Week Review 2026-09-28 to 2026-10-04.md>); the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>).

**The rulebook.** Increment 1 states no rule. Every rule number in it is the inventory's or the plan's citation, used to name a row. The PDF is read task by task from increment 2 on, and each rule is then cited to its page.

**What increment 1 rests on.** The inventory and the coverage document's fault list were made on 2026-10-05 from `main` at 16fb5f0, before pass 32 moved the rule logic into Rules. The inventory's "Where" column was brought up to date by script after each sub-pass; its statuses and notes were not judged again. Nothing in this increment was read again in the code, and nothing was run. Each task's increment reads the code on `main` as it stands and says where a note no longer holds.

## 1. Outline of this design

| Section | What | Increment |
|---|---|---|
| 1 to 6 | The frame: the outline, what the pass is, the rows by task, what is certain and what needs a run, what the frame found outside the rows, the frame's questions | 1 |
| 7 | The Rout Phase: 35.17 (proposed), 35.2, 35.4 | 2a |
| 8 | Infantry: 35.1, 35.3, 35.8 | 2b |
| 9 | Terrain: 35.5, 35.6, 35.7, 35.9 | 2c |
| 10 | Ordnance: 35.10, 35.11, 35.12 | 2d |
| 11 | Vehicles: 35.13, each correction on its own | 2e |
| 12 | Night and weather: 35.14 | 2f |
| 13 | The two that are not rules: 35.15, 35.16 | 2g |
| 14 | Recorded games and versions | 3 |
| 15 | The proofs, and how a difference is read | 3 |
| 16 | The reviews, the Studio check, the commit order | 3 |
| 17 | The estimate against the plan's 6:25 (6:45 with 35.17) | 3 |
| 18 | The questions together, and the user's answers | 4 |
| 19 | As built | after the build |

Each task's subsection in sections 7 to 13 has the same parts: the rule as read on its page; what the code does today, with file and line; the repair as a Rules verdict and the facts it takes; the refusal texts added or changed; the games and proofs expected to change; the tests; the questions, each with a recommendation.

## 2. What the pass is

The plan's section 22.1 (c): sixteen tasks, 45 rows of the inventory, places where the game gives a result the rule forbids, or refuses with a false reason, or leaves part of a built rule out in silence. A seventeenth task, 35.17, is proposed and not yet in the plan (section 5.1). The pass comes first because passes 40, 50, 80, 85, 90, 110, 115, 125, and 135 read what it repairs.

Two things set it apart from every pass before it:

1. **Every repair is written in Rules** (the plan's section 19, decision 7). A repair is a calculator's verdict over facts; Play and the projector hand the facts over. A repair that needs a fact Play does not yet read (Height Advantage along a rout's LOS, the scenario month for an orchard) adds the read in Play and the decision in Rules.
2. **It changes results on purpose.** Pass 32's proofs had to come out equal. Here the replay digest, the planner sweep, and the text list will differ, and each difference must be named as one task's intended change or as a fault. Section 15 will say how; each task's subsection says what it expects to change.

## 3. The 45 rows, by task

Statuses and notes are the inventory's, shortened. "By" is who judged the row on 2026-10-05: "me" means the rule's text in the PDF and the code were both read by Claude that day; "audit" means one of the 19 read-only audits, taken as reported. The task estimates are the plan's.

### 3.1 The Rout Phase group (increment 2a)

| Task | Row | Page | Status | By | What is wrong today |
|---|---|---:|---|---|---|
| 35.17 (proposed) | none yet: the rules are A10.5 (p. 66, pass 70's row) and A20.21 (p. 86, pass 75's row) | | | code read 2026-10-09; A20.21 not read in the PDF | A unit bound to surrender as the RtPh ends still owes a rout in the rout order, so the DEFENDER's units wait for it for ever and neither side can end the phase. The panel says it "must rout" and offers routes the gate refuses. Seen in the user's game `cd-guards-5`. |
| 35.2 | A19.12 Disruption | 86 | partly | me | A Disrupted unit does not stay put: it may rout and Low Crawl like any broken unit when no captor is ADJACENT. Also missing in the row, and to be placed in increment 2a: surrender in other phases (backlog, R14.11); the SS and PRC exceptions. |
| 35.4 | A10.531 Open Ground | 67 | built with a deviation | audit | Ruling R13.3 reads Open Ground from the hex's terrain and a LOS with no map Hindrance, not from the FFMO a given enemy could apply: no TEM, no Height Advantage. |
| 35.4 | A10.533 Concealment | 68 | partly | audit | A rout step into a Location holding only concealed or hidden enemy units is accepted: no reveal, no repulse, no elimination. A concealed unit cannot drop its "?" to Interdict or bar a route. |
| 35.4 | B1.14 Hills | 113 | partly | me | Interdiction ignores Height Advantage: a hill hex seen from below is Interdicted. |
| 35.4 | B1.16 Hexsides | 113 | partly | me | Interdiction across a wall or hedge hexside is not prevented. |
| 35.4 | B1.17 Artificial Terrain | 113 | partly | audit | For Interdiction only SMOKE in the hex and the map's Hindrance count; a wreck or AFV in the hex, and SMOKE or a vehicle along the LOS, do not. |

The task's text also names "a lone SMC with a MG does not Interdict". That is A10.532 (p. 67), whose row the inventory gives to pass 70. Section 5.2 proposes where it goes.

### 3.2 Infantry (increment 2b)

| Task | Row | Page | Status | By | What is wrong today |
|---|---|---:|---|---|---|
| 35.1 | A17.11 Severity | 85 | partly | me | The Wound Severity dr is skipped in three paths, where a SMC is wounded with no dr and an already wounded SMC is eliminated outright: a rout's Interdiction, the Mopping Up casualty (the same helper), and a PF firer's Casualty Reduction. |
| 35.3 | A.7 Good Order | 43 | partly | audit | No single definition. The planner's treats a TI unit as not Good Order and a berserk unit as Good Order, both against the rule; stunned and shocked crews and the Good Order SW sense are in none. The pass 32 design's section 12 lists three definitions and three scans that differ. |
| 35.3 | A.18 Morale Level Ceiling | 44 | built with a deviation | audit | No general ceiling of 10. A berserk Fanatic unit is given 11 (the code names ruling R30.3); a Fanatic unit with a printed 10 would be 11. Leader Creation's Morale Level does the same. |
| 35.8 | A25.2 Russian | 93 | partly | me | A Russian 4-2-6 Battle Hardens to the NKVD 6-2-8, not the 5-2-7 the rule names. Two tests assert the wrong result. The row's other gaps (Guards Depletion, the Deploy exceptions) are not this task's. |

### 3.3 Terrain (increment 2c)

| Task | Row | Page | Status | By | What is wrong today |
|---|---|---:|---|---|---|
| 35.5 | B.2 COT | 112 | partly | audit | SMOKE's MF is added after the uphill doubling: 3 MF where the rule's own example gives 4. |
| 35.5 | B.10 LOS Hindrance Blockage | 113 | partly | audit | Map Hindrance of six blocks; Low Visibility plus other Hindrance of six refuses. Terrain plus SMOKE plus vehicle Hindrance reaching six with no Low Visibility is never tested, and the attack is made at +6. |
| 35.6 | B14.2 Seasons | 129 | partly | audit | Nothing ties the orchard's season to the scenario month, so every orchard is in season; some out-of-season LOS cases are refused; fire leaves any orchard Hindrance undecided. |
| 35.6 | B14.4 (orchard movement) | 129 | partly | me | Vehicles are refused in an orchard with a false reason, where the rule gives the Open Ground cost. |
| 35.6 | B15.2 (grain) | 129 | partly | audit | A LOS through grain is refused when the month is out of season, where the rule makes it Open Ground. (It is also refused when the game has no month; whether that stays is for increment 2c.) |
| 35.7 | B16.31 (HE into marsh) | 130 | not built | audit | Ordnance HE into marsh keeps its full FP. A recorded leave-out (ruling R10.1), and a wrong result. |
| 35.7 | B16.43 Bog | 130 | not built | audit | A vehicle entering a hex adjacent to marsh takes no Bog Check. |
| 35.7 | C3.53 (halved HE) | 170 | partly | audit | The same marsh halving seen from Chapter C; with the Area Target Type it should be halved twice. Fording is not built and not this task's. |
| 35.9 | B25.14 Wreck Blaze | 143 | partly | audit | A wreck burned in Close Combat is recorded as burning but gets no Blaze entity, so it gives wreck cover and no smoke. |

### 3.4 Ordnance (increment 2d)

| Task | Row | Page | Status | By | What is wrong today |
|---|---|---:|---|---|---|
| 35.10 | C5.1 Case A | 171 | partly | audit | Not doubled when the firer is in rubble. (The row's other gaps, 360-degree mounts and bow-mounted Guns, are not this task's.) |
| 35.10 | C5.11 (CA change in woods or building) | 172 | partly | audit | The doubling and the CA lock are built for woods and buildings, not rubble. |
| 35.10 | C5.2 Case B | 172 | partly | audit | +3 in rubble is missing; an Opportunity Firer's Gun still takes Case B and fires once. |
| 35.10 | C5.34 Case C3 | 172 | partly | audit | Rubble as a Backblast Location is missing; so is the Opportunity Fire exemption. |
| 35.10 | C5.5 Case E | 172 | partly | audit | In-hex wreck, SMOKE, and AFV Hindrance are set to 0; no doubling in rubble. |
| 35.10 | C6.3 Case L | 174 | partly | audit | The ATR is denied Case L, though the rule exempts only non-ATR LATW. |
| 35.10 | C6.6 Case O | 175 | not built | audit | A Gun firing at a crew pushing a Gun takes Cases J3 and J4 in place of Case O. |
| 35.10 | C13.1 LATW | 183 | partly | audit | An Opportunity Firer's LATW shot in the AFPh still takes +2. |
| 35.10 | C13.8 Backblast | 185 | partly | audit | A PF or PSK fired from rubble takes no Case C3, and a pinned firer there is not refused. |
| 35.11 | C6.5 Case N | 174 | partly | audit | Acquisition is kept when the Gun turns without firing, is pushed or towed, when a tank moves, and when a light mortar is carried away. |
| 35.11 | C6.51 (Acquisition follows) | 174 | partly | audit | Built for a Gun at Infantry. A vehicle target and a tank's Acquisition are not followed: the counter stays on the Location, and a unit entering inherits it. |
| 35.12 | C10.1 Towing | 180 | partly | audit | A vehicle towing a Gun may use Bypass and may cross a hedge. |

### 3.5 Vehicles (increment 2e)

Task 35.13 is one row of the plan and ten rows of the inventory. The prompt asks for each correction on its own.

| # | Row | Page | Status | By | What is wrong today |
|---|---|---:|---|---|---|
| a | D2.16 Road Rate | 196 | partly | me | The doubling for BU reads the recorded condition only. A CT AFV that is BU by default pays half an MP a road hexside. The same read is in the Recall route cost, the night cost, and the answer "already CE" to a default-BU tank. |
| b | D2.38 TEM | 198 | partly | audit | Ordnance on the Vehicle Target Type gives a Bypassing vehicle the obstacle hex's Case Q TEM. |
| c | D1.321 RST | 194 | partly | audit | A CE RST tank still adds its MA base and CMG to an OVR and its CMG to CC. |
| d | D5.34 Stun | 203 | partly | audit | The +1 is missing on an OVR DR and on the vehicle's CC DR, and a crew that bails out or survives loses its +1. |
| e | D7.21 CC Reaction Fire | 207 | partly | audit | The CC counter is set and never read, so it does not bar Non-CC Reaction Fire. |
| f | D8.5 (bogged vehicle) | 209 | partly | audit | A bogged vehicle is refused every expenditure but Bog Removal, so it cannot unload. |
| g | D5.341 Recall | 203 | partly | audit | A bogged Recalled AFV is not Abandoned; ESB is not barred to a Recalled AFV. |
| h | D9.4 AFV and wreck Hindrance | 210 | partly | audit | A Bypassing AFV always hinders, though the rule gives none when the LOS does not touch the Bypassed hexside. |
| i | D5.33 CE Movement | 203 | partly | audit | No bar on a CE or BU change after the vehicle's own Bounding First Fire in that MPh. The task's text does not name this one. |
| j | D8.4 Target Status | 209 | partly | audit | A Defensive First Fire shot at a vehicle spending Bog Removal MP takes a Case J DRM. The task's text does not name this one, and the audit calls it probable. |

The task's text names one correction with no row among the 45: the ESB table by nationality (D2.5, p. 198, whose row is "built"). The audit calls it latent: right for German and Russian vehicles, which are all the catalog holds. Section 5.2 proposes what to do with it.

### 3.6 Night and weather (increment 2f)

| Task | Row | Page | Status | By | What is wrong today |
|---|---|---:|---|---|---|
| 35.14 | E1.52 Vehicular | 224 | partly | audit | The +1 MP at night is not charged in VBM. |
| 35.14 | E3.64 Movement | 230 | partly | audit | Infantry on an unpaved road in Mud, in a hex whose other terrain is not Open Ground, pay no half MF; Infantry Bypass and VBM add nothing. |
| 35.14 | E3.65 Open Ground | 230 | partly | audit | Only the open-ground key counts, plus an unpaved road for vehicles. (Gullies, dry streams, shellholes, and the rest wait for the terrain passes.) |
| 35.14 | E3.9 MF/MP Cost Additions | 231 | partly | audit | Weather costs are not added per hexside Bypassed. Whether a reversing vehicle should multiply the weather cost is unsettled. |

### 3.7 The two that are not rules (increment 2g)

| Task | Rows | What it is |
|---|---|---|
| 35.15 | none | A refusal for any game that places a fortification counter, until passes 115 and 120 land. Today such a game plays as if the counters were absent, with no message. The coverage document says the same of `asl:fortified-location`, `asl:rubble`, and `asl:flame`; increment 2g says which counters the refusal names. |
| 35.16 | none | From the week review: the seven tests not written (three of pass 31c: a tag through Deployment and Recombination, the Melee mark by view, a kept rate of fire; four page tests of pass 31d: the setup map's tooltips, "Since you last looked" after the view acts, a blocked LOS confirmed and its record, the Close Combat due list where the package refuses the Ambush), and a sweep test of Play for what a side may not read. |

### 3.8 The count

| Task | Rows | Estimate |
|---|---:|---|
| 35.1 | 1 | 0:20 |
| 35.2 | 1 | 0:20 |
| 35.3 | 2 | 0:20 |
| 35.4 | 5 | 0:40 |
| 35.5 | 2 | 0:10 |
| 35.6 | 3 | 0:20 |
| 35.7 | 3 | 0:20 |
| 35.8 | 1 | 0:05 |
| 35.9 | 1 | 0:05 |
| 35.10 | 9 | 0:40 |
| 35.11 | 2 | 0:40 |
| 35.12 | 1 | 0:05 |
| 35.13 | 10 | 1:00 |
| 35.14 | 4 | 0:20 |
| 35.15 | 0 | 0:20 |
| 35.16 | 0 | 0:40 |
| **Sixteen tasks** | **45** | **6:25** |
| 35.17 (proposed) | 0 | 0:20 |

Every one of the 45 rows sits under exactly one task, and the rule columns of the plan's sixteen tasks cite exactly those 45 rules. Checked by listing the rows from the inventory by script and placing each by hand against the plan's citations.

## 4. What is certain, and what needs a run

Three grades. None of them is a fresh reading: they say how far the 2026-10-05 judgement can be trusted before the task's increment reads the code again.

**A. Read by Claude in the PDF and in the code on 2026-10-05.** The fault is as stated unless pass 32's move changed the member.

- 35.1 (A17.11), 35.2 (A19.12), 35.8 (A25.2), 35.4's B1.14 and B1.16, 35.6's B14.4, 35.13 a (D2.16).
- 35.17: the code was read on 2026-10-09 by the session that found it (`GamePlanner.Rout.cs` lines 219, 298, and 310, which are still at those lines on `main`). A20.21 was not read in the PDF. When the surrender happens is open until the page is read.

**B. An audit's reading of the code, stated without reservation.** Likely as stated; the increment confirms each in the code, and the rule on its page.

- 35.3 (A.7, A.18), 35.4's A10.531, A10.533, and B1.17, 35.5 (B.2, B.10), 35.6's B14.2 and B15.2, 35.7's B16.31 and B16.43, 35.9 (B25.14), 35.10's rubble cases (C5.1, C5.11, C5.2, C5.5, C13.8), C6.3, and C6.6, 35.11 (C6.5, C6.51), 35.12 (C10.1), 35.13 b to i, 35.14's E1.52, E3.64, and the Bypass part of E3.9.

**C. Marked by its audit as read and not run, probable, or unsettled; or needing a fixture the tests do not have.** Each needs a run or a board read before the repair is designed, and the increment says which.

| Task | Item | Why it is not certain | What settles it |
|---|---|---|---|
| 35.10 | The Opportunity Fire exemptions (C5.2, C5.34, C13.1) | "Read from the code, not run" | A planner test: an Opportunity Firer's Gun and LATW in the AFPh |
| 35.7 | HE into marsh (C3.53, B16.31) | "Read in code, not run" | A Rules test of the hit attack with a marsh target |
| 35.7 | The Bog Check beside marsh (B16.43) | Only board 01 has real terrain in tests; whether it has a marsh hex is not known | A board read; a fixture hex if not |
| 35.13 j | Case J against Bog Removal (D8.4) | "Probable"; no test found | A planner test of the shot |
| 35.14 | A reversing vehicle and the weather cost (E3.9) | "Unsettled": the audit could not tell from D2 whether the reverse multiple covers it | The PDF, D2 and E3.9 read together |
| 35.6 | Orchard out of season (B14.2) | The LOS data and the refused cases live in Maps; no reader of the month exists there | A LOS fixture across an orchard to a higher level, in and out of season |
| 35.5 | A Hindrance total of six from mixed sources (B.10) | No test found for the six-total block | A fire test with terrain, SMOKE, and a vehicle summing to six |
| 35.4 | A rout into concealed enemy units (A10.533) | No test found | A planner test; the rule on its page for repulse against elimination |
| 35.3 | The Morale ceiling (A.18) against ruling R30.3 | The code cites a ruling for the 11 | Ruling R30.3 read beside the PDF: the ruling may need to be withdrawn in part |
| 35.17 | The surrender's timing (A20.21) | Not read in the PDF | The page; then a cut of `cd-guards-5` as the fixture |

**Rulings the repairs touch.** Four tasks change something a ruling records, so each needs the user's word on the ruling as well as on the code: R13.3 (35.4, Open Ground for Interdiction), R30.3 (35.3, the berserk Fanatic's 11), R10.1 (35.7, HE into marsh at full FP), and whatever ruling 35.17 writes or amends for the surrender's timing (R13.3 again, which takes the surrender "as the RtPh ends"; R31.6 for the hand-over). Each increment quotes the ruling and proposes its new text.

**Tests that assert the wrong result today.** Known from the inventory: 35.8 (`ScenarioA1FireExtensionTests`, two places). Each increment lists the others it finds; a test changed by a repair is named in the task's commit.

## 5. What the frame found outside the 45 rows

The plan says the task list may change in the design, and the prompt asks that a fault outside the rows be proposed for this pass or for the pass that reworks its rule. These came out of the reading. Each has a first placement; the group's increment confirms it against the code and sizes it.

### 5.1 The proposed task 35.17

As the prompt gives it: "35.17 A unit that surrenders instead of routing (A20.21) no longer holds up the rout order: the DEFENDER's units do not wait for it, the phase can end, and the Rout panel says it surrenders and offers it no routes. Rules: A10.5 (p. 67), A20.21. 0:20." The surrender verdict becomes a fact `AttackerMustRoutFirst` takes, in Rules. It is the first task designed (increment 2a) because the user's game waits on it.

If the user accepts it, the plan's figures become: pass 35 build 6:45, total 8:00, likely 4:00; block 1 build 17:45, total 21:30, likely 10:45; the twenty new passes build 109:35, total 134:35, likely 67:18; the 31 rule passes build 157:20, total 196:05, likely 98:02; the whole plan 419:59. A commit of its own, with the inventory's count line for the pass unchanged (the task takes no row; A10.5 and A20.21 keep their passes and gain a note).

### 5.2 Three places where the task list and the rows disagree

| # | What | Proposal |
|---|---|---|
| 1 | 35.4 names "a lone SMC with a MG does not Interdict" (A10.532), a row the inventory gives to pass 70. | Keep the clause in 35.4, as the plan's text has it: it is one test in the function the task rewrites. Note it in the A10.532 row; the row stays pass 70's for vehicles and Guns as Interdictors. |
| 2 | 35.13 names the ESB table by nationality (D2.5), a row marked built with no pass. | Do it in 35.13 as the plan's text has it, since it is a table of five values, and mark the row. It is latent, so no game changes. |
| 3 | 35.13's rows hold D5.33 and D8.4, which its text does not name. | Both stay in 35.13 (corrections i and j). 35.13 then has eleven corrections with ESB, against one hour; section 17 will say whether the hour holds. |

### 5.3 The copies pass 32 kept apart "for pass 35"

The pass 32 design's section 12 lists sixteen places where two or three copies of one rule differ. The migration kept each copy as its own function and said pass 35 would settle them; three comments in Rules point here. The plan's sixteen tasks cover only the first. A first placement of each:

| # | The copies (pass 32 design, section 12) | Placement proposed |
|---|---|---|
| 1 | Good Order, three definitions | 35.3: its subject |
| 2 | "A Good Order enemy with a LOS", three scans | 35.3, with item 5.4 (1) below if the user takes it; otherwise pass 70 |
| 3 | Grain in season April to September for a cost, June to September elsewhere | 35.6: the same season read; the page says which months |
| 4 | Overstacking counted four ways | Pass 40 (stacking is its subject) |
| 5 | A TI vehicle moving: the planner bars it, the projector does not | 35.13 if it is one line in the shared calculator; otherwise pass 50 |
| 6 | What a bogged vehicle may do, planner against projector | 35.13 f: the same member |
| 7 | The rout MF of a wounded SMC: any SMC in the planner, a leader or hero in the projector | Increment 2a, beside 35.2 |
| 8 | The CE test of a vehicle MG repair; the SW repair's checks | Pass 50, which builds per-weapon repair (D3.7) |
| 9 | Inexperience decided twice | Pass 45 (the rows of A19.3 and the B# for Inexperienced use) |
| 10 | Cowering's exemptions in "could cause a NMC" against the calculator's | Pass 45 (A7.9 is its row) |
| 11 | Hindrances valued twice: fire recounts by terrain and season, the rout uses VASL's total | 35.4: Interdiction reads what fire reads |
| 12 | The effects of capture, three copies | Pass 75 (prisoners) |
| 13 | Who may guard prisoners, five tests | Pass 75 |
| 14 | "Stunned" grouping Recalled, Shocked, and Unconfirmed Kill three ways | 35.13 d if the Stun +1 repair reads it; otherwise pass 65 |
| 15 | The road rate's copy in Recall lacks the snow clause; the Recall exit cost adds no night or weather MP | 35.13 a (the same BU read) and 35.14 |
| 16 | A failed creation run twice, its diagnostic written twice | Not a rule; 35.16 if it is a one-line fix, otherwise the backlog |

That is nine of the sixteen in this pass (1, 2, 3, 5, 6, 7, 11, 14, 15), with 16 as a maybe, and seven named for a later pass with a backlog row each. The nine are not in the plan's 6:25. Section 17 will size them once the increments have read them; a first guess from the plan's task sizes is five tiny and four small, about 1:45.

### 5.4 Items the backlog and the week review point at this pass

| # | Item | Where it is recorded | Proposal |
|---|---|---|---|
| 1 | Movement's read of who sees a concealed mover or a Dummy stack ("Good Order, not just unbroken", and a hidden viewer) | Backlog section 51: "the user, 2026-10-04: in the next rules pass". The week review put it in the short rules pass. Section 22.1 then gave A12.14's row to pass 70. | Take it in 35.3. It is a wrong result in play today, the user agreed it for the next rules pass, and it is one of the three scans of 5.3 (2), which 35.3 settles anyway. Small, 0:20. |
| 2 | A voluntary rout by a broken unit that need not rout and is not under DM | Backlog section 52, raised in `cd-guards-5`, waiting on "the referee's reading of A10.5 with the PDF" | Read A10.5 for it in increment 2a, since the page is open for 35.17 and 35.2. Build it there if the rule allows the rout; otherwise write the ruling that it does not. |
| 3 | Interdiction ignores Fanaticism (A10.8, A10.53) | Coverage 8.3; not in the 45 rows | Read it in increment 2a beside 35.4 and take it if it is one term in the function 35.4 rewrites. |
| 4 | Commissars surrender in the RtPh (A20.21) | Coverage 8.3, "not confirmed by a run"; the A20.21 row is pass 75's | Read it in increment 2a: 35.17 makes the surrender verdict a Rules fact, and the exemption is one more input to that verdict. |
| 5 | `GameProjector.CheckInvariants`: "pass 35 or pass 70 deciding whether these are rules or plumbing" | Backlog section 53 | Leave it to pass 70. It gives no wrong result; nothing here reads it. |
| 6 | A board 4 fixture, so the plans of Gambit and Armor Test are tested on real terrain; strict JSInterop in the Play tests; page tests of entry by advance | The week review's "Tests not written", beyond the seven | Not in 35.16 as the plan wrote it. Leave them in the backlog unless a repair needs board 4 (35.7's marsh, 35.6's orchard); increment 2g decides with the fixtures known. |
| 7 | The Armor Test play through the Play page before this pass | The plan's 22.1 (h), question 5, recommended and, as far as the documents show, not answered | Not before the design. Play it as the table player's review of the vehicle group, on the pass's build, where it tests the repairs and not the faults. |

## 6. The frame's questions

Each carries its proposal; the answer asked for is a veto or a change. They are carried into section 18 with the questions of the later increments.

| # | Question | Proposal |
|---|---|---|
| F1 | Is the order of the increments right: the Rout Phase group first, then Infantry, terrain, ordnance, vehicles, night and weather, the two that are not rules, then the cross-cutting sections and the questions? | Yes, as the prompt has it. |
| F2 | Is 35.17 accepted as a task of this pass, and written into the plan now as a commit of its own? | Yes. The figures are in 5.1. |
| F3 | Do the three disagreements of 5.2 go as proposed (the lone SMC with a MG in 35.4; the ESB table in 35.13; D5.33 and D8.4 in 35.13)? | Yes. |
| F4 | Of the sixteen differing copies of 5.3, does this pass take the nine proposed and leave seven to passes 40, 45, 50, and 75 with backlog rows? | Yes, each confirmed in its group's increment before it is fixed. |
| F5 | Does movement's read of who sees a concealed mover (5.4, item 1) come into 35.3, as you said on 2026-10-04, though section 22.1 gave its row to pass 70? | Yes, into 35.3. |
| F6 | Are items 2 to 4 of 5.4 read in increment 2a and decided there, and items 5 to 7 left as proposed? | Yes. |

## 7. The Rout Phase: 35.17, 35.2, 35.4 (increment 2a)

Written 2026-10-09. The user's word for this increment was to build as it goes and to bring the saved copy of `cd-guards-5` back for testing in the Studio. Tasks 35.17 and 35.2 are built; task 35.4 is read on its pages (7.4) and waits for its code reading.

### 7.1 The rules, as read on their pages

Read in the registered PDF, pages 66 to 68 and 86 to 87. Paraphrased; a quotation is short and marked.

- **A10.5 (p. 66).** In the RtPh a broken unit not in Melee may not stay in an Open Ground hex in the Normal Range and LOS of a Known enemy unit, nor end the RtPh ADJACENT to or in the Location of a Known enemy unit that is unbroken and armed (not at night). Such units must rout away, the ATTACKER's first, one at a time, or be eliminated for Failure to Rout, with the exception of surrender (A20.21). A broken unit may rout if it is under DM. Six MF, a wounded SMC fewer.
- **A10.5's second example (p. 67).** A unit that cannot rout without moving ADJACENT to a Known enemy unit "must surrender (20.21) or be eliminated at the end of the RtPh" under No Quarter. The elimination is placed at the phase's end. The surrender is not given a moment of its own.
- **A20.21 (pp. 86 to 87).** A broken Infantry unit, in its RtPh, that is ADJACENT to Known, Good Order, armed enemy Infantry or Cavalry, and that cannot rout away, or can only by risking Interdiction or by Low Crawl, surrenders to that unit instead, whatever way it could in fact rout. A stack surrenders together and is accepted or rejected as a stack. If the only ADJACENT armed enemy is in Melee, berserk, or a vehicle, the unit must rout away, even if Disrupted, or be eliminated. A unit that is also Disrupted, Encircled, or surrendering by Heat of Battle surrenders even with a clear rout path. Partisans, Gurkhas, Commissars, SS facing Russians, Fanatics, Japanese, and units facing No Quarter never surrender this way: they Low Crawl or risk Interdiction, and are eliminated if they can do neither.
- **A19.12 (p. 86).** A Disrupted unit surrenders at the start of any RtPh it begins ADJACENT to a Good Order armed Known enemy Personnel unit not in Melee, and in any phase it shares a Location with one (not under No Quarter). Disrupted Infantry do not rout unless in a Blaze, in Open Ground as A10.531 defines it, in a Water Obstacle, or when the only armed enemy units ADJACENT are in Melee, berserk, or vehicles; and they may not use Low Crawl, but at night.
- **A10.531 (p. 67), A10.532 (pp. 67 to 68), A10.533 (p. 68).** For 35.4; summarized in 7.4.

### 7.2 Task 35.17: a unit that surrenders instead of routing holds nothing up

**What the code did.** Three gates disagreed about one unit.

| Gate | Where | What it asked | Its answer for a unit bound to surrender |
|---|---|---|---|
| The rout itself | `GamePlanner.Rout.cs`, `PlanRout`, the A20.21 block | The surrender verdict: a candidate, with captors, and a cause | Refused: it surrenders as the RtPh ends |
| The rout order | `PlanRout`, the `AttackerMustRoutFirst` call | Not routed, not pinned, must rout, has a legal step | It must rout first, so the DEFENDER's units wait |
| The phase's end | `GamePlanner.Proposer.cs`, `ProposerBar`, with `RoutPhaseEndBar` | Any unit of the other side on the Failure to Rout list | The other side must be handed the screen first |

The first gate was right by A20.21. The second and third never asked the first gate's question. In `cd-guards-5` the two German squads in [H5] were bound to surrender; the Russian units in [G5] waited for them (gate two); and neither side could end the phase, each being told the other still had a unit to rout (gate three, which fires for any unit on the list, not only one that can still act).

**The repair, in Rules.** One verdict, `ScenarioA1RoutCalculator.RoutStillOwed`: a broken unit still owes a rout when it has not routed, is not pinned, must rout, has a legal step, and does not surrender instead. The last three are lazy facts, read in that order.

- `AttackerMustRoutFirst` is the ATTACKER's case of it, and takes the surrender as a sixth fact.
- The phase's end asks it of each unit on the other side's Failure to Rout list (`GamePlanner.RoutStillOwed`), so only a unit that can still act keeps the phase open. This also ends a second deadlock the same gate held: each side with a unit that has no legal step at all.
- Play gathers the surrender's facts in one place, `GamePlanner.RoutSurrender` (the candidate test, the captors, the cause, the trap read last), which the rout's own gate now calls too. The three gates can no longer disagree.
- The panel: `GamePlanner.RoutSurrenderAdvice` with the Rules text `RoutSurrenderAdvice`. A unit bound to surrender is listed as "does not rout: it ..., so it surrenders to ... as the RtPh ends (A20.21)", is given no routes, and is not among the units the rout may be proposed for.

**Texts.** One added (the panel's sentence). None changed: `play.rout-surrender`, `play.rout-order`, and the hand-over refusal keep their words.

**What is not changed: when the surrender happens.** Ruling R13.3 takes it as the RtPh ends, unit by unit. The page does not fix the moment: A20.21 says the unit surrenders "instead" of routing, A19.12 puts a Disrupted unit's surrender at the RtPh's start, and the example places only the elimination at the phase's end. Question 7.5 (1).

**Checked in the Studio** (port 6671, from `bin/p31b`, on `cd-guards-5-rtph`, a copy of the user's game at its 729 events):

- German view: 4-4-7 squad G7 and 4-6-7 squad G9 in [H5] each read "does not rout ... so it surrenders ... as the RtPh ends (A20.21)", with no routes, and neither is offered for a rout. The three German units under DM are offered as before.
- Russian view: the four broken units in [G5] each rout to [G4] ("Every check passed"; before the fix, "The ATTACKER's 4-6-7 squad G13 must rout first").
- The Russian side ends the phase: both German squads surrender, each captor choice is put to the Russian side, both are taken prisoner, the phase ends, and the game stands in the German Advance Phase at 743 events.
- The played copy is kept at `E:\Archive\GitHub\dlandi\pass32-tools\pass35\cd-guards-5-rtph.played-after-fix.game.json`; the test game was then reset to the 729-event cut for the user's own test. The user's game `cd-guards-5` was not touched (its file equals the saved copy byte for byte).

**Expected differences in the proofs.** The replay digest: none, since no recorded event changes meaning. The planner sweep: a DEFENDER's rout refused for `play.rout-order` becomes ready, and an end of the RtPh refused for the hand-over becomes ready, wherever a cut holds a unit bound to surrender or one with no legal step. The text list: one literal added.

**Tests, at the gate.** Rules: `RoutStillOwed` over its five facts and its laziness; `AttackerMustRoutFirst` with the sixth fact (the existing call is updated). Play: a cut of `cd-guards-5` at revision 729 as a fixture, asserting the DEFENDER's rout is ready, the end of the phase is ready for either side, and the surrender follows. Page: the obligation's sentence and the absence of routes.

### 7.3 Task 35.2: Disrupted units stay put

**What the code did.** `MayRout` let any broken unit under DM rout, Disrupted or not, and nothing barred a Disrupted unit's Low Crawl. A Disrupted unit ADJACENT to its captors already surrendered (the cause "is Disrupted").

**The repair, in Rules.**

- `ScenarioA1RoutCalculator.MayRout` takes the Disrupted condition: a Disrupted unit routs only when it must. "Must" is already the rule's list as far as the game has it: Open Ground in an enemy's LOS and Normal Range, or a Known unbroken armed enemy unit ADJACENT. When that enemy can take its surrender it surrenders (35.17's verdict); when it cannot (in Melee, berserk, a vehicle, or under No Quarter) the unit must rout, as A20.21 says.
- `DisruptedLowCrawlBar`: "play.rout-low-crawl: {unit} is Disrupted and may not use Low Crawl (A19.12)", not at night.

**Not in this task.** A Blaze and a Water Obstacle as reasons to rout (neither is built for any unit); surrender in other phases when an enemy shares the Location (backlog, ruling R14.11, pass 75); the surrender at the RtPh's start (question 7.5 (1)).

**Not checked in the Studio.** `cd-guards-5` holds no Disrupted unit in this phase. A fixture is needed; the test is at the gate, and a Studio check wants a game with a Disrupted unit under DM out of enemy sight.

**Expected differences.** The sweep: a rout by a Disrupted unit under DM that need not rout becomes refused (`play.rout-not-allowed`), and a Disrupted unit's Low Crawl is refused. A recorded game in which a Disrupted unit made such a rout would be refused at that event only if the projector checks the obligation. I have not read the projector's `Rout` for this; the replay digest will say, and section 14 settles how such a record is treated.

### 7.4 Task 35.4: what the pages say, before the code is read

- **A10.531.** Open Ground, for rout, Dash, concealment, and Interdiction, is a hex in which the particular enemy unit could apply the FFMO DRM in a hypothetical Defensive First Fire shot. The example: grain, brush, and orchard are not Open Ground; SMOKE or a LOS Hindrance, in the hex or between it and every possible Interdictor, frees the hex; so does any TEM the routing unit can claim; an Entrenched unit, or a crew or HS manning an Emplaced Gun, is not in Open Ground for Failure to Rout.
- **A10.532.** No weapon Interdicts beyond its Normal Range or 16 hexes. A CX or Encircled unit, one in Melee, one using Spotted Fire, a leader without a SW, and any unit whose FP is halved (mortars excepted) may not Interdict. That last clause is the lone SMC with a MG.
- **A10.533.** Concealed units are ignored in the route. On entering a concealed unit's Location, one concealed non-Dummy unit there loses its "?" by Random Selection and repulses the routing unit to the hex it came from, where it ends its RtPh, and is eliminated if that leaves it ADJACENT to a Known enemy unit. A concealed unit may also give up its "?" in the routing unit's LOS to Interdict or to change the route.
- **A10.53.** Interdiction needs an unbroken enemy unit able to fire on the hex with at least one FP and with no LOS Hindrance of any kind.

So the frame's reading holds: Interdiction and the must-rout test are to read the same cover fire reads (Height Advantage, wall and hedge TEM, a wreck or AFV in the hex, SMOKE and vehicle Hindrance along the LOS), which replaces the terrain-name test of ruling R13.3. The code reading, the facts each verdict takes, and the build are the rest of this increment.

**The Open Ground part, built 2026-10-09 (uncommitted).** One Rules verdict, `ScenarioA1RoutCalculator.CouldApplyFfmo`: a clear LOS within range, no Hindrance of any kind along it, and no TEM the unit could claim against that enemy. Both the must-rout test (`ExposedInOpenGround`) and `Interdictor` ask it. Its facts come through a new read of the rout's fact reader, `IRoutFactReader.Cover`, as a `RoutCoverFacts`: the map Hindrance as fire counts it by terrain and season (the pass 32 design's section 12, item 11: where fire can attribute every Hindrance on the LOS its count stands, otherwise the map's own total does, as before); the Hindrance of vehicles, wrecks, and SMOKE along the LOS; the wall or hedge TEM of the hexside crossed; Height Advantage; and a wreck's or AFV's cover in the Location. Play gathers them in `GamePlanner.InterdictionCover` with the members fire uses (`ScenarioA1FireMapRules.LocationLos`, `VehicleHindrance`, `HexsideTemAt`, `HeightAdvantageAt`, `CoverAt`), read only for a clear LOS in range and kept for the scan. A wall whose TEM fire refuses to decide (a Hillside wall) gives none here, as before. The lone SMC: `InterdictionRange`, a SMC with no other unbroken SMC of his side in his Location has his own range alone, which for a leader is none; the must-rout test still counts his SW's range (A10.5 names the SW itself).

**Tests run.** Rules, by filter: 71 of 71, of them five new (the five kinds of cover each alone, fire's count against the map's total, the cover read only for a clear LOS in range, the lone SMC, and the verdicts of 35.17 and 35.2). Play, by filter over passes 13, 15, 31 to 31d and the rout tests: 97 of 97, unchanged.

**The test game `p35-rout`,** made at the user's suggestion of a small scenario for the rule: board 3, German Rout Phase, three broken Russian squads under DM, each two hexes from one German squad with a clear LOS and no Hindrance (checked in the board viewer). On [E5], a level 1 hill hex seen from the woods of [E7] at level 0, and on [O3], behind the wall on its South hexside from the stone building [O5], the squad reads "may rout, since it is under DM": it need not rout. On [P10], plain Open Ground seen from [P8], it reads "must rout: it is in Open Ground in the LOS and Normal Range of" the German squad. Before the repair the old test (the terrain's name, a clear LOS, no map Hindrance) made all three must rout; that is from the code as it stood, not from a run of the old build.

**The repulse, built 2026-10-09 (uncommitted).** Rules: `RoutRepulse` over the enemy units of the entered Location that the routing side does not know (concealed, hidden, or Dummies), and `RoutRepulseShown`. With a real unit among them the rout is repulsed: the hidden units first go beneath a "?"; one real unit loses its "?", the only one with no dice, among several by Random Selection as an ordinary entry draws (A.9; ruling R10.11), ties all; the routing unit stays in the Location it came from, where its rout ends. With Dummies alone, they are removed and the rout goes in. Play reads those units at each step of the rout as it is built, after the dice are in hand, so the plan a side confirms says nothing of them. The record: `rout-stepped` gains an optional `attempted` Location, as `movement-step` has for a forced-back mover; a repulsed step names the Location the unit stays in and the one it tried, costs its MF, and marks the unit as having routed. The page says "is repulsed from [X] by a concealed unit there and ends its rout in [Y] (A10.533)".

**What ends the repulsed unit's RtPh.** A10.533 says it is eliminated for ending ADJACENT to a Known enemy unit (A12.15). As built, the phase's end treats it as any unit that ends ADJACENT to its enemy: it surrenders if a captor can take it (A20.21), and is eliminated otherwise. Question 6 below.

**The test game `p35-repulse`,** board 3, German Rout Phase, played through in the Studio and then reset: R1 in [P10] routs to the wooden building [O10], is repulsed by the one concealed squad there, which loses its "?", and stays in [P10]; R2 in [E5] routs to the woods of [F5], where two concealed squads draw by Random Selection (the Studio's dice came up 6 and 6, so both lost their "?"), and stays in [E5]; R3 in [O3] routs to [P2], where a Dummy alone is removed, and goes in. [P2] is not among the routes the panel offers R3, since [N2] is nearer; the route is typed.

**Tests.** Rules: 709 of 709, of them six of this pass. Play: a new class `BacklogPass35Tests`, five tests on the pass 13 board harness: the rout order and the phase's end with a unit bound to surrender (35.17); a Disrupted unit's Low Crawl (35.2); the repulse by one concealed unit, with the record read back from the store and the surrender at the phase's end; two concealed units and the Random Selection; Dummies alone. With the tests of passes 10, 13, 15, 16, and 31 to 31d and the rout tests: 204 of 204.

**ADJACENT, repaired here at the user's word of 2026-10-09** (it was pass 40's: the A.8 row of the inventory). Seen in the test game: R2 in [E5], a level 1 hill hex, was not ADJACENT to the units revealed beside it in [F5] at level 0. A.8 (p. 43), as read: Locations are ADJACENT only if there is a LOS between them (SMOKE Hindrance and NVR left out of it) and a hypothetical Infantry unit could move from the one into the other in the APh, enemy presence ignored. The code asked for the same level and no hexside terrain, which is narrower. The repair: `ScenarioA1MovementCalculator.IsAdjacent` is a LOS and an Infantry step between the two Locations that the movement rules allow, in either direction; Play reads the step with `InfantryStep`, the member an advance and a rout already use. So a hex one level up a hill, a hex across a wall or hedge, and the next level of a stairwell hex are ADJACENT (the coverage document's B23.25 fault too); the upper level of the next building hex is not ADJACENT to the ground beside it, as A10.5's example on p. 67 says; terrain the movement rules refuse stays not ADJACENT, as before. The two levels a stairwell joins have their LOS by it. Either direction is my reading: the rule speaks of moving from that Location into the other, and no step the game admits is one-way. ADJACENT is read by the rout, DM, surrender and capture, DC placement and throwing, and multi-Location fire groups, so every one of them changes where a hill, a wall, a hedge, or a stairwell lies between the two Locations.

**What the ADJACENT repair moved.** The whole Play project, run before any test was changed: 697 of 698, the one failure the upstairs rout test of 2026-10-08 (`ABrokenUnitUpstairsRoutsDownItsStairwell`). B23.25 (p. 136), read for it: a unit in a building is ADJACENT to a level of the same building on its own level in the next hex, or joined to it by a stairwell. So a broken unit that shares level 1 with its enemy finds the floor below, the floor above, and the next hex's level 1 each ADJACENT to that enemy: its rout may pass through one of them (A10.51: it is leaving the enemy's Location) and may not end there (A10.5). The test now asserts that: a rout of one step down is refused, and a rout down and on into the next building hex's ground level is made. The panel's sentence for the case is new: "It may not end its rout ADJACENT to the enemy unit it began beside (A10.51): its route passes through ... and ends in woods or a building beyond." New tests: a Play test of ADJACENT on the pass 10 board (a hill, a wall, a stairwell, the upper level of the next hex, a blocked LOS), and a Rules test of the verdict. In the Studio, in `p35-repulse`: R2 on the hill in [E5] now must rout from the squad revealed in [F5] and surrenders to it as the phase ends. In the test game `p35-levels` (board 3): a broken squad on level 1 of [M5] must rout from the squad on its ground floor, ADJACENT by the stairwell, and may rout to level 1 of [L4], the building's other hex; a broken squad on the ground floor of [M2] is not ADJACENT to the squad on level 1 of [N1]. A further Play test: a unit above its enemy in a one-hex building has no legal rout step.

**The rout's destination, read at the user's word and found right.** R3's rout to [P2] at 3 MF was accepted though [N2] is 2 MF away. A10.51 (p. 66) makes the nearest building or woods hex in MF the destination, and the exception printed with A10.532 (p. 67) lets a routing unit ignore a building or woods hex that is no farther from a Known enemy unit than its starting hex. When R3 routed, the two repulses before it had made German units Known in [F5] and [O10]; [N2] is nearer to [F5] than [O3] is, so [N2] could be ignored and [P2] was the nearest that could not. `RoutTargets` does this. Before those units were Known the panel offered [N2] alone, and a rout to [P2] would have been refused. No change. The test game's Dummy is moved to [N2] so the case does not depend on the order of play.

**The Commissar and the Fanatic, built 2026-10-09 (uncommitted).** A20.21 and A25.22 (p. 94): `SurrenderCandidate` and `SurrendersInstead` take whether the unit is a Commissar, known by its catalog definition as the rally rules know it. A Commissar bound to surrender by any other unit's reckoning routs through the Interdiction, and one that ends the RtPh ADJACENT to its enemy is eliminated, not taken. A10.8 (p. 69), read: a Fanatic unit has its normal and its broken Morale Level raised by one. `BrokenMorale` takes the Fanatic condition, so the Interdiction NMC is against one more, never above 10 (A.18). Tests: Rules, in the broken morale test; Play, three more in `BacklogPass35Tests` (a Commissar routs through two Interdictions; a Commissar that does not rout is eliminated with no surrender pending; a Fanatic squad's NMC is against 8). With passes 13 and 15: 80 of 80.

**The Interdiction test games,** six variants of one position on board 3, each checked in the Studio: a broken Russian unit in the Open Ground of [D10], seen from [F10] two hexes away, whose rout to the woods of [D8] goes by the Open Ground of [D9].

| Game | What differs | What the Studio showed |
|---|---|---|
| `p35-interdict-squad` | A German squad in [F10] | "Interdicted as it enters [D9] by 4-6-7 squad G1, a NMC each (A10.53)" |
| `p35-interdict-leader` | A leader alone with a LMG in [F10] | The squad must rout ("in the LOS and Normal Range of 8-1 leader G1"), and "no step enters Open Ground an enemy unit could Interdict" |
| `p35-interdict-wreck` | A tank wreck in [D9] | "no step enters Open Ground an enemy unit could Interdict" |
| `p35-interdict-fanatic` | The Russian squad is Fanatic | The rout made: "NMC DR 7 against broken morale 8: passed" |
| `p35-interdict-trap` | A second German squad beside it in [E10] | "does not rout: it can get away ... only by Interdiction or Low Crawl, so it surrenders" |
| `p35-interdict-commissar` | The same, and the Russian unit is a 9-0 Commissar | "must rout", with a route offered; the phase's end, proposed and not confirmed: "is eliminated for Failure to Rout", no surrender |
| `p35-interdict-between` | A tank wreck in [E10], between [F10] and the Open Ground (D9.4) | The squad "may rout, since it is under DM" (it need not), and no Interdiction in [D9] |
| `p35-interdict-smoke` | The same wreck burning, its SMOKE along the LOS (B25.2, A24.2) | The same: need not rout, no Interdiction in [D9] |

The last two were added at the user's word, "six variants" above being eight with them. A SMOKE grenade's counter leaves as its MPh ends (A24.11; ruling R9.5), so in the Rout Phase the only SMOKE the game can hold is a burning wreck's.

Task 35.4 is built in full. What it leaves: ruling R13.3's new text, written with the pass's documents.

### 7.5 Questions of increment 2a

**Answered by the user on 2026-10-09: yes to 1 to 4**, as proposed. Question 5 waits for 35.4's code reading. The rulings they write (R13.3 amended for 1 and 2; a ruling for 4, closing the backlog row of section 52) are written with the pass's documents.

| # | Question | Proposal |
|---|---|---|
| 1 | When does the surrender happen: as the RtPh ends (ruling R13.3, as built), or in the unit's turn of the rout order? | Keep R13.3. The page gives no moment for it but for a Disrupted unit (the RtPh's start), the result is the same either way once the deadlock is gone, and an action for it would add a step to every such phase. Write into R13.3 that a unit bound to surrender owes no rout. |
| 2 | Either side may now end the RtPh while the other side's unit is bound to surrender or has no legal step. Is that right? | Yes. Nothing that side could do changes the outcome; the end still shows the surrenders and eliminations as consequences to confirm. |
| 3 | The Commissar's exemption from surrender (A20.21 names Commissars; the code exempts only Fanatics and No Quarter). Take it here? | Yes, as one more input to `SurrenderCandidate`, with the 35.4 build. The other exempt kinds have no counters. |
| 4 | A voluntary rout by a broken unit that need not rout and is not under DM (backlog section 52). | The page settles it: "A broken unit may rout if currently under DM", and otherwise only if it must. The refusal stands; write it as a ruling and close the backlog row. |
| 5 | Fanaticism in the Interdiction NMC (A10.8). | Read with 35.4's code, where the NMC is built. |
| 6 | A unit repulsed by a concealed unit ends its RtPh ADJACENT to it. A10.533 says it is eliminated; the phase's end, as built, lets it surrender when a captor can take it. | Keep it as built. A20.21 covers any broken unit ADJACENT to Good Order armed enemy Infantry that cannot rout away, and the repulsed unit is one; A10.533's words are the general consequence, said before the prisoner rules. Say so in the ruling. |

## 8. Infantry: 35.1, 35.3, 35.8 (increment 2b)

Written and built 2026-10-09, at the user's word to build as the design goes and to give each fix a small test game in the Studio and Play tests. Uncommitted as this section is written.

### 8.1 Task 35.1: one wound procedure

**The rule, A17.1 and A17.11 (p. 85).** Wounds are accounted for only for a SMC, and come from Casualty Reduction or a Sniper. Whenever a SMC is wounded another dr is made at once: a 5 or 6 is mortal and is treated as a KIA, a 1 to 4 is minor. A man already wounded adds +1 to that dr, and being wounded again has no other penalty.

**What the code did.** Fire, Close Combat, the rally's Fate, and the Sniper made the dr. Three paths did not: a rout's Interdiction and the Mopping Up casualty (both through `GamePlanner.Rout.cs`, `CasualtyReduction`), and a PF firer's Casualty Reduction (`GamePlanner.Ordnance.cs`, `FirerEffectEvents`). There a SMC was wounded with no dr, and an already wounded SMC was eliminated outright.

**The repair, in Rules.** A new file, `ScenarioA1Wounds.cs`: `SeverityDue` (a leader or a hero), `Mortal` (the dr, +1 if already wounded, 5 or more), and the purpose `wound-severity` of the roll. `ScenarioA1Sniper.Mortal` forwards to it, so there is one procedure. `ScenarioA1RoutCalculator.CasualtyReduction` and `ScenarioA1OrdnanceEventRules.Casualty` take the Wound Severity dr and say Wounded or Eliminated by it; with no dr for a SMC they throw, so no path can skip it again. Play rolls the dr in each of the three paths as the events are built and records it as its own `dice-rolled` event. The record: "Wound Severity dr 3: a 5 or more is mortal, with +1 for a man already wounded (A17.11)".

**Not in this task.** A mortal wound is a KIA, and a leader's loss calls for a LLMC of the units with him (A10.2); the rout path, like the paths that already rolled the dr, does not make one. The record says the dr and not, in a sentence of its own, whether the man was wounded or lost; the events after it do.

**Tests.** Rules: the verdicts with and without the dr, the throw, and that the Sniper's is the same function. Play (`BacklogPass35Tests`, a theory of four): a broken leader fails its Interdiction NMC; unwounded with a dr of 3 he is wounded and routs on, with a 5 he is lost; already wounded with a 3 he survives, with a 4 he is lost. The Mopping Up and PF paths have no Play test: both call the Rules verdicts tested here.

**Test games.** `p35-wound` and `p35-wound-again` (board 3): a broken 8-0 leader in [D10] routs by [D9] to [D8] with the dice queued 6, 5, 3. Both show "NMC DR 11 against broken morale 8 [7 for the wounded one]: reduced" and "Wound Severity dr 3", and the leader routs on to [D8]. Before the repair the already wounded leader was eliminated.

### 8.2 Task 35.3: Good Order, and the Morale ceiling

**The rules.** A.7 (p. 43): Good Order is a Personnel unit or inherent crew that is not broken, berserk, captured, stunned, shocked, or held in Melee; a unit can be pinned, CX, TI, or unarmed and still be in Good Order. A.18 (p. 44): a Morale Level can never be raised beyond 10, "even if the unit is Fanatic, heroic, with a Commissar, and/or part of a Human Wave".

**What the code did.** The planner's definition (`GoodOrderAsPlanned`) left a TI unit out and let a berserk unit in. Two of the three scans for "a Good Order enemy with a LOS" tested only "not broken". A berserk Fanatic unit's Morale Level was 11 (ruling R30.3), and a Fanatic's +1 was added with no ceiling in the rally, the PAATC, and Leader Creation.

**The repair, in Rules.**

- `ScenarioA1Definitions.GoodOrderOf`: active, and not broken, berserk, in Melee, captured, stunned, or shocked. It replaces `GoodOrderAsPlanned`. The state's own `GoodOrder` already read A.7 for Personnel and is left as it is.
- `FreeToActAsPlanned`: Good Order and not TI. The six SW and Deployment actions (Deploy for the squad and for the leader, Recombine, Transfer, Recover, Drop) used Good Order to keep a TI unit out; they keep that bar under its own name, so nothing changes for a TI unit there, and a berserk unit is now refused them.
- The scans: `NearestGoodOrderEnemyInLos` and `EnemyGoodOrderInLosWithin16` read the Good Order fact; the second, the move's read of who sees a concealed mover, also leaves out a hidden unit and a Passenger, as fire's `WithSeen` does. This is backlog section 51's row, taken here at the user's word (F5). The read serves movement, the advance, the rally of a concealed unit, and a concealed crew's Gun.
- `MoraleCeiling`, applied where a Morale Level is raised: fire's `Morale` (a berserk Fanatic is 10), the rally, the berserk leader's companions, the PAATC, Leader Creation in Close Combat, and the rout's broken Morale Level.

**Rulings touched.** R30.3 gave a berserk Fanatic unit 11; A.18 forbids it, and that clause is withdrawn. R10.10's read of who forces a mover's loss of "?" becomes "a Good Order enemy unit that is not hidden".

**The Close Combat calculator's own Good Order** (not broken, berserk, or captured, with no Melee clause) is left as it is: it chooses the leader who directs a Close Combat, where every unit is in the Melee, and the A.7 clause would bar them all. Said here so the difference is a known one.

**Tests.** Rules (`ScenarioA1Pass35RulesTests`, new): Good Order clause by clause; the two scans with an enemy not in Good Order, hidden, aboard, a Dummy, and out of range; the ceiling. Play: a berserk squad may not Deploy and the squad beside it may; a concealed mover loses its "?" to a Good Order enemy and keeps it before a hidden one.

**Test games.** `p35-berserk-deploy`: the berserk squad's Deploy is refused ("is not a Good Order squad"), the other squad's passes. `p35-seen-viewer` and `p35-hidden-viewer`: a concealed Russian squad moves from [D10] to [D9] in the LOS of the German squad in [F10]; seen by a Good Order squad it loses its "?", seen only by a hidden one it keeps it, and the record says only that it moved. The Morale ceiling has no Studio game: it needs a berserk Fanatic unit under fire, and is tested in Rules.

### 8.3 Task 35.8: the Russian 4-2-6's Battle Hardening

**The rule, A25.2 (p. 93).** "A 4-2-6 squad Battle Hardens to a 5-2-7 [EXC: 25.211]; a 2-2-7 HS to a 3-2-8." A25.211: before 1941 the Conscript squad and HS harden to a 4-4-7 and a 2-3-7 "instead of to 5-2-7/2-2-7", unless the Russian OB holds a 6-2-8 or a 5-2-7.

**What the code did.** `ScenarioA1FireReference.Hardened` sent the 4-2-6 to the NKVD 6-2-8 and the 2-2-6 to the NKVD 3-2-8, reasoning from the classes. Two tests asserted it.

**The repair.** The 4-2-6 hardens to the 5-2-7 (`defender-line-squad`) and the 2-2-6 to its 2-2-7 (`defender-line-half-squad`), as A25.211's own words pair them. The 2-2-7 to the 3-2-8 was already right. The two tests are corrected.

**Left out: A25.211's exception** for scenarios before 1941. It needs the scenario's year and the Russian OB as facts of the hardening, which is a lookup by definition today. No card in the library is that early. A backlog row.

**Tests.** Rules: the three hardenings. **Test game.** `p35-harden`: a broken 4-2-6 rallied by its leader with the dice queued 1, 1, then 1, 2: "Heat of Battle DR 1, 2 = 3 + 2 (Russian) + 1 (broken) + 1 (inexperienced) = Final DR 7: Battle Hardened into 5-2-7 squad".

### 8.4 Questions of increment 2b

| # | Question | Proposal |
|---|---|---|
| 1 | The SW and Deployment actions keep their bar on a TI unit under its own name. Is that right, or should a TI unit now be free to take them, since A.7 calls it Good Order? | Keep the bar. A.7 says what Good Order is, not what a TI unit may do; I did not read the TI rules for these actions, and the bar is as play had it. A row for the pass that reads them. |
| 2 | Ruling R30.3's berserk Fanatic at 11 is withdrawn for A.18. | Yes: the page leaves no room. |
| 3 | A25.211, the pre-1941 exception, goes to the backlog. | Yes. |
| 4 | A mortal wound in a rout makes no LLMC of the units with the leader. | Backlog, with the same gap wherever a leader is lost outside fire; pass 70 (morale) reads A10.2. |

## 9. Terrain: 35.5, 35.6, 35.7, 35.9 (increment 2c)

Written and built 2026-10-09. Uncommitted as this section is written. At the user's word of that day, each change was tested by the test classes it touches and no wider.

| Task | Rule, as read | What the code did | The repair, in Rules | Tested by |
|---|---|---|---|---|
| 35.5 | B.2 (p. 112): the COT is the hex's cost plus that of the Artificial Terrain in it, so a level climbed doubles SMOKE's MF too: "2 x 2 = 4 MF, not 2 x 1 = 2 + 1 = 3 MF", Abrupt Elevation Changes excepted. | `GroundStep` doubled the terrain's cost and then added SMOKE's. | `ScenarioA1TerrainCosts.GroundStep` doubles the two together one level up; across an Abrupt Elevation Change SMOKE's MF is added once, as before. A burning wreck's extra MF is charged the same way. | Rules; the Studio game `p35-smoke-hill`: a squad climbing from [F5] into [E5], level 1 with a burning wreck, "for 4 MF" |
| 35.5 | B.10 (p. 113): any combination of SMOKE, weather, and terrain Hindrance of +6 or more blocks the LOS. | The map's own total blocked at six; the terrain's count with vehicles', wrecks', and SMOKE's was never tested, and such an attack was made at +6 or more. | `ScenarioA1FireMapRules.HindranceBlocks`, read in `LocationLos`, where the total is formed. | Rules |
| 35.6 | B14.4 (p. 129): an orchard costs what Open Ground does. | A vehicle was refused an orchard ("is not allowed"). | Three rows in the vehicle terrain table, equal to Open Ground's. | Rules |
| 35.6 | B15.2, B15.6 (p. 129): grain is a Hindrance in its season, June to September, and Open Ground outside it. | A LOS through grain out of season was refused as an unattributed Hindrance, in fire and in ordnance. | Only a game with no scenario month leaves grain on a LOS undecided (three conditions). | The existing fire and ordnance tests; no test of its own |
| 35.7 | C3.53 (p. 170), B16.31 (p. 130): ordnance HE is halved into a marsh, beside any halving of the Area Target Type. | Full FP. | The hit's FP is halved for a marsh target in `ScenarioA1FireCalculator`. Ruling R10.1's leave-out ends. | No test of its own |
| 35.7 | B16.43 (p. 130): a ground level or level -1 hex adjacent to a marsh is a Bog hex for a vehicle entering by a non-road hexside. | No Bog Check. | `EntryCost` takes "beside a marsh" as a fact, read in Play from the hex's neighbours, and adds a Bog Check. | Rules |
| 35.9 | B25.14 (p. 143): a Wreck Blaze is a Blaze counter on the wreck; a CC attack DR of half the DR needed or less makes one. | The wreck was recorded as burning with no Blaze entity, so it gave cover and no smoke. | No rule changed: Play writes the Blaze entity after the wreck, as fire and ordnance do. | Play (`AWreckBurnedInCloseCombatCarriesItsBlaze`) |

**Not built, and proposed for another pass: the orchard's season in the LOS (B14.2).** As read: an orchard is a one-level obstacle to a LOS to or from a higher elevation from April to October, and out of season each orchard hex is a +1 Hindrance to such a LOS. Today every orchard is in season, since nothing ties the LOS to the scenario month. The LOS is in Maps, which holds both terrains ("Orchard" and "Orchard, Out of Season") and refuses several of the out-of-season cases outright. Giving it the season means a second LOS map for each board and its cache, and would turn some winter LOS from wrongly blocked into undecided. Fire leaves every orchard Hindrance undecided as well, which pass 135 (task 135.2, "every Hindrance decided in fire", with B14.21) is there to settle. Proposal: the B14.2 row moves to pass 135, with a backlog row.

**The pass 32 design's section 12, item 3** (grain in season April to September for a cost, June to September elsewhere): not a fault. B15.6 says plowed fields in April and May keep grain's movement cost and are Open Ground for everything else. The two copies stay.

**What has no Studio game.** Board 3 has no orchard and no marsh, and a Hindrance total of six, HE into a marsh, and a vehicle's Close Combat are long to stage. Those are tested in Rules or Play as the table says. Two repairs have no test of their own: out-of-season grain on a LOS, and the halved HE.

**Questions of increment 2c**

| # | Question | Proposal |
|---|---|---|
| 1 | The orchard's season in the LOS (B14.2) moves to pass 135. | Yes, for the reasons above. |
| 2 | Write a test each for out-of-season grain on a LOS and HE into a marsh? Both need the fire calculator's test harness. | Yes, at the gate, with the pass's other tests; about 0:10 each. |

## 10. Ordnance: 35.12, 35.10, 35.11 (increment 2d)

Written and built 2026-10-09, in that order. 35.12 is commit 09a9251 and 35.10 is 8b6a9b3; 35.11 is uncommitted as this section is written. Each change was tested by the test classes it touches and no wider.

**The user's answers of that day.** Section 7.5 question 6 and section 8.4 questions 1 to 4: yes to each proposal. The Mud road through a wall's gap (below) goes to task 35.14. The in-hex Hindrance of Case E is built in 35.10; the proposal to defer it named a pass that does not exist (C6.9 is built) and was withdrawn.

| Task | Rule, as read | What the code did | The repair, in Rules | Tested by |
|---|---|---|---|---|
| 35.12 | C10.1 (p. 180): no vehicle tows a Gun over a wall or hedge or into a building or rubble, nor uses Reverse or Bypass while towing. | A towing halftrack crossed a hedge, a towing tank entered rubble, and VBM was admitted at one MP more. | `ScenarioA1VehicleTerrainCosts.EntryCost` refuses a wall or hedge hexside (a road's gap excepted) and rubble; `TowingBypassBar` refuses VBM, and `BypassHalfMp` loses the tow's MP. | Rules; the Studio game `p35-tow2` (the wall of [O3], the woods about [D9]) |
| 35.10 | C5.1, C5.11, C5.2, C5.5 (pp. 171, 172): Case A and Case E doubled, Case B at +3, and the CA fixed after a first shot, for a firer in woods, a building, or rubble. | Built for woods and buildings. | Rubble joins `ScenarioA1Definitions.WoodsOrBuilding`, the one list all four read. The recorded fact keeps its name. | Rules |
| 35.10 | C5.34, C13.8 (pp. 172, 185): a PF or PSK from ground-level rubble or a building's ground floor takes Case C3, and only an unpinned firer may fire so. | Buildings only. | `ScenarioA1OrdnanceMapRules.IsBackblastLocation`. | Rules |
| 35.10 | C6.3 (p. 174): Case L is denied only to a non-ATR LATW using its own To Hit Table. | Denied to every LATW. | The ATR takes it in `ScenarioA1ArmorCalculator`. | Rules; the Studio game `p35-ordnance` ("DRM case-l -1"). `BacklogPass9Tests.AnAtrFiresOnceAPhaseAndItsSquadStillFiresItsInherentFp` relied on the fault and now gives its hit a To Kill DR |
| 35.10 | C6.6 (p. 175): Case O, -2, against a target in Hazardous Movement, never with Case J or its subcases. | A Gun firing at a crew pushing its Gun took Cases J3 and J4. | `ScenarioA1OrdnanceCalculator.MoverDrm`, read by the Infantry and Area Target Types; the `Hazardous` fact of the shot's movement, decided by `ScenarioA1FireMapRules.HazardousMovement` as for the IFT. | Rules; Play (`AGunFiringAtACrewPushingItsGunTakesCaseOAndNoCaseJ`) |
| 35.10 | C5.2, C5.34, C13.1 (pp. 172, 183) with A7.25 (p. 55): Case B, Case C3, the one shot of the AFPh, and the loss of Multiple ROF are for AFPh fire not using Opportunity Fire. | An Opportunity Firer's Gun took Case B and fired once; its LATW took Case C3. This was graded "read, not run" in section 4. | `OpportunityFire` and `AfphFire` in `ScenarioA1OrdnanceCalculator`; the crew's `OpportunityFire` fact, decided by `ScenarioA1OrdnanceEligibility.OpportunityFirer` from its Bounding Fire counter in the AFPh. A pinned firer in a Backblast Location is still refused. | Rules; Play (`AnOpportunityFirersGunFiresInTheAfphWithoutCaseBAndOnItsKeptRof`); the Studio game `p35-ordnance` (the Gun with Case L alone beside a tank with "case-b +2", then "it may fire again this phase") |
| 35.10 | C5.5 (p. 172) with A24.2, A24.8 (p. 91) and D9.4 (p. 210): a wreck, SMOKE, or an AFV in the Gun's own hex, other than firer and target, adds its Hindrance as Case R. | The shot's Hindrance was a fixed 0. | `ScenarioA1VehicleSightRules.OwnHexHindrance`: the hex's SMOKE as for fire within it, and +1 for a wreck or AFV. | Rules; the Studio game `p35-own-hex3` ("case-e +2, case-r +1" beside a wreck, "case-r +3" beside a burning one) |
| 35.11 | C6.5 (p. 174): the target stays acquired until the Gun or its manning Infantry leaves its Location, or the Gun changes its CA without firing on its acquired target in the current phase. | Kept when the Gun turned without firing, was pushed, when a tank moved, and when a light mortar was carried off. | `ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn`, read by the projector with the firer's Location before and after each event and the Gun's shots this phase. | Rules; Play (`AnAcquisitionIsLostWhenTheGunTurnsWithoutFiringOrIsPushed`) |
| 35.11 | C6.51 (p. 174): the counter goes with its target while the target is in the acquirer's LOS and stays in the last Location it held there. | Built for a Gun at Infantry. A vehicle target left the counter on its Location, and a tank's own Acquisition was never followed. | `ScenarioA1ResultTables.AcquiredVehicle` names the vehicle a Vehicle Target Type shot leaves acquired; Play's follow-up reads a tank's Location as it reads a Gun's. | Rules; Play (`AnAcquisitionFollowsAVehicleAndStaysWhereItLeftTheGunsLos`); the Studio game `p35-acquire` ("Acquisition -1 on [D9]", then "on [D10]") |

**What has no Studio game.** Board 3 has no rubble and no hedge known to this pass, so the rubble rows and the hedge crossing rest on Rules tests. Case O and the loss of an Acquisition are shown by Play tests. A live AFV in a Gun's own hex cannot be reached at all: the gate refuses an Infantry Target Type shot at any Location that holds a vehicle (C3.31; rulings R25.10, R7.2), so that part of `OwnHexHindrance` is tested in Rules alone.

**Found beside the tasks.**

| # | What | Proposal |
|---|---|---|
| 1 | In Mud a vehicle on an unpaved road through a wall's gap is charged the wall, or refused it: the road flag is cleared before the wall is read (`EntryCost`). | Task 35.14, where E3.6 is read. Agreed by the user. |
| 2 | The gate's bar on an Infantry Target Type shot at a Location holding any vehicle, friend or foe. | A backlog row for the mixed hit, naming the in-hex AFV Hindrance among what it blocks. Agreed by the user. |
| 3 | In `p35-acquire` the AT Gun's shot at a tank that had just made a VCA change from Motion took Case L beside Case J. C6.3 denies Case L against a Motion or Non-Stopped vehicle. Seen once, not traced. | Read with task 35.13, which reads Motion and Stopped (D2.4). |

**Left out, each for a backlog row:** Deep Stream, wagons, the 76 to 107mm MTR exception, and Narrow Streets (C10.1); 360-degree mounts and bow-mounted Guns (C5.1); a vehicle as firer or target within a hex, Bypass and the CAFP (C5.5); Desperation fire (C13.81); Intensive Fire by an Opportunity Firer (A7.25, pass 45 in the inventory); the Acquisition's other losses (a CMG's or a CC attack on another target, the crew's Inherent FP, SW, or Interdiction, limbering) and the Gyrostabilizer (C6.5, C6.55).

## 11. Vehicles: 35.13 (increment 2e)

Written and built 2026-10-09, in five groups and the ESB table, a commit each: a is caa9fe4; b and h are 2b7cc64; c and d are 9fe3102; f and g are e0dd3f2; e, i, and j are 43116fb; the ESB table is uncommitted as this section is written. The catalog held every counter the rows need (a T tank, an RST tank, an OT halftrack, trucks), so no definition was added.

| # | Rule, as read | What the code did | The repair, in Rules | Tested by |
|---|---|---|---|---|
| a | D2.16, D5.2 (pp. 196, 203): a BU AFV pays a full MP a road hexside; a CT AFV is BU with no counter (ruling R7.11). | Six places read the recorded condition alone, so a default-BU tank was CE to them. | Play's `ButtonedUpAfv` asks the Crew Exposed verdict (`ScenarioA1FireEligibility.CrewExposed`) for the road rate, the Recall exit, the NVR, the night move bar, the button-up answer, and the Starshell firer. `ExitHalfMp` gains the snow clause (pass 32 design, section 12, item 15, first half). | Rules; Play (`ATankButtonedUpByDefaultIsAlreadyBuAndMayExposeItsCrew`); the Studio game `p35-bu-road` (tank "1 MP", CE halftrack "half MP" from [E6] to [F6]) |
| b | D2.38 (p. 198): a vehicle in Bypass takes no beneficial TEM of the woods or building it Bypasses. | A Vehicle Target Type shot gave it the obstacle's Case Q. | The vehicle target's `Bypass` fact; `ScenarioA1ArmorCalculator` adds no positive Case Q with it. | Rules; the Studio game `p35-bypass` ("range 1, woods; DRM case-j +2") |
| h | D9.4 (p. 210): a Bypassing AFV or wreck is no Hindrance to a LOS that does not touch the hexside it Bypasses. | It always hindered. | `ScenarioA1VehicleSightRules.BypassHinders`. The LOS is read as touching the hexside when it also passes through the hex across it, or starts or ends there (agreed by the user as the ruling's text). | Rules only: no LOS was laid across a named hexside |
| (found) | C6.3, D2.13 (pp. 174, 196): no Case L against a Motion or Non-Stopped vehicle. | Non-Stopped was the Motion counter alone, which comes off at the first MP spent. | `VehicleTarget`: Non-Stopped is also a vehicle started in its MPh and not stopped since; never one bogged or immobilized, whose Bog Removal is its Start MP (D8.3). | Rules; the Studio games `p35-bypass` and `p35-bog-mired` |
| c | D1.321, D1.322 (p. 194): an RST or 1MT AFV fires neither its MA nor its CMG while CE. | A CE RST tank added its MA base and CMG to an OVR and its CMG to CC. | `FireDefinition.MaType` and `TurretBarredWhileCe`, read by `OverrunWeapons` and the vehicle's CC FP. | Rules (`ScenarioA1Pass11Tests`); the Studio games `p35-rst-ovr` (13 FP BU, 5 FP CE) and `p35-rst-cc` (4 FP BU, "No cc armament" CE) |
| d | D5.34 (p. 203): under a Stun "+1" counter an AFV adds one to its OVR DR and its CC DR. | Neither DR took it. | A `StunRecovery` fact on the OVR and on the CC vehicle. | Rules; the same two Studio games (Final DR 11 against 10; 7 against 6) |
| f | D8.5 (p. 209): a bogged vehicle may spend MP for non-movement purposes, loading and unloading among them. | Every expenditure but Bog Removal was refused, in the planner and, in other words, in the projector. | `ScenarioA1VehicleProjection.BoggedMaySpend`, one answer for both (section 12, item 6). The Play page offers the unload. | Rules; the Studio game `p35-bog2` |
| g | D5.341 (p. 203): a bogged or immobilized Recalled AFV is Abandoned; ESB is NA; with Passengers it may Stop long enough to unload them. | Only an immobilized one was Abandoned; ESB was allowed; every Stop was refused. | `RecallAbandoned` takes the bog; `EsbBar` takes the Recall; `StopBar` and `ScenarioA1RecallCalculator.StopsToUnload` let a carrying AFV Stop. | Rules; the Studio games `p35-bog`, `p35-recall-stop`, `p35-bog-recall` |
| e | D7.21 (p. 207): the CC counter of CC Reaction Fire prohibits Non-CC Reaction Fire. | The counter was set and never read. | `ScenarioA1FireMapRules.FirerLocation` refuses a marked unit's fire at the vehicle in its Location in the MPh. | Rules; the Studio game `p35-cc-counter` |
| i | D5.33 (p. 203): no CE counter placed or removed in a MPh after the vehicle's own Prep or Bounding First Fire. | Prep Fire only. | `ButtonUpBar` takes Bounding First Fire. | Rules; the Studio game `p35-bff-bog` |
| j | D8.4 (p. 209): a vehicle that begins its MPh bogged is fired on in its Bog hex, for its Bog Removal MP, with no Case J. | Case J from the MP in the firer's LOS. | `ScenarioA1OrdnanceMapRules.InBogHex` and the shot's `InBogHex` fact. | Rules; the Studio games `p35-bff-bog` ("DRM none") and `p35-bog-mired` ("DRM case-l -1") |
| ESB | D2.5 (p. 198): the ESB DRM Table by the manufacturer's nationality. | German +2, Russian +1, all others +3. | `EsbNationalDrm`: U.S. and Czech 0, Russian and Chinese +1, British and German +2, others +3. Latent: the catalog's vehicles are German and Russian. | Rules; the Studio games `p35-esb-german` ("+2 German") and `p35-esb-russian` ("+1 Russian") |

**The pass 32 design's section 12.** Item 5 (a TI vehicle's MP): the projector bars it as the planner does, by one entry in its own list; pass 50 may move the list to a shared calculator. Item 6: closed by `BoggedMaySpend`. Item 14 (three groupings of "Stunned"): not read by the Stun +1 repair, so it stays with pass 65. Item 15: the snow clause is done; the Recall exit's night and weather MP are 35.14's.

**What the Studio found that the tests had not** (the user's rule of 2026-10-09 holds whatever a fix's size; an exception is the user's to give):

1. The page did not offer the unload from a bogged vehicle: its own test of "stopped" left bogged vehicles out.
2. A Recalled AFV's Stop was refused by the shortest-route check before the Stop rule was read.
3. The Non-Stopped repair would have denied Case L against a tank still bogged after a failed Bog Removal.
4. The Close Combat panel for a Location with a vehicle keeps its checked defender from one Location to the next, and the next attack is refused as outside until the page is reloaded. Not a rule: for increment 2g, with 35.16.

**Left out, each for a backlog row:** a building Bypassed inside a woods hex, the LOS to the CAFP, the entrenched and Dug-In cases and the concealed AFV's reveal (D2.37, D2.38, D9.4); the crew counter's +1 after a bail-out, Stun by a Sniper, a MG TK DR, or Falling Rubble, a 1MT AFV's Recall on a Stun (D5.34, D1.322); whether a CE RST tank's CMG still counts as a manned MG against the CC DRM (A11.51); the other non-movement expenditures of a bogged vehicle and the bow MG limits (D8.5); "as soon as possible" for the Recalled AFV's unloading, the crew's bar on re-crewing, Armor Leader loss (D5.341); the Unarmed bar, Cavalry, and the CC counter on SW and Guns (D7.21); Passengers CE or BU with the crew, and fire by the PRC (D5.33); a vehicle in another nation's service (D2.5).

**One reading without a sentence of its own.** A tank freed by its Bog Removal is treated as Non-Stopped from then on, since it has paid its Start MP and not stopped. No rule read says so outright.

## 12. Night and weather: 35.14 (increment 2f)

Written and built 2026-10-09. The ESB table is commit f6ecc93; 35.14 is uncommitted as this section is written.

**The unsettled question, settled on the page.** Is the weather's cost multiplied when a vehicle reverses? No. D2.21 (p. 196) puts the Reverse multiplier on "the total cost of entry of that hex, including any penalties/multipliers". E3.9 (p. 231) adds the weather's MF and MP "per hexside crossed (or Bypassed), after calculating total cost". E1.52 (p. 224) adds the night's MP "as if towing a Gun", and C10.1 (p. 180) adds the tow's "after all other modifications to the MP/MF cost". So both are added once, after the multiplier. The two sentences pull against each other only if "total cost" in D2.21 is read to reach forward into E3.9; the later, narrower rule is taken. This is a reading; the user agreed to it on 2026-10-09, and it goes into a ruling with the pass's documents.

| Row | Rule, as read | What the code did | The repair, in Rules | Tested by |
|---|---|---|---|---|
| E3.9, D2.21 | As above. | The weather's and the night's MP were multiplied by Reverse with the rest: a tank reversing into Open Ground at night paid 8 MP. | `ScenarioA1VehicleTerrainCosts.EntryCost` multiplies, then adds them: 5 MP. | Rules; the Studio game `p35-night` ("enter C10 in Reverse (5 MP)") |
| E1.52, E3.9 | The night's MP per hexside "crossed (or transited via VBM)"; the weather's per hexside "crossed (or Bypassed)". | VBM added neither. | `BypassHalfMp` takes them, after its doubling and the Reverse multiplier. | Rules; the Studio games `p35-night` and `p35-mud` (VBM 3 MP against 2 in `p35-day` and `p35-dry`) |
| E3.9, E3.64 | The same for Infantry Bypass. | Infantry Bypass added none. | `ScenarioA1TerrainCosts.BypassStep` takes the weather read: once for the level, and once more for each further hexside Bypassed. | The Studio games `p35-mud` ("for 1.5 MF") and `p35-dry` ("for 1 MF"); no Rules test of the Bypass step itself |
| E3.64, E3.65 | In Mud an unpaved road is Open Ground, whatever else its hex holds. | Infantry by an unpaved road into a hex whose other terrain is not Open Ground paid no half MF. | `InfantryWeatherHalfMf` counts the unpaved road. | Rules only: no such hex is known on board 3 |
| E1.52 | A BU AFV with an NVR of 0 spends MP only to Stop, but its Passengers may unload. | The unload was refused. | `MoveBar` lets the unload through. | Rules; the Studio game `p35-night0` |
| Section 12, item 15 | The Recall exit pays the night's and the weather's MP as an entry does. | It paid neither. | `ExitHalfMp` takes them; Play reads them for the hexside crossed. | Rules; the Studio game `p35-night` (exit 2 MP against 1 in `p35-day`) |
| Found in 35.12 | B9.4: a road crosses a wall or hedge by its gap. | In Mud an unpaved road lost its gap with its rate: a tank paid the wall and a truck was refused. | `EntryCost` remembers the gap before Mud takes the road's rate. | Rules only: no such hexside is known on board 3 |

**Left out, each for a backlog row:** the other Open Ground of E3.65 (gullies, dry streams, plowed fields, shellholes, trenches, unplowed roads in Deep Snow), which wait for their terrain; a Recall exit by an unpaved road in Mud still pays the road rate, not the Open Ground COT; Straying (E1.53) is untouched.
