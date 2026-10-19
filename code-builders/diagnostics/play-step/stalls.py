"""Find stretches where the allocation counter reads 0 while collections run, and the jump that ends each.
Usage: stalls.py <counters.csv>
"""
import collections
import csv
import datetime
import sys

rows = collections.defaultdict(dict)
with open(sys.argv[1], encoding="utf-8") as f:
    for row in csv.DictReader(f):
        stamp = datetime.datetime.strptime(row["Timestamp"], "%m/%d/%Y %H:%M:%S")
        rows[stamp][row["Counter Name"]] = float(row["Mean/Increment"])
alloc = "dotnet.gc.heap.total_allocated (By / 1 sec)"
g0 = "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen0]"
stamps = sorted(rows)
run = []
for s in stamps:
    a = rows[s].get(alloc, 0)
    if a == 0:
        run.append(s)
        continue
    if len(run) >= 3:
        gcs = sum(rows[x].get(g0, 0) for x in run)
        before = [rows[x].get(alloc, 0) for x in stamps if run[0] - datetime.timedelta(seconds=30) <= x < run[0] and rows[x].get(alloc, 0) > 0]
        rate = sorted(before)[len(before) // 2] if before else 0
        print(f"0 MB from {run[0]:%H:%M:%S} to {run[-1]:%H:%M:%S} ({len(run)} s, {gcs:.0f} gen 0 collections), then {a / 1e6:,.0f} MB at {s:%H:%M:%S}; "
              f"{len(run) + 1} s at the median rate before ({rate / 1e6:.1f} MB/s) = {(len(run) + 1) * rate / 1e6:,.0f} MB")
    run = []
