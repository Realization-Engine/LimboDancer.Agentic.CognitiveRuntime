# Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis

Status: proposed architecture and implementation sequence. 8 October 2026.

## Recommendation

Continue bounded map refinement and force-decomposition work in the HTML/JS prototype, then migrate validated behavior into the separate Blazor application LimboDancer.Campaign. Executable ASL rules remain owned by the existing ASL engine. This separate-host decision supersedes the initial suggestion to host campaign pages in MapStudio. Put campaign identities, mission state, deployment validation, deterministic terrain refinement and tactical handoff in C#. Retain SVG as the map representation and use a small JavaScript layer for browser gestures and rendering operations.

The HTML/JavaScript prototype has reached a useful architectural handoff point, not a technical limit of JavaScript. It has established navigation, campaign/situation relationships, setup and game-map workflows, board composition, palette utilities and interaction conventions. Further prototype work must reuse or adapt existing ASL contracts and avoid duplicating the ASL rules engine.

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

The immediate next step is the bounded HTML/JS formation-to-squad planning slice described in the companion analysis below. Capture its contracts and acceptance fixtures for C01-C07. RCL extraction and host regression checks remain C08, with ASL integration at C09. Prototype work does not replace ASL execution admission.

## Related documents

- [LimboDancer.Campaign Implementation Plan](<LimboDancer.Campaign Implementation Plan.md>) defines the separate host and independent stages. Its implementation sequence takes precedence over the initial migration-stage outline above.

- [Military Command and Multiscale Simulation Design](<../../docs/Military Command and Multiscale Simulation Design.md>)
- [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>)
- [ASL Rule Coverage](<ASL Rule Coverage.md>)
- [ASL Rule Inventory A to E](<ASL Rule Inventory A to E.md>)
- [Campaign prototype README](<../../../prototypes/europe-hex-map/README.md>)

## Formation to squad development update 10 October 2026

The approved direction is continued HTML/JS development while migration plans remain synchronized. See [Formation to Squad Maps and Counter Decomposition Analysis](<Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis.md>) for spatial calibration, persistent child assets, modular JavaScript boundaries and S01-S06 acceptance gates. These phases are proposed, not completed. Preserve campaign-to-ASL dependency direction.

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


## Implemented map-led Situation navigation, 10 October 2026

The overview now renders all 17 imported Situations from a shared campaign/navigation-catalog.js. Nearby anchors form clickable groups at wide zoom; individual labels appear only at close zoom. Mouse or keyboard selection opens a chooser on the central map, then the corresponding command operation. Campaign-local numbers remain separate from source-card numbers. Elsenborn is consistently 04 within Ardennes, including its tailored command card.

St. Lo and Ardennes Thrust/Elsenborn retain their tailored command rules. The other 14 cards now use reusable source-command.js, source-repository.js and source-workspace.js components. This is a deliberately limited authored preparation contract: choose 10 or 20 simulated minutes of staff preparation and standard (30-minute) or priority (20-minute) delivery. The mission unlocks only after order receipt, preparation and supply arrival. Priority currently changes timing only; resource competition, costs and tailored scenario consequences are future work. No source units, transports, board rotations, aircraft handling, or setup restrictions are changed by this generic contract.

The central map displays operation/mission headquarters, depot, supply point and transport progress, and offers the next workflow action above the map. The side panes follow the selected mission and present supporting detail. Mission commitment opens the card, continuation opens the joined source boards in the center, and game launch exposes squad deployment. Saves are keyed by campaign plus source Situation and retain the operation, source hash, formation plan and squad plan together. Existing Normandy and Ardennes saves remain in their original namespaces. A new operation retains its previous stored save, but the new source-operation workspace does not yet provide an archive browser.

Anchors are explicitly approximate regional navigation aids, chosen on the existing atlas grid. They do not georeference source boards. The printed source date, including the disputed Reichswald date, is retained without asserting historical accuracy. Command terrain remains illustrative. These adapters support deployment and inspection, not combat or outcome reconciliation.

Validation: test_campaign_navigation.cjs verifies 17 unique campaign-local routes and 56 preparation paths, readiness gates, replay, source immutability, original setup/launch, save isolation and stale writes. Live browser testing followed every marker through its workflow to squad entry. Each of the 14 new routes was started, gated, committed, deployed on both sides, launched, reloaded and reopened. Goodwood expanded its full 114 ground formations into 530 subordinate counters. The tailored routes were checked with existing saves: St. Lo retained 335 counters, Thrust retained 324; Elsenborn committed its supplied plan and reached squad entry with designation 04. Cluster mouse selection, keyboard selection, wheel zoom separation, and map HQ clicks were checked. Browser testing found and fixed the shared grid-column mismatch, command marker hit area/pointer capture, and stale asset URLs.

For Blazor migration, port the navigation catalog, finite preparation contract, repository and workspace as separate responsibilities. Do not replicate a bespoke command page per card. This implementation supersedes the earlier audit's statement that the 14 source cards lack command bindings; their bindings are now generic preparation adapters, not scenario-specific supply models.


## 11 October 2026: source composition audit and counter information cards

The HTML/JS deployment now loads `tactical/source-composition.js` after the original profiles. It records nominal principal equipment counts and personnel/support inventories from the original Panzer Leader Unit Function Table, printed pages 19-22 (PDF pages 20-23 of the archived original manual). All imported ground types have an evidence record; infantry squad grouping remains a deliberate playable abstraction. Canadian/Belgian equivalents use Commonwealth or Allied rows; German MG teams lack a direct row and remain an explicit assumption. Printed discrepancies (25-pounder and Daimler caliber) are retained rather than silently erased.

Corrected counts apply throughout all 17 Situations and campaign variants. Mixed members are supported: British Sherman formations produce three Sherman 75 vehicles plus one Firefly; the M3 scout formation produces four M3 cars and one Daimler scout car. Each child has a validated definition ID. W Panzer IV/Panther formations contain four vehicles; SS versions contain five, with black backgrounds and white artwork/text. Source-tagged SS colors apply to formation artwork too; no blanket assumption is made that all German forces are SS.

`tactical/tactical-estimates.js` supplies explicit role-based values instead of copying all fields from a tank or mortar template. Exact catalog infantry/halftrack definitions remain usable. Unmatched combat values are provisional, not official ASL transcriptions: there is no new combat engine. Source crew and supporting weapon totals remain information, not independently spawned combatants. Transport capacity and support-weapon allocation still require gameplay design beyond setup.

`tactical/counter-info-card.js` replaces squad counter native tooltips with a keyboard-accessible, hoverable card that can be pinned or closed. It separates tactical values, composition/counts, original printed factors, page evidence, setup constraints, artwork IDs and original notes. Export includes the source counters, rules and composition profiles.

Profile version `source-composition-2` rejects earlier saved decompositions. The UI provides an export of the old setup and an explicit rebuild that retains a local backup before replacing the active squad save. Formation deployment is retained. Rebuild may reposition tactical counters within the existing setup constraints; failure leaves the previous save active. New radius remains six and is adjustable.

Blazor migration contract: port source composition evidence, mixed member identities, tactical estimates, affiliation palette, counter-card presentation and versioned save handling as separate components. Keep the archived source inventory independent of engine-admitted tactical rules.

Validation: all 17 full Situation deployments passed, totaling 7,158 tactical objects; six Ardennes and six Normandy command variants passed. Independent regressions verify the source counts, mixed Firefly membership, invalid member rejection, SS affiliation and role corrections. Browser checks verified Goodwood backup/rebuild, reload persistence, mixed Sherman rendering, four-versus-five W/SS counts, black/white SS artwork, and card pin/close/Escape/I-key behavior. Original table-page images are included as evidence links.


Live UI validation, 11 October 2026: completed the other 16 Situation deployments in addition to Goodwood, covering marker navigation, existing campaign variants, versioned rebuild, all rendered child-group counts, mixed Sherman membership, explicit SS tank styling and source information cards. Full per-Situation results are recorded in the Formation to Squad analysis. Scenario-derived SS infantry affiliation remains a separate pending data task; the source infantry table has no dedicated SS row. Preserve affiliation separately from strength and from the generic counter-type definition.


11 October 2026 affiliation implementation: Campaign Atlas now resolves organizational identity per scenario instance in tactical/scenario-affiliation.js, inherited by squad children and included in export. Port this separately from source equipment profiles and troop quality. Existing deployment positions are unaffected. Mixed Goodwood allocation and probable Marieulles attribution are explicitly labelled; Elsenborn groups follow the card. See the Situation Catalog for evidence and validation. SS affiliation does not automatically confer elite tactical values.


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
