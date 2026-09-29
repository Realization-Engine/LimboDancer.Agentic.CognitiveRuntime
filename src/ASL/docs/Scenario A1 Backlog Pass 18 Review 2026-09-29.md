# Scenario A1 Backlog Pass 18: start from a card

**Status:** Reviewed. Pass 18 of the [ASL Unit Scenario Card Games Plan](<ASL Unit Scenario Card Games Plan.md>): a game starts from a scenario card, records it, and gives each unit its OB group's ELR. The Fire package's rules do not change; it gains an optional per-unit ELR fact.

**Date:** 2026-09-29

**Plan:** the Scenario Card Games Plan, section 2 (pass 18), and the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), sections 1 and 5 (rulings R18.1 to R18.3). A referee (a skeptical ASL rules referee who reads the code) reviewed after the build; a table player tried the three cards through the planner and the Play page.

**Design:** [ASL Unit Backlog Pass 18 Design](<ASL Unit Backlog Pass 18 Design.md>).

## Sources

A19.1 and A19.11 to A19.13 (pp. 86 to 87), A20.4, and A3.9 (p. 47), all in the registered fragments of earlier comparisons. No counter was added.

## Referee findings

| Finding | Disposition |
|---|---|
| 1. Units created in play (heroes, created leaders, crews) got no group, so fire at them had no ELR where a side's groups differ | Fixed: they join the creator's group or a group in their Location; tested. |
| 2. The ELR check refused targets ELR never Replaces (A19.11) | Fixed: heroes, crews, and Commissars need no ELR. |
| 3. The firing side's ELR was demanded of every firer | Fixed: only FPF firers and friendly targets. |
| 4. Groups were never validated, and a group without an ELR hid the side's | Fixed: distinct ids, ELRs of 0 to 5, a side ELR only when shared; the group falls back to the side; tested. |
| 5. The first-side switch could show another side's values | Fixed: the fields are looked up by side; only the card's sides are offered; tested. |
| 6. The page read the card's hash at submit | Fixed: kept from when the card was chosen. |
| 7. The tests did not show the group ELR deciding a MC | Fixed: a 1MC failed by 2 Replaces at ELR 1 and only breaks at ELR 4; tests for a created leader, a Dummy, and a contradictory side ELR. Massacre, Deployment, and Recombination are covered by the projector code, not a test (recorded). |
| 8. HS of two groups recombined took the first group | Fixed: the lower ELR's group (ruled in R18.3). |
| 9. A group named on a Dummy is dropped | Kept: a Dummy has no ELR. |
| 10. A group of another side was dropped silently; placements kept a changed card's groups | Fixed: the page says so, and stale groups are dropped. |
| 11. Cards read on every render | Fixed: read once per page. |
| 12. The card link did not reselect on the Scenarios page | Fixed: read on every visit. |
| 13. Underscored MMC and ELR 5 units after a Massacre; Integrity ELR changes | Recorded (backlog section 28). |

## Table player findings

The table player ran 15 checks (9 through the planner, including Gambit on stand-in boards 2 and 4, and 5 on the page): 10 passed, 5 failed, all on the page.

| Finding | Disposition |
|---|---|
| 1. The die-roll winner was preset | Fixed: no winner until one is named; the setup waits. |
| 2. The first-side switch showed wrong values | Fixed (referee 5). |
| 3. A group of another side dropped silently | Fixed (referee 10). |
| 4. A changed card kept placements' groups | Fixed (referee 10). |
| 5. The stale-card check never fired from the page | Fixed (referee 6). |
| 6. Card notes and a doc comment called setup-from-card backlog | Fixed: they name passes 19 and 20. |
| 7. The group check demanded groups of heroes, crews, and Commissars | Fixed. |
| 8. The card panel lacked the setup order and groups | Fixed: it shows who sets up and moves first and each group with its ELR; the Game Turn count and sequential setup are passes 19 and 20. |
| 9. Recombination across groups | Fixed (referee 8). |

## Live check

The Studio (`map-studio-scripted`): the Play page's picker filled and locked the fields for The Guards Counterattack; a German squad in german-1 at F5 and a Guards squad in russian-2 at F3 were placed; the setup committed (3 events); the card panel showed its turns, Victory Conditions, and three SSRs, with the Russians phasing; its link opened the whole card. No console errors. The Studio was stopped.
