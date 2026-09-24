# ASL A–E back-matter source boundary review

**Status:** Candidate scope amendment; original registry and its 11 verified subjects unchanged
**Source PDF:** user-supplied `eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`
**Edition statement:** delivery ZIP identified by the source provider as 3.01; PDF credits say 3.0
**Current registered boundary:** TOC, Index/Glossary, Chapters A–E and their images, physical pages 6–253

## Finding

The current page boundary is **insufficient as a general definition of A–E source material**. A4.13 cites the Terrain Chart for movement cost; the supplied PDF has the **B. Terrain Chart on physical page 698**. This page is outside the seven registered Markdown files and the currently registered page range. It may be controlling for an A–E scenario even though it is located among the rulebook's later player aids. The existing registry must not silently import it or claim it was covered by the 11 source-provider attestations.

A visual inspection of page 698 found the Infantry MF entrance-cost column and **separate wooden and stone building rows**, each listing **2 MF**. B23.4 states the same ordinary building entry cost in registered Chapter B prose. A4.13 explicitly cites the Terrain Chart as the source for Infantry terrain costs, so the chart remains an **unresolved candidate source dependency** for the first case even though the two numbers agree. Matching numbers do not resolve the authority relationship between rule and chart.

The [bounded chart candidate](<./SourceRegistry/asl-scenario-a1.backmatter-chart-candidate.json>) pins the supplied PDF SHA-256, physical page 698, the SHA-256 of `pdftotext 24.02.0 -f 698 -l 698 -layout` output, the MF header-line hash, and individual extracted-line hashes for the two building rows. It does not import or republish the full PDF page. The chart row notes and legend must be inspected with the source before verifying the artifact; this metadata and the visual spot check are **not** source-provider attestation.

## Back-matter candidates located in the supplied PDF

These are **physical PDF page numbers**, not chapter page labels. This preliminary inventory was obtained by inspecting headings and extracted text near the end of the supplied PDF. It is a triage list, not a claim that every row is required by Scenario A1 or that the list is complete.

| Physical page(s) | Candidate material | A–E relationship and disposition |
| --- | --- | --- |
| 676–677 | Infantry/support-weapon and Gun/vehicle counter examples | Candidate explanatory aids; classify any A–E rules that depend on their counter depiction. |
| 678–681 | Advanced Sequence of Play | Candidate phase/timing aid for A3.3 and other A–E phase rules; determine whether it is necessary for the declared use. |
| 692 | A7 Infantry Fire Table | Chapter A table; potentially relevant to other uses, not automatically to empty-building entry. |
| 693 | A12.121 Concealment Loss/Gain Table | Chapter A table; assess relevance to concealed/hidden occupant cases, not to an attested empty destination. |
| 694–697 | A-labelled VP, Sniper, national-capability, control, Fire Lane and other aids | Classify individually against actual A–E references before importing. |
| **698** | **B. Terrain Chart** | Explicit candidate dependency of A4.13; requires its own registered source artifact, chart-boundary extraction and source review if the domain reviewer determines it controls Scenario A1. |
| 699 | A24 Smoke Summary and B24 rubble summary | Candidate for branches excluded by the declared ordinary-entry case. |
| 700–702 | C-labelled to-hit, kill and related aids | Potentially within Chapter C uses; include only when a declared A–E dependency requires them. |
| 703–705 | D/E-labelled vehicle and night aids | Potentially within Chapters D/E uses; not automatically needed for the Infantry case. |
| 706–716 | F/G/W and other late charts, scenario aids and duplicates | Outside A–E unless a specific in-scope reference is declared and a separate boundary decision approves it. |

## Boundary rule proposed for review

Continue treating the original registered pages 6–253 as immutable evidence. Add **individually identified back-matter artifacts** only when an in-scope A–E rule cites them or a reviewer shows that the declared evaluation requires them. Each addition needs its own physical page, bounded table/figure region, source-file or image hash, extraction identity, comparison record, and explicit decision about applicable row(s). Record an outside-scope reference as unresolved until then. A supplementary registry revision must not re-key or retroactively expand the first 11 verified records.

For Scenario A1, A4.13's citation and the two matching 2-MF rows identify the **B. Terrain Chart building rows, applicable row notes and legend** as a candidate companion artifact. The C# first-case assessor retains a named source-boundary blocker until a supplementary source artifact is registered and verified or a domain reviewer explicitly determines that the chart is unnecessary for this declared use. The Advanced Sequence of Play (pages 678–681) is a separate timing candidate; an already established MPh fact does not by itself prove that every timing aid is controlling. The concealed-unit chart on page 693 pertains to branches excluded by the declared known-empty case. For stacking, A5.5 is within the already registered Chapter A prose; determine whether it becomes controlling only when `IsBelowStackingLimit` is derived from raw unit state instead of supplied as a declared input fact.
