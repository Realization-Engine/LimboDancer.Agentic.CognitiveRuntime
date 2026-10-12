# Pass 45, first session: Infantry II, fire, MGs, and SW

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Pass 40 (Infantry I) is merged into main as 14dca517 and its documents closed as 5982767e, both pushed on 2026-10-12 with CI green. The main checkout still stands on the branch `feature/asl-backlog-pass-40` at ffdd3db7, which is wholly in main. The branch `main` is checked out in a Codex worktree (`C:/Users/dland/.codex/worktrees/formation-design-review`), and a second Codex worktree holds `asl-narrative-extensions`; leave both alone. This prompt is untracked until the pass's first commit.

Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its last six lessons are pass 40's. Then read pass 45's entry in `src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md` (section 22.1, "Pass 45: Infantry II"), and the [pass 40 review](<../Passes/Scenario A1 Backlog Pass 40 Review 2026-10-11.md>) for how the last pass ended.

## What pass 40 did without the user, and the user's answer

The last three tasks of pass 40, its reviews' fixes, and rulings R40.12 to R40.26 were made under the user's word "Complete Pass 40 without my intervention". On 2026-10-12, with the pass's report before them, the user said: "I accept all of your judgments from the latest pass." That settles all of it:

- **Rulings R40.12 to R40.26 are approved** as they stand in the Passes Plan. Build on them as on any ruling; none is open any more. R40.17 (the Mandatory FG of units whose fire counters differ), R40.18 (a crew's FP in FPF), and R40.19 (which acts wait behind the attack owed) are the three this pass builds on.
- **R40.9 stands** (a Fire Group halves whole against a Dash). The referee's reading, halving by firer, keeps its backlog row and is not reopened without the user.
- **The two departures from the answer to question F10 are accepted:** a CE or BU choice at setup is pass 65's, and the half-hex rule for entry and occupation is pass 185's.
- **The reviews' fixes, and which findings were fixed and which given backlog rows,** are accepted as judged (design 12.1 and 12.3).
- **The branch `feature/asl-backlog-pass-40` stays** until the user says to delete it.

The word to go on without stopping is used up, and the acceptance does not renew it: pass 45 stops for the user's word as below.

**The documents still say "open to a veto".** The Passes Plan's rows R40.12 to R40.26, the design's sections 10 to 13, and the review's Status and rulings paragraphs were written before the answer. Correct them in the pass's first commit: each such phrase becomes "approved by the user on 2026-10-12", and the design and the review gain one sentence quoting the user's words. Change nothing else in them.

## How this pass runs: the pattern of pass 35, at the user's word

Pass 45 stops for the user's word, as passes 35 and the first half of 40 did.

1. **The frame first, read-only, then stop.** Read the pass's 45 inventory rows (the rows of `ASL Rule Inventory A to E.md` whose pass column says Pass 45), each rule on its page of the registered PDF, the code each row names, and the backlog rows that point at this pass. Write the design's first sections in `src/ASL/docs/Passes/ASL Unit Backlog Pass 45 Design.md`: what the pass is, the rows by task, what is certain and what needs a run, what the frame found outside the rows, and the questions, each with its analysis and a proposal. Group the tasks into increments. Stop for the user's answers.
2. **Then one increment at a time.** For each: its design section, then each task:
   - the rule read on its page, with the page cited; a rule's example is part of the rule;
   - the rule in Rules as a calculator over facts, with Play handing the facts over and writing the events;
   - MapStudio built into `bin/p31b` with warnings as errors, with my Studio stopped;
   - a small test game in my Studio on port 6671, written by a script kept in the tools folder, with a control game beside it under its own id, played with scripted dice and read back from its file;
   - a Rules test, and a Play test where the planner writes events; say plainly when there is none and why;
   - the test classes the change touches, **and the Units test project whenever a record of the state changes**;
   - a report, and a stop for the user's word before the commit. One commit a task.
3. **The reviews,** once the last increment and the pass's documents are committed: five read-only agents in one message, their briefs in a prompt file (`Pass 40 Review Briefs.md` is the form). Read the lines and the page behind each finding, report them together with a judgement and a proposal for each, and stop. Fix nothing before the user has seen them.
4. **The fixes** the user chooses; a finding not fixed gets a backlog row.
5. **The Studio check of the whole pass** at 1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, and 320 pixels, in each side's view and the adjudicator's, by keyboard and mouse, with one full Player Turn at the largest and the smallest. Pass 40's check did not walk a full turn at 1920 or every view at every width; this one does, or says what it left.
6. **The documents:** the review, the design's "As built" section, the rulings (R45.n in the Passes Plan), the inventory's rows and its two count tables, the coverage document, the backlog's section 57, the plan's status, the time log, and any lesson for the standing rules.
7. **Stop at the gate.** The gate, the merge, and the push are each the user's to start.

Be sparing throughout: narrow reads, one value from the page, the test classes a change touches and no wider. Read the clock (`date +%H:%M`) and only then write a time into the log; pass 40 wrote three times a minute ahead of the clock by passing the time to a script before reading it.

## Start

1. `git fetch origin`, then show `git status`, `git log -3 --oneline origin/main`, and `git worktree list`. Confirm the tree is clean but for `src/ASL/boards` and this prompt.
2. Cut the branch from main as it stands now: `git switch -c feature/asl-backlog-pass-45 origin/main`. Main may have moved; the user also commits there. Note the commit the branch is cut from: the gate's baseline is built from it. Leave `feature/asl-backlog-pass-40` in place unless the user says to delete it.
3. Check that no Studio of the last session runs on 6671.
4. **The first commit, at the user's word:** this prompt, and the documents' correction named above (the rulings recorded as approved on 2026-10-12). Show the changed lines' count for each file before asking.
5. Then the frame, and stop for the user's answers.

## What pass 45 is

Block 1 of the run order, "Repairs and the ground under everything", after 40 and before 50. The plan gives it 38 rows, no new counters (the German HMG's and the mortars' dm values are manufactured, R0.3), 5:40 of building and 6:55 in total. The inventory now names 45 rows for it: passes 35 and 40 moved seven there (A.7, A4.41, A4.42, A4.44, A4.61, A4.63, A8.312). Pass 40's eight tasks took 6:41 by the clock against a plan of 6:35, of which the gate took 1:47.

| Task | What it builds | Rules |
|---|---|---|
| 45.1 | Fire between levels of one hex as PBF; vertical ADJACENT; upper-level Encirclement | A7.21, A7.72, B23.25, B23.26 |
| 45.2 | Cowering marks the unit and its SW; the DEFENDER's choice of ROF or Residual FP | A7.9, A8.23 |
| 45.3 | Residual FP completed: the cap after adjustment, CX and the other outside DRM, a malfunctioned weapon, one attack a Location, ordnance HE, Spraying Fire in the MPh | A8.2, A8.22, A8.221, A8.24, A8.25, A8.26 |
| 45.4 | Squad Spraying Fire off the catalog trait; the capability test for a group | A7.34, A9.5, A9.52 |
| 45.5 | Fire Lanes completed: vehicles, later MF, Snap Shots, hexside TEM and Hindrance, cancellation | A9.22, A9.221, A9.222, A9.223 |
| 45.6 | Field of Fire, Mandatory Fire Direction and the 16-hex limit, a leader directing a MG's later shots | A7.53, A9.21, A9.4 |
| 45.7 | Sustained Fire's X#; SW upkeep: repair of mortars, self-destruction, dismantling for every SW the rule names | A.11, A9.3, A9.72, A9.73, A9.8 |
| 45.8 | Fire group shape as one chain; no berserk unit in a multi-Location group; Opportunity Fire by ordnance; a Gun crew's inherent FP; FPF with unmarked units; MG Bore Sighting | A7.5, A7.54, A7.25, A7.353, A8.31, C6.44 |
| 45.9 | Fire package corrections: a crew's and a prisoner's Casualty Reduction, the A7.212 limit | A7.212, A7.302 |

Rows the plan's tasks do not cite, to place or move in the frame: A.7, A7.51, A7.7, A8.4, and the seven moved in. The page numbers are in the plan; take each page from the PDF or the inventory, never from the transcription's markers.

## What pass 40 left for this pass

Read the backlog's section 56 at the frame. Its rows that lie in this pass's ground, to take or leave with a reason:

- **One Mandatory FG of units whose fire counters differ (ruling R40.17).** The fire package gives one kind of Defensive fire to an attack (ruling R12.3), so a squad with no counter and one marked First Fire cannot fire together, though A8.312 says they must. The repair is a kind for each firer of an attack. Task 45.8's "FPF with unmarked units" (A8.31) is the same ground; design them as one. Until then the DEFENDER may pick the weaker group.
- **A7.353, a crew's halved FP after it fired its Gun** (ruling R8.4 as amended by R40.18): in FPF it adds its FP whole today. 45.8 names the row.
- **A Gun's shot, a thrown DC, and a Starshell are not held behind the attack owed** (ruling R40.19).
- **A SMC's Recovery at once** when a unit is eliminated, surrenders, or routs away (A4.44): an act in the other side's phase, and each loss must say which SW it left.
- **The leader's IPC (A4.42):** a hero's; the choice in the APh; a CX leader, who still lends it; a SMC as the one helped; the unasked default not kept in the record.
- **A4.41:** the German dismantled MMG or HMG firing as a LMG, which meets 45.7's dismantling.
- **A4.61 and A4.63:** one mover's break ends Assault Movement for the whole stack; the Dash's halving by firer in a Fire Group (against ruling R40.9, which stands; leave it); the overstack MF of the Location Dashed to.
- **A4.132:** the road's second target point is tested only inside the target hex. It is a LOS question; it may wait for pass 135.
- **An FPF whose NMC replaces a firer by ELR is refused at the commit** (UNIT-STATE-007, "is consumed and cannot act"). Found by a test with DR 11 and not followed. It is in the fire record's follow-ups, which 45.2 and 45.8 touch; reproduce it first.
- **The page offers no crew as a firer** (ruling R24.8), so a crew's FPF is reached only by the action, and pass 40 could not stage it in the Studio. If 45.8 builds a crew's fire, the page must offer it.
- **Rules decided in Play that the boundary review named and pass 40 left** (section 56.2): the woods-road exception for vehicles, the Dash's SMOKE and DC bar, what a Guard's Deployment at will waives, the recapture's exceptions. Take one when a task opens its file; no move for its own sake.
- **From pass 35's section 55.2, still open here:** the Gun's target list offers a Location with no LOS (the frame of pass 40 left it "for the pass that reworks the fire panels"); the TI rules for the SW and Deployment actions under A.7; the "Latest" line through phase changes.

## The proofs and the tools

In `E:\Archive\GitHub\dlandi\pass32-tools`:

- **The gate's scripts of pass 40,** to copy: `gate40-baseline.sh` and `gate40-gate.sh`. Change the baseline commit, the two grep checks (a member of pass 40 present, such as `ScenarioA1Stacking`; none of pass 45), the output folders, and the swept games.
- **The proof tool is changed** (`proof/Program.cs` and `proof-main/Program.cs`): a game the build cannot read gets one `UNREADABLE` line, and properties named in `PROOF_SKIP` (Type.Property, comma-separated) are left out of the dump. `gate40-proofs-again.sh`, `gate40-sweep-again.sh`, and `gate40-digest-mw.sh` are the runs that used it. **Plan the skip list before the gate, not after:** `pass40/new_properties.py` lists the properties the pass added from the diff; give the gate's side the list on its first run. Pass 40 ran the digest three times and the sweep three times for want of it.
- **A changed value in a property that existed** (pass 40: the list of moved weapons) is set aside on both sides in one further digest, to show nothing else moved.
- **`pass40/sweep_pairs.py`** pairs the two sweeps line by line and classes the differences by where they first differ; `pass40/store_events.py` prints a range of a stored game's events.
- **The store** holds 411 games after pass 40's gate. Bring it up to date with the live folder again before the baseline.
- **`pass40/`:** the game generators (`make_stack.py`, `make_tpbf.py`, `make_ovr.py`, `make_409.py`, and the rest; each writes a game on a VASL board by its blob id through `board_version.py`), `game_events.py`, `game_payloads.py`, `trx_failed.py` (set `PYTHONIOENCODING=utf-8`), the documents' scripts (`docs_inventory40.py --check`, `docs_rulings.py --check`, `docs_close.py`), and `cards_survey.py`. The inventory's count tables and the coverage counts are still rewritten by `pass35/session3/docs_inventory_tables.py` and `docs_coverage_counts.py`.
- **The rulebook's pages as text:** `pass35/session3/pages\pNNN.txt` (pages 43 to 253).
- **Boards:** the Studio loads any Verified VASL board; pass 40's games stand on boards 1, 3, 4, and 5. Board 3's [C9] is woods beside the Open Ground of [D9], and [D10], [E10], [F10] are Open Ground in a row.

**How a difference is read at the gate:** as in pass 35's design, sections 14.4 and 15. The pass changes results on purpose, so every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault. Pass 45 changes fire, so expect the sweep's `asl.game.fire` lines to differ in number; `proof-dump` (see the pass 40 prompt) names one that shows only as a digest.

## To carry from pass 40

- **Write every script with the Write tool, and nothing inline.** Pass 40 slipped five times: an inline `python -c`, two `sed -i`, a bare `python -` at a line's end, and a temporary line in the gate's source to read a refusal. Each was caught, and each cost a check afterwards.
- **A script of many edits checks every anchor before it writes any file.**
- **A refused proposal leaves its scripted dice in the queue.** Clear the queue (`#scripted-dice-clear`) when a game opens and after any refusal, or the next roll takes them.
- **The Studio keeps a game in memory.** A game file written again under the same id is not read again; write it under a new id.
- **A commit refused by the gate with no reason** (`play.commit-refused`) is the store's replay speaking; the diagnostics are in the append's result. Read them in a test of one, not by editing the gate.
- **A staged condition is not the state play makes.** A prisoner staged by `asl:captured` has no Guard; stage one with an `instance-captured` event, or make it by play. A unit staged with a fire counter keeps it through the phase changes of its Player Turn.
- **Before a rule forces an act, ask whether the act can be made.** The attack owed under A8.312 asks the fire plan first; a game must never stand still on a bar.
- **A read of the whole state from the page or a refusal can tell a side what it may not know.** Pass 40's reviews found four: a count over hidden units, a list with a Location left out, a refusal that named the other side's units, a vehicle under a "?". Ask of every new summary and refusal who may read it.
- **When a task grows past what was approved, or the page reads against the frame, stop with the reading and a proposal.** Pass 40 did not stop for the half-hex rule or the CE choice, and the user learned of both after the merge. The user accepted both; that is no leave to do it again.
- **Main may move under the branch.** Check `origin/main` before the gate and again before the merge. The merge is made in a temporary worktree (`git worktree add -b merge-45 E:/Archive/GitHub/dlandi/m45 origin/main`); git refuses the new folder as unsafe, so give each command `-c safe.directory=E:/Archive/GitHub/dlandi/m45` and change no global setting. Push `merge-45:main` with no force, remove the worktree and its branch, then watch the CI run to its end (about 13 minutes).
