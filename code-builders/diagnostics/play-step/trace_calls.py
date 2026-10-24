"""Count the outermost entries of a frame in a speedscope evented trace and their durations (sampled, so short calls may be missed).
Usage: trace_calls.py <trace.speedscope.json> <needle> [<parent needle>]
With a parent needle, also counts the frame's entries by the nearest enclosing frame that matches the parent needle's list (comma separated).
"""
import collections
import json
import sys

doc = json.load(open(sys.argv[1], encoding="utf-8"))
needle = sys.argv[2]
parents = sys.argv[3].split(",") if len(sys.argv) > 3 else []
frames = [f["name"] for f in doc["shared"]["frames"]]
target = {i for i, n in enumerate(frames) if needle in n}
for prof in doc["profiles"]:
    stack, opened, calls, by = [], None, [], collections.Counter()
    for ev in prof["events"]:
        if ev["type"] == "O":
            if ev["frame"] in target and not any(f in target for f in stack):
                opened = ev["at"]
                owner = next((p for f in reversed(stack) for p in parents if p in frames[f]), "other")
                by[owner] += 1
            stack.append(ev["frame"])
        elif stack:
            f = stack.pop()
            if f in target and not any(x in target for x in stack) and opened is not None:
                calls.append(ev["at"] - opened)
                opened = None
    if calls:
        print(f"{prof['name']}: {len(calls)} calls, {sum(calls):.0f} ms, each {', '.join(f'{c:.0f}' for c in calls[:20])}")
        for k, v in by.most_common():
            print(f"   from {k}: {v}")
