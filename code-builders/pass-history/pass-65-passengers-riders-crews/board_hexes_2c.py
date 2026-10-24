"""Pass 65, increment 2c: print a board's terrain by level for the hexes named (read-only): python board_hexes_2c.py bd03 F8 G8."""
import gzip
import json
import sys

BOARD = sys.argv[1] if len(sys.argv) > 1 else "bd01"
PATH = f"E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/{BOARD}.hexfacts.json.gz"
WANTED = sys.argv[2:] or ["B8", "B9", "B10", "C9", "C10", "A9", "A10"]

with gzip.open(PATH, "rt", encoding="utf-8") as handle:
    document = json.load(handle)

for hex_ in document["hexes"]:
    if hex_["hex"] in WANTED:
        locations = hex_.get("locations") or [hex_.get("center")]
        print(hex_["hex"], [(item.get("level"), item.get("terrain")) for item in locations if item])
