# Scenario A1 Backlog Pass 31b: The Replay Page

**Date:** 2026-10-04

**Branch:** `feature/asl-backlog-pass-31b`

**Related documents:** the [pass 31b design](<ASL Unit Backlog Pass 31b Design.md>) (sections 4, 6, 11, and 12), the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>) (pass 31b and section 15.12), the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 49, and the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), rulings R31b.1 and R31b.2.

## Status

Built, reviewed, and checked in the Studio; the merge gate follows. A new page, `/games/replay`, plays a recorded game back on the map one step at a time, in a side's view or the adjudicator's. It reads the game's record and changes no game. The pass touched no rule and no replay of a recorded game: the rulebook was not needed, and no task asked for a rule change.

## What was built

| Task | What a player now meets |
|---|---|
| 31b.1 Steps | A game's events grouped into steps, one for each confirmed proposal (an attempt), with the revisions it covers, its turn and phase, the side that took it, and a title that names sides and Locations and never a unit: "Russian Prep Fire: F3, level 1 at F5, level 1: 2MC", "A Russian stack moves to M3 by Assault Movement", "The German side, the DEFENDER, declines First Fire", "The German Close Combat Phase ends; Game Turn 2 begins". The played game has 221 steps in 81 phases. |
| 31b.2 The records as a service | The Play page's record builders moved into `PlayRecords` (Services), read for a history, a view, and a last revision. Play shows what it showed: its Records block on the played game was the same markup, character for character, in every view, before and after the move. `Play.razor` is 632 lines shorter. |
| 31b.3 The page | The Play workspace, read-only: Play's context header with the step, the turn of the card's turns, the Ended badge, and the result at the last step; the game and view pickers; the map on `BoardWorkspace` at the step's revision for the view; the inspector's Selection, LOS, and Evidence tabs with Step, Units, and Standing; the card panel; Map, Timeline, and Details tabs under 1024 pixels. |
| 31b.4 The timeline and the transport | The game as Game Turns, phases, and steps, the step shown marked; back and forward a step, a phase, and a Game Turn, first and last, and a typed step; Left and Right, Page Up and Page Down, Home and End with the map or the timeline focused; the address following the view and the step. The step panel shows the step's rolls and the Play page's own records of its attempt. |
| 31b.5 What changed | The difference between the view before the step and the view after it, said in the step panel and drawn on the map: a blue arrow for a move, a red dashed arrow for fire, an amber outline for another change. The map comes to the step's Location when it is out of sight. |
| 31b.6 Running it | "Play" runs forward a step every 1, 2, or 4 seconds, and stops at the last step, at any key, at a click, and at a step or view chosen by hand. |
| 31b.7 Play on from here | A new game holding the game's events through the step shown, under a name the player gives, opened on the Play page behind its hand-over. Its label says where it came from. The first game is only read. |
| 31b.8 The links | "Replay" in the navigation's Play group; on Play, "Replay this game" once a game has ended and "Replay from here" beside every record; "Open on Play" on the Replay page. |

## Rulings

R31b.1 (whose view) and R31b.2 (what a side's steps are), in section 5 of the Backlog Passes Plan.

## The three reviews

Three read-only reviews read the branch at commit b688d33. The referee found the record builders a faithful move, with one deliberate change: the state before a Close Combat round is read from the history, not replayed again.

**Referee** (1 blocker, 1 fix, 5 notes):

| # | Finding | Fix |
|---|---|---|
| 1 | A side's view got a step for any attempt holding one public event, even when nothing the side could see had happened: a SW passed between two concealed units showed as a step, so the other side learned that, when, and how often it was done under "?" | A step that says only that something minor was done (a weapon changing hands, a marker changing, a Rally Phase action, anything the page has no words for) is a side's step only when that view sees a change (ruling R31b.2) |
| 2 | The link to the board viewer carried the revision for every view, so a side could count what happened between two of its steps | The link is given only for an ended game or in the adjudicator's view |
| 3 | "Read in part" on a setup step pointed to a hidden unit or a Bore Sighted Location in that setup | Said only of fire, whose public report is table knowledge (A12.14) |
| 4 | The Units tab printed a custodian's or a container's id without checking the view held that unit | "a unit not in view" |
| 5 | "became X and Y" named units from the event, whether or not the view held them | Only the units the view holds after the step |
| 6 | Terms: every fire at a moving stack was "Defensive First Fire"; a single unit a "stack"; "Turn N begins" | Fire is named by its kind; a unit or a stack; "Game Turn N begins" |
| 7 | Several of Play's records name a unit without the view check (Deploy, Recovery, a SW left, dismantle, a public Rally): not from this pass, and Replay shows them as Play does | Backlog section 49 |

**Table player** (15 findings):

| # | Finding | Fix |
|---|---|---|
| 1 | An ended game opened on its last step, with "Play" disabled | It opens at its first step unless the address names a step or a record |
| 2 | Subsequent First Fire, Final Protective Fire, Bounding First Fire, and Overrun were never named | Named from the attack's recorded kind; Opportunity Fire's own attacks are not told from Advancing Fire (backlog) |
| 3 | The header read the state after the step and the step panel the phase it was taken in | Both read the step's own turn and phase |
| 4 | A unit going under "?" read as "no longer in this view" beside "a ? appeared" | "went under "?"", and "A German "?" in G3 was revealed: g-squad-3"; a "?" removed "is gone from" its Location |
| 5 | DM, Disruption, Fanatic, wrecks, weapons, and Residual FP were not among the changes | Added; CX, TI, and vehicle conditions stay out (backlog) |
| 6 | "became" did not say why | "was Reduced to", "was Replaced by", "Deployed into", "Recombined into" |
| 7 | Event names as titles ("Instance eliminated", "Sniper attacked") | Titles for an elimination, a Sniper, a massacre, a freed prisoner, a Gun's turn, a weapon changing hands, a Wind Change DR |
| 8 | A step of created units after play started would have read as "Setup" | Setup is only what comes before play |
| 9 | "The DEFENDER passes" named no side | "The German side, the DEFENDER, declines First Fire" |
| 10 | Titles dropped what the record knew | A unit or a stack, Assault Movement, Low Crawl, Self-Rally, a repair's result, the building Mopped Up, the round of a Close Combat; the choice's kind and the Close Combat's outcome stay in the record |
| 11 | Any click or key stopped a run, the pace among them | The pace may be changed while it runs; the rest is as the design has it |
| 12 | "Russian Defensive Fire Phase" beside "German Final Fire" | The timeline's headings read "Turn 1, Russian Player Turn: Defensive Fire Phase" |
| 13 | A record's link landing on the last step with no word when the view has no such step | The page says so |
| 14 | Texts | The "Play on from here" note says the revision and the phase the new game resumes in; two sentences rewritten |
| 15 | Later work | Backlog section 49 |

**UI and Blazor** (8 to fix, 6 notes):

| # | Finding | Fix |
|---|---|---|
| 1 | The address reaches the page a round trip after the page's own move, so two quick steps could be pulled back by the first | The address is not followed while the page's own move is on its way |
| 2 | A reload of a game still played lost its step | The first view opens on the step the address names |
| 3 | Every step re-sent the click handlers of all the timeline's buttons | Made once for a timeline; the list is drawn again only when the steps or the step shown change |
| 4 | A disabled transport button dropped the keyboard focus | `aria-disabled`, and the button keeps the focus |
| 5 | Every step was a tab stop | The step shown alone |
| 6 | The step was announced at every step of a run; "Stop, pressed" | Not announced while it runs; the button's label alone |
| 7 | The page left during its first render kept a listener on the document; a reveal pending at disposal could throw | A disposed flag checked after each await; the exception caught |
| 8 | A typed name and a refusal carried into the next game or view; the card kept across games | Cleared |
| notes | A run reaching the last step waited one pace more; a step from the address did not stop a run; the step panel's lists were filtered on every render; the live games were listed on every render; the reorder under 1024 pixels differed from the tab order | Fixed: the step controls now come first in the page's own order |

The UI review also listed the existing tests the links on Play change; the merge gate brings them in line.

## The Studio check

In my Studio on port 6670 (built into its own output folder, since the user's Studio held the usual one):

- **Every step:** the 221 steps of `guards-dl-01` in the adjudicator's view, each with its title, record, and marks; no fire without its arrow, no move without a mark.
- **Each side's view:** of the played game before its end (`p31-end`, 612 events, behind the hand-over, with no revision, no board link, and no "Play on from here") and after it (any view at once, the result at the last step, the standing read for the side until the end); of The Tractor Works with hidden and concealed units on both sides (`p30b-tw-1`: the German view reads the Russian setup as "The Russian side sets up" with only the "?" it saw); of `p23-hidden` (the hidden placement is the German side's step, and the Russian view's "A German "?" appeared in F5").
- **Widths:** 1920x1080, 1366x768, 1568x677, 1024x768, 683x384, and 320 pixels, with no sideways scroll at any.
- **Keyboard and mouse:** each transport button, a click on a step, a typed step, each key from the map, the timeline, and a step, and eight key presses in a row.
- **A run:** the whole of Game Turn 1 at a step a second: 45 steps in 46 seconds, none skipped, the step shown kept in view in the timeline.
- **Play on from here:** at steps 5, 60, and 150 (the last from the German view of the ended game); each new game opened on Play behind its hand-over and was played one action. The played game kept its 613 events.

Not done, and said so: every step in each side's view (the sides' views were read at the steps where they differ from the adjudicator's); a game with a SW passed under "?", which no saved game has, so the rule of ruling R31b.2 is in the tests.

## Tests

Written at the merge gate: `ReplayStepsTests`, `ReplayDiffTests`, and `ReplayPageTests` (Studio), with the played game as a fixture. The design's section 8 has the list; the time log has the counts.

## Left out

Backlog section 49.
