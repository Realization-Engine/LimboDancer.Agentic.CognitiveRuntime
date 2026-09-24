# Scenario A1 source review packet

**Status:** Source fidelity attested by the source provider for 11 exact subjects; semantic review and dependency closure pending
**Edition:** 3.01 delivery ZIP as identified by the source provider; supplied PDF SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`  
**Source boundary:** TOC, Index/Glossary, Chapters A–E and their registered images; PDF physical pages 6–253 only  
**Registered source commit:** `a3254ff1d492dbdd28483d86f5b42437b48e80d4`

## Review subjects

The [C# candidate inventory](<./SourceRegistry/asl-scenario-a1.candidate-source-inventory.json>) gives exact source IDs, fragment IDs, content hashes, line spans, conversion page markers and separately checked PDF pages. The [comparison evidence](<./SourceRegistry/asl-scenario-a1.pdf-comparison.json>) records the nine complete prose comparisons, one linked footnote and one visual comparison. The [source-provider attestation](<./SourceRegistry/asl-scenario-a1.source-attestation.json>) names **11 distinct review subjects**: nine rule prose fragments, one Chapter A footnote fragment, and one figure-reference fragment with a registered image dependency. The [generated verification batch](<./SourceRegistry/asl-scenario-a1.source-verification.json>) binds each subject to a full TIR artifact and records its `Verified` source-fidelity disposition. The attestation records a conversation identity; it does not independently authenticate a named reviewer.

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

The tool-assisted comparison found matching alphanumeric sequences for all ten prose subjects. Punctuation differs only by PDF line-layout hyphens in A12.15, B23.922, B23.9221, and footnote 3. The Breach image was visually matched to the rendered page; pixel identity is not asserted. The source provider subsequently attested the PDF fidelity of all 11 cited subjects. Neither comparison nor attestation establishes rule meaning.

## Formal record handoff

`AslScenarioA1VerificationBatchBuilder` validates the pinned PDF digest and all 11 attested fragment IDs, extracts the **full** TIR from the registered source bytes, and passes each exact source-fragment artifact to `TirSourceVerificationService.CreateRecord`. The batch binds records to the full document and artifact digests. The committed TIR **representative sample** remains separate and does not contain the A1 artifacts. CI regenerates the batch from the C# CLI and compares its bytes with the committed file. The record identifies the source provider's conversation attestation; it does not claim an independently authenticated verifier or a second independent review.

For each subject, a future independent source verifier can examine the existing disposition and:

1. Confirm the PDF checksum and the relevant page, the complete Markdown fragment and its hash, and any linked figure/footnote or page continuation. Keep PDF physical pages distinct from conversion markers.
2. Record their own identity, review time, comparison method, artifact reference, disposition and observed discrepancy using the C# review contract. The comparison report is evidence to inspect, not a second review decision.
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

### C# first-case assessment

`AslScenarioA1CaseAssessor` captures nine declared case facts and locates an initial 11-rule baseline: A2.4, A2.8, A3.3, A4.1, A4.11, A4.13, A4.14, A5.1, A5.11, B23.1 and B23.4. Of these, A2.8, A4.14 and B23.4 have the source-provider fidelity attestation above. The [additional PDF comparison evidence](<./SourceRegistry/asl-scenario-a1.first-case-pdf-comparison.json>) covers the other **eight rule declarations and two figure-reference fragments**. It finds complete alphanumeric matches for the rule prose after markup/layout normalization; the linked A2.4 and B23.1 images visually match the rendered PDF pages. These ten exact subjects remain **unverified** until a source provider reviews their comparisons and attests them. B23.1 inherits Markdown conversion page 134 but is on physical PDF page **135**, including its illustration; both locations remain distinct in the evidence. The eight separately listed exclusion branches are A4.132, A4.134, A4.15, A4.7, A12.15, B23.711, B23.922 and B23.9221. A domain reviewer must confirm both the baseline and the applicability of each exclusion; a declared absent feature does not automatically prove its branch irrelevant under all exceptions.

The xUnit cases check missing baseline verification (including both figures), unknown occupancy, a conflicting special-rule fact, and the semantic gate even if every candidate source ID is marked verified. They also check that the additional comparison references ten exact registered fragments and registered image hashes. This assessment always refuses a definitive entry ruling. It is an explicit worklist and scope check, not a semantic interpreter or an admission decision.

The C# pending-comparison batch records an `Indeterminate` disposition for each of the ten new subjects against the **same full TIR document** as the first 11 verified subjects. It records the comparison process as its actor and explicitly states that human source-fidelity attestation is pending. An image hash is a byte-level dependency check, and a visual match is a comparison observation; neither permits `Verified` without the additional source review. This batch does not change the first 11 records.

### Dependency review findings to resolve

The first-case baseline is **not closed**. This preliminary reading of the registered source identifies at least these additional checks for an ASL domain reviewer:

| Source | Question for the declared first case | Current status |
| --- | --- | --- |
| A5.1 → A5.5 | Does classifying the squad and any units already in the destination require the squad-equivalence rule, even though the proposed case declares the destination empty and below stacking limits? | A5.5 is not in the initial C# baseline or the ten new PDF comparisons. Add it if the reviewer finds it controlling. |
| A4.13 → MF Entrance Cost terrain chart; B23.4 | Does the two-MF ordinary building cost from B23.4 suffice when the input excludes roads, Bypass, elevation changes and additional terrain? Identify the exact chart artifact if the chart is independently required. | Chart evidence and any applicable cross-reference remain unresolved; no numeric entry verdict is authorized. |
| A4.11, A4.12 and A4.1 | What evidence establishes the unit's actual remaining MF and movement capability, including prior fire, TI, Melee, experience, portage and any leader bonus? | `CanMoveThisPhase` and `HasEnoughMovementFactors` are declared case facts, not computed by the assessor. |
| B23.1 and B23.9; A4.14 and A12.15 | What authoritative state establishes that the destination is an ordinary, non-fortified building Location with no concealed or hidden occupant? | Known empty and ordinary status are declared facts. Incomplete observation must leave the case outside scope. |
| Scenario special rules and excluded branches | Do any SSR, setup condition, or rule exception override the declared ordinary entry case? | Explicit absence is a declared fact; an unexamined scenario cannot supply it. |

These findings do not make A5.5 or a chart automatically controlling. The domain reviewer must decide applicability, enumerate any added exact source fragments, and record the rejected and unresolved branches before proposing a closed dependency set.

The [back-matter boundary review](<./LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>) finds the B. Terrain Chart on physical PDF page **698**, outside the original 6–253 source registry. The [bounded chart candidate](<./SourceRegistry/asl-scenario-a1.backmatter-chart-candidate.json>) pins the two 2-MF building rows and their page-text provenance without importing the PDF page. The C# case assessment exposes that candidate and retains a source-boundary blocker. Other A–E-labelled aids are triaged for specific uses. No page-698 content is in the current source registry or verification batches.
