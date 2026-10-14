"""Pass 40 documents after the merge: the plan's status, the review's and the design's status, and the merge's row in the time log.

Usage: python docs_merged.py <docs root of the worktree> <start HH:MM> <end HH:MM>
"""
import sys

DOCS = sys.argv[1].rstrip("/") + "/"
start, end = sys.argv[2], sys.argv[3]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)


def edit(name, old, new):
    text = open(DOCS + name, encoding="utf-8", newline="").read()
    assert text.count(old) == 1, (name, old[:70], text.count(old))
    open(DOCS + name, "w", encoding="utf-8", newline="").write(text.replace(old, new))


edit("ASL Card Play and Map Studio Redesign Plan.md",
     "**Status, 2026-10-11:** built on the branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc, one commit a task,",
     "**Status, 2026-10-12:** merged into main as 14dca517 and pushed on 2026-10-12, with the gate passed and CI green. It was built on the branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc, one commit a task,")
edit("Passes/Scenario A1 Backlog Pass 40 Review 2026-10-11.md",
     "## Status\n\nBuilt on branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc:",
     "## Status\n\nMerged into main as 14dca517 on 2026-10-12, with the gate passed and CI green. Built on branch `feature/asl-backlog-pass-40`, cut from main at 3c660afc:")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
row = ("| The gate's row committed (9a353539, ffdd3db7). The merge, under the user's word (\"merge and push only if the gate is clean\"): main still at 3c660afc, so no file had changed "
       "on both sides; merged with --no-ff in a temporary worktree (main is checked out in a Codex worktree) as 14dca517, pushed with no force, the worktree and its branch removed; "
       f"the CI run of the merge watched to its end, success (12 m 48 s). Then the documents closed: the plan's and the review's status, and this row, in a commit of their own on main | {start} | {end} | {minutes} min | "
       "Most of the time was the CI run. The pass by the clock: the frame at 19:12 on 2026-10-11 to the merge's CI at 01:53 on 2026-10-12, 6:41 in all, of which the gate and its "
       "three rounds of proofs took 1:47. Pass 45 is not started; ask before starting it. |\n")
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + row)
print("ok", minutes)
