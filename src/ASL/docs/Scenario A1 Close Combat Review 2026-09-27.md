# Scenario A1 Close Combat, Berserk, and Surrender: source and case review

**Status:** Reviewed. The package `scenario-a1-close-combat` is published, and `scenario-a1-fire` and `scenario-a1-rally` are revised and republished, all with no execution authority. Unit steps 29 (Close Combat and Advance) and 30 (Berserk, Surrender, and capture), pass 2 of the deviations.

**Date:** 2026-09-27

**Plan:** [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), sections 7, 8, and 11. The user approved the plan, its rulings subject to the second-pass reviewer, and autonomous passes on 2026-09-27. Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, for the sources, the CCT transcription, and the package rulings;
- a table player, for the live stage.

**Design:** [ASL Unit Deviations Pass 2 Design](<ASL Unit Deviations Pass 2 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

- Advance in the APh (A3.7, A4.7), into Known enemy units.
- Close Combat in the CCPh between Infantry of catalog 1.3.0 in one Location: Ambush, SMC grouping, leaders and heroes, Casualty Reduction, SW loss, Field Promotion, Melee, and Withdrawal from Melee.
- The Berserk and Surrender results of Heat of Battle, in the Fire and Rally packages; berserk units under fire; the berserk leader's companions; the charge; capture with a Guard.

## Sources

Two comparisons, made as the step 17 Fire comparison was (whole fragments against `pdftotext 4.00 -raw` page text, normalized):

| Comparison | Subjects | Pages |
|---|---|---|
| `asl-scenario-a1.close-combat-pdf-comparison.json` (`2cbb74c2...`) | 28: A1.6, A3.7, A3.8, A4.7, A4.72, A5.1, A5.5, A7.831, A11.1 to A11.41 (with A11.2 and A11.21), A18.12, A19.36 | 45, 47, 52, 53, 58, 72 to 74, 85, 86 |
| `asl-scenario-a1.berserk-surrender-pdf-comparison.json` (`4a247af9...`) | 18: A15.41 to A15.46, A20.2 to A20.55 (with both parts of A20.54) | 83, 84, 86 to 88 |

A11.41 (pp. 73 and 74) and A20.21 (pp. 86 and 87) run across a page break and are compared in two parts. The Close Combat matrix has 36 fragments (with A7.302, A10.7, A15.24, A15.43, A15.46, A17.11, A18.2, and A20.55 from earlier comparisons), the Fire matrix 140, and the Rally matrix 45.

The chart is the A11.11 Close Combat Table with the A11.4 Ambush box (p. 692, also printed on p. 106), transcribed in `Supplements/a11-close-combat-table.transcription.json` (`c2af9d03...`): 14 odds columns, black Kill Numbers 0 to 13 and red 2 to 15, the Infantry modifiers used, and the Ambush drm. The Heat of Battle table and its notes (p. 83) were rendered to read the Berserk and Surrender notes.

## Rulings

The plan's section 11 records R29.1 to R29.19 and R30.1 to R30.8 as built. The referee checked each against the rule text.

## Referee findings

The referee re-extracted every page and rendered the CCT and the Heat of Battle table itself. It confirmed 25 subjects on their pages, the CCT transcription cell by cell, and most rulings: the odds rounding (all three A11.11 examples and the 3-4 of p. 74), the per-defender DRM (A4.8 EX, p. 52; A11.2), a pinned leader not directing (A7.831, p. 58), the Ambush conditions, drm, threshold, and effects, A11.13's colored die, the Field Promotion drm on the pre-leader odds, the Advance scope, Fix A (A3.3), the Heat of Battle notes (a Fanatic's 12 is Berserk; A15.44 turns Berserk into Battle Hardening), the berserk morale, the Berserk TC, the charge rules, Guard capacity, and prisoners moving with their Guard.

It found thirteen defects:

| Finding | Rule | Resolution |
|---|---|---|
| D1. A berserk unit's Original 12 was one Casualty Reduction. | A10.31, p. 66: eliminated if not subject to breaking; a berserk leader wounded with +1 | Fixed (R30.3). |
| D2. A created leader's DRM was added to the director's. | A10.7, p. 68: leadership modifiers are not cumulative | Fixed: the created leader's mandatory DRM takes the director's place (R29.12). |
| D3. A broken unit in Melee was eliminated because Withdrawal was not built. | A11.16, p. 72: only one unable to withdraw | Withdrawal from Melee is built (A11.2, A11.21); a broken unit that can withdraw must attempt it before the CCPh ends (R29.11). |
| D4. An unstacked SMC could combine with a MMC. | A11.14, p. 72 | Fixed. |
| D5. A unit reinforcing a Melee need not attack. | A11.15, p. 72: "must engage in CC" | Fixed (R29.17). |
| D6. TI units were said to refuse CC but were not read. | A4.8, p. 52 | Fixed: CC with a TI unit is refused (R29.14). |
| D7. Captors and a Known enemy in LOS were read before the attack. | A15.44, A15.5, p. 84 | A concealed firer that firing reveals (A12.14) counts as Known; a captor the attack breaks, pins, eliminates, or makes berserk is no captor. |
| D8. A surrender to Guards with no capacity was downgraded to Disrupted. | A20.21, A20.5, p. 87 | Such captors are left unread and the attack refused before any roll (Unarmed units are not built). |
| D9. A berserk unit charged with its SW. | A15.431, p. 84 | It abandons each SW of more than one PP before its charge; 1PP SW beyond its IPC refuse the charge. |
| D10. A berserk unit with prisoners would not massacre them. | A20.4, p. 87 | A Heat of Battle subject sharing a Location with prisoners is refused before any roll (FPF firers, a leader's rally). |
| D11. A charge could end among prisoners of its own side. | A20.4; A20.55 | Refused. |
| D12. Italians and Japanese were admitted without their exceptions. | p. 83 notes | Refused (nationality not reviewed). |
| D13. A20.54's second part was not compared; the backlog lacked the step 29 and 30 exclusions. | p. 88 | Both parts compared; the backlog, sections 1 and 11, lists every exclusion. |

It disputed eleven readings:

| Reading | Resolution |
|---|---|
| 1. Odds above 10 but below 11 to 1. | Undecided before any roll (R29.16). |
| 2. A berserk unit counts Berserk and Lax in the Ambush drm. | Kept as ruled (R29.9: each cause once, and both causes apply); flagged for the user. |
| 3. A created leader in an attack with a berserk unit. | His DRM does not apply (A11.141, A15.42), though he adds his FP (R29.12). |
| 4. No ELR Replacement for a berserk unit. | Kept as a reading (R30.3): A15.42, it never breaks. |
| 5. A Fanatic berserk unit has Morale Level 11. | Kept as a reading (R30.3; A10.8). |
| 6. An undecided charge ends in place. | Kept as a recorded deviation (R30.5; backlog); its berserk status is not cleared by that stop. |
| 7. The charge target when the first target leaves LOS. | Keeps its target until a closer Known enemy unit comes into LOS (R30.5). |
| 8. Berserk ends when another group did part of the killing. | Only when its own group's attack made every elimination (R29.15). |
| 9. A heroic leader and Stealth. | Stealthy, like a hero (A15.24, A15.21). |
| 10. Guards do not fire. | A deviation, in the backlog. |
| 11. Surrender is always accepted. | A deviation, in the backlog. |

## Live stage

The table player, a separate agent briefed as an experienced ASL player, read the live code, the Play page, and the tests against pp. 47, 52, 72 to 76, 83 and 84, and 86 to 88, and ran the Close Combat tests (all passing). It confirmed the Advance, Fix A, the CC arithmetic (SMC FP, the per-defender -2, Partial Kills, Casualty Reduction, SW loss, one attack per unit, leadership), the Ambush conditions and drm, Withdrawal DRM (the K4 example), Melee and the end-of-CCPh eliminations (the J2 example), the berserk charge and its end, the Heat of Battle results, and surrender and capture.

It found four ways a game could freeze and eight other differences from table play:

| Finding | Rule | Resolution |
|---|---|---|
| 1. The Play page had no withdrawal control, so a broken unit that must withdraw blocked the CCPh. | A11.16, A11.2, p. 73 | Fixed: a withdrawal selector per unit in Melee, with its destinations, marking the broken ones that must try. |
| 2. A charge whose next step the model refuses (prisoners, concealed units, a lone SMC) froze the MPh. | A15.43, A15.432, p. 84 | Fixed: such a step leaves the charge undecided, so it ends in place (R30.5). |
| 3. A Guard could advance its prisoners into CC nobody could resolve. | A20.53, A20.55 | Fixed: refused (R29.19). |
| 4. A broken Guard in Melee was eliminated and its prisoners broke replay. | A11.16, p. 72: "non-guard" | Fixed in the planner and the projector (R29.11). |
| 5, 11. Withdrawal excluded Locations needing CX, across hexside terrain, at another level, or holding a friendly Guard. | A11.21, p. 73 | Fixed: those are destinations; no CX counter is placed (a deviation, R29.11, in the backlog). |
| 6. The ambusher's attacks were predesignated. | A11.3, A11.32, p. 73 | Fixed: sequential calls until the ambushed side's round (R29.18). |
| 7. Mandatory attacks could be skipped by declaring no CC. | A15.43, A11.15 | Fixed: the CCPh does not end without the Location's round (R29.17). |
| 8. The phase refusal after an Ambush with no ambusher named an ambushed side's round. | A11.12 | Fixed. |
| 9. Combined attacks by MMC of different Morale Level, or partly attacked, are refused for Field Promotion. | A18.12 | Kept (R29.12); in the backlog as the most frequent refusal. |
| 10. Capture attempts are not offered. | A20.22, p. 86 | In the backlog; the CC panel says so. |
| 12. Berserk units do not fire. | A15.432 | In the backlog; mandatory CC (item 7) keeps the outcome. |

Its page findings, and what was done:

- Fixed: each defender's own DRM now follow the attack's modified DR and lead to the Final DR ("r1: 11 - 2 (vs-broken) = Final DR 9").
- Fixed: the colored die is marked, and pinned FP modifiers and withdrawals are printed.
- Fixed: the Director defaults to the best eligible leader, and each SMC defaults to the first MMC of its side.
- Fixed: attackers and defenders are listed per side, ATTACKER first, and a broken unit may not be chosen to attack.
- Fixed: the advance destination is a list of ADJACENT Locations, and concealed and TI units are not offered.
- Fixed: "roll for Ambush" is enabled only where an Ambush is due, and a list names what CC is still due before the phase can end.
- In the backlog: counters by printed values, per-side declarations and the captor's choice (the page is hot seat), and SW and TC lines in the CC record.

## Digests

| Artifact | SHA-256 |
|---|---|
| Close Combat case matrix | `3bfe30e9...` |
| Close Combat package manifest | `f729b9a8...` |
| CCT transcription | `c2af9d03...` |
| Fire case matrix | `b75aceaf...` |
| Fire package manifest | `8ac91d10...` (prior `ab868f4c...`) |
| Rally case matrix | `8f0c1d5b...` |
| Rally package manifest | `aa60fdde...` (prior `5a1fc655...`) |
| Close Combat PDF comparison | `2cbb74c2...` |
| Berserk and Surrender PDF comparison | `4a247af9...` |

## Cases

The Close Combat matrix has fourteen cases: two resolved (a round, and the Ambush drs), nine abstained (phase, units, concealment, prisoners, overstacking, attack, round, stacking, director, and the units that must attack), and two indeterminate (Field Promotion or odds undecided, and a missing roll). The Fire and Rally matrices keep their cases; their Heat of Battle rulings, resolution, and exclusions are revised.

The reachability walks cover every roll of the Close Combat package over five rounds (the A11.141 example, a broken unit with a hero, an ambusher's round, a berserk unit with a SW, and a Field Promotion), the Fire package over four new scenarios (a berserk leader with companions, a berserk target, captors, and no Known enemy in LOS), and the Rally package over three (a berserk leader with companions, captors, no Known enemy in LOS).
