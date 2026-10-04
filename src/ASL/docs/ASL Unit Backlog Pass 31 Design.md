# ASL Unit Backlog Pass 31 Design

**Status:** Draft, 2026-10-04, waiting for the user's approval and answers (section 12). Nothing is built. Pass 31 (Play-test UI) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, added by the user on 2026-10-04.

**Date:** 2026-10-04

**Related documents:** the [pass 30 handover prompt](<ASL Pass 30 Handover Prompt.md>), whose standing rules and harness lessons apply unchanged; the [pass 28c design](<ASL Unit Backlog Pass 28c Design.md>) (the Play workspace) and the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>) (the shared board workspace); the [pass 30b design](<ASL Unit Backlog Pass 30b Design.md>) and its [review](<Scenario A1 Backlog Pass 30b Review 2026-10-03.md>); the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 44 to 47; the plan's sections 15.9 to 15.11. The play test's report and screenshots are the user's files and are not in the repository.

**What the pass is.** On 2026-10-03 and 2026-10-04 Claude in Chrome played one live game of The Guards Counterattack from setup to its end through the Play page's own controls, both sides, with the hand-over between them. Its report lists 31 problems and 8 possible rules errors. This pass makes the Play page fit to play such a game: no legal action is blocked or lost, a proposal says what it will do, the game says when it ends and who won, and the page holds still and speaks in the player's words. It ends with a second play test.

Unlike the Studio passes before it, this pass changes the game too, so it adds rulings (R31.1 onward, section 9).

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

## 4. Decisions, part A: nothing blocks play

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

**D4. The last turn and the result are in the header** (P-08; the user's note of 2026-10-04).

- The header reads "Turn 4 of 5". In the last Game Turn it adds "last turn", and the proposal that ends the last phase is a consequence (D5): "This ends the game."
- Once the game has ended, the header shows the result in every view: "German win: no Victory Condition of the other side holds (A26.3)", with "Why" opening the facts. The actions pane shows the same notice in place of "its result is above".
- A game that ends with no winner says so: "The game ended with no winner", with its reason. A game that ended in a state the Victory Conditions do not decide says "ended; the result is not decided", with the reason.
- After the end the hand-over is not needed to read the result. "View as" stays as it is (question 8).

**D5. A proposal lists its consequences apart from its checks** (P-06, P-12, P-13).

- `GamePlan` gains a list of consequences, each with a kind: loss (a unit eliminated, captured, or surrendered by the proposal), waste (a shot with every LOS blocked), friendly fire (a target Location holding the proposer's own units or prisoners), and end (the game ends).
- The review's heading is then "This will eliminate r-squad-20 and r-squad-21" in the warning style, with the consequences first. Confirm is labelled for what it does ("Confirm and lose 2 units") and does not take the focus; a routine proposal is as now.
- Failure to Rout, Melee eliminations at a phase end, and the game's end are the first consequences; the kinds are a closed list the planner fills, not text the page parses.

**D6. Fire: five corrections** (P-05, P-02, P-16, P-26, R-05).

- **The MF limit** counts the firer's attacks on this moving stack in this Location in this Movement Phase, a weapon apart from the unit that fires it, and no longer truncates a half MF. Ruling R31.1.
- **Two leaders in a target Location.** The Fire package's order of checks for several leaders, the modifier each takes, and Leader Loss with more than one leader are reviewed against A10.2 and A10.21, tested, and the refusal removed. A leader whose broken Morale Level the catalog lacks keeps its own refusal. Ruling R31.2.
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
- The referee checks whether a leader-directed rally should use up the side's one MMC Self-Rally (A18.11), and whether a leader's own Self-Rally takes the +1 (A10.63). Rulings R31.4 and R31.5.

**D10. The referee's check of the possible rules errors.** Each of R-01 and R-03 to R-08 is checked against the rulebook PDF before any code changes. A confirmed error becomes a ruling and a fix with its test; a cleared one is recorded in the review with its citation. R-02 is closed (section 2). R-06 (Height Advantage shown for a building's upper level) and R-07 (an original 12 in Close Combat) are expected to need a fix; R-01 (a Guards squad Deploying with no leader) and R-08 (an unarmed leader's CC strength) are expected to need a citation or a ruling.

## 5. Decisions, part B: the page reads and holds still

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
- A fire group may take firers from several ADJACENT Locations (question 6): "From" becomes a list of Locations, each with its firers.

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

**D18. Fewer hand-overs in the Movement Phase** (P-23; backlog section 44's row). Two changes, the second by the DEFENDER's choice (question 3):

- **A pass is one step.** "Pass" commits at once and offers the hand-over back; it is a declaration with nothing to review. Each step then costs the mover one proposal and the DEFENDER one click between two hand-overs.
- **A standing pass.** The DEFENDER may say, for the rest of the Movement Phase, "pass unless a moving stack is in the LOS of a unit of mine that could fire at it". The game then closes a step's window itself when no unit of the DEFENDER, hidden ones included, has LOS to the Location with a shot left. The DEFENDER is told what this gives away: a step that does not stop tells the mover that nothing sees the Location, hidden units included. The order is stored as an event, visible to its side only, and ends with the phase. Ruling R31.6.

**D19. Rout is quick** (P-24). The rout search's result is kept for a unit at a revision, in the planner and on the page; the composed map is kept for a map; Low Crawl searches to its targets and no farther. The aim is a check and a Confirm under 2 seconds each for any rout in this game, measured in the Studio. A gate request that runs past half a second shows "Working" beside the button that started it.

## 6. Disclosure

Rulings R23.1 to R23.6 stand. What this pass adds is read for the view:

- A unit's name (D11) comes from what the view may know: a unit under "?" has no name to the other side, and a replaced unit's tag tells nothing its counter does not.
- The fire arithmetic (D14) is cut where a target is not disclosed.
- "Since you last looked" (D17) is built from the incoming view's records only.
- The standing pass (D18) is the one change that tells a side something it did not know. It is the DEFENDER's choice, it is told what it gives away, and the order itself is never shown to the mover.
- The proposing side (D1) is checked by the planner; a refusal for the wrong side names the side that may act and nothing else.
- The hand-over still clears the last view's drafts, proposal, armed field, and announcement. The kept view box (D16) is a zoom and a center, which hold no hidden fact.

## 7. What stays out, for the backlog (section 48)

| Item | Why |
|---|---|
| LOS to the vertices of a Bypassing stack (P-11) | Ruling R10.7; a LOS change, not a page change |
| Controls the play test did not reach and the second play test still does not: Ambush, withdrawal from Melee, a prisoners' escape, Recombine, Repair, leaving the map | Listed with what was reached |
| A unit's name chosen by the player | D11 gives a fixed name |
| A played game's record as a page of its own, with a way to step through it | The Records tab and the game file hold it |
| A push to the other open tab when a view commits | Backlog section 44's row stands |

## 8. Tests

Written at the merge gate, by the handover prompt's rule. The Studio on port 6670 is the only test until the code is complete.

- `BacklogPass31Tests` (Play): the proposing side refused and accepted for each action family; a move ended by fire, for a broken, a pinned, and an eliminated mover; the MF limit across stacks, phases, and turns; fire at a Location with two leaders; a pinned firer's SW; a created leader directing fire; the consequences of a phase end; the standing pass and its event's visibility; the rout search's results the same with and without the cache.
- The played game as a fixture (question 5): `guards-dl-01` replays to revision 613 with the winner it recorded, and its state at revision 441 offers the DEFENDER a pass.
- Studio tests: the names of replaced and created units; no raw id or reason code in the text of the review, the records, and the units table; the pickers by view; the header's result; the Advance list from an upper level; the Close Combat form with a prisoner; a field filled from the map.
- The harness lessons of passes 28c to 30b apply: `UseViewport`, a non-minimal card, the Studio test project run before the full suite, trx files read for failures.

## 9. Rulings to be written

| Ruling | Question | Decided by |
|---|---|---|
| R31.1 | What does the Defensive First Fire MF limit count? | A8.3, A9.2; D6 |
| R31.2 | How do several leaders in a target Location check, and who modifies whom? | A10.2, A10.21; the referee's review |
| R31.3 | Is a pinned firer's SW halved? | A7.8; the referee's check |
| R31.4 | Does a leader-directed rally use the side's one MMC Self-Rally? | A18.11; the referee's check |
| R31.5 | Does a leader's Self-Rally take the +1? | A10.63; the referee's check |
| R31.6 | May the DEFENDER give a standing pass, and what does it close? | A8.1; the user's answer to question 3 |
| R31.7 | Who may propose what? | D1; the user's answer to question 2 |

More are added if the check of R-01, R-06, R-07, and R-08 finds errors.

## 10. The second play test

The pass ends with a game played through the page, as the first was: The Guards Counterattack from setup to its end, both sides, with real pointer and keyboard events in my Studio on port 6670, at 1568 by 677 pixels (the first test's window) and at 1366 by 768.

- Each of P-01 to P-31 is tried again by the report's own steps and marked fixed, changed, or left, with what the page now does.
- The controls the first test did not reach are tried where the game allows: a Fire Lane, a SW recovered and transferred, Recombine, a withdrawal from Melee, Ambush, the adjudicator's view.
- The time for five turns and the count of proposals and refusals are compared with the first game's 2:14, 218, and 21.
- Its report goes into the review document. Screenshots go to the user in the chat.

The user may also want an independent test by Claude in Chrome on another card before the merge (question 7).

## 11. Tasks and estimate

| Task | What it changes | Problems | Estimate |
|---|---|---|---|
| **Part A** | | | |
| 31.1 Who may act | The proposing side in every proposal and the planner's check of it; the pickers by view; the phase end for the view that may end it; a waiting choice first | P-07, P-09 | 1:30 |
| 31.2 A move that fire stops | The DEFENDER's pass for a stack with no member left; the move ended by the fire that eliminates it; the mover's note | P-01 | 0:30 |
| 31.3 The last turn and the result | "Turn N of M", "last turn", the result in the header and the actions pane in every view | P-08 | 0:45 |
| 31.4 Consequences | A proposal's consequences by kind, the warning heading and Confirm, a blocked LOS and a Melee as consequences | P-06, P-12, P-13 | 1:00 |
| 31.5 Fire corrections | The MF limit, two leaders among the targets, the Effect column, a created unit's conditions, a pinned firer's SW | P-05, P-02, P-16, P-26, R-05 | 1:45 |
| 31.6 Advance and Close Combat | The Advance list by level, concealed units in it; prisoners out of the form, the stacked SMC ticked with its MMC, the four refusal texts, the ordered capture list | P-03, P-22, P-04, P-25, P-10 | 1:30 |
| 31.7 The Rally Phase | Ralliers, Dismantle, the three Self-Rally texts, the Self-Rally allowance | P-19, R-03, R-04 | 0:45 |
| 31.8 The referee's check | R-01 and R-03 to R-08 against the PDF; the fixes and rulings for those confirmed | R-01, R-06, R-07, R-08 | 1:00 |
| **Part B** | | | |
| 31.9 Names and words | `DisplayText.Unit` and its tags; modifier names; reasons without codes; Locations in words; about 170 call sites | P-18, P-20 | 2:30 |
| 31.10 Picking on the map | Every typed Location a `LocationField`; an armed field; routes and paths by clicks; the fire panel's From and Target | P-20 | 1:15 |
| 31.11 The fire proposal | The arithmetic before the dice, the headline with weapons, rate of fire kept, "MGs alone", fire groups across Locations | P-14, P-17 | 2:15 |
| 31.12 A workspace that holds still | No window scroll; the card as a dialog and the units as a tab; a header of fixed height; short windows; fixed places for Propose and Confirm | P-28, P-29, P-31 | 2:00 |
| 31.13 The map | The kept view box, the loading line, the Residual FP marker, the Melee mark | P-30, P-27 | 0:45 |
| 31.14 Records and the latest line | The side in the heading, every event in "Latest" with a fire's result, "Since you last looked", Records over the strip, the disclosure check of every record kind | P-15, P-21 | 1:00 |
| 31.15 Fewer hand-overs | The one-step pass; the DEFENDER's standing pass | P-23 | 1:30 |
| 31.16 Rout speed | The kept search, the composed map kept, the Low Crawl search, the "Working" sign | P-24 | 1:15 |
| 31.17 The second play test | Section 10 | all | 1:30 |
| | Overhead: three reviews and their fixes, the Studio check of the whole pass, the documents, the tests, the merge gate | | 2:00 |
| | **Pass 31 total** (build 22:45; part A 8:45, part B 14:00) | | **24:45** |

Passes 30 and 30b took about 3:26 against 9:45 and 3:18 against 9:00, so the estimate is likely high. It is still more than twice any pass before it, which is why question 1 asks about splitting it.

## 12. Questions for the user

Each with a recommendation.

1. **One pass, or two?** Recommended: two. Pass 31 is part A (tasks 31.1 to 31.8: nothing blocks play, the result, the rules), with a short play test of its own; pass 31b is part B (31.9 to 31.17: names, picking, the fire proposal, the layout, the map, records, hand-overs, rout speed), ending in the full second play test. Each has its three reviews, its Studio check, and its merge. Part A is the part that changes the game, and it is better reviewed and merged before 170 call sites change their text. No pass is renumbered: 31b follows 31 as 30b followed 30.
2. **Who may act (D1).** Recommended: the planner checks the proposing side, and the pickers list the viewing side's units. The other way is the page alone, which leaves the gate accepting any action from any view.
3. **Fewer hand-overs (D18).** Recommended: both the one-step pass and the standing pass, the second as the DEFENDER's own choice with what it gives away stated. The other ways: the one-step pass alone; or a window that closes itself whenever no Known enemy unit could fire, which is wrong for hidden units and is not proposed.
4. **Unit names (D11).** Recommended: "4-6-7 squad G4", "9-2 leader G2": printed values, kind, and a tag of the side's letter and a number within the kind. The other ways: keep the present ids for units that were never replaced and name only the others; or a leader's name from a list ("Lt. Weber").
5. **The played game as a test fixture.** Recommended: yes. `guards-dl-01.game.json` (725 KB of JSON, no images) is copied into the Play tests' fixtures, so a real 613-revision game replays in the tests. The audit log is not committed.
6. **Fire groups across Locations (in 31.11).** Recommended: build it. The planner forms them already, and a fire group of ADJACENT Locations is ordinary play.
7. **The second play test (section 10).** Recommended: I play The Guards Counterattack in my Studio as the pass's last task, and you decide after reading its report whether Claude in Chrome plays another card before the merge.
8. **After the game has ended.** Recommended: the result shows in every view, and the views stay as they are, with "?" and hidden units still hidden to the other side; the adjudicator's view shows everything. The other way is to open every unit to both sides once the game ends.
9. **Rules changes.** Recommended: each possible rules error is checked against the PDF first; a confirmed one is fixed under a new ruling and listed in the review for you to read at the merge stop, without a stop of its own, unless its fix changes the pass's scope.
