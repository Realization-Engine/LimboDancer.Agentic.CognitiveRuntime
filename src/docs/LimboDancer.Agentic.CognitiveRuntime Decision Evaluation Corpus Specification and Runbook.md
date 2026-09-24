# LimboDancer.Agentic.CognitiveRuntime Decision Evaluation Corpus Specification and Runbook

**Status:** Corpus preparation standard; no representative corpus or provider adoption approved

**Date:** 2026-09-23

**Applies to:** offline evaluation of every non-reference `IDecisionProvider`

**Current reference provider:** `RuleDecisionProvider`

## 1. Purpose

This document defines how an operator creates, reviews, freezes, runs, and interprets a representative Decision evaluation corpus without expanding replay into execution authority.

It closes the process gap identified by the PR-19 Evaluation Review. The existing evaluator and synthetic fixtures prove the provider boundary and metric calculations. They do not establish model fitness. This specification defines the evidence required before OpenAI, Anthropic, a native local model, or any other non-reference provider can be considered for one bounded Decision class.

This document is a preparation and evaluation standard. It does not approve a provider, create provider routing, enable a live provider in CI, or make stored evidence executable.

## 2. Governing decision

A provider-adoption claim MUST be based on a frozen, representative, independently reviewed corpus of historical Decision boundaries for a named Decision class.

Authoritative rules, synthetic cases, and expert reconstructions are valuable supporting evidence, but none may be reported as historical Decision evidence. In particular, the ASL 3.01 rulebook Markdown is authoritative source material for rule interpretation and case construction; it is not a record of runtime decisions and does not identify the tactically preferred candidate by itself.

The deterministic `RuleDecisionProvider` remains the production default until a separate adoption review accepts a provider for an explicitly bounded workload.

## 3. Scope and non-goals

This specification governs:

- corpus evidence classes and admissible claims;
- the operator-side corpus manifest;
- case provenance, redaction, labeling, independent review, and adjudication;
- development, validation, and locked-holdout separation;
- integrity and leakage controls;
- provider configuration, budgets, repeated offline runs, and reports; and
- the evidence package supplied to a later adoption review.

It does not define:

- a general replay service or public replay API;
- authorization or execution from stored evidence;
- live-provider CI;
- candidate generation, semantic constraints, or Governance policy;
- a universal threshold suitable for every Decision class;
- provider routing, automatic fallback, or retry policy; or
- an ASL tactical policy or best-move oracle.

## 4. Evidence classes and permitted claims

Every corpus case MUST declare exactly one evidence class.

| Evidence class | Construction | Permitted claim | Provider-adoption weight |
| --- | --- | --- | --- |
| `pipeline-conformance` | Synthetic boundary and recorded provider responses | Serialization, validation, classification, accounting, and reporting work as designed | None |
| `rule-conformance` | Authoritative sources plus deterministic or expert-derived rule application | Candidate legality, rule applicability, exception handling, and source traceability are represented correctly | Supporting only |
| `expert-benchmark` | Expert-authored or reconstructed boundary, independently labeled and adjudicated | Provider behavior on the bounded benchmark | Supporting; not historical representativeness |
| `historical-decision` | Tenant-redacted `RuntimeStepEvidence` captured from an actual Decision boundary, then independently relabeled | Provider behavior on the sampled historical workload | Required for adoption |

Counterfactual variants inherit the evidence class of their parent only for provenance; they MUST also be marked `counterfactual: true`. They are useful for sensitivity and invariance testing but MUST NOT inflate historical sample counts or cross a split boundary independently of their parent.

An evaluation report MUST publish metrics by evidence class before publishing any combined view. Combined metrics MUST NOT conceal the absence or underperformance of `historical-decision` cases.

## 5. Runtime case contract and corpus overlay

The executable unit remains the existing `DecisionEvaluationCase`:

```text
RuntimeStepEvidence
+ ExpectedOutcome
+ AcceptableCandidateIds
+ IsAmbiguous
+ ActionRiskProfile
+ DecisionWrongChoiceSeverity
```

The evaluator reconstructs `DecisionContext` from the evidence and supplies only the preserved permitted candidates. It compares the returned `DecisionResult`; it has no gate, authorization, or execution operation.

Provenance, reviews, split membership, and corpus governance are operator-side metadata. They MUST be stored in a corpus manifest and compiled or projected into `DecisionEvaluationCase` instances. They MUST NOT be smuggled into provider-visible Goal text, observations, candidate arguments, reason codes, or evidence references in a way that reveals the expected label.

### 5.1 Required corpus manifest

The frozen manifest MUST contain:

| Field | Requirement |
| --- | --- |
| `corpusId` | Stable, non-secret identifier. |
| `corpusVersion` | Immutable version for the frozen contents. |
| `decisionClass` | Named workload with inclusion and exclusion criteria. |
| `evidenceClasses` | Classes present and case counts by class. |
| `createdAt` | UTC creation time. |
| `sourceRegistry` | Versioned source records used by cases. |
| `caseIndex` | Ordered case records and content hashes. |
| `splitPolicy` | Grouping, stratification, leakage controls, and split names. |
| `thresholdProfile` | Predeclared acceptance thresholds and rationale. |
| `reviewPolicy` | Required roles, independence, and adjudication procedure. |
| `redactionPolicy` | Rules, version, reviewer, and prohibited fields. |
| `integrity` | Canonicalization method and SHA-256 digest of every frozen artifact. |
| `distribution` | Access, retention, licensing, and redistribution restrictions. |

The manifest MUST identify its schema version. A changed case, label, source citation, threshold, or split assignment produces a new corpus version and new hashes.

### 5.2 Required case record

Each manifest case record MUST contain:

| Field | Requirement |
| --- | --- |
| `caseId` | Stable identifier unique within the corpus. |
| `evidenceClass` | One class from section 4. |
| `decisionClass` | Must match the corpus or an explicitly declared subclass. |
| `familyId` | Groups a historical event and all reconstructions or counterfactuals derived from it. |
| `leakageGroupId` | Groups near-duplicates that MUST remain in one split. |
| `split` | `development`, `validation`, or `holdout`. |
| `evidenceArtifact` | Location and SHA-256 of the serialized `RuntimeStepEvidence`. |
| `expectedOutcome` | `Selected`, `Abstained`, or `Escalated`. |
| `acceptableCandidateIds` | Non-empty only when the expected outcome is `Selected`; every identifier must be in the preserved permitted set. |
| `isAmbiguous` | Whether more than one defensible outcome or materially incomplete evidence remains after review. |
| `actionRisk` | The existing orthogonal `ActionRiskProfile`; not a label for tactical desirability. |
| `wrongChoiceSeverity` | Expected consequence of an incorrect selection: `Low`, `Medium`, `High`, or `Critical`. |
| `ruleEvidenceRefs` | Sources supporting legality, applicability, and constraints. |
| `semanticPackageRefs` | Exact ontology/rule-package versions used to resolve entities, conditions, exceptions, and candidates. |
| `decisionEvidenceRefs` | Sources supporting the expected outcome and acceptable set. |
| `counterfactual` | Boolean plus parent case when true. |
| `representativeness` | Prespecified workload strata used for coverage reporting. |
| `redaction` | Policy version, reviewer, status, and transformation log reference. |
| `labelReview` | Author, independent reviewer, disposition, adjudicator if needed, timestamps, and rationale hash. |
| `caseStatus` | `draft`, `reviewed`, `adjudicated`, `excluded`, or `frozen`. Only frozen cases may enter a reported run. |

`ruleEvidenceRefs`, `semanticPackageRefs`, and `decisionEvidenceRefs` are deliberately separate. A rule citation and its reviewed ontology projection can prove that an option is legal or illegal. They usually cannot prove that one legal option is tactically preferable.

### 5.3 Label semantics

- `Selected` means at least one candidate is acceptable. The acceptable set records all choices the reviewers are willing to score as correct; it is not a ranked list.
- `Abstained` means the provider should decline to choose from the bounded set. It is not provider failure.
- `Escalated` means the boundary should request higher authority or additional judgment. It is distinct from abstention and failure.
- Refusal, malformed output, truncation, unexpected provider stop, transport failure, identity mismatch, and out-of-set selection are provider failures, never labels.
- `isAmbiguous` describes the case after review. It does not automatically make every response correct.
- `wrongChoiceSeverity` describes the consequence of an incorrect selected candidate for this case. It is independent of the selected action descriptor's operational `ActionRiskProfile`.

## 6. Source provenance

Each source record MUST include:

- source identifier and type;
- title, edition or version, and publisher or owner when applicable;
- acquisition location and access date;
- repository commit, object identifier, or immutable external version;
- file path and content hash;
- page, section, rule, table, figure, or event locator;
- transcription or conversion method and known defects;
- verification status against the authoritative original; and
- access, licensing, retention, and redistribution restrictions.

Source references SHOULD identify fragments rather than only files. A fragment reference SHOULD be resolvable from the frozen source version and include a fragment hash.

## 7. ASL 3.01 rulebook profile

The Markdown under `docs/ASL/Rulebook_Markdown/` is admitted as an authoritative-source preparation input for the ASL reference domain, subject to human verification against the source edition for rule-critical use.

### 7.1 Stable rule references

Rule identifiers MUST be normalized to include the chapter even where the converted Markdown contains only a local numeric heading. Examples:

```text
asl-3.01:A.6
asl-3.01:A1.1
asl-3.01:B1.13
asl-3.01:C1.2
```

Each reference MUST also retain:

- repository commit SHA;
- Markdown path;
- nearest `<!-- page N -->` marker;
- normalized rule identifier;
- fragment SHA-256; and
- dependent table, chart, or image identifiers when the prose is incomplete without them.

The normalized identifier is a corpus locator, not a rewrite of the published rules.

### 7.2 Conversion limitations

The current conversion preserves page markers, headings, inline emphasis, many tables, and extracted figures, but it is not a lossless semantic edition. Corpus authors MUST account for:

- chapter-local rule numbers that require chapter-aware normalization;
- OCR or extraction hyphenation and line-join artifacts;
- columnar tables rendered as laid-out text;
- rules whose meaning depends on a chart, counter image, diagram, or footnote;
- cross-references whose displayed text is not a stable hyperlink; and
- headings inferred from PDF bookmarks or typography.

A rule-critical citation remains `unverified` until a reviewer compares the cited fragment and every required visual/table dependency with the authoritative edition. Unverified fragments may be used for exploration, never for a frozen label.

### 7.3 What the rulebook can and cannot establish

The rulebook can support:

- semantic action and candidate definitions;
- deterministic legality and applicability checks;
- rule hierarchy, exception, cross-reference, and explanation evidence;
- expert construction and review of decision situations; and
- a separate ASL rule-conformance suite.

The rulebook cannot alone establish:

- what a player or production runtime historically chose;
- that a reconstructed position is representative of the target workload;
- which of several legal choices is tactically best;
- an independently reviewed acceptable-candidate set; or
- provider fitness or production adoption.

Copyrighted rule text and images SHOULD remain in the controlled source registry. Evaluation artifacts SHOULD prefer stable locators, short necessary excerpts, and hashes over duplicating source material. Distribution MUST follow the recorded source restrictions.

### 7.4 Ontology transformation boundary

The ontology-first authoring lifecycle is governed by the [ASL Ontology Transformation Specification](<../ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>).

The intended ASL pipeline is ontology-first:

```text
verified rulebook fragments
-> structure-aware extraction
-> proposed ASL ontology artifacts
-> deterministic validation and expert review
-> immutable published ASL ontology/rule package
-> semantic resolution, adjudication, and candidate constraints
-> Decision boundary only when multiple permitted actions remain
```

The ontology package MUST represent the distinctions required by `ASL-RD-001` through `ASL-RD-015`, including canonical source identity, rules, definitions, examples, conditions, effects, exceptions and nested exceptions, cross-references, tables or matrices, phase restrictions, entities, properties, relations, aliases, and provenance. Source text, the extracted intermediate representation, and the published semantic artifact MUST remain traceable in both directions.

An extraction model or LLM MAY propose artifacts. It MUST NOT publish them as authoritative. Publication requires deterministic validation plus the declared human review. Validation MUST cover at least canonical identifier preservation, hierarchy integrity, reference resolution, exception/precedence consistency, required visual and table dependencies, terminology consistency, duplicate or conflicting definitions, provenance completeness, and package/version isolation.

The legacy `LimboDancer.MCP.Ontology` code is reference material for generic entities, properties, relations, enums, aliases, shapes, provenance, tenant scope, validation, export, and repository boundaries. It is not a completed ASL ingestion implementation: the historical design's specialized rule extraction and `RuleNode`, `ExceptionNode`, `ConditionNode`, `ReferenceNode`, `ExampleNode`, `DefinitionNode`, `PhaseNode`, and `MatrixRuleNode` capabilities are not present as an operational pipeline in the checked-in legacy source. New ASL packages MUST selectively reimplement useful behavior behind the current domain-package and runtime contracts rather than restoring the MCP-centered architecture.

Every corpus case whose candidates depend on ASL semantics MUST pin the published ontology/rule-package identity and content hash in `semanticPackageRefs`. Changing that package invalidates the case's semantic derivation until the candidate set and label are revalidated. The ontology package can establish meaning and permitted alternatives; it does not supply a tactical preference label unless the published artifact contains an independently governed policy expressly intended to do so.

## 8. Decision-class definition

Corpus construction MUST begin with one bounded Decision class, not with a provider or a convenient set of examples.

The class definition MUST state:

- operational purpose and intended consumer;
- inclusion and exclusion criteria;
- required Goal, observation, and candidate information;
- how candidates are generated and deterministically constrained before Decision;
- the meaning of selection, abstention, and escalation;
- consequence model and severity-label guidance;
- workload population and sampling frame;
- representativeness strata and minimum coverage;
- known sources of ambiguity; and
- conditions that make a case unusable.

ASL read-only rule adjudication produces a `DomainConclusion` and belongs primarily to reference-domain conformance. It MUST NOT be relabeled as a Decision-provider selection problem merely to create a corpus. An ASL Decision class becomes eligible only when the runtime supplies a genuine bounded set of already permitted alternatives whose choice advances a defined Goal.

## 9. Corpus construction workflow

### 9.1 Acquire

1. Select the approved Decision class and sampling frame.
2. Export historical `RuntimeStepEvidence` through an operator-controlled process. Preserve the exact Goal, budget, observations, candidate partition, permitted candidates, original Decision if present, descriptor versions, and state-version references.
3. Assign source, family, and leakage-group identifiers before labeling.
4. Record collection gaps and excluded events. Do not silently curate away hard failures.

Historical collection SHOULD cover the natural frequency of common cases while deliberately retaining prespecified rare, ambiguous, abstention, escalation, and high-consequence strata. Any deliberate oversampling MUST be declared and weighted or separately reported.

### 9.2 Redact

1. Apply the versioned redaction policy in a controlled environment.
2. Remove credentials, access tokens, personal data, tenant secrets, unnecessary payload fields, and external identifiers.
3. Replace stable identifiers consistently only where relationship preservation is required.
4. Preserve semantics, candidate membership, ordering when meaningful, numeric scale, temporal relationships, state versions, and risk characteristics needed for the decision.
5. Have a reviewer who did not perform the transformation confirm both confidentiality and semantic fidelity.

Redaction that changes the decision or makes the expected result inferable from artificial tokens invalidates the case. Raw evidence MUST not be committed to this repository. Retention and destruction remain operator responsibilities.

### 9.3 Normalize and validate

Before labeling, tooling or reviewers MUST confirm:

- non-empty and unique case, invocation, step, and candidate identities;
- tenant consistency across Goal and observations;
- a complete candidate partition where candidates are present;
- every acceptable identifier belongs to the permitted set;
- no rejected candidate is exposed to the provider as permitted;
- original Decision membership and identity are internally valid when present;
- state and descriptor versions are preserved;
- referenced source fragments resolve and match their hashes; and
- no expected label or reviewer rationale appears in provider-visible fields.

### 9.4 Label independently

1. A case preparer assembles the boundary and source packet but does not make the final label alone.
2. A primary annotator assigns outcome, acceptable candidates, ambiguity, and wrong-choice severity with rationale.
3. An independent reviewer receives the same provider-visible boundary and source packet without seeing the primary label or original provider selection.
4. Agreement produces a reviewed case.
5. Disagreement goes to a named adjudicator; it is never resolved by majority inference or by treating the historical choice as truth.
6. The final rationale and disposition are hashed and retained in the controlled review record.

For specialized domains, annotators and adjudicators MUST meet the declared expertise requirements. Rule citations support their reasoning but do not replace independent judgment.

### 9.5 Freeze

Only reviewed or adjudicated cases may be frozen. Freezing assigns the corpus version, split, ordered index, canonical serialization, and hashes. The operator MUST archive the manifest, source registry snapshot, case artifacts, threshold profile, and review attestations together.

## 10. Split and leakage policy

Every frozen corpus MUST contain:

- `development`: visible during integration and prompt/configuration work;
- `validation`: used for bounded model/configuration selection; and
- `holdout`: locked until the provider, configuration, and thresholds are frozen.

Assignment MUST occur by `leakageGroupId`, never by individual case. The same historical event, scenario, source example, reconstructed position, templated variation, or counterfactual family MUST remain in one split.

Where time is meaningful, later historical periods SHOULD form validation or holdout data. Where authors, tenants, scenarios, or source documents can leak patterns, those groups SHOULD be separated or explicitly balanced. Near-duplicate detection MUST include normalized provider-visible content and provenance relationships, not only case identifiers.

The split policy MUST be fixed before provider comparison. Holdout labels SHOULD be inaccessible to provider implementers. Opening the holdout is a recorded event. After holdout results influence a model, prompt, feature, or threshold, that split is no longer an untouched holdout for the next iteration.

No universal percentage is mandated. The manifest MUST justify that every reported stratum has enough independent cases to support the claim made about it.

## 11. Acceptance-threshold profile

Thresholds MUST be approved before the holdout run and MUST be specific to the Decision class and intended workload.

At minimum the profile MUST define:

- minimum total and per-stratum case counts;
- overall and selection correctness floors;
- expected-abstention correctness floor;
- unexpected-abstention and escalation caps;
- provider-failure and invalid-result caps;
- maximum High and Critical wrong selections;
- calibration measure and bound when confidence is emitted;
- latency percentile bounds and measurement environment;
- token and cost ceilings using pinned prices;
- required number of independent repeated runs;
- allowed run-to-run variance;
- deterministic-baseline comparison and interpretation; and
- rules for ambiguous cases and missing metric samples.

Any Critical wrong selection is a failed adoption run unless the preapproved profile supplies a documented, independently reviewed exception. A profile MUST NOT weaken thresholds after holdout results are known. Baseline disagreement is diagnostic; the baseline's original selection is not automatically the truth.

Passing thresholds supports only the declared provider identity, version, configuration, Decision class, workload envelope, and corpus version. It is not a general model-quality or safety claim.

## 12. Operator runbook

### Phase 1: authorize the evaluation

- Name the operator, reviewer, Decision class, provider, intended claim, and maximum spend.
- Confirm that credentials, corpus artifacts, and raw outputs remain outside source control and CI.
- Confirm that the run cannot call the Execution Gate, create `AuthorizedAction`, or invoke an executor.

### Phase 2: freeze inputs

Record and hash:

- repository commit containing evaluator code;
- corpus identifier, version, manifest hash, and split;
- provider ID and provider implementation version;
- exact model or inference artifact identifier;
- endpoint or runtime identity;
- system/developer instructions and structured-output schema version;
- generation parameters, seed when supported, and locale;
- SDK/package versions and retry configuration;
- timeout, input/output token limits, per-case cost limit, and total run budget;
- input and output token prices plus currency and effective date; and
- evaluator/reporting tool version.

SDK retries MUST be disabled unless a future design explicitly admits and measures them. Repeated evaluation runs are independent observations, not hidden retries.

### Phase 3: preflight

1. Verify every artifact hash and signature or attestation required by policy.
2. Validate all cases against the runtime and manifest invariants.
3. Scan provider-visible payloads and output locations for secrets and label leakage.
4. Confirm the provider is tool-free and cannot execute or retrieve undeclared external context.
5. Calculate a conservative maximum token and monetary exposure; refuse to start if it exceeds the authorized budget.
6. Exercise cancellation, timeout, malformed output, out-of-set output, and budget termination on conformance fixtures.

### Phase 4: development and validation

Use `development` for integration. Use `validation` only for the bounded choices declared by the evaluation plan. Record every prompt, model, feature, or configuration considered. Do not inspect holdout labels or results.

Select exactly one frozen provider configuration for holdout. Reapprove the threshold profile if the intended workload changed; do not alter it merely because validation performance is inconvenient.

### Phase 5: holdout execution

1. Record the holdout opening event and reverify hashes.
2. Invoke the provider once per case per declared run under the same frozen configuration.
3. Preserve the raw request digest, raw response or failure category, validated `DecisionResult`, latency, token usage, cost, and provider identity.
4. Stop before the next case when cancellation, deadline, token budget, or cost budget requires termination.
5. Never convert failure into abstention, retry a bad result invisibly, or continue beyond the authorized budget.
6. Repeat the complete run only as predeclared. Preserve each run separately.

Case order SHOULD be deterministically shuffled per run with the shuffle seed recorded, unless order is part of the Decision class.

### Phase 6: report

The report MUST include:

- all frozen identities, versions, parameters, prices, budgets, hashes, and timestamps;
- corpus counts by evidence class, split, representativeness stratum, ambiguity, risk, and severity;
- correctness, selection correctness, abstention quality, escalation, provider failure, invalid result, baseline disagreement, and wrong-choice severity;
- calibration, latency distribution, token use, and cost;
- every High or Critical wrong selection as an individually reviewed finding;
- per-run results and aggregate variability;
- incomplete-run accounting and excluded cases with reasons;
- every threshold with pass/fail status; and
- limits on the claim supported by the run.

Metrics MUST be reported separately for `historical-decision` cases and for every materially different stratum. Confidence, latency, token, and cost sample counts MUST be explicit when failures produce no valid `DecisionResult`.

### Phase 7: decide and archive

An independent reviewer verifies the report against raw results and the frozen threshold profile. The result is one of:

- `rejected`;
- `insufficient-evidence`;
- `eligible-for-bounded-adoption-review`; or
- `evaluation-invalid`.

Evaluation success does not itself enable the provider. A separate adoption review MUST name the exact Decision class, configuration, thresholds, monitoring, rollback, and composition change. Provider routing remains ineligible until at least two non-reference providers have comparable evidence.

Archive the complete evidence package under the declared retention and access policy. Publish only redacted reports and artifacts whose distribution is authorized.

## 13. Required evaluation package

A corpus-backed adoption review is incomplete without:

1. the Decision-class definition;
2. frozen corpus manifest and source registry;
3. case and source hashes;
4. redaction policy and review attestation;
5. label review and adjudication attestations;
6. split and leakage report;
7. preapproved threshold profile;
8. frozen provider/run configuration;
9. raw and validated per-case results for every repeated run;
10. aggregate and stratum-level report;
11. independent report review; and
12. explicit claim and limitations.

Missing material yields `insufficient-evidence` or `evaluation-invalid`, not an inferred pass.

## 14. Repository and operational boundaries

This repository MAY contain:

- this specification;
- schema definitions and validation tooling;
- synthetic conformance fixtures;
- rule-source locators and non-sensitive verification metadata; and
- redacted example manifests that make no performance claim.

This repository MUST NOT contain:

- live credentials;
- tenant-identifying raw evidence;
- the representative operator corpus unless its governance explicitly permits publication;
- hidden holdout labels;
- unlicensed rulebook redistribution through generated corpus artifacts; or
- an adoption claim without its review evidence.

Evaluation tooling MUST remain replay-only. It MUST NOT expose authorization, gate, or execution methods.

## 15. ASL work enabled by the new commits

The ASL 3.01 A-E Markdown set enables the following bounded work now:

1. freeze a source-registry snapshot at the rulebook commit;
2. build chapter-aware rule-fragment locators with page and dependency hashes;
3. define a source-to-ontology intermediate representation for rules, definitions, conditions, effects, exceptions, references, examples, phases, and matrices;
4. transform verified fragments into proposed ontology artifacts while retaining bidirectional provenance;
5. validate canonical IDs, hierarchy, references, precedence, terminology, required visuals/tables, coverage, and conflicts;
6. review and publish an immutable, versioned ASL ontology/rule package through the separate domain-package boundary;
7. use that package to drive ASL rule-conformance cases and the first read-only adjudication slice; and
8. define a genuine ASL Decision class only after the runtime exposes bounded, already permitted alternatives.

These steps realize the original ontology-first product intent within the current cognitive-runtime architecture. They improve ASL source and semantic evidence, but they do not satisfy the provider-adoption gate until representative historical Decision boundaries and independent decision labels exist.

## 16. Exit criteria for corpus readiness

A corpus is ready for a provider holdout run only when:

- one Decision class and intended claim are fixed;
- required historical evidence and representativeness strata are present;
- every case is redacted, validated, independently reviewed, and frozen;
- all source fragments used for frozen labels are verified;
- family and leakage groups are assigned and split checks pass;
- the holdout remains untouched;
- artifact hashes reproduce the manifest root digest;
- provider-visible payloads contain no labels or review rationale;
- thresholds and run budget are preapproved; and
- an operator and independent report reviewer are assigned.

Until then, corpus work is preparation evidence and MUST NOT be represented as provider qualification.
