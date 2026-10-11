# Pass 40, first session: Infantry I, movement, stacking, and Locations

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Pass 35 (Repairs) is merged into main as 5355b7f6 and its documents closed as 3e5c721c, both pushed on 2026-10-10. The main checkout stands on a detached HEAD at `origin/main`, because the branch `main` is checked out in a Codex worktree (`C:/Users/dland/.codex/worktrees/formation-design-review`); leave that worktree alone. This prompt is untracked until the pass's first commit.

Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its last six lessons are pass 35's fourth session. Then read pass 40's entry in `src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md` (section 22.1, part (c), "Pass 40: Infantry I"), and the [pass 35 review](<../Passes/Scenario A1 Backlog Pass 35 Review 2026-10-09.md>) for how the last pass ended.

## How this pass runs: the pattern of pass 35, at the user's word

Pass 35 was designed and built at once, increment by increment, with the user's word before every commit. Pass 40 runs the same way.

1. **The frame first, read-only, then stop.** Read the pass's 49 inventory rows (the rows of `ASL Rule Inventory A to E.md` whose pass column says Pass 40), each rule on its page of the registered PDF, the code each row names, and the backlog rows that point at this pass. Write the design's first sections in `src/ASL/docs/Passes/ASL Unit Backlog Pass 40 Design.md`: what the pass is, the rows by task, what is certain and what needs a run, what the frame found outside the rows, and the questions, each with its analysis and a proposal. Group the nine tasks into increments. Stop for the user's answers.
2. **Then one increment at a time.** For each: its design section, then each task as pass 35's fixes ran:
   - the rule read on its page, with the page cited; a rule's example is part of the rule;
   - the repair or the new rule in Rules as a calculator over facts, with Play handing the facts over and writing the events;
   - MapStudio built into `bin/p31b` with warnings as errors, with my Studio stopped;
   - a small test game in my Studio on port 6671, written by a script kept in the tools folder, with a control game beside it under its own id where a contrast shows the rule, and what the game is meant to show confirmed so that its file holds it;
   - a Rules test, and a Play test where the planner writes events; say plainly when there is none and why;
   - a report, and a stop for the user's word before the commit. One commit a task.
   If the user says to build several and ask before the commit, tasks that share files go in one commit with each listed in its message.
3. **The reviews,** once the last increment and the pass's documents are committed: five read-only agents in one message (the referee in two halves if the rules span chapters, the table player, the Rules boundary, the UI and Blazor review if the Studio changed), each pointed at its own section of a prompt that holds the briefs. Read the lines and the page behind each finding, report them together with a judgement and a proposal for each, and stop. Fix nothing before the user has seen them.
4. **The fixes** the user chooses, each as point 2; a finding not fixed gets a backlog row.
5. **The Studio check of the whole pass** at 1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, and 320 pixels, in each side's view and the adjudicator's, by keyboard and mouse, with one full Player Turn at the largest and the smallest.
6. **The documents:** the review, the design's "As built" section, the rulings (R40.n in the Passes Plan), the inventory's rows and its two count tables, the coverage document, the backlog's section for the pass, the plan's status, the time log, and any lesson for the standing rules.
7. **Stop at the gate.** The gate, the merge, and the push are each the user's to start.

Be sparing throughout: narrow reads, one value from the page, the test classes a change touches and no wider. Read the clock (`date +%H:%M`) before writing a time into the log, and log every sub-task.

## Start

1. `git fetch origin`, then show `git status`, `git log -3 --oneline origin/main`, and `git worktree list`. Confirm the tree is clean but for `src/ASL/boards` and this prompt.
2. Cut the branch from main as it stands now: `git switch -c feature/asl-backlog-pass-40 origin/main`. Main may have moved again; the user also commits there (the europe-hex-map prototype). Note the commit the branch is cut from: the gate's baseline is built from it.
3. Check that no Studio of the last session runs on 6671.
4. **The first commit, before the frame, at the user's word:** two things pass 35's closing missed in the Redesign Plan's section 22.1. The status paragraph of pass 35 still reads "built on the branch ... not merged"; it should say merged as 5355b7f6 with the gate passed and CI green. Task 40.1 (ADJACENT, A.8) should be marked as taken by pass 35. This prompt goes in the same commit.
5. Then the frame, and stop for the user's answers.

## What pass 40 is

Block 1 of the run order, "Repairs and the ground under everything", after 35 and before 45. The plan gives it 49 rows, no new counters, 5:20 of building and 6:35 in total, 3:18 as the likely figure.

| Task | What it builds | Rules |
|---|---|---|
| 40.1 | ADJACENT as the rule has it. **Done in pass 35**; see below | A.8 (p. 43) |
| 40.2 | Road entry as the mover's choice, with FFMO and no TEM at the road rate; woods-road hexes; the Road Bonus's cancellations | A4.132, B3.3, B3.4, B13.31, B13.32 |
| 40.3 | Dash | A4.63, C6.1 |
| 40.4 | Infantry OVR in full, and recapture of prisoners by entry | A4.14, A4.15, A4.151, A4.152, A20.54 |
| 40.5 | Mandatory TPBF against an entering unit; FPF by Infantry manning ordnance | A8.311, A8.312 |
| 40.6 | Overstacking in the MPh and on the IFT; vehicles counted; a manning crew's size in play | A5.11, A5.12, A5.131, A5.132, A5.2, A5.4, A5.5 |
| 40.7 | Portage and movement corrections: a moved MMG, HMG, or pushed Gun in the AFPh; no recouped portage; the leader's IPC as a choice; CX removal; Assault Movement after an in-Location MF; SMOKE outside Difficult Terrain; conveyance MF | A4.11, A4.4, A4.41, A4.42, A4.44, A4.51, A4.61, A4.72 |
| 40.8 | Hazardous Movement in every phase | A4.62 |
| 40.9 | Deployment and Recombining exemptions; half-hex seams; offboard options; setup limits; provisional turns | A1.31, A1.32, A1.6, A2.2, A2.3, A2.52, A2.9, A3.9 |

The page numbers are in the plan; take each page from the PDF or the inventory, never from the transcription's markers.

## What pass 35 left for this pass

- **ADJACENT (40.1) is built.** `ScenarioA1MovementCalculator.IsAdjacent` reads a LOS and an Infantry advance in either direction, and no advance enters a marsh (B16.4). One backlog row is left of it: whether a marsh hex and dry ground are ADJACENT, which needs a ruling on "either direction". Propose one in the frame. Count 40.1's rows as done when the row counts are set.
- **Overstacking is counted four ways,** one of the seven copies pass 32 kept apart that pass 35 did not take (the backlog's section 55.2). Task 40.6 merges them into one Rules member as it builds.
- **Backlog rows of section 55.2 that lie in this pass's ground,** to read at the frame and take or leave with a reason:
  - Case J against a vehicle that has paid only its Start MP, and the same question for a firer that only Started and Stopped (C.8, D2.51). It is a moving-target fact; it may belong to pass 50.
  - The Gun's target list offers a Location the Gun has no LOS to.
  - The phasing side's view in the DFPh says it cannot end the phase only by a disabled button; the "Latest" line did not follow five phase changes; at 320 pixels the hand-over prompt lies over the foot of the Activity list.
  - `EnemyUnitFacts.Broken` is a fact no Rules member reads.
- **Infantry OVR (40.4) touches berserk movement and A4.152,** which the sequence calculator already orders ("the berserk Infantry OVR ... has its CC at once"). Read what is built before designing what is "in full".

## The proofs and the tools

In `E:\Archive\GitHub\dlandi\pass32-tools`:

- **The gate's scripts of pass 35,** to copy for pass 40: `gate35-baseline.sh` (the store brought up to date, an archive of the baseline commit checked by a grep, built into `main-bin`, the `proof-main` tool, then the text list, the replay digest, and the sweep) and `gate35-gate.sh` (the solution build with warnings as errors into `bin/gate`, `suite.sh`, the Node viewport test, `chart.sh`, the `proof-gate` tool bound to `bin/gate`, and the three proofs). Change the baseline commit, the two grep checks, the output folders, and the swept games. Both ran side by side with the Docker check in 70 minutes, and a background command of this harness is not cut off at ten minutes.
- **`proof-dump`,** a copy of the proof tool that prints a plan's events when `PROOF_DUMP` names a file: build it twice (`-p:StudioBin=` the baseline's and the gate's folders, each into its own `-p:OutDir=`) and sweep one cut copied into a store of its own. It names a sweep difference that shows only as a digest.
- **The store** holds 380 games after pass 35's gate. Bring it up to date with the live folder again before the baseline.
- **`pass35/session3/`:** the game generators (one a fix, each a short script: `make_crest.py`, `make_check_turn.py`, and the rest), `replay_compare.py`, the documents' scripts (`docs_inventory.py --check` and `docs_coverage_counts.py --check` first), `dump_pages.py`, and `pages\pNNN.txt` (the PDF's pages 43 to 253 as text; make more with `dump_pages.py` when a rule lies outside them).

**How a difference is read at the gate:** as in pass 35's design, sections 14.4 and 15. The pass changes results on purpose, so every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault. A game the pass's own build recorded has no "before" and is listed, not compared.

## To carry from pass 35's fourth session

- **Write every script with the Write tool.** An inline `python -c` inside a double-quoted shell string turned a `\n` into a real line break and broke a build; a heredoc and `sed -i` are the older slips of the same kind.
- **Confirm what a game is meant to show.** A proposal read and cancelled leaves nothing in the file; 28 of pass 35's 69 games hold their setup alone, and the table player found the design citing them. Say in the design which games are recorded.
- **A new test that fails may be showing the rule.** Read a failure against the page before changing the test.
- **Do not skip the Play test quietly.** Fixes 3 to 6 of pass 35 have none, and that is a backlog row. Where a Play test needs a helper that does not exist, say so before the commit and let the user choose.
- **Report what a check did not cover** in the same message as what it did: what was not seen in the Studio, what rests on reading alone.
- **When a fix grows past what was approved, or amends a ruling the user gave, stop with the reading and a proposal** before building it.
- **Main may move under the branch.** Check `origin/main` before the gate and again before the merge, and say what moved and whether any file is changed on both sides. The merge is made in a temporary worktree (`git worktree add -b merge-40 E:/Archive/GitHub/dlandi/m40 origin/main`), pushed as `merge-40:main`, and the worktree removed.
- **The pass numbers in section 3 of the standing rules are the old ones;** the lesson on the renumbering of 2026-10-09 further down is right: the rule passes are 35 to 185 in steps of five. Correct section 3 when the standing rules are next edited.
