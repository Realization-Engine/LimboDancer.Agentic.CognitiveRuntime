# Scenario A1 Backlog Pass 21: Victory Conditions

**Status:** Reviewed. Pass 21 of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>): a game from a card keeps the Control its Victory Conditions name, counts VP, CVP, and Exit VP, lets Infantry leave the map, and records who won. No package changes.

**Date:** 2026-09-29

**Plan:** the Scenario Card Games Plan, section 2 (pass 21), and the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1 and 5 (rulings R21.1 to R21.5). A referee (read-only) reviewed the rules and the code; a table player played the three cards through the planner in a clone.

**Design:** [ASL Unit Backlog Pass 21 Design](<ASL Unit Backlog Pass 21 Design.md>).

## Sources

A.7 (p. 43), A2.6 (p. 47), A7.302, and A26.1 to A26.3 (pp. 98 to 100). A26.1, A26.11, A26.13, A26.14, A26.211, A26.23, and A26.3 are registered fragments of the pass 17 comparison, which the cards cite; the others are cited by the rulings only, and no package or card cites them. No counter was added.

## Referee findings

| Finding | Disposition |
|---|---|
| The rulings against A26 and A.7, and the three cards' outcomes against their text | Confirmed. |
| 1. The immediate check doubled captured units' VP | Fixed: normal VP during play (A26.222). |
| 2. An immediate end could leave an action that does not replay (an open attempt, choice, surrender, or CC) | Fixed: the end waits until nothing is open; ruled (R21.4). |
| 3. Guns are equipment, so their VP never counted | Fixed in the ruling: Gun and vehicle VP are backlog. |
| 4. A squad Reduced and then eliminated gives 1 CVP, not 2 | Kept and ruled (R21.2): A7.302 Reduces, it does not eliminate, and A26.21 pays at the new value. |
| 5. A crew manning a Gun or a Guard with prisoners could leave the map | Fixed: refused (backlog to build it). |
| 6. A game could be judged by a changed or invalid card; an outcome without conditions threw in validation | Fixed: such a game is not judged; validation guards the list. |
| 7. The Play page shows Control to both sides (A26.15) | Recorded (backlog section 31). |
| 8. Start Control edge cases | Recorded; no card is affected. |
| 9. A broken unit's exit through the exit area | Cannot arise: broken units do not leave. |
| 10. Performance of the Control fold | Recorded. |
| Untested paths | Tests added for the real board adjacency and a `game-ended` victory event replaying; the table player covered the end-to-end immediate win, Bypass, Dummies, prisoners, and a gain before DFF. |

## Table player findings

The table player ran 10 situations through the planner: 7 passed and 3 failed on two problems.

| Finding | Disposition |
|---|---|
| 1, 2. A squad carrying a SW could not leave the map (the SW stayed behind, held by a unit off it), so Gambit never ended while the British carried theirs | Fixed: the SW leave with their carriers; the Gambit walk-off now carries them to the immediate win at 20 Exit VP in Game Turn 6. |
| 10. The Tractor Works' facts listed a hex in Melee as Russian beside a draw | Fixed: the facts and the Play page show it as neither's while in Melee. |
| 3 to 9. Exits and CVP, Control gained before DFF, an abandoned SW, Bypass, who gains Control, CVP for casualties and prisoners, the Guards' margin | Passed. The exit refusal's wording is recorded (backlog section 31). |

## Live check

The Studio (`map-studio-scripted`): The Guards Counterattack set up through the Play page (all 52 counters), play started, and the Victory Conditions table showed each side Controlling its five and four setup buildings, 13 and 21 unbroken squad-equivalents, and "if the game ended now, german would win". Played to its end, the page showed the game ending after Game Turn 5 with the Germans winning by Avoidance. No console errors. The Studio was stopped.
