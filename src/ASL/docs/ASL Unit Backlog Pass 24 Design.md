# ASL Unit Backlog Pass 24 Design

**Status:** Built. Pass 24 (Control of Locations and more VP) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, the second game pass.

**Date:** 2026-09-30

**Related documents:** the [Scenario A1 Backlog Pass 24 Review](<Scenario A1 Backlog Pass 24 Review 2026-09-30.md>), the [ASL Unit Backlog Pass 21 Design](<ASL Unit Backlog Pass 21 Design.md>) (Control and VP), the [ASL Unit Backlog Pass 23 Design](<ASL Unit Backlog Pass 23 Design.md>) (Control as a side knows it), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 38.

Rulings R24.1 to R24.7 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Location Control (task 24.1; R24.1).** `ScenarioVictory` tracks a `location` item for each level of each hex of every building the Victory Conditions name, with `Level` and `Building`; the levels come from the board through `VictoryReading.Levels` (`GamePlanner.HexLevels`: ground and upper levels, rooftops and cellars left out). A Location starts with its hex's side and is gained by an armed Good Order Infantry MMC in it with no armed enemy ground unit in it; an unentered Location keeps its own Control. Buildings and hexes keep R21.1's rules. `ScenarioVictory.LocationControl` gives one building's Locations with no view, for Mopping Up.
- **Mopping Up (task 24.1; R24.2).** The action `asl.game.mop-up` (`GamePlanner.PlanMopUp`) takes a card building, the units, and an optional guard. It checks the PFPh, the units (each refusal says why), a building of more than one hex or level, no unconcealed unbroken enemy inside (a vehicle in Bypass is not inside), the two-hex range to every ground-level Location the side does not Control, once per Player Turn, and No Quarter. Its events: `hidden-placed` for each hidden enemy, `instance-eliminated` for each enemy Dummy, then `building-mopped-up` (the units become TI; the secured Locations when secured), and either the surrenders (the SW dropped, the prisoner moved to the guard, `instance-captured`) or, with a concealed armed Good Order enemy left, the DEFENDER's Casualty dr with its drm, a Random Selection among several Mopping-Up units, and the Casualty Reduction. `GameState.MoppedUpThisPlayerTurn` (cleared with the Player Turn) and `GameState.Secured` record it; the Control fold applies a newly secured building before the gains of the same state.
- **Gun and vehicle VP (task 24.2; R24.3).** `GamePlanner.VictoryPoints` values a vehicle by A26.212: one, the MA (a Gun MA by its type, or a MG MA from the catalog), the strongest AF in fives rounded up, and the inherent crew (`HasInherentCrew`: a vehicle with a MA or a MG, not Abandoned, with no crew counter of its own). `ScenarioVictory.GunVp` is two. CVP count wrecked vehicles, captured vehicles, eliminated Guns, and Guns the enemy last held (the fold keeps each Gun's last holder side); `VictorySide.GunAndVehicleCvp` gives that part.
- **Start Control (task 24.3; R24.4).** `StartSide` resolves each hex on its own and gives a side when every hex agrees. Both of the backlog's edge cases are unreachable from a valid card.
- **Vehicles and the cache (task 24.4; R24.5, R24.6).** An armed vehicle not in Bypass holds its Location for now (`VictoryControl.ByVehicle`), and its hex when the hex has one Location; the hold is read from the present state only. Armed vehicles prevent Control; one in Bypass does not prevent a building's. `VictoryCache` keeps each game's fold per view, keyed by the last stored event's id and time; `GamePlanner.Victory` passes how many states follow stored events, so the immediate check's plan states are never cached.
- **The standing (task 24.5; R24.7).** K08 `VictoryStandingTable` adds a row for each Location whose Control differs from its building's, several of one building and side as one row with a disclosure, a vehicle's hold named, and the Gun and vehicle part of each side's CVP. The new `MoppingUpAction` component is the phasing side's in its PFPh, inside the Fire panel: a building picker (only buildings that may be Mopped Up), the units with their places, none checked, and the guard.

## 2. Event format

- `building-mopped-up` (new): `{ "building", "side", "units": [...], "secured": ["bd01:F5:0", ...] }`, `secured` left out when the building is not secured.
- `asl.game.mop-up` (new action): `building`, `unitIds`, optional `guard`.
- Existing games replay unchanged; their recorded results keep their facts.

## 3. Tests

`BacklogPass24Tests` (Play, 12) and three `PlayComponentTests` (MapStudio); the review lists them.
