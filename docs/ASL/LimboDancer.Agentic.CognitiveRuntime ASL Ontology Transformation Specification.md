Warning: truncated output (original token count: 8000)
Total output lines: 567

# LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification

**Status:** Accepted; ASL-OT-01 and ASL-OT-02 complete, ASL-OT-03 in progress

**Date:** 2026-09-23

**Reference domain:** Advanced Squad Leader 3.01, Chapters A-E

**Authority:** Subordinate to the Plane Runtime Specification and Plane Runtime Design; implements the ASL reference-domain requirements and Domain Integration Model

## 1. Purpose

This specification defines how LimboDancer transforms the ASL 3.01 rulebook into a reviewed, immutable, versioned ontology and rule package that the cognitive runtime can resolve exactly.

It restores the original ontology-first product intent within the current architecture:

```text
authoritative rulebook
-> verified source fragments
-> structured intermediate representation
-> proposed semantic artifacts
-> deterministic validation and expert curation
-> published ASL ontology/rule package
-> state-aware semantic resolution
-> evidence-backed DomainConclusion
   or
-> bounded semantic actions governed by the common authority path
```

The transformation lifecycle is domain authoring. It does not run inside a live Goal invocation, authorize actions, or make an extraction model authoritative.

## 2. Governing decisions

1. The ASL implementation belongs in one or more separate domain packages. `LimboDancer.Runtime` MUST NOT reference ASL assemblies or contain ASL vocabulary.
2. The rulebook is authoritative source material. Generated ontology artifacts are trusted only after validation, human review, and immutable publication.
3. Canonical published identifiers and source wording MUST remain recoverable. Normalized identifiers and semantic mappings may supplement them but MUST NOT silently replace them.
4. Rules, definitions, conditions, effects, exceptions, nested exceptions, references, examples, tables, phases, and terminology MUST remain distinguishable artifacts.
5. Partial formalization MUST be represented explicitly. Unmodeled prose MUST NOT be treated as executable semantics.
6. Runtime package resolution is exact-version only. The runtime MUST NOT silently upgrade or regenerate a package.
7. A `DomainConclusion` is evidence-backed interpretation, not execution authority. Any mutation independently enters the registered action and Execution Gate path.
8. Graph, vector, LLM, classifier, parser, and calculation outputs are evidence or proposals. None is authority merely because it produced a result.

## 3. Scope

This specification covers:

- the ASL Markdown source profile;
- canonical source identity and fragment location;
- the transformation intermediate representation;
- semantic artifact kinds and relationships;
- provenance and formalization status;
- deterministic and expert validation;
- curation, publication, versioning, and compatibility;
- runtime consumption through existing domain-neutral contracts;
- the first occupied-building entry adjudication slice; and
- conformance and acceptance tests.

It does not prescribe:

- a graph or vector database product;
- RDF, OWL, JSON-LD, SHACL, or a property graph as the sole canonical representation;
- a generalized runtime rule engine;
- a generalized plugin registry or dynamic domain loading;
- live ontology generation during runtime invocation;
- a complete formalization of Chapters A-E in the first slice;
- ASL-specific contracts in `LimboDancer.Abstractions` or `LimboDancer.Runtime`;
- line-of-sight or spatial-calculation interfaces before a selected scenario requires them;
- tactical policy or best-move labels; or
- provider evaluation or adoption.

## 4. Ownership and dependency boundary

| Concern | Owner |
| --- | --- |
| Source registration, extraction, curation, and ASL publication | ASL authoring implementation |
| ASL vocabulary, rule semantics, precedence, and package-internal models | ASL domain package |
| Published package persistence and retrieval implementation | ASL infrastructure behind inward-facing ports |
| Exact package identity, canonical references, evidence references, questions, and conclusions | Existing domain-neutral Abstractions |
| Runtime package registration and exact resolution | Runtime plus Host composition |
| Current board and game observations | State providers composed for the ASL package |
| Domain adjudication and explanation | ASL implementation of approved domain-resolution contracts |
| Registered calculations and actions | ASL package through existing runtime ports |
| Governance, Diagnostics, Decision, Execution Gate, audit, and verification | LimboDancer runtime |

The Host composes the runtime and ASL packages. Authoring tools may be separate executables or offline workflows; they do not become runtime authority or require a generalized domain framework.

## 5. Source profile

### 5.1 Registered source set

The initial transformation source is the ASL 3.01 Markdown set under `docs/ASL/Rulebook_Markdown/`:

- table of contents;
- index and glossary;
- Chapter A, Infantry and Basic Game Rules;
- Chapter B, Terrain;
- Chapter C, Ordnance and Offboard Artillery;
- Chapter D, Vehicles;
- Chapter E, Miscellaneous; and
- extracted figures under `images/`.

The edition source boundary is **the TOC, Index/Glossary, and Chapters A–E only** (physical PDF pages 6–253 for the supplied 716-page PDF). Material after Chapter E is outside this source baseline. A reference to an excluded section remains an unresolved or separately scoped dependency, never an implicit extension of the ontology source set.

**Boundary finding (2026-09-24):** A4.13 cites the B. Terrain Chart, which appears in the supplied PDF on physical page 698 with other A–E-labelled back-matter aids. Thus physical pages 6–253 describe the **initial registered conversion**, not necessarily every artifact needed to interpret A–E. The [back-matter source boundary review](<./LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>) records a separately registered, **unverified** bounded chart transcription. Page 698 remains an unresolved external dependency until source verification and an explicit applicability/admission decision; the existing 11 source attestations do not cover it.

The source registry MUST pin the repository commit, file path, whole-file SHA-256, edition, chapter, page range, conversion tool version, and applicable distribution restrictions.

### 5.2 Conversion characteristics

The Markdown includes `<!-- page N -->` markers, headings, emphasized rule identifiers, extracted figures, tables, chart-like text, footnotes, and cross-references. It also contains conversion risks:

- chapter-local numeric headings that require chapter context;
- inferred heading levels;
- line-join and hyphenation artifacts;
- columnar material rendered as laid-out text;
- references not represented as stable links;
- meaning split across prose, footnotes, tables, charts, and images; and
- typography that carries semantic distinctions not fully preserved in Markdown.

The Markdown is an authoring source, not a lossless semantic edition. A rule-critical artifact cannot reach `Accepted` status until its source fragments and required visual dependencies are checked against the authoritative edition.

## 6. Identity model

### 6.1 Identity layers

The transformation MUST keep these identities distinct:

| Identity | Purpose |
| --- | --- |
| Source identity | Exact edition and registered source artifact. |
| Published element identity | Identifier exactly as published, when present. |
| Fragment identity | Immutable location and hash for the source material used. |
| Semantic artifact identity | Stable package-local identity for an ontology artifact. |
| Package identity | Exact `DomainPackageRef` used at runtime. |

The initial domain identity is an opaque ASL `DomainId`; its final string value is an implementation decision. The package ID and version MUST be explicit and immutable after publication.

### 6.2 Published rule identifiers

Chapter context MUST be added when the extracted Markdown presents only a local number. For example, a local `1.13` beneath Chapter B is resolved as published rule `B1.13`. Punctuation variants used for discovery MUST NOT overwrite the preserved published form.

A rule locator SHOULD retain:

```text
source edition
source file and hash
page marker
published rule identifier
heading path
fragment hash
required table/figure/footnote dependencies
```

Canonical runtime references use the existing `CanonicalReference` contract: exact package, source ID, element ID, and optional source version. The ASL package owns interpretation of the opaque element ID.

### 6.3 Semantic identifiers

Semantic artifact identifiers MUST be stable within a published package and MUST use the existing opaque `SemanticIdentifier` at runtime boundaries. They MUST NOT encode storage keys, graph-provider syntax, C# type names, or mutable display labels.

Aliases improve discovery but always resolve to a canonical semantic identifier with explicit ambiguity handling.

### 6.4 Source fragments

A source fragment is an immutable, hash-addressed, precisely located unit of content from a registered authoritative source. It preserves the source evidence from which transformation artifacts are proposed, reviewed, and published.

A fragment is a provenance unit. It is not an ontology artifact, a semantic rule, an arbitrary token window, or a vector-index chunk. Its record MUST contain:

- registered source identity and path;
- exact start and end lines plus applicable page markers and heading path;
- exact extracted content hash;
- a fragment identity derived from its source, locator, and content hash;
- structural kind;
- published and chapter-normalized element identifiers when applicable;
- required image or other source dependencies;
- relevant conversion markers such as inline footnote references; and
- verification status against the authoritative edition.

Fragments SHOULD follow structural source boundaries such as a heading, rule paragraph, continuation, figure reference, or laid-out table/chart block. A rule spanning pages is represented by ordered fragments rather than a fabricated contiguous source passage. Conversely, one fragment may support several proposed semantic artifacts, such as a Rule, Condition, Exception, and CrossReference.

Original extracted content remains unchanged. Corrected, dehyphenated, or search-normalized text is a derived representation with its own transformation provenance; it MUST NOT silently replace the fragment. Vector chunks MAY be derived from fragments for discovery, but they are rebuildable retrieval projections and are not authoritative source fragments.

Fragment identity proves which source bytes and location supported a proposal. It does not prove that the proposal's semantic interpretation is correct.

## 7. Transformation intermediate representation

The authoring intermediate representation, abbreviated `TIR`, is the loss-aware boundary between source extraction and reviewed ontology publication. It is ASL-package-owned and does not belong in runtime Abstractions.

### 7.1 Common artifact envelope

Every TIR artifact MUST carry:

| Field | Meaning |
| --- | --- |
| `artifactId` | Stable authoring identity. |
| `artifactKind` | One kind from section 7.2. |
| `packageCandidate` | Candidate package identity; not yet a published package. |
| `publishedId` | Exact published identifier when one exists. |
| `semanticId` | Proposed canonical semantic identity when assigned. |
| `sourceFragments` | Ordered source fragment references with hashes. |
| `dependencies` | Other source or semantic artifacts required to interpret this artifact. |
| `origin` | `extracted`, `curated`, or `derived`. |
| `formalizationStatus` | `unmodeled`, `partial`, or `validated`. |
| `reviewStatus` | `captured`, `proposed`, `in-review`, `accepted`, `rejected`, or `superseded`. |
| `confidence` | Extraction/curation signal; never authority. |
| `createdBy` | Tool/model/configuration or human identity. |
| `createdAt` | UTC creation time. |
| `reviewRecordRefs` | Immutable review and adjudication records. |

### 7.2 Required artifact kinds

The TIR MUST distinguish at least:

| Kind | Required semantics |
| --- | --- |
| `SourceFragment` | Source text or visual dependency and exact locator. |
| `Rule` | Published rule or clause, hierarchy, scope, and source wording. |
| `Definition` | Controlled term and defining source. |
| `Condition` | Circumstance under which a rule, effect, or exception applies. |
| `Effect` | Semantic consequence of an applicable rule; not an execution effect. |
| `Exception` | Modification to a rule or exception, its condition, target, and precedence evidence. |
| `CrossReference` | Typed link between published elements. |
| `Example` | Illustrative material linked to the semantics it illustrates. |
| `Table` | Structured axes, headers, cells, notes, and source dependencies. |
| `PhaseRestriction` | Turn/phase/timing applicability. |
| `Term` | Canonical term, capitalization, aliases, and definition links. |
| `EntityType` | Domain concept classification. |
| `Property` | Typed characteristic owned by or applicable to an entity type. |
| `Relation` | Typed semantic relationship between concepts or instances. |
| `Enumeration` | Closed values where the source establishes closure. |
| `ValidationShape` | Package-internal structural or semantic invariant. |
| `CalculationRequirement` | Deterministic fact required but not supplied by prose alone. |

An artifact may reference several kinds, but it MUST NOT collapse them into one untyped text chunk.

### 7.3 Rule hierarchy

Rule hierarchy MUST be explicit. A child identifies its direct parent, and sibling ordering is preserved when meaningful. Validation MUST detect missing parents, duplicate canonical identifiers, illegal cycles, and hierarchy inferred only from a malformed heading.

The published wording remains attached to the Rule artifact even after formal conditions or effects are modeled.

### 7.4 Conditions and semantic expressions

Conditions MAY be represented by a bounded expression tree whose operands are semantic identifiers, typed properties, literals, entity references, observations, or calculation references. Operators MUST be explicitly declared and type checked.

The first slice SHOULD introduce only operators exercised by occupied-building entry eligibility. Unsupported prose remains attached with `formalizationStatus: partial` or `unmodeled`; it MUST NOT be converted into an assumed Boolean result.

### 7.5 Exception…700 tokens truncated…posed normalized fields while retaining original values and transformation records. Ambiguous normalization remains unresolved.

### Stage 5: Build proposed semantics

Map accepted structure into entity types, properties, relations, conditions, exception links, tables, shapes, and calculation requirements. Formalize only the semantics needed by approved scenarios, and mark the rest honestly.

### Stage 6: Validate

Run the gates in section 10. A failed gate returns artifacts to proposal or review; it never publishes them implicitly.

### Stage 7: Review and curate

A domain reviewer compares proposed artifacts with every required source fragment and dependency. Material disputes require an adjudicator. Accepted corrections are new curated artifacts with provenance, not edits that erase extraction history.

### Stage 8: Publish

Produce an immutable package manifest, semantic artifacts, source registry snapshot, indexes, validation report, review attestations, compatibility declaration, and content hashes. Publication assigns the final exact `DomainPackageRef`.

### Stage 9: Resolve at runtime

The Host registers the published package. `IDomainPackageResolver` resolves only the exact requested identity. Runtime components consume accepted artifacts; they do not invoke authoring or silently reinterpret source material.

## 10. Validation and publication gates

A candidate package MUST pass all applicable gates.

### 10.1 Source integrity

- every source and fragment hash resolves;
- page, heading, rule, table, footnote, and figure locators are valid;
- required visual dependencies are present; and
- rule-critical fragments are verified against the authoritative edition.

### 10.2 Identity integrity

- published identifiers are preserved;
- normalized identities are unique within scope;
- aliases resolve explicitly or remain ambiguous;
- package and source versions are exact; and
- no storage or implementation identifier masquerades as semantic identity.

### 10.3 Structural integrity

- hierarchy is complete and acyclic where required;
- cross-references resolve or are reported unresolved;
- tables have consistent axes and cells;
- definitions, examples, and normative rules remain distinct; and
- package dependencies are closed and version compatible.

### 10.4 Semantic integrity

- expression operands and operators are type compatible;
- conditions and effects identify their governing rules;
- exceptions identify targets and supported precedence;
- conflicting definitions or rules are surfaced;
- closed enumerations are source-supported; and
- partial or unmodeled semantics cannot produce definitive rule evaluation.

### 10.5 Provenance and review integrity

- every accepted artifact has source provenance;
- derived artifacts identify all inputs;
- required reviews and adjudications are complete;
- model-produced proposals are distinguishable from curated artifacts; and
- excluded material and known coverage gaps are reported.

### 10.6 Runtime-boundary integrity

- the package can be identified by an exact `DomainPackageRef`;
- canonical references round-trip without ASL parsing in the runtime kernel;
- package resolution cannot float to another version;
- ASL assemblies do not become runtime dependencies; and
- no package artifact grants action authority.

Publication is atomic. A package is either fully published with a valid manifest root hash or unavailable.

## 11. Published package profile

The published package manifest MUST include:

- exact domain, package, and semantic version;
- immutable package content digest;
- ontology/TIR schema versions;
- source registry snapshot and digests;
- artifact inventories and per-file hashes;
- canonical source references exposed through `DomainPackageDescriptor`;
- package dependencies and exact compatible versions;
- transformation tool/model/configuration identities;
- validation report and coverage summary;
- review and adjudication attestations;
- known unmodeled, partial, ambiguous, or conflicting semantics;
- compatibility and migration classification;
- creation, publication, and supersession timestamps; and
- access, licensing, and redistribution restrictions.

The canonical serialized representation remains an implementation choice until the first slice proves its needs. Graph and vector projections are rebuildable indexes, not the sole authoritative package artifact.

## 12. Versioning and change control

Published packages are immutable. Corrections produce a new version.

| Change | Minimum classification |
| --- | --- |
| Metadata or index rebuild with identical semantic digest | Rebuild; package identity unchanged only if manifest policy permits reproducible projections. |
| Added reviewed artifacts that do not change existing meaning | Minor version. |
| Corrected source mapping, condition, exception, table, or meaning | New version with explicit compatibility review. |
| Removed or renamed semantic identifiers, changed action meaning, or incompatible schema | Major version. |

The package MUST publish a semantic diff: added, removed, changed, and newly ambiguous artifacts; affected source references; and affected conformance scenarios.

Recorded conclusions, observations, replay evidence, and evaluation cases retain the original package identity. They MUST NOT be silently reinterpreted under a newer package.

## 13. Runtime consumption

The existing runtime contracts are sufficient for the first slice:

- `DomainPackageRef` pins the exact ASL package;
- `DomainPackageDescriptor` exposes its canonical sources;
- `CanonicalReference` preserves source element identity;
- `SemanticIdentifier` carries opaque ASL semantic identity;
- `EvidenceReference` identifies source, observation, and calculation evidence;
- `DomainQuestion` binds a question to tenant and package;
- `DomainConclusionContext` supplies the resolved package, entities, observations, and calculation evidence; and
- `DomainConclusion` returns disposition, value, rules, exceptions, evidence, assumptions, ambiguity, reasons, and explanation.

ASL package internals remain behind its `IDomainPackageResolver`, `IDomainEntityResolver`, and `IDomainConclusionResolver` implementations. No new runtime interface is admitted until the first scenario demonstrates that these boundaries are insufficient under the Domain Integration Model's admission rule.

## 14. First implementation slice

### 14.1 Scenario

The first slice is **ASL Scenario A1: occupied-building entry eligibility** from the Milestone A Conformance Review.

Given a known unit, target building location, current phase, occupants, scenario modifiers, and exact ASL package, determine whether the unit may enter now. The result is a `DomainConclusion`; it does not move the unit.

### 14.2 Minimal ontology surface

The slice MUST model only the verified concepts needed to resolve:

- the question kind and its parameters;
- relevant unit classifications and material characteristics;
- target location and building/terrain classification;
- occupancy and occupant relationships;
- timing or phase context;
- applicable scenario or marker modifiers;
- base entry and occupancy rules;
- controlling conditions and exceptions;
- deterministic comparisons or classifications required by those rules; and
- definitive, qualified, indeterminate, and abstained outcomes.

Exact ASL rule identifiers MUST be discovered and verified from the source package during authoring. This specification does not invent them.

### 14.3 Slice exclusions

The first slice does not require:

- line of sight;
- general board geometry;
- all movement rules;
- Chapters A-E completeness;
- generalized inference or exception engines;
- tactical choice among legal moves;
- state mutation; or
- a production persistence product.

### 14.4 Slice acceptance cases

The conformance set MUST include:

- clearly eligible entry;
- clearly prohibited entry;
- one controlling exception;
- nested or competing exception handling if the verified source requires it;
- missing unit, location, phase, occupancy, or modifier evidence;
- ambiguous entity resolution;
- unresolved or conflicting rule/reference evidence;
- exact-package unavailable;
- changed observation version during adjudication;
- cross-tenant or cross-package evidence rejection; and
- proof that the conclusion cannot be executed as an action.

Expected results and citations require independent domain review. Rulebook examples may inspire cases but remain examples, not automatically authoritative acceptance labels.

## 15. Legacy disposition

The legacy `LimboDancer.MCP.Ontology` implementation is reference material, not a dependency.

| Legacy concept | Disposition |
| --- | --- |
| `EntityDef`, `PropertyDef`, `RelationDef`, `EnumDef`, `AliasDef`, `ShapeDef` | Retain the modeling lessons; redesign inside the ASL authoring/package boundary. |
| `ProvenanceRef` | Replace with the richer bidirectional provenance required here. |
| `TenantScope` | Preserve tenant/package isolation through current runtime contracts. |
| `OntologyValidator` and validators | Reimplement scenario-driven validation; do not copy the legacy store coupling. |
| JSON-LD/RDF export | Optional projection after a canonical package representation is proven. |
| Cosmos repository contracts and placeholder implementation | Do not port as architecture; choose persistence after the package profile is proven. |
| Historical `RuleNode`, `ExceptionNode`, `ConditionNode`, and related design | Treat as requirements candidates; they were not an operational checked-in extraction pipeline. |
| MCP tools, generalized plugins, ReAct planning, and Azure topology | Retire from the transformation design. |

Any copied behavior must pass the repository's legacy source admission checklist and gain tests under the current dependency direction.

## 16. Relationship to Decision evaluation

The ontology package constrains meaning, applicability, and candidate legality. It does not automatically identify the tactically preferred permitted action.

Decision evaluation cases that depend on ASL semantics MUST pin the exact package identity and digest through `semanticPackageRefs` under the Decision Evaluation Corpus Specification and Runbook. A package change requires revalidation of derived candidates and labels.

Rule-conformance and DomainConclusion cases validate the ontology and adjudication path. Representative historical Decision boundaries and independently reviewed acceptable choices remain separately required for provider adoption.

## 17. Implementation sequence

The first ASL authoring work SHOULD proceed as these reviewable slices:

1. **ASL-OT-01 — Source registry and fragment locator**: freeze A-E sources, normalize chapter-aware locators, hash dependencies, and verify representative prose/table/figure fragments.
2. **ASL-OT-02 — TIR schema and deterministic extraction**: implement the [C# TIR design](<./LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Schema and Deterministic Extraction Design.md>), define the artifact envelope, and parse hierarchy, identifiers, references, and source boundaries without claiming semantic completeness.
3. **ASL-OT-03 — Validation and review workflow**: implement identity, structure, provenance, formalization, and review-state gates.
4. **ASL-OT-04 — Scenario A1 semantic package**: curate the minimum entities, relations, conditions, rules, exceptions, and shapes for occupied-building entry.
5. **ASL-OT-05 — Immutable publication and exact resolution**: publish one versioned package and resolve it through existing domain contracts.
6. **ASL-OT-06 — Read-only adjudication**: implement Scenario A1 with evidence-backed explanations and non-definitive failure cases.

Each slice MUST build and test independently where code is introduced. Do not create generalized authoring ports in runtime Abstractions unless a concrete slice satisfies the interface admission rule.

## 18. Conformance tests

At minimum, the transformation suite MUST prove:

- source changes alter expected hashes and block publication;
- chapter-local rule numbers normalize without changing published identity;
- canonical identifiers and source references round-trip unchanged;
- headings, footnotes, tables, figures, and cross-references retain dependencies;
- hierarchy and reference errors are detected;
- exception targets and precedence require source support;
- examples cannot be promoted silently to normative rules;
- unmodeled or partial semantics cannot yield definitive evaluation;
- probabilistic proposals cannot become accepted without review;
- rejected and superseded artifacts remain auditable;
- the package manifest and every artifact hash reproduce;
- exact package resolution never floats versions;
- graph/vector projections can be rebuilt from the canonical package;
- tenant and package boundaries fail closed;
- Runtime has no ASL reference;
- `DomainConclusion` has no execution authority; and
- Scenario A1 returns the reviewed dispositions and evidence for its acceptance cases.

## 19. Implementation admission gate

This specification was accepted on 2026-09-23. ASL-OT-01 is implemented and reviewed in the [ASL-OT-01 Source Registry Review](<./LimboDancer.Agentic.CognitiveRuntime ASL-OT-01 Source Registry Review.md>), and ASL-OT-02 is implemented and reviewed in the [ASL-OT-02 TIR Review](<./LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Review.md>). The accepted [ASL-OT-03 Validation and Review Workflow Design](<./LimboDancer.Agentic.CognitiveRuntime ASL-OT-03 Validation and Review Workflow Design.md>) governs the current implementation gate; ASL-OT-03.1–03.3 are implemented, while 03.4 supports captured evidence, exact curated proposals, non-accepting review transitions, and blocked acceptance-readiness assessment. The [draft ASL-OT-03.5 Conformance and ASL-OT-04 Admission Review](<./LimboDancer.Agentic.CognitiveRuntime ASL-OT-03.5 Conformance and ASL-OT-04 Admission Review.md>) records the unverified Scenario A1 source candidates and open admission prerequisites. Later slices require the preceding slice's artifacts and tests.

Before ASL-OT-04 begins, reviewers MUST approve:

- the TIR schema version;
- the canonical identity and locator rules;
- the formalization-status semantics;
- validation and review roles;
- the package manifest profile; and
- the exact verified source subset required by Scenario A1.

No slice may claim complete ASL rule coverage, provider fitness, or governed state mutation. Those claims require their own evidence and review checkpoints.
