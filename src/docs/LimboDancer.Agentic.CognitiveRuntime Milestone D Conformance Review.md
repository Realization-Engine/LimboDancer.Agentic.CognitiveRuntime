# LimboDancer.Agentic.CognitiveRuntime Milestone D Conformance Review

**Status:** Approved with provider-entry conditions

**Review date:** 2026-09-22

**Reviewed branch:** `decision-plane`

**Baseline through:** PR-18, replay-capable decision evidence

**Authority:** Architecture and conformance checkpoint subordinate to the Plane Runtime Specification, Plane Runtime Design, Decision Plane Architecture, and Milestone A through C Conformance Reviews

## 1. Review decision

Milestone D is conformant for Effect Verification and replay-capable Decision evidence. PR-17 establishes an opt-in, deterministic verification boundary that compares trusted expected effects with post-execution observations and represents contradiction or uncertainty explicitly. PR-18 preserves enough immutable, tenant-scoped evidence to reconstruct the historical `DecisionContext`, candidate boundary, provider result, and downstream outcome without retaining executable authority.

One additional Decision-provider experiment may proceed under section 8. Each provider remains an empirical implementation candidate behind the existing `IDecisionProvider` contract, not an architectural dependency or authorization authority. This approval does not admit provider routing, automatic fallback, a replay engine, provider-generated candidates, or execution of replay results.

## 2. Verification and replay conformance evidence

| Milestone D criterion | Decision | Evidence |
| --- | --- | --- |
| Verification is opt-in and descriptor-controlled | Pass | `VerificationProfile` and orchestration invoke verification only for trusted descriptors that require it. |
| Executor success is not semantic proof | Pass | missing or failed evaluators produce `Unverifiable`; only registered effect evaluators can produce findings. |
| Verification uses post-execution observation | Pass | orchestration refreshes known observations after execution and supplies before/after sets in `VerificationContext`. |
| Verification outcomes are explicit | Pass | `Verified`, `PartiallyVerified`, `Unverifiable`, and `Contradicted` are preserved per effect and in aggregate. |
| Verification remains separate from Diagnostics | Pass | distinct contracts, evaluator routing, audit event, policy, and orchestration stage. |
| Contradiction cannot manufacture recovery authority | Pass | the default policy escalates or fails; compensation remains a separately governed future action. |
| Verification results are audited | Pass | `EffectVerifier` records action, authorization, execution, Goal, Step, aggregate status, and reason codes. |
| Historical Decision inputs are preserved | Pass | `RuntimeStepEvidence` retains Goal, runtime budget, observations and versions, descriptors and versions, and the original candidate set. |
| Rejected and permitted candidates remain distinguishable | Pass | evidence validates and retains the constraint partition; Governance-denial coverage proves rejected evidence. |
| Provider identity and complete result are preserved | Pass | `DecisionResult`, provider identity/version, confidence, distribution, latency, and cost remain available unchanged. |
| Downstream outcomes are correlated | Pass | evidence projects gate, execution, and verification results onto the same invocation and Step record. |
| Replay evidence is not execution authority | Pass | `GateEvidence` retains only an authorization identifier; neither it nor `RuntimeStepEvidence` contains `AuthorizedAction`. |
| Stale, blocked, failed, and multi-step attempts are retained | Pass | orchestration writes once per resolved attempt before terminal exit or retry; multi-step coverage proves distinct Step records. |
| Historical Decision context is reconstructable | Pass | `CapturedEvidenceReconstructsHistoricalDecisionContext` rebuilds `DecisionContext` from stored evidence. |
| Reference persistence is tenant-scoped | Pass | `InMemoryReplayEvidenceSink.Snapshot` requires a tenant and returns an immutable filtered snapshot. |
| Production composition remains fail closed | Pass | the Host registers the evidence sink but retains deny-all Goal admission and the deterministic rule provider by default. |

## 3. Decision-provider substrate finding

The Decision boundary is stable enough for comparative provider work:

1. Providers receive only `DecisionContext` and `IReadOnlyList<PermittedAction>`.
2. Rejected candidates never enter the provider call.
3. Provider identity and version must match the returned result.
4. Unknown selections and structurally invalid results are rejected and audited.
5. A validated selection creates `SelectedAction`, not `AuthorizedAction`.
6. Confidence, distributions, latency, and cost are evidence only.
7. The common Execution Gate independently revalidates every selected action.
8. Replay evidence can supply the same historical context and permitted candidate set to a comparison provider without invoking the gate or executor.

These properties allow provider quality to be tested without weakening the authority path.

## 4. Replay boundary findings

PR-18 implements replay-capable evidence, not replay execution. The stored record is sufficient to reconstruct the provider boundary and compare a future result with the original result. It deliberately exposes no operation that invokes a provider, materializes a new `SelectedAction`, reissues authorization, calls the Execution Gate, or invokes an executor.

The in-memory sink is a deterministic reference provider, not a production durability claim. A production evidence store still requires an explicit retention, redaction, encryption, access-control, and deletion design. Sensitive Goal inputs, candidate arguments, observations, provider data, and execution outputs remain subject to `SPEC-AUD-2` regardless of storage technology.

## 5. Evaluation requirements

An additional provider is admitted for evaluation only. Its PR must define a bounded decision class and a labeled corpus containing:

- the Decision context and permitted candidates;
- the expected candidate or set of acceptable candidates;
- explicit abstention and ambiguity cases;
- action risk classification; and
- the expected severity of a wrong choice.

At minimum, evaluation must report:

- selection correctness;
- abstention behavior;
- invalid-result rate;
- latency;
- token use and monetary cost where applicable;
- confidence or distribution calibration where the provider supplies them; and
- disagreement with the deterministic baseline.

Task-completion and effect-verification outcomes may be correlated when safely available, but replay evaluation must stop before authorization or execution.

## 6. Budget and failure requirements

The deterministic baseline does not consume token or cost budgets. A provider that incurs either resource must add enforcement at its invocation boundary before it can be enabled:

- cancellation and a finite timeout independent of provider cooperation;
- pre-invocation budget eligibility checks;
- post-invocation accounting for reported latency, tokens, and cost;
- explicit distinction between provider failure, abstention, and escalation; and
- no silent fallback or retry outside the admitted `RuntimeBudget`.

Provider SDK response types, credentials, prompts, and transport details must remain behind the provider implementation boundary. Hidden reasoning is neither required nor persisted.

## 7. Explicitly deferred

This review does not admit:

- provider routing or a composite provider;
- automatic fallback chains;
- execution of replayed Decisions;
- a generalized replay engine or replay API;
- calibration normalization across heterogeneous providers;
- provider-specific types in Abstractions;
- provider access to rejected candidates, executors, the Execution Gate, or authorization tokens;
- provider-controlled risk, permissions, preconditions, expected effects, or verification policy;
- enabling autonomous execution under the default Host admission policy; or
- a concrete ASL provider or domain package without its own bounded design slice.

## 8. Boundary admitted for PR-19

PR-19 may introduce exactly one concrete `IDecisionProvider` implementation together with the smallest labeled evaluation corpus and replay-only comparison utility needed to measure it against `RuleDecisionProvider`.

The provider must:

1. consume only the existing `DecisionContext` and permitted candidates;
2. return the existing `DecisionResult` contract;
3. use structured, schema-validated output when its transport is generative;
4. remain cancellable, time-bounded, and budget-accounted;
5. expose stable provider and model/version identity;
6. treat malformed, unavailable, or out-of-set output as provider failure rather than abstention;
7. avoid persisting prompts, secrets, or hidden reasoning in replay evidence;
8. remain disabled unless explicitly configured; and
9. leave the deterministic rule provider and Execution Gate unchanged.

No router or fallback chain may be introduced in the same PR.

## 9. Next checkpoint

The next implementation step is a short provider design note selecting the concrete PR-19 provider, its bounded decision class, configuration model, budget semantics, and evaluation corpus. Provider adoption is a later evidence-based decision; completing an implementation does not make that provider the production default.

A routing review is required only after at least two non-reference providers have comparable evidence. A separate review is required before replay evolves from offline provider comparison into any callable replay engine or simulation environment.
