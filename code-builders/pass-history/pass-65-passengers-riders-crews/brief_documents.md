# Brief for the documents agent of pass 65

You write pass 65's documents in the main checkout `E:\Archive\GitHub\dlandi\LimboDancer.MCP`, branch `feature/asl-backlog-pass-65`. You do not commit, stage, build, run tests, start the Studio, or touch code, `src/ASL/boards/`, `.claude/`, or `.codex/`. You edit only the documents named below, under `src/ASL/docs`. The shell rules of `brief_build.md` (beside this file) bind you: no heredoc, no `sed -i`, no multi-line `python -c`, no `cd` into the repository; a script is a file written with the Write tool into this folder and run as `python <file> < /dev/null` (PYTHONIOENCODING=utf-8), asserting every anchor's count before it writes any file, writing LF and UTF-8 without a BOM. No em-dashes anywhere; plain language; a hex in brackets, "[G4]".

## Read first

- The design, `src/ASL/docs/Passes/ASL Unit Backlog Pass 65 Design.md`, whole: sections 1 to 6 (the frame; rulings R65.1 to R65.25 in F1 to F24; R65.26 to R65.33 in F26 to F31, made at the tasks and the reviews). Section 3 places every row in an increment (2a to 2k).
- What happened, in this folder:
  - `notes.md`: the user's word and the timeline of every increment, review, fix round, the Studio check, and the measurement (read it whole; it is the source for the as-built sections);
  - the increment briefs `brief_2a.md` to `brief_2k.md`, `brief_reviews.md`, `brief_fixes.md`;
  - the five reviews' findings: `fixes_referee1.md`, `fixes_referee2.md`, `fixes_table.md`, `fixes_ui.md`, `fixes_boundary.md` (each finding's outcome is in `notes.md` and the fix commits);
  - `backlog_rows.md`: backlog rows gathered during the pass.
- The commits: `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP log --stat d0a8f4cd..HEAD` (7306e834 the frame, 0ccb9f4b to 17820400 the increments 2a to 2k, a0906fbf the reviews, fbca6ac1 to ca0e14fa fix rounds A to D). Where you need a code member or test name, read it in the code.
- Pass 60's documents as the form to follow: `Passes/ASL Unit Backlog Pass 60 Design.md` sections 7 to 15, `Passes/Scenario A1 Backlog Pass 60 Review 2026-10-15.md`, the backlog's section 60, the Passes Plan's R60 rows, the redesign plan's pass 60 entry and status line, and pass 60's inventory notes (rows naming "Pass 60 (2026-10-15)").
- The tools: `code-builders/documents/` (`inventory_rows.py --pass "Pass 65"`, `inventory_count_tables.py`, `verify_inventory_counts.py`, `coverage_counts.py`, `edit_files.py`, `append_fragment.py`, `check_document_line_endings.py`); run each checker with `--check` first to see the state before you change anything.

## Write

1. **The design's as-built sections,** appended after F31, in pass 60's form:
   - sections 7 to 17, one per increment 2a to 2k: what was built (members, record facts with their JSON names, page controls), its departures first, what an old record does, the tests that assert it;
   - section 18, the five reviews, their fixes (rounds A to D), and the Studio check (whole-pass, 1024 and 320 widths, the Russian side's view, the Enter key), with "what departs from the plan's entry and the frame" first, then fixed, then not fixed (each a backlog row);
   - section 19, as built, including the measurement: alone against the base Studio, hand-over 268/229/342 ms and proposal 118/167/158 ms against 280 to 320 and 147 to 164 ms; the seeming 6x regression was three Studios and a profiler appending to one audit file (notes.md has it);
   - section 20, the gate: not run; the user starts it.
   Renumber nothing that exists.
2. **The review,** a new file `src/ASL/docs/Passes/Scenario A1 Backlog Pass 65 Review 2026-10-24.md`, in pass 60's review's form.
3. **The rulings** R65.1 to R65.33 as rows of the Passes Plan's rulings table (`ASL Unit Backlog Passes Plan.md`, section 5), after the R60 rows, in their form: "Pass 65; decided under the user's word of 2026-10-23 ("start on Pass 65 without my intervention"), open to a veto." Quote the word exactly as `notes.md` records it. R65.33 says it was revised at fix round B (Scrounging takes no leadership: A.10's ∆).
4. **The backlog's section 71, "Added by pass 65",** after section 70, in section 60's form: 71.1, what the pass left out or moved (every move of the design's section 2 and section 3's "Placed" column, each row with "Deferred by", "Rules", "Depends on", "What happens now", and the pass that takes it); 71.2, what the reviews and the build found and did not fix; merge in `backlog_rows.md`. Then correct older rows that named pass 65: mark what pass 65 built, and name the new pass of what it moved on.
5. **The redesign plan's pass 65 entry** (`ASL Card Play and Map Studio Redesign Plan.md`, about line 1746): a status line in pass 60's form (built to the merge gate on 2026-10-24, the gate not run; the inventory rows named against the plan's 63; the frame's estimate against the plan's; what moved to passes 70, 75, 100, 105, and others). Where a later pass's entry receives a moved item, add it under that entry as pass 60's moves were added.
6. **The rule inventory** (`ASL Rule Inventory A to E.md`): every row whose pass column is "Pass 65" (list them with `inventory_rows.py --pass "Pass 65"`), and any other row the pass's code touched (section 3 names them, A7.211, A7.308, A7.82, A11.611, A21.2, A26.12, D3.4, D3.43, D8.11, D9.1, D9.2, and the rest). Correct the stale rows that say there is no Carrier in the catalog, and any row that still records an Unconfirmed Kill the pass's section 4 corrects. For each: its status after the pass, its code and tests columns (members and test names, as other rows give them), its pass (blank when built; the pass that takes the remainder otherwise), and a note appended in the form "Pass 65 (2026-10-24): ...". Then the count tables by `inventory_count_tables.py`; `verify_inventory_counts.py --check` must pass.
7. **The coverage** (`ASL Rule Coverage.md`): the sections whose counts change, by `coverage_counts.py`, and its `--check` must pass; any text beside a section the pass changes.
8. **The prompt's wrong line,** `src/ASL/docs/Prompts/Pass 65 Prompt I.md`: the line saying that 1.17.0 games stop replaying after the catalog bump is wrong (a recorded catalog version locks no replay; ruling R65.4 relaxed the fire check). Correct it in place with a note "(corrected 2026-10-24, design section 5)".

Do not write the time log; I do that.

## Report (closing message, under 900 words)

What you could not place or decided yourself, first; each file changed with what changed; the checkers' results (exact lines), and `check_document_line_endings.py` on every file you touched; the count of inventory rows changed, by status before and after; anything in the design or notes you found inconsistent.
