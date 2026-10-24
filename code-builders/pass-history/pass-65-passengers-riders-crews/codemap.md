# Pass 65 code map

Made 2026-10-23 by a read-only frame agent on `vehicle-crew-figure` (a3b73d1f, treated as main). Paths under `src/ASL/`: **Rules/** is `LimboDancer.Domains.Asl.Rules/`, **Play/** `LimboDancer.Domains.Asl.Play/`, **Units/** `LimboDancer.Domains.Asl.Units/State/`, **Studio/** `LimboDancer.Domains.Asl.MapStudio/`, **Tests/** `tests/`. Lines drift: verify before relying on one. The pass 60 map (`../pass-60-armored-combat-iii/codemap.md`) covers the ordnance shot, To Kill, and FT/MOL/DC; its GameTypes.cs condition lines are stale (see section 3).

## 1. Passengers today

- State: a Passenger is a `UnitInstance` whose `Position` is `ContainedPosition(Container, Role)` (Units/GameTypes.cs:361) with `ContainmentRole.Passenger` (:351). No separate list; the vehicle holds nothing.
- Queries (Units/GameState.cs): `Location` :417 follows the container; `At` :445 leaves out anything aboard (`Aboard(item.Id) is null`, :450; D6.61, R26.2); `Aboard` :457 (Passenger or Rider, and SW they possess); `Passengers(vehicle)` :476 (Passenger or Rider). Each is a scan of the state (section 14).
- Load: `asl.game.move-vehicle` kind `load` → GamePlanner.Vehicles.cs:393 → `PlanLoad` (Play/GamePlanner.Passengers.cs:195). Checks: vehicle stopped, no MP spent (:199); each unit Infantry of its side on foot in the Location, not move-ended (:214); not broken, pinned, berserk, captured, Melee, TI (:220); no Double Time (`BoardingDoubleTimeBar`, Rules/ScenarioA1PassengerCalculator.cs:65); one MF left (`BoardingHalfMfLeft` :69, 4 MF allowance `ConveyanceAllowance` :62); `CapacityBar` (:246); MP kept `LoadKeptHalfMp` :72. Event `vehicle-step` `Kind = load`, `Units` (Passengers.cs:256).
- Unload: kind `unload` → `PlanUnload` (Passengers.cs:265): stopped, not Bypass (:271); named units must be Passengers (:282); not past 3/4 MP (`UnloadTooLate` :75); costs a quarter (`UnloadHalfMp` :78), each unit `UnloadMf` :81; units after a Prep Fire stay put (:303). Event `vehicle-step` `Kind = unload`, `Units`, `UnitMf` (:305).
- Projector: `StepVehicle` (Units/GameProjector.cs:2311); validity of load/unload :2392-2397; load sets `ContainedPosition(..., Passenger)`, `MfSpent + 1`, `MovementEnded` (:2462-2468); unload puts the unit on `MapPosition(at)` (:2469-2474). Unload is allowed from a Prep Fired, immobilized, or Abandoned vehicle (:2342-2346; Rules mirror ScenarioA1VehicleMovementCalculator.cs:252-262).
- Capacity: `PassengerCapacity` (Passengers.cs:19) → `ScenarioA1PassengerCalculator.PassengerCapacity` :11 (printed less 4 or 8 PP per towed Gun, C10.13); `PassengerPp` (Passengers.cs:29, Rules :18: squad 10, HS or crew 5, SMC 0, plus `Portage`); `CapacityBar` (Passengers.cs:37 → Rules :25: no capacity, more than four SMC, unrecorded PP, PP over capacity). Hook-up capacity: GamePlanner.Guns.cs:185-192 (`play.hook-capacity`); setup: `PassengerSetupBar` GamePlanner.Vehicles.cs:107 → ScenarioA1VehicleMovementCalculator.cs:114.
- Other boardings: a Gun crew boards as it hooks up (`gun-hooked` `Boards`, Units/OrdnanceRecords.cs:117; projector :2002-2017); setup placements with a vehicle holder (Studio role `passenger`, Play.razor:6699, :6853); entry from off board carries them (Passengers.cs:165, CardSetup.cs:64).
- Fate with the vehicle: `KeepPassengers` (GameProjector.cs:895, run after every event :256): a Passenger or Rider of a vehicle no longer Active is Exited with it or Eliminated with what it holds (`ScenarioA1VehicleProjection.PassengerExits` :54). Comment: Crew Survival for Passengers (D5.6) not built. Exit carries them (:2433).

Refusals that name a Passenger or Rider:

| Code | Where raised | Rule |
|---|---|---|
| `play.aboard` (every action but setup, move-vehicle, hook-gun, advance-phase, choose, pass-fire, end-move, button-up) | `AboardBar` Passengers.cs:327, gated by `ScenarioA1SequenceCalculator.PassengerBar` Rules/ScenarioA1SequenceCalculator.cs:136, called GamePlanner.cs:293 | R26.2 |
| `play.load` (8 texts) | Passengers.cs:202, :217, :222, :233, :239, :248; Rules :66 | D6.4, A4.5 |
| `play.unload` (4 texts) | Passengers.cs:273, :284, :291, :297 | D6.5 |
| CapacityBar texts (inside `play.load`) | Rules/ScenarioA1PassengerCalculator.cs:30, :35, :43, :50 | D6.1, A5.5, C10.13 |
| `play.setup-passenger` | ScenarioA1VehicleMovementCalculator.cs:125, :130 | A2.52, R26.2 |
| `play.hook-capacity` | GamePlanner.Guns.cs:191-192 | C10.13 |
| `UNIT-STATE-034` (replay) | GameProjector.cs:2413 | D6.4, D6.5 |

Studio filter: no action list offers a Passenger (Play.razor:4956, `ActingUnits` :7086).

## 2. Riders

- Only the enum `ContainmentRole.Rider` (Units/GameTypes.cs:354), its reader and writer names (GameEventReader.cs:1063, GameEventWriter.cs:1099), and the queries that treat a Rider as a Passenger (GameState.cs:469, :479; GameProjector.cs:898).
- No planner path creates a Rider, no Rider capacity data (no catalog attribute, section 10), no Rider rule, no Rider refusal. Comments only: ScenarioA1RecallCalculator.cs:27, ScenarioA1VehicleMovementCalculator.cs:252, :389, ScenarioA1ArmorCalculator.cs:640.

## 3. A vehicle's inherent crew

- Conditions (Units/GameTypes.cs): Motion :233, ButtonedUp `asl:bu` :236, Immobilized :239, Stunned :242, StunRecovery :245, Recalled :248, Abandoned :257, Wrecked :260, Shocked :269, UnconfirmedKill :272, Bogged :278, InexperiencedCrew :314. Abandoned is in `Undrawn` :317.
- CE/BU: stored as `asl:bu`; CE derived by `LiveFire.CrewExposed` (Play/LiveFire.cs:383 → `ScenarioA1FireEligibility.CrewExposed` Rules :63, `CrewExposedFacts` FireRuleModels.cs:34). Changed by `asl.game.button-up` (GameActions.cs:607) → `PlanButtonUp` GamePlanner.Vehicles.cs:904.
- Stun, Recall, pin of a crew: `ScenarioA1FireCalculator.VehicleEffects` (:2185; General Collateral Attack :2273-2300) → `CrewOutcome` (:1246); written by `VehicleEffectEvents` (GamePlanner.Fire.cs:1173).
- Shock: To Kill result (ScenarioA1ArmorCalculator.cs:771-796); recovery `asl.game.recover-shock` → GamePlanner.Shock.cs (Unconfirmed Kill wrecks with no Crew Survival, :58-61).
- Recall: `GamePlanner.MustLeave` (GamePlanner.Recall.cs:31 → `ScenarioA1RecallCalculator.MustLeave` :11); route `RecallRoute`; a Recalled AFV with Passengers may Stop to unload (`StopsToUnload`, Vehicles.cs:566; StopBar :518-520).
- Immobilization TC (D5.5): only on an ordnance To Kill `Immobilized` result when `CrewMayTakeTc` (ArmorCalculator.cs:797-815; facts ScenarioA1OrdnanceEligibility.cs:80), and a DC's (FireCalculator.cs:1989-2005, roll `taskCheck`). Failure sets `OrdnanceKill.Abandoned`.
- Crew Survival (D5.6): ArmorCalculator.cs:816-831 (eliminated, not `cs-passengers-only`, not Abandoned; +1 for impaired or Stun +1), AreaCalculator.cs:521-532 (mortar), FireCalculator.cs:2025-2038 (FT, MOL, DC). Record `OrdnanceCrewSurvival` (ArmorModels.cs:221). Roll keys `crewSurvival` (Ordnance.cs:892, :1309), `areaSurvival:` (:901).
- Where a crew counter is created: `CrewCounter` (GamePlanner.Ordnance.cs:1281): the first `asl:crew` definition of the vehicle's nationality (ordinal), id `{attempt}-{vehicle}-crew`, kind **`asl:crew`**, on the vehicle's Location. Called by `KillEvents` :1164 for a surviving crew (:1176) and an Abandonment by a failed TC (:1190-1196). `KillEvents` is also reached from FT/MOL/DC (GamePlanner.Fire.cs:1030-1035) and mortar hits (`AreaVehicleEvents` Ordnance.cs:1204).
- The other crew exit: `AbandonEvents` (GamePlanner.Recall.cs:242; same lookup, same id, kind `asl:crew`, plus `StunRecovery`), from a Recalled AFV immobilized by fire (Fire.cs:1017, Ordnance.cs:1224, rule `AbandonsWhenImmobilized` FireFollowUps.cs:202) and at Player Turn end (GamePlanner.cs:922-932, `RecallAbandoned`).
- No crew placed: IFT Vehicle-line wrecks (Fire.cs:1177-1183), vehicle CC kills (GamePlanner.VehicleCloseCombat.cs:369-378), Unconfirmed Kill (Shock.cs:60), burning wrecks (Ordnance.cs:1171). No bail out of Passengers. No voluntary Abandonment, no self-destruction.
- An Abandoned vehicle in state: still `Active`, same side, `asl:abandoned` and Motion false (`AbandonedConditions` Rules/ScenarioA1OrdnanceEventRules.cs:98; Recall.cs:244-248). It may not move (GameProjector.cs:2344) or fire (`AbandonedVehicleBar` LiveFire.cs:93, `TankRefusal` LiveOrdnance.cs:107), loses its Armor Leader (`ScenarioA1ArmorLeaders.Lost` :140), gives enemy Infantry cover (Wrecks.cs:56), and `HasInherentCrew` is false (GamePlanner.Victory.cs:102-110, which finds the crew by `Kind == "asl:crew"` and the id suffix).
- Re-crewing: none.
- Armor Leader: inherent on the vehicle, `UnitInstance.ArmorLeader` (GameTypes.cs:530, record :540), not a counter aboard; kept by `KeepArmorLeaders` (GameProjector.cs:275).

## 4. Collateral Attacks

- General (D.8B): `ScenarioA1FireCalculator.VehicleEffects` (:2185-2300): unarmored vehicles on the Vehicle line, a CE not-Stunned AFV crew at +2 (:2273-2287). Vehicles enter an IFT attack via `LiveFire.Vehicle` (:394); limits FireCalculator.cs:538-549. Ordnance Infantry TT: vehicles affected `ScenarioA1OrdnanceCalculator` :1314-1320; Area (mortar) AreaCalculator.cs:291.
- Specific (D.8A, R60.7): ordnance `SpecificCollateral` (ScenarioA1ArmorCalculator.cs:606-610, :642-679), gated by `OrdnanceShot.SpecificCollateral` (OrdnanceModels.cs:129; set LiveOrdnance.cs:402, :563; taken from the record OrdnanceEligibility.cs:557-562); FT/MOL/DC at a named AFV FireCalculator.cs:1782-1813. Written by `VehicleEffectsAfterHit` (Ordnance.cs:1216; Fire.cs:1038-1041). Roll `collateralCheck`.
- Passengers excluded: never named. Targets come from `GameState.At`, which leaves them out (section 5); the ArmorCalculator summary says "Passengers and Riders are pass 65's" (:640). No fact for Passengers, Riders, an unarmored vehicle's PRC, or an Abandoned AFV exists in `FireVehicle`/`OrdnanceVehicleTarget`.

## 5. Fire facts at a vehicle's Location

- IFT: `LiveFire.FromState` targets `state.At(target)` (LiveFire.cs:151) → `ScenarioA1FireEligibility.SelectTargets`; vehicles split out to `FireVehicle` (`LiveFire.Vehicle` :394). Other `At` reads: :301, :339, :498, :526, :553 (OVR).
- Ordnance Infantry TT: `state.At(target)` (LiveOrdnance.cs:236), the mortar's hex-wide read with an explicit `Aboard is null` (:234), vehicles :254-262; PF/LATW :521. A shot naming no vehicle at a Location with one: `ScenarioA1OrdnanceEligibility` :335-345.
- Passengers are therefore never targets of fire, ordnance, OVR, or CC today, and are lost with the vehicle (section 1).
- Mounted Fire: nothing. One comment, FireCalculator.cs:982 ("D6.64 is not reviewed"). Any fire action naming a Passenger is refused `play.aboard`.

## 6. Rout, CC, OVR, leaders

- Rout: `MustRout` (GamePlanner.Rout.cs:20; Passenger free of rout, :24-26, `ScenarioA1RoutCalculator` :28); failure-to-rout scan :907-908 skips them.
- CC: all CC Location reads use `state.At` (GamePlanner.CloseCombat.cs:129, :185, :425, :453, :761, :773, :855; LiveCloseCombat.cs:28; GamePlanner.VehicleCloseCombat.cs:37, :45, :70, :92, :121, :130), so Passengers take no part.
- OVR: `OverrunBar` enemies via `state.At` (GamePlanner.Overrun.cs:34); `LiveFire.OverrunFromState` targets (LiveFire.cs:553).
- Other skips by `Aboard`: Map.cs:201 (`EnemyScan`), MandatoryFire.cs:35, Encirclement.cs:49, SupportWeapons.cs:38, ScenarioVictory.cs:318, Stacking (ScenarioA1Stacking.cs:16, SetupCalculator.cs:742), A12.34 range (ScenarioA1MovementCalculator.cs:161).
- Leaders aboard: no rule; a leader Passenger counts 0 PP (`PassengerPp`). `ScenarioA1ArmorLeaders` (Rules :9) handles only the inherent Armor Leader.

## 7. Wrecks and Scrounging

- GamePlanner.Wrecks.cs: `BlazeId` :17, `WrecksAt` :20, `IsBurning` :27, `Standing` :38, `CoverAt` :45 (D9.3), `VehicleHindrance` :66 (D9.4, B25.2), `SmokeSources` :124, `WreckEntryHalfMp` :133.
- Wreck face: `vehicle-wrecked` `VehicleWrecked(Id, Burning)` (Units/LiveRecords.cs:12; reader :814, writer :927) → `GameProjector.Wreck` (:1485): `Status = Wrecked`, `asl:wrecked`, towed Gun eliminated, holdings dropped. Catalog: `crew-survival` sits on face `wreck`.
- Attacks on a wreck: none; a wreck is not Active, so no target list holds it (only a declared OVR of a vehicle wrecked mid-move, LiveFire.cs:549).
- Scrounging (D10.5): nothing.

## 8. Captured vehicles and Control

- A11.52 only: an unarmed vehicle alone with enemy Infantry is captured as the CCPh begins (GamePlanner.cs:904-912; `CapturedVehicles` VehicleCloseCombat.cs:217; rule `ScenarioA1VehicleCloseCombatRules.CapturedVehicle` :142); conditions Captured and Abandoned (`CapturedVehicleConditions` SequenceCalculator.cs:401); text `play.cc-vehicle-capture` ... "the use of captured vehicles is not built" (:405).
- A21.2 to A21.22 use, Temporary Drivers and Crews: not built (ScenarioA1Definitions.cs:145, ScenarioSetup.cs:167, GamePlanner.SupportWeapons.cs:92).
- Control A26.12: a vehicle's temporary Control in Victory (ScenarioA1VictoryCalculator.cs:176, :265, :656; ScenarioVictory.cs:32). Nothing about a captured vehicle's side.

## 9. Burning wrecks, Brew Up, Immobilization TC

- Burning: `OrdnanceKill.Burn` (ArmorModels.cs:175) → `VehicleWrecked(Burning: true)` plus a Blaze entity `asl:fire` (Ordnance.cs:1169-1175; Fire.cs:1179-1183; VehicleCloseCombat.cs:371-378). Smoke, light (Night.cs:67), MP (Wrecks.cs).
- Brew Up (D9.2): nothing (no match for "Brew" or D9.2).
- Immobilization TC: section 3. The second cause (an AFV immobilized other than by a To Kill or DC: Bog, Mechanical, mortar) is not built; `vehicle-check-rolled` `immobilized` (LiveRecords.cs:346) takes no TC.

## 10. Catalog and vocabulary

- `src/ASL/units/catalog/scenario-a1.catalog.json`: `asl-scenario-a1` 1.17.0, `"vocabulary": ["asl@1.3.0"]`; 196 definitions, 17 `asl:vehicle`, 4 `asl:crew` (`attacker-crew`, `defender-crew` FP 2 ML 8; `british-crew` 2/8; `american-crew` 2/7). No `asl:vehicle-crew` definition.
- Vehicles: `attacker-truck`, `defender-truck` (unarmored, `cs-passengers-only`, PP 21, towing 7/8, cs 6), `attacker-halftrack` (OT, AF 1, AAMG, PP 15, cs 5), `attacker-tank`, `defender-tank`, `campaign-british-` sexton, achilles, cromwell, sherman75, firefly, bren-carrier, `campaign-german-` wespe, maultier, hummel, pziv, panther, tiger. PP only on the trucks, halftrack, Bren Carrier.
- Attribute names: `passenger-capacity` (face front), `towing`, `crew-survival` (face wreck), trait `asl:cs-passengers-only`; read by `ScenarioA1FireReference` (:117-118, :695) and `ScenarioA1ArmorReference` (:247). No vehicle-class, Rider, crew FP, or crew morale attribute; crew morale is derived (`AfvCrewMorale`; ArmorCalculator.cs:764).
- `campaign-british-bren-carrier` (sheet MFG, transcription rows 2851-2893): Bren carrier (game default), fully-tracked, MP 18, AF 1/1, MA `mg`, ROF 1, BMG 2, PP 5, cs 5 (wreck face), open-topped; towing not printed; no Carrier marker.
- Adding a definition: rows in `docs/ASL/SourceRegistry/CounterSheets/scenario-a1.counter-transcription.csv` (kind and nationality on face `counter`), the manifest entry `src/ASL/units/catalog/scenario-a1.catalog-manifest.json` (:214 for the Carrier; header names the source record `asl-counter-sheets.scenario-a1.source-record.json` and worksheet), built by `CounterSheetCatalogBuilder` (src/ASL/LimboDancer.Domains.Asl.Units.CounterSheets/CounterSheetCatalogBuilder.cs; kind checked against the vocabulary :235-238), the synthetic catalog `scenario-a1.synthetic.catalog.json` (1.0.0, asl@1.3.0) kept in step. A bump moves `LiveFire.CatalogVersion` (Play/LiveFire.cs:50), `code-builders/test-games/game_file.py` `CATALOG` (:28), test pins (BacklogPass60Tests.cs:181, UnitStudioTests.cs:209-210); pass 55 used `code-builders/pass-history/pass-55-armored-combat-ii/repin_tests.py`.
- Vocabulary `src/ASL/units/vocabulary/asl.vocab.json` 1.18.0: `asl:vehicle-crew` (:300), extends `asl:crew`, sizeClass 2, rule A1.123, D5.1. Example `example-vehicle-crew` (units/examples/catalog.units.json:48, asl@1.18.0, american, 1-2-6, broken 7); pose `vehicle-crew` (Units.Rendering/FigureArt.cs:46; Studio/Services/FigureArtLab.cs:78).
- Kind risk: about 40 lines in Play, Rules, and Units compare the literal `"asl:crew"` rather than `vocabulary.IsA` (e.g. ScenarioA1Stacking.cs:64, ScenarioA1VictoryCalculator.cs:27, :366, ScenarioA1SetupCalculator.cs:297, :302, ScenarioA1FireCalculator.cs:71, :672, :837, :2587, :4170, ScenarioA1OrdnanceCalculator.cs:103, ScenarioA1Definitions.cs:337, ScenarioA1VehicleCloseCombat.cs:241, :347, GamePlanner.Victory.cs:109, MoppingUp.cs:71, Starshells.cs:43, Ordnance.cs:741). A unit of kind `asl:vehicle-crew` would miss each.

## 11. The Play page

- Studio/Components/Play/VehicleStepChoices.razor: `#vehicle-load-units`, `#vehicle-load` (:61-67), `#vehicle-unload-units`, `#vehicle-unload`, `#vehicle-unload-note` (:73-80). VehicleMovementPanel.razor (:14), VehicleMovementStatus.razor, VehicleTowingActions.razor (:6, crew boards on hook-up), VehicleFireEffectsTable.razor (:12, `FireText.CrewText`).
- Studio/Components/Pages/Play.razor: `CargoNotes` :3355 ("aboard X", "Passengers ..."), `LoadableUnits` :3488, `UnloadableUnits` :3504, dispatch `"load" or "unload"` :3518, `ProposeVehicleLoad` :3539 (move-vehicle with `units`), no Passenger acts :4956, `ActingUnits` :7086, setup holder role `passenger` :2417, :2775, :6699, :6853.
- Records: Studio/Services/PlayRecords.cs :310 writes a load or unload as "{vehicle} moves to {At}" (no Passenger named); ReplaySteps.cs :312-315 "a vehicle moves in ..."; crew TC and Crew Survival texts PlayRecords.cs:616-646, FireText.cs:262.
- Tests: `VehicleComponentTests.TheStepChoicesProposeEntriesPassengersAndTheHexWishedToEnterNext` (Tests/LimboDancer.Domains.Asl.MapStudio.Tests/VehicleComponentTests.cs:101), `TheTowingActionsHookUpWithTheCrewOrUnhookWithAFacing` :143; `PlayPagePass26Tests.APassengerIsPlacedAboardAndUnloadsThroughTheVehiclePanel` (PlayPagePass26Tests.cs:78; helpers `Hexes` :51, `Commit` :60, `Place` :68).
- Schema drift: the move-vehicle descriptor (GameActions.cs:549-566) lists kinds start, turn, enter, stop, exit, esb, overrun and no `units` or `turret`, with `additionalProperties: false`, yet the planner takes load, unload, delay, and `units`.

## 12. Records and actions

- Boarding and unloading: `vehicle-step` `VehicleStepped` (Units/LiveRecords.cs:236; `Load` :249, `Unload` :252, `Units` :279, `UnitMf` :285); reader GameEventReader.cs:527, writer GameEventWriter.cs:608, projector arm GameProjector.cs:140 → `StepVehicle` :2311. Hook-up boarding `gun-hooked` (reader :505).
- Crew survival and abandonment: no event of their own. The verdict lives in the `ordnance-fired` or `fire-resolved` resolution (`OrdnanceKill.CrewSurvival`, `.CrewCheck`, `.Abandoned`); the effects are `conditions-changed` (reader :243) and `instance-created` (reader :228) of `{attempt}-{vehicle}-crew`.
- `GameActions.All` GameActions.cs:730 (list :735); `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation` (Tests/LimboDancer.Domains.Asl.Play.Tests/PlayTests.cs:442).
- Projector `ConditionName` (GameProjector.cs:703) maps 15 `Rules.UnitCondition` values and throws on any other (Abandoned, Immobilized, Captured, ButtonedUp, Shocked are absent); the planner's own `ConditionName` (GamePlanner.Fire.cs:1300) maps Abandoned (:1325). `Rules.UnitCondition` enum: Rules/ScenarioA1FireRuleModels.cs:414-452.
- New event type pattern: record, reader case, writer case, projector arm, PlayRecords.cs and ReplaySteps.cs (pass 60 map: `gun-duel-voided`).

## 13. Tests a pass 65 task would add to

- Tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass26Tests.cs (711 lines, catalog 1.13.0 at :122, :690): `Planner` :92, `Play` :96, `Place` :108, `Act` :137, `Drive(vehicle, kind, extra)` :146, `Pass` :154, `EndMove` :162, `AdvanceTo` :170, `Inject` :180, `Refused` :191, `OnMap` :233, `Aboard(id, definition, vehicle)` :243, `OffBoard` :246, `Gun` :248, `Towed` :262, `SetUp` :277. Passenger tests :291, :334 (load, unload, `play.aboard` :328), :480, :638 (immobilized vehicle, broken Passenger), `TheObjectIndexReadsEveryLocationAndPositionAsTheStateDoes` :427 (PR 32).
- BacklogPass60Tests.cs (936 lines, catalog 1.17.0 at :181): `Pass60Board` :58, `StubLos` :92, `Once` :102, `Unit` :116, `Gun` :130, `Setup` :171, `Do` :200, `Shoot` :210, `LastShot` :221, `NoReplayErrors` :225, `Vehicle(id, definition, at, side, facing, crewExposed)` :235, `LastFire` :550; collateral tests :343, :368, :416, :435.
- BacklogPass55Tests.cs (2259 lines, catalog 1.13.0): `Once` :107, `Gun` :137, `Tank` :157, `Setup` :174, `Do` :205, `LastShot` :227, `NoReplayErrors` :242, `GermanSquad` :433, `FireAtTank` :436, `Drive` :1234.
- Tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass60RulesTests.cs (1457 lines): `Resolve` :69, `TankAtTank` :324, `Vehicle` :343, `AtLocation` :350, `Mortar` :488, `Named` :727, `AtAfv` :733, `Charge` :741; Specific Collateral tests :363-411. ScenarioA1Pass55RulesTests.cs: `Gun` :49, `Resolve` :63, `TankShot` :1363. Passenger calculator tests: ScenarioA1VehicleMovementRulesTests.cs:331-340.
- `code-builders/test-games/game_file.py`: `GameFile` :60 with `unit` :94, `weapon` :99, `vehicle` :105 (`bu=False` for CE), `gun` :111, `phases` :119, `write` :124; `CATALOG` :28. No Passenger helper.

## 14. Memory and performance

- `GameState.Find` (:401) and `Unit` (:403) scan; `Location` (:417) and `Aboard` (:457) call `Find`; `At` (:445) calls both for every object, so one `At` is quadratic in the object count. `Passengers` (:476) is one scan.
- Memo: `MemoOf(state)` (Play/GamePlanner.PlayStepMemo.cs:20), `PlayStepMemo` :23 holds UpperLevelEncircled, rout answers (RoutEnemies, MustRout, RoutSurrender, RoutAdvice), board Handles and Reads, DefenderMayFire, Charges, ExitEdges, `Objects` :96, `Custodians` :102, `AttackerRoutOwed` :114, `Composed` :132.
- PR 32 (c69dd542): `ObjectIndex` (:151; `Of` :162, `At` :190, `Location` :193), objects by Location and positions by id, built once per state from `Location` and `Aboard` (Passengers left out as `At` does); used by the charge search and BypassStep. It has no Passengers-by-vehicle map.
- Per-unit scans in passenger code: `AboardBar` (Passengers.cs:327) does `Find` plus `Aboard` for every string in the arguments on every action except the eight, after a full `Replay(existing)` (GamePlanner.cs:294); `PlanLoad` loop (:212-244) does `Unit`, `Aboard`, `Location` per named unit; `CapacityBar` calls `Portage` per Passenger (GamePlanner.Movement.cs:38, an Equipment scan each); `KeepPassengers` (GameProjector.cs:895) scans Units and `Find`s each container after every event; `state.Passengers(id)` is called up to three times in one vehicle step (Vehicles.cs:520, :544, :566); state-wide loops with `Aboard` per unit: Map.cs:201 `EnemyScan`, Rout.cs:908, SupportWeapons.cs:38, ScenarioVictory.cs:318, CardSetup.cs:65. `ObjectIndex.Of` itself calls `Location` and `Aboard` per object (quadratic once per state).
