# Common brief for a pass 71 building agent

You build one increment of "pass 71, Infantry IV: Snipers and wounds" of an Advanced Squad Leader rules engine. You work in the main checkout `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, on the branch `feature/asl-backlog-pass-71`.

I am the session that briefs you. I review your diff, check the work in the Studio, and commit. You do NOT commit, stage, switch branches, push, or touch `main` or any other worktree. Your closing message is your report (see the end).

## Read first
- The design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 71 Design.md`, sections 2 to 7. The rulings R71.n bind you; do not re-decide them. If one cannot be built as written, stop that part, say so in the report, and build the rest.
- The frame agents' reports beside this file: `frame_triggers.md`, `frame_state.md`, `frame_targets.md`, `frame_wounds_codemap.md` (its sections 5 to 7 are the code map: the test helpers, the Play page, the registries, the memo and index). Verify a line before relying on it.
- The rule on its page before any code that states it, with its examples. The page texts are one file a physical page at `E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`. A chart's columns are scrambled in the text; render a region from the PDF (`E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, PyMuPDF, `code-builders/rulebook/render_page_clip.py`) into the scratch folder `C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/447748d5-be7b-4ec1-8fc5-5406ad5c19eb/scratchpad/build/`, never into the repository. Cite the rule and its page in code comments the way the surrounding code does. Before writing code that says something is not attacked, not lost, or never happens, read that sentence of the rule on its page.
- `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`, its design checklist.

## Hard rules
- **Rules live in `LimboDancer.Domains.Asl.Rules` alone.** Every ASL rule is a calculator there that takes facts and returns a verdict. Play reads the state and the map, hands the facts over, and writes the events; it decides no rule. LOS and range stay in Maps. The Rules project references only Abstractions. Tests of Rules reach its public members only.
- **Absent-safety.** Every fact you add to a record (an event payload, a facts record kept as JSON) is nullable or defaulted, and `JsonIgnore` when null or default, so a game recorded before the pass reads and replays exactly as it did. A positional record gains a fact as an `init` property.
  - A rule that changes the result of something recordable before the pass applies only when the record carries the pass's fact for it.
  - That fact is taken from the record at replay, never recomputed from the state.
  - For each new fact, say what the projector's record check does with an old record that lacks it.
  - A new event type: main cannot read a game that holds it (the gate's tool writes UNREADABLE and goes on); that is expected.
  - Before naming a new JSON key, grep the Units and Play sources for the same name already written by an older record type (pass 70's gate stopped on five such names).
- **A side's view.** Every panel, list, record, map layer, preview, and refusal reads the side's view (`GameView`), never the full state. A fact on a record or a resume is a fact the reading side may not know: ask of each whether that side may know it (pass 60's lesson). Say in the report, for each thing you add, what the side that must not see is shown.
- **Memory and performance (design section 7).**
  - Repeated derived answers about a state go through `GamePlanner`'s `MemoOf(state)` and `PlayStepMemo` (its `ObjectIndex`). Never call `GameState.At`, `Aboard`, `Passengers`, or `Location` per unit inside a loop. No new cache outside that memo (or a board handle's memo for map facts). No disk read in a repeated page query. Never replay the history more than once in a plan where the state is already in hand.
  - A new memo store states its key, its lifetime (the state's), and that its fill has no side effect.
  - Say in the report what your code adds per state, per unit, and per render.
- **Style.**
  - Do not over-engineer. Match the surrounding code's naming, comment density, and idiom.
  - No em-dashes anywhere (code comments, texts shown to players, docs).
  - A hex in a player-facing text is written in brackets, like [G4].
  - A choice the page offers that names a counter names its hex first, "[AA6]: 4-6-7 squad A" (the hex UI rule; use the page's `UnitOptions` / `UnitSelectionList` / `SayAt` / `PlaceOfUnit`).
- **Shell.** Never a heredoc, `sed -i`, a `python -c` that writes files, a multi-line `python -c`, or a bare `python -`. Never `cd` into a folder of the repository (absolute paths, or `git -C`). Never `git add`. Never `grep -r` over src/ASL in Bash: use the Grep tool. A hook refuses these; a refusal is a rule broken, so do not route around it.
  - A script is a file written with the Write tool under `code-builders/pass-history/pass-71-snipers-wounds/` and run as `python <file> < /dev/null` (PYTHONIOENCODING=utf-8).
  - Files are LF, UTF-8 without BOM.
- **One step at a time.** Never send an edit and the build or test that depends on it in the same response. Never run two builds or tests at once (they share obj/). Do not edit a project's files while a build of it runs.
- **What you may touch.** Nothing under `src/ASL/boards/`, `.claude/`, or `.codex/`, and nothing outside `src/ASL`, `docs/ASL`, `code-builders/test-games`, and `code-builders/pass-history/pass-71-snipers-wounds`. Never start the Studio (I do). Never run a games script that writes under `src/ASL/boards` (I run them). Never stop Visual Studio or any process you did not start.
- **Never read the user's games.** The user's own Studio may run on port 5178 with a live game (`dl-tractor-works-03`): never open a `dl-tractor-works-*.game.json`. Never search `src/ASL/boards`: point the Grep tool's `path` at a `src/ASL/LimboDancer.*` folder or at `src/ASL/tests`, one at a time. **A `glob` such as `!boards/**` does not exclude the folder** (four agents of passes 70 and 71 read lines of live games that way).
- **Read your own diff.** After every Edit, the diff you made must be exactly what you meant: the editor has dropped clauses before. Before reporting, read `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP diff --stat` and skim the full diff of each changed file once.

## Building and testing
- **The rule for unit tests** (the user, 2026-10-23, the seventh time on record): "Only unit tests pertaining to the new code in the pass must be exercised. The full unit test run waits until the gate." No exception: no whole class, project, or suite.
  - Write the Rules and Play tests that assert what you build.
  - Before any test command, name the changed code, find the test METHODS that assert its behavior, and run those by method: `--filter "FullyQualifiedName~<Class>.<Method>|..."`. Never a whole class picked by a file name. Say the list in one line before running. Over about 25 methods, narrow first.
  - The other edge: when you lift a refusal, rename a text, add an example, an id, an icon, a kind, a modifier name, a result string, or a vocabulary entry, grep the tests for the refusal's code and text and for registry lists, and run the methods that lean on them. The existing Sniper tests (`BacklogPass15Tests`, `BacklogPass15TablePlayerTests`, `EntityTests`, the Rules Sniper tests) are in the blast radius of any change to `ScenarioA1Sniper` or `GamePlanner.Snipers.cs`: grep them for the members and texts you change.
- **Build:** `bash E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/build-and-test/build_studio.sh`. It builds MapStudio and everything under it outside the repository, with warnings as errors, in about 2 minutes, and prints the errors and "exit N". It must end with exit 0. My Studio is stopped while you build; if the build says a file is locked, stop and say so.
- **Tests:** `bash E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/build-and-test/run_test_classes.sh <Rules|Play|Units|MapStudio> "<filter by method>"`.
- **Code analysis under warnings-as-errors:**
  - CA1716 rejects parameters named `to`/`from` and members named `Step`.
  - IDE0040 wants `public` on interface members.
  - IDE0055 formatting is fixed with `dotnet format whitespace <csproj> --include <files>`, never without `--include`. Format the test files you touch before reporting: the strict build compiles them.
  - IDE0011 wants braces. CA1861 wants static fields for constant arrays in tests. xUnit2031 and the like fail the strict test build.
  - A `BoardLocation` is a class: use `!`, not `.Value`. A tuple element may not be named `Rest`.
  - `Assert.Equal` on a tuple holding a list compares the list by reference: compare the parts.
- **Where new tests go:** `tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass71RulesTests.cs` and `tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass71Tests.cs`; a page test class `PlayPagePass71Tests` in the MapStudio tests. Create them if absent, modelled on the Pass70 files beside them; add to them if present. The Sniper helpers of `BacklogPass15Tests` (`Sniper`, `Sides`, `SetupAt`, `Then`) are the model for a Sniper game.
  - Set games up with the catalog the code reads (`LiveFire.CatalogVersion`).
  - A Play test replays its game through the projector as the Pass70 ones do.
  - A new page test class must call `context.UseViewport()` and name its game after itself.
- **New things the registries must know** (code map, section 7):
  - a new action goes into `GameActions.All`, the planner's dispatch, and `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation`;
  - a new event type goes through `GameEventReader`/`GameEventWriter`, the projector, and the Studio's `Services/PlayRecords.cs` and `ReplaySteps.cs`, and Play.razor's latest text;
  - a new choice kind: the `ChoicePending` constants, `ChoiceKindOf`, `DescribeChoice`, the resume in `PlanChoose`, the projector's `PendChoice` list, `GameGate`, and `ChoiceOptionText` on the page;
  - a new condition a verdict carries needs its case in the projector's `ConditionName` switch (and the planner's);
  - a new button or tab id needs the Icon Lab's coverage file: run `IconCoverageTests.TheCoverageFileMatchesTheSources` with `ASL_ICONS_UPDATE_COVERAGE=1`, then without, and keep the file it writes.
- **Player-facing texts.** Proposal lines, refusals, and record lines are plain sentences that cite the rule, as the surrounding ones do. A refusal says what the player can do instead, where that is short.
- **Test games.** `code-builders/test-games/game_file.py` writes a Studio test game. It has no Sniper helper yet; the first increment that needs one adds `sniper(id, side, at)` and a SAN on a side (`started["sides"][i]["san"]`), and checks that the Studio loads a Sniper placed by `instance-created`. Check every LOS pair a games script relies on and write the result in its comment.

## Lessons carried from passes 65 and 70
- The Play page's unit lists go through `SeenOnly`, which leaves out units aboard unless asked (`aboardToo: true`). A list that must offer a Passenger, a Rider, or a crew inside must say so.
- Use the planner's index on the page too, never a per-unit `state.Aboard`/`state.At` in a render.
- A proposal that names a unit the plan creates must name it by its definition, never its raw id.
- A suggested route or LOS pair in a games script must be a legal one: read the panel's advice, or the board viewer's LOS tab, first.
- Ask, for each new fact on a public record, whether the other side may know it.

## Report (your closing message, under 900 words)
1. What departs from the brief or a ruling, first: anything you left out, simplified, or decided yourself, each with its reason.
2. What you built: files and members, new facts in records (with their JSON names), new events and actions, new texts.
3. The build's result and each test run's counts (exact lines), with the list of methods run.
4. For each new fact: what an old record does.
5. A side's view: for each thing you added, what the side that must not see is shown.
6. Memory and performance: what the code adds per state, per unit, and per render.
7. How to see it in the Studio: for each thing built, a tiny game on board 3 (which counters where, which phase, which dice), the element ids or button texts of anything you added to the Play page, and the JSON names to read back in the game file. If you can, write the games script (`games_4x.py`, using `code-builders/test-games/game_file.py`) but do not run it.
8. Anything you saw that looks wrong but is outside the task.
