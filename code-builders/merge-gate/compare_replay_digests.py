"""The replay digests of two sides compared game by game, whatever order the games were written in.

Prints one line a game with a differing line: the count and the game. With --own <names>, exits 1 when a game outside that
comma-separated list differs (the trial's rule). Exits 2 when either side holds an ERROR line or no line at all.

Usage: python replay_games.py <before> <after> [--own a,b,c]
"""
import collections
import sys


def load(path):
    games = collections.defaultdict(list)
    for line in open(path, encoding="utf-8", errors="replace").read().split("\n"):
        if line:
            games[line.split("\t", 1)[0]].append(line)
    return games


before, after = load(sys.argv[1]), load(sys.argv[2])
own = set(sys.argv[sys.argv.index("--own") + 1].split(",")) if "--own" in sys.argv else None
errors = [line for side in (before, after) for lines in side.values() for line in lines if line.split("\t")[1:2] == ["ERROR"]]
for line in errors[:10]:
    print("ERROR LINE", line[:300])
outside = 0
for game in sorted(set(before) | set(after)):
    a, b = before.get(game, []), after.get(game, [])
    differing = sum(1 for x, y in zip(a, b) if x != y) + abs(len(a) - len(b))
    if differing:
        print(f"{differing:7d} {game}")
        if own is not None and game not in own:
            outside += 1
if errors or not before or not after:
    sys.exit(2)
if outside:
    sys.exit(1)
