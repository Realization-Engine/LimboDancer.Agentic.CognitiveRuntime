"""Count a record's steps (attempts: event id up to its last '-') by turn and phase, and the user's own time between steps."""
import collections
import datetime
import json
import sys

d = json.load(open(sys.argv[1], encoding="utf-8"))
events = d["events"]


def attempt(event_id):
    at = event_id.rfind("-")
    return event_id[:at] if at > 0 else event_id


phase = ("setup", 0)
steps = []
for e in events:
    if e["type"] == "phase-changed":
        p = e["payload"]
        phase = (f"T{p['turn']} {p['phasingSide'][:3]} {p['phase']}", e["revision"])
    a = attempt(e["eventId"])
    if not steps or steps[-1]["attempt"] != a:
        steps.append({"attempt": a, "first": e["revision"], "time": e["time"], "phase": phase[0], "n": 0, "kind": e["type"]})
    steps[-1]["n"] += 1
print("steps", len(steps))
by = collections.OrderedDict()
for s in steps:
    by.setdefault(s["phase"], []).append(s)
for k, v in by.items():
    print(f"{k:20} steps {len(v):4}  revs {v[0]['first']}-{v[-1]['first']}")
