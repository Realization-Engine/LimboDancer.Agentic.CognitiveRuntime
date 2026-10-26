"""Increment 3d's board 3 reads: each named hex's center terrain and its hexsides' terrain (side 0 to 5), from the oracle's hex facts.

Usage: python board3_facts_3d.py D9 D10 M5 L4
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402
import gzip
import json

PATH = REPO + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bd03.hexfacts.json.gz"
data = json.load(gzip.open(PATH, "rt", encoding="utf-8"))
wanted = _sys.argv[1:]
for row in data["hexes"]:
    if row["hex"] in wanted:
        sides = ", ".join(f"{side['side']}:{side['terrain']}" + (f"/{side['hexsideTerrain']}" if side["hexsideTerrain"] else "") for side in row["hexsides"])
        print(row["hex"], row["center"]["terrain"], "base", row["baseLevel"], "stairs", row.get("stairway"), "|", sides)
