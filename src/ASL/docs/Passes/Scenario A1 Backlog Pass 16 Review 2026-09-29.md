# Scenario A1 Backlog Pass 16: source and case review

**Status:** Reviewed. The packages `scenario-a1-fire`, `scenario-a1-rally`, `scenario-a1-close-combat`, and `scenario-a1-ordnance` are revised and republished with their prior manifest digests kept. They have no execution authority. Backlog pass 16 (night and weather).

**Date:** 2026-09-29

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 16), and 5 (rulings R16.1 to R16.14). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 16 Design](<ASL Unit Backlog Pass 16 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope. Chapter E is optional and SSR-driven (E.1), so each rule applies only when a game's special rules name night or the weather.

## Scope

Night (E1.1 to E1.15, E1.3 to E1.33, E1.51, E1.52, E1.54, E1.56, E1.7, E1.75 to E1.77, E1.8, E1.81, E1.9 to E1.923, E1.94), the Wind Change DR (B25.65) as E1.12, E3.4, E3.51, and E3.71 use it, and the weather (E3.1, E3.32, E3.4 to E3.54, E3.6 to E3.65, E3.7 to E3.7332, E3.74 to E3.742, E3.8, E3.9), with the Bog DRM of D8.21. The scenario cards, first planned for this pass, are pass 17.

## Sources

The rule text of each subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass16-pdf-comparison.json`, SHA-256 `7318898a...7f16`, 72 fragments: E1 (pp. 222 to 227; E1.7 in two parts), E3 (pp. 228 to 231; E3.6 in two parts), B25.65 (pp. 144 to 145), and B.8 (p. 112). `ThePass16SubjectsAreVerified` checks the 72. The Fire package cites 13 of them, the Rally package 2, the Close Combat package 1, and the Ordnance package 5.

No counter was added. The Starshell is a marker of the vocabulary (1.15.0), with no printed value.

## Rulings

R16.1 to R16.14 are in the plan, section 5. The referee's findings changed R16.2, R16.5, R16.6, R16.8, R16.10 to R16.14; the table player's changed R16.7.

## Referee findings

| Finding | Rules | Disposition |
|---|---|---|
| 1. The Low Visibility DRM, folded into an ordnance shot's Hindrance, cancelled FFMO and the Open Ground cases | E1.7, E3.1 | Fixed: a Case R term of its own (`case-r:lv`); the Ordnance package gains the fact; tested. |
| 2. Vehicle roads in Mud and snow: every unpaved or unplowed road became Open Ground, a woods road cost woods entry, and snow roads lost their entry | E3.6, E3.65, E3.724, E3.7331 | Fixed: in Mud an unpaved road costs the Open Ground COT whatever the hex holds; in snow a road keeps its entry, at least 1 MP; `InMudAnUnpavedRoadIntoWoodsCostsTheOpenGroundRate`, `InDeepSnowARoadEntryCostsAtLeastOneMp`. |
| 3. Marsh and brush as Open Ground in snow were claimed but not built | E3.722, E3.73 | Recorded: R16.13 corrected; backlog section 26. |
| 4. Rain that stopped no longer counted as fallen | E3.54 | Fixed: `GameState.Rained`; `RainThatStoppedHasStillFallen`. |
| 5. NVR was ignored in the Starshell's enemy checks, routing, and a berserk charge | E1.33, E1.91, E1.922 | Fixed for Starshells (`TheFirstStarshellNeedsAnEnemyUnitSeenWithinNvr`); routing and berserk recorded in the backlog. |
| 6. Target Acquisition applied at night | E1.74 | Recorded: R16.2 says so; backlog section 26. |
| 7. SMOKE grenades were placed in rain, Mud, and Deep Snow | E3.53, E3.734 | Fixed: only inside a building; `NoSmokeIsPlacedInMud`. |
| 8. The NVR 0 range of a moving vehicle matched no reading | E1.14 | Fixed: 1 hex wheeled, 2 tracked; R16.2 corrected. |
| 9. A BU AFV's halved NVR was not used for its MA | E1.14 | Fixed: the firing vehicle is the viewer. |
| 10. No night MF on a stairwell move | E1.51 | Fixed. |
| 11. The Low Visibility DRM was capped at 4 | A6.2, E3.1 | Fixed: no cap; with the other Hindrances it blocks the LOS at 6; the Fire package accepts 0 to 5; tested. |
| 12. The weather Bog DRM applied in buildings | D8.21 notes 2, 3 | Fixed: not in a building (never by road). |
| 13. The snow road minimum was added as half an MP | E3.724, E3.7331 | Fixed: the entry's own cost is at least 1 MP. |
| 14. No Wind Change DR in the opening RPh | B25.65, E3.51 | Recorded: the game starts in that RPh; R16.10 says so; backlog. |
| 15. The Starshell timing limit applied in the Player Turn of the first Starshell | E1.921 | Fixed: `GameState.StarshellTurn`; `AnMmcMayFireAStarshellLaterInThePlayerTurnOfTheFirst`. |
| 16. Some Gunflash sources were missing (Opportunity and No Fire counters, a FT's target, a DC) | E1.8, E1.84, E1.85 | Recorded: backlog section 26. |
| 17. The Japanese were not Axis, and Extreme Winter was accepted without snow | E3.741, E3.74 | Fixed; `TheNightAndWeatherRulesAreCheckedAtSetup`. |
| 18. A BU AFV at NVR 0 moved freely | E1.52 | Fixed: only Stop; `ABuAfvWhoseNvrIsZeroOnlyStops`. |
| 19. R16.6 left out Mopping Up | E1.54 | Fixed in the wording (Mopping Up is not built). |
| 20. The verifiers take the night and weather facts as recorded | | Kept: like the LOS, TEM, and Hindrance facts before them, they are the planner's reading of the map and the SSRs; the verifiers still check that by day none is set for the CC Ambush. |

The referee found R16.1, R16.3's exceptions, R16.4, R16.6 in code, R16.7, R16.9, R16.10's precipitation, R16.11's Mist arithmetic, R16.12's cushion, and R16.14's dates sound, and no freeze.

## Table player findings

The table player played 58 situations (86 cases) in a local clone in the scratchpad: fire at night within and beyond NVR, Gunflashes of every fire counter and Melee, Illuminated firers, moving vehicles, the Low Visibility DRM's exceptions, Mist, rain, and snow at many ranges, DC and multi-Location fire at night, Starshells by every method and at the map edge, the Wind Change DR's NVR, rain, and Gusts, rout, DM, Recovery, SAN, concealment, the night Ambush, Infantry and vehicle MF and MP in every weather, Bog, and Extreme Winter. The 57 situations kept as tests are `BacklogPass16TablePlayerTests`.

| Finding | Rules | Disposition |
|---|---|---|
| 1. A hidden unit's Starshell was stored but reported as failed | E1.921 | Fixed: the gate reads the Starshell's record back; `AHiddenStarshellFirerIsConcealed`. |
| 2. A moving vehicle beyond NVR was never seen: the vehicles of the target Location were not read | E1.14 | Fixed; `AMovingTruckIsSeenAtOneAndAHalfNvr`, `AMovingTankIsSeenAtTwiceNvr`. |
| 3. A truck on a road in Ground Snow was charged cross-country | E3.724 | Fixed with the referee's finding 2; `TruckOnAnUnplowedRoadInGroundSnow`. |
| 4. The night Ambush margin applied to both sides | E1.77 | Fixed: only the ATTACKER's need be two lower; R16.7 corrected; `NightAmbushMargin`. |
| 5. The first Starshell ignored NVR | E1.91, E1.101 | Fixed with the referee's finding 5. |
| 6. Method 2 accepted a unit beyond NVR | E1.922, E1.33 | Fixed with the referee's finding 5; `AtTargetNeedsAKnownUnitOrAGunflash`. |
| 7. A router treated an enemy beyond NVR as Known | E1.33, E1.54 | Recorded: backlog section 26. |
| 8. The Starshell timing limit started a Player Turn early | E1.921 | Fixed with the referee's finding 15. |
| 9. "After rain" was lost when the rain stopped | E3.54 | Fixed with the referee's finding 4. |

## Visual check

In the Studio (`map-studio-scripted`, driven by DOM events), game `pass16-demo`: a new game with the special rules `night:1 weather:overcast` showed "Night: Base NVR 1 (E1.1); weather: overcast (E3)." under the summary; fire at a squad four hexes away was refused as beyond the NVR; the first Starshell was refused because board 01 blocks the LOS to the only enemy (E1.91). Game `pass16-demo2`, with the enemy three hexes away in the LOS: a leader's Starshell aimed three hexes out landed in B3, and the line and the Night and weather list named it; fire at the squad in B4 was then accepted, its record reading "IFT DR 1, 1 = 2 + 1 (lv-hindrance, E1.7)".
