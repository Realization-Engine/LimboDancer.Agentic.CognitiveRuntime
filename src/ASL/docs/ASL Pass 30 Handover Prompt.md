# ASL Pass 30 Handover Prompt

**Status:** Revised 2026-10-03 at the start of the pass 30 session, from the draft written at the end of the pass 29 session. Nothing in it is authorized until the user says to start.

**Related documents:** the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>) (section 5, "Pass 30: Prepared setups"), the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 37 and 45.

## Points settled

Settled 2026-10-03 on Claude's recommendations, at the user's word.

- **Stops.** The session stops for the user three times: after the design (step 1), for the plans of each card in task 30.5 (step 3), and before the merge (step 7).
- **The plans' review.** Task 30.5's plans are reviewed one card at a time, so the remarks on one card shape the next card's plans.
- **The branch** is `feature/asl-backlog-pass-30`.
- **Backlog section 45.** No row joins pass 30. None of them touches setup.

## The prompt

Start pass 30, prepared setups, of the ASL Card Play and Map Studio Redesign Plan (src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md, section 5, "Pass 30: Prepared setups"; estimate 5:45, build 4:30). Check `git status` first (only src/ASL/boards/ should be untracked), `git branch --show-current`, and `git log --oneline -3`. Your memory notes pass-22b-handoff and card-play-dyo-plan have the background.

State: pass 29 and the UI batch are merged (904037e, 2026-10-03, CI green). Its design is src/ASL/docs/ASL Unit Backlog Pass 29 Design.md (sections 15 and 16 hold the reviews' fixes and the verification); backlog section 45 holds what it left out. Pass 29 already gave typed Locations a "Use E4 on board 01" button from the hex picked on the map (LocationField, cascading value PickedLocation), which is part of task 30.2.

Do, in order:

1. Preparation, read-only: the plan's pass 30 and section 15.11, the pass 29 design, backlog sections 37 and 45, the scenario card code (ScenarioCards, CardDraft, the built-in cards and their hashes), Play's setup (SetupPlacementEditor, SetupPlacementList, SetupPoolsTable, LocationField, the setup pools, ProposeSetup), and the card editor's CardMapPicker. Write the pass 30 design (src/ASL/docs/ASL Unit Backlog Pass 30 Design.md) with draft decisions and your questions for me. The design states, for each of the four built-in cards (Guards Counterattack, Gambit, Tractor Works, Armor Test), which side sets up first, and how you will read LOS and terrain offline for task 30.5. Stop and ask me those questions before building.
2. Build tasks 30.1 to 30.4 on branch feature/asl-backlog-pass-30. Write each task's tests with the task, but do not run them yet. Check each task in your Studio, then commit it on the branch, so the reviews in step 4 have a diff to read.
3. Task 30.5: make up to three setup plans for the first side of each built-in card (up to twelve plans), each grounded in the Studio's LOS and the card's Victory Conditions. Work one card at a time. For each card, write its plans into the design document as tables (the idea, what it gives up, the terrain facts, the placements), load each plan in your Studio, send me a screenshot of it on the map, and stop for my word. Keep only the plans I approve, and carry my remarks into the next card's plans. Add a test that the gate accepts every kept plan against its card's current hash, and a test that a card's hash is the same with and without its setups file. Commit the kept plans on the branch.
4. Run the referee (disclosure, rulings R23.1 to R23.4; a plan is offered only in the first side's view, R23.3) and the table player and UI reviews as read-only sub-agents in parallel against `git diff main...feature/asl-backlog-pass-30`. Fix their findings, check each in your Studio, and commit.
5. Run the Studio check of the pass: setup from a card with each kind of group (one-hex area, an area of several hexes, an entering group, a non-OB "?"), a chosen plan adjusted on the map and proposed, a stale plan refused with reasons, a plan for an earlier revision of the card offered marked as such, a user card with its own setups file, a card with no plans (setup by hand as before), a chosen plan cleared by the hand-over, a game saved before the plans were added opening unchanged, both sides' views and the adjudicator's, at 1920x1080, 1366x768, 1024x768, 683x384, and 320px, with the keyboard as well as the mouse.
6. Write the review document (src/ASL/docs/Scenario A1 Backlog Pass 30 Review <date>.md), the design's status, the plan's status for pass 30, and the backlog rows (what is left out goes to a new section 46). Update the time log and your memory notes.
7. Merge gate: the full local suite (the first run of the pass's tests; fix what fails), commit, the Docker check, the chart supplement regeneration, then ask me before the merge into main. After my word: merge --no-ff, push, check CI, and report the times.
8. Stop after the merge. Ask before starting anything else.

Standing rules:

- Run `git branch --show-current` before every commit and merge. On 2026-10-03 the checkout was moved to another branch from outside the session. The branch asl-narrative-extensions is checked out in another worktree; leave it alone.
- Stage explicit paths only. Use `git -c core.quotepath=false diff --name-only` into a file, add any new file's path to it by hand, then `git add --pathspec-from-file`. Never `git add -A` or `git add src/ASL`, never add src/ASL/boards/ or .claude/, and never commit images (a plan's screenshot goes to me in the chat, not into the repository).
- Build with DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1, --disable-build-servers, and -m:1.
- Run your own Studio on port 6670 (launch config map-studio-6670), never on 5178. If a running Studio blocks a build, stop it without asking (never Visual Studio itself). Never start the Studio while a build runs.
- Time every sub-task in src/ASL/docs/ASL Unit Time Log.md, as H:MM.
- Writing: no em-dashes, no emoticons, plain language.
- Commit messages end with the Co-Authored-By line.
- Fix IDE0055 with `dotnet format whitespace <csproj> --include <files>`, never without --include. IDE0011 and IDE0040 are warnings in src/ASL/.editorconfig (errors under --warnaserror): fix them with `dotnet format style <csproj> --diagnostics IDE0011 IDE0040 --severity info --include <files>`.
- Do not over-engineer.
- Respect a side's view (rulings R23.1 to R23.4): every panel, list, record, map layer, and table reads the view, never the full state; the hand-over clears everything of the last view.
- A setup plan places the card's fixed OB and never changes it. Adding plans never changes a card's hash or its saved games.
- Test only in your Studio while you write and fix code; run no unit tests, suite, or Docker until all the code is done and passes the Studio check in step 5. Only then apply the merge gate. Tests are written with their task and run first at the gate.
- If a tool call is refused with "no verdict" by auto mode, do not route the same change through another tool. Tell me and wait. Run the pass outside auto mode: on 2026-10-03 auto mode gave "no verdict" on every Edit, although Edit and Write are allowed in .claude/settings.local.json, and the edits passed once I left auto mode. Before step 2, make one small edit to confirm that writes pass; if it is refused, say so at once.

Pass numbering: 30 is prepared setups; 31 and 32 are DYO (deferred); the rule packages are 33 to 42, with 34b.

Harness lessons:

- The Studio's routes: / (Library), /fidelity, /maps, /units/games (Game states), /units/cards/edit, /units/lab, /boards/{name} (for example /boards/bd01), and /games/play (Play). There is no /play.
- The launch config map-studio-6670 uses --no-build. Build MapStudio yourself first, with the Studio stopped, then start it. The Studio serves site.css and the scripts from its build output.
- In the built-in browser, a hidden pane reports a width of 0: set a size with resize_window before measuring. The first navigate after a Studio start may come back empty; call it once more. Coordinate clicks go wrong when the pane rescales. To click on the map, dispatch PointerEvents (pointerdown on the element under the point, pointerup on the board SVG) at the element's own client coordinates.
- Drive pages through JavaScript: set a value and dispatch change, wait about 2.5 seconds after a load, confirm #play-handover-confirm. To wait for a proposal or a commit, wait until #play-status changes and no longer starts with "Working"; do not read #play-outcome at once, since the last proposal's outcome stays on the page. Keep each script under 45 seconds.
- Write Python edit scripts with the Write tool into the scratchpad and run them with `python <file> < /dev/null`. Bash heredocs that contain ''' or long quoted text break. Use exact old/new blocks, each asserted to match once, and write with newline="".
- Code analysis rule CA1716 rejects a C# namespace ending in .Shared. Shared C# helpers live in Services (DisplayText, ImmediateProposals).
- Razor parses an SVG <text> element inside code blocks as its own text tag. Build such SVG as a MarkupString in C# (see MapSlotPreview).
- dotnet format whitespace writes auto-properties as multi-line `get; init;` blocks. That is the repository's style.
- My Studio and the user's share src/ASL/boards and the fidelity report cache. The latest report (2026-10-03) is current: 235 verified, 1 failed (bd79).
- The local suite is three background runs after one solution build with --warnaserror: Play (615 tests, about 16 minutes), Rules (484, about 13), and the other nine projects in one loop (Authoring takes about 12 minutes). Run them with --no-build.
- Play page bUnit tests need context.UseViewport() (PlayMaps.cs). It also registers ImmediateProposals, which keeps a proposal on the test's own thread: without it, tests race in the Linux container although they pass on Windows. A new Play page test class must call UseViewport. Map assertions read the layers the panel sent (context.MapLayer("setUnits"), MapQuery). Views change with PlayViews.ViewAs, games open with page.OpenGame(id), and the DEFENDER passes with page.PassAs(defender, Commit). The inspector's ids are play-inspector-tab-los, los-source, los-target, los-check, los-result, and hex-units; the map pane is #play-panel-map. A new action must be added to PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation.
- After a commit the Play page opens the Actions tab under 1024px and moves the focus to the actions; tests of the narrow layout must expect that.
- Blazor renders a bool attribute as present or absent, so data-* flags need .ToString(). Lists that shift need @key. Nullable flow is lost inside nested Razor blocks: use state! where the compiler warns.
- `git rev-parse --short` takes one ref per call. `git merge -F -` does not read standard input: write the merge message to a scratchpad file and pass its path.
- If a merge on main fails ("Permission denied", or untracked files that would be overwritten), check the leftovers match the branch (git hash-object against git rev-parse branch:path), remove them, restore any deleted file, and retry. Never stop the user's Visual Studio.
- jq is not on the Git Bash path. Compare the chart supplement JSON with Python (json.dumps with sort_keys). The command is in .github/workflows/asl-authoring-ci.yml, step "Regenerate bounded Scenario A1 chart supplement"; run it locally with --no-build after the solution build.
- Docker: `bash src/ASL/tools/docker-check/docker_check.sh <branch>` from the repository root, on the committed branch; it takes about 15 minutes. Start Docker Desktop first if `docker info` fails. A test that passes locally can still fail there, so read the per-project results, not only the last line.
- To watch CI: `gh run list --branch main --limit 3`, then `gh run watch <id> --exit-status`; `gh run view --json` has no "jobs" field.
