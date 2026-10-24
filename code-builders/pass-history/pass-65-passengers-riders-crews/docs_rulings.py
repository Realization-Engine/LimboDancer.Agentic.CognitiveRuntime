"""Pass 65 documents: rulings R65.1 to R65.33 inserted after the R60.27 row of the Passes Plan's section 5.

    python docs_rulings.py <fragment>

Asserts the anchor once, 33 rows in order, no R65 row already there. LF, UTF-8, no BOM.
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402

P = REPO + "/src/ASL/docs/ASL Unit Backlog Passes Plan.md"
rows = io.open(sys.argv[1], encoding="utf-8", newline="").read().replace("\r\n", "\n").strip("\n").split("\n")
assert [re.match(r"^\| (R65\.\d+) \|", r).group(1) for r in rows] == [f"R65.{n}" for n in range(1, 34)]
for r in rows:
    assert r.endswith(" |") and r.count('("start on Pass 65 without my intervention")') == 1 and "—" not in r, r[:40]
text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("﻿") and "| R65.1 |" not in text
lines = text.split("\n")
hits = [i for i, line in enumerate(lines) if line.startswith("| R60.27 |")]
assert len(hits) == 1 and lines[hits[0] + 1] == ""
lines[hits[0] + 1:hits[0] + 1] = rows
io.open(P, "w", encoding="utf-8", newline="").write("\n".join(lines))
print("inserted", len(rows), "rulings after line", hits[0] + 1)
