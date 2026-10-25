# Pass 70 session notes

A running record kept as the pass goes, at the user's word of 2026-10-25 ("Keep meticulous notes as you go"). Times are read from the clock. The design holds the decisions; this file holds what was done, in order, and why.

## The user's word

2026-10-25, about 11:50: "Start Pass 70 (Infantry III: morale, Close Combat, and concealment). Run it without my intervention, up to the merge gate. Questions go to read-only sub-agents; each decision is a numbered ruling R70.n, open to my veto. The gate, the push of the pass's branch, its pull request, and the merge are each mine to start. Keep meticulous notes as you go." It also asks that `Pass 70 Prompt I.md` be read first and its state checked against the repository, main first, since pull request 39 merged after the prompt was written.

- Pass 70 runs without the user's intervention, up to the merge gate. The gate (`baseline`, `build`, `proofs`), the push of the pass's branch, its pull request, and the merge are each the user's to start. The `base` and `strict` stages are not the gate's (the standing rules, section 1): `base` runs when the branch is cut, `strict` before the documents.
- Questions go to read-only sub-agents; each decision is a numbered ruling R70.n, open to the user's veto.
- Unit tests (the user, 2026-10-23): only those pertaining to the pass's new code, by method; the full run at the gate.

## The state checked against the repository (11:51)

- **Main is at 25aa97fd** (pull request 39, the prompt and the five rows moved from pass 75), not 7a734fc3 as the prompt says: PR 39 merged after the prompt was written, as the user said. PR 39 changed four documents only (the plan, the inventory, the backlog, the prompt). Its CI run on main, 40208728075, was in progress at 11:51; PR 38's run (40206829533, 3:58) and the Campaign nightly of 2026-10-25 (40187028836) are green.
- **The working tree** holds only `.codex/`, as the prompt says.
- **Worktrees:** the two Codex worktrees as the prompt says, and one more it does not name: `.claude/worktrees/replay-reenact-scope-9c9f9a` on `claude/replay-reenact-scope-9c9f9a` at 1f621cf4 (the parked Replay re-enactment scope; it touches nothing of this pass). Left alone.
- **The machine:** SDK 10.0.402, 10.0.113, and 11.0 rc.2, as the prompt says. No Studio listens on 5178, 6670, or 6671 (the user's Studio is not running either; the prompt said it runs on 5178). The only `dotnet` processes are the spec-chat MCP servers. `outside_outputs.sh list`: nothing registered is left.
- **Proof games:** the live folder holds 27 `p65-*` games and the two `dl-tractor-works` games; no pass 60 game is left to retire at the cut.

## Timeline

- 11:51 Prompt I, the standing rules (all of sections 0 to 4), and `code-builders/README.md` read. Pass 65's notes, frame and build briefs, configuration, and design (sections 1 to 5) read for form.
- 11:52 The plan's entry for pass 70 (10 tasks, build 7:20, total 8:35), its two "moved here" paragraphs, the 48 inventory rows, and the 14 backlog rows naming pass 70 read. The rulebook page texts the pass needs are all in `asl-gate-work/rulebook-pages`.
- 11:52 Branch `feature/asl-backlog-pass-70` cut from 25aa97fd. `pass70.conf` written (OWN empty; BOTH_SKIP `GameEvent.RulePackage`; PRESENT `IsCrew`, pass 65's; ABSENT `VoluntaryBreak`, which no source holds).
- 11:52:56 `gate.sh 70 25aa97fd base` started in the background (log `asl-gate-work/base-70.log`; SDK 10.0.402).
- 11:53 (Slip, corrected at once: the three lines above were first written as 11:55, 11:57, and 11:58 by estimate. The clock read 11:53 when checked; the base log gives 11:52:56. Section 0, point 10. From here every time is read from the clock.)
- 11:54 Four read-only frame agents launched on `brief_frame.md`, each reading the rules on their pages and the code of its domain: (A) morale outside fire and rout, 70.1 and 70.2 with A15.1, A17.1, A19.11, the PRC, Bail Out, and D5.311 rows, and the LLMC row; (B) who sees, concealment, HIP, and the Right of Inspection, 70.3 to 70.5 and 70.10, with the AFV-naming row; (C) Close Combat, 70.6 and 70.7 with A11.52, A18.12, PRC in CC, the CC counter rows, a wreck, D6.82, and D5.42; (D) Bypass and detection, Searching and Mopping Up, 70.8 and 70.9, the `CheckInvariants` row, and the code map the building agents need.
- 11:58 `base` done (11:52:56 to 11:57:59, 5:03): baseline 10f070d46067d289 of 25aa97fd, 114 older games in the store; text list 1,500 distinct texts (2,048 occurrences), replay digest 3,181 lines in 21 s, sweep 53,935 lines in 79 s (the trimmed store of PR 38).
- 11:55 The Memory and Performance Guidelines read (planner reads per state, cache ownership, measurement, the checklist), for the design.
- 11:56 Read on their pages myself: A10.41 (p. 66), A10.5 (p. 66), A10.711 (p. 69), A12.16 and A12.2 (p. 79). The plan's note on the withdrawn A12.16 ruling (plan, line 2148) read.
- About 12:04 Agent C (Close Combat) reported; kept as `frame_cc.md`. Key: a Gun's crew is refused CC in five places though no rule bars it (A11.13 gives the Gun's loss); Infantry CC in a vehicle's Location is refused (`VehiclePresentBar`); a vehicle's PRC are probably marked in Melee at the CCPh's end (`InMelee` follows containment), against A11.71; the withdrawal bar counts unseen enemies; estimates 70.6 1:15, 70.7 5:00; D10.1 to pass 110.
- About 12:08 Agent B (who sees) reported; kept as `frame_sight.md`. Key: six reads of who sees, two of which count hidden units; the view reads no LOS and is built in six places, none a planner proposal; the replay digest hashes states only, so 70.10 changes only the sweep's view lines; estimates 70.3 2:30, 70.4 2:00, 70.5 3:30, 70.10 2:00; pass 60's AFV-naming row to pass 75.
- About 12:10 Agent A (morale and rout) reported; kept as `frame_morale.md`. Key: ten morale paths outside Fire, each with its gaps; a SMC Rider's Casualty Reduction in a Bail Out throws (a null severity); A15.1 exempts PRC and crews from Heat of Battle; `asl:unarmed` exists but the Fire facts carry no Unarmed fact; estimates 70.1 8:05, 70.2 2:25.
- About 12:12 Agent D (detection, search, code map) reported; kept as `frame_search_codemap.md`. Key: A12.13 is built (pass 60, R60.9); Mopping Up is limited to a card's building because Play reads no building from the map; Searching has nothing; `CheckInvariants` are plumbing; estimates 70.8 1:45, 70.9 4:15; the code map for the building agents.
- 12:05 to 12:10 Read on their pages myself before the rulings: A15.1 (p. 83: Unarmed, PRC, Heroes, crews, berserk exempt from Heat of Battle), A11.13 (p. 72), A12.152 (p. 79), A10.2 (p. 65: "Should an unbroken leader break ... LLTC", which R70.11 rests on).
- 12:10 to 12:13 The design's frame written (`Passes/ASL Unit Backlog Pass 70 Design.md`, sections 1 to 7, rulings R70.1 to R70.34). The agents' estimates sum to 33:05 of building against the plan's 7:20; R70.1 keeps the pass whole, the last increment (3j) the one a session's end would leave out.
- 12:12 **A ruling corrected on the page before any build.** R70.17 was first written as "a passed LLTC or LLMC ... costs no ?". A12.141 (p. 78), read at 12:12, names LLTC/LLMC, a PF or MOL check, and a leadership modifier applied to anything but a PAATC among the actions that cost a "?". R70.17 rewritten with A12.141's own list and exceptions. The lesson of pass 60 (a ruling that says something never happens is read on its page first) caught it. R70.16's clause on a Nondefinitive LOS was also taken back to "as each caller reads it today", since I had not read a rule for it.
- 12:13 `brief_build.md` (from pass 65's, with its lessons and the hidden-information point) and `brief_3a.md` written.
