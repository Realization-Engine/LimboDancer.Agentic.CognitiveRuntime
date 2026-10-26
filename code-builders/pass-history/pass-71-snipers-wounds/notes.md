# Pass 71 session notes

A running record kept as the pass goes, as pass 70's was. Times are read from the clock. The design holds the decisions; this file holds what was done, in order, and why.

## The user's word

2026-10-26, about 08:21 (the session was started by another session with "Start Pass 71", and asked the user how the pass is to run, as the prompt says): "Run Pass 71 without my intervention up to the merge gate, as passes 55 to 70 did: questions go to a sub-agent, decisions become rulings open to my veto, and the gate, push, and merge stay mine to start."

- Pass 71 runs without the user's intervention, up to the merge gate. The gate (`baseline`, `build`, `proofs`), the push of the pass's branch, its pull request, and the merge are each the user's to start. `base` runs when the branch is cut, `strict` before the documents (the standing rules, section 1).
- Questions go to read-only sub-agents; each decision is a numbered ruling R71.n, open to the user's veto.
- Unit tests (the user, 2026-10-23): only those pertaining to the pass's new code, by method; the full run at the gate.

## The state checked against the repository (08:21)

- **Main is at 5379e06f**, as the prompt says; `git fetch` gives origin/main 5379e06f. The CI run of the merge on main (40319428909) passed in 5:36; the run before it (40313958066, the acronym scan) passed.
- **The working tree** holds the prompt (untracked) and `.codex/`, as the prompt says; `stash@{0}` is there, left for the user.
- **No Studio** listens on 5178, 6670, or 6671.
- **The live folder** holds 27 `p65-*` games, the pass 70 games, and the three `dl-tractor-works` games. `dl-tractor-works-03` (1.9 MB, last written 2026-10-25 18:16) is the user's newest game; whether it is finished, and so becomes the swept game, is for the user at the gate's stop. Not opened.

## Timeline

- 08:21 Prompt I, the standing rules (sections 0 to 4), and `code-builders/README.md` read. Pass 70's notes, frame brief, build brief, configuration, and design (sections 1 to 7) read for form.
- 08:22 The plan's entry for pass 71 (2 tasks, build 1:20, total 2:35) and its "moved in" paragraph, and the 11 inventory rows read.
- 08:23 Branch `feature/asl-backlog-pass-71` cut from 5379e06f. **Pass 65's games retired** (the standing rule of 2026-10-25: the games of the pass before the previous one leave at the cut): the 27 `p65-*` of the live folder and the 26 of the store moved (not deleted) to `E:/Archive/GitHub/dlandi/asl-retired-2026-10-26/live` and `/store`. `pass71.conf` written (OWN empty; BOTH_SKIP `GameEvent.RulePackage`; PRESENT `VoluntaryBreak`, pass 70's; ABSENT `SniperCheck`, which no source holds).
- 08:24 `gate.sh 71 5379e06f base` started in the background (log `asl-gate-work/base-71.log`).
- 08:24 Read on their pages myself: A14 in full (p. 82 and the top of p. 83, with A14.4's EX), A17 and A18 (p. 85).
- 08:28 `base` done (08:24:00 to 08:28:25, 4:25; SDK 10.0.403): baseline e419795257091ea3 of 5379e06f, 129 older games in the store (the swept game's cuts, `dl-tractor-works-01` and `-02`, pass 70's 41 games); text list 1,573 distinct texts (2,152 occurrences), replay digest 3,400 lines in 20 s, sweep 56,426 lines in 73 s.
- 08:25 Four read-only frame agents launched on `brief_frame.md`: (A) which DRs call a Sniper attack, the one item that could double the pass; (B) the counter, placement, the SAN as state, the Sniper player's choices, Sniper Check; (C) the Sniper's targets and results, D5.34, A12.14's exception, LLMC after a hit, backlog 26; (D) 71.2 (A17.2, A18.2, the SMC crew's Stun wound) and the code map.
- About 08:33 to 08:36 The four agents reported; kept as `frame_state.md` (B, 14:40), `frame_triggers.md` (A, 0:55 plus 0:40 for ordnance), `frame_targets.md` (C, 7:45), `frame_wounds_codemap.md` (D, 5:25). Key: the trigger reads fire records only and misses about fifteen kinds of qualifying DR; units inside any vehicle are Sniper targets today and the vehicle never is; nothing changes the SAN or gives the counter; a pinned Sniper stays pinned; the Sniper may pause mid-commit (the hook exists); LLMC after a hit is built (pass 70); 4 MF on a conveyance is built; the SMC crew's Stun has no Severity dr; A18.2's -1 vs AFV missing. One more slip of an agent with a `!boards/**` glob (D: file names and two matching lines listed, nothing opened).
- 08:36 to 08:37 Read on their pages myself before the rulings: A12.14 (p. 77: a Sniper attack costs a "?" only in a Good Order enemy's LOS; a break, wound, or Reduction always), A12.14's exception (p. 78), D5.311's "[EXC: Sniper attack]" and D5.34 (p. 203), D5.341 and D5.343 (p. 204), E1.76 (p. 225). The Passes Plan's R15.5 (it already named the ordnance To Hit DR a trigger) and R55.30 (it recorded that ordnance calls none).
- 08:37 to 08:39 The design's frame written (`Passes/ASL Unit Backlog Pass 71 Design.md`, sections 1 to 7, rulings R71.1 to R71.17). The agents' estimates sum to about 28:45 against the plan's 1:20; the table gives 26:35 after the moves. R71.13 keeps ordnance in against agent A's proposal, since one reader serves every record. **A slip:** a `python -c` that rewrote a file, inside a `cd` into the repository, to derive the build brief from pass 70's; the hook refused it before it ran. The brief written with the Write tool instead.
- 08:39 to 08:40 `brief_build.md` and `brief_4a.md` written.
- 08:41 Frame committed, 9e83e768.
- 08:41 to 08:42 **The base measurement** (design, section 7), the base Studio alone: the gate's `main-bin` build of 5379e06f (built 08:26 by `base`) on 6671 (launch config `map-studio-base65`), at 1920x1080, on pass 70's `p70-perf-tw` (used as it is, not copied: it is a measurement copy already, and the user's game is untouched). Method: pass 70's `perf_measure.js`, inline.
  - hand-over: 556, 421, 399, 262, 356, 427, 256, 414 ms (median about 406);
  - "end the phase" from the Russian view (refused, a window is open): 446 (the first), then 167, 148, 76, 74 ms.
  - The base Studio stopped at 08:42. No other Studio was running.
- 08:42 The 4a building agent launched on `brief_4a.md` (R71.3 to R71.8). 08:42 to 08:43 the briefs of 4b to 4g written from the rulings.
- 08:43 **A slip, corrected at once:** the five lines above were first written as 08:45, 08:47, and 08:48 to 08:50 by estimate; the clock read 08:43 when checked. Corrected from the files' and the commit's own times (the design 08:39, `brief_build.md` 08:39, the commit 08:41:06). Section 0, point 10, the same slip pass 70 made at its start. From here every time is read from the clock first.
- 08:44 The time log's pass 71 section written by `timelog_section.py` (the frame row and the base measurement's).
- About 09:23 The 4a agent reported (built 08:42 to about 09:23). Built: `ScenarioA1Sniper.VehicleTargets` (A14.22, A14.33), `CrewResult`, `VehicleResult`, `SniperResult`, `VehicleConditions(result)`, `KeepsConcealment`, `PinnedConditions(keepsConcealment)`, `TargetTem(terrain, smokeSources)`, the result and role constants; `FireDefinition.PartiallyArmored`; `Snipe` rewritten on the index (`SniperTarget`, `SniperTargets`, `SnipeVehicle`, `SnipeSniper`), one replay an attack (three when a leader is lost: the LLMC and the DM after it); `VehicleConditionNames` split out of Fire; the projector's `UnpinSnipers`; `PlayRecords.SniperResultText`; `game_file.py` gains `passenger`, `san`, `sniper`. No new JSON key, event, action, or choice kind. Judged:
  - R71.4's "no PRC" read with the Inherent crew among the PRC (PRC are Passengers, Riders, and Crew): an unarmored vehicle is a target when manned or carrying any PRC. Right.
  - The enemy Sniper's TEM is its own Location's (Play lists no hex's Locations): accepted.
  - A crew already broken is no crew candidate; a crew or Sniper already pinned takes no effect on dr 2: accepted.
  - A concealed target that keeps its "?" is recorded with a null unit; the result string ("stunned", "pinned") stays public and for a concealed vehicle tells the stack holds a vehicle. **For the hidden-information referee.**
  - The A12.14 LOS read is `NearestGoodOrderEnemyInLos` (any range, as A12.14's "regardless of range" says), not turned off at night. **For the referee.**
  - No catalog vehicle carries `asl:partially-armored`: the case is tested in Rules only.
  - Tests by method: Rules 12/12; Play 15/15 and 1/1 (the five new and the existing Sniper methods of passes 15, 16, 55); MapStudio 3/3. Build exit 0. (The agent's report; the logs are its.) Slips: one `cd` and one heredoc, both refused by the hook.
- 09:24 to 09:25 Diff read (Rules, projector, planner): as the rulings say. `games_4a.py` run (p71-sniper-ce-tank, -truck, -vs-sniper); `build_studio.sh` exit 0 (2:18).
- 09:26 to 09:30 Studio check of 4a (my Studio on 6670, 1920x1080, Russian PFPh; dice 3 3, 3 3, then the Sniper's):
  - p71-sniper-ce-tank, Sniper dr 2: "The German Sniper (gs) attacks after the SAN roll ...: dr 2; [Q8]: the crew of T-34 M41 R1 is Stunned (A14.3, D5.34)"; the file holds `sniper-attacked` (unit r-tk, result stunned) and `conditions-changed` r-tk stunned, BU, Motion off.
  - p71-sniper-truck, Sniper dr 1, Random Selection 1 1 6: "[Q8]: GAZ-MM R1 is immobilized (A14.33)".
  - p71-sniper-vs-sniper, dr 2: "[Q8]: the enemy Sniper counter (rs) is pinned and makes no further attack this Player Turn (A14.31)"; the file holds the pin. Its removal at the CCPh's end not walked (the DFPh's end is the DEFENDER's control); asserted by `BacklogPass71Tests.TheEnemySniperIsPinnedUntilTheCloseCombatPhaseEnds`.
  - **Found for the fixes:** the Sniper line names the counter and the trigger by raw ids ("(gs)", "the SAN roll fire-...-roll-1", "(rs)"); older than the pass, against the hex UI and no-raw-id rules.
- 09:31 to 09:42 **The measurement after 4a: inconclusive.** My Studio, warm from the three games: hand-over 1172 (the first), 598, 600, 585, 406, then 780, 642, 728, 432, 532, 597, 517, 553 ms (median about 585); the proposal 92, 65, 68, 71, 81 ms (level with the base's 76 to 167). Main's build again, in a fresh process: 1549, 1472 (the first two), then 240, 382, 413, 756, 505, 395, 712, 514, 344, 363, 362, 804, 446 (median about 413). My Studio again in a fresh process: the page stopped taking the scripted perspective changes and hand-over clicks part of the way through each run (the select's value changed with no hand-over shown; a real click on the confirm did nothing; the server idle, no error in its log; the circuit connected), in three runs. 4a changes no code a hand-over runs (the Sniper planner, and the Sniper record lines, which this game has none of), so the difference is read as the warm Studio's, not the pass's; it is not proven. **Decision:** the measurement is taken again after 4e, each Studio in a fresh process, and the stop threshold applies then. Cost: about 11 minutes. Studios stopped at 09:42.
- 09:43 4a committed, dbf22030 (its games to OWN). The 4b agent launched on brief_4b.md (R71.9, R71.10).
- 09:51 At the user's word (relayed by the session that started this one): the design's estimates calibrated (raw divided by 4.5, between pass 70's 4.1 and 4a's 5.7; 4a as built), one forecast in section 2 and F1: about 8:10 to the gate, 5:41 of it building, against the plan's 1:20 of building (2:35 in all). `design_calibrate.py`.
