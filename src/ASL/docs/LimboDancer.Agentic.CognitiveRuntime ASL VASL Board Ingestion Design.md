# LimboDancer.Agentic.CognitiveRuntime ASL VASL Board Ingestion Design

**Status:** Proposed

**Parent:** [ASL Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), requirements ASL-MAP-020 to 023, ASL-MAP-030 to 036, ASL-MAP-040 to 044, ASL-MAP-071, and ASL-MAP-073.

**Scope:** How `LimboDancer.Domains.Asl.Maps.Vasl` reads a VASL board, decodes it into the canonical Terrain Grid, and how `LimboDancer.Domains.Asl.Maps` derives VASL-compatible Hex Facts from it, together with the provenance record and the F1 and F2 fidelity checks. The grid and Hex Fact types themselves, the Feature Model, and rendering are specified in the Map Model and Authoring Design and the Map Studio Architecture and Rendering Design.

**Evidence base:** VASL repository `vasl-developers/vasl`, local checkout at commit `33324f9adb3b9b97dd93700685103b6940dc9c3c`. Every statement about VASL behavior below cites the source file it was read from. Statements marked **verified** were also checked by decoding board 01 during design.

## 1. Version 1 scope

Version 1 ingests a **single standard geomorphic board**: 33 by 10 hexes, no non-standard geometry attributes in its metadata, not cropped, not rotated, with no overlay and no SSR transform applied. This matches how VASL loads one uncropped board into a game map. In the pinned checkout, 157 boards meet these conditions; ASL-MAP-02 ingests 156 of them, and every one passes F1 (**verified**). The remaining board, `bdLFT1`, declares a 644-row grid and is refused with `VASL-LOS-005`.

Boards whose metadata sets `A1CenterX`, `A1CenterY`, `hexWidth`, or `hexHeight` to a value other than the standard one (0, 32.25, 56.25, 64.5), sets `altHexGrain` true, or declares any `HexGridConfig`, boards of other sizes, and legacy boards without `BoardMetadata.xml` or `LOSData` are reported as out of version 1 scope (diagnostic `VASL-SCOPE-001`) and are not partially ingested. An attribute that restates the standard value keeps the board in scope. `snapScale` controls VASL counter snapping, not LOS geometry, and does not affect scope.

## 2. Findings that shape this design

These were established while preparing this document.

1. **LOSData decodes as expected (verified).** Board 01 `LOSData` is gzip over a Java object stream (`AC ED 00 05`). Decoding gives 33 by 10 hexes, a 1800 by 645 grid, 1,161,000 elevation and terrain pairs, and 346 stairway flags (34 set). Rendering the terrain codes in catalog colors reproduces board 01.
2. **Block framing is deterministic (verified).** The decompressed stream is 2,333,706 bytes: a 4-byte stream header, a 2,322,362-byte payload, and 11,340 bytes of framing. That is exactly 2,268 `TC_BLOCKDATALONG` headers of 5 bytes, one per 1,024-byte block, which is Java's `ObjectOutputStream` block buffer size. An encoder that follows the same rule reproduces the whole decompressed stream, not only the payload.
3. **Building-type overrides are written into LOSData when it is created (verified), but the two can drift.** `LOSDataEditor` applies `BoardMetadata.xml` building types when it *creates* `LOSData` (`VASL/LOS/LOSDataEditor.java`, "apply building-type transformations"). The runtime load path `BoardArchive.addLOSDatatoVASLMap` never reads them. All 63 board 01 overrides match the grid. Across the 156 ingested boards, however, 274 of 1,947 overrides on 29 boards disagree with the grid: 158 where the grid holds a generic building type (such as `Stone Building` or `MultipleStone`), 98 where the level count differs, and 18 with no building at the hex center. Their metadata was evidently changed without regenerating `LOSData`. Because VASL itself plays from the grid, ingestion checks the overrides and reports disagreements, but never applies them (section 6.4).
4. **Depression elevations are also baked in.** `LOSDataEditor` lowers the elevation of depression pixels by one before writing. The grid elevation is therefore final and must not be adjusted again.
5. **Archive and source directory differ in bytes (verified).** For board 01, `LOSData` has the same Git blob in `boards/bdFiles/bd01` and `boards/src/bd01`. `BoardMetadata.xml`, `data`, and `SSRControls` differ in bytes (the XML difference is line endings only), and the archive also contains a legacy entry named `BoardMetadata` without an extension. Equality between the two sources must therefore be defined per file type (section 8.2).
6. **The runtime grid configuration is not "Normal".** For a single uncropped board, VASL's runtime constructor (`Map(LinkedList<VASLBoard>, ...)`, `VASL/LOS/Map/Map.java`) sets the configuration to `HalfHexWidthLeftHexFullHeight`. This selects the adjacency rules and edge-hexside flags used during derivation (sections 5.4 and 5.5). The standalone `Map` constructor used by VASL's LOS editor defaults to `Normal` and behaves differently, so the oracle must not use that default.
7. **Hex geometry is slightly irregular.** VASL's standard hex is 56.25 by 64.5 pixels (1800 / 32 by 645 / 10, `BoardArchive.GEO_*`). Vertices use `hexWidth * 2 / 3` for side length, while hexside sample points use `cos(30°) * hexHeight / 2`. The hex is not regular, and both formulas must be reproduced exactly. VASL computes the hexside offset with `StrictMath.cos` (fdlibm), while .NET uses `Math.Cos`; the two can differ in the last bit. A geometry test proves that no sampled value on a standard board lies within 10^-6 of an integer, so truncation gives the same pixel either way.

## 3. VASL source layout

### 3.1 Locating sources

The Studio and tools take one configured root, `AslMaps:VaslRoot`, which must point to a VASL repository checkout. From it:

| Item | Path |
|---|---|
| Packaged board archives | `boards/bdFiles/bdNN` (zip without extension) |
| Unpacked board sources | `boards/src/bdNN/` |
| Shared terrain metadata | `dist/boardData/SharedBoardMetadata.xml` |
| Repository commit | `git rev-parse HEAD` in the root, or `.git/HEAD` resolved directly |

No network access is used (ASL-MAP-030). If the root is missing or has no `boards` directory, the Studio shows the unconfigured state and tests that need VASL data are skipped (ASL-MAP-073).

### 3.2 Board archive entries

| Entry | Version 1 use |
|---|---|
| `LOSData` | Required. Decoded into the Terrain Grid (section 4). |
| `BoardMetadata.xml` | Required for version 1. Parsed (section 6). |
| `bdNN.gif` (named by `boardImageFileName`) | Never read or displayed (ASL-MAP-065). Its entry name and Git blob SHA are recorded in provenance only. |
| `BoardMetadata` (no extension) | Recorded in provenance and ignored. |
| `data`, `SSRControls`, SSR images (for example `01_NoRoads.gif`) | Recorded in provenance and ignored in version 1. Deferred (section 11). |

## 4. LOSData wire format

### 4.1 Container

`LOSData` is a gzip stream (RFC 1952). Its decompressed content is a Java object serialization stream:

| Offset | Bytes | Meaning |
|---|---|---|
| 0 | `AC ED` | `STREAM_MAGIC` |
| 2 | `00 05` | `STREAM_VERSION` |
| 4 | records | block-data records until end of stream |

VASL writes `LOSData` only with `writeInt` and `writeByte` (`BoardArchive.writeLOSData`), so the stream contains only block-data records:

| Tag | Header | Payload length |
|---|---|---|
| `0x77` `TC_BLOCKDATA` | 1 tag byte, 1 unsigned length byte | 0 to 255 |
| `0x7A` `TC_BLOCKDATALONG` | 1 tag byte, 4-byte big-endian signed length | 0 to 2^31 - 1 |

Any other tag, a negative long length, or a record that runs past the end of the stream is a decode error (`VASL-LOS-002`). The payload is the concatenation of all record bodies.

Java flushes a block whenever its 1,024-byte buffer fills, and at close. A block of 256 bytes or more uses `TC_BLOCKDATALONG`; a shorter final block uses `TC_BLOCKDATA`. The encoder in section 4.4 follows this rule.

### 4.2 Payload

All integers are big-endian.

```text
int32   widthInHexes        board 01: 33
int32   heightInHexes       board 01: 10
int32   gridWidth           board 01: 1800
int32   gridHeight          board 01: 645
repeat for x in 0 .. gridWidth-1:
  repeat for y in 0 .. gridHeight-1:
    int8    elevation       signed, levels
    uint8   terrainCode     index into the terrain catalog (0..255)
repeat for col in 0 .. widthInHexes-1:
  repeat for row in 0 .. rowCount(col)-1:
    uint8   stairway        1 = stairway, 0 = none
```

The grid is **column-major** (x outer, y inner). This matches the Java write loop, and the Terrain Grid stores cells in the same order so re-encoding is a straight copy.

For standard boards, `rowCount(col) = heightInHexes + (col % 2)`, giving 10 rows in even-indexed columns and 11 in odd-indexed columns. VASL uses `heightInHexes` for every column when the metadata declares an `EqualRowCount` grid configuration. That case is out of version 1 scope, but the decoder accepts the rule as a parameter.

The payload must end exactly after the last stairway byte. Trailing or missing bytes are a decode error (`VASL-LOS-003`). Expected payload length is `16 + 2 * gridWidth * gridHeight + sum(rowCount)`, which is 2,322,362 bytes for board 01.

### 4.3 Validation on decode

| Check | Diagnostic |
|---|---|
| gzip or stream header invalid | `VASL-LOS-001` |
| unexpected record tag or truncated record | `VASL-LOS-002` |
| payload length differs from the expected length | `VASL-LOS-003` |
| `widthInHexes`/`heightInHexes` differ from `BoardMetadata.xml` `width`/`height` | `VASL-LOS-004` |
| grid size differs from the geometry implied by the hex size (1800 by 645 for standard boards) | `VASL-LOS-005` |
| a terrain code has no catalog entry | `VASL-CAT-002` (blocks verified status, ASL-MAP-034) |

A decode error never produces a partial grid (ASL-MAP-032).

### 4.4 Encoder and F1

`LosDataCodec.Encode(grid)` writes the stream header, then the payload framed in 1,024-byte blocks under the rule in section 4.1, then gzips the result.

F1 (ASL-MAP-040) compares the **decompressed stream** produced by the encoder with the decompressed source stream, byte for byte. Finding 2 shows that stream-level identity is achievable. Payload identity is reported separately, so a framing difference is not mistaken for a data difference. The compressed gzip bytes are never compared.

## 5. Board geometry for version 1

All values are for a standard board under the runtime configuration `HalfHexWidthLeftHexFullHeight`. They come from `BoardArchive.GEO_*`, `Map.getHexCenterPoint`, `Map.getGEOHexName`, `Map.getAdjacentHex`, and `Hex.initHexNew`. The model stores them as a `BoardGeometry` value, so later board types supply different values without changing the derivation.

### 5.1 Constants

| Name | Value |
|---|---|
| `hexWidth` | 1800 / 32 = 56.25 |
| `hexHeight` | 645 / 10 = 64.5 |
| `A1CenterX` | 0 |
| `A1CenterY` | 32.25 |
| `gridWidth`, `gridHeight` | 1800, 645 |

### 5.2 Hex index, name, and center

Hexes are indexed `(col, row)` with `col` from 0 and `row` from 0 within the column.

```text
centerX(col)      = A1CenterX + hexWidth * col                      (clamped to gridWidth - 1)
centerY(col, row) = A1CenterY + hexHeight * row - (hexHeight / 2) * (col % 2)
columnLetters(col) = letter repeated (col / 26 + 1) times, letter = 'A' + col % 26
                     (A..Z, AA..ZZ; board columns stop at GG)
hexRowNumber(col, row) = row + (col % 2 == 0 ? 1 : 0)
hexName = columnLetters(col) + hexRowNumber(col, row)
```

So even-indexed columns (A, C, ... GG) hold rows 1 to 10, and odd-indexed columns (B, D, ... FF) hold rows 0 to 10, where rows 0 and 10 are half hexes on the board edges. Columns A and GG are half hexes on the left and right edges. With these formulas, all 63 board 01 building overrides fall on the expected terrain (**verified**).

### 5.3 Vertices and sample points

For center `(x, y)`, with `side = hexWidth * 2 / 3` (37.5) and `v = hexHeight / 2` (32.25):

```text
vertex[0] = (x - side/2, y - v)    top-left
vertex[1] = (x + side/2, y - v)    top-right
vertex[2] = (x + side,   y)        right
vertex[3] = (x + side/2, y + v)    bottom-right
vertex[4] = (x - side/2, y + v)    bottom-left
vertex[5] = (x - side,   y)        left
```

Hexsides are numbered clockwise from the top: 0 North (vertex 0 to 1), 1 NorthEast (1 to 2), 2 SouthEast (2 to 3), 3 South (3 to 4), 4 SouthWest (4 to 5), 5 NorthWest (5 to 0).

Each hexside has an **edge sample point**: the edge midpoint moved one pixel toward the center, with `h = cos(30°) * v`, and each coordinate truncated to an integer before the offset is applied as in the Java source:

```text
edge[0] = (int x,           int(y - v + 1))
edge[1] = (int(x + h - 1),  int(y - v/2 + 1))
edge[2] = (int(x + h - 1),  int(y + v/2 - 1))
edge[3] = (int x,           int(y + v - 1))
edge[4] = (int(x - h + 1),  int(y + v/2 - 1))
edge[5] = (int(x - h + 1),  int(y - v/2 + 1))
```

`Hex.fixMapEdgePoints` then clamps: a coordinate of exactly -1 becomes 0, and one equal to `gridWidth` or `gridWidth + 1` (or the height equivalents) becomes the last pixel. The model must reproduce the truncation and clamping exactly, because sample points on half hexes fall on the board edge.

The **hex border** polygon is the six vertices rounded to integers. VASL uses it for point containment (`Polygon.contains`), and the inherent-terrain and bridge scans in section 7 depend on it.

### 5.4 Adjacency

Under `...LeftHexFullHeight`, `Map.getAdjacentHex` computes the neighbor across hexside `s` of `(col, row)` as:

| Hexside | Even col | Odd col |
|---|---|---|
| 0 N | (col, row-1) | (col, row-1) |
| 1 NE | (col+1, row) | (col+1, row-1) |
| 2 SE | (col+1, row+1) | (col+1, row) |
| 3 S | (col, row+1) | (col, row+1) |
| 4 SW | (col-1, row+1) | (col-1, row) |
| 5 NW | (col-1, row) | (col-1, row-1) |

Off-board results return no neighbor. The opposite hexside is `(s + 3) % 6`.

### 5.5 Edge hexside flags

`Hex.setHexFlags` marks hexsides off the map only when the configuration equals `Normal`, `Toplefthalfheight`, `FullHex`, `FullHexhalfheight`, or `ToplefthalfheightEqualRowCount`. `HalfHexWidthLeftHexFullHeight` matches none of these. As read from the source, the runtime therefore treats every hexside of every hex as on the map, and edge-hex sampling depends on the clamping in section 5.3. The only other way a hexside is marked off the map is the `OutOfBounds` terrain rule (section 7.2).

**Confirmed by the oracle (ASL-MAP-03):** across all 156 ingested boards, VASL reports every hexside of every hex on the map (none of the 323,856 hexsides is off). Only `OutOfBounds` terrain can take a hexside off, and no standard board has any.

## 6. Metadata

### 6.1 BoardMetadata.xml

Parsed with `System.Xml.Linq`, using the element and attribute names from `VASL/build/module/map/boardArchive/BoardMetadata.java`.

| XML | Model field | Version 1 handling |
|---|---|---|
| `boardMetadata/@name` | `Name` | required |
| `@version`, `@versionDate`, `@author` | provenance | required, recorded |
| `@boardImageFileName` | `ImageEntryName` | required; recorded in provenance only |
| `@hasHills` | `HasHills` | recorded |
| `@width`, `@height` | size in hexes | must equal the LOSData header |
| `@A1CenterX`, `@A1CenterY`, `@hexWidth`, `@hexHeight`, `@altHexGrain`, `@HexGridConfig` | custom geometry | a non-standard value means out of version 1 scope (`VASL-SCOPE-001`); see section 1 |
| `@snapScale` | recorded | counter snapping only; no effect on scope |
| `buildingTypes/buildingType(@hexName, @buildingTypeName)` | `BuildingTypeOverrides` | consistency check only (section 6.4) |
| `slopes/slope(@hex, @hexsides)` | `Slopes` | applied in derivation |
| `rrembankments/rrembankment(@hex, @hexsides)` | `RailroadEmbankments` | applied in derivation |
| `partialorchards/partialorchard(@hex, @hexsides)` | `PartialOrchards` | applied in derivation |
| board-specific colors, color SSR, overlay and underlay rules | raw | preserved as raw XML with diagnostic `VASL-META-003`; deferred |

`@hexsides` is a string of hexside digits, for example `"03"` meaning hexsides 0 and 3. As in VASL, only the first six characters are read, and any character other than `0` to `5` rejects the metadata (`VASL-META-005`); a hex name that is not a valid name is also `VASL-META-005`. A valid name for a hex that is not on the board is ignored, as VASL ignores it, with warning `VASL-META-002`. When a hex appears more than once in a section, the later entry replaces the earlier one in its original position, matching VASL's map semantics. XML comments are ignored. Unknown elements are preserved and reported (`VASL-META-001`), never dropped silently (ASL-MAP-033).

VASL's parser accepts an XML 1.1 declaration, which .NET's does not. Board 23 has one, so a 1.1 declaration is read as 1.0 with `VASL-META-006`; content that XML 1.0 forbids still fails. Board 79's metadata contains `--` inside a comment, which VASL's parser also rejects, so it fails with `VASL-META-000`.

### 6.2 SharedBoardMetadata.xml and the terrain catalog

`terrainTypes/terrainType` elements become `TerrainType` entries in a `TerrainCatalog`:

| Attribute | Field |
|---|---|
| `typeCode` | `Code` (0 to 255, unique; a duplicate is `VASL-CAT-001`) |
| `name` | `Name` (the exact VASL string; see 6.3) |
| `isLOSObstacle`, `isLOSHindrance`, `isLowerLOSObstacle`, `isLowerLOSHindrance` | LOS flags |
| `isHalfLevelHeight`, `isInherentTerrain`, `height`, `split` | height and inherent-terrain properties |
| `LOSCategory` | `LosCategory` enum mirroring `Terrain.LOSCategories`: HEXSIDE, BUILDING, MARKETPLACE, FACTORY, OPEN, ENTRENCHMENT, BRIDGE, TUNNEL, DEPRESSION, ROAD, WOODS, STREAM, WATER, OTHER. An unknown value is `VASL-CAT-003`. |
| `mapColorRed`, `mapColorGreen`, `mapColorBlue` | `MapColor` |

The rest of `SharedBoardMetadata.xml` (colors, color SSR, LOS SSR rules, overlay and underlay rules, counter rules) is out of version 1 scope. It contributes to the catalog hash, but is not modeled.

The pinned catalog has 181 `terrainType` elements (**verified** by XML parsing in ASL-MAP-01). A 182nd element, `Scrub` with code 121, is inside an XML comment and is not part of the catalog. Codes run from 0 to 213 with gaps; no terrain type uses the STREAM category. Codes and names are both unique. VASL keys its terrain table by name and the grid by code, so the catalog is keyed by code and also rejects duplicate names (`VASL-CAT-004`). Line 1839 carries stray text after its element, which the parser ignores as XML text.

Attributes are parsed as VASL's JDOM calls parse them: integers and floats from the trimmed value, and booleans accepting `true`, `on`, `yes`, or `1` and `false`, `off`, `no`, or `0`, case-insensitively. A missing or unparseable attribute is `VASL-CAT-005`; a document without a `terrainTypes` element, or not well-formed, is `VASL-CAT-000`.

### 6.3 Terrain predicates

VASL classifies terrain partly by category and partly by exact name (`VASL/LOS/Map/Terrain.java`). The catalog exposes the same predicates, implemented from the same rules and tested against every catalog entry:

| Predicate | VASL rule |
|---|---|
| `IsBuilding` | category BUILDING, FACTORY, or MARKETPLACE (`Terrain.isBuilding`; note that `isBuildingTerrain` excludes FACTORY, and derivation must use whichever the cited VASL method uses) |
| `IsHexsideTerrain` | category HEXSIDE, or a rowhouse wall, factory wall, or breach by name (`isRowhouseFactoryWallOrBreach`) |
| `IsOpen` | category OPEN, ROAD, or WATER |
| `IsDepression`, `IsBridge`, `IsTunnel`, `IsWater`, `IsRoad` | the matching category |
| `IsInherent` | the `isInherentTerrain` attribute |
| name-based predicates (`IsStream`, `IsCellar`, `IsOutsideFactoryWall`, and others used by derivation) | the exact name lists in `Terrain.java` |

The name lists are part of the derivation version (section 8.3). A change to them in a new VASL commit is a catalog change.

### 6.4 Building-type override consistency

For each override, ingestion samples the grid at the hex's center-terrain point (section 7.1, steps 1 to 5) and compares the terrain name with `buildingTypeName`:

- equal: consistent;
- not equal: `VASL-META-004`, reported with hex, expected, and actual values.

The grid remains authoritative either way (finding 3), as it is for VASL at run time. This check reproduces the existing `vasl-board-01-building-overrides.json` evidence (63 of 63 consistent) and is the parity bridge to Scenario A1 (ASL-MAP-081). Consumers of building levels must use derived Hex Facts, never the metadata overrides.

## 7. Hex-fact derivation

The derivation reproduces `Hex.resetTerrain` and the map-level steps around it, as the VASL runtime runs them for a single board. It lives in `LimboDancer.Domains.Asl.Maps` as `VaslCompatibleHexFactDerivation`, because authored boards use it too. Its inputs are a Terrain Grid, a `BoardGeometry`, a `TerrainCatalog`, stairway flags, and the slopes, railroad embankments, and partial orchards from metadata.

### 7.1 Per-hex steps

For each hex, following `Hex.resetTerrain`:

1. **Center terrain sample.** Take the first on-map point in this order and read its terrain: `(cx+1, cy-1)`, `(cx+1, cy+1)`, `(cx-1, cy+1)`, `(cx-1, cy-1)`, `(cx, cy)`, where `(cx, cy)` is the integer center point.
2. **Center elevation sample.** Use the same order, with one quirk: when the fourth point `(cx-1, cy-1)` is chosen, VASL reads the elevation at `(cx-1, cy+1)` (`Hex.getnearcenterLocationElevation`). Reproduce this.
3. **Hexside building fallback.** If the center terrain is not a building, take the first hexside `0..5` whose edge sample point is building terrain, if any.
4. **Probe building fallback.** If it is still not a building, read the four points 5 pixels from the center in the order `+x`, `-x`, `+y`, `-y`. The **last** building found wins. The source comments call this a board RO hack, but it runs on every board.
5. If there is still no terrain, use `Open Ground`.
6. **Building levels** (`Hex.addBuildingLevels`, pinned by the oracle in ASL-MAP-03), only when the center terrain is a building:
   1. the stairway is kept only if the hex had one and its terrain is not `Stone Building, 1 Level` or `Wooden Building, 1 Level`; those two types always get a stairway;
   2. a marketplace's ground location becomes `Open Ground`;
   3. upper levels 1 to the catalog height are added with the building terrain, except for `Wooden Building`, `Stone Building`, `Huts`, and `MultipleWooden`, and except for factories without a stairway;
   4. a `Cellar` location at level -1 is added for the same types, except factories;
   5. a `Rooftop` location is added one level above the highest location (a factory's is at its height plus one), except for the four types above and roofless or gutted buildings;
   6. a `Wooden Building` hex named I21 to I26, I29, I30, or J21 to J26 gets a level 1 rooftop instead (the board RO warehouse case, matched by name on any board).

   Location links are replaced, never cleared. A factory without a stairway has no upper levels to rebuild, so the second pass (section 7.2) finds the first pass's rooftop still linked and adds another above it. VASL reports this duplicate rooftop on 57 factory hexes of the BFP boards, and the derivation reproduces it.
7. **Hexside terrain**, for each hexside `s` in `0..5` that is on the map:
   1. read the terrain at the edge sample point;
   2. if metadata declares a railroad embankment on `s`, use `Rrembankment`; if it declares a partial orchard, use `PartialOrchard`;
   3. if there is no terrain, use `Open Ground`;
   4. if the terrain is not hexside terrain and the neighbor's opposite edge sample is, use the neighbor's terrain;
   5. if the result is hexside terrain, record it as the hexside's terrain and set the cliff flag for `Cliff`;
   6. if its name contains `OutOfBounds`, mark the hexside off the map.
8. **Base level** = the center elevation from step 2.
9. **Depression terrain** (`setDepressionTerrain`).
10. **Inherent terrain** (`setInherentTerrain`): scan the hex border's bounding rectangle x-major, and take the first inherent-terrain pixel whose nearest location is the hex center. If one is found, it becomes the center terrain.
11. **Hexside reset** (`resetHexsideTerrain`): the wall and hedge gap check on open edge samples, depression hexsides, and propagation of hexside terrain to the neighbor's opposite hexside. The propagation **writes to the neighbor**, so this step depends on order.
12. **Bridges, tunnels, and water** (`fixBridgesTunnelWater`): if any pixel inside the hex border is bridge or tunnel terrain, add the bridge, depression, or above-tunnel location as the source does. This includes the name-specific `II50` case.

### 7.2 Map-level steps and order

Following `BoardArchive.addLOSDatatoVASLMap` and `ASLMap.addBoardsToMap`:

1. Load the grid and stairway flags.
2. Apply railroad embankments, partial orchards, and slopes from metadata.
3. Run the per-hex steps for every hex in **column-major order** (all rows of column 0, then column 1, and so on).
4. Run step 3 a second time. The runtime calls `resetHexTerrain` twice, once inside `addLOSDatatoVASLMap` and once in `addBoardsToMap`.

**The second pass matters.** Hexside propagation (step 11) reads the neighbor's current state, so a hexside feature found only by the gap check on a later hex reaches an earlier neighbor only on the second pass. Stale location links produce the duplicate factory rooftops of step 6. The derivation reproduces both passes.

`Map.buildHillocks` also runs after each pass. It groups adjacent hillock hexes for line of sight and does not change any Hex Fact, so it belongs to the later LOS executor, not to the derivation.

### 7.3 Output: Hex Facts

For each hex, the derivation emits:

| Field | Content |
|---|---|
| `hex` | canonical name, for example `E4` |
| `col`, `row` | grid index |
| `center` | the center location: level in hex, terrain, and depression terrain |
| `locations` | every location from the lowest to the highest, including the center: level in hex, terrain, depression terrain |
| `baseLevel` | integer |
| `stairway` | boolean |
| `hexsides[0..5]` | on-map flag, location terrain, recorded hexside terrain or null, cliff, slope, railroad embankment, partial orchard, depression terrain |
| `bridge` | terrain and road level, or null |
| `centerSource` | which step decided the center sample: center sample, hexside building fallback, probe building fallback, or default |

`centerSource` supports the Studio's "why" inspection (ASL-MAP-062) and difference reports. It is excluded from F2 equality and from the oracle output.

### 7.4 Reproduce or diverge

The rule is **reproduce**. Every quirk in section 7.1 (sampling order, the elevation read in step 2, the "last wins" probe, the double pass, name-specific cases) is implemented as VASL behaves. A future deliberate divergence requires:

- an entry in this section naming the VASL behavior and the reason;
- a derivation option defaulting to VASL behavior;
- F2 reporting the divergence as expected differences, never silently passing.

No divergences are adopted in version 1. Reproduced behaviors that consumers should know about include the duplicate factory rooftops (section 7.1, step 6), the elevation read at `(cx-1, cy+1)` (step 2), and the name-matched warehouse rooftops and `II50` tunnel terrain.

## 8. Ingestion output and provenance

### 8.1 IngestedBoard

`VaslBoardImporter.Import(source, catalog)` returns either diagnostics only (failure), or an `IngestedBoard` containing:

- `SourceIdentity`: `vasl:bdNN@<LOSData blob SHA>`. The board version hash of section 8.3 is computed when the canonical board package is implemented; until then the source identity plus the provenance record identify the data;
- `BoardGeometry`;
- `TerrainGrid` (codes and elevations, column-major) and stairway flags;
- parsed `BoardMetadata`, including raw preserved elements;
- `Provenance` (section 8.2);
- `Diagnostics`.

Hex Facts are not part of `IngestedBoard`. They are computed from it by the derivation and cached against the board version hash.

### 8.2 Provenance record

| Field | Board 01 at the pinned commit |
|---|---|
| VASL repository commit | `33324f9adb3b9b97dd93700685103b6940dc9c3c` |
| source kind | `source-directory` or `archive` |
| `LOSData` Git blob SHA | `8d77d26222b7bb21d8c1fdda6ba05b447f63c317` |
| `BoardMetadata.xml` Git blob SHA (source directory) | `e91b0d99a7a788812444753cae245841875a8de6` |
| archive file Git blob SHA (when archive) | `6fe51846f20d4420480fcf02a3b85d93804223d6` |
| `SharedBoardMetadata.xml` Git blob SHA | `e8d2254ce19a665fa8a62c1697b297d1e0b843dc` |
| board image entry name and Git blob SHA | `bd01.gif`, `fe13040a197c3e040c5394362cb04d73cd832c9e` |
| metadata version, date, author | `6.9`, `Jan 2025`, `TR` |
| importer and derivation versions | semantic versions of the two components |

Git blob SHAs are computed with Git's algorithm (`blob <length>\0<bytes>`, SHA-1) over the bytes as stored in Git, so they match `git ls-tree` without shelling out to Git. For working-tree files, the importer reads the blob from `.git` when available. Otherwise it hashes the file bytes and marks the hash as working-tree, because checkout line-ending conversion can change text files.

**Equality between archive and source directory** (ASL-MAP-031):

- `LOSData`: identical bytes are required;
- XML: semantic equality after parsing (same elements, attributes, and values; comments and whitespace ignored);
- other entries: recorded, not compared.

When both forms of a board are available, ingestion reads both, reports `VASL-SRC-001` for any `LOSData` difference, and `VASL-SRC-002` for any XML semantic difference. The **source directory** is the pinned identity, which matches the existing board 01 evidence (`BoardMetadata.xml` blob `e91b0d99...`).

### 8.3 Version identities

- **Board version hash:** SHA-256 over the canonical encoding of geometry, grid, stairways, the metadata fields used by derivation, and the catalog hash. Any change to the source data produces a new hash (ASL-MAP-072).
- **Catalog hash:** SHA-256 over the canonical catalog, including the predicate name lists.
- **Derivation version:** the semantic version of `VaslCompatibleHexFactDerivation`. Hex Facts are identified by `(board version hash, derivation version)`.

## 9. Fidelity checks

### 9.1 F1

For each ingested board: decode, encode, decompress both, compare streams. Report `F1-PASS`, `F1-FRAMING` (payloads equal, framing differs), or `F1-FAIL` (payloads differ, with the first differing offset mapped to header, grid cell `(x, y)`, or stairway `(col, row)`).

### 9.2 F2 oracle harness

A Java harness, `src/ASL/tools/vasl-hexfact-oracle/`, produces reference Hex Facts using VASL's own classes.

- **Build:** `generate-fixtures.ps1` resolves VASL's dependency classpath (VASSAL 3.7.27 and its dependencies) with the checkout's Maven wrapper, then compiles the harness with `javac` against the checkout's `src` directory, which compiles only the VASL classes the harness uses. VASL's own Maven build is not used, because its pom compiles for Java 11 and cannot read VASSAL 3.7's Java 17 classes. Nothing is written into the VASL checkout. The harness is a development tool. It is not a dependency of any .NET project and is not distributed (LGPL 2.1 applies to VASL; the harness stays separate).
- **Construction:** the harness reproduces the runtime path for one uncropped, unrotated board without VASSAL's game-module objects:
  1. parse `SharedBoardMetadata.xml` and `BoardMetadata.xml` with VASL's parser classes, which need no VASSAL runtime objects;
  2. construct `VASL.LOS.Map.Map` through its standalone constructor with the standard geometry (section 5.1), passing `HalfHexWidthLeftHexFullHeight` as the grid configuration so that adjacency and edge flags behave as at run time;
  3. read `LOSData` with the same loop as `addLOSDatatoVASLMap`: grid, stairways, and `resetHexAndLocationNames`;
  4. apply railroad embankments, partial orchards, and slopes; call `resetHexsideLocationNames`; call `resetHexTerrain`; then call `resetHexTerrain` again, as the runtime does.
- **Construction check (pending review):** a reviewer compares at least 20 hexes in board 01 (including edge half hexes, E4, a wall or hedge hexside, and a stairway hex) with a live VASL session's hex information, and records the result. This checks the harness's construction of the map, which F2 alone cannot: F2 shows that the C# port matches the harness, and the harness runs VASL's code, but only a live session shows that the construction matches what players see.
- **Output:** the Hex Fact JSON of section 7.3 without `centerSource`, in canonical form (keys in ordinal order, hexes in column-major order, one hex per line), with a header recording the VASL commit, harness version, grid configuration, and the committed Git blob ids of `LOSData`, `BoardMetadata.xml`, and `SharedBoardMetadata.xml`, read from the checkout's git index. The index blob is required because some VASL metadata files are committed with CRLF line endings.
- **Fixtures:** gzipped, one per ingested board (156 boards, about 1 MB in total), under `src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bdNN.hexfacts.json.gz`. They contain derived facts only, as allowed by ASL-MAP-073.

### 9.3 F2 comparison

The C# test ingests the board from the configured local source, derives Hex Facts, and compares them with the fixture field by field. Differences are reported per hex, hexside, and field. The test is skipped, not failed, when the local VASL source is absent. A fixture whose recorded source hashes do not match the local source fails with `F2-SOURCE-MISMATCH`, never with a false pass.

**Result (ASL-MAP-03):** all 156 ingested boards pass F2 with no differences. They exercise hedges (94 boards), walls (82), cliffs (28), bocage (4), rowhouse walls, hills and negative base levels, center and hexside depressions (54), bridges (57 hexes on 35 boards), cellars and rooftops, factories, a marketplace, and inherent crags, graveyards, hillocks, and debris. Railroad embankments, partial orchards, tunnels, and `OutOfBounds` hexsides occur on no in-scope board; synthetic tests cover `OutOfBounds` and the annotations.

## 10. Component design (`LimboDancer.Domains.Asl.Maps.Vasl`)

| Type | Responsibility |
|---|---|
| `VaslSourceOptions` | `VaslRoot` configuration and validation |
| `VaslRepository` | enumerates boards, resolves commit, computes Git blob SHAs |
| `IVaslBoardSource` | one board's entries, from `VaslArchiveBoardSource` (zip) or `VaslDirectoryBoardSource` |
| `JavaBlockDataReader`, `JavaBlockDataWriter` | Java stream header and block-data framing (section 4.1) |
| `LosDataCodec` | payload decode and encode (sections 4.2 to 4.4) |
| `BoardMetadataParser` | section 6.1 |
| `SharedBoardMetadataParser` | builds the `TerrainCatalog` defined in `Maps` (sections 6.2 and 6.3) |
| `VaslBoardImporter` | orchestration, scope check, override consistency, provenance, diagnostics |
| `VaslBatchImporter` | every board, with a per-board result and no fail-fast (ASL-MAP-035); section 10.1 |

`LimboDancer.Domains.Asl.Maps` holds `BoardGeometry`, `TerrainCatalog`, `TerrainGrid`, `HexFacts`, and `VaslCompatibleHexFactDerivation`, none of which depend on VASL file formats.

**Memory and performance.** A standard grid is 1,161,000 cells stored as two arrays (`byte[]` codes, `sbyte[]` elevations), about 2.3 MB per board. Decoding streams through `GZipStream` and a block reader without buffering the full stream. Derivation touches a bounded neighborhood per hex, apart from the border-rectangle scans in steps 10 and 12. Measured in ASL-MAP-05 (Release build, one development workstation, boards run in parallel): the full batch of 297 board directories finishes in about 11 seconds. Per verified board, the median is 34 ms to import, 396 ms to derive, and 51 ms for F2; the slowest board takes 2.7 seconds in total. Derivation is the dominant stage, as expected from the border-rectangle scans.

**Determinism.** No culture-sensitive formatting, no hash-ordered iteration in outputs, and no floating-point results in canonical output other than geometry constants serialized with invariant formatting.

### 10.1 Batch runs (ASL-MAP-05)

`VaslBatchImporter.Run` takes every board directory, or a named subset, and runs each board through the scope check, import and F1, derivation, F2 against the fixture directory, and any additional checks the caller supplies. The Studio adds rendering checks (Architecture and Rendering Design, section 9.2). Boards run in parallel. Results keep VASL board-name order, and one board's failure never stops the batch; an unexpected exception becomes `BATCH-001` on that board. Cancellation stops the run without a report.

Each board ends with one outcome:

| Outcome | Meaning |
|---|---|
| `Verified` | F1 pass, F2 pass, and every gating additional check passed; informational checks such as F3 are recorded but do not decide the outcome |
| `Ingested` | ingested, but F2 has no fixture, F2 failed, or a check failed; the reason is recorded |
| `Failed` | refused with an error diagnostic |
| `OutOfScope` | `VASL-SCOPE-001`, with the reason |

The result is a `FidelityReport`: report version, start time, duration, VASL commit, catalog blob, tool versions (importer, derivation, report, and renderer when rendering checks run), and one entry per board. Each entry has its outcome, reason, `LOSData` and metadata blob ids (recorded for failed boards too), F1 status, F2 status with every coordinate-level difference, the additional checks, the diagnostics, and stage timings. It serializes to indented JSON with camel-case names and enum names as strings.

Scenario M2 on the pinned checkout: 156 boards verified, `bd79` failed with `VASL-META-000`, `bdLFT1` failed with `VASL-LOS-005`, and 139 directories out of scope. The 139 include 5 directories whose names are not valid board references, which the Studio library does not list.

## 11. Deferred VASL features

| Feature | Where it enters later | Effect on the canonical model |
|---|---|---|
| SSR terrain transforms (`colorSSR`, `LOSSSRules`, `SSRControls`) | applied to a copy of the grid before derivation, as `board.applyColorSSRulestoTerrainElevationGrids` does | none: the transformed grid is a new board version with the SSR set in its provenance |
| Overlays | pasted into the grid before derivation, as `ASLMap.adjustLOSForOverlays` does | none: same as SSR |
| Board rotation (inverted boards) | grid and hex-grid flip (`Map.flipTerrainAndElevationGrids`, `flipthehex`) | `BoardPlacement` transform; the source grid is unchanged |
| Multi-board composition and cropping | half-hex seam merging from the `addLOSDatatoVASLMap` grid loop; crop configurations | composed grid built from placed boards |
| Custom geometry, alternate hex grain, `EqualRowCount`, a/b boards, HASL and other non-geomorphic maps | `BoardGeometry` variants and the matching `getHexCenterPoint` and `getGEOHexName` branches | new geometry values; derivation unchanged |
| Legacy V5 `data` files and boards without `LOSData` | not planned; reported as unsupported | none |

## 12. Resolved issues (ASL-MAP-02 and ASL-MAP-03)

1. **Edge hexside flags (section 5.5).** Confirmed: under the runtime configuration no hexside is off the map.
2. **Second derivation pass (section 7.2).** It matters: hexside propagation and stale location links both depend on it.
3. **Building levels (section 7.1, step 6).** Pinned by the oracle, including the duplicate factory rooftops.
4. **Hillocks.** `buildHillocks` does not change Hex Facts; it moves to the LOS executor.
5. **Harness construction.** VASL's parser classes run without VASSAL runtime objects. The live-session spot check of section 9.2 remains to be recorded.

## 13. Diagnostics

| Code | Severity | Meaning |
|---|---|---|
| `VASL-SRC-000` | error | `VaslRoot` missing or not a VASL checkout |
| `VASL-SRC-001` | error | `LOSData` differs between archive and source directory |
| `VASL-SRC-002` | warning | metadata differs semantically between archive and source directory |
| `VASL-SRC-003` | error | the board source, or its `LOSData` in a comparison, does not exist |
| `VASL-SCOPE-001` | info | board out of version 1 scope (reason given) |
| `VASL-LOS-001` to `005` | error | LOSData container, framing, length, or size errors (section 4.3) |
| `VASL-META-001` | warning | unknown metadata element preserved |
| `VASL-META-000` | error | `BoardMetadata.xml` not well-formed, or without a `boardMetadata` root |
| `VASL-META-002` | warning | metadata names a hex that is not on the board; ignored, as in VASL |
| `VASL-META-003` | info | deferred metadata element preserved raw |
| `VASL-META-004` | warning | building-type override inconsistent with grid |
| `VASL-META-005` | error | invalid hex name, hexside digit, or required attribute in metadata |
| `VASL-META-006` | info | XML 1.1 declaration read as XML 1.0 |
| `VASL-CAT-000` | error | `SharedBoardMetadata.xml` not well-formed or without `terrainTypes` |
| `VASL-CAT-001` | error | duplicate terrain code in catalog |
| `VASL-CAT-002` | error | grid uses a code absent from the catalog |
| `VASL-CAT-003` | error | unknown `LOSCategory` value in catalog |
| `VASL-CAT-004` | error | duplicate terrain name in catalog |
| `VASL-CAT-005` | error | missing or unparseable `terrainType` attribute |
| `BATCH-001` | error | unexpected exception while running one board in a batch; the batch continues |

A board is eligible for verified status (ASL-MAP-044) only with no error diagnostics, F1 pass, and F2 pass.

## 14. Tests

| Test | Data | Runs without VASL checkout |
|---|---|---|
| block framing read and write, including both tag sizes and malformed records | synthetic | yes |
| payload decode and encode, including size and row-count rules | synthetic grids from the encoder | yes |
| geometry: centers, names, vertices, edge sample points, clamping, adjacency | constants in section 5 | yes |
| catalog predicates against every catalog entry | small committed extract of names, codes, and categories | yes |
| derivation steps, each against a synthetic grid built to trigger it | synthetic | yes |
| F1 for board 01, then every board in scope | local VASL | skipped |
| override consistency for board 01 (63 of 63) | local VASL | skipped |
| F2 for board 01, then every fixture | local VASL plus fixtures | skipped |
| archive and source-directory equality | local VASL | skipped |
| scope check agrees with import for every board | local VASL | skipped |
| full batch: every fixture board verified, every other outcome explained, report JSON round trip, timings | local VASL plus fixtures | skipped |
| batch with additional checks, a subset, progress, and cancellation | local VASL | skipped |
| report JSON round trip | synthetic | yes |

## 15. Requirement coverage

| Requirement | Section |
|---|---|
| ASL-MAP-020, 021, 023 | 5 |
| ASL-MAP-022 | 7.3 names map to `bd01:<hex>:<level>`; the mapping itself is in the Model design |
| ASL-MAP-030 | 3.1 |
| ASL-MAP-031 | 8.2 |
| ASL-MAP-032 | 4 |
| ASL-MAP-033, 034 | 6 |
| ASL-MAP-035 | 1, 10 |
| ASL-MAP-036 | 11 |
| ASL-MAP-040 | 4.4, 9.1 |
| ASL-MAP-041, 042 | 9.2, 9.3 |
| ASL-MAP-044 | 13 |
| ASL-MAP-071 | 8.2 |
| ASL-MAP-073 | 3.2, 9.2 |
| ASL-MAP-013 | 6.4, 7 |
