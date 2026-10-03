# ASL Named Counters Design

**Status:** Design for review. The principles below were agreed in discussion; the contracts and lifecycle policies are proposed implementation details. The [name-pool data](../units/names/README.md), manifest, schemas, and data validators have been added as editorial drafts. Runtime readers, Scenario Card integration, and lifecycle implementation remain pending.

**Date:** 2026-10-03

**Delivery status:** Data and design groundwork is complete: 40 editorial-draft name pools, the nationality/force manifest, schemas, and local/CI validation. Historical and cultural review remains pending. Resource embedding, C# readers and generation, Scenario Card serialization and authoring, game-state transfer, and identity lifecycle handling are not implemented. Proposed regression tests are documented in the name-pool README only; this change adds no test suite.

**Branch:** `asl-narrative-extensions`, in a separate worktree. The shared checkout and the other agent's `ui-improvements` work have priority.

**Related designs:** [ASL Unit State Model Design](<ASL Unit State Model Design.md>), [ASL Unit Scenario Card Games Plan](<ASL Unit Scenario Card Games Plan.md>), and [ASL Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>).

## 1. Outcome and scope

Bring the named personnel developed for Scenario Card narratives into the ASL implementation. A Scenario Card owns a reusable cast: every leader is named, and every squad, half-squad, and crew has a named noncommissioned officer (NCO). The same saved card produces the same opening cast in every game.

The Guards Counterattack narrative provides the initial integration case: eight fictional leaders and 34 fictional squad NCOs. Import those identities from the reviewed narrative artifacts, preserving their spellings and assignments. The narrative's illustrative hex placement is separate from the cast and must not silently become a mandatory setup rule.

This feature supplies personnel identities and readable handles for cards, counters, inspectors, game history, and later narrative generation. It does not give names a rules effect. Counter definitions, combat values, leadership modifiers, and nationality rules continue to determine play.

The initial work includes name-pool data and lookup, card authoring and persistence, transfer into game state, identity lifecycle handling, and perspective-safe display. Full biographies, voice casting, interview generation, and a complete simulated chain of command are subsequent work.

## 2. Agreed principles

1. Keep rules nationality, force affiliation, and name-pool identity separate. A Polish formation using British counters can retain Polish names.
2. Target 100 given names and 100 surnames per cultural name pool. Prefer a smaller credible pool to padding a list with unsuitable names.
3. Use period-appropriate names, weighted selection, and culturally valid combinations. Support patronymics, compound names, and different display orders.
4. Store the selected personnel in the Scenario Card. Explicitly authored names take precedence over generated names.
5. Assign stable IDs to people and individual counters. Names are labels, never identifiers.
6. Support weapons have no personnel name assignment. Describe their current holder when useful, for example, "the HMG carried by Lebedev's squad."
7. Preserve identities in saved games and replay. Pool updates must not rename existing people.
8. Prevent the same person from being duplicated across active counters when units split or combine.

## 3. Ownership and file placement

Keep source data alongside the existing unit catalogs and Scenario Cards:

```text
src/ASL/units/names/
    manifest.json
    german.names.json
    russian.names.json
    polish.names.json
    french.names.json
    ...

src/ASL/LimboDancer.Domains.Asl.Units/Naming/
    NamePool.cs
    NamePoolReader.cs
    NamePools.cs
    PersonnelIdentity.cs
    PersonnelNameGenerator.cs
```

The class names above are proposed. Each pool file contains both given names and surnames. The manifest lists pool IDs, files, versions, and mappings from national or cultural contexts to pools. A force mapping may require an explicit subgroup choice; it must not assume one language or ethnicity for every member of a multinational force.

Embed these resources in `LimboDancer.Domains.Asl.Units`, following its existing vocabulary and catalog resource pattern, including `WithCulture="false"`. Load and validate each pool once and cache immutable lookup structures by pool ID and version. Expose APIs to list pools, read a pool, and generate an identity. Runtime consumers must not depend on source-tree paths.

`LimboDancer.Domains.Asl.Play` already references Units and owns `ScenarioCards`, `ScenarioCardLibrary`, `ScenarioSetup`, and game planning. It selects naming contexts, validates card assignments, and transfers identities into games. Map Studio presents those capabilities through the card editor and existing counter displays.

## 4. Nationality coverage

The reference is the supplied `eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`, 716 physical pages. The entire book, including Chapter W, is in scope. The rulebook establishes represented forces; it is not a source for the generated personal-name lists.

| Rules grouping | Naming contexts to cover |
|---|---|
| German | German; preserve force distinctions such as SS separately |
| Russian | Soviet forces; Russian is the initial pool, with additional cultural pools where a scenario requires them |
| American | U.S. Army and Marines; Filipino personnel require their own pool |
| British/Commonwealth | British, Australian, New Zealand, Canadian, Indian, Gurkha; colonial formations require appropriate recruiting-population contexts |
| French | French, Vichy French, Free French; additional colonial contexts where required |
| Italian | Italian and Eritrean |
| Finnish | Finnish |
| Japanese | Japanese |
| Chinese | Nationalist, Communist, and Korean War CPVA affiliations, with appropriate shared or regional pools |
| Axis Minor | Romanian, Hungarian, Slovakian, Croatian, Bulgarian |
| Allied Minor | Polish, Norwegian, Danish, Dutch, Belgian, Yugoslavian, Greek, Ethiopian |
| Partisan | Scenario-specific national or regional context |
| Korean | South Korean Army, Marines, KATUSA; North Korean Army and guerrillas |
| Other UN Command | Belgian, Colombian, Ethiopian, French, Greek, Luxembourger, Dutch, Filipino, Thai, Turkish |

Evidence: A25, physical pages 93-98; Allied Minor LG, ELR, and SW Allotment Chart, page 513; W3-W7 and the W. National Capabilities Chart, pages 660-674; A./G. National Capabilities Chart, page 695. British Commonwealth Forces Korea reuse Australian, British, Canadian, and New Zealand contexts (W4.1).

This is a coverage inventory, not a requirement for one file per faction. For example, Free French and Vichy French may share a French pool; North and South Korean forces may share a suitable Korean pool. Conversely, Indian, Belgian, Yugoslavian, and colonial formations may require several compatible subgroups. Record these choices in the manifest and pool metadata rather than silently mixing names.

## 5. Name-pool contract

Each pool has a format identifier, stable ID, version, display label, applicable periods, language/cultural context, naming convention, provenance notes, and entry collections. Each name entry has a stable ID, Unicode text, and positive selection weight. Optional compatibility tags constrain valid combinations. The period applies to the people being represented, not just the year a modern name list was collected.

Use `givenNames` and `familyNames` as the normal collections, with optional patronymic or other components where needed. A convention describes component roles and display order. For traditions without hereditary surnames, the second component must be labeled accurately, such as a parent's name. Do not force every culture into a Western first-name/last-name model.

Keep pool metadata and name provenance sufficient for editorial review. Generated combinations are fictional identities; a plausible name does not establish a historical person's presence in a scenario.

Validation rejects duplicate entry IDs, blank names, nonpositive or invalid weights, unsupported conventions, missing referenced pools, and incompatible metadata. Normalize Unicode for duplicate comparison while preserving intended spelling and display. Counts below 100 produce a completeness note, not an automatic rejection. Validate generation capacity after compatibility restrictions, rather than assuming every pool yields 10,000 valid combinations.

## 6. Personnel and counter identity

Use three distinct identifiers:

| Identifier | Scope and purpose |
|---|---|
| Card counter ID | Stable individual occurrence in the card's order of battle |
| Person ID | Stable identity of a fictional or explicitly documented person |
| Game instance ID | Existing runtime identity of a unit or equipment instance |

Person IDs in a card are card-local; runtime identities are scoped to the game. Two games may use the same cast without sharing mutable personnel state.

A personnel record holds structured name components, a saved display name, identity status (`fictional` or `historical`), and origin (`authored` or `generated`). Rank and narrative notes are optional. Generated records also retain pool ID/version and selected entry IDs. Historical status requires a source reference. An imported fictional character is authored and fictional, even if its spelling also exists in a pool.

Rank is descriptive metadata. An ASL leadership rating does not establish a military rank. Generating a name must not invent an unsupported promotion or confer a rules ability.

| Counter kind | Assignment |
|---|---|
| Leader, including commissar | The person represented by the counter; no additional NCO |
| Squad or half-squad | One designated NCO, with retained additional personnel when lifecycle changes require them |
| Infantry or vehicle crew counter | One designated NCO/crew commander |
| Vehicle with inherent crew | A named commander associated with its crew; avoid a second identity if that crew becomes a separate counter |
| Hero | The represented person; reuse an existing identity when an explicit relationship supports it |
| Support weapon | None; use its equipment ID, type, and current holding relationship |
| Gun | None on the equipment; the manning unit supplies its named NCO |
| Dummy, marker, fortification, abstract entity | None |

The vehicle and hero cases extend the agreed leader-and-NCO principle and must be covered by implementation review. Do not assign an NCO to an inanimate object simply because its runtime representation uses a unit record.

## 7. Scenario Card extension

Currently `ScenarioCardUnit` records a definition, count, and setup area. Preserve these aggregate lines for order-of-battle checks. Extend a named card with stable group and line IDs, a personnel roster, and individual counter entries under each line. Each individual entry identifies its card counter and personnel assignment. This avoids using array position or display text as identity.

For a line containing three squads, a complete named card contains three distinct counter entries and three designated NCO assignments. Equipment can have stable entries for equipment tracking but cannot reference personnel directly. Balance-only additions and delayed reinforcements receive their own entries, with activation governed by existing card rules.

Add naming context at group level, with an optional per-counter override. Keep the existing rules nationality and `ScenarioCardSide.Nation` meanings intact; do not reinterpret `Nation` as a language code. Resolve context in this order: explicit counter context, group context, then an unambiguous manifest default. If a context is ambiguous, require an authoring choice rather than falling back to an unrelated pool.

The proposed card extension is conceptually:

```text
card.naming: format/version and completion state
card.personnel: saved people keyed by person ID
side.groups[].id: stable group ID
side.groups[].namingContext: force affiliation and pool selection
side.groups[].units[].id: stable OB line ID
side.groups[].units[].counters[]: counter ID and personnel references
```

Finalize exact serialized field names and the next format version during implementation. A named card must have a distinct supported contract so older readers cannot silently discard its identities. Existing cards remain readable through an explicit legacy path.

Validation requires unique IDs, exact agreement between line counts and individual entries, valid roster references, appropriate roles, and complete assignments for all eligible counters in a completed named card. A person cannot be the assigned representative of two simultaneously active opening counters. Repeated surnames are valid. Authored duplicate full names may be intentional and receive a warning; duplicate IDs are errors.

## 8. Authoring and generation

Provide a "Fill missing names" operation in card authoring. It preserves all authored identities and existing generated identities, selects compatible names with weights, and avoids repeated full names within the card by default. Explicit regeneration replaces only the author's selected generated assignments and previews the change before saving.

Use a documented deterministic selection algorithm with a recorded seed and algorithm version for reproducible generation. Work in stable counter-ID order and bound duplicate retries. If a pool cannot satisfy the request, report the shortfall and retain the valid assignments already prepared for review. Cosmetic generation must not consume the gameplay dice stream.

Save concrete personnel records in the card. Reopening the editor, rendering a card, or starting a game must not rerun generation. Preserve raw Unicode names through JSON serialization, export, and import.

Legacy cards can be opened and played under existing behavior. Upgrading to a complete named card is an explicit authoring operation, preserving OB quantities, setup areas, rules, and provenance. Draft named cards may be incomplete; they must not be labeled complete or enter the named-game start path until validation succeeds. Minimal cards with no OB need a corresponding fill-name step when eligible units are added by hand.

## 9. Game state, events, and lifecycle

At game creation, snapshot the relevant card roster and stable counter mappings into persisted game data using the existing governed event path. Setup binds a selected card counter ID to a runtime instance ID. Selection from an aggregate pool must identify which individual is being placed; moving a counter does not change that binding. Persist balance choices and reinforcement identities even when their counters are not yet on the board.

Game reads and replay use saved names and assignments, not today's name pools or a newly edited card. Preserve the existing card ID/hash provenance checks. Identity assignments and lifecycle transfers must be included in the event reader, writer, projector, and affected action validation; a UI-only lookup is insufficient.

Proposed lifecycle policies:

| Transition | Identity policy |
|---|---|
| Movement, rally, breaking, concealment, capture | Preserve the people and assignments; apply existing state and visibility changes |
| Same-person leader replacement or promotion | Preserve person ID; update counter definition and only explicitly supported rank metadata |
| Same-size MMC replacement | Carry assigned personnel into the successor instance |
| Squad reduction to one half-squad | Preserve the designated NCO by default as a narrative convention, unless an explicit personnel outcome says otherwise |
| Deployment into two half-squads | One successor retains the original NCO; the other receives a distinct deputy identity, prepared or generated and persisted in the same transition |
| Recombination | Retain both people's identities in the combined unit; designate one NCO and retain the other as deputy |
| Crew survival or abandonment | Transfer an existing inherent-crew identity to the surviving crew instance; do not create a second copy |
| New leader or hero creation | Reuse a known person only when the event explicitly identifies that person; otherwise create and persist a new fictional identity |
| Elimination | Retain identity and lineage in history; do not infer a named person's death from counter elimination alone |

A person has at most one active unit attachment. If a promoted NCO leaves a squad for a leader counter, appoint a distinct successor for the squad in the same operation. Repeated deployment should reuse retained deputies before creating additional people. Dynamic identity creation records complete names and provenance in the event, so replay never performs random generation.

These conventions fill gaps in the counter abstraction; they are not additional ASL casualty rules. They must not block a legal rules action merely because an optional naming resource is unavailable: surface an incomplete narrative assignment for repair, preserve existing identities, and never substitute a culturally unrelated name.

## 10. Display and information boundaries

Use names as a readable secondary handle alongside the counter's normal designation, ID, values, and location. For example: "Lebedev's squad" or "HMG-02, held by Lebedev's squad." Resolve equipment ownership from current state; never store a person's name on the SW as its owner label.

Naming must follow existing perspective filtering. Hidden units, concealed identities, unknown holders, off-board forces, logs, tooltips, search results, and exports must not reveal roster associations that the viewer is not entitled to see. A full authored card roster may be available in authoring, while player-facing game views expose only the information allowed by the game's perspective. Omit the holder's name if the holder is withheld.

Narrative identity does not establish historical evidence. Display fictional status where cast information is presented as documentary material.

## 11. Delivery sequence and verification

1. Define and validate the pool/manifest contract in Units; add a small reviewed pilot set. Verify compatible weighted selection, deterministic behavior, duplicate exhaustion, Unicode, and cultural component ordering.
2. Extend card serialization and validation with roster, stable group/line/counter IDs, and naming contexts. Test legacy parsing, named-card round trips, count mismatches, invalid personnel references, and mixed-nationality groups.
3. Import the Guards Counterattack cast from the narrative source artifacts. Verify eight leader assignments and 34 squad NCO assignments against the source roster, preserve weapon ownership relationships, and exclude names from SW records.
4. Add card editor fill/edit/preview/save behavior. Verify that explicit names survive filling, repeated opens do not regenerate, and saved cards retain the same cast.
5. Implement setup bindings, persisted identities, lifecycle events, and projection filtering. Verify replay after pool changes, reinforcements, balance additions, splitting, recombination, crew survival, promotion, and hidden-information behavior.
6. Expand reviewed pools to the full coverage inventory. Record sources, actual counts, cultural subdivisions, and any gaps per pool. Run the relevant Units, Play, and Map Studio tests and the repository's required checks.

Implementation acceptance requires unchanged rules outcomes for otherwise identical named and unnamed games, stable names across save/load and replay, no duplicated active person attachment, no names assigned to support weapons, and no identity leaks through player projections.

## 12. Decisions to confirm during implementation

- Final naming convention schemas and subdivisions for multilingual and colonial forces, including non-hereditary second-name conventions.
- The exact card format/version migration and naming-completion diagnostics.
- How editors choose the designated NCO after recombination and the successor receiving the original NCO after deployment; use stable-ID defaults with an explicit author override.
- The amount of vehicle, hero, and dynamic deputy handling needed in the first release. Any staged release must state its supported lifecycle coverage rather than claiming universal named-unit support.

No additional permission is needed to implement routine details within an authorized implementation request. These items identify design choices and acceptance boundaries, not separate approval gates.

### Dedicated American pool

The American nationality context defaults to `american.names.json`, shared by U.S. Army and Marine affiliations. It provides 100 given names and at least 200 surnames from multiple American naming traditions, with given-family display order and uniform editorial weights. The SSA 1900-1909 male top-100 table anchors the given-name list to a relevant birth cohort; surname selection remains an editorial draft. Names do not establish ethnicity or a historical formation's composition. Unit-specific profiles and date-sensitive formation restrictions require further implementation and research; authored identities cover those cases in the meantime. Existing saved identities are preserved.
