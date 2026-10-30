# Wiring 002: ammunition supply

**Status:** Proposed, awaiting the user's approval. Claim `wire-work-1`, Codex, reviewer Claude, branch `codex/wire-work-1`, taking W-E10 in inventory commit `49491cce`. Implementation follows `wire-fire-3`'s D3.7 work. This design includes no implementation.

## 1. Dependencies and the reviewed rule

D3.7 Low Ammo, X# and versioned fire outcomes must reach main before E10's B12 benefit is enabled. The user explicitly ordered that dependency in Wiring Prompt 002. Merge the fire branch's result from main before editing shared ordnance/projector files. W-C9 remains claimed; do not work on its shared files concurrently. The inventory, not this design, is the ownership ledger.

Read the approved [E10 rulings](<ASL E10 Ammo Vehicles Claim Design.md>): physical PDF p. 244, D.7 p. 192, D3.71 pp. 201 and 202, Vehicle Note 93 p. 348 and ASOP 8.43B p. 681. The Codex read-only review found no blocking calculation defect within approved RE10.6 and RE10.11. It corrected the design's statement about the E10.3 table (it prints <=9) and identified the need to correct the contract's incomplete inspection wording. A12.16, p. 79, requires inspection authority even for an unconcealed enemy stack. Its facts cannot be read from full state just because it has no concealment counter.

The existing work route is an adjudication route, not a complete governed E10 command. Do not relabel a hand-entered success as automated legality. Build a revision-bound proposal that gathers the supported facts, asks the calculator, and records verified evidence. Rules adapter gaps below remain explicit prerequisites, not a reduced definition of done.

## 2. Site map

| Site | Calculator and facts |
|---|---|
| Scenario card/setup | Persist explicit SSR supply authorization, eligible recipient armament and any named vehicle exception. Represent an Ammo Dump as an actual stationary supply source. Ask `Selection`; do not infer SSR permission from a label or from capacity alone. |
| `GamePlanner.Passengers` | Ask `PassengerCapacity` from current supply state. Validate removal through `SupplyRemovalRefusal`, not a freely editable counter. |
| Planner work proposal and projector `PrepareWorkEffects` | Ask `Selection`, `ReplenishmentRefusal`, `Compatible` and `SameOrAccessible` on actual identities, complete recipient assignment, CE, TI, capacity and current supply. No one-work-per-participant shortcut may bar a legal second recipient. The Rules verdict decides the two-recipient limit. |
| `ValidateSupplyRelocation` | Ask `Immobile`, `TransferRefusal` and `Selection` for the destination. Gather D.7 facts separately, including Mired, Abandoned, broken crew, Shocked and Stunned. Do not reject occupants/equipment by an invented source restriction; disposition uses the applicable existing passenger/equipment Rules. |
| Work progression and `ASL.Turns/TurnWorkProgress` | Use `ReplenishmentTurns`, `TransferTurns` and existing Rules lifecycle boundaries/interruptions. Both participants remain TI for complete counted Game Turns; the recipient stays CE. Show calculator durations rather than duplicating numeric tables in text. |
| `CompleteWorkEffects` | Ask `Restored` and `SupplyExhausted`. Restore only the affected Low Ammo recipient and eligible normally depleted ammo types. Each completed recipient requires its own Ammunition DR, even if the same supply completes two works at this boundary. Rules must decide simultaneous completion and exhaustion before event ordering is chosen. |
| D3.7 fire adapter | Ask `BenefitRefusal`, then compose `BreakdownNumber` in Rules. Track current served recipients and compatibility from actual evidence; the benefit does not mean every nearby vehicle has B12. Use `BenefitBarsMovement` to record the supply's PFPh mark and subsequent movement restriction. |
| Movement, immobilization and wreck paths | Ask `MovementBarred`, `KeepsSupplyingWhenImmobilized`, `WreckAfterTransfer` and `Explosion` at the transition that triggers them. A posted mark must be enforced by both the command and projector. |
| Workbench, work disclosure and history | Gather only facts available to the authorized viewer. Keep hidden identities/armament and incomplete recipient sets unknown. The display of a supply counter is not permission to inspect its whole stack. |

## 3. State, events and old records

Use the existing supply/work journals as the source of truth. Add the minimum structured evidence needed for source authorization, actual participants, current benefit assignments, armament, full Locations and movement restrictions. Keep assignments keyed by supply and actual recipients with the Rules-defined time window; recompute reach/availability from the current revision. A cached old assignment is not perpetual eligibility.

New E10 commands and work records carry an explicit new reading/version. Existing work audit versions retain their old interpretation; they are not silently promoted when loaded. Existing restrictions on continuing legacy work remain unless an approved migration explicitly preserves its evidence and progress. New declaration, reader, verifier, planner and projector agree on version. Unknown versions or forged supply/participant facts refuse. Freeze existing `OrdnanceShot` and other recorded fact records; add optional event evidence through the D3.7 adapter instead.

Confirmations recheck revision, owner, source, both recipient slots, CE, TI, location and available supply. Refusals, cancellation and stale requests produce no dice and no events. A completion revalidates the current state and uses the same Rules lifecycle as the proposal. Treat an interrupted Game Turn as incomplete; never infer credited turns from elapsed event count or phase names alone.

Removal, transfer and replenishment are atomic logical commands. If a completion both relocates supply and wrecks its original vehicle, validate the entire result before publishing events. Do not consume supply, restore ammo, clear TI or mark movement on a rejected command. When two recipients finish at one boundary, obtain a Rules verdict for both completions and any exhaustion before choosing the deterministic event order. Do not let the first serialized DR of 12 cancel an already-earned second completion by accident. This needs an explicit Rules ruling against E10.3, p. 244. A completed recipient must not clear the supply's TI while another valid replenishment still requires it.

## 4. Explosion and Rules prerequisites

`AmmoExplosion` describes important effects in prose as well as fields. Do not parse its text or implement Goliath/DC outcomes in Play. Establish scoped Rules support for a structured composition with the existing DC/fire/armor resolution where it cannot already express these facts: one shared Effects DR, source level, no designated target or X#, same-hex and adjacent units, weakest rear AF/positioning, and same-hex-only fortification and minefield effects. Maps supplies affected Locations; Rules selects legal effects; Play records their results; projection verifies them.

The Rules work also needs the versioned D3.7/E10 number composition, an accessible-location eligibility adapter, and complete shared-supply work lifecycle/participant validation. `SameOrAccessible` combines supplied facts; it does not decide hypothetical Infantry advance eligibility. Gather terrain, levels and connections and ask Rules; adjacency alone is insufficient. The current work lifecycle handles one work at a time. Shared TI ownership, interruptions and exhaustion propagation need a Rules verdict across the participating works. Do not write these decisions in the projector. Add Rules file holds and a separate scoped design before modifying those adapters. Backlog 85 tracks this work and the contract summary corrections.

Do not claim W-E10 complete with burning supplies merely removed, second recipients disabled, Ammo Dumps missing, or an external adjudication standing in for a governed command. Each named behavior in W-E10 must be exercised or the row remains claimed.

## 5. Player controls and disclosure

Use the existing proposal/confirmation flow for supply assignment, replenishment, transfer and offload. Choices show the counter's hex and level and use the same calculator refusal as confirmation. Show both current recipients and progress as permitted by the viewer's inspection rights. Do not display private rejection reasons, identifiers or armament to an opponent. Clear selections and retained facts when state or viewer changes. A Workbench hand-entered fact remains labeled entered by hand and does not authorize a game action.

The required UI checks cover owner, opponent with concealed supply, opponent with unconcealed-but-uninspectable supply, adjudicator, handover, keyboard selection, cancellation and stale confirmation. Propose the final whole-pass UI scope before running it under the standing rules. Port 6680 is Codex's Studio.

## 6. Acceptance and replay

Play the E10 minigame through SSR assignment, valid/invalid supply selection, two matching recipients and a third refusal, incompatible armament, same/Accessible/refused Locations, CE and TI loss, movement/fire/CC interruption, complete Game Turn credit, restored special ammo, supply exhaustion, four-turn transfer/offload, Ammo Dump, immobilized unarmored source and explosion. The B12 sequence starts from the D3.7 Low Ammo fixture; test AFPh use, Bounding Fire refusal, PFPh movement restriction and becoming/ceasing-supply movement bars.

Then add focused command/projector tests for those boundaries and disclosure, plus tampered evidence and all legacy work/fire versions. Shared-supply cases include both recipients due with the first Ammunition DR of 12, one due while the other continues, one recipient losing CE, and supply movement. Their expected outcomes come from the approved shared-work Rules ruling. The explosion fixture proves one shared roll and no duplicate effects. Replay the played games from their bundle. Run this claim's own strict build, replay/sweep gate and CI, naming every difference as a W-E10 behavior. Old recorded games retain their readings. Update inventory to wired only when every site and prerequisite passes; Claude reviews and the user separately authorizes merge.

## 7. Memory, performance and build outputs

Use immutable state-scoped indexes for supplies and participants only where existing reads do not suffice. Gather each command's participants once and use current state/map context for Locations and geometry. If an index is added, its owner is `PlayStepMemo`, lifetime is the state object, key includes the complete supply/recipient facts, values are immutable, missing results are retained for that state only, and concurrent fills have no effects. Viewer-specific projections additionally key by viewer. No per-option full-history scan, global cache or serialized derived cache.

Measure proposal/projection allocations and time for one and two recipients plus a representative explosion, with identical before/after game revisions, boards and viewers and no concurrent builds. Count repeated participant/map reads; investigate regressions. Reuse unchanged counter fragments and patch only affected Locations. Reuse expensive immutable test setup once per run; no new test project is planned. Record limitations and the chosen fixture workload with results, not a speculative speed claim.

Set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` for every `code-builders` call. Register Studio/test/gate tool outputs at creation and remove disposable outputs after green CI. Preserve game stores, logs, results and played proofs. Verify paths stay inside Codex's working folder and use native PowerShell for Windows cleanup. The final task checks an empty output registry. Design preparation creates no build outputs and needs no CI.
