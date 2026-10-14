"""Pass 35, found in the Studio with fix 4: a Recalled AFV is offered no exit but by its Friendly Board Edge (D5.341).

Board 3: a Recalled German tank in Motion in [Q10] on the bottom edge, facing south-east; its Friendly Board Edge is the left. The page offered
"exit the map by the bottom edge" and the gate refused it. Only the game-started payload is taken from an earlier game.
"""
import json

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = "p35-recall-edge"
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T04:45:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


add("game-started", started)
add("instance-created", {"instance": {"id": "tank", "kind": "asl:vehicle", "definition": "attacker-tank", "side": "german",
                                       "position": {"at": "bd03:Q10:0", "facing": "south-east"},
                                       "conditions": {"asl:concealed": False, "asl:hidden": False, "asl:recalled": True, "asl:stun-recovery": True,
                                                      "asl:motion": True}}})
add("instance-created", {"instance": {"id": "r1", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": "bd03:O3:0"},
                                       "conditions": {"asl:broken": False, "asl:concealed": False, "asl:hidden": False}}})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 35: a Recalled tank in [Q10] on the bottom edge is offered no exit there, since its Friendly Board Edge is the left (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
