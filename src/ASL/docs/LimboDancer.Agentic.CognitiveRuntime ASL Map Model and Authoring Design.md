# LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design

**Status:** Proposed

**Parent:** [ASL Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), requirements ASL-MAP-010 to 015, ASL-MAP-020 to 024, ASL-MAP-043, ASL-MAP-050 to 056, ASL-MAP-070 to 074, and ASL-MAP-080 to 082.

**Companion:** The [VASL Board Ingestion Design](<LimboDancer.Agentic.CognitiveRuntime ASL VASL Board Ingestion Design.md>) defines the VASL file formats, the standard board geometry, and the VASL-compatible hex-fact derivation. This document refers to them and does not repeat them.

**Scope:** The types in `LimboDancer.Domains.Asl.Maps`: coordinates and locations, the Terrain Grid, Hex Facts, the Feature Model, the compiler, the vectorizer, validation, Scenes, board composition, the canonical board package, and the read API for consumers. Rendering and the Studio UI are specified in the Map Studio Architecture and Rendering Design.

## 1. Model overview

```text
                    +-------------------+
   authoring ---->  |  Feature Model    |  editable, versioned, fixed-point geometry
                    +-------------------+
                       |  compile (deterministic)          ^  vectorize (approximate,
                       v                                   |  ingested boards only)
                    +-------------------+  -----------------+
   VASL ingest -->  |  Terrain Grid   |  canonical; per-cell terrain code and elevation,
                    +-------------------+  per-hex stairway flags
                       |  derive (VASL-compatible, Ingestion Design section 7)
                       v
                    +-------------------+
                    |  Hex Facts        |  read-only; what consumers use
                    +-------------------+
```

A **board** is identified by a `BoardRef` and versioned by a content hash. An ingested VASL board is defined by its grid; its Feature Model, when present, is a derived and approximate artifact. An authored board is defined by its Feature Model; its grid is a deterministic compilation of it.

## 2. Design findings

These observations from decoding VASL boards drive the compiler and vectorizer design.

1. **Hills are per-pixel elevation plateaus.** Board 02 has pixel elevations 0 to 3 in nested regions. Board 24 has large areas at -1. Elevation is therefore an area property, not a per-hex value.
2. **Depressions are stored one level down.** VASL's LOS editor subtracts one from the elevation of every depression-category pixel (gully, streams) before writing `LOSData`. Board 12 gullies are at -1. The subtraction is relative, so repeating it is not idempotent.
3. **Hexside features are thick strokes centered on the hexside.** Across boards 02, 04, and 12, wall, hedge, bocage, and cliff pixels measured perpendicular to a hexside are 4 to 17 pixels thick. The derivation samples one pixel inside each hex (Ingestion Design section 5.3), so a centered stroke of 3 or more pixels is seen by both hexes.
4. **Some area terrain is dithered.** Grain on boards 04 and 12 has only 25 to 29 percent of its pixels fully surrounded by grain. Other area and linear terrain measured 82 to 96 percent, and hexside terrain 76 to 81 percent. Grain is interleaved with Open Ground pixels because of how VASL classified the board art. As a result, a hex that looks like grain can derive `Open Ground` at its center sample.
5. **VASL's LOS editor runs fixed post-passes after painting.** In `LOSDataEditor.createLOSData`:
   - nearest-terrain fill of unknown pixels;
   - cliff pixels set to the lower adjacent hex base level;
   - building-type overrides;
   - exterior factory walls (factory pixels with a non-factory 4-neighbor become the matching wall code);
   - depression elevation minus one;
   - sunken road pixels become Elevated Road at elevation 1 in level-1 hexes, and elevation -1 elsewhere;
   - stairways from stairway-colored pixels.
   
   A grid that VASL-compatible derivation treats correctly has to obey the same conventions.
6. **The standard hex is not regular.** Width 56.25 and height 64.5 give a height to width ratio of 1.1467, not the regular 1.1547 (Ingestion Design finding 7). Rotating geometry by 60 degrees in pixel space does not map the hex grid onto itself.

## 3. Coordinates and locations

### 3.1 Hex and hexside indices

- `HexIndex(col, row)`: the grid index of the Ingestion Design, section 5.2.
- `HexName`: canonical text such as `E4`. Parsing is case-sensitive, and column letters are uppercase repeated letters (`A` to `Z`, `AA` to `ZZ`).
- `HexsideRef(hex, side)` with `side` in 0 to 5 (N, NE, SE, S, SW, NW). A hexside shared by two hexes has two refs. The **canonical** ref uses `side` 0, 1, or 2: a ref with side 3, 4, or 5 is converted to the neighbor across it with the opposite side. A board-edge hexside with no neighbor keeps its original ref.

### 3.2 Board references

| Kind | `BoardRef` text | Example |
|---|---|---|
| VASL board | `bd` + VASL board name | `bd01`, `bd24`, `bdRB` |
| Authored board | `ab-` + lowercase slug | `ab-river-village` |
| Composed map | `map-` + slug | `map-scenario-12` |

A `BoardRef` names a board lineage. A `BoardVersion` (section 9.3) names one exact version. Consumers that record evidence record both.

### 3.3 Locations

`BoardLocation(board, hex, level)`, with text form `<boardRef>:<hex>:<level>`:

- `level` 0 is ground level, positive values are building upper levels, and -1 is a cellar where the Hex Facts define one;
- `bd01:E4:0` is the existing Scenario A1 form, so the mapping is the identity (ASL-MAP-022);
- a hexside location is `<boardRef>:<hex>:<level>/<side>`, for example `bd01:E4:0/2`, and is normalized to the canonical hexside ref.

### 3.4 Published coordinate forms

`LocationParser` accepts the forms used in rules, examples, and scenarios (ASL-MAP-021):

| Input | Needs context | Result |
|---|---|---|
| `bd01:E4:0` | no | exact location |
| `1E4`, `36AA8` | a board set mapping board numbers to `BoardRef` | ground-level location on that board |
| `E4` | a single current board | ground-level location |

Board numbers are never assumed to map to `bdNN`. The mapping comes from a `BoardSet` (a scenario's board list or a composed map). Ambiguous or unmapped inputs return a parse diagnostic, never a guess (ASL-RD-011). Aliases do not change canonical identity (ASL-RD-001).

### 3.5 Fixed-point geometry

All Feature Model coordinates are `FixedPoint` values: signed 32-bit integers in **1/64 pixel** units, in board pixel space (origin at the top-left pixel corner, y down). Compilation uses integer arithmetic only (section 5.3), so compiled grids are identical on every platform. Geometry constants such as 56.25 and 64.5 are exact in this unit (3600 and 4128).

### 3.6 Geometry for new boards

`BoardGeometry.Standard(widthHexes, heightHexes)` uses the standard hex (56.25 by 64.5, A1 center at (0, 32.25), 10/11 column parity), with:

- `gridWidth = (widthHexes - 1) * 56.25`, rounded up;
- `gridHeight = heightHexes * 64.5`, rounded up.

`Standard(33, 10)` reproduces 1800 by 645 exactly. Other geometries (ASL-MAP-024 composition, the deferred VASL variants) are further `BoardGeometry` instances; every algorithm takes geometry as input.

## 4. Terrain Grid and Hex Facts

### 4.1 TerrainGrid

```text
TerrainGrid
  Geometry        BoardGeometry
  Codes           byte[gridWidth * gridHeight]    column-major, index = x * gridHeight + y
  Elevations      sbyte[gridWidth * gridHeight]   same order
  Stairways       bit set over hex indices
```

The grid holds no catalog reference; the catalog a board's codes refer to is recorded in its provenance (the `SharedBoardMetadata.xml` blob for ingested boards) and in the board package. The grid is immutable. Edits produce a new grid that shares unchanged column blocks. Column-major order matches `LOSData`, so VASL encoding and decoding are copies (Ingestion Design section 4.2).

### 4.2 Catalog

Authored boards use the same pinned VASL `TerrainCatalog` as ingested boards, identified by its hash. This keeps every authored board exportable to VASL format (ASL-MAP-056). Version 1 adds no LimboDancer-specific terrain codes. A later extension would need a separate code space and a review, because it would break VASL export.

### 4.3 HexFacts

`HexFactSet` is the output of the VASL-compatible derivation (Ingestion Design section 7.3): one `HexFacts` per hex, with center terrain, depression terrain, base level, stairway, level locations, six hexside facts, bridge, and derivation trace. It is identified by `(BoardVersion, DerivationVersion)` and is never edited (ASL-MAP-012).

### 4.4 Grid outlines

The Terrain Grid is never shown to users directly (ASL-MAP-064). `GridOutlineTracer.Trace(grid) -> GridOutlines` converts it into a lossless vector form for the Exact view and for the vectorizer's tracing stages (section 7.3):

- **Regions:** one region per 4-connected component of cells with the same `(terrain code, elevation)`. Each region is a set of closed rings along pixel edges: an outer ring and zero or more holes, with integer vertices at pixel corners.
- **Vertices:** only at turns; collinear pixel-edge steps are merged.
- **Order:** regions are sorted by `(code, elevation, top-left cell in column-major order)`. Rings start at their top-left vertex and run clockwise for outer rings and counterclockwise for holes. The output is therefore canonical.
- **Losslessness:** filling the outlines under the coverage rule of section 5.3 reproduces the grid exactly. Tests check this for synthetic and random grids and, with a VASL checkout, for board 01 through the rendered Exact paths. Since ASL-MAP-05, `GridOutlineVerifier` checks it for every ingested board in each fidelity batch, for both the terrain and the elevation outlines.

Measured sizes (ASL-MAP-04): board 01 traces to 132 regions and 47,240 turn vertices. Board 04, whose dithered grain is the worst case measured, traces to 6,924 regions. Tracing takes 140 to 390 ms per board.

Outlines are cached per `BoardVersion` and are recomputed only for the dirty region after an edit (section 5.5).

## 5. Feature Model

### 5.1 Structure

```text
FeatureModel
  Geometry          BoardGeometry
  CatalogHash
  Base              { terrain: Open Ground, elevation: 0 }
  Features          ordered list of Feature
  HexAnnotations    stairways, slopes, railroad embankments, partial orchards
  Provenance        authored | vectorized(source BoardVersion, vectorizer version)
```

Every feature has a stable `FeatureId` (ULID), a `Layer` (integer paint order within its kind), an optional `BuildingId` or `SceneInstanceId`, and optional author notes.

### 5.2 Feature kinds

| Kind | Geometry | Carries | Compiles to |
|---|---|---|---|
| `ElevationRegion` | polygon with holes | `Level` (integer) | elevation of covered pixels |
| `AreaTerrain` | polygon with holes | area terrain code (woods, brush, grain, orchard, marsh, water, graveyard, crags, rubble, and so on) | terrain code of covered pixels |
| `LinearTerrain` | centerline path of line and cubic Bézier segments, plus width | linear code (dirt, paved, sunken, or elevated road; path; track; streams; gully; railroads; runway) | terrain code under the stroked path |
| `Building` | one or more footprint polygons | building code (material, levels, factory, marketplace), `BuildingId` | terrain code of covered pixels |
| `HexsideTerrain` | a set of canonical `HexsideRef`s, not free geometry | hexside code (wall, hedge, bocage, cliff, rowhouse wall variants) and optional per-hexside extent (0 to 1 along the side, default whole side) | stroke of standard width centered on each hexside |
| `Bridge` | polygon | bridge code | terrain code of covered pixels |
| `FidelityPin` | small polygon, normally a few pixels | any code and optional elevation | covered pixels; see section 7.4 |

Buildings are authored with a footprint kit whose outputs are ordinary polygons:

- **Centered box:** a rectangle centered on a hex.
- **Span box:** touches two opposite hexsides of a hex along one of the three hex axes.
- **Flush box:** touches one named hexside.
- **Free polygon.**

A footprint that is meant to join a neighboring hex's footprint must cover the edge sample points of the shared hexside. The kit's span and flush shapes do this by construction, extending one pixel past the hexside. This is how multi-hex building connections become visible to the derivation.

Rowhouse partitions are `HexsideTerrain` features using the rowhouse wall codes, drawn over the building's shared hexsides.

`HexAnnotations` hold facts that VASL keeps outside the pixel grid: stairway flags per hex, and slopes, railroad embankments, and partial orchards per hexside, with the same meaning as in `BoardMetadata.xml`.

### 5.3 Compiler

`FeatureCompiler.Compile(model) -> TerrainGrid` runs these stages in order:

1. **Base:** fill all cells with the base terrain and elevation.
2. **Elevation:** paint `ElevationRegion`s in ascending `Level`, then by `Layer`. Later paint wins.
3. **Area terrain:** paint `AreaTerrain` by `Layer`.
4. **Linear terrain:** stroke `LinearTerrain` by `Layer`. Default layers put depressions (gullies, streams) below roads and railroads, and roads below bridges.
5. **Bridges.**
6. **Buildings:** by `Layer`.
7. **Hexside terrain:** stroke each hexside segment with the standard width of 6 pixels (configurable per catalog code, minimum 3), centered on the hexside and clipped to the given extent.
8. **Fidelity pins:** by `Layer`.
9. **Post-passes**, reproducing `LOSDataEditor` conventions (finding 5), in its order:
   1. cliff pixels take the lower of the two adjacent hexes' base levels;
   2. exterior factory walls;
   3. depression-category pixels: elevation minus one;
   4. sunken road pixels: Elevated Road at elevation 1 in hexes whose base level is 1, otherwise elevation -1.
   
   Base levels for passes 1 and 4 come from an intermediate derivation of the grid produced by stages 1 to 8, exactly as `LOSDataEditor` calls `resetHexTerrain` between its passes.
10. **Stairways:** copied from `HexAnnotations`.

**Cell coverage rule.** A cell `(x, y)` is covered by a shape when its center `(x + 0.5, y + 0.5)` lies inside the shape under the nonzero winding rule. There is no anti-aliasing. Curves are flattened with a fixed subdivision rule (section 5.4). Strokes are converted to polygons (butt caps on hexside strokes; round joins and caps on linear terrain) before filling. Coverage is computed with a scanline algorithm over 64-bit integer edge arithmetic.

### 5.4 Determinism

- Bézier flattening subdivides each cubic segment into a fixed count derived from its control-polygon length in fixed-point units, using integer de Casteljau steps.
- Features are processed by `(stage, Layer, FeatureId)`, never by hash or insertion order.
- Output grids are compared and hashed as bytes.

A property test compiles every test model twice in fresh processes and requires identical hashes. The CI matrix runs it on Windows x64 and Linux x64.

### 5.5 Incremental compilation

Every feature has a pixel bounding box. After an edit, the compiler recomputes the **dirty region**: the union of the old and new bounding boxes of changed features, expanded by:

- 1 pixel for the factory wall pass;
- the full extent of every hex whose base level may change, for the cliff and sunken road passes.

It recompiles only that region, starting from the previous grid. Derivation is then rerun for every hex whose sample points or border scan intersect the dirty region, plus their neighbors, because of hexside propagation (Ingestion Design section 7.1, step 11).

A property test applies random edit sequences and requires the incremental grid and Hex Facts to equal a full recompile after every edit. Live editing (ASL-MAP-052) depends on this.

## 6. Validation

`BoardValidator` runs over the Feature Model, the compiled grid, and the Hex Facts. It reports findings and never changes the model (ASL-MAP-053).

| Code | Severity | Rule |
|---|---|---|
| `MAP-VAL-001` | error | Linear terrain enters a hexside that the adjoining hex does not continue, other than at a board edge, a declared dead end, or a building. |
| `MAP-VAL-002` | error | Stream or water network disconnected where the author marked it continuous. |
| `MAP-VAL-003` | error | Hexes sharing a `BuildingId` are not contiguous through covered shared hexsides. |
| `MAP-VAL-004` | error | Hexes sharing a `BuildingId` derive different building codes. |
| `MAP-VAL-005` | warning | Stairway flag on a hex whose center terrain is not a multi-level building. |
| `MAP-VAL-006` | error | Bridge with no depression or water under it, or no road or path continuing from both ends. |
| `MAP-VAL-007` | warning | Fragile sample: a feature boundary passes within 1 pixel of a derivation sample point (center probes, the four 5-pixel probes, or edge sample points). A small edit could change the derived fact. |
| `MAP-VAL-008` | warning | Hex Fact differs from the author's stated intent for that hex (see below). |
| `MAP-VAL-009` | warning | Geomorphic edge incompatibility: a board meant to abut standard boards has non-open terrain on a half hex of its long edges that would merge unexpectedly under VASL's seam rule, or a linear feature meets the edge between standard crossing points. |
| `MAP-VAL-010` | error | Terrain code not in the catalog, or not valid for the feature kind (for example, a building code in `AreaTerrain`). |
| `MAP-VAL-011` | warning | `ElevationRegion` levels not nested (a level L+2 region not inside a level L+1 region). |
| `MAP-VAL-012` | info | `FidelityPin` present (section 7.4). |

**Author intent** is optional. An author may assert an expected center terrain, base level, or hexside terrain for a hex. The assertion is stored in the Feature Model, never used by derivation, and checked by `MAP-VAL-008`. This is how an author confirms that a drawing means what they think it means, without letting anyone write Hex Facts.

## 7. Vectorizer

### 7.1 Purpose and contract

`Vectorizer.Vectorize(ingestedBoard, hexFacts) -> FeatureModel` produces an editable approximation of an ingested board (ASL-MAP-015). Its contract is F3: compiling its output must reproduce the source board's Hex Facts exactly, and must meet the pixel metrics in section 7.5. The output's provenance records the source `BoardVersion` and the vectorizer version.

The vectorizer has two consumers:

- **authoring**, as the starting point for an original map (ASL-MAP-055);
- **the Styled view** of every ingested board (ASL-MAP-060).

Because of the second, it runs for every board a user opens in Styled view, not only for boards being authored. Its output is cached per `(BoardVersion, vectorizer version)`.

### 7.2 Stages

1. **Reverse the post-passes.**
   - Add one to the elevation of depression-category pixels (finding 2).
   - Record sunken and elevated road pixels as `LinearTerrain` codes as they stand.
   - Record factory wall pixels as belonging to their factory.
   
   After this stage, compiling the output reapplies the passes and returns the original values.
2. **Hexside terrain.** For each hexside whose derived facts record hexside terrain, emit a `HexsideTerrain` ref with that code. The extent is estimated from the pixel run along the hexside. Mask those pixels from later stages.
3. **Buildings.** Take connected components of building-code pixels per code, trace their outer boundaries, and simplify (section 7.3).
   - Components that cover the edge samples of a shared hexside get the same `BuildingId`.
   - Stairway flags become `HexAnnotations`.
   - Rowhouse walls were already taken in stage 2.
4. **Linear terrain.** In version 1, road, path, track, stream, gully, and railroad pixels are emitted as polygons: a `LinearTerrain` with an explicit outline in place of a centerline. This is exact enough for F3 and simple.
   - Centerline and width recovery by skeletonization is planned for a later version, so that vectorized roads edit like drawn ones. It must not lower F3.
5. **Area terrain.** Per code, take connected components, trace pixel-edge boundaries, and simplify. For **dithered** codes (section 7.5), apply a morphological closing before tracing, so the output is one solid region per field instead of thousands of specks.
6. **Elevation.** For each level L above the minimum, take the connected components of pixels with elevation at least L, trace them, simplify, and emit nested `ElevationRegion`s.
7. **Annotations.** Copy slopes, railroad embankments, and partial orchards from the source metadata into `HexAnnotations`.
8. **Fidelity loop** (section 7.4).

### 7.3 Boundary tracing and simplification

Boundaries come from `GridOutlineTracer` (section 4.4), which gives an exact outline of each component. Simplification is topology-preserving Douglas-Peucker with tolerance 1.5 pixels. It keeps shared boundaries between adjacent regions consistent, so simplification never opens gaps or overlaps between neighbors.

Tolerance is reduced locally, down to 0 (the exact pixel outline), wherever the fidelity loop needs it.

### 7.4 Fidelity loop and fidelity pins

After stages 1 to 7, the vectorizer compiles its output, derives Hex Facts, and compares them with the source. For each mismatch:

1. reduce the simplification tolerance for the boundaries within 6 pixels of the failing sample point, and repeat;
2. if tolerance 0 still fails, which is expected where dithering placed an Open Ground pixel at a grain hex's center sample (finding 4), emit a `FidelityPin`: the minimal pixel set around the failing sample point, with the source code and elevation.

The loop ends when the facts match, or after 8 iterations with `MAP-VEC-001` listing the unresolved hexes.

Fidelity pins reproduce VASL artifacts on purpose. They are visible in the Studio (`MAP-VAL-012`) and can be removed. Removing one makes the board diverge from VASL, which is acceptable only for an original map derived from it (ASL-MAP-055).

### 7.5 F3 metrics and thresholds

F3 (ASL-MAP-043) is measured by compiling the vectorized model and comparing it with the source grid.

| Metric | Initial threshold |
|---|---|
| Hex Facts | identical for every hex and hexside (hard requirement) |
| Terrain code agreement over all cells | at least 99.0% |
| Elevation agreement over all cells | at least 99.5% |
| Intersection over union, per code with at least 2,000 source pixels, non-dithered codes | at least 0.95 |
| Intersection over union, dithered codes | reported, not gated |
| Fidelity pins | reported (count and area) |

A code is **dithered** when fewer than 50% of its source pixels have all four neighbors in the same code. On the measured boards, grain falls between 25% and 29%, and every other code with material area falls between 76% and 96%.

The thresholds are initial values, to be calibrated on board 01 in ASL-MAP-06 and then on the batch. They may be tightened without review. Loosening one requires a recorded review decision.

## 8. Scenes and composition

### 8.1 Scenes

A `Scene` is a named Feature Model fragment with its own local hex grid and an anchor hex (ASL-MAP-054). Placing a scene:

- copies its features into the board with new `FeatureId`s and a shared `SceneInstanceId`;
- records the scene name, version, anchor, and transform in provenance;
- does not link the board to later changes in the scene. Re-placing a scene is an explicit author action.

**Transforms.** Rotations are defined in **lattice space**, not pixel space, because of the irregular hex (finding 6). A point is mapped to fractional axial hex coordinates by the affine map implied by the geometry, rotated by a multiple of 60 degrees about the anchor there, and mapped back. This takes hex centers, vertices, and hexsides exactly onto hex centers, vertices, and hexsides. Shapes are sheared slightly, by about 0.7%.

- Rotation by 180 degrees in lattice space is also an exact pixel-space rotation.
- Translation is by whole hexes. Column-parity offsets are handled by the lattice mapping.
- `HexsideTerrain` refs and `HexAnnotations` are transformed as hexside and hex indices, not as pixels.

### 8.2 Board composition

A `ComposedMap` is a list of `BoardPlacement(boardVersion, columnOffset, rowOffset, rotation 0 or 180)` (ASL-MAP-024).

- Standard boards abut so that edge half hexes combine into whole hexes.
- The composed grid follows VASL's seam rule from `BoardArchive.addLOSDatatoVASLMap`: where half hexes overlap, non-open terrain wins over open.
- Composed Hex Facts are derived from the composed grid.
- Location text for composed maps uses the placed board's `BoardRef`, for example `bd01:E4:0`. The composition maps it to composed-grid coordinates.

Version 1 defines these types and their tests but does not expose composition in the Studio (sequence ASL-MAP-08).

## 9. Canonical board package

### 9.1 Contents

A board package is a set of named entries:

| Entry | Content | Present for |
|---|---|---|
| `board.json` | manifest: `BoardRef`, name, geometry, catalog hash, provenance, metadata, validation summary, entry hashes | all |
| `features.json` | Feature Model | authored and vectorized boards |
| `grid.bin` | grid in the canonical binary form (section 9.2) | authored boards (cache); ingested boards only outside the repository |
| `hexfacts.json` | Hex Facts with derivation version | optional cache |
| `source.json` | VASL source reference: provenance record and hashes, in place of the grid | ingested boards in the repository |

For an ingested VASL board, the committed form carries `source.json` in place of `grid.bin` (ASL-MAP-073). The grid is regenerated from the configured local VASL source, and loading fails with a hash mismatch diagnostic if the source differs.

### 9.2 Canonical encodings

- **JSON:** RFC 8785 (JSON Canonicalization Scheme). Coordinates are fixed-point integers, so no floating-point formatting is involved except geometry constants, which are serialized as strings in invariant decimal form.
- **Grid:** magic `ASLR`, format version, geometry fields as big-endian integers, then codes, elevations, and stairway bits in the order of section 4.1. The canonical form is uncompressed. Containers may compress it, but hashes are always taken over the uncompressed bytes, because compressor output varies between runtimes.

### 9.3 Identities

- **Entry hash:** SHA-256 of the canonical entry bytes.
- **BoardVersion:** SHA-256 over the sorted list of `(entry name, entry hash)` for the defining entries only: `board.json` defining fields plus `features.json` for authored boards, or plus `source.json` (or the grid) for ingested boards. Caches (`hexfacts.json`, an authored board's `grid.bin`) are excluded, so regenerating a cache never changes identity.
- Any change to features, source, catalog, or geometry produces a new `BoardVersion` (ASL-MAP-072).

### 9.4 Containers

A package may be stored as a directory or as a zip with the same entry names. Identity does not depend on the container. Authored boards are committed under `src/ASL/boards/<slug>/` as directories, which keeps diffs reviewable. Authored boards that start from a vectorized VASL board are not committed until ASL-MAP-074 allows it.

## 10. Board status

| Status | Meaning | Consumer treatment |
|---|---|---|
| `Ingested` | decoded without error diagnostics | nondefinitive |
| `Verified` | ingested, F1 and F2 pass for this exact source | definitive for this `BoardVersion` |
| `Authored` | authored board with validation errors | nondefinitive |
| `AuthoredValid` | authored board with no validation errors | definitive for this `BoardVersion`; the Feature Model is the source of truth |
| `Vectorized` | vectorized from a verified board, F3 result attached | informational; consumers use the source board, not the vectorization |

Status is computed, not stored as an editable field. It is recomputed on load from diagnostics, fidelity results, and validation.

## 11. Read API for consumers

`LimboDancer.Domains.Asl.Maps` exposes a read-only surface for observation providers (ASL-MAP-080):

```text
IBoardCatalog
  TryGetBoard(BoardRef, BoardVersion?) -> BoardHandle | diagnostics

BoardHandle
  Ref, Version, Status, Provenance, Geometry
  Resolve(BoardLocation) -> LocationFacts | diagnostics
  GetHexFacts(HexName) -> HexFacts
  Neighbor(HexName, side) -> HexName?
  Distance(HexName, HexName) -> int
  Grid  (read-only view, for a later LOS executor; ASL-MAP-082)
```

`LocationFacts` carries the Hex Facts for the hex and level, the `BoardVersion`, the status, and an evidence reference suitable for an `Observation`.

A consumer that receives a nondefinitive status must not produce a definitive conclusion from terrain facts (ASL-MAP-044, ASL-RD-011). This API has no ASL runtime dependency. Wrapping it in `IObservationProvider` implementations is the ASL package's job, as `ScenarioA1BoardObservationProvider` does today.

**Scenario A1** keeps `Board01TerrainCatalog` unchanged (ASL-MAP-081). A later reviewed change may back it with `IBoardCatalog` for `bd01`. That change must reproduce the 63 overrides through the Ingestion Design's override consistency check and pass the existing Scenario A1 tests.

## 12. Authoring operations

Authoring is a set of commands over the immutable Feature Model:

- add, update, or delete a feature;
- reorder layers;
- set or clear hex annotations;
- place a Scene;
- vectorize a verified board into a new authored board (ASL-MAP-055).

Each command produces a new model version and an inverse command, for undo. Commands are validated for type correctness before they are applied. For example, a `HexsideTerrain` command must name existing hexsides and a hexside code.

A new board starts from `BoardGeometry.Standard(33, 10)` or explicit dimensions (ASL-MAP-050), base Open Ground at level 0, and an empty feature list. The Studio design defines how commands are produced from pointer interaction.

## 13. Tests

| Area | Tests |
|---|---|
| Coordinates | parse and format round trips; canonical hexside normalization; board-set resolution and its diagnostics |
| Fixed point | geometry constants exact; conversions stable |
| Compiler | per-stage golden grids on small synthetic boards; post-pass parity with the `LOSDataEditor` rules; determinism across processes and operating systems |
| Incremental compile | random edit sequences equal full recompile, for grid and Hex Facts |
| Validation | one fixture per rule, positive and negative |
| Grid outlines | filling traced outlines reproduces the grid exactly, for synthetic boards and every ingested board; canonical ordering stable |
| Vectorizer | synthetic boards with known features; board 01 F3 (local VASL, skipped when absent); batch F3 metrics report |
| Scenes | lattice transforms map centers, vertices, and hexsides exactly; 180 degree rotation equals pixel rotation |
| Packages | canonical JSON and grid encodings stable; `BoardVersion` excludes caches; ingested packages reject a changed source |
| Read API | location resolution, status propagation, and nondefinitive handling |

## 14. Open issues

1. **Hexside stroke width.** Calibrate the standard 6-pixel width against the measured VASL range (4 to 17 pixels), and decide whether LOS behavior later needs per-code widths.
2. **Dithered terrain and LOS.** Solid grain regions in authored boards will produce more hindrance pixels on an LOS trace than VASL's dithered grain. This does not affect Hex Facts, but it affects the later Scenario B executor. Decide then whether authored grain should be dithered by the compiler.
3. **Centerline recovery** for vectorized linear terrain (section 7.2, stage 4).
4. **Seam rule details** for composition and rotated boards, pinned by oracle fixtures from VASL multi-board maps once composition is implemented.

## 15. Requirement coverage

| Requirement | Section |
|---|---|
| ASL-MAP-010, 011 | 1, 4.1 |
| ASL-MAP-064 | 4.4 |
| ASL-MAP-012 | 4.3 |
| ASL-MAP-014 | 5 |
| ASL-MAP-015 | 7 |
| ASL-MAP-020, 023 | 3.6; Ingestion Design 5 |
| ASL-MAP-021, 022 | 3 |
| ASL-MAP-024 | 8.2 |
| ASL-MAP-043 | 7.5 |
| ASL-MAP-044 | 10, 11 |
| ASL-MAP-050, 051 | 5, 12 |
| ASL-MAP-052 | 5.5 |
| ASL-MAP-053 | 6 |
| ASL-MAP-054 | 8.1 |
| ASL-MAP-055 | 7.4, 12 |
| ASL-MAP-056 | 4.2, 9 |
| ASL-MAP-070, 072 | 9 |
| ASL-MAP-073, 074 | 9.1, 9.4 |
| ASL-MAP-080, 081, 082 | 11 |
