# Pass 60 code map

Made 2026-10-15 by two read-only agents on `feature/asl-backlog-pass-60` (cut from 380b4a7b). Paths under `src/ASL/`: **Rules/** is `LimboDancer.Domains.Asl.Rules/`, **Play/** `LimboDancer.Domains.Asl.Play/`, **Units/** `LimboDancer.Domains.Asl.Units/State/`, **Studio/** `LimboDancer.Domains.Asl.MapStudio/`. Lines drift: verify before relying on one. The pass 55 map beside it (`../pass-55-armored-combat-ii/codemap.md`) is older but wider.

## The ordnance shot

- Action `GameActions.FireOrdnance` (Play/GameActions.cs:481): `gunId`, `target`, `targetVehicle`, `ammunition` (ap, apcr, apds, heat, he, canister), `aim`, `aimFacing`, `intensive`, `spotter`, `director`. Dispatched at GamePlanner.cs:313.
- Planner: `PlanFireOrdnance` (GamePlanner.Ordnance.cs:25; Canister branch :35, Gun Duel :51) → `OrdnanceShotFacts` :75 → `LiveOrdnance.FromState` (LiveOrdnance.cs:75; ammunition defaulted :248) → refusals :101-125 → `OrdnanceMapFacts` :212 or `SupportWeaponMapFacts` :382 (Area, PF, LATW) → weather :141 → `PassEightFacts` :506 → `ScenarioA1OrdnanceCalculator.Precheck` :144 → `AddOrdnanceEvents` :688.
- Rules: `ScenarioA1OrdnanceCalculator.Resolve` :17 (PF Check :40-62, Canister :65, then Area → `ScenarioA1AreaCalculator.Run`, VehicleTarget → `ScenarioA1ArmorCalculator.Run`, else `Run` :740-1068); `Outside` :310; `Undecided` :656; `HitAttack` :707. Map rules: Rules/ScenarioA1OrdnanceMapRules.cs (`TargetFacing` :34, `VehicleTargetFacings` :40, `HeightAdvantage` :103, `Gunshield` :187).
- Events: `AddOrdnanceEvents` draws one roll per `roll-missing:<key>` (:710-769; purposes ordnance-to-hit, -to-kill, -shock-check, -crew-check, -crew-survival, -canister, -random-selection, nested `hit:`/`critical-hit:` IFT keys), may stop on `choice-pending` (:715), writes `ordnance-fired` (:776), PF effect :783, `KillEvents` (:810, defined :989), the Gun target's fate (:819), the hit's IFT unit effects via `EffectEvents` (:827-855), markers :859-949, `acquisition-changed` :966. The hit's IFT effects live inside `ordnance-fired.Resolution.Hit`/`.CriticalHit`, not as `fire-resolved`.
- Replay: `GameProjector.Ordnance` (Units/GameProjector.cs:1122) → `OrdnanceRecordVerifier.Verify` (LiveOrdnance.cs:620-765). State facts are recomputed by `FromState` (:647) and `WithRecomputedStateFacts` (Eligibility.cs:488); map facts are taken from the record by `ScenarioA1OrdnanceEligibility.WithRecordedMapFacts` (Rules/ScenarioA1OrdnanceEligibility.cs:420-481). The merged facts are compared as JSON with the recorded (:702), then a full re-`Resolve` compared with `JsonElement.DeepEquals` (:733-753). **A fact that gates a rule for older records (ruling R60.3) belongs among the facts taken from the record, never recomputed.** Absent-safe examples: `Elite` (OrdnanceModels.cs:53), `HeightAdvantage` (:106, taken from the record at Eligibility.cs:473), `Canister` (:223, :468); an absent resolution `DepletionNumber` dropped from the comparison (LiveOrdnance.cs:744).

## Target Type refusals (the ones pass 60 lifts)

| Case | Where | Code |
|---|---|---|
| Infantry TT at a Location with a vehicle (R25.10) | GamePlanner.Ordnance.cs:117-125 | `play.ordnance-vehicle` |
| Area TT at a hex with a vehicle (R9.3) | :102-108 | `play.ordnance-area-vehicle` |
| Area TT, units on several levels | :110-114 | `play.ordnance-area-levels` |
| Friendly units in the Area target | OrdnanceCalculator.cs:522-526 | `area-friendly-units` (no sentence in RefusalReasons) |
| Area TT at a Gun and crew | :529-532 | `target-outside` |
| No enemy unit (C3.41) | :534-538 | `target-outside` |
| Known and concealed together (C3.4) | `Undecided` :681-687 | `concealment-mixed` |
| Gun with no HE, AP/HEAT at a non-vehicle | `Outside` :336-342 | `gun-outside` |
| HE by Depletion Number, other ammunition at Infantry | :355-370 | `he-equivalency-not-built` |
| LATW (PF, PSK, ATR) at a non-vehicle | :377 | `target-type-outside` |
| PF/PSK/ATR ammunition | ArmorCalculator.cs:247-258 | `ammunition-outside` |
| MOL at a Location with any vehicle | FireCalculator.cs:714-720 | `asl.a1.fire.mol-outside` |
| DC placed where a vehicle is | Play/GamePlanner.DemolitionCharges.cs:57-60 | `play.dc-vehicle` |
| DC with vehicles in the attack | FireCalculator `DemolitionChargeOutside` :893-928 (:919) | `asl.a1.fire.target-outside` |

Refusal sentences: Play/RefusalReasons.cs:85-107. Targets of an Infantry TT hit: `ScenarioA1OrdnanceEligibility.AreaTarget` (Eligibility.cs:274; leaves vehicles out); `FromState` leaves vehicles out of targets (:204-207).

## Area, Air Bursts, Acquisition

- Air Burst: FireCalculator.cs:2233-2237 (`hit is { Area: true }` and woods, -1); area TEM :2238. Direct Fire +1 in woods elsewhere. Nothing for buildings.
- Indirect Fire handling: comments only. No building-level code (B23.32). A mortar may not fire from a building (`SupportWeaponLocationRefusal`, OrdnanceMapRules.cs:119).
- Area Acquisition: AreaCalculator.cs:80-83 (Case N per unit), gain :195-199, played at GamePlanner.Ordnance.cs:961-971, stored as `GunAcquisition` (OrdnanceRecords.cs:138). Bracketing: not found.

## Vehicles under fire

- `ScenarioA1FireCalculator.VehicleEffects` (:1621-1762, called :1446): unarmored on the Vehicle line (:1638-1691, A7.309 Unlikely Kill); AFV: not CE or stunned is `NotVulnerable` (:1693); CE crew +2 `crew-exposed` D5.31 (:1702); KIA/K → Recalled; MC/NMC/PTC → `crewCheck:<id>` roll (:1721-1756). Events: GamePlanner.Fire.cs:920, `VehicleEffectEvents` :1046, `VehicleConditions` :1081. Vehicles enter an IFT attack through `LiveFire.Vehicle` (LiveFire.cs:394); `LiveFire.CrewExposed` :383 (CE = not BU and not closed-topped). Limits on vehicles in an IFT attack: FireCalculator.cs:531-539 (one vehicle; never with an ordnance hit; no AFV in positive TEM, R25.6).
- Residual FP on a vehicle: GamePlanner.Vehicles.cs:194-224.
- Conditions (Units/GameTypes.cs): Motion :215, ButtonedUp `asl:bu` :218, Immobilized :221, Stunned :224, StunRecovery :227, Recalled :230, Abandoned :239, Wrecked :242, Shocked :251, UnconfirmedKill :254, Bogged :260. `vehicle-wrecked` `VehicleWrecked(Id, Burning)` (Units/LiveRecords.cs:12); `ScenarioA1OrdnanceEventRules.KillConditions` :53-77.
- D9.3 cover is built: OrdnanceCalculator.cs:864 (Case Q), FireCalculator.cs:2289.

## To Kill

- Transcription `docs/ASL/SourceRegistry/Supplements/c7-to-kill-tables.transcription.json`, embedded as `ScenarioA1.ordnance-to-kill.json`, loaded by `ScenarioA1ArmorReference.Load` (:186). Its boundary excludes FT, MOL, DC, MG, Indirect Fire, and the Gun Destruction Table. `BasicTk` :97, `CaseD` :117, `UnarmoredTk` :145, `ArmorFactor` :167. The AP list holds `{"tk": 4, "guns": ["39F:fighter", "@MG"]}`, never read (`GunSize` :70 never yields `@MG`).
- `ScenarioA1ArmorCalculator`: `Depletion` :100, `Outside` :230, `Run` :288-602, `Kill` :604-753. Hit location :613 (`armor.Turreted && dice[0] < dice[1]` → turret; R7.4), facing :617 (same hex by colored dr, `SameHexFacing` :206), Case A :633, Case C (CH doubles) :627, the destruction rows hard-coded :673-677, Dud :666.
- Critical Hits: Infantry TT OrdnanceCalculator.cs:930-943 (Random Selection C3.74 :995-1013; TEM reversed `critical-hit-tem` FireCalculator.cs:2252); Area AreaCalculator.cs:102-125.

## FT, MOL, DC

- FT: an IFT attack by a weapon `IsFt` (Play/GamePlanner.Fire.cs:119 `FirerFtFacts`); refusals `flamethrower-outside` FireCalculator.cs:704-710; FP :2723; no TEM :2229; fuel `FlamethrowerRemoved` :1481. No vehicle-specific refusal: it meets the general vehicle limits and `VehicleEffects`.
- MOL: request argument `mol` (GameActions.cs:215), special rule `mol:<side>`; planner GamePlanner.Fire.cs:124-131 (`MolBar`); refusal FireCalculator.cs:714-720; the user breaking on a colored 6: GamePlanner.Fire.cs:954.
- DC: Placed by the move's `placeDc`/`placeDcAt` (GameActions.cs:288) → `PlanPlaceDc` (DemolitionCharges.cs:21-100) → `DcPlacement` in a `movement-step` → `PlacedCharge` in the state (GameState.cs:524) → `asl.game.detonate-dc` `PlanDetonateDc` (:103). Thrown: `asl.game.throw-dc` `PlanThrowDc` (:140-203), then the thrower's own Location at +3 (`GamePlanner.Fire.cs:782-798`). Both end in `ChargePlan` (:206). Facts: `LiveFire.DemolitionChargeFromState` (LiveFire.cs:320), `ScenarioA1FireEligibility.DemolitionCharge` (:650-694; vehicles split out :668-682). Record `FireDemolitionCharge` (ScenarioA1FireModels.cs:396-417), modes placed/thrown/thrower; IFT DRM FireCalculator.cs:2355.

## Guns

- `EquipmentInstance` Kind `asl:gun`, `Holding { Role: Manned | Towed }`. `ScenarioA1OrdnanceEligibility.Emplaced` :36; `LiveOrdnance.Emplaced` :523 (`state.UnemplacedGuns`). Gunshield: OrdnanceMapRules.cs:187.
- Target: `GamePlanner.GunAt` (Ordnance.cs:596) builds `FireGunTarget(GunId, DefinitionId, CrewUnitId, Emplaced, Gunshield)` (FireModels.cs:668); needs `Manned`, so an unmanned Gun is never a target. `GunCrewAlone` FireCalculator.cs:1177; a crew stacked with others `crew-target-unreviewed` (:626, :790). To Hit Case P and Q: OrdnanceCalculator.cs:842-853; IFT +2 :2257; Near Miss gunshield +2 :2484.
- Fate: OrdnanceCalculator.cs:1046-1056 (CH destroyed; KIA destroyed; K malfunctioned) → `OrdnanceResolution.GunTargetFate` (OrdnanceModels.cs:499) → Play writes it at Ordnance.cs:818-824. No Gun fate from an IFT attack. A unit eliminated drops its weapons intact: `GameProjector.Eliminate` :3923 → `DropHeldBy` :4212.

## MG and vehicle weapons

- A vehicle's MGs: `FireAttack.VehicleFire` (FireModels.cs:122), `VehicleFireOutside` (FireCalculator.cs:936-990; no weapon named means the MA `aamg` only, :946), bars in ScenarioA1FireEligibility.cs:156-184.
- Fire Lane: `FireLaneInUseBar` (Eligibility :764), `FireLaneCancelledBy` :787, follow-ups ScenarioA1FireFollowUps.cs:28.
- PF: `ScenarioA1OrdnancePackage.Panzerfaust` (:99); `PanzerfaustCheck` OrdnanceCalculator.cs:86-121; limits ScenarioA1OrdnanceProjection.cs:14.

## The catalog

`src/ASL/units/catalog/scenario-a1.catalog.json`, `asl-scenario-a1` 1.17.0 (`LiveFire.CatalogVersion`, LiveFire.cs:50). Relevant ids: crews `attacker-crew`, `defender-crew`, `british-crew`, `american-crew`; Guns `attacker-inf-gun` (75* INF, H7, no AP), `defender-at-gun` (45L), `british-at-gun` (57L, HE by number, APDS), `american-at-gun` (37LL, Canister), the `campaign-` Guns (81mm mortar, 25-Pounder, 75L, 88L, 20L AA, quad 20L, Nebelwerfer; manufactured); SW `attacker-light-mortar`, `defender-light-mortar`, `british-light-mortar`, `attacker-psk`, `defender-atr`, `british-atr`, `attacker-ft`, `defender-ft`, `attacker-dc`, `defender-dc`; vehicles `attacker-truck`, `defender-truck`, `attacker-halftrack` (SPW 251/1, OT, AF 1/1, MA AAMG), `attacker-tank` (PzKpfw IIIH, AF 6/3), `defender-tank` (T-34 M41, AF 11/6), and the `campaign-` vehicles (Bren Carrier with MA `mg`, Sexton, Achilles, Cromwell, Sherman, Firefly, Wespe, Maultier, Hummel, PzKpfw IV, Panther, Tiger). MOL and PF are rules, not counters. The Crew Survival number has no color (C7.7 note A cannot be read).

## Records, actions, tests

- `OrdnanceFired(Gun, Crew, Target, Facing, RateOfFireKept, Acquisition, Acquired, Rolls, Facts, Resolution)` (OrdnanceRecords.cs:13); `FireResolved` (Units/FireRecords.cs:13). Reader `GameEventReader.cs` (`ordnance-fired` :730), writer `GameEventWriter.cs` :844. A new event type: record, reader case, writer case, projector arm, Studio/Services/PlayRecords.cs and ReplaySteps.cs (pattern: `gun-duel-voided`). `GameActions.All` GameActions.cs:730; `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation` (tests/...Play.Tests/PlayTests.cs:442).
- Tests: `tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass55Tests.cs` (fixture `Pass55Board` :56, `StubLos` :93, `Gun()` :137, `Tank()` :157, `SixPounder()` :403, `GermanTank()` :423, `GermanSquad()` :433, `Setup` :174 with catalog 1.13.0, `Do` :205, `Commit` :167, `FireAtTank` :436, `FireAtSquad` :445, `LastShot` :227, `NoReplayErrors` :242, scripted dice `Once(...)` :107). `tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass55RulesTests.cs` (`Gun(...)` :49, `Resolve` :63, `TankShot` :1363, `Finish` :1376; dice put in `OrdnanceRolls`).
- The Play page: Studio/Components/Play/OrdnanceFirePanel.razor (`#ordnance-gun`, `#propose-ordnance`), OrdnanceTargetFields.razor (`#ordnance-target`, `#ordnance-vehicle`, `#ordnance-ammunition`), SmallArmsFirePanel.razor (`#fire-from`, `.fire-firer`, `#fire-target`, `#propose-fire`); Pages/Play.razor (`ProposeOrdnance` :4644, `FireTargets` :5573, `OrdnanceVehicleChoices` :6440, `ProposeFire` :5587, `FirerChoices` :4735).

## The live test games

`src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/`: 396 files. A DC resolved: 1 (pass15-demo). MOL: none. An Area Target Type shot: 1 (pass9-check2). A Gun as target: 5 (p40-hazard, p40-hazard-control, p45-cr-crew, p55-duel-miss, p55-duel-void). 60 hold an `ordnance-fired`.
