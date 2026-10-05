# Scenario A1 Backlog Pass 20: turns, reinforcements, and the start options

**Status:** Reviewed. Pass 20 of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>): a game from a card ends after its last Game Turn, rolls for the first move and the Balance as it starts, sets up its reinforcements off board and enters them in their MPh, and keeps play in its playable area. No package changes.

**Date:** 2026-09-29

**Plan:** the Scenario Card Games Plan, section 2 (pass 20), and the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1 and 5 (rulings R20.1 to R20.7). A referee (read-only) reviewed the rules and the code; a table player played the three cards through the planner in a clone.

**Design:** [ASL Unit Backlog Pass 20 Design](<ASL Unit Backlog Pass 20 Design.md>).

## Sources

A2.1, A2.3, A2.5, A2.51, A2.52, A2.6, A2.9 (pp. 45 to 47), A3.9 (p. 47), A4.1 (p. 48), and A26.4 (p. 100). A2.1, A2.9, A3.9, and A26.4 are registered fragments of earlier comparisons; the others are cited by the rulings only, and no package or card cites them. The first-move dr (R20.2) is manufactured under R0.3. No counter was added.

## Referee findings

| Finding | Disposition |
|---|---|
| 1. A2.5 lets Infantry delay entry to the APh; the game refuses the end of the MPh | Kept and ruled (R20.5): entry by advance is backlog, so the game forbids the delay; recorded. |
| 2, 3, 5. A2.51 timing, the entering 10%, and the half turn | Confirmed. |
| 4. The Balance by agreement was refused with players who wish different sides | Fixed and ruled (R20.3); tested. |
| 6. The first-move dr is rolled before setup, while the card says before play | Kept and ruled (R20.2): both sides set up knowing it. |
| 7. The rout and charge searches ignored the playable area, so a rout could be forced toward cover outside it and fail | Fixed: the searches stay within the area; tested (with the table player's finding 1). |
| 8. Hexrows on one board made every other board unplayable | Fixed: the other boards play whole; tested. |
| 9. A Balance counter could wait off board in a group that never enters | Fixed; tested. |
| 10. Several entry areas for one definition | Recorded (backlog section 30). |
| 11. The MPh's entry-due test and the entry disagreed on which hexes may be entered | Fixed: one shared test (`EntryHexBar`). |
| 12. An off-board vehicle could hold the MPh open | Fixed: vehicles are refused at entry and not held; vehicle entry recorded. |
| 13. Off-board units meeting fire, rally, sniper, concealment, CC, and the projector | Checked; no crash path. |
| 14. Exit across the playable area's edge | Ruled (R20.6): a unit leaves only by a map edge within the area. |
| 15, 16. Untested paths | Tests added for the off-board Dummy and Hero, the agreed Balance, Assault entry, and the multi-board area; the blocked edge and early entry remain untested (no card allows them). |

## Table player findings

The table player ran 19 situations through the planner on board 01's real terrain and Gambit's stand-in boards: 15 passed and 4 failed.

| Finding | Disposition |
|---|---|
| 1. A rout at the playable edge was told to reach cover outside it and died for Failure to Rout | Fixed (referee 7); the two tests now show the rout reaching cover within the area. |
| 2. A Balance counter could be left out and play still started | Fixed and ruled (R20.4): the Balance counters are owed like the OB. |
| 3. A card's OB could set up broken | Fixed and ruled (R20.7). |
| 4 to 15. Gambit's entry (phase, occupied hex, edge, leader MF, Double Time, Assault), ending a waiting stack's move, Deployed HS entering, Gambit played into Turn 2 with rout and surrender, the Balance requests, both Stalingrad cards to their end, the edges, and the Hero's placement | Passed. The messages for another side's group and for Deployment off board, and the paved-road label, are recorded (backlog section 30); the duplicate-name message now says the names must differ. |

## Live check

The Studio (`map-studio-scripted`): The Tractor Works chosen, the Play page showed the die roll instead of a winner and the Balance choices; with both players wishing the Germans, the first setup committed its drs (the Germans moved first, Ann played the Germans, and the Russians took their Balance), and the card panel showed the roll, the Balance, and the players. The Scenarios page showed the enforced playable area. No console errors from the pages. The Studio was stopped.
