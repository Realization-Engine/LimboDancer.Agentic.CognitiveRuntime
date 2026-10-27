# B5 acceptance and U1 handoff

Date: 2026-10-27

Status: **B5 accepted; U1 eligible.** All six criteria below pass. No required B5 slices remain.

Scope: final acceptance of the admitted B5 backend. The all-Passes ASL.Rules design remains the destination. This document does not claim full rulebook execution. The saved HTML preview remains a design artifact; U1 will build a thin Blazor consumer after this acceptance gate passes.

## Capability and evidence matrix

Every profile retains all eight phases, 538 reviewed topics, 38 dependency definitions and 191 topic/dependency links. An absent evaluator means NotEvaluated, never inapplicable. A source citation or Rules predicate is not command support.

| Surface | Admitted evidence and behavior | What U1 must retain as pending | Named proofs |
|---|---|---|---|
| Reference catalog and stable timing | Typed topics, references, glossary, source review, windows and inventory bindings in every profile | Missing source, open interpretation, unregistered evaluators and commands | `TurnGuideCatalogTests`, `TurnCapabilityInventoryTests`, `TurnGuideServiceTests` |
| Stun/CX obligations | Origin-aware timing, own cleanup, evidence of resolution through existing engine | Unsupported instances and missing legacy origins; marker absence is not resolution | `TurnObligationRulesTests`, `TurnObligationTests`, `TurnGuideCxUsesRealHistoryAndExistingCleanupExactlyOnce` |
| Placed DC | Durable ownership, occurrence-specific opening review, interrupted/due/resolved lifecycle, conditional owning-side route and normal detonation pipeline | Full opening-rule automation, other A23 variants, missing ownership/review and unsupported decisions | `TurnPlacedDcWindowRulesTests`, `BacklogPass15Tests`, `TurnPlacedDcGuidanceTests` |
| Fire and Spotter | Authorized observed history and retention in fire and later profiles | New designation, replacement, Opportunity Fire and remaining execution rules | `TurnFireHistoryTests`, `TurnSpotterRulesTests`, `BacklogPass9Tests` |
| Atmosphere | Ordinary grenade lifecycle and recorded Wind/Gust in atmosphere and work profiles | White SMOKE, drift, cave WP, dust and unimplemented effects of recorded weather | `TurnAtmosphereTests`, `BacklogPass9Tests`, `RecordedGustRemainsVisibleAtAdvancingFireWithoutBeingAppliedOrRerolled` |
| Work | Adjudicated progress, durable lifecycle and bounded assembly/replenishment/transfer/offload effects in work profile | Automatic eligibility, packing semantics, animal transport, broader E10 and Ammo Dump use; Applied does not clear Pending rule automation | `TurnWorkProgressTests`, `TurnWorkDisclosureTests`, `TurnWorkRulesTests`, `TurnWorkLifecycleTests`, `TurnWorkEffectsTests`, Play work/effect/supply tests |
| Other dependencies | Definitions remain discoverable; some pure timing predicates exist | Trip Flare/Clearance command integration and other unadmitted families | `TurnObligationTests`, `TurnHostCapabilityBoundsTests`, inventory tests |
| Phase close | Shared existing command assessment, authorized projection and governed phase-advance action | Additional unintegrated cleanup and rule families | `TurnPhaseCloseTests`, `TurnGuideUsesFullCommandChecksWithoutWritingOrRolling` |

Profile IDs and versions remain unchanged from R5: `existing-engine` / `b5-r2-base`, `existing-engine-dc-v1` / `b5-r5-dc`, `existing-engine-fire-v1` / `b5-r5-fire`, `existing-engine-atmosphere-v1` / `b5-r5-atmosphere`, and `existing-engine-work-v1` / `b5-work-8`. The base has no Placed DC route. Later profiles add their stated payloads, not general permission to execute all topics.

## Six acceptance criteria

| Plan section 9 criterion | Acceptance evidence |
|---|---|
| 1. Complete reviewed inventory and pending visibility | `InventoryAccountsForEveryIdentityAndReferenceLinkWithoutPromotingCapabilities` checks 538 topics, 38 dependencies, 191 links, nine deferred packages and every binding. `HeadlessConsumerBrowsesEveryPhaseAndConfirmsAnOfferedDecision` browses every phase through the real host without losing options. |
| 2. Stable typed contracts and distinct capabilities/sources | `TurnGuideServiceTests`, `TurnHostCapabilityBoundsTests`, `TurnPlacedDcGuidanceTests` cover scoped composition, bounded registrations and serialized routes; inventory tests preserve source status separately from command support. |
| 3. Live decision and cross-phase lifecycle | `APlacedDcIsOperableOnceItsPlacerMovesOnAndDetonatesInTheAfph`, `PlacedDcRouteExecutesThroughNormalConfirmationAndDurableRetry`, the new consumer example, and `TurnGuideCxUsesRealHistoryAndExistingCleanupExactlyOnce` cover origin, due window, interruption, resolution and cross-phase cleanup. |
| 4. Headless reads and one command authority | ASL.Turns references Rules, not Blazor or Play. The consumer example proves browsing and proposal do not append events. `TurnGuideUsesFullCommandChecksWithoutWritingOrRolling` checks read purity and full assessment parity. Existing route tests prove permission and confirmation cannot be bypassed. |
| 5. Disclosure, ownership and stale rejection | R5's five-profile acceptance matrix, `AcceptancePrivateChoiceCannotChangePassiveOwnerRouteDisclosure`, `TurnGuideRejectsWrongPerspectiveStaleMapAndStaleRevision`, and `PlacedDcRouteRejectsStaleConfirmationAndCannotTreatUnknownReviewAsPermission`. |
| 6. Shared close semantics and exactly-once replay | `TurnGuideUsesFullCommandChecksWithoutWritingOrRolling`, the CX cleanup test, `PlacedDcRouteExecutesThroughNormalConfirmationAndDurableRetry`, and R5 replay/completion tests check planner parity, retry identity, terminal effects and persisted reload. |

R5 comparisons hold disclosed phase, own facts and review currency constant. R3's any-event review expiry remains in force. This acceptance is bounded to admitted histories and capabilities, not a universal privacy or rules-completeness claim.

## Executable headless consumer example

The compiled example is [BacklogPass15Tests.Handoff.cs](../../tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass15Tests.Handoff.cs), method `HeadlessConsumerBrowsesEveryPhaseAndConfirmsAnOfferedDecision`. It uses actual Play APIs and the existing fixture's normal placement and authorized opening review. The fixture's adjudication is a prerequisite supplied by a separate authorized actor, never an inference made by the future component.

1. The trusted server selects the game, authenticated perspective and supported profile. Call `ReadTurnGuideProfileAsync` for the browsed phase. `ActualPhaseId` and `BrowsedPhaseId` are distinct. Browsing another phase does not advance the game.
2. Display every `Guide.Options` entry, source/timing information, decisions and pending explanations. Display disclosed `Obligations`, `DecisionWindows`, optional history/work payloads and `PhaseClose.EvaluationGaps`. Missing facts are visible gaps, not unchecked tasks whose completion the UI can invent.
3. Match an offered action's opaque token to the decision's `ActionRoute`. A Placed DC route requests a governed check. Required means the obligation needs consideration; it does not certify private readiness or grant permission.
4. On an explicit player request, the trusted server calls `PreparePlacedDcDecisionAsync` with its authenticated perspective, the selected action and a new attempt ID. Null means refresh the guide. Never construct tokens, bind a client-selected adjudicator or expose raw command arguments to the guide view.
5. Keep the exact prepared arguments on the server. Use `GamePlay.ProposeAsync(GameActions.DetonateDc, args, principal)`. If the result needs confirmation, show the existing action panel and confirm only after the user confirms, with the same arguments and correlation. Handle denied/stale/choice outcomes through existing Play behavior.
6. Refresh after outcomes. The example observes Due becoming Resolved, an expired route being rejected and the identical committed request returning Replay without another event. Historical reads recover the earlier obligation but offer no actions.

For phase close, display the server's `PhaseClose` projection and use the existing governed `AdvancePhase` action. Side views intentionally say GovernedCheckRequired rather than reveal private blockers. An adjudicator's Ready assessment is still not a commit. Do not derive closure from checked boxes, empty visible obligations or locally calculated rule timing. Existing choice, dice and follow-up processing stays in Play.

## U1 implementation boundary

U1 may compose a phase browser, reference/checklist display, disclosed obligations and existing action panels using site styling. It must retain Pending, NotEvaluated, historical read-only, source-missing and interpretation-open states. Preserve options under user filters by offering an explicit way to reveal pending content; never equate unsupported with unavailable.

Considered/bookmarked flags are private preferences. They cannot resolve obligations, satisfy missing facts, remove a counter or close a phase. New rule execution belongs in Rules/Units/Play with durable evidence and tests, then a reviewed profile admission. No generic UI completion endpoint or second scheduler is needed.

Uncommitted route tokens are process-local and state-bound: refresh after restart or state changes. Retain committed attempt arguments for exact retry. Do not cache authoritative guidance solely by phase name or combine perspectives/profile versions. Authorization and host disclosure remain server responsibilities.

The nine deferred packages B5-D1 through B5-D9 remain owned by the inventory and implementation plan. Their source gaps and pending explanations remain visible. They are later Rules-pass work, not additional required B5 slices. The frozen preview is not regenerated by this backend acceptance.

## Final verification

179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). All 97 focused Play acceptance/regression cases, 49 focused Rules cases and 421 Units cases pass. The consumer example also passes a final targeted rerun after strengthening its pre-proposal revision assertion. Coverage applies to the entire ASL.Turns assembly, not the Play, Rules or Units assemblies.

The fresh gate is `tests/ASL.Turns.Tests/Test-Coverage.ps1`. Play selection covers Acceptance, BacklogPass15Tests, BacklogPass9Tests, TurnGuide, TurnWork, WorkEffects, WorkAudit, SupplyEffects, action registration and the recorded Wind/Gust theory. Rules selection covers TurnObligationRulesTests, TurnPlacedDcWindowRulesTests, TurnSpotterRulesTests, TurnWorkRulesTests, TurnWorkLifecycleTests and TurnWorkEffectsTests. Units runs its complete regression suite for persisted event compatibility. No source exclusions, rules promotion, new event schema or profile version change was needed in R6.
