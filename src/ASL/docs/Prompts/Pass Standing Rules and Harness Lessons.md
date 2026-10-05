# Pass Standing Rules and Harness Lessons

**Status:** Standing. Every session prompt for a pass cites this file. It belongs to no one pass and is not deleted when a pass ends.

**Kept by:** Claude, at the user's word of 2026-10-05. When a session changes how the work is done (a renamed project, a changed test or merge step, a new lesson), the session updates this file and drops what no longer holds.

**History:** the rules and lessons were first kept in `ASL Pass 30 Handover Prompt.md`, which passes 30b to 31d cited and which was deleted on 2026-10-05 (in git history at 038dca8). They are restored here and brought up to date.

## 1. How a pass runs

1. **Preparation, read-only.** Read the plan's section for the pass, the designs and reviews it builds on, the backlog sections it draws from, and the code it names. Write the design with its decisions and its questions. Stop for the user's answers.
2. **Build**, one commit a task, on the pass's own branch. Check each task in the Studio.
3. **Reviews**, read-only and in parallel: a referee (the rules and what a side may know), a table player, and for a page a UI and Blazor review. Fix what they find.
4. **The Studio check of the whole pass**, at 1920x1080, 1366x768, 1024x768, 683x384, and 320 pixels, by keyboard and mouse, in each side's view and the adjudicator's.
5. **The documents:** the review, the design's "as built" section, the rulings, the backlog section, the plan's status, and the rows the pass changes in the [rule inventory](<../ASL Rule Inventory A to E.md>) (one row a numbered rule) and the [rule coverage](<../ASL Rule Coverage.md>) (one row a section, its status computed from the inventory).
6. **The merge gate**, only once step 4 passes: the pass's tests written, the solution build with warnings as errors, the full local suite, CI's Node viewport test by hand, the chart supplement regenerated, the Docker Linux check.
7. **Stop before the merge** for the user's word. Merge with `--no-ff`, push, read the CI run. Stop after the merge and ask before starting anything else.

## 2. Standing rules

- **The Studio is the only test until the code is complete.** While code is being written and fixed, build MapStudio only and check the work in the Studio. Write and run no unit tests, and run no solution build, suite, `dotnet format`, chart supplement regeneration, or Docker, until the Studio check of the whole pass has passed. Tests that sit below the page (the planner, the projector, a rules package) may be written with the change they guard.
- **The rulebook first.** Read the rulebook PDF before anything that states a rule, and cite the rule and its page. The page comes from the PDF or from the [rule inventory](<../ASL Rule Inventory A to E.md>), never from the transcription's page markers, which run a page low for about 420 rules.
- **Rules live in the Rules project alone** (the user, 2026-10-05; the plan's section 19, decision 7). Every ASL rule is a calculator in `LimboDancer.Domains.Asl.Rules` that takes facts and returns a verdict. Play reads the state and the map, hands the facts over, and writes the events; it decides no rule. The Rules project references only Abstractions: never give it a reference to Units, Maps, or Play.
- **The branch.** Run `git branch --show-current` before every commit and merge. The branch `asl-narrative-extensions` is checked out in another worktree; leave it alone.
- **Staging.** Stage explicit paths only: `git -c core.quotepath=false diff --name-only` into a file, add any new file's path by hand, then `git add --pathspec-from-file`. Never `git add -A` or `git add src/ASL`. Never add `src/ASL/boards/` or `.claude/`. Never commit an image.
- **No force push.** Never stop Visual Studio.
- **Building.** Build with `DOTNET_CLI_USE_MSBUILD_SERVER=0`, `MSBUILDDISABLENODEREUSE=1`, `--disable-build-servers`, and `-m:1`.
- **The Studio.** Claude's Studio runs on port 6670, never on 5178, which is the user's. Never start the Studio while a build runs.
- **Time.** Read the clock at each boundary and log every sub-task in the [time log](<../ASL Unit Time Log.md>) as H:MM, with its estimate where there is one.
- **Writing.** No em-dashes, no emoticons, plain language. A hex is written in brackets, "[G4]"; a unit's tag is not.
- **Commits.** Messages end with the Co-Authored-By line.
- **Formatting.** Fix IDE0055 with `dotnet format whitespace <csproj> --include <files>`, never without `--include`. IDE0011 and IDE0040 are errors under `--warnaserror`: fix them with `dotnet format style <csproj> --diagnostics IDE0011 IDE0040 --severity info --include <files>`.
- **A side's view** (rulings R23.1 to R23.6, R31b.1, R31c.2, R31c.4, R31d.5). Every panel, list, record, map layer, table, preview, and refusal reads the view, never the full state. The hand-over clears everything of the last view.
- **Scripts and edits.** Never a Bash heredoc for a script or an edit. Write the script with the Write tool into the scratchpad and run it.
- **Out of scope.** Whatever a pass leaves out or simplifies gets a row in the [backlog](<../ASL Unit Backlog.md>). The rule passes build rules only: no legacy card is ported or made playable inside one (the plan's section 19, decision 5).
- **Do not over-engineer.**
- **If a tool call is refused with "no verdict" by auto mode,** do not route the same change through another tool. Say so and wait. Make one small edit before the build begins to confirm that writes pass.

## 3. Where things are

- **Documents:** only the working documents are at the root of `src/ASL/docs`. A pass writes its design and its review in `Passes/`. Other designs are in `Designs/`, reviews in `Reviews/`, requirements in `Requirements/`, plans carried out in `Plans/`. The README has the table.
- **Pass numbers** (since 2026-10-05): 32 and 33 are armored combat, with 33b night and winter; 34 and 35 are the DYO purchase, deferred; 36 to 43 are the other rule packages. The legacy card track (display batches D1 to D18) is deferred.
- **The Studio's routes:** `/` (Library), `/fidelity`, `/maps`, `/units/games` (the Game inspector), `/units/cards/edit`, `/units/lab`, `/boards/{name}` (for example `/boards/bd01`), `/games/play`, and `/games/replay`. There is no `/play`.
- **The rules projects** are `LimboDancer.Domains.Asl.Rules` and its tests; both are in the solution. Shared C# helpers of the Studio live in `Services`.

## 4. Harness lessons

**Building and running the Studio**

- The user's Studio on 5178 often holds MapStudio's usual build output, and it cannot be stopped from a session. Build Claude's Studio into its own folder (`-p:OutDir=bin/p31b/`, launch config `map-studio-p31b`) and build the gate into another (`-p:BaseOutputPath=bin/gate/`).
- The launch config uses `--no-build`. Build MapStudio first, with Claude's Studio stopped, then start it. The Studio serves `site.css` and the scripts from its build output.
- Claude's Studio and the user's share `src/ASL/boards` and the fidelity report cache.

**The browser pane**

- A hidden pane reports a width of 0: set a size with `resize_window` before measuring, and reset the emulated size after a width check. The first navigate after a Studio start may come back empty; call it once more.
- Coordinate clicks go wrong when the pane rescales. To click on the map, dispatch PointerEvents (pointerdown on the element under the point, pointerup on the board SVG) at the element's own client coordinates.
- Drive pages through JavaScript: set a value and dispatch `change`, wait about 2.5 seconds after a load, confirm `#play-handover-confirm`. To wait for a proposal or a commit, wait until `#play-status` changes and no longer starts with "Working". Do not read `#play-outcome` at once: the last proposal's outcome stays on the page. Keep each script under 45 seconds.
- Helpers set on `window` are lost at each navigate: keep them in `sessionStorage` and evaluate them again.
- Run a play test at the pane's full size, and say what a script's refusals and loops were from the audit log, not from a guess. Read a Target list by range before firing; never try targets blind.

**Scripts and the shell**

- Run a Python script as `python <file> < /dev/null`. In an edit script use exact old and new blocks, each asserted to match its expected count before anything is written, and write with `newline=""`.
- A `cd` inside a Bash call moves the session's working directory: use absolute paths, or `cd` back.
- `jq` is not on the Git Bash path. Compare JSON with Python (`json.dumps` with `sort_keys`).
- `git rev-parse --short` takes one ref per call. `git merge -F -` does not read standard input: write the merge message to a scratchpad file and pass its path.
- Agent worktree isolation fails here and leaves stray folders under `.claude/worktrees`: run read-only agents in the main checkout.
- A heredoc slips in by reflex, even an empty one (twice on 2026-10-05). Before sending a Bash call, look for `<<`. Edit a scratch script with the Edit tool, not `sed -i`.
- A large read splits well: dump the PDF's pages to one text file a page (PyMuPDF; `doc.get_toc()` gives the outline with titles and pages), give each read-only agent about 100 rows, an exact output shape (one pipe-separated line a row), and a findings file, and check by script that every row came back once. Nineteen such audits ran in 16 minutes. Say in the result which rows were checked by hand and which are an agent's.

**C#, Razor, and Blazor**

- Code analysis rule CA1716 rejects a namespace ending in `.Shared`.
- Razor parses an SVG `<text>` element inside a code block as its own text tag. Build such SVG as a `MarkupString` in C#.
- `dotnet format whitespace` writes auto-properties as multi-line `get; init;` blocks. That is the repository's style.
- Blazor renders a bool attribute as present or absent, so `data-*` flags need `.ToString()`. Lists that shift need `@key`. Nullable flow is lost inside nested Razor blocks: use `state!` where the compiler warns.
- C# raw strings cannot start or end with a quote: use escaped strings in tests.

**Tests**

- Play page bUnit tests need `context.UseViewport()` (`PlayMaps.cs`). It also registers `ImmediateProposals`, which keeps a proposal on the test's own thread; without it the tests race in the Linux container though they pass on Windows. A new Play page test class must call it.
- Map assertions read the layers the panel sent (`context.MapLayer("setUnits")`). Views change with `PlayViews.ViewAs`, games open with `page.OpenGame(id)`, and the DEFENDER passes with `page.PassAs(defender, Commit)`.
- A new action must be added to `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation`.
- After a commit the Play page opens the Actions tab under 1024 pixels and moves the focus to the actions: tests of the narrow layout must expect that.
- Only board 01 has real terrain in tests (`Board01Fixture`), and its fixture has no LOS data: use an `IFireLosReader` stub. `WideBoards` gives a page test a board of 12 by 6 hexes. The played games `guards-dl-01` and `p31c-tw` are fixtures.
- `dotnet test` at `-v:q` prints no failed test names: use `--logger trx` and read the file.

**The merge gate**

- Run the local suite one project at a time, with `--no-build`, after one solution build with `--warnaserror`. A combined background run is cut off at the time limit with nothing reported. Authoring takes about 12 minutes and Play about 10 to 16.
- CI runs `boardViewport.pointer.test.mjs`, a Node test. The local suite and the Docker image do not. Run it by hand at every gate.
- The chart supplement's command is in `.github/workflows/asl-authoring-ci.yml`, step "Regenerate bounded Scenario A1 chart supplement". Run it locally with `--no-build` after the solution build.
- Docker: `bash src/ASL/tools/docker-check/docker_check.sh <branch>` from the repository root, on the committed branch; about 15 minutes. Start Docker Desktop first if `docker info` fails. Read the per-project results, not only the last line. A Studio fix that was seen in the Studio needs no Docker check.
- If a merge on main fails ("Permission denied", or untracked files that would be overwritten), check that the leftovers match the branch (`git hash-object` against `git rev-parse branch:path`), remove them, restore any deleted file, and retry.
- After every push: `gh run list --branch main --limit 3`, then `gh run watch <id> --exit-status`.

**Measuring**

- Before saying a speed aim is met, measure on the largest saved game, not on the game the report came from.
- To profile the Studio: cut a game at a revision, time the page with a stopwatch, and sample the Studio with `dotnet-trace` installed in the scratchpad.
