# LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements

**Status:** Proposed

**Date:** 2026-09-24

**Capability authority:** Normative for the ASL unit model: unit and equipment definitions, game-scoped instances and their state, events, visibility, and the read contracts that give units to Scenario A1 and later reference-domain cases. The package boundary in section 3 is normative; formats, algorithms, and UI belong to design documents.

**Parent requirements:** This document refines [ASL-RD-002, 003, 005, 006, 008, 009, 010, 011, 012, 013, and 015](<LimboDancer.Agentic.CognitiveRuntime ASL Reference-Domain Requirements.md>) for units. It does not change them or admit any runtime contract, rule package, or action authority.

**Related documents:**

- [ASL Unit Domain Model Analysis](<ASL Unit Domain Model Analysis.md>) is the background analysis these requirements are drawn from. Where the two differ, this document governs.
- [ASL Unit Display Design](<ASL Unit Display Design.md>) governs the unit display; its phase 1 (Personnel and SW) is built. [ASL Unit Map Rendering Slice](<ASL Unit Map Rendering Slice.md>) and [ASL Unit Counter Map Rendering Design](<ASL Unit Counter Map Rendering Design.md>) record the first counter overlay, which it replaces. Section 10 states how display relates to the unit model.
- [ASL Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>) and the map design documents own boards, locations, and terrain facts.

## 1. Purpose

Every reference-domain question the product exists to answer involves units: whether this unit may enter that location, what happens when it does, and who is there (ASL-RD-007, ASL-RD-012). Scenario A1 answers such questions today from bounded, scenario-specific snapshots. Those snapshots are exact and reviewed, but they are not a unit model. Nothing in the solution defines what a squad or leader is, holds a unit's state across a game, or says where that state comes from.

These requirements set out what the unit model must provide, in what order, and which decisions must be made before it can be built. They deliberately start from what Scenario A1 needs rather than from the whole rulebook.

## 2. Source facts that shape these requirements

These facts were checked against the registered rulebook PDF, `eASLRB_v3_01.pdf`, SHA-256 `957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247`, 716 physical pages. Page numbers are physical PDF pages.

- **The rulebook defines kinds, not counters.** A1 (pp. 44 to 45) defines Personnel, SMC and MMC, squads, half-squads, and crews, and how printed values are read. Per-nationality values for a particular squad, leader, or support weapon are printed on the counters. The N Armory is not in this PDF (p. 9). Chapter H (from p. 328) has vehicle and ordnance notes and rarity charts; they do not by themselves give every printed counter value.
- **Some values belong to a side, not a unit.** Each side's Experience Level Rating comes from its scenario OB (A19.1, p. 86), and so does each side's Sniper Activation Number (A14.1, p. 82).
- **Some counters are not units.** "A Sniper counter is not a unit" (A14.1, p. 82). Fortifications such as foxholes (B27.1, p. 146) and pillboxes (B30.1, p. 150) are counters that change where and how units are positioned.
- **Crews depend on the vehicle.** An armed vehicle's inherent crew has no counter until it leaves the vehicle; an unarmed vehicle has only an inherent driver, "never treated as a crew" (D5.1, p. 203). An Armor Leader sets the inherent crew's morale (D3.42, p. 200).
- **A location is not a hex.** A hex can hold several locations: levels of a building, cellars, bridges, depressions (A2.8, p. 47). The map model already derives these as each hex's location chain (Map Model and Authoring Design, section 4.3).
- **Concealment is state about knowledge.** Concealment and hidden placement (A12, pp. 76 to 80) mean one side knows less than the other, so the same game has different correct views.

## 3. Package boundary

### ASL-UNIT-001: Separate ASL projects

The unit model must be delivered as ASL domain-package projects under `src/ASL/`, added to `LimboDancer.Domains.Asl.sln`, each with a test project:

| Project | Responsibility |
|---|---|
| `LimboDancer.Domains.Asl.Units` | Definitions, instances, typed state, relationships and their invariants, events, and pure projections. No UI, storage policy, or source-specific parsing. It now holds the unit vocabulary, display documents, and the plausibility check; definitions and state follow (section 13). |
| `LimboDancer.Domains.Asl.Units.Rendering` | The unit display: style sheets, layout, SVG, and the overlay. It replaced the former `DemoUnitOverlay` in Map Studio. |
| Source adapters, one per chosen source (ASL-UNIT-012, ASL-UNIT-050) | Reading a counter data source or a live game source into the model, with provenance. |

### ASL-UNIT-002: Runtime isolation

No unit project may be referenced by `src/LimboDancer`. The runtime gains no unit, side, or phase vocabulary. Units reach the runtime only through ASL observation providers and the governed action path (ASL-RD-015).

### ASL-UNIT-003: Dependency direction

`Units` may reference `Maps` for locations and terrain facts. `Maps` must never reference `Units`. Scenario A1 packages consume `Units` through the read contract (ASL-UNIT-060); `Units` must not depend on Scenario A1.

## 4. Reference definitions

### ASL-UNIT-010: Kinds

The model must classify Personnel as SMC (leader, hero) or MMC (squad, half-squad, crew), and must distinguish vehicles, support weapons, and Guns. Support weapons and Guns are equipment, not units, unless a specific rule relates them to a unit. The classification must stay open to extension; it must not be a closed enumeration until the relevant chapters have been reviewed.

### ASL-UNIT-011: Source-backed printed values

Each printed characteristic (for example FP, range, morale, broken-side values, ELR and leadership markings, US#, portage, caliber, ROF, armor, MP) must be stored as a typed value with its source. A definition's key must identify more than the printed strength triple: nationality, class, face, and applicability are part of it.

### ASL-UNIT-012: Face content and counter data

Two sources are needed and are decided separately:

- **Face content**, what a unit's display must be able to show, comes from the rulebook's counter anatomy (A1.2 to A1.6, A9, C2.2, D1). The [ASL Unit Display Design](<ASL Unit Display Design.md>), section 4, records it with rule and page and is the information-parity reference for ASL-UNIT-075.
- **Counter data**, the printed values of particular units for real scenarios, comes from the counter data source chosen in decision D1 (section 12): the published counter sheets, transcribed and reviewed. No definition may be published until that source is registered and reviewed under the source rules of the Ontology Transformation Specification. The rulebook alone is not sufficient (section 2). Candidates include the published counter sheets and the VASL module's piece definitions; the latter have not been examined and would need the same licensing boundary the map effort applies to VASL boards. Counter artwork is never evidence of a printed value.

### ASL-UNIT-013: Printed and effective values

The model must keep printed values separate from effective values. Terrain, leadership, condition, date, SSR, captured use, and attack mode produce effective values through rules; they never overwrite printed ones.

### ASL-UNIT-014: Applicability

A definition must record the nationality, date range, and module or SSR conditions under which it applies, and any substitution mapping (A19, A25). A lookup outside a definition's applicability must return an explicit result, not the nearest match.

## 5. Game state

### ASL-UNIT-020: Game scope and side state

Game state must be scoped by tenant and game, and must record the sides with their nationality, ELR, and SAN; the board package or composed-map version in play; the turn and phase; and a monotonic revision.

### ASL-UNIT-021: Instances and lineage

Each unit instance must have a stable identity within its game, a definition reference, and an owning side. Reduction, deployment, recombination, replacement, capture, and elimination must be recorded as events that preserve lineage from the instances they consume to the instances they produce.

### ASL-UNIT-022: Equipment

Support weapons and Guns must be instances with their own identity and condition. Possession, portage, manning, towing, and capture are relationships with exactly one authoritative holder at a time.

### ASL-UNIT-023: Conditions

Conditions must be separate dimensions (broken, disrupted, pinned, CX, TI, berserk, fanatic, wounded, concealed, hidden, captured, and others as reviewed), not one state enumeration. Each must distinguish unknown, withheld, and inapplicable from false. Good Order is derived by rule, not stored.

### ASL-UNIT-024: Positions

A position must be one of:

- a **map location**: the placed board's reference, a hex, and one location in that hex's derived location chain (ground level, a building level, a cellar, a bridge, a depression), with a hexside where the placement needs one;
- **containment**: inside a vehicle (with role), in a fortification, carried, or aboard a conveyance;
- **off-map** or **not yet entered**.

Vehicles must also carry their facing. On a composed map, positions keep the placed board's reference, not map coordinates (ASL-MAP-024); the map translates them. A position must be checked against the exact board or map version in play.

### ASL-UNIT-025: Relationships and invariants

Relationships (stack membership, possession, manning, transport, crew, leadership participation, prisoner custody) must have stated invariants that the model enforces. Examples: an inherent crew is not also a free-standing crew counter; an eliminated instance cannot act; a passenger's position agrees with its vehicle's.

### ASL-UNIT-026: Entities that are not units

Snipers, fortifications, and informational markers must be modeled as their own entities, not as units with special flags.

## 6. Visibility

### ASL-UNIT-030: Perspectives

Every read must name a perspective from a closed set: each side, and an adjudicator who sees everything (decision D3). Perspectives are named, so adding one is a reviewed change that needs no change to the model.

### ASL-UNIT-031: Filtering at the source

A projection for a side must omit what that side cannot know, rather than include it for a consumer to hide. Displays, observation providers, and logs receive only the projection they are entitled to.

## 7. Events and change

### ASL-UNIT-040: Ordered events

State must change only through ordered, committed events with an envelope of tenant, game, event ID, revision, time, source, type, payload, rule package reference, causes, and visibility. The current state is a projection of the events.

### ASL-UNIT-041: Conclusions record what they used

A conclusion drawn from unit state must record the game revision and board or map version it used, and must be treated as stale after a later event or version change that affects them (ASL-RD-010).

### ASL-UNIT-042: Writes are governed

No component may change unit state except through the governed action path: a registered action, re-observation, Governance and Diagnostics, the Execution Gate, an expected revision, atomic commit, and verified effect (ASL-RD-008, ASL-RD-012). Writes are out of scope until a transition has its own reviewed package.

## 8. Live game source

### ASL-UNIT-050: A chosen live source

Before any component claims to report the current state of a real game, one live source must be chosen and reviewed. Candidates:

- an adapter reading VASL saved games or logs;
- a scenario setup and play editor in Map Studio;
- a LimboDancer game engine.

Until then, all unit state is fixture data, labelled as synthetic, and must not reach Scenario A1 adjudication or an Execution Gate.

## 9. Scenario A1 read contract

### ASL-UNIT-060: Read contract

The model must offer a read of the form `ReadCase(tenant, game, package, attacker, location, expectedRevision, perspective)` that returns one consistent snapshot: the attacker's definition and state; the occupants of the location, or their sealed presence where the perspective allows only that; the phase; typed locations with the board version and terrain facts from the pinned map package (through the map read API, ASL-MAP-080); movement expenditure; the ordered reveal and response events; the previous location; and the revision, source, and time.

### ASL-UNIT-061: Nondefinitive results

When data is unavailable, inconsistent, unauthorized, or stale, the read must return an explicit nondefinitive result (ASL-RD-011). It must never report a sole defender from incomplete visible information.

### ASL-UNIT-062: First slice scope

The first catalog and state model cover only what the reviewed Scenario A1 cases read: Infantry Personnel (squads, half-squads, leaders) entering a building location, concealment and reveal, Infantry OVR, fortified building entry, and a second defender. Other kinds and families are added one reviewed slice at a time, each tracked as inventoried or not.

## 10. Display

### ASL-UNIT-070: Display consumes projections

The counter overlay renders a display projection (ASL-UNIT-031) and nothing else. A display placement ID is a click and update key, not evidence that a unit instance exists.

### ASL-UNIT-071: Display on every map

The overlay must work on every board the Studio shows, on authored boards, and on composed maps, where it places counters through the map's translation of placed-board locations. It must show a unit's level within its hex.

### ASL-UNIT-072: Counter artwork

Counter artwork may be committed only with recorded provenance and usage rights. Original generated counters are always permitted.

### ASL-UNIT-073: Open unit vocabulary

What units can exist, and what can be said about them, must be declared data: versioned, namespaced vocabulary packs of kinds, attributes, traits, and states. A pack may extend another pack's kinds. The ASL rulebook is one pack; units beyond the rulebook are other packs, added without code changes.

### ASL-UNIT-074: Documents and style sheets

A unit's structure (a unit document: kind, side, location, faces, values, traits, states, attached equipment) must be kept separate from its appearance (a style sheet of selectors and declarations). A document never states how anything looks.

### ASL-UNIT-075: Information parity

Under the ASL style sheets, every fact a printed ASL counter conveys must be readable from the digital unit. The look is free.

### ASL-UNIT-076: Detail by zoom

A unit must show less detail when small on screen and more when large, by declared detail tiers, without a new request to the server.

### ASL-UNIT-077: States on the unit

Conditions that cardboard shows with separate marker counters (broken, pinned, CX, and the rest) must be drawn as states of the unit itself, styled by the style sheet.

### ASL-UNIT-078: Deterministic, accessible output

The same vocabulary, document, style sheet, detail tier, and perspective must produce the same SVG bytes. Every drawn unit must have an accessible name built from the vocabulary's labels.

## 11. Provenance and versions

### ASL-UNIT-080: Definition catalog versions

The definition catalog must have a version identity that changes when any definition or its source changes, and every instance must record the catalog version it was created against.

### ASL-UNIT-081: Source registration

Rule text, charts, and counter data used by the unit model follow the source registration, review, and publication rules of the Ontology Transformation Specification. Page and source citations use physical PDF pages.

## 12. Decisions

Decided on 2026-09-25 as recommended in the [Decision Memo for D1 to D4](<ASL Unit Model Decisions D1 to D4.md>), which records the options and trade-offs.

| Decision | Blocks | Decided |
|---|---|---|
| D1. Counter data source | ASL-UNIT-012 counter data, catalog definitions (not the display) | A mix. The published counter sheets, transcribed and reviewed, are the source of record; each transcription is registered under ASL-UNIT-081 with the sheets used and its reviewer. VASL module piece definitions may serve only as a local, uncommitted cross-check, and only after a licensing review, as the map work treats the VASL board checkout. |
| D2. Live game source | ASL-UNIT-050, 060 against a real game (not the display) | Deferred until governed writes (section 13, step 7). When needed, a Map Studio setup and play editor comes first, building on the Unit Lab and placement sets; a VASL saved-game import may follow as a read-only source. Until then display input stays the Unit Lab and fixtures, labelled synthetic. |
| D3. Perspective set | ASL-UNIT-030 | Each side plus an adjudicator. Perspectives are named, so a later addition is a reviewed change without a model change. |
| D4. First slice boundary | ASL-UNIT-062 | The Scenario A1 case list, cross-checked against the existing Scenario A1 snapshots (ASL-MAP-081). Wider slices follow one reviewed slice at a time. |

## 13. Sequence

1. **Display:** the [ASL Unit Display Design](<ASL Unit Display Design.md>), Personnel and SW first, then Guns, vehicles, and entities that are not units (all four built; its sections 16 to 19) (ASL-UNIT-070 to 078). It needs neither D1 nor D2.
2. **Decisions:** D1 to D4 decided on 2026-09-25 (section 12). Still to do: register the chosen counter source (the counter sheets and their transcription) and, if VASL is to be the cross-check, run its licensing review.
3. **Scenario A1 catalog:** the Infantry Personnel definitions ASL-UNIT-062 needs, with source-backed values and round-trip tests.
4. **State model:** game and side state, instances, conditions, positions against the map location chain, relationships, events, and perspectives, tested with synthetic fixtures.
5. **Read contract:** ASL-UNIT-060 and 061, read-only, together with the map read API (ASL-MAP-080). Cross-check against the existing Scenario A1 snapshots, which stay unchanged (ASL-MAP-081).
6. **Display from projections:** feed the display the projections of the state model, filtered by perspective.
7. **Governed writes:** one reviewed transition at a time, with the live game source of D2 chosen at that point.

## 14. Acceptance scenarios

- **U1, definition lookup.** Given the registered catalog, a lookup for a squad of a given nationality, class, and date returns its printed values with their source, and a lookup outside its applicability returns an explicit miss.
- **U2, lineage.** Given a squad that is reduced and later recombined with another half-squad, the events show which instances each came from, and the projection at every revision is consistent.
- **U3, position on a composed map.** Given a unit at `bd21:N5` on a map with bd21 reversed, its position stays `bd21:N5` and the map places it in the correct map hex and location.
- **U4, perspective.** Given a concealed defender, the attacker's projection shows sealed presence only, the adjudicator's shows the defender, and the display for the attacker never receives the defender's identity.
- **U5, stale conclusion.** Given a conclusion computed at revision 12, a later reveal event makes it stale and a new read is required.
- **U6, nondefinitive read.** Given a location whose occupants are only partly known to the reader, the Scenario A1 read returns a nondefinitive result rather than a sole defender.

## 15. Traceability

| Parent | Unit requirements |
|---|---|
| ASL-RD-002, 003 (rule semantics, applicability) | ASL-UNIT-010, 011, 013, 014 |
| ASL-RD-005 (reference and changing state) | ASL-UNIT-011, 020 to 026, 040 |
| ASL-RD-006 (spatial reasoning) | ASL-UNIT-024, 060, 071 |
| ASL-RD-009 (explanation), display | ASL-UNIT-073 to 078 |
| ASL-RD-008, 012 (conclusions and governed change) | ASL-UNIT-042, 050 |
| ASL-RD-009 (explanation and traceability) | ASL-UNIT-011, 040, 080, 081 |
| ASL-RD-010 (change sensitivity) | ASL-UNIT-041, 080 |
| ASL-RD-011 (indeterminate outcomes) | ASL-UNIT-023, 061 |
| ASL-RD-013 (tenant and package isolation) | ASL-UNIT-020, 040 |
| ASL-RD-015 (separate domain package) | ASL-UNIT-001 to 003 |
