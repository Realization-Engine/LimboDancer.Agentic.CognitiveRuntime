### 8.2 Task 45.1b

**The rule (A7.72, p. 58).** "Encirclement also applies to any non-heroic/non-berserk unit in an upper level building Location that cannot trace a path free of an unbroken, armed, unconcealed enemy unit/Blaze to ground level through (or past, if Scaling; B23.424) Locations it could legally traverse if so inclined. This type of Encirclement is broken the instant such a path can be traced."

**As built (question F7).** Nothing is stored. Rules owns the search (`ScenarioA1UpperLevelEncirclement`): from the unit's Location, breadth first, through the Locations Infantry could step into, never through one the enemy holds, until a ground-level Location is reached or none is left. Play implements the reader: the steps are the movement rules' own Infantry steps (the stairwell up and down, the same level of the next hex of the building), and a Location is held by an active enemy unit that is not broken, unarmed, captured, concealed, or hidden. The planner's `Encircled` is the counter's Encirclement or this one, and every reader in Play takes it: the fire facts of firers and targets (the morale drop and the +1), the MF of a move and of an advance, the rout's reach and its surrender, the Interdiction read, and the page's units table, which says "Encircled: no path to ground level (A7.72)".

In a fire attack the fact is a read of the map, so it is recorded with the attack and the replay takes it from the record, as it does the LOS.

**What a side may know.** The search reads only unconcealed, unhidden enemy units, as the rule does, so the result tells a side nothing it cannot see.

**Left out, each with a backlog row:** a terrain Blaze on the path (terrain Blazes are not built); Scaling (B23.424); the projector does not know this Encirclement, so it ends no counter and writes none, which is the rule ("broken the instant such a path can be traced").

**The games,** on board 3 in the stone building of [M2], whose only stairwell is in [M2]:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-up-cut` | A Russian squad at level 2; German squads at level 1 and at ground level. The units table reads "Encircled: no path to ground level (A7.72)". The squad at level 1 fires up: the record has the target Encircled and its Morale Level 6, and it breaks on a check of 8. | yes |
| `p45-up-open` | The squad at level 1 is broken. The Russian squad is still Encircled: the unbroken squad at ground level holds the foot of the only stairwell. I had written this as the control and expected no Encirclement; the game was right and my control was wrong. | no: read only |
| `p45-up-free` | The control: both German squads broken. No Encirclement. | no: read only |

**The tests.** Rules: `ScenarioA1Pass45RulesTests` gains the search over a small building: a free path, the stairwell held, the way through the next hex, both ways held, and the three exemptions. No Play test: the test boards have no stairwell building, as for 45.1. Classes run: that one; in Play `RallyAndFireStepsTests`, `CloseCombatStepsTests`, `BacklogPass10Tests`, `BacklogPass12Tests`, `BacklogPass14Tests`, `FireTests`, `OlderGamesTests`. No record of the state changed.
