"""List the inventory's rows that name a pass. Usage: python inventory_rows.py "Pass 40" """
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
for line in open(P, encoding="utf-8").read().split("\n"):
    cells = [cell.strip() for cell in line.split("|")[1:-1]]
    if len(cells) == 9 and re.match(r"^[A-E][0-9.]", cells[0]) and cells[6] == sys.argv[1]:
        print(f"{cells[0]:9} {cells[3]:22} {cells[1][:34]:34} {cells[7][:110]}")
