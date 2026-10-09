# Ardennes Command Prototype Acceptance

9 October 2026. Implements a bounded smoke test of the [decision contract](<Ardennes Command Decision Contract Specification.md>) using PL14 and PL15. The shared HQ, stock, transport and reserve are authored exercise assumptions. Imported Situation Cards remain unchanged.

## Use the prototype

Open `http://127.0.0.1:8080/ardennes-command.html`, or select **Ardennes command exercise: supply and reserve decisions** in the Atlas Choose panel.

1. Start an Operational exercise with standard staff. Leave isolated test mode unchecked for ordinary planning.
2. Select the crossing defense as main effort, assign its reserve and select its first delivery. Preview the competing readiness times, then commit the allocation.
3. Select the crossing mission and Reinforced defense. Advance scheduled events until 06:50. The commitment button remains disabled until orders, preparation, ammunition, fuel and reserve transfer are complete.
4. Commit the defense and open its Situation setup. The generated package contains 26 Allied counters, including two separately identified reserve M10 counters. Deploy through the existing setup and game-map workflow.
5. Return to the command exercise. The other mission shares the same finite stock and transport timeline. It can receive its ammunition at 07:40 under this allocation.

Essentials exposes the main-effort choice and displays its automatic reserve/delivery policy. Operational exposes the allocation tradeoffs. Staff capacity changes the timetable. Personal notes never execute orders.

## Implemented boundary

The pure state model owns command revisions, idempotent commands, the event clock, finite package locations, reserve commitment, readiness gates and command replay. Browser storage is separate from standalone Situation saves. New exercises archive the previous command state. The UI rejects a stale active session; generated packages verify the source hash and use a new package/save identity. A committed package is the initial deployment snapshot, not a post-combat roster.

Both missions support baseline, supplied and reinforced packages. Baseline withholds one specified gun counter; supplied restores it; reinforced also adds the two persistent R1 counters. Original combat factors are retained. The reserve has an explicit authored Board C setup exception. Source card rules and differences remain visible.

Normal play stops at deployment and map inspection because combat execution is not implemented. Isolated test sessions additionally expose labeled fixed outcomes, route interruption/reopening/recall and temporary command vacancy. A fixed outcome reconciles the test reserve before a report reaches the parent five minutes later. Assessment options depend on that report. These controls never appear in a normal session.

The wider specification remains the target. Explicit acknowledgement choices, order amendment/abort/release workflows, reserve recovery/reallocation, detailed route capacity and leadership personalities are not implemented. Acknowledgement is automatic. The two-mission slice is not a complete campaign engine or historical order of battle.

## Verification

Automated command tests cover opposite priorities, guns-first delivery, retained reserve, readiness, finite-resource conservation, source immutability, replay, stale/duplicate commands, route interruption/recall, succession, delayed reports and assessment gates. All six mission/defense variants are exercised against the real deployment model.

Browser checks through the local web server covered invalid parent choices, locked child commitment, event advancement to readiness, a reinforced package opening in the actual map setup, reserve placement and persistence, return navigation, the isolated failed-defense result, delayed report, disabled continuation after failure, assessment persistence and Essentials hiding advanced choices. No browser console errors were recorded in these checks.

The existing Situation regression suites, schema checks and UI encoding checks supplement these checks. This acceptance record does not claim that the absent combat engine was tested.

## Integrated map workspace update

The player entry is now `http://127.0.0.1:8080/index.html?exercise=ardennes`; the former standalone URL redirects there. The left pane identifies division/regiment headquarters. Map markers show headquarters, supply points, transport, reserve and objectives, with named origins/destinations and package custody. Headquarters and routes are authored diagram positions, not historical georeferencing.

Campaign advancement is on the map toolbar. Scrubbing and playback reconstruct past positions without changing live decisions. Forecast mode shows scheduled consequences and blocks commands; unknown report contents remain hidden. Return to current time restores live controls. Timeline ticks group same-minute events. The map has independent pan/zoom, supply and command overlays, and selection-linked details.

Additional browser validation: selecting the crossing HQ exposed only its decisions; history at 06:05 removed commitment controls; a 06:25 forecast showed the convoy en route while campaign time stayed 06:10; actual 06:40 receipt exposed A14/F1 at the receiving point; 06:50 reserve arrival enabled reinforced commitment; the resulting 26-counter Allied card opened and returned to the integrated workspace. Playback does not advance the live clock. Spatial automated tests include interruption, reopening, recall and report-information gates.

## Campaign ownership and continuous-world slice (9 October 2026)

Implemented in the HTML/JS prototype before the Blazor port:

- A campaign save owns the map seed, generator version, command operation (clock, hierarchy, stock, missions and command history), world nodes, roads, river, persistent roster, deployment history and terrain events. Existing command-only sessions migrate without deleting their originals. Campaign exports and saved-session selection use the campaign envelope.
- The Ardennes campaign has an authored 40 km by 16.8 km footprint anchored in the atlas EPSG:3035 coordinate system. Local coordinates are meters east and south of that origin. This is an illustrative region, not a reconstruction of historical deployment locations or roads.
- A committed M14 or M15 mission binds its supply-adjusted roster to an 8 km by 6 km footprint. Setup uses persistent campaign unit IDs and 250 m hexes. It no longer opens the imported board package as though that package were contiguous campaign geography.
- Campaign (1 km), formation (250 m), and tactical terrain preview (40 m) read the same fixed world features. Seeded woodland geometry is independent of the requested grid. The overview shows the same footprint and features through the atlas transform. Terrain events and deployment history are time-filtered.
- Deployment enforces side-specific west/east halves, one counter per hex, current-time editing and completion only after all counters are placed. These are explicit prototype adaptation rules.

Remaining work: researched/georeferenced terrain and road provenance, full campaign-wide spatial refinement beyond this authored region, adapted mission victory conditions and special rules, force decomposition into ASL Scenario Cards, terrain-compiler admission and combat reconciliation. The 40 m view is a terrain preview, not an admitted ASL scenario. Imported Panzer packages remain reference/standalone material. Changing geography invalidates their board-specific rules until those are deliberately adapted.

Validation: `test_campaign_world.cjs` covers common coordinates, grid overlap identity, deterministic terrain, mission/force binding, deployment restrictions, historical placement, terrain events, persistence and stale-write rejection. Existing command and spatial replay suites also pass. Browser verification covers supply receipt, mission commitment, shared-world setup and persisted placement.
