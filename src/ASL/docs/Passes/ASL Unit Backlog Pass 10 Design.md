# ASL Unit Backlog Pass 10 Design

**Status:** Built. Backlog pass 10 (Movement and terrain), as the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 10.1 to 10.9.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 10 Review](<Scenario A1 Backlog Pass 10 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 9 Design](<ASL Unit Backlog Pass 9 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 1 and 20.

Rulings R10.1 to R10.15 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Terrain (10.1).** The Terrain Chart rows for walls, hedges, hills, marsh, and rubble (p. 698) are transcribed. Crossing a wall or hedge costs one more MF, none through a road gap (B9.4); rubble costs three MF with its building's TEM (B24.3, B24.4); marsh costs the whole allotment, only as a unit's first expenditure, and from a lower hex only by Minimum Move at twice the allotment (B16.4); it has no TEM, negates FFMO, and is a same-level Hindrance (B16.2, B16.3). Every Location of board 01 is covered: Open Ground, roads, woods, and buildings at every level. Other terrain is refused (backlog section 20).
- **Levels (10.2).** In a stairwell hex a unit moves up or down one level for one MF (B23.4); at an upper level it moves into the same level of an ADJACENT hex of the same building for two MF, and never out of the building (B23.421, B23.422). One level up a hill doubles the cost; an Abrupt Elevation Change adds two MF per intermediate level up, one down (A4.133, B10.4, B10.51). Infantry fire at another level is resolved with the map read's LOS and Hindrance; PBF doubles the FP at an adjacent target at most one level above (A7.21); Height Advantage gives +1 TEM to a target above any firer with no other positive TEM, and negates FFMO, except across the climbed Crest Line (B10.31). Advances go one hex or one level (A4.7).
- **Hexside terrain (10.3).** A target at its wall's level takes the wall's +2 or hedge's +1 crossed by the firer's LOS into its hex (at a vertex, either hexside or the hexspine) in place of a lower in-hex TEM, reduced for a higher firer (B9.3, B9.31, B9.33, B9.35); it negates FFMO and lowers the Residual FP left. A firer holding Wall Advantage over the shared hexside denies it (B9.321 EX): the game reads WA from which unit entered its Location first, among ground-level units with no positive in-hex TEM (B9.323).
- **Bypass (10.4).** A stack enters a woods or building hex along one or two contiguous hexsides the obstacle does not touch, for the other terrain's cost (A4.3, A4.31); it leaves through the far vertex, or pays to occupy the obstacle, and may not end its move in Bypass (A4.32). Fire at it takes no woods or building TEM and needs an LOS crossing a Bypassed hexside (A4.34).
- **Movement details (10.5).** A pushing crew is under Hazardous Movement (-2, no FFMO or FFNAM, A4.62). The Road Bonus gives one more MF to a unit whose every step was at the road rate (B3.4). A MMC that starts and moves every step with a Good Order leader of its nationality gets two more MF (A4.12), and the leader lends his IPC to it when it is the stack's one laden MMC (A4.42); an advance with a leader counts his two MF and one IPC (A4.72 EX). A Minimum Move enters one hex whatever its cost, then the unit is pinned and CX (A4.134).
- **Concealed movement (10.6).** Concealed units and Dummies move; a concealed mover loses its "?", and a Dummy is removed, when it moves without Assault Movement, or into Open Ground, in the LOS of a Good Order enemy ground unit within 16 hexes (A12.14). A Dummy moves with four MF (A12.11).
- **Entering enemy Locations (10.7).** A move into a Location whose enemy units are all concealed reveals one by Random Selection (hidden units first go beneath a "?"): a real unit forces the stack back, with the MF spent and its move ending; only Dummies are removed and the stack enters (A12.15). A stack of Dummies is removed. Snap Shots are Area Fire at the hexside crossed, with no TEM, FFMO, or FFNAM, and leave no Residual FP (A8.15, A8.223). TPBF triples the FP in the firer's own Location (A7.21), and a unit whose Location holds a Known enemy fires only there (A7.212).
- **One move action (10.8).** `asl.game.move` of one squad, as a stack's first step, into an occupied ground-level building override of board 01 goes to the reviewed entry cases of unit steps 7 to 11, with their records.
- **Berserk routes (10.9).** The shortest route in MF is read over every terrain the game allows; terrain it refuses is on no route. A charge reveals concealed units and stays with them, enters a Location with prisoners, and overruns a lone Known SMC (the CC follows in the CCPh, a deviation).

## 2. The package

The Fire package is revised with its prior manifest digest kept. `FireAttack` adds `TargetLevelAbove` (also per firer), `HexsideTem` (`FireHexsideTem`: `wall` or `hedge` and its value), `HeightAdvantage`, `SnapShot`, and `HazardousMovement`. The reference adds the TEM keys `marsh`, `wooden-rubble`, and `stone-rubble` and `HexsideTem`, and checks the pass 10 transcription (`b-terrain-chart-pass10.transcription.json`, pinned as `terrainChartPass10TranscriptionSha256`). The calculator decides fire across levels, PBF by level (A7.21), TPBF (range 0 in the firer's Location), the wall or hedge TEM, Height Advantage, Snap Shots, and Hazardous Movement, and lowers the Residual FP left by a claimed wall TEM. New fragments come from `asl-scenario-a1.pass10-pdf-comparison.json` (40 subjects); new cases: `A1-fire-walls-and-hedges`, `A1-fire-levels`, `A1-fire-height-advantage`, `A1-fire-snap-shot`, `A1-fire-tpbf`, `A1-fire-hazardous-movement`, and `A1-fire-marsh-rubble`.

## 3. The game model and records

- `movement-step` adds `road` (the step at the road rate, for the Road Bonus), `minimumMove`, `attempted` (the Location of a forced back, the stack staying in `to`), and `bypass` (the hexsides, 0 to 5).
- `MovementState` adds `From` (the Location the step left), `PushedGun`, `MinimumMove`, and `Bypass`; a Minimum Move's or a forced back's movers end their move when the DEFENDER's window closes, and a Minimum Move's unbroken movers become pinned and CX.
- `UnitInstance` adds `OffRoad` and `MovedWith` (the leaders moved with at every step), cleared at every phase change and kept by lineage.
- The fire record verifier takes the new map facts as recorded, as it does range and LOS.
- The planner's `InfantryStep` gives one step's cost for moves, advances, withdrawals, and berserk routes; `MoveEntry` adds Bypass; `HexsideTemAt`, `WallAdvantageHolder`, `Arrivals`, and `HeightAdvantageAt` give the fire facts.
- Actions: `asl.game.move` takes `minimumMove` and `bypass`; `asl.game.fire` takes `snapShot`.

## 4. The Play page

- The movement status says when the stack is in Bypass, along which hexsides, and how it may leave; that a Minimum Move will pin it and make it CX; and which members end their move when the window closes.
- The move panel offers Minimum Move and a Bypass field (the hexsides in order), and explains that the To Location's level moves up or down a stairwell.
- The fire panel offers a Snap Shot in the MPh.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass10Tests` (8).
- Play: `BacklogPass10Tests` (17), on a board 01 grid each test draws terrain on; two older Play tests follow the new rules (the leader's A4.72 bonus, a charge into prisoners), and a Units test the new `From` of the moving stack.
- Authoring: `ThePass10SubjectsAreVerified` verifies the 40 subjects; the matrix tests pin the new digests and fragment counts.

## 6. Not in this pass

Section 20 of the backlog lists what this pass defers, and section 1 its deviations: the road rate always taken, Wall Advantage by arrival order, the leader's IPC, Dummies in a berserk reveal, Bypass in charge routes, the berserk OVR's CC, and a lone revealed SMC's OVR option.
