# Increment 3f: Bypass endings and detection (task 70.8)

Read `brief_build.md` beside this file first; it binds you. The ruling is R70.23 (design section 6, F11). Frame D's report, section 1, names the code and the gaps (a) to (d). 3a is committed: reveals that need a seer read go through its memo.

## Pages
- p. 76: A12.1 (a "broken" vehicle).
- p. 77: A12.13 (built in pass 60, R60.9: documents only; do not change it).
- p. 78 and 79: A12.15 (detection; the forced back; Minimum Move), A12.151 (Bypass).
- p. 80: A12.42.
- A4.3 and A4.31 (Bypass) on their pages, for the lane and the TPBF.

## Build
1. **Minimum Move.** An entry into a concealed enemy's Location by Minimum Move is no longer refused (`ScenarioA1MovementCalculator.cs` about :601-604): the normal forced back applies, all MF spent, the MPh ends.
2. **The forced-back lane (gap a).** A stack forced back from a Bypass exit keeps its lane: record the lane on the step (an absent-safe fact; today `Bypass = bypassing` is null when leaving, `GamePlanner.Movement.cs` about :413, and the projector keeps the null, `GameProjector.cs` about :2343), and reveal every concealed unit in the Location, of both sides.
3. **Broken or pinned in Bypass (gap b).** A member broken or pinned while in Bypass, with concealed enemy units in the Location: all "?" there is lost. At the DEFENDER's window's close (`PlanPassFire`, Movement.cs about :620), a Rules member decides it, and `concealment-lost` events are written (`RevealEvents`, `GamePlanner.VehicleConcealment.cs` about :76).
4. **Occupying the obstacle (gap c).** Attempting to occupy an obstacle that holds concealed units reveals them and forces the stack back into its lane, MF spent (today refused, `ScenarioA1TerrainCosts.cs` about :234-237; the test `BacklogPass10Tests.BypassRefusesWallsFriendsSplitsAndEnemyObstacles` asserts the refusal and changes as intended).
5. **TPBF inside the Bypassed hex (gap d).** A firer in the same Location may fire TPBF at a unit in Bypass there (refused at `ScenarioA1FireMapRules.cs` about :73-76). Read A4.3/A4.31 and the TPBF path first; if the CCPh or the TPBF arithmetic needs more than a lifted refusal, stop there and say so (it goes to the backlog).
6. **A vehicle's Bypass ending (A12.42).** In `PlanEndVehicle` (`GamePlanner.Vehicles.cs` about :951-980, both exits), a vehicle in Bypass (`Straddling`) ending its MPh in a Location with concealed enemy units reveals them unless it is "broken" (no inherent crew and no PRC, A12.1). Hidden units stay hidden.

## Tests (by method)
- **Rules:** each of items 1 to 6 decided in Rules.
- **Play:** a Minimum Move forced back; a stack forced back from a Bypass exit stays in its lane with the TEM of the lane, not the obstacle's; a pinned Bypass member reveals the "?" in the Location; a vehicle ending in Bypass reveals concealed Infantry there; an older recorded forced back replays as it did.
- **Blast radius:** frame D's section 5 (the keep-green list and the methods that change); run those your change reaches, by method.

## Studio
`games_3f.py`, on board 3: the woods of [F5] Bypassed from [E6] beside [F6] (the standing rules' board facts), a concealed Russian squad in [F5], a German stack moving in Bypass; and a German halftrack in Bypass of [F5] ending its MPh there. Say which steps and dice show each item.
