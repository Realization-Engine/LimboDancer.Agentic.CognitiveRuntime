"""Print the counters' seconds around a wall-clock window (ms since the epoch), and run totals.
Usage: window.py <counters.csv> <wall_from_ms> <wall_to_ms>
"""
import collections
import csv
import datetime
import sys

path, w0, w1 = sys.argv[1], float(sys.argv[2]), float(sys.argv[3])
rows = collections.defaultdict(dict)
with open(path, encoding="utf-8") as f:
    for row in csv.DictReader(f):
        stamp = datetime.datetime.strptime(row["Timestamp"], "%m/%d/%Y %H:%M:%S").timestamp() * 1000
        rows[stamp][row["Counter Name"]] = float(row["Mean/Increment"])
alloc = "dotnet.gc.heap.total_allocated (By / 1 sec)"
cpu = "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=user]"
g0 = "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen0]"
g1 = "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen1]"
g2 = "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen2]"
for stamp in sorted(rows):
    if w0 - 4000 <= stamp <= w1 + 4000:
        v = rows[stamp]
        print(f"{datetime.datetime.fromtimestamp(stamp / 1000):%H:%M:%S} alloc {v.get(alloc, 0) / 1e6:9.1f} MB  user cpu {v.get(cpu, 0):5.2f} s  gen0 {v.get(g0, 0):.0f} gen1 {v.get(g1, 0):.0f} gen2 {v.get(g2, 0):.0f}")
stamps = sorted(rows)
print(f"run: {len(stamps)} seconds, allocated {sum(rows[s].get(alloc, 0) for s in stamps) / 1e9:.2f} GB, seconds with an alloc entry: {sum(1 for s in stamps if alloc in rows[s])}")
