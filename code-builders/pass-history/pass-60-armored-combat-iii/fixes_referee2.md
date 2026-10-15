# Fixes from the second referee (R2: tasks 60.3, 60.5, 60.6, 50.5), as judged by the session

Ruling R60.26 (amends R60.18, decided under the user's word of 2026-10-15, read on p. 182): C11.3 ends a Gun's Emplacement for the scenario when it "starts a scenario hooked up or manned by a non-crew unit", moves, or its manning Infantry forfeits Wall Advantage. A non-crew unit that mans a Gun later does not end it; C11.2 only withholds the +2 "while manned by a squad", which `Emplaced`'s crew test already does.

Ruling R60.27 (decided under the same word): a MG's To Kill attack at a concealed AFV is refused. A9.61 asks for no halved FP, and on the IFT a concealed target would halve it (A7.23); whether Case K replaces the halving is left to the backlog.

Fix:
1. MAJOR. `ScenarioA1ArmorCalculator.Run` (about line 326: `gun.ToHitTable.Count > 0 || machineGun ? [] : reference.Modifications(...)`): a MG skips C4.2's modifications, which apply to "any weapon (including MG)" of 57mm or less beyond 12 hexes (-1 for every 12 hexes beyond 12, and another for 40mm or less). Drop `|| machineGun`; the MG is caliber 0 with no barrel suffix, so only C4.2's rows return. A test at 13 or more hexes (the German HMG's Normal Range is 16).
2. MAJOR. `ScenarioA1ArmorCalculator.Run` (about 413-419): a SW MG at an AFV in its own hex takes Case E, which C5.5 gives "any Gun" and the chart marks "S" (not with a SW). Leave Case E out for a SW MG (keep D3.2's colored-dr facing and C6.3's own-hex exclusion of Case L). A test.
3. A Placed DC reads the AFV's Target Facing at detonation (`PlanDetonateDc` via `WithNamedVehicle`); A23.5 fixes it by the hexside it was Placed through "even if the AFV changes its CA". Record the hull and turret facings in `DcPlacement` when the DC is Placed (absent on older records: read at detonation as now) and read them at detonation.
4. Hidden information in `heldWeapons`: the boundary review's item 1; one fix.
5. R60.26. `GameProjector.KeepNonCrewManning` and `LiveOrdnance.Emplaced`: record only a Gun manned by a non-crew unit at the start of the scenario (its setup), not a later manning; and the predicate goes into Rules (the boundary review's item 3). Its test (`AGunMannedByASquadLosesItsEmplacementForTheGame`) becomes one of a Gun set up manned by a squad, plus one of a Gun a squad mans later, which keeps its Emplacement once a crew mans it again.
6. R60.27. `ScenarioA1MachineGunKill.Outside`: refuse a concealed AFV, with its sentence.
7. Rule decisions outside Rules: `LiveFire.WithRandomDestruction` decides A9.74's scope (no OVR, possessed and manned weapons only, a towed Gun left out per C11.6 note 1): move the predicate into Rules (for example `ScenarioA1GunDestruction.Checks(role, overrun)`). `PlanPlaceDc` and `UnattendedGunAt` are the boundary review's items 8 and 7.

Not fixed now (backlog rows): an OVR's KIA makes no A9.74 dr, where A9.74 gives a plain dr to an OVR by a vehicle that is not fully-tracked and -1 to one that is (the design's 11.1 names only the -1; the row takes the whole case); D3.53 lets a vehicle's MA MG make its To Kill attack in the AFPh unhalved (the AFPh row cites it).
