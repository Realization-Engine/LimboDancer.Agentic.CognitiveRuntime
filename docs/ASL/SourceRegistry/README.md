# ASL 3.10 Source Registry

This directory contains the committed ASL-OT-01 provenance outputs for the ASL 3.10 Chapters A-E authoring source.

## Artifacts

| File | Purpose |
| --- | --- |
| `asl-3.10-a-e.source-registry.json` | Pins the seven Markdown sources, 661 image dependencies, source commit, page ranges, file sizes, SHA-256 hashes, conversion-tool hash, and distribution controls. |
| `asl-3.10-a-e.verification-sample.json` | Fourteen representative fragment locators covering structural kinds, Chapters A-E, chapter-local identifiers, a footnote marker, and figure dependency. Every entry remains `unverified`. |

The manifests contain locators and hashes, not duplicated rule text. The Markdown and image files under `../Rulebook_Markdown/` remain the registered content.

## Regeneration

From the repository root:

```bash
dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli -- \
  --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 \
  --registry-output docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json \
  --verification-output docs/ASL/SourceRegistry/asl-3.10-a-e.verification-sample.json

dotnet test src/ASL/LimboDancer.Domains.Asl.sln
```

Regeneration is deterministic for the same source bytes, converter, source commit, and locator implementation. A source or converter change alters the applicable hash and causes the committed-manifest conformance test to fail until the change is reviewed and intentionally registered.

## Verification boundary

`unverified` means the fragment has been located and hashed but has not been compared with the authoritative ASL 3.10 edition. Moving a sample to `verified` requires a human review record covering the fragment and every required table, figure, or footnote dependency.

Neither manifest is an ontology, published domain package, tactical policy, Decision corpus, or execution authority.

The authoring implementation is isolated under `src/ASL/`. The runtime projects under `src/LimboDancer/` do not reference it. `utils/pdf_to_markdown.py` remains an outside source-conversion utility and is intentionally not part of the C# authoring solution.
