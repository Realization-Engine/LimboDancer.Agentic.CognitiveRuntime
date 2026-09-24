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
| `asl-scenario-a1.source-attestation.json` | Records the source provider's conversation attestation of the PDF digest and all 11 initial fragment IDs, without independently authenticating a named actor. |
| `asl-scenario-a1.source-verification.json` | C# generated `Verified` source-fidelity records for the 11 attested subjects, bound to exact artifacts in the full TIR; no semantic review or dependency closure is asserted. |
| `asl-scenario-a1.first-case-pdf-comparison.json` | Tool-assisted PDF comparisons for eight additional first-case rule declarations and the two linked figure-reference fragments. All ten subjects remain `unverified` pending source-provider review. |
| `asl-scenario-a1.pending-comparison-records.json` | C# generated `Indeterminate` source-verification records for those ten comparison subjects, bound to the same full TIR digest as the first 11 verified subjects. Human attestation is pending. |
| `asl-scenario-a1.backmatter-chart-candidate.json` | Bounded page-698 B. Terrain Chart candidate: exact PDF, extraction, MF column and two building-row hashes; remains outside the initial source registry and unverified. |
| `Supplements/b-terrain-chart-building-entry.md` | Bounded page-698 transcription of two building entry rows, printed note and relevant legend; requires source review. |
| `asl-scenario-a1.supplementary-source-registry.json` | Separate C# registered supplement: ties the bounded transcription hash and PDF page evidence to the immutable original registry, with `registered-unverified-supplement` status. |
| `asl-scenario-a1.backmatter-chart-pdf-comparison.json` | Tool-assisted comparison of the bounded supplement against physical PDF page 698, including the Infantry column, both building rows, note placement and legend; human fidelity attestation remains pending. |
| `asl-scenario-a1.chart-review-decision.json` | User-delegated xUnit source-fidelity and first-case domain review decision bound to the separate supplement and PDF comparison hashes; reviewed chart removes the source-boundary blocker only. |
| `asl-scenario-a1.first-case-review-decision.json` | User-delegated xUnit decision over the remaining ten exact TIR subjects and bounded first-case semantics. Pins the previous attestation, comparison, chart decision and TIR digest; an executable test verifies the decision and refuses unknown inputs. |

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

Regenerate the separate Scenario A1 verification batch from the committed attestation:

```bash
dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli -- \
  --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 \
  --registry-output docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json \
  --verification-output docs/ASL/SourceRegistry/asl-3.10-a-e.verification-sample.json \
  --a1-attestation docs/ASL/SourceRegistry/asl-scenario-a1.source-attestation.json \
  --a1-verification-output docs/ASL/SourceRegistry/asl-scenario-a1.source-verification.json
```

CI regenerates the batch and compares it byte for byte with the committed copy.

Regeneration is deterministic for the same source bytes, converter, source commit, and locator implementation. A source or converter change alters the applicable hash and causes the committed-manifest conformance test to fail until the change is reviewed and intentionally registered.

The A1 inventory fails closed if the pinned Chapter A/B source digests, an expected rule declaration or the linked footnote differ. Its `conversionStartPage` and `conversionEndPage` reproduce Markdown markers, whereas `pdfStartPage` and `pdfEndPage` record the separately checked physical PDF location. A4.14 and A4.15 have conversion page 48 and PDF page 49. B23.922 consists of separate rule-text and continuation fragments on pages 140 and 141. B23.9221 also records its adjacent figure-reference dependency. Chapter A footnote 3 inherits conversion page 98 but occurs on PDF page 101. This structural inventory is not a verification record and does not establish rule meaning or dependency closure.

The PDF comparison file is review evidence, not an automatically accepted `TirSourceVerificationRecord`. It records matching alphanumeric sequences across the complete bounded paragraphs and identifies only line-layout hyphens among the remaining punctuation differences. The source provider attested fidelity for the initial 11 subjects in conversation; the C# builder validates the pinned PDF digest and fragment set, regenerates the full TIR, and creates exact per-fragment `Verified` records. Additional source subjects require their own attestation and verification. Neither the comparison nor these records establish semantic meaning.

The [Scenario A1 source review packet](<../LimboDancer.Agentic.CognitiveRuntime Scenario A1 Source Review Packet.md>) lists the 11 reviewed subjects and a narrowly scoped first case for dependency review.

The first-case comparison evidence pins eight more prose fragments and the images for A2.4 and B23.1. B23.1 inherits Markdown conversion page 134 even though its rule and figure appear on physical PDF page 135. The C# conformance test validates all ten fragment IDs, source hashes, line numbers, page markers and registered image hashes; the matching text and visual observations remain review evidence, not verified dispositions.

Regenerate the pending ten-record batch with the C# CLI, passing `--a1-attestation` and the additional options:

```bash
dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli -- \
  --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 \
  --registry-output docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json \
  --verification-output docs/ASL/SourceRegistry/asl-3.10-a-e.verification-sample.json \
  --a1-attestation docs/ASL/SourceRegistry/asl-scenario-a1.source-attestation.json \
  --a1-comparison docs/ASL/SourceRegistry/asl-scenario-a1.first-case-pdf-comparison.json \
  --a1-comparison-output docs/ASL/SourceRegistry/asl-scenario-a1.pending-comparison-records.json
```

The [back-matter boundary review](<../LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>) identifies the B. Terrain Chart on physical PDF page 698 and other A–E-related player aids outside the initial registered page range. They are candidates for explicitly reviewed source additions, not implicit members of this registry.

The supplementary chart registry is reproducible with the C# CLI using the existing `--source-commit`, `--registry-output` and `--verification-output` arguments plus `--a1-supplement-candidate docs/ASL/SourceRegistry/asl-scenario-a1.backmatter-chart-candidate.json` and `--a1-supplement-output docs/ASL/SourceRegistry/asl-scenario-a1.supplementary-source-registry.json`. CI checks the generated JSON against the committed file. The user-supplied original PDF is not stored in this repository, so byte and visual fidelity still require source review against that PDF.

## Verification boundary

`asl-scenario-a1.concealed-smc-overrun-transition.json` pins the A12.15 concealed-SMC reveal to the optional A4.15 Infantry OVR branch, with A4.14, A4.151, A4.152, B23.4 and the reviewed building-cost chart. Its affirmative xUnit review checks the exact source subjects and the controlling fact vocabulary. It is a source and semantic dependency record; the branch cases and immutable package are subsequent increments.

`unverified` in the earlier inventory and representative sample describes their state at generation time and is preserved as historical evidence. The separate verification batch records the later source-provider disposition for its 11 exact subjects. Any newly verified subject requires a review record covering the fragment and its required table, figure, or footnote dependencies.

Neither manifest is an ontology, published domain package, tactical policy, Decision corpus, or execution authority.

The authoring implementation is isolated under `src/ASL/`. The runtime projects under `src/LimboDancer/` do not reference it. `utils/pdf_to_markdown.py` remains an outside source-conversion utility and is intentionally not part of the C# authoring solution.
