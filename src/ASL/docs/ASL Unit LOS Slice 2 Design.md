# ASL Unit LOS Slice 2 Design

**Status:** Built in the order of section 6. Part 1 is recorded in section 3, and parts 2 and 3 in section 2.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 14, and acceptance scenario U15 (section 14).

**Builds on:** the [ASL Unit LOS Design](<ASL Unit LOS Design.md>) (step 13), whose principles all hold: VASL's `Map.LOS` is the reference, reproduced in C# and checked pair by pair against the oracle; a rule not yet reproduced is an unsupported answer with its name; only verified terrain is definitive. The Java oracle is used for fixtures only, provisionally, as the user allowed on 2026-09-26.

## 1. Outcome

The step 13 read answers pairs whose ends are cellars or rooftops, and whose lines cross depressions or cliffs, as VASL does. On boards 01 and 11 every pair is answered. New fixtures cover boards with gullies, streams, and cliffs, and every pair there that is still unanswered names a rule of step 15.

## 2. What changes in the read

`LosCalculator` stops refusing these, in VASL's order:

- **Cellars** (`LOSStatus` setup; `checkHexsideTerrainRule`, O6.3): a cellar source or target at level -1 of its hex, one level higher for the height rules, and "Unit in cellar cannot be seen over hexside terrain by non-adjacent target (O6.3)".
- **Rooftops** (`setSourceAndTargetElevations` and the per-rule adjustments): a rooftop location a half level lower, or a full level when it is not level 1 of the hex, in the building restriction, the terrain-height and terrain-higher rules, and the depression setup.
- **Depressions** (`exitsSourceDepression`, `entersTargetDepression`, `checkDepressionRule` and its helpers, `losFollowsDepression`, `ignoreGroundLevelHex`): gullies, streams, and other depression terrain in the hex and on hexsides, the "elevation difference within range" restrictions of A6.3, LOS along a depression, the crest at a vertex (B19.51), and the non-center depression location adjustment in `setSourceAndTargetElevations`.
- **Cliffs** (`checkBlindHexRule` at cliff hexsides, and the cliff exceptions in `checkGroundLevelRule` and `checkTerrainHeightRule`, B10.23).

Factories and roofless buildings, which VASL treats together with rooftops in some rules, become their own unsupported rule, "Factories and roofless buildings (B23.87)", so a rooftop in an ordinary building is answered while a factory stays unsupported until step 15.

The per-hex data the walk needs grows accordingly: each hexside's depression terrain and cliff flag already come from the hex facts, and the adjacent hex's base level and depression state come from the map.

**As built** (part 2). Cellars and rooftops:
- A cellar or rooftop is a location of the hex's center chain, so `setSourceAndTargetElevations` gives it no vertex adjustment. The rules count a cellar one level higher and a rooftop half a level lower unless it is level 1 of its hex, each where VASL does: `checkGroundLevelRule`, `checkTerrainIsHigherRule`, `checkTerrainHeightRule` (the height test only; its exceptions use the plain elevations), and `checkBlindHexRule` both; `checkSplitTerrainRule` the cellar only; `checkHalfLevelTerrainRule` the rooftop only, and only when the other end is not a rooftop; `isBlindHex` the rooftop only, after its same-level test, with a half-level building half a level higher.
- `checkHexsideTerrainRule`: a cellar source, then a cellar target, takes the O6.3 rule at any hexside terrain, in the source and target hexes too, and never the wall and hedge rule.
- The rooftop adjustments of `checkBuildingRestrictionRule` and the depression setup apply only to factory rooftops and depression ends, which stay unsupported. Rooftop and cellar terrain along the line has no rule of its own.
- `LosUnsupportedRule.Factory` is now "Factories and roofless buildings (B23.87)": factory and roofless ends, a rooftop in a factory hex, and factory terrain along the line.
- Tests: Maps `LosTests` has a cellar to ground pair (clear over open ground, blocked by a wall by O6.3, both ways) and a rooftop pair (blocked by a one-level building as high as the rooftop); Maps.Vasl `LosFidelityTests` asserts boards 01 and 11 and their seam scenarios are fully answered.
- Every answered pair agrees with VASL:

  | Fixture | Pairs | Answered | Unanswered, by rule |
  |---|---|---|---|
  | `bd01` | 18,251 | 18,251 | |
  | `bd11` | 5,662 | 5,662 | |
  | `bd11-over-bd01` | 26,718 | 26,718 | |
  | `bd11r-over-bd01` | 26,682 | 26,682 | |
  | `bd05` | 5,811 | 4,522 | depressions 1,046; bridges 243 |
  | `bd09` | 6,124 | 4,462 | cliffs 1,662 |
  | `bd12` | 8,059 | 5,547 | depressions 1,883; rowhouse walls 598; bridges 31 |
  | `bd15` | 6,051 | 3,907 | cliffs 2,144 |
  | `bd12-over-bd15` | 17,588 | 11,252 | depressions 3,050; cliffs 2,629; rowhouse walls 611; bridges 46 |

  Pairs with a cellar or rooftop end that are still unanswered cross a depression, cliff, rowhouse wall, or bridge, and are counted under that rule.

**As built** (part 3). Depressions and cliffs:
- Depressions: the setup's `exitsSourceDepression` and `entersTargetDepression` (a rooftop a full level lower there), and `checkDepressionRule` first in `applyLOSRules`, in every hex: the A6.3 exit and entry tests with `specialtestDepressionGroundLevelOnExit` and `OnEntry`, `losCrossingBridgeDepiction`, `exitsByRoadHexside` and `entersByRoadHexside`, the blind hex rule at a cliff in the source or target hex, and the crest at a vertex (B19.51, `exitHexsideIsCrest`, `enterHexsideIsCrest`). On entering a new hex, `losFollowsDepression` sets `ignoreGroundLevelHex`, which `checkGroundLevelRule` and `checkTerrainHeightRule` honor; the latter also skips depression terrain while the restrictions apply, unless an obstacle stands in the depression hex (B19.21). Streams are depression terrain and take the same rules. Only center locations are read, so the non-center adjustment of `setSourceAndTargetElevations` is never reached.
- Cliffs: `checkBlindHexRule` tests every cliff pixel, with the exit-hexside exception and, along a hexside, the lower hex's level; `isBlindHex` has its cliff branches (the top of the cliff as ground level, one blind hex fewer). `checkGroundLevelRule` and `checkTerrainHeightRule` take their cliff exceptions along a hexside. Cliff hexsides skip `checkHexsideTerrainRule`.
- Not reached: the bridge adjustments of the depression rule (bridges stay unsupported), and slopes, which no fixture board has; they stay "Slopes (F2.3)". `LosUnsupportedRule.Depression` and `Cliff` are gone.
- VASL quirks kept: `getAdjacentHex` with no hexside is the hex itself, and `getHexsideLocation` gives hexside 0, in the crest and cliff tests; `exitsDepressionTerrainHexside` is false along a hexside, so its setup clause never holds, and the entry test uses it too; `losFollowsDepression` reduces to a one-level difference; `ignoreGroundLevelHex` is never reset; a cliff in a depression hex other than the target is ignored ("cliff artwork"). Where VASL would throw (a neighbor off the map), the result is "A line on which VASL's LOS fails"; no fixture pair reaches it.
- Tests: Maps `LosTests` has a gully exit (blocked leaving the gully, clear to a target higher by the range) and a cliff blind hex (clear over the crest alone, blind below the cliff); each fails with its rule disabled. Maps.Vasl `LosFidelityTests` pins the counts below and asserts that every unanswered pair on the step 14 fixtures names a rule of step 15. Disabling `checkDepressionRule` and the cliff hexside gives 533 disagreements on five fixtures. The Play page LOS test now reads a clear answer.
- Every answered pair agrees with VASL:

  | Fixture | Pairs | Answered | Unanswered, by rule |
  |---|---|---|---|
  | `bd01` | 18,251 | 18,251 | |
  | `bd11` | 5,662 | 5,662 | |
  | `bd11-over-bd01` | 26,718 | 26,718 | |
  | `bd11r-over-bd01` | 26,682 | 26,682 | |
  | `bd05` | 5,811 | 5,231 | bridges 580 |
  | `bd09` | 6,124 | 6,124 | |
  | `bd12` | 8,059 | 7,176 | rowhouse walls 659; bridges 224 |
  | `bd15` | 6,051 | 6,051 | |
  | `bd12-over-bd15` | 17,588 | 16,571 | rowhouse walls 672; bridges 345 |

  Pairs that crossed a depression and a bridge or rowhouse wall are now counted under that rule.

## 3. The fixtures

The oracle's LOS mode is unchanged. New fixtures, with the same observer stride and range as step 13:

- `bd05.los.json.gz`: woods and depressions;
- `bd09.los.json.gz`: hills and cliffs;
- `bd12.los.json.gz`: depressions and buildings;
- `bd15.los.json.gz`: hills and cliffs;
- `bd12-over-bd15` in `los-scenarios.txt`: a depression board above a cliff board, across the seam.

`LosFixtureTests` pins their pair counts. `LosFidelityTests` gains them, pinned at the answered counts each part reaches.

**As built** (part 1). The fixtures, generated with the unchanged oracle; every answered pair already agrees with VASL:

| Fixture | Pairs | Answered | Unanswered, by rule |
|---|---|---|---|
| `bd05` | 5,811 | 4,522 | depressions 1,046; bridges 243 |
| `bd09` | 6,124 | 4,454 | cliffs 1,304; cellars 183; rooftops and factories 183 |
| `bd12` | 8,059 | 4,645 | depressions 1,736; cellars 661; rooftops and factories 661; rowhouse walls 327; bridges 29 |
| `bd15` | 6,051 | 3,714 | cliffs 2,037; cellars 150; rooftops and factories 150 |
| `bd12-over-bd15` | 17,588 | 9,960 | depressions 2,850; cliffs 2,465; cellars 967; rooftops and factories 967; rowhouse walls 335; bridges 44 |

`AnUnsupportedPairNamesItsRule` no longer expects every fixture to have cellar pairs: board 05 has none, and boards 01 and 11 will have none unanswered after part 2.

## 4. Tests

- **Fidelity:** every answered pair on every fixture agrees with VASL (U14 and U15). The pinned answered counts rise with each part.
- **Coverage:** on boards 01 and 11, and their seam scenarios, every pair is answered.
- **Names:** on the new fixtures, every unanswered pair names a rule of step 15 (bridges, hillocks, partial orchards, railroad embankments, bocage, rubble, factories and roofless buildings, rowhouse walls, entrenchments), or one listed in section 7 with a reason.
- **Synthetic:** a cellar to ground pair, a rooftop pair, a gully exit, and a cliff blind hex on synthetic boards in `LosTests`, so the rules have tests that need no VASL checkout.

## 5. The Studio

No change: the board viewer, the Play page, and the Fidelity page read the new rules and fixtures as they are. The Fidelity page lists the new fixtures because it finds every LOS fixture.

## 6. Build order

1. `feature/asl-unit-los2-fixtures`: the fixtures of section 3, with the fidelity tests pinned at the current answered counts.
2. `feature/asl-unit-los2-cellars-rooftops`: cellars and rooftops; boards 01 and 11 fully answered.
3. `feature/asl-unit-los2-depressions-cliffs`: depressions and cliffs; U15.

This design is merged first, on its own branch.

## 7. Not in this step

- The rule groups of step 15.
- Slopes and hillock-adjacent slopes, unless a depression or cliff pair needs them; any that remain are named in the as-built note of part 3.
- Everything section 10 of the step 13 design leaves out.
