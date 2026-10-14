"""Pass 35 documents: the coverage document's count cell of each section row (sections 3 to 6), recomputed from the inventory.

    python docs_coverage_counts.py --check   lists the rows whose cell differs from the inventory's count
    python docs_coverage_counts.py           rewrites those cells
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import re
import sys

D = REPO + "/src/ASL/docs/"
inv = open(D + "ASL Rule Inventory A to E.md", encoding="utf-8").read().splitlines()
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
WORD = {"built": "built", "built with a deviation": "deviation", "partly": "partly", "refused": "refused", "not built": "not built"}

by_section = collections.defaultdict(collections.Counter)
for line in inv:
    if line.startswith("| "):
        c = [cell.strip() for cell in line.split("|")[1:-1]]
        if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
            section = c[0].split(".")[0]
            by_section[section][c[3]] += 1


def cell(section):
    counter = by_section[section]
    total = sum(counter[s] for s in WORD)
    return f"{total}: " + ", ".join(f"{counter[s]} {WORD[s]}" for s in WORD if counter[s])


P = D + "ASL Rule Coverage.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
start = next(i for i, line in enumerate(lines) if line.startswith("## 3. "))
end = next(i for i, line in enumerate(lines) if line.startswith("## 7. "))
pattern = re.compile(r"^\| ([A-E]\d*) \| ([^|]*) \| ([^|]*) \| (\d+: [^|]*?) \|")
differ = 0
for i in range(start, end):
    m = pattern.match(lines[i])
    if not m or m.group(1) not in by_section:
        continue
    new = cell(m.group(1))
    if m.group(4) != new:
        differ += 1
        print(f"{m.group(1)} [{m.group(3)}]: {m.group(4)}  ->  {new}")
        if "--check" not in sys.argv:
            lines[i] = lines[i][:m.start(4)] + new + lines[i][m.end(4):]
print("rows that differ:", differ)
if "--check" not in sys.argv:
    open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
