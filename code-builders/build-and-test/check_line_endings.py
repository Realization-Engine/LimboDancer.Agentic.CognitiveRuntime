"""Count CR bytes, BOMs, and em-dashes in the files the working tree changed or added (untracked ones too, but the live boards): check_line_endings.py

Pass 60: names are read one a line with core.quotepath=false, since a path with a space (the documents) broke the old split."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO  # noqa: E402
import subprocess


def git(*args):
    return subprocess.run(["git", "-c", "core.quotepath=false", "-C", REPO, *args], capture_output=True, text=True, encoding="utf-8").stdout.splitlines()


names = git("diff", "--name-only") + [name for name in git("ls-files", "--others", "--exclude-standard")
                                      if not name.startswith(("src/ASL/boards/", ".codex/", ".claude/"))]
bad = 0
for name in names:
    path = _os.path.join(REPO, name)
    if not _os.path.isfile(path):
        continue
    data = open(path, "rb").read()
    cr = data.count(b"\r")
    bom = data.startswith(b"\xef\xbb\xbf")
    dash = data.count(chr(0x2014).encode("utf-8"))
    if cr or bom:
        bad += 1
        print("CR %d BOM %s %s" % (cr, bom, name))
    if dash:
        print("em-dashes %d %s" % (dash, name))
print("files", len(names), "with CR or BOM", bad)
