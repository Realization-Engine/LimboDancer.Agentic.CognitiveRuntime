# Scenario A1 Backlog Pass 6: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire` and `scenario-a1-ordnance` are revised and republished with their prior manifest digests kept. Neither has execution authority. Backlog pass 6 (vehicles, part 2).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 6), and 5 (rulings R6.1 to R6.10). The user approved the plan and asked for the passes to run without pausing. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 6 Design](<ASL Unit Backlog Pass 6 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

An AFV's and a wreck's TEM and LOS Hindrance for Infantry (D9.3, D9.4, D10.3, A4.6, A6.7, D2.41, C6.1 Case J), burning wrecks and their smoke (B25.14, B25.141, B25.2, A24.2, A24.8), wreck creation and entry costs (D10.1, D10.2, D2.14), Residual FP against vehicles (A8.2, A8.222, A7.308, D.8B), vehicle concealment and entry into concealed Locations (A12.2, A12.12, A12.13, A12.3, A12.4, A12.41), vehicle fire in the MPh (D3.3, D3.31, D2.42, A8.1), and vehicle MG repair (D3.7).

## Sources

`asl-scenario-a1.pass6-pdf-comparison.json` (`adb5ff41...`) compares 14 subjects not verified before: A8.222, A12.2 (in two parts across the page break from p. 79 to p. 80), A12.12, A12.4, A12.41, A24.2, A24.8, B25.14, B25.141, B25.2, D3.3, D10.1, D10.2, and D10.3. D9.3, D9.4, D2.14, D2.41, D3.7, A4.6, A6.7, A8.2, and A12.13 were verified by earlier batches. `AslScenarioA1FireSourceReview.BuildPass6` verifies each against the PDF.

| Artifact | Digest | Prior manifest kept |
|---|---|---|
| Fire case matrix (197 fragments, 26 cases) | `80f5f161...` | |
| Fire package manifest | `a3037939...` | `f36022ca...` |
| Ordnance case matrix (53 fragments, 13 cases) | `fd72783c...` | |
| Ordnance package manifest | `68fdc545...` | `2e3621f2...` |
| Vocabulary 1.10.0 | `57e8aa9d...` | |

## Rulings

R6.1 to R6.10 are in the plan, section 5, as revised after both reviews.

## Referee findings

The referee re-read every cited rule and checked the rulings and the package code.

| Finding | Rule | Resolution |
|---|---|---|
| D1. Bounding First Fire did not halve the MG; only Motion did. | D3.31, p. 200; D2.42, p. 198 | Fixed: Bounding Fire halves it, and a Non-Stopped vehicle's fire is halved again, with a test; R6.9 says so. |
| D2. Residual FP ignored the SMOKE of its Location. | A8.2, p. 60; A24.2, p. 91 | Fixed: the game supplies a burning wreck's smoke as Residual FP's Hindrance, which also cancels FFMO, with a test; R6.6 says so. |
| D3. The Case J clause counted any MP spent; it applies to a vehicle that entered a new hex or was in Motion, and to its wreck. | C6.1, p. 173; D9.3, p. 209 | Fixed: `MovedVehicles` counts hex entries and moves under a Motion counter, and wrecks keep it, with a test; R6.1 and R6.2 say so. |
| D4. Case H was checked only at setup and after the vehicle's own MP. | A12.2, p. 79 | Fixed for movement: Case H is checked after every MF or MP expenditure of either side, with a test; other changes (a Rally) are in backlog section 16. |
| D7. R6.5 did not cite B25.14's causes of a burning wreck or the reverse-side test. | D10.1, p. 210; B25.14, p. 143 | R6.3 and R6.5 cite them; every catalog vehicle has a wreck face; the other causes are in backlog section 16. |

It disputed these readings:

| Reading | Rule | Resolution |
|---|---|---|
| A vehicle loses "?" when it enters a hex, changes its VCA, or is in Motion, not for any MP (a Start or a Stop is not movement). | A12.2, p. 79 | Adopted, with a test that a Start keeps it. |
| Brush, woods, and in-season orchards are Concealment Terrain too, and the road clause applies. | A12.2; Terrain Chart, p. 698 | Vehicles are not admitted in brush, woods, or orchards, so grain is the only case; the road clause is in backlog section 16. |

Its notes are in the rulings (a concealed AFV's Hindrance is real, since a "?" vehicle is never a Dummy here; smoke is added on top of the A6.7 maximum; the D9.3 exceptions; Shock and a Hero Rider for repair) or the backlog (Bypass, entrenched and Dug-In vehicles, Terrain Chart Note D). The ordnance ruling's text now names why only a wreck applies there.

## Table player findings

The table player read the planner and the Play page as a player would. Each defect is fixed with a test.

| Finding | Rule | Resolution |
|---|---|---|
| 1. An AFV or wreck in a Hindrance hex added nothing where the map had a Hindrance at that range. | A6.7 and its example, p. 54 | Fixed: a vehicle in a brush or in-season grain hex adds its +1 to that hex's, with a test; R6.2 says so. |
| 2. The Bounding First Fire control vanished once the vehicle Stopped, so the half-FP shot of a Stopped vehicle was out of reach. | D3.31 | Fixed: the control shows whenever the vehicle may fire, and hides once it has a Bounding Fire counter. |
| 3. A vehicle could not fire before its first MP expenditure. | D3.3, p. 199 | Fixed: an unmoved phasing vehicle may fire while no other move is under way, with a test; R6.9 says so. |
| 4. Only the phasing side could repair a vehicle MG. | D3.7, p. 201; A9.72 | Fixed in the planner and on the page, with a test; R6.10 says so. |
| 5. A concealed vehicle destroyed left a concealed wreck the enemy never saw. | D10.1 | Fixed: a wreck is never concealed, with a test; R6.5 says so. |
| 6. The vehicle Hindrance compared in-hex levels, not absolute ones. | D9.4, p. 210 | Fixed: the vehicle's hex's absolute level must equal the firer's. |
| 7. Case H is not checked after a Rally. | A12.2 | In backlog section 16; R6.7 says so. |
| 8. A refused entry disclosed a concealed enemy vehicle, and fire at a "?" vehicle counted it as seen. | A12.2 | Fixed: the entry is "not decided here", and a concealed vehicle target is unseen for the firing side's refusals. |
| 9. The repair list offered vehicles whose crews could not repair, under an SW-only label. | D3.7 | Fixed: only CE, unstunned crews are offered, and the label and help text name the AAMG. |
| 10. Barring a Recalled crew from repair is stricter than D3.7's "shocked or stunned". | D3.7, D5.341 | Kept as a reading: a Recall is treated as a Stun (D5.341); R6.10 says so. |
| 11. An AFV that used Defensive First Fire cannot Final Fire. | A8.4 | A recorded simplification (R6.9; backlog section 16). |
| 12. The Units table showed MP for a wreck. | | It shows "wreck". |

It confirmed the wreck TEM and its exceptions, the Case J timing, the smoke values, Residual FP and its smoke, entry costs, the A12.41 reveals, the moving vehicle's reveal, the Bounding Fire halvings and AFPh bar, and the Blaze's extra MF.

## Visual check

In the Studio (`map-studio-scripted`), a new game with a German truck in B8 and two Russian squads in B10: the Russians' Prep Fire with scripted dice 2 and 4 eliminated the truck on the Vehicle line, and the Units table showed it wrecked at B8 while the map drew its wreck face ("German wrecked Opel 6700 (Blitz) vehicle, Passenger survival 6"). The pane did not draw, so the check read the page's text and the map's accessible names.
