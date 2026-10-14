"""Pass 45, the gate: the sweep's Ready fire plans, equal and differing, grouped by phase and by the plan's shape.

Usage: python sweep_fire45.py <before sweep> <after sweep>
"""
import collections
import re
import sys


def load(path):
    rows = {}
    for line in open(path, encoding="utf-8", errors="replace"):
        parts = line.rstrip("\n").split("\t")
        if len(parts) >= 6 and parts[1] == "ACT" and parts[2] == "asl.game.fire" and parts[4] == "Ready":
            rows[tuple(parts[:4])] = parts[4:]
    return rows


before, after = load(sys.argv[1]), load(sys.argv[2])
groups = collections.Counter()
shown = collections.Counter()
for key, rest in before.items():
    other = after.get(key)
    same = other == rest
    text = rest[1]
    phase = re.search(r"in the (\w+)", text)
    blocked = "los-blocked" in text
    weapon = "weapons=" in key[3] or "with" in text.split(";")[0]
    events = rest[-2].split(":")[0] if len(rest) >= 2 else "?"
    where = [index for index, (a, b) in enumerate(zip(rest, other or [])) if a != b]
    group = ("same" if same else "DIFFERS", phase.group(1) if phase else "?", "blocked" if blocked else "clear", f"events {events}", f"columns {where}")
    groups[group] += 1
    if not same and shown[group] < 1:
        shown[group] += 1
        print("example", key[0], key[3][:120], "\n   ", " | ".join(rest)[-260:], "\n   ", " | ".join(other)[-260:])
for group, count in sorted(groups.items()):
    print(count, group)
