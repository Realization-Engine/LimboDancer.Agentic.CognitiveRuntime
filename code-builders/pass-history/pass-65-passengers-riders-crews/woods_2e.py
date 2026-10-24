"""Pass 65, increment 2e: list a board's hexes whose ground level is the named terrain (read-only): python woods_2e.py bd03 Woods."""
import gzip
import json
import sys

BOARD = sys.argv[1] if len(sys.argv) > 1 else "bd03"
TERRAIN = sys.argv[2] if len(sys.argv) > 2 else "Woods"
PATH = f"E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/{BOARD}.hexfacts.json.gz"

with gzip.open(PATH, "rt", encoding="utf-8") as handle:
    document = json.load(handle)

found = []
for hex_ in document["hexes"]:
    locations = hex_.get("locations") or [hex_.get("center")]
    if any(item and item.get("level") == 0 and item.get("terrain") == TERRAIN for item in locations):
        found.append(hex_["hex"])
print(len(found), " ".join(found))
