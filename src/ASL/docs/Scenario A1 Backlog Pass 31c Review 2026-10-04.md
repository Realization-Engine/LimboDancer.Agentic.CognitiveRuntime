# Scenario A1 Backlog Pass 31c: Play-test UI II

**Date:** 2026-10-04

**Branch:** `feature/asl-backlog-pass-31c`

**Related documents:** the [pass 31c design](<ASL Unit Backlog Pass 31c Design.md>) (sections 4, 6, 7, 12, and 13), the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>) (sections 5 and 10), the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>) (pass 31c and section 15.13), the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 50, and the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), rulings R31c.1 to R31c.4. The first play test's report is in `C:\Users\dland\Downloads\Playability-Report-2026-10-04\`, outside the repository.

## Status

Built, reviewed, checked in the Studio, and played: tasks 31c.0 to 31c.8. The tests and the merge gate follow the user's answers to the questions at the end of this document. The pass changes how the page words, lays out, and draws a game. It changes no rule's result: nothing under the Rules project changed, the planner's two changes are a refusal's words and a public reading of ADJACENT, and the played game `guards-dl-01` still opens on Play and on Replay with its 613 revisions and its 221 steps.

## What a player now meets

| Task | What a player now meets |
|---|---|
| 31c.0 The Game inspector | "Game states" is "Game inspector", in the Verify group with Fidelity. It lists the fixture and games that have ended, not a game still being played. The board viewer shows such a game only from Play's or Replay's link, in that page's view, with no view picker, no revision stepping, and no revision for a side. |
| 31c.1 Names and words | A unit reads by its printed values, its kind, and a tag: "4-6-7 squad G4", "9-2 leader G3", "9-0 Commissar R1". A side's own units are numbered as they entered; the other side's are numbered in the order that view first held them by name, so a tag counts only what the side has seen. A unit the view could not name reads "a concealed unit". A Location reads "G4, level 1". Modifiers, reasons, dice rolls, and actions read in words; a reason's code is in its title. Every record, on Play and on Replay, is said through one place. |
| 31c.2 Picking on the map | A typed Location shows words and reads "G4", "G4:1", "G4, level 1", and the identifier. "Pick on the map" arms a field, and the next hex clicked fills it; a route takes one hex a click, with "Remove" and "Clear the route". The fire From and Target, Advance, ordnance, Close Combat, and smoke selects offer "Use G3" for the hex picked. Each of a field's actions is on a line of its own (the user, during the build). |
| 31c.3 The fire proposal | Before the dice: each firer's FP with its multipliers, the total and its column, and the DRM. A fire group takes firers from ADJACENT Locations ("Add an ADJACENT Location"). "Only its MG fires" is in sight, disabled with its note, before the unit and its MG are ticked. A record says when a MG kept its rate of fire. |
| 31c.4 A workspace that holds still | With a game open the window does not scroll from 1024 pixels wide and 600 high. The scenario card is a dialog; the records, the units, the scripted dice, and the audit open over the activity strip. The header keeps one height. A proposal moves the focus and scrolls nothing. Confirm and Cancel are at the head of the review, or after the consequences when there are any. A side is shown no revision in a game still played. |
| 31c.5 The map | The map's view is kept for a game and a view across a hand-over. "Loading the map" shows while it loads. The Residual FP marker is small, at the hex's corner, takes no click, and is listed in the Selection tab. A Location in Melee has a "Melee" mark. The hand-over screen shows the map as both sides know it, with the Locations its lines name outlined. |
| 31c.6 Records and the latest line | A record's heading names the side. "Latest" says the last thing done with its result: "... fire at F5, level 1: 1MC; 9-1 leader G1: pinned." "Since you last looked (n)" opens when a side takes the screen and lists what it missed. |
| 31c.7 Rout speed | A rout is checked and confirmed in under 2 seconds. A gate request that runs past half a second says "Working" beside its button. |

## Rulings

R31c.1 (a unit's name and tag), R31c.2 (a game still played and the views chosen at will), R31c.3 (what the hand-over screen's map shows; amends R31.7), and R31c.4 (what a fire's preview may show), in section 5 of the Backlog Passes Plan.

## The three reviews

Three read-only reviews read the branch at commit d4c1505.

**Referee** (4 blockers, 4 fixes, 2 notes). Clean: how a view's names are made, the Replay page's changes and Units tab, and the rules (nothing under Rules changed; the board handle kept for a loaded board cannot go stale).

| # | Finding | Fix |
|---|---|---|
| 1 | The fire preview was never cut: a Known unit and a hidden unit in the target Location showed every firer twice, the second line with the concealed target's halving, which told of the hidden unit | The preview is made from what the firing view may know: a hidden unit is left out of its facts; with a "?" in the target Location the lines, DRM, and total that rest on what lies beneath are not shown (ruling R31c.4) |
| 2 | With a "?" in the target Location the preview still showed FFNAM, FFMO, and Hazardous Movement, whose absence against a moving "?" says it is all Dummies | Left out with the gunshield, emplacement, and "against" modifiers |
| 3 | A public Deploy or dismantle read "a concealed unit tries to Deploy with a concealed unit: NTC DR 7 -1 against morale 7: two HS": the name hidden, the facts not | The record is given only when the view could name the unit |
| 4 | The hand-over map chose a side's units by the counter's nationality, not by who owns the unit | From a side's view come the other sides' units it holds by name, their "?", and unpossessed equipment |
| 5 | A stale Confirm told a side two revisions | "The game has moved on since this proposal was made. Propose again." |
| 6 | The board viewer fell back to the adjudicator's view when a still-played game's link named no view | No game is shown |
| 7 | The other side's HS read "R2a" when the view had named squad R2, though the Deployment was under "?" | A tag is inherited only where the view held the parent just before |
| 8 | "a concealed unit with its a weapon not in view" | A concealed firer's weapons are not said |

**Table player** (23 findings). Fixed: the DC named once; "fire" or "fires" by the count of firers; "Latest" passing over a phase's start; Confirm after the consequences; "Since you last looked" open on arrival; a Location taken out of a fire group taking those it alone joined; FFNAM and FFMO as players say them; a route typed with "level"; the armed line's noun; "Clear the route"; "cellar" in the level select; the hand-over note saying what the outlines are; the Self-Rally refusal; records beginning with a capital. Left for the user: the tags read like hex names (question 1). Left for the backlog: a director in each Location of a group, the IFT column's results in the preview, the MG's holder in its malfunction line, counter names for vehicles and Guns.

**UI and Blazor** (24 findings). Fixed: the names' cache made whole before it is published, bounded, and serving a shorter reading of the same game; a tag never given twice at a Recombination; a text put in words once for a history and a view; a field disarmed when it leaves the page or is disabled; the header's reserved rows only on Play, so Replay has no empty band; the workspace's own top measured, and again when the window resizes; the card's Escape and dimmed page; a commit and Escape closing the open panel; the loading line only once the viewport is ready; the hand-over map inert and its errors caught; "Working" never left on a stale button. Left for the backlog: a modal dialog for the card, the whole Studio narrow under 600 pixels of height, the hand-over map read from the history Play holds.

## The Studio check

In my Studio on port 6670, built into its own output folder:

- **Every step on Replay:** the 221 steps of `guards-dl-01` in the adjudicator's, the German, and the Russian view, read for any identifier, role label, reason code, or identifier Location: none. The tags of the other side's units differ by view, as designed.
- **Play's panels:** the Rally, Prep Fire, Movement, Rout, Advance, and Close Combat Phases on the saved copies of the played game, in a side's view.
- **Sizes:** no window scroll on Play and on Replay at 1920x1080, 1568x677, 1366x768, and 1024x768; the tabs at 1366x580, 683x384, and 320 pixels with no sideways scroll.
- **Holding still:** the header 184 pixels high before a proposal, with it, and after Cancel; the map and the Propose button in the same place.
- **The map:** a zoomed Russian view handed to the German view and back kept its zoom; the Melee mark on H4 at step 179 of the played game.
- **Rout:** the Turn 3 Low Crawl the first play test timed at 25 to 30 seconds checks in 1.2 seconds and confirms in 1.7; the Rout Phase's end in 1.2 (it was 6 to 10).
- **The played game:** 613 revisions and 221 steps, after each task that touched the records or the steps.

## The second play test (task 31c.8)

**The game.** `p31c-play`: The Guards Counterattack from a new game to its end, both sides and their hand-overs, through the Play page in my Studio, driven by real clicks and typed values from a script. At the user's word it was played at the Studio pane's full size (2193 by 1195) so the user could follow; the sizes 1568x677 and 1366x768 were checked on Play and Replay before it. Setup by plan for both sides ("Forward line", answered by "Fire first"), the Russian non-OB "?", then five Game Turns. **Result: a German win**, stated in the header with its account of both Victory conditions and each building's Control.

**The numbers.**

| | First play test | Second |
|---|---|---|
| Time for five turns | 2:14 | 0:26 |
| Proposals | 218 | 190 |
| Confirmed | not counted | 135 |
| Refused | 21 | 51 |
| Hand-overs | not counted | 61 |
| A gate request's median time | not measured | 0.6 seconds; 9 of 325 took over 2 seconds |

The 26 minutes are a script's, not a player's, so they compare poorly with the first test's 2:14; what they do show is that nothing made the play wait.

**The refusals** (from the gate's audit log). 48 were fire refused as out of range or as Final Fire at a hex not adjacent (A8.4): my script ticked every firer and MG of a Location and tried targets blind. 2 were its own repeated pass. 1 was a Rally I proposed to read its refusal. None was a wrong refusal, and no action a player would make on purpose was refused. They do show a fault of the page: see finding 1 below.

**The hand-overs.** 14 of the 61 were a loop of my script in the first Rout Phase, where both sides act and each view is offered a hand-over. The user saw it and said so. It committed nothing; see finding 2.

**What was reached that the first test did not reach:** setup from a plan on each side; a fire group across two Locations (N2 with M2); the fire arithmetic before the dice on every attack; a SW Recovered (the LMG left in I5); Ambush (the German side ambushed in I7), with the ambusher's round and the ambushed side's round across a hand-over; a Battle Hardening choice answered; "Since you last looked"; the hand-over screen's map. **Not reached:** a Fire Lane, a SW transferred, Recombine, a withdrawal from Melee, a prisoner, a multi-level advance (J4 has no way from its ground level to level 1 in the Advance list), the adjudicator's view during play.

**The first test's problems, tried again.**

| Problem | Second test |
|---|---|
| P-01 a move that fire stops | Not met: no moving stack was broken while moving. Pass 31's fix stands as checked there. |
| P-02, P-05, P-16, P-26 fire corrections | Met in part: "already pinned" read correctly; a created leader did not come up. |
| P-03, P-22 the Advance list | The list offered each stack its ADJACENT Locations and said "CC follows" for an enemy Location. |
| P-04, P-10, P-25 the Close Combat form | Two rounds of Close Combat and an Ambush went through; a leader was taken with his squad as one target. No prisoner arose. |
| P-06, P-12, P-13 consequences | "No firer has a LOS ..., so the attack has no effect" came before Confirm. |
| P-07, P-09 who may act | Every action was taken from the view that may take it. |
| P-08 the result | Stated in the header and the actions pane, with its account. |
| P-11 fire at a Bypassing stack | Not tried; left out of both passes (ruling R10.7). |
| P-14 the attack's strength | **Fixed:** "Total 18 FP: the 16 column; DRM +3 (stone building TEM, A7.6) -2 (leadership, 10-2 leader R3, A7.531), +1 in all" before every Confirm. |
| P-15 fire results only under Records | **Fixed:** "Latest" gives the result and what it did. |
| P-17 the headline and rate of fire | **Fixed:** "4-6-7 squad G5 with its LMG in K4, level 1"; "The LMG kept its rate of fire and may fire again this phase (A9.2)"; "only its MG fires" in sight and disabled. |
| P-18 ids and codes | **Fixed** in the records, panels, review, units table, and Replay. Found and fixed during the test: the pending choice's text, the phase's start ("Turn 1, pfph, russian phasing"), sides in lower case, a Close Combat label that said the values twice. Left: finding 5. |
| P-19 refusals that mislead | The Rally refusal read "4-4-7 squad R6 has no Self-Rally capability, and only its own side's RPh gives one MMC a Self-Rally without it"; the rout status reads as a sentence. |
| P-20 three ways to write a Location | **Fixed:** one way, in selects, fields, records, and results. |
| P-21 a concealed unit named to the other side | **Fixed:** the German view read "A concealed unit in N2 fires at F5, level 1". |
| P-23 a hand-over round trip for each step | Left, at the user's word. |
| P-24 rout is slow | **Fixed:** a rout checked in 0.7 seconds and confirmed in 1.2. |
| P-27 no Melee marker | **Fixed:** "Melee" on I7 after the Close Combat. |
| P-28 the page moves | **Fixed:** no window scroll; the header one height; Propose and Confirm in their places. |
| P-29 the header covers the map's toolbar | **Fixed** with P-28: nothing sits under the header. |
| P-30 the map's view | **Fixed:** kept across hand-overs; the Residual FP marker ("2" in M3) small at the hex's corner, taking no click. |
| P-31 the header goes stale | The Review button shows only while a proposal waits; the status of the last commit stays until the next action. |

Each problem was not tried by the report's own steps one by one: those the game met are marked above from what the page showed, and those it did not meet are said as such.

**New findings.**

| # | Finding | What was done |
|---|---|---|
| 1 | The fire panel's Target list offers every Location the side knows to hold an enemy, whatever the range, and a refusal for range names neither the firer or MG nor the range and its limit. A player proposes to learn the range. | Backlog, with the user's idea: Range as a sibling of LOS in the inspector, and drawn on the map (question 2) |
| 2 | In a phase where both sides act (Rally, Rout, Close Combat) the header offers a hand-over in each view, with no cue whether the view on screen has anything left to do | Backlog |
| 3 | A fire group across two Locations whose second Location had no LOS was resolved as two attacks, the second with no effect, and nothing before Confirm said so | Backlog; the group itself was accepted and resolved as A7.52 has it |
| 4 | Choosing the Close Combat Location again, the same one, drops the attacks already declared, with no word | Backlog |
| 5 | The planner's own sentences still say "stone-building" and "open-ground", "enter" for one unit, and "PFPh" | Backlog |
| 6 | The Close Combat Phase can be ended with a Melee in a Location and no round fought there, with no warning | For the referee: backlog, with A11.15 to be read |
| 7 | A choice that waits hides the fire that caused it from "Latest" until it is answered | Backlog section 48's row stands |
| 8 | The pending choice named a unit by id and a side in lower case; the phase's start read as a code; the result's account said "russian Controls"; a Close Combat label read "4-6-7 squad G7 (4-6-7)"; a fire's result read "none" | Fixed during the test |

## At the user's word during the pass

- A Location field's three actions ran together on one line; each is on a line of its own.
- The hand-over screen's note spoke of setup in Turn 5, and its map had no units, as if before setup. The note now says what holds at that moment, and the map shows what both sides know (ruling R31c.3). No map is drawn during setup.
- The building entry block and its paragraph were on screen in every phase. The block is offered only in the moving side's Movement Phase, and its paragraph is a closed line.
- The play test was run at the pane's full size so the user could follow it.
- Range should be a sibling of LOS and be drawn on the map with it: backlog section 50, question 2.

## Tests

Written at the merge gate. The design's section 9 has the list; the time log has the counts.

## Left out

Backlog section 50.

## Questions for the user

| # | Question | Recommendation |
|---|---|---|
| 1 | The tags read like hex names: the German letter is G and the Russian R, and the board has hexrows G and R, so "4-6-7 squad G4 in G4" says two different things with one word. Keep "G4", or change the tag's form? | Change it, before anyone learns it: a number sign, "4-6-7 squad #G4", or the side spelled short, "4-6-7 squad Ger 4". I recommend "#G4": it stays short, and no hex is written with a sign. It is one line in `UnitNames`. |
| 2 | Range beside LOS, drawn on the map, and the fire panel's targets marked by range: in this pass before the merge, or as the next pass? | The next pass, with its own design: a range reading made public in the planner, a map layer, the inspector, and the fire panel. This pass is at its merge gate. |
| 3 | Does Claude in Chrome play another card before the merge? | Yes, The Tractor Works, which has hidden units and "?" on both sides: this pass's disclosure work (names, the fire preview, the hand-over map) is best tried by a player who is not me, on a card that hides more than The Guards Counterattack does. |
