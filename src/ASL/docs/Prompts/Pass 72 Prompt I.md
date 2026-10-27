# Pass 72, first session

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; then `code-builders/README.md`. This prompt gives the state, the user's word, and what is open, and no procedure: where it and those files disagree, the files are right, and say so.

## The state, as of 2026-10-27

- **Main is at b0a36132** (pass 71's closing notes, straight to main at the user's word), on top of dc9cee0c, the merge of pull request 42 (pass 71). The merge's CI run on main (40405624167) is green, all seven jobs; the notes commit's run (40407647914) is green. The nightly Campaign run (40499073982) is green. `origin/main` was b0a36132 when this was written.
- **This prompt is not committed.** It sits in main's working tree as a new file and reaches main with the pass's branch.
- **The last rule pass is pass 71** (Infantry IV: Snipers and wounds), merged as pull request 42. Its record is `code-builders/pass-history/pass-71-snipers-wounds/notes.md`, closed on main. Its design and review are in `src/ASL/docs/Passes/` (Pass 71 Design; Pass 71 Review 2026-10-26). Ruling R71.1 kept it one pass, and nothing it left went to pass 72.
- **The working tree** is clean on main. On 2026-10-27, at the user's word, the pass 71 branch was deleted locally (the remote branch kept), the old stash dropped, and Codex's files moved out of the main checkout; the user: Codex works in its own worktree. The worktrees (`.claude/worktrees/replay-reenact-scope-9c9f9a` and Codex's five under `C:/Users/dland/.codex/worktrees`) touch nothing of this pass; `asl-narrative-extensions` is still checked out there.
- **The machine:** no Studio was listening on 6670, 6671, or 5178 when this was written; `outside_outputs.sh list` reported nothing left. The SDK is 10.0.403 by `global.json`, the lock at 10.0.14.
- **The proof games:** pass 71's games are now the previous pass's, pass 70's retire under the standing rule of 2026-10-25, and `dl-tractor-works-02` is still the swept game (the user's "you decide" in pass 71; `-03` is unfinished, to revisit at the next cut). Pass 72's games join them.
- **Two standing rules govern every test run,** in the standing rules' section 2: the Studio is the only test while code is written and fixed; and only the unit tests of the pass's new code run before the gate.

## The user's word

On 2026-10-27 the user said: "Update yourself on the recent commits (Pass 71) and then prepare to execute Pass 72." This prompt is that preparation. The mode is not given. Passes 55, 60, 65, 70, and 71 each ran without the user's intervention up to the merge gate, at the user's word for each, and that word ended at the gate. If the user's start does not say how the pass runs, ask before cutting the branch.

## Pass 72, as the plan has it

**Infantry V: Heat of Battle and Unit Substitution**, in the redesign plan (`ASL Card Play and Map Studio Redesign Plan.md`, section 22.1 (c), the entry for pass 72): two tasks, 1:40 of build and 1:15 of overhead, 2:55 in all. Needs first: 40 and 71, both merged (a berserk unit, a wounded SMC, and an Inexperienced unit change the same movement allowance). Counters: none. The inventory gives it 10 rows.

- **72.1 (was 75.2) Heat of Battle and berserk remainder:** a hero's weapons, the next nearest target, the return to normal (A15.23 p. 83; A15.43, A15.431, A15.432, A15.45, A15.46 p. 84).
- **72.2 (was 75.4) Unit Substitution:** Ammunition Shortage, an SSR ELR for underscored units, a MG's B# for Inexperienced users (A19.131, A19.132, A19.31, A19.32 p. 86).

Moved in with no estimate: a berserk charge into a Location holding an enemy vehicle and enemy Infantry (A15.431, A15.432; pass 70, backlog row "Pass 72 (task 72.1)"), with 72.1.

What the inventory rows say is missing, in short:

- **A15.23:** a hero firing a LATW, a light mortar, or a Gun is refused (R15.11, backlog); the AAMG as a Rider and the Gun rules of an 82mm and a CA change are not built.
- **A15.43, A15.45:** a charge the model cannot decide ends in place (R30.5), for a Gun crew's Location (R27.2), a vehicle's Location that also holds enemy Infantry, and a target it cannot reach; A15.45's fall back to the next nearest Known enemy and A15.44's fall back when there is none are missing.
- **A15.431:** a unit made berserk in mid move charging with the rest of its MF was not found; Dash, Wire as 1 MF, and DC Thrown only are absent.
- **A15.432:** marked "not verified": the DEFENDER's forced FPF and the halved AFPh TPBF in the charged Location; a comment in `GamePlanner.Fire.cs` calls berserk fire in the AFPh and DFPh not reviewed.
- **A15.46:** the return to normal after AFPh TPBF or a FT clears the Location, and after destroying a vehicle in CC.
- **A19.131:** no Ammunition Shortage state or SSR token at all.
- **A19.132:** R15.9 always applies the underscored exception; a card cannot give underscored units an ELR of 4 or less.
- **A19.31:** the exception for a Player Turn of being carried, or of mounting or dismounting a conveyance, was not checked against `GamePlanner.Passengers.cs` or pass 71's carry; Human Wave is not built.
- **A19.32:** R9.10, a deviation: a MG keeps its printed B# in Inexperienced hands, except when fired as ordnance at an AFV (pass 60).

**It is likely larger than the plan says,** as every pass since 55 has been. Pass 71 is the guide: its frame forecast 5:41 of building against the plan's 1:20, and the build took 5:29 by the clock (08:42 to 14:11), and the gate passed at 17:13.

## Open, and known before the start

Each with the proposal the analysis gives, for the frame to settle (the standing practice of 2026-10-06: a decision, never a bare question).

- **The next nearest target replaces "ends in place".** R30.5 covers several cases; A15.45 says where the charge goes instead. Proposal: the charge calculator (`ScenarioA1ChargeCalculator`) takes the next nearest Known enemy it can reach, and A15.44's fall back when there is none; a Gun crew's Location (R27.2) stays as it is only if CC with a crew is still not built (backlog row at line 940 names pass 110), with that reason in the ruling.
- **Ammunition Shortage and the underscored ELR are card facts.** No card sets either today. Proposal: a side's SSR facts on the card, read by the fire, ordnance, and ELR calculators as facts; games recorded before the pass replay as they were, since no card holds them; one small test card per fact for the Studio game. Ammunition Shortage reaches the Original 12 IFT DR, B#, X#, circled B#, and Fire Lanes: the frame reads which of those packages exist and moves the rest with a row. Pass 73's A8.221 (a malfunctioned weapon's Residual FP) reads Ammunition Shortage, so the fact's shape should serve it.
- **Lifting R9.10 changes older games.** A MG fired by an Inexperienced unit breaks down on one number lower. Proposal: the frame finds whether any proof game or the swept game holds such a fire; if so, the gate's differing lines are this task's intended change and are named as such, and the old outcome is not kept for replay unless the frame finds the recorded events would not replay.
- **A hero's other weapons sit on other passes' code.** LATW (passes 50 to 60), light mortars, and Guns. Proposal: read which packages take a SMC firer today; build the hero for those that do, move the rest with a row.
- **A berserk charge's cost.** The charge search was the 11 s pause of Turn 5 in `dl-tractor-works-02` at revision 1316 (fixed in PR 32, memoized through `GamePlanner`'s `MemoOf(state)`). A fall back to a next nearest target adds searches. Proposal: the fall back reuses the same memo and searches only when the first target fails; measure the charge there first, in the Studio, before and after.
- **The A15.432 "not verified" items** are reads, not builds: the frame reads them on p. 84 and in code, and builds only what is missing.
- **Rulings still open to a veto:** R71.1 to R71.28, R70.1 to R70.48, R65.1 to R65.33, and R45.3 to R45.5. None has a recorded approval in the Passes Plan.

## Memory and performance

The pass follows the [ASL Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>); the design answers its checklist, and each task is checked against its contracts before it is done. Where this pass is most likely to add work:

- **The charge search** (above): read the berserk units, the Known enemy, and the routes once per state through `MemoOf(state)` and `PlayStepMemo`; never a per-unit scan through `GameState.At`.
- **Measurement.** Passes 70 and 71 measured the hand-over and an "end the phase" proposal on `p70-perf-tw` (`dl-tractor-works-02` cut at revision 1316, `perf_cut.py`, `perf_measure.js`); pass 71's whole-pass check found it level with main. Name the measurement in the design, add the berserk charge's MPh at the same cut, and take it in the Studio first, before the costly tests.

## Start

Read the standing rules and the tools' README. Check the state above against the repository before relying on it, main first. Then start as the user's word says, or ask how the pass is to run if it does not.
