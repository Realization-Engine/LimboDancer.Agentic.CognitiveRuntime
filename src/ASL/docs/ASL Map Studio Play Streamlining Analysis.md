# ASL Map Studio Play Streamlining Analysis

**Written:** 2026-10-21, at the user's word: the session's focus is streamlining the Play UI so the game is easier to play; this document records the preliminary analysis and the questions for the user.
**Source:** the user's game `dl-tractor-works-01` (The Tractor Works, played 2026-10-20, about 03:26 to 01:40 UTC), its audit log (`src/ASL/boards/units/live/audit.jsonl`, the 1,972 rows from 2026-10-20T03:26 on), and the Play page in my Studio (port 6670, built from `main` at 52a185c5), opened in the Russian view at Turn 3, Russian Movement Phase. Code read: `Play.razor` (`AwaitingView`, `Reasons`, the Movement and Fire sections), `ProposalReviewPanel.razor`, `LocationField.razor`. Nothing was proposed in the game; its file is unchanged.
**Status:** analysis done; its proposals are built by the [ASL Map Studio Play Streamlining Plan](<ASL Map Studio Play Streamlining Plan.md>) on branch `play-streamlining` (S1 to S6, 2026-10-21), whose section 1 has the measurements. The user approved S1 to S5 in general on 2026-10-21 ("I generally approve them all"), open to refinement. Section 7 (movement plans) answers the user's question of the same day; Q6 to Q10 approved by the user that day as recommended ("Q6 to Q10 approved, go with your recommendations"). Q5 and Q10 are in the backlog, section 68.
**Scope:** the Play page only. Every ASL rule stays in the Rules project (the ruling of 2026-10-05); what changes here is what the page offers, how many clicks an action takes, and what the map shows. Where the page needs a fact it does not have (a Location's MF cost, whether a unit may Self-Rally), it asks Rules, as it does now when it proposes.

## 1. What the game shows

The session committed 285 actions. Each one was proposed, stopped by the gate for confirmation (`risk.confirmation_required`, 295 times, counting re-proposals), and confirmed. 119 proposals were refused.

### 1.1 Time before each action

The mean time between one committed action and the next, by the kind of the second, leaving out breaks of 30 minutes or more:

| Action | Count | Mean time before it | Time in all |
|---|---|---|---|
| Movement step (`move`) | 50 | 134 s | about 112 min |
| Fire (`fire`) | 32 | 102 s | about 54 min |
| Advance to the next phase (`advance-phase`) | 42 | 72 s | about 50 min |
| The DEFENDER passes on a moving stack (`pass-fire`) | 51 | 51 s | about 43 min |
| Rout (`rout`) | 24 | 65 s | about 26 min |
| Advance (`advance`) | 34 | 49 s | about 28 min |
| End the move (`end-move`) | 18 | 33 s | about 10 min |
| Rally (`rally`) | 14 | 35 s | about 8 min |
| Others (recover, setup, drop, ambush, close combat) | 17 | 28 to 75 s | about 18 min |

The time includes thinking about the game, which no change removes. What a change can remove is the clicking, the hand-overs, and the refused attempts.

### 1.2 Refusals

| Group | Refusals | What the page let the player do |
|---|---|---|
| Not enough MF for the entry (`play.move-mf`) | 22 | Propose a step the unit cannot pay for; the page does not show MF left beside each Location's cost |
| Other movement refusals: not adjacent, Dash with no road or declared late, upper level, building level, Assault Movement, Minimum Move, stack, occupied, SMOKE target | 22 | Type or pick any hex; nothing on the map says which Locations the stack may enter |
| Fire: director outside the group | 13 | Choose a director the attack cannot use |
| Fire: a unit or weapon named twice | 7 | Add the same unit or weapon to an attack twice |
| Fire: weapons (every MG for Subsequent First Fire and FPF, malfunctioned or used MG) | 9 | Leave out or add weapons the rules require or forbid |
| Fire: Spraying Fire | 4 | Choose Spraying Fire targets that cannot be sprayed |
| Fire: other (fire within the first CA, Final Fire at a non-adjacent target, Subsequent First Fire range, out of range, target, Prep Fired, Fire Lane, a Dummy as firer, berserk group) | 23 | Choose a target or firer the fire calculators refuse |
| Rally: Self-Rally | 7 | Ask a Self-Rally of a unit that has no Self-Rally, is Disrupted, has a leader present, or whose side used its one MMC Self-Rally |
| Advance: no MF allotment after portage (`play.advance-mf`) | 7 | Propose an advance for engineers whose portage leaves none; three of the seven were the same unit asked again |
| A move while the DEFENDER's window is open (`play.move-window`, `pass-window`, `fire-window`) | 8 | Propose the next step before the DEFENDER passed |
| Recover outside the RPh and MPh | 4 | Offer Recover in a phase it is not allowed in |
| End the Rout Phase before the other side routs (`play.not-your-action`) | 4 | Offer "end the phase" while the other side still owes a rout |

The groups overlap where a refusal gives several reasons; the totals are by refusal, not reason.

## 2. The DEFENDER's window after each step

Every Movement step opens the DEFENDER's window (A8.1, A8.11). `AwaitingView` then names the DEFENDER's side, and in a shared-screen game the page shows the hand-over screen. As read from the page and the code, one hex entered takes:

1. The moving side: choose the units (or keep them), pick the Location, "Propose: move", Confirm.
2. The hand-over screen: "I am the German side: show my view".
3. The DEFENDER: "Propose: the DEFENDER passes", Confirm.
4. The hand-over screen: "I am the Russian side: show my view".

That is about eight clicks a step, of which only the pick and the DEFENDER's decision are play. In this game the DEFENDER passed 51 times and fired a handful; the 51 passes alone took about 43 minutes. The exact click count is to be measured in the Studio before any plan (section 5).

## 3. The other sources of friction

**3.1 The move is a form.** The Movement section is a column of fields: Units moving, To, Minimum Move, Bypass, Dash to, Infantry OVR, Assault Movement, Double Time, Leave by, and several Propose buttons. "Pick on the map" fills the To field; the map does not shade where the stack may go or say what an entry costs. The player learns a step's cost from a refusal (44 movement refusals; 134 s a step).

**3.2 Confirm on routine actions.** Passing, ending a move, and advancing the phase roll no dice and have no consequences to review, yet each is a Propose and a Confirm. In this game they were 111 of the 285 actions.

**3.3 Pickers offer what the rules refuse.** The Fire form accepts a unit twice and any director; Rally offers Self-Rally to every unit; Recover and "end the Rout Phase" are offered when they will be refused.

**3.4 Layout (not yet examined).** The actions pane is a single tall column beside the map; at the pane size I used, the Movement section filled it and Fire was below the fold. The play-ui preferences of 2026-10-04 (sections collapsible, the phase's section open) hold; whether the pane itself needs a different shape is left for after S1 to S4.

## 4. The proposal

In the order recommended, each a Play-only change of its own:

| # | Change | What it should remove |
|---|---|---|
| S1 | The hand-over screen for a DEFENDER window gets a "Pass" button that passes at once, without opening the DEFENDER's view; "Fire" opens the view as now. The screen then hands back to the moving side with no second confirmation of the view. | About five of the eight clicks on most steps; most of the 43 minutes of passes |
| S2 | Routine actions with no dice and no consequences (pass, end the move, advance the phase, a Movement step with no consequence) commit on one click. The governed gate still requires and records the confirmation; the page supplies it. | About 150 of the 295 confirmations in this game |
| S3 | Map-first movement: selecting a stack shades the Locations it may enter, each with its MF cost, from the facts Rules already gives the planner; a click on a shaded Location makes the step. A Location it may not enter shows why on hover. The form stays for Bypass, Dash, Assault Movement, and the rest, behind a closed line. | Most of the 44 movement refusals and much of the 134 s a step |
| S4 | Pickers offer only what the rules allow: a unit or weapon once per attack, only directors who may direct the group, Self-Rally only to a unit that may Self-Rally now, Recover only in its phases, and in place of a refusable "end the Rout Phase" a line saying the other side still owes a rout and a hand-over button. | About 40 refusals |

| S5 | The header bar and the right pane (the user, 2026-10-21, from an annotated screenshot of `a3-gambit-rout`): the "Review: ..." button moves to the right-hand end of the header bar; the "Working: ..." label moves to its left; the Turn and Phase ("Turn 1 \| German Rout Phase", without the game's name) are copied to the top of the right pane, under its tabs, and stay in the header bar too. The copy stays in sight when the pane scrolls and shows in every tab (both confirmed by the user the same day). | The eye's trip from the right pane to the header to learn the phase; Review and Working sit where the actions are |

Each would be measured against this game before and after: clicks a step, refusals, and the time of a Player Turn played from the same position in my Studio.

## 5. To measure before a plan

- The clicks of one Movement step with a DEFENDER pass, counted in the Studio, in place of the reading of section 2.
- How many of the 51 windows had a DEFENDER unit that could have fired at the moving stack (for question Q2).
- Whether the seven `play.advance-mf` refusals of engineers under portage are right under A4.7 and A4.72, read from the rulebook; if not, it is a rules fix and goes to the backlog, not into this work.

## 6. For the user to rule on

| # | Question | Recommendation |
|---|---|---|
| Q1 | Which of S1 to S4 to take, and in what order? | All four, in the order S1, S2, S3, S4: S1 and S2 are small and remove the most clicks; S3 is the largest. |
| Q2 | Should the page skip the DEFENDER's window entirely when no DEFENDER unit could fire at the moving stack? It would remove most remaining hand-overs, but it tells the moving side that no hidden or concealed unit sees that Location, which it would not learn at the table. | No: take S1 alone. Revisit if the measurement of section 5 shows the windows with no possible fire are the large majority. |
| Q3 | In S2, which actions count as routine? | Pass, end the move, advance the phase when no consequence is listed, and a Movement step whose review lists no consequence. Fire, rout, rally, setup, and anything with dice or a listed consequence keep Confirm. |
| Q4 | Should S2 be a setting the player can turn off, so a player who wants to review every action still can? | Yes: one setting, on by default, in the Play panel's view section. |
| Q5 | In S3, should the shaded Locations also show the DEFENDER's possible fire (the hexes in LOS of Known enemy units)? | Not in this work: it is a separate overlay with disclosure questions of its own; add it to the backlog. |

## 7. Movement plans and the DEFENDER's fire (the user, 2026-10-21)

The user's question: in a game of three German units against three Russian units, all in each other's LOS, the German moves all three; ideally the Russian would see all of that movement before choosing what to fire at. Could every player move each unit one hex at a time along a plan, and could a player plot a unit's whole move so the game carries it out, stopping only where the DEFENDER may fire?

### 7.1 What the rules say

Read on their pages in the registered rulebook (eASLRB v3.01, physical pages 47 to 62), after a search of all 716 pages for "Opportunity Fire", "one at a time", "before another", and "must complete":

- **The term.** Fire at a moving unit is **Defensive First Fire** (A8.1), with Subsequent First Fire and FPF (A8.3, A8.31), Snap Shots (A8.15), and Residual FP (A8.2). **Opportunity Fire** (A7.25) is something else: the ATTACKER's Infantry, placed under a Bounding Fire counter in its PFPh, neither fires then nor moves, and fires in its AFPh at full FP. The 60 hits for the term in the book are all A7.25 and its cross-references.
- **One stack at a time, to the end.** A4.2: the ATTACKER states the MF spent in each hex; "Once a unit moves, stops, and another unit moves, the original unit may move no farther during that MPh"; all members of a moving stack "must end their MPh before another unit not in that stack may move". Moving every unit one hex in turn is not allowed.
- **The DEFENDER decides at each MF expenditure, not after all movement.** A8.1: "Anytime a unit/stack expends MF/MP in the LOS of one of his units, the DEFENDER has the option to temporarily halt its movement while he fires at it in that Location." "Once another unit begins movement or the MPh is declared over, previously moved units are no longer subject to Defensive First Fire attack." A8.11: the fire "must be resolved before the moving unit/stack leaves the intended target Location"; the DEFENDER may not ask for a unit to be moved back; the ATTACKER "must give the DEFENDER ample opportunity (as previously defined between the players) to declare his fire before moving on", and declares the end of a unit's movement, at which point it may be fired on as moving.
- **A unit that fires is spent for the phase**, except Subsequent First Fire (Area Fire, only within Normal Range and no farther than the closest armed Known enemy unit) and FPF (A8.1, A8.3, A8.31). A unit with no effect from a shot may be fired at again in the same Location only by other firers, or after a second MF there (A8.14).

So the rules deliberately deny what the user called ideal: the Russian must choose whether to fire at the first German stack, in each Location it enters, without knowing what the other two will do. Holding fire for a better target is the DEFENDER's central gamble in ASL. Residual FP (A8.2) is how fire at one stack can still affect the stacks that follow. A variant in which the DEFENDER sees every move first would be a house rule that changes the game's balance; it is not proposed.

The rules do not forbid what the user's second idea needs. A player may decide a stack's whole route in advance; A4.2 asks only that the MF be stated hex by hex, and A8.11 leaves the form of the DEFENDER's "ample opportunity" to the players. What a plan must not do is show the DEFENDER the hexes ahead of the one he decides on.

### 7.2 What the page does now

Each MF expenditure is a step: the moving side proposes and confirms it, the engine opens the DEFENDER's window (`GameProjector.cs`, line 2275, sets `WindowOpen` on every step), the screen is handed to the DEFENDER, who proposes and confirms a pass or a fire, and the screen is handed back. The window opens whether or not any DEFENDER unit sees the Location; A8.1 owes the DEFENDER the option only where one of his units has LOS. In the user's game that was 51 passes, about 43 minutes.

### 7.3 The proposal: S6, a movement plan with one hand-over

1. **The moving side plots the stack's route** on the map: each Location in turn, with the MF of each entry from Rules and the MF left, and the declarations that belong at the start (Assault Movement, Dash, Double Time, Minimum Move) or at a hex (Bypass). A route the stack cannot pay for, or a hex it may not enter, is refused while plotting, not after a confirm (S3's map-first movement is the plotting). Confirming the plan commits nothing yet.
2. **The screen goes to the DEFENDER once.** His view shows the stack's first step only. At each step, if any of his units could fire at it there (LOS, not yet spent, and the Snap Shot hexside), he chooses Fire or Let it move on; a step where none of his units could fire passes without asking. The next step is shown only after he has decided on this one, so he never sees the route ahead (A8.1, A8.11). Skipping the steps he cannot fire at tells the moving side nothing, since the moving side is not looking and the route goes on either way.
3. **Fire stops the plan.** The fire is resolved at that step. The screen then returns to the moving side, who sees the result and continues the plan, changes the rest of the route, or ends the move there (a pinned or broken stack stops by rule). A continued plan goes back to the DEFENDER from the next step.
4. **The plan stops by itself** on anything that needs a decision or that the moving side could not know when plotting: a concealed or hidden unit revealed, an enemy unit in a Location, a mine or other attack, a stack broken or pinned, MF changed.
5. **The end of the move is the last step the DEFENDER sees** (A8.11), so he may fire at the stack where it stops.

For the user's three-against-three, with routes of four hexes: today about 12 steps of eight clicks and two hand-overs each (24 hand-overs); with S6, two hand-overs per stack (6), plus two for each time the Russian fires.

**What it changes in S1 to S4.** S6 replaces S1 for movement: the DEFENDER's single review is the pass button, and the window per step is gone. Q2 is answered inside the DEFENDER's view, where skipping costs no secrecy; the hand-over itself is skipped only when it is public that no DEFENDER unit could fire (none hidden or concealed, and no Known unit in LOS of any hex of the route), since then the moving side learns nothing it did not know. S2's routine confirm covers the plan's own commit. S3 becomes the plotting step. S4 is unchanged.

**What it needs from Rules and Play.** The step itself stays a `move` action, so every rule check is the one it is today; the plan is a sequence of them made by the page and the gate, not new rule logic. New: whether a DEFENDER unit could fire at a step (LOS, spent or not, Subsequent First Fire's range limits) as a fact Rules answers, and a pending plan in the game state that the DEFENDER's view reveals one step at a time. Vehicles (MP, VCA changes, Stop MP, Motion) come later on the same pattern.

### 7.4 Questions

All five approved by the user on 2026-10-21 as recommended.

| # | Question | Recommendation |
|---|---|---|
| Q6 | Keep A4.2 and A8.1 as written: one stack moves to its end before the next, and the DEFENDER decides at each step without seeing the rest? | Yes. A "DEFENDER sees all movement first" variant is a house rule that removes ASL's central DEFENDER decision; not offered. |
| Q7 | Take S6, the movement plan with one hand-over per stack, Infantry first? | Yes, in place of S1 for movement. |
| Q8 | In the DEFENDER's review, skip the steps where none of his units could fire, without asking him? | Yes: it reveals nothing to the moving side, and it is where most of today's passes go. |
| Q9 | Offer the DEFENDER "Let it move on to the end": decline every remaining step of this stack at once? | Yes, as a second button: it is his choice at each step anyway, taken once. |
| Q10 | Standing orders for the DEFENDER (for example "fire with this squad at anything entering open ground within three hexes") so the engine fires without asking? | Not now: each order is a rules question of its own (which firer, which target first, Subsequent First Fire). Backlog. |
