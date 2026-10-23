"""A follower run's step works split by phase, from a compact step log, beside another run's (the Berserk Charge Plan, task 3).

The log is a JSON list of [revision reached, interval ms] pairs, as the page notes them (each interval less the run's one-second pause is the
step's work). The phase of a step is the record's phase at the revision it reaches.
Usage: split_log.py <record.game.json> <after.json> [<before.json>]
"""
import bisect
import json
import statistics
import sys

record = json.load(open(sys.argv[1], encoding="utf-8"))
marks = []
for e in record["events"]:
    if e["type"] == "phase-changed":
        p = e["payload"]
        marks.append((e["revision"], "rtph" if p["phase"] == "rtph" else "other"))
revisions = [m[0] for m in marks]


def kind(revision):
    i = bisect.bisect_right(revisions, revision) - 1
    return marks[i][1] if i >= 0 else "other"


def split(path):
    pairs = json.load(open(path, encoding="utf-8"))
    by = {"rtph": [], "other": []}
    for revision, interval in pairs:
        by[kind(revision)].append((max(interval - 1000, 0), revision))
    return by


runs = [split(p) for p in sys.argv[2:]]
names = ["after", "before"][: len(runs)]
for k in ("rtph", "other"):
    for name, run in zip(names, runs):
        works = sorted(w for w, _ in run[k])
        if not works:
            continue
        p90 = works[int(len(works) * 0.9)]
        top = sorted(run[k], reverse=True)[:3]
        print(f"{k:5} {name:6} n={len(works):3} median={statistics.median(works):6.0f} ms  p90={p90:6.0f}  max={works[-1]:6.0f}  top {top}")
