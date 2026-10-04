# ASL Unit Backlog Pass 31c Design

**Status:** Built 2026-10-04 on branch `feature/asl-backlog-pass-31c`, with the user's answers (section 12), every one as recommended: tasks 31c.0 to 31c.8, three reviews and their fixes, the Studio check, and the second play test. Section 13 says what was built and where it differs from this design; the [review document](<Scenario A1 Backlog Pass 31c Review 2026-10-04.md>) has the reviews, the check, and the play test's report. Pass 31c (Play-test UI II: the page reads and holds still) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5. It brings decisions D11 to D18 of the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>), section 5, up to date with what passes 31 and 31b built, and adds the Game states decision as its first task. Tasks "31b.1" to "31b.8" of that design are tasks 31c.1 to 31c.8 here.

**Date:** 2026-10-04

**Related documents:** the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>) (sections 5, 6, 10, and 11) and its [review](<Scenario A1 Backlog Pass 31 Review 2026-10-04.md>); the [pass 31b design](<ASL Unit Backlog Pass 31b Design.md>) (section 12) and its [review](<Scenario A1 Backlog Pass 31b Review 2026-10-04.md>); the plan's sections 15.2, 15.4, and 15.9 to 15.12; the [pass 30 handover prompt](<ASL Pass 30 Handover Prompt.md>), whose standing rules and harness lessons apply unchanged; the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 44, 45, 48, and 49; the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), rulings R23.1 to R23.6, R31.1 to R31.8, R31b.1, and R31b.2. The play test's report and screenshots are in `C:\Users\dland\Downloads\Playability-Report-2026-10-04\`, outside the repository.

A Studio pass. It changes how the page words, lays out, and draws a game. It changes no rule's result and no recorded game: section 7 says how that is kept.

**The rulebook comes first.** Where a task touches a rule, its passage in the PDF is read before the change and cited. Read for this design: A7.5, A7.52, A7.53, A7.54, A7.55 (p. 55 to 56 of the PDF's text), and A8.4. They are read again at task 31c.3.

## 1. Outcome

A player reads "4-6-7 squad G4 in G4, level 1", never `fire-a672887feaab-g-squad-1` in `bd01:G4:1`. A Location is picked on the map. A fire proposal shows its firepower, column, and DRM before the dice. The workspace does not move when a proposal lands. The map keeps its view. The latest line says what happened and how it came out. A rout is checked in under two seconds. Play and Replay read the same words, from one place.

The Game states page becomes a developer's inspector outside the Play group, and neither it nor the board viewer opens a game still being played in a view chosen at will.

## 2. What has changed since D11 to D18 were written

- **The records are worded in one place.** `Services/PlayRecords.cs` words a game's records for a history, a view, and a last revision; Play and Replay both read it (plan section 15.12). Names and Locations in words go there and into `DisplayText`, and reach Replay's step panel with no second change.
- **Replay has two more wordings of its own.** `ReplaySteps` writes titles, which name sides and Locations and never a unit; `ReplayDiff` writes what changed and names units by id in 15 places. Both take this pass's words.
- **A Location is already written two ways in words.** `DisplayText.Location` gives "G4 on board 01, level 1" always; `ReplaySteps.Place` gives "G4, level 1" on a one-board map. D12 makes the second the one way.
- **A record on Play carries a "Replay from here" link.** Tests read a record's own text with `RecordText()`.
- **The planner already forms a fire group across Locations.** `LiveFire.FromState` reads each firer's own Location, and the calculator's precheck refuses a group whose Locations are not ADJACENT. Only the page holds a group to one Location: its "From" is one select. D14's group is a page change.
- **The calculator already has the arithmetic without dice.** `ScenarioA1FireCalculator.Preview` returns each firer's FP with its multipliers, the column, and the DRM. Only the Encirclement check calls it.
- **The map already keeps its view over a revision or a phase change.** `BoardWorkspace` loads the board again only when the board or the view changes. The view is lost at a hand-over, because the hand-over screen does not render the workspace at all, and a new one fits the map.
- **The hand-over screen opens the arriving view on the Location the game waits on** (pass 31, task 31.9). D16 must not undo that.
- **The rout search keeps nothing between calls,** and three things under it are made anew each time: the composed map (`GamePlanner.Composed`), the board handle the LOS cache is keyed by (`StudioBoardCatalog.TryGetBoard`), and the location chains. The rout panel also runs the search for every broken unit on every render.

## 3. What the code does now

| Area | Now | Where |
|---|---|---|
| Unit names | Ids in records, pickers, the units table, and what changed; `UnitLabels.AccessibleName` for a counter's accessible name only | `PlayRecords` (47 places), `ReplayDiff` (15), `Play.razor` (11 `PlayChoice` labels), `FireText` |
| The view check of a name | `PlayRecords.Open(before, id)` on DM, Failure to Rout, and a SW passed; not on Deploy, Recovery, a SW left, dismantle, a public Rally | `PlayRecords.cs:385` to `:446`, `:689` |
| Locations | Selects in words; typed fields in the identifier form; records in the identifier form; a pattern on Play turns identifiers in a planner's text into words | `DisplayText.Location`, `Play.razor:2085` |
| Typed Locations | 5 `LocationField`s (Move, building entry, placing a DC, setup, Bore Sighting); 9 plain inputs (rout route, fire's free target, Spray, Fire Lane, Starshell, a thrown DC, a vehicle's next hex, two in setup) | `RoutActionPanel`, `SmallArmsFirePanel`, `StarshellActionPanel`, `ThrowDemolitionChargeAction`, `VehicleStepChoices`, `SetupPlacementList`, `Play.razor:789` |
| The picked hex | Cascaded to the actions pane; a `LocationField` offers "Use <hex>" at ground level | `Play.razor:325`, `LocationField.razor` |
| The fire proposal | A fact table: phase, group, target, range, level, LOS, terrain. No FP, column, or DRM | `FireReviewFacts.razor`, `Play.razor:276` |
| "MGs alone" | Shown only once a firer and its MG are ticked | `SmallArmsFirePanel.razor:31` |
| Rate of fire kept | Decided and recorded (`FireWeaponEffect.RateOfFireRetained`); said nowhere for a MG | `ScenarioA1FireCalculator.cs:2391` |
| The window | The introduction, the scripted dice, the card panel, the units table, and the adjudicator's audit sit outside the workspace grid, so the window scrolls | `Play.razor:21` to `:56`, `:842` to `:845` |
| The header | Sticky, of its content's height; the workspace sizes itself from the height measured | `site.css:2189`, `playWorkspace.js:60` |
| A new proposal | The Proposal tab is chosen, and the pane is scrolled into view and focused | `Play.razor:6053`, `playWorkspace.js:5` |
| Propose and Confirm | Each panel's Propose is the last control of its own toolbar; Confirm and Cancel follow the outcome and the consequences | `ProposalReviewPanel.razor:22` |
| The Residual FP marker | A circle of radius 14 at the hex's center, in the marks layer, taking pointer events; not listed in the Selection tab | `GameMaps.cs:115` |
| Melee | No mark on the map | |
| "Latest" | The newest of the ordnance, Close Combat, night, Sniper, Rally and Repair, and fire records; a fire reads "<group> fires at <target>." with no result | `PlayRecords.Latest` |
| Game states | Any view of any game at once, a game still played too | `Games.razor` |
| The board viewer | Its own view picker and revision stepping for a live game | `BoardViewer.razor:55`, `:258` |

## 4. Decisions

### D0. The Game inspector (the user's answers of 2026-10-04)

The user took option (b) and the five points with it.

- **The page stays, renamed "Game inspector",** at `/units/games`, so links and documents hold. It moves out of the Play group into a new Verify group with Fidelity.
- **A game still being played is not listed** on it. The embedded fixture, saved synthetic games, and ended live games are. The page says so in a line that links to Play and to Replay; its address names no game.
- **The board viewer closes the same gap.** A game still played is not in its source picker. It opens there only through Play's or Replay's link, in that page's view, with no view picker and no revision stepping. A side's view of such a game is shown no revision, and the link carries none: the viewer shows the game's latest state.
- **An ended game** may be read from any view on both pages, as ruling R31b.1 has it. The inspector's event list keeps its revision numbers.
- The inspector keeps the case read, the fixtures, stepping by revision, the raw event types, the findings, and "Show on board".

An address typed by hand can still name another view. Play is hot-seat on the honor system (R23.2); what this closes is a picker one click from Play that skips the hand-over.

### D11. A unit has a name (P-18)

`UnitNames` (Services) gives every unit one name wherever a player reads it: its printed values, its kind, and a tag, such as "4-6-7 squad G4" and "9-2 leader G2". It is built for a history and a view, as `PlayRecords` is, and both pages and the records read it.

**The values** are the unit's own at the moment the text speaks of, so a squad Replaced by a lower class changes its values and keeps its tag. Role labels ("attacker-squad") give way to the name.

**The tag** is the side's letter and a number, and never changes once given.

- **A side's own units** are numbered within the side and the kind in the order they entered the game. A unit that takes another's place keeps its tag: a Replacement, a Reduced unit, a Battle Hardened leader, a unit freed or rearmed. A Deployed HS takes its squad's tag with "a" or "b", and the squad Recombined from them takes the tag back. A created leader or hero takes the next number.
- **The other side's units are numbered in the view's own order:** the first enemy squad that view could name is "R1", the next "R2", whatever their numbers are to their owner. A tag is given at the first revision at which the view holds the unit by name, and never before. So a tag tells a side only how many enemy units of a kind it has seen, which it knows. It never says how many there are.
- **The adjudicator** reads each unit's own-side tag.
- **A unit the view does not hold** reads "a concealed unit", as now. A unit the view once named and that has gone under "?" reads "a concealed unit" until it is Known again, then takes its tag back (question 2).
- **Nothing counts the unseen:** a Dummy, a hidden unit, and a unit under "?" have no tag in the other view; setup out of sight (R23.3) gives none; an uninspected counter beneath a stack's top counter gives none.

So `fire-a672887feaab-g-squad-1` reads "4-4-7 squad G1" to the German side, and `choose-6a4667fb0056-g-leader-9-1-1` reads "9-2 leader G1" while `g-leader-9-2-1` reads "9-2 leader G2".

**Weapons and the rest.** A SW held by a named unit reads "its MMG" beside the unit and "the MMG of 4-6-7 squad G10" alone. A SW no one holds reads "the unpossessed LMG in G4". A Gun or a vehicle takes values, kind, and tag as a unit does ("PzKpfw IVF2 G1").

**One check, in one place.** `UnitNames.Of(id, before)` returns the name when the view could name the unit just before the event (plan section 15.9), and "a concealed unit" otherwise. Every record in `PlayRecords` goes through it, which closes backlog section 49's row on Deploy, Recovery, a SW left, dismantle, and a public Rally, and play test problem P-21.

**Where the id stays:** in an element's `data-unit` and its title, in the addresses, and in the Game inspector. The tests and the Locate button keep working.

**Other words of this decision:**

- Modifier names get words from one table in `DisplayText`: "stone building TEM", "leadership, 9-2 leader G1", "First Fire, no Assault Movement".
- A reason is shown without its code. The code stays in `data-code` and the title.
- A planner's text reaches the page with ids and identifier Locations in it. One method, `UnitNames.InText`, puts names and words in their place for the view, longest id first; the pattern at `Play.razor:2085` becomes part of it.
- The three texts backlog section 48 left for this pass: the Self-Rally refusal names the unit that used the side's first MMC Rally attempt; the rout status says in one sentence what the unit may or must do; each of the four stacking checks has its own text.

### D12. One way to write a Location (P-20)

`DisplayText.Place` is the one way: "G4, level 1" on a one-board map and "G4 on board 01, level 1" otherwise; "cellar" for level -1; ground level is the hex alone. `ReplaySteps.Place` becomes a call to it. Play's records, reasons, selects, the units table (backlog section 45's row), Replay's step panel, titles, and changes all use it.

A typed field takes that form, the short "G4" and "G4:1", and the identifier form. The page reads the text into the identifier before it proposes; the planner's parsing is unchanged.

### D13. A hex picked on the map fills the field in use (P-20; backlog sections 44 and 45)

- Every typed Location becomes a `LocationField` with its "Use <hex>" button, and a level select when the hex has more than one level. That is the nine plain inputs of section 3.
- A field can be armed ("Pick on the map"); the next hex clicked fills it and disarms it. Escape disarms. One field is armed at a time, and the header says which.
- A rout route and a movement path take one hex per click, in order, with the last removable.
- The fire panel's From and Target, the Advance, ordnance, Close Combat, and smoke selects take the picked hex when it is one of their choices.
- A click still shows the hex in the Selection tab when no field is armed.
- An armed field is the view's draft: a hand-over, another game, and another phase drop it.

### D14. A fire proposal shows its arithmetic (P-14, P-17)

- **The arithmetic.** The review shows each firer's FP with its multipliers, the total, the column, and the DRM known before the dice, from the calculator's own `Preview`, which the planner attaches to the proposal's plan. It is not recorded, so no game replays differently. It shows no dice, no result, and no Cowering, which the DR decides.
- **Area Fire in Final Fire** (A8.4: a unit marked with First Fire "may also fire again ... but as Area Fire and only at units in an adjacent (or same) hex") is said in words on its firer's line, with the halving.
- **Disclosure.** A line that rests on a unit the firing view does not hold reads "not known to you", and the total says it is incomplete. The lines of the target's Location (TEM, Hindrance, range, level) are the map's and are always shown.
- **The headline names the weapons:** "4-6-7 squad G10 with its MMG" and "the MMG of 4-6-7 squad G10, alone".
- **A result says when a MG kept its rate of fire** and may fire again (A9.2), from the record's own `RateOfFireRetained`.
- **"MGs alone"** is shown, disabled with a note, before the squad and its MG are ticked.
- **A fire group across ADJACENT Locations** (the user's answer 6 to pass 31). "From" becomes a list of Locations, each with its firers. "Add a Location" offers only a Location ADJACENT to one already in the group that holds a unit able to join (A7.5: "each participating unit occupies a Location ADJACENT to another participating unit of the same FG"). The planner already takes such a group and refuses one that is not ADJACENT, and one at night (E1.75; ruling R16.3).
- **What A7.5 to A7.55 ask beyond that is checked at the task** against the planner: a leader alone in a Location is no link (A7.5); a berserk unit is never in a multi-Location group (A7.54); units of one Location firing at the same target in the same phase must form one group (A7.55); the worst Hindrance and every detrimental DRM apply to the whole group, and a member with a blocked LOS drops out (A7.52). Where the planner lacks one, section 7 says how it may be added (question 6).

### D15. The workspace holds still (P-28, P-29, P-31)

- **The window does not scroll at 1024 pixels and wider.** Everything outside the workspace grid moves into it: the card panel opens as a dialog from the header's link; the units table, the scripted dice, and the adjudicator's audit become tabs of the activity strip; the introduction becomes the header's "How this works" link.
- **The header has a fixed height.** The Review button and the status line have their places reserved; a long status is cut, with its full text in the title. The header names a proposal only while it waits (P-31).
- **Short windows.** The workspace's least height falls to fit a window 600 pixels high; below that the narrow layout's tabs are used whatever the width.
- **The review** has a fixed track at 1024 to 1439 pixels and scrolls inside itself.
- **A new proposal** moves the focus to the review without scrolling the page; the scroll stays for the narrow layout, where it is needed.
- **Propose and Confirm keep their places.** A panel's Propose button sits in a foot of the actions pane that does not move; Confirm and Cancel sit at the head of the review.
- **A side is shown no revision in a game still played** (backlog section 49's row; ruling R31b.1 already holds Replay to it): not in the header, the units table's note, or the board link (D0). The adjudicator's view and an ended game keep it. The page still records a proposal's revision and refuses a stale Confirm.

### D16. The map keeps its view (P-30, P-27)

- **The view box is kept for a game and a view** across a hand-over: the script keeps the last box under that key and restores it when the workspace comes back, in place of the fit. Each view gets its own back, so a side never sees where the other side was looking. Only "Fit", a new game, and a turned map fit again. A pane's resize keeps the center and the scale.
- **The Location the game waits on** is brought into view when it is out of sight (`BoardWorkspace.Reveal`), as pass 31 has it; when it is in sight nothing moves.
- **"Loading the map"** shows while the layers load.
- **The Residual FP marker** (A8.2) sits at the hex's upper corner, smaller, takes no clicks, and is drawn under the counters. The Selection tab lists it: "2 Residual FP (A8.2)".
- **A Location in Melee** has a "Melee" mark on its hex, in the marks layer, for each view that holds a unit in that Melee by name. No vocabulary or catalog version changes.
- **The hand-over screen draws the terrain alone** (backlog section 48's row): the board with no counter and no mark, and an outline on each Location its lines name. Its lines are those both views read alike (ruling R31.7), so the outlines tell nothing more.

### D17. Records and the latest line (P-15, P-21; backlog section 44's row)

- A record's heading names the side: "Turn 5, German Prep Fire Phase".
- **"Latest"** covers every event the view may read (a move, a pass, a choice, a surrender, a capture, a declaration) and gives a fire's result: "4-6-7 squad G10 with its MMG fires at N5: 1MC; 4-4-7 squad R4 broke."
- **"Since you last looked"**, after a hand-over, lists what the other view did, read for the incoming view from its own records.
- **Records opens over the activity strip** with its own scroll and takes no height from the map.
- **The Deploy record of Turn 1** is traced (P-21), and every record kind is read in each side's view for a name the view may not have (plan section 15.9). D11's one check is what makes that hold.
- `DiceRollHistory.Row`, `FireView`, and `FireText` move from Components/Play to Services, and Play drops its kept records at a view change (backlog section 49's row).

### D18. Rout is quick (P-24)

- **What is made anew each time is kept:** the composed map for a map, the board handle for a board, the location chains for a game's map.
- **The rout search's result is kept** for a unit at a revision, in the planner; the page reads the rout obligations and choices once for a revision and a view, not on each render.
- **Low Crawl** searches to its targets and no farther.
- **The aim** is a check and a Confirm under 2 seconds each for any rout of the played game, measured in the Studio at the revisions the report names (Turn 3's Low Crawl took 25 to 32 seconds).
- **"Working"** shows beside the button that started a gate request that runs past half a second.
- The results are the same with and without what is kept; a test says so. No rule changes.

## 5. Where the words are made

| Text | Made in | Read by |
|---|---|---|
| A unit's name and tag | `UnitNames` (Services), for a history and a view | `PlayRecords`, `FireText`, `ReplayDiff`, Play's pickers and units table, Replay's Units tab, the proposal review |
| A Location | `DisplayText.Place` | The same, and `ReplaySteps` |
| A record | `PlayRecords` | Play's Records and "Latest"; Replay's step panel |
| A step's title | `ReplaySteps` (sides and Locations, never a unit: unchanged) | Replay's timeline and header |
| What changed | `ReplayDiff`, with `UnitNames` | Replay's step panel and marks |
| A modifier's name, a reason without its code | `DisplayText` | The review, the records |
| A planner's text | `UnitNames.InText` | The review's reasons and refusals |

A change to a record's words is made in `PlayRecords`, once; never in a page.

## 6. Disclosure

Rulings R23.1 to R23.6, R31.6, R31.7, R31b.1, and R31b.2 stand. What this pass adds is read for the view:

- **A name** (D11) is given to a side only for a unit its view holds. A tag is numbered so that it counts only what the side has seen. `UnitNames` is built from what the view holds at each revision and never reads the full state for another side's units.
- **A name in text made elsewhere** (`InText`): an id the view may not name becomes "a concealed unit", never left as it was.
- **The fire arithmetic** (D14) is cut where it rests on a unit the firing view does not hold.
- **An armed field** (D13) and **the kept map view** (D16) are the view's own and are dropped or kept apart at a hand-over.
- **The Melee mark** (D16) is drawn for a view only where it holds a unit of that Melee.
- **"Since you last looked"** (D17) is built from the incoming view's records only.
- **The hand-over screen's map** (D16) draws terrain and the outlines of Locations both views read alike.
- **No revision** is shown to a side in a game still played (D15, D0), so nothing counts the other side's hidden placements.
- **The Game inspector and the board viewer** (D0) open no game still played in a view chosen at will.

The referee's review reads every record kind in each side's view for a name the view may not have, and every tag for a number that outruns what the view has seen.

## 7. A recorded game replays as it did

Replay recomputes every recorded attack. So:

- No calculator changes its result in this pass. The fire arithmetic shown before the dice is the calculator's own `Preview`, attached to the proposal's plan and not recorded.
- If a check of A7.5 to A7.55 is missing (D14), it is added as the planner's refusal of a new proposal, which no replay runs; or, where it must change a calculator's result, as a fact recorded with new attacks and read only when present, as pass 31 did (`PinnedMgAreaFire`, `TargetsInMelee`).
- What D18 keeps is read-only and gives the same results.
- A record's words change; its events do not.

**The proof is the played game:** `guards-dl-01` must still open on Play and on Replay with its 613 revisions and its 221 steps in the adjudicator's view. It is opened after each task that touches the planner, the records, or the steps.

## 8. What stays out, for the backlog (section 50)

| Item | Why |
|---|---|
| Fewer hand-overs in the Movement Phase (P-23) | The user, 2026-10-04: they stay as they are for now. Backlog section 44's row stands |
| LOS to the vertices of a Bypassing stack (P-11) | Ruling R10.7; a LOS change |
| A unit's name chosen by the player | D11 gives a fixed name |
| One tag for a unit in every view | D11 numbers the other side's units in the view's order (question 1) |
| A leader in each Location of a fire group directing it (A7.531; the action's `directors`) | The page sends one director, as now |
| The Game inspector's raw events and case read on Replay | Option (c), not taken |
| A push to the other open tab when a view commits | Backlog section 44's row stands |
| Controls the second play test still does not reach | Listed in its report with what was reached |

## 9. Tests

Written at the merge gate, by the handover prompt's rule. The Studio on port 6670 is the only test until the code is complete.

- **Names** (`UnitNamesTests`, Studio): own tags by entry and kind; a tag kept through Replacement, Reduction, Battle Hardening, Deployment, and Recombination; a created leader's tag; the other side's tags in the view's order; no tag for a unit never held by name; a tag that does not change as the game grows; "a concealed unit" for a unit under "?"; the adjudicator's tags. A sweep of the played game: in each side's view, at every revision, no text of the records, the changes, or the units table holds an id or a tag of a unit that view did not hold.
- **Locations:** `DisplayText.Place` on one board and on several; each typed form read into the identifier; `ReplaySteps.Place` the same as before.
- **Records:** no raw id, identifier Location, role label, or reason code in the text of the review, the records, "Latest", and the units table; a fire's result in "Latest"; a kept rate of fire said; the heading's side; "Since you last looked" from the incoming view alone.
- **Picking:** a field armed and filled from the map; a route by clicks; an armed field dropped at a hand-over.
- **The fire proposal:** the arithmetic shown is the arithmetic the record then holds, for each fire kind; "not known to you" where the view does not hold a unit; a group across two ADJACENT Locations proposed through the page and accepted; one across Locations not ADJACENT refused.
- **The workspace:** the card as a dialog, the units as a tab, no revision for a side in a game still played, the header's proposal only while it waits.
- **The map:** the Residual FP marker's layer and its line in the Selection tab; the Melee mark by view; the hand-over screen's map with no counter.
- **Rout:** the same results with and without what is kept, for every broken unit of the played game at the revisions the report names.
- **The Game inspector and the board viewer:** a game still played not listed; an ended game and a fixture listed; the viewer locked to its view for a game still played, with no revision for a side; `GameStatesTests`' page tests and the route row of `SharedComponentTests` brought in line.
- **The played game** opens on Play and on Replay with 613 revisions and 221 steps.
- The harness lessons of passes 28c to 31b apply.

## 10. The second play test (task 31c.8)

As section 10 of the pass 31 design has it: The Guards Counterattack from setup to its end through the page, both sides, with real pointer and keyboard events, at 1568 by 677 and at 1366 by 768.

- Each of P-01 to P-31 is tried again by the report's own steps and marked fixed, changed, or left.
- The controls the first test did not reach are tried where the game allows: a Fire Lane, a SW recovered and transferred, Recombine, a withdrawal from Melee, Ambush, a fire group across two Locations, the adjudicator's view.
- The time for five turns and the count of proposals and refusals are compared with the first game's 2:14, 218, and 21.
- The finished game is then read on Replay in each view.
- Its report goes into the review document. Screenshots go to the user in the chat.

After reading that report the user decides whether Claude in Chrome plays another card before the merge.

## 11. Tasks and estimate

| Task | What it changes | Problems and rows | Estimate |
|---|---|---|---|
| 31c.0 The Game inspector | The page renamed and moved to a Verify group; a game still played off its list and off the board viewer's; the viewer locked to its opening view for such a game | D0; backlog 45 (the board viewer's views), 49 (the page's place) | 1:15 |
| 31c.1 Names and words | `UnitNames` and its tags; `DisplayText.Place`; modifier names; reasons without codes; `InText`; `PlayRecords`, `FireText`, `ReplayDiff`, the pickers, the units tables; the three texts of section 48. Checked on Play and on Replay in each side's view | P-18, P-20, P-21; backlog 45 (the table's Locations), 48 (the texts), 49 (names in records; records without the view check) | 2:30 |
| 31c.2 Picking on the map | Nine inputs made `LocationField`s; the level select; an armed field; routes and paths by clicks; the selects that take the picked hex | P-20; backlog 44, 45 | 1:15 |
| 31c.3 The fire proposal | The arithmetic before the dice; the headline with weapons; rate of fire kept; "MGs alone"; a group across ADJACENT Locations; A7.5 to A7.55 and A8.4 read and checked | P-14, P-17 | 2:15 |
| 31c.4 A workspace that holds still | No window scroll; the card as a dialog; the units, dice, and audit as tabs; a header of fixed height; short windows; fixed places for Propose and Confirm; no revision for a side | P-28, P-29, P-31; backlog 49 (the revision) | 2:00 |
| 31c.5 The map | The view kept for a game and a view; the loading line; the Residual FP marker; the Melee mark; the hand-over screen's terrain map | P-30, P-27; backlog 48 (the map), 49 (the marker) | 1:15 |
| 31c.6 Records and the latest line | The side in the heading; every event in "Latest" with a fire's result; "Since you last looked"; Records over the strip; the record types moved to Services | P-15, P-21; backlog 44, 49 | 1:00 |
| 31c.7 Rout speed | What is kept for a map, a board, and a unit at a revision; the page's reads once for a revision; the Low Crawl search; "Working" | P-24 | 1:15 |
| 31c.8 The second play test | Section 10 | all | 1:30 |
| | Overhead: three reviews and their fixes, the Studio check, the documents, the tests, the merge gate | | 1:30 |
| | **Pass 31c total** (build 14:15) | | **15:45** |

The plan had 14:00 (build 12:30). The Game inspector adds 1:15, and the hand-over screen's map and the Residual FP marker add 0:30 to the map task. Passes 31 and 31b took about 2:56 against 13:00 and 2:36 against 11:15, so the estimate is likely high.

The order is 31c.0, then 31c.1, then the rest as numbered. 31c.1 is checked on Play and on Replay, in each side's view, before anything else is built on its words.

The steps are those of passes 30 to 31b: build one task at a time with the Studio on port 6670 as the only test, commit each task once it passes its Studio check; three read-only reviews in parallel; the Studio check of the whole pass at 1920x1080, 1366x768, 1568x677, 1024x768, 683x384, and 320 pixels, by keyboard and mouse, on Play and on Replay; the second play test; the documents; the merge gate; a stop before the merge.

## 12. The user's answers

Answered 2026-10-04: all eight as recommended. The other side's units are numbered in the view's own order; a unit Known again keeps its tag; a SW has no tag; the map's view is kept for a game and a view; the hand-over screen's map and the dice and audit tabs are in this pass; a missing check of A7.5 or A7.54 is added as the planner's refusal after I say what I find, and A7.55 gets a backlog row; a side is shown no revision on Play in a game still played; the estimate is 15:45.

| # | Question | Recommendation, taken |
|---|---|---|
| 1 | How are the other side's units numbered? (a) In the view's own order of first naming, so "R2" in the German view may be "R5" to the Russian side. (b) One number for a unit in every view, by entry, as the ids are now: a Known "R7" then says at least seven Russian squads entered. (c) One tag in every view that is not a count, such as two letters ("4-4-7 squad R-KD"). | (a). It is the only one that tells a side nothing it has not seen and still reads as "G4". Its cost: two players at one screen name the same enemy unit differently; the Location is always beside the name. |
| 2 | An enemy unit that was Known, went under "?", and is Known again: the same tag, or a new one? The same tag says it is the unit seen before, which like counters at a table do not say; a new one makes one unit read as two. | The same tag. A player follows a "?" by its Location anyway, and a record that renames a unit is harder to read than the little it gives away. |
| 3 | Weapons: "its MMG" beside its holder and "the unpossessed LMG in G4" alone, with no tag of their own? | Yes. A SW is always read with its holder or its Location. Guns and vehicles take a tag as units do. |
| 4 | The map's view is kept for a game and a view, not for a game alone as D16 first had it, so a side never sees where the other was looking. | Yes. |
| 5 | The hand-over screen's terrain map (backlog section 48) and the scripted dice and the audit as tabs: in this pass? | Yes, both; they are small and the layout task moves those blocks anyway. |
| 6 | If the planner lacks a check of A7.5, A7.54, or A7.55 (a leader alone as a link, a berserk unit in a multi-Location group, the mandatory group), add it as the planner's refusal of a new proposal, with a ruling, or leave a backlog row? | Add it where the page would otherwise offer an illegal group (A7.5's leader, A7.54); a backlog row for A7.55, which bars separate attacks and would need each side's earlier fire in the phase read against the new one. I will say what I find before I change anything. |
| 7 | A side is shown no revision on Play in a game still played. | Yes; Replay already does this (R31b.1). |
| 8 | The estimate rises from 14:00 to 15:45 with the Game inspector and the map additions. | Accept. |

## 13. Pass 31c as built

**Built 2026-10-04,** tasks 31c.0 to 31c.8, one commit a task, each checked in my Studio on port 6670 before its commit; then the three reviews' fixes, the second play test, and its fixes. The [review document](<Scenario A1 Backlog Pass 31c Review 2026-10-04.md>) lists what a player meets, the reviews' findings, the check, and the play test. Rulings R31c.1 to R31c.4. No rule's result changed, and `guards-dl-01` opens as before with 613 revisions and 221 steps.

**The code.**

| Part | Where | What it holds |
|---|---|---|
| `UnitNames` | Services | A view's names and tags for a game; `Of` with the view check; `InText` and `Held` for a text made elsewhere; `Counter` for a definition |
| `DisplayText` | Services | `Place` and `ReadPlace`; `Modifier`, `Reason`, `Action`, `RollPurpose`, `Sentence` |
| `PlayRecords` | Services | Every record said through `Say`; `Lines`, `Latest`, `Since`; the fire record's weapons |
| `FireText`, `FireView`, `RollRow` | Services | Moved from Components/Play |
| `LocationPicking` | Services | The armed field, the last pick, the map's boards and levels |
| `LocationField`, `PickedOption` | Components/Play | A typed Location that picks on the map; "Use G3" beside a select |
| `FireArithmeticPreview` | Components/Play | A fire's arithmetic before the dice |
| `HandOverMap` | Components/Play | The hand-over screen's map |
| `GameMaps.Public`, the Residual FP and Melee marks | Services | What both sides know; the markers |
| `GameLibrary.StillPlayed`, `OpenNames` | Services | A game still played, read from its record |
| `StudioBoardCatalog` | Services | One handle for a loaded board |
| `boardViewport.js`, `playWorkspace.js`, `studioShell.js` | wwwroot/js | The kept view; the workspace's top, the card's Escape, "Working"; a short window as narrow |
| `GamePlanner.Adjacent`, the Self-Rally refusal | Play | A public reading of ADJACENT; a refusal's words |

**What differs from the design:**

- **D0.** The inspector's address names no game, so there is no line for an address that names one; the page says in one line where a game still played is opened.
- **D11.** A text made elsewhere is said at its edge (`Say`), not by a call at each of 170 places. Squads, half-squads, and crews share one count, and leaders and heroes another, so a unit that becomes another kind never takes a tag in use. Two HS of different squads Recombine into a squad with a new number. A public record whose facts are a concealed unit's own is not given, rather than given with the name hidden (the referee). A text for each of the four stacking checks is left out: it needs four codes in the Close Combat package, which recorded games name.
- **D13.** The vehicle's next hex and setup's two inputs keep their own picking. Escape does not disarm a field. Each of a field's actions is on its own line (the user).
- **D14.** The planner already asked for ADJACENT Locations and the mandatory group, takes no leader without a MG as a firer, and lets no berserk unit fire small arms, so no check was added. The preview is made by the page from what the view may know, and what it leaves out is decided by a "?" in the target Location (ruling R31c.4), not by the planner's reasons.
- **D15.** Propose stays in its panel: with the layout still it no longer moves. Confirm follows the consequences when there are any (the table player). The building entry block is offered only in the moving side's Movement Phase (the user). The narrow layout under 600 pixels of height is the whole Studio's.
- **D16.** The view is kept for a game and a view (the user's answer 4). The hand-over screen's map shows what both sides know, not the terrain alone, and is not drawn during setup (the user; ruling R31c.3). The Residual FP marker is not drawn under the counters; it stands clear of them at the hex's corner and takes no click.
- **D17.** "Latest" is the last attempt's lines, passing over a phase's start. "Since you last looked" opens on arrival.
- **D18.** One board handle and the Rout panel read once met the aim (a rout under 2 seconds); the search's result, the composed map, and the Low Crawl search were not touched.

**The second play test** differed from section 10: it was played at the pane's full size at the user's word, by a script's clicks, and each of P-01 to P-31 was marked from what the game met rather than tried by the first report's steps one by one.
