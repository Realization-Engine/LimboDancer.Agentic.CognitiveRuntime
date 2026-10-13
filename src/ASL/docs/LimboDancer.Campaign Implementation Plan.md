# LimboDancer.Campaign Implementation Plan

Status: updated implementation plan, 9 October 2026. The project name is approved; Blazor scaffolding and migration have not started. A bounded HTML/JS command smoke test exists. The HTML/JS prototype now integrates command, supply and time into an authored map workspace; delivery details and remaining limits are recorded below.

Companion: [Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis](<Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis.md>).

## Project decision

Build **LimboDancer.Campaign** as an independent Blazor application at **src/LimboDancer.Campaign/LimboDancer.Campaign.csproj**.

Use Microsoft.NET.Sdk.Web and Blazor Interactive Server, with net10.0 and C# 14 matching the inspected ASL baseline. Its root namespace and assembly name are LimboDancer.Campaign. It owns its shell, routes, configuration, service registration and static resources. Do not put Campaign pages into MapStudio during the independent stages.

Place the app outside src/ASL. Use a dedicated solution at src/LimboDancer.Campaign.sln and tests at src/tests/LimboDancer.Campaign.Tests/. These are planned paths, not files created by this documentation task.

The settings in src/ASL/Directory.Build.props do not automatically apply outside that directory. Declare equivalent nullable, analyzer, deterministic-build and warning policies within the Campaign build boundary. Respect central package version management and audit compatibility before adding dependencies. Prefer the ASP.NET Core shared framework where it already supplies the required capability.

Use an independent launch profile and available development port so Campaign and ASL can run concurrently.

## Independent implementation boundary

C01-C07 must build, run and test without references to LimboDancer.Domains.Asl projects. They must not modify ASL rules, play, map compilation, schemas, unit catalogs, application registration or UI.

These stages can deliver substantial functionality:

| Area | Campaign-owned capability |
|---|---|
| Application | Navigation, workspaces, inspector and Map Utilities |
| Geography | Europe/North Africa overview, theaters and dated campaign areas |
| Definitions | Campaign/Situation registry, numbering, temporal windows and evidence |
| Organization | Persistent formations and dated subordinate relationships |
| Missions | Assignment, receipt, planning, execution status, reporting and assessment |
| Communications | Queued delivery, audiences, timestamps and explicit delay policy |
| Situations | Briefing, roster, deployment, setup completion and map inspection |
| Mapping | SVG scenes, coordinate transforms, board composition and picking |
| Appearance | Approved themes, editable palettes, demo maps, comparison and exports |
| Persistence | Versioned sessions, supported save migration and recovery |
| Engagement planning | Geographic boundaries, objectives, asset reservations and draft status |

Campaign mission execution status is not combat execution. The St. Lo inspection screen must continue to distinguish completed deployment from implemented turn/combat resolution.

Use a campaign-owned implementation checkout without switching, resetting or editing another active ASL checkout. Select a baseline explicitly, but do not wait for unrelated ASL work merely to scaffold the independent host.

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

## Internal organization

Start with one production Blazor project and separated internal modules. Avoid creating empty assemblies in anticipation of future needs.

| Folder | Responsibility |
|---|---|
| Domain/ | Campaign, Situation, formation, mission, communication and deployment records and invariants |
| Application/ | Commands, queries, lifecycle transitions and repository interfaces |
| Infrastructure/ | Definition loading, manifests, persistence and save migration |
| Mapping/ | Coordinates, board composition and SVG scene generation |
| Appearance/ | Semantic theme tokens, validation and approved baselines |
| Components/Pages/ | Atlas, campaign, Situation, setup, inspection and utility routes |
| Components/Shared/ | Viewport, roster, inspector, navigation and splitter components |
| wwwroot/js/ | Pointer gestures, viewport transforms, downloads and interop lifecycle |
| Content/ | Immutable packages, admitted reference data and licensed artwork |

Domain and Application must not depend on Razor, the DOM or JavaScript interop. This allows later extraction of reusable contracts without redesign. Mutable saves do not belong in Content or wwwroot.

Suggested routes: /atlas, /campaigns, /campaigns/{campaignId}, /campaigns/{campaignId}/situations/{situationId}, setup/map child routes, /utilities/counter-colors and /utilities/terrain-colors. Resolve stable IDs and diagnose missing or incompatible content.

## State and interaction architecture

C# owns validated campaign state. JavaScript handles transient pointer positions, live zoom/pan and drag previews. Submit a completed semantic command with map coordinates and expected session revision; C# validates the final action. Avoid server round trips on every pointer movement.

On reconnection, restore authoritative state and discard uncommitted gestures. Dispose browser event listeners when components are removed.

Separate immutable definitions, mutable sessions, generated scenes and themes. Persist definition versions/hashes, schema version, campaign seed, clock, persistent forces, missions, communication state, deployments and reservations.

Initially a configured local file repository is sufficient for a single-user prototype replacement. Require atomic replacement, a recovery copy, validation on load and optimistic revision checks. Coordinate writes by session identity. Do not put undifferentiated mutable session state into a global singleton. Multiuser ownership and authentication need a separate scoped decision.

Import supported prototype exports through versioned adapters. Preserve original input and surface unsupported fields. Do not silently discard placements or turn unknown unit composition into invented historical facts.

## Implementation stages

### C01 Independent host and shell

Create the project, independent solution and tests. Add configuration, service boundaries, shell, empty states and route navigation.

Acceptance: Campaign starts beside ASL, direct route refresh works, no ASL project reference is present and no existing ASL source file is changed.

### C02 Definitions and durable sessions

Load the campaign registry, canonical St. Lo Situation, source manifests and approved counter/terrain exports into validated C# models. Preserve source Panzer Situation 4 separately from campaign-local Situation 01.

Implement reference/schema checks, seed creation, repository persistence and supported prototype-save import.

Acceptance: all 21 counter types and 76 St. Lo instances retain identities and quantities; duplicate numbering and invalid references are rejected; session state survives process restart; stale writes fail explicitly.

### C03 Atlas and campaign navigation

Port overview, theater choice, dated campaign selection and Situation markers. Load large geography as versioned static resources or demand-loaded layers, rather than repeatedly embedding it in component state.

Acceptance: Whole Map, theater and dated campaign views work; campaign-specific content appears in its correct context; marker titles collapse to tooltips at the agreed zoom threshold. Preserve source dates and research limitations.

This stage does not require renewed research for every country or certify an entire wartime transport network.

### C04 Situation setup and map inspection

Implement briefing -> roster/deployment -> setup complete -> larger inspection map. Preserve German-first and American-second setup, A/C orientation, stable hex identity and existing saved placements.

Port SVG presentation through a reusable campaign viewport. Preserve source-artwork comparison, approved colors, counter outlines and selection amber.

Acceptance: placement and inspection work on both rotated boards; side/type groups retain summaries; invalid setup actions fail; panning and zooming never place a unit; deployment restores reliably. Game-map inspection must not imply formation combat is implemented.

This is the first complete replacement of the St. Lo browser workflow.

### C05 Permanent appearance utilities

Port Counter Colors and Terrain Colors. Keep approved baselines separate from draft preferences and explicitly published themes. Include the continuous demo, draggable sample counters, synchronized comparison, splitter, restores and JSON import/export.

Acceptance: palettes round-trip correctly; appearance changes cannot alter force statistics, terrain semantics or board identity; counter dragging and map panning remain distinct.

C05 can proceed alongside map presentation after theme and viewport contracts stabilize. Preview symbols for every terrain type are not evidence of tactical rule support.

### C06 Command hierarchy, supply and temporal map workflow

Port the hierarchy, parent/child workflow, mission assignment and receiving-headquarters planning. Persist messages with issue, availability and delivery timestamps. Implement the integrated workspace and replay contracts specified below: named command nodes in the left pane, geographically located command and supply entities on the central map, and a shared map timeline with historical/current/forecast modes. A separate form page is not completion of this stage.

Acceptance: orders become visible according to delivery policy; workflow completion reveals the appropriate subordinate choices; reports and assessments preserve causal links and audiences. Campaign time is separate from UI actions. Delay rules remain explicit authored policy until historically grounded.

### C07 Engagement planning and coordinate contracts

Select a bounded Situation-map footprint and participating assets. Save its parent coordinates, time interval, objectives, source versions and reservations.

Acceptance: coordinate round trips agree across the A/C seam; duplicate asset allocation and incompatible time reservations fail; unresolved decomposition remains blocking evidence. An EngagementRequest is a draft, not an executable ASL game.

Establish deterministic seed/version contracts and scene fixtures here without introducing the ASL compiler.

### C08 ASL reuse and integration gate

Review the independently running Campaign app against the then-current ASL baseline. Confirm terrain catalog support, board loading, Scenario Card admission, unit-catalog resolution, viewer visibility and outcome interfaces.

C08a: audit and extract the first required reusable map component into LimboDancer.Domains.Asl.Maps.Components. Switch MapStudio to consume it without changing its behavior, then reference it from Campaign. Keep campaign-only atlas rendering outside ASL when it does not fit the ASL viewport contract.

C08b: extract Scenario Card display and the needed setup/play panels into LimboDancer.Domains.Asl.Play.Components. Move only the reusable service contracts necessary for these components. Do not relocate every MapStudio page or its application shell.

C08c: introduce the campaign-owned tactical adapter against the confirmed ASL contracts. It may depend on extracted campaign contracts and ASL APIs, but no ASL library may reference it or Campaign. Extract a small campaign contracts library if needed to avoid depending on the Campaign executable.

Acceptance: both hosts run using the extracted components, relevant MapStudio regressions pass, static assets resolve in both hosts, and project references contain no reverse dependency or cycle.

No tactical schema changes, direct ASL project references or live ASL launch belong to C01-C07.

### C09 Terrain refinement and tactical execution

Generate the first deterministic engagement scene, compile through the existing ASL pipeline, create and admit a Scenario Card, execute through the tactical engine and reconcile its outcome once.

Use the companion analysis for metric coordinates, overlap consistency, catalog support and force decomposition. A division order still produces subordinate missions; a Campaign Situation may produce several bounded ASL scenarios.

## Parallel development and verification

Maintain a documented integration issue list covering generated-board identity/loading, Scenario Card startup, squad/crew/vehicle catalogs, operational/tactical time, visibility and idempotent outcome delivery. Before C08, these are requirements and fixtures, not speculative edits to ASL.

Use the planned RCL extraction path when Campaign needs ASL UI. Until then, keep campaign-only interaction work behind replaceable interfaces rather than copying MapStudio's service layer or creating another tactical UI implementation.

Run a dedicated Campaign suite. Test lifecycle transitions, reservations, date boundaries, identity, migration, restart recovery and stale writes. Browser checks must exercise actual drag/pick/zoom behavior, splitters, routes and reconnects.

Use the HTML prototype as a behavioral and visual reference. Record known omissions so migration parity does not turn an unfinished prototype feature into an accepted permanent limitation. An iframe, static mockup or copied JavaScript state engine is not a completed C# migration.

Independent stages need no changes to the ASL test suite. C08 adds RCL and host integration checks plus the relevant existing MapStudio/ASL regressions. C09 adds tactical integration checks against pinned ASL packages.

Preserve asset provenance and distribution restrictions. Historical research-only layers must not become published gameplay dependencies merely because they were visible in the prototype. Retain Python preprocessing initially.

## First work package

P01-P05 below now have a bounded, browser-tested prototype implementation. After review of that interaction model, begin Blazor migration with C01 and C02:

1. Select a campaign-owned implementation checkout.
2. Scaffold LimboDancer.Campaign and its tests.
3. Import registry, St. Lo definitions and approved appearance baselines.
4. Validate and persist one CampaignSession.
5. Display the campaign list and Situation briefing in Blazor.

Proceed to C03/C04 next. Tactical refinement and unrelated ASL work are not prerequisites for this independent implementation.


## First command-domain implementation target, 9 October 2026

Use the [Ardennes Command Decision Contract Specification](<Ardennes Command Decision Contract Specification.md>) as the concrete domain/test target for command, supply, leadership and optional complexity. It replaces the earlier St. Lo alternative-task example, without changing the project dependency direction or authorizing ASL rule changes.

Implement versioned commands, resource/custody accounting, event scheduling, knowledge delivery and immutable Situation variants before wiring the Blazor decision cards. Preserve the standalone imported-card workflow. The first end-to-end demonstration ends at committed deployment unless an admitted resolver exists; isolated result fixtures test reconciliation and assessment without being presented as combat. Readiness, source fidelity and capability gates remain separate.

## Prototype phase: command and supply on the campaign map

This section records the accepted walkthrough feedback and timeline proposal. It supersedes the standalone exercise page as the intended user experience. The existing [Ardennes smoke test](<Ardennes Command Prototype Acceptance.md>) proves a bounded decision loop; the integrated spatial and temporal experience below is now implemented within the limits recorded in the delivery section.

### Purpose and first scope

A player must be able to see whom they command, where supplies originate, where they are going, when they arrive and what a decision changes, without reconstructing the operation mentally from a ledger.

Keep the first operation bounded to a defined command hierarchy, two competing defensive missions, one depot, one transport column, one reserve and two receiving supply points. Retain PL14 and PL15 as source Situations. Their shared headquarters, logistics network, travel assumptions and reserve remain authored unless separately substantiated. Source boards are not automatically georeferenced historical ground. Record approximate or authored placement explicitly instead of implying that their roads match a real transport network.

No new tactical combat rules, reverse ASL dependencies or wholesale organization simulation are required. Every command echelon introduced must own a consequential decision; do not add empty levels simply to lengthen the descent.

### Workspace and command identity

- Left pane: the command hierarchy, selected node and only that node's current decisions. Completed parent decisions reveal the applicable subordinate choices. Show the path of named headquarters and their echelons, not a repeated generic "Parent coordinating HQ" label.
- Center: the persistent campaign map, the primary surface for objectives, forces, commands, supply and time. The map remains visible while decisions are made. Adapt the existing shell rather than embedding the standalone exercise page.
- Context inspector: concise details for a selected map entity, route, event or order. It supplements the left workflow rather than duplicating decision forms.
- Map toolbar: campaign date, campaign time, viewing time, timeline controls and an unambiguous Historical view / Current / Forecast indicator.

A command node has a stable ID, display name, echelon, dated superior/subordinate relationships, commanded formations and map location with provenance. A mission is assigned to a command node; it is not itself an organizational echelon. Define the first three command nodes explicitly, including authored echelon assignments where evidence is absent, before drawing their counters. Display readable names to players; keep identifiers such as R1 and M14 as secondary details.

Selecting a hierarchy node selects its HQ counter and frames its area when requested. Selecting an HQ counter selects the same workflow node. Preserve user zoom and pan where possible. Operational HQ markers and logistical symbols must remain distinct from the platoon/company combat counters on Situation boards.

### Physical supply chain and visual feedback

Represent a connected chain: depot -> transport column -> receiving formation supply point -> supported units. Every shipment names its origin and destination and records cargo, capacity, custody, route, departure, expected arrival and actual arrival. Show quantities in explicit authored units such as ammunition packages; do not imply rounds or tonnage without a definition.

Use visible symbols for headquarters, depots, transports, reserves, receiving points and mission objectives. Provide separately toggleable command relationships and supply routes, a compact legend, selection highlighting and status shapes/text as well as color. Aggregate symbols at distant zoom levels; reveal detail on selection or closer zoom.

Before allocation is committed, preview the chosen routes, supported missions and expected readiness changes on the map. Distinguish a proposed route from an active shipment. On commitment and event advancement, show departures, progress, receipt and status changes at the relevant locations. A blocked route visibly interrupts the supply chain and explains which deliveries and missions are affected.

A transport's journey requires a route geometry and an explicit travel-time policy. For the first authored operation, route-segment durations may be fixed. Interpolate display positions from those durations; interpolation must not create deliveries, consume resources or change the event log. Held transports remain at their recorded interruption point. Recall uses an explicit return journey, not a teleport to the depot. Receipt transfers custody once and updates the receiving formation's supply state.

### Shared time and timeline scrubber

Maintain separate campaign time (the latest authoritative simulated point reached) and viewing time (the map moment being inspected). Wall-clock waiting and playback speed are presentation concerns and must not change readiness or travel rules.

The persistent toolbar provides a labeled scrubber, play/pause, playback speed, previous/next event, current date/time and Return to current time. Show event markers for order issue/receipt, departures, arrivals, mission commitments and reports. Events at the same timestamp retain deterministic sequence and can be inspected individually. Use readable event descriptions rather than internal event codes.

| Viewing mode | What the map displays | Permitted actions |
|---|---|---|
| Historical view | Recorded state and knowledge at the selected time | Inspect and replay; no campaign commands |
| Current | Latest committed campaign state | Eligible decisions and explicit advancement |
| Forecast | Scheduled consequences under current assumptions | Inspect projections only; no state mutation |

Scrubbing backward is read-only reconstruction, not undo. Moving forward within recorded history replays that history. Returning to current time restores live controls. The scrubber alone never advances the authoritative campaign or commits a choice. Keep a distinct Advance to next event action for actual progression. Playback stops at the live boundary; continuing into a forecast is explicit and remains read-only.

Shade the future region and distinguish predicted markers/routes from recorded ones. Forecasts include only consequences supported by the current schedule and viewer knowledge. They must not reveal hidden enemy actions, invent future decisions or present delivery estimates as guarantees. Show the forecast horizon and assumptions; invalidate and recompute a preview after a relevant command or disruption.

For example, viewing 06:10 through 06:40 should show a supply column moving from its named depot along its route. In recorded history, its actual arrival transfers cargo and changes supply status. In a forecast, the same expected arrival is labeled predicted and changes no live resources.

Historical views honor information available to the selected player at that time. Distinguish event occurrence from report availability. A loss cannot appear in the parent's historical knowledge before its report arrives. Do not expose full authoritative state through a history or forecast inspector. An omniscient review mode, if later added, requires an explicit separate policy.

Campaign branching is deferred. A future Branch campaign from this point command must create a separate identity and preserve the original history, references and seed policy. It must never be an implicit effect of dragging the scrubber.

### State, replay and migration contracts

Extend campaign-owned records with located command nodes, supply facilities, routes and dated route segments, shipments, transit intervals, reserves and objective locations. Preserve source provenance and effective dates independently from display styling.

Use a versioned event record with event ID, campaign ID, sequence, effective time, recorded time, command/correlation IDs, affected entity IDs, payload version and visibility/availability policy. Record accepted decisions and resulting changes sufficiently to replay the operation without rerunning nondeterministic external logic. Persist seed and definition hashes. Reject stale commands and reconcile resource transfers once.

Separate authoritative state, viewer-filtered knowledge, historical projection, speculative forecast and transient UI selection. Reconstruct from the initial state plus ordered events; add versioned snapshots as an optimization with equivalence tests against full replay. The current command log is a starting point, not proof that all map state and historical knowledge can already be reconstructed.

Use pure time-indexed map projections. Cancel or discard stale projection requests during rapid scrubbing so an older response cannot replace the selected moment. Session reload restores the authoritative clock and can restore viewing time without mistaking it for live state. Empty timelines, unavailable source versions and unsupported saves require clear diagnostics.

In Blazor, C# owns these states and projections. JavaScript may animate/interpolate and handle slider gestures, but cannot authorize decisions or resource changes. The same domain fixtures should verify the prototype behavior and later C# implementation. Existing ASL integration boundaries remain unchanged.

### Ordered implementation work

| Stage | Deliverable | Acceptance gate |
|---|---|---|
| P01 Spatial operation definition | Named/echeloned command nodes, depot, receiving points, reserve, routes and provenance | Every delivery has a visible named origin/destination; every mission has a responsible command node; authored geography is disclosed |
| P02 Integrated workspace | Existing Atlas left hierarchy linked to central map counters and contextual decisions | Selecting a node or HQ counter selects the same command; only its controls show; map remains present throughout decisions |
| P03 Visible logistics and clock | Allocation previews, convoy movement, custody/status changes and shared map toolbar | A player can follow a delivery from origin to recipient; interruption/receipt visibly changes the relevant chain; no duplicate stock or reserve |
| P04 Historical scrub and replay | Recorded-history slider, event navigation, playback and return-to-current | Scrubbing is read-only; past positions and delivered knowledge reconstruct correctly; current state is unchanged after a round trip |
| P05 Forecast and browser acceptance | Clearly separated scheduled preview and integrated workflow tests | Future estimates are visibly conditional, reveal no unavailable information and mutate nothing; complete map-centered walkthrough passes |

Complete and smoke-test these stages in HTML/JS before treating the current forms as migration-ready. C01-C03 subsequently establish the host, durable temporal definitions and integrated shell; C06 ports the validated command/supply/replay workflow. C04 retains Situation deployment behavior. This sequence does not require ASL tactical integration to begin.

### Verification and completion criteria

Automated checks must cover conservation and exclusive custody; route interruption, reopening and recall; simultaneous event ordering; idempotent commands; snapshot/full replay equivalence; history before report arrival; forecast invalidation; no state mutation from scrubbing/playback; and persistence across reload. Test that changing playback speed cannot affect outcomes. Preserve imported cards and separately identified committed variants.

Browser-test the actual local-server application: choose a campaign, select a command node, preview and commit an allocation, watch a shipment depart, inspect its cargo and endpoints, advance to receipt and open the resulting Situation. Then scrub back before dispatch, forward through arrival and return to current. Verify both synchronized selection directions, disabled historical decision controls, forecast labeling, keyboard slider/event navigation, map pan/zoom, overlapping markers and console errors.

Keep isolated test outcomes clearly labeled; neither timeline playback nor visual movement supplies a missing combat resolver. A complete demonstration ends at deployment until a real resolver is admitted.

The user-facing acceptance criterion is: without opening the ledger, a new player can identify the selected headquarters and echelon, trace the supply chain, distinguish current time from viewing time, and see why one mission can act before another. The standalone exercise page may remain a developer harness, but is not the normal player entry point once integration is complete.

## Integrated prototype delivery, 9 October 2026

P01-P05 now have a bounded HTML/JS implementation at `index.html?exercise=ardennes`. The old exercise URL redirects there. Command decisions occupy the left pane of the Atlas; a persistent operational SVG map and timeline occupy the center. Selection works in both directions between hierarchy nodes and HQ markers. Generated Situation Cards remain connected through their setup/return flow.

The first spatial definition is an explicitly authored diagram: one division HQ, two regimental HQs, depot, supply points, mobile reserve, transport column and objective flags. It is not georeferenced terrain or a documented historical command network. Roads and travel durations are illustrative. A guns-first circuit confirms both recipients together at circuit completion, preserving the existing authored timing contract.

Implemented temporal behavior includes replay from accepted commands, interpolated convoy positions, read-only historical scrubbing, explicit campaign advancement, playback speeds and scheduled forecasts. Current and viewing time are separate. Historical/forecast modes remove live command controls. Forecasts suppress undelivered outcome contents. Events at the same minute are displayed at the resulting end-of-minute state; sequence-level stepping within a minute remains future work. No snapshots are required for this small event log; snapshot acceleration, persisted viewing preferences and branching remain deferred.

Automated checks cover command/deployment invariants, route interpolation, held/reopened/recall journeys, finite custody, replay, forecast non-mutation and report visibility. Browser checks cover hierarchy/map selection, history/forecast modes, return to current, live receipt status, reserve readiness, generated card navigation and playback. The original 17 imported-card regression suite remains green. These checks do not imply implemented combat or historical logistics accuracy.

### Navigation cleanup delivered

The player UI now separates Campaign session actions from Map Utilities. The old free-form Normandy command workflow and communication-step clock are inaccessible, along with stub formation phases and test controls. Saved data remains intact. Standalone reference Situations are under Map Utilities; committed mission variants retain their Situation setup route. Session archive/new/export actions have moved out of command decisions into the Campaign menu. This removes competing entry paths but does not itself complete shared campaign ownership or seed-based spatial integration.

## Campaign ownership and continuous-world slice (9 October 2026)

Implemented in the HTML/JS prototype before the Blazor port:

- A campaign save owns the map seed, generator version, command operation (clock, hierarchy, stock, missions and command history), world nodes, roads, river, persistent roster, deployment history and terrain events. Existing command-only sessions migrate without deleting their originals. Campaign exports and saved-session selection use the campaign envelope.
- The Ardennes campaign has an authored 40 km by 16.8 km footprint anchored in the atlas EPSG:3035 coordinate system. Local coordinates are meters east and south of that origin. This is an illustrative region, not a reconstruction of historical deployment locations or roads.
- A committed M14 or M15 mission binds its supply-adjusted roster to an 8 km by 6 km footprint. Setup uses persistent campaign unit IDs and 250 m hexes. It no longer opens the imported board package as though that package were contiguous campaign geography.
- Campaign (1 km), formation (250 m), and tactical terrain preview (40 m) read the same fixed world features. Seeded woodland geometry is independent of the requested grid. The overview shows the same footprint and features through the atlas transform. Terrain events and deployment history are time-filtered.
- Deployment enforces side-specific west/east halves, one counter per hex, current-time editing and completion only after all counters are placed. These are explicit prototype adaptation rules.

Remaining work: researched/georeferenced terrain and road provenance, full campaign-wide spatial refinement beyond this authored region, adapted mission victory conditions and special rules, force decomposition into ASL Scenario Cards, terrain-compiler admission and combat reconciliation. The 40 m view is a terrain preview, not an admitted ASL scenario. Imported Panzer packages remain reference/standalone material. Changing geography invalidates their board-specific rules until those are deliberately adapted.

Validation: `test_campaign_world.cjs` covers common coordinates, grid overlap identity, deterministic terrain, mission/force binding, deployment restrictions, historical placement, terrain events, persistence and stale-write rejection. Existing command and spatial replay suites also pass. Browser verification covers supply receipt, mission commitment, shared-world setup and persisted placement.

### Integration correction: retain established Situation screens

The campaign command workspace now embeds the established Situation Card, setup and game-map screens. Committed command decisions produce the roster variant. The card occupies the right pane, the selected headquarters remains left, and the existing joined-board renderer and counter tray occupy the center. Setup-to-game completion continues to use PanzerSituationModel validation. Plans save in the campaign envelope under situationPlans rather than a parallel standalone save. Original imported packages are not modified.

The simpler generated-terrain deployment screen is no longer the primary campaign path. Its existing placements remain preserved and are not reinterpreted as source-board positions. Original boards retain local coordinates with an explicit geographic limitation. Unifying those maps with generated campaign terrain remains unfinished. During source-board setup the campaign clock remains visible; timeline editing resumes after returning to command. Tests cover embedded setup/game/review transitions and campaign save callbacks, with browser verification of the three-pane map display.

## Prototype formation to squad track 10 October 2026

Continue the HTML/JS prototype before scheduling the corresponding Blazor port. The new [Formation to Squad Maps and Counter Decomposition Analysis](<Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis.md>) defines S01 spatial contracts, S02 force decomposition, S03 deterministic terrain, S04 integrated planning, S05 ASL export admission and S06 reconciliation. All are pending.

Carry spatial frames, refinement manifests, dated organization profiles, persistent asset identities and reservation fixtures into C01-C07. Keep the initial campaign-only stages independent of ASL. S05 interoperability remains a campaign-owned adapter consuming existing ASL contracts and does not authorize ASL project references back to Campaign. Port browser-validated workflows after their domain invariants pass; retain SVG and small gesture modules. Do not port a single combined tactical view/model file.

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


## Next integration task: campaign overview markers, 10 October 2026

Audit after the St. Lo browser test: only St. Lo currently has a parentMapLocation. The overview renderer creates one selected Situation marker, rather than a marker collection. Command adapters exist for St. Lo (source 04) and the Ardennes Thrust/Elsenborn missions (14/15). The other 14 imported cards support formation and squad deployment but have no command-operation binding. Completing squad support does not complete campaign navigation.

Introduce a shared navigation catalog keyed by stable campaign and Situation identities. Each entry should record campaign-local display number, source-card identity, geographic anchor and its precision, command operation/mission destination, and integration status. Render eligible entries as a collection, retaining zoom-dependent labels/tooltips and handling overlapping anchors. Marker selection must enter the corresponding command mission and respect commitment gates. Cards without a command adapter must remain explicitly reference-only rather than imply a complete workflow.

Reconcile catalog numbering with operation labels, especially Elsenborn, whose source catalog number within Ardennes is 04 while the two-mission exercise labels it 02. An approximate town anchor does not georeference its printed boards. Preserve that distinction. Acceptance should cover every catalog entry, map/card/command selection synchronization, campaign-local numbering, readiness gates, and reload/resume. This task is identified, not yet implemented.


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


## Integrated supply baseline status, 11 October 2026

Implemented and tested in the HTML/JS prototype: basic supply bindings for all 17 Situations, shared board geometry in command/formation/squad views, selectable depot/HQ/receiving/transport markers, route-based scheduling for new operations, saved spatial manifests and legacy schedule preservation. Existing command engines remain authoritative for stock and mission readiness. The fresh reinforced St. Lo workflow reaches 345 squad counters from 78 formations.

During migration, extract independent Campaign services for spatial bindings, supply custody/scheduling and map presentation. Keep reinforcement assignment, arrival and readiness separate; fuel-dependent reserve transfer is a scenario policy. Do not move these ownership responsibilities into ASL libraries.

Next spatial work: register board footprints and external supply entries into the campaign geography, replacing legacy generated-world references through an explicit migration. Next advanced logistics work: validated closures/rerouting, capacities, selected supply-crisis Situations and idempotent tactical-result reconciliation. See [Supply analysis](<Campaign Atlas Supply and Shared Map Integration Analysis.md>) for exact implementation limits.


## Prototype synchronization: advanced logistics and geography, 11 October 2026

The HTML/JS prototype now includes optional route blockage/escort decisions, finite repair and escort resources, held cargo, recall/redispatch, connected-road detour support, transport return and deterministic replay. All 17 imported Situations have reversible EPSG:3035 registrations. Atlas detail uses the same joined source boards and supply identities as command, formation and squad views. Geographic placements are authored approximations; continuous intervening terrain and continent-wide rear-area transport networks are still pending.

The migration boundary should preserve the command ledger as authority and treat continental snapshots as a derived view index. Port logistics rules and coordinate transforms into separate C# services, with Razor components for map logistics decisions and supply cards. Preserve source board orientation, registration version, immutable source data, campaign identity, package custody and read-only replay. Do not reintroduce a separate abstract command diagram or duplicate inventories by scale.

See [Campaign Atlas Supply and Shared Map Integration Analysis](<Campaign Atlas Supply and Shared Map Integration Analysis.md>) for the implemented scope, module boundaries, verification and remaining combat/network work. This is prototype validation, not completion of the Blazor migration.


### Rear supply and continental refinement synchronization, 11 October 2026

The prototype now supports optional finite rear-base transport for all 17 Situations, depot receipt and handover, separate local transport, vehicle return, multi-day timestamps, and atlas-based event advancement through the same campaign repositories. Stable continental hex refinement fills terrain between source footprints using existing land/lake/forest geometry and a versioned world seed. Authored corridors connect to existing source-road depots; they are not a historical continent-wide road graph.

Port rear route profiles, custody transitions and terrain sampling as separate C# services. Preserve route/version snapshots, shared depot IDs, optimistic revisions, replay and optional complexity. A save compression adapter preserves existing large prototype campaigns. See the supply analysis for current test evidence and remaining recurring-replenishment, fine-terrain and combat-interdiction scope.


### Situation-aware logistics placement, 11 October 2026

The HTML/JS prototype now derives supply corridors from source deployment constraints, rotated board geometry and opposing setup areas. Port the placement policy as a separate service; preserve shared facilities and distinguish local distribution from established rear access. Saved event timing remains immutable when correcting display geometry. The Supply and Shared Map Integration Analysis records the rules, validation and remaining multi-branch/control work.


### Receiving-area setup policy, 12 October 2026

The HTML/JS prototype now shares a two-formation-hex receiving-area policy across all 17 Situations and both deployment scales. Port this as a separate effective-setup policy service alongside supply placement, not as edits to printed Situation rules. Preserve source allowances, shared polygon identities, parent squad-radius enforcement, loaded pairs, existing player positions and event history. New automatic setups assign existing infantry to SUP. Render the receiving footprint and distinguish exposed receiving points, exposed approaches and direct opposing-unit blockage. Board-edge clipping must be explicit. Deployment coverage is not territorial control. See the receiving-area section in Campaign Atlas Supply and Shared Map Integration Analysis for the implemented scope and remaining combat/bridge-control work.


### Situation workflow navigation correction, 12 October 2026

Use one state-aware navigation component for mounted campaign Situations: Situation Card, Formation setup, Launch/Open formation game map, and Squad deployment. The command pane must identify the current surface. Map entry must be repeatable from card, review or an already open map; no action may silently stop because the card is no longer the active stage. Completion remains gated by both sides' deployment, and squad entry remains gated by formation launch. Returning from squads restores the formation game map without resetting saved placements. The prototype implements this in campaign/situation-navigation.js and the shared Situation screen. Squad deployment is explicitly identified as deployment, without implying implemented squad combat.

Verified the saved Fortified Goose Egg campaign through card, setup, formation game map, squad deployment and return, including repeated map entry. Existing setup/game UI regressions and all 56 source-command preparation paths pass; an additional regression covers repeated map entry from review and maneuver.


### Separate squad setup and game views, 12 October 2026

Formation game-map entry now requests the Squad game map explicitly. A complete saved full-Situation squad deployment opens in game mode; an incomplete deployment opens setup with an explicit completion message. Setup provides Complete squad setup and open game map, gated by all ground formations and placement validation. Game mode retains inspection, supply, pan and zoom, but disables placement gestures and hides deployment, radius and clearing controls. Review squad deployment returns to editing without changing units. Both modes use the same saved deployment; squad combat remains unimplemented. Port the lifecycle gate separately from map rendering (prototype: tactical/squad-lifecycle.js). Command-pane navigation distinguishes squad deployment from squad game map.


### Setup relocation of supply markers (2026-10-12)

Formation and squad setup permit moving mission HQ and SUP from their information cards: choose Move, then a destination hex. Game maps display saved positions without offering relocation. These campaign markers can extend beyond source-card troop setup boundaries. Destinations remain on land on the existing network board, clear of opposing deployed units. SUP requires a connected source road (final connector at most one formation-hex spacing) and a two-hex receiving margin clear of opposing troops; edge clipping is recorded.

Overrides live in the Situation plan and apply at both map scales. SUP relocation updates its route, distance and receiving area. Previous receiving-area deployment permissions are retained to preserve existing troop placements. No troops are automatically moved. This is setup editing, not timed logistics movement; existing stock and event ledgers are unchanged. New network generation prefers a farther-forward eligible road position; saved operational bindings remain unchanged until edited. Propagating these setup overrides into the command/continental operational ledger and timed relocation are follow-up integration work.

Verified: all 17 formation/squad deployment checks, logistics regressions, relocation save/reload and invalid-target tests, and live Goodwood formation/squad consistency.


### Portable campaign saves (2026-10-12)

The HTML/JS Campaign menu now provides Save campaign to file and Load campaign from file for source Situations, Normandy, and Ardennes. The versioned JSON envelope contains the complete operation world, including command progress, board supply bindings, formation plans, squad plans, and player supply adjustments. Loading validates the campaign model before writing, preserves unrelated saves, backs up an existing same-identity save, and rolls back failed writes. Browser viewport and map camera settings are not transferred. The Blazor port should preserve this file contract and add explicit migrations when the schema changes.
