# Common brief for a pass 65 building agent

You build one increment of "pass 65, Passengers, Riders, and crews" of an Advanced Squad Leader rules engine. You work in the main checkout `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, on the branch `feature/asl-backlog-pass-65`.

I am the session that briefs you. I review your diff, check the work in the Studio, and commit. You do NOT commit, stage, switch branches, push, or touch `main` or any other worktree. Your closing message is your report (see the end).

## Read first
- `code-builders/pass-history/pass-65-passengers-riders-crews/codemap.md`: where things are. Verify a line before relying on it.
- The design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 65 Design.md`, sections 2 to 6. The rulings R65.n bind you; do not re-decide them. If one cannot be built as written, stop that part, say so in the report, and build the rest.
- The rule on its page before any code that states it, with its examples. The page texts are one file a physical page at `E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`. A chart's columns are scrambled in the text, so its values are the design's (read from renders). Cite the rule and its page in code comments the way the surrounding code does.
- `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`, its design checklist.

## Hard rules
- **Rules live in `LimboDancer.Domains.Asl.Rules` alone.** Every ASL rule is a calculator there that takes facts and returns a verdict. Play reads the state and the map, hands the facts over, and writes the events; it decides no rule. LOS and range stay in Maps. The Rules project references only Abstractions. Tests of Rules reach its public members only.
- **Absent-safety (rulings R65.5, R65.16, and the design's section 4).** Every fact you add to a record (an event payload, a facts record kept as JSON) is nullable or defaulted, and `JsonIgnore` when null or default, so a game recorded before the pass reads and replays exactly as it did. A positional record gains a fact as an `init` property.
  - A rule that changes the result of something recordable before the pass applies only when the record carries the pass's fact for it.
  - That fact is taken from the record at replay, never recomputed from the state.
  - For each new fact, say what the projector's record check does with an old record that lacks it.
- **Memory and performance (ruling R65.25).**
  - Who is aboard a vehicle, and the Location of a unit, are read once per state through `GamePlanner`'s `MemoOf(state)` and `PlayStepMemo`'s `ObjectIndex`. Never call `GameState.At`, `Aboard`, `Passengers`, or `Location` per unit inside a loop.
  - No new cache outside that memo. No disk read in a repeated page query.
  - Say in the report what your code adds per state, per unit, and per render.
- **A side's view** never shows what that side may not know.
- **Style.**
  - Do not over-engineer. Match the surrounding code's naming, comment density, and idiom.
  - No em-dashes anywhere (code comments, texts shown to players, docs).
  - A hex in a player-facing text is written in brackets, like [G4].
  - A choice the page offers that names a counter names its hex first, "[AA6]: 4-6-7 squad A" (the hex UI rule; use the page's `UnitOptions` / `UnitSelectionList` / `SayAt`).
- **Shell.** Never a heredoc, `sed -i`, a multi-line `python -c`, or a bare `python -`. Never `cd` into a folder of the repository. Never `git add`. Never `grep -r` over src/ASL in Bash: use the Grep tool. A hook refuses these; a refusal is a rule broken, so do not route around it.
  - A script is a file written with the Write tool under `code-builders/pass-history/pass-65-passengers-riders-crews/` and run as `python <file> < /dev/null` (PYTHONIOENCODING=utf-8).
  - Files are LF, UTF-8 without BOM.
- **One step at a time.** Never send an edit and the build or test that depends on it in the same response. Never run two builds or tests at once (they share obj/). Do not edit a project's files while a build of it runs.
- **What you may touch.** Nothing under `src/ASL/boards/`, `.claude/`, or `.codex/`, and nothing outside `src/ASL`, `docs/ASL`, and `code-builders/pass-history/pass-65-passengers-riders-crews`. Never start the Studio (I do). Never stop Visual Studio or any process you did not start.
- **Read your own diff.** After every Edit, the diff you made must be exactly what you meant: the editor has dropped clauses before. Before reporting, read `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP diff --stat` and skim the full diff of each changed file once.

## Building and testing
- **The rule for unit tests** (the user, 2026-10-23): "Only unit tests pertaining to the new code in the pass must be exercised. The full unit test run waits until the gate."
  - Write the Rules and Play tests that assert what you build.
  - Run those methods only, by method: `--filter "FullyQualifiedName~<Class>.<Method>|..."`. Never a whole class picked by a file name, never a project.
  - Say the list in one line before running.
  - The other edge: if you add an example, an id, an icon, a kind, a catalog definition, or a vocabulary entry, grep for the tests that count or draw them and run those methods too (the lesson of PR 33: an example count, the pack version, the "crowded" layer golden, the layer manifest).
- **Build:** `bash E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/build-and-test/build_studio.sh`. It builds MapStudio and everything under it outside the repository, with warnings as errors, in about 2 minutes, and prints the errors and "exit N". It must end with exit 0. My Studio must be stopped while it builds: if the build says a file is locked, stop and say so in the report.
- **Tests:** `bash E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/build-and-test/run_test_classes.sh <Rules|Play|Units|MapStudio> "<filter by method>"`.
- **Code analysis under warnings-as-errors:**
  - CA1716 rejects parameters named `to`/`from` and members named `Step`.
  - IDE0040 wants `public` on interface members.
  - IDE0055 formatting is fixed with `dotnet format whitespace <csproj> --include <files>`, never without `--include`. Format the test files you touch before reporting: the strict build compiles them.
  - IDE0011 wants braces.
  - CA1861 wants static fields for constant arrays in tests.
  - A `BoardLocation` is a class: use `!`, not `.Value`.
  - `Assert.Equal` on a tuple holding a list compares the list by reference: compare the parts.
- **Where new tests go:** `tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass65RulesTests.cs` and `tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass65Tests.cs`. Create them if absent, modelled on the Pass60 files beside them; add to them if present.
  - Set games up with the catalog the code reads (`LiveFire.CatalogVersion`).
  - A Play test replays its game through the projector as the Pass60 ones do.
  - A new page test class must call `context.UseViewport()` and name its game after itself.
- **New things the registries must know:**
  - a new action goes into `GameActions.All` and `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation`;
  - a new event type goes through `GameEventReader`/`GameEventWriter`, the projector, and the Studio's `Services/PlayRecords.cs` and `ReplaySteps.cs`;
  - a new condition a verdict carries needs its case in the projector's `ConditionName` switch.
- **Player-facing texts.** Proposal lines, refusals, and record lines are plain sentences that cite the rule, as the surrounding ones do. A refusal says what the player can do instead, where that is short.

## Lessons of the earlier increments (2a to 2c)
- **A new button or tab id** needs the Icon Lab's coverage file. Run `IconCoverageTests.TheCoverageFileMatchesTheSources` with `ASL_ICONS_UPDATE_COVERAGE=1`, then without, and keep the file it writes.
- **The Play page's unit lists go through `SeenOnly`,** which leaves out units aboard unless asked (`aboardToo: true`). A list that must offer a Passenger, a Rider, or a crew inside (fire, rout, Bail Out, re-crew, scrounge) must say so. In 2c the Rout panel and the director list missed Passengers until the session fixed them. Check in your report that each control you add actually lists the units it acts on.
- **Measure one Studio at a time** (the whole-pass check, 2026-10-24): several Studios and a profiler running together, all appending to the shared audit file, made a 6x seeming regression.
- **Use the planner's index on the page too:** `Live.Planner.VehicleRidden(state, id)`, never `state.Aboard` per unit in a render.

## Report (your closing message, under 900 words)
1. What departs from the brief or a ruling, first: anything you left out, simplified, or decided yourself, each with its reason.
2. What you built: files and members, new facts in records (with their JSON names), new texts.
3. The build's result and each test run's counts (exact lines), with the list of methods run.
4. For each new fact: what an old record does.
5. Memory and performance: what the code adds per state, per unit, and per render.
6. How to see it in the Studio: for each thing built, a tiny game on board 3 (which counters where, which phase, which dice), the element ids or button texts of anything you added to the Play page, and the JSON names to read back in the game file.
7. Anything you saw that looks wrong but is outside the task.
