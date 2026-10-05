# ASL Unit Backlog Pass 19 Design

**Status:** Built. Pass 19 (setup from the OB) of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>), section 2.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 19 Review](<Scenario A1 Backlog Pass 19 Review 2026-09-29.md>), the [ASL Unit Backlog Pass 18 Design](<ASL Unit Backlog Pass 18 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 29.

Rulings R19.1 to R19.6 are in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **The setup checker (R19.1 to R19.6).** `ScenarioSetup.Check` (Play) is a pure function of a card and the counters on the map: it matches each counter to a counter line of its OB group (the line's area when the card names one), counts a SW by its holder's group and Location and two HS of one Deployed squad as that squad, and refuses a counter no line is left for, off the map, outside its group's areas, where Infantry could not enter, in an overstacked Location, under "?" beyond its group's OB allotment or outside Concealment Terrain or where an SSR forbids it, hidden, beyond an area's SSR count, a group's Deployed squads beyond 10% (FRU), or out of the card's order (before its earlier groups have finished, or after a later group has begun). It reports each group's order, whether it sets up on board, whether it has finished, what it still owes (counters, or an SSR area's counters and MMC), and its "?" left.
- **The planner (R19.1, R19.2).** `GamePlanner.CardSetup` reads the game for the checker (terrain keys, Infantry entry, the HS of a squad). Every setup proposal of a game from a card is checked; a card changed since the game started is refused. Until every group that sets up on board has finished, no action but setup is accepted, so nothing starts play.
- **The card (R19.4).** A setup area may carry `counters`, `minMmc`, and `concealed`: Gambit's forward area takes five counters, at least two MMC, none under "?".
- **Dummies (R19.5).** A Dummy keeps its OB group, and a group not of its side is refused.
- **The Play page.** The setup shows the card's OB group by group: order, side, status (setting up now, waits, set up, enters later), what is still to set up, "?" left, and where.

## 2. Tests

`BacklogPass19Tests` (Play, board 01's real terrain): The Guards Counterattack sets up group by group (order, areas, the OB pool, off the map, the upper level of a building, the start of play and a rally refused until the setup is done, then play starts); no Location is overstacked; two of the thirteen German squads may Deploy, not three, and both HS of each; only OB "?" in Concealment Terrain, and no HIP; The Tractor Works' 308th sets up first with its eighteen "?"; a finished group adds no "?" after a later one begins; a SW does not set up on its own; Gambit's five British counters (the limit, the MMC, the area, no "?" by SSR, and what the group still owes); and no counter where it could not enter. `BacklogPass18Tests` keep their order. `ScenarioCardStartPageTests` (MapStudio): the setup table before the game starts.
