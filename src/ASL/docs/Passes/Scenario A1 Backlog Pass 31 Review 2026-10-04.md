# Scenario A1 Backlog Pass 31: Play-test UI I, Nothing Blocks Play

**Date:** 2026-10-04

**Branch:** `feature/asl-backlog-pass-31`

**Related documents:** the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>) (sections 4, 9, 12, 13, and 14), the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>) (passes 31 and 31b), the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 48, and the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), rulings R31.1 to R31.8. The play test's report and its screenshots are the user's files and are not in the repository.

## Status

Built, reviewed, and checked in the Studio; the merge gate follows. The pass works from a game of The Guards Counterattack that Claude in Chrome played to its end through the Play page (`guards-dl-01`, revisions 56 to 613), from its report (31 problems, 8 possible rules errors), its screenshots, the game's event log, and the gate's audit log. It removes what blocked or lost play, states the last turn and the result, corrects the rules the play test put in doubt, and makes the hand-over screen say what is going on. The rulebook was read first, and again at every task. Pass 31b takes the page's words, layout, map, and records.

## The rulebook check

Done before any code changed; the design's section 13 has the passages. In short:

| Id | Verdict | What the pass did |
|---|---|---|
| R-01 | An error, wider than reported: A25.2 bars every Russian squad from Deploying, and the "Guards" of A1.31 and A1.32 are the Guards of prisoners (A20.5) | Ruling R31.4, correcting R13.4 |
| R-02 | A misreading of the record: an original 11 | Nothing |
| R-03 | No error: A10.63 gives "any unit" the +1 | Nothing |
| R-04 | No error in the rule (A18.11: the first MMC Rally attempt); one text for every case | A text for each case |
| R-05 | An error: A7.81 fires a pinned unit's MG as Area Fire, with no Multiple ROF | Ruling R31.3, for attacks recorded from now on |
| R-06 | No error in the result; the fact row already says "when the target has no other positive TEM" | Nothing |
| R-07 | No error against ruling R14.8 | The record says when an Original 12 came and no withdrawal had been declared |
| R-08 | No error: A11.14 gives a SMC a strength of one | Nothing |
| new | A20.551: "Escaped SMC are always Armed"; the freed 9-2 stayed Unarmed | The user ruled on 2026-10-04 that a SMC freed in any way is Armed: ruling R31.8 |

One citation of the design was wrong and was corrected before its task: A8.3 rounds the MF down ("FRD, but a minimum of once per hex"), so the Defensive First Fire limit keeps its rounding.

## What was built

| Problem | What a player now meets |
|---|---|
| P-01 | The DEFENDER's block and pass show for a moving stack that fire left with no member; the pass ends the move, and another stack moves. Checked at revision 441 of the played game: 441 to 443, then five German units offered to move. |
| P-02 | Fire at a Location with several leaders resolves: leaders check first by Morale Level, a unit takes the best modifier a leader may give it, each lost leader causes its own checks, and a unit's second Leader Loss check has its own roll. Checked at revision 494: the shot the play test was refused. |
| P-03, P-22 | The Advance list holds what the movement rules accept: the same level of the same building, a stairwell, ground level from ground level. Concealed units are listed. Checked at revisions 384 and 81. |
| P-04, P-25 | A prisoner is no combatant in the Close Combat form. Ticking an MMC as a target ticks the SMC stacked with it; ticking it as an attacker does the same. A unit in a declared attack is not offered again. A view removes only its own attacks. Checked at revisions 543 and 294. |
| P-05 | The limit counts this stack's move only, from its first step, by unit and by weapon. Checked at revision 332: the refused shot is accepted. |
| P-06, P-12, P-13 | A proposal that eliminates units, ends the game, fires with no LOS, or hits the firing side's own units says so in a warning before Confirm, and Confirm names it ("Confirm: 2 units are lost", "Confirm and end the game"). Checked at revisions 381 and 612. |
| P-07 | A proposal names its view, and the planner refuses what that side may not do. The pickers list the viewing side's units. The phase is ended by the side that acts in it. A side may not end the Rout Phase while the other side must still rout. |
| P-08 | The header reads "Turn 5 of 5 (the last turn)". An ended game states "German win." with its reason and "Why" in the header and the actions pane, in every view, with every Victory condition as met or not met. The result is the one the game already computed from the card (`ScenarioVictory`). |
| P-09 | While a choice or a surrender waits, every other action is disabled, with a line saying why. |
| P-10 | A capture attempt on several units takes the targets' order when none is typed, squads before leaders. |
| P-16 | The Effect column says "already broken" and "already pinned". |
| P-19 | A broken leader is not offered as a rallier. Dismantle is offered only for the German MMG in its phases. |
| P-26 | A created leader's Good Order is known, so he directs fire. |
| P-31 | The header names a proposal only while it waits for Confirm. |
| The hand-over screen | When, why the game waits, what the arriving side will be asked to do, what has happened since it last had the screen, and each side's CVP and Exit VP. It names no unit and reads only events no view is kept from. |

## Rulings

R31.1 to R31.8, in section 5 of the Backlog Passes Plan.

## The three reviews

Three read-only reviews read the branch at commit 842c09c. None found a fault in the replay of saved games, and the Leader Loss loop ends.

**Referee** (12 findings):

| # | Finding | Fix |
|---|---|---|
| 1 | The account of a "sole unbroken" Victory condition would tell a side whether the enemy's "?" in the building were Dummies, or that a hidden unit was there | The account is the adjudicator's during play, and everyone's after the end (ruling R23.4) |
| 2 | Ticking an MMC as a target could tick a concealed enemy SMC kept from the other view's stacking | Never a SMC of the other side under "?" |
| 3 | The First Fire limit did not start again at a vehicle's first step | It does |
| 4 | Setup still lets Russian squads set up Deployed (A2.9) | Backlog section 48; no setup plan uses it |
| 5 | The own-units warning read the Location, so it spoke in Defensive First Fire, which hits only the movers; the blocked LOS citation | It reads the attack's targets; A6.11 |
| 6 | A unit's second Leader Loss DR made no Sniper check | The key is read |
| 7 | An older error now easier to reach: units in Melee took and caused Leader Loss checks under fire from outside | A fact recorded with new attacks; no check in a Melee (A11.141) |
| 8 | The hand-over's "why" was wrong on load in the phases where both sides act | Both sides are awaited there |
| 9 | A leader who broke and is then eliminated by another's LLMC causes only the LLMC | Backlog |
| 10 | Leaders do not always check first across known and concealed groups (older) | Backlog |
| 11 | Either side ends the RPh, RtPh, and CCPh, where the design gave it to the phasing side | Recorded in ruling R31.6 |
| 12 | Wording, and the rulings not yet written | Done here |

**Table player** (11 findings and 6 notes; no blocker):

| # | Finding | Fix |
|---|---|---|
| 1 | Either side could end the Rout Phase while the other side's units still had to rout, eliminating them on the opponent's Confirm | Refused, with "hand over first" |
| 2 | Keeping DM as the RPh ends is the ending side's only, and listed both sides' units | The view's own units; each side's ticks kept across the hand-over go to the backlog |
| 3 | A capture's order of choice is typed by the attacker; the default gave up the leader first | Squads before leaders; the defender's own control goes to the backlog |
| 4 | The hand-over screen on load was wrong in the RPh, RtPh, and CCPh | With the referee's 8 |
| 5 | Three task lines were wrong in ASL terms (Close Combat, Final Fire, Advancing Fire) | Rewritten |
| 6 | The Deploy help's body still gave the Guards exemption | Corrected |
| 7 | The Victory account read as a contradiction ("wins when ... short of") | "A condition for a Russian win, not met: ..., where 2 is needed" |
| 8 | A win at once was a routine reason | A consequence |
| 9 | "What has happened" dropped repeats | Counted |
| 10 | Older texts (the rout status, one stacking text, the Self-Rally text's unit) | Backlog, for pass 31b's words |
| 11 | An attacking MMC did not bring its stacked leader | It does |

**UI and Blazor** (5 to fix, 6 notes; no blocker):

| # | Finding | Fix |
|---|---|---|
| 1 | After a Close Combat round the stacking was read from the state before it | Read after the commit |
| 2 | The status line said "Every check passed" beside a review with consequences | One text |
| 3 | The hand-over's history, the consequences, and the result's list were read on every render | The history and the consequences are read once; the Advance list is left (backlog) |
| 4 | The opening hex could be set when the hand-over was not taken | Set only when it is |
| 5 | The score used the leaving view's read | Checked by the referee: CVP and Exit VP do not depend on the view |

The UI review also listed the existing tests the pass changes; the merge gate brings them in line.

## The Studio check

In my Studio on port 6670, on copies of the played game cut back to the revisions where each problem happened (the copies are in `src/ASL/boards/`, which is not staged), at 1568 by 677 (the play test's window) and 1366 by 768:

- Each row of "What was built" above, by its revision.
- **The short play test:** from the end of setup (revision 58), one whole Russian Player Turn through the page with its hand-overs, to the German Prep Fire Phase (revision 85): Prep Fire, a move with Defensive First Fire and the pass, each phase ended by the side that ends it, an advance from an upper level.
- **After the reviews:** the freed 9-2 Armed by the next action (revision 485); the Rout Phase end refused from the German view while Russian units must rout (revision 381), which also shows the planner's check of the proposing side; the hand-over screen on load in the Rally Phase.
- **The hand-over screen at 1920, 1366, 1024, 683, and 320 pixels:** no sideways scroll at any width.

Not done, and said so: the design's two Game Turns from a new game (one Player Turn was played); a walk of every panel at each of the five widths, since the pass leaves the layout to pass 31b; the blocked LOS warning in the Studio (it is in the tests).

## Tests

Written at the merge gate: `BacklogPass31Tests` (Play) and `PlayPagePass31Tests` (Studio), with the played game as a fixture. The design's section 8 has the list; the time log has the counts.

## Left out

Backlog section 48.
