# Frame agent C: the Sniper's targets and results

(The agent's closing report, kept as it came, 2026-10-26 about 08:3x.)

Read: the brief, the plan entry and prompt, `ScenarioA1Sniper.cs`, `GamePlanner.Snipers.cs`, `GamePlanner.MoraleChecks.cs`, `ScenarioA1PrcRules.cs`, `ScenarioA1ResultTables.cs` (VehicleConditions), parts of FireEligibility, FireCalculator (Unprotected crew), FireFollowUps, Ordnance (`UnprotectedCrewEvents`), Wrecks (SMOKE sources), ConcealmentChoices, GameProjector (Snipe verifier, SmcCrewAfter, phase-end clearing); backlog 25, 26, 55, 72.1; inventory A14.2 to A14.33; pass 70 notes 3b; rulebook pp. 65, 77, 78, 82, 83, 85, 193, 194, 203, 204, 225.

## 1. Today's code
- Rules (`ScenarioA1Sniper.cs`): `Attacks` :105 (a pinned Sniper does not attack); `Effective` :108 (dr <= 2); `RandomLocation` :111; `Eligible` :114 (active, attacked side, not a vehicle, not hidden, not captured, on map); `TargetTem` :117 (the terrain key's TEM only); `TargetHex` :120 (nearest, TEM, name); `TargetLocation` :127 (most targets, then lowest level); `Candidates` :134 (A14.23), `Tied` :148, `Picked` :156; `UnitEffect` :166, `Mortal` :175, `ReductionResult` :178; `Wounded/Broken/PinnedConditions` :181-188 (each clears Concealed and Hidden); `VerifySniperAttack` :191, `ComesUnderDm` :197.
- Play (`GamePlanner.Snipers.cs`): `AddSniperAttacks` :20 on fire records only; `Snipe` :75: pin bar :86, Sniper dr :105, Random Location :114-120, eligible :123, hexes and TEM :132-136, Location :139-142, counter moved :145, candidates and Random Selection :148-167, per-target loop :169-206, LLMC :208-216, DM :218-227; `SnipeUnit` :234-294.
- Events: `dice-rolled` (purposes `sniper`, `sniper-location`, `sniper-random-selection`, `sniper-wound-severity`); `sniper-attacked` (`SniperAttacked(sniper, trigger, roll, dr, at, unit, result)`, results none, no-target, dummy-stack-eliminated, eliminated, wounded, broken, casualty-reduced, pinned); `instance-moved` (the counter), `instance-eliminated`, `conditions-changed`, `lineage`; from the LLMC `dice-rolled`, `morale-check-resolved` and its effects, `surrender-pending`.
- Projector: `Snipe` (GameProjector.cs:4871) only verifies the counter and the rolls; later events apply the attack.

**Finding: the wrong units are eligible today.** `Eligible` takes `state.Location(unit.Id) is not null`, and `GameState.Location` (GameState.cs:429-454) follows a `ContainedPosition` to its vehicle: every Passenger, Rider, and crew inside any vehicle, a BU tank's included, is eligible, and the vehicle never is. Against A14.22. Fixing it changes new plans only (the verifier does not re-check eligibility).

## 2. A14.22 eligibility, A14.3 results for the new targets
Facts: `FireDefinition.Unarmored` and `OpenTopped` from catalog traits (FireReference.cs:727-728). **Partially Armored is not read:** the catalog has `asl:partially-armored` (vocab :251, about 9 vehicles) but no property; one line `Trait("front", "asl:partially-armored")` plus a property. CE: `LiveFire.CrewExposed(vehicle)` via `ScenarioA1FireEligibility.CrewExposed` :63 (Stun, Recall, Shock: not CE). PRC exposure `ScenarioA1PrcRules.Exposure` :25 (Open: Rider or unarmored; Exposed: Carrier CE not broken/pinned; Spared; Buttoned). PRC from the index `MemoOf(state).Objects(state).VehicleOf / Aboard / CrewOf` (MoraleChecks.cs:229-230).

Proposed Rules member `SniperVulnerable(role, unarmored, partiallyArmored, exposure)`: a PRC Vulnerable when Open or Exposed, or its vehicle Partially Armored or unarmored; the Inherent crew Vulnerable when CE, Partially Armored, or unarmored; an AFV with no Vulnerable PRC out; Riders Vulnerable; a Temporary infantry crew is the vehicle's crew here.

New candidates: the Inherent crew (vehicle id, role "crew"), an unarmored vehicle (role "vehicle"), Vulnerable PRC. A14.33: vehicle and its PRC never by the same dr. The empty unarmored vehicle "ineligible at the Sniper player's option": proposal ineligible by default (a ruling), the option joining the Sniper's pending choice if built.

| Target | dr 1 | dr 2 |
|---|---|---|
| CE crew of a fully armored AFV | Recall (D5.341, p. 203) | Stun (D5.34); a crew under Stun +1 Recalled instead (D5.342) |
| Inherent crew of a Partially Armored AFV or unarmored vehicle | breaks | pins |
| Unarmored vehicle | immobilized; none if already (A14.33) | none |
| Vulnerable PRC | as today | as today |

Existing: `ScenarioA1ResultTables.VehicleConditions(FireVehicleEffect)` :86 (Stunned + BU + Motion off; Recalled + not Stunned + not StunRecovery + BU + Motion off; Immobilized + Motion off). The projector handles all three as `conditions-changed` (Stun end and +1, `EndStuns` GameProjector.cs:585-587, 704; the Recall's Armor Leader :364; Passengers BU derived). Breaking the Inherent crew: reuse pass 65's `UnprotectedCrewEvents(state, vehicle, attemptId, broken: true)` (GamePlanner.Ordnance.cs:1266); D5.311 says "[EXC: Sniper attack]", so a ruling. Pin: `VehicleCondition.Pinned`, or Pinned on a crew counter. New `sniper-attacked` result strings (stunned, recalled, crew-broken, crew-pinned, immobilized) absent-safe.

Meets agent D: a Stun or Recall on a vehicle manned by a SMC Temporary crew already Wounds or eliminates him in the projector (`SmcCrewAfter`, GameProjector.cs:4343-4368).

Out: 1MT AFV Stun = Recall (no catalog vehicle); subterranean Locations (pass 145 and G). Interior Building Locations: Play does not know them (Maps has `IsBuilding`, `IsFactory`, no interior-hex fact; B25.63, p. 144): to pass 135 with a row.

## 3. A14.21's TEM comparison
`TargetTem` reads `ScenarioA1FireReference.Tem` (FireReference.cs:237-251), the minimum over the hex's targets' Locations. No HA or Factory TEM in that table, so their NA holds. SMOKE: `SmokeSources` (GamePlanner.Wrecks.cs:143: grenades `asl:smoke` and burning wrecks), `ScenarioA1VehicleSightRules.SmokeDrm`; cheap to add (proposal: as from outside the hex, capped at +3). Fortifications not in state: pass 115.

## 4. A14.31: the enemy Sniper as a target
The hex search groups only eligible units (Snipers.cs:133): a hex holding only the enemy Sniper gives `no-target`, against A14.21. Add the enemy `asl:sniper` entity as a hex in the search (TEM the hex's lowest), part of every Location of its hex, a Random Selection candidate. dr 1 `instance-eliminated` on the entity (SAN is B's); dr 2 Pinned on the entity. **Gap:** the projector clears the CCPh pins on Units and Equipment only (GameProjector.cs:670-672), never Entities: a pinned Sniper stays pinned. Add Entities (absent-safe). The prior choice (A14.2 EXC): into the same pending choice if built; else plain Random Selection with each a candidate (a ruling).

## 5. LLMC and LLTC after a hit
Built: pass 70's 3b (notes.md:68); Snipers.cs:208-216 calls `AddLeaderLossChecks` with `ScenarioA1MoraleCheck.Sniper` for a leader eliminated by dr 1 or a mortal wound; Heat of Battle, ELR, wounds, cascade by the member. A wound causes no LLMC (A17.3); a Sniper never breaks a SMC. Left: stale documents (backlog 25 row at Backlog.md:350, inventory A14.3) closed (0:05); pass 70's no-ELR row stays; an Armor Leader's Recall elimination's LLMC to the backlog.

## 6. The A12.14 exception (backlog 72.1, Backlog.md:1381; A12.14 p. 78)
`asl.game.remove-concealment` (`PlanRemoveConcealment`, GamePlanner.ConcealmentChoices.cs:26); `RemovalBar` (ScenarioA1ConcealmentChoices.cs:19) says the exception is not built. Today the Random Location DR and the counter's move are one commit, so the exception holds by construction; it needs work only with a pending choice before relocation (`RemovalBar` takes "a Sniper attack awaits its hex"): 0:20 with the choice, 0:05 without.

Related deviation: A12.14 (p. 77) says a Sniper attack (dr 1 or 2) costs the "?" only "while in the LOS of a Good Order enemy ground unit"; breaking, Wound, Reduction always cost it. Today a pin always strips "?" and a dr-2 no-effect never does. Proposal: read pass 70's sight table for the pin and no-effect cases (0:45), or a ruling keeping today's.

## 7. Backlog 26: the captured MG's Sniper dr
Backlog.md:365, E1.76 (p. 225): night; the plan places E1.76 in task 90.6. Not this pass's.

## 8. Hidden information
A14.23 built; all `sniper-attacked` records and dice public. **Leak:** `SnipeUnit`'s None branch (Snipers.cs:290-292) names `unit.Id` for a unit that keeps its "?" (a concealed pinned MMC on a dr 2). Write null, or withhold as `AddMoraleCheck` does (0:15). New results change visible counters; a concealed vehicle hit loses "?" under the LOS proposal.

## Memory and performance
`Snipe` calls `state.Location(unit.Id)` four or five times per unit over all units (:123-142); the new reads from `MemoOf(state).Objects(state)`; converting the existing ones 0:20.

## Estimates
| Part | H:MM |
|---|---|
| A14.22 rule, PartiallyArmored, candidates from the index, PRC eligibility fix, empty vehicle default | 2:30 |
| A14.3, A14.33, D5.34 results | 2:00 |
| A14.31 the Sniper as target; entity pin clearing | 1:00 |
| A14.21 SMOKE | 0:30 |
| A12.14 LOS loss on pin / no-effect | 0:45 |
| A12.14 exception | 0:20 |
| Hidden-info fix | 0:15 |
| Index reads | 0:20 |
| LLMC documents | 0:05 |
| Total | 7:45 |

Moved out: Interior Building Locations 135; subterranean 145 and G; Fortification TEM and reveal 115; captured MG at night 90 (90.6); 1MT AFV Stun = Recall with those vehicles; Armor Leader Recall's LLMC backlog.

Not read: GameProjector beyond the sections named; `ScenarioA1Sight` and `GamePlanner.Sight.cs`; the A14.2 to A14.4 examples; D6 Rider rules on CE; the Index's "Interior Building Hex"; the tests; whether SMOKE grenades are Dispersed in state.
