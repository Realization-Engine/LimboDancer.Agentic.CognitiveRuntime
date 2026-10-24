import sys
import xml.etree.ElementTree as ET

# Rank a trx file's tests by duration; print the top rows, the total, each class's sum, and any failures.
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
root = ET.parse(sys.argv[1]).getroot()
top = int(sys.argv[2]) if len(sys.argv) > 2 else 25


def seconds(text):
    h, m, s = text.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


rows = []
for r in root.iterfind(".//t:UnitTestResult", ns):
    rows.append((seconds(r.get("duration", "0:0:0")), r.get("testName"), r.get("outcome"), r))
rows.sort(key=lambda x: -x[0])
total = sum(x[0] for x in rows)
print(f"{len(rows)} tests, {total:.0f} s summed")
for d, name, outcome, _ in rows[:top]:
    print(f"{d:8.1f}  {outcome:7}  {name[:150]}")
by_class = {}
for d, name, _, _ in rows:
    cls = name.rsplit(".", 1)[0] if "(" not in name else name.split("(")[0].rsplit(".", 1)[0]
    by_class[cls] = by_class.get(cls, 0) + d
print("--- by class")
for cls, d in sorted(by_class.items(), key=lambda x: -x[1])[:12]:
    print(f"{d:8.1f}  {cls}")
for d, name, outcome, r in rows:
    if outcome == "Failed":
        msg = r.find(".//t:Message", ns)
        print("FAILED", name, "\n", (msg.text or "")[:800] if msg is not None else "")
