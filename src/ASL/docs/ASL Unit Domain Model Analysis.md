# ASL Unit Domain Model Analysis

**Status:** Long-range analysis, deferred while the [unit map rendering slice](<ASL Unit Map Rendering Slice.md>) is implemented; no implementation or rule admission implied

**Date:** 2026-09-24

**Repository baseline:** `main@41d824b`

**Source:** *ASL Rulebook*, `eASLRB_v3_01.pdf`, 716 physical PDF pages. Page numbers below are **physical PDF page numbers**, followed by printed rule numbers. The existing Markdown conversion begins with the TOC, index, and Chapters A–E (PDF pp. 6–253); the full unit ontology may use all applicable chapters, charts and counter sources after exact registration and review. See [ASL Ontology Transformation Specification](<LimboDancer.Agentic.CognitiveRuntime ASL Ontology Transformation Specification.md>), §5.

## Purpose and finding

Provide an inventory of unit-related rules and a candidate domain model for the ASL solution. The rulebook distinguishes counters, the people or equipment they represent, capabilities printed on counters, mutable game conditions, and transformations. A usable game-state source must preserve these distinctions, plus identity, chronology, visibility, provenance, and version.

The current solution **does have domain models**, but chiefly for terrain/maps and tightly scoped Scenario A1 facts. `LimboDancer.Domains.Asl.Maps` provides board coordinates, terrain and hex facts. `LimboDancer.Domains.Asl.ScenarioA1` has bounded occupancy/reveal snapshots and `ScenarioA1ReturnState`; it does not provide a reusable personnel, vehicle, Gun, or equipment definition catalog, a general unit-instance aggregate, or an authoritative game-wide event source. This analysis designs those missing concepts; it does not silently reinterpret the existing exact-case contracts.

## 1. Rulebook inventory

The table lists principal A–E rule families as a starting inventory. It is not the complete unit ontology; additional chapters, nationality modules, charts and counter sources belong in the same source program.

| Domain concept | Relevant evidence in rulebook | Modeling consequence |
|---|---|---|
| Personnel taxonomy | PDF p. 44, A1.1–1.12: SMC (leader, hero); MMC (squad, HS, crew). Vehicle inherent crew is not an MMC until it leaves the vehicle. | Separate personnel kind from counter form and inherent crew. |
| Personnel definition and printed values | PDF pp. 44–45, A1.121–1.25, A1.4–1.6: squad, HS, crew, FP, range, morale, identity, class, broken side, special status, US#. | Store face-specific printed stats and counter markings with their source and applicable nationality/date. Do not infer capability from a `4-6-7` string alone. |
| Leaders and heroes | PDF pp. 43–44, A.10, A1.11; pp. 83–84, A15.2; pp. 85–86, A17–19. | Model SMC subtype, leadership DRM, heroic/wound state and resulting replacement or loss separately from MMC combat stats. |
| Squad/HS transformations | PDF pp. 45, A1.3–1.32; 85–86, A16 and A19.1–19.13. | Reduction, deployment, recombination, ELR replacement, disruption and battle hardening are events that may change definition and/or counter cardinality. Preserve lineage. |
| Personnel condition and action | PDF pp. 43, A.7 Good Order; 45, A1.4–1.5; 48–52, A4 movement; 65–69, A10 morale/rout; 76–80, A12 concealment; 83–85, A15–17. | Keep condition dimensions explicit: broken, pinned, CX, TI, berserk, fanatic, wounded, concealed/hidden and movement/phase. Good Order is a rule-derived predicate, not a universal single state enum. |
| Position and stacking | PDF pp. 45–47, A1.6, A2.2, A2.8; pp. 52–53, A5. | Unit position can be a hex, level, vehicle, conveyance or other placement; US# and stacking are context dependent. A board hex alone is insufficient. |
| SW and possession | PDF pp. 50–51, A4.4–4.5; pp. 56–57, A7.35; pp. 62–65, A9; pp. 88–91, A21–23. | A support weapon is an equipment instance with its own identity, usage, portage, ammunition and malfunction state. Possession, carriage, firing and captured use are relationships/events; a SW is not automatically a unit. |
| Guns and manning | PDF pp. 167–169, C2.2–2.3; 179–183, C9–12; 183–185, C13. | Gun is an ordnance/equipment entity with caliber/type, ammo, CA, manhandling/towing/emplacement, malfunction and a manning relationship. Do not equate Gun counter, manning crew and vehicle main armament. |
| Vehicles and crews | PDF pp. 192–199, D1–2; pp. 199–204, D3–5. | Vehicle definition includes chassis/armor/armament/mobility/transport features; instance includes location, facing, motion, damage, CE/BU, load and inherent crew. Armed inherent crew and unarmed inherent driver differ (D5.1). |
| PRC, transport and mounting | PDF pp. 58, A7.821; 203–206, D5–6; 80–82, A13 Cavalry. | Personnel may be passengers, riders, crew or cavalry; role and containment vary with time. Personnel and Infantry are not interchangeable predicates. |
| Combat and casualties | PDF pp. 54–62, A7–8; 65–76, A10–11; 176–179, C7–8; 199–204, D3–5. | Attack outcome must be recorded as events and projected to unit, equipment, crew and vehicle effects independently. |
| Nationality, substitution and exceptions | PDF pp. 86, A19; 93–99, A25; Chapter A National Capabilities Chart references. | Definition lookup needs nationality, class/type, time/SSR applicability and substitution mappings. Later module-specific nationality rules remain separate dependencies. |
| Prisoners and capture | PDF pp. 86–88, A20–21. | Capture changes custody, possession and sometimes use restrictions; it is not a simple Boolean on a counter. |

**Full-rulebook coverage required:** The PDF also has rules after p. 253 (for example modules G and W) and back-matter charts. Inventory and incorporate their unit definitions, states and exceptions by module and applicability; register exact sources and review each semantic artifact before publication. Index entries and counter art point to controlling sections; they do not themselves authorize mechanics. The definition catalog requires counter and National Capabilities Chart evidence, not just textual taxonomy.

### Beyond A–E: source expansion map

The rulebook TOC (PDF pp. 7–10) identifies the following unit-relevant families. These are **discovery targets**; chapter presence does not establish that every rule applies to every game. Record module, scenario, date and SSR applicability in the model.

| Source family | Unit-model work to inventory |
|---|---|
| F, North Africa | Desert movement, transport and equipment interactions and local environmental modifiers. |
| G, Pacific Theatre | Japanese and Chinese personnel/capabilities; U.S. Marine Corps and early U.S. Army; caves, landing craft, seaborne assault and animal-pack relationships (TOC p. 7, G1, G10–12, G14, G17–18). |
| H, Design Your Own and vehicle/ordnance notes | National order-of-battle purchase/availability, exact vehicle and Gun variants and national notes (TOC p. 8, H1–2). Distinguish notes/catalog evidence from counter inventory. |
| I, O–R, T, Z, campaign and mini-module rules | Scenario/campaign-specific replacement, substitutions, force availability and specialized unit interactions when those modules are in scope (TOC pp. 8–10; notably R6 and T1). |
| J and S | Deluxe scale and Solitaire ASL roles, control/behavior and generated force state; model as optional ruleset overlays, not universal unit properties (TOC pp. 8–10). |
| W, Korean War | UN and Communist national force distinctions, era-specific equipment, air support and forward air control (TOC p. 10, W2–9). |
| A–W and miscellaneous back-matter charts | Terrain, nationality, ordnance, vehicle and other chart data, including material after the chapter pages (TOC p. 10; B Terrain Chart at PDF p. 698). Register exact chart and page. |

The TOC says the first-edition N Armory pages are not contained as a reissued chapter in this PDF (PDF p. 9). Do not infer a complete physical counter catalog from this PDF alone: identify and register the applicable counter sheets, armory material or equivalent authoritative artifacts. Maintain an explicit `not-yet-inventoried` status for every module and source type until covered.

## 2. Identity and classification

Proposed conceptual types (names are candidates, not code committed here):

| Type | Identity and contents | Changes when |
|---|---|---|
| `UnitDefinition` | Stable catalog key plus source revision, nationality/force, Personnel subtype or Vehicle subtype, class, printed faces and markings, eligible abilities, date/SSR constraints. | Source/counter/catalog revision or a separately reviewed rules package changes. |
| `UnitInstance` | Stable game-scoped identity, definition reference, side/owner and lifecycle/lineage reference. | Created, reduced, deployed, recombined, eliminated, restored, or converted by an explicit event. |
| `EquipmentDefinition` / `EquipmentInstance` | Printed SW or Gun characteristics; game-scoped identity, condition and use state. | Possession, capture, ammo, breakdown/repair, destruction, placement or limbering changes. |
| `VehicleDefinition` / `VehicleInstance` | Vehicle data, crew/armament/transport slots; actual motion, damage, facing, load and crew status. | A vehicle event occurs. Vehicle can be a specialized `UnitInstance` without treating its Gun as a free-standing Gun. |
| `CounterRepresentation` | Physical or virtual counter face and mapping to one or more game entities; source identifier where available. | Counter is flipped, removed, replaced or generated. This is presentation/evidence, not the game entity's identity. |
| `GameState` | Game/scenario ID, sides, board package versions, phase/turn, scenario rule overlays, visibility perspective and monotonic revision. | A committed, sequenced game event occurs. |

Use disjoint *kind* classifications for SMC leader/hero, MMC squad/HS/Infantry crew/dismounted vehicular crew, and vehicle. Model Guns and SW as equipment unless a specific rule calls for a unit relationship. Inherent crew is a component of its vehicle until an abandonment/survival event yields a separately represented crew. Some counter types and roles need additional classifications; avoid a closed exhaustive enum until the relevant chapters and charts have been curated.

Printed FP/range/morale, broken morale, ELR markings, leadership DRM, Smoke/Assault/Spraying Fire markings, US#, portage points, caliber, ROF, ammo, CA, armor, MP, capacity and special armament are **typed, source-backed characteristics** whose presence varies by kind. Distinguish printed values from computed effective values: terrain, leadership, status, date, SSR, captured use and attack mode modify the latter. A catalog key must include more than the printed strength triple.

## 3. Live unit state and relationships

The authoritative instance projection should carry, at minimum:

1. **Scope and consistency:** tenant, game/scenario, unit ID, definition revision, event-stream revision, observed UTC timestamp, source ID, visibility perspective, board/scenario package versions.
2. **Position:** typed `LocationRef` with map/board, hex, level and placement/status where applicable; previous and attempted locations belong to movement history, not an overwritten `LocationId`. Integrate with `Maps.Coordinates` while checking that the actual board/scene version matches.
3. **Conditions:** separate dimensions for Good Order inputs, broken/disrupted/pinned/TI/CX, berserk/fanatic, wounded, concealed/hidden/dummy state and capture. Model unknown, inapplicable and withheld facts distinctly; avoid defaulting unobserved conditions to false.
4. **Operational role:** on foot, cavalry, passenger, rider, inherent or dismounted crew, with vehicle/mount references and eligibility to count as Infantry at the observed instant.
5. **Turn activity:** phase, movement status, remaining and spent MF/MP, MF/MP expenditure location, fire/ROF and use restrictions, attempted entry and result. Costs and entitlements need event provenance.
6. **Relationships:** side/enemy relation, occupant membership, stack membership and ordering where needed, possession and custody of equipment, Gun manning, towing, loading/transport, vehicle crew, leader participation, prisoner guard, and counter-to-entity representation.

These dimensions need constraints. For example, a free standing crew counter cannot simultaneously be the same vehicle's unseparated inherent crew; eliminated entities cannot act; possession must have one authoritative holder; and a passenger's vehicle position must reconcile with the vehicle's position. Concealment or HIP may be inaccessible to one observer: projection must filter by perspective while retaining privileged state for authorized adjudication.

## 4. Events, transitions and provenance

Prefer an authoritative, ordered `GameEvent` stream with a versioned projection rather than independent mutable Boolean fields. A minimal envelope is `(tenantId, gameId, eventId, sequence/revision, occurredAt, actor/source, type, payload, rulePackageRef, causalEventIds, visibilityScope)`. A transition takes an expected version, validates the applicable published rule package and source facts, commits atomically, and emits an auditable effect. The following are **event families to design**, not approved executable actions:

| Event family | Typical state changes | Source anchors |
|---|---|---|
| Deploy/recombine/reduce/replace/eliminate | One-to-many or many-to-one instance lineage, new definitions, possessions and stack positions. | A1.3–1.32 (PDF p. 45); A19 (p. 86). |
| Morale and Heat of Battle | Broken/rally/disrupted/berserk/fanatic/heroic, possibly definition or cardinality changes. | A10 (pp. 65–69); A15–16 (pp. 83–85). |
| Reveal/conceal/hide | Visibility and counter representation, reveal ordering and occupancy evidence. | A12 (pp. 76–80). |
| Move/board/unload/abandon | Position, role, vehicle occupancy, MF/MP and component separation. | A4 (pp. 48–52); A13 (pp. 80–82); D5–6 (pp. 203–206). |
| Acquire/drop/fire/repair/destroy equipment | Equipment condition, holder and usage; Gun manning/towing state. | A4.4 (p. 50); A9 (pp. 62–65); C10–11 (pp. 180–182). |
| Fire/wound/capture/damage | Personnel state, prisoner relation, vehicle damage or crew survival. | A7–11 (pp. 54–76); A17, A20 (pp. 85–88); D3–5 (pp. 199–204). |

Keep event ordering explicit: a concealed occupant can be present before the attacker knows its type; reveal is an event; OVR election, NTC, MF expenditure and second reveal occur in a defined order; any return is a separate authorized transition. Record the observed version(s) used to derive a conclusion and require freshness at execution. A source revision or later event invalidates dependent conclusions. Never turn a synthetic unit-test fixture into an authoritative live-game source.

## 5. Repository gap and integration path

| Existing component | Reuse | Missing or necessary adaptation |
|---|---|---|
| `LimboDancer.Domains.Asl.Maps` (`BoardLocation`, `HexFacts`, terrain catalog) | Typed spatial/reference facts; board versions. | Game-specific placement, occupancy and event stream are outside Maps. |
| `ScenarioA1ConcealedSmcOverrunSnapshot` and other `ScenarioA1*Snapshot` records | Exact reviewed observation projections, tenant/package/version/source checks and ordered reveal facts. | Many scenario-specific nullable flags, no stable cross-scenario unit definition or full game aggregate. Adapt from authoritative events only for reviewed cases. |
| `ScenarioA1ReturnState`, `ScenarioA1JournalReturnStore` | Versioned, atomic return for a narrow clear-return subset. | Explicitly hypothetical aggregate; not the general game-state store or authoritative event source. Its execution behavior remains bounded. |
| ASL authoring/TIR and published Scenario A1 packages | Rule-fragment provenance, review and exact-case semantic package publication. | Curated unit definitions and general transition packages need their own review/evidence; source fragments alone do not create valid live behaviors. |
| Runtime domain/observation/execution abstractions | Package identity, tenant-scoped observation, decision and gate. | Add ASL-owned adapters, registered explicitly; do not put ASL kinds into core runtime contracts. |

Suggested project boundary: `LimboDancer.Domains.Asl.Units` for catalog, identity, typed state, constraints and pure projections; an ASL game-state adapter for an authoritative tenant/game-scoped event and snapshot store; existing `ScenarioA1` as a narrow consumer. Avoid binding the catalog to VASL counter graphics; record VASL or source-counter aliases as provenance and representation mappings.

### Minimum read contract for the next Scenario A1 integration

`ReadCase(tenantId, gameId, packageRef, attackerUnitId, locationRef, expectedRevision, perspective)` should return a consistent snapshot with: attacker definition and state; opposing occupant identities or sealed presence as allowed by perspective; current phase; typed locations, board version and terrain; movement expenditure and remaining MF; ordered reveal/NTC/response events; previous occupied location; version/source/time. Derive adjacency and terrain from the pinned map package, and occupancy/phase/movement from the game source. On unavailable, inconsistent, unauthorized or stale data, return an explicit nondefinitive result. It must never assert a sole defender from incomplete visible information.

The write side must require the reviewed exact-case conclusion, an expected version, gate authorization and atomic compare-and-swap of both unit and game-level occupancy/position. Persist and verify the effect and event ID. The existing return journal could be adapted only after its state ownership, collision rules and event integration are reviewed; it must not become a parallel source of truth. Conditional return attacks and special placement remain blocked pending their own reviews, as does live-use risk authorization.

## 6. Recommended implementation sequence

1. Register and verify the physical rulebook across all relevant chapters, counter/charts and supplement versions. Expand this inventory with chapter-by-chapter unit, equipment, vehicle and nationality coverage. Create a rule-to-field matrix for each printed characteristic and proposed transition; adjudicate ambiguous definitions and exceptions through the existing authoring workflow.
2. Implement **reference definitions first**: Personnel taxonomy and printed front/broken values, then equipment/Guns, vehicles and applicable nationality mappings. Use source-linked examples and round-trip catalog tests.
3. Implement stable IDs, typed position and relationships, game revision and ordered event schema; test invariant and visibility behavior with supplied fixtures. Preserve Scenario A1 exact IDs and observation contracts.
4. Add a read-only authoritative adapter that projects existing game case/events into Scenario A1 snapshots and cross-checks board facts, provenance and version. Exercise unknown, stale, conflicting, hidden and second-reveal cases.
5. Only then integrate authorized write events, one narrowly reviewed transition at a time, with atomic revision checks, effect verification and explicit live-use risk authorization.

**Review gates:** This document is a modeling proposal. No full rules coverage, counter catalog completeness, scenario state authority, host opt-in, or new action authority follows from it. Each later source family, transition and live-use path needs its own testable package contract and explicit review.
