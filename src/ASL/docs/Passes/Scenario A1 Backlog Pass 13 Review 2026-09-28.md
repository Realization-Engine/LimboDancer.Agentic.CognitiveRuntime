# Scenario A1 Backlog Pass 13: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire` (captured MG) and `scenario-a1-rally` (terrain, Self-Rally) are revised and republished with their prior manifest digests kept. They have no execution authority. Backlog pass 13 (Rally, Rout, and support weapons).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 13), and 5 (rulings R13.1 to R13.8). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 13 Design](<ASL Unit Backlog Pass 13 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

Deployment and Recombining (A1.31, A1.32), SW transfer, drop, and Recovery (A4.43, A4.431, A4.44, A10.4), dismantling (A9.8), the Rout Phase, Low Crawl, and Interdiction (A10.5 to A10.533), DM (A10.62), rally terrain and Self-Rally (A10.6, A10.61, A10.63), surrender in the RtPh (A20.21), and captured MG and ATR (A21.1 to A21.12).

## Sources

The rule text of each new subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass13-pdf-comparison.json`, SHA-256 `3488cd18...8409`, 21 Chapter A fragments: A1.31, A1.32 (p. 45), A4.43 (two fragments), A4.431, A4.44 (p. 50), A9.8 (p. 65), A10.5 (two), A10.51 (two), A10.52, A10.53, A10.531 (two), A10.532, A10.533 (two) (pp. 66 to 68), A21.1, A21.11, A21.12 (p. 88). A10.51, A10.531, and A10.532 each match in two parts around a column break or an example. A10.6, A10.61, A10.62, A10.63, and A20.21 were registered by earlier passes. `ThePass13SubjectsAreVerified` checks all 21; the Fire package now cites 330 fragments.

## Rulings

R13.1 to R13.8 are in the plan, section 5. The referee's and the table player's findings changed R13.3, R13.4, and R13.5.

## Referee findings

| Finding | Rules | Disposition |
|---|---|---|
| 1. Burning wrecks and SMOKE were read as a Blaze. | B25.4, B25.141 | Fixed: no Blaze is read (terrain Blazes are not built; a Wreck Blaze does not bar movement); R13.3 says so. |
| 2. A unit that routed and stayed in the open was eliminated for Failure to Rout, as was one pinned by Interdiction. | A10.5, A10.53 | Fixed: the Open Ground test applies only to units that did not rout, and a pinned unit only to the ADJACENT test; test `AUnitThatRoutedIntoTheOpenWithNoCoverInReachIsNotEliminated`. |
| 3. An ATTACKER unit with no legal step held up every DEFENDER rout. | A10.5 | Fixed: only ATTACKER units that must rout and have a legal step go first. |
| 4. Low Crawl skipped the terrain check. | A10.52 | Fixed. |
| 5. Concealed and hidden units Interdicted. | A10.533 | Fixed: only Known units Interdict; test (a concealed German). |
| 6. The nearest-cover search ignored which enemies had seen the unit on the way. | A10.51 | Fixed: the search follows the set of enemies that have seen it. |
| 7. Ignorable woods and buildings could not be chosen. | A10.51 EXC | Fixed: they may be chosen when no farther in MF; the same-building EXC is recorded (backlog section 23). |
| 8. Surrender was decided only as the RtPh ended, so a trapped unit could rout away. | A20.21 | Fixed: such a unit may not rout and surrenders as the RtPh ends; test in `ABrokenUnitAdjacentToItsEnemyAtTheEndOfTheRtPhSurrenders`. |
| 9. A HS left by Interdiction could rout again; a leader wounded on the way kept 6 MF. | A10.53, A10.5 | Fixed. |
| 10. An Encircled unit's first rout step was not doubled. | A7.7 | Fixed. |
| 11. Grain out of season was not Open Ground. | B15.6, A10.531 | Fixed. |
| 12. Deployed HS and a recombined squad had a fresh RPh action. | A1.31, A1.32 | Fixed; asserted in `TwoHalfSquadsRecombineWithTheirLeader`. |
| 13. A leader who directed a Rally attempt could direct a Deployment. | A1.31 | Fixed. |
| 14. A leader could permit only one Recombination. | A1.32 | Fixed. |
| 15. A transfer spent both units' RPh action. | A4.431 | Fixed: a transfer is not an RPh action; R13.5 reworded; test changed. |
| 16. Guards Deployed with no NTC. | A1.31 | Fixed: Guards take the NTC unmodified; R13.4 reworded. |
| 17. A plan that rolled dice gave no DM from an ADJACENT enemy. | R13.1 | Fixed. |
| 18. The DEFENDER could transfer in the ATTACKER's APh. | A4.431 | Fixed. |
| 19. A unit could not Recover a second SW in its MPh; R13.5's "before it moves" is stricter than A4.4. | A4.44, A4.4 | Fixed, then widened by the table player's finding 2. |
| 20. A broken unit could drop any SW in the RtPh. | A10.4 | Fixed: only a laden unit drops, and only toward its best load. |
| 21. A MG First Fired in the MPh could not be dismantled in the DFPh. | A9.8 | Fixed. |
| 22. A captured MG's Fire Lane used the uncaptured B#. | A21.11 | Fixed. |
| 23. The planner did not refuse a pinned broken unit's rout. | A10.53 | Fixed. |
| 24. The Interdiction NMC takes no leadership DRM; units with halved FP other than pinned ones Interdict; vehicles and Guns have no Normal Range here. | A10.53, A10.532 | Recorded (backlog section 23). |
| 25. R13.3 said the unit "follows a shortest route". | A10.51 | Reworded: it must reach the cover this RtPh, not necessarily by a shortest route. |

It confirmed the ATTACKER-first order, the direction and ADJACENT restrictions, the 6 MF and wounded SMC 3 MF, Low Crawl's one Location and its exits, the Interdiction NMC against the broken Morale Level with Casualty Reduction, pin, and Original 12, DM's sources and retention, the rally terrain and Self-Rally, A1.31's NTC with the leadership DRM, and the captured MG's B# and ROF.

## Table player findings

The table player played 27 situations through the planner, projector, and page, in a separate worktree.

| Finding | Where | Disposition |
|---|---|---|
| 1. A leader who directed a Deployment or permitted a Recombination could still rally a unit. | A1.31, A1.32 | Fixed; test `ALeaderWhoDirectedADeploymentRalliesNoOneAndTheDefenderDoesNotDeploy`. |
| 2. A unit could not drop or Recover a SW once it had moved. | A4.4, A4.44 | Fixed: at any point in its move, Recovery needing one MF left; R13.5 reworded; test `AUnitDropsAndRecoversASwDuringItsMove`. The DEFENDER's fire window for the Recovery MF is recorded (backlog section 23). |
| 3. Broken enemy units forced routs, gave DM, and caused Failure to Rout. | A10.531 | Fixed: only enemy units able to fire apply FFMO; test `ABrokenEnemyDoesNotMakeALocationOpenGround`. |
| 4. A unit that dismantled its MMG in the PFPh could move. | A9.8, A3.3 | Fixed: dismantling is its use of a SW; test `DismantlingInThePfphIsAUseOfTheSwSoTheSquadDoesNotMove`. |
| 5. The DEFENDER could Deploy in the ATTACKER's RPh. | A1.31 ("their RPh") | Fixed; Recombining stays allowed in any RPh (A1.32). |
| 6. A broken unit could drop the SW it should keep. | A10.4 | Fixed: a drop must leave a best load; test `ABrokenUnitDropsOnlyWhatItCannotCarry`. |
| 7. The Deploy descriptions said Guards need no NTC. | A1.31 | Fixed. |
| 8. The rout proposal did not say which steps Interdiction reaches or by whom. | A10.53 | Fixed: the proposal lists them, and the Interdiction record names its Interdictor. |
| 9. The Rout panel did not show the destination or a unit with no legal step. | A10.51 | Fixed: each unit shows the cover it must reach, or that it has no legal step. The ATTACKER's pending "may rout" choices and a Failure to Rout preview are recorded (backlog section 23). |
| 10. The records list left out Deployment's HS, Recombining, dismantling, and dropped SW. | Play page | Fixed; DM gained, Failure to Rout, and transfers are recorded (backlog section 23). |
| 11. Generated unit ids are hard to follow. | Planner | Recorded (backlog section 23). |
| 12. The DM retention list offered units in woods and buildings. | Play page | Fixed. |
| 13. Refusals joined several causes. | Planner | Fixed for the rout and surrender refusals. |
| 14. The Recovery record read "against 6". | Play page | Fixed: "needing below 6". |
| 15. A Deployment's second HS takes one SW on the page. | Play page | Recorded (backlog section 23); the action takes a list. |
| 16. A route is typed at level 0. | Play page | Recorded (backlog section 23); a full Location may be typed. |

## Visual check

In the Studio (`map-studio-scripted`), game `pass13-demo` on board 01 (July, ELR 3 and 2): German g1 in E2, g2 with a LMG and the 8-1 leader gl in C3, Russian r1 broken in E3 and r2 broken in H4. In the RPh the Support weapons panel passed the LMG from g2 to gl, and the Deploy control, with scripted dice 3 and 3, recorded "g2 tries to Deploy with gl: NTC DR 6 -1 against morale 7: two HS (A1.31)". In the RtPh the Rout panel read "r1 (russian): must rout: g1 is a Known unbroken armed enemy unit ADJACENT to it or in its Location"; a rout to E4 committed and was recorded "r1 routs to bd01:E4:0 for 2 MF (A10.5)". The rout took about 15 seconds to plan on the real board; the route search and the board's LOS map now keep their reads, and in `pass13-demo-2` the same rout planned in under 4 seconds. The page was driven through its DOM events and read as text.
