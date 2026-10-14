"""Pass 40, task 40.8 (A4.62, p. 51): Hazardous Movement in every fire phase.

Board 3. A Russian crew mans an AT Gun in the Open Ground of [D10]; a German squad stands in [F10], which sees [D10] and [D9] at range 2.
It is the Russian MPh. In the game the crew pushes its Gun to [D9] and is fired on there in the MPh, and again in the DFPh. In the control
the Gun is not pushed, and the DFPh fire takes no Hazardous Movement.

Usage: python make_hazard.py [game id]
"""
import json
import sys

from board_version import version

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p40-hazard"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version("03")
started["map"] = {"reference": "bd03", "version": blob, "boards": [{"board": "bd03", "version": blob}]}
started["phasingSide"] = "russian"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T01:10:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}

add("game-started", started)
add("instance-created", {"instance": {"id": "r-crew", "kind": "asl:crew", "definition": "defender-crew", "side": "russian",
                                       "position": {"at": "bd03:D10:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "r-gun", "kind": "asl:gun", "definition": "defender-at-gun", "side": "russian",
                                       "position": {"at": "bd03:D10:0", "facing": "east"}, "holding": {"holder": "r-crew", "role": "manned"},
                                       "conditions": {"asl:malfunctioned": False}}})
add("instance-created", {"instance": {"id": "g-one", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                       "position": {"at": "bd03:F10:0"}, "conditions": dict(CLEAR)}})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "russian"})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 40 task 40.8: a Russian crew with its AT Gun in [D10], a German squad in [F10]; the Russian MPh (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
