# Increment 3g: Searching, and Mopping Up in any building (task 70.9)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.24 and R70.25 (design section 6, F12) and the memory points of section 7. Frame D's report, section 2, names the code.

## Pages
- p. 79: A12.152 (Searching: who, when, the dr and its drm, TI, what is revealed), A12.153 (Mopping Up), A12.154 (Search Casualties).
- A12.33 (minefields) only to leave them out; A10.72 (leadership is mandatory for a Search Casualties dr).
- The Index's "Accessible" if you can find it (search the page texts for "ACCESSIBLE" in capitals, and the Index pages near the end of the book); if found, use it and say so; if not, R70.24's reading holds (its own hex and the ADJACENT hexes the stack could enter).

## Build
1. **The occupied Locations (R70.25).** An enumerator of the occupied Locations on `ObjectIndex` (built with the index, no second pass). Searching and Mopping Up make one pass over the occupied stacks for a set of hexes, never a lookup per Location.
2. **A building from the map (R70.25).** Move the Studio's `CardMaps.Building` flood fill (`MapStudio/Services/CardMaps.cs` about :55; it splits Rowhouses) into Play, reading `ReadLocation`, the neighbours, and the hexside terrain, memoized per board handle. A building a card names keeps the card's id; any other takes "board:lowest hex". The Studio's caller uses the Play member. Rubble drops out by its terrain; a Location with a Blaze is left out.
3. **Mopping Up anywhere (R70.25).** `PlanMopUp` accepts any multi-hex or multi-level building of the map (A12.153: each Rowhouse hex apart); `BuildingBar`'s card requirement goes; Control folds for any building (`VictoryCardFacts.BuildingHexes` fed with the map's hexes). An older record keeps its card id and replays as it did. The page's `MopUpChoices` (`Play.razor` about :5944-5966) offers only buildings that hold the side's MMC, found by one pass over the side's units mapped hex to building.
4. **Searching (R70.24).**
   - An action `asl.game.search` (unit ids, the hexes in the ATTACKER's order of preference) for members of the moving stack as it ends its move, all Good Order Infantry with at least one MMC, none Pinned or using Assault Movement, with 1 MF left.
   - A new event `units-searched` (units, Location, dr, drm, hexes searched). The projector spends the MF, sets TI (`asl:ti`) on the searchers for the rest of the Player Turn, opens the DEFENDER's window on that MF (A8.1), and ends the move on his pass.
   - The dr's drm: -1 per Stealthy unit, -1 per HS-equivalent beyond one, the best leader's leadership, +1 per Lax or CX unit (each condition). The first (Accessible count less the Final dr) of the named hexes are searched.
   - Concealed enemy units revealed lose "?" (`concealment-lost`); hidden ones are placed beneath "?" (the `hidden-placed` pattern, owner-only visibility, as Mopping Up does at `GamePlanner.MoppingUp.cs` about :121); Dummies are removed.
   - Search Casualties (A12.154): where a Searched Location holds an armed Good Order DEFENDER unit, one Casualty dr per Search dr, rolled at once, Final dr 1 or less Reducing a searcher by Random Selection; reuse `ScenarioA1MoppingUp.CasualtyDrm`, `CasualtyReduces`, `Struck`. Residual FP as a trigger if cheap; minefields, FFE, Booby Traps left out (backlog).
   - It costs a "?" for the searchers under R70.17 (a leadership modifier applied to a Search).
   - What each side learns: the dr, the hexes searched, and every reveal are public; "nothing found" is public.
5. **The page.** A Search control in the Movement section, offered as a move ends (the hexes listed by bracket, "[F5]", in an order the player can set, or a checkbox list in map order with the order of ticking; keep it simple and say what you chose). New button ids join the Icon Lab's coverage file.
6. **Registries:** the action, the event, `PlayRecords.cs`, `ReplaySteps.cs`, `GameGate.cs` if it gates per building.

## Tests (by method)
- **Rules:** the Search dr's drm; the hexes searched for a dr; who may search; Search Casualties' trigger and result; a map building's hexes by flood fill (a Rowhouse split); Mopping Up's bar for a non-card building.
- **Play:** a stack ending its move searches and reveals a concealed and a hidden unit; a Search Casualty Reduces a searcher; Mopping Up a building no card names; an older recorded Mopping Up replays as it did.
- **Blast radius:** frame D's section 5 (the Mopping Up methods that change and the keep-green list); run those your change reaches, by method.

## Studio
`games_3g.py`, on board 3: a German squad and 8-1 leader moving into [M5] (the two-hex wooden building [M5]/[L4] with a level 1), a hidden Russian squad in [L4] and a concealed one in the woods next to it; and a Mopping Up game in a building no card names (the stone buildings [M2], [N1], [N2]). Give the dice and the JSON names to read back.
