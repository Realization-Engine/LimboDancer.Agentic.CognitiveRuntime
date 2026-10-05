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
