# Europe hex map prototype

A standalone, offline geographic prototype for the Military Command and Multiscale Simulation Design. Open **index.html** in a browser. Select hexes, pan/zoom, toggle layers, create a campaign seed and export the structured map or selected hex.

## Implemented
- Natural Earth land, lakes, river lines and selected cities from a pinned upstream commit. Original inputs and SHA-256 checksums are in sources/.
- EPSG:3035 European equal-area projection; pointy hexes with a 60 km radius and approximately 104 km across flats. Axial coordinates and the grid origin are fixed.
- Polygon-intersection land fractions, river intersection lists, city associations, stable hex IDs and geographic GeoJSON export.
- Campaign seed persisted in browser local storage. SHA-256 derives a stable per-hex refinement token from seed, base hash, generator version, hex ID and layer.
- Source geometry remains fixed across seeds. Lower-scale procedural terrain generation is not implemented; fresh seeds currently change refinement tokens only.
- No dependencies or network requests when opening the generated HTML. Browser storage is local to the opening origin/path and exports preserve the seed explicitly.

## Files
- index.html: self-contained interactive deliverable.
- preview.png: static geographic preview, not a browser screenshot.
- map.json: projected features, hex metadata, source manifest and base hash. Feature coordinates use the documented local-kilometer transform, not geographic GeoJSON.
- hexes.geojson: standard longitude/latitude hex polygons and properties.
- viewer.html: interface template.
- build.py: reproducible builder; verifies original input hashes.
- test_map.py: source/hash, coordinate, subset/order, neighbor geometry and export checks.

## Rebuild
Use Python 3.12 with requirements.txt installed in an isolated environment:
    python -m pip install -r requirements.txt
    python build.py
    python -m unittest test_map.py

Optional static preview requires Pillow:
    python preview.py

Pin the full environment for cross-platform byte-identical build guarantees; the prototype's same-environment reproducibility is tested. Generated outputs retain source hashes. Dependency or PROJ changes require revalidation, not silent source substitution.

## Scope and limitations
This is contemporary generalized reference geography, not WWII-admitted terrain. Cities use source names. No political boundaries are displayed. Mountain regions, year-2000 tree cover and modern transport are reference overlays, not gameplay-admitted WWII layers. Numeric elevation, historical crossings and dated settlements still need reviewed sources. A land cell is not an assertion of open ground.

The source is cartographic 1:50 million geography, too generalized to directly recover ASL terrain or precise bridge positions. River lists indicate geometric intersection, not navigability, crossing rules or a routed logistics graph. Coastlines retain their actual simplified geometry inside mixed hexes.

The map has a bounded Europe viewport; cropped geographic features may continue outside it. Full hexes near viewport edges remain identified. The map seed is not combat randomness. No campaign battle-damage system or live ASL integration is implemented.

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

sources/transport-1939-review.json records candidate evidence and its limitations. The first review area is France and the Low Countries. The inspected Seine reconstruction uses December 31 status and excludes temporary wartime changes; the Mitteleuropa catalog dates its map to 1940. Neither establishes September 1 coverage. The 1920-1940 NAKALA dataset has been identified bibliographically but its files have not been inspected.

The historical importer is deliberately not implemented: a nonempty admission file fails the build until segment/date validation is implemented. This prevents manually relabeling modern features as historical. The missing work is source acquisition, georeferencing/digitization, per-segment corroboration, uncertainty and topology review. Modern data is retained solely for comparison, not automatically backdated.

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

The earlier statement that NAKALA files had not been inspected is superseded by this acquisition. A complete exact-date 1939 network is still outstanding.

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
The Europe view is now labeled Continental View. Enable Show theater areas and select an outline, or use the theater selector, then Enter Theater. Return to Europe (or Fit Europe) restores the continental framing. Theater outlines support keyboard selection. Outlines hide on entry so they do not intercept hex selection. Panning beyond an area is unrestricted.

The four areas in theaters.json are illustrative campaign presets, not historical command boundaries: Western Europe, Eastern Europe, Mediterranean and Northern Europe. They may overlap. Definitions use the same local EPSG:3035 coordinates as the map, and contain unassigned command/objective fields. Campaign exports include these definitions separately from base geography. Editing area definitions does not regenerate terrain or alter the geography hash. Entering a theater preserves the campaign seed and selected hex, and reveals at least the regional railway tier.

This implements navigation and transport detail on the existing continuous map. The 104 km hex grid and existing source geography are retained. Finer hex resolution, additional settlement data, formations, logistics and command assignments are not yet implemented. No new geographic detail is invented by entering a theater.


## Western Europe theater workspace
The earlier navigation-only limitation is superseded for Western Europe. Enter this theater to switch to Geography, Logistics and Planning modes. Its globally anchored 15 km-radius grid is approximately 26 km across flats, with 3,200 land-intersecting sectors. It is a separate resolution on shared coordinates, not an assertion that coarse hexes subdivide exactly. Sectors are selectable and show reference settlements; these do not carry ASL terrain or movement adjudication.

The theater adds 293 reference settlements and 54 river features from Natural Earth 1:10m geography, clipped from the pinned upstream commit. sources/western-manifest.json records upstream and clipped-file hashes; prepare_western.py verifies the clipped files. This provides greater cartographic detail, not historical urban extents, 1939 river engineering, or crossing availability. Continental geography and its hash remain unchanged. The finer grid uses the existing generalized land mask, so coastal accuracy remains limited. Close town labels are suppressed deterministically to reduce collisions.

Logistics mode displays 12 approximate town-center markers with individual sources and date labels. Roland G. Ruppenthal, U.S. Army official history, Logistical Support of the Armies, Volume II, chapters V and VI, documents the 1944-1945 ports, transport transfers and forward depot areas. These examples are not backdated to the campaign's September 1939 baseline. Port capacity, working rail routes, depots and unit positions are not synthesized as live state. Sources:
- https://www.ibiblio.org/hyperwar/USA/USA-E-Logistics2/USA-E-Logistics2-5.html
- https://www.ibiblio.org/hyperwar/USA/USA-E-Logistics2/USA-E-Logistics2-6.html

Planning mode stores user-written sector notes under the current campaign seed in browser storage. Clicking a saved note returns to its sector. Campaign export includes notes and theater definitions, but excludes the reference theater package and research rail layers. No order is dispatched and no formation or mission lifecycle is simulated. Commands remain unassigned. Other theater presets retain basic navigation. Browser rendering remains unverified because local-file browser access is unavailable; logic tests cover modes, inspection, storage separation, return navigation and exports.


Western theater focus: a translucent gray veil covers the area outside the union of its hex sectors, with a thin, constant-screen-width perimeter. The footprint is compiled from full-precision lattice vertices to avoid internal seams. Open water outside the hexes is gently dimmed but remains visible. The veil follows the viewport through pan/zoom, ignores pointer events, remains independent of the grid toggle, and disappears outside Western Europe. This is a presentation boundary, not a movement restriction or historical command boundary.
