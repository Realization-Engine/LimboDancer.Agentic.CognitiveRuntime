# Pass 71, first session

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; then `code-builders/README.md`. This prompt gives the state, the user's word, and what is open, and no procedure: where it and those files disagree, the files are right, and say so.

## The state, as of 2026-10-26

- **Main is at 5379e06f** (pull request 41: pass 75 split into passes 71 to 75, and pass 70's closing notes). Its pull request's CI run (40318564426) is green, all five jobs. The CI run of the merge on main (40319428909) is green, all five jobs. The run before it, Codex's acronym scan (920eacc4), is green.
- **This prompt is not committed.** It sits in main's working tree as a new file and reaches main with the pass's branch.
- **The last rule pass is pass 70** (Infantry III: morale, Close Combat, and concealment), merged as pull request 40 (e762ac7c, 2026-10-26), main's CI green. Its record is `code-builders/pass-history/pass-70-infantry-iii/notes.md`, now closed on main (06e57e60). Its rulings R70.1 to R70.48 are open to the user's veto.
- **Pass 75 is now five passes** (the redesign plan, section 4, "Split of 2026-10-26", at the user's word). Pass 71 is the first: Snipers and wounds. The plan's section 4 maps the old task numbers (75.1 and 75.3 are now 71.1 and 71.2). Documents written before 2026-10-26 use the old numbers.
- **The working tree** holds `.codex/` (the Codex session's, unreadable from here: leave it) and this prompt. `stash@{0}` holds the pass 70 branch's last uncommitted state from the app's branch switch: the notes lines (now committed), the acronym files (now on main, identical), and `.codex/hooks.json`. It is kept for the user to drop; it touches nothing of this pass. The worktrees (`.claude/worktrees/replay-reenact-scope-9c9f9a`, and Codex's `asl-acronym-review`, `asl-narrative-extensions`, `formation-design-review`) touch nothing of this pass.
- **The machine:** Visual Studio's installer updated it on 2026-10-26. The SDKs are now 10.0.114, 10.0.402, 10.0.403, and 11.0 rc.3 (among older ones); the repository's `global.json` picks 10.0.403. No Studio was listening on 6670, 6671, or 5178 when this was written. `outside_outputs.sh list` reported nothing left.
- **The proof games:** pass 70's games are now the previous pass's, and `dl-tractor-works-02` is swept at two cuts a phase (the standing rule of 2026-10-25). Pass 71's games join them.
- **Two standing rules govern every test run,** in the standing rules' section 2: the Studio is the only test while code is written and fixed; and only the unit tests of the pass's new code run before the gate (the user, 2026-10-23).

## The user's word

On 2026-10-26 the user asked to split pass 75 into five passes, approved the split as written (merged as pull request 41), and then said: "Let's start a new session. Write the kick-off prompt for Pass 71." This prompt is that kick-off. The mode is not given. Passes 55, 60, 65, and 70 each ran without the user's intervention up to the merge gate, at the user's word for each, and that word ended at the gate. If the user's start does not say how the pass runs, ask before cutting the branch.

## Pass 71, as the plan has it

**Infantry IV: Snipers and wounds**, in the redesign plan (`ASL Card Play and Map Studio Redesign Plan.md`, section 22.1 (c), the entry for pass 71): two tasks, 1:20 of build and 1:15 of overhead, 2:35 in all. Needs first: pass 40, merged. Counters: none. The inventory gives it 11 rows.

- **71.1 (was 75.1) Snipers completed:** the counter at setup, placement, the Sniper player's choices, attacks on vehicles and on the enemy Sniper, Sniper Check, LLMC after a hit (A14.01, A14.1, A14.2, A14.21, A14.22, A14.3, A14.31, A14.33, A14.4; all p. 82).
- **71.2 (was 75.3) Wounds and Field Promotions:** the pin after more than 3 MF, a carried man; Field Promotions' remainder (A17.2, A18.2; p. 85).

Moved in with no estimate (the plan's paragraph after the table):

- Stun of an AFV by a Sniper (D5.34, from pass 65), with 71.1.
- The voluntary removal's Sniper exception, between a Sniper's Random Location DR and its relocation (A12.14, A14.2; pass 70, backlog 72.1), with 71.1.
- The SMC crew's Stun wound in the projector: no Wound Severity dr, and a second wound eliminates (D5.34, A17.11; pass 70, backlog 72.1), with 71.2.

The backlog's older Sniper rows are in section 25 (added by backlog pass 15): Sniper attacks from DRs outside fire records (To Hit, PAATC, Entrenching, checks in the RPh and RtPh); repositioning by forfeiting an attack; the setup placement and SAN changes; the Sniper player's choices of Location and of equidistant hexes; LLMC after a Sniper hit. Section 26 has the captured MG's Sniper dr. Section 55's Stun row still names "Passes 65 and 105" for Stun by a Sniper; it predates the split.

**It is likely larger than the plan says,** as every pass since 55 has been. Pass 70 is the guide: its frame estimated 33:05 of build against the plan's 7:20, and the build took 8:07 by the clock. The pass's whole session ran about 12 hours to the gate.

## Open, and known before the start

Each with the proposal the analysis gives, for the frame to settle (the standing practice of 2026-10-06: a decision, never a bare question).

- **Which Sniper triggers are in the pass.** A14.1's row lists To Hit, Entrenching, PAATC, and the other DRs outside fire records as missing, and backlog 25 calls that "a roll hook in every package". 71.1's text does not name them. Proposal: the frame reads which packages roll those DRs today and decides whether they fit the pass or move to the backlog with a row; this is the one item that could double the pass.
- **The Sniper player's choices replace a deviation.** Ruling R15.5 has the game pick the Location, the tied target, and the alternate hex. Making them the owner's adds a pending choice on the other side's roll, with what each side may see while it waits. Proposal: follow pass 70's pending-choice pattern for the viewer's option on a "?" (`ScenarioA1ConcealmentChoices`, `GamePlanner.ConcealmentChoices`); the referee's review reads it from each side's view.
- **A SAN that changes, and a counter given at setup.** Sniper Check (A14.4) lowers a SAN and can remove the counter; today the SAN is the card's, read whole. A14.01's row says a side with a SAN of 2 or more is never given its counter, so it silently makes no Sniper attacks. Proposal: the SAN and the counter become recorded state, and games recorded before the pass replay as they were; a game whose side had a SAN but no counter is the case to decide, since giving it the counter now would add Sniper attacks to its replay.
- **New Sniper targets meet earlier passes' rules.** Unarmored vehicles (A14.33), Vulnerable PRC and CE crews (A14.22), an AFV's Stun (D5.34), and the enemy Sniper (A14.31) sit on pass 60's and pass 65's code. Proposal: read pass 65's PRC facts (`ScenarioA1PassengerActionRules`) before writing a target rule of its own.
- **LLMC after a Sniper hit** (A14.3, backlog 25): pass 70 made one Morale Check member for every check outside fire (increment 3b). Proposal: reuse it, not a second path.
- **A17.2's "4 MF on a conveyance"** may need conveyances that later passes build (165, 175). Proposal: the frame reads p. 85 and moves what has no unit to carry it, with a backlog row.
- **Rulings still open to a veto:** R70.1 to R70.48, R65.1 to R65.33, and R45.3 to R45.5. None has a recorded approval in the Passes Plan.

## Memory and performance

The pass follows the [ASL Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>); the design answers its checklist, and each task is checked against its contracts before it is done. Where this pass is most likely to add work:

- **A Sniper attack runs on many DRs.** Each candidate DR asks whether the enemy SAN is met, and each attack reads the Location's targets by Random Location. Read the Sniper counter, the SAN, and the eligible targets once per state through `GamePlanner`'s `MemoOf(state)` and `PlayStepMemo`; never a per-unit scan through `GameState.At` (the berserk charge's 11 s, PR 32).
- **Measurement.** Pass 70 measured the hand-over and an "end the phase" proposal on `p70-perf-tw` (`dl-tractor-works-02` cut at revision 1316, `perf_cut.py`, `perf_measure.js`): at its whole-pass check the hand-over median 300 ms against main's 415, the proposal 56 to 74 ms after the first. Its notes say how. Name the measurement in the design and take it in the Studio first, before the costly tests.

## Start

Read the standing rules and the tools' README. Check the state above against the repository before relying on it, main first. Then start as the user's word says, or ask how the pass is to run if it does not.
