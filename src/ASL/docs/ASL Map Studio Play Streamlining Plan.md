# ASL Map Studio Play Streamlining Plan

**Written:** 2026-10-21, from the [ASL Map Studio Play Streamlining Analysis](<ASL Map Studio Play Streamlining Analysis.md>) and the user's rulings of the same day: S1 to S5 approved in general, open to refinement; Q6 to Q10 approved as recommended.
**Branch:** `play-streamlining`, cut from `main` at 52a185c5. S5 is built there, uncommitted.
**Status:** approved by the user 2026-10-21 ("Proceed"); P0 to P7 built on branch `play-streamlining` (S1 to S6), each checked in my Studio; P8 (the gate and the pull request) in progress. What is left is in the backlog, section 68.
**Scope:** the Play page and what it asks of Play and Rules. Every ASL rule stays in the Rules project (the ruling of 2026-10-05): a new rule fact goes into a calculator there, with Play gathering its facts. Vehicles' movement plans, the DEFENDER's LOS overlay (backlog 68), and standing fire orders (backlog 68) are out.

## 1. Baseline, expected gains, method, and when to stop

**Baseline.** From the user's game `dl-tractor-works-01` (analysis, section 1): 285 actions, each a Propose and a Confirm; 119 refusals; a Movement step 134 s, a DEFENDER pass 51 s; 51 passes, about 43 minutes. A step with a pass is about eight clicks and two hand-overs (analysis, section 2). Task P0 measures this again on a small game, so before and after compare like with like.

**The measurement game** (the user, 2026-10-21: "sets or 'clusters' of those units exercising different movement scenarios"). Card `ps-3v3` (a user card: board 1, no OB, the Germans moving first), game `ps-3v3-base`, its German Movement Phase saved as `ps-3v3-mph-template` (revision 22) and copied before each measurement. The layout is `code-builders/boards/los-survey/ps-3v3-layout.txt`; the survey tool's `routes` mode checks that every step is ADJACENT and lists which Russians see it. Seven Russian squads (r5 and r7 concealed), eleven German squads and a leader, twelve stacks, 29 steps:

| Cluster | Germans and route | Russians | What it exercises | Steps seen / unseen |
|---|---|---|---|---|
| C1 | g1 EE10-EE7, g2 FF10-FF7, g3 GG10-GG7 (Open Ground, then woods) | r1 AA10, r2 BB10, r3 BB9 | Mutual LOS: the DEFENDER asked at most hexes; "Let it move on" and "to the end" | 6 / 3 |
| C2 | g4 R4-R2 (road between buildings) | none sees it | Every step passed for the DEFENDER | 0 / 2 |
| C3 | g5 M4 (building), L4 (street), K5 (building) | r4 L1 | Asked only at the exposed street | 1 / 1 |
| C4 | g6 U7-U4 | r5 W6, concealed | A concealed DEFENDER with LOS: the hand-over is not skipped | 1 / 2 |
| C5 | g7 with leader l1, B10, B9, C9 (woods), D8 (building) | r6 A9 | MF costs, the leader's bonus, a route longer than a squad's MF refused while plotted | 2 / 1 |
| C6 | g8 G10-D10, then g9 G10-E10 behind it | r6 A9 | Fire mid-route stops the plan; Residual FP met inside the next stack's step | 5 / 0 |
| C7 | g10 N6-N9, into the concealed r7's hex | r7 N9, concealed | The plan stops at the reveal | 3 / 0 |
| C8 | g11 C10, B10, A10, ADJACENT to r6 after it has fired | r6 A9 | Subsequent First Fire and FPF still ask; a spent unit does not | 2 / 0 |

**Measured today (P0, 2026-10-21, my Studio at a0165947).** One step played as the user plays it (g1 EE10 to EE9), each action and each response timed in the page:

| Part of a step | Actions | Hand-overs | Responses |
|---|---|---|---|
| The moving side: open the units list, tick the stack, the hex (typed; picked on the map is two), Propose, Confirm | 5 (6 picked) | | propose 257 ms, confirm 211 ms |
| The DEFENDER: hand over, "I am the Russian side", Propose pass, Confirm, hand over, "I am the German side" | 6 | 2 | 86, 125, 98, 185, 73, 163 ms |
| End of a stack's move: Propose, Confirm | 2 | 0 | 104, 156 ms |

The units list is emptied by each hand-over, so every step costs the same. The page's flow does not depend on LOS or terrain (the window opens after every step), so for the whole measurement phase: **29 steps × 11 + 12 stacks × 2 = 343 actions and 58 hand-overs**, about 1.2 s of the page's responses a step; the rest of a step's 134 s in the user's game was the player's.

**Expected after S6, for the same phase, with the DEFENDER firing nowhere:**

| Measure | Now (measured) | After (expected) |
|---|---|---|
| Hand-overs | 58 | 24 (two per stack; none skipped, since r5 and r7 are concealed) |
| Actions by the moving side | 29 × 5 + 12 × 2 = 169 | 12 stacks × 2 (choose, commit) + 29 hex clicks = 53 |
| Actions by the DEFENDER | 29 × 6 = 174 | 20 decisions at the steps a Russian sees, and 24 view confirmations = 44 |
| Steps where the DEFENDER is asked | 29 | 20 (the 9 unseen steps pass for him) |
| Refusals for MF or a hex the stack may not enter | possible at each step | none: refused while plotting (C5's over-long route) |
| Total actions | 343 | about 97 |

Each fire adds two hand-overs and the fire's own actions in both columns.

**Measured after P5 (2026-10-21, my Studio at db092393, game `ps-3v3-p5b`).** The whole German Movement Phase, the Russian side letting every stack move on: g1 by hand, the other ten stacks by a driver in the page that clicks what a player clicks (the map's clicks dispatched as pointer events on the board) and counts each action.

| Measure | Before (P0) | Expected | Measured |
|---|---|---|---|
| Actions | 343 | about 97 | 148 |
| Hand-overs | 58 | 24 | 22 |
| Steps where the DEFENDER was asked | 29 | 20 | 24 |
| Refusals for MF or a hex the stack may not enter | possible | none | none |

The stop rule holds (148 of at most 171 actions, 22 of at most 29 hand-overs). Actions came in above the estimate because each hand-over is two actions (the button and the view's confirmation) and the estimate counted one, and because of the next finding. Steps made: 28 of 29; [A10], on the board's left edge half outside the map, took no click, as a player would pan first.

Findings of the run, for the next tasks:

- **"Since you last looked" covers the map after each hand-over.** It opens over the bottom of the map (rows 9 and 10 at this size) every time the moving side's view comes back, which with a plan is once a stack; it cost ten clicks to close and hid hexes being plotted. Proposed: after a hand-over that ends a plan, leave it closed and say in the header what happened.
- **The DEFENDER was asked at 24 steps, not 20.** The rule asks wherever a Snap Shot could be taken (LOS to the Location left, A8.15), which the LOS survey did not count; it errs the way it should.
- **Once, in a long session, the map stopped answering clicks** (real clicks too) until the page was reloaded; not seen again in the rest of the run. To be reproduced before the gate.
- **g10's step into [N9] met the concealed r7** (A12.15): r7 was revealed, g10 forced back, and the engine ended its move; the plan said "no member of it may move on". Correct by the rules; the words could say what happened.

**Method.** The clicks and hand-overs counted in the Studio as the same Movement Phase is played, before and after, with scripted dice; the time of the phase read from the page as in the Play Step Analysis; the refusals read from the audit log.

**When to stop.** If, after P5, the measurement game's Movement Phase takes more than half of P0's hand-overs or actions (29 and 171), stop and report before P6.

**The regression pair, measured 2026-10-21 after P5.** `main` at f6acba66 (whose code is the branch's base, 52a185c5: it adds only the guidelines and two documents) in a worktree outside the repository, Studio on 6671; the branch at 51eea80a on 6670; both Debug, both the Play follower on `cd-guards-5` as the adjudicator, cut to 714 and run a step a second to 760, each tab in front while it ran and the other Studio idle; `split.py` read each step's work.

| Run | Rout Phase median (9 steps) | Other phases median (11 steps) |
|---|---|---|
| `main`, first (cold) | 469 ms | 304 ms |
| branch, first (cold) | 363 ms | 256 ms |
| `main`, second | 321 ms | 191 ms |
| branch, second | 270 ms | 174 ms |

No regression: the branch is as fast or faster in both pairs, within the noise the runs show between first and second. Allocation per step was not traced (`allocread.cs`); the branch adds no work to a follower's step, and the measurement game's steps were not traced either.

**The regression pair is measured at P5, not P0.** The guidelines ask for before and after Studios built the same way under comparable load; a baseline taken hours before the "after" is not that. At P5, `main` (52a185c5) and the branch are built outside the repository and run side by side (6671 and 6670), and the follower on `cd-guards-5` is measured in both.

## 2. Design

### 2.1 S2: routine actions commit on one click

The page proposes, and when the result is `NeedsConfirmation` with no listed consequence and no dice, confirms at once with the proposal's correlation, as the tests' `Commit` helper does (`RallyAndFireStepsTests.cs`, lines 124 to 128). The gate is unchanged and still records the confirmation. Routine (ruling Q3): pass, end the move, advance the phase, and a Movement step whose review lists no consequence. A setting in the Play tab, on by default, turns it off (Q4). A refusal still shows as now.

### 2.2 Rules facts the plan needs

- **A stack's steps.** A public planner method, `InfantrySteps(state, movers, from)`, lists each Location the stack could enter from `from`: its MF cost, the MF the stack has left after it, and the refusal if any, from the planner's own `InfantryStep`, `EntryCost`, and `MfAllotment` (all private today). The cost of a route is the sum over its steps, each read from the Location before it. A route is checked against these as it is plotted; each step is still checked by the gate when it is committed, so a step the plot could not foresee is refused there and stops the plan.
- **Whether a DEFENDER unit could fire at a step.** A calculator in Rules, `ScenarioA1DefensiveFireOpportunity`, given for each DEFENDER unit and weapon its facts (Good Order or not, pinned, marked First Fire or Final Fire, Normal Range, LOS to the Location and to the hexside crossed, range, whether a Known armed enemy is closer), answers whether any of them may fire at the moving stack there under A8.1, A8.15, A8.3, and A8.31. The planner gathers the facts (`DefensiveFireOpportunity(state)`) with its own LOS reading (`GamePlanner.Map.cs`, `Los`). **It errs one way only:** when a fact is not known, the answer is "may fire", so the DEFENDER is asked. A wrong "may fire" costs a click; a wrong "may not" would take his fire from him. Concealed and hidden DEFENDER units count; the answer is used only in the DEFENDER's own view (2.4).

### 2.3 S3: plotting the route on the map

In the Movement section, after the stack is chosen, the map shades the Locations of `InfantrySteps` from the stack's Location, each labelled with its MF cost; a click adds it to the route, and the shading moves to the new end with the MF left. The route is drawn on the map with each hex's running MF. Backspace or "Undo last hex" takes the last one off. Bypass is chosen per hex; Assault Movement and Double Time for the route; Dash and Minimum Move stay in the existing form behind a closed line. "Move along this route" commits the plan. A one-hex route is an ordinary step.

### 2.4 S6: the plan runner and the DEFENDER's review

- **Where the plan lives.** A singleton beside `LivePlay`, keyed by game, holding the route, the movers, the declarations, the next step, and the step attempt ids. It is not a game event and does not survive a restart of the Studio; a lost plan leaves the stack where it stands, its window open or its move ended as the game records it, and the moving side plots the rest again. The page reads it, so the page's own reset at a view change (`ResetForView`) does not lose it.
- **The runner.** Each step is the ordinary `move` action, proposed for the moving side and confirmed at once (S2), with an attempt id made from the plan and the step, so a retried step replays rather than repeats. After each step the window is open, and the DEFENDER decides.
- **The DEFENDER's review.** When the plan is committed, the screen is handed to the DEFENDER once. His view shows the step just made and nothing of the route ahead. At a step where `DefensiveFireOpportunity` says one of his units could fire, he chooses **Fire** (the fire panel opens on that Location), **Let it move on**, or **Let it move on to the end** (Q9). At a step where none could fire, the runner passes for him (Q8) and makes the next step; he sees the stack arrive and move on. A pass is proposed as the DEFENDER, as the gate requires (R31.6), and never while Mandatory Fire (A8.312) is owed: then he is asked.
- **What stops the plan and hands the screen back to the moving side:** the DEFENDER fires (after his fire and his pass, the moving side sees the result and continues the plan, changes the rest of the route, or ends the move); a step's own attack (Residual FP, a Fire Lane) or a fire leaves the stack changed (a member broken, pinned, or eliminated); a step is refused at commit; a concealed or hidden unit is revealed; or the route ends, where the runner declares the end of the move after the DEFENDER's decision on the last step (A8.11).
- **The hand-over to the DEFENDER is skipped** only when it is public that none of his units could fire at any step of the route: no DEFENDER unit concealed or hidden, and none of his Known units able to fire at any of its Locations. Then the runner passes for him, and the moving side learns nothing it did not know.
- **Adjudicator view.** The adjudicator sees the route whole and decides for the DEFENDER, as he may pass for him today.

### 2.5 S4: pickers that offer only what the rules allow

The Fire form takes a unit or weapon once; the director list holds only leaders who may direct the group (the fire calculator's own test, asked of Rules); Self-Rally is offered only to a unit that may Self-Rally now; Recover only in the RPh and MPh; and in place of a refusable "end the Rout Phase", a line that the other side still owes a rout, with the hand-over. Each reads the fact from the calculator that refuses it today, so the offer and the refusal cannot disagree.

### 2.6 S1 for the other windows

The DEFENDER's windows outside a movement plan (a vehicle's step, a move that is not plotted) keep today's flow with S1's "Pass" button on the hand-over screen. Vehicles get plans later, on the same pattern.

## 2.7 Memory and performance (the guidelines' design checklist)

The user, 2026-10-21: every code change follows the [ASL Memory and Performance Guidelines](<Memory-And-Performance/ASL Memory and Performance Guidelines.md>) ("we must maintain memory and performance optimization for all code we write"). The answers below are requirements of the tasks, checked in each task's review.

**Work added, and the path that does it.**

| Work | How often | Path and bound |
|---|---|---|
| The banner's parts (S5) | Once a render | Computed once per render and reused by the right pane's turn line (`bannerNow`): the turn's count reads the card, whose file a user card checks for changes, so a second read would add a disk check per render. Fixed in S5 before commit. |
| `InfantrySteps` | When a stack is chosen or a hex is added to the route, not per render | The planner's own `InfantryStep`, `EntryCost`, and the map helpers (`ReadLocation`, `Step`); memoized in `PlayStepMemo` for the state, keyed by the movers (sorted ids), the `from` Location, and the side. The page keeps the last answer for its state and route end and does not ask again on a render. |
| `DefensiveFireOpportunity` | Once per plan step, by the runner, never per render or per plotted hex | For each DEFENDER unit, one LOS read to the step's Location (and the crossed hexside for a Snap Shot) through the planner's `Los`, which reuses map reads; memoized in `PlayStepMemo` keyed by the side, the Location, and the hexside. No full-state scan inside a per-unit loop: the DEFENDER's units are listed once for the call. |
| A plan step | Once per step | The same propose and confirm as a step today, plus the pass the DEFENDER makes today by hand: no added planner work per step; fewer renders, since the hand-over screens per step are gone. |
| The plotted route and the shaded Locations | When the route changes | Drawn through the map's marks (`MapMarks`), not the counter overlay: counters do not change while a route is plotted, so their hex fragments are reused and nothing forces the whole-layer SVG. P4 checks the marks path sends only the marks and records it. |

**The plan store, as a new cache.** Owner: a `MovementPlans` singleton beside `LivePlay`. Key: the game id. Bound: one plan per game; removed when the plan ends, is stopped, the phase changes, or the game is closed or replaced. Lifetime: the process; not serialized into the game. Invalidation: each plan records the revision after its last step; if the game's revision differs when the runner resumes (another circuit or Studio acted), the plan stops and the moving side is told, rather than stepping on a state it did not see. Missing-result policy: no plan means today's flow. Concurrency: one runner per game under a per-game lock; the runner commits events, so it never runs inside a `GetOrAdd` factory.

**Disclosure.** `InfantrySteps` answers for the moving side's view: a refusal that would reveal a concealed or hidden enemy (an occupied Location, for one) is not shown while plotting; the gate meets it at commit, and the plan stops there as the rules would at the table. `DefensiveFireOpportunity` counts the DEFENDER's concealed and hidden units and is used only in the DEFENDER's view and to pass for him; the hand-over is skipped only on public facts (section 2.4). Memo keys carry the side wherever the answer depends on it.

**Evidence.** Making the planner's step cost public is a planner refactor with no rule change: the gate's text-list, replay-digest, and sweep equivalence, with `cd-guards-5`. The new calculator is a fact the rules already imply; its tests cover each rule case. The runner's passes are the same events a DEFENDER's pass writes today.

**Regression measurement.** Before P2 and after P5, in Studios built the same way (Debug, outside the repository), with the measured tab in front: the Play follower on `cd-guards-5`, its ordinary phases and Rout Phase reported apart (counts, medians, 90th percentile, stalls), because S5 to S6 change what every Play render does; and the measurement game's Movement Phase, server time per step and allocation per step (allocation ticks and `allocread.cs`). Acceptance: no ordinary-step or Rout median worse by more than the noise of a repeated pair; the measurement game's per-step server time no worse than a manual step's propose, confirm, and pass today.

### 2.8 S7: a form shows only when it is of use now

The user, 2026-10-21: "No UI workflow should be visible in the UI unless it is immediately useful to the player", naming the building-entry form, the Movement form, the Fire form, and the Support Weapons form ("the player needs to be able to see that in the appropriate phases"). The Play tab, read in my Studio (`ps-3v3-ui`, the German MPh, the mover's view), showed the entry form, the map-exit form for a stack nowhere near an edge, the SMOKE form, and a Fire section telling the mover the Russian side may fire. Each form now shows only for the view, phase, and step where an action in it could be taken; where the reason is a rule, the page asks Rules or the planner, not its own test.

| Form | Shown when (rulings R-PS.1 to R-PS.9, open to the user's veto) |
|---|---|
| Propose: enter a building | Never: a one-unit step into a building that holds an enemy unit goes to the same entry plan (`GamePlanner`, the move's own routing to `PlanEnterBuildingAsync` under `ScenarioA1EntryRules.ReviewedEntry`), and a step into an empty building is an ordinary move, so the form duplicated the Movement form. The action stays for MCP callers; its plan's tests stay in Play (R-PS.1) |
| Hidden units: place beneath "?" | The owner's view, in its own MPh and APh, the phases in which the rule says it must be placed before it moves or advances (A12.32). Placing it at another time is the owner's free choice but never needed there (R-PS.2) |
| Rout | The Rout Phase, when the view has a unit that may rout or owes one |
| Support weapons | Only the actions whose phase the rules allow to the view's side, each shown only when it has a weapon to act on: transfer in the RPh and the ATTACKER's APh (A4.431); drop in the moving side's MPh and APh, and at the start of the CCPh for a holder in a Location with an enemy unit (A4.43); recover in the RPh and the moving side's MPh, when an unpossessed SW lies on the map (A4.44); dismantle or assemble in the side's PFPh or DFPh, for a weapon that can be (A9.8). In the Rout Phase the Rout form's "keep" chooses what a broken unit leaves, so the SW form is not shown (R-PS.3). The phase windows are Rules facts (`ScenarioA1SupportWeaponRules`). Improving the SW workflow itself is backlog |
| Rally, Repair, Deploy and Recombine, DM retention, Shock recovery | The RPh, each only when it has a unit or weapon to offer; Deploy and Recombine for the side whose RPh it is (A1.31, A1.32) |
| Movement | The moving side's view. While the DEFENDER's window is open the mover's view shows the stack's status and a waiting line, not the form (R-PS.4). The DEFENDER's view no longer shows a line saying the other side moves (the banner says so) |
| Leave the map | A stack chosen whose Location lies on an edge its side may leave by; only those edges are offered (R-PS.5) |
| SMOKE grenades, Place a DC | Behind a closed line under the Movement form, opened by a click: a choice the player makes now and then, not a step of every move (R-PS.6) |
| Fire | The firing side's view, with a unit that may fire; in the MPh only while the DEFENDER's window is open. The other side's view no longer shows a section saying who may fire (R-PS.7) |
| Advance | The ATTACKER's view; the DEFENDER's view no longer shows a waiting section (R-PS.8) |
| Fire Lanes and encirclements | Unchanged: they are the state of the map, not a workflow (R-PS.9) |

**Memory and performance.** Each test is a fact the page already holds for the render, or one planner call per render for the state, memoized where it reads the map (the exit edges of one Location, in `PlayStepMemo`). Fewer forms are rendered, so the change removes work.

### 2.9 S8: the Unit Planner tab

The user, 2026-10-21: a new "Unit Planner" icon in the inspector's tab strip; under it a second row of icons holding LOS, Range (moved there), and a new Movement Plan, which plans the routes of every eligible stack and unit and makes the Movement Phase's moves as far as is practical.

**The rules.** A4.2: one stack moves at a time, to the end of its move, before another starts; the moving player chooses the next stack after seeing what happened to the last. Nothing in the rules makes him declare later moves in advance, so a plan for the whole phase is the player's own intention, which he may change at any point; the game makes each stack's move as today's route runner does (S6), stack after stack.

**The tab.** Play adds a "Unit Planner" tab (`ExtraTab`, kept mounted) after Proposal. Its content opens with a second `InspectorTabs` strip (the pattern Play's Setup strip already uses, `IdPrefix="planner"`): LOS, Range, Movement Plan, each with its icon. LOS and Range render the existing `LosPanel` and `RangePanel` with the shared `LosDraft`, so nothing about them changes but where they sit. `BoardInspector` gains a parameter that leaves LOS and Range out of its own strip, set by Play only; Replay and the Board viewer keep them where they are (Q11). `BoardWorkspace`'s rule that keeps a tab open (`HoldPanel`, or LOS, Range, Selection) adds the planner, so a map click does not close it. Two new icons, "planner" and "movement-plan", drawn in the library's style; the icon coverage file is rewritten.

**The Movement Plan control** (the moving side's view, in its MPh; elsewhere it says when it is used):
- A row per stack that may still move (the planner's `MoveCandidates`, the same list the Movement form offers). Units in one Location start as one stack (Q14); unticking a unit splits it into a stack of its own in the same Location.
- Each row: the units, its route in words ("[B9], [B8], [B7]; 1 MF left") or "no route", and Plot, Clear, Up, Down. Plot puts the map in the S3 plotting mode for that row: the same shading, MF labels, Undo, and the same `GamePlanner.Plot`. The other rows' routes are drawn faintly as marks, so the whole phase is visible at once.
- Each route is plotted on the game as it is now (Q12). When its turn comes, it is plotted again on the game as it is then; a route that no longer holds (a hex now holds a Known enemy, a unit broke, the MF changed) stops there and the row is handed back to the moving side with the reason, as a refused step is today.
- "Move every planned stack" makes the routes in the rows' order: each is today's `MovementPlans.Start` for that stack; when its move ends, the next row starts at once (Q13), with no click. A stack the DEFENDER fires at, or one a step changes, stops the queue at that stack as S6 stops a route today; the moving side continues, changes, or ends it, and the queue goes on from the next row. A row with no route is not moved.
- When the last row is made, the control says so and offers "end the Movement Phase"; it does not end the phase itself.

**The DEFENDER.** Unchanged from S6: he is asked only at steps where a Known unit of his could fire, and "let it move on to the end" passes the rest of that stack's route. No phase-wide pass (Q15).

**The store.** `MovementPlans` keeps, beside today's one running plan per game, the phase's queue: an ordered list of (movers, route, Assault, Double Time). Bound: one queue per game, no more rows than units that may move; dropped when the phase changes, the game is closed, or the Studio restarts (a lost queue leaves the game as recorded; it is not a game event). The running plan is the queue's head, so the runner, its disclosure checks, and its revision check (a game moved on since the pause) are reused unchanged.

**Disclosure.** Plots read the Locations as the moving side knows them (S3); the queue lives in the moving side's view only and is never shown to the DEFENDER.

**Memory and performance.** A plot runs only for the row being plotted, when a hex is added (as S3); the other rows' routes are kept as plotted, not plotted again per render. The recheck at a row's turn is one `Plot` call for that row. The marks for all rows go through `MapMarks`, not the counter overlay. No work per render beyond reading the queue.

**Questions, each with a recommendation:**

| | Question | Recommendation |
|---|---|---|
| Q11 | LOS and Range move under the Unit Planner on Play only, or on Replay and the Board viewer too? | Play only: the planner is a play tool; the other pages keep their strip as it is |
| Q12 | Plot each route on the game as it is now and check it again when its turn comes, or plot each route as if the routes before it had been made? | Now, and checked again at its turn. Projecting earlier routes plots through moves that may not happen as planned (fire stops a stack), and costs a replay of every earlier route per hex added. Projection goes to the backlog |
| Q13 | Start the next stack at once when one ends, or ask the moving side each time? | At once; a stop (fire, a changed stack, a route that no longer holds) is where he is asked |
| Q14 | A stack by default is every unit in one Location that may move? | Yes; untick to split |
| Q15 | A DEFENDER's pass for every remaining stack of the phase? | (Recommended no; the user ruled yes, below) |

**Rulings (the user, 2026-10-21):** Q11 to Q14 as recommended. Q15, yes, as discussed: unlike Q10's standing orders, which would decide when to fire, a phase pass only declares that he will not, which the DEFENDER may always do (A8.11 leaves the pause to the players). "Pass for the rest of this phase" stands beside "Let it move on to the end" and on the hand-over screen; it covers every stack and every window of this MPh, plotted or not, vehicles included, except Mandatory Fire (A8.312), which still stops the stack; Residual FP and Fire Lanes attack as always. It is withdrawn ("ask me again") whenever he has the screen, ends with the phase, and is kept in the plan store, not as a game event; if lost, he is asked again. The moving side's view says that the DEFENDER passes for the rest of the phase. Q15a, yes: an exception "still ask me when a stack comes ADJACENT to one of my units", ticked by default, counting only his units the moving side knows of (Known units), since a stop at a concealed or hidden unit would tell the mover something is there; its tooltip says so.

**Tasks.** P10a: the Unit Planner tab, its second strip, LOS and Range moved, the two icons. P10b: the Movement Plan queue, its control, its marks, and the runner's chaining. P10c: the phase pass and its exception.

## 3. Tasks

Each task: change, build, my Studio (the page as the user would play it, at the widths it touches, in both sides' views where disclosure applies), rework until right, then the test methods that read the changed code, named by method.

| Task | What | Done when |
|---|---|---|
| P0 | The measurement game in my Studio; the baseline counted on it | Done 2026-10-21: section 1's "Now" column measured; the regression pair moved to P5 (section 1) |
| P1 | S5 committed with its tests (built) | Committed on the branch |
| P2 | S2: one-click routine actions and the setting | Done 2026-10-21: a step and a pass commit on one click in my Studio (`ps-3v3-p2`), 9 actions a step for 11; a refused step still opens its review; the setting off asks for Confirm. Component tests register `ConfirmEveryAction`, so the tests written for two clicks keep them; `RoutineActionsCommitOnOneClick` tests the setting |
| P3 | Rules and Play facts: `InfantrySteps`, `ScenarioA1DefensiveFireOpportunity` and its gatherer | Done 2026-10-21, in a form that changed from section 2.2: in place of `InfantrySteps` built from the planner's private step costs, `GamePlanner.Plot` asks the move's own plan (`PlanMove`) for each step on the events the steps before it would add, with the DEFENDER's window closed as if he passed, so a plotted route and the gate cannot disagree; while plotting, the plan reads the target Location as the moving side knows it (its own units and Known enemies) and makes no concealment loss that reads the enemy's LOS, and its projections are not kept in the planner's bounded list. Rules' `MfCheck` gives the MF refusal and what is left from one read of each mover's allotment. `GamePlanner.DefenderMayFire` gathers each DEFENDER unit's facts once a state and step (memoized in `PlayStepMemo`, keyed by the step's Location and the Location left) and `ScenarioA1DefensiveFireOpportunity` decides, an unknown fact counting as "may fire". Tests: `MovementPlotTests` (6) and `ScenarioA1DefensiveFireOpportunityTests` (7); the six movement tests that read the MF check pass |
| P4 | S3: plotting on the map | Built 2026-10-21: "Plot a route on the map" in the Movement section; the map shades the Locations the route's end may enter, each with its MF, and draws the route with what is left, as marks kept between renders; a click adds a hex or says why it may not be; Undo and Clear. Checked in my Studio (`ps-3v3-p5b`, g1 [EE10] to [EE7]: 1, 1, and 2 MF, nothing offered past the MF). The marks' labels longer than two characters sit in a box as wide as the text (the user: the circle was too small; "1 MF" keeps its space, at the user's word). 1024 px not yet checked |
| P5 | S6: the plan store, the runner, the DEFENDER's review, the stops, the skipped hand-over | The measurement game's Movement Phase played through; section 1's "After" column measured; the stop rule applied; section 2.7's regression measurement repeated and within its acceptance |
| P6 | S4: the pickers | Done 2026-10-21, each offer asked of the code that refuses it: the planner's `RallyBar` (the attempt's own plan, no roll) holds Propose: rally back with the gate's reason, checked in the Studio (`ps-s4-check`: the British side's second MMC Self-Rally of the RPh held back); `RecoverPhaseBar` (the Recovery plan's phase check, moved out of it) holds Recover back outside the RPh and the unit's own MPh; `PhaseEndBar` (the gate's proposer check) replaces "end the Rout Phase" with the reason while the other side owes a rout, asked once a state and view; a leader ticked as a firer is not offered as director. Not done: a director who has already directed (the fire calculator's fact is not exposed; backlog), Spraying Fire targets, and the fire refusals that need the attack's own facts |
| P7 | S1 for the other windows | A vehicle step's window passes from the hand-over screen |
| P9 | S7: each Play form only when it is of use now (section 2.8) | Done 2026-10-21: walked in my Studio in all three views of the MPh (`ps-3v3-ui`: an interior stack offers no exit, an edge stack only its edge; the mover waits while the window is open; the DEFENDER's Fire target is the moving stack's Location only), RPh (`p31-rph`), PFPh (`p31-pf`, `p31-aph`), RtPh (`p31-rtph`), CCPh (`p31-cc`, drop only for a holder in a Location with an enemy unit), and the DFF window (`p31-dff`). The planner's `SupportWeaponActions`, `MayOfferDeploy`, and `EdgesAt` (memoized per state and Location). 25 test methods that assert the changed behavior; 7 updated to it, and the two that tested the entry plan's review on an empty building through the removed form deleted (the plan's own tests stay in Play) |
| P9b | The demo's findings (2026-10-21) | Done: the DEFENDER's Defensive First Fire section shows only while his window is open or Mandatory Fire is owed (it kept its pass button after the window closed); the moving side's view waits with only the route's progress while a route waits for the DEFENDER (the one-step line and SMOKE showed); and, at the user's word, "View: German" (or Russian, or the adjudicator) at the right end of the map's toolbar, since the "View as" list alone was too easy to miss after a hand-over. Checked in my Studio (`ps-3v3-fix`); 4 test methods that assert those sections, one given the label's assertions |
| P10 | S8: the Unit Planner tab (section 2.9) | Done 2026-10-21 (P10a to P10c): the Unit Planner tab after Proposal, with its own strip (LOS, Range, Movement Plan) and two new icons; LOS and Range keep their panel ids, so the LOS draft and the open-tab rule are unchanged. The Movement Plan's rows, split by unticking, plotted with S3's plot, the other routes dashed on the map, made in order by the store's queue (`RunQueue`, each route plotted again at its turn), "End the Movement Phase" offered at the end. The phase pass in the store (`PhasePass`), applied by the runner and, for a step outside a plan, after its commit (`PassDeclared`); offered in the DEFENDER's section and on the hand-over screen; withdrawn by "Ask me again". Walked in my Studio (`ps-3v3-plan`, `ps-3v3-plan2`): two planned stacks made with one click and one stop, the phase pass from the hand-over screen, the exception asking at [B8] beside the Known unit in [A9], the queue's next stack started on the DEFENDER's "let it move on", the split, the LOS and Range sub-tabs. 16 test methods (the two new: `AMovementPlanMovesEveryPlannedStackInTurn`, `TheDefendersPassForThePhaseAnswersALaterWindow`) |
| P8 | The analysis and the plan closed; the time log; backlog rows for what is left; the gate (`gate.sh ps <base> ...`, a config for this work); a pull request; CI read | CI green on the pull request; the merge is the user's word |

## 4. Build outputs outside the repository

My Studio builds into `E:/Archive/GitHub/dlandi/asl-gate-work/studio-6670`, the folder the user keeps (not registered). The gate registers its own outputs. Any other output made for this work is registered with `outside_outputs.sh register` as it is made, and P8 ends with `outside_outputs.sh list` empty.

## 5. Risks

- **The runner and the gate disagree** on a step the plot allowed: the step is refused, the plan stops, and the moving side sees why. Nothing is committed that the gate refuses.
- **`DefensiveFireOpportunity` says "may not fire" wrongly:** the DEFENDER loses a shot. The calculator errs to "may fire", and P3's tests cover each rule's case; the adjudicator's view lists, for each auto-passed step, why no unit could fire, so a wrong answer is visible.
- **A plan lost on restart:** the game stays as recorded; only the plan is plotted again.
