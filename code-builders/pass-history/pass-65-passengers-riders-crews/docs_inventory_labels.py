"""Pass 65 documents: after inventory_count_tables.py, restore its Pass 35 label, label pass 65's line, and count the rows that stand by a ruling."""
import collections
import io
import re

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
text = io.open(P, encoding="utf-8", newline="").read()
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
by_pass = collections.Counter()
for line in text.split("\n"):
    c = [x.strip() for x in line.split("|")[1:-1]]
    if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES and c[6]:
        by_pass[c[6]] += 1
EDITS = [
    ("| Pass 35: Repairs: wrong results in rules already built (built 2026-10-09: 18 of its rows are built, 26 moved to the pass that takes what is left, and B14.2 moved to pass 135) | 0 |",
     "| Pass 35: Repairs: wrong results in rules already built (built 2026-10-09: 18 of its rows are built, 26 moved to the pass that takes what is left, and B14.2 moved to pass 135, pass 130 since 2026-10-16) | 0 |"),
    ("| Pass 65: Passengers, Riders, and crews (proposed) | 0 |",
     "| Pass 65: Passengers, Riders, and crews (built to the gate, 2026-10-24: 30 of its rows are built, 2 of them with a deviation, and 53 moved to the pass that takes what is left) | 0 |"),
]
stands = [line for line in text.split("\n") if re.match(r"^\| Stands by its ruling \| \d+ \|$", line)]
assert len(stands) == 1, stands
EDITS.append((stands[0], f"| Stands by its ruling | {by_pass['Stands by its ruling']} |"))
for old, new in EDITS:
    assert text.count(old) == 1, old[:80]
for old, new in EDITS:
    text = text.replace(old, new)
io.open(P, "w", encoding="utf-8", newline="").write(text)
print("applied", len(EDITS), "edits; stands by its ruling:", by_pass["Stands by its ruling"])
