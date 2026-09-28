# ASL Unit Backlog Pass 11 Design

**Status:** Built. Backlog pass 11 (Vehicle movement and OVR), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out: tasks 11.1 to 11.3.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 11 Review](<Scenario A1 Backlog Pass 11 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 10 Design](<ASL Unit Backlog Pass 10 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1 and 21.

Rulings R11.1 to R11.18 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Vehicle movement (11.1).** A Start MP declares forward or Reverse movement; in Reverse a vehicle enters one of its two rear hexes at four times the cost (three for a truck) and keeps one MP to Stop, since Reverse Motion is not built (D2.2 to D2.24). VBM enters a woods or building hex along a clear hexside for twice the Open Ground cost, straddling it; one VCA change at the CAFP, then on in Bypass or Stop in Stationary Bypass (D2.3 to D2.38). A tracked vehicle attempts ESB once per MPh for up to a quarter more MP, immobilized on a Final DR of 12 or more (D2.5); the red-MP T-34 M41 rolls for Mechanical Reliability on each Start MP (D2.51). A Minimum Move enters one hex costing more than the allotment as the only entry, ending in Motion (D2.15); an ALL entry spends the whole allotment, with the Stop MP beyond it (B13.41, D2.7). Any number of vehicles share a Location, each one there adding to the entry cost (A5.2, D2.14); a vehicle may not Stop or end its move in an enemy AFV's Location it could not kill with an Original TK DR of 5, unless it cannot move on (D2.6).
- **Terrain and Bog (11.2).** The Terrain Chart's vehicle columns for pass 10's terrain: woods by ALL MP or, fully-tracked, half MP with a Bog DR at +3; rubble; walls and hedges by vehicle type, a halftrack's hedge Bog DR taken first; hills at 4 MP a level (2 by road); B10.51 costs by road across an Abrupt Elevation Change and no Double-Crest crossing off road (B10.52); buildings only by VBM (the B23.41 entry is not built). A VCA change costs 1 MP, 2 in woods, buildings, or rubble, with a Bog DR where D8.2 asks. A Bog Check is D8.21's DRM against 11; a bogged vehicle's first expenditure is its Bog Removal, colored dr times white dr MP, freed on a colored dr of 1 to 4, Mired on 5, immobilized on 6 or more (D8.3, D8.31).
- **OVR and CC (11.3).** A moving vehicle declares an OVR with its entry into an enemy-occupied Location, or after the A12.41 choice of the concealed units it entered, for a quarter of its printed MP more; after the DEFENDER's window, the Fire package attacks every Known and concealed Infantry unit there with the OVR FP (D7.1 to D7.17), then the Reaction window opens (D7.2). CC Reaction Fire: one unbroken, unpinned DEFENDER unit, alone or with a SMC, attacks the vehicle in its Location after a PAATC (D7.21, A11.6). In the CCPh, a Location holding a vehicle has sequential CC: the non-vehicular side first, one attack at a time, then the sides alternate or pass (A11.31); Infantry attack a vehicle with their CCV as the Kill Number (A11.5 to A11.612), and a vehicle attacks Infantry with its CMG and CE AAMG on the CCT (A11.62). Infantry advance into an AFV's Location after a PAATC (A11.6). An unarmed vehicle alone with enemy Infantry is captured as the CCPh begins (A11.52); a stopped vehicle holds Known enemy Infantry in Melee (A11.7).

## 2. The packages

Both packages are revised with their prior manifest digests kept.

- **Fire** (`14feea97...`, prior `079c03e6...`; matrix `2d478e06...`). `FireAttack.Overrun` (`FireOverrun`: the vehicle, its definition, its Location, CE, Immobile, and which weapons are malfunctioned) and `FireResolution.OverrunEffect` (`FireOverrunEffect`: the weapons malfunctioned or the vehicle immobilized on an Original 12). The reference's `FireDefinition` adds `BowMg`, `CoaxialMg`, `Caliber`, `GroundPressure`, and `MechanicallyUnreliable`. The calculator's OVR: base 1, 2, or 4 FP; each manned MG tripled and halved; the whole halved when Immobile and against concealed units; FFMO in Open Ground. Rulings `overrun`, `overrunMalfunction`, and `reactionFire`; cases `A1-fire-overrun` and `A1-fire-overrun-outside`; 45 new fragments (293 in all).
- **Close Combat** (`2dde0d2b...`, prior `fb0e0bf7...`; matrix `ee109ae5...`). `ScenarioA1VehicleCloseCombat` resolves an attack on a vehicle and a vehicle's attack on Infantry, with its pre-check. Rulings `againstVehicle`, `byVehicle`, `sequential`, `paatc`, and `capture`; cases `A1-cc-vehicle-attacked`, `-attacks`, `-sequential`, `-paatc`, `-capture`, and `-roll-missing`; 15 new fragments (52 in all).

## 3. The game model and records

- Undrawn conditions: `asl:bogged`, Mired, BMG and CMG malfunctioned, and CC Reaction (cleared at the end of the MPh). `UnitInstance` adds `Straddling` (the hex beside a vehicle in Bypass) and `EsbMp`.
- `vehicle-step` adds the kind `overrun` and `reverse`, `straddling`, `overrunning`, `minimumMove`, `bogRemoval`, and `all`. New records: `vehicle-check-rolled` (ESB, Mechanical Reliability, Bog, Bog Removal), `overrun-resolved`, `paatc-taken`, `vehicle-close-combat-resolved`, and `vehicle-close-combat-passed`. `MovementState` adds `Reverse`, `Overrun`, `Reaction`, and `EnteredFrom`; `CloseCombatLocation` adds `Next` and `Passed`; `GameState` adds `PaatcPassed`, cleared at every phase change.
- The projector checks each vehicle step's kind, MP, and Bypass; ends a bogged or immobilized vehicle's move at its check, but not before a declared OVR resolves; counts a HS Reduced from a CC attacker as having attacked; and holds only Known Infantry in Melee with a vehicle.
- The gate reads back the new records; the Fire and Close Combat verifiers check the OVR and vehicle CC records against the packages.
- Actions: `asl.game.move-vehicle` takes the kinds `esb` and `overrun` and `reverse`, `bypass`, `overrun`, `allMp`, `minimumMove`, and `mp`; new `asl.game.overrun` and `asl.game.vehicle-close-combat`; the A12.41 choice goes through `asl.game.choose`.

## 4. The Play page

- The vehicle panel's state line says Reverse, Bypass, ESB MP, bogged, and Mired. It offers Start in Reverse, Bog Removal, each entry with its VBM lane, cost, ALL, and Bog DR, the OVR, ALL, and Minimum Move boxes (cleared after each step), ESB for tracked vehicles, "OVR here" after the A12.41 choice, and "resolve the OVR" once the DEFENDER passes.
- A CC Reaction Fire panel in the DEFENDER's window on a moving vehicle's MP expenditure in its Location.
- A "Close Combat with vehicles" panel in the CCPh: each open Location, the side to attack next, the eligible attackers, and the attack, the vehicle's attack, or the pass.
- The A12.41 choice reads "reveal them" or "take one combined PAATC"; the fire summary names an OVR's vehicle.

## 5. Tests

- ScenarioA1: `ScenarioA1Pass11Tests` (10).
- Play: `BacklogPass11Tests` (30), on a board 01 grid each test draws terrain on; `BacklogPass6Tests`, `VehicleStepsTests`, and `PlayTests` follow the A12.41 choice, vehicle stacking, and the new actions.
- MapStudio: `TheVehiclePanelOffersReverseAnOvrItsResolutionAndCcReactionFire`.
- Authoring: `ThePass11SubjectsAreVerified` verifies the 59 subjects; the matrix tests pin the new digests and fragment counts.

## 6. Not in this pass

Section 21 of the backlog lists what this pass defers, and section 1 its deviations: a vehicle that cannot move on stopping beside an enemy AFV, a side's pass covering all its units there, and no Ambush in a Location holding a vehicle.
