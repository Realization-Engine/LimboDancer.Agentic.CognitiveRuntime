# LimboDancer.Agentic.CognitiveRuntime Decision Plane Architecture

**Status:** Architectural analysis and implementation direction  
**Branch:** `decision-plane`  
**Scope:** Decision architecture, semantic action resolution, execution gating, provider abstraction, audit, and evaluation


**Canonical system identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**Legacy implementation namespace:** Existing `LimboDancer.MCP.*` assemblies retain their current names until an explicit code migration is performed.
**Legacy source root:** `src/Legacy/` (temporary; delete after required legacy behavior is ported)  

## 1. Executive Summary

LimboDancer.Agentic.CognitiveRuntime already contains the foundations of an ontology-first agent platform: durable history, vector memory, a knowledge graph, ontology runtime services, tenant isolation, MCP tools, and graph precondition/effect services. The missing architectural layer is the mechanism that turns an agent goal into a constrained, explainable, auditable choice of action.

Today, the MCP runtime is fundamentally a direct dispatcher. A tool name is supplied, the corresponding tool is resolved, and the tool executes. Some semantic controls exist inside individual tools, but there is no centralized runtime responsible for discovering valid actions, applying authoritative constraints, choosing among allowed actions, handling uncertainty, and recording why the action was selected.

This document proposes a **Decision Plane**.

The Decision Plane separates open-ended reasoning from bounded decision-making. It does not decide what is legal or possible. The ontology, policy, authorization rules, and deterministic preconditions establish that boundary first. The Decision Plane then chooses among the actions that remain.

The core architectural principle is:

> **Ontology defines possibility. Policy defines permission. The Decision Plane expresses preference among permitted alternatives. The Execution Plane causes change. The Reasoning Plane interprets goals and synthesizes plans.**

This separation makes the system safer, more testable, more observable, and less dependent on any single model provider.

The Decision Plane will therefore be provider-independent. Deterministic rules, conventional LLMs, specialized decision models such as Jev, local classifiers, or future decision systems can implement the same contract and can be benchmarked against the same corpus.

A probabilistic model must never become an authorization mechanism.

## 2. Problem Statement

An agentic system must answer several different questions:

1. What is the user trying to accomplish?
2. What actions exist in the current semantic context?
3. Which actions are applicable to the current state?
4. Which actions is this caller permitted to perform?
5. Which of the remaining actions best advances the goal?
6. Is the decision sufficiently certain for the risk of the action?
7. How is the selected action executed?
8. Did execution produce the expected effects?

These are not the same problem.

Collapsing them into one LLM prompt produces an architecture in which reasoning, authorization, selection, and execution are implicitly mixed. That makes behavior difficult to test and creates a dangerous ambiguity: a model may appear to have authority merely because it generated a plausible tool call.

LimboDancer's ontology-first architecture gives us an opportunity to avoid that design.

The ontology can constrain the action space before probabilistic selection occurs. Deterministic policy and precondition evaluation can constrain it further. Only then does a decision model receive the remaining alternatives.

The result is **constrained agency** rather than unconstrained tool selection.

## 3. Current Runtime

The current `LimboDancer.MCP.McpServer` runtime registers MCP tools and dispatches execution by tool name.

Conceptually:

```text
tool name
   |
   v
tool registry
   |
   v
DI scope
   |
   v
tool.ExecuteAsync(...)
```

The current tool set includes history retrieval, history append, graph query, and memory search.

Important capabilities already exist around this execution path:

- tenant-aware services;
- ontology runtime registration;
- property and relationship mapping;
- graph precondition services;
- graph effect services;
- PostgreSQL history;
- Azure AI Search memory;
- Cosmos Gremlin graph state;
- JSON-LD semantic metadata in MCP tool schemas.

These components are valuable, but they do not yet form a centralized decision pipeline.

### 3.1 Preconditions and effects are currently too close to tools

`HistoryAppendTool` demonstrates the intended semantics by checking graph preconditions before mutation and applying graph effects afterward.

However, the request can supply its own preconditions and effects.

That is useful as a prototype of semantic execution, but it is not the desired authority model.

Authoritative preconditions and effects should be derived from trusted server-side action definitions, ontology bindings, policy, and current state. A caller may provide intent and arguments. A caller must not define the rules under which its requested operation becomes permissible.

### 3.2 Semantic mapping is already emerging

`GraphQueryTool` uses `IPropertyKeyMapper` to translate ontology vocabulary into graph-native vocabulary.

This is an important architectural pattern. The agent should reason in semantic concepts rather than storage-specific property keys and edge labels.

The Decision Plane extends this principle from **semantic data access** to **semantic action selection**.

### 3.3 The documented planner is not yet the runtime authority

Existing design material describes planning and precondition gating, but the active MCP execution path remains direct dispatch.

The Decision Plane work should therefore be understood as completion of the ontology-first runtime rather than replacement of the existing architecture.

## 4. The Missing Decision Layer

The system needs an explicit layer between reasoning and execution.

That layer must answer a bounded question:

> Given this goal, current semantic state, caller context, and set of permitted candidate actions, which action should be selected?

The distinction between **candidate generation** and **candidate selection** is fundamental.

A decision model should not invent the universe of possible operations. It should receive a constrained action set produced by the system.

This changes the architecture from:

```text
LLM -> tool call -> execute
```

to:

```text
goal
  |
  v
reasoning
  |
  v
semantic action resolution
  |
  v
policy + preconditions
  |
  v
permitted candidates
  |
  v
decision
  |
  v
risk/confidence gate
  |
  v
execution
  |
  v
effect verification
```

## 5. Reasoning Is Not Decision-Making

Reasoning and decision-making overlap conceptually, but they have different engineering properties.

### Reasoning

Reasoning is open-ended.

It may:

- interpret natural language;
- decompose goals;
- synthesize plans;
- explain state;
- identify missing information;
- formulate queries;
- generate candidate strategies;
- revise a plan after an observation.

Its output space is potentially enormous.

### Decision-making

Decision-making is bounded.

It operates over explicit alternatives and should return structured output such as:

- a selected candidate;
- a probability or confidence distribution;
- an abstention;
- a score;
- a Boolean decision;
- supporting decision metadata.

This boundedness is valuable because it makes decisions measurable.

We can determine whether the selected candidate was correct, whether confidence was calibrated, how long the decision took, how much it cost, and whether another provider would have performed better.

## 6. Five-Plane Architecture

The target architecture separates five logical planes.

### 6.1 Reasoning Plane

The Reasoning Plane interprets goals and constructs plans.

Responsibilities include:

- natural-language interpretation;
- goal decomposition;
- planning;
- synthesis;
- explanation;
- recovery strategy;
- requesting additional information.

An LLM is a natural implementation technology here, but the architecture should expose reasoning through contracts rather than directly coupling orchestration to a model vendor.

### 6.2 Semantic Plane

The Semantic Plane determines what concepts and actions mean in the current domain.

It includes:

- ontology definitions;
- entity and relation semantics;
- action definitions;
- aliases;
- semantic mappings;
- graph state;
- action applicability;
- precondition definitions;
- effect definitions.

The Semantic Plane reduces an open-ended goal to a finite set of meaningful actions.

### 6.3 Decision Plane

The Decision Plane chooses among valid alternatives.

Responsibilities include:

- choice;
- ranking internally when required to make a choice;
- scoring;
- classification;
- confidence estimation;
- abstention;
- provider routing;
- escalation.

The Decision Plane does **not** grant permission.

### 6.4 Execution Plane

The Execution Plane causes actions to occur.

It includes:

- MCP tools;
- application services;
- external APIs;
- commands;
- database mutations;
- workflow invocation.

Execution is deterministic with respect to the selected action and validated arguments wherever practical.

### 6.5 State Plane

The State Plane stores and retrieves the world state used by the other planes.

Current LimboDancer components include:

- PostgreSQL history;
- Cosmos Gremlin knowledge graph;
- Azure AI Search vector memory;
- ontology storage;
- cache and supporting infrastructure.

## 7. The Role of the Ontology

The ontology is not merely descriptive metadata.

In the target architecture it participates directly in action resolution.

For a given semantic context, the ontology should help answer:

- What actions apply to this entity or state?
- What inputs does each action require?
- What preconditions must hold?
- What effects should result?
- What risk classification applies?
- What semantic concepts does the action operate on?

This produces a crucial transformation:

```text
unbounded model output
        |
        v
semantic action universe
        |
        v
context-applicable actions
        |
        v
permitted actions
        |
        v
decision candidates
```

The model no longer needs to infer the entire action universe from prompt text.

## 8. From Tool Selection to Constrained Action Selection

MCP tools are an execution mechanism. They should not be the highest-level semantic abstraction used by the Decision Plane.

A semantic action may map to:

- one MCP tool;
- one tool with specific argument bindings;
- a sequence of tools;
- an application service;
- a workflow;
- a purely internal state transition.

Therefore the Decision Plane should select an `ActionDescriptor`, not an arbitrary tool name.

The descriptor can subsequently resolve to an executor.

This prevents the domain model from becoming coupled to the transport or protocol used to execute it.

## 9. Decision Plane Responsibilities

The Decision Plane should:

- accept an explicit candidate set;
- choose among candidates;
- provide confidence information where the provider supports it;
- support abstention;
- expose provider identity and version;
- support deterministic and probabilistic implementations;
- support fallback and escalation;
- emit sufficient metadata for audit and evaluation;
- remain independent of MCP transport;
- remain independent of a specific model vendor.

## 10. Decision Plane Non-Responsibilities

The Decision Plane must not:

- determine authentication;
- grant authorization;
- override tenant boundaries;
- override failed preconditions;
- invent privileged actions;
- directly mutate application state;
- redefine ontology semantics;
- execute arbitrary generated code;
- treat model confidence as permission.

The following invariant must hold:

> **A high-confidence decision cannot override a failed deterministic constraint.**

## 11. Core Abstractions

The exact contracts should be refined during implementation, but the architecture should converge on abstractions similar to the following.

### 11.1 ActionDescriptor

```csharp
public sealed record ActionDescriptor(
    string Id,
    string Name,
    string Description,
    ActionRisk Risk,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<ActionPrecondition> Preconditions,
    IReadOnlyList<ActionEffect> Effects,
    string ExecutorId);
```

The descriptor is trusted server-side metadata.

### 11.2 ActionCandidate

```csharp
public sealed record ActionCandidate(
    ActionDescriptor Action,
    IReadOnlyDictionary<string, object?> Arguments,
    IReadOnlyDictionary<string, object?> Evidence);
```

A candidate represents an action that has been grounded in the current context.

### 11.3 DecisionContext

```csharp
public sealed record DecisionContext(
    Guid TenantId,
    string? SessionId,
    string Goal,
    IReadOnlyDictionary<string, object?> State,
    ActionRisk MaximumAutonomousRisk,
    string CorrelationId);
```

### 11.4 Decision result

```csharp
public sealed record Decision<T>(
    T? Choice,
    double Confidence,
    bool Abstained,
    IReadOnlyDictionary<string, double>? Distribution,
    string Provider,
    string? RationaleCode = null);
```

The rationale should preferably be structured metadata rather than a requirement to persist private model reasoning.

### 11.5 IDecisionProvider

```csharp
public interface IDecisionProvider
{
    Task<Decision<T>> ChooseAsync<T>(
        DecisionContext context,
        IReadOnlyList<T> candidates,
        CancellationToken ct = default);

    Task<ScoreDecision> ScoreAsync(
        DecisionContext context,
        DecisionRubric rubric,
        CancellationToken ct = default);

    Task<BooleanDecision> DecideAsync(
        DecisionContext context,
        CancellationToken ct = default);
}
```

### 11.6 IActionResolver

`IActionResolver` transforms semantic context into candidate actions.

It should combine ontology metadata, current state, tool/action bindings, and argument grounding.

### 11.7 IExecutionGate

`IExecutionGate` is the final deterministic authority before execution.

It evaluates:

- tenant constraints;
- caller authorization;
- capabilities;
- action risk;
- authoritative preconditions;
- decision confidence requirements;
- escalation requirements.

The selected action reaches the executor only after this gate succeeds.

## 12. Decision Providers

The provider abstraction allows different decision technologies to coexist.

### 12.1 RuleDecisionProvider

A deterministic implementation for cases in which domain rules fully determine the choice.

This should be preferred when the problem is genuinely deterministic.

### 12.2 LlmDecisionProvider

A structured-output LLM implementation.

This provides a baseline and handles decisions requiring richer semantic interpretation.

The provider should be constrained to the supplied candidate set.

#### 12.2.1 Implemented OpenAI baseline

PR-19 implements a disabled-by-default `OpenAiDecisionProvider` as the first remote structured-output experiment. It remains replay-only pending representative evidence and explicit acceptance thresholds.

#### 12.2.2 Anthropic candidate

A future `AnthropicDecisionProvider` can use the official Anthropic C# SDK behind the same `IDecisionProvider` boundary. The [Anthropic Decision Provider Feasibility and Design](<./LimboDancer.Agentic.CognitiveRuntime Anthropic Decision Provider Feasibility and Design.md>) defines the SDK constraints, fail-closed stop-reason policy, budget semantics, and admission gate.

Anthropic is a candidate implementation, not an architectural dependency. No implementation is admitted by this classification.

### 12.3 JevDecisionProvider

A specialized typed decision provider can implement the same contract.

Jev is particularly interesting because its model is oriented toward bounded decisions rather than open-ended generation.

However:

> **Jev is an implementation candidate, not an architectural dependency.**

Its value must be demonstrated empirically against other providers.

### 12.4 CompositeDecisionProvider

A composite provider can route or escalate decisions.

For example:

```text
deterministic rule
      |
      | unresolved
      v
specialized decision model
      |
      | low confidence
      v
LLM decision provider
      |
      | still uncertain / high risk
      v
human or reasoning escalation
```

The routing strategy itself should be observable and testable.

## 13. Confidence, Abstention, and Escalation

A mature Decision Plane must be able to say **I do not have sufficient confidence to select an action**.

Abstention is a first-class successful outcome of decision processing.

The system should not use one universal confidence threshold.

Required confidence should depend on action risk.

A read-only graph query may tolerate uncertainty that would be unacceptable for a destructive external side effect.

A conceptual policy might be:

```text
confidence >= threshold(risk)
        |
       yes
        v
execution gate
        |
       no
        v
fallback / escalation / clarification
```

The thresholds must be configurable and ultimately evidence-driven.

## 14. Risk-Aware Execution

Actions should carry explicit risk metadata.

An initial taxonomy:

```csharp
public enum ActionRisk
{
    ReadOnly,
    IdempotentWrite,
    ReversibleWrite,
    DestructiveWrite,
    ExternalSideEffect,
    PrivilegedAction
}
```

Risk influences:

- confidence requirements;
- permitted decision providers;
- confirmation requirements;
- audit detail;
- fallback strategy;
- whether autonomous execution is allowed.

Risk belongs to trusted action metadata, not caller input.

## 15. Preconditions and Effects

Preconditions and effects are central to the ontology-first architecture.

They should become properties of authoritative action definitions.

### Preconditions

Preconditions answer:

> Is this action valid in the current world state?

They should be deterministic whenever possible.

Examples include:

- entity exists;
- relationship exists;
- state has a required value;
- caller possesses a capability;
- workflow is in an allowed state.

### Effects

Effects answer:

> What semantic state should be true after successful execution?

Effects serve several purposes:

- state mutation;
- post-execution verification;
- audit;
- planning;
- recovery.

### Required change

Caller-supplied preconditions and effects should not be treated as authoritative execution policy.

The target flow is:

```text
ActionDescriptor
      |
      +--> authoritative preconditions
      |
      +--> executor binding
      |
      +--> expected effects
```

## 16. Decision Audit and Replay

Every consequential decision should produce an audit record sufficient to reconstruct the decision boundary without requiring private chain-of-thought.

A useful record includes:

- tenant;
- session;
- correlation ID;
- goal identifier or safe goal summary;
- candidate action IDs;
- candidates removed by deterministic gates and reason codes;
- decision provider;
- provider/model version;
- selected action;
- confidence;
- probability distribution when available;
- abstention state;
- risk classification;
- policy result;
- precondition results;
- execution result;
- observed effects;
- latency;
- token usage where applicable;
- monetary cost where measurable.

This makes provider evaluation possible using production traces while preserving the distinction between audit metadata and hidden model reasoning.

Replay should support evaluating the same historical decision context against another provider without executing the resulting action.

## 17. Observability and Metrics

The Decision Plane should expose metrics such as:

- decision latency;
- provider latency;
- action selection accuracy on labeled cases;
- calibration error;
- abstention rate;
- fallback rate;
- escalation rate;
- provider disagreement rate;
- execution success rate;
- effect verification failure rate;
- token consumption;
- decision cost;
- wrong-action cost by risk class.

The important metric is not simply model accuracy.

The relevant system metric is whether LimboDancer selected and safely completed the correct action at acceptable latency and cost.

## 18. Evaluation Framework

Decision providers should be evaluated against a common corpus.

Candidate providers may include:

- deterministic rules;
- Jev;
- structured-output frontier LLMs;
- smaller language models;
- local classifiers;
- composite routing strategies.

The evaluation corpus should contain:

- decision context;
- permitted candidate set;
- expected choice or acceptable choices;
- risk class;
- ambiguity markers;
- expected abstention cases.

Measurements should include:

- choice accuracy;
- calibration;
- abstention quality;
- latency;
- throughput;
- cost;
- fallback frequency;
- task completion;
- wrong-action severity.

A provider should not be adopted because it is novel or inexpensive. It should be adopted when it performs appropriately for a defined decision class.

## 19. Relationship to MCP

MCP remains the tool discovery and execution protocol.

The Decision Plane sits above MCP execution.

```text
Decision Plane
      |
      v
semantic ActionDescriptor
      |
      v
Execution Gate
      |
      v
executor binding
      |
      +--> MCP tool
      +--> application service
      +--> workflow
      +--> external integration
```

This separation prevents LimboDancer's semantic model from becoming synonymous with MCP.

MCP can evolve independently as an execution interface.

## 20. Relationship to LimboDancer.MCP.Llm

The existing `LimboDancer.MCP.Llm` project is currently an architectural seam.

It should not automatically become a collection of vendor-specific LLM clients.

Two options should be considered during implementation:

1. evolve it into a broader intelligence project; or
2. split reasoning and decision abstractions into separately named projects.

A possible future structure is:

```text
LimboDancer.Agentic.CognitiveRuntime.Intelligence
    Abstractions/
    Reasoning/
    Decisions/

LimboDancer.Agentic.CognitiveRuntime.Intelligence.OpenAI
LimboDancer.Agentic.CognitiveRuntime.Intelligence.Jev
```

The exact project decomposition is less important than preserving dependency direction.

Core decision contracts must not depend on vendor SDKs.

## 21. Target Runtime Flow

The intended runtime is:

```text
User / Agent Request
        |
        v
+-------------------+
|  Reasoning Plane  |
| goal / planning   |
+---------+---------+
          |
          v
+-------------------+
|  Semantic Plane   |
| ontology / state  |
| action resolution |
+---------+---------+
          |
          v
+-------------------+
| Deterministic Gate|
| auth / policy /   |
| preconditions     |
+---------+---------+
          |
     valid actions
          |
          v
+-------------------+
|  Decision Plane   |
| choose / score /  |
| decide / abstain  |
+---------+---------+
          |
          v
+-------------------+
| Confidence + Risk |
| execution gate    |
+---------+---------+
          |
          v
+-------------------+
| Execution Plane   |
| MCP / services    |
+---------+---------+
          |
          v
+-------------------+
|    State Plane    |
| KG / SQL / Vector |
+---------+---------+
          |
          v
 effect verification
          |
          +-------> next observation / decision
```

Every transition should be explicit enough to test independently.

## 22. Implementation Strategy

The Decision Plane should be introduced incrementally.

### Phase 1: Contracts

Introduce provider-independent contracts:

- `ActionDescriptor`;
- `ActionCandidate`;
- `ActionRisk`;
- `DecisionContext`;
- `Decision<T>`;
- `IDecisionProvider`;
- `IActionResolver`;
- `IExecutionGate`.

No production behavior needs to change in this phase.

### Phase 2: Authoritative action metadata

Move precondition/effect authority out of caller payloads.

Bind actions to server-side semantic definitions.

Preserve compatibility where necessary while marking caller-provided semantic policy as non-authoritative or deprecated.

### Phase 3: Action resolution

Implement `IActionResolver`.

Given goal/context/state, produce a finite set of grounded `ActionCandidate` instances.

### Phase 4: Central execution gate

Create a single deterministic gate through which decision-selected actions must pass.

This is the key safety boundary.

### Phase 5: Baseline decision provider

Implement a provider using technology already available to the project, likely deterministic rules and/or a structured-output LLM.

This establishes the contract and evaluation baseline before introducing a specialized provider.

### Phase 6: Audit and replay

Persist decision records and provide replay infrastructure.

This should exist before serious provider benchmarking.

### Phase 7: Jev provider

Implement `JevDecisionProvider` behind the same contract.

Initially run it in shadow mode against real decision contexts without allowing it to control execution.

### Phase 8: Evaluation

Compare providers using the same corpus and production-derived replay cases.

### Phase 9: Composite routing

Introduce risk-, cost-, and confidence-aware provider routing only after empirical results justify it.

## 23. Open Architectural Questions

Several questions should remain explicit until implementation evidence resolves them:

1. Should the current `LimboDancer.MCP.Llm` project be renamed to `Intelligence`, or should Decision become a separate project?
2. Where should authoritative `ActionDescriptor` definitions live: ontology storage, application configuration, generated artifacts, or a hybrid?
3. Which preconditions belong to ontology semantics versus authorization policy?
4. How should action versioning work when ontology definitions change?
5. How should confidence values from heterogeneous providers be calibrated onto comparable scales?
6. Which risk classes require human confirmation?
7. Should the planner generate candidate actions, or should candidate generation remain entirely ontology-driven?
8. How should multi-step actions and transactional effects be represented?
9. How should execution compensation be modeled for reversible workflows?
10. What information is safe and necessary to persist for replay without storing sensitive prompts or private model reasoning?
11. Should the two current HTTP/MCP composition roots converge on a shared application runtime?
12. How should multiple concurrent agents coordinate decisions over changing shared graph state?

These questions are design work, not reasons to delay the foundational contracts.

## 24. Architectural Invariants

The following invariants should guide implementation and code review.

1. **The Decision Plane selects only from explicit candidates.**
2. **Candidate membership does not imply permission.**
3. **Authorization and hard preconditions are deterministic.**
4. **A probabilistic provider cannot override a failed deterministic gate.**
5. **Risk metadata comes from trusted server-side definitions.**
6. **Decision confidence is evidence, not authority.**
7. **Abstention is a valid outcome.**
8. **Execution is separated from selection.**
9. **Expected effects are defined before execution and can be verified afterward.**
10. **Every consequential decision is auditable without persisting hidden chain-of-thought.**
11. **Decision provider implementations are replaceable.**
12. **MCP is an execution protocol, not the domain ontology.**
13. **Tenant boundaries apply across every plane.**

## 25. Success Criteria

The Decision Plane initiative is successful when LimboDancer can demonstrate the following behavior:

A goal enters the system. The runtime resolves a finite semantic action set. Deterministic policy and preconditions remove invalid or unauthorized actions. A replaceable decision provider selects among the remaining alternatives or abstains. Risk and confidence policy determine whether autonomous execution is permitted. The selected action passes through a centralized execution gate. The action executes through its bound executor. Expected semantic effects are verified. The complete decision boundary and outcome are auditable and replayable.

At that point LimboDancer will have moved from a collection of ontology-aware MCP tools to an ontology-constrained agent runtime.

The architectural objective is not to make the model more powerful.

It is to make **agency explicit, bounded, measurable, replaceable, and governable**.
