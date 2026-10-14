"""Print the inventory's rows whose pass column says Pass 50, one a block."""
import io
import sys

INV = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
out = io.open(sys.argv[1], "w", encoding="utf-8", newline="\n") if len(sys.argv) > 1 else sys.stdout
n = 0
with io.open(INV, encoding="utf-8", newline="") as f:
    for no, line in enumerate(f, 1):
        cells = [c.strip() for c in line.rstrip("\r\n").split("|")]
        if len(cells) < 10 or cells[7] != "Pass 50":
            continue
        n += 1
        out.write("%d  %s  %s  p.%s  [%s]\n  code: %s\n  test: %s\n  note: %s\n\n" % (
            no, cells[1], cells[2], cells[3], cells[4], cells[5], cells[6], cells[8]))
out.write("rows: %d\n" % n)
