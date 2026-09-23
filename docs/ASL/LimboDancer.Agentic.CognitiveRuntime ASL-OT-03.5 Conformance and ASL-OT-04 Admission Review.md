# ASL-OT-03.5 Conformance and ASL-OT-04 Admission Review

**Status:** Draft; admission to ASL-OT-04 not approved  
**Assessment date:** 2026-09-23  
**Source baseline:** ASL 3.10 A-E registry at `a3254ff1d492dbdd28483d86f5b42437b48e80d4`  
**Implementation baseline:** `decision-plane` commit `456872a56270bab59fadfaad8409e7c7a096be5e`

## 1. Decision

The current ASL authoring code can pin source bytes, extract structure, create review evidence, and project a non-accepting curated review history. It **cannot** validate ASL semantics or authorize an accepted proposal. The [Ontology Transformation Specification](<./LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) §19 requires an approved package manifest profile and **exact verified Scenario A1 source subset** before ASL-OT-04 begins. Neither is presently available. This review inventories the candidate subset and records the remaining gate work; it grants no ASL-OT-04 admission, semantic labels, or publication authority.

The hosted [ASL Authoring CI run](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/actions/runs/35927212328) built the .NET 10 solution and passed 72 tests, none skipped. Those tests establish implementation behavior and source hash reproducibility; they do not compare the Markdown with an authoritative ASL edition or certify a semantic interpretation.

## 2. Scenario A1 candidate source boundary

The candidate below is an **investigation set**, not an exhaustive dependency closure or verified rule package. The immediate question is whether a known unit may enter a specified building *Location now*, given phase, occupants, and modifiers. A hex may contain multiple Locations; the phase and occupant classification affect which provisions apply. The source registry pins two Markdown files:

| Registered source | Exact SHA-256 | Candidate use |
| --- | --- | --- |
| [Chapter A](<./Rulebook_Markdown/02 - Chapter A - Infantry and Basic Game Rules.md>) | `6347534e64f739cacc57dd75c3d604997723fd0b4988565609deeff007a8843d` | Location, movement phase, occupant and concealment rules. |
| [Chapter B](<./Rulebook_Markdown/03 - Chapter B - Terrain.md>) | `836b1ce5f845efd1c21250a9e6a39e4054ea9398f370305bc3771b6e7f826982` | Building entry and fortified-location restrictions. |

The line and page values below are positions in these exact registered Markdown bytes. Page markers come from the conversion, and neither page markers nor text have been checked against the authoritative edition. The normalized identifiers are **candidate lookup labels** here; no canonical semantic identity or individual fragment ID has been assigned by this review.

| Candidate | Markdown line; printed page marker | Why it enters review | Known closure question |
| --- | --- | --- | --- |
| A2.8 | A:189; p.47 | Distinguishes a hex from its constituent Locations. | Which building levels and units share the target Location? |
| A4.14 | A:255; p.48 | Movement-phase enemy-occupancy restriction and listed exceptions. | Which exception branches matter for the chosen unit and modifiers? |
| A4.15 | A:257; p.48 | Infantry overrun exception involving an enemy SMC. | NTC, unit type, occupant count, and any linked detection conditions. |
| A4.7 | A:329; p.52 | Advance-phase eligibility and movement limits. | APh unit state, accessibility, and linked restrictions. |
| A12.15 | A:1042; p.78 | Detection during attempted entry into a concealed unit's Location. | Hidden/concealed occupant evidence and follow-on branches. |
| B23.4 | B:1382; p.136 | Ordinary building entry and level-change costs. | Road, bypass, terrain and MF interactions. |
| B23.922 | B:1556; p.140 | Fortified-building entry restriction with a breach exception. | Enemy squad status/equivalence and fortified-location evidence. |
| B23.9221 | B:1562; p.141 | Breach path for the fortified-building exception. | Breach creation and whether the proposed entry traverses it. |

The first closure expansion must examine at least A4.132 (road entry), A2.4 (cumulative terrain), applicable A5 stacking limits, B23.711 (breach creation), and the exceptions cited by A4.14. The exact list depends on the independently approved case scope. Vehicles, cavalry, unknown SSR effects, concealed occupants, and unmodeled exceptions must yield non-definitive outcomes unless their own dependencies are verified and modeled. A positive or negative label inferred from only the table above would be unsupported.

## 3. Current evidence and missing proofs

| Gate item | Observed evidence | Disposition |
| --- | --- | --- |
| TIR schema and identity | TIR 1.3 and the ASL-OT-02 review are committed. | Approved for structural extraction, not semantic meaning. |
| Canonical locator and source bytes | Source registry pins A-E at the cited commit; current Chapter A/B SHA-256 values match the registry. | Reproducible conversion input; no authoritative-edition comparison. |
| Formalization status and roles | ASL-OT-03 design distinguishes unmodeled/partial/validated and records actor roles. | Accepted design; source attestation does not authenticate a person. |
| Candidate A1 fragment coverage | The committed verification sample has 14 fragments, all `unverified`; none has the eight normalized identifiers above. The committed TIR sample has no matching A1 artifacts. | Exact A1 fragment IDs and spans still need C# locator/extractor output and authoritative comparison. |
| Source dependencies | Candidate text names exception and cross-reference targets, and fortified breach refers to another rule. | Dependency and evaluation closure not yet enumerated or reviewed. |
| Package manifest profile | Specification §11 lists required fields but leaves canonical serialization to the first slice. The candidate profile below is a review proposal. | No implemented/versioned schema or review approval yet. |
| Structural/review implementation | Hosted CI passes; captured bundles, curated submission and non-accepting history, and a blocked readiness assessment exist. | ASL-OT-03.4 remains partial; no acceptance projection or adjudication contract. |
| ASL-OT-03.5 completion | No representative committed curated review bundle, complete declared-use source-verification record set, or ASL-OT-03 approval review exists. | Pending; do not announce ASL-OT-03 complete. |

## 4. Proposed candidate-package manifest profile for review

This is a **design proposal**, not a published package manifest or an approved canonical schema. An ASL-OT-04 candidate should carry an immutable root digest over a canonical UTF-8 JSON payload excluding its own digest field. The first C# implementation should version the schema and canonicalization profile together; object-property order is fixed, set-valued arrays sort by their canonical IDs, and meaningful ordered sequences retain order. The writer must recompute referenced artifacts and report digests from their actual bytes before output. A caller-supplied hash or approval flag is never sufficient.

| Proposed field family | Minimum content and boundary |
| --- | --- |
| Identity | Candidate domain/package/version, manifest schema/profile version, creation time and source. **No published `DomainPackageRef`.** |
| Source baseline | Exact ASL 3.10 registry ID, registry digest, source commit, applicable chapter/file digests, redistribution restrictions, and edition verification references. |
| Structural baseline | TIR schema/profile/extractor identities, exact TIR document digest, source fragment IDs and spans, and structural diagnostic IDs in the declared scope. |
| Curated inventory | Exact per-artifact semantic kind, identity, schema version, digest, source-fragment references, dependencies, formalization coverage, and unmodeled/ambiguous/conflicting portions. No free-form prose can masquerade as a validated expression. |
| Evidence | Source-verification records, validation reports and policy digests, diagnostic dispositions, reviewer/adjudication record IDs, effective review status, and declared-use dependency closure. References must resolve to accompanying canonical content. |
| Compatibility | Candidate dependency versions, applicable scenario/case identifiers, migration and compatibility classification, and known unsupported branches. Publication/supersession timestamps are absent until ASL-OT-05. |

Review decisions still needed: whether a single root manifest embeds the source/review records or includes them as a content-addressed inventory; which semantic artifact schema binds rule expressions and exception precedence; what counts as closed dependency evidence for a declared use; and how an explicit `partial` artifact excludes definitive cases. The C# schema, writer, and conformance tests should follow those decisions rather than choose them implicitly during ASL-OT-04 coding. This profile is sufficient for reviewers to evaluate the intended **candidate** boundary; it does not claim a publishable package format.

## 5. Required next evidence

1. Fix an explicit **case scope** for the first A1 examples: unit type, phase, occupant state, building status, available MF, and scenario modifiers. List which cases must abstain rather than silently treating exclusions as false.
2. Use the **C#** locator/extractor against the registered source commit to produce exact fragment IDs, line/byte spans, dependencies, and corresponding structural artifacts for that scope. Re-run conformance if the source bytes or extractor version change.
3. Compare each required fragment and visual/table dependency with the authoritative ASL edition. Record an exact `SourceVerificationRecord` per fragment, with the verifier's identity, method, edition, and disposition. Preserve differences; do not repair converted text in place.
4. Review the dependency graph with an ASL domain reviewer, including phase, stacking, concealed occupants, fortified entry, breach, exceptions, and any controlling SSR. Resolve or scope the relevant TIR diagnostics. Record the approved subset and excluded branches explicitly.
5. Review the proposed **candidate package manifest profile** above, resolve its four open design decisions, then implement a versioned C# schema/writer with reproducible hashes. Preserve the distinction from a published `DomainPackageRef`.
6. Complete the ASL-OT-03.4 acceptance/adjudication contract only when a real semantic representation can be validated; commit a representative review bundle and the ASL-OT-03.5 conformance result. Independent reviewers then decide the §19 admission gate before ASL-OT-04 scenario semantics are asserted.

No Python source or dependency is introduced into the ASL implementation. The older PDF-to-Markdown converter remains outside this work.
