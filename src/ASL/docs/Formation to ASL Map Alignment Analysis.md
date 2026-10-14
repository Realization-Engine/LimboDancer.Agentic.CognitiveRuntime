# Formation to ASL Map Alignment Analysis

Status: analysis and proposed design, not an implementation specification.
Date: 13 October 2026.
Code baseline: c1b7c4b7.

Implementation follow-up: [Formation to ASL Map Alignment Design and Implementation Plan](Formation%20to%20ASL%20Map%20Alignment%20Design%20and%20Implementation%20Plan.md).

## Purpose and recommendation

Align Campaign Atlas squad maps with the existing ASL map and terrain contracts so a formation engagement can produce a coherent, executable tactical map. The ASL libraries should own tactical geometry, terrain compilation and rules semantics. Campaign code should own source interpretation, refinement, geographic placement and engagement selection.

Keep the current nominal 250 m formation spacing provisionally. Evaluate 240 m as an alternative after defining the spatial contract. Changing the grid size alone will not turn the current fine-grid illustration into an ASL map.

Use one continuous tactical grid over a joined Situation, with formation hexes treated as operational regions over that grid. Generate continuous terrain features before compiling tactical hex facts. Do not generate a separately centered tactical grid inside every formation hex.

This analysis develops the terrain-refinement proposal in [Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis](Campaign%20Atlas%20Blazor%20Migration%20and%20ASL%20Terrain%20Refinement%20Analysis.md). It does not change that document's dependency direction or authorize a migration of the application host.

## Replayability as a core game feature

Seeded tactical terrain refinement is a major gameplay feature. A new campaign playthrough should offer a meaningfully different tactical landscape within the same Situation's operational constraints. Variation should change decisions about covered approaches, firing positions, building arrangements, woodland routes and local maneuver, rather than only change decorative appearance.

Preserve the Situation's identity: required roads, crossings, objectives, broad terrain relationships and setup restrictions remain constraints. Generated detail changes how players approach those constraints. The goal is substantial replayability, not a guarantee that every seed produces a unique map or that every playthrough unfolds differently.

| Player action | Terrain behavior |
|---|---|
| Start a new campaign playthrough | Generate and persist a new root terrain seed before refinement |
| Resume or load a campaign | Retain its seed, pinned generation inputs, accepted terrain and subsequent changes |
| Revisit a Situation or open another engagement in the same campaign | Reuse the established landscape and its persistent changes |
| Replay a specified seed | Reproduce the initial landscape when the source, profile, spatial and generator versions also match |
| Share a terrain challenge | Transfer the seed and pinned refinement manifest; also specify scenario and starting state for a comparable game |

Do not reroll terrain merely because a player opens a Situation, changes map level, loses an engagement or reloads a save. A new playthrough must be an explicit lifecycle action. Reproducing a map does not by itself reproduce combat outcomes or player decisions.

### Meaningful variation and playability

Profiles should permit changes that affect supported movement and LOS while preserving locked features. Candidate variation includes the arrangement of buildings within a settlement, paths through woodland, connected clearings and permitted field boundaries. The available variation depends on the adopted evidence and terrain profile; a heavily documented, locked area may allow less variation.

Evaluate generated maps across a representative seed set. Check required access, legal setup capacity, objective reachability, supply connectivity and supported terrain combinations. Compare tactical properties such as cover distribution, route alternatives and sight lines using the admitted ASL capabilities. Record the measures and acceptable bounds during profile calibration rather than inventing universal balance thresholds.

Playability does not require symmetric terrain or equal chances for both sides. Preserve the intended Situation asymmetry. Do not adapt generation to the player's current force placement or desired outcome.

A rejected candidate must not trigger an unrecorded random reroll. The first version reports generation failure without rerolling. A later version may use a specified deterministic repair or bounded candidate-selection procedure whose inputs and selected result are recorded in the refinement manifest. If no valid result is available, report the generation failure before admitting the engagement.

### Replayability acceptance checks

- Different seeds produce meaningful differences in permitted tactical features, beyond cosmetic variation.
- Each tested seed preserves the Situation's locked constraints and passes the defined playability checks.
- Resuming a campaign preserves its landscape and accumulated terrain changes.
- Replaying the same seed and pinned inputs reproduces the initial map exactly.
- Shared challenges identify the required content versions and starting state as well as the seed.
- Candidate rejection and repair remain reproducible and do not silently change terrain after play begins.

## Current implementation

| Concern | Campaign prototype | Existing ASL code |
|---|---|---|
| Grid | Custom axial q,r coordinates and regular hex polygons | BoardGeometry, HexIndex, hex naming and hexside contracts |
| Physical scale | Nominal 250 m formation spacing and 40 m squad spacing | Board-space pixel geometry; a metric transform must be supplied explicitly |
| Terrain | Fine-cell center assigned to a containing formation hex; largely inherits its base terrain | Feature model with areas, linear features, bridges, buildings, elevations and hexside annotations |
| Drawing | Original formation artwork with a finer grid | Rendering backed by compiled terrain and derived facts |
| Persistence | Prototype source-grid and deployment records | Versioned board packages and game map references |

The prototype spatial frame computes `step = 113 * 40 / 250`, or 18.08 source-image units between tactical neighbors. It rotates and joins the original board images, then samples a continuous fine grid in that joined coordinate frame.

Terrain refinement identifies the formation polygon containing each fine-cell center. Water and bridge polygons receive limited additional treatment. Most tactical terrain remains inherited from the parent hex. Decorative buildings, roads and vegetation visible in the artwork are not thereby complete tactical locations or movement and LOS facts.

The ASL map library provides a deterministic feature compiler. It is not, by itself, a formation-terrain refinement generator. Campaign refinement must produce appropriate feature inputs rather than duplicate this compiler.

### Source locations

Paths below are relative to the repository root.

- `prototypes/europe-hex-map/tactical/spatial-frame.js`: axial grid, scale, joins and coordinate conversion.
- `prototypes/europe-hex-map/tactical/terrain-refinement.js`: fine-cell sampling and source artwork reuse.
- `prototypes/europe-hex-map/campaign/supply-spatial.js`: separate `250/113` metres-per-unit setting, route distances and travel times.
- `src/ASL/LimboDancer.Domains.Asl.Maps/Geometry/BoardGeometry.cs`: VASL-compatible geometry and sampling conventions.
- `src/ASL/LimboDancer.Domains.Asl.Maps/Features/FeatureModel.cs`: fixed-point feature geometry and terrain representation.
- `src/ASL/LimboDancer.Domains.Asl.Maps/Features/FeatureCompiler.cs`: deterministic compilation into terrain grids.
- `src/ASL/LimboDancer.Domains.Asl.Maps/Features/BoardPackage.cs`: board package identity and persistence.
- `src/ASL/LimboDancer.Domains.Asl.MapStudio/Services/GameMaps.cs`: loading board versions and composed game maps.

## Scale and decomposition

The following calculations use the prototype's nominal 40 m tactical spacing. They assume both levels describe comparable center spacing in regular hex grids. They are design comparisons, not a verification of historical source-map scale.

| Formation spacing | Tactical spacing | Linear ratio | Area ratio | Change from current formation scale |
|---|---|---|---|---|
| 250 m | 40 m | 6.25 | 39.0625 | Baseline |
| 240 m | 40 m | 6 | 36 | 4 percent smaller linear extent |
| 280 m | 40 m | 7 | 49 | 12 percent larger linear extent |

Area ratios do not specify a guaranteed number of complete tactical hexes inside each formation hex. A hex near a formation boundary can overlap two or more formation regions.

An integer spacing ratio can align selected grid centers when orientation and origin also align. It does not make every large-hex boundary coincide with complete small-hex boundaries. A claim that one 240 m formation hex decomposes exactly into 36 whole ASL hexes would therefore be misleading.

A 240 m option is worth evaluating because it stays close to the current scale and permits simpler center alignment in a compatible orientation. It is not necessary for a coherent tactical map. A continuous grid with explicit parent-region mapping also supports 250 m.

Changing display zoom or SVG pixel size does not change physical scale. Changing metres per source unit does. Keep these choices separate.

## Proposed spatial contract

### Continuous metric frame

Represent source features in a stable local metric scene. Define the transform from normalized board coordinates through board rotation and joining into that scene. Record scale, origin, orientation and source revision explicitly.

Treat source boards as authored terrain, not surveyed geography. Placement on the continental atlas must not imply that individual source buildings have verified geographic coordinates.

### ASL geometry adapter

Map the metric scene into ASL board geometry using an explicit, reversible transform. The ASL standard geometry reproduces VASL's pixel layout, arithmetic and sampling. Feature coordinates use 1/64 pixel units. Neither those units nor the geometry property names can be read directly as metres.

Preserve ASL's established indexing, neighbors, hexside ownership and sampling behavior. Resolve grid orientation and stagger conventions in the adapter rather than changing the core ASL geometry to match prototype q,r coordinates.

Specify quantization and boundary tie-breaking. Test round trips within a stated tolerance. Keep visual counter-rotation of labels separate from the underlying map transform.

### Formation ownership

Assign each tactical hex a primary formation parent based on its center, with deterministic tie-breaking on a boundary. Retain overlap information where deployment, control or aggregation needs it.

A primary parent is an indexing convenience, not proof that every part of the tactical hex shares one terrain type or one side's control. A formation boundary must not split a playable tactical hex into independently playable fragments.

Keep source formation boundaries visible as an optional overlay. Decide explicitly how setup restrictions and the supply receiving-area exception translate across boundary hexes.

### Joined boards and engagement crops

Use one grid origin and orientation for the connected Situation. Do not restart the tactical grid at source-board joins or formation boundaries.

Generate stable features before selecting engagement crops. Neighboring and overlapping engagements must reproduce identical shared terrain and feature identities, regardless of generation order.

Local tactical board names can differ from campaign coordinates, but the mapping must persist. The existing ASL composition contract must be checked before assuming that arbitrary crops or rotations can be represented directly.

## Terrain refinement contract

| Source feature | Tactical refinement requirement |
|---|---|
| Village or town | Streets, building footprints, open ground and supported building locations |
| Woods | Explicit boundaries, clearings and connections to paths or roads |
| Road | Continuous centerline, width, junctions and supported terrain classification |
| Stream or bridge | Channel geometry and a crossing connected to its approaches |
| Elevation | Consistent elevation regions and legal transitions |
| Wall, hedge or bocage | Explicit geometry and canonical hexside ownership where applicable |
| Unknown terrain | An admission finding or recorded authored assumption |

Preserve source topology first: road destinations, crossing locations, barriers and settlement relationships. Refine tactical detail through a versioned generation profile. Do not make random decorative roofs or trees authoritative simply because they appear in the SVG.

Compile the resulting ASL feature model through the existing terrain pipeline, then derive the facts required by movement, LOS and other admitted rules. A visually plausible map is insufficient if the engine interprets its crossings, buildings or elevations differently.

Unsupported terrain or locations must be reported at tactical admission. Do not silently replace them with open ground.

## Adding tactical terrain variety

Formation terrain is a coarse constraint, not a complete inventory of everything inside a formation hex. Refinement should create a coherent local landscape with ASL features and locations, rather than assign a random terrain type independently to every squad hex.

A tactical hex can contain several relevant features: a road, a building, elevation and a boundary wall, for example. The generator must therefore produce layered geometry and supported location semantics, not merely replace one formation terrain code with one ASL terrain code.

### Terrain refinement profiles

Introduce versioned profiles selected by Situation and local landscape. Candidate profiles include rural farmland, enclosed farmland, woodland, village, urban district and river corridor. These names describe generation styles, not verified historical assignments to particular Situations.

A profile specifies:

- Allowed ASL terrain families and the required engine capabilities.
- Patch sizes, densities and spatial relationships, with configurable bounds.
- Building size, spacing, material and supported height/location options.
- Field and enclosure structure, road connections and crossing rules.
- Where secondary features are permitted or prohibited.
- The permitted departure from coarse source classifications.
- Date, season and environmental modifiers when supported by Situation evidence or explicit authoring.

Profile values need calibration through representative maps. Do not adopt arbitrary percentages as established historical facts.

| Formation context | Candidate tactical detail | Constraint |
|---|---|---|
| Open ground | Fields, tracks, scattered vegetation or isolated structures | Remains predominantly open; no automatic settlement or major barrier |
| Woods | Contiguous woodland, clearings, paths and varied edges | Preserve the main wooded area and its blocking relationships |
| Village or town | Buildings, streets, yards, gardens, walls and open spaces | Preserve settlement extent and access; avoid filling every hex with buildings |
| Road | Surface classification, junctions, lanes and roadside features | Preserve connected destinations and crossing approaches |
| Stream or river | Channel, banks, bridge geometry and supported local terrain | Preserve drainage and crossings; never invent a convenient ford by default |
| Hill | Elevation regions, contours and supported slopes or depressions | Preserve relative heights; cliffs require explicit permission |

These are candidate refinements. Each specific terrain code and combination must pass the ASL capability check before use.

### Evidence and authoring precedence

Use this precedence when constraints disagree:

1. Explicit Situation restrictions, objectives and locked source features.
2. Verified supplemental geographic evidence adopted for this map revision.
3. Approved designer overrides.
4. Profile-generated detail.
5. Decorative rendering.

Conflicts among the first three require an explicit authoring decision. A generator must not quietly resolve them by removing a road, moving an objective or changing a mandatory crossing.

Store feature provenance as source-preserved, evidence-derived or generated, with the profile/version and assumption where relevant. Generated buildings and field boundaries must not be presented as historically located features. Decorative objects do not affect movement or LOS unless promoted into the authoritative feature model.

### Generation sequence

1. Normalize coarse terrain, source routes, crossings, elevations and objective anchors into the metric scene.
2. Lock required topology and establish shared boundaries with neighboring regions.
3. Partition the landscape into coherent parcels, settlement blocks and woodland patches using the selected profile.
4. Add connected local streets, paths, clearings and building clusters inside those regions.
5. Add permitted secondary terrain and explicit hexside features.
6. Compile through the ASL feature pipeline and derive tactical facts and supported locations.
7. Validate constraints and capability coverage, then publish a versioned board package for the engagement.

Use stable spatial seeds and feature identities. Generating a neighboring formation region must not change an existing road or settlement. Changing the viewport, requesting a different crop or reopening a save must not reroll terrain.

### Deterministic seed contract

A persistent seed is required for tactical terrain refinement. Generate one 128-bit root terrain seed when a campaign's terrain world is created, or accept an explicitly supplied seed when reproducing a world. Save it before generating terrain. A new engagement, crop, browser session or save load must reuse it.

The root seed alone is insufficient to reproduce a map. Pin the source content revision, spatial frame and scale, profile version and parameters, generator version, and adopted evidence and overrides. These inputs, together with the root seed, define the world refinement identity. Reproducing compiled terrain also requires pinned catalog content, compiler, fact-derivation and serialization versions. Crop/package identity is separate from world feature identity.

Derive independent local seeds through a specified cryptographic hash, such as SHA-256, over canonically serialized inputs:

`root seed + stable map-space identity + source revision + spatial revision + profile digest + generator version + feature domain + stable spatial key`

The implementation specification must fix field encoding, order, digest byte order and the conversion of the digest into PRNG state. It must also select and version an explicit random-stream algorithm with portable test vectors. The implementation plan proposes sha256-counter-v1 with C# as the sole first-version generation authority. Do not use JavaScript Math.random, an unspecified System.Random implementation, runtime string hashes, current time or browser state inside refinement.

Use separate feature domains for parcels, buildings, woodland detail and other independent generation stages. This avoids one added random draw for a building unexpectedly changing all later woodland. Within each domain, derive randomness from stable feature or region identities instead of consuming one global sequence in traversal order.

Stable spatial keys must refer to the persistent metric world, not the requested engagement rectangle, viewport, temporary board number or array position. Adjacent regions must share deterministic ownership and identities for crossing features. Seed equality does not by itself guarantee continuity: the algorithm must generate roads, waterways and settlements with sufficient shared context, then crop the result.

Profiles, scale and generation versions remain pinned for active worlds. A deliberate regeneration creates a new terrain revision and requires reconciliation of deployments, supply points and engagements. Do not silently reroll accepted terrain or overwrite designer changes. Persist accepted features and authored overrides alongside their provenance; retain the seed and manifest for reproduction and audit.

Include the root seed and refinement manifest in campaign file export and import. Loading a save into another browser or the C# host must reproduce the same feature identities, geometry and compiled terrain under the pinned versions.

Determinism acceptance checks:

- Identical seed and manifest yield identical canonical feature output and compiled terrain hashes.
- Region generation order and parallel execution do not alter output.
- Overlapping crops share identical features, including border-crossing geometry.
- Save/load and supported browser-to-C# transfer preserve the result.
- Golden fixtures verify PRNG, seed derivation, coordinate quantization and feature ordering.
- Different seeds can vary permitted detail while every seed preserves locked source constraints.
- An unavailable generator or profile version produces an explicit compatibility finding, never a fallback reroll.

### Preserve operational meaning

Validate the refined landscape against its formation parent and neighbors. Compare dominant cover, settlement footprint, elevation relationships, connected roads, crossings and major barriers. Exact area equality is unnecessary, but deviations beyond profile bounds must be reported.

Do not independently randomize each formation hex. Share road entry points, waterways, field boundaries and elevation transitions across formation borders. Generated terrain should not reveal the coarse hex outline as an artificial landscape boundary.

Check that required objectives and supply receiving points remain accessible according to the intended setup, including the approved supply deployment margin. Accessibility checks are not a license to create a new crossing or remove difficult terrain simply to make a scenario easier.

### Tactical complexity and engine support

Keep terrain richness separate from executable support. Catalog presence, feature compilation, rendering, movement, LOS and location handling are different capabilities. Maintain an explicit matrix for the terrain types and combinations admitted by a profile.

Begin with a small supported set. Add new profiles and combinations after their gameplay behavior is covered. If a required feature cannot be executed, reject tactical admission or require an explicit approved substitution recorded with the board. Never silently simplify it.

Track tactical complexity through building/location counts, hexside density, elevation transitions and connectivity, not only terrain-type counts. Complexity limits should prevent accidental overload while preserving required source features. They should not dynamically alter terrain according to the deployed forces or desired outcome.

### Author review and persistent changes

Provide a refinement preview showing the coarse formation overlay, generated tactical features and provenance. Allow the designer to inspect findings, lock features and make explicit overrides before accepting a map revision.

An accepted refinement becomes persistent campaign terrain. Subsequent damage, rubble or fortifications belong to campaign/game state or a derived terrain revision. They must not be regenerated from the original seed on each visit.

### Additional acceptance checks

- Coherent terrain patches replace independent per-hex random choices.
- Generated detail remains inside the selected profile's constraints.
- Source objectives, mandatory crossings and road connections remain intact.
- Shared edges and overlapping crops reproduce identical feature geometry and identities.
- Mixed terrain, buildings, elevations and hexside features agree between drawing and supported rules.
- Provenance is inspectable, and generated detail is distinguishable from evidence-derived terrain.
- Unsupported combinations block admission rather than silently becoming open ground.
- Accepted terrain and subsequent changes survive save, load and repeated engagement generation.

## Consequences of a scale change

Centralize physical scale before experimenting. The prototype currently expresses it independently in the squad frame and supply spatial code.

A change affects:

- Physical board extent and its registration on the continental map.
- Tactical hex density and parent assignments.
- Supply-route distances and derived travel times.
- Deployment radius, receiving-area interpretation and spatial queries.
- Saved unit locations, supply adjustments and engagement footprints.

Keep operational radii and tactical radii explicitly typed by level or unit. Two formation hexes and two tactical hexes must never be interchangeable parameters.

Existing saves must retain their original scale and map revision. A migration should transform positions through the recorded spatial frame and report conflicts. It must not silently reinterpret saved q,r coordinates under a different grid.

## Ownership and persistence

Campaign owns source interpretation, generation profiles, stable spatial feature identities, parent relationships and engagement selection. ASL owns its tactical map contracts, terrain compilation and executable rules.

Use a campaign-owned adapter that consumes ASL libraries. Do not introduce a dependency from ASL domain libraries back to Campaign, or a second copy of ASL terrain semantics in JavaScript.

A refinement manifest should identify the source version, scale profile, grid origin and orientation, transform, generator version, deterministic seed inputs, feature identities and resulting ASL board-package versions. These are proposed fields, not existing implemented contracts.

## Player experience and follow-up register

The tactical map should feel like a closer view of the same battlefield. Additional detail must not make the player wonder whether they have moved to a different location or whether the map changed after deployment.

The following register preserves the UX work beyond the initial release. Proposed defaults remain design recommendations until validated with the first usable version.

| ID | Concern | Intended behavior | Delivery focus |
|---|---|---|---|
| UX01 | Continuity between map levels | Preserve geographic focus, orientation and selected unit or its parent relationship. Return restores the previous formation viewport. | First usable version; animated transitions later |
| UX02 | Formation ownership | Optional subtle formation boundaries; selecting a parent highlights its tactical region without implying exact nesting. | First usable version |
| UX03 | Source versus generated detail | Explain the distinction once. Show provenance and assumptions in inspection cards, not permanent map labels. | First usable version |
| UX04 | New campaign and resume | New playthrough establishes a new seed; continue, reload and revisit preserve terrain. Advanced controls expose seed copying and explicit replay. | Core persistence first; polished replay UI later |
| UX05 | Generation feedback | Keep the formation map visible, show honest generation stages, and enable deployment only after validation. Preserve selection on failure. | First usable version |
| UX06 | Zoom-dependent detail | Show broad terrain and routes at overview scale, then buildings and counters, then optional hex details. Keep labels upright and markers proportional. | First usable version |
| UX07 | Terrain meaning | Inspection explains the hex's actual features, supported locations and relevant engine-backed gameplay effects. | First usable version |
| UX08 | Mixed terrain and levels | Distinguish roads through woods, buildings beside roads and selectable building locations. Never imply that a painted feature is a playable location. | Basic supported combinations first; richer levels later |
| UX09 | Setup restrictions | Highlight legal deployment areas and explain precise rejection reasons, including formation-boundary and supply-margin cases. | First usable version |
| UX10 | Battlefield persistence | Retain accepted terrain and subsequent changes across visits. Distinguish damage and fortifications from the original landscape. | Preserve revision identity first; full change presentation later |
| UX11 | Accessibility | Supplement color with patterns, symbols and labels. Provide keyboard inspection, visible focus and readable contrast. | First usable version baseline |
| UX12 | Preview and reroll policy | Establish terrain at campaign creation. Repeated opening or failed play never rerolls it. Explicit new-playthrough and designer-regeneration flows remain separate. | First usable version contract |
| UX13 | Reconnaissance and visibility | Separate existing terrain from what a viewer is entitled to know. Decide terrain disclosure without weakening existing unit concealment rules. | Explicit first-version policy; reconnaissance gameplay later |
| UX14 | Consistency when zooming out | Summarize accepted tactical features at formation scale where useful, so major generated features retain context. Do not overwrite source evidence or change operational rules implicitly. | Parent overlay first; richer summary later |
| UX15 | Player trust | Terrain never adapts to current force placement or desired outcomes. Transferable saves reproduce the same accepted map and report incompatible versions clearly. | First usable version |

Terrain disclosure is distinct from unit visibility. The proposed first-version policy is fully inspectable generated terrain during setup, subject to the selected Scenario's requirements, while preserving the ASL engine's viewer-specific unit visibility. This is not an implementation of reconnaissance or fog of war and must be recorded in the pilot specification.

## First usable version

### Player outcome

A player selects a bounded area of a formation Situation, opens its generated tactical map, understands its terrain and legal setup, places supported units, and returns to the same formation context. Saving and loading preserves the map. Starting a separate playthrough can produce a different valid landscape.

Deliver one complete supported pilot before expanding to every Situation. Prefer a bounded Goodwood sample containing settlement, road and crossing features, subject to the source and ASL capability audit. Add a separate join fixture if the pilot crop does not cross a board join. The pilot choice and crop bounds are implementation decisions to verify, not assumed support for every feature in Goodwood.

### Scope and defaults

- Retain nominal 250 m formation and 40 m tactical spacing for the baseline adapter. Centralize scale configuration. Evaluate 240 m offline; do not rescale active campaigns in this version.
- Use the existing ASL geometry, feature compilation and board-package contracts through a campaign-owned adapter.
- Implement one constrained settlement and surrounding rural refinement profile, limited to verified terrain and location capabilities. Preserve source routes, crossing and objective anchors.
- Persist a root seed, pinned refinement manifest, accepted feature output and resulting board identity before deployment.
- Provide map-level navigation, formation-boundary overlay, terrain inspection, readable zoom behavior and legal setup feedback.
- Use existing ASL map inspection and scenario admission paths. The release is a usable terrain-and-setup integration, not a claim of complete campaign combat and outcome reconciliation.
- Keep campaign orchestration outside ASL domain libraries. Do not make a broad UI-host rewrite a prerequisite for validating the pilot.

### Initial player flow

1. Start or resume a campaign. Establish a seed only for a new terrain world; retain existing accepted terrain on resume.
2. Select the supported engagement area on the formation map. Show its footprint and retain the current viewport and selection.
3. Open the tactical map. During preparation, keep the formation view visible and report stages such as preparing terrain, compiling map and checking setup. Do not show invented progress percentages.
4. On successful validation, display the tactical map at the same orientation and geographic focus, constrained to the available crop. Provide a fit-area action if the initial view is unsuitable.
5. Inspect terrain through a card with features, supported locations, applicable engine-backed effects and provenance. Keep this information out of permanent full-width panels.
6. Deploy using visible legal-area feedback. Rejections identify the actual reason and do not leave a permanent warning banner. Unsupported generated maps remain unavailable for deployment.
7. Return to the formation map at the previous viewport. Reopening reuses the accepted tactical package.
8. Save and load the campaign, including terrain identity and setup. A missing pinned version must produce an actionable compatibility finding, not regeneration with a different version.

Avoid a player-facing generator configuration form in the normal flow. Terrain profile parameters belong to authoring; optional seed details belong in an advanced card. An explicit retry after a transient failure must reuse the same inputs.

### Delivery sequence

| Step | Deliverable | Completion evidence |
|---|---|---|
| 1 | Spatial and capability contract | Fixed coordinate conventions, crop bounds, parent ownership, terrain whitelist and visibility policy; no unresolved assumption hidden in rendering |
| 2 | Deterministic refinement and compilation | One accepted ASL board package; reproducible feature and terrain hashes; valid road, crossing and join fixtures |
| 3 | Persistent terrain lifecycle | Resume, revisit and export/import retain accepted terrain, seed, versions and overrides |
| 4 | Map-first inspection and setup | Player completes the initial flow with upright labels, proportional markers, optional parent boundaries and precise setup feedback |
| 5 | Pilot acceptance | Representative seed tests plus UI checks pass; measured generation, loading and rendering costs recorded |

### First-version release gate

- The pilot loads through the ASL board and scenario contracts and uses verified terrain semantics.
- A new playthrough varies permitted tactical choices across tested seeds; every accepted candidate retains locked source constraints.
- Same-seed reproduction, region-order independence and overlapping-crop fixtures pass.
- Round-trip navigation preserves the formation viewport and meaningful selection context.
- Terrain inspection and legal setup can be used with pointer and keyboard; color is not the sole indicator.
- Generation failure, invalid setup and incompatible saves produce clear, recoverable messages without discarding accepted state.
- The player can save, reload and revisit without terrain changes or duplicated deployment.
- Generation latency, memory use and interaction responsiveness are measured on the target environment. Set concrete budgets during the pilot specification before calling performance acceptable.

### Deferred from the first version

Broader terrain profiles, elaborate building-level selection, player reroll tools, reconnaissance simulation, animated map transitions, automatic formation summaries of generated detail, full damage visualization, continent-wide tactical compilation and campaign combat reconciliation remain follow-up work. The first version must preserve the identities and contracts needed for these additions without implementing them prematurely.

## Recommended validation slice

1. Define the spatial contract and central scale profile, retaining 250 m as the baseline.
2. Produce one bounded refinement containing a town, road and stream crossing, plus a sample crossing a source-board join.
3. Compile it into the existing ASL board format and inspect it through the existing ASL map tooling.
4. Compare 250 m and 240 m using the same source features and refinement rules. Exercise a representative seed set to assess meaningful variation and playability under each scale.
5. Select scale based on terrain fidelity, usable engagement size, continuity and save compatibility.
6. Expand to other Situations after this slice passes.

Use bounded engagements initially. A whole formation board contains many more tactical cells, and raster compilation cost also depends on pixel dimensions. Measure generation time, memory, package size and rendering behavior before making whole-Situation tactical compilation the default.

## Acceptance criteria

- Adjacent tactical hexes have the expected neighbors and consistent shared sides.
- Map-to-metric and metric-to-map conversions satisfy a documented tolerance at all supported rotations.
- Source-board joins do not introduce grid gaps, duplicate cells or broken road and stream connections.
- Parent assignments are deterministic at formation boundaries.
- Repeated and overlapping refinement produces the same shared world features and admitted interior terrain facts. The implementation plan requires raster-alignment and boundary verification before crop-edge locations become playable.
- Buildings, crossings and elevations agree between rendering and admitted ASL rules.
- Supply points remain tied to physical positions through refinement and supported save migration.
- Existing prototype saves are preserved or explicitly migrated, with no silent scale change.
- The admitted engagement can load through the ASL map and scenario contracts without substituting a parallel rules model.

## Decisions still required

- Retain 250 m or adopt 240 m after comparison.
- Define the precise physical spacing convention and its mapping to VASL-compatible geometry.
- Fix the shared tactical grid origin and orientation for each connected map space.
- Choose boundary ownership rules for setup, control and supply areas.
- Select and calibrate the first terrain-refinement profiles, including allowed combinations, complexity bounds and ASL capability coverage.
- Choose tactical crop/package dimensions and the save migration policy.

No formation rescaling or ASL integration is implemented by this analysis. The next step is the first usable version's spatial and capability contract, followed by one compiled pilot and its complete inspection, setup and persistence flow.
