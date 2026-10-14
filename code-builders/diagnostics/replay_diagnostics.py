"""From a replay digest, each game's D line: events, states, diagnostics. Prints the games whose states fall short or that hold diagnostics."""
import io
import sys

bad = 0
total = 0
for line in io.open(sys.argv[1], encoding="utf-8"):
    cells = line.rstrip("\n").split("\t")
    if len(cells) >= 4 and cells[1] == "D":
        total += 1
        diagnostics = cells[4] if len(cells) > 4 else ""
        if cells[2] != cells[3] or diagnostics.strip():
            bad += 1
            print("%s: %s events, %s states: %s" % (cells[0], cells[2], cells[3], diagnostics[:400]))
print("games: %d, with a fault: %d" % (total, bad))
