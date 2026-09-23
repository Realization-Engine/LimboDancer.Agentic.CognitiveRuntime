# ASL-OT-02 structural TIR samples

This directory contains deterministic review artifacts produced by the C# ASL authoring solution.

## Current artifact

`asl-3.10-a-e.structural-sample.tir.json` is a metadata-only representative sample extracted from the registered ASL 3.10 Chapters A-E source set. It contains selected `SourceFragment` and structural `Rule` artifacts, their ordered source evidence, hierarchy results, dependencies, diagnostics, provenance, and canonical payload digest.

The sample does not duplicate rulebook prose. It preserves source hashes and locators that recover the applicable wording from the controlled source tree.

Every artifact remains:

- `origin: extracted`;
- `formalizationStatus: unmodeled`;
- `reviewStatus: captured`; and
- `semanticId: null`.

The sample is not a reviewed ontology, published domain package, tactical Decision corpus, or execution authority. Parent resolution describes published-number structure only. It does not assert semantic scope, applicability, exception precedence, or legal interpretation.

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
