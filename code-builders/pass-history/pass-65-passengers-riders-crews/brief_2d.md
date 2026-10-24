# Increment 2d: loading and unloading completed (task 65.3)

Read `brief_build.md` beside this file first; it binds you. The rulings are R65.14 and R65.15 of the design (section 6, F14, F15). Increments 2a to 2c are committed before you start.

## Pages
- p. 205: D6.4, D6.5.
- p. 204: D5.43.
- p. 51: A4.6, FFNAM and FFMO.
- p. 58: A7.821, a pinned unloader.
- p. 48 and 49: A4.11, A4.42, A4.5, the four MF, a leader's IPC, Double Time.
- p. 198: D2.401, time aboard and a Motion attempt.
- The code map's sections 1, 11, and 12 (PlanLoad, PlanUnload, the projector's StepVehicle, the Play page's load/unload controls, PlayRecords, ReplaySteps, GameActions' move-vehicle descriptor).

## Build
1. **FFNAM and FFMO on loading and unloading (R65.14).**
   - The step's (un)loaders are Infantry targets of Defensive First Fire, with FFNAM always, and with FFMO only when unloading from an unarmored conveyance in Open Ground (D6.5, A4.6).
   - Find how the DFF window sees a moving stack and give it the (un)loaders of the step just made, with the right Location (the Dismount Location for unloaders; for loaders, the Location they boarded in, attacked as units on foot).
   - A load fails if the loader ends pinned, broken, or eliminated by that fire (D5.43): a follow-up event puts it back on foot in the Location. A pinned unloader stays in the Dismount Location (A7.821).
2. **Loading completed (R65.15).**
   - A unit may board as the last step of its own move: today `PlanLoad` requires an unmoved unit (Passengers.cs about :199, :215).
   - A leader's IPC counts toward the PP (A4.42).
   - A SW may be left aboard a halftrack or Carrier (it rides with the vehicle, held by no unit; the capacity counts it).
   - After unloading, the four-MF cap holds and Double Time is barred (the pass 40 backlog rows, A4.11, A4.5).
   - Unloading into an enemy-occupied Location places a CC counter (the existing condition used for a Bail Out or an OVR's end).
   - Between moves, only the vehicle and its disembarking PRC may move (D6.5).
   - D2.401: a Passenger's time aboard counts toward a later Motion attempt against it (backlog row of pass 55).
3. **Records and the schema.**
   - A load and an unload get their own record lines in `PlayRecords.cs` and `ReplaySteps.cs`: "[G4]: the 4-6-7 squad A boards the SPW 251/1 (D6.4)", "[G4]: the 4-6-7 squad A unloads from the SPW 251/1 (D6.5)", in place of "X moves to ...".
   - The move-vehicle action's descriptor (GameActions.cs about :549-566) gains the kinds `load`, `unload`, and `delay` and the arguments `units` and `turret` that the planner takes. Check that `additionalProperties: false` is then right.

## Gating
- Boarding at the end of a move, a leader's IPC, a SW left aboard, and unloading into an enemy Location were refused before: no record changes.
- The DFF window on (un)loaders changes what an attack recordable today may target. Apply the new targets only where the attack carries a pass fact for them (the attack's record of which units were (un)loading), so an older recorded DFF resolves as it did.
- The four-MF cap and the Double Time bar after unloading refuse what was allowed: an older game that recorded such a move must still replay. Gate the bar on the unload step carrying a pass fact, or check that no older record holds such a move, and say which.

## Tests (by method)
- **Rules:** the FFMO decision; the load's failure after a pin; the four-MF cap and Double Time after unloading.
- **Play:** an unload under DFF with FFNAM and FFMO; a load that fails on a pin; boarding at the end of a move; the new record lines; the descriptor accepting a load.
- **MapStudio:** whatever test reads the move-vehicle descriptor or the vehicle step's record lines (grep).

## Studio
`games_2d.py`, on board 3:
- a German truck Stopped in the open with a 4-6-7 squad aboard, adjacent to a Russian 4-4-7 squad with a LMG, in the German MPh. The squad unloads and the Russian fires on it;
- a German squad that moves two hexes and boards a halftrack at the end of its move.
