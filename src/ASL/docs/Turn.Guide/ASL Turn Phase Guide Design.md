# ASL Turn Phase Guide Design

Date: 2026-10-27

Status: Backend-first design, reopened at the user's direction. `ASL.Turns` and `ASL.Turns.Tests` now provide the tested headless reference catalog, typed capability contracts and validated stable timing schema. Live evaluation, obligation scheduling and command integration below remain proposed. UI components are deferred until the back-end contracts and behavior are proven.

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

Intended compile-time direction: `MapStudio -> Play -> ASL.Turns -> Rules`, with existing `Play -> Units -> Rules`. `ASL.Turns` must not reference Play, Units or MapStudio. When the first executable evaluator is implemented, add its Rules reference; the initial catalog-only library currently needs no project references. Never make Rules call ASL.Turns to resolve a cycle.

Play translates existing state types into narrow facts. Rule fact/verdict records belong to Rules when their meaning is rule-specific. ASL.Turns owns guide request/result and obligation coordination records. This allows reuse of the same Rules predicates by projector validation and guide evaluation without duplicating them.

## 3. Current foundation versus planned behavior

Implemented in this revision:

- `src/ASL/ASL.Turns/ASL.Turns.csproj`, namespace `ASL.Turns`.
- `TurnGuideCatalog.Load()`, immutable phases/topics/glossaries/ASOP ledgers/dependency definitions, exact qualified lookup and related-dependency lookup.
- The nine reviewed JSON source files embedded from `docs/Turn.Guide`. There is no runtime filesystem/PDF path requirement or MapStudio resource dependency.
- Qualified topic identity, for example `ccph:hiddenHero`, and ordinal case handling. Source hash/edition/scope and pending integration prose remain available.
- Tests in `src/ASL/tests/ASL.Turns.Tests`, namespace `ASL.Turns.Tests`; both projects are in the ASL solution with normal lock files and build conventions.

The current loader recognizes the existing Rally schema 2 and other schema 1 documents. It validates group/term references, page bounds, dependency targets and available ASOP ledger links. B1 imports all ten Rally steps from the reviewed walkthrough into its JSON ledger. Legacy inputs without a ledger remain readable by the catalog loader, but do not satisfy the current timing manifest. Numeric group positions and free-text status notes remain authored input, not final stable timing IDs or capability verdicts.

Not implemented: a live `TurnGuideService`, verified production capability registrations, rule-family evaluators, instantiated obligations, command adapters, all-phase closure validation, historical migrations or any UI component. The current tests prove the reference foundation, not live rules correctness.

## 4. Backend API to establish before UI work

Proposed API, not compiled signatures:

```csharp
TurnGuideSnapshot Describe(TurnGuideRequest request, TurnFacts facts);
PhaseCloseAssessment AssessClose(TurnCloseRequest request, TurnFacts facts);
```

Use a concrete `TurnGuideService`, initially with explicit rule-family collaborators rather than a general-purpose plugin engine. A request identifies game scope, internal evaluation stamp, actual and browsed phase/window, viewer/acting side, scenario profile and optional disclosed subject. `TurnFacts` is an immutable, typed adapter result, not a `GameState`, arbitrary dictionary, serialized UI state or callback into a page.

The internal stamp includes event revision, map/catalog identity, rule/capability version, scenario profile, viewpoint and window identity. It is used for cache correctness and stale rejection. A renderer receives a viewer-safe opaque handle, not a full-state revision stream that exposes hidden activity.

`TurnGuideSnapshot` contains:

- Actual phase and browsed phase, the current decision owner and semantic window.
- Complete authored phase options, with separate applicable instances and capability results. Unbound options are retained with Not evaluated.
- Structured attention groups: verified required decisions, verified optional choices, evaluation gaps, due effects, retained effects and future commitments.
- Viewer-safe subjects/reasons/source references, links to dependency definitions and opaque action-route handles when an executable path exists.
- An explicit coverage summary. Absence of verified blockers never means all ASL rules were checked.

No result contains CSS classes, button captions as command identifiers, component names, URL routes or `EventCallback`. Human-readable explanation content is allowed; renderer-specific presentation is not. Output ordering follows semantic windows and rule constraints, not alphabetical labels or numeric ASOP sorting alone.

The service is deterministic for fixed inputs. A read does not change a game, roll dice, allocate a governed attempt or increment progress. Browsing a future phase never changes the actual window.

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

## 7. Replay, nested effects and persistence

Use existing events and projected facts as the source of truth. Derived obligation caches are disposable. Record new declaration/activation/progress facts only where a rule cannot be reconstructed from existing evidence. Prefer rule-specific typed events over a generic client-writable `ObligationResolved` event.

Repeated reads, retries and application restart must not reapply cleanup or advance duration counters. Stable event/attempt IDs and existing expected-revision/replay behavior protect commits. Keep a resolution evidence reference for each settled instance. A child consequence must finish before a parent action proceeds when the governing rule requires it.

Existing `PhaseChanged`-derived cleanup remains projector-owned until an explicit family-specific migration says otherwise. Never execute the same effect both as a new explicit event and existing projector cleanup. Evaluate later effects against the resulting facts from earlier effects, including Clearance before status removal and concealment before illumination removal.

Old histories without required origin/progress facts return incomplete evaluation and retain their documented behavior. A rule upgrade that changes replay needs a reviewed compatibility/version decision and fixtures; silently guessing creation turns is prohibited. Existing session-only simultaneous CC declarations remain uncommitted planning unless a separate durable protocol is implemented with proper disclosure.

## 8. Connection to current Rules, Units and Play

Existing seams inspected in the prior design remain the integration anchors:

- `ScenarioA1Definitions` phase identities; `ScenarioA1SequenceCalculator` and `ScenarioA1SequenceProjection` permissions and transition verdicts.
- `GameState`, `GameEvent`, `GameProjector.ChangePhase` and `GameView` for history, projection and disclosure.
- `GamePlanner.PlanAdvance`, `GamePlanner.PhaseEndBar`, `PhaseEndedBy` and `GameActions.AdvancePhase` for governed progression.
- `LivePlay`, existing proposal/confirmation and action panels as eventual consumers, not back-end dependencies.

`PhaseEndBar` is not a full closure assessment: it reports proposer-related checks, while `PlanAdvance` has further checks and consequences. Extract narrow reusable Rules facts/verdicts and shared Play adapters incrementally. ASL.Turns composes the resulting `PhaseCloseAssessment`; both the read display and authoritative command preparation use the same semantics. The command and projector still independently validate at commit/replay.

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
