# ASL Unit Backlog Pass 21 Design

**Status:** Built. Pass 21 (Victory Conditions) of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>), section 2.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 21 Review](<Scenario A1 Backlog Pass 21 Review 2026-09-29.md>), the [ASL Unit Backlog Pass 20 Design](<ASL Unit Backlog Pass 20 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 31.

Rulings R21.1 to R21.5 are in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Control (R21.1).** `ScenarioVictory` keeps the Control of every building the Victory Conditions name (a building setup area of the card, by its id) and of each hex of a building a hex count names. At scenario start a side Controls what lies wholly in its setup areas (A26.11); then, state by state through the game's history, a side gains a hex by an armed Good Order Infantry MMC at its ground level, and a building by one at any level, with no armed enemy ground unit there, never in Bypass; Dummies and prisoners count for nothing. Control stays until the enemy gains it. A game whose card has changed or no longer validates is not judged.
- **VP, CVP, and Exit VP (R21.2).** `GamePlanner.VictoryPoints` gives A26.211's values (Guns and vehicles are backlog); a squad Reduced to a HS gives no CVP until the HS is eliminated (A26.21). CVP are the VP of enemy units eliminated, of those that left the map other than by their own exit condition, and of those held prisoner (double at the end); Exit VP are the VP of units that left by their side's exit condition, none when broken. A side's unbroken squad-equivalents are counted as A16 counts them.
- **Structured Victory Conditions (R21.3).** A card's `victoryConditions` gain `outcomes` (a winner or a draw, any of whose conditions may hold, checked at once when marked immediate) and `otherwise` (Avoidance, A26.3, or a draw). The conditions are `control-margin`, `control-count` (with Melee hexes counting for neither when the card says so), `squad-ratio`, `sole-unbroken`, `exit-vp`, and `cvp`. The three cards are rewritten in the form; validation checks every side, building, edge, and hex they name.
- **The result (R21.4).** At the game's end the outcomes are read in order and `game-ended` records the result: the winner (none for a draw), the reason, and the facts (Control, CVP, Exit VP, squad-equivalents). After every action in play the immediate outcomes are read at the VP of play (only for a card that has one, Gambit), once nothing is left open in the action, and one that holds ends the game at once with `game-ended` (reason `victory`). The Play page shows a Victory Conditions table during play with what the result would be now, and the result once the game has ended.
- **Leaving the map (R21.5).** The move action may name an `exit` edge instead of a Location: a Good Order stack in a ground-level edge hex within the playable area leaves across it in its MPh for its own hex's MF, the whole moving stack together with the SW it carries, never from Bypass and never a crew manning a Gun or a Guard with prisoners; the units are Exited and recorded (`GameState.Exits`), and the Play page offers "Leave by" an edge.

## 2. Event format

| Where | Added | Read when missing |
|---|---|---|
| `movement-step` | `exit`: the edge the movers leave by | A step on the map |
| `game-ended` | `result`: `{ winner, reason, facts[] }`; reason `victory` for an immediate end | No result |

Existing games replay unchanged.

## 3. Tests

`BacklogPass21Tests` (Play): the cards' structured Victory Conditions validate and a condition naming no building is refused; Control from the setup areas, gained by a Russian squad alone in M9, prevented by a broken German, not gained by a broken Russian, and kept after the Russian leaves; The Guards Counterattack played to its end with the Germans winning by Avoidance, recorded with its facts; three times the unbroken squad-equivalents winning; The Tractor Works' X3 (nine Russian hexes, four German, a draw, the Germans alone, a Melee hex for neither); a German squad leaving Gambit's map by the left edge and the British CVP; Gambit's Exit VP winning at once at 20, with adjacency, broken units, and far exits; VP by unit; and, with the real board geometry, an exit from 2J1 counting as adjacent to 2I1 and a `game-ended` victory event replaying with its result. `BacklogPass21TablePlayerTests` (Play): the table player's situations, among them Gambit's British walking across both boards with their SW and leaving off the south edge until the game ends at once at 20 Exit VP, exits by the wrong edge or far from the road giving CVP, Control gained in a move that the unit does not survive, an abandoned SW, Bypass, who gains Control, CVP for casualties and prisoners, The Guards Counterattack's margin, and The Tractor Works' draw with a hex in Melee.
