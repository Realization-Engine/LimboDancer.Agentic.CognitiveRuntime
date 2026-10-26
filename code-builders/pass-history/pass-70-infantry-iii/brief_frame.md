# Common brief for a pass 70 frame agent (read-only)

You help frame "pass 70: Infantry III: morale, Close Combat, and concealment" of an Advanced Squad Leader rules engine (C#/.NET, Blazor Studio) in the checkout `E:\Archive\GitHub\dlandi\LimboDancer.MCP` (branch `feature/asl-backlog-pass-70`, cut from main at 25aa97fd; treat it as main). You are READ-ONLY: edit no file of the repository, run no build and no test, commit nothing, start no Studio. Your findings go in your closing message.

## Context
- The pass's entry: `src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md`, search "**Pass 70: Infantry III" (tasks 70.1 to 70.10 with their rules and pages, and the two paragraphs after the table: items pass 65 moved in, and five rows moved from pass 75 on 2026-10-25).
- The session's prompt: `src/ASL/docs/Prompts/Pass 70 Prompt I.md` (what is open, and the memory and performance points).
- The backlog `src/ASL/docs/ASL Unit Backlog.md` names pass 70 in about fifteen rows (search "Pass 70", case-insensitive).
- The rule inventory `src/ASL/docs/ASL Rule Inventory A to E.md`: one row a rule, with status, the code that builds it, and what is missing. Pass 70 has 48 rows (the "Pass 70" cell).
- The last pass's design, a model of form and of rulings: `src/ASL/docs/Passes/ASL Unit Backlog Pass 65 Design.md` (sections 2 to 6) and its code map `code-builders/pass-history/pass-65-passengers-riders-crews/codemap.md`.
- The rulebook: one text file a physical page at `E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt` (three digits). Chart columns are scrambled in the text layer; the full PDF is `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`, and a region can be rendered with PyMuPDF (`import fitz`) into the scratch folder `C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/f12c93b0-4f22-4bda-84a5-33ccd7c35641/scratchpad/frame/` (never into the repository). The tool `code-builders/rulebook/render_page_clip.py` may help. A rule's examples are part of the rule: read them. Cite a rule with its physical page.
- Architecture (binding): every ASL rule is a calculator in `src/ASL/LimboDancer.Domains.Asl.Rules` that takes facts and returns a verdict; Play (`LimboDancer.Domains.Asl.Play`, `GamePlanner.*.cs`) reads state and map, hands facts over, and writes events; it decides no rule. LOS and range stay in Maps. State and records are in `LimboDancer.Domains.Asl.Units` (`State/`). A fact new to a record must be absent-safe: an old game replays exactly as it did, and a rule that changes the result of something recordable before the pass applies only where the record carries the pass's fact.
- A side's view (binding): every panel, list, record, map layer, preview, and refusal reads the side's view (`GameView`), never the full state.
- Memory and performance (binding): `src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md`. Repeated derived answers about a state go through `GamePlanner`'s `MemoOf(state)` / `PlayStepMemo` (its `ObjectIndex`); no per-unit `GameState.At` scans inside loops (the berserk charge's 11 s, PR 32).
- Do not over-engineer: the pass builds the rules in the rows, not a general framework.

## Shell rules (a hook enforces them)
No heredoc, no `sed -i`, no multi-line `python -c`, no bare `python -`, no `cd` into a folder of the repository (use absolute paths), no `grep -r` over src/ASL in Bash (use the Grep tool). A script is a file you write into the scratch folder above and run with `python <file> < /dev/null` (set PYTHONIOENCODING=utf-8 to print PDF text).

## Your report (closing message)
Plain prose and short tables, no em-dashes, under 2,000 words. For each question: the answer, with file:line or rule (page) evidence, and where a decision is needed, a one-line proposal with its reason. Give an estimate (H:MM of building by an agent who knows the code) for each task you cover. Name what you would move out of the pass, and to where. Say plainly what you did not read.
