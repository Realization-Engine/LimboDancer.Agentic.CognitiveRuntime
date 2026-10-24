# Increment 2g: the crew's own acts (task 65.6)

Read `brief_build.md` beside this file first; it binds you. The ruling is R65.20 of the design (section 6, F19). Increments 2a to 2f are committed before you start. 2a gives `GamePlanner.LeavingCrew`; 2f gives the Carrier's HS.

## Pages
- p. 203: D5.1.
- p. 204: D5.34, D5.341, D5.41, D5.411, D5.42, D5.43, D5.8.
- p. 206: D6.631.
- p. 200: D3.43, the Armor Leader's return.
- p. 88 and 89: A21.22, for a non-crew MMC re-crewing; the penalties themselves are 2i's.

## Build
1. **Voluntary Abandonment (D5.41).**
   - A new action, `asl.game.abandon-vehicle`: in its own MPh, a vehicle that has not moved this phase (`state.MovedVehicles`) and carries no fire counter.
   - It marks the vehicle Abandoned (the existing `AbandonedConditions`) and places the crew by `LeavingCrew`, beneath the vehicle, spending all MF. FFNAM applies to the crew as to an unloader (2d).
   - A new event, `vehicle-abandoned`, carrying the choices below. It goes through the reader, writer, projector, PlayRecords, ReplaySteps, and `GameActions.All`.
2. **Self-destruction (D5.411).** A choice on that action alone: a non-burning wreck with no Crew Survival, or every MG and the MA malfunctioned.
3. **Re-crewing (D5.42).**
   - A new action, `asl.game.recrew-vehicle`. Infantry in the vehicle's Location at the start of its MPh, of the vehicle's side, spends all its MF and enters by a new `ContainmentRole.Crew`.
   - The unit keeps its own id and its +1 (D5.34). It leaves again, as itself, on the next Abandonment.
   - `GameState.At` and `Aboard` and 2a's `ObjectIndex` leave it out of the Location as they do Passengers; add the role to each.
   - Barred in the Player Turn the old crew left, and for a Recalled crew re-crewing its own AFV (D5.341).
   - A friendly MMC other than a vehicle crew or infantry crew re-crews only under A21.22's penalties, which are 2i's. Let it re-crew, and record that it is a Temporary crew (`temporary: true` on the event), so 2i can read it.
   - The vehicle is then manned: clear Abandoned. It may move and fire again from its next phase.
   - Remember: a crew inside is not a Passenger. It does not count against PP and is not attacked as PRC but as the vehicle's crew.
4. **The Armor Leader's return (D3.43).** When the crew that held an Armor Leader leaves, its `NewInstance` carries the vehicle's `ArmorLeader` as dormant data. When that crew re-crews, the Armor Leader returns to the vehicle. Another crew re-crewing brings none.
5. **MG Removal (D6.631).**
   - An option of Voluntary Abandonment for an armored halftrack of the crew's own nationality: the crew takes the MG as the nationality's LMG when its FP is 3 or less (the SPW 251/1's AAMG is 3), as a new SW it holds, and the vehicle is marked Disabled (a new condition, or the existing one if there is one).
   - A failed D5.5 TC forbids it.
   - Re-crewing with that SW returns it and clears Disabled.
6. **D5.8.** A Rules member gives the new crew counter's Prep or Final Fire counter from the vehicle's fire counters, Motion, and moved state and the phasing side. `LeavingCrew` sets it on every path.

## Gating
- Every new action is new: no record changes.
- D5.8's fire counter on a crew placed by a path that existed before (Survival, a failed TC's Abandonment, a Recall) changes what an older record's crew holds. Apply it only where the placing event carries a pass fact. Or check that the projector takes the conditions from the recorded `instance-created` (as it does the definition), so an older record keeps its own. Say which.

## Tests (by method)
- **Rules:** the abandonment's bar (moved, fired); the re-crew's bars; D5.8's counter; MG Removal's FP limit.
- **Play:** a voluntary Abandonment with each self-destruction choice; a re-crew by the crew that left a turn earlier, with its Armor Leader back; a re-crew refused the same Player Turn; MG Removal and its return.

## Studio
`games_2g.py`, on board 3: a German PzKpfw IIIH with an Armor Leader and a German SPW 251/1, both unmoved, in the German MPh. Abandon the halftrack with MG Removal and the tank with self-destruction; re-crew the tank next turn.
