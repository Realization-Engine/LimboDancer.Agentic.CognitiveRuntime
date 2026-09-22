# LimboDancer.Agentic.CognitiveRuntime Milestone C Conformance Review

**Status:** Approved

**Review date:** 2026-09-22

**Reviewed branch:** `decision-plane`

**Baseline through:** PR-16, Goal orchestration loop

**Authority:** Architecture and conformance checkpoint subordinate to the Plane Runtime Specification, Plane Runtime Design, Milestone A Conformance Review, and Milestone B Conformance Review

## 1. Review decision

Milestone C is conformant after preserving diagnostic lifecycle dispositions across the Execution Gate boundary. PR-15 and PR-16 now provide a bounded autonomous-execution substrate in which an admitted Goal can request observations, reason about a next semantic action, resolve only registered actions, pass semantic and Governance constraints, receive a validated Decision, pass diagnostics and the common Execution Gate, execute only through `AuthorizedAction`, and return to Reasoning for completion or another fully revalidated step.

PR-17 effect verification may proceed under section 8. This approval does not claim that successful executor return proves semantic effects, does not enable autonomous execution under the default Host admission policy, and does not admit an LLM provider, confirmation-resumption UI, replay engine, or concrete ASL domain package.

## 2. Autonomous-execution conformance evidence

| Milestone C criterion | Decision | Evidence |
| --- | --- | --- |
| Admission supplies identity and budget without giving orchestration authority to invent them | Pass | `IGoalAdmissionPolicy`, `GoalAdmissionResult`, tenant-match enforcement, and deny-by-default Host composition. |
| Goal, correlation, tenant, invocation, and step identity remain stable through the lifecycle | Pass | `GoalOrchestrator`, typed stage contexts, gate context checks, audit records, and orchestration tests. |
| Lifecycle transitions fail closed | Pass | `GoalLifecycleTransitionPolicy` and invalid-transition/terminal-state tests. |
| A canonical Goal completes through the complete autonomous authority path | Pass | `GoalOrchestratorTests.CanonicalGoalCompletesThroughGateAndExecutor`. |
| No candidate and Reasoning abstention terminate without execution | Pass | dedicated no-candidate and abstention tests. |
| Unknown or repeated Reasoning proposals fail closed | Pass | PR-15 Reasoning guard tests and orchestration use of the guarded engine. |
| Semantic or Governance denial cannot be overridden | Pass | constraint-pipeline typestate plus `GovernanceDenialCannotBeOverriddenByOrchestration`. |
| Decision selection remains bounded to permitted candidates | Pass | the approved Milestone B Decision Plane is composed unchanged. |
| Diagnostics execute before the final gate | Pass | selected descriptor profiles run through `IDiagnosticRunner`; findings enter `ExecutionContext` and are interpreted by the gate. |
| Diagnostic lifecycle dispositions retain meaning | Pass | `ExecutionGateResult.DiagnosticDisposition` plus retry, re-observe, escalation, and fail-goal orchestration tests. |
| Stale authorization causes re-observation and full revalidation | Pass | `StaleGateReobservesAndRevalidatesBeforeExecution`; stale proposals do not retain authorization. |
| Retry, observation-call, step, and deadline budgets are bounded | Pass | `RuntimeBudget` enforcement in the orchestrator and Reasoning guard; retry-exhaustion test. |
| Cancellation terminates explicitly | Pass | cancellation test returns the `Cancelled` lifecycle state without execution. |
| Multi-step work re-enters every authority boundary | Pass | `MultiStepGoalRevalidatesEverySelectedAction` proves distinct authorizations for two steps. |
| Only the Execution Gate creates execution authority | Pass | orchestration receives `AuthorizedAction` only from `IExecutionGate`; executors still accept only `AuthorizedAction`. |
| Execution is audited | Pass | orchestration invokes `AuditedActionExecutor`, preserving started/completed/failed audit evidence. |
| Host autonomous execution is fail closed by default | Pass | `DenyAllGoalAdmissionPolicy` and the Host composition assertion for `admission.policy_not_configured`. |
| Runtime remains domain-neutral and independent of legacy production | Pass | production dependency graph, architecture tests, and absence of concrete domain types in orchestration. |

## 3. Review observation closed

The first green PR-16 implementation composed diagnostics but collapsed every non-continuation diagnostic disposition into a generic `DiagnosticBlocked` result. That lost the distinction required by `SPEC-ORCH-5` between retry, re-observation, escalation, failure, and ordinary blocking.

The review closed the gap by carrying the evaluated `DiagnosticDisposition` on `ExecutionGateResult` and applying it in the bounded lifecycle:

```text
Retry      -> discard the attempted proposal -> run the authority path again
ReObserve  -> reacquire known observations -> run the authority path again
Escalate   -> terminal Escalated result
FailGoal   -> terminal Failed result
Block      -> terminal Abstained result
```

Retry and re-observation never reuse an `AuthorizedAction`. They return to Reasoning and must repeat registered resolution, constraints, Decision, diagnostics, and gate authorization.

## 4. Authority-boundary findings

The orchestration loop coordinates existing authorities without absorbing them:

1. Admission alone supplies the principal and budget.
2. Reasoning may propose semantic intent or request observations but cannot create candidates or authorization.
3. Registered resolution alone maps intent to server-owned `ActionDescriptor` instances.
4. The constraint pipeline alone creates `PermittedAction` typestate.
5. The Decision Plane alone validates provider output and creates `SelectedAction`.
6. Diagnostics can influence lifecycle control but cannot authorize execution.
7. The Execution Gate independently revalidates tenant, principal, descriptor identity, executor binding, permissions, current constraints, diagnostics, and risk.
8. Only an authorized gate result carries `AuthorizedAction`.
9. The audited executor accepts only that authorized type.
10. Every later step begins again at Reasoning and receives a new `StepId`, Decision, gate evaluation, and authorization.

The orchestrator has no method that constructs `AuthorizedAction`, changes a Decision selection, converts a rejected candidate into `PermittedAction`, or bypasses the registered executor resolver.

## 5. Budget, retry, and state findings

The initial budget enforcement covers the dimensions needed by the deterministic baseline:

- wall-clock deadline;
- maximum consequential action steps;
- maximum stale/diagnostic retries; and
- maximum observation-provider calls.

Stale gate results and retry-oriented diagnostic dispositions remove the unexecuted proposal from step history, consume retry budget, and re-enter the lifecycle. A changed observation version changes the Reasoning state fingerprint. Successfully executed steps remain in history and action outcomes, allowing Reasoning to complete or select a different next action while repeated-step diagnostics remain active.

Token and cost budgets remain present in the shared contract but are not consumed until a provider that incurs those resources is admitted.

## 6. Host and admission findings

The production Host composes `IGoalOrchestrator`, but autonomous execution is not enabled merely by resolving the service. The default `DenyAllGoalAdmissionPolicy` rejects every Goal with `admission.policy_not_configured`. A trusted interaction adapter must supply an admission policy that establishes an authenticated tenant-bound principal and explicit runtime budget.

The empty observation provider is likewise a fail-closed composition default. It creates no synthetic state and reports that no provider is configured.

## 7. Explicitly deferred

This review does not admit:

- treating successful executor return as proof of semantic effect;
- effect contradiction recovery or compensation;
- automatic confirmation resumption after human approval;
- LLM, Jev, classifier, or composite Reasoning/Decision providers;
- provider routing or comparative evaluation;
- replay-engine implementation;
- unrestricted infrastructure access from Reasoning;
- a generalized workflow, planning, or plugin framework;
- concrete ASL types in Abstractions or Runtime; or
- enabling autonomous execution without trusted admission.

## 8. Boundary admitted for PR-17

PR-17 may add opt-in effect verification for actions whose effects can be checked cheaply and deterministically. It may represent `Verified`, `PartiallyVerified`, `Unverifiable`, and `Contradicted` outcomes and may route contradiction to configured recovery or escalation.

Verification must compare authoritative expected effects with post-execution observations. It must not infer success solely from the executor result, become a second diagnostics framework, grant new authorization, or conceal unverifiable effects.

## 9. Next checkpoint

PR-17 may proceed under section 8. Replay-capable evidence may follow only after verification records contain stable inputs and outcomes. A separate review is not required for every increment, but the next milestone review should occur before provider evaluation broadens the intelligence surface in Milestone D.
