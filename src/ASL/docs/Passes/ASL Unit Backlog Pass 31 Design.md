# ASL Unit Backlog Pass 31 Design

**Status:** Pass 31 is built, reviewed, and checked in the Studio (2026-10-04, branch `feature/asl-backlog-pass-31`); section 14 says what was built and where it differs, and the [review document](<Scenario A1 Backlog Pass 31 Review 2026-10-04.md>) has the reviews. What was pass 31b is designed here (section 5) and not started; on 2026-10-04 the user gave the number 31b to the Replay page ([its design](<ASL Unit Backlog Pass 31b Design.md>)), so section 5 is now pass 31c, and where this document says "pass 31b" or "31b.N" of that work, read 31c. Approved by the user on 2026-10-04 with the answers of section 12. Pass 31 (Play-test UI) of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 5, added by the user on 2026-10-04.

**Date:** 2026-10-04

**Related documents:** the pass 30 handover prompt (`ASL Pass 30 Handover Prompt.md`, deleted 2026-10-05; in git history at 038dca8), whose standing rules and harness lessons apply unchanged; the [pass 28c design](<ASL Unit Backlog Pass 28c Design.md>) (the Play workspace) and the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>) (the shared board workspace); the [pass 30b design](<ASL Unit Backlog Pass 30b Design.md>) and its [review](<Scenario A1 Backlog Pass 30b Review 2026-10-03.md>); the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 44 to 47; the plan's sections 15.9 to 15.11. The play test's report and screenshots are the user's files and are not in the repository.

**What the pass is.** On 2026-10-03 and 2026-10-04 Claude in Chrome played one live game of The Guards Counterattack from setup to its end through the Play page's own controls, both sides, with the hand-over between them. Its report lists 31 problems and 8 possible rules errors. This pass makes the Play page fit to play such a game: no legal action is blocked or lost, a proposal says what it will do, the game says when it ends and who won, and the page holds still and speaks in the player's words. It ends with a second play test.

Unlike the Studio passes before it, this pass changes the game too, so it adds rulings (R31.1 onward, section 9).

**The rulebook comes first** (the user, 2026-10-04). The check of the possible rules errors is the pass's first task, not its last. Every task after it reads the rulebook PDF's passages for what it touches before any code changes, cites them in its commit and in the review, and brings a difference between the rulebook and the code to a ruling. A citation in this design that the PDF does not bear out is corrected here before its task is built.

## 1. The four sources

| Source | What it is | What it is used for |
|---|---|---|
| The report | `Playability-Report-2026-1004.md`: problems P-01 to P-31 with steps, possible rules errors R-01 to R-08, what worked, the controls not exercised | The list of work |
| The screenshots | 92 images in five folders, 52 of them from Turn 1 | The layout findings only (section 2) |
| The event log | `src/ASL/boards/units/live/<tenant>/guards-dl-01.game.json`: 613 events, revisions 1 to 613 | What the game did; it decides where the report and the page disagree |
| The audit log | `src/ASL/boards/units/live/audit.jsonl`: every proposal the gate handled; rows matched to the game by time | The refusals, with their exact reasons |

Both logs are live Studio data under `src/ASL/boards/`, which is never staged.

## 2. What the logs and the screenshots add to the report

- **The game.** Setup closed at revision 58; play ran from 02:50 to 05:04 UTC, 2 hours 14 minutes for five Game Turns. The gate confirmed 218 proposals and refused 21.
- **The refusals, by count:** the Close Combat "stacking" refusal 11 times (P-04), "this unit may not Self-Rally" 4 times (R-04), the Defensive First Fire MF limit 3 times (P-05), the upper-level advance 2 times (P-03), two leaders in a target hex 2 times (P-02), and nine others once each.
- **P-01 is in the event log.** In the Turn 4 German MPh, `g-squad-9` breaks at revision 437 and the next event is the phase change at 442. No `movement-window-closed` and no `movement-ended` was written. The same shape appears in Turns 2, 3, and 5.
- **The result is stored.** The `game-ended` event carries the winner, the reason, and its facts (Control of nine buildings, each side's CVP and unbroken squad-equivalents). The page does not show it outside the closed card panel.
- **R-02 is closed as a misreading.** `r-squad-1` failed with a final 12 from an original 11 (6, 5, plus 1). The only original 12 in the game (`g-squad-3`, Turn 1) was reduced to a half-squad.
- **R-03, R-05, P-16, P-18, and P-21** are in the record as the report says.
- **The tester's window** was about 1568 by 677 pixels: the three-pane layout in a very short window. The header took about 130 pixels of the 677. The workspace's least height is 32rem (512 pixels), so the page itself scrolled, and what scrolled up went under the sticky header. That is most of P-28 and P-29.

Only two screenshots were opened for this design. More are opened task by task in the build, for P-06, P-15, P-27, P-28, P-29, and P-30.

## 3. What the code does now

Three read-only surveys of the code found a cause for every problem but one part of P-31 (the header kept a cancelled proposal's name; no cause found, to be reproduced at the build). In short:

| Problem | Cause | Where |
|---|---|---|
| P-01 | A broken, pinned, or eliminated mover leaves the move open with no members. The DEFENDER's pass button is drawn only when the stack has a first member. The planner's pass would end the move; the button cannot be reached. | `Play.razor` (`MovingAt`), `GameProjector.KeepMovingStack`, `GamePlanner.Movement.PlanPassFire` |
| P-02 | The Fire package refuses any attack with more than one leader among its targets. The checks for several leaders are mostly written; they were never reviewed. | `ScenarioA1FireCalculator` |
| P-03 | The Advance list is built from a helper that returns ground level only. The planner already allows the same level of the same building and a stairwell. | `Play.razor` (`AdvanceDestinations`), `GamePlanner.CloseCombat.Neighbors` |
| P-04, P-25 | The form stacks every SMC, prisoners included, with its side's first MMC, and forgets the stacking after each round. Prisoners are offered as attackers and targets. One refusal text serves four different checks. | `Play.razor`, `ScenarioA1CloseCombatCalculator`, `RefusalReasons` |
| P-05 | The MF limit counts every fire record in the whole game whose step number matches the moving stack's, and step numbers restart at 1 for each stack. `g-squad-4` had fired at a first step in Turn 1. | `GamePlanner.Fire` |
| P-06 | A proposal's reasons are plain strings with no kind. The heading reads "Every check passed" for every proposal that needs confirmation. | `GamePlan`, `ProposalReviewPanel` |
| P-07 | A proposal carries no side. The gate has one principal, `studio-user`. Six panels list units of both sides; the phase end is offered to every view; the Close Combat attack list has no owner. | `GamePlanner`, `LivePlay`, `Play.razor` |
| P-08 | The result is drawn only inside the card panel, which starts closed. The header gets a yes or no for "ended". The turn limit is read by the planner and never shown. | `PlayCardPanel`, `GameEndNotice`, `PlayContextHeader` |
| P-09 | The planner refuses other actions while a choice or surrender waits, but only when one is proposed; the page disables nothing. | `Play.razor` |
| P-10 | The order of choice is a free-text field of unit ids, shown only after "capture attempt" is ticked. | `CloseCombatAttackBuilder` |
| P-11 | LOS to a Bypassing stack's vertices is not built (ruling R10.7). | `GamePlanner.Fire` |
| P-12, P-13 | A blocked LOS and a Melee in the target Location are facts or nothing; neither is a warning. | `GamePlanner.Fire`, `FireReviewFacts` |
| P-14 | The Fire package can give FP, column, and DRM before the dice (`Preview`); only Encirclement uses it. The planner forms fire groups across Locations; the panel has one "From". | `ScenarioA1FireCalculator`, `SmallArmsFirePanel` |
| P-15 | "Latest" covers six record kinds and gives fire as one sentence with no result. Records sits in a row that takes height from the map. A record's heading has the turn and phase but not the side. | `Play.razor` (`LatestRecord`, `WhenOf`), `site.css` |
| P-16 | The Effect column prints a target's final states, not what the attack changed. | `FireText.EffectText` |
| P-17 | The proposal's headline lists unit ids only. Rate of fire kept is computed and never shown. | `GamePlanner.Fire`, `FireResolutionCard` |
| P-18 | No function names a unit. A replaced unit's id is the attempt's id plus its old id. About 150 to 180 places print a raw id. Reasons are printed with their codes. | `Play.razor`, the components, `RefusalReasons.Explain` |
| P-19 | Broken leaders are listed as ralliers on purpose; the refusal's text is the generic one. Dismantle is enabled for any held SW. | `Play.razor`, `SupportWeaponActionPanel` |
| P-20 | Nine typed Location inputs are bare text fields without the "Use <hex>" button that `LocationField` has. Outside setup, a hex picked on the map fills nothing. | `RoutActionPanel`, `SmallArmsFirePanel`, others |
| P-21 | Not yet traced: a record names a unit to a side only when the side could name it just before the event (plan section 15.9), and the Deploy record of Turn 1 does not follow it. | `Play.razor` (`RallyAndRepairRecords`) |
| P-22 | The Advance list leaves out concealed units; the planner allows them. | `Play.razor` (`AdvanceCandidates`) |
| P-23 | Every step opens the DEFENDER's window. Nothing lets the DEFENDER decline ahead. | `GameProjector`, `PlanMove` |
| P-24 | The rout search runs again for every broken unit on every render, again at Propose, and again at Confirm. Low Crawl searches a 24 MF radius twice. Nothing is kept between calls. | `GamePlanner.Rout`, `Play.razor` |
| P-26 | A leader created in Close Combat has no recorded value for berserk, captured, or Melee, so Good Order reads "unknown" and he is not offered as a director. Backlog section 45 has the general row. | `GamePlanner.CloseCombat`, `GameState.GoodOrder` |
| P-27 | Melee is in the list of conditions that are not drawn. | `GameTypes`, the style sheets |
| P-28 | The header grows when a proposal waits, and the workspace is sized from the header's height, so every pane resizes. At 1024 to 1439 pixels the review row grows with the proposal. A new proposal scrolls the page to the review. | `PlayContextHeader`, `playWorkspace.js`, `site.css` |
| P-29 | The document scrolls, and the card panel and the units table sit outside the workspace. | `Play.razor`, `site.css` |
| P-30 | The hand-over unmounts the map, so the next view starts fitted. A pane that changes height while the view is fitted fits again. The Residual FP disc is drawn over the counter's center and takes its clicks. The map has no loading sign. | `HandOverScreen`, `boardViewport.js`, `GameMaps` |
| P-31 | The header keeps the last proposal's name after a commit, by design; it should show it only while a proposal waits. | `Play.razor` |

## 4. Decisions, pass 31: nothing blocks play

**D0. The referee's check comes first** (task 31.1). Each of R-01 and R-03 to R-08 is checked against the rulebook PDF before any code changes. A confirmed error becomes a ruling and a fix with its test, built in the task that owns its code; a cleared one is recorded in the review with its citation. R-02 is closed (section 2). R-06 (Height Advantage shown for a building's upper level) and R-07 (an original 12 in Close Combat) are expected to need a fix; R-01 (a Guards squad Deploying with no leader) and R-08 (an unarmed leader's CC strength) are expected to need a citation or a ruling. The same task reads the passages behind D2, D6, D8, and D9 (A8.1, A8.3, A9.2, A10.2, A10.21, A7.8, A11.14, A20.22, A10.63, A18.11), so their tasks start from the rulebook's words.

**D1. A proposal says who proposes it** (P-07; backlog section 44's row on the phase end).

- The page sends the view with each proposal: a side, or the adjudicator.
- The planner refuses an action a side may not take, with a reason that names the side that may: a unit's action belongs to the unit's side; the DEFENDER's pass to the DEFENDER; a choice or a surrender to the side named; the phase end to the phasing side (in the RPh, RtPh, and CCPh, where both sides act, to the phasing side once the other has nothing left that it must do, as now).
- The adjudicator may propose anything, as now. Tests and saved games default to the adjudicator, so no saved game changes.
- The pickers of Rally, Rout, SW, Repair, Recombine, and building entry list the viewing side's units only. The phase end is shown to the view that may end it; the other view reads who ends it.
- A declared Close Combat attack carries its side. A view lists the other side's attacks without "remove", and only once its own are declared (D8).
- Hot-seat play still rests on the hand-over. This guards against a slip, and it stops a view's lists from showing the other side's choices.

**D2. A move that fire stops is closed by the DEFENDER's pass, or by the game** (P-01).

- When Defensive First Fire leaves a moving stack with no member able to move, the DEFENDER's block still shows where the stack is, with its pass. The pass ends the move, as the planner already does.
- When every mover is eliminated, the fire's own plan closes the window and ends the move, since nothing is left to fire at.
- A broken or pinned mover stays a target while the window is open (A8.1), so the window is not closed for the DEFENDER.
- The moving side's view says why its list is empty: "The move ended: <unit> broke under Defensive First Fire. The DEFENDER may still fire at it, or pass."

**D3. A waiting choice comes first** (P-09). While a choice or a surrender waits, the page shows its panel and the hand-over, and every other Propose button is disabled with the note "Answer the choice first." Records gain a line for a resolution that waits on a choice ("Fire at F5 waits for the German side's choice"), so the cause is on the page before its answer.

**D4. The last turn and the result are in the header** (P-08; the user's note of 2026-10-04 and answer 8).

The game already computes its result from the card's Victory Conditions; nothing is deferred to a pass 31c. What the code has:

- `ScenarioVictory.Evaluate` reads a card's structured Victory Conditions (`victoryConditions.outcomes` and `otherwise`) over the game's states. It knows six kinds of condition: a margin of buildings Controlled, a count of a building's hexes Controlled, a ratio of unbroken squad-equivalents, the only unbroken units in a building, Exit VP, and CVP. It returns the Control of each building, each side's CVP, Exit VP, and unbroken squad-equivalents, a result that holds at once (for an outcome marked immediate), and the result if the game ended now.
- The planner uses it twice: after every commit, to end the game at once when an immediate outcome holds (`WithImmediateVictory`, reason "victory"), and as the last phase of the last Game Turn ends (ruling R21.4). Either way the `game-ended` event stores the winner, the reason, and the facts.
- The four built-in cards all have structured conditions: The Guards Counterattack (a margin of two buildings, or three times the unbroken squads, else German), Gambit and Armor Test (Exit VP at once, else the other side), The Tractor Works (six hexes of X3, or the only unbroken units there, else a draw).
- In `guards-dl-01` the stored result is a German win: the Russians Controlled none of the five German buildings and lost none of their four, and had 14 unbroken squad-equivalents against 8, so neither Russian condition held.
- A card with no structured conditions (a user's card with text only) ends with no result stored. That is the one case with no definitive result, and the page says so.

So the gap is in the page and in the wording of the reason, and the pass closes both:

- The header reads "Turn 4 of 5". In the last Game Turn it adds "last turn", and the proposal that ends the last phase is a consequence (D5): "This ends the game."
- Once the game has ended, the header shows the result in every view: "German win", with the reason beside it and "Why" opening the account. The actions pane shows the same notice in place of "its result is above". The card panel keeps its copy.
- **The account lists every condition, held or not.** `Evaluate` gains, beside each outcome, why each of its conditions holds or fails, with its numbers: "Russian needs a margin of 2 buildings: Controls 0 of F5, K5, I7, M7, M9 and lost 0 of N4, J2, M2, F3: not met. Russian needs 3 times the unbroken squad-equivalents: 14 against 8: not met. So the German side wins (A26.3)." Today a condition that fails says nothing, and the reason is only "no Victory Condition of the other side holds".
- During play the same account is the standing: each side's view reads it as that side may know it (A26.15; ruling R23.4), as the standing table does now.
- A draw reads "A draw", with its account. A card with no structured conditions reads "The game has ended. This card's Victory Conditions are text only, so the game does not decide the result", with the card's text.
- After the end the hand-over is not needed to read the result. The views stay as they are: "?" and hidden units stay hidden to the other side, and the adjudicator's view shows everything.

**D5. A proposal lists its consequences apart from its checks** (P-06, P-12, P-13).

- `GamePlan` gains a list of consequences, each with a kind: loss (a unit eliminated, captured, or surrendered by the proposal), waste (a shot with every LOS blocked), friendly fire (a target Location holding the proposer's own units or prisoners), and end (the game ends).
- The review's heading is then "This will eliminate r-squad-20 and r-squad-21" in the warning style, with the consequences first. Confirm is labelled for what it does ("Confirm and lose 2 units") and does not take the focus; a routine proposal is as now.
- Failure to Rout, Melee eliminations at a phase end, and the game's end are the first consequences; the kinds are a closed list the planner fills, not text the page parses.

**D6. Fire: five corrections** (P-05, P-02, P-16, P-26, R-05).

- **The MF limit** counts the firer's attacks on this moving stack in this Location in this Movement Phase, a weapon apart from the unit that fires it. The MF spent there are rounded down, with a least of one attack a hex, as A8.3 and A9.2 say ("FRD, but a minimum of once per hex"), so the present rounding stays. Ruling R31.1.
- **Two leaders in a target Location.** The Fire package's order of checks for several leaders, the modifier each takes, and Leader Loss with more than one leader are reviewed against A10.2 and A10.21, tested, and the refusal removed. A leader whose broken Morale Level the catalog lacks keeps its own refusal. Ruling R31.5.
- **The Effect column** shows what the attack changed. A target that was already broken or pinned reads "already broken".
- **A created unit's conditions.** A unit created in play (a created leader, a replacement, a Deployed HS) has broken, berserk, captured, and Melee recorded, so its Good Order is known. Older games read as before (backlog section 45's row, for new events only).
- **A pinned firer's SW** is halved as its own FP is, if the referee's check confirms it (A7.8). Ruling R31.3.

**D7. Advance** (P-03, P-22). The "To" list holds the Locations the planner would accept for the units ticked: the same level of an ADJACENT hex of the same building, a stairwell's other levels in the hex, and ground-level neighbors for units at ground level. Concealed units of the viewing side are listed.

**D8. The Close Combat form** (P-04, P-25, P-10).

- A prisoner is not a combatant: not an attacker, not a target, no Infiltration, no stacking select.
- Ticking an MMC as a target also ticks the SMC stacked with it, and says so. The stacking is the game's own reading of the Location and is kept from round to round.
- A unit already in a declared attack is not offered again.
- The four checks behind "stacking-outside" get four texts, each naming the unit and the attack.
- A capture attempt on several units shows the defenders in an ordered list, to be put in order by its owner's side.
- A proposal made from one view says whether the other side has declared.

**D9. The Rally Phase** (P-19, R-03, R-04).

- Broken leaders are not offered as ralliers.
- "Dismantle or assemble" is enabled only for a weapon the game dismantles, in a phase that allows it.
- "May not Self-Rally" becomes three texts: the unit's kind may not; the side's one MMC Self-Rally of this Player Turn is used (naming the unit that used it); a leader in the Location must rally it.
- The referee's check (section 13) cleared both doubts: a leader-directed rally does use up the allowance, and a leader's Self-Rally does take the +1. Only the texts change.
- **Deploy.** Russian squads may not Deploy (A25.2), Guards squads included; "Guards" in A1.31 and A1.32 are the Guards of prisoners (A20.5). Ruling R31.4, which corrects ruling R13.4.

**D10. The hand-over screen says what is going on** (the user's answer 3, 2026-10-04). Today the screen is the same text for every hand-over. It becomes an account of the moment, made only of what both sides know, since the side leaving and the side arriving both see it.

- **Who and when.** "Hand the screen to the German side", then "Turn 4 of 5, Russian Movement Phase", and "last turn" when it is.
- **Why now.** One sentence from the reason the game waits (the page's `Awaiting`, pass 28c): "A Russian stack entered I3 for 1 MF. The German side, as the DEFENDER, may fire at it or pass (A8.1)." Others: a choice to answer, a surrender to accept or refuse, the other side's rallies in the RPh, its routs in the RtPh, its Close Combat attacks to declare, Defensive Fire, the phasing side's next phase, a setup to make, a game just opened.
- **What the arriving side will be asked to do,** as a short list: "Fire at the stack in I3, or pass", "Answer: keep the 9-1 or Battle Harden", "Rally your broken units, then hand back".
- **What happened since that side last had the screen,** in the words both sides may read: the phases that passed, the moves, the fire and its results, the routs, the Close Combat. A line is shown only when both sides' views read the same record with the same text; anything one side reads differently is left for the view itself (D17's "Since you last looked", in pass 31b, shows the rest after the confirm). The count of lines left out is not shown.
- **Where.** A map of the terrain alone, with no counters, outlines the Locations those lines name, so the arriving player looks at the right part of the board before the view opens. After the confirm the map opens on that part.
- **The score both sides know:** each side's CVP and Exit VP. Control and the unbroken squad counts are left out, since each side may read them differently (A26.15).
- **At the game's end** the screen is not shown; the result is in every view (D4).
- The confirm button and its focus are as now. The screen still renders nothing of either view: no unit layer, no panel, no draft.
- The rule for a line ("both views read it alike") is one function with its own tests, and the referee's review reads the screen in a game with concealed and hidden units on both sides.

Fewer hand-overs in the Movement Phase (P-23) are not built: the hand-overs stay as they are, at the user's word, and the row stays in the backlog.

## 5. Decisions, pass 31b: the page reads and holds still

**D11. A unit has a name** (P-18). `DisplayText.Unit` gives every unit the same name wherever a player reads it: its printed values, its kind, and a tag that never changes, such as "4-6-7 squad G4" and "9-2 leader G2".

- The tag is the side's letter and a number that runs within the side and the kind, given in the order the units entered the game. A replacement, a reduced unit, a Battle Hardened leader, and a Deployed HS keep the tag of the unit they came from (an HS adds "a" or "b"); a created leader takes the next number.
- So `fire-a672887feaab-g-squad-1` reads "4-4-7 squad G1", and `choose-6a4667fb0056-g-leader-9-1-1` reads "9-2 leader G1" while `g-leader-9-2-1` reads "9-2 leader G2".
- A unit the view may not name reads "a concealed unit", as now.
- The id stays in the element's `data-unit` and its title, so the tests and the Locate button keep working.
- Role labels ("attacker-squad") give way to the unit's name. Modifier names get words ("stone building TEM", "leadership, 9-2 leader G1", "First Fire, no Assault Movement"). A reason is shown without its code; the code stays in `data-code` and the title.

**D12. One way to write a Location** (P-20). Every Location a player reads is "G4, level 1" on a one-board map and "G4 on board 01, level 1" otherwise. A typed field takes that form, the short "G4" and "G4:1", and the identifier form. Records and reasons use the words.

**D13. A hex picked on the map fills the field in use** (P-20; backlog sections 44 and 45).

- Every typed Location becomes a `LocationField` with its "Use <hex>" button and a level select when the hex has more than one level.
- A field can be armed ("Pick on the map"); the next hex clicked fills it. A rout route and a movement path take one hex per click, in order, with the last removable.
- The fire panel's From and Target take the picked hex too.
- A click still shows the hex in the Selection tab when no field is armed.

**D14. A fire proposal shows its arithmetic** (P-14, P-17).

- The review shows each firer's FP with its multipliers, the column, and the DRM known before the dice, from the Fire package's own `Preview`, and says when Final Fire is Area Fire. Where a target is not disclosed to the firing side, the lines that would name it read "not known to you" and the total says it is incomplete.
- The headline names the weapons: "4-6-7 squad G10 with its MMG" and "the MMG of 4-6-7 squad G10, alone".
- A result says when a weapon kept its rate of fire and may fire again.
- "MGs alone" is shown, disabled with a note, before the squad and MG are ticked.
- A fire group may take firers from several ADJACENT Locations (the user's answer 6): "From" becomes a list of Locations, each with its firers.

**D15. The workspace holds still** (P-28, P-29, P-31).

- The window does not scroll at 1024 pixels and wider. The card panel opens as a dialog from the header's link, and the units table becomes a tab of the activity strip, so nothing sits outside the workspace.
- The header has a fixed height: the Review button and the status line have their places reserved, and a long status is cut with its full text in the title.
- The workspace's least height falls to fit a window 600 pixels high; below that the narrow layout's tabs are used whatever the width.
- The review has a fixed track at 1024 to 1439 pixels and scrolls inside itself.
- A new proposal moves the focus to the review without scrolling the page; the scroll stays for the narrow layout, where it is needed.
- Propose and Confirm keep their places: a panel's Propose button sits in a foot that does not move, and Confirm and Cancel sit at the head of the review.
- The header names a proposal only while it waits.

**D16. The map keeps its view** (P-30, P-27).

- The view box is kept per game across a hand-over and a phase change; only "Fit", a new game, and a turned map fit again. A pane's resize keeps the center and the scale.
- The map shows "Loading the map" while its layers load.
- A Residual FP marker sits at the hex's corner, takes no clicks, and its hex lists it in the Selection tab.
- A Location in Melee has a "Melee" mark on its hex, drawn in the marks layer. No vocabulary or catalog version changes.

**D17. Records and the latest line** (P-15, P-21; backlog section 44's row).

- A record's heading names the side: "Turn 5, German Prep Fire Phase".
- "Latest" covers every event the view may read (a move, a pass, a choice, a surrender, a capture, a declaration) and gives a fire's result: "…fires at N5: 1MC; 4-4-7 squad R4 broke."
- After a hand-over, "Since you last looked" lists what the other view did, read for the incoming view.
- Records opens over the activity strip with its own scroll and takes no height from the map.
- The Deploy record of Turn 1 is traced, and every record kind is checked against the rule of plan section 15.9 in each side's view.

**D18. Rout is quick** (P-24). The rout search's result is kept for a unit at a revision, in the planner and on the page; the composed map is kept for a map; Low Crawl searches to its targets and no farther. The aim is a check and a Confirm under 2 seconds each for any rout in this game, measured in the Studio. A gate request that runs past half a second shows "Working" beside the button that started it.

## 6. Disclosure

Rulings R23.1 to R23.6 stand. What this pass adds is read for the view:

- A unit's name (D11) comes from what the view may know: a unit under "?" has no name to the other side, and a replaced unit's tag tells nothing its counter does not.
- The fire arithmetic (D14) is cut where a target is not disclosed.
- "Since you last looked" (D17) is built from the incoming view's records only.
- The hand-over screen (D10) shows a line only when both views read it alike, draws no counter, and gives only the score both sides know.
- The proposing side (D1) is checked by the planner; a refusal for the wrong side names the side that may act and nothing else.
- The hand-over still clears the last view's drafts, proposal, armed field, and announcement. The kept view box (D16) is a zoom and a center, which hold no hidden fact.

## 7. What stays out, for the backlog (section 48)

| Item | Why |
|---|---|
| LOS to the vertices of a Bypassing stack (P-11) | Ruling R10.7; a LOS change, not a page change |
| Fewer hand-overs in the Movement Phase: a pass in one step, and a standing pass by the DEFENDER (P-23) | The user, 2026-10-04: the hand-overs stay as they are for now. Backlog section 44's row stands |
| Controls the play test did not reach and the second play test still does not: Ambush, withdrawal from Melee, a prisoners' escape, Recombine, Repair, leaving the map | Listed with what was reached |
| A unit's name chosen by the player | D11 gives a fixed name |
| A played game's record as a page of its own, with a way to step through it | The Records tab and the game file hold it |
| A push to the other open tab when a view commits | Backlog section 44's row stands |

## 8. Tests

Written at the merge gate, by the handover prompt's rule. The Studio on port 6670 is the only test until the code is complete.

- `BacklogPass31Tests` (Play): the proposing side refused and accepted for each action family; a move ended by fire, for a broken, a pinned, and an eliminated mover; the MF limit across stacks, phases, and turns; fire at a Location with two leaders; a pinned firer's SW; a created leader directing fire; the consequences of a phase end; each Victory condition's account, held and not held; the hand-over screen's lines, shown only when both views read them alike; the rout search's results the same with and without the cache.
- The played game as a fixture (the user's answer 5): `guards-dl-01` replays to revision 613 with the winner it recorded, and its state at revision 441 offers the DEFENDER a pass.
- Studio tests: the names of replaced and created units; no raw id or reason code in the text of the review, the records, and the units table; the pickers by view; the header's result; the Advance list from an upper level; the Close Combat form with a prisoner; a field filled from the map.
- The harness lessons of passes 28c to 30b apply: `UseViewport`, a non-minimal card, the Studio test project run before the full suite, trx files read for failures.

## 9. Rulings to be written

| Ruling | Question | Decided by |
|---|---|---|
| R31.1 | What does the Defensive First Fire limit count? | A8.3, A9.2 |
| R31.2 | Do units in Melee take or cause a Leader Loss check under fire from outside? | A10.2, A11.141; the referee's review |
| R31.3 | How does a pinned unit fire its MG? | A7.81 |
| R31.4 | Who Deploys and Recombines without a leader, and may Russian squads Deploy? | A1.31, A1.32, A20.5, A25.2; corrects R13.4 |
| R31.5 | How is fire at a Location with several leaders decided? | A10.2, A10.21, A10.22, A10.72 |
| R31.6 | Who may propose what? | The user's answer 2 |
| R31.7 | What may the hand-over screen show? | Rulings R23.1 to R23.6 |
| R31.8 | Is a freed SMC Armed? | A20.551; the user's ruling of 2026-10-04 |

The rulings are written in section 5 of the Backlog Passes Plan.

## 10. The two play tests

**Pass 31 ends with a short one** (task 31.10): two Game Turns of The Guards Counterattack through the page in my Studio on port 6670, both sides, at 1568 by 677 pixels (the first test's window), and the report's own steps for P-01 to P-10, P-12, P-13, P-16, P-19, P-22, P-25, and P-26, each marked fixed, changed, or left. The game's end and its result are checked on a copy of `guards-dl-01` taken back to its last phase.

**Pass 31b ends with the full one** (task 31b.8): the same card from setup to its end, both sides, with real pointer and keyboard events, at 1568 by 677 and at 1366 by 768.

- Each of P-01 to P-31 is tried again by the report's own steps.
- The controls the first test did not reach are tried where the game allows: a Fire Lane, a SW recovered and transferred, Recombine, a withdrawal from Melee, Ambush, the adjudicator's view.
- The time for five turns and the count of proposals and refusals are compared with the first game's 2:14, 218, and 21.
- Its report goes into the review document. Screenshots go to the user in the chat.

After reading that report the user decides whether Claude in Chrome plays another card before the merge (answer 7).

## 11. Tasks and estimate

**Pass 31: nothing blocks play**

| Task | What it changes | Problems | Estimate |
|---|---|---|---|
| 31.1 The referee's check, first | R-01 and R-03 to R-08 against the PDF, and the passages behind D2, D6, D8, and D9; the rulings; a fix for each confirmed error goes to the task that owns its code | R-01, R-03 to R-08 | 1:15 |
| 31.2 Who may act | The proposing side in every proposal and the planner's check of it; the pickers by view; the phase end for the view that may end it; a waiting choice first | P-07, P-09 | 1:30 |
| 31.3 A move that fire stops | The DEFENDER's pass for a stack with no member left; the move ended by the fire that eliminates it; the mover's note | P-01 | 0:30 |
| 31.4 The last turn and the result | "Turn N of M", "last turn", the result in the header and the actions pane in every view, the account of every Victory condition, a card with text-only conditions | P-08 | 1:00 |
| 31.5 Consequences | A proposal's consequences by kind, the warning heading and Confirm, a blocked LOS and a Melee as consequences | P-06, P-12, P-13 | 1:00 |
| 31.6 Fire corrections | The MF limit, two leaders among the targets, the Effect column, a created unit's conditions, a pinned firer's SW | P-05, P-02, P-16, P-26, R-05 | 1:45 |
| 31.7 Advance and Close Combat | The Advance list by level, concealed units in it; prisoners out of the form, the stacked SMC ticked with its MMC, the four refusal texts, the ordered capture list | P-03, P-22, P-04, P-25, P-10 | 1:30 |
| 31.8 The Rally Phase | Ralliers, Dismantle, the three Self-Rally texts, the Self-Rally allowance | P-19, R-03, R-04 | 0:45 |
| 31.9 The hand-over screen | Who and when, why now, what the arriving side will do, what both sides know happened, the terrain map with its outlines, the score both know | The user's answer 3 | 1:30 |
| 31.10 A short play test | Two Game Turns and the report's steps for the problems of this pass | | 0:45 |
| | Overhead: three reviews and their fixes, the Studio check, the documents, the tests, the merge gate | | 1:30 |
| | **Pass 31 total** (build 11:30) | | **13:00** |

**Pass 31b: the page reads and holds still**

| Task | What it changes | Problems | Estimate |
|---|---|---|---|
| 31b.1 Names and words | `DisplayText.Unit` and its tags; modifier names; reasons without codes; Locations in words; about 170 call sites | P-18, P-20 | 2:30 |
| 31b.2 Picking on the map | Every typed Location a `LocationField`; an armed field; routes and paths by clicks; the fire panel's From and Target | P-20 | 1:15 |
| 31b.3 The fire proposal | The arithmetic before the dice, the headline with weapons, rate of fire kept, "MGs alone", fire groups across ADJACENT Locations | P-14, P-17 | 2:15 |
| 31b.4 A workspace that holds still | No window scroll; the card as a dialog and the units as a tab; a header of fixed height; short windows; fixed places for Propose and Confirm | P-28, P-29, P-31 | 2:00 |
| 31b.5 The map | The kept view box, the loading line, the Residual FP marker, the Melee mark | P-30, P-27 | 0:45 |
| 31b.6 Records and the latest line | The side in the heading, every event in "Latest" with a fire's result, "Since you last looked", Records over the strip, the disclosure check of every record kind | P-15, P-21 | 1:00 |
| 31b.7 Rout speed | The kept search, the composed map kept, the Low Crawl search, the "Working" sign | P-24 | 1:15 |
| 31b.8 The second play test | Section 10 | all | 1:30 |
| | Overhead: three reviews and their fixes, the Studio check, the documents, the tests, the merge gate | | 1:30 |
| | **Pass 31b total** (build 12:30) | | **14:00** |

The two passes total 27:00. Passes 30 and 30b took about 3:26 against 9:45 and 3:18 against 9:00, so the estimates are likely high.

Each pass runs the handover prompt's steps: build one task at a time with the Studio on port 6670 as the only test, commit each task once it passes its Studio check, three read-only reviews, the Studio check of the whole pass at the five widths, the documents, the merge gate, and a stop before the merge. Pass 31b starts only on the user's word after pass 31 is merged.

## 12. The user's answers

Answered 2026-10-04.

| # | Question | Answer |
|---|---|---|
| 1 | One pass, or two? | Two, as recommended: pass 31 and pass 31b. |
| 2 | Who may act (D1) | As recommended: the planner checks the proposing side, and the pickers list the viewing side's units. |
| 3 | Fewer hand-overs | No. The hand-overs stay as they are for now. The hand-over screen itself is to be fixed: it is a static screen that says nothing of the game at that moment, and it should show the player exactly what is going on. D10 and task 31.9 are that work; the old D18 is removed and its row stays in the backlog. |
| 4 | Unit names (D11) | As recommended: "4-6-7 squad G4", "9-2 leader G2". |
| 5 | The played game as a test fixture | Yes. |
| 6 | Fire groups across ADJACENT Locations | Yes, in 31b.3. |
| 7 | The second play test | As recommended: Claude plays it in its Studio; the user decides after its report whether Claude in Chrome plays another card before the merge. |
| 8 | After the game has ended | The user asked that the result be computed from the card's Victory Conditions, with a pass 31c if the code for it were missing. It is there (D4), so nothing is deferred, and the pass shows the result with an account of every condition. The views after the end stay as they are, which was the recommendation and is taken as accepted unless the user says otherwise. |
| 9 | Rules changes | As recommended, and with top priority: the rulebook PDF is checked first and at every task as the work goes, not at the end. The referee's check is task 31.1, and the rule at the head of this document holds for every task. |

## 13. The referee's check (task 31.1)

**Done 2026-10-04,** before any code changed, against the registered rulebook PDF (`eASLRB_v3_01.pdf`; pages are the PDF's physical pages).

| Id | The rulebook | Verdict | What the pass does |
|---|---|---|---|
| R-01 | A25.2 (p. 93): "Russian squads may not Deploy [EXC: 20.5 & 21.22]". A20.5 (p. 87): "An unbroken Guard squad can Deploy into HS automatically at any time, regardless of nationality or leader requirements." A1.31 and A1.32 (p. 45) exempt "Guards" from the leader. | **An error, and a wider one than reported.** Ruling R13.4 read A1.31's "Guards" as the Russian Guards squads. A25.2 bars every Russian squad from Deploying, so the exemption can only mean the Guards of prisoners, whom A20.5 lets Deploy. The Turn 1 attempt by `r-squad-21` was not legal. | Task 31.8, ruling R31.4: a Russian squad's Deploy is refused with A25.2; no squad Deploys without a leader for being a Guards squad; Recombining still needs a leader, for Guards squads too. The panel's "Guards need no leader" goes. A Guard of prisoners Deploying at will (A20.5) and a temporary crew (A21.22) go to the backlog. |
| R-02 | | Closed in section 2: an original 11, not 12. | Nothing. |
| R-03 | A10.63 (p. 68): "Any unit attempting Self-Rally (i.e., attempting to Rally without the presence of an unbroken leader) must add a +1 DRM." | **No error.** "Any unit" includes a leader. | Nothing. |
| R-04 | A18.11 (p. 85): "The first MMC Rally attempt of a player's own RPh may be performed as Self-Rally regardless of Self-Rally capability (10.63) ... provided there is no Good Order leader in that Location and the broken unit is not Disrupted." | **No error in the rule.** The allowance is the first MMC Rally attempt, so a leader-directed rally made first uses it, as the code has it. The fault is one text for every case. | Task 31.8: a text for each case, naming the attempt that used the allowance. |
| R-05 | A7.81 (p. 58): "Pinned Infantry fires MG/IFE/Canister as Area Fire ... and cannot ... declare a Fire Lane ... or use Intensive Fire or a Multiple ROF". | **An error.** A pinned unit's MG is halved as Area Fire; the code halves only the unit's own FP. Whether the code also keeps a pinned unit's MG from its rate of fire and a Fire Lane is read at the task. | Task 31.6, ruling R31.3. |
| R-06 | B10.31 (p. 136): the +1 for Height Advantage holds only when the unit is "not eligible to receive any other positive TEM". | **No error in the result:** the code adds it only with no other positive TEM. The fact row shows it whenever the firer is lower, which misleads. | Task 31.6: the row is shown only when the +1 applies. |
| R-07 | A11.22 (p. 71): a unit attacked by an Original 12 "may likewise withdraw from CC immediately thereafter". | **No error against ruling R14.8,** which has the withdrawal declared with the round, since the round is resolved in one action. The record says nothing when a 12 comes and no withdrawal was declared. | Task 31.7: the record says so, citing A11.22. Asking after the DR, as a choice, goes to the backlog. |
| R-08 | A11.14 (p. 70): "Any SMC in CC has an inherent FP attack and defense strength of one", added to its MMC's. Ruling R14.5 gives an Unarmed unit a CC FP of one. | **No error** in the strength of 4 + 1 + 1. | Nothing. See the next row. |
| new | A20.551 (p. 88): "Escaped SMC are always Armed." Ruling R14.6 arms a SMC again when its own escape succeeds; ruling R14.5 frees a prisoner as Unarmed when its Guard is eliminated. | **A question, not built.** In Turn 4 the 9-2 was freed when `g-squad-7` eliminated his Guard, and stayed Unarmed for the rest of the game. Whether a SMC freed by his own side is "escaped" is a reading. | The user ruled on 2026-10-04 that a SMC freed in any way is Armed: ruling R31.8, built in this pass. |

**The passages behind the pass's other rule changes.**

| Decision | The rulebook | What it settles |
|---|---|---|
| D2 (P-01) | A8.1 (p. 59): Defensive First Fire is "only vs a moving unit(s)", made "in that Location with as many attacks as he can bring to bear". Ruling R25.7 already has the pass ending the move once fire has broken, pinned, or eliminated every mover. | The window stays open for the DEFENDER after a mover breaks or is pinned; the pass must be reachable. |
| D6, the MF limit (P-05) | A8.3 (p. 60): "The same unit/weapon can never fire on a moving unit in the same Location more times than the number of MF/MP expended (FRD, but a minimum of once per hex) in that Location during that MPh". A9.2 (p. 61) says the same of a MG. | The count is by unit and by weapon, of this moving unit, in this Location, in this MPh; rounded down, at least once. |
| D6, several leaders (P-02) | A10.2 (p. 65): leaders check first, "higher Morale Level leaders checking before lower". A10.21 (p. 66): one unbroken leader's modifier applies, not cumulative, and "the owner must choose which". A10.22: a leader takes the modifier of "an unbroken leader of higher morale", never his own. A10.72 (p. 68): a non-zero modifier cannot be declined unless another leader's is substituted. | Ruling R31.5: the game takes the most favorable modifier among the unbroken leaders who may give it, since the owner would choose it; a leader takes one only from a leader of higher Morale Level. Each lost leader causes its own LLMC or LLTC in units of lower Morale Level (A10.2). |
| D8 (P-04) | A11.14 (p. 70): "the smallest increment which can be the subject of a single CC attack is a single MMC (plus any SMC stacked directly above it)", and stacks are rearranged "prior to both sides' declaration of CC attacks". | The check is right; the form must tick the SMC with its MMC and keep the stacking. |
| D8 (P-10) | A20.22 (p. 87): at the Kill Number "one defending unit of the defender's choice is captured". | The order of choice belongs to the defender's side. |

## 14. Pass 31 as built

**Built 2026-10-04,** tasks 31.1 to 31.10, one commit a task or pair of tasks, each checked in my Studio on port 6670 before its commit. The [review document](<Scenario A1 Backlog Pass 31 Review 2026-10-04.md>) lists what a player now meets for each problem, the three reviews' findings and their fixes, and the Studio check.

**As designed:** D0 (the rulebook first), D1 (who may act), D3's first half (a waiting choice first), D4 (the last turn and the result, with the account of every condition), D5 (consequences), D6 (the five fire corrections), D7 (Advance), D8 (the Close Combat form), D9 (the Rally Phase and Deploy), and D10's text (the hand-over screen).

**What differs from the design:**

- **Old games replay unchanged.** Replay recomputes every recorded attack, so a rule changed outright would have stopped older games, the played game among them, from opening. The pinned MG (R31.3) and the Melee exemption from Leader Loss (R31.2) are facts recorded with each new attack, and a freed SMC is armed by an event (R31.8). A record without them resolves as it did.
- **The phase end.** Either side ends the RPh, RtPh, and CCPh, where D1 gave it to the phasing side: the page hands the screen to the side that has not yet acted, so the side that acts last ends the phase with no extra hand-over. A side may not end the RtPh while the other side must still rout.
- **The account of the Victory conditions** is the adjudicator's during play and everyone's after the end, where D4 gave it to each side's view: a condition such as "the only unbroken units in the building" would tell a side what lies under the enemy's "?" (ruling R23.4; the referee's review).
- **A move whose movers are all eliminated** is ended by the DEFENDER's pass, not by the fire itself (D2). The pass is offered.
- **The hand-over screen has no map** of the terrain; the arriving view opens on the Location the game waits on. The map is in backlog section 48, for pass 31b.
- **D3's record line** for a resolution that waits on a choice is not built (backlog).
- **D8:** one text serves the four stacking checks, now saying what to do; the capture's order is typed by the capturing side, with a default; whether the other side has declared is not said in the proposal (backlog).
- **D9:** the Self-Rally text says which rule bars the unit, without naming the unit that used the side's first attempt.
- **The MF rounding** stays as it was: A8.3 says "FRD, but a minimum of once per hex".
- **Added:** units in Melee take and cause no Leader Loss check (R31.2); a freed SMC is Armed (R31.8); a win at once is a consequence; a stacked SMC is ticked with its attacking MMC.

**The short play test** (task 31.10) was one whole Russian Player Turn from the end of setup, not two Game Turns; each problem was checked on a copy of the played game at the revision where it happened. Pass 31b ends with the full play test.
