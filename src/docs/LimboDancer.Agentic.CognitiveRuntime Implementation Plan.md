# LimboDancer.Agentic.CognitiveRuntime Implementation Plan

**Status:** Implementation planning baseline  
**Branch:** `decision-plane`  
**Product / repository identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**.NET root namespace / project prefix:** `LimboDancer`  
**Target framework:** `net10.0` / C# 14  
**Legacy archive root:** `src/_Legacy/` (archival only; retained, not built, not referenced)  
**Normative source:** `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md`  
**Supporting design:** `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design.md`

**Domain integration design:** `LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`

**Reference-domain requirements:** `docs/ASL/legacy-limbodancer-mcp-system-design.md`

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

Begin with five runtime production projects.

A separate `LimboDancer.AppHost` MAY provide outer application and process orchestration. It is operational composition support, not a sixth cognitive-runtime production layer.

Do not create a project for every plane.

Additional assemblies require a concrete dependency, deployment, packaging, ownership, or isolation justification.

## 3. Solution Strategy

Create the new solution in a physically isolated subtree:

```text
src/LimboDancer/LimboDancer.sln
```

The legacy source tree and legacy solution are now fully contained under `src/_Legacy/`:

```text
src/_Legacy/LimboDancer.MCP.sln
```

The two solutions serve different purposes during reimplementation:

```text
src/_Legacy/LimboDancer.MCP.sln
    archived legacy implementation (archival only)

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
    LimboDancer.AppHost/

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
| `LimboDancer.Host` | Runtime composition root, DI, authentication, configuration, health/telemetry, process hosting |
| `LimboDancer.AppHost` | Aspire application topology, local process/resource orchestration, service wiring, and operational dashboard |
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
LimboDancer.AppHost
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
          +-----------+-----------+
                      ^
                      |
          +-----------+-----------+
          | LimboDancer.AppHost   |
          +-----------------------+
             outer orchestration
```

`LimboDancer.Host` is the runtime composition root. `LimboDancer.AppHost` is an outer Aspire application-orchestration project. The Adapter MUST NOT depend on either host.

Mandatory rules:

1. `LimboDancer.Abstractions` references no other LimboDancer production project.
2. `LimboDancer.Runtime` references `LimboDancer.Abstractions` only.
3. `LimboDancer.Infrastructure` references `LimboDancer.Abstractions` and implements inward-facing ports.
4. `LimboDancer.Adapters.Mcp` references `LimboDancer.Abstractions` and, only if necessary, `LimboDancer.Runtime` application contracts. It MUST NOT reference `LimboDancer.Host`.
5. `LimboDancer.Host` may reference all new production projects required for composition.
6. No new production project references any `LimboDancer.MCP.*` project.
7. `LimboDancer.AppHost` references `LimboDancer.Host` only; no runtime project may reference the AppHost.
8. Aspire SDK and hosting types MUST NOT appear in `LimboDancer.Abstractions` or `LimboDancer.Runtime`.
9. Aspire operational telemetry MUST NOT replace runtime diagnostics, Governance decisions, Execution Gate results, or authoritative audit evidence.
10. The AppHost MAY describe only infrastructure and services admitted by the current runtime implementation; it MUST NOT make deferred provider choices by implication.

### Aspire operational boundary

Aspire orchestration starts processes and infrastructure, supplies configuration and service discovery, and exposes operational health and telemetry. LimboDancer runtime orchestration governs goals, action candidates, decisions, authorization, execution, verification, and audit. These two meanings of orchestration MUST remain distinct.

The initial AppHost orchestrates `LimboDancer.Host` only. PostgreSQL, Redis, graph, vector, cloud, or other integrations are added only when the corresponding inward-facing port and provider implementation have been admitted. `LimboDancer.Host` MUST remain independently runnable without Aspire.

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
- create the five runtime production projects, the outer `LimboDancer.AppHost`, and three test projects;
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

- `src/_Legacy/LimboDancer.MCP.McpServer/McpServer.cs`
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

- `src/_Legacy/LimboDancer.MCP.Storage/`;
- `src/_Legacy/LimboDancer.MCP.McpServer/Services/HistoryService.cs` or its current legacy location;
- EF models and migrations.

Graph:

- `src/_Legacy/LimboDancer.MCP.Graph.CosmosGremlin/`;
- `src/_Legacy/LimboDancer.MCP.McpServer/` implementation of `TenantScopedGraphStore`;
- graph query services;
- `src/_Legacy/LimboDancer.MCP.McpServer/` implementation of `GraphPreconditionsService`;
- `src/_Legacy/LimboDancer.MCP.McpServer/` implementation of `GraphEffectsService`.

Vector:

- `src/_Legacy/LimboDancer.MCP.Vector.AzureSearch/`;
- `src/_Legacy/LimboDancer.MCP.McpServer/` implementation of `VectorSearchService`;
- tenant filter construction.

Ontology:

- `src/_Legacy/LimboDancer.MCP.Ontology/`;
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

- modern MCP `server/discover` capability advertisement;
- bounded legacy `initialize` compatibility for `2025-11-25` and earlier clients;
- tool listing;
- tool execution request parsing;
- external-name -> ActionBinding resolution;
- argument normalization;
- creation of RuntimeInvocationId/CorrelationId;
- transfer of already-admitted principal/tenant context;
- call into Runtime directed-execution API;
- protocol-safe response/error mapping.

The adapter targets the stateless MCP `2026-07-28` lifecycle. Protocol version,
client identity, and client capabilities arrive per request; protocol sessions are
not an authority source. Legacy initialization support may counter-offer the
latest handshake-era revision but MUST NOT create runtime authorization state.

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

- modern discovery and legacy initialize compatibility;
- list tools;
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

The new runtime can be launched independently of `src/_Legacy/LimboDancer.MCP.sln`.

### Suggested PR

**PR-11: New runtime host**

### Implemented baseline

The initial Host is an independently runnable ASP.NET Core process that:

- composes the directed runtime, MCP adapter, and admitted in-memory reference providers;
- validates options plus descriptor, binding, diagnostic, and executor structure at startup;
- fails closed for unconfigured authentication and for descriptors whose preconditions have no concrete evaluator;
- derives tenant and principal context from authenticated credentials rather than request payloads;
- hosts discovery, legacy initialization, tool listing, and tool invocation HTTP endpoints;
- exposes observational liveness and readiness probes without schema or State mutation;
- propagates request cancellation into adapter/runtime execution;
- provides host-level activity and metric instrumentation without replacing runtime audit evidence; and
- remains runnable directly while the AppHost supplies only outer Aspire orchestration and readiness wiring.

The initial credential scheme is a bounded host admission mechanism, not a permanent identity-provider decision. A later deployment may replace it with an admitted external authentication provider while preserving `IMcpCallerContextFactory` and the rule that caller payloads cannot establish authority.

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

The Milestone A review SHALL also select the first ASL read-only adjudication scenario and use it to approve the smallest domain-neutral vocabulary needed by PR-12 and PR-13. This is a design and contract-admission checkpoint, not authorization to implement a generalized domain framework.

The review must identify:

- domain, package, and canonical-source identities;
- required observations and versions;
- semantic and exception-resolution steps;
- deterministic calculations;
- DomainConclusion, ambiguity, explanation, and audit needs;
- which existing runtime ports are sufficient;
- which new interface, if any, has concrete justification.

### Review outcome

Milestone A is approved in the [Milestone A Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone A Conformance Review.md>). The review selected **ASL Scenario A1: occupied-building entry eligibility**, closed the executable-Host proof gap with a loopback HTTP conformance test, admitted the minimum domain-neutral conclusion vocabulary for PR-12, and bounded the package, observation, entity-resolution, and conclusion-resolution responsibilities eligible for PR-13.

## 17. Increment 9: Autonomous Runtime Contracts

### Objective

Add only the contracts necessary to move from explicit action invocation to Goal-driven execution and evidence-backed conclusion outcomes.

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
DomainQuestion (if admitted by the Milestone A scenario)
DomainConclusion (if admitted by the Milestone A scenario)
ConclusionDisposition (if admitted by the Milestone A scenario)
Domain/package and evidence-reference primitives proven necessary by the scenario
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
- Decision never creates authorization;
- DomainConclusion never creates execution authority;
- domain-neutral primitives contain no ASL or infrastructure SDK types.

### Tests

Contract/state-machine tests before provider implementation.

### Exit criteria

A deterministic test harness can construct a Goal, validate lifecycle state transitions, represent ActionCandidate / PermittedAction / Decision contracts, and represent a DomainConclusion terminal outcome without executing anything. Producing a real autonomous SelectedAction is deferred until the deterministic Decision provider exists.

### Suggested PR

**PR-12: Autonomous runtime contracts**

### Implemented baseline

The autonomous contract baseline now:

- introduces typed Goal, Step, observation, budget, and orchestration context contracts with tenant-bound inputs;
- defines explicit lifecycle transitions, including bounded retry, confirmation, abstention, escalation, failure, and cancellation paths;
- separates finite ActionCandidate resolution, constraint evaluation, PermittedAction typestate, and Decision provider inputs;
- validates that provider selections and score distributions refer only to the supplied permitted candidate set;
- admits the Milestone A domain-neutral package, semantic identifier, canonical reference, evidence, question, and conclusion vocabulary;
- represents evidence-backed DomainConclusion values as terminal Goal results without creating action selection or execution authority; and
- retains autonomous SelectedAction materialization for the deterministic provider increment.

Contract and state-machine tests prove the admitted boundaries without adding a provider, orchestrator implementation, generalized domain framework, or concrete ASL package.

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
- resolution diagnostics;
- minimal domain-package, entity-resolution, evidence, or conclusion-resolution contracts admitted by the Milestone A scenario;
- Host-composable domain registration through existing runtime ports.

### Tests

- unknown action semantics produce no executable candidate;
- unresolved required ontology references fail closed;
- observations preserve tenant/version/provenance;
- duplicate candidates rejected;
- failed hard precondition removes candidate;
- a minimal fake domain proves approved contracts are not ASL-specific;
- Runtime has no dependency on ASL or another concrete domain package;
- removing the fake or ASL package does not break core runtime conformance.

### Exit criteria

A Goal can produce a finite, auditable set of PermittedActions. When DomainConclusion contracts are admitted, a fake domain can also produce an evidence-backed terminal conclusion without creating action authority.

### Suggested PR

**PR-13: Observation and semantic action resolution**

### Implemented baseline

The observation and semantic-resolution baseline now:

- resolves only exact registered domain-package versions and reports unavailable versions explicitly;
- bounds observation queries and enforces unique, tenant-scoped, package-exact results with version and provenance retention;
- represents entity resolution as explicit resolved, unresolved, or ambiguous outcomes with canonical and evidence references;
- supplies a conclusion-resolution context containing only an exact package, pre-resolved entities, supplied observations, and bounded calculation evidence;
- resolves exact registered action intent into a finite candidate set and returns no candidate for unknown action semantics;
- evaluates required semantic preconditions into permitted or rejected candidate typestate and fails closed for missing evaluators or unhandled required constraint classes; and
- makes package resolution, action resolution, and semantic constraint evaluation composable by the Host without registering a concrete domain.

A removable fake domain in the test assembly proves the four approved package, observation, entity, and conclusion ports without ASL vocabulary. Its conformance cases keep unknown, ambiguous, stale, conflicting, and cross-tenant evidence from producing a definitive conclusion. No generalized domain service, rule engine, plugin loader, ASL package, Decision provider, or autonomous execution path is introduced.

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

### Implemented baseline

The deterministic Decision baseline now:

- provides a reference rule provider that selects one permitted candidate, abstains from an empty set, and escalates multiple permitted candidates;
- wraps providers in a Decision Plane that validates provider identity, candidate containment, and distribution containment before materializing selection;
- bypasses the provider when no candidate survived constraints and records an explicit abstention;
- materializes `SelectedAction` only from a validated selected result while retaining the complete `DecisionResult` as evidence;
- represents abstention and escalation without a selected action, preventing either outcome from reaching execution;
- audits accepted, rejected, and provider-failed Decision outcomes with Goal, Step, provider, candidate, outcome, reason, and confidence fields;
- leaves `AuthorizedAction` creation exclusively inside the Execution Gate; and
- revalidates decision-selected actions at the gate, including current constraint and state-version checks.

The Host composes the deterministic provider and Decision Plane, but no Goal orchestration loop or autonomous execution path is added in this increment. Alternative providers remain behind the same contract.

## 20. Milestone B: Autonomous Selection Conformance

Before Reasoning and Goal orchestration are implemented, the complete deterministic selection path MUST be proven without creating execution authority.

The milestone requires:

1. one integrated Goal-to-SelectedAction conformance test;
2. finite registered candidates grounded in versioned observations;
3. fail-closed required semantic constraints;
4. Decision access only to `PermittedAction`;
5. provider bypass for an empty permitted set;
6. explicit selection, abstention, and escalation behavior;
7. invalid provider-result rejection;
8. structured Decision audit evidence;
9. independent Execution Gate revalidation; and
10. continued separation of `DomainConclusion`, `SelectedAction`, and `AuthorizedAction`.

### Review outcome

Milestone B is approved in the [Milestone B Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone B Conformance Review.md>). The review closed the integrated-path proof gap with a deterministic Goal-to-SelectedAction conformance test, confirmed the selection and authority boundaries, and admitted only the minimal deterministic Reasoning work described by PR-15.

## 21. Increment 12: Minimal Reasoning Boundary

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

### Implemented baseline

The minimal Reasoning baseline now:

- defines structured action-proposal, observation-required, completed, and abstained result dispositions;
- provides a deterministic pass-through provider that preserves an already structured Goal intent and arguments without using an LLM;
- carries proposed semantic action intent explicitly into registered action resolution rather than mutating the Goal or creating candidates directly;
- blocks unregistered semantic intent before it can proceed to resolution;
- fingerprints proposals and observed state to detect repeated proposals without state change and repeated next steps;
- enforces Reasoning deadline and step budgets through a composable guard;
- exposes bounded observation-request and result-synthesis shapes for the later orchestration loop; and
- composes the provider, guard, and Reasoning engine in the Host.

Reasoning remains proposal-only. Its contracts have no executor or authorization dependency, and all proposed action intent must still pass through registered resolution, constraints, Decision, and the Execution Gate. No LLM, plan framework, Goal loop, or autonomous execution path is introduced.

## 22. Increment 13: Goal Orchestration

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

### Implemented baseline

The initial Goal orchestration baseline now:

- obtains the authenticated principal and bounded `RuntimeBudget` from an explicit admission policy rather than manufacturing authority inside orchestration;
- assigns one `RuntimeInvocationId` per admitted Goal and preserves Goal, correlation, tenant, invocation, and step identity through the lifecycle;
- coordinates observation requests, proposal-only Reasoning, registered action resolution, semantic constraints, deterministic Decision, pre-flight diagnostics, the common Execution Gate, and audited execution;
- returns successful execution to Reasoning so it can complete or propose a different next step;
- re-enters the complete resolution, constraint, Decision, diagnostic, and gate path for every additional step;
- bounds action steps, observation-provider calls, stale-state retries, and wall-clock time;
- re-observes and revalidates after a stale gate result without reusing the stale authorization;
- preserves diagnostic `Retry`, `ReObserve`, `Escalate`, and `FailGoal` dispositions across the gate boundary and applies them through the bounded lifecycle;
- terminates explicitly for completion, abstention, escalation, failure, and cancellation; and
- refuses to override semantic or Governance denial.

The Host composes the orchestrator with deny-by-default admission and an empty observation provider. A trusted adapter must replace admission before autonomous execution is available. Effect verification remains the next increment; the current `Verifying` stage records the execution outcome for Reasoning but does not claim semantic effect verification.

## 23. Milestone C: Autonomous Execution Conformance

### Review requirement

Before effect verification broadens the operational surface, verify that PR-15 and PR-16 preserve the authority hierarchy across a bounded autonomous execution loop.

The review MUST confirm:

- admission supplies identity and budget without granting orchestration authority to invent them;
- canonical, abstaining, no-candidate, cancellation, stale-state, retry, and multi-step paths terminate deterministically;
- every action step repeats resolution, constraints, Decision, diagnostics, and gate authorization;
- diagnostic lifecycle dispositions remain actionable across the gate boundary;
- semantic and Governance denial cannot be overridden;
- only the Execution Gate creates `AuthorizedAction`;
- execution uses the audited executor boundary; and
- the Host remains deny-by-default until a trusted adapter supplies admission.

### Review outcome

Milestone C is approved in the [Milestone C Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone C Conformance Review.md>). The review closed the diagnostic-disposition propagation gap, confirmed the bounded multi-step authority chain, and admitted only opt-in deterministic effect verification for PR-17.

## 24. Increment 14: Effect Verification

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

### Implemented baseline

The initial Effect Verification baseline now:

- keeps verification contracts and evaluators separate from Diagnostics;
- evaluates only trusted `ActionDescriptor.ExpectedEffects` when the descriptor's verification profile opts in;
- provides deterministic evaluator routing by trusted effect type;
- represents per-effect and aggregate `Verified`, `PartiallyVerified`, `Unverifiable`, and `Contradicted` outcomes explicitly;
- never treats a successful executor result as effect verification;
- refreshes known observations after execution before verification;
- audits every aggregate verification result with action, authorization, execution, Goal, and Step identity;
- invokes an injected verification policy for every opt-in result; and
- escalates contradicted or materially unverifiable outcomes under the default risk-aware policy without manufacturing recovery authority.

Compensation remains a separately governed future action. PR-17 does not invoke an executor from verification, reuse Diagnostics semantics, or claim that actions without an opt-in profile were verified.

## 25. Increment 15: Replay-Capable Evidence

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

### Implemented baseline

The initial replay-capable evidence baseline now:

- appends one immutable evidence record for every autonomous action attempt that reaches resolution;
- preserves the exact Goal, budget, observations, descriptor versions, candidate set, and constraint partition seen by the Decision boundary;
- captures Decision provider identity/version and the complete validated Decision result;
- projects gate, execution, and effect-verification outcomes into evidence-only contracts;
- references an authorization by identifier without retaining an `AuthorizedAction` or creating executable authority;
- records denied, stale, confirmation-required, diagnostic-blocked, failed-execution, verified, and policy-terminal attempts before orchestration exits or retries;
- exposes a tenant-scoped in-memory reference sink for composition and conformance tests; and
- proves in a test utility that a historical `DecisionContext` can be reconstructed from the stored record.

PR-18 does not execute stored evidence, reissue authorization, route providers, or introduce a general replay engine. Durable production persistence and retention policy remain provider-specific infrastructure work.

## 26. Increment 16: Additional Decision Providers

### Objective

Add alternative intelligence behind the stable Decision contract.

Candidates include:

- LimboDancer-native local semantic choice model;
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

### PR-19 implemented baseline

The first provider experiment adds an explicitly configured `OpenAiDecisionProvider` that:

- uses the OpenAI Responses API with strict JSON Schema output, no tools, and response storage disabled;
- sees only the existing Decision context and permitted candidates;
- returns the existing validated `DecisionResult` with latency, token usage, and calculated cost evidence;
- applies a finite timeout and conservative pre-invocation token/cost eligibility checks;
- participates in cumulative per-Goal Decision token and cost accounting before any gate or execution;
- treats refusal, incomplete, malformed, transport-failed, and out-of-set output as provider failure rather than abstention;
- remains disabled unless the Host explicitly selects OpenAI and supplies a key, pinned model, endpoint, limits, and prices; and
- includes a replay-only labeled evaluation utility that cannot authorize or execute actions.

The follow-up evaluation checkpoint adds explicit ambiguity, orthogonal action-risk, and wrong-choice-severity labels plus reports for selection correctness, abstention quality, invalid-result rate, latency, tokens, cost, confidence calibration error, deterministic-baseline disagreement, and severe wrong choices. Recorded and synthetic cases prove the measurement contract but do not establish live-model quality. OpenAI adoption remains deferred until an operator-controlled run uses a representative redacted corpus, a pinned model and prices, finite budgets, and documented acceptance thresholds.

The [Decision Evaluation Corpus Specification and Runbook](<./LimboDancer.Agentic.CognitiveRuntime Decision Evaluation Corpus Specification and Runbook.md>) now governs that evidence-preparation checkpoint for every non-reference provider. It separates pipeline conformance, rule conformance, expert benchmarks, and representative historical Decision evidence; defines the manifest, independent review, leakage-controlled holdout, threshold, integrity, and operator-run requirements; and places the ASL 3.01 rulebook upstream of a reviewed, versioned ASL ontology/rule package that supplies semantic evidence and case-construction input, not historical Decision evidence or a tactical label source.

The deterministic rule provider remains the default. PR-19 does not add provider routing, fallback, retries, live-provider CI, or production-provider adoption.

### Future native local Decision model direction

The [Native Local Decision Model](<./LimboDancer.Agentic.CognitiveRuntime Native Local Decision Model.md>) records a future research direction synthesized from Jev-style open implementations. It narrows the prospective capability to bounded selection among runtime-supplied candidates plus explicit abstention and escalation, while retaining the existing `IDecisionProvider`, `DecisionResult`, Decision Plane validation, Execution Gate, and Effect Verification boundaries.

This direction is not an admitted increment. Do not implement a second non-reference provider, model service, training pipeline, router, or fallback from this note. The next eligible activity remains operator-controlled evidence preparation under the PR-19 Evaluation Review. A local-provider design slice requires a representative reviewed corpus, pinned inference identity, finite evaluation budget, repeated offline results, and documented acceptance thresholds for one bounded Decision class.

### Future Anthropic Decision provider direction

The [Anthropic Decision Provider Feasibility and Design](<./LimboDancer.Agentic.CognitiveRuntime Anthropic Decision Provider Feasibility and Design.md>) records technical due diligence for a possible remote structured-output implementation of the existing `IDecisionProvider`. Storyvizor supplies tested Anthropic SDK lifecycle experience; the official SDK supplies typed Messages, structured output, usage, stop reasons, and cancellation. LimboDancer retains canonical input, budgets, semantic validation, replay evidence, and all execution authority.

This direction is not an admitted increment. Any future slice must use one non-streaming, tool-free, stateless invocation; disable SDK retries; fail closed on refusal, truncation, unexpected stop reasons, malformed output, and unknown candidates; and preserve PR-19's deadline, token, cost, and replay-only rules. It remains blocked by the same representative-corpus and acceptance-threshold gate as every other non-reference provider.

## 27. Deferred Until Concrete Use Cases

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

### 25.1 ASL Reference-Domain Capability Horizon

ASL is the first reference domain against which the completed runtime architecture will be tested. Its requirements are defined in `docs/ASL/legacy-limbodancer-mcp-system-design.md`.

The package boundary, contract-admission rules, and interface timing are defined in `LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`. The source-to-package authoring lifecycle and first implementation slices are defined in [ASL Ontology Transformation Specification](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>).

This horizon does not expand PR-01 through PR-18. It prevents the authority substrate from becoming detached from the concrete product scenario that motivated it.

After the applicable runtime boundaries are proven, plan small evidence-driven slices for:

1. authoritative ASL source identity and provenance;
2. versioned ASL ontology and rule-package publication;
3. board, terrain, and other reference-state access;
4. dynamic game-state observations;
5. rule hierarchy, cross-reference, condition, and exception resolution;
6. registered spatial calculations such as coordinate, adjacency, distance, and line-of-sight evaluation;
7. DomainConclusion and explanation output;
8. staleness, ambiguity, and indeterminate-result handling;
9. governed ASL state mutations through the common Execution Gate;
10. end-to-end conformance tests for the reference scenarios.

The first ASL slice SHOULD be read-only adjudication. It should prove that authoritative rules, current state, exception resolution, and deterministic calculations can produce an evidence-backed conclusion without fabricating execution authority.

The admitted planning sequence is `ASL-OT-01` source registry and fragment location, `ASL-OT-02` transformation intermediate representation, `ASL-OT-03` validation and review, `ASL-OT-04` the Scenario A1 semantic package, `ASL-OT-05` immutable publication and exact resolution, and `ASL-OT-06` read-only occupied-building entry adjudication. Implementation begins only after the applicable specification gate is accepted.

ASL-OT-01 is complete and approved in the [ASL-OT-01 Source Registry Review](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL-OT-01 Source Registry Review.md>). It registers seven Markdown artifacts and 661 images, pins source and converter hashes, supplies deterministic structural fragment locators, and retains an explicitly unverified fourteen-fragment review sample.

The [ASL-OT-02 TIR Schema and Deterministic Extraction Design](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Schema and Deterministic Extraction Design.md>) and [ASL-OT-02 TIR Review](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Review.md>) now close the transformation-intermediate-representation slice. Its C# foundation defines the versioned TIR schema, ASL-owned envelope and structural artifact types, deterministic artifact identity, canonical JSON serialization, and fail-closed authority checks. TIR 1.3 and extractor 1.5 produce 105 Sections, 2,001 Rules, 4,834 cross-reference occurrences, 358 example markers, and 21 table/chart blocks. Exact UTF-8 sub-fragment spans support portable evidence and recover the required `A7.37`, `A7.8`, and `E1.93` boundaries without changing converted source or inferring semantics. All missing-parent diagnostics are resolved; 32 missing-reference diagnostics remain explicit ASL-OT-03 review inputs. All extracted artifacts remain unmodeled and unaccepted. Complete example extents and table-row reconstruction are explicitly deferred because the approved artifacts do not claim them. `utils/pdf_to_markdown.py` remains an outside conversion utility only. The next admitted slice is ASL-OT-03 validation and review workflow; ontology semantics, publication, runtime resolution, and provider evidence remain deferred.

The proposed [ASL-OT-03 Validation and Review Workflow Design](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL-OT-03 Validation and Review Workflow Design.md>) binds validation and review to an exact TIR document digest, stable artifact identity, canonical artifact digest, policy digest, and dependency closure. It separates deterministic validation, source verification, semantic authorship, domain review, adjudication, and later release approval. Captured extraction remains immutable; effective review state is projected from canonical append-only records, while formalization and review status remain independent. The design defines seven validation gates, fail-closed diagnostic disposition, source-verification evidence, legal state transitions, role separation, review bundles, C# implementation boundaries, and five incremental implementation steps. It does not admit a database, operator UI, generalized runtime validation framework, semantic package, publication, or execution authority. ASL-OT-04 remains blocked until ASL-OT-03 is implemented and reviewed.

Exact projects, persistence products, canonical serialization format, and PR numbers remain deferred until each slice supplies concrete requirements.

## 28. Test Strategy

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

### Reference-domain conformance tests

When ASL reference-domain slices begin, trace tests to `ASL-RD-*` requirements and the named acceptance scenarios.

The initial suite SHOULD prove:

- canonical rule and source provenance preservation;
- rule, condition, and exception applicability traces;
- reference-state and changing-state version capture;
- deterministic spatial-calculation evidence;
- DomainConclusion explanation and indeterminate outcomes;
- invalidation or qualification after material state changes;
- independent authorization and revalidation for requested mutations;
- absence of ASL-specific dependencies in the runtime kernel.

## 29. Legacy Source Admission Checklist

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

## 30. PR Discipline

Each PR SHOULD:

- compile independently;
- keep the new solution green;
- contain tests for its authority boundary;
- avoid unrelated legacy cleanup;
- avoid speculative project creation;
- update this plan only when implementation evidence changes sequencing;
- reference applicable `SPEC-*` requirements in the PR description.

Do not modify the `src/_Legacy/` archive in implementation PRs.

## 31. Legacy Archive Plan

`src/_Legacy/`, including `src/_Legacy/LimboDancer.MCP.sln`, is retained for archival purposes only. It is not maintained, not built by CI, and not deleted. The intended end state is a new runtime that is fully independent of it.

### Independence prerequisites

- directed MCP parity confirmed;
- autonomous target capabilities implemented;
- required data/state migrations validated;
- operator/deployment workflows replaced;
- new tests cover retained behavior;
- no deployment script requires legacy assemblies;
- no CI workflow requires legacy production projects;
- documentation points to the new runtime.

### Independence sequence

1. keep the archive frozen;
2. keep legacy projects out of active CI/deployment;
3. copy any required compatibility fixtures into the new solution;
4. run full new-solution conformance suite;
5. update repository README/architecture index.

Independence work should be performed in dedicated PRs after parity, not piecemeal during early implementation.

## 32. .NET 11 Upgrade Checkpoint

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

## 33. Milestone Summary

### Milestone 0: Build boundary

`src/LimboDancer/LimboDancer.sln` exists, builds, tests, and has no legacy production dependency.

### Milestone A: Directed authority runtime

The four compatibility MCP capabilities run entirely through the new ActionDescriptor / Diagnostics / Execution Gate / AuthorizedAction path.

### Milestone B: Autonomous selection runtime

Goal -> Observation -> Resolution -> PermittedAction -> Decision -> SelectedAction works with deterministic Decision. This milestone is approved in the [Milestone B Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone B Conformance Review.md>).

### Milestone C: Autonomous execution runtime

Goal orchestration can execute bounded multi-step work through the common gate. This milestone is approved in the [Milestone C Conformance Review](<./LimboDancer.Agentic.CognitiveRuntime Milestone C Conformance Review.md>).

### Milestone D: Intelligence evaluation

Reasoning and multiple Decision providers can be evaluated behind stable contracts.

### Milestone E: Legacy independence

The new runtime satisfies required parity and no active project, workflow, or deployment depends on the `src/_Legacy/` archive.

### Milestone F: ASL reference-domain realization

The runtime satisfies the current ASL reference-domain requirements through evidence-backed adjudication, explanation, change-sensitive conclusions, and governed state mutation without embedding ASL-specific concepts in the runtime kernel.

## 34. Recommended PR Sequence

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

--- Milestone B review ---

PR-15  Minimal Reasoning boundary
PR-16  Goal orchestration loop

--- Milestone C review ---

PR-17  Effect verification
PR-18  Replay-capable decision evidence

--- Milestone D review ---

--- provider work ---

PR-19+ Jev / LLM / other Decision providers
PR-N   Provider routing only when evidence justifies it

--- final migration ---

Dedicated legacy independence PR series (archive retained)

--- reference-domain realization ---

RD-01+ ASL slices selected from concrete acceptance scenarios
```

The PR numbering is planning guidance, not a requirement. PRs may be split further when reviewability benefits, but major authority boundaries SHOULD NOT be collapsed into one large initial rewrite.

## 35. Definition of Implementation Ready

Implementation may begin when:

- the design and specification are approved;
- this plan is approved;
- `decision-plane` remains the working architecture branch or an implementation branch is cut from it;
- the .NET 10 SDK baseline is available in local/CI environments;
- repository permissions allow creation of the new solution/projects;
- no unresolved architecture question blocks PR-01 through PR-04.

No Decision-provider choice, policy-engine choice, replay-engine design, or human-confirmation UX is required before implementation starts.

## 36. First Concrete Engineering Task

The first engineering change should be deliberately small:

```text
Create src/LimboDancer/LimboDancer.sln
Create five runtime production projects under src/LimboDancer
Create the bounded LimboDancer.AppHost outer orchestration project
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
