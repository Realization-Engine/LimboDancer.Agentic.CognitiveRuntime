# LimboDancer.Agentic.CognitiveRuntime Implementation Plan

**Status:** Implementation planning baseline  
**Branch:** `decision-plane`  
**Product / repository identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**.NET root namespace / project prefix:** `LimboDancer`  
**Target framework:** `net10.0` / C# 14  
**Legacy source root:** `src/Legacy/` (temporary; delete after required legacy behavior is ported)  
**Normative source:** `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md`  
**Supporting design:** `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design.md`

## 1. Purpose

This document converts the approved Plane Runtime Specification into an ordered engineering plan.

The plan is designed to:

- create the new runtime without production references to `LimboDancer.MCP.*`;
- preserve the architecture's authority boundaries;
- minimize speculative infrastructure;
- copy forward only proven behavior that remains useful;
- clean and harden copied behavior during reimplementation;
- prove directed execution before introducing autonomous cognition;
- keep each increment independently buildable and testable;
- make eventual deletion of the legacy implementation an explicit completion milestone.

This document is an implementation plan, not a second architecture specification.

When this plan conflicts with the normative runtime specification, the specification wins.

## 2. Implementation Principles

### IP-1: Clean production dependency boundary

No new production project may reference a `LimboDancer.MCP.*` project.

Legacy code may be inspected and copied as source material, but copied code is treated as new code and must conform to the new specification.

### IP-2: Preserve boundaries, minimize machinery

The implementation SHALL preserve the distinctions between:

```text
Candidate
Selected
Authorized
Executed
Verified
```

but SHOULD avoid infrastructure that has no current use case.

### IP-3: Directed execution first

The first end-to-end milestone is not an autonomous agent loop.

It is:

```text
MCP request
-> Action binding
-> ActionDescriptor
-> constraints
-> diagnostics
-> Execution Gate
-> AuthorizedAction
-> executor
-> audit
```

Only after this path is proven will autonomous Goal / Reasoning / Decision behavior be added.

### IP-4: Reimplement behavior, not legacy structure

Legacy classes are not migration units.

A legacy class that mixes transport, semantics, tenancy, persistence, and execution may become several smaller new components.

### IP-5: Keep the first solution small

Begin with five production projects.

Do not create a project for every plane.

Additional assemblies require a concrete dependency, deployment, packaging, ownership, or isolation justification.

## 3. Solution Strategy

Create the new solution in a physically isolated subtree:

```text
src/LimboDancer/LimboDancer.sln
```

The legacy source tree and legacy solution are now fully contained under `src/Legacy/`:

```text
src/Legacy/LimboDancer.MCP.sln
```

The two solutions serve different purposes during reimplementation:

```text
src/Legacy/LimboDancer.MCP.sln
    legacy behavioral reference

LimboDancer.sln
    new production architecture
```

The new solution MUST NOT include legacy production projects.

The `src/LimboDancer/` subtree is the physical boundary for new architecture build configuration, package management, and project files. New `Directory.Build.*` and `Directory.Packages.props` files SHOULD live inside that subtree so they do not alter legacy `LimboDancer.MCP.*` builds.

Legacy test projects MAY remain in the legacy solution until corresponding behavior is replaced.

## 4. Initial Project Topology

Create:

```text
src/
  LimboDancer/
    LimboDancer.sln
    Directory.Build.props
    Directory.Packages.props

    LimboDancer.Abstractions/
    LimboDancer.Runtime/
    LimboDancer.Infrastructure/
    LimboDancer.Adapters.Mcp/
    LimboDancer.Host/

    tests/
      LimboDancer.Tests.Unit/
      LimboDancer.Tests.Integration/
      LimboDancer.Tests.Architecture/
```

This keeps the new build graph, package policy, and analyzer configuration physically isolated from the legacy project tree.

Recommended responsibilities:

| Project | Responsibility |
|---|---|
| `LimboDancer.Abstractions` | Stable host-neutral contracts and authority-bearing value types |
| `LimboDancer.Runtime` | Runtime behavior: actions, constraints, diagnostics, gate, execution coordination, later reasoning/decision/orchestration |
| `LimboDancer.Infrastructure` | PostgreSQL, graph, vector, ontology persistence/providers and external service implementations |
| `LimboDancer.Adapters.Mcp` | MCP protocol translation and action bindings |
| `LimboDancer.Host` | Composition root, DI, authentication, configuration, health/telemetry, process hosting |
| `LimboDancer.Tests.Unit` | Unit tests for contracts and runtime behavior |
| `LimboDancer.Tests.Integration` | Cross-project and infrastructure integration tests |
| `LimboDancer.Tests.Architecture` | Dependency and namespace conformance tests |

## 5. Namespace Structure

Initial namespaces SHOULD be:

```text
LimboDancer.Abstractions
LimboDancer.Abstractions.Actions
LimboDancer.Abstractions.Diagnostics
LimboDancer.Abstractions.Execution
LimboDancer.Abstractions.Runtime

LimboDancer.Runtime.Actions
LimboDancer.Runtime.Diagnostics
LimboDancer.Runtime.Execution
LimboDancer.Runtime.Governance
LimboDancer.Runtime.Semantics
LimboDancer.Runtime.Audit

LimboDancer.Infrastructure.Relational
LimboDancer.Infrastructure.Graph
LimboDancer.Infrastructure.Vector
LimboDancer.Infrastructure.Ontology

LimboDancer.Adapters.Mcp

LimboDancer.Host
```

Second-stage autonomous namespaces SHOULD be introduced only when required:

```text
LimboDancer.Runtime.Observations
LimboDancer.Runtime.Reasoning
LimboDancer.Runtime.Decision
LimboDancer.Runtime.Orchestration
LimboDancer.Runtime.Verification
```

## 6. Project Reference Rules

Initial production dependency direction:

```text
                    +-------------------------+
                    | LimboDancer.Abstractions|
                    +-----------+-------------+
                                ^
                    +-----------+-----------+
                    |                       |
          +---------+---------+   +---------+-------------+
          | LimboDancer.Runtime|   | LimboDancer.Infrastructure |
          +---------+---------+   +-----------------------+
                    ^
                    |
          +---------+-------------+
          | LimboDancer.Adapters.Mcp |
          +-----------------------+

                   composed by

          +-----------------------+
          |   LimboDancer.Host    |
          +-----------------------+
```

The Host is the composition root. The Adapter MUST NOT depend on the Host.

Mandatory rules:

1. `LimboDancer.Abstractions` references no other LimboDancer production project.
2. `LimboDancer.Runtime` references `LimboDancer.Abstractions` only.
3. `LimboDancer.Infrastructure` references `LimboDancer.Abstractions` and implements inward-facing ports.
4. `LimboDancer.Adapters.Mcp` references `LimboDancer.Abstractions` and, only if necessary, `LimboDancer.Runtime` application contracts. It MUST NOT reference `LimboDancer.Host`.
5. `LimboDancer.Host` may reference all new production projects required for composition.
6. No new production project references any `LimboDancer.MCP.*` project.

### Runtime application entry point

Introduce a host-neutral directed execution boundary early:

```text
IDirectedActionRuntime
DirectedActionRequest
DirectedActionResult
```

The MCP adapter calls this boundary. It MUST NOT assemble the authority pipeline itself.

Conceptual flow:

```text
MCP Adapter
    |
    v
IDirectedActionRuntime
    |
    v
binding
descriptor
constraints
diagnostics
gate
executor
audit
```

### Infrastructure ports

Runtime MUST access State through inward-facing contracts declared in `LimboDancer.Abstractions`, for example:

```text
IHistoryReader
IHistoryWriter
IGraphQueryReader
IGraphStateReader
IGraphStateWriter
IMemorySearch
IOntologyResolver
```

Only add ports actually required by implemented behavior.

`LimboDancer.Infrastructure` implements these contracts using EF Core, Gremlin, Azure Search, ontology persistence, or other provider SDKs.

## 7. Build and Repository Baseline

Before runtime code:

### Work

- create `LimboDancer.sln`;
- create the five production projects and three test projects;
- target all new projects at `net10.0`;
- enable nullable reference types;
- enable implicit usings unless a project has a concrete reason not to;
- enable analyzers appropriate to .NET 10;
- establish warnings policy;
- establish shared build properties;
- inventory current NuGet dependencies before copying any package reference;
- introduce central package management if it reduces duplication without coupling the new solution to legacy projects;
- configure test execution;
- configure formatting/static analysis;
- add architecture dependency tests.

### Recommended repository files

```text
src/LimboDancer/Directory.Build.props
src/LimboDancer/Directory.Build.targets      // only if required
src/LimboDancer/Directory.Packages.props     // recommended after package inventory
src/LimboDancer/LimboDancer.sln
global.json                                  // repository-level only if both new and legacy builds can safely share it
```

Do not mechanically copy legacy package references.

Each package must have an identified consumer in the new architecture.

### Exit criteria

- `dotnet restore src/LimboDancer/LimboDancer.sln` succeeds;
- `dotnet build src/LimboDancer/LimboDancer.sln` succeeds;
- tests execute;
- architecture test inspects the real project graph and fails if any new production project references `LimboDancer.MCP.*`;
- no new production project references a legacy project.

### Suggested PR

**PR-01: New .NET 10 solution baseline**

## 8. Increment 1: Runtime Identity and Action Authority

### Objective

Establish the server-authoritative action model before any executor is ported.

### Project

Primarily:

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Tests.Unit
```

### Types to create

Suggested files:

```text
LimboDancer.Abstractions/
  Runtime/
    RuntimeInvocationId.cs
    CorrelationId.cs
  Actions/
    ActionId.cs
    ActionVersion.cs
    ActionRiskProfile.cs
    ActionDescriptor.cs
    ActionBinding.cs
    ExecutorBinding.cs
    IdempotencyMode.cs
    PreconditionDescriptor.cs
    EffectDescriptor.cs
    DiagnosticProfile.cs
    VerificationProfile.cs

LimboDancer.Runtime/
  Actions/
    IActionRegistry.cs
    ActionRegistry.cs
    IActionBindingRegistry.cs
    ActionBindingRegistry.cs
    IActionExecutorResolver.cs
    ActionExecutorResolver.cs
  Directed/
    IDirectedActionRuntime.cs
    DirectedActionRequest.cs
    DirectedActionResult.cs
```

Supporting conceptual types should be implemented only to the depth required by the first four actions.

### Initial action identities

Register:

```text
ldm:action/HistoryRead
ldm:action/HistoryAppend
ldm:action/GraphQuery
ldm:action/MemorySearch
```

These remain working identifiers until the ontology namespace is finalized.

### Legacy source to inspect

- `src/Legacy/LimboDancer.MCP.McpServer/McpServer.cs`
- four existing tool implementations;
- current JSON schemas / JSON-LD action metadata;
- ontology action/property mappings.

Use these only to extract:

- external tool names;
- input/output behavior;
- existing validation;
- semantic intent;
- known side effects.

### Tests

- known ActionId resolves;
- unknown ActionId fails;
- duplicate ActionId + ActionVersion rejected;
- protocol binding resolves;
- unknown protocol binding fails;
- binding cannot alter descriptor risk/preconditions/effects;
- descriptor version is immutable after registration;
- descriptors with missing required executor binding cannot become executable;
- executor binding resolves only to a registered new-runtime executor;
- directed runtime entry point does not expose protocol SDK types.

### Exit criteria

The runtime can resolve an MCP-facing name to an immutable ActionDescriptor without executing anything.

### Suggested PR

**PR-02: Action authority foundation**

## 9. Increment 2: Minimal Diagnostics

### Objective

Implement diagnostics as a runtime assurance boundary without building a plugin framework.

### Projects

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Tests.Unit
```

### Types

Suggested files:

```text
LimboDancer.Abstractions/
  Diagnostics/
    DiagnosticCheckId.cs
    DiagnosticOutcome.cs
    DiagnosticSeverity.cs
    DiagnosticPosition.cs
    DiagnosticDisposition.cs
    DiagnosticFinding.cs
    DiagnosticReference.cs

LimboDancer.Runtime/
  Diagnostics/
    IDiagnosticCheck.cs
    IDiagnosticCheck{TContext}.cs
    IDiagnosticRunner.cs
    DiagnosticRunner.cs
    DiagnosticPolicy.cs
```

Do not expose a general-purpose public diagnostic registry in v1.

DI registration or a small internal resolver is sufficient.

### Initial hard checks

Implement checks for:

- tenant context present;
- ActionDescriptor registered/current;
- executor binding resolvable through `IActionExecutorResolver`;
- required semantic mapping resolvable.

Hard-invariant outcomes:

```text
Fail          -> block
Indeterminate -> block
```

No degraded-mode exception is allowed for a hard invariant.

### Tests

- hard check Pass continues;
- hard check Fail blocks;
- hard check Indeterminate blocks;
- advisory warning can continue;
- finding includes ID, version, outcome, severity, code;
- missing required check prevents descriptor publication/execution;
- diagnostics cannot produce AuthorizedAction;
- diagnostic cancellation/timeout is bounded.

### Exit criteria

A descriptor/action context can be evaluated by a small deterministic diagnostic runner and produces structured findings.

### Suggested PR

**PR-03: Diagnostic runtime substrate**

## 10. Increment 3: Execution Gate and Authorization

### Objective

Create the one boundary that every consequential action must cross.

### Projects

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Tests.Unit
```

### Types

Suggested files:

```text
LimboDancer.Abstractions/
  Execution/
    RuntimePrincipal.cs
    ExecutionContext.cs
    SelectedAction.cs
    SelectionOrigin.cs
    AuthorizedAction.cs
    ExecutionGateOutcome.cs
    ExecutionGateResult.cs

LimboDancer.Runtime/
  Execution/
    IExecutionGate.cs
    ExecutionGate.cs
    IActionExecutor.cs
    ActionExecutionResult.cs
```

### Directed constraint boundary

Before or as part of this increment, define a small deterministic directed constraint abstraction, for example:

```text
IActionConstraintEvaluator
ConstraintEvaluationResult
```

It evaluates the applicable trusted semantic, Governance, tenant, argument-validation, and precondition constraints for one explicitly selected action.

The later autonomous `IActionConstraintPipeline` SHOULD reuse the same underlying evaluators across multiple ActionCandidates rather than implementing a second constraint system.

### Gate responsibilities

The first gate MUST validate:

- immutable tenant context;
- principal/permission requirements;
- descriptor identity/version;
- input validation state;
- required preconditions;
- required diagnostic dispositions;
- applicable action risk policy;
- relevant current State version when available.

Human confirmation is not required in this increment unless one of the first four actions genuinely requires it.

### Authorization construction

For v1 in-process execution:

- `AuthorizedAction` should be constructible only by trusted runtime code;
- internal constructor/factory patterns are sufficient;
- do not introduce signatures/tokens merely to prove object authenticity.

### Tests

- valid directed selection authorizes;
- missing tenant denies;
- permission denial denies;
- stale version returns Stale;
- blocking diagnostic returns DiagnosticBlocked;
- failed required precondition denies;
- AuthorizedAction cannot be supplied from MCP deserialization;
- confidence values cannot bypass the gate.

### Exit criteria

A SelectedAction can become AuthorizedAction only through the new gate.

### Suggested PR

**PR-04: Execution authority boundary**

## 11. Increment 4: Structured Audit Boundary

### Objective

Capture the authority boundary before porting consequential behavior.

### Projects

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Infrastructure
```

### Contracts

Create a small audit boundary:

```text
IAuditSink
RuntimeAuditEvent
AuditEventType
```

Initial audit events SHOULD cover:

- invocation admitted;
- action resolved;
- constraint result;
- diagnostic finding/disposition;
- gate authorized/denied/stale;
- executor started/completed/failed.

Minimum common identity:

```text
RuntimeInvocationId
CorrelationId
TenantId
ActionId
ActionVersion
```

Goal/Decision fields remain optional until autonomous execution exists.

### Persistence

For the first slice, choose the simplest durable or testable implementation that fits current deployment needs.

Do not create a sophisticated event-sourcing subsystem unless required.

### Tests

- authorization recorded;
- denial recorded;
- blocking diagnostic recorded;
- descriptor version recorded;
- sensitive payloads not required;
- hidden chain-of-thought never required.

### Exit criteria

The directed authority path is reconstructable from structured audit evidence: invocation identity, action identity/version, diagnostic findings, gate result, authorization result, and executor outcome.

### Suggested PR

**PR-05: Runtime audit boundary**

## 12. Increment 5: Infrastructure Foundations

### Objective

Reimplement only the State capabilities required by the four compatibility actions.

### Project

```text
LimboDancer.Infrastructure
```

### Ports first

Before implementing provider code, add only the inward-facing State contracts required by the four actions to `LimboDancer.Abstractions`.

Initial likely ports:

```text
IHistoryReader
IHistoryWriter
IGraphQueryReader
IMemorySearch
IOntologyResolver
```

Add mutation-specific graph ports only if HistoryAppend or another migrated action actually requires them.

### Namespaces

```text
LimboDancer.Infrastructure.Relational
LimboDancer.Infrastructure.Graph
LimboDancer.Infrastructure.Vector
LimboDancer.Infrastructure.Ontology
```

### Legacy source to inspect

Relational:

- `src/Legacy/LimboDancer.MCP.Storage/`;
- `src/Legacy/LimboDancer.MCP.McpServer/Services/HistoryService.cs` or its current legacy location;
- EF models and migrations.

Graph:

- `src/Legacy/LimboDancer.MCP.Graph.CosmosGremlin/`;
- `src/Legacy/LimboDancer.MCP.McpServer/` implementation of `TenantScopedGraphStore`;
- graph query services;
- `src/Legacy/LimboDancer.MCP.McpServer/` implementation of `GraphPreconditionsService`;
- `src/Legacy/LimboDancer.MCP.McpServer/` implementation of `GraphEffectsService`.

Vector:

- `src/Legacy/LimboDancer.MCP.Vector.AzureSearch/`;
- `src/Legacy/LimboDancer.MCP.McpServer/` implementation of `VectorSearchService`;
- tenant filter construction.

Ontology:

- `src/Legacy/LimboDancer.MCP.Ontology/`;
- property/relation mapping;
- ontology repository contracts and validators.

### Reimplementation requirements

Do not reproduce known ambiguities:

- history reads must be tenant-safe;
- graph reads and writes must be tenant-safe;
- vector retrieval must always enforce tenant scope;
- unknown semantic predicates must fail closed;
- unknown ontology filters must not silently broaden a correctness/security-sensitive query;
- infrastructure SDK types must not leak into core runtime contracts.

### Tests

Use integration tests where physical SDK behavior matters.

Required:

- cross-tenant relational read prevented;
- cross-tenant graph read prevented;
- cross-tenant graph mutation prevented;
- vector query tenant filter present;
- unknown semantic graph mapping fails closed;
- ontology mapping roundtrip works;
- cancellation is propagated.

### Exit criteria

New infrastructure implementations support the four initial actions with no legacy assembly references.

### Suggested PR

**PR-06: Tenant-safe State infrastructure**

## 13. Increment 6: Reimplement the Four Directed Actions

### Objective

Port behavior into clean action executors.

### Runtime/executor placement

Use new executors, not wrappers around legacy tools.

Suggested files:

```text
LimboDancer.Runtime/
  Actions/
    History/
      HistoryReadExecutor.cs
      HistoryAppendExecutor.cs
    Graph/
      GraphQueryExecutor.cs
    Memory/
      MemorySearchExecutor.cs
```

If domain/application logic grows materially, move implementation services to suitable Runtime namespaces rather than bloating executor classes.

### 13.1 HistoryRead

Inspect legacy:

- history reader/service contract;
- current filtering and output format;
- session/timestamp semantics.

Correct:

- explicit/verified tenant scope;
- stable transport-neutral result contract.

### 13.2 HistoryAppend

Inspect legacy:

- append behavior;
- caller preconditions/effects;
- graph-side interactions.

Correct:

- caller does not define authoritative effects;
- authoritative preconditions/effects come from ActionDescriptor/semantic definitions;
- compatibility payload fields, if accepted, are non-authoritative restrictions only.

### 13.3 GraphQuery

Inspect legacy:

- graph query DTOs;
- ontology mappings;
- filtering behavior.

Correct:

- unknown required semantic mappings fail closed;
- no silent broadening;
- tenant scope structural.

### 13.4 MemorySearch

Inspect legacy:

- Azure Search document shape;
- vector configuration;
- tenant filter behavior.

Preserve:

- mandatory tenant filter;
- relevant ranking/search behavior.

### Tests

Per action:

- descriptor binding;
- schema validation;
- tenant enforcement;
- gate authorization;
- executor behavior;
- output validation;
- audit event;
- cancellation;
- failure reason code.

### Exit criteria

All four action executors can execute successfully through the new runtime authority boundary using only new projects.

### Suggested PRs

Prefer one PR per meaningful capability if implementation size warrants:

- **PR-07: History actions**
- **PR-08: Graph query action**
- **PR-09: Memory search action**

Otherwise combine into one directed-actions PR after infrastructure is stable.

## 14. Increment 7: New MCP Adapter

### Objective

Expose the reimplemented actions through MCP without importing legacy MCP runtime code.

### Project

```text
LimboDancer.Adapters.Mcp
```

### Responsibilities

- MCP initialization;
- tool listing;
- tool execution request parsing;
- external-name -> ActionBinding resolution;
- argument normalization;
- creation of RuntimeInvocationId/CorrelationId;
- transfer of already-admitted principal/tenant context;
- call into Runtime directed-execution API;
- protocol-safe response/error mapping.

The adapter MUST NOT:

- decide action semantics;
- assign authoritative risk;
- manufacture AuthorizedAction;
- execute infrastructure directly.

### Compatibility target

Preserve tool names where useful:

```text
history_get
history_append
graph_query
memory_search
```

Compatibility of legacy caller-controlled semantic fields is optional and must never restore caller authority.

### Tests

- initialize/list tools;
- known tool binds;
- unknown tool rejected;
- invalid args rejected;
- trusted tenant/principal context is supplied by the Host/admission boundary and transferred unchanged by the adapter;
- directed call does not invoke Decision provider;
- authorization object cannot enter from request;
- runtime reason codes map to protocol-safe errors.

### Exit criteria

An MCP client can invoke all four new actions through the new Execution Gate.

### Suggested PR

**PR-10: New MCP interaction adapter**

## 15. Increment 8: Host and Composition Root

### Objective

Create the production process boundary for the new runtime.

### Project

```text
LimboDancer.Host
```

### Responsibilities

- configuration;
- DI;
- authentication/authorization;
- tenant resolution;
- runtime service registration;
- infrastructure provider registration;
- MCP adapter hosting;
- health/readiness;
- telemetry/observability;
- cancellation/shutdown;
- environment validation.

### Rules

- readiness is observational;
- DB/schema migration is not normal readiness work;
- no legacy production project reference;
- no runtime authority logic in Program/bootstrap code;
- Host references and composes `LimboDancer.Adapters.Mcp`; the adapter never references Host.

### Tests

- composition resolves;
- startup structural diagnostics fail when a required descriptor/executor is missing;
- readiness does not mutate schema;
- authentication/tenant context flows into invocation;
- new solution can start without legacy assemblies.

### Exit criteria

The new runtime can be launched independently of `src/Legacy/LimboDancer.MCP.sln`.

### Suggested PR

**PR-11: New runtime host**

## 16. Milestone A: Directed Runtime Conformance

Before autonomous work begins, all of the following MUST be true:

1. `LimboDancer.sln` builds independently.
2. No new production project references `LimboDancer.MCP.*`.
3. Four MCP compatibility actions execute through the new gate.
4. Tenant isolation tests pass for relational, graph, and vector behavior.
5. Unknown required semantic mappings fail closed.
6. Hard diagnostics fail closed.
7. Gate decisions are audited.
8. Executors receive only AuthorizedAction.
9. Legacy caller effects cannot define execution semantics.
10. New Host starts and runs without legacy assemblies.

This milestone should receive a dedicated architecture/conformance review before autonomous implementation begins.

## 17. Increment 9: Autonomous Runtime Contracts

### Objective

Add only the contracts necessary to move from explicit action invocation to Goal-driven execution.

### Projects

```text
LimboDancer.Abstractions
LimboDancer.Runtime
```

### Types

Suggested:

```text
GoalId
StepId
Goal
GoalOrigin
GoalLifecycleState
Observation
ObservationSource
ActionCandidate
PermittedAction
RejectedCandidate
ConstraintResult
DecisionContext
DecisionResult
DecisionOutcome
RuntimeBudget
GoalResult
```

### Runtime interfaces

```text
IActionResolver
IActionConstraintPipeline
IDecisionProvider
IGoalOrchestrator
```

### Rules

- ActionResolver returns finite registered candidates;
- constraints produce PermittedAction;
- empty permitted set does not call Decision;
- Decision sees only PermittedAction;
- provider selection outside candidate set is rejected;
- Decision never creates authorization.

### Tests

Contract/state-machine tests before provider implementation.

### Exit criteria

A deterministic test harness can construct a Goal, validate lifecycle state transitions, and represent ActionCandidate / PermittedAction / Decision contracts without executing anything. Producing a real autonomous SelectedAction is deferred until the deterministic Decision provider exists.

### Suggested PR

**PR-12: Autonomous runtime contracts**

## 18. Increment 10: Observation and Semantic Resolution

### Objective

Make autonomous candidate generation grounded in current State and ontology.

### Namespaces

```text
LimboDancer.Runtime.Observations
LimboDancer.Runtime.Semantics
```

### Work

- observation acquisition contracts;
- provenance/version capture;
- action resolution from semantic intent;
- finite candidate generation;
- semantic constraint evaluation;
- tenant-scoped observations;
- resolution diagnostics.

### Tests

- unknown action semantics produce no executable candidate;
- unresolved required ontology references fail closed;
- observations preserve tenant/version/provenance;
- duplicate candidates rejected;
- failed hard precondition removes candidate.

### Exit criteria

A Goal can produce a finite, auditable set of PermittedActions.

### Suggested PR

**PR-13: Observation and semantic action resolution**

## 19. Increment 11: Deterministic Decision Provider

### Objective

Prove the Decision contract without adding probabilistic behavior.

### Namespace

```text
LimboDancer.Runtime.Decision
```

### Implementation

Create a simple deterministic/reference provider, for example:

```text
RuleDecisionProvider
```

Its purpose is to test:

- candidate boundary;
- selection validity;
- abstention;
- escalation;
- Decision audit;
- gate revalidation.

It is not intended to become the final intelligence layer.

### Tests

- provider gets only PermittedActions;
- selects valid CandidateId;
- abstention executes nothing;
- escalation executes nothing;
- invalid provider result rejected;
- confidence never grants authority.

### Exit criteria

A Goal can progress through Decision to SelectedAction deterministically.

### Suggested PR

**PR-14: Deterministic Decision Plane**

## 20. Increment 12: Minimal Reasoning Boundary

### Objective

Introduce the Reasoning contract before the orchestrator depends on it, without requiring an LLM.

### Namespace

```text
LimboDancer.Runtime.Reasoning
```

### Contracts

Define the smallest viable abstraction needed for:

- structured Goal interpretation;
- semantic-intent proposal;
- missing-observation request;
- plan/subgoal proposal where needed;
- result synthesis boundary.

The first implementation SHOULD be deterministic and minimal, for example a structured pass-through provider that accepts an already-structured Goal and returns its semantic intent unchanged.

Reasoning MUST NOT:

- invoke IActionExecutor;
- construct AuthorizedAction;
- invent unregistered semantic actions;
- override semantic/governance constraints.

### Diagnostics

Add:

- repeated next-step detection;
- repeated plan without state change;
- unresolved entity/semantic references;
- reasoning deadline/step budget.

### Exit criteria

The runtime has a real Reasoning boundary that can be composed by Orchestration without requiring an LLM.

### Suggested PR

**PR-15: Minimal Reasoning boundary**

## 21. Increment 13: Goal Orchestration

### Objective

Coordinate the full autonomous lifecycle without moving authority into orchestration.

### Namespace

```text
LimboDancer.Runtime.Orchestration
```

### Work

Implement:

- Goal admission;
- RuntimeInvocationId creation;
- lifecycle state transition validation;
- observations;
- the minimal Reasoning provider;
- action resolution;
- constraints;
- Decision;
- diagnostics;
- gate;
- execute;
- verify where applicable;
- continue/complete;
- cancellation;
- budget enforcement.

Initial RuntimeBudget SHOULD start with:

```text
Deadline
MaxSteps
MaxRetries
```

Token/cost/external-call budgets can be added when actual providers require them.

### Tests

- canonical autonomous happy path;
- no-candidate termination;
- abstention;
- stale-state re-observe;
- retry budget;
- cancellation;
- invalid lifecycle transition;
- multi-step revalidation;
- orchestration cannot override Governance denial.

### Exit criteria

A simple Goal can complete end-to-end using the minimal Reasoning provider and deterministic Decision provider.

### Suggested PR

**PR-16: Goal orchestration loop**

## 22. Increment 14: Effect Verification

### Objective

Add verification where semantic effects are concrete and worth checking.

### Namespace

```text
LimboDancer.Runtime.Verification
```

### Rules

Verification is opt-in per action/profile.

Start with actions whose effects can be checked cheaply and deterministically.

Statuses:

```text
Verified
PartiallyVerified
Unverifiable
Contradicted
```

Do not turn verification into a second diagnostic framework.

### Tests

- verified change;
- contradicted change;
- unverifiable represented explicitly;
- contradiction audited;
- configured recovery/escalation invoked.

### Suggested PR

**PR-17: Effect verification**

## 23. Increment 15: Replay-Capable Evidence

### Objective

Capture enough history for later provider evaluation without building a replay engine prematurely.

Persist/reference:

- descriptor/version;
- observations/version refs;
- candidate set;
- rejected/permitted candidates;
- Decision provider/version;
- Decision result;
- gate result;
- execution result;
- verification result.

### Exit criteria

A historical Decision context can be reconstructed from stored evidence in a test utility.

A general replay engine is not yet required.

### Suggested PR

**PR-18: Replay-capable decision evidence**

## 24. Increment 16: Additional Decision Providers

### Objective

Add alternative intelligence behind the stable Decision contract.

Candidates include:

- Jev/System One;
- LLM structured-choice provider;
- local classifier;
- composite strategies later.

### Provider evaluation

Measure at least:

- selection correctness on labeled cases;
- abstention behavior;
- calibration where available;
- latency;
- token/cost use;
- invalid-result rate;
- fallback rate;
- wrong-action cost;
- task completion impact.

Provider routing SHOULD NOT be added until at least two real providers have enough evidence to justify routing policy.

### Suggested PR

One PR per provider.

## 25. Deferred Until Concrete Use Cases

Do not implement merely because the architecture permits them:

- sophisticated provider router;
- replay engine;
- distributed authorization signatures;
- compensation engine;
- human confirmation UI;
- multi-agent coordination;
- distributed transaction coordinator;
- dynamic ontology-generated executors;
- complex workflow DSL;
- cross-plane service bus;
- one assembly per plane.

Each requires a concrete scenario and a small design note before implementation.

## 26. Test Strategy

### Unit tests

Focus on authority and deterministic contracts:

- registry;
- binding;
- constraints;
- diagnostics;
- gate;
- lifecycle;
- Decision result validation;
- budgets.

### Integration tests

Focus on boundaries with real infrastructure semantics:

- PostgreSQL tenant isolation;
- Cosmos Gremlin tenant isolation;
- Azure Search tenant filters;
- ontology persistence/mapping;
- MCP request/response;
- Host composition.

### Architecture tests

Enforce:

- no `LimboDancer.MCP.*` production project reference by inspecting the actual project graph;
- Abstractions has no outward project references;
- Runtime does not reference infrastructure SDK projects;
- protocol SDK types do not appear in core authority contracts;
- infrastructure SDK types do not appear in Abstractions;
- namespace/project naming rules.

### Conformance tests

Trace requirements back to `SPEC-*` identifiers where practical.

The architecture test project SHOULD provide a small explicit mapping from critical specification IDs to tests for:

- tenant invariants;
- semantic fail-closed behavior;
- Decision candidate integrity;
- diagnostic hard invariants;
- Execution Gate authorization;
- legacy dependency prohibition.

## 27. Legacy Source Admission Checklist

Whenever code is copied or closely adapted from `LimboDancer.MCP.*`, the PR must answer:

1. What legacy behavior is being preserved?
2. What legacy class/file was used as reference?
3. Which responsibility/plane owns the behavior now?
4. Were transport concerns removed?
5. Is tenant scope explicit or structurally unavoidable?
6. Does semantic resolution fail closed?
7. Are caller-controlled preconditions/effects still authoritative? They must not be.
8. Are external SDK types contained within Infrastructure/Adapter boundaries?
9. Are cancellation and async behavior correct?
10. What tests prove preserved behavior and corrected defects?
11. Does the new implementation have any legacy project/assembly dependency? It must not.

## 28. PR Discipline

Each PR SHOULD:

- compile independently;
- keep the new solution green;
- contain tests for its authority boundary;
- avoid unrelated legacy cleanup;
- avoid speculative project creation;
- update this plan only when implementation evidence changes sequencing;
- reference applicable `SPEC-*` requirements in the PR description.

Do not mix broad legacy deletion into implementation PRs until replacement parity exists.

## 29. Legacy Retirement Plan

Legacy deletion begins only after the new runtime reaches required parity. The intended end state is deletion of the entire `src/Legacy/` directory as one retirement unit.

### Retirement prerequisites

- directed MCP parity confirmed;
- autonomous target capabilities implemented;
- required data/state migrations validated;
- operator/deployment workflows replaced;
- new tests cover retained behavior;
- no deployment script requires legacy assemblies;
- no CI workflow requires legacy production projects;
- documentation points to the new runtime.

### Retirement sequence

1. mark legacy projects frozen;
2. remove legacy projects from active CI/deployment;
3. archive any required compatibility fixtures;
4. delete obsolete `LimboDancer.MCP.*` production projects;
5. delete legacy-only tests;
6. delete the entire `src/Legacy/` tree, including `src/Legacy/LimboDancer.MCP.sln`;
7. remove obsolete package/config entries;
8. run full new-solution conformance suite;
9. update repository README/architecture index.

Legacy removal should be performed in dedicated PRs after parity, not piecemeal during early implementation.

## 30. .NET 11 Upgrade Checkpoint

After .NET 11 GA, perform the specification-required upgrade assessment.

Do not combine the .NET 11 framework migration with a major architecture increment.

Preferred sequence:

```text
stable implementation checkpoint
-> dependency compatibility review
-> isolated framework upgrade PR
-> full conformance/integration test
-> continue feature work
```

## 31. Milestone Summary

### Milestone 0: Build boundary

`src/LimboDancer/LimboDancer.sln` exists, builds, tests, and has no legacy production dependency.

### Milestone A: Directed authority runtime

The four compatibility MCP capabilities run entirely through the new ActionDescriptor / Diagnostics / Execution Gate / AuthorizedAction path.

### Milestone B: Autonomous selection runtime

Goal -> Observation -> Resolution -> PermittedAction -> Decision -> SelectedAction works with deterministic Decision.

### Milestone C: Autonomous execution runtime

Goal orchestration can execute bounded multi-step work through the common gate.

### Milestone D: Intelligence evaluation

Reasoning and multiple Decision providers can be evaluated behind stable contracts.

### Milestone E: Legacy retirement

The new runtime satisfies required parity and all legacy production projects can be removed.

## 32. Recommended PR Sequence

```text
PR-01  New .NET 10 solution baseline
PR-02  Action authority foundation
PR-03  Diagnostic runtime substrate
PR-04  Execution authority boundary
PR-05  Runtime audit boundary
PR-06  Tenant-safe State infrastructure
PR-07  History actions
PR-08  Graph query action
PR-09  Memory search action
PR-10  New MCP interaction adapter
PR-11  New runtime host

--- Milestone A review ---

PR-12  Autonomous runtime contracts
PR-13  Observation and semantic action resolution
PR-14  Deterministic Decision Plane
PR-15  Minimal Reasoning boundary
PR-16  Goal orchestration loop
PR-17  Effect verification
PR-18  Replay-capable decision evidence

--- provider work ---

PR-19+ Jev / LLM / other Decision providers
PR-N   Provider routing only when evidence justifies it

--- final migration ---

Dedicated legacy retirement PR series
```

The PR numbering is planning guidance, not a requirement. PRs may be split further when reviewability benefits, but major authority boundaries SHOULD NOT be collapsed into one large initial rewrite.

## 33. Definition of Implementation Ready

Implementation may begin when:

- the design and specification are approved;
- this plan is approved;
- `decision-plane` remains the working architecture branch or an implementation branch is cut from it;
- the .NET 10 SDK baseline is available in local/CI environments;
- repository permissions allow creation of the new solution/projects;
- no unresolved architecture question blocks PR-01 through PR-04.

No Decision-provider choice, policy-engine choice, replay-engine design, or human-confirmation UX is required before implementation starts.

## 34. First Concrete Engineering Task

The first engineering change should be deliberately small:

```text
Create src/LimboDancer/LimboDancer.sln
Create five production projects under src/LimboDancer
Create three test projects under src/LimboDancer/tests
Target net10.0
Establish project references
Add CI architecture guard
Build
Run tests
Commit
```

No legacy code should be copied in this first PR.

That gives the new architecture a clean physical home before any behavior is reimplemented.
