"""Pass 45 documents after the gate: the design's section 14, the review's status, two backlog rows, the lessons, and the time log's row.
Every target is read and checked before any is written.

Usage: python docs_gate45.py <start HH:MM> <end HH:MM> <the third Docker run's sentence>
"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end, docker3 = sys.argv[1], sys.argv[2], sys.argv[3]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)


def read(name):
    return open(DOCS + name, encoding="utf-8", newline="").read()


def once(text, old, new):
    assert text.count(old) == 1, (old[:80], text.count(old))
    return text.replace(old, new)


GATE = f"""## 14. The gate

Run on 2026-10-12 at the user's word ("Run the gate", then "Continue without my intervention, but stop before the merge"). The baseline is main at 5982767e, built from its own sources; the proof store holds 461 games, 50 of them new.

| Step | Result |
|---|---|
| The solution build with warnings as errors | Failed the first time on style alone: IDE0055 in the pass's Rules test file and CA1861 in two Play test files. Fixed in 98d3dfca, no product code. Then 0 warnings, 0 errors, and again after the fix below. |
| The whole local suite, from `bin/gate` | Dice 24, Rules 818, Units 409, Maps 244, Maps.Vasl 121 (30 skipped), Maps.Rendering 34 (2 skipped), Units.Rendering 352, CounterSheets 19, MapStudio 390, Play 742, Authoring 167: none failed. Play and MapStudio were run again after the fix below: 742 and 390. |
| CI's Node viewport test | 5 of 5. |
| The chart supplement | Equal. |
| The text list | 1,298 texts on main, 1,312 on the branch; 28 lines differ, each a text the pass added or reworded. |
| The replay digest | Every game made before the pass replays to the same digest, line for line, after the fix below. The 50 new games differ, as they must: main does not know their facts. |
| The planner sweep, 11 games, 515,179 lines | 2,021 lines differ, all of the dismantle action and its read: the refusal reworded (1,876), a MG that may now be dismantled refused for the phase instead (113), and the German HMG of `p31c-tw` dismantled where main refused it (16 plans, 16 reads). Every fire plan is equal once the fact of ruling R45.4 is left out of the record's JSON. |
| Docker, Linux | Passed on 98d3dfca: eleven projects, 3,320 tests. On df8005f9 the second run failed one MapStudio page test, `AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked`, which named a weapon by its id; {docker3} |

**What the gate found, and the fix (df8005f9).** The recorded game `guards-06-dlandi` stopped replaying at revision 563 (UNIT-STATE-024). One firer of that attack is berserk; the fact the pass added to a firer (`berserk`, for A7.54 and A7.9) was rebuilt from the state, while the record, made before the pass, has none. The fire record's check now takes the fact from the state for an attack planned from the pass on and from the record for an older one, as ruling R45.4 has the pass's other new facts. No test of its own: the recorded game is the fixture and the replay digest is the check. The pass's own tests and four whole test projects had passed with this fault in place; only the replay of a recorded game with a berserk firer could show it.

**What went wrong in the gate itself.** The first run's digest and sweep on the branch's side were void: the proof tool did not compile against the new build (`MayDismantle` is no longer static) and ran on the copies of the day before, so it reported no difference at all. The script printed the copies' dates and I did not stop it on them. The tool is corrected, and the script now stops unless the copy is the gate's build, byte for byte.

**The Docker failure** is a race between test classes, not the pass: `UnitNames.For` keeps the page's names in a static cache keyed by the game's id, its first event's id, and the viewer, and test classes that run side by side start games with the same id and the same first event id. The pass did not touch that code or that test. It has a backlog row (57.2).
"""
ROWS = [
    "| A page test can read another test class's unit names | Pass 45's gate, 2026-10-12 (Docker, second run) | (tests) | `UnitNames.For`: a cache key that tells two test games apart, or a cache the tests do not share | `AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked` failed once in three Docker runs, naming a weapon by its id; it passes locally. |",
    "| No test for an older fire record with a berserk firer | Pass 45's gate, 2026-10-12 | A7.54, A7.9; ruling R45.4 | A recorded fixture game with a berserk firer in `OlderGamesTests` | The replay digest over the proof store is the only check; `guards-06-dlandi` is the game that showed it. |",
]
LESSONS = """- **The gate's proof tool must stop when its copy is not the gate's build (pass 45).** The tool failed to compile against the new build, kept the copies of the day before, and reported no difference in 600,000 lines. A digest and a sweep that differ in nothing after a rule pass are a void run, not a clean one. `gate45-final.sh` compares the copy with `cmp` and stops.
- **A fact a pass adds to a record's facts must be taken from the record at replay for an older record (pass 45).** `FireFirer.Berserk` was rebuilt from the state and stopped a recorded game with a berserk firer; no test and no review saw it, the replay digest did. For each new fact, ask what the fire record's check does with a record that has none, and run the replay digest over the store right after the commit that adds the fact, not at the gate.
- **To see why a record's facts no longer match,** dump the two JSON texts from the check under a temporary environment variable, build into a folder of its own, and replay the one game (`pass45/debug_replay.sh`, `facts_diff.py`): the differing path is named in a minute. Remove the dump before the commit.
- **A fact kept as JSON in an event is not reached by `PROOF_SKIP`.** `PROOF_SKIP_JSON` (pass 45) names keys left out of a JSON payload's raw text on the gate's side; with it every fire plan of the sweep compared equal.
- **The strict build belongs before the documents commit.** IDE0055 and CA1861 in test files stopped the gate's first build; `dotnet format whitespace --include` on the pass's test files, and static fields for constant arrays, before the gate is started.
"""

design = read("Passes/ASL Unit Backlog Pass 45 Design.md")
review = read("Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md")
backlog = read("ASL Unit Backlog.md")
rules = read("Prompts/Pass Standing Rules and Harness Lessons.md")
log = read("ASL Unit Time Log.md")
plan = read("ASL Card Play and Map Studio Redesign Plan.md")
assert "## 14. The gate" not in design and "The gate's proof tool must stop" not in rules and "A page test can read another" not in backlog
design_new = once(design, "The gate's sweep and digest, Docker, and the Node viewport test are the gate's and were not run.", "The gate is section 14.").rstrip("\n") + "\n\n" + GATE
review_new = once(review, "**The gate has not been run, and nothing is merged or pushed:** the user said to stop before the gate.",
                  "The gate was run on 2026-10-12 at the user's word and passed after two fixes (design 14): style errors of the strict build in three test files (98d3dfca), and an older fire record with a berserk firer that no longer replayed (df8005f9). **Nothing is merged or pushed:** the user said to stop before the merge.")
review_new = once(review_new, "- The gate, the merge, and the push: each is the user's to start.", "- The merge and the push: each is the user's to start.")
plan_new = once(plan, "**Stopped before the gate at the user's word; not merged.**", "The gate passed on 2026-10-12 after two fixes (design 14). **Stopped before the merge at the user's word.**")
backlog_new = backlog.rstrip("\n") + "\n" + "\n".join(ROWS) + "\n"
rules_new = rules.rstrip("\n") + "\n" + LESSONS
row = ("| The gate, at the user's word. The baseline from main at 5982767e (build, text list, replay digest 751 s, sweep 1,673 s); the gate's side three times: the strict build stopped on style in three test files "
       "(98d3dfca); the whole suite (eleven projects, none failed), the Node test, the chart supplement; a digest and a sweep that were void, because the proof tool had not compiled against the new build; "
       "the tool corrected and run again; the recorded game guards-06-dlandi found stopped at revision 563, its cause found by a dump of the two fact texts (a berserk firer's new fact), fixed (df8005f9); "
       f"the build, the digest, Play, MapStudio, and the sweep again; Docker three times; the documents | {start} | {end} | {minutes} min | "
       "No estimate. About forty minutes were lost to the void run: the script printed the stale copies' dates and I did not stop it. Docker's second run failed one page test on a race between test classes "
       "that the pass did not touch. Stopped before the merge at the user's word. |\n")
for new in (GATE, LESSONS, row, "\n".join(ROWS)):
    assert "\u2014" not in new
for name, text in [
    ("Passes/ASL Unit Backlog Pass 45 Design.md", design_new),
    ("Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md", review_new),
    ("ASL Unit Backlog.md", backlog_new),
    ("Prompts/Pass Standing Rules and Harness Lessons.md", rules_new),
    ("ASL Card Play and Map Studio Redesign Plan.md", plan_new),
    ("ASL Unit Time Log.md", log.rstrip("\n") + "\n" + row),
]:
    open(DOCS + name, "w", encoding="utf-8", newline="").write(text)
print("ok", minutes)
