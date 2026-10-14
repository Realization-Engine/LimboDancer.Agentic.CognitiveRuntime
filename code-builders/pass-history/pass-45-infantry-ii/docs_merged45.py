"""Pass 45 documents after the merge: the plan's status, the review's status, and the merge's row in the time log.

Usage: python docs_merged45.py <docs root of the worktree> <start HH:MM> <end HH:MM>
"""
import sys

DOCS = sys.argv[1].rstrip("/") + "/"
start, end = sys.argv[2], sys.argv[3]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)
EDITS = [
    ("ASL Card Play and Map Studio Redesign Plan.md",
     "**Status, 2026-10-12:** built on the branch `feature/asl-backlog-pass-45`, cut from main at 5982767e,",
     "**Status, 2026-10-12:** merged into main as e5b6c0b5 and pushed on 2026-10-12, with the gate passed; the CI run of the merge (38295173548) was left for the user to read. It was built on the branch `feature/asl-backlog-pass-45`, cut from main at 5982767e,"),
    ("ASL Card Play and Map Studio Redesign Plan.md", "**Stopped before the merge at the user's word.**", "Merged at the user's word (\"Merge and push\")."),
    ("Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md",
     "## Status\n\nBuilt on branch `feature/asl-backlog-pass-45`, cut from main at 5982767e:",
     "## Status\n\nMerged into main as e5b6c0b5 on 2026-10-12, with the gate passed; the CI run of the merge (38295173548) was left for the user to read. Built on branch `feature/asl-backlog-pass-45`, cut from main at 5982767e:"),
    ("Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md",
     "**Nothing is merged or pushed:** the user said to stop before the merge.", "The merge and the push followed at the user's word."),
    ("Passes/Scenario A1 Backlog Pass 45 Review 2026-10-12.md", "- The merge and the push: each is the user's to start.\n", ""),
]
texts = {}
for name, old, new in EDITS:
    text = texts.get(name) or open(DOCS + name, encoding="utf-8", newline="").read()
    if "\r\n" in text:
        old, new = old.replace("\n", "\r\n"), new.replace("\n", "\r\n")
    assert text.count(old) == 1, (name, old[:70], text.count(old))
    texts[name] = text.replace(old, new)
log = open(DOCS + "ASL Unit Time Log.md", encoding="utf-8", newline="").read()
row = ("| The gate's documents committed (1a6e1e46). The merge, at the user's word (\"Merge and push\"): main still at 5982767e, so no file had changed on both sides; merged with --no-ff "
       "in a temporary worktree (main is checked out in a Codex worktree) as e5b6c0b5, its tree equal to the branch's head; pushed with no force, the worktree and its branch removed. "
       f"The CI run of the merge (38295173548) was started and not watched: the user reads it later. Then the documents closed: the plan's and the review's status, and this row, in a commit of their own on main | {start} | {end} | {minutes} min | "
       "The pass by the clock on 2026-10-12: the frame's commit to the merge, with the build ending at 11:19, the reviews and documents at 12:10, and the gate at 15:02. Pass 50 is not started; ask before starting it. |\n")
for name, text in texts.items():
    open(DOCS + name, "w", encoding="utf-8", newline="").write(text)
open(DOCS + "ASL Unit Time Log.md", "w", encoding="utf-8", newline="").write(log.rstrip("\n") + "\n" + row)
print("ok", minutes)
