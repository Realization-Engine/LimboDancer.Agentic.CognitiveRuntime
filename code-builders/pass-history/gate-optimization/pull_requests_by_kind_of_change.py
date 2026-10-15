"""Which changes reach main through a pull request and a green CI run (the user, 2026-10-13).

The merge that brought code-builders in broke CI for three runs: it changed no file of the solution and was pushed without a test run,
and an architecture test lists every .py file of the repository. The rule that follows is by kind of change, not for every push: code,
projects, lock files, the workflow, anything under code-builders, and any commit that adds, removes, or renames a file go through a pull
request; an edit to the text of an existing document goes straight. build-and-test/how_to_reach_main.sh answers which. The workflow now
runs for code-builders too, or a tools-only pull request would start no run.

Usage: python pull_requests_by_kind_of_change.py <repository root>
"""
import sys

ROOT = sys.argv[1].rstrip("/")
RULES = ROOT + "/src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md"
WORKFLOW = ROOT + "/.github/workflows/asl-authoring-ci.yml"
README = ROOT + "/code-builders/README.md"

RULE = ("- **How a change reaches main** (the user, 2026-10-13). By a pull request and a green CI run before the merge: code, project files, "
        "lock files, the workflow, anything under `code-builders`, and any commit that adds, removes, or renames a file, a document included. "
        "Straight to main, with the CI run read afterwards: edits to the text of documents that already exist, and nothing else. "
        "`bash code-builders/build-and-test/how_to_reach_main.sh` answers which, from the branch's diff, and names the files main has changed "
        "since the branch was cut. The reason is the day's own: the merge that brought `code-builders` in changed no file of the solution, was "
        "pushed without a test run, and broke an architecture test that lists every `.py` file of the repository, for three CI runs; some tests "
        "read the whole repository, so any file can change them. The user's standing permission: for a branch the user has said to push, the "
        "pull request is opened without a further word; the merge is still the user's to start. A run made before main moved stands when "
        "main's newer commits touch no file the branch touches. The Codex session pushes to main without a pull request; a red run on main may "
        "be its or mine, so read whose commit a failing test belongs to before anything else.\n")

EDITS = {
    WORKFLOW: [('      - "src/ASL/**"\n', '      - "src/ASL/**"\n      - "code-builders/**"\n', 2)],
    RULES: [
        ("- **No force push.** Never stop Visual Studio.\n", "- **No force push.** Never stop Visual Studio.\n" + RULE, 1),
        ("7. **Stop before the merge** for the user's word, with the pull request's CI run green.",
         "7. **Stop before the merge** for the user's word, with the pull request's CI run green (section 2, \"How a change reaches main\", holds for every branch, not only a pass's).", 1),
    ],
    README: [
        ("| `build-and-test/` | The Studio build, a filtered test run, the whitespace formatter, the commit helpers. |",
         "| `build-and-test/` | The Studio build, a filtered test run, the whitespace formatter, the commit helpers, and `how_to_reach_main.sh`, which says whether a branch needs a pull request and a CI run or may go straight. |", 1),
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
