# ASL Unit Backlog Pass 30b Design

**Status:** Approved 2026-10-03, with the answers of section 14. The page and data tasks (30b.1 to 30b.6 and the page's part of 30b.8) are built and checked in the Studio on branch `feature/asl-backlog-pass-30b`; the plans (30b.7) are made one card at a time, each stopped for the user's approval: section 15. Pass 30b (Setup plans for the side that sets up second) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, a Studio pass.

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
| 30b.8 Hidden and concealed setup (section 13) | "Conceal every stack that may be" for the non-OB "?", the notice naming that step, what an unchanged plan gives away on its card and in the review, "Swap" for two stacks, and the ten plans of pass 30 checked against D11. | 1:00 |
| | Overhead: the three reviews and their fixes, the Studio check of the whole pass, the documents, the tests, the merge gate | 1:30 |
| | **Pass 30b total** (build 7:30, revised 2026-10-03 for section 13) | **9:00** |

Pass 30 took about 3:26 against 9:45, so the estimate is likely high.

## 13. Keeping hidden what the rules let a side hide

**Added 2026-10-03.** The user took the recommended answers to section 11 on one condition: that the design looks for ways to give each player as much hidden and concealed setup as the rules or the card allow, with liberal use of Dummies. This section is that work. It waits for the user's word like the rest.

**What each card allows.**

| Card | Side | OB "?" | Hidden at setup | Non-OB "?" once both sides have set up (A12.12; ruling R23.6) |
|---|---|---|---|---|
| The Guards Counterattack | German (first) | None | None | Every stack out of the LOS of the Russian units: F6 and L7 in Out of sight, the stairwell stacks of Tripwire and reserve |
| | Russian (second) | None | None | Every stack out of the LOS of the German units |
| Gambit | British (first) | Forbidden on the five counters (SSR 2) | None | The five counters, where the Germans end up not seeing them; the card's SSR bars a "?" at their setup, and whether it bars this one too is checked against the card before a plan leans on it |
| | German (second) | None | None | Every stack out of the LOS of the five British counters, which are few and seen: most of the German setup |
| The Tractor Works | Russian 308th (first) | 18 | None | None left: every stack is under "?" already |
| | German (second) | 12, Kampfgruppe Stahler's | None | The Engineers' and Tienham's stacks out of the LOS of X3 |
| Armor Test | Russian (first) | 2 | The Gun and its crew, Emplaced in woods (A12.34) | The stack left without a "?" (P8), if the column's entry gives no LOS; the column is off board, so nothing sees it |
| | German (second) | None | None | None: the column is off board |

So the OB's "?" matter on one card and a half. On the other cards concealment comes from the non-OB "?", which the page offers today as a list of checkboxes after both sides have set up, with no word of it in any plan.

**D11. A plan uses everything its side may hide.** A rule for making plans, checked for each plan before the user sees it and stated in its table:

- Every OB "?" is used: a stack of real units under "?", or a Dummy.
- Every counter that may set up hidden does (an Emplaced Gun with its crew).
- Dummies stand where they change what the other side reads: a Dummy stack as tall as a real one, in a hex the real force might hold, and never a lone Dummy where every real stack has three counters.
- In a stack that cannot be under "?", the top counter is a plain squad, not the leader or the squad with the MG, since before play the other side sees only the top counter (A2.9; ruling R23.3). Which counter the game takes as the top is confirmed at the build.
- Where a card's first-side plans can share one footprint (the same hexes with the same counts) without losing their ideas, they do, so the comparison cannot tell them apart.

The ten plans of pass 30 are checked against this rule in task 30b.8. They are approved and merged, so a change to any of them is put to the user, plan by plan. The Tractor Works' three use all 18 "?"; Forward screen uses both of its "?" and hides the Gun.

**D12. The non-OB "?" in one step.** Once both sides have set up, each side's block gains "Conceal every stack that may be": it ticks every Location the gate allows that side (`GamePlanner.NonObConcealment`, which the page already reads). The player unticks what should stay open and proposes as now. A "?" costs a unit nothing until it acts, so all of them is the right start. The notice after the last setup's Confirm names the step: "Next: each side may place a non-OB \"?\"." The gate and ruling R23.6 are unchanged.

**D13. A plan is made for its non-OB "?".** A plan's terrain facts say which of its stacks stand out of every LOS from the other side's setup areas, so that they take a non-OB "?" whatever the other side does, and a response's facts say which stand out of the LOS of the answered plan's footprint. On The Guards Counterattack and Gambit this is the only concealment there is, and a plan that keeps two platoons out of sight is then two platoons under "?" at the start of play.

**D14. The first side is told what an unchanged plan gives away, and how to hide it.** In place of section 6's one general line, the plan's card and the review list it from the plan's own placements: the hidden counters and their hex, the hexes that hold only Dummies, and the stacks whose SW a reader of the plan would know. With it, one sentence: "Setup plans are public. Move or swap these before you propose, and the other side reads the plan wrong." The list is computed from the plan in the first side's own view; nothing of it reaches the other view.

**D15. "Swap" beside "Move".** Two stacks of the list exchange their hexes in one step: "Swap" on a stack, then a click on another stack of the list. Swapping a Dummy stack with a real stack of the same height leaves the footprint as it was and changes what is under it, which is the cheapest way to make an unchanged-looking plan lie. It is the player's act, never the Studio's: the Studio shuffles nothing by itself, since which stack stands where is the plan's idea.

**D16. The comparison stays honest about Dummies.** Dummies count as presences (D4), so a liberal use of them moves the score, as it should. The help line under the second side's Plans tab already says so.

**What is not proposed.**

- No "?" beyond the card's allotment and A12.12. A plan places the card's fixed OB.
- No hidden setup by plan where no rule or SSR gives it. On these four cards that is the Armor Test Gun alone.
- No automatic shuffle of a plan's contents at "Use this plan" (D15).
- Whether a stack set up under "?" in Concealment Terrain out of enemy LOS should be charged to the OB's "?" is still backlog section 46's row, waiting for a ruling. If the ruling frees those "?", the Tractor Works' plans gain Dummies.

**One more question for the user.**

10. **Shared footprints (D11, last point).** On The Tractor Works the three 308th plans could be remade to show the same count in every hex of X3, so that a German player reads nothing from the stacks. It costs Hidden core its idea (three hexes of Dummies only, 12 Dummies against 9), and the three plans are approved. Recommended: leave the three as they are, tell the first side what each gives away (D14), and give it "Swap" (D15).

## 14. The user's answers, and what is built

**The answers, 2026-10-03.** The user took the recommended answer to each of questions 1 to 9, and approved section 13. On question 10 the user took the revised recommendation, made once the counts were worked through:

- A shared footprint needs the three 308th plans to have the same number of real stacks, since the 18 "?" pay for stacks and Dummies alike. With nine real stacks Hidden core loses its idea; with six it stays exactly as it is.
- So East front is remade and All round is replaced on Hidden core's counts (W4 3, W5 3, X2 2, X3 2, X4 4, X5 2, Y3 3, Y4 3, Y5 3): every plan holds six hexes with real units and three with Dummies only, and the plans differ in which three.
- The German side then reads nothing from an unchanged setup, the comparison gives equal scores on that card, and the Germans get one plan for any setup there in place of four.
- The remade plans come to the user for approval at the Tractor Works stop of task 30b.7.

**Built 2026-10-03**, tasks 30b.1 to 30b.6 and the page's part of 30b.8, in one stretch, since the reader, the offer, the tab, and the map share the page's code. Checked in my Studio on port 6670 at 1366x768 and committed. No built-in card has a second side's plan yet; the check used a scratch user card, `p30b-guards` (a copy of The Guards Counterattack in `src/ASL/boards/cards`, with a `/2` setups file: the three German plans, two Russian test answers, one of them with a stale placements hash, and a test plan for any setup).

As designed:

- `ScenarioSetupPlans`: the format `asl-setup-plans/2`, with `/1` still read under pass 30's rules; `SetupPlan.Answers`; a plan's order from its groups (`OrderOf`); `PlacementsSha256`; the caps by side and order.
- `SetupPlanMatch` (Play library): `Footprint`, `Seen`, and `Score`, from a `GameView` only.
- `ComputePlansOffered`: plans for the side whose groups are setting up now; a group that only enters counts while it owes counters off board (Armor Test's column).
- `SetupPlanPicker`: groups as disclosure elements, each with its score, "Their plan", its plans or "No response written", and "For any setup"; the first side's tab is unchanged.
- The map: the answered plan's footprint, the "Their plan's outline" toggle and its legend, and the zoom taking in the enemy stacks seen.
- The words: "N stacks seen" under the bar, "From <plan> (answers <plan>)" in the bar, the review, and the notice.
- Section 13: "Conceal every stack that may be" (D12), what an unchanged plan gives away under "More" and in the review of an unchanged plan (D14), and "Swap" on a stack of the list (D15).

What differs from the design:

- **The footprint is a dashed purple outline with the count in a disc,** not a hatch: the marks layer draws outlines and labels (`GameMaps.HexMark`), and the dashed line and the number tell it apart from the gold draft outline without color.
- **"Swap" works by hex,** all levels of the two hexes together, each counter keeping its level when the other hex has it.
- **The groups appear only once the other side's setup is in sight.** Until then the plans are listed as they are.
- **"Closest to what you see" needs a single best score of six tenths or more.** When two plans tie, neither is marked.

**Studio check, 2026-10-03**, 1366x768, by script, games `p30b-g20918` and `p30b-v90075`:

- The new-game form: "This card has 3 setup plans for the German side, and 3 for the side that sets up after it".
- The German view: three plans, no groups. Forward line used unchanged: the review ends "Setup plans are public: if the other side recognises Forward line, it knows which unit holds each of its 8 SW."
- The Russian view while the Germans set up: no Plans tab. The adjudicator's view: no Plans tab.
- After the German Confirm and the hand-over: "The German side has set up: 10 stacks seen."; four groups, Forward line first at 10 of 10 and marked closest, Tripwire and reserve at 5 of 13 with "No response written", Out of sight at 4 of 13 with its answer marked "Made for an earlier version of Out of sight", then "For any setup".
- "Show on map" on an answer drew the answered plan's footprint (7 hexes for Out of sight) with its legend, and left the list empty.
- "Use this plan" filled 25 counters; the bar read "From Test answer to Forward line (answers Forward line)". "Swap" on the E4 stack, then the N5 stack's name, exchanged the two hexes (4 changes); the gate refused the swapped setup with four reasons, each a link; "Reset to" asked first and restored the plan.
- Proposed unchanged and confirmed: "The Russian setup is in the game, from Test answer to Forward line (answers Forward line). Every group has set up. Each side may now place a \"?\" on its stacks out of the enemy's LOS (A12.12), under Counters".
- "Conceal every stack that may be (4)" ticked all four Locations and then stood disabled.

## 15. The plans

Task 30b.7, one card at a time. A plan's screenshot goes to the user in the chat, not into the repository.

### 15.1 Gambit: the Germans

Made 2026-10-03 from the Studio's terrain and LOS on the card's map (board 4 over board 2 reversed; LOS definitive), read with the scratchpad script of pass 30: the LOS from the four British post Locations of the card's plans (P6 and O6 at level 1, H6, I5) to all 83 hexes numbered 8 to 10, and from the ten German hexes used back to the posts, the centre road, and the exits. Each plan was used in a new game in my Studio with the British plan it answers committed first (`p30b-gb-1` to `p30b-gb-3`), and Three roads after each of the three British plans (`p30b-gb-4` to `p30b-gb-6`): the gate accepted all six setups (11 counters each). The user kept all four on 2026-10-03, with no remarks.

What the plans answer. The five British counters are on the map, none under "?" (SSR 2), when the Germans set up in the hexes numbered 8 to 10: eight 5-4-8 squads, the 9-2, the 9-1, and the 8-1, with no SW and no OB "?". The Germans move first. The other fifteen British counters enter along the north edge in the British half of Turn 1 and must exit 20 Exit VP off the south edge near 2Y1 (below 4I10, the west), 2Q1 (below 4Q10, the centre), or 2I1 (below 4Y10, the east). So the Germans have one turn against five counters, and then eight squads against twelve on three roads.

Facts every plan rests on:

- The British post tells the road: each British plan stands on one, and the comparison tells the three apart by their two hexes (2 of 2 against 0 of 3 or 4).
- The Germans have no OB "?". Their concealment is the non-OB "?" of A12.12, for stacks no British unit sees once both sides have set up. Each plan keeps stacks out of sight for it (section 13, D13), and the Studio offered exactly those stacks after each Confirm.
- In each stack a squad is the first counter, so a leader is never the counter the British see on top (D11).
- No plan takes the Balance (one LMG).

**Storm the farmhouse** (`storm-the-farmhouse`), answers The farmhouse

| | |
|---|---|
| The idea | The Germans move first, and the farmhouse holds two squads and the best British leader, alone until the battalion enters. Three squads and the 9-2 wait behind the house at P8, three squads and the 8-1 behind the woods at M8, and two squads with the 9-1 fire from the woods at R8: the house is attacked from the south and the west in the first German turn, before help can reach it. |
| What it gives up | Nothing stands east of hexrow R or west of hexrow L, so both flank roads are open; if the farmhouse holds through the first turn, the battalion enters behind eight squads that are all in the centre. |
| Terrain facts | P9 has no LOS to or from the upper floor of the farmhouse, O6 or P6: the house at P8 blocks it. It is three hexes from P6 and one from the road hexes Q9 and Q10.<br>L8 has no LOS to or from the upper floor of O6 or P6: the woods at M8 block it. It is four hexes from O6.<br>R8 is woods and sees the upper floor of P6 at range 3; O6 does not see it. |
| Placements | P9: 3 x 5-4-8 squad, 9-2 leader; L8: 3 x 5-4-8 squad, 8-1 leader; R8: 2 x 5-4-8 squad, 9-1 leader |
| Non-OB "?" offered after setup | L8 and P9 |

**Seal the west road** (`seal-the-west-road`), answers West woods

| | |
|---|---|
| The idea | The British post in the woods at H6 and I5 marks the west road as the battalion's way. Three squads and the 9-2 in the woods at K9 fire on H6 and block the road south of it, a squad holds the house at I10 beside the west exit, two squads and the 9-1 start unseen at N8 to come at the woods from the east, and two squads with the 8-1 behind the house at P8 keep the centre road. |
| What it gives up | The east third of the board is empty, and the centre is two squads; the K9 stack is in the LOS of the squad at H6 from the start. |
| Terrain facts | K9 is woods and sees H6 at range 4 through a hindrance of 3, and the road hex I9 at range 2; I5 does not see it.<br>I10 is a wooden building; it sees H6 at range 4 and I9 at range 1.<br>N8 has no LOS to or from H6 or I5: the woods at M8 block both. It is six hexes from H6.<br>P9 has no LOS to or from H6 or I5, and sees the centre road at Q9 and Q10 at range 1. |
| Placements | K9: 3 x 5-4-8 squad, 9-2 leader; I10: 5-4-8 squad; N8: 2 x 5-4-8 squad, 9-1 leader; P9: 2 x 5-4-8 squad, 8-1 leader |
| Non-OB "?" offered after setup | N8 and P9 |

**Both posts** (`both-posts`), answers Two posts

| | |
|---|---|
| The idea | Each British post is one squad, and the two cannot help each other. Three squads and the 9-2 behind the house at P8 go for the farmhouse, three squads and the 9-1 in the woods at K9 go for the woods at H6, and two squads with the 8-1 wait unseen in the woods at T9 to turn east or centre once the battalion shows its route. |
| What it gives up | The force is in three parts from the start, and only the reserve of two squads covers the east exit; the K9 stack is in the LOS of the squad at H6. |
| Terrain facts | P9 has no LOS to or from the upper floor of P6, nor H6: the house at P8 and the woods at M8 block them. It is three hexes from P6.<br>K9 is woods and sees H6 at range 4 through a hindrance of 3; the farmhouse does not see it.<br>T9 is woods; neither post sees it. It sees the centre road at Q9 and Q10 at range 3 and the east exit at Y10 at range 5. |
| Placements | P9: 3 x 5-4-8 squad, 9-2 leader; K9: 3 x 5-4-8 squad, 9-1 leader; T9: 2 x 5-4-8 squad, 8-1 leader |
| Non-OB "?" offered after setup | P9 and T9 |

**Three roads** (`three-roads`), for any setup

| | |
|---|---|
| The idea | One group stands in a building on each way south, whatever the five British counters show: three squads and the 9-2 in the house at P8 on the centre road, two squads and the 8-1 in the house at I10 by the west exit, two squads and the 9-1 in the house at X8 and a squad in the house at Y9 by the east exit. |
| What it gives up | No group is strong enough to attack the British posts in the first turn, and the three cannot help each other quickly: the Germans wait for the battalion to choose its road. |
| Terrain facts | P8 is a wooden building; it sees the centre road at Q9 at range 1 and Q10 at range 2, and the upper floor of the farmhouse sees it at range 2 to 3.<br>I10 is a wooden building beside the west exit; it sees I9 at range 1. Only a post at H6 sees it, at range 4.<br>X8 and Y9 are wooden buildings; they see the east exit at Y10 at range 2 and 1. The farmhouse, H6, and I5 do not see them. |
| Placements | P8: 3 x 5-4-8 squad, 9-2 leader; I10: 2 x 5-4-8 squad, 8-1 leader; X8: 2 x 5-4-8 squad, 9-1 leader; Y9: 5-4-8 squad |
| Non-OB "?" offered after setup | X8 and Y9 against every British plan; I10 too against The farmhouse, where no post is at H6 |

Each plan's terrain facts in the file end with the line on the non-OB "?", and the two that lean on a grain hindrance carry the note on grain in May, as pass 30's plans do.

### 15.2 The Guards Counterattack: the Russians

Made 2026-10-03 from the Studio's LOS table of pass 30 on board 01 (Verified, LOS definitive): every German setup Location, at each level, against the 27 Russian setup Locations. Each plan was used in a new game in my Studio with the German plan it answers committed first (`p30b-gc-1` to `p30b-gc-3`), and Two up, two back after each of the three German plans (`p30b-gc-4` to `p30b-gc-6`): the gate accepted all six setups (25 counters each). The user kept all four on 2026-10-03, with no remarks.

What the plans answer. The Russians set up second and move first. Every OB line names its building, so the choice is small: the hex and level of the 12 Guards squads and the 10-2 inside building F3 (E4, F3, G3, G4, each with levels 0 to 2), and of the 308th's four squads and commissar inside building N4 (M5, N3, N4, N5). J2 (a squad, the 9-1, the MMG), M2 (three squads), and N2 (one squad) are one-hex buildings with a ground level only. The Russians win by Control of the German buildings or by a three to one ratio of unbroken squads, in five turns, so the first turn's fire and the first crossing of the street decide much.

Facts every plan rests on, from the LOS table:

- G3 at ground level, N3 and N4 at ground level, and N2 are seen from no German setup Location.
- F3, at every level, is seen only from F5 and G6.
- E4 is seen from F5, G6, and H5 at range 2 to 3 and from J5 at range 5. G4 is seen from F5, G6, H5, J4, J5, and K4.
- M5 is seen from K4, K5, L6, and M7 at range 2; N5 from L6 and M7 at range 2.
- A 6-2-8 fires at full strength to range 2.
- The comparison told the three German plans apart each time: 10 of 10 or 7 of 7 for the plan used, and 4 or 5 of 11 to 13 for the others.
- The Russians have no OB "?". The stacks each plan keeps out of every German LOS were offered the non-OB "?" after Confirm, exactly as listed below. A squad is the first counter of every stack; the Russian view of the German setup showed squads on top of every German stack, which confirms that the first counter listed is the one seen (D11).

**Fire first** (`fire-first`), answers Forward line

| | |
|---|---|
| The idea | The German forward line stands in the Guards' LOS at range 2, and the Russians move first. Nine Guards squads on the upper floor of E4, F3, and G4, with the 10-2 at F3, fire on the F5 platoon in the first Prep Fire Phase; three squads wait unseen at G3 to cross once it breaks. The 308th fires from the upper floor of M5 and N5 on K4, K5, L6, and M7 at range 2. |
| What it gives up | The nine squads that fire do not move in the first turn, and they stand in the LOS of the German squads they fire on; only three squads are fresh for the first crossing. |
| Guards | E4, level 1: 3 x 6-2-8 squad; F3, level 1: 3 x 6-2-8 squad, 10-2 leader; G4, level 1: 3 x 6-2-8 squad; G3, ground level: 3 x 6-2-8 squad |
| 308th | M5, level 1: 2 x 4-4-7 squad; N5, level 1: 2 x 4-4-7 squad, 9-0 commissar; J2: 4-4-7 squad with the MMG, 9-1 leader; M2: 3 x 4-4-7 squad; N2: 4-4-7 squad |
| Non-OB "?" offered after setup | G3 and N2 |

**Cross unseen** (`cross-unseen`), answers Out of sight

| | |
|---|---|
| The idea | The German wing platoons have left the fronts of their buildings empty, so there is nothing to fire on and the first turn is for moving. The Guards start where the German squads that look out cannot see them: six squads and the 10-2 on two levels of F3, three at ground level in G3, and three at ground level in E4. They cross to F5, G6, and H5 in the first Movement Phase. The 308th waits at ground level in N3 and N4. |
| What it gives up | No Russian fires in the first turn, and the squads on the upper floor of F3 spend movement coming down; if the Germans did not set up as Out of sight, nine Guards squads stand packed in two hexes. |
| Guards | F3, ground level: 3 x 6-2-8 squad, 10-2 leader; F3, level 1: 3 x 6-2-8 squad; G3, ground level: 3 x 6-2-8 squad; E4, ground level: 3 x 6-2-8 squad |
| 308th | N3, ground level: 2 x 4-4-7 squad; N4, ground level: 2 x 4-4-7 squad, 9-0 commissar; J2, M2, and N2 as above |
| Non-OB "?" offered after setup | F3 at both levels, G3, N3, N4, and N2: 9 of the 12 Guards squads and all of building N4 start under "?" |

**Break the tripwires** (`break-the-tripwires`), answers Tripwire and reserve

| | |
|---|---|
| The idea | Each large German building shows one squad forward and holds the rest back. Six Guards squads and the 10-2 on two levels of G4 fire on the squad at H5 at range 2, and when it breaks the six squads waiting unseen at F3 and G3 cross to F5 and G6. The 308th does the same to the squad with the MMG at L6, from the upper floor of N5 and M5 at range 2. |
| What it gives up | G4 is in the LOS of H5, J4, and K4, and holds half the Guards and their leader; if the squad at H5 holds, the crossing waits. |
| Guards | G4, level 1: 3 x 6-2-8 squad, 10-2 leader; G4, ground level: 3 x 6-2-8 squad; F3, ground level: 3 x 6-2-8 squad; G3, ground level: 3 x 6-2-8 squad |
| 308th | N5, level 1: 2 x 4-4-7 squad, 9-0 commissar; M5, level 1: 2 x 4-4-7 squad; J2, M2, and N2 as above |
| Non-OB "?" offered after setup | F3, G3, and N2 |

**Two up, two back** (`two-up-two-back`), for any setup

| | |
|---|---|
| The idea | Half the Guards fire and half move, whatever the Germans show: three squads on the upper floor of E4 and three on the upper floor of G4 cover the street, and six squads with the 10-2 wait at ground level in F3 and G3 to cross where the fire tells. The 308th keeps two squads on the upper floor of N5 and two with the commissar out of sight at N4. |
| What it gives up | Neither the fire nor the crossing has the weight of a plan made for the German setup. |
| Guards | E4, level 1: 3 x 6-2-8 squad; G4, level 1: 3 x 6-2-8 squad; F3, ground level: 3 x 6-2-8 squad, 10-2 leader; G3, ground level: 3 x 6-2-8 squad |
| 308th | N5, level 1: 2 x 4-4-7 squad; N4, ground level: 2 x 4-4-7 squad, 9-0 commissar; J2, M2, and N2 as above |
| Non-OB "?" offered after setup | G3, N4, and N2 against Forward line; F3 too against Out of sight and Tripwire and reserve, where no German stands in F5 or G6 |

### 15.3 The Tractor Works: the 308th remade, and the Germans

Made 2026-10-03 from pass 30's LOS table of building X3 against the German setup Locations on board 01. Each of the three 308th plans was used in a new game in my Studio and accepted by the gate (31 counters each), and Three sides was then used in the German view of each game and accepted (48 counters; games `p30b-tw-1` to `p30b-tw-3`). They wait for the user's approval.

**The shared footprint (section 14, question 10).** The three 308th plans now show the same number of counters in every hex, all of them "?":

| Hex | W4 | W5 | X2 | X3 | X4 | X5 | Y3 | Y4 | Y5 |
|---|---|---|---|---|---|---|---|---|---|
| Counters seen | 3 | 3 | 2 | 2 | 4 | 2 | 3 | 3 | 3 |

Each plan has 13 units in six hexes, 12 Dummies, and three hexes of Dummies only; all 18 "?" are used (6 concealed stacks and 12 Dummies). In the German view the comparison read "9 of 9 hexes match" for all three plans in every game, with none marked closest, so an unchanged setup tells the Germans nothing of which plan it is.

| Plan | Hexes of Dummies only | Real units |
|---|---|---|
| Hidden core, unchanged | Y3, Y4, Y5 (the east face) | X4: 3 squads, 9-2. W4: 2 squads, MMG. W5: 2 squads, 2 LMG. X2: squad, LMG. X3: 2 squads, MMG. X5: 2 squads, HMG |
| East front, remade | W5, X2, X4 (the centre looks like a reserve of four) | Y3: 2 squads, MMG. Y4: 2 squads, MMG, 9-2. Y5: 2 squads, HMG. X5: 2 squads, LMG. W4: 2 squads, LMG. X3: 2 squads, LMG |
| Dummy west, new, in place of All round | W4, W5, Y3 (the west face and the north-east tip) | X4: 3 squads, LMG. Y5: 2 squads, HMG, 9-2. Y4: 2 squads, MMG. X5: 2 squads, MMG. X3: 2 squads, LMG. X2: squad, LMG |

Hidden core's placements are untouched; one terrain fact was added to each of the three, the line on the shared footprint. All round is gone from the file: no plan can hold all nine hexes with real units on six stacks.

**East front** (`east-front`), remade

| | |
|---|---|
| The idea | The weight faces the assault engineers and Kampfgruppe Tienham: two squads in each hex of the east face with the HMG, both MMG, and the 9-2, and two more at X5. The west is two squads with an LMG at W4 and two at X3. W5, X2, and the centre hold only Dummies, the centre four of them, so it looks like the reserve. |
| What it gives up | W5, X2, and X4 are not defended: a German squad that enters them takes their Control, and there is no reserve behind the two faces. The 9-2 stands at Y4, so the HMG at Y5 fires without a leader. |

**Dummy west** (`dummy-west`), new

| | |
|---|---|
| The idea | The west face, W4 and W5, and the north-east tip Y3 hold only Dummies, three to a hex. The real force is the centre and the south-east: three squads in the centre, where nothing sees them, the HMG and the 9-2 at Y5, an MMG at Y4, the other MMG at X5, and an LMG at X3 and at X2 behind the Dummy face. |
| What it gives up | W4, W5, and Y3 are not defended: Kampfgruppe Stahler may walk into the west face, and the Russians then hold exactly six hexes and can lose no other. |

What each plan gives away if recognised is listed on its card under "More", as built in step 2: for Dummy west, "which hexes hold only Dummies (W4, W5, Y3); what is under each \"?\"; which unit holds each of its 6 SW". With the shared footprint the Germans cannot recognise it from the stacks.

**Three sides** (`three-sides`), the Germans, for any setup

The German side has this one plan, since no first-side plan can be told from another (section 14).

| | |
|---|---|
| The idea | Every hex of the Works shows the same "?" whichever plan the Russians used, so the Germans press three faces at once and learn by fire which stacks are real. The engineers attack the east face: three squads with the LMG at AA4, and behind them, unseen at BB4, the 10-3 with both flamethrowers and three demolition charges. Kampfgruppe Tienham fires from the upper floor of Z6 and Y7 on X5 and Y5 at range 2, with the HMG under the 10-2. Kampfgruppe Stahler stands under "?" at V2 and U3, two hexes from the west face, with two squads behind at U2, and its nine Dummies fill T4, S5, and T7 so that it looks twice its size. |
| What it gives up | The force is spread over three faces, so no face has the weight to break in alone; the flamethrowers start three hexes from the Works; and the Dummies fool nobody once they are fired on. |
| Terrain facts | AA4 is seen from Y3, Y4, and Y5 at range 2. BB4, one hex behind it, is seen from no hex of the Works, and has a stairwell.<br>Z6 and Y7 see X5 and Y5 at range 2; Y8, behind them, is seen from no hex of the Works.<br>V2 sees W4, X2, and X3 at range 2; U3 sees W4 at range 2 and W5, X2, and X3 at range 3; U2 is seen from no hex of the Works.<br>Of the Dummy hexes, T4 is seen from X2 at range 4, S5 from W5 at range 4, and T7 from X5 at range 4: each is in a Russian LOS, so the Russians see a "?" stack there. |

| Group | Placements |
|---|---|
| Assault Engineer Company A | BB4, ground level: 3 x 8-3-8 squad (two with a flamethrower and a demolition charge each, one with a demolition charge), 10-3 leader; AA4, ground level: 3 x 8-3-8 squad (two with an LMG, one with a demolition charge) |
| Kampfgruppe Stahler, all under "?" | V2, level 1: 2 x 4-6-7 squad with an MMG each, 9-2 leader; U3, ground level: 3 x 4-6-7 squad (two with an LMG), 8-1 leader; U2, ground level: 2 x 4-6-7 squad. Dummies: 3 at T4, 3 at S5, 3 at T7, each on level 1 |
| Kampfgruppe Tienham | Z6, level 1: 2 x 4-6-7 squad (one with the HMG), 10-2 leader; Y7, level 1: 2 x 4-6-7 squad (one with the MMG), 9-1 leader; Y8, ground level: 2 x 4-6-7 squad, 8-0 leader |

The plan uses all 12 of Kampfgruppe Stahler's "?": 3 for its concealed stacks and 9 Dummies. BB4 and Y8 are out of every LOS from the Works, so their stacks should be offered the non-OB "?"; that step comes after the Russian remnants set up (order 3), which the check did not do, so it is not confirmed in the Studio.
