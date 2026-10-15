# Pass 60, first session

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; then `code-builders/README.md`. This prompt gives the state, the user's word, and what is open, and no procedure: where it and those files disagree, the files are right, and say so.

## The state, as of 2026-10-14

- **Main is at 9cf2ab1f, green in CI.** Pass 55 (Armored combat II) is complete: merged as 50987ff3, documents closed in 3557744f. The checkout is on `gate-optimization`, which is merged into main and holds nothing main lacks. Local `main` is checked out in a Codex worktree and lags; leave it and the `asl-narrative-extensions` worktree alone, and the untracked `.codex/` folder.
- **This prompt is committed on main,** by itself, at the user's word of 2026-10-15.
- **The tools are in the repository now,** under `code-builders/` (the user, 2026-10-13, reversing the early rule not to commit them). Every script a pass writes goes into `code-builders/pass-history/<the pass's folder>` and is committed with the pass. What the tools write goes to the working folder `E:/Archive/GitHub/dlandi/asl-gate-work`, which was `pass32-tools` until 2026-10-13 and has been cleaned: it holds pass 55's baseline and the proof store, and nothing older.
- **The gate changed on 2026-10-13,** at the user's word, and the standing rules and `code-builders/merge-gate/gate.sh` say how: the local stages are the baseline, the strict build, and the proofs (text list, replay digest, sweep), about a quarter of an hour; the whole test suite and the Linux check are CI's, on the pass's pull request. The trial stage is gone, and the local suite and Docker are run on demand only. No pass has yet gone through this gate whole.
- **How a change reaches main** is a standing rule since 2026-10-13: a pull request and a green CI run for code, tools, the workflow, and any commit that adds, removes, or renames a file; straight to main only for text edits to existing documents. `code-builders/build-and-test/how_to_reach_main.sh` answers which.
- **The .NET SDK is 10.0.402** on this machine and in CI, and MapStudio's lock file was moved to it (53a22fcf). The Docker image `mcr.microsoft.com/dotnet/sdk:10.0` was still at 10.0.401 on 2026-10-13; it matters only for the Docker check on demand.
- **A Codex session commits to main,** lately under `src/ASL/docs` as well (two documents on a formation-to-ASL map alignment). A red CI run on main may be its or mine: read whose commit the failing test belongs to first.

## The user's word

On 2026-10-13 the user said that pass 60 runs as pass 55 did, without the user's intervention up to the gate, and then: "Do not start Pass 60 until I say." The mode is granted; the start is not. Cut no branch, write no frame, and run no stage of the gate for pass 60 until the user says to start.

Once started, the word ends at the gate, as pass 55's did. The gate's stages, the push of the branch and its pull request, the merge, and any closing commit are each the user's to start by name. Under the rule of 2026-10-13, once the user has said to push a branch, its pull request is opened without a further word.

## Pass 60, as the plan has it

**Armored combat III: other attacks on vehicles and Guns**, in the redesign plan (`ASL Card Play and Map Studio Redesign Plan.md`, the entry for pass 60): 37 inventory rows, no new counters, six tasks (60.1 Collateral Attacks and the Infantry Target Type against mixed Locations; 60.2 Indirect Fire against vehicles and Air Bursts; 60.3 FT, MOL, and DC against an AFV; 60.4 HE Equivalency and shaped charges against Personnel; 60.5 Guns as targets; 60.6 the rest of the To Kill tables), 4:40 of build and 1:15 of overhead by the plan. Items carried in by passes 40, 50, and 55 are listed under the entry and in the backlog's sections 58 and 59. Pass 50's task 50.5, MG fire against armor, is said to wait on pass 60 and has no row in the plan's table: the frame places it or moves it on.

The pass is likely about pass 55's size, not the plan's: pass 55's plan said 20 rows and the inventory held 35.

## Open, and known before the start

- **The time log:** the work of 2026-10-13 after pass 55's closing (the tools' move into `code-builders`, the gate's changes, the CI failures and their fixes) is not in the time log. Pass 55's design, section 16, records the gate and its repairs; nothing records the rest.
- **What pass 60 will ask of the new gate:** it changes what attacks of existing kinds resolve to (Random SW Destruction, Air Bursts), so recorded games must keep their results by a fact on the record, and the replay digest is where a miss shows. It admits fire that is refused today (60.1), so the sweep should show refusals turning into plans in older games: lines to read as intended changes, the sweep's first real work in a rule pass. The proof store holds no game with a DC. If the pass adds cases to a rule package, the packages are pinned again and `GameEvent.RulePackage` belongs in the pass's `BOTH_SKIP`, as pass 55 learned.
- **The baseline:** pass 55's cached baseline is of 846eb5a3 and does not serve pass 60, whose branch is cut from a later main.
- **Rulings still open to a veto** from pass 45: R45.3 to R45.5. Everything of passes 50 and 55 was approved on 2026-10-13.
- **On the user's side:** the restart the SDK installer asked for, and the Recycle Bin, which holds the working folder's old output.

## Start

Read the standing rules and the tools' README. Check the state above against the repository before relying on it, main first. Then wait for the user's word.
