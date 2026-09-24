# LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements

**Status:** Proposed

**Capability authority:** Normative for the ASL map model, VASL board ingestion, original map authoring, and the Map Studio application delivered under `LimboDancer.Domains.Asl.sln`.

**Architecture authority:** Normative only for the package boundaries and canonical-model decisions recorded in sections 3 and 4. Detailed formats, algorithms, and UI design belong to the design documents listed in section 12.

**Parent requirements:** This document refines [ASL-RD-001, ASL-RD-005, ASL-RD-006, ASL-RD-010, ASL-RD-011, ASL-RD-013, and ASL-RD-015](<LimboDancer.Agentic.CognitiveRuntime ASL Reference-Domain Requirements.md>) for board and map data. It does not change those requirements or admit any runtime contract.

**Supersedes:** The map and renderer material in [`docs/ASL/MapGenerators/`](../../../docs/ASL/MapGenerators/) and the legacy `src/_Legacy/Samples/ASL/AslHexMap` application. Both remain reference material only; no code or data format is carried forward from them.

## 1. Purpose

LimboDancer needs board and terrain reference state it can trust. The first reference-domain slice (Scenario A1) uses 63 building overrides from VASL board 01 metadata and supplies adjacency and hexside facts as test inputs. Spatial adjudication (Scenario B) and every later case that depends on printed terrain need more: complete, versioned, verifiable board data.

This effort delivers that data and the tooling around it in two stages:

1. **Faithful recreation.** Ingest VASL board archives and reproduce each board's terrain exactly enough that the result can be verified against VASL itself.
2. **Original authoring.** Use the same model and tooling to author new, original maps that are as rule-complete as an ingested VASL board.

The second stage depends on the first. Authoring is trustworthy only if the pipeline that authored maps pass through has first been shown to reproduce known boards.

## 2. Source facts that shape these requirements

These facts were established by reading the VASL source (`vasl-developers/vasl`, local checkout at commit `33324f9ad`) and decoding board 01. They are recorded here because the canonical-model decision in section 4 follows from them. The VASL Board Ingestion Design will specify them precisely.

- A VASL board archive is a zip containing `LOSData`, `BoardMetadata.xml`, the board image, and optional legacy `data` and `SSRControls` files. Module-wide terrain definitions live in `SharedBoardMetadata.xml`.
- `LOSData` is a gzip-compressed Java object stream written with `writeInt` and `writeByte` in block-data mode. It holds the board size in hexes, the grid size in pixels, then **one elevation byte and one terrain-code byte for every pixel**, then one stairway flag for every hex. Board 01 decodes to 33 by 10 hexes, a 1800 by 645 grid, and 346 hex records with 34 stairways.
- `SharedBoardMetadata.xml` defines 181 terrain types (a 182nd element is commented out). Each has a numeric code, name, LOS obstacle and hindrance flags, half-level flag, inherent-terrain flag, height, LOS category, and map color.
- VASL treats the pixel grid as ground truth. Hex terrain, hexside terrain, and building levels are **derived** from it (`VASL.LOS.Map.Hex.resetTerrain`): center terrain is sampled at the hex center with building fallbacks, and hexside terrain is sampled at edge midpoints with an opposite-hex fallback. `BoardMetadata.xml` building types, which color alone cannot distinguish, are written into the grid when VASL creates `LOSData`.
- Roads, bridges, railroads, walls, hedges, and rowhouse walls are terrain codes in the grid, not vector features.
- The VASL checkout contains 265 `BoardMetadata.xml` files (264 are well-formed XML). 180 declare 33 by 10 hexes, 177 of those have `LOSData`, and 157 use only standard geometry. ASL-MAP-02 ingests 156 of those 157 and every one passes F1; `bdLFT1` declares a non-standard 644-row grid.
- ASL line of sight is traced across depicted terrain (A6), not across hex centers. A hex-only model therefore cannot support true LOS.

## 3. Package boundary

### ASL-MAP-001: Separate ASL projects

All map capability must be delivered as ASL domain-package projects under `src/ASL/` and added to `LimboDancer.Domains.Asl.sln`:

| Project | Responsibility |
|---|---|
| `LimboDancer.Domains.Asl.Maps` | Board geometry, coordinates, terrain catalog, terrain grid, hex-fact derivation, lossless grid-outline tracing, feature model, compiler, and vectorizer. No UI, file-system policy, or VASL-specific parsing. |
| `LimboDancer.Domains.Asl.Maps.Vasl` | VASL board archive reading, `LOSData` decoding, metadata parsing, and provenance capture. |
| `LimboDancer.Domains.Asl.Maps.Rendering` | Deterministic server-side SVG generation from any model layer. |
| `LimboDancer.Domains.Asl.MapStudio` | Blazor Web App for browsing, viewing, comparing, and authoring boards. |

Each library must have a corresponding test project under `src/ASL/tests/`.

### ASL-MAP-002: Runtime isolation

No map project may be referenced by `src/LimboDancer`. The runtime must not gain hex, board, terrain, or map vocabulary. Map data reaches the runtime only through existing domain-neutral contracts, such as an ASL `IObservationProvider`, as required by ASL-RD-015 and the Domain Integration Model.

### ASL-MAP-003: UI independence of the model

`Maps`, `Maps.Vasl`, and `Maps.Rendering` must not reference Blazor or ASP.NET Core. Observation providers, command-line tools, and tests must be able to ingest, derive, and render boards without the Studio.

### ASL-MAP-004: Rewrite, not port

The implementation must be new code. Concepts from the MapGenerators prototypes and the legacy sample (flat-top hex geometry, linear-feature curves, the building footprint kit, Template and Scene composition) may inform the design, but their code and their JSON formats must not be reused.

## 4. Canonical model

### ASL-MAP-010: Layered model

The map model must consist of four layers with one direction of authority:

```text
Feature Model  --compile-->  Terrain Grid  --derive-->  Hex Facts
      ^                          |
      +-------vectorize----------+   (approximate; ingested boards only)

Rendering reads any layer and never writes one.
```

### ASL-MAP-011: Terrain Grid is canonical

The Terrain Grid is the canonical, comparable representation of a board's terrain. It must hold, for each grid cell, a terrain code and an elevation, together with board geometry and per-hex stairway flags. It must be able to represent every terrain code and elevation that VASL `LOSData` can represent.

### ASL-MAP-012: Hex Facts are always derived

Hex Facts (center terrain, hexside terrain, base elevation, building type and levels, stairwell, and depression and hillock status where applicable) must be produced only by a single deterministic derivation from the Terrain Grid and board metadata. No editor, importer, or consumer may set a Hex Fact directly.

### ASL-MAP-013: Derivation reproduces VASL

The derivation must reproduce VASL's hex-fact derivation for ingested boards, including sampling points, building fallbacks, opposite-hexside fallback, slopes, railroad embankments, and partial orchards. VASL bakes metadata building-type overrides into `LOSData` when it creates the file, so ingestion checks them for consistency with the grid rather than applying them (see the VASL Board Ingestion Design). Where VASL behavior is a known defect or board-specific hack, the design must record the decision to reproduce or diverge, and any divergence must be reported by fidelity tests.

### ASL-MAP-014: Feature Model is the authoring layer

The Feature Model must be able to describe terrain regions, linear features, hexside features, building footprints and building properties, and elevation. It compiles to a Terrain Grid through a deterministic compiler. Every terrain code reachable in the grid must be reachable from the Feature Model.

### ASL-MAP-015: Vectorization is explicitly approximate

Converting an ingested grid into a Feature Model must be labelled approximate. A vectorized Feature Model must record its source grid identity. It must never replace the ingested grid as the board's canonical form.

## 5. Geometry and coordinates

### ASL-MAP-020: Board geometry

The model must represent board geometry explicitly: size in hexes, grid size, hex width and height, A1 center offset, column parity rule (VASL geomorphic boards have 10 hexes in even-indexed columns and 11 in odd-indexed columns, including half hexes), and any alternate grid configuration declared by metadata.

### ASL-MAP-021: Canonical coordinates

The model must provide a canonical coordinate type covering board identity, column letters (A through GG and beyond), row (including row 0 and final-row half hexes), hexside index, and level. Parsing must accept the published forms used in rules and scenarios, such as `1E4`, `36AA8`, and `E4` with an explicit board context. Aliases must not replace canonical identity (ASL-RD-001).

### ASL-MAP-022: Location identity compatibility

The canonical location identity must be compatible with the `bd01:<hex>:<level>` form already used by `Board01ValidatedSnapshotSource`, or the design must define a lossless mapping to it. The same form is used for `LocationId` and `PreviousLocationId` in the Scenario A1 second-defender return path and its journal (for example `bd01:D4:0`). ASL-MAP-01 must include a test that every location string used by the Scenario A1 providers, packages, and execution tests parses to a canonical `BoardLocation` and formats back to the identical string.

### ASL-MAP-023: Geometric relations

The model must provide hex center and vertex positions, hexside midpoints, neighbor and hexside adjacency (including the opposite hexside), hex distance, and the mapping between grid cells and hexes. These are prerequisites for ASL-RD-006 and must be deterministic and independently tested.

### ASL-MAP-024: Placement transforms and composition

The model must represent board placement: position in a composed map, 180 degree rotation (inverted boards), and multi-board composition with shared half-hex edges. Version 1 must ingest and render single boards, but the geometry types must not assume a single unrotated board.

## 6. VASL ingestion

### ASL-MAP-030: Configured local source

The Studio and tools must read VASL boards from a configured local VASL checkout or boards directory. They must not download boards from the network.

### ASL-MAP-031: Archive and source-directory input

Ingestion must accept both packaged board archives (`boards/bdFiles/bdNN`) and unpacked source directories (`boards/src/bdNN`), and must produce identical results for the same board version.

### ASL-MAP-032: LOSData decoding

Ingestion must decode `LOSData` fully, including gzip, the Java object-stream header, block-data records of both lengths, the board header, the elevation and terrain grids in VASL's column-major order, and stairway flags with per-column row counts. Malformed or truncated data must fail with a specific diagnostic, never a partial board.

### ASL-MAP-033: Metadata parsing

Ingestion must parse `BoardMetadata.xml` (name, version, version date, author, image name, `hasHills`, size, grid configuration, building-type overrides, slopes, railroad embankments, partial orchards) and the terrain-type catalog from `SharedBoardMetadata.xml`. Unrecognized elements must be preserved as diagnostics, not silently dropped.

### ASL-MAP-034: Terrain catalog

The terrain catalog must be loaded from `SharedBoardMetadata.xml` with its codes, names, flags, heights, LOS categories, and colors. Terrain codes in a grid that are absent from the catalog must be reported and must block a board from being marked verified.

### ASL-MAP-035: Version 1 scope

Version 1 must ingest board 01 first and then every standard 33 by 10 geomorphic board that has `LOSData`. It must report, without failing the batch, boards it cannot ingest and why.

### ASL-MAP-036: Deferred VASL features

Overlays, SSR terrain transforms (for example `NoRoads` and `LightWoods`), legacy V5 `data` files, and non-geomorphic boards are out of version 1 scope. The model and ingestion design must identify how each will be added without changing the canonical model.

## 7. Fidelity

Fidelity is defined as three independently testable levels.

### ASL-MAP-040: F1 Grid fidelity

For every ingested board, re-encoding the decoded model must reproduce the decompressed `LOSData` payload byte for byte. Gzip container bytes are excluded because they depend on the compressor.

### ASL-MAP-041: F2 Hex-fact fidelity

For every ingested board in the verified set, derived Hex Facts must equal the facts VASL itself computes for the same board version. The comparison must cover every hex and every hexside, and report each difference by coordinate and field.

### ASL-MAP-042: F2 oracle

F2 must be verified against an oracle produced by VASL's own code: a small Java harness, built against a pinned VASL commit, that loads a board through VASL's `Map` and `Hex` classes and writes Hex Facts as canonical JSON. Oracle fixtures must record the VASL commit, board archive hash, and harness version. The harness is a development tool and must not be a runtime or Studio dependency.

### ASL-MAP-043: F3 Authoring round-trip fidelity

For every board in the verified set, vectorizing the ingested grid and compiling the resulting Feature Model must reproduce identical Hex Facts and must reach a pixel agreement threshold defined in the model design. F3 is the acceptance gate for original authoring: authoring is not considered ready until F3 passes for board 01.

### ASL-MAP-044: Verified status

A board may be marked verified only when F1 and F2 pass for its exact source version. Consumers such as observation providers must be able to tell verified boards from unverified ones, and must treat unverified terrain as nondefinitive evidence (ASL-RD-011).

## 8. Authoring

### ASL-MAP-050: New boards

The Studio must let a user create a new board from a geometry preset (standard geomorphic 33 by 10 by default) or explicit dimensions.

### ASL-MAP-051: Feature editing

The Studio must support creating, editing, and deleting:

- terrain regions (woods, brush, grain, marsh, orchard, water, and every other area terrain in the catalog);
- linear features (roads by subtype, paths, streams by depth, railroads by elevation, gullies), with their hexside crossings;
- hexside features (walls, hedges, bocage, cliffs, rowhouse walls);
- buildings, using a footprint kit (centered, spanning, and hexside-flush footprints, and free polygons), with material, levels, stairwells, multi-hex building identity, and rowhouse partitions;
- elevation levels, hills, and depressions.

### ASL-MAP-052: Live compile and derive

Edits must recompile the affected grid region and re-derive affected Hex Facts, so that the user always sees the rule-relevant consequence of an edit.

### ASL-MAP-053: Validation

Authoring validation must report at least: linear-feature continuity across hexsides, stream and water connectivity, multi-hex building contiguity and consistent properties, stairwell placement, orphan bridges, terrain codes that compile ambiguously at hex centers or hexside midpoints, and geomorphic edge compatibility for boards meant to abut standard boards. Validation findings must not silently alter the model.

### ASL-MAP-054: Reusable scenes

The model should support reusable Scenes: named Feature Model fragments that can be placed on a board with an anchor hex and rotation. Placement copies features into the board and records provenance.

### ASL-MAP-055: Starting from an ingested board

A user may start an original map from a vectorized copy of an ingested board. The result must be a new board identity whose provenance records the source board and version, and it must not be presented as the VASL board.

### ASL-MAP-056: Export

The Studio must export an authored board as a canonical board package (ASL-MAP-070). It should also be able to export any rendering view as an SVG document. Export of a VASL-compatible archive (`LOSData` and `BoardMetadata.xml`) may be added later and must be validated by F1 against the exported data.

## 9. Rendering and Studio

### ASL-MAP-060: Rendering modes

Rendering must provide at least:

1. **Exact view:** the Terrain Grid traced losslessly into SVG regions and drawn in catalog terrain colors, so that every rendered cell matches the grid exactly.
2. **Styled view:** an SVG rendering from the Feature Model that resembles a printed board, with woods and building textures, footprints, stairwell squares, rowhouse bars, linear features, hexside features, and elevation shading. For an ingested board, the Feature Model is the vectorizer's output.
3. **Hex-fact view:** the hex grid annotated with derived Hex Facts.
4. **Comparison view:** the Exact and Styled views of the same board shown together, with differences between them highlighted by hex and hexside.

### ASL-MAP-061: Deterministic rendering

For the same model, options, and renderer version, rendering must produce byte-identical SVG, so that rendered output can be used as test evidence.

### ASL-MAP-062: Studio capabilities

The Studio must provide a board browser for the configured VASL source and for authored boards, per-board ingestion and fidelity status, pan and zoom across a full board, hex and hexside inspection showing raw grid samples and derived facts with their derivation reasons, layer toggles, and the authoring functions in section 8.

### ASL-MAP-063: Hosting

The Studio is a local authoring and verification tool, built as a Blazor Web App with interactive server rendering. It is not part of the LimboDancer Host and is not exposed through MCP.

### ASL-MAP-064: SVG-only rendering

All board rendering, in the Studio, in exports, and in test evidence, must be SVG. The map projects must not generate raster images and must not depend on an image-processing library. The Terrain Grid is a data structure for derivation and fidelity checks. It is never presented to users except through an SVG view.

### ASL-MAP-065: No VASL artwork

The Studio and tools must not read, display, or underlay VASL board images. Visual verification uses only the Exact, Styled, and Hex-fact views of LimboDancer's own data. The board image's entry name and hash may be recorded in provenance.

## 10. Provenance, determinism, and licensing

### ASL-MAP-070: Canonical board package

Every board, ingested or authored, must have a canonical, versioned package form containing geometry, terrain catalog identity, grid (or a reference to its source), board metadata, Feature Model where present, and provenance. Serialization must be canonical and deterministic, so the same board always produces the same bytes and hash.

### ASL-MAP-071: Source provenance

Ingested boards must record the VASL repository commit, board archive or source-file hashes (including the `LOSData` and `BoardMetadata.xml` Git blob SHAs, consistent with the existing board 01 evidence), and the `SharedBoardMetadata.xml` hash. Authored boards must record author, version, and source boards used.

### ASL-MAP-072: Change sensitivity

Any change to source data, terrain catalog, derivation version, or Feature Model must produce a new board version identity, so that conclusions depending on superseded board data can be detected (ASL-RD-010).

### ASL-MAP-073: No artwork or full grids in the repository

The repository must not contain VASL board images, images derived from them, or full decoded grids of VASL boards. It may contain hashes, small evidence extracts (such as the existing building-override inventory), and F2 oracle fixtures of derived Hex Facts for boards used in tests. Test assets that need a grid must be synthetic, or must load from the configured local VASL source and be skipped when it is absent.

### ASL-MAP-074: Original map ownership

Authored boards that do not start from a VASL board are original LimboDancer content and may be committed. Boards that start from a vectorized VASL board carry that provenance and are subject to ASL-MAP-073 until the user replaces the source-derived content.

## 11. Consumption by the reference domain

### ASL-MAP-080: Read API for observation providers

`LimboDancer.Domains.Asl.Maps` must expose a read-only API that an ASL observation provider can use to resolve a location and obtain Hex Facts, board version, verification status, and provenance.

### ASL-MAP-081: Scenario A1 compatibility

The existing `Board01TerrainCatalog` and Scenario A1 providers must keep working unchanged until a separate reviewed change replaces their evidence source. When that happens, the replacement must reproduce the 63 building overrides exactly and pass the existing Scenario A1 tests.

The catalog's current consumers are:

- `Board01ValidatedSnapshotSource`, defined in the same file, which the original occupied-building board observation provider uses;
- the post-reveal, concealed-SMC overrun, second-defender eligibility, and second-defender consequence observation providers, which take the concrete `Board01TerrainCatalog` class as a constructor parameter;
- `ScenarioA1VerifiedReturnConclusionSource` in `LimboDancer.Domains.Asl.Execution`, which constructs it directly and feeds the governed second-defender return action.

Because one consumer is on an execution path, a replacement is a change to execution evidence, not only to read-only conclusions. The reviewed change must first introduce an abstraction those consumers accept, then show that the second-defender return gate-binding, journal, and Host registration tests still pass unchanged.

### ASL-MAP-082: LOS is out of scope

Line-of-sight calculation is not part of this effort. The grid, geometry, and derivation must nevertheless expose everything a later LOS executor for Scenario B needs, including per-cell terrain and elevation, terrain-type LOS properties, and grid-to-hex mapping.

## 12. Planned documents and sequence

The following documents will refine these requirements:

1. **ASL Map Studio Requirements** (this document).
2. **VASL Board Ingestion Design:** archive layout, `LOSData` wire format, metadata schemas, terrain catalog, derivation rules cited to the VASL source, provenance, and the F2 oracle harness.
3. **ASL Map Model and Authoring Design:** grid, Hex Facts, Feature Model, compiler, vectorizer, canonical board package schema, composition, Scenes, validation, and the deferred VASL features.
4. **ASL Map Studio Architecture and Rendering Design:** project structure, dependencies, Blazor hosting, rendering pipeline and modes, and editor interaction.

Proposed implementation sequence:

```text
ASL-MAP-01  Geometry, coordinates, and terrain catalog; Scenario A1 location-string round trip (ASL-MAP-022)
ASL-MAP-02  LOSData and metadata ingestion; F1 for board 01
ASL-MAP-03  Hex-fact derivation and F2 oracle harness; F2 for board 01
ASL-MAP-04  SVG rendering (Exact and Hex-fact views) and Studio viewer
ASL-MAP-05  Batch ingestion and F1/F2 for all standard geomorphic boards
ASL-MAP-06  Feature Model, compiler, and vectorizer; F3 for board 01
ASL-MAP-07  Styled and Comparison views and Studio authoring
ASL-MAP-08  Deferred VASL features: overlays, SSR transforms, non-geomorphic boards
```

## 13. Acceptance scenarios

### Scenario M1: Faithful board 01

Given a configured VASL checkout, when a user opens board 01 in the Studio, the board is ingested, F1 and F2 pass, the board is marked verified with its source hashes, and the Exact and Hex-fact views render as SVG. Inspecting E4 shows a two-level stone building at ground level, with the metadata override identified as the source of the level count.

### Scenario M2: Geomorphic batch

Given the same checkout, when all standard geomorphic boards are ingested in batch, every board with `LOSData` either passes F1 and F2 or is reported with coordinate-level differences or a specific ingestion diagnostic.

### Scenario M3: Authoring round trip

Given verified board 01, when it is vectorized and recompiled, the resulting Hex Facts are identical to the ingested ones and pixel agreement meets the defined threshold.

### Scenario M4: Original map

Given a new geomorphic board, when a user draws a village with roads, multi-hex buildings, walls, woods, and a hill, the Studio compiles and derives it live, validation reports no errors, the export is a canonical board package with a stable hash, and a map consumer can resolve a location on it and obtain its Hex Facts.

### Scenario M5: Missing or changed source

Given a board whose source `LOSData` changes or whose terrain code is absent from the catalog, the board receives a new version identity or is refused verified status, and a consumer receiving it treats its terrain as nondefinitive.

## 14. Traceability

| Parent requirement | Map requirements |
|---|---|
| ASL-RD-001 Authoritative source fidelity | ASL-MAP-021, 022, 033, 071 |
| ASL-RD-005 Reference and changing state | ASL-MAP-010 to 015, 070 |
| ASL-RD-006 Spatial reasoning | ASL-MAP-020 to 024, 082 |
| ASL-RD-010 Change sensitivity | ASL-MAP-072 |
| ASL-RD-011 Indeterminate outcomes | ASL-MAP-034, 044 |
| ASL-RD-013 Tenant and package isolation | ASL-MAP-001, 002 |
| ASL-RD-015 Separate domain-package integration | ASL-MAP-001 to 003, 080 |
