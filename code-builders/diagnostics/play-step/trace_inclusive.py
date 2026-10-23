"""Inclusive time of frames matching a filter, over all threads, in a speedscope evented trace; CPU leaves only.
Each frame counts once per stack. Also prints the busiest threads.
Usage: trace_inclusive.py <trace.speedscope.json> <filter substring> [top N]
"""
import collections
import json
import sys

path, needle = sys.argv[1], sys.argv[2]
top = int(sys.argv[3]) if len(sys.argv) > 3 else 40
doc = json.load(open(path, encoding="utf-8"))
frames = [f["name"] for f in doc["shared"]["frames"]]
cpu = {i for i, n in enumerate(frames) if n == "CPU_TIME"}
incl = collections.Counter()
threads = []
for prof in doc["profiles"]:
    stack, last, busy = [], None, 0.0
    for ev in prof["events"]:
        t = ev["at"]
        if last is not None and stack and t > last and stack[-1] in cpu:
            d = t - last
            busy += d
            for f in set(stack):
                if needle in frames[f]:
                    incl[f] += d
        if ev["type"] == "O":
            stack.append(ev["frame"])
        elif stack:
            stack.pop()
        last = t
    threads.append((busy, prof.get("name", "")))
for busy, name in sorted(threads, reverse=True)[:5]:
    print(f"thread {name[:60]}: CPU {busy:.0f} ms")
for f, ms in incl.most_common(top):
    print(f"{ms:8.0f} ms  {frames[f][:170]}")
