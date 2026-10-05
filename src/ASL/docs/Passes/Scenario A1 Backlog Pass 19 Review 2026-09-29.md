# Scenario A1 Backlog Pass 19: setup from the OB

**Status:** Reviewed. Pass 19 of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>): a game from a card sets up its OB group by group. No package changes.

**Date:** 2026-09-29

**Plan:** the Scenario Card Games Plan, section 2 (pass 19), and the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1 and 5 (rulings R19.1 to R19.6). A referee reviewed the rules and the code; a table player set up the three cards through the planner and the checker.

**Design:** [ASL Unit Backlog Pass 19 Design](<ASL Unit Backlog Pass 19 Design.md>).

## Sources

A2.9 (p. 47), A5.1 and A5.5, A12.11, A12.12, and A12.3 (pp. 76 to 78), A1.31, B16.4, and A14.1, all in the registered fragments of earlier comparisons. No counter was added.

## Referee findings

| Finding | Disposition |
|---|---|
| 1. The 10% Deployment rounded down; A2.9 says FRU | Fixed: two of thirteen; the test encoded the bug and now asserts two committed, three refused. |
| 2. A rally or other RPh action before the first advance started play with the setup unfinished | Fixed: no action but setup until the setup is done; tested with a rally. |
| 3. A counter off the map filled its OB line | Fixed: refused; tested. |
| 4. The Deployment base counted squads that enter | Fixed: the squads set up on board, whole or Deployed. |
| 5. A changed or missing card was not caught at the start of play | Fixed: refused. Not tested: the cards are embedded (recorded). |
| 6. Marsh refused at setup | Fixed: marsh is entered at all MF (B16.4); other unread terrain stays refused (recorded). |
| 7. Only the last SSR-limited area decided completion | Fixed. |
| 8. Equipment with no holder escaped the checks | Fixed: refused, a SW sets up possessed; tested. |
| 9. The "none under ?" test failed on terrain and allotment first | Fixed: in woods, the SSR is the reason. |
| 10. The terrain test never reached the real predicate | Fixed in part: the upper level of F5 sets up through the planner; a marsh test needs a board with marsh (recorded). |
| 11. SMC beyond four over-counted | Fixed: five SMC equal a HS (A5.5); a manned Gun's crew as a squad is recorded. |
| 12. One "?" over two groups' units charged to each | Kept and ruled (R19.5). |
| 13. Both HS of a Deployed squad in one proposal | Kept and ruled (R19.6); the page says so. |
| 14. A Dummy naming an unknown group now refused in any game | Kept and ruled (R19.5). |
| 15. The page showed "-" for Gambit's British | Fixed: what they still owe, and the area's MMC and "?" limits. |

## Table player findings

The table player ran about 45 situations in 23 checks on board 01's real terrain and through the checker for Gambit: 17 passed, 6 failed. No dead end: every group can finish, and play starts in both board 01 cards once the setup is in.

| Finding | Disposition |
|---|---|
| 1. Deployment rounded down | Fixed (referee 1). |
| 2. An unpossessed SW escaped every check | Fixed (referee 8). |
| 3. A finished group could add "?" after later groups began (A12.12) | Fixed: nothing added once a later group has begun; tested. |
| 4. The order message read backwards | Fixed: "may not set up until ... has finished". |
| 5. Gambit's British could not see what they owe | Fixed (referee 15); tested. |
| 6. The Deployment base counted entering squads | Fixed (referee 4). |
| 7. Stacking and pool messages | Fixed: the stacking message gives the count; a counter outside its areas gets only the area reason. |
| 8. One proposal may finish one order and begin the next | Kept (hot-seat, R19.2). |
| 9. The 8-3-8 has no HS in the Fire package | Recorded (backlog section 29). |

## Live check

The Studio (`map-studio-scripted`): The Guards Counterattack chosen; the setup table showed Company H setting up now with every counter it owes and the Russian groups waiting; a Russian squad placed first was refused on order; a German squad at F5 committed, and the table counted it; the start of play was refused, listing what Company H still owes. No console errors. The Studio was stopped.
