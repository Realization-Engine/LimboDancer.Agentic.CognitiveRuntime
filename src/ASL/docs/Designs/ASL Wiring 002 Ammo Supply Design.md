# Wiring 002: ammunition supply

**Status:** Approved by the user on 2026-10-29. Claim `wire-work-1`, Codex, reviewer Claude, branch `codex/wire-work-1`, taking W-E10 in inventory commit `49491cce`. Implementation follows `wire-fire-3`'s D3.7 work. Code merge remains separately authorized.
Revised 2026-10-31 to answer Claude's review; the changed sections are listed in section 10, for the user to see before building.

## 1. Dependencies and the reviewed rule

D3.7 Low Ammo, X# and versioned fire outcomes had to reach main before E10's B12 benefit is enabled; the user ordered that dependency in Wiring Prompt 002. They have: `wire-fire-3` and the D3.7 adapters merged as PR 68 (`0fea9104`, 2026-10-31), and W-C9 (`wire-fire-2`) merged as PR 85 (`2edd2106`). The B12 benefit composes with `ScenarioA1VehicleBreakdownResolutionCalculator.Numbers`, which PR 68 put on main. The inventory, not this design, is the ownership ledger.

Read the approved [E10 rulings](<ASL E10 Ammo Vehicles Claim Design.md>): physical PDF p. 244, D.7 p. 192, D3.71 pp. 201 and 202, Vehicle Note 93 p. 348 and ASOP 8.43B p. 681. The Codex read-only review found no blocking calculation defect within approved RE10.6 and RE10.11. It corrected the design's statement about the E10.3 table (it prints <=9) and identified the need to correct the contract's incomplete inspection wording. A12.16, p. 79, requires inspection authority even for an unconcealed enemy stack: the opponent may inspect it "unless the stack is out of the LOS of all of his Good Order ground units". Its facts cannot be read from full state just because it has no concealment counter.

The existing work route is an adjudication route, not a complete governed E10 command. Do not relabel a hand-entered success as automated legality. Build a revision-bound proposal that gathers the supported facts, asks the calculator, and records verified evidence. The Rules prerequisites below are a rule claim of their own (section 2), not a reduced definition of done.

## 2. Order of work

AGENTS.md (2026-10-31): every branch starts from main, every pull request targets main, and a wiring claim takes rows only once their rule claims are merged into main. A wiring claim changes Rules only to remove the old members its rows name. So the work goes in this order:

1. **The rule claim `e10-supply-adapters`** (Claude builds, Codex reviews, branch `claim/e10-supply-adapters` from main). Its claim commit adds it to the inventory's section 6 with these held Rules files: `ScenarioA1AmmoVehicleCalculator.cs`, `ScenarioA1AmmoVehicleModels.cs`, `ScenarioA1AmmoVehicleContract.cs`, `TurnWorkLifecycle.cs (shared)`, `TurnWorkRules.cs (shared)`, `TurnWorkEffects.cs (shared)`, `ScenarioA1VehicleBreakdownResolutionCalculator.cs (shared)`, `ScenarioA1VehicleBreakdownResolutionModels.cs (shared)`, `ScenarioA1OrdnanceCalculator.cs (shared)`, `ScenarioA1AdvanceCalculator.cs (shared)`, `ScenarioA1RuleContracts.cs (shared)`, and `ScenarioA1DemolitionChargeRules.cs (shared)` once the A23.3 claim, which holds that file whole while it is "built", reaches "reviewed". It builds what section 5 lists, the rulings of section 9, the A12.16 contract correction and the <=9 comment (backlog 85, third row), and repoints the proving slice's contract (`ScenarioA1RuleContracts.cs:280, :321`) from `TurnAmmoEligibilityRules.EvaluateRecipients` to `ScenarioA1AmmoVehicleCalculator.Recipients` (answers equal by RE10.10), so that class has no Rules caller left. Old members keep their names and answers (CONVENTIONS.md, section 2, step 5). It merges on its tests and a green CI run, and counts toward the next batch gate.
2. **wire-work-1's added holds.** After step 1 merges, on an up-to-date main, one claim commit adds to wire-work-1's row (section 8) the files its sites need that it does not hold today, by path under `src/ASL/`: `LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs (shared)`, `LimboDancer.Domains.Asl.Play/GamePlanner.VehicleCloseCombat.cs (shared)`, `LimboDancer.Domains.Asl.Play/GamePlanner.MotionAttempts.cs (shared)`, `LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs (shared)`, `LimboDancer.Domains.Asl.Play/LiveFire.cs (shared)`, `LimboDancer.Domains.Asl.Play/LiveOrdnance.cs (shared)` and `LimboDancer.Domains.Asl.Play/LiveCloseCombat.cs (shared)`. `wire-fire-3`, which listed most of them shared, merged with PR 68; run `claim_check.py --touched` first and agree the order with any open claim whose branch changes them.
3. **wire-work-1 builds** on a fresh branch from main, then merges main again before its pull request (AGENTS.md, step 3).

## 3. Site map

Line numbers are at `origin/main` `43c9cf45`.

| Site | Calculator and facts |
|---|---|
| Scenario card/setup (`ScenarioCards.cs`, `ScenarioSetup.cs`, `ScenarioSetupPlans.cs`) | Persist explicit SSR supply authorization, eligible recipient armament and any named vehicle exception. Represent an Ammo Dump as an actual stationary supply source (`DumpImmobile`). Ask `Selection`; do not infer SSR permission from a label or from capacity alone. |
| `GamePlanner.Passengers.cs:27` | Ask `PassengerCapacity` from current supply state. Validate removal through `SupplyRemovalRefusal`, not a freely editable counter. |
| `GamePlanner.TurnWork.cs:44` to `:51` and projector `PrepareWorkEffects` (`GameProjector.WorkEffects.cs:9`, `:21`) | Ask `Selection`, `ReplenishmentRefusal`, `Compatible` and the Accessible member of section 5 on actual identities, complete recipient assignment, CE, TI, capacity and current supply. |
| `GameProjector.TurnWork.cs:36` to `:38` | Today a participant in another open work is refused, which bars a legal second recipient on one supply. Ask the shared-supply verdict of section 5 instead; the Rules verdict decides the two-recipient limit. |
| Replenishment validation (`GameProjector.WorkEffects.cs:56` to `:75`) | Replace `ReplenishmentReady` (:67) with `ReplenishmentRefusal`. |
| `ValidateSupplyRelocation` (`GameProjector.WorkEffects.cs:92` to `:123`) | Replace `RelocationSourceReady` (:97) and `TransferDestinationReady` (:115) with `TransferRefusal`, `Immobile` and `Selection` for the destination. Gather D.7 facts separately, including Mired, Abandoned, broken crew, Shocked and Stunned. Do not reject occupants or equipment; disposition uses the applicable existing passenger/equipment Rules. |
| Work progression and `ASL.Turns/TurnWorkProgress.cs:84`, `:98` to `:102` | Use `ReplenishmentTurns`, `TransferTurns` and the Rules lifecycle boundaries and interruptions (ruling RE10W.2). Both participants remain TI for complete counted Game Turns; the recipient stays CE. Show calculator durations rather than duplicating numeric tables in text. |
| `CompleteWorkEffects` (`GameProjector.WorkEffects.cs:125` to `:158`) | Ask `Restored`, `SupplyExhausted` and the shared-completion verdict (ruling RE10W.1) in place of `TurnWorkEffects.Resolve`'s restoration (:128, :151 to :157). Restore only the affected Low Ammo recipient and its C8.9-depleted types. |
| B12 benefit: `GamePlanner.Ordnance.cs` (the shot's breakdown) and `LiveOrdnance.cs` (its facts) | Ask `BenefitRefusal`, then the breakdown composition of section 5 inside `ScenarioA1VehicleBreakdownResolutionCalculator.Numbers` (the ordnance calculator reads it at `ScenarioA1OrdnanceCalculator.cs:1148`). Track current served recipients and compatibility from actual evidence; the benefit does not mean every nearby vehicle has B12. Bounding (First) Fire is refused by `BenefitRefusal`. |
| PFPh mark and movement: `GamePlanner.Movement.cs`, enforced again in `GameProjector.cs` | `BenefitBarsMovement` marks the supply vehicle with the existing `Conditions.PrepFire` (E10.21); `MovementBarred` bars MP in the turn it becomes or ceases to be an Ammo Vehicle (E10.4). A posted mark is enforced by both the command and the projector. |
| Immobilization: `GamePlanner.Fire.cs:1454`, `:1550`; `GamePlanner.Ordnance.cs` (the Immobilization TC, :1306); `GamePlanner.VehicleCloseCombat.cs:689` to `:692`; `GamePlanner.MotionAttempts.cs:61` with `GameProjector.cs:3428`; target facts in `LiveFire.cs:698`, `LiveOrdnance.cs:636`, `LiveCloseCombat.cs:288` | Ask `KeepsSupplyingWhenImmobilized` where an unarmored Ammo Vehicle becomes Immobilized: it is marked Immobilized and keeps its supply, never flipped to its wreck side. The Live readers gather the Ammo Vehicle fact into the target facts the verdicts read. |
| Burning wreck: `GamePlanner.Fire.cs:1355`, `GamePlanner.Ordnance.cs:1275`, `GamePlanner.VehicleCloseCombat.cs:672`; projector `Wreck` (`GameProjector.cs:1789`) | Ask `Explosion` and the structured resolution of section 5 when an Ammo Vehicle becomes, or would become, a Burning Wreck. |
| Transfer wreck: `GameProjector.WorkEffects.cs:138` | `WreckAfterTransfer`, as today. |
| Workbench, work disclosure (`ASL.Turns/TurnWorkDisclosure.cs`) and history | Gather only facts available to the authorized viewer. Keep hidden identities/armament and incomplete recipient sets unknown. The display of a supply counter is not permission to inspect its whole stack. |

## 4. State, events and old records

Use the existing supply/work journals as the source of truth. Keep assignments keyed by supply and actual recipients with the Rules-defined time window; recompute reach and availability from the current revision. A cached old assignment is not perpetual eligibility.

**The new record shape.** A governed E10 work is written as a new event type, `turn-work-governed` (`TurnWorkRecorded.GovernedType`), read as `HostAuditVersion` 2 with the package `asl.turn-work-effects.v2`. Old records never carry that type, so the version needs no field. The fields it adds:

| Record | New field | Meaning |
|---|---|---|
| `TurnWorkEffectSpec` | `AccessBasis` (string?) | "same" or "accessible": the Accessible verdict's basis at declaration, rechecked at completion. |
| `TurnWorkRecorded` | `SharedCompletion` (string[]?) | On a completion: the other work ids completed at the same boundary under RE10W.1, in the order their DRs were made. |
| `TurnWorkRecorded` | `RestoredTypes` (string[]?) | On a completion: the Ammo Types `Restored` gave back. |
| `OrdnanceShot` | `AmmoSupplyId` (string?) | The supply whose E10.2 benefit made the shot's B# a B12. |
| new event `ammo-supply-assigned` | `AmmoSupplyAssigned(SupplyId, Vehicle, Owner, Authorization, CaliberMm, Barrel, DumpLocation?)` | The card's SSR supply at setup, with the armament it fits. A new event type, so no old record changes. |
| `TurnAmmoSupply` (state) | `Authorization`, `CaliberMm`, `Barrel` (nullable), `ChangedTurn` (int?) | Read from the event above; `ChangedTurn` is the turn it became or ceased to be an Ammo Vehicle, for `MovementBarred`. Null for a supply of an older record. |

Each new field on a recorded record follows CONVENTIONS.md rule 6 as amended (the user, 2026-10-30): a nullable property omitted from the JSON when null, and null is the reading before it, so a record written before the field serializes as it did. `GameEventWriter` writes `TurnWorkRecorded` with `JsonSerializerOptions.Web` (`GameEventWriter.cs:114`), which writes nulls, so each new property carries `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`, and `OrdnanceShot`'s new field likewise. Unknown versions or forged supply/participant facts refuse. The E10.21 PFPh mark uses the existing PrepFire condition; it adds no field.

**Old records and the old members.** The W-E10 row names the old members to remove once callers move: `TurnWorkEffects.CanRelocateSupply`, `ReplenishmentReady`, `RelocationSourceReady`, `TransferDestinationReady`, the Replenish arm of `TurnWorkEffects.Resolve` (its restoration of every depleted type), and the `TurnAmmoEligibilityRules` class. Today those members are the reading of an older work record with completion effects (`GameProjector.WorkEffects.cs:60`, `:67`, `:97`, `:115`, `:128`). Read on 2026-10-31: no recorded game holds any work event at all. None of the 117 games of the proof store (`asl-gate-work/store`), none of the live boards folder, and none of Codex's working folder contains `turn-work-recorded`, `turn-work-audited` or `turn-work-effects-recorded`. So no recorded reading needs the old members, and wire-work-1 removes them, with their legacy assertions (`TurnAmmoEligibilityTests`, `ScenarioA1AmmoVehicleCalculatorTests.cs:26`, `:42`, `:150`). The wiring's own merge gate proves it: no store game may differ. An `asl.turn-work-effects.v1` record carrying E10 completion effects that turns up later is refused with a named diagnostic ("an E10 work effect recorded before W-E10 has no reading"); it is not silently read under the new rules. Lifecycle-only records (versions 0 and 1 with no completion effects) and G10 packing effects keep their reading unchanged. Should such a game appear before the build, the old members instead move into Rules as a version-selected legacy reading (the precedent of C9.3's `SpotterUseVersion`) until no recorded reading needs them.

Confirmations recheck revision, owner, source, both recipient slots, CE, TI, location and available supply. Refusals, cancellation and stale requests produce no dice and no events. A completion revalidates the current state and uses the same Rules lifecycle as the proposal. Never infer credited turns from elapsed event count or phase names alone (ruling RE10W.2).

Removal, transfer and replenishment are atomic logical commands. If a completion both relocates supply and wrecks its original vehicle, validate the entire result before publishing events. Do not consume supply, restore ammo, clear TI or mark movement on a rejected command. When two recipients finish at one boundary, the shared-completion verdict (ruling RE10W.1) gives both completions and any exhaustion before the event order is chosen; the first serialized DR of 12 cannot cancel the second completion. A completed recipient does not clear the supply's TI while another valid replenishment still requires it.

## 5. Explosion and Rules prerequisites

These are the rule claim `e10-supply-adapters` (section 2, step 1). Backlog 85 tracks them until it merges.

- **Explosion.** `AmmoExplosion` describes important effects in prose as well as fields (`ScenarioA1AmmoVehicleCalculator.cs:229` returns the positionings as strings). Do not parse its text or implement Goliath or DC outcomes in Play. The claim adds a structured composition with the existing DC/fire/armor resolution: one shared Effects DR, source level, no designated target or X#, same-hex and adjacent units, weakest rear AF Successfully Positioned in its hex, Poorly Positioned adjacent, and Fortification and minefield effects in its hex only (Vehicle Note 93, p. 348). Maps supplies affected Locations; Rules selects legal effects; Play records their results; projection verifies them.
- **The B12 composition.** A versioned D3.71/E10.2 number composition inside `ScenarioA1VehicleBreakdownResolutionCalculator.Numbers`, taking the benefit as a fact and giving `BreakdownNumber`'s answer (RE10.11). Old resolvers keep their entry points.
- **Accessible.** `SameOrAccessible(same, adjacent, normalInfantryAdvanceIgnoringEnemy)` (:43) combines supplied facts; it does not decide whether Infantry could advance. The claim adds a member that takes terrain, levels and connections and asks the advance rules (`ScenarioA1AdvanceCalculator`) for the hypothetical advance; adjacency alone is insufficient.
- **The shared-supply lifecycle.** The current work lifecycle handles one work at a time. The claim adds, in `ScenarioA1AmmoVehicleCalculator`, a verdict across the works that share one supply: shared TI ownership, interruption of the supply's TI and its effect on each work, the two-recipient limit, and completion and exhaustion at one boundary (RE10W.1). `TurnWorkLifecycle` calls it at its edge. Do not write these decisions in the projector.
- **Contract corrections.** The A12.16 inspection wording of the E10 contracts and the `<=9` comment (backlog 85, third row).

Do not claim W-E10 complete with burning supplies merely removed, second recipients disabled, Ammo Dumps missing, or an external adjudication standing in for a governed command. Each named behavior in W-E10 must be exercised or the row remains claimed.

## 6. Player controls and disclosure

Use the existing proposal/confirmation flow for supply assignment, replenishment, transfer and offload. Choices show the counter's hex and level and use the same calculator refusal as confirmation. Show both current recipients and progress as permitted by the viewer's inspection rights. Do not display private rejection reasons, identifiers or armament to an opponent. Clear selections and retained facts when state or viewer changes. A Workbench hand-entered fact remains labeled entered by hand and does not authorize a game action.

The required UI checks cover owner, opponent with concealed supply, opponent with unconcealed-but-uninspectable supply, adjudicator, handover, keyboard selection, cancellation and stale confirmation. Propose the final whole-pass UI scope before running it under the standing rules. Port 6680 is Codex's Studio.

## 7. Acceptance and replay

Play the E10 minigame through SSR assignment, valid/invalid supply selection, two matching recipients and a third refusal, incompatible armament, same/Accessible/refused Locations, CE and TI loss, movement/fire/CC interruption, complete Game Turn credit, restored special ammo, supply exhaustion, four-turn transfer/offload, Ammo Dump, immobilized unarmored source and explosion. The B12 sequence starts from the D3.7 Low Ammo fixture; test AFPh use, Bounding Fire refusal, PFPh movement restriction and becoming/ceasing-supply movement bars.

Then add focused command/projector tests for those boundaries and disclosure, plus tampered evidence and all legacy work/fire versions. Shared-supply cases include both recipients due with the first Ammunition DR of 12, one due while the other continues, one recipient losing CE, and supply movement; their expected outcomes come from RE10W.1 and RE10W.2. The explosion fixture proves one shared roll and no duplicate effects. Replay the played games from their bundle. Run this claim's own strict build, replay/sweep gate and CI.

**The behavior changes,** one by one against the W-E10 row, so the gate can name each difference:

1. Replenishment and transfer across an Accessible Location, not only the same Location.
2. Two recipients on one supply at once (the open-work bar at `GameProjector.TurnWork.cs:36` no longer bars the second).
3. Replenishment refused for an incompatible or non-TI pair.
4. Immobile read by D.7: a Mired, Abandoned, Shocked or Stunned vehicle's supply, or one whose crew is broken, may move.
5. A transfer source with occupants or equipment no longer refused.
6. Only a Low Ammo vehicle's C8.9-depleted ammunition restored.
7. The new movement bars: E10.21 after a PFPh benefit, E10.4 in the turn of becoming or ceasing to be an Ammo Vehicle.
8. The explosion of a burning Ammo Vehicle, and an unarmored Immobilized one kept in play.
9. The B12 benefit on a recipient's shot (D3.71 with E10.2).
10. An E10 work effect recorded under `asl.turn-work-effects.v1` refused (section 4); no recorded game holds one.

Old recorded games retain their readings. Update the inventory to wired only when every site and prerequisite passes; Claude reviews and the user separately authorizes merge.

## 8. Memory, performance and build outputs

Use immutable state-scoped indexes for supplies and participants only where existing reads do not suffice. Gather each command's participants once and use current state/map context for Locations and geometry. If an index is added, its owner is `PlayStepMemo`, lifetime is the state object, key includes the complete supply/recipient facts, values are immutable, missing results are retained for that state only, and concurrent fills have no effects. Viewer-specific projections additionally key by viewer. No per-option full-history scan, global cache or serialized derived cache.

The benefit adds one lookup per fire option of a B# vehicle: the served-recipient lookup, which scans `GameState.TurnAmmoSupplies` (a list of the few supplies in play, usually none) for an available supply of the firer's side whose armament matches, then reads the supply's and the firer's Locations through `GameState.Location` and the step's existing map context. It reads no history and runs only when the firer has a B#; a game without an Ammo Supply pays one empty-list check per option. If a turn shows the lookup repeated across many options, it moves into `PlayStepMemo` under the rules above.

Measure proposal/projection allocations and time for one and two recipients plus a representative explosion, with identical before/after game revisions, boards and viewers and no concurrent builds. Count repeated participant/map reads; investigate regressions. Reuse unchanged counter fragments and patch only affected Locations. Reuse expensive immutable test setup once per run; no new test project is planned. Record limitations and the chosen fixture workload with results, not a speculative speed claim.

Set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` for every `code-builders` call. Register Studio/test/gate tool outputs at creation and remove disposable outputs after green CI. Preserve game stores, logs, results and played proofs. Verify paths stay inside Codex's working folder and use native PowerShell for Windows cleanup. The final task checks an empty output registry.

## 9. Rulings

- **RE10W.1 Two recipients completing at one boundary (E10.3, p. 244).** E10.3 orders it: "Upon the successful completion of each vehicle's Replenishment period, the Ammo Vehicle must make an Ammunition DR." Completion comes first. Both recipients completing at the same CCPh end are both Replenished. The DRs are made in the works' declaration order. An Original 12 on the first exhausts the supply after both completions; the second DR is then moot and not rolled. An Original 12 on the second exhausts it likewise. The order changes no outcome. The rule claim's shared-completion member returns both completions and the exhaustion as one verdict; the projector records them in that order.
- **RE10W.2 Credit after an interrupted Replenishment (E10.3, p. 244).** E10.3's exception: "that Game Turn does not count toward Replenishment." Read literally, only the Game Turn in which the TI counter is removed is lost; complete Game Turns already counted stay credited when both vehicles are again TI in the same or an Accessible Location for the same work. This is today's answer (`TurnWorkLifecycle.Change`, "interrupt", keeps `CompletedGameTurns`; `NextGameTurn` credits only consecutive boundaries), and `TurnWorkLifecycle` in Rules keeps deciding it; `TurnWorkProgress.cs:100` states it as "prior completed intervals remain". A reading open to the user's veto.

## 10. Revisions of 2026-10-31

Claude's review of PR 69 (2026-10-30, ten findings) is answered in these sections:

- **Status line:** kept; one line added under it (finding 7; the user confirmed the approval to Claude on 2026-10-30).
- **Section 1:** D3.7 and W-C9 are on main (PRs 68 and 85); the B12 composition sits in the D3.7 resolution calculator.
- **Section 2 (new):** the order of work: the Rules prerequisites as the rule claim `e10-supply-adapters`, merged before wire-work-1 builds, with its held files (finding 3); the files wire-work-1 must add by a claim commit (finding 4).
- **Section 3 (was 2):** every site named with its line at main and the calculator member it asks, including the movement, immobilization, wreck and fire sites (finding 4).
- **Section 4 (was 3):** the new record shape (finding 5); rule 6 as amended (finding 8); the old members, the recorded games that hold none, and their removal (finding 1); RE10W.1 and RE10W.2 referenced (findings 2, 9).
- **Section 5 (was 4):** the prerequisites as the rule claim's scope (finding 3).
- **Section 7 (was 6):** the behavior changes one by one (finding 10).
- **Section 8 (was 7):** the served-recipient lookup per fire option (finding 10); the sentence that the design needs no CI dropped (finding 6).
- **Section 9 (new):** rulings RE10W.1 (finding 2) and RE10W.2 (finding 9).
- **Backlog section 88 (new):** the DYO purchase of an Ammo Vehicle or Ammo Dump (finding 10).

Section 6 is unchanged.
