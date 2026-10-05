# Scenario A1 Heat of Battle: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire` and `scenario-a1-rally` are revised and republished with no execution authority. Unit steps 27 (Leader Creation) and 28 (Heat of Battle, heroes, and Battle Hardening), pass 1 of the deviations.

**Date:** 2026-09-27

**Plan:** [ASL Unit Deviations, Ordnance, and Vehicles Plan](<../Plans/ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), sections 5, 6, and 11. The user approved the plan, its rulings subject to the second-pass reviewer, and autonomous passes on 2026-09-27. Under the plan, two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, for the counters and the package rulings;
- a table player, for the live stage.

**Design:** [ASL Unit Deviations Pass 1 Design](<../Passes/ASL Unit Deviations Pass 1 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). Since 2026-09-27 the whole rulebook is in scope.

## Scope

- Heat of Battle after an Original MC DR of 2 in a fire attack (MC, LLMC, or an FPF firer's NMC), and after an Original 2 on a leader's rally.
- Leader Creation after an Original 2 on the side's first MMC Self-Rally of its own RPh.
- For German and Russian units of catalog 1.3.0.

## Sources

Twelve new fragments are compared with the text of their physical pages, as the step 17 Fire comparison was (`asl-scenario-a1.heat-of-battle-pdf-comparison.json`, SHA-256 `41cfbe21...`):

| Rule | Pages |
|---|---|
| A15.2, A15.21 (two fragments), A15.23, A15.24, A15.3, A15.4 | p. 83 |
| A15.5 | p. 84 |
| A18.2 | p. 85, with its continuation on p. 86 |
| A10.8 | p. 69 |
| A25.25 | p. 96 |

A second pass with PyMuPDF confirmed each. The Fire matrix now has 122 fragments and the Rally matrix 27. The charts used are:

- the Heat of Battle table (p. 83);
- the A18.2 Leader Creation Table (p. 694);
- the National Capabilities Chart (p. 695);
- the Chapter H leader table (p. 331);
- the Counter Examples (p. 676).

## Counters

Catalog 1.3.0 adds 25 counters (299 rows); the [Scenario A1 Catalog Design](<../Designs/ASL Scenario A1 Catalog Design.md>), section 9.4, lists them. The referee rendered each chart region itself and confirmed every value. It found one defect: a Russian Conscript Battle Hardens into NKVD 2nd Line (A25.25), not 1st Line. The NKVD squad and HS were added and confirmed in turn.

## Rulings

These are the plan's rulings as built. The referee checked each against the rule text.

| Ruling | Content |
|---|---|
| R28.1 | Berserk (9 to 11) and Surrender (12) are recorded as not taken until step 30. |
| R28.2 | The exemptions of A15.1 that name built units apply: heroes and heroic leaders take no Heat of Battle. |
| R27.2 | The Leader Creation dr is always rolled. |
| R28.3 | Heroes: 1-4-9 (wounded 1-3-8), the -1 heroic DRM at Normal Range, no Cowering alone, wounded and then eliminated by failed MCs, SW use not reviewed. |
| R28.4 | A heroic leader keeps his counter and leadership, rallies, and has a Morale Level of at least 9. |
| R28.5 | Battle Hardening is always taken. |
| R28.6 | The next higher quality is the same size, with no printed number lower and the least gain. |
| R28.7 | Added after the referee's review; see below. |
| R28.8 | Added after the table player's review: one Heat of Battle DR per unit per attack. |

## Referee findings on the packages

The referee confirmed:

- the table's bands and DRM;
- what triggers Heat of Battle, and what does not (NTC, PTC, LLTC, Self-Rally);
- the exemptions;
- the hero's behavior, Cowering, the heroic DRM, and the heroic leader's Morale Level (it matches the 1-4-10 and 1-3-9 examples);
- the leader chains and the Battle Hardening maps (each passes the no-decrease, least-gain test against the catalog);
- Fanaticism;
- the Leader Creation drm and bands;
- the pre-check's closure over Reduction, Replacement, and Battle Hardening.

It found five defects, each fixed with a test:

| Finding | Rule | Fix |
|---|---|---|
| D1. A hero or heroic leader was pinned by a failed PTC or LLTC. | A15.2, p. 83: not subject to enforced Pin results | He takes no PTC or LLTC. |
| D2. A KIA that did not eliminate a hero broke him. | A7.301, p. 55: units that cannot break suffer Casualty Reduction instead | He is Casualty Reduced (wounded). |
| D3. A hero's Casualty MC lacked the +1 to the Wound Severity dr. | A10.31 EXC, p. 66 | +1, as if already wounded. |
| D4. A Disrupted unit stayed Disrupted after Battle Hardening. | A15.3: exchanged for an unbroken, unpinned unit | Battle Hardening clears Disrupted; so does a rally by a leader (A19.12, p. 86: "Unless rallied by a leader first"). |
| D5. A leader made heroic after a failed Rally DR stayed broken. | A15.21: he rallies | The rally records `rallied-heat-of-battle` and the leader is unbroken. |

It disputed five readings:

| Reading | Resolution |
|---|---|
| P1. A15.44 turns Berserk into Battle Hardening when no Known enemy unit is in LOS. | Left with Berserk for step 30, which will need that fact; the backlog's section 1 row says so. |
| P2. The +1 for a broken unit when the Original 2 MC itself breaks it. | Adopted (ruling R28.7): the unit is broken when the Heat of Battle DR is made. |
| P3. A broken elite MMC or 10-3 that Battle Hardens became Fanatic but stayed broken. | Adopted (ruling R28.7): A15.3 exchanges the unit "(even if broken)" for an unbroken, unpinned one, and "also becomes Fanatic" adds to that exchange. |
| P4. A Russian Conscript hardening into NKVD gains the NKVD traits. | Kept: the A25.25 text gives that class. |
| P5. Leader Creation uses the broken Morale Level. | Kept, as accepted in the plan (A18.2: "at the time of the Original 2"; A10.4). |

Other notes:

- A Fanatic heroic 10-x leader's Morale Level could reach 11. It is now capped at 10, or 9 if wounded (A15.2).
- An underscored NKVD FPF firer could be Replaced as a single squad. FPF by an underscored MMC is now refused, like an attack on one (A19.13 EXC stays in the backlog).
- Two latent items with no built counters are in the backlog, section 10: nationalities other than German and Russian in the drm, and the Green leader exemption of A19.3.

## Live stage

A second reviewer, briefed as an ASL table player, read the live records, the planners, the projector, and the Play page against the rules and the acceptance tests.

It confirmed:

- the Heat of Battle DRM and who takes it;
- a created hero's Location, side, state, and fire markers;
- a heroic leader and a Battle Hardened unit coming back unbroken and unpinned, with DM cleared;
- when Leader Creation applies, its dr, and the created leader;
- stack splitting: moves limited to the members still moving, other stacks refused, partial ends, members leaving when they break or pin, and a Reduced member followed by its HS;
- replay of the new roll purposes.

It found five defects, each fixed:

| Finding | Rule | Fix |
|---|---|---|
| L1. A leader both made heroic and Battle Hardened on a rally lost his heroic status. | A15.1, A15.21, p. 83 | The Replacement carries `asl:heroic`. |
| L2. A unit with an Original 2 on its MC and again on its LLMC reused one Heat of Battle DR for both. | A15.1 | One DR per unit per attack; the second is recorded as not taken (ruling R28.8, backlog). |
| L3. A unit created or Replaced in the MPh had fresh MF: a hero could move as a new stack, and a Battle Hardened mover gained its MF back. | A15.21, p. 83; A4.2, p. 49 | Lineage carries the MF spent and the move's end to the produced unit; a unit created in its side's MPh moves no further that phase (backlog). |
| L4. A unit Battle Hardened by fire lost "?" with no reason shown. | A12.14, p. 77 | It keeps "?" unless the attack cost it, as on a rally. |
| L5. Battle Hardening dropped the unit's SW. | A15.3, p. 83: unit substitution | A Replacement, whether by Battle Hardening or A19.13, keeps the SW. |

Its other points:

| Point | Resolution |
|---|---|
| A15.44 is not applied. | Left with Berserk for step 30 (backlog, section 1). |
| The live game never supplies the Inexperienced fact, so a Green unit would be refused. | Latent, since the catalog has no Green unit (backlog, section 10). |
| A Fanatic unit's 12 was shown as Surrender. | Fixed: the table's note treats it as Berserk. |
| A Fanatic heroic 10-x had a Morale Level of 11. | Fixed with the referee's finding. |
| The page's Heat of Battle and Leader Creation lines lacked "Final DR" and the rules of their modifiers, and repeated the hardened unit. | Fixed. |
| Created units show definition ids. | Backlog, section 10. |
| A hero from a concealed MMC is created unconcealed. | Backlog, section 10. |
| A created id could collide if a unit's own id ended in "-hero". | Not fixed: the ids the planners give never do. |

## Digests

| Artifact | SHA-256 |
|---|---|
| Fire case matrix | `50cabc94...` |
| Fire package manifest | `ab868f4c...` (prior `e0c28e88...`) |
| Rally case matrix | `8ef21925...` |
| Rally package manifest | `5a1fc655...` (prior `612414fa...`) |
| Heat of Battle PDF comparison | `41cfbe21...` |
| Counter transcription | `ba26868d...` |

## Cases

Two new cases join the matrices:

- `A1-fire-heat-of-battle-undecided` (indeterminate): a Heat of Battle DR needs a fact or counter the attack does not supply, such as a Green unit's inexperience.
- `A1-rally-field-promotion-unreviewed` (indeterminate): an NKVD MMC's Field Promotion would create a Commissar.

The reachability walks cover the new rolls, and two new scenarios: heroes with Fanatic units, and Conscripts with elite units.
