# Increment 4c: the Sniper counter at setup (71.1, part III)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for 4a and 4b, and `frame_state.md` sections 1, 2, and 6 (agent B's map). Read the diffs of the increments already committed.

## Pages
A14.01 and A14.2's first paragraph (p. 82).

## Build (ruling R71.11)
1. **A `sniper` argument of `asl.game.setup`** (side, hex), after both sides have set up, built like the non-OB "?" step (`PlanSetup`'s `conceal` branch, GamePlanner.cs about :463, R23.6; the page's `nonObBlock`, Play.razor about :112). It writes the counter's `instance-created` (kind `asl:sniper`) as today's hand placement does.
2. **The gate:** a card game whose side has a SAN of 2 or more is not started until that side's counter is placed (`CardSetupIncomplete`, GamePlanner.CardSetup.cs about :287), the DEFENDER first (`ScenarioDefender`, else the side that does not move first). The refusal says what to do.
3. **The placement check (A14.2):** an unoccupied hex within six hexes of the most enemy-occupied hexes there can be, at most six, else five, and so on. Rules decides it from counts Play hands over: for each candidate hex the number of enemy-occupied hexes within 6 (Maps distance), counted from the placing side's view (enemy "?" counted; hidden units and the enemy Sniper counter not). The same check applies to a hand placement in a game made without a card. Read the legal hexes once per (state, side) through the memo.
4. **Old games:** a game whose setup is closed is never given a counter. The Play page says once, where the side's SAN is shown or with the Snipers records, that the side has a SAN but no Sniper counter, so it makes no Sniper attacks (A14.01); worded so it names nothing hidden.
5. **What the other side sees** while a placement waits: that the side is placing its Sniper, no hexes. The placed counter is public.
6. **The setup page:** the Sniper block offers the legal hexes (hex UI rule: "[G4]"), DEFENDER first; a page test of the block.

## Tests by method
Rules: the tier (six, then five, ...), unoccupied, the view's count (a hidden unit not counted, a "?" counted). Play: a card game with SAN 3 refused until the counter is placed; the DEFENDER first; an illegal hex refused; a game made without a card placed by hand under the check; a closed game's replay unchanged. MapStudio: the block and the old-game line. Grep the setup tests (`SetupPlansPageTests`, the card setup tests) for the refusals and the counts you change.

## Studio game
A card setup is the case: one of the three scenario cards with a SAN (read which card has one; if none has a SAN of 2 or more, write a small card-backed test game in the live folder's format and say so). Name the steps to place each side's Sniper in the Studio.
