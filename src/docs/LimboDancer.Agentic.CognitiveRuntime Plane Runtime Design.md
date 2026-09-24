# LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design

**Status:** Design specification  
**Branch:** `decision-plane`  
**Scope:** Target runtime design for the LimboDancer plane architecture  
**Supporting analysis:** `LimboDancer.Agentic.CognitiveRuntime Plane Architecture Analysis.md`, `LimboDancer.Agentic.CognitiveRuntime Plane Architecture Codebase Validation.md`, `LimboDancer.Agentic.CognitiveRuntime Runtime Orchestration Model.md`, `LimboDancer.Agentic.CognitiveRuntime Decision Plane Architecture.md`

**Reference-domain requirements:** `src/ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL Reference-Domain Requirements.md`


**Canonical system identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**.NET root namespace / project prefix:** `LimboDancer`  
**Legacy code status:** Existing `LimboDancer.MCP.*` projects are reference implementations only. New architecture code SHALL NOT depend on them and they are intended for deletion after replacement.
**Legacy archive root:** `src/_Legacy/` (archival only; retained, not built, not referenced)  

## 1. Purpose

This document specifies the target runtime design for LimboDancer.Agentic.CognitiveRuntime.

The preceding analysis documents established the architectural rationale. This document converts that analysis into normative design.

It defines:

- the canonical runtime vocabulary;
- the six architectural planes;
- Governance, Diagnostics, and Orchestration;
- the runtime authority model;
- Goal lifecycle;
- semantic action model;
- directed and autonomous invocation;
- action resolution;
- evidence-backed domain conclusions and explanation;
- deterministic constraints;
- Decision Plane boundaries;
- execution authorization;
- preconditions and effects;
- observation, diagnostics, and verification;
- audit and replay;
- concurrency and stale-state handling;
- failure, retry, escalation, and confirmation behavior;
- host/runtime boundaries;
- dependency rules;
- clean reimplementation from the current implementation;
- target .NET platform and framework lifecycle policy;
- legacy-code retirement.

This document intentionally stops short of prescribing every C# type or project layout. Those are implementation decisions derived from this design.

## 2. Design Objective

LimboDancer SHALL operate as an ontology-constrained cognitive runtime.

The runtime SHALL transform goals that require operational work into governed actions through explicit authority transitions.

The runtime SHALL also support goals whose successful terminal outcome is an evidence-backed domain conclusion rather than a state-changing action. Producing a conclusion SHALL NOT implicitly create execution authority.

The canonical authority flow is:

```text
Interaction connects.
Reasoning proposes.
Semantics constrains.
Governance permits.
Decision selects.
Execution acts.
State remembers.
Orchestration coordinates.
Diagnostics assure.
```

No single model, protocol, tool, database, or provider defines the runtime.

## 3. Normative Language

The terms **SHALL**, **SHALL NOT**, **SHOULD**, **SHOULD NOT**, and **MAY** are normative.

- **SHALL / SHALL NOT** indicate required design behavior.
- **SHOULD / SHOULD NOT** indicate the expected design unless a documented exception exists.
- **MAY** indicates an optional capability.

## 4. Canonical Runtime Vocabulary

The following terms SHALL have consistent meanings throughout LimboDancer.

### 4.1 Goal

A **Goal** is a desired outcome submitted to the runtime.

A Goal is not an action and is not a model prompt.

Examples:

- retrieve recent session history;
- determine the most relevant memory;
- confirm a reservation;
- reconcile an entity with current graph state.

### 4.2 Observation

An **Observation** is state acquired from the world or LimboDancer's State Plane.

An Observation may originate from:

- relational state;
- graph state;
- vector retrieval;
- ontology state;
- an external system;
- an execution result;
- user input.

Observations are evidence. They are not decisions.

### 4.3 Plan

A **Plan** is a Reasoning Plane proposal describing how a Goal may be advanced.

A Plan SHALL NOT grant execution authority.

Plan steps SHALL be re-evaluated against current state when they become actionable.

### 4.4 Semantic Action

A **Semantic Action** is a domain capability identified independently of its transport or executor.

Example:

```text
ldm:action/HistoryRead
```

A semantic action is not synonymous with an MCP tool name.

### 4.5 ActionDescriptor

An **ActionDescriptor** is the authoritative server-side definition of an executable semantic action.

It binds semantic identity to execution-relevant metadata.

### 4.6 Action Binding

An **Action Binding** maps an external or internal invocation identity to an ActionDescriptor and ultimately to an executor.

Examples:

```text
MCP history_get -> ldm:action/HistoryRead
HTTP route      -> ldm:action/SomeAction
workflow step   -> ldm:action/SomeAction
```

### 4.7 ActionCandidate

An **ActionCandidate** is an ActionDescriptor grounded with arguments and evidence for a specific runtime context.

Candidate status does not imply permission.

### 4.8 PermittedAction

A **PermittedAction** is a candidate that has passed the deterministic semantic and governance constraints required before autonomous selection. Directed invocation may produce a SelectedAction from an explicitly requested, constraint-valid action without invoking the Decision Plane.

### 4.9 Decision

A **Decision** is the Decision Plane result over a finite set of PermittedActions.

A Decision MAY:

- select;
- abstain;
- escalate.

### 4.10 SelectedAction

A **SelectedAction** is an action that has passed the applicable semantic and governance constraints and has been selected either explicitly by the caller in directed invocation or by the Decision Plane in autonomous invocation.

Selection is not final execution authorization.

### 4.11 AuthorizedAction

An **AuthorizedAction** is a SelectedAction that has passed the final Execution Gate.

Only an AuthorizedAction may produce a consequential execution.

### 4.12 Executor

An **Executor** performs the operational work associated with an AuthorizedAction.

Executors SHALL NOT determine whether an action should have been selected.

### 4.13 Effect

An **Effect** is an expected semantic state transition associated with successful action execution.

### 4.14 Verification

**Verification** compares expected effects with observed post-execution state.

### 4.15 OrchestrationContext

The **OrchestrationContext** is the typed runtime envelope carrying identity, Goal, observations, stage results, budgets, action state, and audit correlation through the lifecycle.

It SHALL NOT become an unstructured shared-state dictionary.

### 4.16 Diagnostic Check

A **Diagnostic Check** is a registered, purposeful evaluation of a runtime invariant, readiness condition, integrity property, or behavioral expectation for a defined execution context.

A Diagnostic Check produces evidence. It does not itself grant or revoke authority.

### 4.17 Diagnostic Finding

A **Diagnostic Finding** is the structured result of a Diagnostic Check.

A finding SHALL identify at least:

- check identity;
- execution phase;
- status or severity;
- stable reason code;
- human-readable summary;
- relevant evidence or evidence references;
- timestamp;
- correlation and tenant context where applicable.

### 4.18 Diagnostic Policy

**Diagnostic Policy** is governed policy that maps a Diagnostic Finding to an allowed runtime disposition. It is not an independent authority domain.

Permission-affecting dispositions are enforced through Governance and the Execution Gate. Lifecycle-control dispositions are coordinated by Orchestration.

Possible dispositions include:

```text
Continue
ContinueDegraded
Retry
ReObserve
Escalate
Block
FailGoal
```

Diagnostics detect conditions. Diagnostic Policy determines their operational consequence.

### 4.19 DomainConclusion

A **DomainConclusion** is a semantic interpretation of a question or proposition grounded in authoritative material, observations, applicable rules and exceptions, and deterministic calculations.

A DomainConclusion is not an ActionCandidate, SelectedAction, AuthorizedAction, or permission to mutate state.

A DomainConclusion SHOULD carry or reference:

- the question or proposition evaluated;
- a definitive, qualified, indeterminate, or abstention disposition;
- applicable semantic rules and controlling exceptions;
- material observations and calculated facts;
- source, ontology, and state-version provenance;
- assumptions, ambiguity, and missing or conflicting evidence;
- an explanation suitable for the caller.

The exact implementation contract is deferred until a concrete reference-domain slice justifies it.

## 5. Platform Baseline and Clean-Reimplementation Strategy

The new architecture SHALL be implemented as a clean .NET namespace and project family under `LimboDancer.*`. The product/repository identity remains `LimboDancer.Agentic.CognitiveRuntime`.

The existing `LimboDancer.MCP.*` projects SHALL be treated as a source of proven behavior, algorithms, tests, schemas, and infrastructure knowledge, not as production dependencies of the new runtime.

The migration model is:

```text
Legacy implementation
LimboDancer.MCP.*
        |
        | inspect / copy / refactor / re-test
        v
New implementation
LimboDancer.*
        |
        v
feature + conformance parity
        |
        v
retire and delete LimboDancer.MCP.*
```

The objective is not source-level preservation. Code copied from the legacy implementation SHOULD be cleaned, decomposed, renamed, re-tested, and repositioned according to the new plane/fabric architecture before it is admitted to the new runtime.

### 5.1 Target Framework

New production projects SHALL initially target:

```text
TargetFramework: net10.0
Language baseline: C# 14
```

.NET 10 is the current Long Term Support baseline for the implementation phase.

The solution SHOULD remain current on supported .NET 10 servicing releases.

### 5.2 .NET 11 Upgrade Checkpoint

.NET 11 SHALL be treated as an explicit upgrade checkpoint after General Availability.

The architecture SHALL NOT depend on .NET 11 preview or release-candidate-only APIs unless a separate, documented decision changes this rule.

After .NET 11 GA, the project SHALL evaluate:

- SDK and runtime stability;
- ASP.NET Core compatibility;
- EF Core compatibility;
- MCP and AI package compatibility;
- Azure client-library compatibility;
- diagnostic and observability dependencies;
- deployment environment readiness;
- material runtime or language features useful to this architecture.

If the evaluation is favorable, the target framework MAY move to `net11.0` before the first production release.

### 5.3 Namespace Isolation

All newly implemented architecture code SHALL use the `LimboDancer.*` root namespace family.

New production projects SHALL use `LimboDancer.*` namespaces and SHALL NOT use `LimboDancer.MCP.*` namespaces for newly authored runtime types.

### 5.4 Zero Legacy Production Dependency

There SHALL be no production project reference from any new `LimboDancer.*` project to any `LimboDancer.MCP.*` project.

This is a hard architectural boundary.

Legacy code MAY be:

- inspected;
- copied;
- adapted during development;
- used as behavioral reference;
- compared in tests;
- used to derive compatibility fixtures.

Legacy assemblies SHALL NOT be required at runtime by the new architecture.

### 5.5 Copy-Forward Admission Rule

Every copied or reimplemented legacy capability SHALL pass three questions before entering the new runtime:

1. Does this behavior still belong in the new architecture?
2. Is the responsibility located in the correct plane or cross-cutting fabric?
3. Can the implementation be simplified, hardened, or made more testable while preserving required behavior?

Copying a class into a new namespace without architectural review SHALL NOT constitute migration completion.

### 5.6 Legacy Archive

The legacy `LimboDancer.MCP.*` projects are retained under `src/_Legacy/` for archival purposes only. The new runtime SHALL NOT depend on them, and SHALL reach independence from legacy behavior by satisfying:

- required functional parity;
- specification conformance;
- tenant-isolation tests;
- diagnostic conformance;
- protocol compatibility requirements that remain in scope;
- State migration requirements;
- operational deployment validation.

Legacy independence is an intended end state, not an optional cleanup task. The archive is not deleted when it is reached.

### DESIGN RULE PLAT-1

New architecture projects SHALL target `net10.0` until the explicit .NET 11 GA upgrade review is completed.

### DESIGN RULE PLAT-2

New production code SHALL use the `LimboDancer.*` namespace family.

### DESIGN RULE PLAT-3

New production projects SHALL NOT reference `LimboDancer.MCP.*` projects.

### DESIGN RULE PLAT-4

Legacy code SHALL be treated as reference source, not as an architectural dependency.

### DESIGN RULE PLAT-5

Copied legacy code SHALL be reviewed against current plane ownership, Governance, Diagnostics, tenant isolation, and execution-authority rules before admission.

### DESIGN RULE PLAT-6

The completed architecture SHALL support deletion of all legacy `LimboDancer.MCP.*` production projects.

## 6. Architectural Planes

LimboDancer SHALL be modeled as six logical planes.

```text
+---------------------+
|  INTERACTION PLANE  |
+----------+----------+
           |
           v
+---------------------+
|   REASONING PLANE   |
+----------+----------+
           |
           v
+---------------------+
|   SEMANTIC PLANE    |
+----------+----------+
           |
           v
+---------------------+
|   DECISION PLANE    |
+----------+----------+
           |
           v
+---------------------+
|   EXECUTION PLANE   |
+----------+----------+
           |
           v
+---------------------+
|     STATE PLANE     |
+---------------------+
```

The diagram shows conceptual progression, not compile-time dependencies or deployment boundaries.

### 5.1 Interaction Plane

The Interaction Plane SHALL own protocol and presentation concerns.

Examples:

- MCP;
- HTTP;
- SSE;
- CLI;
- Blazor;
- future agent-to-agent interfaces.

It SHALL normalize protocol-specific requests into runtime requests.

It SHALL NOT define semantic authority.

### 5.2 Reasoning Plane

The Reasoning Plane SHALL own open-ended interpretation and planning.

It MAY:

- interpret goals;
- decompose goals;
- identify missing observations;
- produce plans;
- revise plans;
- synthesize results.

It SHALL NOT create executable capabilities that do not exist in the Semantic Plane.

### 5.3 Semantic Plane

The Semantic Plane SHALL own domain meaning and semantic action applicability.

It SHALL include:

- ontology;
- entities;
- relations;
- properties;
- semantic action definitions;
- semantic preconditions;
- expected effects;
- semantic mappings;
- action resolution.

### 5.4 Decision Plane

The Decision Plane SHALL choose among explicit PermittedActions.

It SHALL NOT:

- authorize;
- override failed preconditions;
- create new privileged actions;
- execute actions.

### 5.5 Execution Plane

The Execution Plane SHALL perform AuthorizedActions.

It SHALL contain or host the execution boundary for:

- the final execution gate;
- executor bindings;
- operational application capabilities;
- effect realization;
- post-execution observation hooks.

### 5.6 State Plane

The State Plane SHALL provide durable and ephemeral state.

It includes:

- PostgreSQL;
- Cosmos Gremlin;
- Azure AI Search;
- ontology persistence;
- caches;
- execution coordination state where appropriate.

State SHALL NOT own semantic interpretation merely because semantic data is persisted there.

## 7. Governance and Control Fabric

Governance SHALL cross all planes.

Governance includes:

- identity;
- tenancy;
- authorization;
- policy;
- risk;
- confirmation;
- quotas;
- secrets;
- audit;
- compliance constraints.

Governance MAY consume telemetry and Diagnostic Findings as policy evidence, but telemetry and Diagnostics remain distinct architectural concerns.

```text
+------------------------------------------------------+
|             GOVERNANCE / CONTROL FABRIC             |
| identity | tenant | authorization | policy | risk   |
| audit | compliance | confirmation | quotas | secrets |
+------------------------------------------------------+
        |        |        |        |        |        |
        v        v        v        v        v        v
 Interaction Reason Semantic Decision Execution State
```

### DESIGN RULE G-1

Tenant scope SHALL be present or structurally derivable at every consequential runtime stage.

### DESIGN RULE G-2

Probabilistic confidence SHALL NOT grant authorization.

### DESIGN RULE G-3

Governance denials SHALL NOT be converted into Decision candidates.

### DESIGN RULE G-4

Governance decisions SHALL emit structured reason codes suitable for audit.

## 8. Diagnostic Fabric

Diagnostics SHALL be a first-class cross-cutting runtime assurance capability.

Diagnostics asks:

> Is this part of the runtime healthy, coherent, correctly configured, and behaving within expected bounds for this execution context?

Diagnostics SHALL NOT be modeled as a seventh plane.

It spans every plane and may execute at lifecycle transitions, within a phase, and after consequential execution.

```text
+------------------------------------------------------+
|                 DIAGNOSTIC FABRIC                    |
| health | integrity | validity | readiness | drift   |
| consistency | correctness | performance | behavior  |
+------------------------------------------------------+
        |        |        |        |        |        |
        v        v        v        v        v        v
 Interaction Reason Semantic Decision Execution State
```

Diagnostics SHALL remain distinct from Governance, Observability, and Effect Verification.

```text
Telemetry
    = raw signals and measurements

Observability
    = understanding runtime behavior from signals

Diagnostics
    = explicit checks against expected invariants or conditions

Governance
    = authority and policy response

Effect Verification
    = comparison of expected semantic effects with observed post-state
```

A Diagnostic Finding MAY inform Governance or Orchestration, but the finding itself SHALL NOT constitute authorization.

### 7.1 Diagnostic Categories

The runtime SHALL support three broad diagnostic categories.

| Category | Question | Example |
|---|---|---|
| Structural | Is the runtime assembled correctly? | Does every ActionDescriptor resolve to a registered executor binding? |
| Contextual | Is this execution context internally valid? | Does the current candidate belong to the active tenant and descriptor version? |
| Behavioral | Is the runtime behaving within expected bounds? | Is repeated identical reasoning occurring without state change? |

Structural diagnostics MAY run at startup, deployment validation, registry publication, or on demand.

Contextual diagnostics SHOULD run within the Goal lifecycle where the relevant execution context exists.

Behavioral diagnostics MAY combine current execution evidence with telemetry and historical baselines.

### 7.2 Diagnostic Positions

Diagnostic Checks SHALL support execution positions appropriate to their purpose.

```text
Pre-flight
    before a phase or consequential action

In-flight
    while work is executing

Post-flight
    after a phase or consequential action
```

Pre-flight diagnostics SHOULD evaluate readiness, integrity, configuration, mappings, dependency availability, and contextual invariants.

In-flight diagnostics MAY evaluate timeouts, cancellation, retry behavior, duplicate execution, resource consumption, and dependency degradation.

Post-flight diagnostics SHOULD evaluate result integrity, unexpected side effects, state consistency, audit completeness, and runtime anomalies.

### 7.3 Diagnostic Context

Diagnostic Checks SHALL execute against an explicit, typed DiagnosticContext or a plane-specific context derived from the OrchestrationContext.

A DiagnosticContext SHOULD expose only information needed by the check.

Conceptually:

```text
DiagnosticContext
├── RuntimeInvocationId
├── GoalId / StepId, when applicable
├── CorrelationId
├── TenantId
├── Principal
├── Plane
├── Lifecycle phase
├── Diagnostic position
├── ActionDescriptor / version, if applicable
├── Candidate / Selected / Authorized action, if applicable
├── Relevant observations and versions
├── Execution result, if applicable
├── Expected and observed effects, if applicable
├── Budget and timing information
└── Trace / audit references
```

Diagnostic contexts SHALL NOT become a mechanism for bypassing plane contracts or exposing unrestricted global state.

### 7.4 Diagnostic Check Contract

Diagnostic Checks SHOULD be independently registered and testable.

A conceptual contract is:

```csharp
public interface IDiagnosticCheck<in TContext>
{
    string Id { get; }
    DiagnosticPhase Phase { get; }
    DiagnosticPosition Position { get; }

    Task<DiagnosticResult> EvaluateAsync(
        TContext context,
        CancellationToken ct = default);
}
```

A conceptual result is:

```csharp
public sealed record DiagnosticResult(
    string CheckId,
    DiagnosticOutcome Outcome,
    DiagnosticSeverity Severity,
    string Code,
    string? Message,
    IReadOnlyDictionary<string, object?> Evidence);
```

These examples define the design shape, not final implementation signatures.

### 7.5 Diagnostic Outcome and Severity

The Diagnostic Fabric SHALL distinguish diagnostic outcome, diagnostic severity, and action risk.

Diagnostic outcome SHOULD support:

```text
Pass
Fail
Indeterminate
```

Diagnostic severity SHOULD support:

```text
Info
Warning
Error
Critical
```

Action risk is a separate multidimensional profile.

For example:

```text
ActionRiskProfile:
    Mutability = ReadOnly
    Boundary = Internal

Diagnostic:
    tenant filter absent

DiagnosticOutcome: Fail
DiagnosticSeverity: Critical
Disposition: Block
```

A low-risk action does not make a critical integrity failure acceptable.

### 7.6 Diagnostic Disposition

Diagnostic results SHALL be evaluated under Diagnostic Policy.

Diagnostic Policy is implemented through existing authority boundaries: Governance and the Execution Gate own dispositions that permit or block consequential execution; Orchestration owns non-authority lifecycle responses such as retry, re-observation, degradation, and escalation.

A finding MAY result in:

- continuing normally;
- continuing in degraded mode;
- bounded retry;
- re-observation;
- provider fallback;
- escalation;
- execution block;
- Goal failure.

The same diagnostic outcome MAY have different dispositions depending on severity and context, except for hard invariants, which SHALL fail closed on Fail or Indeterminate. Other dispositions may depend on:

- action risk;
- plane;
- execution phase;
- environment;
- tenant policy;
- whether the check protects a hard system invariant.

Hard invariants such as tenant isolation SHALL fail closed on both Fail and Indeterminate.

Performance degradation MAY permit continued execution.

### 7.7 Plane-Specific Diagnostic Catalog

Each plane SHALL expose meaningful diagnostics appropriate to its responsibilities.

| Plane | Representative diagnostics |
|---|---|
| Interaction | protocol version supported; authentication established; tenant resolved; correlation established; input schema valid; action binding exists; request limits valid |
| Reasoning | observation requirements satisfied; unresolved entity references; repeated-plan/loop detection; step explosion; reasoning budget; unresolvable capability requests |
| Semantic | ontology version valid; action IRI resolves; property/relation mappings resolve; aliases unambiguous; descriptor semantics valid; preconditions/effects reference known vocabulary |
| Decision | candidate set non-empty when selection is requested; all submitted candidates are permitted; no duplicates; provider eligible; returned selection belongs to submitted set; result structurally valid; latency/cost within provider budget |
| Execution | executor binding resolves; dependency ready; arguments conform; action version current; idempotency key present where required; duplicate execution detection; timeout/cancellation behavior; result schema valid |
| State | tenant scope present; schema/index version current; graph/vector/relational state accessible as required; embedding dimensions correct; partition/filter invariants satisfied; consistency expectations met |

This catalog is illustrative rather than exhaustive.

### 7.8 Lifecycle Diagnostic Hooks

The runtime SHALL support diagnostics at meaningful lifecycle transitions.

| Lifecycle stage | Representative checks |
|---|---|
| Admit | identity, tenant, protocol integrity, correlation |
| Observe | freshness, provenance, tenant scope, state version |
| Reason | loop detection, budget, unresolved references |
| Resolve | ontology integrity, descriptor consistency, binding validity |
| Constrain | constraint completeness, hard-invariant evaluation completeness |
| Decide | candidate integrity, provider eligibility, result validity |
| Gate | stale state, confirmation validity, executor readiness, descriptor version |
| Execute | timeout, retry, idempotency, duplicate execution, dependency health |
| Verify | expected versus observed effects, unexpected mutations, consistency |
| Complete | audit completeness, unresolved anomalies, terminal-state coherence |

Diagnostics SHOULD be selected contextually rather than executing every registered check for every Goal.

### 7.9 Action Diagnostic Profile

ActionDescriptors SHOULD be able to reference diagnostics appropriate to that action.

Conceptually:

```text
ActionDescriptor
├── ...
├── DiagnosticProfile
│   ├── PreExecutionChecks
│   ├── InExecutionChecks
│   └── PostExecutionChecks
└── VerificationProfile
```

ActionDescriptor diagnostic entries SHOULD reference registered diagnostic definitions rather than embed arbitrary executable delegates.

This preserves portability, versioning, auditability, and provider independence.

### 7.10 Diagnostics and Effect Verification

Diagnostics and Effect Verification SHALL remain distinct.

Example:

```text
Expected semantic effect:
reservation.status == Confirmed

Effect Verification:
Did reservation.status become Confirmed?

Diagnostics:
Was a duplicate event published?
Did any cross-tenant mutation occur?
Was the audit record written?
Did graph and relational projections remain consistent?
Did execution exceed an abnormal latency threshold?
```

Effect Verification establishes whether intended semantic outcomes occurred.

Diagnostics evaluates surrounding runtime integrity and behavior.

### 7.11 Diagnostics and Observability

Observability SHALL supply much of the evidence consumed by behavioral diagnostics.

Diagnostics MAY use:

- logs;
- traces;
- metrics;
- state queries;
- schema registries;
- descriptor registries;
- provider metadata;
- audit records.

Diagnostics SHALL NOT be reduced to log emission.

A purposeful invariant check is a diagnostic even when its evidence comes from telemetry.

### 7.12 Diagnostic Audit

Consequential Diagnostic Findings SHALL be auditable.

Audit SHOULD capture:

- check ID and version;
- phase and position;
- status;
- reason code;
- evidence reference;
- resulting disposition;
- action and descriptor version when applicable;
- tenant and correlation;
- duration;
- whether the finding affected control flow.

Diagnostic audit SHALL follow the same sensitive-data minimization rules as the rest of the runtime.

### DESIGN RULE DX-1

Every consequential execution phase SHALL support contextual Diagnostic Checks appropriate to its plane, action, risk, and current execution context.

### DESIGN RULE DX-2

Diagnostic Findings SHALL be structured, auditable, and distinguishable from authorization decisions.

### DESIGN RULE DX-3

Diagnostics SHALL NOT grant permission or override failed Governance or semantic constraints.

### DESIGN RULE DX-4

Hard system invariants, including tenant isolation and authoritative action identity, SHALL fail closed when a diagnostic establishes that the invariant cannot be satisfied.

### DESIGN RULE DX-5

Diagnostics SHOULD operate pre-flight, in-flight, and post-flight where those positions provide meaningful assurance.

### DESIGN RULE DX-6

Diagnostic Policy MAY vary by action risk, environment, execution phase, and tenant policy.

### DESIGN RULE DX-7

Diagnostic checks SHALL be independently testable and SHOULD have stable identifiers and versions.

### DESIGN RULE DX-8

A model or Decision provider SHALL NOT be the sole authority for declaring a hard diagnostic invariant satisfied.

### DESIGN RULE DX-9

ActionDescriptors MAY reference registered Diagnostic Checks but SHALL NOT accept caller-supplied diagnostics as authoritative replacements for server-defined checks.

### DESIGN RULE DX-10

Diagnostic execution SHALL respect runtime budgets and SHALL avoid creating unbounded recursive diagnostics.

### DESIGN RULE DX-11

Failure or timeout of a required hard-invariant diagnostic SHALL fail closed unless an explicit trusted policy defines a safe degraded behavior.

### DESIGN RULE DX-12

Diagnostics SHALL be observable themselves: duration, failures, skipped checks, and policy dispositions SHOULD be measurable.

## 9. Orchestration

Orchestration SHALL coordinate movement through the planes.

Orchestration SHALL NOT replace plane authority.

The orchestrator SHALL:

- maintain lifecycle state;
- propagate context;
- sequence stages;
- enforce budgets;
- route retries;
- route escalation;
- coordinate observation loops;
- record stage transitions.

The orchestrator SHALL NOT independently decide:

- semantic applicability;
- authorization;
- preferred candidate;
- expected domain effects.

### DESIGN RULE O-1

Orchestration SHALL be host-neutral.

It SHALL NOT depend on SSE, HTTP controllers, Blazor components, or MCP transport types.

### DESIGN RULE O-2

The current chat-session orchestrator SHALL NOT define the future cognitive orchestration contract.

Chat/session orchestration and Goal orchestration are separate concerns.

## 10. Runtime Authority Model

Authority SHALL narrow as work moves toward execution.

```text
Goal
 |
 v
ActionCandidate
 |
 | semantic + governance constraints
 v
PermittedAction
 |
 | caller selection or Decision
 v
SelectedAction
 |
 | final execution gate
 v
AuthorizedAction
 |
 | executor
 v
ExecutedAction
```

### DESIGN RULE A-1

An executor SHALL NOT accept raw model output as execution authority.

### DESIGN RULE A-2

An executor SHOULD accept a type or runtime token representing final authorization.

### DESIGN RULE A-3

Candidate membership SHALL NOT imply permission.

### DESIGN RULE A-4

Selection SHALL NOT imply authorization.

## 11. ActionDescriptor Design

Every executable capability SHALL have a server-authoritative ActionDescriptor.

An ActionDescriptor SHALL conceptually contain:

```text
ActionDescriptor
├── Id
├── Version
├── Name
├── Description
├── Semantic action type
├── Input contract
├── Output contract
├── Risk class
├── Required capabilities/permissions
├── Semantic preconditions
├── Governance metadata
├── Expected effects
├── Idempotency semantics
├── Reversibility/compensation metadata
├── Executor binding
├── Diagnostic profile
├── Verification profile
└── Observability metadata
```

### DESIGN RULE AD-1

Action identity SHALL be independent of MCP tool identity.

### DESIGN RULE AD-2

Risk classification SHALL come from trusted action metadata or policy, never caller input.

### DESIGN RULE AD-3

Authoritative preconditions and effects SHALL NOT be supplied by an untrusted caller.

### DESIGN RULE AD-4

ActionDescriptors SHALL be versioned.

Audit records SHALL identify the descriptor version used for a consequential execution.

### DESIGN RULE AD-5

The design SHALL permit ActionDescriptors to be sourced from generated ontology artifacts, code registration, configuration, or a hybrid registry without changing consumer contracts.

The authoritative source mechanism remains an implementation decision.

### DESIGN RULE AD-6

ActionDescriptor diagnostic references SHALL identify registered checks or profiles and SHALL be versionable independently from caller input.

## 12. Initial Risk Model

Action risk SHALL be modeled as orthogonal characteristics rather than a single mutually exclusive enum.

The initial ActionRiskProfile SHALL represent at least:

```text
Mutability:
    ReadOnly | Write

Idempotency:
    Idempotent | NonIdempotent

Reversibility:
    Reversible | Compensatable | Irreversible

Boundary:
    Internal | ExternalSideEffect

Privilege:
    Normal | Privileged
```

Governance MAY derive policy-specific risk levels from this profile.

Risk characteristics SHALL influence:

- autonomous execution policy;
- confidence requirements;
- provider eligibility;
- confirmation requirements;
- audit detail;
- retry policy.

Exact thresholds and derived policy levels SHALL remain configurable.

## 13. Action Registry and Binding

The runtime SHALL provide an authoritative registry of known actions.

The registry SHALL support lookup by semantic action identity.

Protocol adapters MAY additionally resolve bindings by protocol-specific identity.

Example:

```text
"history_get"
     |
     v
MCP ActionBinding
     |
     v
ldm:action/HistoryRead@1
     |
     v
ActionDescriptor
     |
     v
ExecutorBinding
```

### DESIGN RULE AR-1

Unknown action identities SHALL fail closed.

### DESIGN RULE AR-2

A protocol binding SHALL NOT alter authoritative risk, preconditions, effects, or permissions.

### DESIGN RULE AR-3

Multiple protocol bindings MAY reference the same semantic action.

## 14. Directed Invocation

Directed invocation occurs when the caller explicitly selects an action.

MCP tool calls are the primary current example.

The runtime path SHALL be:

```text
Interaction
    |
    v
Resolve Action Binding
    |
    v
Validate arguments
    |
    v
Evaluate semantic constraints
    |
    v
Evaluate governance
    |
    v
SelectedAction
    |
    v
Pre-flight Diagnostics
    |
    v
Final Execution Gate
    |
    v
AuthorizedAction
    |
    v
Execute + In-flight Diagnostics
    |
    v
Post-flight Diagnostics
    |
    v
Verify / Audit
```

### DESIGN RULE DI-1

Directed invocation SHALL NOT require a Decision provider merely to reaffirm an explicit action selection.

### DESIGN RULE DI-2

Directed invocation SHALL NOT bypass semantic constraints, governance, or the final gate.

### DESIGN RULE DI-3

Existing MCP clients SHOULD remain compatible as the gate is introduced.

## 15. Autonomous Invocation

Autonomous invocation occurs when the caller supplies a Goal rather than an action.

The runtime path SHALL be:

```text
Admit
  |
Normalize Goal
  |
Observe
  |
Reason
  |
Resolve Actions
  |
Constrain
  |
Decide
  |
Pre-flight Diagnostics
  |
Gate
  |
Execute + In-flight Diagnostics
  |
Post-flight Diagnostics
  |
Verify Effects
  |
Observe Result
  |
Continue or Complete
```

### DESIGN RULE AI-1

Reasoning SHALL NOT directly invoke executors.

### DESIGN RULE AI-2

The Semantic Plane SHALL reduce an autonomous intent to a finite action set before Decision.

### DESIGN RULE AI-3

Decision SHALL receive only PermittedActions.

### DESIGN RULE AI-4

If no PermittedAction exists, Decision SHALL NOT be invoked.

## 16. Goal Lifecycle State Machine

The runtime SHALL expose an explicit lifecycle.

Initial states:

```text
Admitted
Observing
Reasoning
Resolving
Constraining
Deciding
AwaitingConfirmation
Gating
Executing
Verifying
Completed
Abstained
Escalated
Failed
Cancelled
```

A runtime implementation MAY refine these states.

These states describe the full autonomous Goal lifecycle. Directed invocation SHALL use only the applicable execution states and SHALL NOT be forced through Reasoning or Deciding. A common RuntimeInvocationId SHOULD correlate both directed and autonomous paths; GoalId is required only when a Goal exists.

Diagnostic hooks SHALL attach to lifecycle states and transitions without requiring Diagnostics to become a separate lifecycle state.

### DESIGN RULE GL-1

State transitions SHALL be observable.

### DESIGN RULE GL-2

Terminal states SHALL include structured reason information.

### DESIGN RULE GL-3

Abstention SHALL be a normal terminal or transition outcome, not an exception.

## 17. Observation Design

Observations SHALL represent state evidence available to reasoning, constraints, and verification.

An Observation SHOULD identify:

- source;
- timestamp;
- tenant;
- entity or resource identity where applicable;
- version/ETag/revision where available;
- data or reference to data;
- provenance.

### DESIGN RULE OB-1

Observation acquisition SHALL be distinct from Reasoning.

### DESIGN RULE OB-2

Critical state used to authorize a write SHOULD carry a version or concurrency token where the underlying store supports it.

### DESIGN RULE OB-3

Sensitive observation payloads SHOULD be referenced rather than duplicated into audit records where practical.

### 17.1 Domain Adjudication and Explanation

A Goal MAY terminate successfully with a DomainConclusion when the requested outcome is understanding or adjudication rather than state mutation.

Domain adjudication MAY use semantic retrieval, graph traversal, authoritative rule resolution, exception precedence, current observations, and registered deterministic calculations. Each contributes evidence; none independently grants authority.

### DESIGN RULE DC-1

The runtime SHALL distinguish a DomainConclusion from every action-authority type.

### DESIGN RULE DC-2

A DomainConclusion SHALL preserve sufficient evidence and provenance to explain its semantic basis.

### DESIGN RULE DC-3

Missing, stale, ambiguous, or conflicting material evidence SHALL produce a qualified, indeterminate, re-observe, or abstention outcome rather than fabricated certainty.

### DESIGN RULE DC-4

A DomainConclusion SHALL NOT authorize a subsequent mutation. A requested mutation SHALL independently enter the applicable directed or autonomous action-authority path and revalidate material state.

### DESIGN RULE DC-5

User-facing explanation SHALL remain distinguishable from runtime audit evidence even when both reference the same rules, observations, calculations, and provenance.

## 18. Planning

A Plan SHALL represent proposed future work.

### DESIGN RULE P-1

A Plan SHALL NOT constitute authorization.

### DESIGN RULE P-2

Each consequential Plan step SHALL be resolved and gated when it becomes actionable.

### DESIGN RULE P-3

Plans MAY be revised after every new Observation.

### DESIGN RULE P-4

The planner SHALL express desired semantic outcomes rather than inventing unregistered executor names.

## 19. Action Resolution

Action resolution SHALL map a semantic need to finite ActionCandidates.

Resolution MAY use:

- ontology action definitions;
- entity type;
- current semantic state;
- aliases;
- action bindings;
- required input availability;
- plan intent.

### DESIGN RULE R-1

Action resolution SHALL fail closed for unknown semantic action vocabulary.

### DESIGN RULE R-2

Ontology-bound identifiers SHALL NOT silently fall through to physical storage identifiers.

### DESIGN RULE R-3

Resolution SHALL produce explicit evidence sufficient to understand why an action was considered applicable.

## 20. Constraint Pipeline

Before Decision, candidates SHALL pass deterministic constraints.

The conceptual pipeline is:

```text
ActionCandidates
      |
      +--> semantic applicability
      |
      +--> tenant boundary
      |
      +--> authorization
      |
      +--> policy
      |
      +--> hard preconditions
      |
      +--> risk admission
      |
      v
PermittedActions
```

Semantic and governance results SHALL remain distinguishable even if evaluated by a shared pipeline.

### DESIGN RULE C-1

A failed hard constraint SHALL remove the candidate.

### DESIGN RULE C-2

Decision providers SHALL NOT be able to restore a removed candidate.

### DESIGN RULE C-3

Constraint failures SHALL produce structured reason codes.

## 21. Decision Plane Design

The Decision Plane operates over a finite set of PermittedActions.

It SHALL support at least:

- selection;
- abstention;
- escalation.

A decision result SHOULD contain:

```text
DecisionResult
├── Outcome
├── Selected action, if any
├── Confidence
├── Distribution, if available
├── Provider
├── Provider/model version
├── Reason code
├── Latency
└── Cost/token metadata where applicable
```

### DESIGN RULE D-1

Decision providers SHALL be replaceable.

### DESIGN RULE D-2

Decision contracts SHALL NOT depend on vendor SDK types.

### DESIGN RULE D-3

Decision providers SHALL NOT receive candidates that failed hard constraints.

### DESIGN RULE D-4

Decision confidence SHALL be treated as evidence consumed by policy.

### DESIGN RULE D-5

Jev, LLMs, rules, classifiers, and composite strategies SHALL be implementations behind the same architectural boundary.

## 22. Decision Provider Routing

Provider routing MAY use:

- action risk;
- candidate count;
- latency budget;
- cost budget;
- task class;
- historical provider performance;
- required confidence.

A possible routing chain is:

```text
Rule provider
     |
 unresolved
     v
Specialized decision provider
     |
 insufficient confidence
     v
LLM decision provider
     |
 policy requires confirmation
     v
Human
```

### DESIGN RULE DPR-1

Providers SHALL report results. They SHALL NOT decide whether their own result satisfies system risk policy.

### DESIGN RULE DPR-2

Escalation policy SHALL be external to individual providers.

## 23. Final Execution Gate

The Execution Gate is the final deterministic authority before consequential execution.

It SHALL revalidate execution-sensitive constraints.

The gate SHALL be capable of evaluating:

- tenant;
- principal;
- action version;
- authorization;
- hard preconditions;
- current state/version;
- risk;
- confidence policy;
- confirmation;
- budget;
- execution environment.

### DESIGN RULE EG-1

No consequential executor SHALL run without a successful gate result.

### DESIGN RULE EG-2

The gate SHALL fail closed.

### DESIGN RULE EG-3

The gate SHALL revalidate critical mutable state rather than relying solely on earlier candidate evaluation.

### DESIGN RULE EG-4

A stale decision SHALL normally trigger re-observation rather than blind execution or blind retry.

## 24. Concurrency and TOCTOU

LimboDancer SHALL assume state can change between observation and execution.

Where possible:

```text
observe S1
   |
resolve / constrain / decide
   |
gate
   |
   +--> S1 still valid -> execute
   |
   +--> state changed -> stale
                         |
                         v
                     re-observe
```

### DESIGN RULE CC-1

Critical write preconditions SHALL be checked at or immediately before execution.

### DESIGN RULE CC-2

Stores supporting optimistic concurrency SHOULD expose version information through observations.

### DESIGN RULE CC-3

A stale SelectedAction SHALL NOT automatically inherit authorization after re-observation.

## 25. Executors

Executors SHALL perform operational work.

An executor SHOULD:

- accept an AuthorizedAction or equivalent authorization token;
- validate executor-specific input shape;
- perform one bounded capability;
- return structured execution results;
- expose idempotency information where applicable;
- avoid embedding policy selection logic.

### DESIGN RULE EX-1

Executors SHALL NOT accept caller-defined risk or authorization policy.

### DESIGN RULE EX-2

Executors SHALL NOT invoke a Decision provider to determine whether they should execute.

### DESIGN RULE EX-3

Executor implementations MAY use MCP tools, application services, workflows, message buses, or external APIs.

## 26. Preconditions

Preconditions SHALL be classified by authority.

### Semantic preconditions

Determine whether an action is meaningful in domain state.

### Governance preconditions

Determine whether execution is permitted.

### Operational preconditions

Determine whether execution is safe against current operational state.

### DESIGN RULE PC-1

Precondition origin SHALL remain identifiable in audit.

### DESIGN RULE PC-2

Caller-supplied preconditions MAY be treated as requests or additional restrictions, but SHALL NOT weaken authoritative preconditions.

### DESIGN RULE PC-3

The current pattern in which `HistoryAppendTool` accepts authoritative preconditions from input SHALL be retired through migration.

## 27. Effects

Expected effects SHALL be associated with authoritative action definitions.

Effects MAY describe:

- graph property changes;
- relation changes;
- workflow transitions;
- persisted records;
- externally observable outcomes.

### DESIGN RULE EF-1

Caller-supplied effects SHALL NOT define authoritative postconditions.

### DESIGN RULE EF-2

Execution success SHALL NOT automatically imply semantic effect success.

### DESIGN RULE EF-3

Effects SHOULD be verified against observed post-execution state where technically feasible and proportionate to risk.

## 28. Effect Verification

Verification results SHALL support:

```text
Verified
PartiallyVerified
Unverifiable
Contradicted
```

A contradicted high-risk effect SHOULD trigger escalation, recovery, or compensation policy.

Verification MAY be asynchronous where immediate observation is impossible.

## 29. Reversibility and Compensation

Action metadata SHOULD describe reversibility.

An action MAY be:

- read-only;
- idempotent;
- reversible;
- compensatable;
- irreversible.

A compensation action SHALL itself be a governed semantic action.

### DESIGN RULE RC-1

Authorization of an action SHALL NOT automatically authorize its compensation action.

### DESIGN RULE RC-2

Compensation SHALL pass current semantic, governance, and execution constraints.

## 30. Human Confirmation

Human confirmation is a Governance mechanism.

It SHALL NOT be modeled as a Decision provider.

```text
SelectedAction
      |
      v
Gate: confirmation required
      |
      v
AwaitingConfirmation
      |
      +--> approve
      |       |
      |       v
      |   revalidate
      |       |
      |       v
      |    execute
      |
      +--> deny
      |
      +--> expire -> re-observe
```

### DESIGN RULE HC-1

Approval SHALL be bound to action identity, arguments, tenant, and relevant version/context.

### DESIGN RULE HC-2

The runtime SHALL revalidate mutable constraints after approval.

## 31. Multi-Step Goals

Each consequential step of a multi-step Goal SHALL pass through the runtime authority lifecycle.

```text
Plan Step 1
  -> resolve -> constrain -> select -> gate -> execute -> observe

Plan Step 2
  -> resolve again against new state

Plan Step 3
  -> resolve again against new state
```

### DESIGN RULE MS-1

A Plan SHALL NOT grant blanket authorization to future steps.

### DESIGN RULE MS-2

New observations MAY invalidate remaining Plan steps.

## 32. Budgets

Autonomous execution SHALL be bounded.

The orchestration context SHOULD support:

- maximum steps;
- deadline;
- token budget;
- monetary budget;
- external-call budget;
- retry budget;
- provider fallback budget.

### DESIGN RULE B-1

Budget exhaustion SHALL terminate or escalate execution.

### DESIGN RULE B-2

Budget enforcement SHALL be independent of model cooperation.

## 33. Error Taxonomy

Runtime failures SHALL be stage-aware.

The design SHALL distinguish at least:

```text
InteractionError
ObservationError
ReasoningError
ResolutionError
SemanticConstraintFailure
GovernanceDenial
DecisionAbstention
DecisionProviderError
DiagnosticFailure
DiagnosticIndeterminate
StaleStateError
ExecutionDenied
ExecutionError
EffectVerificationError
BudgetExceeded
Cancelled
```

### DESIGN RULE ER-1

Abstention and governance denial SHALL NOT be represented as generic executor exceptions.

### DESIGN RULE ER-2

Errors SHALL carry stable reason codes suitable for telemetry and audit.

## 34. Retry Design

Retry behavior SHALL depend on stage and action semantics.

Examples:

| Condition | Default response |
|---|---|
| transient state read failure | bounded retry |
| governance denial | no unchanged retry |
| semantic precondition failure | re-observe or re-reason |
| decision provider timeout | provider retry/fallback by policy |
| required diagnostic transient failure | bounded diagnostic retry or fail closed by policy |
| diagnostic integrity failure | apply diagnostic disposition; do not blind retry |
| stale state | re-observe |
| idempotent executor transient failure | bounded retry |
| non-idempotent executor timeout | reconcile execution state before retry |
| effect contradiction | escalate/recover |

### DESIGN RULE RT-1

There SHALL NOT be a universal retry policy for the entire cognitive loop.

## 35. Audit Design

Every consequential runtime step SHALL emit structured audit information.

The audit model SHOULD include:

- Goal ID;
- Step ID;
- Correlation ID;
- Tenant ID;
- principal;
- origin;
- lifecycle state transitions;
- observation references and versions;
- ActionDescriptor IDs and versions;
- candidate set;
- semantic constraint results;
- governance results;
- diagnostic findings and dispositions;
- decision provider/version;
- selected action;
- confidence/distribution where available;
- risk;
- confirmation;
- gate result;
- execution result;
- expected effects;
- observed effects;
- verification result;
- latency;
- cost/token metadata;
- escalation/abstention reason.

### DESIGN RULE AU-1

Audit SHALL capture the decision boundary without requiring hidden model chain-of-thought.

### DESIGN RULE AU-2

Sensitive data SHOULD be referenced, hashed, redacted, or minimized where full persistence is unnecessary.

## 36. Replay Design

Replay SHALL allow historical decision contexts to be evaluated without replaying consequential side effects.

Replay SHOULD support:

- reconstructing candidate sets;
- comparing provider decisions;
- evaluating calibration;
- evaluating policy changes;
- measuring provider disagreement;
- regression testing.

### DESIGN RULE RP-1

Replay SHALL be able to stop before execution.

### DESIGN RULE RP-2

Provider benchmarking SHOULD use replayable historical contexts plus curated labeled cases.

## 37. Observability

Runtime telemetry SHOULD expose:

- Goal lifecycle duration;
- stage latency;
- candidate counts;
- filtered candidate counts;
- governance denials;
- Decision latency;
- provider fallback;
- abstention;
- confidence;
- gate denials;
- stale-state frequency;
- execution success;
- verification outcome;
- retries;
- token use;
- cost;
- task completion.

Observability SHALL distinguish system safety from model quality.

Observability provides signals and evidence. Diagnostics performs explicit checks against runtime expectations and invariants. Governance and Orchestration determine the operational response to those findings.

A zero policy-violation rate is an architectural property, not a model benchmark.

## 38. Host Architecture

Interaction hosts SHALL converge on a host-neutral LimboDancer runtime contract.

Conceptually:

```text
MCP Host -----------+
                    |
HTTP / Chat Host ---+
                    |
CLI ----------------+----> LimboDancer Runtime
                    |
Blazor -------------+
                    |
Future adapters ----+
```

The hosts MAY remain independently deployable.

### DESIGN RULE H-1

One web host SHALL NOT be the long-term application-layer dependency of another web host.

### DESIGN RULE H-2

Shared runtime composition SHOULD move behind host-neutral service registration and contracts.

### DESIGN RULE H-3

Readiness checks SHOULD be observational. Schema migration SHOULD be a deployment/startup concern rather than normal readiness behavior.

## 39. Dependency Rules

### DESIGN RULE DEP-1

Core runtime contracts SHALL NOT depend on:

- MCP SDKs;
- ASP.NET controllers;
- Azure Search SDK;
- Gremlin SDK;
- EF Core;
- model-vendor SDKs.

### DESIGN RULE DEP-2

Infrastructure and provider implementations SHALL depend inward on contracts.

### DESIGN RULE DEP-3

Protocol adapters SHALL depend on application/runtime contracts, not vice versa.

### DESIGN RULE DEP-4

Semantic/application services SHALL NOT depend on contracts declared inside protocol-facing tool classes.

### DESIGN RULE DEP-5

Plane boundaries SHALL guide dependency direction but SHALL NOT require one project per plane.

### 39.1 Domain Package Integration Boundary

Concrete domains SHALL integrate through domain-neutral LimboDancer contracts and Host composition. The complete design guidance is defined in `LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`.

### DESIGN RULE DOM-1

Runtime projects SHALL NOT reference ASL or another concrete domain package.

### DESIGN RULE DOM-2

A domain package MAY depend on approved LimboDancer abstractions and SHALL NOT redefine runtime authority transitions.

### DESIGN RULE DOM-3

The Host SHALL compose the runtime with selected domain packages. Protocol adapters SHALL NOT serve as the domain-integration boundary.

### DESIGN RULE DOM-4

Domain packages SHOULD reuse existing Observation, semantic-action, constraint, Diagnostic, executor, effect-verification, and audit contracts before proposing new extension interfaces.

### DESIGN RULE DOM-5

Domain-specific infrastructure and provider SDK types SHALL remain behind inward-facing ports.

### DESIGN RULE DOM-6

Domain and package identity, version, tenant, evidence provenance, and state-version information SHALL remain explicit where required for semantic resolution or DomainConclusion integrity.

### DESIGN RULE DOM-7

A generalized plugin registry, dynamic domain discovery, and domain activation lifecycle SHALL NOT be introduced until multiple concrete domains demonstrate requirements that Host composition cannot satisfy cleanly.

### DESIGN RULE DOM-8

Concrete domain-integration interfaces SHALL be admitted only from a named scenario after the authority primitive on which they depend has been implemented and tested.

## 40. Initial Mapping of Existing Tools

The four current tools SHALL receive initial semantic action identities.

Proposed working identities:

| MCP Tool | Semantic Action |
|---|---|
| `history_get` | `ldm:action/HistoryRead` |
| `history_append` | `ldm:action/HistoryAppend` |
| `graph_query` | `ldm:action/GraphQuery` |
| `memory_search` | `ldm:action/MemorySearch` |

These identifiers are design placeholders until aligned with the canonical ontology namespace.

The bindings SHALL allow existing MCP names to remain stable.

## 41. Migration of HistoryAppend

The legacy `HistoryAppendTool` is an important reimplementation reference because it accepted:

- `preconditions`;
- `effects`.

Target design:

```text
Caller
  |
  | arguments only
  v
HistoryAppend ActionDescriptor
  |
  +--> authoritative preconditions
  +--> authoritative effects
  +--> risk
  +--> permissions
  +--> executor binding
  |
  v
Gate
  |
  v
HistoryAppend executor
```

The new implementation MAY preserve protocol compatibility where necessary, but it SHALL NOT preserve caller-provided semantic authority.

## 42. Tenant Design

Tenant isolation SHALL be structural.

### DESIGN RULE T-1

All State reads and writes SHALL be tenant-scoped.

### DESIGN RULE T-2

Tenant scope SHALL be established before semantic resolution and SHALL remain immutable for the Goal.

### DESIGN RULE T-3

Action resolution SHALL NOT expose cross-tenant candidates or state.

### DESIGN RULE T-4

Audit SHALL record tenant identity.

### DESIGN RULE T-5

The new history-read and graph-read implementations SHALL enforce tenant isolation explicitly or through a verified global mechanism before autonomous execution is enabled.

## 43. Semantic Fail-Closed Behavior

Ontology-constrained runtime paths SHALL fail closed.

### DESIGN RULE SF-1

Unknown ontology predicates SHALL NOT silently become physical graph property keys.

### DESIGN RULE SF-2

Unknown semantic actions SHALL NOT fall through to executor names.

### DESIGN RULE SF-3

Unknown effect mappings SHALL produce explicit verification/execution failure according to action policy.

This rule specifically prevents reproduction of the legacy mixed behavior in graph precondition/effect mapping.

## 44. Security Boundary

Probabilistic systems SHALL be treated as untrusted advisors with respect to execution authority.

This includes:

- LLM reasoning providers;
- LLM Decision providers;
- Jev or other decision models;
- classifiers;
- generated plans.

They MAY propose or select within permitted boundaries.

They SHALL NOT define those boundaries.

## 45. Initial Runtime Component Model

The target component relationships are:

```text
+-----------------------+
| Interaction Adapters  |
+-----------+-----------+
            |
            v
+-----------------------+
|   Goal Orchestrator   |
+-----------+-----------+
            |
    +-------+--------+----------------+
    |                |                |
    v                v                v
Reasoning       Action Resolver   Observation
Provider             |             Services
    |                v                |
    |        Constraint Pipeline      |
    |                |                |
    |                v                |
    |        Decision Provider        |
    |                |                |
    +----------------+----------------+
                     |
                     v
              Execution Gate
                     |
                     v
               Action Executor
                     |
                     v
              Effect Verifier
                     |
                     v
                  State

Diagnostic Runner participates across
admission, observation, reasoning, resolution,
decision, gate, execution, verification, and completion.
```

Governance services participate in the constraint pipeline, gate, confirmation, audit, and host admission.

Diagnostic services participate across all planes and lifecycle phases but do not replace the authority of those components.

## 46. Initial Implementation Boundary

The first implementation SHALL prove the common execution authority boundary with minimal machinery before autonomous Decision is introduced.

The initial solution SHOULD begin with five projects:

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Infrastructure
LimboDancer.Adapters.Mcp
LimboDancer.Host
```

Plane and fabric separation SHOULD initially be expressed primarily through namespaces. Additional assemblies SHOULD be created only when a concrete dependency, deployment, packaging, ownership, or isolation need appears.

### Increment 1: Authority foundation

Define:

- runtime/action identities;
- `ActionRiskProfile`;
- `ActionDescriptor`;
- action registry;
- protocol action bindings;
- execution context;
- `SelectedAction`;
- `AuthorizedAction`;
- `IExecutionGate`.

### Increment 2: Minimal diagnostics and audit

Define:

- diagnostic outcome and severity;
- `DiagnosticContext`;
- diagnostic check/result contracts;
- a small `IDiagnosticRunner`;
- hard-invariant checks;
- structured audit boundary.

A public plugin-style diagnostic registry is not required for the initial implementation.

Initial hard-invariant checks SHALL cover:

- tenant context;
- action registration;
- descriptor version;
- executor binding;
- required semantic mapping validity.

### Increment 3: Directed execution convergence

Implement a new MCP adapter and new executors under the new namespace family.

Reimplement the four current MCP capabilities without production references to `LimboDancer.MCP.*`.

Prove:

```text
MCP binding
-> ActionDescriptor
-> constraints
-> diagnostics
-> Execution Gate
-> AuthorizedAction
-> executor
-> audit
```

### Increment 4: Semantic and State reimplementation

Reimplement:

- authoritative preconditions/effects;
- fail-closed semantic mappings;
- tenant-safe relational, graph, and vector access;
- ontology-backed semantic resolution required by the initial actions.

Legacy code is behavioral reference only.

### Increment 5: Autonomous contracts

Add:

- Goal;
- Observation;
- ActionCandidate;
- PermittedAction;
- Decision result;
- `IActionResolver`;
- `IDecisionProvider`;
- Goal orchestration.

An empty PermittedAction set SHALL terminate or redirect orchestration without invoking the Decision provider.

### Increment 6: Baseline autonomous loop

Implement a deterministic or structured baseline provider.

Do not introduce provider routing until at least two real providers exist.

### Increment 7: Verification and replay-capable evidence

Add effect verification only where it is concrete and meaningful.

Capture sufficient structured audit evidence to support future replay, but do not require a replay engine yet.

### Increment 8: Advanced capabilities when justified

Add Jev/LLM Decision providers, provider routing, human confirmation, compensation, distributed authorization, or a replay engine only when concrete use cases require them.

## 47. Testing Requirements

The runtime design SHALL be tested at authority boundaries.

Initial required test classes include:

### Action registry

- known action resolves;
- unknown action fails closed;
- protocol binding cannot override descriptor authority.

### Tenant

- missing tenant denied;
- cross-tenant State read denied;
- cross-tenant write denied;
- candidate resolution is tenant-scoped.

### Semantic constraints

- valid predicate maps;
- unknown predicate fails closed;
- failed hard precondition removes candidate.

### Decision

- provider sees only permitted candidates;
- provider cannot select removed candidate;
- abstention produces no execution.

### Diagnostics

- required hard-invariant checks execute;
- contextual checks receive only the expected context;
- unknown diagnostic ID fails according to profile policy;
- critical tenant diagnostic blocks;
- warning diagnostic can continue under policy;
- indeterminate required diagnostic fails closed;
- diagnostic finding and disposition are audited;
- Decision output outside candidate set is diagnosed and rejected;
- diagnostic checks cannot grant authorization;
- diagnostic timeout obeys budget and policy.

### Execution gate

- valid directed invocation allowed;
- failed authorization denied;
- stale state denied;
- required confirmation blocks execution;
- risk policy enforced.

### Executor

- cannot execute without authorized context;
- idempotency/retry behavior honored.

### Audit

- denied action audited;
- executed action audited;
- descriptor version recorded;
- no hidden reasoning required.

## 48. Non-Goals of the First Design Increment

The first implementation SHALL NOT attempt to solve all agent behavior.

Specifically, the first increment does not require:

- a sophisticated planner;
- multi-agent coordination;
- autonomous destructive writes;
- distributed transactions across all State stores;
- universal compensation;
- dynamic ontology-generated executors;
- a Jev dependency;
- a specific LLM vendor;
- one project per plane;
- replacement of all current MCP tools.

The design deliberately establishes authority and contracts before expanding cognition.

## 49. Design Decisions Held Open

The following remain intentionally open until implementation analysis provides stronger evidence:

1. The exact project partitioning within the new `LimboDancer.*` solution.
2. The final new-project boundary for reasoning/model-provider integrations; the legacy `LimboDancer.MCP.Llm` project will not be retained as a runtime dependency.
3. The authoritative physical source for ActionDescriptors.
4. The final ontology namespace for action identifiers.
5. The persistence schema for decision audit.
6. The exact policy engine implementation.
7. Confidence calibration across heterogeneous providers.
8. Human-confirmation UI and transport.
9. Distributed locking versus optimistic concurrency for particular actions.
10. How complex multi-step transactional actions are represented.
11. Whether effect verification is synchronous or asynchronous per action.
12. Exact provider-routing policy.
13. The persistence and retention model for Diagnostic Findings.
14. Whether diagnostic profiles are primarily ActionDescriptor metadata, registry configuration, ontology artifacts, or a hybrid.

These are implementation or subsequent design decisions. They do not alter the core authority model.

## 50. Acceptance Criteria for the Runtime Design

The design is successfully realized when all of the following are true:

1. Every executable capability has a trusted ActionDescriptor.
2. Semantic action identity is distinct from transport/tool identity.
3. Existing directed MCP invocation passes through the common execution authority boundary.
4. Caller input cannot weaken authoritative preconditions, effects, risk, or permissions.
5. Tenant scope is enforced on all relevant State reads and writes.
6. Autonomous reasoning cannot directly invoke executors.
7. Autonomous action resolution produces a finite candidate set.
8. Decision providers receive only PermittedActions.
9. Decision may abstain without causing execution.
10. Selected actions are revalidated by the final gate.
11. Executors receive only authorized work.
12. Critical state changes invalidate stale decisions.
13. Expected effects can be compared with observed outcomes.
14. Consequential decisions and executions are auditable.
15. Historical decision contexts can be replayed without side effects.
16. Decision providers can be replaced without modifying executors.
17. Interaction hosts share the same runtime authority model.
18. No model provider is an authorization authority.
19. Consequential lifecycle phases support contextual Diagnostic Checks.
20. Hard-invariant diagnostics fail closed.
21. Diagnostic findings are distinct from Governance decisions and are auditable.
22. ActionDescriptors can bind versioned diagnostic profiles without accepting caller-defined authoritative checks.
23. A domain question can terminate with an evidence-backed DomainConclusion without inventing a state-changing action.
24. Domain conclusions remain distinct from action selection, authorization, and execution.
25. Material rule, ontology, reference-data, or observed-state changes can qualify, invalidate, or trigger recomputation of dependent conclusions.
26. Incomplete or conflicting evidence can produce an explicit indeterminate or abstention outcome.
27. The ASL reference-domain scenarios can be realized without embedding ASL-specific concepts in the runtime kernel.

## 51. Canonical Runtime Sequence

The canonical autonomous sequence is:

```text
Actor
  |
  v
Interaction
  |
  v
Admit + establish Governance context
  |
  v
Goal
  |
  v
Observe
  |
  v
Reason
  |
  v
Resolve ActionCandidates
  |
  v
Semantic + Governance constraints
  |
  v
PermittedActions
  |
  v
Decision
  |
  +--> Abstain / Escalate
  |
  v
SelectedAction
  |
  v
Pre-flight Diagnostics
  |
  v
Final Execution Gate
  |
  +--> Deny / Stale / Confirm
  |
  v
AuthorizedAction
  |
  v
Executor + In-flight Diagnostics
  |
  v
ExecutedAction
  |
  v
Post-flight Diagnostics
  |
  v
Observe effects
  |
  v
Verify
  |
  v
Audit
  |
  v
Reason: complete or continue
```

The canonical directed sequence is:

```text
Actor
  |
  v
Interaction
  |
  v
Explicit action binding
  |
  v
ActionDescriptor
  |
  v
Semantic + Governance constraints
  |
  v
SelectedAction
  |
  v
Pre-flight Diagnostics
  |
  v
Final Execution Gate
  |
  v
AuthorizedAction
  |
  v
Executor + In-flight Diagnostics
  |
  v
Post-flight Diagnostics
  |
  v
Verify + Audit
```

Both paths converge on the same execution authority model.

## 52. Final Design Statement

LimboDancer SHALL NOT be designed as an LLM that happens to call MCP tools.

It SHALL be designed as a governed runtime in which goals cross explicit semantic and authority boundaries before the world can be changed.

The essential design is:

> **A Goal enters through Interaction. Orchestration carries it through the runtime. Reasoning proposes how to advance it. Semantics resolves those proposals into known capabilities. Governance removes what is not permitted. Decision selects among what remains. Diagnostics applies contextual checks to the integrity and fitness of the current execution context. The Execution Gate revalidates authority against current state. Execution acts. State records the result. Verification compares reality with expected effects. New observations feed the next cognitive cycle.**

This design makes agency explicit, bounded, diagnosable, testable, auditable, and independent of any particular intelligence provider.
