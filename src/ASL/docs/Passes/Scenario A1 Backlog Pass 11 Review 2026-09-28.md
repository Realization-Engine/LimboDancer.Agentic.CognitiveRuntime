# Scenario A1 Backlog Pass 11: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire` (the OVR) and `scenario-a1-close-combat` (CC with vehicles) are revised and republished, each with its prior manifest digest kept. Neither has execution authority. Backlog pass 11 (Vehicle movement and OVR).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 11), and 5 (rulings R11.1 to R11.18). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 11 Design](<ASL Unit Backlog Pass 11 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

Vehicle movement: Reverse (D2.2 to D2.24), VBM (D2.3 to D2.38), ESB and Mechanical Reliability (D2.5, D2.51), Minimum Move and ALL entries (D2.15, D2.7, B13.41, B13.42), vehicle stacking and D2.6 (A5.2, D2.14, D2.6), vehicle terrain costs across pass 10's terrain (the Terrain Chart's vehicle columns, B9.4, B10.4, B10.51, B10.52, B23.41, B24.4), and Bog (D8.1 to D8.4). The OVR (D7.1 to D7.17), the A12.41 choice of concealed units a vehicle enters, Reaction Fire (D7.2 to D7.22), and Close Combat with vehicles (A11.31, A11.5 to A11.7).

## Sources

The rule text of each subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass11-pdf-comparison.json`, SHA-256 `8371dda5...d074797`, 59 fragments (40 Chapter D, 16 Chapter A, 3 Chapter B). D7.22 runs across a column break and matches in two parts. B10.52 is missing from the Markdown conversion and is cited from the PDF only. The source review registers the subjects (`BuildPass11`); `ThePass11SubjectsAreVerified` checks all 59.

The Fire package takes the 45 subjects outside A11 (catalog unchanged); the Close Combat package takes the 14 A11 subjects and A12.41.

## Rulings

R11.1 to R11.18 are in the plan, section 5. The referee's and the table player's findings below changed R11.1, R11.4, R11.6, R11.7, R11.9, R11.11, R11.13, R11.14 to R11.17.

## Referee findings

The referee read R11.1 to R11.18 against D2, D7, D8, A11.3 to A11.7, A12.41, B9.4, B10.5, B13.41, B13.42, B23.41, and B24.4, with the Terrain Chart rendered (p. 698).

| Finding | Rules | Disposition |
|---|---|---|
| 1. Every off-road change of two or more levels was refused as a Double-Crest; B10.51 charges 4 MP per intermediate level up and 2 down by road. | B10.51, B10.52, p. 125 | Fixed: only a real Double-Crest is refused off road; the road costs follow B10.51; R11.7 reworded, with a test. |
| 2. CC Reaction Fire was allowed in the window on the OVR's MP expenditure. | D7.1, D7.2, p. 207 | Fixed: refused until the OVR resolves, with a test. |
| 3. A vehicle destroyed by DEFENDER First Fire lost its OVR. | D7.11, p. 207 | Fixed: resolved at half FP from the wreck's last state; R11.11 reworded. |
| 4. An OVR with an ALL entry or a Minimum Move exceeded the allotment; the ALL entry was inferred from its cost. | D2.7, p. 199; D2.15, p. 196 | Fixed: such an OVR is refused, and the step records `all`, with a test. |
| 5. The Reverse Stop reserve refused ALL entries in Reverse and skipped VCA changes. | B13.41, p. 132; D2.7 | Fixed: ALL entries are exempt, turns keep the reserve; R11.1 reworded, with a test. |
| 6. A vehicle leaving a woods-road hex off the road needs a Bog Check. | B13.421, p. 132 | Recorded: backlog section 21. |
| 7. A halftrack crossing a hedge into woods took only the woods Bog DR. | B9.4, p. 121 | Fixed: both, the hedge's first, with two tests. |
| 8. A SMC defending alone has CCV 2, and the defender's CCV takes its A11.5 modifications. | A11.5, A11.622 example, pp. 73 to 74 | Fixed as the referee reads it; R11.15 reworded. |
| 9. The Immobile -1 applies to AFVs only. | A11.61, p. 74 | Fixed; R11.14 reworded. |
| 10. D2.6 read the entry hexside from the whole history and used the hull's facing for the turret. | D2.6, p. 199 | Fixed: the moving stack's `EnteredFrom` and the turret's TCA; R11.6 reworded. |
| 11. Capture of an unarmed vehicle is automatic in the CCPh. | A11.52, p. 74 | Fixed: at the start of the CCPh, with a test; R11.16 reworded. |
| 12. A pass may be per unit. | A11.31, p. 73 | Recorded: R11.16 reads a side's pass as covering its remaining units (backlog section 21). |
| 13. Mechanical Reliability applies on a Bog Removal's Start MP too. | D2.51, p. 198 | Fixed; R11.4 reworded. |
| 14. The combined PAATC uses the current Morale Level; a vehicle holds only Known Infantry in Melee. | A12.41, p. 83; A11.7, p. 74 | The second fixed; the first recorded as a reading (printed ML, Fanatic, and wounds). |
| 15. An OVR of a Location with only a CE AFV attacks its crew. | D7.12, p. 207 | Confirmed in the Fire package; R11.11 says a CE crew is attacked. |
| 16. A Minimum Move VCA guard could never be true. | D2.15 | Removed. |

It confirmed the Terrain Chart vehicle costs, Reverse multipliers and rear entries, VBM with its CAFP and D2.32 test, ESB, Mechanical Reliability on each Start, the Minimum Move, the Bog DRM and Bog Removal, the D2.6 kill test, the OVR FP and its halvings, FFMO, the wall TEM across the hexside entered, the malfunction on 12, the A12.41 PAATC, CC Reaction Fire, CC against a vehicle and by it, the sequential order, and the A11.6 advance PAATC.

## Table player findings

The table player walked the Play page and the planner through vehicle moves, OVRs, and CC with vehicles.

| Finding | Where | Disposition |
|---|---|---|
| 1. A vehicle entering an enemy AFV's hex on its last MP, by an ALL entry, in Reverse, or beside a hidden AFV could neither Stop nor end its move. | D2.6; `EnemyAfvBar` | Fixed: a vehicle that cannot move on (no entry it can pay for, no VCA change and 1 MP entry) may Stop or end its move; R11.1 reworded; test `AVehicleThatCannotMoveOnMayStopBesideAnEnemyAfv`. |
| 2. A VCA change at the CAFP with no affordable lane after it left the vehicle stuck. | D2.33 | Fixed by the same rule. |
| 3. An OVR whose attack stopped at an owner's option was never marked resolved, and could be rolled again. | D7.1; the fire resume path | Fixed: the resumed attack records the OVR's resolution; test `AnOvrStoppedAtAnOwnersOptionIsResolvedWhenTheAnswerResumesIt`. |
| 4. A Bog on the OVR entry ended the move and lost the OVR. | D7.1, D8.2 | Fixed: the move ends after the Reaction window; test `ATankBoggingAsItOverrunsStillResolvesTheOvr`. |
| 5. A declared OVR the Fire package refused froze the move. | D7.1 | Fixed: the OVR is checked when declared, with the entry or by itself; the Studio showed the refusal for a game with no ELR declared. |
| 6. A vehicle's Minimum Move pinned it and made it CX. | A4.134, D2.15 | Fixed; the Minimum Move test asserts it. |
| 7. Infantry could not attack Infantry in a Location holding a vehicle. | A11.31 | Recorded: backlog section 21; R11.16 and the action text no longer claim it. Such Infantry stay in Melee and may withdraw. |
| 8. A squad Reduced to a HS in CC with a vehicle could attack again as the HS. | A11.31 | Fixed: the HS counts as having attacked; test `ASquadReducedInCcWithAVehicleHasMadeItsAttackAndSoHasItsHalfSquad`. |
| 9. Units could fire at a moving vehicle in their own Location on every MP it spent there. | D7.22 | Fixed: only in the Reaction window after its OVR; test `InTheMphUnitsFireAtAVehicleInTheirOwnLocationOnlyAfterItsOvr`. |
| 10. A Bog on entry skipped the A12.41 choice. | A12.41 | Fixed; test `ABogOnEntryStillMakesTheConcealedUnitsChoose`. |
| 11. The OVR, ALL, and Minimum Move boxes stayed checked. | Play page | Fixed: cleared after each committed vehicle step. |
| 12. An open vehicle CC Location was described with the Ambush wording. | `CloseCombatDue` | Fixed: it names the side to attack or pass. |
| 13. The vehicle CC attacker list included vehicles, broken units, and units that had attacked; ESB showed for trucks; refusals could print negative MP; the "wished to enter next" field is free text. | Play page, planner | The first three fixed; the free-text field recorded (backlog section 21). |

## Visual check

In the Studio (`map-studio-scripted`), game `pass11-check` on board 01 (July 1942): the PzKpfw IIIH in E5 (VCA north-east) and a Russian squad in the building F5. After Start and a VCA change to the east, the entry buttons read "enter bd01:F4:0 (½ MP)", "enter bd01:F5:0" (disabled: "a vehicle enters a building hex only by VBM here; the B23.41 entry of a fully-tracked AFV is not built (ruling R11.7)"), and "enter bd01:F5:0 in VBM beside bd01:F4:0 (2 MP)". The VBM entry read "de-tk enters bd01:F5:0 in VBM, straddling the hexside with bd01:F4:0 for 2 MP; 9 MP left"; the state line read "VCA east, 4 of 13 MP spent, 9 left, moving, in Bypass along the hexside with bd01:F4:0"; and the CC Reaction Fire panel offered r1. The squad's fire at its own Location was refused: "play.fire-reaction: a DEFENDER unit fires at a moving vehicle in its own Location only as Reaction Fire after the vehicle's OVR there (D7.22)".

In `pass11-check-2`, with no ELR declared, an OVR of the squad in F4 was refused at the entry ("play.overrun-refused: ... elr-undeclared"), where before the fix the tank would have been left unable to move on. In `pass11-check-3`, with ELRs 3 and 2, the entry read "de-tk enters bd01:F4:0 for 4½ MP, with an OVR (4 MP, D7.1)"; the resolve button stayed disabled ("The DEFENDER fires or passes first (D7.1)") until the pass; the OVR resolved with 4 + 4.5 + 7.5 = 16 FP, 6 + 5 - 1 FFMO = 10, NMC, and the Reaction Fire panel followed. The fire summary named no firer for the OVR; it now names the vehicle's OVR. The page was driven through its DOM events and read as text.
