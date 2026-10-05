# ASL Unit Backlog Pass 20 Design

**Status:** Built. Pass 20 (turns, reinforcements, and the start options) of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>), section 2.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 20 Review](<Scenario A1 Backlog Pass 20 Review 2026-09-29.md>), the [ASL Unit Backlog Pass 19 Design](<ASL Unit Backlog Pass 19 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 30.

Rulings R20.1 to R20.7 are in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Game end (R20.1).** Advancing the phase out of the CCPh of the last Player Turn of the card's last Game Turn (the first side's, when the card gives that Game Turn a half turn) records `game-ended` with the turn and the reason, not a new phase; `GameState.Ended` carries it, the projector refuses any event after it, and the planner refuses every action with `play.game-over`. `ScenarioCards.EndsAfter` decides it from the card's Turn Record Chart. A game with no card never ends.
- **The first move (R20.2).** A card that leaves the first move to a die roll (The Tractor Works) has it rolled at the first setup: a dr for each side, in the card's order, rerolled while they tie; the higher moves first. A start that names a winner is refused. The drs are `dice-rolled` events with the purpose `first-move`, and they count as setup events (`GameState.IsSetupEvent`).
- **The Balance (R20.3, R20.4).** The start may give the Balance to a side by agreement (`balance.side`), or name two players and the side each wishes to play (`balance.players`): the same side for both is decided by a dr each, rerolled while they tie, and the other side takes its Balance. `game-started` records the Balance side and the players in its `scenario`. A card side's `balanceUnits` join its OB when it has the Balance: a German Hero in any German group (The Guards Counterattack, The Tractor Works) and a German LMG (Gambit). Like the rest of the OB they must be set up before play starts. Sewer Movement and foxholes are shown only.
- **Reinforcements (R20.5).** An OB line that enters is set up off board (`position: { offMap: true }`) during the setup, outside the setup order; `ScenarioSetup.EntryOf` names a line's entry area (its own, or its group's when the group's lines beyond an SSR-limited area enter, as Gambit's British). Each group reports what it still owes off board, play starts only when nothing is owed, and up to 10% (FRU) of the squads entering in each Game Turn may be Deployed. In its side's MPh of its entry turn or later, an off-board stack of one edge enters a ground-level hex of that edge within the playable area (`EntryHexes`) as its first MF expenditure at the hex's cost (`PlanEnter`), by a plain step, Assault Movement, or Double Time; the DEFENDER's window opens as at any step. The MPh does not end while a unit whose turn has come waits off board and a hex of its edge may be entered (`EntryDue` and `PlanEnter` share `EntryHexBar`); vehicles do not enter yet. A Balance counter waits off board only in a group that enters.
- **The playable area (R20.6).** A card's `playableArea` carries `hexrows` (from, to, and optionally the board); when enforced, `ScenarioCards.Playable` refuses setup, Infantry movement, entry, rout, advance, vehicle entry, and withdrawal outside it, and the rout, charge, and withdrawal searches stay within it; the hexrows limit only their own board. The Guards Counterattack plays hexrows A to P and The Tractor Works O to GG.
- **Good Order at setup (R20.7).** A card's counters set up in Good Order; one set up broken is refused.
- **The card read once.** The planner keeps each embedded card it reads, since the route searches ask for the playable area at every step.
- **The Play page.** A card's start shows the die roll instead of a winner to choose, and offers the Balance (none, by agreement, or both players wishing a side, with their names). Setup has an "off board (enters)" choice, and the setup table shows what each group still owes off board. In the MPh, units whose entry turn has come are listed to move, with their edge. The card panel shows the Balance and players, and the end of the game.

## 2. Event format

| Where | Added | Read when missing |
|---|---|---|
| `game-started` `scenario` | `balance`, `players: [{ name, side }]` | No Balance; no players |
| new event | `game-ended`: `{ turn, reason }` (`last-game-turn` or `half-turn`) | The game goes on |
| `dice-rolled` | purposes `first-move` and `balance`, drawn with the start | |

Existing games replay unchanged.

## 3. Tests

`BacklogPass20Tests` (Play): The Guards Counterattack played to its end after the German Player Turn of Game Turn 5, then refused; the half turn; The Tractor Works' first-move drs (a tie rerolled) and the refused named winner; the Balance by a dr (Ann plays the Russians, Ben the Germans with their Hero) and by agreement, and the Hero refused without it; Gambit's German LMG; Gambit's British on open stand-in boards: five set up, the rest off board with one of ten entering squads Deployed and two refused, the German Player Turn, the British MPh refused while they wait, the entry edge, a mixed stack refused, four stacks entering the north edge, and the MPh ending; the playable hexrows; a Russian step out of hexrows A to P refused; an off-board Dummy and an off-board Hero of a group that never enters refused; and players who wish different sides agreeing on the Balance. `BacklogPass20TablePlayerTests` (Play): the table player's 19 situations, among them Gambit played through the British entry into Turn 2, entry by Double Time and Assault Movement and with a leader, The Guards Counterattack and The Tractor Works to their ends, the Balance requests, and a rout at the playable edge seeking cover within it. `BacklogPass18Tests` and `BacklogPass19Tests` roll for The Tractor Works' first move. `ScenarioCardStartPageTests` (MapStudio): the die roll and the Balance choices on the Play page.
