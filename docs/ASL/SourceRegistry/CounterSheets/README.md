# Counter-sheet source for the Scenario A1 catalog

This folder registers the published counter sheets as the source of printed counter values, as decided in D1 ([ASL Unit Requirements](<../../../../src/ASL/docs/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 12). The [Scenario A1 Catalog Design](<../../../../src/ASL/docs/ASL Scenario A1 Catalog Design.md>), section 9, describes the format and lifecycle.

| File | Content |
|---|---|
| `asl-counter-sheets.scenario-a1.source-record.json` | The source record. Status: `reviewed`. |
| `scenario-a1.counter-worksheet.csv` | The worksheet: 57 rows for four counters, with no values. |
| `scenario-a1.counter-transcription.csv` | The worksheet with its values filled in, by Claude on 2026-09-25 from the registered rulebook's charts (see below). |

## First transcription

At the user's request, Claude filled in the worksheet from the A./G. National Capabilities Chart (physical PDF page 695) and the A18.2 Leader Creation Table (page 694), not from physical counter sheets: a German 1st Line 4-6-7 squad and 2-4-7 half-squad attack a Russian 1st Line 4-4-7 squad (class 1 in a square) and an 8-0 leader. Values the charts do not show are `not-in-source`. The [Scenario A1 Catalog Design](<../../../../src/ASL/docs/ASL Scenario A1 Catalog Design.md>), section 9.1, gives the details. Dennis Landi reviewed all 57 rows on 2026-09-25 and confirmed them; the published catalog is `src/ASL/units/catalog/scenario-a1.catalog.json`. Where a physical counter differs from the chart, the counter governs under D1.

No counter artwork, scans, or photographs belong here. VASL piece definitions are not used (D1).

## Transcription worksheet

The Scenario A1 cases read the kind of each unit (squad, half-squad, MMC, SMC), never a printed value, so any counter of the right kind serves. Choose one attacking nationality and one defending nationality, then one counter for each row below, from sheets you have to hand.

| Counter id | Kind | Side | Stands for |
|---|---|---|---|
| `attacker-squad` | squad (`asl:squad`) | attacker | the moving squad, MMC, and Infantry of every case |
| `attacker-half-squad` | half-squad (`asl:half-squad`) | attacker | the friendly half-squad in the stacking case |
| `defender-squad` | squad (`asl:squad`) | defender | the enemy squad and MMC, and a second defending MMC |
| `defender-leader` | leader (`asl:leader`) | defender | the enemy SMC, concealed or known, and a second defending SMC |

A Japanese squad or leader has no broken side (A1.4, p. 45); if you choose one, write `not-printed` for its broken-face rows.

### Sheets

List each sheet you use. The id is yours to choose (for example `S1`); it goes in the `sheet` column of every row read from that sheet.

| Sheet id | Title as printed | Publisher | Product (module) | Printing or edition |
|---|---|---|---|---|
| | | | | |

### Rows

Copy `scenario-a1.counter-worksheet.csv` to `scenario-a1.counter-transcription.csv` and, in every row:

- `sheet`: the sheet id from the table above;
- `value`: what the counter prints, in the form the `note` column asks for:
  - a whole number for firepower, range, morale, broken morale, BPV, and smoke exponent;
  - a signed number for leadership, such as `-1`, `0`, or `+1`;
  - `elite`, `1st-line`, `2nd-line`, `green`, or `conscript` for class, and `circle` or `square` for a class variant;
  - `yes` or `no` for a marking such as an underline or a square;
  - `not-printed` when the counter does not print it;
  - for `kind`, the kind in the table above, and for `nationality`, one of `american`, `british`, `finnish`, `french`, `german`, `italian`, `japanese`, or `russian`;
- leave `transcriber` and `reviewer` empty; they are filled in when the transcription is registered and reviewed;
- `note`: keep the guidance or replace it with a remark of your own.

Read each value from the counter itself. If something printed on the counter has no row, add a row for it with a note; if a row does not apply, write `not-printed` and say why in the note.

You may instead reply with the values in any clear form, and they will be entered for you exactly as given.

## After the transcription

1. The transcription is committed, its SHA-256 and transcriber are recorded, and the record's status becomes `transcribed-unreviewed`. A draft catalog is built from it.
2. A second person checks every row against the sheets. Their name goes in each row's `reviewer` column and in the record, and the status becomes `reviewed`.
3. The published catalog is built, committed as `src/ASL/units/catalog/scenario-a1.catalog.json`, and embedded in `LimboDancer.Domains.Asl.Units`.
