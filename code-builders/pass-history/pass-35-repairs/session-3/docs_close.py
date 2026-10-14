"""Pass 35: the documents closed after the merge. Usage: docs_close.py <repository root> <HH:MM now>. Exact anchors, each asserted once."""
import sys

ROOT = sys.argv[1].rstrip("/") + "/src/ASL/docs/"
NOW = sys.argv[2]


def edit(name, pairs):
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:60])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))


edit("Passes/Scenario A1 Backlog Pass 35 Review 2026-10-09.md", [
    ("Built on branch `feature/asl-backlog-pass-35`, local and not pushed: the seventeen tasks",
     "Built on branch `feature/asl-backlog-pass-35`: the seventeen tasks"),
    ("**The gate was run on 2026-10-10 at the user's word, at c22b70ce, and passed** (the section \"The gate\" below). Not merged as this is written.",
     "**The gate was run on 2026-10-10 at the user's word, at c22b70ce, and passed** (the section \"The gate\" below). **Merged into main as 5355b7f6 "
     "on 2026-10-10 at the user's word and pushed; the CI run of the merge passed.** Main had moved six commits past the branch point, to 4b30d429 "
     "(the europe-hex-map prototype and eight documents); no file was changed on both sides, the merge had no conflict, and CI on the merged tree "
     "is the test of the two together."),
])
edit("Passes/ASL Unit Backlog Pass 35 Design.md", [
    ("section 19 is the pass as built. The gate is the user's to start.",
     "section 19 is the pass as built. The gate ran and passed on 2026-10-10 (the review's section \"The gate\"), and the pass was merged into main "
     "as 5355b7f6 and pushed the same day, with CI green."),
])
edit("ASL Unit Time Log.md", [
    ("Docker Desktop was down and was started. Stopped before the merge |",
     "Docker Desktop was down and was started. Stopped before the merge |\n"
     "| The gate's documents committed (aa78017c). The merge, at the user's word: main found six commits past the branch point (f3b7c47 to 4b30d429, "
     "the europe-hex-map prototype and eight documents), no file changed on both sides, a trial merge clean; merged with --no-ff in a temporary "
     "worktree (main is checked out in a Codex worktree) as 5355b7f6, pushed, the worktree and its branch removed; the CI run of the merge watched "
     "to its end, success. Then, at the user's word, the documents closed: the review's and the design's status, and this row, in a commit of "
     f"their own on main | 09:43 | {NOW} | {int(NOW[:2]) * 60 + int(NOW[3:]) - (9 * 60 + 43)} min | Most of the time was the CI run. The merged tree "
     "was not tested locally before the push, at the user's choice; CI was its test. The pass by the clock in the fourth session: 1:42 of work "
     "before the gate, 1:15 for the gate. Pass 40 is not started |"),
])
