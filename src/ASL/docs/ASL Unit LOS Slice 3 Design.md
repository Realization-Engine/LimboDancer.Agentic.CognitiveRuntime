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
