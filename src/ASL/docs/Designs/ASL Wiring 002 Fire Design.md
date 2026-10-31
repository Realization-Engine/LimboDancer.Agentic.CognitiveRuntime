# Wiring 002: vehicle breakdown and SMOKE strength

**Status:** Approved by the user on 2026-10-29. Claim `wire-fire-3`, Codex, reviewer Claude, branch `codex/wire-fire-3`. Inventory claim commit `49491cce` takes W-D3.7 and W-A24.5 together as the open fire capability. The three Rules reviews are recorded in their claim designs and on main in `4c573630`. Implementation is authorized in the dependency order below; code merge remains separately authorized.

## 1. Dependencies and implementation order

The work that is not yet available comes first: D3.7's calculator exists, but the fire resolution adapters cannot yet carry its Low Ammo and Disabled results; SA repair is explicitly unsupported by its repair entry point. Those are Rules dependencies, not permission to decide their rules in Play. Complete the scoped Rules support below before enabling the corresponding cases. Do not mark W-D3.7 wired with these cases omitted.

Implement D3.7 before E10's B12 benefit, as the user directed in Wiring Prompt 002. This branch goes before `wire-work-1`; that branch merges this result from main before touching its fire paths. W-C9 remains claimed as `wire-fire-2`, with its existing approved participation/hero prerequisites. Do not edit shared ordnance files concurrently: finish this branch's changes before resuming W-C9, and merge current main into each branch before work. Numbered claims identify ownership, not a promise to implement blocked work first.

Claude's B25.2 claim is active. W-A24.5 must preserve its legacy Blaze path until the B25.2 calculator and wiring scope are ready. Re-read its merged contract before implementation; claiming B25.2 alone is not enough to replace a live path. If its wiring becomes open during this work, take it only through a separate inventory amendment and design approval. Publish branches early and run `claim_check.py --touched` and the branch overlap check before edits and before the PR.

## 2. Rules and the completed review

Use the approved [D3.7 rulings](<ASL D3.7 Vehicle Breakdown Claim Design.md>) and [A24.5 rulings](<ASL A24.5 Smoke Strength Claim Design.md>). Physical PDF references: D3.7/D3.71 pp. 201 and 202; A.11 p. 43; A19.131 p. 86; G10.41 p. 292; A24.11/A24.2 p. 91, A24.5/A24.61 pp. 92 and 93, A24.8 p. 93, D13.31 to D13.35 p. 212. The independent read-only reviews found no blocking calculation defect within the approved readings.

A12.16, p. 79, also restricts inspection of an unconcealed enemy stack outside LOS of the viewer's Good Order ground units. Use the authorized inspection view, not a test for concealment alone. The D3.7 contract's A.11 citation and its inspection summary need correction in scoped Rules maintenance. Contract display labels are not persisted facts: a grenade's outcome label `full` does not mean `TurnSmokeStrength.Full`; its non-dispersible strength fact is `Unknown`.

## 3. D3.7 site map and recording

| Site | Change and boundary |
|---|---|
| Catalog definitions and pinned data | Represent printed B#, printed X#, circled B#, Vehicle Note extensions and explicit SA identity separately. A missing printed number is not the same as a number hidden from the viewer. Preserve pinned historical definitions; new source-backed values require reviewed provenance and the normal catalog publication path. Add any further catalog/manufacturing file to the inventory before changing it. |
| `LiveOrdnance`, `LiveFire`, planner fire/ordnance/overrun paths | Gather the actual weapon, printed facts, Low Ammo, capture/weather/shortage facts and roll purpose. Ask a Rules-owned versioned resolution adapter, which calls `Numbers` and `Outcome`. Play never selects lowerings or tests breakdown thresholds. |
| Existing Rules resolution sites in the W-D3.7 row | The ordnance, armor, canister, area and vehicle-MG/OVR paths all need the adapter. A9.71 still selects which firing MG is affected; D3.7 decides that weapon's outcome. Qualifying IFT use and CC's Ammunition Shortage exclusion are Rules decisions. A target's unrelated IFT roll must not become a second weapon breakdown test. |
| `GamePlanner.Rally`, `RepairActionPanel`, projector repair | One fact gatherer for the selected real weapon and repair participant. Command and list ask `RepairRefusal`; resolution asks `RepairResult`. Gather the actual Hero Rider and his eligibility, separate AAMG Disabled state, and per-weapon attempts. Use existing Rules phase/crew checks; do not create a new phase or Hero qualification rule in the page. |
| Fire follow-ups and projector | Apply the returned Low Ammo, Malfunction or Disabled effect to the actual weapon. Ask `Recalled` after both disabling fire and failed repair, using every explicitly listed SA's state and Passenger/Towing capability. Unknown SA state stays unknown. An MG is not invented as SA merely because the vehicle has one. |
| Contract facts and history | Reuse the same snapshot facts for the owner; give other viewers only inspectable or action-verification facts. Hidden numbers must stop evaluation, not silently become B12. Show new outcomes through the established counter/history paths. |

Rule 6, as amended by the user on 2026-10-30, permits a new nullable property on any recorded record, including `OrdnanceShot`, omitted from JSON when null. Null means the reading before the property existed. Versioned evidence records explicit weapon identity and the facts necessary for re-verification. Do not change the meaning of `SpotterWiringVersion`. New writer, reader, verifier, planner and projector must agree; absent evidence selects the recorded legacy reading, unsupported versions refuse, and forged or incomplete evidence is rejected before state changes.

New resolution returns a structured result carrying the weapon outcome, including Low Ammo and Disabled, alongside the existing shot/fire result. Projection recomputes from the recorded revision and Rules adapter, not from a trusted client outcome. Old public resolver entry points keep their answers. Legacy repair and fire support may be removed only when no recorded reading needs it; otherwise retain an explicitly named compatibility path and record that retention, rather than deleting replay support to satisfy the removal column.

## 4. Rules prerequisites and scope

The wiring claim holds outside-Rules files and the explicitly named obsolete members only. Before changing a Rules adapter, establish a scoped Rules claim in the inventory with Claude as reviewer, compatible shared holds, its own design and tests. Required support:

- Versioned adapters for the W-D3.7 fire sites and structured weapon effects, keeping all old signatures/results compatible.
- Rules-owned interpretation of the attack kind, qualifying Gun IFT use, CC shortage exclusion, and A.11 lowering facts.
- SA repair identity, refusal and once-per-RPh handling, plus actual Hero Rider qualification where existing Rules cannot express it.
- An E10 composition point that applies `BreakdownNumber` to D3.7's numbers in Rules, reserved for the later supply branch. This branch does not enable the supply benefit.

Backlog 85 records these dependencies and the contract citation/disclosure corrections. They are not new rule interpretations. Any new interpretation requires review against the PDF before code. Catalog sources must be read before adding real counter values; synthetic test definitions must remain labeled synthetic.

## 5. A24.5 source gathering and Locations

Replace the count-only `SmokeSources` read with snapshot facts containing the complete `BoardLocation` and authoritative source, chemical and strength. Existing grenade entities derive those facts from their actual creation evidence. Do not guess from a display name or treat every `asl:smoke` entity as a grenade. Missing new-source facts refuse evaluation or remain unsupported; they never become a zero hindrance.

`GamePlanner.Wrecks.VehicleHindrance`, the ordnance own-Location hindrance, sniper TEM, residual-fire map facts and overrun smoke ask `Hindrance` for each supported source and pass the values to the existing by-value Rules overloads. Combine grenade and applicable Blaze values in one Location before applying the common cap and outgoing modifier. Preserve B25.2's recorded count reading until its calculator is wired; never add two separately capped totals. Retain lazy crossed-LOS evaluation where the existing overload has it.

Board, hex and level all belong in the grouping key. Whether a line crosses a source's vertical extent needs the existing Maps geometry and the relevant height Rules; grouping alone does not establish intersection. Do not introduce smoke-height arithmetic into Play. W-A24.5 corrects the current merging of distinct Locations. It does not authorize new ordnance, OBA, WP, dispenser or drift placement controls, which require their placement and timing claims. Pass 85 and the B25.2 row continue to record those missing capabilities.

Version the changed fact reading for new fire records and preserve old serialized facts/verifiers. `TurnWhiteSmokeRules.InitialStrength` and count overloads are removed only after all production and compatibility callers are accounted for. Tests that establish the legacy result remain with the compatibility path.

Implementation checkpoint, 2026-10-30: residual fire, ordnance's own-Location hindrance, OVR, concealment gain and Sniper target TEM gather occupying SMOKE values through the same height-aware reader. It caches the self-LOS endpoint read within one Location query and combines all occupying values before the Rules cap. Concealment asks lazily only for relevant candidates and refuses missing facts before committing. Blaze keeps its prior reading until W-B25.2.

New Sniper continuations and queued checks carry nullable `smokeVersion`; absence preserves the old exact-Location count through subsequent pauses, LLMC attacks and Sniper Checks. Unsupported versions refuse before dice. The pre-roll availability check uses public map Locations rather than hidden target eligibility; its conservative handling of unknown sources is recorded in backlog section 85. The strict Studio build passed with zero warnings and errors. Ten focused cases passed for occupancy boundaries, missing height facts, current and legacy target selection, repeated pauses, current/legacy/mixed queued checks and unsupported-version refusal. Logs: `build-fire3-smoke-latest.log` and `Fire3-Smoke-Occupancy-Sniper.log` under Codex's gate working folder. Replay gates and full acceptance are still pending.

Studio acceptance, 2026-10-30: `wire-smoke-fire` was generated as a fresh board 1 position with an explicit German Scenario Defender. The squad placed a grenade at [B8] on a dr of 1; Russian Defensive First Fire from [B10] displayed Hindrance 2 before dice and recorded the same value with `smokeWiringVersion: 1`. The scripted 6,6 attack had no effect, with +2 LOS Hindrance and -1 FFNAM, and no FFMO. The played game is saved in the A24 bundle. The earlier board 2 placement-only record was retained: attempting fire across its wall is refused because it names no Scenario Defender. No played history was rewritten to remove that refusal.

## 6. Commands, UI and disclosure

Use the existing repair panel and fire controls. Every offered counter includes its hex and level through shared option components. Repair weapon choices include actual AAMG and SA identities when supported. A separate Hero participant choice is required when he repairs; an adjacent Hero or a directing leader is not automatically the participant. Candidate lists and confirmation use the same refusal.

A proposal is read-only. Confirmation checks revision, side, weapon identity, participant and verdict again. Cancellation, stale confirmation and refusal consume no dice and write nothing. Reset page choices on state/view changes. An opponent's hidden or uninspectable equipment, numbers, attempts and refusal detail must not escape through options, history, Workbench facts, counters or summaries. Verify both concealed and unconcealed-uninspectable cases and handover.

## 7. Acceptance and performance

First build the Studio and play small cases on port 6680. Then run focused methods for: IS-3 Low Ammo/Disabled sequence; printed X#; shortage/capture/winter/Intensive Fire combinations; MA, MG, SA and AAMG effects; A9.71 selection; all repair refusals and outcomes; Hero Rider; Recall with partial/all/unknown SA and Passenger/Towing exceptions; mixed source Location cap, different levels, outgoing/crossed LOS, unknown smoke metadata and unchanged grenade values. Test old and new evidence, forgery, cancellation, staleness, per-weapon attempts and both opponent inspection boundaries.

Keep played acceptance games with their minigame bundles. Before the whole UI check, state the changed controls and confirm the check scope under the standing rules. Run this claim's own strict build, merge gate and CI, describing every replay/sweep difference by W-D3.7 or W-A24.5. Older recorded games must keep their readings; a changed old replay is a fault. New planner sweep changes must be explained explicitly. Claude reviews the wiring; the user separately authorizes merge.

Gather definitions and weapon states once per command snapshot, and smoke sources once per immutable state when repeated LOS queries require them. Prefer existing `PlayStepMemo` and map context. If a smoke index is retained, it belongs to the state object, keys by full Location, contains only viewer-independent physical facts, is immutable, and permits equivalent concurrent fills without side effects. Viewer projections remain keyed by state and viewer and discard stale selections. No process-wide cache or repeated per-candidate history scan. Counter changes use existing incremental fragments, not full-layer regeneration.

Measure added per-shot/per-repair allocations and repeated smoke queries on the same before/after revision sets, with identical boards and views and no concurrent builds. Compare answer counts and timings; investigate unexplained regressions. Tests reuse expensive immutable fixtures once per run, and no new test project is planned. The design makes no performance-gain claim.

## 8. Build outputs outside the repository

Set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` before every `code-builders` tool. Register Studio 6680 output and any focused-test output when created; the gate registers its baseline and proof-tool builds. Keep proof stores, results, logs and played games. Remove disposable registered outputs after CI, using verified Codex-only paths and native PowerShell cleanup on Windows; do not touch Claude's folders. The last implementation task confirms the output registry is empty. This design/review stage creates no build outputs and runs no CI.

## 9. Wiring gate checkpoint, 2026-10-30

The `wire-fire-3` gate completed `base`, `strict`, `baseline`, `build` and `proofs` against main `ee967f71`, with implementation head `e3e7bbc9`. The strict solution build had zero warnings and errors. The early sweep matched all 10,296 lines; the complete sweep matched all 55,839 lines, with no changed answers or words. No older game's replay differed.

The sole replay difference is the owned `wire-ma-low-ammo` acceptance game, 17 comparison lines: its versioned D3.7 shots introduce Low Ammo and then Disabled and Recall, which the baseline cannot reconstruct under its old weapon-breakdown reading. The other owned records are `wire-vehicle-repair`, `wire-smoke-strength` and `wire-smoke-fire`. The pre-existing `p71-sniper-residual` and `wire-stealth-ambush` records remain NOSTATE on both sides; they are not counted as successful replay positions. The source-text comparison contains 27 changed lines from the new refusals and evidence descriptions.

Evidence is retained under `E:/Archive/GitHub/dlandi/asl-gate-work-codex`: `gate-fire3-strict.log`, `gate-fire3-baseline.log`, `gate-fire3-build.log`, `gate-fire3-proofs.log`, and `before-wire-fire-3`/`after-wire-fire-3`. CI passed all seven checks on implementation branch head `6e03c959` (run `41137914246`). Studio was rebuilt at that head with locked dependencies and zero warnings or errors, then the saved `wire-smoke-fire` result was loaded again, with its +2 LOS Hindrance and final DR 13 visible. The build log is `build-fire3-current-main.log`. This is replay-gate and build evidence, not final acceptance or a performance claim. Performance measurements, remaining focused integration checks, and the other agent's wiring review still precede marking the rows wired.

## 10. PR 68 review corrections

The earlier gate checkpoint does not validate the review corrections. Re-run the gate before requesting merge. A24.4's design records the corrected whole-level calculation, minimum ordering, attributed cellar ruling and unsupported geometries. D3.7's RD3.8 to RD3.12 record Canister, captured shortage, Area pre-shot numbers, Overrun and the user's scoped catalog-evidence decision. New Overrun version 1 malfunctions at B/X; null preserves the prior recorded reading. Recalled and Unconfirmed Kill vehicles retain their CC attack bars.

The source snapshot belongs to breakdown and repair only. New proposals gather from the catalog actually loaded for the state; recorded actions carry their source values so those checks survive a later catalog. Replays without that evidence keep the existing catalog policy. The focused repair test changes a catalog so the BMG disappears: the old recorded repair still replays, while a new repair choice disappears. It also checks exact-catalog evidence tampering, round trips, missing required fields and null omission. Other catalog/rules upgrades are a separate backlog proposal.

`p71-sniper-residual` remains a historical NOSTATE fixture and is not claimed as passing evidence. The replacement evidence is focused live-play tests: `ResidualFpOnAMoveCallsTheSniperOnlyWhenItsDrEqualsTheEnemySan` covers the residual trigger; `SniperSmokeHeightChangesNewChoicesButPreservesLegacyContinuations` plays and replays both versioned height choices and the legacy continuation; `UnknownSniperSmokeVersionIsRefusedBeforeDice` covers unsupported versions. These tests use the production planner, event store and replay verifier, including paused choices, and directly exercise the changed behavior. This follows the reviewer's permitted focused-test alternative; it does not treat an unreplayable fixture as proof.

Removed the unused repair members, initial-strength forwarding member and all five SMOKE count overloads. The null-version Sniper path retains its exact-Location reading by supplying explicit +2 values. Wreck Blaze strength now comes from B25.2's calculator under its existing no-wind reading; Fire5's broader Blaze wiring remains parked.

### Review-fix validation and handoff, 2026-10-30

Implementation head `785190cc` passed the strict build with zero warnings and errors and all seven ASL Authoring CI jobs (run `41163933367`). The final focused runs passed six Rules cases and twelve Play cases, including catalog-evidence replay after a catalog change, legacy fire/repair/CC records, Hero Rider repair, vehicle CC bars and Sniper SMOKE continuation. The earlier five geometry and Overrun cases also passed. No local full test suite was run outside the required gate and CI.

The repeated gate against `ee967f71` completed baseline, build and proofs. All 10,296 early sweep lines and all 55,840 full sweep lines matched. Replay grew from 3,568 to 3,584 lines. The sole differing game is the owned `wire-ma-low-ammo`: revisions 7 to 22 and its final diagnostic make 17 differing comparison keys. The baseline cannot read its new breakdown facts; the current build reconstructs its Low Ammo, Disabled and Recall sequence. No reconstructable older replay changed. The source-text comparison has 31 changed lines, attributable to W-D3.7's evidence/repair/CC refusals and W-A24.4/W-A24.5's SMOKE and Sniper refusals, plus their occurrence counts.

Three records remain incomplete on both sides and are not successful replay evidence: `p71-sniper-residual` stops at revision 2 because [P11] is not on bd03; `village` stops at revision 8 because its two-SMC `participation` property belongs to the later PR 85; `wire-stealth-ambush` stops at revision 14 because its Ambush facts do not match. The focused Sniper tests above are the replacement evidence permitted by the review. Neither later branch was pulled into PR 68 to mask these limitations.

Studio on port 6680 now runs the gate build from this worktree, whose assembly version contains `785190ccc96965174bf95da0152eaef187fcc267`. The `wire-smoke-strength` Play view opened at revision 11. Its Workbench A24.4 contract showed two levels for conventional ordnance SMOKE, four for WP, and not-evaluated for Blaze or unknown chemical. No game events were written in this check.

Evidence is under `E:/Archive/GitHub/dlandi/asl-gate-work-codex`: `gate-fire3-review-{strict,baseline,build,proofs}.log`, `before-wire-fire-3`, `after-wire-fire-3`, and `test-classes/PR68-review-{rules,play}-8a8c8e21.{log,trx}`. Disposable registered build outputs were removed after CI and the proofs; the current Studio runs from its in-worktree build. The results, logs and baseline cache remain.

The review corrections are ready for Claude's separate Rules and wiring review. PR 81 remains open as the Rules review copy until PR 68 merges. The user gives the word for that merge; only then does PR 85 take main and continue the chain. Formal per-action allocation measurements from section 7 are still outstanding, so this report makes no performance or final acceptance claim and does not promote the wiring claim to wired. The proposed upgrade of an old game's copy is recorded separately in the backlog and is not implemented here.

## 11. Final nine-item review completion, 2026-10-31

This report supersedes section 10's outstanding measurement and fixture items. Implementation `db32441e` addresses the final prompt's nine items; Claude's recheck and the user's merge instruction remain separate. No later PR was advanced.

1. `MannedMg` honors both malfunction flags and the unavailable list, including the MA AAMG, with null-list and present-list tests.
2. `CanAttack` and Precheck share the positive firepower calculation, including eligible CE halftrack Passengers. Turn choices gather those same Passengers and retain Captured, Recall and Unconfirmed Kill bars. The old unconditional Passenger fallback is removed.
3. Focused tests cover inherent-crew refusal, two failed CC weapons, missing Random Selection, highest/tied selection, and the 1 through 6 die range.
4. Unknown OVR Disabled weapon names are refused. Permanent disablement is reported before the missing inherent-crew refusal.
5. W-D3.7 names the removed `ScenarioA1VehicleCloseCombatRules.VehicleCcArmament`.
6. RD3.12 and backlog 85 explicitly record the permitted OVR limit: its participating weapon list and printed BMG/CMG FP still depend on the embedded reference. A future reference change must preserve or separately version that composition.
7. Recorded evidence overlays its sixteen scoped front-face fields, including recorded absence, while retaining unrelated definition values. The catalog-change replay test checks this.
8. The allocation probe and raw paired results are in `code-builders/pass-history/wire-fire-3/MEASUREMENT.md` and its `measurements` folder. Two initial pairs found 4,360 bytes per repeated active-Sniper preflight, above the 4 KiB investigation threshold. Reusing the completed public-fact check for the same state and planner reduced that to 40 bytes. Weak keys retain no discarded state or history. Both final pairs pass every unchanged allocation and timing threshold, and all 26 workload answers agree. The largest added helper allocation is 22,971.4 bytes cold, below 32 KiB. Fire adds 600 bytes and repair 39,696 bytes per complete proposal and rolled batch, below 64 KiB. This is not a whole-game or browser speedup claim.
9. The `village` fixture is explained and the gate restored as described below.

### Corrected proof-store provenance

The record named `village` was a fresh **Minigame: C9 Mortars**, created on 2026-10-30 at 23:54:08.6722927 UTC (19:54 EDT), first event `setup-8838d6327781-1`. It carries the later Wire2/PR 85 two-SMC `participation` field. It came from the shared live/proof inputs used with that later build, not from PR 68's four owned games. Studio 6680 had been running the Wire2 artifact before it was replaced for PR 68. This was a new game, not an upgrade of an old recorded game.

The earlier PR 68 baseline cache `6ff45e469bba9079`, made at 17:46 EDT, has no such game and contains 132 games after the four owned fixtures. The September 25 archived `village`, first event `setup-d4a983558336-1`, is a different game and is not a valid substitute. Restoring the pre-PR-85 proof-store state therefore means restoring the absence of the newly added C9 fixture. Its full record is preserved at `E:/Archive/GitHub/dlandi/asl-gate-work-codex/late-pr85-proof-games/village.game.json`; the live game and the September archive are untouched. The corrected gate again has 132 games and reuses the earlier baseline cache. No cut or unrelated game is presented as a complete replay.

### Final local validation

The final strict build has zero warnings and errors. Only changed-behavior tests were run locally: ten Rules cases, three Play cases, then four Sniper live/replay cases for the measured reuse change. All seventeen pass. The allocation probes append no game events.

Against `ee967f71`, all 10,296 early sweep lines and all 55,839 full sweep lines match. Replay has 3,560 baseline lines and 3,576 current lines. The only differing game is owned `wire-ma-low-ammo`, at the same 17 comparison keys: its new Low Ammo, Disabled and Recall evidence is reconstructable only on the current side. The 32 source-text differences are the previously documented D3.7 and SMOKE refusals/occurrence counts plus the explicit unknown Disabled weapon refusal. No reconstructable older replay differs.

Two historical fixtures remain incomplete and are not claimed as successful evidence: `p71-sniper-residual` stops at revision 2 because [P11] is not on bd03, and `wire-stealth-ambush` stops at revision 14 because its Ambush facts disagree. Section 10's focused Sniper tests remain the reviewer's permitted alternative.

Evidence is retained under `E:/Archive/GitHub/dlandi/asl-gate-work-codex`: `pr68-final-{baseline,build,proofs}.log`, `before-wire-fire-3`, `after-wire-fire-3`, and `test-classes/PR68-final-{rules,play,sniper}.{log,trx}`. The code is wired; Claude sets its reviewed state after the nine-item recheck. CI and the user's merge instruction still govern reaching main. PR 81 stays open until PR 68 merges; the later chain and the game-upgrade proposal remain parked.
