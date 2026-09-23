# LimboDancer.Agentic.CognitiveRuntime Anthropic Decision Provider Feasibility and Design

**Status:** Candidate provider design; implementation not admitted  
**Date:** 2026-09-23  
**Branch:** `decision-plane`  
**Governing authority:** Plane Runtime Specification, Decision Plane Architecture, Milestone D Conformance Review, PR-19 OpenAI Decision Provider Design, and PR-19 Evaluation Review

## 1. Purpose

This document records the feasibility and design constraints for a possible future `AnthropicDecisionProvider`.

It does not admit another provider, authorize implementation, select a Claude model, add an Anthropic dependency, change host composition, or weaken the provider-adoption gate. The deterministic `RuleDecisionProvider` remains the default. `OpenAiDecisionProvider` remains the only admitted non-reference provider experiment and remains disabled by default.

The research establishes a narrower conclusion:

> The official Anthropic C# SDK can support the existing bounded `IDecisionProvider` contract, provided LimboDancer overrides SDK defaults and retains its own budget, validation, failure, evidence, and execution-authority semantics.

Technical feasibility is not provider fitness. Representative, independently reviewed Decision evidence remains required before any additional provider increment may be admitted.

## 2. Process position

This design belongs after PR-19's provider-neutral boundary and evaluation machinery and before any Anthropic implementation increment.

```text
Decision Plane architecture                 COMPLETE
RuleDecisionProvider                        COMPLETE
PR-19 OpenAI provider boundary              COMPLETE
provider-neutral replay evaluation          COMPLETE
Anthropic technical feasibility             DOCUMENTED HERE
representative reviewed Decision corpus     NOT AVAILABLE
Anthropic implementation increment          NOT ADMITTED
Anthropic fitness evaluation                BLOCKED
provider adoption                           DEFERRED
```

This research reduces implementation uncertainty. It does not supply selection-correctness, abstention, calibration, severe-error, latency, or cost evidence for a real domain workload.

## 3. Evidence reviewed

The design is based on three implementation sources.

### 3.1 Storyvizor

The private Storyvizor repository demonstrates production-oriented use of the official `Anthropic` C# package from a .NET Blazor application.

Relevant patterns include:

- `AnthropicClient` registration;
- configuration-first model selection;
- system prompts separated from user messages;
- streaming Messages API consumption;
- per-turn cancellation and deadlines;
- explicit stop-reason handling;
- typed Anthropic exception mapping;
- pre-turn conversation rollback; and
- internal network-substitution test hooks.

Storyvizor is a generative, multi-turn prose application. Its conversation history, streaming UI, prompt caching, adaptive thinking, large output limits, and prose acceptance rules are not Decision Plane semantics.

### 3.2 cookbook-agent-platform

The older cookbook repository demonstrates OpenAI and Anthropic transports behind a general LLM router. Its useful contributions are provider configuration, secret injection, and usage normalization.

Its generic `ILlmRouter`, automatic retry policy, generative request contract, and incomplete Anthropic normalization must not replace or wrap `IDecisionProvider`.

### 3.3 Official Anthropic C# SDK

The official SDK is maintained at [anthropics/anthropic-sdk-csharp](https://github.com/anthropics/anthropic-sdk-csharp) and published as the `Anthropic` NuGet package. Version 10 and later is the official Anthropic package line.

At the research date:

- Storyvizor pins `Anthropic` 12.8.0;
- structured-output request support exists in that package generation;
- the generic `Messages.Create<T>()` structured-output helper was added later; and
- the current release line is newer than Storyvizor's pin.

A future increment must select, review, lock, and record an exact package version. It must not inherit Storyvizor's version or float to the latest version without compatibility and supply-chain review.

## 4. Architectural decision

A possible Anthropic integration implements the existing semantic contract:

```text
DecisionContext + PermittedAction[]
                |
                v
      AnthropicDecisionProvider
                |
                v
  private Anthropic SDK transport
                |
                v
       Claude Messages API
                |
                v
          DecisionResult
                |
                v
 existing Decision Plane validation
                |
                v
          SelectedAction
                |
                v
       existing Execution Gate
```

The SDK is an Infrastructure dependency. Anthropic request types, content blocks, exceptions, usage objects, stop reasons, and client configuration must not appear in `LimboDancer.Abstractions` or `LimboDancer.Runtime`.

The provider may judge only among the supplied permitted candidates. It may not:

- create or restore candidates;
- inspect rejected candidates;
- invoke tools;
- maintain conversation state;
- request a second model turn;
- retry or fall back silently;
- redefine permissions, risk, preconditions, or effects;
- construct `SelectedAction` or `AuthorizedAction`;
- invoke the Execution Gate or an executor; or
- convert replay evaluation into execution authority.

## 5. Provider shape

The prospective type is:

```csharp
public sealed class AnthropicDecisionProvider : IDecisionProvider
{
    public Task<DecisionResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken cancellationToken = default);
}
```

An internal transport seam should isolate the SDK:

```csharp
internal interface IAnthropicDecisionTransport
{
    Task<AnthropicDecisionResponse> SendAsync(
        AnthropicDecisionRequest request,
        CancellationToken cancellationToken);
}
```

This is not a new public provider abstraction. It exists only to keep SDK mechanics out of semantic validation and to make request construction, stop reasons, usage, cancellation, and failures testable without live calls.

## 6. Request construction

The provider should reuse the canonical Decision input shape established by PR-19:

- Goal intent and inputs;
- authoritative observations with identity, version, provenance, and data;
- permitted candidate identity;
- action descriptor identity and version;
- candidate description and arguments;
- evidence references;
- state versions; and
- successful constraint results.

The provider must not send rejected candidates or unrelated tenant state.

The request should be:

- one non-streaming Messages API call;
- stateless;
- tool-free;
- bounded by a small explicit output-token ceiling;
- constrained by structured output;
- free of conversational history;
- free of hidden retry or fallback; and
- cancellable by the earlier of caller cancellation, Goal deadline, and provider timeout.

Streaming provides no semantic benefit for a compact `DecisionResult` and complicates usage aggregation, incomplete-output handling, and cancellation evidence. It should not be part of the initial design.

## 7. SDK configuration requirements

The official SDK provides authentication headers, request serialization, typed responses, exceptions, streaming, retries, timeouts, and connection management. LimboDancer must explicitly override several defaults.

A future client configuration should be equivalent to:

```csharp
var client = new AnthropicClient(new ClientOptions
{
    ApiKey = apiKey,
    BaseUrl = endpoint,
    MaxRetries = 0,
    Timeout = providerTimeout,
    ResponseValidation = true,
    HttpClient = httpClient,
});
```

The supplied `HttpClient` should not impose an earlier unrelated timeout. The linked Decision cancellation token remains authoritative.

### 7.1 Retries

The SDK automatically retries eligible connection errors, 408, 409, 429, and 5xx results by default. It may also honor server retry instructions.

`MaxRetries = 0` is mandatory. One provider invocation must mean one remote attempt so that:

- Goal budgets remain exact;
- latency and cost evidence identify one attempt;
- failures remain observable;
- no second charge occurs silently; and
- retry policy cannot bypass operator review.

### 7.2 Response validation

SDK response validation is not fully eager by default. The provider should enable `ResponseValidation` and explicitly call `Validate()` on the returned message before interpreting its content.

A missing, malformed, or unknown required response member is provider failure.

### 7.3 Endpoint and credentials

The provider must require:

- an absolute HTTPS endpoint;
- an explicit secret source;
- a pinned model identifier;
- an optional workspace identifier when required;
- finite token and cost limits;
- finite timeout;
- explicit input, output, and cache pricing; and
- disabled-by-default host selection.

Secrets must not be committed, logged, persisted in replay evidence, or embedded in provider identity.

## 8. Structured output

Current SDK releases provide a generic structured-output call:

```csharp
StructuredMessage<AnthropicDecisionOutput> response =
    await client.Messages.Create<AnthropicDecisionOutput>(
        parameters,
        cancellationToken);
```

The helper derives an `output_config.format` JSON Schema from a C# DTO and preserves the underlying message metadata, including model, usage, stop reason, and message ID.

A private DTO should mirror the public semantic result without coupling the domain contract to the SDK:

```csharp
internal sealed class AnthropicDecisionOutput
{
    public string Outcome { get; set; } = "";
    public string? SelectedCandidateId { get; set; }
    public string ReasonCode { get; set; } = "";
    public double? Confidence { get; set; }
    public List<AnthropicDistributionEntry> Distribution { get; set; } = [];
}

internal sealed class AnthropicDistributionEntry
{
    public string CandidateId { get; set; } = "";
    public double Probability { get; set; }
}
```

Structured output provides shape conformance during normal completion. It does not establish semantic correctness or authorization.

The API documents exceptions in which a successful HTTP response may not match the requested schema, including refusal and token exhaustion. The provider must inspect the stop reason before accessing parsed content.

## 9. Stop-reason policy

Only a natural completed turn is eligible for parsing.

| Anthropic stop reason | LimboDancer treatment |
| --- | --- |
| `EndTurn` | Continue response and semantic validation |
| `MaxTokens` | Provider failure |
| `Refusal` | Provider failure |
| `ModelContextWindowExceeded` | Provider failure |
| `StopSequence` | Provider failure |
| `ToolUse` | Provider failure |
| `PauseTurn` | Provider failure |
| null | Provider failure |
| unknown future value | Provider failure |

There is no continuation request, output-token increase, automatic retry, tool execution, or provider fallback.

Refusal is not abstention. Transport failure is not abstention. Truncation is not escalation. `Abstained` and `Escalated` are valid semantic outcomes only when returned through a normally completed and fully validated Decision result.

## 10. Semantic validation

The provider must validate the parsed DTO independently of schema generation.

At minimum:

- outcome maps to exactly `Selected`, `Abstained`, or `Escalated`;
- outcome parsing tolerates case variation only when it maps unambiguously;
- `Selected` requires a supplied candidate ID;
- `Abstained` and `Escalated` prohibit a selected candidate ID;
- every distribution entry references a supplied candidate;
- distribution keys are unique;
- confidence is null or within `[0,1]`;
- every probability is within `[0,1]`;
- distribution semantics satisfy the existing `DecisionResult` contract;
- reason code is non-empty, bounded, and suitable for audit;
- exactly one usable structured text block exists; and
- no tool, thinking, refusal, or other content block substitutes for the result.

Anthropic's supported JSON Schema subset does not enforce every numeric constraint. Local validation is therefore authoritative.

Candidate membership must be checked again by the existing Decision Plane after the provider returns. Provider validation is defense in depth, not a replacement for runtime validation.

## 11. Budget and usage accounting

Before invocation, the provider must conservatively determine whether the remaining Decision budget can contain the request and maximum response.

The SDK response exposes:

- input tokens;
- output tokens;
- cache-creation input tokens;
- cache-read input tokens;
- output-token detail;
- service tier; and
- inference geography when supplied.

Total Anthropic input accounting may include normal input, cache creation, and cache read categories. These categories can have different prices.

The initial provider should disable prompt caching and adaptive thinking unless a separately reviewed use case requires them. That keeps cost eligibility and evidence comparable to PR-19.

If caching or thinking is later admitted, the evidence and price model must distinguish all billed categories. It must not collapse them into a misleading single input-token price.

The provider should record:

- provider ID;
- exact requested model;
- exact returned model;
- SDK/package version;
- message/request ID where available;
- latency;
- normalized token usage;
- calculated cost;
- configured pricing identity; and
- provider result or failure classification.

## 12. Failure mapping

Typed SDK exceptions should be translated into stable provider-failure categories without leaking credentials, request bodies, tenant data, or unrestricted remote error bodies.

Required categories include:

- authentication or authorization failure;
- invalid request or unsupported model;
- rate limit;
- server failure;
- network/I/O failure;
- timeout;
- caller cancellation;
- invalid SDK response data;
- refusal;
- truncation;
- context-window exhaustion;
- structured-output parse failure;
- semantic validation failure; and
- unknown failure.

These categories support audit and evaluation. They do not imply automatic retryability.

## 13. Security, privacy, and supply chain

Before an implementation increment is admitted, review and record:

- exact SDK version, transitive dependency lock, license, and provenance;
- known security advisories and relevant open SDK defects;
- API endpoint and deployment jurisdiction;
- workspace and credential isolation;
- provider data-retention and training terms;
- zero-data-retention eligibility for the selected feature and model;
- structured-output compatibility with the selected platform;
- log redaction;
- replay-corpus redaction;
- tenant-boundary tests; and
- incident-response ownership.

A provider-specific retention control equivalent to OpenAI's `store: false` must not be assumed. Current Anthropic service terms and workspace controls must be verified during admission.

## 14. Testing requirements

A future implementation slice requires deterministic tests proving:

### Request boundary

- only permitted candidates are serialized;
- rejected candidates are absent;
- no tools are configured;
- no prior conversation is sent;
- model, endpoint, timeout, token ceiling, and workspace are exact;
- SDK retries are disabled;
- caller cancellation and Goal deadlines propagate; and
- secrets are absent from logs and evidence.

### Response boundary

- `EndTurn` plus valid structured output maps correctly;
- every other stop reason fails closed;
- null and unknown stop reasons fail closed;
- refusal is distinct from abstention;
- truncation is distinct from escalation;
- missing or multiple result blocks fail;
- malformed JSON fails;
- invalid SDK response data fails;
- unknown candidate selection fails;
- unknown distribution candidates fail;
- duplicate distribution keys fail;
- invalid probabilities and confidence fail; and
- selected/non-selected outcome invariants hold.

### Budget and evidence

- pre-invocation token rejection;
- pre-invocation worst-case cost rejection;
- earlier-of-deadline timeout behavior;
- exact single-attempt behavior;
- input/output usage mapping;
- cache-category accounting when enabled;
- returned-model identity recording;
- latency recording; and
- cost calculation.

Live-provider tests must remain opt-in, finite-budget, non-CI evaluation activities.

## 15. Relationship to other provider directions

### 15.1 OpenAI

PR-19 remains the implemented remote structured-output baseline. Anthropic should preserve its semantic rules rather than introduce a parallel contract.

Possible internal reuse may eventually include:

- canonical Decision input serialization;
- provider-independent result validation; and
- cost/usage calculation primitives.

Do not refactor the implemented OpenAI slice merely to anticipate Anthropic. Extract shared internal helpers only inside an admitted provider increment with tests proving no semantic change.

### 15.2 Native local model

The [Native Local Decision Model](<./LimboDancer.Agentic.CognitiveRuntime Native Local Decision Model.md>) describes a future LimboDancer-owned inference kernel. Anthropic is a remote generative provider candidate, not a substitute for or prerequisite of that research direction.

Both candidates must use the same representative corpus, labels, budgets, and acceptance thresholds.

### 15.3 Reasoning providers

The Anthropic SDK may also support future conversational or generative reasoning capabilities. That is a separate boundary. `AnthropicDecisionProvider` must not become a general chat service or introduce an `ILlmRouter` into the Decision Plane.

## 16. Admission sequence

No implementation follows directly from this document.

The conformant sequence is:

1. prepare a representative tenant-redacted historical Decision corpus;
2. independently review expected and acceptable outcomes;
3. label ambiguity, action risk, and wrong-choice severity;
4. define acceptance thresholds for one bounded Decision class;
5. complete the operator-controlled PR-19 evaluation;
6. decide whether another remote provider comparison is justified;
7. if explicitly admitted, create one bounded Anthropic provider PR;
8. evaluate it through the same replay machinery; and
9. conduct a separate adoption review.

Provider routing and fallback remain ineligible until at least two non-reference providers possess comparable accepted evidence and a separate policy is approved.

## 17. Implementation decision record

| Question | Decision |
| --- | --- |
| Can the official C# SDK support the existing boundary? | Yes, technically |
| Does this document admit `AnthropicDecisionProvider`? | No |
| Does Anthropic require a new public abstraction? | No |
| Should LimboDancer use a generic LLM router? | No |
| Initial invocation style | One non-streaming structured-output call |
| SDK retries | Disabled |
| Tools | Prohibited |
| Conversation state | Prohibited |
| Refusal or truncation | Provider failure |
| Unknown stop reason | Provider failure |
| Candidate validation | Provider and Runtime |
| Execution authority | Unchanged; Execution Gate only |
| Next process activity | Representative evaluation evidence |

## 18. Sources

- [Official Anthropic C# SDK](https://github.com/anthropics/anthropic-sdk-csharp)
- [Anthropic C# SDK documentation](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp)
- [Anthropic structured outputs](https://platform.claude.com/docs/en/build-with-claude/structured-outputs)
- [Anthropic Messages API](https://platform.claude.com/docs/en/api/messages)
- Storyvizor: `src/Storyvizor/Storyvizor.Studio/Program.cs`
- Storyvizor: `Services/WorkshopService.cs`
- Storyvizor: `Services/StreamingConversationEngine.cs`
- cookbook-agent-platform: `Cookbook.Platform.Shared/Llm`
- cookbook-agent-platform: `Cookbook.Platform.Infrastructure/Llm`
