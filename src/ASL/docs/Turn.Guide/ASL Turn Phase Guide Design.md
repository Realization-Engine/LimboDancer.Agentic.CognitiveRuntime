# ASL Turn Phase Guide Design

Date: 2026-10-27

Status: Backend-first design, reopened at the user's direction. `ASL.Turns` and `ASL.Turns.Tests` now provide the tested headless reference catalog, typed capability contracts, validated stable timing schema, pure B2 guidance composition and a bounded B3 obligation assessment slice. B4 adds Play host adapters and shared phase-close assessment. Additional families and broader integration remain proposed. UI components are deferred until the back-end contracts and behavior are proven.

References: [Analysis](ASL%20Turn%20Phase%20Guide%20Analysis.md), [Implementation Plan](ASL%20Turn%20Phase%20Guide%20Implementation%20Plan.md), [cross-phase audit](ASL%20Cross-Phase%20Dependencies%20and%20Cleanup%20Audit.md), [folder index](README.md).

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

B5 owns further durable facts, finer disclosure and remaining families. Clearance-before-status and concealment-before-illumination end-to-end integration remain pending. B4 does not claim the whole rulebook is executable or the UI gate is satisfied.

The earlier proposed AssessClose signature is superseded by TurnPhaseClose.Describe(TurnClosePlanFacts, bool adjudicator, bool historical), composed through the two Play APIs above. Earlier B2/B3 validation paragraphs record their milestone scope.

B4 final verification: 123 ASL.Turns tests and 77 focused Play regression tests pass. Coverage is 100% for ASL.Turns, not a claim of full Play assembly coverage.
