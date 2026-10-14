# Pass 55: the gate's result in the documents. Every anchor is checked before any file is written.
D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
DESIGN = D + "Passes/ASL Unit Backlog Pass 55 Design.md"
REVIEW = D + "Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md"
TIMELOG = D + "ASL Unit Time Log.md"
RULES = D + "Prompts/Pass Standing Rules and Harness Lessons.md"

GATE = """Run on 2026-10-13 at the user's word ("start the gate and stop before the merge"), by `gate.sh` in its first full run. The baseline is main at 846eb5a3, built from its own sources; the proof store went from 473 to 504 games with the pass's 31 recorded games. **The gate passed.** Nothing is merged: the merge and the push are the user's to start. `origin/main` moved during the gate to 681e8bdb by one commit, 14 files, all under `prototypes/europe-hex-map`: nothing of the ASL solution and no file of the branch.

In the same message the user answered the pass's report: "All open proposals are approved." The departures of section 14.1, rulings R55.1 to R55.33, and pass 50's rulings R50.1 to R50.14 with its design's section 10.1 are no longer open to a veto.

| Step | Result |
|---|---|
| The solution build with warnings as errors | **Failed at the first try: 450 errors in four of the pass's test files** (440 of IDE0055, six of CA1861, one each of CA1822, CA1875, xUnit2013, xUnit2029). Fixed in 8862d209, a change to test files only; 0 warnings and 0 errors at the second try. See below. |
| The trial | Passed: five older games, 230 replay lines and 1,441 sweep lines equal on both sides. |
| The whole local suite, from `bin/gate` | Dice 24, Rules 897, Units 416, Maps 244, Maps.Vasl 121 (30 skipped), Maps.Rendering 34 (2 skipped), Units.Rendering 352, CounterSheets 19, MapStudio 395, Play 819, Authoring 167: none failed, each at its first run. Rules and Play ran whole for the first time on this branch. |
| CI's Node viewport test | 5 of 5. |
| The chart supplement | Equal. |
| The text list | 1,336 texts on main, 1,376 on the branch; 70 lines differ. Each is a text the pass added (Canister, the Motion attempt, the Gun Duel, fire at another level, C13.8, the Armor Leader's and the Inexperienced Crew's setup bars) or a raised count of a code it uses again. One literal is gone, `asl.a1.fire.levels-differ`, which task 55.4 removed. |
| The replay digest | 93,401 lines on main, 93,732 on the branch. No line of a game made before the pass differs. 387 lines differ, all in 28 of the pass's 31 games: main cannot read five of them (a `motion-attempted` or `gun-duel-voided` event) and stops the others with a diagnostic at their first record of the pass. `p55-al-bog`, `p55-al-shock`, and `p55-duel-none` replay alike on both sides. All 31 replay to their end on the branch with no diagnostic. `p35-acquire` carries its known diagnostic on both sides. |
| The planner sweep, 17 games, 515,357 lines on main and 516,111 on the branch | 758 lines are new, all in the four games of the pass in the sweep, of which main brings none to a state. 2,511 lines of games made before the pass have the same text and another hash; they are read below, and with their one cause left out on both sides no line of the 515,357 differs. |
| Docker, Linux | Passed on 8862d209 at the first run: eleven projects, 3,488 tests, the two CI checks. |

**The 2,511 differing lines of the sweep** are plans of `p31c-tw` (2,500), `p25-visual` (7), `p35-mud` (2), and `p35-smoke-hill` (2): moves, drops, fire, advances, dismantlings, rallies, repairs, routs, and one Ambush, each with the same status and the same words on both sides. A full dump of one game's planned events from each side (`pass55/dump_events.sh`, `PROOF_DUMP_EVENTS`) showed the one difference: the event's rule package reference. Increment 2a pinned the four packages again with the catalog (fire, ordnance, rally, close combat), so their digests changed, and every planned event carries its package's digest. The sweep was then run again on both sides with `GameEvent.RulePackage` left out as well (`pass55/sweep_nopkg.sh`): first eleven games, 33,392 lines, where 693 lines had differed and none did; then all 17, where no line of a game made before the pass differs and the same 758 are new. This is the second thing of the kind `BOTH_SKIP` was written for, and `pass55.conf` named only the first.

**What the sweep does not show.** A Canister aim, a named ammunition, a Motion attempt, and a duel's answer are asked for only with arguments the sweep does not send, and main has no answer to compare with. For those the proof is the pass's tests and Studio games. What the digest and the sweep show is that nothing outside the pass's ground moved.

**A false sentence of section 15 and of the review, corrected here.** Both said "the four test projects build with warnings as errors, 0 warnings". No log backs it. The pass's test script, `pass55/test.sh`, builds the test projects with `TreatWarningsAsErrors=false`, and the one strict build of the session, the Studio's, leaves the test projects out; the sentence came from an agent's report and was written down unchecked. The lesson that the strict build belongs before the documents commit was written after pass 45 and was a line of prose with no script behind it.

**What went wrong in the gate itself.**

- **The baseline's sweep ended in a null reference after 53 minutes and wrote nothing.** `SWEEP_EXTRA` named four of the pass's games; main cannot read `p55-mot-pass`, the replay mode passes over such a game and the sweep mode did not, and the tool writes its file only at its end. The trial did not meet it: it sweeps older games only. The check that would have, the pass's four games on main's side, took six seconds afterwards. The tool now writes `UNREADABLE` and goes on; the second run took 2,065 s.
- **`gate.sh` wrote the baseline's DONE marker after the crash,** so the proofs stage started against a baseline with no sweep. The second sweep ended before the gate's own, and the stage's comparison found it.
- **The stages ran side by side** (the baseline, the suite, Docker, then the proofs), and each ran two to three times as long: the baseline's replay took 1,855 s against 682 s alone, Rules 32 minutes against about ten. The last comparison ran both sides alone and together in 1,066 s each.
- **The proof tool was changed in the gate:** the sweep's guard above; `PROOF_DUMP_EVENTS`; and `GameEvent.RulePackage` as a name `PROOF_SKIP` may hold, since the events' digest wrote the reference outside the property walk. The replay digest and the stage's own sweep were made before the last two changes, which alter nothing unless asked for.
- **The gate by the clock:** 14:47 to 17:32, 2:45. About 13 minutes went to the failed build, 53 minutes of machine time to the lost sweep, and 25 minutes to the last comparison.

**Approved by the user during the gate, to be made after the merge and the push** (the user's word: "Make these changes after we have merged and pushed Pass-55"): the trial covers every game the long runs touch; the tool writes as it goes and turns a game's exception into a line; no DONE marker without the files; the baseline is made once and reused; proofs and Docker together, then the suite; the replay passes over cut files, which are 78% of its lines and repeat the games they are cut from; a rule pass sweeps `p31c-tw` at each of its eight phases at four points of the game, about 32 cuts of 127; a strict build script beside `test.sh`, which the documents' checklist requires; and a line in the standing rules that an agent's report is not evidence of a build or a test.
"""

REVIEW_GATE = """
## The gate

Run on 2026-10-13 at the user's word ("start the gate and stop before the merge"). **It passed;** the design's section 16 has the table, the differing lines, and what went wrong. In short:

- **The strict solution build failed at its first try** on 450 style errors in four of the pass's test files, fixed in 8862d209 (test files only). The sentence above that the four test projects build with warnings as errors was false when written: the pass's test script builds them with warnings not as errors, and no strict build of them had been run.
- The whole suite (eleven projects, none failed; Rules 897, Play 819), the Node viewport test, the chart supplement, and Docker on 8862d209 (3,488 tests) passed at their first run.
- The text list differs in 70 lines, each the pass's own. The replay digest differs only in 28 of the pass's 31 games; no game made before the pass replays differently.
- The sweep has 758 new lines, all in the pass's four games, and 2,511 lines of older games with the same text and another hash: the rule packages' digests, which changed with the catalog and are carried by every planned event. With that reference left out on both sides, no line of 515,357 differs.
- The baseline's sweep was lost once, after 53 minutes, to a game main cannot read; the gate took 2:45.

The user approved all that was open to a veto on 2026-10-13: the departures, rulings R55.1 to R55.33, and pass 50's R50.1 to R50.14. Nothing is merged; the merge and the push are the user's to start.
"""

TIME = """
## Pass 55: the gate

| Sub-task | Start | End | Duration | Notes |
|---|---|---|---|---|
| Second session, 2026-10-13: the standing rules, the design's sections 14 and 15, and the review read; the state checked (the fetch, origin/main at 846eb5a3, local main only behind) | not read | 14:47 | about 15 min | Stopped for the user's word. The start's clock was not read. |
| The gate, at the user's word ("start the gate and stop before the merge"): the baseline in the background; the strict build, failed on 450 style errors in four test files, fixed (8862d209), passed; the trial; the suite, the proofs, and Docker side by side | 14:47 | 17:07 | 2:20 | The baseline's sweep lost after 53 minutes to a game main cannot read, and run again (2,065 s). Suite, Docker, text list, and replay digest clean. |
| The sweep's 2,511 lines of same text and other hash: one game's events dumped from both sides, the rule packages' digests named, the sweep run again on both sides with the reference left out (eleven games, then all 17: no line differs) | 17:07 | 17:32 | 25 min | Both sides alone and together: 1,066 s each. |
| The gate's result in the documents | 17:32 | %END% | %DUR% | Stopped before the merge. Eleven tool repairs approved by the user, to be made after the merge and the push. |
"""

LESSONS = """
- **A changed package digest makes every planned event differ (pass 55's gate).** Pinning the rule packages again changes their digests, and every planned event carries its package's. 2,511 sweep lines of older games had the same text and another hash. `GameEvent.RulePackage` belongs in `BOTH_SKIP` beside the catalog's identity whenever a pass changes the catalog or a package; `PROOF_DUMP_EVENTS=<file>` dumps a game's planned events in full, and `pass55/dump_events.sh` and `sweep_nopkg.sh` are the two scripts.
- **Sweep the pass's own games on the baseline's side before the long run (pass 55's gate).** Main cannot read a game with a new event type; the sweep ended in a null reference after 53 minutes and wrote nothing, and the script still marked the baseline done. Four games on main's side take six seconds.
- **`test.sh` builds the test projects with warnings not as errors,** and the Studio's strict build leaves them out: nothing strict compiles a test file before the gate. The gate's first build met 450 style errors, and the review said the opposite on an agent's word. A sentence about a build or a test names the log it was read from.
- **The gate's stages side by side run two to three times as long** (pass 55: the replay 1,855 s against 682 s, Rules 32 minutes against ten). Two sweeps alone and together took 1,066 s each, against 2,065 s and 1,448 s beside the suite.
- **The replay digest replays the cuts, which repeat their games:** 73,175 of 93,401 lines. The repairs the user approved on 2026-10-13 (the design of pass 55, section 16) are to be made after pass 55's merge and are not yet in `gate.sh` or the tool.
"""


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    assert "\r\n" not in text, path
    return text


import sys

end, dur = sys.argv[1], sys.argv[2]
out = {}

text = read(DESIGN)
old = "(not run: the gate is the user's to start)\n"
assert text.count(old) == 1
out[DESIGN] = text.replace(old, GATE)

text = read(REVIEW)
edits = [
    ("The gate is not run: it is the user's to start.", "The gate was run later that day and passed: the last section."),
    ("**The gate is not run, and nothing is merged:** the gate, the merge, and the push are the user's to start.", "**The gate passed on 2026-10-13 (the last section), and nothing is merged:** the merge and the push are the user's to start."),
    ("The four test projects build with warnings as errors, 0 warnings.", "(A sentence here said that the four test projects build with warnings as errors; it was false, and the gate's section says why.)"),
    ("**`gate/pass55.conf` was not there when this was written:**", "**`gate/pass55.conf` was written after this section:**"),
]
for a, b in edits:
    assert text.count(a) == 1, a
    text = text.replace(a, b)
assert text.endswith("\n")
out[REVIEW] = text + REVIEW_GATE

text = read(TIMELOG)
assert text.rstrip("\n").endswith("The gate is not run; ask before starting it.")
out[TIMELOG] = text.rstrip("\n") + "\n" + TIME.replace("%END%", end).replace("%DUR%", dur)

text = read(RULES)
assert text.rstrip("\n").endswith("The hook caught every shell slip before it ran.")
out[RULES] = text.rstrip("\n") + "\n" + LESSONS

for path, text in out.items():
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", path.rsplit("/", 1)[1])
