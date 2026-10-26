"""Increment 3e's board 3 reads: the hexes whose center terrain is one of the names given, from the oracle's hex facts.

Usage: python board3_terrain_3e.py Grain Woods
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
for name in wanted:
    print(name, " ".join(row["hex"] for row in data["hexes"] if row["center"]["terrain"] == name))
