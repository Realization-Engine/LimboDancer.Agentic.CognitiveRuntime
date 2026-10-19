"""Time on stacks that hold <outer>, and of it, time on stacks that also hold <inner> below it.
Usage: trace_nested.py <trace.speedscope.json> <outer> <inner> [<inner2> ...]
"""
import json
import sys

path, outer, inners = sys.argv[1], sys.argv[2], sys.argv[3:]
doc = json.load(open(path, encoding="utf-8"))
frames = [f["name"] for f in doc["shared"]["frames"]]
o_ids = {i for i, n in enumerate(frames) if outer in n}
i_ids = [{i for i, n in enumerate(frames) if inner in n} for inner in inners]
total = 0.0
nested = [0.0] * len(inners)
for prof in doc["profiles"]:
    stack = []
    last = None
    for ev in prof["events"]:
        t = ev["at"]
        if last is not None and stack and t > last:
            d = t - last
            pos = next((k for k, f in enumerate(stack) if f in o_ids), None)
            if pos is not None:
                total += d
                below = stack[pos + 1:]
                for j, ids in enumerate(i_ids):
                    if any(f in ids for f in below):
                        nested[j] += d
        last = t
        f = ev["frame"]
        if ev["type"] == "O":
            stack.append(f)
        elif stack and stack[-1] == f:
            stack.pop()
        elif f in stack:
            del stack[len(stack) - 1 - stack[::-1].index(f)]
print(f"{outer}: {total:.0f} ms")
for inner, v in zip(inners, nested):
    print(f"  of which under {inner}: {v:.0f} ms ({100 * v / total if total else 0:.0f}%)")
