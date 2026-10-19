"""Children breakdown and self CPU in a speedscope sampled-thread-time trace, within a time range.

Usage: trace_children.py <trace.speedscope.json> <needle> [<t0_ms> <t1_ms>]
Prints, for stacks holding a frame matching <needle> (outermost), the time by the frame right below it,
split into CPU_TIME and UNMANAGED_CODE_TIME (blocked or native) leaves; and the top self CPU frames overall in the range.
"""
import collections
import json
import sys

path, needle = sys.argv[1], sys.argv[2]
t0 = float(sys.argv[3]) if len(sys.argv) > 3 else float("-inf")
t1 = float(sys.argv[4]) if len(sys.argv) > 4 else float("inf")
doc = json.load(open(path, encoding="utf-8"))
frames = [f["name"] for f in doc["shared"]["frames"]]
target = {i for i, n in enumerate(frames) if needle in n}
cpu = {i for i, n in enumerate(frames) if n == "CPU_TIME"}
unm = {i for i, n in enumerate(frames) if n == "UNMANAGED_CODE_TIME"}

children = collections.Counter()
self_cpu = collections.Counter()
for prof in doc["profiles"]:
    stack = []
    last = None
    for ev in prof["events"]:
        t = ev["at"]
        if last is not None and stack and t > last:
            a, b = max(last, t0), min(t, t1)
            if b > a:
                d = b - a
                leaf = stack[-1]
                kind = "cpu" if leaf in cpu else "unmanaged" if leaf in unm else "other"
                if leaf in cpu and len(stack) > 1:
                    self_cpu[stack[-2]] += d
                for i, f in enumerate(stack):
                    if f in target:
                        below = stack[i + 1] if i + 1 < len(stack) else None
                        name = "(itself)" if below is None or below in cpu or below in unm else frames[below]
                        children[(name, kind)] += d
                        break
        last = t
        f = ev["frame"]
        if ev["type"] == "O":
            stack.append(f)
        elif stack and stack[-1] == f:
            stack.pop()
        elif f in stack:
            del stack[len(stack) - 1 - stack[::-1].index(f)]

total = sum(children.values())
print(f"under '{needle}': {total:.0f} ms")
for (name, kind), v in children.most_common(20):
    print(f"  {v:9.0f} ms  {kind:<9} {name[:170]}")
print("\ntop self CPU in range:")
for f, v in self_cpu.most_common(30):
    print(f"  {v:9.0f} ms  {frames[f][:170]}")
