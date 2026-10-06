# Europe and North Africa campaign map prototype

An offline geographic prototype for the Military Command and Multiscale Simulation Design. Open **index.html** in a browser, keeping `site.css`, `app.js`, `map-data.js` and `image-export.js` beside it. Select hexes, pan/zoom, explore theater workspaces, toggle layers, create a campaign seed, and export campaign data, a selected overview hex, or a PNG of the current map view.

## Implemented
- Natural Earth land, lakes, river lines and selected cities from a pinned upstream commit. Original inputs and SHA-256 checksums are in sources/.
- EPSG:3035 European equal-area projection; pointy hexes with a 60 km radius and approximately 104 km across flats. Axial coordinates and the grid origin are fixed.
- Polygon-intersection land fractions, river intersection lists, city associations, stable hex IDs and geographic GeoJSON export.
- Campaign seed persisted in browser local storage. SHA-256 derives a stable per-hex refinement token from seed, base hash, generator version, hex ID and layer.
- Source geometry remains fixed across seeds. Lower-scale procedural terrain generation is not implemented; fresh seeds currently change refinement tokens only.
- No server, package installation or network requests are needed to use the generated viewer. It loads four sibling asset files. Browser storage is local to the opening origin/path and exports preserve the seed explicitly.
- All six theaters provide Geography, Logistics and Planning workspaces with approximately 26 km sectors.
- Export campaign data produces JSON; Export map image produces PNG.

## Files

| File | Role |
| --- | --- |
| `index.html` | Generated browser entry point; loads the sibling assets below. |
| `site.css` | Viewer stylesheet. |
| `app.js` | Generated application logic. |
| `map-data.js` | Generated map payload, loaded before application logic. |
| `image-export.js` | PNG serialization, rasterization and download. |
| `viewer.html` | HTML source template. |
| `app.template.js` | Application source template. |
| `theater-view.js` | Shared theater workspace code inserted into `app.js` by the builder. |
| `workflow-view.js` | Stage navigation, mission inspector coordination, sector picker and local mission drafts; loaded after `app.js`. |
| `build.py` | Builder and source-hash verification. |
| `prepare_theaters.py` | Compiles all six detailed theater packages. |
| `map.json` | Projected base geography, source manifest and geography hash. |
| `hexes.geojson` | Geographic overview hex polygons and metadata. |
| `theater-workspaces.json` | Generated detailed theater reference packages. |
| `transport-research.html`, `southern-transport-research.html`, `africa-terrain-research.html` | Linked evidence registers; keep these with a distributed viewer to preserve the local links. |
| `preview.py`, `preview.png` | Independent static preview generator and output, not a browser screenshot. |
| `test_map.py`, `test_transport.py`, `test_theaters.py` | Geography, provenance and workspace data checks. |
| `test_viewer.cjs`, `test_image_export.cjs` | Viewer and PNG-export logic checks using test doubles. |

Additional preparation modules and source manifests document the individual terrain and transport layers. Edit the templates and source modules, rather than generated outputs.

## Rebuild
Use Python 3.12 with `requirements.txt` installed in an isolated environment. Node.js is needed for the JavaScript checks.

```text
python -m pip install -r requirements.txt
python build.py
python -m unittest test_map test_transport test_theaters
node test_viewer.cjs
node test_image_export.cjs
```

Optional static preview generation requires Pillow:

```text
python preview.py
```

The build writes `index.html`, `app.js` and `map-data.js`. The authored `site.css` and `image-export.js` remain separate. Rebuilding the forest extraction additionally requires the dependencies described below and its locally retained raster input.

Pin the full environment for cross-platform byte-identical build guarantees; the prototype's same-environment reproducibility is tested. Generated outputs retain source hashes. Dependency or PROJ changes require revalidation, not silent source substitution.

## Scope and limitations
This is contemporary generalized reference geography, not WWII-admitted terrain. Cities use source names. No political boundaries are displayed. Mountain regions, year-2000 tree cover and modern transport are reference overlays, not gameplay-admitted WWII layers. Numeric elevation, historical crossings and dated settlements still need reviewed sources. A land cell is not an assertion of open ground.

The base uses generalized 1:50 million geography, supplemented with 1:10 million reference detail and other documented sources. These sources are too generalized to directly recover ASL terrain or precise bridge positions. River lists indicate geometric intersection, not navigability, crossing rules or a routed logistics graph. Coastlines retain their actual simplified geometry inside mixed hexes.

The map has a bounded Europe and North Africa viewport; cropped geographic features may continue outside it. Full hexes near viewport edges remain identified. The map seed is not combat randomness. No campaign battle-damage system or live ASL integration is implemented.

Source: Natural Earth, public domain. https://www.naturalearthdata.com/about/terms-of-use/
Exact source URLs, commit and hashes: sources/manifest.json.

## Terrain and transport update
- Mountains: Natural Earth physical-label polygons restricted to Range/mtn and Foothills, clipped to dry land. These are approximate region outlines, not elevation bands or movement-cost data.
- Forest: GLC2000 v1.1, European Commission Joint Research Centre. Classes 1-8 tree cover are averaged into a 10 km equal-area grid. Cells with at least 40% tree-class area are retained; patches below 200 square km are omitted and outlines simplified by 2 km. The displayed percentage is the resulting generalized footprint as a share of the hex, not measured canopy density. Tree mosaics and burnt forest classes are excluded.
- Roads: Natural Earth Major Highway features with scalerank <=6. Railways: its multi-track features. Both are contemporary selected reference networks, off by default, with separate toggles. These criteria are display filters, not claims of historical importance, capacity, or connectivity.
- Every new source/derived vector has a checksum in sources/manifest.json. The forest raster archive is retained locally but ignored by Git because of its size; prepare_forest.py reproduces the derived vector. Preparation additionally needs rasterio 1.4.3 and numpy. Source CRS is assigned EPSG:4326 from JRC metadata because the TIFF's embedded CRS is incomplete.
- GLC2000 is a separate JRC source, not Natural Earth public-domain material. Attribution: The Global Land Cover Map for the Year 2000, 2003, GLC2000 database, European Commission Joint Research Centre. See its current terms before redistribution.
- Historical review: ETH RShapes covers 1834-1922 and excludes the British Isles in its analysis; it was not imported as a WWII railway network. The 1940 Mitteleuropa map catalog is a candidate for subsequent digitization, not an admitted vector layer. A WWII transport network remains outstanding.
- Data/logic tests do not constitute browser visual verification. Local file navigation is blocked by the browser tool; preview.png is independently rendered.

References:
https://www.naturalearthdata.com/downloads/50m-physical-vectors/50m-physical-labels/
https://forobs.jrc.ec.europa.eu/glc2000/data
https://forobs.jrc.ec.europa.eu/glc2000/metadata?product=Global
https://forobs.jrc.ec.europa.eu/static/glc2000/legend/GLC2000_Lccs_110604_export.htm
https://icr.ethz.ch/data/rshapes/
https://geodiscovery.uwm.edu/catalog/stanford-vv402mj3929

## September 1939 transport admission
The target baseline is 1939-09-01. No historical segments are admitted yet. The campaign data and GeoJSON use unknown/null coverage instead of zero route length. Modern routes are held separately in research-transport.json and in a collapsible research-only viewer section. Normal campaign export explicitly removes those research features. Closing that section hides its overlays.

sources/transport-1939-review.json records candidate evidence and its limitations. The first review area is France and the Low Countries. The inspected Seine reconstruction uses December 31 status and excludes temporary wartime changes; the Mitteleuropa catalog dates its map to 1940. Neither establishes September 1 coverage. The 1920-1940 NAKALA files have been acquired, inspected and displayed as a research layer, as described below; they are not admitted September 1939 gameplay data.

The historical importer is deliberately not implemented: a nonempty admission file fails the build until segment/date validation is implemented. This prevents manually relabeling modern features as historical. Remaining work includes filling source gaps, georeferencing/digitization where needed, per-segment corroboration, uncertainty and topology review. Modern data is retained solely for comparison, not automatically backdated.

## Regional prewar transport survey

Refresh index.html and use **Country / region**, then **Focus selected region**. The survey includes 55 selected connections across 11 regions, supported by 42 source records. Purple dashed lines show rail connections and orange dashed lines show roads. Selecting an individual connection isolates it, focuses the map, and shows its evidence date and source link. The Full research register link opens transport-research.html, generated from the same provenance records.

The sources/historical-transport-pilot.json registry retains its existing filename for compatibility. It records findings, source sections, retrieval limitations, approximate town coordinates, region groups and coverage gaps. The generated historical-transport-pilot.json uses the map projection. Region names are navigation groups, not 1939 borders or sovereignty claims. Display names generally use familiar modern town names; Leningrad is explicitly historical.

Reviewed 5 October 2026. Most records use railway operators, infrastructure managers, municipal archives or museums; the Lithuanian and Ukrainian records use national encyclopedias. Indexed-excerpt records are identified in the register and need full-page follow-up. Findings are paraphrased; publication dates are not confused with infrastructure dates. Existence-by years include opening dates and documented upgrades, for example the 1926 electrification of Stockholm to Gothenburg. The October 1939 Chiasso electrification milestone is excluded from the September baseline evidence.

These are schematic connections, not digitized historical alignments. Straight lines may cross water, hills or borders where the actual route did not. Shared town markers do not establish shared stations or interoperable tracks. Opening evidence does not prove uninterrupted service or condition on 1 September 1939. Distances, crossings, gauges, capacities and movement costs must not be inferred. No route has yet passed admission into gameplay.

The research overlays stay outside map.json, campaign exports, cell transport summaries and the campaign geography hash. Their own source hash tracks changes independently. Regional gaps are visible in both the viewer and the register. Whole national networks remain incomplete, especially roads. Subsequent work requires period maps and timetables, georeferenced geometry and segment-level date, border, gauge and operational review.

Validation: run the geography tests, test_transport.py and test_viewer.cjs. preview.py --pilot produces a static geographic overview, not a browser rendering test.

The second research pass adds seven connections, including a contemporary 1935 motorway opening report and a 1933 Polish official notice corroborated by PKP. Berlin to Hamburg now carries separate 1933 scheduled-service evidence. Supporting sources and period-map candidate limitations appear in the register. None of this changes the gameplay admission status.

## Historical railway GIS acquisition, 5 October 2026

The research approach now starts with existing historical networks, followed by targeted date and service checks. Individual line-opening histories remain corroboration rather than the primary geometry source.

### Acquired and displayed
Bárbara Polo Martín, *Shapefile of railways in Europe between 1920 and 1940* (2023), NAKALA / MAGNETICS: https://doi.org/10.34847/nkl.6296qx69

The public NAKALA API supplied all six original shapefile/metadata components. They are retained in sources/europe-railways-1920-1940 together with the repository metadata. prepare_historical_rail.py verifies every supplied SHA-1 and records SHA-256 digests. The source CRS is EPSG:3035. There are 14,427 source records; 12,933 with TYPE_1940 equal to ML or SL are displayed in burgundy. The source geometry is transformed to the map's local coordinates and simplified by 0.5 km for display. This does not establish junction topology, bridge positions, track capacity or a mainline hierarchy. Original geometry remains available for subsequent work.

The repository dates the collection to the interval 1920-1940, not September 1939. Fields include OP_YEAR, CL_YEAR, REOP_YEAR, RECL_YEAR, decade classifications, gauge and notes. Audit of all source records found 7,108 opening years recorded as zero and eight opening years after 1939; some decade classifications disagree with opening years. Zero is not a real opening year, and 5000 appears as a closure sentinel. Do not apply naive date filtering. The map uses the explicit 1940 classification filter as a period research view, not as proof of prewar operation. Source code semantics and conflicts remain to be reconciled.

Attribution: Bárbara Polo Martín. License: Creative Commons Attribution-NonCommercial 4.0 (CC-BY-NC-4.0), https://creativecommons.org/licenses/by-nc/4.0/. Display conversion and simplification are our modifications. This research layer is excluded from campaign exports, campaign hashes and gameplay admission. Commercial inclusion would require appropriate permission or replacement sources.

A complete exact-date 1939 network is still outstanding.

### Systematic source chain
- NAKALA dataset and its related study: https://www.techscience.com/RIG/v35n1/65922/html . The study documents a wider historical transportation GIS, with regional variation in precision and completeness.
- Morillas-Torne (2012), *Creation of a Geo-Spatial Database to Analyse Railways in Europe (1830-2010)*: https://file.scirp.org/pdf/JGIS20120200013_30315294.pdf . Its methods identify historical atlases, library maps and company records; use it to trace the provenance and limitations of inherited fields. Its basic passenger-network scope omits some freight and narrow-gauge lines, a relevant military-logistics gap. This is a related methodology, not proof of the exact lineage of every downloaded record.
- ETH RShapes: https://icr.ethz.ch/data/rshapes/ . Construction history and digitized geometry through 1922 offer an earlier comparison network; not a 1939 snapshot. British Isles are excluded from the analysis. CC-BY-NC-SA-4.0. Inspected documentation, not imported.
- Period timetables and national railway maps remain the evidence for late openings, closures, freight branches and cross-border service. Existing regional evidence and the 1939 Poland map lead remain in transport-research.html.

### Next reconciliation pass
Compare the 1930 and 1940 classifications to isolate changed segments, rather than re-research every established route. Resolve conflicts against period maps/timetables; explicitly handle changes during 1939. Audit geographic gaps, particularly eastern Europe, against an independent network. Then separate strategic main lines from local branches and validate junction connectivity. Retain a source/date/confidence record per admitted segment. The atlas is not sufficiently detailed to determine ASL-scale crossings.


## Railway display hierarchy
Railways use burgundy, while waterways retain blue. The Europe view (viewport width over 2,400 km) shows a provisional corridor backbone. Regional views reveal additional through routes; at 900 km width or less all mapped segments appear. Zooming out reverses the reveal. The railway toggle remains authoritative at every zoom.

Research: Morillas-Torne (2012), pages 179-180, describes a broad main-line/passenger-network scope, not a strategic trunk ranking. The acquired TYPE_1940 field puts 12,302 records in ML and 631 in SL. Treating ML alone as the Europe layer would remove very little clutter, and the exact code meanings have not been independently confirmed. We therefore preserve these fields and add a separate displayTier, explicitly not a historical traffic or capacity classification.

rail_display.py constructs a deterministic display approximation from source vertices quantized into 100 m bins. A representative well-connected vertex per 250 km tile is a display hub. Shortest source-network paths to up to four nearby hubs within 800 km form the top layer. Terminal branches up to 60 km are peeled for the intermediate layer; all remaining features appear at close zoom. Geometry is never replaced with straight hub-to-hub lines. The output records algorithm settings and tier counts. Binning may miss actual joins or merge nearby endpoints; disconnected components and isolated local lines may be absent from the top view. This display graph must not be used for simulation routing.

The next historical refinement is to replace inferred hub importance with reviewed period trunk routes, principal junctions, ports and timetable service classes. Line length, gauge and the ML field alone do not establish strategic importance. Existing historical dates and admission status are unchanged.


## Continental and theater navigation

The **Game controls** area contains Explore theaters and the Theater workspace, which appears when a theater is active. Selecting a theater enters it immediately; selecting **Whole Map** returns to the overview. There are no separate enter/return buttons. All other sidebar controls are grouped under **Map controls** in closed-by-default disclosures, including layers, transport research, selected hex, campaign exports and sources. Pan/zoom buttons are in their own disclosure; drag and scroll remain available directly on the map. **Fit map** fits the current view without changing the theater selection. Theater outlines can still be enabled in Map layers and selected by mouse or keyboard.

The six presets are Western Europe, Eastern Europe, Mediterranean, Northern Europe, Northwest Africa and Libya-Egypt. They are illustrative campaign areas, not historical command boundaries, and may overlap. Definitions share the map's local EPSG:3035 coordinates. Command and objective assignments remain unassigned. Entering a theater preserves the campaign seed and overview selection, and reveals at least the regional railway tier.

Every theater uses the shared workspace described below. The continental grid is approximately 104 km across flats; theater sectors are approximately 26 km. Both use fixed origins and coordinates. This does not imply that coarse hexes subdivide exactly into the finer hexes.

The gray focus veil and perimeter follow the union of the active theater's sector footprints. They update with pan/zoom, ignore pointer events, remain independent of the grid toggle and disappear on return to the overview. Only the active theater's finer grid is shown. The footprint is a presentation boundary, not a movement restriction.

### Western logistics provenance

Western Europe retains 12 original approximate port, transfer and depot markers, supplemented with transportation references by the shared workspace builder. The original examples come from Roland G. Ruppenthal's U.S. Army official history, *Logistical Support of the Armies*, Volume II, chapters V and VI, and describe 1944-1945 conditions. They are not backdated to September 1939:

- https://www.ibiblio.org/hyperwar/USA/USA-E-Logistics2/USA-E-Logistics2-5.html
- https://www.ibiblio.org/hyperwar/USA/USA-E-Logistics2/USA-E-Logistics2-6.html

`prepare_western.py` retains this legacy reference package and its source verification. The displayed workspace for every theater is compiled by `prepare_theaters.py`, using `sources/theater-manifest.json` for the expanded detailed geography. Logistics markers are dated evidence, not live facilities or available capacity.

## North African map extension
The continental overview now covers Europe and North Africa through Egypt: local bounds (-2400, -2500, 2800, 3300) km. Hex radius, origin, projection and IDs remain unchanged. The southward extension adds cells; it does not move existing ones. Generator 1.4.0 and the geography hash identify the current baseline. Browser campaign seeds are retained, but refinement tokens include the revised geography hash and therefore change. Existing exported campaigns retain their old baseline; this prototype has no automatic save migration.

Two additional illustrative campaign areas are available: Northwest Africa (Morocco, Algeria and Tunisia) and Libya–Egypt. They use sampled geographic perimeter edges projected into the common coordinate system. Both have the same detailed 26 km workspace capabilities as the four European theaters. Historical motivation: U.S. Army Center of Military History, Northwest Africa and Egypt-Libya campaign studies:
- https://history.army.mil/Publications/Publications-Catalog/Northwest-Africa/
- https://history.army.mil/portals/143/Images/Publications/catalog/72-13.pdf

Additional city-center reference points come from the pinned Natural Earth 1:10m populated places source, with reviewed display aliases for Benghazi, Tobruk, Port Said and Mersa Matruh. These are not historical city extents. The Nile and African coastline use the existing global reference sources. The historical European railway dataset is not extended by inference into North Africa. Sparse rail coverage remains unknown. Forest reference coverage spans the expanded map. The southern terrain and transport research described below improves the regional coverage without claiming a fully reconstructed WWII operational map.

### Southern terrain review, October 2026

The southern coverage now includes finer Natural Earth mountain regions, 18 river features, Great Bitter Lake and nine salt-basin features. Lake Nasser is removed because the Aswan High Dam and its reservoir postdate WWII; the older Low Dam reservoir is not reconstructed. Salt basins have separate tan dashed styling and do not contribute to open-water fractions. Regional historical context appears in intersecting hexes and in `africa-terrain-research.html`. Discussion boxes are evidence locators, not terrain geometry.

GLC2000 forest extraction now covers the expanded map, superseding the earlier southern coverage limitation. This remains year-2000 tree cover, not verified wartime woodland. The official campaign histories support distinctions between Atlas corridors, chotts, wadis, rocky desert and scrub. Precise period forest boundaries, water seasonality, Qattara and escarpment geometry, crossings and terrain passability still need reconstruction. Source geometry is pinned and checksummed in the manifest.


### Southern transportation survey

The separate `southern-transport.json` research overlay supplies 28 dated schematic rail, road and track connections across Northwest Africa and Libya-Egypt. Major corridors display at overview scale, branches at regional scale and local lines/tracks at close zoom. Burgundy rail is distinct from blue waterways; roads are orange. The default subset uses evidence dated by 1939. An explicit control enables 1940-1942 evidence, including campaign roads and the Benghazi local railway descriptions. No wartime evidence is silently backdated.

Sources include the contemporary 1935 Railway Wonders survey, SNCFT/ONCF histories, British and US official histories, Wavell's dispatch and the Milan archive catalog of the 1937 road inauguration. `southern-transport-research.html` lists every connection and source. Coordinates are approximate town waypoints, not digitized railway or road centerlines. Presentation tiers are editorial, not measured historical capacity. Gauge changes, bridges, daily service and wartime damage remain unmodeled. Tripolitanian railways, Egyptian branches and Suez connections remain gaps. This does not yet match the geometric detail of the acquired European railway GIS. Research is excluded from campaign and hex exports.


### Shared theater workspace audit

All six theaters use one workspace implementation: Geography, Logistics and Planning modes; 26 km land sectors on the same lattice; detailed settlements and waterways; footprint shading; source-linked logistics references; and campaign-local notes. Packages are built from the same pinned Natural Earth inputs. Lakes and salt basins remain visible in theater views, fixing their previous disappearance in Western Europe. Layer toggles apply consistently.

The workspace title, selection and note entry reset when changing theater. Notes carry theater identity, are filtered to the active theater and all theaters' notes are exported together. Existing Western notes are read from their original storage key and migrate on the next save. Shared coordinate IDs identify matching sectors across overlapping theaters, while theater-specific IDs preserve planning scope.

Logistics combines Western's dated port/depot examples with source-linked transportation reference points in every theater. These are not equivalent to verified depots, facilities or operational capacities. Geographic and source coverage limitations remain explicit. The generated theater packages are excluded from gameplay exports; saved planning notes remain included.


### UI assets

`viewer.html` contains the HTML template. `site.css` holds the stylesheet. `app.template.js` contains application logic; the build inserts `theater-view.js` into it to produce `app.js`. `index.html` loads these sibling CSS and JavaScript files directly, including when opened locally. Keep them together when copying the viewer. `map-data.js` contains the generated map payload and loads before `app.js`. A classic script allows direct local-file viewing without a server or JSON fetch. Keep `index.html`, `site.css`, `app.js`, `map-data.js` and `image-export.js` together, plus the linked research pages. Edit source templates, not generated `index.html`, `app.js` or `map-data.js`.


### Image export

Export campaign data saves JSON. Export map image saves the current SVG map viewport as a PNG, preserving zoom, theater shading, selected sectors and visible layers. The sidebar and HTML toolbar are excluded. The image uses an opaque water-colored background and up to 2x display resolution, capped at 4096 pixels on the longest side. `image-export.js` supplies serialization and rasterization; keep it alongside the other viewer assets. Image export does not establish additional reuse rights for the research layers.


### Interface refinements

Explore theaters remains visible above the collapsible map controls. Transport research and source caveats live in expandable groups. Status feedback uses olive for success, rust for errors and muted text for progress. Sector IDs and dated site evidence use monospace. Narrow layouts place the title below the toolbar and let mode buttons wrap. PNG exports can optionally include a caption containing the view title, reference dates and attribution; the default remains a plain map. The caption is outside the geographic viewport, and the complete image remains capped at 4096 pixels.


### Regional campaign command prototype

Choose Western Europe, expand Regional campaigns under Game controls, then choose Normandy: Omaha beachhead. The regional workspace uses a continuous 13 km land-sector lattice, the same reference geography, and approximate objective markers. Whole theater returns to the 26 km theater workspace. Only Normandy has a regional command exercise so far.

The dated reference is 7-8 June 1944, grounded in the US Army's Omaha Beachhead account. The partial hierarchy includes First Army, V Corps, and the 1st and 29th Infantry Divisions. The 1st Division branch now continues through the 18th Infantry Regiment and its three organic battalions. It is not a complete dated order of battle. Strength, readiness, supply, enemy knowledge and unit locations are unknown. Mission wording is authored for this exercise, not quoted historical orders. Modern geography and the existing transport reference layers do not establish a reconstructed 1944 operational network.

Select First Army and assign the beachhead mission. Advance communications, select V Corps, enter a plan and save it, then begin execution. V Corps can assign the two subordinate division missions after planning its parent mission. Each division receives its order on a later communication step, records a plan, begins execution and submits a written report. The 1st Division must delegate to the 18th Infantry, which delegates three battalion missions. Each parent must receive and assess all subordinate reports before reporting upward. V Corps ultimately reports to First Army. A communication step demonstrates delivery delay, not elapsed campaign minutes. No combat, movement or outcome is generated by these buttons.

Exercise state is saved locally under the campaign seed, reference-package hash and geography hash. Starting a new campaign creates independent state. Planning JSON export is explicitly non-executable; campaign JSON includes saved exercise state but excludes the regional reference geometry. Headquarters see incoming messages only after delivery. Switching headquarters is an inspection tool, not player access control.

ASL admission remains a checklist, not a launch button. Company and platoon organizations, actual tactical rosters, validated terrain, a complete Scenario Card and engagement/reconciliation contracts must be implemented before tactical execution.

`regional-state.js` is a required sibling runtime file loaded before `app.js`. `regional-view.js` is injected alongside `theater-view.js` by the builder. `prepare_regional.py` produces `regional-campaign.json` from `sources/regional-campaign.json` and the shared geography. The generated package is included in `map-data.js`. Run `test_regional_state.cjs` and `test_viewer.cjs` with Node to check mission state and navigation. These are logic tests, not browser rendering verification.


#### Regiment and battalion extension

The 18th Infantry branch follows the organic hierarchy documented in the official Omaha account for 7-8 June. Its three battalion exercises distinguish the Engranville approaches, the eastern Aure crossing route toward Mosles, and the Mandeville flank. These are authored planning prompts, not transcribed orders or a historical replay. Named areas, timing constraints and support requests are shown with each mission. Exact boundaries, routes and force positions are not inferred from the 13 km sectors. The temporary 3/26 attachment and historical armor/artillery support are not allocated as playable forces.

The tree renders arbitrary depth. Division, regiment and battalion share the regional map. Parent plans gate subordinate assignment, and assessed subordinate reports gate upward reporting at every level. Complete the 29th Division branch as before; the 1st Division branch now requires the regiment and three battalions. Companies and platoons are the next missing echelons before ASL Scenario Card admission.

Reference version 2 uses a new package hash, so this expanded exercise starts fresh for an existing campaign seed. Earlier exercise records remain in local storage under their original hash; no automatic migration or historical force allocation is performed.


### Formation Situation Card and ASL handoff drafts

At a battalion headquarters in the Normandy exercise, receive and plan its mission, select a regional sector, and choose **Create Situation Card**. The panel records the parent mission, date, geographic anchor, intent, success criteria, authored 120-minute planning window, support limitations and roster provenance. **Prepare two engagement drafts** divides the illustrative company into an access engagement, a conditional objective engagement and a reserve. The second engagement depends on the first; this is an authored decomposition example, not automatic contact detection.

The illustrative roster has three platoons with three squad placeholders each. Those nine identities persist in saved regional state; six are held for the two drafts and three remain reserved. They are not reconstructed wartime strengths, catalog units or playable counters. Leaders, crews, vehicles, weapons and opposing forces are not invented. No formation movement, combat, tactical terrain generation or result reconciliation is implemented by this panel.

Each export uses a `formation-asl-handoff-draft/1` envelope. Its nested `scenarioCard` follows the field names of the existing `ScenarioCard` record, but required fields remain unresolved. The envelope holds campaign/situation lineage, temporal and geographical scope, individual asset bindings, admission blockers and reconciliation requirements. It is explicitly non-executable and must not be submitted as a complete ASL card. Actual admission requires `ScenarioCards.Parse/Validate`, a registered unit catalog, validated map/setup data and enforceable victory conditions. Parent victory remains unassessed; it is not a count of child wins.

Keep the new `situation-state.js` and `situation-view.js` beside the other offline viewer assets. They load before `app.js`. `test_situation.cjs` checks identities, reserve protection, overlap rejection, time bounds, exports and persistence. Viewer tests cover creation, decomposition, export and reload. The build's encoding check includes both scripts.


### Guided headquarters navigation

Opening Normandy starts at First Army, including when an exercise has saved progress. Only that headquarters' mission controls are shown. Assign the Army order and advance communications; V Corps then becomes available as its direct child. At each receiving headquarters, save the received mission's plan to reveal subordinate orders. Assign and deliver an order to enable that child, then select the child to continue. Siblings and deeper descendants are not shown in the child's workspace. The command path provides return navigation for other branches and upward reporting.

Only currently available actions and required text entry are visible. The Next prompt explains the current planning/delivery step. Completion here means readiness to delegate, not the final report and assessment of the entire mission, which would prevent downward progression. A Situation Card appears only at the receiving battalion. Existing saved exercise data is preserved; the navigation does not reset mission state or auto-issue orders.


The **Clear** button at the top of the Normandy workflow deletes this campaign's saved regional exercise, including older reference-package versions, mission plans, messages, events, Situation Cards and engagement drafts. It also clears unsaved workflow text and sector selection, returning to First Army. It keeps the seed, map, theater planning notes and other campaigns. Clearing does not delete previously downloaded exports. No confirmation dialog is required; storage failures are reported in the workflow status.


### Next phase: catalog-backed tactical test

After preparing the two engagement drafts, choose **Prepare catalog-backed tactical test**. Existing squad IDs are preserved and mapped to `american-squad`; each platoon receives one explicitly authored `american-leader-8-0`. The first engagement adds two `attacker-squad` German squads and an `attacker-leader-8-0` as authored test opposition. The second platoon and the third-platoon reserve remain distinct. These are actual catalog definitions, but the organization and strengths remain an authored exercise, not historical evidence.

The first engagement can now export a complete **ASL test Scenario Card** and a separate **test identity manifest**. The card uses six turns, explicit setup bands, sides, ELR/SAN, friendly edges and an enforceable exit-VP condition on representative board 04. This board is not asserted to fit the selected regional location. The manifest binds exact exported card bytes with SHA-256 and retains the situation revision, campaign identity, catalog hash and per-counter asset identities. Keep both exports together. This is a tactical rehearsal only: campaign admission and reconciliation are disabled, and the second engagement remains a draft.

The card template passes the actual C# `ScenarioCards.Parse`, user-card `Save`/`Read` and `ScenarioCards.Start` path in `FormationHandoffTests`, including rejection of an unknown unit definition. This validates the card and start request; it does not prove full gameplay or certify a generated map. ASL Studio reads user cards from its configured boards-root `cards` folder (normally `src/ASL/boards/cards`); copy the exported `.scenario-card.json` there to make it available to Studio's card list, then validate its board/setup in Studio before starting a separate test game. The atlas has no live Studio connection and does not launch a game. Downloading the identity manifest does not automatically bind Studio's placed counters to campaign assets.

`prepare_tactical.py` regenerates `tactical-reference.js` from the card source and existing catalog during the map build. Keep `tactical-reference.js` and `tactical-handoff.js` with the offline viewer. Existing Situation Cards can opt into the new test without clearing their mission progress. Clear removes the prepared test along with the rest of the workflow.


### Formation Situation map

At a battalion's Situation Card, optionally prepare the catalog-backed tactical test first to include a German opposing counter, then choose **Open Situation**. A dedicated formation display replaces the regional map and controls. It has 99 hexes, 250 m across flats, on a globally anchored axial grid near the selected regional sector center. Terrain is generated from campaign seed, generator version and global hex identity, making overlapping cells deterministic. The terrain and objectives are illustrative; no claim of historical Normandy geometry or ASL terrain compatibility is made.

Select a platoon counter. During **Command and Rally**, toggle Hold/Maneuver as needed. Complete Command and Preparatory Fire to reach **Movement**, then click adjacent empty hexes. Each counter has four prototype movement points per player turn; woods cost two, other terrain one. One formation per hex is a prototype restriction. Units, budgets, orders, phases, locations and contacts persist in the Situation Card. Each counter lists its constituent asset IDs. The third platoon remains the tactical-allocation reserve, although its formation position can be maneuvered. Deployment positions are authored initial placements, not historical observations.

Both sides are controlled manually, with eight ASL-inspired phases per player turn. The non-movement combat phases record explicit passes; fire, recovery, rout, advance and close combat are not adjudicated. A complete American/German cycle increments the turn and advances simulation time by the design's nominal six minutes. This is a rules convention, not a calibrated historical action duration. Adjacent enemy counters record contact and freeze movement/phase advancement. Contact does not instantiate a battle or bypass ASL admission. Objective occupancy alone does not award victory. Automatic contact-to-card selection and tactical reconciliation remain future work.

Use **Return to regional command** to inspect or export the Situation Card. Reopen it to resume the map. Clear deletes this state with the rest of the exercise. Preparing a different tactical roster after opening a map is blocked to avoid stale counter identities. `formation-state.js` and `formation-view.js` are required offline assets. `test_formation.cjs` checks deterministic geography, movement budgets, turn transitions, contact blocking and identity validation. Viewer tests cover opening, persistence and return navigation; browser rendering was not verified by automation.


### Dated campaign packages (reference v3)

The theater is geography; a dated campaign package establishes the play period. Western Europe offers **Omaha beachhead expansion, 8 June 1944**. The package has an opening instant, exclusive end instant, historical reference period, purpose, source IDs and explicit deployment/geography/schedule limitations. Other theaters show that no dated package is available yet. They remain browsable, without inheriting Normandy dates or forces.

The Normandy prototype opens at 00:00 on 8 June and ends at 00:00 on 9 June, normalized to UTC. These instants are authored scenario boundaries, not historical operation timestamps. The official source describes 7-8 June; later narrative is context, not intelligence automatically revealed at the opening. Command structure remains partial and forces/terrain authored or approximate. Reinforcements, withdrawals, daylight, weather and historically verified opening deployments are not implemented. Transport overlays keep their own evidence dates and do not become a validated 1944 network merely by selecting this campaign.

Regional state stores campaign opening/current/end times. Communication demonstration steps leave simulation time unchanged and record timestamps. Situation Cards reserve an absolute two-hour interval starting at current campaign time and are refused if it cannot fit before campaign end. Draft engagement intervals are translated from parent-relative minutes to absolute times. Each completed two-sided formation turn advances both the situation clock and campaign clock six minutes; individual phases and the first side's player turn do not advance them separately. The situation stops at its deadline; contacts still stop progression earlier. The six-minute increment is a design convention, not an ASL-turn conversion.

Only serial situation progression is supported. If another situation advances the campaign clock, a stale formation cannot act; concurrent scheduling and waiting/rebasing require a later implementation. Advancing the clock does not apply a battle result, move reinforcements or award parent victory. The representative ASL rehearsal remains outside campaign execution and cannot return elapsed time or outcomes.

Reference v3 has a new package hash. Existing v2 exercises remain stored under their old hash; the dated package starts fresh rather than silently assigning historical time to old progress. The **Clear** button removes all saved regional versions for the current seed. `test_campaign_time.cjs` verifies the clock boundaries and separation of communication steps.


### Dated campaign division counters

Choose Western Europe, then select the dated Normandy campaign. Only then do the 1st Infantry Division, 29th Infantry Division and V Corps headquarters counters appear on the regional campaign map. The undated theater and continental maps have no campaign counters. Counters are also hidden outside the package's opening/end window and while the formation map is open. This package has no individual reinforcement or withdrawal schedule yet.

The counters use authored callouts anchored to regional cells nearest the existing objective references. Dashed leaders distinguish these from verified deployments; badge offsets are for legibility, not physical dispersion. The regional grid is approximately 13 km across flats, equivalent in area to 2,700 formation hexes. Neither area nor hex occupancy determines a unit's composition. The 1st Division has a partial 18th Infantry branch; the 29th's subordinate organization is not populated.

Click a badge or sidebar entry to inspect the dated organization. **Focus on Normandy counters** fits the regional campaign area. **Show command workflow** opens the existing workflow without resetting headquarters or progress. Selecting a counter does not issue orders or bypass mission prerequisites. `theater-counters.js` remains the implementation filename, but visibility, anchors and labels now belong to the selected dated campaign. Tests cover undated-map exclusion, regional anchors, campaign expiration and return navigation.


## Historical division infantry prototype (reference v4)

The Omaha package opens on 8 June 1944 after an Allied beachhead has been established. Amphibious assault is outside simulation scope. Beaches are abstract supply and reinforcement entry points; no arrival schedule or unloading model is implemented.

The 1st Infantry Division now contains the 16th, 18th and 26th Infantry; the 29th contains the 115th, 116th and 175th. Each regiment has three persistent infantry battalion identities. This infantry core is not a full divisional order of battle. Personnel, equipment availability, supporting arms, supply and precise deployment geometry remain unverified.

Division counters show organic composition, approximate opening-area reports and subordinate order status known to that headquarters. Select Western Europe, then the dated Omaha campaign, then a division counter or its list button. The guided First Army / V Corps / division workflow now supplies missions for every infantry regiment and battalion. The new battalion paths reach the existing illustrative Situation Card prototype; their squad rosters do not become historical merely because the parent battalion is real.

Orders remain authored player plans. Temporary historical attachments are displayed separately from organic membership and are not executable command transfers or duplicate force allocations. Historical later outcomes are not forced. Counter anchors remain illustrative callouts, not headquarters locations or occupied hexes. There is no division movement or combat resolver in this increment.

Evidence: [U.S. Army, Omaha Beachhead, 7-8 June](https://www.ibiblio.org/hyperwar/USA/USA-A-Omaha/USA-A-Omaha-6.html) supplies dated area context; [Army ETO order of battle, 29th Division](https://history.army.mil/documents/ETO-OB/29ID-ETO.htm) corroborates its organic regiments. June 7 actions inform starting conditions rather than becoming scheduled June 8 attacks.

Version 4 changes the reference hash, creating a fresh exercise. Earlier saves remain under their original keys. Validation: `test_divisions.cjs` checks roster identity, all new mission branches, delayed headquarters knowledge, persistence and post-landing scope; existing lifecycle and viewer checks cover navigation and handoff behavior.


### Campaign action navigation

Clear campaign data and Open Situation are grouped under Saved Situations and campaign data in the left panel. Clear is enabled for an active dated campaign, including while its Formation map is open. Open Situation uses the saved Situation selector and works from any headquarters once a card exists; a card without an established setup opens Prepare first. With no card, the adjacent message explains the battalion planning prerequisites. Dated campaigns is nested inside Theater workspace, which remains available in the regional view. Campaign formation counters is nested within Dated campaigns, below the campaign selector and briefing. Clearing preserves the map seed and returns to First Army. This UI change does not alter the campaign reference version or migrate older saves.


### Guided exercise workspace

The stage bar sits below the campaign header, above the map. Choose opens the task selector; Brief explains Normandy; Command preserves the existing authority and delivery rules; Prepare is available for a planned battalion mission; Maneuver opens a prepared or existing formation; Review presents communications and exports. Stages reflect the selected mission, not overall campaign completion. Revisiting a stage does not issue orders or advance time.

Command ancestry, subordinate navigation and mission selection live in the left panel. The active mission, preparation controls and formation controls live in the right inspector. At narrower widths the panels reflow around the map. Saved Situations and campaign data are grouped in a disclosure in the left panel.

Open Normandy exercise resumes the current reference-package record without resetting it. First-time Situation opening requires an explicit maneuver-only choice or preparation of the catalog-backed tactical test. Saved Situations without a chosen setup return to Prepare. After formation creation, the action reads Resume Situation. Existing formation state and roster restrictions are unchanged.

Mission text is retained per campaign, headquarters, mission and action in browser storage, separately from submitted plans/reports. Successful submission removes the corresponding draft; communication steps retain it. Clearing campaign data also clears that campaign seed's drafts. If storage is unavailable, typed drafts remain in memory for this page session and the inspector explains the limitation. The regional sector dropdown provides a keyboard alternative to map clicks.

Keep `workflow-view.js` with the other runtime assets when distributing the viewer. `test_viewer.cjs` covers stage gating, mission authority, draft navigation/reload, keyboard sector selection, both formation setup routes, and return navigation using a DOM double. It does not verify rendered layout, screen-reader behavior or touch interaction.
