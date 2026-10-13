# Pass 55, first session: Armored combat II

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it. Its section 0 is a checklist to read before each step; its section 1 says how a pass runs. This prompt gives the state and the orders, and no procedure: where it and that file disagree, the file is right, and say so.

## The state, as of 2026-10-12

- **Pass 50 (Armored combat I) is merged** into main as 85d628bd, its documents closed in e65194c2. Main is at 846eb5a3, the standing rules' checklist. All three were pushed; their CI runs were left for the user to read, and I do not know their results.
- **The main checkout is still on `feature/asl-backlog-pass-50`** at 1f551c69, which lacks main's last two commits. The local `main` ref lags: `main` is checked out in a Codex worktree (`C:/Users/dland/.codex/worktrees/formation-design-review`), and a second Codex worktree holds `asl-narrative-extensions`. Leave both alone. A Codex session commits to main: it moved once during pass 50's gate.
- **This prompt is untracked** until the session's first commit. `src/ASL/boards/` is untracked and stays so.
- **No Studio of mine was running** when the last session ended.

## The user's word

On 2026-10-12 the user asked what the next pass is, was shown the table below, and said: "Create a prompt with the above marching orders for the next chat session." That starts pass 55. Then, the same day: "I want you to run without my intervention up to the Gate."

- **Work alone to the gate, then stop.** The frame, the build, the reviews and their fixes, the Studio check, and the documents run without a stop for the user, the frame's own questions included. The gate, the merge, the push, and any closing commit are each the user's to start, by name.
- **A question that would have gone to the user goes to a specialized read-only sub-agent,** which researches and answers it (the user, 2026-10-11 and again 2026-10-12, as a standing practice for this mode). Judge its answer against the page and the code, decide, and go on. Each decision is a numbered ruling in the design, "decided under the user's word of 2026-10-12", open to a veto, and carried into the Passes Plan.
- **What departs from the plan's entry goes first** in the design's section, the review, and the report.
- **Still a stop:** anything on `main`, on the Codex worktrees, or on the Studio at 5178; a force push; a tool call refused with "no verdict".
- **This practice is not yet in the standing rules file.** Add it there with the session's first commit.

If the user's first message says otherwise, that message rules.

## The orders

**Pass 55, Armored combat II.** The plan's entry is in the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 22.1, "Pass 55": 20 rows of the rule inventory, planned at 4:55 in total. It needs pass 50 first, which is merged.

| Task | What | Rules |
|---|---|---|
| 55.1 | Special ammunition: APDS, the Supply Chart's months, Elite depletion, AP# and HE# | C4.3, C7.32, C8.11, C8.12, C8.2, C8.8 |
| 55.2 | Canister | C8.4 to C8.42 |
| 55.3 | Motion attempts | D2.401 |
| 55.4 | Ordnance and vehicle fire at another level, with Height Advantage | C2.6, B10.3, B10.31 |
| 55.5 | Armor Leaders in fire, OVR, Close Combat, and Bog Removal; Inexperienced Crews | D3.4 to D3.45, D7.16 |
| 55.6 | The counters: a Gun or AFV with APDS, one with Canister, Armor Leaders 8-1 to 10-2 | |

Two things carry into it from pass 50:

- **Gun Duels (C2.2401),** pass 50's task 50.4, was not built and was sent to pass 55. The plan's table does not list it. The frame adds it as a task of its own, with its rows and an estimate, and gives the pass's new total. Pass 50's design, section 6 (question F5) and section 10.1, says why it was left; the backlog's section 58.1 has its row.
- **The new gate script gets its first full run at this pass's gate** (the user, 2026-10-12: "We will test them on the next pass"). See "Not yet run" below.

MG fire against armor, pass 50's task 50.5, went to pass 60 and is not this pass's.

## Still open to the user's veto from pass 50

The user has not answered pass 50's reports. Build on them; if a veto comes, the design's section and the Passes Plan's row name what changes.

- Tasks 50.4 and 50.5 not built.
- What the [pass 50 design](<../Passes/ASL Unit Backlog Pass 50 Design.md>) lists in section 10.1: a BMG's hull pivot for a shot, a turret turned without fire at the end of a fire phase, "one hex for all weapons" (D3.51), the MA's Acquisition lost to a CMG's other target, D2.18, and the rest.
- Rulings R50.1 to R50.14, in the Passes Plan's section 5, all made without the user.

Several of these touch pass 55's ground (the MA's shots, Motion, Armor Leaders' DRM on a vehicle's fire). Where the frame builds on one, say which.

## Not yet run, and known before the start

- **The gate script** (`E:/Archive/GitHub/dlandi/pass32-tools/gate/gate.sh`; the standing rules' section 0, point 2, and section 1, step 6). Its skip list from the diff, its `trial` stage, and the refusal of `proofs` without a passed trial were tested on pass 50's data (`gate/selftest.sh`). Its `baseline`, `build`, `suite`, and `docker` stages, and the long runs of `proofs`, have had a syntax check only. Expect a stage to fail on its first run for a reason of the script's own; repair the script, not a copy of it. Pass 55 needs its own `gate/pass55.conf`.
- **The trial's catch is narrow.** On pass 50's data it found the fault through five lines of one game. Choose this pass's trial games so that they hold what the pass changes.
- **The page tests' race** (backlog 57.2) was removed for one class by a game id of its own; the cache it comes from is unchanged. A new page test class names its game after itself.
- **A Bash hook is installed** in this repository's local Claude settings and refuses a heredoc, `sed -i`, a multi-line `python -c`, a `cd` into a folder of the repository, a `git add` of everything, and a force push. A refused call is a rule broken, not a fault of the tool.
- **The proof store** holds 473 games, twelve of them pass 50's.

## Start

Read the standing rules, then the plan's entry for pass 55 and pass 50's design, sections 10 to 12. Check the state above against the repository before relying on it, main first. Then the pass begins with its preparation and runs to the gate under the user's word above, where it stops and reports.
