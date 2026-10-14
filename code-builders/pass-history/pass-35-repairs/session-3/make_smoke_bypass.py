"""Pass 35 review fix 10 (B.2): SMOKE's MF in an Infantry Bypass, one level up and on the level.

Board 3: a German squad Bypasses a woods hex that holds a burning wreck (SMOKE). Usage: make_smoke_bypass.py game from woods.
Only the game-started payload is taken from an earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME, FROM, WOODS = sys.argv[1], sys.argv[2], sys.argv[3]
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T04:55:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}
add("game-started", started)
add("instance-created", {"instance": {"id": "sq", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                       "position": {"at": f"bd03:{FROM}:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "r-tank", "kind": "asl:vehicle", "definition": "defender-tank", "side": "russian",
                                       "position": {"at": f"bd03:{WOODS}:0", "facing": "east"},
                                       "conditions": {"asl:concealed": False, "asl:hidden": False}}})
add("vehicle-wrecked", {"id": "r-tank", "burning": True})
add("instance-created", {"instance": {"id": "r-tank-blaze", "kind": "asl:fire", "position": {"at": f"bd03:{WOODS}:0"}}})
add("instance-created", {"instance": {"id": "r1", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": "bd03:O3:0"}, "conditions": dict(CLEAR)}})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": f"Pass 35 review fix 10: a squad in [{FROM}] Bypasses the woods of [{WOODS}], which hold a burning wreck's SMOKE (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
