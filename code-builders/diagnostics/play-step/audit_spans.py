"""Server-side spans of the user's proposals and confirms, from the audit log, placed in the game's turn and phase by time.

For each correlation in a time window: check -> gate (the propose: the check's end to the gate's verdict), and executor start -> end.
Usage: audit_spans.py <audit.jsonl> <game.json> <from ISO> <to ISO>
"""
import bisect
import collections
import datetime
import json
import sys

audit_path, game_path, t0, t1 = sys.argv[1:5]


def ts(text):
    text = text.replace("Z", "+00:00")
    head, _, tail = text.partition(".")
    frac, zone = tail[:-6], tail[-6:]
    return datetime.datetime.fromisoformat(f"{head}.{(frac + '000000')[:6]}{zone}")


lo, hi = ts(t0), ts(t1)
game = json.load(open(game_path, encoding="utf-8"))
marks = []
for e in game["events"]:
    if e["type"] == "phase-changed":
        p = e["payload"]
        marks.append((ts(e["time"]), f"T{p['turn']} {p['phasingSide'][:3]} {p['phase']}"))
stamps = [m[0] for m in marks]


def phase_at(t):
    i = bisect.bisect_right(stamps, t) - 1
    return marks[i][1] if i >= 0 else "setup"


by = collections.defaultdict(list)
for line in open(audit_path, encoding="utf-8"):
    r = json.loads(line)
    t = ts(r["time"])
    if lo <= t <= hi:
        by[r["correlation"]].append((t, r))

rows = []
for corr, items in by.items():
    items.sort(key=lambda x: x[0])
    action = items[0][1]["action"]
    checks = [t for t, r in items if r["type"] == "ConstraintEvaluated"]
    gates = [(t, r["type"]) for t, r in items if r["type"].startswith("Gate")]
    start = next((t for t, r in items if r["type"] == "ExecutorStarted"), None)
    end = next((t for t, r in items if r["type"] in ("ExecutorCompleted", "ExecutorFailed")), None)
    for t, kind in gates:
        before = [c for c in checks if c <= t]
        if before:
            rows.append((t, action, kind, (t - before[-1]).total_seconds(), phase_at(t)))
    if start and end:
        rows.append((end, action, "execute", (end - start).total_seconds(), phase_at(end)))

rows.sort()
summary = collections.defaultdict(list)
for t, action, kind, secs, phase in rows:
    summary[(phase.split(" ")[0], kind)].append(secs)
print("turn kind             n   median   max")
for (turn, kind), v in sorted(summary.items()):
    v.sort()
    print(f"{turn:4} {kind:15} {len(v):4} {v[len(v) // 2]:7.2f} {v[-1]:6.2f}")
print("\nslowest 25:")
for t, action, kind, secs, phase in sorted(rows, key=lambda r: -r[3])[:25]:
    print(f"{t:%H:%M:%S} {phase:16} {action:32} {kind:15} {secs:6.2f}s")
