# Scenario A1 Backlog Pass 7: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire`, `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are revised for catalog 1.6.0 and republished with their prior manifest digests kept. None has execution authority. Backlog pass 7 (armor).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 7), and 5 (rulings R7.1 to R7.12). The user approved the plan and asked for the passes to run without pausing. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 7 Design](<ASL Unit Backlog Pass 7 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

The German PzKpfw IIIH and the Russian T-34 M41 (Chapter H, pp. 337, 355; Listings Key, p. 338), the Vehicle Target Type (C3.31, C.8, C.9, C4.3, C5, C6), hit location and Target Facing (C3.6, C3.9, D3.2, D1.6 to D1.64), To Kill (C7.1 to C7.35 and the tables of p. 701), To Kill results, Shock, and the Unconfirmed Kill (C7.4 to C7.7), Special Ammunition (C8.1, C8.3, C8.9, C8.91), a tank's MA fire (D1.3 to D1.32, D3.12, C5.1, C5.11), closed-topped crews (D5.2), and crew checks and survival (D5.1, D5.34, D5.5 to D5.7).

## Sources

`asl-scenario-a1.pass7-pdf-comparison.json` (`c778b4f7...`) compares 50 subjects: C.8, C3.31, C3.9, C5.9, C6.1, C6.7, C7.1, C7.11, C7.2, C7.21, C7.23, C7.24, C7.31, C7.311, C7.32, C7.33, C7.331, C7.34, C7.342, C7.35, C7.4, C7.41, C7.42, C7.5, C7.6, C7.7, C8.1, C8.3, C8.9, C8.91, D1.3, D1.31, D1.32, D1.321, D1.6, D1.61, D1.62, D1.63, D1.64, D1.7, D1.73, D1.74, D3.12, D3.2, D5.2, D5.5, D5.6, and D5.7, two of them with a continuation. `AslScenarioA1FireSourceReview.BuildPass7` verifies each against the PDF. The comparison first failed on fourteen fragments: the tool removed everything between a `<` and a `>` on the page and in escaped Markdown, and the rulebook uses both as dice signs. The tool and the review now strip only Markdown tags from the fragment and never touch the page; the earlier batches keep the method they were recorded with.

The charts are transcribed in `c3-to-hit-vehicle-row.transcription.json` (p. 700) and `c7-to-kill-tables.transcription.json` (p. 701), with the colored nationality entries read from the page's span colors and confirmed on a rendering.

| Artifact | Digest | Prior manifest kept |
|---|---|---|
| Catalog 1.6.0 | `5cec641b...` | |
| Fire case matrix (197 fragments, 27 cases) | `c34229b7...` | |
| Fire package manifest | `e00107a7...` | `a3037939...` |
| Rally package manifest | `fdae11e5...` | `f8e87429...` |
| Close Combat package manifest | `59e5b645...` | `d976896c...` |
| Ordnance case matrix (103 fragments, 19 cases) | `b680f858...` | |
| Ordnance package manifest | `d10ce346...` | `68fdc545...` |

## Rulings

R7.1 to R7.12 are in the plan, section 5, as revised after both reviews.

## Referee findings

The referee re-read every cited rule, the charts, and the listing rows, and checked the rulings, the package code, and the test numbers.

| Finding | Rule | Resolution |
|---|---|---|
| 1. An improbable hit took its location from the dice; the subsequent dr decides it (2 turret, 3 hull). | C3.6, p. 170 | Fixed, with a test; R7.4 says so. |
| 2. The "+1" counter after a Stun was not applied to the MA's To Hit DR or the target crew's NTC, TC, and Crew Survival. | D5.34, p. 203 | Fixed on both sides, with a test; R7.9 and R7.10 say so. |
| 3. An AFV that entered a new hex and fires in the AFPh needs Case C. | C5.3, p. 172 | Refused as outside, with a test; Case C is in backlog section 17. |
| 4. A Motion firer needs Case C4 in any phase, not only the PFPh. | D2.42, p. 198; C5.35 | Refused in every phase, with a test; C4 is in backlog section 17. |
| 5. A Recalled AFV may fire with +1 after its Stun period. | D5.341, p. 203 | Recorded simplification: a Recalled AFV does not fire (R7.10; backlog section 17). |
| 6. Elite forces add one to Depletion Numbers. | C8.2, p. 179 | Not built (R7.6; backlog section 17). |
| 7. An Original DR above the Depletion Number also means the ammunition is gone for good. | C8.9, p. 179 | Fixed: `none` depletes it too, with a test; R7.6 says so. |
| 8. The Immobilization TC applies to unarmored vehicles too, on 1st Line morale; D5.5 has a second trigger. | D5.5, D5.1, p. 203 | The TC is taken by every vehicle's crew, with a test; the second trigger is in backlog section 17. |
| 9. A red CS# gives -1 for burning only. | C7.7 note A, D5.7 | Not built: no catalog vehicle has one (R7.7; backlog section 17). |
| 10. The APCR To Hit modifier cited C4.5. | C4.3, p. 171 | Cites C4.3. |
| 11. A missing crew morale skipped the checks silently. | | Indeterminate with `definition-incomplete:crew-morale`. |
| 12. An NT AFV's upper superstructure hit. | C3.9, p. 171 | Backlog section 17; no catalog AFV is NT. |
| 13. Case D NA refuses the whole shot. | C7.24, p. 176 | Kept; R7.5 says so. |

It confirmed the To Hit and To Kill charts, the colored entries, the Destruction Table, Case A to Case R as built, the turret AF steps, the Depletion Number notation, the Shock and UK drs, the crew morale of 8, the catalog rows, and every expected number in the package tests.

## Table player findings

The table player read the planner, the projector, the gate, and the Play page as a player would. Each defect is fixed with a test.

| Finding | Rule | Resolution |
|---|---|---|
| 1. An Unconfirmed Kill, and a Shocked AFV outside the ordnance path, could move, expose its crew, fire its AAMG, and repair. | C7.42, p. 177 | Fixed: Shocked and UK stop a vehicle wherever a Stun does, with a test; R7.8 says so. |
| 2. An Abandoned tank could fire its MA. | D5.41 | Fixed, with a test; R7.10 says so. |
| 3. A concealed tank kept its "?" after firing. | A12.14 | Fixed, with a test. |
| 4. A tank's Acquisition vanished right after its shot. | C6.5 | Fixed: a tank keeps it while active and not Abandoned, with a test. |
| 5. A Gun's Acquisition of a vehicle stays on the hex rather than following the vehicle. | C6.51 | Backlog section 17. |
| 6. A malfunction on a shot that found no Special Ammunition was lost. | C8.9, p. 179 | Fixed: the MA malfunctions and the shot counts as fired, with a test. |
| 7. The Immobilization TC was taken by Shocked, Stunned, and absent crews, and an Abandoned wreck could produce a surviving crew. | D5.5, D5.6 | Fixed, with a package test; R7.9 says so. |
| 8. The verifier takes the recorded Target Facing, and the events after a kill or a Shock dr are not checked against it. | D3.2 | Backlog section 1, a deviation: the facing is a map read, as range and LOS are. |
| 9. The ammunition list offered what the Gun lacks, and Shock drs did not appear in the records. | C8.1 | Fixed: the list follows the Gun's listing, and the RPh records show each Shock dr. |
| 10. A turret does not turn for a shot that found no Special Ammunition. | C8.9 | Kept: the shot was never fired. |
| 11. Rulings a player would dispute: no fire after moving, no BMG or CMG, no TCA set at the end of a phase, HE refused at a Location with any enemy vehicle, no Hazardous Movement for bailed-out crews. | R7.10, R7.12, R25.10, D5.5 | Backlog section 17. |

It confirmed the Target Facing geometry and hexspine ties, turret traverse and the kept TCA, fire markers and ROF, depletion, Shock, immobilization, burning wrecks, the loss of "?" by a hit, the RPh block for both sides, Cases I and J, target validation, HE at Infantry, and the scenario year.

## Visual check

In the Studio (`map-studio-scripted`), a new game on board 01, July 1942, with the German PzKpfw IIIH in B10 facing north-east and the Russian T-34 in B8 facing east. The Ordnance panel offered the tank as "attacker-tank MA, VCA north-east, TCA north-east, BU". Its AP shot with scripted dice 4, 2, 1, 1 read: Modified TH# 10, DRM Case I +1 and Case L -1, a hull hit on the side Target Facing, Basic TK# 11 + 1 (Case D) - AF 6 = Final TK# 6, TK DR 2: burns. The Units table showed the T-34 wrecked and burning with its Blaze, and the map named "Russian wrecked T-34 M41 vehicle" and the Blaze. The pane did not draw, so the check read the page's text and the map's accessible names.
