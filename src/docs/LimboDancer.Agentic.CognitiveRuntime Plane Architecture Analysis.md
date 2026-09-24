# LimboDancer.Agentic.CognitiveRuntime Plane Architecture Analysis

**Status:** Architectural analysis  
**Branch:** `decision-plane`  
**Purpose:** Recast the existing and emerging LimboDancer.Agentic.CognitiveRuntime architecture as a set of explicit planes before modifying the primary architecture specification.


**Canonical system identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**Legacy implementation namespace:** Existing `LimboDancer.MCP.*` assemblies retain their current names until an explicit code migration is performed.
**Legacy archive root:** `src/_Legacy/` (archival only; retained, not built, not referenced)  

## 1. Executive Summary

LimboDancer.Agentic.CognitiveRuntime has evolved beyond the boundaries implied by its original description as a .NET MCP server backed by relational history, vector memory, a knowledge graph, and an ontology.

Those technologies remain important, but they no longer provide the clearest description of the system.

The architecture can be understood more coherently as a set of logical planes:

1. **Interaction Plane**
2. **Reasoning Plane**
3. **Semantic Plane**
4. **Decision Plane**
5. **Execution Plane**
6. **State Plane**

A seventh concern, **Governance and Control**, should not be modeled as a peer plane. It is a cross-cutting control fabric that establishes invariants across every plane.

This reframing is not primarily a proposal to replace the existing architecture. Most of the existing system already maps naturally into these planes. The major missing capability is the Decision Plane, while the Reasoning Plane remains comparatively immature. The Semantic, Execution, State, Interaction, and Governance capabilities are already substantially represented in the repository.

The plane model therefore provides a way to describe what LimboDancer has already become, identify where responsibilities are misplaced, and establish cleaner boundaries for future implementation.

A concise description of the target architecture is:

> **LimboDancer is an ontology-constrained cognitive runtime in which interaction supplies goals, reasoning interprets them, semantics determines possible actions, governance determines permissible actions, decision selects among those actions, execution changes or observes the world, and persistent state supplies subsequent observations.**

MCP remains important, but it becomes one protocol and execution adapter within this larger architecture rather than the definition of the architecture itself.

---

## 2. Why Recast the Architecture?

The current repository is organized primarily around technical subsystems and deployable projects:

- Core;
- Storage;
- Azure AI Search vector memory;
- Cosmos Gremlin graph;
- Ontology;
- MCP server;
- HTTP server;
- CLI;
- Blazor operator console;
- LLM/intelligence seam.

That organization is appropriate for source code.

It is less effective as the conceptual model of the system.

A component view answers:

> What software components exist?

A plane view answers:

> What kind of responsibility is being exercised?

That distinction becomes increasingly important as agent behavior emerges.

For example, an MCP tool can currently contain semantic validation, graph access, application behavior, and execution concerns. Those responsibilities may occupy several conceptual planes even though they happen to reside in one C# class.

The plane model lets implementation boundaries evolve without losing the conceptual architecture.

---

## 3. Architectural Thesis

The proposed model separates six questions.

| Plane | Primary question |
|---|---|
| Interaction | How does an actor communicate with LimboDancer? |
| Reasoning | What are we trying to accomplish? |
| Semantic | What does the current world mean, and what actions are possible? |
| Decision | Which permissible action should be selected? |
| Execution | How is the selected action performed? |
| State | What does the system know, remember, and persist? |

Governance adds another question that applies everywhere:

> Under what identity, tenant, authorization, policy, risk, and audit constraints may this processing occur?

The planes are logical responsibility boundaries. They do not necessarily correspond one-to-one with projects, processes, deployment units, or network boundaries.

---

## 4. Interaction Plane

### 4.1 Purpose

The Interaction Plane connects humans, agents, applications, and operators to the LimboDancer runtime.

It is responsible for protocols, transport, request admission, streaming, presentation, and interaction lifecycle.

### 4.2 Existing capabilities

Current LimboDancer capabilities that belong primarily here include:

- MCP protocol endpoints;
- HTTP APIs;
- SSE streaming;
- CLI;
- Blazor Operator Console;
- request/response serialization;
- protocol-specific authentication integration;
- health/readiness surfaces where appropriate.

Conceptually:

```text
                     INTERACTION PLANE

       +-------------+-------------+-------------+
       |             |             |             |
      MCP          HTTP/SSE       CLI       Operator UI
       |             |             |             |
       +-------------+-------------+-------------+
                             |
                       Runtime Boundary
```

### 4.3 Boundary

The Interaction Plane should understand how to receive a request and how to return a result.

It should not be responsible for determining the semantic meaning of an action or choosing which action the agent should perform.

This gives us several useful invariants:

```text
MCP != Agent
HTTP != Agent
CLI != Agent
Blazor != Agent
```

They are interfaces to the runtime.

### 4.4 MCP placement

MCP currently appears in more than one architectural role.

The plane model clarifies this.

MCP protocol handling belongs to the Interaction Plane.

MCP tool invocation is an adapter into the Execution Plane.

The semantic definition of an action belongs to neither. It belongs to the Semantic Plane.

This separation prevents the domain action model from becoming synonymous with MCP.

---

## 5. Reasoning Plane

### 5.1 Purpose

The Reasoning Plane performs open-ended cognition.

It interprets goals, decomposes problems, constructs or revises plans, synthesizes information, and determines what information is missing.

### 5.2 Responsibilities

Potential responsibilities include:

- natural-language interpretation;
- goal extraction;
- problem decomposition;
- planning;
- plan revision;
- synthesis;
- explanation;
- ambiguity detection;
- recovery strategy;
- deciding when additional observations are required.

### 5.3 Current state

The repository contains planner concepts and an LLM-oriented project seam, but reasoning is not yet a mature centralized runtime capability.

This is advantageous because its contract can still be defined without substantial provider-specific implementation debt.

### 5.4 Boundary

The Reasoning Plane may determine:

> We need to determine the current session history and then inspect the related semantic entity.

It should not possess unilateral authority to execute arbitrary operations.

Instead, reasoning produces goals, subgoals, plans, or semantic intents that are resolved by lower planes.

### 5.5 Open-ended versus bounded intelligence

Reasoning has a potentially enormous output space.

That distinguishes it from the Decision Plane, whose job is intentionally bounded.

This separation permits different technologies to be optimized for different cognitive tasks.

---

## 6. Semantic Plane

### 6.1 Purpose

The Semantic Plane defines what the domain means.

It describes entities, relationships, properties, actions, applicability, preconditions, effects, aliases, and mappings between semantic vocabulary and physical implementations.

This plane is likely to become the most distinctive architectural feature of LimboDancer.

### 6.2 Existing capabilities

Existing components that map naturally here include:

- ontology runtime;
- ontology definitions;
- ontology generator;
- JSON-LD contexts;
- entity definitions;
- relationship definitions;
- property mappings;
- `IPropertyKeyMapper`;
- graph semantic mappings;
- precondition definitions;
- effect definitions;
- semantic validation.

### 6.3 From descriptive ontology to operational semantics

The ontology should not remain merely descriptive metadata.

In the target architecture it participates in runtime action resolution.

For a given context, the Semantic Plane should be able to help answer:

- What entities are involved?
- What relationships exist?
- What actions apply to this type of entity?
- What arguments does an action require?
- What semantic preconditions apply?
- What effects are expected?
- What executor is bound to the action?
- What risk metadata applies?
- What state must be observed before a decision can be made?

### 6.4 Action-space reduction

The Semantic Plane transforms an open-ended intent into a finite action universe.

```text
open-ended goal
      |
      v
semantic interpretation
      |
      v
known action universe
      |
      v
context-applicable actions
      |
      v
grounded action candidates
```

This is one of the central advantages of ontology-first agent design.

The probabilistic model does not need to invent the action universe from prompt text.

---

## 7. Governance and Control Fabric

Governance should not be modeled as an ordinary peer plane.

Identity, tenancy, authorization, policy, risk, audit, secrets, quotas, and operational controls apply across the architecture.

They are better represented as a control fabric.

```text
+----------------------------------------------------------------+
|                  GOVERNANCE / CONTROL FABRIC                   |
| tenant | identity | authorization | policy | risk | audit      |
| telemetry | secrets | quotas | compliance | operational limits |
+----------------------------------------------------------------+
       |          |          |          |          |          |
       v          v          v          v          v          v
 Interaction  Reasoning  Semantic  Decision  Execution      State
```

### 7.1 Tenancy as a system invariant

Tenant isolation illustrates why governance cannot belong to only one plane.

Tenant context affects:

- request admission;
- reasoning context;
- ontology/state visibility;
- candidate actions;
- decision audit;
- tool execution;
- graph queries;
- vector searches;
- relational persistence.

Therefore:

> **Tenant identity is a system invariant propagated across every plane.**

The same reasoning applies to identity, authorization, and audit correlation.

### 7.2 Governance versus semantics

A useful distinction is:

- **Semantics** determines whether an action makes sense in the modeled world.
- **Governance** determines whether the action is allowed under system policy.

Some preconditions may be semantic. Others may be policy constraints.

The architecture should preserve that distinction even if a common execution gate evaluates both.

---

## 8. Decision Plane

### 8.1 Purpose

The Decision Plane chooses among explicit permissible alternatives.

It is the major missing runtime capability identified by the current architectural analysis.

### 8.2 Responsibilities

The Decision Plane may perform:

- choice;
- scoring;
- classification;
- confidence estimation;
- abstention;
- provider selection;
- fallback;
- escalation.

### 8.3 Bounded input

The Decision Plane receives a constrained candidate set.

```text
Candidate A
Candidate B
Candidate C
     |
     v
 Decision Provider
     |
  +--+-------+
  |          |
choose     abstain
  |
  v
Selected Candidate
```

It should not invent arbitrary privileged operations.

### 8.4 Authority boundary

The most important invariant is:

> **The Decision Plane does not determine what actions are permissible. It selects among actions that semantics and governance have already admitted as candidates.**

A probabilistic model therefore never becomes the authorization system.

Likewise:

> **High confidence cannot override a failed deterministic constraint.**

### 8.5 Provider independence

Potential implementations include:

- deterministic rules;
- structured-output LLMs;
- specialized decision models such as Jev;
- local classifiers;
- composite routing strategies;
- future decision-specific models.

The plane contract must remain independent of all of them.

---

## 9. Execution Plane

### 9.1 Purpose

The Execution Plane performs selected actions against application capabilities and external systems.

### 9.2 Existing capabilities

Existing components include:

- `HistoryGetTool`;
- `HistoryAppendTool`;
- `MemorySearchTool`;
- `GraphQueryTool`;
- history application services;
- vector search services;
- graph query services;
- graph effect execution;
- MCP tool registration and dispatch.

### 9.3 Semantic action versus tool

The plane model reveals an important distinction.

A semantic action should not fundamentally be defined by an MCP tool class.

For example:

```text
Semantic action:
    ldm:history/read

        |
        v

Application capability:
    IHistoryReader

        |
        v

Possible executor adapter:
    history_get MCP tool
```

This makes the action portable across transports and execution mechanisms.

A future action might execute through:

- MCP;
- an internal application service;
- a workflow;
- a message bus;
- an external API;
- a durable function.

The Semantic and Decision planes should not need to know which transport performs the work.

### 9.4 Execution gate

Before execution, a centralized deterministic gate should verify the final execution context.

It may evaluate:

- tenant;
- caller identity;
- authorization;
- capability;
- authoritative preconditions;
- risk;
- required confirmation;
- confidence policy;
- concurrency/version constraints.

Only after the gate succeeds should the executor receive the action.

---

## 10. State Plane

### 10.1 Purpose

The State Plane stores the durable and retrievable information used by the cognitive loop.

### 10.2 Existing capabilities

Current implementations include:

- PostgreSQL;
- Cosmos Gremlin;
- Azure AI Search;
- ontology persistence;
- caches and supporting stores.

### 10.3 Different forms of machine state

These stores serve different epistemic purposes.

#### Transactional state

PostgreSQL provides structured durable records such as conversation and application history.

#### Semantic/world state

The knowledge graph represents entities, relationships, and graph-addressable world state.

#### Associative state

Vector search provides similarity-oriented retrieval and semantic memory.

#### Semantic definition state

Ontology storage defines the vocabulary and formal structures through which other state is interpreted.

#### Ephemeral state

Caches, current execution context, and short-lived coordination state may also belong here even when they are not durable.

### 10.4 State is not cognition

The State Plane stores information.

It does not determine what the information means, which belongs to Semantics, or what to do about it, which belongs to Reasoning and Decision.

---

## 11. Mapping the Existing Repository to the Plane Model

The mapping is not one-to-one, and it should not be forced to become one-to-one.

A project may support several planes.

A preliminary mapping is:

| Existing area | Primary plane | Secondary concerns |
|---|---|---|
| `LimboDancer.MCP.McpServer.Http` | Interaction | Governance, Execution |
| MCP protocol endpoints | Interaction | Execution |
| CLI | Interaction | Governance |
| Blazor Console | Interaction | Governance, State observation |
| Planner concepts | Reasoning | Semantic |
| `LimboDancer.MCP.Llm` | Reasoning / future Decision | Provider integration |
| `LimboDancer.MCP.Ontology` | Semantic | State |
| Ontology Generator | Semantic | State |
| JSON-LD tool metadata | Semantic | Interaction |
| `IPropertyKeyMapper` | Semantic | Execution |
| Graph preconditions/effects | Semantic | Governance, Execution |
| Proposed Decision contracts | Decision | Governance |
| MCP tools | Execution | Interaction, Semantic |
| History services | Execution | State |
| Graph query/effect services | Execution | Semantic, State |
| Vector search services | Execution | State |
| PostgreSQL storage | State | Governance |
| Cosmos Gremlin | State | Semantic |
| Azure AI Search | State | Semantic retrieval |
| Tenant accessors | Governance | all planes |
| Authentication/authorization | Governance | Interaction, Execution |
| Audit/telemetry | Governance | all planes |

This table should be treated as an analytical map, not a mandate to reorganize projects immediately.

---

## 12. The Cognitive Control Loop

The six planes should not be understood as a simple one-way layered stack.

Agent behavior is cyclical.

```text
            +---------------+
            |   REASONING   |
            +-------+-------+
                    |
                    v
            +---------------+
            |   SEMANTICS   |
            +-------+-------+
                    |
                    v
            +---------------+
            |   DECISION    |
            +-------+-------+
                    |
                    v
            +---------------+
            |   EXECUTION   |
            +-------+-------+
                    |
                    v
            +---------------+
            |     STATE     |
            +-------+-------+
                    |
                observation
                    |
                    +--------------------> REASONING
```

Interaction injects goals and exposes results.

Governance constrains every transition.

Execution changes or observes state.

State changes produce new observations.

New observations may invalidate previous assumptions and trigger further reasoning.

The architecture is therefore better understood as a **governed cognitive control loop**.

---

## 13. Complete Conceptual Model

```text
                         HUMAN / AGENT / SYSTEM
                                  |
                                  v
                       +---------------------+
                       |  INTERACTION PLANE  |
                       | MCP / HTTP / CLI/UI |
                       +----------+----------+
                                  |
                                  v
                       +---------------------+
                       |   REASONING PLANE   |
                       | understand / plan   |
                       +----------+----------+
                                  |
                                  v
                       +---------------------+
                       |   SEMANTIC PLANE    |
                       | ontology / actions  |
                       | applicability       |
                       +----------+----------+
                                  |
                        permissible candidates
                                  |
                                  v
                       +---------------------+
                       |   DECISION PLANE    |
                       | choose / score /    |
                       | abstain / escalate  |
                       +----------+----------+
                                  |
                            selected action
                                  |
                                  v
                       +---------------------+
                       |   EXECUTION PLANE   |
                       | gate / execute /    |
                       | verify effects      |
                       +----------+----------+
                                  |
                                  v
                       +---------------------+
                       |     STATE PLANE     |
                       | SQL / KG / Vector   |
                       | Ontology / Cache    |
                       +----------+----------+
                                  |
                             observation
                                  |
                                  +-----------> Reasoning

        +=========================================================+
        |              GOVERNANCE / CONTROL FABRIC               |
        | tenant | identity | authorization | policy | risk      |
        | audit | telemetry | secrets | quotas | compliance      |
        +=========================================================+
```

---

## 14. Data Plane, Control Plane, and Cognitive Planes

There is a useful relationship between this model and conventional distributed-systems terminology.

The State and Execution planes resemble aspects of a traditional data plane: they interact with operational state and perform work.

Governance resembles a control plane.

Reasoning, Semantics, and Decision introduce something different: **cognitive planes**.

We should use this analogy cautiously.

LimboDancer's "planes" are responsibility boundaries, not necessarily independently deployed infrastructure planes.

Still, the distinction is useful:

```text
Interaction        -> ingress / egress
Reasoning          -> cognitive interpretation
Semantics          -> meaning and possibility
Decision           -> bounded choice
Execution          -> action
State              -> memory and world representation
Governance         -> constraints and control
```

---

## 15. Architectural Flow of Authority

The plane model also exposes where authority should live.

### Reasoning may propose.

Reasoning can formulate goals, plans, queries, and hypotheses.

### Semantics may constrain possibility.

The ontology determines whether concepts and actions exist and whether they apply to the current domain state.

### Governance may permit or deny.

Identity, tenant, authorization, policy, and risk controls determine whether an otherwise meaningful action is allowed.

### Decision may select.

The Decision Plane chooses among the candidates that survived those constraints.

### Execution may act.

The Execution Plane performs only the selected, gated action.

### State may record.

The State Plane persists the resulting observations and effects.

This produces a compact principle:

> **Reasoning proposes. Semantics constrains. Governance permits. Decision selects. Execution acts. State remembers.**

Interaction surrounds the cycle by supplying goals and returning observations or outcomes.

---

## 16. Implications for Preconditions and Effects

The existing precondition/effect design becomes clearer under this model.

### Semantic preconditions

These describe domain truth required for an action to make sense.

Example:

```text
Order must be in Pending state before ApproveOrder is applicable.
```

These belong primarily to the Semantic Plane.

### Governance preconditions

These describe system permission.

Example:

```text
Caller must possess order.approve capability.
```

These belong to Governance.

### Execution preconditions

These describe immediate operational validity.

Example:

```text
The entity version must still equal the version observed during decision.
```

These belong to the Execution Gate.

All three may be evaluated during a single pipeline, but their conceptual origins should remain distinguishable.

### Effects

Expected effects originate semantically but are realized and verified through Execution and State.

```text
Semantic definition
      |
 expected effect
      |
      v
Execution
      |
 actual outcome
      |
      v
State observation
      |
      v
effect verification
```

This gives effects a role in both planning and post-execution verification.

---

## 17. Implications for the Existing Tool Model

The current tools are valuable but carry more architectural responsibility than they should in the target model.

A mature runtime should move toward:

```text
Semantic Action
      |
      v
Action Binding
      |
      v
Application Capability
      |
      v
Executor Adapter
      |
      +---- MCP
      +---- internal service
      +---- workflow
      +---- external API
```

This does not require immediately rewriting existing tools.

Instead, existing tools can become the first executor adapters behind explicit action definitions.

This provides a migration path rather than a rewrite.

---

## 18. Implications for Project Structure

The plane model should not automatically result in projects named:

```text
LimboDancer.Interaction
LimboDancer.Reasoning
LimboDancer.Semantic
...
```

Logical architecture and source packaging are different concerns.

Project boundaries should be driven by:

- dependency direction;
- deployability;
- provider SDK isolation;
- testability;
- ownership;
- runtime composition.

For example, Decision abstractions might belong in a core intelligence assembly while provider implementations live in separate projects.

Likewise, ontology storage belongs physically to State but its contracts primarily serve Semantics.

The plane model should guide dependencies without forcing artificial packaging symmetry.

---

## 19. Dependency Direction

Although runtime observations cycle, compile-time dependencies should remain disciplined.

A desirable dependency shape is:

```text
Interaction
     |
     v
Application / Orchestration
     |
     +------> Reasoning contracts
     +------> Semantic contracts
     +------> Decision contracts
     +------> Execution contracts
     +------> State contracts

Provider and infrastructure implementations depend inward on contracts.
Core contracts do not depend outward on vendor SDKs.
```

No ontology or decision contract should require an OpenAI, Jev, Azure Search, Gremlin, PostgreSQL, or MCP SDK type.

This keeps the conceptual planes portable.

---

## 20. Current Architectural Gaps Revealed by the Model

Recasting the existing system as planes exposes several gaps more clearly.

### 20.1 Decision is missing as a centralized capability

Tool dispatch currently bypasses an explicit bounded decision layer.

### 20.2 Reasoning is not yet a mature runtime subsystem

This is expected at the current stage but should remain architecturally distinct from Decision.

### 20.3 Semantic policy is partly embedded in tools

Preconditions and effects should become authoritative action metadata rather than caller-defined execution policy.

### 20.4 Execution and semantic action identity are coupled

Tool names currently function as action identities in places where semantic action identifiers would be more durable.

### 20.5 Multiple composition roots may drift

The MCP server and HTTP server should eventually share a common application runtime or have an explicitly documented service boundary.

### 20.6 Governance is implemented but not yet modeled as a unified fabric

Tenant propagation already demonstrates the need for cross-plane invariants.

### 20.7 Audit must become decision-aware

Operational logs are not equivalent to a replayable decision audit.

---

## 21. What Should Not Change

The plane analysis does not imply that the existing technical choices are wrong.

The following remain compatible with the model:

- .NET as the implementation platform;
- MCP as an interaction and execution protocol;
- PostgreSQL for durable history;
- Cosmos Gremlin for graph state;
- Azure AI Search for vector memory;
- JSON-LD and ontology-driven semantics;
- Blazor for operator surfaces;
- Azure-hosted infrastructure;
- tenant-aware services;
- dependency injection;
- application services behind tools.

The proposed change is principally one of **architectural organization and responsibility**.

---

## 22. A More Precise Definition of LimboDancer

The technology-oriented description of LimboDancer might be:

> A .NET MCP server using PostgreSQL, Azure AI Search, Cosmos Gremlin, and an ontology runtime.

That remains factually useful but architecturally incomplete.

A stronger definition is:

> **LimboDancer is an ontology-constrained cognitive runtime in which interaction supplies goals, reasoning interprets and decomposes them, semantics establishes meaning and possible actions, governance determines permissible actions, a bounded Decision Plane selects among those actions, execution performs them, and persistent state provides the observations that drive the next cognitive cycle.**

This definition describes the system independently of any particular model, protocol, database, or cloud provider.

---

## 23. Architectural Principles

The plane model suggests the following principles.

1. **Protocols are interfaces, not cognition.**
2. **Reasoning and decision-making are distinct capabilities.**
3. **The ontology is operational, not merely descriptive.**
4. **Semantic actions are independent of executor protocols.**
5. **Possibility and permission are different concepts.**
6. **Probabilistic models do not grant authority.**
7. **Decision occurs over explicit bounded candidates.**
8. **Execution is gated deterministically.**
9. **Expected effects can be verified against observed state.**
10. **State stores knowledge but does not own its interpretation.**
11. **Governance spans every plane.**
12. **Tenant isolation is a system invariant.**
13. **Provider implementations remain replaceable.**
14. **Runtime feedback is cyclical even when compile-time dependencies are directional.**
15. **Architectural planes need not map one-to-one to source projects.**

---

## 24. Relationship to the Decision Plane Analysis

The existing `Decision Plane Architecture.md` should become a specialized deep-dive beneath this broader model.

This document defines:

> Where Decision belongs in LimboDancer.

The Decision Plane document defines:

> How Decision itself should work.

That relationship should eventually be reflected in the primary architecture documentation.

```text
LimboDancer Architecture
        |
        +-- Plane Architecture
        |
        +-- Interaction
        +-- Reasoning
        +-- Semantics / Ontology
        +-- Decision Plane
        +-- Execution
        +-- State
        +-- Governance
```

---

## 25. Recommended Next Analysis

Before changing production code, the plane model should be tested against the actual repository in greater detail.

The next analytical pass should answer:

- Does every major existing component have a clear primary plane?
- Which classes currently mix plane responsibilities?
- Which interfaces should move inward toward plane-neutral contracts?
- Where does runtime authority currently cross boundaries incorrectly?
- Which current dependencies violate the desired direction?
- Which capabilities are genuinely missing versus merely misplaced?
- How should the two current server hosts compose the same cognitive runtime?
- Where should orchestration itself live relative to the six planes?
- Is "Governance and Control Fabric" sufficient, or do observability and operations deserve a separate architectural view?

Only after that mapping should the primary architecture document be rewritten.

The purpose of the next stage is therefore not implementation.

It is **architectural validation against the codebase**.
