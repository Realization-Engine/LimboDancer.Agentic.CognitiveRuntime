# C6.17 recovery history implementation design

Status: approved 2026-11-01. Rules-only implementation on `codex/c617-recovery-history`, based on main `6cbf7df0` plus claim lock `235404fe`. Claude reviewed head `0943317a`; the corrections below await his re-review. Wiring has not started and awaits the Rules delivery and its separately approved merge.

Companion: [analysis and rulebook evidence](<../ASL C6.17 Recovery History Analysis.md>). Rules references below use physical PDF pages: A4.2 p. 49, A4.44 p. 50, A8.1-.12 p. 59, C6.17 pp. 173-174.

## 1. Approved scope and decisions

1. Correct personnel C6.17 accounting across joint movement, individual recovery and switches between members of the original moving stack. Each target keeps its own expenditure and each gun's shots at it.
2. Keep `Movers` as the participants in the latest expenditure and `Members` as the unfinished original stack. Retaining history never makes a unit attackable outside the current window.
3. Preserve recorded readings. Introduce a separate nullable C6.17 version, rather than changing `ConfigurationUseVersion` or treating existing recovery version 1 as a new reading.
4. Leave vehicle MP/Target Facing, Case J allocation, Gun Duel and Canister's multi-Location attacks (C8.4, p. 178) outside this correction. Rules selects the legacy reading for vehicle targets, Canister and fire kinds other than Defensive First Fire. This selects a reading, not permission to fire. Do not claim all of C6.17 is complete or close those existing backlog items.
5. Deliver the Rules calculator first and caller wiring second under the normal claim process. SW-01 was specific to PR 96 and supplies no exception here.

Version 1 covers ordinary on-foot personnel and supported one-predecessor/one-successor casualty reduction or replacement. Carried wounded men, newly created additional units and multi-predecessor/multi-successor relationships are not claimed as covered. A Rules-owned reading selector explicitly keeps proposals containing those known excluded relationships on the existing null-version reading for this MPh, including a mixed target set as a whole. This preserves existing behavior rather than inventing an independent one-shot ceiling for a carried man. Those boundaries are recorded in backlog 94. An unknown or corrupt history for an otherwise supported target is not an excluded relationship and must not fall back to the old reading.

## 2. Rules boundary and model

Add `ScenarioA1DefensiveFirstFireHistoryModels.cs`, `ScenarioA1DefensiveFirstFireHistoryCalculator.cs` and `ScenarioA1DefensiveFirstFireHistoryContract.cs` in Rules. These tentative names describe one current calculator, not a parallel eligibility implementation in Play or Units.

The logical history contains entries keyed by `(MPh instance, personnel target, full Location)`, with shot counts keyed additionally by gun. MPh instance includes the turn and phasing side, not just the phase name. Full Location identity includes board, hex and level. Keep entries for previously visited Locations until the phase ends; terminal removal may prune them only after supported lineage succession has been applied. Leaving and returning does not reset that Location's MF or shots. Moving one member must not clear another member's history. `MovementEnded`, broken or pinned alone is not a retirement condition: A8.14 (p. 59) permits further attacks in the still-open window.

Use the target's existing cumulative MF, in half-MF units, together with the cumulative total immediately before its current entry. Rules adds their difference to the MF already retained from earlier visits to that same Location. On leaving, finalize the old visit using its pre-expenditure cumulative total; charge the new entry's expenditure to the destination. Track which visit is active so no delta is counted twice. This includes in-place recovery without charging the other members. On a first expenditure in the starting Location, use the known MPh-start total. Never infer an unknown baseline as zero. Moving away and back is a calculator boundary check, not permission to restart a unit whose MPh has already ended.

Proposed plain facts and verdicts:

| Operation | Facts | Verdict |
| --- | --- | --- |
| Observe expenditure/location | Phase identity; affected unit id; previous/current full Location; previous/current cumulative half-MF; known starting baseline; prior target histories | Retain or finalize the active visit, activate a destination entry, or invalidate an unknown transition, preserving previous visits' totals and shots |
| Read allowance | Known current target context; current cumulative half-MF; weapon id; prior shots; current-window membership | Known half-MF spent, ceiling and shots remaining, or not-evaluated/refused with C6.17 reason |
| Record shot | Accepted Defensive First Fire shot; weapon identity; verified personnel target ids; prior contexts | Increment each actually attacked target's count for that gun exactly once |
| Continue target identity | Accepted lineage action; consumed/produced ids; before/after cumulative half-MF and Locations; predecessor history | Transfer supported one-to-one target history and rebase its active visit; classify other relationships explicitly as excluded or unknown |
| End phase/remove target | Phase transition or terminal removal facts, after lineage handling | Clear phase history or retire the removed target's entries; movement-ended status alone does neither |

The ceiling is `max(1, floor(halfMfSpent / 2))`, independent of whether ROF or Intensive Fire would otherwise permit another shot. Preserve half-MF until this final calculation. Missing context, negative differences or unsupported transition facts give not-evaluated, not an allowance of zero or permission to fire.

A shot against a joint moving stack counts once against each target in that shot's verified personnel target list. A later shot against A alone does not count against B. A missed shot still counts; do not use casualties or hits as the count predicate. Retain existing distinctions between an actual accepted shot and a proposal, voided Gun Duel response or a non-shot event. The Rules transition owns that distinction.

For supported one-to-one reduction/replacement, the successor is the continuing target, not a fresh allowance (A7.302 p. 55, A19.13 p. 86, A8.14 p. 59). Finalize the predecessor's active visit, copy its per-Location totals and gun counts under the successor id, then set the active-visit baseline to the successor's actual cumulative counter, with zero new expenditure for the identity change itself. Apply this after recording the causing shot and before pruning the predecessor. Never sum copied cumulative counters into expenditure again. Newly created heroes are additional units, not this one-to-one case (A15.21 p. 83).

For a version-1 multi-target proposal, the calculator checks every intended personnel target separately. Do not manufacture one allowance by adding MF, averaging histories or taking a gun-only total. Do not silently remove an exhausted target from an attack. This task adds no new selective-target UI. For a personnel set, any unknown relationship prevents evaluation, even if a different member has a known exclusion. Otherwise known excluded relationships select Legacy for the whole set before version-1 evaluation. A previously established exclusion survives later uncertainty about that same target, as RC617.1 specifies.

## 3. State and transition wiring

Add immutable derived personnel history to `GameState`, separate from legacy `MovementState.HalfMfInLocation` and `OrdnanceShotsHere`. It is replay-derived rule state, not a process-wide cache or new mutable game-file store.

Units reads event participants, Locations and exact cumulative MF before/after accepted projection, passes those facts to Rules, and applies the returned history. It must not decide which costs count, where a context resets, or how many shots are available.

Integration points:

- `StepMovement`: compare actual previous/next Locations, not merely the existence of a `MovementStepped` event. Same-Location steps retain the active visit. Changed Locations finalize the old visit and activate the destination's retained history, rather than clearing it. Failed entry must not create history in the attempted destination. Account for each supported participant's actual cumulative expenditure. Detect carried relationships explicitly: `ScenarioA1Wounds.MoverHalfMf` records zero personal MF for a carried man, so his counter cannot establish the new allowance. Mark that relationship as excluded under section 1, not as an ordinary zero-MF target.
- `AttemptRecovery`: ordinary successful and failed attempts increase only the recovering unit's existing cumulative MF. Free broken-holder recovery adds no MF and opens no new window. Retain the old public recovery helper's answer for compatibility; new ordnance allowance no longer depends on its scalar total or list-clearing verdict.
- Other existing in-place MF expenditures: use the same cumulative-MF baseline. Audit all writers of `MfSpent`/`HalfMfSpent` and Location before wiring; explicitly handle phase resets and supported relocation events. A target whose Location changed without a supported baseline transition is not evaluated on the new reading.
- `Ordnance`: after verification, gather the actual target ids from the accepted shot and apply the Rules counting transition. A joint shot must not be charged twice through separate projection paths.
- `RecordLineage` / `KeepMovingStack`: transfer/rebase supported successor histories as described above, in step with participant-id substitution. Do not treat a consumed unit as eliminated without checking its successor. `CreateInPlay` classifies an additional newly created unit separately; inherited movement status does not by itself prove the new unit's C6.17 shot history.
- `ChangePhase` and terminal elimination/exit: clear or retire contexts through Rules after lineage handling. Retain histories through window closure and movement-ended flags until phase cleanup unless the target is terminally removed. A broken or pinned member remaining in `Movers` stays eligible for the usual A8.14 follow-up checks. Retention never reopens its window. A new unrelated moving stack cannot read another unit's context by Location alone.

Keep the old scalar state updated exactly as before for verification of old events. Do not derive a modern target history from `OrdnanceShotsHere`, because its lost identities cannot be recovered from that list.

## 4. Planning, contracts and replay compatibility

Both `LiveOrdnance.FromState` and `Panzerfaust` use one shared personnel-history reader. It gathers facts for the same calculator that projection uses. Existing fire rules still decide ROF, weapon use, LOS, target eligibility and all other bars.

Append a nullable version property to `OrdnanceFired`, provisionally `DefensiveFirstFireHistoryVersion`, omitted when null. Append nullable `OrdnanceMovement.PersonnelHistory`, a Rules record containing `Version` and a deterministically ordered per-target fact list, likewise omitted when null; do not reorder/remove existing positional fields. New supported personnel DFF proposals write version 1 in both places. Vehicles, unrelated fire and the explicitly excluded relationships in section 1 retain their existing reading. The reading selector must be shared by command construction and verification, including the early `Panzerfaust` dispatch.

`ScenarioA1OrdnanceCalculator.Missing` and `Outside` dispatch on `PersonnelHistory.Version`. Null retains the existing scalar path. Version 1 requires complete, nonempty, distinct target facts and checks C6.17 only through the per-target calculator; it does not additionally apply the scalar ceiling. For version 1, `SpentHere`, `ShotsHere` and `MpClaimed` are null, while the existing non-Assault/Open Ground and other unrelated facts still apply. Missing/invalid version-1 facts and unknown versions fail explicitly. The event verifier requires agreement between event version and fact version, including rejecting non-null history under a null event version. `WithRecordedMapFacts` must not overwrite reconstructed history with recorded history.

| Recorded reading | Verification |
| --- | --- |
| No C6.17 version and no personnel-history facts | Rebuild the existing scalar facts and compare under the old behavior. Existing serialized facts remain byte-shape compatible. New proposals use this only for the explicitly excluded readings, not as an error fallback. |
| Version 1 | Rebuild target histories from state; verify exact target set, MF and per-gun counts before evaluating the new calculator. Do not trust supplied allowance totals. |
| Unknown/mismatched versions, missing version-1 facts, an unsupported target set under version 1 or inappropriate vehicle facts | Reject recorded events; return not-evaluated for an unavailable proposal fact. Never downgrade silently. |

Build the shadow history while replaying old accepted events, without changing their old verdicts. Reconstruct shot participation from verified facts, not today's window when the record supplies a different authoritative target set. Replay-and-continue must match live history for the same accepted events. Apply the same lineage and reading classification during legacy replay. If a historical event lacks enough identity information, mark affected supported contexts unknown and explain the limitation; never fabricate unused shots. Shadow-history uncertainty cannot reject an otherwise valid old event. Existing `OrdnanceRecordVerifier` must explicitly choose the old or new fact read before its serialized comparison.

Inside Rules, move the existing C6.17 scalar comparison to the new calculator as a legacy-compatible entry point, with the existing caller forwarding and preserving every answer. Version-1 evaluation uses the per-target entry point. Preserve the vehicle `MpClaimed` clause unchanged. Document the old `MphMovement`, `ClearsShotsHere` and `RecoveryMovement` roles; do not duplicate the new rule inside them.

The Workbench contract exposes a single-target allowance check plus known/unknown history inputs. Phase is an All Choice. Target and firing-unit facts retain their Unit owner/disclosure chain and anonymous enemy handles; weapon identity remains owned by the firing side. Announced half-MF and resolved shot counts are All facts without an owner dependency (A8.11/.12, p. 59). The numeric allowance does not require private target identity: a sealed target remains unknown while the firing side evaluates supplied public totals. The opposing view cannot name the firing weapon and remains not-evaluated. This does not expose a raw history ledger or prove the later gathered facts. Compound transitions remain Rules tests and, after wiring, played acceptance checks.

## 5. Delivery and claim plan

No claims are taken by this proposal. Before implementation, refresh main and inspect current holds.

1. Claim/audit C6.17 for the Rules owner. Proposed new files are the three above. Shared edge holds: `ScenarioA1OrdnanceCalculator.cs` for delegation and `ScenarioA1OrdnanceModels.cs` for nullable facts. Add any other required Rules file to the claim before editing it. Preserve existing member answers. Build the contract, minigame checks and focused Rules tests. When built, add the open `W-C6.17` row listing caller sites, legacy members and intended behavior changes. Review and merge that Rules-only PR with green CI and user merge permission.
2. Claim the existing `W-C6.17` row under a fire-capability wiring claim after the Rules work is merged. Start the wiring branch from that up-to-date main. Candidate holds: `Play/LiveOrdnance.cs`, `Play/GamePlanner.Ordnance.cs`, a shared history fact reader; `Units/State/GameState.cs`, `GameProjector.cs`, `OrdnanceRecords.cs`, `GameEventReader.cs`, `GameEventWriter.cs`, plus affected tests and the fixture generator. Paths here abbreviate the existing `LimboDancer.Domains.Asl.*` project names; the ledger must use full paths.
3. Implement versioned wiring, prove the clean Studio interaction, then run only the methods below that assert changed behavior. Record each intended sweep difference, run the wiring gate and CI, and ask for merge permission. Update backlog 94 only after its complete reproducer and shot-history variant pass.

A combined Rules/wiring PR would require a new explicit exception. This design does not request or assume one. Ordinary claim delivery is the recommended route.

## 6. Verification and demo

Prioritize a clean Studio demo using an imported verified board, two squads, distinct recoverable weapons and a defensive Gun with suitable ROF. Pick interior Locations with definitive LOS, not map-edge geometry. Create from a fresh setup, not a played game. Script repeatable non-casualty shots retaining ROF where the fixture allows it. Keep the initial demo unplayed for the user.

| Check | Required evidence |
| --- | --- |
| A+B enter, A recovers and ends, B recovers, no shots | At B's window, B has 2 MF of history; A is not attackable merely because retained. Two otherwise legal shots at B, then refusal. |
| Joint shot, A recovery/shot and end, B recovery | B retains its one earlier shot and has exactly one remaining under C6.17; A's later shot does not debit B. |
| A makes successive recoveries before ending | A retains its own prior gun shots; no fresh zero-shot allowance. After A ends and B starts, history must not authorize A to resume. |
| Two defensive guns | Each has independent counts against each target. |
| Failure/free exception | Failed paid recovery counts MF; free broken-holder recovery adds none and creates no new firing window. |
| Boundaries | Half-MF rounding, minimum one, retained ROF, Intensive Fire, closed windows, phase change, actual Location change, leave-and-return without reset, same-Location action, different board/level and member completion. |
| Target sets | Joint-shot accounting, surviving target identities after casualties, invalid/missing/duplicate facts and unsupported target-set relationships. |
| Identity and follow-up | An accepted shot followed by one-to-one reduction/replacement retains MF and that shot under the successor id; no double-count on rebasing. Broken/pinned targets retain history during their open window even when `MovementEnded` is true. |
| Excluded relationships | Carried men, new additional units and unsupported lineage shapes retain the explicitly selected old reading for the whole target set. The same selection occurs during replay; unknown ordinary history never selects that route. No version-1 payload is emitted for an excluded set. |
| Compatibility | Null-version JSON unchanged; old recorded replay unchanged; new event write/read round trip; old replay followed by a new shot; tampered totals/targets/counts and unknown versions rejected. |
| Disclosure | Both player views and adjudicator; no hidden identities or raw history in choices/refusals. |

Existing scalar Pass 8 tests and PR 96's recovery test keep their assertions. Add focused calculator and Play methods proving the new behavior, plus only a UI test if rendering/interaction actually changes. Reuse immutable expensive fixture inputs. Do not run whole projects locally before the gate. No tests or performance runs are warranted for this documentation-only proposal.

The gate must separately identify new proposal/fact differences and prove old recorded events still agree. A sweep that omits recovery or required new arguments is not proof of the alternating sequence; the committed minigame and focused Play tests supply it.

## 7. Memory, performance and outputs

- Ownership/lifetime: immutable history belongs to a `GameState` snapshot and one MPh. Entries exist only for target/Location pairs actually visited, with gun entries only for shots actually made at that pair; one active-visit baseline per target. Bound growth by this phase's actual participants, visited Locations and shots, not elapsed game revisions. Retire entries at established boundaries, not by scanning all past revisions.
- Work: update affected targets at entry/recovery/shot boundaries, reuse unchanged collections, and read cumulative MF from the current unit. Do not rescan the event log per proposal or scan all units inside each target loop. Target-set reads are proportional to that set and its actual gun entries.
- Keys/invalidation: unit id, complete Location/context and gun id; phase identity prevents cross-turn reuse. Runtime memoization, if needed after measurement, uses the existing state-scoped memo and includes gun/targets/view. No new global cache is proposed.
- Concurrency/disclosure: immutable snapshots and pure calculators; no shared mutable ledger, side effects during cache fill, or direct UI exposure. Preserve pinned map reads, counter fragments and incremental overlays; this change needs no whole-layer rebuild.
- Measurement: after implementation, compare repeated proposals and recovery/shot steps on the clean fixture and a representative Tractor Works cut, same build configuration and data. Record allocations, history entry counts and retention after phase change; distinguish noise from improvement. Acceptance requires bounded entries, no full-history rescans, unchanged unrelated behavior and investigation of repeatable timing/allocation regressions. This is correctness work, not a promised speedup.
- Build outputs outside the repository: this analysis creates none. Future Studio output is under `E:/Archive/GitHub/dlandi/asl-gate-work-codex/c617-recovery-history/studio-6680`; tests/gate use the standard Codex work folder. Register outputs as created, stop only this task's Studio when finished, clean registered builds after CI, and confirm the registry is empty. Preserve proof logs and fixtures according to the existing harness contract.

## 8. Approval boundary

The user approved the personnel-history model, versioned compatibility strategy and normal two-stage delivery on 2026-11-01, explicitly requiring the Rules implementation to be complete and independently reviewed before wiring. The known vehicle and Gun Duel limitations remain explicitly open. Design approval does not authorize a merge or extend the old SW-01 exception.

## 9. Design review

A separate read-only reviewer checked the proposal against the code and PDF. Three corrections were applied: preserve per-Location history across return visits; replace the invalid A-resumes-after-B demo with consecutive A recoveries before A ends; create the open wiring row in the Rules delivery before claiming it after merge. Accepted legacy sequences may still be folded into shadow history for compatibility without treating them as permission for new movement. No build or test result is claimed for this proposal.

A second review added supported lineage succession/rebasing and open-window retention after pinning/breaking, made the version discriminator reach both Rules validation paths, specified event/fact version agreement, and separated carried/new-unit relationships from ordinary cumulative-MF accounting. Those excluded cases preserve the old reading and are tracked in backlog 94. The Workbench weapon fact was corrected to avoid treating equipment as a Unit. Application code remains unchanged.

## 10. Rules-stage implementation and review

`ScenarioA1DefensiveFirstFireHistoryCalculator` owns the immutable history transitions, reading selection, allowance and versioned-fact validation. `ScenarioA1OrdnanceCalculator` delegates the existing scalar expression unchanged and validates a supplied version-1 supplement through the new calculator. `PersonnelHistoryTargets` reuses resolution's target selection, including the non-20L ATR's Gun crew alone (C13.23/.24, p. 183). Reading a history also checks the current cumulative counter, so an unobserved expenditure is not silently accepted. No Play, Units or Studio application code changed.

The independent Rules review completed on 2026-11-01 with no remaining concrete defects after three corrections: detect unobserved counter gaps, reuse actual ordnance target selection, and provide real counters in the minigame setup. The fresh six-counter `ui-c617-rules-01` record has ten setup/phase revisions. Its Workbench contract accepts history facts entered by hand; it is not proof of the later event wiring or a played recovery sequence.

Initial verification at `0943317a`: Studio checked allowance boundaries and disclosure. The Rules run passed 12 cases (ten new and two legacy methods), followed by one separate unchanged scalar-limit method, recorded in `asl-gate-work-codex/test-classes/C617-legacy.trx`. Thus 13 distinct Rules methods were checked across two runs; Claude's independent 12-case selection was a different selection. The Play run passed eleven minigame verdicts plus two bundle-validity methods. These historical results do not stand in for verification of the review corrections. Final CI remains required.

Wiring remains a separate delivery. It must implement the recorded-event version agreement, shared command/projector readers, history retention and succession, gathered Workbench facts, played recovery proof, performance measurements and replay/sweep gate described above. None is implied by this Rules-stage check. The inventory's existing five-claim batch-gate requirement also remains a prerequisite to merging a sixth Rules claim. No merge is authorized by the design approval.

## 11. Claude review corrections and recorded rulings

- **RC617.1, sticky known exclusions:** C6.17 pp. 173-174, A17.2 p. 85 and A15.21 p. 83. Once a target is positively classified as carried, additional-created or complex-lineage during this MPh, a later unknown classification invalidates its accumulated history but does not erase that known exclusion. A separate target whose relationship is unknown still makes a mixed personnel set Unknown, preserving the existing unknown-set assertion. This distinguishes a previously established relationship from absence of evidence about another target.
- **RC617.2, minimum allowance boundary:** C6.17 p. 173 says a minimum once per hex, while expenditure is per Location. This bounded personnel correction retains the existing minimum once per full Location as an explicit simplification. It may over-allow a shot in a Location where the target spent less than 1 MF, in a hex where that weapon already had an allowance against it in another Location, including one where it spent 1 MF or more. Exact cross-Location minimum tracking is deferred in backlog 94; it is not claimed as the full printed rule.
- `Reading` returns the named `Unknown`, `Legacy` or `Version1` decision. These enum values are not serialized versions. Legacy means omit both event version and supplement; Version1 means write 1; Unknown means no evaluated proposal. Shot-kind exclusions are decided in Rules before personnel accounting. Canister is excluded because C8.4 p. 178 attacks a cluster of Locations.
- Version-1 target facts must be strictly ordinal by TargetId. Duplicate or unsorted facts fail validation. A bound target outside the current fire window is a fact mismatch, not an exhausted allowance.
- The duplicate minigame case now proves the firing side can refuse an exhausted allowance without target identity. Focused tests additionally cover mixed-target exhaustion, binding contradictions, unobserved-transition identity mismatches and JSON read-back.
- Claude is the claim reviewer. The claim stays built while his findings await re-review, not under a standing exception. The checker/file-hold conflict is separately recorded in backlog 94 for a user-approved process resolution.

Review-correction verification: the strict Studio build passed with zero warnings/errors (`asl-gate-work-codex/build-codex-studio.log`). Workbench checks passed for German allowed/exhausted verdicts with no target identity, Russian public counts with the firing weapon still hidden and a corresponding not-evaluated verdict, and the adjudicator's allowed verdict. Evidence: `asl-gate-work-codex/c617-recovery-history/review-workbench.jpg`. Eight focused Rules methods passed (`test-classes/Rules.log` and `Rules.trx`); eleven C6.17 minigame cases plus the two bundle-validity methods passed (`test-classes/Play.log` and `Play.trx`). Status generation and the Minigames column check passed. Claim validation reports only the pre-existing five-claim batch-gate prerequisite, with all five held Rules files recognized. The batch baseline finished, but its head build/proofs/carry and final CI remain outstanding. No merge or wiring is authorized.
