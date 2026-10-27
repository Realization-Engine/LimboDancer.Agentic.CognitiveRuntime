# ASL Turn Phase Guide Implementation Plan

Date: 2026-10-27

Status: Backend-first plan. B0, B1, B2 and the bounded B3 obligation slice are implemented and tested. B4 host/command integration and further families remain planned. UI work is intentionally last.

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
| B4 | Play/Units adapters and shared phase-close assessment | B2-B3 | Existing command validation and guidance agree without duplicate cleanup; disclosure and stale checks pass. |
| B5 | Durable missing facts, replay and profile expansion | B4, per rule pass | New families have source, Rules, persistence, replay and visibility evidence. |
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

## 7. B4: Adapters, disclosure and closure parity

Add Play-side adapters translating GameState/history and authenticated perspective into the narrow facts/results ASL.Turns needs. Keep unrestricted rule evaluation internal and apply GameView-equivalent disclosure before returning guidance to any consumer. Test hidden information in counts, identifiers, negative reasons, routes and ordering, not just visible prose.

Inventory every existing `PlanAdvance` refusal/consequence and `GameProjector.ChangePhase` effect before extracting shared assessment. `PhaseEndBar` alone is not comprehensive. Share read-only semantic assessment and use the existing governed proposal/confirmation path for writes. Keep projector validation and event replay independent of whether a guide is present.

For every transition, name one mutation owner: current projector consequence or reviewed explicit event. Never both. Apply ordered effects to resulting facts before evaluating downstream eligibility. Explain blockers, automatic effects, retained state, queued obligations and evaluation gaps separately.

Tests: guidance/command parity, actual command rejection without UI, stale/wrong-game/wrong-view intentions, save/reload at a boundary, idempotent retries, no duplicate Stun/cleanup, Clearance before Pin/TI removal, concealment before illumination removal, and no new gate for unread optional topics. Existing adjudicator permissions/rulings remain unchanged.

## 8. B5: Persistence and additional families

Where current history cannot supply needed origin, declaration or progress facts, add rule-specific typed event/state support in Units and an existing or new governed Play action. Round-trip through reader/writer/projector and test replay. Do not introduce an editable guide obligation database or a generic client `Resolve` event.

Expand in bounded slices: fire/vehicle/Spotter history; smoke/drift/cave WP/dust; packing/replenishment; Placed DC/tunnel/airborne; CC declarations/heroes; water/aerial/module profiles. Each slice needs sources/ruling, narrow Rules predicates, durable facts if required, command integration, display-safe result and focused tests before its capability is marked integrated.

Old saves missing facts return incomplete evaluation. Changes to legacy replay require their own explicit compatibility/version decision and fixtures. Do not infer dates from current marker presence. Unsupported required effects, when authoritatively established, must be exposed and handled in the shared planner gate, not dismissed through consideration.

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

Next work: B4 adapters, disclosure and shared phase-close parity for the admitted B3 slice, then additional rule-family proofs. Verify production capability registrations against actual Rules/Play proofs before admitting live support. The repository mock-up is preserved as design evidence, not a requirement to construct UI now.
