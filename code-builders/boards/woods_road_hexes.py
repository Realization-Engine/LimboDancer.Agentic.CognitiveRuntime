"""List the hexes VASL's fillWoodsRoadHexes would call woods-road: a road at the center and Woods on a hexside location.

Usage: python woods_road.py 01 02 03 04 05     (prints each board's hexes with the count of Woods hexside locations)
Also lists road-center hexes with any other non-open hexside location terrain, to see what else shares a hex with a road.
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import gzip
import json
import sys

ORACLE = REPO + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bd"
OPEN = {"Open Ground", "Dirt Road", "Paved Road"}

for board in sys.argv[1:]:
    hexes = json.load(gzip.open(ORACLE + board + ".hexfacts.json.gz", "rt", encoding="utf-8"))["hexes"]
    woods, other = [], collections.Counter()
    for entry in hexes:
        if "Road" not in (entry["center"]["terrain"] or ""):
            continue
        sides = [side["terrain"] for side in entry["hexsides"] if side["onMap"] and side["hexsideTerrain"] is None]
        count = sides.count("Woods")
        if count:
            woods.append(f'{entry["hex"]}({count})')
        for name in set(sides) - OPEN - {"Woods"}:
            other[name] += 1
    print("bd" + board, len(woods), "woods-road:", " ".join(woods))
    print("   other terrain on a road-center hex's hexside locations:", dict(other))
