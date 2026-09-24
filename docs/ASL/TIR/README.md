# ASL-OT-02 structural TIR samples

This directory contains deterministic review artifacts produced by the C# ASL authoring solution.

ASL-OT-02 is approved by the [ASL-OT-02 TIR Review](<../LimboDancer.Agentic.CognitiveRuntime ASL-OT-02 TIR Review.md>). The review records the complete-corpus counts, residual diagnostics, accepted source-boundary interpretations, and the gate into ASL-OT-03.

## Current artifact

`asl-3.10-a-e.structural-sample.tir.json` is a metadata-only representative sample extracted from the registered ASL 3.01 Chapters A-E source set. It contains selected `SourceFragment`, `Section`, structural `Rule`, `CrossReference`, `Example`, and `Table` artifacts, their ordered source evidence, hierarchy and reference-resolution results, dependencies, diagnostics, provenance, and canonical payload digest.

The sample does not duplicate rulebook prose. It preserves source hashes and locators that recover the applicable wording from the controlled source tree. Each source-fragment reference also carries a nullable UTF-8 byte span. A null/null span denotes the complete immutable fragment; numeric `startUtf8ByteOffset` and exclusive `endUtf8ByteOffsetExclusive` values identify exact evidence inside that fragment.

Every artifact remains:

- `origin: extracted`;
- `formalizationStatus: unmodeled`;
- `reviewStatus: captured`; and
- `semanticId: null`.

The sample is not a reviewed ontology, published domain package, tactical Decision corpus, or execution authority. Parent and cross-reference resolution describe published-number structure only. A `Section` is a structural anchor extracted from one of three explicit major-section boundary forms: a numbered Markdown heading, a bold numbered declaration, or a numbered declaration in structured text. Its payload identifies the observed boundary kind and records a source heading level only when one actually exists. General rules such as `A.1` remain distinct from section identity `A1`. Within a section, ASL's digit hierarchy makes `A1.1` a child of Section `A1`, `A1.11` a child of Rule `A1.1`, and `A1.111` a child of Rule `A1.11`. Zero-padded child cases such as `A7.301` resolve to `A7.3`, and `A14.01` resolves to Section `A14`, only when the corresponding structural parent exists. Example artifacts record explicit `EX:` markers, and table artifacts record explicitly labelled fenced blocks with unverified structure. None of these artifacts asserts semantic scope, applicability, exception precedence, table-cell meaning, or legal interpretation.

Extractor 1.5 also recovers `A7.37`, `A7.8`, and `E1.93` as structural Rules because extracted children require those parent identifiers and each boundary has explicit sub-fragment evidence. The recovered artifacts cite only their exact declaration markers. They do not claim complete Rule text, and an unneeded lookalike marker is not promoted.

The committed TIR 1.3 sample contains 49 artifacts: 29 source fragments, five sections, 11 structural rules, two cross-references, one example, and one table. Cross-reference occurrences and `EX:` markers use exact sub-fragment spans; whole-fragment evidence remains explicitly unbounded. Its single diagnostic preserves an unresolved reference target selected for review.

## Regeneration

From the repository root:

```bash
dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli -- \
  --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 \
  --registry-output docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json \
  --verification-output docs/ASL/SourceRegistry/asl-3.10-a-e.verification-sample.json \
  --tir-output docs/ASL/TIR/asl-3.10-a-e.structural-sample.tir.json \
  --tir-created-at 2026-09-23T12:00:00Z

dotnet test src/ASL/LimboDancer.Domains.Asl.sln
```

The timestamp is explicit reproducible build metadata. It is serialized into the document but excluded from artifact identity derivation.
