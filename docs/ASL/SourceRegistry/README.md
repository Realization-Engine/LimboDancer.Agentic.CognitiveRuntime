# ASL 3.01 Source Registry

This directory contains the committed ASL-OT-01 provenance outputs for the ASL 3.01 Chapters A-E authoring source.

The user identified the delivery ZIP as version 3.01, matching the converted image filename stem `eASLRB_v3_01`. The PDF credits still read “Version 3.0; June 2025.” The original `3.10` edition designation was erroneous. Existing `3.10` spellings in registry IDs, source IDs, candidate IDs and filenames are immutable historical identifiers, not the edition value; changing them would re-key fragment evidence and downstream citations. The declared edition is `3.01`. The supplied PDF is identified by SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247` and is not copied into this repository.

The authoring scope is **only the TOC, Index/Glossary, and Chapters A–E**: seven registered Markdown files and their registered images from physical PDF pages 6–253. Other sections of the 716-page PDF are outside the transformation and verification scope. Cross-references to excluded material may be recorded as unresolved dependencies; they do not expand the source baseline implicitly.

## Artifacts

| File | Purpose |
| --- | --- |
| `asl-3.10-a-e.source-registry.json` | Pins the seven Markdown sources, 661 image dependencies, source commit, page ranges, file sizes, SHA-256 hashes, conversion-tool hash, and distribution controls. |
| `asl-3.10-a-e.verification-sample.json` | Fourteen representative fragment locators covering structural kinds, Chapters A-E, chapter-local identifiers, a footnote marker, and figure dependency. Every entry remains `unverified`. |
| `asl-scenario-a1.candidate-source-inventory.json` | Eight candidate rule identifiers, ten exact rule-associated fragments (including a page continuation and a figure reference), plus the linked Chapter A footnote 3 fragment. Its status is `candidate-unverified`. |
| `asl-scenario-a1.pdf-comparison.json` | Tool-assisted comparison of nine A1 prose fragments, the linked Chapter A footnote 3, and the registered Breach figure against the supplied PDF. Preserves layout differences and `unverified` status pending independent source review. |

The manifests contain locators and hashes, not duplicated rule text. The Markdown and image files under `../Rulebook_Markdown/` remain the registered content.

## Regeneration

From the repository root:

```bash
dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli -- \
  --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 \
  --registry-output docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json \
  --verification-output docs/ASL/SourceRegistry/asl-3.10-a-e.verification-sample.json \
  --tir-output docs/ASL/TIR/asl-3.10-a-e.structural-sample.tir.json \
  --a1-output docs/ASL/SourceRegistry/asl-scenario-a1.candidate-source-inventory.json \
  --tir-created-at 2026-09-23T12:00:00Z

dotnet test src/ASL/LimboDancer.Domains.Asl.sln
```

Regeneration is deterministic for the same source bytes, converter, source commit, and locator implementation. A source or converter change alters the applicable hash and causes the committed-manifest conformance test to fail until the change is reviewed and intentionally registered.

The A1 inventory fails closed if the pinned Chapter A/B source digests, an expected rule declaration or the linked footnote differ. Its `conversionStartPage` and `conversionEndPage` reproduce Markdown markers, whereas `pdfStartPage` and `pdfEndPage` record the separately checked physical PDF location. A4.14 and A4.15 have conversion page 48 and PDF page 49. B23.922 consists of separate rule-text and continuation fragments on pages 140 and 141. B23.9221 also records its adjacent figure-reference dependency. Chapter A footnote 3 inherits conversion page 98 but occurs on PDF page 101. This structural inventory is not a verification record and does not establish rule meaning or dependency closure.

The PDF comparison file is review evidence, not an automatically accepted `TirSourceVerificationRecord`. It records matching alphanumeric sequences across the complete bounded paragraphs and identifies only line-layout hyphens among the remaining punctuation differences. A source verifier must inspect the cited PDF pages, confirm the footnote and figure dependencies, and create the formal per-fragment review records for the relevant TIR artifacts before marking a fragment `verified`.

The [Scenario A1 source review packet](<../LimboDancer.Agentic.CognitiveRuntime Scenario A1 Source Review Packet.md>) lists the 11 review subjects, formal record handoff and a narrowly scoped first case for dependency review.

## Verification boundary

`unverified` means the fragment has been located and hashed but has not been compared with the authoritative ASL 3.01 edition. Moving a sample to `verified` requires a human review record covering the fragment and every required table, figure, or footnote dependency.

Neither manifest is an ontology, published domain package, tactical policy, Decision corpus, or execution authority.

The authoring implementation is isolated under `src/ASL/`. The runtime projects under `src/LimboDancer/` do not reference it. `utils/pdf_to_markdown.py` remains an outside source-conversion utility and is intentionally not part of the C# authoring solution.
