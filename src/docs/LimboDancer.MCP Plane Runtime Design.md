# LimboDancer.MCP Plane Runtime Design

**Status:** Design specification  
**Branch:** `decision-plane`  
**Scope:** Target runtime design for the LimboDancer plane architecture  
**Supporting analysis:** `LimboDancer.MCP Plane Architecture Analysis.md`, `LimboDancer.MCP Plane Architecture Codebase Validation.md`, `LimboDancer.MCP Runtime Orchestration Model.md`, `Decision Plane Architecture.md`

## 1. Purpose

This document specifies the target runtime design for LimboDancer.MCP.

The preceding analysis documents established the architectural rationale. This document converts that analysis into normative design.

It defines:

- the canonical runtime vocabulary;
- the six architectural planes;
- Governance and Orchestration;
- the runtime authority model;
- Goal lifecycle;
- semantic action model;
- directed and autonomous invocation;
- action resolution;
- deterministic constraints;
- Decision Plane boundaries;
- execution authorization;
- preconditions and effects;
- observation and verification;
- audit and replay;
- concurrency and stale-state handling;
- failure, retry, escalation, and confirmation behavior;
- host/runtime boundaries;
- dependency rules;
- migration from the current implementation.

This document intentionally stops short of prescribing every C# type or project layout. Those are implementation decisions derived from this design.

## 2. Design Objective

LimboDancer SHALL operate as an ontology-constrained cognitive runtime.

The runtime SHALL transform goals into governed actions through explicit authority transitions.

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

A **PermittedAction** is a candidate that has passed the deterministic semantic and governance constraints required before selection.

### 4.9 Decision

A **Decision** is the Decision Plane result over a finite set of PermittedActions.

A Decision MAY:

- select;
- abstain;
- escalate.

### 4.10 SelectedAction

A **SelectedAction** is a PermittedAction chosen by the caller in directed invocation or by the Decision Plane in autonomous invocation.

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

## 5. Architectural Planes

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

It SHALL contain:

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

## 6. Governance and Control Fabric

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
- telemetry;
- compliance constraints.

```text
+------------------------------------------------------+
|             GOVERNANCE / CONTROL FABRIC             |
| identity | tenant | authorization | policy | risk   |
| audit | telemetry | confirmation | quotas | secrets |
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

## 7. Orchestration

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

## 8. Runtime Authority Model

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

## 9. ActionDescriptor Design

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

## 10. Initial Risk Model

The initial runtime risk taxonomy SHALL support at least:

```text
ReadOnly
IdempotentWrite
ReversibleWrite
DestructiveWrite
ExternalSideEffect
PrivilegedAction
```

Risk SHALL influence:

- autonomous execution policy;
- confidence requirements;
- provider eligibility;
- confirmation requirements;
- audit detail;
- retry policy.

Exact thresholds SHALL be configurable policy.

## 11. Action Registry and Binding

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

## 12. Directed Invocation

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
Final Execution Gate
    |
    v
AuthorizedAction
    |
    v
Execute
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

## 13. Autonomous Invocation

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
Gate
  |
Execute
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

## 14. Goal Lifecycle State Machine

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

### DESIGN RULE GL-1

State transitions SHALL be observable.

### DESIGN RULE GL-2

Terminal states SHALL include structured reason information.

### DESIGN RULE GL-3

Abstention SHALL be a normal terminal or transition outcome, not an exception.

## 15. Observation Design

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

## 16. Planning

A Plan SHALL represent proposed future work.

### DESIGN RULE P-1

A Plan SHALL NOT constitute authorization.

### DESIGN RULE P-2

Each consequential Plan step SHALL be resolved and gated when it becomes actionable.

### DESIGN RULE P-3

Plans MAY be revised after every new Observation.

### DESIGN RULE P-4

The planner SHALL express desired semantic outcomes rather than inventing unregistered executor names.

## 17. Action Resolution

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

## 18. Constraint Pipeline

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

## 19. Decision Plane Design

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

## 20. Decision Provider Routing

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

## 21. Final Execution Gate

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

## 22. Concurrency and TOCTOU

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

## 23. Executors

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

## 24. Preconditions

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

## 25. Effects

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

## 26. Effect Verification

Verification results SHALL support:

```text
Verified
PartiallyVerified
Unverifiable
Contradicted
```

A contradicted high-risk effect SHOULD trigger escalation, recovery, or compensation policy.

Verification MAY be asynchronous where immediate observation is impossible.

## 27. Reversibility and Compensation

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

## 28. Human Confirmation

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

## 29. Multi-Step Goals

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

## 30. Budgets

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

## 31. Error Taxonomy

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

## 32. Retry Design

Retry behavior SHALL depend on stage and action semantics.

Examples:

| Condition | Default response |
|---|---|
| transient state read failure | bounded retry |
| governance denial | no unchanged retry |
| semantic precondition failure | re-observe or re-reason |
| decision provider timeout | provider retry/fallback by policy |
| stale state | re-observe |
| idempotent executor transient failure | bounded retry |
| non-idempotent executor timeout | reconcile execution state before retry |
| effect contradiction | escalate/recover |

### DESIGN RULE RT-1

There SHALL NOT be a universal retry policy for the entire cognitive loop.

## 33. Audit Design

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

## 34. Replay Design

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

## 35. Observability

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

A zero policy-violation rate is an architectural property, not a model benchmark.

## 36. Host Architecture

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

## 37. Dependency Rules

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

## 38. Initial Mapping of Existing Tools

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

## 39. Migration of HistoryAppend

`HistoryAppendTool` is the first important migration case because it currently accepts:

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

Migration SHOULD preserve protocol compatibility where necessary, but caller-provided semantic authority SHALL be deprecated and eventually removed.

## 40. Tenant Design

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

The existing history-read and graph-read paths SHALL be verified for explicit or global-filter tenant enforcement before autonomous execution is enabled.

## 41. Semantic Fail-Closed Behavior

Ontology-constrained runtime paths SHALL fail closed.

### DESIGN RULE SF-1

Unknown ontology predicates SHALL NOT silently become physical graph property keys.

### DESIGN RULE SF-2

Unknown semantic actions SHALL NOT fall through to executor names.

### DESIGN RULE SF-3

Unknown effect mappings SHALL produce explicit verification/execution failure according to action policy.

This rule specifically addresses the current mixed behavior in graph precondition/effect mapping.

## 42. Security Boundary

Probabilistic systems SHALL be treated as untrusted advisors with respect to execution authority.

This includes:

- LLM reasoning providers;
- LLM Decision providers;
- Jev or other decision models;
- classifiers;
- generated plans.

They MAY propose or select within permitted boundaries.

They SHALL NOT define those boundaries.

## 43. Initial Runtime Component Model

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
```

Governance services participate in the constraint pipeline, gate, confirmation, audit, and host admission.

## 44. Initial Implementation Boundary

The first implementation increment SHALL establish a trustworthy execution convergence point before autonomous Decision is introduced.

### Increment 1: Action authority

Define:

- `ActionRisk`;
- `ActionDescriptor`;
- action identity/version;
- action registry;
- protocol action bindings.

Bind the four existing MCP tools.

### Increment 2: Execution authorization

Define:

- execution context;
- gate result;
- `IExecutionGate`;
- deterministic baseline gate;
- structured denial reasons.

Route directed MCP invocation through the gate.

### Increment 3: Audit

Record:

- action;
- descriptor version;
- tenant;
- principal;
- gate result;
- execution outcome.

### Increment 4: Semantic authority migration

Move authoritative preconditions/effects out of caller control.

Correct fail-open semantic mappings.

Verify tenant enforcement on State reads.

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

### Increment 6: Baseline autonomous loop

Implement a deterministic or structured baseline provider.

Do not introduce specialized provider routing until the lifecycle is observable and replayable.

### Increment 7: Provider evaluation

Add Jev and other providers behind the established contract and evaluate using replay.

## 45. Testing Requirements

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

## 46. Non-Goals of the First Design Increment

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
- project reorganization matching every plane;
- replacement of all current MCP tools.

The design deliberately establishes authority and contracts before expanding cognition.

## 47. Design Decisions Held Open

The following remain intentionally open until implementation analysis provides stronger evidence:

1. Whether a new `LimboDancer.MCP.Runtime` or `Application` project should be created.
2. Whether `LimboDancer.MCP.Llm` should be renamed or replaced.
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

These are implementation or subsequent design decisions. They do not alter the core authority model.

## 48. Acceptance Criteria for the Runtime Design

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

## 49. Canonical Runtime Sequence

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
Final Execution Gate
  |
  +--> Deny / Stale / Confirm
  |
  v
AuthorizedAction
  |
  v
Executor
  |
  v
ExecutedAction
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
Final Execution Gate
  |
  v
AuthorizedAction
  |
  v
Executor
  |
  v
Verify + Audit
```

Both paths converge on the same execution authority model.

## 50. Final Design Statement

LimboDancer SHALL NOT be designed as an LLM that happens to call MCP tools.

It SHALL be designed as a governed runtime in which goals cross explicit semantic and authority boundaries before the world can be changed.

The essential design is:

> **A Goal enters through Interaction. Orchestration carries it through the runtime. Reasoning proposes how to advance it. Semantics resolves those proposals into known capabilities. Governance removes what is not permitted. Decision selects among what remains. The Execution Gate revalidates authority against current state. Execution acts. State records the result. Verification compares reality with expected effects. New observations feed the next cognitive cycle.**

This design makes agency explicit, bounded, testable, auditable, and independent of any particular intelligence provider.
