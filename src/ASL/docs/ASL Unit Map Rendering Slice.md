# ASL Unit Map Rendering Slice

**Status:** Proposed first slice; analysis only

**Date:** 2026-09-24

**Baseline:** `main@cbf0ee3` (ASL-MAP-04 board viewer)

## Goal and boundary

Display one or more unit counters at named locations on a map built by the parallel map effort. The first result should let a viewer identify a counter, see its side and displayed face, tell which hex and level it occupies, and inspect a stack of counters. A user-supplied placement snapshot is enough for this slice. This work does not establish the legal presence, movement, ownership, concealment, combat characteristics, or authority of any unit.

The [broader unit domain model analysis](<ASL Unit Domain Model Analysis.md>) remains background for later game-state and rules work. Its definition catalog, event stream, state transitions, rulebook-wide inventory, and Scenario A1 execution adapter are **not prerequisites** for putting counters on a map.

## Existing map integration points

| Existing code | What this slice uses |
|---|---|
| `Maps.Coordinates.BoardLocation` | Typed board, hex, and level (`bd01:E4:0`); the optional hexside form is out of this first slice. |
| `Maps.Geometry.BoardGeometry` | `IndexOf(HexName)` and `CenterDot(HexIndex)` position a counter in the board's SVG coordinate space. Check that the hex exists before rendering. |
| `Maps.Rendering.BoardRenderer` | Exact and HexFacts terrain views remain pure, versioned board renders. A separate `units` group is drawn above their terrain, grid, and labels. |
| Map Studio `BoardViewer` and `boardViewport.js` | The inline SVG already supports zoom, pan, layer loading, board-coordinate clicks, and a hex inspector. A unit overlay can share that viewport and report a clicked unit ID to the inspector. |
| `RenderCache` and `/render/{board}/{version}/{view}/{file}` | These currently cache immutable board-only SVG. Unit placements change independently, so they must not be served under a board-version-only immutable URL or included in a board cache entry. |

This is a presentation contract owned by the ASL unit/map integration, not a new terrain feature or a field in `HexFacts`. The map renderer should expose its geometry and viewport contract; the unit overlay should consume it without changing board derivation, board versions, or golden terrain SVGs. Coordinate with the map effort on the SVG group insertion and click handling, since those files are being developed in parallel.

## Minimum display input

The initial input can be an in-memory or fixture-backed `UnitPlacementSnapshot`. Names are illustrative, not existing contracts:

| Field | Purpose |
|---|---|
| `boardRef`, `boardVersion` | Select the exact displayed board; reject a mismatched board/version instead of silently showing a counter elsewhere. |
| `snapshotId` or `revision` | Identify the set of placements for replacement or refresh; never use it as a terrain board version. |
| `placements[]` | Zero or more displayed counters, each with stable `placementId`, `location: BoardLocation`, `sideId`, `counterKey`, `faceKey`, `label`, and optional `stackOrder` and `facing`. |
| `counterArt` mapping | Map `(counterKey, faceKey)` to a permitted SVG/image asset or a deterministic generated counter. A missing asset gets an obvious labeled fallback. |

`placementId` identifies a visible counter instance for updates and clicks; it is not yet a rules-engine `UnitInstance` ID. `sideId`, `label`, `faceKey`, and `facing` are explicit display data. The renderer must not infer broken status, nationality, concealment, owner, or combat values from a label or an image filename. The supplying caller chooses what its audience may see; a demo fixture contains no hidden state. If a later authorized source filters by perspective, the overlay renders only the filtered snapshot it receives.

For a first demonstration, use a small, checked-in example placement set against a known board and locally defined counter art. Do not make a full rulebook catalog, VASL counter extraction, or live-game service a condition of rendering the first counters. Asset provenance and usage rights must be known for any imported artwork; simple original counters are sufficient initially.

## Placement and display behavior

1. Resolve each `BoardLocation` against the loaded `BoardRef` and geometry. Reject malformed names, off-board hexes, mismatched boards, and unsupported hexside placements with a diagnostic identifying the placement. Preserve the level value for its badge and inspector; all levels at the same hex share one 2D map anchor in this slice.
2. Group placements by hex and sort by explicit `stackOrder`, then `placementId`, so repeated renders are deterministic. Give counters small, stable offsets around the hex center. Keep the count and full list available in the inspector when a stack is too large to display clearly at ordinary zoom. Do not interpret visual order as ASL stacking legality.
3. Render original counter art or a labeled fallback in board SVG units, with a side cue, visible face, level badge when needed, and optional facing cue. Keep counters legible at normal zoom, keyboard-focusable if interactive, and identifiable by an accessible label. A counter may visually extend beyond its hex; avoid clipping at board edges or under the legend.
4. Place a dedicated `layer-units` group after terrain, grid, and hex labels. Its content depends on placement snapshot and art version, not on terrain `RenderCache`. On snapshot refresh, replace only that group, retaining the current viewport and selected hex; a counter click should identify the placement without accidentally selecting underlying terrain.
5. Support an empty snapshot, one counter, counters in different hexes, and several counters in one hex. Board-view changes between Exact and HexFacts keep the same placement snapshot and anchor coordinates. Board changes require a new placement snapshot or an empty overlay.

The overlay may initially be generated server-side as a standalone SVG fragment, then inserted into the existing inline SVG by the Studio. Any endpoint for it must key caching by both board and placement revision (and art version), or use a non-immutable response. The existing board-only endpoints and their year-long immutable cache policy remain specific to terrain layers.

## First acceptance cases

| Case | Observable result |
|---|---|
| One unit at `bd01:E4:0` | Counter centers on E4 in both available views; its label, side, face, and selected location are visible. |
| Two different hexes | Each counter anchors to its own center; switching views and zooming preserves placement. |
| Three counters in one hex | Stable ordering and offsets; inspector lists all three with their levels, even when overlap limits visible text. |
| Upper-level placement | Correct hex anchor, explicit level badge and inspector value; no unsupported claim about in-building legality. |
| Changed snapshot | Counter overlay updates without a terrain rerender or a stale immutable-cache hit; pan and zoom are retained. |
| Invalid or incomplete input | Wrong board/version or off-board location is diagnosed; missing art uses the labeled fallback; no counter is silently moved to a neighboring hex. |
| Click and accessibility | Counter click selects the intended placement; keyboard and accessible name identify it; clicking bare terrain still selects the hex. |

## Next implementation slice

Define the small placement DTO and original placeholder art; implement a deterministic SVG overlay renderer with geometry and stack tests; integrate the overlay into Map Studio's viewport and inspector; demonstrate the acceptance cases on an existing board. Keep it read-only. Later, if Scenario A1 or a game service supplies authoritative unit state, adapt *its authorized visible projection* to this display contract without letting the display DTO become an authority for game decisions.
