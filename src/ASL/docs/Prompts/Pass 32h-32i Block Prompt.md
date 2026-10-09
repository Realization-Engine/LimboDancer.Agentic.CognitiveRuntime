# Pass 32.h and 32.i as one block: night, weather, Starshells, Snipers (S9) and the sequence of play with the A1 entry (S2), into Rules

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, main at 8c8f64a (pass 32.g merged as b6a3f80 on 2026-10-08). Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it throughout; its lessons of 2026-10-08 (the proof binding, the checked baseline, the text list's doubled literals) apply here.

## The task

Move every rule of slices S9 and S2 of the pass 32 design into the Rules project with no behavior change, as one block on one branch, `feature/asl-backlog-pass-32hi`, from main. 65 members: S9's 21 (`GamePlanner.Night.cs`, `.Starshells.cs`, `.Snipers.cs`, the projector's three) and S2's 44 (`GamePlanner.cs`, `.Proposer.cs`, `.Choices.cs`, the projector's eighteen, the entry packages' providers). New Rules files per the design's section 9: `ScenarioA1NightAndWeather.cs`, `ScenarioA1Starshells.cs`, `ScenarioA1Sniper.cs`, `ScenarioA1SequenceCalculator.cs`, `ScenarioA1SequenceModels.cs`; the entry packages' providers grow. The rules of the move are the design's: Rules references Abstractions only; calculators take facts and return verdicts; one-line forwards keep Play's public names; a read made only on some path crosses as a delegate; Locations cross as text or as indexes into the caller's table; condition changes cross as ordered `(UnitCondition, bool)` lists applied through Play's `ConditionChanges`; the projector's refusals cross as `RecordRefusal`; the order of checks and every refusal text are behavior; the request's parsing stays in Play; a text whose hole calls a Play local stays in Play.

## The user's decision of 2026-10-08 (how this block runs)

Build and text list at every commit; nothing else until the block's end. Then once: the test run (Rules, Units, Play), the proofs, the read-only review, the Studio self-test, the gate, the documents, and the stop before the merge. The user's go starts the build; the user's word merges. 32.j (S10) comes after, alone.

## The reading, in order

1. `src/ASL/docs/Passes/ASL Unit Backlog Pass 32 Design.md`: sections 5 to 9, 12, 15, and 22 (32.g as built: the shape every section follows, and the proofs' finding).
2. `src/ASL/docs/Passes/ASL Unit Backlog Pass 32 Design Appendix.md`: the rows of `GamePlanner.Night.cs`, `.Starshells.cs`, `.Snipers.cs`, `GamePlanner.cs`, `.Proposer.cs`, `.Choices.cs`, and the projector's S9 and S2 rows (the 32.f and 32.g sections name which projector members belonged to S2: `ChangePhase`, `End`, `KeepCx`, among others). Confirm the counts (21 and 44) before anything moves and say what differs.
3. The Play files named, whole, and the projector members named.
4. The Rules files earlier slices left reads for: `NightAndWeatherFacts`, `NvrOf`, `Illuminated`, `AddSniperAttacks`, `ExtremeWinterReduction` (named as "S9's" in sections 17 to 21), and `Pending`, `RollNode` ("S2's").

Preparation is read-only. Present the member list, the searches (S9 has one, S2 six), the delegates, the files, section 12's copies kept apart, and the commit order; stop for the go.

## Proof tooling (`E:\Archive\GitHub\dlandi\pass32-tools`)

- `textlist.py <out.txt> [src/ASL root]`: run at every commit and diff against the block's start list; a count that changes only because call sites became one function is accepted and recorded; a changed hole is not.
- The proof project `proof/` is bound to `bin/p31b/` (the flat OutDir output of Claude's Studio build); `proof-main/` to `main-bin/`. Rebuild each proof before it runs and check the copied DLLs' dates. For the gate's proofs build the proof with `-p:StudioBin=<repo>\src\ASL\LimboDancer.Domains.Asl.MapStudio\bin\gate\Debug\net10.0\` and `-p:OutDir=` into `proof-gate/`.
- The baseline: `git -C <repo> archive --format=tar -o <absolute path>/main.tar <commit>`, extract, grep the archived source for a member only that commit has, build with `-o main-bin`, rebuild `proof-main`, then `replay` and `sweep ... store p31c-tw` into `before-hi/`. `rebuild-baseline-g.sh` is the model.
- The store holds 277 games and 127 Tractor Works cuts (every fourth phase change and every phase entry of each kind). S2 touches the phase change itself: cut at every remaining phase entry if any kind is uncut (`Proof.dll cut <out> store p31c-tw <phase>`), and re-run the baseline over the enlarged store.
- Build Claude's Studio with `-p:OutDir=bin/p31b/` and the gate with `-p:BaseOutputPath=bin/gate/`; the ASL solution is `src/ASL/LimboDancer.Domains.Asl.sln`. Flags: `DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 --disable-build-servers -m:1 --warnaserror`. Never start a build while a test build or the Studio's build runs.
- The gate, in order: solution build into bin/gate; `node --test src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/boardViewport.pointer.test.mjs`; `chart.sh`; `suite.sh`; Docker (`bash src/ASL/tools/docker-check/docker_check.sh <branch>` on the committed branch); the gate-build proofs against the checked baseline.

## Documents at the end

The design's section 23 (32.h and 32.i as built, one section), the inventory's Where column by a script modelled on `inventory_where_32g.py`, the plan's section 22.2 status line, the standing rules' lessons, the time log (`src/ASL/docs/ASL Unit Time Log.md`, H:MM per sub-task, a row per commit). Commit messages end with `Co-Authored-By: GitHub Copilot <copilot@github.com>`.

## Standing rules that bite here

Stage explicit paths, never `src/ASL/boards` or `.claude`; the user's Studio on 5178 is never touched, Claude's runs on 6670 from bin/p31b; no force push; no em-dashes or emoticons in anything written; every open question comes with a proposal; stop before the merge.
