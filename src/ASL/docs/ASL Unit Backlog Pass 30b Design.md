# ASL Unit Backlog Pass 30b Design

**Status:** Draft, 2026-10-03, on branch `feature/asl-backlog-pass-30b`. Nothing is built. It waits for the user's approval and the answers to section 11. Pass 30b (Setup plans for the side that sets up second) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, a Studio pass.

**Date:** 2026-10-03

**Related documents:** the [pass 30 design](<ASL Unit Backlog Pass 30 Design.md>) (sections 4, 5, 10, 11, 12); the [pass 30 review](<Scenario A1 Backlog Pass 30 Review 2026-10-03.md>); the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 46; the [pass 30 handover prompt](<ASL Pass 30 Handover Prompt.md>), whose standing rules and harness lessons apply unchanged; the [pass 23 design](<ASL Unit Backlog Pass 23 Design.md>) (a side's view at setup). Claude Design's report, "Setup plans for the side that sets up second" (the user's PDF, 8 pages, not in the repository), is checked in section 4.

**The name.** A "setup plan", as in pass 30. A setup plan of the second side that names the first-side plan it answers is a "response"; one that names none is "for any setup". "Deployment plan" is not used (A1.31).

A Studio pass adds no rulings. The gate's setup checks and a side's view at setup (rulings R19.1 to R19.6, R23.1 to R23.6) are unchanged.

## 1. Outcome

After pass 30 only the side that sets up first is offered setup plans. The other side sets up by hand: "Set up" its groups, then place each stack.

After the pass:

- A card may carry setup plans for the side that sets up second: one response for each plan of the first side, and one for any setup.
- That side sees them in its own view while it sets up, grouped under the first-side plan each answers, ordered by how closely each first-side plan matches the stacks it can see.
- The Studio never says which plan the first side used. It does not know: the game stores placements only.
- Each of the four built-in cards has its second side's plans, reviewed by the user one card at a time.

A plan places the card's fixed OB and never changes it. Adding plans never changes a card's hash or its saved games.

## 2. Who sets up second, card by card

`ScenarioSetup` gives a group its card's `setupOrder`, or 1 for the side that sets up first and 2 for the other. The side that sets up second is the side with the groups of order 2.

| Card | Sets up second | OB groups, all of order 2 | Counters | Areas | "?" | What "setup" means |
|---|---|---|---|---|---|---|
| The Guards Counterattack | Russian (moves first) | Two: the 308th Rifle Division; the 2nd Battalion, 37th Guards | 25 | 308th: buildings N4, J2, M2, N2. Guards: building F3 | None | On board, in sight of the German stacks |
| Gambit | German (moves first) | One: Sturm Regiment 1 (eight 5-4-8 squads, the 9-2, the 9-1, the 8-1) | 11 | Hexes numbered 8 to 10 on board 4 | None | On board, in sight of the five British counters |
| The Tractor Works | German | Three: Assault Engineer Company A (15 counters), Kampfgruppe Stahler (13), Kampfgruppe Tienham (11) | 39 | Engineers: AA4, CC3, or Y8. Stahler: U3, T4, R7, or T7. Tienham: Y8, CC7, or AA4 | 12, Stahler's | On board, around building X3, in sight of nine hexes of "?" |
| Armor Test | German (moves first) | One: the column (a halftrack, a PzKpfw IIIH, a truck, an infantry gun, a crew, a squad, an 8-1) | 7 | One entry area, the north edge, Turn 1 | None | Off board only: who rides in which vehicle and which vehicle tows the Gun. No hex is chosen; the entry hex is chosen when the column enters |

The Tractor Works' Russian remnants are order 3. They are the first side's own second group, they set up after the Germans, and a plan for them would answer the German setup. They stay out of this pass, as in pass 30 (section 11, question 8).

No plan takes the Balance, as in pass 30.

## 3. What the code has now

- **The file and its reader** (`ScenarioSetupPlans`). Format `asl-setup-plans/1`. `Validate` refuses a file of more than three plans (`MostPlans`, counted over the whole file) and any plan whose side is not `card.Turns.SetsUpFirst`; a plan's groups must be groups of that side.
- **The offer** (`Play.razor`, `ComputePlansOffered`). Plans are offered when the game is in setup, no hand-over waits, the viewer is not the adjudicator, `card.Turns.SetsUpFirst == viewAs`, and every group a plan places is setting up now. The line "This card has no setup plans" is shown only while `CurrentOrder == 1`. The new-game form's note counts every plan in the file as the first side's (`ChosenPlanCount`).
- **Nothing of a plan is in the game** (pass 30, D7). The chosen plan, the list, the plan shown on the map, the review, and the notice after Confirm are fields of the page; `ResetForView` and `ChangeGame` clear them. The gate receives placements only.
- **What the second side sees** (`GameView.Of`, `GamePlanner.OutOfSight`; rulings R23.1, R23.3; A2.9, A12.12). While the first side sets up, its groups are out of the other side's sight. Once its order is complete, the second side's view holds: the top counter of each enemy stack not under "?", as a unit; every other unit of that stack as a `SealedPresence` marked `Uninspected`; every concealed unit and every Dummy as a `SealedPresence`; and nothing of hidden units, of SW and other equipment held by a withheld counter, or of a Gun beneath a unit. A presence carries its side and its Location, level included. So the view already gives, for each hex and level, how many enemy counters stand there.
- **The second side's own setup** is out of the first side's sight while it sets up (the same rule, one order on).
- **The setup mode** (pass 30, section 11): `SetupBar`, the Plans, Counters, and Card OB tabs, `SetupPlanPicker`, `SetupCounters`, draft counters, the map's plan switcher with `[` and `]`, the zoom to the setup areas, the review, refusal links, and the notice after Confirm. None of them is tied to the first side except through `PlansOffered`.

## 4. Claude Design's response, checked

Its model is sound and can be built on the pass 30 page. What it has right, checked in the code:

- The game stores no plan id (D7), so the Studio cannot leak the first side's choice. The first side's list, chosen plan, and change count are page fields cleared at the hand-over.
- `PlansOffered` is held to the first side by one condition, and its other rule (every group a plan places is setting up now) already covers a later order.
- The second side's view carries the enemy's stacks as counts by Location, so a comparison can be computed from the view alone.
- The adjudicator is offered no plans, as now.
- A development-only switch exists as it says (`ScriptedDice`: the Development environment with `Play:ScriptedDice` set).
- Reload in the middle of setup is covered by the pass 30 fixes.

Where it is wrong, or cannot be built as written:

| # | Its claim | What the code or the cards say | What the design does |
|---|---|---|---|
| 1 | Armor Test: "The first-side plans aren't written yet, so no responses can be." | One Russian plan is kept, Forward screen. And the German side there never sets up on board: its one group only enters. | Armor Test is in the pass, with an off-board loading plan for any setup (section 2; question 5). |
| 2 | The comparison counts "presences" in a plan's footprint. | The view shows units and Dummies only. SW, a manned Gun, and hidden counters are not in it. A footprint that counted every placement would never match: Forward line's F5 holds a squad, an LMG, and a leader, and the Russian view shows two counters. | A plan's footprint is computed as the other side would see it: units and Dummies on the map and not hidden, by hex. Equipment and hidden counters are left out (D4). |
| 3 | "9 of 10 hexes match", with no word on what the 10 is. | If the 10 is the plan's hexes, a stack the first side added outside the plan costs nothing. | The 10 is the hexes of the plan's footprint together with the hexes where a stack is seen (D4). |
| 4 | The Tractor Works: "Hidden core puts 3 in each of Y3, Y4 and Y5, where All round puts 2." | All round puts 2 in Y3 and Y4 and 4 in Y5 (two squads, the 9-2, a Dummy). | The point stands: the counts tell the three plans apart. |
| 5 | "Confirm by ruling that showing each presence is right." | It is ruled already: R23.3 with A2.9 (pass 23). The top counter is shown and the rest are counted. | No new ruling. |
| 6 | "The cap of three (`MostPlans`) applies per side." | Today the cap is three for the whole file. | The cap becomes per side and setup order, plus the one for any setup (D2). |
| 7 | A plan's side may be "a side with a group in a later setup order". | The Tractor Works' Russians have a group of order 1 and one of order 3, so the side alone does not say which order a plan is for. | A plan places groups of one setup order, and that order is the plan's (D2). |
| 8 | "`/1` files are read unchanged." | `Validate` requires the format to equal one constant. | Both formats are accepted; a `/1` file may hold no `answers` (D2). |
| 9 | Step 1 "with no file changes" gives the second side plans. | It also needs the file cap, the new-game form's count of plans, and the "no setup plans" line, which is tied to `CurrentOrder == 1`. | Task 30b.1 covers the four places. |
| 10 | "It can tell the player nothing the player couldn't work out by eye." | True of the Studio. But the plans are public, so a full match against an unchanged plan tells the second side what the plan's text says is under each stack: which squad holds which SW, which "?" of the Tractor Works are Dummies, and, on Armor Test, where the hidden Gun is. Armor Test has one Russian plan, so an unchanged setup is read at once. This is so since pass 30; the score makes it effortless. | Section 6 and question 3. |
| 11 | "Rehearse" as a tool of the Studio. | It can be built. But my Studio's browser script already does it (pass 30 chose, proposed, and confirmed each plan by script), and the tool would add a product path whose only user is the plans' author. | Not built; the script does it (question 6). A backlog row. |
| 12 | The map "hatched in the enemy's colour". | The marks layer draws outlines and counts today (`CardMaps.Marks`, `GameMaps.Layers`); a hatch needs an SVG pattern in the layer, to be confirmed at the build. | Hatched if the layer takes a pattern; otherwise a dashed outline in the enemy's colour with its count. Either way it is told apart from the gold draft outline by more than colour. |
| 13 | After the second side's Confirm, "play begins". | When both sides have set up, the non-OB "?" comes before play (ruling R23.6). | The notice after Confirm keeps the pass 30 wording, which names what comes next. |

## 5. Draft decisions

**D1. Responses (Claude Design's model B).** A plan of the second side may name the first-side plan it answers. A card offers, for the second side, at most one response for each first-side plan and at most one plan for any setup. So the second side's Plans tab holds at most four plans on today's cards.

**D2. The data.** One file per card, as now; section 7 has the format.

**D3. Every response is always offered.** Which responses are shown never depends on the game: all of the second side's plans are listed whenever that side sets up, each under the first-side plan it answers. Only their order depends on the comparison (D4). Nothing is hidden for a low score.

**D4. The comparison, from the view alone.**

- A new static class in the Play library, `SetupPlanMatch`, with one function of a `GameView`, a first-side plan, and the catalog. It cannot take the `GameState`, so it cannot read what the view leaves out.
- **What is seen:** for each hex, the number of enemy counters the view holds there: its enemy units on the map and its sealed presences, all levels of the hex together.
- **A plan's footprint:** for each hex, the number of the plan's units and Dummies placed on the map and not hidden. SW, Guns, and hidden counters are left out, since the view never shows them.
- **The score:** the hexes where the two counts are equal, of the hexes that are in either. "9 of 10 hexes match."
- The responses' groups are sorted by score, the highest first, and the first is marked "Closest to what you see". When the best score is under six tenths, "For any setup" comes first and the bar says "Their setup does not follow any of their plans closely."
- One line under the tab: "Based on the stacks you can see. Dummies and hidden units can mislead it."
- Levels are not compared (question 9): the view knows them, but one squad moved upstairs should not count as a different setup.

**D5. The Plans tab for the second side.** `SetupPlanPicker` gains groups. Each group is a disclosure button: "If they set up like Forward line, 10 of 10". Opened, it shows "Their plan" (the first-side plan's name, idea, and what it gives up, which are public) and the response's card, as in pass 30: "Show on map", "Use this plan", "More". A first-side plan with no response keeps its header and score, with "No response written". "For any setup" is the last group. The first side's tab is unchanged.

**D6. The map.** "Show on map" on a response draws its draft counters, as now, and under the enemy's stacks the footprint of the plan it answers: each hex of the footprint marked in the enemy's colour with the plan's count. A toggle on the map's toolbar, "Their plan's outline", turns it off. Where a mark has no stack on it, or a stack has no mark under it, the difference is seen. The map's plan switcher holds the second side's plans and "My list". The zoom takes in the viewer's setup areas and the enemy stacks seen.

**D7. The bar, the review, and the notice.** The bar adds "The German side has set up: 10 stacks seen" and, for a list from a response, "From Street ambush (answers Forward line)". The review names the response and the plan it answers. The notice after Confirm is as in pass 30.

**D8. A response made for an earlier version of its plan.** A response records a SHA-256 of the answered plan's placements. When the plan's placements have changed since, the response is marked "Made for an earlier version of Forward line", beside pass 30's "Made for an earlier version of this card".

**D9. Armor Test.** The column's plan is for any setup and places all seven counters off board: the Gun in tow of the truck, the crew aboard the truck, the squad and the 8-1 aboard the halftrack (the loading the gate accepted in pass 30's check). The Plans tab still shows the header "If they set up like Forward screen" with its score.

**D10. The gate decides,** as in pass 30. A response is proposed through the same setup action and is refused with the gate's reasons when it is stale or illegal. Nothing of a plan is stored in the game.

## 6. Disclosure

**What the second side may know** (rulings R23.1 to R23.4; A2.9, A12.11, A12.12, A12.3): where each enemy stack stands and how many counters it holds; the top counter of a stack not under "?"; the card; and the card's setup plans, which are public like the card. **What it may not know:** which plan the first side used, what it changed, what is under a "?", which "?" are Dummies, and where hidden units are.

How the design keeps to that:

- **No plan id is stored,** in the game or anywhere else, not even for the adjudicator. The first side's chosen plan, list, changes, review, and notice are cleared at the hand-over (pass 30; `ResetForView`).
- **The offer does not depend on the first side's choice.** All of the second side's plans are listed, whatever the first side did, by plan or by hand (D3).
- **The order depends only on the second side's own view** (D4). `SetupPlanMatch` takes the `GameView`. A test holds it: two games whose stacks look the same to the second side and whose contents differ give the same scores.
- **The first side adjusted its plan, or set up by hand:** the scores fall, "For any setup" comes first, and every response stays usable.
- **The second side's own choice** is kept from the first side the same way: its plans show only in its view while it sets up, and its setup is out of the first side's sight until its order is complete.
- **The adjudicator** is offered no plans.

**What the design cannot keep, and the user decides (question 3).** The plans are public. A first-side setup left exactly as a plan is therefore readable: a full match tells the second side that the stacks stand as the plan says, and the plan's text says what is in each stack. On The Tractor Works that includes which hexes hold only Dummies (Hidden core's Y3, Y4, Y5). On Armor Test it includes the hidden Gun's hex, and that card has one plan. A table player with the file open could work this out since pass 30; the score shows it at a glance. The Studio leaks nothing here, but the first side should be told. The recommended answer: the first side's Plans tab and its review say "Setup plans are public. Left unchanged, the other side may recognise this one: move what you want kept secret."

## 7. The data

One file per card, `<card id>.setups.json`, as in pass 30. The card's text is untouched, so its SHA-256 and every saved game are untouched; a game stores no plan.

```json
{
  "format": "asl-setup-plans/2",
  "card": "guards-counterattack",
  "plans": [
    { "id": "forward-line", "side": "german", "...": "as in pass 30" },
    {
      "id": "street-ambush",
      "cardSha256": "ad43d29d...",
      "side": "russian",
      "answers": { "plan": "forward-line", "placementsSha256": "..." },
      "name": "...", "idea": "...", "givesUp": "...", "terrain": ["..."],
      "placements": [ { "id": "r-squad-1", "definition": "defender-squad", "group": "russian-1", "at": "bd01:N4:1" } ]
    },
    { "id": "any-setup", "side": "russian", "...": "no answers: for any setup" }
  ]
}
```

- **The format version changes** to `asl-setup-plans/2`. A `/1` file is still read, with pass 30's rules: it may hold no `answers`. The four built-in files become `/2`; the user's `p30-guards-copy.setups.json` stays `/1` and reads as before.
- **A plan's order.** Every group a plan places has one setup order, and that is the plan's order. Its side is the side of those groups.
- **`answers`** names a plan of the same file with an earlier order and another side, and holds the SHA-256 of that plan's placements in a fixed form (its placements sorted by id, one line each). A plan of order 1 has no `answers`.
- **The caps.** For each side and order: at most three plans that answer nothing when the order is 1 (as now); for a later order, at most one response for each answered plan and at most one for any setup. Ids are unique in the file.
- `Validate` checks a plan's definitions and groups against the plan's own side, in place of the first side.
- Reading still checks form only; whether a response is a legal setup is the gate's question.

## 8. The page: what is reused, what is new

**Reused as it is:** the setup mode and its three tabs; `SetupBar`'s Propose, Clear, and question; `SetupCounters` and everything of the list (stacks, levels, holders, "Move", the limited area's count, the "?" count); draft counters and a side's own Dummies; the map's plan switcher and `[` and `]`; "Use this plan" asking before a changed list is replaced; the review, the refusal links, and the notice after Confirm; the hand-over clearing the view; the layout under 1024 pixels with the small bar on the Map tab.

**Changed:**

- `ScenarioSetupPlans`: the format, a plan's order, `answers`, the caps (section 7).
- `ComputePlansOffered`: offered to the side whose groups are setting up now, in place of the first side.
- The "no setup plans" line and the new-game form's note: counted for the side they speak of.
- `SetupPlanPicker`: groups by answered plan, with the score and "Their plan".
- The bar's and the review's wording (D7).

**New:**

- `SetupPlanMatch` in the Play library (D4).
- The answered plan's footprint on the map, with its toggle (D6).
- The zoom taking in the enemy stacks seen (D6).
- The first side's note that plans are public (section 6), if the user takes question 3 as recommended.

## 9. The plans

Made in task 30b.7, one card at a time, each card's plans stopped for the user's approval with a screenshot of each in the chat. Order: Gambit (the clearest footprints), The Guards Counterattack, The Tractor Works, Armor Test.

- Each response is made from the scratchpad terrain and LOS script on the Studio's assemblies, as in pass 30 (its design, section 6), with the cited lines checked again in my Studio's LOS tab.
- To be accepted by the gate, a response needs the plan it answers committed first. A browser script in my Studio does that for each pair: a new game, "Use this plan" on the first-side plan, Propose, Confirm, the hand-over, then the response chosen, proposed, and confirmed. The plan for any setup is proposed after each of the card's first-side plans.
- The tables go into this section as in pass 30: the idea, what it gives up, the terrain facts, the placements.

The Tractor Works' responses place 39 counters and 12 "?" each. If four of them is more than the card needs, the user says so at that card's stop.

## 10. Tests

Written at the merge gate, by the pass's rule.

`BacklogPass30bTests` (Play):

- Each built-in file parses as `/2` with no diagnostic; a `/1` file still parses; a `/1` file with `answers` is refused.
- The form's refusals: a response naming no plan of the file, a plan of the same or a later order, or a plan of its own side; two responses to one plan; two plans for any setup; a plan whose groups have two orders; none throws.
- Each response names its card's current SHA-256 and its answered plan's current placements hash.
- A card's hash is the same with and without its setups file (pass 30's test, kept).
- On board 01 (`Board01Fixture`), The Guards Counterattack and The Tractor Works: with each first-side plan committed, the gate accepts its response and the plan for any setup.
- `SetupPlanMatch`: each first-side plan, committed unchanged, scores a full match against itself and less against the others; two games whose stacks look the same to the second side and differ beneath give equal scores; SW, hidden counters, and a manned Gun do not change a score.

Gambit and Armor Test are on board 4, which the tests have no terrain for (backlog section 46). Their responses are checked for form in the tests and accepted by the gate in the Studio on the real boards, as pass 30's plans were. Building the board 4 fixture here is question 7. Armor Test's column sets up off board, so its acceptance may need no terrain; that is tried at the gate.

`SetupPlansPageTests` and component tests (Studio): the second side's Plans tab with its groups, scores, and "Their plan"; the first side's view and the adjudicator's never showing a response; a response shown on the map leaving the list alone and used filling it; the bar naming the response and the plan it answers. The harness lessons of pass 30 apply: a non-minimal card, `UseViewport`, the Studio test project run before the full suite.

## 11. Questions for the user

Each with a recommendation.

1. **The model.** Recommended: responses (D1), as Claude Design proposes. The other way is plans for the second side with no link to the first side's plans, which is task 30b.1 alone.
2. **Show the match score, or only sort by it?** Recommended: show it. It tells the player how far to trust the order.
3. **An unchanged public plan is readable** (section 6). Recommended: keep the plans public and the score shown, and tell the first side on its Plans tab and in its review. The other ways: sort and score by hexes only, without counts, which reads less; or no comparison at all, which leaves the player to compare by eye.
4. **How many plans for the second side.** Recommended: one response for each first-side plan, plus one for any setup, as Claude Design proposes. That is up to four a card and up to thirteen in all, each reviewed by the user.
5. **Armor Test.** Recommended: one plan for any setup, the column's loading, off board. The other way is no plan for that side, since "Set up" already puts the column off board in one click and the loading is three changes.
6. **"Rehearse".** Recommended: not built; my Studio's script commits the first-side plan for each pair. A backlog row keeps the idea.
7. **The board 4 fixture in the tests** (backlog section 46). Recommended: leave it in the backlog. Gambit's map is board 4 over board 2 reversed, so the fixture is two boards and a composed map, and the Studio check on the real boards covers the gate.
8. **The Tractor Works' remnants** (order 3). Recommended: later, as in pass 30. The data's rules already allow a plan of any order.
9. **Compare by hex, or by hex and level?** Recommended: by hex.

## 12. Tasks and estimate

| Task | What it changes | Estimate |
|---|---|---|
| 30b.1 Plans for any side that is setting up | `ComputePlansOffered`, the per-side cap and the plan's own side in `Validate`, the "no setup plans" line, the new-game form's count. | 0:30 |
| 30b.2 The format `asl-setup-plans/2` | A plan's order, `answers` with its placements hash, the caps and their diagnostics, `/1` still read, "Made for an earlier version of" a plan. | 0:45 |
| 30b.3 The Plans tab in groups | `SetupPlanPicker` grouped by answered plan, "Their plan", "For any setup", "No response written"; written order, no scores yet. | 0:45 |
| 30b.4 The comparison | `SetupPlanMatch` from the `GameView`; the score on each group, the sort, "Closest to what you see", the help line, the bar's line when nothing matches. | 1:00 |
| 30b.5 The answered plan on the map | Its footprint under the enemy's stacks, the toggle, the legend line, the zoom taking in the enemy stacks. | 1:00 |
| 30b.6 The words | The bar, the review, "N stacks seen", and the first side's note that plans are public. | 0:30 |
| 30b.7 The plans | Up to thirteen plans on four cards, one card at a time, each accepted by the gate in my Studio and approved by the user. | 2:00 |
| | Overhead: the three reviews and their fixes, the Studio check of the whole pass, the documents, the tests, the merge gate | 1:30 |
| | **Pass 30b total** (build 6:30) | **8:00** |

Pass 30 took about 3:26 against 9:45, so the estimate is likely high.
