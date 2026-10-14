"""Pass 50 documents after the merge: the design's, the review's, and the plan's status, and the merge's row in the time log.

Usage: python docs_merged50.py <docs root of the worktree> <start HH:MM> <end HH:MM>
"""
import sys

DOCS = sys.argv[1].rstrip("/") + "/"
start, end = sys.argv[2], sys.argv[3]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)
MERGED = ("Merged into main as 85d628bd and pushed on 2026-10-12 at the user's word (\"Proceed with the merge, push, and close the commit\"); "
          "the CI run of the merge was left for the user to read.")
EDITS = [
    ("ASL Card Play and Map Studio Redesign Plan.md",
     "2026-10-12: built on `feature/asl-backlog-pass-50`, the merge gate passed, not merged.**",
     "2026-10-12: merged into main as 85d628bd and pushed, with the gate passed; the CI run of the merge was left for the user to read.** It was built on `feature/asl-backlog-pass-50`."),
    ("Passes/ASL Unit Backlog Pass 50 Design.md",
     "**The gate was run on 2026-10-12 at the user's word and passed (section 12); nothing is merged.**",
     "**The gate was run on 2026-10-12 at the user's word and passed (section 12).** " + MERGED),
    ("Passes/ASL Unit Backlog Pass 50 Design.md",
     "**The gate passed. Nothing is merged or pushed.**",
     "**The gate passed.** " + MERGED + " Main had moved to cb5935b6 after the gate, by one commit in `prototypes/europe-hex-map` and three Campaign documents: "
     "nothing of the ASL solution and no file of the branch, so the gate's results stand for the merge, whose tree under `src/ASL` is the branch's with those three documents."),
    ("Passes/Scenario A1 Backlog Pass 50 Review 2026-10-12.md",
     "and passed; nothing is merged.**",
     "and passed.** " + MERGED),
]
texts = {}
for name, old, new in EDITS:
    text = texts.get(name) or open(DOCS + name, encoding="utf-8", newline="").read()
    assert "\r\n" not in text, name
    assert text.count(old) == 1, (name, old[:70], text.count(old))
    texts[name] = text.replace(old, new)
log = open(DOCS + "ASL Unit Time Log.md", encoding="utf-8", newline="").read()
assert log.endswith("|\n")
row = ("| The merge, at the user's word (\"Proceed with the merge, push, and close the commit. Don't monitor the CI\"): main had moved from b4ce31ef to cb5935b6 by one commit "
       "(the Campaign Atlas prototypes and three Campaign documents), no file changed on both sides and nothing of the ASL solution; a trial merge was clean; merged with --no-ff in a "
       "temporary worktree (main is checked out in a Codex worktree) as 85d628bd, its tree equal to the trial's; pushed with no force. The CI run was not watched: the user reads it. "
       f"Then the documents closed: the design's, the review's, and the plan's status, and this row, in a commit of their own on main | {start} | {end} | {minutes} min | "
       "The pass by the clock on 2026-10-12: the build, the reviews, and the documents 16:44 to 18:56, the gate 19:14 to 21:32, the merge after it. Pass 55 is not started; ask before starting it. |\n")
for name, text in texts.items():
    open(DOCS + name, "w", encoding="utf-8", newline="").write(text)
open(DOCS + "ASL Unit Time Log.md", "w", encoding="utf-8", newline="").write(log + row)
print("ok", minutes)
