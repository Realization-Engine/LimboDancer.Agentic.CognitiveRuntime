# Pass 70, first session

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; then `code-builders/README.md`. This prompt gives the state, the user's word, and what is open, and no procedure: where it and those files disagree, the files are right, and say so.

## The state, as of 2026-10-25

- **Main is at 7a734fc3** (pull request 38, proof games trimmed to the essentials), green in CI (run 40206829533, 3:58). Nothing is running in CI. The Campaign nightly of 2026-10-25 is green.
- **The last rule pass is pass 65** (Passengers, Riders, and crews), merged as pull request 34 (1bc294dc, 2026-10-24); its record is `code-builders/pass-history/pass-65-passengers-riders-crews/notes.md`. Since then: CI split into five parallel jobs (PR 35), the Authoring tests from 4:00 to 0:17 in CI (PR 36), the MapStudio layer oracle and its split classes (PR 37), and the proof games trimmed (PR 38). None of them is a rule pass. A pull request's CI now takes about 4 minutes, not 14.
- **The proof games** (PR 38, the user's standing rule of 2026-10-25): recent whole games, the current and previous pass's games, and `dl-tractor-works-02` swept at two cuts a phase. Pass 70's games join that set; pass 65's stay as the previous pass's.
- **The working tree** holds only `.codex/`, which is the Codex session's: leave it. The Codex worktrees (`asl-narrative-extensions`, `codex/formation-asl-map-alignment`) are weeks behind main and touch nothing of this pass.
- **The machine:** SDK 10.0.402 (and 10.0.113, 11.0 rc.2); no Studio on 6670 or 6671; the user's Studio on 5178; `outside_outputs.sh list` reports nothing left.
- **Two standing rules govern every test run of this pass,** in the standing rules' section 2: the Studio is the only test while code is written and fixed; and the rule for unit tests (the user, 2026-10-23): "Only unit tests pertaining to the new code in the pass must be exercised. The full unit test run waits until the gate."

## The user's word

On 2026-10-25 the user said: "Catch up on our last git commits and current running CI and prepare for execution of Pass 70." This prompt is that preparation. The same day the user asked whether to combine passes 70 and 75; after the analysis the user chose to keep them separate and move five pass 75 rows into pass 70 (below). The start is not given, and neither is a mode (passes 55, 60, and 65 each ran without the user's intervention up to the gate, at the user's word for each). Cut no pass branch and write no frame until the user says to start, and ask how the pass is to run if the start does not say.

## Pass 70, as the plan has it

**Infantry III: morale, Close Combat, and concealment**, in the redesign plan (`ASL Card Play and Map Studio Redesign Plan.md`, the entry for pass 70): ten tasks (70.1 to 70.10), 7:20 of build and 1:15 of overhead, 8:35 in all. Needs first: pass 40, merged. Counters: none.

- 70.1 Voluntary Break and Voluntary Rout; LLMC and LLTC outside fire (A10.2, A10.41, A10.711).
- 70.2 Rout by level and inside a building; Interdiction by vehicles and Guns; Fanaticism by SSR (A10.5 to A10.532, A10.8).
- 70.3 Who sees: one Good Order read for fire, movement, advance, Rally, and gain (A12.1, A12.14, A12.141).
- 70.4 The owner's choices on a "?" (A12.14).
- 70.5 "?" for an Emplaced Gun's crew and for vehicles; entering concealed; HIP for vehicles (A12.11 to A12.121, A12.2, A12.3, A12.34).
- 70.6 Withdrawal completed; CC with a Gun's crew (A11.1, A11.21).
- 70.7 Infantry against Infantry in a Location with a vehicle, its Ambush; Street Fighting (A11.31 to A11.52, A11.8; A11.52 now whole, its capture of Passengers included).
- 70.8 Bypass endings and detection leftovers (A12.13, A12.15, A12.151, A12.42).
- 70.9 Searching; Mopping Up in any building (A12.152 to A12.154).
- 70.10 Right of Inspection by LOS (A12.16; the plan's note on the withdrawn A12.16 ruling: a side today sees an unconcealed enemy stack in full whatever the LOS).

**It is larger than the plan says.** The plan's entry names 35 inventory rows; the inventory now gives pass 70 48. Five of them came from pass 75 on 2026-10-25, at the user's word, after weighing whether to combine passes 70 and 75 (they stay separate). They sit on code pass 70 changes anyway, so each path is changed once:

- with 70.1, the morale check outside fire: A15.1 (Heat of Battle's Unarmed and PRC exemptions, and its DR in the MCs outside Fire and Rally), A17.1 (the wound procedure in every Casualty Reduction), A19.11 (an Unarmed unit's ELR immunity);
- with 70.7, CC with a vehicle: A18.12 (Leader Creation in CC against a vehicle) and A11.52's capture of Passengers.

No task was renumbered and no estimate moved; the frame estimates them. Pass 65 also moved eight more items under the entry with no task renumbered (the plan's paragraph after the table), and the backlog names pass 70 in about fifteen rows (search it for "Pass 70"), most of them in section 71.1:

- PRC in CC, decided in Rules (`ScenarioA1PassengerActionRules.CloseCombat`) and refused in Play (A11.611, A11.71; with 70.7);
- Heat of Battle, leadership, and LLMC for PRC, and D6.651's reach of a Passenger leader, decided in Rules (`LeaderReaches`) and read by no Play path;
- a CC condition on an unload or a Bail Out into an enemy Location (D6.5, D6.24), and TPBF's at PRC (A7.211); no such condition exists today;
- the Bail Out NMC's and D5.311's crew attack's missing Heat of Battle, leadership, Wound Severity, and ELR;
- CC against a wreck (D10.1; `ScenarioA1WreckRules.CloseCombatBar`), a Carrier HS recombining without a TC (D6.82), entering a vehicle at its capture in the CCPh (D5.42);
- older rows: a LLMC after a leader's loss outside fire (pass 35's backlog row), naming an AFV in a Location that also holds concealed units (pass 60's), and whether `GameProjector.CheckInvariants` are rules or plumbing (pass 32.i's).

Expect it to run about twice the plan, as passes 55, 60, and 65 did (pass 65: 83 rows against 63, the frame's 16:50 against 8:15). Ruling R65.1 kept a pass of that size whole; the frame says again whether that holds here.

## Open, and known before the start

- **The morale check outside fire is one path, not several.** Pass 65's moved items (Heat of Battle, leadership, Wound Severity, and the ELR for the Bail Out NMC, D5.311's crews, and PRC), the LLMC outside fire of 70.1, and A15.1, A17.1, and A19.11 all ask the same thing of it. The frame decides whether one Rules member serves every MC made outside the Fire and Rally packages.
- **Hidden information is most of this pass** (70.3 to 70.5, 70.8 to 70.10). Every task there changes what a side may see, so the referee's review reads it from each side's view, and the scoped Studio check keeps the view of the side that must not see (the standing rules, section 1, point 4).
- **Right of Inspection by LOS (70.10) changes what older games show,** not what they record: a view built from a recorded game hides stacks it showed before. Expect the gate's text list and digest to differ there; each differing line is named as 70.10's or it is a fault. Decide in the frame whether a fact on the record keeps older games as they were.
- **Rulings still open to a veto:** R65.1 to R65.33, and R45.3 to R45.5 (no approval is recorded for them in the Passes Plan). R40.12 to R40.26 were approved on 2026-10-12.

## Memory and performance

The pass follows the [ASL Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>); the design answers its checklist, and each task is checked against its contracts before it is done. Where this pass is most likely to add work:

- **Right of Inspection and "who sees" read LOS per enemy stack per view.** Built naively, every hand-over and every proposal pays a LOS check for each pair of stacks. Read it once per state and view through `GamePlanner`'s `MemoOf(state)` and `PlayStepMemo`, as pass 65's index of PRC by vehicle is read; never a per-unit scan through `GameState.At` (the berserk charge's 11 s, PR 32).
- **Searching and Mopping Up** reach every Location of a building or a radius: one pass over the state's object index, not a lookup per Location.
- **Measurement.** Pass 65 measured the hand-over and an "end the phase" proposal on a copy of `dl-tractor-works-02` cut at revision 1316 (`perf_cut.py` in pass 65's folder; base 225 to 276 ms and 112 to 157 ms). Name the measurement in the design and take it in the Studio first, before the costly tests.

## Start

Read the standing rules and the tools' README. Check the state above against the repository before relying on it, main first. Then wait for the user's word.
