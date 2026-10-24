# Increment 2b: Passengers and Riders as targets (task 65.1)

Read `brief_build.md` beside this file first; it binds you. The rulings are R65.5 to R65.9 of the design (section 6, F5 to F9). Increment 2a is committed before you start: it gives `IsCrew`, catalog 1.18.0 with the vehicle class, and `ObjectIndex`'s PRC by vehicle (read Passengers and Riders only through it).

## Pages
- p. 192 and 193: D.6, D.8, D.8A, D.8B, the Collateral Attack Table.
- p. 203: D5.31, D5.311.
- p. 205 and 206: D6.61, D6.62, D6.66, D6.7, D6.71.
- p. 207: D6.9.
- p. 209: D9.1.
- p. 55 and 56: A7.211, A7.308, A7.309.
- p. 58: A7.82, A7.821.
- p. 204: D5.6, for the Survival roll.
- pp. 169 to 171 and 176, 177, where pass 60's Collateral and Infantry Target Type paths cite them.

## Build
1. **The fact (R65.5).**
   - A nullable list of each vehicle's PRC on `FireVehicle` (the IFT's vehicle facts) and on the ordnance vehicle target record (find it from `OrdnanceShot` / `OrdnanceVehicleTarget`). Each entry: unit id, role (passenger or rider), morale, broken, and whatever the calculator needs (Vulnerable, CE).
   - Built by Play from the index, written on every new plan (empty when nobody is aboard), and taken from the record at replay. Its absence (null) resolves exactly as before.
2. **Vulnerability (R65.6).**
   - A halftrack's Passengers are CE or BU with the crew (the vehicle's `asl:bu`).
   - Broken, pinned, or shocked Passengers are BU.
   - A BU halftrack's Passengers are attacked only through D6.61's elevation exception or by TPBF (A7.211: a firer in the same Location, or a higher one, against non-BU PRC).
   - A truck's PRC are always Vulnerable, with no CE DRM.
   - Riders are always Vulnerable (no Rider exists until 2e, but the calculator takes the role).
3. **The Collateral Attacks (R65.7).**
   - **General** (D.8, D.8B): the attack's own column and Original DR against every Vulnerable PRC of every vehicle in the target Location, each with TEM or the CE DRM, whichever applies, never both.
   - **Specific** (D.8A, D6.71): after an ordnance hit, or a FT, MOL, or DC at a named vehicle, that does not destroy it. It is pass 60's existing crew attack, extended to the Passengers, Riders, and an unarmored vehicle's crew: the IFT FP or HE Equivalency, with the CE DRM alone.
   - Neither applies when the vehicle is destroyed, after a Dud or a Deliberate Immobilization, or to shocked or stunned PRC.
   - Passengers take Infantry results (KIA, K, MC, NMC, PTC, pin) through `CrewOutcome`'s Collateral path or beside it. A failed MC makes a Passenger broken and BU, never Stunned (D6.62, D9.1). A broken Passenger gets DM when the attack could have caused an NMC (D6.66).
   - **D5.31:** the CE DRM is not cumulative with positive TEM, nor with Height Advantage under Infantry fire (FireCalculator.cs about :2282). This applies only where the attack carries the fact of item 1, which lifts ruling R25.6's refusal of an AFV in positive TEM under other IFT attacks.
   - **D5.311:** an OT AFV's crew under Air Bursts is attacked as unarmored; a crew that breaks routs beneath. Today the AFV is on the Vehicle line with no Air Burst TEM; see `AirBurstUnarmored`. The unarmored Target Facing part goes to pass 105: leave it.
4. **Vehicles beyond the KIA# (R65.8).** A7.308's Random Selection of the vehicles affected replaces the refusal `vehicles-beyond-kia`, for the IFT and for C1.55's mortar, with the selection's dr on the record.
5. **Survival (R65.9).**
   - When a vehicle with PRC is destroyed by anything but CC or burning, each Passenger and Rider makes its own Survival DR against the vehicle's CS# or cs#: +1 if broken, no Shock or Stun DRM.
   - A survivor is placed beneath the wreck, on foot in its Location. A SW not held by a survivor is eliminated.
   - The destroying event carries the Survival results; `KeepPassengers` in the projector must leave a survivor alone when the record says it survived. Where the record carries no Survival, the units are eliminated as before.
   - Reuse the existing Survival roll machinery (roll keys like `crewSurvival`); a new key per unit is fine.
   - The crew's own Survival paths are 2h's: do not unify them here.
6. **The page.**
   - The fire and ordnance previews and the record lines name the PRC attacked and their results ("[G4]: the 4-6-7 squad A aboard the SPW 251/1 takes a General Collateral Attack, +2 for CE (D.8): pinned").
   - A Passenger's state (broken, pinned, DM) shows where a Passenger is shown (`CargoNotes`).
   - A side's view never names an enemy Passenger it cannot see. Check what the page already shows of an enemy vehicle's Passengers and keep to it.

## Gating
- Items 3, 4 (now planned instead of refused), and 5 change the result of attacks and destructions recordable today. Each applies only where the record carries item 1's fact (or, for Survival, the destroying event's Survival).
- Say how you checked that an older recorded attack on a Location with a loaded halftrack, and an older destroyed vehicle with Passengers (find them in BacklogPass26Tests, the test games p26-visual and p40-boarding, or make one shaped as an old record), replay unchanged.

## Tests (by method)
- **Rules:**
  - a General Collateral Attack on a CE halftrack's Passengers, and none on a BU one but by elevation or TPBF;
  - a truck's PRC with no CE DRM;
  - a Specific Collateral Attack on Passengers after a missed TK DR;
  - none after a kill;
  - D5.31's CE DRM not added to positive TEM or Height Advantage, with the fact; as before without it;
  - Survival per Passenger (+1 broken);
  - A7.308's selection.
- **Play:** a game each for an IFT attack on a loaded halftrack, an ordnance kill of a loaded truck with a survivor, and an old-shaped record replaying as before.

## Studio
`games_2b.py`, on board 3:
- a German SPW 251/1 CE with a 4-6-7 squad aboard, in the open;
- a Russian 4-4-7 squad with a LMG, two hexes away, in its PFPh (an IFT attack on the halftrack's Location);
- a German truck with a HS aboard, beside a Russian 45mm AT Gun, for the kill and Survival.
Give the dice that show a pin on a Passenger and a survivor.
