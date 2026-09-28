# Scenario A1 Backlog Pass 9: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire`, `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are revised for catalog 1.8.0 and republished with their prior manifest digests kept. None has execution authority. Backlog pass 9 (Mortars, SMOKE, and anti-tank weapons).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 9), and 5 (rulings R9.1 to R9.9). The user approved the plan and asked for the passes to run without pausing. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 9 Design](<ASL Unit Backlog Pass 9 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

Light mortars and spotting (C9.1 to C9.5), the Area Target Type (C3.33, C3.331), Indirect Fire TEM and Air Bursts (C1.52, B13.3), SMOKE grenades (A24.1 to A24.8), the Panzerfaust (C13.1 to C13.36, C13.8), the To Hit Table's Area row (p. 700), and the two light mortar listing rows (pp. 351, 363).

## Question for the user

The Panzerschreck and the ATR are not built. The PSK's To Hit Table is printed only on its counter (C13.48), and no registered source prints the ATR counter's B#, ROF, or PP (C13.2). As with the manufactured MG values of ruling R0.3, they need the user's ruling or a source.

## Sources

`asl-scenario-a1.pass9-pdf-comparison.json` (`0f9e72fb...`) compares 23 subjects: C9.1, C9.2 (with its continuation), C9.3, C9.31, C9.4, C9.5, C3.33 (with its continuation), C3.331, C1.52, C13.1, C13.3, C13.31, C13.32 (in two parts across the page break from p. 183 to p. 184), C13.33, C13.34, C13.36, C13.8, A24.1, A24.11, A24.5, and A24.7. `AslScenarioA1FireSourceReview.BuildPass9` verifies each against the PDF with the pass 7 normalization. The Area row is transcribed in `c3-to-hit-area-row.transcription.json` (`33718432...`).

| Artifact | Digest | Prior manifest kept |
|---|---|---|
| Catalog 1.8.0 | `5ae459fd...` | |
| Fire package manifest | see `ScenarioA1FirePackage.ManifestSha256` | `5ed50cc5...` |
| Rally package manifest | see `ScenarioA1RallyPackage.ManifestSha256` | `197c68e4...` |
| Close Combat package manifest | see `ScenarioA1CloseCombatPackage.ManifestSha256` | `663e30b0...` |
| Ordnance package manifest | see `ScenarioA1OrdnancePackage.ManifestSha256` | `09ceeb08...` |

The Fire case matrix holds 207 fragments and 30 cases, the Ordnance case matrix 158 fragments and 29 cases.

## Rulings

R9.1 to R9.9 are in the plan, section 5, as revised after both reviews.

## Referee findings

The referee checked the rulings, the package code, the 36 catalog rows, the Area row, and the package tests against the rulebook.

| Finding | Rule | Resolution |
|---|---|---|
| 1. A mortar's Defensive First Fire leaves Residual FP. | A8.2 | A deviation, as for Guns: backlog section 1. |
| 2. SMOKE's +2 applies to Residual FP; only the outgoing +1 does not. | A8.2, A24.8 | Fixed, with the ruling. |
| 3. Area Target Acquisition applies to concealed units and always steps. | C6.521, C6.57, p. 175 | Fixed, with a test. |
| 4. A mortar that moved in the MPh does not fire in the AFPh. | A4.41, p. 51 | Fixed, with a test. |
| 5. No mortar fire from a building Location. | B23.423, p. 137 | Fixed, with a test. |
| 6. A squad fires one SW after its inherent FP. | A7.351, C13.31 | Fixed for mortars and PF Checks. |
| 7. A leader goes on directing a SW's further ROF shots. | A7.53 | Fixed. |
| 8. A pinned unit does not fire a PF from a ground-level building. | C13.8, p. 185 | Fixed, with a test. |
| 9. The C3.7 exception is judged per unit. | C3.7, C3.331 | Fixed: per unit with one subsequent dr, with a test. |
| 10. A PF at range 0 is TH# 10. | C13.33 | Named: not reached, since Infantry and an enemy vehicle never share a Location (R25.3). |
| 11. Light mortars may Bore Sight. | C6.41 | Backlog section 19. |
| 12. SMOKE placement at other levels; a 6 ends only the placing squad's MPh. | A24.1 | Deviations in backlog section 1; the 6 already ends only the placer. |
| 13. A Spotter is designated in the PFPh or DFPh. | C9.3 | Fixed: no spotted fire in the AFPh, with a test. |
| 14. The OB for the PF limit; a PF Check after a First Fire counter. | C13.31 | The OB is the setup (no reinforcements); a First Fire counter bars only a squad's check in the MPh. |
| 15. Inexperienced PF firers are reduced on an 11 or 12. | C13.36, A19.32 | Fixed, with a test. |

It confirmed the two listing rows (rendered), B# 12, the malfunctioned sides not in any source, the Area row, the 6 FP halved to the 2 column and a Critical Hit at 12, the TEM and Air Bursts, the DRM of a light mortar and of a PF by the chart's marks, the PF Check, the PF's range, TH#, TK#, and usage limit, the leadership, and the SMOKE values.

## Table player findings

The table player read the planner, the projector, the gate, and the Play page as a player would. Each defect is fixed with a test or recorded.

| Finding | Rule | Resolution |
|---|---|---|
| 1. A mortar's Acquisition was dropped at once. | C6.521, C9.2 | Fixed: a Good Order possessor keeps it, with a test. |
| 2. A squad's fire counter from an earlier phase did not bar its mortar or PF. | A7.351, C13.31 | Fixed: a squad that fired in an earlier phase, or is marked First Fire in the MPh for a PF, uses no SW, with a test. |
| 3. MGs fired through the Fire action were not counted as SW. | A7.351 | Fixed: a squad that fired its inherent FP with a MG fires no mortar and makes no PF Check, and one that used its mortar fires its inherent FP with no MG, with a test. |
| 4. A PF's Casualty Reduction dropped the fire marker. | C13.36 | Fixed: the HS, the broken unit, and the wounded leader carry the phase's marker, with a test. |
| 5. The SMOKE record's exponent, cost, and counter were taken as recorded. | A24.1 | Fixed: the projector reads the exponent from the catalog, checks the MF cost, and admits a SMOKE counter only right after the attempt that placed it. |
| 6. A CX squad's SMOKE dr takes +1. | A4.51, p. 51 | Fixed. |
| 7. A leader directing a mortar in the MPh could direct again. | A7.531 | Fixed: a leader recorded as directing a SW has directed. |
| 8. A unit spotted for two mortars, and a HS that had fired could spot. | C9.31 EX, A7.352 | Fixed. |
| 9. A kept Spotter never expires; the HS of a Reduced Spotter. | C9.3 | Backlog section 19. |
| 10. The verifier takes some SW map reads as recorded. | B23.423, C9.3, C13.8 | A deviation in backlog section 1, as for other map reads; a moved mortar is recomputed. |
| 11. The PF usage was not shown. | C13.31 | Fixed: the Ordnance panel shows shots taken of allowed. |
| 12. The Ammunition and Intensive Fire controls showed for SW; the mortar label. | C13.34, C5.6 | Fixed: hidden for SW, a PF shows HEAT, the label names the possessor and Spotter. |
| 13. SMOKE in a Residual FP Location. | A24.1, A8.2 | Backlog section 19. |
| 14. The SMOKE panel lists every checked squad; a free-text Location; a misplaced comment. | A24.1 | The panel is in backlog section 19; the comment is fixed. |

It confirmed the Area To Hit per unit, spotting, the refusals, a squad firing its inherent FP after its mortar, the SMOKE costs, Hindrance, and removal, the PF Check, To Hit, and Casualty Reduction, the usage arithmetic, and a failed PF Check's record.

## Visual check

In the Studio (`map-studio-scripted`), a new game on board 01, July 1944, with a German squad and its 5cm mortar in B10, a second squad in B9, a Russian squad in the woods of B6, and a T-34 in B7. The PFPh Ordnance panel listed the mortar and each squad's Panzerfaust. The mortar's shot with scripted dice 2 and 3 read "Basic TH# 7 (red) = 7; ... + 1 (case-r, C6.9) = Final DR 6: hit; each unit: r1 Final DR 6 hit; hit on r1: 2 FP, IFT DR 5, 6 = 11 - 1 (air-burst, B13.3)", the +1 being the T-34's Hindrance on the LOS. The PF from B9 read "PF Check dr 2 = 2: a shot; ... Basic TH# 10 (black) - 4 (pf-range, C13.33) = 6; ... Basic TK# 31 - AF 6 = Final TK# 25; TK DR 3, 4 = 7: burns". A first attempt without ELRs was refused for the undeclared ELR, as it should be. The pane did not draw, so the check read the page's text and markup.
