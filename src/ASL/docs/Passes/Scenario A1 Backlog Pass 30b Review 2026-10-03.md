# Scenario A1 Backlog Pass 30b: Setup Plans for the Side That Sets Up Second

**Date:** 2026-10-03

**Branch:** `feature/asl-backlog-pass-30b`

**Related documents:** the [pass 30b design](<ASL Unit Backlog Pass 30b Design.md>) (sections 1 to 16), the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>) (pass 30b), the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 47, and the [pass 30 review](<Scenario A1 Backlog Pass 30 Review 2026-10-03.md>).

## Status

Built, reviewed, and checked in the Studio; the merge gate follows. The pass gave the side that sets up second its own setup plans: answers to the first side's plans and plans for any setup, offered in groups ordered by how closely each first-side plan matches the stacks the viewer can see (tasks 30b.1 to 30b.6); the plans themselves for the four built-in cards (30b.7); and, at the user's condition, as much hidden and concealed setup as the rules and each card allow (30b.8). Three reviews read the branch at commit d45db80. None found a blocker in the code; the referee raised one point for a ruling, which the user gave. The fixes were checked in the Studio (port 6670), then the whole pass (step 5).

A setup plan places the card's fixed OB and never changes it. A card's plans are in their own file, so adding or changing plans never changes the card's text, its SHA-256, or a saved game. The game stores no plan: the Studio cannot tell one side which plan the other used, because it does not know.

## The plans

| Card | First side | Second side |
|---|---|---|
| The Guards Counterattack | German: Forward line, Out of sight, Tripwire and reserve (pass 30) | Russian: Fire first, Cross unseen, Break the tripwires, each an answer; Two up, two back for any setup |
| Gambit | British: The farmhouse, West woods, Two posts (pass 30) | German: Storm the farmhouse, Seal the west road, Both posts, each an answer; Three roads for any setup |
| The Tractor Works | Russian: Dummy west (new), East front (remade), Hidden core. The three show the same counts in every hex | German: Three sides and Feint west, both for any setup, which look the same to the Russians |
| Armor Test | Russian: Forward screen, West trap (new), Back stop (new). The three look the same to the Germans | German: Loaded column, off board, for any setup |

Sixteen plans were made or remade in the pass, each accepted by the gate in the Studio and approved by the user, one card at a time.

## Referee findings

No path reads the game's state where it should read the viewer's own view: the match score, the groups, the stacks seen, the answered plan's outline, the zoom, and the note on public plans read the `GameView` and the public plan files only. The hand-over and a game change clear what the last view prepared. Eight findings:

| # | Finding | Fix |
|---|---|---|
| 1 | The offer of a non-OB "?" (ruling R23.6, pass 23) tests LOS against real enemy units only, so a stack seen only by Dummies is offered a "?", and the offer tells its owner that those are Dummies. On The Tractor Works, Three sides' stack at AA4 was seen only from Y3, Y4, and Y5, so the offer would have named Hidden core. The new note on plans that look alike promised the opposite. | The user ruled that R23.6 stands: at the table an opponent denies a "?" only by showing a real unit. Three sides' engineers moved to BB4, which no hex of the Works sees. A plan with hexes of Dummies only now tells its user how the offer may show them. |
| 2 | `SetupPlanMatch.Look` and `Footprint` leave out Passengers on the map and the SW of an open top counter. No card needs them today. | Backlog section 47. |
| 3 | A `/1` file did not read exactly as before in three corners (two setup orders in one plan, and the caps). | A `/1` file keeps its own cap of three plans in all and is not held to one setup order a plan. |
| 4 | On a tie the first tied group opened, by file order, and the plan for any setup stood closed at the bottom. | The table player's finding 1. |
| 5 | A swap begun on a stack that was then removed moved the other stack and said "Swapped". | The UI review's finding 3. |
| 6 | The outline toggle survived a hand-over. | Reset with the view. |
| 7 | "N stacks seen" counted hexes. | "you see its stacks in N hexes". |
| 8 | West trap's idea named other ground than the hex it Bore Sights. | The idea says H9. |

## Table player findings

The reviewer checked every plan against its own words, its card's OB, areas, and stacking, and found the counts and placements sound. Fifteen findings:

| # | Finding | Fix |
|---|---|---|
| 1 | On The Tractor Works and Armor Test the only usable plan stood collapsed at the bottom, under an open group that said "No response written" and printed one first-side plan's idea. | When the plans tie, none is followed closely, or the closest has no answer, "For any setup" opens first and the tied groups stay closed; a line says "Their plans look the same from here, so the stacks do not say which was used." |
| 2 | X3 and X5 hold real units in all three 308th plans. | Left, by the user's answer: three plans of this footprint can leave at best one hex always real. Each plan's facts now say those two hexes are no secret. Backlog section 47. |
| 3 | The note for plans that look alike overstated. | "The other side still knows it is one of these, and whatever all of them share; move or swap stacks to hide more." |
| 4 | Three sides was the card's only German plan, so unchanged it was read in full. | Feint west, a second German plan with the same look, by the user's answer. |
| 5 | Three sides ignored the die roll for the first move. | Both German plans say what the Russians' Prep Fire costs if they move first. |
| 6 | Gambit's German plans called the south edge of board 4 "the exit". | They say where each road leaves board 4, and a fact names the exits on board 2. |
| 7 | Cross unseen said there is nothing to fire on. | Reworded; it gives up that J4 and J5 see the street. |
| 8 | "Swap" said "stack" and worked by hex; its explanation was a tooltip only. | It says "hex"; the banner carries the explanation. |
| 9 | Back stop's Gun faces south-west and claims the east exit. | It gives up that the Gun must change its Covered Arc to fire on Y10. |
| 10 | A changed first-side plan still earned "Closest to what you see". | "N hexes differ from <plan>: check its outline on the map before you use an answer to it." |
| 11 | East front: the 9-2 and a Dummy could trade places, giving the HMG its leader. | Done, by the user's answer. |
| 12 | West trap did not say what the lone 8-1 costs. | Added to what it gives up. |
| 13 | Small differences between a plan's idea and its placements. | Three sides, Dummy west, and Break the tripwires reworded. |
| 14 | "N stacks seen" counted hexes. | The referee's finding 7. |
| 15 | Strings: the conceal-all button, "gun", "response", the legend, "for this setup", the new-game note, the words for the "?" stacks of Armor Test. | Reworded: "Tick every stack that may take a \"?\" (N)", "Gun", "No answer to this plan is written", "where their setup differs from that plan", "for this side", each side named. |

## UI and Blazor findings

Thirteen findings; the larger ones:

| # | Finding | Fix |
|---|---|---|
| 1 | Pass 30's tests fail in five places against the new plans and format. | Brought in line at the merge gate. |
| 2 | "For any setup" collapsed when it held the only usable plan. | The table player's finding 1. |
| 3 | A stale swap acted as a move with a false message; the level changed without a word. | A swap needs counters of the list in both hexes; a list change ends a swap whose hex is empty; a counter sent to ground level is said. |
| 4 | The swap's words were not seen or announced under 1024 pixels. | They are in the map's live region and in the list; the waiting stack carries a badge. |
| 5 | The groups and scores were recomputed four times a render, with a SHA-256 for each answer. | Read once for each state of the game and view (`Reading`). |
| 6 | A group opened by hand closed again after "Use this plan"; no keys on groups and cards. | A latch held by the page; `@key` on each. |
| 7 | A plan whose answered plan was not among those compared was listed nowhere. | It is listed under "For any setup". |
| 8 | The conceal-all button disabled itself under the focus and could overflow at 320 pixels. | It stays enabled and wraps. |
| 9 | What a plan gives away named hexes without their board. | A hex is named with its board on a map of several boards. |
| 10 | A long plan name in a badge, and the outline's legend, at narrow widths. | The badge wraps; the legend is hidden under 1024 pixels. |
| 11 | `PlanCard` is built with `__builder`, unlike the repository's templates. | Backlog section 47. |

## The Studio check of the whole pass (step 5)

At 1920x1080, 1366x768, 1024x768, 683x384, and 320x640, in my Studio on port 6670:

- No sideways scroll at any width, with every group and every "More" open at 320 pixels.
- Under 1024 pixels "Show on map" opens the Map tab with the small setup bar, "Use <plan>", and the outline toggle; the legend is hidden there.
- The keyboard: Enter on a group's summary opens it; Tab reaches "Show on map", and Enter shows the plan with the answered plan's outline; `]` and `[` on the focused map step through the plans; Enter on "Swap", then Escape on the map, cancels it; Enter on "Swap", then Enter on another stack's name, swaps the two hexes and returns the focus to the actions.
- The views: while the Russians set up, the German view and the adjudicator's show no Plans tab, no groups, no outline, and no draft; back in the Russian view the list is empty and the groups are fresh.
- A user's card with a `/1` setups file reads as before, with its plan for an earlier text marked. A card with no plans says "This card has no setup plans for this side". The older game `p28c-walk2` opens in play, unchanged.
- Gambit with the British plan changed (its two stacks swapped): "The British side has set up: you see its stacks in 2 hexes. Their setup does not follow any of their plans closely.", and "For any setup" first and open.
- A reload in the middle of the second side's setup opens the same game behind the hand-over of the side setting up.

Two faults were found and fixed: a group brought into view stopped under the bar that stays at the top (it now has the rows' scroll margin), and the outline's legend took three lines of the map's toolbar at 683 pixels (hidden under 1024 pixels).

## Not confirmed

- The LOS facts of the plans come from the scratchpad script on the Studio's own LOS code, run in pass 30 and again in this pass. They were not checked again one by one in the Studio's LOS tab, as pass 30's were.
- On The Tractor Works the offer of the non-OB "?" to the German stacks at BB4 and Y8 comes after the Russian remnants set up, which the checks did not do.

## Left out

Backlog section 47 holds what the pass leaves out: "Rehearse", plans for later setup orders, Passengers and SW in the comparison, a page to study a card's plans, a hatched footprint, the two hexes real in all three 308th plans, the board 4 fixture for the second sides' plans, and the smaller items of the reviews.

## Tests

`BacklogPass30bTests` (Play, 22 tests):

- Each card's second side has its answers and its plans for any setup; each is of setup order 2 and names its card's current SHA-256; each answer names a plan of the side that sets up first and that plan's current placements hash.
- The placements hash changes with a placement and not with a plan's words or the order of its placements.
- A file of the earlier format reads as before (the first side only, no answers, three plans in all); the new format is refused with its reasons for an answer to no plan, to a plan of its own side, to a later plan, or to itself, for two answers to one plan, for four plans for any setup, for a malformed hash, for a side that is not the card's, and for a plan of two setup orders.
- On board 01 the gate accepts each of the second side's plans with a first-side plan committed before it: the three answers and the plan for any setup on The Guards Counterattack, and Three sides and Feint west on The Tractor Works.
- The comparison: on The Guards Counterattack the plan used matches the Russian view in full and the other two fall under six tenths; SW are not counted.
- Games that look the same give the same scores: with each of the three 308th plans committed in its own game, the German view holds no Russian unit, the same 25 sealed presences by hex, and 9 of 9 for every plan.
- The look-alike plans of The Tractor Works (both sides) and Armor Test show the same look and footprint and differ in their placements; The Guards Counterattack's German plans differ to the eye; a footprint leaves out SW, hidden counters, and counters off board.

`BacklogPass30Tests` (Play) was brought in line: the plan ids of each card, seven plans on The Guards Counterattack, a plan for any side of the card, `/3` as the refused format, and Dummy west in place of All round.

`SetupAnswersPageTests` (Studio, 6 tests): the new-game form counting each side's plans; the second side's Plans tab in groups with the closest first, its answer, "Their plan", the outline toggle, and the bar, review, and notice naming the plan answered; a setup that follows a plan with no answer opening the plans for any setup; the other side and the adjudicator shown nothing of the second side's plans, and the hand-over clearing its list; "Swap" changing two stacks' hexes, refusing the same hex, and ending when its stack leaves the list; and the picker's groups, scores, and notes.

Gambit and Armor Test are on board 4, which the tests have no terrain for; their second sides' plans are checked for form in the tests and were accepted by the gate in the Studio on the real boards (backlog section 47).
