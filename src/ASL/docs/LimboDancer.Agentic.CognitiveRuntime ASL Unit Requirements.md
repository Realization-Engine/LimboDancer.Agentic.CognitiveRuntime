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

    Designed in the [ASL Unit LOS Result Design](<ASL Unit LOS Result Design.md>).

Later candidates, not yet sequenced: revisiting the Java VASL oracle tool, which the user allowed on 2026-09-26 only to generate test fixtures, for now; multi-user play, which must first close the lone-SMC election probe of step 11; the lone SMC's options and immediate CC after an OVR (A4.151 and A4.152), once the IFT and CC tables are registered; Fire, on the LOS of step 13, once the IFT is registered; scenario OB and SSR checks at setup, which need the scenario cards as a registered source; and a read-only VASL saved-game import, which needs a format and licensing review first.

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
