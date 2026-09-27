# Scenario A1 Vehicles: source and case review

**Status:** Reviewed. The package `scenario-a1-fire` is revised for vehicles and republished with its prior manifest digest kept; `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are republished for the new catalog digest with theirs kept. None has execution authority. Unit step 25 (vehicles), pass 4 of the deviations.

**Date:** 2026-09-27

**Plan:** [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), sections 10 and 11. The user approved the plan, its rulings subject to the second-pass reviewer, and autonomous passes on 2026-09-27. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, for the catalog rows, the sources, the chart transcriptions, and the package rulings;
- a table player, for the live stage.

**Design:** [ASL Unit Deviations Pass 4 Design](<ASL Unit Deviations Pass 4 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

- Vehicles in the target Location of an Infantry attack: an unarmored truck on the IFT Vehicle line (A7.308, A7.309), and an open-topped AFV unharmed by small arms whose CE crew takes a General Collateral Attack (A7.307, D.8B, D5.31).
- The crew's results: Stun, Stun +1, and Recall (D5.34, D5.341, D5.342), and the pin of a failed PTC (A7.82).
- A vehicle's MA AAMG firing on the IFT (D1.83, D3.5, D3.53, D2.42, D3.7).
- Vehicle movement by MP expenditure, CE and BU, and the refusals that keep the rest out (rulings R25.3 to R25.10).

## Counters

Catalog 1.5.0 adds three vehicles (129 rows): the German Opel 6700 (Blitz) truck and SPW 251/1 halftrack (sheet VLG, p. 339) and the Russian GAZ-MM truck (sheet VLR, p. 356), read with the Listings Key (p. 338). The referee rendered the listing rows and the span colors and fonts and confirmed the 126 value rows: the ★ AF (unarmored), the halftrack's single AF 1 as front and side, OT, MA AAMG, ROF 1, the blank B# as B# 12, AAMG 3 FP, 15 PP/T7, CS 5 (roman); the trucks' 28 and 25 MP printed black, the italic cs 6, T7 and T8, 21 PP. It found that a passenger-only cs# could not be told from a crew's (D8), so the trait `asl:cs-passengers-only` was added (three rows). Vocabulary 1.8.0 adds the states `asl:immobilized`, `asl:stunned`, `asl:stun-recovery`, and `asl:recalled`.

## Sources

`asl-scenario-a1.vehicle-pdf-comparison.json` (`c9f59e1c...`) compares 45 subjects, made as the earlier comparisons were (whole fragments against `pdftotext 4.00 -raw` page text, normalized): A4.6, A7.307 to A7.309, A7.82, A7.9, A8.2, A10.31, A15.1, and Chapter D's D.3 to D.8, D1.21 to D1.83, D2.1 to D2.6, D3.11 to D3.7, D5.1 to D5.341, D9.3, and D9.4 (pp. 55 to 210). A8.2 and A10.31 were added after the referee's review. Each occurs whole on its page or in two parts across a column or page break; the referee checked every stored page digest against its own extraction. The Fire matrix now has 186 fragments.

Two chart rows are transcribed:

- `Supplements/a7-ift-vehicle-line.transcription.json` (`c9a2d785...`): the IFT's Vehicle line (p. 692), Kill Numbers 3 to 13 for the 1 to 36 FP columns;
- `Supplements/b-terrain-chart-vehicle-mp.transcription.json` (`20ae5a74...`): the Terrain Chart's MP entrance costs (p. 698) for Open Ground (halftrack 1, truck 4), Grain (halftrack 1, truck 5, "MF/MP Apr-Sept"), and the road rate (½, BU 1).

## Rulings

R25.1 to R25.10 are in the plan, section 11, as revised after both reviews.

## Referee findings

The referee re-extracted every cited page, rendered the listing rows and the Terrain Chart, and confirmed the catalog rows, the Vehicle line, the MP costs, the comparison, the Vehicle line thresholds (a Final DR at most half the Kill Number burns, below it eliminates, equal immobilizes, on the Cowering-shifted column), the crew results, the AAMG's modifications, and the movement and BU timing.

It found these defects:

| Finding | Rule | Resolution |
|---|---|---|
| D1. An Original 12 on the crew's MC Stunned it; it is a Casualty MC, which Recalls. | A10.31, p. 66; D5.341, pp. 203 to 204 | Fixed, with a test (R25.6). |
| D2. A Stunned OT AFV became CE again by itself when its Stun turned to Stun +1. | D5.34, p. 203; D5.33 | Fixed: a Stun or Recall buttons the crew up and Stops the vehicle; only the owner removes the BU counter (R25.6). |
| D3. A vehicle starting its Player Turn in Motion could Prep Fire. | D2.4, p. 198 | Fixed in the package and the planner, with a test (R25.7). |
| D4. The R25.9 refusals were not wired; MotionBar had no caller. | D9.3, D9.4 | Fixed: the package refuses Infantry sharing a Location with an AFV; the planner refuses fire past an AFV; ending a move checks D2.4. |
| D5. A vehicle in Motion was never made to spend an MP. | D2.4, p. 198 | Fixed: the MPh does not end while one has not (R25.3). |
| D6. Residual FP in a vehicle's own hex did not attack its further MP there. | A8.2, p. 60 | Fixed: no MP in such a Location (R25.3), revised after the table player's item 2. |
| D7. Grain cost the same in every month. | B15.6, p. 129 | Fixed for vehicles; the Infantry path's same gap is in the backlog. |
| D8. The trucks' italic cs# read as a crew's. | Listings Key, p. 338; D5.6 | Fixed: the trait `asl:cs-passengers-only`. |
| D9. The CE DRM was applied without checking a positive TEM. | D5.31, p. 203 | Fixed: an AFV in terrain with a positive TEM is refused (R25.6). |
| D10. R25.3 said "the hex" its VCA points at. | D2.11 EX, p. 195 | Fixed: either hex. |

And these disputed readings, each resolved as the referee recommended:

| Reading | Resolution |
|---|---|
| 1. The vehicle's DR under A7.308 takes the attack's own DRM but not TEM, FFMO, or FFNAM. | Kept (R25.4). |
| 2. The Collateral DRM is the attack's DRM with +2 CE. | Kept (R25.6). |
| 3. The Unlikely Kill dr is rolled whenever an Original 2 did nothing, and not offered after harm. | Kept as a recorded deviation (R25.5). |
| 4. No Multiple ROF in the AFPh. | Kept (R25.7). |
| 5. When a vehicle may end in Motion. | Stricter than D2.4, recorded (R25.3), and revised after the table player's item 5. |
| 6. The halftrack's single AF as front and side. | Accepted. |
| 7. Recall recorded as elimination. | Recorded as a deviation (R25.6, backlog). |

## Live stage

The table player, a separate agent briefed as an experienced ASL player, read the live code, the Play page, and the tests against the rulebook. It confirmed the MP costs, Start, Stop, and Motion, a vehicle leaving its move when immobilized, Stunned, or Recalled, the Stun and CE timing, the AAMG, the Vehicle line for Defensive First Fire at a moving vehicle, and the refusals around vehicles.

It found one way the game could get stuck, and these other differences:

| Finding | Rule | Resolution |
|---|---|---|
| 1. A berserk unit whose nearest Known enemy was a vehicle had no legal step, and the MPh could not end. | A15.43, p. 84 | Fixed: a charge step into an enemy vehicle's Location ends the charge in place (R25.3, R30.5), with a test. |
| 2. Residual FP barred a BU halftrack from Stopping, though it cannot attack it. | A8.222, p. 60 | Fixed: a BU or Stunned AFV may enter and spend MP there (R25.3); Residual FP against trucks and CE crews is in the backlog. |
| 3. The AFV cover refusals are broader than D9.3 and D9.4, and Infantry stacked with their AFV cannot be fired on. | D9.3, D9.4, D2.41 | A recorded deviation (R25.9); building the +1 TEM and the LOS-trace test is in the backlog. |
| 4. The AAMG and Infantry of its Location could both fire at one target in a phase. | D3.5, p. 201; A7.55 | Fixed: the Mandatory Fire Group binds them; only the vehicle's own Multiple ROF fires again (R25.7), with a test. |
| 5. The Motion test looked only at the two VCA hexes. | D2.4 | Revised: every ADJACENT hex it may enter, with 1 MP for each hexspine its VCA must turn (R25.3), with a test. |
| 6. Subsequent First Fire counted an unarmed truck as an armed enemy. | A8.3, p. 61 | Fixed. |
| 7. A refused entry disclosed a hidden or concealed enemy unit. | A12 | Fixed: the refusal says the entry is not decided here, with a test. |
| 8. A Recalled vehicle is recorded as eliminated; an immobilized one should be Abandoned. | D5.341 | Recorded (R25.6, backlog). |
| 9. Vehicles did not wait for a berserk unit's charge. | A15.43 | Fixed, with a test. |
| 10. The page offered always-refused actions. | | Fixed: no vehicle fire in the MPh, no BU or Stunned AAMG, no Start while moving, no BU toggle after Prep Fire, and each disabled entry names its reason. A toggle already used this phase is in the backlog. |
| 11. The page lacked MP left and whether a Start is due. | | Fixed; the Stun +1 label is in the backlog. |
| 12. MP left unspent at the end of the MPh. | D2.1, A8.14 | In the backlog. |
| 13. Residual FP's companions counted vehicles. | | Fixed. |

A visual check on the Studio (games `pass4-vehicles-2` and `pass4-vehicles-3`, scripted dice) found five more page gaps, fixed before the table player's report: the unarmed truck offered as a firer, the BU toggle after Prep Fire, Start offered while moving, the crew's DRM missing from the record, and the VCA missing from the placement list.

## Digests

| Artifact | SHA-256 |
|---|---|
| Catalog 1.5.0 | `a1c7fd2c...` |
| Vocabulary 1.8.0 | `b9ee7dc8...` |
| Vehicle PDF comparison | `c9f59e1c...` |
| Vehicle line transcription | `c9a2d785...` |
| Vehicle MP transcription | `20ae5a74...` |
| Fire case matrix | `596674d8...` |
| Fire package manifest | `e69b4bfb...` (prior `b944ed34...`) |
| Rally case matrix | `63931938...` |
| Rally package manifest | `9a1f5173...` (prior `7784925f...`) |
| Close Combat case matrix | `afde3459...` |
| Close Combat package manifest | `9f5d613b...` (prior `02e31479...`) |
| Ordnance case matrix | `6229eed1...` |
| Ordnance package manifest | `e02e8e4b...` (prior `1bf0bb77...`) |

## Cases

The Fire matrix gains four cases (18 in all): the Vehicle line resolved, a Collateral Attack resolved, a vehicle's AAMG resolved, and the vehicle cases outside the review (more than one vehicle, Residual FP or an ordnance hit on a vehicle, an AFV in terrain with a positive TEM, Infantry sharing a Location with an AFV, and a vehicle that may not fire). The ScenarioA1 tests `ScenarioA1VehicleFireTests` exercise each.
