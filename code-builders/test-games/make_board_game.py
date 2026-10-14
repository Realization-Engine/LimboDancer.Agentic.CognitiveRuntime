"""Pass 40: a small test game on any single VASL board, at the German MPh of turn 1.

Usage: python make_board_game.py <game id> <board, as 05> "<label>" <side>=<Location>[,<Location>...] ...
  python make_board_game.py p40-bd05-road 05 "..." german=bd05:H3:0 russian=bd05:I5:0,bd05:G2:0

Each Location gets one squad of its side (attacker-squad for the German side, defender-squad for the Russian). Only the shape of the
game-started payload is taken from an earlier game; the map is written for the board named, with the version the Studio computes for it
(the Git blob id of the board archive's LOSData entry).
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import sys

from board_version import version

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME, BOARD, LABEL = sys.argv[1], sys.argv[2], sys.argv[3]
PLACED = dict(argument.split("=", 1) for argument in sys.argv[4:])

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version(BOARD)
started["map"] = {"reference": "bd" + BOARD, "version": blob, "boards": [{"board": "bd" + BOARD, "version": blob}]}
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-11T23:40:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}
DEFINITION = {"german": "attacker-squad", "russian": "defender-squad"}

add("game-started", started)
for side, locations in PLACED.items():
    for number, location in enumerate(locations.split(","), start=1):
        add("instance-created", {"instance": {"id": f"{side[0]}-{number}", "kind": "asl:squad", "definition": DEFINITION[side], "side": side,
                                               "position": {"at": location}, "conditions": dict(CLEAR)}})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": LABEL, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events), blob)
