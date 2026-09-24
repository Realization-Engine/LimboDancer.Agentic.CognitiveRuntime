# LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Review

**Status:** Approved

**Review date:** 2026-09-23

**Reviewed branch:** `decision-plane`

**Reviewed implementation:** TIR schema 1.3.0 and structural extractor 1.5.0

**Scope:** ASL-OT-02 Transformation Intermediate Representation and deterministic structural extraction

## 1. Review decision

ASL-OT-02 conforms to the accepted ASL Ontology Transformation Specification and satisfies its completion boundary. Given the pinned ASL 3.01 Chapters A-E source set, the C# authoring implementation reproducibly describes source fragments, structural Sections and Rules, hierarchy, explicit identifiers, explicit reference occurrences, example markers, table/chart blocks, and provenance without claiming semantic completeness or authority.

ASL-OT-03 may proceed to define validation, review-state transitions, and human-adjudication gates. This approval does not validate any ASL rule interpretation, accept any semantic artifact, publish a runtime package, or authorize execution.

## 2. Reviewed baseline

| Item | Reviewed value |
| --- | --- |
| Registered source edition | ASL 3.01, Chapters A-E |
| Pinned source commit | `a3254ff1d492dbdd28483d86f5b42437b48e80d4` |
| TIR schema | `urn:limbodancer:asl:tir:schema:1.3.0` |
| Extractor | `LimboDancer.Domains.Asl.Authoring.StructuralExtractor` 1.5.0 |
| Implementation | C# under `src/ASL/` |
| Canonical sample | `docs/ASL/TIR/asl-3.10-a-e.structural-sample.tir.json` |
| CI baseline | .NET 10, warnings treated as errors |
| Reviewed CI run | [ASL Authoring CI 35913082374](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/35913082374) |
| Review head | `b0293728344f8d3c66142dbcb346f2cdafba9e62` |

The canonical representative sample records extractor configuration SHA-256 `22f154ec579965639925734ef73338b547da38af4a3b4349e8bb45e769a18fd1` and document SHA-256 `7c537fb92bc793212c3a9d8d415ed857dd7c20bfffb64fdf6dc7203f997921d3`.

## 3. Deliverable assessment

| Required deliverable | Decision | Evidence |
| --- | --- | --- |
| Versioned TIR JSON Schema | Pass | `docs/ASL/Schemas/asl-tir-1.3.schema.json` defines the document, artifact vocabulary, authority states, diagnostics, and nullable UTF-8 sub-fragment spans. |
| ASL-owned C# TIR types | Pass | `TirModels.cs` defines the envelope and discriminated structural artifact records outside the runtime kernel. |
| ASL-OT-01 input parity | Pass | The extractor consumes the approved C# registry and fragment locator; committed registry and verification-sample parity remain tested. |
| Deterministic structural extractor | Pass | `TirStructuralExtractor.cs` maps the complete registered fragment stream through versioned, configuration-hashed rules. |
| Canonical JSON and digest | Pass | `TirCanonicalJson.cs` normalizes unordered collections, preserves ordered evidence, validates authority and span invariants, and calculates the canonical document digest. |
| Complete Chapters A-E structural output | Pass | The CLI emits the complete metadata-focused output from the pinned corpus. CI executes the complete extraction before selecting the committed representative sample. |
| Deterministic diagnostics | Pass | Diagnostics are part of the canonical document; the CLI reports corpus counts and missing-parent totals without suppressing unresolved findings. |
| Representative review artifact | Pass | The committed metadata-only sample contains all six currently extracted artifact kinds and is compared byte-for-byte with C# regeneration. |
| Unit and conformance tests | Pass | Thirty tests cover the registry, fragment locator, TIR foundation, extraction behavior, canonical sample parity, and architecture boundary. |
| ASL-OT-02 review | Pass | This document records the completion decision, accepted interpretations, residual diagnostics, and deferrals. |
| .NET CI coverage | Pass | `ASL Authoring CI` restores, builds with warnings as errors, and runs the complete authoring test project. |

The complete generated TIR is an on-demand derived artifact. The repository commits the schema, generator, exact representative sample, and regeneration command rather than a large second copy of all derived corpus metadata. This artifact policy is accepted because CI regenerates the complete corpus and the committed sample detects canonical-format, identity, and selection drift.

## 4. Corpus result

The approved extractor produces the following complete-corpus structural inventory:

| Result | Count |
| --- | ---: |
| Sections | 105 |
| Rules | 2,001 |
| Cross-reference occurrences | 4,834 |
| Example markers | 358 |
| Table/chart blocks | 21 |
| Missing-parent diagnostics | 0 |
| Missing-reference diagnostics | 32 |
| Total diagnostics | 32 |

The final hierarchy recovery admits exactly three damaged source boundaries: `A7.37`, `A7.8`, and `E1.93`. Each is required by an already-extracted child, supported by an explicit marker, and cited through an exact UTF-8 byte span. The recovery resolves all eight former missing-parent findings and fourteen formerly missing reference targets.

The 32 remaining diagnostics are retained findings, not hidden failures. They identify explicit chapter-qualified references whose target does not resolve uniquely within the registered Chapters A-E structural Rule set. Their presence does not invalidate deterministic extraction. ASL-OT-03 must preserve, classify, and adjudicate them before any dependent semantic artifact can advance toward acceptance.

## 5. Completion-boundary assessment

The repository can truthfully make the ASL-OT-02 completion statement because:

1. source identities, hashes, locators, and ordered fragment evidence remain recoverable;
2. published and normalized identifiers are distinct;
3. hierarchy is derived only from documented identifier, chapter, heading, source-order, and zero-padded-child conventions;
4. damaged boundaries are admitted only through required-parent-gated explicit markers;
5. exact sub-fragment evidence uses language-independent UTF-8 byte offsets against immutable fragment content;
6. reference resolution requires a unique structural target;
7. incomplete or ambiguous structure produces diagnostics rather than guessed semantics;
8. every extracted artifact remains `unmodeled` and `captured`, with no semantic identity;
9. canonical output and artifact identities reproduce under CI; and
10. no ASL authoring dependency enters `LimboDancer.Abstractions` or `LimboDancer.Runtime`.

Hierarchy cycles cannot be produced by the current extraction grammar: a Rule parent is either a root Section or a strictly shorter identifier candidate, including the documented zero-padded fallback. Ambiguous candidates do not create a direct-parent edge. Cycle validation becomes an active diagnostic requirement in ASL-OT-03 when reviewed relationships can be proposed independently of this strictly decreasing structural derivation.

## 6. Source-boundary decisions

The review accepts the following evidence interpretations:

- a null/null sub-fragment span means that the complete immutable fragment is evidence;
- a numeric span is inclusive at `startUtf8ByteOffset` and exclusive at `endUtf8ByteOffsetExclusive`;
- a `CrossReference` span covers the exact chapter-qualified token;
- an `Example` span covers the exact `EX:` marker, not the complete example prose;
- a recovered structured-text Section or damaged Rule span covers only its explicit declaration marker;
- an unverified `Table` identifies an explicitly labelled fenced block but does not assert headers, axes, rows, cells, or continuation structure; and
- whole-fragment Rule evidence may contain source-conversion damage without authorizing the extractor to repair its meaning.

These decisions are sufficient for honest structural extraction. Complete example extents and table-row reconstruction are not ASL-OT-02 completion requirements because the current artifacts do not claim that information.

## 7. Reproducibility and test result

The reviewed GitHub Actions run completed successfully with:

- zero build warnings;
- zero build errors;
- 30 passing tests;
- zero failed tests; and
- zero skipped tests.

The conformance suite proves canonical collection ordering, ordered source evidence, artifact-identity sensitivity, fail-closed semantic authority, span validation, hierarchy conventions, missing and ambiguous parent behavior, duplicate identity diagnostics, exact reference resolution, explicit example/table markers, required embedded-boundary recovery, sample parity, runtime isolation, and the C# repository-language boundary.

CI is the supported execution environment. No production or CI dependency on Python was introduced. `utils/pdf_to_markdown.py` remains an unchanged outside conversion utility.

## 8. Explicit exclusions and deferrals

ASL-OT-02 does not provide or approve:

- verified source wording or visual comparison against the authoritative edition;
- semantic entities, definitions, conditions, effects, exceptions, precedence, or applicability;
- complete example-prose boundaries;
- reconstructed table/chart headers, rows, axes, cells, or continuation relationships;
- resolution of the 32 remaining missing structural reference targets;
- reviewed semantic identifiers or review-state promotion;
- immutable ASL package publication or exact runtime package resolution;
- state-provider observations, registered calculations, or a `DomainConclusion`;
- tactical labels, provider evidence, or model evaluation; or
- execution authority.

These exclusions are preserved inputs to later slices, not implied implementation gaps in the approved structural boundary.

## 9. ASL-OT-03 entry conditions

ASL-OT-03 may now define the validation and review workflow subject to these conditions:

1. it consumes an exact TIR schema version, extractor identity, configuration hash, source-registry digest, and document digest;
2. it never upgrades `captured` artifacts automatically because extraction confidence is high;
3. it separates structural validation, source verification, semantic proposal, and expert acceptance;
4. it retains unresolved diagnostics and prevents dependent artifacts from bypassing them;
5. it requires authoritative-edition comparison for rule-critical source fragments and visual dependencies;
6. it records reviewer identity, decision, rationale, and evidence without rewriting extracted provenance;
7. it diagnoses cycles in proposed or curated relationships that are no longer constrained by the structural identifier grammar; and
8. it does not publish a runtime-resolvable package; publication remains ASL-OT-05.

## 10. Next checkpoint

The next admitted slice is **ASL-OT-03: Validation and Review Workflow**. Its first deliverable is the [ASL-OT-03 Validation and Review Workflow Design](<./LimboDancer.Agentic.CognitiveRuntime ASL-OT-03 Validation and Review Workflow Design.md>), which proposes validation stages, review records, diagnostic disposition, source-verification gates, semantic proposal states, and the exact conditions under which an artifact may advance beyond `captured`.

ASL-OT-04 semantic curation does not begin automatically. It remains blocked until the ASL-OT-03 design and acceptance gate are approved.
