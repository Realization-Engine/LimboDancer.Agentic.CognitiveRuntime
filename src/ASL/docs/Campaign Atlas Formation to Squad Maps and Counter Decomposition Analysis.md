# Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis

Status: proposed design, 10 October 2026. No tactical execution or automatic historical force decomposition is implemented by this document.

## Decision

Continue the HTML/JS prototype with one bounded formation-to-squad vertical slice. A selected engagement footprint produces a finer map and a reserved subset of persistent subordinate units. It must preserve its parent terrain, time, mission and force identities. Zoom alone does not create an engagement or new troops.

The first delivery should be a squad-scale planning and inspection workspace, followed by a validated ASL Scenario Card export. Executable ASL combat remains behind the existing ASL admission and engine boundary. Do not create a second JavaScript ASL rules engine.

Use separate components for coordinates, terrain refinement, organization, asset reservations, engagement state, rendering and ASL adaptation. Keep source data outside those components. The intended Blazor direction remains LimboDancer.Campaign consuming ASL domain and reusable Razor libraries, never the reverse.

## Current implementation and gaps

Reviewed campaign baseline: commit 207c4ae, with this analysis prepared against the active campaign worktree.

| Existing implementation | Reusable behavior | Remaining gap |
|---|---|---|
| campaign-world.js | Campaign identity, seed, mission footprints, forces, terrain events and repository ownership | Authored world positions and imported Situation board positions are not one coordinate frame. The EPSG:3035 label needs a verified transform before claiming real georeferencing. |
| campaign-world-view.js | Campaign, nominal 250 m formation and 40 m tactical views | The tactical view is a preview, not a compiled ASL board with complete rules semantics. |
| panzer-map-art.js and per-Situation geography data | Board rotation, joined layout, roads, streams, terrain and settlements | Artwork coordinates and transcribed terrain require explicit metric calibration and semantic review. Decorative trees and roofs are not automatically authoritative features. |
| panzer-situation-state.js draft | Both-side selection, parent placements, planning holds, blocked Scenario Card | boards and sides are empty; footprint, time and decomposition are unresolved. Free-form intent is not an enforceable objective. |
| tactical-handoff.js | Catalog binding, hashes and explicit admission state | Its three-platoon roster and test opposition are authored fixtures. It must not become the default decomposition of arbitrary imported counters. |
| Situation counter organization fields | Place to record decomposition evidence | St. Lo counter types inspected still declare decompositionStatus unresolved. Artwork factors do not establish troop strength. |
| Split situation-data components | Scenario definitions separate from geography, deterministic source reconstruction | Continue this separation for tactical terrain and organization data. |

Current formation game maps support deployment and inspection, not resolved formation combat. Initially the player explicitly selects a contact to investigate; the application must not claim it detected a battle through a combat simulation that does not exist.

## Map scale and coordinates

Adopt one metric plane per authoritative battlefield and store the grid definition separately from SVG dimensions. Define origin, axes, orientation, distance convention, clipping boundary and transform version. Use meters for world geometry; pixels and browser zoom are presentation only.

The prototype's nominal 250 m formation and 40 m tactical spacing gives a linear ratio of 6.25 and an area ratio of 39.0625 for regular hexes measured with the same convention. These are design calculations, not evidence that every source board has exactly that ground scale. They do not mean that each formation hex contains exactly 39 tactical hexes. Grids cross one another's boundaries; choose cells by geometric overlap and explicit clipping policy.

Prefer adjacent-center distance as the normalized grid spacing. For a pointy regular hex with adjacent-center distance d, circumradius is d/sqrt(3); the axial basis can be (d, 0) and (d/2, sqrt(3)*d/2). Record rotations as separate transforms. Audit the current cells implementation against this convention before reuse. If a source gives a different measure or unit, retain the original measure and its evidence and convert explicitly. Never infer ground scale from a scanned image's pixel dimensions.

The current renderer uses 40 meters per drawing unit in the authored campaign view. That conversion is not itself a definition of tactical hex size.

Two coordinate cases must remain explicit:

1. **Imported Situation battlefield.** Calibrate the joined boards into one local metric frame, preserving each board rotation and seam. A tactical crop is spatially continuous with that battlefield. It remains a reference battlefield within the campaign until an evidenced georeferencing/adaptation transform exists.
2. **Campaign-world battlefield.** Take the crop directly from the campaign metric world, using its feature identities and terrain events. This can supply a genuinely shared multiscale view once the current frame labeling and transforms are corrected.

Do not silently place a Panzer source town onto an unrelated campaign road just because the campaign seed matches. Record frameId on every footprint, placement, feature and export. Reject incompatible frames unless a versioned transform is explicitly supplied.

## Refinement of terrain

Select a bounded contact area spanning enough surrounding terrain for maneuver and reinforcement entry. Use a configurable margin around the selected parent cells, not one tactical board per parent counter. For an illustrative 1.2 km by 0.9 km footprint, a 40 m center-spacing grid has roughly 780 hex areas before boundary clipping. The final count depends on grid origin and clipping.

The immutable parent scene supplies topology and constraints; a versioned generator supplies subordinate detail. Roads remain connected polylines, rivers remain connected features, and bridges remain crossings between those networks. Preserve town extent, named settlements, forest boundaries, important elevation relationships and objective locations. A road crossing a crop boundary must meet the same road in an adjacent crop.

| Parent feature | Tactical treatment |
|---|---|
| Road or track | Preserve route identity and centerline connections; resolve tactical entrances and exits. |
| Stream, river and crossing | Preserve water topology, crossing identity and destruction state; width and bank rules need evidence or an explicit authored profile. |
| Settlement | Generate or curate buildings and streets within an approved footprint; label invented details as authored. |
| Woods | Refine edges and interior clearings consistently without severing preserved routes. |
| Elevation category | Use a reviewed elevation profile; a color category alone cannot establish tactical contours or line of sight. |
| Unknown terrain | Retain an unknown/admission finding rather than silently converting to open ground. |

Derive random detail from campaign seed, frame identity, generator version and stable world-cell or feature identity. Do not seed solely from the selected crop, render order or engagement ID, which would change overlapping terrain. Generate with a margin and then clip. Cache by source hash and refinement inputs; serialized canonical features, not incidental SVG output, define terrain identity.

Apply time-stamped bridge destruction, wrecks, rubble and other approved changes over the immutable scene. Reopening a saved engagement uses its pinned terrain snapshot and approved event revision. A generator update must not move terrain beneath a running game.

## Counter decomposition and conservation

A formation counter becomes a parent organization with persistent children, not a multiplied image. Maintain separate records for authorized organization, scenario strength and current surviving strength. Counter combat factors cannot be divided to obtain ASL firepower, morale, range or movement factors.

Each organization profile needs nationality, service, date range, unit role, parent echelon, source references, confidence, quantities and catalog mappings. A named unit or scenario override can supersede a generic establishment only with explicit provenance. Keep verified, authored and unresolved profiles visibly distinct.

| Parent counter kind | Possible tactical children | Restriction |
|---|---|---|
| Rifle or armored infantry | Squads, half-squads, supported leaders and assigned weapons | Do not assume every platoon has the same squad count or every nationality uses the same organization. |
| Engineers | Engineer infantry and specifically allocated equipment | Do not automatically add demolition charges, flamethrowers or special capabilities from a symbol. |
| Tank or vehicle platoon | Individually identified vehicles with model and current condition | Vehicle count needs strength evidence or an approved authored profile; printed attack factors are not a vehicle count. |
| Gun or mortar unit | Guns/tubes, eligible crews, ammunition and transport relationships | Avoid counting crew both as infantry and as an independent duplicate. |
| Truck or halftrack unit | Individual transport and capacity, with existing passenger/load bindings | A transport counter does not create extra passengers or dismounted squads. |
| Headquarters, off-map support or obstacles | Supported command/observer/support objects or retained parent assets | Not every parent counter becomes a squad counter on the tactical board. |

ASL leader ratings are game abstractions, not a direct conversion from a command appointment or preparation modifier. Preserve leadership effects through an explicit mapping policy. Never add one 8-0 leader per parent counter by default. Validate unit and weapon definitions against the actual ASL catalog version before export.

Materialize child identities once and persist them. Reopening a map must not reroll composition or create new identities. A first illustrative fixture may use two authored infantry platoons, each with three squad assets, but must label that composition as an example rather than a historical assertion. Leaders, weapons and vehicles require their own approved records.

Engagement membership can reserve only some children of a parent counter. Track the remaining children as available, already committed, off-map, delayed or unavailable. Preserve consumed ammunition and losses. A parent is an aggregate view of these assets; do not allow the parent and its engaged children to operate independently as duplicate forces.

Define conservation across engagement boundaries: survivors plus permanent losses, captured assets and approved transfers must account for every admitted identity. Splitting a squad into half-squads requires lineage and personnel conservation; later recombination must not create additional strength. Disabling this detail for simplified play changes the accounting policy explicitly, not the identity rules.

## Parent formation deployment areas

Each parent formation counter assigns its decomposed tactical assets a deployment area defined by a fixed anchor hex and an explicit radius in tactical hex steps. Automatically place those assets on legal hexes inside that area, then allow the player to reposition them within the same limits before confirming setup. The radius may deliberately provide more room than the smallest area needed to accommodate the units.

Deployment areas are placement limits, not exclusive territories. Areas belonging to different parent counters may overlap. Infantry, engineers, weapons and vehicles may intermingle in an overlap, provided every asset remains inside its own parent's area and obeys terrain, stacking and Situation restrictions. An overlap does not change parentage, pool the areas, reserve the territory exclusively, or permit an asset to use another parent's larger radius.

### Definition and ownership

- Derive the initial anchor from the parent counter's mapped position in the tactical grid. Record the grid/frame identity and the coordinate conversion policy. Moving a child must not move the anchor.
- Set the radius before placement through an approved Situation or organization profile. It can differ by formation role and need not be the tightest possible radius. No universal numeric default is established by this requirement.
- Store the anchor, radius, parent identity and rule/profile version in the engagement deployment definition. Persist player placements separately so saves and reloads preserve both the boundary and the arrangement.
- Use axial hex distance: for coordinate differences dq and dr, distance is max(abs(dq), abs(dr), abs(dq + dr)). Include hexes whose distance from the anchor is less than or equal to the radius. Radius zero includes the center; radius one includes the center and six neighbors.
- The anchor and radius are fixed for the current setup revision. An authorized change must explicitly create a revised deployment definition and revalidate all affected placements. Never silently move the anchor or enlarge the radius.

### Legal placement and automatic deployment

The legal destinations for each asset are the intersection of its parent's radius, the engagement footprint, the parent's authorized setup zone and applicable terrain restrictions. Enforce stacking, occupancy and transport/load rules across all parents, including in overlapping areas. A shared area does not imply unlimited stacking.

Generate initial placements deterministically from a stable asset order and legal candidate hexes. Prefer nearby suitable hexes while distributing counters where practical; do not require deployment on the outer ring or pack every formation into the smallest possible cluster. Preserve existing valid placements when deploying remaining assets. The selected placement algorithm and tie-breaking rules belong to a versioned deployment profile.

Plan the complete automatic placement before committing it. If there is insufficient legal capacity, leave existing placements unchanged and identify the affected assets and constraint. Do not widen the radius or place units outside their authorized area. Only offer remedies supported by the Situation, such as an approved anchor/radius revision or retaining eligible assets off-map. Unsupported remedies must not appear as available choices.

A relaxed radius permits intermingling but does not require children of one parent to occupy adjacent hexes. There is no additional child-to-child cohesion requirement in the initial implementation.

### Interaction and enforcement

When a child counter or parent group is selected, show its anchor, boundary and legal destination hexes on the central map. Distinguish the selected parent's boundary from other overlapping boundaries without obscuring terrain. State the parent and radius in the inspector. During dragging, preview destination validity; reject an illegal drop, retain the prior position and explain the violated constraint.

Apply the same validator to automatic placement, click placement, drag placement, imported saves and setup completion. UI highlighting alone is not enforcement. Revalidate after changes to terrain, stacking, the participating roster or deployment rules. A temporarily invalid draft may be inspected and repaired, but cannot be confirmed or admitted for play.

These are setup restrictions. After play begins, tactical movement rules apply unless the Situation explicitly defines a separate ongoing command-distance restriction. Keep deployment radius and in-play command range as distinct rules.

### Acceptance cases

- Units exactly at radius R are legal; units at R + 1 are rejected, including across rotated boards and joined-board seams.
- Two parents with overlapping areas can deploy intermingled units, but a child cannot use the other parent's area beyond its own radius.
- Stacking and terrain constraints are enforced across parent groups within an overlap.
- Automated deployment respects the same boundaries as manual movement, remains deterministic and preserves existing valid placements.
- Insufficient capacity causes no partial deployment, silent radius expansion or anchor movement.
- Saves and reloads preserve anchor, radius, parentage and placements; invalid imported placements block completion.
- Browser zoom and viewport resizing do not affect hex-distance validation.
- Starting tactical play ends the setup-only restriction unless a separate scenario rule explicitly retains it.

## Engagement contract and workflow

Introduce campaign-owned versioned contracts:

- **SpatialFrame:** id, units, grid convention, source calibration, transforms and evidence.
- **RefinementManifest:** parent source hash, frameId, footprint, seed, generator/profile versions, feature identities and terrain-event revision.
- **OrganizationProfile:** dated applicability, evidence, composition entries, catalog references and confidence.
- **ForceAsset:** stable id, parentId, type, strength/state, equipment links, supply and leadership bindings.
- **Engagement:** parent campaign/Situation/mission, revision, start time, interval policy, footprint, selected assets, reservations, parent deployment areas, objective template and lifecycle state.
- **TacticalPackage:** board definition, Scenario Card, asset bindings, catalog hashes, admission report and source provenance.
- **EngagementOutcome:** unique outcome id, input revision, asset transitions, elapsed time, objective result and terrain changes.

Use draft, reserved, prepared, admitted, active, resolved, reconciled and cancelled states with explicit permitted transitions. Terrain preview is permitted with incomplete evidence; playable admission is not. Cancellation releases holds without recreating spent assets. Outcome application must be idempotent and reject stale parent revisions.

Keep the left pane as the decision workflow, the central map as the visual surface, and the right pane as the card/evidence inspector:

1. Select participating formation counters and a contact footprint on the current Situation map. Highlight both and show frame and scale.
2. Choose a definitive engagement purpose from supported mission templates, such as hold a crossing or seize a specified objective. Notes remain optional and have no rules effect.
3. Select an approved organization profile or review missing mappings. Show parent-to-child counts and exclusions before reservation. Unknown historical composition produces a clear block or an explicitly authored alternative, never an invented historical answer.
4. Preview the refined map. Show feature continuity, entry areas, objectives and derived squad/vehicle counters. Preserve wheel zoom and Space-left/middle-button pan. Counters scale with tactical hexes.
5. Confirm deployment and Scenario Card settings using validated options. Explain unavailable choices in the left pane beside the next action.
6. Open the squad map for planning. Enable ASL execution only after board, rules and catalog admission succeeds through the existing ASL system.
7. On completion, apply a validated outcome once, show the changed parent units and advance/reconcile campaign time according to the selected scheduling policy.

For the initial slice, serialize tactical engagements and freeze unrelated campaign decisions during tactical execution. Record the tactical start and duration, then process scheduled campaign events up to the reconciled end in order. Do not equate a formation turn to an ASL turn or silently add wall-clock play time. Simultaneous engagements and multiplayer visibility are later explicit scheduling policies.

Existing supply and command decisions constrain tactical eligibility, reinforcement entry and approved capabilities. Use supported ASL rules/SSR mappings only. If a supply consequence cannot yet be expressed, report it as unsupported rather than modifying counter factors arbitrarily. Never expose hidden enemy placements through a tactical preview; the initial demonstration must be clearly an open-information planning mode.

## JavaScript component boundaries

Proposed paths below are new modules, not implemented files. Each module exposes a small API and has one owner of state. Use explicit dependency injection at the composition root; domain modules must not access DOM, localStorage or mutable browser globals.

| Proposed file under prototypes/europe-hex-map/tactical/ | Responsibility |
|---|---|
| spatial-frame.js | Metric transforms, board assembly calibration, axial conversions and overlap queries |
| terrain-refinement.js | Deterministic subdivision of authoritative parent features |
| terrain-validation.js | Seams, connectivity, unknowns and supported terrain checks |
| organization-profiles.js | Load and select dated composition profiles; no force mutation |
| force-decomposition.js | Materialize child assets and lineage from approved profiles |
| deployment-areas.js | Parent anchors and radii, legal-area intersections, deterministic initial placement and shared placement validation |
| asset-reservations.js | Availability, engagement holds, release and conservation |
| engagement-state.js | Commands, revisions and engagement state transitions |
| engagement-repository.js | Versioned serialization and campaign-owned persistence adapter |
| scenario-adapter.js | Existing ASL Scenario Card and board-package translation |
| outcome-reconciliation.js | Idempotent asset, time and terrain return to campaign |
| squad-map-art.js | SVG terrain/counter layers from prepared view models |
| squad-map-viewport.js | Pan, zoom and drag mechanics, shared with formation UI where compatible |
| squad-workflow-view.js | Left-pane choices and admission feedback |
| squad-card-view.js | Right-pane Scenario Card and evidence |
| squad-workspace.js | Composition root connecting existing Atlas panes and module interfaces |

Keep organization JSON in sources/organizations/, refinement profiles in sources/refinement/, and schemas in schemas/. Generated tactical packages belong in separate versioned outputs or campaign saves, never in source-code literals. Load only needed profiles/packages when a compatible loader is introduced; preserve current offline file access or explicitly migrate that distribution contract. Do not disguise another monolith behind a generated wrapper.

As a review guideline, split a module when it mixes domain and rendering responsibilities or approaches roughly 400 lines of substantive code. This is a maintenance trigger, not a reason to fragment cohesive algorithms arbitrarily. Share the viewport implementation by extracting it from the current view, not by copying the entire panzer-situation-view.js into a new squad view.

## Delivery sequence and acceptance gates

| Phase | Deliverable | Acceptance |
|---|---|---|
| S01 | Spatial and engagement schemas with local-board calibration fixtures | Round-trip coordinates across rotations; declared units; boundary ownership and frame mismatch tests |
| S02 | One reviewed or explicitly authored infantry profile for each participating side | Stable child identities; no duplicated weapons/crews; persisted partial reservations; unresolved profiles block admission |
| S03 | A crop around a road, settlement/woods edge and crossing | Same inputs produce identical canonical features; overlapping crops agree; board seams and roads remain connected |
| S04 | Integrated squad planning workspace | Entire descent and return through the left pane; overlapping parent deployment areas enforced for automatic and manual setup; parent state preserved; usable counter scale and keyboard controls |
| S05 | ASL adapter and validated exported package | Actual catalog bindings, enforceable objective, deployment and rules; existing ASL validation rejects unsupported inputs |
| S06 | Outcome reconciliation | Duplicate/stale outcome rejection; asset conservation; supply and campaign clock reconcile correctly |

Use a bounded St. Lo local-board crop for the initial continuity test because its joined-board implementation is established. Choose the exact contact after auditing its terrain and composition evidence. Add the Ardennes command-derived variant as the next fixture so withheld guns and the two reserved M10s retain identity through descent. Do not substitute one scenario's profile for another simply to make the demonstration run.

Test portrait and landscape boards, reversed/rotated assemblies, non-integer grid overlap, edge crops, unresolved terrain, cancelled drafts, saves/reloads, catalog changes and browser scaling. Test a supply-constrained roster and a counter partly committed to another engagement. Compare DOM workflow checks with actual browser inspection at formation and squad scales.

## Migration alignment and unresolved decisions

This analysis updates the delivery sequence: use the HTML/JS prototype to validate map refinement and force decomposition before porting their contracts and accepted behavior. The earlier instruction to stop prototype refinement until a Blazor host exists is superseded. Executable tactical rules still belong to ASL.

Port pure modules into campaign domain/application services. Implement the Scenario Card adapter in campaign-owned code that references ASL Play and Maps. Extract reusable Razor rendering, roster and inspector components only where both applications benefit. Browser gesture code can remain JavaScript behind narrow interop contracts.

Before playable admission, resolve source-board scale calibration, at least one dated or explicitly authored organization profile per side, catalog mapping coverage, terrain semantics and supported objective templates. These are evidence and implementation gates; they do not prevent building a clearly labeled planning preview now.

## Repository references

- [Blazor migration and terrain refinement analysis](<Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis.md>)
- [LimboDancer.Campaign implementation plan](<LimboDancer.Campaign Implementation Plan.md>)
- [Command supply leadership and optional complexity analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>)
- Prototype source: prototypes/europe-hex-map/campaign-world.js, campaign-world-view.js, panzer-situation-state.js, panzer-map-art.js, tactical-handoff.js, schemas/situation.schema.json and sources/situations/.
- Existing ASL contracts: src/ASL/LimboDancer.Domains.Asl.Play/ScenarioCards.cs and src/ASL/LimboDancer.Domains.Asl.Maps/Features/FeatureModel.cs and BoardPackage.cs. Re-audit their current versions at integration time.

## First squad setup implementation, 10 October 2026

Implemented a bounded St. Lo formation-to-squad setup in the HTML/JS prototype. From the completed St. Lo game map, select a supported parent and choose Open squad map. The workspace uses the existing rotated and joined source artwork with a stable 40 m center-spacing grid. Roads, streams, buildings and source feature identities retain their coordinates. This is a fine-grid setup view, not a completed terrain refinement engine or geographically located ASL battle.

Seven small JavaScript modules under prototypes/europe-hex-map/tactical separate organization profiles, spatial transforms, terrain sampling, deployment state, repository, viewport interaction and workspace UI. The largest module is the workspace, under 100 lines. Existing panzer-situation-view.js only supplies the entry control.

Provisional defaults: US rifle 3 squads; German rifle 4 squads; German engineers 4 squads; US M4/75 5 vehicles. No separate leaders, crews or support weapons are generated in this slice. These are playable defaults, not claims of an exact historical establishment. Historical research is not a release gate.

Each parent has a stable center and a four-hex radius (61 possible hexes before terrain exclusions). A radius-14 local footprint supports adding nearby parents whose whole deployment area fits. Areas may overlap. At most two friendly subordinate counters share a hex; opposing sides may not share one. Water blocks deployment; woods and marsh block vehicles as provisional setup rules. Initial placement is deterministic and adding a parent is atomic. Drag and click placement use the same validator.

State preserves parent and child identities and positions in a separate versioned local save. One squad setup per Situation is supported. Reopening resumes it; changing the parent plan invalidates it visibly instead of silently regenerating troops. Parent counters already held by legacy engagement drafts cannot be added. Multiple simultaneous engagements, campaign casualty reconciliation and formal ASL Scenario Card admission remain future work.

Validation: test_squad_deployment.cjs covers deterministic rosters, rotation round trips, axial grid, radius boundary and rejection beyond it, overlapping groups, stacking, atomic capacity failure, duplicate IDs, stale parent plans and save/reload. Existing test_panzer_situation.cjs passes. Live localhost UI testing traversed both St. Lo setup sides, launched the formation game map, opened the squad map, zoomed, dragged legally, rejected an illegal drag, added a second parent, and verified retained positions and eight counters after return and reopen. This testing is in the in-app browser, not standalone Chrome.

Port these contracts to LimboDancer.Campaign without making ASL reference Campaign. Keep gameplay validation separate from Razor rendering and use JS interop for pointer/viewport handling where necessary.


### Full St. Lo squad deployment and ASL counter values, 10 October 2026

The first local implementation is now extended to the entire joined St. Lo battlefield. `Deploy full Situation` expands all 76 deployed formation counters into 335 subordinate counters, covering all 21 source counter types. Existing squad placements are preserved. Expansion is atomic and repeatable without duplicates. Both boards retain their original rotations and shared artwork coordinates beneath the 40 m grid.

Each parent retains a fixed four-hex deployment radius. Regions may overlap. Two friendly counters can share a hex; opposing counters cannot. Open water blocks deployment; vehicles cannot deploy in marsh. Woods permit initial placement, including armored groups already deployed there at formation scale. These are setup constraints, not a claim that tactical movement rules have been implemented. Gun and mortar counters include their crews; separate crew deployment and leaders remain future work.

`asl-counter-definitions.js` snapshots values from the ASL codebase with source paths, catalog version, source hash, definition IDs and explicit provenance. Direct matches include US 6-6-6 rifle squads, German 4-6-7 rifle squads and German 8-3-8 engineer squads. Where exact definitions are absent, comparable ASL catalog or synthetic example definitions supply clearly labeled provisional estimates. No Panzer strength, movement, attack, defense or range factors are transferred. ASL morale, firepower, range, armor, caliber and movement remain distinct concepts.

`squad-counter-art.js` crops the central Panzer illustration and renders new ASL values around it. The selected-unit inspector exposes the complete value set and its provenance. Full deployment, artwork reuse, selection, zoom, legal repositioning and persistence are supported; combat execution and adjudication are not implemented.

Terrain geometry is shared with the source boards. Fine terrain classification is still inherited from source hexes, with recorded water/bridge polygons overlaid. This is not yet a full ASL terrain rules model or geographically continuous campaign refinement. The implementation is split into spatial, terrain, organization, ASL definitions, state, persistence, counter rendering, viewport and workspace components. Port these responsibilities to LimboDancer.Campaign while preserving the dependency direction toward consumable ASL libraries.

Validation: model regression covers all 76 parents and 335 children, fixed deployment radii, stacking, persistence, atomic failure, duplicate prevention and catalog-value fidelity. The browser displays the full joined board with both forces and supports switching between the full-map and selected-unit views.


## HTML/JS squad integration update, 10 October 2026

Squad deployment is now available from every imported Situation game map, including campaign-linked Ardennes variants. It occupies the central map pane and preserves the surrounding command and Situation Card panes. Returning to formation scale retains deployment. Campaign-linked squad saves belong to the campaign and mission; standalone Situations retain their own saves.

The playable library now covers all imported ground counter types. Aircraft remain off-map, equipment retains its original anchor, and loaded weapons remain with their assigned transport. ASL catalog matches and provisional estimates are identified explicitly. This remains deployment and inspection, not combat execution or admission to the ASL engine. Source boards retain their local coordinates rather than becoming georeferenced campaign terrain.

Deployment radius is a saved setting, not a fixed four-hex rule. New setups use 6 tactical hexes, offering 127 candidate hexes compared with 61 at radius 4, before terrain and authored setup exclusions. The UI accepts whole-number radii from 0 to 50 as a prototype input guard. Equipment retains radius zero. Existing saves retain their prior profile radius until the player explicitly applies a change. Applying a radius revises the setup definition and validates existing positions atomically. An invalid reduction is rejected without moving counters. If the chosen area exceeds the local preview, the view expands to the full board footprint. No automatic placement silently increases the radius. Source setup zones, terrain, stacking and transport constraints still take precedence. Crowded parent placement can therefore still require adjustment even with a larger radius.

Validation: the full 17-Situation library deployed 6,909 subordinate counters from legal, evenly distributed formation fixtures at radius 6. All six Ardennes baseline/supplied/reinforced variants passed, including reserve decomposition and campaign-scoped save round trips. Boundary, invalid reduction, radius increase, revision, legacy-save compatibility and atomic failure tests passed. Existing Situation, command/card transition, command mechanics and campaign world tests passed. Live browser verification reopened the saved reinforced Crossing Defense map with 324 subordinate counters and applied radius 6 without relocating them.

For Blazor migration, preserve separate profile/catalog, spatial refinement, deployment validation, persistence and workspace components. Port deployment radius, setup revision, transport relationships and mission-scoped storage as domain data. Campaign continues to reference consumable ASL libraries, with no reverse dependency required.


## Normandy St. Lo command integration, 10 October 2026

The existing Normandy inland campaign now owns a distinct 29 June St. Lo operation. Its operation identity, seed, command log, supplies, mission binding, formation deployment and squad setup persist together. A separate Normandy active-operation key preserves Ardennes saves and avoids treating the 8 June command snapshot as the 29 June force state. Existing standalone St. Lo saves are retained, not silently imported into a newly committed command variant.

Player route: Campaign > Open / resume Normandy: St. Lo > Start Normandy operation > Commit operation allocation > Continue to St. Lo Regiment HQ > advance scheduled events > Commit St. Lo mission > Continue to Situation setup > complete both setup sides > Launch situation game map > Open squad map. Committing the mission opens its card on the right; the left command pane retains the next action. The central map shows headquarters, supply route, transport and reserve before displaying the joined source boards and then squad deployment.

Finite choices govern supply shipment and reserve assignment. Essentials assigns support automatically; Operational exposes both choices. Staff preparation is 10 or 20 simulated minutes. Baseline withholds one gun, supplied retains all 76 source formation counters, and reinforced adds two Sherman formation counters. These yield 331, 335 and 345 subordinate counters respectively. Source identities, board rotations and original objectives are preserved. The command organization, support timings and extra reserve are explicitly authored game assumptions. Historical timeline inspection is read-only. Commitment consumes the chosen allowances once.

Implementation is split across normandy/command-state.js, situation.js, repository.js, map-view.js and workspace.js. Shared CampaignWorld validation/terrain, the Situation screen, squad modules and viewport interactions are reused. During Blazor migration, extract operation configuration and reusable command services before adding more theaters; do not extend one monolithic workspace. Dependency direction remains Campaign consuming ASL libraries.

Normal St. Lo reference entries now lead to its command operation rather than independent setup. The raw source workflow remains available only with the explicit developmentReferences query parameter for regression and artwork reference work. Existing tests opt into that reference path and also check the player redirect.

Validation: model tests cover all three variants at both staff timings, source immutability, full formation/squad deployment, replay, retained-supply gating, campaign storage isolation, stale writes and retained previous saves. Live browser testing started a new operation, verified preparation and ammunition gates, inspected history, committed the supplied plan, completed both setup sides, launched the map, expanded all 335 subordinate counters and reloaded/resumed them successfully. A duplicate card heading found during this run was corrected.

Limitations: deployment and inspection only. Combat resolution, outcome reconciliation and a surveyed connection between the campaign diagram and printed boards remain outside this implementation. Source boards retain local battlefield coordinates; the command route is illustrative.


## 11 October 2026: source composition audit and counter information cards

The HTML/JS deployment now loads `tactical/source-composition.js` after the original profiles. It records nominal principal equipment counts and personnel/support inventories from the original Panzer Leader Unit Function Table, printed pages 19-22 (PDF pages 20-23 of the archived original manual). All imported ground types have an evidence record; infantry squad grouping remains a deliberate playable abstraction. Canadian/Belgian equivalents use Commonwealth or Allied rows; German MG teams lack a direct row and remain an explicit assumption. Printed discrepancies (25-pounder and Daimler caliber) are retained rather than silently erased.

Corrected counts apply throughout all 17 Situations and campaign variants. Mixed members are supported: British Sherman formations produce three Sherman 75 vehicles plus one Firefly; the M3 scout formation produces four M3 cars and one Daimler scout car. Each child has a validated definition ID. W Panzer IV/Panther formations contain four vehicles; SS versions contain five, with black backgrounds and white artwork/text. Source-tagged SS colors apply to formation artwork too; no blanket assumption is made that all German forces are SS.

`tactical/tactical-estimates.js` supplies explicit role-based values instead of copying all fields from a tank or mortar template. Exact catalog infantry/halftrack definitions remain usable. Unmatched combat values are provisional, not official ASL transcriptions: there is no new combat engine. Source crew and supporting weapon totals remain information, not independently spawned combatants. Transport capacity and support-weapon allocation still require gameplay design beyond setup.

`tactical/counter-info-card.js` replaces squad counter native tooltips with a keyboard-accessible, hoverable card that can be pinned or closed. It separates tactical values, composition/counts, original printed factors, page evidence, setup constraints, artwork IDs and original notes. Export includes the source counters, rules and composition profiles.

Profile version `source-composition-2` rejects earlier saved decompositions. The UI provides an export of the old setup and an explicit rebuild that retains a local backup before replacing the active squad save. Formation deployment is retained. Rebuild may reposition tactical counters within the existing setup constraints; failure leaves the previous save active. New radius remains six and is adjustable.

Blazor migration contract: port source composition evidence, mixed member identities, tactical estimates, affiliation palette, counter-card presentation and versioned save handling as separate components. Keep the archived source inventory independent of engine-admitted tactical rules.

Validation: all 17 full Situation deployments passed, totaling 7,158 tactical objects; six Ardennes and six Normandy command variants passed. Independent regressions verify the source counts, mixed Firefly membership, invalid member rejection, SS affiliation and role corrections. Browser checks verified Goodwood backup/rebuild, reload persistence, mixed Sherman rendering, four-versus-five W/SS counts, black/white SS artwork, and card pin/close/Escape/I-key behavior. Original table-page images are included as evidence links.


### Live UI sweep of the other 16 Situations, 11 October 2026

Opened each Situation through its atlas marker, resumed the existing committed scenario, opened squad deployment, rebuilt outdated saves with backup, and inspected the complete rendered roster. Every parent group matched the audited child-count rule. Cards retained printed formation factors and composition evidence, and only explicitly SS-designated tank types received black backgrounds. Mixed Sherman groups contained three Sherman 75s and one Firefly. Existing campaign decisions were retained.

| Source Situation | Scenario / committed variant | Formation counters | Tactical objects | Result |
|---|---|---:|---:|---|
| 4 | St. Lo | 76 | 335 | Passed |
| 6 | The Reichswald | 63 | 282 | Passed |
| 7 | Encirclement of Nancy | 64 | 279 | Passed |
| 8 | Marieulles | 45 | 194 | Passed |
| 9 | Operation Market: Nijmegen | 73 | 324 | Passed |
| 10 | Operation Market: Arnhem | 47 | 198 | Passed |
| 11 | Operation Garden: Anticlimax | 118 | 535 | Passed |
| 12 | Prelude: The Saar | 92 | 450 | Passed |
| 13 | The 'Fortified Goose Egg' | 178 | 797 | Passed |
| 14 | Bulge: Thrust (reinforced) | 72 | 327 | Passed |
| 15 | Elsenborn Ridge (supplied) | 116 | 513 | Passed |
| 16 | Bastogne: Prelude | 51 | 219 | Passed |
| 17 | Turning Point: Celles | 152 | 710 | Passed |
| 18 | Bastogne: Siege | 108 | 496 | Passed |
| 19 | Patton's Counter Offensive | 186 | 871 | Passed |
| 20 | Remagen Bridge | 25 | 106 | Passed |

These are deployment, presentation and conversion-data checks, not verification of official ASL combat statistics. Vehicle/weapon quantities follow the source table; infantry subdivision remains a declared playable assumption and unmatched tactical values remain provisional. Bulge uses the reinforced campaign roster, so its 327 tactical objects differ from the unchanged source-card roster of 319.

SS affiliation limitation: the original infantry table on printed page 21 lists Engineer, Security, Rifle and Submachinegun, with no separate SS infantry row. The explicit W/SS tank distinctions appear on page 22. Generic infantry may belong to an SS formation named by a Situation Card; that requires per-formation affiliation metadata, not a global change to the German rifle profile. Such scenario-derived infantry affiliation is not implemented by this audit. Affiliation must remain separate from tactical strength.


## Implemented scenario affiliation, 11 October 2026

`tactical/scenario-affiliation.js` now resolves identity per Situation and counter instance; squad children inherit their parent identity at display/export time. This updates existing deployed saves without rebuilding or moving units. The source table SS designation is retained separately from affiliation. Formation map artwork, roster labels, squad colors, information cards and squad exports use the scenario resolver.

Implemented: Elsenborn Group A SS / Group B Heer; Peiper and source-card Nijmegen forces SS; Arnhem core Rifle counters SS with unresolved supporting attachments neutral; Goodwood Rifle counters 01-08 SS and 09-15 Heer as an explicitly authored split; Marieulles infantry uses probable 37th SS attribution with the conflicting card description displayed. Known Army scenarios, including Panzer Lehr, no longer inherit SS color from the printed tank type. Unresolved mixed formations remain neutral German. Squad exports include resolver version and per-parent/per-child identity evidence.

Affiliation is not troop quality. No automatic SS firepower, morale, count or special-rule bonus has been introduced. Future quality profiles should distinguish experience, equipment and current readiness, applicable to both Army and SS. The official Lorraine history reports the replacement-heavy 17th SS rated fit only for defense in November 1944: https://www.ibiblio.org/hyperwar/USA/USA-E-Lorraine/USA-E-Lorraine-9.html . Thus SS identity alone is insufficient evidence for an elite rating.

Validation: affiliation tests cover all 17 rosters, exact Elsenborn split, Goodwood allocation, inherited identity, source immutability and removal of false SS labels from Army forces. Full squad-library tests still produce 7,158 objects. Live UI verified Bastogne Siege resumes its 496 existing counters with no black SS counters, and Elsenborn resumes 513 counters with Group A infantry black, Group B infantry blue, and formation evidence visible in the counter card.


### Scenario-specific SS infantry quality, 11 October 2026

Implemented `tactical/scenario-troop-quality.js`, separate from affiliation, source composition and deployment geometry. A historical unit identity does not automatically grant elite values.

| Printed Situation | Rating decision | Affected formation parents |
|---|---|---|
| 09 Nijmegen | Rifle 4-6-7 to catalog 4-6-8 | 9 SS rifle parents |
| 10 Arnhem | Rifle 4-6-7 to catalog 4-6-8 | 12 SS rifle parents |
| 14 Bulge: Thrust | Rifle 4-6-7 to catalog 4-6-8 | 9 SS rifle parents |
| 05 Goodwood | Retain current ratings | The mixed SS/Army split is authored, insufficient evidence for a specific quality allocation |
| 08 Marieulles | Retain current ratings | Probable SS attribution conflicts with printed trainees; determined defense alone does not prove elite status |
| 15 Elsenborn | Retain current ratings | Training and combined-arms problems argue against a blanket upgrade |
| Other imported Situations | Retain current ratings | No additional confirmed SS infantry quality assignment |

The complete existing ASL `attacker-elite-squad` profile is used: firepower 4, range 6, morale 8, elite class, smoke exponent 2 (previously 1). These are authored scenario assignments using catalog values, not historical numerical measurements or official ASL scenario conversions. No 6-5-8 firepower increase is inferred. Engineer and SMG specialist profiles, vehicles, guns, printed Panzer factors, unit counts, positions and radius settings remain unchanged.

Evidence: [Cole, Ardennes chapter V](https://www.ibiblio.org/hyperwar/USA/USA-E-Ardennes/USA-E-Ardennes-5.html), especially comparative training and replacements, supports Peiper's rifle rating. [MacDonald, Siegfried Line chapter VII](https://www.ibiblio.org/hyperwar/USA/USA-E-Siegfried/USA-E-Siegfried-7.html) describes SS reinforcement at Nijmegen and successful blocking actions at Arnhem. [RAF Arnhem study](https://www.raf.mod.uk/what-we-do/our-history/air-historical-branch/second-world-war-thematic-studies/arnhem-the-air-reconnaissance-story/) identifies an experienced 9th SS nucleus. Nijmegen is a lower-confidence gameplay inference: the source card names 9th SS, while the historical narrative includes 10th SS. Applying the profile to every represented rifle parent is explicit game abstraction.

[Cole, Ardennes chapter VI](https://www.ibiblio.org/hyperwar/USA/USA-E-Ardennes/USA-E-Ardennes-6.html) records training problems around Elsenborn. [Cole, Lorraine chapter III](https://www.ibiblio.org/hyperwar/USA/USA-E-Lorraine/USA-E-Lorraine-3.html) qualifies the Marieulles unit identification. Neither supports automatically upgrading these entire rosters.

The resolver applies current ratings when existing deployments are opened, without rebuilding saved units. Map artwork, information cards and selected-unit details resolve the same values. Export includes the quality policy version and each unit's effective definition, base values and rationale. Cards link evidence and preserve printed source information. Combat remains unimplemented; these are deployment/display/export ratings.

Migration contract: retain separate affiliation, source profile and dated scenario-quality policy in C#. Persist effective profile provenance in exported engagement snapshots. Never derive troop quality solely from SS palette or formation tank-table designation.
