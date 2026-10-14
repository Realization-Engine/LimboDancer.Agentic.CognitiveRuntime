"""Pass 35 documents: the inventory's two count tables of section 2, rewritten from its rows."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import re

P = REPO + "/src/ASL/docs/ASL Rule Inventory A to E.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable"]

by_chapter = collections.OrderedDict((letter, collections.Counter()) for letter in "ABCDE")
by_pass = collections.Counter()
chapter = None
for line in lines:
    if line.startswith("### Chapter "):
        chapter = line[len("### Chapter "):][0]
    if chapter and line.startswith("| "):
        c = [cell.strip() for cell in line.split("|")[1:-1]]
        if len(c) == 9 and re.match(r"^[A-E][0-9.]", c[0]) and c[3] in STATUSES:
            by_chapter[chapter][c[3]] += 1
            if c[6]:
                by_pass[c[6]] += 1

total = collections.Counter()
for counter in by_chapter.values():
    total.update(counter)
rows_all = sum(total.values())
with_rule = rows_all - total["not applicable"]
need = sum(by_pass.values())
built = total["built"]
print("rows", rows_all, "with a rule", with_rule, "built", built, "need a pass or ruling", need, "difference", with_rule - built - need)

end = next(i for i, line in enumerate(lines) if line.startswith("## 3. Reading the tables"))
changed = 0
for i in range(end):
    line = lines[i]
    m = re.match(r"^\| ([A-E])\. ([^|]+) \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \|$", line)
    if m:
        counter = by_chapter[m.group(1)]
        lines[i] = f"| {m.group(1)}. {m.group(2)} | {sum(counter.values())} | " + " | ".join(str(counter[s]) for s in STATUSES) + " |"
        changed += 1
    elif line.startswith("| **A to E** |"):
        lines[i] = f"| **A to E** | **{rows_all}** | " + " | ".join(f"**{total[s]}**" for s in STATUSES) + " |"
        changed += 1
    elif line.startswith("| Pass 35: "):
        lines[i] = "| Pass 35: Repairs: wrong results in rules already built (built 2026-10-09: 18 of its rows are built, 26 moved to the pass that takes what is left, and B14.2 moved to pass 135) | 0 |"
        changed += 1
    elif re.match(r"^\| (Pass \d+): [^|]+ \| \d+ \|$", line):
        m = re.match(r"^\| (Pass \d+)(: [^|]+) \| \d+ \|$", line)
        lines[i] = f"| {m.group(1)}{m.group(2)} | {by_pass[m.group(1)]} |"
        changed += 1
    elif line.startswith("| **All** |"):
        lines[i] = f"| **All** | **{need}** |"
        changed += 1
    elif "rows that carry a rule" in line:
        new = re.sub(r"Of the \d+ rows that carry a rule, \d+ are built and \d+ need a pass or a ruling\.",
                     f"Of the {with_rule} rows that carry a rule, {built} are built and {need} need a pass or a ruling.", line)
        assert new != line or str(need) in line
        lines[i] = new
        changed += 1
    elif re.match(r"^\*\*Where the \d+ rows go\.\*\*$", line):
        lines[i] = f"**Where the {need} rows go.**"
        changed += 1
print("lines rewritten:", changed)
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
