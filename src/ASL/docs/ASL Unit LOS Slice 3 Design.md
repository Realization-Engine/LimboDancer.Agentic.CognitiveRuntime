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
- **Slopes** (F2.3), where a fixture board has them.

Each rule becomes answered only when the fixtures exercise it and every answered pair agrees. A rule a board never reaches stays unsupported.

## 4. Tests

- **Fidelity:** every answered pair on every fixture agrees with VASL (U14 to U16), with the answered counts pinned.
- **Names:** every unanswered pair names partial orchards, out-of-season orchards, or entrenchments, or a situation the as-built notes list with its reason.
- **Synthetic:** a case per reproduced group in `LosTests`, where a synthetic board can show it, confirmed to fail with the rule disabled.

## 5. Build order

1. `feature/asl-unit-los3-fixtures`: the fixtures of section 2, with the fidelity tests pinned at the current answered counts.
2. `feature/asl-unit-los3-buildings-bridges`: part 2 of section 3.
3. `feature/asl-unit-los3-hexside-terrain`: part 3 of section 3; U16.

This design is merged with the step 15 plan.

## 6. Not in this step

- Partial orchards, out-of-season orchards, and entrenchments, until a fixture board has them.
- Counters, night, overlays, and scenario-specific rules.
- Everything step 16 adds: hexside aiming points, the hindrance breakdown, and LOS from a unit in a live game.
