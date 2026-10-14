"""Pass 45, the gate: the sweep's differing lines paired by their key, and grouped by how the answer changed.

Usage: python sweep_pairs45.py <before sweep> <after sweep> [action to print examples of]
"""
import collections
import re
import sys


def load(path):
    rows = {}
    for line in open(path, encoding="utf-8", errors="replace"):
        parts = line.rstrip("\n").split("\t")
        if len(parts) >= 5:
            rows[tuple(parts[:4])] = parts[4:]
    return rows


def shape(rest):
    outcome = rest[0]
    text = rest[1] if len(rest) > 1 else ""
    code = re.match(r"^([a-z.\-0-9]+)", text)
    return outcome + (" " + code.group(1) if outcome == "Refused" and code else "")


before, after = load(sys.argv[1]), load(sys.argv[2])
want = sys.argv[3] if len(sys.argv) > 3 else None
groups = collections.Counter()
examples = {}
for key, rest in before.items():
    other = after.get(key)
    if other is None:
        groups[(key[1], key[2], "MISSING AFTER")] += 1
        continue
    if rest == other:
        continue
    if key[1] != "ACT":
        group = (key[1], key[2], "read differs")
    else:
        a, b = shape(rest), shape(other)
        group = (key[1], key[2], f"{a} -> {b}" if a != b else f"{a}: " + ("same text, other hash" if rest[:2] == other[:2] else "text differs"))
    groups[group] += 1
    examples.setdefault(group, (key, rest, other))
for key in after:
    if key not in before:
        groups[(key[1], key[2], "NEW AFTER")] += 1
for group, count in sorted(groups.items(), key=lambda item: -item[1]):
    print(count, " | ".join(group))
    if want and group[1] == want and group in examples:
        key, rest, other = examples[group]
        print("    ", key[0], key[3][:160])
        print("     before:", " | ".join(rest)[:700])
        print("     after: ", " | ".join(other)[:700])
