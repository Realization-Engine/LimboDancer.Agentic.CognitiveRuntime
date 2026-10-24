# Pass 65, first session

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; then `code-builders/README.md`. This prompt gives the state, the user's word, and what is open, and no procedure: where it and those files disagree, the files are right, and say so.

## The state, as of 2026-10-23

- **Main is at 4702d90c** (pull request 32, the berserk charge fix), green in CI. The last rule pass is pass 60 (Armored combat III), merged as 9ce72e29 and closed in 9def98a9; its rulings R60.1 to R60.27 were approved on 2026-10-15. Since then the work was Play and performance (pull requests 18 to 32), none of it a rule pass.
- **The branch `vehicle-crew-figure` (a30f5a5c) is one commit past main and not pushed.** It holds the vehicle crew's figure and kind, and the corrected pass 65 entry of the redesign plan. It adds a file, so it reaches main by a pull request and a green CI run. Pass 65 is cut from main once it is merged, or from it if the user says so.
- **What that branch gives pass 65:**
  - the kind `asl:vehicle-crew`, extending `asl:crew` (vocabulary asl@1.18.0; A1.123, D5.1), drawn in its own pose by both style sheets;
  - the accepted pose `vehicle-crew`, from the user's ChatGPT pick: a tank crewman crouching with a pistol, keeping his own padded helmet for every side (Figure Art Plan, decision F17, the user's ruling: tanker headgear per nationality is not worth drawing);
  - the Studio example `example-vehicle-crew`, which is synthetic, not a catalog definition.
- **The catalog is `asl-scenario-a1` 1.17.0** and has no vehicle-crew definition. Its four `asl:crew` definitions (attacker, defender, British, American) are infantry crews (2-2-8 or 2-2-7). When a vehicle's crew survives today, Play places that nationality's infantry crew (`GamePlanner.Ordnance.cs`, `CrewCounter`). That gives the survivors FP 2 and elite morale, where D5.1 gives them FP 1.
- **The vehicle-crew values are printed, not manufactured.** The A./G. National Capabilities Chart (physical page 695) lists each nationality's infantry crew and vehicle crew in its left column. Read from the text layer on 2026-10-23, so not yet confirmed: German, British, and Finnish 1-2-7, broken 7, BPV 7; Russian 1-2-7, broken 7, BPV 5; American 1-2-6, broken 7, BPV 6; Italian 1-2-6, broken 5, BPV 5; French 1-2-7, broken 6, BPV 5. Axis Minor was not read. The text layer drops superscripts, so the broken morale needs a rendering of the chart. The catalog design's section 9 says how printed values are taken and reviewed.
- **Carrier and Rider need data, not counters** (the plan's entry, corrected 2026-10-23):
  - A Carrier's inherent crew is a 2-4-7 or 2-4-8 HS (D6.82), and those counters exist. The only Carrier in the catalog is `campaign-british-bren-carrier`, a game-default vehicle on sheet MFG.
  - Rider capacity goes by vehicle class (D6.2, D6.81), and no vehicle carries a class today.
- **The tools and the gate** are as the standing rules and `code-builders/merge-gate/gate.sh` describe them: local proofs, then a green CI run on the pass's pull request.
- **Two standing rules govern every test run of this pass,** both in the standing rules' section 2: the Studio is the only test while code is written and fixed, and no unit test runs before the Studio check of the whole pass has passed; and the rule for unit tests, in the user's words of 2026-10-23: "Only unit tests pertaining to the new code in the pass must be exercised. The full unit test run waits until the gate." It has no exception; past passes lost hours to runs it now forbids.

## The user's word

On 2026-10-23 the user said: "let's prepare to execute Pass 65." This prompt is that preparation. The start is not given, and neither is a mode (pass 55 and pass 60 each ran without the user's intervention up to the gate, at the user's word for each). Cut no pass branch and write no frame until the user says to start, and ask how the pass is to run if the start does not say.

## Pass 65, as the plan has it

**Passengers, Riders, and crews**, in the redesign plan (`ASL Card Play and Map Studio Redesign Plan.md`, the entry for pass 65): 63 inventory rows, nine tasks (65.1 to 65.9), 7:00 of build and 1:15 of overhead, 8:15 in all. Passes 55 and 60 moved more items under the entry without renumbering a task, and the backlog names pass 65 in about a dozen more rows (search it for "Pass 65"). Expect it to run larger than the plan, as passes 55 and 60 did.

## Open, and known before the start

- **The branch above** waits on its pull request.
- **The vehicle-crew values** wait on the chart's rendering and the referee's check before they enter the catalog. Adding definitions makes catalog 1.18.0, and games that name 1.17.0 then stop replaying, as at every catalog bump before.
- **Recorded games** where a crew survived hold an infantry crew. If the pass places a vehicle crew in its place, the replay digest will show it: keep what an older game recorded by a fact on the record, as earlier passes did.
- **Rulings still open to a veto:** R40.12 to R40.26 and R45.3 to R45.5.

## Memory and performance

Pass 65 is the first rule pass under the [ASL Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>) (the user, 2026-10-21: every change follows them; the standing rules cite them). The design answers the guidelines' checklist, and every implementation task is checked against its contracts before it is done. Where this pass is most likely to add work:

- **Units aboard vehicles.** Passengers, Riders, and Carrier crews give each vehicle a set of units that every attack, Collateral Attack, Bail Out, and Survival check reads. A per-unit or per-step lookup of that set through `GameState.At` repeats the cost found in the berserk charge search (pull request 32, 11 s at revision 1316 of `dl-tractor-works-02`). Read it once per state through `GamePlanner`'s `MemoOf(state)` and `PlayStepMemo` (the guidelines, section on memoization), as the charge search's index of the state's objects now is.
- **The Play page and the counters.** A vehicle's stack now shows the units aboard it. New counters go through the incremental counter layer and its shared outlines (pull requests 19 and 22), not a page-wide redraw.
- **Measurement.** Name the regression measurement in the design, and take it in the Studio before the costly tests.

## Start

Read the standing rules and the tools' README. Check the state above against the repository before relying on it, main first. Then wait for the user's word.
