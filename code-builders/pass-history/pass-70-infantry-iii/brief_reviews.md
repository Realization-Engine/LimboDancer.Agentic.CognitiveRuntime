# The reviews of pass 70

Six read-only reviews of the branch `feature/asl-backlog-pass-70` against its base 25aa97fd, run in parallel in the main checkout.

**What a reviewer may do.** Each reviewer reads, and never edits, stages, builds, or runs tests.
- It may run `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP diff 25aa97fd..HEAD -- <paths>`.
- It may read files and the rulebook's page texts (`E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`, one physical page a file). A chart's columns are scrambled in the text, so trust the values the design and the notes give from renders.
- Never `cd` into the repository. Never `grep -r` over src/ASL in Bash: use the Grep tool, pointed at `src/ASL/LimboDancer.*` folders and `src/ASL/tests`. **Never search or open anything under `src/ASL/boards` except the `p70-*.game.json` files the table player reads;** the user's live game `dl-tractor-works-03.game.json` is being played in the user's own Studio: never touch any `dl-tractor-works-*` file.

**The report** is the closing message, under 1,800 words: findings numbered, each with
- its grade (BLOCKER, MAJOR, MINOR, NOTE),
- the file and member, with a line,
- the rule and its page,
- what is wrong,
- and a proposed fix in one or two sentences.

**Read first:**
- the design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 70 Design.md`, sections 2 to 7. The rulings R70.n bind the build (R70.35 to R70.40 were added during the build); a finding against a ruling says so and argues from the page;
- the session's notes, `code-builders/pass-history/pass-70-infantry-iii/notes.md`, which list what each increment left out and decided; and `backlog_rows.md` beside it (those are known: do not report them again unless they are wrong);
- the code map, section 4 of `frame_search_codemap.md` beside it.

**Every reviewer also answers:**
- Is any rule decided outside `LimboDancer.Domains.Asl.Rules` in what you read?
- Does any fact added to a record change what an older record replays to (absent-safety)?
- Does any text a side reads (a proposal, a refusal, a record line, a list, a panel, a map layer) name or count a unit it may not know: a concealed or hidden unit, a Dummy's nature, an uninspected stack's contents, a declined "?" choice?

## R1. Referee: morale and rout (tasks 70.1, 70.2; increments 3b, 3c, 3d)

**Rules to read on their pages, with their examples:** A10.2 (p. 65); A10.3, A10.41, A10.5, A10.51 and its exception (pp. 66, 67); A10.53, A10.531, A10.532, A10.533 (p. 67, 68); A10.62 (p. 68); A10.7, A10.71, A10.711, A10.72, A10.8 and the examples on p. 69; A15.1 and its table (p. 83); A17.1, A17.11 (p. 85); A19.11, A19.12, A19.13 (p. 86); A7.83 (p. 58); D3.44 (p. 200); D5.311 (p. 203); D6.24 (p. 205); D6.651 (p. 206).

**Code:** `ScenarioA1MoraleCheck`, the check-only branch of `ScenarioA1FireCalculator` (`ResolveCheck`, `RunCheck`, `LeadershipReaches`, `LeaderLossReaches`, `NeverDisrupted`, the Unarmed and `crewBroken` paths), `ScenarioA1HeatOfBattle`, `ScenarioA1RiderRules`, `ScenarioA1VoluntaryRout`, the rout calculator's new members (`InterdictionReach`, `VehicleNormalRange`, `VehicleInterdiction`, `GunNormalRange`, `GunArcWeapon`, `BuildingGoesOn`, `RoutTargets`), `ScenarioA1GameStart.FanaticBySsr`; Play's `GamePlanner.MoraleChecks.cs`, `GamePlanner.VoluntaryRout.cs`, `GamePlanner.Rout.cs`, `GamePlanner.Riders.cs`.

**Look hardest at:**
- every check the member now makes outside fire: who checks, in what order, with which DRM, and whether a LLMC reaches the right units (A10.2's exceptions);
- the Voluntary Break's eligibility and its refusals;
- a vehicle's Interdiction: when its weapons reach, and when a positive DRM bars it;
- the case of `p70-rout-own-building`: a broken squad on level 1 of [M5] with a Known Russian squad on [M5]'s ground level is told "No woods or building is within its reach, so any legal route will do". Is that A10.51's answer, with its ADJACENT clauses and the starting-building exception? Read the rule and say what the routing unit may do.

## R2. Referee: who sees and hidden information (tasks 70.3, 70.4, 70.5, 70.8, 70.9, 70.10; increments 3a, 3e, 3f, 3g)

Read every change **from each side's view**, as the side that must not see.

**Rules to read on their pages:** A.7 (p. 43); A12.1 to A12.16 (pp. 76 to 79; the Concealment Table on p. 107, from the notes' render findings); A12.2, A12.3, A12.31 to A12.34, A12.4 to A12.42 (pp. 79, 80); A4.134 (p. 48); A4.3, A4.31, A4.33 (Bypass); A8.312 (Mandatory Fire); A11.12, A11.14, A11.15, A11.19 (pp. 72, 73), for R70.40.

**Code:** `ScenarioA1Sight`, `ScenarioA1Visibility`, `GameView`, `GamePlanner.Sight.cs`, `ScenarioA1ConcealmentChoices`, `GamePlanner.ConcealmentChoices.cs`, `ScenarioA1BypassDetection`, `GamePlanner.Bypass.cs`, `ScenarioA1Searching`, `GamePlanner.Searching.cs`, `GamePlanner.Buildings.cs`, `ScenarioA1MoppingUp`, the gain path (`ScenarioA1FireFollowUps.ConcealmentGains`), the setup's concealment and HIP checks, and every event's `visibility`.

**Look hardest at:**
- each read of who sees against its purpose (Loss, GainDenial, Inspection, LossInView): who counts as a seer, and how a LOS read is taken;
- the Right of Inspection's view: what an uninspected stack shows and withholds (its equipment beneath, its count), in every place a view is read;
- the viewer's option (R70.21): does the mover learn anything from the wait, the refusal while it is pending, the record, or the page? The choice events' visibility;
- the A12.141 "?" losses: which actions lose it and whether the exceptions stay free;
- Searching's reveal and its public drm (R70.39); Mopping Up's hidden placements;
- the forced backs and reveals of Bypass, and the hidden units that stay hidden.

## R3. Referee: Close Combat (tasks 70.6, 70.7; increments 3h, 3i, and 3j if it is committed when you start)

**Rules to read on their pages:** A1.32 (p. 45); A11.1 to A11.8 with every example (pp. 72 to 76); A18.12 (p. 85); A21.2 (pp. 88, 89); D5.42 (p. 204); D6.5, D6.24 (p. 205); D6.82 (p. 206); A7.211 (p. 55).

**Code:** `ScenarioA1Withdrawal`, the CC calculator's Gun loss (A11.13), the CC package's lifted crew refusals, the sequential round and Ambush with a vehicle, `ScenarioA1PassengerActionRules.CloseCombat` as wired, `ScenarioA1VehicleCloseCombat`, `ScenarioA1VehicleCloseCombatRules`, the projector's `InMelee`, `ScenarioA1CarrierRules`; Play's `GamePlanner.Withdrawal.cs`, `GamePlanner.CloseCombat.cs`, `GamePlanner.VehicleCloseCombat.cs`, `LiveCloseCombat.cs`, `CloseCombatEffects`.

**Look hardest at:** the order of attacks in a Location with a vehicle (A11.31 and the H3 example); each DRM of an Ambush and of vehicle CC; who is held in Melee after the CCPh; a withdrawal into a concealed or hidden enemy's Location against the p. 74 example; the Gun's loss dr.

## R4. Table player

Play the pass's games by reading their records, as a player at the table would.
- **The games:** `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/p70-*.game.json` (read the events; `p70-perf-tw` is a measurement copy, skip it; never touch a `dl-tractor-works-*` file).
- **The record lines** the Studio writes for them (`Services/PlayRecords.cs`, `Services/ReplaySteps.cs`, and the texts in Rules they read).
- **The proposal texts** in the planner and Rules.

Ask: would a player understand what happened and why, from what each side's view shows? Is a refusal's advice right? Is a result the rule's, on its page? Is anything shown to the side that should not see it? Name the game, the event, and the text.

Known, to be fixed and not reported again (the notes): the Records tab's list leaves out the LLMC line; the Interdiction's Heat of Battle line doubles its citation; the viewer's choice text ends "(ruling R5.8)." after R70.21's citation; the Search's Latest line leaves out the Casualty dr; a forced back's line reads "moves from [F5] to [F5]".

## R5. UI and Blazor

**What to read:** the page changes of the pass, `git diff 25aa97fd..HEAD -- src/ASL/LimboDancer.Domains.Asl.MapStudio`.

**The new controls:** the Rout panel's Voluntary Break list (`input.voluntary-break-unit`, `#propose-voluntary-break`) and `#rout-with-leader`; the "Give up "?"" list (`#play-remove-concealment`, `.remove-concealment-unit`, `#propose-remove-concealment`); the viewer's choice buttons; the Search box (`SearchAction.razor`, `input.search-hex`, `#propose-search`); the withdrawal and infiltration drops (`.cc-drop`, `.cc-infiltration-drop`); the CC panels' changes of 3i and 3j; the units table's uninspected rows; the setup editor's `#place-concealed` title; the Mopping Up box for any building.

**Check:**
- **The hex UI rule:** no choice names a counter without its hex, hex first; the logic of `code-builders/play-ui/hex_audit.js` by reading the markup. 3e found that `UnitSelectionList` and `UnitGrouping` show no hex for a one-unit list: confirm and say where it bites.
- **Labels:** a side's own labels, never an id.
- `@key` on lists that shift; state cleared on a change of game or view (the viewer's choice above all).
- **The narrow layout:** under 1024 pixels, the Actions tab.
- A disabled control's `title` as its reason.
- Every per-render read: a state scan per unit in a render is a fault under the Memory and Performance Guidelines.
- The page tests' coverage of each new control.

## R6. The Rules boundary, and memory and performance

**What to read:** `git diff 25aa97fd..HEAD -- src/ASL/LimboDancer.Domains.Asl.Play src/ASL/LimboDancer.Domains.Asl.Units src/ASL/LimboDancer.Domains.Asl.MapStudio code-builders/proof-tool`.

**Look for:**
- every decision of a rule made outside Rules (a threshold, a table, a condition on a unit's kind, a DRM);
- every fact Play computes that Rules should;
- every fact the verifier recomputes from the state that changes an older record's result (it must be taken from the record);
- every new JSON property that is not `JsonIgnore` when null or default.

**Also, against `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`:**
- every `GameState.At`, `Aboard`, `Passengers`, `Location`, `Find`, `Unit`, or `Units` scan inside a loop or per render that the pass added, where `GamePlanner.MemoOf(state)` and its `ObjectIndex` should serve;
- every new memo store and the building map per board handle, with its owner, key (side, purpose, viewer), lifetime, and invalidation;
- the view built through `GamePlanner.ViewOf` in `UnitNames` over every revision of a history: its cost per revision;
- the proof tool's reflection binding of `ViewOf` (the gate depends on it).

List each finding with its member and line, and where it belongs.
