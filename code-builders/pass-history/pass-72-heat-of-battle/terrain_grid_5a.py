"""Pass 72, increment 5a: a board's center terrain as a grid of columns and rows, one letter a hex, from the oracle's hex facts.

    python terrain_grid_5a.py 07 A K < /dev/null

W water, o Open Ground, r road, b building, w woods, m marsh, g grain, h brush, . other; a hex's base level follows when not 0.
"""
import gzip
import json
import sys

ORACLE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/bin/Debug/net10.0/Oracle/bd{}.hexfacts.json.gz"


def letter(name):
    if name is None:
        return "?"
    if name in ("Water", "River", "Canal", "Shallow Water", "Shallow River"):
        return "W"
    if name == "Open Ground":
        return "o"
    if "Road" in name:
        return "r"
    if "Building" in name:
        return "b"
    return {"Woods": "w", "Marsh": "m", "Grain": "g", "Brush": "h"}.get(name, ".")


def main(board, first, last):
    with gzip.open(ORACLE.format(board), "rt", encoding="utf-8") as source:
        facts = json.load(source)
    hexes = {(hex_["col"], int(hex_["hex"].lstrip("ABCDEFGHIJKLMNOPQRSTUVWXYZ"))): hex_ for hex_ in facts["hexes"]}
    names = {}
    for hex_ in facts["hexes"]:
        names[hex_["hex"].rstrip("0123456789")] = hex_["col"]
    start, end = names[first], names[last]
    columns = [name for name, col in sorted(names.items(), key=lambda item: item[1]) if start <= col <= end]
    print("row " + " ".join(f"{name:>3}" for name in columns))
    for row in range(0, 11):
        cells = []
        for name in columns:
            hex_ = hexes.get((names[name], row))
            if hex_ is None:
                cells.append("   ")
                continue
            mark = letter(hex_["center"].get("terrain"))
            cells.append(f"{mark:>2}{'' if hex_['baseLevel'] == 0 else hex_['baseLevel'] % 10}".rjust(3))
        print(f"{row:>3} " + " ".join(cells))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
