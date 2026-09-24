# LimboDancer.Agentic.CognitiveRuntime

> **LimboDancer is a domain-aware runtime that lets AI systems reason, choose, and act against real state without giving the AI direct authority over the world.**

LimboDancer.Agentic.CognitiveRuntime is a governed execution environment for machine cognition.

It sits between intelligence providers and the systems they are allowed to affect. An LLM, classifier, rules engine, or other model may propose what to do. LimboDancer determines whether that proposal maps to a real capability, whether the action is semantically valid, whether policy permits it, whether current state still supports it, and whether the result actually produced the intended effect.

The system is designed around a simple principle:

```text
Reasoning proposes.
Semantics constrains.
Governance permits.
Decision selects.
Execution acts.
State remembers.
Orchestration coordinates.
Diagnostics assure.
```

LimboDancer is not an LLM wrapper, not merely an MCP server, and not a workflow engine with a model attached. It is a runtime for converting goals into governed, auditable, state-aware action.

---

## Why LimboDancer Exists

A conventional agent loop often looks like this:

```text
prompt
  -> model
  -> tool call
  -> result
  -> prompt
```

That is easy to build, but it frequently leaves too much implicit authority with the model.

The model may end up deciding:

- what actions exist;
- what an action means;
- whether an action is allowed;
- whether current state satisfies its prerequisites;
- whether an action is safe;
- whether execution actually accomplished the intended effect.

LimboDancer separates those responsibilities.

A model can reason and propose. It does not get to define reality, policy, or execution authority.

```text
Model output != authority
```

---

## What LimboDancer Does

LimboDancer supports two primary execution modes.

### Directed execution

A caller explicitly requests a known action.

Examples:

```text
Read session history
Append a message
Query the knowledge graph
Search semantic memory
```

A protocol-specific request is mapped to a semantic action and processed through a common authority boundary:

```text
Interaction
    |
    v
Action Binding
    |
    v
ActionDescriptor
    |
    v
Argument + Semantic Constraints
    |
    v
Governance
    |
    v
Diagnostics
    |
    v
Execution Gate
    |
    v
AuthorizedAction
    |
    v
Executor
    |
    v
Audit / Verification
```

An MCP client, HTTP endpoint, CLI, UI, scheduler, or another agent can invoke the same runtime without owning execution authority.

### Autonomous goal execution

A caller supplies a desired outcome instead of a specific action.

For example:

> Reconcile this customer's current status with our records and correct anything that is inconsistent.

That becomes a **Goal**.

The runtime can then:

```text
Observe relevant state
        |
        v
Reason about what is missing
        |
        v
Resolve possible semantic actions
        |
        v
Remove invalid or forbidden actions
        |
        v
Choose among permitted actions
        |
        v
Revalidate current state
        |
        v
Execute
        |
        v
Observe the result
        |
        v
Verify expected effects
        |
        v
Continue or complete
```

This is the core agentic behavior of the runtime.

---

## Architecture

LimboDancer is organized around six logical planes plus several cross-cutting runtime fabrics.

### Interaction Plane

Owns protocol and presentation concerns.

Examples:

- MCP;
- HTTP;
- CLI;
- Blazor;
- schedulers;
- future agent-to-agent protocols.

Interaction translates external requests into runtime requests. It does not define semantic authority.

### Reasoning Plane

Owns open-ended interpretation and planning.

It may:

- interpret Goals;
- decompose work;
- identify missing observations;
- propose plans;
- revise plans;
- synthesize results.

Reasoning does not execute actions directly.

### Semantic Plane

Owns domain meaning.

It defines:

- entities;
- properties;
- relations;
- aliases;
- semantic actions;
- action applicability;
- authoritative preconditions;
- expected effects;
- ontology mappings.

The Semantic Plane answers a critical question:

> What actions are actually meaningful in this domain?

### Decision Plane

Selects among explicit, permitted alternatives.

A Decision provider may:

- select;
- abstain;
- escalate.

It cannot create new capabilities, override failed preconditions, grant permissions, or execute.

### Execution Plane

Owns final authorization and operational action.

Only an `AuthorizedAction` may produce consequential execution.

The Execution Gate revalidates the action immediately before execution.

### State Plane

Provides persistent and observed state.

Current and anticipated State mechanisms include:

- relational data;
- graph data;
- vector retrieval;
- ontology state;
- execution results;
- external-system observations.

State is evidence. It is not authority.

---

## Cross-Cutting Fabrics

### Governance

Governance determines what is permitted.

It includes concerns such as:

- tenant isolation;
- permissions;
- policy;
- risk;
- confirmation requirements;
- execution budgets;
- environment restrictions.

Governance is authoritative. Models are not.

### Orchestration

Orchestration coordinates runtime progression.

It moves Goals through:

```text
Observe
-> Reason
-> Resolve
-> Constrain
-> Decide
-> Gate
-> Execute
-> Verify
-> Continue or Complete
```

Orchestration coordinates authority boundaries but does not replace them.

### Diagnostics

Diagnostics explicitly evaluates whether the runtime is healthy, coherent, correctly configured, and behaving within expected bounds.

Examples:

- Is tenant context present?
- Does this action have a registered executor?
- Does the ontology property map correctly?
- Did the Decision provider select a candidate that actually existed?
- Is execution being duplicated?
- Are we repeating the same reasoning step without state change?
- Did a required invariant become indeterminate?

Diagnostics is not merely logging. It evaluates runtime expectations and invariants.

---

## The Ontology Is Central

LimboDancer is ontology-constrained.

Actions are not arbitrary functions. They are semantic capabilities with domain meaning.

Examples:

```text
ldm:action/HistoryRead
ldm:action/HistoryAppend
ldm:action/GraphQuery
ldm:action/MemorySearch
```

The ontology can describe:

- what entities exist;
- what properties they have;
- what relationships are meaningful;
- what actions exist;
- what preconditions an action requires;
- what effects an action is expected to produce.

This lets the runtime answer questions such as:

```text
Is this action meaningful for this entity?

Does this requested property exist in the ontology?

Is this relationship valid?

What registered actions could advance this Goal?

What effects should this action produce?
```

The Semantic Plane constrains the possible action universe before probabilistic systems are allowed to choose among alternatives.

---

## Authority Narrowing

LimboDancer makes execution authority explicit.

The autonomous authority path is:

```text
Goal
  |
  v
ActionCandidate
  |
  v
PermittedAction
  |
  v
SelectedAction
  |
  v
AuthorizedAction
  |
  v
ExecutedAction
```

Each stage narrows authority.

### ActionCandidate

A possible action grounded in a runtime context.

It is not yet permitted.

### PermittedAction

A candidate that passed the deterministic semantic and governance constraints required before autonomous selection.

### SelectedAction

An action selected explicitly by a directed caller or by a Decision provider.

Selection is not final authorization.

### AuthorizedAction

A SelectedAction that has passed the final Execution Gate.

Only this form may reach an executor.

---

## Models Are Replaceable Advisors

LimboDancer is deliberately model-neutral.

Different providers can serve different cognitive roles:

```text
LLM
Jev / System One
Rules
Local classifier
Small language model
Specialized model
Human decision
Composite strategy
```

A runtime might use:

```text
LLM
    for interpretation and planning

Jev
    for bounded action selection

Rules
    for deterministic safety constraints
```

Reasoning providers and Decision providers are interchangeable behind runtime contracts.

Executors do not need to change when intelligence providers change.

---

## Stale-State Protection

Agentic systems act against mutable state.

Suppose the runtime observes:

```text
Account balance: $1,000
Version: 42
```

A model reasons about that observation.

Before execution, another system changes the account:

```text
Account balance: $200
Version: 43
```

The Execution Gate can detect that the selected action was based on stale state.

Instead of blindly executing:

```text
SelectedAction
    |
    v
Stale
    |
    v
Re-observe
    |
    v
Re-reason / reselect
```

This protects the runtime from time-of-check/time-of-use failures.

---

## Execution Success Is Not Semantic Success

A successful API call does not necessarily mean the intended result occurred.

LimboDancer distinguishes:

```text
Execution succeeded
```

from:

```text
Expected semantic effect occurred
```

For example:

```text
Expected effect:
reservation.status == Confirmed
```

The remote API might return success while the reservation remains `Pending`.

LimboDancer can represent outcomes such as:

```text
Verified
PartiallyVerified
Unverifiable
Contradicted
```

That allows the runtime to escalate, recover, retry where safe, or initiate a separately governed compensation action.

---

## Multi-Tenant by Design

Tenant isolation is a runtime invariant, not a database convention.

Tenant identity is established during admission and must remain structurally enforced through:

```text
Goal
Observations
Action resolution
Decision
Execution
Audit
State access
```

Cross-tenant reads and writes must fail closed.

This is particularly important in agentic systems, where probabilistic reasoning must never be allowed to widen a tenant boundary.

---

## Interaction Is Replaceable

MCP is one adapter into the runtime, not the product identity.

The same runtime can be exposed through:

```text
MCP
HTTP
CLI
Blazor
Scheduled jobs
Background workers
Other agents
Future A2A protocols
Internal workflows
```

All interaction surfaces converge on the same semantic and execution-authority model.

---

## Current .NET Architecture

The new runtime uses **.NET 10** and the `LimboDancer` root namespace.

The initial production solution is intentionally small:

```text
src/LimboDancer/
  LimboDancer.sln

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

The logical planes are primarily represented through namespaces rather than one project per plane.

Examples:

```text
LimboDancer.Runtime.Semantics
LimboDancer.Runtime.Diagnostics
LimboDancer.Runtime.Decision
LimboDancer.Runtime.Execution
LimboDancer.Runtime.Orchestration
LimboDancer.Runtime.Observations

LimboDancer.Infrastructure.Relational
LimboDancer.Infrastructure.Graph
LimboDancer.Infrastructure.Vector
LimboDancer.Infrastructure.Ontology
```

---

## Legacy Implementation

The previous `LimboDancer.MCP.*` implementation has been isolated under:

```text
src/Legacy/
```

including its legacy solution:

```text
src/Legacy/LimboDancer.MCP.sln
```

Legacy code is behavioral reference only.

New production projects must not reference legacy projects or assemblies.

The migration model is:

```text
inspect legacy behavior
    |
    v
copy or reimplement only what is still useful
    |
    v
clean / harden / retest
    |
    v
admit into the new architecture
    |
    v
prove parity and conformance
    |
    v
delete src/Legacy/
```

The intended end state is complete removal of the `src/Legacy/` directory.

---

## Initial Runtime Capabilities

The first implementation slice focuses on directed execution through a common authority boundary.

Initial semantic actions are expected to include:

```text
ldm:action/HistoryRead
ldm:action/HistoryAppend
ldm:action/GraphQuery
ldm:action/MemorySearch
```

The first end-to-end runtime target is:

```text
MCP request
-> Action binding
-> ActionDescriptor
-> deterministic constraints
-> diagnostics
-> Execution Gate
-> AuthorizedAction
-> executor
-> audit
```

Autonomous Goal execution is added only after this path is proven.

---

## Intended Use Cases

LimboDancer is designed as a reusable runtime beneath intelligent applications such as:

- enterprise agents;
- knowledge assistants;
- operations automation;
- workflow agents;
- data reconciliation agents;
- regulatory and policy assistants;
- semantic-search systems;
- autonomous support agents;
- DevOps agents;
- research agents;
- simulation systems;
- game and world agents.

The domain changes primarily through:

- ontology;
- semantic actions;
- Governance policy;
- State adapters;
- executors;
- Reasoning providers;
- Decision providers.

The cognitive runtime remains largely the same.

---

## What LimboDancer Is Not

LimboDancer is not primarily:

```text
an MCP server
an LLM wrapper
a chatbot
a vector database abstraction
a workflow engine
a policy engine
a planner
an agent framework
```

It incorporates or integrates capabilities associated with several of those categories, but none of them defines the system.

A more accurate description is:

> **LimboDancer is an execution environment for governed machine cognition.**

And the repository's core definition is:

> **LimboDancer is a domain-aware runtime that lets AI systems reason, choose, and act against real state without giving the AI direct authority over the world.**

---

## Original Product Goal and Reference Domain

The cognitive runtime exists to serve a concrete product goal: turn complex authoritative material into operational domain knowledge that can be interpreted against current state to produce explainable conclusions and safely governed actions.

Advanced Squad Leader (ASL) is the first reference domain and architectural fitness test. It requires the runtime to reconcile canonical rules, hierarchies, cross-references, nested exceptions, tables, spatial relationships, reference maps, and changing game state.

The target outcome is broader than retrieval:

```text
authoritative rules and reference material
-> validated semantic model
-> current observations
-> rule, exception, and domain calculation
-> evidence-backed DomainConclusion
   or
-> governed AuthorizedAction
```

A DomainConclusion explains what the modeled rules and evidence imply. It is not permission to mutate state. Any requested mutation independently passes through the runtime authority model and Execution Gate.

The ASL requirements do not dictate the runtime's project layout, storage products, protocols, or implementation sequence. They define concrete capabilities the reusable architecture must ultimately support without embedding ASL-specific concepts in the runtime kernel.

See `docs/ASL/legacy-limbodancer-mcp-system-design.md` for the current ASL reference-domain requirements, acceptance scenarios, and preserved historical design. The ontology authoring lifecycle is defined in `docs/ASL/LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md`.

---

## Design Documentation

The active architecture and implementation documents are under `src/docs/`.

Key documents include:

- `LimboDancer.Agentic.CognitiveRuntime Plane Architecture Analysis.md`
- `LimboDancer.Agentic.CognitiveRuntime Plane Architecture Codebase Validation.md`
- `LimboDancer.Agentic.CognitiveRuntime Runtime Orchestration Model.md`
- `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design.md`
- `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification.md`
- `LimboDancer.Agentic.CognitiveRuntime Implementation Plan.md`
- `LimboDancer.Agentic.CognitiveRuntime Domain Knowledge Modeling Requirements.md`
- `LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`
- `LimboDancer.Agentic.CognitiveRuntime Native Local Decision Model.md`
- `LimboDancer.Agentic.CognitiveRuntime Anthropic Decision Provider Feasibility and Design.md`
- `LimboDancer.Agentic.CognitiveRuntime Decision Evaluation Corpus Specification and Runbook.md`
- `Decision Plane Architecture.md`
- `docs/ASL/LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md` — rulebook-to-ontology authoring, validation, publication, and first adjudication slice
- `docs/ASL/legacy-limbodancer-mcp-system-design.md` — ASL reference-domain requirements and historical design source

The **Plane Runtime Specification** is normative for implementation. The **Implementation Plan** defines the current engineering sequence. The **Domain Knowledge Modeling Requirements** document provides supporting guidance for semantic and state representations. The **Domain Integration Model** defines how separate domain packages depend on and compose with the runtime and when their shared contracts may be introduced. The **ASL Ontology Transformation Specification** defines the separate authoring lifecycle that converts registered rulebook sources into a reviewed, immutable domain package. ASL-OT-01 now provides the reproducible source registry and structural fragment locator while keeping every representative fragment unverified until authoritative-edition review.

Domain and use-case reference material lives under `docs/`. Advanced Squad Leader material is consolidated under `docs/ASL/`. Its requirements are authoritative for reference-domain capability but do not override the runtime architecture or its authority semantics.

---

## Current Engineering Direction

The implementation strategy is intentionally incremental:

```text
Build boundary
    |
    v
Action authority
    |
    v
Diagnostics
    |
    v
Execution Gate
    |
    v
Audit
    |
    v
Tenant-safe State infrastructure
    |
    v
Directed actions
    |
    v
MCP adapter + Host
    |
    v
Autonomous contracts
    |
    v
Observation + Semantic resolution
    |
    v
Decision
    |
    v
Reasoning
    |
    v
Goal orchestration
    |
    v
Provider evaluation
    |
    v
Legacy retirement
```

The architecture deliberately establishes authority and runtime invariants before expanding cognitive capability.
