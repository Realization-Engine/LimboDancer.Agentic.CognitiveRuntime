# Increment 3i: Infantry against Infantry in a Location with a vehicle, its Ambush, PRC in CC (task 70.7, part I)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.30 to R70.32 (design section 6, F15). Frame C's report, Q2 to Q4 and Q7, names the code. The standing rules' section 4 has "Close Combat with vehicles is one Location at a time (A11.12)".

## Pages
- p. 72: A11.11, A11.12 (with its EXC), A11.141.
- p. 73: A11.3, A11.31 (vs Vehicle), A11.34, A11.4 (Ambush; its table's vehicle drm).
- p. 74: A11.5, A11.51 (the Ambush -1/+1 in vehicle CC), the H3 and I5 examples.
- p. 75 and 76: A11.6, A11.611 (PRC), A11.62, A11.7, A11.71 (PRC of an Immobile vehicle held in Melee).
- p. 55: A7.211 (TPBF at PRC; not held in Melee until after the CCPh). p. 205: D6.5, D6.24.

## Build
1. **The sequence (R70.30).** In a Location with a vehicle, Infantry against Infantry CC joins the vehicle sequence (`PlanSequentialCloseCombat`, `GamePlanner.VehicleCloseCombat.cs` about :257): a sequential round kind in the CC package, one attack by the side named next, no simultaneous reply. `VehiclePresentBar` (`GamePlanner.CloseCombat.cs` about :425) goes for this case; `SidesWithAttacks` (`ScenarioA1VehicleCloseCombatRules.cs` about :30-47) counts Infantry facing enemy Infantry; `LiveCloseCombat.Units` (about :28) drops vehicles from the Infantry units. Each unit attacks once per CCPh, whatever its target; being attacked by Infantry and by a vehicle are counted apart (A11.12's EXC).
2. **Ambush with a vehicle (R70.30).** `AmbushDueCandidate` (`ScenarioA1CloseCombatRules.cs` about :255) gives an Ambush where a vehicle stands; the Ambush dr takes the vehicle drm (+2 Vehicle, +1 for each BU or Stunned AFV, A11.4's table), recorded per side, null where no vehicle stands; the ambusher resolves all its attacks first (A11.34), then the sides alternate; the -1 and +1 in vehicle CC both ways (A11.51, the I5 example).
3. **PRC in CC (R70.31).** Wire `ScenarioA1PassengerActionRules.CloseCombat` (about :318): a Rider attacks apart in Infantry against Infantry with +1, defends with -1 (A11.611); a CE halftrack's Passengers join the vehicle's attack on Infantry (A11.62); the rest are offered nothing. Confirm by a test whether `GameProjector.InMelee` (about :794-805) marks a vehicle's PRC and crew inside in Melee at the CCPh's end; if so, only the PRC of an Immobile vehicle are held (A11.71), the CCPh-ending record carrying the PRC so held (absent meaning as before).
4. **The CC counter (R70.32).** No stored condition. The unload, Bail Out, and TPBF record lines say the units are in an enemy Location and not held in Melee, citing D6.5, D6.24, or A7.211.
5. **The page.** The vehicle CC panel (`VehicleCloseCombatPanel.razor`, `.vehicle-cc[data-location]`) gains an Infantry against Infantry attack (attackers, defenders, hex first, leaders first, Riders as "[F4]: 4-4-7 squad, Rider on vehicle R1"); its help text loses "CC between Infantry there is not built". `VehicleCcRows` (`Play.razor` about :4005-4016) reads the side's view through the planner's index, not `state.At`.

## Gating
- `vehicle-close-combat-resolved` and `-passed` keep their recorded `Next` and `Closed` (GameProjector.cs about :1084, :1104), so an older game's sequence replays as it did: confirm it.
- The Ambush vehicle drm and the PRC held in Melee are recorded facts, absent meaning as before.

## Tests (by method)
- **Rules:** the sequential round; the Ambush vehicle drm; the -1/+1 both ways; a Rider's +1 attacking and -1 defending; who is held in Melee.
- **Play:** the H3 example's shape (Infantry and a vehicle against Infantry, alternating); an Ambush in a vehicle's Location; an older recorded vehicle CC replays as it did.
- **Blast radius:** frame C's Q8, its "Infantry beside a vehicle" and page methods; each changes as intended; run them by method.

## Studio
`games_3i.py`, on board 3: a Russian 4-4-7 advancing into [F8], which holds a German PzKpfw IIIH and a German 4-6-7 squad, in the CCPh; a second with a German halftrack with Riders. Give the dice.
