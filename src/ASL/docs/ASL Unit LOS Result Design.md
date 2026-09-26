# ASL Unit LOS Result Design

**Status:** Built in the order of section 6. Part 1 is recorded in section 4, part 2 in section 3, and part 3 in section 5.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 16, and acceptance scenario U17 (section 14).

**Builds on:** the LOS designs of steps 13 to 15 ([LOS](<ASL Unit LOS Design.md>), [slice 2](<ASL Unit LOS Slice 2 Design.md>), [slice 3](<ASL Unit LOS Slice 3 Design.md>)). Their principles hold. The Java oracle is used for fixtures only, provisionally.

## 1. Outcome

The read reports what VASL's LOS reports beyond blocked or clear, from center and hexside locations alike, and a player can check LOS from a unit on the Play page. No game action uses LOS; Fire, which will apply the hindrance DRM, is left for the user to scope.

## 2. The hindrance breakdown

VASL's `LOSResult.addMapHindrance` keeps, for each range from the source, the largest map hindrance met at that range, and the first point where any hindrance was met. `getHindrance` is the floor of their sum.

- `LosResult` gains `Hindrances`: the range and value of each entry, in range order, and `FirstHindranceAt`: the grid point and its board-relative hex, or none.
- The values are those VASL adds: 1 for most hindrances, 2 for light woods and roofless hexes, and 0.5 for grain and paddy (its `addHindranceHex`).
- Counter hindrances (smoke, vehicles, OBA) stay out, as in step 13.

## 3. Hexside locations and aiming points

A hexside location in VASL (`Hex.getHexsideLocation`) has an LOS point at one vertex of the hexside, an auxiliary LOS point at the next, and an edge point at the hexside's middle. `Map.LOS` starts or ends at the auxiliary point when its `useAux` flag is set.

- `LosCalculator.Check` accepts a `BoardLocation` with a hexside, as `bd01:E4:0/0` names it (side 0 to 5, north first), and an aim for each end: the LOS point, or the auxiliary point.
- Its elevation follows `setSourceAndTargetElevations`: a non-center location in a depression hex is one level higher.
- Everything the walk does with the source and target hex applies unchanged; the rules that test `isCenterLocation` take the hexside branch.

**As built** (part 2).
- `LosResult` gains `Hindrances` (`LosHindrance(Range, Value)`, in range order) and `FirstHindranceAt` (`LosHindranceAt(Point, Board, Hex)`), kept as `addMapHindrance` and `setFirstHindrance` keep them; its equality compares the breakdown by value. `LosCalculator.Check(map, source, sourceAim, target, targetAim)` takes a `LosAim` per end (`LosPoint` or `AuxiliaryPoint`); the two-location overload aims at the LOS points, and a center location ignores its aim.
- A hexside location is level 0 (as `Hex.createLocations` makes it; other levels are refused), with the hexside's terrain and depression terrain, its LOS point at vertex *side* and its auxiliary point at the next, truncated. Its hex follows the owner rule.
- Reproduced for hexside ends: `setSourceAndTargetElevations` (+1 in a depression hex), `setEnterExitHexsides` (`getHexsideCrossed` with `removeVertexHexsides`), `getAdjacentHexes` (no even-range shortcut), `applyLOSRules` tests 1, 2, 4 and 5, `checkSameHexRule` (only with a center end; two hexsides of one hex walk at range 0; the bridge test pairs a bridge end with a center end), the blind hex rule's odd-range building test (a hexside target counts as even), and `checkBuildingRestrictionRule`'s same-hex factory exemption. The depression, `specialtestDepressionGroundLevel` and B10.2 tests now measure from the ends' hex centers, as VASL does.
- VASL quirk: an end point off the grid (a hexside of a map-edge half hex) has no terrain, and `applyLOSRules`' catch ends the LOS at its first point, clear at the full range. The read does the same. Off the grid in `checkLOSOnHexsideRule` VASL would fail; the read names `VaslFails` there.
- `LosFidelity` reads `hindrances`, `firstHindranceAt`, `firstHindranceHex` and `sourceAux`, compares the breakdown and the first point and hex on every answered pair, and reads a hexside pair from the point VASL used. `LosFidelity.FindHexside` lists the hexside fixtures. Every center fixture stays fully answered (except VASL's 10 failures on `bdBFPDW2b`) with no disagreement on the breakdown.

  | Fixture | Pairs | Answered | Disagreements |
  |---|---|---|---|
  | `bd01.los-hexside` | 20,388 | 20,388 | 0 |
  | `bd12.los-hexside` | 13,668 | 13,668 | 0 |
  | `bdBFPD.los-hexside` | 11,304 | 11,304 | 0 |

## 4. The oracle

- **Breakdown on every pair.** The LOS mode writes each pair's map hindrances by range and its first hindrance point, read from `LOSResult` (its hindrance map is not public, so the harness reads the field). The LOS harness becomes 1.2.0 and every LOS fixture is regenerated; the other fields are unchanged.
- **Hexside fixtures.** A hexside mode writes `bdNN.los-hexside.json.gz`: from each hexside location of every tenth column and fifth row, with each aiming point, to every center location within range 8. Sources are named `bdNN:HEX:level/side` and carry `sourceAux`. Boards: 01 (walls, hedges, buildings), 12 (depressions and rowhouses), and BFP D (bocage).

**As built** (part 1).
- The LOS harness is 1.2.0. Each pair adds `firstHindranceAt` and `firstHindranceHex`, and `hindrances`: VASL's range and value pairs, read by reflection from `LOSResult.mapHindrances` and written in range order. Every LOS fixture was regenerated; results and pair counts are unchanged. Fixture headers gain `mode` (`center` or `hexside`).
- `--los-hexside` and `-Hexside` write the hexside fixtures. A hexside pair adds `sourceAux`; its source is written with its side, as `BoardLocation` reads it.

  | Fixture | Pairs | Blocked | With a hindrance |
  |---|---|---|---|
  | `bd01.los-hexside` | 20,388 | 14,984 | 0 |
  | `bd12.los-hexside` | 13,668 | 7,496 | 1,874 |
  | `bdBFPD.los-hexside` | 11,304 | 7,732 | 2,834 |

- `LosFixtureTests` covers both modes: the header, hexside sources and `sourceAux`, and each breakdown, whose values sum to the total's floor with a first point exactly when there is a hindrance.

## 5. LOS from a unit

- **Play page.** The LOS panel gains a "From unit" choice listing the units the viewer can see; choosing one fills the source with its location. The result shows the hindrance breakdown ("hindrance 2 at range 3; first at bd01:E4").
- **Board viewer.** The LOS panel shows the same breakdown.
- A hidden or concealed enemy is never offered, since the viewer cannot see it; the adjudicator sees every unit.

**As built** (part 3).
- `LosCheck.Breakdown` writes the breakdown ("Hindrances 1 at range 1, 2 at range 3; first at bd01:E3"), and `Summary` appends it to the result.
- `StudioLos.Check` takes the source's aim. A hexside location is drawn from its first vertex, or the next when aimed at the auxiliary point, as the read builds its ends.
- The board viewer's LOS panel accepts hexside locations (`bd01:E4:0/3`) and has a "from a hexside's next vertex" box for the source's aim.
- The Play page's LOS panel has a "From unit" list of the units the viewer can see at a map location; choosing one fills the source with its location. The adjudicator sees every unit.
- Tests, Map Studio: the breakdown text, a hexside source aimed at each vertex, and the "From unit" list, which offers the German side its own squad but not a concealed Russian one.

## 6. Build order

1. `feature/asl-unit-los4-oracle`: section 4, with the fixture tests.
2. `feature/asl-unit-los4-read`: sections 2 and 3, with the fidelity tests of U17.
3. `feature/asl-unit-los4-play`: section 5.

This design is merged with the step 16 plan.

## 7. Tests

- **Fidelity:** every answered pair agrees with VASL on the breakdown and the first hindrance point as well as on the result; the hexside fixtures agree where answered, with their answered counts pinned.
- **Synthetic:** a grain and a woods hindrance at different ranges, and a hexside location aimed at each point, in `LosTests`.
- **Map Studio:** the Play page's "From unit" choice offers only visible units and fills the source; the result shows the breakdown.

## 8. Not in this step

- Counter hindrances, night, and overlays.
- Any game action that uses LOS, and Fire.
- Bypass movement itself; a hexside location here is only an LOS end.
