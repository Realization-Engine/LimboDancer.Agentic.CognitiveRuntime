# LimboDancer.Agentic.CognitiveRuntime Runtime Orchestration Model

**Status:** Architecture analysis  
**Branch:** `decision-plane`  
**Companion documents:** `LimboDancer.Agentic.CognitiveRuntime Plane Architecture Analysis.md`, `LimboDancer.Agentic.CognitiveRuntime Plane Architecture Codebase Validation.md`, `Decision Plane Architecture.md`


**Canonical system identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**Legacy implementation namespace:** Existing `LimboDancer.MCP.*` assemblies retain their current names until an explicit code migration is performed.
**Legacy source root:** `src/Legacy/` (temporary; delete after required legacy behavior is ported)  

## 1. Purpose

This document defines how work moves through the LimboDancer plane architecture at runtime.

The previous analyses established the capability model:

- Interaction connects;
- Reasoning proposes;
- Semantics constrains;
- Governance permits;
- Decision selects;
- Execution acts;
- State remembers;
- Orchestration coordinates.

The unresolved question is operational:

> What actually happens, in what order, when a request enters LimboDancer?

The answer must support both explicit MCP tool invocation and autonomous agent behavior without confusing their authority models.

This document defines that runtime lifecycle before implementation contracts are introduced.

## 2. Orchestration Is Coordination, Not Authority

The orchestrator owns sequencing.

It does **not** own:

- domain meaning;
- authorization policy;
- precondition truth;
- candidate preference;
- execution semantics.

Those authorities remain with their respective planes.

The orchestrator is therefore analogous to a workflow kernel:

```text
                +---------------------------+
request ------> |      ORCHESTRATOR         |
                | context + stage sequencing|
                +---------------------------+
                    |    |    |    |    |
                    v    v    v    v    v
                 Reason Sem  Gov  Decision Exec
                    _______________________/
                              |
                              v
                            State
```

A useful invariant is:

> The orchestrator may ask every authoritative component a question, but it may not answer those questions on their behalf.

## 3. The Runtime Unit: Goal

The fundamental autonomous runtime unit should be a **Goal**, not a tool call.

A Goal represents the user's or system's desired outcome before it has been reduced to an executable action.

Conceptually:

```csharp
public sealed record Goal(
    string GoalId,
    string CorrelationId,
    string TenantId,
    string? SessionId,
    GoalOrigin Origin,
    string Intent,
    IReadOnlyDictionary<string, object?> Inputs,
    DateTimeOffset CreatedAt);
```

The exact C# shape should be finalized during implementation.

Important properties are:

- stable identity;
- tenant identity;
- correlation identity;
- origin;
- user/system intent;
- supplied inputs;
- lifecycle timestamp.

The Goal is not an LLM prompt.

It is a runtime object that can be produced from MCP, HTTP, CLI, scheduled work, another agent, or future interfaces.

## 4. The Runtime Envelope: OrchestrationContext

A Goal moves through the runtime inside an **OrchestrationContext**.

The context is the shared envelope that allows planes to cooperate without becoming coupled to each other's implementations.

Conceptually it contains:

```text
OrchestrationContext
├── Goal
├── Tenant / principal
├── Correlation / trace
├── Session
├── Origin
├── Current observation
├── Semantic scope
├── Candidate actions
├── Governance results
├── Decision result
├── Execution result
├── Expected effects
├── Observed effects
├── Budget
├── Risk posture
└── Audit references
```

The context should evolve through explicit stage results rather than becoming an unstructured mutable dictionary.

This is important.

A "context bag" eventually becomes invisible shared state. The orchestration context should instead preserve typed stage boundaries.

## 5. Two Entry Modes

LimboDancer has two fundamentally different entry modes.

They must not be conflated.

### 5.1 Directed Invocation

The caller explicitly requests a known action.

Example:

```text
MCP client:
    execute history_get with these arguments
```

The caller has already selected the action.

The runtime does **not** need a probabilistic Decision Plane selection.

However, explicit selection does not bypass:

- authentication;
- tenant isolation;
- authorization;
- semantic validation;
- hard preconditions;
- risk policy;
- final execution gate;
- audit.

The path is:

```text
Interaction
    |
    v
Resolve explicit action
    |
    v
Semantic validation
    |
    v
Governance
    |
    v
Execution gate
    |
    v
Execution
    |
    v
Effect verification / audit
```

This preserves MCP as a useful direct execution protocol.

### 5.2 Autonomous Goal

The caller supplies an outcome or intent, not an action.

Example:

```text
"Find the relevant memory and update the trip state if the reservation is confirmed."
```

The runtime must determine what action should occur.

The path is:

```text
Interaction
    |
    v
Goal normalization
    |
    v
Reasoning / planning
    |
    v
Semantic action resolution
    |
    v
Governance + hard preconditions
    |
    v
Permitted candidate set
    |
    v
Decision
    |
    v
Execution gate
    |
    v
Execution
    |
    v
Effect verification
    |
    v
Observation
    |
    +----> complete
    |
    +----> Reasoning for next step
```

The Decision Plane exists specifically because this second path exists.

## 6. Why Directed Invocation Still Needs an Action Model

The current runtime treats MCP tool identity as executable identity.

That should evolve.

A directed tool call should resolve to a semantic action descriptor:

```text
MCP tool name
    |
    v
Action binding
    |
    v
ActionDescriptor
```

For example:

```text
history_get
    -> ldm:action/HistoryRead

history_append
    -> ldm:action/HistoryAppend
```

This means both direct and autonomous paths eventually converge on the same trusted action representation.

The difference is only **who selected the action**.

```text
Directed:
caller selected action
        |
        v
ActionDescriptor

Autonomous:
Decision Plane selected action
        |
        v
ActionDescriptor
```

Everything after that convergence can share the same gate and executor.

## 7. ActionDescriptor as the Runtime Contract

The ActionDescriptor is the bridge between Semantic, Governance, Decision, and Execution.

Conceptually:

```text
ActionDescriptor
├── Semantic identity
├── Version
├── Description
├── Input contract
├── Preconditions
├── Effects
├── Risk class
├── Required permissions
├── Idempotency semantics
├── Executor binding
└── Observability metadata
```

The descriptor must be server-authoritative.

Caller payloads may supply action **arguments**.

They must not supply authoritative:

- preconditions;
- effects;
- risk classification;
- permission requirements;
- executor identity.

This directly resolves the current `HistoryAppendTool` authority problem.

## 8. Autonomous Runtime Lifecycle

The canonical autonomous lifecycle should be modeled as explicit stages.

### Stage 0: Admit

Interaction accepts the request.

The runtime establishes:

- authenticated principal;
- tenant;
- correlation ID;
- trace;
- origin;
- request budget.

Failure here terminates the request.

### Stage 1: Normalize Goal

Protocol-specific input becomes a Goal.

The rest of the cognitive runtime should not care whether the request originated from:

- MCP;
- HTTP;
- CLI;
- Blazor;
- scheduler;
- another agent.

### Stage 2: Observe

The runtime acquires the state necessary to reason about the Goal.

This may include:

- recent history;
- graph state;
- ontology definitions;
- vector retrieval;
- external observations.

Observation is deliberately separate from Reasoning.

State returns facts. Reasoning interprets them.

### Stage 3: Reason

The Reasoning Plane produces a structured proposal.

It may:

- decompose the Goal;
- identify missing information;
- formulate subgoals;
- request observations;
- propose intended semantic outcomes.

Reasoning does not execute.

Reasoning should preferably output typed planning artifacts rather than free text when its output enters the control path.

### Stage 4: Resolve Actions

The Semantic Plane maps the current intended outcome to a finite set of ActionDescriptors.

This is the critical reduction:

```text
open-ended intent
      |
      v
finite semantic action set
```

If no action applies, the runtime returns to Reasoning or terminates with an explicit no-action result.

### Stage 5: Constrain

Semantic preconditions and Governance policy remove invalid actions.

The result is not "the best action."

It is the set of actions that are presently admissible.

```text
candidate actions
      |
      +--> semantic applicability
      |
      +--> tenant boundary
      |
      +--> authorization
      |
      +--> hard preconditions
      |
      +--> risk policy
      |
      v
permitted candidates
```

If the permitted set is empty, Decision is not invoked.

### Stage 6: Decide

The Decision Plane selects among the permitted candidates or abstains.

Decision receives only candidates that have survived deterministic constraints.

It returns:

- selected candidate;
- confidence;
- provider identity;
- optional distribution;
- abstention/escalation reason.

Decision cannot restore a filtered candidate.

### Stage 7: Final Execution Gate

Selection does not itself authorize execution.

The final gate revalidates execution-sensitive invariants immediately before the side effect.

This is necessary because state can change between candidate evaluation and execution.

The gate verifies at least:

- tenant;
- authorization;
- action version;
- hard preconditions;
- concurrency/state token if applicable;
- risk/confidence policy;
- required confirmation;
- execution budget.

This is a TOCTOU defense: time-of-check versus time-of-use.

### Stage 8: Execute

Execution invokes the bound executor with validated arguments.

The executor performs the operational side effect.

It does not choose whether it should have been called.

### Stage 9: Verify Effects

Expected effects from the ActionDescriptor are compared with observed post-execution state where feasible.

Possible results:

- verified;
- partially verified;
- unverifiable;
- contradicted.

A successful API return is not automatically proof that the semantic effect occurred.

### Stage 10: Observe Result

The execution result and verified state changes become a new Observation.

### Stage 11: Continue or Complete

Reasoning determines whether:

- the Goal is complete;
- another step is required;
- new information is required;
- compensation is required;
- human intervention is required.

This produces the agentic loop.

## 9. The Agentic Loop

The runtime is therefore not simply:

```text
prompt -> model -> tool
```

It is:

```text
                    +----------------------+
                    |         GOAL         |
                    +----------+-----------+
                               |
                               v
                         +-----------+
                         |  OBSERVE  |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         |  REASON   |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         |  RESOLVE  |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         | CONSTRAIN |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         |  DECIDE   |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         |   GATE    |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         | EXECUTE   |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         |  VERIFY   |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         | OBSERVE   |
                         +-----+-----+
                               |
                    +----------+----------+
                    |                     |
                 complete              continue
                    |                     |
                    v                     +----> REASON
                  RESULT
```

The loop is bounded by budget, policy, cancellation, and maximum-step constraints.

## 10. Planning Versus Action Resolution

Planning and action resolution must remain distinct.

### Planning

Planning answers:

> What needs to happen next to advance the Goal?

Its output may be:

- a subgoal;
- an information requirement;
- an intended state transition;
- a sequence hypothesis.

### Action resolution

Action resolution answers:

> Which registered semantic actions are capable of producing that outcome in the current domain?

Planning may remain open-ended.

Action resolution must produce a finite typed set.

This distinction prevents the planner from inventing executable capabilities.

```text
Planner:
"I need the current reservation state."

Resolver:
Allowed capabilities capable of satisfying that need:
- ldm:action/GraphQuery
- ldm:action/MemorySearch
- ldm:action/HistoryRead
```

Decision then selects among the admissible options if selection is required.

## 11. Semantic Constraints Versus Governance Constraints

Both filter candidate actions, but they answer different questions.

### Semantic

> Does this action make sense in the domain given current state?

Examples:

- entity exists;
- reservation is pending;
- relation is valid;
- required domain property exists.

### Governance

> Is this actor/runtime allowed to perform this action under current policy?

Examples:

- tenant matches;
- principal has permission;
- action risk is permitted;
- human confirmation exists;
- quota is available;
- action is allowed in this environment.

An action must satisfy both.

```text
admissible(action) =
    semantic_valid(action)
    AND governance_permitted(action)
```

This distinction should be visible in audit records.

## 12. Decision Outcomes

Decision should not be modeled as merely returning an action.

The runtime needs at least four outcomes:

```text
Selected
Abstained
Escalated
NoCandidate
```

### Selected

A candidate is preferred with sufficient evidence to continue to the gate.

### Abstained

The provider cannot justify a selection.

No action occurs.

### Escalated

The current provider or policy requests a stronger provider, human confirmation, or another reasoning pass.

### NoCandidate

Deterministic resolution/constraint stages produced no executable alternative.

This is not a model failure.

These distinctions are important for both operations and evaluation.

## 13. Risk and Confidence

Risk and confidence belong to different authorities.

Risk comes from the trusted action definition and policy.

Confidence comes from the Decision provider.

The gate combines them.

Illustrative policy:

```text
ReadOnly
    high confidence   -> autonomous
    medium confidence -> autonomous or verify
    low confidence    -> escalate

ReversibleWrite
    high confidence   -> autonomous
    medium confidence -> stronger decision / confirm
    low confidence    -> abstain

DestructiveWrite
    any confidence    -> confirmation or privileged policy
```

Exact thresholds should be policy, not hard-coded into Decision providers.

## 14. State Versioning and TOCTOU

Autonomous agents introduce a concurrency problem.

The system may evaluate:

```text
reservation.status == Pending
```

then spend time reasoning and deciding.

Before execution, another actor may change the status.

Therefore a candidate evaluation should carry a state observation/version token when the underlying store supports it.

The final gate should revalidate.

Conceptually:

```text
Observed state S1
     |
Resolve / constrain / decide
     |
     v
Execution gate
     |
     +--> still S1 or preconditions still true? yes -> execute
     |
     +--> changed? -> reject stale decision and re-observe
```

A stale decision should normally cause re-observation, not blind retry.

## 15. Multi-Step Plans

A plan should not be treated as blanket authorization for all future steps.

Each consequential step passes through the lifecycle independently.

```text
Plan
├── Step 1 -> resolve -> constrain -> decide -> gate -> execute -> observe
├── Step 2 -> resolve again against new state
└── Step 3 -> resolve again against new state
```

This allows the system to adapt to:

- failed actions;
- external state changes;
- newly discovered information;
- policy changes;
- unexpected effects.

The plan is a hypothesis about future work, not a transaction granting future authority.

## 16. Compensation and Reversibility

ActionDescriptors should eventually describe reversibility semantics.

Possible classes:

- read-only;
- idempotent write;
- reversible write;
- compensatable external effect;
- irreversible/destructive effect.

For reversible or compensatable actions, the descriptor may identify a compensation action.

However, compensation should itself be a governed action.

```text
execute A
   |
unexpected downstream failure
   |
propose compensate(A)
   |
resolve / constrain / gate
   |
execute compensation
```

The runtime should never assume "undo" is automatically authorized merely because the original action was authorized.

## 17. Audit and Replay

Audit must record the decision boundary without storing private model chain-of-thought.

For each consequential step, persist structured facts such as:

```text
GoalId
StepId
CorrelationId
TenantId
PrincipalId
Origin
Observation references / versions
Resolved candidate IDs + versions
Semantic constraint results
Governance constraint results
Decision provider
Decision provider version
Selected candidate
Confidence
Distribution when available
Risk class
Gate result
Execution start/end
Execution result
Expected effects
Observed effects
Verification result
Latency
Token/cost metrics when applicable
Escalation / abstention reason
```

Reasoning may persist concise rationale summaries or structured planning artifacts where appropriate.

Hidden model reasoning is neither required nor desirable for replay.

### Replay

Replay should reconstruct the **decision boundary**:

- what state was observed;
- what candidates existed;
- which were filtered;
- which provider was used;
- what it selected;
- what policy permitted;
- what happened.

This enables provider benchmarking without re-running side effects.

## 18. Direct Invocation Runtime

The existing `McpServer.ExecuteToolAsync` path should eventually evolve conceptually from:

```text
toolName -> executor
```

to:

```text
toolName
   |
   v
ActionBinding
   |
   v
ActionDescriptor
   |
   v
validate arguments
   |
   v
semantic constraints
   |
   v
governance
   |
   v
execution gate
   |
   v
executor
   |
   v
effect verification
```

No Decision provider is necessary because the caller explicitly selected the action.

This is important for compatibility and latency.

The Decision Plane should not be inserted merely to say "yes" to an already explicit deterministic request.

## 19. Autonomous Invocation Runtime

Autonomous execution adds the missing stages:

```text
Goal
 |
 v
Reason
 |
 v
Resolve multiple ActionDescriptors
 |
 v
Constrain
 |
 v
Decision provider
 |
 v
Selected ActionDescriptor
 |
 v
Execution gate
 |
 v
Executor
```

Thus direct and autonomous invocation converge before execution.

This is the cleanest integration point with the current MCP tool system.

## 20. Proposed Runtime Interfaces

The following interfaces are architectural sketches, not final code.

### Goal orchestration

```csharp
public interface IGoalOrchestrator
{
    Task<GoalResult> RunAsync(
        Goal goal,
        CancellationToken ct = default);
}
```

### Action resolution

```csharp
public interface IActionResolver
{
    Task<IReadOnlyList<ActionCandidate>> ResolveAsync(
        ActionResolutionContext context,
        CancellationToken ct = default);
}
```

### Semantic constraint evaluation

```csharp
public interface ISemanticConstraintEvaluator
{
    Task<ConstraintResult> EvaluateAsync(
        ActionCandidate candidate,
        ConstraintContext context,
        CancellationToken ct = default);
}
```

### Governance

```csharp
public interface IActionPolicyEvaluator
{
    Task<PolicyResult> EvaluateAsync(
        ActionCandidate candidate,
        PolicyContext context,
        CancellationToken ct = default);
}
```

### Decision

```csharp
public interface IDecisionProvider
{
    Task<DecisionResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<ActionCandidate> candidates,
        CancellationToken ct = default);
}
```

### Final gate

```csharp
public interface IExecutionGate
{
    Task<ExecutionGateResult> AuthorizeAsync(
        SelectedAction action,
        ExecutionContext context,
        CancellationToken ct = default);
}
```

### Execution

```csharp
public interface IActionExecutor
{
    Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken ct = default);
}
```

The type system should make invalid transitions difficult.

For example, an executor should ideally accept an `AuthorizedAction`, not a raw `ActionCandidate`.

## 21. Typestate as an Architectural Tool

The runtime lifecycle can use distinct types to represent authority transitions.

```text
ActionCandidate
      |
      | semantic + governance pass
      v
PermittedAction
      |
      | decision selection
      v
SelectedAction
      |
      | final gate
      v
AuthorizedAction
      |
      | executor
      v
ExecutedAction
```

This gives compile-time expression to an important invariant:

> An action that has not passed the final gate should not have the type required by the executor.

Not every transition must become a separate public C# record, but the principle is valuable.

## 22. Orchestration State Machine

The lifecycle should have an explicit state machine.

Suggested states:

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

Transitions should be observable.

This will make the Blazor Operator Console much more useful because it can display where an agent step currently is and why it stopped.

## 23. Budgets and Loop Termination

An autonomous runtime needs explicit budgets.

Potential dimensions:

- maximum steps;
- wall-clock deadline;
- model token budget;
- monetary budget;
- external-call budget;
- retry budget;
- maximum consecutive abstentions;
- maximum repeated state signature.

The orchestrator enforces these budgets.

This is orchestration authority because it governs lifecycle resources, not domain permission.

When a budget is exhausted, the Goal terminates or escalates rather than continuing indefinitely.

## 24. Error Taxonomy

Errors should be classified by stage.

Examples:

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

A failed semantic precondition should not surface as a generic tool exception.

Likewise, abstention is not an exception.

This taxonomy is necessary for reliable retries and useful telemetry.

## 25. Retry Semantics

The orchestrator should not generically retry every failed stage.

Examples:

- transient State read failure: retry may be appropriate;
- Governance denial: do not retry unchanged;
- failed semantic precondition: re-observe/reason, not blind retry;
- Decision provider timeout: provider fallback may be appropriate;
- stale state at gate: re-observe;
- non-idempotent executor timeout: do not retry unless execution identity/idempotency semantics make it safe.

Retry policy therefore depends on stage and action metadata.

## 26. Provider Escalation

Decision provider escalation belongs to orchestration policy, not to the provider itself.

Example:

```text
RuleDecisionProvider
        |
        | unresolved
        v
JevDecisionProvider
        |
        | confidence below threshold
        v
LlmDecisionProvider
        |
        | high-risk action
        v
Human confirmation
```

The exact sequence should be configurable by risk class and workload.

A provider reports its result.

The runtime decides whether that result is sufficient.

## 27. Human Confirmation

Human confirmation is a Governance event, not a Decision provider.

The model may prefer an action, but policy may require explicit approval.

The lifecycle becomes:

```text
SelectedAction
     |
     v
Gate detects confirmation requirement
     |
     v
AwaitingConfirmation
     |
     +--> approved -> revalidate state/policy -> execute
     |
     +--> denied   -> terminate/reason
     |
     +--> expired  -> re-observe
```

Approval must not bypass revalidation because state may change while waiting.

## 28. Integration With Current Code

The architecture can be introduced incrementally.

### Current

```text
McpController
    -> McpServer
        -> Tool
            -> service/storage
```

### First convergence step

```text
McpController
    -> McpServer
        -> ActionBinding
            -> ExecutionGate
                -> existing Tool
```

This introduces authoritative action metadata and a gate without rewriting every tool.

### Autonomous path added later

```text
GoalOrchestrator
    -> Reasoning
    -> ActionResolver
    -> Constraint evaluators
    -> DecisionProvider
    -> ExecutionGate
    -> existing Tool / executor
```

Both paths share `ActionDescriptor` and `ExecutionGate`.

This is a low-disruption migration path.

## 29. The Role of Existing MCP Tools

Existing MCP tools can initially remain executor adapters.

They do not need to be deleted.

However, their responsibilities should narrow over time.

For example, `HistoryAppendTool` currently receives caller-supplied preconditions/effects.

Target:

```text
HistoryAppendTool
    receives:
        validated action arguments

ActionDescriptor
    owns:
        authoritative preconditions
        authoritative effects
        risk
        permissions
```

Eventually the tool becomes mostly:

```text
protocol input
    -> argument DTO
    -> application executor
    -> protocol output
```

That is a cleaner Interaction/Execution boundary.

## 30. The Role of the Existing Chat Orchestrator

`InMemoryChatOrchestrator` should not be evolved directly into the cognitive runtime orchestrator.

Its current concerns are:

- chat session management;
- channels;
- streaming;
- cancellation;
- in-memory history;
- heartbeat;
- simulated generation.

Those are Interaction/session concerns.

The future `IGoalOrchestrator` should be host-neutral and independent of SSE or chat session implementation.

A chat controller can call the Goal orchestrator.

The Goal orchestrator should not know that SSE exists.

## 31. Host Convergence

Both current hosts should eventually invoke the same runtime.

Conceptually:

```text
MCP Host ---------+
                  |
HTTP Chat Host ---+
                  |
CLI --------------+----> LimboDancer Runtime
                  |
Blazor Operator --+
                  |
Future adapters --+
```

This does not require one executable.

It requires one application runtime contract.

The hosts may remain separately deployable.

## 32. Runtime Placement

The orchestration runtime should not live inside the `LimboDancer.MCP.Llm` project.

It is broader than LLM usage.

Nor should it remain embedded in the MCP host.

A future host-neutral application/runtime project may be warranted, for example:

```text
LimboDancer.Agentic.CognitiveRuntime
```

or

```text
LimboDancer.Agentic.CognitiveRuntime.Application
```

No project should be created solely to match the diagram, however.

The extraction should occur when the first shared orchestration contracts make the boundary concrete.

## 33. Sequence: Autonomous Read-Only Example

Consider a Goal:

> Find the most relevant memory about a subject.

```text
Interaction
    |
    v
Goal created
    |
    v
Reasoning: relevant memory retrieval is needed
    |
    v
Semantic resolver:
    - MemorySearch
    - HistoryRead
    - GraphQuery
    |
    v
Semantic/Governance filters:
    all three permitted
    |
    v
Decision:
    MemorySearch, confidence .91
    |
    v
Gate:
    read-only, tenant valid, budget valid
    |
    v
MemorySearch executor
    |
    v
Observation returned
    |
    v
Reasoning synthesizes result
    |
    v
Complete
```

The provider does not generate a tool name.

It chooses a typed candidate.

## 34. Sequence: Autonomous Write Example

Consider a Goal requiring a state change.

```text
Goal
 |
 v
Reasoning proposes desired state transition
 |
 v
Semantic resolver finds:
    UpdateReservation
    CancelReservation
 |
 v
Semantic constraints:
    reservation currently Pending
 |
 v
Governance:
    UpdateReservation permitted
    CancelReservation requires confirmation
 |
 v
Decision selects UpdateReservation
 |
 v
Final gate re-reads critical state
 |
 +--> status still Pending
 |
 v
AuthorizedAction
 |
 v
Executor
 |
 v
Expected effect:
    reservation.status = Confirmed
 |
 v
Post-state observation
 |
 v
Effect verified
 |
 v
Audit + complete
```

If status changed before the gate, the decision becomes stale and the runtime returns to observation.

## 35. Sequence: Direct MCP Example

Current client call:

```text
POST /api/mcp/tools/history_get
```

Target runtime:

```text
McpController
 |
 v
resolve binding "history_get"
 |
 v
ldm:action/HistoryRead@version
 |
 v
validate arguments
 |
 v
semantic constraints
 |
 v
governance
 |
 v
final gate
 |
 v
HistoryGet executor
 |
 v
audit
 |
 v
MCP response
```

No planner and no Decision provider are required.

This preserves MCP determinism.

## 36. Architectural Invariants

The runtime orchestration model establishes these invariants:

1. A Goal is distinct from an Action.
2. A protocol tool name is distinct from semantic action identity.
3. Directed and autonomous invocation use different selection paths.
4. Both paths converge on the same trusted ActionDescriptor before execution.
5. Reasoning cannot invent executable capabilities.
6. Semantic resolution produces a finite action set.
7. Governance and hard preconditions execute before probabilistic selection.
8. Decision sees only permitted candidates.
9. Decision selection is not execution authorization.
10. The final gate revalidates execution-sensitive state.
11. Executors accept authorized work, not raw model output.
12. Expected effects are defined before execution.
13. Post-execution effects are verified where feasible.
14. Every consequential step produces structured audit data.
15. Multi-step plans do not grant future authority.
16. Human confirmation is followed by revalidation.
17. Retry semantics depend on stage and action idempotency.
18. Tenant scope is carried through the complete lifecycle.
19. Orchestration coordinates authority but does not replace it.
20. Interaction transports requests but does not define domain semantics.

## 37. Consequences for Decision Plane Interfaces

This orchestration analysis materially narrows the Decision Plane contract.

The Decision Plane does **not** need to know about:

- MCP;
- HTTP;
- SSE;
- graph SDKs;
- Azure Search;
- executors;
- authentication implementation;
- persistence technology.

It needs:

- decision context;
- a finite list of already permitted candidates;
- candidate semantic metadata relevant to choice;
- relevant observations;
- provider budget;
- cancellation.

It returns a typed decision result.

This means the first Decision Plane interfaces can now be designed with substantially less ambiguity.

## 38. Recommended First Implementation Slice

The first code slice should prove the convergence point rather than build the entire autonomous loop.

Recommended sequence:

1. Define `ActionRisk`.
2. Define `ActionDescriptor`.
3. Define `ActionCandidate`.
4. Define action binding/registry abstraction.
5. Define `ExecutionContext`.
6. Define `IExecutionGate`.
7. Implement a deterministic baseline gate.
8. Bind the four existing MCP tools to trusted ActionDescriptors.
9. Route direct MCP execution through the gate.
10. Add structured audit events for gate allow/deny.
11. Add tests for unknown action, tenant absence, and denied execution.
12. Only then add `IDecisionProvider` and the autonomous selection path.

This ordering is deliberate.

The Decision Plane should select into a trustworthy execution path that already exists.

Building probabilistic selection before the execution authority boundary would invert the dependency.

## 39. Final Model

The complete runtime can be summarized as:

```text
                         GOVERNANCE / CONTROL
                identity | tenant | policy | risk | audit
                                  |
                                  v
+-------------+      +--------------------------+
| INTERACTION | ---> |      ORCHESTRATION       |
+-------------+      +--------------------------+
                            |           ^
                            v           |
                      +-----------+     |
                      | REASONING |     |
                      +-----+-----+     |
                            |           |
                            v           |
                      +-----------+     |
                      | SEMANTICS |     |
                      +-----+-----+     |
                            |           |
                            v           |
                      +-----------+     |
                      | DECISION  |     |
                      +-----+-----+     |
                            |           |
                            v           |
                      +-----------+     |
                      | EXECUTION |     |
                      +-----+-----+     |
                            |           |
                            v           |
                      +-----------+     |
                      |   STATE   | ----+
                      +-----------+
```

The diagram is intentionally simplified.

Governance crosses every transition.

Orchestration coordinates every transition.

State feeds new observations back into Reasoning.

## 40. Conclusion

The runtime architecture should not be built around an LLM calling tools.

It should be built around **goals moving through explicit authority transitions**.

The crucial convergence is the trusted ActionDescriptor and final Execution Gate.

That allows LimboDancer to support both deterministic MCP clients and autonomous agents without creating two execution systems.

The resulting runtime has a clear chain of authority:

> **A Goal is interpreted by Reasoning, grounded by Semantics, bounded by Governance, selected by Decision, revalidated by the Execution Gate, enacted by Execution, recorded in State, and coordinated throughout by Orchestration.**

The next implementation work should therefore begin at the convergence boundary: **ActionDescriptor + Action Binding + Execution Gate**, followed by the Decision Plane contracts.
