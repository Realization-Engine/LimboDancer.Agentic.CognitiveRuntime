"""Pass 55's documents, the session's part, second script: the C13.8 inventory row, the review's first paragraph, and the lessons of pass 55
appended to the standing rules."""
import io

D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
INV = D + "ASL Rule Inventory A to E.md"
REVIEW = D + "Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md"
RULES = D + "Prompts/Pass Standing Rules and Harness Lessons.md"

inv = io.open(INV, encoding="utf-8", newline="").read()
rows = [line for line in inv.split("\n") if line.startswith("| C13.8 | Backblast |")]
assert len(rows) == 1, len(rows)
cells = rows[0].split(" | ")
ADD = (" Pass 55 (2026-10-13): the last sentence is built: a PF or PSK is refused at a target two or more levels higher in an adjacent hex and "
       "at a higher Location of its own hex (ScenarioA1OrdnanceMapRules.cs: BackblastElevationRefusal; ScenarioA1Pass55RulesTests; "
       "BacklogPass55Tests.APanzerfaustIsNotFiredAtATargetTwoLevelsUpInAnAdjacentHex). A LATW fired down within its own hex stays refused, "
       "stricter than the rule (backlog 59.2).")
# The note is the cell before the last (the audit cell); find it as the cell that holds "rulings" or is the longest of the tail.
note_index = len(cells) - 2
assert "Pass 55 (2026-10-13)" not in rows[0]
cells[note_index] = cells[note_index].rstrip() + ADD
new_row = " | ".join(cells)

review = io.open(REVIEW, encoding="utf-8", newline="").read()
R_OLD = "The Studio check of the whole pass is the session's to add, and the gate is not run."
R_NEW = "The Studio check of the whole pass is in its own section below. The gate is not run: it is the user's to start."
assert review.count(R_OLD) == 1, review.count(R_OLD)

rules = io.open(RULES, encoding="utf-8", newline="").read()
assert "\r\n" not in rules
LESSONS = """
- **A changed catalog version makes every digest line differ (pass 55).** The catalog's identity is hashed into every state, in `GameState.Catalog` and in each unit's `DefinitionReference.Catalog`. The first trial after the catalog went to 1.14.0 differed in 198 lines of 210. `gate.sh` has `BOTH_SKIP` for this, set in the pass's `.conf`: properties left out on the baseline's side as well. When every state's line differs, dump one state from each side before guessing: `PROOF_DUMP_STATE="<file>|<game>|<state number>"` writes it in full, and `pass55/dump_state.sh` shows where the two first differ. It named the property in a minute.
- **The replay digest during the build needs a base of its own (pass 55).** `pass55/base.sh build <commit>` builds main's sources and binds a proof tool to them; `base.sh replay` makes the base's digest once (682 s over 473 games); `replay_now.sh <tag> <store> <suffix>` trials six games in two seconds, and `replay_copy.sh <tag>` runs the whole store from the tool's copy while the next increment is built. Both sides run with one and the same skip list: the both-sides names and the output of `gate/new_properties.py <base>`, which names nothing the base holds. Seven increments and the reviews' fixes each had a clean digest before the next began.
- **An increment can be built by an agent to a brief (pass 55).** `pass55/brief_build.md` is the brief every building agent read first: the Rules boundary, absent-safety, the shell rules, the build and test scripts, what the report holds. The session wrote each task's brief from the frame's rulings and the research agents' findings, read the diff, played the games in its own Studio, and committed. Seven increments took 4:50 by the clock against 9:50 estimated. What made it work: one increment at a time (the projects share `obj/`); the Studio stopped before each agent built; the report's first section is "what departs from the brief"; and the session's Studio games, which found nothing the agents' tests had passed in this pass but did find two of the session's own script faults.
- **Ask the documents' agent and the fixing agent what they saw outside their task.** Two faults of pass 55 came that way, after five reviews had read the diff: an unrevealed Armor Leader in a duel answer's summary, and C13.8's bar on a PF or PSK two levels up, which opening fire between levels had left unguarded. When a refusal that covered many cases is lifted ("levels are not reviewed"), search the rulebook for every rule that refusal was standing in for, not only the rules of the task.
- **A game written to stage a hidden unit may not stage it (pass 55).** The engine reveals a hidden vehicle when an enemy moves into its LOS, so by the time the DEFENDER's window opened the T-34 of `p55-fix-hidden` was no longer hidden. Read the game's file for a `concealment-lost` before trusting what the page then offers.
- **A new game file is not in the Play page's list until the page is loaded again,** and a script that queues dice before finding that out leaves them for the next shot: clear the queue (`#scripted-dice-clear`) before every roll a script scripts, and wait for the panel's first control before using it.
- **The pane's real keys work when the pane is on screen (pass 55):** focus the button by script, then `computer key Return`. Say which controls were pressed by a key and which were set by script.
- **The slips of pass 55:** a bare `python -` appended to a Bash call by reflex (once; the hook refused it); a background chain whose second command lacked the environment the first script set inside itself (the 2a digest was run again from the copy); two building agents each sent an edit and its build in one response once, and each met the hook once for a heredoc. The hook caught every shell slip before it ran.
"""
assert "A changed catalog version makes every digest line differ" not in rules

io.open(INV, "w", encoding="utf-8", newline="").write(inv.replace(rows[0], new_row))
io.open(REVIEW, "w", encoding="utf-8", newline="").write(review.replace(R_OLD, R_NEW))
io.open(RULES, "w", encoding="utf-8", newline="").write(rules.rstrip("\n") + "\n" + LESSONS)
print("written: the C13.8 row, the review's first paragraph, eight lessons")
