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
- 08:37 to 08:45 The design's frame written (`Passes/ASL Unit Backlog Pass 71 Design.md`, sections 1 to 7, rulings R71.1 to R71.17). The agents' estimates sum to about 28:45 against the plan's 1:20; the table gives 26:35 after the moves. R71.13 keeps ordnance in against agent A's proposal, since one reader serves every record. **A slip:** a `python -c` that rewrote a file, inside a `cd` into the repository, to derive the build brief from pass 70's; the hook refused it before it ran. The brief written with the Write tool instead.
- 08:45 `brief_build.md` and `brief_4a.md` written.
