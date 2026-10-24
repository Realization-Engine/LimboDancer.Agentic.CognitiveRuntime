# Increment 2k: what 2b and 2h left (rulings R65.26 and R65.28)

Read `brief_build.md` beside this file first; it binds you. Increments 2a to 2j are committed before you start. Build these three items, in this order. If time runs short, leave the last and say so.

## 1. A crew as a target (R65.28)
- **Pages:** p. 44 (A1.123); p. 182 (C11, a Gun's crew); ruling R8.3 in `ScenarioA1FireCalculator` (about :675 and :840, `asl.a1.fire.crew-target-unreviewed`, `GunCrewAlone`).
- A crew (`IsCrew`: an infantry or vehicle crew) that mans no Gun is an ordinary Infantry target, alone or with other units, on the IFT and on the ordnance Infantry Target Type. A Gun's crew keeps R8.3's reading (alone with its Gun, as built).
- Only refusals turn into plans, so no recorded game changes.
- **Tests:** a German vehicle crew alone in the open under a Russian squad's fire; a vehicle crew with a squad; a Gun's crew with a squad still refused.

## 2. A7.308's Random Selection (R65.26)
- **Pages:** p. 55 and 56 (A7.308, A7.309); p. 165 (C1.55); ruling R25.3's one-vehicle refusal (`vehicles.Count > 1` in FireCalculator's `Outside`) and `vehicles-beyond-kia`.
- When an IFT attack (or a mortar's C1.55 hit) meets more unarmored vehicles in the Location than the column's highest KIA#, Random Selection picks which vehicles are affected, with its dr on the record.
- Lift R25.3's one-vehicle refusal for the IFT where the attack carries 2b's `prc` fact.
- An older record (no fact) resolves as before, refusals included.
- **Tests:** three trucks under a 2 FP attack (KIA# column: pick by the dr); an older-shaped record refusing as before.

## 3. D5.311: an OT AFV's crew under Air Bursts (R65.26)
- **Pages:** p. 203 (D5.311); p. 128 (B13.3); the existing `AirBurstUnarmored`.
- An OT AFV hit by Air Bursts: its crew is attacked as unarmored, with the Air Burst DRM, on the hit's DR. A crew that breaks routs beneath (it leaves the AFV as a broken vehicle crew, which Abandons the AFV).
- Gate it on the attack's `prc` fact.
- **Tests:** an OT halftrack under a mortar's Air Burst whose crew breaks; an older record as before.

## Studio
`games_2k.py`, on board 3:
- a German vehicle crew in the open beside a German squad, under a Russian 4-4-7's fire in the Russian PFPh;
- three German trucks in one hex under a Russian LMG's fire.
