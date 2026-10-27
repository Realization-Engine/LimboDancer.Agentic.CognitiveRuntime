# ASL Turn Phase Guide Implementation Plan

Date: 2026-10-27

Status: Backend-first plan. B0, B1, B2 and the bounded B3 obligation slice are implemented and tested. B4 and the bounded B5 Placed DC, fire/Spotter, atmosphere history and work-progress, live work adjudication and bounded completion-effect slices are implemented; additional families remain planned. UI work is intentionally last.

References: [Design](ASL%20Turn%20Phase%20Guide%20Design.md), [Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md), [Cross-phase audit](ASL%20Cross-Phase%20Dependencies%20and%20Cleanup%20Audit.md).

## 1. Working boundary

Build and prove `ASL.Turns` independently of any renderer. Its tests live in `ASL.Turns.Tests`. Rules keeps rule semantics; Units keeps authoritative event/state projection; Play adapts facts and executes governed actions. No UI project, bUnit, browser state or component lifecycle is needed for backend tests.

Keep all reviewed options visible from the start, including partial, pending, deferred and source-gap entries. Current baseline: 538 topics, 38 dependency chains and 191 links. These counts are completeness checks for the current sources, not a universal ASL coverage claim.

Run focused tests only for changed behavior and direct dependencies. Broaden when an observed failure or shared-contract change warrants it, and record why. Do not use passing reference tests as evidence of live rules support.

## 2. Stages

| Stage | Deliverable | Depends on | Exit gate |
|---|---|---|---|
| B0, implemented | New library/test projects, headless immutable reference catalog, relocated documentation and saved mock-up | Reviewed inventories | Complete baseline loads without UI/Play dependencies; focused tests pass. |
| B1, contracts implemented | Stable identity/timing manifest, contextual term IDs and typed capabilities | B0 | No prose parsing for legality, unregistered capability is Not evaluated, all IDs/links traceable. |
| B2, implemented | Pure guidance request/result and read composition | B1 | Fixed facts yield deterministic snapshots; no writes, dice or UI state. |
| B3, bounded slice implemented | Rule-owned timing and obligation lifecycle for Stun, CX, Trip Flare and non-Flame Clearance | B2 and relevant Rules families | Due, interrupt, resolve and retained history proved at exact boundaries. |
| B4, implemented | Play/Units adapters and shared phase-close assessment | B2-B3 | Existing command validation and guidance agree without duplicate cleanup; disclosure and stale checks pass. |
| B5, bounded persistence/history slices implemented | Durable missing facts, replay and profile expansion | B4, per rule pass | New families have source, Rules, persistence, replay and visibility evidence. |
| U1, deferred | Reference and Live Blazor consumers | Backend readiness gate | UI renders backend results and routes intentions without rule calculations. |

The previous UI-first P1-P7 plan is superseded by this order. The full final scope remains intact; deferring UI does not defer reference content.

## 3. B0: Implemented foundation

- Added `src/ASL/ASL.Turns/ASL.Turns.csproj` and `TurnGuideCatalog.cs`, with namespace `ASL.Turns`.
- Added `src/ASL/tests/ASL.Turns.Tests/ASL.Turns.Tests.csproj`, with namespace `ASL.Turns.Tests`, normal xUnit/test SDK references and no UI reference.
- Added both projects to the ASL solution and generated dependency lock files.
- Embedded the reviewed catalogs from `docs/Turn.Guide`. The library exposes immutable phases, topics, terms, ledgers and dependency definitions with qualified IDs and reference lookups.
- Preserved pending notes, source metadata, separate phase glossaries and the Rally schema difference. An absent Rally JSON ledger does not replace the reviewed walkthrough ledger.

At the B0 commit, `dotnet test src/ASL/tests/ASL.Turns.Tests/ASL.Turns.Tests.csproj --no-restore` passed five tests. B1 expands this baseline and adds the coverage gate below. These verify the full inventory/links, pending visibility, qualified/case-sensitive identity, documented flare deadlines and absence of UI/Play assembly dependencies. No live timing engine is claimed.

## 4. B1: Stable schema and capability contracts, implemented

The library now exposes typed source review, evaluator/command support and applicability dimensions. An immutable capability catalog retains all topics; missing registrations stay Not evaluated. Explicit sub-action/profile registrations require evidence for partial or integrated support. The design documents the distinction between production registration and existing Rules implementation.

The versioned timing manifest assigns stable group IDs and source-reference windows to all 538 topics. It preserves printed steps, imports Rally's ten-step ledger and uses explicit unclassified supplemental windows. Validation rejects unknown/duplicate/missing links, incorrect phase ownership and incomplete multi-window mappings. Group identities survive label and numeric-position changes. Contextual term IDs preserve phase and case. Typed due payloads distinguish event, window, owner-relative phase, Player Turn and completed Game Turn intervals without executing them.

The existing JSON remains authoritative for topic text, sources and glossaries. The identity manifest adds keys and bindings only. No normalization generator or second content catalog is introduced. No IDs are renamed; aliases are required before any future rename. External acronym mappings and executable capability/profile registrations require reviewed evidence during B2/B3 integration, not guessed IDs or parsed integration prose. Existing interpretation and module-source gaps remain visible.

Required validation: `./src/ASL/tests/ASL.Turns.Tests/Test-Coverage.ps1`. Current result is 62 passing tests, 242/242 lines and 132/132 branches covered for the entire library. Keep both coverage measures at 100%; test the behavior and failure paths rather than adding exclusions. The gate uses fresh reports so stale successful runs cannot mask a regression.

## 5. B2: Headless guidance composition, implemented

Added `TurnGuideService`, immutable request/context/facts/verdict contracts and consumer-neutral snapshots with source, vocabulary, timing, capability and decision data. Composition retains every browsed-phase topic and its pending notes. It consumes authorized host verdicts rather than evaluating rule prose. Missing evaluator registrations or subjects remain NotEvaluated. Required, optional, informational and evaluation-gap attention remain distinct.

Context equality rejects stale/wrong game, viewer, side, profile, phase, window or evaluation stamp. Audience filtering happens before validating visible verdicts. Exact profile and current-window filtering prevent unrelated facts from creating legality. Historical reads, browsing another phase, another side's decision and absent command support suppress action routes. All writes remain outside the library.

Snapshots do not reveal internal stamps or other viewers' verdict data. The host remains responsible for authentication, disclosure-safe reasons/handles and action revalidation. There is no live GamePlanner/Rules adapter, phase-close engine or obligation scheduler in B2. Production capability registrations stay unverified until those integration proofs exist; the Movement/Rout test registrations are fixtures only.

The required coverage gate passes **88 tests, 336/336 lines and 176/176 branches**. Tests prove full reference inventory, pending visibility, required/optional distinction, historical read-only output, unknown-subject behavior, stale scopes, deterministic reads, input immutability and hidden-world equivalence. Use `./src/ASL/tests/ASL.Turns.Tests/Test-Coverage.ps1` to reproduce and enforce both 100% thresholds.

## 6. B3: Typed clocks and obligation instances, bounded slice implemented

`ASL.Turns` now references Rules and calls `TurnObligationRules` through `TurnObligationService.Assess`. Rules evaluates Stun/CX release, activation-dependent Trip Flare expiry and qualified non-Flame Clearance timing. It uses existing Stun/CX predicates where available. The source audit supplies the additional timing boundaries; no projector mutation is duplicated.

The evidence contract includes stable origin identity, owner and decision owner, typed positions/deadlines, completed-interval receipts, terminal outcome evidence and prerequisite keys. Read-only coordination validates chronology, detects cycles/missing prerequisites, orders child effects before dependents and rejects parent resolution before child evidence. Missing origins and unsupported families remain NotEvaluated. All 38 definitions and all 538 reference topics remain available; command support is Pending until governed integration is proven.

The assessment is internal and may contain hidden facts. B4 must provide authenticated projection, origin extraction, coherent as-of history, task-specific eligibility and existing-command enforcement. The present save/reload tests exercise the evidence contract, not Units migration. Duration rules for packing/replenishment are still pending even though partial progress receipts can be retained.

Validation: **116 tests; 425/425 lines and 234/234 branches for ASL.Turns**. The focused Rules suite passes **28 tests**, covering new timing boundaries and existing sequence behavior. Tests include Stun versus next-RPh timing, CX owner-relative release and provenance, early/late flares, Clearance before cleanup, interruption with retained progress, Game Turn versus Player Turn clocks, distinct instances, prerequisite order, terminal chronology, legacy gaps, deterministic replay and repeated reads.

## 7. B4: Adapters, disclosure and closure parity, implemented

Play now references ASL.Turns. GamePlanner.ReadTurnGuideAsync combines the complete phase reference snapshot, disclosed obligations and shared phase-close assessment. AssessTurnPhaseCloseAsync accepts StateStamp, authenticated Perspective and retain-DM selections. These are server APIs, not network endpoints. The caller must authenticate Perspective, as with ViewOf; a client-selected adjudicator is not authorization.

TurnHistoryAdapter derives Stun/CX activation and terminal evidence from actual projected transitions. Repeated reads and planner recreation reconstruct obligations without writes. Historical reads use the requested revision and are read-only. Setup markers have unknown origins and remain NotEvaluated. Other families retain their catalog entries and pending support, including Trip Flare and Clearance whose B3 timing contracts exist but whose host evidence/command integration is not admitted.

The host persists phases, not ASOP substeps. Reserved <phase>:phase context composes reference guidance while suppressing live verdicts and action routes. It never substitutes the first ASOP window. Production topic capability registrations remain empty until specific Rules/command evidence is admitted; all 538 topics and 38 dependency definitions remain available.

Actual phase-advance commands and assessment share the full PlanCoreAsync pipeline and BuildPhaseClosePlan (formerly PlanAdvance), including global guards, proposer checks, phase-specific refusals and consequences. Assessment bypasses only committed-attempt replay detection using an ephemeral, undisclosed probe ID. It never commits or invokes a deferred roll delegate. Existing projector cleanup retains sole mutation ownership. Assessment reports planned event types without creating replacement cleanup events. PhaseEndBar remains a legacy proposer-only check.

Adjudicators receive exact reasons, event-type effects, roll requirement and Ready/Blocked/Stale/ConsequencePending classification. A consequence-only plan is not a completed phase transition. Side views always receive fixed GovernedCheckRequired wording without hidden readiness, reasons, counts or roll flags. They use the existing governed action to confirm closure. Side obligation disclosure requires own visible counters and visible origin/outcome events. Internal revisions, origin IDs and prerequisite graphs never leave the API. Historical views return ReadOnly. Finer side readiness disclosure remains future work.

Validation: the full ASL.Turns coverage gate passes at 452/452 lines and 254/254 branches (100% each), without source exclusions. Host tests compare assessment to command plans across all eight phases over two player turns; prove invalid retain-DM refusal parity beyond PhaseEndBar; reject wrong perspectives, missing games and stale map/revision stamps; compare entire side responses before/after hidden enemy/revision changes; and exercise actual Double Time/CX and Stun cleanup, historical reconstruction and repeat-read safety.

B5 owns further durable facts, finer disclosure and remaining families. Clearance-before-status and concealment-before-illumination end-to-end integration remain pending. B4 does not claim the whole rulebook is executable or the UI gate is satisfied.

## 8. B5: Persistence and profiles, Placed DC slice implemented

B5 adds a bounded Placed DC integration (A23.3-.4; ASOP 3.32A and 5.22A), not blanket support for every remaining rule family.

**Durable facts and compatibility.** New governed DC placements write optional `dcOwnerSide` on the existing `movement-step` event. Units reads/writes it and rejects a recorded owner that differs from the placer at that revision. Origin ID, sequence, phase, target, CX and concealed-target facts already exist in the event log, so no duplicate obligation store or generic resolution event is introduced. The existing schema version remains 1: the new field is additive and omitted for legacy null values. Older events replay exactly as before, but the guide keeps their missing placement ownership incomplete (`NotEvaluated`, command support `Pending`), even if later completion evidence exists. No automatic migration guesses ownership from a later counter or custodian.

**Lifecycle and authority.** The history adapter creates a distinct `placedDc` candidate from each placement event, preserves its original owner and follows projected operability. Rules schedules it before AFPh and marks an operable DC due in AFPh or overdue afterward. The placer's subsequent ordinary firing eligibility does not erase that obligation. A supported placement interrupted before becoming operable is `Interrupted`. Completion requires a recorded Placed DC attack followed by removal of that charge; removal alone is not proof of detonation. The original governed detonation/choice/follow-up path and projector remain the sole mutation owners. The existing shared phase-close planner blocks a due executable DC attack. Reads do not draw dice, complete attacks or bypass confirmation.

**Profiles.** `TurnGuideProfiles` names integration evidence and a version. `existing-engine` admits Stun/CX command integration only when origin facts exist; `existing-engine-dc-v1` adds the reviewed Placed DC path. `ReadTurnGuideAsync` preserves the baseline; `ReadTurnGuideProfileAsync` explicitly selects the expanded profile and returns its identity/evidence. Unknown profiles fail instead of implying full support. Profile version participates in the internal evaluation context. Profiles describe admitted host integration, not scenario applicability or a claim that all night, water, aerial or module rules are supported.

**Disclosure and pending content.** All 538 topics and 38 dependency definitions remain present regardless of profile. Missing origins and unregistered families remain pending. A profile's command support does not manufacture an executable topic route: the host still lacks persisted ASOP substeps and uses phase-level reference guidance. Side projection requires original ownership, visible origin/outcome events and disclosed subject identity. A completed own DC remains visible after counter removal only when its identity was visible at placement. Adjudicators retain terminal history. Side closure remains the fixed `GovernedCheckRequired` result, including when an opposing hidden obligation exists.

**Proof and remaining work.** Tests exercise new/legacy wire round-trips, owner mismatch rejection, disk reload, idempotent append retry, historical reads, interruption, AFPh closure blocking, detonation completion, post-removal disclosure and repeated-read safety. Rules tests cover phase boundaries and continued obligation despite placer ineligibility. ASL.Turns passes 127 tests with 465/465 lines and 264/264 branches covered (100% each, no source exclusions). Coverage is for ASL.Turns, not the entire existing Rules, Units or Play assemblies.

Further B5 slices remain explicit: remaining fire/Spotter execution semantics; remaining smoke/drift/cave WP/dust execution; packing/replenishment automatic eligibility, animal transport and remaining supply consequences; tunnels/airborne; CC declarations/heroes; and water/aerial/module profiles. Trip Flare and Clearance still need host facts and governed command integration. Each needs source reconciliation, narrow Rules predicates, durable evidence, replay, disclosure and command proofs before admission. The complete UI gate is still pending, especially a supported live topic decision route with exact window evidence.

### B5 fire and Spotter history slice, implemented 2026-10-27

The explicit `existing-engine-fire-v1` profile adds `TurnHostGuide.FireHistory` through `ReadTurnGuideProfileAsync`. Existing profiles keep that optional result absent. This profile retains the admitted Stun/CX/Placed DC commands; `spotter` command integration remains Pending. All 538 reference topics and 38 dependency definitions remain available.

`TurnFireHistory.Compose` is pure and receives already-disclosed typed records. A shot records its actual Game Turn, phasing side, phase, firers, optional weapon/Spotter, recorded ordnance ROF result and whether it belongs to the current phase occurrence. Direct fire has no fabricated ROF value. The host uses the last actual phase-change event, so a later Player Turn's identically named phase does not revive an earlier shot's phase status. These are recorded attacks, not every declaration, attempt, weapon-use restriction or pending response, and never permission to fire again.

Spotter history keeps the first **observed firing use**, not an invented original designation time. Repeated shots with the same assignment keep that observation; a later disclosed assignment preserves the former as Superseded. Present disclosed facts yield Retained, Lost or NotEvaluated. The existing C9.3 retention predicate is shared by the live ordnance command and this projection: active, same side, not broken, not captured. A pinned Spotter is not automatically discarded by that retention check. Per-phase spotting use resets through the existing projector while the recorded relationship remains. Retained does not certify LOS, ammunition, target legality, replacement permission or a future firing opportunity.

No new persistence format or migration is needed: `FireResolved` and `OrdnanceFired`, their verified facts/ROF, and projected historical states already preserve this slice's evidence. The adapter reads ownership and phase at the shot, not from a later custodian. Disk read/write/replay and duplicate append tests reconstruct identical history. Reads neither alter markers nor roll dice. Missing prior designation evidence stays unknown rather than being reconstructed from a present marker.

Disclosure precedes composition. The adjudicator can inspect recorded firing history. A side receives only its own disclosed firing participants, weapon and Spotter, checked against the view at the shot and event visibility. Raw revisions, event IDs, targets, resolutions and hidden payloads are absent. Withheld attacks are omitted even if a public fire report exists; there is no hidden-attack count. Current Spotter observations require disclosed participants; absent observations are NotEvaluated. Historical reads use the requested revision and stay read-only. Fixed evaluation-gap text accompanies even an empty history.

The reviewed dependency remains broader than this slice (C9.3-.31; ASOP 2.22A, 3.11A, 4.21D). Original designation before the first shot, replacement at the owner's MPh opening, Opportunity Fire and its C9.3/A6.11 breakdown obligation remain visible as pending. This does not endorse current command timing as complete rulebook coverage. Vehicle/PRC combinations and other fire dependencies also remain pending. The guide introduces no alternate firing or phase-close authority.

Validation: 131 ASL.Turns tests; 516/516 executable lines and 288/288 branches covered (100%, no exclusions). Tests cover ordinary fire and mortar shots, earliest observed assignment, ROF history, missing observations, lost/superseded relationships, phase reset, repeated phase names, actual command refusal to change a retained Spotter, disk round-trip/replay/retry, historical reads, hidden-world invariance and withheld-event omission. Shared Rules retention checks and existing ordnance regressions are also exercised.

### B5 smoke, drift, cave WP and dust slice, implemented 2026-10-27

`existing-engine-atmosphere-v1` adds `TurnHostGuide.Atmosphere` and includes the previous fire/Spotter history. Older profiles keep Atmosphere absent. The admitted live slice is ordinary infantry grenade attempt/cleanup history plus recorded Wind Change/Gust history. Drift, cave WP and dust are represented by distinct typed pending-rule entries; this milestone does **not** implement their domain mutations or claim complete atmospheric-rule coverage. The broad `smoke`, `driftGusts`, `caveWp` and `vehicleDust` dependencies remain Pending. All 538 reference topics and 38 dependencies stay available.

**Authoritative evidence.** The host reads the existing `MovementStepped.Smoke`, `InstanceCreated.Causes`, `PhaseChanged` and `WindChanged` events. No duplicate event, migration or guide-owned counter store is needed. A grenade counter must be the immediately following, causally linked SMOKE creation at the attempt's target, with the existing grenade suffix. Its source owner is the placing unit's side at the attempt, never the current ATTACKER or the anonymous counter's side. Timing is restricted to the existing ordinary-grenade command path; no arbitrary SMOKE entity is classified by its location or suffix alone.

**Lifecycle.** A failed attempt remains `FailedAttempt` and its attempt remains spent for that phase occurrence. A linked active counter is `Active`. `RemovedAtMovementEnd` requires the actual active-to-eliminated transition across a phase change for which the shared Rules predicate `RemovesSmokeGrenades` applies. Counter absence alone is NotEvaluated. Historical reads stop at the requested revision; repeated reads/restarts neither clear counters nor refund attempts. Legacy logs lacking causal links still replay unchanged but report NotEvaluated, with any visible active counter listed as unclassified. No origin or cleanup receipt is invented.

**Pending families and ordering.** `TurnAtmosphereGap` carries a typed family, dependency, reference, timing, missing facts and Pending command support. Seven entries remain visible even in an empty game:

- White SMOKE: preserve owner/type/size/dispersal; owner's PFPh removal of previously dispersed counters precedes dispersal of remaining ones.
- Drift: preserve source links, wind direction/force and shortening/removal evidence; immediate invalidation is separate from first-RPh/AFPh placement.
- Gusts: preserve the recorded RPh event for later AFPh processing. AFPh drift precedes WP, which precedes Gusts. History is not an applied-effect receipt.
- Cave WP: preserve cave/type/size, original timing and eligible connection identities; half-inch cave smoke survives MPh and expires after concealment at Player Turn end. Ordinary drift/Gusts treatment does not apply, and a disclosed tunnel opening does not disclose a hidden pillbox.
- Vehicle Dust: preserve source vehicle, creation/shift/impulse, Motion and disabling-state history. Source departure/wind, next friendly PFPh checks and continuing-Motion initial MP are distinct triggers.
- Ambient Dust: preserve Desert profile and intensity changes; do not substitute Vehicle Dust or SMOKE timing.
- FFE Dust: preserve its own source and duration/effect evidence; do not reuse Vehicle Dust wind/Gust rules. Detailed execution/source reconciliation remains pending.

The existing Wind Change event already records Gusts, even though current projected state does not preserve a general drift/Gust engine. The guide exposes that durable history at RPh and AFPh without rerolling or claiming its smoke/fire-spread effects occurred. Future execution needs typed source ownership, kind/size, drift/cave links, wind direction/force, Motion/impulse facts, effect receipts, Rules predicates and governed commands before support can be promoted. Existing phase-close behavior is unchanged; reference reminders never become unsupported automatic cleanup.

**Disclosure.** Side grenade histories require a disclosed own source unit and visible attempt, creation and cleanup evidence. Adjudicators can inspect both sides. Wind rows require visible events. Unclassified counters come only from GameView-visible SMOKE entities; no hidden graph, global revision, die IDs or raw payload escapes. Missing classification grants no timing or action route. The side's phase-close assessment remains conservative.

**Validation.** 140 ASL.Turns tests pass with 572/572 lines and 304/304 branches covered (100%, no exclusions). The focused Play suite passes 40 tests and the Rules sequence suite passes 15. Tests cover lifecycle/failed attempts, all pending families, unknown metadata, causal links and missing-link legacy replay, anonymous-counter ownership, cleanup through the existing projector, history/restart/retry, hidden-world invariance and real recorded Gusts retained at AFPh without mutation.

### B5 packing and replenishment progress slice, implemented 2026-10-27

`TurnWorkProgress.Assess` adds a headless internal evidence ledger. It shares the stable `TurnObligationKey` identity with the obligation model and preserves linked participant IDs, the work kind, original B#, origin sequence, first Game Turn, eligibility and interval receipts. This is a trusted host contract, not a client-writable progress counter. Callers must prove each receipt came from the corresponding first RPh or second CCPh and disclose any resulting instances before returning them to players. The current host has no such authoritative declarations/events, so it exposes gaps only rather than interpreting TI markers as work commitments.

**Complete intervals and replay.** A countable interval has both first-RPh and second-CCPh receipts and is not interrupted. A first boundary alone, second boundary alone or unknown boundaries is incomplete. Interrupted intervals never add progress, and previously completed Game Turns remain counted. The ledger rejects duplicate work origins, wrong games, future/out-of-history receipts, duplicate Game Turns or boundary IDs, reversed/equal endpoints and overlapping known boundaries, including incomplete intervals. Repeated reads and serialization/reload reconstruct the same result. These tests prove the evidence contract; they are not a claim of new Units journal events or live save migration.

**Rules ownership.** `TurnWorkRules` owns the E10.3 table: original B# at most 9, 10 and 11 require one, two and three complete Game Turns respectively. Missing/out-of-scope B# leaves duration unknown. E10.4 offload/transfer uses four complete Game Turns. Verified accumulated progress reaching its duration yields `CompletionPending` only at the second CCPh window, never automatic resolution. Unknown eligibility/origin remains NotEvaluated; current ineligibility reports Interrupted while retaining earlier counts. Nothing rolls the supply depletion DR, clears TI, restores ammunition or moves an Ammo Supply counter.

The source was checked in the local Chapter E markdown, E10.3-.4, and its page-244 replenishment table image (`eASLRB_v3_01-p244-3.png`). E10.3 requires CE and preserves the distinction between complete prior turns and an interrupted turn; MP expenditure, firing and CC attack remain rule-specific eligibility evidence for a future adapter. Compatible supply/locations, up to two identically armed recipients, special ammunition, depletion on Original 12 and supply/portage consequences still require governed integration.

**Packing is explicitly incomplete.** Animal Packing can retain evidenced complete, interrupted and incomplete intervals, but no base duration is guessed. G10.11/G10.3-.31 duration/minimum, night doubling, linked Gun/Mule/crew roles, midpoint loading/unloading and reversal remain pending. Its release belongs at next first RPh ASOP 1.13B after sufficient complete Game Turns, not ordinary CCPh TI cleanup. The current Rules evaluator therefore returns an unknown packing duration and does not announce completion. The reviewed catalog remains the reference for these pending requirements.

**Host/profile boundary.** `existing-engine-work-v1` retains the earlier fire/atmospheric history and returns five `WorkProgressGaps`. It does not expose internal ledger keys, participants, receipts or a generic start/update/resolve command. The `pack` and `ammo` dependency command capabilities remain Pending in every profile. All 538 topics and 38 dependencies remain visible. B3's older `TurnProgressReceipt` alone cannot prove these two-boundary intervals and is not silently promoted to sufficient evidence.

Next integration needs typed, rule-specific Play/Units declarations, participant roles and eligibility histories; authoritative complete-interval/interruption receipts; packing duration/midpoint/reversal rules; governed completion and depletion consequences; and authenticated instance disclosure. No new rule-family gate or mutation owner has been introduced, and the full UI readiness gate remains pending.

Validation: 146 ASL.Turns tests pass, with 651/651 executable lines and 366/366 branches covered (100%, no source exclusions). The focused Rules suite passes 28 tests; 4 host-guide tests pass, including the work profile's pending visibility and hidden-world invariance. Coverage applies to the entire ASL.Turns assembly, not the existing Rules or Play assemblies.

## 9. Gate before building Blazor

Headless tests must demonstrate:

1. Complete reviewed reference inventory and pending-rule visibility.
2. Stable typed request/results and capability/source distinctions.
3. At least one supported live decision path and cross-phase lifecycle with origin, due window, interruption and resolution evidence.
4. No UI dependency, side effects from reads or alternate command authority.
5. Viewer-safe output, correct decision ownership and stale request rejection.
6. Shared phase-close semantics and exactly-once replay for the admitted slice.

Only then build a thin consumer using existing site styling and action panels. Client consideration/bookmarks remain private preferences, not domain lifecycle. Full eventual rule coverage continues through the planned Rules passes; do not label the initial slice as complete ASL execution.

## 10. Test ownership

`ASL.Turns.Tests` owns content, coordination and backend contracts. Existing Rules tests own rule verdicts; Units tests own events/projection/replay; Play tests own governed command enforcement and adapters. Add renderer tests only at U1. Run the narrow project/class filters relevant to each change, with no blanket whole-solution test requirement.

Next work: further B5 rule-family slices and a supported live topic decision route, with finer side disclosure where supported. Verify production capability registrations against actual Rules/Play proofs before admitting live support. The repository mock-up is preserved as design evidence, not a requirement to construct UI now.

B4 final verification: 123 ASL.Turns tests and 77 focused Play regression tests pass. Coverage is 100% for ASL.Turns, not a claim of full Play assembly coverage.

B5 final focused verification: 127 Turns tests, 35 Rules tests, 69 Units tests and 79 Play tests pass. The Turns coverage gate measures 465/465 lines and 264/264 branches, both 100%.

B5 fire/Spotter final validation: 131 Turns tests, 39 Rules tests and 45 Play tests pass. ASL.Turns coverage is 516/516 lines and 288/288 branches (100%).


### B5 live work adjudication and completion slice, implemented 2026-10-27

This slice adds live, persisted work lifecycles. It is an **adjudication integration**, not full automated implementation of G10 or E10. The previous progress-only section describes the earlier baseline; its statement that no live work records exist is superseded here.

**Authority and missing facts.** `asl.game.record-turn-work` requires the separate `asl.game.adjudicate-work` permission. Ordinary play permission does not authorize it, and no UI is added. A host must grant that permission only to its trusted adjudicator. The authenticated execution gate still checks permission, confirmation, expected revision and attempt identity. `proposedBy` does not grant permission. A start records identity, owner, subject, distinct participants, kind, original B# and a nonempty rule basis. Packing additionally requires an explicit adjudicated whole-Game-Turn duration. This value records an external ruling, including any night/minimum adjustment; it is not a newly implemented packing-duration formula. A missing duration is refused. E10.3 replenishment still derives one/two/three turns from original B# <=9/10/11, and E10.4 transfer derives four, without a duration override.

**Events and replay.** Units reads and writes typed `turn-work-recorded` journal payloads for start, interrupt, resume, cancel and complete. `GameState.TurnWork` is projected from those records and actual phase changes; old records yield an empty list, never invented work from a TI marker. First-RPh starts/resumptions open a whole-Game-Turn interval. Both Player Turns must pass in order, including both CCPhs. Replay rejects skipped phase boundaries. Interrupted intervals do not accrue progress; earlier completed intervals remain in the adjudicated ledger. Packing resumption itself remains an external adjudication, including whether prior progress is reusable. Cancel/start a new identity when the ruling requires restarting. Completed/cancelled identities cannot be completed again or reused.

**TI ownership and interruptions.** A declaration cannot take over existing TI or share participants with another open work assignment. This deliberately leaves simultaneous two-vehicle replenishment pending. The projector retains work-owned TI across ordinary CCPh cleanup and clears it on interruption, cancellation or completion. Participant removal/location change, movement expenditure, firing, a recorded CC attack, explicit TI removal or subject buttoning-up interrupt the interval. Unrelated concealment updates do not. Other packing-specific eligibility changes, role changes, supply depletion from another operation, and exact ASOP substep eligibility still require adjudicator intervention. Ordinary GameState phase data cannot prove the declaration occurred before every relevant first-RPh activity.

**Completion and phase closure.** Replenishment/transfer become due in the second CCPh after the requisite full intervals, including that current interval. Packing becomes due in the following first RPh, not the preceding CCPh. Rules owns the transition predicates. Play phase-close planning, the guide's shared close assessment and Units replay refuse to leave a due release window until the work is completed, interrupted or cancelled. Replenishment completion uses a system-generated two-die Ammunition DR inside the atomic store commit. Original 12 records supply depletion. The journal validates the referenced roll and prevents its reuse. Planning/guide reads never draw dice. Repeated committed attempts replay without a second roll or completion.

**What completion means here.** The work lifecycle becomes completed, its owned TI is released, and the Ammunition DR/depletion result is durable. This slice does **not** mutate packing containment, Gun/Mule/crew roles, midpoint loads/unloads, ammunition availability in combat, Ammo Supply counters, dumps, wrecks or portage capacities. Those effects need their rule-specific model and governed consequences. A completion record must not be presented as full packing or ammo rules integration. No substitute arbitrary counter-mutation endpoint is exposed.

**Disclosure and profile.** `existing-engine-work-v1` now has version `b5-work-2` and exposes `TurnHostGuide.Work`. The adjudicator can read all work; a side receives only its own work with all participants currently disclosed and all recorded work changes visible. Raw journal IDs, dice and adjudicator notes are absent from the guide. Historical reads use the selected revision. Work summaries always carry `RuleAutomation = Pending`; broad `pack` and `ammo` dependency support remains Pending. The catalog still contains all 538 topics, 38 dependencies and 191 links.

**Source and remaining work.** E10.3-.4 and the local Ammo Replenishment Table support the replenishment interval and DR mechanics. The cross-phase catalog identifies G10.11/G10.3-.31 and first-RPh ASOP 1.13B. The full Chapter G packing source remains absent from the local Markdown set. Next admission work must reconcile that source, implement duration/night/minimum/midpoint/reversal rules, model participant roles and compatible supplies, integrate Low Ammo/special ammo and supply effects, support shared supply vehicles, and replace external eligibility attestations with rule-owned host facts. These gaps remain visible now.

**Validation.** ASL.Turns passes 147 tests with 659/659 lines and 366/366 branches covered (100%, no source exclusions). Live host tests exercise permission denial, persistence through FileGameStore and JSON round-trip, historical/owner/opponent disclosure, replay-safe completion, system dice, retained TI through both Player Turns, interruption/resumption/cancellation, invalid declarations, duplicate identity and skipped-boundary rejection. Rules tests cover replenishment, four-turn transfer, packing's externally supplied duration and following-first-RPh release, invalid transitions and depletion.


Additional validation: 33 focused Rules tests, 421 Units regression tests, and 9 focused Play/guide/action-registration tests pass. The full Play run passed 1,067 tests and identified the registration-list expectation for the new action; that expectation was updated and passed in the final focused run. The final TI ownership regression also passes: cancellation of interrupted work preserves independently acquired TI. Hidden Ammunition DR evidence suppresses the corresponding side summary.


### B5 packing and replenishment completion effects, implemented 2026-10-27

This slice extends the permissioned work-adjudication route with **opt-in completion effects**. It supersedes the preceding slice's statement that completion changes only the work lifecycle and TI. It does not certify G10 packing eligibility/duration or all E10 supply rules. Broad `pack` and `ammo` rule automation stays Pending, and every reviewed option remains in the catalog.

**Bind the effect at declaration.** `TurnWorkDeclaration.CompletionEffects` holds a typed `TurnWorkEffectSpec`. Its closed choices are `PackGun`, `UnpackGun`, and `Replenish`; each binds the exact crew or supply identity/vehicle within the original participants. Rules validates kind/role consistency and derives the completion result. The complete operation takes no replacement declaration or arbitrary mutation list. Old records with no specification remain lifecycle-only, even if already completed. No migration guesses effects from TI, elapsed time, participants, or a historical completion record.

**Packing assembly and manning.** An adjudicated PackGun completion records the Gun in `GameState.PackedGuns` and removes its manning link. UnpackGun removes that assembly state and mans it with the crew bound at declaration. The Gun stays in its existing map Location. Both declaration and completion require an active own Gun and Good Order own Personnel in that Location, with the expected prior assembly/manning state. Ordinary fire, turn, recovery/transfer, towing and Manhandling routes cannot bypass packed state: the planner rejects requests naming the packed Gun and authoritative replay rejects the corresponding ordinary use/transfer/move events. Animal loading, containment and transport are not inferred from this assembly change. Those depend on the separate midpoint/animal rules and remain pending.

**Durable supply and Low Ammo facts.** `GameState.TurnAmmoSupplies` stores typed counter identities, their owner, bound vehicle and availability; depleted identities remain as unavailable records rather than being reusable. `LowAmmoVehicles` stores explicit adjudicated Low Ammo facts. On a replenishment declaration, `CreateSupply` can record an adjudicated new supply counter with a fresh identity; `RecordLowAmmo` can record the subject's externally adjudicated depletion. These are explicit factual assertions under `asl.game.adjudicate-work`, not facts inferred from a weapon's special-ammo depletion. Without those flags, the facts must already exist. Existing identities cannot be overwritten, and one vehicle cannot acquire a second available supply through this route. Invalid declarations publish none of the proposed facts. Cancelling work does not erase independently recorded supply or Low Ammo facts.

**Replenishment effects.** This admitted path requires active own vehicles in the same Location, an available supply bound to the declared source vehicle, recorded Low Ammo on the recipient, and no Passengers in the supply vehicle. Accessible-but-different Locations remain pending. Completion clears that recipient's Low Ammo fact and its entries in the existing C8.9 `DepletedAmmunition` table; other vehicles are unchanged. This restores the actual special-ammunition choices used by the existing ordnance planner. The Ammunition DR is still generated inside the atomic store commit. Original 12 makes the bound supply unavailable; it does not undo the recipient's successful replenishment. A depleted supply cannot support another declaration unless a separately adjudicated new counter with a fresh identity is recorded.

**Passenger capacity.** Rules returns zero usable Passenger PP while a vehicle has an available Ammo Supply counter (E10.12). `GamePlanner.PassengerCapacity` uses that predicate, so the existing boarding/towing capacity checks observe it. When replenishment depletes that supply, capacity is recalculated from the vehicle's ordinary current definition and towing state, not from a cached pre-work number. This implements supply depletion's capacity effect, not a supply-transfer or portage editor.

**Atomicity and visibility.** The versioned `turn-work-effects-recorded` completion event projects lifecycle completion, effects, the consumed roll and TI release together. All operations on effect-bearing work use that event type and the `asl.turn-work-effects.v1` package. Older journal readers reject the unknown event type instead of silently ignoring its effect specification. Current replay rejects a downgrade to the legacy event type; lifecycle-only work retains `turn-work-recorded`. Hosts must support profile version `b5-work-3` before offering effect specifications. Current participant/resource facts are rechecked before effects. If they no longer match, replay and the store reject the entire completion, without clearing TI or partially applying effects. Attempt replay does not run effects twice. Guide summaries expose `EffectKind` and `EffectStatus` (`NotSpecified`, `AwaitingCompletion`, `Applied`, `Cancelled`) only through the existing authorized work projection. They disclose no new source IDs or adjudicator notes. `RuleAutomation` remains Pending even when the bound effects are Applied. `existing-engine-work-v1` now has profile version `b5-work-3`.

**Remaining all-Passes work.** Automatic packing duration/night/minimum/eligibility, midpoint/reversal, Gun/Mule/crew transport roles, animal loading/unloading and transport remain pending. The local Markdown set still lacks the full Chapter G source; PackGun/UnpackGun are explicitly recorded assembly adjudications. E10 eligibility, caliber/barrel compatibility, simultaneous two-recipient supply, Accessible Locations, Ammo Dumps, four-turn transfer/offload counter relocation, immobilized-source wreck effects, supply destruction/capture/explosion and movement restrictions remain pending. Low Ammo's D3.71/B# firing behavior and automatic Low Ammo generation are not integrated by this slice; C8.9 special-ammo restoration is. Supply counters are authoritative typed state records; general counter rendering/selection remains future UI/host work. The full catalog remains 538 topics, 38 dependencies and 191 links.

**Verification.** Tests cover assembly/manning round-trip and replay, packed-Gun rejection, frozen effect targets, stale-crew rejection with an unchanged persisted journal, and legacy records with unspecified effects. Fixed-dice host tests cause real APCR depletion through the ordnance planner, complete replenishment with both non-depleting and Original-12 DRs, preserve another vehicle's depleted ammo, verify Passenger capacity, reject duplicate/depleted supply reuse, reload the journal, and attempt APCR again through the normal fire path. Headless disclosure tests distinguish pending, applied, cancelled and unspecified effects without promoting rule-family support.


Completion-effects validation: 148 ASL.Turns tests pass with 666/666 lines and 374/374 branches covered (100%, no exclusions). The focused Rules suite passes 38 tests, Units passes 421 tests, and the relevant Play/Gun/ordnance/vehicle/guide regression selection passes 57 tests. Journal versioning is additionally checked by the final focused work/guide suite.
