# The reviews of pass 65

Five read-only reviews of the branch `feature/asl-backlog-pass-65` against its base d0a8f4cd, run in parallel in the main checkout.

**What a reviewer may do.** Each reviewer reads, and never edits, stages, builds, or runs tests.
- It may run `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP diff d0a8f4cd..HEAD -- <paths>`.
- It may read files and the rulebook's page texts (`E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`, one physical page a file). A chart's columns are scrambled in the text, so trust the design's values, which were read from renders.
- Never `cd` into the repository. Never `grep -r` over src/ASL in Bash: use the Grep tool.

**The report** is the closing message, under 1,800 words: findings numbered, each with
- its grade (BLOCKER, MAJOR, MINOR, NOTE),
- the file and member, with a line,
- the rule and its page,
- what is wrong,
- and a proposed fix in one or two sentences.

**Read first:**
- the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 65 Design.md`, sections 2 to 6. The rulings R65.n bind the build; a finding against a ruling says so and argues from the page;
- the session's notes, `code-builders/pass-history/pass-65-passengers-riders-crews/notes.md`, which list what each increment left out (those are known: do not report them again unless they are wrong);
- the code map, `codemap.md` beside it.

**Every reviewer also answers:**
- Is any rule decided outside `LimboDancer.Domains.Asl.Rules` in what you read?
- Does any fact added to a record change what an older record replays to (rulings R65.5 and following: absent-safety)?
- Does any text a side reads name a unit it may not know, such as an enemy Passenger, Rider, or crew inside a vehicle?

## R1. Referee, first half: Passengers (tasks 65.1, 65.2, 65.3; increments 2b, 2c, 2d, and 2k)

**Rules to read on their pages, with their examples:**
- D.6, D.8, D.8A, D.8B and the Collateral Attack Table (pp. 192, 193);
- D5.31, D5.311 (p. 203); D5.43 (p. 204);
- D6.1 to D6.72 (pp. 204 to 206); D7.11, D7.12, D7.16 (p. 207); D9.1 (p. 209);
- A4.6 (p. 51); A4.11, A4.42, A4.5 (pp. 48, 49); A7.211, A7.308, A7.309 (pp. 55, 56); A7.82, A7.821 (p. 58); A11.611, A11.71 (pp. 75, 76);
- A1.123 (p. 44), for 2k's crew as a target and in CC.

**Code:** `ScenarioA1PrcRules`, `ScenarioA1PassengerActionRules`, `ScenarioA1PassengerCalculator`, the PRC paths in `ScenarioA1FireCalculator` and `ScenarioA1ArmorCalculator`, and their callers in Play (`GamePlanner.PassengerTargets.cs`, `GamePlanner.PassengerActions.cs`, `GamePlanner.Passengers.cs`).

**Look hardest at:**
- every DRM line an attack on PRC now reaches (pass 50's lesson: a rule a pass opens is reached by every old line beside it);
- the FP each PRC attack uses (halved or not);
- when Passengers are BU or CE;
- the Survival roll's DRM.

## R2. Referee, second half: Riders, Carriers, crews, captures, wrecks (tasks 65.4 to 65.9; increments 2a, 2e to 2j)

**Rules to read on their pages:**
- D5.1 to D5.8 (pp. 203, 204); D6.2 to D6.24 (p. 205); D6.631, D6.8 to D6.84 (pp. 206, 207);
- D3.43 (p. 200); D8.11, D8.22, D9.2 (pp. 208, 209); D10.1, D10.5 to D10.52 (pp. 210, 211);
- B13.43 (p. 128); A21.2 to A21.22 (pp. 88, 89); A26.12 (p. 99); A9.8 (p. 65); C1.55 (p. 165).

**Code:** `ScenarioA1RiderRules`, `ScenarioA1CarrierRules`, `ScenarioA1CrewActsRules`, `ScenarioA1CrewSurvivalRules`, `ScenarioA1CapturedVehicleRules`, `ScenarioA1WreckRules`, `ScenarioA1Definitions.IsCrew` and `LeavingCrew`, and their callers in Play.

**Look hardest at:**
- the catalog's eight vehicle crews against the chart values in the design's R65.3;
- the Rider dates and capacities;
- the TC's second cause;
- the Survival DRM over every path;
- the start dr and its colored die;
- every place a crew leaves a vehicle: is it the right counter, and does it carry the right fire counter and leader?

## R3. Table player

Play the pass's games by reading their records, as a player at the table would.
- **The games:** `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/p65-*.game.json` (read the events, not the board folder's other files; `p65-perf-tw` is a measurement copy, skip it).
- **The record lines** the Studio writes for them (`Services/PlayRecords.cs`, `Services/FireText.cs`, `Services/ReplaySteps.cs`).
- **The proposal texts** in the planner.

Ask:
- Would a player understand what happened and why, from what each side's view shows?
- Is a refusal's advice right?
- Is a result the rule's, on its page?
- Is anything shown to the side that should not see it?

Name the game, the event, and the text.

Two known faults, to be fixed and not reported again:
- a unit the plan creates is named by its raw id in its proposal (2f's crew unload, 2g's Abandonment);
- the vehicle CC panel's and the ordnance and FT/MOL panels' vehicle choices are not hex-first.

## R4. UI and Blazor

**What to read:** the page changes of the pass, `git diff d0a8f4cd..HEAD -- src/ASL/LimboDancer.Domains.Asl.MapStudio`.

**The new controls:**
- `#vehicle-load-riders`, `#place-rider`, `#vehicle-unload-crew`, `.vehicle-leave`;
- `button.rout-beneath`, the "Vehicle crews" section (`VehicleCrewActions.razor`), the "Scrounging" section (`VehicleScroungeActions.razor`);
- `.propose-vehicle-cc-capture`, and the fire panel's Passengers.

**Check:**
- **The hex UI rule:** no choice names a counter without its hex, hex first; run the logic of `code-builders/play-ui/hex_audit.js` by reading the markup.
- **Labels:** a side's own labels, never an id.
- `@key` on lists that shift.
- State cleared on a change of game or view.
- **The narrow layout:** under 1024 pixels, the Actions tab.
- A disabled control's `title` as its reason.
- Every per-render read: a state scan per unit in a render is a fault under the Memory and Performance Guidelines.
- The page tests' coverage of each new control.

## R5. The Rules boundary, and memory and performance

**What to read:** `git diff d0a8f4cd..HEAD -- src/ASL/LimboDancer.Domains.Asl.Play src/ASL/LimboDancer.Domains.Asl.Units src/ASL/LimboDancer.Domains.Asl.MapStudio`.

**Look for:**
- every decision of a rule made outside Rules (a threshold, a table, a condition on a unit's kind, a DRM);
- every fact Play computes that Rules should;
- every fact the verifier recomputes from the state that changes an older record's result (it must be taken from the record);
- every new JSON property that is not `JsonIgnore` when null or default.

**Also, against `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`:**
- every `GameState.At`, `Aboard`, `Passengers`, `Location`, `Find`, or `Units` scan inside a loop or per render that the pass added, where `GamePlanner.MemoOf(state)` and its `ObjectIndex` should serve;
- every new cache, with its owner, key, lifetime, and invalidation;
- every event-history scan per plan that grows with the game.

List each finding with its member and line, and where it belongs.
