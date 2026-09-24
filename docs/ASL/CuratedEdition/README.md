# ASL 3.01 Curated Markdown Edition (Chapters A-E)

This directory holds a curated Markdown edition of the ASL Rulebook TOC, Index/Glossary and Chapters A-E. It is built deterministically from two pinned inputs and records every change it makes:

1. The registered converter output in [`../Rulebook_Markdown/`](../Rulebook_Markdown/), pinned by [`../SourceRegistry/asl-3.10-a-e.source-registry.json`](../SourceRegistry/asl-3.10-a-e.source-registry.json).
2. The supplied `eASLRB_v3_01.pdf` (SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`), used only as evidence. It is not copied into the repository.

The registered Markdown, the source registry, the TIR and the Scenario A1 verification records are unchanged. Nothing in the existing authoring pipeline reads this edition yet.

**Status:** `draft-uncertified`. No section is certified. The edition is a candidate working text, not a verified source.

## Why a curated edition

Scenario A1 showed that verifying against two imperfect witnesses, the PDF and a raw conversion, costs the same effort each time a fragment is reused. Most of the disagreement between them is mechanical: line-break hyphens, missing page markers, split emphasis. The curated edition fixes those defects once, with evidence, so that later verification can review a clean text section by section instead of reconciling both witnesses fragment by fragment.

The PDF stays the root witness. The edition never becomes an authority on rule meaning.

## Change classes

Every change carries one class. The classes decide what evidence is required.

| Class | Meaning | Evidence required | Applied by |
| --- | --- | --- | --- |
| `conversion-fix` | Undo a defect introduced by PDF-to-Markdown conversion | PDF page and line evidence | Builder, automatically |
| `typographic-correction` | Fix a typesetting defect printed in the PDF itself that cannot change rule meaning | Word-form attestation across the PDF | Builder, automatically, under narrow conditions |
| `markup-normalization` | Change Markdown syntax only; rendered text is identical | None beyond the rule | Builder, automatically |
| `observation` | Records a fact about the PDF; no text change | PDF page | Builder |
| `reviewed-decision` | A queued case resolved by a named reviewer | Reviewer, rationale | `decisions/decisions.json` |
| `source-correction` | A change to rule wording (typo, errata, wrong cross-reference) | A cited authority such as official errata | Not yet supported; the builder rejects it without an authority |

Rule meaning is never edited on judgment alone. A suspected error in the rules text is recorded for review, and it is corrected only when an authority is cited.

## Rules in version 0.1.0

| Rule | Class | What it does |
| --- | --- | --- |
| `page-marker` | conversion-fix | Replaces conversion markers `<!-- page N -->` with `<!-- pdf-page N -->` placed where physical PDF page N begins. A page that starts mid-paragraph gets an inline marker. This restores 47 pages the converter left unmarked; for example A4.14 and A4.15 now sit on page 49, not 48. |
| `linebreak-hyphen` | conversion-fix | For each hyphen the PDF sets at a line end or page end, removes it when the joined form is attested unbroken in the PDF and the hyphenated form is not (or is rare). A real hyphen that falls at a line end stays. Unit ratings (`4-5-7`), acronym compounds and genuine compounds (`Pre-Registered`) stay. |
| `stale-hyphen` | typographic-correction | Removes a hyphen that the PDF prints mid-line inside an ordinary word, such as `condi-tions` on page 103, where the joined word is attested and the suffix is not a word. When the left part is a real prefix (`re-`, `pre-`, `non-` and others), the case is queued instead, because `re-fused` and `refused` are both possible. |
| `split-emphasis` | markup-normalization | Merges `*a* *b*` and `**a** **b**` runs split by the converter at line boundaries. |
| `image-link-rebase` | markup-normalization | Points image links at `../../Rulebook_Markdown/images/`. |
| `symbol-font-glyph` | conversion-fix | Replaces a private-use code point with its Unicode symbol when the PDF shows the glyph set in the expected symbol font. U+F0AB (Wingdings 0xAB, a black five-pointed star) becomes U+2605 `★`, the form the converter already uses elsewhere for the IFT "★ Vehicle line". |
| `blank-page` | observation | Records PDF pages 188, 219 and 252, which carry only the chapter banner and folio. |

The build fails closed when:

- the PDF or any registered Markdown file does not match its pinned digest;
- the edition differs from the base text in anything but hyphens, emphasis markers, whitespace and comments;
- the number of hyphens removed differs from the number of ledgered removals;
- a reviewer decision matches no queued item, or a `source-correction` lacks an authority.

## Current results

| File | Pages | Hyphens joined | Stale hyphens | Emphasis merges | Page markers | Symbols | Reviewed decisions | Open review items |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| TOC | 6-10 | 0 | 0 | 0 | 5 | 0 | 0 | 0 |
| Index and Glossary | 11-42 | 134 | 0 | 8 | 32 | 0 | 1 | 0 |
| Chapter A | 43-111 | 335 | 3 | 265 | 69 | 0 | 19 | 1 |
| Chapter B | 112-161 | 128 | 0 | 126 | 50 | 0 | 13 | 0 |
| Chapter C | 162-191 | 189 | 0 | 248 | 30 | 1 | 5 | 1 |
| Chapter D | 192-221 | 133 | 0 | 92 | 30 | 0 | 3 | 0 |
| Chapter E | 222-253 | 253 | 4 | 122 | 32 | 5 | 8 | 1 |
| **Total** | | **1,172** | **7** | **861** | **248** | **6** | **49** | **3** |

The 49 reviewed decisions are 19 joins, 29 retains and 1 replacement (`ground-and` becomes the suspended hyphen `ground- and`). They were proposed from context and approved by the maintainer on 2026-09-23; each carries its rationale in `decisions/decisions.json`.

The 3 open items are the same convention question: whether `on-board` (A, line 1094) and `Off-board`/`off-board` (C, line 54; E, line 815) keep the hyphen or take the closed form. The PDF uses both forms.

## Layout

| Path | Content |
| --- | --- |
| `edition/` | The curated Markdown, one file per registered source file, same names. |
| `ledger/` | Every change per file: rule, class, base line, before/after, PDF page and evidence. |
| `review/` | Cases left unchanged for a reviewer, with context and evidence. |
| `decisions/decisions.json` | Reviewer decisions that resolve review items (`join`, `retain` or `replace`). |
| `manifest.json` | Digests of the PDF, the base registry, each base file, each edition file and the builder, plus per-file counts and the (empty) certified-section list. |
| `.gitattributes` | Forces LF so checked-out bytes match the manifest digests. |

The builder is [`utils/asl_curated_edition/build_curated_edition.py`](../../../utils/asl_curated_edition/build_curated_edition.py).

## Regeneration

From the repository root, with PyMuPDF installed:

```bash
python utils/asl_curated_edition/build_curated_edition.py --pdf "<path>/eASLRB_v3_01.pdf"
```

The output is byte-for-byte deterministic for the same PDF, base files, decisions and builder.

## Resolving review items

Add an entry to `decisions/decisions.json` and rebuild:

```json
{
  "sourceId": "asl-easlrb-3.10:chapter-a",
  "rule": "linebreak-hyphen",
  "baseLine": 1094,
  "text": "on-board",
  "decision": "retain",
  "decidedBy": "<reviewer>",
  "rationale": "Both forms are used in the rulebook; the printed line-end hyphen is part of the compound here."
}
```

`baseLine` and `text` come from the review item. For a token with more than one hyphen, a `join` also needs `hyphenIndex` (0-based). A `replace` needs `replacement`, and the replacement may differ from the base only in hyphens, emphasis markers and whitespace; the build fails otherwise. Decisions are keyed to the pinned base text, so they stay valid across rebuilds.

## Not yet addressed

These defect classes are known and are left for later rules or review:

- **Charts and tables** in ```` ```text ```` blocks are protected from every rule. Their fidelity to the PDF (column alignment, soft hyphens, symbols) has not been checked.
- **Duplicate headings**: the converter emits an outline heading such as `###### 4.152 CC` directly before the bold rule text `**4.152 CC:**`. They are kept for now; removing them is a structural choice that affects heading-based navigation.
- **Symbol consistency**: the triangle symbol appears as both U+2206 and U+0394; dash usage mixes en and em dashes where the PDF does.
- **Residual hyphens**: a small number of line-break hyphens the PDF word stream did not expose (for example `in-volves`) remain. A residual audit against the PDF is the next rule to add.
- **Back matter**: charts outside pages 6-253, such as the B Terrain Chart on page 698, are outside this edition's scope, as they are for the source registry.

## Certification

A section becomes usable as source text when a reviewer compares it with the PDF pages that its `pdf-page` markers name, clears its review items, and records it in `certifiedSections` in the manifest. Certification covers text fidelity only. It does not establish rule meaning, dependency closure or semantic correctness, which remain with the ASL-OT review workflow.

## Distribution

The edition is a derivative of copyrighted material, like the registered Markdown it is built from. It carries the same distribution controls as the source registry: repository-controlled access and redistribution governed by the source license.
