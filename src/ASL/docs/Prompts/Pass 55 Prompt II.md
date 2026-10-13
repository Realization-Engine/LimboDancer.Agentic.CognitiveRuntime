# Pass 55, second session: the gate

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its last eight lessons are pass 55's. This prompt gives the state and what is open, and no procedure: where it and that file disagree, the file is right, and say so.

## The state, as of 2026-10-13

- **Pass 55 (Armored combat II) is built to the merge gate** on `feature/asl-backlog-pass-55`, cut from main at 846eb5a3: the frame, seven increments in seven commits, the five reviews' fixes (99456de6), and the documents in the commit after it, which is the branch's head. Nothing is pushed or merged. `git log --oneline 846eb5a3..HEAD` has the ten commits.
- **The design** is `Passes/ASL Unit Backlog Pass 55 Design.md`: section 14.1 lists what departs from the plan's entry and the frame, section 15 what was built, section 16 is the gate's and is empty. The review is `Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md`.
- **Main was at 846eb5a3 when the branch was cut,** with a green CI run on it and on the two pushes before it. A Codex session commits to main; it has not been read since the session's start. `main` is checked out in a Codex worktree (`C:/Users/dland/.codex/worktrees/formation-design-review`), and a second holds `asl-narrative-extensions`. An untracked `.codex/` folder appeared at the repository's root during the session. Leave all three alone.
- **The catalog is at 1.14.0** on the branch, with four new definitions. Games, cards, fixtures, and tests that name 1.13.0 were left as they are and load against it.
- **This prompt and `src/ASL/boards/` aside, the tree is clean.** No Studio of mine was running when the session ended.

## The user's word

On 2026-10-12 the user said of this pass: "I want you to run without my intervention up to the Gate." That word is used up: the session stopped at the gate and reported. The gate, the merge, the push, and any closing commit are each the user's to start, by name. Nothing here starts one.

If the user's first message starts the gate, the practice of that word carries through it only as far as the message says.

## Open to the user's veto

The user has not answered the pass's report.

- **What departs from the plan's entry:** the pass is about twice the plan's size (35 inventory rows, seven tasks); Gun Duels is a slice, and with equal Gun Duel DRM no duel is offered though the rule gives one; no Armor Leader counter was added, he is a morale and a leadership on his vehicle; the Canister counter is a Gun, and Canister is not fired as Defensive First Fire; a wall's TEM on an ordnance To Hit DR, missing before the pass, was left as found. The design's section 14.1 has the whole list and the backlog's section 59 the rows.
- **Rulings R55.1 to R55.33,** all made without the user, in the Passes Plan's section 5. R55.32 (a Hidden vehicle may not attempt Motion) came from the reviews' one BLOCKER.
- **From pass 50, still unanswered:** task 50.5 not built; rulings R50.1 to R50.14; the design's section 10.1 of that pass.

## Not yet run, and known before the start

- **The gate script has still had no full run.** `gate/pass55.conf` is written. `gate.sh` gained `BOTH_SKIP` in this session, for the catalog's identity, which is hashed into every state; the change has had a syntax check only. Expect a stage to fail on its first run for a reason of the script's own.
- **The baseline will differ from every earlier one** in that both sides leave out `GameState.Catalog` and `DefinitionReference.Catalog`. During the build the same comparison was made by the pass's own scripts and was clean eight times; those scripts and their base are in `pass32-tools/pass55` and are not the gate's.
- **The store holds none of the pass's games** until the baseline stage copies the 31 that `pass55.conf` names. Each replays to its end on the branch's head; main cannot read most of them.
- **The sweep will prove little of what the pass built:** a Canister aim, a named ammunition, a Motion attempt, and a duel's answer are asked for only with arguments the sweep does not send.
- **The whole Rules and Play projects, the Node viewport test, the chart supplement, the text list, and Docker have not run on this branch.** Classes were run by filter after every change; the four test projects that the pass touched build with warnings as errors.
- **The package digests changed with the catalog** (the four case matrices and packages are pinned again). Authoring's matrix tests passed after the change; Docker will be the first Linux read of them.
- **A page test of this pass tries spots until the gate accepts one** (a Canister shot on the small test board, in `PlayPageOrdnanceTests`). If it fails by turns under load, look there first.

## Start

Read the standing rules, then the design's sections 14 and 15 and the review. Check the state above against the repository before relying on it, main first. Then wait for the user's word.
