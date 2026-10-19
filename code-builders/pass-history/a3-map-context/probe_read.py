"""The A3 plan's task 2: a probe's step lines read by phase (the probe itself lived in Claude's Studio build only and was never committed).

Each line of the probe's TSV holds the work done while the follower reached and showed one revision: the board lookups (TryGetBoard) and
their time, the lookups made inside a placed map's Composed build, the BoardHandle.Resolve calls and the distinct Locations among them, the
Composed builds and their time (their lookups included), and the MapInPlay.Board calls. A line is put in the phase of its revision.

Usage: python probe_read.py <probe.tsv> <record game id> [first revision] [last revision]
"""
import json
import os
import statistics
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
probe, record = sys.argv[1], sys.argv[2]
first = int(sys.argv[3]) if len(sys.argv) > 3 else 0
last = int(sys.argv[4]) if len(sys.argv) > 4 else 10**9

phase_of, phase = {}, "?"
for event in json.load(open(LIVE + record + ".game.json", encoding="utf-8"))["events"]:
    if event["type"] == "phase-changed":
        phase = event["payload"]["phase"]
    phase_of[event["revision"]] = phase

rows = []
with open(probe, encoding="utf-8") as lines:
    header = next(lines).rstrip("\n").split("\t")
    for line in lines:
        cells = line.rstrip("\n").split("\t")
        if cells[0] == "-" or not first <= int(cells[0]) <= last:
            continue
        rows.append(dict(zip(header, cells)))

groups = {}
for row in rows:
    groups.setdefault("rout" if phase_of.get(int(row["revision"])) == "rtph" else "other", []).append(row)

print(f"{record}, revisions {first} to {last}: {len(rows)} steps")
for name, items in groups.items():
    def column(key):
        return [float(item[key]) for item in items]
    print(f"  {name}: {len(items)} steps")
    for key in header[2:]:
        values = column(key)
        print(f"    {key:22} median {statistics.median(values):12.2f}   min {min(values):12.2f}   max {max(values):12.2f}   sum {sum(values):14.2f}")
    resolves, distinct = column("resolves"), column("distinct_locations")
    repeats = [r / d for r, d in zip(resolves, distinct) if d]
    if repeats:
        print(f"    resolves per distinct Location: median {statistics.median(repeats):.1f}")
    lookups, lookup_ms = column("lookups"), column("lookup_ms")
    if sum(lookups):
        print(f"    a lookup's time: {sum(lookup_ms) * 1000 / sum(lookups):.3f} us")
