"""Prints, for each line that differs between the gate's two dump files (dump_both_sides.sh's main.txt and gate.txt), the text around the first
character where they part. Pass 70's gate: the early check's fire plans differed by hash alone.

    python dump_first_diffs.py <dump folder>
"""
import os
import sys

folder = sys.argv[1]
base = open(os.path.join(folder, "main.txt"), encoding="utf-8").read().splitlines()
gate = open(os.path.join(folder, "gate.txt"), encoding="utf-8").read().splitlines()
print("lines", len(base), len(gate))
for number, (left, right) in enumerate(zip(base, gate), start=1):
    if left == right:
        continue
    at = next((k for k in range(min(len(left), len(right))) if left[k] != right[k]), min(len(left), len(right)))
    print("line", number, "at", at)
    print("  BASE", left[max(0, at - 150):at + 250])
    print("  GATE", right[max(0, at - 150):at + 350])
