# LimboDancer.Agentic.CognitiveRuntime PR-19 Evaluation Review

**Status:** Implementation conformant; provider adoption deferred

**Review date:** 2026-09-22

**Reviewed branch:** `decision-plane`

**Baseline:** PR-19 OpenAI Decision provider

## 1. Review decision

PR-19 conforms to the Milestone D boundary for one additional Decision-provider experiment. The OpenAI provider remains disabled by default and cannot create candidates, authorize actions, invoke the Execution Gate, or execute replay results.

Provider adoption is not approved by this review. Recorded transport tests and a synthetic labeled replay corpus prove the boundary and measurement machinery, but they are not evidence of model quality on representative LimboDancer decisions. The deterministic `RuleDecisionProvider` remains the production default.

## 2. Conformance evidence

| Requirement | Decision | Evidence |
| --- | --- | --- |
| Bounded input | Pass | The provider receives only `DecisionContext` and the already permitted candidate set. |
| Structured result | Pass | Responses use strict JSON Schema and are projected into the existing `DecisionResult`. |
| Out-of-set and malformed output | Pass | Unknown candidates, refusal, incomplete output, and malformed results fail rather than becoming abstentions. |
| Time and resource bounds | Pass | Deadline-aware timeout plus conservative pre-invocation token and cost checks are enforced. |
| Cumulative accounting | Pass | Goal orchestration supplies the remaining Decision budget and terminates before the gate when reported use exceeds it. |
| Explicit non-selection | Pass | Abstention and escalation remain distinct from provider failure. |
| Stable identity | Pass | Provider and pinned model/version identity are returned and validated. |
| Default composition | Pass | `RuleDecisionProvider` remains the default; OpenAI requires complete explicit configuration. |
| Replay remains non-authoritative | Pass | Evaluation reconstructs the historical provider boundary and exposes no gate, authorization, or execution operation. |
| Labeled case metadata | Pass | Cases retain acceptable choices, ambiguity, orthogonal `ActionRiskProfile`, and expected wrong-choice severity. |
| Required measures | Pass | Reports expose selection correctness, abstention quality, invalid-result rate, latency, token/cost use, confidence calibration error, deterministic-baseline disagreement, and wrong-choice severity. |

## 3. Evidence interpretation

The automated corpus intentionally uses recorded provider responses and deterministic test providers. It proves that:

- selection, abstention, escalation, invalid output, unexpected abstention, and wrong selection are classified separately;
- invalid output never becomes an executable selection;
- measurements exclude provider failures from latency, confidence, token, and cost samples when no valid result exists;
- ambiguity, risk, and wrong-choice severity survive into each evaluation result; and
- replay evaluation stops after result comparison.

The resulting metric values are conformance fixtures, not performance claims about an OpenAI model. They must not be used to justify enabling the provider.

## 4. Adoption evidence still required

Before the OpenAI provider can be considered for a production default or a defined workload, an offline evaluation must use:

1. a tenant-redacted, representative corpus of historical Decision boundaries;
2. independently reviewed expected outcomes and acceptable-candidate sets;
3. explicit ambiguity, risk profiles, and wrong-choice severity labels;
4. an explicitly pinned model, price inputs, timeout, and finite evaluation budget;
5. repeated results sufficient to characterize correctness, abstention, invalid results, calibration, latency, tokens, cost, and baseline disagreement; and
6. documented acceptance thresholds for the intended decision class.

No live credential, representative corpus, or adoption threshold is committed to source control. Those inputs require an explicit operator-controlled evaluation outside CI.

## 5. Deferred boundaries

This review does not admit:

- provider routing or automatic fallback;
- a second non-reference provider;
- enabling OpenAI by default;
- live-provider CI;
- replay authorization or execution;
- a generalized replay service or public replay API; or
- model-quality, safety, or cost claims beyond the evaluated corpus.

## 6. Next checkpoint

The next checkpoint is an operator-produced evaluation report for one pinned model against a representative labeled corpus. If that evidence supports adoption, a separate review must define the exact admitted decision class and thresholds. Provider routing remains ineligible until at least two non-reference providers have comparable evidence.

Corpus preparation and execution are governed by the [Decision Evaluation Corpus Specification and Runbook](<./LimboDancer.Agentic.CognitiveRuntime Decision Evaluation Corpus Specification and Runbook.md>). The ASL 3.10 rulebook Markdown feeds a reviewed, versioned ASL ontology/rule package that may supply semantic evidence and expert case-construction inputs under that process, but neither the source nor its ontology projection is historical Decision evidence or a provider-adoption corpus by itself.
