# LimboDancer.Agentic.CognitiveRuntime ASL-OT-01 Source Registry Review

**Status:** Approved

**Review date:** 2026-09-23

**Reviewed branch:** `decision-plane`

**Scope:** ASL-OT-01 source registry and fragment locator

## 1. Review decision

ASL-OT-01 conforms to the accepted ASL Ontology Transformation Specification. The slice registers the ASL 3.01 Chapters A-E source set, produces deterministic structural fragment locators, commits a representative unverified review sample, and adds isolated authoring CI without introducing ontology semantics or runtime dependencies.

ASL-OT-02 may proceed under the accepted specification. Human comparison with the authoritative edition remains required before any rule-critical fragment can become verified or support an accepted semantic artifact.

## 2. Implementation evidence

| Requirement | Decision | Evidence |
| --- | --- | --- |
| Complete registered source set | Pass | The manifest contains seven Markdown artifacts and 661 image artifacts under `docs/ASL/Rulebook_Markdown/`. |
| Immutable source evidence | Pass | Every artifact records repository-relative path, byte size, and SHA-256; Markdown and image page ranges are retained where derivable. |
| Conversion provenance | Pass | The registry pins source commit `a3254ff1d492dbdd28483d86f5b42437b48e80d4` and the SHA-256 of `utils/pdf_to_markdown.py`. |
| Structural fragments | Pass | The locator distinguishes headings, rule text, rule continuations, figure references, structured text, and ordinary paragraphs. |
| Exact content evidence | Pass | Fragment hashes use the exact extracted content bytes represented by the located source span; manifests do not duplicate rule text. |
| Canonical identity preservation | Pass | Published identifiers remain unchanged while chapter-local identifiers receive a separate normalized form such as `1.13` -> `B1.13`. |
| Page and hierarchy context | Pass | Locators retain line ranges, page markers, and heading paths. |
| Dependency capture | Pass | Markdown figure references retain their relative image dependencies; inline footnote markers are flagged for review. |
| Fail-closed source changes | Pass | The registry builder rejects missing or unexpected Markdown sources, and tests compare the committed manifest with current bytes. |
| Reproducibility | Pass | Stable source bytes produce identical registry values and fragment identities. |
| Review honesty | Pass | All fourteen representative samples are explicitly `unverified`; no comparison with the authoritative edition is claimed. |
| Runtime isolation | Pass | Implementation is an ASL-owned C# authoring library and CLI in the separate `src/ASL/` solution; no project under `src/LimboDancer/` references it. |
| Repository language boundary | Pass | Repository-owned ASL authoring source and CI are C#. `utils/pdf_to_markdown.py` remains an unchanged outside conversion utility. |
| CI coverage | Pass | `ASL Authoring CI` restores, builds with warnings as errors, and runs the source-registry, fragment-locator, manifest-parity, and architecture tests when authoring sources, manifests, converter, or tooling change. |

## 3. Fragment interpretation

A fragment is an immutable, hash-addressed, precisely located source-evidence unit. It is not a semantic artifact or arbitrary search chunk. One rule may require multiple ordered fragments, and one fragment may support multiple later semantic proposals.

Fragment identity establishes provenance only. Semantic correctness remains the responsibility of ASL-OT-02/03 transformation, validation, and human review.

## 4. Explicit exclusions

ASL-OT-01 does not provide:

- ontology entities, properties, relations, conditions, effects, or exceptions;
- semantic completeness for Chapters A-E;
- authoritative correction of conversion artifacts;
- verified rule interpretations;
- runtime package publication or resolution;
- a `DomainConclusion` implementation;
- tactical Decision labels or provider evidence; or
- execution authority.

## 5. Test result

ASL-OT-01 coverage comprises eleven tests: the eight original source-registry and fragment-locator cases, a committed verification-sample parity test, and two architecture-boundary tests. Later ASL authoring slices extend the same conformance suite. CI is the supported execution environment and uses the repository's .NET 10 baseline.

## 6. Next checkpoint

ASL-OT-02 may define the Transformation Intermediate Representation schema and deterministic structural extraction over the registered fragments. It must preserve original fragments, represent partial formalization honestly, and avoid introducing semantic authority before ASL-OT-03 validation and review.
