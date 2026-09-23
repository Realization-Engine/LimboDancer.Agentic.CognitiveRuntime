# LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Schema and Deterministic Extraction Design

**Status:** In progress; schema and canonical C# foundation implemented
**Date:** 2026-09-23  
**Branch:** `decision-plane`  
**Implementation language:** C# on the repository's current .NET baseline  
**Governing authority:** ASL Ontology Transformation Specification, Domain Integration Model, and ASL-OT-01 Source Registry Review

## 1. Purpose

This document preserves the design boundary for `ASL-OT-02`: the Transformation Intermediate Representation (`TIR`) schema and deterministic extraction of ASL rulebook structure.

ASL-OT-02 is the bridge between locating exact source evidence and proposing reviewed ontology semantics. Its governing rule is:

> The TIR records what can be established mechanically about source structure, identity, boundaries, and explicit references. It does not turn prose into executable semantics.

This slice defines a C# authoring implementation. It does not add ASL vocabulary or authoring responsibilities to `LimboDancer.Abstractions` or `LimboDancer.Runtime`, publish an ontology package, or create runtime authority.

## 2. Architectural position

The transformation layers remain distinct:

| Layer | What it knows | Authority |
| --- | --- | --- |
| Source registry | Which files and images constitute the pinned source edition | Source identity only |
| Source fragment | Exact source bytes, location, page, heading path, and dependencies | Evidence only |
| TIR artifact | Structural role, published identity, hierarchy, boundaries, and explicit references | Authoring proposal |
| Curated semantic artifact | Reviewed entities, conditions, effects, exceptions, and relations | Accepted only after review |
| Published package | Immutable accepted semantic artifacts | Runtime-resolvable domain authority |

For example, a fragment may establish that exact lines occur on a page, carry local published identifier `1.13`, appear in Chapter B, and mention another rule or figure. A TIR artifact may then record that the source structurally presents a Rule, preserve `1.13`, retain separate normalized identifier `B1.13`, attach ordered fragments, identify a supported parent candidate, and capture explicit reference occurrences.

The TIR may not yet claim the formal condition, effect, exception status, precedence, or Scenario A1 consequence of that wording.

```text
registered sources
-> immutable fragments
-> deterministic structural extraction
-> captured TIR artifacts
-> ASL-OT-03 validation and review
-> ASL-OT-04 semantic curation
```

Without this boundary, Markdown extraction, semantic interpretation, and acceptance would be collapsed into one unauditable operation.

## 3. C# implementation decision

The repository's implementation language is C#. ASL-OT-02 MUST therefore be implemented as C#/.NET authoring code and tests.

The former Python package under `utils/asl_ot` was used to establish the first ASL-OT-01 source-registry and fragment-locator evidence. Its behavior was ported to the isolated `src/ASL/` C# authoring solution, with committed-manifest and representative-case parity tests. The committed JSON manifests and approved review remain valid, language-neutral artifacts.

ASL-OT-02 MUST NOT introduce a production or CI dependency on Python. The C# implementation of the required ASL-OT-01 registry generation and fragment-location behavior is the normative baseline. `utils/pdf_to_markdown.py` remains an outside conversion utility and is not an implementation precedent for authoring or runtime code.

The authoring implementation belongs outside the runtime kernel. The established boundary is the separate `src/ASL/LimboDancer.Domains.Asl.sln`, containing the ASL-owned `LimboDancer.Domains.Asl.Authoring` library, its focused test project, and the `LimboDancer.Domains.Asl.Authoring.Cli` operator entry point used to regenerate committed manifests. ASL-OT-02 extends these projects rather than adding ASL types to the runtime solution. The permanent dependency rules are:

1. `LimboDancer.Runtime` MUST NOT reference the ASL authoring project.
2. `LimboDancer.Abstractions` MUST NOT contain TIR or extraction types.
3. The ASL authoring project MAY use domain-neutral value concepts only where an already admitted contract is genuinely required.
4. Source parsing, TIR schema types, canonical serialization, diagnostics, and authoring provenance remain ASL-owned.
5. Removing the ASL authoring project MUST leave the runtime build and conformance suite intact.

The C# implementation should use `System.Text.Json`, `System.Security.Cryptography`, and other base-class-library facilities unless a dependency is justified and reviewed.

## 4. Inputs inherited from ASL-OT-01

ASL-OT-02 consumes the approved ASL-OT-01 evidence boundary:

- seven registered Markdown artifacts;
- 661 registered image artifacts;
- pinned source commit and converter hash;
- whole-file SHA-256 values and byte sizes;
- structural fragment kinds;
- source, line, page, and heading-path locators;
- exact fragment content hashes;
- preserved published identifiers and separate chapter-normalized identifiers;
- image dependencies and footnote markers; and
- explicit `unverified` source-verification status.

The C# implementation MUST reject a changed, incomplete, or incompatible registry rather than reinterpret it silently. It MUST preserve the original fragment identities and exact source hashes.

## 5. ASL-OT-02 extraction scope

The complete TIR schema will describe the artifact kinds required by the governing specification. Deterministic extraction will initially populate only fields supported by source structure.

### 5.1 Source evidence

Each registered fragment is represented by or referenced through a `SourceFragment` TIR artifact. Generated TIR files SHOULD NOT duplicate the complete rulebook text. Ordered fragment identities, locators, and hashes must make the exact wording recoverable from the registered source.

### 5.2 Structural Rule artifacts

For each mechanically recognized rule identifier, the extractor creates a structural `Rule` artifact containing:

- stable authoring artifact identity;
- exact published identifier;
- separate chapter-normalized identifier;
- ordered source-fragment references;
- page and heading context;
- direct-parent candidate and its evidence;
- sibling order where supported;
- explicit dependencies; and
- captured extraction provenance and diagnostics.

Calling the artifact a `Rule` means that the source structurally presents it as a rule. It does not mean that the rule's semantics have been formalized.

An extracted Rule begins with:

```json
{
  "origin": "extracted",
  "formalizationStatus": "unmodeled",
  "reviewStatus": "captured",
  "semanticId": null
}
```

ASL-OT-02 MUST NOT advance an artifact to `proposed`, `in-review`, `validated`, or `accepted`.

### 5.3 Hierarchy

Hierarchy may be derived only from documented structural evidence, including:

- published identifier structure;
- chapter context;
- heading path; and
- source ordering.

Each hierarchy relationship records its basis. When these signals disagree, the extractor records an unresolved diagnostic rather than guessing.

The output distinguishes:

- supported direct parent;
- parent candidate requiring review;
- missing parent;
- duplicate normalized identifier;
- ambiguous hierarchy; and
- illegal cycle.

ASL-OT-02 detects and reports structural defects. ASL-OT-03 defines the review and acceptance workflow that resolves them.

### 5.4 Cross-reference occurrences

The extractor captures explicit reference occurrences with:

- exact reference text;
- containing fragment and line location;
- chapter context;
- normalized target candidate;
- resolution status: `resolved`, `ambiguous`, `missing`, or `unresolved`;
- resolved target artifact identity when unique; and
- figure, table, or footnote dependencies.

The extractor MUST NOT infer implicit references such as pronouns, “the preceding rule,” or a semantic relationship absent from the source. A structural reference may resolve to `B1.13` without asserting that the relationship is an exception, prerequisite, clarification, or precedence rule.

### 5.5 Structured source boundaries

The extractor recognizes boundaries already supported by source evidence:

- headings;
- rule text;
- rule continuations;
- explicit examples;
- table or chart blocks;
- figure references;
- footnote-bearing material; and
- ordinary paragraphs.

A `Table` artifact at this stage may identify its source boundary, ordered fragments, notes, and image dependencies. It MUST NOT claim that headers, axes, cells, or continuation structure are authoritative until reconstruction and review have occurred.

Examples remain distinguishable from normative rules and cannot create general semantics.

## 6. TIR document and artifact profile

The canonical TIR document should contain:

- TIR schema identifier and version;
- candidate package identity;
- source-registry identity and digest;
- source commit;
- extractor identity, assembly version, and configuration digest;
- canonical serialization profile;
- deterministically ordered artifacts;
- extraction diagnostics summary; and
- document digest calculated over the canonical payload.

Every artifact carries the common envelope required by the transformation specification:

| Field | ASL-OT-02 interpretation |
| --- | --- |
| `artifactId` | Deterministic authoring identity, stable for unchanged structural evidence. |
| `artifactKind` | One declared TIR artifact kind. |
| `packageCandidate` | Candidate identity only; never a published package reference. |
| `publishedId` | Exact identifier as presented by the source. |
| `normalizedPublishedId` | Separate chapter-aware identifier used for deterministic resolution. |
| `semanticId` | `null` unless a later curated step assigns one. |
| `sourceFragments` | Ordered fragment identities, locators, and hashes. |
| `dependencies` | Typed source or artifact dependencies supported by evidence. |
| `origin` | `extracted` for deterministic output. |
| `formalizationStatus` | `unmodeled` for structural extraction. |
| `reviewStatus` | `captured` for structural extraction. |
| `confidence` | Structural extraction signal only; never semantic authority. |
| `confidenceBasis` | Machine-readable reason for the structural confidence value. |
| `createdBy` | Extractor assembly, version, configuration, and source revision. |
| `createdAt` | Reproducible provenance value supplied under section 7.3. |
| `reviewRecordRefs` | Empty during ASL-OT-02. |

Artifact-kind payloads are discriminated C# records or classes. The canonical JSON representation is the interchange and review artifact; C# type names are not semantic identifiers and MUST NOT leak into published identity.

## 7. Determinism, identity, and canonical serialization

### 7.1 Artifact identity

`artifactId` is derived deterministically from:

- artifact kind;
- registered source identity;
- published or normalized structural identity when present;
- ordered source-fragment identities; and
- a deterministic disambiguator when several artifacts share a boundary.

It MUST NOT depend on enumeration order, database keys, wall-clock time, random values, C# type names, or mutable display text.

### 7.2 Canonical JSON

The implementation MUST define one canonical UTF-8 JSON profile with:

- fixed property order;
- fixed artifact and dependency ordering;
- ordinal string comparison;
- invariant formatting;
- explicit null policy;
- LF line endings; and
- no platform-dependent values.

The implementation should use an explicit canonical writer rather than assume the default `System.Text.Json` object traversal is a hashing contract. SHA-256 digests are calculated over canonical UTF-8 bytes.

Given identical registry, sources, extractor version, schema version, configuration, and reproducible build metadata, the extractor produces identical artifact identities, hierarchy, reference states, ordering, canonical bytes, diagnostics, and digest.

### 7.3 Time metadata

A wall-clock timestamp would make identical extraction output differ. `createdAt` MUST therefore be supplied as reproducible build metadata and excluded from identity derivation. The extraction run records the supplied timestamp and its source. CI regeneration uses the committed build value or another explicitly fixed value.

Volatile operator-run timestamps belong in a non-canonical run report, not in the hashed TIR payload.

### 7.4 Confidence

Confidence reports structural extraction quality only. It should be accompanied by a finite, declared basis such as:

- exact published-identifier match;
- chapter-local identifier normalized from registered context;
- heading-supported boundary;
- reference-pattern candidate;
- ambiguous structural evidence; or
- manual structural correction.

High extraction confidence does not raise `formalizationStatus` and does not establish semantic correctness.

## 8. Diagnostics and fail-closed behavior

The extractor emits a deterministic diagnostics report that includes at least:

- duplicate identifiers;
- malformed identifiers;
- missing, ambiguous, or conflicting parents;
- hierarchy cycles;
- unresolved, missing, and ambiguous references;
- discontinuous or cross-page Rule boundaries;
- table and figure dependencies;
- footnote-bearing fragments;
- unclassified structural material;
- registry or fragment hash incompatibility; and
- canonical serialization or identity collisions.

Declining to classify is a correct result. The extractor MUST fail closed when source identity, hashes, schema version, required dependencies, or canonical identity are invalid. Ordinary unresolved semantics remain captured diagnostics and do not become fabricated values.

## 9. Explicit exclusions

ASL-OT-02 does not:

- extract formal conditions or effects from prose;
- determine that prose is legally controlling;
- infer exception status or precedence;
- promote examples into normative rules;
- treat an unreviewed table reconstruction as authoritative;
- correct or normalize source wording silently;
- assign accepted semantic identities;
- publish an ontology or domain package;
- create a `DomainConclusion`;
- add ASL dependencies to the runtime kernel;
- introduce graph, vector, LLM, or classifier authority;
- implement provider evaluation; or
- grant execution authority.

A probabilistic system may later propose annotations, but proposals remain distinguishable, reproducible evidence and require the ASL-OT-03 review path.

## 10. Expected implementation deliverables

ASL-OT-02 should deliver:

1. versioned TIR JSON Schema;
2. ASL-owned C# TIR envelope and discriminated artifact types;
3. use of the established C# source-registry and fragment-locator implementation;
4. deterministic C# structural extractor;
5. canonical JSON writer and digest calculator;
6. complete metadata-focused Chapters A-E TIR output;
7. deterministic extraction diagnostics report;
8. representative verification sample;
9. unit and conformance tests;
10. `ASL-OT-02 TIR Review` document; and
11. .NET CI coverage for the authoring project.

The C# ASL Authoring CI is already normative for ASL-OT-01 behavior. ASL-OT-02 extends that path with extraction and reproducibility tests; it does not restore a Python authoring dependency.

## 11. Test requirements

The C# test suite must prove:

- committed ASL-OT-01 registry and representative fragments deserialize without loss;
- C# fragment identity and locator behavior match the approved cases;
- changed or missing source bytes fail closed;
- chapter-local identifiers normalize without overwriting published identity;
- repeated extraction produces identical canonical bytes and hashes;
- filesystem and collection enumeration order do not affect output;
- Rule boundaries retain ordered fragments and dependencies;
- supported hierarchy is reproducible;
- missing parents, duplicates, and cycles are diagnosed;
- references resolve only when structurally unique;
- ambiguous and missing references remain explicit;
- figures, tables, footnotes, and examples retain their structural distinctions;
- extracted artifacts remain `unmodeled` and `captured`;
- no automatic Condition, Effect, Exception, or accepted semantic artifact is fabricated;
- no TIR or ASL authoring dependency enters `LimboDancer.Abstractions` or `LimboDancer.Runtime`; and
- generated artifacts reproduce on the repository's supported CI environment.

## 12. Completion and review boundary

ASL-OT-02 is complete when the repository can truthfully state:

> Given the pinned ASL 3.10 Chapters A-E sources, the C# authoring implementation reproducibly describes structural elements, hierarchy, explicit identifiers, reference occurrences, and source boundaries while preserving exact provenance and marking all unformalized meaning honestly.

ASL-OT-02 is not complete merely because the extractor can answer ASL questions or generate plausible ontology objects. It does not answer ASL rules questions.

ASL-OT-03 follows by defining identity, structure, provenance, formalization, review-state, and human-adjudication gates. ASL-OT-04 then curates the minimum accepted semantic artifacts for Scenario A1. No later slice begins automatically when this implementation completes.

## 13. Admission checkpoint

Before implementation begins, the ASL-OT-02 change should confirm:

1. C# project and solution placement outside the runtime kernel;
2. TIR schema version and namespace;
3. artifact identity derivation;
4. canonical JSON profile;
5. reproducible `createdAt` handling;
6. structural confidence representation;
7. compatibility with the established C# ASL-OT-01 behavior; and
8. exact generated artifacts committed for review.

These are authoring implementation decisions. They do not admit runtime contracts, persistence products, ontology engines, or publication infrastructure.

## 14. Implemented foundation

The first ASL-OT-02 implementation slice establishes the following admitted foundation:

- `docs/ASL/Schemas/asl-tir-1.0.schema.json` defines schema identity `urn:limbodancer:asl:tir:schema:1.0.0`, the complete required artifact-kind vocabulary, the common envelope, and kind-specific structural payloads;
- `TirModels.cs` defines the ASL-owned C# document, envelope, provenance, dependency, diagnostic, and structural payload types;
- strongly typed structural artifacts exist for `SourceFragment`, `Rule`, `CrossReference`, `Example`, and `Table`;
- the remaining required artifact kinds are reserved in the versioned vocabulary but cannot be emitted by the structural canonical writer;
- `TirArtifactIdentity` derives stable identities from kind, source-registry identity, published structural identity, ordered source-fragment identities, and an explicit deterministic disambiguator;
- `TirCanonicalJson` fixes property order, ordinal set ordering, explicit nulls, invariant UTC formatting, and SHA-256 calculation over the canonical payload excluding the digest field itself; and
- the writer fails closed if an ASL-OT-02 artifact claims a semantic identity or advances beyond `extracted`, `unmodeled`, and `captured`.

The canonical writer deliberately preserves source-fragment and table-note order because those sequences carry source meaning. It sorts set-like collections such as artifacts, dependencies, confidence bases, review references, and diagnostics by declared ordinal keys.

This foundation does not implement document extraction. The next implementation slice maps the registered ASL-OT-01 fragments into `SourceFragment` and structural `Rule` artifacts, emits hierarchy candidates and diagnostics, and commits a representative generated TIR artifact for review.
