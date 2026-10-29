# Wiring Prompt 002

**Batch gate:** `580aadf6` (2026-10-29). Written by Claude with `code-builders/documents/wiring_prompt.py` from the [rule inventory](<../../ASL Rule Inventory A to E.md>), sections 6 to 8; the previous prompt was Wiring Prompt 001, at `f525ea78`.

## For Codex

You take track 3, the wiring (the user, 2026-10-29): Play, the Units projector, ASL.Turns, and the Studio's services moved onto the calculators the rule claims built. How to claim, build, gate, and review a wiring claim is in [AGENTS.md](<../../../../../AGENTS.md>) ("Wiring claims (track 3)") and the [Rules-first plan](<../../Plans/ASL Rules-First Four-Track Plan.md>), section 9.1; this prompt says only what is open on the board at this batch gate. Each rule claim's design (linked) has its map of the sites and its rulings.

## Open wiring rows (3)

### Fire

| Wiring | Rule claim | Sites to rewire | Old members to remove | Behavior changes |
|---|---|---|---|---|
| W-D3.7 (new) | [D3.7](<../../Designs/ASL D3.7 Vehicle Breakdown Claim Design.md>) | `ScenarioA1OrdnanceCalculator.Run` (:1123) and `Breakdown` (:1451), `ScenarioA1CanisterCalculator` (:256), `ScenarioA1AreaCalculator` (:216), `ScenarioA1ArmorCalculator` (:553), `ScenarioA1FireCalculator.OverrunEffect` (:2456): ask `ScenarioA1VehicleBreakdownCalculator.Numbers` and `Outcome`, the ordnance result gaining Low Ammo and Disabled, with older recorded games on their recorded reading; `ScenarioA1FireCalculator.VehicleWeaponEffect` and `VehicleWeaponEffects`: the same, for the weapon A9.71 picks; `ScenarioA1FireFollowUps` and the projector: Low Ammo (`GameState.LowAmmoVehicles`) and Disabled placed from fire, Recall by `Recalled` on the fire and the repair paths; `Play/GamePlanner.Rally.cs` (:326, :411 to :452, :497) and `Units/State/GameProjector.cs` (:2280 to :2330): `RepairRefusal`, `RepairResult`, `Recalled`, with every SA's state and the AAMG's own Disabled condition, and the repair of an SA weapon once a catalog vehicle has one; the catalog: a circled B#, a printed vehicle X#, a Vehicle Note's extension; with E10, `ScenarioA1AmmoVehicleCalculator.BreakdownNumber` reads `Numbers` | The AAMG repair path (`VehicleRepairPhaseBar`, `VehicleRepairWeaponBar`, `VehicleRepairCrewBarAsPlanned`, `VehicleRepairChange`), `ScenarioA1RoutRallyProjection.VehicleRepairAllowedAsRecorded`, `VehicleWeaponRepairChanges`'s Recall | A circled B# gives Low Ammo instead of a malfunction; an X# Disables; A19.131 for vehicles; a captured or Extreme Winter lowering of an X#; Recall only when every SA is Disabled too, and on a Disabling fire result; an AAMG crew shocked or stunned may not repair, and a Hero Rider may |
| W-A24.5 | [A24.5](<../../Designs/ASL A24.5 Smoke Strength Claim Design.md>) | `GamePlanner.Wrecks.cs` `SmokeSources` (:143) and `VehicleHindrance` (:129), `GamePlanner.Ordnance.cs:296`, `GamePlanner.Snipers.cs:487`, `GamePlanner.Fire.cs:1706`, `GamePlanner.Overrun.cs:148`: gather each SMOKE counter's source, chemical, and strength, ask `ScenarioA1SmokeStrengthCalculator.Hindrance`, and hand the values to the by-value overloads (`SmokeDrm`, `OwnHexHindrance`, `TargetTem`, `ResidualMapFacts`, `OverrunSmoke`), by Location rather than by hex (A24.2); a Wreck Blaze still through the count members until B25.2 is claimed (ruling RA24.6) | The count overloads `SmokeDrm(int ...)`, `OwnHexHindrance(int ...)`, `TargetTem(string?, int)`, `ResidualMapFacts(..., int)`, `OverrunSmoke(int)` (the Wreck Blaze's once B25.2 is claimed), and `TurnWhiteSmokeRules.InitialStrength` | None in play today (grenades keep +2); SMOKE in two Locations of one hex no longer adds together |

### Setup and work

| Wiring | Rule claim | Sites to rewire | Old members to remove | Behavior changes |
|---|---|---|---|---|
| W-E10 (new) | [E10](<../../Designs/ASL E10 Ammo Vehicles Claim Design.md>) | `GameProjector.WorkEffects.cs`: `PrepareWorkEffects` (:9, :21) asks `SupplyRemovalRefusal` and `Selection`; Replenishment (:56 to :75) asks `ReplenishmentRefusal` (Accessible, `Compatible`, TI, the CE posture for every record); `ValidateSupplyRelocation` (:99, :108, :116) asks `TransferRefusal`; `CompleteWorkEffects` (:153) asks `Restored`; `GameProjector.TurnWork.cs:36`: an Ammo Vehicle may be in two Replenishments at once; `Play/GamePlanner.Passengers.cs:27`, `Play/GamePlanner.TurnWork.cs:51`, `ASL.Turns/TurnWorkProgress.cs:84` and :100: the calculator's members and its durations (`ReplenishmentTurns`, `TransferTurns`); the burning Ammo Vehicle's `Explosion` and `KeepsSupplyingWhenImmobilized` where a vehicle becomes a wreck or Immobilized; the bars `BenefitBarsMovement` and `MovementBarred`; a card's Ammo Vehicle and Ammo Dump SSR; with D3.71 in fire, `ScenarioA1OrdnanceCalculator.Breakdown` asks `BenefitRefusal` and `BreakdownNumber` | `TurnWorkEffects.CanRelocateSupply`, `ReplenishmentReady`, `RelocationSourceReady`, `TransferDestinationReady`, `Resolve`'s restoration, and the `TurnAmmoEligibilityRules` class | Replenishment and transfer across an Accessible Location; two recipients at once; Replenishment refused for an incompatible or non-TI pair; Immobile read by D.7 (a Mired, Abandoned, Shocked, or Stunned vehicle's supply may move); a transfer source with occupants or equipment no longer refused; only a Low Ammo vehicle's depleted ammunition restored; the new movement bars and the explosion |

## Wiring claims in progress

- wire-fire-2 (Codex, claimed): W-C9
- wire-fire-1 (Codex, wired): W-C9.3

## Rule claims waiting for review

The agent that did not build a claim reviews it (AGENTS.md, step 4). Built and not yet reviewed:

- proving-slice (Claude)
- [A24.5](<../../Designs/ASL A24.5 Smoke Strength Claim Design.md>) (Claude)
- [E10](<../../Designs/ASL E10 Ammo Vehicles Claim Design.md>) (Claude)
- [D3.7](<../../Designs/ASL D3.7 Vehicle Breakdown Claim Design.md>) (Claude)

## Claude's note

W-A24.5 is new at this batch too: the A24.5 and E10 claims were built on one branch, and the tool marks one claim per branch. W-E10's benefit wiring (BreakdownNumber in Breakdown) waits for W-D3.7's Low Ammo in fire, so wire D3.7 first. Your Studio (6680) and your working folder stay as AGENTS.md names them.
