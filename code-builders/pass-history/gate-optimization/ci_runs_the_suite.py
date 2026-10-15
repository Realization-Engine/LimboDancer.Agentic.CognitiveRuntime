"""The gate after the user's ruling of 2026-10-13: the whole test suite and the Linux check are CI's, on the pass's pull request.

The local suite and the Docker check ran the same tests CI runs, a second and a third time, and took an hour between them. They leave
the gate and stay as tools run on demand. The gate keeps what CI cannot do (the text list, the replay digest, the sweep) and gains a
stage that reads the CI run of the branch's head. The workflow's time limit goes from 20 to 30 minutes: a run takes 14 and grows with
every pass, and a limit of 5 once cancelled every run from pass 5 to pass 12.

Usage: python ci_runs_the_suite.py <repository root>
"""
import sys

ROOT = sys.argv[1].rstrip("/")
GATE = ROOT + "/code-builders/merge-gate/gate.sh"
README = ROOT + "/code-builders/README.md"
RULES = ROOT + "/src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md"
WORKFLOW = ROOT + "/.github/workflows/asl-authoring-ci.yml"

OLD_HEADER = """# The gate's stages, in the order they are started (pass 55 ran them side by side and each took two to three times as long):
#   baseline  the pass's own games copied into the store and replayed and swept on the baseline's side; the rest reused from base
#   build     the solution with warnings as errors into bin/gate; the proof tool compiled against it          (about 6 min)
#   trial     the trial games, the pass's own games, and its extra sweep games on both sides; must pass       (about 3 min)
#   proofs    text list, replay digest, sweep on the gate's side, each compared with the baseline            (about 25 min; refuses without a
#             passed trial of this very build and these very skip lists)
#   docker    the Docker Linux check on the committed branch, started beside proofs                           (about 25 min)
#   suite     every test project from bin/gate, CI's Node viewport test, the chart supplement                 (about 35 min; after proofs)
"""
NEW_HEADER = """# The gate's stages, in order. The whole test suite and the Linux check are CI's, on the pass's pull request (the user, 2026-10-13):
#   baseline  the pass's own games copied into the store and replayed and swept on the baseline's side; the rest reused from base  (under 1 min)
#   build     the solution with warnings as errors into bin/gate; the proof tool compiled against it          (about 4 min)
#   trial     the trial games, the pass's own games, and its extra sweep games on both sides; must pass       (about 2 min)
#   proofs    text list, replay digest, sweep on the gate's side, each compared with the baseline            (about 7 min; refuses without a
#             passed trial of this very build and these very skip lists)
#   ci        once the branch is pushed and its pull request opened, at the user's word: says whether the CI run of the branch's head has
#             passed. CI runs the locked restore, the strict build, the Node viewport test, every test project, and the source checks on
#             Linux, on the branch merged with main (about 14 min). The merge waits for it.
# On demand, no longer stages of the gate:
#   suite     every test project from bin/gate on this machine, the Node viewport test, the chart supplement   (about 35 min)
#   docker    CI's steps in a Linux container on the committed branch                                          (about 25 min)
"""
CI_STAGE = """ci)
  BRANCH=$(git -C "$R" branch --show-current); HEAD_SHA=$(git -C "$R" rev-parse HEAD)
  [ "$(git -C "$R" rev-parse -q --verify "origin/$BRANCH")" = "$HEAD_SHA" ] || stop "the head ${HEAD_SHA:0:8} of $BRANCH is not what origin has (push the branch, at the user's word, and fetch)"
  RUNS=$(cd "$R" && gh run list --workflow asl-authoring-ci.yml --commit "$HEAD_SHA" --json status,conclusion,event,url --jq '.[] | "\\(.status) \\(.conclusion) \\(.event) \\(.url)"') || stop "gh could not list the CI runs"
  [ -n "$RUNS" ] || stop "no CI run for ${HEAD_SHA:0:8}: open the branch's pull request (the workflow runs on pull requests that touch src/ASL)"
  echo "$RUNS"
  if echo "$RUNS" | grep -q "^completed success "; then echo "CI passed for ${HEAD_SHA:0:8}"
  elif echo "$RUNS" | grep -qv "^completed "; then echo "CI is still running for ${HEAD_SHA:0:8}; ask again"; exit 3
  else stop "CI did not pass for ${HEAD_SHA:0:8}"; fi
  ;;
suite)
"""

EDITS = {
    GATE: [
        (OLD_HEADER, NEW_HEADER, 1),
        ('sed -n 2,26p "$0"', 'sed -n 2,30p "$0"', 2),
        ("suite)\n", CI_STAGE, 1),
    ],
    WORKFLOW: [("    timeout-minutes: 20\n", "    timeout-minutes: 30\n", 1)],
    README: [
        ("In short: `base` when the pass's branch is cut, `strict` before the documents commit, and at the gate `baseline`, `build`, `trial`, `proofs` beside `docker`, then `suite`.",
         "In short: `base` when the pass's branch is cut, `strict` before the documents commit, and at the gate `baseline`, `build`, `trial`, and `proofs` on this machine, about a quarter of an hour. Then the branch is pushed and its pull request opened, and CI runs the whole test suite on Linux; `ci` says whether that run has passed. The `suite` and `docker` stages run the same tests on this machine and in a Linux container, on demand.", 1),
    ],
    RULES: [
        ("6. **The merge gate**, only once step 4 passes, by `gate.sh` (section 0, point 2), in this order: `baseline`, which reuses what `base` made; `build`; `trial`, which must pass; `proofs` and `docker` on the committed branch, side by side; then `suite`. (Pass 55's gate ran the stages side by side and each took two to three times as long. This order is to be timed at one gate before it is called settled.) Every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault.",
         "6. **The merge gate**, only once step 4 passes, by `gate.sh` (section 0, point 2). On this machine, in this order, about a quarter of an hour: `baseline`, which reuses what `base` made; `build`; `trial`, which must pass; `proofs`. Every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault. Then stop for the user's word to push the branch and open its pull request: CI runs the locked restore, the strict build, the Node viewport test, every test project, and the source checks on Linux, on the branch merged with main, in about 14 minutes, and `gate.sh <pass> <base> ci` says whether that run has passed for the branch's head. (The user, 2026-10-13, in place of the local suite and the Docker check of 2026-09-27: both ran the tests CI runs, a second and a third time, and took an hour of pass 55's gate. They are `suite` and `docker`, run on demand.)", 1),
        ("7. **Stop before the merge** for the user's word. Merge with `--no-ff`, push, read the CI run.",
         "7. **Stop before the merge** for the user's word, with the pull request's CI run green. Merge with `--no-ff`, push, read the CI run of the merge.", 1),
        ("at the gate `baseline`, `build`, `trial`, `proofs` beside `docker`, then `suite`.",
         "at the gate `baseline`, `build`, `trial`, `proofs`, then CI on the pass's pull request (`ci`); `suite` and `docker` on demand.", 1),
        ("**The merge gate**\n\n", "**The merge gate**\n\n- Since 2026-10-13 the local suite and the Docker check are not stages of the gate: CI runs the same tests on the pass's pull request. The lessons below on running them hold when one is run on demand.\n", 1),
    ],
}
out = {}
for path, edits in EDITS.items():
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    assert "\r\n" not in text, path
    for old, new, count in edits:
        assert text.count(old) == count, (path[-30:], old[:60], text.count(old))
        text = text.replace(old, new)
    out[path] = text
for path, text in out.items():
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", path.rsplit("/", 1)[1])
