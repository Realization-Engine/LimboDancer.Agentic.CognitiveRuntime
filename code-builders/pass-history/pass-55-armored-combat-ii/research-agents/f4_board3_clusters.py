"""Name the five Canister clusters of a firer on a 33-column board (A = column 0), per facing, in hex names."""
import sys

FACINGS = ["east", "north-east", "north-west", "west", "south-west", "south-east"]
CLUSTERS = [((2, 0), (3, 0), (2, 1)), ((2, 0), (1, 1), (2, 1)), ((1, 1), (2, 1), (1, 2)), ((1, 1), (0, 2), (1, 2)), ((0, 2), (0, 3), (1, 2))]
ADJACENT = [("L",), ("L",), ("L", "R"), ("R",), ("R",)]


def neighbor(hex_, side):
    c, r = hex_
    even = c % 2 == 0
    return [(c, r - 1), (c + 1, r + (0 if even else -1)), (c + 1, r + (1 if even else 0)), (c, r + 1),
            (c - 1, r + (1 if even else 0)), (c - 1, r + (0 if even else -1))][side]


def name(hex_):
    c, r = hex_
    rows = 10 + (c % 2)
    if c < 0 or r < 0 or r >= rows:
        return "off"
    return "%s%d" % (chr(ord("A") + c), r + (1 if c % 2 == 0 else 0))


def index(text):
    c = ord(text[0]) - ord("A")
    return (c, int(text[1:]) - (1 if c % 2 == 0 else 0))


def walk(hex_, side, steps):
    for _ in range(steps):
        hex_ = neighbor(hex_, side)
    return hex_


for text in sys.argv[1:]:
    firer = index(text)
    print("firer [%s]" % text)
    for f in range(6):
        L, R = (1 - f) % 6, (2 - f) % 6
        parts = []
        for n, cluster in enumerate(CLUSTERS):
            hexes = [name(walk(walk(firer, L, i), R, j)) for i, j in cluster]
            adj = [name(neighbor(firer, L if side == "L" else R)) for side in ADJACENT[n]]
            parts.append("%d: %s via %s" % (n + 1, " ".join("[%s]" % h for h in hexes), "+".join("[%s]" % a for a in adj)))
        print("  %-10s %s" % (FACINGS[f], "; ".join(parts)))
