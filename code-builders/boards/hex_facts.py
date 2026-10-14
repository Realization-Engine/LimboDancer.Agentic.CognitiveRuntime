"""Print the oracle's hex facts of a board for the hexes named.

Usage: python hexfacts.py 05 I4 I3 H3      (no hex: the shape of the file and its first hex)
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import gzip
import json
import sys

ORACLE = REPO + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bd"

data = json.load(gzip.open(ORACLE + sys.argv[1] + ".hexfacts.json.gz", "rt", encoding="utf-8"))
wanted = sys.argv[2:]
if isinstance(data, dict):
    print("keys:", list(data.keys())[:12])
    hexes = next((value for value in data.values() if isinstance(value, list) and value and isinstance(value[0], dict)), [])
else:
    hexes = data
if not wanted:
    print(json.dumps(hexes[0])[:1500])
for entry in hexes:
    name = entry.get("hex") or entry.get("name") or entry.get("id")
    if name in wanted:
        print(json.dumps(entry)[:900])
