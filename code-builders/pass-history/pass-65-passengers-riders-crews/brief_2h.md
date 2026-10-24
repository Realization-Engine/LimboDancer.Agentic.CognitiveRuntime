# Increment 2h: the Immobilization TC, Crew Survival, Brew Up (task 65.7)

Read `brief_build.md` beside this file first; it binds you. The ruling is R65.21 of the design (section 6, F20). Increments 2a to 2g are committed before you start:
- 2a gives `LeavingCrew` and the `asl:cs-red` trait (absent on every catalog vehicle);
- 2b gives Passenger Survival.

## Pages
- p. 204: D5.5, D5.6, D5.7.
- p. 208: D8.11.
- p. 209: D8.22, D9.2.
- p. 165: C1.55, a mortar's hit on a vehicle.
- p. 176 and 177: C7.6, C7.7 and its note A; the table's values are the design's and pass 60's.
- The code map's section 3: Crew Survival in ArmorCalculator about :816, AreaCalculator about :521, FireCalculator about :2025; the TC in ArmorCalculator about :797 and FireCalculator about :1989.

## Build
1. **The TC's first cause** (immobilized by a non-CC attack) on the mortar path (C1.55's hull hit) and on the IFT Vehicle line (an unarmored vehicle's Immobilization). Today only an ordnance To Kill or a DC's takes it. Both are backlog rows of pass 60. Reuse the existing TC member and its roll key.
2. **The TC's second cause (D5.5, D8.11, D8.22).** A Rules member recomputes a Direct Fire ordnance shot's To Kill result on an Original DR of 5. It applies only at a target that was already bogged or immobilized, when the shot's own result is none or Immobilized. If the DR-5 result is a Burn, an Eliminated, or an automatic Shock (not a possible one), the crew takes the TC. Today `ScenarioA1OrdnanceEligibility.VehicleTarget` (about :81) gives no TC when Immobilized.
3. **One Crew Survival member.**
   - The +1 (broken, Stunned, Shocked, UK, Recalled, or a recorded +1) and the Abandoned bar (an Abandoned vehicle has no crew to survive) in one Rules member, used by all three paths.
   - `FireVehicle` gains `Shocked` and `Abandoned` (backlog rows of pass 60: the +1 read Stunned, Recalled, or Stun +1 only).
   - The IFT Vehicle-line kill of an armed vehicle gets Crew Survival (today none).
   - Vehicle CC kills, an Unconfirmed Kill's wreck, and a burning wreck keep none (D5.6 EXC: none in CC or burning).
4. **Hazardous Movement for survivors (D5.6).** A surviving crew (and 2b's surviving Passengers) are under Hazardous Movement against all later attacks of that phase: a phase-scoped condition written only by the new events. The killing attack's Collateral Attack and its Residual FP skip them.
5. **The red CS# (D5.7).** Where a vehicle carries `asl:cs-red`, -1 to the Final TK DR for the burn determination only, at each burn test: ordnance, mortar, FT/MOL/DC, and vehicle CC. No catalog vehicle has it, so test with a synthetic definition.
6. **Brew Up (D9.2).** A kill that does not burn rolls Survival instead of attacking the PRC, on every destruction path. With 2b and item 3, check that each path does.

## Gating
- Items 1 to 4 change results recordable today: a TC after a mortar's hit; the second cause; the +1 for Shocked or Abandoned; Survival on the IFT Vehicle line; Hazardous Movement.
- Each applies only where the attack carries the pass's fact. 2b's PRC list is on every fire and ordnance plan since 2b, so it can serve, or a fact of your own. An older recorded attack resolves as it did. Say how you checked for each.
- Item 5 changes nothing until a vehicle carries the trait.

## Tests (by method)
- **Rules:** the second cause (a DR-5 Burn takes the TC, a possible Shock does not); the Survival member's +1 and bar; the red CS#'s -1 on burning only.
- **Play:**
  - a mortar immobilizes an AFV and its crew takes the TC;
  - an IFT attack wrecks an armed unarmored vehicle and its crew survives (the catalog's armed vehicles are all armored, the SPW 251/1 having AF 1, so use a synthetic armed truck or say there is none to test);
  - a Shocked tank killed, with the +1;
  - old-shaped records replaying as before.

## Studio
`games_2h.py`, on board 3: a German PzKpfw IIIH already immobilized, under a Russian 45mm AT Gun's fire (the second cause); a Russian 50mm mortar firing at a German SPW 251/1 (the first cause).
