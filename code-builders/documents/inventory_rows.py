"""Print the inventory's rows for the rules named on the command line, or every row whose pass cell is the first argument after --pass."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import re
import sys

P = REPO + "/src/ASL/docs/ASL Rule Inventory A to E.md"
lines = open(P, encoding="utf-8", newline="").read().split("\n")
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
want = sys.argv[1:]
by_pass = None
if want and want[0] == "--pass":
    by_pass = want[1]
    want = []
for index, line in enumerate(lines):
    if not line.startswith("| "):
        continue
    c = [cell.strip() for cell in line.split("|")[1:-1]]
    if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
        if c[0] in want or (by_pass and c[6] == by_pass):
            print(f"{index + 1}  {c[0]}  {c[1]}  p.{c[2]}  [{c[3]}]  pass=[{c[6]}]  by={c[8]}")
            print("  code:", c[4])
            print("  test:", c[5])
            print("  note:", c[7])
            print()
