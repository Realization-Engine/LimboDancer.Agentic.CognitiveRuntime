"""The inventory's two count tables of section 2, rewritten from its rows."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
import collections
import re

STATUSES = inventory_table.STATUSES
P = inventory_table.PATH
lines, nl = inventory_table.read_lines()
rows = inventory_table.rows(lines)
by_chapter = collections.OrderedDict()
by_pass = collections.Counter()
for row in rows:
    by_chapter.setdefault(row["chapter"], collections.Counter())[row["Status"]] += 1
    if inventory_table.pass_of(row):
        by_pass[inventory_table.pass_of(row)] += 1

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
    m = re.match(r"^\| ([A-Z])\. ([^|]+) \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \| \d+ \|$", line)
    if m:
        counter = by_chapter.get(m.group(1), collections.Counter())
        lines[i] = f"| {m.group(1)}. {m.group(2)} | {sum(counter.values())} | " + " | ".join(str(counter[s]) for s in STATUSES) + " |"
        changed += 1
    elif re.match(r"^\| \*\*[^*]+\*\* \| \*\*\d+\*\* \|", line):
        label = re.match(r"^\| (\*\*[^*]+\*\*) \|", line).group(1)
        lines[i] = f"| {label} | **{rows_all}** | " + " | ".join(f"**{total[s]}**" for s in STATUSES) + " |"
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
