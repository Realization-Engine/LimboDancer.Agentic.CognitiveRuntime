# LimboDancer.Agentic.CognitiveRuntime Milestone B Conformance Review

**Status:** Approved

**Review date:** 2026-09-22

**Reviewed branch:** `decision-plane`

**Baseline through:** PR-14, Deterministic Decision Plane

**Authority:** Architecture and conformance checkpoint subordinate to the Plane Runtime Specification, Plane Runtime Design, and Milestone A Conformance Review

## 1. Review decision

Milestone B is conformant after closing the integrated-path proof gap described in section 3. PR-12 through PR-14 now provide a deterministic autonomous-selection substrate in which a tenant-scoped Goal can carry versioned observations through registered action resolution, semantic constraints, a bounded Decision provider, and validated `SelectedAction` materialization.

Reasoning implementation may proceed to PR-15. This approval does not authorize Goal orchestration, autonomous execution, an LLM provider, a generalized domain framework, or any path that bypasses the Execution Gate.

## 2. Autonomous-selection conformance evidence

| Milestone B criterion | Decision | Evidence |
| --- | --- | --- |
| Goal and Step identities remain typed and tenant-scoped | Pass | `GoalContractsTests`, orchestration-context tenant tests, and the integrated Milestone B conformance test. |
| Observations retain tenant, exact package where applicable, version, and provenance | Pass | `ObservationAcquisitionContractsTests`, `FakeDomainConformanceTests`, and `MilestoneBConformanceTests`. |
| Unknown action semantics create no executable candidate | Pass | `SemanticResolutionTests.UnknownSemanticIntentProducesNoExecutableCandidate`. |
| Action resolution returns a finite registered candidate set | Pass | `RegisteredActionResolver`, candidate uniqueness checks, and registered-intent tests. |
| Required semantic constraints fail closed | Pass | Semantic pipeline tests for passing, failed, indeterminate, missing-evaluator, unhandled-class, and duplicate-candidate cases. |
| Decision receives only `PermittedAction` | Pass | `IDecisionProvider` signature and `DecisionContractsTests.DecisionProviderConsumesOnlyPermittedActions`. |
| Empty permitted sets bypass the provider | Pass | `DecisionPlaneTests.EmptyPermittedSetBypassesProviderAndAbstains`. |
| Invalid provider selections are rejected | Pass | Decision-result validator tests and `DecisionPlaneTests.UnknownProviderSelectionIsRejectedAndAudited`. |
| Abstention and escalation create no selection | Pass | deterministic provider and non-selection outcome tests. |
| Valid deterministic Decision materializes `SelectedAction` | Pass | Decision Plane tests and the integrated Milestone B conformance test. |
| Confidence creates no authority | Pass | `DecisionPlaneTests.ConfidenceNeverCreatesAuthorization`; Decision contracts expose no authorization value. |
| Decision evidence is audited | Pass | accepted and rejected Decision audit tests preserve invocation, Goal, Step, provider, candidate, outcome, reason, and confidence fields. |
| Selection is revalidated before authorization | Pass | `DecisionPlaneTests.DecisionSelectionIsRevalidatedByExecutionGate`. |
| `DomainConclusion` remains outside action authority | Pass | domain conclusion contract and fake-domain conformance tests. |
| Runtime remains domain-neutral and independent of legacy production | Pass | architecture tests, fake-domain removability test, and production dependency graph. |
| Host composes selection services without a concrete domain | Pass | Host composition resolves action resolution, constraint, package, provider, and Decision Plane services with an empty domain registry. |

## 3. Review observation closed

PR-12 through PR-14 initially proved every stage independently but did not include one test that joined the complete Milestone B action-selection path.

The review added `MilestoneBConformanceTests.GoalProgressesThroughObservationResolutionConstraintsAndDecisionToSelection`. It proves in one deterministic flow:

```text
Goal
  -> versioned Observation
  -> registered ActionCandidate
  -> required semantic constraint
  -> PermittedAction
  -> deterministic Decision
  -> SelectedAction
```

The test also proves that observation evidence and state versions reach the candidate, Decision evidence reaches the selection and audit record, and no `AuthorizedAction` is created.

## 4. Authority-boundary findings

The autonomous selection substrate preserves the authority hierarchy:

1. Reasoning is not yet present and therefore cannot manufacture candidates or authority.
2. `RegisteredActionResolver` can resolve only a descriptor already present in the trusted registry.
3. `SemanticActionConstraintPipeline` alone creates `PermittedAction` typestate and fails closed for missing required evaluation.
4. `IDecisionProvider` receives only permitted candidates.
5. `DecisionPlane` validates provider identity and candidate containment before creating `SelectedAction`.
6. `SelectedAction` retains Decision evidence but is not authorization.
7. The Execution Gate independently re-resolves the descriptor, evaluator, permissions, diagnostics, risk, and current constraint state.
8. Only the Execution Gate can create `AuthorizedAction`.
9. Executors continue to accept only `AuthorizedAction`.

Neither provider confidence nor a `DomainConclusion` changes this ordering.

## 5. Domain and evidence findings

The Milestone A domain boundary remains intact:

- package resolution is exact-version and never silently floats;
- observation acquisition is bounded and tenant/package scoped;
- entity resolution distinguishes resolved, unresolved, and ambiguous results;
- conclusion resolution receives already supplied semantics and evidence rather than arbitrary infrastructure access;
- unknown, ambiguous, stale, conflicting, and cross-tenant inputs cannot produce an unqualified definitive conclusion; and
- the fake domain remains test-only and removable.

Milestone B does not admit a concrete ASL package. The selected ASL occupied-building eligibility scenario remains the reference pressure test for later domain realization.

## 6. Decision and audit findings

The deterministic provider is deliberately narrow:

- zero candidates produces abstention;
- one candidate produces selection;
- multiple candidates produce escalation.

The Decision Plane bypasses the provider entirely when no candidate survives constraints. Accepted Decisions and rejected provider results are recorded with stable structured evidence. The audit record does not contain hidden reasoning or grant authority.

Provider routing, comparative evaluation, calibration analysis, and replay remain later work.

## 7. Explicitly deferred

This review does not admit:

- autonomous Goal orchestration or execution;
- direct Reasoning access to executors, authorization, or infrastructure;
- LLM, Jev, classifier, or composite Decision providers;
- provider routing;
- generalized planning, workflow, rule-engine, or plugin frameworks;
- human-confirmation UI;
- replay engine implementation;
- concrete ASL types in Abstractions or Runtime; or
- weakening of gate revalidation because a deterministic provider selected the candidate.

## 8. Boundary admitted for PR-15

PR-15 may introduce the smallest deterministic Reasoning boundary required before orchestration depends on it. It may support:

- structured Goal interpretation;
- proposal of an already registered semantic action intent;
- bounded missing-observation requests;
- a minimal plan or subgoal proposal only where the orchestration contract requires one; and
- a result-synthesis boundary.

The initial implementation must be deterministic and must not require an LLM. Reasoning output remains a proposal and must pass through registered resolution, constraints, Decision, and the Execution Gate.

PR-15 must also add bounded diagnostics for repeated next steps, repeated proposals without state change, unresolved semantic references, and Reasoning step/deadline budgets.

## 9. Next checkpoint

PR-15 may proceed under section 8. PR-16 may compose the approved Reasoning, resolution, Decision, gate, execution, and verification boundaries into a bounded Goal loop, but it may not move authority into orchestration.

A separate Milestone C review should occur after PR-16 proves the first complete autonomous execution path and before effect-verification and replay evidence broaden the operational surface.
