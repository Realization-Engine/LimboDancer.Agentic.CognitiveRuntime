# Pass 55: the documents closed after the merge. Every anchor is checked before any file is written.
# Usage: python close_docs.py <repository root> <file with the paragraph on the repaired gate's first run> <end time> <duration>
import sys

ROOT, RUN_FILE, END, DUR = sys.argv[1].rstrip("/"), sys.argv[2], sys.argv[3], sys.argv[4]
D = ROOT + "/src/ASL/docs/"
DESIGN = D + "Passes/ASL Unit Backlog Pass 55 Design.md"
REVIEW = D + "Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md"
TIMELOG = D + "ASL Unit Time Log.md"
RULES = D + "Prompts/Pass Standing Rules and Harness Lessons.md"
PLAN = D + "ASL Unit Backlog Passes Plan.md"
BACKLOG = D + "ASL Unit Backlog.md"
RUN = open(RUN_FILE, encoding="utf-8").read().strip()

AFTER = """
**After the gate.** Merged into main as 50987ff3 with `--no-ff` in a temporary worktree and pushed on 2026-10-13 at the user's word ("merge and push"); the merge's tree under `src/ASL` is the gated branch's. **The CI run of the merge failed, for a reason outside the pass:** it stopped at the locked restore, before any build or test, with NU1004. The workflow asks for .NET `10.0.x`; the October servicing release came out between two pushes, the run installed SDK 10.0.402, whose `Microsoft.AspNetCore.App.Internal.Assets` is 10.0.13, and MapStudio's lock file held 10.0.12 from SDK 10.0.401, which the green run of 20 hours before had found on the runner. The user chose to move to 10.0.402 rather than pin the workflow: the SDK was installed on this machine, the one lock entry regenerated, a locked restore of the solution run with it, and the file pushed as 53a22fcf. That run's restore passed; the rest of it was left for the user to read. The Docker check's image, `mcr.microsoft.com/dotnet/sdk:10.0`, was still at 10.0.401 that evening and fails its locked restore until its next image is out (backlog 59.2).

**The repairs, made after the merge** at the user's word, in the tools folder (`pass32-tools/gate`, `proof/Program.cs`):

- The proof tool writes its output a game at a time and turns a game's exception into an ERROR line; the replay passes over cut files; a name with a leading `=` matches one game; `PROOF_EXCLUDE` leaves games out by name.
- `gate.sh` has a `base` stage, which makes the baseline of the games made before the pass once and keeps it under a key (the base commit, the older games, `BOTH_SKIP`, the sweep list, the tool), and a `baseline` stage that reuses it and adds only the pass's own games. A stage stops unless the tool exited 0, wrote its file, and no game ended in an ERROR line.
- The trial holds the pass's own games and its extra sweep games on both sides, and two cuts of `p31c-tw`.
- A rule pass sweeps `p31c-tw` at each of its eight phases at four points of the game, 32 cuts of 127 (`pick_cuts.py`); `CODE_MOVE=1` in a pass's `.conf` sweeps them all.
- A `strict` stage formats the pass's changed test files and builds the solution with warnings as errors, and `strict-check` says whether that passed for the tree as it stands.
- The order of the stages: the baseline, the build, the trial, the proofs and Docker side by side, then the suite. It is to be timed at one gate before it is called settled.

""" + RUN + "\n"

BACKLOG_ROW = "| The CI workflow and the Docker check take the newest .NET 10 patch | Pass 55's merge, 2026-10-13 (the user chose to move to SDK 10.0.402 rather than pin the workflow) | (tools) | A decision to pin the SDK (a `global.json`, the workflow's version, the image's tag), or the lock file regenerated at each patch | The workflow asks for `10.0.x` and the Docker check for the image `sdk:10.0`. Each SDK patch changes `Microsoft.AspNetCore.App.Internal.Assets`, and the locked restore fails with NU1004 until MapStudio's lock file is regenerated (53a22fcf for 10.0.402). The image was still at 10.0.401 on 2026-10-13: the Docker check fails its restore until the 10.0.402 image is out, and `gate.sh docker` prints both versions. |\n"

TIME = """| The merge, at the user's word ("merge and push"): the fetch (origin/main at 681e8bdb, prototypes only, no file on both sides); merged with --no-ff in a temporary worktree as 50987ff3 and pushed | 18:31 | 18:33 | 2 min | The CI run failed at the locked restore (NU1004). |
| The CI failure read from the two runs' logs: the workflow's 10.0.x resolved to SDK 10.0.402, new that day; at the user's word ("Move to 10.0.402") the SDK installed by winget, MapStudio's lock entry regenerated, a locked restore run, pushed as 53a22fcf | 18:34 | 18:56 | 22 min | The installer alone took about 12 minutes. The Docker image was still at 10.0.401. |
| The tool repairs the user approved during the gate: the proof tool, gate.sh with its base, baseline, strict, and trial stages, the 32 cuts, the trial's check; run on pass 55's own data; the documents closed | 18:57 | %END% | %DUR% | See the design's section 16. |
"""

LESSONS = """- **CI takes the newest .NET 10 patch, and a locked restore fails on the day one comes out (pass 55's merge).** The run before had found SDK 10.0.401 on the runner; the merge's run downloaded 10.0.402 and stopped with NU1004 on MapStudio's lock file. Compare the "Set up .NET" step of the last green run with the failed one before reading anything else. `dotnet restore --force-evaluate` rewrites every lock file with CRLF and no final newline: commit the changed entry alone, with LF. The Docker check's image lags the SDK by a day or more; `gate.sh docker` prints the image's SDK beside the machine's.
- **The gate after its repairs (2026-10-13).** `base` when the branch is cut; `strict` before the documents commit; at the gate `baseline`, `build`, `trial`, `proofs` beside `docker`, then `suite`. `gate/tool_selftest.sh` checks the proof tool after a change to it in a minute. A pass that pins the packages again names `GameEvent.RulePackage` in `BOTH_SKIP`; the trial now meets it, since it sweeps two cuts of `p31c-tw` that plan moves and fire.
"""


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    assert "\r\n" not in text, path
    return text


def apply(text, edits):
    for old, new in edits:
        assert text.count(old) == 1, (old[:80], text.count(old))
        text = text.replace(old, new)
    return text


out = {}
out[DESIGN] = apply(read(DESIGN), [
    ("**Status:** Built to the merge gate on 2026-10-13", "**Status:** Merged into main as 50987ff3 and pushed on 2026-10-13, after the gate passed (section 16); the user approved the departures and the rulings that day. Built to the merge gate on 2026-10-13"),
    ("**The gate is not run:** it is the user's to start (section 16).", "The gate was run later that day (section 16)."),
    ("**The gate passed.** Nothing is merged: the merge and the push are the user's to start. `origin/main` moved", "**The gate passed.** `origin/main` moved"),
    ("**Approved by the user during the gate, to be made after the merge and the push**", AFTER.lstrip("\n") + "\n**What the user approved during the gate, as it was put, to be made after the merge and the push**"),
])
out[REVIEW] = apply(read(REVIEW), [
    ("**The gate passed on 2026-10-13 (the last section), and nothing is merged:** the merge and the push are the user's to start.", "**The gate passed on 2026-10-13 (the last section). Merged into main as 50987ff3 and pushed that day** at the user's word."),
    ("Nothing is merged; the merge and the push are the user's to start.\n", "Merged into main as 50987ff3 and pushed on 2026-10-13 at the user's word (\"merge and push\"). The CI run of the merge failed at its locked restore for a reason outside the pass, a .NET SDK patch published that day; the design's section 16 has it, and 53a22fcf moved the lock file.\n"),
])

text = read(PLAN)
lines = text.split("\n")
count = 0
for index, line in enumerate(lines):
    if (line.startswith("| R50.") or line.startswith("| R55.")) and "open to the user's veto" in line:
        assert line.count("open to the user's veto") == 1, line[:40]
        lines[index] = line.replace("open to the user's veto", "approved by the user on 2026-10-13 (\"All open proposals are approved\")")
        count += 1
assert count == 47, count
out[PLAN] = "\n".join(lines)

text = read(BACKLOG)
assert text.endswith("The fixes rest on the build and on reading. |\n")
out[BACKLOG] = text + BACKLOG_ROW

text = read(TIMELOG)
anchor = "| Stopped before the merge. Eleven tool repairs approved by the user, to be made after the merge and the push. |\n"
assert text.count(anchor) == 1 and text.endswith(anchor)
out[TIMELOG] = text + TIME.replace("%END%", END).replace("%DUR%", DUR)

out[RULES] = apply(read(RULES), [
    ("For the gate that is the trial stage of `gate.sh`, which the proofs stage enforces.", "For the gate that is the trial stage of `gate.sh`, which the proofs stage enforces; it holds the pass's own games and its extra sweep games on both sides."),
    ("not only into section 4.\n", "not only into section 4.\n13. **A sentence about a build or a test names the log it was read from.** An agent's report is not evidence of either. The strict build of the test projects is `gate.sh <pass> <base> strict`, run before the documents commit, and `strict-check` says whether it passed for the tree as it stands: the pass's test script builds with warnings not as errors, and pass 55's review said the opposite on an agent's word.\n"),
    ("2. **Build**, one commit a task, on the pass's own branch.", "2. **Build**, one commit a task, on the pass's own branch. Start `gate.sh <pass> <base> base` when the branch is cut: it makes the baseline of the games made before the pass once, and the gate reuses it."),
    ("5. **The documents:** the review,", "5. **The documents,** once `gate.sh <pass> <base> strict` has passed for the tree (section 0, point 13): the review,"),
    ("in this order: `baseline` in the background; `build`; `trial`, which must pass; `suite` in the background; `proofs`; `docker` on the committed branch.", "in this order: `baseline`, which reuses what `base` made; `build`; `trial`, which must pass; `proofs` and `docker` on the committed branch, side by side; then `suite`. (Pass 55's gate ran the stages side by side and each took two to three times as long. This order is to be timed at one gate before it is called settled.)"),
    ("The repairs the user approved on 2026-10-13 (the design of pass 55, section 16) are to be made after pass 55's merge and are not yet in `gate.sh` or the tool.", "Since the repairs of 2026-10-13 the replay passes over them (`PROOF_REPLAY_CUTS=1` replays them as before)."),
])
assert out[RULES].endswith("\n")
out[RULES] += LESSONS

for path, text in out.items():
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", path.rsplit("/", 1)[1])
