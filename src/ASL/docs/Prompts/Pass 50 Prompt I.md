# Pass 50, first session: Armored combat I

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Pass 45 (Infantry II) is merged into main as e5b6c0b5 and its documents closed as b4ce31ef, both pushed on 2026-10-12. The user said they would read the CI runs of those two pushes themselves; read them at the start (below). The main checkout still stands on the branch `feature/asl-backlog-pass-45` at 1a6e1e46, which is wholly in main. The branch `main` is checked out in a Codex worktree (`C:/Users/dland/.codex/worktrees/formation-design-review`), and a second Codex worktree holds `asl-narrative-extensions`; leave both alone. This prompt is untracked until the pass's first commit.

Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its last ten lessons are pass 45's. Then read pass 50's entry in `src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md` (section 22.1, "Pass 50: Armored combat I"), and the [pass 45 review](<../Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md>) with the design's sections 12 to 14 for how the last pass ended.

## The user's word for this pass: work alone up to the merge gate, then pause

The user asked for this prompt on 2026-10-12 with these words: "stress in it that your to work independently as much as possible up to merge gate and then pause."

**This is the rule of the session, above every stop the older prompts describe:**

- **Do not stop for the user between the start and the gate.** Not after the frame, not before a commit, not after the reviews, not for a ruling. The frame's questions are answered by their own proposals; each task is committed when it is done; the reviews' findings are judged and fixed or given backlog rows; the Studio check and the documents follow. Carry on through errors, refused proposals, failing tests, and locked files by diagnosing them.
- **Pause at the merge gate.** When the build, the reviews' fixes, the Studio check of the whole pass, and the documents are committed, stop and report. Do not start the gate, the merge, or the push: each is the user's to start. (If the user's first message of the session says to run the gate too, run it and stop before the merge.)
- **What working alone obliges.** Every decision the user would have made is written down where a veto can find it:
  - each ruling in the design as "decided under the user's word of 2026-10-12", numbered R50.n, and carried into the Passes Plan with those words;
  - **whatever the plan's entry or the frame placed in the pass and the build leaves out goes first:** first in its design section, first in the review, first in the report, each with its reason and its backlog row. Pass 45 left out six such things; the user learned of them from the report's first lines, which is the least that is owed;
  - a finding not fixed gets a backlog row.
- **The few things that still stop the session,** because no word given in advance covers them: an action that cannot be undone outside the branch (a force push, a deletion of the user's files or games, anything on `main`, on the Codex worktrees, or on the Studio at 5178); a change to the catalog's version or to a scenario card the user plays; a tool call the harness refuses with "no verdict". For anything else, decide, write the decision down, and go on.
- **Prefer the smaller build to the stop.** Where a task turns out to be a design of its own (pass 45: Spraying Fire in the MPh, five of six Fire Lane points), build the part that stands alone, leave the rest with its reason, and go on. Do not widen the pass and do not wait.

## What pass 45 did without the user

Pass 45 ran under the same kind of word. As of this prompt the user has not answered its report, so these are **still open to a veto**; build on them, and if the user's first message vetoes one, the design's section and the Passes Plan's row name it:

- **Rulings R45.3, R45.4, R45.5.** R45.3: a MG's Bore Sighting DRM against Infantry in First Fire only, fired alone. R45.4: the pass's result-changing rules apply to attacks planned from pass 45 on, by the fact `coweringMarksAndWeaponResidual`. R45.5: a squad with no Spraying Fire option adds no FP to its MG's spray. (R45.1, R45.2, and R45.6 came from questions the user answered yes.)
- **The departures from the frame** (design 12.1, backlog 57.1): ordnance HE's Residual FP to pass 110; Spraying Fire in the MPh; five of six Fire Lane points; the mortars' Repair number not manufactured, against the answer to F9; SW Self-Destruction; most of pass 40's movement rows to pass 75.
- **The closing commit on main** (b4ce31ef) was pushed after the merge without a word of its own. I told the user so. In this pass the merge is not reached, so the question does not arise; if a later session merges, ask for the closing commit by name.

## How this pass runs

1. **The frame, read-only, and no stop.** Read the pass's inventory rows (the rows of `ASL Rule Inventory A to E.md` whose pass column says Pass 50: 39 of them), each rule on its page of the registered PDF, the code each row names, and the backlog rows that point at this pass. Write the design's first sections in `src/ASL/docs/Passes/ASL Unit Backlog Pass 50 Design.md`: what the pass is, the rows by task, what is certain and what needs a run, what the frame found outside the rows, and the questions, **each with its analysis and its answer, decided under the user's word.** Group the tasks into increments. Commit the frame with this prompt, and go on.
2. **Then one increment at a time.** For each: its design section, then each task:
   - the rule read on its page, with the page cited; a rule's example is part of the rule;
   - the rule in Rules as a calculator over facts, with Play handing the facts over and writing the events;
   - MapStudio built into `bin/p31b` with warnings as errors, with my Studio stopped;
   - a small test game in my Studio on port 6671, written by a script kept in the tools folder, with a control game beside it under its own id, played with scripted dice and read back from its file for the event the game is for;
   - a Rules test, and a Play test where the planner writes events; say plainly when there is none and why;
   - the test classes the change touches, **and the Units test project whenever a record of the state or the projector changes**;
   - **the replay digest over the proof store, right after any commit that adds a fact to a record** (see "To carry from pass 45");
   - the commit. One commit a task, or one for tasks that share their files; no stop before it.
3. **The reviews,** once the last increment is committed: five read-only agents in one message (the referee in two halves, the table player, the Rules boundary, the UI and Blazor review), their briefs in a prompt file (`Pass 45 Review Briefs.md` is the form). Read the lines and the page behind each finding and judge it. Fix those with a contained fix; give the rest a backlog row. No stop.
4. **The Studio check of the whole pass** at 1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, and 320 pixels, in each side's view and the adjudicator's, by keyboard and mouse, with one full Player Turn at the largest and the smallest. Pass 45 walked the turn at both sizes and measured its new controls at every width, but did not walk a turn that used its new actions at 320; this one does, or says what it left. Reset the pane's emulated size afterwards.
5. **Before the documents: the strict build of the test projects the pass touched.** `dotnet format whitespace <csproj> --include <the pass's test files>`, and no constant array as an argument in a test (CA1861). Pass 45's gate stopped on both.
6. **The documents:** the review, the design's sections on the reviews and "As built", the rulings (R50.n in the Passes Plan), the inventory's rows and its two count tables, the coverage counts, the backlog's section 58 and the marks on older rows the pass closes, the plan's status, the time log, the lessons for the standing rules, and the memory.
7. **Pause at the gate, and report.** What departs from the plan's entry and the frame first; then the rulings open to a veto; then the reviews, the tests, the Studio check, and what was not done.

Be sparing throughout: narrow reads, one value from the page, the test classes a change touches and no wider. Read the clock (`date +%H:%M`) and only then write a time into the log.

## Start

1. `git fetch origin`, then show `git status`, `git log -3 --oneline origin/main`, and `git worktree list`. Confirm the tree is clean but for `src/ASL/boards`, `.claude`, and this prompt.
2. **Read the CI runs the last session left unread:** `gh run list --branch main --limit 3` from the repository's folder (the remote is `Realization-Engine/LimboDancer.Agentic.CognitiveRuntime`). The merge's run is 38295173548; the closing commit started one after it. If either is red, read its log before anything else. Look first at the MapStudio page test `AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked`: it is a known race between test classes (backlog 57.2) that failed once in three Docker runs. A red run for that test alone is rerun, not repaired on main; any other failure is told to the user at once, which is one of the stops this session keeps.
3. Cut the branch from main as it stands now: `git switch -c feature/asl-backlog-pass-50 origin/main`. Main may have moved; the user also commits there. Note the commit the branch is cut from: the gate's baseline is built from it. Leave `feature/asl-backlog-pass-45` and `feature/asl-backlog-pass-40` in place unless the user says to delete them.
4. Check that no Studio of the last session runs on 6671 (`preview_list`).
5. Then the frame, its commit, and the build, without stopping.

## What pass 50 is

Block 2 of the run order, the first of the armored combat passes, after 45 and before 55. The plan gives it 33 rows, no new counters (the PzKpfw IIIH and the T-34 M41 suffice; a Gun Duel needs none), 5:00 of building and 6:15 in total. The inventory now names 39 rows for it: earlier passes moved six there. Pass 45 by the clock on 2026-10-12: the build ended at 11:19, the reviews and documents at 12:10, and the gate ran from 12:10 to 15:02.

| Task | What it builds | Rules |
|---|---|---|
| 50.1 | Vehicle weapon states: per-weapon fire in a phase, malfunction and repair for MA, BMG, CMG, and AAMG, Recall with a disabled MA | D1.8, D1.81, D1.82, D3.5, D3.7 |
| 50.2 | BMG and CMG on the IFT, by VCA and TCA, with Case A as an IFT DRM, in Bypass too | D2.35, D3.51, D3.52 |
| 50.3 | The MA while moving: Bounding First Fire, Cases C to C4, Delay, a declared MP expenditure, a TCA or VCA change with MP or at the end of a fire phase | C3.22, C5.13, C5.3 to C5.35, C6.16, D2.11, D2.17, D2.18, D2.41, D2.42, D3.11, D3.12, D3.3 to D3.32 |
| 50.4 | Gun Duels | C2.2401 |
| 50.5 | MG fire against armor: To Hit, To Kill, and the Stun it causes | A9.6, A9.61, A9.611, D3.54 |
| 50.6 | Target Facing against a vehicle in the firer's own hex | D3.2 |

Rows the plan's tasks do not cite, to place or move in the frame: A5.132, A7.21, A8.311, A8.4, A9.3, C5.4, C6.1. The page numbers are in the plan; take each page from the PDF or the inventory, never from the transcription's markers. Task 50.3 is the largest by far (18 rules, 1:40): expect it to be an increment of its own, and expect to leave part of it with a reason rather than widen the pass.

## What earlier passes left for this pass

Read the backlog's sections 55 to 57 at the frame. Their rows that lie in this pass's ground, to take or leave with a reason:

- **A hit on another vehicle of a vehicle-overstacked hex (A5.132; pass 40):** the To Hit's target choice.
- **OVR Prevention (C5.64; pass 40):** a crew fires no Gun in FPF today.
- **A vehicle's MG at another level of its hex (A7.21; pass 45):** the page's Range tab says "no attack at another level of its hex" for an AAMG; 50.2 decides what a BMG, a CMG, and an AAMG may do there.
- **A7.211, TPBF against PRC from a higher Location (pass 45's reviews):** it is pass 65's; leave it unless 50.5 opens its member.
- **The CE test of a vehicle MG's repair** (one of the differing copies pass 32 kept apart; section 54 or 55): 50.1 is its pass.
- **The step bar of a TI vehicle in two places:** one Rules member read by `MoveBar` and the projector.
- **Residual FP and a vehicle (pass 45, task 45.3):** a vehicle is attacked by a Location's Residual FP once; Bounding First Fire's own Residual FP and a MG's against armor meet it in 50.3 and 50.5.
- **Ordnance HE's Residual FP (A8.25; pass 45, against its frame)** is pass 110's, not this pass's, though 50.3 works in the ordnance record. Leave it.
- **A crew offered as a firer on the page (ruling R24.8)** is still not built; a vehicle's crew is not the same question, but the fire panel's firer list is the same file.

## The proofs and the tools

In `E:\Archive\GitHub\dlandi\pass32-tools`:

- **The gate's scripts of pass 45,** for whoever runs the gate: `gate45-baseline.sh` (main from its own sources: build, text list, replay digest, sweep) and `gate45-final.sh` (the strict build, the proof tool rebuilt and its copy compared with `cmp`, the digest, Play and MapStudio, the sweep with its comparison). `gate45-gate.sh` has the whole suite, the Node viewport test, and the chart supplement. Change the baseline commit, the two checks (a member of pass 45 present, such as `ScenarioA1UpperLevelEncirclement`; none of pass 50), the output folders, the skip lists, and the swept games.
- **The proof tool** (`proof/Program.cs`; `proof-main/Program.cs` is the baseline's copy): `PROOF_SKIP` (Type.Property, comma-separated) leaves properties out of the dump, and **`PROOF_SKIP_JSON`** (new in pass 45) leaves named keys out of a payload kept as JSON, which is where a fire record's facts are. `pass45/new_properties45.py` lists the properties a pass added, as Type.Property, from the diff; it also lists positional parameters that only moved, so check each name against the base commit. **The tool calls the planner's members by name:** when a pass changes a member it calls (pass 45: `MayDismantle` became an instance member), the tool no longer compiles and silently keeps its old copies. Build it against `bin/p31b` once after the last task, long before the gate.
- **`pass45/sweep_pairs45.py`** pairs two sweeps by their key and groups the differences by how the answer changed; `pass45/sweep_fire45.py` does it for the Ready fire plans.
- **`pass45/debug_replay.sh` with `facts_diff.py`:** why a record's facts no longer match the state, named by its JSON path in a minute. It needs a temporary dump line in the record's check; remove the line before the commit.
- **The store** holds 461 games after pass 45's gate. Bring it up to date with the live folder again before the baseline.
- **`pass45/`:** `p45lib.py` (a game on a VASL board by its blob id: `Game`, `unit`, `weapon`, `phases`, `write`) and the generators that use it (`make_45*.py`, `make_setup.py`); `game_read.py` and `event_read.py`; `build_studio.sh`, `test.sh <Rules|Play|Units|MapStudio> <filter>` (builds into `bin/p45` and prints the failed tests), `commit.sh <message file> [new paths]` (it checks the branch by name: copy it and change the name); the documents' scripts (`docs_inventory45.py --check`, `docs_close45.py`, `docs_backlog_closed.py`, `docs_gate45.py`, `docs_merged45.py`). The inventory's count tables and the coverage counts are still rewritten by `pass35/session3/docs_inventory_tables.py` and `docs_coverage_counts.py`.
- **The Studio helpers** are kept in the pane's `sessionStorage` under `H`, `Hw` (overflow past the right edge), `Adv` (advance the phases), `Hshot`, `Hstep`, `Hmg`; evaluate them again after each navigate. A newly written game is opened by navigating to `/games/play?game=<id>`. The first move proposal after a load often returns no click; try it once more.
- **The rulebook's pages as text:** `pass35/session3/pages\pNNN.txt` (pages 43 to 253).
- **Boards:** the Studio loads any Verified VASL board; pass 45's games stand on board 3. Its [D10], [E10], [F10] are Open Ground in a row; a Hex Grain from [F10] runs [E10], [D9]. Its stairwell hexes are [L4], [M2], [M5], [R3], [R5], [R6], [S3], [T6], [U6]. Vehicles need roads and open ground: pass 35's vehicle games (`p35-tow2`, `p35-bog`, `p35-mud`) and `p30-armor` show boards that take them.

**How a difference is read at the gate:** as in pass 45's design, section 14. The pass changes results on purpose, so every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault. A digest and a sweep that differ in nothing after a rule pass are a void run, not a clean one.

## To carry from pass 45

- **A fact a pass adds to a record's facts must be taken from the record at replay for an older record.** Pass 45 added `berserk` to a firer, rebuilt it from the state, and stopped the user's recorded game `guards-06-dlandi` at revision 563; four whole test projects and five reviews had passed. Pass 50 adds facts to ordnance and vehicle records, which the user's armor games hold. For each new fact, write what the record's check does with a record that has none, and run the replay digest over the store right after that commit, not at the gate.
- **A rule that changes a result needs a fact in the record** (ruling R45.4, after R31.3): an attack planned from the pass on carries it, and an older record resolves as it was recorded. Ask of every such rule whether a recorded attack would now resolve differently.
- **A page test whose helper copies the page's arguments proves nothing about the page.** The MG's Bore Sighted Location was tested through a copy of `PlacementArguments` and never sent by the page. For a field the page sends, set it up by hand in the Studio and read the game's file.
- **Scripted dice of 11 malfunction a B11 weapon,** and a vehicle's MA has a B# too. Read the weapon's B# before choosing the DR, and read the game's file back for the event the game is for.
- **Write every script with the Write tool, and nothing inline.** Pass 45 slipped four times: an inline `python -c`, a bare `python -`, one empty heredoc, and `cd` in a compound command, which moves the session's folder. Use absolute paths and `git -C`.
- **Never an edit and the build or test that depends on it in the same response.** The user stopped pass 45 once for it.
- **A script of many edits checks every anchor before it writes any file.**
- **Clear the scripted dice** (`#scripted-dice-clear`) when a game opens and after any refusal. **The Studio keeps a game in memory:** write a changed game under a new id.
- **A read of the whole state from the page or a refusal can tell a side what it may not know.** Ask of every new summary, list, and refusal who may read it. In armored combat this is a concealed or hidden Gun and a vehicle under a "?".
- **Main may move under the branch.** Check `origin/main` before the gate. The merge, when the user starts it, is made in a temporary worktree as the standing rules say.
