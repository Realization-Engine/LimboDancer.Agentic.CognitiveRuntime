"""Print the inventory's rows for the rules named on the command line, or every row whose pass cell is the first argument after --pass."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
import sys

want = sys.argv[1:]
by_pass = None
if want and want[0] == "--pass":
    by_pass = want[1]
    want = []
lines, _ = inventory_table.read_lines()
for row in inventory_table.rows(lines):
    if row["§"] in want or (by_pass and inventory_table.pass_of(row) == by_pass):
        print(f"{row['line'] + 1}  {row['§']}  {row['Title']}  p.{row['Page']}  [{row['Status']}]  pass=[{inventory_table.pass_of(row)}]  by={row['By']}")
        print("  code:", row["Where"])
        if "Rules calculator" in row:
            print("  rules:", row["Rules calculator"])
        print("  test:", row["Test"])
        print("  note:", row["Note"])
        print()
