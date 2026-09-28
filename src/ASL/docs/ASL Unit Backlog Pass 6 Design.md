# ASL Unit Backlog Pass 6 Design

**Status:** Built. Backlog pass 6 (vehicles, part 2), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 6.1 to 6.5.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 6 Review](<Scenario A1 Backlog Pass 6 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 5 Design](<ASL Unit Backlog Pass 5 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1, 14, and 16.

Rulings R6.1 to R6.10 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **AFV and wreck cover (6.1).** Infantry with a non-burning wreck, a friendly AFV, or an abandoned enemy AFV take +1 TEM where their terrain gives none, with no FFMO, except against fire from within the Location (D9.3, D10.3, A4.6; R6.1). An AFV or non-burning wreck in a hex an LOS crosses is a +1 Hindrance when both ends see it, counted with the map's Hindrances as A6.7 requires (D9.4; R6.2). Neither applies to an AFV in Motion, nor, through the AFPh, to an AFV or wreck that entered a new hex or moved under a Motion counter this Player Turn (the Case J clause). The LOS read now reports the hexes it crosses. The refusals of ruling R25.9 are gone.
- **Residual FP against vehicles (6.2).** A vehicle that enters or spends MP in a Residual FP Location is attacked by it: a truck on the Vehicle line, an AFV's Vulnerable crew Collaterally, a BU AFV not at all; the Location's SMOKE applies (A8.2, A8.222; R6.6).
- **Wrecks (6.3).** A destroyed vehicle becomes a wreck in its Location, keeping its VCA; a burning one carries a Blaze (D10.1, B25.14; R6.5). A wreck gives cover and Hindrance as above; a burning wreck gives neither, but its smoke is a +2 Hindrance into, through, within, or out of its Location, one more out of or within it (B25.2, A24.2, A24.8; R6.3). A vehicle pays one more MP per wreck in a hex, two by road, and a burning wreck costs one more MF or MP (D2.14, B25.141; R6.4).
- **Vehicle concealment (6.4).** A vehicle sets up concealed or hidden in grain in season (A12.2, A12.12; R6.7). It loses its "?" when it enters a hex or changes its VCA within 16 hexes and in the LOS of a Good Order enemy ground unit, when it stands outside Concealment Terrain in such a unit's LOS (Case H), when it fires, and when fire gives its crew a check or its Vehicle line a result. A vehicle may enter a Location of enemy units it cannot see: their concealed and hidden Personnel are revealed and their Dummies removed (A12.41; R6.8), a recorded deviation until the PAATC.
- **Vehicle MG fire in the MPh (6.5).** The halftrack's AAMG fires as Defensive First Fire at a moving unit, and as Bounding First Fire during its own MPh, at its outset or once the DEFENDER has passed, at half FP and halved again while Non-Stopped (D3.3, D3.31, D2.42; R6.9). A Bounding Fire counter bars its fire in the AFPh. Its malfunctioned AAMG is repaired in either side's RPh by its CE crew on a dr of 1 and disabled on a 6 (D3.7; R6.10). No catalog vehicle has a BMG or CMG.

## 2. The packages

The Fire and Ordnance packages are revised with their prior manifest digests kept. `FireAttack.AfvCover` names the wreck or AFV the targets may claim; the package adds `afv-cover:<id>` +1 when the terrain gives no positive TEM and the attack is not from within the Location, drops FFMO, keeps it out of the Vehicle line, and reverses it on a Critical Hit. `FireVehicle.Concealed` puts a concealed vehicle on the halved FP's column. Residual FP may attack vehicles, and takes the SMOKE Hindrance the game supplies. The fire kind `bounding-first-fire` admits a vehicle's fire in its own MPh, halved by D3.31, with the counter `bounding-fire`. The Ordnance package adds `case-q:afv-cover:<id>` to the To Hit DR. New fragments come from `asl-scenario-a1.pass6-pdf-comparison.json` (14 subjects); new cases: `A1-fire-afv-cover`, `A1-fire-residual-vehicle`, `A1-fire-concealed-vehicle`, `A1-fire-bounding-first-fire`, and `A1-ordnance-afv-cover`.

## 3. The game model and records

- `InstanceStatus.Wrecked` and the record **`vehicle-wrecked`** (`VehicleWrecked`: the vehicle and whether it burns); a burning wreck's Blaze is an `asl:fire` entity, `<vehicle>-blaze`. Wrecks stay in every perspective's view.
- `GameState.MovedVehicles`: the vehicles that entered a new hex or moved under a Motion counter this Player Turn, cleared at each new Player Turn.
- The record type **`concealment-lost`** (a `ConditionsChanged`) reveals a unit as a consequence of a move.
- Vocabulary 1.10.0 adds the states `asl:bounding-fire` and `asl:disabled`; the Bounding Fire counter leaves at the end of the AFPh.
- A vehicle's repair is a `repair-attempted` record naming the vehicle as both unit and weapon.
- The gate reads back each new record; a move's trailing reveals and Dummy removals do not count as its last event.

## 4. The Play page

- The Fire panel offers the DEFENDER's halftrack for Defensive First Fire in the MPh; the vehicle panel offers Bounding First Fire at a Location its side can see once the DEFENDER has passed.
- The RPh panel offers a vehicle's malfunctioned AAMG for repair.
- The Units table shows a wreck at its Location, and a burning one as burning.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass6Tests` (8) and `ScenarioA1OrdnanceTests.AWreckInTheTargetLocationAddsOneToTheToHitDr`; the vehicle fire test updated for Infantry with an AFV.
- Play: `BacklogPass6Tests` (14), and the vehicle and pass 5 tests updated for wrecks, cover, and entries into concealed Locations.
- Studio: `PlayPageVehicleTests.TheMovingHalftrackOffersBoundingFirstFireOnceTheDefenderPasses`.
- Authoring: `BuildPass6` verifies the 14 subjects; the matrix tests pin the new digests.
- Units: vocabulary 1.10.0. Rendering: goldens for the two new states, in both style sheets.

## 6. Not in this pass

Section 16 of the backlog lists what this pass defers: Armored Assault, entrenched and Dug-In vehicles, Bypass, Spreading Fire, pushing and Scrounging wrecks, the A12.2 road clause, Case H after changes other than movement, a vehicle's Final Fire, and repair by a Hero Rider. The PAATC on a vehicle's entry is a deviation in section 1.
