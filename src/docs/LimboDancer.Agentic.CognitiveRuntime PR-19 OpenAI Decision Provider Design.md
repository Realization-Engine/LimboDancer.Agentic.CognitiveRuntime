# LimboDancer.Agentic.CognitiveRuntime PR-19 OpenAI Decision Provider Design

**Status:** Approved implementation slice

**Date:** 2026-09-22

**Governing checkpoint:** Milestone D Conformance Review, section 8

## 1. Decision

PR-19 will add one `OpenAiDecisionProvider` as the first non-reference Decision provider. It will use the OpenAI Responses API with strict JSON Schema structured output, no tools, no conversation state, and response storage disabled. The provider is an evaluation candidate behind the existing `IDecisionProvider` contract; it is not an execution authority or the default provider.

The deterministic `RuleDecisionProvider` remains the default Host composition. OpenAI composition requires explicit configuration of the provider, API key, pinned model identifier, timeout, output-token bound, and model-specific input/output prices used for budget accounting.

## 2. Bounded decision class

The provider may choose among an already resolved, already constrained, non-empty set of `PermittedAction` values. It may return:

- `Selected` with exactly one supplied candidate identifier;
- `Abstained` when the supplied evidence is insufficient; or
- `Escalated` when a human or stronger policy decision is required.

It cannot create candidates, restore rejected candidates, alter descriptor risk or policy, call tools, invoke an executor, or authorize execution.

## 3. Request boundary

The request contains only the semantic information required for selection:

- Goal intent and inputs;
- observation identifiers, versions, provenance, and data;
- permitted candidate identifiers, trusted descriptor identity/version/name/description, proposed arguments, evidence references, state versions, and passing constraint reason codes.

Tenant identifiers, credentials, authorization tokens, executor bindings, hidden reasoning, and rejected candidates are excluded. The request sets `store` to `false`, supplies no tools, and uses a strict JSON Schema for the existing Decision outcome fields.

## 4. Response boundary

Only a completed `output_text` item matching the strict schema is accepted. Refusal, incomplete response, transport failure, malformed JSON, duplicate distribution entries, provider-identity mismatch, or an out-of-set candidate is provider failure, not abstention.

The provider returns the existing `DecisionResult` plus explicit input/output token usage, measured latency, and calculated cost. `DecisionPlane` continues to validate the result and is still the only component that may materialize `SelectedAction`.

## 5. Budget semantics

Before invocation, the provider:

1. bounds the request with the smaller of configured output tokens and remaining runtime tokens;
2. uses UTF-8 request bytes as a conservative upper bound for input tokens;
3. rejects a request whose worst-case token or configured-price cost exceeds the remaining Decision budget; and
4. applies the earlier of the configured provider timeout and Goal deadline.

After invocation, reported token usage and calculated cost are returned in `DecisionResult`. Orchestration accumulates those values across Decision calls and supplies only the remaining budget to the next Decision context. A result exceeding the remaining budget terminates before Diagnostics, the Execution Gate, or execution.

## 6. Evaluation slice

Tests will use recorded strict-schema responses through a fake HTTP transport; no live API key is required in CI. The labeled cases cover selection, abstention, escalation, malformed/out-of-set output, refusal/failure, and token/cost rejection. A replay-only comparison utility will run a provider against stored `RuntimeStepEvidence`, compare it with acceptable labeled outcomes and the original Decision, and expose correctness, abstention, disagreement, latency, tokens, and cost without calling the Execution Gate or an executor.

## 7. Explicitly excluded

PR-19 does not add provider routing, fallback, retries, tool calling, prompt persistence, live-provider CI, model adoption criteria, replay execution, or a generalized evaluation framework.

## 8. API references

- [OpenAI Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs?api-mode=responses)
- [OpenAI Responses API](https://developers.openai.com/api/reference/cli/resources/responses/methods/create)
