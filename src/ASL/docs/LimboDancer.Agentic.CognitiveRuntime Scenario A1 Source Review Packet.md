# Scenario A1 source review packet

**Status:** Delegated xUnit review accepted the bounded first case for 11 baseline rule IDs, ten additional source subjects and the page-698 chart. General rule interpretation and package admission remain outside this case.
**Edition:** 3.01 delivery ZIP as identified by the source provider; supplied PDF SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`  
**Source boundary:** TOC, Index/Glossary, Chapters A–E and their registered images; PDF physical pages 6–253 only  
**Registered source commit:** `a3254ff1d492dbdd28483d86f5b42437b48e80d4`

## Review subjects

The [C# candidate inventory](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.candidate-source-inventory.json>) gives exact source IDs, fragment IDs, content hashes, line spans, conversion page markers and separately checked PDF pages. The [comparison evidence](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.pdf-comparison.json>) records the nine complete prose comparisons, one linked footnote and one visual comparison. The [source-provider attestation](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.source-attestation.json>) names **11 distinct review subjects**: nine rule prose fragments, one Chapter A footnote fragment, and one figure-reference fragment with a registered image dependency. The [generated verification batch](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.source-verification.json>) binds each subject to a full TIR artifact and records its `Verified` source-fidelity disposition. The attestation records a conversation identity; it does not independently authenticate a named reviewer.

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

## First case: historical dependency inventory

Use a synthetic *candidate* case: a known Good Order, unpinned Infantry squad attempts an MPh ground-level entry into an adjacent ordinary building Location; the destination has no enemy, concealed or hidden occupants, no fortification, no road entry, no Bypass, no elevation change, no additional terrain, no SMOKE, no scenario special rule, enough remaining MF, and stacking below the normal limit. These are declared input facts for a bounded review, not facts inferred from an incomplete board observation or a validated outcome.

| Dependency family | In-scope source to examine | Current review state |
| --- | --- | --- |
| Location and terrain | A2.8, A2.4, B23.1, B23.4 | A2.8 and B23.4 compared with PDF; A2.4 and B23.1 require source comparison and semantic review. |
| Phase, capability and cost | A3.3, A4.11, A4.13 and any controlling movement limits | Additional source and domain review required; sufficient MF alone does not prove eligibility. |
| Occupancy and stacking | A4.14, A5.1, A5.11 | A4.14 compared with PDF; stacking and classification still require review. |
| Excluded branches | A4.132 (road), A4.134 (Minimum Move), A4.15 (OVR), A4.7 (APh), A12.15 (concealed occupancy), B23.922/23.9221 (fortified entry/breach) | Compare applicability and explicit exclusion assumptions; unknown occupant or terrain status must abstain. |

This is an **initial dependency inventory**, not a closed rule graph or a positive legality label. The independent ASL domain reviewer must examine cross-references and exceptions under the declared facts, add any missing rules, and either approve a closed case scope or require abstention. Dependencies outside TOC, Index/Glossary, and A–E remain unresolved unless the source boundary is explicitly revised.

### Initial C# first-case assessment (before delegated review)

`AslScenarioA1CaseAssessor` captures nine declared case facts and locates an initial 11-rule baseline: A2.4, A2.8, A3.3, A4.1, A4.11, A4.13, A4.14, A5.1, A5.11, B23.1 and B23.4. Of these, A2.8, A4.14 and B23.4 have the source-provider fidelity attestation above. The [additional PDF comparison evidence](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.first-case-pdf-comparison.json>) covers the other **eight rule declarations and two figure-reference fragments**. It finds complete alphanumeric matches for the rule prose after markup/layout normalization; the linked A2.4 and B23.1 images visually match the rendered PDF pages. These ten exact subjects remain **unverified** until a source provider reviews their comparisons and attests them. B23.1 inherits Markdown conversion page 134 but is on physical PDF page **135**, including its illustration; both locations remain distinct in the evidence. The eight separately listed exclusion branches are A4.132, A4.134, A4.15, A4.7, A12.15, B23.711, B23.922 and B23.9221. A domain reviewer must confirm both the baseline and the applicability of each exclusion; a declared absent feature does not automatically prove its branch irrelevant under all exceptions.

The xUnit cases check missing baseline verification (including both figures), unknown occupancy, a conflicting special-rule fact, and the semantic gate even if every candidate source ID is marked verified. They also check that the additional comparison references ten exact registered fragments and registered image hashes. This assessment always refuses a definitive entry ruling. It is an explicit worklist and scope check, not a semantic interpreter or an admission decision.

The C# pending-comparison batch records an `Indeterminate` disposition for each of the ten new subjects against the **same full TIR document** as the first 11 verified subjects. It records the comparison process as its actor and explicitly states that human source-fidelity attestation is pending. An image hash is a byte-level dependency check, and a visual match is a comparison observation; neither permits `Verified` without the additional source review. This batch does not change the first 11 records.

### Dependency review findings (resolved for the declared case below)

The first-case baseline is **not closed**. This preliminary reading of the registered source identifies at least these additional checks for an ASL domain reviewer:

| Source | Question for the declared first case | Current status |
| --- | --- | --- |
| A5.1 → A5.5 | Does classifying the squad and any units already in the destination require the squad-equivalence rule, even though the proposed case declares the destination empty and below stacking limits? | A5.5 is not in the initial C# baseline or the ten new PDF comparisons. Add it if the reviewer finds it controlling. |
| A4.13 → MF Entrance Cost terrain chart; B23.4 | Does the two-MF ordinary building cost from B23.4 suffice when the input excludes roads, Bypass, elevation changes and additional terrain? Identify the exact chart artifact if the chart is independently required. | Chart evidence and any applicable cross-reference remain unresolved; no numeric entry verdict is authorized. |
| A4.11, A4.12 and A4.1 | What evidence establishes the unit's actual remaining MF and movement capability, including prior fire, TI, Melee, experience, portage and any leader bonus? | `CanMoveThisPhase` and `HasEnoughMovementFactors` are declared case facts, not computed by the assessor. |
| B23.1 and B23.9; A4.14 and A12.15 | What authoritative state establishes that the destination is an ordinary, non-fortified building Location with no concealed or hidden occupant? | Known empty and ordinary status are declared facts. Incomplete observation must leave the case outside scope. |
| Scenario special rules and excluded branches | Do any SSR, setup condition, or rule exception override the declared ordinary entry case? | Explicit absence is a declared fact; an unexamined scenario cannot supply it. |

These findings do not make A5.5 or a chart automatically controlling. The domain reviewer must decide applicability, enumerate any added exact source fragments, and record the rejected and unresolved branches before proposing a closed dependency set.

The [back-matter boundary review](<./LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>) finds the B. Terrain Chart on physical PDF page **698**, outside the original 6–253 source registry. The [bounded chart candidate](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.backmatter-chart-candidate.json>) pins the two 2-MF building rows and their page-text provenance. A [separate supplementary registry](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.supplementary-source-registry.json>) pins the bounded transcription in its original unverified state. The source provider subsequently delegated the chart's bounded fidelity and first-case domain review to xUnit; the [review decision](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.chart-review-decision.json>) is checked against both the registry and [PDF comparison](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.backmatter-chart-pdf-comparison.json>) by tests. At this intermediate step the reviewed-chart C# assessment cleared only the source-boundary blocker; the ten-fragment and semantic decisions are recorded in the subsequent delegated first-case decision below. No page-698 content enters the original registry or its verification batches.

## Subsequent delegated first-case decision

The source provider directed xUnit assertions to stand in for the remaining source and domain reviewers. The [first-case decision](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.first-case-review-decision.json>) is a separate, later disposition: `AslScenarioA1FinalReviewTests` checks exact hashes of the ten-fragment PDF comparison, initial 11-fragment attestation and chart review, confirms all ten source subjects against the **same full TIR**, and creates ten `Verified` review records under the explicit delegated xUnit authority. The historical `Indeterminate` comparison batch remains intact as comparison history.

For the declared first case, the domain decision accepts the original 11-rule baseline and the reviewed chart; A5.5 and A4.12 are not added because squad identity, remaining MF and stacking status are supplied facts. Road, Bypass, elevated or compound terrain, hidden occupancy, fortification, SSR and other modifiers are excluded by the declared facts. Under those assumptions an eligible Good Order Infantry squad may enter the adjacent empty ordinary wooden or stone ground-level building during its MPh for **2 MF**. The reviewed assessment has no blockers only for those exact facts. Missing or contrary facts fail the ruling. This is a bounded first-case decision, not a general ASL interpretation, package admission, or proof that external game state satisfies the declared facts.

The final reviewer requires the exact approved 11-rule baseline, eight excluded branches, no unresolved chart source and precisely one semantic-review blocker before it applies the delegated decision. All nine declared facts must be true; the xUnit theory checks each missing fact separately. This pins the decision to the examined dependencies and prevents an expanded rule inventory or unknown case fact from inheriting this ruling.

## Read-only domain adapter

`src/ASL/LimboDancer.Domains.Asl.ScenarioA1` embeds the accepted first-case decision and checks its SHA-256 at construction. Its `IDomainPackageResolver` resolves only the exact digest-qualified package. Its `IDomainConclusionResolver` requires the pinned rule/chart source set, one versioned location observation, resolved unit and location references, and all nine declared facts. It yields the reviewed 2-MF eligibility only when all facts are explicitly true; a false fact abstains and missing or ambiguous evidence is indeterminate. The supplied observation is a declared input, not authenticated board state. This adapter does not implement occupied-building opposition, exceptions outside the declared case, state mutation, general ASL rules or full ASL-OT-04 package admission. ASL-specific behavior stays outside Runtime and Abstractions.

## Occupied-building case matrix (delegated bounded review)

The [case matrix](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.occupied-case-matrix.json>) extends the declared first case with two narrow prohibitions: ordinary MPh entry into a known unconcealed enemy MMC's Location with every A4.14 exception excluded, and entry into an unbreached Fortified Building Location containing a known unpinned Good Order armed enemy squad with no other exception. The verified A4.14 and page-crossing B23.922/B23.9221 subjects support these scoped conclusions. The matrix and xUnit review pin the declared facts, expected disposition, source IDs and supplemental chart reference; the test cross-checks each asserted rule's exact fragments against the 11 initial and ten delegated verification records.

Concealed/hidden occupancy and unknown controlling facts remain indeterminate. The single-SMC Infantry OVR path, fortified breach creation, computed squad equivalents, and APh entry are deferred: A4.151/A4.152, B23.711 and A5.5 now have bounded PDF comparison and delegated source-fidelity records; their broader semantic branches remain unresolved. APh applicability likewise requires its own semantic review. The matrix makes no positive label for those paths. A source ID in this matrix identifies a reviewed dependency for its *declared facts*; it is not a general interpretation of that rule or evidence of actual board state.

The [occupied-case PDF comparison](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.occupied-pdf-comparison.json>) records seven additional subjects: A4.151, A4.152, A5.5, linked Chapter A footnote 7, both page-crossing B23.711 prose fragments, and its Breach image. The supplied PDF digest matches the original pinned PDF. Full alphanumeric comparisons match physical pages 49, 53, 101, 137 and 138; the Breach image was visually checked on rendered page 137. `AslScenarioA1OccupiedSourceReview` validates the pinned report and exact registered bytes and creates `Verified` records against the original full TIR under the delegated xUnit review authority. This settles source fidelity for these subjects only; the matrix keeps their deferred outcomes non-definitive pending semantic closure.

## Typed candidate semantic profile

The [candidate manifest](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.candidate-manifest.json>) binds the first-case decision, occupied-case matrix and supplemental PDF comparison by byte digest. Its root digest is the SHA-256 of the ordered UTF-8 fields `schemaVersion`, `candidateId`, `sourceDecisionSha256`, `caseMatrixSha256`, `occupiedComparisonSha256`, and decimal `caseCount`, joined by LF with no trailing LF. `ScenarioA1SemanticCandidate` checks each embedded artifact, validates the three exact reviewed predicate sets, and refuses to produce a definitive result for the four deferred and two non-definitive cases. A missing predicate is indeterminate; a contrary or extra predicate abstains. These predicates are bounded input contracts, not a general ASL rule expression or accepted ASL-OT-04 package. The manifest is explicitly `candidate-semantic-profile-not-admitted`; it creates no published `DomainPackageRef`.

## Delegated bounded admission and Infantry OVR exception

The [bounded admission record](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.bounded-admission.json>) pins the regenerated candidate-manifest digest and explicitly accepts four case labels: ordinary empty-building entry, two narrow prohibitions, and a **qualified Infantry OVR entry attempt**. The xUnit review checks the 28 exact source subjects across the initial 11, later ten and occupied-case seven; A4.15's linked footnote and the reviewed 2-MF chart are included in that evidence. `ScenarioA1SemanticCandidate` validates the admission record, matrix, comparison and manifest digests before returning a reviewed result. Its overrun predicate set requires exactly one Known enemy SMC outside an AFV, a passed NTC by the moving MMC, at least four MF for the doubled ordinary-building cost, no other occupant or modifier, and unresolved defender response. The qualified conclusion permits the **attempt** only; A4.151/A4.152 still govern the defender's choice and any immediate CC. It does not determine whether movement succeeds or change occupancy.

Fortified breach, computed stacking equivalence and APh entry remain deferred. This is admission of exact synthetic labels, not general ASL-OT-04 package admission, a published `DomainPackageRef`, an authenticated board observation, or execution authority. The older read-only `IDomainConclusionResolver` remains bound to its original digest-qualified empty-building first case; the expanded candidate is evaluated separately until immutable publication and exact package resolution are implemented.
