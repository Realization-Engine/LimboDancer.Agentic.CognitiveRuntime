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
- **Rules live in the Rules project alone** (the user, 2026-10-05; the plan's section 19, decision 7). Every ASL rule is a calculator in `LimboDancer.Domains.Asl.Rules` that takes facts and returns a verdict. LOS and range stay in Maps, read by Play and handed to Rules as facts (the user, 2026-10-05). A calculator whose decision is a search may take a fact reader declared in Rules and implemented in Play. Units references Rules so the projector calls the same calculators. Play reads the state and the map, hands the facts over, and writes the events; it decides no rule. The Rules project references only Abstractions: never give it a reference to Units, Maps, or Play.
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
- **A question to the user carries its analysis and a proposal** (the user, 2026-10-06). Before a stop that lists open points, do the reading each point needs (the design's words and counts, the Appendix rows, the rule inventory and the PDF, the callers and the state lists in code) and write a one-line decision with its reasons. The stop asks for a veto, not for the work. A read-only agent may do the reading, before the stop, never after the user asks.
- **If a tool call is refused with "no verdict" by auto mode,** do not route the same change through another tool. Say so and wait. Make one small edit before the build begins to confirm that writes pass.

## 3. Where things are

- **Documents:** only the working documents are at the root of `src/ASL/docs`. A pass writes its design and its review in `Passes/`. Other designs are in `Designs/`, reviews in `Reviews/`, requirements in `Requirements/`, plans carried out in `Plans/`. The README has the table.
- **Pass numbers** (since the second renumbering of 2026-10-05): 32 is the Rules migration, in sub-passes 32.a, 32.b, and on; 33 and 34 are armored combat, with 34b night; 35 and 36 are the DYO purchase, deferred; 37 to 44 are the other planned rule packages, and 45 to 64 the ones section 22.1 adds. The legacy card track (display batches D1 to D18) is deferred.
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
- A heredoc slips in by reflex, even an empty one (twice on 2026-10-05, once more on 2026-10-06 as `python - <<'EOF'` with nothing in it). Before sending a Bash call, look for `<<`. Edit a scratch script with the Edit tool, not `sed -i`.
- The eight commits of a migration pass can all be drafted ahead as apply scripts (exact old blocks asserted once each, the new Rules code in draft files appended inside the class's last brace) while the first commits prove; each cycle is then apply, build, text list, stage, and the proof run, and the pass's build time is the proof runs alone (pass 32.c: eight commits in 1:47).
- Read-only agents cannot write a Markdown report file here: the harness refuses "report files" (13 of 14 on 2026-10-05). They can write a plain-text rows file. Ask for the findings in the closing message and keep a digest in the scratchpad.
- A tool built in the scratchpad against the Studio's build (bin/p31b) must set StudioOptions.BoardsRoot to src/ASL/boards, or it reads an empty cache folder under the user's local application data.
- The catalog version a game or a card records locks nothing (the user, 2026-10-05): UnitCatalogs.For reads the loaded catalog of the recorded name. Definitions are added and corrected, never removed; a correction may make an older record fail to reproduce, and the replay says so.
- A long proof or sweep is measured on a few games first, and its progress goes to a log file, never through a `tail` pipe that hides it until the end. On 2026-10-05 a sweep ran twelve minutes unseen and was restarted; the user's time was wasted. Each phase-change fixture of a planner sweep costs about 8 seconds with one request of each kind per unit, a whole game a few seconds.
- A scratchpad tool that wires the Studio's services must set `StudioOptions.VaslRoot` (`E:/Archive/GitHub/dlandi/vasl`) and `OracleFixtures` (the Maps.Vasl tests' Oracle folder) as well as `BoardsRoot`; without VaslRoot the planner reads no map, every request is refused for the wrong reason, and a before-and-after comparison proves nothing.
- The proofs of a migration pass (the pass 32 design, section 8; pass 32.a as built, section 16): the replay digest over the proof store, the planner sweep of one game (`p31c-tw`, at its last state, every fourth phase change, and every entry into the phase the moved action belongs to; one request of each kind per unit; the user's word, 2026-10-05), and the text list with interpolation holes normalized. The sweep's baseline comes from a build of main's own sources (`git archive` into the scratchpad, built into its own folder, a second copy of the tool bound to it). Cuts at every fourth phase change land on two phases only: cut by phase name for an action's proof. Per commit: the MapStudio build with `--warnaserror` into bin/p31b, then the three proofs; the test projects only at the gate, after the Studio check.
- A slice's sweep proves only the actions its swept game holds. Pass 32.c moved the Demolition Charge attack, the swept game has no DC, and a dropped `with` on a DC's targets passed eight commits of equal proofs and the read-only review; the gate's tests caught it. Before a slice, add to the store a game of each action kind the slice moves (a DC thrown and placed, a Fire Lane, Spraying Fire, an Encirclement), or run the slice's tests early with `dotnet test --filter` into a folder of their own (two minutes), and ask the review to compare every `with` of a moved builder property by property.
- A one-line forward is not always a move as it stood: a dictionary rebuilt in a fixed order is serialized in that order, and a record written to disk is behavior (the vehicle result conditions, pass 32.a). A read-only review of every moved member, old body against new, finds what the proofs miss; it costs four minutes.
- The Play page's checkbox lists repeat the same unit labels in several sections (Opportunity Fire, Fire, Movement, Advance). Check the boxes inside the section that holds the propose button, or the button stays disabled. `#play-status` keeps the last commit's text, so wait for it to change, not for it to be non-empty.
- A migration commit's proofs (about six minutes) overlap the next commit's drafting: stage the finished commit's paths once its text list has run, edit the working tree for the next, and build only once the sweep has released `bin/p31b` (the running tool holds the DLLs). Eight commits ran in under two hours that way (pass 32.b).
- A projector refusal moved to Rules carries its diagnostic code with its text (a code-and-text record), or the text list's count of the `UNIT-STATE-` literal drops with each `Fail` call folded away. A planner block whose checks sit between reads is one calculator function a block, called in the old order; a check that sits between two blocks (the SMOKE check of a step) keeps its place by splitting the calculator in two.
- Code analysis under `--warnaserror`: CA1716 rejects an interface parameter named `to` (or `from`), in a fact reader's interface too (pass 32.c, twice), and an interface member named `Step` (pass 32.f); IDE0040 wants `public` on an interface member; CA1859 asks a private member to take the array its one caller passes; IDE0055 wants a `switch` expression on several lines. `dotnet format whitespace` fixes the last.
- `BoardLocation` is a class, not a struct: a `BoardLocation?` is dereferenced with `!`, never `.Value`, and a `TryParse(out var x)` whose result is tested later needs `x!` at each later use, since the compiler does not carry the test. `GameState.RuleStateOf` is internal to Units: Play maps a `ConditionState` to `RuleState` with its own switch. Each cost a build try in pass 32.c.
- The text list cuts an interpolation hole short at a nested quote, so the hole's expression text is part of the listed literal: when such a literal moves to Rules, name the parameter as the old hole's expression (`name` for `{name ?? "unknown"}`), or the list differs on that line alone. When the expression is a call on Play's locals (`{(FirstMmcRallier(existing, state, unit.Side) is { } first ? ...)}`, pass 32.f), the text stays in Play and Rules decides which text applies as an enum.
- Cuts added to the proof store are games of their own: the baseline's replay digest and sweep must be run again over the new store before the comparison (pass 32.f: 15 new cuts, a replay of 230 s and a sweep of 16 minutes a side, the two sides run side by side).
- Do not edit a project's files while a background build of it runs: the build reads whatever is on disk and the commit's boundary blurs (pass 32.f: the next commit's Rules code was split out again by script and the build run twice). Draft the next commit's Rules code in the tools folder and append it once the build has ended.
- `Assert.Equal` on a tuple that holds a list compares the list by reference: compare the parts.
- A one-line text list check over the working tree (`textlist.py` into a scratch file, compared with `before/`) right after a commit's edits are applied, before its build, catches a renamed hole or a dropped literal eight minutes earlier than the proofs do.
- This shell does not expand `$'\r'`: `grep -c $'\r'` counts lines with the letter r. Test a file's line endings in Python (`"\r\n" in text` after reading with `newline=""`) and write back whichever it had.
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
- When `main` is checked out in another worktree (a Codex session's under `C:/Users/dland/.codex/worktrees`), or the user's uncommitted file blocks the switch, merge in a temporary worktree: `git worktree add -b merge-<pass> E:/Archive/GitHub/dlandi/m<pass> origin/main` (a short path on the repository's drive; the scratchpad's path is too long for the Oracle fixtures, and the temp drive fails the ownership check), run every command there as `git -c safe.directory=<path> -C <path> ...`, merge with `--no-ff -F <file>`, `push origin merge-<pass>:main`, then `git worktree remove --force` and delete the branch. The local `main` ref then lags until that worktree pulls; say so.
- After every push: `gh run list --branch main --limit 3`, then `gh run watch <id> --exit-status`.

**Measuring**

- Before saying a speed aim is met, measure on the largest saved game, not on the game the report came from.
- To profile the Studio: cut a game at a revision, time the page with a stopwatch, and sample the Studio with `dotnet-trace` installed in the scratchpad.
- After an edit tool applies a change, read every changed line of the diff before building. In pass 32.d the editor dropped a line's prefix, a lookup, or half an interpolated string in four of eight commits; the build caught each, but a dropped clause that still compiles would not be caught. Restore on disk and diff again.
- A move must not change an interpolated text or the cut of a hole. Run `textlist.py` before the build; when it differs, keep the text inline in Play (the Throw summary of pass 32.d moved to Rules and was put back).
- Line endings are LF: `src/ASL/.gitattributes` sets `text=auto eol=lf`. A file a tool wrote with CRLF shows as modified with no content change (`git diff --ignore-cr-at-eol --quiet` exits 0); never stage it for that alone, and restore it with `git restore` if it blocks a branch switch. Write files with LF and a UTF-8 encoding without a BOM.
