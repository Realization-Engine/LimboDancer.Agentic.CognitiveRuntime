# Support Weapons Studio demo

Status: all eight phase demos approved, Rules contracts and focused checks complete. Historical compatibility gate and CI remain before release. The user's latest instruction is to pause before merging the PR. This is not a completed rule-claim acceptance record.

## Checkout and authorization

The user approved implementing Rules prerequisites and callers together on `codex/support-weapons-implementation`, in `C:/Users/dland/.codex/worktrees/support-weapons-implementation/LimboDancer.MCP`. The exception changes sequencing, not the Rules-only decision boundary or merge requirements.

Claim locks `c2d03ac3`, `1d1c22aa` and `63351a22` are document-only commits. Claims A4.43/.431/.44, A9.72, A9.8 and A11.21 are built, with final independent read-only Rules review completed. Their combined caller delivery is the approved exception; formal track acceptance and deferred rule coverage are not implied by the phase demos.

## Clean positions

The `ui-sw-demo-01` games began with only setup and phase-positioning events. The user and agent subsequently exercised some actions during the approved review, so these are now review records, not untouched starting positions. Development checks use separate `ui-sw-check-*` and `ui-sw-night-check-*` games. The user's ongoing games were not changed.

| Phase | Link | Review |
|---|---|---|
| Rally | [Open Rally](http://localhost:6680/games/play?game=ui-sw-demo-01-rph) | Separate Recover, Transfer and Repair. Broken holders and leaders in [F5] and [J5]; each receiving squad has three malfunctioned weapons. |
| Prep Fire | [Open Prep Fire](http://localhost:6680/games/play?game=ui-sw-demo-01-pfph) | Configuration beneath each holder without a fire target. Duplicate MMGs have numbered labels. |
| Movement | [Open Movement](http://localhost:6680/games/play?game=ui-sw-demo-01-mph) | Handling follows the parent unit selection. [F5] has ground recovery; [J5] also offers broken-holder recovery for its leader. |
| Defensive Fire | [Open Defensive Fire](http://localhost:6680/games/play?game=ui-sw-demo-01-dfph) | Select German as the viewing side and take the screen. German defenders may configure during the Russian DFPh. |
| Advancing Fire | [Open Advancing Fire](http://localhost:6680/games/play?game=ui-sw-demo-01-afph) | Fire workflow without routine weapon management. |
| Rout | [Open Rout](http://localhost:6680/games/play?game=ui-sw-demo-01-rtph) | Routable squads in [A3] and [B2]. Each leaves its MMG and keeps its LMG under the Rules-approved load. No independent Drop. |
| Advance | [Open Advance](http://localhost:6680/games/play?game=ui-sw-demo-01-aph) | Drop/transfer follows the advancing-unit scope. Transfer has a start window; dropping remains available after advancement. |
| Close Combat | [Open Close Combat](http://localhost:6680/games/play?game=ui-sw-demo-01-ccph) | Melee in [F5] and [J5]. Each side sees its own equipment. The German squad must leave equipment before withdrawing. Both sides still declare before resolution. |

Collapse the latest outcome if the setup narrative occupies the map area. Expand an operation to see its fields. In Movement, use `Move units from [hex]` and select the acting unit before reviewing its handling choices.

## Implemented wiring

- Recovery, transfer, repair and configuration share selected-action eligibility between command planning and current-record verification. Rules decides eligibility/capacity; components collect choices and format feedback.
- Recovery checks possession, the Rally start window, repeated attempts, current movement, MF, known enemies and supported environmental facts. Paid current-version recovery establishes the mover and opens a new defensive-fire window. Zero-MF recovery does not open that window.
- Transfer checks full Location, participant relationships, activity, earlier recovery, the APh start window and vehicle Motion. Automatic transfers/losses retain their separate path.
- Repair retains per-weapon and per-holder Player Turn history. A squad can repair a second weapon; neither weapon nor holder budgets can be bypassed. Vehicle armament remains separate.
- Configuration consumes full weapon use and the holder's shared allowance. History includes inherent fire, ordnance and MG assistants. Current fire/ordnance checks enforce the same allowance. Night uses the existing E1.89 calculator without a normal fire counter.
- Movement/Advance handling follows parent origin and actor choices. Configuration has shared map scope across owner groups, with switch and release controls.
- Melee, Ambush and infiltration equipment remains part of the withdrawal draft. Rules supplies load readiness. Changing equipment clears unsupported destinations and affected proposals. Rout retains its existing best-load calculation.

## Recorded-reading boundaries

New transfer records identify `TransferredBy`; configuration identifies `ConfiguredBy`. Repair uses `SupportWeaponRepairVersion`; recovery uses `RecoveryVersion` and recorded terrain evidence. Fire/ordnance use `ConfigurationUseVersion`. Null-version legacy verification retains its prior reading.

Use/repair histories are derived state, not serialized snapshots. Use history is bounded to the Player Turn and reads the appropriate phase window, including MPh/DFPh continuity. Legacy records may supply derived history without changing their acceptance.

Recovery terrain is planner-supplied map evidence. Projection checks state relationships and Rules readiness against that evidence; it does not independently reread map terrain. Cover this limit in the final contract review.

## Browser evidence

Registered Studio output: `E:/Archive/GitHub/dlandi/asl-gate-work-codex/support-weapons/studio-6680`. Port: 6680. Build log: `E:/Archive/GitHub/dlandi/asl-gate-work-codex/build-codex-studio.log`.

The final demo build passed with zero warnings and zero errors in 00:00:38.70. Studio process 82220 was then started from that output; verify its identity before stopping it. `git diff --check` passed for tracked changes.

Observed on disposable fixtures:

1. Ground recovery committed with its roll/MF cost. After the corrected projection loaded, Russian handover offered Defensive First Fire against that expenditure.
2. A leader recovered from a friendly broken holder at zero MF without defender handover. A squad received the SMC restriction. A failed attempt could not be repeated.
3. A squad committed two repair attempts. A third displayed `2 of 2` and disabled the proposal. Retrying the first weapon was refused.
4. APh transfer committed between same-location squads and changed the recorded holder.
5. Configuration committed without a target, changed PP and switched the verb to Assemble. Immediate reuse was refused; two configurations passed and a third was refused.
6. Night configuration recorded the dismantled condition and `configuredBy`, without a fire marker, while still enforcing the use limit.
7. Selecting [J5] limited configuration to its holder. Releasing scope restored [F5].
8. Melee withdrawal showed only the viewing side's units. Leaving the HMG changed 6/3 PP to 1/3 PP and enabled destinations. Taking it back cleared the destination.
9. Both clean Rout units have legal routes and correct carry/leave text. AFPh has no management panel. DFPh configuration appears for the defender.

Build corrections included formatting and an incorrect component parameter. A malformed night fixture was moved out of live games and replaced; the generator now writes `specialRules` as an array. The initial Rout positions caused surrender rather than useful rout choices; the clean demo uses the established [A3]/[B2] demonstration locations.

## Remaining release verification

The user approved all eight phase workflows, including the final one-checkbox-per-row CC equipment layout, and authorized commit, push and merge after green CI on 2026-10-31. Final review found an unsupported aboard-configuration case: the shared reader now supplies the carriage fact and Rules returns NotEvaluated. The seven focused Rules boundary checks pass. Four contract adapters and the manufactured `support-weapon-scope` bundle cover recovery, transfer, repair, configuration and retained withdrawal load. The contract adapter uses `none` for a known absent vehicle; unknown remains unknown. No broad local suite was run. Compatibility gate and final CI results will be appended below when complete.

- Four Rules contracts, independent review, seven Rules checks and thirteen minigame checks are complete. Finish the compatibility gate and CI. No broad test suite ran during Studio implementation. Do not claim replay equivalence from these browser checks.
- Independently exercise exceptional Ambush/infiltration outcomes, supported PRC transfer, hidden/unknown boundaries, repeated defensive-fire ordnance after recovery and MG-assistant configuration sequences. Their wiring is implemented; normal Melee is the browser-proven withdrawal case.
- Known Wire without recorded above/below relationships returns cannot-evaluate feedback for recovery/transfer. Special terrain/carriage relationships, triggered any-phase recovery, deliberate destruction and missing dismantled representations remain separate capabilities in backlog section 93.
- The condition-knowledge guard is conservative. Review relevant unknown facts before broadening it. Do not turn missing facts into permission.
- State-scoped candidate memoization and bounded derived histories remain. A representative allocation comparison is still required for final performance acceptance.
- No merge without explicit user approval and green CI.
