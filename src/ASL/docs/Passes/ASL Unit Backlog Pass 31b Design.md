# ASL Unit Backlog Pass 31b Design

**Status:** Built 2026-10-04 on branch `feature/asl-backlog-pass-31b`, with the user's answers (section 11), every one as recommended: tasks 31b.1 to 31b.8, three reviews and their fixes, and the Studio check. Section 12 says what was built and where it differs from this design; the [review document](<Scenario A1 Backlog Pass 31b Review 2026-10-04.md>) has the reviews and the check. Pass 31b (the Replay page) of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 5, added by the user on 2026-10-04. It takes the number 31b; the pass that held it (the page's words, layout, map, and records, from the play test) becomes pass 31c and keeps its design, section 5 of the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>).

**Date:** 2026-10-04

**Related documents:** the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>) and its [review](<Scenario A1 Backlog Pass 31 Review 2026-10-04.md>); the [pass 28c design](<ASL Unit Backlog Pass 28c Design.md>) (the Play workspace) and the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>) (the shared board workspace); the plan's sections 15.2, 15.4, and 15.9 to 15.11; the pass 30 handover prompt (`ASL Pass 30 Handover Prompt.md`, deleted 2026-10-05; in git history at 038dca8), whose standing rules and harness lessons apply unchanged; the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 44, 47, and 48.

A Studio pass: it adds no rules and changes no game. Its disclosure rule (section 6) is a ruling, R31b.1.

## 1. Outcome

A new page, Replay, plays a recorded game back on the map, one action at a time. For each step it shows what was done, its dice and arithmetic, and what changed, in the view of a side or of the adjudicator. A player steps forward and back, jumps to a phase or a turn, or lets it run at a pace. From any step an ended game can be opened as a new game that plays on from there.

It is built from parts the Studio already has. Three things are new: the grouping of a game's events into steps, the difference between two states drawn on the map, and the controls that move through the steps.

## 2. What a game's replay is today

- **A game is its events.** `GameProjector.Project` rebuilds the state at every revision and checks each record against a recomputation; `GameHistory` holds every state (`At(revision)`).
- **The Game states page** (`/units/games`) shows one revision at a time for a chosen view: a units table, the events that view may read as raw types, and a case read. It has no map, steps by revision, and speaks in the model's terms. It is a developer's inspector.
- **The Play page** always shows the last revision. Its records are the game in a player's words, but as lists under the map, latest first.
- **The board viewer** draws a live game at a revision and view named in its address.
- **Nothing plays a game back.** Nothing groups revisions into actions, says what changed between two states, or moves through a game in order. In pass 31 each check of a problem at its own moment was made by copying the game file and cutting it back by hand.

## 3. What is taken from where

| From | What | Its use in Replay |
|---|---|---|
| Game states | `GameLibrary.Load`, `ViewOf(history, revision, view)`, `Projection` | The game, and its state at a revision as a view may know it |
| Game states | `GameReplayToolbar` (game, view, revision), `RevisionNavigator` (Shared), `GameContextSummary` | The toolbar's game and view pickers; the navigator's keyboard handling is the model for the transport |
| Game states | `ProjectedGameUnitTable` | The units at the step, in a tab |
| Play | `BoardWorkspace` with `BoardInspector` (pass 29) and `GameMaps.Layers(game, map, viewer, revision)` | The map and its Selection, LOS, and Evidence tabs. `Layers` already takes a revision; Play always gives it the last |
| Play | The record builders in `Play.razor`: fire (`FireRecords`, with `FireResolutionCard`, `FireArithmeticBreakdown`, `InfantryFireEffectsTable`, `VehicleFireEffectsTable`), Close Combat, rally and rout (`ActionRecordList`), dice (`DiceRollHistory`), and `WhenOf` | A step's record in a player's words. They move out of the page into a service that both pages read (task 31b.2) |
| Play | `PlayContextHeader` and `GameLine`: "Turn N of M", the Ended badge, the result line and its "Why" (pass 31) | The header |
| Play | `VictoryStandingTable` and `ScenarioVictory`'s account (pass 31) | The standing at the step, in a tab: how the score stood as the game went |
| Play | `PlayCardPanel` | The card, from the header's link |
| Play | `HandOverScreen` and the page's hand-over state | A view change in a game that has not ended |
| Play | The hand-over screen's lines (pass 31, `ReadHappened`): a step said without a unit's name | The timeline's line for a step a view may not read in full |
| Board viewer | A game, view, revision, and hex in the address | A step has an address, so it can be linked and reloaded |
| Pass 31's tests | The played game as a fixture, cut back to a revision | The tests' game, and the model for "play on from here" |
| The planner | An attempt's id, shared by every event of one proposal (`fire-a672887feaab-1`, `-2`, ...) | The boundary of a step |

What is not taken: the case read (it stays on Game states), anything that proposes or confirms, and the setup mode.

## 4. Decisions

**D1. A page of its own, in the Play group.** `/games/replay?game=<id>&step=<n>&view=<view>`, listed under Play as "Replay", between Play and Game states. Play links to it: "Replay this game" once a game has ended, and "Replay from here" beside a record. Game states stays as it is, a developer's inspector (question 6).

**D2. A step is one confirmed proposal.** Every event of one attempt is one step: a fire with its dice, its record, the conditions it changed, and the Residual FP it left is one step, as it was one Confirm. Each step has the revisions it covers, its turn, phase, and phasing side, the side that took it, and a title in words. Above the steps are the phases, the Player Turns, and the Game Turns, read from `phase-changed`.

- Setup is one step for each setup proposal, shown only to the views that could see it (D6).
- The step's title comes from its record: "Russian Prep Fire: G4, level 1 at H5, level 1: NMC", "A German stack enters H5", "The DEFENDER passes", "The Russian side ends the Movement Phase".
- A step is the unit of the transport. A stack's whole move (several steps with the DEFENDER's fire between them) is not one step; "next phase" skips over it (question 3).

**D3. The page is the Play workspace, read-only.** The same three regions, so a player who knows Play knows Replay:

- **Left, the timeline:** the game as Turn, Player Turn, phase, and steps, the present step marked; a click goes to a step. It takes the place of Play's actions pane.
- **Center, the map:** `BoardWorkspace` at the step's last revision, for the view. Pan, zoom, fit, rotate, click a hex or a counter, the LOS check: as on Play.
- **Right, the step:** what was done and its record, with the dice and arithmetic the Play page's records show; then the inspector's Selection, LOS, and Evidence tabs; then tabs for the units and for the Victory standing at the step.
- **The header:** the game, the view, "Turn N of M, <side> <phase>", "Step 142 of 240", the transport, and the result once the last step is shown.
- Under 1024 pixels: the step above labelled Map, Timeline, and Details tabs, as Play's narrow layout.

**D4. What changed is drawn and said.** Each step carries the difference between the view before it and the view after it:

- A unit that moved: an arrow from its Location to its new one.
- A fire: a line from the firers' Location to the target Location, in the LOS line's style.
- A unit that broke, was pinned, was eliminated, was captured, rallied, or lost or gained its "?": its hex outlined, and a line in the step panel ("r-squad-20 broke").
- A counter placed or removed (Residual FP, a created leader, a HS from a Deployment): the same.
- The map centers on the step's Locations when they are out of sight, and otherwise holds still.
- The difference is computed from the two `GameView`s, never from the full state, so a view's marks show nothing the view may not know.

**D5. The transport.** First, back, forward, last; previous and next phase; previous and next Game Turn; a number field for a step. With the map or the timeline focused: Left and Right step, Home and End go to the ends, Page Up and Page Down go a phase. "Play" runs forward at a chosen pace (a step each 1, 2, or 4 seconds) and stops at the end, at any key, or at a click; with reduced motion asked for, it steps without the map's glide. The address follows the step, so Back and a reload return to it.

**D6. Whose view** (ruling R31b.1; question 2).

- **A game that has ended:** any view may be chosen freely, and the adjudicator's is the default. Nothing is left to hide: Control is declared at the end (ruling R23.4), and the result is public. A side's view is then a way to study what that side knew at each step.
- **A game that has not ended:** the views are as on Play. A side's view opens behind the hand-over screen, and a view change goes through it. The adjudicator's view is offered as Play offers it.
- **A step a side's view may not read:** its events are withheld from that view (a hidden placement, a concealed unit's rally). The timeline shows no line for it and the step count is the view's own, so a view cannot count the other side's hidden actions. A step read in part (fire at concealed units with no effect) shows the public part, as Play's records do.
- Every panel, the timeline, the marks, and the address read the view. A view change clears the step's marks and the picked hex.

**D7. The records are read from one place.** The record builders move from `Play.razor` into a service (`PlayRecords`, in Services) that takes a history, a view, and a last revision. Play calls it with the game's last revision and shows what it shows now, with its tests unchanged; Replay calls it for the step. No record is written twice.

**D8. Play on from here** (question 4). On an ended game, or from the adjudicator's view, "Play on from here" makes a new game: the events up to the step, under a new name the player gives, opened on the Play page behind its hand-over. The dice from then on are new. The first game is not changed. A game made this way says where it came from in its label ("from guards-dl-01 at step 142"). This is the cut I made by hand ten times in pass 31, and backlog section 47's "Rehearse" row asks for something near it.

**D9. What a replay does not judge.** A replay shows what was recorded. It does not ask whether today's rules would allow a step: the played game's Turn 1 Russian Deployment replays, though ruling R31.4 now refuses it. A mark for "today's rules would refuse this" is in the backlog.

## 5. Steps in the code

- `ReplaySteps` (Services): from a `GameHistory` and a view, the list of steps and their grouping. A step: its index, first and last revision, attempt id, kind, turn, phase, phasing side, acting side, title, and whether the view reads it in full, in part, or not at all.
- An attempt's events are those whose id is the attempt's id and a number. Events an action adds to a plan (DM from an ADJACENT enemy, a SMC armed again, the game's end) carry the same attempt id and so belong to its step.
- `ReplayDiff` (Services): from the views before and after a step, the units that moved, changed condition, appeared, or went, as marks for the map and lines for the panel.
- The page keeps the step, the view, the pace, and the open tab. It caches the map's layers by game, revision, and view, as Play does.

## 6. Disclosure

Rulings R23.1 to R23.6 stand, and ruling R31b.1 (D6) adds the rule for an ended game.

- The state shown is always `ViewOf(history, revision, view)`.
- The steps a view sees, their count, their titles, and the timeline come from the events that view is entitled to.
- The marks come from the difference of two views.
- "Play on from here" is offered only where the whole game may be known: an ended game, or the adjudicator's view.
- The referee's review reads the page on a game with concealed and hidden units on both sides, in each side's view, before and after its end.

## 7. What stays out, for the backlog

| Item | Why |
|---|---|
| A mark on a step that today's rules would refuse | D9; it needs each step proposed again against the planner |
| Notes on a step, kept with the game | A place to keep them that is not the game's record |
| A replay exported as a file or a film | The address of a step is the link for now |
| Two views side by side at one step | One view at a time |
| A stack's whole move as one step | D2; "next phase" passes over it |
| The Game states page moved out of the Play group | Question 6 |

## 8. Tests

Written at the merge gate. The Studio on port 6670 is the only test until the code is complete.

- `ReplayStepsTests` (Studio): the played game (`guards-dl-01`, 613 revisions) gives the same steps each time; each of its attempts is one step; the steps' phases and turns follow the game's; a side's view has no step for an event it may not read, and its count differs from the adjudicator's.
- `ReplayDiffTests`: a move, a fire that breaks a unit, an elimination, a capture, a created leader, each as marks and lines; a concealed unit's change gives nothing to the other view.
- `ReplayPageTests` (bUnit): the page opens at a step from its address; forward, back, phase, and turn; the keyboard; the view picker on an ended game with no hand-over, and on a game not ended behind it; the step's record equal to Play's record of the same attempt; "Play on from here" making a game that the Play page opens.
- Play's own tests pass unchanged after the records move to the service.

## 9. The Studio check

On the played game and on one game with hidden units (The Tractor Works or Armor Test): every step from first to last in the adjudicator's view; each side's view before and after the end; at 1920x1080, 1366x768, 1568x677, 1024x768, 683x384, and 320 pixels; by keyboard and by mouse; autoplay through a whole Game Turn; "Play on from here" at three steps, each then played one action on the Play page.

## 10. Tasks and estimate

| Task | What it changes | Estimate |
|---|---|---|
| 31b.1 Steps | `ReplaySteps`: a game's events grouped into steps by attempt, with their phases and turns, titles, and what each view reads of them | 1:30 |
| 31b.2 The records as a service | The record builders moved from `Play.razor` into `PlayRecords`, read for a history, a view, and a last revision; Play unchanged in what it shows | 2:00 |
| 31b.3 The page | The route, the game and view pickers with the rule of D6, the header, the map on `BoardWorkspace` at a step, the units and standing tabs, the narrow layout | 1:30 |
| 31b.4 The timeline and the transport | The timeline, the step panel with its record, first, back, forward, last, phase, turn, the keyboard, the address | 1:30 |
| 31b.5 What changed | `ReplayDiff`, its marks on the map, its lines in the step panel, the map centering on the step | 1:30 |
| 31b.6 Running it | "Play" at a pace, stopping, reduced motion | 0:30 |
| 31b.7 Play on from here | A new game from the events up to a step, named by the player, opened on Play | 0:45 |
| 31b.8 The links | "Replay this game" and "Replay from here" on Play; the navigation | 0:30 |
| | Overhead: three reviews and their fixes, the Studio check, the documents, the tests, the merge gate | 1:30 |
| | **Pass 31b total** (build 9:45) | **11:15** |

Recent passes ran at about a third of their estimates. The largest risk is task 31b.2: `Play.razor` is about 6,700 lines, and its record builders read the page's own view and caches.

## 11. The user's answers

Given 2026-10-04, each as recommended. None changes a decision, a task, or the estimate.

| # | Question | Answer |
|---|---|---|
| 1 | The pass that held the number 31b | It is pass 31c, unchanged, and follows this one. It holds the rest of the play test's problems: unit names, picking on the map, the fire proposal, the layout, the map, records, rout speed, and the full second play test. |
| 2 | Whose view (D6) | An ended game opens in the adjudicator's view and any view may be chosen freely; a game not ended keeps Play's views and hand-over. Ruling R31b.1. |
| 3 | What a step is (D2) | One confirmed proposal, with jumps by phase and by Game Turn. A stack's whole move as one step stays in the backlog. |
| 4 | Play on from here (D8) | Built, for ended games and the adjudicator's view. |
| 5 | Running at a pace (task 31b.6) | Built. |
| 6 | The Game states page | Left as it is in this pass; its place in the navigation is decided in pass 31c, with the layout. |
| 7 | The order | Accepted: until pass 31c the Replay page shows unit ids and Locations as Play does today; pass 31c's names then reach both pages through the shared records service. |

## 12. Pass 31b as built

**Built 2026-10-04,** tasks 31b.1 to 31b.8, one commit a task, each checked in my Studio on port 6670 before its commit; then the three reviews' fixes and the Studio check of the whole pass. The [review document](<Scenario A1 Backlog Pass 31b Review 2026-10-04.md>) lists what a player meets, the reviews' findings and their fixes, and the check. Rulings R31b.1 and R31b.2. No rule changed, and no recorded game replays differently.

**As designed:** D1 (a page of its own, in the Play group, linked from Play), D2 (a step is one confirmed proposal), D3 (the Play workspace, read-only), D4 (what changed, drawn and said), D5 (the transport, the keys, running at a pace, the address), D6 (whose view), D7 (the records read from one place), D8 (play on from here), D9 (a replay does not judge).

**The code.**

| Part | Where | What it holds |
|---|---|---|
| `ReplaySteps` | Services | A view's steps of a game and their phases; a step's kind, acting side, title, fires, and Locations; the jumps by phase and by Game Turn (`ReplayTimeline`) |
| `ReplayDiff` | Services | The difference of two views with the step's entitled events: its lines and its marks |
| `PlayRecords` | Services | The Play page's records, read for a history, a view, and a last revision; `RollsIn` for a step's rolls |
| `LivePlay.PlayOn` | Services | A new game from another's events through a revision |
| `GameMaps.Layers` | Services | A second form for a game already loaded, so a replay loads its game once |
| `Replay.razor` | Components/Pages | The page: the address, the game, the view and its hand-over, the step, the run, the caches by step and view |
| `ReplayTransport`, `ReplayTimelineList`, `ReplayStepPanel` | Components/Replay | The transport, the timeline, and the step's panel |
| `replay.js` | wwwroot/js | The keys, the stop of a run, and the step shown brought into view |
| `BoardWorkspace.Reveal` | Components/Board | Brings a point of the map into view when it is out of sight |
| `ActionRecordList`, `FireHistory` | Components/Play | An optional link beside a record, which Play gives |

**What differs from the design:**

- **A side's steps (ruling R31b.2).** D6 left out every step a view may not read. Two cases were settled in the build and the referee's review. A step the view reads nothing of is still a step when the view sees the map change over it, as the other side would at the table: a "?" placed where a hidden unit stood, the non-OB "?", the other side's setup once it comes into sight; it says only what the map showed. And a step whose public events say only that something minor was done (a weapon changing hands, a marker changing) is a side's step only when that view sees a change, so the timing of what is done under "?" stays its owner's.
- **"Read in part"** is said only of fire, whose public report is table knowledge; on any other step it would point to what the view may not read.
- **The step's number in the address is the view's own,** and Play's "Replay from here" names the record's event instead (`at`), which the page turns into the step of the view shown.
- **The board viewer's link,** which carries the revision, is given only for an ended game or in the adjudicator's view; a side's view of a game still played shows no revision anywhere.
- **An ended game opens at its first step** in the adjudicator's view; a game still played opens at its last, behind the hand-over. D6 did not say which step.
- **The Victory standing at a step** is read only while its tab is open, and for the result at the last step: read at an earlier step it folds the game again, about 4.5 seconds late in the played game. Each standing read is kept. The card panel on this page has no standing.
- **A run** stops at any key and at any click, as designed, but for the pace, which may be changed while it runs. The map has no glide to drop under reduced motion: it jumps to a step's Location for everyone; the timeline's scroll glides and does not under reduced motion.
- **The header** reads the step's own turn and phase, the ones it was taken in, not the state after it; a phase's last step is its ending.
- **Play on from here** is not offered from a step at which the game had ended. The new game's name defaults to the game's with the revision; its label reads "from guards-dl-01 at step 60, revision 188".
- **The timeline** has Game Turns and phases in one heading ("Turn 1, Russian Player Turn: Defensive Fire Phase"), not a level for each Player Turn.
- **Unit ids and raw Locations** stand in the records until pass 31c, as the user accepted (answer 7); the titles and the lines of what changed already say Locations in words.

**Tests** (section 8) are written at the merge gate.
