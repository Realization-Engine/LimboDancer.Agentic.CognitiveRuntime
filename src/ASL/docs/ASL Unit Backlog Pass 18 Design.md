# ASL Unit Backlog Pass 18 Design

**Status:** Built. Pass 18 (start from a card) of the [ASL Unit Scenario Card Games Plan](<ASL Unit Scenario Card Games Plan.md>), section 2.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 18 Review](<Scenario A1 Backlog Pass 18 Review 2026-09-29.md>), the [ASL Unit Backlog Pass 17 Design](<ASL Unit Backlog Pass 17 Design.md>) (the card format), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 28.

Rulings R18.1 to R18.3 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **A game from a card (R18.1).** The Play page's new game opens with a scenario card picker. Choosing a card fills the start fields (label, boards, sides, ELR, SAN, Friendly Board Edges, month, year, Scenario Defender, SSRs) and locks them; choosing none frees them. When the card leaves the first move to a die roll, the first side offers only the card's sides, starts with no winner, and the setup waits until one is named; the other side follows it. The setup's `start` names the card by id and SHA-256; the planner reads the embedded card, refuses one that is unknown, invalid against the catalog, or changed, and takes the start from the card (`ScenarioCards.Start`), keeping from the request only the label and the die-roll side. The page keeps the card's hash from when it was chosen, reads each card once, and drops a changed card's stale groups from its placements. The path into the game is otherwise the one every start takes.
- **The card in the game (R18.2).** `game-started` records `scenario` (id, SHA-256, title); `GameState.Scenario` carries it. During play the Play page shows the card: turns, place and date, who sets up and moves first, the OB groups with their ELRs, the Victory Conditions, and the SSRs, a link to the whole card (`/units/scenarios?card=<id>`), and a warning when the card has changed since the game started.
- **ELR per OB group (R18.3).** A side carries its OB groups (`<side>-<n>`, name, ELR). A unit names its group at setup (the setup panel offers the groups); a unit made from others keeps their group (two groups recombined take the lower ELR's); a unit created in play joins its creator's group or a group in its Location; `GameState.ElrOf` gives the group's ELR, else the side's. A start's groups must have distinct ids, ELRs of 0 to 5, and a side ELR only when they share it. A side's ELR is the one its groups share, else none, and then each of its units must name a group (`play.group`), but for Dummies, Snipers, heroes, crews, and Commissars (A19.11), which the Fire package also exempts from needing an ELR. Fire and ordnance pass each target's and each FPF firer's ELR to the Fire package (`FireTarget.Elr`, `FireFirer.Elr`), only for units with a group, so games without groups record the same facts as before. A Massacre raises every group's ELR with the side's.

## 2. Event format

| Where | Added | Read when missing |
|---|---|---|
| `game-started` side | `groups`: `[{ id, name, elr }]` | No groups |
| `game-started` | `scenario`: `{ id, sha256, title }` | No card |
| `instance-created` instance | `group` | No group, or the group of the units it comes from |

Existing games replay unchanged.

## 3. Tests

`BacklogPass18Tests` (Play): The Guards Counterattack starts as the card says, whatever the request's other fields (board, SAN, ELR, first side), and records the card; an unknown or changed card is refused; The Tractor Works takes the side that won the die roll and refuses a start naming none; a unit may not name another side's group; where a side's groups differ in ELR a unit must name its group, and fire gives the target its group's ELR with no side ELR; a Replaced unit keeps its group and ELR; a 1MC failed by 2 Replaces a squad of the ELR 1 group and only breaks one of the ELR 4 group; a leader created in play joins the group in its Location; a Dummy needs no group, and a side ELR contradicting its groups is refused; a card's start carries its label, first side, groups, shared ELR, and placed boards. `ScenarioCardStartPageTests` (MapStudio): a card fills and locks the start fields and offers its OB groups, and freeing it unlocks them; The Tractor Works offers only its sides, presets no winner, and the other side follows the one chosen.
