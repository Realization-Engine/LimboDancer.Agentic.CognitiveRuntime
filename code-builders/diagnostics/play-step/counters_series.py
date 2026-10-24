"""Per-second series of the main runtime counters from a dotnet-counters CSV, sampled every N seconds, plus a detail window.
Usage: counters_series.py <csv> <every N s> [<from HH:MM:SS> <to HH:MM:SS>]
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
every = int(sys.argv[2])
cols = [
    ("ws MB", "dotnet.process.memory.working_set (By)", 1e6),
    ("commit MB", "dotnet.gc.last_collection.memory.committed_size (By)", 1e6),
    ("g2 MB", "dotnet.gc.last_collection.heap.size (By)[gc.heap.generation=gen2]", 1e6),
    ("loh MB", "dotnet.gc.last_collection.heap.size (By)[gc.heap.generation=loh]", 1e6),
    ("alloc MB/s", "dotnet.gc.heap.total_allocated (By / 1 sec)", 1e6),
    ("cpu s", "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=user]", 1),
    ("gcpause s", "dotnet.gc.pause.time (s / 1 sec)", 1),
    ("g0", "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen0]", 1),
    ("g1", "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen1]", 1),
    ("g2", "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen2]", 1),
    ("locks", "dotnet.monitor.lock_contentions ({contention} / 1 sec)", 1),
    ("tp q", "dotnet.thread_pool.queue.length ({work_item} / 1 sec)", 1),
]


def line(s):
    r = rows[s]
    return f"{s:%H:%M:%S} " + " ".join(f"{r.get(k, 0) / d:>{max(len(n), 6)}.1f}" for n, k, d in cols)


head = "time     " + " ".join(f"{n:>{max(len(n), 6)}}" for n, _, _ in cols)
stamps = sorted(rows)
print(head)
for s in stamps[::every]:
    print(line(s))
if len(sys.argv) > 4:
    day = stamps[0].date()
    lo = datetime.datetime.combine(day, datetime.time.fromisoformat(sys.argv[3]))
    hi = datetime.datetime.combine(day, datetime.time.fromisoformat(sys.argv[4]))
    print("\n" + head)
    for s in stamps:
        if lo <= s <= hi:
            print(line(s))
