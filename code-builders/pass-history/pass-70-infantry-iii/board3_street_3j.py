"""Increment 3j's board 3 read: the road hexes flanked by building hexes on both sides of the road (A11.8), as Maps' RoadFlanks reads them, from the
oracle's hex facts. A road hexside is one whose terrain name holds "Road"; the sides of the road are the runs of the other hexsides between them.

Usage: python board3_street_3j.py
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402
import gzip
import json

PATH = REPO + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bd03.hexfacts.json.gz"
data = json.load(gzip.open(PATH, "rt", encoding="utf-8"))
by_index = {(row["col"], row["row"]): row for row in data["hexes"]}


def neighbor(col, row, side):
    even = col % 2 == 0
    if side == 0:
        return col, row - 1
    if side == 1:
        return col + 1, row + (0 if even else -1)
    if side == 2:
        return col + 1, row + (1 if even else 0)
    if side == 3:
        return col, row + 1
    if side == 4:
        return col - 1, row + (1 if even else 0)
    return col - 1, row + (0 if even else -1)


def building(row):
    return row is not None and "Building" in (row["center"]["terrain"] or "")


for (col, row), hexrow in sorted(by_index.items()):
    sides = hexrow["hexsides"]
    road = [("Road" in (side["terrain"] or "")) for side in sides]
    if sum(road) < 2:
        continue
    start = road.index(True)
    runs, run = [], None
    for step in range(1, 7):
        at = (start + step) % 6
        if road[at]:
            if run is not None:
                runs.append(run)
                run = None
            continue
        run = run if run is not None else []
        other = by_index.get(neighbor(col, row, at))
        if sides[at]["onMap"] and building(other):
            run.append(other["hex"])
    flanked = sum(1 for item in runs if item) >= 2
    if flanked:
        print(hexrow["hex"], hexrow["center"]["terrain"], "sides:", runs)
