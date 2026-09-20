# LimboDancer.MCP Plane Architecture: Codebase Validation

**Status:** Codebase validation analysis  
**Branch:** `decision-plane`  
**Companion documents:** `LimboDancer.MCP Plane Architecture Analysis.md`, `Decision Plane Architecture.md`

## 1. Purpose

This document tests the proposed plane architecture against the current LimboDancer.MCP codebase.

The goal is not to redesign the repository around plane-named projects. The goal is to determine whether the conceptual model accurately describes the software that exists, where responsibilities currently cross plane boundaries, where authority is misplaced, and which capabilities are genuinely absent.

The analysis validates six logical planes:

1. Interaction
2. Reasoning
3. Semantic
4. Decision
5. Execution
6. State

Governance and Control remains a cross-cutting fabric.

## 2. Overall Finding

The plane model fits the current repository well.

It is not an artificial decomposition imposed on unrelated code. Most major components already have a clear primary plane.

The codebase is strongest in:

- Interaction;
- Semantic foundations;
- Execution;
- State;
- tenant-oriented Governance.

It is weakest in:

- centralized Reasoning;
- Decision;
- centralized orchestration between the planes;
- authoritative semantic action definitions;
- decision-aware audit and replay.

The most important structural finding is that **`LimboDancer.MCP.McpServer` currently acts as a convergence project for Interaction, Semantic, Governance, Execution, and State-adapter concerns**. This was reasonable for the prototype, but it is the primary place where plane boundaries are currently mixed.

The second major finding is that **the repository already contains several examples of governance wrappers and semantic adapters that point toward the plane model**. `TenantScopedGraphStore`, `VectorSearchService`, `IPropertyKeyMapper`, and the ontology repository contracts are examples.

The third major finding is that the Decision Plane is genuinely absent from the active runtime. It is not merely hidden under another name.

## 3. Repository-Level Mapping

### 3.1 Core

`LimboDancer.MCP.Core` contains contracts and primitives that are intended to be reusable across implementation layers.

Examples include tenant abstractions such as `ITenantAccessor`.

**Primary role:** plane-neutral contracts and Governance primitives.

This is appropriate. Core should not itself become a "plane."

The desirable direction is for stable cross-plane contracts to move inward toward Core or similarly neutral assemblies while implementation-specific contracts remain outside it.

### 3.2 Ontology

`LimboDancer.MCP.Ontology` maps strongly to the Semantic Plane.

The repository includes:

- ontology runtime types;
- entity/property/relation definitions;
- mappings;
- validation;
- repository contracts;
- persistence implementations.

`IOntologyRepository` explicitly describes itself as the authoritative persistence contract for ontology artifacts and requires `TenantScope` for operations.

This project therefore spans two conceptual responsibilities:

```text
Semantic Plane
    |
    | defines meaning
    v
Ontology contracts / runtime
    |
    | persists definitions
    v
State Plane
```

This is not inherently a problem. It demonstrates why planes should not be mapped one-to-one onto projects.

### 3.3 Storage

`LimboDancer.MCP.Storage` maps primarily to the State Plane.

Its persistence model and EF infrastructure are storage mechanisms.

Application behavior built on top of storage should remain outside this project where practical.

### 3.4 Graph.CosmosGremlin

`LimboDancer.MCP.Graph.CosmosGremlin` maps primarily to State.

It is a physical graph persistence implementation.

However, graph state is frequently consumed by the Semantic Plane. This means graph infrastructure is a State implementation serving semantic capabilities.

The distinction matters:

```text
Cosmos Gremlin != Semantic Plane

Cosmos Gremlin = graph state mechanism
Ontology/KG interpretation = Semantic Plane
```

### 3.5 Vector.AzureSearch

`LimboDancer.MCP.Vector.AzureSearch` maps primarily to State.

It implements associative/vector retrieval.

As with graph storage, semantic retrieval behavior may use this state, but the Azure Search SDK itself is infrastructure.

### 3.6 McpServer

`LimboDancer.MCP.McpServer` currently contains the greatest concentration of mixed plane responsibilities.

It includes:

- HTTP/MCP transport;
- authentication integration;
- tenancy;
- telemetry;
- tool registration;
- tool execution;
- history application service;
- vector facade;
- graph semantic precondition evaluation;
- graph effect application;
- ontology runtime composition;
- resilience;
- runtime composition.

This project is therefore currently both a host and an application integration layer.

That is the central structural pressure point revealed by the plane model.

### 3.7 McpServer.Http

`LimboDancer.MCP.McpServer.Http` is primarily Interaction, but it also contains an MVP chat orchestrator with in-memory session state.

It references `LimboDancer.MCP.McpServer` but does not compose the same ontology, graph, vector, and MCP runtime in its own `Program.cs`.

This confirms the previously identified composition-root divergence.

### 3.8 BlazorConsole

`LimboDancer.MCP.BlazorConsole` maps primarily to Interaction.

Its Graph, Memory, Sessions, Chat, and Ontology Validator pages are operator views over other planes.

The console should observe and administer capabilities, not own their semantics.

### 3.9 CLI

The CLI maps primarily to Interaction.

Individual commands invoke Execution or State-facing capabilities.

### 3.10 Llm

`LimboDancer.MCP.Llm` currently contains only a placeholder `Class1.cs` and a minimal project file.

This confirms that it is an unused architectural seam rather than an established reasoning implementation.

It should therefore not constrain the future design.

## 4. Interaction Plane Validation

The Interaction Plane is clearly present.

### Evidence

`McpController` in `Transport/HttpTransport.cs` exposes:

- MCP initialization;
- tool listing;
- tool execution;
- SSE events.

It is decorated with `[Authorize]` and resolves tenant context through `ITenantAccessor`.

The separate HTTP host exposes controllers, CORS, authentication/authorization, OpenAPI, health/readiness, and chat endpoints.

Blazor and CLI provide additional interaction surfaces.

### Finding

The conceptual Interaction Plane is strongly validated.

### Boundary issue

`McpController.ExecuteTool` calls:

```text
_mcpServer.ExecuteToolAsync(toolName, arguments, ...)
```

directly.

There is no semantic action resolution, decision step, or centralized execution gate between protocol input and tool execution.

Thus the Interaction Plane currently has a short path directly into Execution.

That path will need an orchestration boundary when agent-selected actions are introduced.

Direct explicit tool calls may still be supported, but their authority semantics should be documented separately from autonomous agent decisions.

## 5. Reasoning Plane Validation

The Reasoning Plane is primarily aspirational in active code.

### Evidence

Repository search finds planner architecture mainly in documentation rather than runtime classes.

The `LimboDancer.MCP.Llm` project is effectively empty.

The HTTP chat implementation is an MVP orchestrator, not a reasoning engine.

### Finding

The Reasoning Plane is **genuinely missing as a mature runtime capability**.

This validates keeping it distinct from Decision rather than assuming the existing LLM project already represents it.

### Implication

We can define Reasoning contracts based on architectural requirements rather than preserving accidental implementation choices.

## 6. Semantic Plane Validation

The Semantic Plane is already substantial.

### Evidence

The ontology project contains authoritative domain definitions and repository contracts.

`IPropertyKeyMapper` decouples ontology vocabulary from physical graph keys.

`GraphPreconditionsService` describes itself as evaluating ontology-bound preconditions against the tenant-scoped graph and maps ontology predicates before accessing graph properties.

`GraphEffectsService` similarly maps semantic predicates before graph mutation.

JSON-LD metadata is emitted by MCP tools.

### Finding

The Semantic Plane is strongly validated.

### Boundary issue: semantic contracts live in tool namespaces

`GraphPreconditionsService` imports precondition request and result types from `LimboDancer.MCP.McpServer.Tools`.

Likewise, several history interfaces are defined alongside tools.

This reverses the desired dependency direction.

Semantic/application services should not depend on protocol-facing tool classes for their contracts.

### Boundary issue: unknown semantic mapping behavior is inconsistent

`GraphEffectsService` skips an unknown property predicate.

`GraphPreconditionsService`, however, logs an unknown predicate and then uses the supplied predicate as a physical graph property key.

That is a semantic boundary leak.

In ontology-bound execution, unknown vocabulary should generally fail closed rather than silently becoming storage-native vocabulary.

This is a concrete example of why Semantic and Execution responsibilities need clearer separation.

## 7. Governance and Control Validation

Governance is already distributed across the repository.

### Evidence

`ITenantAccessor` is defined in Core.

`McpController` requires authorization.

`TenantScopedGraphStore` wraps graph mutation and requires a non-empty tenant before writes.

`VectorSearchService` constructs a mandatory tenant filter around Azure Search queries.

`HistoryService` resolves the tenant before writes.

`IOntologyRepository` requires explicit `TenantScope`.

Telemetry packages and dedicated telemetry folders are present.

### Finding

Governance as a cross-cutting fabric is strongly validated by the code.

Tenancy in particular already behaves like a system invariant rather than a feature of one subsystem.

### Important inconsistency: read versus write tenant enforcement

`TenantScopedGraphStore` enforces tenant identity for graph mutation methods, but its read method:

```csharp
GetVertexPropertyAsync(string localId, string propertyKey, ...)
```

delegates directly to the inner `GraphStore` without an explicit tenant argument.

Whether isolation remains safe depends on `GraphStore` implementation details.

The plane model suggests a stronger invariant: tenant scope should be explicit or structurally unavoidable for **both reads and writes**.

### Important inconsistency: history read filtering

`HistoryService.AppendMessageAsync` assigns `TenantId` on writes.

However, `HistoryService.ListAsync` visibly filters by `SessionId` and optional timestamp, not by `TenantId`.

Unless `ChatDbContext` supplies a verified global tenant query filter, this is a potential cross-tenant read boundary defect.

This should be verified before implementation work begins.

## 8. Decision Plane Validation

No centralized Decision Plane exists in active runtime code.

### Evidence

`McpServer.ExecuteToolAsync` performs direct registry dispatch.

The planner found by repository search exists in implementation documentation, not as the active execution authority.

No `IDecisionProvider`, candidate action abstraction, confidence policy, abstention model, or provider-routing runtime exists.

### Finding

The Decision Plane is **genuinely missing**, not merely misplaced.

This is the clearest validation of the Decision Plane initiative.

### Consequence

Decision contracts can be introduced without needing to preserve a legacy decision API.

## 9. Execution Plane Validation

Execution is mature enough to identify clearly.

### Evidence

The active tools include:

- history read;
- history append;
- graph query;
- memory search.

`McpServer` registers tool executors and dispatches them using scoped DI.

Application services such as `HistoryService`, `VectorSearchService`, graph query services, and graph effect services perform operational work.

### Finding

The Execution Plane exists, but tool classes currently act as both protocol adapters and application boundary objects.

### Boundary issue: interfaces defined beside executors

`IHistoryReader`, `IHistoryStore`, graph precondition contracts, and related DTOs are defined in tool-oriented source files.

This causes lower-level services to reference the tool namespace.

The desired direction is:

```text
stable application/semantic contract
          ^
          |
     tool adapter
```

rather than:

```text
tool contract
     ^
     |
application service
```

### Boundary issue: caller-controlled execution semantics

`HistoryAppendTool` accepts preconditions and effects in its input and then invokes graph precondition/effect services.

This proves the semantic execution concept, but places authoritative execution semantics too close to caller input.

Under the plane architecture, authoritative preconditions/effects should originate from trusted Semantic/Governance action definitions.

## 10. State Plane Validation

The State Plane is the most mature plane.

### Physical state mechanisms

- PostgreSQL / EF Core for history and relational persistence;
- Cosmos Gremlin for graph state;
- Azure AI Search for vector/associative state;
- ontology repositories for semantic definitions.

### Finding

The State Plane is strongly validated.

### Important distinction

The code confirms that storage technology and semantic meaning are already separable.

For example, `IPropertyKeyMapper` sits above graph storage and translates ontology terms into physical keys.

This is precisely the kind of adapter boundary the plane architecture seeks to generalize.

## 11. Orchestration: The Missing Vertical Capability

The codebase exposes an important concept not fully captured by treating planes only as horizontal layers: **orchestration**.

Orchestration is the runtime mechanism that moves work through the planes.

It is not itself a cognitive authority.

Conceptually:

```text
Interaction
    |
    v
+----------------------+
|     ORCHESTRATOR     |
| carries context and  |
| coordinates stages   |
+----------------------+
   |    |    |    |
   v    v    v    v
Reason Semantic Decision Execution
```

The orchestrator should not decide what is permissible or which action is best. Those responsibilities remain with Governance/Semantics and Decision.

Its responsibilities are sequencing and context propagation.

This distinction is important because the current `InMemoryChatOrchestrator` is named as an orchestrator but does not orchestrate the cognitive planes. It currently manages chat sessions, channels, history, cancellation, and simulated response generation.

A future application orchestrator should be a different and more central abstraction.

## 12. The Two Composition Roots

The repository currently contains two web-hosting composition roots.

### `LimboDancer.MCP.McpServer`

Its `Program.cs` composes:

- tenancy;
- ontology runtime;
- property mapping;
- storage;
- Cosmos Gremlin;
- graph preconditions/effects;
- vector search;
- history services;
- MCP tools;
- authentication;
- MCP transport.

This is currently the closest thing to the full LimboDancer runtime.

### `LimboDancer.MCP.McpServer.Http`

Its `Program.cs` composes:

- tenancy;
- storage;
- authentication;
- CORS;
- controllers;
- MVP in-memory chat orchestration;
- OpenAPI;
- health/readiness.

It references the McpServer project but does not compose the same semantic/graph/vector/tool runtime.

### Finding

This is genuine architectural drift, not merely duplication.

The plane model suggests two acceptable futures:

1. both hosts compose a common application/cognitive runtime; or
2. they become intentionally separate services with an explicit service boundary.

The current state is ambiguous between those models.

### Additional operational observation

The HTTP host's `/ready` endpoint may apply EF migrations when configured.

A readiness probe is normally observational. Mutating schema during readiness can create surprising lifecycle and multi-instance behavior.

This is not a plane-model defect, but the codebase validation surfaced it as a host responsibility issue.

## 13. Dependency Direction Findings

### 13.1 Positive examples

Core tenant abstractions are consumed outward by hosts and services.

Ontology mapping contracts are reused by graph-facing services.

Infrastructure projects are referenced by the host rather than Core depending on them.

These are good dependency directions.

### 13.2 Mixed example: McpServer as convergence project

`LimboDancer.MCP.McpServer.csproj` directly references:

- Core;
- Ontology;
- Storage;
- Vector.AzureSearch;
- Graph.CosmosGremlin.

It also references MCP, ASP.NET, Microsoft.Extensions.AI, Azure Search, telemetry, authentication, and resilience packages.

This makes the project simultaneously:

- host;
- transport;
- application layer;
- infrastructure composition root;
- semantic integration layer.

That concentration explains many of the current cross-plane dependencies.

### 13.3 Http host depends on McpServer host

`LimboDancer.MCP.McpServer.Http` references `LimboDancer.MCP.McpServer`, which is itself an SDK Web project.

A host depending on another host is a warning sign for long-term layering.

The shared runtime should eventually be extracted behind host-neutral contracts and composition extensions.

This does not need to happen immediately, but it should influence the target architecture.

## 14. Classes That Mix Plane Responsibilities

The following are the most important examples.

### `McpServer`

**Primary:** Execution orchestration  
**Also:** tool registry, DI integration, transport-facing metadata.

Future direction: tool registry/execution should sit behind application runtime contracts rather than becoming the cognitive orchestrator itself.

### `HistoryAppendTool`

**Primary:** Execution adapter  
**Also:** Semantic precondition policy and effect specification.

Future direction: receive an already-resolved/gated action or obtain authoritative action metadata server-side.

### `GraphPreconditionsService`

**Primary:** Semantic applicability evaluation  
**Also:** direct State access and tenant context.

Direct state access is reasonable for an evaluator implementation, but its contracts should not originate in the Tools namespace.

### `GraphEffectsService`

**Primary:** Execution of semantic effects  
**Also:** Semantic mapping and Governance tenant handling.

Future direction: separate effect description from effect execution; preserve semantic mapping through a dedicated adapter.

### `HistoryService`

**Primary:** application capability / Execution  
**Also:** State persistence and Governance tenant handling.

This is a normal application-service composition, but its interfaces should live in a host-neutral application contract namespace.

### `InMemoryChatOrchestrator`

**Primary:** Interaction session lifecycle  
**Also:** ephemeral State and simulated response generation.

Despite its name, it is not the target cognitive orchestrator.

## 15. Authority Flow in Current Code

Current MCP execution is approximately:

```text
authenticated HTTP request
       |
       v
McpController
       |
       v
tool name supplied by caller
       |
       v
McpServer registry
       |
       v
tool.ExecuteAsync
       |
       +--> optional tool-local preconditions
       +--> application/storage operation
       +--> optional effects
```

The target authority flow is:

```text
request / goal
       |
       v
reasoning intent
       |
       v
semantic action resolution
       |
       v
governance + semantic constraints
       |
       v
permitted candidates
       |
       v
decision
       |
       v
final execution gate
       |
       v
executor
       |
       v
effect verification
```

This comparison identifies the precise architectural gap.

## 16. Missing Versus Misplaced Capabilities

### Genuinely missing

- centralized Decision Plane;
- decision provider abstraction;
- explicit action candidates;
- abstention/confidence model;
- risk-aware autonomous execution policy;
- central execution gate;
- decision audit/replay;
- mature Reasoning Plane;
- cognitive runtime orchestrator.

### Present but misplaced or over-coupled

- semantic preconditions;
- semantic effects;
- history contracts;
- graph precondition contracts;
- action identity as tool identity;
- some governance enforcement;
- orchestration terminology.

### Already well-positioned conceptually

- ontology definitions;
- property mapping;
- tenant abstraction;
- physical storage projects;
- MCP transport;
- operator UI;
- vector facade enforcing tenant filter;
- tenant-scoped graph mutation wrapper.

## 17. Security and Correctness Findings Relevant to the Plane Model

These findings should be investigated before the architecture is considered validated for autonomous execution.

### 17.1 History reads may not visibly enforce tenant

`HistoryService.ListAsync` filters by session but not visibly by tenant.

Verify whether EF global query filters guarantee isolation. If not, add explicit tenant filtering.

### 17.2 Graph reads need tenant-scope verification

`TenantScopedGraphStore` clearly guards mutations but delegates property reads without explicit tenant context.

Verify the underlying graph query implementation.

### 17.3 Unknown ontology predicates should fail closed

`GraphPreconditionsService` falls back to the raw predicate as a graph property key when mapping fails.

For ontology-constrained autonomous execution, this should not silently cross from semantic vocabulary into physical storage vocabulary.

### 17.4 Graph effect tenant handling is weaker than desired

`GraphEffectsService` logs a warning when `TenantId == Guid.Empty` and proceeds to the graph abstraction, which later may reject mutation.

Governance invariants should fail at the earliest authoritative boundary rather than rely on a downstream wrapper.

### 17.5 Direct tool execution needs an explicit trust model

Authenticated callers can request a tool by name through the MCP endpoint.

That may be correct for MCP clients, but autonomous Decision Plane execution and externally requested direct tool execution are different authority paths.

The architecture should model them separately.

## 18. Refined Plane Model

The codebase validation suggests one refinement to the original diagram.

**Orchestration should be shown as a vertical coordination mechanism, not as a seventh plane.**

Governance is also vertical, but it has authority.

Orchestration coordinates; Governance constrains.

```text
                   INTERACTION
                       |
                       v
        +--------------------------------+
        |          ORCHESTRATION         |
        | context / sequencing / retries |
        +--------------------------------+
          |          |          |
          v          v          v
      REASONING -> SEMANTIC -> DECISION
                                 |
                                 v
                              EXECUTION
                                 |
                                 v
                               STATE
                                 |
                           observation loop

+---------------------------------------------------+
|             GOVERNANCE / CONTROL FABRIC           |
| identity | tenant | authorization | policy | risk |
| audit | telemetry | secrets | quotas              |
+---------------------------------------------------+
```

This produces three different architectural concepts:

- **Planes** own kinds of capability.
- **Orchestration** coordinates movement among planes.
- **Governance** constrains movement and authority across planes.

That distinction is worth preserving.

## 19. Recommended Architectural Direction

The validation does **not** justify a large-scale project reorganization yet.

It does justify the following direction:

1. Treat the six-plane model as the conceptual architecture.
2. Treat Governance as a cross-cutting authority fabric.
3. Add Orchestration as a cross-plane coordination concept.
4. Keep physical storage projects as infrastructure implementations.
5. Move stable semantic/application contracts away from tool namespaces.
6. Introduce semantic action identity distinct from MCP tool identity.
7. Introduce the Decision Plane behind provider-neutral contracts.
8. Centralize the final execution gate.
9. Make tenant isolation structurally unavoidable for reads and writes.
10. Converge the two hosts on a common runtime or explicitly separate them as services.
11. Keep provider SDK dependencies outside core contracts.
12. Add decision-aware audit/replay before autonomous decision routing becomes consequential.

## 20. Validation Verdict

The proposed plane architecture survives contact with the codebase.

More importantly, it explains several existing architectural tensions better than the current subsystem-oriented description:

- why ontology mappings should sit above graph storage;
- why tenant context must cross every subsystem;
- why tools should not define semantic authority;
- why MCP should not define the action ontology;
- why reasoning and decision should be separated;
- why the current chat orchestrator is not the future cognitive orchestrator;
- why the two server hosts need a common runtime boundary;
- why Decision is a genuinely missing capability.

The architecture can therefore move from exploratory hypothesis to a **working conceptual model**, subject to continued refinement.

The strongest compact formulation remains:

> **Reasoning proposes. Semantics constrains. Governance permits. Decision selects. Execution acts. State remembers. Interaction connects. Orchestration coordinates.**

## 21. Next Analysis

Before rewriting the canonical architecture document, one additional analysis would provide high value:

**Define the runtime orchestration model.**

That analysis should answer:

- What object carries context across the planes?
- What is the lifecycle of a goal?
- What is the difference between a direct MCP tool invocation and an autonomous agent action?
- Where does planning stop and action resolution begin?
- When are semantic and governance constraints evaluated?
- When may Decision abstain or escalate?
- Where is the final execution gate?
- How are observations returned to Reasoning?
- How are multi-step plans represented?
- How are concurrent state changes detected between decision and execution?
- What is persisted for audit and replay?
- How do HTTP, MCP, CLI, and future interfaces enter the same runtime?

That orchestration model should be established before defining the first Decision Plane interfaces, because it determines the context those interfaces must carry.
