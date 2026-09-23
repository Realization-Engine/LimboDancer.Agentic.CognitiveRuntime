# ASL-OT-03.5 Conformance and ASL-OT-04 Admission Review

**Status:** Draft; admission to ASL-OT-04 not approved  
**Assessment date:** 2026-09-23  
**Source baseline:** ASL 3.01 Chapters A-E, registered at source commit `a3254ff1d492dbdd28483d86f5b42437b48e80d4` under the historical `asl-easlrb-3.10-a-e` ID
**Implementation baseline:** `decision-plane` commit `456872a56270bab59fadfaad8409e7c7a096be5e`

**Original PDF supplied for comparison:** `eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247` (716 pages; not stored in this repository). The user reports that its delivery ZIP was labeled **version 3.01**; the ZIP itself has not been inspected here. The PDF's introductory credits identify **“Version 3.0; June 2025”** on physical PDF page 5 (roman-numbered page iii). The earlier registry value `3.10` was a designation error, corrected here to `3.01`.

## 1. Decision

The current ASL authoring code can pin source bytes, extract structure, create review evidence, and project a non-accepting curated review history. It **cannot** validate ASL semantics or authorize an accepted proposal. The [Ontology Transformation Specification](<./LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) §19 requires an approved package manifest profile and **exact verified Scenario A1 source subset** before ASL-OT-04 begins. Neither is presently available. The PDF used to generate the Markdown has now been supplied; the initial comparison below establishes candidate locations and exposes a conversion boundary discrepancy. The declared edition has been corrected to the delivery version `3.01` based on the user's provenance statement; full fragment verification is still pending. This review grants no ASL-OT-04 admission, semantic labels, or publication authority.

The hosted [ASL Authoring CI run](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/35927212328) built the .NET 10 solution and passed 72 tests, none skipped. Those tests establish implementation behavior and source hash reproducibility; they do not compare the Markdown with an authoritative ASL edition or certify a semantic interpretation.

## 2. Scenario A1 candidate source boundary

The candidate below is an **investigation set**, not an exhaustive dependency closure or verified rule package. The immediate question is whether a known unit may enter a specified building *Location now*, given phase, occupants, and modifiers. A hex may contain multiple Locations; the phase and occupant classification affect which provisions apply. The source registry pins two Markdown files:

| Registered source | Exact SHA-256 | Candidate use |
| --- | --- | --- |
| [Chapter A](<./Rulebook_Markdown/02 - Chapter A - Infantry and Basic Game Rules.md>) | `6347534e64f739cacc57dd75c3d604997723fd0b4988565609deeff007a8843d` | Location, movement phase, occupant and concealment rules. |
| [Chapter B](<./Rulebook_Markdown/03 - Chapter B - Terrain.md>) | `836b1ce5f845efd1c21250a9e6a39e4054ea9398f370305bc3771b6e7f826982` | Building entry and fortified-location restrictions. |

The line and page values below are positions in these exact registered Markdown bytes. The Markdown's `<!-- page N -->` markers attempt to identify the **physical PDF page index** (starting at 1), not the page label printed within each chapter. The PDF column records a visual check of the supplied original PDF; this is a location and initial text comparison, not a complete character-by-character or visual-dependency attestation. The normalized identifiers are **candidate lookup labels** here; no canonical semantic identity or individual fragment ID has been assigned by this review.

| Candidate | Markdown line; conversion page marker | Original PDF physical page | Why it enters review | Known closure question |
| --- | --- | --- | --- | --- |
| A2.8 | A:189; p.47 | 47 | Distinguishes a hex from its constituent Locations. | Which building levels and units share the target Location? |
| A4.14 | A:255; p.48 | **49** | Movement-phase enemy-occupancy restriction and listed exceptions. | Which exception branches matter for the chosen unit and modifiers? |
| A4.15 | A:257; p.48 | **49** | Infantry overrun exception involving an enemy SMC. | NTC, unit type, occupant count, and any linked detection conditions. |
| A4.7 | A:329; p.52 | 52 | Advance-phase eligibility and movement limits. | APh unit state, accessibility, and linked restrictions. |
| A12.15 | A:1042; p.78 | 78 | Detection during attempted entry into a concealed unit's Location. | Hidden/concealed occupant evidence and follow-on branches. |
| B23.4 | B:1382; p.136 | 136 | Ordinary building entry and level-change costs. | Road, bypass, terrain and MF interactions. |
| B23.922 | B:1556; p.140, continued at p.141 | 140–141 | Fortified-building entry restriction with a breach exception. | Enemy squad status/equivalence and fortified-location evidence. |
| B23.9221 | B:1562; p.141 | 141 | Breach path for the fortified-building exception. | Breach creation and whether the proposed entry traverses it. |

**Comparison finding:** A4.14 and A4.15 are visibly on PDF page 49 (chapter page A7), although the preceding Markdown marker remains `<!-- page 48 -->`; the conversion does not insert the page-49 marker at that boundary. B23.922 spans pages 140–141 and crosses a page marker *inside* its paragraph. An identifier-to-page lookup based only on the preceding Markdown marker would miscite A4.14/A4.15; a fragment extractor that stops at a page marker would truncate B23.922. Keep the registered Markdown bytes intact, record the corrected PDF locator as separate provenance, and test both boundaries in the C# locator/extractor before issuing exact fragment verification records. The visual spot check found the candidate headings and the corresponding opening rule content on the cited PDF pages; it did not certify every character, footnote, figure, or dependency.

**Edition identity correction:** The user identifies the delivery ZIP as version `3.01`, consistent with `v3_01` in the PDF filename; no PDF or converted Markdown edition label supports `3.10`. The PDF's introductory credits still say `Version 3.0; June 2025`, so this introductory statement may predate the packaging revision. The source registry's declared edition and the specification's source baseline were corrected from `3.10` to `3.01`. The existing registry ID, source IDs, package-candidate ID and generated artifact filenames retain `3.10` as **historical identifiers only**. Their spelling must never be interpreted as a claim about the edition. Re-keying these already generated identities would alter fragment IDs, TIR identities and downstream citations; if that becomes necessary, use an explicit migration with an old-to-new identity map.

The first closure expansion must examine at least A4.132 (road entry), A2.4 (cumulative terrain), applicable A5 stacking limits, B23.711 (breach creation), and the exceptions cited by A4.14. The exact list depends on the independently approved case scope. Vehicles, cavalry, unknown SSR effects, concealed occupants, and unmodeled exceptions must yield non-definitive outcomes unless their own dependencies are verified and modeled. A positive or negative label inferred from only the table above would be unsupported.

## 3. Current evidence and missing proofs

| Gate item | Observed evidence | Disposition |
| --- | --- | --- |
| TIR schema and identity | TIR 1.3 and the ASL-OT-02 review are committed. | Approved for structural extraction, not semantic meaning. |
| Canonical locator and source bytes | Source registry pins A-E at the cited commit; current Chapter A/B SHA-256 values match the registry. The supplied 716-page PDF is separately identified by its SHA-256 above; candidate locations were visually compared. The delivery ZIP was reportedly labeled 3.01. | Reproducible conversion input and initial original-PDF comparison; exact fragment verification remains pending. Historical `3.10` identifiers are not edition metadata. |
| Formalization status and roles | ASL-OT-03 design distinguishes unmodeled/partial/validated and records actor roles. | Accepted design; source attestation does not authenticate a person. |
| Candidate A1 fragment coverage | The committed verification sample has 14 fragments, all `unverified`; none has the eight normalized identifiers above. The committed TIR sample has no matching A1 artifacts. The PDF comparison found a missing page-49 marker and a paragraph crossing pages 140–141. | Exact A1 fragment IDs, full spans, edition comparison, and visual-dependency checks still need recorded C# locator/extractor output. |
| Source dependencies | Candidate text names exception and cross-reference targets, and fortified breach refers to another rule. | Dependency and evaluation closure not yet enumerated or reviewed. |
| Package manifest profile | Specification §11 lists required fields but leaves canonical serialization to the first slice. The candidate profile below is a review proposal. | No implemented/versioned schema or review approval yet. |
| Structural/review implementation | Hosted CI passes; captured bundles, curated submission and non-accepting history, and a blocked readiness assessment exist. | ASL-OT-03.4 remains partial; no acceptance projection or adjudication contract. |
| ASL-OT-03.5 completion | No representative committed curated review bundle, complete declared-use source-verification record set, or ASL-OT-03 approval review exists. | Pending; do not announce ASL-OT-03 complete. |

## 4. Proposed candidate-package manifest profile for review

This is a **design proposal**, not a published package manifest or an approved canonical schema. An ASL-OT-04 candidate should carry an immutable root digest over a canonical UTF-8 JSON payload excluding its own digest field. The first C# implementation should version the schema and canonicalization profile together; object-property order is fixed, set-valued arrays sort by their canonical IDs, and meaningful ordered sequences retain order. The writer must recompute referenced artifacts and report digests from their actual bytes before output. A caller-supplied hash or approval flag is never sufficient.

| Proposed field family | Minimum content and boundary |
| --- | --- |
| Identity | Candidate domain/package/version, manifest schema/profile version, creation time and source. **No published `DomainPackageRef`.** |
| Source baseline | Exact ASL 3.01 declared edition and historical registry ID, registry digest, source commit, applicable chapter/file digests, redistribution restrictions, and edition verification references. |
| Structural baseline | TIR schema/profile/extractor identities, exact TIR document digest, source fragment IDs and spans, and structural diagnostic IDs in the declared scope. |
| Curated inventory | Exact per-artifact semantic kind, identity, schema version, digest, source-fragment references, dependencies, formalization coverage, and unmodeled/ambiguous/conflicting portions. No free-form prose can masquerade as a validated expression. |
| Evidence | Source-verification records, validation reports and policy digests, diagnostic dispositions, reviewer/adjudication record IDs, effective review status, and declared-use dependency closure. References must resolve to accompanying canonical content. |
| Compatibility | Candidate dependency versions, applicable scenario/case identifiers, migration and compatibility classification, and known unsupported branches. Publication/supersession timestamps are absent until ASL-OT-05. |

Review decisions still needed: whether a single root manifest embeds the source/review records or includes them as a content-addressed inventory; which semantic artifact schema binds rule expressions and exception precedence; what counts as closed dependency evidence for a declared use; and how an explicit `partial` artifact excludes definitive cases. The C# schema, writer, and conformance tests should follow those decisions rather than choose them implicitly during ASL-OT-04 coding. This profile is sufficient for reviewers to evaluate the intended **candidate** boundary; it does not claim a publishable package format.

## 5. Required next evidence

1. Fix an explicit **case scope** for the first A1 examples: unit type, phase, occupant state, building status, available MF, and scenario modifiers. List which cases must abstain rather than silently treating exclusions as false.
2. Use the **C#** locator/extractor against the registered source commit to produce exact fragment IDs, line/byte spans, dependencies, and corresponding structural artifacts for that scope. Re-run conformance if the source bytes or extractor version change.
3. Compare each required fragment and visual/table dependency in full against the supplied version-3.01 PDF, checking the A4.14/A4.15 page-49 locator and the B23.922 page crossing explicitly. Record an exact `SourceVerificationRecord` per fragment, with the verifier's identity, method, edition, and disposition. Preserve differences; do not repair converted text in place. Record the PDF's older `3.0` introductory credit and user-reported ZIP designation as separate provenance facts.
4. Review the dependency graph with an ASL domain reviewer, including phase, stacking, concealed occupants, fortified entry, breach, exceptions, and any controlling SSR. Resolve or scope the relevant TIR diagnostics. Record the approved subset and excluded branches explicitly.
5. Review the proposed **candidate package manifest profile** above, resolve its four open design decisions, then implement a versioned C# schema/writer with reproducible hashes. Preserve the distinction from a published `DomainPackageRef`.
6. Complete the ASL-OT-03.4 acceptance/adjudication contract only when a real semantic representation can be validated; commit a representative review bundle and the ASL-OT-03.5 conformance result. Independent reviewers then decide the §19 admission gate before ASL-OT-04 scenario semantics are asserted.

No Python source or dependency is introduced into the ASL implementation. The older PDF-to-Markdown converter remains outside this work.
