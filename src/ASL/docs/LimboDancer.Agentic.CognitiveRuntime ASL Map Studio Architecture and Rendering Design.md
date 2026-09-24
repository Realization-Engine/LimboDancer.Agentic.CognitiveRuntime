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
- `Maps.Rendering.Tests` covers SvgWriter numbers and escaping; determinism; well-formedness; unique ids; layer order; no raster images; fragment shape; and lossless refill of the Exact paths. It also compares both views of a synthetic 3 by 2 board with golden files in `Golden/`; set `ASL_MAPS_UPDATE_GOLDEN=1` to regenerate them. With a VASL checkout, it refills board 01 as well. Refilling every ingested board and recording hashes is left for the ASL-MAP-05 batch run.
- `MapStudio.Tests` runs the Studio with `WebApplicationFactory` and a fake board provider. It tests layer and document responses, entity tags and 304 responses, trace mode, 404 cases, and the prerendered library and viewer pages. bUnit tests arrive with the component split in ASL-MAP-07.

Browser automation (for example, Playwright) is deferred. It can be added when the editor stabilizes, without changing the design.

## 9. Sequence mapping

| Step | Delivers from this design |
|---|---|
| ASL-MAP-01 | the four projects and their solution entries (section 2); `Maps.Tests` and `Maps.Vasl.Tests`; a minimal Studio page reporting configuration status. `Maps.Rendering.Tests` and `MapStudio.Tests` are added in ASL-MAP-04 with the first rendering and Studio code. |
| ASL-MAP-04 | SvgWriter, `catalog` theme, Exact and Hex-fact views, render endpoints, board library and viewer, inspector (as built: section 9.1) |
| ASL-MAP-05 | `/fidelity` batch pages and job runner |
| ASL-MAP-07 | `board` theme, Styled and Comparison views, editor, patches |

### 9.1 ASL-MAP-04 as built

ASL-MAP-04 delivers a smaller surface than sections 4 and 5 describe. The rest moves to the steps that need it:

- **Services.** `IBoardProvider` and its VASL implementation, `VaslBoardProvider`, combine the roles of `BoardLibrary`, `IngestionService`, `FidelityService`, and `DerivationCache` for VASL boards. A load ingests the board, runs F1, derives Hex Facts, runs F2 against the committed oracle fixture when one exists, and traces outlines. `RenderCache` holds rendered documents and fragments keyed by board, version, view, layer, and trace flag. Both caches are in memory only; `CacheRoot`, `BoardsRoot`, and `JobRunner` are not implemented yet.
- **Board version.** For VASL boards, the version in render URLs is the `LOSData` content blob SHA.
- **Endpoints.** `GET /render/{boardRef}/{version}/{view}/{layer}.svg` and `.../document.svg` take only `?trace=true`. There is no `theme` or `options` query yet, because `catalog` is the only theme. A stale version, an unknown board, view, or layer, or a non-SVG file name returns 404.
- **Pages.** `/` lists the VASL boards whose names are valid board references. A board's status, F1, F2, and diagnostics appear once it has been loaded, either by opening it or by the "Check all boards" button, which loads them one at a time. Non-geomorphic boards show as out of scope. `/boards/{boardRef}` has the Exact and Hex-fact views, layer toggles, a trace toggle, and fit. Its inspector shows the clicked hex's facts, the grid code and elevation under the pointer, and the board's provenance and fidelity. `/fidelity`, `/settings`, and `/author` are not implemented.
- **Viewport.** `boardViewport.js` creates the `<svg>` itself, imports every layer of the view, and toggles layers with CSS. It pans by drag, zooms by wheel about the pointer, and keeps the zoom when switching views. It reports clicks only; hover, keyboard zoom, and patches wait for ASL-MAP-07. The selected hex is outlined by a polygon that the module draws from vertices the server sends.
- **Legend.** In the Exact view, hexside terrain uses its catalog color, matching the paths. The hex-side palette is used only in the Hex-fact view.
- **Local run.** Static web assets resolve from build output only in the Development environment, so run the Studio from source with `--environment Development`.

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
