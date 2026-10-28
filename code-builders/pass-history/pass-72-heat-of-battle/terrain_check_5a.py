"""Pass 72, increment 5a: checks the hexes games_5a.py relies on, from the oracle's hex facts of a board: each pair's range in hexes and the terrain of the
hexes its LOS line passes through (an approximation: the hexes whose centers lie nearest the points along the line between the two centers; a level 0
Water hex is a depression, open to a LOS between level 0 hexes, B21.2), and the region a berserk charge can cross from a hex (no Water Obstacle entered, no
cliff hexside crossed) with the terrain in it the movement model does not review.

    python terrain_check_5a.py 07 < /dev/null
"""
import gzip
import json
import math
import sys

ORACLE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/bin/Debug/net10.0/Oracle/bd{}.hexfacts.json.gz"
WATER = {"Water", "Shallow Water", "River", "Shallow River", "Canal", "Ocean", "Shallow Ocean"}
REVIEWED = {"Open Ground", "Paved Road", "Dirt Road", "Brush", "Woods", "Orchard", "Grain", "Marsh", "Wooden Rubble", "Stone Rubble", "Rooftop", "Cellar"}
LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

# The pairs and starts games_5a.py relies on (board 7).
PAIRS = [("A3", "A8"), ("A3", "D0"), ("A3", "F0"), ("A3", "F1"), ("A3", "F2"), ("A3", "G1"), ("A3", "G2"), ("A2", "A8"), ("A2", "G2"), ("A1", "A8"), ("A8", "A3")]
STARTS = ["A3", "A8"]


def load(board):
    with gzip.open(ORACLE.format(board), "rt", encoding="utf-8") as source:
        facts = json.load(source)
    return {hex_["hex"]: hex_ for hex_ in facts["hexes"]}


def place(hexes, name):
    hex_ = hexes[name]
    return hex_["col"], int(name.lstrip(LETTERS))


def center(col, row):
    # Odd columns sit half a hex higher (their rows start at 0).
    return col * math.sqrt(3) / 2, row - (0.5 if col % 2 == 1 else 0.0)


def neighbour(col, row, side):
    even = col % 2 == 0
    return {0: (col, row - 1), 3: (col, row + 1),
            1: (col + 1, row - 1) if even else (col + 1, row), 2: (col + 1, row) if even else (col + 1, row + 1),
            5: (col - 1, row - 1) if even else (col - 1, row), 4: (col - 1, row) if even else (col - 1, row + 1)}[side]


def main(board):
    hexes = load(board)
    by_place = {place(hexes, name): name for name in hexes}
    terrain = {name: hex_["center"].get("terrain") for name, hex_ in hexes.items()}

    def distance(one, two):
        start, goal = place(hexes, one), place(hexes, two)
        frontier, seen, steps = [start], {start}, 0
        while frontier:
            if goal in frontier:
                return steps
            nxt = []
            for col, row in frontier:
                for side in range(6):
                    other = neighbour(col, row, side)
                    if other not in seen and -1 <= other[0] <= 40 and -1 <= other[1] <= 11:
                        seen.add(other)
                        nxt.append(other)
            frontier, steps = nxt, steps + 1
        return None

    def line(one, two):
        (x1, y1), (x2, y2) = center(*place(hexes, one)), center(*place(hexes, two))
        names = []
        for step in range(1, 200):
            t = step / 200
            x, y = x1 + (x2 - x1) * t, y1 + (y2 - y1) * t
            nearest = min(by_place, key=lambda item: (center(*item)[0] - x) ** 2 + (center(*item)[1] - y) ** 2)
            name = by_place[nearest]
            if name not in (one, two) and name not in names:
                names.append(name)
        return [f"{name}:{terrain[name]}" for name in names]

    for one, two in PAIRS:
        print(f"{one} -> {two}: range {distance(one, two)}; line through {line(one, two)}")

    cliffs = {(place(hexes, name), side["side"]) for name, hex_ in hexes.items() for side in hex_.get("hexsides", []) if side.get("cliff")}
    water = {place(hexes, name) for name, hex_ in hexes.items() if any(item.get("terrain") in WATER for item in hex_.get("locations", []))}
    for start in STARTS:
        seen, stack, gaps = {place(hexes, start)}, [place(hexes, start)], set()
        while stack:
            here = stack.pop()
            for item in hexes[by_place[here]].get("locations", []):
                name = item.get("terrain")
                if name and name not in REVIEWED and "Building" not in name:
                    gaps.add(name)
            for side in range(6):
                other = neighbour(here[0], here[1], side)
                if other in by_place and other not in water and other not in seen and (here, side) not in cliffs:
                    seen.add(other)
                    stack.append(other)
        print(f"region of {start}: {sorted(by_place[item] for item in seen)}; gaps {sorted(gaps)}")


if __name__ == "__main__":
    main(sys.argv[1])
