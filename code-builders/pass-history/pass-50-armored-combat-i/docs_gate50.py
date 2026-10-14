"""Pass 50, the gate's result in the documents: the design's section 12 and status, the review's status, the plan's status, the backlog's row
57.2, the time log's row, and the standing rules' lessons. Every anchor is checked before any file is written."""
D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"

SECTION = """
## 12. The gate

Run on 2026-10-12 at the user's word ("Proceed with the gate"), which is the word for the gate and for nothing after it. The baseline is main at b4ce31ef, which had not moved, built from its own sources; the proof store holds 473 games, 12 of them the pass's own recorded games. **The gate passed. Nothing is merged or pushed.**

| Step | Result |
|---|---|
| The solution build with warnings as errors | 0 warnings, 0 errors, at the first try (702024df). |
| The whole local suite, from `bin/gate` | Dice 24, Rules 836, Units 409, Maps 244, Maps.Vasl 121 (30 skipped), Maps.Rendering 34 (2 skipped), Units.Rendering 352, CounterSheets 19, Play 748, Authoring 167: none failed. MapStudio 389 of 390 in the first run and in one of two more: the race of backlog 57.2, see below. After the fix (85685aac), 390 of 390 in four whole runs. Rules and Play ran whole for the first time on this branch. |
| CI's Node viewport test | 5 of 5. |
| The chart supplement | Equal. |
| The text list | 1,312 texts on main, 1,336 on the branch; 34 lines differ, each a sentence the pass added or reworded (the refusals of a vehicle's MGs, the Delay MP, the turret's turn, Bounding First Fire, the same-hex shot, the repair by weapon). |
| The replay digest | 93,169 lines on main, 93,266 on the branch. No line of a game made before the pass differs. 155 lines differ, all in eleven of the pass's twelve games, which main refuses at their first record of the pass (`weapons` or `mpInLos` on a vehicle firer, UNIT-STATE-024 and 033; `p50-repair2` at its repair by weapon, UNIT-STATE-028) and the branch replays to their end. `p50-repair-ctl2` holds nothing main cannot read and replays alike on both sides. The baseline's digest equals pass 45's last one line for line. |
| The planner sweep, 16 games, 515,356 lines on main and 515,769 on the branch | 8 lines differ in the games made before the pass, and 416 are new: the three games of the pass in the sweep (`p50-repair2`, `p50-bff-move2`, `p50-mg-turn2`), which main cannot bring to their last state. The 8 are read below. |
| Docker, Linux | Passed on 85685aac at the first run: eleven projects, 3,344 tests, the two source regenerations equal. |

**The 8 differing lines of the sweep,** each a refusal that gave way to a later one, in `p35-bog`, `p35-bog-recall`, and `p35-mud`:

- A tank named as a firer on the IFT was refused for the hexside terrain at the target (ruling R10.5); it is now asked for the BMG or the CMG that fires (3 lines), or refused as Abandoned (1 line; D5.41).
- A vehicle's MA in its own MPh was refused by the DEFENDER's window; that refusal is gone, and the shot now meets the next check: the halftrack's MA is not in a turret (2 lines), the tank's target is outside its TCA for Bounding First Fire (1 line), the tank has no LOS (1 line).

**What the sweep does not show.** The sweep asks one request of each kind for each unit and names no weapon, so a tank's MGs on the IFT, the MA's Bounding First Fire with its cases, the Delay MP, the repair by weapon, and the same-hex shot are reached only in the pass's own games, where main has no answer to compare with. For those the proof is the pass's tests and Studio games, not the sweep. What the sweep and the digest do show is that nothing outside the pass's ground moved.

**What the gate found, and the fix (85685aac).** One MapStudio page test of `PlayPagePass31dTests` failed in two whole runs of three, a different test each time (`AFtIsSaidToFireAloneAndHoldsProposeWhenTickedWithAnotherUnit`, then `AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked`), each naming a weapon by another game's names. It is the race pass 45's gate met in Docker (backlog 57.2), now met on Windows while the baseline's sweep ran beside the suite: `UnitNames.For` keeps a view's names by the game's id and first event id, and `PlayPagePass35Tests`, which runs beside that class, starts games named "wide" with the same event ids. The pass did not touch that code, and its change to the Fire panel's row leaves a FT's and a MG's words as they were. The class's games are now named "wide-31d": a test-only change, no product code. The cache itself is unchanged and keeps its backlog row.

**What went wrong in the gate itself.**

- **The first sweep on the branch's side was misread by its own skip list.** The prompt asked for pass 45's `PROOF_SKIP` list beside pass 50's on the gate's side. That list was right while the comparison was with pass 45's last digest, made with the same list; against a baseline built from main, which holds pass 45, it left pass 45's properties out on one side only, and all 6,783 fire lines of the sweep differed in their hash. A full dump of one game's fire plans from each side named the three properties in a minute. The second run, with pass 50's twelve properties alone, is the one reported above. The first run's replay digest gave the same result as the second.
- **`PROOF_SKIP_JSON` was cut short by the shell** in the first run: a default written as `${NAME-...{}...}` ends at the first closing brace. The script now assigns the value in single quotes and prints it.
- **The proof tool was changed in the gate:** `key{}` and `key[{}]` in `PROOF_SKIP_JSON` leave out only a flat object or an array of flat objects, so that the older `weapons` of a fire request, an array of strings, stays in the text; and `PROOF_DUMP_FIRE` writes each fire plan in full. My first edit did not compile; an early build of the tool, before the baseline had ended, showed it.
- **The suite ran beside the baseline,** against the prompt's "first and alone", to learn of a failure early. It slowed the baseline's replay from about 750 s to 961 s and brought the race to light. Docker ran beside the first replay and passed.
- **The first session's `PROOF_SKIP` list of twelve is complete.** `new_properties50.py` over `b4ce31ef..HEAD` lists 47 names: the twelve, the members of the pass's five new records (reached only through the twelve), and eight older members of two records the pass reformatted, which must not be skipped.

**The store** now holds the twelve recorded games of the pass. Those labelled SUPERSEDED were not taken.

**Still open to a veto,** as before the gate: tasks 50.4 and 50.5 not built; what section 10.1 lists; rulings R50.1 to R50.14.
"""

LESSONS = """
- **A skip list belongs to a baseline (pass 50's gate).** `PROOF_SKIP` names what the branch has and the baseline lacks. A baseline built from main already holds the last pass's properties: carrying that pass's list onto the gate's side leaves them out on one side only, and every fire plan differs. Take the list from the diff between the baseline's commit and the head, and drop the members of records the diff only reformatted.
- **Never write a default with braces inside `${NAME-...}`:** the shell ends it at the first closing brace. Assign in single quotes and print the value at the script's start.
- **When every line of one action differs by hash alone,** dump one small game's plans in full from each side (`pass50/dump_fire.sh <game>`, `PROOF_DUMP_FIRE`) before guessing; the differing properties are named in a minute.
- **Compile the proof tool against the gate's build as soon as that build ends,** not when the baseline is done: a tool that fails then costs the baseline's forty minutes nothing.
- **Two page test classes that start games of one name share `UnitNames.For`'s cache** and fail by turns under load (backlog 57.2). A new page test class names its game after itself.
- **The sweep proves little of a pass whose requests need a new argument.** Pass 50's sweep differed in 8 lines outside the pass's own games; a tank's MGs, the Delay MP, and the repair by weapon are asked for only with arguments the sweep does not send. Say so in the gate's section, and count on the tests and the Studio games for them.
"""


def edit(path, pairs, append=None):
    text = open(D + path, encoding="utf-8", newline="").read()
    assert "\r\n" not in text, path
    for old, new in pairs:
        assert text.count(old) == 1, (path, old[:60], text.count(old))
        text = text.replace(old, new)
    if append:
        assert text.endswith("\n"), path
        text += append.lstrip("\n") if not append.startswith("\n## ") else append
    return path, text


out = []
out.append(edit("Passes/ASL Unit Backlog Pass 50 Design.md", [
    ("**The gate is not run:** it is the user's to start.", "**The gate was run on 2026-10-12 at the user's word and passed (section 12); nothing is merged.**"),
    ("**The gate is not run.** It is the user's to start.\n", "**The gate** is section 12.\n"),
], SECTION))
out.append(edit("Passes/Scenario A1 Backlog Pass 50 Review 2026-10-12.md", [
    ("**The gate is not run, and nothing is merged.**", "**The gate was run on 2026-10-12 at the user's word (\"Proceed with the gate\") and passed; nothing is merged.** The design's section 12 has its table. It found one thing, outside the pass: the race between two page test classes over unit names (backlog 57.2), met locally for the first time and removed on the tests' side in 85685aac. No product code changed in the gate."),
]))
out.append(edit("ASL Card Play and Map Studio Redesign Plan.md", [
    ("2026-10-12: built to the merge gate on `feature/asl-backlog-pass-50`, not merged.**", "2026-10-12: built on `feature/asl-backlog-pass-50`, the merge gate passed, not merged.**"),
]))
out.append(edit("ASL Unit Backlog.md", [
    ("`AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked` failed once in three Docker runs, naming a weapon by its id; it passes locally. |", "`AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked` failed once in three Docker runs, naming a weapon by its id; it passes locally. Pass 50's gate met it on Windows in two whole runs of three, in two tests of `PlayPagePass31dTests`, while a sweep ran beside the suite; that class's games now have their own id (85685aac), which parts it from `PlayPagePass35Tests`. The cache is unchanged. |"),
]))
out.append(edit("ASL Unit Time Log.md", [], "| The gate, second session, at the user's word (\"Proceed with the gate\"): the start checks (main unmoved at b4ce31ef); the gate's scripts; the baseline from main's sources (build, text list, replay 961 s, sweep 1,677 s); the strict solution build, clean at the first try; the whole suite beside the baseline; the MapStudio race found, read, and removed on the tests' side (85685aac); the Node test, the chart supplement, Docker; the proofs twice, the first sweep void by its own skip list (replay 1,117 s and 861 s, sweep 1,094 s and 1,446 s); the documents | 19:14 | %END% | %DUR% | No estimate. The baseline ended at 20:02, the first proofs at 20:40, the second at 21:26. About 40 minutes went to the second run of the proofs. The gate passed; the merge is the user's to start. |\n"))
out.append(edit("Prompts/Pass Standing Rules and Harness Lessons.md", [
    ("its last nine lessons are pass 50's", "its last nine lessons are pass 50's"),
] if False else [], LESSONS.lstrip("\n")))

import sys
end, dur = sys.argv[1], sys.argv[2]
for path, text in out:
    text = text.replace("%END%", end).replace("%DUR%", dur)
    open(D + path, "w", encoding="utf-8", newline="").write(text)
    print("written", path)
