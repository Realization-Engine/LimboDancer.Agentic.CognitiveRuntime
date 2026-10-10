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
