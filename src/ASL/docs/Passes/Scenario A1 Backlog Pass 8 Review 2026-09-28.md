# Scenario A1 Backlog Pass 8: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire`, `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are revised for catalog 1.7.0 and republished with their prior manifest digests kept. None has execution authority. Backlog pass 8 (Guns, part 2).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 8), and 5 (rulings R8.1 to R8.12). The user approved the plan and asked for the passes to run without pausing. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 8 Design](<ASL Unit Backlog Pass 8 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

ROF, First and Final Fire, and Intensive Fire (C2.24, C2.241, C2.5, C5.6 to C5.63); Defensive First Fire by ordnance (C6.1 to C6.17, A8.1); Covered Arc changes and the C2.6 limit (C2.6, C3.21, C3.22); Cases E, H, and M (C5.5, C5.51, C5.8, C6.4 to C6.43); Guns and their crews as targets (C11.1 to C11.6); Gun movement and towing (C10.1 to C10.3, A4.41); overstacking (A5.12 to A5.131); a crew's inherent fire (A7.352); concealment (A12.14, A12.141, C6.57); the Ordnance Listings' BPV, dates, and Note O (pp. 351, 354, 363).

## Sources

`asl-scenario-a1.pass8-pdf-comparison.json` (`44a3f222...`) compares 54 subjects: C2.24, C2.241, C2.5, C2.6, C3.21, C3.22, C5.5, C5.51, C5.6, C5.61, C5.62, C5.63, C5.8, C6.11 to C6.17 (C6.17 in two parts across the page break from p. 173 to p. 174), C6.4 to C6.43, C6.57, C10.1, C10.11, C10.111, C10.12, C10.23, C10.3, C11.1 to C11.5, A5.12, A5.13, A5.131, A7.352, A12.14, and A12.141, some with continuations. `AslScenarioA1FireSourceReview.BuildPass8` verifies each against the PDF with the pass 7 normalization. C2.2401 (Gun Duels) did not match whole and is not cited, since Gun Duels are not built.

| Artifact | Digest | Prior manifest kept |
|---|---|---|
| Catalog 1.7.0 | `4657c440...` | |
| Fire case matrix (201 fragments, 29 cases) | `708356eb...` | |
| Fire package manifest | `5ed50cc5...` | `e00107a7...` |
| Rally package manifest | `197c68e4...` | `fdae11e5...` |
| Close Combat package manifest | `663e30b0...` | `59e5b645...` |
| Ordnance case matrix (140 fragments, 24 cases) | `15ddd033...` | |
| Ordnance package manifest | `09ceeb08...` | `d10ce346...` |

## Rulings

R8.1 to R8.12 are in the plan, section 5, as revised after both reviews.

## Referee findings

The referee checked the rulings, the package code, the six catalog rows, and the package tests against the rulebook.

| Finding | Rule | Resolution |
|---|---|---|
| 1. Intensive Fire was allowed after a Final Fire counter and with a pinned crew. | C5.6, p. 172 | Fixed: both bar it, with a test; R8.2 says so. |
| 2. C6.17's MP were counted without the MP earlier shots claimed. | C6.17 and its EX, p. 174 | Fixed: the MP a shot claims in a Location count neither toward the next shot's J1 or J2 nor toward its limit, with a test; R8.1 says so. A Target Facing change restarting the count is in backlog section 18. |
| 3. Case E during Defensive First Fire (C5.51) turns the CA with Case A. | C5.51, p. 172 | Not built: refused, in R8.8 and backlog section 18. |
| 4. A Critical Hit keeps FFNAM and FFMO. | C3.71, p. 170 | Fixed, with a test; R8.1 says so. |
| 5. A pushing crew had its gunshield, and Hazardous Movement is missing. | C11.5, A4.62 | The gunshield is denied to a moving or pushing crew; Hazardous Movement is in backlog section 18. |
| 6. A squad manning a Gun could take the gunshield and Emplacement. | C11.2, C11.5 | Fixed: both need a crew; R8.3 says so. |
| 7. A squad keeps its inherent FP after firing a Gun; the reverse rule is a project ruling. | A7.351, A7.352, p. 56 | Fixed, and R8.4 names the project ruling. |
| 8. Case H must exclude a crew. | C5.8 | Already so: `IsMmc` excludes crews. |
| 9. "At the end of the phase" as "no more fire that phase" is a deviation. | C3.22 | R8.9 names it as a project ruling. |

It confirmed J1 and J2, C6.16, C2.241, the Intensive Fire mechanics, Cases E, H, and M, overstacking, Emplacement and the gunshield, the Direct Hit test and the Gun Destruction Table, the C2.6 reading, the six catalog rows, and every test value.

## Table player findings

The table player read the planner, the projector, the gate, and the Play page as a player would. Each defect is fixed with a test or recorded.

| Finding | Rule | Resolution |
|---|---|---|
| 1. TI units still moved and fired. | A4.8, C10.11 | Fixed: a TI vehicle, Gun, or crew neither moves nor fires nor turns, with tests. |
| 2. A successful push left no TI. | C10.3 | Fixed: every push makes the Gun and crew TI, and the crew still pushing goes on, with a test. |
| 3. No Intensive Fire after a normal DFPh shot. | C5.6 | Kept: C5.6 bars a crew marked Final Fire; R8.2 records the disputed reading. |
| 4. A vehicle that began its MPh out of LOS lost Case J1 or J2. | C6.15 | Fixed: its starting Location is read too. |
| 5. The Bore Sighting was public. | C6.42 | Fixed: the record is visible to the Scenario Defender only; secrecy until use is in backlog section 18. |
| 6. A Bore Sighting was never lost; tanks may Bore Sight. | C6.41, C6.43 | Fixed: a push or hook-up removes it, with a test; tanks are in backlog section 18. |
| 7. A fire group with a firer within the CA faces the gunshield. | C11.51 | Fixed: any firer within the CA suffices. |
| 8. An abandoned Gun could never be used again, and one misclick abandoned it. | C11, C10.11 | Fixed: a crew or HS on foot hooks up an abandoned Gun, with a test, and the page pushes by default; an unmanned Gun as a target is in backlog section 18. |
| 9. Pushing with Assault Movement, CA changes while pushing, PP, and fire at a failed push. | C10.3 | Assault Movement is refused, with a test; the rest is in backlog section 18. |
| 10. A hook-up opens no DEFENDER window. | C10.11 | Backlog section 18. |
| 11. The verifier trusted state facts. | | Fixed: Bore Sighting, overstacking, and Emplacement are recomputed on replay. |
| 12. A turn without fire was followed by Intensive Fire. | C3.22 | Fixed: a Gun turned this phase fires no more, with a test. |
| 13. Overstacking ignores vehicles. | A5.12 | R8.10 says so; backlog section 18. |
| 14. Case E is rarely reachable; the page did not show C2.6 refusals; a HS may unhook. | C5.5, C2.6, C10.111 | The page shows C2.6 refusals; a HS unhooks and hooks up; Case E's reach is in backlog section 18. |

It confirmed Defensive First Fire timing and targets, the per-Location limit, First Fire markers, crews' fire both ways, gunshield and Emplacement for moving crews, concealment with the crew, CA turning and movement, elevation and bearings, towing, the Bore Sighting setup checks, and the record readbacks.

## Visual check

In the Studio (`map-studio-scripted`), a new game on board 01, July 1942, the Germans the Scenario Defender, with the leIG and its crew in B10 facing north-east Bore Sighting B8 and a Russian squad in B8. In the PFPh the Ordnance panel read "bd01:B8:0: range 2, in its CA", and the map carried the Covered Arc layer. The shot with scripted dice 5 and 6 read "- 1 (case-l, C6.3) - 2 (case-m, C6.4) = Final DR 8: hit", its colored 5 above ROF 2 ending its fire; the Intensive Fire shot then read "+ 2 (case-f, C5.61)". The pane did not draw, so the check read the page's text and markup.
