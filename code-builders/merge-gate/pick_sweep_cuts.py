"""The cuts of a swept game for a rule pass (the user, 2026-10-13): each phase at four points of the game, chosen by phase name.

Every fourth cut lands on two phases only (the standing rules); this takes, for each phase a cut enters, the cuts at the first, the
last, and the two evenly spaced entries between. Prints the list as the tool's names, comma-separated, with '=<game>' for the game itself.

Usage: python pick_cuts.py <game> [points, default 4] [--table]
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import collections
import glob
import json
import re
import sys

S = WORK + "/store/5a7d1f000000400080000000000057d0/"
game = sys.argv[1]
points = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else 4
by_phase = collections.defaultdict(list)
for path in glob.glob(S + game + "-r*.game.json"):
    revision = int(re.search(r"-r(\d+)\.game\.json$", path).group(1))
    with open(path, encoding="utf-8") as f:
        last = json.load(f)["events"][-1]
    payload = last.get("payload", {})
    by_phase[payload.get("phase", last.get("type"))].append((revision, payload.get("turn"), payload.get("phasingSide")))
chosen = []
for phase, cuts in sorted(by_phase.items()):
    cuts.sort()
    count = min(points, len(cuts))
    picks = sorted({round(index * (len(cuts) - 1) / (count - 1)) if count > 1 else 0 for index in range(count)})
    chosen += [cuts[index][0] for index in picks]
    if "--table" in sys.argv:
        print(f"{phase}: {len(cuts)} cuts; taken " + ", ".join(f"r{cuts[i][0]} (turn {cuts[i][1]}, {cuts[i][2]})" for i in picks), file=sys.stderr)
print(",".join(["=" + game] + [f"{game}-r{revision}" for revision in sorted(chosen)]))
