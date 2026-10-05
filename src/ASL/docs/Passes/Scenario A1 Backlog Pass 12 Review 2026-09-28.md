# Scenario A1 Backlog Pass 12: source and case review

**Status:** Reviewed. The package `scenario-a1-fire` is revised for the fire extensions and republished with its prior manifest digest kept. It has no execution authority. Backlog pass 12 (Fire extensions).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 12), and 5 (rulings R12.1 to R12.12). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 12 Design](<ASL Unit Backlog Pass 12 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

Opportunity Fire (A7.24, A7.25), fire at a blocked LOS (A6.11, A7.52), FPF variants and pinned movers (A8.31, A7.83), split fire and SMC fire (A7.351 to A7.353, A7.53, A9.12), concealment gain and the Concealment Table (A12.12 to A12.122, p. 107), Spraying Fire (A9.5 to A9.52), Fire Lanes (A9.22 to A9.223), fire into a Melee and at prisoners (A11.15, A20.52, A20.54), berserk fire (A15.432), and Encirclement (A7.7, A7.71).

## Sources

The rule text of each new subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass12-pdf-comparison.json`, SHA-256 `c4f006db...18e59a`, 16 Chapter A fragments. A7.7 and A9.5 each match in two parts around a boxed example. A9.222's Markdown text merges the rule with its example and does not match; it is cited from the PDF (p. 63) only. A6.11, A7.25, A7.7, A7.71, A9.12, A9.22, A9.223, A9.5, A9.51, A9.52, A12.121, and A12.122 are new; A7.24, A7.351, A7.352, A7.52, A7.53, A7.83, A8.31, A11.15, A12.12, A15.432, A20.52, and A20.54 were registered by earlier passes. `ThePass12SubjectsAreVerified` checks all 16. The Concealment Terrain is the Terrain Chart's red terrain (p. 698), read from the PDF's text colors.

## Rulings

R12.1 to R12.12 are in the plan, section 5. The referee's findings changed R12.1, R12.3 to R12.7, R12.9 to R12.11.

## Referee findings

| Finding | Rules | Disposition |
|---|---|---|
| 1. Assault Fire was added to Opportunity Fire. | A7.36, p. 56 | Fixed; R12.1 reworded. No catalog unit has Assault Fire, so no test exercises it. |
| 2. The concealment dr subtracted brush, grain, orchard, and marsh as in-hex Hindrance; only SMOKE hinders a unit in its own hex. | A6.7, p. 51; A12.122 | Fixed: SMOKE in the Location only; R12.5 reworded. |
| 3. A Fire Lane applied grain, brush, marsh, and SMOKE Hindrance as a DRM, and gave no wall or hedge TEM. | A9.222, p. 63 | The first fixed (they cancel FFMO, no DRM); the hexside TEM recorded (backlog section 22); R12.7 reworded. |
| 4. A MG with a Fire Lane could fire again in the MPh. | A9.22, A9.223 | Fixed, with a test: its MG, and its manning Infantry's Subsequent First Fire or FPF, are refused while the lane stands. |
| 5. Non-FPF firers in a mixed FPF group got a Final Fire counter. | A8.31, A8.3 | Fixed: FPF groups with Subsequent First Fire firers only, all rightly marked Final Fire; an unmarked unit's First Fire does not join, with a test; R12.3 reworded. |
| 6. Subsequent First Fire firers in a mixed FPF group skipped A8.3's limits. | A8.3 | Fixed: the range and closest-enemy limits apply to them. |
| 7. A squad could fire two MGs and its inherent FP in one phase. | A7.351 | Fixed: no more than two SW, and no inherent FP with two. |
| 8. A leader could direct and then fire a MG, and fire two MGs. | A9.12, p. 62 | Fixed: a leader who directed fire this phase fires no MG, and fires one MG a phase; R12.4 reworded. |
| 9. A crew's US# is 2. | Index; A20.51 | Fixed in the Guard limit and the concealment dr. |
| 10. Spraying Fire did not check that each SW is a MG. | A9.5, A9.52 | Fixed. |
| 11. Spraying Fire never counts toward Encirclement. | A7.7 | Recorded as a limit (backlog section 22); R12.6 says so. |
| 12. Encirclement's MF doubling applies to rout too; a vehicle kept an Encirclement alive; a citation. | A7.7, A15.41 | Personnel only, fixed; rout noted for pass 13; the citation corrected. |

It confirmed Opportunity Fire's declaration, Case D, the missing AFPh halving, and Multiple ROF; the blocked LOS DR, markers, and split; the directed FPF NMC and its leadership; A7.83's second Final DR; A9.12's Area Fire and partner; the Concealment Table's Cases I, J, K, and NA; Spraying Fire's hexside, DR, Area Fire, and A9.52; the Fire Lane's Residual FP column, PBF, no reduction, no Cowering, malfunction, and cancellation; fire into a Melee and at prisoners; berserk fire; and the Encirclement geometry, consecutiveness, NMC test, Morale Level, and +1.

## Table player findings

The table player walked turns through the planner, projector, package, and page.

| Finding | Where | Disposition |
|---|---|---|
| 1. An attack that sealed an Encirclement and eliminated every target could not be committed. | A7.7; `Encircle` | Fixed: no Encirclement is placed on an empty Location; test `AnAttackThatEncirclesAndEliminatesItsTargetsStillCommits`. |
| 2. After an owner's choice stopped the first attack, the spray's second Location, the Encirclement, and the Fire Lane were lost; lane attacks ran on after a pending choice and could read the Residual record's DR. | A9.5, A7.7, A9.22 | The lane attack now stops at a pending choice and reads its own record; the follow-ons after a choice are recorded (backlog section 22). |
| 3. A squad that fired only its MG in the PFPh could move in the MPh. | A3.3 | Fixed: a unit that fired only a SW in the PFPh does not move; test `ASquadThatFiredOnlyItsMgInThePfphDoesNotMove`. |
| 4. Fire Lanes never attacked vehicles or units spending MF in a lane Location other than by entering it. | A9.222, A8.22 | Recorded (backlog section 22). |
| 5. Every lane Hindrance was treated as soft, and SMOKE placed later was not read. | A9.222 | Recorded (backlog section 22). |
| 6. An ordnance shot at another Location did not break an Encirclement sequence, and earlier Spraying Fire counted. | A7.7 | Fixed. |
| 7. Spraying Fire's second Location escaped A7.55's one group per target. | A7.55, A9.52 | Fixed; test `SprayingFireKeepsOneGroupPerTarget`. |
| 8. An Opportunity Firer or a berserk unit could fire ordnance or a LATW in its PFPh. | A7.25, A15.432 | Fixed. |
| 9. The page showed no Fire Lanes or Encircled Locations. | Play page | Fixed: they are listed; drawing them on the map is recorded (backlog section 22). |
| 10. The Opportunity Fire boxes stayed checked into the next PFPh; a Fire Lane could name an unticked MG; the partner was free text; the page left crews out; the move refusal did not name Opportunity Fire. | Play page, planner | Fixed. |
| 11. An enemy entering the lane MG's hex draws no fire from it or its manning squad. | A9.223 | Recorded with the TPBF cancellation (backlog section 22). |

## Visual check

In the Studio (`map-studio-scripted`), game `pass12-check` on board 01 (July 1942, ELR 3 and 2): German squads g1 (with an LMG) in E5 and g2 in E6, Russian squads r1 in F4 and r2 in the stone building F5. In the German PFPh the Opportunity Fire panel offered g1 and g2; declaring it for g2 read "play.opportunity-fire: g2 hold their fire for the AFPh under a Bounding Fire counter, and do not move in the MPh (A7.25)", and the units table showed g2's Bounding Fire counter. g1 with its LMG then sprayed F4 and F5: one roll (6, 5) and two records, each at "4 FP x 2 (point-blank-fire) x 0.5 (spraying-fire) = 4" and "3 FP x 2 x 0.5 = 3", 7 FP on the 6 column, F5 taking its +3 stone building TEM. In the MPh neither German squad was offered a move. In the AFPh g2 fired at F5 at "4 FP x 2 (point-blank-fire) = 8", with no AFPh halving. The fire proposal did not name the Spraying Fire's second Location; it now does. The page was driven through its DOM events and read as text.
