# code-builders

The tools that build and check the LimboDancer codebase, and the record of the scripts that made each pass.

Until 2026-10-13 these lived outside the repository, by an early rule not to commit them. The user reversed that rule: the tools are part of how the code is made, and they belong beside it. `pass-history/code-builders-setup` holds the three scripts that brought them in.

## Layout

| Folder | What it holds |
|---|---|
| `merge-gate/` | The merge gate of a rule pass: `gate.sh`, its helpers, the text list, the test suite and chart supplement runners. `pass-configs/` has one `.conf` a pass. |
| `proof-tool/` | The C# tool behind the gate's replay digest and planner sweep, and its self-test. |
| `diagnostics/` | What names a difference once the gate has found one: `dump_both_sides.sh` writes a game's planned events, fire plans, or one state in full from each side; comparers of digests and sweeps; the reader of a failed test run. `svg-memory/` holds the SVG writer benchmark and the SVG and heap profilers of the SVG memory analysis. |
| `build-and-test/` | The Studio build, a filtered test run, the whitespace formatter, the commit helpers, and `how_to_reach_main.sh`, which says whether a branch needs a pull request and a CI run or may go straight. |
| `test-games/` | `game_file.py`, the library that writes a small Studio test game as a file (it replaces the libraries of passes 45, 50, and 55), and the readers of recorded games. |
| `rulebook/` | Page dumps, term and counter scans, and page renders of the rulebook PDF. |
| `documents/` | The multi-file edit helper; the rule inventory's reader by column name (`inventory_table.py`), which every inventory tool uses; the checkers of the rule inventory and the rule coverage; `pass_rules_status.py`, which writes each rule pass's Rules status line from the inventory and, with `--check`, stops the gate's strict stage when one differs; and `claim_check.py`, the claim board's check (the inventory's claims table against its rows, and a branch's changed Rules files against its claims), which CI and the strict stage run. |
| `boards/` | Hex facts, building and woods-road listings, the scenario cards' survey. `los-survey/` reads LOS over a card's setup areas with the game's own `LosCalculator` (build it outside the repository; its header says how). |
| `claude-hooks/` | The Bash guard that refuses the shell slips the standing rules name, and its test. |
| `pass-history/` | The one-time scripts of each pass, as they ran. |

`paths.sh` and `paths.py` say where the repository, the tools, and the working folder are. Every tool outside `pass-history` takes its paths from them.

## The working folder

What the tools write is not committed: the proof store of recorded games, baselines, digests, sweeps, logs, and a built copy of the baseline's sources. It goes to a working folder outside the repository, on a short path, since a built copy of the sources inside the repository has paths too long for the Oracle fixtures. The default is `E:/Archive/GitHub/dlandi/asl-gate-work`; set `ASL_GATE_WORK` to use another.

Three more settings have defaults for the machine the code is built on: `ASL_REPO` (set by `paths.sh`), `ASL_VASL_ROOT` (the VASL checkout the proof tool reads boards from), and `ASL_RULEBOOK_PDF` (the registered rulebook, which is not in the repository).

## The merge gate

```bash
bash code-builders/merge-gate/gate.sh <pass> <base commit> <stage>
```

The script's header lists the stages and their order. In short: `base` when the pass's branch is cut, `strict` before the documents commit, and at the gate `baseline`, `build`, and `proofs` on this machine, about a quarter of an hour. Then the branch is pushed and its pull request opened, and CI runs the whole test suite on Linux; `ci` says whether that run has passed. The `suite` and `docker` stages run the same tests on this machine and in a Linux container, on demand. A pass supplies `merge-gate/pass-configs/pass<pass>.conf`. The procedure a pass follows is in `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md`.

After a change to the proof tool, run `proof-tool/selftest.sh` before anything long.

## pass-history

One folder a pass, named for the pass. Each holds the scripts that applied that pass's edits, wrote its test games and its documents, and ran its gate before the gate was one script; the briefs and fix lists given to building and reviewing agents are there too. They are records: they name the paths they ran with, including the old tools folder, and are not kept up. To see how a change was made, read them; to make a new one, use the tools above.

## The Bash guard

`claude-hooks/asl_bash_guard.py` is the source of the hook. The copy that runs is the one `.claude/settings.local.json` names, under the user's `.claude/hooks`; after a change here, copy it there and run `asl_bash_guard_test.py`.
