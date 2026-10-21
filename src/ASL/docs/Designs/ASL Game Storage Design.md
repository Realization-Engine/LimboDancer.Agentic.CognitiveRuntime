# ASL Game Storage Design

**Status:** Proposed. This document defines a candidate storage contract and implementation sequence; it does not change the running store or approve migration.  
**Date:** 2026-10-21

## Purpose and decision

The proposed store commits event batches as transactions in a local SQLite database, retains the exact scenario card text a game started with, and maintains a game-to-card index in the same transaction as game creation. The event payload begins as UTF-8 JSON. CBOR remains an optional payload encoding to adopt only if a controlled comparison shows material benefit.

This addresses three deferred items in the [ASL Unit Backlog](<../ASL Unit Backlog.md>): section 65's whole-game rewrite, section 42's game that can no longer play after its card changes or disappears, and section 42's full-record scan for the game-to-card index. It implements the storage direction examined in the [CBOR analysis](<../Memory-And-Performance/ASL CBOR Architecture and Storage Analysis.md>). It preserves the governed action and replay contracts in the [Governed Writes Design](<ASL Unit Governed Writes Design.md>) and the [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>).

The proposal assumes Studios sharing a database run on the **same host**, with the database on a local filesystem. SQLite transactions coordinate separate processes, with one writer at a time. SQLite WAL mode does not work across host machines on a network filesystem; a multi-host deployment would need a database service and the same `IGameStore` semantics. Do not migrate an existing shared folder until its actual deployment topology is checked. [SQLite transactions](https://www.sqlite.org/lang_transaction.html), [SQLite WAL constraints](https://www.sqlite.org/wal.html)

## Current behavior and required outcomes

`FileGameStore` holds a complete `{tenant}/{game}.game.json` file. `Commit` checks an attempt and revision, builds events and any dice rolls, replays the whole proposed history, writes the complete JSON record, reads it back, and replaces the old file through a temporary file. `Read` loads and hashes the full file, then reuses parsed event objects when those bytes match. The in-process lock does not coordinate separate Studio processes. Source: [GameStore.cs](../../LimboDancer.Domains.Asl.Play/GameStore.cs).

The measured store portion of `LivePlay.FollowStep` was about 48 ms per step at a 1.2 MB game in `cd-guards-5`. That measurement includes append and read work; it is not an isolated JSON codec benchmark. One 23-second outlier in a traced run needs separate diagnosis. Source: [Play Step Analysis](<../Memory-And-Performance/ASL Map Studio Play Step Analysis.md>), sections 2.3, 4.J, and 8.2.

The first event carries a `ScenarioCardReference` with card ID, SHA-256, and title. `GamePlanner.CardOf` currently requires the live card to match the recorded reference, subject to an explicit earlier-revision exception. Deleting or editing the card can therefore make play refuse. `LivePlay.GamesFrom` reads every game record to find the `GameStarted` reference. Sources: [GameEvent.cs](../../LimboDancer.Domains.Asl.Units/State/GameEvent.cs), [GameTypes.cs](../../LimboDancer.Domains.Asl.Units/State/GameTypes.cs), [GamePlanner.CardSetup.cs](../../LimboDancer.Domains.Asl.Play/GamePlanner.CardSetup.cs), [ScenarioCards.cs](../../LimboDancer.Domains.Asl.Play/ScenarioCards.cs), [LivePlay.cs](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs).

The replacement must keep these observable results:

| Operation | Required result |
|---|---|
| Valid append at expected revision | One complete, ordered event batch becomes visible, and the returned revision identifies its end. |
| Repeated committed attempt | `Replay`, with no additional events or new dice draw. |
| Stale expected revision | `Stale`, with no event or dice draw. |
| Invalid event batch or replay | `Invalid`, with no committed events. |
| Reader during concurrent commit | Either the complete earlier revision or the complete later revision. |
| Restart after acknowledged commit | The complete committed batch remains readable under the selected durability setting. |
| Game started from a card | The game's retained card content remains available even if the editable source changes or is deleted. |
| Card editor game lookup | Games started from the card appear without loading each full event history. |

Existing rules, board-version pinning, viewer disclosure, and recorded dice results remain governed by their current contracts. A card snapshot alone does not make an entire game self-contained: board packages, catalog, rules, and other dependencies still need their existing provenance and version handling.

## Storage choice

| Candidate | Fit for the three backlog items | Limitation |
|---|---|---|
| Keep the complete JSON file | Retains familiar files and current validation | Every append still rewrites history; card retention and metadata indexing need separate consistency mechanisms. |
| Per-game append log plus sidecar metadata | Avoids whole-file rewrites and keeps files inspectable with a JSON encoding | Requires a cross-process commit lock, crash-safe batch framing, and coordinated sidecar updates. A partly written batch cannot become visible as committed. |
| Local transactional SQLite store | Commits batch, game metadata, index, and card snapshot together; coordinates separate processes on one host | Adds a database provider and migration, permits one writer at a time, and requires a local supported filesystem. |

SQLite is the proposed implementation because one transaction can keep the history, index, and retained card consistent. The choice is conditional on the same-host deployment check and a measured concurrency gate. If the Studios must write from different hosts, choose a transactional server provider behind the same store boundary before migration. CBOR changes none of these coordination requirements.

## Storage boundary and layout

Keep `IGameStore` as the play-facing boundary. Introduce a transactional implementation for local Studio use and leave the JSON file implementation available for migration, comparison, and export. The adapter may add a narrow read contract for game metadata and a card-snapshot resolver; rule and UI code should not issue SQL directly.

Proposed logical schema, with one database per tenant:

| Record | Fields and constraints | Purpose |
|---|---|---|
| `games` | `game_id` primary key; label, synthetic flag, current revision, creation time, card ID, recorded card digest, card title, snapshot digest, storage schema version | List games, check revision, and locate the card without scanning events. Card fields are null for a game without a card. |
| `event_batches` | `(game_id, first_revision)` primary key; last revision, attempt key, event count, codec ID, payload bytes, payload SHA-256; unique `(game_id, attempt_key)` | Store each committed action as one unit, preserve order, and answer idempotency checks. |
| `card_snapshots` | digest primary key; card ID, exact normalized UTF-8 text, optional parse schema version | Retain the identity-defining card content; share identical text without duplicating it in every game. |
| `games` index | `(card_id, game_id)` | Answer `GamesFrom(card)` directly. |

This is a logical contract, not final SQL. Store byte counts and types must be bounded; enforce foreign keys and check constraints; validate that batches form a gap-free revision sequence and that metadata agrees with the first `GameStarted` event. A game's `snapshot_digest` should reference the `card_snapshots` row in the same database. Keep snapshots while referenced by any game. A deliberate game deletion may remove its unreferenced snapshot only after reference checks.

The event batch payload should initially contain the same complete `GameEvent` fields read by `GameEventReader`: event ID, revision, timestamp, source, type, rule package, disclosure information, and payload. The record envelope identifies the codec. An adapter must decode and check the batch against its row's revision bounds, event count, and checksum. Do not infer event meaning from the SQL index alone.

## Commit protocol

Use a transaction that obtains write authority before checking the current revision. SQLite permits one writer across connections, including connections from separate processes. Keep the transaction bounded and apply a configured busy timeout; a timed-out write returns an explicit storage failure, not `Stale` or `Invalid`. Do not silently retry after dice have been drawn. [SQLite transaction behavior](https://www.sqlite.org/lang_transaction.html), [Microsoft.Data.Sqlite concurrency](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)

1. Resolve the tenant and game. Open a connection with verified durability and foreign-key settings. Begin a write transaction.
2. Check whether the attempt key already names a committed batch. If so, return `Replay` at its stored revision without invoking the event builder or roller.
3. Check the game's stored revision against `expectedRevision`. Return `Stale` before invoking either builder or roller if it differs.
4. Build the batch once. For a rolled action, draw only while building the committed candidate and only for branches actually taken. Validate event IDs, scope, contiguous revisions, and the first-event attempt identity.
5. Replay the candidate history under the current gate's rules and pinned dependencies. If replay fails, roll back the transaction and return `Invalid` with the existing diagnostics. Full replay can remain initially for correctness; optimizing it requires independent replay evidence.
6. If this is the game's first committed batch with a scenario, retain the exact normalized card text whose hash matches the `ScenarioCardReference`; parse and validate it. Insert the snapshot, game metadata, and batch together. A card change during setup must cause a retry or refusal before an inconsistent snapshot is recorded.
7. Encode the batch, decode it back through the storage reader, and validate equivalence and form before commit. Insert the batch and update `games.current_revision` within the same transaction. Confirm the update affected the expected game revision.
8. Commit, then return `Committed`. Do not publish success or update an in-memory read cache before the transaction reports success. If commit outcome is uncertain because of an I/O failure, reopen the store and query the attempt key before deciding whether to retry; never draw again merely because the caller lost the acknowledgement.

The existing API uses the first event ID as the idempotency indicator. Preserve that mapping when deriving `attempt_key`, including the behavior of actions that already supply `firstEventId`. Define behavior for an empty batch explicitly before implementation, since it cannot be identified by a first event ID. A rolled action that fails validation leaves no recorded roll even though the random source was consumed.

Long replay under SQLite's single writer can block unrelated game commits. Measure transaction hold time. If that becomes material, precompute a candidate outside the transaction, then recheck and validate it against the authoritative revision and dependencies inside the transaction; a speculative dice draw cannot be accepted as the committed result after a conflict. Any later partial-replay optimization must preserve the same acceptance result.

The initial durability target is an acknowledged transaction surviving process crash and host restart on a supported local filesystem. Set SQLite's journal and synchronous options explicitly, read back their effective values, and exercise power-interruption recovery before promising that target. WAL with `synchronous=FULL` is a candidate for the same-host case; measure it against rollback-journal mode. Back up a live database with SQLite's online backup mechanism, not by copying a database file in isolation. [SQLite synchronous settings](https://www.sqlite.org/pragma.html#pragma_synchronous), [SQLite backup API](https://www.sqlite.org/backup.html)

## Reads, replay, and indexing

`Read(scope)` assembles batches in revision order and returns the existing `GameRecord` shape. The loader verifies codec, checksum, revision continuity, event IDs, and schema version before replay. Corruption in an acknowledged batch is an error; it must not be ignored as an unfinished tail. The database transaction itself defines visibility, so readers never see a partial batch.

Keep a bounded per-process parsed-record cache keyed by game and authoritative revision. A read after another Studio's commit must observe the new revision before reusing a cached object. Reuse historical event objects when their stored batches have not changed, preserving the incremental replay behavior added in pass 31d. Cache entries follow the existing lifetime and bound guidance.

`GamesFrom(card)` queries the indexed `games.card_id` and returns game names in the current order. The index records the card named at game creation, not whether the editable card currently matches its snapshot. Rename and delete confirmation therefore continue to name the affected games. An index audit compares each row with its first decoded `GameStarted`; repair is an explicit rebuild operation, not a silent read-time guess. For imported files, build the index during validated import. This removes full-record reads from the ordinary editor path.

Exports write the current JSON game-record format through `GameEventWriter`, ordered by revision. An exported file remains useful for inspection and existing readers; its card dependency must be included in a separate archive manifest if portable continuation is intended. The database format and archive format are different contracts.

## Retained scenario cards

`ScenarioCardLibrary` identifies a card by the SHA-256 of its LF-normalized text. Keep those exact normalized UTF-8 bytes as the snapshot and verify the digest on read. A game started without a card has no snapshot and continues to follow the existing no-card path.

Add a game-aware card resolver. For a new game with a snapshot, load and parse the retained text under the game's recorded card ID and applicable catalog. The resolver should return that card even if the editable source is later changed or deleted. Update every rule path that currently calls `CardLibrary.Matches` or `CardOf`, including Victory and setup validation, to use the same resolved card. Otherwise the game could pass one check and fail another. Display the retained version as the game's card and identify a changed or missing editable source separately in the card editor.

Existing `ScenarioCards.SameCard` earlier-revision exceptions must remain valid for legacy games. When a validated snapshot exists, its exact digest governs that game's card; the current editable card does not silently replace it. Treat a card snapshot as game input, with access controlled by the same tenant and viewer boundaries as the game. Do not expose concealed information through a metadata or export path.

For old games whose current card still matches the recorded digest, import its text as a snapshot. If only an explicitly compatible earlier-revision mapping exists, keep that game's established compatibility behavior and mark it as using a compatibility card; do not claim to have recovered the original bytes. If the card is missing or incompatible, import the game history but leave continuation refused with a precise diagnostic until the original card is supplied and its digest verified. Do not fabricate a snapshot from a changed card.

## Migration and rollback

Migration is a versioned, explicit operation over the existing JSON files. Preserve source files until the new store has passed acceptance and a rollback plan has been exercised.

1. Inventory game files per tenant and validate each with `GameEventReader` and the existing projector. Record file hash, event count, revision, and scenario reference.
2. Build a candidate database in a new location. Insert each validated game, its ordered batches, the game-to-card metadata, and any verified card snapshot. Flag files that cannot be imported without changing their meaning.
3. Read each imported game back and compare the complete event sequence, replay diagnostics, current state, and card resolution. Exercise games without cards, compatible earlier revisions, changed cards, and missing cards.
4. During cutover, stop writes from all Studios, recheck source hashes, and import any changed files. Atomically select the new store in configuration only after validation and a backup. Do not permit one Studio to write JSON while another writes the database for the same game.
5. Keep the old files read-only as rollback evidence. A rollback after new database commits requires exporting those commits through `GameEventWriter` and validating them before any old store resumes writes. Merely switching configuration back would lose acknowledged actions.

The migration should be rerunnable against unchanged inputs and refuse a changed source unless it can reconcile by exact event identity and revision. Historical files may have been edited externally; never trust file time alone. Maintain a format version for the database schema and a separate version for encoded event payloads.

## CBOR decision within this design

The first implementation uses JSON event payloads because the existing reader and writer supply concrete validation and export behavior. Compare CBOR as an alternative for **one event batch payload**, not as a replacement for game metadata, the card's identity bytes, or the transaction mechanism. A compact JSON payload and direct UTF-8 writing are comparison cases before adding a second codec.

If a CBOR codec is admitted, specify named fields or a documented integer-key mapping; exact integer widths, timestamp representation, optional fields, duplicate-key handling, nesting and size limits, and unknown-field behavior. Include a codec ID on every batch so old and new batches can coexist in one game. Reject unknown rule-affecting event fields rather than silently skipping them. Keep the JSON export and event-domain model stable. Deterministic CBOR is not byte-compatible with the existing canonical JSON hashes. Sources: [CBOR RFC 8949](https://www.rfc-editor.org/rfc/rfc8949.html#section-4.2), [System.Formats.Cbor API](https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborreader?view=aspnetcore-10.0).

Use the same store schema and workloads for JSON and CBOR. Measure payload bytes, encode/decode time, allocations, commit latency, read latency, replay latency, and database growth. Compare cold reads, warm reads, long histories, and concurrent Studios. A smaller payload is insufficient if end-to-end Play latency or maintenance cost worsens. A CBOR map cache remains a separate experiment under the [CBOR analysis](<../Memory-And-Performance/ASL CBOR Architecture and Storage Analysis.md>).

## Acceptance and staged delivery

| Stage | Deliverable | Acceptance evidence |
|---|---|---|
| 1. Baseline | Instrument current read, replay, serialization, write, and card-index costs | Repeat `cd-guards-5` and a long game with consistent build, machine, and file-size records; keep the 23-second outlier separate. |
| 2. Transactional store | New local adapter behind `IGameStore`, with JSON batches and JSON export | Same `Committed`, `Replay`, `Stale`, and `Invalid` behavior; full event and replay equivalence. |
| 3. Retained cards | Snapshot at start, game-aware resolver, migration diagnostics | New games continue after card edit or deletion; legacy compatibility and missing-card refusal behave as specified. |
| 4. Index | Indexed `GamesFrom` and index audit | Same results and ordering as the current full scan, including rename and delete confirmations, without decoding every game. |
| 5. Recovery and rollout | Crash tests, separate-process races, backup and migration rehearsal | No partial batch, double roll, stale acceptance, lost acknowledged commit, or unreported migration gap. |
| 6. Optional codec | Controlled JSON versus CBOR comparison | Adopt only on measured net benefit with equivalent events and diagnostics. |

Test a commit racing another Studio's commit to the same game, independent games sharing the database, uncertain acknowledgement, bad payload checksum, mid-import source changes, interrupted migration, and backup restoration. Verify viewer disclosure on metadata, snapshots, and export. Follow the performance guidelines' allocation-tick method and record end-to-end Play impact as well as isolated storage timings.

The first review decision is whether the actual Studio topology permits a same-host local database. Once that is established, the transaction and migration gates make this proposal implementable without depending on CBOR adoption.
