# ASL Independent Map Creation Analysis

**Status:** Analysis for discussion, 2026-11-02. Nothing here is approved. It answers the user's request of 2026-11-02: design new maps that do not come from VASL, with an approach flexible enough to create very large, memory-efficient maps; importing the Panzer Leader campaign boards is one input to it, not the approach itself.

## 1. The question and the short answer

Today every map is either a VASL board, an authored board edited by hand, or the one Goodwood board the Campaign generates. All three end in the same place: a **terrain grid with one cell per pixel**, which line of sight walks and the renderer traces. That grid is VASL's model, and it is what stops maps from growing: it costs about 7.3 KB per hex, so the Goodwood board (40,442 hexes) needs about 290 MB for its grid alone, and a map of a million hexes would need about 7 GB.

The options in section 5 differ in two choices:

1. **What a map designer authors**: free-form terrain shapes (today's Feature Model), or hexes built from a library of hex designs (the "hex template" idea of the earlier prototypes), or both, with hex designs that produce shapes.
2. **What the running app holds and draws**: the whole pixel grid, the grid in tiles made on demand, or shapes with a spatial index and no grid.

Memory and speed come almost entirely from the second choice. The Hex Lab and Map Lab come from the first.

## 2. What exists today

### 2.1 Three sources, one representation

| Source | How it is made | UI |
|---|---|---|
| VASL boards | Read from a VASL checkout (`BoardMetadata.xml` and the binary `LOSData`) by `VaslBoardImporter` | None for the import; `/fidelity` checks it |
| Authored boards | Feature Model edited by hand: elevation regions, area terrain, linear terrain, bridges, buildings, hexside terrain (`Maps/Features/FeatureModel.cs`) | `/author/new`, `/author/{board}` |
| Goodwood | `PilotGenerator` turns Panzer Leader regions (open, town, woods, water, marsh, levels 0 to 2, roads, streams) into Feature Models in tiles of 32 by 16 hexes; `SituationAslBoard` joins them into one board of 209 by 193 hexes | None; the Campaign Host calls `/api/campaign/import` |

Every source is compiled (`FeatureCompiler.Compile`) or read into a `TerrainGrid` (`Maps/Grid/TerrainGrid.cs`): a terrain code (byte) and an elevation (sbyte) for every pixel cell of the board, plus a stairway flag per hex. From the grid come:

- the per-hex facts (`Derivation/HexFacts.cs`) that movement and the rules read;
- line of sight: `LosMap` holds the grid and `LosCalculator` walks it, as VASL does (`Maps/Los/LosMap.cs:18-22`);
- the Exact and Hex facts views; the Styled view draws the Feature Model instead.

### 2.2 How a map looks

The look comes from `Maps.Rendering/Themes/board.json` (12 fill patterns, 28 style rules, elevation tints) for the Styled view, and from VASL catalog colours plus a hexside palette in `BoardRenderer.cs` for the other views. The server writes the colours into each SVG element. No page shows or edits them.

### 2.3 Earlier work on hex-level design

`docs/ASL/MapGenerators/` holds prototypes from 2025, kept as history:

- `asl-map-architecture.md` proposes three tiers: **hex templates** (a stone building, a woods hex), **scenes** (a village square, a river crossing) made of templates, and **maps** made of scenes, each tier able to override the one below.
- `asl-hex-map-generator.v39.html` and `Hex Generators/src/HexLab.js` draw each hex from a small record: base terrain, six wedge elevations and a centre elevation, features placed by wedge (woods, building, brush), one linear feature by entry and exit edge, and hexside walls and hedges, with seeded scatter for building positions.
- The same note proposes precomputing LOS for every pair of hexes. At Goodwood's size that is 1.6 billion pairs, so it does not scale; it is noted here so it is not revived.

## 3. What today's model costs

### 3.1 The pixel grid

A standard hex is 56.25 by 64.5 pixels (`BoardGeometry.cs:16-17`), so a hex is about 3,628 cells, and each cell holds 2 bytes.

| Map | Hexes | Grid cells | Grid bytes |
|---|---|---|---|
| One geomorphic board (33 by 10) | 330 | 1,161,000 | 2.3 MB |
| Goodwood (209 by 193) | 40,337 lattice hexes (the analysis counts 40,442 hex labels on the composed map) | 145,653,300 | 291 MB |
| A map of 1,000 by 1,000 hexes | 1,000,000 | about 3.6 billion | about 7.3 GB |

The grid's constructor copies its input arrays (`TerrainGrid.cs:32-34`), and `CompileResult` keeps the grid as it stood before the post-passes as well, so compiling holds two or three grids at once for a moment.

### 3.2 Measured figures (Large Campaign Map Analysis, sections 3.4 and 10)

| Process | Figure |
|---|---|
| Studio, steady, with Goodwood open | 905 MB |
| Studio, peak during the Goodwood import | 4,069 MB |
| PilotHost after the hand-off | 2,175 MB |
| Largest browser process | about 2,000 MB |
| Board SVG after the first wave | 4.9 million characters, of which 3.4 million are terrain and 1 million elevation |

The memory peaks have not been attributed stage by stage (backlog section 96, D1 and D2), but section 3.1 shows the grid is a large share of what any map of this size must hold.

### 3.3 What a hex needs

The rules read per hex: base level, the terrain of each level, depression terrain, six hexsides (terrain, hexside terrain, cliff, slope, embankment), bridge and stairway. Packed, that is tens of bytes per hex, not thousands. A million hexes in a compact per-hex table is tens of megabytes. The cost of a large map is therefore not its hexes; it is the pixel-level geometry that LOS and the Exact view need.

## 4. What an independent approach must give

1. **Design from nothing.** A map that never touched VASL, of any size, authored in the Studio.
2. **Design at the hex.** A library of hex designs, each with its look, that maps are built from; the Hex Lab is its home.
3. **Very large maps.** Size limited by what is on screen and in play, not by the whole map: memory and drawing cost grow with the view and the area being played, not the map's area.
4. **Rules unchanged.** Rules keep reading the same facts (`StepLocationFacts`, `LosFacts`, `HexFacts`). LOS in ASL is checked against terrain as drawn on the board, which is why VASL keeps a pixel grid; the design will read and cite rule A6 on its PDF page before choosing how LOS is answered.
5. **Inputs from elsewhere.** The Panzer Leader import (and later others) produces maps in the same model, as one input adapter.
6. **VASL boards keep working** and keep their proofs (`/fidelity`), and recorded games keep replaying on their pinned board versions.

## 5. Options

### Option A: Keep the model, tile the grid

The Feature Model stays the source. The grid is cut into tiles (for example 32 by 16 hexes, the size Goodwood already uses) compiled on demand and held in a bounded cache keyed by tile and board version. LOS and hex facts ask for the tiles a query crosses. Drawing uses raster tiles at far zoom (decision B4) and vectors near.

- **Gains:** the smallest change; LOS stays VASL-exact; the existing editor, renderer, and proofs stay. Memory is bounded by the cache, not by the map.
- **Costs:** compiling a tile is the heavy step, and a long LOS line or a rout search may cross many tiles. Disk still stores free-form shapes, which grow with the map's detail. Nothing changes for hex-level design: the Hex Lab would only show styles.

### Option B: Hexes as the unit of design and storage

A map is a table of hexes, each naming a **hex design** from a library plus small overrides (level, rotation, hexside marks, a linear feature's entry and exit edges). This is the three-tier idea of section 2.3: hex designs, scenes, maps.

- **Gains:** the cheapest storage and memory (bytes per hex; designs are shared, drawn once, and placed with SVG `use`); the most natural Hex Lab and Map Lab; generation from campaign terrain becomes "choose a design per hex".
- **Costs:** maps look tiled unless designs match at their edges (roads, streams, woods edges, buildings that cross hexsides). LOS needs either a hex-level reading of A6 or shapes derived from the designs, and a hex-level reading would not match VASL; the rules' fidelity becomes a design question. It is a second map model beside VASL's, with its own proofs.

### Option C: Shapes with a spatial index, no grid

The Feature Model stays the source but is stored and held in chunks with a spatial index. Hex facts are derived per chunk and kept in a compact per-hex table. LOS is answered from the shapes (a line against the obstacle shapes the index returns) instead of walking pixels. Drawing as in A.

- **Gains:** the best fit for very large maps; exact shapes; memory proportional to terrain detail, not to pixels.
- **Costs:** the largest build. Vector LOS must be proved equal to the grid LOS on every VASL board before it replaces it; the `/fidelity` harness and its LOS fixtures are the place to prove it. Hex-level design is still absent, as in A.

### Option D: Hex designs that produce shapes (B as the authoring layer over A or C)

Hex designs are small Feature Model fragments in hex-local coordinates: a woods hex, a stone building, a crossroads. Scenes group hexes. A map stores design references and overrides (as in B) and compiles them into Feature Model chunks on demand (as in A); later the chunks can feed vector LOS (as in C) without changing authoring. Free-form features stay available on top, for terrain that should not follow hexes (a winding stream, a large hill).

- **Gains:** hex-level design and its look in the Hex Lab, map painting in the Map Lab, compact storage, and the existing compiler, LOS, renderer, and proofs unchanged at first. The Panzer Leader adapter maps campaign terrain to hex designs instead of to code.
- **Costs:** two layers to keep consistent (designs and the features they emit); edge matching between neighbouring designs is still needed; the tiling of A is still needed for size.

### 5.1 Comparison

| | A: tile the grid | B: hex table | C: shapes, no grid | D: designs that emit shapes |
|---|---|---|---|---|
| Hex-level design (Hex Lab) | No | Yes | No | Yes |
| Map painting (Map Lab) | Free-form only | By hex | Free-form only | By hex and free-form |
| Memory at 1 million hexes | Bounded by tile cache | Tens of MB | Proportional to detail | Tens of MB stored, tile cache held |
| LOS fidelity with VASL | Exact | A new reading of A6 | Exact once proved | Exact (through A) |
| Change to Rules facts | None | None, if LOS is settled | None | None |
| Size of the work | Small to medium | Medium, plus LOS design | Large | Medium, then grows with C |

## 6. Memory and speed techniques that apply

These hold under any option; most are already proved in the Studio's earlier performance work.

- **Shared definitions:** draw each hex design or pattern once and place it with `use` (the grid's column pair already does this, B2).
- **Chunks:** store, compile, derive, and cache by chunk; a chunk's content hash is its version, so caches key on it and ETags follow.
- **Bounded caches with owners:** an LRU of compiled chunks per Studio, with the guidelines' rules on keys, lifetime, invalidation, and concurrent fills.
- **Compact per-hex tables:** arrays of small structs or one array per field, not a record object per hex and per hexside.
- **Raster tiles at far zoom** (B4), vectors near; or a simplified far-zoom drawing (B5) if B4 is not chosen.
- **Draw only what is in view:** labels already do (B1); terrain chunks and counters can follow.
- **Compact storage on disk:** the CBOR analysis (`Memory-And-Performance/ASL CBOR Architecture and Storage Analysis.md`) applies to map chunks as well as game records.
- **LOS answers kept per state** through the planner memo, as the guidelines require, so a long rout search does not repeat the same lines.

## 7. Where the two Labs fit

| | Hex Lab | Map Lab |
|---|---|---|
| A, C | Shows and edits the style sheet per terrain type, on sample hexes | Today's editor at a larger scale, with tiles |
| B, D | Builds hex designs and scenes: terrain, levels, hexsides, edges, look; tests them on a small sample map | Paints designs and scenes onto a map of any size; runs input adapters (Panzer Leader); shows the map's memory and drawing figures |

## 8. Answers to the guidelines' checklist (for whichever option is chosen)

- **Work per hex or step:** each option must say what one LOS check and one movement step read, and from which cache; the per-state planner memo stays the owner of answers.
- **Ownership and lifetime:** chunk caches belong to the Studio's board service, keyed by board version and chunk hash, bounded by count or bytes.
- **Pinned versions and seams:** a map's version is the hash of its chunks; a recorded game names it; seams between chunks are inside one board and need no seam ownership rule.
- **Measurements:** the Goodwood reference (`asl-gate-work/reference-goodwood-2026-11-01`) and a generated map of 1 million hexes, measured for process memory, compile time per chunk, pan and zoom frames, and LOS time.
- **Proofs:** VASL boards and recorded games must give identical answers; a new LOS path is proved on every VASL board through `/fidelity` before it is used.

## 9. Open questions for the user

1. **Look of a map built from hexes.** Is a tiled look acceptable if designs match at their edges, or must maps read like printed boards with free-form shapes (which points to D or C over B)?
2. **LOS fidelity.** Must maps not from VASL answer LOS exactly as VASL would for the same drawing (A, C, D), or is a hex-level reading of A6 acceptable for them (B)?
3. **Size target.** What is the largest map you expect: a few Goodwood-sized Situations (tens of thousands of hexes), or a theatre (millions)? It decides whether tiling (A) is enough or the grid must go (C).
4. **Earlier prototypes.** Should the three-tier idea and the v39 hex record be the starting vocabulary for hex designs, or only history?

## Related documents

- [ASL Map Studio Large Campaign Map Analysis](<Memory-And-Performance/ASL Map Studio Large Campaign Map Analysis.md>)
- [ASL Memory and Performance Guidelines](<Memory-And-Performance/ASL Memory and Performance Guidelines.md>)
- [Formation to ASL Map Alignment Analysis](<Formation to ASL Map Alignment Analysis.md>)
- [Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis](<Campaign Atlas Formation to Squad Maps and Counter Decomposition Analysis.md>)
- [ASL Unit Backlog](<ASL Unit Backlog.md>), section 96
- `docs/ASL/MapGenerators/asl-map-architecture.md` (the 2025 three-tier note)
