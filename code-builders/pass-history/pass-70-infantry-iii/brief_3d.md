# Increment 3d: rout from a building, Interdiction by vehicles and Guns, Fanaticism by SSR (task 70.2)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.13 to R70.15 (design section 6, F6). Frame A's report, section 6, names the code.

## Pages
- p. 66: A10.5 (a Known enemy "unit/its-SW/Gun"), A10.51 (direction).
- p. 67: A10.53, A10.532 (the Interdictor; its EXC for the building a unit starts in; a Gun's CA change), A10.533.
- p. 69: A10.8 (Fanaticism).
- p. 200: D3.44 (an Armor Leader negating a positive DRM is not enough to let his vehicle Interdict).
- The armor reference or the catalog for a vehicle's weapon ranges (read how pass 50 to 60 give a vehicle's MA and MG range, and use it).

## Build
1. **The starting building (R70.13).** A routing unit may ignore the hexes of the building it starts in when it chooses its destination (A10.532's EXC, p. 67; read its exact words). `RoutTargets` (`ScenarioA1RoutCalculator.cs` about :471) excludes only the starting Location today. Use 3g's building read if it exists by then; if not, read the building's hexes the way the rout code already reads a building, and say how.
2. **Vehicles and Guns (R70.14).** A vehicle and a manned Gun Interdict within their Normal Range (the longest of their weapons, at most 16) when no positive DRM would apply to their attack (D3.44; a Gun that must change CA to fire, A10.532). They also enter must-rout and the RtPh's DM as "unit/its-SW/Gun" (A10.5). `Interdictor` skips vehicles today (`ScenarioA1RoutCalculator.cs` about :199); `NormalRange` gives a vehicle none (`GamePlanner.Rout.cs` about :303-311). Interdiction is decided at plan time and recorded, so no record fact is needed: say so after checking that the projector does not recompute it.
3. **Fanaticism by SSR (R70.15).** A `game-started` special rule `fanatic:<side>` (optionally `fanatic:<side>:<group>` if a group is easy to read; otherwise `<side>` alone and say so), checked by the setup calculator's rule diagnostics, setting `asl:fanatic` on the side's units at setup. Absent-safe. Fanatic units' rules (A10.8) are already built: check they read the condition.
4. **The page.** The rout preview names the Interdictor (a vehicle or Gun by its hex, "[F8]: vehicle G1"); nothing new is offered.

## Tests (by method)
- **Rules:** a unit in a building ignores its own building's hexes when choosing; a vehicle within its MG's range Interdicts; one whose attack would take a positive DRM does not; a Gun that must change CA does not; the SSR's token sets Fanaticism.
- **Play:** a broken squad in open ground next to an enemy AFV must rout and is interdicted by it; an older recorded rout replays as it did.
- **Blast radius:** frame A's section 9, its 70.2 list (the Interdictor and Normal Range methods, the Crest Line and stairwell methods, `BattleHardeningMakesTheCardSquadsFanatic`); run those your change reaches, by method.

## Studio
`games_3d.py`, on board 3: a broken German 4-6-7 in [D10] (open ground), a Russian T-34 in [D9] or a hex that sees it within its MG's range (check the pair on `/boards/bd03`'s LOS tab or the standing rules' board facts), in the German RtPh; and a German squad broken upstairs in the two-hex building [M5]/[L4] that would rout across its own building. Say which pairs you used.
