# ASL Unit Model: Decision Memo for D1 to D4

**Status:** Decided on 2026-09-25: all four recommendations were accepted and are recorded in the [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 12.

**Date:** 2026-09-25

**Baseline:** `main@11ad2c0`, with all four phases of the [ASL Unit Display Design](<ASL Unit Display Design.md>) built

## Purpose

The unit display is built; the next step in the Unit Requirements sequence (section 13, step 2) is to settle decisions D1 to D4 and register the chosen counter source. Nothing after step 2 can start without them: the Scenario A1 catalog needs D1 and D4, the state model needs D3, and governed writes need D2.

This memo recommended an answer for each. D3 and D4 already carried proposed answers in the requirements. D1 is the decision that gates the next piece of work.

## Recommendations at a glance

| Decision | Recommendation | Unblocks |
|---|---|---|
| D1. Counter data source | Transcribe the needed counters from the published counter sheets and review them; use VASL piece definitions only as a local cross-check, after a licensing review | The Scenario A1 catalog (step 3), ASL-UNIT-012 counter data |
| D2. Live game source | Keep deferred; when needed, start with a Map Studio setup and play editor, with a VASL saved-game import later | Reading a real game (ASL-UNIT-050, 060); governed writes (step 7) |
| D3. Perspective set | Each side plus an adjudicator, with named perspectives | Visibility (ASL-UNIT-030); the state model (step 4) |
| D4. First slice boundary | The Scenario A1 case list | The first catalog (ASL-UNIT-062) |

## D1. Counter data source

Recommendation: transcribe the counters the first slice needs from the published counter sheets, register that transcription as a source, and review it; treat VASL piece definitions as a local cross-check only, and only after a licensing review.

The rulebook gives counter anatomy, which the display already uses, but not the printed values of particular units (ASL-UNIT-012). Whatever source is chosen must be registered and reviewed under the Ontology Transformation Specification before any definition is published.

| Option | Provenance | Licensing | Effort | Coverage |
|---|---|---|---|---|
| Published counter sheets, transcribed and reviewed | Printed counters, cited per counter | Printed values are facts; artwork is never copied (ASL-UNIT-072) | Manual transcription and a second review; small for the Scenario A1 slice | Only what is transcribed |
| VASL module piece definitions | Community module files, not yet examined | Unreviewed; would need the same boundary the map work applies to VASL boards | Low once a reader exists | Broad |
| A mix | Transcription is the source of record; VASL is compared against it | VASL stays local and uncommitted, like the board checkout | Moderate | Transcribed slice, cross-checked |

The mix mirrors how the map work already treats VASL: a locally configured checkout that is read and compared, never committed. It gives the catalog a reviewed source of record without making the project depend on terms not yet examined.

Open question: which counter sheets are to hand for the Scenario A1 nationalities.

## D2. Live game source

Recommendation: keep D2 deferred until step 7; when it is needed, start with a Map Studio setup and play editor, and add a VASL saved-game import later as a read-only source.

D2 blocks only reading a real game and governed writes. Steps 3 to 6 run on synthetic fixtures and the catalog, and the display already takes its input from the Unit Lab and fixtures, labelled synthetic.

| Option | Strengths | Costs and risks |
|---|---|---|
| Map Studio setup and play editor | Builds on the Unit Lab and placement sets; keeps every change inside LimboDancer's governed path | Players must enter positions by hand |
| VASL saved-game adapter | Reads games people already play | Format and licensing not yet examined; read-only at first |
| LimboDancer engine | One source of truth for state | The largest build; depends on the state model and governed writes being mature |

The editor comes first because it reuses what exists and cannot bypass the Execution Gate. The VASL import follows the same local, uncommitted pattern as the board checkout.

## D3. Perspective set

Recommendation: accept the proposal of each side plus an adjudicator. Perspectives are named, so adding one later stays a reviewed change (ASL-UNIT-030) that needs no change to the model.

Concealment and hidden placement (A12, pp. 76 to 80) mean each side knows less than the game does, and the Scenario A1 packages reason as an adjudicator who sees everything. The display already expects this split: a unit concealed from its viewer arrives as a placeholder, while its owner sees the full unit.

| Option | Covers | Cost |
|---|---|---|
| Each side plus adjudicator | Two-sided play, concealment, adjudication | Smallest; matches existing packages |
| Wider (observers, multi-player sides, delayed replays) | Spectators and teams | More projections to test now, for needs not yet stated |

## D4. First slice boundary

Recommendation: accept the proposal and bound the first catalog and state model by the Scenario A1 case list.

ASL-UNIT-062 already scopes the first slice to what the reviewed Scenario A1 cases read: Infantry Personnel (squads, half-squads, leaders) entering a building location, concealment and reveal, Infantry OVR, fortified building entry, and a second defender. Those cases have reviewed snapshots, so the new model can be cross-checked against known answers (ASL-MAP-081) instead of being judged on its own.

| Option | Scope | Verification |
|---|---|---|
| Scenario A1 case list | The units and conditions the reviewed cases read | Against the existing Scenario A1 snapshots |
| Wider Infantry slice | All Infantry Personnel and their conditions | No reviewed cases to compare against yet |

A wider slice can follow one reviewed slice at a time, as ASL-UNIT-062 requires.

## Next steps

- [x] Record the decisions in the Unit Requirements (section 12), with the date.
- [ ] Register the counter source under the Ontology Transformation Specification: the counter sheets used, the transcription, and its reviewer. The source record, format, and worksheet are in place ([Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>), section 9); the sheets, transcription, and reviewer are awaited.
- [ ] If VASL is to be the cross-check, run the licensing review of the module's piece definitions first.
- [ ] Transcribe and review the Scenario A1 Infantry counters, then start step 3: the Scenario A1 catalog, with round-trip tests.
- [ ] Name the perspectives (each side, adjudicator) in the state model design for step 4.
