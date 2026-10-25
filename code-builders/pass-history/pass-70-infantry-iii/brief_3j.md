# Increment 3j: Street Fighting, A11.52's Passengers, A18.12 against a vehicle, D5.42 (task 70.7, part II)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.33 and R70.34 (design section 6, F15). Frame C's report, Q2, Q3, and Q5, names the code. 3i is committed: Infantry against Infantry in a vehicle's Location runs in sequence.

## Pages
- p. 74: A11.5, A11.52 (capture of a vehicle in CC, its Passengers included).
- p. 76: A11.8 (Street Fighting), with its example if any.
- p. 85: A18.1, A18.12 (Leader Creation in the CCPh).
- p. 204: D5.42 (entering a vehicle; its EXC in the CCPh at a capture, A21.2).
- p. 88, 89: A21.2 (capture).

## Build
1. **Street Fighting (R70.33).** A Maps read: a road hex ADJACENT to building hexes on both sides of its road hexsides. A state fact the projector sets from the advance: the Location a unit advanced from (state only, not recorded). Infantry advancing from the ground level of those buildings into the vehicle's Location fight it with -1, the vehicle's attack on them +1, not with an actual Ambush (A11.8). When the Location closes, each such unit returns to the Location it came from or stays (the owner's choice, a step), unless broken or pinned. Reaction Fire (D7.211) stays with pass 105; Bypass with pass 100.
2. **A11.52's Passengers (R70.34).** Ordinary Infantry CC at the Passengers of an unarmored stationary vehicle, the attack naming capture: -2 on attacks at them with the capture's +1; +2 on their attacks at the captors. When all are captured and no enemy Personnel remain, the vehicle is captured (`vehicle-captured`).
3. **A18.12 against a vehicle (R70.34).** A MMC's Original 2 in vehicle CC draws a Leader Creation dr (reuse the Infantry calculator's table drm); the created leader adds his leadership to the Original 2, and +1 CCV only if the attack had no SMC (A11.5 limits it to two units); the Unlikely Kill still follows; Play writes the leader's `instance-created`. `leaderCreation` is taken from the record; absent, no roll (an older record with a MMC's Original 2 replays as it did).
4. **D5.42 (R70.34).** A captor may enter the captured vehicle as its crew in the CCPh of its capture (`PlanRecrew` in the CCPh for a vehicle captured in this CCPh by the unit's side, without the same-Player-Turn bar).
5. **The page.** The vehicle CC panel offers the capture of Passengers, the Street Fighting return ("return to [F3]"), and the CCPh entry; hex first.

## Tests (by method)
- **Rules:** the building flank; the -1/+1; the return; the capture DRM and the vehicle captured; Leader Creation in vehicle CC and its +1 CCV rule; D5.42's CCPh entry.
- **Play:** Street Fighting with a return; capture of a truck's Passengers; an Original 2 creating a leader against a tank; a captor entering a captured AFV in the CCPh; an older recorded vehicle CC with an Original 2 replays as it did.
- **Blast radius:** grep the tests for `CapturedVehicle`, `leaderCreation`, `PlanRecrew`, and run those your change reaches, by method.

## Studio
`games_3j.py`, on board 3: a German truck with a German HS aboard, stopped, and a Russian squad advancing into its Location in the Russian APh (the capture of Passengers); a Russian 4-4-7 against a German PzKpfw IIIH in CC (Leader Creation, with the dice for an Original 2); the captured AFV entered in the CCPh. For Street Fighting, a road hex flanked by buildings on both sides: find one on board 3 with the board viewer (`/boards/bd03`) or the standing rules' board facts and say which hexes; if board 3 has none, say so, and that item is shown by its test alone.
