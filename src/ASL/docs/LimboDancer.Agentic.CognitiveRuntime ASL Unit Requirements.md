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
| `LimboDancer.Domains.Asl.Play` | The live game source of D2 and its governed writes: the game store, the registered actions, the planner, and their wiring to the Execution Gate ([Governed Writes Design](<ASL Unit Governed Writes Design.md>)). |
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

Chosen under D2 (section 12): the Map Studio setup and play editor. Live games carry the source `map-studio`, change only through registered actions and the Execution Gate, and are refused by any reader that does not accept that source; fixtures stay synthetic ([Governed Writes Design](<ASL Unit Governed Writes Design.md>), sections 2 and 3).

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
| D2. Live game source | ASL-UNIT-050, 060 against a real game (not the display) | Decided on 2026-09-26, at step 7: a Map Studio setup and play editor, whose games change only through governed writes ([Governed Writes Design](<ASL Unit Governed Writes Design.md>)). A VASL saved-game import may follow as a read-only source. Fixtures and Unit Lab sets stay synthetic. |
| D3. Perspective set | ASL-UNIT-030 | Each side plus an adjudicator. Perspectives are named, so a later addition is a reviewed change without a model change. |
| D4. First slice boundary | ASL-UNIT-062 | The Scenario A1 case list, cross-checked against the existing Scenario A1 snapshots (ASL-MAP-081). Wider slices follow one reviewed slice at a time. |

## 13. Sequence

1. **Display:** the [ASL Unit Display Design](<ASL Unit Display Design.md>), Personnel and SW first, then Guns, vehicles, and entities that are not units (all four built; its sections 16 to 19) (ASL-UNIT-070 to 078). It needs neither D1 nor D2.
2. **Decisions:** D1 to D4 decided on 2026-09-25 (section 12). The counter source is registered, transcribed, and reviewed ([Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>), section 9). Still to do: if VASL is to be the cross-check, run its licensing review.
3. **Scenario A1 catalog:** the Infantry Personnel definitions ASL-UNIT-062 needs, with source-backed values and round-trip tests. Designed and built in the [Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>), and published from the reviewed transcription (built).
4. **State model:** game and side state, instances, conditions, positions against the map location chain, relationships, events, and perspectives, tested with synthetic fixtures. Built in the [ASL Unit State Model Design](<ASL Unit State Model Design.md>), with a Game states page in Map Studio.
5. **Read contract:** ASL-UNIT-060 and 061, read-only, together with the map read API (ASL-MAP-080). Cross-check against the existing Scenario A1 snapshots, which stay unchanged (ASL-MAP-081). Built in the [ASL Unit Read Contract Design](<ASL Unit Read Contract Design.md>), with a Read a case panel in Map Studio.
6. **Display from projections:** feed the display the projections of the state model, filtered by perspective. Built for the synthetic games ([Unit Display Design](<ASL Unit Display Design.md>), section 20).
7. **Governed writes:** one reviewed transition at a time, with the live game source of D2 chosen at that point. Built in the [ASL Unit Governed Writes Design](<ASL Unit Governed Writes Design.md>): D2 decided as the Map Studio play editor; setup, the sequence of play, and entry into an empty building as registered actions through the Execution Gate; and a Play page in Map Studio. Further transitions follow, each with its own reviewed case.
8. **Occupied and concealed entry in live play:** extend the governed entry of step 7 to locations that are occupied, concealed, or hidden, using the reviewed Occupied and PostReveal packages. No new rule area is opened and no die is rolled. In order:
   1. *Terrain abstraction (ASL-MAP-081).* The Scenario A1 packages check terrain through `Board01TerrainCatalog`; back that check with the map read API (ASL-MAP-080), so a live game and the packages read the same board, with every reviewed board 01 override reproduced and the package digests unchanged.
   2. *Live snapshot sources.* Implement the packages' snapshot sources over the read contract (ASL-UNIT-060) for a live game, so a package observes the game the planner plans against. A fact the game does not record stays unknown (ASL-UNIT-061); none is inferred from board metadata.
   3. *Inexperienced status.* Derive it from the definition's class (Green or Conscript, A19.2 and A19.3, p. 86), with the exemption for a Green MMC stacked with an unbroken leader, and use the resulting MF allowance (A4.11, p. 48; A19.31, p. 86). An entry after exactly 2 MF then becomes Definitive. Any new counter definitions this needs take their printed values from the user.
   4. *Occupied entry.* An entry into a location holding a known, unconcealed enemy unit is refused with the reviewed A4.14 conclusion (p. 49) as its reason. Entries that need stacking equivalents, an OVR, a Breach, or the APh stay out of scope, since their conclusions are attempts or need state the model does not hold.
   5. *Concealed entry and forced back.* A governed action for an MPh entry attempt into a location holding exactly one enemy unit, concealed or hidden, that is a non-Dummy MMC (A12.15, p. 78). Its events record the attempt and its MF, the reveal caused by the attempt, and the mover's return to its previous location, with the attempted MF counted there and its MPh ended, and commit only on the Definitive PostReveal conclusion. Before code, an execution boundary review, like the one for the second defender, states the clear-return conditions. In a live game these are derived, not assumed: no Fire action exists, so there is no Residual FP or FFE, and setup cannot place minefields, Wire, or entrenchments. Several concealed units (Random Selection), a revealed SMC (the OVR option), Dummies, Bypass, a concealed or Berserk mover, and Defensive First Fire on return stay out of scope. A case the package does not decide is a refusal with its reasons.
   6. *Acceptance.* U4 to U6 run end to end over a live game in which a reveal occurred, and U7 and U8 (section 14) pass. The Play page offers the new entry, shows the facts and conclusion, and shows the reveal in each side's view.

   Designed and built in the [ASL Unit Occupied and Concealed Entry Design](<ASL Unit Occupied and Concealed Entry Design.md>), with the [post-reveal forced-back execution boundary review](<Scenario A1 Post-Reveal Forced-Back Execution Boundary Review.md>).
9. **Random Selection and the declined OVR:** finish the reveal side of A12.15 (p. 78) in live play with reviewed conclusions only, and bring system dice into games through the [.NET Dice Roller](<NET Dice Roller Requirements.md>) (DICE-07 to DICE-12). In order:
   1. *Random Selection source review.* A short review, before code, of Random Selection (A.9, p. 43) as it resolves an A12.15 reveal among several concealed units:
      - hidden units are placed beneath a "?" first;
      - one dr per unit, and the highest is revealed;
      - every unit tied for the highest is revealed.

      It must confirm that the reviewed PostReveal forced back applies once the reveal is resolved this way, whatever the number of units in the location.
   2. *Dice in the game store.* A `dice-rolled` event and a store operation that draws inside the per-game commit lock, after gate authorization and the revision and attempt checks, following the [.NET Dice Roller Design](<NET Dice Roller Design.md>), sections 3 and 4. Planning, previews, gate evaluation, and replay never draw; a committed attempt returns its recorded values; an unfavorable result is recorded like any other.
   3. *Several concealed units.* The entry of step 8 extends to a target holding several enemy units, each concealed or hidden. A Random Selection roll decides the reveal:
      - if any revealed unit is a non-Dummy MMC, or more than one SMC is revealed (A4.15, p. 49), the PostReveal forced back commits with the roll;
      - if exactly one SMC is revealed, the entry stops at a pending declaration (part 4).

      Dummies stay out of scope.
   4. *The OVR declaration.* When the only unit revealed is one SMC, the attacker may choose an Infantry OVR (A12.15; A4.15, p. 49). The attempt stays open as a pending declaration: the unit may not act, and the phase may not advance, until the choice is made.
      - A new registered action records the choice.
      - Declining commits the PostReveal forced back, which the reviewed matrix already names for this case (`A1-concealed-smc-declined`).
      - Electing is refused with a generic "the adjudicator cannot resolve" reason, and the attempt stays pending, until step 10 reviews the NTC and what follows it.
      - The entry of step 8 into a location holding exactly one concealed SMC then leads to this declaration instead of being refused.
   5. *Acceptance.* U9 and U10 (section 14) pass. The Play page shows the roll and its values in each side's view, and offers the declaration while one is pending.

   Designed and built in the [ASL Unit Random Selection and Declined OVR Design](<ASL Unit Random Selection and Declined OVR Design.md>), with the [Random Selection reveal review](<Scenario A1 Random Selection Reveal Review.md>). The Execution adapter's return aggregate is left as it is until step 10.
10. **The OVR NTC review:** a review-only step that authors and admits a new reviewed case package for the Infantry OVR's NTC through the source pipeline of the Ontology Transformation Specification. The user is the delegated reviewer. It changes no live play. The package must decide:
    1. *Resolution.* The NTC passes when the Final DR of two dice is at or below the unit's Morale Level (A10.1, p. 65; NTC in the Index). The DRM equals the TEM of the enemy-occupied building (+3 stone, +2 wooden; B23.3, p. 136), plus any LOS Hindrance in the location (A4.15, p. 49). The unit's printed morale comes from the reviewed catalog.
    2. *Failure.* The consequence of a failed OVR NTC for a mover that attempted to enter a location whose lone concealed SMC was revealed. A10.1 says the task cannot be performed and no other action may be taken that phase; the package must state whether the A12.15 forced back (p. 78) follows, and with what MF.
    3. *The second defender.* Which unit the DEFENDER reveals when several non-Dummy units remain concealed (A12.15: by Random Selection or by the DEFENDER's choice), and whether the NTC precedes that reveal. The additional-defender review records that the order is not fixed.
    4. *Exclusions.* The leader's exemption from the NTC (A4.15), a Berserk mover, and every case the package does not decide stay out of scope.

    The package pins its source fragments (A4.15, A10.1, A12.15, B23.3) and its case matrix by digest. It has conformance tests like the existing Scenario A1 packages. Its review is recorded in a review document, written before the package is published.

    Done in the [Scenario A1 OVR NTC Review](<Scenario A1 OVR NTC Review 2026-09-26.md>). The user ruled that the NTC comes before the second reveal, that Random Selection (A.9, p. 43, also verified) chooses the second defender, and that a failed NTC forces the mover back with the ordinary 2 MF. The package `scenario-a1-concealment-ovr-ntc` publishes five cases.
11. **The Infantry OVR in live play:** wire the published OVR NTC package of step 10 into live games, so an attacker may elect an Infantry OVR after an entry reveals a lone concealed SMC. In order:
    1. *Rolls on demand.* A commit may draw more than one roll. Each is drawn inside the per-game lock only when the outcome so far needs it, and each is recorded as its own `dice-rolled` event (DICE-07 to DICE-12). The build function asks for rolls as it goes; nothing is drawn for a branch that is not taken.
    2. *The task check.* A `task-check` event records an NTC: the unit, the roll, the unit's Morale Level (its printed morale from the reviewed catalog), each DRM (the building TEM of B23.3; no LOS Hindrance and no leadership, as step 10 admits), the final DR, and the result (A10.1, p. 65; the NTC entry, p. 30). Replay recomputes the result from the recorded roll.
    3. *The election.* `asl.game.declare-overrun` with `elect` commits when:
       - the attempt is pending with one revealed SMC;
       - the mover has at least four MF left (A4.15, p. 49);
       - another concealed non-Dummy unit is in the location.

       The NTC is rolled first (A4.15). A failure commits the forced back with the ordinary 2 MF and reveals nothing further. A pass is followed by a Random Selection roll among the remaining concealed units (A.9, p. 43), their reveal, and the forced back. Each outcome commits only on the Definitive OVR NTC conclusion, read through a live observation provider for that package and checked before any roll. The replay rule that forbids a forced back after an election allows these two reviewed outcomes.
    4. *A lone SMC.* An election against a lone SMC stays refused with the generic reason, since a passed NTC leads to its options and immediate CC (A4.151 and A4.152, p. 49), which are unreviewed. The refusal tells the attacker the SMC is alone. That is a known limitation, accepted while the Studio's single user acts for both sides, and to be closed before multi-user play.
    5. *Retire the separate return aggregate.* Live games now carry the second-defender return, so remove the Execution adapter's journal store and its return action, and the Host's registration and constraint evaluator. The runtime then no longer references Scenario A1, and the game log is the only source of unit state. The second-defender execution review stays as the record of that work. This part touches the runtime Host and its architecture tests, so it is built on its own branch.
    6. *Acceptance.* U11 and U12 (section 14) pass, and the Play page shows each roll and the task check.

    Designed and built in the [ASL Unit Infantry OVR Design](<ASL Unit Infantry OVR Design.md>).
12. **Composed maps in live play:** let a live game be played on several placed boards, as Map Studio's map pages already compose them (ASL-MAP-024), with positions kept board-relative (ASL-UNIT-024). No rule area is opened and no reviewed package changes. In order:
    1. *Placement in the game record.* A game's map records, for each board, its place in the composition and whether it is reversed, beside the board reference and version it already records. A game recorded before this step, with boards and no placement, still replays unchanged. Replay refuses a placement the map builder rejects (VASL-MAP-001 to 004), a board placed twice, and a position on a board that is not in play.
    2. *Setup.* `asl.game.setup` accepts placed boards: each board with its column, row, and reversal. A single board needs no placement. The Play page's setup offers the same, and can start from a map already defined in Map Studio.
    3. *Neighbours across a seam.* The map read API (ASL-MAP-080) gains a read over the game's composed map: neighbour, distance, and the crossed hexside for two board-relative locations, including locations on different boards that share a seam. A reversed board keeps its own hex names. A seam hexside takes the merged terrain the map builder gives it. The per-board read stays as it is.
    4. *Entry facts across a seam.* The entry facts of steps 7 and 8 use the composed read, so adjacency and the crossed hexside are known across a seam. The terrain evidence of the Scenario A1 packages is unchanged: an entry into a board 01 building the reviewed cases cover is decided as before, wherever the mover starts, and any other entry across a seam is refused as outside the reviewed cases.
    5. *The Play page map.* The Play page draws the game's composed map with the units each viewer may see, reusing the board viewer's overlay (ASL-UNIT-071), and links each location in its tables to the map.
    6. *Acceptance.* U3 runs over a live game on a map with a reversed board, and U13 (section 14) passes.

    Designed and built in the [ASL Unit Composed Maps Design](<ASL Unit Composed Maps Design.md>).
13. **LOS, blocked or clear:** a read-only LOS check between two locations of a board or placed map, the first slice of ASL-MAP-082. It changes no game and opens no rule for play: it reports what VASL's LOS reports, so later Fire has a verified LOS to build on. In order:
    1. *The requirement.* ASL-MAP-082 changes from out of scope to this first slice: LOS between two locations, as VASL's `Map.LOS` computes it for a map without counters, overlays, or scenario-specific rules. The result is whether LOS is blocked, the hex where it is first blocked, and the range. Hindrances are counted and reported, never used as a DRM.
    2. *A VASL oracle.* The VASL hex-fact oracle tool gains an LOS mode that runs VASL's own `Map.LOS` on sampled pairs of locations of the fixture boards, including upper building levels and hills, and writes the results as fixtures. As for the hex facts, the fixtures hold derived results only, and no VASL code or data is committed.
    3. *The read.* The map read API (ASL-MAP-080) gains an LOS read over a board handle and over a placed map (step 12), in C#, reproducing VASL's results as the hex-fact derivation does. A read on a board that is not Verified or AuthoredValid is nondefinitive (ASL-MAP-044). A location on a board that is not placed, or not on the map, is refused.
    4. *The Studio.* The board viewer, on boards and composed maps, and the Play page gain an LOS tool: choose two locations, and see the line, whether it is blocked, and where.
    5. *Out of scope.* Hindrance DRM, counters (smoke, vehicles, wrecks, OBA), night and illumination, overlays and scenario-specific rules, bypass aiming points, and any use of LOS by a game action.
    6. *Acceptance.* U14 (section 14) passes.

    Designed and built in the [ASL Unit LOS Design](<ASL Unit LOS Design.md>).
14. **LOS slice 2: cellars, rooftops, depressions, and cliffs:** extend the read of step 13 to the rule groups that leave the most pairs unanswered. It stays read-only, reproduces VASL's `Map.LOS`, and keeps every other rule unsupported by name. Steps 14 to 16 were approved together on 2026-09-26, to be built without further review unless a decision is the user's. In order:
    1. *Cellars and rooftops.* Cellar and rooftop locations as source or target, with VASL's height adjustments (a rooftop half a level or a level lower, a cellar one level higher) and the cellar hexside rule (O6.3). Factories and roofless buildings stay unsupported until step 15.
    2. *Depressions.* Gullies, streams, and other depression terrain as VASL applies them: exiting and entering a depression (A6.3), LOS along a depression, depression hexsides, and the crest at a vertex (B19.51).
    3. *Cliffs.* Cliff hexsides, the blind-hex rule at cliffs (B10.23), and the cliff exceptions of the ground-level and terrain-height rules.
    4. *Oracle fixtures.* LOS fixtures, as in step 13, for boards with depressions and cliffs (among them boards 05, 09, 12, and 15), and a seam scenario that joins a depression board to a cliff board.
    5. *Acceptance.* U15 (section 14) passes.

    Designed and built in the [ASL Unit LOS Slice 2 Design](<ASL Unit LOS Slice 2 Design.md>).

15. **LOS slice 3: the remaining terrain:** extend the read to the rule groups step 14 leaves unsupported, reproducing VASL as before. In order:
    1. *Oracle fixtures.* LOS fixtures for boards chosen for these features: board 23 (bridges), board 51 (rowhouses and cellars), board rdx (factories), BFP board D (bocage), BFP board DW2b (hillocks), BFP board B (railroad), and board 96 (rubble).
    2. *Buildings and bridges.* Bridges and tunnels, rowhouse and factory walls, factories and their rooftops, roofless and gutted buildings, and rubble.
    3. *Hexside and rise terrain.* Bocage, hillocks (F6.4), railroad embankments, out-of-season orchards, and slopes (F2.3), where the fixture boards have them.
    4. *What cannot be checked.* Partial orchards and entrenchments appear on no fixture board. They stay unsupported by name until a board with them is found, since no answer is admitted without a VASL comparison.
    5. *Acceptance.* U16 (section 14) passes.

    Designed and built in the [ASL Unit LOS Slice 3 Design](<ASL Unit LOS Slice 3 Design.md>).
16. **The full VASL LOS result:** complete what the read reports, so Fire can later rely on it. Still no game action uses LOS. In order:
    1. *The hindrance breakdown.* The read reports what VASL's `LOSResult` keeps: the largest map hindrance at each range, and the point where the first hindrance was met, besides the total of step 13.
    2. *Hexside locations and aiming points.* A hexside location (a bypass location) as source or target, aimed at its LOS point or at its auxiliary LOS point, as VASL's `Map.LOS` takes them.
    3. *Oracle fixtures.* The oracle's LOS mode records the breakdown on every pair, and a hexside mode writes pairs from hexside locations, with both aiming points, on boards chosen for walls, depressions, and bocage.
    4. *LOS from a unit.* The Play page checks LOS from a unit the viewer can see, at its location, to a location, and shows the result with its hindrance breakdown; the board viewer shows the breakdown too.
    5. *Acceptance.* U17 (section 14) passes.

    Designed and built in the [ASL Unit LOS Result Design](<ASL Unit LOS Result Design.md>).
17. **The Fire source review:** a review-only step, like step 10, that registers and reviews the sources the first Fire slice needs and publishes a reviewed Fire case package. It changes no live play; Fire is built in later steps. The scope was decided by the user on 2026-09-26:
    - *Phases.* Prep Fire (PFPh) by the phasing side and Defensive Fire (DFPh) by the other side (A3.2 and A3.4). Defensive First Fire, Final Fire, Subsequent First Fire, and Residual FP stay out.
    - *Firers.* Good Order MMC in one Location firing together as one fire group (A7.5), optionally directed by a leader in that Location (A7.53). Multi-Location fire groups, support weapons, and MGs stay out.
    - *Rally.* Not in this step. Broken units stay broken until a later step reviews Rally.

    In order:
    1. *The chart supplement.* The A7 Infantry Fire Table (physical page 692) and the TEM column of the B Terrain Chart (page 698) lie outside the registered pages 6 to 253. They are registered as a bounded chart supplement, as the [back-matter boundary review](<LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>) did for the building entry rows of page 698: the PDF hash, the physical page, the extractor and the hash of its output, and a bounded transcription of only the cells the slice uses (the IFT result cells for every DR and FP column; the TEM of the terrain rows the fixture needs). The source registry and its verified subjects are unchanged.
    2. *Delegated transcription review.* xUnit assertions check the transcription against the pinned `pdftotext` extraction of each page: every IFT row and every admitted Terrain Chart row, normalized as the tests record, must hash to the value pinned from the extraction. The user spot-checks a rendered image of each page against a few cells. Chart glyphs that the text layer loses (flame and rubble marks, underlines) are read from the rendered page and stay out of the slice.
    3. *Rule fragments.* The prose rules the slice needs are verified against their physical pages, as step 10 did: among them A3.2 and A3.4 (the PFPh and DFPh), A6.7 (LOS Hindrance), A7.1 to A7.306 (fire, PBF, long range, Area Fire, resolution, and the results), A7.4 (target determination), A7.5 to A7.55 (fire groups and fire direction), A7.6 (TEM and Hindrance), A7.8 (pins), A7.9 (Cowering), A10.1 to A10.4 and A10.7 to A10.72 (morale and task checks, leaders checking first and their DRM, failure, Casualty MC, broken units, leadership), A12.14 (concealment loss by firing), A19.1 to A19.13 (ELR and Replacement, which a failed MC can cause), and the B terrain rules for the fixture's TEM.
    4. *Rulings.* A review document states what the fragments establish and asks the user to rule on what they leave open, among them: Cowering (which German 1st Line squads are subject to) and whether a 0 leader's direction prevents it, how a Hindrance DRM combines with TEM, the limits of the leadership DRM, which units a fire attack on a Location affects and in what order, concealment loss by firing, and whether ELR Replacement is admitted, since the scenario's ELR is not a registered source.
    5. *The package.* A reviewed Fire case package, digest-pinned like the earlier Scenario A1 packages, with no execution authority. It pins the chart supplement, the fragments, the rulings, and a case matrix. Its resolver takes a declared fire attack (phase, firers and their printed values from the reviewed catalog, the directing leader, range, the LOS result of step 16, the target Location's terrain and occupants, and a recorded DR) and concludes the FP column, each DRM, the final DR, the IFT result, and its effect on each target unit (eliminated, Casualty Reduced, broken, pinned, or unaffected, with any MC and its recorded DR). A case outside the reviewed scope concludes Indeterminate or Abstained with its reasons. Missing, contrary, extra, and stale facts refuse a conclusion, as in the earlier packages.
    6. *Exclusions.* Everything the scope above leaves out, and also: SW, MG, and ordnance fire; fire at or by vehicles; Dummies; ELR Replacement unless a ruling admits it; Encirclement; Rally; Heat of Battle and Berserk; snipers (SAN); smoke, night, and weather; and any use of the package by a game action.
    7. *Acceptance.* U18 (section 14) passes.

    Its review is recorded in a review document, written before the package is published. Printed counter values come only from the reviewed catalog or from the user.

    Done in the [Scenario A1 Fire Review](<Scenario A1 Fire Review 2026-09-26.md>). The user ruled that any directing leader prevents Cowering, that the Hindrance DRM comes from the LOS read for same-level fire only, that LLMC and LLTC are admitted, and that the case declares the target side's ELR; leader wounds and the Russian HS stay Indeterminate. The package `scenario-a1-fire` resolves a declared attack to its FP column, DRM, IFT result, and each target unit's effect.

18. **Fire in live play:** wire the Fire package of step 17 into live games, so a side may fire in its PFPh or DFPh through a governed action. The user decided on 2026-09-26 that fire commits only when every outcome the dice can reach is decided, that the fire markers are drawn states, that the LOS read reports the terrain of each hindrance, and that setup records the scenario month. In order:
    1. *Closing the undecided branches.* A review, like step 17, of what leaves a reachable outcome Indeterminate: ELR Replacement and Disruption (A19.12, A19.13, p. 86), the Russian HS for Casualty Reduction (A7.302, p. 55), and wounds (A17, p. 85). The Replacement units and the Russian HS enter the catalog with printed values from the user. The Fire package is revised and republished with the new cases; its earlier digest stays in the record.
    2. *Hindrance terrain.* The LOS read (ASL-MAP-082) also reports the terrain of the hex behind each entry of the hindrance breakdown, as VASL's `Map.addHindranceHex` meets it, verified on the oracle fixtures. A Hindrance DRM is then attributed when every counted hex is brush, or grain in a declared month from June to September, each adding 1; anything else stays unattributed.
    3. *Game model.* The vocabulary gains `asl:prep-fire` and `asl:final-fire` (asl@1.5.0), drawn by the display. Setup may record the scenario month. New events record a fire attack with its declared facts, the rolls it drew, and its arithmetic, and each MC, NTC, LLMC, or LLTC with its arithmetic, as `task-check` does; the unit effects use the existing condition, elimination, and lineage events. Replay recomputes the arithmetic through the package's calculator and refuses a record that disagrees. The Final Fire state is cleared at the end of the DFPh (A3.4), Prep Fire at the end of the AFPh (A3.5), and pins in the CCPh (A3.8).
    4. *The fire action.* `asl.game.fire` takes the firers in one Location, an optional directing leader, and the target Location. The planner derives every fact from the live game and the map read (phase and side, range, levels, target terrain, LOS and its attributed Hindrance, each side's ELR, the month), and refuses anything the package abstains on. Before any roll, a reachability check over the package's calculator must show every outcome the dice can reach decided; otherwise the attack is refused. The build function then draws each roll the package asks for, in order, and nothing else (DICE-07 to DICE-12). A second attack from the same Location on the same target in the same phase is refused (A7.55), and so is fire by a unit or direction by a leader already marked.
    5. *Visibility.* Rolls are public. A concealed target unit stays concealed on a result of no effect, so its identity is never shown; any other result reveals it (A12.14). A firer or director that loses concealment is revealed. A refusal because a hidden unit is in the target Location tells the firing side that one is there; that is accepted as a known limitation while the Studio's single user acts for both sides, like the election probe of step 11.
    6. *The Play page.* A fire panel chooses the firers, the director, and the target, shows the facts, the FP arithmetic, the DRM, the Final DR, and each unit's effect in each side's view, and draws the fire markers and pins.
    7. *Acceptance.* U19 and U20 (section 14) pass.

    Done: part 1 in the [Scenario A1 Fire Review](<Scenario A1 Fire Review 2026-09-26.md>), section "Revision at unit step 18", and parts 2 to 7 in the [ASL Unit Fire in Live Play Design](<ASL Unit Fire in Live Play Design.md>). A record that leaves a concealed target concealed is the target side's, and a public report gives the firing side its arithmetic; U19 and U20 pass in the Play tests and on the Play page.

Later candidates, not yet sequenced: revisiting the Java VASL oracle tool, which the user allowed on 2026-09-26 only to generate test fixtures, for now; multi-user play, which must first close the lone-SMC election probe of step 11; the lone SMC's options and immediate CC after an OVR (A4.151 and A4.152), once the IFT and CC tables are registered; Rally, which step 17 leaves out; scenario OB and SSR checks at setup, which need the scenario cards as a registered source; and a read-only VASL saved-game import, which needs a format and licensing review first.

19. **Rally:** broken units rally in the RPh through a governed action, resolved by a reviewed Rally package, with DM and Fate. Designed in the [ASL Unit Rally and Fire Extensions Plan](<ASL Unit Rally and Fire Extensions Plan.md>), section 4; its rulings, like those of steps 20 to 23, were accepted by the user on 2026-09-26, and what they leave out is in the [ASL Unit Backlog](<ASL Unit Backlog.md>). In order:
    1. *Heat of Battle in the Fire package.* An Original MC DR of 2 calls for Heat of Battle (A15.1, p. 83), which the Fire package does not decide. The package and its reachability walk change as ruling R0.2 of the plan decides.
    2. *Sources and rulings.* The rule fragments A3.1 (p. 47), A10.4 (p. 66), A10.6 to A10.64 and A10.7 (p. 68), A10.71 and A10.72 (p. 69), A12.141 (p. 78), A17.11 and A17.3 (p. 85), and A19.12 (p. 86) verified against their pages, and the plan's rulings R19.1 to R19.10 recorded in a review document.
    3. *The package.* `scenario-a1-rally`, digest-pinned with no execution authority, resolves a declared attempt (the unit, the rallying leader or Self-Rally, DM, terrain, and one recorded DR) to rallied, not rallied, or Fate, with its arithmetic, and refuses what the rulings leave out.
    4. *DM.* A unit that breaks gains DM, with the other sources R19.1 admits; DM is removed at the end of every RPh (A10.62, p. 68).
    5. *The action.* `asl.game.rally` takes a broken unit and an optional leader in the RPh, allows one attempt per unit per Player Turn, draws its roll, and records the attempt with its arithmetic and effects; replay recomputes it through the package.
    6. *Visibility and the Play page.* Rally DRs are public; a concealed unit's rally follows A12.141. An RPh panel offers each side's broken units, the leader choices, and the DRM.
    7. *Acceptance.* U21 and U22 (section 14) pass.
20. **Advancing Fire and multi-Location fire groups:** the ATTACKER fires in the AFPh at half FP (A3.5, p. 47; A7.24, p. 55), with Assault Fire (A7.36, p. 56), and a fire group may span ADJACENT Locations (A7.5 to A7.531, p. 57; A.8, p. 43), with each member's own range, PBF, and Long Range and the group's worst-case DRM. The Fire package is revised and republished, the map read gains ADJACENT, and the fire action takes firers in several Locations. Designed in the plan, section 5, with rulings R20.1 to R20.6. Acceptance: U23.
21. **Fire at hidden units and Dummies:** the Fire package resolves hidden units as concealed (A12.3, p. 80) and Dummy stacks (A12.14, p. 77) with the Concealment Table from registered page 107, setup may place Dummies, and a side may fire at a Location where it sees nothing, so a refusal no longer reveals a hidden unit. Designed in the plan, section 6, with rulings R21.1 to R21.5. Acceptance: U24.
22. **Movement and fire during it:** Infantry move hex by hex at the terrain's MF cost (A4.1 to A4.62, pp. 48 to 51; the B Terrain Chart, p. 160), and after each MF expenditure the DEFENDER may fire. Defensive First Fire with FFNAM and FFMO (A8.1 to A8.15, p. 59), Subsequent First Fire and FPF (A8.3, A8.31, p. 61), Residual FP (A8.2 to A8.26, pp. 60 to 61), and First-Fire-marked units in the DFPh (A8.4, p. 61). The building entry of steps 7 to 11 becomes one case of the move. Designed in the plan, section 7, with rulings R22.1 to R22.8. Acceptance: U25 and U26.
23. **Support weapons and MGs on the IFT:** SW definitions in the catalog with printed values from the user, equipment instances that name their definition, a colored die in the dice record, SW firepower in a fire group with the inherent FP it costs (A7.35 to A7.353, p. 56), MG usage, multiple ROF, and Sustained Fire (A9.1 to A9.3, pp. 62 to 63), malfunction (A9.7, A9.71, p. 65), and Repair in the RPh (A9.72, p. 65), from the Support Weapons Chart on registered page 108. Designed in the plan, section 8, with rulings R23.1 to R23.5. Acceptance: U27.
24. **Ordnance:** a review step and a live step for Chapter C (pp. 162 to 191) with its charts on registered pages 189 to 191, starting from a Gun firing HE at Infantry. It follows step 23. Outlined in the plan, section 9. Acceptance: U28.
25. **Vehicles:** vehicle definitions, placement and movement, and fire by and at vehicles (Chapter D, pp. 192 to 221), starting from vehicular MGs and Infantry fire at an unarmored vehicle on the IFT Vehicle line (A7.308, p. 55). It follows step 24. Outlined in the plan, section 10. Acceptance: U29.

    Steps 19 to 23 are to be built in one implementation pass after one review sitting, with the Windows and Linux checks run once at its end (the plan, section 11). Steps 24 and 25 are later passes.

    Done for steps 19 to 23 together: the review sitting in the [Scenario A1 Rally Review](<Scenario A1 Rally Review 2026-09-26.md>) and the [Scenario A1 Fire Review](<Scenario A1 Fire Review 2026-09-26.md>), section "Revision at unit steps 19 to 23", and the game model, actions, and Play page in the [ASL Unit Rally and Fire Extensions in Live Play Design](<ASL Unit Rally and Fire Extensions in Live Play Design.md>). U21 to U27 pass in the Play tests, and on the Play page for a rally, a MG in a fire group removing a Dummy, and a move under Defensive First Fire with Residual FP. Recorded deviations: Heat of Battle and Leader Creation are not taken (R0.2, R19.4), and a moving stack is not split when a mover breaks or pins.

26. **Stack splitting:** a moving stack may break up and continue separately, and no other unit moves until every member of the stack has ended its MPh (A4.2, p. 49); a member broken or pinned by fire ends its move, and a member Reduced to a HS is followed by the HS. Designed in the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), section 4, ruling R26.1. Acceptance: U30.
27. **Leader Creation:** the first MMC Self-Rally of a side's own RPh that rolls an Original 2 rallies the unit and calls for a Leader Creation dr (A18.11, A18.2, p. 85), which may create a 6+1 to 8-1 leader of the unit's nationality. Designed in the plan, section 5, rulings R27.1 and R27.2. Acceptance: U31.
28. **Heat of Battle, heroes, and Battle Hardening:** an Original MC or Rally (not Self-Rally) DR of 2 calls for a Heat of Battle DR with its DRM (A15.1, p. 83): a hero is created or a leader made heroic (A15.2 to A15.24), a unit is Battle Hardened or made Fanatic (A15.3, A10.8, A25.25), and Berserk and Surrender are recorded as not taken until step 30. Catalog 1.3.0 adds the leader grades, heroes, and the classes Battle Hardening needs. Designed in the plan, section 6, rulings R28.1 to R28.6. Acceptance: U32.
29. **Close Combat:** Infantry against Infantry in one Location in the CCPh, with Ambush, Melee, and leaders (A11, pp. 72 to 76), and Advance into an enemy Location in the APh (A4.7). Designed in the plan, section 7, ruling R29.1. Acceptance: U33.
30. **Berserk, Surrender, and capture:** Berserk (A15.4 to A15.46, pp. 83 to 84) and Surrender with capture and prisoners (A15.5, p. 84; A20.1 to A20.5, pp. 86 to 87); the Heat of Battle deviation is removed. Designed in the plan, section 8, ruling R30.1. Acceptance: U34.

    Steps 26 to 30, then 24 and 25, are built in four passes under the plan, section 3, with a second-pass reviewer in place of the user's review; each pass merges when the local test run and the Docker check pass.

    Done for steps 26 to 28 together (pass 1): the review in the [Scenario A1 Heat of Battle Review](<Scenario A1 Heat of Battle Review 2026-09-27.md>), with the referee's and the table player's findings, catalog 1.3.0, and the game model, records, and Play page in the [ASL Unit Deviations Pass 1 Design](<ASL Unit Deviations Pass 1 Design.md>). U30 to U32 pass in the Units and Play tests. Step 28 is partial by design: Berserk and Surrender are recorded as not taken until step 30. What the pass leaves out is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1 and 10.

    Done for steps 29 and 30 together (pass 2), with Fix A (units that Prep Fired may not move, A3.3) and Fix B (the Play page's fire selection and Director list): the review in the [Scenario A1 Close Combat Review](<Scenario A1 Close Combat Review 2026-09-27.md>), with the referee's and the table player's findings, the `scenario-a1-close-combat` package, the revised Fire and Rally packages, and the game model, records, and Play page in the [ASL Unit Deviations Pass 2 Design](<ASL Unit Deviations Pass 2 Design.md>). Rulings R29.1 to R29.19 and R30.1 to R30.8 are in the plan, section 11. U33 and U34 pass in the Units and Play tests. The Heat of Battle deviation of step 28 is removed. What the pass leaves out is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 1 and 11.

    Done for step 24 (pass 3): the review in the [Scenario A1 Ordnance Review](<Scenario A1 Ordnance Review 2026-09-27.md>), with the referee's and the table player's findings, catalog 1.4.0 with two Guns and two crews, the `scenario-a1-ordnance` package, the revised Fire, Rally, and Close Combat packages, and the game model, records, and Play page in the [ASL Unit Deviations Pass 3 Design](<ASL Unit Deviations Pass 3 Design.md>). Rulings R24.1 to R24.8 are in the plan, section 11. U28 passes in the Units and Play tests. What the pass leaves out is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 12.

    Done for step 25 (pass 4): the review in the [Scenario A1 Vehicle Review](<Scenario A1 Vehicle Review 2026-09-27.md>), with the referee's and the table player's findings, catalog 1.5.0 with two trucks and a halftrack, vocabulary 1.8.0, the revised Fire package (the IFT Vehicle line, Collateral Attacks on a CE crew, and a vehicle's AAMG) with the Rally, Close Combat, and Ordnance packages republished for the catalog digest, and the game model, records, and Play page in the [ASL Unit Deviations Pass 4 Design](<ASL Unit Deviations Pass 4 Design.md>). Rulings R25.1 to R25.10 are in the plan, section 11. U29 passes in the ScenarioA1, Play, and Studio tests. What the pass leaves out is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 14.

    Done for backlog pass 5 (deviations and small items): the review in the [Scenario A1 Backlog Pass 5 Review](<Scenario A1 Backlog Pass 5 Review 2026-09-27.md>), with the referee's and the table player's findings, the revised Fire, Rally, Close Combat, and Ordnance packages, vocabulary 1.9.0, and the game model, records, and Play page in the [ASL Unit Backlog Pass 5 Design](<ASL Unit Backlog Pass 5 Design.md>): CX and Double Time, the captor's choice at a surrender and Massacre, owner options as pending choices, the second Heat of Battle DR, Acquisition on units, vehicle Motion, Recall and Abandonment, and the page fixes. Rulings R5.1 to R5.20 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 15.

    Done for backlog pass 6 (vehicles, part 2): the review in the [Scenario A1 Backlog Pass 6 Review](<Scenario A1 Backlog Pass 6 Review 2026-09-28.md>), with the referee's and the table player's findings, the revised Fire and Ordnance packages, vocabulary 1.10.0, and the game model, records, and Play page in the [ASL Unit Backlog Pass 6 Design](<ASL Unit Backlog Pass 6 Design.md>): an AFV's and a wreck's cover and Hindrance, wrecks and burning wrecks, Residual FP against vehicles, vehicle concealment and entry into concealed Locations, a vehicle's Defensive and Bounding First Fire, and vehicle MG repair. Rulings R6.1 to R6.10 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 16.

    Done for backlog pass 7 (armor): the review in the [Scenario A1 Backlog Pass 7 Review](<Scenario A1 Backlog Pass 7 Review 2026-09-28.md>), with the referee's and the table player's findings, catalog 1.6.0 with the PzKpfw IIIH and the T-34 M41, the revised Fire, Rally, Close Combat, and Ordnance packages, vocabulary 1.11.0, and the game model, records, and Play page in the [ASL Unit Backlog Pass 7 Design](<ASL Unit Backlog Pass 7 Design.md>): the Vehicle Target Type, hit location and Target Facing, To Kill with AP, APCR, HEAT, and HE, Special Ammunition and its Depletion Numbers, a tank's MA fire and turret, Shock and the Unconfirmed Kill, and the crews' TC and survival. Rulings R7.1 to R7.12 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 17.

    Done for backlog pass 8 (Guns, part 2): the review in the [Scenario A1 Backlog Pass 8 Review](<Scenario A1 Backlog Pass 8 Review 2026-09-28.md>), with the referee's and the table player's findings, catalog 1.7.0, the revised Fire, Rally, Close Combat, and Ordnance packages, vocabulary 1.12.0, and the game model, records, and Play page in the [ASL Unit Backlog Pass 8 Design](<ASL Unit Backlog Pass 8 Design.md>): Defensive First Fire by Guns and tanks, First Fire and Intensive Fire, Guns and crews as targets, crews' own fire, concealed Guns, turning, pushing, abandoning, and towing Guns, Bore Sighting, Cases E and H, the C2.6 limit, bearings across boards, and overstacking. Rulings R8.1 to R8.12 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 18.

    Done for backlog pass 9 (mortars, SMOKE, and anti-tank weapons): the review in the [Scenario A1 Backlog Pass 9 Review](<Scenario A1 Backlog Pass 9 Review 2026-09-28.md>), with the referee's and the table player's findings, catalog 1.8.0 with two light mortars, the revised Fire, Rally, Close Combat, and Ordnance packages, vocabulary 1.13.0, and the game model, records, and Play page in the [ASL Unit Backlog Pass 9 Design](<ASL Unit Backlog Pass 9 Design.md>): light mortars on the Area Target Type with Spotters and directing leaders, SMOKE grenades with their Hindrance, and the Panzerfaust with its PF Check and usage limit. Rulings R9.1 to R9.9 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 19; its deviations are in section 1. Backlog pass 9b (2026-09-28) adds a manufactured ATR and Panzerschreck (catalog 1.9.0, vocabulary 1.14.0; rulings R9.10 and R9.11), in the same review and design.

    Done for backlog pass 10 (movement and terrain): the review in the [Scenario A1 Backlog Pass 10 Review](<Scenario A1 Backlog Pass 10 Review 2026-09-28.md>), with the referee's and the table player's findings, the pass 10 Terrain Chart rows, the revised Fire package, and the game model, records, and Play page in the [ASL Unit Backlog Pass 10 Design](<ASL Unit Backlog Pass 10 Design.md>): walls, hedges, hills, marsh, rubble, and building levels on entry and as TEM, Wall and Height Advantage, fire across levels with PBF and TPBF, Bypass, Hazardous Movement, the Road and leader bonuses, Minimum Move, concealed movement, entry into concealed enemy Locations, Snap Shots, and every berserk charge route decided. Rulings R10.1 to R10.15 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 20; its deviations are in section 1.

    Done for backlog pass 11 (vehicle movement and OVR): the review in the [Scenario A1 Backlog Pass 11 Review](<Scenario A1 Backlog Pass 11 Review 2026-09-28.md>), with the referee's and the table player's findings, the revised Fire and Close Combat packages, and the game model, records, and Play page in the [ASL Unit Backlog Pass 11 Design](<ASL Unit Backlog Pass 11 Design.md>): Reverse movement, VBM, ESB, Mechanical Reliability, Minimum Move and ALL entries, vehicle stacking and D2.6, vehicle movement over pass 10's terrain with Bog and Bog Removal, the OVR, the A12.41 choice, CC Reaction Fire, and sequential CC with vehicles, with the PAATC and capture. Rulings R11.1 to R11.18 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 21; its deviations are in section 1.

    Done for backlog pass 12 (fire extensions): the review in the [Scenario A1 Backlog Pass 12 Review](<Scenario A1 Backlog Pass 12 Review 2026-09-28.md>), with the referee's and the table player's findings, the revised Fire package, and the game model, records, and Play page in the [ASL Unit Backlog Pass 12 Design](<ASL Unit Backlog Pass 12 Design.md>): Opportunity Fire, fire at a blocked LOS, directed and mixed FPF, pinned and unpinned movers in one attack, split fire and a leader's MG, the concealment gained as a Player Turn ends, Spraying Fire, Fire Lanes, fire into a Melee and at prisoners, Guards' and berserk units' fire, and Encirclement. Rulings R12.1 to R12.12 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5. What the pass leaves out is in the backlog, section 22.

    Done for backlog pass 13 (Rally, Rout, and support weapons): the review in the [Scenario A1 Backlog Pass 13 Review](<Scenario A1 Backlog Pass 13 Review 2026-09-28.md>), with the referee's and the table player's findings, the revised Fire and Rally packages, and the game model, records, and Play page in the [ASL Unit Backlog Pass 13 Design](<ASL Unit Backlog Pass 13 Design.md>): the RtPh with Low Crawl, Interdiction, Failure to Rout, and surrender; DM from ADJACENT enemies, at the start of the RtPh, and retained; Deployment and Recombining; SW transfer, drop, and Recovery; dismantling; and captured MG. What it defers is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 23.

    Done for backlog pass 14 (Close Combat and capture, part 2): the review in the [Scenario A1 Backlog Pass 14 Review](<Scenario A1 Backlog Pass 14 Review 2026-09-28.md>), with the referee's and the table player's findings, the revised Close Combat package, and the game model, records, and Play page in the [ASL Unit Backlog Pass 14 Design](<ASL Unit Backlog Pass 14 Design.md>): Hand-to-Hand, concealed and hidden units, Dummies, and TI units in CC, capture attempts, Unarmed units, Guards, prisoners' escape and rearming, transferring and abandoning prisoners, Infiltration, Ambush Withdrawal, overstacking, a Disrupted unit's surrender to an advance, Field Promotion by BPV, and mandatory CC. What it defers is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 24.

    Done for backlog pass 15 (special units and nationalities): the review in the [Scenario A1 Backlog Pass 15 Review](<Scenario A1 Backlog Pass 15 Review 2026-09-29.md>), with the referee's and the table player's findings, the revised Fire, Rally, Close Combat, and Ordnance packages, catalog 1.10.0, and the game model, records, and Play page in the [ASL Unit Backlog Pass 15 Design](<ASL Unit Backlog Pass 15 Design.md>): FT, DC (Placed and Thrown), and MOL; Snipers; Commissars and NKVD Field Promotion; Allied Troops; underscored Morale Factors; Green MMC; a hero's MG and a hero created concealed; American, British, Italian, Finnish, and French units with their national rules for the Heat of Battle, Leader Creation, Replacement, Battle Hardening, Cowering, and Self-Rally; and a berserk unit's kept SW. What it defers is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 25.

    Done for backlog pass 16 (night and weather): the review in the [Scenario A1 Backlog Pass 16 Review](<Scenario A1 Backlog Pass 16 Review 2026-09-29.md>), with the referee's and the table player's findings, the revised Fire, Rally, Close Combat, and Ordnance packages, and the game model, records, and Play page in the [ASL Unit Backlog Pass 16 Design](<ASL Unit Backlog Pass 16 Design.md>): the night and weather SSRs; the NVR, Illumination, Gunflashes, and Starshells; the Low Visibility DRM; night rout, DM, concealment, movement, Ambush, SAN, and Recovery; the Wind Change DR with the NVR, rain, snowfall, and Gusts; Mud, rain, and snow for Infantry and vehicles; and Extreme Winter. What it defers is in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 26; the scenario cards are pass 17.

    Done for backlog pass 17 (scenario cards, the presentation only, by the user's ruling of 2026-09-29): the review in the [Scenario A1 Backlog Pass 17 Review](<Scenario A1 Backlog Pass 17 Review 2026-09-29.md>), with the referee's and the table player's findings, and the [ASL Unit Backlog Pass 17 Design](<ASL Unit Backlog Pass 17 Design.md>): the scenario-card format with its reader and validation, The Guards Counterattack and Gambit adapted from legacy cards of The General to the registered rulebook, catalog 1.11.0 (six counters, three manufactured under R0.3), and the Studio's Scenario cards page. Card-driven play (setting up a game from a card, reinforcements entering, and evaluating the Victory Conditions) and the full DYO purchase are in the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 27.

## 14. Acceptance scenarios

- **U1, definition lookup.** Given the registered catalog, a lookup for a squad of a given nationality, class, and date returns its printed values with their source, and a lookup outside its applicability returns an explicit miss.
- **U2, lineage.** Given a squad that is reduced and later recombined with another half-squad, the events show which instances each came from, and the projection at every revision is consistent.
- **U3, position on a composed map.** Given a unit at `bd21:N5` on a map with bd21 reversed, its position stays `bd21:N5` and the map places it in the correct map hex and location.
- **U4, perspective.** Given a concealed defender, the attacker's projection shows sealed presence only, the adjudicator's shows the defender, and the display for the attacker never receives the defender's identity.
- **U5, stale conclusion.** Given a conclusion computed at revision 12, a later reveal event makes it stale and a new read is required.
- **U6, nondefinitive read.** Given a location whose occupants are only partly known to the reader, the Scenario A1 read returns a nondefinitive result rather than a sole defender.
- **U7, forced back.** Given a live game in which a Good Order squad attempts, in its MPh, to enter an adjacent building location holding one hidden enemy squad, the committed events show the attempt, the reveal it caused, and the squad in its previous location with the attempted MF counted there and its MPh ended; the attacker's view shows the revealed squad, and confirming the same attempt again changes nothing.
- **U8, occupied refusal.** Given a live game in which the target location holds a known, unconcealed enemy squad, the entry is refused with the A4.14 conclusion as its reason, and the game's revision is unchanged.
- **U9, Random Selection.** Given a live game in which a squad attempts, in its MPh, to enter a building location holding two concealed enemy squads, one committed roll records one dr per unit in the event log. The revealed squad forces the mover back as in U7. Replaying the game reproduces the reveal without drawing dice, and confirming the same attempt again returns the recorded roll.
- **U10, declined OVR.** Given a live game in which the only unit revealed by an entry is one enemy SMC, the attempt is pending and the phase cannot advance. An election is refused and nothing changes. A decline commits the forced back of U7, citing the reviewed delegation.
- **U11, failed OVR NTC.** Given a live game in which an entry reveals a lone concealed SMC and another concealed squad is in the location, an election with at least four MF left records one NTC roll and a task check that fails. The mover is forced back with the ordinary 2 MF, the other squad stays concealed, and replaying the game draws no dice.
- **U12, passed OVR NTC.** In the same game, an election whose NTC passes records the NTC roll and task check, then a Random Selection roll that reveals the other squad. The mover is forced back with the ordinary 2 MF, citing the OVR NTC package. Confirming the same attempt again returns the recorded rolls.
- **U13, entry across a seam.** Given a live game on two placed boards, a squad on the other board, in a hex that shares a seam with a board 01 building the reviewed cases cover, may enter that building: the entry facts show it adjacent with its crossed hexside, and the entry commits as it would from board 01. An entry across the same seam into a location the reviewed cases do not cover is refused with its reasons, and nothing changes.
- **U14, LOS.** Given the LOS oracle's pairs of locations on its fixture boards, the LOS read agrees with VASL on every pair: whether LOS is blocked, the hex where it is first blocked, and the range. The same pairs agree on a placed map of those boards, and a read on a board whose status is not Verified or AuthoredValid is nondefinitive.
- **U15, LOS over depressions and cliffs.** On every LOS fixture of steps 13 and 14, the read agrees with VASL on every pair it answers; on boards 01 and 11 it answers every pair; and every unanswered pair on the new fixtures names a rule that step 15 reproduces. The answered counts are pinned.
- **U16, LOS over the remaining terrain.** On every LOS fixture of steps 13 to 15, the read agrees with VASL on every pair it answers, and every unanswered pair names a rule that no fixture board exercises (partial orchards, entrenchments) or a situation the as-built notes list with its reason. The answered counts are pinned.
- **U17, the full LOS result.** On every LOS fixture, every answered pair agrees with VASL on the hindrance at each range and the first hindrance point as well as the result of U14; on the hexside fixtures, every pair from a hexside location, aimed at either point, agrees where it is answered; and on the Play page, LOS from a unit the viewer can see is checked and drawn with its breakdown.
- **U18, the Fire package.** The chart supplement's transcription reproduces the pinned `pdftotext` extraction of pages 692 and 698 for every admitted row, and a changed cell or hash is rejected. Given a declared PFPh attack by two Good Order squads in one Location, directed by a leader, against an adjacent Location with a clear LOS, the package concludes the FP column (with PBF), the TEM and leadership DRM, the IFT result for a recorded DR, and each target unit's effect with its MC, all citing the pinned fragments and rulings. The same attack with an unreviewed element (a support weapon, a second firing Location, Final Fire) concludes Indeterminate or Abstained with its reason, and changed or stale facts refuse a conclusion.
- **U19, Fire in live play.** Given a live game in its PFPh in which two Good Order squads and a leader of the phasing side share a Location adjacent to an enemy-occupied building with a clear same-level LOS, a fire action directed by the leader commits: the events record the attack's facts, the IFT roll, the arithmetic, and each target unit's MC with its roll and result; the firers and the leader are drawn with the Prep Fire state, and any broken or pinned target is drawn so in each side's view. Replaying the game draws no dice and reproduces the same state; confirming the same attempt again returns the recorded rolls. The Prep Fire state is gone after the AFPh, and pins after the CCPh.
- **U20, Fire refusals.** In the same game, a second attack from the same Location on the same target, fire by a unit already marked, fire in the MPh, and an attack from which an undecided outcome is reachable are each refused before any roll with their reason, and the game's revision is unchanged.

- **U21, Rally.** Given a live game in an RPh in which a broken squad under DM shares a building Location with a Good Order leader, a rally attempt by the leader commits: the record shows the DR, the leadership DRM, +4 for DM, -1 for the building, the Final DR against the broken Morale Level, and the result; a rallied squad is drawn in Good Order in each side's view. An Original 12 Casualty Reduces the squad. DM is gone after the RPh. Replaying draws no dice, and confirming the same attempt again returns the recorded roll.
- **U22, Rally refusals.** In the same game, a second attempt by the same unit in the Player Turn, a Self-Rally by a unit without the capability, a Self-Rally by a Disrupted unit, a rally outside the RPh, and a rally with a leader in another Location are each refused before any roll with their reason, and the revision is unchanged.
- **U23, Advancing Fire and fire groups.** An AFPh attack by squads that did not Prep Fire is resolved at half FP, an underscored squad adds its Assault Fire FP, and a squad marked Prep Fire is refused. A fire group in two ADJACENT Locations resolves with each member's own PBF and Long Range and the worst Hindrance, and takes a leader's DRM only when a directing leader is in each Location.
- **U24, hidden units and Dummies.** An attack on a Location holding a hidden squad commits, and its result is shown to the firing side only as the arithmetic unless the squad is revealed. An attack on a Dummy stack removes it on a PTC or worse, and its removal is public. An attack on an empty Location commits with its roll and markers.
- **U25, Defensive First Fire.** A squad moving into Open Ground in LOS of a DEFENDER's squads is stopped after the MF expenditure until the DEFENDER fires or passes; the DFF record shows FFNAM and FFMO; a broken or pinned mover's move ends; the firers are marked First Fire and may fire again only as Subsequent First Fire or FPF, as Final Fire.
- **U26, Residual FP.** A DFF attack leaves Residual FP at the value A8.2 and A8.26 give; the next unit entering that Location is attacked by it first, without a fire group, and the marker is gone at the end of the MPh.
- **U27, support weapons.** A squad firing its possessed LMG in a fire group adds the MG's FP and keeps its inherent FP; an Original DR at the MG's breakdown number malfunctions it; a multiple ROF result gives the MG a further attack; Repair in the RPh succeeds on a dr at or below the repair number and eliminates the weapon on a 6.
- **U28, ordnance.** In the PFPh a German 7.5cm leIG 18 manned by its crew fires HE at a Russian squad three hexes away on the Infantry Target Type: the record shows the black Basic TH# 8, the To Hit DR with its colored die and DRM, the hit, and the IFT attack on the 12 FP column with no TEM on the Effects DR. A colored dr of 2 or less keeps its ROF of 2 and it fires again with the -1 Acquisition DRM; after a colored 3 it fires no more that Player Turn, nor in the AFPh. A shot at a target outside its Covered Arc turns the Gun with Case A and a lower ROF. A Final DR below half the Modified TH#, or an Original 2 and a subsequent dr of 1 or at most half of it, is a Critical Hit on the doubled FP with the TEM reversed. Replay reproduces each shot without dice.
- **U29, vehicles.** A German Opel truck set up with its VCA east spends its MP one expenditure at a time: Start (1 MP), entering a hex its VCA points at over Open Ground (4 MP), and Stop (1 MP), with the DEFENDER's window after each; it may not end its move in Motion while it has the MP to Stop or move on. A Russian squad's Defensive First Fire at it resolves on the IFT Vehicle line of the attack's column with no TEM, FFMO, or FFNAM, and a Final DR equal to the Kill Number immobilizes it. The SPW 251/1's AAMG fires 3 FP on the IFT with no Cowering, keeps its ROF on a colored 1, and fires no third time. Fire at the CE halftrack leaves it unharmed and attacks its crew with the +2 CE DRM: a failed MC Stuns it, which buttons it up; at the end of that Player Turn it is Stun +1 and still BU, and its owner may expose it again in its MPh. Replay reproduces each record without dice.
- **U30, stack splitting.** A two-squad stack loses one squad to Defensive First Fire and the other moves on; the stack's move ends only when both have ended, and another stack is refused until then.
- **U31, Leader Creation.** A first MMC Self-Rally on 1, 1 rallies the squad and creates the leader the Leader Creation dr gives; replay recreates it without dice.
- **U32, Heat of Battle.** An Original 2 on a MC or a leader's rally is followed by its Heat of Battle DR; a Final DR of 4 creates a hero, a 7 Battle Hardens the unit, and a 5 or 6 does both; each record shows the DR and its DRM.
- **U33, Close Combat.** In the APh a German squad advances into a Location holding a Known Russian squad; in the CCPh both attack in one simultaneous round, each record showing the odds column, the DRM, the Final DR, and the Kill Number; with both surviving, the phase change sets both in Melee, and neither may fire or move until the Melee ends. An Ambush dr 3 lower than the other's makes that side the ambusher, whose round comes first, and the phase is held until the ambushed side's round. A broken unit left in Melee must withdraw before the CCPh ends, and a Disrupted one is eliminated. Replay reproduces each round without dice.
- **U34, Berserk and Surrender.** A Final Heat of Battle DR of 10 makes a squad berserk; at the start of its side's next MPh it must charge along a shortest route to the nearest Known enemy unit in its LOS, may not end its move while it can still charge, and attacks in CC with no leadership DRM. The same DR with no Known enemy unit in LOS Battle Hardens it instead (A15.44). A Final DR of 12 next to a Good Order enemy squad breaks and Disrupts it and holds the game until that squad's side takes it prisoner; the prisoner's SW stay behind, and it moves with its Guard.

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
