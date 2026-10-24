# Increment 2f: Carriers (task 65.5)

Read `brief_build.md` beside this file first; it binds you. The ruling is R65.18 of the design (section 6, F17). Increments 2a to 2e are committed before you start. The only Carrier in the catalog is `campaign-british-bren-carrier` (class carrier, Passenger capacity 4 since 2a, MA a 2 FP MG, BMG 2). 2a's `GamePlanner.LeavingCrew` has a marked place for the Carrier's HS.

## Pages
- p. 206 and 207: D6.8, D6.81, D6.82, D6.83, D6.84.
- p. 206: D6.631, as D6.83 cites it.
- p. 205: D6.5, the quicker unload.
- p. 203 and 204: D5.31, D5.8.
- p. 100: the Carrier HS's 2 VP; find the line.
- p. 419: British Vehicle Note 64, the Carrier note.

## Build
1. **Never BU, always CE (D6.84).**
   - A vehicle of class carrier is never BU: `asl.game.button-up` refuses it, and its setup is CE.
   - Its crew, HS, and Passengers are always CE and Vulnerable, stunned or shocked included, in every path that reads CE (`LiveFire.CrewExposed` and the fire eligibility's CE facts, 2b's PRC Vulnerability).
2. **The inherent HS (D6.82, D6.83).**
   - Where a crew leaves a Carrier (dismount, Abandonment, Survival), `LeavingCrew` places the nationality's 2-4-7 HS (`british-half-squad`) in place of a vehicle crew, unless the Carrier is armed with a FT or a 76mm mortar or tows a Gun. Then it is a vehicle crew. The catalog's Carrier is none of these.
   - A Carrier's HS may dismount as an unload (D6.83's quicker unload: as D6.5, the inherent HS being the unloader), but then removes no armament and destroys nothing.
   - The re-occupying of a Carrier by its HS with the MG is 2g's re-crewing; leave a note.
   - D6.82's recombination without a TC or leader goes to pass 70: leave it.
3. **Capacity (D6.81).** 4 Passenger PP, or none if the MA is a mortar, a FT, or a 6 FP AAMG; 8 Rider PP (2e's member, class carrier). Check that `ScenarioA1PassengerCalculator.PassengerCapacity` and 2e's Rider capacity give that for the Bren carrier.
4. **2 VP.** An inherent Carrier HS is worth 2 VP while it is inherent (p. 100). Find how `ScenarioA1VictoryCalculator` counts a vehicle's crew (`HasInherentCrew`) and add the Carrier's 2.

## Gating
- A Carrier's crew placement as a HS changes what a new destruction or Abandonment places. Older records keep what they recorded (`instance-created`).
- Never BU refuses an action allowed before. Check whether any older game buttoned up a Bren carrier (grep the store's game files for `campaign-british-bren-carrier` and `button-up`). If one did, gate the refusal on the action's time (a pass fact), else say none did.
- Always CE changes attacks on a Carrier. Apply it where the attack carries 2b's PRC fact (every plan since the pass).

## Tests (by method)
- **Rules:** a Carrier never BU, its HS and Passengers CE when stunned; the capacity; the HS chosen for a Carrier and a vehicle crew for a FT Carrier.
- **Play:** a Bren carrier destroyed with its crew surviving places a British 2-4-7 HS; button-up refused; the victory count.

## Studio
`games_2f.py`, on board 3, in 1944: a British Bren carrier with a British 4-5-8 squad aboard, in the open, under a German 4-6-7 squad's fire in the German PFPh; a German PzKpfw IIIH to kill it.
