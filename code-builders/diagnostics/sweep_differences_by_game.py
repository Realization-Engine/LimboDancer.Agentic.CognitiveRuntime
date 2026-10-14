# Pass 55's gate: which games and which lines of the sweep differ between the two sides, among keys both sides have.
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import re
import sys

S = WORK


def load(path):
    rows = {}
    for line in open(path, encoding="utf-8", errors="replace"):
        parts = line.rstrip("\n").split("\t")
        if len(parts) >= 5:
            rows[tuple(parts[:4])] = parts[4:]
    return rows


before, after = load(S + "/before-55/sweep.txt"), load(S + "/after-55/sweep.txt")
games = collections.Counter()
families = collections.Counter()
shown = collections.Counter()
for key, rest in before.items():
    other = after.get(key)
    if other is None or other == rest:
        continue
    games[key[0]] += 1
    families[(re.sub(r"-r\d+$", "", key[0]), key[2])] += 1
    if shown[key[2]] < 2 and (len(sys.argv) < 2 or key[0] == sys.argv[1]):
        shown[key[2]] += 1
        print("KEY", key)
        print("  before", [x[:300] for x in rest])
        print("  after ", [x[:300] for x in other])
print(sorted(families.items(), key=lambda kv: -kv[1]))
print(len(games), "games or cuts;", sorted(games.items(), key=lambda kv: -kv[1])[:12])
