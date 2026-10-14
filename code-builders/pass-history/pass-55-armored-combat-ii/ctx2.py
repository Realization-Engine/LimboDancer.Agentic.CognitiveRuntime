"""As ctx.py, over pass55/pages: ctx2.py <regex> <before> <after> <pages...>"""
import io
import re
import sys

T = "E:/Archive/GitHub/dlandi/pass32-tools/pass55/pages/p%03d.txt"
pattern = re.compile(sys.argv[1], re.I)
before, after = int(sys.argv[2]), int(sys.argv[3])
for page in sys.argv[4:]:
    lines = io.open(T % int(page), encoding="utf-8").read().splitlines()
    last = -1
    for i, line in enumerate(lines):
        if pattern.search(line) and i > last:
            lo, hi = max(0, i - before), min(len(lines), i + after + 1)
            print("--- p.%s line %d" % (page, i + 1))
            print("\n".join(lines[lo:hi]))
            last = hi - 1
