# Increment 2c: Passenger action (task 65.2)

Read `brief_build.md` beside this file first; it binds you. The rulings are R65.10 to R65.13 of the design (section 6, F10 to F13). Increments 2a and 2b are committed before you start. 2b gives the PRC fact, Passengers as targets, and their morale and DM; build on its members, do not duplicate them.

## Pages
- p. 204, 205, 206: D6.1, D6.31, D6.63, D6.64, D6.65, D6.651, D6.72.
- p. 200: D3.4, the Armor Leader.
- p. 207: D7.11, D7.12, D7.16, the OVR.
- p. 75, 76: A11.611, A11.71.
- p. 192: D.3, Prep Fire freezing a vehicle.
- p. 205: D6.5, firing bars unloading.

## Build
1. **Mounted Fire (R65.10).**
   - Lift `ScenarioA1SequenceCalculator.PassengerBar` / `AboardBar` (`play.aboard`) for fire actions (IFT fire, a SW's fire) by a Passenger. Other actions stay refused until their own increment.
   - LOS and range are from the vehicle's Location.
   - FP: a truck's and other unarmored vehicles' Passengers fire at half, halved again for Bounding Fire, Motion, or Area fire. An armored halftrack's Passengers fire only if CE and at half only when the halftrack moved this phase (D6.63, D6.1).
   - The SW allowed are a LMG, a PIAT, and a DC (D6.72); any other SW fired by a Passenger is refused with the rule.
   - A Passenger that fired may not unload in that phase (D6.5); Prep Fire by a Passenger freezes the vehicle for its MPh as the vehicle's own Prep Fire does (D.3).
   - Fire from a vehicle in Bypass stays refused (pass 100), with the reason.
   - These rules belong in Rules: one calculator member, `MountedFire`, that takes the facts (vehicle armored, OT, CE, moved, Motion, Bypass, fire kind, SW) and returns the multiplier or a refusal.
2. **Passing a SW (D6.31).** A Passenger may pass or take a SW to or from a PRC of the same vehicle or Infantry in the Location, as the SW transfer that exists does (find it: `GamePlanner.SupportWeapons.cs`).
3. **Rout beneath (R65.11).** A broken Passenger of a Stopped vehicle may rout beneath it, spending all its initial RtPh MF (place it on foot in the vehicle's Location). It must do so when the crew is eliminated, broken, or has Abandoned (the vehicle is Abandoned or wrecked). Otherwise it stays aboard as today (`MustRout` exempts it). Put the decision in `ScenarioA1RoutCalculator`.
4. **Leaders aboard (R65.12).**
   - A CE Passenger leader directs his halftrack's Passengers' fire, and the hex's other units only if the halftrack has not moved that phase.
   - An Armor Leader directs Passengers only inside his own halftrack's FG.
   - A Passenger leader's MC, Rally, and LLMC reach are confined to the same vehicle (D6.651).
   - Build D6.64's FG makeup in its own Rules member, called where the FG's units and leader are checked. Do not rework the general FG code.
5. **OVR and CC (R65.13).**
   - In an OVR by a vehicle with Passengers: a CE halftrack's Passengers add half their printed FP and other Passengers a quarter, before the OVR's tripling, with one leadership modifier from the Armor or the Passenger leader (D7.11, D7.16).
   - Results against Passengers after the declaration do not change the OVR's FP.
   - An OVR may attack an enemy vehicle's Vulnerable PRC in the overrun Location (D7.12), through 2b's General Collateral path.
   - In CC: BU Passengers do not attack. Riders are apart (A11.611, +1 to their DR); no Rider exists until 2e, but take the role. Passengers of a Mobile vehicle leave Melee after the first round (A11.71).
   - A11.52's capture of Passengers is pass 75's: leave its refusal or note.
6. **The page.**
   - The fire panel offers a Passenger as a firer, with its hex ("[G4]: 4-6-7 squad A, aboard the SPW 251/1").
   - The proposal line says the Mounted Fire multiplier with its rule.
   - The rout panel offers "rout beneath the vehicle" where R65.11 allows it.

## Gating
- Mounted Fire, SW passing, a Passenger's rout, and their leaders were refused before the pass: no record changes.
- The OVR's added Passenger FP changes an OVR recordable today. Apply it only where the OVR's record carries a pass fact (2b's PRC list on the OVR's fire facts, or a fact of your own), so an older recorded OVR resolves as it did. Say how you checked.

## Tests (by method)
- **Rules:** `MountedFire` for a truck (half, quarter when moving), a CE halftrack that moved and one that did not, a BU halftrack (refused), a SW not allowed; the rout-beneath decision; D6.64's FG makeup; the OVR's Passenger FP.
- **Play:** a Passenger's IFT attack from a truck, replaying clean; a broken Passenger routing beneath; an OVR by a loaded halftrack; an old-shaped OVR record replaying as before.

## Studio
`games_2c.py`, on board 3:
- a German truck with a 4-6-7 squad and a 9-1 leader aboard, Stopped, two hexes from a Russian 4-4-7 squad, in the German PFPh;
- a German SPW 251/1 CE with a squad aboard, set to overrun the Russian squad in the German MPh;
- a broken German squad aboard a Stopped truck in the German RtPh.
