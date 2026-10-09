# Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis

Status: proposed architecture and implementation sequence. 8 October 2026.

## Recommendation

Move the Campaign Atlas into a separate Blazor application, LimboDancer.Campaign, before extending it into executable ASL engagements. This separate-host decision supersedes the initial suggestion to host campaign pages in MapStudio. Put campaign identities, mission state, deployment validation, deterministic terrain refinement and tactical handoff in C#. Retain SVG as the map representation and use a small JavaScript layer for browser gestures and rendering operations.

The HTML/JavaScript prototype has reached a useful architectural handoff point, not a technical limit of JavaScript. It has established navigation, campaign/situation relationships, setup and game-map workflows, board composition, palette utilities and interaction conventions. Continuing to build an independent tactical integration in it would duplicate contracts and validation already owned by the ASL system.

Migrate incrementally. Preserve the prototype as a comparison fixture until the corresponding Blazor workflow passes acceptance checks. Do not rewrite the ASL combat engine or port the entire geographic research pipeline before delivering a useful integrated slice.

## Evidence and current implementation

The campaign browser is HTML/CSS/JavaScript, with Python preparation/build tooling, JSON source packages and generated offline script wrappers. ASL is a C# domain stack hosted by a Blazor Interactive Server application. It already uses JavaScript interop for SVG viewport interactions.

This analysis inspects the campaign worktree at c437b69 plus its current uncommitted appearance utilities, and the newer primary checkout at 4e39504. The latter is on feature/asl-backlog-pass-32g, not assumed to be current main. Implementation must select an integration baseline containing the necessary ASL changes without resetting or switching another active checkout.

| Existing code | Architectural consequence |
|---|---|
| Campaign README, viewer.html, app.template.js and build.py | Preserve source/build distinctions. Generated index.html and map-data.js are delivery outputs. |
| schemas/situation.schema.json and sources/situations/panzer-leader-04.json | The Situation package is authoritative source data. Do not reconstruct it from SVG or UI fields. |
| regional-state.js, situation-state.js, formation-state.js and panzer-situation-state.js | Candidate behavioral specifications for C# models, not a single mature campaign engine. |
| panzer-situation-view.js and panzer-map-art.js | Useful workflow and artwork references. Source counter art and geometry remain reusable assets. |
| MapStudio/Program.cs | Registers Interactive Server components and existing board, rendering and play services. |
| MapStudio/wwwroot/js/boardViewport.js | Already keeps pan/zoom in the browser, imports SVG layers and sends completed editing gestures to .NET. Reuse this boundary. |
| Maps/Features/FeatureModel.cs and FeatureCompiler.cs | Existing geometric feature representation and deterministic compilation into TerrainGrid. |
| Maps/Features/BoardPackage.cs | Existing package manifest, features.json and SHA-256 identity, with caches excluded from identity. |
| Maps/Features/BoardValidator.cs | Validation exists but explicitly defers several checks. A successful report is not proof of every generated-map invariant. |
| Play/ScenarioCards.cs and ScenarioCardLibrary.cs | Existing Scenario Card, setup, roster, turn, area, SSR and victory structures must remain the tactical entry contract. |
| MapStudio/Services/BoardProvider.cs and GameMaps.cs | Existing board-provider and viewer-specific map-layer integration points. Preserve information visibility. |

Paths beginning Maps, Play or MapStudio above refer respectively to LimboDancer.Domains.Asl.Maps, LimboDancer.Domains.Asl.Play and LimboDancer.Domains.Asl.MapStudio under src/ASL.

The St. Lo game screen currently displays a completed deployment and supports inspection. It does not implement formation combat or turn execution. Separate prototype formation movement and command exercises must not be presented as a completed operational simulation.

## What should migrate and what should remain

| Responsibility | Destination |
|---|---|
| Campaign registry, dated definitions, Situation numbering, source evidence | Versioned JSON loaded into validated C# records |
| Mission lifecycle, command hierarchy, communication delivery, deployment and asset reservation | C# application/domain services |
| Scenario generation, admission and outcome reconciliation | C# orchestration around the existing ASL contracts |
| Terrain refinement and compilation | C# generator plus adapter to the existing map feature/compiler pipeline |
| Navigation, workspaces, roster, inspector and utility panels | Razor components with explicit view models |
| SVG terrain, counters and overlays | Existing C# rendering where suitable; migrate prototype illustration features with visual parity |
| Pointer capture, wheel anchoring, live dragging, splitters and browser downloads | Small JavaScript modules behind Blazor interop |
| Research ingestion, historical map preprocessing and source manifests | Retain Python/offline builds initially |
| Saved campaign state | Repository abstraction and durable versioned storage |
| Personal viewport and draft palette preferences | Browser settings may remain local |

Interactive Server must not require a server round trip for every pointer movement. Preview a drag locally, then submit its intended destination once. C# validates the action and returns accepted state or a rejection. A resized pane or disconnected circuit must not turn a visual gesture into an unvalidated domain change.

Program.cs currently registers several application services as singletons. Do not infer that mutable campaign sessions should therefore be global singletons. Define campaign ownership, game identity, revision checks and viewer context explicitly; confirm appropriate service lifetimes before adding live campaign state.

## Proposed boundaries

Use the independent LimboDancer.Campaign host at src/LimboDancer.Campaign/LimboDancer.Campaign.csproj. Build campaign-owned capabilities without ASL project references first, then introduce an explicit tactical integration adapter. Keep campaign modules behind application interfaces rather than putting campaign state into Razor code-behind.

Suggested conceptual contracts, with names provisional:

- CampaignDefinition and SituationDefinition: immutable, versioned source content and historical windows.
- CampaignSession: seed, selected source versions, campaign clock and persistent force state.
- Mission and Communication: assignment, receipt, planning, execution, reporting and assessment, including delivery state and audience.
- SituationDeployment: persistent unit identities, placements and setup-completion state.
- EngagementRequest: parent Situation, geographic footprint, time interval, objectives and reserved force assets.
- RefinementManifest: coordinate frame, source hashes, generator/profile versions, seed inputs and generated feature identities.
- TacticalHandoff: existing ASL Scenario Card plus campaign linkage, board versions, asset mapping and admission findings.
- EngagementOutcome: idempotent return of losses, status, elapsed time and relevant control changes.

Keep campaign orchestration outside the ASL rules package. Put ASL-specific translation in a campaign-owned adapter that consumes Play and Maps. Campaign references ASL projects; ASL projects must not reference Campaign or its adapter. Reusable UI belongs in ASL Razor libraries consumed by both hosts, not in a dependency on the MapStudio executable.

Initially store campaign linkage alongside the Scenario Card. Add optional fields to the card only where a demonstrated use case requires them, with backward-compatible serialization. The current card board arrangement includes column, row and reversal fields; arbitrary campaign-space crops and orientations require an explicit mapping, not an assumption that those fields already express every transform.

A division order produces subordinate missions. A Campaign Situation may produce several ASL Scenario Cards. A single ASL card is neither the entire campaign nor an automatic replacement for one formation counter.

## Dependency direction and reusable Razor libraries

LimboDancer.Campaign consumes ASL capabilities. ASL libraries and the standalone MapStudio application must not reference LimboDancer.Campaign, campaign-owned contracts or the campaign integration adapter.

Keep the existing Rules, Maps, Play, Units and rendering projects as ordinary C# class libraries. Extract reusable UI from MapStudio into Razor Class Libraries (RCLs), rather than moving domain logic into Razor libraries.

The intended dependency direction is:

- Campaign host -> reusable ASL Razor libraries and ASL application/domain APIs.
- MapStudio host -> the same Razor libraries and ASL APIs.
- Razor libraries -> the ASL contracts and rendering services they actually require.
- Campaign-owned tactical adapter -> campaign contracts and ASL APIs.
- ASL domain libraries -> no application host or Razor library.

Campaign must not reference the MapStudio executable project. MapStudio remains independently runnable and becomes the first regression consumer of extracted components. No separate copy of tactical rules, Scenario Cards or map semantics is created for Campaign.

| Candidate library | Initial extraction scope |
|---|---|
| LimboDancer.Domains.Asl.Maps.Components | SVG viewport, map inspection and layer controls, with their JavaScript modules and scoped styles |
| LimboDancer.Domains.Asl.Play.Components | Scenario Card presentation, deployment components and tactical play panels, extracted incrementally |
| Existing unit rendering library | Retain counter rendering here; introduce a separate unit UI library only when a concrete reusable interactive component warrants it |

Proposed RCL locations are src/ASL/LimboDancer.Domains.Asl.Maps.Components/ and src/ASL/LimboDancer.Domains.Asl.Play.Components/. Names and exact boundaries remain subject to a dependency audit; these projects are not yet created.

Extract a component when Campaign has a concrete need, not as a wholesale prerequisite to the independent campaign work. Audit its parameters, callbacks, injected services, navigation assumptions, static assets, render-mode assumptions and mutable state first.

Reusable components accept explicit data and commands. Hosts own routes, configuration, session selection, lifetime and persistence policy. Components must not assume MapStudio URLs or resolve an implicit global game. Move genuinely reusable service contracts into appropriate ASL libraries; implement host-specific behavior at the host boundary. Keep viewer identity and visibility policy explicit.

Package browser modules and styles with the RCL through static web assets. Use isolated styles or scoped selectors, avoid duplicate DOM IDs across instances, and dispose listeners and JS references. Both hosts must register the required services explicitly. Interactive Server is the initial supported render mode; broader hosting compatibility is not assumed.

For each extraction: preserve behavior in MapStudio first, then exercise it in Campaign. Verify standalone startup, asset loading, multiple component instances, viewer isolation, navigation, keyboard and pointer behavior, reconnects and disposal. Existing tactical tests must continue passing.

## State and migration safeguards

Keep definitions, running state, generated terrain and presentation themes separate. Existing browser saves are not a durable cross-device campaign store.

Import supported prototype saves through explicit version adapters. Validate references, campaign/Situation numbering, side, board, placement and asset allocation. Retain an original backup and report unsupported fields. Do not silently drop a draft or translate an illustrative strength value into a historical roster.

Use stable IDs independently of display numbers. Situation 01 is numbered within its parent campaign; Panzer Leader Situation 4 remains a source citation. Capture source and engine versions when starting a session so an updated data package cannot quietly rewrite a running game.

Reserve assets for the whole engagement interval. Overlapping engagements must not deploy the same platoon, tank, crew or support weapon twice. Apply results once using an outcome identity and expected parent-state revision. Define how operational time advances before enabling concurrent tactical engagements.

Persistence must survive process restart, not merely a Razor component refresh. Start with the host's existing storage conventions where appropriate; choose a database only after inspecting those conventions and session requirements.

## Refining a formation map into ASL terrain

Adopt a continuous metric scene between the two hex grids. Using the design's nominal 250 m formation spacing and 40 m ASL spacing gives 6.25 hex widths and 39.0625 hex areas for similarly defined regular hexes. This is an approximate scale relationship, not an integer nesting rule. Confirm both measurements use the same across-flats or center-spacing convention before implementing coordinate conversion.

The source boards use normalized image coordinates. Introduce explicit transforms from board coordinates through rotation/join placement into a persistent local metric frame. The geomorphic St. Lo boards are not surveyed Normandy geography. A campaign marker near St. Lo does not georeference each building.

Generate continuous features first, then derive ASL hex and location semantics. Preserve road destinations, stream crossings, settlement extents, terrain barriers and elevation relationships. Define shared geometry at the A/C join. Decorative seam repairs, tree dots and building illustrations are not authoritative tactical features.

| Parent input | Required refinement |
|---|---|
| Town/village area | Streets, building footprints, open spaces and explicitly supported building locations |
| Woodland area | Woods boundaries, clearings and path connections |
| Road centerline | Width, junctions, crossing continuity and terrain-catalog mapping |
| Stream/bridge | Channel and crossing geometry, with supported depth/width/bridge semantics |
| Elevation category | Nested elevation regions, contours and legal transitions |
| Open terrain | Open ground by default; additional features only through the selected generation profile |
| Bocage/hedge/wall | Explicit feature or hexside spans with consistent edge ownership |
| Unknown terrain | Admission finding or explicit authored assumption; never silent open-ground conversion |

The complete palette demo is an appearance exercise. Its desert, wadi, fortification and other symbols do not prove equivalent ASL engine coverage. The current rule-coverage documentation defers Chapters F, G and H from its active scope. Review the current inventory and actual engine support for each proposed engagement; this migration does not implicitly reopen those scopes.

## Determinism and identity

Use a campaign seed generated once, combined with stable spatial feature or tile coordinates, parent source revision, generation profile and generator version. Do not use viewport bounds, scenario number, rendering order or request order as random inputs.

Generate stable spatial content, then crop it for an engagement. Overlapping crops must share identical roads, buildings and terrain, including when requested in reverse order. Use deterministic feature ownership and sufficient surrounding context to prevent tile-boundary seams.

Specify the PRNG, seed derivation, canonical serialization, rounding and ordering. Do not rely on System.Random implementation behavior or string.GetHashCode for a persisted world. Quantize into the ASL feature model's fixed-point board coordinates through a documented metric transform. Its coordinates use 1/64 pixel units, not metres.

Pin generator versions for active campaigns. A new generator can produce a new refinement revision, but must not silently regenerate terrain under a running tactical engagement.

Theme identity is separate from terrain identity. Changing open ground to #d4eebe or town ground to #8b95a2 must not change movement, LOS, seeds or board hashes. Include theme version in visual cache keys. Prototype SVG color filters are a transitional counter presentation technique, not unit data or combat factors.

## Existing ASL integration path

The intended pipeline is:

Campaign Situation -> bounded EngagementRequest -> deterministic metric scene -> ASL FeatureModel -> FeatureCompiler -> derived facts and BoardValidator -> BoardPackage -> Scenario Card admission -> existing ASL play.

FeatureModel already supports area geometry, centerlines, elevations and structured feature kinds. Determine the exact catalog codes and supported location semantics before selecting the generation profile. Prefer these existing types over a parallel tactical terrain schema.

BoardValidator currently lists implemented and deferred checks in its source. Add generator-specific checks for route continuity, crop overlap, board seams, crossing placement, location reachability and unsupported semantic combinations. F1/F2 source-fidelity checks and authored-board validity are different evidence; a generated board should not claim VASL source fidelity.

Compile success alone cannot admit an ASL mission. Also validate the card's force definitions, setup and entry areas, supported SSRs/victory conditions, playable boundary, board versions and asset reservations. Unknown organization remains blocked; a Panzer infantry counter cannot be expanded into a fixed number of squads solely from its combat factors.

## Incremental implementation sequence

| Stage | Deliverable | Acceptance gate |
|---|---|---|
| 1. Baseline and contracts | Import St. Lo definitions and prototype-save fixtures into validated C# models | Preserve IDs, dates, quantities, numbering and diagnostic behavior |
| 2. Blazor campaign slice | Campaign -> Situation Card -> setup -> game inspection, plus Map Utilities | Deployment survives restart; source art, palettes, selection, zoom, pan and counter placement match agreed behavior |
| UI reuse gate | Extract the required map and Scenario Card components into ASL Razor libraries; retain MapStudio as a consumer | Both hosts pass component/asset/interaction checks; no reverse dependency |
| 3. Engagement boundary | Select one bounded area and reserve a small force detachment | Mission, time, parent footprint and assets remain traceable; duplicates rejected |
| 4. Terrain refinement | A settlement edge, road, crossing and woods compiled into one authored ASL package | Repeated and overlapping generation agrees; supported semantics and validation pass |
| 5. Scenario Card admission | Convert verified forces and objectives into an existing ASL card | Existing startup accepts the package; missing data produces actionable findings |
| 6. Outcome return | Complete an admitted engagement and reconcile the parent once | Assets, losses, elapsed time and control changes remain consistent and replay-safe |
| 7. Broader migration | Additional campaign regions, Situations and terrain profiles | No invented historical readiness or silently unsupported rules |

Stage 2 is an integration and workflow milestone, not a promise of newly implemented formation combat. Keep the rest of the atlas available as reference browsing while this slice matures. An iframe may temporarily expose the prototype, but it does not count as migration if JavaScript remains the authoritative campaign engine.

## Verification and unresolved decisions

Use existing Node/Python checks as behavioral evidence while moving domain assertions into C# tests. Keep visual comparison fixtures for A/C orientation, joined terrain, counter readability and both approved palette exports. Add browser interaction checks for drag versus selection, zoom anchoring, counter movement, splitters, keyboard operation and server disconnect/reconnect behavior.

Determinism tests must include adjacent tiles, overlapping footprints, reversed request order and stable identity after restart. Contract tests must include invalid saves, duplicate assets, stale revisions, unavailable terrain semantics and duplicate outcome delivery.

Before implementation, resolve the exact integration revision, current durable storage conventions, whether initial generated scenes require one board or composed packages, the first supported terrain profile, dated force decomposition and the operational/tactical time policy. These are bounded design tasks, not reasons to delay the C# definition importer and Blazor St. Lo workflow.

The immediate next step is independent campaign contracts and the Blazor host, following C01-C07 in the implementation plan. RCL extraction and host regression checks follow at C08 before tactical integration at C09. Keep further tactical refinement work in the integrated architecture rather than expanding the independent browser prototype.

## Related documents

- [LimboDancer.Campaign Implementation Plan](<LimboDancer.Campaign Implementation Plan.md>) defines the separate host and independent stages. Its implementation sequence takes precedence over the initial migration-stage outline above.

- [Military Command and Multiscale Simulation Design](<../../docs/Military Command and Multiscale Simulation Design.md>)
- [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>)
- [ASL Rule Coverage](<ASL Rule Coverage.md>)
- [ASL Rule Inventory A to E](<ASL Rule Inventory A to E.md>)
- [Campaign prototype README](<../../../prototypes/europe-hex-map/README.md>)
