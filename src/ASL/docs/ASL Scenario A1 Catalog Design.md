# ASL Scenario A1 Catalog Design

**Status:** Built with synthetic definitions; the counter-sheet transcription is awaiting its transcriber

**Date:** 2026-09-25

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 3: ASL-UNIT-010 to 014, 062, 080, and 081, under decisions D1 and D4 (section 12).

**Related documents:** the [Decision Memo for D1 to D4](<ASL Unit Model Decisions D1 to D4.md>); the [Ontology Transformation Specification](<LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>) for source registration, review, and publication; the [ASL Unit Domain Model Analysis](<ASL Unit Domain Model Analysis.md>) for vocabulary; and the [ASL Unit Display Design](<ASL Unit Display Design.md>), sections 3 and 16 to 19, for the display vocabulary a definition must produce.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`. They were checked against that PDF and the page markers in `docs/ASL/Rulebook_Markdown`. Where the two differ, the PDF page is given: A10.7 is on physical page 68, while its conversion marker says 67.

## 1. Outcome

The catalog holds reference definitions: the printed values of particular counters, each value typed and traced to the transcription row it was read from. It is versioned, so every instance of the state model (step 4) can record the exact catalog it was created against. It covers only what the reviewed Scenario A1 cases need (ASL-UNIT-062, D4).

This step delivers:

- the definition model, catalog reader and validator, version identity, lookup, and conversion to unit documents, in `LimboDancer.Domains.Asl.Units`;
- the D1 source registration: a counter-sheet source record and a transcription format;
- a source adapter, `LimboDancer.Domains.Asl.Units.CounterSheets`, that builds the catalog from the transcription;
- a transcription worksheet listing the counters and faces to transcribe;
- cross-check hooks against the existing Scenario A1 snapshots, which stay unchanged;
- a synthetic catalog, clearly labelled, for tests until the reviewed transcription exists.

No printed counter value is in the repository yet. The values come from the transcriber (section 9).

## 2. What the Scenario A1 cases read

The Scenario A1 packages were read for every unit fact their snapshots and observation providers use. The finding shapes the whole catalog: **the reviewed cases read kinds of unit and their conditions, never a printed value.** No case reads a nationality, a strength factor, a morale level, or a class. Movement allowances (A4.1, p. 48) come from rules, not counters, and NTC results are supplied as facts.

| Case | Unit facts the snapshot states | Kind the fact needs |
|---|---|---|
| Empty building entry | `IsGoodOrderInfantrySquad` | squad |
| Occupied building entry | `KnownUnconcealedEnemyMmc`; moving `ordinaryInfantry` | enemy MMC; Personnel |
| Fortified building entry | `KnownUnpinnedGoodOrderArmedEnemySquad` | enemy squad |
| Infantry OVR against a known SMC | `IsGoodOrderInfantryMmc`, `ExactlyOneKnownEnemySmc`, `IsSmcOutsideAfv` | MMC; enemy SMC |
| Advance into a fortified or ordinary building | `IsGoodOrderUnpinnedInfantry`, `IsGoodOrderUnpinnedInfantryWithoutTiOrCc`, `ExactlyOneKnownUnconcealedEnemyMmc` | Personnel; enemy MMC or squad |
| Stacking | `FriendlySquads`, `FriendlyUnmannedCrewsOrHalfSquads`, `FriendlySmc`, `IncomingSquads` | squad; half-squad or crew |
| Concealed SMC revealed, OVR | `IsGoodOrderUnconcealedNonDummyInfantryMmc`, `RevealedOccupant.EnemySmc` | MMC; enemy SMC |
| Second defender | `SecondDefenderState.RevealedSmc` or `RevealedMmc`, `AdditionalDefenderType` | enemy SMC or MMC |
| Post-reveal entry | `IsOrdinaryInfantry` | Personnel |

The vocabulary has no separate Infantry kind. Within this slice, Infantry is satisfied by the Personnel kinds squad, half-squad, and leader (A1.1, p. 44); Cavalry and other Personnel are outside it.

So the catalog is organised by **slots**: roles the cases need a definition for, each with the kind the snapshot fact names. Four counters fill the nine slots:

| Counter | Slots |
|---|---|
| `attacker-squad` | `a1-moving-squad`, `a1-moving-mmc`, `a1-moving-infantry` |
| `attacker-half-squad` | `a1-friendly-half-squad` |
| `defender-squad` | `a1-defending-squad`, `a1-defending-mmc`, `a1-second-defender-mmc` |
| `defender-leader` | `a1-defending-smc`, `a1-second-defender-smc` |

Because the cases do not depend on which squad or leader it is, the transcriber chooses the nationalities and counters. The catalog records the whole printed face of each (section 4), so a definition can produce a unit document with information parity (ASL-UNIT-075), even though the cases themselves read only the kind.

## 3. Definitions and keys

A **definition** (`UnitDefinition`) is the printed content of one counter: its kind, nationality, printed class, counter reference, applicability, the slots it fills, and its printed values. It is reference data, not game state (ASL-UNIT-010).

A definition's **key** (`DefinitionKey`) is more than the printed strength triple (ASL-UNIT-011): nationality, kind, class, the counter (source, sheet, and counter id), and the applicability dates. Two counters with the same front values but a different nationality, class, broken face, or applicability are different definitions. Face is part of every value's key: a value is identified by its definition, its face, and its attribute.

The kind and nationality are themselves transcribed rows (the silhouette count and the counter's nationality), so they carry a source like every value. The key's class must equal the printed class value, and a leader, which prints no class, has none.

**Applicability** (ASL-UNIT-014) records a date range as `YYYY-MM`, modules, SSR or other conditions, and the basis for them. The counter sheets do not give dates; national dates and substitutions come from A25 and Chapter H, whose charts (the A./G. National Capabilities Chart, among the back-matter candidates on physical pages 694 to 697 in the [back-matter boundary review](<LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>)) are not registered. The first catalog therefore records applicability as `unreviewed`, with no dates. Substitution mappings (A19, A25) wait for the same source.

**Lookup** (acceptance scenario U1) takes a nationality, an exact kind, a class, and an optional date, and returns one of:

| Status | Meaning |
|---|---|
| `Found` | Exactly one definition matches, and the date, if given, is inside its reviewed applicability. |
| `NotInCatalog` | No definition has this nationality, kind, and class. A kind above the definition's (`asl:mmc` for a squad) is not a match. |
| `OutsideApplicability` | Definitions match, but the date is outside every reviewed range. |
| `ApplicabilityUnreviewed` | Definitions match, but their applicability is unreviewed, so the date cannot be checked. The candidates are returned, marked nondefinitive. |
| `Ambiguous` | More than one definition matches; the caller must name the counter. |

Only `Found` is definitive. No status returns the nearest match.

## 4. Printed values and their sources

Each printed characteristic is a `PrintedValue` (ASL-UNIT-011): the face it is printed on, the vocabulary attribute or trait, a typed value, and a `ValueSource` naming the counter and the transcription row. Values use the `asl` vocabulary's types (`integer`, `rating`, `enumeration`, `text`, `list`), so the catalog and the display agree on what a value is.

A value is one of three things:

- an attribute with its printed value, such as firepower 4;
- an attribute the transcriber checked and found not printed (`printed: false`), such as a class variant on a counter without one; this is a recorded fact, distinct from a value nobody has looked at;
- a trait marking, present or absent, such as the underlined firepower of `asl:assault-fire` (A1.21, p. 44).

A value belongs on a face the kind has, and on the face the vocabulary allows (the broken morale level on the broken face, A1.4, p. 45). An attribute the vocabulary scopes to the whole unit, such as the smoke exponent, is recorded on the face it is printed on and shown on the unit.

## 5. Printed and effective values

Printed values never change (ASL-UNIT-013). An `EffectiveValue` starts from a printed number and adds `ValueAdjustment`s, each naming its rule and reason, so the printed value is kept and the result can be explained. No rule produces adjustments in this step; terrain, leadership, condition, date, SSR, captured use, and attack mode arrive with the reviewed transitions of later steps. A unit document made from a definition shows printed values only.

## 6. The catalog file

A catalog is one JSON file, read by `UnitCatalogReader` and written in one canonical form by `UnitCatalogJson` (two-space indentation, LF endings, fixed field order, short attribute names where unambiguous):

```json
{
  "schemaVersion": 1,
  "catalog": "asl-scenario-a1",
  "version": "1.0.0",
  "label": "...",
  "publication": "published",
  "vocabulary": ["asl@1.3.0"],
  "sources": [
    { "id": "asl-counter-sheets:scenario-a1", "status": "reviewed", "record": "...", "recordSha256": "...",
      "transcription": "...", "transcriptionSha256": "...", "transcriber": "...", "reviewer": "..." }
  ],
  "slots": [ { "id": "a1-moving-squad", "kind": "asl:squad", "label": "..." } ],
  "definitions": [
    {
      "id": "attacker-squad", "kind": "asl:squad", "kindRow": 2, "nationality": "...", "nationalityRow": 3, "class": "...",
      "counter": { "source": "asl-counter-sheets:scenario-a1", "sheet": "...", "counter": "attacker-squad" },
      "applicability": { "status": "unreviewed", "modules": [], "conditions": [] },
      "slots": ["a1-moving-squad", "a1-moving-mmc", "a1-moving-infantry"],
      "values": [
        { "face": "front", "attribute": "firepower", "value": 0, "row": 4 },
        { "face": "front", "attribute": "class-variant", "printed": false, "row": 9 },
        { "face": "front", "trait": "asl:assault-fire", "present": false, "row": 11 }
      ]
    }
  ]
}
```

The values in this example are placeholders, not counter data.

**Publication** says what the catalog may be used for:

| Publication | Sources allowed | Use |
|---|---|---|
| `synthetic` | only `synthetic` sources | Tests and fixtures. Never counter data. |
| `draft` | transcribed sources, reviewed or not | Checking a transcription before review. Every slot must be filled. |
| `published` | only reviewed sources, each reviewed by someone other than its transcriber | The catalog of record (ASL-UNIT-012). |

The second-person rule follows D1's "transcribed and reviewed" and the memo's "a second review". A catalog with any error is refused whole, because it is used as one versioned unit.

| Code | Refused when |
|---|---|
| UNIT-CAT-001 | The file is not JSON, or a field has the wrong JSON type. |
| UNIT-CAT-002 | The schema version is not 1. |
| UNIT-CAT-003 | The catalog id is not a slug, the version is not `major.minor.patch`, or the publication is unknown. |
| UNIT-CAT-004 | No vocabulary is recorded, or a recorded pack is not loaded. |
| UNIT-CAT-005 | A source is declared twice, has an unknown status, or, when registered, lacks its record, transcription, transcriber, or lowercase SHA-256 pins; or it is reviewed without a reviewer. |
| UNIT-CAT-006 | A slot id is not a unique slug, or its kind is undeclared. |
| UNIT-CAT-007 | A definition id is not a slug or is used twice. |
| UNIT-CAT-008 | A definition's kind or nationality is undeclared. |
| UNIT-CAT-009 | The key's class is not a class member, or differs from the printed class. |
| UNIT-CAT-010 | A counter names an undeclared source. |
| UNIT-CAT-011 | A value is on a face the kind lacks or the attribute or trait does not allow, names an attribute or trait the kind does not accept, has the wrong type, is recorded twice, or has no row; or one transcription row backs two values. |
| UNIT-CAT-012 | A pinned source hash differs from the bytes checked (`VerifySource`). |
| UNIT-CAT-013 | A definition names an undeclared slot, or a slot whose kind it is not. |
| UNIT-CAT-014 | Two definitions have the same key. |
| UNIT-CAT-015 | Applicability is missing, has a bad or reversed date, has dates while unreviewed, or is reviewed without a basis. |
| UNIT-CAT-016 | The publication rules above are broken, or a slot is unfilled. |
| UNIT-CAT-017 | (Warning) A field is not a catalog field. |

## 7. Version identity

A catalog's identity (ASL-UNIT-080) is its id, its version, and the SHA-256 of its canonical form: `asl-scenario-a1@1.0.0+sha256:...`. The canonical form holds every definition and the pinned hashes of the source record and the transcription, so:

- changing any value, key, slot, or applicability changes the hash;
- changing the transcription or the source record changes its pinned hash, and so the catalog's hash;
- reformatting the file does not change the hash.

`VerifySource` checks the pinned hashes against the committed files, with CRLF read as LF so a Windows checkout agrees with Linux CI. A `DefinitionReference` (`identity#definition`) is what an instance or a unit document records. The declared version follows the Ontology Transformation Specification, section 12: added definitions are a minor version, corrected values a new version with a compatibility review.

## 8. Definitions and the display vocabulary

Definitions use the display's vocabulary (the `asl` pack, [Display Design](<ASL Unit Display Design.md>), section 3.1) for kinds, faces, attributes, and traits, so a definition can become a unit document without translation. `CatalogDocuments.ToDocument` makes one: the nationality becomes the side, printed values go on the face they are printed on (or on the unit for unit-scoped attributes), and present traits become the face's traits. Values not printed and absent traits are left out. The document is read back through `UnitDocumentReader`, so it is always valid against the vocabulary, and it is returned with its `DefinitionReference`. It carries no states: conditions are game state (step 4), and the document is a projection (ASL-UNIT-070, 074).

The Unit Display Design's section 4.1 table is the list of what a Personnel counter prints; the worksheet asks for each of those facts that a transcriber can read from the counter. The Unit Size Number is not transcribed: it follows from the kind (A1.6, p. 45) and is the vocabulary's size class.

## 9. Source registration for D1

D1 made the published counter sheets, transcribed and reviewed, the source of record. The registration follows the Ontology Transformation Specification's stages: register the source (stage 1), review it (stage 7), and publish (stage 8), with the provenance and review gates of sections 10.1 and 10.5. The files live in `docs/ASL/SourceRegistry/CounterSheets/`:

| File | Role |
|---|---|
| `asl-counter-sheets.scenario-a1.source-record.json` | The source record: the sheets used, the transcription path and SHA-256, the transcriber and date, the reviewer and date, the status, and the distribution terms. |
| `scenario-a1.counter-worksheet.csv` | The worksheet: one row for each value to transcribe, with no values. |
| `scenario-a1.counter-transcription.csv` | The transcription, once written: the worksheet with every value filled in. |
| `README.md` | How to fill in the worksheet. |

**Transcription format** (`asl-counter-transcription/1`): CSV in UTF-8, one row per counter face value, with the header `sheet,counter,face,attribute,value,transcriber,reviewer,note`.

- `sheet` is an id from the source record's `sheets` list, where each sheet is identified by title, publisher, product, and printing.
- `counter` is the worksheet's counter id; `face` is `front`, `broken`, or `counter` for the kind and nationality.
- `attribute` is a vocabulary attribute or trait name; `value` is what is printed: a number, a signed number for leadership, a class member name, `yes` or `no` for a trait, or `not-printed`.
- `transcriber` and `reviewer` name the people; the row number is the line number in the file.

**Lifecycle.** The record's status moves from `awaiting-transcription` to `transcribed-unreviewed` when the transcription is committed with its hash and transcriber, and to `reviewed` when a second person has checked every row against the sheets and signed each row. The adapter builds a draft catalog from a transcribed source and a published one from a reviewed source; each state is committed, and a test rebuilds the catalog from the committed sources and compares it byte for byte.

**Distribution.** Printed values are recorded as facts. No counter artwork, scans, or photographs are committed, and the sheets are not copied (ASL-UNIT-072). Counter artwork is never evidence of a value (ASL-UNIT-012).

**VASL.** VASL piece definitions are not used. D1 allows them only as a local, uncommitted cross-check after a licensing review, which is not part of this step.

## 10. The source adapter

ASL-UNIT-001 keeps source-specific parsing out of `LimboDancer.Domains.Asl.Units`, so the transcription is read by a separate source adapter, `LimboDancer.Domains.Asl.Units.CounterSheets`, which references only `Units`. `CounterSheetCatalogBuilder` takes the catalog manifest (`src/ASL/units/catalog/scenario-a1.catalog-manifest.json`: id, version, slots, and which counter fills which slot), the source record, the transcription, and the worksheet, and:

1. checks the record's status and that its pinned hash matches the transcription;
2. checks that every worksheet row is transcribed, every value is filled in, every sheet is declared, and every row names the recorded transcriber and, once reviewed, the recorded reviewer;
3. makes one definition per counter, with its kind, nationality, and class from the rows, and each other row as one typed value with its row number;
4. validates the result with `UnitCatalogReader` and writes it in canonical form.

Its diagnostics are UNIT-CS-001 to 006 for the transcription, 010 and 011 for the source record, and 020 to 022 for the manifest and values. The published catalog is committed as `src/ASL/units/catalog/scenario-a1.catalog.json` and embedded in `Units` beside the synthetic one; `UnitCatalogs` reads either.

## 11. Cross-check hooks

The Scenario A1 snapshots stay unchanged (ASL-MAP-081). `ScenarioA1CatalogCrossCheckTests`, in the Scenario A1 test project, binds each snapshot fact that names a kind of unit to its slot with `nameof`, so renaming or removing the fact breaks the build. For every embedded catalog, synthetic and published, it checks that:

- each such fact has a slot of the kind it names, filled by definitions of that kind or below;
- every slot stands for at least one snapshot fact;
- the kinds of a stack of two squads and a half-squad, with a squad entering, give exactly the facts the existing `ScenarioA1StackingCost` reviews (A5.1, p. 52; A5.5, p. 53), with the Unit Size Numbers of A1.6 (p. 45).

The conditions in those facts (Good Order, unpinned, concealed, armed) are state. They are cross-checked when the state model and read contract produce them (steps 4 and 5).

## 12. Tests

- `Units.Tests/CatalogTests`: the synthetic catalog reads; canonical writing keeps the identity; the identity changes with a value or a source; values are typed and sourced; effective values leave printed ones alone; U1 lookups, including explicit misses; each definition becomes a valid unit document that round-trips through the display's writer and reader; pinned sources are verified; publication rules; and each refusal code.
- `Units.CounterSheets.Tests`: building from synthetic fills of the committed worksheet (draft, published, second-person review, identity change on review); each transcription and record refusal; and the committed files agree (the worksheet has no values and asks for exactly the manifest's counters, the synthetic catalog has the manifest's slots, and the record, transcription, and committed catalog match or are all absent).
- `ScenarioA1.Tests/ScenarioA1CatalogCrossCheckTests`: section 11.

Synthetic definitions use deliberately low values and invented dates (1901), and are labelled synthetic wherever they appear.

## 13. Not in this step

- Printed values for real counters: awaiting the transcription and its review (section 9).
- Reviewed applicability, national dates, and substitution mappings: they need a registered source for A25 and Chapter H charts.
- Effective values produced by rules: later steps, one reviewed transition at a time.
- Kinds beyond the four counters: added one reviewed slice at a time (ASL-UNIT-062).
- VASL cross-check: needs its licensing review first (D1).
- Instances recording the catalog version: the state model, step 4, uses `DefinitionReference`.
