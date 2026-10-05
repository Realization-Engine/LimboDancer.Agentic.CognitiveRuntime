# ASL Unit Backlog Pass 26 Design

**Status:** Built. Pass 26 (Vehicles and Guns on a card) of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 5, the fourth game pass.

**Date:** 2026-10-01

**Related documents:** the [Scenario A1 Backlog Pass 26 Review](<Scenario A1 Backlog Pass 26 Review 2026-10-01.md>), the [ASL Unit Backlog Pass 25 Design](<ASL Unit Backlog Pass 25 Design.md>) (entry and exit), the [ASL Unit Backlog Pass 24 Design](<ASL Unit Backlog Pass 24 Design.md>) (Gun and vehicle VP), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 40.

Rulings R26.1 to R26.8 are in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

A card may now field vehicles and Guns from setup to exit. The user answered the five questions on 2026-10-01: Passengers load and unload; limbering and en portee wait in the backlog (both catalog Guns are QSU); A12.34's HIP is built for Concealment Terrain only; the MPh holds vehicles until they enter, since they cannot advance; and the pass ran as one.

- **Entry (R26.1).** A vehicle waits off board in Motion and enters across its edge as its first MP expenditure, at the hex's own cost, with a VCA holding the hex; its Passengers and towed Gun enter with it. The MPh does not end while one may enter.
- **Passengers (R26.2).** Set up aboard, boarded from the vehicle's Location before it spends MP (D6.4), and let off while it is stopped for a quarter of its MP (D6.5), within its capacity less a towed Gun's ammunition (D6.1, C10.13). A crew boards as it hooks up its Gun and disembarks as the vehicle unhooks it. Passengers are reached through their vehicle: `GameState.At` leaves them out, no action list offers them, and an action naming one is refused until it unloads.
- **Guns at setup (R26.3).** A Gun sets up manned or in tow; its manning crew or HS counts as a squad for stacking (the pass 19 half-squad bug).
- **Exits (R26.4).** A vehicle's exit is recorded for itself, its Passengers, and its towed Gun, so Exit VP and CVP read them (the pass 25 bug); a crew pushes its Gun off the map with a Manhandling DR.
- **Hidden Guns (R26.5).** An Emplaced Gun and its crew set up hidden in Concealment Terrain with no SSR; a firing Emplaced Gun is revealed or placed beneath "?" by the colored dr of its To Hit DR. The manned-Gun loophole in the R23.5 setup check is closed.
- **Covered Arc (R26.6).** Already read in the map's frame on reversed boards; a test pins it and the comments are corrected.
- **The card (R26.7).** Armor Test, manufactured under R0.3.
- **Components (R26.8).** All ten of task 26.5.

## 2. Event format

| Record | Change |
|---|---|
| `vehicle-step` | Kinds `load` and `unload` with `units` (and `unitMf` for an unload); `entry` names the edge of an entry from off board, with the VCA in `facing`; `edge` names an exit's edge. |
| `gun-hooked` | `boards`: the crew boards as the Gun is hooked up. |
| Positions | A Passenger's position is `{ "in": vehicle, "role": "passenger" }`, as the format already allowed; a towed Gun has no position of its own. |

The projector applies an entry from `OffMapPosition`, a load or unload, and an exit that adds `UnitExit` rows for the vehicle, its Passengers, and its towed Gun; `KeepPassengers` eliminates (or exits) Passengers whose vehicle left play. `GameState.Aboard` and `Passengers` read who rides what.

## 3. Planner and page

`GamePlanner.Passengers` holds the entry, load, unload, capacity, the MPh hold (`VehicleEntryDue`), and the aboard refusal. `VehicleCost` is split out of `VehicleOutright` so an entry across the edge reuses the Terrain Chart reads. The setup counters carry `Aboard`, `Manning`, and `Towed`. The A12.34 reveal runs in `AddOrdnanceEvents` before the A12.14 loss of "?". `ScenarioVictory` counts a Gun's exit for its side's Exit VP and its CVP when it meets no condition.

The reviews' fixes are in the [review](<Scenario A1 Backlog Pass 26 Review 2026-10-01.md>): among them Passengers left out of rout, Control, fire lists, and A12.34's spotting, unloading from a vehicle that Prep Fired, is immobilized, or is Abandoned, Recall exits unscored, and the page's drafts reset and filtered.

The Play page keeps the drafts; the components render and raise. The setup form names a Passenger's vehicle or a Gun's crew or tower in its holder field and an off-board counter's entry area; the vehicle panel offers an off-board vehicle's entry by hex and VCA, the Passengers to board or let off, the hex wished to enter next as buttons for its VCA's hexes, and the crew boarding with a hook-up.

## 4. Tests

`BacklogPass26Tests` (10 cases) plays Armor Test on a stand-in board 4: the entry and its MPh hold, unloading and boarding, the truck's entry and unhooking, a vehicle's exit with its Passengers winning the game, the setup stacking and HIP checks, the hidden Gun's reveal by its colored dr (a theory), the push off the map, Passengers leaving an immobilized vehicle with a broken one free of rout, and the Covered Arc on a reversed board. `VehicleComponentTests` (7) covers the ten components, and `PlayPagePass26Tests` (1) the page: a Passenger placed aboard, keyed removal, the movers and cargo per view, and an unload. Three earlier tests were updated: the fourth embedded card (pass 17), the hidden refusal's text (pass 23), and the ordnance test whose concealed Emplaced Gun now follows A12.34.
