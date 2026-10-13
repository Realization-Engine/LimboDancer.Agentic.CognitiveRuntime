# Campaign Atlas Supply and Shared Map Integration Analysis

Date: 11 October 2026

Status: Proposed architecture and first implementation scope. All Scenarios require basic supply on integrated geography; advanced supply is selective. This document records the supply and spatial integration discussion; it does not indicate that the proposed systems are implemented.

## Purpose and conclusion

Supply should create understandable choices about which forces can operate, where, and by when. Headquarters, depots, convoys, supply points and combat formations must inhabit one persistent campaign world. The central map is the primary interface for those choices. Side panes explain and execute the decision associated with the selected map object.

The existing command maps demonstrate scheduling and allocation, but their connections to imported Situation boards remain incomplete. Grounding those boards and logistical entities in common geography should precede expansion into more commodities or detailed consumption calculations. Practical, playable geography is sufficient; historical surveying accuracy is not a prerequisite.

## Required baseline for every Scenario

Every Scenario must use the basic supply design on the integrated campaign, formation and squad geography. This includes all existing imported Situations, their campaign variants and future Scenarios. Abstract command-map placement is transitional and does not satisfy this requirement. Advanced supply is an additional capability for selected Scenarios, not a prerequisite for receiving the integrated-map baseline.

The required basic design includes:

- A campaign placement for the Situation footprint, preserving source board layout, orientation and local hex identities.
- Persistent physical positions for relevant command posts, depots and receiving supply points in the same world as the formations. Facilities outside the footprint remain connected through named boundary entry points.
- At least one explicitly defined supply connection for each represented force that requires supply. A force with no available connection must be deliberately authored as isolated, with its local allowance and limitations stated.
- Defined ammunition and fuel allowances where applicable, with compatible recipients and a clear mission effect. A commodity that is not relevant to a Scenario need not be invented.
- Consistent stock custody, allocation and receipt. When delivery is simulated, show origin, destination, cargo and route-based timing. When supplies are already delivered at Scenario start, record that initial condition rather than forcing a ceremonial delivery sequence.
- Map selection that connects the supply object to command choices and Situation readiness, using the same state in every pane and scale.
- Preserved remaining allowances and commitments when progressing between views or linked Situations. No automatic resupply from reopening a map.

Basic supply can be automatically managed. It does not require an ambush, competing missions, a route-choice puzzle or detailed consumption in every Scenario. It does require a visible, coherent connection between the force, its supply and the actual battlefield geography. Use published authored package rules where detailed accounting is unnecessary.

Apply the same baseline to the represented sides. A non-player side may use an automatic policy and retain hidden logistical information under the visibility rules; integration does not expose its depots or routes to the opponent.

## Advanced supply for selected Scenarios

Selected Scenarios add constraints that create worthwhile logistical decisions. Examples include competing mission demands, limited reusable transport, alternate routes, capacity limits, route interdiction, convoy escort, bridge repair, supply capture and conditional corridor-defense Situations. Mortain and Veghel are candidates for this layer.

Declare each Scenario's supported advanced features and their authored parameters. A Scenario without advanced features still has the full basic integrated-map supply design. Do not retain an abstract map as the simpler alternative.

Content capability and player complexity are separate settings. The Scenario defines which supply problems exist; the player's Essentials, Operational or Detailed profile determines which supported decisions are automatic and how much detail is exposed. Automation cannot remove a mandatory shortage or blocked route. Unsupported advanced features should not appear as selectable options.

## Current implementation and gaps

| Area | Current behavior | Consequence |
|---|---|---|
| Normandy supply | A finite ammunition package, fuel package, transport and optional Sherman reserve; authored delivery timing | Sending support has little competing demand, making allocation less interesting |
| Ardennes supply | Two competing missions, shared transport, deliveries and return trips | A useful foundation for a reusable supply system |
| Generic Situation preparation | Standard or priority delivery with different fixed durations | Priority has no modeled opportunity cost; preparation is mostly an unlock sequence |
| Stock | Named packages change location or status | Quantity, compatibility, duration of support and remaining allowance are not consistently visible |
| Map | HQ, depot, transport and route graphics | Objects primarily explain a schedule rather than support map-based logistical decisions |
| Situation commitment | Supplies enable an authored roster and become issued or consumed | Little continuing supply management after deployment begins |
| Geography | Imported boards have local layout and rotation; command locations and routes are authored separately | Correctly joined boards do not establish a geographical connection to the command map |

Ardennes also has route interruption and recovery behavior restricted to test sessions. That is not yet an ordinary player-facing route-management feature. Supply implementations differ between Normandy, Ardennes and generic source operations and should converge on common rules with scenario-specific data.

Code inspected: `normandy/command-state.js`, `normandy/workspace.js`, `normandy/map-view.js`, `normandy/situation.js`, `ardennes-command-state.js` and `campaign/source-command.js`, under `prototypes/europe-hex-map`.

## One world at multiple scales

Campaign, formation and squad views are windows into shared geography. Zooming or changing scale must not relocate a headquarters, create an unrelated road, move a depot or replace a bridge with an independent copy.

| View | Primary content |
|---|---|
| Campaign | Command posts, depots, major routes, convoys and Situation footprints |
| Formation | Selected Situation footprint, formations, local command posts, supply points and connecting roads |
| Squad | Refined terrain and decomposed tactical units within part or all of that footprint |

A depot need not be inside the formation map. A rear depot can remain kilometres outside its boundary while retaining its world position and a valid supply connection. At formation scale, show its road entry point and an off-map indicator identifying the origin, distance and delivery state. Distance values must come from the authored route, not decorative labels.

Keep organizational and physical identity separate. A headquarters organization defines authority, subordinate relationships and appointments. Its command post has a position and may move. A depot is a facility with capacity and access; stock held there is separate state. A shipment is cargo assigned to a transport journey, not the truck itself.

## Binding imported boards to campaign geography

Preserve each source board's coordinates and the Situation Card's board order and rotations. Add a campaign placement for the joined layout rather than modifying the archived source.

Each placement needs:

- Campaign identity, Situation identity and a world footprint.
- A common coordinate reference, origin, orientation and explicit metres per map unit.
- A reversible transform between board coordinates and campaign coordinates.
- Named boundary connections tied to specific road exits, crossings or transfer points.
- Persistent feature identities and a terrain-generation version, alongside the campaign seed.
- Provenance distinguishing source terrain, authored campaign placement and generated surroundings.

The campaign view should display the same placed board terrain at reduced detail. Generated surroundings must connect to its declared road and river boundary features. A shared seed alone cannot guarantee this. Features shared by adjacent footprints need one authoritative identity and geometry; conflicting board edges need an authored connection or an explicit discontinuity.

Where two imported Situations cannot credibly occupy the same geography, treat them as separate authored placements or alternate scenario variants. Do not silently stack incompatible roads, towns or bridges. Reuse of one physical board image does not mean that every scenario using it represents the same physical place.

The initial bridge between existing systems can be an explicitly authored campaign route ending at a board entry point. This is useful playable integration even before detailed surrounding terrain is generated. It must not be described as a historically georeferenced reconstruction.

Destroyed bridges, moved command posts and other wartime changes belong in dated persistent state layered over terrain. They must appear consistently at every scale and in timeline replay.

## A concrete supply loop

The recurring player question is: Which force can I support, where, and by when?

1. Select a mission or force on the map. Highlight its supply chain, demand and known shortfall.
2. Inspect feasible delivery plans. Show origin, cargo, transport, route, arrival time and the opportunity cost to other missions.
3. Commit a structured allocation. Reserve the stock and the required transport capacity.
4. Dispatch and follow the convoy on the map. Cargo in transit is not available at either endpoint.
5. Receive supplies at a named supply point, then issue them to the intended force through an explicit local connection or handling delay.
6. Carry remaining stock, expenditure, losses and transport availability forward into subsequent missions.

Packages remain appropriate. Define what each package supports, its compatible recipients and the mission allowance it provides. Do not introduce individual bullets or fuel gallons merely to make the model appear more detailed.

Distinguish reservations from custody. Reserved stock is still physically at its origin but unavailable to another allocation. Dispatch transfers custody to a shipment; receipt transfers it to the destination. Issued supplies belong to the recipient force and are not automatically expended. Expenditure and loss require explicit events.

Until tactical combat supplies actual expenditure, use a published campaign expenditure rule at a defined mission boundary. Do not charge both that allowance and simulated individual shots for the same ammunition. Cancellation releases only recoverable commitments; recalling a convoy does not instantly restore stock to its depot.

## Map interaction and pane responsibilities

| Selected object | Visible information | Structured actions |
|---|---|---|
| Depot | Available and reserved stock, transport availability | Allocate stock, release reservation |
| Mission or formation | Demand, shortage, support received, earliest readiness | Request support, reduce scope, wait |
| Route | Endpoints, travel time, capacity, known obstruction | Choose an available route or reroute |
| Convoy | Cargo, origin, destination, progress and estimated arrival | Dispatch, hold or recall when legal |
| Supply point | Delivered stock, compatible recipients, last-mile connection | Issue or retain supplies |

Selection on any surface must highlight the same entity on the map. The left pane provides command choices and their consequences. The right pane shows Situation readiness and resulting roster or support changes. Neither pane should become a parallel workflow with an unexplained handoff.

Show direction, convoy progress and blocked connections directly on the map. Use compact labels and expandable information cards so logistics detail does not obscure terrain. Represent shortages through symbols and text as well as color. Disable invalid actions with a known reason and a feasible remedy, without revealing hidden enemy information.

Illustrative authored plan card: one ammunition package enables the withheld St. Lo 57 mm formation; delivery at 06:40; transport available again at 07:10; another shipment waits until then. These are example planning terms, not historical transport data.

## Meaningful choices and optional complexity

Priority delivery needs a real trade-off, such as exclusive transport use or delay to another mission. A route choice needs different known travel times, capacities or constraints. If there is no competing demand, do not manufacture a choice between an obviously useful shipment and purposeless retention.

Normandy should receive either a second genuine demand or a defined future reserve requirement before asking the player to manage scarcity. Ardennes already offers two missions competing for shared transport and is the better initial supply validation case. Neither requires inventing additional combat Situations immediately.

| Profile | Player responsibility |
|---|---|
| Essentials | Automatic allocation under an inspectable policy; deliveries and consequences remain visible |
| Operational | Choose priorities, packages, transport and available routes |
| Detailed | Add compatibility, handling delays, route capacity and disruptions |

All profiles use the same underlying stocks and commitments. Automation changes who decides, not the amount of supply. Preserve state when changing a supported profile; reject unsupported conversions rather than refilling the army.

Supply can later generate Situations such as securing a junction, protecting a crossing until a convoy passes or relieving an isolated formation. A blockage does not automatically create combat. Contact, opposing forces and engagement rules must establish whether a tactical Situation is warranted.

## Supply routes as contested objectives

Supply lines are integral strategic and tactical game objects. Enemy penetrations can threaten the roads, bridges, depots and deliveries supporting specific formations. The resulting command decisions include continuing an offensive, diverting a reserve, escorting a convoy, reducing mission scope or restoring a broken connection.

A dated enemy attack can be an authored campaign event. Its success must follow the game state and resolution rules, not a predetermined historical outcome. Schedule the opportunity and initial forces within a plausible historical window; do not automatically cut a supply route when the clock reaches that date. Structured choices remain deterministic in their validation, while combat outcomes use the supported resolution rules and recorded randomness.

### Historical candidates

| Candidate | Historical starting point | Proposed gameplay |
|---|---|---|
| Mortain, 7 August 1944 | German counterattack toward Avranches intended to cut off American forces advancing beyond Normandy | Defend the corridor and decide how much offensive strength to divert against the penetration |
| Veghel, 22–24 September 1944 | German attacks cut the Market Garden corridor along Hell's Highway | Protect deliveries, contain attacks, clear roadblocks and restore passage before forward supply allowances are exhausted |

The Mortain objective is described in [Army University Press, Gaining the Advantage](https://www.armyupress.army.mil/Journals/Military-Review/English-Edition-Archives/March-April-2022/French/). The interruptions around Veghel and their impact on the congested corridor are described in [The Siegfried Line Campaign, chapter VIII](https://www.ibiblio.org/hyperwar/USA/USA-E-Siegfried/USA-E-Siegfried-8.html). These sources establish useful historical contexts, not the numerical stocks, force allocations, transport timings or victory thresholds for our game. Those parameters may be authored and must be identified as such.

Mortain supports a larger operational breakthrough problem. A Veghel-inspired corridor crisis is the recommended first combat-linked supply scenario because it can exercise a small, legible network. It needs its own dated campaign state. It must not silently change the earlier imported Nijmegen or Arnhem Situations or imply that a later convoy can affect an already completed engagement.

### One crisis across three scales

| Scale | Player decision | Persistent consequence |
|---|---|---|
| Campaign | Redirect reserves, prioritize shipments, hold a convoy or choose an alternate route | Changes allocation, transport bookings and mission readiness |
| Formation | Defend a junction, contain a penetration or escort transport through a threatened area | Changes control of the same routes and crossings |
| Squad | Fight for a roadblock, bridge or village controlling passage | Updates the originating campaign feature, participating forces and cargo |

The central map must show the threatened segment, affected convoy and dependent forces as one connected problem. Selecting any of them highlights the relevant chain and its known consequences. Side panes offer the finite legal responses and preview their effects.

A squad engagement returns changes to the original persistent entities. A destroyed bridge stays destroyed; surviving transport retains its actual cargo; losses are not replenished by leaving the tactical view. Reopening a route restores only the capacity that its physical condition permits. Engagement results must be applied once, with a recorded result identity, so reopening a screen cannot duplicate deliveries or losses.

Use one authoritative campaign clock. The engagement must have an explicit start, elapsed duration and result time. Shipments and other campaign events must be reconciled over that interval under a declared time policy. Tactical resolution must not freeze unrelated logistics indefinitely or allow a convoy to pass through a contested segment before the relevant outcome is known.

### Route availability and enemy threat

| Condition | Meaning and effect |
|---|---|
| Open | Passage is available at the route's normal current capacity |
| Threatened | Known hostile activity prompts a choice to hold, escort, divert or proceed under the supported rules |
| Interdicted | Enemy fire can affect traffic under explicit operational or tactical rules |
| Blocked | Enemy control or a physical obstruction prevents passage |
| Reopened | Passage has been restored, potentially at reduced capacity pending clearance or repairs |

These labels are a player-facing summary, not necessarily a single mutually exclusive state field. Physical blockage, route capacity, enemy control and known threat should remain distinct facts. A reopened road can still be threatened. Record the cause, affected segment, effective time and any recovery condition.

An enemy counter near a road does not automatically block it. Interdiction requires a defined capability and position, with appropriate range, line-of-sight or operational control rules. Show only threats known to the player; an undiscovered enemy must not leak through route highlighting or disabled-action explanations.

A blockage stops or delays replenishment. It does not instantly disable all dependent units. Forward stock and existing issued allowances provide a buffer. Forecast the next unmet mission requirement from those allowances and expected use, and label it an estimate where consumption is uncertain. This creates a visible deadline for reopening the route without inventing a universal out-of-supply penalty.

### Conditional Situation sequence

A corridor crisis can produce several linked Situations:

1. **Contain the penetration:** prevent German forces reaching the supply route.
2. **Protect the convoy:** escort, hold or divert a shipment approaching the threatened junction.
3. **Reopen the corridor:** clear a roadblock if the initial defense fails.
4. **Restore deliveries:** protect clearance or repair work and hold passage long enough to deliver the required cargo.

These are conditional branches, not mandatory stages. Successful containment may avert the convoy crisis entirely. A valid alternate route can avoid a clearance battle at the cost of delay. Contact and engagement-selection rules determine whether tactical combat occurs; a blockage by itself does not generate opposing troops.

Each resulting Situation binds its objectives to stable campaign route, feature, shipment and force identities. Its deployment, support and time limit reflect the current campaign state. Any additional forces must come from an authored reinforcement event or an existing allocation, not appear merely because a new card was created.

Victory conditions should express the supply purpose. German success might require denying a specified passage for a defined duration or destroying a designated shipment. Allied success might require delivering a stated allowance before a mission deadline. Specify the amount, destination, time window and passage conditions on the Situation Card. Destroying every opposing unit is not inherently necessary.

### First combat-linked supply prototype

After the shared spatial and supply foundations are validated, build a Veghel-inspired example with one major road, one threatened junction, a depot, a convoy and forward demand. Provide a defensible passage and, only if supported by the authored map, an alternative route with an explicit cost. Show cargo, route status and downstream readiness on the map.

Use labelled authored forces, timings and allowances where exact historical data would delay a playable prototype. Until a real combat resolver is connected, any scripted or manually selected outcome used to test the connection must remain an explicit test fixture. It must not be presented as combat that the game has simulated.

Essentials may automate routing and allocation while retaining visible consequences. Operational play exposes those decisions. Detailed play can add handling, capacity and interdiction rules. All modes reconcile the same finite cargo and persistent world.

## Implementation sequence and component boundaries

First establish the spatial contract on St. Lo: place its joined boards in campaign space, identify a supply entry, position its forward command post and supply point, and connect the rear depot. Verify that formation and squad views preserve these relationships.

Roll out that spatial contract and the basic shared supply model to every existing Scenario and campaign variant through data rather than independent supply engines. Maintain a per-Scenario checklist covering placement, relevant HQ and depot positions, receiving points, boundary connections, initial allowances and readiness effects. A pilot is not completion of this baseline.

Use Ardennes to validate the advanced shared logistical loop: two competing missions, reusable transport, explicit return availability and finite allocations. Apply advanced features only to Scenarios designed to benefit from them. Follow with the Veghel-inspired corridor crisis; Mortain is a subsequent, larger operational case. Advanced pilots may proceed alongside baseline rollout, but they must not substitute for integrating the remaining Scenarios.

Suggested small JavaScript components:

- `campaign-spatial-bindings.js`: coordinate transforms, footprints and boundary connections.
- `campaign-supply-network.js`: nodes, routes, accessibility and travel-time calculations.
- `campaign-supply-state.js`: inventory, reservations, custody and expenditure validation.
- `campaign-supply-scheduler.js`: transport bookings, movement, handling and return events.
- `campaign-supply-view.js`: map overlays and synchronized selection.
- `campaign-supply-situations.js`: conditional supply objectives, engagement bindings and idempotent result reconciliation.
- Scenario supply data: authored stocks, demands, capacities and effects, separate from the engine.

Names are proposed boundaries, not instructions to duplicate equivalent existing modules. Reuse the campaign clock, event history, identity and terrain facilities where suitable. Campaign owns these concepts in the eventual Blazor architecture; ASL tactical modules consume the engagement conditions and return supported results.

Existing saves require a versioned migration. Preserve completed commitments and deployment positions, and provide an explicit legacy placement when a reliable binding is unavailable. Do not regenerate a campaign or replenish its stocks as a side effect of opening it.

## Acceptance criteria

These baseline criteria apply to every existing and future Scenario, including campaign variants.

- Every Scenario has a validated integrated-map supply configuration; none relies on a separate abstract command map as its final design.
- Basic-only Scenarios remain playable without advanced logistics, with explicit initial supply and automatic policies where appropriate.
- Each Scenario declares its advanced capabilities separately from the player complexity profile.
- An HQ, depot or supply point resolves to the same world location in every view containing it.
- Joined-board orientation and local hex identities remain unchanged after campaign placement.
- Every shipment has a named origin, destination, cargo, assigned transport and traversable connection.
- The formation map identifies the boundary entry for supplies originating outside its footprint.
- Travel and handling times come from the chosen route and transport rules; convoy graphics reflect those times.
- Reservations prevent double allocation of stock and transport. Return travel affects the next feasible dispatch.
- A known route closure changes the affected delivery forecast and mission readiness without inventing resources.
- Receiving at a supply point and issuing to a force are distinct, inspectable transitions.
- Map, command pane, Situation pane and export show the same supply and readiness state.
- Timeline replay is read-only and reconstructs historical positions, custody and commitments.
- Source board data, saved placements, unit counts and squad deployment-radius settings remain intact.
- Simplified play uses the same finite resources through an explicit automatic policy.

The first release need not include historical depot placement, individual ammunition accounting, dynamic combat interdiction or a complete European logistics network. It must make the displayed geography, resource decisions and Situation consequences agree.

## Supply crisis acceptance criteria

These extend the foundation criteria when combat-linked supply Situations are introduced.

- A plausible dated attack creates an opportunity to threaten supply; the breakthrough outcome follows play.
- The same road segment and crossing identities are used at campaign, formation and squad scales.
- Enemy proximity alone does not block a route; the applicable control or interdiction rule is inspectable.
- Route obstruction affects deliveries while preserving supplies already held by forward forces.
- A threatened shipment exposes legal responses and the consequences for dependent missions.
- Successful containment or rerouting can prevent a later Situation from being required.
- Tactical results update bridge condition, force losses, cargo and route capacity exactly once.
- Campaign time and logistics remain consistent with the elapsed engagement interval.
- Supply-oriented victory conditions are measurable and shown before commitment.
- Historical source Situations remain unchanged; authored variants and test outcomes are explicitly identified.

## Related documents

- [Campaign Command Supply Leadership and Optional Complexity Analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>)
- [Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis](<Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis.md>)
- [Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis](<Campaign Atlas Blazor Migration and ASL Terrain Refinement Analysis.md>)
- [LimboDancer Campaign Implementation Plan](<LimboDancer.Campaign Implementation Plan.md>)


## First integrated-board implementation, 11 October 2026

The HTML/JS prototype now uses the original joined Situation boards for the active command supply map, formation deployment and squad deployment. All 17 imported Situations have an authored basic supply binding for both sides. Board orientation, source hex identities, source rosters and deployment restrictions remain unchanged.

The implementation is split into `campaign/supply-spatial.js`, `campaign/supply-state.js`, `campaign/supply-view.js` and `campaign/supply.css`. Existing command models remain responsible for finite stock, scheduled events, mission readiness and commitments. The overlay reads those ledgers; opening a different map does not create another inventory. The basic binding explicitly declares its capabilities independently of player complexity.

Each binding identifies command HQ, forward depot, receiving point, mission HQ and a source-road route. Locations are deterministic authored placements along a transcribed road, not researched historical depot sites. New operations snapshot the binding and derive delivery duration from its length, a provisional 12 km/h convoy speed and five minutes of receiving. Receiving and issuing are displayed separately. Retained packages stay at the depot. German initial allowances do not inherit Allied convoy or reserve state.

The two Ardennes missions share stable command/depot identities in the authored Board C reference frame, despite different card rotations. Their receiving points differ. Existing single-transport scheduling remains authoritative. New operation saves include the board supply manifest; Situation and squad exports carry the binding and command snapshot. Existing saves retain their recorded schedules and placements and are explicitly described as legacy schedules.

Command counters open an information card on the central map. Formation maps provide a supply visibility checkbox and side selector; squad maps provide the side selector. Callout offsets prevent overlapping symbols without moving their geographical anchors. Reinforcement markers use a fixed map position rather than a screen-size-dependent offset.

Reinforcements and supplies remain separate concepts. The current Normandy reserve requires fuel before transfer because that is its authored scenario rule. It must not become a universal requirement. Future reinforcement policies should distinguish assignment, arrival and readiness, including formations arriving with their own initial allowances.

### Verification

- `test_supply_integration.cjs`: all 17 Situations, both sides, coordinate round trips, shared Ardennes facilities, source immutability, saved binding reuse, custody, route timing, legacy replay and exclusive transport scheduling.
- Existing Ardennes command checks, 56 campaign-navigation preparation paths and six Normandy variant/decomposition paths pass.
- Browser sweep: all 17 command maps render their corresponding joined source boards and supply markers without reported console errors.
- Fresh St. Lo browser workflow: allocation at 06:00, dispatch at 06:10, receipt at 06:45, reserve arrival at 06:55, reinforced commitment, both formation setup stages, game map, then 345 squad counters from 78 formations. Formation and squad supply anchors match.
- Previously saved St. Lo deployment with 335 squad counters from 76 formations was opened successfully before the fresh test. The previous operation save is retained.

### Remaining architecture and advanced scope

This is the integrated **Situation-board** foundation. It does not yet geographically register every imported board footprint into one continuous European terrain model. Legacy generated-world fields remain for compatibility; the new board supply manifest is the spatial authority for these active board views. Importing several different boards does not establish geographic continuity between them.

External rear-area networks and footprint boundary connectors still need authored placement and validation. Dynamic bridge closures, combat interdiction, alternative-route capacity, supply-crisis generation and tactical result reconciliation are not implemented by this change. The German baseline currently starts with its authored mission allowance; it is not a second player-controlled dispatch workflow. Normandy and generic source operations retain their one-delivery preparation model, while Ardennes retains its reusable shared transport. These limits must remain explicit before claiming completion of every acceptance criterion above.


## Advanced logistics and continental registration, 11 October 2026

The next HTML/JS increment is implemented. Basic supply remains the default for all 17 Situations. Before issuing allocation orders, players can enable advanced logistics on the central map and choose routine delivery, an authored road blockage, or an authored threat requiring escort.

### Implemented behavior

- The existing command clock and event ledger own logistics. A convoy has two package slots. One finite engineer team repairs a blocked route in 20 campaign minutes; one escort assembles in 10 minutes for a threatened route. These are provisional gameplay settings, not historical performance claims.
- A blocked or unescorted threatened route holds its convoy in place. Undelivered cargo stays assigned to the vehicle. Repair or escort changes the delivery forecast without granting early receipt.
- A held outbound convoy can be recalled. Its cargo returns to depot custody only when the vehicle arrives. Redispatch requires the route and shared transport to be available. Successful deliveries also schedule the vehicle's return. Reinforcement arrival remains a separate event governed by the scenario's fuel rule.
- Connected-road detours use a segment graph and shortest-path search after removing a blocked segment. They include physical backtracking from the stopped vehicle. No cross-country shortcut is fabricated. The current transcribed delivery roads have no verified alternative around the selected blockage, so their reroute buttons remain disabled. A synthetic connected loop exercises the supported detour path in tests.
- History inspection cannot issue logistics orders. Decisions, incidents, revised schedules, custody and returns replay deterministically from the command log.

### Shared continental frame

`campaign/geography.js` registers every imported board footprint in EPSG:3035, the same European projection as the atlas. Conversion is reversible between original joined-board coordinates, continental metres and atlas kilometres. Board rotations and source hex IDs are preserved. Ardennes Situations 14 and 15 use a common Board C reference frame and share depot and command identities and locations.

`campaign/continental-view.js` renders the original detailed board terrain through its registration when a Situation is focused. The atlas shows the same HQ, depot, receiving point, convoy and reserve identities and supply state. The command map's **View on continental map** button opens this view; its HQ markers return to the command operation. Continental supply snapshots carry the operation identity and revision in `campaign/geographic-store.js`. They are a view index, not another stock ledger. The obsolete generated-world footprint overlay has been removed from the atlas overview.

The registration is authored near the named operation. It does not make the game's fictional board settlements surveyed historical locations. Detailed terrain between registered patches, rear-area supply corridors across Europe, boundary connectors and reconciliation of overlapping source boards remain future work. Road and stream features have stable registration-scoped identities; independent source scenarios remain separate operations rather than sharing stock merely because footprints overlap.

### Components and validation

New components: `campaign/logistics.js`, `campaign/logistics-ui.js`, `campaign/route-network.js`, `campaign/geography.js`, `campaign/geographic-store.js`, and `campaign/continental-view.js`. They extend the existing supply adapters and command engines without replacing source cards or formation/squad deployment rules.

`test_advanced_logistics.cjs` checks routine, blockage and escort conditions in Normandy, generic source operations and Ardennes; recall/redispatch custody; deterministic replay; connected detour continuity and return; saved geographic snapshots; all 17 coordinate round trips; shared Ardennes anchors; and projection agreement with existing atlas cells. Existing supply, Normandy, Ardennes and campaign-navigation suites also pass.

Browser verification followed a fresh advanced St. Lo operation: blockage at 06:15, engineer repair at 06:35, delayed delivery at 07:05, reserve arrival at 07:15 and vehicle return at 07:40. Reload preserved the repair order, history remained read-only, and the reinforced mission opened formation setup and the squad map. Atlas and command views showed the same held convoy and cargo custody.

### Explicit remaining limits

This increment implements optional logistics decisions and continental registration. It does not implement combat-driven interdiction, bridge demolition adjudication, per-shot ammunition expenditure, automatic supply-crisis scenario generation, a continent-spanning dispatch network, or tactical combat-result reconciliation. German source allowances remain the basic initial supply model. These pending items are not implied by the advanced toggle.

Final browser verification reached 345 squad counters from 78 reinforced St. Lo formations. Goodwood, both Ardennes registrations and Remagen rendered their registered footprints and supply markers without console errors. Legacy operation publication preserves the original supply timing contract. The focused Situation marker is anchored to the footprint's upper-left corner with a white background at 25% opacity (75% transparency); its Situation chooser remains functional.


## Rear-area transport and intervening terrain, 11 October 2026

All 17 Situations now have an authored rear-area supply corridor. Normandy uses a coastal base, Lorraine and Saar use Reims, northern operations use Brussels, Ardennes uses Liege, and Remagen uses Aachen. These are gameplay assignments, not claims that the named base supplied the historical force on the card date. The corridor terminates at the existing source-road entry and follows that road to the existing forward depot. It preserves the forward depot identity and continental coordinate.

New components are `rear-network.js` (versioned geographic route profiles), `rear-supply.js` (finite custody and events), `rear-view.js` (map and cards), `rear-controller.js` (delegation to existing campaign repositories), and `interstitial-terrain.js` (stable world-grid refinement).

### Player workflow

Before issuing orders, expand Optional advanced logistics and select Include rear-area supply convoy. Existing basic and advanced operations remain compatible. Enabling this option moves the existing mission packages to rear-base custody; it does not create another stock allocation. One rear convoy has capacity for three packages, separate from the local transport. Its authored speed is 20 km/h, with 30 minutes for depot receipt and a five-minute handover before local dispatch. The route and timing profile are saved with the operation.

Orders and staff preparation continue while the rear convoy travels. Progress reports and unloading are scheduled campaign events. Packages stay unavailable through unloading, become depot stock on receipt, then follow the existing local delivery, issue and reinforcement rules. The empty rear vehicle returns to its base. Normandy and generic source operations retain their one-mission package model; this change does not manufacture recurring replenishment or combat consumption.

The View rear supply network link opens the complete corridor on the central atlas. Rear base, entry, forward depot and both transport stages share geographic coordinates with the Situation map. The persistent map toolbar displays campaign time and advances the next event through the original command repository, including revision checks. A command-pane return remains available. Multi-day events display Day +N rather than invalid hours above 23.

### Intervening terrain

The atlas now renders deterministic hex refinement between source footprints. Its versioned continental seed and kilometre grid are shared across operations. Land, lake and forest masks come from the existing atlas polygons; additional woodland uses deterministic coarse vegetation context. Panning, revisiting and changing Situations do not generate a new world. Source-board terrain remains authoritative inside its footprint. Rendering is bounded by viewport and detail level.

This supplies continuous regional terrain visualization and stable sampling, not a newly surveyed tactical landscape. Fine elevations, buildings, road intersections, bridge semantics, adjoining-board conflicts and playable engagements outside imported boards need further refinement. Rear corridors are authored polylines with source-road connectors, not shortest paths through a surveyed European road graph. Combat interdiction on the long-distance leg, recurring requisitions and tactical result reconciliation remain future work. The existing forward-route disruption mechanics remain available.

### Persistence and verification

A full browser reached local-storage quota because previous full squad saves were retained. `save-storage.js` adds reversible compression for owned campaign save keys and migrates existing JSON only when space is needed. Old saves remain readable; unrelated keys and campaign histories are not deleted. Raw JSON exports remain unchanged through the repository adapter.

- `test_rear_supply.cjs`: all 17 corridor endpoints, finite rear cargo, no premature forward dispatch or receipt, final delivery and vehicle return, replay, stable terrain identity and multi-day clocks.
- `test_save_storage.cjs`: compressed Unicode JSON, legacy conversion, previous-save preservation and nested repository adapters.
- Existing advanced logistics, basic supply, Normandy, Ardennes and 56 campaign-navigation preparation paths pass.
- Live St. Lo: 39.1 km rear route; departure 06:10; unloading 08:10; receipt 08:40; local dispatch 08:45; local delivery 09:20. Depot inspection showed cargo still in the rear convoy during unloading, then reserved at the depot after receipt. Atlas advance controls updated the same command state, and the returning rear convoy moved along the corridor.


## Situation-aware supply placement correction (11 October 2026)

The original longest-road/reversed-direction heuristic was invalid: Goodwood's Allied depot could appear inside German deployment. It has been replaced by `campaign/supply-placement.js` and the `board-supply-3` binding.

- Build side-specific hex sets from the source card's per-counter deployment restrictions, with board-only fallback for St. Lo. Apply minimum separation to the Arnhem enclave.
- Project roads and hexes through the same oriented, joined-board frame. Split roads at forbidden or opposing setup areas. Choose a continuous friendly segment on the primary deployment board, with another permitted board as fallback. Never bridge a forbidden gap with a straight line.
- Orient the local corridor from the end farther from opposing setup areas toward the receiving point. HQs must snap to friendly land hexes. An internal road segment is local distribution, not proof of an external rear supply connection or service to every disconnected pocket.
- Keep the shared Bulge/Elsenborn Allied depot and command HQ at identical Board C coordinates despite their different board rotations.
- Arnhem and Bastogne Siege have no assumed open Allied rear-road corridor. New rear-convoy activation is refused for these isolated forces; local stock remains available.
- Formation and squad views check the corridor against deployed opposing units, display it blocked, and identify those units in its information card. This is deployment inspection, not a combat-control or interdiction engine.
- Old saves resolve to corrected map geometry without rewriting their recorded stock, event history, convoy schedule, or troop deployments. Their cards explicitly distinguish corrected placement from retained delivery timing.

Validation: `test_supply_zones.cjs` checks both sides of all 17 Situations against legal setup areas, opposing deployment and an injected enemy obstruction. The integration and advanced-logistics suites cover coordinate consistency, shared facilities, stock conservation and deterministic replay. `supply-review.html` uses the production squad workspace with in-memory full deployments for repeatable UI review, without altering player saves.

Remaining: authored multi-branch networks for disconnected force groups, territorial control and combat interdiction. Do not claim that every allowed deployment hex is automatically supplied by the displayed local corridor.

### Completed browser review

Reviewed each source Situation in the production squad workspace through `supply-review.html`, using temporary automatic deployments and both side selectors. This review does not overwrite player saves or replay every campaign decision workflow. Also reloaded the actual saved Goodwood campaign, opened its formation and squad maps, switched both supply sides, and verified all 530 saved counters were retained.

| Card | Situation | Squad counters | Both supply views |
|---|---|---:|---|
| 4 | St. Lo | 335 | Verified, zero opposing deployment conflicts |
| 5 | Goodwood | 530 | Verified, zero opposing deployment conflicts |
| 6 | Reichswald | 282 | Verified, zero opposing deployment conflicts |
| 7 | Nancy | 279 | Verified, zero opposing deployment conflicts |
| 8 | Marieulles | 194 | Verified, zero opposing deployment conflicts |
| 9 | Nijmegen | 324 | Verified, zero opposing deployment conflicts |
| 10 | Arnhem | 198 | Verified, zero opposing deployment conflicts |
| 11 | Anticlimax | 535 | Verified, zero opposing deployment conflicts |
| 12 | Saar | 450 | Verified, zero opposing deployment conflicts |
| 13 | Fortified Goose Egg | 797 | Verified, zero opposing deployment conflicts |
| 14 | Bulge: Thrust | 319 | Verified, zero opposing deployment conflicts |
| 15 | Elsenborn | 513 | Verified, zero opposing deployment conflicts |
| 16 | Bastogne: Prelude | 219 | Verified, zero opposing deployment conflicts |
| 17 | Celles | 710 | Verified, zero opposing deployment conflicts |
| 18 | Bastogne: Siege | 496 | Verified, zero opposing deployment conflicts |
| 19 | Patton counteroffensive | 871 | Verified, zero opposing deployment conflicts |
| 20 | Remagen | 106 | Verified, zero opposing deployment conflicts |

The Arnhem depot information card explicitly states local stocks only and no established rear corridor. Browser interaction timeouts on large review maps were resolved by checking completed state or reloading the temporary review page; no player saves were cleared.


## Receiving-area deployment rule (12 October 2026)

The previous source-zone test established only that a route avoided opposing setup hexes. It did not establish a friendly frontline, a defended receiving point, or a secure approach. Earlier zero-conflict results must be read with that limitation.

Every Situation now uses a campaign receiving area with a radius of two formation hexes, nominally 500 metres. Formation and squad views use the same source-hex polygons, so the physical area does not change with scale. The squad parent's independent, configurable deployment radius continues to apply.

The original printed setup instructions remain intact. Effective campaign setup is the original allowance plus the receiving area for eligible ground counters. Aircraft, fixed obstacles, bridge equipment and explicitly loaded pairs retain their source restrictions. No opposing setup hex is granted. Full 19-hex areas are preferred; when board boundaries prevent this, the available area is visibly flagged as clipped, rather than represented as a complete margin. The German Fortified Goose Egg entry strip is one such exception. This is an explicit boundary limitation, not permission to occupy Allied deployment territory.

Receiving points are selected from friendly road candidates, favouring a full area with minimal extension of source setup. The local route ends at the selected receiving hex. A short final approach may connect the road to that hex. Route selection is authored deployment planning, not a proof of bridge security or territorial control.

For new automatic setups, up to three existing infantry formations are assigned near SUP before remaining source deployments. No counters are added to the roster. Existing saved positions are never relocated automatically. Players can reposition eligible formations inside the additional area, and their squads remain constrained by the parent radius and effective setup area. Loaded weapons retain their transport relationship.

The map shades the receiving area. Selecting a supply counter explains its radius, any edge clipping, and current deployment coverage. A receiving point without nearby friendly formations is exposed. A route with portions beyond nearby friendly deployment is an exposed approach. Opposing deployed units still cause a blocked indication. These are planning indicators; friendly coverage is not combat control and does not rewrite historical transport events.

Implementation: `campaign/supply-placement.js` chooses the network and receiving area; `campaign/supply-deployment.js` supplies shared effective setup references, initial defenders and coverage assessment; formation and squad validators consume the same references. Binding version is `board-supply-4`. Old event times and player deployments are preserved while displayed geometry is refreshed.

Validation: `test_supply_receiving.cjs` checks all 17 Situations, both sides, full squad decomposition, unchanged source definitions and preservation of existing placements. It also checks that empty friendly deployment reports an exposed receiving point. Existing supply integration tests cover stock, delivery, replay and shared Ardennes facilities. Combat-based interdiction, secured bridges and multi-branch coverage remain separate work.

Browser checks for this increment: Goodwood opened with all 530 squad counters and its 19-hex German receiving footprint; selecting SUP displayed the two-hex rule. After the final candidate-orientation correction, its shorter German route reports friendly deployment coverage (0.6 km); uncovered approaches remain marked exposed. Fortified Goose Egg opened with all 797 counters; its German SUP card explicitly reported the clipped 16-hex board-edge area. These checks supplement the all-17 automated deployment regression, not a repeated all-17 browser sweep.


### Setup relocation of supply markers (2026-10-12)

Formation and squad setup permit moving mission HQ and SUP from their information cards: choose Move, then a destination hex. Game maps display saved positions without offering relocation. These campaign markers can extend beyond source-card troop setup boundaries. Destinations remain on land on the existing network board, clear of opposing deployed units. SUP requires a connected source road (final connector at most one formation-hex spacing) and a two-hex receiving margin clear of opposing troops; edge clipping is recorded.

Overrides live in the Situation plan and apply at both map scales. SUP relocation updates its route, distance and receiving area. Previous receiving-area deployment permissions are retained to preserve existing troop placements. No troops are automatically moved. This is setup editing, not timed logistics movement; existing stock and event ledgers are unchanged. New network generation prefers a farther-forward eligible road position; saved operational bindings remain unchanged until edited. Propagating these setup overrides into the command/continental operational ledger and timed relocation are follow-up integration work.

Verified: all 17 formation/squad deployment checks, logistics regressions, relocation save/reload and invalid-target tests, and live Goodwood formation/squad consistency.


### Portable campaign saves (2026-10-12)

The HTML/JS Campaign menu now provides Save campaign to file and Load campaign from file for source Situations, Normandy, and Ardennes. The versioned JSON envelope contains the complete operation world, including command progress, board supply bindings, formation plans, squad plans, and player supply adjustments. Loading validates the campaign model before writing, preserves unrelated saves, backs up an existing same-identity save, and rolls back failed writes. Browser viewport and map camera settings are not transferred. The Blazor port should preserve this file contract and add explicit migrations when the schema changes.
