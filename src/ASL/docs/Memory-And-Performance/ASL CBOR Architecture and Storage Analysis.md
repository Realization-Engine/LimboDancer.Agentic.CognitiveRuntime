# ASL CBOR Architecture and Storage Analysis

**Date:** 2026-10-21  
**Status:** Analysis and proposed experiments. No storage format or persistence behavior has been changed.

CBOR is a candidate encoding for rebuildable map caches and, later, event batches in a redesigned game store. It does not by itself resolve the storage backlog: whole-game rewrites, games depending on mutable scenario cards, and full-record reads for the game-to-card index need changes to storage behavior. Keep authoritative JSON and existing identities while measuring whether a binary encoding adds enough value to justify its maintenance.

This analysis applies to the current ASL implementation on `play-streamlining`. It relates Andrew Lock's [CBOR article](https://andrewlock.net/an-introduction-to-compact-binary-object-notation-in-dotnet/) to the [ASL Unit Backlog](<../ASL Unit Backlog.md>), particularly sections 42 and 65, and the [ASL Memory and Performance Guidelines](<ASL Memory and Performance Guidelines.md>).

## What CBOR provides

Concise Binary Object Representation is a standard binary data format with integers, strings, arrays, maps, byte strings, and other types. Numeric and binary payloads are plausible candidates for smaller encodings; text remains text, and compression can change the comparison substantially. A self-describing encoding still needs an application schema that defines field meanings, valid values, required fields, and compatibility. [RFC 8949](https://www.rfc-editor.org/rfc/rfc8949.html)

The article demonstrates explicit encoding and decoding with `System.Formats.Cbor`. These APIs require codec code rather than offering a direct replacement for `JsonSerializer.Serialize<T>`. The reader accepts a memory buffer, and the writer builds encoded output in a buffer. Do not infer bounded-memory file streaming merely from CBOR's support for indefinite-length values. [Microsoft CborReader documentation](https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborreader?view=aspnetcore-10.0), [CborWriter source](https://source.dot.net/System.Formats.Cbor/System/Formats/Cbor/Writer/CborWriter.cs.html)

Two details qualify the article for implementation work. Microsoft's package predates .NET 10, as the [5.0.0 package](https://www.nuget.org/packages/System.Formats.Cbor/5.0.0) demonstrates; that historical version is not a recommendation for use. Also, documentation can expose newer preview APIs. A prototype must select a supported package compatible with our target and verify its actual constructors and options.

## Existing architecture boundaries

| Boundary | Current implementation | Consequence for CBOR |
|---|---|---|
| Authored board identity | `BoardPackage.Version` hashes canonical defining manifest and feature content. `board.json` records the feature hash. | Re-encoding defining data changes identity. Preserve JSON-derived board versions and put caches outside defining entries. |
| Board loading | `BoardPackageStore.Load` reads and hashes source files, verifies the manifest, and reuses parsed results when hashes match. | A cache must preserve source-change detection and validation. Bypassing these reads is a separate storage decision. |
| Game persistence | `FileGameStore` stores one JSON record per tenant and game, replacing the complete file on commit. | Encoding changes alone leave whole-history I/O and validation work in place. |
| Incremental replay | The game store retains previously read event objects when appending. | A new decoder must not defeat replay reuse by recreating all historical objects on every read. |
| Scenario card identity | `ScenarioCardLibrary` hashes UTF-8 card text with LF line endings. | Retaining the card requires retaining its identity-defining representation, not just an equivalent object. |
| Drawing | `RenderCache` retains UTF-8 SVG, gzip output, and an ETag; counter updates reuse fragments. | Wrapping SVG in CBOR offers little reason to change this path. |
| Planner caches | State-associated memoization retains map context and derived answers. | Serialized size does not determine the retained size of the reconstructed object graph or the cost of rules searches. |

Source: [BoardPackage.cs](../../LimboDancer.Domains.Asl.Maps/Features/BoardPackage.cs), [FeatureModelJson.cs](../../LimboDancer.Domains.Asl.Maps/Features/FeatureModelJson.cs), [GameStore.cs](../../LimboDancer.Domains.Asl.Play/GameStore.cs), [ScenarioCardLibrary.cs](../../LimboDancer.Domains.Asl.Play/ScenarioCardLibrary.cs), [RenderEndpoints.cs](../../LimboDancer.Domains.Asl.MapStudio/Services/RenderEndpoints.cs), and the performance guidelines.

## Applicability to the storage backlog

The references below are backlog section numbers, not newly assigned issue IDs. Historical timings are evidence from the cited runs, not new CBOR measurements.

| Backlog item | What CBOR could contribute | What actually resolves the need | Recommendation |
|---|---|---|---|
| Section 65: whole-game rewrite on every append, historically about 48 ms at a 1.2 MB game | Potentially fewer bytes and less codec work | A storage design that writes committed batches without rewriting prior history, while preserving validation and consistency | Design storage first; compare JSON and CBOR inside the same design. |
| Section 42: continue a game after its card changes or is deleted | A container for retained card content | An immutable card copy or version store, linked to the game and its existing card digest | Start with the exact normalized JSON text. Encoding is secondary to retention and lookup policy. |
| Section 42: game-to-card index reads every game record | Smaller records could reduce scanning cost | A bounded metadata read or maintained, rebuildable index | Change the access pattern; do not use a smaller full scan as the end design. |
| Section 65: one append took 23 seconds in a traced run | No established contribution | Diagnose file contention or other I/O delay and establish reproducibility | Keep this separate from a codec experiment. |
| Section 46: setup draft is lost on page reload | A possible draft encoding | Draft persistence scoped to game and viewer, with isolation from the other side | Solve draft lifetime and disclosure first. The payload is unlikely to justify a dedicated codec. |
| Section 46: setup-plan file does not follow its card on rename or delete | No material contribution | Coordinated file lifecycle and explicit reference behavior | Keep out of the CBOR scope. |
| Section 49: persistent notes on a replay step | A possible annotation encoding | A separate annotation store linked to game and step, with appropriate access rules | Keep annotations outside authoritative events; choose encoding only after the annotation contract. |
| Section 49: replay exported as a file or film | A possible binary export container | Export scope, disclosure policy, required dependencies, and a portable versioned contract | Consider only after those requirements are settled. CBOR does not produce a film. |

The [Play Step Analysis](<ASL Map Studio Play Step Analysis.md>), sections 2.3 and 4.J, records the whole-file cost. Its store timing includes append and read work; it does not isolate JSON parsing as the cause of the full 48 ms. The 23-second outlier in section 8.2 did not recur in the untraced run. Neither observation establishes a CBOR speedup.

### Whole-game writes

`FileGameStore.Commit` checks the attempt and expected revision, constructs the new batch, replays the combined history, serializes the entire record, reads it back for validation, writes a temporary file, and replaces the old file. Its parsed-record cache avoids some decoding, but reads still load and hash the file. `GameEventWriter` writes indented UTF-8 JSON into a buffer, converts it to a string, and the store converts that string back to UTF-8 bytes. These are distinct costs.

A smaller codec can reduce constants while leaving work proportional to history length. Even an append-only file will not make the whole commit proportional to the new batch if the implementation still reads or validates all historical events. Storage I/O, encoding, and replay must be measured separately.

Before adopting CBOR, include direct UTF-8 JSON output and compact JSON as comparison cases. They can reveal how much of the opportunity comes from indentation and intermediate string copies. Keep the current readable export available and preserve numeric, timestamp, event-type, and embedded resolution semantics. The existing JSON reader's validation cannot be lost in a new binary reader. Sources: [GameEventWriter.cs](../../LimboDancer.Domains.Asl.Units/State/GameEventWriter.cs), [GameEventReader.cs](../../LimboDancer.Domains.Asl.Units/State/GameEventReader.cs).

### Retained scenario cards

A proposed game archive could retain the exact normalized card text and its existing SHA-256, either within the game package or in an immutable store addressed by that hash. The game would resolve that retained version after the live card changes. This requires a deliberate change to the current refusal behavior and a retention policy.

CBOR can carry that text, but converting the card to CBOR and hashing the new bytes would not preserve its existing identity. Retaining a card also does not automatically retain the boards, rule packages, unit definitions, or other inputs needed to reproduce a game. Define the archive's dependency coverage explicitly.

### Game-to-card indexing

The backlog's metadata-read proposal is independent of the event encoding. `LivePlay.GamesFrom` currently calls `Store.Read` for each game and looks for its `GameStarted` payload. A small header or index should identify tenant, game, and card identity without decoding every event. It needs rebuild behavior and consistency with game creation, replacement, and deletion. A timestamp-only cache must not silently weaken source-change detection where stronger checks are required. Source: [LivePlay.cs](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs).

CBOR would be optional for the metadata representation. The primary gain comes from reading only the information the card editor needs.

## Proposed event storage direction

Keep `IGameStore` as the application boundary. Compare an append-only batch log with a transactional storage provider before selecting the persistence mechanism. Each can store JSON or CBOR payloads. Avoid introducing a general serialization framework into rules or planner code just to support one experiment.

A proposed batch envelope would contain a format version, tenant and game scope, expected and resulting revisions, an attempt identifier, event count, and ordered events. Scope and revision checks belong to the store, not to the codec.

[CBOR Sequences](https://www.rfc-editor.org/rfc/rfc8742.html) permit concatenated CBOR items and could represent successive batches. They do not provide transaction commits, durable acknowledgement, writer coordination, or random-access indexes. A production log still needs bounded records, integrity checks, a defined commit boundary, and recovery rules for an interrupted final batch. Corruption inside committed history must not be treated as an ordinary incomplete tail.

The implementation must settle these requirements before replacing the current store:

- Commit an action's complete event batch atomically, including its recorded rolls. A stale revision or repeated attempt must not draw new rolls.
- Coordinate separate Studio processes. The inspected store uses a static in-process lock and a fixed temporary filename; this alone is not evidence of safe simultaneous writers across processes. Test that requirement explicitly.
- Define when a successful acknowledgement is durable, including crash and restart behavior. File replacement alone does not specify power-loss durability.
- Preserve event order, diagnostics, replay outcomes, and object reuse needed by the planner.
- Retain a migration and JSON export path. Version the format independently of event schema changes.
- Treat a checkpoint, if later added, as validated derived state tied to an exact history prefix and relevant package versions. It must not authorize actions or replace missing history without an explicit archival contract.

Write-behind would introduce a period in which an acknowledged game action may not yet be durable. It should not be the default shortcut for this backlog item.

## Proposed map cache experiment

The smallest CBOR experiment is a rebuildable cache of one stable numeric map structure, such as terrain cells or derived hex facts. Place the codec at the loading and persistence boundary and continue returning the existing map read types.

Use a cache envelope with the source identity, catalog identity, derivation version, and cache schema version. Include overrides, placement, footprint, and other dependencies whenever they affect the selected cached result. A composed-map result cannot be keyed only by an individual board version.

Retain authoritative inputs and validation. On a cache miss, stale version, or malformed cache, rebuild through the established loader. Bound cache retention and coordinate fills and publication so readers never consume a partial cache. Do not serialize planner memos or mutable intermediate derivation objects as an incidental shortcut.

Start with named fields and direct typed encoding. Integer field keys or packed byte arrays can be later comparison variants, but they require a documented mapping, element widths, and ordering. Smaller payloads must not come from omitting facts or reducing coordinate precision.

## Encoding and compatibility requirements

Define the allowed types, required fields, duplicate-key policy, numeric ranges, timestamp precision, maximum payload sizes, nesting limits, and unknown-field behavior. An optional field may be skippable; an unknown event type or rule-affecting field must not silently disappear during replay.

Deterministic CBOR requires an agreed profile. Microsoft's `Canonical` mode is documented against RFC 7049; RFC 8949 distinguishes deterministic encoding conventions, including map-key ordering. Do not assume any setting reproduces existing canonical JSON bytes or hashes. Keep semantic identity separate from a checksum of the encoded cache. [Microsoft conformance modes](https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborconformancemode), [RFC 8949 deterministic encoding](https://www.rfc-editor.org/rfc/rfc8949.html#section-4.2)

Keep JSON for authored packages, review evidence, and existing agent-facing contracts in the initial scope. Preserve compressed SVG and counter patches. A binary cache is not a reason to alter these interfaces.

## Evaluation and acceptance

Run two separate evaluations so a storage-layout improvement is not attributed to CBOR:

1. **Map cache:** compare the current loader, a comparable JSON cache, and CBOR for the same derived data. Include compressed variants if useful. Measure cold load, warm reuse, cache build cost, file size, allocations, and retained memory independently.
2. **Game storage:** first compare current indented JSON, direct-byte JSON, compact JSON, and CBOR under the same rewrite behavior. Then compare selected encodings under the proposed batch storage design. Separate replay, codec, read, write, and flush costs.

Use the established `bd01`, `cd-guards-5`, and `a3-gambit-rout` workloads where appropriate, plus longer histories and repeated game/card indexing. Record exact data versions, build configuration, machine load, and measurement method. Follow the performance guidelines' allocation-tick method; the earlier allocation-counter jumps are not reliable per-step measurements.

Acceptance requires equivalent map facts and LOS answers; unchanged game events, roll records, diagnostics, and replay outcomes; preserved disclosure boundaries; and correct old-file import. Storage tests must cover competing processes, stale revisions, repeated attempts, interrupted writes, restart recovery, corrupted records, and external changes. Cache tests must cover invalidation, reconstruction, bounds, and concurrent fills.

Choose adoption thresholds before implementation based on the measured bottleneck. A reduction in file size alone does not justify a second authoritative game format. Adopt a map cache only if its end-to-end benefit outweighs source verification, rebuilding, and maintenance costs. Adopt a game codec only if it adds material value beyond the storage-layout and direct-JSON improvements.

## Recommended sequence

1. Establish a current storage baseline and retain the existing backlog rulings.
2. Run the bounded map-cache experiment if cold loading warrants it.
3. Design the game-store commit and recovery model, together with metadata access and retained-card requirements.
4. Evaluate CBOR within that design and preserve JSON compatibility and export.

The storage backlog changes the priority of the original recommendation: a map cache remains the lowest-risk place to learn about CBOR, but addressing the known game persistence cost requires a dedicated storage design. CBOR is one encoding candidate within that work, not a prerequisite for it.
