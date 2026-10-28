# Increment 5b: the return to normal (72.1, part II)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its section for 5a, and `frame_charge.md` section 5. 5a is committed before you start: read its diff (`git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP log --oneline -3`, then `git show --stat`).

## Pages
A15.46 (p. 84), A15.432 (p. 84: TPBF halved in the AFPh, "survivors are not yet held in Melee"), A22 on a FT's attack and its target Location (find its page), D-chapter vehicle CC as far as "destroyed" (find it).

## Build (ruling R72.7)
1. **A Rules verdict.** A berserk unit returns to Good Order whenever it, or the group it attacks with, eliminates all (and at least one) Known enemy units in its own Location with TPBF (halved) or a FT during the AFPh, or with CC (built), or with CC that destroys an enemy vehicle there, leaving no other Known enemy unit in the Location. One Rules member beside the existing A15.46 code (`ScenarioA1SequenceCalculator` Berserk* members or the CC calculator's `BerserkEnded`); facts: the phase, whether the fire was in the firer's own Location (TPBF) or by a FT, the berserk members of the attacking group, the Known enemy units in the Location before and after.
2. **Play.** After a fire attack's effects in the AFPh (GamePlanner.Fire.cs, near where the fire's events are written), and after a vehicle is destroyed in CC (GamePlanner.VehicleCloseCombat.cs near the `if (destroyed)` branch, reading the Location with the vehicle wrecked as `NextAfter` does), write `conditions-changed` (Berserk off) for each berserk member, with `BerserkEndsText` or a sibling text citing A15.46. Never derived in the projector from an older record.
3. **A FT in its own Location:** build the FT case only if the Fire package already lets a FT attack its own Location; if not, say so and leave a line for the backlog.

## Not in 5b
The hero's weapons (5e). Unit Substitution (5c, 5d).

## Tests by method
Rules: the verdict for TPBF clearing the Location, TPBF leaving one Known enemy, a FT, a concealed enemy left (not Known: still returns), a vehicle destroyed in CC with Infantry left (stays berserk) and alone (returns), not the AFPh (no return). Play: a berserk squad's halved TPBF in the AFPh eliminates the last enemy and returns to normal; a vehicle destroyed in CC by a berserk group with no one else there returns it. Grep and run by method the existing A15.46 tests (`ScenarioA1CloseCombatTests.ABerserkUnitMustAttackAndReturnsToNormalWhenItClearsTheLocation`, `ScenarioA1RefereeFixesTests.ABerserkGroupReturnsToNormalOnlyWhenItsOwnAttackClearedTheLocation`) and any you touch.

## Studio games
`games_5b.py`, board 3: a berserk Russian squad in a German HS's Location in the Russian AFPh (dice for a kill); a berserk Russian squad with a German truck or tank alone in its Location in the CCPh (dice for the destruction).
