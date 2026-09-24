# LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Architecture and Rendering Design

**Status:** Proposed

**Parent:** [ASL Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), requirements ASL-MAP-001 to 004, ASL-MAP-052, ASL-MAP-056, and ASL-MAP-060 to 065.

**Companions:** The [VASL Board Ingestion Design](<LimboDancer.Agentic.CognitiveRuntime ASL VASL Board Ingestion Design.md>) and the [Map Model and Authoring Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design.md>). This document specifies how their components are packaged, how every view is rendered as SVG, and how the Blazor Map Studio presents, inspects, and edits boards.

## 1. Principles

1. **SVG only.** Every visual output is SVG (ASL-MAP-064). No project generates bitmaps or references an image library. VASL board artwork is never read or shown (ASL-MAP-065).
2. **Rendering is a pure function.** `(model layer, view, theme, options, renderer version) -> SVG bytes`, byte-identical on every run and platform (ASL-MAP-061).
3. **The server renders, the browser displays.** SVG is produced in `Maps.Rendering`, which has no Blazor dependency (ASL-MAP-003). The Studio moves prebuilt SVG to the browser and keeps only small interactive overlays in the Blazor render tree.
4. **Pan and zoom never cross the network.** Viewport changes are handled in the browser. Only semantic events, such as hover over a hex, a click, or a completed drawing gesture, reach the server.
5. **The prototypes set the visual vocabulary, not the code.** The terrain patterns, curvy roads, building footprint kit, dynamic legend, per-hex symbols, and flat-top N=0 side naming from `docs/ASL/MapGenerators/` inform the themes and views. They are re-authored, not copied (ASL-MAP-004).

## 2. Solution structure

### 2.1 Projects

All projects target the solution defaults in `src/ASL/Directory.Build.props` (net10.0, nullable, analyzers, warnings as errors, lock files) and are added to `LimboDancer.Domains.Asl.sln`.

```text
src/ASL/
  LimboDancer.Domains.Asl.Maps/              geometry, catalog, grid, derivation, outlines,
                                              feature model, compiler, vectorizer, validation,
                                              packages, read API
  LimboDancer.Domains.Asl.Maps.Vasl/         VASL source access and LOSData/metadata codecs
  LimboDancer.Domains.Asl.Maps.Rendering/    SVG writer, themes, view builders
  LimboDancer.Domains.Asl.MapStudio/         Blazor Web App
  tests/
    LimboDancer.Domains.Asl.Maps.Tests/
    LimboDancer.Domains.Asl.Maps.Vasl.Tests/
    LimboDancer.Domains.Asl.Maps.Rendering.Tests/
    LimboDancer.Domains.Asl.MapStudio.Tests/
  tools/
    vasl-hexfact-oracle/                     Java F2 oracle (javac + Maven-resolved classpath; not in the .sln)
  boards/                                    committed authored board packages
```

### 2.2 Dependencies

```text
Maps.Vasl ------> Maps
Maps.Rendering -> Maps
MapStudio ------> Maps, Maps.Vasl, Maps.Rendering
```

- `Maps`, `Maps.Vasl`, and `Maps.Rendering` use only the base class library (`System.IO.Compression`, `System.Xml.Linq`, `System.Text.Json`, `System.Security.Cryptography`). They have no package references.
- `MapStudio` uses the ASP.NET Core shared framework only.
- New test packages, added to `src/ASL/Directory.Packages.props`:
  - `Microsoft.AspNetCore.Mvc.Testing` for endpoint and prerendered page tests (added in ASL-MAP-04);
  - `bunit` for component tests, deferred until the viewer is split into the components of section 5.2 (ASL-MAP-07).
- No map project references `src/LimboDancer` (ASL-MAP-002).

### 2.3 Existing solution gap

The Scenario A1 projects (`LimboDancer.Domains.Asl.ScenarioA1` and its tests) exist under `src/ASL/` but are not in `LimboDancer.Domains.Asl.sln`. They also opt out of lock files and code-style enforcement. Adding them to the solution is outside this design. The map projects do not opt out of either.

## 3. SVG rendering (`LimboDancer.Domains.Asl.Maps.Rendering`)

### 3.1 Coordinate system

- SVG user units are board pixels, the same space as the Terrain Grid and the Feature Model.
- The root element is `<svg viewBox="0 0 gridWidth gridHeight">`. Margins for edge labels and the legend are added by extending the view box, never by transforming board content.
- Feature Model coordinates are fixed-point 1/64 pixel (Model Design section 3.5). A value `n/64` always has an exact decimal form of at most 6 fractional digits, so coordinates are written exactly, with trailing zeros trimmed. No floating-point formatting occurs.

### 3.2 SvgWriter

`SvgWriter` is a forward-only UTF-8 writer that guarantees byte-identical output:

- no XML declaration, no BOM, LF line endings, no indentation in production output (an indented option exists for debugging);
- attributes written in a fixed order per element kind;
- numbers formatted as in section 3.1; integers as invariant decimal;
- identifiers derived from content only (layer names, theme keys, hex names, `FeatureId`s), never counters or GUIDs generated at render time;
- path data in absolute commands, one fixed command letter per segment kind, with separators fixed.

Every view builder writes through `SvgWriter`. Nothing in the rendering project builds SVG with string concatenation.

### 3.3 Documents and layers

A rendered board is a `RenderDocument`: a `<defs>` block plus ordered layers. Each layer is a `<g id="layer-<name>">` and can also be produced alone as an **SVG fragment**. The Studio loads fragments separately (section 5.3), and exports concatenate them into one standalone document (ASL-MAP-056).

| Layer | Views | Content |
|---|---|---|
| `defs` | all | theme patterns, symbols, markers, clip paths |
| `exact-terrain` | Exact, Comparison | one `<path>` per `(terrain code, elevation)` group from `GridOutlines`, `fill-rule="evenodd"`, `shape-rendering="crispEdges"` |
| `exact-elevation` | Exact (toggle) | elevation bands from elevation-only outlines, drawn as translucent tints |
| `styled-elevation` | Styled | hill level tints and crest lines |
| `styled-area` | Styled | area terrain with pattern fills |
| `styled-linear` | Styled | roads, paths, streams, gullies, railroads |
| `styled-bridges` | Styled | bridges |
| `styled-buildings` | Styled | footprints, material texture, shadow for multi-level buildings |
| `styled-hexside` | Styled | walls, hedges, bocage, cliffs, rowhouse bars |
| `styled-marks` | Styled | stairwell squares, fidelity pin highlights (toggle) |
| `hexfacts` | Hex-fact | per-hex symbols (section 3.6) |
| `diff` | Comparison | differences (section 3.7) |
| `grid` | all (toggle) | hex outlines as one path; center dots |
| `labels` | all (toggle) | hex names |
| `legend` | all (toggle) | terrain present on the board |

In Styled and Hex-fact layers, each feature or hex is wrapped in its own `<g>` whose id is derived from its `FeatureId` or hex name. This lets the Studio replace one feature's or one hex's markup after an edit without re-sending the layer (section 5.4).

### 3.4 Themes

A `RenderTheme` is versioned data, stored as an embedded JSON resource. It maps each terrain code to a style:

- fill (color or pattern reference), stroke, stroke width and dash, opacity, and layer;
- pattern and symbol definitions, as parameterized SVG snippets authored for this project;
- legend label and legend order.

Two themes ship in version 1:

- **`catalog`**, used by the Exact view: flat fills in the VASL catalog map colors (Ingestion Design section 6.2). It is generated from the catalog, so it covers every code.
- **`board`**, used by the Styled view: patterns for woods (canopy clusters), orchard (tree grid), grain (stipple rows), brush, marsh (tufts), graveyard, crags, rubble, and water; stone courses and wooden planks for buildings; two-tone road strokes; banked streams; stone walls; dotted hedges; hatched cliffs; tinted hill levels with crest lines. The prototype terrain definitions are the visual reference.

A code with no style in the `board` theme falls back to its catalog color with a plain fill, and the legend marks it as unstyled. Theme version is part of the render cache key.

### 3.5 Exact view

1. Take `GridOutlines` for the board (Model Design section 4.4).
2. Group regions by `(code, elevation)`, and write one path per group containing all its rings.
3. Fill with the `catalog` theme color and `crispEdges` rendering, so each rendered pixel edge falls on a board pixel edge at 1:1 zoom.
4. Tag each path with `data-code` and `data-elev`, so the inspector can report exact terrain without a server call when the pointer is over a region.

Measured in ASL-MAP-04: the board 01 Exact document is 241 KB (132 regions, about 47,000 ring vertices) and renders in about 26 ms from traced outlines. The dithered worst case, board 04, is 646 KB (6,924 regions). Tracing a board takes 140 to 390 ms, and the Studio does it once per board version.

### 3.6 Hex-fact view

Each hex is drawn from its derived `HexFacts` only, never from the grid or the features:

- hex fill in the style of its center terrain, with a depression hatch when present;
- a badge for base level when it is not zero;
- building levels as a stacked-level glyph, and a stairwell square when set;
- each hexside with recorded terrain drawn as a colored edge segment on that side; off-map hexsides drawn dashed;
- a bridge glyph when a bridge location exists;
- in **trace mode**, the fill color encodes which derivation step decided the center terrain (sample, hexside fallback, probe fallback, default, inherent, depression, or marketplace), so fallback-driven hexes stand out.

This view carries forward the per-hex symbol style of the v39 generator and HexLab, now driven by derived facts.

### 3.7 Styled and Comparison views

The **Styled view** renders a Feature Model:

- For an authored board, that is the author's model.
- For an ingested board, it is the vectorizer output (Model Design section 7.1).
- Outlines are drawn smoothed. Each simplified polyline becomes a Catmull-Rom spline converted to cubic Bézier segments, with fixed tension, computed in fixed point. Smoothing is display-only and does not change the model.
- `LinearTerrain` with a centerline is drawn as layered strokes along the centerline; with an outline (vectorized version 1), it is drawn as a filled outline.
- `HexsideTerrain` is drawn along the hexside geometry with the theme's hexside style.

The **Comparison view** shows the Exact and Styled views of one board together (ASL-MAP-060). Three presentations are available, all switched in the browser without re-rendering:

- side by side, with synchronized pan and zoom;
- overlaid, with a Styled opacity slider;
- a swipe divider.

The `diff` layer has two parts, both computed on the server:

- **cell differences**, where the compiled Styled model's grid differs from the source grid, traced as outlines with a hatch;
- **hex differences**, where compiled Hex Facts differ from the source facts, drawn as bold hex outlines. For a vectorized board this is the F3 result and must be empty for F3 to pass.

### 3.8 Performance targets

| Operation | Target, standard board, development workstation |
|---|---|
| Exact view, full document | under 200 ms from cached outlines |
| Hex-fact view | under 100 ms |
| Styled view from a cached Feature Model | under 300 ms |
| Per-feature fragment after an edit | under 20 ms |

The ASL-MAP-04 and ASL-MAP-07 reviews record measured values.

## 4. Studio hosting and services (`LimboDancer.Domains.Asl.MapStudio`)

### 4.1 Hosting

- Blazor Web App with interactive server rendering (ASL-MAP-063).
- Kestrel binds to `localhost` only.
- There is no authentication, because the Studio is a single-user local tool.
- The Studio is not part of the LimboDancer Host and is not exposed through MCP.

### 4.2 Configuration

| Key | Meaning | Default |
|---|---|---|
| `AslMaps:VaslRoot` | VASL checkout (Ingestion Design section 3.1) | unset: VASL features disabled |
| `AslMaps:BoardsRoot` | authored board packages | `src/ASL/boards` in the repository |
| `AslMaps:CacheRoot` | decoded grids, outlines, vectorizations, rendered fragments | `%LOCALAPPDATA%/LimboDancer/AslMaps/cache`, always outside the repository (ASL-MAP-073) |

### 4.3 Services

| Service | Lifetime | Responsibility |
|---|---|---|
| `BoardLibrary` | singleton | lists VASL boards and authored packages, with computed status (Model Design section 10) |
| `IngestionService` | singleton | ingests on demand and in batch; caches decoded grids by `LOSData` blob SHA |
| `FidelityService` | singleton | runs F1, F2 (with fixtures), and F3; stores results by `BoardVersion` |
| `DerivationCache` | singleton | Hex Facts and outlines by `(BoardVersion, version)` |
| `RenderService` | singleton | produces documents and fragments; content-addressed cache |
| `JobRunner` | singleton | background jobs (batch ingestion, vectorization, fidelity) with progress and cancellation |
| `AuthoringSession` | per circuit | current Feature Model, undo and redo stacks, incremental compiler state, validation results |

Singleton caches are keyed by content hashes, so concurrent tabs share results safely. Authoring state is per circuit. Saving writes a package to `BoardsRoot`. Saving is refused if the package on disk has changed since it was loaded, detected by comparing `BoardVersion`.

### 4.4 Render endpoints

Static layers are served over HTTP rather than through the Blazor render tree:

```text
GET /render/{boardRef}/{boardVersion}/{view}/{layer}.svg?theme=&options=
GET /render/{boardRef}/{boardVersion}/{view}/document.svg?...        (export)
```

- Responses are `image/svg+xml`, with an `ETag` equal to the content hash and immutable caching, because the URL contains the version.
- A fragment response is an SVG document whose root `<svg>` wraps the layer's `<g>`, so it can be parsed and imported directly (section 5.3).

**Why not the render tree.** A full board has tens of thousands of path vertices in a few hundred elements, or thousands of elements for dithered boards. Keeping them in Blazor's render tree would put them in circuit memory and in every diff. HTTP fragments are cached by the browser and cost nothing after the first load.

## 5. Board viewer

### 5.1 Pages

| Route | Purpose |
|---|---|
| `/` | board library: VASL boards (when configured) and authored boards, with status, F1, F2, and F3 badges and diagnostic counts, filtered by status and scope |
| `/boards/{boardRef}` | viewer: view switcher (Exact, Styled, Hex facts, Comparison), layer toggles, inspector, diagnostics |
| `/fidelity` | batch runs and reports: per-board results, differences by hex and field, export as JSON |
| `/author/{boardRef}` | editor (section 6) |
| `/author/new` | new board from a geometry preset (ASL-MAP-050) or from a verified VASL board (ASL-MAP-055) |
| `/settings` | configuration status: VASL root detected, commit, catalog hash, cache size |

### 5.2 Component structure

```text
BoardViewer.razor
  BoardViewport.razor         <svg> host; owns the JS viewport module
    <g id="static">           fragments inserted by JS, never touched by Blazor
    <g id="overlay">          Blazor-rendered: hover hex, selection, edit handles, tool previews
  ViewToolbar.razor           view, theme, layer toggles, comparison mode
  Inspector.razor             facts for the hovered or selected hex
  DiagnosticsPanel.razor
  Legend (static layer)
```

### 5.3 JS viewport module

A small ES module, `boardViewport.js`, loaded through `IJSObjectReference`, is the Studio's only JavaScript. It:

- fetches layer fragments by URL, parses them with `DOMParser`, and imports them into the static group, replacing the previous version of the same layer;
- handles pan (drag), zoom (wheel, pinch, keyboard `+`, `-`, `0`), and fit-to-board by changing the root `viewBox`, entirely in the browser;
- converts pointer positions to board pixel coordinates using the SVG's screen transform;
- reports to .NET only hover changes (throttled to at most 20 per second, and only when the pointer crosses into another hex, which the module computes from the geometry sent at load), clicks, and completed gestures;
- applies per-feature and per-hex fragment patches (section 5.4);
- switches comparison presentation (side by side, overlay opacity, swipe) with CSS and a second synchronized viewport, without server calls.

Hex hit-testing on the server uses `BoardGeometry.HexAt`, which returns the hex whose center dot is nearest the point. It is a UI hit test only and is not a port of VASL's `gridToHex`; nothing in ingestion or derivation depends on it. Half hexes whose center dot lies on the grid boundary are still hit by points inside the grid.

### 5.4 Incremental updates

After an edit (section 6.3), the server returns a `RenderPatch`:

```text
RenderPatch
  BoardVersion
  Layers[]   { layer, upserts: [{ id, svgFragment }], removals: [id] }
  Replace[]  { layer }     layers to refetch in full (for example, Exact outlines
                           when the dirty region is large)
```

The JS module applies upserts and removals by element id inside the named layer. The Exact layer is patched region by region: outline groups intersecting the dirty region are re-emitted with ids derived from `(code, elevation, anchor cell)`. The Hex-fact layer is patched for re-derived hexes only.

### 5.5 Inspector

For the hovered or selected hex, the inspector shows (ASL-MAP-062):

- all `HexFacts` fields and the derivation trace, in words ("center terrain from hexside 2 building fallback");
- **raw grid samples** at every derivation sample point: the center probe order, the four 5-pixel probes, and the six edge sample points, each with coordinates, terrain name, and elevation, and which one was used;
- features covering the hex (Styled and authoring views), with links to select them;
- validation findings for the hex;
- the location in canonical form (`bd01:E4:0`), with a copy button.

## 6. Editor

### 6.1 Layout

The editor reuses `BoardViewer` in Styled view, with the Exact and Hex-fact views available as a toggle or side by side, plus:

- a tool palette;
- a properties panel for the selected feature;
- a layer list (features by kind and layer, with visibility and lock);
- a validation panel whose findings zoom to their location when clicked;
- a scene browser;
- undo and redo, and a save button with the current `BoardVersion`.

### 6.2 Tools

| Tool | Gesture | Command produced |
|---|---|---|
| Select | click, shift-click, box drag | selection only |
| Move and edit vertices | drag feature or handle | update feature geometry |
| Area | click vertices; double-click closes; hold `Alt` to cut a hole | add `AreaTerrain` |
| Elevation | same as Area, with a level selector | add `ElevationRegion` |
| Linear | click points; drag to pull Bézier handles; width and code in properties | add `LinearTerrain` (centerline) |
| Building kit | click a hex, then choose centered, span (with axis), or flush (with side); or draw a free polygon | add `Building` footprint; the building id is shared with adjacent footprints when requested |
| Hexside | click a hexside, or drag across several; hit-testing picks the nearest hexside within 6 pixels | add or extend `HexsideTerrain` |
| Stairway | click a hex | toggle stairway annotation |
| Annotation | click a hexside | toggle slope, railroad embankment, or partial orchard |
| Scene | choose a scene, click an anchor hex, rotate with `R` in 60 degree steps | place scene (Model Design section 8.1) |
| Intent | click a hex, then set expected facts | author intent assertion (Model Design section 6) |

**Snapping:** to hex centers, vertices, hexside midpoints, existing feature vertices, and whole pixels, in that priority, and only within a configurable radius. Holding `Shift` disables snapping. Coordinates are converted to fixed point on the server; the browser never decides final geometry.

**Tool previews** (rubber-band lines, ghost footprints) are rendered in the overlay group by the JS module during the gesture. Only the completed gesture is sent to the server.

### 6.3 Edit cycle

```text
gesture complete (browser)
 -> command (server, AuthoringSession)
 -> type validation; reject with message if invalid
 -> apply: new FeatureModel version, inverse command pushed to undo
 -> incremental compile of the dirty region (Model Design section 5.5)
 -> re-derive affected hexes; re-validate affected rules
 -> RenderPatch for Styled features, Exact outline regions, changed Hex-fact hexes, diff
 -> browser applies patch; panels update
```

The target from gesture completion to patch applied is under 150 ms for single-feature edits on a standard board. Larger edits, such as placing a scene, may show a progress indicator.

### 6.4 Vectorized boards

When a user starts from a verified VASL board (ASL-MAP-055):

- the vectorization runs as a background job;
- the new board receives an `ab-` reference with provenance pointing to the source `BoardVersion`;
- fidelity pins are visible by default;
- a banner states that the board derives from VASL data and is subject to ASL-MAP-073 and ASL-MAP-074, which prevents committing it to `BoardsRoot` until the source-derived content is replaced.

## 7. Error handling and diagnostics

- Ingestion, fidelity, validation, and render diagnostics use their documented codes and appear in the diagnostics panel with their location.
- A render failure for one layer shows an error placeholder for that layer only. Other layers still display.
- An unconfigured or invalid `VaslRoot` disables VASL boards and shows setup guidance on `/settings`. Authored boards keep working.
- Circuit loss discards unsaved authoring state after a confirmation on navigation. Autosaving a draft to `CacheRoot` every 30 seconds limits the loss.

## 8. Tests

| Project | Tests |
|---|---|
| `Maps.Rendering.Tests` | SvgWriter formatting and ordering rules; each view on synthetic boards compared with committed golden SVG files; byte-identical output across two processes and on Windows and Linux in CI; every output parses as XML and passes structural checks (unique ids, layer order, no raster `<image>` elements) |
| `Maps.Rendering.Tests`, local VASL | Exact view of each ingested board: rendered SVG hash recorded in the test run, and cell coverage of the rendered paths equals the grid (by re-filling the parsed paths); skipped without a VASL checkout. Golden files for VASL boards are not committed, because they would be images derived from VASL boards (ASL-MAP-073). |
| `MapStudio.Tests` | bUnit tests for toolbar, inspector, and panels; endpoint tests with `WebApplicationFactory` for ETag, caching, content type, and fragment shape; `AuthoringSession` command and patch tests |
| Manual, recorded in reviews | pan, zoom, hover, and editing interactions in a browser, for ASL-MAP-04 and ASL-MAP-07 |

As built in ASL-MAP-04:

- `Maps.Tests` adds outline tracer tests: a uniform grid, a hole, a corner pinch, elevation splits, ring orientation, and random grids refilled losslessly. It also adds `HexAt` tests.
- `Maps.Rendering.Tests` covers SvgWriter numbers and escaping; determinism; well-formedness; unique ids; layer order; no raster images; fragment shape; and lossless refill of the Exact paths. It also compares both views of a synthetic 3 by 2 board with golden files in `Golden/`; set `ASL_MAPS_UPDATE_GOLDEN=1` to regenerate them. With a VASL checkout, it refills board 01 as well. ASL-MAP-05 adds `BoardRenderChecks` tests and a batch over every ingested board that requires lossless terrain and elevation outlines and raster-free SVG for both views.
- `MapStudio.Tests` runs the Studio with `WebApplicationFactory` and a fake board provider. It tests layer and document responses, entity tags and 304 responses, trace mode, 404 cases, and the prerendered library and viewer pages. bUnit tests arrive with the component split in ASL-MAP-07.

Browser automation (for example, Playwright) is deferred. It can be added when the editor stabilizes, without changing the design.

## 9. Sequence mapping

| Step | Delivers from this design |
|---|---|
| ASL-MAP-01 | the four projects and their solution entries (section 2); `Maps.Tests` and `Maps.Vasl.Tests`; a minimal Studio page reporting configuration status. `Maps.Rendering.Tests` and `MapStudio.Tests` are added in ASL-MAP-04 with the first rendering and Studio code. |
| ASL-MAP-04 | SvgWriter, `catalog` theme, Exact and Hex-fact views, render endpoints, board library and viewer, inspector (as built: section 9.1) |
| ASL-MAP-05 | `/fidelity` batch pages and job runner (as built: section 9.2) |
| ASL-MAP-06 | F3 as an option of the fidelity batch (as built: section 9.3) |
| ASL-MAP-07 | `board` theme, Styled and Comparison views, editor, patches (as built: section 9.4) |

### 9.1 ASL-MAP-04 as built

ASL-MAP-04 delivers a smaller surface than sections 4 and 5 describe. The rest moves to the steps that need it:

- **Services.** `IBoardProvider` and its VASL implementation, `VaslBoardProvider`, combine the roles of `BoardLibrary`, `IngestionService`, `FidelityService`, and `DerivationCache` for VASL boards. A load ingests the board, runs F1, derives Hex Facts, runs F2 against the committed oracle fixture when one exists, and traces outlines. `RenderCache` holds rendered documents and fragments keyed by board, version, view, layer, and trace flag. Both caches are in memory only; `CacheRoot`, `BoardsRoot`, and `JobRunner` are not implemented yet.
- **Board version.** For VASL boards, the version in render URLs is the `LOSData` content blob SHA.
- **Endpoints.** `GET /render/{boardRef}/{version}/{view}/{layer}.svg` and `.../document.svg` take only `?trace=true`. There is no `theme` or `options` query yet, because `catalog` is the only theme. A stale version, an unknown board, view, or layer, or a non-SVG file name returns 404.
- **Pages.** `/` lists the VASL boards whose names are valid board references. Scope is decided when the library opens, from each board's metadata and the presence of LOSData only (`VaslBoardImporter.CheckScope`, which `Import` also uses). By default only in-scope boards are listed: 158 in the pinned checkout, which are the 156 verified boards plus `bd79` and `bdLFT1`, which fail. A toggle shows the 134 out-of-scope boards with the reason for each. A totals line counts verified, ingested, failed, and not-loaded boards. A board's status, F1, F2, and diagnostics appear once it has been loaded, either by opening it or by "Check all boards in scope". Diagnostics are grouped by code and severity, such as "18 × VASL-META-004 (warning)", with the messages in a tooltip. `/boards/{boardRef}` has the Exact and Hex-fact views, layer toggles, a trace toggle, and fit. Its inspector shows the clicked hex's facts, the grid code and elevation under the pointer, and the board's provenance and fidelity. `/fidelity`, `/settings`, and `/author` are not implemented.
- **Viewport.** `boardViewport.js` creates the `<svg>` itself, imports every layer of the view, and toggles layers with CSS. It pans by drag, zooms by wheel about the pointer, and keeps the zoom when switching views. It reports clicks only; hover, keyboard zoom, and patches wait for ASL-MAP-07. The selected hex is outlined by a polygon that the module draws from vertices the server sends.
- **Legend.** In the Exact view, hexside terrain uses its catalog color, matching the paths. The hex-side palette is used only in the Hex-fact view.
- **Local run.** Static web assets resolve from build output only in the Development environment, so run the Studio from source with `--environment Development`.

### 9.2 ASL-MAP-05 as built

- **Batch.** `VaslBatchImporter` in `Maps.Vasl` (Ingestion Design section 10.1) runs every board. `IFidelityBatch` and its VASL implementation, `VaslFidelityBatch`, add `BoardRenderChecks` from `Maps.Rendering` for each ingested board. The checks are:
  - `exact-outlines` and `elevation-outlines`: `GridOutlineVerifier` refills the traced outlines one (code, elevation) at a time under the even-odd rule, as the Exact view draws them, and counts cells that are uncovered, covered twice, or covered by the wrong key;
  - `exact-svg` and `hexfacts-svg`: each view's full document, recorded by SHA-256, size, and render time, and required to contain no `<image>` element.
- **Job runner.** `FidelityJobRunner` runs one batch at a time in the background, with progress and cancellation. Boards run by the batch are not kept in the viewer's cache, so a full run does not hold every grid in memory.
- **Reports.** `FidelityReportStore` saves each completed report as `{CacheRoot}/fidelity/fidelity-{UTC start}.json`. `AslMaps:CacheRoot` defaults to `%LOCALAPPDATA%/LimboDancer/AslMaps/cache`, outside the repository. `GET /fidelity/reports/{id}.json` serves a saved report; ids are validated against a fixed pattern.
- **Page.** `/fidelity` starts and cancels a run and shows progress. It lists saved reports and shows the selected one:
  - a summary with its source, tool versions, and median stage timings;
  - a table of boards with outcome, F1, F2, the checks (each detail in a tooltip), diagnostics, and time;
  - F2 differences, expandable per board, with the first 200 shown and the rest in the JSON.
  The table can be filtered to boards in scope, boards not verified, or all boards.
- **Library.** The "Check all" button is replaced by a link to `/fidelity`. For a board that has not been opened, the library shows the latest report's result, marked "(batch)", only when the report still applies: the board's `LOSData` and metadata blobs, the catalog blob, and the importer, derivation, and renderer versions must all match the current ones. A stale report never marks a board verified.
- **Measured.** In the Studio, a Debug build, the full run with rendering checks took 53 seconds for 297 directories. In the Release test run, the batch without rendering checks takes about 11 seconds, and the batch with rendering checks about 30 seconds.

### 9.3 ASL-MAP-06 as built

- The `/fidelity` page has an "Include F3" option. With it, each ingested board is also vectorized and recompiled (Model Design section 16). The run records `f3-hexfacts` (identical facts, iterations, pins) and `f3-pixels` (agreement, overlap, and model size, with any threshold failures).
- F3 checks are informational: a `FidelityCheck` now has a `Gating` flag, and only gating checks decide a board's outcome, because F3 describes the vectorized model and not the board (Model Design section 10). Failed informational checks show as warnings.
- Reports that include F3 record the compiler and vectorizer versions.

### 9.4 ASL-MAP-07 as built

- **Theme.** The `board` theme is versioned JSON embedded in `Maps.Rendering` (`Themes/board.json`, version 1.0.0).
  - It defines twelve patterns (woods, orchard, grain, brush, marsh, graveyard, crags, rubble, water, stone, wood, and cliff hatching), hill tints by level with a crest stroke, and ordered style rules.
  - A rule matches a terrain by exact name, a name substring, and a category, in order; the first match wins. Unmatched codes fall back to their catalog color and are marked "(unstyled)" in the legend.
  - Pattern elements are data written through `SvgWriter`, so no SVG is built by string concatenation.
- **Styled view.** Layers `styled-elevation`, `styled-area`, `styled-linear`, `styled-bridges`, `styled-buildings`, `styled-hexside`, and `styled-marks` draw the Feature Model in compile order, one `<g id="f-{FeatureId}">` per feature.
  - Area and elevation outlines are smoothed as closed Catmull-Rom splines. Their cubic control points are computed in fixed point (one sixth of the neighbor difference, rounded), so the output stays exact and deterministic. Buildings, bridges, and pins keep sharp corners.
  - Linear terrain with a centerline is drawn as the theme's stacked strokes on the centerline path; vectorized linear terrain is drawn as its filled outline.
  - Multi-level buildings and factories get a shadow offset by two pixels.
  - Stairways are squares in `styled-marks`, and fidelity pins are magenta.
  - For a VASL board, the Styled input is the vectorizer output, computed on first use and cached with the board. Board 01 takes about 5 seconds the first time, and its Styled document is 327 KB with 94 feature groups.
- **Comparison view.** It layers `exact-terrain`, the styled layers, and `diff`.
  - The diff layer hatches the cells where the Styled model's compiled grid differs from the board's grid, and outlines hexes whose facts differ. Its summary element carries both counts. On board 01, 8,184 cells differ (99.30% agreement) and 0 hexes, which matches F3.
  - Overlay with a Styled opacity slider, swipe with a divider, and side by side with synchronized pan and zoom are switched in the browser without re-rendering.
- **Render patches.** `BoardRenderer.RenderFeature` returns one feature's fragment with its layer and the id of the next feature in paint order.
  - After an edit, the editor sends the changed features' fragments and the removed ids to the viewport, which replaces, inserts, or removes them by id.
  - Layers a patch does not cover (Exact, Hex-fact, legend, and the marks layer) are refetched in place by the new version's URLs.
  - Patches are sent over the circuit rather than an HTTP endpoint, because the session holding the new version lives in the circuit.
- **Authored boards.** `AuthoredBoardService` keeps packages in `BoardsRoot` (default `src/ASL/boards`), and boards derived from VASL data in `{CacheRoot}/drafts` (ASL-MAP-074).
  - Each build (compile, derive, validate) is cached by `(board, BoardVersion)`, up to 64 versions, so render URLs for recent edit versions keep working.
  - `StudioBoardProvider` routes `ab-` references to it and `bd` references to the VASL provider.
  - Authoring uses the VASL catalog through `ICatalogSource`, so it needs a configured checkout.
- **Editor** (`/author/{boardRef}`, section 6):
  - The tool palette has Select, Move, Area, Elevation, Linear (straight or curved through the clicked points), Building (the kit's centered, span, and flush boxes, optionally joining the selected building), Hexside (adds, extends, or removes a span), Stairway, and Annotation (slope, railroad embankment, partial orchard).
  - Panels show properties, layers, validation, and the hex inspector.
  - Undo and redo keep stacks of inverse commands; save writes the package.
  - Keyboard: Ctrl+Z and Ctrl+Y, Delete, Escape, Enter to finish a gesture, and Backspace to drop the last point.
  - Snapping runs on the server with the priorities of section 6.2.
  - A banner marks drafts derived from VASL data.
- **Other pages.**
  - `/author/new` creates a blank board of any standard size, or a draft from a vectorized VASL board.
  - `/settings` shows the configuration, cache size, and tool versions.
  - The library lists authored boards.
  - The viewer offers all four views; the inspector (section 5.5) shows derived facts in words, the fifteen raw grid samples, the features at the hex center, the hex's validation findings, and the canonical location with a copy button.
  - Hover updates the inspector when the pointer crosses into another hex, and `+`, `-`, and `0` zoom.
- **Components.** `HexInspector`, `ValidationPanel`, `ToolPalette`, `FeatureProperties`, and `LayerList` are components with bUnit tests. The viewport remains one module shared by the viewer and the editor.
- **Measured.** A single-feature edit on a standard board rebuilds in 30 to 50 ms in a Release build, against the 150 ms target of section 6.3; undo to a cached version takes a few milliseconds. This needed the derivation speedup in Model Design section 17, not incremental compilation.
- **Deferred.**
  - The scene browser and Scene tool, and the Intent tool (Model Design sections 8.1 and 6).
  - Incremental compilation (Model Design section 5.5); full rebuilds meet the latency target.
  - Region-by-region patching of the Exact layer, which is refetched instead.
  - Autosave of drafts every 30 seconds (section 7).
  - Bézier handle dragging for the Linear tool; curves come from a Catmull-Rom fit through the clicked points.

## 10. Open issues

1. **Label fonts.** SVG bytes are deterministic, but text appearance depends on the viewer's fonts. Decide whether labels should use a bundled web font served by the Studio, or be converted to paths at render time.
2. **Very large composed maps.** Multi-board maps (ASL-MAP-08) may need tiling of the static layers by board. The fragment and patch model supports this, but it is not designed here.
3. **Dithered boards in Styled view.** The vectorizer's closing step makes grain solid, which reads better but differs visibly from the Exact view. The Comparison view shows this honestly. Whether the `board` theme should suggest dithering with a pattern is a theme decision for ASL-MAP-07.

## 11. Requirement coverage

| Requirement | Section |
|---|---|
| ASL-MAP-001, 002, 003 | 2 |
| ASL-MAP-004 | 1, 3.4 |
| ASL-MAP-052 | 5.4, 6.3 |
| ASL-MAP-056 | 3.3, 4.4 |
| ASL-MAP-060 | 3.5 to 3.7 |
| ASL-MAP-061 | 3.1, 3.2, 8 |
| ASL-MAP-062 | 5.1, 5.5 |
| ASL-MAP-063 | 4.1 |
| ASL-MAP-064 | 1, 2.2, 3, 8 |
| ASL-MAP-065 | 1, 3.4 |
| ASL-MAP-073 | 4.2, 6.4, 8 |
