# What the proof store holds: games, cuts of games, and the replay's lines by family.
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import os
import re

S = WORK
folder = S + "/store/5a7d1f000000400080000000000057d0"
names = [n[:-len(".game.json")] for n in os.listdir(folder) if n.endswith(".game.json")]
cuts = [n for n in names if re.search(r"-r\d+$", n)]
whole = [n for n in names if n not in cuts]
print(len(names), "files:", len(whole), "whole games,", len(cuts), "cuts")
parents = collections.Counter(re.sub(r"-r\d+$", "", n) for n in cuts)
print("cuts by game:", parents.most_common(8), "... of", len(parents), "games")
prefix = collections.Counter(re.match(r"[a-z]+\d*[a-z]?", n).group(0) if re.match(r"[a-z]+\d*[a-z]?", n) else n for n in whole)
print("whole games by prefix:", sorted(prefix.items(), key=lambda kv: -kv[1]))
lines = collections.Counter()
with open(S + "/before-55/replay.txt", encoding="utf-8") as f:
    for line in f:
        game = line.split("\t", 1)[0]
        lines["cut" if re.search(r"-r\d+$", game) else "whole"] += 1
        if game.startswith("p31c-tw"):
            lines["p31c-tw family"] += 1
print("replay lines:", dict(lines))
