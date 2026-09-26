# ASL Unit Composed Maps Design

**Status:** Proposed. Designed before code, on `feature/asl-unit-step12-design`; to be built in the order of section 10.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 12, and acceptance scenarios U3 and U13 (section 14). Maps: [ASL Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), ASL-MAP-023, 024, and 080.

**Related documents:**

- the [ASL Map Model and Authoring Design](<ASL Map Model and Authoring Design.md>), whose composed map is a list of board placements;
- the [ASL Unit Governed Writes Design](<ASL Unit Governed Writes Design.md>) (step 7), whose setup action and Play page this step extends;
- the [ASL Unit Occupied and Concealed Entry Design](<ASL Unit Occupied and Concealed Entry Design.md>) (step 8), whose entry facts this step computes across a seam.

No rule is opened and no reviewed package changes, so this design cites no rulebook pages.

## 1. Outcome

A live game can be played on several placed boards, some of them reversed:

- **The game records the placement.** Each board keeps its reference and version, and gains its slot and whether it is reversed. Positions stay board-relative, such as `bd21:N5`, on a reversed board too (U3).
- **Setup places boards.** One board needs no placement. Several boards are placed by slot, or copied from a map saved in Map Studio.
- **Entry works across a seam.** A squad on one board may enter an adjacent building on another. The entry of steps 7 to 11 then runs unchanged for a board 01 building the reviewed cases cover (U13), and any other target stays outside the reviewed cases.
- **The Play page draws the map** with the units each viewer may see.

## 2. Principles

- **Board-relative positions.** A position names its board and that board's own hex, never a map hex (ASL-UNIT-024). Map coordinates exist only inside the composed read and the drawing.
- **One layout rule.** The game, the read, and the drawing lay boards out with the map builder's rules (VASL-MAP-001 to 004), so a placement the builder rejects is never played.
- **Nothing guessed at a seam.** Where two boards meet, a fact is used only when both boards give it. Where they disagree, the fact is unknown and the reviewed resolvers stay indeterminate (ASL-UNIT-061).
- **Old games replay.** A game recorded before this step has boards and no placement. It still replays, reads, and draws as before.

## 3. The layout

The map builder lays boards out in `VaslMapBuilder.Layout` from each board's geometry alone: slots, hex size, rows, and the map hex of each board hex. Building the terrain grid then needs the VASL board files.

- **A public layout.** The layout becomes a public, geometry-only `MapLayout`, built from placements and board geometries. `VaslMapBuilder.Build` uses it, so the built maps are unchanged.
- **What it answers.**
  - `Locate(board, hex)`: the map hex of a board hex.
  - `OwnerOf(mapHex)`: the board and hex name that own a map hex.
  - `Names(mapHex)`: every board hex name at a map hex. A half hex on a seam has two names, one per board.
- **Shared half hexes.** Boards abut on half hexes: the 0 and 10 hexes of the lettered columns with eleven hexes, and the A and GG columns. Such a hex belongs to the board placed later (row, then column), as the builder already records. A position in it uses the owner's name. The other board's name for it is refused.
- **Reversal.** A reversed board is rotated 180 degrees and keeps its own hex names, as `VaslMap.MapHex` already does.

## 4. The composed read

The map read API (ASL-MAP-080) gains `ComposedMapRead`, built from the layout and one `BoardHandle` per placed board. The per-board read is unchanged.

| Read | Meaning |
|---|---|
| `Resolve(location)` | The location's facts, from its own board. For a shared half hex, the facts are definitive only when both boards give the same base level and the same terrain at the location's level; otherwise the read is nondefinitive with a diagnostic. |
| `Neighbor(location, side)` | The board-relative hex across a hexside, on the same board or across a seam, named by its owner. |
| `Distance(from, to)` | The distance in hexes between two board-relative hexes, measured on the map. |
| `Crossed(from, to)` | For adjacent hexes, the hexside as each hex records it. Across a seam, each board records its own edge; a fact counts only when both records agree. |

Board handles still come from `IBoardCatalog` at the versions the game records. A board whose status is not Verified or AuthoredValid keeps every fact nondefinitive (ASL-MAP-044).

**As built** (part 1).
- `MapLayout` (Composition) holds the layout `VaslMapBuilder.Build` used to compute inline; the builder now calls it, and `VaslMap.MapHex` delegates to it.
- A board placed twice is refused by `ComposedMapRead.Create` (MAP-READ-003), not by the layout, so the maps the builder accepts are unchanged.
- `Resolve` gives no read, with MAP-READ-004, for a shared hex whose boards disagree, since a read's definitiveness follows its board's status. Callers already treat a missing read as nondefinitive.
- `Neighbor` takes a direction on the map. On a reversed board that is the opposite of the same side on the board's own hexes, and `Crossed` turns it back before reading each board's hexside.
- Tests:
  - Maps, `ComposedMapReadTests`: neighbours and distances across a seam, a reversed board above board 01 (its AA1 above G1), U3's rotated `bd21:N5`, the owner rule and disagreement for a shared hex, agreement of the two edge records of a seam hexside, and the refusals of `Create`.
  - Maps, `CompositionTests`: the layout agrees with the built map on two rows of two synthetic boards, with and without a reversed board. The design's comparison against the VASL-backed fixtures is covered by these synthetic builds, since the builder now uses the layout itself.

## 5. The game record

`MapInPlay` gains an optional placement for each board:

| Field | Meaning |
|---|---|
| `board`, `version` | As now |
| `column`, `row` | The board's slot, as in `BoardPlacement` |
| `reversed` | Whether the board is rotated 180 degrees |

- **All or none.** Either every board has a placement or none does. A game with one board and no placement is a single-board game. A game with several boards and no placement is an old game: it replays, but its entry facts never cross between its boards.
- **Reference.** `MapInPlay.Reference` is the saved map's reference when setup copied one, and otherwise the placements in their compact form, such as `bd21@0,0/r bd01@0,1`.
- **Replay checks** (UNIT-STATE-010, as the other map checks):
  - a board placed twice, or two boards in one slot;
  - a row with a gap or more than three boards, or a missing row (VASL-MAP-001 and 003);
  - placements on some boards and not others;
  - a position in a shared half hex under the name of the board that does not own it.

  The hex-size and grid checks (VASL-MAP-002 and 004) need the board geometries, so setup runs them, not replay.
- **The reader and writer** round-trip the new fields. A record without them reads as before.

## 6. Setup

`asl.game.setup` keeps `boards` and accepts, for each board, either its reference as now or an object with `board`, `column`, `row`, and `reversed`. Mixing the two forms is refused. `start.map` may name a saved map; the Play page sends that map's placements with it.

The planner:

1. reads each board as now, and refuses one that is not Verified or AuthoredValid;
2. builds the `MapLayout` from the placements and the board geometries, and refuses with the builder's diagnostic when it fails;
3. checks each placed unit's position against the layout, including the owner rule for shared half hexes;
4. records the placements in the `game-started` event.

The Play page's setup offers:

- one board, as now;
- several boards, each with its slot and a reversed box;
- a saved Map Studio map, whose placements it copies.

## 7. Entry across a seam

`GamePlanner.EntryFacts` reads through a `ComposedMapRead` for the game:

- **Adjacency.** `isAdjacentGroundLevelOrdinaryBuilding` uses the composed distance, so a mover on another board can be adjacent to the target.
- **The crossed hexside.** `hasNoRoadBypassElevationOrAdditionalTerrain` uses `Crossed`, so a seam hexside counts only when both boards agree.
- **Unchanged.** A single-board game reads as now. The facts of a game with several unplaced boards stay per board.

The Scenario A1 packages bind only the target's terrain to board 01 (`bd01:{hex}:0`). The previous location is any location string. An entry from another board into a board 01 building the reviewed cases cover is therefore decided exactly as an entry from board 01: the reveal, Random Selection, the declaration, and the OVR all follow. An entry into a building on any other board is refused by `BoardCatalogTerrainEvidence` as outside the reviewed board, as now.

**The U13 case.** Board 01's reviewed building G1 (wooden) lies in hex row 1 of a column with ten hexes, so its top hexside is the board's edge. With another board placed in the row above, that board's G10, or AA1 when it is reversed, lies across that hexside. A squad there may enter G1. F1 (wooden), R1, and S1 (stone) are also in row 1 of board 01. F1's neighbour F0 is a shared half hex that board 01 owns when it is placed later.

**As built** (part 2).
- `PlacedBoard` gains an optional `BoardSlot(Column, Row, Reversed)`. `MapInPlay.IsPlaced` is true when every board has one, and `Placements()` gives them as map placements. The writer omits `reversed` when false.
- The slot checks that need no geometry are `MapLayout.CheckSlots`, shared by the layout and replay. `ILocationChains` gains `Geometry(board)`, a default method returning null, so replay lays the map out, and checks positions in shared half hexes, whenever the chains give every board's geometry. The Studio's and the planner's chains always do.
- Replay also refuses a board placed twice, whether the map is placed or not.
- Setup accepts `boards` as references or as placed objects, not mixed. A placed map's reference is `start.map` when given, and otherwise the placements with board references, such as `bd02@0,0/r bd01@0,1`.
- `EntryFacts` reads through `ComposedMapRead` when the map is placed, and through the target's board as before otherwise.
- An entry across a seam into a building on another board is refused on confirmation, as every concealed entry's outcome is withheld from the mover until then.
- Tests:
  - Units, `MapPlacementTests`: the round trip, an unplaced record, a placed game's replay, four placement refusals, the owner rule for a shared half hex, and U3.
  - Play, `SeamEntryTests`: U13 from bd02's G10 and from a reversed bd02's AA1 into bd01's G1, an entry into bd02's building refused as outside the reviewed board, and three setup refusals (mixed boards, a row gap, a shared hex under the wrong name).

## 8. The Play page map

- **Drawing.** The Play page draws the game's map with the units each viewer may see, at the shown revision, using the board viewer's renderer and the game's overlay (ASL-UNIT-071). A single board draws that board.
- **A transient map.** `MapService` gains `LoadPlacements`, which builds a map from placements without saving it, cached by placement text, so a game whose map was never saved can still be drawn.
- **Without VASL.** A composed map needs the VASL checkout. Without it, the page says so and shows its tables as now.
- **Locations.** Selecting a location in the page's tables highlights its hex on the map.
- **The link.** The "View on" link opens the game's saved map when setup copied one, and otherwise its first board, as now.

## 9. Tests

**Maps:**
- `MapLayout` gives the same map hexes as `VaslMap` for the composition fixtures, including a reversed board; that comparison needs the VASL root.
- On synthetic geometries: a reversed board keeps its names, the owner rule holds for shared half hexes, and neighbours and distances cross a seam.
- `ComposedMapRead` refuses a shared half hex whose boards disagree, and `Crossed` gives an unknown fact where the two edge records disagree.

**Units:**
- A placed map round-trips through the reader and writer, and a record without placements reads as before.
- Replay refuses each check of section 5.
- U3: a unit at `bd21:N5` on a map with bd21 reversed keeps that position, and `MapLayout.Locate` places it in the rotated map hex.

**Play**, with board 01 from the oracle facts and a synthetic second board:
- Setup records placements, and refuses a layout the builder rejects and a mixed `boards` list.
- U13: a squad in the other board's G10 enters board 01's G1 and the entry commits as from board 01; the same with the other board reversed, from its AA1.
- An entry across a seam into a building on the other board is refused as outside the reviewed board.
- A single-board game's entry facts are unchanged.

**Map Studio:** setup with placed boards and with a saved map, and the map drawn for a viewer, with a concealed unit shown only as a sealed presence to the other side. The drawing tests need the VASL root.

The LF clone and Docker verify each part as before.

## 10. Build order

1. `feature/asl-unit-composed-read`: sections 3 and 4.
2. `feature/asl-unit-placed-boards`: sections 5 to 7, U3 and U13.
3. `feature/asl-unit-play-map`: section 8 and the Play page's setup of section 6.

This design is merged first, on its own branch.

## 11. Not in this step

- Overlays and scenario-specific terrain transforms (ASL-MAP-036, ASL-MAP-065).
- Cropped boards and half boards.
- Movement other than the entry of steps 7 to 11.
- LOS across a seam (ASL-MAP-082).
- A game's map changing after setup.
