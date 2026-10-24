# Increment 2i: captured vehicles (task 65.8)

Read `brief_build.md` beside this file first; it binds you. The ruling is R65.22 of the design (section 6, F21). Increments 2a to 2h are committed before you start. 2g gives `ContainmentRole.Crew`, re-crewing, and the Temporary crew flag.

## Pages
- p. 88 and 89: A21.2, A21.21, A21.22 (all of it, with its continuation).
- p. 99: A26.11, A26.12.
- p. 204: D5.343.
- p. 75 and 76: A11.61, A11.52; the existing capture of an unarmed vehicle, ruling R11.16.
- p. 86 and 87: A20.5, only if a Personnel Escort needs it.

## Build
1. **Capture of an Abandoned AFV (A21.2).**
   - At the end of the CCPh, an Abandoned AFV in a Location with Good Order enemy Infantry is captured: automatically when no Personnel of its own side is in the Location, or else by a capture attempt in vehicle CC at -1, with A11.61's CE and Immobile DRM.
   - A new event, `vehicle-captured`, names the captor's side. The vehicle changes side and stays Abandoned until a crew enters.
   - Today's capture of an unarmed vehicle (ruling R11.16, `CapturedVehicles` in VehicleCloseCombat.cs about :217, at the start of the CCPh) stays as it is for older records. Decide whether new captures of an unarmed vehicle also use the new event, and say which.
2. **A Temporary Driver (A21.21).** A captured unarmed vehicle runs for an MMC captor: the captor boards as its Temporary Driver (2g's crew role), and the vehicle may then move. A SMC captor is removed (eliminated? read A21.21 for what "removed" means and say).
3. **Temporary crews (A21.22).** A crew entering a captured AFV by 2g's re-crew is a Temporary crew unless it is that nationality's own vehicle crew. Penalties by who mans it (vehicle crew, Infantry crew, HS, SMC) as A21.22 prints them:
   - halved MP;
   - the start dr (4 or less moves; 5 no move; 6 or more Bogs; +1 for a Russian captor);
   - the captured-weapon penalties that exist (A21.11's B# and ROF for its MG; red To Hit numbers and Case H for its ordnance);
   - red MP as `asl:mechanically-unreliable`.
4. **A SMC crew (D5.343).** It fires the AAMG only while CE, is Wounded on a Stun, and is eliminated on a Recall.
5. **Control (A26.12).** Ruling R24.5 (a captured vehicle neither holds nor prevents Control) is lifted for a vehicle captured by the new event: a manned captured vehicle holds Control for its captor. Find R24.5 in `ScenarioA1VictoryCalculator`.
6. **The page.** The CC panel shows the capture attempt. The vehicle's side and its Temporary crew show in its panel. The record says "[G4]: the PzKpfw IIIH is captured by the Russian side (A21.2)".

## Gating
- A capture of an AFV, Temporary crews, and the SMC crew were impossible before: no record changes.
- Control's change applies only to vehicles captured by the new event. Older captures (R11.16's) keep R24.5's reading.

## Tests (by method)
- **Rules:** the capture's conditions; the start dr; the SMC crew's Stun and Recall; Control.
- **Play:** an Abandoned PzKpfw IIIH captured at the end of the CCPh and re-crewed by a Russian crew with the penalties; an older captured truck's record replaying as before.

## Studio
`games_2i.py`, on board 3: a German PzKpfw IIIH Abandoned at [F8], and a Russian 4-4-7 squad and a Russian infantry crew adjacent, in the Russian MPh. The squad advances in, the CCPh captures it, and the crew re-crews next turn.
