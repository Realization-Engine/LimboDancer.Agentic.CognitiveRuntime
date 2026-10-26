"""Pass 70 documents: list the inventory rows whose status is not built and whose pass cell is blank, and the pass cells in use. Read only."""
import collections
import re

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
cells = collections.Counter()
for line in open(P, encoding="utf-8").read().split("\n"):
    if not line.startswith("| "):
        continue
    c = [cell.strip() for cell in line.split("|")[1:-1]]
    if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
        cells[c[6]] += 1
        if not c[6] and c[3] not in ("built", "not applicable"):
            print(c[0], "[" + c[3] + "]", c[7][-160:])
for name, n in sorted(cells.items()):
    if not name.startswith("Pass "):
        print(repr(name), n)
