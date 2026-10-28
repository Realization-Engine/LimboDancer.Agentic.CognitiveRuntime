"""Pass 72, increment 5a: the Water Obstacle hexes and cliff hexsides of a board from the oracle's hex facts, and the regions a berserk charge can cross
between them, each with the terrain the movement model does not review (a gap of ruling R72.4), for the Studio games.

    python terrain_scan_5a.py 07 < /dev/null

Neighbours: a standard geomorphic board, an even column (A = 0) a half hex below the odd ones; hexsides 0 to 5 from north, clockwise (the oracle's
cliffs C4 side 1 and D3 side 4 of board 3 are one hexside, which this reading gives).
"""
import gzip
import json
import sys

ORACLE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/bin/Debug/net10.0/Oracle/bd{}.hexfacts.json.gz"
WATER = {"Water", "Shallow Water", "River", "Shallow River", "Canal", "Ocean", "Shallow Ocean"}
REVIEWED = {"Open Ground", "Paved Road", "Dirt Road", "Brush", "Woods", "Orchard", "Grain", "Marsh", "Wooden Rubble", "Stone Rubble", "Rooftop", "Cellar"}


def neighbour(col, row, side):
    even = col % 2 == 0
    return {0: (col, row - 1), 3: (col, row + 1),
            1: (col + 1, row - 1) if even else (col + 1, row), 2: (col + 1, row) if even else (col + 1, row + 1),
            5: (col - 1, row - 1) if even else (col - 1, row), 4: (col - 1, row) if even else (col - 1, row + 1)}[side]


def main(board):
    with gzip.open(ORACLE.format(board), "rt", encoding="utf-8") as source:
        facts = json.load(source)
    by_place = {(hex_["col"], hex_["row"]): hex_ for hex_ in facts["hexes"]}
    water = {place for place, hex_ in by_place.items() if any(item.get("terrain") in WATER for item in hex_.get("locations", []))}
    cliffs = {(place, side["side"]) for place, hex_ in by_place.items() for side in hex_.get("hexsides", []) if side.get("cliff")}
    print("water hexes:", sorted(by_place[place]["hex"] for place in water))
    print("cliff hexsides:", sorted((by_place[place]["hex"], side) for place, side in cliffs))
    seen, regions = set(), []
    for start in by_place:
        if start in water or start in seen:
            continue
        region, stack, gaps = [], [start], set()
        seen.add(start)
        while stack:
            place = stack.pop()
            region.append(place)
            hex_ = by_place[place]
            for item in hex_.get("locations", []):
                name = item.get("terrain")
                if name and name not in REVIEWED and "Building" not in name:
                    gaps.add(name)
            for side in hex_.get("hexsides", []):
                if side.get("slope"):
                    gaps.add("slope")
                if side.get("hexsideTerrain") not in (None, "Wall", "Hedge", "Cliff"):
                    gaps.add("hexside " + str(side.get("hexsideTerrain")))
            for side in range(6):
                other = neighbour(place[0], place[1], side)
                if other in by_place and other not in water and other not in seen and (place, side) not in cliffs:
                    seen.add(other)
                    stack.append(other)
        regions.append((len(region), sorted(by_place[place]["hex"] for place in region)[:12], sorted(gaps)))
    for size, sample, gaps in sorted(regions):
        print("region", size, sample, "gaps:", gaps)


if __name__ == "__main__":
    main(sys.argv[1])
