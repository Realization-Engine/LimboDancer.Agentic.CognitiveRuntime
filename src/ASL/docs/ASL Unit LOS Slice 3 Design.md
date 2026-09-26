# ASL Unit LOS Slice 3 Design

**Status:** Proposed. Designed before code, on `feature/asl-unit-step15-plan`; to be built in the order of section 5.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 15, and acceptance scenario U16 (section 14).

**Builds on:** the [ASL Unit LOS Design](<ASL Unit LOS Design.md>) (step 13) and the [ASL Unit LOS Slice 2 Design](<ASL Unit LOS Slice 2 Design.md>) (step 14). Their principles hold: VASL's `Map.LOS` is the reference, reproduced in C# and checked pair by pair against the oracle; a rule not reproduced is an unsupported answer with its name. The Java oracle is used for fixtures only, provisionally.

## 1. Outcome

The read answers the rule groups that step 14 leaves unsupported wherever a fixture board exercises them, and names the rest. After this step, a pair is unanswered only when its line meets terrain no fixture board has, or a situation the as-built notes list with its reason.

## 2. The fixtures

New LOS fixtures, with the observer stride and range of step 13, for boards chosen from the hex-fact fixtures by their features:

| Board | Chosen for |
|---|---|
| 23 | bridges |
| 51 | rowhouses and cellars |
| rdx | factories |
| BFP D | bocage |
| BFP DW2b | hillocks |
| BFP B | railroad |
| 96 | rubble |

A board whose layout the oracle's LOS mode cannot build is replaced by the next board with the feature, and the as-built note says which. Boards 05 and 12 of step 14 already carry bridges and rowhouse walls.

**As built** (part 1).
- The seven boards all build. On BFP board DW2b, VASL's own LOS throws a NullPointerException on 10 lines along the map edge, in a crest test that asks for a neighbour off the map. The oracle's LOS mode now records such a pair with `vaslError` in place of a result instead of stopping (harness 1.1.0), and every LOS fixture was regenerated; the other fixtures' pairs are unchanged.
- `LosFidelity` reads `vaslError`. On such a pair an answer (Clear or Blocked) is a disagreement; an unsupported result is counted by its reason, since the read may stop at an unreproduced rule before the point where VASL fails.
- Out-of-season orchards occur on BFP board DW2b, so they join part 3, and the requirement no longer lists them as unchecked.
- Fixtures, with the answers the read gives today (all agreeing with VASL):

  | Fixture | Pairs | Answered | Unanswered, by rule |
  |---|---|---|---|
  | `bd23` | 12,866 | 11,554 | bridges 980; rowhouse walls 294; cellars 38 |
  | `bd51` | 29,151 | 26,581 | rowhouse walls 2,570 |
  | `bd96` | 6,079 | 5,858 | rubble 221 |
  | `bdBFPB` | 11,882 | 7,124 | factories and roofless buildings 4,758 |
  | `bdBFPD` | 5,566 | 1,301 | bocage 4,242; slopes 23 |
  | `bdBFPDW2b` | 5,584 | 2,684 | hillocks 2,448; out-of-season orchards 417; bridges 25; VASL fails 3 (of 10) |
  | `bdrdx` | 2,046 | 64 | factories and roofless buildings 1,982 |

## 3. The rules

**Part 2, buildings and bridges,** in VASL's order:

- **Bridges and tunnels:** bridge locations as ends, `checkBridgeHindranceRule` with the bridge and road shapes, and the bridge adjustments inside the depression rule.
- **Rowhouse and factory walls** (B23.71), and the rowhouse and factory wall rules of `checkHexsideTerrainRule`.
- **Factories:** factory terrain along the line, factory rooftops (`isRooftopLOSBlocked`), and the factory cases of the building restriction and blind hex rules (B23.87).
- **Roofless and gutted buildings:** their hindrance and blind hex cases.
- **Rubble:** the rubble state of `applyLOSRules` and the rubble cases of the hillock rule.

**Part 3, hexside and rise terrain:**

- **Bocage** (B9.52) in `checkHexsideTerrainRule`.
- **Hillocks** (F6.4): `buildHillocks`, the hillock state of `LOSStatus`, `setHillockStatus`, and `checkHillockRule`, including the wall and rubble crossings.
- **Railroad embankments** (`checkRBrrembankments`) and the embankment adjustment.
- **Out-of-season orchards**, which BFP board DW2b has.
- **Slopes** (F2.3), where a fixture board has them.

Each rule becomes answered only when the fixtures exercise it and every answered pair agrees. A rule a board never reaches stays unsupported.

**As built** (part 2).
- Bridges: bridge locations as ends, among them the depression location under a bridge (the 38 "Cellars (O6.3)" pairs on board 23 were `P7:-1`, the stream under P7's bridge, at level -1); `checkSameHexRule` ("Cannot see location under the bridge"); `checkBridgeHindranceRule`, with the shape of the single-hex bridge `Hex.fixBridgesTunnelWater` makes (32 by 48 pixels at the hex center, unrotated, its road 9 pixels in from each long side), taken from `BridgeFacts` and the hex center, so the derivation is unchanged; the bridge case of `checkGroundLevelRule` and LOS under a bridge in `checkTerrainIsHigherRule`.
- Rowhouse walls: `checkRowhouseFactoryWallAndBreach` first in `checkHexsideTerrainRule`, before the cellar rule; the rowhouse skip of `checkTerrainHeightRule`; the rowhouse exception of `isBlindHex`.
- Factories: the factory case of `checkBuildingRestrictionRule` with `isRooftopLOSBlocked`, including the odd-range hexside case with a rooftop end; the factory cases of `checkTerrainIsHigherRule`; outside factory walls and the factory case (off a hexside) of `checkBlindHexRule`; no hindrance from factory terrain in the blind hex rule.
- Rubble takes the general rules; VASL's rubble state serves only the hillock rule (part 3).
- Refused, as no fixture reaches them: "Bridge hexes in the depression rule (A6.3)"; "Factory rooftop and hexside cases (B23.87)" (the factory rooftop test of `checkTerrainHeightRule`, the factory blind hex case along a hexside, rubble in `isRooftopLOSBlocked`, LOS up to a rooftop blocked past the first hex, the odd-range case without a rooftop end); "Roofless and gutted buildings (B23.87)"; "Interior factory walls and breaches (B23.71, O5.31)"; "Tunnels"; a bridge hex with a railroad embankment hexside, as "Railroad embankments". The names "Bridges and tunnels", "Factories and roofless buildings (B23.87)", "Rowhouse and factory walls (B23.71)", "Rubble", and "Cellars (O6.3)" are gone.
- VASL quirks kept: `isRooftopLOSBlocked` adds the terrain height twice and ignores the ground level; the rooftop adjustment is -1 in `checkBuildingRestrictionRule` and `checkRowhouseFactoryWallAndBreach`; the rowhouse blind hex test measures the range from the source for a rising line too; the factory blind hex case compares the target's elevation with twice the terrain height; a rowhouse wall adds a hindrance in `checkBlindHexRule`; the bridge hex stays `ignoreGroundLevelHex` until another replaces it.
- Tests: Maps `LosTests` has a bridge hindrance off the road with the same-hex bridge rule, and a rowhouse wall; each fails with its rule disabled. Disabling a rule gives disagreements on the part 2 fixtures: rowhouse walls 2,340, the rowhouse blind hex exception 169, factory terrain in `checkTerrainIsHigherRule` 281, the factory restriction 159, the bridge hindrance 56, the rowhouse skip of the height rule 52, the bridge in the ground level rule 15, same-hex bridges 2, the factory blind hex case 1. `LosFidelityTests` asserts every fixture but BFP D and DW2b fully answered, and that those two name only part 3 rules.
- Every answered pair agrees with VASL:

  | Fixture | Pairs | Answered | Unanswered, by rule |
  |---|---|---|---|
  | `bd01` | 18,251 | 18,251 | |
  | `bd11` | 5,662 | 5,662 | |
  | `bd11-over-bd01` | 26,718 | 26,718 | |
  | `bd11r-over-bd01` | 26,682 | 26,682 | |
  | `bd05` | 5,811 | 5,811 | |
  | `bd09` | 6,124 | 6,124 | |
  | `bd12` | 8,059 | 8,059 | |
  | `bd15` | 6,051 | 6,051 | |
  | `bd12-over-bd15` | 17,588 | 17,588 | |
  | `bd23` | 12,866 | 12,866 | |
  | `bd51` | 29,151 | 29,151 | |
  | `bd96` | 6,079 | 6,079 | |
  | `bdBFPB` | 11,882 | 11,882 | |
  | `bdBFPD` | 5,566 | 1,301 | bocage 4,242; slopes 23 |
  | `bdBFPDW2b` | 5,584 | 2,704 | hillocks 2,459; out-of-season orchards 417; VASL fails 4 (of 10) |
  | `bdrdx` | 2,046 | 2,046 | |

## 4. Tests

- **Fidelity:** every answered pair on every fixture agrees with VASL (U14 to U16), with the answered counts pinned.
- **Names:** every unanswered pair names partial orchards or entrenchments, or a situation the as-built notes list with its reason (among them VASL's own failures, below).
- **Synthetic:** a case per reproduced group in `LosTests`, where a synthetic board can show it, confirmed to fail with the rule disabled.

## 5. Build order

1. `feature/asl-unit-los3-fixtures`: the fixtures of section 2, with the fidelity tests pinned at the current answered counts.
2. `feature/asl-unit-los3-buildings-bridges`: part 2 of section 3.
3. `feature/asl-unit-los3-hexside-terrain`: part 3 of section 3; U16.

This design is merged with the step 15 plan.

## 6. Not in this step

- Partial orchards and entrenchments, until a fixture board has them.
- Counters, night, overlays, and scenario-specific rules.
- Everything step 16 adds: hexside aiming points, the hindrance breakdown, and LOS from a unit in a live game.
