# LimboDancer.Agentic.CognitiveRuntime Native Local Decision Model

**Status:** Research direction; implementation not admitted  
**Date:** 2026-09-22  
**Branch:** `decision-plane`  
**Governing authority:** Plane Runtime Specification, Decision Plane Architecture, Milestone D Conformance Review, and PR-19 Evaluation Review

## 1. Purpose

This document records the architectural direction for a future LimboDancer-native local Decision model.

It does not approve another provider, select a model, create an implementation increment, or change the current provider-adoption gate. The deterministic `RuleDecisionProvider` remains the default. `OpenAiDecisionProvider` remains the only admitted non-reference provider experiment, disabled by default. A second non-reference provider remains deferred until representative operator-controlled evidence and explicit acceptance thresholds exist.

The durable conclusion from the emerging Jev-style ecosystem is narrower and more useful than adopting any one product:

> LimboDancer can eventually own a local semantic choice engine that supplies bounded judgment over runtime-generated candidates while leaving candidate authority, execution authority, and effect verification unchanged.

## 2. Architectural decision

`IDecisionProvider` is a provider-neutral semantic Decision boundary. It is not an LLM abstraction.

A conforming provider may be deterministic, probabilistic, neural, symbolic, remote, or local. It receives only the existing `DecisionContext` and already permitted candidates. It may return `Selected`, `Abstained`, or `Escalated` through the existing `DecisionResult`. It may not:

- create or restore candidates;
- inspect rejected candidates;
- redefine risk, policy, preconditions, effects, or permissions;
- manufacture `SelectedAction` or `AuthorizedAction`;
- invoke the Execution Gate or an executor; or
- convert replay evidence into execution authority.

A future native local model therefore belongs behind the existing boundary:

```text
DecisionContext + PermittedAction[]
                |
                v
      local semantic choice engine
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

No downstream authority boundary changes.

## 3. Why a native model is plausible

Jev and its open alternatives demonstrate several independent ways to implement typed probabilistic decisions without free-form generation. Dion Wiggins's article, ["Jev is dead. Long live Jev."](https://www.linkedin.com/pulse/jev-dead-long-live-dion-wiggins-ktbqf/), argues that the enduring category is the bounded Decision primitive rather than any single proprietary implementation.

That observation fits LimboDancer particularly well because the runtime already owns the surrounding machinery that many experimental model projects do not:

- semantic candidate construction;
- deterministic constraint partitioning;
- explicit permitted candidates;
- distinct selection, abstention, escalation, and provider-failure outcomes;
- provider and model identity;
- confidence, distribution, latency, token, and cost evidence;
- replay-capable Decision evidence;
- ambiguity, action-risk, and wrong-choice-severity labels;
- an independent Execution Gate; and
- post-execution Effect Verification.

The missing future component is therefore not an autonomous agent. It is a bounded inference kernel plus a provider adapter.

## 4. External implementation survey

The following projects were reviewed as design evidence. Their reported quality, latency, and calibration results are project claims on their own workloads and hardware. They are not LimboDancer performance evidence.

| Project | Approach | Useful lesson for LimboDancer | Caution |
| --- | --- | --- | --- |
| [SemIf](https://github.com/TheoLeeCJ/SemIf) | Frozen open-weight model with direct option-logit readout and shared-state execution | Establish a no-training baseline; preserve exact model revision and prompt hash | Probabilities remain conditional on supplied options and require workload-specific validation |
| [jqv](https://github.com/Octalab-Inc/jqv) | Naive, KV-cache, packed, and shared-prefix inference engines with direct logits | Separate inference strategies; prove numerical equivalence, sibling isolation, and calibration provenance | Its general wire format is not LimboDancer's contract |
| [reflex](https://github.com/kshetrajna12/reflex) | Frozen Qwen readout with isolated branches and option-order averaging | Measure permutation sensitivity and out-of-distribution behavior before specialization | Its own experiments report that fine-tuning can harm long or ambiguous cases |
| [Kev](https://github.com/jaredpalmer/kev) | Qwen-based trained Decision models with LoRA/readout machinery | Use locked evaluation, checksummed data, and eventual domain specialization | Training is premature without a representative reviewed corpus |
| [Bespoke Nimble](https://github.com/bespokelabsai/nimble) | Candidate-logit training and contrastive examples that flip one decisive fact | Use ontology-guided counterfactuals as supplemental training and diagnostic data | Its published labels are synthetic and not human-reviewed; no root license file was present in the reviewed snapshot |
| [Laya](https://github.com/NandhaKishorM/laya) | Compact bidirectional encoders and typed Decision heads | Consider a later high-throughput specialist once real candidate cardinality and context distributions are known | Fixed context and specialization tradeoffs must be tested on LimboDancer workloads |
| [OpenJev / Verdict](https://github.com/Heman10x-NGU/Verdict-open-jev) | ModernBERT plus a classification head and an explicit insufficient-evidence route | Treat non-selection as first-class and measure it explicitly | Model-scored abstention must still map into LimboDancer policy semantics |
| [GLiNER2](https://github.com/fastino-ai/GLiNER2) | Schema-conditioned extraction, classification, records, and relations | Strong candidate for upstream observation-to-fact extraction in the Semantic Plane | It should not collapse semantic grounding and action selection into one boundary |
| [djev](https://github.com/Davipar/djev-dev) | DiffusionGemma answer canvas with typed probability readout and multimodal inputs | Preserve as a future multimodal research path | Operational weight and specialized GPU requirements make it unsuitable for the first local slice |

The synthesis matters more than selecting a repository to fork. LimboDancer should borrow tested ideas while preserving its own contracts and authority model.

## 5. Proposed bounded capability

The first native local model should implement only the Decision class LimboDancer already needs:

```text
BoundedChoice
  - one supplied candidate identifier
  - ABSTAIN
  - ESCALATE
```

General `Choice`, `Score`, and binary `Noul` APIs are not required. Adding them would expand the runtime contract before a concrete use case exists.

The provider may internally score candidates plus special outcomes, but its adapter must project them into the existing result algebra:

| Model result | LimboDancer result |
| --- | --- |
| supplied candidate ID | `DecisionOutcome.Selected` |
| insufficient evidence | `DecisionOutcome.Abstained` |
| policy-defined human/stronger-review route | `DecisionOutcome.Escalated` |
| malformed, unavailable, out-of-set, incompatible, or non-finite output | provider failure |

Abstention and escalation are not interchangeable. Provider failure must never be converted into either one.

The current `DecisionResult.Distribution` accepts supplied candidate identifiers only. Therefore, an initial adapter must not expose `ABSTAIN` or `ESCALATE` as distribution keys. It may use their scores internally to choose the outcome, return candidate-only distribution evidence, and set confidence according to a documented outcome-confidence rule. Preserving special-outcome probabilities would require a separately reviewed contract change.

## 6. Candidate technical shape

The first experiment should favor the smallest falsifiable design:

1. Canonically serialize the existing `DecisionContext` and supplied `PermittedAction` values.
2. Load one pinned open-weight model locally.
3. Process shared context once where the runtime and model permit it.
4. Isolate decision branches so one candidate or question cannot leak information into a sibling branch.
5. Read only the logits associated with supplied candidate labels and special outcomes.
6. Convert logits into an internal probability distribution, then project only supplied candidate probabilities into the current public distribution contract.
7. Apply a compatible calibration profile when, and only when, its full provenance matches.
8. Return the existing `DecisionResult` with stable provider/model identity and measured resource evidence.
9. Let the existing Decision Plane validate the result.

The likely deployment boundary is a local inference process implemented in the model ecosystem's native runtime, with a thin implementation of `IDecisionProvider` in `LimboDancer.Infrastructure`. Model SDK types, tensors, prompts/encodings, credentials, and transport details must remain outside `LimboDancer.Abstractions` and `LimboDancer.Runtime`.

This process boundary is a deployment hypothesis, not an approved project or protocol. In-process inference remains eligible if a future experiment proves it simpler without leaking provider concerns into the runtime.

## 7. Canonical encoding and identity

A local model is reproducible only when the complete inference identity is reproducible. At minimum, an evaluation artifact should bind:

- provider implementation version;
- model repository and immutable revision or weights digest;
- tokenizer revision;
- quantization and numerical precision;
- Decision encoding/schema version;
- prompt or canonical-encoding hash;
- inference-engine type and version;
- candidate-label mapping;
- option-order/permutation policy;
- calibration-profile identity;
- calibration corpus revision and split;
- calibration method and parameters; and
- relevant hardware/runtime metadata.

A calibration profile must fail closed when its model, tokenizer, encoding, candidate mapping, or corpus provenance is incompatible. Calibration must not silently survive a semantic encoding or model change.

The existing `ProviderId` and `ProviderVersion` remain the public provider identity. Any richer evidence contract requires its own reviewed design slice; this document does not modify Abstractions.

## 8. Evaluation requirements

A future experiment must use the existing replay-only evaluation boundary and must add only the measures necessary to test the local inference claims.

### 8.1 Existing mandatory measures

- overall and selection correctness;
- abstention correctness and unexpected abstention;
- escalation behavior;
- invalid-result/provider-failure rate;
- deterministic-baseline disagreement;
- latency and resource use;
- confidence/distribution calibration;
- wrong choices; and
- high/critical wrong choices.

### 8.2 Local-model-specific measures

- candidate-order and label-token sensitivity;
- repeated-run stability;
- sibling-branch leakage with a negative control;
- equivalence across naive, cached, packed, or shared inference paths where more than one path exists;
- behavior as candidate count and context length increase;
- truncation and unsupported-input failure behavior;
- in-distribution versus out-of-distribution behavior;
- calibration by risk, ambiguity, candidate cardinality, and Decision class; and
- calibration drift after any model, encoding, tokenizer, quantization, or runtime change.

Accuracy alone is insufficient. A provider with a slightly higher aggregate score but worse severe-error or calibration behavior may be unacceptable for the intended Decision class.

## 9. Corpus strategy

The primary asset is a representative, tenant-redacted, independently reviewed corpus of historical LimboDancer Decision boundaries. It must contain:

- reconstructed `DecisionContext`;
- the exact permitted candidate set;
- expected outcome and acceptable-candidate set;
- explicit abstention and escalation cases;
- ambiguity label;
- orthogonal `ActionRiskProfile`;
- expected wrong-choice severity; and
- independently reviewed rationale or provenance for the label.

Before any training, the corpus must be split by scenario or decision family so near-duplicate cases cannot leak across train, development, calibration, and locked test sets. The locked test set must not guide prompt, model, adapter, threshold, or calibration selection.

Ontology-guided contrastive cases may supplement the corpus. For example, one authoritative fact may be changed while the remaining Goal, observations, and candidates remain constant. These cases can test whether the model responds to the decisive semantic fact. Synthetic or generated cases must remain separately labeled and must never be presented as representative adoption evidence.

## 10. Staged research path

No stage begins automatically when the previous stage completes.

### Stage 0 - Evidence readiness

Obtain the operator-controlled corpus, labeling protocol, pinned evaluation budget, and acceptance thresholds already required by the PR-19 Evaluation Review.

### Stage 1 - Frozen-model readout

Evaluate one pinned, unchanged open-weight model using direct candidate-logit readout. No fine-tuning, routing, fallback, or production composition.

### Stage 2 - Inference invariants

Add shared-state reuse only after branch isolation, numerical equivalence, ordering sensitivity, cancellation, timeout, and bounded-resource behavior are proven.

### Stage 3 - Workload calibration

Fit and test calibration only on proper non-test splits. Bind the resulting profile to the complete inference identity and reject incompatible profiles.

### Stage 4 - Specialization study

Only if the frozen baseline and corpus size justify it, compare LoRA/readout specialization with a compact encoder specialist. Include negative-transfer and out-of-distribution tests. Improvement on training-shaped data is not sufficient.

### Stage 5 - Adoption review

Compare the candidate with `RuleDecisionProvider` and the existing OpenAI provider on the same representative corpus, budgets, and acceptance thresholds. Define the exact admitted Decision class if, and only if, the evidence supports adoption.

### Stage 6 - Routing review

Provider routing or fallback remains a separate architecture decision. It is ineligible until at least two non-reference providers possess comparable evidence and a policy can be justified without weakening failure semantics or budgets.

## 11. Domain relationship

Domain implementations make a native Decision model more useful by producing typed, evidence-backed decision surfaces. They do not move domain rules into the model.

```text
domain knowledge and state
          |
          v
semantic grounding and candidate construction
          |
          v
constraints and permitted candidates
          |
          v
local semantic choice engine
          |
          v
independent Execution Gate
```

For the ASL reference domain, the future model might choose among already legal domain actions or abstain when evidence is insufficient. It must not determine authoritative rules, invent legal actions, or mutate game state. ASL-specific encodings, training data, thresholds, and model behavior belong in the ASL domain package or its provider assets, not in the runtime kernel.

## 12. Security, privacy, and supply chain

Local inference reduces external data transfer but does not eliminate governance requirements.

- Tenant data must remain isolated in requests, caches, traces, calibration artifacts, and corpora.
- Sensitive inputs and model outputs remain subject to `SPEC-AUD-2` and applicable retention/redaction policy.
- Model and tokenizer revisions must be pinned and integrity checked.
- Model code, weights, datasets, and transitive runtimes require separate license and provenance review.
- Remote-code loading should be disabled unless an explicit security review approves it.
- Model artifacts should be scanned, inventoried, and reproducibly acquired.
- Logs must not persist raw prompts, tenant data, hidden activations, or secrets by default.
- A local provider must fail closed when its model, calibration profile, or inference runtime is unavailable or incompatible.

This document borrows architectural ideas, not source code. Any future code reuse requires a repository- and dependency-specific license review.

## 13. Explicitly not decided

This document does not decide:

- the foundation model or parameter count;
- decoder/logit-readout versus encoder architecture;
- Python service versus in-process inference;
- CPU, GPU, or accelerator target;
- quantization or precision;
- training framework;
- production deployment topology;
- provider routing or fallback;
- confidence thresholds;
- any ASL-specific model;
- any production-default provider; or
- whether a native local model should ultimately be adopted.

Those decisions require representative evidence.

## 14. Admission gate

The smallest conformant next action is not provider implementation. It is operator-controlled evidence preparation under the PR-19 Evaluation Review.

A local-provider design PR becomes eligible only after the operator supplies:

1. a representative tenant-redacted Decision corpus;
2. independently reviewed expected and acceptable outcomes;
3. ambiguity, risk, and wrong-choice-severity labels;
4. a pinned candidate model and complete inference identity;
5. a finite token, cost, compute, and time budget;
6. repeated offline results; and
7. acceptance thresholds for one bounded Decision class.

Until then, the native local model remains a documented research direction rather than an implementation commitment.

## Appendix A - Reviewed source snapshots

The survey reflects the following repository snapshots reviewed on 2026-09-22:

| Repository | Commit |
| --- | --- |
| `TheoLeeCJ/SemIf` | `1f2dea3` |
| `Octalab-Inc/jqv` | `795586c` |
| `kshetrajna12/reflex` | `231f896` |
| `jaredpalmer/kev` | `84f23b3` |
| `bespokelabsai/nimble` | `f136b3f` |
| `NandhaKishorM/laya` | `c752770` |
| `Heman10x-NGU/Verdict-open-jev` | `30f1556` |
| `fastino-ai/GLiNER2` | `4abb613` |
| `Davipar/djev-dev` | `3ce907e` |

These snapshots are research inputs, not dependencies or approved supply-chain artifacts.
