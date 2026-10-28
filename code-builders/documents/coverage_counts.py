"""The coverage document's count cell of each section row (sections 3 to 6), recomputed from the inventory.

    python coverage_counts.py --check   lists the rows whose cell differs from the inventory's count
    python coverage_counts.py           rewrites those cells
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
from paths import REPO  # noqa: E402
import collections
import re
import sys

WORD = {"built": "built", "built with a deviation": "deviation", "partly": "partly", "refused": "refused", "not built": "not built"}

by_section = collections.defaultdict(collections.Counter)
inventory_lines, _ = inventory_table.read_lines()
for row in inventory_table.rows(inventory_lines):
    by_section[row["§"].split(".")[0]][row["Status"]] += 1


def cell(section):
    counter = by_section[section]
    total = sum(counter[s] for s in WORD)
    return f"{total}: " + ", ".join(f"{counter[s]} {WORD[s]}" for s in WORD if counter[s])


P = REPO + "/src/ASL/docs/ASL Rule Coverage.md"
lines, nl = inventory_table.read_lines(P)
start = next(i for i, line in enumerate(lines) if line.startswith("## 3. "))
end = next(i for i, line in enumerate(lines) if line.startswith("## 7. "))
pattern = re.compile(r"^\| ([A-Z]\d*) \| ([^|]*) \| ([^|]*) \| (\d+: [^|]*?) \|")
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
