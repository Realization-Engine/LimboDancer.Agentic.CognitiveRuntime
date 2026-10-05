# Scenario A1 Backlog Pass 10: source and case review

**Status:** Reviewed. The package `scenario-a1-fire` is revised for movement and terrain and republished with its prior manifest digest kept. It has no execution authority. Backlog pass 10 (Movement and terrain).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 10), and 5 (rulings R10.1 to R10.15). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 10 Design](<ASL Unit Backlog Pass 10 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

Infantry movement costs and levels (A4.12 to A4.134, B10.4, B10.51, B23.22 to B23.422), walls and hedges (B9.1 to B9.41), Height Advantage (B10.31), marsh (B16.2 to B16.4) and rubble (B24.2 to B24.4), Bypass (A4.3 to A4.34, A12.151), Hazardous Movement (A4.62), concealed movement and entry into concealed Locations (A12.11 to A12.15), Snap Shots (A8.15), PBF and TPBF (A7.21, A7.212), berserk charges (A15.43 to A15.432), and the Terrain Chart rows for walls, hedges, hills, marsh, and rubble (p. 698).

## Sources

`asl-scenario-a1.pass10-pdf-comparison.json` (`71b543b0...`) compares 40 subjects: A4.12, A4.133, A4.134 (in two parts across the page break from p. 48 to p. 49), A4.3, A4.31, A4.32, A4.34, A4.62, A8.15, A12.151 (with its continuation), A6.21, B9.1, B9.2, B9.3, B9.31, B9.32 (with its continuation), B9.323, B9.33, B9.35, B9.4, B9.41 (with its continuation), B10.4, B10.51, B16.2, B16.3, B16.32, B16.4, B23.22 (with its continuation), B23.25, B23.26, B23.421 (with its continuation), B23.422, B24.2, B24.3, and B24.4. A4.31, A8.15, B9.35, B9.41, and B24.2 match in two parts, an example interrupting them on the page. `AslScenarioA1FireSourceReview.BuildPass10` verifies each against the PDF with the pass 7 normalization. Rules already verified by earlier comparisons (A4.132, A4.14, A4.42, A4.7, A4.72, A6.7, A12.14, A12.141, A12.15, A15.43 to A15.432, B3.4, B23.4) are cited from them. B9.321 matches in three parts (the rule and two examples), which the comparison method does not admit, so it is cited by page only; B10.3, B10.31, and B23.23 have no registered fragment of their own (printed beside illustrations) and are read from the rendered pages. The Terrain Chart rows are transcribed in `b-terrain-chart-pass10.transcription.json` (`5169bfc2...`).

| Artifact | Digest | Prior manifest kept |
|---|---|---|
| Fire package manifest | see `ScenarioA1FirePackage.ManifestSha256` | `f10455b9...` |

The Fire case matrix holds 248 fragments and 38 cases.

## Rulings

R10.1 to R10.15 are in the plan, section 5, as revised after both reviews.

## Referee findings

The referee checked the rulings, the transcription (rendered), the package code, and the planner's terrain reads against the rulebook.

| Finding | Rule | Resolution |
|---|---|---|
| 1. Height Advantage in a group needs only one lower firer. | A.5, A7.52, B10.31 | Fixed: any lower firer not excluded by the Crest Line clause gives it to the whole group; R10.4 reworded. |
| 2. A unit keeping its building or woods TEM holds no Wall Advantage. | B9.31, B9.321 EX | Fixed: only a unit with no positive in-hex TEM holds WA; R10.6 reworded. |
| 3. A firer above the wall holds no Wall Advantage. | B9.32, B9.35 EX | Fixed: only a ground-level unit at the wall's level. |
| 4. A Snap Shot leaves no Residual FP. | A8.223, p. 60 | Fixed in the package, with a test. |
| 5. A Bypassing target in a hex with a wall or hedge. | A4.34 EX | Fixed: refused until the vertex LOS is built (backlog section 20). |
| 6. Walls, hedges, SMOKE, and rubble of either hex modify a Snap Shot. | A8.15, B9.42 EX | Fixed: such a Snap Shot is refused (backlog section 20); R10.13 reworded. |
| 7. Through a road gap the wall TEM applies only to a non-moving target. | B9.3 | Fixed; R10.5 says so. |
| 8. A broken unit is armed and bars Bypass. | A4.3 | Fixed. |
| 9. A Snap Shot at the climbed hexside takes no Height Advantage. | B10.31 | Fixed. |
| 10. Only an unarmed, unarmored vehicle frees a unit from A7.212. | A7.212 | Fixed. |
| 11. The mover chooses between the road rate and the terrain's cost. | A4.132, B3.3 | A deviation in backlog section 1. |
| 12. A Minimum Move into marsh from below costs twice the allotment. | A4.134 EX | Fixed. |
| 13. Hazardous Movement applies only to the pushing units. | A4.62, A.5 | Fixed: only when every target is the pushing crew. |
| 14. A moving stack of Dummies is removed; A.9 removes only Dummies drawn above a real unit. | A12.15, A.9 | The first fixed, with a test; the second a deviation in backlog section 1. |
| 15. Wall Advantage by arrival order returns it to a unit that broke and rallied. | B9.322, B9.323 | A deviation in backlog section 1; R10.6 says so. |
| 16. A4.42 lets any SMC lend its IPC to a unit of the player's choice. | A4.42 | A deviation in backlog section 1; R10.8 says so. |
| 17. Marsh is a Hindrance only at the same level. | B16.2 | Fixed. |
| 18. The transcription's legend lacked the *, ■, and † entries. | p. 698 | Fixed. |

It confirmed the five transcribed rows cell by cell, the wall and road costs, rubble and marsh, rooftops and cellars excluded, stairwells and upper levels, the hill and Abrupt Elevation Change costs, the vertex and hexspine reading of B9.3, B9.33 and B9.35, the wall TEM in place of a lower in-hex TEM with FFMO negated and the Residual FP lowered, PBF by level, TPBF, the Snap Shot arithmetic, Hazardous Movement, Height Advantage with no other positive TEM, the Bypass cost and route, the Road and leader bonuses, the Minimum Move reading, concealed movement, and the A12.15 forced back.

## Table player findings

The table player read the planner, the projector, the records, and the Play page as a player would. Each defect is fixed with a test or recorded.

| Finding | Rule | Resolution |
|---|---|---|
| 1. A stack in Bypass lost its Bypass when it split, made a SMOKE attempt, or another member stepped. | A4.3, A4.32 | Fixed: the stack moves on together, and no SMOKE attempt or other step is taken in Bypass, with a test. |
| 2. Occupying the Bypassed obstacle skipped the enemy checks. | A4.14, A12.15 | Fixed: occupying an obstacle that holds enemy units is refused (backlog section 20), with a test. |
| 3. A stack Bypassing a walled hex could not be fired on. | R10.7 | Fixed: a hex with a wall or hedge is not Bypassed, with a test. |
| 4. Fire at a Bypass Location stripped the TEM of other units there. | A4.3, A7.6 | Fixed: a hex holding friendly units is not Bypassed, and fire from within the hex is refused. |
| 5. On a reversed board the pass read the opposite hexside. | B9.3, A4.31 | Fixed: hexsides are read in the map's frame, as the composed map read does. |
| 6. Residual FP in a wall or hedge hex was refused, and took the obstacle's TEM against a Bypassing stack. | A8.22, A4.34 | Fixed: Residual FP takes no wall TEM and the Bypass lane's terrain. |
| 7. A group with one LOS across the wall was refused; units adjacent across a wall since setup could not fire. | B9.3, A.5, B9.32 | The group takes the highest wall TEM; the setup tie is refused and recorded in backlog section 20. |
| 8. A Snap Shot at a Bypass step aimed at the wrong hexside. | A8.15 | Fixed: refused. |
| 9. Berserk charges never use stairwells or upper levels. | A15.431 | Backlog section 20. |
| 10. A charge could stick on marsh entered from below. | B16.4 | Fixed: not on any route. |
| 11. A Dummy did not get its leader's bonus. | A12.11 | Fixed, with a test. |
| 12. The advance gave every MMC the leader's IPC. | A4.72 EX, A4.42 | Fixed: two MF to each, the IPC to the one laden MMC. |
| 13. The building entry route dropped Assault Movement, Double Time, and Minimum Move. | R10.12 | Fixed: a move with any of them is planned as a movement step. |
| 14. Assault Movement into marsh. | A4.61, B16.4 | Fixed: refused, with a test. |
| 15. A unit pinned in Bypass ends up in the obstacle. | A4.32, A4.33 | A deviation in backlog section 1. |
| 16. Replay trusts the reveal, the Bypass exit, the Road Bonus, the leader bonus, and a Minimum Move's allowance. | A12.15 | A deviation in backlog section 1, as for other planner reads. |
| 17. The page did not show Bypass, a Minimum Move, or a forced back. | | Fixed: the movement status says so. |
| 18. TPBF by units in the Bypassed obstacle. | A7.212 | Refused with finding 4. |

It confirmed the step costs, marsh, Minimum Move, the Road and leader bonuses, concealed movement, the A12.15 reveal and forced back, the Bypass geometry, the wall TEM and Wall Advantage, Height Advantage, fire by level, Snap Shots, Hazardous Movement, and the records' round trip.

## Visual check

In the Studio (`map-studio-scripted`), a new game `pass10-check` on board 01 (July 1942) with a German squad in the two-story house F1 and a Russian squad in Z9. In the German MPh the move panel showed the Minimum Move and Bypass controls. The move to `bd01:F1:1` read "play.move: g1 enter bd01:F1:1 (wooden-building) for 1 MF", the stairwell VASL marks in F1; the movement status read "g1 in bd01:F1:1, step 1: the DEFENDER may fire, or pass". After the DEFENDER passed, the move along level 1 to G1, the same building, read "for 2 MF", and a step from level 1 of G1 to the ground of G2 was refused: "play.move-upper-level: from an upper level a unit moves only into the same level of an ADJACENT hex of the same building (B23.421, B23.422)". The page was driven through its DOM events and read as text.
