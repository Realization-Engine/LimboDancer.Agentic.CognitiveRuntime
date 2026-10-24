# Increment 2e: Riders and Bailing Out (task 65.4)

Read `brief_build.md` beside this file first; it binds you. The rulings are R65.16 and R65.17 of the design (section 6, F16). Increments 2a to 2d are committed before you start:
- 2a gives the vehicle class on every vehicle;
- 2b gives Riders as targets (always Vulnerable) and Survival;
- 2c gives Mounted Fire;
- 2d gives FFNAM on (un)loaders.

## Pages
- p. 205: D6.2, D6.21, D6.22, D6.23, D6.24.
- p. 128: B13.43.
- p. 206: D6.81, a Carrier's 8 Rider PP.
- p. 75: A11.611, a Rider's +1 in CC.
- p. 53: A5.5, equivalents.
- The Chapter H Listings Key, p. 338, only if a vehicle's class needs a read.

## Build
1. **Boarding as a Rider (R65.16).**
   - The move-vehicle `load` step takes `riders: true`. The `VehicleStepped` record gains `Riders` (JSON `riders`, absent meaning Passengers), and the projector places the units as `ContainmentRole.Rider`.
   - The projector's Unload takes either role.
   - Rider capacity is a Rules member from the vehicle class: tank, td, spa, spaa, assault-gun 14 PP; carrier 8 PP; any other AFV (halftrack, armored-car, tankette, other armored) one SMC and the two PP he possesses; a truck or other unarmored vehicle none. It is counted apart from Passenger PP.
   - Riders are allowed for the Russians from 1942 and for every nationality from 1943 (D6.2): the game's scenario year and the side's nationality (`state.ScenarioYear`, `state.Side(side).Nationality`). A game with no year refuses Riders and says why.
   - Setup may place a unit as a Rider where setup places Passengers (the Studio's holder role), within the same rules.
2. **D6.21 and B13.43 (R65.17).**
   - One Rules member decides a Bail Out on a vehicle's entry step: woods, orchard, a building (but a friendly Vehicular-Sized Entrance), rubble, a Water Obstacle, or bocage (but a Breach), unless the vehicle is crossing a road hexside, is in Bypass, or (B13.43) is in a stream. A turreted AFV changing its TCA makes them Bail Out.
   - The Riders Bail Out in the last Location occupied before the prohibited terrain, before the entry.
3. **D6.22 and D6.23.**
   - Riders use no SW: a SW fired by a Rider is refused.
   - They fire at half their inherent FP, a quarter if the vehicle moved or is in Motion (OVR included), through 2c's Mounted Fire member, with the Rider's own multiplier. They take +1 to their CC DR.
   - A pin or break result on a Rider makes it Bail Out in the same event set.
4. **Bailing Out (D6.24).**
   - A new event `rider-bailed-out`: the unit, the vehicle, the Location, the cause (terrain, turret, pin, break), the NMC DR, and each SW's dr (1 to 3 kept, 4 to 6 malfunctioned, an X# SW eliminated; every SW eliminated if the carrier breaks or is eliminated).
   - After it, the unit is beneath the vehicle (on foot in its Location) with its movement ended, and a Prep or Final Fire counter if unbroken.
   - FFNAM applies to it as to an unloader (2d), as if it spent all its remaining MF, at least one.
   - A Bail Out into an enemy-occupied Location places a CC counter.
   - The new event goes through the reader, writer, projector, PlayRecords, and ReplaySteps.
5. **The page.**
   - The load controls offer "as Riders" when the vehicle can carry them, with the capacity left.
   - A Rider is named "Rider" where Passengers are named.
   - The record of a Bail Out says why.

## Gating
- Every Rider path was impossible before the pass (no planner path created a Rider): no record changes, except that the projector's Unload now takes a Rider, which no older record holds.

## Tests (by method)
- **Rules:** Rider capacity by class (a tank 14, a Carrier 8, a halftrack one SMC, a truck none); the dates (a Russian in 1942 yes, a German in 1942 no, anyone in 1943 yes); the terrain Bail Out (woods yes, a road hexside no, Bypass no, a stream no); the TCA change.
- **Play:** Riders board a T-34 in 1943 and replay; a Rider bails out on entering woods; a pinned Rider bails out with its SW dr; a German Rider refused in 1942.

## Studio
`games_2e.py`, on board 3, in 1943:
- a Russian T-34 M41 with a 4-4-7 squad and a 7-0 leader beside it, in the Russian MPh. They board as Riders, the tank moves toward woods, and they bail out;
- a German PzKpfw IIIH with Riders under a Russian squad's fire.
