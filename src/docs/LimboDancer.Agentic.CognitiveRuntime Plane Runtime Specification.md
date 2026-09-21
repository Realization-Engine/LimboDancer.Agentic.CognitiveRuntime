# LimboDancer.Agentic.CognitiveRuntime Plane Runtime Specification

**Status:** Normative implementation specification  
**Branch:** `decision-plane`  
**Source design:** `LimboDancer.Agentic.CognitiveRuntime Plane Runtime Design.md`  
**Audience:** Runtime, MCP, ontology, diagnostics, infrastructure, and test engineers

**Reference-domain requirements:** `docs/ASL/legacy-limbodancer-mcp-system-design.md`


**Canonical system identity:** `LimboDancer.Agentic.CognitiveRuntime`  
**.NET root namespace / project prefix:** `LimboDancer`  
**Legacy code status:** Existing `LimboDancer.MCP.*` projects are reference implementations only. Conforming new production code MUST NOT depend on them.
**Legacy source root:** `src/Legacy/` (temporary; delete after required legacy behavior is ported)  

## 1. Purpose

This specification defines the implementation contract for the LimboDancer.Agentic.CognitiveRuntime plane runtime.

It translates the Plane Runtime Design into:

- concrete runtime types;
- required interfaces;
- required fields;
- lifecycle contracts;
- validation rules;
- authority transitions;
- diagnostic contracts;
- error and disposition semantics;
- dependency constraints;
- minimum audit fields;
- conformance tests;
- migration requirements for the current MCP runtime.

This specification is normative.

Where the design document explains architectural intent, this document defines the minimum behavior required for a conforming implementation.

## 2. Conformance Language

The terms **MUST**, **MUST NOT**, **REQUIRED**, **SHOULD**, **SHOULD NOT**, and **MAY** are normative.

A component is conformant only when all applicable **MUST** and **MUST NOT** requirements are satisfied.

## 3. Specification Scope

This specification covers the runtime boundary from admitted request through a terminal outcome, including evidence-backed domain conclusion or authorized execution and effect verification.

It covers:

```text
Interaction
Reasoning
Semantic
Decision
Execution
State
Governance
Diagnostics
Orchestration
Audit / Replay
```

Implementation principle: preserve authority boundaries while minimizing machinery. Components SHOULD be introduced only when they enforce a required boundary or satisfy a concrete current use case.

The runtime capability horizon includes evidence-backed domain adjudication as well as governed action. A domain question may terminate with a DomainConclusion without producing a state-changing action. This specification does not yet require a concrete DomainConclusion C# contract in the first implementation slice, but implementations MUST preserve the distinction between semantic conclusion and execution authority.

The ASL reference-domain requirements in `docs/ASL/legacy-limbodancer-mcp-system-design.md` are the initial end-to-end fitness criteria for that horizon. They do not override this specification's authority model or prescribe runtime structure.

It does not prescribe:

- a specific LLM provider;
- a specific Decision provider;
- a specific policy engine product;
- a one-project-per-plane source layout;
- a particular persistence implementation for audit;
- a particular ontology persistence mechanism;
- a particular deployment topology;
- exact signatures for supporting conceptual types not explicitly defined as required contracts in this document.

## 4. Platform, Project, and Dependency Requirements

### 4.1 Target Framework

All newly created production projects for the new architecture MUST initially target:

```xml
<TargetFramework>net10.0</TargetFramework>
```

The implementation baseline is .NET 10 LTS and C# 14.

Projects MUST remain on supported .NET 10 servicing levels.

### SPEC-PLAT-1

New production projects MUST target `net10.0` until the .NET 11 GA upgrade checkpoint is completed.

### SPEC-PLAT-2

Preview or release-candidate-only .NET APIs MUST NOT become required production dependencies without a separately documented architectural decision.

### 4.2 .NET 11 GA Upgrade Checkpoint

After .NET 11 General Availability, the implementation MUST perform and record an upgrade assessment covering at least:

- SDK/runtime stability;
- ASP.NET Core;
- EF Core;
- MCP packages;
- AI/model-provider packages;
- Azure SDK dependencies;
- test infrastructure;
- deployment platform support;
- diagnostics and observability libraries.

The assessment MAY approve migration to `net11.0`.

### SPEC-PLAT-3

A move to `net11.0` MUST be an explicit repository-wide framework decision, not an accidental per-project divergence.

### 4.3 New Namespace Family

All newly authored production runtime code MUST use the root namespace:

```text
LimboDancer
```

The product/repository identity remains `LimboDancer.Agentic.CognitiveRuntime`; this does not require repeating `Agentic.CognitiveRuntime` in every .NET namespace.

Subnamespaces MAY include:

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Semantics
LimboDancer.Diagnostics
LimboDancer.Decision
LimboDancer.Execution
LimboDancer.Observations
LimboDancer.State.*
LimboDancer.Adapters.*
```

### SPEC-PLAT-4

New production runtime types MUST use the `LimboDancer.*` namespace family and MUST NOT be introduced under `LimboDancer.MCP.*`.

### 4.4 Zero Legacy Production Dependency

The implementation is a clean reimplementation.

No new production project under the new `LimboDancer.*` project family may reference a `LimboDancer.MCP.*` project.

The dependency boundary is:

```text
Legacy source/reference               New production runtime
LimboDancer.MCP.*                     LimboDancer.*
        |                                          ^
        | inspect/copy/refactor/test               |
        +------------------------------------------+
                 source transfer only

NO project reference crosses this boundary.
```

### SPEC-PLAT-5

A conforming new `LimboDancer.*` production project MUST have zero project references to `LimboDancer.MCP.*`.

### SPEC-PLAT-6

A conforming new `LimboDancer.*` production assembly MUST NOT require a `LimboDancer.MCP.*` assembly at runtime.

### SPEC-PLAT-7

CI MUST include an architectural dependency test that fails if a new production project references a legacy `LimboDancer.MCP.*` project.

### 4.5 Legacy Code Usage

Legacy source MAY be inspected, copied, or used to derive tests and compatibility fixtures.

Copied code MUST be treated as newly admitted code.

Before admission, copied code MUST be reviewed for:

- correct plane/fabric ownership;
- dependency direction;
- tenant isolation;
- fail-closed semantics;
- Governance boundaries;
- Diagnostic hooks;
- Execution Gate compatibility;
- asynchronous/cancellation behavior;
- current .NET APIs;
- testability.

### SPEC-PLAT-8

Copying a legacy file and changing only its namespace MUST NOT be considered sufficient architectural migration.

### SPEC-PLAT-9

Any copied behavior that conflicts with the Plane Runtime Design or this specification MUST be changed rather than preserved for compatibility.

### 4.6 Host-Neutral Contracts

Core runtime contracts MUST remain host-neutral and MUST NOT reference:

- ASP.NET controller types;
- MCP SDK types;
- Azure Search SDK types;
- Gremlin SDK types;
- EF Core types;
- Jev SDK types;
- OpenAI or other model-vendor SDK types.

Interaction adapters and infrastructure implementations MUST depend toward runtime/application contracts.

### SPEC-PLAT-10

Protocol and infrastructure SDK types MUST NOT leak into core runtime authority contracts.

### 4.7 Legacy Retirement

The target end state is removal of the legacy `LimboDancer.MCP.*` production projects.

Deletion readiness MUST require:

- specification conformance;
- required feature parity;
- tenant-isolation conformance;
- protocol compatibility where required;
- data/state migration validation;
- operational diagnostics;
- deployment validation;
- replacement of all required legacy runtime behaviors.

### SPEC-PLAT-11

Legacy deletion MUST be treated as a planned completion milestone.

### SPEC-PLAT-12

No legacy project may be retained solely because the new runtime accidentally depends on it.

## 5. Canonical Identifier Types

The runtime MUST use stable identifiers for authority-bearing entities.

At minimum:

```csharp
public readonly record struct RuntimeInvocationId(Guid Value);
public readonly record struct GoalId(Guid Value);
public readonly record struct StepId(Guid Value);
public readonly record struct CorrelationId(string Value);
public readonly record struct ActionId(string Value);
public readonly record struct ActionVersion(string Value);
public readonly record struct DiagnosticCheckId(string Value);
```

Equivalent strongly typed representations MAY be used.

Raw strings MAY be used at transport boundaries but SHOULD be converted to typed identifiers before entering the runtime.

### SPEC-ID-1

`ActionId` MUST identify the semantic action, not the MCP tool name or executor type.

### SPEC-ID-2

`ActionVersion` MUST be immutable for a published ActionDescriptor.

### SPEC-ID-3

A consequential execution MUST record both `ActionId` and `ActionVersion`.

## 6. Goal Contract

A Goal MUST contain enough information to identify intent, tenant, origin, and correlation.

Minimum conceptual contract:

```csharp
public sealed record Goal(
    GoalId Id,
    CorrelationId CorrelationId,
    Guid TenantId,
    string? SessionId,
    GoalOrigin Origin,
    string Intent,
    JsonElement Inputs,
    DateTimeOffset CreatedAt);
```

`GoalOrigin` MUST distinguish at least:

```text
Mcp
Http
Cli
Blazor
Scheduler
Agent
System
Other
```

### SPEC-GOAL-1

`TenantId` MUST NOT be `Guid.Empty` for a tenant-scoped Goal.

### SPEC-GOAL-2

`Intent` MUST NOT be null, empty, or whitespace.

### SPEC-GOAL-3

A Goal MUST NOT contain executable authority such as caller-defined risk classification, authoritative effects, or authorization grants.

### SPEC-GOAL-4

The runtime MUST preserve Goal identity through all steps generated for that Goal.

## 7. Orchestration Context

The runtime MUST carry a typed orchestration context.

Minimum conceptual contract:

```csharp
public sealed record OrchestrationContext
{
    public required Goal Goal { get; init; }
    public required RuntimePrincipal Principal { get; init; }
    public required RuntimeBudget Budget { get; init; }
    public required GoalLifecycleState State { get; init; }

    public IReadOnlyList<Observation> Observations { get; init; }
        = Array.Empty<Observation>();

    public StepId? CurrentStepId { get; init; }
    public Plan? Plan { get; init; }
    public DecisionResult? Decision { get; init; }
}
```

Implementations MAY use immutable snapshots or controlled mutable state.

### SPEC-CTX-1

The context MUST NOT expose an unrestricted mutable dictionary as the primary inter-plane contract.

### SPEC-CTX-2

Tenant identity MUST be immutable after admission.

### SPEC-CTX-3

Correlation identity MUST be preserved through diagnostics, decision, execution, audit, and verification.

### SPEC-CTX-4

Sensitive provider prompts or hidden model reasoning MUST NOT be required fields.

## 8. Goal Lifecycle State Machine

The runtime MUST support the following logical states:

```csharp
public enum GoalLifecycleState
{
    Admitted,
    Observing,
    Reasoning,
    Resolving,
    Constraining,
    Deciding,
    AwaitingConfirmation,
    Gating,
    Executing,
    Verifying,
    Completed,
    Abstained,
    Escalated,
    Failed,
    Cancelled
}
```

Equivalent names MAY be used if semantics are preserved.

Allowed transitions MUST be explicitly validated.

Minimum autonomous Goal flow:

```text
Admitted
  -> Observing
  -> Reasoning
  -> Resolving
  -> Constraining
  -> Deciding
  -> Gating
  -> Executing
  -> Verifying
  -> Completed
```

Directed invocation does not require the Reasoning or Decision states. Its minimum execution progression is:

```text
Admitted
  -> Constraining
  -> Gating
  -> Executing
  -> Verifying
  -> Completed
```

A directed invocation MAY still be associated with a Goal, but it MUST NOT be forced into a synthetic Goal solely for correlation. RuntimeInvocationId is the common correlation identity for both modes.

Additional valid transitions include:

```text
Deciding -> Abstained
Deciding -> Escalated
Gating -> AwaitingConfirmation
AwaitingConfirmation -> Gating
Gating -> Observing          // stale state
Executing -> Failed
Verifying -> Reasoning       // multi-step continuation
any non-terminal -> Cancelled
any non-terminal -> Failed   // unrecoverable failure
```

### SPEC-LC-1

Every lifecycle transition MUST be auditable.

### SPEC-LC-2

Invalid state transitions MUST fail deterministically.

### SPEC-LC-3

Diagnostics MAY execute at a transition without becoming a lifecycle state.

### SPEC-LC-4

A terminal Goal MUST include a structured terminal reason.

## 9. Observation Contract

Minimum conceptual contract:

```csharp
public sealed record Observation(
    string ObservationId,
    ObservationSource Source,
    Guid TenantId,
    DateTimeOffset ObservedAt,
    string? ResourceId,
    string? Version,
    JsonElement Data,
    string? Provenance);
```

### SPEC-OBS-1

All tenant-scoped observations MUST carry the tenant identity used to obtain them.

### SPEC-OBS-2

Where the underlying system exposes an ETag, revision, row version, or equivalent concurrency token, the Observation SHOULD preserve it.

### SPEC-OBS-3

Observation producers MUST NOT return cross-tenant state.

### SPEC-OBS-4

Observation provenance SHOULD be sufficient to identify the source system or query class without requiring sensitive query contents to be persisted.

### 9.1 Domain Conclusion Capability Horizon

A DomainConclusion is a semantic interpretation grounded in authoritative material, observations, applicable rules and exceptions, and deterministic calculations.

### SPEC-CONCL-1

A DomainConclusion MUST NOT be treated as an ActionCandidate, PermittedAction, SelectedAction, AuthorizedAction, or permission to mutate state.

### SPEC-CONCL-2

An implementation claiming reference-domain adjudication conformance MUST preserve or reference the question evaluated, conclusion disposition, material evidence, applicable rules and exceptions, ontology and state versions, assumptions, unresolved ambiguity, and explanation provenance.

### SPEC-CONCL-3

Missing, stale, ambiguous, or conflicting material evidence MUST NOT be converted into a definitive conclusion without an explicit, deterministic basis. The runtime MUST support a qualified, indeterminate, re-observe, or abstention outcome.

### SPEC-CONCL-4

A mutation requested after a DomainConclusion MUST independently enter the applicable directed or autonomous authority path and revalidate material state.

### SPEC-CONCL-5

The concrete DomainConclusion contract and persistence model MAY be deferred until a reference-domain implementation slice, provided earlier runtime contracts do not preclude these requirements.

## 10. Semantic Action Identity

Each executable semantic capability MUST have a stable ActionId.

Initial working mappings are:

| MCP tool | ActionId |
|---|---|
| `history_get` | `ldm:action/HistoryRead` |
| `history_append` | `ldm:action/HistoryAppend` |
| `graph_query` | `ldm:action/GraphQuery` |
| `memory_search` | `ldm:action/MemorySearch` |

These identifiers MAY be revised before ontology publication, but once published they MUST be versioned rather than silently repurposed.

## 11. Action Risk Profile

Risk characteristics are orthogonal and MUST NOT be represented as a single mutually exclusive severity enum.

Minimum conceptual model:

```csharp
public enum ActionMutability { ReadOnly, Write }
public enum ActionIdempotency { Idempotent, NonIdempotent }
public enum ActionReversibility { Reversible, Compensatable, Irreversible }
public enum ActionBoundary { Internal, ExternalSideEffect }
public enum ActionPrivilege { Normal, Privileged }

public sealed record ActionRiskProfile(
    ActionMutability Mutability,
    ActionIdempotency Idempotency,
    ActionReversibility Reversibility,
    ActionBoundary Boundary,
    ActionPrivilege Privilege);
```

Governance MAY derive a policy-specific risk level from this profile, but the derived level MUST NOT erase the underlying dimensions.

### SPEC-RISK-1

Risk characteristics MUST come from trusted server-side action metadata or Governance policy.

### SPEC-RISK-2

Caller input MUST NOT reduce or rewrite any ActionRiskProfile dimension.

### SPEC-RISK-3

A provider MUST NOT alter ActionRiskProfile.

### SPEC-RISK-4

ActionRiskProfile MUST be available to the Execution Gate and Diagnostic Policy.

## 12. ActionDescriptor Contract

A conforming runtime MUST provide a server-authoritative ActionDescriptor.

Minimum conceptual contract:

```csharp
public sealed record ActionDescriptor
{
    public required ActionId Id { get; init; }
    public required ActionVersion Version { get; init; }

    public required string Name { get; init; }
    public string? Description { get; init; }

    public required JsonElement InputSchema { get; init; }
    public JsonElement? OutputSchema { get; init; }

    public required ActionRiskProfile Risk { get; init; }

    public IReadOnlySet<string> RequiredPermissions { get; init; }
        = new HashSet<string>();

    public IReadOnlyList<PreconditionDescriptor> Preconditions { get; init; }
        = Array.Empty<PreconditionDescriptor>();

    public IReadOnlyList<EffectDescriptor> ExpectedEffects { get; init; }
        = Array.Empty<EffectDescriptor>();

    public required IdempotencyMode Idempotency { get; init; }

    public CompensationDescriptor? Compensation { get; init; }

    public required ExecutorBinding Executor { get; init; }

    public DiagnosticProfile Diagnostics { get; init; }
        = DiagnosticProfile.Empty;

    public VerificationProfile Verification { get; init; }
        = VerificationProfile.Default;
}
```

### SPEC-ACT-1

Every registered executor exposed for runtime use MUST be reachable through an ActionDescriptor.

### SPEC-ACT-2

ActionDescriptor instances MUST be treated as immutable after publication.

### SPEC-ACT-3

Unknown ActionIds MUST fail closed.

### SPEC-ACT-4

ActionDescriptor input and output schemas MUST be transport-neutral.

### SPEC-ACT-5

Preconditions, expected effects, permissions, risk, executor binding, diagnostic profile, and verification profile MUST NOT be replaceable by caller-supplied values.

### SPEC-ACT-6

The ActionDescriptor registry MUST reject duplicate `ActionId + ActionVersion` registrations.

### SPEC-ACT-7

A descriptor MUST NOT be considered executable unless its executor binding resolves.

## 13. Action Registry

Required interface:

```csharp
public interface IActionRegistry
{
    bool TryGet(
        ActionId id,
        ActionVersion? version,
        out ActionDescriptor descriptor);

    IReadOnlyList<ActionDescriptor> List();
}
```

Equivalent asynchronous implementations MAY be used if descriptors are externally persisted.

### SPEC-REG-1

Lookup by ActionId without version MUST resolve according to an explicit version-selection policy.

### SPEC-REG-2

The selected version MUST be recorded in execution audit.

### SPEC-REG-3

Registry publication MUST run structural diagnostics before making a descriptor available.

### SPEC-REG-4

A descriptor with unresolved semantic mappings, executor binding, or required diagnostic check MUST NOT be published unless explicitly marked non-executable.

## 14. Protocol Action Binding

Protocol-facing names MUST map to semantic actions through an explicit binding.

Minimum conceptual contract:

```csharp
public sealed record ActionBinding(
    string Protocol,
    string ExternalName,
    ActionId ActionId,
    ActionVersion? Version);
```

Required interface:

```csharp
public interface IActionBindingRegistry
{
    bool TryResolve(
        string protocol,
        string externalName,
        out ActionBinding binding);
}
```

### SPEC-BIND-1

Bindings MUST NOT contain authoritative risk, effects, permissions, or preconditions.

### SPEC-BIND-2

Unknown protocol bindings MUST fail closed.

### SPEC-BIND-3

Multiple external bindings MAY map to the same ActionId.

## 15. Grounded ActionCandidate

Minimum conceptual contract:

```csharp
public sealed record ActionCandidate
{
    public required string CandidateId { get; init; }
    public required ActionDescriptor Descriptor { get; init; }
    public required JsonElement Arguments { get; init; }

    public IReadOnlyList<string> EvidenceRefs { get; init; }
        = Array.Empty<string>();

    public IReadOnlyDictionary<string, string> StateVersions { get; init; }
        = new Dictionary<string, string>();
}
```

### SPEC-CAND-1

CandidateId MUST be unique within a Decision request.

### SPEC-CAND-2

Arguments MUST validate against the descriptor input schema before the candidate is executable.

### SPEC-CAND-3

A candidate MUST NOT carry caller-authored authoritative policy.

### SPEC-CAND-4

Relevant state versions SHOULD be carried into the final gate.

## 16. Action Resolution

Required interface:

```csharp
public interface IActionResolver
{
    Task<ActionResolutionResult> ResolveAsync(
        ActionResolutionContext context,
        CancellationToken ct = default);
}
```

Minimum result:

```csharp
public sealed record ActionResolutionResult(
    IReadOnlyList<ActionCandidate> Candidates,
    IReadOnlyList<ResolutionFinding> Findings);
```

### SPEC-RES-1

Resolution MUST return a finite candidate set.

### SPEC-RES-2

Unknown ontology vocabulary MUST fail closed when it is required to resolve an executable action.

### SPEC-RES-3

Ontology identifiers MUST NOT silently fall through to physical graph property names, executor names, or storage identifiers.

### SPEC-RES-4

Resolution findings MUST be auditable.

### SPEC-RES-5

No candidate MUST be synthesized for an unregistered action.

## 17. Preconditions

PreconditionDescriptor MUST identify its authority class.

Minimum conceptual shape:

```csharp
public enum PreconditionKind
{
    Semantic,
    Governance,
    Operational
}

public sealed record PreconditionDescriptor(
    string Id,
    PreconditionKind Kind,
    string EvaluatorId,
    JsonElement Parameters,
    bool Required);
```

### SPEC-PRE-1

Required preconditions MUST fail closed when their evaluator cannot be resolved.

### SPEC-PRE-2

Caller-supplied preconditions MAY add restrictions but MUST NOT remove or weaken descriptor preconditions.

### SPEC-PRE-3

Precondition evaluation MUST produce structured results.

### SPEC-PRE-4

Execution-sensitive required preconditions MUST be re-evaluated by or immediately before the Execution Gate.

## 18. Constraint Pipeline

Required conceptual interface:

```csharp
public interface IActionConstraintPipeline
{
    Task<ConstraintPipelineResult> EvaluateAsync(
        IReadOnlyList<ActionCandidate> candidates,
        ConstraintContext context,
        CancellationToken ct = default);
}
```

Minimum result:

```csharp
public sealed record ConstraintPipelineResult(
    IReadOnlyList<PermittedAction> Permitted,
    IReadOnlyList<RejectedCandidate> Rejected);
```

### SPEC-CON-1

A failed hard semantic or Governance constraint MUST reject the candidate.

### SPEC-CON-2

Rejected candidates MUST NOT be passed to IDecisionProvider.

### SPEC-CON-3

Each rejection MUST identify candidate ID, constraint ID, authority class, and reason code.

### SPEC-CON-4

An empty permitted set MUST bypass the Decision provider and produce a runtime/orchestration no-permitted-action outcome. It MUST NOT create a provider DecisionResult.

## 19. PermittedAction

Minimum conceptual representation:

```csharp
public sealed record PermittedAction(
    ActionCandidate Candidate,
    IReadOnlyList<ConstraintResult> ConstraintResults);
```

### SPEC-PERM-1

Creation of a PermittedAction MUST be controlled by the constraint pipeline or an equivalent trusted component.

### SPEC-PERM-2

A caller MUST NOT be able to submit a serialized PermittedAction and thereby bypass constraint evaluation.

## 20. Decision Contract

Required outcome type:

```csharp
public enum DecisionOutcome
{
    Selected,
    Abstained,
    Escalated
}
```

Required conceptual result:

```csharp
public sealed record DecisionResult
{
    public required DecisionOutcome Outcome { get; init; }
    public string? SelectedCandidateId { get; init; }

    public double? Confidence { get; init; }

    public IReadOnlyDictionary<string, double>? Distribution { get; init; }

    public required string ProviderId { get; init; }
    public string? ProviderVersion { get; init; }

    public required string ReasonCode { get; init; }

    public TimeSpan? Latency { get; init; }
    public decimal? Cost { get; init; }
}
```

Required provider interface:

```csharp
public interface IDecisionProvider
{
    Task<DecisionResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken ct = default);
}
```

### SPEC-DEC-1

`SelectedCandidateId` MUST be null unless Outcome is `Selected`.

### SPEC-DEC-2

For Outcome `Selected`, SelectedCandidateId MUST reference exactly one supplied candidate.

### SPEC-DEC-3

A provider return selecting an unknown candidate MUST be rejected as an invalid provider result.

### SPEC-DEC-4

Confidence MUST NOT be treated as authorization.

### SPEC-DEC-5

Provider error and provider abstention MUST remain distinct outcomes.

### SPEC-DEC-6

The Decision provider MUST NOT receive rejected candidates.

### SPEC-DEC-7

Provider SDK types MUST NOT appear in the public Decision contract.

## 21. Directed Invocation Contract

Directed invocation MUST resolve an explicit external action name to an ActionDescriptor.

A Decision provider MUST NOT be invoked solely to confirm explicit caller selection.

Required flow:

```text
binding
-> descriptor
-> argument validation
-> constraints
-> SelectedAction
-> pre-flight diagnostics
-> execution gate
-> AuthorizedAction
-> execute
-> post-flight diagnostics
-> verification
-> audit
```

### SPEC-DIR-1

Directed invocation MUST pass the same Execution Gate used by autonomous invocation.

### SPEC-DIR-2

Directed invocation MUST NOT bypass tenant, authorization, precondition, diagnostic, or concurrency enforcement.

## 22. SelectedAction Contract

Minimum conceptual representation:

```csharp
public sealed record SelectedAction
{
    public required ActionCandidate Candidate { get; init; }
    public required SelectionOrigin Origin { get; init; }
    public DecisionResult? Decision { get; init; }
}
```

`SelectionOrigin` MUST distinguish at least:

```text
DirectedCaller
DecisionProvider
SystemRule
```

### SPEC-SEL-1

Autonomous selection MUST include the DecisionResult.

### SPEC-SEL-2

Directed selection MUST identify the authenticated caller through execution context, even though no DecisionResult exists.

## 23. Diagnostics Core Contract

The runtime MUST support registered diagnostic checks.

Required types:

```csharp
public enum DiagnosticOutcome
{
    Pass,
    Fail,
    Indeterminate
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum DiagnosticPosition
{
    PreFlight,
    InFlight,
    PostFlight
}

public enum DiagnosticDisposition
{
    Continue,
    ContinueDegraded,
    Retry,
    ReObserve,
    Escalate,
    Block,
    FailGoal
}
```

A diagnostic phase representation MUST identify the runtime lifecycle phase.

Minimum result:

```csharp
public sealed record DiagnosticFinding
{
    public required DiagnosticCheckId CheckId { get; init; }
    public required string CheckVersion { get; init; }
    public required DiagnosticOutcome Outcome { get; init; }
    public required DiagnosticSeverity Severity { get; init; }
    public required string Code { get; init; }
    public required string Summary { get; init; }

    public IReadOnlyDictionary<string, JsonElement> Evidence { get; init; }
        = new Dictionary<string, JsonElement>();

    public required DateTimeOffset Timestamp { get; init; }
}
```

Required check interface:

```csharp
public interface IDiagnosticCheck<in TContext>
{
    DiagnosticCheckId Id { get; }
    string Version { get; }
    DiagnosticPosition Position { get; }

    Task<DiagnosticFinding> EvaluateAsync(
        TContext context,
        CancellationToken ct = default);
}
```

### SPEC-DX-1

Diagnostic checks MUST have stable IDs.

### SPEC-DX-2

Published diagnostic behavior MUST be version identifiable.

### SPEC-DX-3

A diagnostic finding MUST NOT itself grant permission.

### SPEC-DX-4

Diagnostic evidence MUST be serializable or referentially auditable.

### SPEC-DX-5

A hard-invariant diagnostic with Outcome `Fail` MUST result in a blocking disposition.

### SPEC-DX-6

A hard-invariant diagnostic with Outcome `Indeterminate` due to unresolved state, timeout, unavailable evaluator, or missing evidence MUST fail closed. Hard invariants do not have degraded-mode exceptions.

### SPEC-DX-7

Diagnostics MUST observe runtime budgets.

### SPEC-DX-8

Diagnostic checks MUST NOT recursively trigger unbounded diagnostic execution.

## 24. Diagnostic Context

Minimum conceptual contract:

```csharp
public sealed record DiagnosticContext
{
    public required RuntimeInvocationId InvocationId { get; init; }
    public GoalId? GoalId { get; init; }
    public StepId? StepId { get; init; }
    public required CorrelationId CorrelationId { get; init; }
    public required Guid TenantId { get; init; }

    public required GoalLifecycleState Phase { get; init; }
    public required DiagnosticPosition Position { get; init; }

    public ActionDescriptor? Descriptor { get; init; }
    public ActionCandidate? Candidate { get; init; }
    public SelectedAction? SelectedAction { get; init; }

    public IReadOnlyList<Observation> Observations { get; init; }
        = Array.Empty<Observation>();

    public ActionExecutionResult? ExecutionResult { get; init; }
}
```

### SPEC-DXC-1

DiagnosticContext MUST expose only context appropriate to the check being run.

### SPEC-DXC-2

DiagnosticContext MUST preserve tenant and correlation identity.

### SPEC-DXC-3

Diagnostics MUST NOT use DiagnosticContext as an unrestricted route to global State.

## 25. Diagnostic Runner and Profiles

The first implementation MUST expose a small diagnostic execution boundary rather than requiring a general-purpose plugin registry.

Required conceptual interface:

```csharp
public interface IDiagnosticRunner
{
    Task<IReadOnlyList<DiagnosticFinding>> RunAsync(
        DiagnosticContext context,
        DiagnosticProfile profile,
        CancellationToken ct = default);
}
```

ActionDescriptor MUST support a DiagnosticProfile.

Minimum conceptual form:

```csharp
public sealed record DiagnosticReference(
    DiagnosticCheckId Id,
    string? Version,
    bool Required,
    GoalLifecycleState? Phase,
    DiagnosticPosition Position);

public sealed record DiagnosticProfile(
    IReadOnlyList<DiagnosticReference> Checks);
```

The runner MAY use an internal registry or dependency-injection collection of IDiagnosticCheck implementations. A separately public diagnostic-registry interface is not required for v1.

### SPEC-DXP-1

Required diagnostic references MUST resolve before an ActionDescriptor is considered executable.

### SPEC-DXP-2

Caller-provided diagnostic references MUST NOT replace authoritative profile entries.

### SPEC-DXP-3

Missing optional diagnostics MAY generate a warning finding but MUST NOT silently masquerade as successful execution of the diagnostic.

### SPEC-DXP-4

Diagnostic registration MUST preserve check type safety. A normative public contract MUST NOT resolve checks as untyped object values.

## 26. Diagnostic Policy

Required conceptual interface:

```csharp
public interface IDiagnosticPolicy
{
    DiagnosticDisposition Evaluate(
        DiagnosticFinding finding,
        DiagnosticPolicyContext context);
}
```

### SPEC-DPOL-1

Diagnostic Policy MUST NOT create an authority path that bypasses Governance or the Execution Gate. Hard-invariant diagnostics always fail closed on Fail or Indeterminate; policy discretion applies only to non-hard diagnostics.

### SPEC-DPOL-2

`Block` on consequential execution MUST be enforced through the Execution Gate or equivalent Governance boundary.

### SPEC-DPOL-3

`Retry`, `ReObserve`, and `Escalate` MUST be coordinated by Orchestration.

### SPEC-DPOL-4

The disposition applied to a consequential finding MUST be audited.

## 27. Minimum Plane Diagnostic Set

A conforming first implementation MUST provide at least the following checks.

### Interaction

- tenant resolved;
- authenticated principal present where required;
- action binding resolves;
- input schema valid.

### Reasoning

- repeated-plan or repeated-next-step loop detection;
- runtime budget still available;
- unresolved semantic/entity references identified;
- requested capability can be represented by known semantic actions.

### Semantic

- ActionDescriptor registered;
- descriptor version valid;
- required ontology predicates map;
- required effects map;
- executor binding declared.

### Decision

- candidate IDs unique;
- all decision candidates are PermittedActions;
- selected candidate belongs to submitted candidate set;
- provider result shape valid.

### Execution

- tenant present;
- descriptor version current;
- executor resolves;
- critical mutable preconditions current;
- required confirmation valid;
- idempotency data present when required.

### State

- tenant scope present for relevant reads and writes;
- relevant backing store reachable when execution requires it.

### SPEC-DSET-1

Additional diagnostics SHOULD be added per action risk and State technology.

### SPEC-DSET-2

Tenant-isolation diagnostics MUST be hard-invariant checks.

## 28. ExecutionContext

Minimum conceptual contract:

```csharp
public sealed record ExecutionContext
{
    public required RuntimeInvocationId InvocationId { get; init; }
    public GoalId? GoalId { get; init; }
    public StepId? StepId { get; init; }
    public required CorrelationId CorrelationId { get; init; }

    public required Guid TenantId { get; init; }
    public required RuntimePrincipal Principal { get; init; }

    public required RuntimeBudget Budget { get; init; }

    public IReadOnlyList<Observation> Observations { get; init; }
        = Array.Empty<Observation>();

    public IReadOnlyList<DiagnosticFinding> DiagnosticFindings { get; init; }
        = Array.Empty<DiagnosticFinding>();
}
```

### SPEC-EXECCTX-1

When GoalId is present, ExecutionContext tenant MUST equal the Goal tenant. Directed invocations without a Goal MUST still carry the admitted immutable tenant.

### SPEC-EXECCTX-2

ExecutionContext MUST NOT permit transport callers to replace RuntimePrincipal or tenant.

## 29. Execution Gate

Required interface:

```csharp
public interface IExecutionGate
{
    Task<ExecutionGateResult> AuthorizeAsync(
        SelectedAction action,
        ExecutionContext context,
        CancellationToken ct = default);
}
```

Minimum result:

```csharp
public enum ExecutionGateOutcome
{
    Authorized,
    Denied,
    Stale,
    ConfirmationRequired,
    DiagnosticBlocked
}

public sealed record ExecutionGateResult(
    ExecutionGateOutcome Outcome,
    AuthorizedAction? AuthorizedAction,
    IReadOnlyList<string> ReasonCodes);
```

### SPEC-GATE-1

Only Outcome `Authorized` MAY contain AuthorizedAction.

### SPEC-GATE-2

The gate MUST validate tenant.

### SPEC-GATE-3

The gate MUST validate applicable authorization/permissions.

### SPEC-GATE-4

The gate MUST validate required preconditions against current execution-sensitive state.

### SPEC-GATE-5

The gate MUST validate descriptor version.

### SPEC-GATE-6

The gate MUST apply required pre-flight diagnostic dispositions.

### SPEC-GATE-7

The gate MUST apply confirmation policy.

### SPEC-GATE-8

The gate MUST apply risk/confidence policy for autonomous selections.

### SPEC-GATE-9

The gate MUST fail closed if a required authoritative dependency cannot be evaluated.

### SPEC-GATE-10

A stale state result MUST NOT produce AuthorizedAction.

## 30. AuthorizedAction

Minimum conceptual representation:

```csharp
public sealed record AuthorizedAction
{
    public required string AuthorizationId { get; init; }
    public required SelectedAction Selected { get; init; }

    public required Guid TenantId { get; init; }

    public required DateTimeOffset AuthorizedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    public IReadOnlyDictionary<string, string> ValidatedStateVersions { get; init; }
        = new Dictionary<string, string>();
}
```

### SPEC-AUTH-1

AuthorizedAction MUST be runtime-created.

### SPEC-AUTH-2

Transport deserialization MUST NOT allow a caller to manufacture an AuthorizedAction accepted by an executor. In-process v1 implementations SHOULD enforce this by construction using internal constructors/factories or otherwise inaccessible creation paths; cryptographic authorization envelopes are NOT required unless execution crosses a trust/process boundary.

### SPEC-AUTH-3

Authorization SHOULD expire for delayed high-risk execution.

### SPEC-AUTH-4

Execution MUST preserve AuthorizationId in audit.

## 31. Executor Contract

Required interface:

```csharp
public interface IActionExecutor
{
    ActionId ActionId { get; }

    Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken ct = default);
}
```

Minimum result:

```csharp
public sealed record ActionExecutionResult
{
    public required bool Succeeded { get; init; }
    public required string Code { get; init; }

    public JsonElement? Output { get; init; }

    public IReadOnlyList<string> ProducedResourceIds { get; init; }
        = Array.Empty<string>();

    public string? IdempotencyKey { get; init; }

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}
```

### SPEC-EXE-1

An executor MUST reject execution lacking valid runtime authorization.

### SPEC-EXE-2

An executor MUST NOT invoke a Decision provider to decide whether to proceed.

### SPEC-EXE-3

Executor output MUST validate against the descriptor output schema when one is defined.

### SPEC-EXE-4

Non-idempotent actions MUST expose enough execution identity to reconcile uncertain timeout outcomes before retry.

## 32. Effects

Minimum conceptual descriptor:

```csharp
public sealed record EffectDescriptor(
    string Id,
    string EffectType,
    JsonElement Definition,
    bool RequiredVerification);
```

### SPEC-EFF-1

Expected effects MUST originate from trusted ActionDescriptor metadata or trusted semantic definitions.

### SPEC-EFF-2

Caller-supplied effect definitions MUST NOT replace authoritative effects.

### SPEC-EFF-3

Execution success MUST NOT automatically mark effects verified.

## 33. Effect Verification

Required interface:

```csharp
public interface IEffectVerifier
{
    Task<EffectVerificationResult> VerifyAsync(
        AuthorizedAction action,
        ActionExecutionResult execution,
        IReadOnlyList<EffectDescriptor> expectedEffects,
        VerificationContext context,
        CancellationToken ct = default);
}
```

Required statuses:

```csharp
public enum VerificationStatus
{
    Verified,
    PartiallyVerified,
    Unverifiable,
    Contradicted
}
```

### SPEC-VER-1

Required verifiable effects SHOULD be checked after execution.

### SPEC-VER-2

`Contradicted` MUST be auditable and MUST invoke escalation, recovery, or compensation policy for actions whose risk policy requires it.

### SPEC-VER-3

Effect Verification MUST remain distinct from Diagnostics.

## 34. TOCTOU and State Versions

The runtime MUST assume state can change between observation and execution.

### SPEC-TOC-1

Where a required precondition depends on mutable State, the final gate MUST re-read or otherwise validate current State.

### SPEC-TOC-2

If a relevant version token differs from the candidate's observed version, the gate MUST return `Stale` unless the precondition can be safely re-evaluated and remains valid under policy.

### SPEC-TOC-3

A stale selection MUST NOT be executed using prior authorization.

### SPEC-TOC-4

Orchestration SHOULD respond to `Stale` with re-observation and renewed resolution/selection as appropriate.

## 35. Confirmation Contract

Confirmation MUST be treated as Governance evidence.

Minimum conceptual token:

```csharp
public sealed record ConfirmationGrant(
    string ConfirmationId,
    Guid TenantId,
    ActionId ActionId,
    ActionVersion ActionVersion,
    string ArgumentHash,
    string PrincipalId,
    DateTimeOffset GrantedAt,
    DateTimeOffset ExpiresAt);
```

### SPEC-CONF-1

Confirmation MUST be bound to tenant, action, version, and action arguments or equivalent stable digest.

### SPEC-CONF-2

Expired confirmation MUST be rejected.

### SPEC-CONF-3

After confirmation, mutable execution conditions MUST still be revalidated.

## 36. Runtime Budget

Minimum conceptual contract:

```csharp
public sealed record RuntimeBudget(
    int MaxSteps,
    DateTimeOffset Deadline,
    long? MaxTokens,
    decimal? MaxCost,
    int MaxExternalCalls,
    int MaxRetries);
```

### SPEC-BUD-1

Budget enforcement MUST be independent of model/provider cooperation.

### SPEC-BUD-2

Budget exhaustion MUST produce a structured outcome.

### SPEC-BUD-3

Diagnostics and provider fallback MUST consume applicable budget.

## 37. Error Contract

Runtime errors MUST be classified.

Required codes/categories MUST cover:

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

### SPEC-ERR-1

Governance denial MUST NOT surface as a generic internal server error.

### SPEC-ERR-2

Decision abstention MUST NOT surface as an execution exception.

### SPEC-ERR-3

Diagnostic blocking MUST identify the responsible DiagnosticCheckId and disposition.

### SPEC-ERR-4

Internal exception details MUST NOT be exposed through public protocol responses by default.

## 38. Retry Contract

Retry policy MUST be stage-aware.

Minimum rules:

| Condition | Required default behavior |
|---|---|
| transient observation failure | bounded retry |
| Governance denial | no unchanged retry |
| failed semantic hard precondition | re-observe/reason or terminate |
| provider timeout | bounded retry or configured fallback |
| required diagnostic transient failure | bounded retry, then fail closed unless degraded policy exists |
| hard diagnostic failure | no blind retry |
| stale State | re-observe |
| idempotent executor transient failure | bounded retry |
| non-idempotent timeout | reconcile execution before retry |
| effect contradiction | escalate/recover according to risk |

### SPEC-RET-1

A generic retry wrapper MUST NOT be applied indiscriminately across all runtime stages.

## 39. Audit Contract

A conforming runtime MUST produce structured audit records for consequential execution.

Minimum logical fields:

```text
AuditId
RuntimeInvocationId
GoalId (when applicable)
StepId (when applicable)
CorrelationId
TenantId
PrincipalId
Origin
LifecycleState
ActionId
ActionVersion
CandidateId (when applicable)
SelectionOrigin
DecisionProviderId (when applicable)
DecisionProviderVersion (when applicable)
DecisionOutcome (when applicable)
Confidence (when applicable)
ActionRiskProfile
ConstraintResults
DiagnosticFindings
DiagnosticDispositions
ConfirmationId
ExecutionGateOutcome
AuthorizationId
ExecutionCode
ExpectedEffects
VerificationStatus
ObservationVersionRefs
StartedAt
CompletedAt
Latency
Cost/TokenMetadata
TerminalReason
```

Fields MAY be normalized across multiple tables/documents/events. Fields marked when applicable MUST NOT be fabricated for directed invocations or other paths where the corresponding stage did not occur.

### SPEC-AUD-1

Audit MUST NOT require hidden chain-of-thought.

### SPEC-AUD-2

Sensitive data MUST be minimized, redacted, hashed, or referenced according to policy.

### SPEC-AUD-3

Denied and blocked consequential actions MUST be auditable, not only successful actions.

### SPEC-AUD-4

Diagnostic findings that affect control flow MUST be audited.

## 40. Replay Contract

Replay MUST allow historical decision boundaries to be reconstructed without performing side effects.

Required replay capabilities:

- reconstruct ActionDescriptor version;
- reconstruct or reference observation versions;
- reconstruct candidate set;
- reconstruct rejected/permitted candidates;
- re-run Decision providers against the preserved candidate set;
- compare original and replay decisions;
- stop before Execution Gate or executor invocation.

### SPEC-RPL-1

Replay MUST NOT execute consequential actions unless explicitly moved into a separately authorized simulation/test environment.

### SPEC-RPL-2

Replay data SHOULD be sufficient for provider regression and calibration analysis.

## 41. Reasoning Contract Boundary

This specification does not prescribe a reasoning model.

However, Reasoning MUST obey the following boundary.

### SPEC-REA-1

Reasoning MAY propose plans, semantic intents, observations needed, and subgoals.

### SPEC-REA-2

Reasoning MUST NOT manufacture AuthorizedAction.

### SPEC-REA-3

Reasoning MUST NOT invoke IActionExecutor directly.

### SPEC-REA-4

Any action proposed by Reasoning MUST be resolved through IActionResolver before it can enter the authority pipeline.

### SPEC-REA-5

Repeated reasoning with materially identical Goal, observation signature, and proposed next step SHOULD trigger loop diagnostics.

## 42. Orchestrator Contract

Required interface:

```csharp
public interface IGoalOrchestrator
{
    Task<GoalResult> RunAsync(
        Goal goal,
        CancellationToken ct = default);
}
```

Minimum GoalResult:

```csharp
public sealed record GoalResult(
    GoalId GoalId,
    GoalLifecycleState TerminalState,
    string ReasonCode,
    JsonElement? Output);
```

### SPEC-ORCH-1

The orchestrator MUST coordinate but MUST NOT independently define semantic applicability.

### SPEC-ORCH-2

The orchestrator MUST NOT override Governance denial.

### SPEC-ORCH-3

The orchestrator MUST NOT alter a Decision result to select an unreturned candidate.

### SPEC-ORCH-4

The orchestrator MUST enforce RuntimeBudget.

### SPEC-ORCH-5

The orchestrator MUST apply diagnostic lifecycle dispositions such as Retry, ReObserve, Escalate, and FailGoal.

### SPEC-ORCH-6

The orchestrator MUST create or preserve StepId for each consequential step.

## 43. New MCP Adapter Integration

The legacy MCP path is behavioral reference only. The new runtime MUST implement a new MCP adapter under the LimboDancer.Agentic.CognitiveRuntime namespace family.

The conforming target path MUST be:

```text
tool name
-> IActionBindingRegistry
-> ActionDescriptor
-> argument validation
-> constraint pipeline
-> SelectedAction(origin=DirectedCaller)
-> required pre-flight diagnostics
-> IExecutionGate
-> AuthorizedAction
-> new action executor
-> applicable post-flight diagnostics
-> effect verification where applicable
-> audit
```

### SPEC-MCP-1

Public MCP tool names MAY remain unchanged where compatibility is required.

### SPEC-MCP-2

The new MCP adapter MUST NOT depend on legacy LimboDancer.MCP.McpServer or legacy tool assemblies.

### SPEC-MCP-3

Directed MCP execution MUST route through the new common Execution Gate.

### SPEC-MCP-4

MCP tool listing SHOULD expose transport-facing schemas without exposing internal authorization types.

## 44. HistoryAppend Reimplementation Specification

The legacy `HistoryAppendTool` accepted caller-supplied `preconditions` and `effects`. This is reference behavior that MUST NOT be reproduced as authoritative semantics.

The target runtime MUST move authoritative preconditions/effects to the ActionDescriptor or associated trusted semantic definitions.

Reimplementation requirements:

1. Define `ldm:action/HistoryAppend`.
2. Register the descriptor.
3. Bind `history_append` to the descriptor.
4. Define authoritative preconditions/effects server-side.
5. If protocol compatibility requires accepting legacy caller preconditions, treat them only as additional restrictive assertions.
6. Never allow caller preconditions to remove authoritative checks.
7. Ignore or reject caller effects for authority purposes.
8. Prefer a clean new protocol shape that omits authoritative caller effects.

### SPEC-HIST-1

No caller payload may define a graph mutation effect that is executed solely because it appeared in the request.

## 45. Tenant Isolation Specification

Tenant isolation MUST be structural.

### SPEC-TEN-1

Tenant MUST be resolved during admission.

### SPEC-TEN-2

Tenant MUST be immutable within a Goal.

### SPEC-TEN-3

All tenant-scoped State reads and writes MUST enforce tenant scope.

### SPEC-TEN-4

The action registry MAY be global, tenant-specific, or layered, but action visibility MUST respect tenant policy.

### SPEC-TEN-5

Diagnostics MUST verify tenant invariants at critical boundaries.

### SPEC-TEN-6

The new history-read implementation MUST enforce tenant filtering explicitly or through a verified global mechanism before autonomous runtime enablement.

### SPEC-TEN-7

The new graph-read implementation MUST enforce tenant isolation before autonomous runtime enablement.

### SPEC-TEN-8

Vector retrieval MUST include tenant filtering.

## 46. Semantic Fail-Closed Specification

### SPEC-SEM-1

An unknown semantic property predicate MUST NOT be treated automatically as a raw graph property key.

### SPEC-SEM-2

An unknown semantic relation MUST NOT be treated automatically as an executor-native edge label.

### SPEC-SEM-3

Required semantic mappings MUST fail closed when unresolved.

### SPEC-SEM-4

The new semantic precondition evaluator MUST NOT reproduce the legacy GraphPreconditionsService raw-predicate fallback.

### SPEC-SEM-5

Skipped unknown semantic filters MUST be reported explicitly and MUST NOT silently broaden a security- or correctness-sensitive query.

## 47. Minimum Structural Diagnostics

At runtime startup or registry publication, the following MUST be diagnosable:

- duplicate ActionDescriptor identity/version;
- unresolved executor binding;
- unresolved required diagnostic reference;
- invalid input/output schema;
- unknown required semantic precondition mapping;
- unknown required effect mapping;
- invalid compensation reference;
- invalid protocol action binding;
- incompatible descriptor/ontology version where compatibility is enforced.

A deployment MAY fail startup when required structural diagnostics fail.

## 48. Minimum Runtime Diagnostics

For every consequential SelectedAction, the runtime MUST be capable of producing findings for:

- tenant present;
- descriptor current;
- binding/executor resolvable;
- argument schema valid;
- required semantic mappings valid;
- required preconditions evaluable;
- required confirmation valid if applicable;
- state version current if applicable;
- idempotency requirements satisfied if applicable;
- provider-selected CandidateId valid for autonomous selection.

## 49. Diagnostic and Verification Separation

The following example is normative in intent:

```text
Expected effect:
    reservation.status == Confirmed

Effect Verification:
    Did reservation.status become Confirmed?

Diagnostics:
    Was execution duplicated?
    Was another tenant affected?
    Was the audit record produced?
    Did graph and relational projections remain coherent?
```

### SPEC-DV-1

Effect Verification MUST answer whether declared semantic outcomes occurred.

### SPEC-DV-2

Diagnostics MUST answer runtime integrity, health, validity, readiness, consistency, or behavioral questions.

### SPEC-DV-3

An implementation MAY reuse the same observation data for both, but MUST preserve separate result semantics.

## 50. Observability Specification

The runtime SHOULD expose at least:

- Goal count and terminal outcomes;
- lifecycle stage latency;
- action resolution candidate count;
- rejected/permitted candidate count;
- Governance denials;
- diagnostic outcome/severity counts;
- diagnostic dispositions;
- Decision provider latency;
- provider fallback count;
- abstention count;
- confidence distribution where meaningful;
- Execution Gate outcomes;
- stale-state count;
- executor latency and errors;
- verification outcome/severity counts;
- retry counts;
- token/cost metrics where available.

### SPEC-OBSERV-1

Telemetry collection MUST NOT be treated as a substitute for explicit Diagnostic Checks.

### SPEC-OBSERV-2

Diagnostic check duration and diagnostic failures SHOULD themselves be observable.

## 51. Provider Escalation

Provider selection and escalation MUST be external to individual Decision providers.

### SPEC-PROV-1

A provider MAY return Selected, Abstained, Escalated, or provider failure.

### SPEC-PROV-2

A provider MUST NOT grant itself permission to execute.

### SPEC-PROV-3

Provider fallback MUST consume RuntimeBudget.

### SPEC-PROV-4

Risk policy MAY restrict which providers are eligible for a Decision.

## 52. Security Requirements

### SPEC-SEC-1

Model output MUST be treated as untrusted with respect to execution authority.

### SPEC-SEC-2

Caller payload MUST be treated as untrusted with respect to tenant, risk, permissions, authoritative preconditions, authoritative effects, and authorization tokens.

### SPEC-SEC-3

Authorization tokens or AuthorizedAction objects MUST NOT be accepted from external protocol callers.

### SPEC-SEC-4

Public errors MUST not expose sensitive implementation details by default.

### SPEC-SEC-5

Sensitive diagnostic evidence MUST follow the same minimization and access-control rules as other audit evidence.

## 53. Initial Component Interfaces

A conforming first implementation SHOULD define equivalents of:

```text
IActionRegistry
IActionBindingRegistry
IExecutionGate
IActionExecutor
IDiagnosticRunner
IAuditSink

Second-stage autonomous runtime adds:
IActionResolver
IActionConstraintPipeline
IDecisionProvider
IEffectVerifier
IGoalOrchestrator
```

Reimplemented history, graph, and vector capabilities MAY sit behind executor or infrastructure adapters. Legacy LimboDancer.MCP assemblies MUST NOT.

## 54. Namespace and Placement Guidance

The new implementation MUST be created entirely under the `LimboDancer.*` project and namespace family.

Recommended initial projects are:

```text
LimboDancer.Abstractions
LimboDancer.Runtime
LimboDancer.Infrastructure
LimboDancer.Adapters.Mcp
LimboDancer.Host
```

Plane and fabric separation SHOULD initially be expressed primarily through namespaces inside Runtime:

```text
Runtime.Semantics
Runtime.Diagnostics
Runtime.Decision
Runtime.Execution
Runtime.Orchestration
Runtime.Observations
```

Infrastructure SHOULD initially group concrete implementations through namespaces such as:

```text
Infrastructure.Relational
Infrastructure.Graph
Infrastructure.Vector
```

Assemblies SHOULD be split later only when there is a concrete dependency, deployment, ownership, packaging, or isolation reason.

The implementation SHOULD optimize for:

- dependency correctness;
- independently testable contracts;
- clear provider/infrastructure boundaries;
- minimal cyclic references;
- clean eventual deletion of `LimboDancer.MCP.*`.

### SPEC-NS-1

Stable new contracts MUST NOT be declared inside legacy `LimboDancer.MCP.*` projects.

### SPEC-NS-2

New adapters MAY reproduce required legacy protocol behavior but MUST depend only on new runtime contracts and approved external packages.

### 54.1 Domain Package Integration

Concrete domain implementations are separately composed packages. The design and contract-admission process are defined in `LimboDancer.Agentic.CognitiveRuntime Domain Integration Model.md`.

### SPEC-DOM-1

Runtime production projects MUST NOT reference ASL or another concrete domain package.

### SPEC-DOM-2

A domain package MUST integrate through approved domain-neutral LimboDancer contracts and MUST NOT redefine, bypass, or weaken runtime authority transitions.

### SPEC-DOM-3

The Host MUST compose selected domain packages with the runtime. Protocol adapters MUST NOT be required as the domain integration mechanism.

### SPEC-DOM-4

Domain-specific infrastructure SDK types MUST NOT appear in domain-neutral runtime contracts.

### SPEC-DOM-5

Concrete domain-integration interfaces MUST NOT be added solely for hypothetical extensibility. Their owning plane, authority, context, failure behavior, and reference-domain justification MUST be documented before admission.

## 55. Required First Implementation Slice

The first conforming slice MUST prove the common authority boundary with minimal machinery.

It MUST implement:

1. `RuntimeInvocationId`, `ActionId`, `ActionVersion`, and `ActionRiskProfile`.
2. `ActionDescriptor`.
3. `IActionRegistry`.
4. `ActionBinding` and `IActionBindingRegistry`.
5. new descriptors for the four compatibility MCP capabilities.
6. `ExecutionContext`.
7. `SelectedAction`.
8. `IExecutionGate`.
9. `AuthorizedAction`.
10. `DiagnosticFinding`, `IDiagnosticCheck<TContext>`, and `IDiagnosticRunner`.
11. hard checks for tenant, descriptor version, executor binding, and required semantic mappings.
12. `IActionExecutor`.
13. `IAuditSink` or equivalent structured audit boundary.
14. a new MCP adapter routing directed invocation through the gate.
15. new implementations of the required history, graph, vector, and ontology-backed behaviors needed by those four actions.

The first slice MUST NOT require autonomous Reasoning, a Decision provider, provider routing, human confirmation infrastructure, a replay engine, compensation execution, or distributed authorization tokens.

The first slice SHOULD support effect verification only where a reimplemented action has a concrete, inexpensive, meaningful effect to verify.

## 56. Required Second Implementation Slice

The second conforming slice MUST implement:

1. Goal and lifecycle state.
2. Observation.
3. ActionCandidate.
4. IActionResolver.
5. constraint pipeline.
6. PermittedAction.
7. DecisionResult.
8. IDecisionProvider.
9. SelectedAction.
10. IGoalOrchestrator.
11. autonomous pre-flight diagnostics.
12. replay-capable decision audit capture.

A deterministic RuleDecisionProvider SHOULD be implemented first as a reference provider. Provider routing and escalation chains SHOULD be deferred until at least two real providers exist.

## 57. Required Conformance Tests

A conforming implementation MUST include automated tests for the following.

### 57.1 Registry

- known action resolves;
- unknown action fails;
- duplicate descriptor/version rejected;
- missing executor binding prevents executable publication;
- missing required diagnostic prevents executable publication.

### 57.2 Binding

- known MCP tool resolves;
- unknown MCP tool fails;
- binding cannot change descriptor risk;
- binding cannot change descriptor effects.

### 57.3 Tenant

- missing tenant rejected;
- cross-tenant history read prevented;
- cross-tenant graph read prevented;
- vector query contains tenant filter;
- cross-tenant mutation prevented.

### 57.4 Semantic

- known predicate maps;
- unknown required predicate fails;
- raw fallback is not used for ontology-bound required preconditions;
- unknown effect mapping fails according to descriptor policy.

### 57.5 Diagnostics

- hard invariant pass continues;
- hard invariant fail blocks;
- hard-invariant diagnostic indeterminate fails closed;
- warning may continue under policy;
- DiagnosticFinding includes ID/version/code/outcome/severity;
- disposition is audited;
- diagnostic timeout consumes budget;
- diagnostic cannot create AuthorizedAction.

### 57.6 Decision

- provider receives only permitted candidates;
- duplicate candidate IDs rejected;
- provider selection outside candidate set rejected;
- abstention executes nothing;
- provider failure distinct from abstention;
- confidence does not bypass gate.

### 57.7 Execution Gate

- authorized directed invocation succeeds;
- missing permission denied;
- stale version returns Stale;
- missing confirmation returns ConfirmationRequired;
- blocking diagnostic returns DiagnosticBlocked;
- required evaluator failure fails closed.

### 57.8 Executor

- executor cannot accept externally manufactured authorization through protocol/deserialization paths;
- output schema validated;
- idempotent retry behavior tested;
- non-idempotent uncertain timeout invokes reconciliation path.

### 57.9 Effects

- verified effect returns Verified;
- mismatch returns Contradicted;
- unverifiable effect represented explicitly;
- contradiction invokes configured policy.

### 57.10 Audit/Replay

- successful action audited;
- denied action audited;
- blocked diagnostic audited;
- ActionDescriptor version recorded;
- provider/version recorded;
- replay stops before side effects;
- hidden model reasoning not required.

## 58. Required Legacy Behavior Corrections During Reimplementation

The following legacy behaviors MUST NOT be copied unchanged into the new runtime. They are reference-source defects or ambiguities that MUST be corrected in the new implementation before autonomous execution is enabled:

1. implement tenant-safe history reads rather than reproducing the ambiguous `HistoryService.ListAsync` behavior;
2. implement explicit/verified graph-read tenant isolation;
3. do not reproduce the fail-open raw predicate fallback from `GraphPreconditionsService`;
4. ensure unknown ontology filters do not silently broaden security-sensitive graph queries;
5. do not reproduce authoritative caller-controlled effects from `HistoryAppendTool`;
6. define stable history/precondition contracts directly in the new host-neutral runtime;
7. implement new directed MCP execution through the common Execution Gate;
8. establish new host-neutral runtime contracts independent of legacy server hosts.

## 59. Acceptance Criteria

The Plane Runtime Specification is implemented when all of the following are true:

1. Every executable action has a registered immutable ActionDescriptor.
2. Every consequential action passes the common Execution Gate.
3. Direct MCP calls and autonomous actions converge at the same authority boundary.
4. External callers cannot create or weaken execution authority.
5. Tenant scope is enforced for every relevant State read and write.
6. Semantic mappings fail closed.
7. Decision providers see only permitted candidates.
8. Invalid provider selections are rejected.
9. Required diagnostics execute in context.
10. Hard diagnostic invariants fail closed.
11. Diagnostic findings remain distinct from Governance decisions.
12. Diagnostic dispositions are enforced by the appropriate authority.
13. Executors receive only runtime-authorized work.
14. Mutable critical state is revalidated before execution.
15. Expected effects are independently verifiable.
16. Consequential execution is auditable.
17. Replay can reconstruct the decision boundary without side effects.
18. Provider implementations are replaceable.
19. No model or diagnostic component can grant execution authority independently.
20. Existing MCP tool names remain usable through new action bindings where compatibility is required.
21. All new production projects target the approved framework baseline.
22. No new production project or runtime assembly depends on `LimboDancer.MCP.*`.
23. The legacy `LimboDancer.MCP.*` production projects can be deleted without breaking the new runtime.
24. The runtime preserves a non-mutating domain-conclusion outcome distinct from action authority.
25. An implementation claiming ASL reference-domain conformance satisfies the current requirements and acceptance scenarios in `docs/ASL/legacy-limbodancer-mcp-system-design.md`.

## 60. Specification Invariants

The following invariants are non-negotiable:

```text
Goal != Action

MCP tool name != Semantic ActionId

Candidate != PermittedAction

PermittedAction != SelectedAction

SelectedAction != AuthorizedAction

Decision confidence != Permission

Diagnostic finding != Authorization

Telemetry != Diagnostic Check

Execution success != Effect verification

Plan != Future authorization

Caller input != Authoritative policy

Model output != Execution authority

Domain conclusion != Execution authority

Domain conclusion != Permission to mutate state

New runtime != Legacy runtime dependency
```

## 61. Implementation Sequence

The required implementation order is:

```text
1. Create the five-project net10.0 `LimboDancer.*` solution skeleton
2. Add CI guard forbidding LimboDancer.MCP.* production references
3. Implement runtime/action identifiers and ActionDescriptor
4. Implement action registry and MCP action bindings
5. Implement minimal diagnostic contracts and runner
6. Implement ExecutionContext, Execution Gate, and AuthorizedAction
7. Implement new directed MCP adapter and new executors
8. Implement structured audit capture
9. Reimplement semantic authority and tenant-safe State behavior
10. Prove the four directed compatibility actions end-to-end
11. Add Goal / Observation / ActionCandidate contracts
12. Add ActionResolver and constraint pipeline
13. Add Decision contracts and deterministic reference provider
14. Add Goal orchestration
15. Add opt-in effect verification where meaningful
16. Add replay-capable audit data; defer replay engine until useful
17. Add additional Decision providers such as Jev or LLM providers
18. Add provider routing, human confirmation, compensation, or distributed authorization only when concrete use cases require them
```

This ordering preserves the architectural boundaries while minimizing speculative infrastructure. Probabilistic decision-making is introduced only after the execution authority boundary exists and has been proven through directed invocation.

## 62. Final Specification Statement

A conforming LimboDancer runtime MUST implement explicit, typed transitions from intent to authority.

The runtime MUST know:

- what the Goal is;
- what state was observed;
- what actions exist;
- which actions are semantically valid;
- which actions Governance permits;
- what diagnostics say about the execution context;
- what Decision selected;
- whether the current state still permits execution;
- what was actually executed;
- what effects were expected;
- what effects were observed;
- what was audited.

No model, caller, tool name, diagnostic, or transport adapter may skip that chain.

The target runtime is a clean implementation under the `LimboDancer.*` .NET namespace family, while the product/repository remains `LimboDancer.Agentic.CognitiveRuntime`, with no production dependency on the legacy `LimboDancer.MCP.*` codebase.

It is therefore not a tool-calling LLM.

It is a governed, diagnosable, ontology-constrained execution system with replaceable reasoning and decision intelligence.
