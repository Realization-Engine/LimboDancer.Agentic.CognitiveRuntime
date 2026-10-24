# Common brief for a pass 65 frame agent (read-only)

You help frame "pass 65: Passengers, Riders, and crews" of an Advanced Squad Leader rules engine (C#/.NET, Blazor Studio) in the checkout `E:\Archive\GitHub\dlandi\LimboDancer.MCP` (branch `vehicle-crew-figure`, about to be merged into main; treat it as main). You are READ-ONLY: edit no file of the repository except the one output file your own brief names, run no build and no test, commit nothing, start no Studio.

## Context
- The pass's entry: `src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md`, search "**Pass 65: Passengers, Riders, and crews**" (tasks 65.1 to 65.9 with their rules and pages, and the items passes 55 and 60 moved in under it).
- The backlog `src/ASL/docs/ASL Unit Backlog.md` names pass 65 in about a dozen rows (search "Pass 65" and "pass 65").
- The rule inventory `src/ASL/docs/ASL Rule Inventory A to E.md`: one row a rule, with status and the code that builds it.
- The last pass's design, a model of form and of rulings: `src/ASL/docs/Passes/ASL Unit Backlog Pass 60 Design.md` (sections 2 to 6) and its code map `code-builders/pass-history/pass-60-armored-combat-iii/codemap.md`.
- The rulebook: one text file a physical page at `E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt` (three digits). Chart columns are scrambled in the text layer; the full PDF is `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, and a region can be rendered with PyMuPDF (`import fitz`) into your scratch folder `C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/4a85be88-d519-4d48-bcb4-3ee955f9f6b0/scratchpad/frame/` (never into the repository). The tool `code-builders/rulebook/render_page_clip.py` may help.
- Architecture (binding): every ASL rule is a calculator in `src/ASL/LimboDancer.Domains.Asl.Rules` that takes facts and returns a verdict; Play (`LimboDancer.Domains.Asl.Play`, `GamePlanner.*.cs`) reads state and map, hands facts over, and writes events; it decides no rule. State and records are in `LimboDancer.Domains.Asl.Units` (`State/`). A fact new to a record must be absent-safe: an old game replays exactly as it did, and a rule that changes the result of something recordable before the pass applies only where the record carries the pass's fact.
- Memory and performance (binding): `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`. Repeated derived answers about a state go through `GamePlanner`'s `MemoOf(state)` / `PlayStepMemo`; no per-unit `GameState.At` scans inside loops.
- Do not over-engineer: the pass builds the rules in the rows, not a general framework.

## Shell rules (a hook enforces them)
No heredoc, no `sed -i`, no multi-line `python -c`, no bare `python -`, no `cd` into a folder of the repository (use absolute paths), no `grep -r` over src/ASL in Bash (use the Grep tool). A script is a file you write into your scratch folder and run with `python <file>`.

## Your report (closing message)
Plain prose and short tables, no em-dashes. For each question: the answer, with file:line or rule (page) evidence, and where a decision is needed, a one-line proposal with its reason. Say plainly what you did not read.
