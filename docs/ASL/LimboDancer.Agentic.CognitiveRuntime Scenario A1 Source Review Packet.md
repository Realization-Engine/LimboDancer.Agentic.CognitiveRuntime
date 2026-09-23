# Scenario A1 source review packet

**Status:** Ready for independent source review; no source-verification disposition approved  
**Edition:** 3.01 delivery ZIP as identified by the source provider; supplied PDF SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`  
**Source boundary:** TOC, Index/Glossary, Chapters A–E and their registered images; PDF physical pages 6–253 only  
**Registered source commit:** `a3254ff1d492dbdd28483d86f5b42437b48e80d4`

## Review subjects

The [C# candidate inventory](<./SourceRegistry/asl-scenario-a1.candidate-source-inventory.json>) gives exact source IDs, fragment IDs, content hashes, line spans, conversion page markers and separately checked PDF pages. The [comparison evidence](<./SourceRegistry/asl-scenario-a1.pdf-comparison.json>) records the nine complete prose comparisons, one linked footnote and one visual comparison. Neither document contains source-verification approval. There are **11 distinct review subjects**: nine rule prose fragments, one Chapter A footnote fragment, and one figure-reference fragment with a registered image dependency.

| Source subject | Physical PDF page | Review point |
| --- | --- | --- |
| A2.8 | 47 | Location versus hex. |
| A4.14 | 49 | Markdown marker says page 48. |
| A4.15 | 49 | Markdown marker says page 48; inspect its superscript footnote reference. |
| A4.7 | 52 | Advance phase; review applicability to any APh case separately. |
| A12.15 | 78 | Detection when attempted entry concerns a concealed occupant. |
| B23.4 | 136 | Building entry and level change. |
| B23.922, declaration | 140 | First half of the fortified entry restriction. |
| B23.922, continuation | 141 | Second half after a Markdown page marker; both fragments are needed. |
| B23.9221, rule prose | 141 | Breach exception and reference to B23.711. |
| Chapter A footnote 3, linked from A4.15 | 101 | Markdown still inherits page 98; reviewer determines whether the note carries any rule-critical meaning. |
| B23.9221 figure reference | 141 | Registered `images/eASLRB_v3_01-p141-1.png` SHA-256 `ce441cb728f2c000f18fb05c9f823ea158c636255bff1f1d23a723c217d9a6c6`; inspect the Breach counter against the PDF. |

The tool-assisted comparison found matching alphanumeric sequences for all ten prose subjects. Punctuation differs only by PDF line-layout hyphens in A12.15, B23.922, B23.9221, and footnote 3. The Breach image was visually matched to the rendered page; pixel identity is not asserted. This finding supports source review; it does not certify the conversion or establish rule meaning.

## Formal record handoff

The existing `TirSourceVerificationService.CreateRecord` accepts an exact TIR artifact and source fragment, checks registry and fragment hashes, and produces a `TirSourceVerificationRecord` with verifier identity, comparison method and disposition. The current committed TIR **representative sample** contains none of these A1 artifacts. Before creating a record, regenerate the **full** TIR from the pinned registry and source bytes and identify its exact source-fragment artifact for each of the 11 subjects. Bind each record to the full TIR document digest and artifact digest. Do not create a placeholder `Verified` record or claim an independent reviewer acted when they have not.

For each subject the independent source verifier should:

1. Confirm the PDF checksum and the relevant page, the complete Markdown fragment and its hash, and any linked figure/footnote or page continuation. Keep PDF physical pages distinct from conversion markers.
2. Record their own identity, review time, comparison method, artifact reference, disposition and observed discrepancy using the C# review contract. The comparison report is evidence to inspect, not the review decision.
3. Use `Verified` only for a fully checked source fragment and required dependencies. Use `Mismatch` or `Indeterminate` with an explanation when the conversion, visual evidence or source boundary cannot be established. The PDF page corrections are provenance and should appear in the record's method/discrepancy context, since the current `TirSourceEvidenceContext` carries the Markdown locator page.
4. Preserve all 11 independently reviewable subjects and link the A4.15 and B23.9221 records to their footnote and image checks. A registered image hash establishes its bytes, not the accuracy of its visual transcription.

The source-verification records address **source fidelity only**. They cannot approve a rule interpretation, dependency closure, candidate package, or tactical outcome.

## First case for dependency review

Use a synthetic *candidate* case: a known Good Order, unpinned Infantry squad attempts an MPh ground-level entry into an adjacent ordinary building Location; the destination has no enemy, concealed or hidden occupants, no fortification, no road entry, no Bypass, no elevation change, no additional terrain, no SMOKE, no scenario special rule, enough remaining MF, and stacking below the normal limit. These are declared input facts for a bounded review, not facts inferred from an incomplete board observation or a validated outcome.

| Dependency family | In-scope source to examine | Current review state |
| --- | --- | --- |
| Location and terrain | A2.8, A2.4, B23.1, B23.4 | A2.8 and B23.4 compared with PDF; A2.4 and B23.1 require source comparison and semantic review. |
| Phase, capability and cost | A3.3, A4.11, A4.13 and any controlling movement limits | Additional source and domain review required; sufficient MF alone does not prove eligibility. |
| Occupancy and stacking | A4.14, A5.1, A5.11 | A4.14 compared with PDF; stacking and classification still require review. |
| Excluded branches | A4.132 (road), A4.134 (Minimum Move), A4.15 (OVR), A4.7 (APh), A12.15 (concealed occupancy), B23.922/23.9221 (fortified entry/breach) | Compare applicability and explicit exclusion assumptions; unknown occupant or terrain status must abstain. |

This is an **initial dependency inventory**, not a closed rule graph or a positive legality label. The independent ASL domain reviewer must examine cross-references and exceptions under the declared facts, add any missing rules, and either approve a closed case scope or require abstention. Dependencies outside TOC, Index/Glossary, and A–E remain unresolved unless the source boundary is explicitly revised.
