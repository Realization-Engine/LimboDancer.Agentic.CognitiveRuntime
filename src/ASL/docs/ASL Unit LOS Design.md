# ASL Unit LOS Design

**Status:** Built in the order of section 9. Part 1 is recorded in section 4, part 2 in section 5, and part 3 in section 8.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 13, and acceptance scenario U14 (section 14). Maps: [ASL Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), ASL-MAP-044, 080, and 082 as revised at step 13.

**Related documents:**

- the [VASL Board Ingestion Design](<LimboDancer.Agentic.CognitiveRuntime ASL VASL Board Ingestion Design.md>), whose terrain grid, derivation, and hex-fact oracle this step builds on, and which assigns hillocks to the LOS executor;
- the [ASL Map Model and Authoring Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design.md>), whose board handle already plans a read-only grid for LOS;
- the [ASL Unit Composed Maps Design](<ASL Unit Composed Maps Design.md>) (step 12), whose placed maps LOS crosses.

The LOS rules are VASL's, as its `VASL.LOS.Map.Map.LOS` applies them. This step reproduces its results and cites no rulebook pages of its own. The rule names below (A6.2, B.10, and so on) are the ones VASL's code gives.

## 1. Outcome

A read-only LOS check between two locations, on a board or a placed map:

- **It answers as VASL does:** whether LOS is blocked, the hex where it is first blocked, the range, and the hindrance total. Six or more hindrances block, as in VASL (B.10). Hindrances are otherwise reported only, and are never a DRM.
- **It says when it cannot answer.** Terrain whose rule is not yet reproduced gives an unsupported result with the rule's name, never a guess.
- **It is checked against VASL itself.** An oracle runs VASL's own `Map.LOS` on sampled pairs of locations, and the C# read must agree on every pair it answers (U14).
- **It is shown in the Studio.** The board viewer and the Play page gain an LOS tool.

No game action uses LOS in this step.

## 2. Principles

- **VASL is the reference.** The C# read reproduces VASL's results, as the hex-fact derivation does, and the oracle compares them with VASL's own code. No VASL code or data is committed; the fixtures hold derived results only (ASL-MAP-073). VASL is licensed under LGPL 2.1.
- **Unsupported is an answer.** A rule not yet reproduced makes the result unsupported with that rule's name. It never falls back to a simpler rule.
- **Only verified terrain is definitive.** A board whose status is not Verified or AuthoredValid gives a nondefinitive result (ASL-MAP-044).
- **One engine for boards and maps.** The engine reads a geometry, a terrain grid, the hex facts, and the terrain catalog. A single board and a built composed map both supply them.

## 3. The VASL algorithm, as this step reproduces it

`Map.LOS` walks the terrain grid from the source's LOS point to the target's, column by column, and applies its rules to every cell on the line:

- **Setup.** Source and target heights are the hex base level plus the level in the hex. The range is `Map.range`. The walk also fixes whether the line is horizontal or along a 60-degree hexspine, the hexsides it leaves and enters by, and the depression and slope state at each end.
- **Per cell.** The cell's hex is the source or target hex when the cell is inside its extended border, and otherwise the hex under the cell. On a horizontal or 60-degree line, the rule for LOS along a hexside picks which adjacent hex counts.
- **Rules that can block,** in VASL's order:
  1. the same-hex rule: building levels more than one apart, or without a stairway;
  2. depression exit and entry (A6.3), and the crest at a vertex (B19.51);
  3. the building restriction (A6.8), and factory rooftops and roofless factories (B23.87);
  4. hexside terrain: walls, hedges, bocage, partial orchards, rowhouse and factory walls, entrenchments (B27.2), and cellars;
  5. railroad embankments and partial orchards on crossed hexsides;
  6. between the two ends: bridges, ground level higher than both ends (A6.2), split terrain, half-level terrain, terrain higher than the observer, terrain as high as the higher end, blind hexes (A6.4), and hillocks (F6.4);
  7. the hindrance total: six or more blocks (B.10).
- **Not in this step.** VASL's counter rules (smoke, vehicles, OBA, wrecks, rubble counters) are skipped when there are no counters. The night rule is skipped when the scenario is not at night.

The first build reproduces rule groups 1, 3 (without factories), 4 (walls and hedges only), 6 (ground level, split terrain, half-level terrain, terrain higher, terrain height, blind hexes), and 7, over open ground, woods, brush, orchards, grain, buildings with their levels and stairways, hills, and walls and hedges. Everything else is unsupported with its rule's name, among them:

- depressions, gullies, and streams;
- cliffs;
- bridges and tunnels;
- factories, rooftops, rowhouse walls, cellars, and entrenchments;
- bocage and partial orchards;
- railroad embankments and hillocks.

Each later slice moves a group from unsupported to reproduced, with its own oracle pairs.

## 4. The oracle

The VASL hex-fact oracle tool (`src/ASL/tools/vasl-hexfact-oracle`) gains an LOS mode.

- **Building the map.** As for hex facts: VASL's parsers, its `Map` with the runtime grid configuration, its `LOSData` loop, and `resetHexTerrain`, which also builds hillocks. A composed scenario is built as the existing scenario fixtures are.
- **Calling LOS.** `Map.LOS` needs a game interface and a game module even without counters or night:
  - `new VASLGameInterface(null, null)`, whose lists stay empty;
  - a stub game module that gives a scenario that is not at night. VASL's `ScenInfo` opens a window in its constructor, so the stub is made without calling it.

  The first part spikes this. If VASL cannot be called headless this way, the part stops and reports before any fixture is written.
- **Pairs.** For each fixture, a fixed list of observer locations, chosen to include ground level, upper building levels, and hill hexes, each paired with every location within range 12. Upper building locations are reached as VASL reaches them, through its up and down links from the center location. The list and the range are recorded in the fixture header, so a fixture is regenerated exactly.
- **Fixtures.** `bdNN.los.json.gz` and `name.scenario.los.json.gz`, beside the hex-fact fixtures, in the same canonical JSON with the same header: VASL commit, harness version, and the Git blobs of the board files. Each pair records:
  - the source and target, as board-relative locations;
  - whether LOS is blocked, VASL's blocking point, and the hex at that point;
  - the range, the hindrance total, and VASL's reason text.
- **Boards.** Board 01, a board with hills and woods chosen in part 1, and one two-board scenario for LOS across a seam.

**As built** (part 1).
- The LOS mode is a `--los` argument of `HexFactOracle` and a `-Los` switch of `generate-fixtures.ps1`, with its own harness version (1.0.0). Seam scenarios are listed in `Oracle/Scenarios/los-scenarios.txt`, so the hex-fact scenarios are unchanged.
- The headless spike succeeded: `new VASLGameInterface(null, null)`, and a stub `GameModule` holding a `ScenInfo` with night "No", both made without their constructors.
- Observers are every location of every fifth column and third row of the map. Board-relative names use the board whose pixel rectangle holds the hex center last, which is the step 12 owner rule.
- The hill board is board 11: 126 hill hexes, with no cliffs or depressions. The seam scenarios are board 11 above board 01, plain and reversed.
- Fixtures:

  | Fixture | Pairs | Blocked | Across the seam |
  |---|---|---|---|
  | `bd01.los.json.gz` | 18,251 | 15,142 | |
  | `bd11.los.json.gz` | 5,662 | 4,702 | |
  | `bd11-over-bd01.scenario.los.json.gz` | 26,718 | 22,461 | 7,983 |
  | `bd11r-over-bd01.scenario.los.json.gz` | 26,682 | 22,599 | 7,950 |

- Regenerating board 01 twice gave identical bytes.
- Tests, Maps.Vasl `LosFixtureTests`: each fixture's board sources equal its hex-fact fixture's, every location and blocking hex it names is in the hex facts, the pair counts are pinned, and the seam scenarios have pairs across the seam. They need no VASL checkout. `VaslMapTests` now lists only hex-fact files as scenario fixtures.
- The user allowed the Java oracle for fixtures provisionally, to be revisited; the unit requirements' later candidates record it.

## 5. The read

- **LOS data on a board.** `BoardHandle` gains an optional `LosData`: the terrain grid, the hexside annotations, the terrain catalog, and the board's LOS rules. The Map Model design already plans a read-only grid here. VASL and authored boards supply it. A handle without it answers LOS as unsupported.
- **The engine.** `LosCalculator` in the Maps project, over a `LosMap`: geometry, grid, hex facts, and catalog. `LosMap.ForBoard(handle)` uses one board. `LosMap.ForPlacedMap(read)` builds the map from the handles' LOS data with `VaslMapBuilder` and caches it by placement text.
- **Locations.** Source and target are board-relative locations. On a placed map they are located with `MapLayout`, and a shared half hex follows the owner rule of step 12. The LOS point of a location is its hex center, as VASL's center locations use; upper levels share the center point.
- **The result.** `LosResult` records:

  | Field | Meaning |
  |---|---|
  | `Status` | Clear, Blocked, Unsupported, or Nondefinitive |
  | `BlockedAt` | The board-relative hex where LOS is first blocked, and the grid point |
  | `Range` | As `Map.range` |
  | `Hindrance` | The total, the floor of the sum of the largest hindrance at each range, as VASL keeps it |
  | `Reason` | The rule that blocked, or that is not reproduced |

  Clear and Blocked are definitive only on Verified or AuthoredValid boards; otherwise the status is Nondefinitive with the answer beside it.
- **Missing geometry.** VASL's extended hex borders become public on `VaslHexLocator`, and the line's hexside crossings, the nearest location, and the hexside of a point are added to the geometry, each tested against VASL through the oracle.

**As built** (part 2).
- `LimboDancer.Domains.Asl.Maps.Los`: `LosMap` (`ForGrid`, `ForVaslMap`, `ForBoard`, `ForPlacedMap`), `LosCalculator.Check`, `LosResult`, `LosLine` (horizontal and 60-degree tests, `Line2D.linesIntersect`), and `LosUnsupportedRule`, the names an unsupported result gives. `BoardHandle.Los` is an init-only `LosData`: grid, catalog, hexside annotations, and LOS rules.
- `ForBoard` refuses a handle without LOS data (MAP-LOS-001). `ForPlacedMap` builds with `VaslMapBuilder` and caches by placement text, board versions, and statuses. A board that is not Verified or AuthoredValid gives Nondefinitive, with the answer in `IsBlocked`.
- A hexside location, a hex off the map, a shared hex named by the board that does not own it, or a level outside the hex's chain throws `ArgumentException`.
- `VaslHexLocator` gains `ExtendedBorder`, `ExtendedBorderContains`, and `BorderContains`; `LosMap` gains `NearestLocation`, `NearestHexside`, and `HexsidesCrossed`.
- Reproduced as VASL runs them: the same-hex rule (bridges excepted), A6.8, walls and hedges (B9.2) with `isIgnorableHexsideTerrain`, ground level, split terrain, half-level terrain, terrain higher, terrain height, blind hexes (A6.4, B10.23), inherent spill, and hindrances (B.10). VASL quirks kept: only the level 0 center location takes the even-range shortcut of `getAdjacentHexes`; `getHexsideWhenLOSAlongHexside` fills in missing target hexsides; the blind hex rule clears the result before its odd-range test.
- Unsupported where the walk would need them: rooftop, cellar, factory, entrenchment, bridge, embankment, or hillock ends; depression ends, slopes, and adjacent hillocks; and, per point, depressions, cliffs, bridges, factories, roofless hexes, rowhouse walls, bocage, partial orchards, embankments, hillocks, rubble, Deir, out-of-season orchards, sand dunes, and Volga piers.
- U14, Maps.Vasl `LosFidelityTests`: every answered pair agrees on blocked, blocking hex, range, hindrance, and reason text. Answered counts are pinned. Every unanswered pair has a rooftop or cellar end:

  | Fixture | Pairs | Answered and agreed | Unsupported, cellar | Unsupported, rooftop |
  |---|---|---|---|---|
  | `bd01.los.json.gz` | 18,251 | 10,701 | 3,775 | 3,775 |
  | `bd11.los.json.gz` | 5,662 | 5,598 | 32 | 32 |
  | `bd11-over-bd01.scenario.los.json.gz` | 26,718 | 19,028 | 3,845 | 3,845 |
  | `bd11r-over-bd01.scenario.los.json.gz` | 26,682 | 19,018 | 3,832 | 3,832 |

- Also in Maps.Vasl: a board with status Ingested answers Nondefinitive, and `ForPlacedMap` from handles answers as the built map. Maps `LosTests` cover the geometry and the read on a synthetic board without VASL.

## 6. The Studio

- **Board viewer.** On a board or composed map, an LOS tool: choose a source and a target location, by hex name and level or by clicking. The viewer draws the line between the two LOS points and marks the blocking hex, and shows the status, range, hindrance total, and reason.
- **Play page.** The same tool over the game's map, with the game's locations. LOS reads terrain only, so every viewer may use it.

## 7. Tests

**Oracle (Java tool):** regenerating a fixture twice gives the same bytes.

**Maps:**
- U14: on every oracle pair, where the C# read is Clear or Blocked, it agrees with VASL on blocked, the blocking hex, the range, and the hindrance total. The same holds on the scenario fixture across the seam.
- Coverage: the share of pairs answered on each fixture is pinned by a test, so it can only rise as rules are reproduced.
- Unsupported: a pair whose line crosses a gully, a cliff, or a bridge is unsupported with that rule's name.
- Nondefinitive: the same pair on a board with an unverified status is nondefinitive.
- The new geometry against VASL: extended borders, hexside crossings, nearest locations.

The oracle comparisons need the VASL root, as F2 does. Without it, the fixture integrity checks run.

**Map Studio:** the LOS tool on a board and on the Play page, with a clear and a blocked pair on the synthetic board.

The LF clone and Docker verify each part as before.

## 8. The fidelity record

The Fidelity page gains an LOS row per fixture: pairs, answered, agreed, and unsupported by rule. A disagreement on an answered pair fails the check, as F2 does.

**As built** (part 3).
- `StudioLos` (Map Studio services) reads LOS over a loaded board or composed map, draws the LOS layer (the line, dashed beyond the blocking point, and the blocking hex outlined), and compares the read with the LOS fixtures. On a single board a location may omit the board, as in `E4` or `E4:1`.
- A composed map is definitive when every placed board is Verified or AuthoredValid, as `LosMap.ForPlacedMap` judges it. A map built from placements that match no oracle scenario is itself only Ingested, but its terrain is its boards'.
- The board viewer has an LOS panel whose locations can be typed or taken from the selected hex; the layer is drawn through a new `setLos` in the viewport script. The Play page has the same panel over the game's map, drawn into the inline map.
- The Fidelity page has an "LOS against VASL" section: pairs, answered, agreed, outcome, and unanswered pairs by rule, for every LOS fixture the Studio finds. It runs on request, not as part of the board batch.
- The fixture comparison is `LosFidelity` in Maps.Vasl, shared by the page and `LosFidelityTests`.
- Studio board handles carry `LosData`: the board's grid and catalog, with a VASL board's hexside annotations.
- Live check, with the VASL checkout: on board 01, K1 to A3 is blocked at D2 with VASL's point and reason; the Fidelity page passes all four fixtures with the counts of section 5; on the step 12 seam game, LOS from bd02:G10 to bd01:G4 is drawn across the seam, blocked at bd01:G1.
- Tests, Map Studio: `StudioLosTests` (a clear pair, a blocked pair with its layer, an unverified board, refused locations, and a composed map judged by its boards, on a synthetic geomorphic board with woods) and a Play page test of the LOS panel over the game's map.

## 9. Build order

1. `feature/asl-unit-los-oracle`: the oracle LOS mode, the headless spike, and the fixtures of section 4.
2. `feature/asl-unit-los-read`: `LosData` on handles, the geometry of section 5, the engine, and the comparison tests (U14).
3. `feature/asl-unit-los-studio`: the tools of section 6 and the fidelity record of section 8.

This design is merged first, on its own branch.

## 10. Not in this step

- The rule groups listed as unsupported in section 3.
- Hindrance DRM, counters, night and illumination, overlays, and scenario-specific rules.
- Bypass and hexside aiming points (VASL's auxiliary LOS points).
- Any game action that uses LOS, and Fire.
