"""Pass 70 documents: each coverage section's status computed from its count cell (section 1's rule), against the status column, and the
chapter tally of section 2. Read only."""
import collections
import re

P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Rule Coverage.md"
lines = open(P, encoding="utf-8").read().split("\n")
start = next(i for i, line in enumerate(lines) if line.startswith("## 3. "))
end = next(i for i, line in enumerate(lines) if line.startswith("## 7. "))
pattern = re.compile(r"^\| ([A-E]\d*) \| ([^|]*) \| ([^|]*) \| (\d+): ([^|]*?) \|")
tally = collections.defaultdict(collections.Counter)
for i in range(start, end):
    m = pattern.match(lines[i])
    if not m:
        continue
    total = int(m.group(4))
    parts = dict((w, int(n)) for n, w in re.findall(r"(\d+) (built|deviation|partly|refused|not built)", m.group(5)))
    done = parts.get("built", 0) + parts.get("deviation", 0)
    share = (done + parts.get("partly", 0) / 2) / total if total else 0
    if total and done == total:
        status = "Built"
    elif share >= 0.65:
        status = "Mostly built"
    elif share >= 0.30:
        status = "Partly"
    elif share > 0:
        status = "Mostly not built"
    elif parts.get("refused", 0) * 2 >= total:
        status = "Refused"
    else:
        status = "Not built"
    tally[m.group(1)[0]][status] += 1
    if status != m.group(3).strip():
        print(m.group(1), "column", m.group(3).strip(), "computed", status, round(share, 3))
for chapter in "ABCDE":
    print(chapter, dict(tally[chapter]))
