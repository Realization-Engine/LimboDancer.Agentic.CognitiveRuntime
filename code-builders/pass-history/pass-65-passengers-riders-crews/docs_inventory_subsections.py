"""Pass 65 documents: each "####" subsection's summary line of the inventory ("NN rows: x built, ...") against its rows.

    python docs_inventory_subsections.py --check   lists the lines that differ
    python docs_inventory_subsections.py           rewrites them
"""
import collections
import io
import re
import sys

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Inventory A to E.md"
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]
lines = io.open(P, encoding="utf-8", newline="").read().split("\n")
summary = re.compile(r"^\d+ rows?: ")
differ = 0
i = 0
while i < len(lines):
    if lines[i].startswith("#### "):
        j = i + 1
        while j < len(lines) and not summary.match(lines[j]) and not lines[j].startswith("#"):
            j += 1
        if j >= len(lines) or not summary.match(lines[j]):
            i += 1
            continue
        counter = collections.Counter()
        k = j + 1
        while k < len(lines) and not lines[k].startswith("#"):
            c = [x.strip() for x in lines[k].split("|")[1:-1]]
            if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
                counter[c[3]] += 1
            k += 1
        total = sum(counter.values())
        want = f"{total} row{'s' if total != 1 else ''}: " + ", ".join(f"{counter[s]} {s}" for s in STATUSES if counter[s]) + "."
        if lines[j] != want:
            differ += 1
            print(lines[i], "|", lines[j], "->", want)
            lines[j] = want
        i = k
    else:
        i += 1
print("summary lines that differ:", differ)
if "--check" not in sys.argv and differ:
    io.open(P, "w", encoding="utf-8", newline="").write("\n".join(lines))
    print("written")
