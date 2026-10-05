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

Enable **Show theater areas** and select an outline, or use the theater selector, then **Enter Theater**. **Return to overview** or **Fit overview** restores continental framing. Return to overview remains visible but disabled in the overview. Theater outlines support keyboard selection and hide on entry so they do not intercept sector selection. Panning beyond the area is unrestricted.

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
