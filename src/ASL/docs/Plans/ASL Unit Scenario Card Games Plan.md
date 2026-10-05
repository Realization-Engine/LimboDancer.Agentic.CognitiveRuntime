# ASL Unit Scenario Card Games Plan

**Status:** Approved by the user on 2026-09-29 as the plan to follow, after ruling the direction the same day: every game is a card.

**Date:** 2026-09-29

**Scope:** card-driven play, the rows of the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 27, that make a scenario card define a game: starting a game from a card, setting up its OB, its turns and reinforcements, evaluating its Victory Conditions, and a card editor that replaces the new-game form. Five passes, 18 to 22, numbered after the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), whose section 1 (how a pass is run, the standing rules, and the merge gate) applies unchanged.

**Related documents:** the [ASL Unit Backlog Pass 17 Design](<../Passes/ASL Unit Backlog Pass 17 Design.md>) (the card format, its reader, and the Studio's Scenario cards page) and the [Scenario A1 Backlog Pass 17 Review](<../Passes/Scenario A1 Backlog Pass 17 Review 2026-09-29.md>).

## 1. Direction and decisions

**Every game is a card (user ruling, 2026-09-29).** A game starts from a scenario card and nowhere else. The built-in cards stay embedded; the cards a user writes are saved under `src/ASL/boards/` next to the saved maps. The new-game form is replaced by a card editor (pass 22). A game with no scenario is a minimal card: boards and two sides, nothing more. The Chapter H DYO purchase, when built, generates a card.

**One path into a game.** A card is turned into the `start` payload of `asl.game.setup` that the planner already validates, so every rule the new-game form checks today (published catalog, verified boards, a composable map, the SSR tokens, Extreme Winter's month and year) keeps a single implementation. `GameStarted` records the card's id and SHA-256 as it records the catalog, so a replay knows its scenario.

**The user's answers (2026-09-29):**

1. **Setup is hot-seat on the honor system.** Both sides see the whole map during setup. Hidden setup per side (a side not seeing the other's placements, HIP, "?" contents) goes to the backlog.
2. **The game evaluates the Victory Conditions** (pass 21), at game end or at once when a card says so.
3. **A card editor replaces the new-game form** (pass 22).
4. **Estimates for the phases** are in section 3.

## 2. The passes

### Pass 18: Start from a card

**Purpose:** a game begins from a card. **After:** pass 17b. **From:** backlog section 27, the first row.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 18.1 Card to start payload | A card picker on the Play page builds the `start` payload: boards and placements, sides, SAN, Friendly Board Edges, the Scenario Defender, month and year, and the SSR tokens. The planner validates it as today. | A2.1, A14.1, A20.53, R17.1 | 0:20 |
| 18.2 The card in the game | `GameStarted` records the card id and hash; the Play page shows the card in a side panel during play; a replay shows which card it was. | R17.1 | 0:15 |
| 18.3 ELR per OB group | Each unit carries its OB group, and its ELR is its group's (A19.1: "each scenario OB will list an ELR for that group"). A card whose groups on one side differ in ELR is no longer shown only. | A19.1 | 0:10 |
| | Overhead | | 1:15 |
| | **Pass 18 total** (build 0:45) | | **2:00** |

Units are still placed by hand in this pass; pass 19 places them from the OB.

### Pass 19: Setup from the OB

**Purpose:** each side sets up exactly its OB, where the card allows, in the card's order. **After:** pass 18. **From:** backlog section 27 (setting up from a card; a sequential setup and "?").

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 19.1 The setup phase and OB pools | A game from a card opens in a setup phase. Each OB group's counters wait in an off-board pool; the groups set up in the card's order (R17.12), one side at a time, and the game begins when every group that sets up on board has placed its counters. | A2.9, A12.12, A3.9 | 0:40 |
| 19.2 Placement checks | A counter is placed only in its group's setup area (building hexes, hex-number rows, hexrows), within the playable area, and never overstacked or where it could not enter in play (A2.9); the checks read the board's Hex Facts, not the card's hex list alone. | A2.9, A5.1, R17.8 | 0:35 |
| 19.3 "?" and Deployment at setup | A group's "?" are placed with it (A12.11, A12.12); up to 10% of its squads may Deploy before setup (A2.9). | A12.11, A12.12, A2.9 | 0:25 |
| 19.4 SSR setup limits | Structured limits the SSRs set (Gambit's five counters, none a Concealment counter, at least two MMC, in rows 5 to 7), checked at setup. | R17.10 | 0:20 |
| 19.5 The Play page setup view | The pools, the current group, its area highlighted on the map, and what is left to place. | | 0:15 |
| | Overhead | | 1:15 |
| | **Pass 19 total** (build 2:15) | | **3:30** |

### Pass 20: Turns, reinforcements, and the start options

**Purpose:** the Turn Record Chart runs the game. **After:** pass 19. **From:** backlog section 27 (reinforcements entering; the playable area; Balance; the first-move die roll).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 20.1 Game end | The game ends after the card's last Game Turn, or after the first Player Turn of a half turn (A3.9). | A3.9 | 0:20 |
| 20.2 The first move and Balance | A die roll decides the first move when the card says so; at start the players may take the Balance, by a dr when both want the same side (A26.4), and the Balance is applied (a Hero, a LMG, foxholes where built, Sewer Movement where built). | A3.9, A26.4 | 0:25 |
| 20.3 Reinforcements | An entry group waits off board and enters in its side's MPh of its Game Turn along its edge or edge hexes, entering counts as its first MF expenditure (A2.6, A4.1). Gambit's British on Turn 1. | A2.6, A4.1, R17.8 | 0:45 |
| 20.4 The playable area | Movement, rout, and setup refuse hexes outside the playable area (Guards Counterattack's hexrows A to P, The Tractor Works' O to GG). | A2.1 | 0:15 |
| | Overhead | | 1:15 |
| | **Pass 20 total** (build 1:45) | | **3:00** |

### Pass 21: Victory Conditions

**Purpose:** the game says who won. **After:** pass 20. **From:** backlog section 27 (evaluating the Victory Conditions; a draw).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 21.1 Control | Control of Locations, hexes, and buildings kept through play: at start from the setup areas (A26.11), gained by a Good Order armed Infantry MMC (A26.13, A26.14), kept until the enemy gains it, a hex in Melee Controlled by neither where a card says so. | A26.1 to A26.16 | 0:50 |
| 21.2 VP and Exit VP | VP values of units (A26.211 to A26.213), CVP for eliminated and captured units (A26.22, A26.222), and Exit VP off an exit area, none for broken Personnel (A26.23). | A26.2 to A26.23 | 0:40 |
| 21.3 Structured Victory Conditions | The card's Victory Conditions gain a structured form beside the text: Control of a building or hex set (with a margin, a hex count, or a comparison of setup buildings), Exit VP through given hexes, CVP, a ratio of unbroken squad-equivalents, an immediate win, Avoidance (A26.3), and a draw. The three cards are rewritten in it; validation checks it. | A26.3, R17.11 | 0:35 |
| 21.4 The result | At game end (or at once for an immediate condition) the game records the result with the facts behind it; the Play page shows it. | A26 | 0:25 |
| | Overhead | | 1:15 |
| | **Pass 21 total** (build 2:30) | | **3:45** |

### Pass 22: The card editor

**Purpose:** the card editor replaces the new-game form. **After:** pass 21, so the editor writes every field play reads. **From:** the user's ruling of 2026-09-29.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 22.1 Editing a card | A Studio page edits every card field: boards on the map composer, sides, groups, setup areas picked on the map, counters from the catalog, SSRs with their tokens, and the Victory Conditions; the card validates as it is edited. | R17.1 to R17.13 | 1:15 |
| 22.2 User cards | Cards saved under `src/ASL/boards/` and listed with the built-in cards; a built-in card copied to edit it; a minimal card for a game with no scenario. | | 0:30 |
| 22.3 The form retired | The Play page's new-game form goes; every game starts from a card; the tests and the demo games move to cards (the user's live games are left as they are). | | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 22 total** (build 2:15) | | **3:30** |

## 3. Duration report

The estimates follow the recent actuals: pass 16 took 1:50 against a plan of 3:45, pass 17 2:25 (with the catalog additions it had not planned), and pass 17b 1:01. Each pass carries 1:15 of overhead: reading and rulings, the two reviews and their fixes, the documents, the full local suite (about 0:25), and the merge gate (about 0:20).

| Pass | Title | Tasks | Build | Total | Range (-30 % to +30 %) |
|---|---|---|---|---|---|
| 18 | Start from a card | 3 | 0:45 | 2:00 | 1:24 to 2:36 |
| 19 | Setup from the OB | 5 | 2:15 | 3:30 | 2:27 to 4:33 |
| 20 | Turns, reinforcements, and the start options | 4 | 1:45 | 3:00 | 2:06 to 3:54 |
| 21 | Victory Conditions | 4 | 2:30 | 3:45 | 2:38 to 4:53 |
| 22 | The card editor | 3 | 2:15 | 3:30 | 2:27 to 4:33 |
| | **All passes** | **19** | **9:30** | **15:45** | **11:02 to 20:29** |

About 15:45 of working time, 2.1 working days of 7.5 hours.

**Order.** The passes run in order, 18 to 22: each needs the one before it (the setup needs a game from a card, reinforcements need the OB pools, the Victory Conditions need the turns, and the editor writes what the others read). Pass 18 alone already replaces typing the start fields by hand.

**Risks.** Pass 19 is the largest unknown: setup-area checks against the Hex Facts for every building and row kind, and the SSR limits, may need more rulings than planned. Pass 21 depends on the game tracking Control through every movement, advance, rout, and Close Combat record; if a record lacks what Control needs, the pass grows. The editor (pass 22) is mostly Studio work, where the estimates have been most reliable.

## 4. Left out

Recorded in the backlog, section 27: hidden setup per side (user ruling, 2026-09-29), the full Chapter H DYO purchase, Battlefield Integrity, and the SSRs the game has no rule for yet (Factory, Fanaticism by building, EC and wind, the encirclement SSR, Infantry SMOKE bans, ANZAC Stealth).

## 5. Rulings

Each pass adds its rulings to the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5, R18.1 onward, subject to the referee's review.
