"""The sweep lines of the gate that differ from the baseline's, for one group: sweep_lines.py <before answer text> [<after answer text>] [limit].
Lines are matched by their key (game, kind, action, arguments); prints the pair, cut to 600 characters each."""
import sys

W = "E:/Archive/GitHub/dlandi/asl-gate-work/"
CACHE = W + "baseline-cache/2b82cee3968bff1d/sweep.txt"
BEFORE_OWN = W + "before-60/sweep-own.txt"
AFTER = W + "after-60/sweep.txt"
before_text, after_text = sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else ""
limit = int(sys.argv[3]) if len(sys.argv) > 3 else 6


def read(*paths):
    lines = {}
    for path in paths:
        with open(path, encoding="utf-8") as source:
            for line in source:
                cells = line.rstrip("\n").split("\t")
                lines["\t".join(cells[:4])] = line.rstrip("\n")
    return lines


before, after = read(CACHE, BEFORE_OWN), read(AFTER)
shown = 0
for key, line in before.items():
    other = after.get(key)
    if other is None or other == line or before_text not in line or after_text not in other:
        continue
    print("BEFORE", line[:600])
    print("AFTER ", other[:600])
    print()
    shown += 1
    if shown >= limit:
        break
print(len(before), "lines before,", len(after), "after; keys only before:", len(set(before) - set(after)), "only after:", len(set(after) - set(before)))
