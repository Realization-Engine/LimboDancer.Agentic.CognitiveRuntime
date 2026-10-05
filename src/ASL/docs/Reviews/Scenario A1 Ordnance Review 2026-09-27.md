# Scenario A1 Ordnance: source and case review

**Status:** Reviewed. The package `scenario-a1-ordnance` is published, and `scenario-a1-fire`, `scenario-a1-rally`, and `scenario-a1-close-combat` are revised and republished with their prior manifest digests kept, all with no execution authority. Unit step 24 (ordnance), pass 3 of the deviations.

**Date:** 2026-09-27

**Plan:** [ASL Unit Deviations, Ordnance, and Vehicles Plan](<../Plans/ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), sections 9 and 11. The user approved the plan, its rulings subject to the second-pass reviewer, and autonomous passes on 2026-09-27. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, for the catalog rows, the sources, the To Hit Table transcription, and the package rulings;
- a table player, for the live stage.

**Design:** [ASL Unit Deviations Pass 3 Design](<../Passes/ASL Unit Deviations Pass 3 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

- A Gun of the reviewed catalog, manned by its own nationality's Infantry crew, fires HE on the Infantry Target Type at the enemy units of one Location in the PFPh, AFPh, or DFPh (C3.3, C3.32).
- The To Hit DR against the Modified TH# with Cases A, B, D, K, L, N, Q, and R; Critical and Improbable Hits; ROF; breakdown; Acquisition.
- A hit's IFT attack on the Gun's HE FP column, through the Fire package.

## Counters

Catalog 1.4.0 adds four counters (78 rows): the German 7.5cm leIG 18 (sheet OLG, p. 351), the Russian 45mm PTP obr. 32 (sheet OLR, p. 363), and a 2-2-8 Infantry crew of each side (sheet NCC, p. 695, with A1.123, p. 44). The referee rendered the listing rows and the chart and confirmed all 78 rows: the overscored 75* (no AP), the 45L, ROF 2 and 3, the blank B# column read as the inherent B# 12 (C2.28), range 115 and 110, M# 10 and 11, TSize +1 as a Small Target (C2.271), NT and QSU, H7, and the A4 to A7 APCR superscripts by year; the crews' 2-2-8 with broken Morale 8, BPV 8 (German) and 6 (Russian), elite, and Self-Rally. It noted that a Gun's BPV, dates, and Animal-Pack capability (note O) have no vocabulary attribute (in the backlog).

## Sources

`asl-scenario-a1.ordnance-pdf-comparison.json` (`cebc0e78...`) compares 49 subjects, made as the earlier comparisons were (whole fragments against `pdftotext 4.00 -raw` page text, normalized): C.3, C.4, C.6, C2.1 to C2.6, C3.2 to C3.8, C4.1 to C4.5, C5.1 to C5.8, C6.2 to C6.9 (pp. 162 to 175), A1.123 (p. 44), and A21.13 (p. 88). Each occurs whole on its page. The matrix has 49 fragments; the Fire matrix gains C.3, C.4, C.6, C3.53, C3.71, and C3.74 (146 fragments).

The chart is the C3 To Hit Table's Infantry row with the C4 Gun and Ammo modifications (p. 700), transcribed in `Supplements/c3-to-hit-table.transcription.json` (`936b62d5...`): ten range columns, the black and red Basic TH# (black first, confirmed on a rendering), and the *, L, LL, 57mm-or-less, and 40mm-or-less rows. The IFT's FP/caliber headers (p. 692) give the HE FP (C.6): 12 for 75mm, 4 for 45mm. The TH# color is the National Capabilities Chart's (p. 695): German black, Russian red.

## Rulings

The plan's section 11 records R24.1 to R24.8 as built.

## Referee findings

The referee re-extracted the pages, rendered the To Hit Table, the p. 702 summary boxes, and the listing rows, and confirmed the transcription cell by cell, the TH# colors, the HE FP, Cases A, B, D, K, L, N, Q, and R, Improbable Hits, the Critical Hit rule and its resolution (C3.71: no Air Burst, since a Gun's fire is Direct, B13.3), the normal hit's Effects DR with no DRM, ROF, breakdown, the AFPh limit, and the Acquisition mechanics.

It found these defects:

| Finding | Rule | Resolution |
|---|---|---|
| D1. A pinned crew could turn its Gun. | A7.81, p. 58; C5.11, p. 172 | Fixed: refused (R24.8). |
| D2. Case N applied against a concealed Location. | C6.51, p. 174; C6.57 EX, p. 175 | Fixed: no Case N; the shot loses the Acquisition and acquires afresh at -1 only when it costs a concealment (R24.6). |
| D3. Targets were not checked as enemy units. | C3.32, p. 169 | Fixed: refused. |
| D4. A moved Gun had no input (C2.8, A4.41). | p. 169, p. 50 | No Gun moves in this build and crews may not leave their Guns, so it cannot arise; recorded (R24.8). |
| D5. A crew's inherent fire was tracked per phase. | A3.5, A3.4, p. 47 | Crews' inherent fire is not built (the Fire package refuses crews as firers), so it cannot arise; recorded (R24.8, backlog). |
| D6. A Gun that fired from woods or a building could turn for another shot. | C5.11, p. 172 | Fixed: refused (R24.8). |
| D7. CX, Encircled, overstacked firer, overstacked target, and Opportunity Fire DRM were neither applied nor refused. | A4.51, A7.7, A5.12, A5.131, A7.25 | Overstacked Locations are refused; a crew cannot become CX, since it cannot move; Encirclement and Opportunity Fire are not built anywhere (in the backlog). |
| D8. A DFPh shot after a kept ROF in Defensive First Fire was refused. | A3.3, C2.241 | Guns do not Defensive First Fire in this build, so the case cannot arise; the Gun fact's comment is corrected. |
| D9. Random Selection ties were cited to A7.301. | A.9, p. 43 | Fixed. |
| D10. The matrix said one Effects DR, the code rolled two. | C3.32, C3.74 | Fixed as reading C below. |
| D11. Rubble and C2.5's Intensive Fire counter. | C5.11, C2.5 | Rubble is not a reviewed terrain; no reviewed Gun lacks a Multiple ROF. Recorded. |

And these disputed readings:

| Reading | Resolution |
|---|---|
| A. The bracketed exception of C3.7 (a Critical Hit only on a dr of 1 when only the lowest Final DR hits) belongs to the Area and Vehicle Target Types; the p. 702 box omits it for the Infantry Target Type. | Adopted (R24.7). |
| B. Beyond 60 hexes C4.2 gives -5 where the chart's last column prints -4. | The chart is used; a Covered Arc is read on one board, so ranges stay under 34 hexes. |
| C. One Effects DR for the whole Location (C3.32), the Critical Hit's units on the doubled column. | Adopted (R24.5). |
| D. A crew firing its inherent FP and then its Gun in one phase. | Crews' inherent fire is not built; the refusal stays as a guard. |

## Live stage

The table player, a separate agent briefed as an experienced ASL player, read the live code, the Play page, and the tests, and played probe games in a throwaway project outside the repository. It confirmed the Covered Arc (the six hexspines, the hex centers, and the 30-degree wedge with its boundary rows, C3.2), the recorded and shown facing, Case A only on the turning shot (C5.12), the fixed Covered Arc from woods or a building (C5.11), ROF (C2.24, C2.5, C5.4, C5.2), the fire markers, a kept ROF used at another Location, Acquisition across phases and Player Turns (C6.53), Improbable and Critical Hits, the shared Effects DR, malfunction, and replay.

It found three ways a game could get stuck, and these other differences:

| Finding | Rule | Resolution |
|---|---|---|
| 1. A berserk unit charged into a crew's Location, and the CCPh required a CC the package refuses. | A15.43 | Fixed: a step into a Location holding an enemy crew ends the charge in place (R30.5), and no CC is required where a crew is. |
| 2. An enemy advancing into a crew's Location was held in Melee forever. | A11.15, A3.7 | Fixed: the advance is refused while CC with a crew is not built (R24.3). |
| 3. A crew held in Melee fired its Gun at another Location. | A11.15 | Fixed: refused, and a captured crew too. |
| 4. A concealed crew could never fire its Gun. | A12.14, p. 78 | Fixed: it fires and loses its "?", undecided unless a Good Order target is within 16 hexes, as for Infantry fire. |
| 5. Acquisition is kept on the Location, where C6.5 puts it on the target unit and lets it follow the unit within LOS. | C6.5, C6.51 | A recorded deviation (R24.6, backlog). |
| 6. A Gun cannot change its Covered Arc without firing. | C3.22, p. 169 | In the backlog. |
| 7. The planner always turns the fewest hexspines. | C3.21, C5.1 | In the backlog (already listed). |
| 8. The verifier did not check that the recorded facing lies the recorded number of hexspines away. | | Fixed. |
| 9. The advance's stacking count ignored crews while the Gun's counted them. | A5.1 | Fixed: both count a crew as a HS. |

Its page findings, and what was done:

- Fixed: a refusal now reads as a refused shot with its reasons, and the proposal shows the Modified TH# and every DRM before any roll.
- Fixed: the Gun list shows malfunctioned Guns (Gun repair is not built), Guns that fired this Player Turn, the ROF, the Acquisition, and the crew's state.
- Fixed: the record says the Gun "may fire again this phase" instead of naming a lowered ROF.
- Fixed: "Gun facing (hexspine)" appears only for a Gun, with a note that north and south are hexsides, and "Or held by" is cleared after each placement.
- In the backlog: marking which target Locations are in the Covered Arc, in range, or refused, and drawing the Covered Arc on the map.

## Digests

| Artifact | SHA-256 |
|---|---|
| Catalog 1.4.0 | `dae90ca1...` |
| Ordnance case matrix | `644dfe5f...` |
| Ordnance package manifest | `1bf0bb77...` |
| To Hit Table transcription | `936b62d5...` |
| Ordnance PDF comparison | `cebc0e78...` |
| Fire case matrix | `8a6e7164...` |
| Fire package manifest | `b944ed34...` (prior `8ac91d10...`) |
| Rally case matrix | `568dab9e...` |
| Rally package manifest | `7784925f...` (prior `aa60fdde...`) |
| Close Combat case matrix | `7290f1c8...` |
| Close Combat package manifest | `02e31479...` (prior `f729b9a8...`) |

## Cases

The Ordnance matrix has ten cases: two resolved (a hit, a miss), six abstained (phase, Gun, crew, already fired, range, target), and two indeterminate (levels, Hindrance, or mixed concealment; a missing roll). The reachability walks cover every ordered To Hit DR, every subsequent dr, and one IFT DR per total for five shots: a German Gun in the open, one at two targets in a stone building with Acquisition, a Russian Gun at long range, one whose every Final DR misses (Improbable Hits), and a pinned crew in the AFPh from woods.
