"""Pass 40, task 40.6 (A5.11, A5.12, A5.131, p. 52): the example of A5.11 on board 1.

The stone building [F5] holds two German squads and a HS (the page has a crew; a HS counts the same). A third squad waits on the road in [F4]
and a fourth in [G5]; a Russian squad stands in [E6]. The squad from [F4] enters for three MF (two for the building, one for the Location it
overstacks); the Russian fire at it takes the overstacking DRM.

Usage: python make_stack.py [game id] [control]
  control: [F5] holds one squad and the HS, so the entry overstacks nothing and costs two MF.
"""
import json
import sys

from board_version import version

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p40-stack"
CONTROL = len(sys.argv) > 2 and sys.argv[2] == "control"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version("01")
started["map"] = {"reference": "bd01", "version": blob, "boards": [{"board": "bd01", "version": blob}]}
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T00:10:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(identifier, kind, definition, side, at):
    add("instance-created", {"instance": {"id": identifier, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR)}})


add("game-started", started)
unit("g-in-1", "asl:squad", "attacker-squad", "german", "bd01:F5:0")
if not CONTROL:
    unit("g-in-2", "asl:squad", "attacker-squad", "german", "bd01:F5:0")
unit("g-in-hs", "asl:half-squad", "attacker-half-squad", "german", "bd01:F5:0")
unit("g-mover", "asl:squad", "attacker-squad", "german", "bd01:F4:0")
unit("g-advancer", "asl:squad", "attacker-squad", "german", "bd01:G5:0")
unit("r-one", "asl:squad", "defender-squad", "russian", "bd01:E6:0")
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

label = ("Pass 40 task 40.6 control: [F5] holds a squad and a HS, so a squad entering from [F4] overstacks nothing (board 1)" if CONTROL
         else "Pass 40 task 40.6: the example of A5.11 (p. 52): two squads and a HS in [F5], a third squad entering from [F4] for three MF (board 1)")
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": label, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
