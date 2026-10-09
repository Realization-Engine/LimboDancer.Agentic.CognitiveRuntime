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
BÃƒÆ’Ã‚Â¡rbara Polo MartÃƒÆ’Ã‚Â­n, *Shapefile of railways in Europe between 1920 and 1940* (2023), NAKALA / MAGNETICS: https://doi.org/10.34847/nkl.6296qx69

The public NAKALA API supplied all six original shapefile/metadata components. They are retained in sources/europe-railways-1920-1940 together with the repository metadata. prepare_historical_rail.py verifies every supplied SHA-1 and records SHA-256 digests. The source CRS is EPSG:3035. There are 14,427 source records; 12,933 with TYPE_1940 equal to ML or SL are displayed in burgundy. The source geometry is transformed to the map's local coordinates and simplified by 0.5 km for display. This does not establish junction topology, bridge positions, track capacity or a mainline hierarchy. Original geometry remains available for subsequent work.

The repository dates the collection to the interval 1920-1940, not September 1939. Fields include OP_YEAR, CL_YEAR, REOP_YEAR, RECL_YEAR, decade classifications, gauge and notes. Audit of all source records found 7,108 opening years recorded as zero and eight opening years after 1939; some decade classifications disagree with opening years. Zero is not a real opening year, and 5000 appears as a closure sentinel. Do not apply naive date filtering. The map uses the explicit 1940 classification filter as a period research view, not as proof of prewar operation. Source code semantics and conflicts remain to be reconciled.

Attribution: BÃƒÆ’Ã‚Â¡rbara Polo MartÃƒÆ’Ã‚Â­n. License: Creative Commons Attribution-NonCommercial 4.0 (CC-BY-NC-4.0), https://creativecommons.org/licenses/by-nc/4.0/. Display conversion and simplification are our modifications. This research layer is excluded from campaign exports, campaign hashes and gameplay admission. Commercial inclusion would require appropriate permission or replacement sources.

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

Two additional illustrative campaign areas are available: Northwest Africa (Morocco, Algeria and Tunisia) and LibyaÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“Egypt. They use sampled geographic perimeter edges projected into the common coordinate system. Both have the same detailed 26 km workspace capabilities as the four European theaters. Historical motivation: U.S. Army Center of Military History, Northwest Africa and Egypt-Libya campaign studies:
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


### Campaign Situation Card and ASL handoff drafts

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


### Campaign Situation map

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


## Panzer Leader Situation 4: St. Lo
Open **St. Lo Situation 4** in Game controls or Choose your task. This is a separate, locally saved June 29 package, not the June 8 Omaha exercise. Review the original card, then place the German counters on board A and complete German setup. Place the Allied counters on C and complete Allied setup. Expand a counter type to select individual counters; click the board to place or reposition them. Clear this Situation resets only this package. Export Situation plan preserves the full roster and placements.

The package includes 21 verified counter types, 76 individual counter identities, Imaginative Strategist artwork, source-board images, source hashes, the original card, setup order, 15-turn limit and victory alternatives. Boards are displayed separately in source-sheet orientation. Planning positions now carry hex identities and normalized image centers. Terrain remains a first-pass interpretation rather than an admitted combat map. No combat or victory adjudication is implemented. Infantry artwork range asterisks remain source annotations pending interpretation.

After setup, select participants from both sides, enter a local objective and create an ASL engagement draft. Counter identities are held against duplicate draft allocation. These blocked drafts require dated squad/crew/vehicle decomposition, contact and time boundaries, compiled terrain, completed Scenario Cards and engine admission. A Panzer counter is not automatically one ASL squad. There is no campaign reconciliation yet.

Files: `panzer-situation-data.js` initializes the offline library; `situation-data/` contains its generated components; `panzer-situation-state.js` owns validated setup and engagement draft state; `panzer-situation-view.js` supplies the UI; `assets/panzer-leader-04/` holds the manifest, counter images, boards and original card excerpt. Keep these files with the viewer. Artwork attribution: Imaginative Strategist; original Situation Card: Avalon Hill Panzer Leader. Archive source paths and hashes are retained in the manifest. The game card's date and unit attribution are not independent historical verification.


## Authoritative Situation contract

`schemas/situation.schema.json` is the JSON Schema 2020-12 contract. `sources/situations/panzer-leader-04.json` is the canonical St. Lo instance. Edit that instance, then run `python build_situation.py`. The regular `build.py` invokes the same validator. The offline `panzer-situation-data.js`, `situation-data/` components and asset manifest are generated outputs. The Atlas renderer and Situation exports consume that generated package; neither the images nor a hand-edited JavaScript roster are the source of truth. Runtime planning saves remain separate from the immutable Situation definition.

The contract includes:

- Card identity, printed date, source references and hashes, briefing, setup sequence, first player, turn limit, special rules and victory text.
- Counter types, original factors, quantities, artwork provenance, unique counter instances, optional formation lineage, initial hex positions and reinforcement availability. Historical personnel/equipment composition is separate from game factors and has its own evidence status.
- Boards, scale, axial coordinates, assembly transforms and transcription coverage. Each hex has a stable ID, base terrain, additional features, elevation, six directional edges, barriers, crossings, route references, place membership and an optional image anchor.
- Named places and objective/setup zones with explicit hex membership; route paths; cross-board seams and reciprocal adjacency. A board image is reference artwork, not terrain semantics. Directions 0 through 5 require a consistent convention in the admitting geometry compiler; rotations and seams cannot be inferred from page orientation.
- Optional geographic binding, campaign seed and generator version. Legacy geomorphic boards have no asserted real-world coordinate binding. Fixed source-board geography is not randomized by the campaign seed.
- Campaign/mission lineage, date and optional absolute interval, weather, proposed ASL-inspired phases, terrain/combat profiles and structured victory predicates. The Allied victory is an OR of two alternatives; the second is an AND of four control zones. German victory prevents that Allied result at the end of the game.
- ASL Scenario Card handoff format, identity conservation, reservation and reconciliation requirements, plus explicit admission status and blockers.

`reference-only` permits missing terrain and unresolved composition. `setup-ready` requires complete board geometry, known terrain and resolved zones. `engine-ready` additionally requires engine/rule profiles, no blockers and verified decomposition. These are data admission gates, not proof that a combat engine or ASL validator has executed. Actual engine admission still needs its own validated Scenario Card and board-package contract.

JSON Schema checks field structure and types. `build_situation.py` additionally checks cross-references, unique IDs, roster totals, setup order, formation cycles, hex adjacency, seams, objective references and readiness. JSON Schema alone cannot enforce those relationships. Both validation layers are mandatory before generating the Atlas payload.

St. Lo remains **reference-only**. Both boards now have 346 hex records and `coverage: partial`; terrain interpretation and assembly still require review. Its five victory zones remain unresolved. See the board transcription section below for the current data and interaction model. Synthetic test hexes are isolated validator fixtures, not additional St. Lo geography.

Checks: `python -m unittest test_situation_schema`, `node test_panzer_situation.cjs`, and `node test_viewer.cjs`. Install the pinned `jsonschema` dependency from `requirements.txt` before building.


### Board transcription, first pass
Both boards now contain 346 uniquely addressed hex fragments (692 total), with A..Z/AA..GG row labels and the original column-counting convention. Each has image polygons, centers, six reciprocal neighbors, terrain, feature memberships, elevation categories and colored LOS-symbol interpretations. Source directions are east, northeast, northwest, west, southwest and southeast. Ground/slope/hilltop are relative categories; no surveyed meter elevation is asserted.

The workspace opens in **JSON terrain** mode, rendered from the Situation records. **Show source artwork** switches to the source scan. Click a hex to inspect it; with a counter selected, click an interior hex to place it. Positions now store board/hex identity and snap to its center. Boundary half-hexes are excluded from setup, following original rule XIV.B.2. Earlier freehand saves remain under their v1 storage key; hex setup uses v2 and does not silently reinterpret them.

`transcribe_panzer_maps.py` compiles `sources/situations/panzer-leader-04-terrain-review.json` and samples heavy colored source hexsides. It writes the canonical Situation and two labeled review images. Then run `build_situation.py` to validate and regenerate the Atlas payload. Requires Pillow in addition to the existing schema dependency. The board images must remain unchanged for the recorded calibration to hold.

This is an interpreted first pass, not validated engine terrain. All 692 cells exist, but coverage remains **partial** because decorative terrain spill, slope extent, source LOS-symbol classification, road/stream connectivity, crossings and assembled boundary identities need further review. Town candidates are recorded for Grancelles, Kuhn, Sambleu, Caverge, Wiln and St. Athan. Victory zones remain unresolved, especially the east-of-stream subset. No movement cost, LOS result or victory is inferred from the colored view. Rivers/roads now include source-traced line geometry and ordered hex crossings; the operational transport graph remains unadmitted. Two board-local fragments must not be counted as two unique world hexes once joined.


### Road and stream line geometry
The Situation JSON now carries source-aligned `LineString` geometry for each road/stream trace in normalized board-image coordinates. The SVG renderer uses those coordinates directly, preserving bends and town streets instead of linking hex centers. Streams use blue strokes; roads use a pale center with a dark casing. Roads draw over streams at source crossings; this visual ordering does not certify bridge capacity or movement rules.

`sources/situations/panzer-leader-04-lines.json` retains control points and refined trace points. `compile_panzer_lines.py` writes these to the canonical Situation and derives the ordered hex traversals, reciprocal edge route IDs and road/stream feature membership. It also generates `board-A-line-review.png` and `board-C-line-review.png`, with pink road traces and blue stream traces over the artwork for comparison. Run it before `build_situation.py` when changing linework. The terrain transcription compiler invokes the same line compiler to preserve route geometry after rebuilding hexes.

Tracing is raster-derived display geometry. It preserves the source board's alignment, not surveyed geographic accuracy. Grancelles streets were refined from an enlarged source crop to avoid rooftop false positives. Transport graph junctions, shared board seams, legal crossings and bridge rules remain separate admission work. Source scans remain available through Show source artwork.

### Situation map illustration

`panzer-map-art.js` renders the generated board with muted ground texture, irregular woodland patches, layered stream banks, outlined roads and lighter hex lines. Grancelles has 43 source-guided illustrative building footprints. The canonical Situation package keeps these in `illustration`, separate from game terrain. Building shapes are not verified tactical obstacles. A fixed illustration seed gives stable woodland symbols across redraws.

Use **Show hex details** for terrain annotations, or **Show source artwork** to compare with the original board. Selecting a hex still reveals its identity and recorded terrain. Sambleu, Caverge, Kuhn, Wiln and St. Athan also have source-guided building artwork, with roof colors, shadows and ridgelines. All building footprints remain illustration rather than admitted tactical obstacles.

The Situation maps show an N compass rose in both generated and source-artwork views. Bearings are stored in `illustration.northIndicators`, measured clockwise from the displayed sheet top. Situation 4 uses A = 90 degrees (right) and C = 270 degrees (left), interpreted from the card diagram and the inverted board letters on the source sheets. This does not resolve board assembly translations or seams.

### Joined Situation setup

Both boards now display side by side: A rotated 90 degrees clockwise on the left, C rotated 90 degrees counter-clockwise on the right, following the specified Situation assembly. North points up in this joined view. This supersedes the earlier separate-sheet bearing interpretation. The source sheets are preserved. Counter placement and inspection convert rotated display coordinates back to saved sheet coordinates, preserving existing plans. The same assembly applies to source artwork. Labeled approach arrows indicate opposing setup sides, not mandatory entry routes. Horizontal scrolling keeps counters readable. Cross-board hex seam admission remains pending.

The joined view uses one compass rose at the upper left of the left board, with 70% opaque background. Matching boundary half-hexes inherit woods visually if either half is wooded. Both sheets render identical deterministic woodland geometry in shared seam coordinates, so the patch continues across the join. This illustration reconciliation preserves the source transcription and does not admit cross-board movement rules.

Mouse-wheel zoom on either Situation board follows the pointer, from fitted size to 6x. The board viewport scrolls horizontally and vertically for panning. Zoom and pan survive counter selection and placement redraws. **Fit both boards** returns to the overview. Wheel input outside the boards keeps normal page scrolling.

Pan the Situation setup with **Space + left-mouse drag** or **middle-button drag**. Pointer capture keeps the drag active outside the map until release. Panning suppresses counter placement and native middle-button autoscroll. Space remains available for typing in notes and other text fields.

German and American roster groups can be expanded or collapsed independently. Their open/closed state, and the nested unit-type sections, are retained during setup redraws.

### Campaign Situation Card workflow

Open **Normandy campaign** to enter the parent operational workspace (8-30 June 1944). Its **Campaign Situations** list contains **St. Lo | 29 June 1944 | Review card** and saved Situations generated by battalion mission planning. Select a card, review its forces, setup, objectives and map, then choose **Open campaign map**. Returning from the detailed map returns to the Normandy workspace. St. Lo is not a separate campaign entry.

The parent browsing window is authored scope, not an expanded simulation clock. The existing 8 June command snapshot retains its own dated forces and save state; St. Lo retains its 29 June roster and placements. The canonical Situation package records its `parentCampaign` identity and window. Cross-situation scheduling, force reconciliation and combat remain pending. Panzer Leader remains source attribution; ASL Scenario Cards are lower-scale engagement contracts.

### Deployment setup and game map

The Situation Card opens **Set up campaign map**, or **Resume map setup** when counters have been placed. Complete German setup, then American setup. **Launch situation game map** becomes available only after both sides are complete. The game view uses a wider dialog and taller map viewport, with no deployment roster or setup editing. Zoom, drag panning and inspection remain available. Source artwork remains available in setup. **Review deployment** returns to the completed setup view. The launched state and original counter positions are saved; the card then offers **Resume game map**. This is a game presentation shell, not an implemented turn/combat engine.

### Parent map and organization references

The Normandy parent map no longer displays the former illustrative 1st/29th Infantry Division and V Corps badges. Their dated organization records remain in the collapsed **8 June command organization (reference)** section. Actual deployment counters belong to each selected Campaign Situation map; the parent workspace lists Situation Cards.

### Situation marker preview

The Normandy parent map shows a numbered **01** marker for St. Lo, synchronized with the Situation list. Select it with the mouse or Enter/Space to see its date, objective, force totals, deployment count and next action. Progress derives from the saved Situation: Not started, Deploying, Ready to launch or Game map opened. The marker is a [town location reference](https://en.wikipedia.org/wiki/Saint-L%C3%B4), not a board footprint or historical deployment coordinate. The marker is hidden outside the parent workspace and while a child map is open.

### Campaign-local Situation numbering

`sources/campaign-registry.json` is the campaign list. Each campaign has a stable ID, its own Situation records and a monotonically increasing `nextSituationNumber`. Normandy currently assigns **01** to St. Lo; its next number is **02**. Another campaign may independently have a Situation 01. Situation IDs remain stable and distinct from display numbers. `campaignAssignment` links the exported Situation package to this registry. Build validation rejects duplicate campaign IDs, duplicate Situation IDs, duplicate numbers within a campaign and reuse of an assigned number.

Panzer Leader Situation **4** remains the source reference. Its existing package ID and save keys are retained for compatibility and do not define the campaign display number. Generated battalion cards are still unnumbered until registered; their list order is not an implicit numbering scheme.

Situation marker titles and progress labels appear only at 0.10 map kilometers per screen pixel or closer. At wider zoom, only the number remains; hover reveals the name, date and progress in an SVG tooltip. Accessible marker names and the selectable side-panel preview retain these details at every zoom. The threshold is defined by `situationLabelMaxKmPerPixel` in the Situation view.

## Map Utilities: Counter Colors

Choose **Map Utilities > Counter Colors** in the atlas header, available independently of the campaign workflow. The utility retains the `counter-color-test.html` address for existing bookmarks. This isolated dummy Situation reuses Board A terrain and places 14 symbolic infantry color samples at stable interior hex positions. Samples cover WWII ASL nationality groups and the black SS alternative. Shared minor-power and Commonwealth colors represent multiple countries. Sample factors are illustrative, not historical counter data.

Select a counter or palette, then adjust hue, saturation and lightness within proposed family bounds. Compare the same sample over open, woods and town backgrounds; toggle its contrasting edge and shadow, resize counters and zoom/pan the map. Nationalist Chinese samples use the editable Soviet palette as their brown surround. Export palette JSON to record candidate screen colors. Changes persist only under `campaign-atlas:counter-color-test:v1`; they do not affect campaign saves or production counter artwork. These SVG symbols are color-test substitutes, not recolored Imaginative Strategist scans.

The user-approved defaults are archived in `sources/counter-palette-defaults.json` and used as the initial and restore-default values in `counter-color-test.js`. German, Soviet, British, Communist Chinese and Partisan defaults incorporate the supplied palette export. Existing browser adjustments take precedence until restored. The permanent menu is maintained in both `viewer.html` and generated `index.html`.

## Map Utilities: Terrain Colors

Choose **Map Utilities > Terrain Colors** in the atlas header. The utility uses the existing Board A/C renderer, with synchronized current and edited previews, wheel zoom and Space+left/middle drag. Select a feature in the edited preview or choose a semantic color from the list. Controls cover terrain fills, woodland textures, roofs, bridges, road/stream edges, grid opacity, and text/halos. Enable hex identifiers or hide the comparison for a larger edited map.

Optional symbolic counters use the approved exported palette via `counter-palette-defaults.js`, an offline wrapper of `sources/counter-palette-defaults.json`. The terrain preview does not read draft Counter Colors browser settings. Terrain edits save only under `campaign-atlas:terrain-colors:v1`; export `campaign-terrain-palette.json` for review. Restore defaults returns to shipped map colors. No production map, campaign data, or terrain geometry is modified. The renderer is reused without changing its production defaults; utility color bindings are applied after rendering, with separate SVG pattern IDs for each preview.

Files: `terrain-colors.html`, `terrain-colors.css`, `terrain-colors.js`, and `counter-palette-defaults.js`. Keep these alongside the existing Panzer map scripts.

Terrain Colors opens on **All hex types: demo board**. Its 32 labeled samples cover all 11 base terrain enum values (including unknown), all 19 feature enum values from the Situation schema, and elevations 1/2. Additional terrain symbols and 19 new color controls are utility-only appearance proposals, not campaign rule implementations. Board A/C remain available. Demo samples use stable D1-D32 identifiers; optional counter samples sit beside the primary symbols. The authored gallery is defined in `terrain-demo.js`.

The default terrain demo is now a continuous adjoining hex landscape, replacing the isolated specimen gallery. It combines wooded slopes, roadside towns/villages, orchard/bocage fields, lake/marsh shores, a continuous stream and road bridge, ridge defenses, and desert dunes/scrub/wadi. All schema base/feature values remain represented. Regional captions replace per-sample captions; enable hex identifiers for individual cells. This deliberately mixed landscape tests appearance rather than historical geography.

Terrain utility counters can be left-dragged in either preview; positions stay synchronized and are retained per board during the visit. Space+left drag and middle drag still pan. Arrow keys move a focused counter by 10 map units (Shift: 50). Reset counters affects only the selected preview board. Placements are not exported or stored in campaign data.

Terrain Colors reserves ordinary terrain clicks for color selection. Left-drag on a counter moves it; Space+left or middle drag pans over either counters or terrain. Plain left-drag does not pan.

A horizontal splitter between Default and Edited palette previews adjusts their relative height (15-85%). Double-click or press Enter to restore 50/50; Up/Down arrows adjust, Shift accelerates, Home/End reach the limits. The ratio is saved under `campaign-atlas:terrain-split:v1` and the divider hides with comparison mode. Implementation: `terrain-splitter.js`.

Approved terrain defaults are stored in `sources/terrain-palette-defaults.json` with the offline wrapper `terrain-palette-defaults.js`. Both the Default preview and restore actions use this approved baseline. Original renderer color tokens remain separate so recoloring preserves semantic bindings. The supplied export changes open ground to #d4eebe and town ground to #8b95a2; other colors and 27% grid opacity are retained. Browser drafts continue to override initial edited values. Production campaign artwork is unchanged.

St. Lo setup and game maps now apply the approved terrain and counter exports through `campaign-appearance.js`. Generated terrain is recolored without changing geometry, source scans or selection amber. Roster and deployed counters use SVG color-transfer filters over the original Imaginative Strategist images: German blue and US green, with preserved dark symbols/factors, contrasting edges and shadows. The source-artwork toggle still shows the unmodified board scan. Utility browser drafts do not alter production appearance; approved defaults files do.


## Imported Western Front Situation library

From **Choose your task > Historical Campaign Situations**, select a campaign collection and choose **Open campaign situations**. Select a numbered Situation, **Review Situation Card**, then **Set up campaign map**. Select a counter to highlight its permitted deployment hexes. Complete both sides in the source card's setup order, then **Launch situation game map**. Each Situation saves independently. **Return to campaign selection** returns to the library entry; the existing Normandy command exercise is still available.

| Collection | Local number | Source | Situation | Printed date | Allied / German counters | Boards |
|---|---|---|---|---|---|---|
| Normandy | 01 | PL04 | St. Lo | 29 June 1944 | 42 / 34 | A, C |
| Normandy | 02 | PL05 | Operation Goodwood | 18 July 1944 | 60 British ground + 8 aircraft / 54 | B above A above C |
| Reichswald source-card study | 01 | PL06 | The Reichswald | 7 September 1944, unverified | 44 Canadian / 19 including block | D |
| Lorraine advance | 02 | PL07 | Encirclement of Nancy | 14 September 1944 | 41 / 23 | D beside A |
| Saar counterattack | 01 | PL12 | Prelude: The Saar | 25 November 1944 | 41 / 51 | A, D, C beside one another |
| Lorraine advance | 01 | PL08 | Marieulles | 16 September 1944 | 30 / 15 | A above D |
| Ardennes operations | 01 | PL14 | Bulge: Thrust | 18 December 1944 | 24 / 46 | C above D |
| Ardennes operations | 02 | PL16 | Bastogne: Prelude | 19 December 1944 | 27 / 24 | D |
| Ardennes operations | 03 | PL13 | The 'Fortified Goose Egg' | 17 December 1944 | 88 / 90 | C above D |
| Ardennes operations | 04 | PL15 | Elsenborn Ridge | 18 December 1944 | 45 + one block / 70 | C beside D, portrait |
| Ardennes operations | 05 | PL17 | Turning Point: Celles | 25 December 1944 | 58 US + 22 British / 72 | A above C above D |
| Ardennes operations | 06 | PL18 | Bastogne: Siege | 26 December 1944 | 52 ground + 11 aircraft / 56 | D, C, A beside one another |
| Ardennes operations | 07 | PL19 | Patton's Counter Offensive | 31 December 1944 to 1 January 1945 | 107 ground + 6 aircraft / 79 | A, C, D beside one another |
| Rhine crossing | 01 | PL20 | Remagen Bridge | 7 March 1945 | 14 / 11 including block | D |
| Market-Garden ground operations | 01 | PL09 | Operation Market: Nijmegen | 20 September 1944 | 36 including bridge / 37 | D beside A |
| Market-Garden ground operations | 02 | PL10 | Operation Market: Arnhem | 22 September 1944 | 16 British / 31 | A |
| Market-Garden ground operations | 03 | PL11 | Operation Garden: Anticlimax | 29 September 1944 | 35 Belgian + 30 US / 53 | A above D above C |

The new collections are dated source-card libraries. They do not assert new historical headquarters snapshots, geographic deployments or linked supply/force ledgers. St. Lo retains its existing approximate parent-map marker. The other Situations currently open from the list, without invented geographic board footprints.

All imports use the shared renderer, approved terrain/nationality palettes, Imaginative Strategist counter artwork, source-card review, collapsible rosters and placed counts, mouse-wheel zoom, Space/left-drag or middle-drag pan, plan export, saved deployment and larger game-map view. Marieulles limits German setup to Grancelles. Bulge has distinct Allied Group A/B zones and Allied-first setup. Bastogne restricts both sides to their source roads and automatically places each loaded mortar with its assigned truck.

The source victory clauses and special rules are preserved. Turn execution, combat, bridge demolition, turn-two elimination, exit scoring, graded victory, supply reconciliation and executable ASL decomposition are **not implemented**. Terrain and setup geometry remain interpreted, reference-only data. See [batch import review](sources/situations/IMPORT-REVIEW.md) for source evidence and remaining gates.

### Rebuilding and validating Situation packages

- `import_situation_batch.py` reproduces the three new packages and shared assets from the local archive. Requires Pillow and pypdfium2; the archive root is declared at the top of the script. Run it only when regenerating the reviewed imports, because it replaces those source packages and their campaign registrations.
- `build_situation.py` validates all numbered `sources/situations/panzer-leader-*.json` packages and the campaign registry, then emits the initializer and `situation-data/` components. Requires jsonschema and existing builder dependencies. `PANZER_SITUATION_DATA` remains the St. Lo compatibility entry; `PANZER_SITUATION_LIBRARY` contains the seventeen packages.
- `test_situation_batch.cjs` covers all rosters, assets, restricted setup, loaded mortars, launch, dated handoff drafts, separate saves and shared UI handlers. `test_panzer_situation.cjs` retains St. Lo regression coverage. These use a DOM test double and do not replace browser visual checks.
- `test_situation_schema.py` and `test_ui_encoding.py` validate package structure and reject UI text encoding corruption.


### Second Situation batch

`import_situation_batch2.py` reproduces PL13/15/17 using the first batch's shared board data and the archived original cards (PDF pages 44/45/46, upper halves). Run after `import_situation_batch.py`, then `build_situation.py`. Both importers preserve other registered Situations and existing local numbering. The second importer requires Pillow and pypdfium2.

Multi-board setup is per instance. Selecting a counter highlights every permitted board; the first board is only a view hint. Quarter-turn transforms support Elsenborn's two portrait boards. Celles British units use the approved British palette and remain on their own setup board. Elsenborn includes an authored X roadblock symbol, identified from the original unit-function table, rather than claiming an Imaginative Strategist artwork match. It has no combat factors and cannot be allocated as an ASL combat unit.

All packages use the existing setup, export, independent save and game inspection workflow. Roadblock effects, combat, movement, graded scoring, source-rule precedence and cross-Situation supply/force continuity remain unimplemented. No formation engine admission is implied by a completed deployment.


### Third Situation batch

`import_situation_batch3.py` reproduces PL18/19/20 and nine additional Imaginative Strategist counter crops. Run after the earlier importers, then run `build_situation.py`. It preserves other registrations. Original card evidence is PDF 46 lower half and PDF 47 upper/lower halves.

PL18 and PL19 have a separate **Air support** disclosure in setup and game views. Aircraft have no ground placement or invented ground factors, and do not count toward ground setup completion. Their typed availability and flight restrictions are reference data; aircraft arrival, activation and combat remain unimplemented. Patton's two-day date is retained. PL20 restricts its roadblock to D-BB-6 and ground forces to their interpreted bank/town zones. Bridge clearance, demolition and movement restrictions require future rules implementation.


### Fourth Situation batch

`import_situation_batch4.py` reproduces PL09/10/11, their source cards and counter assets. Run after earlier importers, then `build_situation.py`. Stream calculations are cached; other registrations and numbering are preserved. The Market-Garden collection is available from Historical Campaign Situations.

Arnhem's German highlights and saved-plan validation use actual British placements for minimum separation. Nijmegen's tank bridge is equipment with null combat factors, counted in setup but excluded from ASL combat selection; emplacement is not implemented. Belgian counters use Allied Minor coloring and explicit Belgian labels. The Valentine bridge carrier uses original-card artwork as a documented exception to IS imagery. Source victory grades, movement, bridge effects and Anticlimax's conditional sector release remain reference-only.


### Fifth Situation batch

`import_situation_batch5.py` reproduces PL05/06/07/12 after batches 1-4; then run `build_situation.py`. All seventeen original land cards are included. Amphibious PL01-03 remain excluded. The importer preserves existing local numbers, extends Normandy's browsing window through Goodwood, and adds a separate date-unverified Reichswald source collection and the Saar collection.

Board B has 346 interpreted hexes and source-guided coastal water, settlements, roads, streams, bridge, hills and woods. `sources/situations/board-B-review.json` retains its transcription controls. Canadian units use Commonwealth coloring with Canadian labels. Goodwood enforces its armor-on-A deployment restriction and records a five-aircraft concurrency limit. Aircraft activation is not implemented. Original-card Cromwell/Maultier images are documented exceptions to IS artwork because the printed factors differ.

Reichswald and Nancy enforce their printed row boundaries. The Saar retains all three boards and four exit references. The shared woods-seam treatment applies only to the matching A90/C270 adjacency. Reference terrain, setup interpretation, source-rule precedence, combat and scoring still require further work before gameplay admission.

## Ardennes command smoke test

Open `ardennes-command.html` through the local server, or use its link in the Atlas Choose panel. Parent allocation now controls two missions, delivery times and generated Situation deployments. Essentials applies a disclosed automatic policy; Operational exposes supply and reserve tradeoffs. Normal sessions stop at deployment. Fixed results and disruption controls require an explicitly isolated test session. See [workflow and acceptance](../../src/ASL/docs/Ardennes%20Command%20Prototype%20Acceptance.md). Run `node test_ardennes_command.cjs` for command and deployment integration checks.

### Integrated command map and timeline

Use `index.html?exercise=ardennes` (the older `ardennes-command.html` redirects here). Command hierarchy and decisions are in the left pane, linked to HQ markers on a central authored operation diagram. Select supply or transport markers to inspect origin, destination and custody. The toolbar separates live campaign advancement from read-only history playback and forecast scrubbing. Return to current time before issuing orders. The diagram is not historical/georeferenced terrain. Combat remains unavailable.

`ardennes-spatial.js` implements pure history/forecast and route-position projections. `ardennes-workspace.js` and `.css` provide the integrated map UI; `ardennes-command-view.js` retains the decision forms. Run `node test_ardennes_spatial.cjs` alongside `node test_ardennes_command.cjs`. Same-minute events are projected as one time boundary. Snapshots, branching and saved viewing-time preferences are deferred.

### Player navigation cleanup

Campaign actions live in the header Campaign menu: open/resume the Ardennes operation, start a new operation while archiving the current one, export the saved operation, and restore an archived operation. Progress saves automatically. Map Utilities contains palette tools, the standalone Situation reference library, source research and atlas image export. Image export is offered on the atlas view only.

The legacy Normandy free-form order/communication workflow, stub phase controls, development fixtures and separate seed-reset/export controls are retired from the player interface. Their DOM bindings and saved data are preserved for compatibility. Reference Situation cards still open their briefing/setup workflow; active operation cards remain accessible through mission commitment. The right inspector appears only for relevant Situation content. `atlas-navigation.js` and `.css` implement this navigation boundary.

## Campaign ownership and continuous-world slice (9 October 2026)

Implemented in the HTML/JS prototype before the Blazor port:

- A campaign save owns the map seed, generator version, command operation (clock, hierarchy, stock, missions and command history), world nodes, roads, river, persistent roster, deployment history and terrain events. Existing command-only sessions migrate without deleting their originals. Campaign exports and saved-session selection use the campaign envelope.
- The Ardennes campaign has an authored 40 km by 16.8 km footprint anchored in the atlas EPSG:3035 coordinate system. Local coordinates are meters east and south of that origin. This is an illustrative region, not a reconstruction of historical deployment locations or roads.
- A committed M14 or M15 mission binds its supply-adjusted roster to an 8 km by 6 km footprint. Setup uses persistent campaign unit IDs and 250 m hexes. It no longer opens the imported board package as though that package were contiguous campaign geography.
- Campaign (1 km), formation (250 m), and tactical terrain preview (40 m) read the same fixed world features. Seeded woodland geometry is independent of the requested grid. The overview shows the same footprint and features through the atlas transform. Terrain events and deployment history are time-filtered.
- Deployment enforces side-specific west/east halves, one counter per hex, current-time editing and completion only after all counters are placed. These are explicit prototype adaptation rules.

Remaining work: researched/georeferenced terrain and road provenance, full campaign-wide spatial refinement beyond this authored region, adapted mission victory conditions and special rules, force decomposition into ASL Scenario Cards, terrain-compiler admission and combat reconciliation. The 40 m view is a terrain preview, not an admitted ASL scenario. Imported Panzer packages remain reference/standalone material. Changing geography invalidates their board-specific rules until those are deliberately adapted.

Validation: `test_campaign_world.cjs` covers common coordinates, grid overlap identity, deterministic terrain, mission/force binding, deployment restrictions, historical placement, terrain events, persistence and stale-write rejection. Existing command and spatial replay suites also pass. Browser verification covers supply receipt, mission commitment, shared-world setup and persisted placement.

### Campaign Situation screen integration (9 October 2026)
Committed Ardennes missions now open the established Situation Card in the right pane. Its setup action embeds the existing counter tray, joined boards, deployment rules, palette, zoom/pan, completion and game-map controls in the central pane. The selected regiment remains on the left. Source-board plans save under the campaign's `situationPlans`, independently of preserved generated-terrain preview placements and standalone saves. Generated placements are not silently mapped onto different source-board coordinates. During setup the shared clock stays visible; return to campaign command to change timeline view. Source boards are explicitly local authored battlefields, not geographically continuous refinements. Combat remains unavailable.

Validation includes the existing package suite plus an embedded-screen test for card/setup/game launch/review and campaign save callbacks. The browser was checked for card opening and joined-board display with all three panes visible.

### Map workspace layout

The command map has a single-line date/navigation toolbar with labelled icon tooltips, plus a single-row timeline scrubber. The top arrow collapses timeline controls while retaining the date and time. Map details below the map collapse independently. Map tools remain available. Drag either vertical pane divider, use its arrow keys, or double-click to reset its width. Layout preferences persist locally and do not alter campaign state. Situation setup retains these layout controls; campaign time navigation remains locked while setup is open.

Situation setup uses a compact row for setup completion, game launch and the hex-details checkbox. The hamburger toggles the entire counter roster, giving its width to the board. Source artwork is available under the right-hand Campaign Situation Card during setup. Wheel zoom and Space+left-drag or middle-drag panning remain available. Redundant instruction strips and the enlarge button have been removed; validation errors still appear when needed.

During embedded Situation setup the timeline is automatically collapsed and its disclosure disabled; the campaign date and time remain visible. Returning to command restores the preferred timeline layout. Situation planning notes use a disclosure closed by default, retaining their saved text.

Each active unit type has Deploy All and Clear All icon controls with tooltips. Deploy All highlights a suggested origin, then waits for a legal hex click. Only unplaced units of that type are allocated, in increasing hex-distance rings on that board, skipping unavailable hexes. Placement is atomic if space is insufficient. Clear All removes only that type. Mandatory loaded transport pairs use individual carrier placement. Counters can be dragged between legal hexes during their side's setup; Space+left drag and middle drag still pan.

Board orientation audit: reviewed the original card diagrams for imported Situations 05-20. The recorded board order, layout axis and rotations agree with the diagrams. Board headings and setup paragraphs have been removed from the joined geometry so vertical joins are edge-to-edge as well as horizontal joins. Board order and setup instructions are available in the toolbar legend tooltip and Situation Card.

### Situation data components

The former 51.91 MiB aggregate is split by Situation and responsibility:

- `panzer-situation-data.js`: small library initializer.
- `situation-data/campaign-registry.js`: campaign membership and numbering.
- `situation-data/panzer-leader-NN.js`: one Situation definition, including counters, instances, rules, deployment constraints and provenance.
- `situation-data/panzer-leader-NN-geography.js`: that Situation's boards, hex terrain, topology, places, routes, seams and illustration layout.
- `situation-data/scripts.json`: ordered script manifest used by tests. The builder writes the corresponding explicit script tags into all four consuming HTML pages.

Definitions and geography assemble into the existing `PANZER_SITUATION_LIBRARY` interface. `PANZER_SITUATION_DATA` references the same St. Lo object rather than embedding a second copy. Geography remains scoped to each Situation because board assemblies, seams and feature references can differ even for the same printed board letter. Loading uses synchronous classic scripts, preserving offline file access and startup order. This is a packaging change, not lazy loading; the complete library still loads at startup.

Edit canonical JSON under `sources/situations/`, then run `python build_situation.py`. Do not edit generated components directly. Run `node test_situation_data.cjs` to verify exact reconstruction of every canonical source, registry, compatibility alias and all consuming pages' script order.
