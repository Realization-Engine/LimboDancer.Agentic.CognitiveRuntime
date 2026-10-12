# Pass 40: the five reviews' briefs

Pass 40 (Infantry I: movement, stacking, and Locations) is built on `feature/asl-backlog-pass-40`, cut from main at `3c660afc`. Its design is `src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md`: sections 7 (stacking, 40.6), 8 (the road, Dash, Hazardous Movement: 40.2, 40.3, 40.8), 9 (portage and movement corrections, 40.7), 10 (Infantry OVR and the attack owed under A8.312: 40.4, 40.5), 11 (the small rows, 40.9). Rulings R40.1 to R40.25 are in the design only.

## Said in every brief

- Read-only. Do not edit any file, build, run tests, start a server, or run a git command that changes state.
- The pass is `git diff 3c660afc..HEAD -- <path>`, run from `E:\Archive\GitHub\dlandi\LimboDancer.MCP`.
- Search with the Grep and Read tools; never `grep -r` in a shell over `src/ASL` (it walks `bin` and `obj`).
- The rulebook's page texts are in `E:\Archive\GitHub\dlandi\pass32-tools\pass35\session3\pages\pNNN.txt` (physical pages 043 to 253). A rule is read on its page file, never from memory. Quote at most one phrase of under 15 words for a finding.
- No file is written: the findings are the closing message. At most 12, most serious first, each with a grade (BLOCKER, SHOULD FIX, NOTE), the file and line, the rule and its page, what is wrong, and the smallest fix. Then one paragraph on what was read and what could not be checked.
- What the design lists as "left out" or "not built" is not a finding. A finding is a case the code reaches that the rule does not cover, a case of the rule left out that the design does not list, or a plain fault.

## 1. The referee, movement (the design's sections 7 to 9)

Tasks 40.6, 40.2, 40.3, 40.8, 40.7. Rulings R40.1 to R40.11.

- **Pages:** A4.11 to A4.134 (48); A4.4 to A4.44 (50); A4.5, A4.51, A4.61, A4.62, A4.63 (51); A4.72 (52); A5.1 to A5.6 (52 to 53); C10.3 (181).
- **Members:** in Rules, `ScenarioA1Stacking`, `ScenarioA1Dash`, `ScenarioA1Definitions` (`IsWoodsRoadHex`, `RoadRateTargetTerrain`), `ScenarioA1MovementCalculator` (`MoveTiming`, `AssaultHolds`, `PortagedTwiceBar`, the IPC members, `EnemiesAtTarget`), `ScenarioA1SupportWeaponRules` (`RecoveryBypassBar`), the overstack and Dash terms in `ScenarioA1FireCalculator`; in Maps, `LosTargetPoints`; in Play, `LiveStacking.cs` and the facts handed in `GamePlanner.Movement.cs`.
- **Asked:** for each task, a case the code reaches that the rule does not cover, or a case of the rule the design does not list as left out. Whether the page supports, contradicts, or is silent on: the road's two target points (A4.132); Dash's halving held against a Fire Group if it holds against any one Location (R40.9); the leader's IPC by the mover's name (R40.11). Whether a refusal or a record tells a side of a concealed or hidden enemy unit before the rule reveals it. Whether Play decides a rule.

## 2. The referee, enemy Locations and the small rows (sections 10 and 11)

Tasks 40.4, 40.5, 40.9. Rulings R40.12 to R40.25.

- **Pages:** A1.31, A1.32, A1.6, A2.3 (45); A4.14 to A4.152 (49); A7.352, A7.353 (56); A7.55 (57); A8.3, A8.31, A8.311, A8.312 (61); A15.43 to A15.432 (82 to 83); A20.5, A20.54 (88).
- **Members:** in Rules, `ScenarioA1InfantryOverrun`, `ScenarioA1MandatoryFire`, `ScenarioA1SupportWeaponRules` (`NeedsNoLeader`, `GuardDeploysAtWill`, `RecombineStartBar`), `ScenarioA1Definitions.UnitSize`, `ScenarioA1SetupCalculator.InAreas`, the crew term in `ScenarioA1FireCalculator` (`CrewAddsInherentFp`); in Play, `GamePlanner.InfantryOverrun.cs`, `GamePlanner.MandatoryFire.cs`, the OVR parts of `GamePlanner.Movement.cs` and `GamePlanner.VehicleCloseCombat.cs`, `PlanDeploy` and `PlanRecombine` in `GamePlanner.SupportWeapons.cs`, `OtherHalf` in `GamePlanner.CardSetup.cs`.
- **Asked:** the same first question. In particular: whether the NTC, the doubled MF, the SMC's move away, and the CC at once follow A4.15 to A4.152 in their order; whether the attack owed can be escaped (a pass, an end of the move, another act of the DEFENDER or the ATTACKER) or can deadlock a game (owed with no way to make it); whether the trial plan inside `MandatoryFire` can recurse or change state; whether ruling R40.17's fallback is the least departure from A8.312; whether a Guard squad's Deployment at will leaves its prisoners guarded; whether `RecombineStartBar` reads "the start of any RPh" too strictly or too loosely; whether `InAreas` is A2.3. Whether any of it tells a side of a concealed or hidden enemy unit.

## 3. The table player

Reads what the game says to its players. The 31 test games are `p40-*.game.json` in `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/`, each with a `label` and its events. The sentences come from the planner's summaries and refusals (literals beginning "play." in Play and in Rules) and from `PlayRecords.cs` and `ReplaySteps.cs` in the Studio's Services. The house style: plain language, a hex in brackets, a unit by its counter and tag, rules cited in parentheses, ASL's terms with their capitals, no internal identifier, no em-dash.

- **Asked:** every refusal, summary, note, and record sentence the pass added or changed (from the diff's string literals): one a player could not follow, that says the wrong thing by the rule it cites, cites the wrong number, leaves no way forward, or shows an identifier; and what it would write instead. In the played games, the arithmetic a player would check at the table (MF, the NTC's DRM, the FP and DRM of an attack against the recorded facts), with the event's revision for any that looks wrong. Any game whose label promises something its events do not show. Which tasks have no played game, and whether the design admits it.

## 4. The Rules boundary

The user's ruling: every ASL rule is a calculator in `LimboDancer.Domains.Asl.Rules` that takes facts and returns a verdict; LOS and range stay in Maps, read by Play and handed over as facts; Units references Rules so the projector calls the same calculators; Rules references only Abstractions; the Studio decides no rule. The diff in Rules, Play, Units, Maps, and the Studio.

- **Asked, in the added or changed lines only:** a rule decided in Play, Units, or the Studio (a DRM computed, a "may" or "must" decided, an exception tested), as against fact gathering and event writing. Look hardest at `GamePlanner.MandatoryFire.cs` (the trial plan, `UsableMgs`, the plainest attack's arguments), `GamePlanner.InfantryOverrun.cs` (`EvadeDestinations`, `OverrunEvader`, the NTC's morale and TEM reads), `PlanDeploy` and `PlanRecombine` (the `atWill` and `noLeader` branches, `SideActedThisRph`), `LiveStacking.cs`, the projector's handling of `infantry-overrun-ntc`, and the page's `GuardSquads` and its `MandatoryFire` calls. A Rules member that is not a pure verdict over facts; a reference from Rules to Units, Maps, or Play; one rule written in two places; a new required positional parameter with no default on a record serialized into game events; a new event field an older record lacks that is not optional on read; dead code the pass left.

## 5. The UI and Blazor review

`git diff 3c660afc..HEAD` over `src/ASL/LimboDancer.Domains.Asl.MapStudio`. The page is played hot-seat: each side has its own view, a view never reads what the other side holds in secret, and a hand-over clears everything the last view prepared. `Play.razor` is about 7,000 lines: the diff first, then only what it touches.

- **Asked:** for each control the pass added (the move panel's road, Dash, IPC, and Infantry OVR inputs; `#propose-overrun-ntc`; the `#overrun-evade` block; the Mandatory FG's note and buttons in `#dff-window`; the `#guard-deploy` block), any path on which a stale selection reaches a proposal or leaves a button enabled: a proposal refused by the gate, one cancelled, a hand-over, a refresh caused by the other side's commit, a phase change. Page state the pass added that `ResetDrafts`, `ResetPanelChoices`, `ResetForView`, or the game-open path does not clear; each field with its line and the reset that misses it. Whether a block is shown to a view not entitled to it: the SMC's "moves away" block and the Mandatory FG's note name units and a Location, and `Live.Planner.MandatoryFire` and `OverrunEvader` read the whole state; whether what they show the DEFENDER's view, the ATTACKER's view, and the adjudicator is only what each may know. Whether the NTC's record sentence in `PlayRecords` reaches a view not entitled to it. Blazor correctness in the changed markup (`@key`, a select bound to a value its options may not hold, bool attributes, nullable flow, a planner call made several times in one render). Cost: `MandatoryFire` runs trial fire plans and is called from the markup; say how often per render and whether it should be cached.
