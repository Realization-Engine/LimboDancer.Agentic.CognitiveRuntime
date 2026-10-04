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

## 14. Range (added after the report)

**Status:** designed 2026-10-04 and answered the same day: all eight questions as recommended (section 14.9). Built as task 31c.9; section 14.10 says what was built and where it differs.

The second play test proposed fire to learn a range and was refused. The user's idea: Range is a sibling of LOS. It is read in the inspector, drawn on the map with the LOS line, read in the fire panel before any proposal, and a refusal for range names what was out of range.

### 14.1 The rules read

Read in the PDF before this section (A7.2 to A7.22 are on its pages 54 and 55).

| Rule | What it gives Range |
|---|---|
| A6.7 | Range is the least number of hexes from firer to target, whatever the LOS. So Range needs no LOS read. |
| A6.11 | In play, no LOS check for an attack before the attack is declared. So the fire panel reads its targets by range and never by LOS. |
| A7.2 | FP modifiers are cumulative and fractions are kept. Range gives a multiplier, not a final FP. |
| A7.21 | PBF: Small Arms and MG FP doubled when ADJACENT to the target, or adjacent and within one level of it or higher than it. TPBF: tripled at units in the firer's own Location, where such fire is allowed. Ordnance does not double. |
| A7.211, A7.212 | TPBF against PRC; a unit whose Location holds a Known enemy unit fires at nothing else. |
| A7.22 | Long Range: beyond Normal Range up to twice it, at half FP; not for an ATR, MOL, a DC, ordnance, and some FT. |
| A8.3, A8.31, A8.4 | A fire kind's own limit: Subsequent First Fire within Normal Range and no farther than the closest armed Known enemy unit; FPF and a First Fire unit's Final Fire only at an ADJACENT or same-hex target. |

### 14.2 What the code does now

- **The range of an attack** is read by the planner in `FireMapFacts` (GamePlanner.Fire.cs): for each firer's Location the LOS read's `Range`; 0 for the firer's own Location (TPBF); for a Snap Shot the nearer of two hex distances. `HexDistance` (GamePlanner.CloseCombat.cs) is private. `LiveFire` replays an attack with the range it recorded.
- **The bands** are in the Fire package. `Multipliers`: range 0 is TPBF (x3), range 1 with the target at most one level above is PBF (x2), a range over the Normal Range is Long Range (x1/2). The pre-check refuses with `asl.a1.fire.out-of-range` when a firer's own FP is used beyond twice its Normal Range, a weapon fires beyond twice its own (an ATR beyond its own, C13.24), or the range is under 1 without TPBF. One sentence serves them all: "a firer or MG is beyond twice its Normal Range, or fires into its own hex". It names no unit, no range, and no limit.
- **The fire panel's Target list** (`Play.razor`, `FireTargets`) is every Location where the view holds an enemy unit or a "?", in the order of their names. Nothing says how far each is.
- **The LOS tab** is `LosPanel` inside `BoardInspector` (tabs Selection, LOS, Evidence). `BoardWorkspace` keeps its draft and result and sends `StudioLos.Layer` to the viewport as the group `layer-los`. The board viewer and Play share all of it. The tab reads terrain only.

### 14.3 Decisions

**R1. A Range tab beside LOS, with the same two ends.** `BoardInspector` gets a fourth built-in tab, "Range", after LOS. It shows the From and To of the LOS tab (one draft, typed or picked once), and "Read the range". A hex clicked while Range is open fills an end as it does for LOS. The result is one line and, in a game, a table:

- The line: "[G4] to [N5], level 1: 7 hexes." With no game (the board viewer) that is all.
- The table, one row for each unit and weapon read (R3): its name, its Normal Range, and where the range falls for it.

**R2. Where a range falls.** For a Normal Range N, a range r, and the target's level against the firer's:

| Case | Said as |
|---|---|
| The firer's own Location | TPBF, FP x3 (A7.21) |
| Its own hex, another level | "the same hex, another level: not built" (the Fire package refuses it; section 14.6) |
| r = 1, the target at most one level above | PBF, FP x2 (A7.21) |
| r = 1, the target two or more levels above | Normal Range, no PBF (A7.21) |
| 1 < r <= N | Normal Range |
| N < r <= 2N | Long Range, FP x1/2 (A7.22) |
| r > 2N | out of range: "13 hexes; it fires to 12" |
| An ATR beyond N | out of range (C13.24: no Long Range) |
| A FT beyond its Normal Range | Long Range to twice it, as the Fire package has it (ruling R15.1); A22 is read in the PDF at the task before its words are written |
| A unit with no FP of its own (a leader) and no weapon | no row |

The words come from one function in the Rules project, `FireRange.Band`, new and called by nothing that resolves an attack. The calculator's `Multipliers` and pre-check are not touched; a test at the gate holds the two together (section 14.7).

**R3. Whose Normal Range is read.** The page gives the tab its subjects:

- With a fire group chosen in the fire panel: each ticked firer and each ticked weapon, read from its own Location to the panel's target. The tab's From and To are filled from the panel ("Range of this group" beside the Target select opens the tab with them).
- Otherwise: the units in the From Location that the view holds by name, with the weapons they hold.
- A unit under "?" or hidden gives no row. A Known enemy unit gives one (question 2).

**R4. On the map, with the LOS line.** Range draws inside the `layer-los` group, so the viewport's script does not change:

- A line from From to To in blue, thinner than the LOS line and under it, with the hex count at its middle ("7"). When an LOS check stands for the same ends, the LOS line is drawn over it in its own color.
- Two outlines around the From hex for one subject: the outer edge of the hexes within its Normal Range (solid) and within twice it (dashed), each with a small label ("6", "12"). An ATR gets one outline.
- One subject at a time. The tab's rows are radio choices; the first shown is the member with the shortest Normal Range, since it is the first to fall out. A group of several Locations draws the chosen member's outlines from that member's hex.
- "Clear" removes the range marks; the LOS tab's Clear removes only the LOS line.

**R5. The fire panel reads its targets by range.** Once a From and at least one firer are ticked:

- Each option of the Target select says its range and the group's worst band: "[N5], level 1: 7 hexes, Long Range for 1 of 3" or "[R4]: 14 hexes, out of range for 4-6-7 squad G1". The list is ordered nearest first.
- A target out of range for some member stays in the list and is said so: unticking that member brings it in range. A target out of range for every ticked member is disabled.
- Under the select, one closed line ("Range: 7 hexes") opens the same table as the tab (R1), so nothing sits open on every screen.
- "Or any Location" reads the same way once filled.
- Where a ticked firer's kind of fire has its own limit, the line says it: a First Fire unit in the DFPh, "only at an ADJACENT or same-hex target (A8.4)"; Subsequent First Fire, "within Normal Range (A8.3)". The closest-Known-enemy half of A8.3 stays with the planner's refusal.
- No LOS is read (A6.11). The LOS tab stays what it is.

**R6. A refusal for range names what was out of range.** When the Fire package's pre-check gives `asl.a1.fire.out-of-range`, the planner adds its own reason from the ranges `FireMapFacts` read, one for each firer or weapon out: "play.fire-range: g1 in bd01:G4:0 is 13 hexes from bd01:R4:0; its Normal Range is 6, so it fires to 12 (A7.22)", "play.fire-range: the MMG of g1 ...", and for the own-hex case "play.fire-range: g1 fires at another level of its own hex, which is not built (A7.21)". `UnitNames.InText` and `DisplayText.Place` say it for the view as they do every planner text. The package's code stays in the refusal, so the tests that ask for it still find it; the page shows the planner's sentence in place of the package's.

**R7. Where range is read.** `GamePlanner.Range(state, from, to)` is made public beside `Adjacent`: the hex distance, 0 for one hex, null when the map cannot give it. The page and the tab read it; the board viewer with no game reads the board's own distance. A subject's Normal Range and kind come from the Fire package's reference (`FireReference`), the same definitions the calculator reads, with a wounded hero's own range (A15.2).

**R8. Guns and vehicles.** A vehicle's MG reads as a weapon with its Normal Range (D1.83: eight hexes for a MA AAMG). A Gun's range goes by its To Hit table and C2.25, which is the Ordnance package's: the tab gives a Gun the hex count alone, and the ordnance refusal keeps its sentence (question 4).

### 14.4 Disclosure

- A hex count is the map's. Every view may read it between any two Locations, as every view may use the LOS tab.
- A Normal Range is printed on a counter. A view is given it only for a unit it holds by name. A "?" and a hidden unit give no row, and the list of subjects never shows that a Location holds more than the view knows.
- The Target list is the view's own (ruling R23.1), as now; reading it by range adds the map's hex count and the firing side's own printed ranges.
- The outlines are drawn for the view that asked and are dropped at a hand-over with the LOS check and the view's drafts.
- The refusal names the proposer's own firer and weapon and the Location it named.

### 14.5 A recorded game replays as it did

- `FireRange.Band` is new and resolves nothing. No line of `ScenarioA1FireCalculator`, `LiveFire`, or `FireMapFacts`' reading changes.
- The planner's added reason is part of a refusal, and a refusal records no event; a replay runs only recorded attacks, which passed the pre-check.
- `GamePlanner.Range` is a public reading of what `HexDistance` already gives.
- The proof is section 7's: `guards-dl-01` with its 613 revisions and 221 steps, and `p31c-play` with its 134 steps, opened on Play and on Replay after the build.

### 14.6 What stays out, for the backlog

| Item | Why |
|---|---|
| PBF between Locations of one hex at different levels (A7.21: ADJACENT) | The Fire package refuses a range under 1 that is not TPBF; building it changes what the package resolves |
| A Gun's range bands (C2.25, the To Hit table, a mortar's least range) in the Range tab | The Ordnance package's own; question 4 |
| The night's NVR as a band (E1.101) | The night facts are read with the attack, not before it |
| The closest armed Known enemy unit of A8.3 read before the proposal | It needs each firer's LOS to every Known enemy, which A6.11 keeps for the attack |
| A band drawn for several subjects at once | One at a time keeps the map readable |

### 14.7 Tests, at the merge gate

- `FireRange.Band` against the calculator: for every definition with a Normal Range and every range from 0 to twice it plus one, at the target's levels -1 to 2, the band's multiplier is the one `Preview` gives and "out of range" is where the pre-check refuses.
- `GamePlanner.Range` equal to the LOS read's range for the pairs of the played game's attacks.
- The refusal: a squad at 13 hexes named with "13" and "12"; a MG out while its squad is in; the own-hex case; the package's code still present.
- The Range tab (bUnit): hexes alone with no game; a row for each held unit and none for a "?"; the radio choice; Clear.
- The layer: the range line, the two outlines, and their labels inside `layer-los`; an LOS check drawn over them; none after a hand-over.
- The fire panel: options with range and band, nearest first; an option disabled when every member is out; the closed range line; no LOS read (the LOS service is not called).
- The played games open as before (section 14.5).

### 14.8 Task and estimate

| Task | What it changes | Estimate |
|---|---|---|
| 31c.9 Range | `FireRange` (Rules); `GamePlanner.Range` and the planner's range reason; `RangePanel`, the tab in `BoardInspector`, the draft and subjects in `BoardWorkspace`; the range marks in `StudioLos.Layer`; the fire panel's Target options and range line; `Play.razor`'s subjects. Checked in my Studio on port 6670 on `p31c-play` and `p31-pf` in each side's view, and on the board viewer | 1:00 |

### 14.9 Questions for the user

| # | Question | Recommendation |
|---|---|---|
| 1 | A Range tab of its own beside LOS, sharing the LOS tab's From and To, or one tab "LOS and Range"? | Its own tab with the shared ends. One tab would read the LOS each time a player only wants a range, which A6.11 does not allow at a table; apart, Range can be used freely and LOS stays the deliberate check it is. |
| 2 | May a side read the bands of a Known enemy unit (one its view holds by name)? | Yes. The counter is face up and its range printed; "can that MMG reach me" is what a player counts at a table. Never for a "?" or a hidden unit. |
| 3 | A target out of range for every ticked firer: disabled in the list, or left out? | Disabled and said ("14 hexes, out of range"). Leaving it out would make a player wonder where the enemy went. |
| 4 | Guns: the hex count alone in this pass, with a backlog row for their bands? | Yes. Their range is the Ordnance package's and differs by Gun and ammunition. |
| 5 | The Target list ordered nearest first once a firer is ticked, in place of the order of names? | Yes. |
| 6 | PBF between levels of one hex is refused today as "fires into its own hex". Name it in the refusal as not built and give it a backlog row, or build it in this pass? | A backlog row. Building it changes what the Fire package resolves, which this pass does not do. |
| 7 | The fire panel says a fire kind's own limit (A8.3 Normal Range, A8.4 ADJACENT) beside the range. In this pass? | Yes; it is a line of words from facts the page already holds (the firer's fire counter and the phase). |
| 8 | The estimate: 1:00 for task 31c.9, added to the pass. | Accept. |

### 14.10 Range as built

Built 2026-10-04 and checked in my Studio on port 6670 on `p31-pf` in the Russian view, on the board viewer, and on Replay.

| Part | Where |
|---|---|
| `FireRange` (`Band`, `Words`), `FireRangeBand`, `FireRangeReading` | Rules; called by nothing that resolves an attack |
| `GamePlanner.Range`; `RangeNamed` at the Fire pre-check's refusal | Play |
| `RangeCheck`, `RangeRow`, `StudioLos.Range`, the range marks in `StudioLos.Layer` | Services |
| `RangePanel`; the Range tab in `BoardInspector`; the reading, its rows, and `ReadRange` in `BoardWorkspace` | Components/Board |
| The Target options, the closed range line, and "Show the range on the map" in `SmallArmsFirePanel`; `RangeRowsOf`, `FireTargetsByRange`, `RangeSubjects` in `Play.razor` | Components |

What differs from the decisions:

- **R4.** Each outline's label sits on its edge nearest the way to the target, where the range line leaves it. The map's own edge is not drawn as part of an outline.
- **R5.** The range line under the Target select lists a line for each firer and weapon and has "Show the range on the map", which opens the Range tab with the group drawn.
- **R6.** The planner's sentence takes the place of the package's under the package's own code, so the code is still in the refusal. Where the target Location holds nothing the firing side may see, that side was told only that the package does not decide the attack; it is now told the range lines and nothing else, since they rest on its own units and the map alone. Whatever else the package found stays untold.
- **Checked:** the Target list by range and nearest first; "6 hexes, Long Range for 1 of 2"; the outlines at 4 and 8 with the line's "6"; a refusal naming the squad (10 hexes, fires to 8) and both the squad and its MMG (23 hexes, fires to 20); the tab's rows for own units and Known enemy units and none for a Location with a leader alone; the board viewer's hexes alone, with the LOS line drawn over the range marks and each Clear removing its own; `guards-dl-01` with 613 revisions and 221 steps and `p31c-play` with 134 steps.
- **Not yet seen in the Studio:** a target disabled because every ticked firer is out, and the fire kind's own limit (A8.3, A8.31, A8.4) in a row. Both are watched for in The Tractor Works.
