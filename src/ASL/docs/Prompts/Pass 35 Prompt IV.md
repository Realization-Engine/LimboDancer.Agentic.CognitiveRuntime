# Pass 35, fourth session: the reviews, their fixes, the Studio check; then stop at the gate

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, branch `feature/asl-backlog-pass-35`, local and not pushed, 33 commits past main (f3b7c47, pushed); the head is 5dd9934, "Pass 35: the pass's documents", or this prompt's own commit on top of it. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its last seven lessons are the third session's. Then read the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 35 Design.md`: sections 13 to 18 are the third session's, and section 16 is this session's plan.

## How this session runs, at the user's word of 2026-10-09

1. **The reviews first, read-only and in parallel, then stop for the user with their findings.** Fix nothing before the user has seen the findings and said which to fix.
2. **Each fix as the pass ran:** the rule read on its page of the registered PDF, the repair in Rules (Play hands facts over), MapStudio built with warnings as errors, a small test game in the Studio, a Rules test and a Play or page test where one applies, a report, and a stop for the user's word before the commit. One commit a fix. Do not push.
3. **Then the Studio check of the whole pass,** and stop.
4. **Stop at the gate.** Do not start the gate's runs, the baseline of main, or the merge. The user starts the gate.
5. **Be sparing:** narrow reads, one value from the page, the test classes a change touches and no wider. Read the clock (`date +%H:%M`) before writing a time into the log.

## Where the third session stopped

Built and committed in the third session, on top of the second's 5172eba:

| Commit | What |
|---|---|
| d667ced | The vehicle Close Combat panel's shared draft (page only) |
| d9e32ca | Task 35.16: the seven tests not written and the sweep of Play; the Recombine review's wording |
| 9448658 | The terrain increment's two owed tests, with two Studio games for the grain |
| 909b113 | The design's section 13 |
| 9749bff | The design's sections 14 to 18 |
| 5dd9934 | The documents: rulings R35.1 to R35.10, the inventory's 59 rows and its count tables, the coverage document, the backlog's section 55.2, the plan's status, seven lessons in the standing rules |

**The build of the seventeen tasks is done.** Every question put to the user is answered (the design's section 18).

**Run in the third session, read-only:** the replay digest over the proof store with the build of 9448658 is identical to main's (309 games, 88,419 states); of the 210 live games, 209 replay whole and `p35-acquire` stops at its shot's record, which the design's section 14.3 explains. It is kept as it is.

**Not run whole in this pass:** the Rules project and the Play project (only the classes each change touched), and MapStudio's page tests but for the classes named in the time log. That is the gate's.

## What is left, in order

1. **The four reviews** (below). They were briefed and launched once on 2026-10-09 and stopped at the user's word before any reported; nothing of them is kept.
2. **The fixes** the user chooses from their findings, each as point 2 above; a finding not fixed gets a row in the backlog's section 55.2 with its reason.
3. **The Studio check of the whole pass** (the design's section 16): at 1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, and 320 pixels, by keyboard and mouse, in each side's view and the adjudicator's. The pass changed four Studio files (`Play.razor`, `VehicleCloseCombatPanel.razor`, `PlayRecords.cs`, `ReplaySteps.cs`): walk the vehicle panels and the records at every width, and one full Player Turn of a game with vehicles at the largest and the smallest.
4. **The documents that wait on the reviews:** the pass's review, `src/ASL/docs/Passes/Scenario A1 Backlog Pass 35 Review 2026-10-09.md` (the form of the pass 31d review: status, what a player now meets, the rulings, the reviews' findings and what was done with each, the Studio check); the design's section 19, "As built"; any ruling a referee's finding changes; the time log; a lesson for the standing rules if the session teaches one.
5. **Stop.** The gate is the user's to start: the whole Rules and Play projects, MapStudio's page tests, the solution build with warnings as errors, the Node viewport test, the chart supplement, Docker, and the three proofs against a baseline built from main's own sources, with the proof store brought up to date first (the design's sections 14.4 and 15).

## The four reviews

All read-only, in the main checkout (worktree isolation fails here), five agents in one message so they run side by side: the referee's is split in two by chapter. An agent cannot write a Markdown report file here: each gives its findings in its closing message, at most 15, most serious first, each with a grade (BLOCKER, SHOULD FIX, NOTE), the file and line, the rule and its page with the deciding words (under 15 words), what is wrong, and the smallest fix; then a paragraph on what it read and what it could not check. Keep a digest of each in the scratchpad.

**Say in every brief:** do not edit any file, build, run tests, start a server, or run a git command that changes state; the pass is `git diff f3b7c47..HEAD -- <path>`; search with the Grep and Read tools and never `grep -r` in a shell over `src/ASL` (it walks `bin` and `obj`); the rulebook's page texts are in `E:\Archive\GitHub\dlandi\pass32-tools\pass35\session3\pages\pNNN.txt` (physical pages 043 to 253, made by `dump_pages.py` beside them; make them again if the folder is gone), and a rule is read on its page file, never from memory.

### 1. The referee, chapters A and B (the design's sections 7 to 9)

Tasks 35.17, 35.2, 35.4 with ADJACENT; 35.1, 35.3, 35.8; 35.5, 35.6, 35.7, 35.9. Rulings R35.1 to R35.6 and R35.9 in `src/ASL/docs/ASL Unit Backlog Passes Plan.md`.

- **Pages to read at least:** A.7, A.8, A.18 (43 to 44); A10.5 to A10.533, A10.8 (66 to 69); A12.14, A12.15 (77 to 78); A17.1, A17.11 (85); A19.12 (86); A20.21 (86 to 87); A25.2, A25.211, A25.22 (93 to 94); B.2, B.10, B1.14 to B1.17 (112 to 113); B14.4, B15.2, B15.6 (129); B16.31, B16.43 (130); B23.25 (136); B25.14 (143); C3.53 (170).
- **Members to read:** in Rules, `ScenarioA1RoutCalculator` (`CouldApplyFfmo`, `RoutStillOwed`, `AttackerMustRoutFirst`, `MayRout`, `DisruptedLowCrawlBar`, `RoutRepulse`, `RoutRepulseShown`, `SurrenderCandidate`, `SurrendersInstead`, `BrokenMorale`, `InterdictionRange`, `CasualtyReduction`), `ScenarioA1Wounds`, `ScenarioA1Definitions` (`GoodOrderOf`, `FreeToActAsPlanned`, `MoraleCeiling`, the Good Order scans), `ScenarioA1MovementCalculator.IsAdjacent`, `ScenarioA1TerrainCosts.GroundStep`, `ScenarioA1FireMapRules` (`HindranceBlocks`, `LocationLos`), `ScenarioA1FireReference.Hardened`, the marsh halving in `ScenarioA1FireCalculator`, the beside-marsh Bog Check in `ScenarioA1VehicleTerrainCosts.EntryCost`; in Play, `GamePlanner.Rout.cs` (`InterdictionCover` and the callers) and where `IsAdjacent`'s facts are gathered.
- **Asked:** for each repair, a case the code reaches that the rule does not cover, or a case of the rule left out that the design does not list as left out. Whether the page supports, contradicts, or is silent on three readings: ADJACENT as a LOS and an Infantry step "in either direction"; a unit repulsed under A10.533 surrendering under A20.21 where A10.533's words give elimination; the surrender taken as the RtPh ends, with a unit bound to surrender owing no rout. Whether any new refusal, record, or plan a side confirms tells it of a concealed or hidden enemy unit before the rule reveals it (the repulse is the main risk). Whether Play decides a rule that should be a Rules verdict.

### 2. The referee, chapters C, D, and E (the design's sections 10 to 12)

Tasks 35.12, 35.10, 35.11; 35.13 a to j and the ESB table; 35.14. Rulings R35.7, R35.8, the second half of R35.9, and R35.10.

- **Pages to read at least:** A7.25 (55); C5.1, C5.11, C5.2, C5.34, C5.5 (171 to 172); C6.3, C6.5, C6.51, C6.6 (174 to 175); C10.1 (180); C13.1, C13.8 (183, 185); D1.321, D1.322 (194); D2.13, D2.16, D2.21 (196); D2.37, D2.38, D2.4, D2.5 (197 to 198); D5.2, D5.33, D5.34, D5.341 (203); D7.21 (207); D8.3, D8.4, D8.5 (209); D9.4 (210); E1.52 (224); E3.6, E3.64, E3.65 (229 to 230); E3.9 (231).
- **Members to read:** in Rules, `ScenarioA1VehicleTerrainCosts` (`EntryCost`, `BypassHalfMp`, `TowingBypassBar`), `ScenarioA1TerrainCosts` (`BypassStep`, `InfantryWeatherHalfMf`), `ScenarioA1Definitions.WoodsOrBuilding`, `ScenarioA1OrdnanceMapRules` (`IsBackblastLocation`, `InBogHex`), `ScenarioA1ArmorCalculator` (Case L, the Bypass fact and Case Q), `ScenarioA1OrdnanceCalculator` (`MoverDrm`, `OpportunityFire`, `AfphFire`), `ScenarioA1OrdnanceEligibility.OpportunityFirer`, `ScenarioA1VehicleSightRules` (`OwnHexHindrance`, `BypassHinders`), `ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn`, `ScenarioA1ResultTables.AcquiredVehicle`, `ScenarioA1VehicleProjection.BoggedMaySpend`, `ScenarioA1RecallCalculator` (`ExitHalfMp`, `StopsToUnload`), `RecallAbandoned`, `EsbBar`, `EsbNationalDrm`, `StopBar`, `ButtonUpBar`, `TurretBarredWhileCe`, the `StunRecovery` facts of the Overrun and the vehicle Close Combat, `ScenarioA1FireMapRules.FirerLocation` (the CC counter), `ScenarioA1SequenceCalculator` (`UnbuiltCounter`, `UnbuiltCounterBar`); in Units, `GameProjector.KeepAcquisitions`; in Play, the facts handed to each (the vehicle target's Non-Stopped, `ButtonedUpAfv`, the callers).
- **Asked:** the same first question, with this said: in the second session a repair reached one case too many (Case L denied against a tank still bogged); look for others of that kind. Whether the page supports, contradicts, or is silent on three readings: the weather's and the night's MP added after the Reverse multiplier (E3.9, D2.21, E1.52, C10.1); a LOS "touching" a Bypassed hexside read as passing through both hexes the hexside lies between, or starting or ending in the hex across it (D9.4); a vehicle freed by its Bog Removal being Non-Stopped from then on, and one still bogged or immobilized never (D2.13, D8.3, D8.4, C6.3). Whether the projector's loss of an Acquisition is exactly C6.5's condition, and whether it can drop a counter the rule keeps (a Gun that turns and then fires in the same phase; a firer that has not moved but whose holder changed). Whether Play decides a rule.

### 3. The table player

Reads what the game says to its players. The 52 test games are `p35-*.game.json` in `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/`, each with a `label` and its events; many hold the played events with their recorded facts and results. The sentences come from the planner's summaries and refusals (literals beginning "play." in Play and in Rules) and from `PlayRecords.cs`, `FireText.cs`, and `ReplaySteps.cs` in the Studio's Services. Give it the house style: plain language, a hex in brackets, a unit by its counter and tag, rules cited in parentheses, ASL's terms with their capitals, no internal identifier, no em-dash.

- **Asked:** every refusal, summary, consequence, and record sentence the pass added or changed (from the diff's string literals): one a player could not follow, that says the wrong thing by the rule it cites, cites the wrong number, leaves no way forward, or shows an identifier; and what it would write instead. In the played games, the arithmetic a player would check at the table (the DRM against the recorded facts, the Final DR, the result), with the event's revision for any that looks wrong, and a plain word when it judges from memory and not from a page. Any game whose label promises something its events do not show (`p35-acquire` is known and explained in section 14.3; not that one). Which repairs of sections 7 to 12 have no played game, and whether the design admits it.

### 4. The Rules boundary

Checks the user's ruling (the plan's section 19, decision 7): every ASL rule is a calculator in `LimboDancer.Domains.Asl.Rules` that takes facts and returns a verdict; LOS and range stay in Maps, read by Play and handed over as facts; a calculator whose decision is a search may take a fact reader declared in Rules and implemented in Play; Units references Rules so the projector calls the same calculators; Rules references only Abstractions; the Studio decides no rule. The diff in Rules, Play, Units, and the four changed Studio files.

- **Asked, in the added or changed lines only:** a rule decided in Play, Units, or the Studio (a DRM computed, a "may" or "must" decided, a table row chosen, an exception tested), as against fact gathering and event writing; a Rules member that is not a pure verdict over facts, a fact reader whose implementation decides, a reference from Rules to Units, Maps, or Play; one rule still written in two places, among the copies the design's section 5.3 says the pass merged; a new required positional parameter with no default on a record serialized into game events (section 14.1 says there is none); dead code the pass left (a planner-side helper its Rules replacement made unused).

### 5. The UI and Blazor review

`git diff f3b7c47..HEAD` over `src/ASL/LimboDancer.Domains.Asl.MapStudio` and `src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests`. Tell it the page is played hot-seat: each side has its own view, a view never reads what the other side holds in secret, and a hand-over clears everything the last view prepared. `Play.razor` is about 6,600 lines: the diff first, then only what it touches.

- **Asked:** for the vehicle Close Combat panel (`vehicleCcAttacker`, `vehicleCcLeader`, `vehicleCcVehicle`, `vehicleCcDefenders`, `ProposeVehicleCcAt`, `ClearVehicleCc`, its calls in `ResetPanelChoices` and in `Confirm`), any path on which a stale selection still reaches a proposal or leaves a button enabled: a proposal refused by the gate, one cancelled, a leader chosen as attacker and as SMC, a refresh caused by the other side's commit, the CC Reaction Fire draft cleared while a reaction is pending or on a phase change. **Other drafts in `Play.razor` with the same defect:** page state of a panel that `ResetDrafts`, `ResetPanelChoices`, `ResetForView`, or the game-open path does not clear, or that its own committed action does not clear; each field with its line and the reset that misses it. This is the question most worth its time: the defect fixed here passed many reviews. Whether the two new records (the repulse, which names the Location of a unit that has just lost its "?"; the Wound Severity dr) reach a view not entitled to them, by how `PlayRecords` filters its neighbours. Blazor correctness in the changed markup (`@key`, a select bound to a value its options may not hold, bool attributes, nullable flow, first render against re-render). For the new tests (`PlayPagePass35Tests`, and the tests added to `PlayPageVehicleTests`, `SetupPlansPageTests`, `CloseCombatComponentTests`, with the hill added to `WideBoards`): whether each asserts what its comment says, could pass for the wrong reason, or may be flaky in the Linux container; and what `NoPhaseOfThePageNamesAUnitTheViewDoesNotHold` would not catch.

## What the reviews do not cover

- They run nothing. The suites and the proofs are the gate's.
- The Armor Test card played through the Play page, which the design's section 5.4 proposed as the table player's review of the vehicle repairs, is not among them: it is a backlog row (section 55.2) that needs a session of its own. Ask the user before taking it up.

## To carry, from the third session

- **Three readings the rulings record as readings** (R35.3, R35.7 to R35.9): if a referee's finding overturns one, the ruling, the inventory's row, the design, and the code change together, and records made under the old reading fail their checks as `p35-acquire` does.
- **I committed the documents (5dd9934) without stopping for the user's word,** reading "do 1 through 3" as leave to go on. The user has not objected; do not repeat it. Stop before each commit.
- **Where each inventory row's remainder went** (passes 45 to 185) is my placement from the plan's pass titles; the plan's own table of rows by pass is left as approved, with a note. The user may move a row.
- **Three kinds of slip recurred in the third session:** a bare `python -`, an empty heredoc, and `sed -i` on a script. Look for them before sending a shell call.

## The tools and the games

In `E:\Archive\GitHub\dlandi\pass32-tools\pass35\session3\`: the game generators (`make_cc_panel.py`, `make_recombine.py`, `make_grain.py`), `replay_compare.py`, the documents' scripts (`docs_rulings.py`, `docs_inventory.py` and `docs_inventory_tables.py`, `docs_coverage_counts.py` and `docs_coverage_text.py`, `docs_backlog.py`; each was run once and asserts it is not run twice), `dump_pages.py`, and `pages\`. The replay digests of the third session are in `E:\Archive\GitHub\dlandi\pass32-tools\after-35-s3\`.

The third session's test games, in the live folder, untracked: `p35-cc-panel`, `p35-cc-panel2` (played through), `p35-recombine` (played), `p35-grain-nov`, `p35-grain-jul` (each with its one shot fired). The Studio for checks runs from `bin/p31b` on port 6671 (launch config `map-studio-p31b-6671`); build MapStudio into `bin/p31b` with warnings as errors first, with that Studio stopped.

## Start

Show `git status` and `git log -3 --oneline`, confirm the branch and that the tree is clean but for `src/ASL/boards` (and this prompt, if it is not yet committed), check that no Studio of the last session still runs on 6671, check that the page texts are in `pass35\session3\pages`, then launch the five review agents in one message and wait for all five. Report their findings together, most serious first, each with your own judgement of it (agree, doubt, or disagree, and why, after reading the lines it names) and a proposal: fix now, backlog, or no change. Then stop for the user.
