# ASL Turn Phase Guide Design

Date: 2026-10-27

Status: Backend-first design, reopened at the user's direction. `ASL.Turns` and `ASL.Turns.Tests` now provide the tested headless reference catalog, typed capability contracts, validated stable timing schema, pure B2 guidance composition and a bounded B3 obligation assessment slice. B4 adds Play host adapters and shared phase-close assessment. B5 adds durable Placed DC ownership, fire/Spotter and atmospheric history, work-progress evidence, permissioned live work adjudication and completion effects, replay and explicit integration profiles. Additional families remain proposed. The B5 acceptance gate is passed; U1 now provides the thin reference and live Blazor consumers.

References: [Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md), [Implementation Plan](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md), [cross-phase audit](ASL%20Cross-Phase%20Dependencies%20and%20Cleanup%20Audit.md), [folder index](README.md).


Current B5 scope (2026-10-27): B5-R1 through B5-R6 are implemented and accepted; no required B5 slices remain and U1 is implemented. The [finite completion backlog](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md#11-finite-b5-completion-backlog-agreed-scope-2026-10-27) is authoritative. B5-D1 through B5-D9 preserve later ASL.Rules work without making full rulebook execution a prerequisite for the initial UI. Historical slice sections describe the evidence at their implementation date, not additional B5 gates.

## 1. Design decision

`ASL.Turns` is the owner of turn-guide content, application-independent guidance contracts, dependency coordination and phase-close assessment composition. It must be useful from tests, a console, an API or a future Blazor client without changing its behavior. A renderer displays its output and submits intentions to the existing command path; it does not decide what an option means or when an obligation expires.

The final target remains ASL.Rules after all passes. All 538 reviewed topics and 38 dependency definitions remain available even when execution is pending. Current integration changes capability and explanation, not content membership. The 191 links are reference relationships, not proof that a particular game has 191 obligations.

This design supersedes the earlier proposal to house the catalog and guide models in MapStudio and to build Reference UI before the headless mechanism. It also moves the proposed guide reader's coordination responsibility out of Play. Play retains the adapters that read actual game state and execute governed commands.

## 2. Projects and permitted dependencies

| Project | Owns | Must not own |
|---|---|---|
| `ASL.Turns` | Immutable authored catalog, stable topic/dependency identities, typed guidance and timing contracts, rule-result composition, obligation views and phase-close assessment | Blazor, DOM, CSS, navigation, UI preferences, game-store writes, dice rolling, raw command execution or a second ASL rules engine |
| `ASL.Turns.Tests` | Headless content, contracts, lifecycle, timing, missing-capability and disclosure-composition tests | A dependency on MapStudio or bUnit merely to exercise the backend |
| Existing `LimboDancer.Domains.Asl.Rules` | Rule-specific eligibility, timing, interruption, cleanup and transition verdicts over narrow facts | UI text catalogs, Units/Play state objects or a reference back to ASL.Turns |
| Existing Units | Authoritative event payloads, history, projected game facts and replay validation | User consideration or a mutable guide-owned game state |
| Existing Play | Adapt state/history into facts, authenticated perspective policy, existing planning/proposal/confirmation, event construction and commit | Duplicate rules or an alternative guide-only write path |
| Future renderer/MapStudio | Display snapshots, map selections, private browsing/consideration and route requests | Option discovery, expiry calculations, legality, cleanup or phase advancement semantics |

Intended compile-time direction: `MapStudio -> Play -> ASL.Turns -> Rules`, with existing `Play -> Units -> Rules`. `ASL.Turns` must not reference Play, Units or MapStudio. B3 adds the Rules project reference because obligation assessment now calls rule-owned timing predicates. The initial B0 catalog required no project references. Never make Rules call ASL.Turns to resolve a cycle.

Play translates existing state types into narrow facts. Rule fact/verdict records belong to Rules when their meaning is rule-specific. ASL.Turns owns guide request/result and obligation coordination records. This allows reuse of the same Rules predicates by projector validation and guide evaluation without duplicating them.

## 3. Current foundation versus planned behavior

Implemented in this revision:

- `src/ASL/ASL.Turns/ASL.Turns.csproj`, namespace `ASL.Turns`.
- `TurnGuideCatalog.Load()`, immutable phases/topics/glossaries/ASOP ledgers/dependency definitions, exact qualified lookup and related-dependency lookup.
- The nine reviewed JSON source files embedded from `docs/Turn.Guide`. There is no runtime filesystem/PDF path requirement or MapStudio resource dependency.
- Qualified topic identity, for example `ccph:hiddenHero`, and ordinal case handling. Source hash/edition/scope and pending integration prose remain available.
- Tests in `src/ASL/tests/ASL.Turns.Tests`, namespace `ASL.Turns.Tests`; both projects are in the ASL solution with normal lock files and build conventions.

The current loader recognizes the existing Rally schema 2 and other schema 1 documents. It validates group/term references, page bounds, dependency targets and available ASOP ledger links. B1 imports all ten Rally steps from the reviewed walkthrough into its JSON ledger. Legacy inputs without a ledger remain readable by the catalog loader, but do not satisfy the current timing manifest. Numeric group positions and free-text status notes remain authored input, not final stable timing IDs or capability verdicts.

B3 evaluates four timing families in Rules. B4 derives Stun/CX evidence from real Play history and shares the governed phase-close planning path. All-family timing, migrations, finer side readiness disclosure and UI components remain pending. See the B4 contract below.

## 4. Backend API before UI work

B2 composition and B4 disclosure projection are implemented:

```csharp
TurnGuideSnapshot Describe(TurnGuideRequest request, TurnFacts facts);
TurnPhaseCloseAssessment TurnPhaseClose.Describe(TurnClosePlanFacts facts, bool adjudicator, bool historical);
```

B2 implements a concrete `TurnGuideService` over immutable externally supplied verdicts. Future Rules adapters supply those verdicts; there is no general-purpose plugin engine. A request identifies game scope, internal evaluation stamp, actual and browsed phase/window, viewer/acting side, scenario profile and optional disclosed subject. `TurnFacts` is an immutable, typed adapter result, not a `GameState`, arbitrary dictionary, serialized UI state or callback into a page.

The internal stamp includes event revision, map/catalog identity, rule/capability version, scenario profile, viewpoint and window identity. It is used for cache correctness and stale rejection. A renderer receives a viewer-safe opaque handle, not a full-state revision stream that exposes hidden activity.

`TurnGuideSnapshot` contains:

- Actual phase and browsed phase, the current decision owner and semantic window.
- Complete authored phase options, with separate applicable instances and capability results. Unbound options are retained with Not evaluated.
- Structured attention groups: verified required decisions, verified optional choices, evaluation gaps, due effects, retained effects and future commitments.
- Viewer-safe subjects/reasons/source references, links to dependency definitions and opaque action-route handles when an executable path exists.
- An explicit coverage summary. Absence of verified blockers never means all ASL rules were checked.

No result contains CSS classes, button captions as command identifiers, component names, URL routes or `EventCallback`. Human-readable explanation content is allowed; renderer-specific presentation is not. Output ordering follows semantic windows and rule constraints, not alphabetical labels or numeric ASOP sorting alone.

The service is deterministic for fixed inputs. A read does not change a game, roll dice, allocate a governed attempt or increment progress. Browsing a future phase never changes the actual window.

### B2 implemented contract, 2026-10-27

`new TurnGuideService(catalog, registrations)` validates the capability registrations and stable timing manifest once. `Describe(request, facts)` has no service callbacks, game store, clock, random source, command executor or UI dependency. The default host passes an empty registration collection until an adapter has verified evidence.

`TurnEvaluationContext` identifies game, authenticated viewer, viewer side, profile, actual phase, semantic window and internal evaluation stamp. `TurnGuideRequest` adds browsed phase and historical mode. `TurnFacts` carries the same context and immutable authorized verdicts. Exact context mismatch rejects stale or cross-game/viewer/profile/window input. Required context strings, current phase/window ownership and browsed phase are validated. The host must include fact, catalog and capability versions in its stamp.

`TurnGuideVerdict` is a trusted adapter contract: an opaque decision ID, audience viewer ID, capability ID, authorized subject ID, current window, decision owner, applicability, safe explanation and optional opaque action route. This is already authorized content, not unrestricted game state. The host must use authenticated identity and GameView-equivalent disclosure before constructing it. Caller-supplied viewer IDs or stamps are not credentials, and support evidence is not runtime proof of a rule verdict. Production topic-verdict registrations remain pending; B4 adds history and closure adapters.

Other audiences are removed before content validation, counts or output. Visible duplicate decision IDs and invalid applicability values fail. Only exact-profile registrations with Partial or IntegratedSubset evaluator support can supply decisions, and only verdicts matching the current window participate. Unknown registrations and unrelated windows cannot create legality. A missing subject returns NotEvaluated with a generic reason and no route; side-wide decisions must use an explicitly authorized game/side subject handle.

Each snapshot retains every authored topic of the browsed phase, including pending/deferred/source-gap topics. It includes original integration notes and checks, profile-specific capability registrations, timing bindings, relevant dependency definitions, source metadata, glossary definitions, stable groups and windows. A topic without a verified assessment receives an explicit evaluation gap. Presence of one supported sub-action does not erase its topic's pending prose or establish universal support.

Decision attention distinguishes EvaluationGap, RequiredDecision, OptionalChoice and Informational. It is a neutral enum, not CSS or UI state. Decisions retain the supplied safe explanation and decision owner. Visible decisions sort by ordinal opaque ID for deterministic composition; this is display stability, not rule execution ordering. Reference topics remain in authored order. Obligations and semantic execution order are still B3 responsibilities.

An action route survives only for a nonhistorical read of the actual phase, when the decision belongs to the viewer's side, command support is Partial/IntegratedSubset, and applicability is Available/Required. The host still owns route creation, scope/expiry checks and governed command revalidation. The service never executes a route. Browsing and historical output cannot advance the game or mark consideration as resolution.

Snapshots expose neither the internal evaluation stamp nor other audiences' identifiers, reasons or counts. Equivalent visible inputs with different hidden verdicts and internal stamps produce identical serialized snapshots. The host must also ensure its own route handles and safe strings do not encode hidden revision churn. Every snapshot explicitly states that supplied assessments do not establish complete ASL legality or phase closure.

Example reference-only composition, with all current Rally options retained:

```csharp
var catalog = TurnGuideCatalog.Load();
var guide = new TurnGuideService(catalog, []);
var context = new TurnEvaluationContext("game-1", "viewer-1", "side-a",
    "base", "rph", "rph:asop:1.23B", "host-evaluation-stamp");
var snapshot = guide.Describe(new TurnGuideRequest(context, "rph", false),
    new TurnFacts(context, []));
```

B2 validation: **88 tests pass; 336/336 executable lines and 176/176 branches covered (100% each)** across ASL.Turns. Behavioral tests cover all eight phase inventories, required versus optional decisions, unknown subjects and capability gaps, profile/window filtering, stale scopes, historical/browsed/other-side route suppression, deterministic reads, unchanged input facts and hidden-world equivalence. Movement Reaction Fire and compulsory Rout fixtures demonstrate composition of host verdicts, not new Rules integration or validation of those rules.

## 5. Authored content and capabilities

The reviewed source documents live together in `docs/Turn.Guide`. Keep the eight phase catalogs and dependency catalog as the current authored source of truth. The library embeds them directly for now. Introduce a normalized generated resource only when the stable group/timing IDs, term crosswalk and typed source/capability schema justify it; do not maintain a hand-edited second catalog in MapStudio.

Keep `<phase>:<local-id>` topic IDs and dependency IDs stable. Map group positions to reviewed stable group IDs before executable timing binds to them. Support aliases for renamed topics. Preserve printed ASOP locators and distinguish supplemental semantic events. Do not invent ASOP numbers to fill older schema gaps.

Terms can reference reviewed acronym IDs or explicit concepts. Preserve `DR` versus `dr`; never merge phase glossaries by last-write-wins. Review inherited contextual definitions and link them to the separate acronym catalog through IDs, without taking a dependency on its current MapStudio implementation. A future shared acronym service can move independently if required; it is not needed to bootstrap ASL.Turns.

Typed capability records must separate source review, evaluator support, command support, scenario/nationality/equipment subset and required evidence tests. English Partial/Pending labels are reference notes, never executable logic. Default missing registration to Not evaluated, not Not applicable.

| Independent dimension | Examples |
|---|---|
| Source review | Reconciled; interpretation open; source missing |
| Implementation | Integrated subset; partial; pending; deferred |
| Applicability | Not evaluated; available; required; unavailable with safe reason; not applicable |
| Domain lifecycle | Declared; committed; scheduled; due; resolved; cancelled; interrupted |
| Private consideration | Unreviewed; considered; bookmarked, stored by the eventual client outside game state |

A partial topic can expose a proven supported sub-action while keeping other choices visibly pending. Unsupported context does not remove a topic, generate an invented obligation or make a manual consideration mark authoritative.

### B1 contracts implemented, 2026-10-27

`TurnCapabilityCatalog.Create(catalog, registrations)` retains every topic in authored order. Each result carries the original reference topic and zero or more explicitly scoped registrations. `UnregisteredSupport` and `Applicability` are `NotEvaluated`. An empty registration list never means Not applicable, unavailable, or complete. These are reference assessments, not live legality verdicts.

`TurnCapability` identifies a sub-action, topic and exact profile, with independent `TurnSourceReview`, evaluator support, command support and evidence IDs. Source review supports NotReviewed, Reconciled, InterpretationOpen and SourceMissing. Support distinguishes NotEvaluated, Pending, Deferred, Partial and IntegratedSubset. No unqualified Integrated value claims universal ASL execution. Partial and IntegratedSubset require nonempty evidence; invalid enum values, duplicate IDs, unknown topics and blank profiles/evidence fail validation. Evidence strings identify proofs for review, not code that this library executes.

Production registrations are intentionally empty at this boundary. Existing Rules features have not yet been proven through a Turns adapter. In particular, absence of a registration does not downgrade the existing Rules engine. Register a verified sub-action/profile with its tests during integration. The authored pending/deferred notes, campaign source gaps and prisoner-abandonment interpretation remain visible unchanged. Never populate registrations by parsing those notes.

The checked-in [timing manifest](ASL%20Turn%20Timing%20Schema.json), schema version 1, assigns **117 stable groups, 176 reference windows and 538 topic bindings**. Group IDs were assigned once and are now immutable keys; label edits or numeric group reordering must not regenerate them. Topic IDs remain ordinal `<phase>:<local-id>`. Printed window IDs are `<phase>:asop:<printed-step>`, preserving the suffix and edition recorded by the phase source. Supplemental IDs are `<phase>:supplemental:<topic-id>` and explicitly `SupplementalUnclassified`. They are placeholders for source traceability, not invented printed steps or executable schedules.

The manifest stores identities and links, not duplicate topic text, rule prose or glossary definitions. The loader checks version, uniqueness, phase ownership and exact correspondence with every ledger topic link. It rejects missing topics, missing multi-window links, duplicate window references, unrelated supplemental references and unused windows. Rally ski handling retains both 1.14B and 1.22B. Source-derived windows do not establish a total order: nested effects, repeated expenditures and within-step choices still require Rules predicates.

`TurnTermId(PhaseId, Term)` preserves contextual identity and case, including `DR` versus `dr`. Definitions and source hash/edition/pages remain in the reference catalog. An external acronym crosswalk requires reviewed IDs; B1 does not guess external IDs or merge phase definitions. No existing IDs were renamed, so no alias migration is required in this revision. Any later rename needs an explicit compatibility mapping and schema review before consumers change.

`TurnDueSpec` defines separate data shapes for AfterEvent, BeforeWindowCloses, AtWindow, NextFriendlyPhase, PlayerTurnEnd, CompletedGameTurns and SourceChanges. Owner identity, origin event and origin Player Turn remain distinct fields. These are proposed immutable payload shapes, not validated live obligations: B3 must validate actual identities, positive interval counts, allowed windows and rule-specific prerequisites before admitting an instance. No dependency prose is converted into a due specification automatically, and all 38 dependency definitions remain visible.

`TurnGuideCatalog.Load(resourceProvider)` also accepts versioned source streams for authoring validation; it disposes supplied streams after reading. `TurnTimingSchema.Read(stream, catalog)` validates a manifest without modifying it and leaves the caller's input stream ownership unchanged. Raw record constructors are data containers; consumers must use validated loading before relying on authored bindings.

### Required coverage gate

Run `./src/ASL/tests/ASL.Turns.Tests/Test-Coverage.ps1` from PowerShell. It collects coverage for the complete ASL.Turns assembly into a fresh results directory and fails unless tests pass and both covered line and branch counts equal their nonzero totals. It rejects absent or ambiguous reports and unexpected assembly scope. There are no source-file or coverage-attribute exclusions in this project's settings.

B1 verification: **62 tests pass; 242/242 executable lines and 132/132 branches covered (100% each)**. Tests include full inventory/pending visibility, independent support dimensions, all source-loader failure branches, malformed manifests, exact multi-window coverage, contextual term identity and owner-relative due payloads. This measures the implemented library, not completion of future rule execution. Generated reports remain ignored build artifacts; rerun the gate after every library change.

## 6. Obligation facts, lifecycle and clocks

Distinguish a descriptive `TurnDependency` from a proposed live `TurnObligation`. A definition can produce zero, one or many instances. Derive existing instances from recorded facts first. If a rule needs facts that are absent, add a reviewed domain event/state extension in Units through the existing Play action path, not a mutable obligation file in ASL.Turns.

An instance requires stable ID from game, originating event/attempt, rule key, subjects and occurrence; parent event/obligation; owner side and current decision owner; creation Game Turn/Player Turn/window; typed due specification; prerequisites; progress; interruption/completion evidence; capability status and disclosure classification. Do not use the current read revision as its identity.

Proposed due cases: immediate after a named event; before a named window closes; at a semantic ASOP window; next friendly phase; Player Turn end; completed Game-Turn intervals; and source-change trigger. A Player Turn is tied to its recorded start and phasing side. ATTACKER/DEFENDER roles are not persistent owner identities. No wall-clock timers or background scheduler.

Rules evaluates whether an obligation arises, is due, is interrupted or completes. ASL.Turns composes these verdicts into dependency order and a consumer-neutral result. Play uses those results to prepare its existing governed action plan. Units persists the actual rule facts and replays committed events. There is one authoritative state, not a second guide state machine that can disagree with it.

Required proof cases:

| Chain | Facts and boundary to preserve |
|---|---|
| CX | Release event and prior-CX history; marker removal does not restore Double Time permission. |
| Shock/Stun | Next-RPh recuperation versus end-turn flip; no duplicated boundary effect. |
| Trip Flare | Activation phase and Player Turn; late-phase flare survives to next AFPh. |
| Clearance | Task, participants, target and eligibility before Pin/TI cleanup; Flame Hamper is immediate and distinct. |
| Packing/replenishment | Complete Game-Turn intervals and interruptions; preserve completed intervals and exact release window. |
| Smoke/drift | Stable source/owner, dispersal and weather changes; cave disclosure and dependent drift removal. |
| CC | Location/round, simultaneous commitments, sequential result windows and concealed information. |

Optional expiring opportunities and personal reminders are not mandatory obligations. A failed attempt can be resolved, while a successful attack with retained ROF can leave another opportunity. Returning an item as Resolved requires domain evidence, never a UI checkbox.

### B3 implemented slice, 2026-10-27

`TurnObligationService.Assess(read, candidates)` derives an immutable assessment from supplied domain evidence. The new compile-time reference is `ASL.Turns -> LimboDancer.Domains.Asl.Rules`; neither Rules nor Turns references Play or UI. `TurnObligationRules.Evaluate` owns the timing/lifecycle predicates. It reuses `ScenarioA1SequenceProjection.StunEnds`, `LosesCx` and `CxRests`, preserving existing projector ownership of mutations.

This is an internal host API, not a viewer-safe endpoint. Candidate subjects, origins, dependency graphs and receipts may contain hidden information. Do not attach its raw results to a B2 snapshot or send them to a renderer. B4 must authenticate the requester, scope/version the facts and project permitted data, including aggregate counts and missing-fact reasons, through the established disclosure policy.

| Family | Implemented timing assessment | Boundary and limit |
|---|---|---|
| Stun/Recall | Normal Player Turn cleanup using the existing Rules consequence predicate; overdue at a later Player Turn | Ordinary status cleanup at 8.43B, after combat/work. Missing markers without resolution evidence return NotEvaluated; no duplicate flip or CE restoration. |
| CX | Immediate broken/berserk release; subsequent friendly Player Turn PFPh completion/Opportunity Fire, TI, MPh release and overdue later phases | Retain permanent owner identity and the originating CX record. Counter removal does not establish renewed Double Time permission. No new mutation path is added. |
| Trip Flare | MPh origin waits through ordinary CCPh cleanup until concealment; RtPh/APh/CCPh origin waits until the next Player Turn AFPh end | Unsupported activation phases stay NotEvaluated. This evaluates expiry of an already-qualified activation, not activation eligibility or illumination attacks. |
| Non-Flame Clearance | Qualified MPh/DFPh declaration waits until 8.42B; eligibility loss interrupts, unknown eligibility stays NotEvaluated | Before ordinary Pin/TI removal. Eligibility input must describe the task's full participation history, including disqualifying interruptions, not merely current markers. No Clearance roll or task-specific eligibility engine is introduced. |

The source basis is the existing [cross-phase audit](ASL%20Cross-Phase%20Dependencies%20and%20Cleanup%20Audit.md): A4.51-.52, D5.34/.341, E1.95-.951, B24.7-.8 and ASOP 8.42B/8.43B. The slice assumes a qualifying recorded origin within those reviewed profiles. SSR/module exceptions and additional task conditions remain source/profile-gated by the future host adapter.

Every assessment retains all **38 dependency definitions**. The four admitted families report Partial timing support; every other family reports Pending and NotEvaluated. Command support is Pending for every obligation until B4 proves its existing governed route. The 538-topic reference catalog and its pending notes remain unchanged. No registration claims full ASL or scenario coverage.

`TurnObligationKey` is a structural identity: game, originating event, definition, subject and occurrence. It is independent of read revision and display phase. Candidate facts retain owner side separately from current decision owner, origin position/sequence, subject facts, progress receipts, optional terminal evidence and prerequisite keys. Multiple instances of one definition never share an identity merely because they concern the same phase.

`TurnRulePosition` carries Game Turn, absolute Player Turn ordinal, permanent phasing side, phase and semantic checkpoint. These clocks are independent; two Player Turns do not count as two completed Game Turns. Rules validates known phases/checkpoints, positive clocks, origin not after evaluation, and consistent same-Player-Turn phase/side/Game Turn facts. Checkpoints are typed semantic events, not numeric ASOP sorting or wall-clock timers. The host must supply a coherent domain-history position, including checkpoint ordering within a phase.

The evaluator returns a typed `TurnRuleDeadline` alongside its verdict: PlayerTurnCleanup, OwnerReleaseEvents, AfterConcealment, NextAdvancingFireEnd or ClearanceAfterCombat, with earliest Player Turn and permanent owner. These Rules-owned descriptions refine the B1 proposed due payloads for this slice; they are not generic scheduling commands. `Scheduled`, `Due`, `Resolved`, `Interrupted`, `Cancelled` and `NotEvaluated` remain separate states. Due is an assessment, never permission to mutate.

Resolution, interruption and cancellation receipts require a domain event ID and an in-range sequence. Marker absence cannot substitute for a receipt. Known terminal evidence takes precedence over current marker changes, so provenance and release history survive reload. Missing legacy origins stay NotEvaluated rather than receiving invented creation turns. For Clearance, the supplied eligibility-through-history may also identify an interruption before a terminal receipt is available; a later adapter must not erase that interruption when markers clear.

Progress is a list of evidenced completed Game-Turn intervals tied to the originating task. Validation rejects duplicate interval numbers, pre-origin/future progress and progress after terminal evidence. Repeated reads, another Player Turn in the same Game Turn and replay do not increment it. Interrupted work retains prior receipts. Packing/replenishment duration semantics are still pending: retaining an interval receipt is not an evaluator for those unsupported rules.

The coordinator deterministically orders prerequisites before dependents, rejects missing prerequisite instances and cycles, and preserves stable ordering for unrelated origins. A dependent remains waiting unless each prerequisite is resolved. Interrupted or cancelled prerequisites do not silently satisfy a required consequence. Parent resolution before its child resolution evidence is rejected. The prerequisites themselves are trusted rule-adapter facts; B3 does not infer arbitrary cross-rule dependencies from prose. Clearance-before-Stun and child-before-parent tests demonstrate coordination, not automatic construction of every cleanup graph.

There is no mutable obligation store, event writer, background scheduler, automatic cleanup or new Units event in this revision. Save/reload tests round-trip the supplied evidence contract and reconstruct the same result. They prove deterministic derivation and no duplicate read effects, not end-to-end Units save migration. Existing projector cleanup remains its only mutation owner; B4 now shares phase-close planning and proves live Stun/CX cleanup parity; further family integrations remain pending.

Validation: **116 ASL.Turns tests pass, 425/425 executable lines and 234/234 branches covered (100% each)**. The focused Rules run also passes **28 tests**, including the new timing boundaries and existing sequence regressions. Run the existing Turns coverage gate and the Rules tests filtered to `TurnObligationRulesTests|ScenarioA1SequenceRulesTests`. Coverage scope remains the entire ASL.Turns assembly without source exclusions; this is not a claim of 100% coverage for the much larger existing Rules assembly.

## 7. Replay, nested effects and persistence

Use existing events and projected facts as the source of truth. Derived obligation caches are disposable. Record new declaration/activation/progress facts only where a rule cannot be reconstructed from existing evidence. Prefer rule-specific typed events over a generic client-writable `ObligationResolved` event.

Repeated reads, retries and application restart must not reapply cleanup or advance duration counters. Stable event/attempt IDs and existing expected-revision/replay behavior protect commits. Keep a resolution evidence reference for each settled instance. A child consequence must finish before a parent action proceeds when the governing rule requires it.

Existing `PhaseChanged`-derived cleanup remains projector-owned until an explicit family-specific migration says otherwise. Never execute the same effect both as a new explicit event and existing projector cleanup. Evaluate later effects against the resulting facts from earlier effects, including Clearance before status removal and concealment before illumination removal.

Old histories without required origin/progress facts return incomplete evaluation and retain their documented behavior. A rule upgrade that changes replay needs a reviewed compatibility/version decision and fixtures; silently guessing creation turns is prohibited. Existing session-only simultaneous CC declarations remain uncommitted planning unless a separate durable protocol is implemented with proper disclosure.

## 8. Connection to current Rules, Units and Play

Existing seams inspected in the prior design remain the integration anchors:

- `ScenarioA1Definitions` phase identities; `ScenarioA1SequenceCalculator` and `ScenarioA1SequenceProjection` permissions and transition verdicts.
- `GameState`, `GameEvent`, `GameProjector.ChangePhase` and `GameView` for history, projection and disclosure.
- `GamePlanner.BuildPhaseClosePlan`, `GamePlanner.PhaseEndBar`, `PhaseEndedBy` and `GameActions.AdvancePhase` for governed progression.
- `LivePlay`, existing proposal/confirmation and action panels as eventual consumers, not back-end dependencies.

`PhaseEndBar` is not a full closure assessment: it reports proposer-related checks, while `BuildPhaseClosePlan` has further checks and consequences. B4 shares the full planning pipeline. ASL.Turns projects the resulting `TurnPhaseCloseAssessment`; both the read display and authoritative command preparation use the same semantics. The command and projector still independently validate at commit/replay.

An action-route result is an opaque, scoped reference resolved by the authenticated Play host. The host opens its existing action path, supplies permissible selections and revalidates current state. Never embed executable command JSON in the reference catalog. UI-free integration tests can use the same route/intent contract without rendering a component.

## 9. Disclosure and authority

The Play adapter owns access to unrestricted state and the established GameView policy. Rule evaluation may require internal facts unavailable to a viewer; do not infer truth solely from that viewer's incomplete view. Keep internal evaluation records distinct from public guidance records.

ASL.Turns composes authorized guidance from classified results. Before data leaves the authenticated boundary, project only permitted subjects, counts, labels, reasons and routes. Neither hidden raw facts nor revealing negative reasons reach a renderer. Topic presence is static reference content; live instance counts cannot reveal concealed units. Generic waiting-for-the-other-side output replaces private decision details.

Opaque handles are not credentials. Scope them to game, viewer, evaluation stamp and window; recheck on use. Stale state, hand-over, game/map changes and different viewers invalidate them. Do not expose full hidden-state revision churn through handles or notifications visible to an unauthorized viewer. Test equivalent visible worlds with differing hidden facts.

## 10. Closure with incomplete implementation

`PhaseCloseAssessment` separates verified blockers, ordered automatic effects, retained effects, queued future obligations and evaluation gaps.

- Verified supported mandatory decision: existing governed command refuses until resolution.
- Supported automatic effect: existing phase plan applies it at its proper window, exactly once.
- Unreviewed optional topic: no added gate.
- Pending reference topic with unknown applicability: visible Not evaluated; no invented per-unit blocker.
- Proven required effect with unsupported execution: explicit unsupported required effect in the shared planner path when that capability is admitted. No guide-only override or mark-resolved escape hatch.
- Source interpretation/profile missing: preserve reference material and do not expose an unproven executable action.

During incremental integration, the existing engine command gate remains authoritative and coverage limitations are explicit. Do not claim comprehensive legal closure for an unsupported scenario. Existing adjudicator permissions and documented rulings remain unchanged unless separately reviewed and implemented.

## 11. Consumer boundary and readiness for UI

After the headless contract is stable, a Blazor component can render the phase strip, attention groups, topic details, glossary and dependency view. It stores private selection/consideration separately and emits route intentions. Current and browsed phases remain distinct; historical snapshots are read-only.

The saved [design preview](turn-phase-guide.html) is a frozen reference snapshot, not a dependency or implementation of ASL.Turns. Production must not import its JavaScript or reproduce rule logic from the mock-up.

UI construction begins only after headless tests demonstrate complete reference inventory, unknown/pending visibility, a supported live slice, viewer-safe output, stale rejection, representative cross-phase obligations and phase-close parity with the existing command path. Completing the current foundation does not satisfy those later gates.

## B4 host contract, implemented 2026-10-27

Play now references ASL.Turns. GamePlanner.ReadTurnGuideAsync combines the complete phase reference snapshot, disclosed obligations and shared phase-close assessment. AssessTurnPhaseCloseAsync accepts StateStamp, authenticated Perspective and retain-DM selections. These are server APIs, not network endpoints. The caller must authenticate Perspective, as with ViewOf; a client-selected adjudicator is not authorization.

TurnHistoryAdapter derives Stun/CX activation and terminal evidence from actual projected transitions. Repeated reads and planner recreation reconstruct obligations without writes. Historical reads use the requested revision and are read-only. Setup markers have unknown origins and remain NotEvaluated. Other families retain their catalog entries and pending support, including Trip Flare and Clearance whose B3 timing contracts exist but whose host evidence/command integration is not admitted.

The host persists phases, not ASOP substeps. Reserved <phase>:phase context composes reference guidance while suppressing live verdicts and action routes. It never substitutes the first ASOP window. Production topic capability registrations remain empty until specific Rules/command evidence is admitted; all 538 topics and 38 dependency definitions remain available.

Actual phase-advance commands and assessment share the full PlanCoreAsync pipeline and BuildPhaseClosePlan (formerly PlanAdvance), including global guards, proposer checks, phase-specific refusals and consequences. Assessment bypasses only committed-attempt replay detection using an ephemeral, undisclosed probe ID. It never commits or invokes a deferred roll delegate. Existing projector cleanup retains sole mutation ownership. Assessment reports planned event types without creating replacement cleanup events. PhaseEndBar remains a legacy proposer-only check.

Adjudicators receive exact reasons, event-type effects, roll requirement and Ready/Blocked/Stale/ConsequencePending classification. A consequence-only plan is not a completed phase transition. Side views always receive fixed GovernedCheckRequired wording without hidden readiness, reasons, counts or roll flags. They use the existing governed action to confirm closure. Side obligation disclosure requires own visible counters and visible origin/outcome events. Internal revisions, origin IDs and prerequisite graphs never leave the API. Historical views return ReadOnly. Finer side readiness disclosure remains future work.

Validation: the full ASL.Turns coverage gate passes at 452/452 lines and 254/254 branches (100% each), without source exclusions. Host tests compare assessment to command plans across all eight phases over two player turns; prove invalid retain-DM refusal parity beyond PhaseEndBar; reject wrong perspectives, missing games and stale map/revision stamps; compare entire side responses before/after hidden enemy/revision changes; and exercise actual Double Time/CX and Stun cleanup, historical reconstruction and repeat-read safety.

B5 owns the finite integration work in B5-R1 through B5-R6. Remaining rule-family execution is retained under B5-D1 through B5-D9 for later Rules passes. Clearance-before-status and concealment-before-illumination end-to-end integration remain pending. B4 does not claim the whole rulebook is executable or the UI gate is satisfied.

The earlier proposed AssessClose signature is superseded by TurnPhaseClose.Describe(TurnClosePlanFacts, bool adjudicator, bool historical), composed through the two Play APIs above. Earlier B2/B3 validation paragraphs record their milestone scope.

B4 final verification: 123 ASL.Turns tests and 77 focused Play regression tests pass. Coverage is 100% for ASL.Turns, not a claim of full Play assembly coverage.


## B5 durable facts, replay and profiles, implemented 2026-10-27

B5 adds a bounded Placed DC integration (A23.3-.4; ASOP 3.32A and 5.22A), not blanket support for every remaining rule family.

**Durable facts and compatibility.** New governed DC placements write optional `dcOwnerSide` on the existing `movement-step` event. Units reads/writes it and rejects a recorded owner that differs from the placer at that revision. Origin ID, sequence, phase, target, CX and concealed-target facts already exist in the event log, so no duplicate obligation store or generic resolution event is introduced. The existing schema version remains 1: the new field is additive and omitted for legacy null values. Older events replay exactly as before, but the guide keeps their missing placement ownership incomplete (`NotEvaluated`, command support `Pending`), even if later completion evidence exists. No automatic migration guesses ownership from a later counter or custodian.

**Lifecycle and authority.** The history adapter creates a distinct `placedDc` candidate from each placement event, preserves its original owner and follows projected operability. Rules schedules it before AFPh and marks an operable DC due in AFPh or overdue afterward. The placer's subsequent ordinary firing eligibility does not erase that obligation. A supported placement interrupted before becoming operable is `Interrupted`. Completion requires a recorded Placed DC attack followed by removal of that charge; removal alone is not proof of detonation. The original governed detonation/choice/follow-up path and projector remain the sole mutation owners. The existing shared phase-close planner blocks a due executable DC attack. Reads do not draw dice, complete attacks or bypass confirmation.

**Profiles.** `TurnGuideProfiles` names integration evidence and a version. `existing-engine` admits Stun/CX command integration only when origin facts exist; `existing-engine-dc-v1` adds the reviewed Placed DC path. `ReadTurnGuideAsync` preserves the baseline; `ReadTurnGuideProfileAsync` explicitly selects the expanded profile and returns its identity/evidence. Unknown profiles fail instead of implying full support. Profile version participates in the internal evaluation context. Profiles describe admitted host integration, not scenario applicability or a claim that all night, water, aerial or module rules are supported.

**Disclosure and pending content.** All 538 topics and 38 dependency definitions remain present regardless of profile. Missing origins and unregistered families remain pending. A profile's command support does not manufacture an executable topic route: the host still lacks persisted ASOP substeps and uses phase-level reference guidance. Side projection requires original ownership, visible origin/outcome events and disclosed subject identity. A completed own DC remains visible after counter removal only when its identity was visible at placement. Adjudicators retain terminal history. Side closure remains the fixed `GovernedCheckRequired` result, including when an opposing hidden obligation exists.

**Proof and remaining work.** Tests exercise new/legacy wire round-trips, owner mismatch rejection, disk reload, idempotent append retry, historical reads, interruption, AFPh closure blocking, detonation completion, post-removal disclosure and repeated-read safety. Rules tests cover phase boundaries and continued obligation despite placer ineligibility. ASL.Turns passes 127 tests with 465/465 lines and 264/264 branches covered (100% each, no source exclusions). Coverage is for ASL.Turns, not the entire existing Rules, Units or Play assemblies.

Scope correction, 2026-10-27: completed history/work slices below remain admitted only within their proven boundaries. Remaining rule-family execution is tracked under B5-D1 through B5-D9 in the implementation plan. B5-R1 through B5-R6 close the headless integration and UI readiness gate; full execution of those deferred families is not required to close B5.


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


### B5 ammunition supply transfer and offload completion effects, implemented 2026-10-27

This slice extends the existing permissioned `asl.game.record-turn-work` route. `TransferSupply` and `OffloadSupply` are explicit completion specifications for the existing `AmmoTransfer` four-Game-Turn lifecycle. It supersedes the previous slice's pending transfer/offload relocation and Immobilized-source wreck effects. Automatic E10 eligibility and the broader Ammo Dump rules remain Pending. No catalog topics or dependency links are removed.

**Rule reconciliation.** E10.4 moves the Ammo Supply counter at the end of the completing CCPh and flips the original vehicle to its wreck side if Immobilized. E10.12 restricts removal of an available counter to an Immobile source. This bounded path therefore requires Immobilized or Bogged state for both transfer and offload; it does not infer Immobility from a stopped vehicle or its TI counter. Other forms of Immobility and any broader offload interpretation remain pending. The existing ASOP 8.43B work-completion entry and E10.3 full-Game-Turn boundaries remain the timing contract. Completion occurs in the second Player Turn's CCPh after four full Game Turns, not after four Player Turns. No Ammunition DR is made for relocation, and no Low Ammo or C8.9 depletion is cleared.

**Frozen source and destination.** The declaration's subject is the source vehicle. `SupplyId` and `SupplyVehicle` bind its available supply; transfer also binds `DestinationVehicle` among the participants. Offload instead freezes `DumpLocation`, which must match the source's current Location at both declaration and completion. Offload permits a single source participant, so no fictitious second vehicle or crew must be invented. All other work retains its existing minimum participant requirement. `CreateSupply` optionally attests an initial supply fact under the existing adjudication permission; otherwise that bound supply must already exist. Invalid proposals do not publish new facts.

**Atomic state effects.** Completion relocates the same `TurnAmmoSupply` identity, owner and availability. Its representation is either a vehicle binding or a stationary `DumpLocation`, never both. Transfer clears the source binding and assigns the recipient; offload sets Vehicle to null and retains the exact Location string. Available supply then makes the recipient's usable Passenger capacity zero; the source's ordinary capacity is restored. An Immobilized source is converted through the existing non-burning wreck projection. A merely Bogged source remains active. The supported source has no explicit occupants or held equipment; their disposition requires separate rules and is refused rather than guessed. This does not add a generic counter-placement endpoint.

**Validation and continuity.** The source must be an active own vehicle with the bound available supply and supported Immobile state. Transfer requires an active own vehicle in the same Location, without an available supply or Passengers. E10.11 recipient qualification remains an external adjudication; Accessible-but-different Locations are not yet admitted. Current source, destination and supply facts are rechecked on completion. A stale fact rejects the whole append, preserving the prior supply, TI and lifecycle. Cancellation releases work-owned TI but leaves the supply at its source. Completed work cannot execute twice or accept a replacement specification. The existing `turn-work-effects-recorded` journal protocol persists the specification and derives all effects in replay. Old readers reject the new unsupported effect kinds. The work profile is now `b5-work-4`.

**Headless disclosure.** Authorized guide summaries show TransferSupply/OffloadSupply and AwaitingCompletion/Applied/Cancelled through the existing disclosure model. They do not expose the supply identity, destination or adjudication notes. Applied effects do not promote `RuleAutomation` from Pending. The relocated vehicle supply can feed the existing replenishment path; dump-backed replenishment is still refused by that vehicle-only adapter and remains visible as pending.

**Remaining all-Passes work.** Accessible Locations, automatic E10.11 qualification and caliber compatibility, other forms of Immobility, explicit occupant/towed-equipment disposition, full Ammo Dump benefit/replenishment and SSR/DYO setup, supply capture/destruction/explosion, the E10.4 transition-turn MP restriction and E10.21 firing-benefit movement restriction, automatic unarmored Ammo Vehicle immobilization handling, simultaneous recipients, and D3.71/B# firing behavior remain pending. Animal transport and automatic packing eligibility/duration remain pending from earlier slices. This implementation is not a claim of full E10 integration.

**Tests.** Rules tests distinguish four Game Turns from early completion, reject an Ammunition DR and malformed destinations, and derive relocation without replenishment. Live tests cover transfer/offload from Immobilized and Bogged sources, source wrecking versus survival, supply identity and passenger capacity, historical replay and persisted reload, retargeting refusal, stale-source atomic rollback, cancellation and invalid source/destination facts. Guide tests retain Pending automation and suppress private destination details for both effect kinds.


Supply transfer/offload validation: 150 ASL.Turns tests pass with 666/666 lines and 374/374 branches covered (100%, no exclusions). The focused Rules suite passes 40 tests, the relevant Play/Gun/ordnance/vehicle/guide selection passes 67 tests, and the Units regression suite passes 421 tests.


## Finite B5 architecture and completion boundary

The all-Passes ASL.Rules design remains the target. B5 is the finite integration milestone that makes the present subset safe and usable by a headless consumer. Its completion does not certify the entire rulebook. The implementation plan owns the numbered backlog and deferred register; this section defines the design constraints behind them.

| Required slice | Design responsibility |
|---|---|
| B5-R1 | Exhaustive mapping from the reviewed reference inventory to capabilities, evidence and retained gaps. Keep 538 topics, 38 dependencies and 191 links visible. |
| B5-R2 | Truthful bounds on every shipped profile and adjudicated effect. Unsupported contexts are refused or marked unknown, not treated as automatically legal. |
| B5-R3 | Stable, durable occurrence/window evidence for the selected live route; never infer an ASOP substep solely from a phase name. |
| B5-R4 | One complete AFPh Placed DC topic-to-command route, using existing governed detonation and follow-up ownership. The guide exposes typed intentions without executing or rolling dice on reads. |
| B5-R5 | Cross-profile replay, legacy missing-fact behavior and viewer-safe disclosure proofs for the entire admitted baseline. |
| B5-R6 | Named evidence for the existing UI readiness criteria and a headless handoff example; then B5 completion and U1 eligibility. |

**No new mutation authority.** Rules owns verdicts and timing. Units owns authoritative history and projection. Play owns authenticated fact adapters, proposal, confirmation and commit. ASL.Turns composes guidance. The finite backlog adds integration evidence and contracts only as needed; it does not create a mutable guide ledger, a universal substep engine or a generic override command. Known legacy gaps remain explicit rather than being backfilled with guessed facts.

**Executable, adjudicated and reference-only are distinct.** Current profiles through work version `b5-work-4` form the frozen baseline. Stun/CX and bounded Placed DC support do not promote unrelated topics. Permissioned packing/ammunition effects can be Applied while rule automation is Pending. B5-R4 admits the selected topic route only after B5-R3 proves its window and B5-R2 proves the host capability. Work adjudication retains its separate permission and cannot be offered as ordinary player authority.

**Deferred execution stays represented.** B5-D1 fire/Spotter, D2 atmosphere, D3 packing/animals, D4 supply/dumps, D5 tunnels/airborne, D6 CC/heroes, D7 water/aerial/modules and D8 Trip Flare/Clearance and D9 core-phase rule/guide integration are later Rules-pass packages. Their existing reference content, partial calculators and admitted history remain intact. Unsupported execution is accompanied by source/timing, missing-fact and manual/adjudication explanations where known. Deferred planning is not runtime NotApplicable, hidden content or a completed obligation. No new concrete Rules pass number is invented by this decision.

**Safe closure with incomplete rules.** Keep the shared governed phase-close assessment and viewer-safe output. A reference consideration cannot resolve a mandatory obligation, and an unsupported family cannot be fabricated into an executable blocker. Any claimed live subset must be narrowed or refused when missing facts prevent correctness. New rule-family semantics wait for their B5-D package; safety defects in the admitted subset remain part of the six required integration slices.

**Acceptance.** All six required slices must pass their implementation-plan criteria before B5 is marked complete. Preserve 100% ASL.Turns line and branch coverage without exclusions, with appropriate dependent regression checks. U1 consumes these contracts and keeps all pending rules visible; it does not own timing, eligibility or lifecycle calculations. R1 implements the inventory and R2 implements the audit below. R3 through R6 remain Planned; the UI readiness gate is not yet complete.


## B5-R1 capability and pending-work inventory, implemented 2026-10-27

`TurnCapabilityInventory` is immutable public reference metadata, embedded from [ASL Turn Capability Inventory.json](ASL%20Turn%20Capability%20Inventory.json). Its schema has explicit topic/dependency bindings, source-review state, subset-evidence links and pending-gap links. The 576 bindings cover all 538 topics and 38 dependencies; joining the original catalog retains its references, source pages and 191 links without duplicating the catalog. Construction fails if the authored inventory no longer matches the catalog, so new content cannot silently acquire a guessed owner or vanish from coverage.

Evidence is typed as RulePredicate, GovernedSubset, ObservedHistory or AdjudicatedEffect. Each record names its supported scope, profile IDs and repository evidence. A recorded shot does not authorize the next shot; a timer predicate does not establish a governed command; an adjudicated completion does not prove automatic eligibility. These distinctions are data, not inferred from prose or the presence of an implementation file. All unregistered remainders keep NotEvaluated applicability/support and Pending rule support. The inventory grants no action route and does not populate executable `TurnCapability` registrations.

Consumers use `TurnGuideService.Inventory` or load the inventory directly, then join by kind and stable topic/dependency ID. Source references and timing remain in the catalog and timing schema; game-specific facts and decisions still come from the existing authenticated adapter. The current host guide response is unchanged. No game event, dice, permission or profile version changes in R1. The following R2 audit must confirm the detailed bounds of the existing evidence before any broader admission.

The deferred registry now has nine owners. D9 explicitly retains core Rally/Rout/movement/terrain/status/possession and other core-phase gaps omitted from the first eight packages. Multiple owners can apply to a topic. This extends retained rule coverage without adding any B5 exit gate; five required integration slices remained at R1 completion. Five bindings preserve the Chapter G packing source gap and two preserve the MPh prisoner-drop/source-order interpretation issue. Reconciled source review describes the prior reference review, not complete engine support.

Tests validate the exact inventory and all links, reject incomplete/unknown/duplicate mappings and malformed evidence, and prove unchanged reference visibility and absent topic action routes across all five profiles and eight phases. R1 passes 162 ASL.Turns tests with 738/738 lines and 424/424 branches, both 100%, without exclusions. The original Rules/Units/Play mutation authority and the all-Passes target remain intact.


R1 host regression verification: all four Play TurnGuide tests pass after the service begins loading the inventory. No game commands or event formats changed.


## B5-R2 admitted host capability audit, implemented 2026-10-27

This audit covers the shipped guide profiles, their bounded evidence and the permissioned work action. It does not admit a new rule family or a live topic action route. All 538 topics, 38 dependencies and 191 links remain present; B5-D1 through B5-D9 retain deferred execution. R3/R4 still own exact decision-window evidence and topic routing, and R5 still owns the broader acceptance matrix.

### Audited capability matrix

| Admitted subset | Required facts and authority | Current boundary, refusal or pending behavior | Evidence checked |
|---|---|---|---|
| Stun/CX lifecycle and cleanup | Recorded origin and stable owner, supported Rules verdict, authorized subject/origin/outcome. Existing phase/action authority. | Missing origins or a NotEvaluated verdict report Pending command support. Setup markers do not fabricate origins. Only the existing cleanup subset is admitted; no topic route is created. | TurnObligationRules, TurnHistoryAdapter, GamePlanner.TurnGuide, TurnGuideProfileTests, PlayTests.TurnGuide |
| Placed DC | Recorded MPh placement owner, operability, disclosed attack/removal evidence; normal governed detonation/choice/follow-up. | Legacy missing owner remains unknown. Removal alone does not prove detonation. Phase-level guide reads do not establish an ASOP substep or grant a topic action. | TurnObligationRules, GamePlanner.TurnGuide, BacklogPass15Tests |
| Fire/Spotter history | Disclosed firing events, event-time owner, real phase occurrence, disclosed current holder/Spotter facts. Read-only. | Observed use/ROF is not permission to fire, designate or replace. Original designation, replacement and Opportunity Fire consequences remain pending. Current-phase use is now derived only from disclosed shots, preventing hidden shots from leaking through the Spotter summary. | GamePlanner.FireHistory, TurnFireHistoryTests, BacklogPass9Tests |
| Grenade and Wind/Gust history | Disclosed attempt and owner, immediately causal grenade creation at the recorded Location, recorded cleanup boundary. Read-only. | Unknown origin remains unknown; unmatched visible SMOKE is not reclassified. White SMOKE, drift, cave WP and dust execution remain pending. | GamePlanner.Atmosphere, TurnAtmosphereTests, BacklogPass9Tests |
| Work lifecycle without effects | Separate `asl.game.adjudicate-work` authority, rule basis, first-RPh declaration/resume, active own uncaptured participants, externally adjudicated packing duration or Rules E10 interval. | Lifecycle-only work remains an external adjudication, not certification of vehicle/Gun eligibility or completion effects. No arbitrary counter mutation is introduced. | GamePlanner.TurnWork, GameProjector.TurnWork, TurnWorkLifecycle, PlayTests.TurnWork |
| PackGun / UnpackGun effects | Frozen Gun/crew roles, active own Good Order crew and Gun in one Location, expected current assembly/manning; external G10 eligibility/duration. | Rechecked on resume, every subsequent non-work event and completion. Invalid roles, assembly or crew facts interrupt the current interval. Animal transport, midpoint/reversal and automatic duration remain pending. | GameProjector.WorkEffects, TurnWorkEffects, PlayTests.WorkEffects |
| Replenish effects | Bound available supply, active own co-located vehicles, explicit recipient Low Ammo, no source Passengers, **known CE** posture, system Ammunition DR on completion. | Unknown, withheld, inapplicable or BU posture is refused. Loss of known CE interrupts the current interval; resume rechecks all bound facts. Qualification/compatibility, Accessible Locations, simultaneous recipients and B# combat behavior remain pending. | E10.3-.31; TurnWorkEffects, GameProjector.WorkEffects, BacklogPass7Tests.WorkEffects/WorkAudit |
| TransferSupply / OffloadSupply effects | Available bound supply; supported Immobile source (Immobilized/Bogged); frozen same-Location recipient or dump. Recipient has no supply or Passengers; source has no explicit occupants/held equipment. | Recheck source, recipient and binding at resume and completion and after other events. Preserve identity/availability; Immobilized source wrecks. Broader Immobility, Accessible Locations, dump use, occupant/equipment disposition, movement restrictions and supply destruction remain pending. | E10.12, E10.4-.6; TurnWorkEffects, GameProjector.WorkEffects, BacklogPass7Tests.SupplyEffects |

**Read and mutation authority.** `TurnGuideProfile.Bounds` exposes typed scope, required facts, pending restrictions and authority labels for the actual profile. The labels describe existing authority; they grant none. All profile versions advance (`b5-r2-base`, `b5-r2-dc`, `b5-r2-fire`, `b5-r2-atmosphere`, `b5-work-5`) so evaluation stamps distinguish the audited contract. `AssessedCommandSupport` combines the existing profile/origin boundary with a supported assessed state; NotEvaluated or unknown states cannot advertise integrated command support. Pack/ammo automatic rule support remains Pending even when an adjudicated effect is Applied. No inventory row is converted into an executable topic registration.

### Work safety fixes and compatibility

The audit found that the previous effect path checked CE loss only after an explicit BU change, not known CE at admission, and that resume did not revalidate effect bindings. The new admitted path requires explicit CE for replenishment effects. It rechecks active own uncaptured participants and supplied effect facts at start/resume, after non-work events and at completion. Capture, ownership loss, crew/assembly changes, source mobility or loss of CE can interrupt work immediately. Only previously completed Game Turns survive; the current incomplete interval does not. Work-owned TI is released on interruption, and an invalid resume records no state. Missing eligibility is not inferred from the fact that work was once declared.

New work uses `turn-work-audited` with package `asl.turn-work.v2`. The projector derives `HostAuditVersion = 1` from the event type, not from a caller-supplied assertion, and keeps all later transitions in that protocol. Unknown old readers reject that type. Old lifecycle/effects events keep HostAuditVersion 0 and their original replay semantics, including existing completions; they are not retroactively rejected for newly required CE facts. The live planner offers only cancellation for old open work. Redeclaration uses a fresh work identity and current facts, with no imported progress. There is no automatic upgrade, duration compensation or hidden migration. Cancellation leaves independent supply/Low Ammo facts intact and does not apply completion effects. The host disclosure exposes the audit version so a consumer can distinguish the boundary.

The existing action permission, confirmation, expected revision and attempt-id retry rules remain authoritative. A profile, a supplied `proposedBy`, or an external rule basis cannot grant work-adjudication authority. The projector preserves atomicity: invalid admissions/resumptions/completions publish neither partial supply facts nor partial effects. Profile integration is not a universal permission to act, and no history read mutates state or rolls dice.

### Audit outcome and remaining work

The original profile scopes remain bounded rather than being expanded to full ASL rules. Explicit pending explanations accompany the typed bounds; the nine deferred packages remain owned and visible. Work effect guards and the hidden-Spotter-use fix close demonstrated gaps in already admitted behavior. Exact UI decision windows/routes, full rule semantics, and broader cross-profile acceptance remain R3/R4/R5 or deferred Rules work as recorded in the plan. This audit does not claim to finish those milestones.

ASL.Turns validation: 168 tests pass, with 780/780 lines and 446/446 branches covered (100%, no exclusions). New tests cover every profile boundary, unknown profiles/states, permission labels, legacy/audited disclosure, missing CE states, interruption/resume, captured participants, protocol downgrade/upgrade rejection, old completed replay and withheld current-phase Spotter use. Dependent regression results are recorded after the final run below.


B5-R2 final verification: 168 ASL.Turns tests pass with 780/780 lines and 446/446 branches covered (100%, no exclusions). All 46 focused Play work/guide/fire-history tests, 43 focused Rules tests and 421 Units regression tests pass. Four required B5 slices remain; B5-R3 is next.


### B5-R3: durable Placed DC decision-window evidence

The all-Passes design still represents every reviewed option. This slice admits timing evidence for one route, not automatic execution of opening AFPh rules. The catalog remains 538 topics, 38 dependencies and 191 links. No executable topic registrations are added; B5-R4 owns that connection.

**Authority and persistence.** `TurnPhaseOccurrence` is reconstructed from each actual `game-started` or `phase-changed` event, retaining its event identity, revision, Game Turn, phasing side and phase. A phase name alone is never an occurrence. A Placed DC retains its placement event identity, recorded original owner and Game Turn. Legacy missing owners stay unknown, rather than being supplied from a counter's present owner. This metadata is derived during replay, so no journal migration is needed.

The existing journal does not establish completion of AFPh opening tasks. The narrowly scoped `asl.game.review-placed-dc-window` action therefore records a typed `placed-dc-window-reviewed` event. Its separate `asl.game.adjudicate-window` permission is for a trusted external adjudicator. Ordinary play permission cannot create this evidence. Normal proposal, confirmation, expected revision, attempt identity and journal validation apply. Reusing an attempt with different review inputs is refused. The record binds one charge, its original placement, the exact AFPh occurrence, the owning side, a nonempty rule basis and four explicit prerequisite observations:

| Prerequisite | Source | Meaning of resolved evidence |
|---|---|---|
| Drifting SMOKE | ASOP 5.11B | Required source-linked drift work has been adjudicated and completed. |
| Opening WP | ASOP 5.12A | Opening choices have been considered or waived, and any resulting WP effects have been completed. |
| Gusts and SMOKE | ASOP 5.13A | Required Gusts consequences have been adjudicated after WP. |
| Glider contents | ASOP 5.21A | Required landing-content work has been adjudicated before ordinary AFPh decisions. |

Each observation is `Unknown`, `Resolved` or `NotApplicable`. Unknown observations may be recorded durably but cannot open the window. `NotApplicable` is an explicit external ruling, never a default for an unimplemented rule. These observations are a checkpoint attestation that the listed sequence was handled; they do not execute those tasks or certify each underlying rule. A waiver of an optional choice belongs under resolved consideration, not inferred nonapplicability.

**Conservative lifetime.** A review is valid only at the state immediately after its own event. Every subsequent game event expires it, including a replacement review, a changed condition, an attack, or another phase occurrence. A replacement review can establish fresh evidence. This intentionally requires a new review after intervening play, even if a future, richer dependency model could prove some evidence reusable. It avoids granting an old ruling indefinite authority and requires no guessed invalidation list. Multiple charges are reviewed individually. It is not a phase-wide completed-ASOP flag.

**Rules and headless output.** `TurnPlacedDcWindowRules` owns the bounded timing predicate. It checks AFPh, original owner, same Game Turn, operability, exact occurrence/origin and complete opening evidence. It returns `NotEvaluated`, `OutsideWindow` or `ReadyForCommandAssessment`. The last value means only that the timing evidence permits assessing the existing command; it is not legality, permission, an action route, or phase-close approval. Existing command and closure behavior are unchanged. B5-R4 must re-read the current stamp and evidence, run the existing governed command assessment, and withhold routes for historical or unauthorized views.

`TurnDecisionWindows.Describe` composes sanitized evidence with source timing and explicit pending explanations. Play exposes `DecisionWindows` in the DC and later profiles, after origin, subject, phase-entry and review disclosure checks. Enemy evidence is omitted; withheld entry/review evidence cannot authorize a window. Raw event identities, revisions and free-text adjudicator rule bases do not leave this guide response. The authenticated administrative host uses authoritative state to prepare a review; the player guide cannot grant adjudication authority. Historical reads reconstruct the evidence at that revision and are explicitly read-only. Global guide context remains `<phase>:phase`; one charge's review does not advance every topic into ASOP 5.22A.

Profile versions are `b5-r3-dc`, `b5-r3-fire`, `b5-r3-atmosphere` and `b5-work-6`; the base profile is unchanged. The nine deferred packages remain visible. Drift, WP consequences, Gusts and glider automation retain their pending status even when their external adjudications are complete. The frozen UI preview remains design evidence, not a renderer for this backend contract.


B5-R3 final verification: all 170 ASL.Turns tests pass, covering 789/789 lines and 450/450 branches (100%, no exclusions). All 22 focused Play tests, 21 focused Rules tests and 421 Units regression tests pass. Three required B5 slices remain; B5-R4 is next.


### B5-R4: one complete headless Placed DC route

`afph:placed` now has one bounded evaluator/command registration in each DC-capable profile. `TurnPlacedDcGuidance` composes typed timing and existing-command readiness into Required, Unavailable or NotEvaluated. Its registrations do not apply to thrown DC, Breach, other AFPh weapons, or the rest of A23. All 538 topics, 38 dependencies and 191 links remain present, and the nine deferred rule-family packages remain pending.

**Read path.** The Play adapter first applies R3's origin, occurrence, ownership and disclosure filters. For a live owning-side read with complete timing evidence, it invokes the same full `PlanCoreAsync(DetonateDc)` assessment used by the existing command, without building its roll or writing events. No raw plan or refusal text enters the guide. A refused assessment gives a generic unavailable explanation. Missing timing or unassessed command facts remain NotEvaluated. Opposing-side, adjudicator, historical, base-profile and other-phase views receive no executable route. Adjudicator evidence is still available for review; this initial route is explicitly an owning-player route.

Only the Placed DC topic is composed using the subject's ASOP 5.22A window. The snapshot's overall context stays `afph:phase`. Other topic decisions and pending explanations do not inherit a charge's opening-task review. `TurnHostGuide.Actions` provides typed `TurnPlacedDcAction` values containing an opaque token, subject, profile and the existing `asl.game.detonate-dc` action identity. The matching topic decision references that token.

**State and authority.** A route token is an HMAC over scope/state stamp, owner, subject and profile/version, using a process-local key. Repeated reads in the same state are stable, but these tokens are not durable rule evidence or authorization. Restarting the host requires refreshing an uncommitted route. R3's journal evidence remains durable and can produce a fresh route after restart. Tokens reveal no raw revision, placement event ID, hidden target, or command plan. The existing play permission and confirmation gate still decides execution authority.

**Consumer flow.** A trusted server supplies the authenticated perspective to `PreparePlacedDcDecisionAsync`, selecting an offered typed route and a fresh attempt ID. It may pass an optional named vehicle target, which the existing command validates. The method re-reads the current guide, verifies that the exact route remains offered, and returns command arguments, or null if the route is stale/unoffered. These arguments are for the trusted host, not the player-facing guide response. Keep them unchanged through proposal and confirmation. Submit them using `GamePlay.ProposeAsync` and then `ConfirmAsync` with `GameActions.DetonateDc` and the authenticated principal. Do not bind a client-selected side or adjudicator perspective.

The existing detonation command accepts optional guide-route/profile arguments. When present, it rechecks the exact token, owner and current R3 evidence at planning, gate and commit time. A later event therefore blocks a prepared request through expected revision and route validation. An ordinary command without guide arguments retains its existing behavior; R4 does not impose external adjudication on every old engine client.

**Execution and retries.** Dice, target checks, player choices, Sniper processing, fire follow-ups, removal, victory and phase closure remain in the existing command pipeline. If an attack pauses for a defender's choice, the normal Choose command owns that response; the guide does not detonate again or take over the defender's authority. The review expires on intervening events and the route disappears. The existing follow-up eventually removes the charge and resolves its obligation.

For guided commits only, the first event carries optional `GuidedDecisionHash` envelope metadata. This canonical digest binds action, game, attempt, expected revision, charge, vehicle selection, owner, profile and opaque route. It is request identity, not an ASL fact. Existing journals omit it and round-trip unchanged. Identical committed retries return Replay without dice; changed inputs or another action cannot reuse that committed attempt. A retry can use the retained original arguments after process restart because it compares the persisted digest, rather than reopening a timing window. A fresh attempt must obtain a current offered route.

**Versions and remaining work.** The DC-capable profiles are `b5-r4-dc`, `b5-r4-fire`, `b5-r4-atmosphere` and `b5-work-7`; base remains unchanged. R5 owns the broader cross-profile replay/disclosure acceptance matrix, and R6 owns final B5 acceptance and the UI handoff. Full opening-rule automation, unsupported A23 variants and all other deferred rules remain visible and pending.


B5-R4 final verification: all 178 ASL.Turns tests pass, covering 819/819 lines and 464/464 branches (100%, no exclusions). All 25 focused Play tests and 421 Units regression tests pass. The Rules predicates were unchanged; R3 Rules results remain historical evidence, not new R4 runs. Two required B5 slices remain; B5-R5 is next.


### B5-R5: cross-profile replay and disclosure acceptance

R5 exercises the five shipped profiles without adding rule families. Exact topic identities and order are checked against the catalog, alongside profile-specific payloads, pending explanations, owning/opposing/adjudicator views, repeated reads, file reloads, historical-prefix replay, legacy gaps, retry identity and completed effects. All 538 topics, 38 dependencies and 191 links remain represented. The detailed profile/evidence matrix is in implementation plan section 11.7.

**Disclosure finding and correction.** The new counterfactual test created two replay-valid journals with the same disclosed DC origin, occurrence, opening review, own-counter facts and state stamp. Only an opposing private choice differed. R4's passive command assessment changed the owning-side topic from Required to Unavailable and removed its action. All four DC-capable profiles failed the complete-response comparison. Generic refusal text alone did not protect privacy because applicability and action count still disclosed the private blocker.

R5 corrects that boundary. A passive owning-side route means **request the governed command check**, not that the command has passed its private eligibility checks. `TurnCommandReadiness.GovernedCheckRequired` preserves the due obligation as Required and explicitly says private decisions may remain. The Play adapter no longer runs a private command-readiness assessment to decide whether to expose a route. This supersedes R4's read-path requirement for a Ready result; it does not change execution authority or the attack rules. The existing Ready/Refused composition values remain available for appropriately authorized assessments, but passive side reads do not receive those private results.

The route still requires the admitted profile, correct live phase, owning perspective and fully disclosed R3 timing evidence. Missing, legacy, withheld, interrupted and historical evidence never becomes an executable action. Preparation binds the offered route to its current state as before. Explicit proposal, confirmation and commit continue through the full existing `PlanCoreAsync` path, including permission, private pending-choice guards, expected revision, timing/token validation, attack legality, dice and follow-ups. The counterfactual test proves identical passive responses while the actual governed proposal is denied in the blocked world without a roll or journal write. A Required checklist item is an obligation to consider, not permission to execute.

**Evidence and freshness boundaries.** Replay comparisons use the same game scope, board data and profile version. A historical read against a full journal must equal the read against a journal ending at that revision, so future effects cannot contaminate past disclosure. Hidden-fact comparisons hold disclosed phase, own facts and decision-evidence currency constant. R3's explicit any-event review expiry is retained: this slice does not conceal an authorized window's invalidation or reuse a stale route. Opaque route handles remain process-local; persisted request digests still support identical committed retries after reload. These are acceptance tests for the admitted host contracts, not a formal proof for every future rule or arbitrary corrupt journal.

Fire/Spotter tests now run in all three profiles that expose them. Grenade origin, cleanup and legacy-link tests run in both atmosphere profiles. The five-profile work matrix checks current versus legacy audit versions, applied packing effects versus pending rule automation, historical pre-completion disclosure and withheld work evidence. Existing supply, replenishment, work-interruption and fire/atmosphere regressions are retained. The base and earlier profiles explicitly omit unsupported optional history/work payloads while preserving the reference topics.

DC-capable profile versions are `b5-r5-dc`, `b5-r5-fire`, `b5-r5-atmosphere` and `b5-work-8`. Base remains `b5-r2-base`. The nine B5-D packages remain pending. R6 alone remains for final B5 acceptance and the UI handoff; no renderer is introduced by R5.


B5-R5 final verification: 179 ASL.Turns tests pass, covering 821/821 lines and 466/466 branches (100%, no exclusions). The focused Play matrix/regression run passes 94 tests, and the additional Wind/Gust theory passes two cases. R6 alone remains. Rules and Units source are unchanged; earlier direct suite results remain historical evidence.


### B5-R6: final acceptance and U1 handoff

The [B5 acceptance and U1 handoff](ASL%20Turn%20Guide%20B5%20Acceptance%20and%20U1%20Handoff.md) records the six pre-Blazor criteria, final capability/evidence matrix, deferred-rule boundary and executable headless consumer example. B5 is accepted for its admitted baseline; U1 is eligible, not implemented. The all-Passes design goal and all 538 topics, 38 dependencies, 191 links and nine deferred packages remain intact. No additional rule execution, profile version or journal schema is introduced by R6.

The tested consumer browses every phase while keeping actual and browsed phase separate, retains missing facts and pending explanations, observes Due and Resolved obligations, and requests an offered Placed DC action through the existing trusted-server preparation and Play proposal/confirmation pipeline. Its history is read-only and its committed retry cannot execute twice. Renderer preferences cannot become domain facts. Phase close remains a shared governed action, not the result of completing a checklist.

**R6 verification:** 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). All 97 focused Play acceptance/regression cases, 49 focused Rules cases and 421 Units cases pass. The consumer example also passes a final targeted rerun after strengthening its pre-proposal revision assertion. Coverage applies to the entire ASL.Turns assembly, not the Play, Rules or Units assemblies.


### U1: thin Blazor Turn Phase Guide

The Reference navigation now opens `/turn-guide`. `TurnGuidePanel` consumes `ASL.Turns` snapshots: reference mode calls `TurnGuideService` with no executable registrations; live mode receives an authorized `TurnHostGuide`. Both keep all 538 topics across eight phases, pending explanations, source gaps, references, glossary terms and related dependency definitions. Search has a visible Show all options escape. Source review remains distinct from applicability and command support.

The saved preview supplies the phase buttons, grouped checklist and detail-pane structure. The implementation uses Studio's shared `site.css` theme, responsive stacking below 700px, semantic buttons with selected states, labelled search and a private Considered checkbox. These marks live only in the component instance, survive phase browsing, reset when game/viewer/profile changes, and are lost when the guide is closed or the page reloads. They do not write game facts, change obligation state or authorize actions. Acronym links use the existing search page.

In Play, the Turn Phase Guide button opens `LiveTurnGuide` in the Actions pane. It calls the existing planner with the same server-owned perspective as the workspace and the accepted work profile. The adapter refreshes on revision or scope change, hides old content while reading, suppresses outdated asynchronous results and clears content on failed reads. Error text never includes raw exception details. Hand-over hides the guide. The Studio remains a local shared-table application with its existing viewer selection, not a new authentication system.

Disclosed obligations, decision windows, phase-close assessment, fire/Spotter and atmospheric history, work progress/effects and their pending gaps render without interpreting rule prose. Only an offered token matching a displayed decision can raise a command request. Historical snapshots suppress both topic actions and phase close. `ProposeTurnDecision` prepares the selected token using the current trusted view and sends the exact prepared arguments into the existing Play proposal path. That path preserves the owner, permission, revision, target and confirmation checks. Phase close uses the existing `ProposeAdvance` action and review panel, not checklist state. No automatic missing-fact adjudication is added; if opening evidence is absent, the existing pending state stays visible.

The public panel and loader also accept historical backend responses as read-only. U1 does not add a replay-page guide attachment, persistent personal bookmarks, or new rule execution. The frozen HTML preview remains preserved. No ASL.Turns, Rules, Units or Play library contract, profile version or event schema changes in U1.

Tests: `TurnGuideComponentTests` covers all eight phases/538 topics, pending/source gaps, search reset, private marks, exact offered-action intents, viewer resets, historical action suppression and late-read/error handling. `TurnGuideReadsWithoutWritingAndUsesExistingPhaseConfirmation` opens the real Play page, browses another phase without changing the game, and verifies phase close reaches the existing confirmation panel before committing. Existing workspace/navigation regressions remain in the focused run.


U1 final verification: 22 focused MapStudio component, page, workspace and navigation tests pass. The new tests retain all 538 topics and verify offered actions, historical read-only behavior, private marks, scope changes, stale read suppression, safe errors and normal phase-close confirmation. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). Browser inspection verified the shared-theme checklist/detail layout, unclipped rows, selected-state styling and Rally-to-Advancing-Fire navigation. Backend libraries were unchanged.


### U1 audit follow-up

The [U1 audit](ASL%20Turn%20Guide%20U1%20Audit.md) reconciles the initial component against the frozen preview and accepted backend. The global dependency browser now retains all 38 chains and 191 topic links; topic selection is remembered by phase, reminder counts are phase-local, and the complete phase glossary remains available. Checklist attention labels come directly from backend decisions, alongside pending-rule explanations. Disclosed work lifecycle, opening-review prerequisites, spent attempts and unknown firing/Spotter observations now render explicitly. A support section preserves profile scope and pending remainders.

Refresh is available during normal use and suppresses outdated asynchronous reads. Topic request buttons match complete route identity and live scope; execution still uses the existing governed path. No consideration flag or source reference grants permission. The audit records deliberate preview adaptations and remaining later-pass rule work.

Verification: 30 focused MapStudio component, page, workspace and navigation tests pass. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). Browser inspection confirms readable attention rows and the expanded cross-phase audit with its source evidence and related topic controls. No backend library source changed.


### U1 live scenario acceptance

The [live scenario acceptance](ASL%20Turn%20Guide%20Live%20Scenario%20Acceptance.md) continues The Tractor Works on the real board through UI placement, authorized opening review, guided DC proposal/confirmation, resolved obligations and governed phase close. A separate interruption branch rejects stale confirmation and retains Unknown evidence and the close block. All pending rules remain visible.

The Play confirmation revision is the expected revision actually submitted with the command, including routes prepared from newer host evidence. Confirm still re-reads the journal and rejects an intervening change. The live guide follows phase changes while browsing the current phase and preserves deliberate browsing elsewhere. Opening-review authorship remains outside Studio and requires its separate existing permission. No capability, rules contract or ASL.Turns source changes.

Verification: 33 focused MapStudio tests pass, including both real-board scenario cases. The isolated browser rehearsal confirms refusal before detonation, governed detonation, resolved obligation and confirmed advance to Rout. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions).


### Authorized opening-review workflow

The [opening-review workflow](ASL%20Turn%20Guide%20Opening%20Review%20Workflow.md) completes the live UI path for the admitted Placed DC route. GamePlanner supplies current-state review offers and prepares server-owned timing identities. The form defaults all four opening tasks to Unknown and requires a reason; the existing confirmation panel displays the complete evidence before a governed write. The owner separately confirms detonation and phase close. Pending rules remain visible.

Authorization is supplied by trusted server configuration, disabled by default. Adjudicator view selection alone grants no permission. The reviewer is restricted to the review action; ordinary play permissions remain unchanged. The previous external-only UI limitation is superseded when this host authorization is enabled. Tests use an explicitly authorized test reviewer.

Verification: 43 focused MapStudio tests pass, including all three real-board scenario cases (not skipped). All 45 focused Play tests pass, including review preparation, timing, replay and disclosure acceptance. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). No ASL.Turns or Rules source changed.


### Full opening-review browser acceptance

The [opening-review browser acceptance](ASL%20Turn%20Guide%20Opening%20Review%20Workflow.md#full-live-browser-acceptance) completes the user-authorized local path: cancel an incomplete proposal, commit a reasoned four-task review, hand over to the owner, refuse premature phase close, confirm guided DC detonation, then confirm Rout. The journal records review at 143, detonation at 144-146 and phase change at 147. BB3 is empty, so this is lifecycle acceptance rather than occupied-target consequence coverage.

The confirmation summary uses full opening-task names, ASOP references and readable evidence labels; the same evidence-only explanation remains accurate after commit. It does not derive legality. Regressions verify mixed task values, a still-blocked close after complete review, and resolved obligations plus pending rules after reopening the game. The eventual all-Passes target and deferred B5-D inventory remain unchanged. Local reviewer authorization remains a server setting with disabled checked-in defaults.

Verification for this follow-up: 44 focused MapStudio tests pass, with no skips; all three real-board scenarios also pass after strengthening the no-write-on-reopen assertion. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). Restarting the local server and reloading the browser preserves German Rout, the resolved DC obligation and pending disclosures, with no obsolete review, detonation or confirmation controls. No ASL.Turns, Rules, permission defaults or journal contracts changed.


### Occupied-target continuation acceptance

[Occupied-target acceptance](ASL%20Turn%20Guide%20Opening%20Review%20Workflow.md#occupied-target-consequences-and-phase-close) covers a K/4 casualty reduction with a failed MC and a separate pending Battle Hardening decision. The existing engine owns casualty lineage, conditions, pending decisions, continuation and DC removal. Phase close remains a separately governed command and cannot bypass the due charge or required answer.

Required live decisions and handover controls precede the guide so reference content cannot bury the next required action. A committed DC receipt must distinguish an unfinished consequence from completed removal, using existing host state. Neither rendering rule adds a capability or changes an obligation. Pending integrations and the eventual all-Passes target remain unchanged.

Occupied-target verification: 65 focused MapStudio tests pass with no skips, including all five real-board guide scenarios and existing pending-choice/handover regressions. All 179 ASL.Turns tests pass at 821/821 lines and 466/466 branches (100%, no exclusions). The browser confirms casualty reduction, failed morale and DM, separately confirmed phase close, and durable replay after restart. The interrupted browser branch declines Battle Hardening after restart; a fresh verification branch accepts it, records replacement by the elite squad, and also closes into Rout. Both continuations record one completed fire resolution and one DC removal. The corrected receipt and priority of required decisions were inspected in the browser. Normal random dice have been restored; the explicitly approved local reviewer remains enabled, with checked-in defaults unchanged.


### Reinstated later-pass implementation

B5-D1 through B5-D9 are retained requirements for the eventual all-Passes implementation. The remaining nine packages are explicitly deferred to later ASL.Rules passes and are now tracked in the [36-task later-pass implementation plan](ASL%20Turn%20Guide%20Later%20Rules%20Implementation%20Plan.md). Rules owns semantics; Units persists authoritative facts; Play executes and discloses; ASL.Turns coordinates; Blazor renders. Starting or completing a planning task never promotes a pending capability. Every catalog option and package remains visible during implementation.


### D1.1: rule-owned Spotter timing

The later-pass implementation now has a typed `TurnSpotterTimingRules` contract in ASL.Rules. Hosts supply the authoritative owner, turn position and opening checkpoint, designation history, actual loss reason/position, and evidence for a Good Order reduction survivor. The verdict distinguishes retained assignment, original designation window, replacement window, outside window and not evaluated. It grants no candidate eligibility or firing permission. Missing history remains unknown; current unit condition cannot substitute for loss history.

C9.3 keeps the original assignment until elimination, break or capture. A surviving Good Order half-squad retains spotting ability. A replacement requires an owner MPh opening following the loss; loss during an MPh cannot reuse that opening. The full rule history, candidate checks and governed commands remain D1.2 work, so the guide continues to display Spotter integration as pending. Sources and acceptance results are recorded in the later-pass plan. D1.1 passes 18 focused Rules tests; ASL.Turns passes 180 tests with 100% line and branch coverage (821/821 and 466/466).


### D1.2: observed Spotter loss survives rally

Units now derives durable `MortarSpotterLoss` evidence from the authoritative journal, using ASL.Rules to distinguish elimination, capture and break from mere lineage consumption. The evidence retains the event, revision and phase occurrence. The fire-history adapter discloses it only when the relevant event is visible; withheld loss makes the assignment NotEvaluated. ASL.Turns keeps a recorded loss Lost after rally, and a historical read before the loss remains Retained. Neither the observed shot nor its loss proves an original designation or grants replacement permission.

This is an implementation increment within D1.2. Explicit designation/replacement, reduced-squad inheritance and multi-mortar restrictions remain pending and visible. Validation: 20 Play and 23 Rules tests pass; all 180 ASL.Turns tests pass with 821/821 lines and 468/468 branches covered (100%, no exclusions).


### D1.2 progress: reduced Spotter successor

A recorded squad reduction now transfers a retained observed spotting assignment to its surviving Good Order half-squad. The journal-derived succession carries event provenance, preserves phase SW use, and permits the successor to continue an otherwise legal ROF shot. Earlier loss prevents inheritance. A broken survivor records the actual break; an unknown survivor remains NotEvaluated. Mere lineage consumption never invents elimination or a replacement window.

The guide preserves the first observed assignment and names its disclosed half-squad successor. Hidden lineage yields NotEvaluated and omits the successor identity. Original designation and replacement commands remain pending; this exception grants no new firing permission and does not complete D1.2.

Validation: 24 focused Play tests, 29 focused Rules tests and 24 Turn Guide component tests pass. All 181 ASL.Turns tests pass at 823/823 lines and 470/470 branches (100%, no exclusions). Coverage includes prior loss, broken/unknown survivors, hidden lineage, serialized replay and continued firing.


### D1.2 progress: shared mortar use

C9.31 now permits one Spotter to serve multiple mortars only when their recorded firing Location and target agree for the entire Player Turn. The state preserves each use across phase changes, clears it at the next Player Turn, and carries it to a retained reduction successor. Different levels are different Locations. Once mortars share the Spotter, changing the target is refused; earlier shots cannot be forgotten to permit another mortar. Phase SW-use records retain every mortar served.

New spotted-shot facts carry `SpotterUseVersion = 1`; legacy records with no version retain their earlier replay validation, and unsupported versions are rejected. This preserves committed history while applying the new contract to new commands. Missing historical Locations cannot justify multi-mortar sharing. Original designation, replacement and the additional source-clarification cases remain pending, so D1.2 is still In progress and B5-D1 remains pending.

Validation: all 1,128 Play tests pass with no skips, including 25 focused pass-9 cases. All 30 focused Spotter Rules tests pass. All 181 ASL.Turns tests pass with 823/823 lines and 470/470 branches covered (100%, no exclusions).


### D1.2 progress: governed original designation

`asl.game.designate-spotter` now records a bounded daytime original designation during the owner's PFPh/DFPh. ASL.Rules checks timing, known Good Order Personnel, ownership, range, one-hex mortar compatibility, TI, phase SW use and outstanding decisions. Unknown Good Order or legacy observed-only origin is not upgraded to proof. Setup/DFF activation, replacement, night, vehicle/PRC and wider profile cases remain pending. The ordinary play permission and confirmation gate apply; this is not an adjudication override or a new guided topic route.

Units records a typed `mortar-spotter-designated` event and explicit provenance separately from shot observations, applies the Rules fire marker and SW-use effects, and tracks the actual journal-relative Player Turn ordinal. Packed/aboard cases remain refused. A squad's shared spotting activity consumes one spotting use, preserving its permitted inherent fire. Recorded loss now bars new spotted fire until a governed replacement exists; rally or naming a different Spotter cannot erase that loss. Legacy unversioned firing records retain their replay behavior.

The read model and thin guide show explicit declarations before any shot without manufacturing a firing history. Declaration and phase-origin visibility are both required. Concealed mortar/holder or Spotter identities are recorded privately. Proposal reads do not write; stale confirmations, changed attempt inputs and duplicate original declarations are rejected. All 538 topics, 38 dependencies and nine pending packages remain represented. The Movement checklist, cross-phase label, walkthrough and saved preview now clarify that C9.3 requires elimination, break or capture; generic non-Good-Order status is insufficient.

Validation: the full 1,131-test Play suite and all 421 Units tests pass. The final disclosure adjustment also passes all 29 focused pass-9/registered-action cases. All 33 focused Spotter Rules tests and 24 Turn Guide component tests pass. All 182 ASL.Turns tests pass with 825/825 lines and 470/470 branches covered, without exclusions. D1.2 remains In progress, with replacement, setup/DFF activation, deployment selection and wider profile acceptance still required.


### D1.2 progress: chosen Deployment successor

The governed Deployment action now requires `spotterHalf` (first or second) for a squad with a retained assignment. The choice is bound before its NTC, including failed attempts, and successful lineage must name the matching produced HS. A Guard can make the same choice during its at-will Deployment. Missing/invalid choices fail before dice or writes; changed retries and altered successor evidence are refused. New choice-bearing records are owner/adjudicator-only so they cannot disclose a concealed or HIP Spotter assignment. Legacy lineage without a chosen successor stays unknown.

Units transfers each retained mortar assignment, phase spotting/SW-use evidence and Player Turn mortar-use history to the chosen Good Order HS. Existing movement restrictions carry through the split; deployment does not create fresh movement or firing permission. Loss history is not erased. The ordinary Deployment form and Guard buttons expose the backend choice. The guide's existing successor evidence remains an owner/adjudicator read, with withheld succession producing unknown evidence.

Validation: the full Play suite passed 1,134 tests before the final evidence/disclosure tightening. The final change passes 74 focused Play tests, including both HS choices, failed NTC, Guard succession, changed retries, altered lineage, legacy records, private history, continued fire and movement restrictions. All 421 Units tests, 45 focused Spotter Rules tests and 37 affected component/page tests pass. ASL.Turns remains 182 passing tests, 825/825 lines and 470/470 branches (100%, no exclusions). All 538 topics, 38 dependencies and nine pending packages remain represented. D1.2 is not complete.

Replacement source dependency: C9.3 and ASOP 3.11A establish replacement at the following owner's MPh opening. The reviewed local text and attributed Q&A do not settle the exact marker/movement effects to apply to that replacement designation. That execution path remains blocked on source clarification; no inferred Prep Fire marker or movement permission is admitted. Setup/DFF activation and wider-profile acceptance are also outstanding. Independent source-backed tasks may proceed under the execution-order policy while this point remains visible.
