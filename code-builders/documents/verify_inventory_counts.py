"""Compare the inventory's two count tables of section 2 with the counts of its rows, line by line. Prints each mismatch and a last line."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
import collections
import re

STATUSES = inventory_table.STATUSES
lines, _ = inventory_table.read_lines()
rows = inventory_table.rows(lines)
by_chapter = collections.OrderedDict()
by_pass = collections.Counter()
for row in rows:
    by_chapter.setdefault(row["chapter"], collections.Counter())[row["Status"]] += 1
    pass_cell = inventory_table.pass_of(row)
    if pass_cell:
        by_pass[pass_cell] += 1
    if row["Status"] in ("built", "not applicable") and pass_cell:
        print("a built row with a pass:", row["§"], pass_cell)
total = collections.Counter()
for counter in by_chapter.values():
    total.update(counter)
end = next(i for i, line in enumerate(lines) if line.startswith("## 3. Reading the tables"))
start = next(i for i, line in enumerate(lines) if line.startswith("## 2. The counts"))
bad = 0
seen = set()
in_pass_table = False
for i in range(start, end):
    line = lines[i]
    if line.startswith("| Pass or ruling |"):
        in_pass_table = True
        continue
    if not line.startswith("| ") or line.startswith("|---") or line.startswith("| Chapter |"):
        continue
    c = inventory_table.cells(line)
    if not in_pass_table:
        if c[0].startswith("**"):
            want = [f"**{sum(total.values())}**"] + [f"**{total[s]}**" for s in STATUSES]
        else:
            counter = by_chapter.get(c[0][0], collections.Counter())
            want = [str(sum(counter.values()))] + [str(counter[s]) for s in STATUSES]
        if c[1:] != want:
            bad += 1
            print("chapter line differs:", line, "want", want)
    else:
        if c[0] == "**All**":
            if c[1] != f"**{sum(by_pass.values())}**":
                bad += 1
                print("the All line differs:", line, sum(by_pass.values()))
            continue
        m = re.match(r"^(Pass \d+): ", c[0])
        key = m.group(1) if m else c[0]
        seen.add(key)
        if int(c[1]) != by_pass.get(key, 0):
            bad += 1
            print("pass line differs:", line[:80], "rows", by_pass.get(key, 0))
for key in by_pass:
    if key not in seen:
        bad += 1
        print("no table line for:", key, by_pass[key])
with_rule = sum(total.values()) - total["not applicable"]
sentence = f"Of the {with_rule} rows that carry a rule, {total['built']} are built and {sum(by_pass.values())} need a pass or a ruling."
if not any(sentence in line for line in lines[start:end]):
    bad += 1
    print("the sentence differs; want:", sentence)
if f"**Where the {sum(by_pass.values())} rows go.**" not in lines[start:end]:
    bad += 1
    print("the heading of the second table differs")
print("rows read:", len(rows))
print("mismatches:", bad)
